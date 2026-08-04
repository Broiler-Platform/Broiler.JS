using System;
using System.Collections.Generic;
using System.Reflection;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.ExpressionCompiler.Runtime;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Compiler.Tests;

// The in-method bailout (docs/performance-roadmap.md item 4-3b): a speculative fast path and its
// generic fallback compiled into ONE method, so a failed guard is a branch rather than a restart.
//
// The property that matters is the one restart cannot give: because nothing is re-entered,
// effects performed BEFORE the guard are not repeated. The phase's own stated verification is
// "a test that forces every guard to fail at every point in a function body and asserts the
// generic path produces the unspecialized answer WITH THE SAME OBSERVABLE EFFECT SEQUENCE", and
// that is what this file does — the bodies are built as expression trees and compiled through
// the engine's own IL generator, because the facility is codegen and testing it through
// JavaScript would test whatever chose to emit it instead.
//
// Runs single-threaded and resets Speculation between cases, since the poison table is
// process-wide.
[Collection(Phase3DiagnosticsCollection.Name)]
public sealed class InMethodFallbackTests
{
    // ── the effect log the assertions are made against ───────────────────────────────

    private static readonly List<string> Log = [];
    private static bool guardHolds;

    public static int Effect(string tag, int value)
    {
        Log.Add(tag);
        return value;
    }

    // A per-invocation schedule, so a test can fail the k-th guard of a body and let the rest
    // hold. Null means "use the plain flag".
    private static Func<bool> guardScheduler;

    public static bool GuardCondition(int subject)
    {
        Log.Add("guard");
        return guardScheduler != null ? guardScheduler() : guardHolds;
    }

    private static readonly MethodInfo EffectMethod =
        typeof(InMethodFallbackTests).GetMethod(nameof(Effect), [typeof(string), typeof(int)]);

    private static readonly MethodInfo GuardMethod =
        typeof(InMethodFallbackTests).GetMethod(nameof(GuardCondition), [typeof(int)]);

    private static BExpression Effects(string tag, BExpression value)
        => BExpression.Call(null, EffectMethod, BExpression.Constant(tag), value);

    /// <summary>
    /// Builds <c>before → [guarded op]×<paramref name="operations"/> → after</c> as one method
    /// and compiles it. When <paramref name="speculate"/> is false the guarded operations are
    /// replaced by their generic form alone, which is the control every speculated run is
    /// compared against.
    /// </summary>
    private static Func<int, int> Build(int operations, bool speculate, out int[] sites)
    {
        var parameter = BExpression.Parameter(typeof(int), "p");
        var accumulator = BExpression.Parameter(typeof(int), "acc");
        var body = new Sequence<BExpression>
        {
            BExpression.Assign(accumulator, Effects("before", parameter)),
        };

        var allocated = new int[operations];
        for (var i = 0; i < operations; i++)
        {
            var index = i;
            // The subject carries an effect of its own, which is what makes "evaluated exactly
            // once" checkable: hand-rolling the conditional would run it in the guard and again
            // in whichever arm was taken, and the log would show it twice.
            BExpression Subject() => Effects($"subject{index}", accumulator);

            if (!speculate)
            {
                allocated[i] = -1;
                body.Add(BExpression.Assign(accumulator, Effects($"slow{index}", Subject())));
                continue;
            }

            var site = Speculation.Allocate();
            allocated[i] = site;
            body.Add(BExpression.Assign(accumulator, SpeculationBuilder.Guarded(
                site,
                Subject(),
                s => BExpression.Call(null, GuardMethod, s),
                s => Effects($"fast{index}", s),
                s => Effects($"slow{index}", s),
                typeof(int))));
        }

        body.Add(Effects("after", accumulator));

        sites = allocated;
        var lambda = BExpression.Lambda(
            typeof(Func<int, int>),
            BExpression.Block(new Sequence<BParameterExpression> { accumulator }, body),
            new FunctionName("inMethodFallback"),
            [parameter]);

        return (Func<int, int>)lambda.Compile();
    }

    private static (int Result, string[] Effects) Run(Func<int, int> compiled, int argument)
    {
        Log.Clear();
        var result = compiled(argument);
        return (result, Log.ToArray());
    }

    // ── the phase's stated verification ──────────────────────────────────────────────

    // Every guard failing, at every position in the body, must produce exactly the unspeculated
    // answer AND the unspeculated effect sequence — apart from the guard evaluations themselves,
    // which are the speculation's own cost and are not observable to the program.
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(5)]
    public void EveryGuardFailingReproducesTheUnspeculatedRunExactly(int operations)
    {
        Speculation.Reset();
        var control = Build(operations, speculate: false, out _);
        var speculated = Build(operations, speculate: true, out _);

        guardHolds = false;
        var expected = Run(control, 7);
        var actual = Run(speculated, 7);

        Assert.Equal(expected.Result, actual.Result);
        Assert.Equal(expected.Effects, StripGuards(actual.Effects));
    }

    // The same, one guard at a time: with N operations, fail only the k-th and let the rest
    // hold. This is the "at every point in the body" half — a bailout in the middle must leave
    // the effects already performed alone and must not re-run them.
    [Theory]
    [InlineData(3, 0)]
    [InlineData(3, 1)]
    [InlineData(3, 2)]
    [InlineData(5, 0)]
    [InlineData(5, 2)]
    [InlineData(5, 4)]
    public void FailingOneGuardMidBodyRepeatsNothingBeforeIt(int operations, int failing)
    {
        Speculation.Reset();
        var speculated = Build(operations, speculate: true, out _);

        // Hold every guard except the k-th of this invocation.
        Log.Clear();
        var localFailing = failing;
        var result = RunWithGuardSchedule(speculated, 7, i => i != localFailing);
        var effects = Log.ToArray();

        // "before" happens once, every subject happens exactly once, and exactly one operation
        // took the slow path — the one whose guard failed.
        Assert.Equal(1, Count(effects, "before"));
        for (var i = 0; i < operations; i++)
            Assert.Equal(1, Count(effects, $"subject{i}"));

        Assert.Equal(1, Count(effects, $"slow{failing}"));
        Assert.Equal(0, Count(effects, $"fast{failing}"));
        for (var i = 0; i < operations; i++)
        {
            if (i == failing)
                continue;
            Assert.Equal(1, Count(effects, $"fast{i}"));
            Assert.Equal(0, Count(effects, $"slow{i}"));
        }

        Assert.Equal(1, Count(effects, "after"));
        Assert.Equal(7, result);
    }

    // ── the evaluate-once contract, which is what a hand-rolled conditional gets wrong ──

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TheSubjectIsEvaluatedExactlyOnceWhicheverPathIsTaken(bool guardPasses)
    {
        Speculation.Reset();
        var speculated = Build(1, speculate: true, out _);

        guardHolds = guardPasses;
        var (_, effects) = Run(speculated, 3);

        Assert.Equal(1, Count(effects, "subject0"));
        Assert.Equal(1, Count(effects, "guard"));
        Assert.Equal(guardPasses ? 1 : 0, Count(effects, "fast0"));
        Assert.Equal(guardPasses ? 0 : 1, Count(effects, "slow0"));
    }

    // ── poisoning ────────────────────────────────────────────────────────────────────

    // A guard that keeps failing costs its own evaluation plus the generic path forever, which
    // is worse than never having speculated. After PoisonThreshold misses the site stops
    // evaluating the guard at all — visible as "guard" disappearing from the log while the
    // answer stays the same.
    [Fact]
    public void ASiteThatKeepsMissingStopsEvaluatingItsGuard()
    {
        Speculation.Reset();
        var speculated = Build(1, speculate: true, out var sites);

        guardHolds = false;
        for (var i = 0; i < Speculation.PoisonThreshold; i++)
        {
            var (_, duringWarmup) = Run(speculated, 5);
            Assert.Equal(1, Count(duringWarmup, "guard"));
        }

        Assert.True(Speculation.IsPoisoned(sites[0]), "the site should have poisoned by now");

        var (result, afterPoison) = Run(speculated, 5);
        Assert.Equal(0, Count(afterPoison, "guard"));
        Assert.Equal(1, Count(afterPoison, "slow0"));
        Assert.Equal(1, Count(afterPoison, "subject0"));
        Assert.Equal(5, result);

        // Misses stop being counted once the site is poisoned, so the figure stays a count of
        // real transitions rather than growing with every later execution.
        Assert.Equal(Speculation.PoisonThreshold, Speculation.Snapshot().GuardsMissed);
    }

    // A site that never misses is never poisoned and keeps taking the fast path.
    [Fact]
    public void ASiteWhoseGuardHoldsIsNeverPoisoned()
    {
        Speculation.Reset();
        var speculated = Build(1, speculate: true, out var sites);

        guardHolds = true;
        for (var i = 0; i < Speculation.PoisonThreshold * 3; i++)
        {
            var (_, effects) = Run(speculated, 4);
            Assert.Equal(1, Count(effects, "fast0"));
            Assert.Equal(0, Count(effects, "slow0"));
        }

        Assert.False(Speculation.IsPoisoned(sites[0]));
        Assert.Equal(0, Speculation.Snapshot().GuardsMissed);
    }

    // A refused site index (the table is bounded) must emit the generic form alone rather than
    // a guard against a site that does not exist.
    [Fact]
    public void ARefusedSiteEmitsTheGenericFormWithNoGuard()
    {
        Speculation.Reset();
        var parameter = BExpression.Parameter(typeof(int), "p");
        var lambda = BExpression.Lambda(
            typeof(Func<int, int>),
            SpeculationBuilder.Guarded(
                -1,
                Effects("subject", parameter),
                s => BExpression.Call(null, GuardMethod, s),
                s => Effects("fast", s),
                s => Effects("slow", s),
                typeof(int)),
            new FunctionName("refusedSite"),
            [parameter]);

        guardHolds = true;
        var (result, effects) = Run((Func<int, int>)lambda.Compile(), 9);

        Assert.Equal(9, result);
        Assert.Equal(new[] { "subject", "slow" }, effects);
    }

    // ── helpers ─────────────────────────────────────────────────────────────────────

    private static int Count(string[] effects, string tag)
    {
        var n = 0;
        foreach (var e in effects)
        {
            if (e == tag)
                n++;
        }
        return n;
    }

    /// <summary>The control never evaluates a guard, so those entries are dropped before comparing.</summary>
    private static string[] StripGuards(string[] effects)
    {
        var kept = new List<string>(effects.Length);
        foreach (var e in effects)
        {
            if (e != "guard")
                kept.Add(e);
        }
        return kept.ToArray();
    }

    /// <summary>
    /// Runs <paramref name="compiled"/> with a per-guard schedule: the guards are numbered in
    /// the order they execute, and <paramref name="holds"/> decides each one.
    /// </summary>
    private static int RunWithGuardSchedule(Func<int, int> compiled, int argument, Func<int, bool> holds)
    {
        var index = 0;
        guardScheduler = () => holds(index++);
        try
        {
            return compiled(argument);
        }
        finally
        {
            guardScheduler = null;
        }
    }
}
