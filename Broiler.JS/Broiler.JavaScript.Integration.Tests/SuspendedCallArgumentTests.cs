using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for a call inside a generator or async body whose OPERANDS are hoisted out
// to statement level by the generator rewrite — `obj.method(await p)`, `f(1, 2, 3, 4, yield x)`,
// `f(...[await p])`, and, just as much, the plain `obj.method(a.b())`. Ordinary code evaluates a
// call's operands onto the IL evaluation stack and invokes; a generator or async body cannot,
// because FlattenBlocks lifts any block-valued operand's statements out as siblings and passes a
// spilled temp in its place. Two things were relying on the stack:
//
//   * The receiver and the resolved method live in temps taken from a per-function pool, and
//     a nested call in the arguments is handed the same two temps back. That is safe while
//     both values are already on the stack by the time an argument runs; once the argument is
//     a statement, the nested call's assignment lands between them and the invocation, and
//     the outer call went to the nested callee instead. `obj.hit(await Promise.resolve(1))`
//     called `Promise.resolve` again and never called `obj.hit` — silently, because nothing
//     looks at the wrong callee's return value. docs/compliance/known-gaps.md recorded this
//     as the largest single contributor to the suite's unverifiable `async` results.
//
//     This was first fixed only for operands holding an `await` or a `yield`, which is how it
//     was found, but the suspension was never the cause — the hoist is. A nested member call
//     compiles to a block whether or not anything suspends, so `trace.push(items.join('+'))` in
//     an ordinary `async` function hit the identical bug: it re-ran `items.join` and dropped the
//     `trace.push`, so the statement vanished with no error and the next one ran normally. The
//     guard now asks what the operands COMPILED to rather than what the source said.
//   * An argument list of more than four arguments — or any length once one of them is a
//     spread — is built as an array initializer, which nothing hoisted. The suspension stayed
//     between one element's store and the next, which the CLR rejects outright: an
//     InvalidProgramException rather than a wrong answer.
public class SuspendedCallArgumentTests
{
    // Runs a program that assigns its answer to `globalThis.r`, pumping the event loop so an
    // async body actually completes.
    private static string Drive(string body)
    {
        using var ctx = new JSContext();
        ctx.Eval("globalThis.r = '<unset>';");
        ctx.Execute(body);
        return ctx.Eval("'' + globalThis.r").ToString();
    }

    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    private const string Recorder =
        "var out = []; var obj = { hit: function () { out.push('hit(' + "
        + "[].slice.call(arguments).join(',') + ')'); return 'H'; }, "
        + "nested: { deep: function (v) { out.push('deep:' + v); return 'D'; } } }; "
        + "function id(v) { return v; } ";

    [Theory]
    // The call the source names is the call that runs, whatever the argument suspends on.
    [InlineData("obj.hit(await Promise.resolve(1));", "hit(1)")]
    [InlineData("obj.hit(1, await Promise.resolve(2), 3);", "hit(1,2,3)")]
    [InlineData("obj.hit(id(await Promise.resolve(4)));", "hit(4)")]
    [InlineData("obj.nested.deep(await Promise.resolve(5));", "deep:5")]
    [InlineData("obj['hit'](await Promise.resolve(6));", "hit(6)")]
    [InlineData("obj[await Promise.resolve('hit')](7);", "hit(7)")]
    [InlineData("obj.hit(await Promise.resolve(8), await Promise.resolve(9));", "hit(8,9)")]
    [InlineData("out.push('r:' + obj.hit(await Promise.resolve(10)));", "hit(10),r:H")]
    [InlineData("obj.nested.deep(obj.hit(await Promise.resolve(11)));", "hit(11),deep:H")]
    // More than four arguments, and spreads, are the array-initializer path.
    [InlineData("obj.hit(1, 2, 3, 4, await Promise.resolve(12));", "hit(1,2,3,4,12)")]
    [InlineData("obj.hit(...[await Promise.resolve(13)]);", "hit(13)")]
    [InlineData("obj.hit(...(await Promise.resolve([14, 15])));", "hit(14,15)")]
    public void AwaitInAnArgumentStillCallsTheMethodTheSourceNamed(string statement, string expected)
        => Assert.Equal(expected, Drive(
            Recorder + "(async function () { " + statement
            + " globalThis.r = out.join(','); })();"));

    [Theory]
    [InlineData("obj.hit(yield 1);", "hit(V)")]
    [InlineData("out.push('r:' + obj.hit(yield 1));", "hit(V),r:H")]
    [InlineData("obj.hit(1, 2, 3, 4, yield 1);", "hit(1,2,3,4,V)")]
    [InlineData("obj.hit(...[yield 1]);", "hit(V)")]
    public void YieldInAnArgumentStillCallsTheMethodTheSourceNamed(string statement, string expected)
        => Assert.Equal(expected, Eval(
            Recorder + "function* g() { " + statement + " return out.join(','); } "
            + "var it = g(); it.next(); '' + it.next('V').value"));

    [Fact(Timeout = 600000)]
    public void AComputedKeyThatSuspendsStillResolvesAgainstItsOwnReceiver()
        // The key is read after the receiver, so it is inside the same window: a nested call
        // while computing it must not become what the call invokes.
        => Assert.Equal("hit(2)", Eval(
            Recorder + "function* g() { obj[String(yield 1)](2); return out.join(','); } "
            + "var it = g(); it.next(); '' + it.next('hit').value"));

    [Fact(Timeout = 600000)]
    public void PlainFunctionCallsAndConstructorsTakeTheSamePath()
    {
        Assert.Equal("1,2,3,4,5", Drive(
            "function f() { return [].slice.call(arguments).join(','); } "
            + "(async function () { globalThis.r = f(1, 2, 3, 4, await Promise.resolve(5)); })();"));
        Assert.Equal("1,2,3,4,5", Drive(
            "function C() { this.v = [].slice.call(arguments).join(','); } "
            + "(async function () { globalThis.r = new C(1, 2, 3, 4, await Promise.resolve(5)).v; })();"));
        Assert.Equal("1,2,3,4,5,6", Drive(
            "(async function () { globalThis.r = [1, 2, 3, 4, 5, await Promise.resolve(6)].join(','); })();"));
    }

    [Fact(Timeout = 600000)]
    public void ArgumentsStillEvaluateInSourceOrderAroundTheSuspension()
        // Hoisting the operands must not reorder them: the receiver is read first, then each
        // argument left to right, and the method runs last.
        => Assert.Equal("recv,a,b,call", Drive(
            "var out = []; "
            + "function recv() { out.push('recv'); return { m: function () { out.push('call'); } }; } "
            + "function arg(name, v) { out.push(name); return v; } "
            + "(async function () { recv().m(arg('a', await Promise.resolve(1)), arg('b', 2)); "
            + "globalThis.r = out.join(','); })();"));

    [Fact(Timeout = 600000)]
    public void ANestedCallInTheArgumentsStillRunsExactlyOnce()
        => Assert.Equal("inner,outer", Drive(
            "var out = []; "
            + "var o = { inner: function () { out.push('inner'); return 1; }, "
            + "outer: function () { out.push('outer'); } }; "
            + "(async function () { o.outer(o.inner(await Promise.resolve(0))); "
            + "globalThis.r = out.join(','); })();"));

    // ---------------------------------------------------------------- no suspension needed
    //
    // Everything below holds no `await` and no `yield`. It is the same defect: a nested member
    // call compiles to a block, the rewrite hoists it, and the pooled receiver/callee temps are
    // clobbered under the outer call. The guard that only looked for a suspension answered "no"
    // for all of it.

    /// <summary>
    /// The smallest reproduction, and the shape that makes this worth more than its size:
    /// <c>console.log(list.join(', '))</c> inside any <c>async</c> function is a member call
    /// whose argument is a member call. It recorded <c>1,3</c> — the statement did not run, no
    /// error was raised, and the statement after it ran normally.
    /// </summary>
    [Fact(Timeout = 600000)]
    public void AMemberCallArgumentToAMemberCallIsNotSkippedInAnAsyncFunction()
        => Assert.Equal("1,a,3", Drive(
            "var t = []; (async function () { t.push('1'); var g = ['a']; "
            + "t.push(g.join('+')); t.push('3'); globalThis.r = t.join(','); })();"));

    [Theory]
    // The outer call runs, and its value is its own — not the inner call's, which is what the
    // clobbered callee returned.
    [InlineData("var r = t.push(g.join('+')); t.push('r=' + r);", "a,r=1")]
    // Three deep, and two member-call arguments to one call.
    [InlineData("t.push(String(g.join('+')));", "a")]
    [InlineData("t.push(g.concat(['b']).join('-'));", "a-b")]
    [InlineData("t.push(g.join('') + g.slice(0).join(''));", "aa")]
    // A member call in a computed key, which is read after the receiver and so shares the window.
    [InlineData("var o = { p: function (v) { t.push(v); } }; o[['p'].join('')](g.join(''));", "a")]
    // A nested call as the receiver's own expression rather than as an argument.
    [InlineData("t.push(g.slice(0).join('+'));", "a")]
    public void ANestedMemberCallDoesNotClobberTheOuterCallInAnAsyncFunction(
        string statement, string expected)
        => Assert.Equal(expected, Drive(
            "var t = []; (async function () { var g = ['a']; " + statement
            + " globalThis.r = t.join(','); })();"));

    [Fact(Timeout = 600000)]
    public void TheSameHoldsInAGeneratorAndInAnAsyncArrow()
    {
        Assert.Equal("1,a,3", Eval(
            "var t = []; function* g() { t.push('1'); var a = ['a']; t.push(a.join('+')); "
            + "t.push('3'); return t.join(','); } var it = g(); '' + it.next().value"));
        Assert.Equal("1,a,3", Drive(
            "var t = []; (async () => { t.push('1'); var g = ['a']; t.push(g.join('+')); "
            + "t.push('3'); globalThis.r = t.join(','); })();"));
    }

    [Fact(Timeout = 600000)]
    public void ANestedPrivateMethodCallDoesNotClobberTheOuterPrivateKey()
        // The private-name key is copied into a pooled temp of its own, and a nested private
        // call in the arguments reaches it the same way.
        => Assert.Equal("b", Drive(
            "var t = []; class C { #b() { return 'b'; } #a(x) { t.push(x); } "
            + "async go() { this.#a(this.#b()); globalThis.r = t.join(','); } } new C().go();"));

    [Fact(Timeout = 600000)]
    public void AnOrdinaryFunctionIsUnaffected()
        // Nothing hoists outside a state machine, so the pooled temps stay correct there and
        // this path must keep costing nothing.
        => Assert.Equal("1,a,3", Eval(
            "var t = []; (function () { t.push('1'); var g = ['a']; t.push(g.join('+')); "
            + "t.push('3'); })(); t.join(',')"));

    [Fact(Timeout = 600000)]
    public void EvaluationOrderIsUnchangedWithoutASuspension()
        // The receiver first, then each argument left to right, then the call — the same order
        // ArgumentsStillEvaluateInSourceOrderAroundTheSuspension pins with an await in play.
        => Assert.Equal("recv,a,b,call", Drive(
            "var out = []; "
            + "function recv() { out.push('recv'); return { m: function () { out.push('call'); } }; } "
            + "var s = { arg: function (name, v) { out.push(name); return v; } }; "
            + "(async function () { recv().m(s.arg('a', 1), s.arg('b', 2)); "
            + "globalThis.r = out.join(','); })();"));
}
