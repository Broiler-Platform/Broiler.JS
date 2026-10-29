using Broiler.JavaScript.Engine;
using Broiler.JavaScript.ExpressionCompiler;

namespace Broiler.JavaScript.Compiler.Tests;

// Item 1-1's remaining half: the capture layout derived from SOURCE, checked against the one
// LambdaRewriter derives from the TREE (docs/performance-roadmap.md item 1-1).
//
// The item's obstacle is that a captured name's index in the enclosing lambda's Box[] is decided
// by a walk over the enclosing tree, and a deferred body has no tree to walk. So the layout has to
// be derivable without one — and the only property that matters about that derivation is that it
// never MISSES.
//
//   * over-approximating boxes a binding that did not need it. A cost: one box per creation site,
//     and the enclosing function loses that name's numeric tier.
//   * under-approximating means a deferred body resolves a name to a box that is not there. A
//     miscompile.
//
// The two are not comparable, so `MissedSites` is the go/no-go number and everything here exists
// to make a zero in it mean something. A checker that predicted "every binding in scope" would
// never miss and would be useless; a checker that predicted nothing would never be exercised. The
// fixtures below pin both ends.
[Collection(Phase3DiagnosticsCollection.Name)]
public sealed class CaptureLayoutTests
{
    private readonly record struct Layout(
        long Sites,
        long Exact,
        long Over,
        long Missed,
        long PredictedNames,
        long ActualNames,
        long MissedNames,
        long Synthetic);

    private static Layout Check(string source)
    {
        var previous = DeferredCaptureLayout.Checking;
        DeferredCaptureLayout.Checking = true;
        using var context = new JSContext();
        DeferredCaptureLayout.Reset();
        try
        {
            context.Eval(source);
            return new Layout(
                DeferredCaptureLayout.Sites,
                DeferredCaptureLayout.ExactSites,
                DeferredCaptureLayout.OverApproximatedSites,
                DeferredCaptureLayout.MissedSites,
                DeferredCaptureLayout.PredictedNames,
                DeferredCaptureLayout.ActualNames,
                DeferredCaptureLayout.MissedNames,
                DeferredCaptureLayout.SyntheticNames);
        }
        finally
        {
            DeferredCaptureLayout.Checking = previous;
        }
    }

    [Fact]
    public void TheSwitchIsOffByDefault()
        => Assert.False(DeferredCaptureLayout.Checking);

    [Fact]
    public void ASimpleCaptureIsPredictedExactly()
    {
        var l = Check("""
            function outer() {
              var q = 1;
              var inner = function () { return q; };
              return inner();
            }
            outer();
            """);

        Assert.True(l.Missed == 0, "missed: " + string.Join(", ", DeferredCaptureLayout.MissedNameSamples));
        Assert.True(l.ActualNames >= 1, "the rewrite should have handed q in");
        Assert.Equal(0, l.MissedNames);
    }

    [Fact]
    public void ACaptureThreeLevelsDownIsPredictedAtEveryLevel()
    {
        // The transitive case, and the one a per-function walk gets wrong if it does not compose:
        // `q` is free in the reader AND in every function between it and the binding, because each
        // of those has to hand the box on.
        var l = Check("""
            function outer() {
              var q = 1;
              return (function () {
                return (function () {
                  return (function () { return q; })();
                })();
              })();
            }
            outer();
            """);

        Assert.Equal(0, l.Missed);
        Assert.Equal(0, l.MissedNames);
    }

    [Fact]
    public void AWriteThroughACaptureIsPredicted()
    {
        // A capture the body only ever ASSIGNS. A walk that collects reads and forgets writes
        // passes every read-shaped fixture and misses this one.
        var l = Check("""
            function outer() {
              var q = 1;
              var bump = function () { q = q + 1; };
              bump(); bump();
              return q;
            }
            outer();
            """);

        Assert.Equal(0, l.Missed);
        Assert.Equal(0, l.MissedNames);
    }

    [Fact]
    public void APerIterationLoopCellIsPredicted()
    {
        // `let` in a for-head is a distinct binding per iteration, which is the case where the
        // rewrite creates the most cells and where an off-by-one in the layout would show.
        var l = Check("""
            var fs = [];
            for (let i = 0; i < 3; i++) { fs.push(function () { return i; }); }
            fs[0]() + fs[1]() + fs[2]();
            """);

        Assert.Equal(0, l.Missed);
        Assert.Equal(0, l.MissedNames);
    }

    [Fact]
    public void ACaptureInsideACatchAndAWithFreeBodyIsPredicted()
    {
        var l = Check("""
            function outer() {
              var q = 1;
              try { throw 2; } catch (e) {
                var inner = function () { return q + e; };
                return inner();
              }
            }
            outer();
            """);

        Assert.Equal(0, l.Missed);
        Assert.Equal(0, l.MissedNames);
    }

    [Fact]
    public void AShadowedNameIsNotPredictedAndIsNotCaptured()
    {
        // The over-approximation guard. `q` is mentioned and bound by the inner function, so
        // neither the prediction nor the rewrite should treat it as a capture. A checker that
        // predicted on mentions would report an over-approximation here.
        var l = Check("""
            function outer() {
              var q = 1;
              var inner = function (q) { return q; };
              return inner(2);
            }
            outer();
            """);

        Assert.Equal(0, l.Missed);
        Assert.Equal(0, l.MissedNames);
        Assert.Equal(0, l.Over);
    }

    [Fact]
    public void ASelfReferentialFUNCTIONDECLARATIONCapturesItsOwnName()
    {
        // **The defect the corpus run found, as a fixture.** A function DECLARATION's name is
        // bound in the ENCLOSING scope, so a self-reference reads through that binding and a
        // deferred body must be handed a box for it. FreeNameScan bound the name inside the
        // function as well — correct for a named function expression and wrong here — so every
        // self-referential declaration reported as capturing nothing. That was 138 sites across
        // five corpora and every remaining miss the checker had.
        var l = Check("""
            function outer() {
              function fact(n) { return n < 2 ? 1 : n * fact(n - 1); }
              return fact(5);
            }
            outer();
            """);

        Assert.True(l.Missed == 0, "missed: " + string.Join(", ", DeferredCaptureLayout.MissedNameSamples));
        Assert.Equal(0, l.MissedNames);
    }

    [Fact]
    public void ANamedFunctionEXPRESSIONSelfNameIsHandedInAndIsPredicted()
    {
        // **The one shape the corpus does not contain, and it took two attempts.** A named
        // function expression's own name is bound INSIDE the function by the specification, so
        // FreeNameScan is right to leave it out of the free set — and this engine gives it a
        // JSVariable parameter in the ENCLOSING scope which the body captures, so the layout has
        // to carry it anyway.
        //
        // The first attempt looked the name up in the function's own scope and tested
        // `Variable != null`, which is exactly the field this binding deliberately leaves null:
        // its own comment says it "is not a local Variable of this scope (it is captured
        // read-only), so it is exposed via EvalCaptureExpression only". Item 0097's rule, for
        // the third time and the first time deciding a mechanism rather than a measurement — ask
        // what the compiler BUILT, not what the analysis PROVED.
        var expression = Check("var f = function g(n) { return n < 1 ? 0 : g(n - 1); }; f(3);");
        var expressionMisses = string.Join(", ", DeferredCaptureLayout.MissedNameSamples);
        var declaration = Check("function g(n) { return n < 1 ? 0 : g(n - 1); } g(3);");

        Assert.True(expression.Missed == 0, "expression missed: " + expressionMisses);
        Assert.Equal(0, declaration.Missed);

        // And the two are predicted by DIFFERENT routes, which is why the fix is two conditions
        // rather than one: the declaration's name is free and resolves through the enclosing
        // scope, the expression's is not free and is added from its own scope. Asserted by
        // pairing so neither can pass by accident — both name exactly one binding here.
        Assert.Equal(declaration.PredictedNames, expression.PredictedNames);
    }

    [Fact]
    public void TheCheckerIsShownToCatchAMissBeforeItsZeroIsTrusted()
    {
        // **The fixture that makes every zero above mean something**, and §3.5's rule from 0096
        // applied to a checker rather than an emitter: a comparison that has never reported a
        // failure is a claim about the comparison.
        //
        // A miss is "the rewrite captured a binding the source-derived prediction did not name".
        // It cannot be produced from JavaScript — if the prediction were wrong on real source,
        // that would be the defect this exists to find — so it is produced directly: record a
        // prediction that deliberately omits everything, then check it against a non-empty actual
        // set. If that does not register a miss, the comparison is inert and every other test in
        // this file is vacuous.
        var previous = DeferredCaptureLayout.Checking;
        DeferredCaptureLayout.Checking = true;
        try
        {
            DeferredCaptureLayout.Reset();

            var captured = ExpressionCompiler.Expressions.BExpression.Parameter(typeof(object), "q");
            var lambda = ExpressionCompiler.Expressions.BExpression.Lambda(
                typeof(System.Action),
                ExpressionCompiler.Expressions.BExpression.Empty,
                "probe",
                []);

            DeferredCaptureLayout.Predict(lambda, []);
            DeferredCaptureLayout.Check(lambda, [captured]);

            Assert.Equal(1, DeferredCaptureLayout.MissedSites);
            Assert.Equal(1, DeferredCaptureLayout.MissedNames);
        }
        finally
        {
            DeferredCaptureLayout.Checking = previous;
        }
    }

    [Fact]
    public void SyntheticBindingsAreExcludedRatherThanCountedAsMisses()
    {
        // `this` and `arguments` are captured by the rewrite and are not identifiers a free-name
        // walk can see. They must be excluded, or every method in the corpus reads as a miss and
        // the go/no-go number is noise. The exclusion is narrow on purpose — anything it lets
        // through is charged to the prediction.
        var l = Check("""
            function Outer() {
              this.v = 1;
              var self = this;
              this.get = function () { return self.v + arguments.length; };
            }
            var o = new Outer();
            o.get();
            """);

        Assert.Equal(0, l.Missed);
        Assert.True(l.Synthetic >= 0);
    }
}
