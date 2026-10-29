using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/828 Problem 5.
//
// A closure created inside a `with` block captures the with-object chain, but it also needs the
// `with`-fallback lexical environment: a name the with-object lacks — or that @@unscopables
// blocks — must fall through to the enclosing lexical binding (e.g. a parameter). That fallback
// used to be active only while the `with` body ran, so a returned closure resolving such a name
// after the `with` exited threw "x is not defined" (test262
// staging/sm/lexical-environment/unscopables-closures). The captured fallback overlay is now
// re-established on invocation.
public class Issue828WithClosureUnscopablesTests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // @@unscopables blocks env.x, so the returned closure's `x` is the parameter (3), not env.x.
    [Fact]
    public void ClosureInWithFallsThroughUnscopableToParameter()
        => Assert.Equal("13", Eval("""
            let env = { x: 9000, [Symbol.unscopables]: { x: true } };
            function make_adder(x) { with (env) return function (y) { return x + y; }; }
            '' + make_adder(3)(10);
        """));

    // Without @@unscopables the closure resolves `x` to env.x (9000), as before.
    [Fact]
    public void ClosureInWithStillResolvesScopableWithProperty()
        => Assert.Equal("9010", Eval("""
            function make_adder(x) { with ({ x: 9000 }) return function (y) { return x + y; }; }
            '' + make_adder(3)(10);
        """));

    // The full sm test shape: nested eval-created closures still fall through @@unscopables to the
    // global `x` after the `with` has exited.
    [Fact]
    public void EvalClosureInWithFallsThroughUnscopableToGlobal()
        => Assert.Equal("510", Eval("""
            let env = { x: 9000, [Symbol.unscopables]: { x: true } };
            let x = 500;
            function make_adder_with_eval() { with (env) return eval('y => eval("x + y")'); }
            '' + make_adder_with_eval()(10);
        """));
}
