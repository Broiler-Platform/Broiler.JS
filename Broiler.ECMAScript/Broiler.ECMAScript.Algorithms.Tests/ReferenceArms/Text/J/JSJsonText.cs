// REFERENCE ARM J: a test-only copy of Broiler.JS's implementation before the cut-over, for the
// reference-arm comparison of bundle sjl-5-001 (docs/evidence/sjl-5-001/reference-arms.md). It is
// never referenced outside this test project, and it is retired at SJL-16.
//
// Copied from Broiler.JS at 62c42e3 (SJL-4's merge, the commit the area verdict's correspondence reads):
//   Broiler.JS/Broiler.JavaScript.BuiltIns/Json/JSJsonParser.cs
//     blob 099b1ff239fc80fbb65ae2401a941fe779ba4931: the whole class JSJsonParser
//   Broiler.JS/Broiler.JavaScript.BuiltIns/Json/JSJSON.cs
//     blob 2c48ce25108f1a283bf00e53662db0e26a585250: Parse's call of JSJsonParser.Parse and its exception filter
//     (:391-410, :412), the gap's normalisation in Stringify (:452-475) and QuoteString (:884-976)
//
// Changes from the source, none of which changes a result:
//   - line endings normalised to LF where the source has CRLF, and a byte-order mark dropped;
//   - the namespace; the members sit in a static class of their own; private and public members are internal;
//   - the [JSExport...] and [JSPrototypeMethod] attributes are not copied;
//   - JSValue and the types the parser builds (JSString, JSNumber, JSBoolean, JSNull, JSArray, JSObject, KeyStrings,
//     JSPropertyAttributes) are the plain values below, which hold what the engine's would: an object keeps its
//     integer-index keys in ascending order and its other keys in their first position with their last value, as
//     the engine's elements and property storage do; JsonParserReceiver and JsonParserReceiverWithSource are
//     delegates of the shapes the parser calls, and the comparison passes neither;
//   - Parse is JSJSON.Parse's parse of its String without source text access and without a reviver: the
//     try/catch and its filter, and `parsed ??= JSNull.Value`, verbatim; JSEngine.NewSyntaxError(message) is the
//     recorded ArmError("SyntaxError", message);
//   - GapFromNumber and GapFromString are the two branches of Stringify's normalisation, after its wrapper
//     unwrapping (the adapter's): `indent` starts null, and the function returns it, or the empty String when
//     it stays null;
//   - QuoteString takes a String where the source takes Broiler.JS's StringSpan;
//   - nullable annotations (`?` and `!`) where the source's project builds without nullable warnings: the
//     receivers and the two Parse functions' results may be null, and GetString's of a String or a property
//     name is not.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using Broiler.ECMAScript.Numbers;

namespace Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.Text.J;

/// <summary>A value as arm J's parser builds it.</summary>
internal abstract class JSValue
{
    public virtual bool IsUndefined => false;
}

internal sealed class JSString(string value) : JSValue
{
    public string Value { get; } = value;

    public override string ToString() => Value;
}

internal sealed class JSNumber(double value) : JSValue
{
    public double Value { get; } = value;
}

internal sealed class JSBoolean : JSValue
{
    private JSBoolean(bool value) => Value = value;

    public bool Value { get; }

    public static JSBoolean True { get; } = new(true);

    public static JSBoolean False { get; } = new(false);
}

internal sealed class JSNull : JSValue
{
    public static JSNull Value { get; } = new();
}

internal sealed class JSArray : JSValue
{
    public List<JSValue> Elements { get; } = [];

    public int Length => Elements.Count;

    public void Add(JSValue value) => Elements.Add(value);
}

internal readonly record struct KeyString(string Key);

internal static class KeyStrings
{
    public static KeyString GetOrCreate(string name) => new(name);
}

internal enum JSPropertyAttributes
{
    EnumerableConfigurableValue,
}

internal sealed class JSObject : JSValue
{
    public SortedDictionary<uint, JSValue> Indexed { get; } = [];

    public List<KeyValuePair<string, JSValue>> Named { get; } = [];

    public void FastAddValue(uint index, JSValue value, JSPropertyAttributes attributes)
    {
        _ = attributes;
        Indexed[index] = value;
    }

    public void FastAddValue(KeyString key, JSValue value, JSPropertyAttributes attributes)
    {
        _ = attributes;
        var at = Named.FindIndex(m => m.Key == key.Key);
        if (at < 0)
            Named.Add(new KeyValuePair<string, JSValue>(key.Key, value));
        else
            Named[at] = new KeyValuePair<string, JSValue>(key.Key, value);
    }
}

internal delegate JSValue JsonParserReceiver((JSValue target, string key, JSValue value) property);

internal delegate JSValue JsonParserReceiverWithSource((JSValue target, string key, JSValue value, string? source) property);

internal class JSJsonParser(JsonParserReceiver? r) : System.Text.Json.Serialization.JsonConverter<JSValue>
{
    private static void AddJsonObjectProperty(JSObject target, string name, JSValue value)
    {
        if (uint.TryParse(name, out var index) && index != uint.MaxValue && index.ToString() == name)
        {
            target.FastAddValue(index, value, JSPropertyAttributes.EnumerableConfigurableValue);
            return;
        }

        target.FastAddValue(KeyStrings.GetOrCreate(name), value, JSPropertyAttributes.EnumerableConfigurableValue);
    }

    public static JSValue? Parse(string str, JsonParserReceiver? r) => JsonSerializer.Deserialize<JSValue>(str, new JsonSerializerOptions
    {
        Converters =
            {
                new JSJsonParser(r)
            }
    });

    /// <summary>
    /// Parse with source text tracking for the reviver context (ES2026 §4.7).
    /// </summary>
    public static JSValue? ParseWithSource(string str, JsonParserReceiverWithSource r)
    {
        var parser = new JSJsonParser(null) { reviverWithSource = r };
        return JsonSerializer.Deserialize<JSValue>(str, new JsonSerializerOptions
        {
            Converters = { parser }
        });
    }

    private JsonParserReceiverWithSource? reviverWithSource;

    public override JSValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.None => throw new NotSupportedException($"Unable to read JSON Data"),
            JsonTokenType.StartObject => ReadObject(ref reader, options),
            JsonTokenType.StartArray => ReadArray(ref reader, options),
            JsonTokenType.String => new JSString(reader.GetString()!),
            JsonTokenType.Number => new JSNumber(reader.GetDouble()),
            JsonTokenType.True => JSBoolean.True,
            JsonTokenType.False => JSBoolean.False,
            JsonTokenType.Null => JSNull.Value,
            _ => throw new NotSupportedException($"Unexpected JSON {reader.TokenType} at {reader.TokenStartIndex}"),
        };
    }

    /// <summary>
    /// Extracts the raw source text for a primitive token from the reader.
    /// </summary>
    private static string GetSourceText(ref Utf8JsonReader reader)
    {
        if (reader.HasValueSequence)
        {
            var sequence = reader.ValueSequence;
            return Encoding.UTF8.GetString(sequence.FirstSpan);
        }
        
        return Encoding.UTF8.GetString(reader.ValueSpan);
    }

    /// <summary>
    /// Reads a value and returns both the value and its source text for
    /// the reviver context.
    /// </summary>
    private (JSValue value, string? source) ReadWithSource(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.StartObject:
                return (ReadObject(ref reader, options), null);

            case JsonTokenType.StartArray:
                return (ReadArray(ref reader, options), null);

            case JsonTokenType.String:
                // Source includes the surrounding quotes
                var strSource = "\"" + GetSourceText(ref reader) + "\"";
                return (new JSString(reader.GetString()!), strSource);

            case JsonTokenType.Number:
                var numSource = GetSourceText(ref reader);
                return (new JSNumber(reader.GetDouble()), numSource);

            case JsonTokenType.True:
                return (JSBoolean.True, "true");

            case JsonTokenType.False:
                return (JSBoolean.False, "false");

            case JsonTokenType.Null:
                return (JSNull.Value, "null");

            default:
                throw new NotSupportedException($"Unexpected JSON {reader.TokenType} at {reader.TokenStartIndex}");
        }
    }

    private JSValue ReadArray(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        var j = new JSArray();
        // read properties...
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            if (reviverWithSource != null)
            {
                var (value, source) = ReadWithSource(ref reader, options);
                value = reviverWithSource((j, j.Length.ToString(), value, source));
                if (!value.IsUndefined)
                    j.Add(value);
            }
            else
            {
                var value = Read(ref reader, typeof(JSValue), options);
                value = r?.Invoke((j, j.Length.ToString(), value)) ?? value;
                if (!value.IsUndefined)
                    j.Add(value);
            }
        }

        return j;
    }

    private JSValue ReadObject(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        var j = new JSObject();

        // read properties...
        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.EndObject:
                    return j;

                case JsonTokenType.PropertyName:
                    var name = reader.GetString()!;

                    if (!reader.Read())
                        throw new InvalidOperationException($"Unable to read JSON");

                    if (reviverWithSource != null)
                    {
                        var (value, source) = ReadWithSource(ref reader, options);
                        value = reviverWithSource((j, name, value, source));

                        if (value.IsUndefined)
                            continue;

                        AddJsonObjectProperty(j, name, value);
                    }
                    else
                    {
                        var value = Read(ref reader, typeof(JSValue), options);
                        value = r?.Invoke((j, name, value)) ?? value;
                        if (value.IsUndefined)
                            continue;

                        AddJsonObjectProperty(j, name, value);
                    }
                    break;

                default:
                    throw new InvalidOperationException($"Invalid token {reader.TokenType} at {reader.TokenStartIndex}");
            }
        }

        return j;
    }

    public override void Write(Utf8JsonWriter writer, JSValue value, JsonSerializerOptions options) => throw new NotImplementedException();
}

/// <summary>Broiler.JS's JSON.parse entry, gap and quoting, as copied.</summary>
internal static class JSJsonText
{
    /// <summary>JSON.parse's parse of its String, with its exception filter.</summary>
    internal static JSValue Parse(string jText)
    {
        JSValue? parsed;
        try
        {
            parsed = JSJsonParser.Parse(jText, null);
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or NotSupportedException)
        {
            throw new ArmError("SyntaxError", ex.Message);
        }

        parsed ??= JSNull.Value;
        return parsed;
    }

    /// <summary>Stringify's gap from a Number space.</summary>
    internal static string GapFromNumber(double space)
    {
        string indent = null!;
        {
            // step 6.a: space = min(10, ToIntegerOrInfinity(space)), the shared library's
            // (SJL-4.5); step 6.b: a gap shorter than one space yields the empty String
            // (compact output).
            var count = Math.Min(10, IntegerConversions.ToIntegerOrInfinity(space));
            if (count >= 1)
                indent = new string(' ', (int)count);
        }

        return indent ?? string.Empty;
    }

    /// <summary>Stringify's gap from a String space.</summary>
    internal static string GapFromString(string space)
    {
        string indent = null!;
        {
            // step 7: a gap longer than 10 code units is truncated to its first 10;
            // an empty gap leaves indent null so the output stays compact.
            var gap = space.ToString();
            if (gap.Length > 10)
                gap = gap.Substring(0, 10);
            if (gap.Length > 0)
                indent = gap;
        }

        return indent ?? string.Empty;
    }

    /// <summary>
    /// Adds double quote characters to the start and end of the given string and converts any
    /// invalid characters into escape sequences.
    /// </summary>
    /// <param name="input"> The string to quote. </param>
    /// <param name="result"> The StringBuilder to write the quoted string to. </param>
    internal static void QuoteString(string input, TextWriter result)
    {
        result.Write('\"');

        // Check if there are characters that need to be escaped.
        // These characters include '"', '\', any character with an ASCII value
        // less than 32, and any UTF-16 surrogate code unit. Surrogates route to
        // the slow path so a lone (unpaired) surrogate can be escaped as \uXXXX
        // per the well-formed JSON.stringify rules (ES2019); a well-formed pair is
        // still emitted verbatim there.
        bool containsUnsafeCharacters = false;
        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];

            if (c == '\\' || c == '\"' || c < 0x20 || char.IsSurrogate(c))
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
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
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
                        else if (char.IsHighSurrogate(c) && i + 1 < input.Length && char.IsLowSurrogate(input[i + 1]))
                        {
                            // A well-formed surrogate pair encodes a single code point;
                            // emit both code units verbatim.
                            result.Write(c);
                            result.Write(input[i + 1]);
                            i++;
                        }
                        else if (char.IsSurrogate(c))
                        {
                            // Lone (unpaired) surrogate: escape as \uXXXX so the output is
                            // well-formed and round-trips through JSON.parse.
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
