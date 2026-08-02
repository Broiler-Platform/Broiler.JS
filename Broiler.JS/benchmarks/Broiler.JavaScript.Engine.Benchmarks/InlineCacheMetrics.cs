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

        // Item 2-1's premise, as a measurement rather than an assertion. Each iteration
        // builds a fresh three-field object, so every one of the 600 000 stores CREATES the
        // property it writes. The store cache only describes overwriting a slot that already
        // exists, so the shape it records after the write never matches the shape the next
        // object presents before it: expect ~0 hits and 3 misses per iteration, forever.
        new(
            "constructor-field-creation",
            "(function () { function T(a, b, c) { this.a = a; this.b = b; this.c = c; } var last = null; for (var i = 0; i < 200000; i++) { last = new T(i, i + 1, i + 2); } return last.c; })",
            600_000,
            "item 2-1: store-cache hits ~= 0, misses ~= 3x iterations - a property-creating store can never hit"),

        // The control for the row above. Same three fields created per iteration, same count
        // of property-creating stores, but built by a literal instead of a constructor. If the
        // prototype-invalidation counts differ, what advances the prototype version is `new`,
        // not property creation.
        new(
            "literal-field-creation",
            "(function () { var last = null; for (var i = 0; i < 200000; i++) { last = { a: i, b: i + 1, c: i + 2 }; } return last.c; })",
            200_000,
            "control for constructor-field-creation: same creations, no `new`"),

        // The consequence, if `new` is what invalidates: a warm inherited-method site should
        // go cold as soon as the same loop also allocates. inherited-method-call above hoists
        // the allocation out of the loop and reports ~400 000 hits; this one does not.
        new(
            "inherited-method-call-while-allocating",
            "(function () { function P(v) { this.v = v; } P.prototype.get = function () { return this.v; }; var p = new P(1); var s = 0; var last = null; for (var i = 0; i < 200000; i++) { s = s + p.get(); last = new P(i); } return s; })",
            200_000,
            "compare with inherited-method-call: identical read site, allocation added to the loop"),

        // Item 2-2's premise. Shape eligibility is GetType() == typeof(JSObject), so a JSArray,
        // a JSFunction and every built-in exotic are excluded wholesale — no shape, so no
        // inline-cache entry, so every named property access on one resolves generically. These
        // six sites say which of those accesses a hot loop actually performs, because the item
        // is worth doing only for the ones that do.
        new(
            "array-length-read",
            "(function () { var a = [1, 2, 3]; var s = 0; for (var i = 0; i < 200000; i++) { s = s + a.length; } return s; })",
            200_000,
            "item 2-2: `a.length` in a loop condition, the array access a hot loop really makes"),

        new(
            "array-named-read",
            "(function () { var a = []; a.tag = 7; var s = 0; for (var i = 0; i < 200000; i++) { s = s + a.tag; } return s; })",
            200_000,
            "item 2-2: an expando named property on an array"),

        new(
            "array-named-store",
            "(function () { var a = []; a.tag = 0; for (var i = 0; i < 200000; i++) { a.tag = i; } return a.tag; })",
            200_000,
            "item 2-2: the write side of the same"),

        new(
            "array-element-read",
            "(function () { var a = [1, 2, 3]; var s = 0; for (var i = 0; i < 200000; i++) { s = s + a[1]; } return s; })",
            200_000,
            "control: an ELEMENT is not a named property and is never shape-tracked by design"),

        new(
            "function-named-read",
            "(function () { function f() {} f.tag = 7; var s = 0; for (var i = 0; i < 200000; i++) { s = s + f.tag; } return s; })",
            200_000,
            "item 2-2: a named property on a JSFunction"),

        // DeltaBlue's `Strength.stronger(...)` and `Strength.REQUIRED` are named reads on a
        // FUNCTION object, in the hot path of the worst throughput score in the suite. Sloppy
        // and strict are separate rows because an ordinary non-strict function carries two
        // deferred own properties from birth — the Annex B `caller` and `arguments` (P0-3) — and
        // a deferred cell is not shape-trackable.
        new(
            "sloppy-function-static-read",
            "(function () { function S() {} S.REQUIRED = 7; var s = 0; for (var i = 0; i < 200000; i++) { s = s + S.REQUIRED; } return s; })",
            200_000,
            "item 2-2: DeltaBlue's shape exactly; sloppy, so the function has legacy caller/arguments"),

        new(
            "strict-function-static-read",
            "(function () { 'use strict'; function S() {} S.REQUIRED = 7; var s = 0; for (var i = 0; i < 200000; i++) { s = s + S.REQUIRED; } return s; })",
            200_000,
            "item 2-2: the same read on a function with no legacy caller/arguments"),

        new(
            "class-static-read",
            "(function () { class S { static REQUIRED = 7; } var s = 0; for (var i = 0; i < 200000; i++) { s = s + S.REQUIRED; } return s; })",
            200_000,
            "item 2-2: a class static, which is always strict"),

        // Item 2-4's premise. The store cache was installed for a constant-key ASSIGNMENT only;
        // compound assignment, increment, computed keys and optional chains all kept an older
        // lowering that reached neither cache - measured 0 hits AND 0 misses, against
        // `monomorphic-store` above at 199 999. The first two now take both caches and read
        // 199 999 / 199 999; the last two stay at 0 / 0 on purpose, and these rows are what
        // would notice if either fact stopped being true.
        new(
            "compound-assign-store",
            "(function () { var o = { x: 0 }; for (var i = 0; i < 200000; i++) { o.x += 1; } return o.x; })",
            200_000,
            "item 2-4: `o.x += 1` - one read and one write, both on a constant key"),

        new(
            "increment-store",
            "(function () { var o = { x: 0 }; for (var i = 0; i < 200000; i++) { o.x++; } return o.x; })",
            200_000,
            "item 2-4: `o.x++`, which the item calls the most expensive of the group"),

        new(
            "computed-key-read",
            "(function () { var o = { x: 1 }; var k = 'x'; var s = 0; for (var i = 0; i < 200000; i++) { s = s + o[k]; } return s; })",
            200_000,
            "item 2-4: a computed key is not a constant key, so no site is allocated for it"),

        new(
            "optional-chain-read",
            "(function () { var o = { x: 1 }; var s = 0; for (var i = 0; i < 200000; i++) { s = s + o?.x; } return s; })",
            200_000,
            "item 2-4: `o?.x` - a constant key reached through a different lowering"),

        new(
            "typed-array-length-read",
            "(function () { var a = new Float64Array(4); var s = 0; for (var i = 0; i < 200000; i++) { s = s + a.length; } return s; })",
            200_000,
            "item 2-2: the same question for a typed array, which Crypto/NavierStokes/zlib use"),
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
