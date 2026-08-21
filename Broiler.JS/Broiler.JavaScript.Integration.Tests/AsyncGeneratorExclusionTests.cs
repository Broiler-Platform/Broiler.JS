using System.Threading.Tasks;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Integration.Tests;

// Resuming an async generator after `await` runs the rest of its body, which is user JavaScript, so
// the resumption is subject to the same exclusion as a promise reaction: one thread runs JavaScript
// in a context at a time.
//
// `JSGenerator.AwaitThenable` was the last site still making that decision for itself, after
// `JSPromise.Post` and `JSAsyncFunction.ToPromise` were moved onto `JSContext.PostJob`. It preferred
// whatever `SynchronizationContext` happened to be current and fell back to
// `ThreadPool.QueueUserWorkItem` when there was none — the two answers `JSMicrotaskQueue`'s remarks
// record as wrong, because "a context is present" was never the same claim as "that context is the
// JavaScript thread".
//
// **Which of these two is the regression test, and why it is not the one it looks like.** The
// obvious shape — drive the racy dispatch and watch `JSContext.ExecutionConcurrency` — was written
// first and **passes against the unfixed engine**, so it is not evidence of anything. The counter is
// incremented by `JSMicrotaskQueue.EnterExecution`, and the whole of the pre-fix defect is that the
// resumption ran *without entering the context at all*: a bare `ThreadPool.QueueUserWorkItem` never
// reaches `RunJob`, so the thread it lets in is exactly the thread the counter cannot see. That is
// worth recording rather than deleting — a counter that only observes entries can never observe the
// absence of one, and the same blind spot applies to any future site that dispatches around
// `PostJob`.
//
// `TheResumptionStillRunsAndItsSideEffectIsWhole` is the one that discriminates, and it does so on
// **ordering** rather than on corruption: the fix makes the resumption run before the host's next
// statement, which is where the specification puts a microtask, while the thread pool runs it at an
// arbitrary later moment on an arbitrary thread. Checked both ways — 5 of 5 failures against the
// pre-fix dispatch, 5 of 5 passes with it.
[Collection(nameof(AsyncGeneratorExclusionTests))]
public sealed class AsyncGeneratorExclusionTests
{
    // A hand-written thenable rather than a promise, and that is what reaches the site. A native
    // promise settles through `JSPromise.Post`, which was already routed correctly; only a foreign
    // thenable — an object with a callable `then` — takes `AwaitThenable`'s own dispatch. `then`
    // parks the resumption callback in a global so a host thread can fire it later, which is the
    // shape that gets a job dispatched with nothing executing.
    private const string Arm = """
        globalThis.sink = 0;
        globalThis.resumed = 0;
        globalThis.thenable = { then: function (onFulfilled) { globalThis.fire = onFulfilled; } };
        async function* generate() {
            await globalThis.thenable;
            for (var i = 0; i < 200000; i++) { globalThis.sink += i; }
            globalThis.resumed += 1;
            yield 1;
        }
        globalThis.pending = generate().next();
        """;

    private const string Busy =
        "(function () { var t = 0; for (var j = 0; j < 20000; j++) { t += j; } return t; })()";

    [Fact(Timeout = 600000)]
    public void AnAsyncGeneratorResumedFromAHostThreadDoesNotOverlapAnEvaluation()
    {
        // The generator suspends at `await` with nothing else running, so firing the thenable's
        // callback from a host thread dispatches the resumption while the main thread is inside
        // `Eval`. With `JSContext.PostJob` that is either this context's queue or a pool dispatch
        // that takes the execution lock, and neither overlaps.
        //
        // **Kept as a guard, not offered as proof**: this passes against the pre-fix dispatch too,
        // for the reason in the class comment — an unentered thread is invisible to a counter of
        // entries. What it does catch is a future change that routes the resumption through
        // `EnterExecution` on a second thread, which is a different and equally real defect.
        JSContext.ResetExecutionConcurrency();

        for (var round = 0; round < 30; round++)
        {
            using var context = new JSContext();
            context.Eval(Arm);

            // No SynchronizationContext is installed on this thread and none was supplied to the
            // context, so the pre-fix code took its `ThreadPool.QueueUserWorkItem` branch here.
            // That is deliberate: it is the branch the fix removes.
            var fire = context["fire"];
            var settled = Task.Run(() => fire.InvokeFunction(new Arguments(JSUndefined.Value, JSUndefined.Value)));

            for (var i = 0; i < 20; i++)
                context.Eval(Busy);

            settled.Wait();
            context.Eval("1");
        }

        var (peak, overlaps) = JSContext.ExecutionConcurrency;
        Assert.Equal(0, overlaps);
        Assert.Equal(1, peak);
    }

    [Fact(Timeout = 600000)]
    public void TheResumptionStillRunsAndItsSideEffectIsWhole()
    {
        // **The discriminating case.** Two things are asserted at once and both matter: the
        // resumption still runs (a dispatch rule that "fixed" the race by dropping the job would
        // satisfy the counter above and break every async generator), and it runs *by the time the
        // host's next statement does*. The second is what fails against the pre-fix dispatch, which
        // hands the resumption to the pool and returns — `resumed` is still 0 when the assertion
        // reads it. `sink` accumulates in a loop, so a torn read-modify-write from a second thread
        // shows as a wrong total rather than as nothing at all.
        using var context = new JSContext();
        context.Eval(Arm);

        var fire = context["fire"];
        using (context.EnterExecution())
            fire.InvokeFunction(new Arguments(JSUndefined.Value, JSUndefined.Value));

        context.Eval("1");

        Assert.Equal("1", context.Eval("String(globalThis.resumed)").ToString());

        // 0 + 1 + ... + 199999.
        Assert.Equal("19999900000", context.Eval("String(globalThis.sink)").ToString());
    }
}
