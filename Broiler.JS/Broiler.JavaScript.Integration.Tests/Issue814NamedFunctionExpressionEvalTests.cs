using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/814 — Problem 22
// (test/language/expressions/function/named-strict-error-reassign-fn-name-in-body-in-eval.js
//  and the generator counterpart under expressions/generators/).
//
// A named function expression's own name binding (`g` in `function g(){…}`) was visible
// to the body's static identifier resolution but NOT captured for a *direct eval* in the
// body, because it has no local Variable of its own (it is captured read-only). So
// `eval("g")` resolved against nothing — `typeof` reported "undefined", and a strict-mode
// `eval("g = 1")` threw a ReferenceError ("g is not defined") instead of the TypeError the
// spec requires for assigning to the immutable self-name binding. The binding now exposes
// its underlying JSVariable to the direct-eval/`with` capture path.
public class Issue814NamedFunctionExpressionEvalTests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    [Fact]
    public void DirectEvalSeesNamedFunctionExpressionName()
        => Assert.Equal("function", Eval(
            "var f = function g() { return eval('typeof g'); }; f()"));

    [Fact]
    public void StrictDirectEvalReassignOfNameThrowsTypeError()
        => Assert.Equal("TypeError", Eval(
            "var f = function g() { 'use strict'; try { eval('g = 1'); return 'no-error'; } " +
            "catch (e) { return e.constructor.name; } }; f()"));

    [Fact]
    public void StrictDirectEvalReassignOfGeneratorNameThrowsTypeError()
        => Assert.Equal("TypeError", Eval(
            "var f = function* g() { 'use strict'; try { eval('g = 1'); } " +
            "catch (e) { yield e.constructor.name; } }; f().next().value"));

    [Fact]
    public void DirectEvalCanRecurseThroughName()
        => Assert.Equal("24", Eval(
            "var fact = function f(n) { return n <= 1 ? 1 : n * eval('f(n - 1)'); }; '' + fact(4)"));

    [Fact]
    public void InnerVarShadowsNameForDirectEval()
        => Assert.Equal("number:7", Eval(
            "var f = function g() { var g = 7; return eval('typeof g + \":\" + g'); }; f()"));

    [Fact]
    public void NameStillResolvesStaticallyInBody()
        => Assert.Equal("function", Eval(
            "var f = function g() { return typeof g; }; f()"));

    [Fact]
    public void DirectStrictReassignOfNameStillThrowsTypeError()
        => Assert.Equal("TypeError", Eval(
            "var f = function g() { 'use strict'; try { g = 1; return 'no-error'; } " +
            "catch (e) { return e.constructor.name; } }; f()"));
}
