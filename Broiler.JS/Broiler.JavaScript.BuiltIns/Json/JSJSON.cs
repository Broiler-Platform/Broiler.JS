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

namespace Broiler.JavaScript.BuiltIns.Json;

public delegate JSValue JsonParserReceiver((JSObject holder, string key, JSValue value) property);

/// <summary>
/// Delegate for reviver with source text access (ES2026 §4.7).
/// </summary>
public delegate JSValue JsonParserReceiverWithSource((JSObject holder, string key, JSValue value, string source) property);

[JSClassGenerator("JSON"), JSInternalObject]
public partial class JSJSON : JSObject
{
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
        var value = holder[key];
        if (value is JSObject valueObject)
        {
            if (valueObject.IsArray)
            {
                var length = (uint)Math.Max(valueObject.Length, 0);
                for (uint index = 0; index < length; index++)
                {
                    var revived = InternalizeJsonProperty(valueObject, index, reviver, sourceMap);
                    if (revived.IsUndefined)
                        valueObject.Delete(index);
                    else
                        valueObject[index] = revived;
                }
            }
            else
            {
                List<KeyString> propertyKeys = [];
                var properties = valueObject.GetOwnProperties(false).GetEnumerator();
                while (properties.MoveNext(out var keyString, out var property))
                {
                    if (!property.IsEmpty)
                        propertyKeys.Add(keyString);
                }

                foreach (var propertyKey in propertyKeys)
                {
                    var revived = InternalizeJsonProperty(valueObject, propertyKey.ToString(), reviver, sourceMap, null);
                    if (revived.IsUndefined)
                        valueObject.Delete(propertyKey);
                    else
                        valueObject[propertyKey] = revived;
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
            else if (TryGetSource(sourceMap, holder, key, out var source))
            {
                context["source"] = new JSString(source);
            }

            return reviver.f(new Arguments(holder, new JSString(key), value, context));
        }

        return reviver.f(new Arguments(holder, new JSString(key), value));
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
                var length = (uint)Math.Max(valueObject.Length, 0);
                for (uint childIndex = 0; childIndex < length; childIndex++)
                {
                    var revived = InternalizeJsonProperty(valueObject, childIndex, reviver, sourceMap);
                    if (revived.IsUndefined)
                        valueObject.Delete(childIndex);
                    else
                        valueObject[childIndex] = revived;
                }
            }
            else
            {
                List<KeyString> propertyKeys = [];
                var properties = valueObject.GetOwnProperties(false).GetEnumerator();
                while (properties.MoveNext(out var keyString, out var property))
                {
                    if (!property.IsEmpty)
                        propertyKeys.Add(keyString);
                }

                foreach (var propertyKey in propertyKeys)
                {
                    var revived = InternalizeJsonProperty(valueObject, propertyKey.ToString(), reviver, sourceMap, null);
                    if (revived.IsUndefined)
                        valueObject.Delete(propertyKey);
                    else
                        valueObject[propertyKey] = revived;
                }
            }

            value = holder[index];
        }

        var key = index.ToString();
        if (sourceMap != null)
        {
            var context = new JSObject();
            if (TryGetSource(sourceMap, holder, key, out var source))
                context["source"] = new JSString(source);

            return reviver.f(new Arguments(holder, new JSString(key), value, context));
        }

        return reviver.f(new Arguments(holder, new JSString(key), value));
    }

    [JSExport]
    public static JSValue Parse(in Arguments a)
    {
        var (text, receiver) = a.Get2();

        Dictionary<JSObject, Dictionary<string, string>> sourceMap = null;
        var sourceTextAccessEnabled = JSEngine.Current is JSContext context
            && context.HasExperimentalFeature(JavaScriptFeatureFlags.JsonParseSourceTextAccess);

        var parsed = sourceTextAccessEnabled
            ? JSJsonParser.ParseWithSource(
                text.ToString(),
                p =>
                {
                    RecordSource(sourceMap ??= [], p.holder, p.key, p.source);
                    return p.value;
                })
            : JSJsonParser.Parse(text.ToString(), null);

        parsed ??= JSNull.Value;

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
                replacer = (item) => rf.f(new Arguments(item.target, item.key, item.value));
            }
            else if (r.IsArray && r is JSObject ra)
            {
                StringMap<int> map = new();

                replacer = (item) =>
                {
                    var en = ra.GetElementEnumerator();
                    while (en.MoveNext(out var hasValue, out var ri, out var index))
                    {
                        map.Put(ri.ToString()) = 1;
                    }

                    if (map.TryGetValue(item.key.ToString(), out var a1))
                        return item.value;

                    return JSUndefined.Value;
                };
            }
        }

        if (indent != null)
        {
            var writer = new IndentedTextWriter(sb, indent);
            Stringify(writer, f, replacer, writer);
        }
        else
        {
            Stringify(sb, f, replacer, null);
        }

        return new JSString(sb.ToString());
    }

    public static string Stringify(JSValue value)
    {
        var sb = new StringWriter();
        Stringify(sb, value, null, null);
        return sb.ToString();
    }

    private static void Stringify(TextWriter sb, JSValue target, Func<(JSValue, JSValue, JSValue), JSValue> replacer, IndentedTextWriter indent)
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

            case JSArray a:
                sb.Write('[');
                if (indent != null)
                    indent.Indent++;

                bool f = true;
                var ae = a.GetElementEnumerator();

                while (ae.MoveNext(out var hasValue, out var item, out var index))
                {
                    if (!f)
                        sb.Write(',');

                    f = false;
                    if (indent != null)
                        sb.WriteLine();

                    Stringify(sb, ToJson(item), replacer, indent);
                }

                if (indent != null)
                {
                    sb.WriteLine();
                    indent.Indent--;
                }

                sb.Write(']');
                return;
        }

        sb.Write('{');

        if (indent != null)
            indent.Indent++;

        bool first = true;
        // the only left type is JSObject...
        var obj = target as JSObject;
        var pen = obj.GetOwnProperties().GetEnumerator();

        while (pen.MoveNext(out var key, out var value))
        {
            if (value.IsEmpty || !value.IsEnumerable)
                continue;

            JSValue jsValue;
            if (!value.IsValue)
            {
                if (value.get == null)
                    continue;

                jsValue = ((JSFunction)value.get).f(new Arguments(target));
            }
            else
            {
                jsValue = (JSValue)value.value;
            }

            if (jsValue.IsUndefined || jsValue is JSFunction)
                continue;

            jsValue = ToJson(jsValue);

            // check replacer...
            if (replacer != null)
            {
                jsValue = replacer((target, KeyStringCoreExtensions.GetJSString(value.key), jsValue));
                if (jsValue.IsUndefined)
                    continue;
            }

            // write indention here...
            if (!first)
                sb.Write(',');

            first = false;
            if (indent != null)
                sb.WriteLine();

            QuoteString(key.Value, sb);
            sb.Write(':');
            if (indent != null)
                sb.Write(' ');

            Stringify(sb, jsValue, replacer, indent);

        }

        if (indent != null)
        {
            sb.WriteLine();
            indent.Indent--;
        }

        sb.Write('}');
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static JSValue ToJson(JSValue value)
    {
        if (value is not JSObject jobj)
            return value;

        var p = jobj.GetMethod(KeyStrings.toJSON);
        if (p == null)
            return value;

        return p(new Arguments(value));
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
