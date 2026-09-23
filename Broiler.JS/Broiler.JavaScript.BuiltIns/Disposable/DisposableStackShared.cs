using System;
using System.Collections.Generic;
using Broiler.JavaScript.BuiltIns.Error;
using Broiler.JavaScript.BuiltIns.Promise;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.Engine.Extensions;

namespace Broiler.JavaScript.BuiltIns.Disposable;

// Shared machinery for the user-facing DisposableStack / AsyncDisposableStack built-ins.
internal static class DisposableStackShared
{
    // OrdinaryCreateFromConstructor entry: a plain call (no new.target) is a TypeError;
    // otherwise allocate from the (possibly subclass) new.target prototype.
    internal static JSObject ResolveConstructorPrototype(string constructorName)
    {
        if (JSEngine.NewTarget == null && (JSEngine.Current as IJSExecutionContext)?.CurrentNewTarget == null)
            throw JSEngine.NewTypeError($"Constructor {constructorName} requires 'new'");

        return JSEngine.NewTargetPrototype;
    }

    // Operating on an already-disposed stack (use/adopt/defer/move) is a ReferenceError.
    internal static void RequireNotDisposed(bool disposed)
    {
        if (disposed)
            throw JSEngine.NewReferenceError("Cannot operate on a disposed stack");
    }

    // GetMethod(V, P): undefined when the property is null/undefined; a TypeError when it
    // is present but not callable; otherwise the callable. (So the async @@asyncDispose →
    // @@dispose fallback only fires when @@asyncDispose is *absent*, not when it is a
    // present-but-invalid value.)
    internal static JSValue GetDisposeMethod(JSValue value, IJSSymbol symbol)
    {
        var method = value[symbol];
        if (method.IsNullOrUndefined)
            return JSUndefined.Value;

        if (!method.IsFunction)
            throw JSEngine.NewTypeError("Disposable resource @@dispose method is not callable");

        return method;
    }

    // DisposeResources for the synchronous stack: run the disposal thunks last-in-first-out,
    // aggregating thrown errors into a SuppressedError chain (newest error wraps the prior).
    internal static void DisposeResources(List<Func<JSValue>> resources)
    {
        JSValue pending = null;

        for (var i = resources.Count - 1; i >= 0; i--)
        {
            try
            {
                resources[i]();
            }
            catch (JSException ex)
            {
                var thrown = ex.Error ?? JSError.From(ex);
                pending = pending == null ? thrown : new JSSuppressedError(thrown, pending);
            }
            catch (Exception ex)
            {
                var thrown = JSError.From(ex);
                pending = pending == null ? thrown : new JSSuppressedError(thrown, pending);
            }
        }

        resources.Clear();

        if (pending != null)
            JSException.Throw(pending);
    }

    // DisposeResources for the asynchronous stack: each disposer's result is awaited (in
    // LIFO order) before the next runs; errors aggregate identically into a SuppressedError
    // chain. Returns a Promise that resolves with undefined or rejects with the chain. A null
    // entry is a resource added as null or undefined, which only makes the disposal await.
    internal static JSValue DisposeResourcesAsync(List<Func<JSValue>> resources)
    {
        var snapshot = new List<Func<JSValue>>(resources);
        resources.Clear();
        var i = snapshot.Count;
        return DisposeAwaitingEach(() => --i >= 0 ? (snapshot[i], true) : null);
    }

    /// <summary>
    /// DisposeResources with its Awaits on promise jobs: <paramref name="next"/> yields the
    /// resources in disposal order (null when none remain) — the disposer, or null for a resource
    /// with an undefined method (an async-dispose resource of null or undefined), and whether its
    /// hint is async-dispose; <paramref name="pending"/> is the completion the disposal starts
    /// from (a block's thrown value, or null). Returns a promise that resolves with undefined, or
    /// rejects with the error — a SuppressedError chain when several were thrown — once the
    /// disposal has resumed from its last Await: AsyncDisposableStack.prototype.disposeAsync.
    /// </summary>
    /// <remarks>
    /// Each Await is PromiseResolve(%Promise%, result) plus a reaction that resumes the disposal,
    /// so it takes one job, on the realm's job queue. It used to be a C# <c>async</c> method
    /// awaiting <see cref="JSPromise.Task"/> and wrapped by <c>Task.ToPromise</c>: its
    /// continuations ran on the thread pool, outside the job queue, so a host that ends when its
    /// job queue drains (the test262 script host) could exit before the disposal — and anything
    /// awaiting it — finished. The needsAwait / hasAwaited bookkeeping is the spec's: a resource
    /// without a method still costs one Await(undefined), before the next sync-dispose resource or
    /// at the end, unless something else was awaited.
    /// </remarks>
    internal static JSValue DisposeAwaitingEach(Func<(Func<JSValue> Dispose, bool Async)?> next, JSValue pending = null)
    {
        var promise = new JSPromise(static (_, _) => { });
        var needsAwait = false;
        var hasAwaited = false;
        (Func<JSValue> Dispose, bool Async)? held = null;
        var continuationContext = System.Threading.SynchronizationContext.Current
            ?? (JSEngine.Current as JSContext)?.synchronizationContext;

        void Record(JSValue thrown)
            => pending = pending == null ? thrown : new JSSuppressedError(thrown, pending);

        void Await(JSValue value)
        {
            JSPromise awaited;
            try
            {
                awaited = JSPromise.PromiseResolveIntrinsic(value);
            }
            catch (Exception ex)
            {
                Record(JSException.JSErrorFrom(ex));
                Step();
                return;
            }

            awaited.AwaitReaction(
                _ => Step(),
                e =>
                {
                    Record(e);
                    Step();
                },
                continuationContext);
        }

        void Step()
        {
            while ((held ?? next()) is { } resource)
            {
                held = null;

                if (!resource.Async && needsAwait && !hasAwaited)
                {
                    needsAwait = false;
                    held = resource;
                    Await(JSUndefined.Value);
                    return;
                }

                if (resource.Dispose == null)
                {
                    needsAwait = true;
                    continue;
                }

                JSValue result;
                try
                {
                    result = resource.Dispose();
                }
                catch (Exception ex)
                {
                    Record(JSException.JSErrorFrom(ex));
                    continue;
                }

                if (!resource.Async)
                    continue;

                hasAwaited = true;
                Await(result);
                return;
            }

            if (needsAwait && !hasAwaited)
            {
                needsAwait = false;
                hasAwaited = true;
                Await(JSUndefined.Value);
                return;
            }

            if (pending != null)
                promise.Reject(pending);
            else
                promise.Resolve(JSUndefined.Value);
        }

        Step();
        return promise;
    }

    /// <summary>
    /// The @@dispose fallback of an async-dispose resource (GetDisposeMethod's closure): calls
    /// <paramref name="method"/> on <paramref name="value"/> and returns undefined — what the
    /// method returned is discarded, never awaited — or a rejected promise with what it threw
    /// (IfAbruptRejectPromise), which the disposal's Await then throws.
    /// </summary>
    internal static JSValue CallDisposeFallback(JSValue method, JSValue value)
    {
        try
        {
            method.InvokeFunction(new Arguments(value));
            return JSUndefined.Value;
        }
        catch (Exception ex)
        {
            return new JSPromise(JSException.JSErrorFrom(ex), JSPromise.PromiseState.Rejected);
        }
    }
}
