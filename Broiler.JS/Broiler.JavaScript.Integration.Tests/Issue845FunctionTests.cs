using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/845 — three further
// clusters:
//   * A body FunctionDeclaration overrides a same-named formal parameter
//     (FunctionDeclarationInstantiation order).
//   * A single-name parameter's anonymous-function default adopts the parameter name
//     (NamedEvaluation), only when the default is actually used.
//   * A Script/eval StatementList ends only at EOF and a braced block only at "}", so a
//     stray "}" or an unterminated "{" is a SyntaxError.
public class Issue845FunctionTests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // ---- Body FunctionDeclaration shadowing a parameter (Problem 7) ----

    [Theory]
    [InlineData("(function f(x){ return typeof x; function x(){} })()", "function")]
    [InlineData("(function f(x){ return typeof x; function x(){} })(5)", "function")]
    [InlineData("(function f(x){ return x(); function x(){ return 7; } })()", "7")]
    // No conflicting parameter: hoisting still works.
    [InlineData("(function f(){ return typeof x; function x(){} })()", "function")]
    public void BodyFunctionDeclarationOverridesParameter(string code, string expected)
        => Assert.Equal(expected, Eval(code));

    [Theory]
    // Ordinary parameter binding is unaffected.
    [InlineData("(function f(a){ return a; })(42)", "42")]
    [InlineData("(function f(a = 9){ return a; })()", "9")]
    public void ParameterBindingUnaffected(string code, string expected)
        => Assert.Equal(expected, Eval(code));

    // ---- Anonymous function naming from a parameter default (Problem 82) ----

    [Theory]
    [InlineData("(function(p = function(){}){ return p; })().name", "p")]
    [InlineData("(function(p = () => {}){ return p; })().name", "p")]
    // A named default keeps its own name.
    [InlineData("(function(p = function named(){}){ return p; })().name", "named")]
    // A supplied (anonymous) argument is NOT renamed to the parameter name.
    [InlineData("(function(p = function(){}){ return p; })(function(){}).name", "")]
    // Destructuring defaults keep working.
    [InlineData("(function({ q = function(){} }){ return q; })({}).name", "q")]
    public void ParameterDefaultAdoptsName(string code, string expected)
        => Assert.Equal(expected, Eval(code));

    // ---- Block/program terminator syntax errors (surfaced by Problem 77) ----

    [Theory]
    [InlineData("}")]   // stray close brace at top level
    [InlineData("{")]   // unterminated block reaching EOF
    public void StrayOrUnterminatedBraceIsSyntaxError(string source)
        => Assert.Equal("SyntaxError", Eval(
            $"(function(){{ try {{ eval({System.Text.Json.JsonSerializer.Serialize(source)}); return 'no throw'; }} catch (e) {{ return e.constructor.name; }} }})()"));

    [Theory]
    [InlineData("{ }", "ok")]
    [InlineData("{ var x = 1; } 'ok'", "ok")]
    [InlineData("function f(){ return 1; } 'ok'", "ok")]
    public void WellFormedBlocksStillParse(string source, string expected)
        => Assert.Equal(expected, Eval(
            $"(function(){{ eval({System.Text.Json.JsonSerializer.Serialize(source)}); return 'ok'; }})()"));
}
