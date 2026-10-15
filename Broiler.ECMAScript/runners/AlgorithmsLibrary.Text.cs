using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Broiler.ECMAScript.Conformance;
using Broiler.ECMAScript.Numbers;
using Broiler.ECMAScript.Text;

namespace Broiler.ECMAScript.Runners;

/// <summary>
/// The text area (corpus revision 10): each operation as an engine's adapter calls the library, with
/// the script's own reading of the result where a vector's script shows it through a built-in.
/// </summary>
/// <remarks>
/// <para>
/// What a vector's script does around the shared step is written here once more, as each engine's
/// adapter does it: the fixed patterns of the substitution vectors and their match, the empty
/// pattern's walk through <c>replace</c>, <c>JSON.stringify</c>'s driver over a parsed document,
/// and the arrays the index vectors slice. None of it is a grammar, an escape or a codec; every such
/// step is the library's.
/// </para>
/// <para>
/// <b>The engine-level vectors are not answered</b>: those whose expected outcome is an engine's
/// conversion order, option reading, typed-array validation or raw-JSON brand, and those that pin
/// an engine's string ceiling, which is a policy each engine passes.
/// </para>
/// </remarks>
internal sealed partial class AlgorithmsLibrary
{
    /// <summary>Whether an operation of the text area is the engine's, not the library's, so that its vectors are engine-level only.</summary>
    internal static bool IsEngineLevelText(string operation) => operation switch
    {
        "GetOptionsObject refuses a non-object" or "GetOptionsObject refuses null" or "an alphabet that is not a name"
            or "a last-chunk handling that is not a name" or "an alphabet that is not a String value"
            or "toBase64's GetOptionsObject refuses a non-object" or "fromBase64 refuses a non-String"
            or "fromHex refuses a non-String" or "toBase64 refuses a detached array" or "toHex refuses a detached array"
            or "setFromBase64 refuses a detached array" or "setFromHex refuses a detached array"
            or "ValidateUint8Array refuses another typed array" => true,
        "replaceAll converts the replacement first" or "GetSubstitution converts a named capture with ToString"
            or "replace with a search value whose Symbol.replace is undefined"
            or "replace with a search value whose Symbol.replace is null" => true,
        "StringPaddingBuiltinsImpl converts the filler first" or "StringPad length at 16777216" or "StringPad length at 16777217" => true,
        "rawJSON's text is written as it is" or "rawJSON inside an object" or "rawJSON's property is enumerable"
            or "isRawJSON of a raw JSON object" or "isRawJSON of a look-alike" or "a look-alike is an ordinary object"
            or "rawJSON refuses an object" or "rawJSON refuses an array" or "rawJSON refuses leading white space"
            or "rawJSON refuses trailing white space" or "rawJSON refuses an empty text" or "rawJSON's object is frozen"
            or "rawJSON's object has a null prototype" => true,
        _ => false,
    };

    private static string TextArea(string operation, IReadOnlyList<TypedValue> inputs)
    {
        if (IsEngineLevelText(operation))
            return EmptyLibrary.NotImplemented;

        switch (operation)
        {
            case "TrimString start": return Text(new string(NumberParsing.TrimStrWhiteSpace(inputs[0].Chars, TrimEnds.Start)));
            case "TrimString end": return Text(new string(NumberParsing.TrimStrWhiteSpace(inputs[0].Chars, TrimEnds.End)));
            case "TrimString start+end": return Text(new string(NumberParsing.TrimStrWhiteSpace(inputs[0].Chars, TrimEnds.Both)));
            case "IsStringWellFormedUnicode": return "value: " + TypedValue.RenderBoolean(Utf16.IsWellFormed(inputs[0].Chars));
            case "toWellFormed": return Text(Utf16.ToWellFormed(inputs[0].Chars));
            case "CodePointAt code point":
            {
                var at = (int)inputs[1].Integer;
                return at < inputs[0].Chars.Length ? Number(Utf16.CodePointAt(inputs[0].Chars, at).CodePoint) : "value: undefined";
            }

            case "StringToCodePoints joined by |": return Text(CodePointsJoined(inputs[0].Chars));
            case "AdvanceStringIndex walk":
            case "AdvanceStringIndex walk unicode": return Text(EmptyPatternWalk(inputs[0].Chars, inputs[1].Boolean));
            case "StringPad start": return Pad(inputs[0].Chars, (long)inputs[1].Integer, inputs[2].Chars, PadPlacement.Start);
            case "StringPad end": return Pad(inputs[0].Chars, (long)inputs[1].Integer, inputs[2].Chars, PadPlacement.End);
            case "StringPad with an empty filler": return Pad("x", (long)inputs[0].Integer, inputs[1].Chars, PadPlacement.End);
            case "relative index": return Text(Letters((int)inputs[0].Integer)[(int)RelativeIndex.Clamped(Relative(inputs[1]), (long)inputs[0].Integer)..]);
            case "relative index for at":
                return RelativeIndex.TryElement(Relative(inputs[1]), (long)inputs[0].Integer, out var element)
                    ? Text(Letters((int)inputs[0].Integer)[(int)element].ToString())
                    : "value: undefined";
            case "relative index for slice of a list": return Text(ListFrom((int)RelativeIndex.Clamped(Relative(inputs[1]), (long)inputs[0].Integer), (int)inputs[0].Integer));
            case "lastIndexOf with fromIndex": return Number(LastIndexOfOne([1, 2, 1], Relative(inputs[0])));
            case "GetSubstitution string": return Text(Substitute(inputs[0].Chars, inputs[1].Chars, Pattern.Text));
            case "GetSubstitution regexp": return Text(Substitute(inputs[0].Chars, inputs[1].Chars, Pattern.Captures));
            case "GetSubstitution named": return Text(Substitute(inputs[0].Chars, inputs[1].Chars, Pattern.Named));
            case "Encode encodeURI": return UriOutcome(UriCodec.Encode(inputs[0].Chars, UriCodec.ReservedAndNumberSign, out var encoded, out _), encoded);
            case "Encode encodeURIComponent": return UriOutcome(UriCodec.Encode(inputs[0].Chars, string.Empty, out var component, out _), component);
            case "Decode decodeURI": return UriOutcome(UriCodec.Decode(inputs[0].Chars, UriCodec.ReservedAndNumberSign, out var decoded, out _), decoded);
            case "Decode decodeURIComponent": return UriOutcome(UriCodec.Decode(inputs[0].Chars, string.Empty, out var whole, out _), whole);
            case "escape": return Text(UriCodec.Escape(inputs[0].Chars));
            case "unescape": return Text(UriCodec.Unescape(inputs[0].Chars));
            case "FromHex": return FromHex(inputs[0].Chars);
            case "FromHex into a destination": return SetFromHex(inputs[0].Chars, (int)inputs[1].Integer);
            case "ToHex": return Text(Base64Codec.ToHex(Bytes(inputs[0].Chars)));
            case "QuoteJSONString": return Text(JsonText.Quote(inputs[0].Chars));
            case "JSON parse then serialize": return ParseThenSerialize(inputs[0].Chars, string.Empty);
            case "JSON parse a number":
            case "JSON parse a lone surrogate": return ParseOnly(inputs[0].Chars);
            case "JSON stringify with a gap": return ParseThenSerialize(inputs[0].Chars, GapOf(inputs[1].Chars));
            case "JSON stringify writes LF and no CR": return Number(Serialize(Parse("{\"a\":[1,{\"b\":2}]}"), GapOf(inputs[0].Chars), string.Empty).IndexOf('\r', StringComparison.Ordinal));
        }

        if (operation.StartsWith("JSON parse nested ", StringComparison.Ordinal))
        {
            var depth = (int)inputs[0].Integer;
            var open = inputs[1].Chars;
            var document = new StringBuilder();
            for (var at = 0; at < depth; at++)
                document.Append(open);
            document.Append('1').Append(open == "[" ? ']' : '}', depth);
            var parsed = TryParse(document.ToString(), out var value);
            return parsed is null ? Number(Serialize(value, string.Empty, string.Empty).Length) : parsed;
        }

        if (operation.StartsWith("FromBase64 ", StringComparison.Ordinal))
            return FromBase64(operation, inputs);

        if (operation.StartsWith("ToBase64 ", StringComparison.Ordinal))
        {
            var alphabet = operation.Contains("base64url", StringComparison.Ordinal) ? Base64Alphabet.Base64Url : Base64Alphabet.Base64;
            return Text(Base64Codec.ToBase64(Bytes(inputs[0].Chars), alphabet, operation.EndsWith("omitPadding", StringComparison.Ordinal)));
        }

        return DateArea(operation, inputs);
    }

    // ---------------------------------------------------------------------------------------------
    // UTF-16, padding and indices.

    /// <summary><c>[...s].join("|")</c>: the String iterator's code points.</summary>
    private static string CodePointsJoined(string text)
    {
        var joined = new StringBuilder();
        for (var at = 0; at < text.Length;)
        {
            var count = Utf16.CodePointAt(text, at).CodeUnitCount;
            if (at > 0)
                joined.Append('|');
            joined.Append(text, at, count);
            at += count;
        }

        return joined.ToString();
    }

    /// <summary><c>s.replace(/(?:)/g, "|")</c> or with <c>u</c>: a bar at every index the global walk reaches through AdvanceStringIndex.</summary>
    private static string EmptyPatternWalk(string text, bool unicode)
    {
        var walked = new StringBuilder();
        var copied = 0;
        for (long at = 0; at <= text.Length; at = Utf16.AdvanceStringIndex(text, at, unicode))
        {
            walked.Append(text, copied, (int)at - copied).Append('|');
            copied = (int)at;
        }

        return walked.Append(text, copied, text.Length - copied).ToString();
    }

    /// <summary><c>s.padStart(n, f)</c> or <c>padEnd</c>: StringPaddingBuiltinsImpl's early return, then the library, under no ceiling but the runtime's.</summary>
    private static string Pad(string text, long maxLength, string filler, PadPlacement placement)
    {
        if (maxLength <= text.Length)
            return Text(text);

        var status = StringPadding.Pad(text, maxLength, filler, placement, new LengthLimit(StringPaddingCeiling), out var padded);
        return status == ResultStatus.Completed ? Text(padded) : "error: RangeError";
    }

    /// <summary>A longest String no padding vector reaches, standing for an engine's policy.</summary>
    private const long StringPaddingCeiling = 1L << 24;

    /// <summary>A relative-index argument, ToIntegerOrInfinity'd.</summary>
    private static double Relative(TypedValue value) => value.Kind == TypedValueKind.Integer ? (double)value.Integer : ToIntegerOrInfinity(value);

    /// <summary>The String the index vectors slice: <c>"abcdefg"</c>, for length 7.</summary>
    private static string Letters(int length) => "abcdefghijklmnopqrstuvwxyz"[..length];

    /// <summary><c>[1, 2, ..., n].slice(from)</c> joined with commas, as the list vectors write it.</summary>
    private static string ListFrom(int from, int length)
    {
        var joined = new StringBuilder();
        for (var at = from; at < length; at++)
        {
            if (at > from)
                joined.Append(',');
            joined.Append((char)('1' + at));
        }

        return joined.ToString();
    }

    /// <summary><c>new Int8Array(list).lastIndexOf(1, fromIndex)</c>.</summary>
    private static double LastIndexOfOne(int[] list, double fromIndex)
    {
        for (var at = RelativeIndex.LastSearchStart(fromIndex, list.Length); at >= 0; at--)
        {
            if (list[at] == 1)
                return at;
        }

        return -1;
    }

    // ---------------------------------------------------------------------------------------------
    // Substitution templates: the vectors' subject is "xbdy" and their three patterns are fixed.

    private enum Pattern
    {
        /// <summary><c>"b"</c>, a String pattern: no captures, no named captures.</summary>
        Text,

        /// <summary><c>/(b)(c)?/</c>: two captures, the second undefined.</summary>
        Captures,

        /// <summary><c>/(?&lt;a&gt;b)(?&lt;b&gt;c)?/</c>: the same, named a and b.</summary>
        Named,
    }

    /// <summary><c>subject.replace(pattern, template)</c> for one of the three fixed patterns, the parts evaluated as an engine evaluates them.</summary>
    private static string Substitute(string subject, string template, Pattern pattern)
    {
        var position = subject.IndexOf('b', StringComparison.Ordinal);
        if (position < 0)
            return subject;

        const string Matched = "b";
        string?[] captures = pattern == Pattern.Text ? [] : ["b", null];
        var groups = pattern == Pattern.Named ? new Dictionary<string, string?>(StringComparer.Ordinal) { ["a"] = "b", ["b"] = null } : null;
        var replacement = new StringBuilder();
        foreach (var part in ReplacementTemplate.Parse(template, captures.Length, groups is not null))
        {
            switch (part.Kind)
            {
                case ReplacementPartKind.Literal: replacement.Append(template, part.Start, part.Length); break;
                case ReplacementPartKind.Dollar: replacement.Append('$'); break;
                case ReplacementPartKind.Match: replacement.Append(Matched); break;
                case ReplacementPartKind.Before: replacement.Append(subject, 0, position); break;
                case ReplacementPartKind.After:
                    var tail = Math.Min(position + Matched.Length, subject.Length);
                    replacement.Append(subject, tail, subject.Length - tail);
                    break;
                case ReplacementPartKind.Capture: replacement.Append(captures[part.CaptureIndex - 1]); break;
                case ReplacementPartKind.NamedCapture:
                    // Get on the groups object: an absent name reads undefined, which expands to nothing.
                    groups!.TryGetValue(template.Substring(part.Start, part.Length), out var named);
                    replacement.Append(named);
                    break;
            }
        }

        return subject[..position] + replacement + subject[(position + Matched.Length)..];
    }

    // ---------------------------------------------------------------------------------------------
    // URI, base64 and hex.

    private static string UriOutcome(ResultStatus status, string result) => status == ResultStatus.Completed ? Text(result) : "error: URIError";

    /// <summary>A vector's bytes, written as decimal numbers joined by commas.</summary>
    private static byte[] Bytes(string list)
    {
        if (list.Length == 0)
            return [];

        var items = list.Split(',');
        var bytes = new byte[items.Length];
        for (var at = 0; at < items.Length; at++)
            bytes[at] = byte.Parse(items[at], NumberStyles.None, NumberFormatInfo.InvariantInfo);

        return bytes;
    }

    private static string Joined(ReadOnlySpan<byte> bytes)
    {
        var joined = new StringBuilder();
        for (var at = 0; at < bytes.Length; at++)
        {
            if (at > 0)
                joined.Append(',');
            joined.Append(bytes[at].ToString(NumberFormatInfo.InvariantInfo));
        }

        return joined.ToString();
    }

    /// <summary><c>Uint8Array.fromBase64</c> joined with commas, or <c>setFromBase64</c> into a zeroed destination as the vector's script reports it.</summary>
    private static string FromBase64(string operation, IReadOnlyList<TypedValue> inputs)
    {
        var alphabet = operation.StartsWith("FromBase64 base64url ", StringComparison.Ordinal) ? Base64Alphabet.Base64Url : Base64Alphabet.Base64;
        var handling = operation.Contains(" stop-before-partial", StringComparison.Ordinal) ? LastChunkHandling.StopBeforePartial
            : operation.Contains(" strict", StringComparison.Ordinal) ? LastChunkHandling.Strict
            : LastChunkHandling.Loose;
        var text = inputs[0].Chars;
        if (!operation.EndsWith(" into a destination", StringComparison.Ordinal))
        {
            var buffer = new byte[Base64Codec.DecodedLengthBound(text.Length)];
            var status = Base64Codec.FromBase64(text, alphabet, handling, buffer, out _, out var written, out _);
            return status == ResultStatus.Completed ? Text(Joined(buffer.AsSpan(0, written))) : "error: SyntaxError";
        }

        var destination = new byte[(int)inputs[1].Integer];
        var into = Base64Codec.FromBase64(text, alphabet, handling, destination, out var read, out var count, out _);
        return Text(Settled(into, read, count, destination));
    }

    private static string FromHex(string text)
    {
        var buffer = new byte[text.Length / 2];
        var status = Base64Codec.FromHex(text, buffer, out _, out var written, out _);
        return status == ResultStatus.Completed ? Text(Joined(buffer.AsSpan(0, written))) : "error: SyntaxError";
    }

    private static string SetFromHex(string text, int length)
    {
        var destination = new byte[length];
        var status = Base64Codec.FromHex(text, destination, out var read, out var written, out _);
        return Text(Settled(status, read, written, destination));
    }

    /// <summary>What the destination vectors' scripts return: <c>read/written/bytes</c>, or <c>SyntaxError/bytes</c> with the bytes written before the error.</summary>
    private static string Settled(ResultStatus status, int read, int written, byte[] destination)
    {
        return status == ResultStatus.Completed
            ? read.ToString(NumberFormatInfo.InvariantInfo) + "/" + written.ToString(NumberFormatInfo.InvariantInfo) + "/" + Joined(destination)
            : "SyntaxError/" + Joined(destination);
    }

    // ---------------------------------------------------------------------------------------------
    // JSON: the reader builds a plain tree, and JSON.stringify's driver writes it back. No vector's
    // document has a key that is an array index, so a member's order is its first appearance.

    private sealed class Node
    {
        internal JsonToken Kind { get; init; }

        internal string String { get; init; } = string.Empty;

        internal double Number { get; init; }

        internal List<Node>? Elements { get; init; }

        internal List<KeyValuePair<string, Node>>? Members { get; init; }
    }

    private static string ParseOnly(string text)
    {
        var failed = TryParse(text, out var value);
        if (failed is not null)
            return failed;

        return value.Kind switch
        {
            JsonToken.Number => Number(value.Number),
            JsonToken.String => Text(value.String),
            _ => throw new NotSupportedException("No parse vector answers a " + value.Kind + "."),
        };
    }

    private static string ParseThenSerialize(string text, string gap)
    {
        var failed = TryParse(text, out var value);
        return failed ?? Text(Serialize(value, gap, string.Empty));
    }

    private static Node Parse(string text) => TryParse(text, out var value) is null ? value : throw new FormatException(text);

    /// <summary>Reads a document under no depth limit a vector reaches; answers the error outcome, or null.</summary>
    private static string? TryParse(string text, out Node value)
    {
        var reader = new JsonReader(text, new DepthLimit(100_000));
        var stack = new Stack<(Node Container, string? Name)>();
        string? name = null;
        value = new Node();
        while (true)
        {
            if (reader.Read() != ResultStatus.Completed)
                return reader.Failure.Status == ResultStatus.LimitExceeded ? "error: RangeError" : "error: SyntaxError";

            Node? completed = null;
            switch (reader.Token)
            {
                case JsonToken.BeginObject:
                    stack.Push((new Node { Kind = JsonToken.BeginObject, Members = [] }, name));
                    continue;
                case JsonToken.BeginArray:
                    stack.Push((new Node { Kind = JsonToken.BeginArray, Elements = [] }, name));
                    continue;
                case JsonToken.PropertyName:
                    name = reader.StringValue;
                    continue;
                case JsonToken.EndObject:
                case JsonToken.EndArray:
                    (completed, name) = stack.Pop();
                    break;
                case JsonToken.String:
                    completed = new Node { Kind = JsonToken.String, String = reader.StringValue };
                    break;
                case JsonToken.Number:
                    completed = new Node { Kind = JsonToken.Number, Number = reader.NumberValue };
                    break;
                case JsonToken.True:
                case JsonToken.False:
                case JsonToken.Null:
                    completed = new Node { Kind = reader.Token };
                    break;
                case JsonToken.EndOfText:
                    return null;
            }

            if (stack.Count == 0)
            {
                value = completed!;
                continue;
            }

            var container = stack.Peek().Container;
            if (container.Elements is { } elements)
            {
                elements.Add(completed!);
            }
            else
            {
                // CreateDataProperty: a repeated key keeps its first position and takes the last value.
                var members = container.Members!;
                var existing = members.FindIndex(m => m.Key == name);
                if (existing < 0)
                    members.Add(new KeyValuePair<string, Node>(name!, completed!));
                else
                    members[existing] = new KeyValuePair<string, Node>(name!, completed!);
            }
        }
    }

    /// <summary>SerializeJSONProperty, SerializeJSONObject and SerializeJSONArray over a parsed tree, writing line feeds.</summary>
    private static string Serialize(Node value, string gap, string indent)
    {
        switch (value.Kind)
        {
            case JsonToken.True: return "true";
            case JsonToken.False: return "false";
            case JsonToken.Null: return "null";
            case JsonToken.String: return JsonText.Quote(value.String);
            case JsonToken.Number: return double.IsFinite(value.Number) ? NumberText.ToString(value.Number) : "null";
        }

        var inner = indent + gap;
        var parts = new List<string>();
        if (value.Elements is { } elements)
        {
            foreach (var element in elements)
                parts.Add(Serialize(element, gap, inner));
        }
        else
        {
            foreach (var member in value.Members!)
                parts.Add(JsonText.Quote(member.Key) + ":" + (gap.Length == 0 ? string.Empty : " ") + Serialize(member.Value, gap, inner));
        }

        var (open, close) = value.Elements is not null ? ("[", "]") : ("{", "}");
        if (parts.Count == 0)
            return open + close;

        return gap.Length == 0
            ? open + string.Join(',', parts) + close
            : open + "\n" + inner + string.Join(",\n" + inner, parts) + "\n" + indent + close;
    }

    /// <summary>The gap a vector's <c>space</c> argument gives, from its source text: a Number, a String, a wrapper of either, or anything else.</summary>
    private static string GapOf(string source)
    {
        if (source.StartsWith("new Number(", StringComparison.Ordinal))
            return GapOf(source["new Number(".Length..^1]);
        if (source.StartsWith("new String(", StringComparison.Ordinal))
            return GapOf(source["new String(".Length..^1]);
        if (source.StartsWith('"'))
            return JsonText.GapFromString(Parse(source).String);

        return source switch
        {
            "NaN" => JsonText.GapFromNumber(double.NaN),
            "Infinity" => JsonText.GapFromNumber(double.PositiveInfinity),
            "-Infinity" => JsonText.GapFromNumber(double.NegativeInfinity),
            "2**53" => JsonText.GapFromNumber(9007199254740992),
            "null" or "true" or "false" or "[2]" or "{}" => string.Empty,
            _ => JsonText.GapFromNumber(double.Parse(source, NumberStyles.Float, NumberFormatInfo.InvariantInfo)),
        };
    }
}
