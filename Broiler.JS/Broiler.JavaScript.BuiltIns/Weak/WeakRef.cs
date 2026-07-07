using Broiler.JavaScript.BuiltIns.Symbol;
using Broiler.JavaScript.ExpressionCompiler;
using System;
using System.Collections.Generic;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.BuiltIns.Function;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Weak;

[JSClassGenerator("FinalizationRegistry")]
public partial class JSFinalizationRegistry : JSObject
{
    private readonly JSSymbol finalizationSymbol = new("finalization");
    // [[Cells]] entries that carry a non-undefined [[UnregisterToken]]. The token may be
    // a Symbol, which cannot hold a hidden property, so unregister-by-token is tracked in
    // an explicit list keyed by SameValue rather than via a property on the token object
    // (test262: FinalizationRegistry/prototype/unregister/unregister-symbol-token).
    private readonly List<(JSValue Token, WeakObject Ref)> tokenCells = [];
    private readonly JSFunction finalizer;

    [JSExport(Length = 1)]
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
        _ = holdings;
    }

    [JSExport(Length = 1)]
    public JSValue Unregister(in Arguments a)
    {
        if (!CanBeHeldWeakly(a[0]))
            throw JSEngine.NewTypeError("Argument must be an object or symbol");

        return Unregister(a[0]) ? BooleanTrue : BooleanFalse;
    }

    [JSExport(Length = 2)]
    public JSValue Register(in Arguments a)
    {
        var target = a[0];
        if (!CanBeHeldWeakly(target))
            throw JSEngine.NewTypeError("Argument must be an object or symbol");

        var holdings = a[1] ?? JSUndefined.Value;
        if (target.Is(holdings).BooleanValue)
            throw JSEngine.NewTypeError("target and holdings must not be the same");

        var unregisterToken = a[2] ?? JSUndefined.Value;
        if (!unregisterToken.IsUndefined && !CanBeHeldWeakly(unregisterToken))
            throw JSEngine.NewTypeError("Argument must be an object or symbol");

        Register(target, holdings, unregisterToken);
        return JSUndefined.Value;
    }

    // AO CanBeHeldWeakly: an object, or a non-registered (non-Symbol.for) symbol.
    // A registered symbol cannot be held weakly, so delegate to the shared helper
    // rather than accepting every symbol.
    private static bool CanBeHeldWeakly(JSValue value) => JSSymbol.CanBeHeldWeakly(value);

    private void Register(JSValue target, JSValue holdings, JSValue unregisterToken)
    {
        var weakRef = new WeakObject(this, holdings);

        if (target is JSObject targetObject)
            targetObject[(IJSSymbol)finalizationSymbol] = weakRef;

        if (!unregisterToken.IsUndefined)
            tokenCells.Add((unregisterToken, weakRef));
    }

    private bool Unregister(JSValue token)
    {
        // §26.2.3.4: remove every cell whose [[UnregisterToken]] is SameValue to the
        // argument; return whether any cell was removed. Works for object and symbol
        // tokens alike.
        var removed = false;
        for (var i = tokenCells.Count - 1; i >= 0; i--)
        {
            if (!tokenCells[i].Token.Is(token).BooleanValue)
                continue;

            GC.SuppressFinalize(tokenCells[i].Ref);
            tokenCells.RemoveAt(i);
            removed = true;
        }

        return removed;
    }
}

[JSClassGenerator("WeakRef")]
public partial class JSWeakRef : JSObject
{
    internal WeakReference<JSValue> weak;
    public JSWeakRef(JSValue value) : this() => weak = new WeakReference<JSValue>(value);
    [JSExport(Length = 1)]
    public JSWeakRef(in Arguments a) : base(JSEngine.NewTargetPrototype)
    {
        // §26.1.1.1: the target must be able to be held weakly (an object or a
        // non-registered symbol); anything else (including a missing argument)
        // is a TypeError.
        var target = a[0] ?? JSUndefined.Value;
        if (!JSSymbol.CanBeHeldWeakly(target))
            throw JSEngine.NewTypeError("WeakRef: target must be an object or an unregistered symbol");

        weak = new WeakReference<JSValue>(target);
    }

    [JSExport]
    public JSValue Deref(in Arguments a)
    {
        if (weak.TryGetTarget(out var v))
            return v;

        return JSUndefined.Value;
    }
}
