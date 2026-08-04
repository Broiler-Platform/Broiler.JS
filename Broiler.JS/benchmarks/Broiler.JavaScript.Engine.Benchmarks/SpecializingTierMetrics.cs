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

    internal static void Write(string octaneDirectory, bool tieringEnabled)
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
            var row = RunSuite(octaneDirectory, baseSource, suite, tieringEnabled);
            if (row != null)
                rows.Add(row);
        }

        Console.WriteLine(JsonSerializer.Serialize(
            new
            {
                schema = "broiler.specializing-tier-metrics/1",
                runsPerBenchmark = RunsPerBenchmark,
                tieringEnabled,
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

    private static object RunSuite(string octaneDirectory, string baseSource, Suite suite, bool tieringEnabled)
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
        using var context = new JavaScriptContextBuilder()
            .UseFunctionTiering(tieringEnabled
                ? new FunctionTieringOptions
                {
                    Enabled = true,
                    InvocationThreshold = 64,
                    MaxRecompilations = 100_000,
                    MaxRetainedCodeBytes = 512L * 1024 * 1024,
                }
                : FunctionTieringOptions.Disabled)
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
        };
    }
}
