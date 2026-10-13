// REFERENCE ARM V: a test-only copy of Broiler.VM's implementation before the cut-over, for the
// reference-arm comparison of bundle sjl-3-001 (docs/evidence/sjl-3-001/reference-arms.md). It is
// never referenced outside this test project, and it is retired at SJL-16.
//
// Copied from Broiler.VM at 0087e8d7 (bundle sjl-0-001's base):
//   src/Broiler.VM.Profile.JavaScript/JsRealm.Number.cs
//     blob 8173f7031e09b681f3f24e1d2627c12e22a60917: the constants, and the statics from NumberIsIntegral to NumberIncrementFixed
//
// Changes from the source, none of which changes a result:
//   - the namespace; the statics sit in a static class of their own instead of the partial class JsRealm, whose other members are the engine's; NumberFixedText, NumberExponentialText and NumberPrecisionText made internal so the arm can call them;
//   - the generated assurance header and the per-member assurance annotations removed;
//   - the built-in methods that call these statics are not copied: their coercion, RangeErrors and charging are the adapter's and are not compared (the rule's 'operations' line). The arm's entry points in ReferenceArms/Arms.cs call NumberFixedText, NumberExponentialText (digits -1 when absent) and NumberPrecisionText exactly as the built-ins did after validating.

using System.Globalization;

namespace Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.V;

/// <summary>The digit generation of Broiler.VM's <c>Number.prototype.toFixed</c>, <c>toExponential</c> and <c>toPrecision</c>.</summary>
internal static class JsRealmNumber
{
    /// <summary>Two to the fifty-third, less one: the largest integer a binary64 holds alone.</summary>
    private const double NumberMaxSafe = 9007199254740991.0;

    /// <summary>Two to the minus fifty-second: the gap between one and the next Number.</summary>
    private const double NumberEpsilonStep = 2.220446049250313E-16;

    /// <summary>The magnitude at which <c>toFixed</c> hands the value back to <c>ToString</c>.</summary>
    private const double NumberFixedCeiling = 1e21;

    /// <summary>How many significant digits a binary64 ever needs to round-trip.</summary>
    private const int NumberRoundTripDigits = 17;

    /// <summary>The largest <c>t</c> for which <c>5^t</c> can divide a fifty-three bit odd number.</summary>
    private const int NumberFivePowerLimit = 22;

    /// <summary>The culture every conversion in this file uses, because none of them are localised.</summary>
    private static readonly CultureInfo NumberCulture = CultureInfo.InvariantCulture;

    /// <summary>Whether <paramref name="value"/> is a finite integer.</summary>
    private static bool NumberIsIntegral(double value) =>
        double.IsFinite(value) && System.Math.Truncate(value) == value;

    /// <summary><c>Number.prototype.toFixed</c>, over an already-validated digit count.</summary>
    internal static string NumberFixedText(double value, int digits)
    {
        if (double.IsNaN(value))
        {
            return "NaN";
        }

        var negative = value < 0;

        // Abs() rather than a negation, so that -0 loses its sign here and renders as "0" while
        // -0.0001 keeps the "-" the specification's `s` gives it and renders as "-0.00".
        var magnitude = System.Math.Abs(value);
        string text;

        if (magnitude >= NumberFixedCeiling)
        {
            // At and above 1e21 the answer is ToString(x), infinities included.
            text = JsNumberFormat.ToJsString(magnitude);
        }
        else if (NumberIsMidpoint(magnitude, digits))
        {
            var exact = magnitude.ToString("F" + (digits + 1).ToString(NumberCulture), NumberCulture);
            var kept = exact[..^1];

            if (digits == 0)
            {
                kept = kept[..^1];
            }

            text = NumberIncrementFixed(kept);
        }
        else
        {
            text = magnitude.ToString("F" + digits.ToString(NumberCulture), NumberCulture);
        }

        return negative ? "-" + text : text;
    }

    /// <summary><c>Number.prototype.toExponential</c>; <paramref name="digits"/> is -1 when absent.</summary>
    internal static string NumberExponentialText(double value, int digits)
    {
        var negative = value < 0;
        var magnitude = System.Math.Abs(value);
        string significant;
        int exponent;

        if (digits < 0)
        {
            significant = NumberShortestDigits(magnitude, out exponent);
        }
        else
        {
            NumberRoundSignificant(magnitude, digits + 1, out significant, out exponent);
        }

        var text = NumberPointAfterFirst(significant) + "e" + NumberExponentSuffix(exponent);
        return negative ? "-" + text : text;
    }

    /// <summary><c>Number.prototype.toPrecision</c>, over an already-validated precision.</summary>
    internal static string NumberPrecisionText(double value, int precision)
    {
        var negative = value < 0;
        var magnitude = System.Math.Abs(value);
        NumberRoundSignificant(magnitude, precision, out var digits, out var exponent);
        string text;

        if (exponent < -6 || exponent >= precision)
        {
            text = NumberPointAfterFirst(digits) + "e" + NumberExponentSuffix(exponent);
        }
        else if (exponent == precision - 1)
        {
            text = digits;
        }
        else if (exponent >= 0)
        {
            text = digits[..(exponent + 1)] + "." + digits[(exponent + 1)..];
        }
        else
        {
            text = "0." + new string('0', -(exponent + 1)) + digits;
        }

        return negative ? "-" + text : text;
    }

    /// <summary>
    /// Rounds <paramref name="magnitude"/> to <paramref name="count"/> significant digits, breaking
    /// an exact halfway case away from zero.
    /// </summary>
    /// <remarks>
    /// <paramref name="digits"/> comes back with exactly <paramref name="count"/> characters and
    /// <paramref name="exponent"/> is the decimal exponent of the first of them, so the value is
    /// <c>0.digits x 10^(exponent + 1)</c>.
    /// </remarks>
    private static void NumberRoundSignificant(double magnitude, int count, out string digits, out int exponent)
    {
        var rendered = magnitude.ToString("E" + (count - 1).ToString(NumberCulture), NumberCulture);
        NumberSplitRendered(rendered, out var mantissa, out exponent);
        digits = NumberWithoutPoint(mantissa);

        if (!NumberIsMidpoint(magnitude, count - 1 - exponent))
        {
            return;
        }

        // A halfway case terminates one digit past the cut, so THIS rendering is exact and its
        // last character is the '5' that .NET just broke to even and the language breaks upward.
        // It is also why the exponent is re-read here: the rounded rendering may have carried.
        var exact = magnitude.ToString("E" + count.ToString(NumberCulture), NumberCulture);
        NumberSplitRendered(exact, out var exactMantissa, out exponent);
        digits = NumberIncrementDigits(NumberWithoutPoint(exactMantissa)[..^1], out var carried);

        if (carried)
        {
            exponent++;
        }
    }

    /// <summary>The fewest significant digits that read back as <paramref name="magnitude"/>.</summary>
    private static string NumberShortestDigits(double magnitude, out int exponent)
    {
        for (var count = 0; count < NumberRoundTripDigits; count++)
        {
            var candidate = magnitude.ToString("E" + count.ToString(NumberCulture), NumberCulture);

            if (double.Parse(candidate, NumberStyles.Float, NumberCulture) == magnitude)
            {
                NumberSplitRendered(candidate, out var mantissa, out exponent);
                return NumberWithoutPoint(mantissa);
            }
        }

        var widest = magnitude.ToString("E" + (NumberRoundTripDigits - 1).ToString(NumberCulture), NumberCulture);

        NumberSplitRendered(widest, out var last, out exponent);
        return NumberWithoutPoint(last);
    }

    /// <summary>
    /// Whether <paramref name="magnitude"/> is exactly halfway between two values that
    /// <paramref name="decimals"/> decimal places apart can hold.
    /// </summary>
    /// <remarks>
    /// Writing the value as <c>m x 2^p</c> with <c>m</c> odd, twice the value scaled by
    /// <c>10^decimals</c> is <c>m x 5^decimals x 2^(p + decimals + 1)</c>. That is an odd integer -
    /// which is what halfway means - exactly when the power of two vanishes, and, for a negative
    /// <paramref name="decimals"/>, when the five it is divided by divides <c>m</c> as well.
    /// </remarks>
    private static bool NumberIsMidpoint(double magnitude, int decimals)
    {
        if (magnitude == 0 || !double.IsFinite(magnitude))
        {
            return false;
        }

        var bits = System.BitConverter.DoubleToInt64Bits(magnitude);
        var biased = (int)((bits >> 52) & 0x7FF);
        var significand = bits & 0xFFFFFFFFFFFFFL;
        int exponent;

        if (biased == 0)
        {
            exponent = -1074;
        }
        else
        {
            significand |= 0x10000000000000L;
            exponent = biased - 1075;
        }

        if (significand == 0)
        {
            return false;
        }

        while ((significand & 1) == 0)
        {
            significand >>= 1;
            exponent++;
        }

        if (exponent + decimals + 1 != 0)
        {
            return false;
        }

        if (decimals >= 0)
        {
            return true;
        }

        var power = -decimals;

        if (power > NumberFivePowerLimit)
        {
            // 5^23 is larger than any odd significand a binary64 has, so nothing divides.
            return false;
        }

        long divisor = 1;

        for (var step = 0; step < power; step++)
        {
            divisor *= 5;
        }

        return significand % divisor == 0;
    }

    /// <summary>Splits a <c>"E"</c>-formatted rendering into its mantissa and its exponent.</summary>
    private static void NumberSplitRendered(string rendered, out string mantissa, out int exponent)
    {
        var atE = rendered.IndexOf('E', System.StringComparison.Ordinal);

        if (atE < 0)
        {
            mantissa = rendered;
            exponent = 0;
            return;
        }

        mantissa = rendered[..atE];
        exponent = int.Parse(rendered[(atE + 1)..], NumberCulture);
    }

    /// <summary>Drops the point from a mantissa the <c>"E"</c> format wrote.</summary>
    private static string NumberWithoutPoint(string mantissa) =>
        mantissa.Length > 1 ? mantissa[..1] + mantissa[2..] : mantissa;

    /// <summary>Puts the point back after the first digit, which is where <c>d.ddd</c> wants it.</summary>
    private static string NumberPointAfterFirst(string digits) =>
        digits.Length > 1 ? digits[..1] + "." + digits[1..] : digits;

    /// <summary>The exponent part, with the sign the language writes and no zero padding.</summary>
    private static string NumberExponentSuffix(int exponent) =>
        (exponent < 0 ? "-" : "+") + System.Math.Abs(exponent).ToString(NumberCulture);

    /// <summary>Adds one to a digit string, reporting the carry that lengthened it.</summary>
    private static string NumberIncrementDigits(string digits, out bool carried)
    {
        var characters = digits.ToCharArray();

        for (var at = characters.Length - 1; at >= 0; at--)
        {
            if (characters[at] != '9')
            {
                characters[at]++;
                carried = false;
                return new string(characters);
            }

            characters[at] = '0';
        }

        // Every digit was a nine, so the count is kept and the exponent moves instead.
        carried = true;
        return "1" + new string('0', digits.Length - 1);
    }

    /// <summary>Adds one to the last place of a fixed-point string, carrying across the point.</summary>
    private static string NumberIncrementFixed(string text)
    {
        var characters = text.ToCharArray();

        for (var at = characters.Length - 1; at >= 0; at--)
        {
            if (characters[at] == '.')
            {
                continue;
            }

            if (characters[at] != '9')
            {
                characters[at]++;
                return new string(characters);
            }

            characters[at] = '0';
        }

        return "1" + new string(characters);
    }
}
