using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for a method call whose COMPUTED KEY holds a member call, in ordinary code:
// `o[f.key()]()`, `o[(f.g(), 'm')]()`. The receiver and the resolved method live in temps taken
// from a per-function pool, and a nested member call is handed the same pair back. That is safe
// for an argument, whose evaluation starts only once both values are on the evaluation stack,
// but not for the key: it runs after the receiver is assigned and before the invocation reads
// the receiver back as the call's `this`. So the outer call found the right method and ran it on
// the nested call's receiver. SuspendedCallArgumentTests covers the same window inside a
// generator or async body, where it was fixed first and where every operand is in it.
//
// It was found in reCAPTCHA. Its transpiled async functions end a state with
// `ctx[(cond ? F[15](48, ...) : ..., 'm')]()` -- the context's jumpToEnd behind a computed key
// whose first operand is a table call. The jump set `S = 0` on the table instead of the context,
// so the state machine's `while (ctx.S)` ran the same state again, forever, inside one microtask:
// the click on the checkbox froze the page and grew its memory without bound.
public class ComputedKeyCallReceiverTests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // In a function body, where reCAPTCHA's calls were: script code at the top level of a
    // program did not show the defect, so a test written there passes either way.
    private static string InFunction(string expression)
        => Eval(Objects + "(function () { return " + expression + "; })()");

    // `m` answers the name of the object it ran on, so a wrong receiver answers 'f' (or 'p').
    private const string Objects =
        "var o = { name: 'o', m: function () { return this && this.name; } }; "
        + "var f = { name: 'f', key: function () { return 'm'; }, g: function () { return 1; } }; "
        + "var p = { name: 'p', m: function () { return 'm'; } }; ";

    [Theory]
    [InlineData("o[f.key()]()")]
    [InlineData("o[(f.g(), 'm')]()")]
    [InlineData("o[f.g() ? 'm' : 'x']()")]
    [InlineData("o[`${f.key()}`]()")]
    [InlineData("o[['m'].join('')]()")]
    [InlineData("o[f.key()](f.key())")]
    [InlineData("o[p[f.key()]()]()")]
    [InlineData("(() => o[f.key()]())()")]
    public void AMemberCallInAComputedKeyIsNotTheCallsReceiver(string call)
        => Assert.Equal("o", InFunction(call));

    [Theory]
    [InlineData("o?.[f.key()]()")]
    [InlineData("o[f.key()]?.()")]
    public void TheSameHoldsForAnOptionalCall(string call)
        => Assert.Equal("o", InFunction(call));

    [Fact(Timeout = 600000)]
    public void AMethodReachedThroughThisAndAComputedKeyRunsOnThis()
        => Assert.Equal("true", Eval(
            "var k = { name: function () { return 'm'; } }; "
            + "class C { m() { return this; } go() { return this[k.name()](); } } "
            + "var c = new C(); '' + (c.go() === c)"));

    /// <summary>
    /// The shape reCAPTCHA's transpiled async functions end a state with, reduced: a program
    /// function stepped by <c>while (ctx.S)</c>, whose second state jumps to the end through a
    /// computed key that calls into a table first. The jump has to reach the context, or the
    /// loop never ends; the guard stops it at ten runs if it does not.
    /// </summary>
    [Fact(Timeout = 600000)]
    public void AStateMachinesJumpThroughAComputedKeyReachesItsContext()
        => Assert.Equal("2:0", Eval(
            "function Ctx() { this.S = 1; } "
            + "Ctx.prototype.v = function (x, s) { this.S = s; return { value: x }; }; "
            + "Ctx.prototype.m = function () { this.S = 0; }; "
            + "var F = [function () { return 1; }]; var runs = 0; "
            + "function program(M, R) { runs++; R = ['m']; if (M.S == 1) return M.v(0, 2); "
            + "M[(runs > 0 ? F[0](48) : 0, R[0])](); } "
            + "var ctx = new Ctx(); var guard = 0; "
            + "while (ctx.S && guard++ < 10) program(ctx); "
            + "runs + ':' + ctx.S"));

    [Fact(Timeout = 600000)]
    public void EvaluationOrderIsUnchanged()
        // The receiver, then the key, then each argument, then the call -- and the call still
        // runs on the receiver, with the key's call and the argument's call each run once.
        => Assert.Equal("recv,key,arg,call:R", Eval(
            "var out = []; "
            + "var s = { key: function () { out.push('key'); return 'm'; }, "
            + "arg: function () { out.push('arg'); return 1; } }; "
            + "function recv() { out.push('recv'); "
            + "return { tag: 'R', m: function () { out.push('call:' + this.tag); } }; } "
            + "(function () { recv()[s.key()](s.arg()); })(); out.join(',')"));
}
