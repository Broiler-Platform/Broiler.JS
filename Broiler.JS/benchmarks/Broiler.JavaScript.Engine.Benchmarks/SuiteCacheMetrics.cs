using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Engine.Benchmarks;

/// <summary>
/// Runs real Octane suites and reports what the property inline caches actually did on them —
/// hit rate, megamorphic sites, dictionary fallbacks, shape transitions and trie
/// materializations — rather than what a synthetic loop shaped like them does.
/// </summary>
/// <remarks>
/// Exists because phase 2's exit criterion has now been measured and it SPLIT: Richards is inside
/// 200× at 183× and DeltaBlue is not, at 576×. Every phase 2 item was sized on a probe, and §3.5
/// already records what that costs — 2-8 was justified by DeltaBlue's score, measured with a loop
/// written to look like DeltaBlue's hot path, and broke DeltaBlue outright, because the loop
/// reproduced the reads the item was about and none of the writes.
/// <para>
/// So the question this answers is deliberately narrow: <em>do the caches phase 2 built actually
/// hit on the suite phase 2 is failing?</em> Richards is the control — the same cluster, the same
/// items, and it passes — so a counter that differs sharply between the two is a lead and a
/// counter that does not is an exoneration. Box2D is included because §0 names the same trio.
/// </para>
/// <para>
/// Counters over wall clock on purpose. Hit rates and counts are deterministic and exact, and
/// this machine has already been shown to be too noisy for a single-run score to mean anything
/// (§Phase 0). A ratio between two suites measured in the same process is not a timing claim.
/// </para>
/// </remarks>
internal static class SuiteCacheMetrics
{
    private sealed record Suite(string Name, string[] Files);

    /// <summary>The cluster §0 names for phase 2, with Richards as the passing control.</summary>
    private static readonly Suite[] Suites =
    [
        new("Richards", ["richards.js"]),
        new("DeltaBlue", ["deltablue.js"]),
        new("Box2D", ["box2d.js"]),
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
                schema = "broiler.suite-cache-metrics/1",
                runsPerBenchmark = RunsPerBenchmark,
                note = "Property inline cache behaviour on the real Octane suites of phase 2's "
                    + "cluster. Richards passes the phase's 200x exit criterion and DeltaBlue "
                    + "fails it at 576x, so a counter that separates them is a lead. Counts are "
                    + "deterministic; no wall clock is reported because this machine is too "
                    + "noisy for a single run to mean anything.",
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
        PropertyOptimizationSnapshot snapshot;

        // The window covers the LOAD as well as the run: the shapes a suite's top-level code
        // builds are shapes the workload builds, and a cache that misses while warming is part
        // of what the score pays for.
        using (PropertyOptimizationDiagnostics.Enable())
        {
            PropertyOptimizationDiagnostics.Reset();
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

            snapshot = PropertyOptimizationDiagnostics.Snapshot();
        }

        var split = outcome.Split('|', 2);
        var reads = snapshot.CacheHits + snapshot.CacheMisses;
        var writes = snapshot.StoreCacheHits + snapshot.StoreCacheMisses;

        return new
        {
            suite = suite.Name,
            loaded = true,
            benchmarksRun = int.TryParse(split[0], out var ran) ? ran : 0,
            failures = split.Length > 1 && split[1].Length > 0 ? split[1] : null,

            readCacheHits = snapshot.CacheHits,
            readCacheMisses = snapshot.CacheMisses,
            readHitRatePercent = reads == 0 ? 0 : Math.Round(100.0 * snapshot.CacheHits / reads, 2),
            readMegamorphicSites = snapshot.MegamorphicSites,

            storeCacheHits = snapshot.StoreCacheHits,
            storeCacheMisses = snapshot.StoreCacheMisses,
            storeHitRatePercent = writes == 0 ? 0 : Math.Round(100.0 * snapshot.StoreCacheHits / writes, 2),
            storeMegamorphicSites = snapshot.StoreMegamorphicSites,

            polymorphicPromotions = snapshot.PolymorphicPromotions,
            dictionaryFallbacks = snapshot.DictionaryFallbacks,
            shapeTransitions = snapshot.ShapeTransitions,
            prototypeInvalidations = snapshot.PrototypeInvalidations,
            namedPropertiesMaterializations = snapshot.NamedPropertiesMaterializations,
        };
    }
}
