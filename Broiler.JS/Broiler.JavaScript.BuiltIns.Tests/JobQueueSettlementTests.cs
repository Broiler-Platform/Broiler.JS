using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Work a script starts through promises finishes on the realm's job queue, so it has finished
// when the evaluation that queued it returns — the point at which a host that ends once its job
// queue drains (the test262 script host) exits.
//
// Two built-ins settled their promises off that queue, and test262 recorded the symptom as
// flakiness: the result depended on whether the host exited before a thread-pool continuation
// ran, so the same test passed in a loaded parallel run and never settled in isolation, or the
// other way round.
//
//  - Promise.allKeyed / Promise.allSettledKeyed deferred every element through
//    SynchronizationContext.Post (test262 Promise/allKeyed/reject-deferred never settled in
//    isolation), and allSettledKeyed read each input's state synchronously, reporting pending
//    and later-rejected inputs as fulfilled.
//  - `await using` disposal (and AsyncDisposableStack.prototype.disposeAsync) was a C# async
//    method awaiting JSPromise.Task: its continuations ran on the thread pool (test262
//    statements/await-using/* passed or never settled with machine load — 47 of 60 concurrent
//    runs of await-using-outer-inner-using-bindings.js printed nothing).
public class JobQueueSettlementTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    // Evaluates `script`, which records into `log`, then reads `log` in a SECOND evaluation: no
    // waiting in between, so only work done on the job queue is visible.
    private static string LogAfter(string script)
    {
        Load();
        using var ctx = new JSContext();
        ctx.Eval("var log = [];");
        ctx.Eval(script);
        return ctx.Eval("log.join('|')").ToString();
    }

    [Fact(Timeout = 600000)]
    public void AllKeyedSettlesFromDeferredInputsOnTheJobQueue()
        => Assert.Equal("rejected 1|fulfilled a=2,b=3", LogAfter("""
            function later(settle, value) {
                return new Promise(function (resolve, reject) {
                    Promise.resolve().then(function () { (settle ? resolve : reject)(value); });
                });
            }
            Promise.allKeyed({ key: later(false, 1) })
                .then(function () { log.push('fulfilled'); }, function (e) { log.push('rejected ' + e); });
            Promise.allKeyed({ a: later(true, 2), b: 3 })
                .then(function (r) { log.push('fulfilled ' + Object.keys(r).map(function (k) { return k + '=' + r[k]; })); });
            """));

    [Fact(Timeout = 600000)]
    public void AllSettledKeyedReportsEachInputsEventualOutcome()
        => Assert.Equal("null|a:fulfilled:1|b:rejected:2|c:fulfilled:3", LogAfter("""
            var pending = new Promise(function (resolve) { Promise.resolve().then(function () { resolve(1); }); });
            var laterRejected = new Promise(function (_, reject) { Promise.resolve().then(function () { reject(2); }); });
            Promise.allSettledKeyed({ a: pending, b: laterRejected, c: 3 }).then(function (r) {
                log.push(String(Object.getPrototypeOf(r)));
                Object.keys(r).forEach(function (k) {
                    log.push(k + ':' + r[k].status + ':' + ('value' in r[k] ? r[k].value : r[k].reason));
                });
            });
            """));

    [Fact(Timeout = 600000)]
    public void AwaitUsingDisposalFinishesOnTheJobQueue()
        => Assert.Equal("dispose inner|dispose outer|asyncDispose|after", LogAfter("""
            (async function () {
                {
                    await using a = { [Symbol.asyncDispose]() { log.push('asyncDispose'); return Promise.resolve(); } };
                    await using b = { [Symbol.dispose]() { log.push('dispose outer'); } };
                    for (await using c = { [Symbol.dispose]() { log.push('dispose inner'); } }; ; ) break;
                }
                log.push('after');
            })();
            """));

    [Fact(Timeout = 600000)]
    public void AwaitUsingDisposalErrorsRejectOnTheJobQueue()
        => Assert.Equal("SuppressedError:1:2", LogAfter("""
            (async function () {
                await using a = { [Symbol.asyncDispose]() { return Promise.reject(1); } };
                await using b = { [Symbol.asyncDispose]() { throw 2; } };
            })().then(null, function (e) { log.push(e.name + ':' + e.error + ':' + e.suppressed); });
            """));

    [Fact(Timeout = 600000)]
    public void AsyncDisposableStackDisposeAsyncSettlesOnTheJobQueue()
        => Assert.Equal("second|first|done|again", LogAfter("""
            var stack = new AsyncDisposableStack();
            stack.defer(function () { log.push('first'); });
            stack.defer(function () { log.push('second'); return Promise.resolve(); });
            stack.disposeAsync().then(function () {
                log.push('done');
                return stack.disposeAsync();
            }).then(function () { log.push('again'); });
            """));

    // Every input goes through Call(promiseResolve, C, value), and Promise.resolve used to fulfil
    // with an object directly, so a thenable input's own object — not what it resolved to — was
    // the keyed result (and Promise.all's element, and Promise.resolve's value).
    [Fact(Timeout = 600000)]
    public void KeyedCombinatorsAdoptThenableInputs()
        => Assert.Equal("allKeyed number 8 1 2|allKeyed rejected 9|allSettledKeyed fulfilled 8 rejected 9", LogAfter("""
            var fulfils = { then: function (resolve) { resolve(8); } };
            var rejects = { then: function (_, reject) { reject(9); } };
            var out = [];
            Promise.allKeyed({ t: fulfils, n: 1, p: Promise.resolve(2) })
                .then(function (r) { out.push('allKeyed ' + typeof r.t + ' ' + r.t + ' ' + r.n + ' ' + r.p); });
            Promise.allKeyed({ r: rejects })
                .then(function () { out.push('allKeyed fulfilled'); }, function (e) { out.push('allKeyed rejected ' + e); });
            Promise.allSettledKeyed({ t: fulfils, r: rejects }).then(function (r) {
                out.push('allSettledKeyed ' + r.t.status + ' ' + r.t.value + ' ' + r.r.status + ' ' + r.r.reason);
            });
            var tick = Promise.resolve();
            for (var i = 0; i < 10; i++) tick = tick.then(function () {});
            tick.then(function () { out.sort().forEach(function (x) { log.push(x); }); });
            """));

    [Fact(Timeout = 600000)]
    public void PromiseResolveAdoptsAThenable()
        => Assert.Equal("TypeError|number 8|all number 8", LogAfter("""
            var fulfils = { then: function (resolve) { resolve(8); } };
            Promise.resolve(fulfils).then(function (v) { log.push(typeof v + ' ' + v); });
            Promise.all([fulfils]).then(function (v) { log.push('all ' + typeof v[0] + ' ' + v[0]); });
            var poisoned = {};
            Object.defineProperty(poisoned, 'then', { get: function () { throw new TypeError('poisoned'); } });
            Promise.resolve(poisoned).then(null, function (e) { log.push(e.name); });
            """));
}
