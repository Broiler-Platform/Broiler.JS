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

    /// <summary>
    /// What a call costs and what perfect inlining of it would save — item 4-4's premise, measured
    /// in this engine rather than carried over from item 2-6's older probe.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Inlining removes the call's prologue and epilogue: the five <c>using</c> scopes, the
    /// <c>Arguments</c> construction, the frame, the delegate dispatch, and the boxing of the
    /// argument and the return — 2-6 established that the cost is there and <em>not</em> in
    /// resolving the callee. The upper bound on what inlining can buy is therefore the difference
    /// between calling a small callee and writing its body out by hand, with the callee's own work
    /// held identical across the two.
    /// </para>
    /// <para>
    /// The hand-inlined arm is a <em>control</em>, not a proposal: it is what a perfect inliner
    /// would produce, so the gap is the whole prize before any of it is lost to guards, to callees
    /// too large to inline, or to sites a tier-2 recompile never reaches.
    /// </para>
    /// </remarks>
    internal static void WriteCallProbe(int iterations, int repetitions)
    {
        var shapes = new (string Name, string Source)[]
        {
            // A plain call to a one-expression global function.
            ("plain-call",
                "function callee(x) { return x + 1; }\n" +
                "function hot(n) { var s = 0; for (var i = 0; i < n; i++) { s = s + callee(i); } return s; }"),
            ("plain-inlined",
                "function hot(n) { var s = 0; for (var i = 0; i < n; i++) { s = s + (i + 1); } return s; }"),

            // A method on a prototype, which is the shape Richards and DeltaBlue are built from.
            ("method-call",
                "function Box(k) { this.k = k; }\nBox.prototype.add = function (x) { return x + this.k; };\n" +
                "var box = new Box(1);\n" +
                "function hot(n) { var s = 0; for (var i = 0; i < n; i++) { s = s + box.add(i); } return s; }"),
            ("method-inlined",
                "function Box(k) { this.k = k; }\nvar box = new Box(1);\n" +
                "function hot(n) { var s = 0; for (var i = 0; i < n; i++) { s = s + (i + box.k); } return s; }"),

            // The floor: the same loop with neither a call nor a property read.
            ("no-call-control",
                "function hot(n) { var s = 0; for (var i = 0; i < n; i++) { s = s + (i + 1); } return s; }"),

            // The same loop with the bound a LITERAL, so every local in it is provably numeric
            // (item 3-3) and nothing in the body touches a JSValue. A parameter is the one
            // category that cannot reach the numeric tier, so `i < n` in the control compares a
            // raw double against a JSValue — and copying the parameter into a local does not
            // help, because the local inherits its unknown type. This is what says whether the
            // control's per-iteration allocation is the bound or the arithmetic itself.
            ("no-call-literal-bound",
                "function hot(n) { var s = 0; for (var i = 0; i < {ITER}; i++) { s = s + (i + 1); } return s; }"),

            // A cached property read in the SAME loop and the same run set, so the read path and
            // the call path can be compared against one control instead of across two probes.
            // This is what turns "where does Octane's time go" from two separate upper bounds into
            // one arithmetic statement.
            ("property-read",
                "var o = { x: 1 };\n" +
                "function hot(n) { var s = 0; for (var i = 0; i < n; i++) { s = s + o.x; } return s; }"),

            // Where inside the prologue the cost is. Item 2-6 ruled out the five `using` scopes
            // (removing all of them moved a call loop by a single-digit percentage) and named
            // `Arguments`, the frame, the dispatch and the boxing as the remainder — but did not
            // separate them. Argument count is the one knob that moves `Arguments` and the
            // per-argument boxing while leaving the frame, the scopes and the dispatch fixed, so
            // the slope across these four IS the per-argument cost and the intercept is
            // everything a zero-argument call still pays.
            ("call-0-args",
                "function callee() { return 1; }\n" +
                "function hot(n) { var s = 0; for (var i = 0; i < n; i++) { s = s + callee(); } return s; }"),
            ("call-1-arg",
                "function callee(a) { return 1; }\n" +
                "function hot(n) { var s = 0; for (var i = 0; i < n; i++) { s = s + callee(i); } return s; }"),
            ("call-2-args",
                "function callee(a, b) { return 1; }\n" +
                "function hot(n) { var s = 0; for (var i = 0; i < n; i++) { s = s + callee(i, i); } return s; }"),
            ("call-3-args",
                "function callee(a, b, c) { return 1; }\n" +
                "function hot(n) { var s = 0; for (var i = 0; i < n; i++) { s = s + callee(i, i, i); } return s; }"),

            // WHICH prologue. A call to a NATIVE builtin takes the same JSFunction.InvokeFunction
            // entry as a call to a JavaScript function — the same five `using` scopes, the same
            // executing-function bookkeeping, the same dispatch — but its delegate is a C# method,
            // so it pays none of the prologue a COMPILED JAVASCRIPT BODY emits for itself: the
            // context capture and the CallStackItem frame push. The difference between these and
            // the matching `call-N-args` shape is therefore the compiled-body half, and what is
            // left is the InvokeFunction half. Item 4-5 has to know which half it is fixing, and
            // this is the only split that needs no engine change to measure.
            //
            // Each native does a little work of its own, so the InvokeFunction half comes out
            // slightly high and the compiled-body half slightly low. Stated rather than corrected.
            ("native-call-1-arg",
                "function hot(n) { var s = 0; for (var i = 0; i < n; i++) { s = s + Math.abs(i); } return s; }"),
            ("native-call-2-args",
                "function hot(n) { var s = 0; for (var i = 0; i < n; i++) { s = s + Math.max(i, 0); } return s; }"),
        };

        var rows = new List<object>();
        for (var repetition = 0; repetition < repetitions; repetition++)
        {
            // Rotate the order so a drift in the machine does not land on one shape.
            for (var offset = 0; offset < shapes.Length; offset++)
            {
                var shape = shapes[(offset + repetition) % shapes.Length];

                TypeFeedback.Enabled = false;
                PropertyOptimizationDiagnostics.Enabled = false;
                CallPathDiagnostics.Enabled = false;

                using var context = BenchmarkContext.Create();
                var source = shape.Source.Replace("{ITER}", iterations.ToString());
                context.Eval(source + "\nhot(1000); hot(1000); hot(1000);", "probe.js");

                // Bytes are deterministic where the wall clock is not, and a call boundary boxes:
                // an argument goes in as a JSValue and a result comes back as one, while the
                // control loop's locals are raw doubles (item 3-3). So this says how much of a
                // call's cost is the value representation rather than the prologue — which is a
                // question about phase 3, and one no timing arm can answer on its own.
                var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
                var stopwatch = Stopwatch.StartNew();
                var answer = context.Eval($"hot({iterations});", "run.js").ToString();
                stopwatch.Stop();
                var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

                rows.Add(new
                {
                    shape = shape.Name,
                    repetition,
                    elapsedMs = stopwatch.Elapsed.TotalMilliseconds,
                    nsPerIteration = stopwatch.Elapsed.TotalMilliseconds * 1_000_000 / iterations,
                    bytesPerIteration = (double)allocated / iterations,
                    answer,
                });
            }
        }

        Console.WriteLine(JsonSerializer.Serialize(
            new
            {
                schema = "broiler.inlining-call-probe/1",
                iterations,
                note = "What a call costs and what perfect inlining would save. Each '-inlined' arm "
                    + "writes the callee's body out by hand with its work held identical, so the "
                    + "difference from the matching call arm is the entire prize inlining could "
                    + "win — before guards, callee size limits, or the share of calls a tier-2 "
                    + "recompile can reach.",
                runs = rows,
            },
            new JsonSerializerOptions { WriteIndented = true }));
    }

    /// <summary>Names the waterfall's buckets so the JSON is readable without the enum.</summary>
    private static object DescribeDropCauses(long[] counts)
    {
        var names = System.Enum.GetNames(typeof(Broiler.JavaScript.Compiler.NumericDropCause));
        var described = new Dictionary<string, long>(names.Length);
        for (var i = 0; i < names.Length && i < counts.Length; i++)
            described[names[i]] = counts[i];
        return described;
    }

    private static object DescribeUpdateTargets(long[] counts)
        => Describe<ArithmeticOperandDiagnostics.UpdateTarget>(counts);

    private static object DescribeNumericTreeRefusals(long[] counts)
        => Describe<Broiler.JavaScript.Compiler.NumericTreeRefusal>(counts);

    private static object DescribeNumericTreeOrderBlockers(long[] counts)
        => Describe<Broiler.JavaScript.Compiler.NumericTreeOrderBlocker>(counts);

    private static Dictionary<string, long> Describe<TEnum>(long[] counts)
        where TEnum : struct, System.Enum
    {
        var names = System.Enum.GetNames(typeof(TEnum));
        var described = new Dictionary<string, long>(names.Length);
        for (var i = 0; i < names.Length && i < counts.Length; i++)
            described[names[i]] = counts[i];
        return described;
    }

    private static object DescribeRejections(long[] counts)
    {
        var names = System.Enum.GetNames(typeof(Broiler.JavaScript.Compiler.NumericLocalRejection));
        var described = new Dictionary<string, long>(names.Length);
        for (var i = 0; i < names.Length && i < counts.Length; i++)
            described[names[i]] = counts[i];
        return described;
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
        Broiler.JavaScript.Compiler.CompilerSpecializationDiagnostics.Reset();

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
        CallPathDiagnostics.Enabled = counters;
        CallPathDiagnostics.Reset();

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

        // A COMPILE-time counter, so it goes on before the corpus is compiled below rather than
        // beside the run-time censuses further down — which is where it went first, and why it
        // read zero on all seven suites (item 3-1's `0083` failure mode, a second time).
        Broiler.JavaScript.Compiler.SpeculativeNumericLocals.Counting = counters;
        // Item 3-9's population, on the same terms and for the same reason.
        Broiler.JavaScript.Compiler.ImportedOuterNumerics.Counting = counters;

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

        // Allocation over the whole driver run. Deterministic where the wall clock is not, and
        // the direct corpus-level reading of item 3-5: a box per loop iteration removed shows up
        // here exactly, whether or not it is visible in the time.
        // Item 3-8: how much of this run's allocation is number boxing at all — the ceiling on
        // every raw-double item in phase 3, counted rather than inferred from a per-shape figure.
        //
        // Gated on `counters` for the same reason the inline-cache ones above are, and here it is
        // not merely noise: the arithmetic census increments once per generic invocation, and the
        // two arms of item 3-1's switch differ by 20.5 M invocations. Leaving it on for a timing
        // pass would charge the slower arm for 20.5 M interlocked increments it does not otherwise
        // pay — a bias pointing the same way as the result, which is the worst kind.
        Broiler.JavaScript.BuiltIns.Number.NumberBoxingDiagnostics.Reset();
        Broiler.JavaScript.BuiltIns.Number.NumberBoxingDiagnostics.Enabled = counters;
        // Item 3-1's shared half: of the operators that mint those boxes, how many are handed two
        // values that ARE Numbers — i.e. how many a native form guarded on that test could reach.
        ArithmeticOperandDiagnostics.Reset();
        ArithmeticOperandDiagnostics.Enabled = counters;
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();

        // The denominator phase 3 has never had. Every item in it is priced in boxes removed, and
        // the exchange rate into wall clock has come out at roughly a sixth of that share three
        // times running with no explanation. GC.GetTotalPauseDuration is exact rather than sampled
        // — it is the runtime's own accounting of how long execution was suspended for collection
        // — so it says directly what share of a suite an allocation item is even bidding for.
        // Collection counts are taken with it because pause time alone cannot distinguish "many
        // cheap gen0s" from "a few expensive gen2s", and those want opposite follow-ups.
        var pauseBefore = GC.GetTotalPauseDuration();
        var gen0Before = GC.CollectionCount(0);
        var gen1Before = GC.CollectionCount(1);
        var gen2Before = GC.CollectionCount(2);
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
        var gcPauseMs = (GC.GetTotalPauseDuration() - pauseBefore).TotalMilliseconds;
        var gen0Collections = GC.CollectionCount(0) - gen0Before;
        var gen1Collections = GC.CollectionCount(1) - gen1Before;
        var gen2Collections = GC.CollectionCount(2) - gen2Before;
        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        var boxing = Broiler.JavaScript.BuiltIns.Number.NumberBoxingDiagnostics.Snapshot();
        Broiler.JavaScript.BuiltIns.Number.NumberBoxingDiagnostics.Enabled = false;
        var arithmeticGeneric = ArithmeticOperandDiagnostics.Generic;
        var arithmeticBothNumbers = ArithmeticOperandDiagnostics.BothNumbers;
        var arithmeticRawDouble = ArithmeticOperandDiagnostics.RawDoubleOperand;
        var arithmeticRawDoubleOtherNumber = ArithmeticOperandDiagnostics.RawDoubleOtherNumber;
        var arithmeticUnaryNegate = ArithmeticOperandDiagnostics.UnaryNegate;
        var arithmeticUnaryUpdate = ArithmeticOperandDiagnostics.UnaryUpdate;
        var arithmeticUpdateTargets = ArithmeticOperandDiagnostics.UpdateTargets;
        var arithmeticUnaryToNumeric = ArithmeticOperandDiagnostics.UnaryToNumeric;
        var arithmeticUnaryToNumericReused = ArithmeticOperandDiagnostics.UnaryToNumericReused;
        ArithmeticOperandDiagnostics.Enabled = false;

        var compiler = Broiler.JavaScript.Compiler.CompilerSpecializationDiagnostics.Snapshot();
        var tiering = context.FunctionTiering.Snapshot();
        var speculation = Speculation.Snapshot();
        var properties = PropertyOptimizationDiagnostics.Snapshot();
        var callPath = CallPathDiagnostics.Snapshot();
        var split = outcome.Split('|', 2);

        return new
        {
            suite = suite.Name,
            loaded = true,
            benchmarksRun = int.TryParse(split[0], out var ran) ? ran : 0,
            failures = split.Length > 1 && split[1].Length > 0 ? split[1] : null,
            elapsedMs = stopwatch.ElapsedMilliseconds,
            allocatedBytes,

            // How much of elapsedMs the runtime spent with execution suspended for collection,
            // from its own accounting rather than from a sampled profile. This is the ceiling on
            // every allocation item in phase 3 at once: a change that removed EVERY box could not
            // buy back more than this from the collector, and three items have now measured an
            // exchange rate into wall clock that this is the missing half of the explanation for.
            gcPauseMs,
            gen0Collections,
            gen1Collections,
            gen2Collections,

            // Item 3-5's shape, counted at compile time: how many relational comparisons had one
            // operand already unboxed, against how many had neither — and how few locals reach the
            // numeric tier at all, which is what bounds the first number.
            scalarLocals = compiler.ScalarLocals,
            numericLocals = compiler.NumericLocals,
            mixedComparisons = compiler.MixedNumericComparisons,
            boxedComparisons = compiler.BoxedNumericComparisons,

            // Item 3-1's guarded numeric tree: how many arithmetic trees took the speculative
            // form, and how many leaf type tests that cost in total. Read against
            // arithmeticGeneric below — the trees are compile-time sites, the invocations are
            // run-time, and it is the second that has to collapse for this to have worked.
            speculativeNumericTrees = compiler.SpeculativeNumericTrees,
            speculativeNumericGuards = compiler.SpeculativeNumericGuards,

            // Item 3-1's refusal waterfall: every candidate arithmetic node attributed to the
            // first eligibility condition it fails. speculativeNumericTrees says how many sites
            // took the guarded form; this says what the OTHER ones ran into, which is the number
            // that decides whether the gap to the census's 86.6% ceiling is one rule or five.
            numericTreeRefusals = DescribeNumericTreeRefusals(compiler.NumericTreeRefusals),

            // Read only against the OrderUnsafe row above, which is its total: what kind of leaf
            // sat after the first coercion. An element read says the rule is refusing array-
            // resident arithmetic, which is exactly the population phase 3 keeps failing to reach.
            numericTreeOrderBlockers = DescribeNumericTreeOrderBlockers(compiler.NumericTreeOrderBlockers),

            // Item 3-6: the waterfall. Every hoisted name attributed to the first conjunct of
            // the numeric-local gate it fails, which is what says WHICH condition costs the
            // coverage item 3-5 measured at 5.0%.
            hoistedNames = compiler.HoistedNames,
            numericRejections = DescribeRejections(compiler.NumericRejections),
            numericCandidatesOffered = compiler.NumericCandidatesOffered,
            numericCandidatesRejected = compiler.NumericCandidatesRejected,
            numericCandidatesDropped = compiler.NumericCandidatesDropped,
            numericCandidatesSurviving = compiler.NumericCandidatesSurviving,

            // Item 3-8: what defeated the proof for each name the fixed point dropped. The
            // premise it was specified from reads all 1 916 as one population wanting one
            // runtime guard; these counts are what says whether that is true.
            numericDropCauses = DescribeDropCauses(compiler.NumericDropCauses),

            // Item 3-8a: how many locals would be numeric if a name from outside their function
            // were known to hold one. Read against numericLocals — it is what the one conjunct
            // NumericLocalDefeatTests isolated is costing.
            speculativeNumericCandidates = compiler.SpeculativeNumericCandidates,
            speculativeNumericLocalsEmitted = compiler.SpeculativeNumericLocalsEmitted,

            // Item 3-9: of those, the ones an ENCLOSING scope has already PROVED numeric, so no
            // run-time test is needed and the local becomes an ordinary raw double. Bounded above
            // by speculativeNumericCandidates by construction — a reading above it is a defect in
            // the counter, not a discovery — and the gap between the two is what only a run-time
            // guard could ever reach.
            importedOuterNumericCandidates = compiler.ImportedOuterNumericCandidates,
            importedOuterNumericOffers = compiler.ImportedOuterNumericOffers,

            // The ceiling on all of phase 3: a raw double can only ever remove a box, so
            // boxesAllocated x 24 B is every byte the whole family could take.
            boxingRequests = boxing.Requests,
            boxesCached = boxing.CacheHits,
            boxesAllocated = boxing.FactoryAllocations,
            boxesAllocatedTotal = boxing.Allocations,
            boxesAllocatedDirect = boxing.DirectAllocations,
            boxingLiteralRequests = boxing.LiteralRequests,
            // Item 3-1: of the boxes that remain after the guarded tree, how many the COMPILER
            // mints to carry a raw double across into a JSValue — the root of a tree on its way
            // into a local, a slot or an element. Only these are what a typed backing store could
            // remove; the rest an operator or a builtin produced.
            boxingConversionRequests = boxing.ConversionRequests,
            // Item 3-8a: of those, the ones minted READING a speculative local, i.e. what the dual
            // representation still costs. Read it against the fall in arithmeticUpdateTargets'
            // LocalSlot row, which is what the representation buys: the item pays exactly while
            // the second number exceeds the first.
            boxingSpeculativeReadRequests = boxing.SpeculativeReadRequests,

            // Item 3-1's shared half, counted on the far side of the boundary: not what the
            // compiler could prove, but what the operators were actually handed. arithmeticGeneric
            // is every invocation of a two-JSValue arithmetic or bitwise operator;
            // arithmeticBothNumbers is the subset a native form guarded on "both are Numbers"
            // could answer. arithmeticRawDouble is the shape item 3-5 specialized for < and > and
            // no arithmetic operator has — one side already an unboxed double, the other a JSValue.
            arithmeticGeneric,
            arithmeticBothNumbers,
            arithmeticRawDouble,
            arithmeticRawDoubleOtherNumber,
            // The unary operators, which the binary census above cannot see and which mint through
            // the same factory: -x and ~x, the ++/-- step, and the ToNumeric that re-boxes the
            // operand of ++/-- to hand back the old value. Together with the binary count these
            // name every operator box; what is left over a builtin minted directly.
            arithmeticUnaryNegate,
            arithmeticUnaryUpdate,

            // Item 3-1: where each of those steps' operands lived. The rows sum to
            // arithmeticUnaryUpdate by construction — the total is recorded by Increment itself
            // and the rows by the overload the compiler calls — so a call site the emitter forgot
            // shows up as a shortfall rather than disappearing. These are REQUESTS: multiply by
            // the suite's own request-to-allocation ratio before reading them as memory, since the
            // small-integer cache answers Crypto's updates for free and NavierStokes' barely at all.
            arithmeticUpdateTargets = DescribeUpdateTargets(arithmeticUpdateTargets),
            arithmeticUnaryToNumeric,
            // The coercions handed back instead of copied. This plus arithmeticUnaryToNumeric is
            // the coercion count and is invariant across the two settings of
            // BROILER_JS_NUMERIC_UPDATE_REUSE, so the split measures the switch and not the run.
            arithmeticUnaryToNumericReused,

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

            // Item 3-2's population: of the reads the inline cache answers, how many hand back a
            // number. A shape slot holding a raw double would serve exactly these without a box.
            cacheHitsNumeric = properties.CacheHitsNumeric,

            // Item 4-4's surface: every JavaScript call, and the share of them made FROM a
            // promoted function — the only calls a tier-2 recompile could ever inline.
            calls = callPath.Calls,
            callbackCalls = callPath.CallbackCalls,
            userCalls = callPath.UserCalls,
            userCallsFromPromoted = callPath.UserCallsFromPromoted,
        };
    }
}
