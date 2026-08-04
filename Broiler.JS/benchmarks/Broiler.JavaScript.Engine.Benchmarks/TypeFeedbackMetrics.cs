using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Engine.Benchmarks;

/// <summary>
/// Reports what item 4-1's per-site type feedback actually saw on the real Octane suites: how
/// many executed property reads and calls happened at a site that only ever observed one shape,
/// or one callee.
/// </summary>
/// <remarks>
/// <para>
/// 4-1 buys nothing by itself — it is collection, and 4-2 and 4-4 are what consume it. So the
/// deliverable is this number, because it is the premise those two items rest on and neither
/// has ever been checked against it. <em>"Monomorphic property access → shape check plus direct
/// slot read"</em> (4-2) and <em>"inlining of small JS callees at monomorphic sites"</em> (4-4)
/// are both worth an XL each only in proportion to how much of the real work happens at
/// monomorphic sites. If that share is high the phase is well-founded; if it is not, the phase
/// needs re-specifying before anything is built, which is exactly the failure §3.5 keeps
/// recording.
/// </para>
/// <para>
/// <b>Weighted by execution, not by site count.</b> A tier only pays where the work is: ten
/// thousand cold monomorphic sites are worth nothing and one hot one is worth everything. Both
/// are reported, and they differ enough that quoting the wrong one would mislead.
/// </para>
/// <para>
/// <b>Not a timing measurement.</b> Recording is on throughout, which adds a branch per read
/// and a call per call, and it retains every callee. Counts here are deterministic; the cost of
/// collection is measured separately, with the feedback OFF, because that is the configuration
/// that ships.
/// </para>
/// </remarks>
internal static class TypeFeedbackMetrics
{
    private sealed record Suite(string Name, string[] Files);

    /// <summary>
    /// The call-heavy cluster phase 4 exists for, plus two controls. Richards and DeltaBlue are
    /// built out of one-line methods and have the worst throughput ratios in the suite (§4.3
    /// B2); Crypto and NavierStokes are arithmetic-heavy rather than call-heavy, so they say
    /// whether a high monomorphic share is a property of this corpus or of those two suites.
    /// </summary>
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

    internal static void Write(string octaneDirectory)
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
            var row = RunSuite(octaneDirectory, baseSource, suite);
            if (row != null)
                rows.Add(row);
        }

        Console.WriteLine(JsonSerializer.Serialize(
            new
            {
                schema = "broiler.type-feedback-metrics/1",
                runsPerBenchmark = RunsPerBenchmark,
                maxTrackedPerSite = TypeFeedback.MaxTracked,
                note = "What item 4-1's per-site feedback saw on real Octane suites. "
                    + "monomorphicObservationShare is the number items 4-2 and 4-4 rest on: the "
                    + "share of EXECUTED reads/calls at a site that only ever saw one shape or "
                    + "one callee. Site-count shares are reported too and are much lower, "
                    + "because most sites barely run. A site that saw more than "
                    + "maxTrackedPerSite distinct observations is counted megamorphic, which is "
                    + "the same threshold the inline cache uses. Counts are deterministic; no "
                    + "wall clock is reported, and collection is off in any timing run.",
                suites = rows,
            },
            new JsonSerializerOptions { WriteIndented = true }));
    }

    private static object RunSuite(string octaneDirectory, string baseSource, Suite suite)
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

        using var context = BenchmarkContext.Create();

        string outcome;
        TypeFeedback.Distribution properties, calls;

        // Enabled around the LOAD as well as the run, and that ordering is required rather than
        // tidy: call feedback is emitted at compile time, so a suite compiled before the flag
        // was set would report no call sites at all.
        using (TypeFeedback.Enable())
        {
            TypeFeedback.Reset();
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

            try
            {
                outcome = context.Eval(Driver, "driver.js").ToString();
            }
            catch (Exception e)
            {
                outcome = $"0|driver threw {e.GetType().Name}: {e.Message}";
            }

            properties = TypeFeedback.PropertyDistribution();
            calls = TypeFeedback.CallDistribution();
        }

        var split = outcome.Split('|', 2);

        return new
        {
            suite = suite.Name,
            loaded = true,
            benchmarksRun = int.TryParse(split[0], out var ran) ? ran : 0,
            failures = split.Length > 1 && split[1].Length > 0 ? split[1] : null,

            propertyReads = Describe(properties),
            calls = Describe(calls),
        };
    }

    private static object Describe(TypeFeedback.Distribution d) => new
    {
        liveSites = d.LiveSites,
        coldSites = d.ColdSites,
        monomorphicSites = d.MonomorphicSites,
        polymorphicSites = d.PolymorphicSites,
        megamorphicSites = d.MegamorphicSites,
        monomorphicSitePercent = Math.Round(100 * d.MonomorphicSiteShare, 2),

        observations = d.Observations,
        monomorphicObservations = d.MonomorphicObservations,
        polymorphicObservations = d.PolymorphicObservations,
        megamorphicObservations = d.MegamorphicObservations,

        // The headline: what share of the work happens somewhere a specializing tier could
        // speculate on a single shape or a single callee.
        monomorphicObservationPercent = Math.Round(100 * d.MonomorphicObservationShare, 2),
    };
}
