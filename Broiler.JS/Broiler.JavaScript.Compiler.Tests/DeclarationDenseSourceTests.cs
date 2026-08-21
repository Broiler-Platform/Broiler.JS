using System.Diagnostics;
using System.Text;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Compiler.Tests;

/// <summary>
/// Fixtures for the front end's cost on source that is <em>wide</em> rather than deep — many
/// bindings in one scope, which is what machine-generated JavaScript produces and what
/// <c>DeeplyNestedSourceTests</c> deliberately does not cover.
/// </summary>
/// <remarks>
/// <para>
/// The closure rewrite that runs before IL emission held each lambda's in-scope variables in a
/// <c>List</c> and asked it <c>Contains</c> once per parameter reference in the tree, so
/// emission cost grew as the square of the number of bindings in a scope. A script's top level
/// is one scope, which made the count of top-level declarations the term that squared:
/// emitting 500 / 1 000 / 2 000 top-level function declarations took 797 / 2 981 / 13 865 ms
/// while parse and expression-tree construction stayed flat. Mandreel is 1 364 top-level
/// function declarations and a matching <c>var</c> apiece, and MandreelLatency is the score
/// that measures the resulting pause (docs/performance-roadmap.md item 1-4).
/// </para>
/// <para>
/// Both halves are pinned here, because each is only half the claim. The scaling test says the
/// cost is no longer superlinear; the shadowing tests say the structure that made it linear
/// kept the semantics the list had. The second is not incidental — the list held
/// <em>duplicates</em>, and both operations depended on that (a variable registered by two
/// nested block scopes is added twice and must survive the inner scope's exit), so replacing
/// it with a plain set would have taken a still-live binding out of scope and miscompiled the
/// references after it.
/// </para>
/// </remarks>
public class DeclarationDenseSourceTests
{
    private static string TopLevelFunctions(int count)
    {
        var source = new StringBuilder(count * 20 + 64);
        for (var i = 0; i < count; i++)
            source.Append("function f").Append(i).Append("(){return ").Append(i).Append(";}\n");
        return source.ToString();
    }

    [Fact(Timeout = 600000)]
    public void ManyTopLevelDeclarations_CompileInTimeLinearInTheirCount()
    {
        // Four times the declarations should cost about four times as much. Quadratic predicts
        // sixteen; the ratio measured 17.4x before the fix and 2.6x after it, so the bound sits
        // decisively between the two rather than close to either. A ratio is used rather than a
        // wall-clock budget because the budget that separates them on one machine does not on
        // another, and this has to hold on whatever runs it.
        const int baseline = 500;
        const int wide = 2_000;
        const double quadraticWouldExceed = 8.0;

        var baselineSource = TopLevelFunctions(baseline);
        var wideSource = TopLevelFunctions(wide);

        // Compile both once untimed: the first compilation in the process pays to JIT the
        // pipeline itself, and charging that to whichever ran first would swamp the ratio.
        Compile(baselineSource);
        Compile(wideSource);

        var baselineMs = Median(baselineSource);
        var wideMs = Median(wideSource);

        Assert.True(
            wideMs < baselineMs * quadraticWouldExceed,
            $"{wide} top-level declarations took {wideMs:F0} ms against {baselineMs:F0} ms for "
                + $"{baseline} ({wideMs / baselineMs:F1}x for 4x the declarations); the closure "
                + "rewrite is scanning a linear structure per reference again.");

        static void Compile(string source)
        {
            using var context = new JSContext();
            context.Eval(source);
        }

        static double Median(string source)
        {
            var samples = new double[3];
            for (var i = 0; i < samples.Length; i++)
            {
                var stopwatch = Stopwatch.StartNew();
                Compile(source);
                samples[i] = stopwatch.Elapsed.TotalMilliseconds;
            }

            System.Array.Sort(samples);
            return samples[1];
        }
    }

    [Fact(Timeout = 600000)]
    public void BindingDeclaredBeforeANestedBlock_StaysInScopeAfterIt()
    {
        // The duplicate-registration case, at the top level where the deep block-variable
        // collection runs: `outer` is registered once by the program body's collection and
        // again by the block that declares it, so the block's exit must decrement rather than
        // remove. If it removed, the closure below — created after that exit — would resolve
        // `outer` through the enclosing scope instead of its own and capture the wrong thing.
        const string source = """
            var captured;
            {
                var outer = 'before';
                { var inner = 'nested'; }
                captured = function () { return outer + '/' + inner; };
                outer = 'after';
            }
            captured();
            """;

        using var context = new JSContext();
        Assert.Equal("after/nested", context.Eval(source).ToString());
    }

    [Fact(Timeout = 600000)]
    public void ClosuresOverManyTopLevelBindings_CaptureTheirOwn()
    {
        // Width plus capture: every one of these closures reads a distinct top-level binding,
        // which is the reference pattern whose per-reference scope lookup was the quadratic
        // term. Reading them back is what says the lookup still answers correctly at a width
        // where it used to be answered by a scan.
        const int count = 400;

        var source = new StringBuilder(count * 48 + 128);
        source.Append("var readers = [];\n");
        for (var i = 0; i < count; i++)
        {
            source.Append("var v").Append(i).Append(" = ").Append(i).Append(";\n");
            source.Append("readers.push(function () { return v").Append(i).Append("; });\n");
        }

        source.Append("var total = 0;\n");
        source.Append("for (var i = 0; i < readers.length; i++) total += readers[i]();\n");
        source.Append("total;");

        using var context = new JSContext();
        Assert.Equal(count * (count - 1) / 2, context.Eval(source.ToString()).DoubleValue);
    }
}
