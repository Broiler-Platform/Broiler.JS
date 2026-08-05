namespace Broiler.JavaScript.Runtime;

/// <summary>
/// Marks a <see cref="System.Threading.SynchronizationContext"/> that runs what it is given on the
/// thread executing JavaScript, one item at a time — so a job posted to it cannot run beside the
/// JavaScript that queued it.
/// </summary>
/// <remarks>
/// <para>
/// <b>The engine cannot assume that of an ambient context, and used to.</b> Promise reactions and
/// <c>await</c> resumptions were posted to <c>SynchronizationContext.Current</c> whenever one was
/// present, on the reasoning that a context being pumped is the JavaScript thread. That holds for
/// <see cref="AsyncPump"/>'s, which is why it carries this marker. It does not hold for an
/// arbitrary one: xUnit installs <c>AsyncTestSyncContext</c> on every test thread and dispatches
/// through the thread pool, so the "safe" path ran user JavaScript concurrently with the test that
/// queued it — the same defect as the no-context fallback, reached by the opposite branch.
/// </para>
/// <para>
/// A context without this marker is not distrusted, only not treated as the job queue: while
/// JavaScript is executing the job takes the context's own queue instead, which runs it on the
/// JavaScript thread at the point the specification says. Timers keep using the host context as
/// before — a timer callback is a task, not a microtask, and arrives when nothing is running.
/// </para>
/// </remarks>
public interface IJSJobPump
{
}
