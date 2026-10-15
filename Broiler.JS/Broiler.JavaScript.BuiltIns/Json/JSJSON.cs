using Broiler.ECMAScript.Numbers;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.BuiltIns.Boolean;
using Broiler.JavaScript.BuiltIns.Null;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Core;
using System.Collections.Generic;
using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Runtime.CompilerServices;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.BuiltIns.BigInt;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.BuiltIns.Function;
using Broiler.JavaScript.BuiltIns.Proxy;
using Broiler.JavaScript.BuiltIns.Symbol;
using Broiler.ECMAScript.Text;

// Maps a holder object to the parse-time source text and originally-parsed value of
// each of its primitive members, so the reviver's `context.source` can be supplied only
// while a slot still holds the value JSON.parse produced for it (see InternalizeJsonProperty).
using SourceMap = System.Collections.Generic.Dictionary<
    Broiler.JavaScript.Runtime.JSObject,
    System.Collections.Generic.Dictionary<string, (string source, Broiler.JavaScript.Runtime.JSValue value)>>;

namespace Broiler.JavaScript.BuiltIns.Json;

public delegate JSValue JsonParserReceiver((JSObject holder, string key, JSValue value) property);

/// <summary>
/// Delegate for reviver with source text access (ES2026 §4.7).
/// </summary>
public delegate JSValue JsonParserReceiverWithSource((JSObject holder, string key, JSValue value, string source) property);

[JSClassGenerator("JSON"), JSInternalObject]
public partial class JSJSON : JSObject
{
    private sealed class JSObjectReferenceComparer : IEqualityComparer<JSObject>
    {
        internal static readonly JSObjectReferenceComparer Instance = new();

        public bool Equals(JSObject x, JSObject y) => ReferenceEquals(x, y);

        public int GetHashCode(JSObject obj) => RuntimeHelpers.GetHashCode(obj);
    }

    private static JSValue ToNumberPrimitive(JSValue value)
    {
        if (value is not JSObject @object)
            return value;

        var toPrimitive = @object[(IJSSymbol)JSSymbol.toPrimitive];
        if (!toPrimitive.IsUndefined && !toPrimitive.IsNull)
        {
            var primitive = toPrimitive.InvokeFunction(new Arguments(@object, JSConstants.Number));
            if (primitive.IsObject)
                throw JSEngine.NewTypeError("Cannot convert object to primitive value");

            return primitive;
        }

        if (@object[KeyStrings.valueOf] is IJSFunction valueOf)
        {
            var primitive = valueOf.InvokeFunction(new Arguments(@object));
            if (!primitive.IsObject)
                return primitive;
        }

        if (@object[KeyStrings.toString] is IJSFunction toString)
        {
            var primitive = toString.InvokeFunction(new Arguments(@object));
            if (!primitive.IsObject)
                return primitive;
        }

        throw JSEngine.NewTypeError("Cannot convert object to primitive value");
    }

    // LengthOfArrayLike's ToLength (§7.1.20): this engine's ToNumber, then the shared library's
    // IntegerConversions.ToLength (SJL-4.5).
    private static long LengthOf(JSValue value)
        => value == null || value.IsUndefined ? 0 : IntegerConversions.ToLength(ToNumberPrimitive(value).DoubleValue);

    private static long GetArrayLength(JSObject valueObject)
    {
        // A trapless array proxy forwards "length" straight to its target, but that target
        // may itself be a proxy whose own get trap defines the length — so recurse rather
        // than read the innermost array's raw .Length (which would ignore the inner trap).
        if (valueObject is JSProxy proxy && proxy.IsArray && !proxy.HasTrap(KeyStrings.get))
            return GetArrayLength(proxy.Target);

        return LengthOf(valueObject[KeyStrings.length]);
    }

    // Builds the PropertyList from an array replacer per JSON.stringify step 5.b.
    // Only String and Number values (and their wrapper objects) become keys;
    // everything else (undefined, holes, booleans, etc.) is ignored. Duplicate
    // keys are appended only once, preserving first-seen order.
    private static List<string> BuildPropertyList(JSObject replacerArray)
    {
        var list = new List<string>();
        var seen = new HashSet<string>();
        var length = GetArrayLength(replacerArray);

        for (uint index = 0; index < length; index++)
        {
            var v = replacerArray[index];

            string item = null;
            if (v is JSString)
                item = v.ToString();
            else if (v is JSNumber)
                item = v.ToString();
            else if (v is JSPrimitiveObject wrapper && (wrapper.value.IsString || wrapper.value is JSNumber))
            {
                // The spec sets item to ToString(v): ToString(ToPrimitive(v, String)), which
                // runs the wrapper's own toString (then valueOf) through the prototype chain
                // — so a user-redefined Number/String.prototype.toString is observed. Reading
                // the [[NumberData]]/[[StringData]] slot directly would skip that override.
                item = CoerceJsonWrapperToPrimitive(wrapper, preferString: true).StringValue;
            }

            if (item != null && seen.Add(item))
                list.Add(item);
        }

        return list;
    }

    private static void StringifyArray(
        TextWriter sb,
        JSObject array,
        Func<(JSValue, JSValue, JSValue), JSValue> replacer,
        List<string> propertyList,
        IndentedTextWriter indent,
        HashSet<JSObject> stack)
    {
        if (!stack.Add(array))
            throw JSEngine.NewTypeError("Converting circular structure to JSON");

        try
        {
            sb.Write('[');
            if (indent != null)
                indent.Indent++;

            var length = GetArrayLength(array);
            bool wroteElement = false;
            for (uint index = 0; index < length; index++)
            {
                wroteElement = true;
                if (index > 0)
                    sb.Write(',');

                if (indent != null)
                    sb.WriteLine();

                var jsValue = ToJson(array[index], CreateString(index.ToString()));
                // A PropertyList never filters array elements; only a function
                // replacer is consulted here.
                if (replacer != null)
                    jsValue = replacer((array, CreateString(index.ToString()), jsValue));

                // SerializeJSONArray: an element whose serialization is undefined — an
                // undefined / callable / Symbol value — is rendered as the literal "null".
                if (jsValue.IsUndefined || jsValue is JSFunction || jsValue is JSSymbol)
                    jsValue = JSNull.Value;

                Stringify(sb, jsValue, replacer, propertyList, indent, stack);
            }

            if (indent != null)
            {
                // SerializeJSONArray: an empty array collapses to "[]" with no interior newline.
                if (wroteElement)
                    sb.WriteLine();
                indent.Indent--;
            }

            sb.Write(']');
        }
        finally
        {
            stack.Remove(array);
        }
    }

    private static List<string> EnumerableOwnPropertyNames(JSObject valueObject)
    {
        List<string> propertyKeys = [];
        var properties = valueObject.GetAllKeys(showEnumerableOnly: true, inherited: false);
        while (properties.MoveNext(out var hasValue, out var propertyKey, out var _))
        {
            if (!hasValue || propertyKey.IsSymbol)
                continue;

            propertyKeys.Add(propertyKey.ToString());
        }

        return propertyKeys;
    }

    // Per InternalizeJSONProperty, revived values are stored with CreateDataProperty
    // (7.3.5) whose boolean result is discarded: a failed [[DefineOwnProperty]]
    // (e.g. the target index was made non-configurable by the reviver) is silently
    // ignored rather than throwing a TypeError.
    private static void CreateDataProperty(JSObject target, JSValue key, JSValue value)
    {
        var descriptor = new JSObject();
        descriptor.FastAddValue(KeyStrings.value, value, JSPropertyAttributes.EnumerableConfigurableValue);
        descriptor.FastAddValue(KeyStrings.writable, JSBoolean.True, JSPropertyAttributes.EnumerableConfigurableValue);
        descriptor.FastAddValue(KeyStrings.enumerable, JSBoolean.True, JSPropertyAttributes.EnumerableConfigurableValue);
        descriptor.FastAddValue(KeyStrings.configurable, JSBoolean.True, JSPropertyAttributes.EnumerableConfigurableValue);
        target.DefineProperty(key, descriptor);
    }

    private static void RecordSource(
        SourceMap sourceMap,
        JSObject holder,
        string key,
        string source,
        JSValue value)
    {
        if (source == null)
            return;

        if (!sourceMap.TryGetValue(holder, out var holderSources))
        {
            holderSources = [];
            sourceMap[holder] = holderSources;
        }

        holderSources[key] = (source, value);
    }

    // The source text is exposed to the reviver only when the slot still holds the exact
    // value JSON.parse produced for it. A reviver can forward-modify a not-yet-visited
    // sibling (e.g. `this.q = 5` while visiting "p"); that replaces the parsed value, so the
    // recorded source no longer describes it and `context.source` must be undefined.
    private static bool TryGetSource(
        SourceMap sourceMap,
        JSObject holder,
        string key,
        JSValue currentValue,
        out string source)
    {
        if (sourceMap.TryGetValue(holder, out var holderSources)
            && holderSources.TryGetValue(key, out var recorded)
            && ReferenceEquals(currentValue, recorded.value))
        {
            source = recorded.source;
            return true;
        }

        source = null;
        return false;
    }

    private static bool IsPrimitiveJsonValue(JSValue value)
        => value is JSNumber || value is JSString || value == JSBoolean.True || value == JSBoolean.False || value == JSNull.Value;

    private static JSValue InternalizeJsonProperty(
        JSObject holder,
        string key,
        JSFunction reviver,
        SourceMap sourceMap,
        string rootSource)
    {
        if (key.Length > 0)
        {
            var propertyKey = CreateString(key).ToKey(false);
            if (propertyKey.Type == KeyType.UInt)
                return InternalizeJsonProperty(holder, propertyKey.Index, reviver, sourceMap);
        }

        var value = holder[key];
        if (value is JSObject valueObject)
        {
            if (valueObject.IsArray)
            {
                var length = GetArrayLength(valueObject);
                for (uint index = 0; index < length; index++)
                {
                    var revived = InternalizeJsonProperty(valueObject, index, reviver, sourceMap);
                    if (revived.IsUndefined)
                        valueObject.Delete(index);
                    else
                        CreateDataProperty(valueObject, CreateNumber(index), revived);
                }
            }
            else
            {
                foreach (var propertyKey in EnumerableOwnPropertyNames(valueObject))
                {
                    var revived = InternalizeJsonProperty(valueObject, propertyKey, reviver, sourceMap, null);
                    if (revived.IsUndefined)
                        valueObject.Delete(propertyKey);
                    else
                        CreateDataProperty(valueObject, CreateString(propertyKey), revived);
                }
            }

            value = holder[key];
        }

        if (sourceMap != null)
        {
            var context = new JSObject();
            if (key.Length == 0)
            {
                if (rootSource != null && IsPrimitiveJsonValue(value))
                    context["source"] = new JSString(rootSource);
            }
            else if (IsPrimitiveJsonValue(value) && TryGetSource(sourceMap, holder, key, value, out var source))
            {
                context["source"] = new JSString(source);
            }

            return reviver.InvokeCallback(new Arguments(holder, new JSString(key), value, context));
        }

        return reviver.InvokeCallback(new Arguments(holder, new JSString(key), value));
    }

    private static JSValue InternalizeJsonProperty(
        JSObject holder,
        uint index,
        JSFunction reviver,
        SourceMap sourceMap)
    {
        var value = holder[index];
        if (value is JSObject valueObject)
        {
            if (valueObject.IsArray)
            {
                var length = GetArrayLength(valueObject);
                for (uint childIndex = 0; childIndex < length; childIndex++)
                {
                    var revived = InternalizeJsonProperty(valueObject, childIndex, reviver, sourceMap);
                    if (revived.IsUndefined)
                        valueObject.Delete(childIndex);
                    else
                        CreateDataProperty(valueObject, CreateNumber(childIndex), revived);
                }
            }
            else
            {
                foreach (var propertyKey in EnumerableOwnPropertyNames(valueObject))
                {
                    var revived = InternalizeJsonProperty(valueObject, propertyKey, reviver, sourceMap, null);
                    if (revived.IsUndefined)
                        valueObject.Delete(propertyKey);
                    else
                        CreateDataProperty(valueObject, CreateString(propertyKey), revived);
                }
            }

            value = holder[index];
        }

        var key = index.ToString();
        if (sourceMap != null)
        {
            var context = new JSObject();
            if (IsPrimitiveJsonValue(value) && TryGetSource(sourceMap, holder, key, value, out var source))
                context["source"] = new JSString(source);

            return reviver.InvokeCallback(new Arguments(holder, new JSString(key), value, context));
        }

        return reviver.InvokeCallback(new Arguments(holder, new JSString(key), value));
    }

    [JSExport]
    public static JSValue Parse(in Arguments a)
    {
        var (text, receiver) = a.Get2();

        // JSON.parse step 1: Let JText be ? ToString(text). This runs the spec
        // ToString abstract operation (StringValue), not the lenient CLR ToString, so
        // a Symbol argument throws TypeError and a throwing toString/valueOf
        // propagates — neither must be mistaken for malformed JSON. It is also
        // performed exactly once, before parsing, and the result reused throughout.
        var jText = text.StringValue;

        SourceMap sourceMap = null;
        var sourceTextAccessEnabled = JSEngine.Current is JSContext context
            && context.HasExperimentalFeature(JavaScriptFeatureFlags.JsonParseSourceTextAccess);

        // The reader is the shared library's (SJL-5.5), and a text it refuses is a SyntaxError where
        // it stopped, or the RangeError of this engine's depth policy; no .NET exception is mapped.
        var parsed = sourceTextAccessEnabled
            ? JSJsonParser.ParseWithSource(
                jText,
                p =>
                {
                    RecordSource(sourceMap ??= new SourceMap(JSObjectReferenceComparer.Instance), p.holder, p.key, p.source, p.value);
                    return p.value;
                })
            : JSJsonParser.Parse(jText);

        parsed ??= JSNull.Value;
        if (sourceTextAccessEnabled)
            sourceMap ??= new SourceMap(JSObjectReferenceComparer.Instance);

        if (receiver is not JSFunction function)
            return parsed;

        // CreateDataPropertyOrThrow, not [[Set]]: an inherited setter on
        // Object.prototype[""] must not be invoked when building the wrapper.
        var root = new JSObject();
        CreateDataProperty(root, EmptyString, parsed);
        return InternalizeJsonProperty(
                root,
                "",
                function,
                sourceMap,
                jText) ?? JSNull.Value;

    }

    [JSExport]
    public static JSValue Stringify(in Arguments a)
    {
        var (f, r, pi) = a.Get3();
        if (f.IsUndefined)
            return f;

        TextWriter sb = new StringWriter();
        Func<(JSValue target, JSValue key, JSValue value), JSValue> replacer = null;
        List<string> propertyList = null;
        string indent = null;

        // build replacer...
        if (a.Length > 1)
        {
            if (a.Length > 2)
            {
                // §25.5.2.1 steps 4-5: a Number/String wrapper object is coerced to a
                // primitive with ToNumber / ToString — both run through ToPrimitive, so a
                // user-redefined valueOf / toString on the wrapper is observed (and an abrupt
                // completion propagates) rather than the raw internal slot being read.
                var space = pi;
                if (space is JSPrimitiveObject spaceWrapper)
                {
                    if (spaceWrapper.value is JSNumber)
                        space = CreateNumber(CoerceJsonWrapperToPrimitive(spaceWrapper, preferString: false).DoubleValue);
                    else if (spaceWrapper.value.IsString)
                        space = CreateString(CoerceJsonWrapperToPrimitive(spaceWrapper, preferString: true).ToString());
                }

                // Steps 6 to 8: the gap is the shared library's (SJL-5.5), min(10, ToIntegerOrInfinity)
                // spaces for a Number and the first ten code units of a String; an empty gap leaves
                // indent null so the output stays compact.
                var gap = space.IsNumber
                    ? JsonText.GapFromNumber(space.DoubleValue)
                    : space.IsString ? JsonText.GapFromString(space.ToString()) : string.Empty;
                if (gap.Length > 0)
                    indent = gap;
            }

            if (r is JSFunction rf)
            {
                replacer = (item) => rf.InvokeCallback(new Arguments(item.target, item.key, item.value));
            }
            else if (r.IsArray && r is JSObject ra)
            {
                propertyList = BuildPropertyList(ra);
            }
        }

        // CreateDataPropertyOrThrow, not [[Set]]: an inherited setter on
        // Object.prototype[""] must not be invoked when building the wrapper.
        var root = new JSObject();
        CreateDataProperty(root, EmptyString, f);

        f = ToJson(f, EmptyString);
        // Only a function replacer participates in SerializeJSONProperty for the
        // root holder; a PropertyList (array replacer) restricts object keys only
        // and must not be applied to the root value.
        if (replacer != null)
            f = replacer((root, EmptyString, f));

        // SerializeJSONProperty yields undefined for an undefined/callable/symbol
        // root value; JSON.stringify then returns the undefined value rather than
        // the string "null".
        if (f.IsUndefined || f is JSFunction || f is JSSymbol)
            return JSUndefined.Value;

        if (indent != null)
        {
            // SerializeJSONObject / SerializeJSONArray break lines with LINE FEED (U+000A) alone.
            // WriteLine writes the writer's NewLine, which defaults to Environment.NewLine (CR LF
            // on Windows); IndentedTextWriter forwards NewLine to the StringWriter it wraps.
            var writer = new IndentedTextWriter(sb, indent) { NewLine = "\n" };
            Stringify(writer, f, replacer, propertyList, writer, []);
        }
        else
        {
            Stringify(sb, f, replacer, propertyList, null, []);
        }

        return new JSString(sb.ToString());
    }

    public static string Stringify(JSValue value)
    {
        value = ToJson(value, EmptyString);
        var sb = new StringWriter();
        Stringify(sb, value, null, null, null, []);
        return sb.ToString();
    }

    private static readonly KeyString rawJSONKey = KeyStrings.GetOrCreate("rawJSON");
    private static readonly KeyString isRawJSONKey = KeyStrings.GetOrCreate("isRawJSON");

    [JSExport("rawJSON", Length = 1)]
    public static JSValue RawJSON(in Arguments a)
    {
        var text = a.Get1();
        var str = text.StringValue;
        if (str.Length == 0)
            throw JSEngine.NewSyntaxError("JSON.rawJSON requires a non-empty string");
        if (IsIllegalRawJsonBoundaryChar(str[0]) || IsIllegalRawJsonBoundaryChar(str[^1]))
            throw JSEngine.NewSyntaxError("JSON.rawJSON cannot start or end with whitespace");

        var parsed = JSJsonParser.Parse(str);
        if (parsed is JSObject)
            throw JSEngine.NewSyntaxError("JSON.rawJSON cannot be called with a JSON object or array");

        // §JSON.rawJSON: obj is OrdinaryObjectCreate(null, « [[IsRawJSON]] ») with its "rawJSON"
        // made by CreateDataPropertyOrThrow, so enumerable (J7: it was not), and then frozen.
        var result = new JSRawJsonObject(str);
        result.BasePrototypeObject = null;
        result.FastAddValue(rawJSONKey, CreateString(str), JSPropertyAttributes.EnumerableConfigurableValue);
        FreezeObject(result);
        return result;
    }

    /// <summary>
    /// A value <c>JSON.rawJSON</c> made: the brand is the [[IsRawJSON]] internal slot, which this
    /// type is, and not a property a script can write. Until SJL-5.5 any frozen object with an own
    /// string "rawJSON" passed for one, and its text went into JSON.stringify's output unchecked (J6).
    /// </summary>
    private sealed class JSRawJsonObject(string text) : JSObject
    {
        /// <summary>The validated text, which is also the frozen "rawJSON" property's value.</summary>
        internal string Text { get; } = text;
    }

    private static bool IsIllegalRawJsonBoundaryChar(char ch)
        => ch is '\t' or '\n' or '\r' or ' ';

    [JSExport("isRawJSON", Length = 1)]
    public static JSValue IsRawJSON(in Arguments a)
        => TryGetRawJsonText(a.Get1(), out _) ? JSBoolean.True : JSBoolean.False;

    // A value produced by JSON.rawJSON, which only JSON.rawJSON constructs: SerializeJSONProperty
    // emits its text verbatim instead of serializing the object. A Proxy of one, or an object that
    // merely carries a "rawJSON" property, is an ordinary object.
    private static bool TryGetRawJsonText(JSValue value, out string text)
    {
        if (value is JSRawJsonObject raw)
        {
            text = raw.Text;
            return true;
        }

        text = null;
        return false;
    }

    private static void Stringify(
        TextWriter sb,
        JSValue target,
        Func<(JSValue, JSValue, JSValue), JSValue> replacer,
        List<string> propertyList,
        IndentedTextWriter indent,
        HashSet<JSObject> stack)
    {
        // SerializeJSONProperty step 6: a Number/String/Boolean/BigInt wrapper object is
        // unwrapped to its primitive before serialization. Boolean and BigInt wrappers use
        // the internal slot directly (steps 6.c/6.d), but Number and String wrappers run
        // ToNumber / ToString — both of which start at ToPrimitive and so MUST observe any
        // user-redefined valueOf / toString on the prototype chain (test262
        // sm/JSON/stringify-boxed-primitives). A BigInt wrapper still surfaces as a
        // bare BigInt, which step 10 then rejects with a TypeError.
        if (target is JSPrimitiveObject wrapper)
        {
            var wrapped = wrapper.value;
            if (wrapped is JSNumber)
                // ToNumber(wrapper): ToPrimitive then ToNumber, so when valueOf/toString are
                // removed and ToPrimitive falls back to Object.prototype.toString ("[object
                // Number]"), the final ToNumber yields NaN and the value serializes as null.
                target = CreateNumber(CoerceJsonWrapperToPrimitive(wrapper, preferString: false).DoubleValue);
            else if (wrapped.IsString)
                target = CreateString(CoerceJsonWrapperToPrimitive(wrapper, preferString: true).ToString());
            else
                target = wrapped;
        }

        if (target == null || target.IsNullOrUndefined)
        {
            sb.Write("null");
            return;
        }

        if (target == JSBoolean.True)
        {
            sb.Write("true");
            return;
        }

        if (target == JSBoolean.False)
        {
            sb.Write("false");
            return;
        }

        switch (target)
        {
            case JSNumber n:
                // Non-finite numbers (NaN, ±Infinity) serialize as null (SerializeJSONProperty step 9).
                // Otherwise use the spec Number::toString (NumberText.ToString) so negative zero
                // serializes as "0" (ToString(-0) is "0") and large/small magnitudes use
                // ECMAScript scientific notation rather than .NET's "R" formatting.
                sb.Write(double.IsFinite(n.value) ? NumberText.ToString(n.value) : "null");
                return;

            case JSString str:
                WriteQuoted(str.value, sb);
                return;

            case JSBigInt:
                throw JSEngine.NewTypeError("Do not know how to serialize a BigInt");

            case JSFunction _:
                return;

        }

        // SerializeJSONProperty (json-parse-with-source): a JSON.rawJSON object is emitted
        // as its stored text verbatim, never serialized as an ordinary object.
        if (TryGetRawJsonText(target, out var rawJsonText))
        {
            sb.Write(rawJsonText);
            return;
        }

        if (target is JSObject arrayObject && arrayObject.IsArray)
        {
            StringifyArray(sb, arrayObject, replacer, propertyList, indent, stack);
            return;
        }

        if (!stack.Add((JSObject)target))
            throw JSEngine.NewTypeError("Converting circular structure to JSON");

        try
        {
        sb.Write('{');

        if (indent != null)
            indent.Indent++;

        bool first = true;
        // the only left type is JSObject...
        var obj = target as JSObject;

        // Serializes a single own enumerable property. Per OrdinaryOwnPropertyKeys,
        // integer-indexed keys come first (ascending), then string keys in
        // insertion order; SerializeJSONObject visits EnumerableOwnPropertyNames in
        // that same order. `keyText` is both the emitted property name and the key
        // handed to `toJSON`/the replacer.
        // Emits a single resolved member value under `keyText`, applying toJSON,
        // the function replacer, and indentation. `jsValue` has already been read
        // from the holder.
        void EmitMember(in StringSpan keyText, JSValue keyValue, JSValue jsValue)
        {
            // SerializeJSONProperty runs toJSON (step 2) then the replacer (step 3) before
            // deciding whether a member produces text — the replacer is consulted even when
            // the slot value is undefined (e.g. a property an earlier getter deleted).
            jsValue = ToJson(jsValue, keyValue);

            if (replacer != null)
                jsValue = replacer((target, keyValue, jsValue));

            // Steps 8-11: an undefined / callable / Symbol value has no JSON text, so the
            // member is omitted entirely (neither key nor colon is written) — unlike an
            // array element, which would be rendered as "null".
            if (jsValue.IsUndefined || jsValue is JSFunction || jsValue is JSSymbol)
                return;

            // write indention here...
            if (!first)
                sb.Write(',');

            first = false;
            if (indent != null)
                sb.WriteLine();

            WriteQuoted(keyText, sb);
            sb.Write(':');
            if (indent != null)
                sb.Write(' ');

            Stringify(sb, jsValue, replacer, propertyList, indent, stack);
        }

        if (propertyList != null)
        {
            // SerializeJSONObject step 5: when a PropertyList is present, iterate it
            // in order and read each key via [[Get]] (so inherited and non-enumerable
            // listed keys are still serialized), ignoring own-property order.
            foreach (var key in propertyList)
            {
                var keyValue = CreateString(key);
                EmitMember(key, keyValue, obj[KeyStrings.GetOrCreate(key)]);
            }
        }
        else
        {
            // SerializeJSONObject step 6.a snapshots EnumerableOwnPropertyNames ONCE, then
            // step 8 reads each member live via [[Get]] (SerializeJSONProperty step 1). So a
            // property a sibling getter deletes mid-serialization is still visited (its value
            // reads as undefined and the replacer still runs), while a property added
            // mid-serialization is not. Integer-indexed keys (stored apart from named ones)
            // come first in ascending order, then named keys in insertion order.
            List<string> snapshot;
            if (obj is JSProxy || obj.HasObservableOwnProperties)
            {
                // A Proxy exposes its own keys only through the [[OwnPropertyKeys]] and
                // [[GetOwnProperty]] traps; its internal slot stores nothing. Route it
                // through EnumerableOwnPropertyNames so the traps drive the key list.
                // An exotic object such as a module namespace likewise keeps its
                // properties outside the storage.
                snapshot = EnumerableOwnPropertyNames(obj);
            }
            else
            {
                snapshot = [];
                foreach (var (index, element) in obj.GetElements(create: false).AllValues())
                {
                    if (element.IsEmpty || !element.IsEnumerable)
                        continue;
                    snapshot.Add(index.ToString(System.Globalization.CultureInfo.InvariantCulture));
                }

                var pen = obj.GetOwnProperties().GetEnumerator();
                while (pen.MoveNext(out var key, out var value))
                {
                    if (value.IsEmpty || !value.IsEnumerable)
                        continue;
                    snapshot.Add(key.Value.ToString());
                }
            }

            foreach (var keyText in snapshot)
                EmitMember(keyText, CreateString(keyText), obj[KeyStrings.GetOrCreate(keyText)]);
        }

        if (indent != null)
        {
            // SerializeJSONObject: an empty object collapses to "{}" with no interior newline.
            if (!first)
                sb.WriteLine();
            indent.Indent--;
        }

        sb.Write('}');
        }
        finally
        {
            stack.Remove((JSObject)target);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static JSValue ToJson(JSValue value, JSValue key)
    {
        if (value is not JSObject jobj)
        {
            // SerializeJSONProperty step 2 also reads toJSON when Type(value) is
            // BigInt (the BigInt proposal). GetV resolves BigInt.prototype.toJSON
            // and calls it with the BigInt as receiver.
            if (value is JSBigInt)
            {
                var bigIntToJson = value[KeyStrings.toJSON];
                if (bigIntToJson is IJSFunction bp)
                    // Resolve a tail-call sentinel: under the script host a toJSON whose body
                    // ends in `return f()` (e.g. `return this.toString()`) returns a JSTailCall,
                    // which would otherwise be serialized as a bare object ("{}") instead of its
                    // resolved value (#912 Problem 14).
                    return JSTailCall.Resolve(bp.Delegate(new Arguments(value, key)));
            }

            return value;
        }

        // No ValueOf/ToPrimitive here: SerializeJSONProperty unwraps Number/String/
        // Boolean/BigInt wrapper objects via their internal slot at serialization time
        // (see the JSPrimitiveObject branch in Stringify), never by invoking a user
        // valueOf. Calling jobj.ValueOf() here would run an object's own `valueOf`
        // property (e.g. `{valueOf: 3}` → "3 is not a function").

        // SerializeJSONProperty: Get(value, "toJSON") then call only if IsCallable.
        // A present-but-non-callable toJSON is ignored (must NOT throw), unlike the
        // GetMethod abstract op which throws on a non-callable own property.
        var toJson = jobj[KeyStrings.toJSON];
        if (toJson is IJSFunction p)
            // Resolve a tail-call sentinel (script host): a toJSON ending in `return f()`
            // returns a JSTailCall that must be trampolined to its value before serialization.
            return JSTailCall.Resolve(p.Delegate(new Arguments(value, key)));

        return value;
    }


    // Spec ToPrimitive on a Number / String wrapper, used by SerializeJSONProperty step 6.
    // @@toPrimitive (none on the built-in wrappers, but a subclass or proxy may add one) is
    // tried first; then valueOf / toString are looked up through the prototype chain so a
    // user-redefined Number.prototype.valueOf is observed. JSPrimitiveObject's own
    // ValueOf / ToString shortcut to the internal slot for performance, so the wrapper-to-
    // primitive coercion here cannot delegate to them.
    private static JSValue CoerceJsonWrapperToPrimitive(JSPrimitiveObject wrapper, bool preferString)
    {
        var toPrimitive = wrapper[(IJSSymbol)JSSymbol.toPrimitive];
        if (!toPrimitive.IsNullOrUndefined)
        {
            if (!toPrimitive.IsFunction)
                throw JSEngine.NewTypeError("@@toPrimitive is not callable");

            var hint = CreateString(preferString ? "string" : "number");
            var primitive = toPrimitive.InvokeFunction(new Arguments(wrapper, hint));
            if (primitive.IsObject)
                throw JSEngine.NewTypeError("Cannot convert object to primitive value");

            return primitive;
        }

        var firstKey = preferString ? KeyStrings.toString : KeyStrings.valueOf;
        var secondKey = preferString ? KeyStrings.valueOf : KeyStrings.toString;

        var first = wrapper[firstKey];
        if (first.IsFunction)
        {
            var primitive = first.InvokeFunction(new Arguments(wrapper));
            if (!primitive.IsObject)
                return primitive;
        }

        var second = wrapper[secondKey];
        if (second.IsFunction)
        {
            var primitive = second.InvokeFunction(new Arguments(wrapper));
            if (!primitive.IsObject)
                return primitive;
        }

        throw JSEngine.NewTypeError("Cannot convert object to primitive value");
    }

    /// <summary>
    /// QuoteJSONString (§25.5.2.3): the shared library's (SJL-5.5), written to <paramref name="result"/>.
    /// A lone surrogate is escaped as \uXXXX, so the output is well formed.
    /// </summary>
    private static void WriteQuoted(in StringSpan input, TextWriter result) => result.Write(JsonText.Quote(input.AsSpan()));
}
