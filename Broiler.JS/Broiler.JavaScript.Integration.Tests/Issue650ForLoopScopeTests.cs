using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/650 — Problem 6
// (test/language/statements/for-of/scope-body-var-none.js and friends).
//
// Two distinct bugs:
//  1. `var` declarations in a for-of/for-in body were not hoisted to the function
//     scope when the loop head was lexical (let/const) — they were stranded in the
//     ForStatement scope and never reached the function's hoisting scope.
//  2. A newline between a declaration-form loop head and the `of`/`in` keyword
//     (e.g. `for (let a\n of obj)`) hid the contextual keyword, so it was parsed
//     as an identifier expression ("of is not defined" / "Value is not iterable").
public class Issue650ForLoopScopeTests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // ---- var hoisting out of a lexically-headed for-of/for-in body ----

    [Fact]
    public void VarInForOfBodyHoistsToFunctionScope()
        => Assert.Equal("function", Eval(
            "for (let a of [9]) var f = function () { return 7; }; typeof f"));

    [Fact]
    public void MultipleVarDeclaratorsInForOfBodyAllHoist()
        => Assert.Equal("2 3", Eval(
            "for (let [a] of [[9]]) var x = 2, y = 3; '' + x + ' ' + y"));

    [Fact]
    public void VarInBracedForOfBodyHoists()
        => Assert.Equal("function", Eval(
            "for (let a of [9]) { var f = function () { return 7; }; } typeof f"));

    [Fact]
    public void VarInForInBodyHoists()
        => Assert.Equal("function", Eval(
            "for (let k in {p:1}) var f = function () { return 7; }; typeof f"));

    // ---- newline before the of/in contextual keyword (declaration head) ----

    [Theory]
    [InlineData("for (\n  let a\n  of\n  [1,2,3]\n) n++;")]
    [InlineData("for ( let a\n of [1,2,3] ) n++;")]
    [InlineData("for ( var a\n of\n [1,2,3] ) n++;")]
    [InlineData("for ( const a\n of\n [1,2,3] ) n++;")]
    [InlineData("for ( let [a]\n of\n [[1],[2],[3]] ) n++;")]
    public void NewlineBeforeOfKeywordParsesAsForOf(string loop)
        => Assert.Equal("3", Eval($"var n=0; {loop} '' + n"));

    [Fact]
    public void NewlineBeforeInKeywordParsesAsForIn()
        => Assert.Equal("2", Eval("var n=0; for (\n  let k\n  in\n  {p:1,q:2}\n) n++; '' + n"));

    // ---- the full test262 scope-body-var-none.js body ----

    [Fact]
    public void ScopeBodyVarNone()
        => Assert.Equal("22222", Eval(@"
var probeBefore = function() { return x; };
var probeExpr, probeDecl, probeBody;
var x = 1;
for (
    let [_, __ = probeDecl = function() { return x; }]
    of
    [[probeExpr = function() { return x; }]]
  )
  var x = 2, ___ = probeBody = function() { return x; };
'' + probeBefore() + probeExpr() + probeDecl() + probeBody() + x"));
}
