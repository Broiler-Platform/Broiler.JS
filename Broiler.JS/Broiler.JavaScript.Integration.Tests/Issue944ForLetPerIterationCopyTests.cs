using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/Broiler-Platform/Broiler.JS/issues/944 — the seven
// test/language/statements/continue/* timeouts, all of which share the head shape
// `for (let x = 0; x < 10;)`: a `let` head with an EMPTY increment.
//
// ForBodyEvaluation (14.7.4.3) performs CreatePerIterationEnvironment AFTER the body and BEFORE the
// increment, so the next iteration is seeded from the values the body last saw. Broiler's desugar
// copied the head bindings into a per-iteration carrier but only ever advanced that carrier from the
// increment, so a mutation made by the BODY was discarded. With an empty increment nothing advanced
// the carrier at all and the loop re-read the seed value forever — the tests did not fail, they hung.
//
// The body now copies each per-iteration binding back into its carrier at the end of every iteration
// (in a `finally` when the body can `continue`, since `continue` jumps straight to the increment).
// That also fixes the non-empty-increment case, where a body-side mutation used to be lost:
// `for (let i = 0; i < 8; i += 2) { i += 1; }` visited 0,2,4,6 instead of 0,3,6.
public class Issue944ForLetPerIterationCopyTests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // ---- The seven test262 fixtures, verbatim in shape -----------------------------------------

    // test/language/statements/continue/no-label-continue.js
    [Fact(Timeout = 60000)]
    public void EmptyIncrementWithBareContinueTerminates()
        => Assert.Equal("10", Eval(
            "var count = 0;" +
            "for (let x = 0; x < 10;) { x++; count++; continue; }" +
            "count"));

    // test/language/statements/continue/simple-and-labeled.js
    [Fact(Timeout = 60000)]
    public void EmptyIncrementWithLabeledContinueTerminates()
        => Assert.Equal("10", Eval(
            "var count = 0;" +
            "label: for (let x = 0; x < 10;) { x++; count++; continue label; }" +
            "count"));

    // test/language/statements/continue/labeled-continue.js
    [Fact(Timeout = 60000)]
    public void LabeledContinueFromInnerWhileTerminates()
        => Assert.Equal("10", Eval(
            "var count = 0;" +
            "label: for (let x = 0; x < 10;) { while (true) { x++; count++; continue label; } }" +
            "count"));

    // test/language/statements/continue/shadowing-loop-variable-in-same-scope-as-continue.js
    // The copy-back reads the per-iteration binding, so an inner block that shadows the loop name
    // must not be the `x` that is written to the carrier.
    [Fact(Timeout = 60000)]
    public void ContinueFromBlockShadowingTheLoopVariableTerminates()
        => Assert.Equal("10", Eval(
            "var count = 0;" +
            "for (let x = 0; x < 10;) { x++; count++; { let x = 'hello'; continue; } }" +
            "count"));

    // test/language/statements/continue/nested-let-bound-for-loops-outer-continue.js
    [Fact(Timeout = 60000)]
    public void NestedEmptyIncrementLoopsOuterContinueTerminates()
        => Assert.Equal("20", Eval(
            "var count = 0;" +
            "for (let x = 0; x < 10;) { x++; for (let y = 0; y < 2;) { y++; count++; } continue; }" +
            "count"));

    // test/language/statements/continue/nested-let-bound-for-loops-inner-continue.js
    [Fact(Timeout = 60000)]
    public void NestedEmptyIncrementLoopsInnerContinueTerminates()
        => Assert.Equal("20", Eval(
            "var count = 0;" +
            "for (let x = 0; x < 10;) { x++; for (let y = 0; y < 2;) { y++; count++; continue; } }" +
            "count"));

    // test/language/statements/continue/nested-let-bound-for-loops-labeled-continue.js
    [Fact(Timeout = 60000)]
    public void NestedEmptyIncrementLoopsLabeledContinueTerminates()
        => Assert.Equal("20", Eval(
            "var count = 0;" +
            "outer: for (let x = 0; x < 10;) { x++; for (let y = 0; y < 2;) { y++; count++; if (y == 2) continue outer; } }" +
            "count"));

    // ---- The underlying rule: a body-side mutation reaches the next iteration ------------------

    // The plain (continue-free) shape takes the cheaper trailing-copy lowering rather than a
    // per-iteration try/finally, so it needs its own coverage.
    [Fact(Timeout = 60000)]
    public void EmptyIncrementAdvancedByTheBodyTerminates()
        => Assert.Equal("0,1,2", Eval(
            "var out = [];" +
            "for (let x = 0; x < 3;) { out.push(x); x++; }" +
            "out.join(',')"));

    // CreatePerIterationEnvironment runs BEFORE the increment, so the increment sees the body's
    // mutation: 0 → body makes it 1 → ++ → 2 → body makes it 3 → ++ → 4, which fails the test.
    [Fact(Timeout = 60000)]
    public void IncrementSeesTheBodysMutation()
        => Assert.Equal("0,2", Eval(
            "var out = [];" +
            "for (let x = 0; x < 3; x++) { out.push(x); x++; }" +
            "out.join(',')"));

    [Fact(Timeout = 60000)]
    public void CompoundIncrementSeesTheBodysMutation()
        => Assert.Equal("0,3,6", Eval(
            "var out = [];" +
            "for (let i = 0; i < 8; i += 2) { out.push(i); i += 1; }" +
            "out.join(',')"));

    // Only the mutated binding advances; a second head binding driven by the increment is unaffected.
    [Fact(Timeout = 60000)]
    public void MultipleHeadBindingsCopyIndependently()
        => Assert.Equal("0/100,1/101,2/102", Eval(
            "var out = [];" +
            "for (let i = 0, j = 100; i < 3; j++) { out.push(i + '/' + j); i++; }" +
            "out.join(',')"));

    // A destructuring head binds several names per declarator; every one needs its own copy-back.
    [Fact(Timeout = 60000)]
    public void DestructuringHeadWithEmptyIncrementTerminates()
        => Assert.Equal("0:10,1:9,2:8", Eval(
            "var out = [];" +
            "for (let [a, b] = [0, 10]; a < 3;) { out.push(a + ':' + b); a++; b--; }" +
            "out.join(',')"));

    // `continue` inside a body-level try/finally still reaches the copy-back exactly once.
    [Fact(Timeout = 60000)]
    public void ContinueThroughABodyFinallyTerminates()
        => Assert.Equal("3", Eval(
            "var n = 0;" +
            "for (let x = 0; x < 3;) { try { x++; continue; } finally { n++; } }" +
            "n"));

    // ---- Closure identity is unchanged by the copy-back ----------------------------------------

    // Each iteration still has its own binding; the carrier copy must not merge them.
    [Fact(Timeout = 60000)]
    public void ClosuresStillCapturePerIterationBindings()
        => Assert.Equal("0,1,2", Eval(
            "var f = [];" +
            "for (let x = 0; x < 3; x++) { f.push(function () { return x; }); }" +
            "f.map(function (g) { return g(); }).join(',')"));

    // A closure captures the value its own iteration ended with, not the seed it started from.
    [Fact(Timeout = 60000)]
    public void ClosuresCaptureTheBodysMutation()
        => Assert.Equal("1,2,3,4", Eval(
            "var f = [];" +
            "for (let x = 0; x < 4;) { x++; f.push(function () { return x; }); continue; }" +
            "f.map(function (g) { return g(); }).join(',')"));

    // A closure created in the increment shares the environment the following test and body use
    // (the increment-at-top lowering), which also relies on the copy-back.
    [Fact(Timeout = 60000)]
    public void IncrementClosureStillSharesThePerIterationEnvironment()
        => Assert.Equal("0,1,2", Eval(
            "var f = [];" +
            "for (let x = 0; x < 3; (function () { return 0; })(), x++) { f.push(function () { return x; }); }" +
            "f.map(function (g) { return g(); }).join(',')"));

    // A hoisted function declaration in the body also sees the mutated per-iteration value.
    [Fact(Timeout = 60000)]
    public void HoistedBodyFunctionSeesTheMutatedBinding()
        => Assert.Equal("1,2,3", Eval(
            "var f = [];" +
            "for (let x = 0; x < 3;) { x++; function g() { return x; } f.push(g); }" +
            "f.map(function (h) { return h(); }).join(',')"));

    // ---- Untouched neighbours ------------------------------------------------------------------

    // A `const` head is instantiated once and never copied, so it keeps the original lowering.
    [Fact(Timeout = 60000)]
    public void ConstHeadIsUnaffected()
        => Assert.Equal("0,0", Eval(
            "var out = [];" +
            "for (const x = 0; out.length < 2;) { out.push(x); }" +
            "out.join(',')"));

    // `var` heads have no per-iteration environment at all.
    [Fact(Timeout = 60000)]
    public void VarHeadWithEmptyIncrementIsUnaffected()
        => Assert.Equal("0,1,2", Eval(
            "var out = [];" +
            "for (var x = 0; x < 3;) { out.push(x); x++; }" +
            "out.join(',')"));

    // `break` leaves the loop, so the copy-back must not leak the loop binding outwards.
    [Fact(Timeout = 60000)]
    public void BreakDoesNotLeakTheLoopBinding()
        => Assert.Equal("outer", Eval(
            "let x = 'outer';" +
            "for (let x = 0; x < 3; x++) { break; }" +
            "x"));

    // A closure created in the initializer resolves the single loop binding, not a carrier.
    [Fact(Timeout = 60000)]
    public void InitializerClosureStillResolvesTheLoopBinding()
        => Assert.Equal("5", Eval(
            "let save;" +
            "for (let outer = (save = function () { return outer; }, 5); ;) break;" +
            "String(save())"));

    // `return` out of the loop is unaffected by the end-of-iteration copy.
    [Fact(Timeout = 60000)]
    public void ReturnFromTheLoopBodyIsUnaffected()
        => Assert.Equal("4", Eval(
            "(function () { for (let x = 0; x < 9;) { x++; if (x === 4) return x; } return -1; })()"));
}
