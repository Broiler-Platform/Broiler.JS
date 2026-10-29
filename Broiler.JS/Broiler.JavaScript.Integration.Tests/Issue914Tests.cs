using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/914
//
// Problem 13 (test/staging/sm/strict/10.6.js). An ordinary function DECLARED inside a
// direct eval has its own `arguments` object; a reference to `arguments` from its body
// must materialise that object — not leak to the eval-calling frame's `arguments` via
// the dynamic eval-body resolution path. The eval-body path is only correct for
// `arguments` references that are lexically inside the eval program itself.
public class Issue914Tests
{
    private static JSValue Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code);
    }

    // A function defined inside a direct eval reads ITS OWN arguments, not the caller's.
    [Theory]
    // top-level direct eval (no enclosing function frame at all)
    [InlineData("eval('(function f(){ return arguments[0]; })(7)')", 7d)]
    [InlineData("eval('(function f(){ return arguments.length; })(1,2,3)')", 3d)]
    // direct eval nested inside a function whose own (empty) arguments must NOT be seen
    [InlineData("(function g(){ return eval('(function f(){ return arguments[0]; })(7)'); })()", 7d)]
    [InlineData("(function g(){ return eval('(function f(){ return arguments.length; })(1,2,3)'); })()", 3d)]
    public void FunctionInDirectEvalUsesOwnArguments(string code, double expected)
    {
        Assert.Equal(expected, Eval(code).DoubleValue);
    }

    // `arguments` referenced DIRECTLY in the eval body (not in a nested function) still
    // resolves to the ENCLOSING function's arguments — the eval has no arguments of its own.
    // (test262 sm/extensions/function-caller-skips-eval-frames relies on arguments.callee here.)
    [Theory]
    [InlineData("(function f(){ return eval('arguments[0]'); })(11)", 11d)]
    [InlineData("(function f(){ return eval('arguments.length'); })(1,2,3)", 3d)]
    [InlineData("(function f(){ return eval('(()=>arguments[0])()'); })(9)", 9d)]
    public void ArgumentsInEvalBodyResolvesToEnclosingFunction(string code, double expected)
    {
        Assert.Equal(expected, Eval(code).DoubleValue);
    }

    [Fact]
    public void ArgumentsCalleeInEvalBodyResolvesToEnclosingFunction()
    {
        Assert.Equal("function", Eval("(function f(){ return eval('typeof arguments.callee'); })(1)").ToString());
    }

    // The full test262 staging/sm/strict/10.6 scenario: indexed properties of a mapped
    // arguments object, made non-writable / non-configurable via Object.defineProperties,
    // honour those attributes for assignment/delete in both sloppy and strict mode — even
    // when the whole IIFE is evaluated through eval (as the non262 strict shell does).
    [Theory]
    [InlineData("arguments[0] = 42", "42", "42")]      // untouched index: writable+configurable
    [InlineData("delete arguments[0]", "true", "true")]
    [InlineData("arguments[1] = 42", "42", "TypeError")] // writable:false -> strict throws
    [InlineData("delete arguments[1]", "true", "true")]  // still configurable -> deletable
    [InlineData("arguments[2] = 42", "42", "42")]        // configurable:false but writable
    [InlineData("delete arguments[2]", "false", "TypeError")]
    [InlineData("arguments[3] = 42", "42", "TypeError")]
    [InlineData("delete arguments[3]", "false", "TypeError")]
    public void MappedArgumentsAttributesHonouredUnderEval(string expr, string sloppy, string strict)
    {
        Assert.Equal(sloppy, RunCallFunctionBody(expr, strictMode: false));
        Assert.Equal(strict, RunCallFunctionBody(expr, strictMode: true));
    }

    // Mirrors the non262 strict shell: build the IIFE source, eval it, and report the
    // value (or "TypeError" if a TypeError was thrown).
    private static string RunCallFunctionBody(string expr, bool strictMode)
    {
        var prefix = strictMode ? "'use strict'; " : "";
        var body =
            "(function f() {"
            + "Object.defineProperties(arguments, {1: { writable: false },"
            + "                                     2: { configurable: false },"
            + "                                     3: { writable: false, configurable: false }});"
            + "return (" + expr + ");"
            + "})(0, 1, 2, 3);";
        var code =
            "(function run(){ try { return '' + eval(" + JsString(prefix) + " + " + JsString(body) + "); }"
            + " catch (e) { return (e instanceof TypeError) ? 'TypeError' : ('OTHER:' + e); } })()";
        return Eval(code).ToString();
    }

    private static string JsString(string s) => "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
}
