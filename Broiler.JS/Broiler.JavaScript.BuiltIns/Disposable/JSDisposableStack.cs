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
    private Stack<(JSValue value, JSValue method, bool async)> stack = new();

    public JSDisposableStack() { }

    // CreateDisposableResource: the dispose method is looked up and validated *now* (at the `using`
    // declaration), not deferred to disposal. A null/undefined resource (sync) is a no-op; any other
    // non-object, or an object whose @@dispose (or, for await using, @@asyncDispose with a @@dispose
    // fallback) method is absent or not callable, is a TypeError here — so e.g. `using x = {}` throws
    // a TypeError rather than surfacing as a SuppressedError at end of block.
    public void AddDisposableResource(JSValue value, bool async = false)
    {
        if (value.IsNullOrUndefined)
            return;

        if (!value.IsObject)
            throw JSEngine.NewTypeError("using declaration requires an object with a Symbol.dispose method");

        JSValue method;
        if (async)
        {
            method = DisposableStackShared.GetDisposeMethod(value, (IJSSymbol)JSSymbol.asyncDispose);
            if (!method.IsFunction)
                method = DisposableStackShared.GetDisposeMethod(value, (IJSSymbol)JSSymbol.dispose);
        }
        else
        {
            method = DisposableStackShared.GetDisposeMethod(value, (IJSSymbol)JSSymbol.dispose);
        }

        if (!method.IsFunction)
            throw JSEngine.NewTypeError("using declaration value is not disposable (no Symbol.dispose method)");

        isAsync |= async;
        stack.Push((value, method, async));
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

        var task = DisposeAsync();
        return task.ToPromise();
    }

    void IDisposable.Dispose()
    {
        while (stack.Count > 0)
        {
            var (v, m, a) = stack.Pop();

            if (a)
                throw JSEngine.NewTypeError("Async resource must not be disposed synchronously.");

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
            var (v, m, a) = stack.Pop();
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
