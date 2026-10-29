using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/814 — BindingIdentifier
// early errors for the contextual keywords `let` and `await`.
//
//  * `let` is not a valid BoundName of a lexical declaration (let / const / using),
//    regardless of strict mode: `let let`, `const let`, `using let`, `for (let let of …)`.
//    It remains a valid `var` binding in sloppy mode.
//  * `await` is a reserved word inside an async function (or async generator) body, so it
//    cannot be a BindingIdentifier or parameter there. It remains a valid identifier in
//    sloppy, non-async, non-module code.
public class Issue814BindingIdentifierTests
{
    private static string ParseResult(string code)
    {
        using var ctx = new JSContext();
        var literal = "\"" + code.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        return ctx.Eval(
            $"var t='OK'; try {{ eval({literal}); }} catch (e) {{ t = e.constructor.name; }} t").ToString();
    }

    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // ---- `let` as a lexical binding name is a SyntaxError ----

    [Theory]
    [InlineData("let let = 1;")]
    [InlineData("const let = 1;")]
    [InlineData("{ using let = null; }")]
    [InlineData("for (let let of []) {}")]
    [InlineData("for (const let of []) {}")]
    public void LetAsLexicalBindingNameIsSyntaxError(string code)
        => Assert.Equal("SyntaxError", ParseResult(code));

    [Fact]
    public void LetAsVarBindingIsAllowedInSloppyMode()
        => Assert.Equal("5", Eval("var let = 5; '' + let"));

    [Fact]
    public void LetAsVarShorthandPatternIsAllowedInSloppyMode()
        => Assert.Equal("5", Eval("var { let } = { let: 5 }; '' + let"));

    [Fact]
    public void LetAsPropertyKeyIsAllowed()
        => Assert.Equal("1", Eval("var o = { let: 1 }; '' + o.let"));

    // ---- `await` as a binding name in async context is a SyntaxError ----

    [Theory]
    [InlineData("(async function () { let await = 1; })")]
    [InlineData("(async function () { const await = 1; })")]
    [InlineData("(async function () { var await = 1; })")]
    [InlineData("(async function (await) {})")]
    [InlineData("(async function () { using await = null; })")]
    [InlineData("(async function* () { let await = 1; })")]
    public void AwaitAsBindingNameInAsyncIsSyntaxError(string code)
        => Assert.Equal("SyntaxError", ParseResult(code));

    [Fact]
    public void AwaitAsVarBindingIsAllowedInSloppyNonAsync()
        => Assert.Equal("7", Eval("var await = 7; '' + await"));

    [Fact]
    public void AwaitAsBindingIsAllowedInNonAsyncNestedInAsync()
        => Assert.Equal("OK", ParseResult("(async function () { function g() { let await = 1; return await; } })"));

    [Fact]
    public void AwaitAsPropertyKeyIsAllowedInAsync()
        => Assert.Equal("OK", ParseResult("(async function () { var o = { await: 1 }; return o.await; })"));

    [Fact]
    public void AwaitAsOperatorStillWorksInAsync()
        => Assert.Equal("OK", ParseResult("(async function () { return await Promise.resolve(1); })"));

    // ---- existing rules unchanged ----

    [Theory]
    [InlineData("(function* () { let yield = 1; })")]   // yield reserved in a generator
    [InlineData("'use strict'; var let = 1;")]          // let reserved in strict mode
    [InlineData("'use strict'; var yield = 1;")]        // yield reserved in strict mode
    public void ExistingReservedBindingRulesStillHold(string code)
        => Assert.Equal("SyntaxError", ParseResult(code));
}
