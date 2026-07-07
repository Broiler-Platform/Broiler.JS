using System;
using System.Collections.Generic;
using Broiler.JavaScript.BuiltIns.Promise;
using Broiler.JavaScript.BuiltIns.Symbol;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Disposable;

// ES2026 Explicit Resource Management — the user-facing `AsyncDisposableStack` built-in.
// Mirrors DisposableStack, but disposers are awaited (LIFO) and disposeAsync returns a
// Promise. `use` prefers @@asyncDispose and falls back to @@dispose.
[JSClassGenerator("AsyncDisposableStack")]
public partial class JSAsyncDisposableStackObject : JSObject
{
    private bool _disposed;
    private List<Func<JSValue>> _resources = [];

    [JSExport(Length = 0)]
    public JSAsyncDisposableStackObject(in Arguments a) : base(DisposableStackShared.ResolveConstructorPrototype("AsyncDisposableStack"))
    {
    }

    // get AsyncDisposableStack.prototype.disposed
    [JSExport("disposed")]
    public bool DisposedState => _disposed;

    // AsyncDisposableStack.prototype.disposeAsync ( )  — also installed as [@@asyncDispose].
    [JSExport("disposeAsync", Length = 0)]
    public JSValue DisposeAsync(in Arguments a)
    {
        if (_disposed)
            return System.Threading.Tasks.Task.CompletedTask.ToPromise();

        _disposed = true;
        return DisposableStackShared.DisposeResourcesAsync(_resources);
    }

    // AsyncDisposableStack.prototype.use ( value )
    [JSExport("use", Length = 1)]
    public JSValue Use(in Arguments a)
    {
        DisposableStackShared.RequireNotDisposed(_disposed);
        var value = a.Get1();

        if (!value.IsNullOrUndefined)
        {
            // Prefer @@asyncDispose; fall back to @@dispose only when @@asyncDispose is absent.
            var method = DisposableStackShared.GetDisposeMethod(value, (IJSSymbol)JSSymbol.asyncDispose);
            if (!method.IsFunction)
                method = DisposableStackShared.GetDisposeMethod(value, (IJSSymbol)JSSymbol.dispose);

            if (!method.IsFunction)
                throw JSEngine.NewTypeError("AsyncDisposableStack.prototype.use: value is not async-disposable");

            var resolved = method;
            _resources.Add(() => resolved.InvokeFunction(new Arguments(value)));
        }

        return value;
    }

    // AsyncDisposableStack.prototype.adopt ( value, onDisposeAsync )
    [JSExport("adopt", Length = 2)]
    public JSValue Adopt(in Arguments a)
    {
        DisposableStackShared.RequireNotDisposed(_disposed);
        var (value, onDispose) = a.Get2();

        if (!onDispose.IsFunction)
            throw JSEngine.NewTypeError("AsyncDisposableStack.prototype.adopt: onDisposeAsync is not a function");

        _resources.Add(() => onDispose.InvokeFunction(new Arguments(JSUndefined.Value, value)));
        return value;
    }

    // AsyncDisposableStack.prototype.defer ( onDisposeAsync )
    [JSExport("defer", Length = 1)]
    public JSValue Defer(in Arguments a)
    {
        DisposableStackShared.RequireNotDisposed(_disposed);
        var onDispose = a.Get1();

        if (!onDispose.IsFunction)
            throw JSEngine.NewTypeError("AsyncDisposableStack.prototype.defer: onDisposeAsync is not a function");

        _resources.Add(() => onDispose.InvokeFunction(new Arguments(JSUndefined.Value)));
        return JSUndefined.Value;
    }

    // AsyncDisposableStack.prototype.move ( )
    [JSExport("move", Length = 0)]
    public JSValue Move(in Arguments a)
    {
        DisposableStackShared.RequireNotDisposed(_disposed);

        var moved = new JSAsyncDisposableStackObject(GetCurrentPrototype() as JSObject)
        {
            _resources = _resources,
        };

        _resources = [];
        _disposed = true;
        return moved;
    }
}
