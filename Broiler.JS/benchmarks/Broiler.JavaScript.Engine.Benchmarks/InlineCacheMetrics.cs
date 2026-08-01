using System;
using System.Linq;
using System.Text.Json;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Engine.Benchmarks;

/// <summary>
/// Emits the property inline-cache hit rates the roadmap's phase C is quoted on, as JSON.
/// <para>
/// P1 is measured by hit rate rather than by wall clock — its headline is that a monomorphic
/// read went from <c>0 hits / 200 000 misses</c> to <c>199 999 / 1</c>. That result existed
/// only as a one-off observation from a harness outside the repository, which is what
/// <c>docs/performance-roadmap.md</c> §8.1 records as owed. This is its permanent home.
/// </para>
/// <para>
/// Each site runs in a fresh context with a <em>cold</em> cache: the source is evaluated to
/// produce a function (which compiles but does not run the body), then the counters are reset
/// and the loop is invoked exactly once. So the first iteration is expected to miss and every
/// later one to hit — a site reporting one miss per 200 000 reads is working, and a site
/// reporting 200 000 misses is the pre-P1 defect.
/// </para>
/// <para>
/// The counters are process-wide statics and are disabled by default (P0-1 made them opt-in
/// because they sat on the hottest path). Sites are therefore run sequentially, each inside a
/// recording scope, with a reset immediately before the measured invocation.
/// </para>
/// </summary>
internal static class InlineCacheMetrics
{
    private sealed record Site(string Name, string Source, int Iterations, string Expectation);

    private static readonly Site[] Sites =
    [
        new(
            "object-literal-read",
            "(function () { var o = { x: 1, y: 2 }; var s = 0; for (var i = 0; i < 200000; i++) { s = s + o.x; } return s; })",
            200_000,
            "hits ~= iterations - 1; this site already worked before P1"),

        new(
            "assigned-property-read",
            "(function () { var o = {}; o.x = 1; var s = 0; for (var i = 0; i < 200000; i++) { s = s + o.x; } return s; })",
            200_000,
            "hits ~= iterations - 1; was 0 hits / 200 000 misses before P1-1"),

        new(
            "class-field-read",
            "(function () { class C { constructor(v) { this.v = v; } } var c = new C(1); var s = 0; for (var i = 0; i < 200000; i++) { s = s + c.v; } return s; })",
            200_000,
            "hits ~= iterations - 1; was 0 / 200 000 before P1-1"),

        new(
            "inherited-method-call",
            "(function () { function P(v) { this.v = v; } P.prototype.get = function () { return this.v; }; var p = new P(1); var s = 0; for (var i = 0; i < 200000; i++) { s = s + p.get(); } return s; })",
            200_000,
            "hits ~= 2x iterations (method lookup + this.v); was 0 / 200 001 before P1-2, with 200 006 dictionary fallbacks"),

        new(
            "class-method-call",
            "(function () { class C { constructor(v) { this.v = v; } get() { return this.v; } } var c = new C(1); var s = 0; for (var i = 0; i < 200000; i++) { s = s + c.get(); } return s; })",
            200_000,
            "hits ~= 2x iterations; was 0 / 200 000 before P1-2"),

        new(
            "monomorphic-store",
            "(function () { var o = { x: 0 }; for (var i = 0; i < 200000; i++) { o.x = i; } return o.x; })",
            200_000,
            "store-cache hits ~= iterations - 1; P1-3 added the write side"),
    ];

    internal static void Write()
    {
        var rows = Sites.Select(Measure).ToArray();

        Console.WriteLine(JsonSerializer.Serialize(
            new
            {
                schema = "broiler.inline-cache-metrics/1",
                note = "Cold-cache single invocation per site. See docs/performance-roadmap.md phase C.",
                sites = rows,
            },
            new JsonSerializerOptions { WriteIndented = true }));
    }

    private static object Measure(Site site)
    {
        using var context = BenchmarkContext.Create();

        // Compiles the function without running its body, so the caches stay cold.
        var function = context.Eval(site.Source, $"{site.Name}.js");
        var arguments = new Arguments(JSUndefined.Value);

        PropertyOptimizationSnapshot snapshot;
        using (PropertyOptimizationDiagnostics.Enable())
        {
            PropertyOptimizationDiagnostics.Reset();
            function.InvokeFunction(in arguments);
            snapshot = PropertyOptimizationDiagnostics.Snapshot();
        }

        var lookups = snapshot.CacheHits + snapshot.CacheMisses;
        var stores = snapshot.StoreCacheHits + snapshot.StoreCacheMisses;

        return new
        {
            site = site.Name,
            iterations = site.Iterations,
            expectation = site.Expectation,
            cacheHits = snapshot.CacheHits,
            cacheMisses = snapshot.CacheMisses,
            hitRate = lookups == 0 ? 0d : (double)snapshot.CacheHits / lookups,
            storeCacheHits = snapshot.StoreCacheHits,
            storeCacheMisses = snapshot.StoreCacheMisses,
            storeHitRate = stores == 0 ? 0d : (double)snapshot.StoreCacheHits / stores,
            dictionaryFallbacks = snapshot.DictionaryFallbacks,
            prototypeInvalidations = snapshot.PrototypeInvalidations,
            shapeTransitions = snapshot.ShapeTransitions,
            polymorphicPromotions = snapshot.PolymorphicPromotions,
            megamorphicSites = snapshot.MegamorphicSites,
            storeMegamorphicSites = snapshot.StoreMegamorphicSites,
        };
    }
}
