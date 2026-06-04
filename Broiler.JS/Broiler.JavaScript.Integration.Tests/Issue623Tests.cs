using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/623
public class Issue623Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // Problem 1a: duplicate FunctionDeclarations at the top level of a (eval) body
    // are var-scoped, so they are allowed and the last declaration wins.
    [Fact]
    public void DuplicateTopLevelFunctionDeclarationsAllowed_LastWins()
    {
        var code = "var initial;"
            + "eval('initial = f(); function f(){return \"first\";} function f(){return \"second\";}');"
            + "initial + ',' + f();";
        Assert.Equal("second,second", Eval(code));
    }

    [Theory]
    [InlineData("'use strict'; function f(){} function f(){} typeof f;")]
    [InlineData("function f(){return 1;} if (true) function f() {} else function _f() {} typeof f;")]
    [InlineData("var f; if (true) function f() {} else function _f() {} typeof f;")]
    public void RedeclaredFunctionNamesDoNotThrow(string code)
        => Assert.Equal("function", Eval(code));

    // Problem 1b: an `if`-clause FunctionDeclaration is scoped to its own implicit
    // block (Annex B.3.4); a sibling lexical binding in the enclosing switch/block
    // blocks Annex B var-hoisting, so nothing leaks to the global scope.
    [Theory]
    [InlineData("eval('switch (0) { default: let f; if (true) function f() {  } else function _f() {} }'); typeof f;")]
    [InlineData("(0,eval)('switch (0) { default: let f; if (true) function f() {  } else function _f() {} }'); typeof f;")]
    [InlineData("eval('switch (0) { default: let f; if (true) function f() {} else ; }'); typeof f;")]
    [InlineData("eval('switch (0) { default: let f; if (true) function f() {} }'); typeof f;")]
    public void IfClauseFunctionBlockedByLexicalSiblingDoesNotLeak(string code)
        => Assert.Equal("undefined", Eval(code));

    // ...but without a blocking lexical binding the if-clause function still
    // Annex B var-hoists (here, out of the switch to the global scope).
    [Fact]
    public void IfClauseFunctionWithoutConflictStillHoists()
        => Assert.Equal("function,9", Eval("eval('switch (0) { default: if (true) function f() {return 9;} } typeof f + \",\" + f();')"));

    // The if-clause function declaration must still be an early error in strict mode.
    [Fact]
    public void StrictModeIfClauseFunctionIsSyntaxError()
    {
        var ex = Assert.Throws<JSException>(() => Eval("'use strict'; if (true) function f() {} typeof f;"));
        Assert.Contains("functions can only be declared at top level or inside a block", ex.Message);
    }

    // A direct eval inside a function: an if-clause function declaration must
    // update the surrounding var binding (Annex B copy-out), with the last
    // declaration winning over an earlier block-scoped one.
    [Fact]
    public void DirectEvalInFunctionIfClauseFunctionUpdatesVarBinding()
    {
        var code = "(function () { var updated;"
            + "eval('{ function h(){return 1;} } if (true) function h(){return 2;} else function _h(){} updated = h;');"
            + "return updated(); }())";
        Assert.Equal("2", Eval(code));
    }

    // Problem 2: a computed member key that is a `null` (or other non-string/number)
    // literal must be evaluated and coerced to a property key, not throw.
    [Theory]
    [InlineData("var x=[]; x[null]=1; x[null];", "1")]
    [InlineData("var o={[null]:7}; o[null];", "7")]
    [InlineData("var o={a:1}; '' + o?.[null];", "undefined")]
    public void ComputedMemberKeyFromNullLiteral(string code, string expected)
        => Assert.Equal(expected, Eval(code));

    // Problem 3: Intl.RelativeTimeFormat.prototype.resolvedOptions() must expose a
    // valid `locale` so that supportedLocalesOf(default-locale) round-trips.
    [Fact]
    public void RelativeTimeFormatResolvedOptionsHasLocale()
        => Assert.Equal("string", Eval("typeof new Intl.RelativeTimeFormat().resolvedOptions().locale;"));

    [Fact]
    public void RelativeTimeFormatSupportedLocalesOfDefaultLocale()
    {
        var code = "var d = new Intl.RelativeTimeFormat().resolvedOptions().locale;"
            + "Intl.RelativeTimeFormat.supportedLocalesOf([d, d]).length;";
        Assert.Equal("1", Eval(code));
    }
}
