using System.Runtime.CompilerServices;
using Broiler.JavaScript.BuiltIns.Boolean;
using Broiler.JavaScript.BuiltIns.Iterator;
using Broiler.JavaScript.BuiltIns.Symbol;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.Engine.Extensions;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.BuiltIns.Map;

internal sealed class WeakMapValueBox(JSValue value)
{
    internal JSValue Value = value;
}

[JSClassGenerator("WeakMap")]
public partial class JSWeakMap : JSObject
{
    // ConditionalWeakTable is already a thread-safe, reference-identity ephemeron table.
    // A value -> key cycle therefore does not keep an otherwise unreachable key alive.
    private readonly ConditionalWeakTable<JSValue, WeakMapValueBox> entries = [];

    public JSWeakMap(in Arguments a) : base(JSEngine.NewTargetPrototype)
    {
        var iterable = a.Get1();
        if (iterable.IsNullOrUndefined)
            return;

        var adderTarget =
            (((JSEngine.Current as IJSExecutionContext)?.CurrentNewTarget as IJSFunction)?.Prototype as JSValue)
            ?? JSEngine.NewTargetPrototype
            ?? this;
        if (adderTarget[KeyStrings.set] is not IJSFunction adder)
            throw JSEngine.NewTypeError("WeakMap instance 'set' property is not callable");

        var en = iterable.GetIterableEnumerator();
        while (en.MoveNext(out var item))
        {
            try
            {
                if (item is not JSObject entry)
                    throw JSEngine.NewTypeError(NotEntry(item));

                adder.InvokeFunction(new Arguments(this, entry[0], entry[1]));
            }
            catch
            {
                JSIteratorObject.CloseIteratorIfPossible(en);
                throw;
            }
        }
    }

    [JSExport("set")]
    public JSValue Set(in Arguments a)
    {
        var (key, value) = a.Get2();
        EnsureWeakKey(key);
        entries.AddOrUpdate(key, new WeakMapValueBox(value));
        return this;
    }

    [JSExport("delete")]
    public JSValue Delete(in Arguments a)
    {
        var key = a.Get1();
        return JSSymbol.CanBeHeldWeakly(key) && entries.Remove(key)
            ? JSBoolean.True
            : JSBoolean.False;
    }

    [JSExport("has")]
    public JSValue Has(in Arguments a)
    {
        var key = a.Get1();
        return JSSymbol.CanBeHeldWeakly(key) && entries.TryGetValue(key, out _)
            ? JSBoolean.True
            : JSBoolean.False;
    }

    [JSExport("get")]
    public JSValue Get(in Arguments a)
    {
        var key = a.Get1();
        if (!JSSymbol.CanBeHeldWeakly(key))
            return JSUndefined.Value;

        return entries.TryGetValue(key, out var box) ? box.Value : JSUndefined.Value;
    }

    [JSExport("getOrInsert", Length = 2, Feature = (int)JavaScriptFeatureFlags.MapUpsert)]
    public JSValue GetOrInsert(in Arguments a)
    {
        var (key, defaultValue) = a.Get2();
        EnsureWeakKey(key);

        if (entries.TryGetValue(key, out var box))
            return box.Value;

        entries.AddOrUpdate(key, new WeakMapValueBox(defaultValue));
        return defaultValue;
    }

    [JSExport("getOrInsertComputed", Feature = (int)JavaScriptFeatureFlags.MapUpsert)]
    public JSValue GetOrInsertComputed(in Arguments a)
    {
        var (key, callbackfn) = a.Get2();
        EnsureWeakKey(key);
        if (!callbackfn.IsFunction)
            throw JSEngine.NewTypeError("getOrInsertComputed requires a callback function");

        if (entries.TryGetValue(key, out var box))
            return box.Value;

        var value = callbackfn.Call(JSUndefined.Value, key);
        entries.AddOrUpdate(key, new WeakMapValueBox(value));
        return value;
    }

    private static void EnsureWeakKey(JSValue key)
    {
        if (!JSSymbol.CanBeHeldWeakly(key))
            throw JSEngine.NewTypeError("WeakMap key must be an object or a non-registered symbol");
    }
}
