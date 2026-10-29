using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Integration.Tests;

// A job queued by `await` or by a promise reaction must run on the JavaScript thread AFTER the
// current script finishes — never on a thread beside it.
//
// Before this, both `JSPromise.Post` and `JSAsyncFunction`'s `Queue` fell back to
// `ThreadPool.QueueUserWorkItem` whenever the host had installed no SynchronizationContext, which
// is the default for every plain `Eval`. A queued job resumes a generator and runs user JavaScript,
// so **two threads executed JavaScript in one context at the same time**. What surfaced was a wrong
// answer; what it was is an unsynchronized heap.
//
// **The fixtures below are written to lose the race deterministically**, which is the whole
// difference between them and the test that found this. `CapturedNumericLocalTests`'
// `SuspendingNestedFunctionsCaptureThroughTheSameBox` asserts the same value and caught the bug
// **0.6% of the time** — three runs in four only while a saturated container let the pool thread
// win, and never on an idle one. Asserting a value the racing thread would corrupt is not enough
// when the racing thread usually loses; each fixture here gives it time to win, so a regression is
// a failure rather than a probability.
public sealed class MicrotaskSchedulingTests
{
    /// <summary>Enough synchronous work after the call that a pool thread would reliably finish.</summary>
    private const string Spin = "var spin = 0; for (var i = 0; i < 3000000; i++) { spin += i; }";

    [Fact]
    public void AnAwaitResumptionCannotRunWhileTheScriptThatStartedItIsStillRunning()
    {
        // §27.7.5.3: `await` queues a job. The statements after `f()` belong to the job already
        // running, so `v` must still be 2 when they read it — the `v = v + 10` after the await
        // cannot have happened. The spin is what makes this deterministic rather than lucky.
        using var context = new JSContext();
        Assert.Equal("2,2", context.Eval($$"""
            (function () {
                var out = 'no';
                var v = 1;
                var f = async function () { v = v + 1; out = v; await 0; v = v + 10; };
                f();
                {{Spin}}
                return String(out) + ',' + v;
            })()
            """).ToString());
    }

    [Fact]
    public void APromiseReactionCannotRunWhileTheScriptThatQueuedItIsStillRunning()
    {
        // The same guarantee through the other dispatch site. `Promise.resolve().then(...)` queues
        // a reaction job; it may not be observed by the script that queued it.
        using var context = new JSContext();
        Assert.Equal("no", context.Eval($$"""
            (function () {
                var out = 'no';
                Promise.resolve(1).then(function () { out = 'ran'; });
                {{Spin}}
                return out;
            })()
            """).ToString());
    }

    [Fact]
    public void TheJobHasRunByTheTimeTheNextEvaluationLooks()
    {
        // The other half, and the one that says the queue is a *deferral* and not a drop: deferring
        // a job forever would pass every fixture above. By the time control returns to the host the
        // queue has been drained, so a second evaluation sees the completed effect.
        using var context = new JSContext();
        context.Eval("""
            var v = 1;
            var f = async function () { v = v + 1; await 0; v = v + 10; };
            f();
            """);

        Assert.Equal("12", context.Eval("String(v)").ToString());
    }

    [Fact]
    public void JobsRunInTheOrderTheyWereQueued()
    {
        // A queue, not a set. Three reactions queued in one script must run first-in-first-out,
        // which the thread-pool fallback could not promise even when it happened to defer them.
        using var context = new JSContext();
        context.Eval("""
            var log = '';
            Promise.resolve().then(function () { log += 'a'; });
            Promise.resolve().then(function () { log += 'b'; });
            Promise.resolve().then(function () { log += 'c'; });
            """);

        Assert.Equal("abc", context.Eval("log").ToString());
    }

    [Fact]
    public void AJobMayQueueAnotherAndTheDrainRunsToTheEnd()
    {
        // The drain loops until the queue is empty rather than taking one pass, because an `await`
        // chain resolves one job at a time and stopping early would strand the tail.
        using var context = new JSContext();
        context.Eval("""
            var v = 0;
            var f = async function () { v = v + 1; await 0; v = v + 1; await 0; v = v + 1; await 0; v = v + 1; };
            f();
            """);

        Assert.Equal("4", context.Eval("String(v)").ToString());
    }

    [Fact]
    public void AThrowingJobDoesNotSwallowTheRestOfTheQueue()
    {
        // A job is a complete unit of work and there is no caller left to hand its exception to —
        // the script that queued it has returned. So an unhandled one is reported and the loop
        // continues; the alternative is that one bad reaction silently strands every job behind it.
        using var context = new JSContext();
        context.Eval("""
            var log = '';
            Promise.resolve().then(function () { log += 'a'; throw new Error('boom'); });
            Promise.resolve().then(function () { log += 'b'; });
            """);

        Assert.Equal("ab", context.Eval("log").ToString());
    }

    [Fact]
    public void TheAsyncFunctionStillRunsSynchronouslyUpToItsFirstAwait()
    {
        // The guarantee this change must NOT break, and the idiom several existing tests rest on:
        // an async body runs synchronously to its first `await`, so effects before it ARE visible
        // to the caller. Only what follows the await is deferred.
        using var context = new JSContext();
        Assert.Equal("2,2", context.Eval("""
            (function () {
                var out = 'no';
                var v = 1;
                var f = async function () { v = v + 1; out = v; };
                f();
                return String(out) + ',' + v;
            })()
            """).ToString());
    }

    [Fact]
    public void ANestedEvaluationDoesNotDrainTheOuterOnesQueue()
    {
        // The reason the queue counts execution depth instead of draining whenever an evaluation
        // ends. A host callback that evaluates more source while JavaScript is on the stack is
        // still inside the outer job, so draining there would run a job in the middle of another
        // job — exactly the interleaving this change removes, reintroduced on one thread.
        using var context = new JSContext();
        var inner = context;
        context["reenter"] = JSValue.CreateFunction((in Arguments a) => inner.Eval(a.Get1().ToString()));

        Assert.Equal("no", context.Eval($$"""
            (function () {
                var out = 'no';
                Promise.resolve().then(function () { out = 'ran'; });
                reenter('1 + 1');
                {{Spin}}
                return out;
            })()
            """).ToString());
    }
}
