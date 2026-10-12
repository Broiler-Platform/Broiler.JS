// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   11
// Annotated:        11/11
// Exempt:           13
// Human-reviewed:   0/11
// IP risk:          Low
// Security risk:    Low
// Criteria:         0/0
// Resource impact:  6/10 max
// Unverified:       11
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Numerics;
using System.Text;

namespace Broiler.ECMAScript.Conformance;

/// <summary>The kinds of typed value the corpus schema's section 3 defines.</summary>
// Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=2BE10D
// Broiler-Human:        PENDING
public enum TypedValueKind
{
    /// <summary>A Number, written <c>f64:0x</c> and its sixteen-digit bit pattern.</summary>
    Number,

    /// <summary>A String, written <c>str:"..."</c> over its UTF-16 code units.</summary>
    String,

    /// <summary>A BigInt, written <c>bigint:</c> and its decimal value.</summary>
    BigInt,

    /// <summary>An integral Number given by its value, an input notation only: <c>int:</c> and its decimal value.</summary>
    Integer,

    /// <summary>A Boolean, <c>bool:true</c> or <c>bool:false</c>.</summary>
    Boolean,

    /// <summary>The value <c>undefined</c>.</summary>
    Undefined,

    /// <summary>The value <c>null</c>.</summary>
    Null,

    /// <summary>Some Symbol, an outcome notation only: <c>symbol</c>.</summary>
    Symbol,

    /// <summary>Some object, an outcome notation only: <c>object</c>.</summary>
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
/// <param name="Kind">Which of the schema's kinds the value is.</param>
/// <param name="Text">The value's one written form.</param>
// Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=6; Fingerprint=C74C22
// Broiler-Human:        PENDING
public sealed record TypedValue(TypedValueKind Kind, string Text)
{
    /// <summary>The bit pattern every NaN is written with: <c>f64:0x7FF8000000000000</c>.</summary>
    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=CCAD94
    // Broiler-Human:        PENDING
    public const ulong CanonicalNaN = 0x7FF8000000000000;

    /// <summary>The bit pattern of a Number.</summary>
    public ulong Bits { get; init; }

    /// <summary>The UTF-16 code units of a String.</summary>
    public string Chars { get; init; } = string.Empty;

    /// <summary>The value of a BigInt or of an integral Number given by its value.</summary>
    public BigInteger Integer { get; init; }

    /// <summary>The value of a Boolean.</summary>
    public bool Boolean { get; init; }

    /// <summary>The value of a Number, from its bit pattern.</summary>
    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=B570F4
    // Broiler-Human:        PENDING
    public double Number => BitConverter.UInt64BitsToDouble(Bits);

    /// <summary>
    /// Decodes <paramref name="text"/>, refusing a value that is malformed or not in its one
    /// canonical form (schema section 3).
    /// </summary>
    /// <param name="text">One typed value's text.</param>
    /// <param name="value">The decoded value, when the text is accepted.</param>
    /// <param name="code">The defect, named the way the validator reports it: <c>bad-double</c>, <c>bad-string</c> or <c>bad-value</c>.</param>
    /// <param name="message">What is wrong with the text, when it is refused.</param>
    /// <returns>Whether the text is a typed value in its canonical form.</returns>
    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=6; Fingerprint=A48044
    // Broiler-Human:        PENDING
    public static bool TryParse(string text, out TypedValue value, out string code, out string message)
    {
        ArgumentNullException.ThrowIfNull(text);
        value = null!;
        code = "bad-value";
        message = string.Empty;

        if (text.StartsWith("f64:", StringComparison.Ordinal))
        {
            code = "bad-double";
            if (text.Length != 22 || !text.AsSpan(4, 2).SequenceEqual("0x") || !Ascii.All(text.AsSpan(6), Ascii.IsUpperHex))
            {
                message = "'" + text + "' is not f64:0x and 16 uppercase hexadecimal digits";
                return false;
            }

            var bits = Ascii.ParseUpperHex(text.AsSpan(6));
            if (double.IsNaN(BitConverter.UInt64BitsToDouble(bits)) && bits != CanonicalNaN)
            {
                message = "'" + text + "' is a NaN not written f64:0x7FF8000000000000";
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
            var canonical = RenderString(chars);
            if (canonical != text)
            {
                message = "'" + text + "' is not the canonical form of its string, " + canonical;
                return false;
            }

            value = new TypedValue(TypedValueKind.String, text) { Chars = chars };
            return true;
        }

        var bigint = text.StartsWith("bigint:", StringComparison.Ordinal);
        if (bigint || text.StartsWith("int:", StringComparison.Ordinal))
        {
            var digits = text.AsSpan(bigint ? 7 : 4);
            if (!Ascii.IsCanonicalInteger(digits))
            {
                message = "'" + text + "' is not a decimal integer without leading zeros or -0";
                return false;
            }

            value = new TypedValue(bigint ? TypedValueKind.BigInt : TypedValueKind.Integer, text)
            {
                Integer = Ascii.ParseInteger(digits),
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

        message = "'" + text + "' is not a typed value";
        return false;
    }

    /// <summary>
    /// Reads one typed value's text starting at <paramref name="position"/>: a string up to its
    /// closing quote, which may hold spaces and <c>|</c>, anything else up to the next space
    /// (schema sections 2 and 4).
    /// </summary>
    /// <param name="line">The text the value is read from.</param>
    /// <param name="position">Where the value starts; on return, the index just after it.</param>
    /// <param name="token">The value's text, which may still be malformed.</param>
    /// <returns>Whether a complete token was read.</returns>
    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=2; Fingerprint=7B1E3E
    // Broiler-Human:        PENDING
    public static bool TryReadToken(string line, ref int position, out string token)
    {
        ArgumentNullException.ThrowIfNull(line);
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

    /// <summary>A Number in the notation: its bit pattern, every NaN as the canonical one (schema section 7).</summary>
    /// <param name="value">The Number an engine or a library produced.</param>
    /// <returns>The value's one written form.</returns>
    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=525F9B
    // Broiler-Human:        PENDING
    public static string RenderNumber(double value)
        => "f64:0x" + Ascii.UpperHex(double.IsNaN(value) ? CanonicalNaN : BitConverter.DoubleToUInt64Bits(value), 16);

    /// <summary>A String in the notation, from its UTF-16 code units (schema section 3).</summary>
    /// <param name="chars">The string's code units, lone surrogates included.</param>
    /// <returns>The value's one written form.</returns>
    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=2; Fingerprint=CFB68E
    // Broiler-Human:        PENDING
    public static string RenderString(string chars)
    {
        ArgumentNullException.ThrowIfNull(chars);
        var builder = new StringBuilder(chars.Length + 6).Append("str:\"");
        foreach (var c in chars)
        {
            if (c == '"' || c == '\\')
                builder.Append('\\').Append(c);
            else if (c >= ' ' && c <= '~')
                builder.Append(c);
            else
                builder.Append('\\').Append('u').Append(Ascii.UpperHex(c, 4));
        }

        return builder.Append('"').ToString();
    }

    /// <summary>A BigInt in the notation (schema section 3).</summary>
    /// <param name="value">The BigInt's value.</param>
    /// <returns>The value's one written form.</returns>
    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=56E40B
    // Broiler-Human:        PENDING
    public static string RenderBigInt(BigInteger value) => "bigint:" + Ascii.Decimal(value);

    /// <summary>A Boolean in the notation (schema section 3).</summary>
    /// <param name="value">The Boolean's value.</param>
    /// <returns><c>bool:true</c> or <c>bool:false</c>.</returns>
    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=5C4FCA
    // Broiler-Human:        PENDING
    public static string RenderBoolean(bool value) => value ? "bool:true" : "bool:false";

    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=4; Fingerprint=6CB6F1
    // Broiler-Human:        PENDING
    private static bool TryDecodeString(string text, out string chars, out string message)
    {
        chars = string.Empty;
        if (text.Length < 6 || text[4] != '"' || text[^1] != '"')
        {
            message = "'" + text + "' is not str: and a quoted string";
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
                    message = "'" + text + "' holds U+" + Ascii.UpperHex(c, 4) + " written as itself";
                    return false;
                }

                builder.Append(c);
                continue;
            }

            if (i + 1 >= text.Length - 1)
            {
                message = "'" + text + "' ends in an incomplete escape";
                return false;
            }

            var next = text[++i];
            if (next == '"' || next == '\\')
            {
                builder.Append(next);
            }
            else if (next == 'u' && i + 4 < text.Length - 1 && Ascii.All(text.AsSpan(i + 1, 4), Ascii.IsUpperHex))
            {
                builder.Append((char)Ascii.ParseUpperHex(text.AsSpan(i + 1, 4)));
                i += 4;
            }
            else
            {
                message = "'" + text + "' holds an escape that is neither a quote, a backslash nor u and four uppercase hexadecimal digits";
                return false;
            }
        }

        chars = builder.ToString();
        message = string.Empty;
        return true;
    }
}
