using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.BuiltIns.Array;
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
using Broiler.JavaScript.Storage;
using System.Text.Json;

namespace Broiler.JavaScript.BuiltIns.Json;

public delegate JSValue JsonParserReceiver((JSObject holder, string key, JSValue value) property);

/// <summary>
/// Delegate for reviver with source text access (ES2026 §4.7).
/// </summary>
public delegate JSValue JsonParserReceiverWithSource((JSObject holder, string key, JSValue value, string source) property);

[JSClassGenerator("JSON"), JSInternalObject]
public partial class JSJSON : JSObject
{
    private const double MaxArrayLikeLength = 9007199254740991d;

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

    private static long ToLength(JSValue value)
    {
        if (value == null || value.IsUndefined)
            return 0;

        var length = ToNumberPrimitive(value).DoubleValue;
        if (double.IsNaN(length) || length <= 0)
            return 0;

        if (double.IsPositiveInfinity(length) || length >= MaxArrayLikeLength)
            return (long)MaxArrayLikeLength;

        return (long)Math.Floor(length);
    }

    private static long GetArrayLength(JSObject valueObject)
    {
        if (valueObject is JSProxy proxy && proxy.IsArray && !proxy.HasTrap(KeyStrings.get))
            return proxy.Target.Length;

        return ToLength(valueObject[KeyStrings.length]);
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
            else if (v is JSPrimitiveObject wrapper)
            {
                var primitive = wrapper.ValueOf();
                if (primitive.IsString || primitive is JSNumber)
                    item = primitive.ToString();
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
            for (uint index = 0; index < length; index++)
            {
                if (index > 0)
                    sb.Write(',');

                if (indent != null)
                    sb.WriteLine();

                var jsValue = ToJson(array[index], JSValue.CreateString(index.ToString()));
                // A PropertyList never filters array elements; only a function
                // replacer is consulted here.
                if (replacer != null)
                    jsValue = replacer((array, JSValue.CreateString(index.ToString()), jsValue));

                if (jsValue.IsUndefined || jsValue is JSFunction)
                    jsValue = JSNull.Value;

                Stringify(sb, jsValue, replacer, propertyList, indent, stack);
            }

            if (indent != null)
            {
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
        Dictionary<JSObject, Dictionary<string, string>> sourceMap,
        JSObject holder,
        string key,
        string source)
    {
        if (source == null)
            return;

        if (!sourceMap.TryGetValue(holder, out var holderSources))
        {
            holderSources = [];
            sourceMap[holder] = holderSources;
        }

        holderSources[key] = source;
    }

    private static bool TryGetSource(
        Dictionary<JSObject, Dictionary<string, string>> sourceMap,
        JSObject holder,
        string key,
        out string source)
    {
        if (sourceMap.TryGetValue(holder, out var holderSources)
            && holderSources.TryGetValue(key, out source))
        {
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
        Dictionary<JSObject, Dictionary<string, string>> sourceMap,
        string rootSource)
    {
        if (key.Length > 0)
        {
            var propertyKey = JSValue.CreateString(key).ToKey(false);
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
                        CreateDataProperty(valueObject, JSValue.CreateNumber(index), revived);
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
                        CreateDataProperty(valueObject, JSValue.CreateString(propertyKey), revived);
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
            else if (IsPrimitiveJsonValue(value) && TryGetSource(sourceMap, holder, key, out var source))
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
        Dictionary<JSObject, Dictionary<string, string>> sourceMap)
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
                        CreateDataProperty(valueObject, JSValue.CreateNumber(childIndex), revived);
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
                        CreateDataProperty(valueObject, JSValue.CreateString(propertyKey), revived);
                }
            }

            value = holder[index];
        }

        var key = index.ToString();
        if (sourceMap != null)
        {
            var context = new JSObject();
            if (IsPrimitiveJsonValue(value) && TryGetSource(sourceMap, holder, key, out var source))
                context["source"] = new JSString(source);

            return reviver.InvokeCallback(new Arguments(holder, new JSString(key), value, context));
        }

        return reviver.InvokeCallback(new Arguments(holder, new JSString(key), value));
    }

    [JSExport]
    public static JSValue Parse(in Arguments a)
    {
        var (text, receiver) = a.Get2();

        Dictionary<JSObject, Dictionary<string, string>> sourceMap = null;
        var sourceTextAccessEnabled = JSEngine.Current is JSContext context
            && context.HasExperimentalFeature(JavaScriptFeatureFlags.JsonParseSourceTextAccess);

        JSValue parsed;
        try
        {
            parsed = sourceTextAccessEnabled
                ? JSJsonParser.ParseWithSource(
                    text.ToString(),
                    p =>
                    {
                        RecordSource(sourceMap ??= new Dictionary<JSObject, Dictionary<string, string>>(JSObjectReferenceComparer.Instance), p.holder, p.key, p.source);
                        return p.value;
                    })
                : JSJsonParser.Parse(text.ToString(), null);
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or NotSupportedException)
        {
            throw JSEngine.NewSyntaxError(ex.Message);
        }

        parsed ??= JSNull.Value;
        if (sourceTextAccessEnabled)
            sourceMap ??= new Dictionary<JSObject, Dictionary<string, string>>(JSObjectReferenceComparer.Instance);

        if (receiver is not JSFunction function)
            return parsed;

        var root = new JSObject();
        root[""] = parsed;
        return InternalizeJsonProperty(
                root,
                "",
                function,
                sourceMap,
                text.ToString()) ?? JSNull.Value;

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
                if (pi is JSNumber jn)
                {
                    indent = new string(' ', pi.IntValue);
                }
                else if (pi is JSString js)
                {
                    indent = js.ToString();
                }
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

        var root = new JSObject();
        var emptyKey = KeyStrings.GetOrCreate(string.Empty);
        root[emptyKey] = f;

        f = ToJson(f, JSValue.EmptyString);
        // Only a function replacer participates in SerializeJSONProperty for the
        // root holder; a PropertyList (array replacer) restricts object keys only
        // and must not be applied to the root value.
        if (replacer != null)
            f = replacer((root, JSValue.EmptyString, f));

        // SerializeJSONProperty yields undefined for an undefined/callable/symbol
        // root value; JSON.stringify then returns the undefined value rather than
        // the string "null".
        if (f.IsUndefined || f is JSFunction || f is JSSymbol)
            return JSUndefined.Value;

        if (indent != null)
        {
            var writer = new IndentedTextWriter(sb, indent);
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
        value = ToJson(value, JSValue.EmptyString);
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

        JSValue parsed;
        try
        {
            parsed = JSJsonParser.Parse(str, null);
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or NotSupportedException)
        {
            throw JSEngine.NewSyntaxError(ex.Message);
        }

        if (parsed is JSObject)
            throw JSEngine.NewSyntaxError("JSON.rawJSON cannot be called with a JSON object or array");

        var result = new JSObject();
        result.FastAddValue(rawJSONKey, JSValue.CreateString(str), JSPropertyAttributes.ConfigurableValue);
        JSObject.FreezeObject(result);
        return result;
    }

    private static bool IsIllegalRawJsonBoundaryChar(char ch)
        => ch is '\t' or '\n' or '\r' or ' ';

    [JSExport("isRawJSON", Length = 1)]
    public static JSValue IsRawJSON(in Arguments a)
    {
        var value = a.Get1();
        if (value is not JSObject obj)
            return JSBoolean.False;

        if (!obj.IsFrozen())
            return JSBoolean.False;

        ref var ownProps = ref obj.GetOwnProperties();
        if (!ownProps.TryGetValue(rawJSONKey.Key, out var prop) || prop.IsEmpty)
            return JSBoolean.False;

        return prop.value is JSValue v && v.IsString ? JSBoolean.True : JSBoolean.False;
    }

    private static void Stringify(
        TextWriter sb,
        JSValue target,
        Func<(JSValue, JSValue, JSValue), JSValue> replacer,
        List<string> propertyList,
        IndentedTextWriter indent,
        HashSet<JSObject> stack)
    {
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
                sb.Write(n.value.ToString());
                return;

            case JSString str:
                QuoteString(str.value, sb);
                return;

            case JSBigInt:
                throw JSEngine.NewTypeError("Do not know how to serialize a BigInt");

            case JSFunction _:
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
            if (jsValue.IsUndefined || jsValue is JSFunction)
                return;

            jsValue = ToJson(jsValue, keyValue);

            // check replacer...
            if (replacer != null)
            {
                jsValue = replacer((target, keyValue, jsValue));
                if (jsValue.IsUndefined)
                    return;
            }

            // write indention here...
            if (!first)
                sb.Write(',');

            first = false;
            if (indent != null)
                sb.WriteLine();

            QuoteString(keyText, sb);
            sb.Write(':');
            if (indent != null)
                sb.Write(' ');

            Stringify(sb, jsValue, replacer, propertyList, indent, stack);
        }

        void WriteMember(in StringSpan keyText, JSValue keyValue, in JSProperty value)
        {
            if (value.IsEmpty || !value.IsEnumerable)
                return;

            JSValue jsValue;
            if (!value.IsValue)
            {
                if (value.get == null)
                    return;

                jsValue = ((JSFunction)value.get).InvokeCallback(new Arguments(target));
            }
            else
            {
                jsValue = (JSValue)value.value;
            }

            EmitMember(keyText, keyValue, jsValue);
        }

        if (propertyList != null)
        {
            // SerializeJSONObject step 5: when a PropertyList is present, iterate it
            // in order and read each key via [[Get]] (so inherited and non-enumerable
            // listed keys are still serialized), ignoring own-property order.
            foreach (var key in propertyList)
            {
                var keyValue = JSValue.CreateString(key);
                EmitMember(key, keyValue, obj[KeyStrings.GetOrCreate(key)]);
            }
        }
        else
        {
            // Integer-indexed properties (stored separately from named ones) are own
            // enumerable string keys too and must be serialized — previously they were
            // skipped entirely, so e.g. `JSON.stringify({0:'a',x:1})` dropped "0".
            // ElementArray.AllValues() yields them in ascending index order.
            foreach (var (index, element) in obj.GetElements(create: false).AllValues())
            {
                var indexText = index.ToString(System.Globalization.CultureInfo.InvariantCulture);
                WriteMember(indexText, JSValue.CreateString(indexText), element);
            }

            var pen = obj.GetOwnProperties().GetEnumerator();
            while (pen.MoveNext(out var key, out var value))
                WriteMember(key.Value, KeyStringCoreExtensions.GetJSString(value.key), value);
        }

        if (indent != null)
        {
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
            return value;

        var primitive = jobj.ValueOf();
        if (!primitive.IsObject)
            value = primitive;

        var p = jobj.GetMethod(KeyStrings.toJSON);
        if (p == null)
            return value;

        return p(new Arguments(value, key));
    }


    /// <summary>
    /// Adds double quote characters to the start and end of the given string and converts any
    /// invalid characters into escape sequences.
    /// </summary>
    /// <param name="input"> The string to quote. </param>
    /// <param name="result"> The StringBuilder to write the quoted string to. </param>
    private static void QuoteString(in StringSpan input, TextWriter result)
    {
        result.Write('\"');

        // Check if there are characters that need to be escaped.
        // These characters include '"', '\' and any character with an ASCII value less than 32.
        bool containsUnsafeCharacters = false;
        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];

            if (c == '\\' || c == '\"' || c < 0x20)
            {
                containsUnsafeCharacters = true;
                break;
            }
        }

        if (containsUnsafeCharacters == false)
        {
            // The string does not contain escape characters.
            result.Write(input);
        }
        else
        {
            // The string contains escape characters - fall back to the slower code path.
            var en = input.GetEnumerator();
            while (en.MoveNext(out var c))
            {
                switch (c)
                {
                    case '\"':
                    case '\\':
                        result.Write('\\');
                        result.Write(c);
                        break;
                    case '\b':
                        result.Write("\\b");
                        break;
                    case '\f':
                        result.Write("\\f");
                        break;
                    case '\n':
                        result.Write("\\n");
                        break;
                    case '\r':
                        result.Write("\\r");
                        break;
                    case '\t':
                        result.Write("\\t");
                        break;
                    default:
                        if (c < 0x20)
                        {
                            result.Write('\\');
                            result.Write('u');
                            result.Write(((int)c).ToString("x4"));
                        }
                        else
                            result.Write(c);
                        break;
                }
            }
        }

        result.Write('\"');
    }
}
