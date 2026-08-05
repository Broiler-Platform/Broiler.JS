using System;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.BuiltIns.String;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Engine.Benchmarks;

/// <summary>
/// Prices one generic binary operation against the guarded native form a specialization would
/// emit, so item 4-2's arithmetic half can be decided on a throughput number rather than on the
/// allocation argument that currently refuses it (docs/performance-roadmap.md item 4-2).
/// </summary>
/// <remarks>
/// <para>
/// <b>The population this prices is named and small: item 3-1 refuses these sites, and it refuses
/// them for a reason that is about boxes.</b> Its <c>NoSavingToMake</c> condition asks whether the
/// guarded form removes an intermediate or an already-native leaf, and a single-node tree over two
/// unprovable leaves — <c>a.x * b.y</c> — removes neither: the generic operator mints one box and
/// so does the guarded form. That argument is correct about allocation and says nothing about
/// time, and it is <b>26.22% of the corpus's candidate nodes</b>, the whole of 3-1's compile-time
/// residue. What it costs has never been measured.
/// </para>
/// <para>
/// <b>Three arms, because the guard has a losing side and the whole question is how the two
/// compare.</b> <c>generic</c> is what such a site executes today. <c>guarded-hit</c> is the
/// specialized form on operands that satisfy it. <c>guarded-miss</c> is the same form on operands
/// that do not, which is the cost a wrong speculation pays — and the widened census says that is
/// not a corner case: the both-Numbers share runs from <b>0.46% on Splay</b> to 100% on four
/// suites, so an unconditional static widening would be paying the miss on millions of operations.
/// </para>
/// <para>
/// <b>Comparison is measured beside arithmetic because nothing specializes it at all.</b> 3-1's
/// speculation is gated on <c>IsNativeNumericOperator</c>, which is the six arithmetic operators
/// and the bitwise ones; <c>&lt;</c> and <c>&gt;</c> are excluded, and item 3-5 only helps when one
/// side is <em>already</em> an unboxed double. A comparison over two boxed Numbers is therefore
/// reached by no fast path in the engine, and unlike arithmetic it allocates nothing either way —
/// so its whole cost is the virtual call and the coercions, which is exactly what a guard removes.
/// </para>
/// <para>
/// All arms run round-robin in one process on one build, reversed on alternate rounds, with each
/// arm's own spread reported beside its median — §3.5's rule, because a ratio between two arms
/// whose spread exceeds the effect is not a measurement.
/// </para>
/// </remarks>
internal static class GenericArithmeticCostMetrics
{
    private const int Iterations = 1_000_000;
    private const int Rounds = 12;
    private const int WarmUpPasses = 3;

    private sealed record Arm(
        string Name,
        string Note,
        double NanosecondsPerOperation,
        double BytesPerOperation,
        double SpreadPercent);

    internal static void Write()
    {
        // The operands a NoSavingToMake site is handed: two boxed Numbers, neither provable at
        // compile time. Deliberately outside the small-integer cache, so the box the arms mint is
        // a real allocation rather than a table hit — that is what the corpus's 75.8 M allocated
        // boxes are made of.
        var left = JSNumber.Create(1_234.5);
        var right = JSNumber.Create(6_789.25);

        // The guard's losing side. A String left operand is the commonest way a `+` site sees
        // something other than two Numbers, and it is what Splay and PdfJS are full of.
        var stringLeft = JSValue.CreateString("1234.5");

        var plan = new[]
        {
            new Plan("multiply-generic", left, right, Kind.Generic, Op.Multiply,
                "what a NoSavingToMake site executes today: the virtual JSValue operator, which "
                    + "coerces both operands through ToNumericPrimitive before multiplying"),
            new Plan("multiply-guarded-hit", left, right, Kind.Guarded, Op.Multiply,
                "the specialized form when the guard holds: two type tests, then the native "
                    + "multiply. It mints the SAME box, which is why 3-1 refuses the site"),
            new Plan("multiply-guarded-miss", stringLeft, right, Kind.Guarded, Op.Multiply,
                "the cost of speculating wrongly: the guard's two type tests, then the generic "
                    + "operator anyway"),

            new Plan("add-generic", left, right, Kind.Generic, Op.Add,
                "the same for `+`, whose generic form has the extra ToDefaultPrimitive and "
                    + "string-concat branches"),
            new Plan("add-guarded-hit", left, right, Kind.Guarded, Op.Add, "as above, guard holding"),
            new Plan("add-guarded-miss", stringLeft, right, Kind.Guarded, Op.Add,
                "as above, guard failing — and for `+` the failure is a real answer (concatenation) "
                    + "rather than a coercion, so it is the expensive miss"),

            new Plan("less-generic", left, right, Kind.Generic, Op.Less,
                "a comparison over two boxed Numbers, which NO fast path in this engine reaches: "
                    + "3-1's speculation excludes relational operators and 3-5 needs one side "
                    + "already unboxed"),
            new Plan("less-guarded-hit", left, right, Kind.Guarded, Op.Less,
                "what a guard would make of it. Allocates nothing either way, so the whole "
                    + "difference is the virtual call and the coercions"),
            new Plan("less-guarded-miss", stringLeft, right, Kind.Guarded, Op.Less,
                "the failing guard on a comparison"),
        };

        var (arms, samples) = MeasureInterleaved(plan);

        // The pairing the question is actually about. Each ratio is computed WITHIN a round, so
        // whatever the machine was doing that round divides out — which is the whole reason the
        // arms are interleaved rather than run in blocks. A ratio of medians would not have that
        // property, and on these spreads it is the difference between an answer and a number.
        var comparisons = new[]
        {
            Compare("multiply", plan, samples, "multiply-generic", "multiply-guarded-hit"),
            Compare("multiply-when-wrong", plan, samples, "multiply-generic", "multiply-guarded-miss"),
            Compare("add", plan, samples, "add-generic", "add-guarded-hit"),
            Compare("add-when-wrong", plan, samples, "add-generic", "add-guarded-miss"),
            Compare("less", plan, samples, "less-generic", "less-guarded-hit"),
            Compare("less-when-wrong", plan, samples, "less-generic", "less-guarded-miss"),
        };

        Console.WriteLine(JsonSerializer.Serialize(
            new
            {
                metric = "generic-arithmetic-cost",
                iterations = Iterations,
                rounds = Rounds,
                note =
                    "Item 4-2's arithmetic half, priced. The population is item 3-1's NoSavingToMake "
                    + "refusal — a single-node arithmetic tree over two unprovable leaves, 26.22% of "
                    + "the corpus's candidate nodes — which 3-1 refuses because the guarded form "
                    + "removes no box. It does not, and this asks what it removes instead. Read the "
                    + "hit against the generic arm for the prize and the miss against it for the "
                    + "cost of being wrong; the widened census (arithmeticBothNumbers, 92.10% of "
                    + "26.2 M and 0.46%-100.00% per suite) says how often each applies.",
                arms,
                comparisons,
            },
            new JsonSerializerOptions { WriteIndented = true }));

        foreach (var arm in arms)
        {
            Console.Error.WriteLine(
                $"{arm.Name,-24} {arm.NanosecondsPerOperation,8:F2} ns  "
                + $"{arm.BytesPerOperation,6:F1} B  spread {arm.SpreadPercent,5:F1}%");
        }

        Console.Error.WriteLine();
        foreach (var c in comparisons)
        {
            Console.Error.WriteLine(
                $"{c.Name,-22} median pair ratio {c.MedianPairRatio,6:F3}x  "
                + $"favouring the guard {c.RoundsFavouringGuard,2}/{Rounds}  "
                + $"saving {c.NanosecondsSaved,7:F2} ns");
        }
    }

    private sealed record Comparison(
        string Name,
        string GenericArm,
        string GuardedArm,
        double MedianPairRatio,
        int RoundsFavouringGuard,
        double NanosecondsSaved);

    /// <summary>
    /// The guarded arm against its generic counterpart, ratioed <em>within</em> each round.
    /// </summary>
    private static Comparison Compare(
        string name,
        Plan[] plan,
        double[][] samples,
        string genericArm,
        string guardedArm)
    {
        var g = Array.FindIndex(plan, p => p.Name == genericArm);
        var s = Array.FindIndex(plan, p => p.Name == guardedArm);

        var ratios = new double[Rounds];
        var saved = new double[Rounds];
        var favouring = 0;
        for (var round = 0; round < Rounds; round++)
        {
            ratios[round] = samples[s][round] / samples[g][round];
            saved[round] = samples[g][round] - samples[s][round];
            if (ratios[round] < 1.0)
                favouring++;
        }

        Array.Sort(ratios);
        Array.Sort(saved);
        return new Comparison(name, genericArm, guardedArm, ratios[Rounds / 2], favouring, saved[Rounds / 2]);
    }

    private enum Kind
    {
        Generic,
        Guarded,
    }

    private enum Op
    {
        Multiply,
        Add,
        Less,
    }

    private sealed record Plan(string Name, JSValue Left, JSValue Right, Kind Kind, Op Op, string Note);

    /// <summary>
    /// Runs every arm once per round, forward on even rounds and reversed on odd ones, and takes
    /// each arm's median across rounds.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The first version of this ran each arm's samples consecutively and its generic arms came
    /// back with spreads of 161%, 76% and 470% against effects near 3x</b> — §3.5's rule about a
    /// control varying by more than the effect, produced by the instrument rather than found by
    /// it. Consecutive samples give an arm a private slice of the process's history: its own
    /// gen-0 debt, its own place in the tiered-JIT ramp, whatever the previous arm left on the
    /// heap. Round-robin gives every arm the same history.
    /// </para>
    /// <para>
    /// Reversing on alternate rounds is the ABBA the rest of this campaign uses, and it matters
    /// here for a specific reason: an arm that allocates 32 B an operation leaves a collection
    /// owed, and in a fixed order the SAME later arm pays it every time. Reversal makes that debt
    /// fall on both sides.
    /// </para>
    /// <para>
    /// A full blocking collection between samples, because the arms differ in allocation by
    /// design — the guarded-hit form mints the same box as the generic one, and the string miss
    /// mints eleven times as many — so leaving the collections where they fall would price an arm
    /// for its neighbour's garbage.
    /// </para>
    /// </remarks>
    private static (Arm[] Arms, double[][] Samples) MeasureInterleaved(Plan[] plan)
    {
        // The tiered-JIT ramp, paid before anything is timed. One pass was not enough: the loop
        // body is promoted on call count, and an arm still at tier-0 in its first timed sample is
        // most of where the old spread came from.
        for (var warmUp = 0; warmUp < WarmUpPasses; warmUp++)
        {
            foreach (var arm in plan)
                Run(arm.Left, arm.Right, arm.Kind, arm.Op);
        }

        var samples = new double[plan.Length][];
        var bytes = new double[plan.Length][];
        for (var i = 0; i < plan.Length; i++)
        {
            samples[i] = new double[Rounds];
            bytes[i] = new double[Rounds];
        }

        for (var round = 0; round < Rounds; round++)
        {
            for (var step = 0; step < plan.Length; step++)
            {
                var i = round % 2 == 0 ? step : plan.Length - 1 - step;

                GC.Collect(2, GCCollectionMode.Forced, blocking: true);
                GC.WaitForPendingFinalizers();

                var before = GC.GetAllocatedBytesForCurrentThread();
                var watch = Stopwatch.StartNew();
                Run(plan[i].Left, plan[i].Right, plan[i].Kind, plan[i].Op);
                watch.Stop();
                var after = GC.GetAllocatedBytesForCurrentThread();

                samples[i][round] = watch.Elapsed.TotalMilliseconds * 1_000_000.0 / Iterations;
                bytes[i][round] = (after - before) / (double)Iterations;
            }
        }

        var arms = new Arm[plan.Length];
        for (var i = 0; i < plan.Length; i++)
        {
            var ordered = samples[i].OrderBy(x => x).ToArray();
            var spread = ordered[0] > 0 ? (ordered[^1] - ordered[0]) * 100.0 / ordered[0] : 0;
            arms[i] = new Arm(
                plan[i].Name,
                plan[i].Note,
                ordered[Rounds / 2],
                bytes[i].OrderBy(x => x).ElementAt(Rounds / 2),
                spread);
        }

        return (arms, samples);
    }

    /// <summary>
    /// The loop under test. Both arms consume their result into an accumulator that is returned,
    /// so neither can be elided — a dead-store elimination in one arm and not the other is the
    /// way a two-arm microbenchmark most easily lies.
    /// </summary>
    private static double Run(JSValue left, JSValue right, Kind kind, Op op)
    {
        var accumulator = 0.0;
        for (var i = 0; i < Iterations; i++)
        {
            accumulator += op switch
            {
                Op.Multiply => kind == Kind.Generic
                    ? left.Multiply(right).DoubleValue
                    : GuardedMultiply(left, right).DoubleValue,
                Op.Add => kind == Kind.Generic
                    ? left.AddValue(right).DoubleValue
                    : GuardedAdd(left, right).DoubleValue,
                _ => (kind == Kind.Generic ? left.Less(right) : GuardedLess(left, right)) ? 1 : 0,
            };
        }

        return accumulator;
    }

    /// <summary>
    /// The shape a specialized emission would have: a type test per operand, the native operation
    /// when both hold, and the generic operator otherwise.
    /// </summary>
    /// <remarks>
    /// Written against <see cref="JSNumber"/> rather than against <c>IsNumber</c> because that is
    /// what the emitted guard can test cheaply — a type check the JIT compiles to one comparison,
    /// not a virtual property. Getting this wrong would price a guard nobody could emit.
    /// </remarks>
    private static JSValue GuardedMultiply(JSValue left, JSValue right)
        => left is JSNumber l && right is JSNumber r
            ? JSNumber.Create(l.DoubleValue * r.DoubleValue)
            : left.Multiply(right);

    private static JSValue GuardedAdd(JSValue left, JSValue right)
        => left is JSNumber l && right is JSNumber r
            ? JSNumber.Create(l.DoubleValue + r.DoubleValue)
            : left.AddValue(right);

    private static bool GuardedLess(JSValue left, JSValue right)
        => left is JSNumber l && right is JSNumber r
            ? l.DoubleValue < r.DoubleValue
            : left.Less(right);
}
