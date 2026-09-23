using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Job ordering of `await` and of `await using` / AsyncDisposableStack disposal, counted against a
// chain of promise reactions (t1, t2, ...) queued BEFORE the code under test starts, so each job the
// code takes shows up as a reaction it falls behind. Expected strings follow the specification and
// are Node 24's output for the same scripts.
//
//  - Await (§27.7.5.3) is PromiseResolve(%Promise%, v) plus PerformPromiseThen with the resumption
//    as the reaction: one job for a native promise or a plain value, the resumption running in the
//    reaction job itself. JSAsyncFunction used to call `then` (observably, the current
//    Promise.prototype.then) and then queue the resumption as a second job; and it called a
//    thenable's `then` synchronously, where PromiseResolve defers it to a job.
//  - An async function's promise is one promise, resolved with the body's completion. It used to
//    be a promise per await step, each resolved with the next one, which adopted it two jobs late.
//  - DisposeResources records `await using x = null` (needsAwait) and performs Await(undefined)
//    for it — before a following sync-dispose resource, or at the end.
//  - An `await using` block seeds its thrown error into the disposal as the sync `using` block
//    does, so a disposer error becomes SuppressedError(disposerError, bodyError).
//  - In an async generator the disposal's await is an await, not a yield the consumer sees.
//  - AsyncDisposableStack.prototype.use's @@dispose fallback discards the method's result.
//  - `yield*` in an async generator over an async iterator awaits each next/throw/return result
//    of the delegate and yields its value without awaiting it again (§15.5.5); a native async
//    generator delegate used to be stepped synchronously, so its awaits surfaced as values and its
//    return value was lost, and a pending result of any other async iterator was read as a record.
//  - `return expr` in an async generator awaits expr, and return(v) awaits v (at a yield, or on a
//    generator that has not started or has completed).
//  - for-await awaits each value only for a sync iterable; a value of an async iterator is not
//    awaited again.
public class AsyncJobOrderTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Run(string script, int ticks = 4)
    {
        Load();
        using var ctx = new JSContext();
        ctx.Eval("var log = [];");
        ctx.Eval($$"""
            (function () {
                var p = Promise.resolve();
                for (let i = 1; i <= {{ticks}}; i++)
                    p = p.then(function () { log.push('t' + i); });
            })();
            {{script}}
            """);
        return ctx.Eval("log.join(',')").ToString();
    }

    [Fact(Timeout = 600000)]
    public void AwaitOfANativePromiseResumesInTheReactionJob()
        => Assert.Equal("f0,t1,f1,t2,t3", Run("""
            async function f() { log.push('f0'); await Promise.resolve(1); log.push('f1'); }
            f();
            """, 3));

    [Fact(Timeout = 600000)]
    public void AwaitOfAPlainValueTakesOneJob()
        => Assert.Equal("t1,a,t2,b,t3,done,t4,t5", Run("""
            async function f() { await null; log.push('a'); await 1; log.push('b'); }
            f().then(function () { log.push('done'); });
            """, 5));

    [Fact(Timeout = 600000)]
    public void AwaitOfARejectedPromiseThrowsInTheReactionJob()
        => Assert.Equal("t1,caught,t2,t3", Run("""
            async function f() { try { await Promise.reject(1); } catch (e) { log.push('caught'); } }
            f();
            """, 3));

    [Fact(Timeout = 600000)]
    public void AwaitOfAnAsyncCallResumesOneJobAfterItSettles()
        => Assert.Equal("f,t1,g1,t2,g2,t3,gdone,t4,t5", Run("""
            async function f() { log.push('f'); return 1; }
            async function g() { await f(); log.push('g1'); await 1; log.push('g2'); return 2; }
            g().then(function () { log.push('gdone'); });
            """, 5));

    [Fact(Timeout = 600000)]
    public void AwaitDoesNotCallPromisePrototypeThen()
        => Assert.Equal("t1,f1,t2", Run("""
            var then = Promise.prototype.then;
            async function f() {
                var p = Promise.resolve(1);
                Promise.prototype.then = function (a, b) { log.push('patched'); return then.call(this, a, b); };
                await p;
                log.push('f1');
            }
            f();
            Promise.prototype.then = then;
            """, 2));

    [Fact(Timeout = 600000)]
    public void AwaitReadsTheConstructorOfANativePromiseOnce()
        => Assert.Equal("ctor,t1,f1,t2", Run("""
            async function f() {
                var p = Promise.resolve(1);
                Object.defineProperty(p, 'constructor', { get: function () { log.push('ctor'); return Promise; } });
                await p;
                log.push('f1');
            }
            f();
            """, 2));

    [Fact(Timeout = 600000)]
    public void AwaitOfAThenableCallsThenInAJob()
        => Assert.Equal("get then,sync,t1,then,t2,v5,t3", Run("""
            var thenable = { get then() { log.push('get then'); return function (r) { log.push('then'); r(5); }; } };
            async function f() { var v = await thenable; log.push('v' + v); }
            f();
            log.push('sync');
            """, 3));

    [Fact(Timeout = 600000)]
    public void AsyncFunctionReturningAPromiseAdoptsIt()
        => Assert.Equal("t1,t2,t3,done,t4", Run("""
            async function f() { return Promise.resolve(1); }
            f().then(function () { log.push('done'); });
            """));

    [Fact(Timeout = 600000)]
    public void AwaitUsingNullAwaitsBeforeASyncDisposer()
        => Assert.Equal("body,t1,a,after,t2,t3", Run("""
            (async function () {
                {
                    using a = { [Symbol.dispose]() { log.push('a'); } };
                    await using b = null;
                    log.push('body');
                }
                log.push('after');
            })();
            """, 3));

    [Fact(Timeout = 600000)]
    public void AwaitUsingNullAwaitsWhenTheBlockThrows()
        => Assert.Equal("body,t1,caught,after,t2,t3", Run("""
            (async function () {
                try {
                    { await using x = null; log.push('body'); throw 1; }
                } catch (e) {
                    log.push('caught');
                }
                log.push('after');
            })();
            """, 3));

    [Fact(Timeout = 600000)]
    public void AwaitUsingSeedsTheBodyErrorIntoTheDisposal()
        => Assert.Equal("SuppressedError dispose body", Run("""
            (async function () {
                try {
                    await using x = { [Symbol.asyncDispose]() { throw new Error('dispose'); } };
                    throw new Error('body');
                } catch (e) {
                    log.push(e.constructor.name + ' ' + e.error.message + ' ' + e.suppressed.message);
                }
            })();
            """, 0));

    [Fact(Timeout = 600000)]
    public void AwaitUsingRethrowsTheBodyErrorAfterACleanDisposal()
        => Assert.Equal("disp,body", Run("""
            (async function () {
                try {
                    await using x = { async [Symbol.asyncDispose]() { log.push('disp'); } };
                    throw new Error('body');
                } catch (e) {
                    log.push(e.message);
                }
            })();
            """, 0));

    [Fact(Timeout = 600000)]
    public void AwaitUsingInAnAsyncGeneratorYieldsNothingExtra()
        => Assert.Equal("v1,d,end", Run("""
            async function* g() {
                await using x = { async [Symbol.asyncDispose]() { log.push('d'); } };
                yield 1;
            }
            (async function () {
                for await (var v of g()) log.push('v' + v);
                log.push('end');
            })();
            """, 0));

    [Fact(Timeout = 600000)]
    public void AsyncGeneratorAwaitAndYieldTakeOneJobEach()
        => Assert.Equal("g0,t1,g1,t2,t3,n,t4,t5,t6", Run("""
            async function* g() { log.push('g0'); await 1; log.push('g1'); yield 2; }
            g().next().then(function () { log.push('n'); });
            """, 6));

    [Fact(Timeout = 600000)]
    public void AwaitInAnAsyncGeneratorIsNotAForAwaitValue()
        => Assert.Equal("v2,v3,fin,end", Run("""
            async function* g() {
                await 7;
                yield 2;
                try { yield 3; } finally { await null; log.push('fin'); }
            }
            (async function () {
                for await (var v of g()) log.push('v' + v);
                log.push('end');
            })();
            """, 0));

    [Fact(Timeout = 600000)]
    public void AsyncDisposableStackUseNullAwaits()
        => Assert.Equal("t1,t2,done,t3", Run("""
            var s = new AsyncDisposableStack();
            s.use(null);
            s.disposeAsync().then(function () { log.push('done'); });
            """, 3));

    [Fact(Timeout = 600000)]
    public void AsyncDisposableStackUseFallbackDiscardsTheResult()
        => Assert.Equal("dispose,done", Run("""
            var thenable = { then: function (r) { log.push('then called'); r(); } };
            var s = new AsyncDisposableStack();
            s.use({ [Symbol.dispose]() { log.push('dispose'); return thenable; } });
            s.disposeAsync().then(function () { log.push('done'); });
            """, 0));

    [Fact(Timeout = 600000)]
    public void AsyncDisposableStackUseFallbackRejectionIsNotObserved()
        => Assert.Equal("done", Run("""
            var s = new AsyncDisposableStack();
            var rejected = Promise.reject(1);
            rejected.catch(function () { });
            s.use({ [Symbol.dispose]() { return rejected; } });
            s.disposeAsync().then(function () { log.push('done'); }, function () { log.push('rejected'); });
            """, 0));

    [Fact(Timeout = 600000)]
    public void YieldStarOverAnAsyncGeneratorAwaitsItsAwaits()
        => Assert.Equal("t1,t2,t3,v1,t4,t5,t6,t7,v2,t8,r=r,end", Run("""
            async function* inner() { yield 1; await null; yield 2; return 'r'; }
            async function* outer() { var r = yield* inner(); log.push('r=' + r); }
            (async function () {
                for await (var v of outer()) log.push('v' + v);
                log.push('end');
            })();
            """, 8));

    [Fact(Timeout = 600000)]
    public void YieldStarOverAnAsyncGeneratorTakesTheSpecJobs()
        => Assert.Equal("o0,i0,t1,t2,i1,t3,n1 a,i2,t4,t5,o=R,t6,t7,n2 b,n3 true,t8", Run("""
            async function* inner() { log.push('i0'); yield 'a'; log.push('i1'); await 0; log.push('i2'); return 'R'; }
            async function* outer() { log.push('o0'); var r = yield* inner(); log.push('o=' + r); yield 'b'; }
            var it = outer();
            it.next().then(function (s) { log.push('n1 ' + s.value); });
            it.next().then(function (s) { log.push('n2 ' + s.value); });
            it.next().then(function (s) { log.push('n3 ' + s.done); });
            """, 8));

    [Fact(Timeout = 600000)]
    public void YieldStarDelegateAwaitUsingDisposalIsNotAValue()
        => Assert.Equal("t1,t2,t3,v1,d,t4,t5,t6,end,t7,t8", Run("""
            async function* g() {
                await using x = { async [Symbol.asyncDispose]() { log.push('d'); } };
                yield 1;
            }
            async function* outer() { yield* g(); }
            (async function () {
                for await (var v of outer()) log.push('v' + v);
                log.push('end');
            })();
            """, 8));

    [Fact(Timeout = 600000)]
    public void YieldStarAwaitsAPendingAsyncIteratorResult()
        => Assert.Equal("next undefined,t1,t2,t3,1 false,next s1,t4,t5,t6,2 false,next s2,t7,t8,r=3,undefined true", Run("""
            var source = {
                [Symbol.asyncIterator]() {
                    var i = 0;
                    return { next(v) { log.push('next ' + v); i++; return Promise.resolve().then(function () { return { value: i, done: i > 2 }; }); } };
                }
            };
            async function* outer() { var r = yield* source; log.push('r=' + r); }
            (async function () {
                var it = outer();
                for (var i = 0; i < 3; i++) { var s = await it.next('s' + i); log.push(s.value + ' ' + s.done); }
            })();
            """, 8));

    [Fact(Timeout = 600000)]
    public void YieldStarForwardsThrowAndReturnToAnAsyncGeneratorDelegate()
        => Assert.Equal("t1,t2,t3,caught E,t4,t5,t6,c,inner finally,t7,t8,r=x,after,inner finally,RV true", Run("""
            async function* inner() {
                try { yield 1; yield 2; } catch (e) { log.push('caught ' + e); yield 'c'; } finally { log.push('inner finally'); }
                return 'x';
            }
            async function* outer() { var r = yield* inner(); log.push('r=' + r); yield 'after'; }
            (async function () {
                var it = outer();
                await it.next();
                log.push((await it.throw('E')).value);
                log.push((await it.next()).value);
                var it2 = outer();
                await it2.next();
                var s = await it2.return('RV');
                log.push(s.value + ' ' + s.done);
            })();
            """, 8));

    [Fact(Timeout = 600000)]
    public void YieldStarReturnWithoutADelegateReturnAwaitsTheValue()
        => Assert.Equal("t1,t2,t3,t4,t5,t6,t7,3 true,t8", Run("""
            var source = { [Symbol.asyncIterator]() { return this; }, next() { return { value: 1, done: false }; } };
            async function* outer() { yield* source; }
            var it = outer();
            it.next().then(function () {
                return it.return(Promise.resolve(2).then(function () { return 3; }));
            }).then(function (s) { log.push(s.value + ' ' + s.done); });
            """, 8));

    [Fact(Timeout = 600000)]
    public void AsyncGeneratorReturnStatementAwaitsItsOperand()
        => Assert.Equal("t1,t2,5,t3,caught Z,t4,t5,kk,t6,t7,t8", Run("""
            async function* g() { return Promise.resolve(5); }
            async function* k() { try { return Promise.reject('Z'); } catch (e) { log.push('caught ' + e); return 'kk'; } }
            (async function () {
                log.push((await g().next()).value);
                log.push((await k().next()).value);
            })();
            """, 8));

    [Fact(Timeout = 600000)]
    public void AsyncGeneratorReturnMethodAwaitsTheValue()
        => Assert.Equal("t1,t2,t3,t4,6,t5,t6,t7,caught Y,t8,2,rejected X,7", Run("""
            async function* h() { try { yield 1; } catch (e) { log.push('caught ' + e); yield 2; } }
            async function* e() { }
            (async function () {
                var it = h();
                await it.next();
                log.push((await it.return(Promise.resolve(6))).value);
                it = h();
                await it.next();
                log.push((await it.return(Promise.reject('Y'))).value);
                it = e();
                try { await it.return(Promise.reject('X')); } catch (x) { log.push('rejected ' + x); }
                log.push((await e().return(Promise.resolve(7))).value);
            })();
            """, 8));

    [Fact(Timeout = 600000)]
    public void ForAwaitDoesNotAwaitAnAsyncIteratorValue()
        => Assert.Equal("t1,promise,t2,t3", Run("""
            var p = Promise.resolve(5);
            var source = { [Symbol.asyncIterator]() { var n = 0; return { next() { n++; return Promise.resolve({ value: p, done: n > 1 }); } }; } };
            (async function () {
                for await (var v of source) log.push(v === p ? 'promise' : 'unwrapped');
            })();
            """, 3));
}
