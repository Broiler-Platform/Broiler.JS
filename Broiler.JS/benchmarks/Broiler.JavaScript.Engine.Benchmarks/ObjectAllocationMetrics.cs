using System;
using System.Linq;
using System.Text.Json;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Engine.Benchmarks;

/// <summary>
/// Emits allocated bytes per ordinary object, by how the object was built and how many named
/// fields it carries, as JSON.
/// <para>
/// Exists to size roadmap item 2-3, which proposes removing the double storage every
/// shape-tracked object carries — its value in both <c>shapeSlots</c> and the
/// <c>PropertySequence</c>. That item was justified as "storing twice and paying to keep them
/// in sync", and the throughput half of that turned out to be unmeasurable: dropping the
/// PropertySequence write from the overwrite path moved a 20 M-iteration store loop by 0.7%,
/// inside noise. What remains is memory, and memory is what this measures — so the item can be
/// decided on a number instead of on the phrase.
/// </para>
/// <para>
/// Field values are small integer constants, which the per-thread small-integer cache serves
/// without allocating, so a delta between two rows is the cost of the object's own structure
/// rather than of the values put in it. The empty rows are the baseline the per-field cost is
/// measured against; the 8-field rows are there because the slot array grows by doubling from
/// four, so the step between 3 and 8 fields shows what that growth costs.
/// </para>
/// </summary>
internal static class ObjectAllocationMetrics
{
    private const int Objects = 50_000;

    private sealed record Site(string Name, string Body, string Note);

    private static readonly Site[] Sites =
    [
        new("literal-empty", "last = {};", "baseline: an object with no named properties"),
        new("literal-3", "last = { a: 1, b: 2, c: 3 };", "the cheapest three-field object the engine builds"),
        new("literal-8", "last = { a: 1, b: 2, c: 3, d: 4, e: 5, f: 6, g: 7, h: 8 };", "past the slot array's first growth step"),
        new("ctor-empty", "last = new E();", "baseline for the constructor form"),
        new("ctor-1", "last = new T1();", "one field: where the slot array is first allocated"),
        new("ctor-3", "last = new T3();", "item 2-3's subject, and the shape Richards and DeltaBlue build"),
        new("ctor-8", "last = new T8();", "past the slot array's first growth step"),
        new("class-3", "last = new C3();", "the class form of ctor-3"),
        new("create-null-3", "last = Object.create(null); last.a = 1; last.b = 2; last.c = 3;", "no prototype, three assigned fields"),
    ];

    private const string Preamble = """
        function E() {}
        function T1() { this.a = 1; }
        function T3() { this.a = 1; this.b = 2; this.c = 3; }
        function T8() { this.a = 1; this.b = 2; this.c = 3; this.d = 4; this.e = 5; this.f = 6; this.g = 7; this.h = 8; }
        class C3 { constructor() { this.a = 1; this.b = 2; this.c = 3; } }
        """;

    internal static void Write()
    {
        var rows = Sites.Select(Measure).ToArray();

        Console.WriteLine(JsonSerializer.Serialize(
            new
            {
                schema = "broiler.object-allocation-metrics/1",
                objectsPerSite = Objects,
                note = "Bytes per object, warmed then measured after a forced gen2 collection. "
                    + "See docs/performance-roadmap.md item 2-3.",
                sites = rows,
            },
            new JsonSerializerOptions { WriteIndented = true }));
    }

    private static object Measure(Site site)
    {
        using var context = BenchmarkContext.Create();

        var source = $"(function () {{\n{Preamble}\nvar last = null;\nfor (var i = 0; i < {Objects}; i++) {{ {site.Body} }}\nreturn last;\n}})";
        var function = context.Eval(source, $"{site.Name}.js");
        var arguments = new Arguments(JSUndefined.Value);

        // Warmed first, so the measured run pays for objects and nothing else: the compile, the
        // key strings, the shapes and the inline-cache entries are all one-time and would
        // otherwise land in the delta.
        function.InvokeFunction(in arguments);

        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);

        var before = GC.GetAllocatedBytesForCurrentThread();
        function.InvokeFunction(in arguments);
        var total = GC.GetAllocatedBytesForCurrentThread() - before;

        return new
        {
            site = site.Name,
            note = site.Note,
            totalBytes = total,
            bytesPerObject = Math.Round((double)total / Objects, 1),
        };
    }
}
