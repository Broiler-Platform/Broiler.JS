using System;
using System.Threading;
using System.Threading.Tasks;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Integration.Tests;

// A host function called from a script that has to wait on asynchronous work.
//
// `task.Wait()` deadlocks there, in two independent ways, and the two arrived with different
// changes — which is worth stating precisely because the first was attributed to the second:
//
//   * **the queue**: the host frame is inside an execution, so a promise reaction it is waiting for
//     is QUEUED and cannot run until that execution ends, which it never does;
//   * **the lock**: the execution lock the frame holds keeps out host work that would have to enter
//     the context to complete the task.
//
// Measured against each build: form one hangs with the queue alone AND with the lock; form two
// completes with the queue alone and hangs once the lock is added. `WaitFor` drains and then
// releases, which answers one shape each.
//
// **Every fixture runs on a worker with a bounded wait**, so a regression fails in seconds instead
// of hanging the suite. That is not caution — the deadlock these cover took twelve minutes and
// `--blame-hang` to identify the last time it was met.
public sealed class BlockingHostWaitTests
{
    private const int Budget = 15000;

    private static void WithinBudget(Action body)
    {
        Exception failure = null;
        var done = new ManualResetEventSlim();
        var worker = new Thread(() =>
        {
            try { body(); }
            catch (Exception ex) { failure = ex; }
            finally { done.Set(); }
        })
        { IsBackground = true };

        worker.Start();
        Assert.True(done.Wait(Budget), $"deadlocked: did not finish within {Budget} ms");
        if (failure != null)
            throw new Xunit.Sdk.XunitException($"worker threw: {failure}");
    }

    [Fact]
    public void WaitForCompletesWhenOnlyAQueuedJobCanCompleteTheTask()
    {
        // Form one. The reaction that signals the task is queued behind the very execution that is
        // waiting for it, so waiting without draining can only hang. `WaitFor` drains first.
        WithinBudget(() =>
        {
            using var context = new JSContext();
            var signal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            context["hostWait"] = JSValue.CreateFunction((in Arguments a) =>
            {
                context.WaitFor(signal.Task);
                return JSUndefined.Value;
            });
            context["hostSignal"] = JSValue.CreateFunction((in Arguments a) =>
            {
                signal.TrySetResult(true);
                return JSUndefined.Value;
            });

            context.Eval("globalThis.done = 'no'; Promise.resolve().then(function () { hostSignal(); }); hostWait(); globalThis.done = 'yes';");
            Assert.Equal("yes", context.Eval("globalThis.done").ToString());
        });
    }

    [Fact]
    public void WaitForCompletesWhenTheTaskNeedsAnotherThreadToEnterTheContext()
    {
        // Form two, and the one the execution lock introduced. Draining cannot help here — the work
        // that completes the task is on another thread and needs the context — so the wait has to
        // give the context up.
        WithinBudget(() =>
        {
            using var context = new JSContext();
            var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            context["hostWait"] = JSValue.CreateFunction((in Arguments a) =>
            {
                Task.Run(() =>
                {
                    using (context.EnterExecution())
                        context.Eval("globalThis.fromOtherThread = 1;");

                    entered.TrySetResult(true);
                });

                context.WaitFor(entered.Task);
                return JSUndefined.Value;
            });

            context.Eval("hostWait();");
            Assert.Equal("1", context.Eval("String(globalThis.fromOtherThread)").ToString());
        });
    }

    [Fact]
    public void WaitForRunsQueuedJobsBeforeItReleasesTheContext()
    {
        // "Nothing is left owed" — a job queued by the suspending execution runs on the thread that
        // queued it and in the order it was queued, rather than being handed to whichever thread
        // takes the context next. Asserted through ordering rather than through timing.
        WithinBudget(() =>
        {
            using var context = new JSContext();
            var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            context["hostWait"] = JSValue.CreateFunction((in Arguments a) =>
            {
                Task.Run(() => { Thread.Sleep(50); gate.TrySetResult(true); });
                context.WaitFor(gate.Task);
                return JSUndefined.Value;
            });

            context.Eval("""
                globalThis.log = '';
                Promise.resolve().then(function () { globalThis.log += 'job'; });
                globalThis.log += 'before';
                hostWait();
                globalThis.log += '|after';
                """);

            Assert.Equal("beforejob|after", context.Eval("globalThis.log").ToString());
        });
    }

    [Fact]
    public void WaitForReturnsTheValueAndPropagatesAFault()
    {
        WithinBudget(() =>
        {
            using var context = new JSContext();

            Assert.Equal(42, context.WaitFor(Task.Run(() => 42)));

            var boom = Task.Run(new Func<int>(() => throw new InvalidOperationException("boom")));
            var thrown = Assert.Throws<InvalidOperationException>(() => context.WaitFor(boom));
            Assert.Equal("boom", thrown.Message);
        });
    }

    [Fact]
    public void WaitForOutsideAnExecutionIsAnOrdinaryWait()
    {
        // There is no context held to release, so the suspension is a no-op and the depth
        // bookkeeping has to survive being asked to release zero.
        WithinBudget(() =>
        {
            using var context = new JSContext();
            context.WaitFor(Task.Delay(20));
            Assert.Equal("3", context.Eval("1 + 2").ToString());
        });
    }

    [Fact]
    public void ReleasingAndRetakingTheContextLeavesTheExclusionIntact()
    {
        // The suspension hands the context to another thread on purpose, so the invariant has to
        // hold across the handover in both directions — the counter must stay at one rather than
        // recording the deliberate handoff as a violation.
        WithinBudget(() =>
        {
            JSContext.ResetExecutionConcurrency();

            using var context = new JSContext();
            context.Eval("globalThis.n = 0;");

            context["hostWait"] = JSValue.CreateFunction((in Arguments a) =>
            {
                var work = Task.Run(() =>
                {
                    for (var i = 0; i < 20; i++)
                        context.Eval("globalThis.n = globalThis.n + 1;");
                });

                context.WaitFor(work);
                return JSUndefined.Value;
            });

            context.Eval("hostWait();");

            var (peak, overlaps) = JSContext.ExecutionConcurrency;
            Assert.Equal(0, overlaps);
            Assert.Equal(1, peak);
            Assert.Equal("20", context.Eval("String(globalThis.n)").ToString());
        });
    }

    [Fact]
    public void NestedExecutionsAreReleasedAndRestoredTogether()
    {
        // The depth released is also the number of lock entries this thread holds, so a wait made
        // two levels deep has to give up both and take both back. Getting that wrong leaks the lock
        // and the next entry deadlocks — which no value assertion would catch.
        WithinBudget(() =>
        {
            using var context = new JSContext();

            using (context.EnterExecution())
            using (context.EnterExecution())
                context.WaitFor(Task.Delay(20));

            // The context must be fully released by now: another thread can take it.
            var other = Task.Run(() =>
            {
                using (context.EnterExecution())
                    return context.Eval("6 * 7").ToString();
            });

            Assert.True(other.Wait(5000), "the context was not released");
            Assert.Equal("42", other.Result);
        });
    }
}
