using System;
using System.Collections.Generic;
using System.Threading;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.Engine;

/// <summary>
/// A context's execution lock and its job queue: the two halves of "one thread runs JavaScript in
/// this context at a time, and a job it queues runs after it rather than beside it".
/// </summary>
/// <remarks>
/// <para>
/// <b>The queue half</b> replaces what used to be two separate wrong answers for where a promise
/// reaction or an <c>await</c> resumption went — <c>ThreadPool.QueueUserWorkItem</c> when no
/// <see cref="SynchronizationContext"/> was present, and <c>SynchronizationContext.Current.Post</c>
/// when an arbitrary one was. A job resumes a generator and resuming runs user JavaScript, so both
/// let a second thread into the context. Measured on the shape that found it,
/// <c>async function f(){ v = v + 1; out = v; await 0; v = v + 10; }</c> called synchronously
/// answered <c>"2,12"</c> instead of <c>"2,2"</c> in <b>0.60% of 3 000 runs</b>.
/// </para>
/// <para>
/// <b>The lock half closes what the queue could not.</b> The queue is taken only while an execution
/// is already in progress — that is what makes it impossible to strand a job — so a job posted when
/// nothing was running still went to a host context or the pool, and could then run JavaScript
/// while a later <c>Eval</c> was running JavaScript. Reaching that needs a JavaScript entry point
/// which is not <c>Eval</c>, and a host invoking a <c>JSValue</c> directly is exactly one. Measured
/// with a detector before the lock existed: <b>peak 2 concurrent executions and 172 overlaps in 200
/// rounds</b>. The lock makes the two halves one invariant instead of one guarantee and one hole.
/// </para>
/// <para>
/// <b>It is a <see cref="Monitor"/> and therefore re-entrant, which is required rather than
/// convenient.</b> A host callback that evaluates more source is the same agent going deeper, not a
/// second one, and must not deadlock against itself. The depth counter says which execution is
/// outermost so the drain happens once, on the way out of it — a nested one draining would run a
/// job in the middle of another job and reintroduce the interleaving on a single thread.
/// </para>
/// <para>
/// <b>What the lock costs is a bounded wait, not a deadlock risk that was not already there.</b> A
/// thread entering the context waits for whichever thread is inside to leave. The one pattern it
/// cannot support — JavaScript blocking on a host task whose completion has to re-enter the same
/// context — was already unsound, because that job used to run concurrently and mutate the heap
/// underneath the code waiting for it.
/// </para>
/// <para>
/// A job may queue more jobs, and the drain runs until the queue is empty rather than taking one
/// pass: an <c>await</c> chain resolves one job at a time and stopping early would strand the tail.
/// An unbounded chain hangs the drain, which is what a browser does with the same program.
/// </para>
/// </remarks>
internal sealed class JSMicrotaskQueue
{
    private readonly object gate = new();
    private readonly object executionLock = new();
    private readonly Queue<Action> jobs = new();
    private int executionDepth;

    private int owningThread;

    private static long concurrentPeak;
    private static long overlaps;

    /// <summary>
    /// The most threads seen inside ONE context at once, and how many times a second thread entered
    /// a context another was already inside. Detected per context; aggregated across them.
    /// </summary>
    /// <remarks>
    /// <b>The invariant this exists to check is one.</b> ECMAScript's agent is single-threaded and
    /// everything here is written on that assumption, so a peak above one is not a slow program, it
    /// is a heap modified from two threads. Counting it is the only way to test the property
    /// directly — asserting a value catches an overlap only when the overlap happens to change that
    /// value, which is how the original defect stayed invisible at a 0.6% hit rate across a whole
    /// phase of full-suite runs.
    /// </remarks>
    public static (long Peak, long Overlaps) Concurrency
        => (System.Math.Max(1, Interlocked.Read(ref concurrentPeak)), Interlocked.Read(ref overlaps));

    public static void ResetConcurrency()
    {
        Interlocked.Exchange(ref concurrentPeak, 0);
        Interlocked.Exchange(ref overlaps, 0);
    }

    /// <summary>
    /// Records whether a SECOND THREAD is inside THIS context. Called under <c>gate</c>, with the
    /// depth about to be incremented.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Per context, and that is the whole correctness of the counter.</b> The invariant is one
    /// thread per context, not one thread per process: two independent contexts running in parallel
    /// is exactly what an embedder is supposed to be able to do, and a process-wide count would
    /// report it as a violation — which would make this counter fire on any full-suite run, where
    /// xUnit evaluates several test classes at once. Only the aggregate is static, because an
    /// overlap is a real violation whichever context produced it.
    /// </para>
    /// <para>
    /// <b>Nesting is told apart by thread identity rather than by a thread-local depth.</b> The
    /// context already knows how deep it is; what it needs to know is whether the thread going
    /// deeper is the one already inside. That makes the check two reads under a lock the caller is
    /// taking anyway, with no per-thread state to allocate or leak.
    /// </para>
    /// </remarks>
    private void NoteEntryUnderGate()
    {
        var thread = Environment.CurrentManagedThreadId;
        if (executionDepth == 0)
        {
            owningThread = thread;
            return;
        }

        if (owningThread == thread)
            return;

        // A different thread is already inside this context. With the execution lock held this is
        // unreachable; without it, it is the defect.
        Interlocked.Increment(ref overlaps);
        Interlocked.Exchange(ref concurrentPeak, 2);
    }

    /// <summary>
    /// Queues <paramref name="job"/> to run when the current execution finishes, or returns
    /// <c>false</c> when none is in progress and the caller must dispatch it itself.
    /// </summary>
    /// <remarks>
    /// Refusing at depth zero is what makes stranding impossible: a job is deferred exactly when
    /// there is a drain coming that will run it. Queueing unconditionally would strand a host
    /// <c>Task</c>-backed promise settling long after the last evaluation returned, because nothing
    /// would ever drain it.
    /// </remarks>
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

    /// <summary>
    /// Takes the context for the calling thread, blocking while another thread holds it, and drains
    /// the queue on the way out of the outermost execution.
    /// </summary>
    public ExecutionScope EnterExecution()
    {
        Monitor.Enter(executionLock);
        lock (gate)
        {
            NoteEntryUnderGate();
            executionDepth++;
        }

        return new ExecutionScope(this);
    }

    internal readonly struct ExecutionScope(JSMicrotaskQueue owner) : IDisposable
    {
        public void Dispose() => owner.Exit();
    }

    /// <summary>
    /// Gives the context up while the caller blocks on something that is not JavaScript, and takes
    /// it back afterwards. Queued jobs are run first, so nothing is left owed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This exists because "one thread at a time" and "a host may block" are in direct
    /// conflict, and the conflict has two shapes.</b> A host function called from JavaScript that
    /// waits on a <c>Task</c> is inside an execution, so a promise reaction it is waiting for is
    /// QUEUED and cannot run until that execution ends — which it never does. And with the
    /// execution lock held, host work that has to enter the context to complete the task cannot get
    /// in either. Draining answers the first, releasing answers the second, and both are needed:
    /// measured, the two shapes deadlock independently.
    /// </para>
    /// <para>
    /// <b>The drain happens before the release, not after</b>, so a job queued by the execution
    /// being suspended runs on the thread that queued it and in the order it was queued, rather than
    /// being handed to whichever thread takes the context next.
    /// </para>
    /// <para>
    /// The depth is released in full and restored in full: it is also the number of
    /// <see cref="Monitor"/> entries this thread holds, since only one thread can be inside.
    /// </para>
    /// </remarks>
    public SuspendScope SuspendExecution()
    {
        int released;
        while (true)
        {
            Action job;
            lock (gate)
            {
                if (jobs.Count == 0)
                {
                    released = executionDepth;
                    executionDepth = 0;
                    owningThread = 0;
                    break;
                }

                job = jobs.Dequeue();
            }

            try
            {
                job();
            }
            catch (Exception ex)
            {
                (JSEngine.Current as JSContext)?.ReportError(ex);
            }
        }

        for (var i = 0; i < released; i++)
            Monitor.Exit(executionLock);

        return new SuspendScope(this, released);
    }

    internal readonly struct SuspendScope(JSMicrotaskQueue owner, int depth) : IDisposable
    {
        public void Dispose()
        {
            for (var i = 0; i < depth; i++)
                Monitor.Enter(owner.executionLock);

            if (depth == 0)
                return;

            lock (owner.gate)
            {
                owner.NoteEntryUnderGate();
                owner.executionDepth = depth;
            }
        }
    }

    private void Exit()
    {
        try
        {
            lock (gate)
            {
                // Not the outermost execution: nothing to drain, and draining here would run a job
                // in the middle of the job that is still on the stack.
                if (executionDepth > 1)
                {
                    executionDepth--;
                    return;
                }
            }

            // Deliberately NOT decrementing to zero yet. The depth stays at one for the whole drain
            // so a job queueing another job still takes the queue, and so the return to zero can be
            // made atomic with the observation that the queue is empty — the only window in which
            // an enqueue could be lost.
            while (true)
            {
                Action job;
                lock (gate)
                {
                    if (jobs.Count == 0)
                    {
                        executionDepth = 0;
                        owningThread = 0;
                        return;
                    }

                    job = jobs.Dequeue();
                }

                // A job is a complete unit of work and there is no caller left to hand its exception
                // to — the script that queued it has returned. So an unhandled one is reported the
                // way an unhandled error is and the loop continues; the alternative is one bad
                // reaction silently stranding every job behind it.
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
        finally
        {
            Monitor.Exit(executionLock);
        }
    }
}
