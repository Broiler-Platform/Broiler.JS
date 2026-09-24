using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// `for await` leaving its loop early (break, return, throw, a labelled jump) closes the iterator
// with AsyncIteratorClose. That means calling its return() and awaiting the result before the
// loop's completion goes on; the result must be an object. It closed only iterators with a
// synchronous return path and never awaited, so an async generator was not closed at all (its
// finally never ran), and a custom iterator's pending or rejected return() result was dropped.
// Expected values match V8.
public class ForAwaitIteratorCloseTests
{
    // Runs the script (Execute pumps its jobs) and returns the global `r`.
    private static string Run(string code)
    {
        using var ctx = new JSContext();
        ctx.Execute("var r;\n" + code);
        return ctx.Eval("'' + globalThis.r").ToString();
    }

    [Theory(Timeout = 600000)]
    // An async generator is closed by break, return and throw, and the loop waits for its
    // finally, awaits included.
    [InlineData("var log = []; async function* g(){ try { yield 1; yield 2; } finally { log.push('fin'); } } (async () => { for await (var x of g()) { log.push(x); break; } log.push('after'); r = JSON.stringify(log); })();",
        "[1,\"fin\",\"after\"]")]
    [InlineData("var log = []; async function* g(){ try { yield 1; yield 2; } finally { log.push('fin'); } } async function f(){ for await (var x of g()) return 'ret' + x; } f().then(v => { log.push(v); r = JSON.stringify(log); });",
        "[\"fin\",\"ret1\"]")]
    [InlineData("var log = []; async function* g(){ try { yield 1; } finally { log.push('fin'); } } (async () => { try { for await (var x of g()) throw 'body'; } catch (e) { log.push('caught ' + e); } r = JSON.stringify(log); })();",
        "[\"fin\",\"caught body\"]")]
    [InlineData("var log = []; async function* g(){ try { yield 1; } finally { await null; log.push('fin1'); await null; log.push('fin2'); } } (async () => { for await (var x of g()) break; log.push('after'); r = JSON.stringify(log); })();",
        "[\"fin1\",\"fin2\",\"after\"]")]
    // An error from closing replaces a break, but not a throw from the body.
    [InlineData("var log = []; async function* g(){ try { yield 1; } finally { throw 'fin-err'; } } (async () => { try { for await (var x of g()) break; log.push('no'); } catch (e) { log.push('caught ' + e); } r = JSON.stringify(log); })();",
        "[\"caught fin-err\"]")]
    [InlineData("async function* g(){ try { yield 1; } finally { throw 'fin-err'; } } (async () => { try { for await (var x of g()) throw 'body'; } catch (e) { r = e; } })();",
        "body")]
    // Nested loops close innermost first.
    [InlineData("var log = []; async function* g(n){ try { yield n + 'a'; yield n + 'b'; } finally { log.push(n + '-fin'); } } (async () => { outer: for await (var x of g('o')) { for await (var y of g('i')) { log.push(x + y); break outer; } } log.push('after'); r = JSON.stringify(log); })();",
        "[\"oaia\",\"i-fin\",\"o-fin\",\"after\"]")]
    // A custom async iterator's return() result is awaited, and must be an object.
    [InlineData("var log = []; var it = { [Symbol.asyncIterator]() { return this; }, next() { return Promise.resolve({ value: 1, done: false }); }, return() { log.push('return'); return Promise.resolve().then(() => null).then(() => { log.push('settled'); return {}; }); } }; (async () => { for await (var x of it) break; log.push('after'); r = JSON.stringify(log); })();",
        "[\"return\",\"settled\",\"after\"]")]
    [InlineData("var it = { [Symbol.asyncIterator]() { return this; }, next() { return Promise.resolve({ value: 1, done: false }); }, return() { return Promise.resolve(42); } }; (async () => { try { for await (var x of it) break; r = 'no error'; } catch (e) { r = e.constructor.name; } })();",
        "TypeError")]
    [InlineData("var it = { [Symbol.asyncIterator]() { return this; }, next() { return Promise.resolve({ value: 1, done: false }); }, return() { return Promise.reject(new Error('rejected')); } }; (async () => { try { for await (var x of it) break; r = 'no'; } catch (e) { r = e.message; } })();",
        "rejected")]
    [InlineData("var it = { [Symbol.asyncIterator]() { return this; }, next() { return Promise.resolve({ value: 1, done: false }); } }; (async () => { for await (var x of it) break; r = 'ok'; })();",
        "ok")]
    public void LeavingTheLoopClosesTheIterator(string code, string expected)
        => Assert.Equal(expected, Run(code));
}
