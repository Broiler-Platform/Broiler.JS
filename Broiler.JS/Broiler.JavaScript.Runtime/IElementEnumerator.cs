namespace Broiler.JavaScript.Runtime;

/// <summary>
/// Enumerator protocol for iterating over JavaScript object elements.
/// </summary>
public interface IElementEnumerator
{
    bool MoveNext(out bool hasValue, out JSValue value, out uint index);
    bool MoveNext(out JSValue value);
    bool MoveNextOrDefault(out JSValue value, JSValue @default);
    JSValue NextOrDefault(JSValue @default);

    /// <summary>
    /// One step of <c>for await…of</c>: the result of <c>next()</c>, handed back <em>unexamined</em>
    /// so the async function can await it itself. See <see cref="AsyncIterationStep"/> for why the
    /// awaiting cannot happen here.
    /// </summary>
    /// <remarks>
    /// The default is the synchronous fallback — <c>for await</c> over an array or any other
    /// non-async iterable — which has no promise to await and so synthesises the same
    /// <c>{value, done}</c> record a real iterator would resolve to. <see cref="JSIterator"/>
    /// overrides it to call the iterator's own <c>next()</c>.
    /// </remarks>
    JSValue AsyncNextRaw() =>
        MoveNext(out var value) ? AsyncIterationStep.ValueResult(value) : AsyncIterationStep.DoneResult();
}
