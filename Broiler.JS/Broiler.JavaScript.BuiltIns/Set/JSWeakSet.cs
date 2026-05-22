using Broiler.JavaScript.BuiltIns.Map;
using Broiler.JavaScript.BuiltIns.Array;
using Broiler.JavaScript.BuiltIns.Boolean;
using Broiler.JavaScript.BuiltIns.Iterator;
using Broiler.JavaScript.ExpressionCompiler;
using System;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Set;

[JSClassGenerator("WeakSet")]
public partial class JSWeakSet : JSObject
{
    private StringMap<WeakReference<WeakValue>> index;

    public JSWeakSet(in Arguments a) : base(JSEngine.NewTargetPrototype)
    {
        var iterable = a.Get1();
        if (iterable.IsNullOrUndefined)
            return;

        var adderTarget =
            (((JSEngine.Current as IJSExecutionContext)?.CurrentNewTarget as IJSFunction)?.Prototype as JSValue)
            ?? JSEngine.NewTargetPrototype
            ?? this;
        if (adderTarget[KeyStrings.GetOrCreate("add")] is not IJSFunction adder)
            throw JSEngine.NewTypeError("WeakSet instance 'add' property is not callable");

        var en = iterable.GetIterableEnumerator();
        while (en.MoveNext(out var item))
        {
            try
            {
                adder.InvokeFunction(new Arguments(this, item));
            }
            catch
            {
                JSIteratorObject.CloseIteratorIfPossible(en);
                throw;
            }
        }
    }

    [JSExport("add")]
    public JSValue Add(JSObject a)
    {
        HashedString key = a.ToUniqueID();
        lock (this)
        {
            if (!index.TryGetValue(key, out var w))
                index.Put(key) = new(new (key, a, Unregister));
        }

        return a;
    }

    private void Unregister(in HashedString key) => index.RemoveAt(key.Value);

    [JSExport("delete")]
    public JSValue Delete(in Arguments a)
    {
        var key = a.Get1().ToUniqueID();
        lock (this)
        {
            if (index.TryRemove(key, out var w))
            {
                if (w.TryGetTarget(out var target))
                    GC.SuppressFinalize(target);

                return JSBoolean.True;
            }
        }

        return JSBoolean.False;
    }

    [Prototype("has")]
    public JSValue Has(in Arguments a)
    {
        var key = a.Get1().ToUniqueID();
        lock (this)
        {
            if (index.TryGetValue(key, out var v))
            {
                if (v.TryGetTarget(out var target))
                    return JSBoolean.True;
            }
        }

        return JSUndefined.Value;
    }
}
