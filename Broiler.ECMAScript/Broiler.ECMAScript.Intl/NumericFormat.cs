// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// HAIRS Code Assurance
// --------------------
// Relevant units:   9
// Annotated:        9/9
// Exempt:           0
// Human-reviewed:   0/9
// IP risk:          Low
// Security risk:    High
// Criteria:         1/1
// Resource impact:  2/10 max
// Unverified:       9
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using Broiler.ECMAScript.Numbers;

namespace Broiler.ECMAScript.Intl;

/// <summary>What FormatNumericToString answers (<c>sec-formatnumerictostring</c>): the rounded value, with its sign, and its decimal String.</summary>
/// <param name="RoundedNumber">[[RoundedNumber]]: the value rounded as the digit options say, with the input's sign.</param>
/// <param name="FormattedString">[[FormattedString]]: ASCII digits with an optional <c>.</c>, without a sign.</param>
// Hairs-AI:           Origin=AI; Spec=ECMA-402 sec-formatnumerictostring records; IP=Low; Security=Low; Resources=0; Fingerprint=775832
// Hairs-Human:        PENDING
public readonly record struct FormattedNumeric(IntlDecimal RoundedNumber, string FormattedString);

/// <summary>
/// ECMA-402's FormatNumericToString (<c>sec-formatnumerictostring</c>), with ToRawPrecision (<c>sec-torawprecision</c>), ToRawFixed
/// (<c>sec-torawfixed</c>), the rounding priority, the rounding increment, <c>trailingZeroDisplay</c> and the minimum integer
/// digits, over an Intl mathematical value (rows M6, M7 and M8). The rounding modes are the shared library's: the signed mode
/// is made unsigned by <see cref="ExactRounding.GetUnsignedRoundingMode"/>, and ApplyUnsignedRoundingMode's choice is made over
/// the decimal digits.
/// </summary>
/// <remarks>
/// <para>
/// <b>Ported from Broiler.VM's <c>JsNumberFormatter.FormatNumericToString</c> and <c>JsRawDecimal</c></b> at its SJL-9 rule
/// commit <c>4cf9ca44</c>, with the options as a <see cref="DigitOptions"/> record where it read its formatter's strings, and the
/// caller's <see cref="WorkCharge"/> where it charged its engine's meter, in the same units (decision D18).
/// </para>
/// </remarks>
// Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-formatnumerictostring sec-torawfixed sec-torawprecision via Broiler.VM JsNumberFormatter JsRawDecimal; IP=Low; Security=Low; Resources=0; Fingerprint=345121
// Hairs-Human:        PENDING
public static class NumericFormat
{
    /// <summary>
    /// FormatNumericToString (<c>sec-formatnumerictostring</c>): the value rounded by the digit options, as a decimal String and
    /// as the rounded value with the input's sign; negative zero stays negative (row M8).
    /// </summary>
    /// <param name="options">The digit options, as SetNumberFormatDigitOptions resolved them.</param>
    /// <param name="x">A finite value.</param>
    /// <param name="charge">The caller's meter: <c>digits / 16 + 1</c> units, the value's significant digits, before any work (Broiler.VM's <c>JsNumberFormatter.cs:821</c>).</param>
    /// <returns>The rounded value and its String.</returns>
    /// <exception cref="ArgumentException"><paramref name="x"/> is not finite, or the options lack a pair their rounding type reads.</exception>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-formatnumerictostring sec-torawfixed sec-torawprecision via Broiler.VM JsNumberFormatter JsRawDecimal; IP=Low; Security=High; Resources=2; Fingerprint=854FCF
    // Hairs-Falsified-If: the String or the rounded value differs from FormatNumericToString's for a value and digit options, or the charge differs from digits / 16 + 1
    // Hairs-Human:        PENDING
    public static FormattedNumeric FormatNumericToString(DigitOptions options, IntlDecimal x, WorkCharge? charge = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!x.IsFinite)
        {
            throw new ArgumentException("FormatNumericToString takes a finite value", nameof(x));
        }

        var negative = x.IsNegative;
        var magnitude = x.Abs();
        charge?.Invoke((ulong)(magnitude.Digits.Length / 16) + 1);

        var mode = ExactRounding.GetUnsignedRoundingMode(options.RoundingMode, negative);
        RawFormat result;

        switch (options.RoundingType)
        {
            case NumberRoundingType.SignificantDigits:
                result = ToRawPrecision(magnitude, Required(options.MinimumSignificantDigits), Required(options.MaximumSignificantDigits), mode);
                break;
            case NumberRoundingType.FractionDigits:
                result = ToRawFixed(magnitude, Required(options.MinimumFractionDigits), Required(options.MaximumFractionDigits), options.RoundingIncrement, mode);
                break;
            default:
                var precision = ToRawPrecision(magnitude, Required(options.MinimumSignificantDigits), Required(options.MaximumSignificantDigits), mode);
                var fixedResult = ToRawFixed(magnitude, Required(options.MinimumFractionDigits), Required(options.MaximumFractionDigits), options.RoundingIncrement, mode);
                var fixedIsMorePrecise = fixedResult.RoundingMagnitude < precision.RoundingMagnitude;
                result = options.RoundingType == NumberRoundingType.MorePrecision
                    ? (fixedIsMorePrecise ? fixedResult : precision)
                    : (fixedIsMorePrecise ? precision : fixedResult);
                break;
        }

        var text = result.Formatted;

        if (options.TrailingZeroDisplay == TrailingZeroDisplay.StripIfInteger && IsInteger(result.Rounded))
        {
            var point = text.IndexOf('.', StringComparison.Ordinal);

            if (point >= 0)
            {
                text = text[..point];
            }
        }

        if (result.IntegerDigits < options.MinimumIntegerDigits)
        {
            text = new string('0', options.MinimumIntegerDigits - result.IntegerDigits) + text;
        }

        return new FormattedNumeric(result.Rounded.WithSign(negative), text);
    }

    // Hairs-AI:           Origin=AI; Spec=ECMA-402 sec-formatnumerictostring records; IP=Low; Security=Low; Resources=1; Fingerprint=25F9CF
    // Hairs-Human:        PENDING
    private static int Required(int? value) =>
        value ?? throw new ArgumentException("the digit options lack a pair their rounding type reads", "options");

    /// <summary>Whether a rounded value is an integer.</summary>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-formatnumerictostring sec-torawfixed sec-torawprecision via Broiler.VM JsNumberFormatter JsRawDecimal; IP=Low; Security=Low; Resources=1; Fingerprint=325FAC
    // Hairs-Human:        PENDING
    private static bool IsInteger(IntlDecimal value) => value.IsZero || value.Point >= value.Digits.Length;

    /// <summary>ECMA-402's ToRawFixed (<c>sec-torawfixed</c>) of a nonnegative value.</summary>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-formatnumerictostring sec-torawfixed sec-torawprecision via Broiler.VM JsNumberFormatter JsRawDecimal; IP=Low; Security=Medium; Resources=2; Fingerprint=125F3A
    // Hairs-Human:        PENDING
    internal static RawFormat ToRawFixed(IntlDecimal x, int minFraction, int maxFraction, int increment, UnsignedRoundingMode mode)
    {
        var f = maxFraction;
        var n = x.RoundToUnits(-f, increment, mode);
        var rounded = IntlDecimal.Finite(false, n, n.Length - f);
        var m = n;
        int integerDigits;

        if (f != 0)
        {
            var k = m.Length;

            if (k <= f)
            {
                m = new string('0', f + 1 - k) + m;
                k = f + 1;
            }

            m = m[..(k - f)] + "." + m[(k - f)..];
            integerDigits = k - f;
        }
        else
        {
            integerDigits = m.Length;
        }

        m = Cut(m, maxFraction - minFraction);
        return new RawFormat(m, rounded, integerDigits, -f);
    }

    /// <summary>ECMA-402's ToRawPrecision (<c>sec-torawprecision</c>) of a nonnegative value.</summary>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-formatnumerictostring sec-torawfixed sec-torawprecision via Broiler.VM JsNumberFormatter JsRawDecimal; IP=Low; Security=Medium; Resources=2; Fingerprint=C72039
    // Hairs-Human:        PENDING
    internal static RawFormat ToRawPrecision(IntlDecimal x, int minPrecision, int maxPrecision, UnsignedRoundingMode mode)
    {
        var p = maxPrecision;
        string n;
        int e;
        IntlDecimal rounded;

        if (x.IsZero)
        {
            n = new string('0', p);
            e = 0;
            rounded = IntlDecimal.Zero;
        }
        else
        {
            e = x.Magnitude;
            n = x.RoundToUnits(e - p + 1, 1, mode);

            // ROUNDING UP CARRIED INTO A NEW DIGIT: the same value one magnitude higher.
            if (n.Length > p)
            {
                e++;
                n = n[..p];
            }

            rounded = IntlDecimal.Finite(false, n, e + 1);
        }

        string m;
        int integerDigits;

        if (e >= p - 1)
        {
            m = n + new string('0', e - p + 1);
            integerDigits = e + 1;
        }
        else if (e >= 0)
        {
            m = n[..(e + 1)] + "." + n[(e + 1)..];
            integerDigits = e + 1;
        }
        else
        {
            m = "0." + new string('0', -(e + 1)) + n;
            integerDigits = 1;
        }

        if (m.Contains('.', StringComparison.Ordinal) && maxPrecision > minPrecision)
        {
            m = Cut(m, maxPrecision - minPrecision);
        }

        return new RawFormat(m, rounded, integerDigits, e - p + 1);
    }

    /// <summary>Removes up to <paramref name="cut"/> trailing zeros after a point, then a trailing point.</summary>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-formatnumerictostring sec-torawfixed sec-torawprecision via Broiler.VM JsNumberFormatter JsRawDecimal; IP=Low; Security=Low; Resources=1; Fingerprint=B0D5DE
    // Hairs-Human:        PENDING
    private static string Cut(string m, int cut)
    {
        var end = m.Length;

        if (m.Contains('.', StringComparison.Ordinal))
        {
            while (cut > 0 && end > 0 && m[end - 1] == '0')
            {
                end--;
                cut--;
            }

            if (end > 0 && m[end - 1] == '.')
            {
                end--;
            }
        }

        return m[..end];
    }

    /// <summary>What ToRawFixed and ToRawPrecision answer: [[FormattedString]], [[RoundedNumber]], [[IntegerDigitsCount]] and [[RoundingMagnitude]].</summary>
    // Hairs-AI:           Origin=AI; Spec=ECMA-402 sec-formatnumerictostring records; IP=Low; Security=Low; Resources=0; Fingerprint=C70924
    // Hairs-Human:        PENDING
    internal readonly record struct RawFormat(string Formatted, IntlDecimal Rounded, int IntegerDigits, int RoundingMagnitude);
}
