// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   12
// Annotated:        12/12
// Exempt:           0
// Human-reviewed:   0/12
// IP risk:          Low
// Security risk:    Medium
// Criteria:         0/0
// Resource impact:  1/10 max
// Unverified:       12
//
// GENERATED - DO NOT EDIT MANUALLY

using System;

namespace Broiler.ECMAScript.Numbers;

/// <summary>
/// Numbers to strings: <c>Number::toString</c> at every radix and the digit generation of
/// <c>Number.prototype.toFixed</c>, <c>toExponential</c> and <c>toPrecision</c>.
/// </summary>
/// <remarks>
/// <para>
/// Every input is already converted and validated, which is the engine adapter's work: the
/// receiver check, <c>ToIntegerOrInfinity</c> of the argument in the specification's order, and
/// the <c>RangeError</c>. A digit count or radix outside the range a member documents is a broken
/// precondition and throws <see cref="ArgumentOutOfRangeException"/>, which no script can cause
/// through a correct adapter.
/// </para>
/// <para>
/// Formatting a double does bounded work, so no member takes a meter (decision SJL-D-10, as
/// JSD-0065 applies it). Results depend on the value and the digit count only: no culture, no host
/// Unicode data and no state.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.6.1.20; IP=Low; Security=Medium; Resources=1; Fingerprint=3D11DD
// Broiler-Human:        PENDING
public static class NumberText
{
    /// <summary>The longest string <see cref="TryFormat"/> ever writes: a sign, seventeen digits, a point and an exponent.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.6.1.20; IP=Low; Security=Medium; Resources=1; Fingerprint=09EE93
    // Broiler-Human:        PENDING
    public const int MaximumLength = 26;

    /// <summary><c>Number::toString(x, 10)</c> (ECMA-262 6.1.6.1.20).</summary>
    /// <param name="value">Any Number.</param>
    /// <returns>The shortest digits that read back as the value, nearest the value when several are shortest, placed by the specification's rules.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.6.1.20; IP=Low; Security=Medium; Resources=1; Fingerprint=6327CE
    // Broiler-Human:        PENDING
    public static string ToString(double value)
    {
        Span<char> buffer = stackalloc char[MaximumLength];
        TryFormat(value, buffer, out var written);
        return new string(buffer[..written]);
    }

    /// <summary><c>Number::toString(x, 10)</c> (ECMA-262 6.1.6.1.20), written into a caller's buffer.</summary>
    /// <param name="value">Any Number.</param>
    /// <param name="destination">The buffer; <see cref="MaximumLength"/> characters always suffice.</param>
    /// <param name="written">The characters written.</param>
    /// <returns><see langword="false"/> when the buffer is too short, in which case nothing is written.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.6.1.20; IP=Low; Security=Medium; Resources=1; Fingerprint=ED879E
    // Broiler-Human:        PENDING
    public static bool TryFormat(double value, Span<char> destination, out int written)
    {
        Span<char> text = stackalloc char[MaximumLength];
        var length = Format(value, text);
        if (length > destination.Length)
        {
            written = 0;
            return false;
        }

        text[..length].CopyTo(destination);
        written = length;
        return true;
    }

    /// <summary><c>Number::toString(x, radix)</c> (ECMA-262 6.1.6.1.20) for a validated radix.</summary>
    /// <param name="value">Any Number.</param>
    /// <param name="radix">The radix, 2 to 36.</param>
    /// <returns>The shortest digits in the radix that read back as the value, placed positionally; radix 10 is <see cref="ToString(double)"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The radix is outside 2 to 36.</exception>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.6.1.20; IP=Low; Security=Medium; Resources=1; Fingerprint=25AF45
    // Broiler-Human:        PENDING
    public static string ToRadixString(double value, int radix)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(radix, 2);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(radix, 36);
        if (radix == 10 || !double.IsFinite(value) || value == 0)
            return ToString(value);

        return value < 0 ? "-" + RadixDigits.Format(-value, radix) : RadixDigits.Format(value, radix);
    }

    /// <summary>The digits of <c>Number.prototype.toFixed</c> (ECMA-262 21.1.3.3), steps 5 to 12, for a validated digit count.</summary>
    /// <param name="value">Any Number; a non-finite value and one of magnitude 10^21 or more give <c>Number::toString</c>, as the specification's steps 6 and 9 do.</param>
    /// <param name="fractionDigits">The places after the point, 0 to 100.</param>
    /// <returns>The value rounded to that many places, the larger candidate on a tie, with a "-" for a value below zero.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The digit count is outside 0 to 100.</exception>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.6.1.20; IP=Low; Security=Medium; Resources=1; Fingerprint=48699C
    // Broiler-Human:        PENDING
    public static string ToFixed(double value, int fractionDigits)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(fractionDigits);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(fractionDigits, 100);
        if (!double.IsFinite(value))
            return ToString(value);

        // -0 is not below zero, so it loses its sign here, as the specification's step 7 has it.
        var negative = value < 0;
        var magnitude = Math.Abs(value);
        if (magnitude >= 1e21)
            return negative ? "-" + ToString(magnitude) : ToString(magnitude);

        Span<char> buffer = stackalloc char[FixedDigits.BufferLength + 1];
        var written = FixedDigits.Fixed(magnitude, fractionDigits, buffer[1..]);
        if (!negative)
            return new string(buffer.Slice(1, written));

        buffer[0] = '-';
        return new string(buffer[..(written + 1)]);
    }

    /// <summary>
    /// <c>Number.prototype.toExponential</c> (ECMA-262 21.1.3.2) with an undefined digit count:
    /// as many digits as the value needs to be read back.
    /// </summary>
    /// <param name="value">Any Number; a non-finite value gives <c>Number::toString</c>, as step 4 does.</param>
    /// <returns>The shortest digits that read back as the value, written <c>d.ddde+x or d.ddde-x</c>.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.6.1.20; IP=Low; Security=Medium; Resources=1; Fingerprint=28C272
    // Broiler-Human:        PENDING
    public static string ToExponential(double value)
    {
        if (!double.IsFinite(value))
            return ToString(value);

        var negative = value < 0;
        var magnitude = Math.Abs(value);
        Span<char> digits = stackalloc char[20];
        int count;
        int exponent;
        if (magnitude == 0)
        {
            digits[0] = '0';
            count = 1;
            exponent = 0;
        }
        else
        {
            var significand = ShortestDecimal.Digits(magnitude, out var last);
            count = ShortestDecimal.DecimalLength(significand);
            significand.TryFormat(digits, out _, default, System.Globalization.NumberFormatInfo.InvariantInfo);
            exponent = last + count - 1;
        }

        return Exponential(negative, digits[..count], exponent);
    }

    /// <summary>
    /// <c>Number.prototype.toExponential</c> (ECMA-262 21.1.3.2), steps 5 to 15, for a validated
    /// digit count.
    /// </summary>
    /// <param name="value">Any Number; a non-finite value gives <c>Number::toString</c>, as step 4 does.</param>
    /// <param name="fractionDigits">The digits after the point, 0 to 100.</param>
    /// <returns>The value rounded to <paramref name="fractionDigits"/> + 1 significant digits, the larger candidate on a tie, written <c>d.ddde+x or d.ddde-x</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The digit count is outside 0 to 100.</exception>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.6.1.20; IP=Low; Security=Medium; Resources=1; Fingerprint=1372F6
    // Broiler-Human:        PENDING
    public static string ToExponential(double value, int fractionDigits)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(fractionDigits);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(fractionDigits, 100);
        if (!double.IsFinite(value))
            return ToString(value);

        var negative = value < 0;
        var magnitude = Math.Abs(value);
        Span<char> digits = stackalloc char[fractionDigits + 1];
        int exponent;
        if (magnitude == 0)
        {
            digits.Fill('0');
            exponent = 0;
        }
        else
        {
            FixedDigits.Significant(magnitude, fractionDigits + 1, digits, out exponent);
        }

        return Exponential(negative, digits, exponent);
    }

    /// <summary><c>Number.prototype.toPrecision</c> (ECMA-262 21.1.3.5), steps 5 to 13, for a validated precision.</summary>
    /// <param name="value">Any Number; a non-finite value gives <c>Number::toString</c>, as step 4 does.</param>
    /// <param name="precision">The significant digits, 1 to 100.</param>
    /// <returns>The value rounded to that many significant digits, the larger candidate on a tie, in fixed or exponential notation as step 10 chooses.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The precision is outside 1 to 100.</exception>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.6.1.20; IP=Low; Security=Medium; Resources=1; Fingerprint=2796B6
    // Broiler-Human:        PENDING
    public static string ToPrecision(double value, int precision)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(precision, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(precision, 100);
        if (!double.IsFinite(value))
            return ToString(value);

        var negative = value < 0;
        var magnitude = Math.Abs(value);
        Span<char> digits = stackalloc char[precision];
        int exponent;
        if (magnitude == 0)
        {
            digits.Fill('0');
            exponent = 0;
        }
        else
        {
            FixedDigits.Significant(magnitude, precision, digits, out exponent);
            if (exponent < -6 || exponent >= precision)
                return Exponential(negative, digits, exponent);
        }

        var sign = negative ? "-" : string.Empty;
        if (exponent == precision - 1)
            return string.Concat(sign, digits);

        if (exponent >= 0)
            return string.Concat(sign, digits[..(exponent + 1)], ".", digits[(exponent + 1)..]);

        return string.Concat(sign, "0.", new string('0', -(exponent + 1)), digits);
    }

    /// <summary>Writes <c>d.ddde+x or d.ddde-x</c>, the form <c>toExponential</c> and <c>toPrecision</c> share.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.6.1.20; IP=Low; Security=Medium; Resources=1; Fingerprint=83B414
    // Broiler-Human:        PENDING
    private static string Exponential(bool negative, ReadOnlySpan<char> digits, int exponent)
    {
        Span<char> text = stackalloc char[digits.Length + 9];
        var at = 0;
        if (negative)
            text[at++] = '-';

        text[at++] = digits[0];
        if (digits.Length > 1)
        {
            text[at++] = '.';
            digits[1..].CopyTo(text[at..]);
            at += digits.Length - 1;
        }

        text[at++] = 'e';
        text[at++] = exponent < 0 ? '-' : '+';
        Math.Abs(exponent).TryFormat(text[at..], out var written, default, System.Globalization.NumberFormatInfo.InvariantInfo);
        return new string(text[..(at + written)]);
    }

    /// <summary>Writes <c>Number::toString(x, 10)</c>; the buffer holds <see cref="MaximumLength"/> characters.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.6.1.20; IP=Low; Security=Medium; Resources=1; Fingerprint=C004BD
    // Broiler-Human:        PENDING
    private static int Format(double value, Span<char> text)
    {
        if (double.IsNaN(value))
            return Write(text, "NaN");

        if (value == 0)
            return Write(text, "0");

        var at = 0;
        if (value < 0)
        {
            text[at++] = '-';
            value = -value;
        }

        if (double.IsPositiveInfinity(value))
            return at + Write(text[at..], "Infinity");

        // An integer below 2^53 is its own shortest digits: every other number its digits could
        // spell is at least one away, and its rounding interval is at most one wide.
        if (value < 9007199254740992.0 && value == Math.Floor(value))
        {
            ((ulong)value).TryFormat(text[at..], out var integerLength, default, System.Globalization.NumberFormatInfo.InvariantInfo);
            return at + integerLength;
        }

        var significand = ShortestDecimal.Digits(value, out var last);
        var k = ShortestDecimal.DecimalLength(significand);
        var n = last + k;
        Span<char> digits = stackalloc char[20];
        significand.TryFormat(digits, out _, default, System.Globalization.NumberFormatInfo.InvariantInfo);
        digits = digits[..k];

        if (k <= n && n <= 21)
        {
            digits.CopyTo(text[at..]);
            text.Slice(at + k, n - k).Fill('0');
            return at + n;
        }

        if (0 < n && n <= 21)
        {
            digits[..n].CopyTo(text[at..]);
            text[at + n] = '.';
            digits[n..].CopyTo(text[(at + n + 1)..]);
            return at + k + 1;
        }

        if (-6 < n && n <= 0)
        {
            text[at++] = '0';
            text[at++] = '.';
            text.Slice(at, -n).Fill('0');
            at -= n;
            digits.CopyTo(text[at..]);
            return at + k;
        }

        text[at++] = digits[0];
        if (k > 1)
        {
            text[at++] = '.';
            digits[1..].CopyTo(text[at..]);
            at += k - 1;
        }

        text[at++] = 'e';
        text[at++] = n - 1 < 0 ? '-' : '+';
        Math.Abs(n - 1).TryFormat(text[at..], out var exponentLength, default, System.Globalization.NumberFormatInfo.InvariantInfo);
        return at + exponentLength;
    }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.6.1.20; IP=Low; Security=Medium; Resources=1; Fingerprint=A49790
    // Broiler-Human:        PENDING
    private static int Write(Span<char> text, string literal)
    {
        literal.AsSpan().CopyTo(text);
        return literal.Length;
    }
}
