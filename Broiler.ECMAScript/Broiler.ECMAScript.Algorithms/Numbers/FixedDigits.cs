// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   10
// Annotated:        10/10
// Exempt:           0
// Human-reviewed:   0/10
// IP risk:          Low
// Security risk:    Medium
// Criteria:         0/0
// Resource impact:  1/10 max
// Unverified:       10
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Globalization;

namespace Broiler.ECMAScript.Numbers;

/// <summary>
/// Digit generation for <c>Number.prototype.toFixed</c>, <c>toExponential</c> and
/// <c>toPrecision</c> (ECMA-262 21.1.3.3, 21.1.3.2 and 21.1.3.5): a finite magnitude rounded to a
/// number of decimal places or significant digits, exactly, with the specification's tie rule.
/// </summary>
/// <remarks>
/// <para>
/// <b>The platform's digits, and the specification's tie.</b> .NET's <c>"F"</c> and <c>"E"</c>
/// formats write the exact decimal digits of a double at any precision, under the invariant number
/// format; but they break an exact half-way case to even, and the specification asks for the
/// larger of the two candidates. A half-way case is therefore detected exactly: writing the value as
/// <c>m x 2^p</c> with <c>m</c> odd, it is half way at <c>d</c> decimal places only when
/// <c>p + d + 1 = 0</c>, and for a negative <c>d</c> only when <c>5^-d</c> divides <c>m</c> as well.
/// A half-way value's expansion ends one digit after the cut, so it is written one digit longer,
/// the final 5 dropped and the rest incremented.
/// </para>
/// <para>
/// This is Broiler.VM's route before this library, which bundle <c>sjl-1-001</c>'s vectors pass in
/// both engines, kept over Broiler.JS's exact-rational route because it is the faster of the two
/// (JSD-0065, decision 4). The vectors pin the platform's behaviour, as convention 9 asks.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=Ported; Spec=ECMA-262 21.1.3.3 via Broiler.VM JsRealm.Number; IP=Low; Security=Medium; Resources=1; Fingerprint=68E0F3
// Broiler-Human:        PENDING
internal static class FixedDigits
{
    /// <summary>The largest t for which 5^t can divide a 53-bit odd significand.</summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 21.1.3.3 via Broiler.VM JsRealm.Number; IP=Low; Security=Medium; Resources=1; Fingerprint=3406D0
    // Broiler-Human:        PENDING
    private const int FivePowerLimit = 22;

    /// <summary>The longest rendering the platform writes for a digit count this area accepts.</summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 21.1.3.3 via Broiler.VM JsRealm.Number; IP=Low; Security=Medium; Resources=1; Fingerprint=ED60D9
    // Broiler-Human:        PENDING
    internal const int BufferLength = 160;

    /// <summary>
    /// Writes a non-negative finite magnitude below 10^21 rounded to <paramref name="decimals"/>
    /// places, with the point when there is a fraction: <c>toFixed</c>'s steps 10 to 12.
    /// </summary>
    /// <returns>The number of characters written.</returns>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 21.1.3.3 via Broiler.VM JsRealm.Number; IP=Low; Security=Medium; Resources=1; Fingerprint=076D79
    // Broiler-Human:        PENDING
    internal static int Fixed(double magnitude, int decimals, Span<char> destination)
    {
        if (IsMidpoint(magnitude, decimals))
        {
            // The value has exactly one more decimal, a 5: write it, drop it, and round up.
            var written = Format(magnitude, 'F', decimals + 1, destination);
            written -= decimals == 0 ? 2 : 1;
            IncrementFixed(destination, ref written);
            return written;
        }

        return Format(magnitude, 'F', decimals, destination);
    }

    /// <summary>
    /// Rounds a positive finite magnitude to <paramref name="count"/> significant digits, the
    /// larger candidate on a tie.
    /// </summary>
    /// <param name="magnitude">The value.</param>
    /// <param name="count">The significant digits wanted, 1 to 101.</param>
    /// <param name="digits">Receives exactly <paramref name="count"/> digits.</param>
    /// <param name="exponent">Receives the decimal exponent of the first digit.</param>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 21.1.3.3 via Broiler.VM JsRealm.Number; IP=Low; Security=Medium; Resources=1; Fingerprint=E46B6C
    // Broiler-Human:        PENDING
    internal static void Significant(double magnitude, int count, Span<char> digits, out int exponent)
    {
        Span<char> rendered = stackalloc char[BufferLength];
        var written = Format(magnitude, 'E', count - 1, rendered);
        Split(rendered[..written], digits, out exponent);
        if (!IsMidpoint(magnitude, count - 1 - exponent))
            return;

        // A half-way case ends one digit after the cut, so the longer rendering is exact; its last
        // digit is the 5 the platform broke to even. The exponent is read again, because the
        // shorter rendering may have carried into it.
        written = Format(magnitude, 'E', count, rendered);
        Span<char> exact = stackalloc char[count + 1];
        Split(rendered[..written], exact, out exponent);
        exact[..count].CopyTo(digits);
        if (IncrementDigits(digits[..count]))
        {
            // Every digit was a nine: the count stays and the exponent moves.
            digits[0] = '1';
            digits[1..count].Fill('0');
            exponent++;
        }
    }

    /// <summary>Formats with an invariant standard format of the given precision.</summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 21.1.3.3 via Broiler.VM JsRealm.Number; IP=Low; Security=Medium; Resources=1; Fingerprint=D2F3F5
    // Broiler-Human:        PENDING
    private static int Format(double value, char specifier, int precision, Span<char> destination)
    {
        Span<char> format = stackalloc char[4];
        format[0] = specifier;
        precision.TryFormat(format[1..], out var length, default, NumberFormatInfo.InvariantInfo);
        if (!value.TryFormat(destination, out var written, format[..(1 + length)], NumberFormatInfo.InvariantInfo))
            throw new InvalidOperationException("A digit buffer was too short for the platform's rendering.");

        return written;
    }

    /// <summary>Splits an <c>"E"</c> rendering into its digits, without the point, and its exponent.</summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 21.1.3.3 via Broiler.VM JsRealm.Number; IP=Low; Security=Medium; Resources=1; Fingerprint=8705DE
    // Broiler-Human:        PENDING
    private static void Split(ReadOnlySpan<char> rendered, Span<char> digits, out int exponent)
    {
        var e = rendered.IndexOf('E');
        var mantissa = rendered[..e];
        digits[0] = mantissa[0];
        if (mantissa.Length > 1)
            mantissa[2..].CopyTo(digits[1..]);

        exponent = int.Parse(rendered[(e + 1)..], NumberStyles.AllowLeadingSign, NumberFormatInfo.InvariantInfo);
    }

    /// <summary>
    /// Whether a positive finite magnitude lies exactly half way between two values
    /// <paramref name="decimals"/> decimal places apart.
    /// </summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 21.1.3.3 via Broiler.VM JsRealm.Number; IP=Low; Security=Medium; Resources=1; Fingerprint=95B984
    // Broiler-Human:        PENDING
    private static bool IsMidpoint(double magnitude, int decimals)
    {
        if (magnitude == 0)
            return false;

        var bits = DoubleBits.Of(magnitude);
        var significand = bits.Significand;
        var exponent = bits.Exponent;
        while ((significand & 1) == 0)
        {
            significand >>= 1;
            exponent++;
        }

        if (exponent + decimals + 1 != 0)
            return false;

        if (decimals >= 0)
            return true;

        var power = -decimals;
        if (power > FivePowerLimit)
            return false;

        ulong divisor = 1;
        for (var step = 0; step < power; step++)
            divisor *= 5;

        return significand % divisor == 0;
    }

    /// <summary>Adds one to the last place of a fixed rendering, carrying across the point.</summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 21.1.3.3 via Broiler.VM JsRealm.Number; IP=Low; Security=Medium; Resources=1; Fingerprint=434B09
    // Broiler-Human:        PENDING
    private static void IncrementFixed(Span<char> text, ref int length)
    {
        for (var at = length - 1; at >= 0; at--)
        {
            if (text[at] == '.')
                continue;

            if (text[at] != '9')
            {
                text[at]++;
                return;
            }

            text[at] = '0';
        }

        text[..length].CopyTo(text[1..]);
        text[0] = '1';
        length++;
    }

    /// <summary>Adds one to a digit string; true when every digit was a nine and it wrapped.</summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 21.1.3.3 via Broiler.VM JsRealm.Number; IP=Low; Security=Medium; Resources=1; Fingerprint=6CFEA2
    // Broiler-Human:        PENDING
    private static bool IncrementDigits(Span<char> digits)
    {
        for (var at = digits.Length - 1; at >= 0; at--)
        {
            if (digits[at] != '9')
            {
                digits[at]++;
                return false;
            }

            digits[at] = '0';
        }

        return true;
    }
}
