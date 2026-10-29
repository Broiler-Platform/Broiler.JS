using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/814 — the `using`
// disposal area (relates to the await-using/using for-statement problems).
//
// A scope whose disposal was emitted inside an async function always awaited
// (`Yield`ed) the DisposableStack.Dispose() result, even when every resource was a
// synchronous `using`. Two defects followed:
//  1. A synchronous `using` inside *any* loop body in an async function produced an
//     invalid program — the async state-machine rewrite cannot lower a `Yield` inside a
//     try/finally nested in a loop.
//  2. Synchronous `using` disposal introduced a spurious microtask tick (it must run
//     synchronously at scope exit per Explicit Resource Management).
// A scope is now awaited only when it declares an `await using` (async-disposed)
// resource; a sync-only `using` scope disposes synchronously.
public class Issue814SyncUsingInAsyncLoopTests
{
    // Pumps the event loop so an async function runs to completion, then reads the
    // recorded global result.
    private static string Drive(string body)
    {
        using var ctx = new JSContext();
        ctx.Eval("globalThis.r = '<unset>';");
        ctx.Execute(body);
        return ctx.Eval("'' + globalThis.r").ToString();
    }

    [Fact]
    public void SyncUsingInAsyncForOf()
        => Assert.Equal("b,d", Drive(
            "(async function () { var g = []; " +
            "for (using x of [{ [Symbol.dispose]() { g.push('d'); } }]) g.push('b'); " +
            "globalThis.r = g.join(','); })();"));

    [Fact]
    public void SyncUsingInAsyncCStyleFor()
        => Assert.Equal("b,d0,b,d1", Drive(
            "(async function () { var g = []; " +
            "for (var i = 0; i < 2; i++) { using x = { [Symbol.dispose]() { g.push('d' + i); } }; g.push('b'); } " +
            "globalThis.r = g.join(','); })();"));

    [Fact]
    public void SyncUsingInAsyncWhile()
        => Assert.Equal("b,d", Drive(
            "(async function () { var g = []; var n = 0; " +
            "while (n++ < 1) { using x = { [Symbol.dispose]() { g.push('d'); } }; g.push('b'); } " +
            "globalThis.r = g.join(','); })();"));

    [Fact]
    public void SyncUsingInAsyncBlockStillDisposes()
        => Assert.Equal("b,d", Drive(
            "(async function () { var g = []; " +
            "{ using x = { [Symbol.dispose]() { g.push('d'); } }; g.push('b'); } " +
            "globalThis.r = g.join(','); })();"));

    [Fact]
    public void SyncUsingDisposalIsSynchronousNotAwaited()
        // The sync disposal must not introduce a microtask tick: 'after-block' runs before
        // a separately-queued microtask.
        => Assert.Equal("dispose,after-block,microtask", Drive(
            "var order = [];" +
            "(async function () { { using x = { [Symbol.dispose]() { order.push('dispose'); } }; } order.push('after-block'); })();" +
            "Promise.resolve().then(() => { order.push('microtask'); globalThis.r = order.join(','); });"));

    [Fact]
    public void AwaitUsingInAsyncBlockStillAwaitsDisposal()
        => Assert.Equal("b,ad,after", Drive(
            "(async function () { var g = []; " +
            "{ await using x = { [Symbol.asyncDispose]() { g.push('ad'); return Promise.resolve(); } }; g.push('b'); } " +
            "g.push('after'); globalThis.r = g.join(','); })();"));

    [Fact]
    public void SyncUsingInSyncLoopUnaffected()
    {
        using var ctx = new JSContext();
        Assert.Equal("b,d", ctx.Eval(
            "(function () { var g = []; " +
            "for (using x of [{ [Symbol.dispose]() { g.push('d'); } }]) g.push('b'); " +
            "return g.join(','); })()").ToString());
    }
}
