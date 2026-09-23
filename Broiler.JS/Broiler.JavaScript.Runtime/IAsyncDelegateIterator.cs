namespace Broiler.JavaScript.Runtime;

/// <summary>
/// The delegate of a <c>yield*</c> in an async generator, when it is an async iterator: the
/// iterator's own <c>next</c> / <c>throw</c> / <c>return</c>, each handing back its result
/// <em>unexamined</em> for the delegating generator to Await (§15.5.5, <c>yield*</c> with
/// generatorKind async).
/// </summary>
/// <remarks>
/// The generator awaits each result and resumes in the reaction job, as its own <c>await</c> does,
/// so an <c>await</c> inside a native async-generator delegate is awaited there and never surfaces
/// as a delegated value, and a result that is still pending is awaited rather than read as a
/// record. A synchronous iterator (<see cref="IsAsyncIterator"/> false) keeps the enumerator path.
/// </remarks>
public interface IAsyncDelegateIterator
{
    /// <summary>
    /// Whether this enumerator stands for an async iterator. <c>for await</c> also reads it: only
    /// the sync-iterable fallback awaits each value (see <see cref="AsyncIterationStep.AwaitsValue"/>).
    /// </summary>
    bool IsAsyncIterator { get; }

    /// <summary>Calls <c>next(value)</c> and returns its result.</summary>
    JSValue DelegateNext(JSValue value);

    /// <summary>Calls <c>throw(value)</c>; false when the iterator has no <c>throw</c> method.</summary>
    bool TryDelegateThrow(JSValue value, out JSValue result);

    /// <summary>Calls <c>return(value)</c>; false when the iterator has no <c>return</c> method.</summary>
    bool TryDelegateReturn(JSValue value, out JSValue result);
}
