using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;
using Xunit;

namespace Broiler.JavaScript.Integration.Tests;

// #921 P7 — a direct eval inside a function defined in a `with` must resolve an
// identifier against the function's OWN locals before the closure-captured with-object,
// which is lexically OUTER to them. Previously the flat with-scope chain consulted the
// captured with-object first, so `directWith` saw the with-object's `a` (or the global)
// instead of the inner `var a = 1`.
// (test262 staging/sm/global/eval-02.js)
public class Issue921EvalWithOrderingTests
{
    private static JSValue Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code);
    }

    [Fact]
    public void DirectEvalSeesFunctionLocalNotCapturedWithObject()
    {
        // eval-02: `eval` reached via an argument, a var, and a `with` object — all direct
        // evals that must see the inner function's `var a = 1`.
        Assert.Equal("2,2,2,2", Eval(@"
            var a = 9;
            function directArg(eval, s) { var a = 1; return eval(s); }
            function directVar(f, s) { var eval = f; var a = 1; return eval(s); }
            function directWith(obj, s) { var f; with (obj) { f = function () { var a = 1; return eval(s); }; } return f(); }
            [directArg(eval, 'a+1'), directVar(eval, 'a+1'),
             directWith(this, 'a+1'), directWith({eval: eval, a: -1000}, 'a+1')].join(',')").ToString());
    }

    [Fact]
    public void CapturedWithStillWinsWhenFunctionHasNoSuchLocal()
    {
        // No local `a` in the closure: the eval reads the captured with-object's `a`.
        Assert.Equal("-1000", Eval(@"
            var a = 9;
            function w(obj, s) { var f; with (obj) { f = function () { return eval(s); }; } return f(); }
            '' + w({a: -1000, eval: eval}, 'a')").ToString());
    }

    [Fact]
    public void BodyWithStillBeatsFunctionLocal()
    {
        // A `with` pushed by a with STATEMENT in the running body is INNER to the
        // function's locals, so the with-object wins (unchanged behavior).
        Assert.Equal("-7", Eval(@"
            function g(obj, s) { var a = 1; with (obj) { return eval(s); } }
            '' + g({a: -7, eval: eval}, 'a')").ToString());
    }

    [Fact]
    public void EvalReadWriteOfFunctionLocalAreCoherentUnderCapturedWith()
    {
        // The eval reads AND writes the inner local; the captured with-object is untouched.
        Assert.Equal("5,-1000", Eval(@"
            function dw(obj) {
                var f, r;
                with (obj) { f = function () { var a = 1; eval('a = 5'); return a; }; }
                r = f();
                return [r, obj.a].join(',');
            }
            dw({a: -1000})").ToString());
    }

    [Fact]
    public void ClosureInWithWithoutEvalIsUnaffected()
    {
        // No eval: a closure created in a `with` still resolves the with-object's binding
        // (regression guard for the with-fallback overlay).
        Assert.Equal("9010", Eval(@"
            function make_adder(x) { with ({ x: 9000 }) return function (y) { return x + y; }; }
            '' + make_adder(3)(10)").ToString());
    }
}
