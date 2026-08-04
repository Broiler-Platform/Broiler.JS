using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;

namespace Broiler.JavaScript.Engine.Benchmarks;

/// <summary>
/// Prices the pieces a JavaScript call's fixed prologue is built from — the ablation pass item 4-5
/// is not allowed to skip (docs/performance-roadmap.md item 4-5).
/// </summary>
/// <remarks>
/// <para>
/// Item 4-4's probe established that a call costs <b>142 ns before it carries any argument</b> and
/// that ~89% of that is inside <c>JSFunction.InvokeFunction</c> rather than the compiled body. That
/// says <em>where</em> to look and not <em>what to change</em>. Guessing from a reading of the code
/// is what §3.5 keeps recording as the failure mode, so each candidate is priced here first, in
/// isolation, against a control that does the same loop and nothing else.
/// </para>
/// <para>
/// <b>These are framework costs, replicated locally rather than called through the engine.</b> An
/// <c>AsyncLocal&lt;bool&gt;</c> read costs what it costs whoever declares it, and the benchmark
/// assembly cannot see the engine's internal one anyway. The claim being tested is about the
/// mechanism, so the mechanism is what is measured — and a local replica keeps the arms honest by
/// making them differ in one thing.
/// </para>
/// <para>
/// The standing claim this exists to check is in <c>JSEngine</c> itself: <em>"An AsyncLocal SET is
/// expensive though … Reads are cheap"</em> (P0-2). The set half is not in doubt. The read half is
/// asserted, has never been measured, and happens on every single call.
/// </para>
/// </remarks>
internal static class CallPrologueMetrics
{
    private static readonly AsyncLocal<bool> AsyncLocalFlag = new();

    [ThreadStatic]
    private static bool threadStaticFlag;

    private static bool plainStaticFlag;

    private static readonly Func<int, int> Callee = static x => x + 1;

    /// <summary>A no-op disposable struct, the shape every scope in the prologue has.</summary>
    private readonly struct NoOpScope : IDisposable
    {
        private readonly bool changed;

        public NoOpScope(bool changed) => this.changed = changed;

        public void Dispose()
        {
            if (changed)
                plainStaticFlag = !plainStaticFlag;
        }
    }

    internal static void Write(long iterations, int repetitions)
    {
        var shapes = new (string Name, Func<long, long> Body)[]
        {
            ("control-empty-loop", static n =>
            {
                long acc = 0;
                for (long i = 0; i < n; i++) acc += i & 1;
                return acc;
            }),

            ("threadstatic-read", static n =>
            {
                long acc = 0;
                for (long i = 0; i < n; i++) acc += threadStaticFlag ? 1 : 0;
                return acc;
            }),

            ("plainstatic-read", static n =>
            {
                long acc = 0;
                for (long i = 0; i < n; i++) acc += plainStaticFlag ? 1 : 0;
                return acc;
            }),

            // The claim under test.
            ("asynclocal-read", static n =>
            {
                long acc = 0;
                for (long i = 0; i < n; i++) acc += AsyncLocalFlag.Value ? 1 : 0;
                return acc;
            }),

            // Five nested `using`s over no-op scopes, which is the shape InvokeFunction has —
            // five EH regions per call whether or not any of them does anything.
            ("five-nested-usings", static n =>
            {
                long acc = 0;
                for (long i = 0; i < n; i++)
                {
                    using (new NoOpScope(false))
                    using (new NoOpScope(false))
                    using (new NoOpScope(false))
                    using (new NoOpScope(false))
                    using (new NoOpScope(false))
                    {
                        acc += i & 1;
                    }
                }

                return acc;
            }),

            ("one-using", static n =>
            {
                long acc = 0;
                for (long i = 0; i < n; i++)
                {
                    using (new NoOpScope(false))
                    {
                        acc += i & 1;
                    }
                }

                return acc;
            }),

            ("try-catch-finally", static n =>
            {
                long acc = 0;
                for (long i = 0; i < n; i++)
                {
                    try
                    {
                        acc += i & 1;
                    }
                    catch (NullReferenceException)
                    {
                        throw;
                    }
                    finally
                    {
                        plainStaticFlag = plainStaticFlag;
                    }
                }

                return acc;
            }),

            ("delegate-invoke", static n =>
            {
                long acc = 0;
                for (long i = 0; i < n; i++) acc += Callee((int)(i & 1));
                return acc;
            }),
        };

        var rows = new List<object>();
        for (var repetition = 0; repetition < repetitions; repetition++)
        {
            for (var offset = 0; offset < shapes.Length; offset++)
            {
                var shape = shapes[(offset + repetition) % shapes.Length];

                // Warm the tier-1 JIT so the timed run measures steady state.
                shape.Body(100_000);

                var stopwatch = Stopwatch.StartNew();
                var answer = shape.Body(iterations);
                stopwatch.Stop();

                rows.Add(new
                {
                    shape = shape.Name,
                    repetition,
                    elapsedMs = stopwatch.Elapsed.TotalMilliseconds,
                    nsPerIteration = stopwatch.Elapsed.TotalMilliseconds * 1_000_000 / iterations,
                    answer,
                });
            }
        }

        Console.WriteLine(JsonSerializer.Serialize(
            new
            {
                schema = "broiler.call-prologue-probe/1",
                iterations,
                note = "What the pieces of a call's fixed prologue cost, each against the same empty "
                    + "loop. Framework mechanisms are replicated locally rather than called through "
                    + "the engine, because the claim under test is about the mechanism. Subtract "
                    + "control-empty-loop from each.",
                runs = rows,
            },
            new JsonSerializerOptions { WriteIndented = true }));
    }

    /// <summary>Keeps the flags from being folded away as constants.</summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void Poke(bool value)
    {
        threadStaticFlag = value;
        plainStaticFlag = value;
        AsyncLocalFlag.Value = value;
    }
}
