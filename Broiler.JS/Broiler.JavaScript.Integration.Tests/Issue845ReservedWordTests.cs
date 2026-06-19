using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/845 (Problem 47).
// The always-reserved words (class, const, enum, export, extends, import, super) lex as
// identifier-typed keyword tokens, so several grammar positions accepted them. They are
// now rejected as:
//   * a function (declaration/expression) name,
//   * a label,
//   * an `enum`/`extends` IdentifierReference in expression position.
// (Contextual keywords — let, yield, await, static, … — remain valid identifiers in
// sloppy code, and genuinely valid uses of `super`/`extends` keep working.)
public class Issue845ReservedWordTests
{
    private static string ErrorOf(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval($"(function(){{ try {{ eval({System.Text.Json.JsonSerializer.Serialize(code)}); return 'no throw'; }} catch (e) {{ return e.constructor.name; }} }})()").ToString();
    }

    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // ---- reserved word as a function name ----

    [Theory]
    [InlineData("function class() {}")]
    [InlineData("function enum() {}")]
    [InlineData("var s = (function extends() {});")]
    [InlineData("var s = (function super() {});")]
    public void ReservedWordFunctionNameIsSyntaxError(string code)
        => Assert.Equal("SyntaxError", ErrorOf(code));

    // ---- reserved word as a label ----

    [Theory]
    [InlineData("enum: while (false);")]
    [InlineData("extends: while (false);")]
    [InlineData("super: while (false);")]
    public void ReservedWordLabelIsSyntaxError(string code)
        => Assert.Equal("SyntaxError", ErrorOf(code));

    // ---- enum / extends as an IdentifierReference ----

    [Theory]
    [InlineData("enum = 1;")]
    [InlineData("extends = 1;")]
    [InlineData("var x = enum;")]
    [InlineData("(enum);")]
    public void EnumExtendsIdentifierReferenceIsSyntaxError(string code)
        => Assert.Equal("SyntaxError", ErrorOf(code));

    // ---- valid uses are unaffected ----

    [Theory]
    [InlineData("function foo() {}")]
    [InlineData("function let() {}")]          // contextual keyword, valid in sloppy
    [InlineData("foo: while (false);")]
    [InlineData("await: while (false);")]      // contextual outside async
    [InlineData("var x = 1; x = 2;")]
    public void ValidBindingsStillParse(string code)
        => Assert.Equal("no throw", ErrorOf(code));

    [Fact]
    public void ValidSuperAndExtendsStillWork()
        => Assert.Equal("[object Object]",
            Eval("class C extends Object { m() { return super.toString; } } Object.prototype.toString.call(new C())"));
}
