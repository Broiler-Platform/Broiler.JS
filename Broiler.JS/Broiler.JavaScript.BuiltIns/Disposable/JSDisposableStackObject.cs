using System;
using System.Collections.Generic;
using Broiler.JavaScript.BuiltIns.Symbol;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.Engine.Extensions;

namespace Broiler.JavaScript.BuiltIns.Disposable;

// ES2026 Explicit Resource Management — the user-facing `DisposableStack` built-in
// (§ DisposableStack Objects). This is distinct from the internal IJSDisposableStack
// helper that backs `using` declarations: this is the constructor/prototype exposed on
// the global object. A DisposableStack owns an ordered list of disposal "thunks"; the
// stack is disposed LIFO, and errors thrown by disposers are aggregated into a
// SuppressedError chain.
[JSClassGenerator("DisposableStack")]
public partial class JSDisposableStackObject : JSObject
{
    private bool _disposed;
    // Disposal thunks in insertion order; disposed last-in-first-out (iterate in reverse).
    // A thunk returns the dispose call's result (unused for the synchronous stack).
    private List<Func<JSValue>> _resources = new();

    [JSExport(Length = 0)]
    public JSDisposableStackObject(in Arguments a) : base(DisposableStackShared.ResolveConstructorPrototype("DisposableStack"))
    {
    }

    // get DisposableStack.prototype.disposed
    [JSExport("disposed")]
    public bool DisposedState => _disposed;

    // DisposableStack.prototype.dispose ( )  — also installed as [@@dispose].
    [JSExport("dispose", Length = 0)]
    public JSValue Dispose(in Arguments a)
    {
        if (_disposed)
            return JSUndefined.Value;

        _disposed = true;
        DisposableStackShared.DisposeResources(_resources);
        return JSUndefined.Value;
    }

    // DisposableStack.prototype.use ( value )
    [JSExport("use", Length = 1)]
    public JSValue Use(in Arguments a)
    {
        DisposableStackShared.RequireNotDisposed(_disposed);
        var value = a.Get1();

        if (!value.IsNullOrUndefined)
        {
            var method = DisposableStackShared.GetDisposeMethod(value, (IJSSymbol)JSSymbol.dispose);
            if (!method.IsFunction)
                throw JSEngine.NewTypeError("DisposableStack.prototype.use: value is not disposable (no @@dispose method)");

            _resources.Add(() => method.InvokeFunction(new Arguments(value)));
        }

        return value;
    }

    // DisposableStack.prototype.adopt ( value, onDispose )
    [JSExport("adopt", Length = 2)]
    public JSValue Adopt(in Arguments a)
    {
        DisposableStackShared.RequireNotDisposed(_disposed);
        var (value, onDispose) = a.Get2();

        if (!onDispose.IsFunction)
            throw JSEngine.NewTypeError("DisposableStack.prototype.adopt: onDispose is not a function");

        // The disposer calls onDispose(value) with undefined `this`.
        _resources.Add(() => onDispose.InvokeFunction(new Arguments(JSUndefined.Value, value)));
        return value;
    }

    // DisposableStack.prototype.defer ( onDispose )
    [JSExport("defer", Length = 1)]
    public JSValue Defer(in Arguments a)
    {
        DisposableStackShared.RequireNotDisposed(_disposed);
        var onDispose = a.Get1();

        if (!onDispose.IsFunction)
            throw JSEngine.NewTypeError("DisposableStack.prototype.defer: onDispose is not a function");

        _resources.Add(() => onDispose.InvokeFunction(new Arguments(JSUndefined.Value)));
        return JSUndefined.Value;
    }

    // DisposableStack.prototype.move ( )
    [JSExport("move", Length = 0)]
    public JSValue Move(in Arguments a)
    {
        DisposableStackShared.RequireNotDisposed(_disposed);

        var moved = new JSDisposableStackObject(GetCurrentPrototype() as JSObject)
        {
            _resources = _resources,
        };

        // This stack relinquishes its resources and is marked disposed (without running them).
        _resources = new List<Func<JSValue>>();
        _disposed = true;
        return moved;
    }
}
