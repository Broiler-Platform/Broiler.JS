// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   14
// Annotated:        14/14
// Exempt:           3
// Human-reviewed:   0/14
// IP risk:          Low
// Security risk:    High
// Criteria:         1/1
// Resource impact:  2/10 max
// Unverified:       14
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Text;

namespace Broiler.ECMAScript.Text;

/// <summary>
/// The URI codecs: <c>Encode</c> and <c>Decode</c> (ECMA-262 19.2.6.5 and 19.2.6.6), which the four
/// global URI functions call, and Annex B's <c>escape</c> and <c>unescape</c> (B.2.1.1, B.2.1.2).
/// </summary>
/// <remarks>
/// <para>
/// <b>Written from the specification</b> and from Broiler.VM's implementation, which cites no third
/// party; Broiler.JS's former <c>UriHelper</c> is not its source. Every hexadecimal digit written is
/// upper case, every one read is either case, and nothing here goes through a base class library
/// URI or UTF-8 API, which replace or refuse differently from the language.
/// </para>
/// <para>
/// <b>Each step is charged one unit before it is taken</b>, and <c>Decode</c> charges one more
/// before it reads each continuation octet, which is what Broiler.VM charged before the cut-over
/// (JSD-0070, decision 3). A malformed text is <see cref="ResultStatus.Malformed"/> with the
/// position of the code unit or escape at fault; an engine raises <c>URIError</c>.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 19.2.6; IP=Low; Security=Medium; Resources=2; Fingerprint=91F488
// Broiler-Human:        PENDING
public static class UriCodec
{
    /// <summary><c>encodeURI</c>'s <c>extraUnescaped</c> and <c>decodeURI</c>'s <c>preserveEscapeSet</c>: the <c>uriReserved</c> characters and <c>#</c> (ECMA-262 19.2.6.2 and 19.2.6.1).</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 19.2.6.1; IP=Low; Security=Low; Resources=1; Fingerprint=0ED4B6
    // Broiler-Human:        PENDING
    public const string ReservedAndNumberSign = ";/?:@&=+$,#";

    /// <summary><c>Encode</c> (ECMA-262 19.2.6.5): every code point outside the unescaped set as its UTF-8 octets, each <c>%XX</c>.</summary>
    /// <param name="text">The String.</param>
    /// <param name="extraUnescaped">What is left as it is beyond <c>uriUnescaped</c>: empty for <c>encodeURIComponent</c>, <see cref="ReservedAndNumberSign"/> for <c>encodeURI</c>.</param>
    /// <param name="result">The encoded String, when the status is <see cref="ResultStatus.Completed"/>.</param>
    /// <param name="failure">The lone surrogate's position, when it is not.</param>
    /// <param name="charge">The caller's meter: one unit before each step, a surrogate pair being one step; <see langword="null"/> for none.</param>
    /// <returns><see cref="ResultStatus.Completed"/>, or <see cref="ResultStatus.Malformed"/> at a lone surrogate (<c>uri.unpaired-surrogate</c>).</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 19.2.6.5; IP=Low; Security=Medium; Resources=2; Fingerprint=781004
    // Broiler-Human:        PENDING
    public static ResultStatus Encode(ReadOnlySpan<char> text, ReadOnlySpan<char> extraUnescaped, out string result, out Failure failure, WorkCharge? charge = null)
    {
        failure = default;
        var output = new StringBuilder(text.Length);
        for (var at = 0; at < text.Length; at++)
        {
            charge?.Invoke(1);
            var unit = text[at];
            if (IsUriUnescaped(unit) || extraUnescaped.Contains(unit))
            {
                output.Append(unit);
                continue;
            }

            var record = Utf16.CodePointAt(text, at);
            if (record.IsUnpairedSurrogate)
                return Refuse(at, "uri.unpaired-surrogate", out result, out failure);

            at += record.CodeUnitCount - 1;
            AppendUtf8(output, record.CodePoint);
        }

        result = output.ToString();
        return ResultStatus.Completed;
    }

    /// <summary><c>Decode</c> (ECMA-262 19.2.6.6): every <c>%XX</c> escape, alone or as a UTF-8 sequence, as the code point it encodes.</summary>
    /// <param name="text">The String.</param>
    /// <param name="preserveEscapeSet">The characters whose one-octet escape is kept as it is: empty for <c>decodeURIComponent</c>, <see cref="ReservedAndNumberSign"/> for <c>decodeURI</c>.</param>
    /// <param name="result">The decoded String, when the status is <see cref="ResultStatus.Completed"/>.</param>
    /// <param name="failure">Where the malformed escape or sequence starts, when it is not.</param>
    /// <param name="charge">The caller's meter: one unit before each step and one before each continuation octet; <see langword="null"/> for none.</param>
    /// <returns>
    /// <see cref="ResultStatus.Completed"/>, or <see cref="ResultStatus.Malformed"/>: an escape that is
    /// not <c>%</c> and two hexadecimal digits (<c>uri.malformed-escape</c>), an octet that cannot
    /// lead or continue a sequence (<c>uri.invalid-octet</c>), or a sequence that is overlong, a
    /// surrogate or above U+10FFFF (<c>uri.invalid-utf8</c>).
    /// </returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 19.2.6.6; IP=Low; Security=High; Resources=2; Fingerprint=230B5B
    // Broiler-Falsified-If: an overlong sequence, an encoded surrogate or a value above U+10FFFF completes, a preserved character's escape is decoded, or a well-formed sequence is refused
    // Broiler-Human:        PENDING
    public static ResultStatus Decode(ReadOnlySpan<char> text, ReadOnlySpan<char> preserveEscapeSet, out string result, out Failure failure, WorkCharge? charge = null)
    {
        failure = default;
        var output = new StringBuilder(text.Length);
        var at = 0;
        while (at < text.Length)
        {
            charge?.Invoke(1);
            if (text[at] != '%')
            {
                output.Append(text[at]);
                at++;
                continue;
            }

            var start = at;
            var lead = OctetAt(text, at);
            if (lead < 0)
                return Refuse(at, "uri.malformed-escape", out result, out failure);

            at += 3;
            if ((lead & 0x80) == 0)
            {
                if (preserveEscapeSet.Contains((char)lead))
                    output.Append(text.Slice(start, 3));
                else
                    output.Append((char)lead);

                continue;
            }

            var length = lead switch
            {
                >= 0xC0 and <= 0xDF => 2,
                >= 0xE0 and <= 0xEF => 3,
                >= 0xF0 and <= 0xF7 => 4,
                _ => 0,
            };
            if (length == 0)
                return Refuse(start, "uri.invalid-octet", out result, out failure);

            var point = lead & (0xFF >> (length + 1));
            for (var extra = 1; extra < length; extra++)
            {
                charge?.Invoke(1);
                var continuation = OctetAt(text, at);
                if (continuation < 0)
                    return Refuse(at, "uri.malformed-escape", out result, out failure);

                if ((continuation & 0xC0) != 0x80)
                    return Refuse(at, "uri.invalid-octet", out result, out failure);

                at += 3;
                point = (point << 6) | (continuation & 0x3F);
            }

            var minimum = length switch
            {
                2 => 0x80,
                3 => 0x800,
                _ => 0x10000,
            };
            if (point < minimum || point > 0x10FFFF || point is >= 0xD800 and <= 0xDFFF)
                return Refuse(start, "uri.invalid-utf8", out result, out failure);

            AppendCodePoint(output, point);
        }

        result = output.ToString();
        return ResultStatus.Completed;
    }

    /// <summary>Annex B's <c>escape</c> (ECMA-262 B.2.1.1, steps 3 to 6): every code unit outside its set as <c>%XX</c> or <c>%uXXXX</c>.</summary>
    /// <param name="text">The String.</param>
    /// <param name="charge">The caller's meter: one unit before each code unit; <see langword="null"/> for none.</param>
    /// <returns>The escaped String. <c>escape</c> never fails.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 B.2.1.1; IP=Low; Security=Medium; Resources=2; Fingerprint=FCE721
    // Broiler-Human:        PENDING
    public static string Escape(ReadOnlySpan<char> text, WorkCharge? charge = null)
    {
        var output = new StringBuilder(text.Length);
        foreach (var unit in text)
        {
            charge?.Invoke(1);
            if (IsAsciiAlphanumeric(unit) || EscapeUnescaped.Contains(unit))
            {
                output.Append(unit);
            }
            else if (unit < 256)
            {
                AppendOctet(output, unit);
            }
            else
            {
                output.Append('%').Append('u')
                    .Append(UpperHexDigits[(unit >> 12) & 0xF]).Append(UpperHexDigits[(unit >> 8) & 0xF])
                    .Append(UpperHexDigits[(unit >> 4) & 0xF]).Append(UpperHexDigits[unit & 0xF]);
            }
        }

        return output.ToString();
    }

    /// <summary>Annex B's <c>unescape</c> (ECMA-262 B.2.1.2, steps 3 to 6): every <c>%XX</c> and <c>%uXXXX</c> as its code unit, and anything malformed as it is.</summary>
    /// <param name="text">The String.</param>
    /// <param name="charge">The caller's meter: one unit before each step, an escape being one step; <see langword="null"/> for none.</param>
    /// <returns>The unescaped String. <c>unescape</c> never fails.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 B.2.1.2; IP=Low; Security=Medium; Resources=2; Fingerprint=1BA52E
    // Broiler-Human:        PENDING
    public static string Unescape(ReadOnlySpan<char> text, WorkCharge? charge = null)
    {
        var output = new StringBuilder(text.Length);
        var at = 0;
        while (at < text.Length)
        {
            charge?.Invoke(1);
            var unit = text[at];
            if (unit == '%')
            {
                if (at + 5 < text.Length && text[at + 1] == 'u')
                {
                    var value = (HexDigitValue(text[at + 2]) << 12) | (HexDigitValue(text[at + 3]) << 8)
                        | (HexDigitValue(text[at + 4]) << 4) | HexDigitValue(text[at + 5]);
                    if (value >= 0)
                    {
                        unit = (char)value;
                        at += 5;
                    }
                }
                else if (at + 2 < text.Length)
                {
                    var value = (HexDigitValue(text[at + 1]) << 4) | HexDigitValue(text[at + 2]);
                    if (value >= 0)
                    {
                        unit = (char)value;
                        at += 2;
                    }
                }
            }

            output.Append(unit);
            at++;
        }

        return output.ToString();
    }

    /// <summary>The value of one hexadecimal digit of either case (ECMA-262 12.9.3 <c>HexDigit</c>), or a negative number for any other code unit.</summary>
    /// <param name="unit">The code unit.</param>
    /// <returns>Zero to fifteen, or -1, which stays negative when it is shifted left by up to 30 bits and combined with other digits by a bitwise or.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 12.9.3; IP=Low; Security=Low; Resources=1; Fingerprint=039EF5
    // Broiler-Human:        PENDING
    public static int HexDigitValue(char unit) => unit switch
    {
        >= '0' and <= '9' => unit - '0',
        >= 'a' and <= 'f' => unit - 'a' + 10,
        >= 'A' and <= 'F' => unit - 'A' + 10,
        _ => -1,
    };

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 19.2.6.6; IP=Low; Security=Medium; Resources=1; Fingerprint=565B81
    // Broiler-Human:        PENDING
    private static int OctetAt(ReadOnlySpan<char> text, int at)
    {
        if (at + 2 >= text.Length || text[at] != '%')
            return -1;

        var value = (HexDigitValue(text[at + 1]) << 4) | HexDigitValue(text[at + 2]);
        return value < 0 ? -1 : value;
    }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 19.2.6.5; IP=Low; Security=Low; Resources=1; Fingerprint=F424F8
    // Broiler-Human:        PENDING
    private static void AppendUtf8(StringBuilder output, int point)
    {
        if (point <= 0x7F)
        {
            AppendOctet(output, point);
        }
        else if (point <= 0x7FF)
        {
            AppendOctet(output, 0xC0 | (point >> 6));
            AppendOctet(output, 0x80 | (point & 0x3F));
        }
        else if (point <= 0xFFFF)
        {
            AppendOctet(output, 0xE0 | (point >> 12));
            AppendOctet(output, 0x80 | ((point >> 6) & 0x3F));
            AppendOctet(output, 0x80 | (point & 0x3F));
        }
        else
        {
            AppendOctet(output, 0xF0 | (point >> 18));
            AppendOctet(output, 0x80 | ((point >> 12) & 0x3F));
            AppendOctet(output, 0x80 | ((point >> 6) & 0x3F));
            AppendOctet(output, 0x80 | (point & 0x3F));
        }
    }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 19.2.6.5; IP=Low; Security=Low; Resources=1; Fingerprint=D10E52
    // Broiler-Human:        PENDING
    private static void AppendOctet(StringBuilder output, int octet) =>
        output.Append('%').Append(UpperHexDigits[(octet >> 4) & 0xF]).Append(UpperHexDigits[octet & 0xF]);

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 19.2.6.6; IP=Low; Security=Low; Resources=1; Fingerprint=C6C821
    // Broiler-Human:        PENDING
    private static void AppendCodePoint(StringBuilder output, int point)
    {
        if (point <= 0xFFFF)
        {
            output.Append((char)point);
            return;
        }

        var shifted = point - 0x10000;
        output.Append((char)(0xD800 + (shifted >> 10))).Append((char)(0xDC00 + (shifted & 0x3FF)));
    }

    /// <summary><c>uriUnescaped</c> (ECMA-262 19.2.6.1): <c>uriAlpha</c>, <c>DecimalDigit</c> and <c>uriMark</c>.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 19.2.6.1; IP=Low; Security=Low; Resources=1; Fingerprint=D40E9B
    // Broiler-Human:        PENDING
    private static bool IsUriUnescaped(char unit) => IsAsciiAlphanumeric(unit) || UriMark.Contains(unit);

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 19.2.6.1; IP=Low; Security=Low; Resources=1; Fingerprint=096A98
    // Broiler-Human:        PENDING
    private static bool IsAsciiAlphanumeric(char unit) => unit is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9');

    private static ReadOnlySpan<char> UriMark => "-_.!~*'()";

    /// <summary>What <c>escape</c> leaves as it is beyond ASCII letters and digits (ECMA-262 B.2.1.1 step 2).</summary>
    private static ReadOnlySpan<char> EscapeUnescaped => "@*_+-./";

    private static ReadOnlySpan<char> UpperHexDigits => "0123456789ABCDEF";

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 19.2.6; IP=Low; Security=Low; Resources=1; Fingerprint=5A0616
    // Broiler-Human:        PENDING
    private static ResultStatus Refuse(int position, string messageId, out string result, out Failure failure)
    {
        result = string.Empty;
        failure = new Failure(ResultStatus.Malformed, position, messageId);
        return ResultStatus.Malformed;
    }
}
