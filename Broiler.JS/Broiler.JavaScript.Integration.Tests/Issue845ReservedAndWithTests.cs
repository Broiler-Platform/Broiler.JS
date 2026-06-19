using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/845:
//   * A bare `super` (not part of a property access or call) is a SyntaxError, which —
//     with the earlier reserved-word fixes — fully closes sm/misc/future-reserved-words
//     (Problem 47) for the always-reserved words.
//   * A destructuring-assignment target that is a strict-reserved word / eval / arguments
//     is a SyntaxError in strict mode (`'use strict'; ({ implements } = x)`), closing the
//     strict-reserved part of Problem 47.
//   * Reading `arguments` inside a `with` resolves against the with-object first (the
//     read counterpart to the previously-fixed `delete arguments` in a `with`).
public class Issue845ReservedAndWithTests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    private static string ErrorOf(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval($"(function(){{ try {{ eval({System.Text.Json.JsonSerializer.Serialize(code)}); return 'no throw'; }} catch (e) {{ return e.constructor.name; }} }})()").ToString();
    }

    // ---- bare super ----

    [Theory]
    [InlineData("super = 1;")]
    [InlineData("var x = super;")]
    [InlineData("super;")]
    public void BareSuperIsSyntaxError(string code)
        => Assert.Equal("SyntaxError", ErrorOf(code));

    [Fact]
    public void SuperPropertyAndCallStillWork()
        => Assert.Equal("ok",
            Eval("class C extends Object { constructor(){ super(); this.t = super.toString; } m(){ return super['toString']; } } var c = new C(); (typeof c.t === 'function' && typeof c.m() === 'function') ? 'ok' : 'no'"));

    // ---- strict-mode destructuring assignment target ----

    [Theory]
    [InlineData("'use strict'; ({ implements } = {});")]
    [InlineData("'use strict'; ({ yield } = {});")]
    [InlineData("'use strict'; [ static ] = [];")]
    [InlineData("'use strict'; ({ eval } = {});")]
    public void StrictDestructuringRestrictedTargetIsSyntaxError(string code)
        => Assert.Equal("SyntaxError", ErrorOf(code));

    [Theory]
    // Allowed in sloppy code, and a non-restricted strict target is fine.
    [InlineData("({ implements } = { implements: 1 });", "no throw")]
    [InlineData("'use strict'; var x; ({ x } = { x: 5 });", "no throw")]
    public void DestructuringAllowedWhenNotRestricted(string code, string expected)
        => Assert.Equal(expected, ErrorOf(code));

    // ---- reading arguments inside a with ----

    [Fact]
    public void ArgumentsInWithResolvesWithObjectProperty()
        => Assert.Equal("42",
            Eval("(function(){ var o = { arguments: 42 }; with (o) { return arguments; } })()"));

    [Theory]
    // The with-object lacks `arguments`, so it falls back to the function's arguments object.
    [InlineData("(function(){ with ({}) { return arguments.length; } })(1, 2, 3)", "3")]
    [InlineData("(function(){ return arguments.length; })(1, 2)", "2")]
    public void ArgumentsFallsBackToBinding(string code, string expected)
        => Assert.Equal(expected, Eval(code));
}
