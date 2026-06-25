using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/914
//
// P15 (test/staging/sm/Function/arguments-parameter-shadowing.js): a function with a
// non-simple parameter list has a separate parameter environment and var environment
// (FunctionDeclarationInstantiation §10.2.11). A parameter-initializer closure such as
// `h = () => arguments` captures the parameter-environment `arguments` object; a body
// `var arguments` is a DISTINCT var-environment binding, initialised to a copy of that
// object, whose writes are not observed through the captured one.
public class Issue914ArgumentsParamVarEnvTests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // g8: `var arguments = 0` shadows; the arrow still sees the arguments object.
    [Fact]
    public void BodyVarArgumentsWithInitializerShadowsParamArguments()
    {
        Assert.Equal("0", Eval(
            "function g8(h=()=>arguments){ var arguments=0; return arguments; } g8()"));
        Assert.Equal("false", Eval(
            "function g8(h=()=>arguments){ var arguments=0; return (arguments===h()); } g8()"));
    }

    // g9: `var arguments` (no initializer) starts as the param arguments object, then a
    // later assignment writes only the body binding.
    [Fact]
    public void BodyVarArgumentsWithoutInitializerSeedsFromParamArguments()
    {
        Assert.Equal("false", Eval(
            "function g9(h=()=>arguments){ var arguments; return (void 0===arguments); } g9()"));
        Assert.Equal("true", Eval(
            "function g9(h=()=>arguments){ var arguments; return (h()===arguments); } g9()"));
        Assert.Equal("false", Eval(
            "function g9(h=()=>arguments){ var arguments; arguments=0; return (arguments===h()); } g9()"));
    }

    // Without a `var arguments` there is no shadow: the body `arguments` IS the captured
    // parameter-environment object.
    [Fact]
    public void BodyArgumentsWithoutVarSharesParamArguments()
        => Assert.Equal("true", Eval(
            "function g(h=()=>arguments){ return (arguments===h()); } g()"));

    // A simple parameter list has a single environment, so `var arguments` shares the
    // function's own arguments binding (no split) — test262 language/.../S13_A15_T2.
    [Fact]
    public void SimpleParameterListVarArgumentsSharesSingleBinding()
    {
        // var arguments = x overrides the arguments object for that single binding.
        Assert.Equal("5", Eval("function g(){ var arguments = 5; return arguments; } g(1,2,3)"));
        // The arguments object is still the materialised one before the override.
        Assert.Equal("3", Eval("function g(){ return arguments.length; } g(1,2,3)"));
    }

    // The arrow captures the actual mapped arguments object (not a copy lacking identity).
    [Fact]
    public void ParamArrowCapturesArgumentsObject()
        => Assert.Equal("[object Arguments]", Eval(
            "function g(h=()=>arguments){ var arguments=0; return Object.prototype.toString.call(h()); } g()"));
}
