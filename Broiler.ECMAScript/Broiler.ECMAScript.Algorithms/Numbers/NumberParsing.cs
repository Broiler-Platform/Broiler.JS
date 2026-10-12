// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   21
// Annotated:        21/21
// Exempt:           4
// Human-reviewed:   0/21
// IP risk:          Low
// Security risk:    High
// Criteria:         3/3
// Resource impact:  3/10 max
// Unverified:       21
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Globalization;
using System.Numerics;

namespace Broiler.ECMAScript.Numbers;

/// <summary>Which ends of a string <see cref="NumberParsing.TrimStrWhiteSpace"/> trims (ECMA-262 22.1.3.32.1 <c>TrimString</c>'s <c>where</c>).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 7.1.4.1.1; IP=Low; Security=Medium; Resources=2; Fingerprint=D1256F
// Broiler-Human:        PENDING
[Flags]
public enum TrimEnds
{
    /// <summary>The start.</summary>
    Start = 1,

    /// <summary>The end.</summary>
    End = 2,

    /// <summary>Both ends.</summary>
    Both = Start | End,
}

/// <summary>
/// Strings to numbers: <c>StringToNumber</c>, the global <c>parseInt</c> and <c>parseFloat</c>, and
/// the string white-space set they trim.
/// </summary>
/// <remarks>
/// <para>
/// Every result is correctly rounded: the nearest double, ties to even, for every literal form,
/// radix strings beyond 2^53 included. Decimal digits go through the platform's correctly rounded
/// parser under the invariant number format, after this class has matched the specification's
/// grammar itself, so the platform is never asked whether something is a number (convention 9);
/// binary, octal and hexadecimal digits, and every other radix, are rounded here exactly.
/// </para>
/// <para>
/// <b>Work is charged by the caller's meter</b> (decision SJL-D-10 (a), as JSD-0065 applies it):
/// each operation charges the input's length in UTF-16 code units once, before it reads the input.
/// A <see langword="null"/> meter charges nothing.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 7.1.4.1.1; IP=Low; Security=Medium; Resources=2; Fingerprint=5015F4
// Broiler-Human:        PENDING
public static class NumberParsing
{
    /// <summary>The Unicode version of the white-space set's Space_Separator members (convention 13).</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 7.1.4.1.1; IP=Low; Security=Medium; Resources=2; Fingerprint=335916
    // Broiler-Human:        PENDING
    public const string UnicodeVersion = StrWhiteSpace.UnicodeVersion;

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 7.1.4.1.1; IP=Low; Security=Medium; Resources=2; Fingerprint=428116
    // Broiler-Human:        PENDING
    private const ulong TwoToThe53 = 1UL << 53;

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 7.1.4.1.1; IP=Low; Security=Medium; Resources=1; Fingerprint=27D05D
    // Broiler-Human:        PENDING
    private const NumberStyles DecimalStyle =
        NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent;

    /// <summary>Whether a code unit is <c>StrWhiteSpaceChar</c> (ECMA-262 7.1.4.1.1): white space or a line terminator.</summary>
    /// <param name="c">The code unit.</param>
    /// <returns><see langword="true"/> for the specification's members, from Unicode 17.0.0, and only those.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 7.1.4.1.1; IP=Low; Security=Medium; Resources=2; Fingerprint=7701D9
    // Broiler-Human:        PENDING
    public static bool IsStrWhiteSpaceChar(char c) => StrWhiteSpace.Contains(c);

    /// <summary>Removes <c>StrWhiteSpaceChar</c> code units from the chosen ends (ECMA-262 22.1.3.32.1 <c>TrimString</c>, steps 3 to 5).</summary>
    /// <param name="text">The string.</param>
    /// <param name="ends">Which ends to trim.</param>
    /// <returns>The trimmed part of <paramref name="text"/>.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 7.1.4.1.1; IP=Low; Security=Medium; Resources=2; Fingerprint=495116
    // Broiler-Human:        PENDING
    public static ReadOnlySpan<char> TrimStrWhiteSpace(ReadOnlySpan<char> text, TrimEnds ends)
    {
        var start = 0;
        var end = text.Length;
        if ((ends & TrimEnds.Start) != 0)
        {
            while (start < end && StrWhiteSpace.Contains(text[start]))
                start++;
        }

        if ((ends & TrimEnds.End) != 0)
        {
            while (end > start && StrWhiteSpace.Contains(text[end - 1]))
                end--;
        }

        return text[start..end];
    }

    /// <summary><c>StringToNumber</c> (ECMA-262 7.1.4.1.1): the value of a <c>StringNumericLiteral</c>, or NaN.</summary>
    /// <param name="text">The string.</param>
    /// <param name="charge">The caller's meter, charged the string's length; <see langword="null"/> for none.</param>
    /// <returns>The correctly rounded value; +0 for a string of white space; NaN when the string is not a <c>StringNumericLiteral</c>.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 7.1.4.1.1; IP=Low; Security=High; Resources=3; Fingerprint=0E0469
    // Broiler-Falsified-If: a string the specification's StringNumericLiteral grammar refuses is given a value other than NaN, or the meter is charged after the input is read
    // Broiler-Human:        PENDING
    public static double StringToNumber(ReadOnlySpan<char> text, WorkCharge? charge = null)
    {
        if (charge is not null && !text.IsEmpty)
            charge((ulong)text.Length);

        var trimmed = TrimStrWhiteSpace(text, TrimEnds.Both);
        if (trimmed.IsEmpty)
            return 0;

        if (trimmed.Length > 2 && trimmed[0] == '0')
        {
            var radix = PrefixRadix(trimmed[1]);
            if (radix != 0)
            {
                var digits = trimmed[2..];
                return Digits(digits, radix) == digits.Length ? NonDecimal(digits, radix) : double.NaN;
            }
        }

        var match = MatchDecimal(trimmed);
        if (match != trimmed.Length)
            return double.NaN;

        return Decimal(trimmed);
    }

    /// <summary>The global <c>parseFloat</c> (ECMA-262 19.2.4): the longest <c>StrDecimalLiteral</c> after leading white space.</summary>
    /// <param name="text">The string, already <c>ToString</c>'d.</param>
    /// <param name="charge">The caller's meter, charged the string's length; <see langword="null"/> for none.</param>
    /// <returns>The correctly rounded value of the longest prefix that is a <c>StrDecimalLiteral</c>, or NaN when none is.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 7.1.4.1.1; IP=Low; Security=Medium; Resources=2; Fingerprint=E0554C
    // Broiler-Human:        PENDING
    public static double ParseFloat(ReadOnlySpan<char> text, WorkCharge? charge = null)
    {
        if (charge is not null && !text.IsEmpty)
            charge((ulong)text.Length);

        var trimmed = TrimStrWhiteSpace(text, TrimEnds.Start);
        var match = MatchDecimal(trimmed);
        return match == 0 ? double.NaN : Decimal(trimmed[..match]);
    }

    /// <summary>The global <c>parseInt</c> (ECMA-262 19.2.5), steps 2 to 16.</summary>
    /// <param name="text">The string, already <c>ToString</c>'d.</param>
    /// <param name="radix">The radix argument after <c>ToInt32</c>; 0 when it was undefined.</param>
    /// <param name="charge">The caller's meter, charged the string's length; <see langword="null"/> for none.</param>
    /// <returns>
    /// The value of the longest run of radix digits, exactly rounded at every radix (the specification
    /// lets an implementation approximate beyond twenty decimal digits and at radixes other than 2, 4,
    /// 8, 10, 16 and 32; this one does not); -0 for a negative zero; NaN when there is no digit or the
    /// radix is outside 2 to 36.
    /// </returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 7.1.4.1.1; IP=Low; Security=High; Resources=3; Fingerprint=61FCDC
    // Broiler-Falsified-If: a run of digits beyond 2^53 is not rounded to the nearest double with ties to even, or a radix outside 2 to 36 other than 0 gives a value
    // Broiler-Human:        PENDING
    public static double ParseInt(ReadOnlySpan<char> text, int radix, WorkCharge? charge = null)
    {
        if (charge is not null && !text.IsEmpty)
            charge((ulong)text.Length);

        var s = TrimStrWhiteSpace(text, TrimEnds.Start);
        var negative = false;
        if (!s.IsEmpty && s[0] is '+' or '-')
        {
            negative = s[0] == '-';
            s = s[1..];
        }

        var stripPrefix = true;
        if (radix != 0)
        {
            if (radix is < 2 or > 36)
                return double.NaN;

            if (radix != 16)
                stripPrefix = false;
        }
        else
        {
            radix = 10;
        }

        if (stripPrefix && s.Length >= 2 && s[0] == '0' && s[1] is 'x' or 'X')
        {
            s = s[2..];
            radix = 16;
        }

        var end = Digits(s, radix);
        if (end == 0)
            return double.NaN;

        var magnitude = Integer(s[..end], radix);
        return negative ? -magnitude : magnitude;
    }

    /// <summary>The radix a <c>0x</c>, <c>0o</c> or <c>0b</c> prefix names, or 0.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 7.1.4.1.1; IP=Low; Security=Medium; Resources=2; Fingerprint=643E4C
    // Broiler-Human:        PENDING
    internal static int PrefixRadix(char letter) => letter switch
    {
        'x' or 'X' => 16,
        'o' or 'O' => 8,
        'b' or 'B' => 2,
        _ => 0,
    };

    /// <summary>The value of a digit in radixes up to 36, or 36 or more for a character that is not one.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 7.1.4.1.1; IP=Low; Security=Medium; Resources=2; Fingerprint=CFE35C
    // Broiler-Human:        PENDING
    internal static int DigitValue(char c) => c switch
    {
        >= '0' and <= '9' => c - '0',
        >= 'a' and <= 'z' => c - 'a' + 10,
        >= 'A' and <= 'Z' => c - 'A' + 10,
        _ => 36,
    };

    /// <summary>How many leading characters are digits of the radix.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 7.1.4.1.1; IP=Low; Security=Medium; Resources=2; Fingerprint=B7BD72
    // Broiler-Human:        PENDING
    internal static int Digits(ReadOnlySpan<char> text, int radix)
    {
        var at = 0;
        while (at < text.Length && DigitValue(text[at]) < radix)
            at++;

        return at;
    }

    /// <summary>
    /// The length of the longest <c>StrDecimalLiteral</c> at the start of <paramref name="text"/>:
    /// an optional sign, then <c>Infinity</c> or decimal digits with an optional fraction and
    /// exponent; 0 when there is none.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 7.1.4.1.1; IP=Low; Security=Medium; Resources=2; Fingerprint=BD4488
    // Broiler-Human:        PENDING
    internal static int MatchDecimal(ReadOnlySpan<char> text)
    {
        var at = 0;
        if (at < text.Length && text[at] is '+' or '-')
            at++;

        if (text[at..].StartsWith("Infinity", StringComparison.Ordinal))
            return at + 8;

        var integer = Digits10(text, at);
        var end = integer;
        if (end < text.Length && text[end] == '.')
        {
            var fraction = Digits10(text, end + 1);
            if (integer > at || fraction > end + 1)
                end = fraction;
        }

        if (end == at || (end == at + 1 && text[at] == '.'))
            return 0;

        if (end < text.Length && text[end] is 'e' or 'E')
        {
            var exponent = end + 1;
            if (exponent < text.Length && text[exponent] is '+' or '-')
                exponent++;

            var exponentEnd = Digits10(text, exponent);
            if (exponentEnd > exponent)
                end = exponentEnd;
        }

        return end;
    }

    /// <summary>The correctly rounded value of a matched <c>StrDecimalLiteral</c>.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 7.1.4.1.1; IP=Low; Security=Medium; Resources=2; Fingerprint=E8C40C
    // Broiler-Human:        PENDING
    internal static double Decimal(ReadOnlySpan<char> literal)
    {
        var at = literal[0] is '+' or '-' ? 1 : 0;
        if (literal[at] == 'I')
            return literal[0] == '-' ? double.NegativeInfinity : double.PositiveInfinity;

        return TryExactDecimal(literal, out var value) ? value : double.Parse(literal, DecimalStyle, NumberFormatInfo.InvariantInfo);
    }

    /// <summary>
    /// Clinger's exact case: a matched <c>StrDecimalLiteral</c> whose significant digits make an
    /// integer below 2^53 and whose decimal exponent is at most 22 in magnitude is that integer
    /// multiplied or divided by an exact power of ten, and one IEEE 754 operation on two exact
    /// operands is correctly rounded. Anything else is left to the platform's parser.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 7.1.4.1.1; IP=Low; Security=Medium; Resources=1; Fingerprint=CE700A
    // Broiler-Human:        PENDING
    internal static bool TryExactDecimal(ReadOnlySpan<char> literal, out double value)
    {
        value = 0;
        var at = 0;
        var negative = false;
        if (literal[0] is '+' or '-')
        {
            negative = literal[0] == '-';
            at = 1;
        }

        ulong significand = 0;
        var significant = 0;
        var scale = 0;
        var point = false;
        for (; at < literal.Length; at++)
        {
            var c = literal[at];
            if (c == '.')
            {
                point = true;
                continue;
            }

            if (c is < '0' or > '9')
                break;

            if (significand == 0 && c == '0')
            {
                if (point)
                    scale--;

                continue;
            }

            // Nineteen digits always fit 64 bits; whether the integer is below 2^53 is checked after.
            if (++significant > 19)
                return false;

            significand = (significand * 10) + (ulong)(c - '0');
            if (point)
                scale--;
        }

        if (at < literal.Length)
        {
            // The exponent: MatchDecimal has already checked it is digits, with a sign.
            at++;
            var exponentNegative = false;
            if (literal[at] is '+' or '-')
            {
                exponentNegative = literal[at] == '-';
                at++;
            }

            var exponent = 0;
            for (; at < literal.Length; at++)
            {
                exponent = (exponent * 10) + (literal[at] - '0');
                if (exponent > 1000)
                    return false;
            }

            scale += exponentNegative ? -exponent : exponent;
        }

        if (significand == 0)
        {
            value = negative ? -0.0 : 0.0;
            return true;
        }

        if (significand > TwoToThe53 || scale is < -22 or > 22)
            return false;

        var magnitude = scale >= 0 ? significand * ExactPowersOfTen[scale] : significand / ExactPowersOfTen[-scale];
        value = negative ? -magnitude : magnitude;
        return true;
    }

    /// <summary>10^0 to 10^22, every power of ten a double holds exactly.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 7.1.4.1.1; IP=Low; Security=Medium; Resources=1; Fingerprint=D87D20
    // Broiler-Human:        PENDING
    private static ReadOnlySpan<double> ExactPowersOfTen =>
    [
        1e0, 1e1, 1e2, 1e3, 1e4, 1e5, 1e6, 1e7, 1e8, 1e9, 1e10, 1e11,
        1e12, 1e13, 1e14, 1e15, 1e16, 1e17, 1e18, 1e19, 1e20, 1e21, 1e22,
    ];

    /// <summary>The correctly rounded value of binary, octal or hexadecimal digits, all valid.</summary>
    internal static double NonDecimal(ReadOnlySpan<char> digits, int radix) => Integer(digits, radix);

    /// <summary>The correctly rounded value of a run of valid digits in any radix from 2 to 36.</summary>
    /// <remarks>
    /// Twenty-four digits in any radix up to 36 make an integer below 2^128, which is accumulated
    /// exactly; a longer run is accumulated exactly in a big number, a limb's worth of digits at a
    /// time. Either is rounded once, from its leading 64 bits and whether anything below them is set.
    /// Past 2^1100 the value is +Infinity whatever follows, so accumulation stops there.
    /// </remarks>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 19.2.5; IP=Low; Security=High; Resources=3; Fingerprint=D21CA7
    // Broiler-Falsified-If: a run of digits is rounded twice, or not to the nearest double with ties to even, or is accumulated past 2^1100 instead of answering +Infinity
    // Broiler-Human:        PENDING
    internal static double Integer(ReadOnlySpan<char> digits, int radix)
    {
        var first = 0;
        while (first < digits.Length && digits[first] == '0')
            first++;

        digits = digits[first..];
        if (digits.Length <= NarrowDigits)
        {
            UInt128 value = 0;
            foreach (var c in digits)
                value = (value * (uint)radix) + (uint)DigitValue(c);

            return ExactRounding.ToDouble(value);
        }

        // The largest power of the radix that fits a limb, and how many digits it holds.
        var chunk = (uint)radix;
        var chunkDigits = 1;
        while ((ulong)chunk * (uint)radix <= uint.MaxValue)
        {
            chunk *= (uint)radix;
            chunkDigits++;
        }

        Span<uint> storage = stackalloc uint[BigNumber.Capacity];
        var big = new BigNumber(storage);
        for (var at = 0; at < digits.Length; at += chunkDigits)
        {
            var take = Math.Min(chunkDigits, digits.Length - at);
            uint part = 0;
            uint scale = 1;
            for (var k = 0; k < take; k++)
            {
                part = (part * (uint)radix) + (uint)DigitValue(digits[at + k]);
                scale *= (uint)radix;
            }

            big.Multiply(scale);
            big.AddSmall(part);
            if (big.BitLength > InfiniteBits)
                return double.PositiveInfinity;
        }

        return big.ToDouble();
    }

    /// <summary>The most digits in any radix up to 36 that always make an integer below 2^128: 36^24 &lt; 2^128.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 19.2.5; IP=Low; Security=Medium; Resources=1; Fingerprint=33FF6F
    // Broiler-Human:        PENDING
    private const int NarrowDigits = 24;

    /// <summary>The bit length past which an integer rounds to +Infinity whatever its lower bits are.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 19.2.5; IP=Low; Security=Medium; Resources=1; Fingerprint=549D0D
    // Broiler-Human:        PENDING
    private const int InfiniteBits = 1100;

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 7.1.4.1.1; IP=Low; Security=Medium; Resources=1; Fingerprint=7BDD63
    // Broiler-Human:        PENDING
    private static int Digits10(ReadOnlySpan<char> text, int at)
    {
        while (at < text.Length && text[at] is >= '0' and <= '9')
            at++;

        return at;
    }
}
