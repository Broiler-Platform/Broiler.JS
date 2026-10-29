using System.Collections.Concurrent;
using System.Globalization;
using Broiler.ECMAScript.Conformance;
using Broiler.ECMAScript.Runners;
using Broiler.JavaScript.Vectors.Tests.Runners;

namespace Broiler.JavaScript.Vectors.Tests;

/// <summary>Broiler.JS's engine-level run of the corpus, under bundle sjl-1-001's decision rule.</summary>
public class EngineRunTests
{
    /// <summary>When set, the run writes its report there: an absolute path, or one relative to the repository.</summary>
    public const string ReportVariable = "BROILER_VECTORS_REPORT";

    /// <summary>
    /// How many evaluating processes the corpus test runs its vectors in: a whole number above 1, or
    /// one process when unset or anything else (<see cref="CorpusRun"/>). The report is one process's,
    /// byte for byte, unless a vector finds no completion in that one process's run, which several
    /// processes run again alone and keep what it gives there. Retained evidence runs use one, with the
    /// variable unset; CI's engine vectors job uses 4.
    /// </summary>
    public const string WorkersVariable = "BROILER_VECTORS_WORKERS";

    /// <summary>The trait the corpus test carries, so CI can run it in a job of its own and leave it out of the others.</summary>
    public const string CorpusCategory = "EngineCorpus";

    private static readonly BroilerJsEngine Engine = new();

    private readonly ITestOutputHelper output;

    public EngineRunTests(ITestOutputHelper output) => this.output = output;

    /// <summary>
    /// Every vector has a result, and every difference is classified. A difference nothing
    /// classifies fails the run: an engine defect becomes a <c>divergence.broiler-js</c> field in
    /// a new corpus revision, a vector defect is fixed in one, and nothing is ever deleted.
    /// </summary>
    [Fact]
    [Trait("Category", CorpusCategory)]
    public void Every_difference_from_the_corpus_is_classified()
    {
        var corpus = Home.Corpus;
        Assert.Empty(corpus.Diagnostics);
        var vectors = corpus.Records.ToList();
        var workers = CorpusRun.WorkersFrom(Environment.GetEnvironmentVariable(WorkersVariable));
        var run = CorpusRun.Run(vectors, Home.Prelude, workers, Engine);
        output.WriteLine(run.Summary());
        // Also on standard error, which dotnet test prints at normal verbosity, where it leaves out a
        // passing test's output: a collector's log then shows N and the vectors re-checked.
        Console.Error.WriteLine(run.Summary());
        var results = run.Results;
        WriteReport(results, corpus);

        Assert.Equal(vectors.Count, results.Count);
        var unclassified = results.Where(r => r.Verdict == Verdict.Difference).ToList();
        Assert.True(
            unclassified.Count == 0,
            $"{unclassified.Count} of {results.Count} vectors differ and nothing classifies them:\n"
            + string.Join("\n", unclassified.Take(25).Select(r => $"{r.Vector.Id}: expected {r.Expected}, rendered {r.Rendered}")));
    }

    /// <summary>
    /// The negative control: one vector's expected value is mutated in a copy of the corpus, and the
    /// runner, given that copy, reports exactly that vector as an unclassified difference.
    /// </summary>
    [Fact]
    public void The_negative_control_is_reported_as_an_unclassified_difference()
    {
        const string file = "numbers/to-fixed.vectors";
        var original = Home.Corpus.Files.Single(f => f.Path == file).Records;
        var before = VectorRun.RunEngine(original, Home.Prelude, Engine);
        var target = before.First(r => r.Verdict == Verdict.Match && r.Vector.Class == "normative").Vector;

        var copy = Directory.CreateTempSubdirectory("broiler-vectors-control-");
        try
        {
            CopyDirectory(Home.Root, copy.FullName);
            var path = Path.Combine(copy.FullName, file);
            var text = File.ReadAllText(path);
            var line = $"out: {target.Out}\n";
            var start = text.IndexOf($"id: {target.Id}\n", StringComparison.Ordinal);
            var at = text.IndexOf(line, start, StringComparison.Ordinal);
            File.WriteAllText(path, text[..at] + $"out: {Mutate(target.Out)}\n" + text[(at + line.Length)..]);

            var mutated = CorpusDirectory.Load(copy.FullName);
            Assert.Empty(mutated.Diagnostics);
            var after = VectorRun.RunEngine(mutated.Files.Single(f => f.Path == file).Records, CorpusDirectory.Prelude(copy.FullName), Engine);

            WriteReport(after, mutated, ".negative-control.tsv");

            var changed = after.Zip(before).Where(pair => pair.First.Verdict != pair.Second.Verdict).Select(pair => pair.First).ToList();
            var control = Assert.Single(changed);
            Assert.Equal(target.Id, control.Vector.Id);
            Assert.Equal(Verdict.Difference, control.Verdict);
        }
        finally
        {
            copy.Delete(recursive: true);
        }
    }

    [Fact]
    public void The_renderer_reads_values_errors_and_parse_failures_the_schema_s_way()
    {
        const string prelude = "";
        Assert.Equal("value: f64:0x8000000000000000", Engine.Evaluate(prelude, "-0"));
        Assert.Equal("value: f64:0x7FF8000000000000", Engine.Evaluate(prelude, "NaN"));
        Assert.Equal("value: str:\"a" + '\\' + "u000Ab\"", Engine.Evaluate(prelude, "'a\\nb'"));
        Assert.Equal("value: bigint:-12345678901234567890123", Engine.Evaluate(prelude, "-12345678901234567890123n"));
        Assert.Equal("value: bool:false", Engine.Evaluate(prelude, "1 > 2"));
        Assert.Equal("value: undefined", Engine.Evaluate(prelude, "void 0"));
        Assert.Equal("value: null", Engine.Evaluate(prelude, "null"));
        Assert.Equal("value: object", Engine.Evaluate(prelude, "[]"));
        Assert.Equal("value: object", Engine.Evaluate(prelude, "(function () {})"));
        Assert.Equal("value: symbol", Engine.Evaluate(prelude, "Symbol()"));
        Assert.Equal("error: RangeError", Engine.Evaluate(prelude, "(1).toFixed(101)"));
        Assert.Equal("error: SyntaxError", Engine.Evaluate(prelude, "1__0"));
        Assert.Equal("throw: f64:0x4000000000000000", Engine.Evaluate(prelude, "throw 2"));
        Assert.Equal("throw: object", Engine.Evaluate(prelude, "throw {}"));
        Assert.Equal("abort: no completion within 5 s", Engine.Evaluate(prelude, "for (;;) {}"));
    }

    [Fact]
    public void Each_vector_runs_in_a_fresh_realm()
    {
        Assert.Equal("value: undefined", Engine.Evaluate("", "globalThis.leak = 1; undefined"));
        Assert.Equal("value: str:\"undefined\"", Engine.Evaluate("", "typeof leak"));
    }

    /// <summary>
    /// Several processes give one process's results in the list's order, and a vector that finds no
    /// completion in the parallel pass is run again alone, where one that never completes still aborts.
    /// </summary>
    [Fact]
    public void Several_processes_keep_the_order_and_re_check_a_vector_that_found_no_completion()
    {
        var records = Home.Corpus.Files.Single(f => f.Path == "numbers/to-fixed.vectors").Records.Take(40).ToList();
        var serial = VectorRun.RunEngine(records, Home.Prelude, Engine);
        const int looping = 17;
        var vectors = records.Select((record, i) => i == looping ? record.With("script", "for (;;) {}") : record).ToList();

        var run = CorpusRun.Run(vectors, Home.Prelude, workers: 3, Engine, blockSize: 4);

        Assert.Equal(3, run.Workers);
        Assert.Equal(vectors.Select(v => v.Id), run.Results.Select(r => r.Vector.Id));
        for (var i = 0; i < vectors.Count; i++)
        {
            if (i != looping)
                Assert.Equal(serial[i].Rendered, run.Results[i].Rendered);
        }

        Assert.Equal(BroilerJsEngine.TimeoutOutcome, run.Results[looping].Rendered);
        Assert.Contains(vectors[looping].Id, run.Rechecked);
        Assert.Equal(vectors[looping].Id, run.SlowestId);
    }

    /// <summary>
    /// A vector that finds no completion in the parallel pass but completes alone keeps what it gives
    /// alone, in its own place, and is named among those re-checked; every other vector is evaluated
    /// once. A stand-in engine finds no completion for one vector the first time it is asked.
    /// </summary>
    [Fact]
    public void A_vector_re_checked_alone_keeps_what_it_gives_alone_in_its_own_place()
    {
        var records = Home.Corpus.Files.Single(f => f.Path == "numbers/to-fixed.vectors").Records.Take(40).ToList();
        // A script of its own for each vector, so the stand-in can tell them apart.
        var vectors = records.Select((record, i) => record.With("script", "vector " + i.ToString(CultureInfo.InvariantCulture))).ToList();
        var answers = vectors.ToDictionary(v => v.Script, v => v.Out);
        const int late = 17;
        var asked = new ConcurrentDictionary<string, int>();
        var created = 0;
        ITimedVectorEngine Create()
        {
            Interlocked.Increment(ref created);
            return new StandInEngine(answers, vectors[late].Script, asked);
        }

        var run = CorpusRun.Run(vectors, Home.Prelude, workers: 3, new StandInEngine(answers, vectors[late].Script, asked), Create, blockSize: 4);

        Assert.Equal(vectors.Select(v => v.Id), run.Results.Select(r => r.Vector.Id));
        for (var i = 0; i < vectors.Count; i++)
        {
            Assert.Equal(answers[vectors[i].Script], run.Results[i].Rendered);
            Assert.Equal(Verdict.Match, run.Results[i].Verdict);
            Assert.Equal(i == late ? 2 : 1, asked[vectors[i].Script]);
        }

        Assert.Equal(vectors[late].Id, Assert.Single(run.Rechecked));
        Assert.Equal(4, created);
    }

    /// <summary>A different expected value of the same kind: the last bit of a double, or a character of a string.</summary>
    private static string Mutate(string outcome)
    {
        Assert.True(Outcome.TryParseAlternatives(outcome, out var alternatives, out _, out _));
        var value = Assert.Single(alternatives).Value!;
        return value.Kind switch
        {
            TypedValueKind.Number => "value: " + TypedValue.RenderNumber(BitConverter.UInt64BitsToDouble(value.Bits ^ 1)),
            TypedValueKind.String => "value: " + TypedValue.RenderString(value.Chars + "0"),
            _ => throw new InvalidOperationException($"no mutation for {outcome}"),
        };
    }

    private static void CopyDirectory(string source, string destination)
    {
        foreach (var directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(Path.Combine(destination, Path.GetRelativePath(source, directory)));
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
            File.Copy(file, Path.Combine(destination, Path.GetRelativePath(source, file)));
    }

    /// <summary>
    /// An engine that answers each script with the outcome its vector expects, and finds no completion
    /// for <paramref name="late"/> the first time any engine sharing <paramref name="asked"/> is asked it.
    /// </summary>
    private sealed class StandInEngine(IReadOnlyDictionary<string, string> answers, string late, ConcurrentDictionary<string, int> asked) : ITimedVectorEngine
    {
        public string Name => "broiler-js";

        public TimeSpan LastEvaluation { get; private set; }

        public string Evaluate(string prelude, string script)
        {
            var times = asked.AddOrUpdate(script, 1, static (_, n) => n + 1);
            LastEvaluation = TimeSpan.FromMilliseconds(times);
            return script == late && times == 1 ? BroilerJsEngine.TimeoutOutcome : answers[script];
        }

        public void Dispose()
        {
        }
    }

    /// <summary>
    /// Writes a report where <see cref="ReportVariable"/> says; the negative control's goes beside it,
    /// its extension replaced by <paramref name="suffix"/>.
    /// </summary>
    private static void WriteReport(IReadOnlyList<VectorResult> results, VectorCorpus corpus, string? suffix = null)
    {
        var target = Environment.GetEnvironmentVariable(ReportVariable);
        if (string.IsNullOrEmpty(target))
            return;
        var repository = Path.GetFullPath(Path.Combine(Home.Root, "..", ".."));
        var path = Path.IsPathRooted(target) ? target : Path.Combine(repository, target);
        if (suffix is not null)
            path = Path.ChangeExtension(path, null) + suffix;
        File.WriteAllText(path, VectorRun.Report(results, Engine.Name, corpus.Sources));
    }
}
