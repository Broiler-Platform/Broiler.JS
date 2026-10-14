// REFERENCE ARM V: a test-only copy of Broiler.VM's implementation before the cut-over, for the
// reference-arm comparison of bundle sjl-5-001 (docs/evidence/sjl-5-001/reference-arms.md). It is
// never referenced outside this test project, and it is retired at SJL-16.
//
// Copied from Broiler.VM at 1f253ff (SJL-4's merge, the commit the area verdict's correspondence reads):
//   src/Broiler.VM.Profile.JavaScript/JsRealm.Json.cs
//     blob f23d40585f9e81b2c31d36331cd0ec7fc342ff40: JsonMaximumDepth (:63), JsonParseEntry (:217-249), JsonSkipSpace
//     (:254-267), JsonParseValue (:272-285), JsonValueStart (:295-333), JsonParseUnrecorded (:338-384),
//     JsonParseObject (:389-451), JsonParseArray (:456-491), JsonParseString (:496-584), JsonParseHex (:594-617),
//     JsonHexDigit (:622-628), JsonParseNumber (:633-698), JsonParseKeyword (:703-712), JsonSyntax and
//     JsonUnexpected (:717-726), JsonApplySpace (:934-962), JsonQuote (:1231-1287), JsonIsLoneSurrogate
//     (:1292-1307) and JsonRecord (:1315-1336)
//
// Changes from the source, none of which changes a result or a charge:
//   - the namespace; the members sit in a class JsRealmJson of their own instead of the partial class JsRealm;
//     private members are internal;
//   - the per-member assurance annotations, the generated assurance header and the SPDX lines removed;
//   - JsEngine is the arm's (JsRealmText.cs); JsValue, JsObject and JsArray are the plain values below, which
//     hold what the engine's would: ObjectPrototype is null and NewArray() a new JsArray. JsObject.DefineOrdinary
//     keeps a key's first position and its last value, as the engine's ordinary define does;
//   - JsonParseEntry takes the text's ToString where the source converts its first argument, and it never
//     records: the comparison passes no reviver, so `recording` is false and the reviver's lines after the parse
//     (the holder and JsonInternalize) are not copied;
//   - JsonApplySpace takes no JsEngine and begins at its Number and String tests: its first lines unwrap a
//     Number or String object, which the comparison never passes; JsonWriter is the one-field state below.

using System.Collections.Generic;
using System.Globalization;
using Broiler.ECMAScript.Numbers;

namespace Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.Text.V;

/// <summary>A JSON value as the arm builds it: what a JsValue of the engine would hold.</summary>
internal readonly struct JsValue
{
    private JsValue(object? payload, bool isNull) => (Payload, IsNull) = (payload, isNull);

    public object? Payload { get; }

    public bool IsNull { get; }

    public static JsValue True => new(true, false);

    public static JsValue False => new(false, false);

    public static JsValue Null => new(null, true);

    public bool IsString => Payload is string;

    public bool IsNumber => Payload is double;

    public bool IsObject => Payload is JsObject or JsArray;

    public static JsValue String(string text) => new(text, false);

    public static JsValue Number(double value) => new(value, false);

    public static JsValue Object(JsObject value) => new(value, false);

    public static JsValue Object(JsArray value) => new(value, false);

    public string AsString() => (string)Payload!;

    public double AsNumber() => (double)Payload!;
}

/// <summary>An ordinary object's own data properties, in order.</summary>
internal sealed class JsObject(object? prototype)
{
    public object? Prototype { get; } = prototype;

    public List<KeyValuePair<string, JsValue>> Members { get; } = [];

    public void DefineOrdinary(string key, JsValue value)
    {
        var at = Members.FindIndex(m => m.Key == key);
        if (at < 0)
            Members.Add(new KeyValuePair<string, JsValue>(key, value));
        else
            Members[at] = new KeyValuePair<string, JsValue>(key, value);
    }
}

/// <summary>An Array's elements.</summary>
internal sealed class JsArray
{
    public List<JsValue> Elements { get; } = [];

    public void Push(JsValue value) => Elements.Add(value);
}

/// <summary>Broiler.VM's JSON reader, quoting and gap, as copied.</summary>
internal sealed class JsRealmJson
{
    private static object? ObjectPrototype => null;

    private static JsArray NewArray() => new();

    /// <summary>The gap JsonApplySpace writes.</summary>
    internal sealed class JsonWriter
    {
        internal string Gap { get; set; } = string.Empty;
    }

    /// <summary>
    /// How deep either direction may nest before it stops rather than exhausting the CLR stack.
    /// </summary>
    /// <remarks>
    /// Both directions are recursive over a structure the guest chose the depth of, and the CLR has
    /// no recoverable stack overflow. A counted ceiling turns "the process dies" into "a RangeError
    /// is thrown", which is a difference the host can act on.
    /// </remarks>
    internal const int JsonMaximumDepth = 512;

    /// <summary><c>JSON.parse</c>.</summary>
    internal JsValue JsonParseEntry(JsEngine engine, string text)
    {
        var at = 0;

        // THE PARSE IS RECORDED ONLY WHEN THERE IS A REVIVER TO HAND IT TO, which is the JSON Parse
        // Record the specification builds for one: the source text of every primitive value and the
        // record of every element and member, so a reviver is told what was written as well as what
        // it was read as (JSP-7, JSC-239). Reading the reviver first is unobservable: it is an
        // argument, and only its callability is asked.
        var recording = false;

        JsonSkipSpace(engine, text, ref at);
        var parsed = JsonParseValue(engine, text, ref at, 0, recording, out var snapshot);
        JsonSkipSpace(engine, text, ref at);

        if (at != text.Length)
        {
            throw JsonUnexpected(engine, text, at);
        }

        _ = snapshot;
        return parsed;
    }

    /// <summary>Consumes the four characters JSON calls whitespace, and no others.</summary>
    internal static void JsonSkipSpace(JsEngine engine, string text, ref int at)
    {
        var start = at;

        while (at < text.Length && text[at] is ' ' or '\t' or '\n' or '\r')
        {
            at++;
        }

        if (at > start)
        {
            engine.Charge((ulong)(at - start));
        }
    }

    /// <summary>Reads one JSON value, whatever kind it is.</summary>
    internal JsValue JsonParseValue(JsEngine engine, string text, ref int at, int depth, bool recording, out JsonRecord? record)
    {
        var value = JsonParseUnrecorded(engine, text, ref at, depth, recording, out record);

        // A PRIMITIVE'S RECORD IS ITS TEXT, exactly as written - the escapes of a string, the
        // exponent of a number, the sign of `-0` - with nothing around it. A container records its
        // members while it is read.
        if (recording && record is null)
        {
            record = new JsonRecord(value) { Source = text[JsonValueStart(text, at, value)..at] };
        }

        return value;
    }

    /// <summary>Where the primitive that ended at <paramref name="end"/> began.</summary>
    /// <remarks>
    /// A primitive is one token, and the reader stopped on its last character; walking back over the
    /// characters a token of its kind can hold finds its first. A string walks back to the quote
    /// that opened it, past every escaped quote inside.
    /// </remarks>
    internal static int JsonValueStart(string text, int end, JsValue value)
    {
        if (value.IsString)
        {
            var start = end - 2;

            while (start > 0)
            {
                if (text[start] == '"')
                {
                    var slashes = 0;

                    while (start - 1 - slashes >= 0 && text[start - 1 - slashes] == '\\')
                    {
                        slashes++;
                    }

                    if (slashes % 2 == 0)
                    {
                        break;
                    }
                }

                start--;
            }

            return start;
        }

        var first = end - 1;

        while (first > 0 && text[first - 1] is (>= '0' and <= '9') or '.' or 'e' or 'E' or '+' or '-' or
            (>= 'a' and <= 'z'))
        {
            first--;
        }

        return first;
    }

    /// <summary>Reads one JSON value, whatever kind it is.</summary>
    internal JsValue JsonParseUnrecorded(JsEngine engine, string text, ref int at, int depth, bool recording, out JsonRecord? record)
    {
        engine.Charge(1);
        record = null;

        if (depth > JsonMaximumDepth)
        {
            throw engine.Error("RangeError", "Maximum JSON nesting depth exceeded");
        }

        if (at >= text.Length)
        {
            throw JsonUnexpected(engine, text, at);
        }

        switch (text[at])
        {
            case '{':
                return JsonParseObject(engine, text, ref at, depth, recording, out record);

            case '[':
                return JsonParseArray(engine, text, ref at, depth, recording, out record);

            case '"':
                return JsValue.String(JsonParseString(engine, text, ref at));

            case 't':
                return JsonParseKeyword(engine, text, ref at, "true", JsValue.True);

            case 'f':
                return JsonParseKeyword(engine, text, ref at, "false", JsValue.False);

            case 'n':
                return JsonParseKeyword(engine, text, ref at, "null", JsValue.Null);

            case '-':
                return JsonParseNumber(engine, text, ref at);

            default:
                if (text[at] is >= '0' and <= '9')
                {
                    return JsonParseNumber(engine, text, ref at);
                }

                throw JsonUnexpected(engine, text, at);
        }
    }

    /// <summary>Reads an object literal into an ordinary object.</summary>
    internal JsValue JsonParseObject(JsEngine engine, string text, ref int at, int depth, bool recording, out JsonRecord? record)
    {
        at++;
        var target = new JsObject(ObjectPrototype);
        record = recording ? new JsonRecord(JsValue.Object(target)) { Entries = [] } : null;
        JsonSkipSpace(engine, text, ref at);

        if (at < text.Length && text[at] == '}')
        {
            at++;
            return JsValue.Object(target);
        }

        while (true)
        {
            JsonSkipSpace(engine, text, ref at);

            if (at >= text.Length || text[at] != '"')
            {
                throw JsonUnexpected(engine, text, at);
            }

            var key = JsonParseString(engine, text, ref at);
            JsonSkipSpace(engine, text, ref at);

            if (at >= text.Length || text[at] != ':')
            {
                throw JsonUnexpected(engine, text, at);
            }

            at++;
            engine.Charge(1);
            JsonSkipSpace(engine, text, ref at);
            var value = JsonParseValue(engine, text, ref at, depth + 1, recording, out var member);

            // An unchecked define, which is what CreateDataProperty is here: a key of "__proto__"
            // becomes an own data property rather than reaching Object.prototype's setter, and a
            // repeated key replaces the earlier one in the earlier one's position. Its record is the
            // last one too, which is the member the specification's record names.
            target.DefineOrdinary(key, value);

            if (record?.Entries is { } entries)
            {
                entries[key] = member!;
            }
            JsonSkipSpace(engine, text, ref at);

            if (at < text.Length && text[at] == ',')
            {
                at++;
                engine.Charge(1);
                continue;
            }

            if (at < text.Length && text[at] == '}')
            {
                at++;
                return JsValue.Object(target);
            }

            throw JsonUnexpected(engine, text, at);
        }
    }

    /// <summary>Reads an array literal into an Array.</summary>
    internal JsValue JsonParseArray(JsEngine engine, string text, ref int at, int depth, bool recording, out JsonRecord? record)
    {
        at++;
        var target = NewArray();
        record = recording ? new JsonRecord(JsValue.Object(target)) { Elements = [] } : null;
        JsonSkipSpace(engine, text, ref at);

        if (at < text.Length && text[at] == ']')
        {
            at++;
            return JsValue.Object(target);
        }

        while (true)
        {
            JsonSkipSpace(engine, text, ref at);
            target.Push(JsonParseValue(engine, text, ref at, depth + 1, recording, out var element));
            record?.Elements!.Add(element!);
            JsonSkipSpace(engine, text, ref at);

            if (at < text.Length && text[at] == ',')
            {
                at++;
                engine.Charge(1);
                continue;
            }

            if (at < text.Length && text[at] == ']')
            {
                at++;
                return JsValue.Object(target);
            }

            throw JsonUnexpected(engine, text, at);
        }
    }

    /// <summary>Reads a string literal, resolving the escapes JSON has.</summary>
    internal static string JsonParseString(JsEngine engine, string text, ref int at)
    {
        var start = at;
        at++;
        var built = new System.Text.StringBuilder();

        while (true)
        {
            if (at >= text.Length)
            {
                throw JsonSyntax(engine, "Unterminated string in JSON", at);
            }

            var character = text[at];

            if (character == '"')
            {
                at++;
                engine.Charge((ulong)(at - start));
                return built.ToString();
            }

            // A raw control character inside a string is the single most common way a hand-built
            // encoder produces something no conforming reader will take back.
            if (character < ' ')
            {
                throw JsonSyntax(engine, "Bad control character in JSON", at);
            }

            if (character != '\\')
            {
                built.Append(character);
                at++;
                continue;
            }

            at++;

            if (at >= text.Length)
            {
                throw JsonSyntax(engine, "Unexpected end of JSON input", at);
            }

            var escape = text[at];
            at++;

            switch (escape)
            {
                case '"':
                    built.Append('"');
                    break;

                case '\\':
                    built.Append('\\');
                    break;

                case '/':
                    built.Append('/');
                    break;

                case 'b':
                    built.Append('\b');
                    break;

                case 'f':
                    built.Append('\f');
                    break;

                case 'n':
                    built.Append('\n');
                    break;

                case 'r':
                    built.Append('\r');
                    break;

                case 't':
                    built.Append('\t');
                    break;

                case 'u':
                    built.Append(JsonParseHex(engine, text, ref at));
                    break;

                default:
                    throw JsonSyntax(engine, "Bad escaped character in JSON", at - 1);
            }
        }
    }

    /// <summary>Reads the four hexadecimal digits of a <c>\u</c> escape.</summary>
    /// <remarks>
    /// The result is one UTF-16 code unit and is not validated as a scalar value: <c>"\ud800"</c> is
    /// a well-formed JSON string holding a lone surrogate, and a String in this language is a
    /// sequence of code units rather than of scalars, so rejecting it would be inventing a rule.
    /// </remarks>
    internal static char JsonParseHex(JsEngine engine, string text, ref int at)
    {
        if (at + 4 > text.Length)
        {
            throw JsonSyntax(engine, "Bad Unicode escape in JSON", at);
        }

        var unit = 0;

        for (var step = 0; step < 4; step++)
        {
            var digit = JsonHexDigit(text[at + step]);

            if (digit < 0)
            {
                throw JsonSyntax(engine, "Bad Unicode escape in JSON", at + step);
            }

            unit = (unit * 16) + digit;
        }

        at += 4;
        return (char)unit;
    }

    /// <summary>The value of one hexadecimal digit, or a negative number when it is not one.</summary>
    internal static int JsonHexDigit(char character) => character switch
    {
        >= '0' and <= '9' => character - '0',
        >= 'a' and <= 'f' => character - 'a' + 10,
        >= 'A' and <= 'F' => character - 'A' + 10,
        _ => -1,
    };

    /// <summary>Reads a number literal in the JSON grammar, which is narrower than the language's.</summary>
    internal static JsValue JsonParseNumber(JsEngine engine, string text, ref int at)
    {
        var start = at;

        if (at < text.Length && text[at] == '-')
        {
            at++;
        }

        if (at >= text.Length || text[at] is < '0' or > '9')
        {
            throw JsonUnexpected(engine, text, at);
        }

        // A leading zero admits no further digits: "01" is two tokens in JSON and therefore an
        // error, where the language would read it as an octal-looking decimal.
        if (text[at] == '0')
        {
            at++;
        }
        else
        {
            while (at < text.Length && text[at] is >= '0' and <= '9')
            {
                at++;
            }
        }

        if (at < text.Length && text[at] == '.')
        {
            at++;

            if (at >= text.Length || text[at] is < '0' or > '9')
            {
                throw JsonUnexpected(engine, text, at);
            }

            while (at < text.Length && text[at] is >= '0' and <= '9')
            {
                at++;
            }
        }

        if (at < text.Length && text[at] is 'e' or 'E')
        {
            at++;

            if (at < text.Length && text[at] is '+' or '-')
            {
                at++;
            }

            if (at >= text.Length || text[at] is < '0' or > '9')
            {
                throw JsonUnexpected(engine, text, at);
            }

            while (at < text.Length && text[at] is >= '0' and <= '9')
            {
                at++;
            }
        }

        // The library charges the number's length, which is never empty here, before reading it.
        return JsValue.Number(NumberParsing.StringToNumber(System.MemoryExtensions.AsSpan(text, start, at - start), engine.WorkCharge));
    }

    /// <summary>Reads one of the three bare words.</summary>
    internal static JsValue JsonParseKeyword(JsEngine engine, string text, ref int at, string word, JsValue value)
    {
        if (at + word.Length > text.Length || string.CompareOrdinal(text, at, word, 0, word.Length) != 0)
        {
            throw JsonUnexpected(engine, text, at);
        }

        at += word.Length;
        return value;
    }

    /// <summary>A SyntaxError naming where in the text the reader stopped.</summary>
    internal static JsThrow JsonSyntax(JsEngine engine, string message, int at) =>
        engine.Error("SyntaxError", message + " at position " + at.ToString(CultureInfo.InvariantCulture));

    /// <summary>The error for a character the grammar has no place for.</summary>
    internal static JsThrow JsonUnexpected(JsEngine engine, string text, int at) =>
        at >= text.Length
            ? JsonSyntax(engine, "Unexpected end of JSON input", at)
            : JsonSyntax(engine, "Unexpected token '" + text[at] + "' in JSON", at);

    /// <summary>The specification's <c>QuoteJSONString</c>.</summary>
    /// <remarks>
    /// Every code unit below <c>0x20</c> that has no short escape, and every surrogate without its
    /// partner, becomes a <c>\u</c> escape. The lone-surrogate case is what makes the output
    /// well-formed UTF-16 text that any reader can take back: a bare <c>\ud800</c> in the output
    /// cannot be encoded as UTF-8 and would make the document unreadable to a consumer that decodes
    /// before it parses.
    /// </remarks>
    internal static string JsonQuote(JsEngine engine, string value)
    {
        engine.Charge((ulong)value.Length + 1);
        var text = new System.Text.StringBuilder(value.Length + 2);
        text.Append('"');

        for (var at = 0; at < value.Length; at++)
        {
            var character = value[at];

            switch (character)
            {
                case '\\':
                    text.Append("\\\\");
                    break;

                case '"':
                    text.Append("\\\"");
                    break;

                case '\b':
                    text.Append("\\b");
                    break;

                case '\f':
                    text.Append("\\f");
                    break;

                case '\n':
                    text.Append("\\n");
                    break;

                case '\r':
                    text.Append("\\r");
                    break;

                case '\t':
                    text.Append("\\t");
                    break;

                default:
                    if (character < ' ' || JsonIsLoneSurrogate(value, at))
                    {
                        text.Append("\\u").Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        text.Append(character);
                    }

                    break;
            }
        }

        text.Append('"');
        return text.ToString();
    }

    /// <summary>Whether the code unit at <paramref name="at"/> is a surrogate without its partner.</summary>
    internal static bool JsonIsLoneSurrogate(string value, int at)
    {
        var character = value[at];

        if (char.IsHighSurrogate(character))
        {
            return at + 1 >= value.Length || !char.IsLowSurrogate(value[at + 1]);
        }

        if (char.IsLowSurrogate(character))
        {
            return at == 0 || !char.IsHighSurrogate(value[at - 1]);
        }

        return false;
    }

    /// <summary>Reads the space argument into one level of indentation.</summary>
    internal static void JsonApplySpace(JsonWriter state, JsValue space)
    {
        if (space.IsNumber)
        {
            var count = (int)System.Math.Min(
                10, System.Math.Max(0, IntegerConversions.ToIntegerOrInfinity(space.AsNumber())));

            state.Gap = count > 0 ? new string(' ', count) : string.Empty;
            return;
        }

        if (space.IsString)
        {
            var text = space.AsString();
            state.Gap = text.Length <= 10 ? text : text[..10];
        }
    }

    /// <summary>
    /// The specification's JSON Parse Record: a value as the parse produced it, the text a primitive
    /// was read from, and the records of an Array's elements or an object's members.
    /// </summary>
    internal sealed class JsonRecord(JsValue value)
    {
        /// <summary>The value the parse produced, which a reviver may since have replaced.</summary>
        internal JsValue Value { get; } = value;

        /// <summary>The text a primitive was read from, exactly as written.</summary>
        internal string? Source { get; init; }

        /// <summary>An Array's elements' records, in order.</summary>
        internal List<JsonRecord>? Elements { get; init; }

        /// <summary>An object's members' records, by key, the last of a repeated key kept.</summary>
        internal Dictionary<string, JsonRecord>? Entries { get; init; }
    }
}
