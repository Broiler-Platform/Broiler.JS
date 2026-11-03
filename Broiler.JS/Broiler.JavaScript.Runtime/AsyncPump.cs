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
    /// <para>
    /// <b>The loop ends when the function's task has completed AND nothing is left queued, not when
    /// the task completes.</b> The task's completion is observed by a continuation on the thread
    /// pool, which runs beside this loop. It used to end the loop by calling
    /// <see cref="BlockingCollection{T}.CompleteAdding()"/> there and then, and the loop went on to
    /// drain only what had already been queued at that instant. A job still running on this thread
    /// that queued another — a promise reaction settling the next promise of a chain the script did
    /// not return, an async generator's step resolving its <c>next()</c> promise — then had its
    /// <see cref="Post"/> refused with an <see cref="InvalidOperationException"/>, which the
    /// reaction's own error handling swallowed, and the rest of the chain silently never ran. How
    /// much of it ran depended on how far this thread got before the pool thread did, so the same
    /// program gave different results run to run and machine to machine.
    /// </para>
    /// <para>
    /// <see cref="Complete"/> now only queues a check behind the work already queued. Run on this
    /// thread, the check ends the loop when the queue is empty and otherwise queues itself again
    /// behind what is there. Everything a job queues from this thread it queues while it runs, so
    /// once the check finds the queue empty no job of this loop can queue another, and the loop ends
    /// rather than waiting. A chain that never stops queueing never lets the check through, which is
    /// what a JavaScript host does with the same program.
    /// </para>
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