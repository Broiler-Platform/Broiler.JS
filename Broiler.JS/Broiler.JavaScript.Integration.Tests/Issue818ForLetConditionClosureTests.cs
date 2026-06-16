using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/818 — Problem 13
// (test/language/statements/let/syntax/let-closure-inside-condition.js):
//   Test262Error: Expected SameValue(«0», «5») to be true.
//
// A `for (let …)` loop creates a fresh per-iteration binding so closures observe
// each iteration's value. Broiler already gave the loop *body* a fresh binding,
// but the loop *test* was evaluated against the single shared carrier — so a
// closure created in the condition captured the final value (all 5s) instead of
// the per-iteration value. The test is now evaluated against the body block's
// fresh per-iteration binding (injected as `if (!test) break;` at the top of the
// body), so condition closures capture the correct value while normal loops,
// break/continue (incl. labeled), completion values and the body binding are
// unchanged.
public class Issue818ForLetConditionClosureTests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // The exact test262 fixture shape.
    [Fact]
    public void ConditionClosureCapturesPerIterationBinding()
        => Assert.Equal("0,1,2,3,4", Eval(
            "let a = [];" +
            "for (let i = 0; a.push(function () { return i; }), i < 5; ++i) {}" +
            "var out = [];" +
            "for (let k = 0; k < 5; ++k) out.push(a[k]());" +
            "out.join(',')"));

    // The loop-terminating evaluation of the condition still runs (its closure is
    // pushed) before the loop exits, so `a` has one extra closure for i === 5.
    [Fact]
    public void ConditionRunsOnTheTerminatingIterationToo()
        => Assert.Equal("0,1,2,3,4,5", Eval(
            "let a = [];" +
            "for (let i = 0; a.push(function () { return i; }), i < 5; ++i) {}" +
            "a.map(function (f) { return f(); }).join(',')"));

    [Fact]
    public void BodyClosureStillCapturesPerIterationBinding()
        => Assert.Equal("0,1,2", Eval(
            "var a = [];" +
            "for (let i = 0; i < 3; i++) { a.push(function () { return i; }); }" +
            "a[0]() + ',' + a[1]() + ',' + a[2]()"));

    [Fact]
    public void ConditionAndBodyClosuresAreBothPerIteration()
        => Assert.Equal("0123|012", Eval(
            "var c = [], b = [];" +
            "for (let i = 0; c.push(function () { return i; }), i < 3; i++) { b.push(function () { return i; }); }" +
            "c.map(function (f) { return f(); }).join('') + '|' + b.map(function (f) { return f(); }).join('')"));

    [Theory]
    [InlineData("var s = 0; for (let i = 0; i < 5; i++) s += i; '' + s", "10")]
    [InlineData("var s = 0; for (let i = 0; i < 10; i++) { if (i === 3) break; s += i; } '' + s", "3")]
    [InlineData("var s = 0; for (let i = 0; i < 5; i++) { if (i % 2 === 0) continue; s += i; } '' + s", "4")]
    [InlineData("var c = 0; outer: for (let i = 0; i < 3; i++) { for (let j = 0; j < 3; j++) { if (j === 1) continue outer; c += 10 * i + j; } } '' + c", "30")]
    [InlineData("var r; for (let i = 0; i < 3; i++) r = i * 10; '' + r", "20")]
    [InlineData("function* g() { for (let i = 0; i < 3; i++) yield i; } [...g()].join(',')", "0,1,2")]
    public void NormalForLetSemanticsArePreserved(string code, string expected)
        => Assert.Equal(expected, Eval(code));
}
