using System;
using System.Diagnostics;
using System.Text.Json;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Engine.Benchmarks;

/// <summary>
/// Emits what an ordinary function's Annex B <c>caller</c>/<c>arguments</c> deferred cells cost
/// it, by comparing a NON-STRICT function against a STRICT one that is identical in every other
/// respect.
/// </summary>
/// <remarks>
/// Exists to test the hypothesis roadmap item 2-9 records for its own losing side, and which it
/// is explicit is <em>not</em> settled: "every ordinary non-strict function carries the Annex B
/// caller/arguments as deferred cells from birth (P0-3), a deferred cell cannot be described
/// from a slot, and <c>TrackShapeKeyWithoutSlotValue</c> therefore materializes. So a function
/// does the shape work AND the trie rebuild where before it did the trie work alone."
/// <para>
/// A strict function is the control the hypothesis needs and never had.
/// <c>AddLegacyCallerAndArguments</c> runs for non-strict functions only, so if the deferred
/// cells are what forces materialization, a strict function must not pay it — and if strict and
/// non-strict cost the same, the mechanism is wrong no matter how plausible it reads. That is a
/// sharper instrument than the wall-clock ratio the item was measured with, because the byte
/// counts are deterministic and exact while the 1.23 was a ratio between two builds.
/// </para>
/// <para>
/// Both halves are reported with and without a first CALL, because the item's own decomposition
/// is that creation alone costs 1.03 and creation-plus-one-call costs 1.23 — "something on the
/// first-call path is carrying the rest and has not been identified". If the call's cost also
/// splits on strictness it belongs to the same mechanism; if it does not, it is a second thing
/// and the item needs to say so.
/// </para>
/// </remarks>
internal static class DeferredCellCostMetrics
{
    private const int Functions = 4_000;

    private sealed record Row(
        string Site,
        string Note,
        double BytesPerFunction,
        double NanosecondsPerFunction,
        double MaterializationsPerFunction,
        long TotalBytes,
        double TotalMilliseconds,
        long TotalMaterializations);

    internal static void Write()
    {
        var rows = new[]
        {
            Measure("nonstrict-create", false, false,
                "an ordinary function, created and never called — it carries the Annex B "
                    + "caller/arguments deferred cells from birth"),
            Measure("strict-create", true, false,
                "the CONTROL: identical but strict, so AddLegacyCallerAndArguments never runs "
                    + "and there are no deferred cells to describe"),
            Measure("nonstrict-create-and-call", false, true,
                "the shape the item measured at 1.23 — define many, call each once"),
            Measure("strict-create-and-call", true, true,
                "the same, strict. If the first-call cost is the deferred cells, this row does "
                    + "not pay it; if it pays anyway, the rest of the 1.23 is something else"),
        };

        Console.WriteLine(JsonSerializer.Serialize(
            new
            {
                schema = "broiler.deferred-cell-cost/1",
                functions = Functions,
                note = "Allocated bytes and nanoseconds per FUNCTION. Strict is the control for "
                    + "non-strict: the only difference between the two halves is whether the "
                    + "function gets Annex B caller/arguments as deferred cells, which is what "
                    + "roadmap item 2-9's losing-side hypothesis turns on.",
                rows,
            },
            new JsonSerializerOptions { WriteIndented = true }));
    }

    private static Row Measure(string name, bool strict, bool call, string note)
    {
        using var context = BenchmarkContext.Create();

        // Each function is given a distinct body so nothing can be shared or cached between
        // them, and a trivially small one so the measurement is about the function OBJECT
        // rather than about compiling a body.
        var prologue = strict ? "'use strict'; " : string.Empty;
        var body = call
            ? $"var f = new Function('a', \"{prologue}return a + \" + i + \";\"); sink += f(1);"
            : $"var f = new Function('a', \"{prologue}return a + \" + i + \";\"); sink += f ? 1 : 0;";

        var source = $$"""
            (function (n) {
                var sink = 0;
                for (var i = 0; i < n; i++) { {{body}} }
                return sink;
            })
            """;

        var function = context.Eval(source, $"{name}.js");
        var warm = new Arguments(JSUndefined.Value, new JSNumber(50));
        function.InvokeFunction(in warm);

        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);

        var arguments = new Arguments(JSUndefined.Value, new JSNumber(Functions));
        PropertyOptimizationDiagnostics.Reset();
        using var recording = PropertyOptimizationDiagnostics.Enable();
        var before = GC.GetAllocatedBytesForCurrentThread();
        var stopwatch = Stopwatch.StartNew();
        function.InvokeFunction(in arguments);
        stopwatch.Stop();
        var bytes = GC.GetAllocatedBytesForCurrentThread() - before;
        var materializations = PropertyOptimizationDiagnostics.Snapshot().NamedPropertiesMaterializations;

        return new Row(
            name,
            note,
            Math.Round((double)bytes / Functions, 1),
            Math.Round(stopwatch.Elapsed.TotalMilliseconds * 1_000_000 / Functions, 1),
            Math.Round((double)materializations / Functions, 2),
            bytes,
            Math.Round(stopwatch.Elapsed.TotalMilliseconds, 1),
            materializations);
    }
}
