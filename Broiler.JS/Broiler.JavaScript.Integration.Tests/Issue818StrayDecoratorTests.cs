using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/818 — Problem 11
// (test/staging/sm/class/compPropNames.js and test/staging/sm/class/methDefn.js):
//   Test262Error: Expected a SyntaxError to be thrown but no exception was thrown.
//
// Both files assert that a stray `@` in expression position is a SyntaxError
// (`a = {[f1@]: "a"}` and `b = {a : 5@ , a(){}}`). The parser silently dropped a
// `@` that trailed a complete expression instead of rejecting it, so the malformed
// sources parsed without error. A `@` is only ever a DecoratorList prefix (before a
// class declaration/expression or a class element); when it follows a complete
// expression on the same line it is now a SyntaxError. A newline-separated
// `@dec class {}` remains valid (ASI is preserved).
public class Issue818StrayDecoratorTests
{
    // Compile `body` as a function body (the test262 `Function(script)` shape) and
    // report the thrown error's constructor name, or "NO THROW" when it parses.
    private static string ErrorNameOrResult(string body)
    {
        using var ctx = new JSContext();
        var js = "(function(){ try { Function(" + QuoteJs(body) +
                 "); return 'NO THROW'; } catch (e) { return e.constructor.name; } })()";
        return ctx.Eval(js).ToString();
    }

    private static string QuoteJs(string s)
    {
        var sb = new System.Text.StringBuilder("\"");
        foreach (var c in s)
        {
            sb.Append(c switch
            {
                '\\' => "\\\\",
                '"' => "\\\"",
                '\n' => "\\n",
                '\r' => "\\r",
                _ => c.ToString(),
            });
        }
        return sb.Append('"').ToString();
    }

    [Theory]
    [InlineData("5@")]
    [InlineData("(5@)")]
    [InlineData("[5@]")]
    [InlineData("b = {a : 5@ , a(){}}")]              // sm/class/methDefn.js
    [InlineData("a = {[f1@]: \"a\", [f2]: \"b\"}")]   // sm/class/compPropNames.js
    public void StrayAtSignIsSyntaxError(string body)
        => Assert.Equal("SyntaxError", ErrorNameOrResult(body));

    [Theory]
    [InlineData("@dec class C {}")]
    [InlineData("var x = @dec class {}")]
    [InlineData("@a @b class C {}")]
    [InlineData("class C { @dec m(){} }")]
    [InlineData("@(d) class C {}")]
    [InlineData("var x = 5\n@dec class C {}")]        // newline before `@` (ASI)
    public void ValidDecoratorStillParses(string body)
        => Assert.Equal("NO THROW", ErrorNameOrResult(body));
}
