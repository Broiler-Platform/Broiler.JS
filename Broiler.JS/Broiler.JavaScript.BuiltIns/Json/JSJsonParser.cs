using System.Globalization;
using Broiler.ECMAScript;
using Broiler.ECMAScript.Text;
using Broiler.JavaScript.BuiltIns.Boolean;
using Broiler.JavaScript.BuiltIns.Null;
using Broiler.JavaScript.BuiltIns.Array;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Json;

/// <summary>
/// <c>JSON.parse</c>'s reading: the shared library's <see cref="JsonReader"/> checks the grammar and
/// converts the leaves, and this class builds the values (SJL-5.5).
/// </summary>
/// <remarks>
/// Until SJL-5.5 this was System.Text.Json, whose grammar is not quite the language's: its default
/// depth of 64, which nobody chose, refused deeper documents (J1); an escaped lone surrogate was
/// refused (J2); and a raw one let the transcoding's .NET exception reach a script (J3). The reader
/// reads code units, so either surrogate is a string holding it, and the depth is this engine's
/// policy, <see cref="MaximumDepth"/>.
/// </remarks>
internal static class JSJsonParser
{
    /// <summary>
    /// This engine's JSON nesting policy (SJL-5.5): the deepest value <c>JSON.parse</c> and
    /// <c>JSON.rawJSON</c> read, the document itself being depth 0. Past it the parse is a
    /// <c>RangeError</c>. The reader does not recurse; the building below does, one frame a level.
    /// </summary>
    internal const int MaximumDepth = 512;

    private static void AddJsonObjectProperty(JSObject target, string name, JSValue value)
    {
        if (uint.TryParse(name, out var index) && index != uint.MaxValue && index.ToString() == name)
        {
            target.FastAddValue(index, value, JSPropertyAttributes.EnumerableConfigurableValue);
            return;
        }

        target.FastAddValue(KeyStrings.GetOrCreate(name), value, JSPropertyAttributes.EnumerableConfigurableValue);
    }

    public static JSValue Parse(string text) => Read(text, null);

    /// <summary>
    /// Parse with source text tracking for the reviver context (ES2026 §4.7): each primitive's
    /// source is its text exactly as written, quotes, escapes and exponent included.
    /// </summary>
    public static JSValue ParseWithSource(string text, JsonParserReceiverWithSource sources) => Read(text, sources);

    private static JSValue Read(string text, JsonParserReceiverWithSource? sources)
    {
        var reader = new JsonReader(text, new DepthLimit(MaximumDepth));
        Next(ref reader, text);
        var value = Build(ref reader, text, sources, out _);

        // The end of the text, after nothing but white space: anything else is refused where it starts.
        Next(ref reader, text);
        return value;
    }

    private static void Next(ref JsonReader reader, string text)
    {
        if (reader.Read() != ResultStatus.Completed)
            throw Error(text, reader.Failure);
    }

    /// <summary>The error for a text the reader refused: a SyntaxError where it stopped, or the RangeError of the depth policy.</summary>
    internal static JSException Error(string text, Failure failure) => failure.MessageId switch
    {
        "json.too-deep" => JSEngine.NewRangeError("Maximum JSON nesting depth exceeded"),
        "json.unterminated-string" => Syntax("Unterminated string in JSON", failure.Position),
        "json.control-character" => Syntax("Bad control character in JSON", failure.Position),
        "json.bad-escape" => Syntax("Bad escaped character in JSON", failure.Position),
        "json.bad-unicode-escape" => Syntax("Bad Unicode escape in JSON", failure.Position),
        _ when failure.Position >= text.Length => Syntax("Unexpected end of JSON input", failure.Position),
        _ => Syntax("Unexpected token '" + text[failure.Position] + "' in JSON", failure.Position),
    };

    private static JSException Syntax(string message, int position) =>
        JSEngine.NewSyntaxError(message + " at position " + position.ToString(CultureInfo.InvariantCulture));

    /// <summary>Builds the value whose first token the reader has just read.</summary>
    private static JSValue Build(ref JsonReader reader, string text, JsonParserReceiverWithSource? sources, out string? source)
    {
        source = null;
        switch (reader.Token)
        {
            case JsonToken.BeginObject:
                return BuildObject(ref reader, text, sources);

            case JsonToken.BeginArray:
                return BuildArray(ref reader, text, sources);
        }

        if (sources != null)
            source = text[reader.TokenStart..reader.TokenEnd];

        return reader.Token switch
        {
            JsonToken.String => new JSString(reader.StringValue),
            JsonToken.Number => new JSNumber(reader.NumberValue),
            JsonToken.True => JSBoolean.True,
            JsonToken.False => JSBoolean.False,
            _ => JSNull.Value,
        };
    }

    private static JSValue BuildArray(ref JsonReader reader, string text, JsonParserReceiverWithSource? sources)
    {
        var j = new JSArray();
        while (true)
        {
            Next(ref reader, text);
            if (reader.Token == JsonToken.EndArray)
                return j;

            var value = Build(ref reader, text, sources, out var source);
            if (sources != null)
                value = sources((j, j.Length.ToString(), value, source!));

            if (!value.IsUndefined)
                j.Add(value);
        }
    }

    private static JSValue BuildObject(ref JsonReader reader, string text, JsonParserReceiverWithSource? sources)
    {
        var j = new JSObject();
        while (true)
        {
            Next(ref reader, text);
            if (reader.Token == JsonToken.EndObject)
                return j;

            var name = reader.StringValue;
            Next(ref reader, text);
            var value = Build(ref reader, text, sources, out var source);
            if (sources != null)
            {
                value = sources((j, name, value, source!));
                if (value.IsUndefined)
                    continue;
            }

            AddJsonObjectProperty(j, name, value);
        }
    }
}
