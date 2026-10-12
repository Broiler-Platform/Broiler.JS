// REFERENCE ARM J: a test-only copy of Broiler.JS's implementation before the cut-over, for the
// reference-arm comparison of bundle sjl-3-001 (docs/evidence/sjl-3-001/reference-arms.md). It is
// never referenced outside this test project, and it is retired at SJL-16.
//
// Copied from Broiler.JS at 44a214dc (bundle sjl-0-001's base):
//   Broiler.JS/Broiler.JavaScript.BuiltIns/Number/JSNumberPrototype.cs
//     blob 4d5df02dfd5e8ee79a72626a5e509c95ecb79308: FormatExponential, RoundScaled, ToExactFraction, ExactSignificantDigits, Compare10Pow, ShortestSignificantDigits, DecimalToBase and DecimalToArbitrarySystem verbatim; ToString, ToExponential, ToFixed and ToPrecision adapted
//
// Changes from the source:
//   - line endings normalised to LF where the source has CRLF, as rule SL10 requires of the shared folder;
//   - the namespace; the members sit in a static class of their own instead of the built-in prototype class, whose other members are the engine's; DecimalToBase and DecimalToArbitrarySystem are internal instead of public;
//   - the four built-ins are adapted as the comment above them says: each takes the validated value and digit count instead of the Arguments (an engine type), returns the string instead of a JSString, and leaves out the coercion and RangeError lines, which the comparison does not cover; every remaining line is the source's, verbatim, which the generator of this file checked line by line;
//   - ToECMAString is qualified as JSNumber.ToECMAString, the copy beside this file; no result changes.

using System;
using System.Globalization;
using System.Numerics;

namespace Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.J;

/// <summary>Number.prototype's digit generation, as the built-ins computed it.</summary>
internal static class JSNumberPrototype
{
    // ---- The four built-ins' paths after their arguments are validated, ADAPTED: each is the
    // source method's body from the point where it has a validated value and digit count to its
    // return, with `new JSString(s)` returning s, JSConstants.Infinity and NegativeInfinity
    // returning "Infinity" and "-Infinity", and `n.value` read from the parameter. Coercion,
    // argument validation and RangeErrors are the adapter's and are not compared (the rule's
    // "operations" line), so the lines doing them are not here. Every other line is verbatim.

    /// <summary>ToString(in Arguments a) for a radix already validated as 2 to 36.</summary>
    internal static string ToStringRadix(double value, int radix)
    {
        var integerRadix = (double)radix;

        // radix 10 (and the absent-radix case) use Number::toString, the shortest
        // decimal string that round-trips — naive digit extraction would render
        // (0.3).toString(10) as "0.299999999999999988…".
        if (integerRadix == 0 || integerRadix == 10)
            return JSNumber.ToECMAString(value);

        string result;
        result = DecimalToBase(value, radix);
        return result;
    }

    /// <summary>ToExponential(in Arguments a): <paramref name="fraction"/> is null when fractionDigits is undefined, and otherwise validated as 0 to 100.</summary>
    internal static string ToExponential(double nv, int? fraction)
    {
        var hasFractionDigits = fraction is not null;
        var f = (double)(fraction ?? 0);

        if (double.IsNaN(nv))
            return "NaN";

        if (double.IsPositiveInfinity(nv))
            return "Infinity";

        if (double.IsNegativeInfinity(nv))
            return "-Infinity";

        // ECMAScript formats negative zero as positive zero in toExponential
        // (e.g., (-0).toExponential(4) === "0.0000e+0").
        var sign = nv < 0 ? "-" : "";
        var x = Math.Abs(nv);
        var fractionCount = (int)f;

        string digits;
        int exponent;

        if (x == 0.0)
        {
            // Leading "0" digit plus the requested fraction zeros, exponent 0.
            digits = new string('0', hasFractionDigits ? fractionCount + 1 : 1);
            exponent = 0;
        }
        else if (hasFractionDigits)
        {
            // fractionDigits supplied: round the exact value of x to f+1 significant
            // decimal digits (ties away from zero), matching the spec's choice of the
            // integer n with n × 10^(e-f) closest to x.
            (digits, exponent) = ExactSignificantDigits(x, fractionCount + 1);
        }
        else
        {
            // fractionDigits undefined: use as many significant digits as necessary
            // (the shortest string that round-trips through IEEE-754).
            (digits, exponent) = ShortestSignificantDigits(x);
        }

        return sign + FormatExponential(digits, exponent);
    }

    /// <summary>ToFixed(in Arguments a) for digits already validated as 0 to 100.</summary>
    internal static string ToFixed(double nv, int digits)
    {
        if (double.IsNaN(nv))
            return "NaN";

        if (double.IsPositiveInfinity(nv))
            return "Infinity";

        if (double.IsNegativeInfinity(nv))
            return "-Infinity";

        // Step 9: if |x| ≥ 10^21 the fixed-notation algorithm is abandoned and the
        // result is Number::toString(x).
        if (Math.Abs(nv) >= 1e21)
            return JSNumber.ToECMAString(nv);

        // Negative zero (and any negative value) keeps its sign per the spec, which
        // sets the sign from x < 0 before taking the magnitude; -0 < 0 is false, so
        // -0 renders without a sign.
        var sign = nv < 0 ? "-" : "";
        var x = Math.Abs(nv);

        // Step 6: choose the integer n with n / 10^f − x closest to zero, ties away
        // from zero, computed from the exact binary value of x.
        var scaled = RoundScaled(x, digits);
        var m = scaled.ToString(CultureInfo.InvariantCulture);

        if (digits == 0)
            return sign + m;

        // Insert the decimal point f digits from the right, padding the integer part
        // with a leading "0" when the magnitude is below 1.
        if (m.Length <= digits)
            m = new string('0', digits - m.Length + 1) + m;

        var pointIndex = m.Length - digits;
        return sign + m.Substring(0, pointIndex) + "." + m.Substring(pointIndex);
    }

    /// <summary>ToPrecision(in Arguments a) for a precision already validated as 1 to 100.</summary>
    internal static string ToPrecision(double value, int precision)
    {
        if (double.IsPositiveInfinity(value))
            return "Infinity";

        if (double.IsNegativeInfinity(value))
            return "-Infinity";

        // Step 4: if x is NaN, return "NaN" — this happens after coercing
        // precision but BEFORE the range check, so an out-of-range precision
        // (e.g. a valueOf returning Infinity) still yields "NaN" rather than
        // a RangeError.
        if (double.IsNaN(value))
            return "NaN";

        var i = precision;

        // Step 6: when x is zero (including -0) the result is p zero digits with no
        // sign — "0", "0.0", "0.00", … — never a signed/exponential ".NET" rendering
        // such as "-0".
        if (value == 0)
            return i == 1 ? "0" : "0." + new string('0', i - 1);

        // Steps 10-11: round the exact value of x to p significant digits (ties
        // away from zero), then place the decimal point / choose exponential form
        // from the base-10 exponent e of the leading digit. Working from the
        // magnitude keeps the sign out of the digit count.
        var sign = value < 0 ? "-" : "";
        var x = Math.Abs(value);
        var (digits, e) = ExactSignificantDigits(x, i);

        string formatted;
        if (e < -6 || e >= i)
        {
            // Exponential notation when the exponent falls outside [-6, p).
            formatted = FormatExponential(digits, e);
        }
        else if (e == i - 1)
        {
            // The p digits exactly fill the integer part.
            formatted = digits;
        }
        else if (e >= 0)
        {
            // Decimal point sits e+1 digits in from the left.
            formatted = digits.Substring(0, e + 1) + "." + digits.Substring(e + 1);
        }
        else
        {
            // 0 < x < 1: leading "0." then -(e+1) zeros before the digits.
            formatted = "0." + new string('0', -(e + 1)) + digits;
        }

        return sign + formatted;
    }

    /// <summary>
    /// Formats a significand digit string and base-10 exponent of its leading digit
    /// as ECMAScript exponential notation, e.g. ("12300", 4) → "1.2300e+4".
    /// </summary>
    private static string FormatExponential(string digits, int exponent)
    {
        var mantissa = digits.Length > 1
            ? digits[0] + "." + digits.Substring(1)
            : digits;
        var sign = exponent >= 0 ? "+" : "-";
        return mantissa + "e" + sign + Math.Abs(exponent).ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Returns round(x × 10^f) for a non-negative finite double, using exact rational
    /// arithmetic with ties resolved away from zero (matching the ECMAScript spec).
    /// </summary>
    private static BigInteger RoundScaled(double x, int f)
    {
        var (num, den) = ToExactFraction(x);
        if (f >= 0)
            num *= BigInteger.Pow(10, f);
        else
            den *= BigInteger.Pow(10, -f);

        var quotient = BigInteger.DivRem(num, den, out var remainder);
        if (remainder * 2 >= den)
            quotient += 1;

        return quotient;
    }

    /// <summary>
    /// Decomposes a positive finite double into an exact rational num / den
    /// (den &gt; 0), so that callers can round its value with arbitrary precision.
    /// </summary>
    private static (BigInteger num, BigInteger den) ToExactFraction(double x)
    {
        var bits = BitConverter.DoubleToInt64Bits(x);
        var biasedExponent = (int)((bits >> 52) & 0x7FF);
        var mantissaBits = bits & 0xFFFFFFFFFFFFFL;

        BigInteger mantissa;
        int exponent;
        if (biasedExponent == 0)
        {
            // Subnormal: no implicit leading 1 bit.
            mantissa = mantissaBits;
            exponent = -1074;
        }
        else
        {
            mantissa = mantissaBits | 0x10000000000000L;
            exponent = biasedExponent - 1075;
        }

        return exponent >= 0
            ? (mantissa * BigInteger.Pow(2, exponent), BigInteger.One)
            : (mantissa, BigInteger.Pow(2, -exponent));
    }

    /// <summary>
    /// Returns the <paramref name="precision"/> most-significant decimal digits of a
    /// positive finite double (correctly rounded, ties away from zero) together with
    /// the base-10 exponent <c>e</c> of the leading digit (value ≈ d.dddd × 10^e).
    /// </summary>
    private static (string digits, int e) ExactSignificantDigits(double x, int precision)
    {
        var (num, den) = ToExactFraction(x);

        // Seed e = floor(log10(x)) from a float estimate, then correct it exactly so
        // that 10^e ≤ x < 10^(e+1).
        var e = (int)Math.Floor(Math.Log10(x));
        while (Compare10Pow(num, den, e) > 0)
            e--;
        while (Compare10Pow(num, den, e + 1) <= 0)
            e++;

        // n = round(x × 10^(precision-1-e)); n then has exactly `precision` digits.
        var k = precision - 1 - e;
        var scaledNum = num;
        var scaledDen = den;
        if (k >= 0)
            scaledNum *= BigInteger.Pow(10, k);
        else
            scaledDen *= BigInteger.Pow(10, -k);

        var n = BigInteger.DivRem(scaledNum, scaledDen, out var remainder);
        if (remainder * 2 >= scaledDen)
            n += 1;

        // A rounding carry can push n to 10^precision (one digit too many); drop the
        // trailing zero and bump the exponent.
        if (n >= BigInteger.Pow(10, precision))
        {
            n /= 10;
            e += 1;
        }

        return (n.ToString(CultureInfo.InvariantCulture), e);
    }

    /// <summary>
    /// Compares 10^m against x = num / den, returning the sign of (10^m − x).
    /// </summary>
    private static int Compare10Pow(BigInteger num, BigInteger den, int m)
        => m >= 0
            ? (BigInteger.Pow(10, m) * den).CompareTo(num)
            : den.CompareTo(num * BigInteger.Pow(10, -m));

    /// <summary>
    /// Returns the shortest significand digit string that round-trips through
    /// IEEE-754 for a positive finite double, along with the base-10 exponent of its
    /// leading digit (the digits used by Number::toString).
    /// </summary>
    private static (string digits, int e) ShortestSignificantDigits(double x)
    {
        var repr = JSNumber.RoundTrippable(x);

        var eIdx = repr.IndexOf('E');
        string mantissa;
        var exp = 0;
        if (eIdx >= 0)
        {
            mantissa = repr.Substring(0, eIdx);
            exp = int.Parse(repr.AsSpan(eIdx + 1), CultureInfo.InvariantCulture);
        }
        else
        {
            mantissa = repr;
        }

        var dotIdx = mantissa.IndexOf('.');
        if (dotIdx >= 0)
        {
            var fracLen = mantissa.Length - dotIdx - 1;
            mantissa = string.Concat(mantissa.AsSpan(0, dotIdx), mantissa.AsSpan(dotIdx + 1));
            exp -= fracLen;
        }

        mantissa = mantissa.TrimStart('0');
        var origLen = mantissa.Length;
        mantissa = mantissa.TrimEnd('0');
        exp += origLen - mantissa.Length;

        // value = int(mantissa) × 10^exp; the leading digit's place value is 10^(exp+k-1).
        var k = mantissa.Length;
        return (mantissa, exp + k - 1);
    }

    public static string DecimalToBase(double number, int radix)
    {
        if (number == 0.0)
            return "0";

        if (double.IsPositiveInfinity(number))
            return "Infinity";

        if (double.IsNegativeInfinity(number))
            return "-Infinity";

        if (double.IsNaN(number))
            return "NaN";

        var isNegative = number < 0.0;
        number = Math.Abs(number);
        
        var sign = isNegative ? "-" : "";

        var integerPart = Math.Floor(number);
        var digitsTxt = DecimalToArbitrarySystem((long)integerPart, radix);
        if (integerPart == number)
            return $"{sign}{digitsTxt}";

        // Fractional part: repeatedly multiply the remaining fraction by the radix and
        // emit the integer digit each step (so 0.5 base 2 is "0.1", 255.5 base 16 is
        // "ff.8"). A terminating fraction drives `fraction` to 0 and stops; the cap
        // bounds non-terminating expansions (e.g. 0.1 in base 2).
        const string Digits = "0123456789abcdefghijklmnopqrstuvwxyz";
        var fraction = number - integerPart;
        var fractionText = new System.Text.StringBuilder();
        for (int i = 0; i < 1100 && fraction > 0.0; i++)
        {
            fraction *= radix;
            var digit = (int)Math.Floor(fraction);
            fractionText.Append(Digits[digit]);
            fraction -= digit;
        }

        return $"{sign}{digitsTxt}.{fractionText}";
    }

    /// <summary>
    /// https://stackoverflow.com/questions/923771/quickest-way-to-convert-a-base-10-number-to-any-base-in-net
    /// Converts the given decimal number to the numeral system with the
    /// specified radix (in the range [2, 36]).
    /// </summary>
    /// <param name="decimalNumber">The number to convert.</param>
    /// <param name="radix">The radix of the destination numeral system (in the range [2, 36]).</param>
    /// <returns></returns>
    public static string DecimalToArbitrarySystem(long decimalNumber, int radix)
    {
        const int BitsInLong = 64;
        const string Digits = "0123456789abcdefghijklmnopqrstuvwxyz";

        if (radix < 2 || radix > Digits.Length)
            throw new ArgumentException("The radix must be >= 2 and <= " + Digits.Length.ToString());

        if (decimalNumber == 0)
            return "0";

        int index = BitsInLong - 1;
        long currentNumber = Math.Abs(decimalNumber);
        char[] charArray = new char[BitsInLong];

        while (currentNumber != 0)
        {
            int remainder = (int)(currentNumber % radix);
            charArray[index--] = Digits[remainder];
            currentNumber = currentNumber / radix;
        }

        string result = new(charArray, index + 1, BitsInLong - index - 1);
        if (decimalNumber < 0)
            result = "-" + result;

        return result;
    }
}
