using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for a call whose callee is a parenthesised optional chain: `(o?.m)(…)`. The parens
// close the chain, so where it short-circuits its value is `undefined` -- and that is what is called, a
// TypeError thrown after the arguments are evaluated. Broiler.JS answered `undefined` instead, without
// evaluating the arguments, because the call reused the short-circuit an unparenthesised `o?.m()` has.
// It also unwrapped the short-circuit of a call that is optional itself, `(n?.m)?.()`, before the chain
// that call opens could carry it on: `(n?.m)?.().c` read `c` off undefined.
//
// Where the chain runs to its end, the method is called on the object it was read from
// (test262 optional-chaining/optional-call-preserves-this), and that is unchanged.
//
// Every expectation was measured in Chromium first. The tests run inside function bodies, as the
// compiler's temps behave differently at a program's top level.
public class ChainBoundaryCallTests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // `run(f)` answers what f returned, or the name of what it threw, and then what was logged while it
    // ran: `arg` and `k` log their argument, so the log shows which operands were evaluated.
    private const string Fixture =
        "var log = []; function arg(t) { log.push(t); return t; } function k(t) { log.push(t); return 'b'; }" +
        " var o = { name: 'o', b: function () { return this && this.name; }," +
        " a: { name: 'o.a', b: function () { return this && this.name; } }," +
        " s: function () { return [].slice.call(arguments).join(','); } };" +
        " var n = null, z = { a: null };" +
        " function run(f) { log = []; try { return String(f()) + ' [' + log.join(',') + ']'; }" +
        " catch (e) { return 'threw ' + e.name + ' [' + log.join(',') + ']'; } }";

    private static string Run(string expression) =>
        Eval(Fixture + " (function () { return run(function () { return " + expression + "; }); })()");

    [Theory]
    // The chain short-circuits: its arguments are evaluated, the key of the link it skipped is not, and
    // the call throws.
    [InlineData("(n?.b)(arg('x'))", "threw TypeError [x]")]
    [InlineData("(n?.a.b)(arg('x'))", "threw TypeError [x]")]
    [InlineData("(n?.[k('key')])(arg('x'))", "threw TypeError [x]")]
    // A link that is nullish without `?.` is not a short-circuit: reading past it throws, before the
    // arguments, as it always did.
    [InlineData("(z?.a.b)(arg('x'))", "threw TypeError []")]
    public void CallingAChainThatShortCircuitedThrowsAfterTheArguments(string expression, string expected)
        => Assert.Equal(expected, Run(expression));

    [Theory]
    [InlineData("(o?.b)()", "o []")]
    [InlineData("(o?.a.b)()", "o.a []")]
    [InlineData("(o?.a?.b)()", "o.a []")]
    [InlineData("(o?.[k('key')])()", "o [key]")]
    [InlineData("(o?.s)(...[1, 2])", "1,2 []")]
    [InlineData("(o?.b)?.()", "o []")]
    public void AChainThatRunsToItsEndCallsTheMethodOnItsObject(string expression, string expected)
        => Assert.Equal(expected, Run(expression));

    [Theory]
    // A call that is optional itself short-circuits on its undefined callee before its arguments, and the
    // chain it opens goes on short-circuiting past it -- as an unparenthesised chain does.
    [InlineData("(n?.b)?.(arg('x'))", "undefined []")]
    [InlineData("(n?.b)?.().c", "undefined []")]
    [InlineData("(n?.b)?.(arg('x')).c.d", "undefined []")]
    [InlineData("n?.b(arg('x'))", "undefined []")]
    public void AnOptionalCallStillShortCircuits(string expression, string expected)
        => Assert.Equal(expected, Run(expression));

    [Fact(Timeout = 600000)]
    public void APrivateMethodInAParenthesisedChain()
        => Assert.Equal("self [] | threw TypeError []", Eval(
            Fixture +
            " class C { #m() { return this === c ? 'self' : 'other'; } t(x) { return (x?.#m)(); } }" +
            " var c = new C(); run(function () { return c.t(c); }) + ' | ' + run(function () { return c.t(null); })"));

    [Fact(Timeout = 600000)]
    public void TheSameHoldsInAGeneratorAndInAnAsyncFunction()
    {
        // The arguments of a generator's call are where it suspends: the call throws once they are in.
        Assert.Equal("TypeError", Eval(
            Fixture +
            " function* g() { return (n?.b)(yield 1); } var it = g(); it.next();" +
            " (function () { try { it.next(2); return 'no throw'; } catch (e) { return e.name; } })()"));

        using var ctx = new JSContext();
        ctx.Eval("globalThis.r = '<unset>';");
        ctx.Execute(Fixture +
            " (async function () { var first = (o?.b)(await Promise.resolve(1));" +
            " try { (n?.b)(await Promise.resolve(2)); globalThis.r = first + ' no throw'; }" +
            " catch (e) { globalThis.r = first + ' ' + e.name; } })();");
        Assert.Equal("o TypeError", ctx.Eval("'' + globalThis.r").ToString());
    }
}
