using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Runtime.Tests;

// AsyncPump.Run ends its loop when the function's task has completed and nothing is left queued.
// Work a job queues on the pump after the task's completion has been seen on the thread pool still
// runs. The old pump refused it, which is how an unreturned promise chain lost its tail under
// JSContext.Execute (ExecuteDrainsJobQueueTests, AsyncGeneratorAwaitTests.YieldStarOverAsyncIterable).
public sealed class AsyncPumpTests
{
    // The job waits until the task's completion has been seen on the thread pool, and only then
    // queues more work, which is the interleaving that lost it. The wait is for a continuation the
    // job registers after AsyncPump.Run registered its own, plus one more hop through the pool, so
    // by the time the job posts, the pump's continuation has almost always run (continuations on
    // one task are not guaranteed to run in order, so the old pump could rarely pass; the fixed one
    // cannot fail). With the old pump the Post threw InvalidOperationException ("The collection has
    // been marked as complete with regards to additions") out of Run.
    [Fact]
    public void Run_runs_work_queued_after_the_task_completed()
    {
        var functionTask = new TaskCompletionSource();
        var completionSeen = new ManualResetEventSlim();
        var ran = new List<string>();

        AsyncPump.Run(() =>
        {
            var pump = SynchronizationContext.Current!;
            pump.Post(_ =>
            {
                functionTask.Task.ContinueWith(
                    _ => Task.Run(completionSeen.Set),
                    CancellationToken.None,
                    TaskContinuationOptions.None,
                    TaskScheduler.Default);

                functionTask.SetResult();
                Assert.True(completionSeen.Wait(TimeSpan.FromSeconds(30)), "the task's completion was not seen");

                // A chain: each job queues the next, as promise reactions do.
                pump.Post(_ =>
                {
                    ran.Add("first");
                    pump.Post(_ => ran.Add("second"), null);
                }, null);
            }, null);

            return functionTask.Task;
        });

        Assert.Equal(["first", "second"], ran);
    }

    [Fact]
    public void Run_of_T_runs_work_queued_after_the_task_completed_and_returns_the_result()
    {
        var functionTask = new TaskCompletionSource<int>();
        var completionSeen = new ManualResetEventSlim();
        var ran = false;

        var result = AsyncPump.Run(() =>
        {
            var pump = SynchronizationContext.Current!;
            pump.Post(_ =>
            {
                functionTask.Task.ContinueWith(
                    _ => Task.Run(completionSeen.Set),
                    CancellationToken.None,
                    TaskContinuationOptions.None,
                    TaskScheduler.Default);

                functionTask.SetResult(42);
                Assert.True(completionSeen.Wait(TimeSpan.FromSeconds(30)), "the task's completion was not seen");
                pump.Post(_ => ran = true, null);
            }, null);

            return functionTask.Task;
        });

        Assert.Equal(42, result);
        Assert.True(ran);
    }

    // The other half of the contract: with nothing queued the loop ends rather than waiting for work
    // that will never come — for a task that is already complete when Run receives it, and for one
    // that completes later on another thread.
    [Fact]
    public void Run_returns_when_nothing_is_queued()
    {
        Assert.Equal(7, RunOnWorker(() => AsyncPump.Run(() => Task.FromResult(7))));
        Assert.Equal(8, RunOnWorker(() => AsyncPump.Run(async () =>
        {
            await Task.Delay(1).ConfigureAwait(false);
            return 8;
        })));
        Assert.Equal(9, RunOnWorker(() => AsyncPump.Run(async () =>
        {
            await Task.Yield();
            return 9;
        })));
    }

    private static T RunOnWorker<T>(Func<T> body)
    {
        var run = Task.Run(body);
        Assert.True(run.Wait(TimeSpan.FromSeconds(30)), "AsyncPump.Run did not return");
        return run.Result;
    }
}
