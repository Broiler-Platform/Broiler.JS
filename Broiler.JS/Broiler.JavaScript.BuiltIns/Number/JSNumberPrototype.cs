using Broiler.JavaScript.BuiltIns.BigInt;
using Broiler.JavaScript.BuiltIns.Function;
using Broiler.JavaScript.BuiltIns.Intl;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.Runtime;
using System;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Broiler.JavaScript.BuiltIns.Number;

internal static class JSNumberExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static JSNumber ToNumber(this JSValue target, [CallerMemberName] string name = null)
    {
        if (target is not JSNumber n)
        {
            if (target is JSPrimitiveObject primitiveObject)
                return primitiveObject.value.ToNumber();

            if (target is JSObject @object
                && (JSEngine.Current as JSObject)?[Names.Number] is JSFunction numberConstructor
                && ReferenceEquals(@object, numberConstructor.prototype))
                return JSNumber.Zero;

            throw JSEngine.NewTypeError($"Number.prototype.{name} requires that 'this' be a Number");
        }

        return n;
    }
}

partial class JSNumber
{
    public override bool Less(JSValue value)
    {
        value = value.UnwrapPrimitive();

        if (value is JSBigInt bigint)
        {
            if (double.IsNaN(this.value))
                return false;

            return bigint.value.CompareToNumber(this.value) > 0;
        }

        return base.Less(value);
    }

    public override bool LessOrEqual(JSValue value)
    {
        value = value.UnwrapPrimitive();

        if (value is JSBigInt bigint)
        {
            if (double.IsNaN(this.value))
                return false;

            return bigint.value.CompareToNumber(this.value) >= 0;
        }

        return base.LessOrEqual(value);
    }

    public override bool Greater(JSValue value)
    {
        value = value.UnwrapPrimitive();

        if (value is JSBigInt bigint)
        {
            if (double.IsNaN(this.value))
                return false;

            return bigint.value.CompareToNumber(this.value) < 0;
        }

        return base.Greater(value);
    }

    public override bool GreaterOrEqual(JSValue value)
    {
        value = value.UnwrapPrimitive();

        if (value is JSBigInt bigint)
        {
            if (double.IsNaN(this.value))
                return false;

            return bigint.value.CompareToNumber(this.value) <= 0;
        }

        return base.GreaterOrEqual(value);
    }

    [JSExport(Length = 1, IsConstructor = true)]
    public static JSValue Constructor(in Arguments a)
    {
        static JSNumber ToNumberValue(JSValue value)
        {
            value = value is JSPrimitiveObject primitiveObject ? primitiveObject.ValueOf() : value;
            // Number(v) is ToNumber(v) = ToPrimitive(v, NUMBER) then ToNumber, so a user
            // @@toPrimitive receives the "number" hint rather than "default".
            value = value is JSObject @object ? @object.ToNumberPrimitive() : value;
            return value is JSBigInt bigint
                ? new JSNumber(JSBigInt.ToNumber(bigint.value))
                : new JSNumber(value.DoubleValue);
        }

        if ((JSEngine.Current as IJSExecutionContext)?.CurrentNewTarget == null)
        {
            if (a.Length == 0)
                return Zero;

            return ToNumberValue(a[0]);
        }

        if (a.Length == 0)
            return new JSPrimitiveObject(Zero);

        return new JSPrimitiveObject(ToNumberValue(a.Get1()));
    }

    [JSPrototypeMethod]
    [JSExport("clz")]
    public static JSValue Clz(in Arguments a)
    {
        var x = a.This.ToNumber().IntValue;

        // Propagate leftmost 1-bit to the right 
        x = x | (x >> 1);
        x = x | (x >> 2);
        x = x | (x >> 4);
        x = x | (x >> 8);
        x = x | (x >> 16);

        int i = sizeof(int) * 8 - CountOneBits((uint)x);
        return new JSNumber(i);
    }

    /// <summary>
    /// Counts the number of set bits in an integer.
    /// </summary>
    /// <param name="x"> The integer. </param>
    /// <returns> The number of set bits in the integer. </returns>
    private static int CountOneBits(uint x)
    {
        x -= (x >> 1) & 0x55555555;
        x = ((x >> 2) & 0x33333333) + (x & 0x33333333);
        x = ((x >> 4) + x) & 0x0f0f0f0f;
        x += x >> 8;
        x += x >> 16;
        return (int)(x & 0x0000003f);
    }

    [JSPrototypeMethod]
    [JSExport("valueOf")]
    public static JSValue ValueOf(in Arguments a) => a.This.ToNumber();

    [JSPrototypeMethod]
    [JSExport("toString", Length = 1)]

    public static JSString ToString(in Arguments a)
    {
        var n = a.This.ToNumber();
        string result;
        var value = n.value;
        var arg = a.Get1();

        if (!arg.IsNullOrUndefined)
        {
            var integerRadix = Math.Truncate(arg.DoubleValue);
            if (double.IsInfinity(integerRadix) || (integerRadix != 0 && (integerRadix < 2 || integerRadix > 36)))
                throw JSEngine.NewRangeError("The radix must be between 2 and 36, inclusive.");

            // radix 10 (and the absent-radix case) use Number::toString, the shortest
            // decimal string that round-trips — naive digit extraction would render
            // (0.3).toString(10) as "0.299999999999999988…".
            if (integerRadix == 0 || integerRadix == 10)
                return new JSString(ToECMAString(value));

            var radix = (int)integerRadix;
            result = DecimalToBase(value, radix);
            return new JSString(result);
        }

        return new JSString(ToECMAString(value));
    }

    [JSPrototypeMethod]
    [JSExport("toExponential", Length = 1)]
    public static JSValue ToExponential(in Arguments a)
    {
        var n = a.This.ToNumber();
        var nv = n.value;
        var fractionDigits = a.Get1();
        var hasFractionDigits = !fractionDigits.IsUndefined;

        // Step 2 of Number.prototype.toExponential: ToIntegerOrInfinity(fractionDigits)
        // is evaluated (with any observable coercion side effects) before the
        // non-finite short-circuit. ToIntegerOrInfinity truncates toward zero and
        // maps NaN to 0.
        var fRaw = hasFractionDigits ? fractionDigits.DoubleValue : 0.0;
        var f = double.IsNaN(fRaw) ? 0.0 : Math.Truncate(fRaw);

        // Step 3: a non-finite x returns "NaN"/"Infinity"/"-Infinity" *before* the
        // fractionDigits range check (e.g. NaN.toExponential(-1) === "NaN").
        if (double.IsNaN(nv))
            return new JSString("NaN");

        if (double.IsPositiveInfinity(nv))
            return JSConstants.Infinity;

        if (double.IsNegativeInfinity(nv))
            return JSConstants.NegativeInfinity;

        if (hasFractionDigits)
        {
            // Step 4: the range check (0..100) only applies when fractionDigits is
            // supplied, and is performed on the truncated integer value.
            if (f < 0 || f > 100)
                throw JSEngine.NewRangeError("toExponential() digits argument is out of range");
        }

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

        return new JSString(sign + FormatExponential(digits, exponent));
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

    [JSPrototypeMethod]
    [JSExport("toFixed", Length = 1)]
    public static JSValue ToFixed(in Arguments a)
    {
        var n = a.This.ToNumber();
        var nv = n.value;
        var digitsValue = a.Get1();
        var hasDigits = !digitsValue.IsUndefined;
        var digits = 0;

        if (hasDigits)
        {
            var digitsNumber = digitsValue.DoubleValue;
            // Per spec, ToIntegerOrInfinity(fractionDigits) maps NaN to 0; only the
            // subsequent [0, 100] range check rejects out-of-range values (which
            // includes ±Infinity, since Math.Truncate leaves them unchanged).
            if (double.IsNaN(digitsNumber))
                digitsNumber = 0;

            var integerDigits = Math.Truncate(digitsNumber);
            if (integerDigits < 0 || integerDigits > 100)
                throw JSEngine.NewRangeError("toFixed() digits argument must be between 0 and 100");

            digits = (int)integerDigits;
        }

        if (double.IsNaN(nv))
            return new JSString("NaN");

        if (double.IsPositiveInfinity(nv))
            return JSConstants.Infinity;

        if (double.IsNegativeInfinity(nv))
            return JSConstants.NegativeInfinity;

        // Step 9: if |x| ≥ 10^21 the fixed-notation algorithm is abandoned and the
        // result is Number::toString(x).
        if (Math.Abs(nv) >= 1e21)
            return new JSString(ToECMAString(nv));

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
            return new JSString(sign + m);

        // Insert the decimal point f digits from the right, padding the integer part
        // with a leading "0" when the magnitude is below 1.
        if (m.Length <= digits)
            m = new string('0', digits - m.Length + 1) + m;

        var pointIndex = m.Length - digits;
        return new JSString(sign + m.Substring(0, pointIndex) + "." + m.Substring(pointIndex));
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

    [JSPrototypeMethod]
    [JSExport("toPrecision", Length = 1)]
    public static JSValue ToPrecision(in Arguments a)
    {
        var n = a.This.ToNumber();

        if (double.IsPositiveInfinity(n.value))
            return JSConstants.Infinity;

        if (double.IsNegativeInfinity(n.value))
            return JSConstants.NegativeInfinity;

        var precisionArg = a.Get1();
        if (!precisionArg.IsUndefined)
        {
            // Step 3: p = ToIntegerOrInfinity(precision). ToNumber rejects a
            // Symbol or BigInt with a TypeError; objects coerce via valueOf.
            var precisionPrimitive = precisionArg is JSObject precisionObject
                ? precisionObject.ToDefaultPrimitive()
                : precisionArg;
            if (precisionPrimitive.IsSymbol || precisionPrimitive is JSBigInt)
                throw JSEngine.NewTypeError($"Cannot convert {precisionPrimitive.TypeOf()} to a number");

            var n1 = precisionPrimitive as JSNumber ?? new JSNumber(precisionPrimitive.DoubleValue);

            // Step 4: if x is NaN, return "NaN" — this happens after coercing
            // precision but BEFORE the range check, so an out-of-range precision
            // (e.g. a valueOf returning Infinity) still yields "NaN" rather than
            // a RangeError.
            if (double.IsNaN(n.value))
                return new JSString("NaN");

            // Step 8: the precision must be in the inclusive range 1..100.
            if (double.IsNaN(n1.value) || n1.value > 100 || n1.value < 1)
                throw JSEngine.NewRangeError("toPrecision() digits argument must be between 1 and 100");

            var i = (int)n1.value;

            // Step 6: when x is zero (including -0) the result is p zero digits with no
            // sign — "0", "0.0", "0.00", … — never a signed/exponential ".NET" rendering
            // such as "-0".
            if (n.value == 0)
                return new JSString(i == 1 ? "0" : "0." + new string('0', i - 1));

            // Steps 10-11: round the exact value of x to p significant digits (ties
            // away from zero), then place the decimal point / choose exponential form
            // from the base-10 exponent e of the leading digit. Working from the
            // magnitude keeps the sign out of the digit count.
            var sign = n.value < 0 ? "-" : "";
            var x = Math.Abs(n.value);
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

            return new JSString(sign + formatted);
        }

        return new JSString(ToECMAString(n.value));
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
        var repr = x.ToString("R", CultureInfo.InvariantCulture);

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

    [JSPrototypeMethod]
    [JSExport("toLocaleString", Length = 0)]
    public static JSString ToLocaleString(in Arguments a)
    {
        var n = a.This.ToNumber();
        var (locale, format) = a.Get2();

        // Broiler extension: a string `format` is a .NET numeric-format string (not part
        // of the spec), kept for compatibility. Everything else follows §21.1.3.4:
        // Number.prototype.toLocaleString(locales, options) ≡
        // Intl.NumberFormat(locales, options).format(this), so grouping, percent/currency
        // styles, alternate numbering systems, etc. all match Intl.NumberFormat.
        if (format.IsString)
        {
            var culture = locale.IsNullOrUndefined
                ? CultureInfo.CurrentCulture
                : CultureInfo.GetCultureInfo(locale.ToString());
            return new JSString(n.value.ToString(format.ToString(), culture));
        }

        var nf = new JSIntlNumberFormat(new Arguments(JSUndefined.Value, locale, format));
        return new JSString(nf.Format(new Arguments(JSUndefined.Value, n)).StringValue);
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
