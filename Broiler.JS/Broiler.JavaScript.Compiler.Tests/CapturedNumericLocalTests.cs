using System;
using Broiler.JavaScript.Compiler;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Compiler.Tests;

// A numeric local a nested function captures is held in a raw CLR double rather than a
// JSVariable cell (docs/performance-roadmap.md item 3-7). The sharing a closure needs survives
// because a captured CLR local is rewritten into a `Box<double>`, and that box IS the shared
// cell — one allocation where the JSVariable form is two.
//
// What does NOT survive is the hoisting argument the numeric tier rests on. Every other
// condition is textual: a name with any reference before its declaration is refused, so the
// initializer has always run by the time anything reads it and a raw double hoisted to 0 is
// never observed where `undefined` belongs. Capture breaks the link between text order and
// execution order in two ways, and both were found by running them rather than by reading:
//
//   * a function DECLARATION at body top level exists before the body runs, so its mention of
//     the name can execute before the name's initializer while sitting textually after it; and
//   * a declaration INSIDE a nested function — a parameter, a var — is a different binding, so
//     letting it mark the outer name initialized masks a read that really does see `undefined`.
//
// Both produced wrong answers on a build with only the gate widened, so the first half of this
// file is those cases. Every assertion here holds on BOTH settings of the switch, which is what
// makes them a regression guard rather than a description of the optimization: the values are
// the same either way, and only the counts move.
[Collection(Phase3DiagnosticsCollection.Name)]
public sealed class CapturedNumericLocalTests
{
    /// <summary>Runs <paramref name="source"/> under an explicit setting of the switch.</summary>
    private static string Eval(string source, bool captured)
    {
        var previous = CapturedNumericLocals.Enabled;
        CapturedNumericLocals.Enabled = captured;
        try
        {
            using var context = new JSContext();
            return context.Eval(source).ToString();
        }
        finally
        {
            CapturedNumericLocals.Enabled = previous;
        }
    }

    /// <summary>Compiles and runs a function body, reporting how many locals became doubles.</summary>
    private static (string Result, long NumericLocals) Compile(string body, bool captured)
    {
        var previous = CapturedNumericLocals.Enabled;
        CapturedNumericLocals.Enabled = captured;
        try
        {
            using var context = new JSContext();
            CompilerSpecializationDiagnostics.Reset();
            var result = context.Eval("(function(){ " + body + " })()").ToString();
            return (result, CompilerSpecializationDiagnostics.Snapshot().NumericLocals);
        }
        finally
        {
            CapturedNumericLocals.Enabled = previous;
        }
    }

    // ── the two hazards the widening exposes ──────────────────────────────────────────

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AHoistedDeclarationCanReadTheBindingBeforeItsInitializer(bool captured)
    {
        // `g` exists from function entry, so `g()` runs before `var s = 0` does — from a
        // position the analysis accepts, because g's BODY is textually after the declaration.
        // Measured at 0 instead of undefined on a build with only the gate widened.
        Assert.Equal(
            "undefined",
            Eval("(function () { var r = g(); var s = 0; function g() { return s; } return String(r); })()", captured));

        // Same shape one level down: `g` calls `inner`, which reads the binding.
        Assert.Equal(
            "undefined",
            Eval("(function () { var r = g(); var s = 0; function g() { return inner(); function inner() { return s; } } return String(r); })()", captured));

        // A labelled function declaration hoists exactly like an unlabelled one (Annex B).
        Assert.Equal(
            "undefined",
            Eval("(function () { var r = g(); var s = 0; l: function g() { return s; } return String(r); })()", captured));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AFunctionExpressionCannotReadItBecauseItDoesNotExistYet(bool captured)
        // The counterpart that says the rule above is about hoisting and not about closures:
        // a function EXPRESSION is created when its statement runs, which is after the
        // declaration's, so calling it earlier is a TypeError on undefined and never a read.
        => Assert.Equal(
            "TypeError",
            Eval("""
                (function () {
                    var out;
                    try { out = String(g()); } catch (e) { out = e.constructor.name; }
                    var s = 0;
                    var g = function () { return s; };
                    return out;
                })()
                """, captured));

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ADeclarationInsideANestedFunctionDoesNotInitializeTheOuterBinding(bool captured)
    {
        // The nested function's PARAMETER is named `t`, which is a different binding — it may
        // not mark the outer `t` readable, or the read below is masked. 0 instead of undefined
        // on a build with only the gate widened.
        Assert.Equal(
            "undefined,5",
            Eval("""
                (function () {
                    var r;
                    { var g = function (t) { return t; }; r = String(t); var t = 5; }
                    return r + ',' + t;
                })()
                """, captured));

        // The same through a nested function's own `var`.
        Assert.Equal(
            "undefined,5",
            Eval("""
                (function () {
                    var r;
                    { var g = function () { var t = 1; return t; }; r = String(t); var t = 5; }
                    return r + ',' + t;
                })()
                """, captured));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AFunctionDeclarationStoresAFunctionIntoTheBindingBeingTyped(bool captured)
    {
        // A declaration is not an assignment expression, so the walk never saw this store. It
        // was covered by accident: the declaration mentions its own name, so the name counted
        // as captured and was refused. `let f = 5; { function f(){} }` reaches the same binding
        // through Annex B's copy-out and failed to compile at all once the refusal was lifted
        // ("Assignment target Call is not supported").
        Assert.Equal("5", Eval("(function () { let f = 5; { function f() {} } return f; })()", captured));
        Assert.Equal("number", Eval("(function () { var w = 1; function w() {} return typeof w; })()", captured));
        Assert.Equal("undefined,7", Eval("""
            (function () {
                var before = q;
                switch (1) { case 1: function q() { return 7; } }
                return '' + before + ',' + q();
            })()
            """, captured));
    }

    // ── ordinary capture, which is what the item is for ───────────────────────────────

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ACapturedNumericLocalObservesTheLiveValue(bool captured)
    {
        Assert.Equal("45", Eval("(function () { var s = 0; var g = function () { return s; }; for (var i = 0; i < 10; i++) s += i; return g(); })()", captured));
        Assert.Equal("2", Eval("(function () { var i = 0; var f = function () { i++; }; f(); f(); return i; })()", captured));
        Assert.Equal("10", Eval("(function () { var v = 0; var w = function () { v = v + 5; }; var r = function () { return v; }; w(); w(); return r(); })()", captured));
        Assert.Equal("7", Eval("(function () { var s = 0; var r = function () { return s; }; var w = function (x) { s = x; }; w(7); return r(); })()", captured));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void EachActivationGetsItsOwnBindingAndEachLoopOnlyOne(bool captured)
    {
        // One box per activation: two closures from two calls must not share.
        Assert.Equal("1,2,1", Eval("""
            (function () {
                function make() { var v = 0; return function () { v = v + 1; return v; }; }
                var a = make(), b = make();
                return a() + ',' + a() + ',' + b();
            })()
            """, captured));

        // ...and exactly one box for a `var` a loop closes over, since a var is function-scoped.
        Assert.Equal("3,3,3", Eval("""
            (function () {
                var fs = [];
                for (var i = 0; i < 3; i++) { fs.push(function () { return i; }); }
                return fs[0]() + ',' + fs[1]() + ',' + fs[2]();
            })()
            """, captured));

        // Recursion: an inner activation must not reach the outer one's binding.
        Assert.Equal("3:2:1:0:0", Eval("""
            (function () {
                function f(n) {
                    var v = n;
                    var r = function () { return v; };
                    return r() + ':' + (n > 0 ? f(n - 1) : 0);
                }
                return f(3);
            })()
            """, captured));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AClosureThatStoresSomethingElseStillDefeatsTheProof(bool captured)
    {
        // The analysis descends into nested functions and conflates names, so a write there is
        // recorded and drops the candidate. These are the cases that say so.
        Assert.Equal("string:x", Eval("(function () { var s = 0; var g = function () { s = 'x'; }; g(); return typeof s + ':' + s; })()", captured));
        Assert.Equal("object", Eval("(function () { var s = 0; var g = function () { s = {}; }; g(); return typeof s; })()", captured));
        Assert.Equal("undefined", Eval("(function () { var s = 0; var g = function () { s = undefined; }; g(); return String(s); })()", captured));
        // ...including through a destructuring pattern, which has no identifier in target position.
        Assert.Equal("string", Eval("(function () { var s = 0; var g = function (o) { ({ a: s } = o); }; g({ a: 'x' }); return typeof s; })()", captured));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SuspendingNestedFunctionsCaptureThroughTheSameBox(bool captured)
    {
        // A generator and an async body are rewritten into state machines by a different path
        // than an ordinary lambda, so the capture is worth asserting rather than assuming.
        Assert.Equal("0,1,1", Eval("""
            (function () {
                var v = 0;
                var gen = function* () { yield v; v = v + 1; yield v; };
                var it = gen();
                var a = it.next().value, b = it.next().value;
                return a + ',' + b + ',' + v;
            })()
            """, captured));

        // An async body runs synchronously up to its first `await`, which is how the captured
        // value is observed without a microtask drain (the idiom Issue719Tests uses). First with
        // no await at all, then with one, so the suspending rewrite is the thing under test.
        Assert.Equal("2,2", Eval("""
            (function () {
                var out = 'no';
                var v = 1;
                var f = async function () { v = v + 1; out = v; };
                f();
                return String(out) + ',' + v;
            })()
            """, captured));

        Assert.Equal("2,2", Eval("""
            (function () {
                var out = 'no';
                var v = 1;
                var f = async function () { v = v + 1; out = v; await 0; v = v + 10; };
                f();
                return String(out) + ',' + v;
            })()
            """, captured));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ControlFlowAroundTheWriteIsUnaffected(bool captured)
    {
        Assert.Equal("112", Eval("(function () { var v = 1; var r = function () { return v; }; try { v = v + 1; throw 0; } catch (e) { v = v + 10; } finally { v = v + 100; } return r(); })()", captured));
        Assert.Equal("9", Eval("(function () { let v = 2; const k = 3; var r = function () { return v * k; }; v = v + 1; return r(); })()", captured));
        Assert.Equal("NaN", Eval("(function () { var v = 0/0; var r = function () { return v; }; return String(r()); })()", captured));
        Assert.Equal("-Infinity", Eval("(function () { var v = -1/0; var r = function () { return v; }; return String(r()); })()", captured));
        Assert.Equal("-0", Eval("(function () { var v = -0; var r = function () { return v; }; return (1 / r()) < 0 ? '-0' : '0'; })()", captured));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ADynamicNestedFunctionStillRefusesTheWholeFunction(bool captured)
    {
        // A nested function that can resolve a name it never mentions disqualifies the enclosing
        // function outright, exactly as before — the widening is about which NAMES are refused,
        // not about which functions are eligible.
        var (result, numericLocals) = Compile("function h() { return eval('1'); } var s = 0; var g = function () { return s; }; for (var i = 0; i < 10; i++) s += i; return s;", captured);
        Assert.Equal("45", result);
        Assert.Equal(0, numericLocals);
    }

    // ── the counts, which are the half the switch actually moves ──────────────────────

    [Fact]
    public void ACapturedNumericLocalReachesTheTierOnlyWhenTheWideningIsOn()
    {
        const string Body = "var s = 0; var g = function () { return s; }; for (var i = 0; i < 10; i++) s += i; return s;";

        var widened = Compile(Body, captured: true);
        var narrow = Compile(Body, captured: false);

        Assert.Equal("45", widened.Result);
        Assert.Equal("45", narrow.Result);
        // `i` reaches the tier either way; `s` only when capture stops being a refusal.
        Assert.Equal(2, widened.NumericLocals);
        Assert.Equal(1, narrow.NumericLocals);
    }

    [Fact]
    public void AHoistedDeclarationRefusesTheNameOnBothSettings()
    {
        // The same body with the closure written as a declaration instead of an expression.
        // This is the conjunct that cost 247 of the corpus's 478 captured names, and it is a
        // correctness rule rather than a policy, so the switch does not reach it.
        const string Body = "var s = 0; function g() { return s; } for (var i = 0; i < 10; i++) s += i; return s;";

        Assert.Equal(1, Compile(Body, captured: true).NumericLocals);
        Assert.Equal(1, Compile(Body, captured: false).NumericLocals);
    }

    [Fact]
    public void TheJSValueTierStaysClosedToCapturedNames()
    {
        // Widening the NUMERIC tier does not widen the other one: a captured name the analysis
        // cannot prove numeric keeps its JSVariable, because that tier has no cell at all and a
        // cell is what a TDZ, a const's TypeError and a deleted eval binding are. `s` here is
        // assigned a string, so it is scalar-eligible but not numeric.
        var (result, numericLocals) = Compile(
            "var s = 'a'; var g = function () { return s; }; s = s + 'b'; return g();",
            captured: true);

        Assert.Equal("ab", result);
        Assert.Equal(0, numericLocals);
    }

    [Fact]
    public void TheHelpersRestoreTheSwitchWhicheverWayItWasSet()
    {
        // Guards the fixture itself: every test above sets the flag and restores it, so a leak
        // would make whichever test ran next report the other arm's counts. Asserted against
        // the AMBIENT value rather than against `true`, because the switch reads an environment
        // variable and this suite is run on both arms.
        var ambient = CapturedNumericLocals.Enabled;

        Eval("(function () { var v = 1; var g = function () { return v; }; return g(); })()", captured: true);
        Assert.Equal(ambient, CapturedNumericLocals.Enabled);

        Compile("var v = 1; var g = function () { return v; }; return g();", captured: false);
        Assert.Equal(ambient, CapturedNumericLocals.Enabled);
    }
}
