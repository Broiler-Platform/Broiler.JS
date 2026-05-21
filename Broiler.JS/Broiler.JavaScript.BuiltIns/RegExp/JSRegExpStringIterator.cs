using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Storage;
using Broiler.JavaScript.BuiltIns.Symbol;

namespace Broiler.JavaScript.BuiltIns.RegExp;

internal sealed class JSRegExpStringIterator : JSObject
{
    private static readonly JSObject Prototype = CreateIteratorPrototype();

    private readonly JSValue regexp;
    private readonly JSValue input;
    private readonly bool global;
    private readonly bool unicode;
    private bool done;

    public JSRegExpStringIterator(JSValue regexp, JSValue input, bool global, bool unicode)
    {
        BasePrototypeObject = Prototype;
        this.regexp = regexp;
        this.input = input;
        this.global = global;
        this.unicode = unicode;
    }

    private static JSObject CreateIteratorPrototype()
    {
        var prototype = new JSObject();
        prototype.FastAddValue(KeyStrings.next, JSValue.CreateFunction(Next, "next", null, 0, false), JSPropertyAttributes.ConfigurableValue);
        prototype.FastAddValue((IJSSymbol)JSSymbol.iterator, JSValue.CreateFunction(static (in Arguments a) => a.This, "[Symbol.iterator]", null, 0, false), JSPropertyAttributes.ConfigurableValue);
        return prototype;
    }

    private static JSValue Next(in Arguments a)
    {
        if (a.This is not JSRegExpStringIterator iterator)
            throw JSEngine.NewTypeError("RegExp String Iterator.prototype.next called on incompatible receiver");

        return iterator.Next();
    }

    private JSValue Next()
    {
        if (done)
            return CreateIterResult(JSUndefined.Value, true);

        var match = RegExpExec();
        if (match.IsNull)
        {
            done = true;
            return CreateIterResult(JSUndefined.Value, true);
        }

        if (!global)
        {
            done = true;
            return CreateIterResult(match, false);
        }

        var matchValue = JSUndefined.Value;
        var ownZero = match.GetOwnPropertyDescriptor(JSValue.CreateString("0"));
        if (!ownZero.IsUndefined)
        {
            var getter = ownZero[KeyStrings.get];
            matchValue = getter.IsUndefined
                ? ownZero[KeyStrings.value]
                : getter.InvokeFunction(new Arguments(match));
        }

        if (matchValue.IsUndefined)
            matchValue = match[0];
        if (matchValue.IsUndefined)
            matchValue = match[KeyStrings.GetOrCreate("0")];

        var matchString = matchValue.ToString();
        if (matchString.Length == 0)
        {
            var nextIndex = GetObservableLastIndex();
            regexp[KeyStrings.lastIndex] = JSValue.CreateNumber(nextIndex + 1);
        }

        return CreateIterResult(match, false);
    }

    private JSValue RegExpExec()
    {
        var exec = regexp[KeyStrings.GetOrCreate("exec")];
        if (exec.IsUndefined)
        {
            if (regexp is not JSRegExp regExp)
                throw JSEngine.NewTypeError("RegExp.prototype[Symbol.matchAll] called on incompatible receiver");

            return regExp.Exec(new Arguments(regexp, input));
        }

        if (!exec.IsFunction)
            throw JSEngine.NewTypeError("RegExp exec property is not callable");

        var result = exec.InvokeFunction(new Arguments(regexp, input));
        if (!result.IsObject && !result.IsNull)
            throw JSEngine.NewTypeError("RegExp exec result must be an object or null");

        return result;
    }

    private int GetObservableLastIndex()
    {
        var observableLastIndex = regexp[KeyStrings.lastIndex].DoubleValue;
        if (double.IsNaN(observableLastIndex) || observableLastIndex <= 0)
            return 0;

        if (observableLastIndex >= int.MaxValue)
            return int.MaxValue;

        return (int)observableLastIndex;
    }

    private static JSObject CreateIterResult(JSValue value, bool done)
    {
        var result = NewWithProperties();
        result.FastAddValue(KeyStrings.value, value, JSPropertyAttributes.EnumerableConfigurableValue);
        result.FastAddValue(KeyStrings.done, done ? JSValue.BooleanTrue : JSValue.BooleanFalse, JSPropertyAttributes.EnumerableConfigurableValue);
        return result;
    }
}
