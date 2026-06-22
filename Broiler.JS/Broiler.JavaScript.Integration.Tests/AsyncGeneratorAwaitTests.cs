using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for the async-generator await-vs-yield distinction
// (follow-up to https://github.com/MaiRat/Broiler.JS/issues/715, problem 2).
//
// `await` and a user `yield` both lower to the same `Yield` node, so the
// async-generator driver could not tell an internal await (resume internally
// with the settled value) from a consumer-facing yield (surface {value,done}).
// As a result the per-iteration await of `for await` — and any explicit `await`
// — leaked to the consumer as if it were a yield. A new IsAwait marker now flows
// from `await`/`for await` through BYieldExpression and GeneratorState, and the
// async-generator driver awaits internal awaits while surfacing only user yields.
//
// This unblocks the 4 test262 for-await-of/async-gen-decl-dstr-*-yield-expr files.
public class AsyncGeneratorAwaitTests
{
    // Drives an async generator body to completion (Execute pumps the event loop)
    // and returns the recorded global result string.
    private static string Drive(string body)
    {
        using var ctx = new JSContext();
        ctx.Eval("globalThis.r = '<unset>';");
        ctx.Execute(body);
        return ctx.Eval("'' + globalThis.r").ToString();
    }

    [Fact]
    public void ForAwaitInternalAwaitDoesNotLeakAsYield()
        // The per-iteration await of `{}` must not surface; the consumer sees 'BODY'.
        => Assert.Equal("BODY", Drive(
            "async function* g(){ for await (var z of [{}]) { yield 'BODY'; } }"
            + " var it = g(); it.next().then(a => { globalThis.r = a.value; });"));

    [Fact]
    public void ForAwaitWithDestructuringDefaultYield()
        => Assert.Equal("def", Drive(
            "async function* g(){ for await ({ x = yield 'def' } of [{}]) {} }"
            + " var it = g(); it.next().then(a => { globalThis.r = a.value; });"));

    [Fact]
    public void ExplicitAwaitOfPlainValueThenYield()
        => Assert.Equal("42", Drive(
            "async function* g(){ var q = await 41; yield q + 1; }"
            + " var it = g(); it.next().then(a => { globalThis.r = a.value; });"));

    [Fact]
    public void ExplicitAwaitOfPromiseThenYield()
        => Assert.Equal("10", Drive(
            "async function* g(){ var q = await Promise.resolve(5); yield q * 2; }"
            + " var it = g(); it.next().then(a => { globalThis.r = a.value; });"));

    [Fact]
    public void PlainYieldStillWorks()
        => Assert.Equal("def", Drive(
            "async function* g(){ yield 'def'; }"
            + " var it = g(); it.next().then(a => { globalThis.r = a.value; });"));

    [Fact]
    public void YieldAwaitsItsOperand()
        // AsyncGeneratorYield awaits the operand: `yield <promise>` surfaces 7, not the promise.
        => Assert.Equal("7", Drive(
            "async function* g(){ yield Promise.resolve(7); }"
            + " var it = g(); it.next().then(a => { globalThis.r = a.value; });"));

    [Fact]
    public void YieldStarOverAsyncIterable()
        => Assert.Equal("i1,i2", Drive(
            "async function* inner(){ yield 'i1'; yield 'i2'; }"
            + " async function* g(){ yield* inner(); }"
            + " var it = g(); it.next().then(a => { it.next().then(b => { globalThis.r = a.value + ',' + b.value; }); });"));

    [Fact]
    public void ForAwaitTwoIterationsThenDone()
        => Assert.Equal("A,B|done=true", Drive(
            "var out = []; async function* g(){ for await (var z of ['a','b']) { yield z.toUpperCase(); } }"
            + " var it = g();"
            + " it.next().then(a => { out.push(a.value);"
            + "   it.next().then(b => { out.push(b.value);"
            + "     it.next().then(c => { globalThis.r = out.join(',') + '|done=' + c.done; }); }); });"));

    [Fact]
    public void NoArgNextResumesWithUndefinedNotStaleAwaitValue()
        // The destructuring default `x = yield` resumes with undefined when next()
        // is called with no argument — not the prior internal await's value ({}).
        => Assert.Equal("PASS", Drive(
            "var x, iterCount = 0;"
            + " var fn = async function*() { for await ({ x = yield } of [{}]) { iterCount += 1; } };"
            + " var iter = fn();"
            + " iter.next().then(function(){ return iter.next(); }).then(function(){"
            + "   globalThis.r = (x === undefined && iterCount === 1) ? 'PASS' : ('FAIL x=' + x + ' c=' + iterCount); });"));

    [Fact]
    public void SentValueFlowsIntoDestructuringDefaultYield()
        => Assert.Equal("PASS", Drive(
            "var x, iterCount = 0;"
            + " var fn = async function*() { for await ({ x = yield } of [{}]) { iterCount += 1; } };"
            + " var iter = fn();"
            + " iter.next().then(function(){ return iter.next(77); }).then(function(){"
            + "   globalThis.r = (x === 77 && iterCount === 1) ? 'PASS' : ('FAIL x=' + x + ' c=' + iterCount); });"));
}
