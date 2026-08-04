using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Engine.Benchmarks;

/// <summary>
/// What the tier-2 recompile actually reaches on the real Octane suites: how many functions are
/// tiering candidates, how many get promoted, and — the number item 4-2 rests on — what share of
/// executed property reads happen inside a promoted function.
/// </summary>
/// <remarks>
/// <para>
/// Item 4-2 says "replace the <c>numericPlan == null</c> branch" of
/// <c>JSFunction.RecompileForTiering</c>. That is only worth an XL if the branch is reached by
/// code that matters, and nothing in this engine could say whether it was: tiering is off by
/// default, and its eligibility gate in <c>FastCompiler.CreateFunction</c> is narrow enough
/// (no nested functions, no outer-function captures, scalar-replaceable locals, not a class,
/// not an arrow) that "how many real functions survive it" is a genuine open question rather
/// than a formality.
/// </para>
/// <para>
/// So this runs before the specializing tier is built, on the same seven suites item 4-1 used,
/// and reports both halves: the candidate/promotion counts, and the share of executed property
/// reads that a promoted function would own. The second is the one that decides the item — a
/// specializing tier can only speed up reads it emits, so the reads inside promoted functions
/// are its entire addressable surface, and 4-1's 93.5% monomorphic share applies to that
/// surface rather than to the whole corpus.
/// </para>
/// </remarks>
internal static class SpecializingTierMetrics
{
    /// <summary>
    /// What one monomorphic property read costs on each path, isolated from everything else.
    /// </summary>
    /// <remarks>
    /// The suite measurement says the specialization removes 44.7% of executed reads from the
    /// inline-cache path and does not move the wall clock. Two things could explain that — the
    /// specialized path is not actually cheaper, or property reads are too small a share of the
    /// time for any change to them to be visible — and they call for opposite follow-ups. This
    /// separates them: one promoted function whose body is a read in a loop, so essentially all
    /// of the measured time IS the read path, timed with the specialization on and off.
    /// </remarks>
    internal static void WriteReadProbe(int iterations, int repetitions)
    {
        const string Probe = """
            function hot(o, n) {
              var s = 0;
              for (var i = 0; i < n; i++) { s = s + o.x; }
              return s;
            }
            """;

        var rows = new List<object>();
        for (var repetition = 0; repetition < repetitions; repetition++)
        {
            // Alternate which arm goes first, so a monotonic drift in the machine does not land
            // entirely on one of them.
            var armsInOrder = repetition % 2 == 0
                ? new[] { Arm.Feedback, Arm.Specializing }
                : new[] { Arm.Specializing, Arm.Feedback };

            foreach (var arm in armsInOrder)
            {
                TypeFeedback.Enabled = arm == Arm.Feedback;
                TypeFeedback.Reset();
                Speculation.Reset();
                PropertyOptimizationDiagnostics.Enabled = false;

                using var context = new JavaScriptContextBuilder()
                    .UseFunctionTiering(new FunctionTieringOptions
                    {
                        Enabled = true,
                        InvocationThreshold = 2,
                        MaxRecompilations = 64,
                        MaxRetainedCodeBytes = 8L * 1024 * 1024,
                        SpecializeFromTypeFeedback = arm == Arm.Specializing,
                    })
                    .Build();

                context.Eval(Probe + "\nvar o = { x: 1 };\nhot(o, 1000); hot(o, 1000); hot(o, 1000);", "probe.js");

                var stopwatch = Stopwatch.StartNew();
                var answer = context.Eval($"hot(o, {iterations});", "run.js").ToString();
                stopwatch.Stop();

                rows.Add(new
                {
                    arm = arm.ToString(),
                    repetition,
                    elapsedMs = stopwatch.Elapsed.TotalMilliseconds,
                    nsPerRead = stopwatch.Elapsed.TotalMilliseconds * 1_000_000 / iterations,
                    answer,
                    specializedReads = Speculation.Snapshot().Sites,
                    guardsMissed = Speculation.Snapshot().GuardsMissed,
                });
            }
        }

        TypeFeedback.Enabled = false;
        Console.WriteLine(JsonSerializer.Serialize(
            new
            {
                schema = "broiler.specializing-tier-read-probe/1",
                iterations,
                note = "One promoted function whose body is a monomorphic property read in a loop, "
                    + "so nearly all the measured time is the read path. Both arms record type "
                    + "feedback; they differ only in whether the read is emitted as a shape guard "
                    + "plus a direct slot load or as the ordinary cached get.",
                runs = rows,
            },
            new JsonSerializerOptions { WriteIndented = true }));
    }

    private sealed record Suite(string Name, string[] Files);

    private static readonly Suite[] Suites =
    [
        new("Richards", ["richards.js"]),
        new("DeltaBlue", ["deltablue.js"]),
        new("RayTrace", ["raytrace.js"]),
        new("Box2D", ["box2d.js"]),
        new("EarleyBoyer", ["earley-boyer.js"]),
        new("Crypto", ["crypto.js"]),
        new("NavierStokes", ["navier-stokes.js"]),
    ];

    private const int RunsPerBenchmark = 3;

    private const string Driver = """
        (function () {
            var suites = (typeof BenchmarkSuite !== 'undefined' && BenchmarkSuite.suites) || [];
            var ran = 0, failures = [];
            for (var i = 0; i < suites.length; i++) {
                var suite = suites[i];
                for (var j = 0; j < suite.benchmarks.length; j++) {
                    var benchmark = suite.benchmarks[j];
                    try {
                        benchmark.Setup();
                        for (var k = 0; k < __runs; k++) benchmark.run();
                        benchmark.TearDown();
                        ran++;
                    } catch (e) {
                        failures.push(suite.name + '/' + benchmark.name + ': ' + e);
                    }
                }
            }
            return ran + '|' + failures.join(' ;; ');
        })()
        """;

    /// <summary>Which of the three arms to run: no tiering, tiering, or tiering that specializes.</summary>
    internal enum Arm
    {
        /// <summary>Tiering off entirely — the answer the engine gives without any of this.</summary>
        None,
        /// <summary>Tiering on, specialization off — what item 4-2a left behind.</summary>
        Tiered,
        /// <summary>
        /// Tiering on, feedback recording on, specialization off. The control that separates the
        /// cost of COLLECTING feedback from the effect of CONSUMING it — without it the two arms
        /// differ in two things and neither number means anything on its own.
        /// </summary>
        Feedback,
        /// <summary>Tiering on, monomorphic reads specialized — item 4-2b.</summary>
        Specializing,
    }

    internal static void Write(string octaneDirectory, Arm arm, bool counters)
    {
        if (!Directory.Exists(octaneDirectory))
        {
            Console.Error.WriteLine($"octane directory not found: {octaneDirectory}");
            Environment.ExitCode = 2;
            return;
        }

        var basePath = Path.Combine(octaneDirectory, "base.js");
        if (!File.Exists(basePath))
        {
            Console.Error.WriteLine($"not an Octane checkout (no base.js): {octaneDirectory}");
            Environment.ExitCode = 2;
            return;
        }

        var baseSource = File.ReadAllText(basePath);
        var rows = new List<object>();
        foreach (var suite in Suites)
        {
            Console.Error.WriteLine($"{suite.Name}: running ...");
            var row = RunSuite(octaneDirectory, baseSource, suite, arm, counters);
            if (row != null)
                rows.Add(row);
        }

        Console.WriteLine(JsonSerializer.Serialize(
            new
            {
                schema = "broiler.specializing-tier-metrics/1",
                runsPerBenchmark = RunsPerBenchmark,
                arm = arm.ToString(),
                counters,
                note = "What item 4-2's tier-2 recompile reaches. candidates is how many compiled "
                    + "functions passed FastCompiler.CreateFunction's tiering gate; recompilations "
                    + "how many of those got hot enough to be promoted. promotedReadShare is the "
                    + "share of EXECUTED property reads that happen at a site owned by a promoted "
                    + "function — the specializing tier's entire addressable surface, and the "
                    + "number the item rests on.",
                suites = rows,
            },
            new JsonSerializerOptions { WriteIndented = true }));
    }

    private static object RunSuite(string octaneDirectory, string baseSource, Suite suite, Arm arm, bool counters)
    {
        var sources = new List<string> { baseSource };
        foreach (var file in suite.Files)
        {
            var path = Path.Combine(octaneDirectory, file);
            if (!File.Exists(path))
            {
                Console.Error.WriteLine($"{suite.Name}: missing {file}, skipped");
                return null;
            }

            sources.Add(File.ReadAllText(path));
        }

        // A deliberately generous budget: the question is how many functions the GATE admits and
        // how many get hot, not how many a 32-recompilation cap lets through. A cap that bound
        // the answer would make the measurement report the cap.
        // Process-wide tables, so the arm's numbers are this suite's rather than cumulative.
        TypeFeedback.Enabled = arm == Arm.Feedback;
        TypeFeedback.Reset();
        Speculation.Reset();
        PropertyOptimizationDiagnostics.Reset();

        // Cache hits are what makes the addressable surface COUNTABLE rather than argued. A read
        // that takes the specialized path never calls PropertyInlineCacheSite.Get, so it records
        // no hit — so cacheHits(Tiered) - cacheHits(Specializing) is exactly the number of
        // executed reads the specialization took off the cache path. Deterministic, unlike a wall
        // clock, and the arms differ in nothing else.
        //
        // Off for a timing pass, because they are a branch and an interlocked increment per read
        // on the very path being measured. Counts and wall clock therefore come from separate
        // passes, which is the only honest way to have both.
        PropertyOptimizationDiagnostics.Enabled = counters;

        using var context = new JavaScriptContextBuilder()
            .UseFunctionTiering(arm == Arm.None
                ? FunctionTieringOptions.Disabled
                : new FunctionTieringOptions
                {
                    Enabled = true,
                    InvocationThreshold = 64,
                    MaxRecompilations = 100_000,
                    MaxRetainedCodeBytes = 512L * 1024 * 1024,
                    // Arm.Feedback records but does not consume, which is the whole point of it.
                    SpecializeFromTypeFeedback = arm == Arm.Specializing,
                })
            .Build();

        string outcome;
        var stopwatch = new Stopwatch();
        try
        {
            context.Eval("if (typeof print === 'undefined') { var print = function () { }; }", "shim.js");
            for (var i = 0; i < sources.Count; i++)
                context.Eval(sources[i], i == 0 ? "base.js" : suite.Files[i - 1]);

            context.Eval($"var __runs = {RunsPerBenchmark};", "runs.js");
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"{suite.Name}: failed to load — {e.GetType().Name}: {e.Message}");
            return new { suite = suite.Name, loaded = false, error = $"{e.GetType().Name}: {e.Message}" };
        }

        stopwatch.Start();
        try
        {
            outcome = context.Eval(Driver, "driver.js").ToString();
        }
        catch (Exception e)
        {
            outcome = $"0|driver threw {e.GetType().Name}: {e.Message}";
        }

        stopwatch.Stop();
        var tiering = context.FunctionTiering.Snapshot();
        var speculation = Speculation.Snapshot();
        var properties = PropertyOptimizationDiagnostics.Snapshot();
        var split = outcome.Split('|', 2);

        return new
        {
            suite = suite.Name,
            loaded = true,
            benchmarksRun = int.TryParse(split[0], out var ran) ? ran : 0,
            failures = split.Length > 1 && split[1].Length > 0 ? split[1] : null,
            elapsedMs = stopwatch.ElapsedMilliseconds,

            candidates = tiering.Candidates,
            invocations = tiering.Invocations,
            recompilationAttempts = tiering.RecompilationAttempts,
            recompilations = tiering.Recompilations,
            recompilationFailures = tiering.RecompilationFailures,
            budgetRejections = tiering.BudgetRejections,
            deoptimizations = tiering.Deoptimizations,
            retainedCodeBytes = tiering.RetainedCodeBytes,

            // One speculation site per property read the tier-2 compile specialized. Guard misses
            // and poisoned sites say whether the feedback's "only ever saw one shape" held for the
            // rest of the run — the claim the whole item rests on, checked against execution
            // rather than argued.
            specializedReads = speculation.Sites,
            guardsMissed = speculation.GuardsMissed,
            poisonedSites = speculation.PoisonedSites,

            cacheHits = properties.CacheHits,
            cacheMisses = properties.CacheMisses,
        };
    }
}
