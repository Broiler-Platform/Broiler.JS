using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.Numerics;

namespace Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.Text;

/// <summary>An engine error an arm raises, recorded by its constructor's name (the rule's "recorded exception").</summary>
internal class ArmError(string kind, string message) : Exception(message)
{
    public string Kind { get; } = kind;
}

/// <summary>
/// One input of an operation of bundle sjl-5-001's reference-arm comparison: the population line it came
/// from, decoded, and the parameters the operation runs it at.
/// </summary>
internal sealed record TextInput
{
    /// <summary>The population class the line belongs to.</summary>
    public required string Class { get; init; }

    /// <summary>The line's number in its class, from 0.</summary>
    public required long Line { get; init; }

    /// <summary>The line as the population writes it.</summary>
    public required string Raw { get; init; }

    /// <summary>The line's string, decoded; for U1C the code point's string, for B1Y the empty string.</summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>A second string: a filler, or a text built from the line.</summary>
    public string? Text2 { get; init; }

    /// <summary>An index, a target length, a length or a destination's length (-1 for unbounded).</summary>
    public long Int { get; init; }

    /// <summary>A maximum length or a depth limit.</summary>
    public long Limit { get; init; }

    /// <summary>A relative index or a Number, already ToIntegerOrInfinity'd where the operation takes one.</summary>
    public double Number { get; init; }

    /// <summary>The bytes of a B1Y line.</summary>
    public byte[] Bytes { get; init; } = [];

    /// <summary>The first parameter the operation runs the line at (a placement, a set, an alphabet), or -1.</summary>
    public int Parameter { get; init; } = -1;

    /// <summary>A second parameter (a last-chunk mode, a capture state), or -1.</summary>
    public int Parameter2 { get; init; } = -1;

    /// <summary>A third parameter (a named-capture configuration), or -1.</summary>
    public int Parameter3 { get; init; } = -1;

    /// <summary>A fourth parameter (a match position), or -1.</summary>
    public int Parameter4 { get; init; } = -1;

    /// <summary>What the listing shows for the parameters and any derived input.</summary>
    public string Detail { get; init; } = "-";
}

/// <summary>
/// The population scripts/conformance/generate_text_population.py writes: one gzip file per class, one
/// input per line, a string with every character outside '!' to '~' (and the backslash) as \uXXXX.
/// </summary>
internal sealed class TextPopulation(string directory)
{
    public string Directory { get; } = directory;

    /// <summary>A class's lines, in order.</summary>
    public IEnumerable<string> ReadLines(string name)
    {
        using var file = File.OpenRead(Path.Combine(Directory, name + ".txt.gz"));
        using var gzip = new GZipStream(file, CompressionMode.Decompress);
        using var reader = new StreamReader(gzip, new UTF8Encoding(false, true));
        while (reader.ReadLine() is { } line)
            yield return line;
    }

    /// <summary>A small class whole, decoded.</summary>
    public IReadOnlyList<string> Strings(string name) => ReadLines(name).Select(NumericsPopulation.Unescape).ToList();

    /// <summary>A class's inputs, decoded, in chunks of <paramref name="size"/> lines.</summary>
    public IEnumerable<List<TextInput>> Chunks(string name, int size)
    {
        var chunk = new List<TextInput>(size);
        long number = 0;
        foreach (var line in ReadLines(name))
        {
            chunk.Add(Parse(name, number++, line));
            if (chunk.Count == size)
            {
                yield return chunk;
                chunk = new List<TextInput>(size);
            }
        }

        if (chunk.Count > 0)
            yield return chunk;
    }

    /// <summary>Decodes one line of a class.</summary>
    public static TextInput Parse(string name, long number, string line)
    {
        var input = new TextInput { Class = name, Line = number, Raw = line };
        switch (name)
        {
            case "U1C":
            {
                var point = int.Parse(line, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
                return input with { Text = point <= 0xFFFF ? ((char)point).ToString() : char.ConvertFromUtf32(point) };
            }

            case "B1Y":
                return input with { Bytes = line == "-" ? [] : Convert.FromHexString(line) };
            case "P1X":
            {
                var fields = line.Split(' ');
                return input with
                {
                    Int = long.Parse(fields[0], CultureInfo.InvariantCulture),
                    Number = fields[1] switch
                    {
                        "-Infinity" => double.NegativeInfinity,
                        "Infinity" => double.PositiveInfinity,
                        _ => double.Parse(fields[1], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture),
                    },
                };
            }

            case "J1G":
            {
                var kind = line[0];
                var rest = line[2..];
                return kind == 'n'
                    ? input with
                    {
                        Parameter = 0,
                        Number = rest switch
                        {
                            "-Infinity" => double.NegativeInfinity,
                            "Infinity" => double.PositiveInfinity,
                            _ => double.Parse(rest, NumberStyles.Float, CultureInfo.InvariantCulture),
                        },
                    }
                    : input with { Parameter = 1, Text = NumericsPopulation.Unescape(rest) };
            }

            default:
                return input with { Text = NumericsPopulation.Unescape(line) };
        }
    }
}

/// <summary>How the text area's results are written, beside the numerics' <see cref="Render"/>.</summary>
internal static class TextRender
{
    public static string Str(string text) => "str:" + NumericsPopulation.Escape(text);

    public static string Bool(bool value) => value ? "bool:true" : "bool:false";

    public static string Bytes(ReadOnlySpan<byte> bytes) => "bytes:" + (bytes.IsEmpty ? "-" : Convert.ToHexStringLower(bytes));

    public static string Error(string kind) => "error:" + kind;

    /// <summary>A decode's outcome: what it read, the bytes it wrote, and its error, or none.</summary>
    public static string Decoded(int read, ReadOnlySpan<byte> bytes, string? error) =>
        "read:" + read.ToString(CultureInfo.InvariantCulture) + "|" + Bytes(bytes) + "|" + (error is null ? "ok" : Error(error));

    /// <summary>
    /// A parsed JSON value by its structure: strings by their code units, numbers by their bit patterns,
    /// an object's members in [[OwnPropertyKeys]] order (array-index keys ascending, then the others in
    /// their order of creation), which is how either engine's objects enumerate.
    /// </summary>
    public static string Json(JsonTree value)
    {
        var text = new StringBuilder();
        Write(value, text);
        return "json:" + text;
    }

    private static void Write(JsonTree value, StringBuilder text)
    {
        switch (value.Kind)
        {
            case JsonTreeKind.Null: text.Append("null"); break;
            case JsonTreeKind.True: text.Append("true"); break;
            case JsonTreeKind.False: text.Append("false"); break;
            case JsonTreeKind.Number: text.Append(Render.F64(value.Number)); break;
            case JsonTreeKind.String: text.Append(Str(value.String)); break;
            case JsonTreeKind.Array:
                text.Append('[');
                for (var at = 0; at < value.Elements!.Count; at++)
                {
                    if (at > 0)
                        text.Append(',');
                    Write(value.Elements[at], text);
                }

                text.Append(']');
                break;
            case JsonTreeKind.Object:
                text.Append('{');
                var first = true;
                foreach (var (key, member) in Ordered(value.Members!))
                {
                    if (!first)
                        text.Append(',');
                    first = false;
                    text.Append(Str(key)).Append(':');
                    Write(member, text);
                }

                text.Append('}');
                break;
        }
    }

    private static IEnumerable<(string Key, JsonTree Value)> Ordered(List<(string Key, JsonTree Value)> members)
    {
        var indices = members.Where(static m => IsArrayIndex(m.Key)).OrderBy(static m => uint.Parse(m.Key, CultureInfo.InvariantCulture));
        return indices.Concat(members.Where(static m => !IsArrayIndex(m.Key)));
    }

    private static bool IsArrayIndex(string key) =>
        uint.TryParse(key, NumberStyles.None, CultureInfo.InvariantCulture, out var index) && index != uint.MaxValue
        && index.ToString(CultureInfo.InvariantCulture) == key;
}

internal enum JsonTreeKind
{
    Null,
    True,
    False,
    Number,
    String,
    Array,
    Object,
}

/// <summary>A parsed JSON value, whichever arm parsed it.</summary>
internal sealed class JsonTree
{
    public JsonTreeKind Kind { get; init; }

    public double Number { get; init; }

    public string String { get; init; } = string.Empty;

    public List<JsonTree>? Elements { get; init; }

    /// <summary>The members, a repeated key at its first position with its last value.</summary>
    public List<(string Key, JsonTree Value)>? Members { get; init; }
}
