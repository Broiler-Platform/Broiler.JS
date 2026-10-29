using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// A promise reaction is invoked through the function's raw compiled delegate rather than
// through InvokeFunction, so the caller owes it the proper-tail-call trampoline. Under
// ScriptHostMode a body that ENDS IN A CALL compiles to a JSTailCall sentinel which the
// callee returns instead of making the call; whoever holds that sentinel has to force it.
// JSPromise.Post did not, and simply resolved the derived promise WITH the sentinel — so
// the call never happened and nothing said so:
//
//     Promise.resolve().then(() => f())            // f is never called
//     Promise.resolve().then(function () { return f(); })   // likewise
//     Promise.resolve().then(function () { f(); }) // ran, because it is not a tail call
//
// The syntactic difference between the last two is invisible in a review and total at
// runtime, which is what makes this worth pinning: `p.then(v => assert(v))` asserted
// nothing, and a test262 async test built from that shape passed by never running.
//
// Every fixture here runs with ScriptHostMode on, because that is where tail calls are
// lowered to the sentinel — with it off the same source calls directly and passes either
// way, which is why the ordinary suite never saw this.
public sealed class PromiseReactionTailCallTests
{
    /// <summary>
    /// Evaluates <paramref name="body"/> with a fresh `log` array and returns what the queued
    /// jobs appended to it. The queue drains on the way out of the outermost execution, so the
    /// second Eval sees everything the first one's reactions did.
    /// </summary>
    private static string Run(string body)
    {
        using var context = new JSContext(options: new JSContextOptions { ScriptHostMode = true });
        context.Eval("globalThis.log = [];");
        context.Eval(body);
        return context.Eval("globalThis.log.join(',')").ToString();
    }

    private const string Mark = "function mark(s) { log.push(s); return s; }\n";

    [Fact]
    public void AConciseArrowReactionRuns()
        => Assert.Equal("fulfilled", Run(Mark + "Promise.resolve().then(() => mark('fulfilled'));"));

    [Fact]
    public void AReturnedCallInAReactionRuns()
        => Assert.Equal("fulfilled", Run(
            Mark + "Promise.resolve().then(function () { return mark('fulfilled'); });"));

    [Fact]
    public void ARejectionHandlerEndingInACallRuns()
        => Assert.Equal("rejected", Run(
            Mark + "Promise.reject(new Error('x')).then(null, () => mark('rejected'));"));

    [Fact]
    public void ACatchHandlerEndingInACallRuns()
        => Assert.Equal("caught", Run(
            Mark + "Promise.reject(new Error('x')).catch(() => mark('caught'));"));

    [Fact]
    public void AFinallyHandlerEndingInACallRuns()
        => Assert.Equal("finally", Run(
            Mark + "Promise.resolve(1).finally(() => mark('finally'));"));

    [Fact]
    public void EveryReactionShapeRunsAndKeepsQueueOrder()
        // Both shapes in one queue, so a regression shows up as a GAP in the order rather than
        // as an empty log: the tail-call ones are the odd positions.
        => Assert.Equal("a,b,c,d", Run(Mark + """
            Promise.resolve().then(() => mark('a'));
            Promise.resolve().then(function () { mark('b'); });
            Promise.resolve().then(function () { return mark('c'); });
            Promise.resolve().then(() => { mark('d'); });
            """));

    [Fact]
    public void TheDerivedPromiseSeesTheCallsValueRatherThanTheSentinel()
        // The sentinel is an ordinary object, so resolving with it did not throw — the next
        // reaction just received something that was not the handler's result.
        => Assert.Equal("2", Run(
            "function inc(v) { return v + 1; }\n"
            + "Promise.resolve(1).then(v => inc(v)).then(v => { log.push(v); });"));

    [Fact]
    public void AReactionReturningAPromiseIsStillAdopted()
        // Forcing the sentinel must produce the CALL's value for the resolution to adopt, not
        // the sentinel itself: `() => Promise.resolve(x)` is a tail call too.
        => Assert.Equal("adopted", Run(
            "function later(v) { return Promise.resolve(v); }\n"
            + "Promise.resolve().then(() => later('adopted')).then(v => { log.push(v); });"));

    [Fact]
    public void AThrowFromATailCalledReactionRejectsTheDerivedPromise()
        // The call now happens inside the handler's try, so its error rejects the derived
        // promise as any other handler error would.
        => Assert.Equal("boom", Run(
            "function fail() { throw new Error('boom'); }\n"
            + "Promise.resolve().then(() => fail()).catch(function (e) { log.push(e.message); });"));

    [Fact]
    public void AnAwaitedTailCallReactionResumesWithTheValue()
        // The awaited value is spilled to a local first, deliberately: `log.push(await …)` — a
        // METHOD call with an await among its arguments — is silently skipped by the async
        // lowering, independently of anything here, and would hide this assertion.
        => Assert.Equal("41,42", Run(
            "function answer() { return 42; }\n"
            + "(async function () { log.push(41); "
            + "var v = await Promise.resolve().then(() => answer()); log.push(v); })();"));
}
