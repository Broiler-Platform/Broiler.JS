using System;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Storage;
using System.Runtime.CompilerServices;

namespace Broiler.JavaScript.BuiltIns.RegExp;

public partial class JSRegExp
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetObservableLastIndex()
    {
        var observableLastIndex = this[KeyStrings.lastIndex].DoubleValue;
        if (double.IsNaN(observableLastIndex) || observableLastIndex <= 0)
            return 0;

        if (observableLastIndex >= int.MaxValue)
            return int.MaxValue;

        return (int)observableLastIndex;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetObservableLastIndex(int value)
    {
        if (!SetValue(KeyStrings.lastIndex, JSValue.CreateNumber(value), this, true))
            return;

        lastIndex = value;
    }

    internal protected override bool SetValue(KeyString name, JSValue value, JSValue receiver, bool throwError = true)
    {
        if (name.Key == KeyStrings.lastIndex.Key
            && ReferenceEquals(receiver as JSObject ?? this, this)
            && GetInternalProperty(name, false).IsEmpty)
        {
            ref var ownProperties = ref GetOwnProperties();
            ownProperties.Put(name, value, JSPropertyAttributes.Value);
            return true;
        }

        return base.SetValue(name, value, receiver, throwError);
    }

    [JSExport("compile", Length = 2)]
    public JSValue Compile(in Arguments a)
    {
        var patternValue = a.Get1();
        var flagsValue = a.TryGetAt(1, out var second) ? second : JSValue.UndefinedValue;

        if (!ReferenceEquals(GetPrototypeOf(), GetCurrentPrototype()))
            throw JSEngine.NewTypeError("RegExp.prototype.compile called on incompatible receiver");

        string nextPattern;
        string nextFlags;

        if (patternValue is JSRegExp regExp)
        {
            if (!flagsValue.IsUndefined)
                throw JSEngine.NewTypeError("Cannot supply flags when constructing one RegExp from another");

            nextPattern = regExp.pattern;
            nextFlags = regExp.flags;
        }
        else
        {
            nextPattern = patternValue.IsUndefined ? string.Empty : patternValue.StringValue;
            nextFlags = flagsValue.IsUndefined ? string.Empty : flagsValue.StringValue;
        }

        pattern = nextPattern;
        (value, globalSearch, ignoreCase, multiline, hasIndices, sticky, unicode, unicodeSets, flags) = CreateRegex(nextPattern, nextFlags, out captureMap);
        SetObservableLastIndex(0);
        return this;
    }

    // NOTE: not [JSExport]. Per spec `lastIndex` is a per-instance own data property
    // (installed at construction), not a property of %RegExp.prototype%. Exporting it
    // here would install an accessor on the prototype whose getter casts `this` to a
    // JSRegExp and throws "Failed to convert this to JSRegExp" when read off the
    // prototype object itself (which is an ordinary object, not a RegExp instance).
    public int LastIndex
    {
        get => lastIndex; set => lastIndex = value;
    }

    // §22.2.6.16 RegExp.prototype.test is generic: it only requires `this` to be an
    // Object and then performs RegExpExec, which uses the receiver's `exec` property
    // (any callable) rather than assuming a real RegExp. A static prototype method so
    // the generated wrapper does not cast `this` to JSRegExp.
    [JSPrototypeMethod]
    [JSExport("test")]
    public static JSValue Test(in Arguments a)
    {
        if (a.This is not JSObject receiver)
            throw JSEngine.NewTypeError("RegExp.prototype.test called on a non-object");

        var s = a.Get1().StringValue;
        var match = RegExpExec(receiver, JSValue.CreateString(s));
        return match.IsNull ? JSValue.BooleanFalse : JSValue.BooleanTrue;
    }

    // §22.2.7.1 RegExpExec ( R, S ): use a callable `exec` property if present,
    // otherwise fall back to the builtin RegExpBuiltinExec (requires a real RegExp).
    private static JSValue RegExpExec(JSObject r, JSValue s)
    {
        var exec = r[KeyStrings.GetOrCreate("exec")];
        if (exec.IsFunction)
        {
            var result = exec.InvokeFunction(new Arguments(r, s));
            if (!result.IsObject && !result.IsNull)
                throw JSEngine.NewTypeError("RegExp exec method returned something other than an Object or null");

            return result;
        }

        if (r is JSRegExp regexp)
            return regexp.Exec(new Arguments(r, s));

        throw JSEngine.NewTypeError("Method called on incompatible receiver");
    }

    [JSExport("exec")]
    public JSValue Exec(in Arguments a)
    {
        var input = a.Get1().StringValue;

        // RegExpBuiltinExec reads `lastIndex` exactly once (via ToLength), even when
        // the regex is neither global nor sticky; the value is only consulted in the
        // global/sticky case (steps 8 and 12).
        var observableLastIndex = GetObservableLastIndex();
        var useLastIndex = globalSearch || sticky;

        // RegExpBuiltinExec step 12.a: for a global or sticky regex whose lastIndex
        // has advanced past the end of the subject, there is no remaining position to
        // search. Reset lastIndex to 0 and report no match instead of clamping the
        // start position back to the end (which would spuriously match an empty
        // pattern at the final index).
        if (useLastIndex && observableLastIndex > input.Length)
        {
            SetObservableLastIndex(0);
            return JSValue.NullValue;
        }

        // Perform the regular expression matching.
        var startPosition = useLastIndex ? observableLastIndex : 0;
        var match = value.Match(input, startPosition);

        if (sticky && (!match.Success || match.Index != startPosition))
            match = System.Text.RegularExpressions.Match.Empty;

        // Return null if no match was found.
        if (match.Success == false)
        {
            // Reset the lastIndex property.
            if (globalSearch || sticky)
                SetObservableLastIndex(0);

            return JSValue.NullValue;
        }

        if (globalSearch || sticky)
            SetObservableLastIndex(match.Index + match.Length);

        var groups = match.Groups;
        // When the pattern has named groups every capture was renamed to a
        // synthetic, source-ordered name (see RewriteCaptureGroups), so .NET now
        // numbers them 1..n in ECMAScript order and integer indexing is correct.
        // The captureMap supplies that count and the original-name mapping.
        var c = captureMap != null ? captureMap.Count + 1 : groups.Count;
        var result = JSValue.CreateArray((uint)c);

        for (int i = 0; i < c; i++)
        {
            var group = groups[i];
            result[(uint)i] = group.Success ? JSValue.CreateString(group.Value) : JSUndefined.Value;
        }

        result[KeyStrings.index] = JSValue.CreateNumber(match.Index);
        // RegExpBuiltinExec stores the *coerced* (ToString) subject string in the
        // result's `input` property, not the raw argument — e.g. exec(undefined)
        // yields input === "undefined".
        result[KeyStrings.input] = JSValue.CreateString(input);

        var groupsKey = KeyStrings.GetOrCreate("groups");
        var indicesKey = KeyStrings.GetOrCreate("indices");
        JSObject indicesGroups = null;
        if (hasIndices)
        {
            var indices = JSValue.CreateArray((uint)c);
            var indicesObject = (JSObject)indices;
            for (int i = 0; i < c; i++)
            {
                var group = groups[i];
                if (!group.Success)
                {
                    indices[(uint)i] = JSUndefined.Value;
                }
                else
                {
                    var range = JSValue.CreateArray(2);
                    range[0] = JSValue.CreateNumber(group.Index);
                    range[1] = JSValue.CreateNumber(group.Index + group.Length);
                    indices[(uint)i] = range;
                }
            }

            indicesGroups = new JSObject();
            indicesGroups.SetPrototypeOf(JSValue.NullValue);
            indicesObject.FastAddValue(groupsKey, JSUndefined.Value, JSPropertyAttributes.EnumerableConfigurableValue);
            ((JSObject)result).FastAddValue(indicesKey, indices, JSPropertyAttributes.EnumerableConfigurableValue);
        }

        // Populate named groups (§2.7 — including ES2025 duplicate named groups).
        // The groups object is ObjectCreate(null) and its properties are installed
        // via CreateDataProperty (writable/enumerable/configurable), never [[Set]],
        // so inherited setters / a "__proto__" group name do not interfere
        // (RegExpBuiltinExec steps 24-28). Distinct names appear in source order,
        // and a name shared by several alternatives resolves to whichever group
        // participated (at most one can, since duplicates are mutually exclusive).
        if (captureMap != null && captureMap.NamedGroups.Count > 0)
        {
            var namedGroups = new JSObject();
            namedGroups.SetPrototypeOf(JSValue.NullValue);

            foreach (var (name, indices) in captureMap.NamedGroups)
            {
                var nameKey = KeyStrings.GetOrCreate(name);

                System.Text.RegularExpressions.Group matched = null;
                foreach (var idx in indices)
                {
                    var g = groups[idx];
                    if (g.Success)
                    {
                        matched = g;
                        break;
                    }
                }

                namedGroups.FastAddValue(nameKey, matched != null
                    ? JSValue.CreateString(matched.Value)
                    : JSUndefined.Value, JSPropertyAttributes.EnumerableConfigurableValue);

                if (hasIndices)
                {
                    if (matched != null)
                    {
                        var range = JSValue.CreateArray(2);
                        range[0] = JSValue.CreateNumber(matched.Index);
                        range[1] = JSValue.CreateNumber(matched.Index + matched.Length);
                        indicesGroups.FastAddValue(nameKey, range, JSPropertyAttributes.EnumerableConfigurableValue);
                    }
                    else
                    {
                        indicesGroups.FastAddValue(nameKey, JSUndefined.Value, JSPropertyAttributes.EnumerableConfigurableValue);
                    }
                }
            }

            ((JSObject)result).FastAddValue(groupsKey, namedGroups, JSPropertyAttributes.EnumerableConfigurableValue);
            if (hasIndices)
                ((JSObject)result[indicesKey]).FastAddValue(groupsKey, indicesGroups, JSPropertyAttributes.EnumerableConfigurableValue);
            return result;
        }

        ((JSObject)result).FastAddValue(groupsKey, JSUndefined.Value, JSPropertyAttributes.EnumerableConfigurableValue);

        return result;
    }

    [JSPrototypeMethod]
    [JSExport("toString")]
    public static JSValue ToString(in Arguments a)
    {
        // §22.2.6.13 RegExp.prototype.toString is generic: it requires only that the receiver be an
        // Object and builds the result from its "source" and "flags" properties (each coerced with
        // ToString), so it also works on RegExp.prototype itself and on non-RegExp objects.
        if (a.This is not JSObject receiver)
            throw JSEngine.NewTypeError("RegExp.prototype.toString called on a non-object receiver");

        var pattern = receiver[KeyStrings.GetOrCreate("source")].StringValue;
        var flags = receiver[KeyStrings.GetOrCreate("flags")].StringValue;
        return JSValue.CreateString($"/{pattern}/{flags}");
    }
}
