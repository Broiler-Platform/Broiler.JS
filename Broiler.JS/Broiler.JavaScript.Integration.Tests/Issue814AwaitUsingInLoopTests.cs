using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/814 — Problems 7–10
// (await-using initializer disposal in for / for-of statements) and the `await using`
// for-of head (P36).
//
// `await using` inside a loop body — and a `for (await using x of …)` head, which
// desugars to a per-iteration block with `await using x = …` — disposes asynchronously
// at the end of each iteration. The disposal lowers to a value-producing `try/finally`
// whose `finally` awaits (Yield). The async state-machine rewrite flattened that into a
// goto machine ending in `Pop` (void), dropping the value and leaving the rewritten block
// typed `void` while the enclosing IL still expected the original type — an unbalanced
// stack that faulted as "Common Language Runtime detected an invalid program" whenever
// any statement followed the `await using` in the same block. The rewrite now spills the
// try/catch value into a lifted temp (so it survives the awaited finally) and yields it,
// preserving the node's type.
public class Issue814AwaitUsingInLoopTests
{
    // Pumps the event loop so an async function runs to completion, then reads the result.
    private static string Drive(string body)
    {
        using var ctx = new JSContext();
        ctx.Eval("globalThis.r = '<unset>';");
        ctx.Execute(body);
        return ctx.Eval("'' + globalThis.r").ToString();
    }

    [Fact]
    public void AwaitUsingInCStyleForWithTrailingStatement()
        => Assert.Equal("b0,ad0,b1,ad1", Drive(
            "(async function () { var g = []; " +
            "for (var i = 0; i < 2; i++) { " +
            "  await using x = { [Symbol.asyncDispose]() { g.push('ad' + i); return Promise.resolve(); } }; " +
            "  g.push('b' + i); } " +
            "globalThis.r = g.join(','); })();"));

    [Fact]
    public void AwaitUsingInForOfBody()
        => Assert.Equal("b,ad,b,ad", Drive(
            "(async function () { var g = []; " +
            "for (const y of [1, 2]) { " +
            "  await using x = { [Symbol.asyncDispose]() { g.push('ad'); return Promise.resolve(); } }; " +
            "  g.push('b'); } " +
            "globalThis.r = g.join(','); })();"));

    [Fact]
    public void AwaitUsingInWhileBody()
        => Assert.Equal("b,ad,b,ad", Drive(
            "(async function () { var g = []; var n = 0; " +
            "while (n++ < 2) { " +
            "  await using x = { [Symbol.asyncDispose]() { g.push('ad'); return Promise.resolve(); } }; " +
            "  g.push('b'); } " +
            "globalThis.r = g.join(','); })();"));

    [Fact]
    public void AwaitUsingForOfHead()
        => Assert.Equal("b,ad,b,ad2", Drive(
            "(async function () { var g = []; " +
            "for (await using x of [" +
            "  { [Symbol.asyncDispose]() { g.push('ad'); return Promise.resolve(); } }, " +
            "  { [Symbol.asyncDispose]() { g.push('ad2'); return Promise.resolve(); } }]) { g.push('b'); } " +
            "globalThis.r = g.join(','); })();"));

    [Fact]
    public void MixedAwaitAndSyncUsingDisposeInReverseOrder()
        // Disposal is LIFO: the later (sync) `using y` disposes before the earlier
        // (async) `await using x`.
        => Assert.Equal("b,d,ad", Drive(
            "(async function () { var g = []; " +
            "for (var i = 0; i < 1; i++) { " +
            "  await using x = { [Symbol.asyncDispose]() { g.push('ad'); return Promise.resolve(); } }; " +
            "  using y = { [Symbol.dispose]() { g.push('d'); } }; " +
            "  g.push('b'); } " +
            "globalThis.r = g.join(','); })();"));

    [Fact]
    public void AwaitUsingInBlockStillWorks()
        => Assert.Equal("b,ad,after", Drive(
            "(async function () { var g = []; " +
            "{ await using x = { [Symbol.asyncDispose]() { g.push('ad'); return Promise.resolve(); } }; g.push('b'); } " +
            "g.push('after'); globalThis.r = g.join(','); })();"));

    [Fact]
    public void ValueProducingTryFinallyWithAwaitInLoop()
        // The general rewriter fix: a value-producing try/finally whose finally awaits,
        // inside a loop, must preserve its value.
        => Assert.Equal("30", Drive(
            "(async function () { var s = 0; " +
            "for (var i = 0; i < 3; i++) { " +
            "  s += await (async function () { try { return 10; } finally { await Promise.resolve(); } })(); } " +
            "globalThis.r = s; })();"));
}
