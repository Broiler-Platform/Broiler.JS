using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Broiler.JavaScript.BuiltIns.Function;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Engine.Benchmarks;

/// <summary>
/// Prices this engine's two call entries against each other on the same callee, which item 4-4
/// named as the first thing item 4-5 should do and which had never been done
/// (docs/performance-roadmap.md items 4-4 and 4-5).
/// </summary>
/// <remarks>
/// <para>
/// <b>The two entries are a natural ablation that already ships, and that is the whole reason this
/// is worth measuring rather than reasoning about.</b> 4-5's component pass priced the prologue's
/// mechanisms by <em>replicating</em> them locally — five nested <c>using</c>s, a
/// <c>try</c>/<c>catch</c>/<c>finally</c>, a delegate invoke — and they summed to about 10 ns of a
/// ~147 ns call, which is why the item concluded that ~85% of a call's fixed cost is
/// "unattributable from outside the engine" and blocked itself on a sampling profiler. A replica
/// prices the <em>mechanism</em>; it says nothing about what the engine's own scopes do inside
/// themselves. <c>JSFunction.InvokeCallback</c> is the engine's own short path — the same delegate
/// selection and the same <c>this</c> coercion, with one <c>using</c> scope instead of five and
/// none of the executing-function or legacy-caller bookkeeping — so the difference between it and
/// <c>InvokeFunction</c> is that bookkeeping <em>as the engine actually performs it</em>.
/// </para>
/// <para>
/// <b>Reached by a delegate built once through reflection, so no engine change is needed.</b>
/// <c>InvokeCallback</c> is <c>internal</c> and the benchmark assembly is not a friend; binding a
/// delegate to it once and invoking that in the loop costs one delegate dispatch, which the
/// component pass already priced at <b>0.68 ns</b> — under 1% of the difference being measured, and
/// charged to the short arm, so it makes the gap look <em>smaller</em> rather than larger.
/// </para>
/// <para>
/// <b>What the difference is not.</b> The short path also resolves a tail-call sentinel that the
/// long path handles in its own loop, so a callee that never tail-calls pays a type test on the
/// short arm; and the long path's <c>with</c>-scope pushes are null-checked against a context that
/// has none here. Both make the measured gap a <em>lower</em> bound on the bookkeeping, which is
/// the direction that keeps the conclusion safe.
/// </para>
/// <para>
/// Round-robin with the arms reversed on alternate rounds and ratioed within each round — the same
/// harness `0107` had to adopt after running its arms in blocks reported spreads of 161–470%.
/// </para>
/// </remarks>
internal static class CallEntryCostMetrics
{
    private const int Iterations = 2_000_000;
    private const int Rounds = 12;
    private const int WarmUpPasses = 3;

    private delegate JSValue CallbackEntry(JSFunction target, in Arguments arguments);

    private sealed record Arm(
        string Name,
        string Note,
        double NanosecondsPerCall,
        double BytesPerCall,
        double SpreadPercent);

    internal static void Write()
    {
        using var context = BenchmarkContext.Create();

        // A callee with a body small enough that the entry dominates it, and one argument, which
        // is the corpus's commonest arity.
        var callee = (JSFunction)context.Eval(
            "(function (x) { return x + 1; })",
            "callee.js");

        var arguments = new Arguments(JSUndefined.Value, JSValue.CreateNumber(1));

        var method = typeof(JSFunction).GetMethod(
            "InvokeCallback",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("JSFunction.InvokeCallback not found");
        var callback = (CallbackEntry)Delegate.CreateDelegate(typeof(CallbackEntry), method);

        // Both arms must answer the same thing, or one of them is not running the callee. Asserted
        // before anything is timed, because a short path that silently returned early would look
        // like the finding this exists to produce.
        var longAnswer = callee.InvokeFunction(in arguments).ToString();
        var shortAnswer = callback(callee, in arguments).ToString();
        if (!string.Equals(longAnswer, shortAnswer, StringComparison.Ordinal))
            throw new InvalidOperationException($"entries disagree: {longAnswer} vs {shortAnswer}");

        // The STRICT control, which needs no engine change and which item 2-9 already established
        // as the right one: AddLegacyCallerAndArguments runs for non-strict functions only, so a
        // strict callee has HasLegacyCallerArguments false and InvokeFunction skips the legacy
        // frame entirely. The difference between the two callees through the SAME entry is
        // therefore that one piece of the bookkeeping, isolated without touching the engine.
        var strictCallee = (JSFunction)context.Eval(
            "(function () { 'use strict'; return function (x) { return x + 1; }; })()",
            "strict-callee.js");

        // Two more sloppy shapes. AddLegacyCallerAndArguments is emitted for ordinary non-strict
        // function declarations and expressions; an arrow and a shorthand method are neither, so
        // if the legacy frame is what the long entry is paying for, these separate from `callee`
        // without dragging in the strict-mode transition the strict arm turned out to be dominated
        // by. Two shapes rather than one, because a single one that separated could be separating
        // for a reason particular to arrows.
        var arrowCallee = (JSFunction)context.Eval("((x) => x + 1)", "arrow-callee.js");
        var methodCallee = (JSFunction)context.Eval(
            "({ m(x) { return x + 1; } }).m",
            "method-callee.js");

        // **Which shape pays what, asked of the engine rather than inferred from the timings.**
        // The arms below differ in more than one thing at a time — an arrow has no legacy frame AND
        // does not coerce `this` — so attributing their difference to one of those needs the engine
        // to say which of them each shape actually performs. One call each, counters on.
        var shapes = new[]
        {
            ("ordinary-sloppy-function", callee),
            ("strict-function", strictCallee),
            ("arrow", arrowCallee),
            ("object-method", methodCallee),
        }
        .Select(pair =>
        {
            CallPathDiagnostics.Reset();
            CallPathDiagnostics.Enabled = true;
            try
            {
                pair.Item2.InvokeFunction(in arguments);
                var snapshot = CallPathDiagnostics.Snapshot();
                return new
                {
                    shape = pair.Item1,
                    pushesLegacyFrame = snapshot.LegacyFrames == 1,
                    coercesThis = snapshot.ThisCoercions == 1,
                    strictTransition = snapshot.StrictTransitions > 0,
                };
            }
            finally
            {
                CallPathDiagnostics.Enabled = false;
                CallPathDiagnostics.Reset();
            }
        })
        .ToArray();

        var arms = Measure(callee, strictCallee, arrowCallee, methodCallee, arguments, callback);
        var longArm = arms[0];
        var shortArm = arms[1];

        Console.WriteLine(JsonSerializer.Serialize(
            new
            {
                metric = "call-entry-cost",
                iterations = Iterations,
                rounds = Rounds,
                answer = longAnswer,
                note =
                    "The engine's two call entries on the same callee. InvokeFunction is what every "
                    + "emitted JavaScript call site reaches; InvokeCallback is what a native builtin "
                    + "uses for a JavaScript callback, with one `using` scope instead of five and no "
                    + "executing-function or legacy-caller bookkeeping. Their difference is that "
                    + "bookkeeping as the engine performs it, rather than as a local replica of the "
                    + "mechanisms performs it — which is what item 4-5's component pass measured and "
                    + "why it could only account for ~10 ns of a ~147 ns call.",
                shapes,
                arms,
                differenceNs = longArm.NanosecondsPerCall - shortArm.NanosecondsPerCall,
                differenceBytes = longArm.BytesPerCall - shortArm.BytesPerCall,
                ratio = shortArm.NanosecondsPerCall / longArm.NanosecondsPerCall,
            },
            new JsonSerializerOptions { WriteIndented = true }));

        foreach (var shape in shapes)
        {
            Console.Error.WriteLine(
                $"{shape.shape,-26} legacyFrame={shape.pushesLegacyFrame,-6} "
                + $"coercesThis={shape.coercesThis,-6} strictTransition={shape.strictTransition}");
        }

        Console.Error.WriteLine();
        foreach (var arm in arms)
        {
            Console.Error.WriteLine(
                $"{arm.Name,-22} {arm.NanosecondsPerCall,8:F2} ns  {arm.BytesPerCall,6:F1} B  "
                + $"spread {arm.SpreadPercent,5:F1}%");
        }

        Console.Error.WriteLine(
            $"difference {longArm.NanosecondsPerCall - shortArm.NanosecondsPerCall:F2} ns  "
            + $"({shortArm.NanosecondsPerCall / longArm.NanosecondsPerCall:F3}x)");
    }

    private sealed record Plan(string Name, string Note, Func<double> Body);

    private static Arm[] Measure(
        JSFunction callee,
        JSFunction strictCallee,
        JSFunction arrowCallee,
        JSFunction methodCallee,
        Arguments arguments,
        CallbackEntry callback)
    {
        // The component arms call the ENGINE's own accessors, not local replicas of the
        // mechanisms — which is the whole correction 0108 rests on. Only the pieces that are
        // publicly reachable are here; the two that are not (`HasLegacyCallerArguments` and the
        // legacy frame it gates) are isolated instead by the strict/sloppy pair above, and the
        // strict-mode scope is left to its own already-measured figure rather than reached by
        // reflection, because binding a delegate to it would box the scope struct and price an
        // allocation the real path does not make.
        var context = (JSContext)JSEngine.Current;
        var localArguments = arguments;
        var plan = new[]
        {
            new Plan("InvokeFunction", "every emitted JavaScript call site: five `using` scopes, "
                + "the executing-function save/set/restore, the legacy-caller check and frame, the "
                + "strict-mode scope, the with-scope pushes, two try/finally regions and the "
                + "tail-call loop",
                () => RunLong(callee, in localArguments)),
            new Plan("InvokeCallback", "the engine's own short path, used by every native builtin "
                + "running a JavaScript callback: one `using` scope, the same delegate selection "
                + "and `this` coercion, and none of the bookkeeping above",
                () => RunShort(callback, callee, in localArguments)),
            new Plan("InvokeFunction-strict", "the same long entry on a STRICT callee, which has no "
                + "legacy caller/arguments — so the legacy frame is skipped and this arm's distance "
                + "from InvokeFunction is that piece alone",
                () => RunLong(strictCallee, in localArguments)),
            new Plan("InvokeCallback-strict", "the short entry on the strict callee, as the control "
                + "for the pair above: it never pushes a legacy frame either way, so the two "
                + "callback arms should NOT separate — and if they do, the strict pair is measuring "
                + "something other than the frame",
                () => RunShort(callback, strictCallee, in localArguments)),

            new Plan("InvokeFunction-arrow", "a SLOPPY arrow callee through the long entry. An "
                + "arrow gets no legacy caller/arguments, so this isolates the legacy frame from "
                + "the strict-mode transition the strict arm turned out to be dominated by",
                () => RunLong(arrowCallee, in localArguments)),
            new Plan("InvokeFunction-method", "a sloppy object METHOD through the long entry, as a "
                + "second shape with the same property",
                () => RunLong(methodCallee, in localArguments)),

            new Plan("component-empty", "the loop itself, subtracted from the component arms below",
                RunEmpty),
            new Plan("component-executing-function", "JSEngine.ExecutingFunction saved, set and "
                + "restored — three accesses to the [ThreadStatic] the long entry keeps in step",
                RunExecutingFunction),
            new Plan("component-current-and-options", "`JSEngine.Current as JSContext` and the "
                + "`Options.ScriptHostMode` read the long entry makes on every call to decide "
                + "whether to suspend with-scopes",
                RunCurrentAndOptions),
            new Plan("component-with-scope-pushes", "the two with-scope pushes under `using`. Both "
                + "return null early here (nothing captured), so this prices the calls and the null "
                + "disposes rather than the scopes",
                () => RunWithScopePushes(context)),
        };

        for (var warmUp = 0; warmUp < WarmUpPasses; warmUp++)
        {
            foreach (var arm in plan)
                arm.Body();
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
                plan[i].Body();
                watch.Stop();
                var after = GC.GetAllocatedBytesForCurrentThread();

                samples[i][round] = watch.Elapsed.TotalMilliseconds * 1_000_000.0 / Iterations;
                bytes[i][round] = (after - before) / (double)Iterations;
            }
        }

        var arms = new Arm[plan.Length];
        for (var i = 0; i < plan.Length; i++)
            arms[i] = Build(plan[i].Name, samples[i], bytes[i], plan[i].Note);

        return arms;
    }

    private static double RunEmpty()
    {
        var accumulator = 0.0;
        for (var i = 0; i < Iterations; i++)
            accumulator += i & 1;

        return accumulator;
    }

    private static double RunExecutingFunction()
    {
        var accumulator = 0.0;
        for (var i = 0; i < Iterations; i++)
        {
            var previous = JSEngine.ExecutingFunction;
            JSEngine.ExecutingFunction = null;
            accumulator += previous == null ? 1 : 0;
            JSEngine.ExecutingFunction = previous;
        }

        return accumulator;
    }

    private static double RunCurrentAndOptions()
    {
        var accumulator = 0.0;
        for (var i = 0; i < Iterations; i++)
        {
            var context = JSEngine.Current as JSContext;
            accumulator += context?.Options.ScriptHostMode == true ? 1 : 0;
        }

        return accumulator;
    }

    private static double RunWithScopePushes(JSContext context)
    {
        var accumulator = 0.0;
        for (var i = 0; i < Iterations; i++)
        {
            using var fallback = context?.PushWithFallbackScopes(null);
            using var scope = context?.PushWithScopes(null);
            accumulator += fallback == null && scope == null ? 1 : 0;
        }

        return accumulator;
    }

    private static Arm Build(string name, double[] samples, double[] bytes, string note)
    {
        var ordered = samples.OrderBy(x => x).ToArray();
        var spread = ordered[0] > 0 ? (ordered[^1] - ordered[0]) * 100.0 / ordered[0] : 0;
        return new Arm(name, note, ordered[Rounds / 2], bytes.OrderBy(x => x).ElementAt(Rounds / 2), spread);
    }

    private static double RunLong(JSFunction callee, in Arguments arguments)
    {
        var accumulator = 0.0;
        for (var i = 0; i < Iterations; i++)
            accumulator += callee.InvokeFunction(in arguments).DoubleValue;

        return accumulator;
    }

    private static double RunShort(CallbackEntry callback, JSFunction callee, in Arguments arguments)
    {
        var accumulator = 0.0;
        for (var i = 0; i < Iterations; i++)
            accumulator += callback(callee, in arguments).DoubleValue;

        return accumulator;
    }
}
