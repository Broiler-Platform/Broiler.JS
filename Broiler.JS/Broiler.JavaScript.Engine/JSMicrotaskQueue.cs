using System;
using System.Collections.Generic;
using System.Threading;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.Engine;

/// <summary>
/// The job queue a context uses <b>while it is executing JavaScript</b> and the host has installed
/// no <see cref="SynchronizationContext"/> — so a promise reaction or an <c>await</c> resumption
/// runs on the thread that is running JavaScript, after the current script finishes, instead of on
/// a thread-pool thread beside it.
/// </summary>
/// <remarks>
/// <para>
/// <b>What this replaces was not an ordering bug, it was a data race.</b> Both
/// <c>JSPromise.Post</c> and <c>JSAsyncFunction</c>'s <c>Queue</c> fell back to
/// <c>ThreadPool.QueueUserWorkItem</c> whenever no synchronization context was present, which is the
/// default for every plain <c>Eval</c>. A queued job resumes a generator and runs user JavaScript,
/// so the fallback let <b>two threads execute JavaScript in one context at the same time</b> — a
/// pool thread resuming after an <c>await</c> while the calling thread was still evaluating the
/// script that started it. ECMAScript's agent is single-threaded by construction and the whole
/// engine is written on that assumption, so what surfaced as a wrong answer was the visible corner
/// of an unsynchronized heap.
/// </para>
/// <para>
/// It surfaced as a wrong answer because a job that only increments a number is benign:
/// <c>async function f(){ v = v + 1; out = v; await 0; v = v + 10; }</c> called synchronously must
/// leave <c>v</c> at 2 when the caller returns, because <c>await</c> queues a job and the caller's
/// remaining statements belong to the job already running. Measured before the fix, <b>0.60% of
/// 3 000 runs answered "2,12" instead of "2,2"</b>, and only under load — which is why every
/// full-suite run in this phase passed until a saturated container let the pool thread win.
/// </para>
/// <para>
/// <b>The queue is taken only while a JavaScript execution is in progress, and that is what makes
/// it impossible to strand a job.</b> A job is deferred exactly when there is something for it to
/// race; when the depth is zero nothing is running that it could interleave with, so the caller
/// keeps its old thread-pool dispatch and paths that never re-enter the engine — a host
/// <c>Task</c>-backed promise settling long after <c>EvalWithTopLevelAwaitAsync</c> returned — go on
/// working exactly as before. The alternative, queueing unconditionally, deadlocks that case: the
/// job waits for a drain that nothing will ever run.
/// </para>
/// <para>
/// <b>The depth stays at one for the whole drain</b>, so a job that queues another job takes the
/// queue rather than the pool, and the transition back to zero happens under the same lock as the
/// final dequeue — the only window in which an enqueue could be lost. The loop runs until the queue
/// is empty rather than taking one pass, because an <c>await</c> chain resolves one job at a time
/// and stopping early would strand the tail. An unbounded chain hangs the drain, which is what a
/// browser does with the same program.
/// </para>
/// <para>
/// A nested execution — a host callback that evaluates more source while JavaScript is on the
/// stack — must not drain, or a job would run in the middle of another job and reintroduce the
/// interleaving this removes on a single thread. The depth counter is what says which execution is
/// outermost.
/// </para>
/// <para>
/// This is the fallback only. When a host installs a context — <c>Execute</c> and
/// <c>ExecuteAsync</c> do, via <c>AsyncPump</c> — jobs post there and are pumped on that thread,
/// which was already correct and is untouched.
/// </para>
/// </remarks>
internal sealed class JSMicrotaskQueue
{
    private readonly object gate = new();
    private readonly Queue<Action> jobs = new();
    private int executionDepth;

    /// <summary>
    /// Queues <paramref name="job"/> to run on the JavaScript thread when the current execution
    /// finishes, or returns <c>false</c> when no execution is in progress and the caller should
    /// dispatch it itself.
    /// </summary>
    public bool TryPost(Action job)
    {
        lock (gate)
        {
            if (executionDepth == 0)
                return false;

            jobs.Enqueue(job);
            return true;
        }
    }

    /// <summary>Marks the start of a JavaScript execution on this context.</summary>
    public ExecutionScope EnterExecution()
    {
        lock (gate)
            executionDepth++;

        return new ExecutionScope(this);
    }

    internal readonly struct ExecutionScope(JSMicrotaskQueue owner) : IDisposable
    {
        public void Dispose() => owner.Exit();
    }

    private void Exit()
    {
        lock (gate)
        {
            // Not the outermost execution: nothing to drain, and draining here would run a job in
            // the middle of the job that is still on the stack.
            if (executionDepth > 1)
            {
                executionDepth--;
                return;
            }
        }

        // Deliberately NOT decrementing to zero yet. The depth stays at one for the whole drain so
        // that a job queueing another job still takes the queue, and so that the return to zero can
        // be made atomic with the observation that the queue is empty.
        while (true)
        {
            Action job;
            lock (gate)
            {
                if (jobs.Count == 0)
                {
                    executionDepth = 0;
                    return;
                }

                job = jobs.Dequeue();
            }

            // A job is a complete unit of work, and there is no caller left to hand its exception
            // to — the script that queued it has returned. So an unhandled one is reported the way
            // an unhandled error is and the loop continues; the alternative is one bad reaction
            // silently stranding every job behind it.
            try
            {
                job();
            }
            catch (Exception ex)
            {
                (JSEngine.Current as JSContext)?.ReportError(ex);
            }
        }
    }
}
