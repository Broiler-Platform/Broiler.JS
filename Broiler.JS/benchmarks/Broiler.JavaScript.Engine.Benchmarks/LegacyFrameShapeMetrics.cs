using System;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Engine.Benchmarks;

/// <summary>
/// Prices the shape of item 4-5's legacy frame against the shape a fix would have, before anybody
/// builds the fix (docs/performance-roadmap.md item 4-5).
/// </summary>
/// <remarks>
/// <para>
/// <b>`0110` established the size of the problem; this asks whether the obvious fix is the right
/// one.</b> The Annex B frame costs ~44 ns on 60.16% of the corpus's calls, and the reason is
/// visible in the code rather than mysterious: <c>Arguments</c> is a <b>56-byte</b> struct — one
/// <c>int</c> and six references — and <c>LegacyFrame</c> wraps it to <b>72</b>. Pushing a frame
/// copies the field out into a saved frame, copies the new arguments in, returns the saved frame by
/// value, and pops by copying back: <b>four 56-to-72-byte copies per call</b>, on the function
/// object, to keep a value nothing reads.
/// </para>
/// <para>
/// <b>A replica is the right instrument HERE, which is worth saying given `0108`.</b> That patch's
/// finding was that replicating a <em>mechanism</em> — a <c>using</c> scope, an EH region — says
/// nothing about what the engine's own scopes do inside themselves. This replicates a <b>struct
/// copy</b>, which has no inside: the JIT emits the same moves for the same layout wherever it
/// appears. The arms below use a struct of identical shape, so what they measure is the copying,
/// which is the thing the fix would change.
/// </para>
/// <para>
/// <b>The alternative arm is the design the item now points at.</b> Instead of saving and restoring
/// per-invocation state <em>on the function object</em>, push it onto a thread-local invocation
/// stack and pop an index — one write in, one decrement out, and no read-modify-write of a field
/// that every other call also touches. `caller`/`arguments` would then find the innermost
/// invocation by scanning, which is free in practice because nothing reads them. The arm is here to
/// say whether that trade is worth making before an M–L is spent on it.
/// </para>
/// </remarks>
internal static class LegacyFrameShapeMetrics
{
    private const int Iterations = 20_000_000;
    private const int Rounds = 12;
    private const int WarmUpPasses = 3;

    /// <summary>The same layout as <c>JSFunction.LegacyFrame</c>: a reference, an
    /// <see cref="Arguments"/>, and a flag.</summary>
    private struct Frame(JSValue caller, Arguments arguments, bool active)
    {
        public JSValue Caller = caller;
        public Arguments Arguments = arguments;
        public bool Active = active;
    }

    // The per-function state the current design saves and restores.
    private static JSValue callerValue;
    private static Arguments frameArguments;
    private static bool frameActive;

    // The alternative: one thread-local stack, an index, and no field round-trip.
    [ThreadStatic]
    private static Frame[] stack;
    [ThreadStatic]
    private static int depth;

    private sealed record Arm(string Name, string Note, double NanosecondsPerCall, double SpreadPercent);

    internal static void Write()
    {
        using var context = BenchmarkContext.Create();
        var arguments = new Arguments(JSUndefined.Value, JSValue.CreateNumber(1));
        var caller = context.Eval("(function (x) { return x + 1; })", "caller.js");

        stack = new Frame[1024];
        depth = 0;

        var plan = new (string Name, string Note, Func<long> Body)[]
        {
            ("control", "the loop and the guard, with no frame work at all — what an arrow or a "
                + "strict callee pays",
                () => RunControl()),
            ("save-restore-on-the-function", "what the engine does today: build a saved frame from "
                + "three fields, write the new arguments in, then copy all three back",
                () => RunSaveRestore(caller, in arguments)),
            ("push-pop-a-thread-local-stack", "the alternative: one 72-byte write at the top of a "
                + "thread-local stack and an index decrement, with no field round-trip",
                () => RunStack(caller, in arguments)),
            ("arguments-copy-alone", "one 56-byte Arguments copy, to say how much of the difference "
                + "is the struct's size rather than the field traffic",
                () => RunArgumentsCopy(in arguments)),
        };

        var arms = Measure(plan);

        Console.WriteLine(JsonSerializer.Serialize(
            new
            {
                metric = "legacy-frame-shape",
                iterations = Iterations,
                rounds = Rounds,
                argumentsBytes = 56,
                frameBytes = 72,
                note =
                    "Item 4-5's legacy frame costs ~44 ns on 60.16% of the corpus's calls (0110). "
                    + "This prices the SHAPE of that cost against the shape a fix would have, so the "
                    + "M-L is decided on a number. A replica is legitimate here because what is "
                    + "being measured is struct copying, which has no inside — unlike 0108's "
                    + "`using` scopes, where replicating the mechanism was the error.",
                arms,
            },
            new JsonSerializerOptions { WriteIndented = true }));

        foreach (var arm in arms)
            Console.Error.WriteLine($"{arm.Name,-32} {arm.NanosecondsPerCall,7:F2} ns  spread {arm.SpreadPercent,5:F1}%");

        var control = arms[0].NanosecondsPerCall;
        Console.Error.WriteLine();
        Console.Error.WriteLine($"today over control      {arms[1].NanosecondsPerCall - control,7:F2} ns");
        Console.Error.WriteLine($"stack over control      {arms[2].NanosecondsPerCall - control,7:F2} ns");
        Console.Error.WriteLine(
            $"the fix would save      {arms[1].NanosecondsPerCall - arms[2].NanosecondsPerCall,7:F2} ns "
            + $"({(arms[2].NanosecondsPerCall - control) / (arms[1].NanosecondsPerCall - control):F3}x)");
    }

    private static Arm[] Measure((string Name, string Note, Func<long> Body)[] plan)
    {
        for (var warmUp = 0; warmUp < WarmUpPasses; warmUp++)
            foreach (var arm in plan)
                arm.Body();

        var samples = new double[plan.Length][];
        for (var i = 0; i < plan.Length; i++)
            samples[i] = new double[Rounds];

        for (var round = 0; round < Rounds; round++)
        {
            for (var step = 0; step < plan.Length; step++)
            {
                var i = round % 2 == 0 ? step : plan.Length - 1 - step;
                GC.Collect(2, GCCollectionMode.Forced, blocking: true);
                GC.WaitForPendingFinalizers();

                var watch = Stopwatch.StartNew();
                plan[i].Body();
                watch.Stop();
                samples[i][round] = watch.Elapsed.TotalMilliseconds * 1_000_000.0 / Iterations;
            }
        }

        var arms = new Arm[plan.Length];
        for (var i = 0; i < plan.Length; i++)
        {
            var ordered = samples[i].OrderBy(x => x).ToArray();
            arms[i] = new Arm(
                plan[i].Name,
                plan[i].Note,
                ordered[Rounds / 2],
                ordered[0] > 0 ? (ordered[^1] - ordered[0]) * 100.0 / ordered[0] : 0);
        }

        return arms;
    }

    private static long RunControl()
    {
        var accumulator = 0L;
        for (var i = 0; i < Iterations; i++)
            accumulator += frameActive ? 1 : 0;

        return accumulator;
    }

    private static long RunSaveRestore(JSValue caller, in Arguments arguments)
    {
        var accumulator = 0L;
        for (var i = 0; i < Iterations; i++)
        {
            var saved = new Frame(callerValue, frameArguments, frameActive);

            callerValue = caller is JSValue ordinary ? ordinary : JSUndefined.Value;
            frameArguments = arguments;
            frameActive = true;

            accumulator += frameActive ? 1 : 0;

            callerValue = saved.Caller;
            frameArguments = saved.Arguments;
            frameActive = saved.Active;
        }

        return accumulator;
    }

    private static long RunStack(JSValue caller, in Arguments arguments)
    {
        var accumulator = 0L;
        for (var i = 0; i < Iterations; i++)
        {
            stack[depth++] = new Frame(caller, arguments, true);
            accumulator += stack[depth - 1].Active ? 1 : 0;
            depth--;
        }

        return accumulator;
    }

    private static long RunArgumentsCopy(in Arguments arguments)
    {
        var accumulator = 0L;
        for (var i = 0; i < Iterations; i++)
        {
            frameArguments = arguments;
            accumulator += frameArguments.Length;
        }

        return accumulator;
    }
}
