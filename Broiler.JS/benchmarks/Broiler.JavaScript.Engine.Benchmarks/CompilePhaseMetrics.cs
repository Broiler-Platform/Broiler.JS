using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using Broiler.JavaScript.Ast;
using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.ExpressionCompiler.Runtime;
using Broiler.JavaScript.Parser;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Engine.Benchmarks;

/// <summary>
/// Splits the front end's cost three ways — parse, expression-tree construction, IL emission —
/// on the six real corpora, and attributes each phase to function bodies by differencing against
/// the body-free control.
/// </summary>
/// <remarks>
/// <para>
/// This sizes what is left of roadmap item 1-1. Its emission half landed and the roadmap records
/// what remains as <em>"the parse and the expression-tree construction are still eager, and on
/// real source that is the larger half"</em> — a share that has never been measured directly. It
/// was derived: <see cref="CompileProfileMetrics"/>'s ceiling says 92-96% of compile is bodies,
/// the deferral A/B says emission is 17-36% of compile, and the remainder was read off as tree
/// construction. §3.5 — "a count you inferred is not a count, however well it reconciles" — is
/// about exactly that move, and 3-6 paid for it once already.
/// </para>
/// <para>
/// The split itself is not new: <see cref="CompileScalingMetrics"/> has taken it since item 1-4,
/// but only on synthetic declaration walls, where it reads parse 0.5% / tree 11% / emit 89%. 1-1
/// then established that figure does not carry to real source, so applying the same instrument to
/// the corpora is what turns an inference into a reading.
/// </para>
/// <para>
/// Four columns per corpus, each also taken on the control (the same source with every outermost
/// function body replaced by <c>{}</c>):
/// </para>
/// <list type="bullet">
/// <item><description><c>parse</c> — <c>FastParser.ParseProgram</c> alone.</description></item>
/// <item><description><c>tree</c> — the registered <see cref="IJSCompiler"/>, which parses and
/// then builds the <c>BExpression</c> tree, minus the parse above.</description></item>
/// <item><description><c>emitDeferred</c> — <c>CompileWithNestedLambdas</c> with item 1-1's
/// deferral on, i.e. what the engine does today.</description></item>
/// <item><description><c>emitEager</c> — the same with it off, which is the pre-1-1
/// engine.</description></item>
/// </list>
/// <para>
/// The derived row is <c>bodyTreeMs = treeFull - treeStub</c>: the tree construction that exists
/// only because the bodies do, and therefore the gross prize of deferring tree construction.
/// Against it sits <c>scanMs</c>, the charge-back — a deferred body still has to be walked for the
/// names it references, because the enclosing lambda's creation site has to box those bindings
/// before the body is compiled, and the walk is what the capture mechanism cannot avoid. It is
/// measured rather than assumed, the same way <see cref="CompileProfileMetrics"/> charges the
/// early-error pre-parse back to the emission half.
/// </para>
/// </remarks>
internal static class CompilePhaseMetrics
{
    public static void Write(string octaneDirectory, int repetitions, string only = null)
    {
        var corpora = CompileProfileMetrics.LoadCorpora(octaneDirectory);
        // Same rule as --compile-profile: one corpus per process. A deferred site holds its
        // expression tree until it is forced, so a corpus measured after Mandreel's 5 MB pays
        // collection time that is not its own.
        if (!string.IsNullOrEmpty(only))
            corpora = corpora.Where(c => c.Name == only).ToList();

        var rows = new List<object>(corpora.Count);

        foreach (var corpus in corpora)
        {
            var stub = CompileProfileMetrics.StubFunctionBodies(corpus.Source, out var outermost, out var total, out var bodyBytes, out var skipped);

            // One untimed pass of each phase: the first compile in the process pays for JIT of
            // the pipeline itself, which is not what this is measuring.
            Phases(corpus.Source, corpus.Name);
            Phases(stub, corpus.Name + "-stub");

            var full = Repeat(repetitions, () => Phases(corpus.Source, corpus.Name));
            var control = Repeat(repetitions, () => Phases(stub, corpus.Name + "-stub"));
            var scan = Repeat(repetitions, () => new Split(0, 0, 0, 0, 0, ScanNames(corpus.Source), 0)).Scan;
            // The same charge-back taken with the real resolver rather than an identifier count.
            var freeScan = Repeat(repetitions, () => new Split(0, 0, 0, 0, 0, ScanFreeNames(corpus.Source), 0)).Scan;

            var bodyTree = full.Tree - control.Tree;
            var bodyRewrite = full.Rewrite - control.Rewrite;
            var bodyEmit = full.EmitEager - full.EmitDeferred;
            var todayTotal = full.Parse + full.Tree + full.EmitDeferred;

            rows.Add(new
            {
                corpus = corpus.Name,
                sourceBytes = corpus.Source.Length,
                functionsTotal = total,
                functionsOutermost = outermost,
                functionsNotStubbed = skipped,
                bodyByteShare = Math.Round((double)bodyBytes / corpus.Source.Length, 4),

                parseMs = Round(full.Parse),
                treeMs = Round(full.Tree),
                rewriteMs = Round(full.Rewrite),
                emitDeferredMs = Round(full.EmitDeferred),
                emitEagerMs = Round(full.EmitEager),

                parseStubMs = Round(control.Parse),
                treeStubMs = Round(control.Tree),
                rewriteStubMs = Round(control.Rewrite),
                emitStubMs = Round(control.EmitDeferred),

                // What the engine spends today, deferral on, and how it divides.
                todayMs = Round(todayTotal),
                endToEndMs = Round(full.EndToEnd),
                parseShare = Share(full.Parse, todayTotal),
                treeShare = Share(full.Tree, todayTotal),
                emitShare = Share(full.EmitDeferred, todayTotal),

                // The two halves of 1-1, on the same scale: what deferring emission already
                // removed, and what deferring tree construction could remove.
                bodyEmitMs = Round(bodyEmit),
                bodyTreeMs = Round(bodyTree),
                bodyRewriteMs = Round(bodyRewrite),
                scanMs = Round(scan),
                // Item 1-1's remaining half, priced with the walk it actually needs: FreeNameScan
                // per function, resolving each name against the enclosing scopes. scanMs is the
                // identifier-count lower bound; this is the thing.
                freeScanMs = Round(freeScan),
                freeScanShareOfBodyTree = Share(freeScan, bodyTree),
                // The charge-back applied: a deferred body still needs its names collected.
                captureCeilingMs = Round(bodyTree + bodyRewrite - scan),
                captureCeilingShare = Share(bodyTree + bodyRewrite - scan, todayTotal),
            });

            Console.Error.WriteLine(
                $"{corpus.Name,-18} parse={full.Parse,8:F1} tree={full.Tree,9:F1} "
                + $"emit={full.EmitDeferred,8:F1} (eager {full.EmitEager,8:F1})  "
                + $"rewrite={full.Rewrite,8:F1} bodyTree={bodyTree,8:F1} bodyRw={bodyRewrite,8:F1} scan={scan,6:F1} => ceiling={bodyTree + bodyRewrite - scan,9:F1} ms "
                + $"of {todayTotal,9:F1} ms (end-to-end {full.EndToEnd,9:F1})");
        }

        Console.WriteLine(JsonSerializer.Serialize(
            new
            {
                metric = "compile-phases",
                repetitions,
                note = "todayMs = parseMs + treeMs + emitDeferredMs, the engine as it ships. "
                    + "bodyTreeMs = treeMs - treeStubMs, the tree construction that exists only "
                    + "because the bodies do. captureCeilingMs = bodyTreeMs - scanMs, what item "
                    + "1-1's remaining half could remove once the free-name walk it cannot defer "
                    + "is charged back to it.",
                rows,
            },
            new JsonSerializerOptions { WriteIndented = true }));
    }

    private static double Round(double value) => Math.Round(value, 2);

    private static double Share(double part, double whole)
        => whole > 0 ? Math.Round(part / whole, 4) : 0d;

    private readonly record struct Split(double Parse, double Tree, double Rewrite, double EmitDeferred, double EmitEager, double Scan, double EndToEnd);

    /// <summary>
    /// Times one pass of each phase over <paramref name="source"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Both emission arms need a tree of their own: <c>LambdaRewriter.Rewrite</c> mutates, so a
    /// tree that has been emitted once cannot be emitted again. The tree column is the timed one
    /// and the second build is untimed, which keeps the arms comparing emission and nothing else.
    /// </para>
    /// <para>
    /// <c>DeferredMethodCompilation.Enabled</c> is toggled in-process rather than read from the
    /// environment, because the question here is a difference between two emissions of the same
    /// tree in the same process — an environment variable would make it a difference between two
    /// processes as well. It is restored afterwards.
    /// </para>
    /// </remarks>
    private static Split Phases(string source, string name)
    {
        var span = new StringSpan(source);
        var compiler = CoreScript.Compiler;
        var stopwatch = new Stopwatch();

        // The decomposition's own check, and it is taken FIRST. Every compile in this method
        // registers a deferred site per relayed lambda, each rooted by a GCHandle that is never
        // freed and each holding its subtree — so a phase measured late in the sequence pays
        // collection time the phases before it caused. Measured last, this column read 3.4x the
        // sum of the phases on Box2D and 1.0x on jQuery, and the difference between those two
        // corpora is exactly how many sites a deferred compile registers: 982 against 1. That is
        // item 1-1's own retained-tree artifact, one level down from where the roadmap records
        // it. Taken first, against a fresh collection, it is comparable with the sum.
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        var previousForEndToEnd = DeferredMethodCompilation.Enabled;
        DeferredMethodCompilation.Enabled = true;
        stopwatch.Restart();
        CoreScript.Compile(source, name + ".js", codeCache: new NoCodeCache());
        stopwatch.Stop();
        DeferredMethodCompilation.Enabled = previousForEndToEnd;
        var endToEnd = stopwatch.Elapsed.TotalMilliseconds;

        GC.Collect(2, GCCollectionMode.Forced, blocking: true);

        stopwatch.Restart();
        new Broiler.JavaScript.Parser.FastParser(new FastTokenStream(in span)).ParseProgram();
        stopwatch.Stop();
        var parse = stopwatch.Elapsed.TotalMilliseconds;

        // The front end recurses over the source, so it takes the sized stack the engine gives
        // it in production (item 1-2) rather than whatever this thread happens to have.
        stopwatch.Restart();
        var tree = CompilationStack.Run(
            () => compiler.Compile(span, name + ".js", null, new NoCodeCache()),
            source.Length);
        stopwatch.Stop();
        // compiler.Compile parses before it builds, so its own parse is subtracted out; leaving
        // it in is what would make "tree" the number the roadmap already has.
        var treeOnly = stopwatch.Elapsed.TotalMilliseconds - parse;

        var previous = DeferredMethodCompilation.Enabled;
        try
        {
            DeferredMethodCompilation.Enabled = true;
            stopwatch.Restart();
            CompilationStack.Run(() => tree.CompileWithNestedLambdas(), source.Length);
            stopwatch.Stop();
            var emitDeferred = stopwatch.Elapsed.TotalMilliseconds;

            // Emission is two walks and only one of them is deferrable. LambdaRewriter.Rewrite
            // recurses through every nested lambda — that is how a variable a nested function
            // reads is registered as a capture in the scope that owns it — so with item 1-1's
            // deferral on, the whole tree is still walked even though only the outermost lambda
            // has IL generated. Timed on a tree of its own, because the rewrite mutates.
            var rewriteTree = CompilationStack.Run(
                () => compiler.Compile(span, name + ".js", null, new NoCodeCache()),
                source.Length);
            stopwatch.Restart();
            if (rewriteTree is BLambdaExpression rewriteLambda)
                CompilationStack.Run(() => LambdaRewriter.Rewrite(rewriteLambda), source.Length);
            stopwatch.Stop();
            var rewrite = stopwatch.Elapsed.TotalMilliseconds;

            var eagerTree = CompilationStack.Run(
                () => compiler.Compile(span, name + ".js", null, new NoCodeCache()),
                source.Length);

            DeferredMethodCompilation.Enabled = false;
            stopwatch.Restart();
            CompilationStack.Run(() => eagerTree.CompileWithNestedLambdas(), source.Length);
            stopwatch.Stop();
            var emitEager = stopwatch.Elapsed.TotalMilliseconds;

            return new Split(parse, treeOnly, rewrite, emitDeferred, emitEager, 0, endToEnd);
        }
        finally
        {
            DeferredMethodCompilation.Enabled = previous;
        }
    }

    private static Split Repeat(int repetitions, Func<Split> body)
    {
        var samples = new List<Split>(repetitions);
        for (var i = 0; i < repetitions; i++)
            samples.Add(body());

        // Each column's own median: the repetition whose parse is the median need not be the one
        // whose emission is.
        static double Median(IEnumerable<double> values)
        {
            var ordered = values.OrderBy(v => v).ToArray();
            return ordered[ordered.Length / 2];
        }

        return new Split(
            Median(samples.Select(s => s.Parse)),
            Median(samples.Select(s => s.Tree)),
            Median(samples.Select(s => s.Rewrite)),
            Median(samples.Select(s => s.EmitDeferred)),
            Median(samples.Select(s => s.EmitEager)),
            Median(samples.Select(s => s.Scan)),
            Median(samples.Select(s => s.EndToEnd)));
    }

    /// <summary>
    /// Parses, then walks every function body collecting the identifier names it mentions —
    /// the charge-back against deferring tree construction.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A deferred body cannot be compiled later against nothing. The enclosing lambda's creation
    /// site passes a <c>Box[]</c>, and which bindings are in it is decided while the enclosing
    /// lambda is emitted — before the deferred body exists as a tree. So the names the body
    /// mentions have to be known eagerly even if nothing else about it is, and the cheapest form
    /// of knowing them is this walk. Reporting <c>bodyTree</c> without subtracting it would credit
    /// the capture half with a saving it is forbidden from taking, which is the same error
    /// <see cref="CompileProfileMetrics"/> avoids by charging back the early-error pre-parse.
    /// </para>
    /// <para>
    /// It is a lower bound on the real mechanism and deliberately so: a real scanner also has to
    /// resolve each name against the enclosing scopes and distinguish a free reference from a
    /// locally bound one. Both are proportional to this walk, so it bounds the charge-back from
    /// below and the ceiling from above — and if the ceiling is small even at the lower bound,
    /// nothing further needs measuring.
    /// </para>
    /// </remarks>
    private static double ScanNames(string source)
    {
        var span = new StringSpan(source);
        var program = new Broiler.JavaScript.Parser.FastParser(new FastTokenStream(in span)).ParseProgram();

        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        var stopwatch = Stopwatch.StartNew();
        var scanner = new NameScanner();
        scanner.Visit(program);
        stopwatch.Stop();

        // Read so the walk cannot be optimized away, and so a scanner that collected nothing
        // would be visible rather than fast.
        if (scanner.Names.Count == 0 && source.Length > 1024)
            throw new InvalidOperationException("Name scan collected nothing.");

        return stopwatch.Elapsed.TotalMilliseconds;
    }

    /// <summary>
    /// The REAL scan: every function in the program walked for the names it references and does
    /// not bind, which is what item 1-1's remaining half actually has to run eagerly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="ScanNames"/> is the lower bound and says so — it counts identifiers and resolves
    /// nothing. This runs <see cref="FreeNameScan"/> once per function, which is the unit the
    /// deferral works in: each deferred site needs its own free-name set, so the cost is the sum
    /// over functions and not one walk of the program. **That distinction is most of the gap
    /// between the two numbers**, because a name inside three nested functions is walked by all
    /// three.
    /// </para>
    /// <para>
    /// Reported beside the bare walk rather than instead of it, so the difference between "a walk"
    /// and "the walk this needs" is visible rather than asserted.
    /// </para>
    /// </remarks>
    private static double ScanFreeNames(string source)
    {
        var span = new StringSpan(source);
        var program = new Broiler.JavaScript.Parser.FastParser(new FastTokenStream(in span)).ParseProgram();

        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        var stopwatch = Stopwatch.StartNew();
        var all = Broiler.JavaScript.Compiler.FreeNameScan.ForProgram(program);
        stopwatch.Stop();

        // A scan that resolved everything to bound would be fast and useless, so the reading is
        // guarded the same way the bare walk's is.
        if (all.Count == 0 && source.Length > 1024)
            throw new InvalidOperationException("No functions found to scan.");

        GC.KeepAlive(all);
        return stopwatch.Elapsed.TotalMilliseconds;
    }

    /// <summary>Every function in the program, including nested ones.</summary>
    private sealed class FunctionCollector(List<Broiler.JavaScript.Ast.Expressions.AstFunctionExpression> functions) : AstReduce
    {
        protected override AstNode VisitFunctionExpression(Broiler.JavaScript.Ast.Expressions.AstFunctionExpression function)
        {
            functions.Add(function);
            return base.VisitFunctionExpression(function);
        }

        protected override VariableDeclarator VisitVariableDeclarator(VariableDeclarator declarator)
        {
            Visit(declarator.Identifier);
            if (declarator.Init != null)
                Visit(declarator.Init);
            return declarator;
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

    /// <summary>
    /// Collects every identifier name in the program.
    /// </summary>
    /// <remarks>
    /// The three container overrides are the ones <see cref="AstReduce"/> leaves as leaves for
    /// its rewriting visitors, and their absence here would make the walk skip
    /// <c>var f = function () {}</c> — the dominant spelling in jQuery — and so under-report the
    /// charge-back. (§3.5: "a comment that says missing one here is a miscompile is a checklist".)
    /// </remarks>
    private sealed class NameScanner : AstReduce
    {
        public readonly HashSet<string> Names = new(StringComparer.Ordinal);

        protected override AstNode VisitIdentifier(AstIdentifier identifier)
        {
            Names.Add(identifier.Name.Value);
            return identifier;
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
