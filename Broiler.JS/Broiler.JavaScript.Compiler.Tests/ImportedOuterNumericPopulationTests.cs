using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Compiler.Tests;

// The population instrument for item 3-9: locals that become numeric once an ENCLOSING function's
// proven-numeric conclusion is carried across the closure boundary
// (docs/performance-roadmap.md item 3-9).
//
// Written before the corpus is measured, and in this order deliberately. Item 3-8a's first
// population instrument read zero on all seven Octane suites and was nearly published as a finding
// before anyone had shown it could read anything else; §3.5 gained the rule that a counter never
// shown to read non-zero is a claim about the counter. So the discrimination is established here on
// constructed shapes first.
//
// The negatives are the interesting half, and there are two distinct kinds:
//
//   * shapes that separate 3-9 from **3-8a** — a global, or an outer name the enclosing analysis
//     dropped. 3-8a assumes any outer name holds a number; 3-9 accepts only one already proved to.
//     An instrument that could not tell them apart would report 3-8a's 26 names again.
//   * shapes that separate 3-9 from a **wrong answer** — an inner binding that merely shares a
//     spelling with an outer numeric local. Typing one of those from the other is not a missed
//     opportunity, it is a miscompile.
//
// And one bound that comes free: **3-9's population is a subset of 3-8a's by construction**, since
// everything an enclosing scope has proved numeric is also something an optimistic pass assumes
// numeric. Every fixture asserts it, so a reading that violates it fails here rather than on the
// corpus.
[Collection(Phase3DiagnosticsCollection.Name)]
public sealed class ImportedOuterNumericPopulationTests
{
    private readonly record struct Population(long Imported, long Speculative, long Numeric);

    private static Population Count(string source)
    {
        var previousOuter = ImportedOuterNumerics.Counting;
        var previousSpeculative = SpeculativeNumericLocals.Counting;
        ImportedOuterNumerics.Counting = true;
        SpeculativeNumericLocals.Counting = true;
        using var context = new JSContext();
        CompilerSpecializationDiagnostics.Reset();
        try
        {
            context.Eval(source);
            var c = CompilerSpecializationDiagnostics.Snapshot();
            return new Population(
                c.ImportedOuterNumericCandidates,
                c.SpeculativeNumericCandidates,
                c.NumericLocals);
        }
        finally
        {
            ImportedOuterNumerics.Counting = previousOuter;
            SpeculativeNumericLocals.Counting = previousSpeculative;
        }
    }

    private static Population CountAndCheckBound(string source)
    {
        var p = Count(source);

        // 3-9 ⊆ 3-8a, by construction rather than by measurement. Asserted on every shape so the
        // relation is a property of the instrument and not an observation about one program.
        Assert.True(
            p.Imported <= p.Speculative,
            $"imported={p.Imported} exceeded speculative={p.Speculative}, which is impossible");
        return p;
    }

    [Fact]
    public void TheSwitchIsOffByDefault()
    {
        // It costs two extra analysis passes per compiled function, which is compile time nothing
        // else needs to pay.
        Assert.False(ImportedOuterNumerics.Counting);
    }

    [Fact]
    public void AnEnclosingNumericLocalReadThroughAFunctionExpressionIsInThePopulation()
    {
        // The shape item 3-9 exists for, and the only one it can reach. `rowSize` is proven
        // numeric by `o`'s own analysis AND survives to a raw double because its single capturer
        // is a function EXPRESSION — item 3-7 gives that a Box<double> rather than a JSVariable
        // cell. So the conclusion is available to carry, and `c` becomes an ordinary numeric local
        // with no run-time test anywhere.
        var p = CountAndCheckBound("""
            function o() {
              var rowSize = 10;
              var f = function () { var c = 2 * rowSize; c++; return c; };
              return f();
            }
            o();
            """);

        Assert.Equal(1, p.Imported);
    }

    [Fact]
    public void AnEnclosingNameHeldByAHoistedDeclarationIsNotInThePopulation()
    {
        // **The item's own prediction, and the reason it was expected not to reach NavierStokes.**
        // One character of difference from the fixture above — `function f()` instead of
        // `var f = function ()` — and `rowSize` is now captured by a hoisted function DECLARATION,
        // which item 3-7 proved must keep its JSVariable cell for correctness. It is still proven
        // numeric by the analysis and it is still not a raw double, so there is no conclusion to
        // carry and 3-9 reaches nothing.
        //
        // This is also why the probe asks `NumericStorage != null` — what the compiler BUILT —
        // rather than asking the enclosing analysis what it proved. The two differ exactly here,
        // and an instrument built on the second would report this shape as a win.
        var p = CountAndCheckBound("""
            function o() {
              var rowSize = 10;
              function f() { var c = 2 * rowSize; c++; return c; }
              return f();
            }
            o();
            """);

        Assert.Equal(0, p.Imported);

        // And it is in 3-8a's population, which is what says the fixture reaches the analysis at
        // all rather than failing to compile the shape.
        Assert.True(p.Speculative > 0);
    }

    [Fact]
    public void AGlobalIsNotInThePopulationAlthoughItIsIn3_8aS()
    {
        // The sharpest separation from 3-8a, because this is the shape 3-8a's own count was
        // demonstrated on. A global has no enclosing binding to have proved anything about, so
        // there is no static conclusion to import and only a run-time test could type `c`.
        var p = CountAndCheckBound("gg = 3; function f(){ var c = 2 * gg; c++; return c; } f();");

        Assert.Equal(0, p.Imported);
        Assert.Equal(1, p.Speculative);
    }

    [Fact]
    public void AnEnclosingNameTheAnalysisDroppedIsNotInThePopulation()
    {
        // The second separation from 3-8a: the outer name exists and is a local, and the enclosing
        // analysis could not type it. 3-8a assumes it anyway and pays a guard; 3-9 has nothing to
        // import and correctly declines.
        var p = CountAndCheckBound("""
            function o(size) {
              var rowSize = size.width;
              var f = function () { var c = 2 * rowSize; c++; return c; };
              return f();
            }
            o({ width: 10 });
            """);

        Assert.Equal(0, p.Imported);
        Assert.True(p.Speculative > 0);
    }

    [Fact]
    public void AnInnerDeclarationSharingTheSpellingIsNotTypedFromTheOuterOne()
    {
        // A wrong-answer negative rather than a coverage one. `rowSize` inside `f` is `f`'s own
        // binding and holds a string; that it shares a spelling with `o`'s numeric local is a fact
        // about names, not about values. An instrument that typed `c` here would be describing a
        // miscompile.
        var p = CountAndCheckBound("""
            function o() {
              var rowSize = 10;
              var f = function () {
                var rowSize = "wide";
                var c = 2 * rowSize;
                c++;
                return c;
              };
              return f();
            }
            o();
            """);

        Assert.Equal(0, p.Imported);
    }

    [Fact]
    public void AnInnerParameterSharingTheSpellingIsNotTypedFromTheOuterOne()
    {
        // The same hazard through the other binding form, and the one 3-8a's instrument also had
        // to exclude: a parameter is a value the caller picks per call, so no conclusion about an
        // enclosing local says anything about it. Item 3-3's acknowledged gap, and a different
        // item.
        var p = CountAndCheckBound("""
            function o() {
              var rowSize = 10;
              var f = function (rowSize) { var c = 2 * rowSize; c++; return c; };
              return f("wide");
            }
            o();
            """);

        Assert.Equal(0, p.Imported);
    }

    [Fact]
    public void TheCascadeResolvesRatherThanBeingCountedAsOneRootCause()
    {
        // The imported pass is a fixed point like the real one, so relaxing the first name
        // resolves the second for free and BOTH come out. That is what makes this a population
        // rather than a count of root causes, and it is the half a hand-written rule would most
        // easily get wrong.
        var p = CountAndCheckBound("""
            function o() {
              var rowSize = 10;
              var f = function () {
                var r = rowSize;
                var c = 2 * r;
                c++;
                return c + r;
              };
              return f();
            }
            o();
            """);

        Assert.Equal(2, p.Imported);
    }

    [Fact]
    public void TwoLevelsOutIsStillOneLookup()
    {
        // The probe resolves through the whole enclosing chain rather than one frame, which is
        // what a lexical reference does. Worth pinning because the alternative — checking only the
        // immediately enclosing function — would silently under-count on any nested helper.
        var p = CountAndCheckBound("""
            function o() {
              var rowSize = 10;
              var mid = function () {
                var inner = function () { var c = 2 * rowSize; c++; return c; };
                return inner();
              };
              return mid();
            }
            o();
            """);

        Assert.Equal(1, p.Imported);
    }
}
