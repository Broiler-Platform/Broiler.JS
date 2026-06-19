using System.Collections.Concurrent;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Storage;
using Broiler.JavaScript.BuiltIns.Function;
using Broiler.JavaScript.BuiltIns.Symbol;

namespace Broiler.JavaScript.BuiltIns.RegExp;

internal sealed class JSRegExpStringIterator : JSObject
{
    // %RegExpStringIteratorPrototype% inherits from %IteratorPrototype%. That
    // intrinsic is realm-specific, so cache one prototype per realm (keyed by the
    // realm's %IteratorPrototype% identity) rather than a single process-global
    // object, mirroring how JSGenerator builds its iterator prototypes.
    private static readonly ConcurrentDictionary<string, JSObject> Prototypes = new();

    private readonly JSValue regexp;
    private readonly JSValue input;
    private readonly bool global;
    private readonly bool unicode;
    private bool done;

    public JSRegExpStringIterator(JSValue regexp, JSValue input, bool global, bool unicode)
    {
        BasePrototypeObject = GetPrototype();
        this.regexp = regexp;
        this.input = input;
        this.global = global;
        this.unicode = unicode;
    }

    private static JSObject GetIteratorPrototype()
        => ((JSEngine.Current as JSObject)?[KeyStrings.GetOrCreate("Iterator")] as JSFunction)?.prototype;

    private static JSObject GetPrototype()
    {
        var iteratorPrototype = GetIteratorPrototype();
        var key = iteratorPrototype == null ? string.Empty : iteratorPrototype.UniqueID.ToString();
        return Prototypes.GetOrAdd(key, _ => CreateIteratorPrototype(iteratorPrototype));
    }

    private static JSObject CreateIteratorPrototype(JSObject iteratorPrototype)
    {
        var prototype = new JSObject { BasePrototypeObject = iteratorPrototype };
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
            // §22.2.9.2.1 step 9.e: empty match → lastIndex = AdvanceStringIndex(S, …,
            // fullUnicode). With a `u`/`v` flag a surrogate pair is a single position, so
            // the iterator does not yield a spurious empty match inside an astral char.
            var s = input.ToString();
            var nextIndex = GetObservableLastIndex();
            var advanced = nextIndex + 1;
            if (unicode && advanced < s.Length
                && char.IsHighSurrogate(s[nextIndex]) && char.IsLowSurrogate(s[advanced]))
                advanced++;
            regexp[KeyStrings.lastIndex] = JSValue.CreateNumber(advanced);
        }

        return CreateIterResult(match, false);
    }

    private JSValue RegExpExec()
    {
        // RegExpExec: only a callable "exec" is invoked; any other value (undefined, null,
        // or a non-callable primitive) falls back to the built-in RegExpBuiltinExec.
        var exec = regexp[KeyStrings.GetOrCreate("exec")];
        if (exec.IsFunction)
        {
            var result = exec.InvokeFunction(new Arguments(regexp, input));
            if (!result.IsObject && !result.IsNull)
                throw JSEngine.NewTypeError("RegExp exec result must be an object or null");

            return result;
        }

        if (regexp is not JSRegExp regExp)
            throw JSEngine.NewTypeError("RegExp.prototype[Symbol.matchAll] called on incompatible receiver");

        return regExp.Exec(new Arguments(regexp, input));
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
