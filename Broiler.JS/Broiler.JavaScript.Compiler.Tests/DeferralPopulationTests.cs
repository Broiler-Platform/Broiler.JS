using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Compiler.Tests;

// The population instrument for item 1-1's remaining half: function sites whose body tree could be
// built on first invocation instead of at compile time, with NO capture mechanism
// (docs/performance-roadmap.md item 1-1).
//
// Written before the corpus is measured, in this order deliberately, for the reason §3.5 records
// twice over: item 3-8a's first population instrument read zero on all seven Octane suites and was
// nearly published as a finding before anyone had shown it could read anything else, and item 3-9's
// was made to discriminate on nine constructed shapes first precisely so its zero would mean
// something. A counter never shown to read non-zero is a claim about the counter.
//
// **The negatives are the interesting half, and they come in three kinds:**
//
//   * shapes that separate this from an IDENTIFIER SCAN. `function (q) { return q; }` mentions `q`
//     and captures nothing; `function () { return q; }` captures one binding. The roadmap says
//     NestedFunctionScanner cannot tell them apart and that is exactly why it cannot answer this
//     question — so a fixture that only checked "a free name was found" would pass against the
//     wrong instrument.
//   * shapes that separate this from a NAME SCAN — the same body text, verbatim, in two enclosing
//     scopes, giving two different verdicts. Nothing about the function decides this; the enclosing
//     scope does. If a change ever made these two agree, the instrument would have stopped doing
//     the one thing it exists to do.
//   * shapes where the refusal is PERMANENT rather than mechanical: a direct `eval`, a `with` or a
//     `debugger` reaches bindings the text never names, so no capture mechanism helps and the site
//     must be attributed separately from one that is merely waiting for the box layout.
//
// And one invariant that comes free and is asserted on every shape: the three refusal rows sum to
// the number of function sites compiled, because the classification is a waterfall. A reading where
// they do not is a defect here rather than a discovery on the corpus.
[Collection(Phase3DiagnosticsCollection.Name)]
public sealed class DeferralPopulationTests
{
    private readonly record struct Population(
        long CaptureFree,
        long Dynamic,
        long Captures,
        long FreeNames,
        long BoundFreeNames,
        long FunctionOwnedFreeNames,
        long CellBackedFreeNames)
    {
        public long Sites => CaptureFree + Dynamic + Captures;
    }

    private static Population Count(string source)
    {
        var previous = DeferrableFunctions.Counting;
        DeferrableFunctions.Counting = true;
        using var context = new JSContext();
        CompilerSpecializationDiagnostics.Reset();
        try
        {
            context.Eval(source);
            var c = CompilerSpecializationDiagnostics.Snapshot();
            var p = new Population(
                c.DeferralRefusals[(int)DeferralRefusal.CaptureFree],
                c.DeferralRefusals[(int)DeferralRefusal.Dynamic],
                c.DeferralRefusals[(int)DeferralRefusal.CapturesEnclosingBinding],
                c.DeferralFreeNames,
                c.DeferralBoundFreeNames,
                c.DeferralFunctionOwnedFreeNames,
                c.DeferralCellBackedFreeNames);

            // Waterfall invariants, asserted on every shape rather than in one test, so a
            // classification that starts double-counting fails wherever it is used.
            Assert.True(p.BoundFreeNames <= p.FreeNames, $"bound={p.BoundFreeNames} exceeded free={p.FreeNames}");
            Assert.True(
                p.FunctionOwnedFreeNames <= p.BoundFreeNames,
                $"functionOwned={p.FunctionOwnedFreeNames} exceeded bound={p.BoundFreeNames}");
            Assert.True(
                p.CellBackedFreeNames <= p.BoundFreeNames,
                $"cellBacked={p.CellBackedFreeNames} exceeded bound={p.BoundFreeNames}");
            return p;
        }
        finally
        {
            DeferrableFunctions.Counting = previous;
        }
    }

    [Fact]
    public void TheSwitchIsOffByDefault()
    {
        // It costs a FreeNameScan per compiled function, which 0101 measured as superlinear in
        // nesting depth in exactly this per-function shape — affordable for a counter nobody ships,
        // and not what a real implementation would do.
        Assert.False(DeferrableFunctions.Counting);
    }

    [Fact]
    public void AFunctionReferencingOnlyItsOwnBindingsIsCaptureFree()
    {
        // The floor case: nothing free at all, so nothing to resolve and nothing to box.
        var p = Count("function f(a, b) { var c = a + b; return c; } f(1, 2);");

        Assert.Equal(1, p.CaptureFree);
        Assert.Equal(0, p.Captures);
        Assert.Equal(0, p.FreeNames);
    }

    [Fact]
    public void AFunctionReferencingOnlyGlobalsIsCaptureFree()
    {
        // Free names that resolve to NOTHING this compilation holds. `Math` is looked up on the
        // global object at run time, from a body compiled whenever — which is the entire argument
        // for why this population needs no capture mechanism.
        var p = Count("function f(x) { return Math.abs(x); } f(-1);");

        Assert.Equal(1, p.CaptureFree);
        Assert.Equal(0, p.Captures);
        Assert.True(p.FreeNames >= 1, "Math should have been reported free");
        Assert.Equal(0, p.BoundFreeNames);
    }

    [Fact]
    public void AFunctionReadingAnEnclosingLocalCaptures()
    {
        var p = Count("""
            function outer() {
              var q = 1;
              var inner = function () { return q; };
              return inner();
            }
            outer();
            """);

        // `outer` itself is capture-free; `inner` is not.
        Assert.Equal(1, p.CaptureFree);
        Assert.Equal(1, p.Captures);
        Assert.Equal(1, p.BoundFreeNames);
        Assert.Equal(1, p.FunctionOwnedFreeNames);
    }

    [Fact]
    public void TheSameBodyTextIsCaptureFreeOrNotDependingOnTheENCLOSINGScope()
    {
        // **The fixture the whole instrument rests on.** The two bodies are byte-identical. Nothing
        // about the function decides the answer — the enclosing scope does, and resolving against
        // it is the one thing no existing scanner in this compiler does. If these ever agree, the
        // count has stopped measuring what it claims to.
        const string body = "var inner = function () { return q; };";

        var free = Count($"{body} inner;");
        var captured = Count($"function outer() {{ var q = 1; {body} return inner(); }} outer();");

        Assert.Equal(1, free.CaptureFree);
        Assert.Equal(0, free.Captures);

        Assert.Equal(1, captured.Captures);
        Assert.Equal(1, captured.FunctionOwnedFreeNames);
    }

    [Fact]
    public void AParameterSharingTheOuterSpellingCapturesNothing()
    {
        // Separates this from an identifier scan, which is the distinction the roadmap names as the
        // reason NestedFunctionScanner cannot answer here: `q` is MENTIONED and is not free, so
        // boxing on a mention would box a binding for a name the inner function owns.
        var p = Count("""
            function outer() {
              var q = 1;
              var inner = function (q) { return q; };
              return inner(2);
            }
            outer();
            """);

        Assert.Equal(2, p.CaptureFree);
        Assert.Equal(0, p.Captures);
        Assert.Equal(0, p.BoundFreeNames);
    }

    [Fact]
    public void AnInnerDeclarationSharingTheOuterSpellingCapturesNothing()
    {
        // The same separation through a `var` rather than a parameter, and through hoisting: the
        // reference is textually before the declaration and still bound.
        var p = Count("""
            function outer() {
              var q = 1;
              var inner = function () { var r = q; var q = 2; return r; };
              return inner();
            }
            outer();
            """);

        Assert.Equal(2, p.CaptureFree);
        Assert.Equal(0, p.Captures);
    }

    [Fact]
    public void ANamedFunctionExpressionDoesNotCaptureItsOwnName()
    {
        var p = Count("var f = function g(n) { return n < 1 ? 0 : g(n - 1); }; f(3);");

        Assert.Equal(1, p.CaptureFree);
        Assert.Equal(0, p.Captures);
    }

    [Fact]
    public void CaptureIsTransitiveThroughAnInterveningFunction()
    {
        // A name read two levels down is free in BOTH the reader and the function between it and
        // the binding, because the intervening body has to hand the box on. Charging only the
        // innermost would understate what a capture mechanism has to arrange.
        var p = Count("""
            function outer() {
              var q = 1;
              var middle = function () {
                var deep = function () { return q; };
                return deep();
              };
              return middle();
            }
            outer();
            """);

        Assert.Equal(1, p.CaptureFree);   // outer
        Assert.Equal(2, p.Captures);      // middle and deep
        Assert.Equal(2, p.BoundFreeNames);
    }

    [Fact]
    public void ADirectEvalMakesTheSiteUndeferrableRatherThanCapturing()
    {
        // A permanent refusal, and it must not be filed under the mechanical one: no capture
        // mechanism helps a body that can reach a binding the text never names.
        //
        // Both sites are Dynamic, not one. `inner` is dynamic because it contains the eval, and
        // `outer` because it contains `inner` — this fixture was written expecting 1 and the
        // instrument said 2, which is FreeNameScan propagating Dynamic outward exactly as it
        // documents. The expectation was wrong and the propagation is the correct answer: `q` is
        // reachable from a body neither function names, so neither can be deferred.
        var p = Count("""
            function outer() {
              var q = 1;
              var inner = function (s) { return eval(s); };
              return inner('q');
            }
            outer();
            """);

        Assert.Equal(2, p.Dynamic);
        Assert.Equal(0, p.Captures);
        Assert.Equal(0, p.CaptureFree);
    }

    [Fact]
    public void AWithStatementMakesTheSiteUndeferrable()
    {
        var p = Count("""
            var o = { q: 1 };
            var inner = function () { with (o) { return q; } };
            inner();
            """);

        Assert.Equal(1, p.Dynamic);
    }

    [Fact]
    public void ADynamicBodyPoisonsTheFunctionsAboveItAndNotThoseBesideIt()
    {
        // FreeNameScan propagates Dynamic to the parent, because an enclosing body cannot be
        // deferred either while something inside it can reach unnamed bindings. The sibling is the
        // control: without it a test asserting "two dynamic" cannot tell propagation from a blanket
        // refusal of everything in the program.
        var p = Count("""
            function outer() {
              var inner = function (s) { return eval(s); };
              return inner('1');
            }
            function beside() { return 1; }
            outer(); beside();
            """);

        Assert.Equal(2, p.Dynamic);      // outer, poisoned by inner; and inner
        Assert.Equal(1, p.CaptureFree);  // beside
    }

    [Fact]
    public void EveryFunctionSiteIsClassifiedExactlyOnce()
    {
        // The waterfall's own invariant, as a test rather than as a corpus reading: a program with
        // a known number of function sites reports that number, so a site classified twice or not
        // at all fails here.
        var p = Count("""
            function a() { return 1; }
            var b = function () { return 2; };
            var c = () => 3;
            var o = { m: function () { return 4; }, get g() { return 5; } };
            a(); b(); c(); o.m(); o.g;
            """);

        Assert.Equal(5, p.Sites);
    }

    [Fact]
    public void ATopLevelLexicalBindingIsCountedAsBoundButNotFunctionOwned()
    {
        // The two name counts exist to be told apart. A program-level `let` is a real binding this
        // compilation holds, so it is charged as bound — the safe direction — while its owner is
        // the program rather than a function. Whether that one needs a box is the question the
        // separate count leaves open instead of settling by assertion.
        var p = Count("""
            let q = 1;
            var inner = function () { return q; };
            inner();
            """);

        Assert.Equal(1, p.Captures);
        Assert.Equal(1, p.BoundFreeNames);
        Assert.Equal(0, p.FunctionOwnedFreeNames);
    }

    [Fact]
    public void ABindingWithNoCellOfItsOwnIsBoundAndNotCellBacked()
    {
        // **The fixture that makes the corpus reading mean anything.** `cellBacked` reads exactly
        // equal to `bound` on all six corpora, and an equality from a counter never shown to
        // separate is a claim about the counter rather than about the corpus — §3.5, twice paid
        // for in phase 3. This is the shape where they differ.
        //
        // A named function expression's own name binds inside itself with NO local Variable: the
        // scope entry exists (which is how `g` resolves) and there is no CLR local to box, which
        // is why it also carries an EvalCaptureExpression instead. Read from a function nested
        // inside `g`, `g` is genuinely free — and genuinely costs a deferral nothing.
        var p = Count("""
            var f = function g() {
              var inner = function () { return typeof g; };
              return inner();
            };
            f();
            """);

        Assert.Equal(1, p.BoundFreeNames);
        Assert.Equal(0, p.CellBackedFreeNames);

        // And the classification follows the cell rather than the binding: with nothing to box,
        // the site is deferrable today.
        Assert.Equal(0, p.Captures);
    }

    [Fact]
    public void AnOrdinaryEnclosingLocalIsBothBoundAndCellBacked()
    {
        // The positive half of the pair above. Without it, "cellBacked reads 0 here" is consistent
        // with the counter never incrementing at all.
        var p = Count("""
            function outer() {
              var q = 1;
              var inner = function () { return q; };
              return inner();
            }
            outer();
            """);

        Assert.Equal(1, p.BoundFreeNames);
        Assert.Equal(1, p.CellBackedFreeNames);
    }

    [Fact]
    public void AProgramTopLevelVarIsCellBackedDESPITEBeingAGlobalProperty()
    {
        // **The reading that looked like an opening, refused.** A script's top-level `var` is a
        // property of the global object per spec, and Mandreel is 1 364 top-level declarations —
        // so its 7 605 bound free names being only 165 function-owned looked like a population
        // that could be deferred with no capture mechanism at all.
        //
        // It cannot. This engine gives a program-level binding a CLR local in the program lambda
        // like any other, so it needs a Box[] entry on exactly the same terms. A spec-level fact
        // about where a binding lives is not a fact about where the compiler puts it, and the
        // separate functionOwned count is what let the two be told apart instead of assumed equal.
        var p = Count("""
            var q = 1;
            var inner = function () { return q; };
            inner();
            """);

        Assert.Equal(1, p.BoundFreeNames);
        Assert.Equal(0, p.FunctionOwnedFreeNames);
        Assert.Equal(1, p.CellBackedFreeNames);
        Assert.Equal(1, p.Captures);
    }

    [Fact]
    public void CountingChangesNoAnswer()
    {
        // The instrument reads the enclosing scope through TryResolveBinding rather than
        // GetVariable precisely because GetVariable sets RootScope.HasOuterFunctionCaptures, which
        // is a conjunct of the tiering gate — so a probe built on it would turn tiering off for
        // functions it merely asked about. This is that hazard as a test: the same program on both
        // settings of the switch.
        const string source = """
            function outer() {
              var q = 1;
              var inner = function () { return q; };
              var tierable = function (n) { var s = 0; for (var i = 0; i < n; i++) { s = s + i; } return s; };
              return inner() + tierable(10);
            }
            outer();
            """;

        static string Run(bool counting)
        {
            var previous = DeferrableFunctions.Counting;
            DeferrableFunctions.Counting = counting;
            try
            {
                using var context = new JSContext();
                return context.Eval(source).ToString();
            }
            finally
            {
                DeferrableFunctions.Counting = previous;
            }
        }

        Assert.Equal(Run(false), Run(true));
    }
}
