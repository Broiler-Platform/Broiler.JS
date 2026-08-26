using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

/// <summary>
/// <c>for await…of</c> over an iterator whose <c>next()</c> hands back a promise that is not
/// already settled.
/// </summary>
/// <remarks>
/// <para>
/// <b>The defect was a deadlock, not a wrong answer.</b> Async iteration unwrapped the result of
/// <c>next()</c> inside <c>JSIterator</c> with <c>promise.Task.GetAwaiter().GetResult()</c> — a
/// blocking wait, on the one thread allowed to run this context's JavaScript. That works only while
/// the promise is <em>already</em> settled, which is why the shapes already covered elsewhere
/// (arrays, async generators, an iterator returning <c>Promise.resolve(record)</c>) all passed. The
/// moment <c>next()</c> returns the ordinary <c>something.then(…)</c>, the job that would settle it
/// can never run: the queue that runs it drains on the way out of the execution the thread is stuck
/// inside, which <c>JSMicrotaskQueue</c>'s own documentation names as the one pattern it cannot
/// support. The agent hung until the process was killed.
/// </para>
/// <para>
/// The step is now three pieces with the state machine's own <c>await</c> between them —
/// <c>IElementEnumerator.AsyncNextRaw</c>, the await, then <c>AsyncIterationStep</c>'s
/// <c>IsDone</c>/<c>Value</c>. Nothing blocks, so the settling job runs at the checkpoint it was
/// queued for.
/// </para>
/// <para>
/// Every test here would hang rather than fail before the change, which is why they are worth
/// carrying separately from the shapes that merely returned the wrong value.
/// </para>
/// </remarks>
public class ForAwaitUnsettledResultTests
{
    private static string Drive(string body)
    {
        using var ctx = new JSContext();
        ctx.Eval("globalThis.r = '<unset>'; globalThis.log = [];");
        ctx.Eval(body);
        return ctx.Eval("'' + globalThis.r").ToString();
    }

    /// <summary>The shape that hung: <c>next()</c> returns a chained promise.</summary>
    [Fact(Timeout = 600000)]
    public void ChainedNextResult() => Assert.Equal("1,2", Drive(
        "var it = { i: 0 };" +
        "it[Symbol.asyncIterator] = function () { return this; };" +
        "it.next = function () { var self = this; return Promise.resolve(0).then(function () {" +
        "    return self.i++ < 2 ? { value: self.i, done: false } : { value: undefined, done: true }; }); };" +
        "(async function () { var g = []; for await (const v of it) g.push(v); globalThis.r = g.join(','); })();"));

    /// <summary>A promise nothing has resolved when the loop reaches it.</summary>
    [Fact(Timeout = 600000)]
    public void DeferredNextResult() => Assert.Equal("ready", Drive(
        "var release;" +
        "var gate = new Promise(function (resolve) { release = resolve; });" +
        "var it = { done: false };" +
        "it[Symbol.asyncIterator] = function () { return this; };" +
        "it.next = function () { var self = this;" +
        "    if (self.done) return Promise.resolve({ value: undefined, done: true });" +
        "    self.done = true;" +
        "    return gate.then(function (v) { return { value: v, done: false }; }); };" +
        "(async function () { var g = []; for await (const v of it) g.push(v); globalThis.r = g.join(','); })();" +
        "release('ready');"));

    /// <summary>The already-settled shape still works — it is what used to be the only one that did.</summary>
    [Fact(Timeout = 600000)]
    public void SettledNextResult() => Assert.Equal("1,2", Drive(
        "var it = { i: 0 };" +
        "it[Symbol.asyncIterator] = function () { return this; };" +
        "it.next = function () { var self = this;" +
        "    return Promise.resolve(self.i++ < 2 ? { value: self.i, done: false } : { value: undefined, done: true }); };" +
        "(async function () { var g = []; for await (const v of it) g.push(v); globalThis.r = g.join(','); })();"));

    /// <summary>A rejected step result rejects the loop rather than hanging it.</summary>
    [Fact(Timeout = 600000)]
    public void RejectedNextResult() => Assert.Equal("caught:boom", Drive(
        "var it = {};" +
        "it[Symbol.asyncIterator] = function () { return this; };" +
        "it.next = function () { return Promise.resolve(0).then(function () { throw new Error('boom'); }); };" +
        "(async function () { try { for await (const v of it) { } globalThis.r = 'no throw'; }" +
        "  catch (e) { globalThis.r = 'caught:' + e.message; } })();"));

    /// <summary>A step result that settles to a non-object is the specified TypeError, checked after
    /// the await rather than before it.</summary>
    [Fact(Timeout = 600000)]
    public void NonObjectNextResult() => Assert.Equal("TypeError", Drive(
        "var it = {};" +
        "it[Symbol.asyncIterator] = function () { return this; };" +
        "it.next = function () { return Promise.resolve(0).then(function () { return 7; }); };" +
        "(async function () { try { for await (const v of it) { } globalThis.r = 'no throw'; }" +
        "  catch (e) { globalThis.r = e.name; } })();"));

    /// <summary>The synchronous fallback — <c>for await</c> over an array — goes through the same
    /// three pieces and still iterates.</summary>
    [Fact(Timeout = 600000)]
    public void SyncIterableFallback() => Assert.Equal("1,2", Drive(
        "(async function () { var g = []; for await (const v of [1, 2]) g.push(v); globalThis.r = g.join(','); })();"));

    /// <summary>An early <c>break</c> still closes the iterator through <c>return()</c>.</summary>
    [Fact(Timeout = 600000)]
    public void BreakClosesTheIterator() => Assert.Equal("1/closed", Drive(
        "var it = { i: 0, closed: false };" +
        "it[Symbol.asyncIterator] = function () { return this; };" +
        "it.next = function () { var self = this;" +
        "    return Promise.resolve(0).then(function () { return { value: ++self.i, done: false }; }); };" +
        "it['return'] = function () { this.closed = true; return Promise.resolve({ value: undefined, done: true }); };" +
        "(async function () { var g = [];" +
        "  for await (const v of it) { g.push(v); break; }" +
        "  globalThis.r = g.join(',') + '/' + (it.closed ? 'closed' : 'open'); })();"));
}
