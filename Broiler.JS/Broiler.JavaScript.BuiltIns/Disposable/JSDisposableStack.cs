using Broiler.JavaScript.BuiltIns.Promise;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Broiler.JavaScript.BuiltIns.Error;
using Broiler.JavaScript.BuiltIns.Symbol;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Disposable;

public class JSDisposableStack : IJSDisposableStack, IDisposable, IAsyncDisposable
{
    public bool Disposed { get; private set; }
    public bool isAsync { get; private set; }
    public JSValue Error { get; private set; }
    // `discardResult`: an `await using` resource disposed through its @@dispose fallback, whose
    // return value the spec's wrapper closure discards (it awaits undefined instead). `method` is
    // null for an `await using` of null or undefined, which is recorded only so that the disposal
    // still performs an Await for it (DisposeResources' needsAwait).
    private Stack<(JSValue value, JSValue method, bool async, bool discardResult)> stack = new();

    // DisposeResources' needsAwait / hasAwaited, and the value the compiled `await` awaits next.
    private bool needsAwait;
    private bool hasAwaited;
    private JSValue awaitValue = JSUndefined.Value;

    public JSDisposableStack() { }

    // CreateDisposableResource: the dispose method is looked up and validated *now* (at the `using`
    // declaration), not deferred to disposal. A null/undefined resource (sync) is a no-op; any other
    // non-object, or an object whose @@dispose (or, for await using, @@asyncDispose with a @@dispose
    // fallback) method is absent or not callable, is a TypeError here — so e.g. `using x = {}` throws
    // a TypeError rather than surfacing as a SuppressedError at end of block.
    public void AddDisposableResource(JSValue value, bool async = false)
    {
        if (value.IsNullOrUndefined)
        {
            // AddDisposableResource: an `await using` of null or undefined is still recorded, with
            // an undefined method, so that the disposal performs an Await for it.
            if (async)
            {
                isAsync = true;
                stack.Push((JSUndefined.Value, null, true, false));
            }

            return;
        }

        if (!value.IsObject)
            throw JSEngine.NewTypeError("using declaration requires an object with a Symbol.dispose method");

        JSValue method;
        var discardResult = false;
        if (async)
        {
            method = DisposableStackShared.GetDisposeMethod(value, (IJSSymbol)JSSymbol.asyncDispose);
            if (!method.IsFunction)
            {
                method = DisposableStackShared.GetDisposeMethod(value, (IJSSymbol)JSSymbol.dispose);
                discardResult = true;
            }
        }
        else
        {
            method = DisposableStackShared.GetDisposeMethod(value, (IJSSymbol)JSSymbol.dispose);
        }

        if (!method.IsFunction)
            throw JSEngine.NewTypeError("using declaration value is not disposable (no Symbol.dispose method)");

        isAsync |= async;
        stack.Push((value, method, async, discardResult));
    }

    public JSValue SeedPendingError(Exception bodyException)
    {
        // The block body completed abruptly; record its thrown value as the initial
        // pending error so the disposal loop wraps it (SuppressedError.suppressed) when a
        // disposer also throws, and re-throws it unchanged when none do.
        Error = bodyException is JSException jx ? (jx.Error ?? JSError.From(bodyException)) : JSError.From(bodyException);
        return JSUndefined.Value;
    }

    public JSValue Dispose()
    {
        if (!isAsync)
        {
            ((IDisposable)this).Dispose();
            return JSUndefined.Value;
        }

        // Only a scope compiled without awaits of its own reaches this with async resources:
        // dispose them on promise jobs and hand back the promise of the whole disposal.
        return DisposableStackShared.DisposeAwaitingEach(NextDisposer, Error);
    }

    /// <summary>
    /// DisposeResources for an <c>await using</c> scope, one Await at a time: runs the disposers
    /// (last in, first out) up to the next one whose result must be awaited and returns true, with
    /// that value in <see cref="TakeAwaitValue"/>; returns false once the disposal is done. The
    /// scope's compiled disposal is
    /// <c>while (DisposeStep()) try { await TakeAwaitValue(); } catch (e) { RecordDisposeError(e); }
    /// CompleteDisposal();</c>
    /// so each Await of the disposal is the async function's own — one job, as the spec's — and in
    /// an async generator it is an await, never a yield the consumer would see.
    /// </summary>
    /// <remarks>
    /// It replaced one compiled <c>await</c> of a thenable that the disposal settled when its last
    /// Await would have, which relied on the async-function driver calling a thenable's
    /// <c>then</c> synchronously (it now does so in a job, as PromiseResolve does), awaited nothing
    /// for `await using x = null`, and was lowered as a <c>yield</c> in an async generator.
    /// </remarks>
    public bool DisposeStep()
    {
        while (stack.Count > 0)
        {
            var (v, m, a, discardResult) = stack.Peek();

            // A sync-dispose resource after an `await using` of null or undefined: Await(undefined)
            // first, and dispose it on the next step.
            if (!a && needsAwait && !hasAwaited)
            {
                needsAwait = false;
                awaitValue = JSUndefined.Value;
                return true;
            }

            stack.Pop();

            if (m == null)
            {
                needsAwait = true;
                continue;
            }

            JSValue result;
            try
            {
                result = discardResult
                    ? DisposableStackShared.CallDisposeFallback(m, v)
                    : m.InvokeFunction(new Arguments(v));
            }
            catch (Exception ex)
            {
                RecordDisposeError(ex);
                continue;
            }

            if (!a)
                continue;

            hasAwaited = true;
            awaitValue = result;
            return true;
        }

        if (needsAwait && !hasAwaited)
        {
            needsAwait = false;
            hasAwaited = true;
            awaitValue = JSUndefined.Value;
            return true;
        }

        return false;
    }

    /// <summary>The value the compiled disposal awaits next (see <see cref="DisposeStep"/>).</summary>
    public JSValue TakeAwaitValue()
    {
        var value = awaitValue;
        awaitValue = JSUndefined.Value;
        return value;
    }

    /// <summary>Records an error thrown by a disposer, or by the Await of its result, into the
    /// pending completion: the first as is, each later one as a SuppressedError wrapping it.</summary>
    public JSValue RecordDisposeError(Exception exception)
    {
        var thrown = exception is JSException jx ? (jx.Error ?? JSError.From(exception)) : JSError.From(exception);
        Error = Error == null ? thrown : new JSSuppressedError(thrown, Error);
        return JSUndefined.Value;
    }

    /// <summary>Ends the disposal: throws the pending completion's error, if there is one.</summary>
    public JSValue CompleteDisposal()
    {
        if (Error != null)
            JSException.Throw(Error);

        return JSUndefined.Value;
    }

    private (Func<JSValue> Dispose, bool Async)? NextDisposer()
    {
        if (stack.Count == 0)
            return null;

        var (v, m, a, discardResult) = stack.Pop();
        if (m == null)
            return (null, a);

        if (discardResult)
            return (() => DisposableStackShared.CallDisposeFallback(m, v), a);

        return (() => m.InvokeFunction(new Arguments(v)), a);
    }

    void IDisposable.Dispose()
    {
        while (stack.Count > 0)
        {
            var (v, m, a, _) = stack.Pop();

            if (a)
                throw JSEngine.NewTypeError("Async resource must not be disposed synchronously.");

            if (m == null)
                continue;

            try
            {
                m.InvokeFunction(new Arguments(v));
            }
            catch (Exception ex)
            {
                var thrown = ex is JSException jx ? (jx.Error ?? JSError.From(ex)) : JSError.From(ex);
                Error = Error == null ? thrown : new JSSuppressedError(thrown, Error);
            }
        }

        if (Error != null)
            JSException.Throw(Error);
    }

    private async Task DisposeAsync()
    {
        while (stack.Count > 0)
        {
            var (v, m, a, _) = stack.Pop();
            if (m == null)
                continue;

            try
            {
                var r = m.InvokeFunction(new Arguments(v));
                if (a)
                    await JSPromise.Await(r);
            }
            catch (Exception ex)
            {
                var thrown = ex is JSException jx ? (jx.Error ?? JSError.From(ex)) : JSError.From(ex);
                Error = Error == null ? thrown : new JSSuppressedError(thrown, Error);
            }
        }

        if (Error != null)
            JSException.Throw(Error);
    }

    async ValueTask IAsyncDisposable.DisposeAsync() => await DisposeAsync();
}
