using Broiler.JavaScript.BuiltIns.Symbol;
using Broiler.JavaScript.ExpressionCompiler;
using System;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.BuiltIns.Function;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Weak;

[JSClassGenerator("FinalizationRegistry")]
public partial class JSFinalizationRegistry : JSObject
{
    private readonly JSSymbol finalizationSymbol = new("finalization");
    private readonly JSSymbol finalizationToken = new("finalizationToken");

    private readonly JSFunction finalizer;

    public JSFinalizationRegistry(in Arguments a) : base(JSEngine.NewTargetPrototype)
    {
        if (a[0] is not JSFunction fx)
            throw JSEngine.NewTypeError($"Argument is not a function");

        finalizer = fx;
    }

    internal class WeakObject(JSFinalizationRegistry registry, JSValue holdings) : JSObject
    {
        ~WeakObject()
        {
            registry.FinalizeReference(holdings);
        }
    }

    private void FinalizeReference(JSValue holdings)
    {
        finalizer.InvokeFunction(new Arguments(this, holdings));
    }

    [JSExport]
    public JSValue Unregister(in Arguments a)
    {
        if (!CanBeHeldWeakly(a[0]))
            throw JSEngine.NewTypeError($"Argument is not an object");

        return Unregister(a[0]) ? JSValue.BooleanTrue : JSValue.BooleanFalse;
    }

    [JSExport]
    public JSValue Register(in Arguments a)
    {
        var target = a[0];
        if (!CanBeHeldWeakly(target))
            throw JSEngine.NewTypeError($"Argument is not an object");

        var holdings = a[1] ?? JSUndefined.Value;
        if (target.Is(holdings).BooleanValue)
            throw JSEngine.NewTypeError("target and holdings must not be the same");

        var unregisterToken = a[2] ?? JSUndefined.Value;
        if (!unregisterToken.IsUndefined && !CanBeHeldWeakly(unregisterToken))
            throw JSEngine.NewTypeError($"Argument is not an object");

        Register(target, holdings, unregisterToken);
        return JSUndefined.Value;
    }

    private static bool CanBeHeldWeakly(JSValue value) => value is JSObject || value.IsSymbol;

    private void Register(JSValue target, JSValue holdings, JSValue unregisterToken)
    {
        var weakRef = new WeakObject(this, holdings);

        if (target is JSObject targetObject)
            targetObject[(IJSSymbol)finalizationSymbol] = weakRef;

        if (unregisterToken is JSObject unregisterTokenObject)
            unregisterTokenObject[(IJSSymbol)finalizationToken] = weakRef;
    }

    private bool Unregister(JSValue token)
    {
        if (token is not JSObject tokenObject)
            return false;

        var weakRef = tokenObject[(IJSSymbol)finalizationToken];
        if (weakRef.IsUndefined)
            return false;

        tokenObject.Delete((IJSSymbol)finalizationToken);
        GC.SuppressFinalize(weakRef);
        return true;
    }
}

[JSClassGenerator("WeakRef")]
public partial class JSWeakRef : JSObject
{
    internal WeakReference<JSValue> weak;
    public JSWeakRef(JSValue value) : this() => weak = new WeakReference<JSValue>(value);
    public JSWeakRef(in Arguments a) : base(JSEngine.NewTargetPrototype) => weak = new WeakReference<JSValue>(a[0] ?? throw new JSException($"argument is missing"));

    [JSExport]
    public JSValue Deref(in Arguments a)
    {
        if (weak.TryGetTarget(out var v))
            return v;

        return JSUndefined.Value;
    }
}
