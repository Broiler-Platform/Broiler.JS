using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests: an async function that `return`s a thenable must resolve its
// result promise by ADOPTING that thenable's state (§27.7.5.2 — normal completion
// resolves the result promise with the return value, and Promise Resolve Functions
// follow a thenable). Previously the completion path fulfilled the result promise
// with the thenable as a plain value (no adoption), so `await asyncFn()` where
// asyncFn returned a promise resumed immediately with the inner promise instead of
// awaiting it to settle. Found via the WPT css-anchor-position scroll cluster,
// where `waitUntilNextAnimationFrame` is `async () => new Promise(r => rAF(r))`.
public class AsyncReturnPromiseAdoptionTests
{
    // Drives the body to completion (Execute pumps the event loop) and returns the
    // recorded global result string.
    private static string Drive(string body)
    {
        using var ctx = new JSContext();
        ctx.Eval("globalThis.r = '<unset>';");
        ctx.Execute(body);
        return ctx.Eval("'' + globalThis.r").ToString();
    }

    [Fact]
    public void AwaitAsyncFnReturningResolvedPromiseUnwrapsInnerValue()
        // Before the fix `await w()` resumed with the inner promise object, so r was
        // "[object Promise]"; adoption unwraps it to the settled value 42.
        => Assert.Equal("42", Drive(
            "async function w(){ return Promise.resolve(42); }"
            + " async function run(){ globalThis.r = '' + (await w()); } run();"));

    [Fact]
    public void AwaitAsyncFnReturningGenericThenableUnwrapsInnerValue()
        => Assert.Equal("99", Drive(
            "async function w(){ return { then: function(res){ res(99); } }; }"
            + " async function run(){ globalThis.r = '' + (await w()); } run();"));

    [Fact]
    public void AwaitAsyncFnReturningPromiseWaitsForInnerToSettle()
        // Adoption means `await w()` must not resume until the inner promise settles:
        // the inner microtask ('inner;') runs before the code after the await
        // ('after5;'). Without adoption, w() fulfils with the inner promise as a value
        // and the await resumes early (before 'inner;'), yielding a different order.
        => Assert.Equal("before;inner;after5;", Drive(
            "globalThis.r = '';"
            + " async function w(){ return Promise.resolve().then(function(){ globalThis.r += 'inner;'; return 5; }); }"
            + " async function run(){ globalThis.r += 'before;'; var v = await w(); globalThis.r += 'after' + v + ';'; } run();"));

    [Fact]
    public void AwaitAsyncFnReturningRejectedPromiseThrows()
        // Adopting a rejected thenable makes `await w()` throw rather than resume with
        // the rejected promise as a value.
        => Assert.Equal("caught:e", Drive(
            "async function w(){ return Promise.reject('e'); }"
            + " async function run(){ try { await w(); globalThis.r = 'no-throw'; }"
            + " catch(err){ globalThis.r = 'caught:' + err; } } run();"));

    [Fact]
    public void AwaitAsyncFnReturningPrimitiveStillWorks()
        // The fast (non-adopting) path for primitive completions is unchanged.
        => Assert.Equal("5", Drive(
            "async function w(){ return 5; }"
            + " async function run(){ globalThis.r = '' + (await w()); } run();"));

    [Fact]
    public void AwaitAsyncFnReturningPlainObjectStillYieldsObject()
        // A returned non-thenable object is not adopted; await resumes with the object.
        => Assert.Equal("7", Drive(
            "async function w(){ return { v: 7 }; }"
            + " async function run(){ globalThis.r = '' + (await w()).v; } run();"));
}
