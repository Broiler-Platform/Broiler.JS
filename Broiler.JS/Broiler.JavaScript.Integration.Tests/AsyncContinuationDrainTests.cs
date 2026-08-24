using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Integration.Tests;

// The roadmap retest-queue item "async continuations did not run under in-process Eval or
// Execute" (an archived observation, never reproduced against a current pointer).
//
// It does not reproduce. A job queued by script during an in-process Eval — a promise reaction,
// a chained reaction, or an async function's resumption after `await` — has run by the time the
// call returns, so a later Eval on the same context observes its effect. These tests pin that,
// because it is the property a host relies on when it drives the engine synchronously.
//
// What is deliberately NOT asserted here is that a continuation runs SYNCHRONOUSLY during the
// script that queues it: it must not, and the ordering tests below state that too. The two
// together are the actual contract — deferred to the end of the job, but not lost.
public class AsyncContinuationDrainTests
{
    private static JSContext NewContext()
        => new JSContext(options: new JSContextOptions { ScriptHostMode = true });

    // ---- The continuation is deferred, not run inline ----

    [Fact(Timeout = 600000)]
    public void APromiseReactionDoesNotRunDuringTheScriptThatQueuesIt()
    {
        using var ctx = NewContext();
        Assert.Equal("sync", ctx.Eval(
            "(function () {"
            + "  var log = [];"
            + "  Promise.resolve().then(function () { log.push('reaction'); });"
            + "  log.push('sync');"
            + "  return log.join(',');"
            + "})()").ToString());
    }

    // ---- ...but it has run by the time the Eval returns ----

    [Fact(Timeout = 600000)]
    public void APromiseReactionQueuedInOneEvalHasRunByTheNextOne()
    {
        using var ctx = NewContext();
        ctx.Eval("globalThis.ran = false; Promise.resolve().then(function () { globalThis.ran = true; });");
        Assert.Equal("true", ctx.Eval("String(globalThis.ran)").ToString());
    }

    [Fact(Timeout = 600000)]
    public void AChainOfReactionsAllRunAndKeepTheirOrder()
    {
        using var ctx = NewContext();
        ctx.Eval(
            "globalThis.log = [];"
            + "Promise.resolve()"
            + "  .then(function () { globalThis.log.push('a'); })"
            + "  .then(function () { globalThis.log.push('b'); })"
            + "  .then(function () { globalThis.log.push('c'); });");
        Assert.Equal("a,b,c", ctx.Eval("globalThis.log.join(',')").ToString());
    }

    [Fact(Timeout = 600000)]
    public void AnAsyncFunctionResumesAfterItsAwait()
    {
        using var ctx = NewContext();
        ctx.Eval(
            "globalThis.out = 'never resumed';"
            + "(async function () { await null; globalThis.out = 'resumed'; })();");
        Assert.Equal("resumed", ctx.Eval("globalThis.out").ToString());
    }

    [Fact(Timeout = 600000)]
    public void AnAsyncFunctionResumesAcrossSeveralAwaits()
    {
        using var ctx = NewContext();
        ctx.Eval(
            "globalThis.steps = [];"
            + "(async function () {"
            + "  globalThis.steps.push('enter');"
            + "  await null; globalThis.steps.push('one');"
            + "  await null; globalThis.steps.push('two');"
            + "})();");
        Assert.Equal("enter,one,two", ctx.Eval("globalThis.steps.join(',')").ToString());
    }

    // A rejection handler is reached the same way.
    [Fact(Timeout = 600000)]
    public void ARejectionHandlerRunsBeforeTheEvalReturns()
    {
        using var ctx = NewContext();
        ctx.Eval(
            "globalThis.caught = 'none';"
            + "Promise.reject(new Error('boom')).catch(function (e) { globalThis.caught = e.message; });");
        Assert.Equal("boom", ctx.Eval("globalThis.caught").ToString());
    }

    // The same holds through the top-level-await entry point.
    [Fact(Timeout = 600000)]
    public async Task AReactionQueuedUnderTopLevelAwaitHasRunWhenItCompletes()
    {
        using var ctx = NewContext();
        var result = await ctx.EvalWithTopLevelAwaitAsync(
            "globalThis.ran = false; Promise.resolve().then(function () { globalThis.ran = true; }); 1");

        Assert.Equal("1", result.ToString());
        Assert.Equal("true", ctx.Eval("String(globalThis.ran)").ToString());
    }

    [Fact(Timeout = 600000)]
    public async Task TopLevelAwaitObservesTheAwaitedValue()
    {
        using var ctx = NewContext();
        var result = await ctx.EvalWithTopLevelAwaitAsync("var x = await Promise.resolve(7); x");
        Assert.Equal("7", result.ToString());
    }
}
