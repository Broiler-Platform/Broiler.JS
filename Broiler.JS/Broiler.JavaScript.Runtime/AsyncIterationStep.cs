using Broiler.JavaScript.Storage;

namespace Broiler.JavaScript.Runtime;

/// <summary>
/// One step of <c>for await…of</c>, split so the async function itself awaits the iterator's result
/// instead of the host blocking on it.
/// </summary>
/// <remarks>
/// <para>
/// <b>The defect this exists to fix was a deadlock, not a wrong answer.</b> Async iteration used to
/// unwrap the result of <c>next()</c> inside <see cref="JSIterator"/> with
/// <c>promise.Task.GetAwaiter().GetResult()</c> — a blocking wait, on the one thread that is allowed
/// to run this context's JavaScript. That works only while the promise is <em>already</em> settled.
/// The moment <c>next()</c> hands back a promise that still needs a job to run — the ordinary shape,
/// <c>return somePromise.then(…)</c> — the settling job can never run, because the queue that would
/// run it drains on the way out of the execution the thread is stuck inside.
/// <c>JSMicrotaskQueue</c> names this exact pattern as the one it cannot support.
/// </para>
/// <para>
/// So the step is now three pieces the compiler can put an <c>await</c> between:
/// <see cref="IElementEnumerator.AsyncNextRaw"/> calls <c>next()</c> and hands back its result
/// unexamined, the state machine awaits that value the way it awaits any other, and
/// <see cref="IsDone"/> / <see cref="Value"/> read the settled record. Nothing blocks, so the
/// settling job runs at the checkpoint it was queued for.
/// </para>
/// <para>
/// <b>The sync-iterable fallback goes through the same three pieces.</b> <c>for await</c> over an
/// array reaches an ordinary element enumerator, whose default <c>AsyncNextRaw</c> synthesises the
/// same <c>{value, done}</c> record; awaiting a plain object is a tick and nothing more, which is
/// what the specification's async-from-sync wrapper does anyway.
/// </para>
/// </remarks>
public static class AsyncIterationStep
{
    /// <summary>Whether the settled step result says the iterator is exhausted.</summary>
    public static bool IsDone(JSValue result) => Validate(result)[KeyStrings.done].BooleanValue;

    /// <summary>The value carried by a settled step result.</summary>
    public static JSValue Value(JSValue result) => Validate(result)[KeyStrings.value];

    /// <summary>The record an exhausted synchronous enumerator reports.</summary>
    public static JSValue DoneResult() => Record(JSUndefined.Value, done: true);

    /// <summary>The record a synchronous enumerator reports for one element.</summary>
    public static JSValue ValueResult(JSValue value) => Record(value, done: false);

    /// <summary>
    /// IteratorResult's own check, applied after the await rather than before it: for a real async
    /// iterator the object under test is what the promise resolved to, so validating the promise
    /// would have tested the wrong thing.
    /// </summary>
    private static JSValue Validate(JSValue result)
    {
        if (!result.IsObject)
            throw JSValue.NewTypeError("Iterator next result is not an object");

        return result;
    }

    private static JSValue Record(JSValue value, bool done)
    {
        var record = JSObject.NewWithProperties();
        record.FastAddValue(KeyStrings.value, value, JSPropertyAttributes.EnumerableConfigurableValue);
        record.FastAddValue(KeyStrings.done, done ? JSValue.BooleanTrue : JSValue.BooleanFalse,
            JSPropertyAttributes.EnumerableConfigurableValue);
        return record;
    }
}
