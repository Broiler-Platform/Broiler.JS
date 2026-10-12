using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;

namespace Broiler.JavaScript.Vectors.Tests.Corpus;

/// <summary>The kinds of typed value the schema's section 3 defines.</summary>
public enum TypedValueKind
{
    Number,
    String,
    BigInt,
    Integer,
    Boolean,
    Undefined,
    Null,
    Symbol,
    Object,
}

/// <summary>
/// One typed value in the corpus notation (conformance/vectors/SCHEMA.md, section 3), decoded, and
/// the rendering of an engine's value into that notation (section 7).
/// </summary>
/// <remarks>
/// Every value has exactly one written form, so a parsed value is accepted only when writing it
/// again gives back the text it was read from, and a rendered outcome compares with an expected
/// one as text.
/// </remarks>
public sealed partial record TypedValue(TypedValueKind Kind, string Text)
{
    public const ulong CanonicalNaN = 0x7FF8000000000000;

    /// <summary>The bit pattern of a Number.</summary>
    public ulong Bits { get; init; }

    /// <summary>The UTF-16 code units of a String.</summary>
    public string Chars { get; init; } = string.Empty;

    /// <summary>The value of a BigInt or of an integral Number given by its value.</summary>
    public BigInteger Integer { get; init; }

    public bool Boolean { get; init; }

    public double Number => BitConverter.UInt64BitsToDouble(Bits);

    /// <summary>
    /// Decodes <paramref name="text"/>, refusing a value that is malformed or not in its one
    /// canonical form. <paramref name="code"/> names the defect the way the validator reports it.
    /// </summary>
    public static bool TryParse(string text, out TypedValue value, out string code, out string message)
    {
        value = null!;
        code = "bad-value";
        message = string.Empty;

        if (text.StartsWith("f64:", StringComparison.Ordinal))
        {
            code = "bad-double";
            if (!DoublePattern().IsMatch(text))
            {
                message = $"'{text}' is not f64:0x and 16 uppercase hexadecimal digits";
                return false;
            }

            var bits = ulong.Parse(text.AsSpan(6), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
            if (double.IsNaN(BitConverter.UInt64BitsToDouble(bits)) && bits != CanonicalNaN)
            {
                message = $"'{text}' is a NaN not written f64:0x7FF8000000000000";
                return false;
            }

            value = new TypedValue(TypedValueKind.Number, text) { Bits = bits };
            return true;
        }

        if (text.StartsWith("str:", StringComparison.Ordinal))
        {
            code = "bad-string";
            if (!TryDecodeString(text, out var chars, out message))
                return false;
            if (RenderString(chars) != text)
            {
                message = $"'{text}' is not the canonical form of its string, {RenderString(chars)}";
                return false;
            }

            value = new TypedValue(TypedValueKind.String, text) { Chars = chars };
            return true;
        }

        if (text.StartsWith("bigint:", StringComparison.Ordinal) || text.StartsWith("int:", StringComparison.Ordinal))
        {
            var bigint = text[0] == 'b';
            var digits = text[(bigint ? 7 : 4)..];
            if (!IntegerPattern().IsMatch(digits))
            {
                message = $"'{text}' is not a decimal integer without leading zeros or -0";
                return false;
            }

            value = new TypedValue(bigint ? TypedValueKind.BigInt : TypedValueKind.Integer, text)
            {
                Integer = BigInteger.Parse(digits, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture),
            };
            return true;
        }

        switch (text)
        {
            case "bool:true":
            case "bool:false":
                value = new TypedValue(TypedValueKind.Boolean, text) { Boolean = text == "bool:true" };
                return true;
            case "undefined":
                value = new TypedValue(TypedValueKind.Undefined, text);
                return true;
            case "null":
                value = new TypedValue(TypedValueKind.Null, text);
                return true;
            case "symbol":
                value = new TypedValue(TypedValueKind.Symbol, text);
                return true;
            case "object":
                value = new TypedValue(TypedValueKind.Object, text);
                return true;
        }

        message = $"'{text}' is not a typed value";
        return false;
    }

    /// <summary>
    /// Reads one typed value's text starting at <paramref name="position"/>: a string up to its
    /// closing quote, which may hold spaces and <c>|</c>, anything else up to the next space.
    /// </summary>
    public static bool TryReadToken(string line, ref int position, out string token)
    {
        var start = position;
        if (string.CompareOrdinal(line, start, "str:\"", 0, 5) == 0)
        {
            var i = start + 5;
            while (i < line.Length && line[i] != '"')
                i += line[i] == '\\' ? 2 : 1;
            if (i >= line.Length)
            {
                token = line[start..];
                position = line.Length;
                return false;
            }

            position = i + 1;
            token = line[start..position];
            return true;
        }

        var end = line.IndexOf(' ', start);
        position = end < 0 ? line.Length : end;
        token = line[start..position];
        return token.Length > 0;
    }

    /// <summary>A Number in the notation: its bit pattern, every NaN as the canonical one.</summary>
    public static string RenderNumber(double value)
        => "f64:0x" + (double.IsNaN(value) ? CanonicalNaN : BitConverter.DoubleToUInt64Bits(value))
            .ToString("X16", CultureInfo.InvariantCulture);

    /// <summary>A String in the notation, from its UTF-16 code units.</summary>
    public static string RenderString(string chars)
    {
        var builder = new StringBuilder(chars.Length + 6).Append("str:\"");
        foreach (var c in chars)
        {
            if (c == '"' || c == '\\')
                builder.Append('\\').Append(c);
            else if (c >= ' ' && c <= '~')
                builder.Append(c);
            else
                builder.Append('\\').Append('u').Append(((int)c).ToString("X4", CultureInfo.InvariantCulture));
        }

        return builder.Append('"').ToString();
    }

    public static string RenderBigInt(BigInteger value)
        => "bigint:" + value.ToString(CultureInfo.InvariantCulture);

    public static string RenderBoolean(bool value) => value ? "bool:true" : "bool:false";

    private static bool TryDecodeString(string text, out string chars, out string message)
    {
        chars = string.Empty;
        if (text.Length < 6 || text[4] != '"' || text[^1] != '"')
        {
            message = $"'{text}' is not str: and a quoted string";
            return false;
        }

        var builder = new StringBuilder(text.Length);
        for (var i = 5; i < text.Length - 1; i++)
        {
            var c = text[i];
            if (c != '\\')
            {
                if (c == '"' || c < ' ' || c > '~')
                {
                    message = $"'{text}' holds U+{(int)c:X4} written as itself";
                    return false;
                }

                builder.Append(c);
                continue;
            }

            if (i + 1 >= text.Length - 1)
            {
                message = $"'{text}' ends in an incomplete escape";
                return false;
            }

            var next = text[++i];
            if (next == '"' || next == '\\')
            {
                builder.Append(next);
            }
            else if (next == 'u' && i + 4 < text.Length - 1 && HexPattern().IsMatch(text.AsSpan(i + 1, 4)))
            {
                builder.Append((char)int.Parse(text.AsSpan(i + 1, 4), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture));
                i += 4;
            }
            else
            {
                message = $"'{text}' holds an escape that is neither a quote, a backslash nor u and four uppercase hexadecimal digits";
                return false;
            }
        }

        chars = builder.ToString();
        message = string.Empty;
        return true;
    }

    [GeneratedRegex("^f64:0x[0-9A-F]{16}$")]
    private static partial System.Text.RegularExpressions.Regex DoublePattern();

    [GeneratedRegex("^(0|-?[1-9][0-9]*)$")]
    private static partial System.Text.RegularExpressions.Regex IntegerPattern();

    [GeneratedRegex("^[0-9A-F]{4}$")]
    private static partial System.Text.RegularExpressions.Regex HexPattern();
}
