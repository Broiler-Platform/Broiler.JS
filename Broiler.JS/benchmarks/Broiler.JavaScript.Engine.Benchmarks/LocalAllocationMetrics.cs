using System;
using System.Linq;
using System.Text.Json;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.Compiler;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Engine.Benchmarks;

/// <summary>
/// Emits allocated bytes per iteration for each place a JavaScript number can live in a
/// function, together with the compiler's own count of how many locals it managed to keep in a
/// raw CLR <c>double</c>, as JSON.
/// </summary>
/// <remarks>
/// Exists to size roadmap item 3-3, which proposes widening the unboxed-locals eligibility gate
/// and asserts an ordering while doing so: "<em>Parameters are the valuable one — every numeric
/// helper takes them, and every Octane benchmark is full of numeric helpers. Do parameters
/// first.</em>" That is a claim about where the bytes are, and it has never been measured. The
/// item names three ineligible categories — parameters, <c>let</c>/<c>const</c>, and a
/// <c>var</c> declared inside a block — so all three are measured against the one category that
/// <em>is</em> eligible today, and the ordering comes out of the numbers rather than out of the
/// sentence.
/// <para>
/// Two instruments, because they answer different questions and only one of them is exact.
/// <see cref="CompilerSpecializationDiagnostics"/> reports how many locals the gate admitted,
/// which is deterministic and settles <em>whether a shape is eligible at all</em>; the
/// allocation delta settles <em>what that eligibility is worth</em>. A site reporting zero
/// numeric locals and the same bytes as an eligible one is an item with no prize, and the pair
/// is the only way to tell that from a site the probe failed to reach (§3.5, "check how much of
/// the probe the change can reach").
/// </para>
/// <para>
/// The helper-call sites are the item's own claim written as a measurement. They are three
/// spellings of one call, differing only in where the operand lives: read from a parameter,
/// read from an eligible numeric <c>var</c> while a parameter is still bound, and read from an
/// eligible numeric <c>var</c> with no parameter declared at all. The argument is passed in all
/// three, so the differences decompose the parameter's cost into the binding and the reads
/// without either being confounded by argument passing. §4.3's B2 says a call costs ~250-300 ns
/// against a loop body of ~20 ns, so a helper is mostly call — these rows are what says how much
/// of the remainder 3-3 can reach.
/// </para>
/// </remarks>
internal static class LocalAllocationMetrics
{
    private const int Iterations = 200_000;

    /// <summary>
    /// One shape. <paramref name="Source"/> is a function expression taking one argument and
    /// returning a number, so every site is invoked identically.
    /// </summary>
    private sealed record Site(string Name, string Category, string Source, string Note);

    /// <summary>
    /// The loop with the value under test removed, so a row can be reported net of the loop
    /// that carries it.
    /// </summary>
    /// <remarks>
    /// The control's accumulator and counter are themselves eligible numeric locals, so it
    /// measures the floor this whole family is aiming at rather than an arbitrary baseline: a
    /// site that reaches 0.00 B net has put its value where the control already keeps its own.
    /// </remarks>
    private static readonly Site LoopControl = new(
        "loop-control",
        "control",
        $$"""
        (function (arg) {
            var s = 0;
            for (var i = 0; i < {{Iterations}}; i++) { s = s + 3.5 * 2; }
            return s;
        })
        """,
        "the same loop with a literal in place of the value under test; subtract to get the "
            + "value's own cost");

    private static readonly Site[] Sites =
    [
        new(
            "top-level-var",
            "in-loop",
            $$"""
            (function (arg) {
                var v = 3.5;
                var s = 0;
                for (var i = 0; i < {{Iterations}}; i++) { s = s + v * 2; }
                return s;
            })
            """,
            "the ONLY eligible category today: a function-top-level var no closure names. This "
                + "is the floor the other three are measured against, not a target"),

        new(
            "parameter",
            "in-loop",
            $$"""
            (function (v) {
                var s = 0;
                for (var i = 0; i < {{Iterations}}; i++) { s = s + v * 2; }
                return s;
            })
            """,
            "item 3-3's headline category. Same loop, same arithmetic, same accumulator — the "
                + "only difference from top-level-var is that the value arrives as an argument"),

        new(
            "let-binding",
            "in-loop",
            $$"""
            (function (arg) {
                let v = 3.5;
                var s = 0;
                for (var i = 0; i < {{Iterations}}; i++) { s = s + v * 2; }
                return s;
            })
            """,
            "3-3's second category. Identical to top-level-var but for the declaration keyword, "
                + "so the delta is exactly what the missing TDZ analysis costs"),

        new(
            "const-binding",
            "in-loop",
            $$"""
            (function (arg) {
                const v = 3.5;
                var s = 0;
                for (var i = 0; i < {{Iterations}}; i++) { s = s + v * 2; }
                return s;
            })
            """,
            "a const can never be reassigned, so it is the easiest half of the let/const "
                + "category and worth separating from let"),

        new(
            "block-var",
            "in-loop",
            $$"""
            (function (arg) {
                var s = 0;
                {
                    var v = 3.5;
                }
                for (var i = 0; i < {{Iterations}}; i++) { s = s + v * 2; }
                return s;
            })
            """,
            "3-3's third category: a var hoisted to the function but declared inside a block, "
                + "so reaching the loop without running the initializer is what has to be ruled "
                + "out"),

        // ── item 3-7: the same value, captured ────────────────────────────────────────────
        //
        // Four spellings of top-level-var with a closure added, differing only in HOW the
        // closure names the value. They are the item's whole surface: the first says what
        // capture costs today, the second what the soundness condition costs, and the last two
        // are the losing side — a raw double is cheaper to write and dearer to read through a
        // closure, and only a measurement says which way a given shape falls.

        new(
            "captured-var",
            "capture",
            $$"""
            (function (arg) {
                var v = 3.5;
                var s = 0;
                var h = function () { return v; };
                for (var i = 0; i < {{Iterations}}; i++) { s = s + v * 2; }
                return s;
            })
            """,
            "top-level-var with one closure naming the value and never called. Identical "
                + "arithmetic, so the delta from top-level-var is exactly what capture costs"),

        new(
            "captured-var-hoisted-fn",
            "capture",
            $$"""
            (function (arg) {
                var v = 3.5;
                var s = 0;
                for (var i = 0; i < {{Iterations}}; i++) { s = s + v * 2; }
                function h() { return v; }
                return s;
            })
            """,
            "the same closure written as a function DECLARATION. Its object exists from function "
                + "entry, so it can read the binding before the initializer runs and the name has "
                + "to keep its cell — this row is what that correctness condition costs"),

        new(
            "captured-var-read-in-closure",
            "capture",
            $$"""
            (function (arg) {
                var v = 3.5;
                var s = 0;
                var h = function () { return v; };
                for (var i = 0; i < {{Iterations}}; i++) { s = s + h(); }
                return s;
            })
            """,
            "the losing side: the value is read THROUGH the closure every iteration. A "
                + "JSVariable hands back the JSValue it already holds; a raw double has to box "
                + "one. If capture is worth widening this row is what pays for it"),

        new(
            "captured-var-written-in-closure",
            "capture",
            $$"""
            (function (arg) {
                var v = 3.5;
                var s = 0;
                var h = function () { v = v + 1; };
                for (var i = 0; i < {{Iterations}}; i++) { h(); s = s + v * 2; }
                return s;
            })
            """,
            "the winning side of the same pair: the value is WRITTEN through the closure every "
                + "iteration, where a JSVariable boxes the result and a raw double stores it"),

        new(
            "call-parameter-read",
            "call",
            $$"""
            (function (arg) {
                function h(a) { return a * 2 + 1; }
                var s = 0;
                for (var i = 0; i < {{Iterations}}; i++) { s = s + h(3.5); }
                return s;
            })
            """,
            "the numeric helper 3-3 is justified by. Binding AND reading a parameter"),

        new(
            "call-parameter-bound-only",
            "call",
            $$"""
            (function (arg) {
                function h(a) { var b = 3.5; return b * 2 + 1; }
                var s = 0;
                for (var i = 0; i < {{Iterations}}; i++) { s = s + h(3.5); }
                return s;
            })
            """,
            "the same call with the same argument, but the arithmetic reads an ELIGIBLE numeric "
                + "var instead of the parameter. The difference from call-parameter-read is what "
                + "reading a parameter costs; what remains over call-no-parameter is what "
                + "binding one costs"),

        new(
            "call-parameter-returned",
            "call",
            $$"""
            (function (arg) {
                function h(a) { return a; }
                var s = 0;
                for (var i = 0; i < {{Iterations}}; i++) { s = s + h(3.5); }
                return s;
            })
            """,
            "binds and READS a parameter but performs no arithmetic on it, so the value handed "
                + "back is the very object the caller passed. Whatever this costs over "
                + "call-no-parameter is the BINDING; whatever call-parameter-read costs over "
                + "this is the arithmetic. The two want different fixes, and only one of them is "
                + "statically sound"),

        new(
            "call-parameter-read-twice",
            "call",
            $$"""
            (function (arg) {
                function h(a) { return a * 2 + a * 3 + 1; }
                var s = 0;
                for (var i = 0; i < {{Iterations}}; i++) { s = s + h(3.5); }
                return s;
            })
            """,
            "the discriminator. One binding, TWO reads: if a parameter's cost is its cell this "
                + "matches call-parameter-read, and if it is charged per read this is one box "
                + "dearer. The two diagnoses want opposite fixes — a cell is removable by static "
                + "analysis alone, per-read boxing needs a numeric proof the caller can defeat"),

        new(
            "call-numeric-var-read-twice",
            "call",
            $$"""
            (function (arg) {
                function h(a) { var b = 3.5; return b * 2 + b * 3 + 1; }
                var s = 0;
                for (var i = 0; i < {{Iterations}}; i++) { s = s + h(3.5); }
                return s;
            })
            """,
            "the same second read on an ELIGIBLE numeric var, so the pair above is read against "
                + "a control that provably cannot move"),

        new(
            "call-one-parameter-thrice",
            "call",
            $$"""
            (function (arg) {
                function h(a) { return a + a + a; }
                var s = 0;
                for (var i = 0; i < {{Iterations}}; i++) { s = s + h(3.5, 3.5, 3.5); }
                return s;
            })
            """,
            "one parameter read three times, the control for the row below. It passes three "
                + "arguments and declares one, so the CALL SITE is identical to that row and "
                + "only the binding count differs — the first version of this pair passed one "
                + "argument here and three below, which measured the arguments and not the "
                + "bindings"),

        new(
            "call-three-parameters",
            "call",
            $$"""
            (function (arg) {
                function h(a, c, d) { return a + c + d; }
                var s = 0;
                for (var i = 0; i < {{Iterations}}; i++) { s = s + h(3.5, 3.5, 3.5); }
                return s;
            })
            """,
            "three parameters read once each: the same three reads and the same arithmetic as "
                + "the row above, differing only in how many bindings carry them. The delta is "
                + "the per-parameter cost, and it is what says whether a parameter's price is "
                + "paid once per call or once per binding"),

        new(
            "call-captured-var",
            "call",
            $$"""
            (function (arg) {
                function h() { var b = 3.5; var g = function () { return b; }; return b * 2 + 1; }
                var s = 0;
                for (var i = 0; i < {{Iterations}}; i++) { s = s + h(3.5); }
                return s;
            })
            """,
            "call-no-parameter with a closure naming the helper's local. Measured per CALL rather "
                + "than per loop iteration, so it prices what a captured binding costs to CREATE: "
                + "a captured JSVariable is two allocations, the cell and the box a closure reads "
                + "it through, where a captured raw double is the box alone"),

        new(
            "call-no-parameter",
            "call",
            $$"""
            (function (arg) {
                function h() { var b = 3.5; return b * 2 + 1; }
                var s = 0;
                for (var i = 0; i < {{Iterations}}; i++) { s = s + h(3.5); }
                return s;
            })
            """,
            "the same call site passing the same argument to a function that declares no "
                + "parameter at all: the floor for a helper, and the row that says how much of a "
                + "call 3-3 could ever reach"),
    ];

    internal static void Write()
    {
        var control = Measure(LoopControl);
        var rows = Sites.Select(site => Measure(site, control)).ToArray();

        Console.WriteLine(JsonSerializer.Serialize(
            new
            {
                schema = "broiler.local-allocation-metrics/1",
                iterationsPerSite = Iterations,
                note = "Bytes per iteration, warmed then measured after a forced gen2 "
                    + "collection, reported net of a loop control that carries no value under "
                    + "test. numericLocals is the compiler's own count for that site and is "
                    + "exact: it says whether the eligibility gate admitted the binding at all. "
                    + "See docs/performance-roadmap.md item 3-3.",
                loopControl = control,
                sites = rows,
            },
            new JsonSerializerOptions { WriteIndented = true }));
    }

    private static Row Measure(Site site, Row control = null)
    {
        using var context = BenchmarkContext.Create();

        // Compilation happens inside Eval, so the counters are read around it and nowhere else.
        // They are process-wide statics; every site gets a fresh context and its own reset.
        CompilerSpecializationDiagnostics.Reset();
        var function = context.Eval(site.Source, $"{site.Name}.js");
        var specialization = CompilerSpecializationDiagnostics.Snapshot();

        var arguments = new Arguments(JSUndefined.Value, new JSNumber(3.5));

        // Warmed first, so the measured run pays for the loop and nothing else — the inline-cache
        // entries and any first-call work are one-time and would otherwise land in the delta.
        function.InvokeFunction(in arguments);

        var bytes = Sample(() => function.InvokeFunction(in arguments));
        var perIteration = Math.Round((double)bytes / Iterations, 2);

        return new Row(
            site.Name,
            site.Category,
            site.Note,
            perIteration,
            control == null ? null : Math.Round(perIteration - control.BytesPerIteration, 2),
            bytes,
            specialization.NumericLocals,
            specialization.ScalarLocals);
    }

    /// <summary>
    /// One measured shape. <paramref name="NumericLocals"/> is exact and
    /// <paramref name="NetBytesPerIteration"/> is what item 3-3 is about.
    /// </summary>
    internal sealed record Row(
        string Site,
        string Category,
        string Note,
        double BytesPerIteration,
        double? NetBytesPerIteration,
        long TotalBytes,
        long NumericLocals,
        long ScalarLocals);

    private static long Sample(Action body)
    {
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);

        var before = GC.GetAllocatedBytesForCurrentThread();
        body();
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }
}
