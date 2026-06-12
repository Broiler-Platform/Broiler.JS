using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Broiler.JavaScript.BuiltIns.Error;
using Broiler.JavaScript.BuiltIns.Promise;
using Broiler.JavaScript.BuiltIns.Symbol;
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
    // chain. Returns a Promise that resolves with undefined or rejects with the chain.
    internal static JSValue DisposeResourcesAsync(List<Func<JSValue>> resources)
    {
        var snapshot = new List<Func<JSValue>>(resources);
        resources.Clear();
        return RunAsync(snapshot).ToPromise();
    }

    private static async Task RunAsync(List<Func<JSValue>> resources)
    {
        JSValue pending = null;

        for (var i = resources.Count - 1; i >= 0; i--)
        {
            try
            {
                var result = resources[i]();
                await JSPromise.Await(result);
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

        if (pending != null)
            JSException.Throw(pending);
    }
}
