using Broiler.JavaScript.BuiltIns.BigInt;
using Broiler.JavaScript.BuiltIns.Function;
using Broiler.JavaScript.BuiltIns.Intl;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.Runtime;
using System;
using System.Globalization;
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
            value = value is JSObject @object ? @object.ToDefaultPrimitive() : value;
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

        // BROILER-PATCH: ECMAScript specifies that negative zero formats as
        // positive zero in toExponential (e.g., (-0).toExponential(4) === "0.0000e+0")

        if (IsNegativeZero(nv))
            nv = 0.0;

        if (hasFractionDigits)
        {
            // Step 4: the range check (0..100) only applies when fractionDigits is
            // supplied, and is performed on the truncated integer value.
            if (f < 0 || f > 100)
                throw JSEngine.NewRangeError("toExponential() digits argument is out of range");

            var m = (int)f;
            var fx = m == 0 ? "0e+0" : "0." + new string('0', m) + "e+0";
            return new JSString(nv.ToString(fx, CultureInfo.InvariantCulture));
        }

        // fractionDigits undefined: use as many significant digits as necessary.
        var text = nv.ToString("0.################e+0", CultureInfo.InvariantCulture);
        return new JSString(text);
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

        // Per ECMAScript spec, -0 should produce "0" (not "-0")
        if (nv == 0.0 && double.IsNegative(nv))
            nv = 0.0;

        if (double.IsPositiveInfinity(nv))
            return JSConstants.Infinity;

        if (double.IsNegativeInfinity(nv))
            return JSConstants.NegativeInfinity;

        if (hasDigits)
        {
            if (nv > 999999999999999.0 && digits <= 15)
                return new JSString(nv.ToString("g21", CultureInfo.InvariantCulture));

            return new JSString(nv.ToString($"F{digits}", CultureInfo.InvariantCulture));
        }

        if (nv > 999999999999999.0)
            return new JSString(nv.ToString("g21", CultureInfo.InvariantCulture));

        return new JSString(nv.ToString("F0", CultureInfo.InvariantCulture));
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

            var originalPrecision = i;
            var d = n.value;
            var prefix = 'g';
            var iteration = 0;

            if (d < 1)
            {
                prefix = 'f';

                // switch to f when number is less than 1
                // because precision is measured from the first non zero
                // digit position
                // Assert.AreEqual("0.0000012", Evaluate("0.00000123.toPrecision(2)"));
                while (d < 1)
                {
                    d = d * 10;
                    i++;
                    iteration++;

                    if (iteration > 6)
                    {
                        // do this only 6 times
                        // or switch back to g
                        // Assert.AreEqual("1.2e-7", Evaluate("0.000000123.toPrecision(2)"));
                        prefix = 'g';
                        i = originalPrecision + 1;
                        break;
                    }
                }

                i--;
            }

            string txt;
            txt = n.value.ToString($"{prefix}{i}");

            // add trailing zeros after .

            var eIndex = txt.IndexOf('e');
            if (eIndex != -1)
            {
                if (txt[eIndex + 2] == '0')
                    txt = txt.Substring(0, eIndex + 2) + txt.Substring(eIndex + 3);

                var totalDigits = eIndex;
                var hasDot = txt.IndexOf('.');

                if (hasDot != -1)
                    totalDigits--;

                var diff = originalPrecision - totalDigits;
                if (diff > 0)
                {
                    if (hasDot == -1)
                    {
                        txt = txt.Insert(eIndex, ".");
                        eIndex++;
                    }

                    txt = txt.Insert(eIndex, new string('0', diff));
                }
            }
            else
            {
                var totalDigits = txt.Length;
                var dotIndex = txt.IndexOf('.');
                if (dotIndex != -1)
                    totalDigits--;

                if (totalDigits < originalPrecision)
                {
                    if (dotIndex == -1)
                        txt += ".";

                    var diff = originalPrecision - totalDigits;
                    txt += new string('0', diff);
                }
            }

            return new JSString(txt);
        }

        return new JSString(n.value.ToString());
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
