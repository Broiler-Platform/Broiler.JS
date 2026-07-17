using System.Runtime.CompilerServices;
using Broiler.JavaScript.BuiltIns.Boolean;
using Broiler.JavaScript.BuiltIns.Iterator;
using Broiler.JavaScript.BuiltIns.Symbol;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.BuiltIns.Set;

[JSClassGenerator("WeakSet")]
public partial class JSWeakSet : JSObject
{
    private static readonly object Present = new();
    private readonly ConditionalWeakTable<JSValue, object> entries = [];

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
    public JSValue Add(in Arguments a)
    {
        var value = a.Get1();
        if (!JSSymbol.CanBeHeldWeakly(value))
            throw JSEngine.NewTypeError("WeakSet value must be an object or a non-registered symbol");

        entries.GetValue(value, static _ => Present);
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
}
