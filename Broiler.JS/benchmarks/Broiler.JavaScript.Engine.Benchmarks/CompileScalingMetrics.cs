using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.ExpressionCompiler.Runtime;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Parser;
using Broiler.JavaScript.Engine.FastParser.Compiler;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.ExpressionCompiler.Expressions;

namespace Broiler.JavaScript.Engine.Benchmarks;

/// <summary>
/// Reports how the front end's cost scales with the number of top-level declarations in a
/// script, and with the length of the names they bind.
/// </summary>
/// <remarks>
/// <para>
/// Written because <see cref="CompileProfileMetrics"/> found Mandreel behaving unlike every
/// other corpus. Removing every function body takes PdfJS, Typescript and Box2D to ~5 ms of
/// compile; it leaves Mandreel at <em>17.7 s</em> for the 248 KB that remains — 6 224 lines of
/// top-level <c>function f(){}</c> and <c>var x = register_delegate(f);</c>, with the
/// 100-250 character mangled C++ symbol names Mandreel generates. Per byte that residue is
/// some 70x more expensive than Box2D's whole source, which is not a difference lazy
/// compilation can explain or fix.
/// </para>
/// <para>
/// A cost that large on that little code is either superlinear in the declaration count or
/// linear in something per-declaration that is itself large — and the two are told apart by
/// varying one at a time, which is all this does. Each row is a synthetic script at a given
/// declaration count and name length; a doubling of N that quadruples the time is quadratic,
/// and a doubling of the name length that doubles the time at fixed N is per-name.
/// </para>
/// </remarks>
internal static class CompileScalingMetrics
{
    private static readonly int[] Counts = [125, 250, 500, 1_000, 2_000];

    public static void Write()
    {
        // Creating a context is what installs CoreScript's compiler; without one, Compile
        // dereferences null.
        using var context = BenchmarkContext.Create(new NoCodeCache());
        var rows = new List<object>();

        foreach (var shape in Shapes)
        {
            foreach (var count in Counts)
            {
                var source = shape.Build(count);
                // One untimed compile per shape pays for JIT of the pipeline itself.
                if (count == Counts[0])
                    Compile(source);

                var (parse, tree, emit, rewrite) = Measure(source);
                var total = parse + tree + emit;
                rows.Add(new
                {
                    shape = shape.Name,
                    note = shape.Note,
                    declarations = count,
                    sourceBytes = source.Length,
                    parseMs = Math.Round(parse, 2),
                    treeMs = Math.Round(tree, 2),
                    emitMs = Math.Round(emit, 2),
                    rewriteMs = Math.Round(rewrite, 2),
                    ms = Math.Round(total, 2),
                    msPerDeclaration = Math.Round(total / count, 4),
                });

                // Streamed as it completes: a shape whose cost is superlinear is exactly the
                // one whose run may not finish, and a report that only exists at the end
                // would lose the rows that say so.
                Console.Error.WriteLine(
                    $"{shape.Name,-22} N={count,-6} parse={parse,8:F1} tree={tree,9:F1} "
                    + $"emit={emit,9:F1} (rewrite={rewrite,8:F1}) total={total,9:F1} ms  ({total / count:F3} ms/decl)");
            }
        }

        Console.WriteLine(JsonSerializer.Serialize(
            new
            {
                metric = "compile-scaling",
                note = "msPerDeclaration constant => linear in the declaration count; rising in "
                    + "proportion to the count => quadratic.",
                rows,
            },
            new JsonSerializerOptions { WriteIndented = true }));
    }

    private sealed record Shape(string Name, string Note, Func<int, string> Build);

    /// <summary>
    /// Mandreel's mangled names run 100-250 characters; the short variants are the control that
    /// says whether length matters at all.
    /// </summary>
    private const int LongNameLength = 200;

    private static readonly Shape[] Shapes =
    [
        new("function-decl-short", "N top-level `function fN(){}`, short names",
            count => Build(count, i => $"function f{i}(){{}}\n")),
        new("function-decl-long", "N top-level `function <200-char>(){}`",
            count => Build(count, i => $"function {Name(i)}(){{}}\n")),
        new("var-decl-short", "N top-level `var vN = 0;`, short names",
            count => Build(count, i => $"var v{i} = 0;\n")),
        new("var-decl-long", "N top-level `var <200-char> = 0;`",
            count => Build(count, i => $"var {Name(i)} = 0;\n")),
        // The shape Mandreel's residue actually is: a declaration and a var that references it.
        new("mandreel-shape", "N pairs of `function <long>(){}` + `var <long>__index__ = g(<long>);`",
            count => Build(count, i => $"function {Name(i)}(){{}}\nvar {Name(i)}__index__ = g({Name(i)});\n")),
    ];

    private static string Name(int index)
    {
        var suffix = index.ToString();
        return "_ZN" + new string('a', LongNameLength - 3 - suffix.Length) + suffix;
    }

    private static string Build(int count, Func<int, string> line)
    {
        var builder = new StringBuilder();
        // `g` exists so the mandreel shape's call has a callee; it is never invoked.
        builder.Append("function g(x){return x;}\n");
        for (var i = 0; i < count; i++)
            builder.Append(line(i));
        return builder.ToString();
    }

    private static void Compile(string source)
        => CoreScript.Compile(source, "compile-scaling.js", codeCache: new NoCodeCache());

    /// <summary>
    /// Times the three phases separately: recursive-descent parse, expression-tree
    /// construction (the <c>FastCompiler</c> front end), and IL emission.
    /// </summary>
    /// <remarks>
    /// <c>CoreScript.Compile</c> fuses all three behind a code cache, so it can say a compile
    /// is slow but never which part of it is. The phases are reachable individually — the
    /// registered <see cref="IJSCompiler"/> returns the tree, and
    /// <c>CompileWithNestedLambdas</c> turns that into a delegate — which is what makes this a
    /// diagnosis rather than another total. Phase 1's B4 asks for exactly this split
    /// ("splitting the cost three ways: parse, expression-tree construction, IL emission").
    /// </remarks>
    private static (double Parse, double Tree, double Emit, double Rewrite) Measure(string source)
    {
        var span = new StringSpan(source);
        var compiler = CoreScript.Compiler;

        // Three repetitions, median, because a single compile of a multi-second source can be
        // disturbed by a collection it did not cause.
        var parse = new List<double>(3);
        var tree = new List<double>(3);
        var emit = new List<double>(3);
        var rewrite = new List<double>(3);

        for (var i = 0; i < 3; i++)
        {
            GC.Collect(2, GCCollectionMode.Forced, blocking: true);

            var stopwatch = Stopwatch.StartNew();
            new Broiler.JavaScript.Parser.FastParser(new FastTokenStream(in span)).ParseProgram();
            stopwatch.Stop();
            parse.Add(stopwatch.Elapsed.TotalMilliseconds);

            // The front end recurses over the source, so it takes the same sized stack the
            // engine gives it in production (item 1-2) rather than whatever this thread has.
            stopwatch.Restart();
            var expression = Broiler.JavaScript.ExpressionCompiler.CompilationStack.Run(
                () => compiler.Compile(span, "compile-scaling.js", null, new NoCodeCache()),
                source.Length);
            stopwatch.Stop();
            tree.Add(stopwatch.Elapsed.TotalMilliseconds);

            stopwatch.Restart();
            Broiler.JavaScript.ExpressionCompiler.CompilationStack.Run(
                () => expression.CompileWithNestedLambdas(),
                source.Length);
            stopwatch.Stop();
            emit.Add(stopwatch.Elapsed.TotalMilliseconds);

            // Emission is two passes over the same tree — the closure rewrite that decides
            // which variables a nested lambda captures, and the IL generation itself — and
            // "emit is slow" does not say which. Rewrite mutates, so it gets a tree of its
            // own; building one costs a fraction of what is being attributed.
            var forRewrite = Broiler.JavaScript.ExpressionCompiler.CompilationStack.Run(
                () => compiler.Compile(span, "compile-scaling.js", null, new NoCodeCache()),
                source.Length);
            stopwatch.Restart();
            if (forRewrite is BLambdaExpression lambda)
                Broiler.JavaScript.ExpressionCompiler.CompilationStack.Run(
                    () => LambdaRewriter.Rewrite(lambda),
                    source.Length);
            stopwatch.Stop();
            rewrite.Add(stopwatch.Elapsed.TotalMilliseconds);
        }

        return (Median(parse), Median(tree), Median(emit), Median(rewrite));
    }

    private static double Median(List<double> samples) => samples.OrderBy(v => v).ElementAt(1);
}
