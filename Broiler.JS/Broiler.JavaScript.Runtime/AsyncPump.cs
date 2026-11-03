using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Broiler.JavaScript.Runtime;

/// <summary>Provides a pump that supports running asynchronous methods on the current thread.</summary>
public static class AsyncPump
{
    /// <summary>Runs the specified asynchronous function.</summary>
    /// <param name="func">The asynchronous function to execute.</param>
    /// <remarks>
    /// Runs <paramref name="func"/> with a single-threaded <see cref="SynchronizationContext"/> on the
    /// calling thread, and returns once the task it returns has completed and every item posted to
    /// that context from the calling thread has run, including the items those queue in turn. A chain
    /// of items that keeps queueing another keeps <c>Run</c> from returning. Work posted from another
    /// thread after <c>Run</c> has ended its loop is refused: <see cref="SynchronizationContext.Post"/>
    /// throws <see cref="InvalidOperationException"/> on that thread.
    /// </remarks>
    public static T Run<T>(Func<Task<T>> func)
    {
        ArgumentNullException.ThrowIfNull(func);

        var prevCtx = SynchronizationContext.Current;

        try
        {
            // Establish the new context
            var syncCtx = new SingleThreadSynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(syncCtx);

            // Invoke the function and alert the context to when it completes
            var t = func() ?? throw new InvalidOperationException("No task provided.");
            t.ContinueWith(delegate { syncCtx.Complete(); }, TaskScheduler.Default);

            // Pump continuations and propagate any exceptions
            syncCtx.RunOnCurrentThread();
            return t.GetAwaiter().GetResult();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(prevCtx);
        }
    }

    /// <summary>Runs the specified asynchronous function.</summary>
    /// <param name="func">The asynchronous function to execute.</param>
    /// <remarks>
    /// Runs <paramref name="func"/> with a single-threaded <see cref="SynchronizationContext"/> on the
    /// calling thread, and returns once the task it returns has completed and every item posted to
    /// that context from the calling thread has run, including the items those queue in turn. A chain
    /// of items that keeps queueing another keeps <c>Run</c> from returning. Work posted from another
    /// thread after <c>Run</c> has ended its loop is refused: <see cref="SynchronizationContext.Post"/>
    /// throws <see cref="InvalidOperationException"/> on that thread.
    /// </remarks>
    public static void Run(Func<Task> func)
    {
        ArgumentNullException.ThrowIfNull(func);

        var prevCtx = SynchronizationContext.Current;
        try
        {
            // Establish the new context
            var syncCtx = new SingleThreadSynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(syncCtx);

            // Invoke the function and alert the context to when it completes
            var t = func();
            if (t == null) throw new InvalidOperationException("No task provided.");
            t.ContinueWith(delegate { syncCtx.Complete(); }, TaskScheduler.Default);

            // Pump continuations and propagate any exceptions
            syncCtx.RunOnCurrentThread();
            t.GetAwaiter().GetResult();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(prevCtx);
        }
    }

    /// <summary>Provides a SynchronizationContext that's single-threaded.</summary>
    /// <remarks>
    /// The loop ends when the function's task has completed and nothing is left queued, not when the
    /// task completes. The completion is seen by a continuation on the thread pool, which calls
    /// <see cref="Complete"/>; that only queues a check behind the work already queued. Run on this
    /// thread, the check ends the loop when the queue is empty and otherwise queues itself again
    /// behind what is there. A job running on this thread queues its successors while it runs, so
    /// once the check finds the queue empty no job running on this thread can queue another, and the
    /// loop ends rather than waiting. A chain that never stops queueing never lets the check through,
    /// which is what a JavaScript host does with the same program. Work posted from another thread
    /// after the loop has ended, such as the settling of a promise backed by a host task the script
    /// did not return, is still refused: the loop does not wait for host work the script does not
    /// return.
    /// </remarks>
    private sealed class SingleThreadSynchronizationContext : SynchronizationContext, IJSJobPump
    {
        /// <summary>The queue of work items.</summary>
        private readonly BlockingCollection<KeyValuePair<SendOrPostCallback, object>> m_queue = [];

        /// <summary>Dispatches an asynchronous message to the synchronization context.</summary>
        /// <param name="d">The System.Threading.SendOrPostCallback delegate to call.</param>
        /// <param name="state">The object passed to the delegate.</param>
        public override void Post(SendOrPostCallback d, object state)
        {
            ArgumentNullException.ThrowIfNull(d);
            m_queue.Add(new KeyValuePair<SendOrPostCallback, object>(d, state));
        }

        /// <summary>Not supported.</summary>
        public override void Send(SendOrPostCallback d, object state) => throw new NotSupportedException("Synchronously sending is not supported.");

        /// <summary>Runs an loop to process all queued work items.</summary>
        public void RunOnCurrentThread()
        {
            foreach (var workItem in m_queue.GetConsumingEnumerable())
                workItem.Key(workItem.Value);
        }

        /// <summary>
        /// Notifies the context that the function's task has completed: the loop ends once the work
        /// queued so far, and whatever that work queues in turn, has run.
        /// </summary>
        public void Complete() => Post(static self => ((SingleThreadSynchronizationContext)self).CompleteWhenDrained(), this);

        /// <summary>
        /// Runs on the loop's thread. Ends the loop when nothing is queued, otherwise checks again
        /// after what is queued has run.
        /// </summary>
        private void CompleteWhenDrained()
        {
            if (m_queue.Count == 0)
                m_queue.CompleteAdding();
            else
                Complete();
        }
    }
}