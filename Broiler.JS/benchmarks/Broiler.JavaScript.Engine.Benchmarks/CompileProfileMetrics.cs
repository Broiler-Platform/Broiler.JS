using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Broiler.JavaScript.Ast;
using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.Parser;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Engine.Benchmarks;

/// <summary>
/// Sizes roadmap item 1-1 (lazy function compilation) on the real corpora it names, by
/// measuring the front end against a control in which every function body has already been
/// removed.
/// </summary>
/// <remarks>
/// <para>
/// 1-1 proposes compiling a function body on first invocation instead of at script-compile
/// time. Its prize is therefore bounded by one quantity nobody has measured: <em>how much of
/// the front end's cost is function bodies at all.</em> §3.5 — "an acceptance criterion is a
/// claim too" — says to establish that before building, because a body-deferral that saves
/// nothing is not slow, it is pointless.
/// </para>
/// <para>
/// The control is the same source with every outermost function body replaced by <c>{}</c>.
/// That is not a proposal for how the engine should behave; it is the floor an ideal
/// deferral converges to, since a deferred body contributes no expression tree and no IL.
/// Two rows are therefore reported per corpus and the difference between them is what 1-1
/// can win:
/// </para>
/// <list type="bullet">
/// <item><description><c>full</c> — parse, tree construction and IL emission over the whole
/// source, which is what the engine does today.</description></item>
/// <item><description><c>stub</c> — the same pipeline over the body-free source.</description></item>
/// </list>
/// <para>
/// The saving is <em>not</em> <c>full - stub</c>. A deferred body still has to be parsed, because
/// a syntax error inside a function that is never called is still a <c>SyntaxError</c> at
/// script-compile time (1-1's first named risk), and the stub source does not contain those
/// bodies to parse. The parse of each variant is measured separately so that cost can be added
/// back: <c>ceiling = full - stub - (parseFull - parseStub)</c>. Reporting <c>full - stub</c>
/// alone would credit 1-1 with a saving it is forbidden from taking.
/// </para>
/// <para>
/// Corpus is the suites phase 1 names — CodeLoad's two eval'd payloads (jQuery 1.7.2 and the
/// Closure library base, which is the entire benchmark: it evaluates them and calls almost
/// nothing) and Mandreel — plus PdfJS, Typescript and Box2D as the "large real program that
/// also runs" controls, where a large body-share would predict a load-time gain and a small
/// one would predict none.
/// </para>
/// </remarks>
internal static class CompileProfileMetrics
{
    public static void Write(string octaneDirectory, int repetitions, string only = null)
    {
        var corpora = LoadCorpora(octaneDirectory);
        // One corpus per process is how a comparison of two *compilers* is run, because the
        // corpora share a heap: item 1-1's deferred generation keeps an un-generated lambda's
        // tree alive, so a corpus measured after Mandreel's 5 MB pays for it in collection time
        // that has nothing to do with its own compile. Measured together, the last two corpora
        // read 1.6x and 2.6x SLOWER under deferral while the first three read 0.56-0.65x
        // faster, and the ratios were bimodal — the tell that it was ordering, not the change.
        if (!string.IsNullOrEmpty(only))
            corpora = corpora.Where(c => c.Name == only).ToList();

        var rows = new List<object>(corpora.Count);

        foreach (var corpus in corpora)
        {
            var stub = StubFunctionBodies(corpus.Source, out var outermost, out var total, out var bodyBytes, out var skipped);

            // The control is the interesting artifact when a corpus does NOT behave like the
            // others, so it is inspectable rather than internal.
            var dumpDirectory = Environment.GetEnvironmentVariable("BROILER_COMPILE_PROFILE_DUMP");
            if (!string.IsNullOrEmpty(dumpDirectory))
            {
                Directory.CreateDirectory(dumpDirectory);
                File.WriteAllText(Path.Combine(dumpDirectory, corpus.Name + "-stub.js"), stub);
            }

            // One untimed pass per variant: the first compile of the process pays for JIT of
            // the pipeline itself, which is not what this is measuring.
            Measure(() => Parse(corpus.Source));
            Measure(() => Compile(corpus.Source, corpus.Name));
            Measure(() => Parse(stub));
            Measure(() => Compile(stub, corpus.Name + "-stub"));

            var parseFull = Repeat(repetitions, () => Parse(corpus.Source));
            var parseStub = Repeat(repetitions, () => Parse(stub));
            var full = Repeat(repetitions, () => Compile(corpus.Source, corpus.Name));
            var compileStub = Repeat(repetitions, () => Compile(stub, corpus.Name + "-stub"));

            // The pre-parse 1-1 must keep: the bodies the stub source no longer contains still
            // have to be parsed for early errors, so their parse cost is not available to save.
            var preparse = parseFull.Milliseconds - parseStub.Milliseconds;
            var ceiling = full.Milliseconds - compileStub.Milliseconds - preparse;

            rows.Add(new
            {
                corpus = corpus.Name,
                note = corpus.Note,
                sourceBytes = corpus.Source.Length,
                stubBytes = stub.Length,
                functionsTotal = total,
                functionsOutermost = outermost,
                // Bodies the rewrite did not recognize and therefore left in the control. Any
                // non-zero value understates the ceiling, so it is reported rather than assumed.
                functionsNotStubbed = skipped,
                bodyByteShare = Math.Round((double)bodyBytes / corpus.Source.Length, 4),
                parseFullMs = Round(parseFull.Milliseconds),
                parseStubMs = Round(parseStub.Milliseconds),
                fullMs = Round(full.Milliseconds),
                stubMs = Round(compileStub.Milliseconds),
                preparseMs = Round(preparse),
                ceilingMs = Round(ceiling),
                ceilingShare = full.Milliseconds > 0 ? Math.Round(ceiling / full.Milliseconds, 4) : 0d,
                fullAllocMb = Round(full.AllocatedBytes / 1024d / 1024d),
                stubAllocMb = Round(compileStub.AllocatedBytes / 1024d / 1024d),
                allocCeilingShare = full.AllocatedBytes > 0
                    ? Math.Round((full.AllocatedBytes - compileStub.AllocatedBytes) / (double)full.AllocatedBytes, 4)
                    : 0d,
            });
        }

        Console.WriteLine(JsonSerializer.Serialize(
            new
            {
                metric = "compile-profile",
                repetitions,
                note = "ceilingMs = fullMs - stubMs - preparseMs; the most 1-1 can remove once the "
                    + "early-error pre-parse it must keep is charged back to it.",
                rows,
            },
            new JsonSerializerOptions { WriteIndented = true }));
    }

    private static double Round(double value) => Math.Round(value, 2);

    private sealed record Corpus(string Name, string Note, string Source);

    private readonly record struct Sample(double Milliseconds, long AllocatedBytes);

    private static AstProgram Parse(string source)
    {
        var span = new StringSpan(source);
        return new Broiler.JavaScript.Parser.FastParser(new FastTokenStream(in span)).ParseProgram();
    }

    private static JSFunctionDelegate Compile(string source, string name)
        => CoreScript.Compile(source, name + ".js", codeCache: new NoCodeCache());

    /// <summary>
    /// Allocation is read with <see cref="GC.GetTotalAllocatedBytes"/> rather than the
    /// per-thread counter every other emitter here uses, because compilation does not
    /// necessarily happen on this thread: <c>CompilationStack</c> hands any source over 512
    /// characters to a worker it sizes itself (item 1-2), and every corpus below is over it.
    /// The per-thread counter would report the handoff and none of the compile.
    /// </summary>
    private static Sample Measure(Action body)
    {
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        GC.WaitForPendingFinalizers();
        var before = GC.GetTotalAllocatedBytes(precise: true);
        var stopwatch = Stopwatch.StartNew();
        body();
        stopwatch.Stop();
        var after = GC.GetTotalAllocatedBytes(precise: true);
        return new Sample(stopwatch.Elapsed.TotalMilliseconds, after - before);
    }

    private static Sample Repeat(int repetitions, Action body)
    {
        var samples = new List<Sample>(repetitions);
        for (var i = 0; i < repetitions; i++)
            samples.Add(Measure(body));

        // Median of both columns independently — a run whose time is the median need not be the
        // run whose allocation is, and allocation here is near-deterministic anyway.
        var times = samples.Select(s => s.Milliseconds).OrderBy(v => v).ToArray();
        var bytes = samples.Select(s => s.AllocatedBytes).OrderBy(v => v).ToArray();
        return new Sample(times[times.Length / 2], bytes[bytes.Length / 2]);
    }

    private static List<Corpus> LoadCorpora(string octaneDirectory)
    {
        var corpora = new List<Corpus>();

        var (closure, jquery) = ExtractCodeLoadPayloads(octaneDirectory);
        if (closure != null)
            corpora.Add(new Corpus("codeload-closure", "CodeLoad's Closure library payload, eval'd 16x per iteration", closure));
        if (jquery != null)
            corpora.Add(new Corpus("codeload-jquery", "CodeLoad's jQuery 1.7.2 payload, eval'd 16x per iteration", jquery));

        AddFile(corpora, octaneDirectory, "mandreel.js", "mandreel", "Mandreel + MandreelLatency: machine-generated, the two worst scores in the suite");
        AddFile(corpora, octaneDirectory, "pdfjs.js", "pdfjs", "control: a large real program that also runs");
        AddFile(corpora, octaneDirectory, "typescript-compiler.js", "typescript", "control: a large real program that also runs");
        AddFile(corpora, octaneDirectory, "box2d.js", "box2d", "control: a large real program that also runs");

        return corpora;
    }

    private static void AddFile(List<Corpus> corpora, string directory, string fileName, string name, string note)
    {
        var path = Path.Combine(directory, fileName);
        if (File.Exists(path))
            corpora.Add(new Corpus(name, note, File.ReadAllText(path)));
    }

    /// <summary>
    /// Recovers the two source strings CodeLoad evaluates, by running <c>code-load.js</c> with
    /// stubs for the two harness constructors it calls at top level and reading the globals back
    /// out. Extracting them textually would mean reimplementing JavaScript string-literal
    /// escaping against a 100 KB single-line literal; evaluating the file is what the benchmark
    /// itself does.
    /// </summary>
    private static (string Closure, string JQuery) ExtractCodeLoadPayloads(string octaneDirectory)
    {
        var path = Path.Combine(octaneDirectory, "code-load.js");
        if (!File.Exists(path))
            return (null, null);

        using var context = BenchmarkContext.Create(new NoCodeCache());
        context.Eval("function BenchmarkSuite() {} function Benchmark() {}", "code-load-stubs.js", context);
        context.Eval(File.ReadAllText(path), "code-load.js", context);

        static string Read(JSContext context, string name)
        {
            var value = context[Broiler.JavaScript.Storage.KeyStrings.GetOrCreate(name)];
            return value.IsString ? value.ToString() : null;
        }

        return (Read(context, "BASE_JS"), Read(context, "JQUERY_JS"));
    }

    /// <summary>
    /// Replaces every outermost function body with an empty one, and reports how many functions
    /// the source contains and what share of its bytes are inside a body.
    /// </summary>
    /// <remarks>
    /// Only the outermost bodies are rewritten, because removing one removes everything nested
    /// in it; the spans therefore never overlap and can be spliced right-to-left.
    /// </remarks>
    private static string StubFunctionBodies(string source, out int outermost, out int total, out long bodyBytes, out int skipped)
    {
        skipped = 0;
        var program = Parse(source);

        var collector = new OutermostFunctionCollector();
        collector.Visit(program);
        var counter = new FunctionCounter();
        counter.Visit(program);

        outermost = collector.Functions.Count;
        total = counter.Count;
        bodyBytes = 0;

        var spans = new List<(int Offset, int Length, string Replacement)>(collector.Functions.Count);
        foreach (var function in collector.Functions)
        {
            var body = function.Body.Code;
            var replacement = ReplacementFor(source, function.Body, body.Offset, body.Length);
            if (replacement == null)
            {
                skipped++;
                continue;
            }

            spans.Add((body.Offset, body.Length, replacement));
            bodyBytes += body.Length;
        }

        spans.Sort((a, b) => a.Offset.CompareTo(b.Offset));

        var builder = new StringBuilder(source.Length);
        var cursor = 0;
        foreach (var (offset, length, replacement) in spans)
        {
            if (offset < cursor)
                continue;

            builder.Append(source, cursor, offset - cursor);
            builder.Append(replacement);
            cursor = offset + length;
        }

        builder.Append(source, cursor, source.Length - cursor);
        var stub = builder.ToString();

        // The control has to be valid JavaScript or it is not a control: a stub the parser
        // rejects would report the cost of failing early rather than of compiling less.
        try
        {
            Parse(stub);
        }
        catch (Exception ex)
        {
            var dump = Path.Combine(Path.GetTempPath(), "compile-profile-stub.js");
            File.WriteAllText(dump, stub);
            throw new InvalidOperationException(
                $"Stubbed control did not parse ({ex.Message}); written to {dump}", ex);
        }

        return stub;
    }

    /// <summary>
    /// The text that replaces a function body's source span, or <c>null</c> when the span is
    /// not one this rewrite recognizes.
    /// </summary>
    /// <remarks>
    /// A block body's span begins at its first <em>statement</em> and ends at its closing
    /// brace — the opening brace sits outside it — so the replacement is a bare <c>}</c> and the
    /// original <c>{</c> is what survives. An empty block has no first statement, so its span
    /// covers both braces and the replacement has to supply both. The two are told apart by
    /// looking at the span rather than by scanning backwards for the brace, because a comment
    /// containing <c>{</c> sits between the parameter list and the body often enough in real
    /// minified sources to make scanning wrong.
    /// </remarks>
    private static string ReplacementFor(string source, AstStatement body, int offset, int length)
    {
        if (length <= 0 || offset < 0 || offset + length > source.Length)
            return null;

        // A concise arrow body is an expression, so its replacement has to be one too.
        if (body is not AstBlock)
            return "0";

        if (source[offset + length - 1] != '}')
            return null;

        return source[offset] == '{' ? "{}" : "}";
    }

    /// <summary>
    /// Collects the function expressions that no other function encloses.
    /// </summary>
    /// <remarks>
    /// The three container overrides are not optional and their absence is not a lost
    /// optimization but a wrong measurement: <see cref="AstReduce"/> treats
    /// <see cref="VariableDeclarator"/>, <see cref="ObjectProperty"/> and <see cref="Case"/> as
    /// leaves for the benefit of its rewriting visitors, and <c>var f = function () {}</c> —
    /// the dominant spelling in jQuery — is a function hidden in the first of them. Without
    /// them the stub source would still contain most of its bodies and the control would
    /// quietly measure nothing. (§3.5: "a comment that says missing one here is a miscompile is
    /// a checklist".)
    /// </remarks>
    private sealed class OutermostFunctionCollector : AstReduce
    {
        public readonly List<AstFunctionExpression> Functions = [];

        protected override AstNode VisitFunctionExpression(AstFunctionExpression functionExpression)
        {
            Functions.Add(functionExpression);
            return functionExpression;
        }

        protected override VariableDeclarator VisitVariableDeclarator(VariableDeclarator declarator)
        {
            Visit(declarator.Identifier);
            if (declarator.Init != null)
                Visit(declarator.Init);
            return declarator;
        }

        protected override ObjectProperty VisitObjectProperty(ObjectProperty property)
        {
            if (property.Key != null)
                Visit(property.Key);
            if (property.Value != null)
                Visit(property.Value);
            if (property.Init != null)
                Visit(property.Init);
            return property;
        }

        protected override Case VisitCase(Case @case)
        {
            if (@case.Test != null)
                Visit(@case.Test);
            var statements = @case.Statements.GetFastEnumerator();
            while (statements.MoveNext(out var statement))
                Visit(statement);
            return @case;
        }
    }

    private sealed class FunctionCounter : AstReduce
    {
        public int Count { get; private set; }

        protected override AstNode VisitFunctionExpression(AstFunctionExpression functionExpression)
        {
            Count++;
            return base.VisitFunctionExpression(functionExpression);
        }

        protected override VariableDeclarator VisitVariableDeclarator(VariableDeclarator declarator)
        {
            Visit(declarator.Identifier);
            if (declarator.Init != null)
                Visit(declarator.Init);
            return declarator;
        }

        protected override ObjectProperty VisitObjectProperty(ObjectProperty property)
        {
            if (property.Key != null)
                Visit(property.Key);
            if (property.Value != null)
                Visit(property.Value);
            if (property.Init != null)
                Visit(property.Init);
            return property;
        }

        protected override Case VisitCase(Case @case)
        {
            if (@case.Test != null)
                Visit(@case.Test);
            var statements = @case.Statements.GetFastEnumerator();
            while (statements.MoveNext(out var statement))
                Visit(statement);
            return @case;
        }
    }
}
