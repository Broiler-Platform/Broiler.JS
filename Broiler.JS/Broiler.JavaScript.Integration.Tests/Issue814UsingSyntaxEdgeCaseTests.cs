using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/814 — `using` /
// `await using` (Explicit Resource Management) syntax edge cases, including Problem 54
// (using-invalid-arraybindingpattern-does-not-break-element-access).
//
// Two corrections:
//  1. Every `using` / `await using` binding requires an Initializer, so `using x;`,
//     `using a = null, b;` and `await using x;` are SyntaxErrors (a bare `using x;` was
//     previously accepted).
//  2. `using` is a contextual keyword: when not followed (same line) by a BindingIdentifier
//     it is an ordinary IdentifierReference — `using;`, `using = 1`, `using.x`, `using()`,
//     and crucially `using[i]` element access. The parser consumed `using` before deciding
//     and never fell back to the expression path, so these threw spuriously.
public class Issue814UsingSyntaxEdgeCaseTests
{
    // The runtime/parse error name of evaluating `code`, or "OK" if it completes. A nested
    // eval lets the outer try/catch observe a parse-time SyntaxError of the inner code.
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

    // ---- initializer is required ----

    [Theory]
    [InlineData("{ using x; }")]
    [InlineData("{ using a = null, b; }")]
    [InlineData("{ using a, b = null; }")]
    public void UsingWithoutInitializerIsSyntaxError(string code)
        => Assert.Equal("SyntaxError", ParseResult(code));

    [Theory]
    [InlineData("(async function(){ await using x; })")]
    [InlineData("(async function(){ await using a = null, b; })")]
    public void AwaitUsingWithoutInitializerIsSyntaxError(string code)
        => Assert.Equal("SyntaxError", ParseResult(code));

    [Fact]
    public void ValidUsingWithInitializerWorks()
        => Assert.Equal("d", Eval(
            "(function () { var log = []; { using x = { [Symbol.dispose]() { log.push('d'); } }; } return log.join(','); })()"));

    [Fact]
    public void ValidUsingMultipleInitializersWork()
        => Assert.Equal("ok", Eval("(function () { using a = null, b = null; return 'ok'; })()"));

    // ---- `using` as a contextual identifier ----

    [Fact]
    public void UsingAsVariableReference()
        => Assert.Equal("5", Eval("var using = 5; '' + using"));

    [Fact]
    public void UsingAsAssignmentTarget()
        => Assert.Equal("9", Eval("var using; using = 9; '' + using"));

    [Fact]
    public void UsingAsMemberBase()
        => Assert.Equal("42", Eval("var using = { a: 42 }; '' + using.a"));

    [Fact]
    public void UsingAsCallee()
        => Assert.Equal("99", Eval("var using = () => 99; '' + using()"));

    // Problem 54: `using[i]` is element access, not a using declaration with a pattern.
    [Fact]
    public void UsingElementAccessIsNotADeclaration()
        => Assert.Equal("20", Eval("var using = [10, 20, 30]; '' + using[1]"));

    [Fact]
    public void UsingElementAssignmentWorks()
        => Assert.Equal("7", Eval("(function () { var using = [0]; { using[0] = 7; } return '' + using[0]; })()"));

    // ---- still-correct rejections ----

    [Theory]
    [InlineData("for (using x in {}) {}")]          // using not allowed in for-in
    [InlineData("for (using x = null; false;) {}")] // using not allowed in C-style for head
    public void UsingInDisallowedForHeadIsSyntaxError(string code)
        => Assert.Equal("SyntaxError", ParseResult(code));
}
