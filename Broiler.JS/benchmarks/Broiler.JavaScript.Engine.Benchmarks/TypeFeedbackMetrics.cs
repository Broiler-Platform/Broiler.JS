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
    /// **Every Octane suite, not the seven this census used to run.**
    /// </summary>
    /// <remarks>
    /// <para>
    /// The original seven were the call-heavy cluster phase 4 exists for plus two arithmetic
    /// controls, and that was a defensible corpus for the question 4-1 asked. It stopped being
    /// one the moment its output started being quoted as <em>"the corpus"</em> — the phrase every
    /// phase-3 and phase-4 headline uses, over a denominator that was **7 of 15 suites** and
    /// never said so.
    /// </para>
    /// <para>
    /// <b>What the seven leave out is not a tail.</b> zlib is the suite on which this engine is
    /// furthest behind a plain managed interpreter — 12.0x behind Jint relative to Chromium,
    /// against 0.77x on DeltaBlue — and it was in no census at all. Nor were Mandreel, Gameboy,
    /// PdfJS, Typescript, CodeLoad, Splay or RegExp. A number computed over the seven describes
    /// the suites this campaign was already looking at, which is the one thing a corpus figure
    /// must not do.
    /// </para>
    /// <para>
    /// <b>Cost is why it was seven, and it is worth paying once.</b> zlib evaluates a 185 KB
    /// asm.js blob through <c>eval</c> inside its own timed function and one run takes ~35 s here,
    /// so a widened census is minutes rather than seconds. It runs off a switch nobody ships and
    /// answers a question no cheaper instrument can.
    /// </para>
    /// </remarks>
    private static readonly Suite[] Suites =
    [
        new("Richards", ["richards.js"]),
        new("DeltaBlue", ["deltablue.js"]),
        new("Crypto", ["crypto.js"]),
        new("RayTrace", ["raytrace.js"]),
        new("EarleyBoyer", ["earley-boyer.js"]),
        new("RegExp", ["regexp.js"]),
        new("Splay", ["splay.js"]),
        new("NavierStokes", ["navier-stokes.js"]),
        new("PdfJS", ["pdfjs.js"]),
        new("Mandreel", ["mandreel.js"]),
        new("Gameboy", ["gbemu-part1.js", "gbemu-part2.js"]),
        new("CodeLoad", ["code-load.js"]),
        new("Box2D", ["box2d.js"]),
        new("zlib", ["zlib.js", "zlib-data.js"]),
        new("Typescript", ["typescript.js", "typescript-input.js", "typescript-compiler.js"]),
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
        var totals = new Totals();

        // On the shell's stack, with the shell's budget. Mandreel's global_init recurses deep
        // enough to take the default one down with an UNCATCHABLE .NET stack overflow, and an
        // uncatchable abort in the ninth of fifteen suites is what a partial corpus looks like
        // from the inside.
        BenchmarkContext.RunOnScriptHostStack(() =>
        {
            foreach (var suite in Suites)
            {
                Console.Error.WriteLine($"{suite.Name}: running ...");
                var row = RunSuite(octaneDirectory, baseSource, suite, totals);
                if (row != null)
                    rows.Add(row);

                // Written after EVERY suite, not once at the end. A stack overflow cannot be
                // caught, so the only defence against one discarding the fourteen suites that did
                // work is to have already emitted them — which the first widened run of this
                // census learned by losing nine.
                WriteReport(rows, totals, partial: true);
            }
        });

        WriteReport(rows, totals, partial: false);
    }

    private static void WriteReport(List<object> rows, Totals totals, bool partial)
    {
        var json = JsonSerializer.Serialize(
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

                // The corpus aggregate, EMITTED rather than left to be totalled by hand. The
                // roadmap's "93.54% of 37.9 M reads and 96.70% of 4.24 M calls" was added up
                // outside the instrument, over the seven suites this census used to run, and then
                // quoted as "the corpus" — so the denominator was invisible at the point of use.
                // Reporting suiteCount beside the shares is what makes a partial corpus say so.
                corpus = new
                {
                    suiteCount = totals.Suites,
                    expectedSuiteCount = Suites.Length,
                    complete = totals.Suites == Suites.Length,
                    propertyReads = totals.DescribeReads(),
                    calls = totals.DescribeCalls(),
                },
            },
            new JsonSerializerOptions { WriteIndented = true });

        if (partial)
            File.WriteAllText(PartialPath, json);
        else
            Console.WriteLine(json);
    }

    /// <summary>
    /// Where an in-progress census parks its rows so an abort cannot discard them.
    /// </summary>
    internal static string PartialPath { get; } =
        Path.Combine(Path.GetTempPath(), "broiler-type-feedback-partial.json");

    private static object RunSuite(string octaneDirectory, string baseSource, Suite suite, Totals totals)
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

        using var context = BenchmarkContext.Create(scriptHostStackBudget: true);

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
        totals.Add(properties, calls);

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

    /// <summary>
    /// Corpus-wide sums, so the headline share is emitted by the instrument rather than added up
    /// by whoever quotes it.
    /// </summary>
    /// <remarks>
    /// A hand-totalled aggregate carries no record of what it was totalled over, which is how
    /// "93.54% of the corpus's reads" came to describe seven suites of fifteen in a document that
    /// uses "the corpus" for both. <see cref="Suites"/> is the denominator and
    /// <c>suiteCount</c> ships beside the shares.
    /// </remarks>
    private sealed class Totals
    {
        public int Suites { get; private set; }

        private long readObservations, readMonomorphic, readPolymorphic, readMegamorphic;
        private long callObservations, callMonomorphic, callPolymorphic, callMegamorphic;

        public void Add(TypeFeedback.Distribution reads, TypeFeedback.Distribution calls)
        {
            Suites++;
            readObservations += reads.Observations;
            readMonomorphic += reads.MonomorphicObservations;
            readPolymorphic += reads.PolymorphicObservations;
            readMegamorphic += reads.MegamorphicObservations;
            callObservations += calls.Observations;
            callMonomorphic += calls.MonomorphicObservations;
            callPolymorphic += calls.PolymorphicObservations;
            callMegamorphic += calls.MegamorphicObservations;
        }

        public object DescribeReads() => Describe(
            readObservations, readMonomorphic, readPolymorphic, readMegamorphic);

        public object DescribeCalls() => Describe(
            callObservations, callMonomorphic, callPolymorphic, callMegamorphic);

        private static object Describe(long total, long mono, long poly, long mega) => new
        {
            observations = total,
            monomorphicObservations = mono,
            polymorphicObservations = poly,
            megamorphicObservations = mega,
            monomorphicObservationPercent = total == 0 ? 0d : Math.Round(100.0 * mono / total, 2),
        };
    }
}
