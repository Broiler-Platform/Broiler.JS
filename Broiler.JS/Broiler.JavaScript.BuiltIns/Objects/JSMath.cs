using Broiler.JavaScript.ExpressionCompiler;
using System;
using System.Collections.Generic;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Objects;

[JSClassGenerator("Math"), JSInternalObject]
public partial class JSMath : JSObject
{
    static Random randomGenertor = new();

    internal static double RandomNumber() => randomGenertor.NextDouble();


    [JSExportSameName]
    public readonly static double E = Math.E;

    [JSExportSameName]
    public readonly static double LN10 = Math.Log(10);

    [JSExportSameName]
    public readonly static double LN2 = Math.Log(2);

    [JSExportSameName]
    public readonly static double LOG10E = Math.Log10(E);

    [JSExportSameName]
    // Math.LOG2E is the base-2 logarithm of E (≈1.4426950408889634), not the natural log of E (=1).
    public readonly static double LOG2E = Math.Log2(E);

    [JSExportSameName]
    public readonly static double PI = Math.PI;

    [JSExportSameName]
    public readonly static double SQRT1_2 = Math.Sqrt(0.5);

    [JSExportSameName]
    public readonly static double SQRT2 = Math.Sqrt(2);

    [JSExport]
    public static JSValue Random(in Arguments a) => new JSNumber(randomGenertor.NextDouble());

    [JSExport]
    public static JSValue Round(in Arguments args)
    {
        var first = args.Get1();
        if (first.IsUndefined)
            return JSNumber.NaN;

        if (first.IsNull)
            return JSNumber.Zero;

        if (first.IsDecimal)
        {
            var dv = first.DecimalValue;
            return JSValue.CreateDecimal(Math.Floor(dv + 0.5m));
        }

        var number = first.DoubleValue;
        if (number > 0.0)
            return new JSNumber(Math.Floor(number + 0.5));

        if (number >= -0.5)
        {
            // BitConverter is used to distinguish positive and negative zero.
            if (BitConverter.DoubleToInt64Bits(number) == 0L)
                return JSNumber.Zero;
            return new JSNumber(-0.0D);
        }

        return new JSNumber(Math.Floor(number + 0.5));
    }

    /// <summary>
    /// We do not want to recreate new objects for standard known constants. 
    /// Hence, we need to check and return already existing constants.
    /// 
    /// </summary>
    /// <param name="t"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    [JSExport]
    public static JSValue Floor(in Arguments args)
    {
        var first = args.Get1();
        if (first.IsDecimal)
            return JSValue.CreateDecimal(Math.Floor(first.DecimalValue));

        var d = first.DoubleValue;
        var r = new JSNumber(Math.Floor(d));
        return r;
    }

    [JSExport]
    public static JSValue Acos(in Arguments args)
    {
        var first = args.Get1();
        var d = first.DoubleValue;
        var r = new JSNumber(Math.Acos(d));

        return r;
    }

    [JSExport]
    public static JSValue Abs(in Arguments args)
    {
        var first = args.Get1();
        if (first.IsDecimal)
            return JSValue.CreateDecimal(Math.Abs(first.DecimalValue));

        var d = first.DoubleValue;
        var r = new JSNumber(Math.Abs(d));
        return r;
    }

    [JSExport]
    public static JSValue Acosh(in Arguments args)
    {
        var first = args.Get1();
        var d = first.DoubleValue;
        // Math.Acosh is correctly rounded; the naive log(d + sqrt(d²−1)) loses precision for d near 1
        // (cancellation in d²−1) and for large d (overflow of d²).
        return new JSNumber(Math.Acosh(d));
    }

    [JSExport]
    public static JSValue Asin(in Arguments args)
    {
        var first = args.Get1();
        var d = first.DoubleValue;
        var r = new JSNumber(Math.Asin(d));

        return r;
    }

    [JSExport]
    public static JSValue Asinh(in Arguments args)
    {
        var first = args.Get1();
        var d = first.DoubleValue;
        // Math.Asinh preserves the sign of zero (asinh(-0) === -0) and handles ±∞.
        var r = new JSNumber(Math.Asinh(d));
        return r;
    }

    [JSExport]
    public static JSValue Atan(in Arguments args)
    {
        var first = args.Get1();
        var d = first.DoubleValue;
        var r = new JSNumber(Math.Atan(d));
        return r;
    }

    [JSExport]
    public static JSValue Atan2(in Arguments args)
    {
        var (first, second) = args.Get2();
        var d1 = first.DoubleValue;
        var d2 = second.DoubleValue;

        if (double.IsInfinity(d1) || double.IsInfinity(d2))
        {
            if (double.IsPositiveInfinity(d1) && double.IsPositiveInfinity(d2))
                return new JSNumber(Math.PI / 4.0);

            if (double.IsPositiveInfinity(d1) && double.IsNegativeInfinity(d2))
                return new JSNumber(3.0 * Math.PI / 4.0);

            if (double.IsNegativeInfinity(d1) && double.IsPositiveInfinity(d2))
                return new JSNumber(-Math.PI / 4.0);

            if (double.IsNegativeInfinity(d1) && double.IsNegativeInfinity(d2))
                return new JSNumber(-3.0 * Math.PI / 4.0);
        }

        var r = new JSNumber(Math.Atan2(d1, d2));
        return r;
    }

    [JSExport]
    public static JSValue Atanh(in Arguments args)
    {
        var first = args.Get1();
        var d = first.DoubleValue;
        // Math.Atanh preserves the sign of zero (atanh(-0) === -0), returns ±∞ at ±1
        // and NaN outside [-1, 1].
        var r = new JSNumber(Math.Atanh(d));

        return r;
    }

    [JSExport]
    public static JSValue Cbrt(in Arguments args)
    {
        var first = args.Get1();
        var d = first.DoubleValue;
        return new JSNumber(CorrectlyRoundedCbrt(d));
    }

    // System.Math.Cbrt can be off by one ULP (e.g. Math.Cbrt(27) is
    // 3.0000000000000004), which breaks perfect-cube landmarks. Choose whichever of
    // the result and its two neighbours cubes closest to the input. The ±0, ±∞ and
    // NaN cases are returned as-is (Math.Cbrt already preserves the sign of zero and
    // propagates non-finite values).
    private static double CorrectlyRoundedCbrt(double d)
    {
        var y = Math.Cbrt(d);
        if (d == 0.0 || double.IsNaN(d) || double.IsInfinity(d))
            return y;

        var best = y;
        var bestErr = Math.Abs(y * y * y - d);

        var lower = Math.BitDecrement(y);
        var lowerErr = Math.Abs(lower * lower * lower - d);
        if (lowerErr < bestErr) { best = lower; bestErr = lowerErr; }

        var upper = Math.BitIncrement(y);
        var upperErr = Math.Abs(upper * upper * upper - d);
        if (upperErr < bestErr) { best = upper; }

        return best;
    }

    [JSExport]
    public static JSValue Ceil(in Arguments args)
    {
        var first = args.Get1();
        if (first.IsDecimal)
            return JSValue.CreateDecimal(Math.Ceiling(first.DecimalValue));

        var d = first.DoubleValue;
        var r = new JSNumber(Math.Ceiling(d));
        return r;
    }

    private static readonly int[] clz32Table = [
        32, 31,  0, 16,  0, 30,  3,  0, 15,  0,  0,  0, 29, 10,  2,  0,
         0,  0, 12, 14, 21,  0, 19,  0,  0, 28,  0, 25,  0,  9,  1,  0,
        17,  0,  4,  0,  0,  0, 11,  0, 13, 22, 20,  0, 26,  0,  0, 18,
         5,  0,  0, 23,  0, 27,  0,  6,  0, 24,  7,  0,  8,  0,  0,  0
    ];


    /// <summary>
    /// we have Int value, so we might want to replace DoubleValue with Intvalue, 
    /// But since the implementation is not complete, we have continued with Doublevalue
    /// https://github.com/paulbartrum/jurassic/blob/0522bcb42b29f87bdf65ae74b9a450179c1d168d/Jurassic/Library/MathObject.cs#L475
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    [JSExport]
    public static JSValue Clz32(in Arguments args)
    {
        var first = args.Get1();
        var d = first.DoubleValue;
        var x = JSValue.ToUint32(d);

        x |= x >> 1;       // Propagate leftmost
        x |= x >> 2;       // 1-bit to the right.
        x |= x >> 4;
        x |= x >> 8;
        x |= x >> 16;
        x *= 0x06EB14F9;     // Multiplier is 7*255**3.

        var r = clz32Table[x >> 26];
        return new JSNumber(r);
    }

    [JSExport]
    public static JSValue Cos(in Arguments args)
    {
        var first = args.Get1();
        var d = first.DoubleValue;
        var r = new JSNumber(Math.Cos(d));

        return r;
    }

    [JSExport]
    public static JSValue Cosh(in Arguments args)
    {
        var first = args.Get1();
        var d = first.DoubleValue;
        var r = new JSNumber(Math.Cosh(d));

        return r;
    }

    [JSExport]
    public static JSValue Exp(in Arguments args)
    {
        var first = args.Get1();
        var d = first.DoubleValue;
        var r = new JSNumber(Math.Exp(d));
        return r;
    }

    [JSExport]
    public static JSValue Expm1(in Arguments args)
    {
        var first = args.Get1();
        var d = first.DoubleValue;
        // expm1(±0) === ±0: Math.Exp(-0)-1 would lose the sign of zero, so pass
        // ±0 (and NaN) through unchanged.
        if (d == 0.0 || double.IsNaN(d))
            return new JSNumber(d);
        var u = Math.Exp(d);
        // |d| tiny: e^d rounds to 1, so e^d − 1 underflows to 0 and loses all precision; expm1(d) ≈ d.
        if (u == 1.0)
            return new JSNumber(d);
        var um1 = u - 1.0;
        var logU = Math.Log(u);
        // When e^d overflowed to +∞ (logU = +∞) or underflowed to 0 (logU = −∞), the correction is
        // undefined but u − 1 (= +∞ or −1) is already exact.
        if (double.IsInfinity(logU))
            return new JSNumber(um1);
        // Accurate expm1 = (e^d − 1) · d / ln(e^d): the relative errors of (u − 1) and ln(u) cancel,
        // giving a result good to ~1 ulp where the direct e^d − 1 would not.
        return new JSNumber(um1 * d / logU);
    }

    [JSExport]
    public static JSValue Fround(in Arguments args)
    {
        var first = args.Get1();
        var d = first.DoubleValue;
        var r = (double)(float)d;

        return new JSNumber(r);
    }

    /// <summary>
    /// ES2025 §2.8 — Math.f16round(x)
    /// Rounds a number to the nearest IEEE 754 half-precision (16-bit) float.
    /// </summary>
    [JSExport("f16round")]
    public static JSValue F16round(in Arguments args)
    {
        var first = args.Get1();
        var d = first.DoubleValue;
        var r = (double)(Half)d;

        return new JSNumber(r);
    }

    [JSExport]
    public static JSValue Hypot(in Arguments args)
    {
        int length = args.Length;

        if (length == 0)
            return JSNumber.Zero;

        if (length == 1)
            return new JSNumber(Math.Abs(args.Get1().DoubleValue));

        var (first, second) = args.Get2();
        double d1 = first.DoubleValue;
        double d2 = second.DoubleValue;

        if (length == 2)
            return new JSNumber(Hypot(d1, d2));

        double result = Hypot(d1, d2);
        for (int i = 2; i < length; i++)
        {
            double val = args.GetAt(i).DoubleValue;
            result = Hypot(result, val);
        }

        return new JSNumber(result);
    }

    public static double Hypot(double number1, double number2)
    {
        double abs1 = Math.Abs(number1);
        double abs2 = Math.Abs(number2);

        // Per spec (Math.hypot), an infinite magnitude takes precedence over NaN: if any argument
        // is ±Infinity the result is +Infinity even when another is NaN. Math.Min/Math.Max
        // propagate NaN, so these cases must be handled before the general computation.
        if (double.IsInfinity(abs1) || double.IsInfinity(abs2))
            return double.PositiveInfinity;
        if (double.IsNaN(abs1) || double.IsNaN(abs2))
            return double.NaN;

        double min = Math.Min(abs1, abs2);
        double max = Math.Max(abs1, abs2);

        if (min == 0)
            return max;

        double u = min / max;
        return max * Math.Sqrt(1 + u * u);
    }

    [JSExport]
    public static JSValue Imul(in Arguments args)
    {
        var (first, second) = args.Get2();
        var d1 = first.IntValue;
        var d2 = second.IntValue;
        var r = d1 * d2;

        return new JSNumber(r);
    }

    [JSExport]
    public static JSValue Log(in Arguments args)
    {
        var first = args.Get1();
        var d = first.DoubleValue;
        var r = Math.Log(d);

        return new JSNumber(r);
    }

    [JSExport]
    public static JSValue Log10(in Arguments args)
    {
        var first = args.Get1();
        var d = first.DoubleValue;
        var r = Math.Log10(d);

        return new JSNumber(r);
    }


    [JSExport]
    public static JSValue Log1p(in Arguments args)
    {
        var first = args.Get1();
        var d = first.DoubleValue;
        double r;

        if (Math.Abs(d) < 0.01)
        {
            // For small numbers, use a taylor series approximation.
            r = d * (1.0 + d * (-1.0 / 2.0 + d * (1.0 / 3.0 + d *
                (-1.0 / 4.0 + d * (1.0 / 5.0 + d * (-1.0 / 6.0 + d * (1.0 / 7.0)))))));
            return new JSNumber(r);
        }

        // Otherwise just use the normal log function.
        r = Math.Log(1.0 + d);
        return new JSNumber(r);
    }

    [JSExport]
    public static JSValue Log2(in Arguments args)
    {
        var first = args.Get1();
        var d = first.DoubleValue;
        // Math.Log2 is correctly rounded (and returns exact integers for powers of two); the naive
        // Math.Log(d) / LN2 carries an extra rounding error that can exceed the test tolerance.
        return new JSNumber(Math.Log2(d));
    }

    [JSExport(Length = 2)]
    public static JSValue Max(in Arguments args)
    {
        int length = args.Length;
        double result = double.NegativeInfinity;

        for (int i = 0; i < length; i++)
        {
            double val = args.GetAt(i).DoubleValue;

            // +0 is considered larger than -0 (Math.max(-0, +0) is +0), so when both
            // are zero and the running result is -0, prefer the new value.
            if (val > result || double.IsNaN(val)
                || (val == 0 && result == 0 && double.IsNegative(result)))
                result = val;
        }

        return new JSNumber(result);
    }

    [JSExport(Length = 2)]
    public static JSValue Min(in Arguments args)
    {
        int length = args.Length;
        double result = double.PositiveInfinity;

        for (int i = 0; i < length; i++)
        {
            double val = args.GetAt(i).DoubleValue;

            // -0 is considered smaller than +0 (Math.min(+0, -0) is -0).
            if (val < result || double.IsNaN(val)
                || (val == 0 && result == 0 && double.IsNegative(val)))
                result = val;
        }

        return new JSNumber(result);
    }

    [JSExport]
    public static JSValue Pow(in Arguments args)
    {
        var (first, second) = args.Get2();
        var @base = first.DoubleValue;
        var exponent = second.DoubleValue;

        // Number::exponentiate step 1: a NaN exponent always yields NaN, even for
        // base 1 where IEEE pow(1, NaN) would return 1.
        if (double.IsNaN(exponent))
            return JSNumber.NaN;

        if ((@base == 1.0 || @base == -1) && double.IsInfinity(exponent))
            return JSNumber.NaN;

        if (double.IsNaN(@base) && exponent == 0.0)
            return JSNumber.One;

        var r = Math.Pow(@base, exponent);
        return new JSNumber(r);
    }

    [JSExport]
    public static JSValue Sign(in Arguments args)
    {
        var first = args.Get1();

        if (first.IsDecimal)
            return JSValue.CreateDecimal(Math.Sign(first.DecimalValue));

        var d = first.DoubleValue;

        if (double.IsNaN(d))
            return JSNumber.NaN;

        // Math.sign preserves the sign of zero: +0 → +0, -0 → -0. Note that
        // d == -0.0 is also true for +0.0, so use the sign bit to distinguish.
        if (d == 0.0)
            return double.IsNegative(d) ? JSNumber.NegativeZero : JSNumber.Zero;

        var r = Math.Sign(d);
        return new JSNumber(r);
    }

    [JSExport]
    public static JSValue Sin(in Arguments args)
    {
        var first = args.Get1();
        var d = first.DoubleValue;
        var r = new JSNumber(Math.Sin(d));

        return r;
    }

    [JSExport]
    public static JSValue Sinh(in Arguments args)
    {
        var first = args.Get1();
        var d = first.DoubleValue;
        var r = new JSNumber(Math.Sinh(d));

        return r;
    }

    [JSExport]
    public static JSValue Sqrt(in Arguments args)
    {
        var first = args.Get1();
        var d = first.DoubleValue;
        var r = new JSNumber(Math.Sqrt(d));

        return r;
    }

    [JSExport]
    public static JSValue Tan(in Arguments args)
    {
        var first = args.Get1();
        var d = first.DoubleValue;
        var r = new JSNumber(Math.Tan(d));

        return r;
    }

    [JSExport]
    public static JSValue Tanh(in Arguments args)
    {
        var first = args.Get1();
        var d = first.DoubleValue;
        var r = new JSNumber(Math.Tanh(d));

        return r;
    }

    [JSExport]
    public static JSValue Trunc(in Arguments args)
    {
        var first = args.Get1();
        if (first.IsDecimal)
            return JSValue.CreateDecimal(Math.Truncate(first.DecimalValue));

        var d = first.DoubleValue;
        var r = new JSNumber(Math.Truncate(d));
        return r;
    }

    /// <summary>
    /// ES2026 §4.2 — Math.sumPrecise(iterable)
    /// Returns the sum of values from an iterable using Neumaier compensated
    /// summation for improved floating-point precision.
    /// </summary>
    // 2**1023, 2**53 and 2**(1023-52); computed with ScaleB so no decimal-literal rounding creeps in.
    private static readonly double Two1023 = Math.ScaleB(1.0, 1023);
    private static readonly double Two53 = Math.ScaleB(1.0, 53);
    private static readonly double MaxUlp = Math.ScaleB(1.0, 971); // == double.MaxValue - PENULTIMATE_DOUBLE

    // (hi, lo) such that hi + lo == x + y exactly and hi == fl(x + y). Requires |x| >= |y|.
    private static (double hi, double lo) TwoSum(double x, double y)
    {
        var hi = x + y;
        var lo = y - (hi - x);
        return (hi, lo);
    }

    // Object.is for two non-finite operands: NaN matches NaN; ±Infinity match only themselves.
    private static bool SameNonFinite(double a, double b) => (double.IsNaN(a) && double.IsNaN(b)) || a == b;

    /// <summary>
    /// Math.sumPrecise — the maximally precise (correctly rounded) sum of a finite list of Numbers.
    /// Ports the TC39 reference algorithm (Shewchuk/Python-fsum exact addition, as used by
    /// CPython's math.fsum, extended with a "biased" partial that tracks multiples of 2**1024 so
    /// intermediate overflow does not lose precision). See
    /// https://github.com/tc39/proposal-math-sum.
    /// </summary>
    [JSExport("sumPrecise", Length = 1)]
    public static JSValue SumPrecise(in Arguments args)
    {
        var iterable = args.Get1();
        if (iterable.IsNullOrUndefined)
            throw JSEngine.NewTypeError("Math.sumPrecise requires an iterable argument");

        var en = iterable.GetIterableEnumerator();

        // Pull the next Number summand (skipping holes); a non-Number element is a TypeError after
        // closing the iterator. Returns false at end of iteration.
        bool Next(out double value)
        {
            while (en.MoveNext(out var hasValue, out var item, out _))
            {
                if (!hasValue)
                    continue;

                if (item is not JSNumber number)
                {
                    if (en is IReturnableEnumerator returnable)
                        returnable.Return();
                    throw JSEngine.NewTypeError("Math.sumPrecise only accepts Number values");
                }

                value = number.value;
                return true;
            }

            value = 0;
            return false;
        }

        // Once a non-finite value is seen, the remaining (finite) summands cannot affect the result;
        // only the non-finite values matter. Summing two distinct non-finite values gives NaN, while
        // a non-finite value with itself gives itself. The rest of the iterable is still consumed
        // (and type-checked).
        double DrainNonFinite(double current)
        {
            while (Next(out var value))
            {
                if (!double.IsFinite(value) && !SameNonFinite(value, current))
                    current = double.NaN;
            }

            return current;
        }

        var partials = new List<double>();
        double overflow = 0; // conceptually 2**1024 times this value; the final (biased) partial

        // Skip a leading run of -0 (the accumulator is -0𝔽, so an all--0 list — and the empty list —
        // yields -0), stopping at the first value that is not -0.
        while (true)
        {
            if (!Next(out var value))
                return new JSNumber(-0.0);

            if (!(value == 0.0 && double.IsNegative(value)))
            {
                if (!double.IsFinite(value))
                    return new JSNumber(DrainNonFinite(value));
                partials.Add(value);
                break;
            }
        }

        // Main loop: fold each summand into the non-overlapping partials, spilling whole-2**1024
        // overflow into the biased partial so the running sum can exceed the double range losslessly.
        while (Next(out var value))
        {
            var x = value;
            if (!double.IsFinite(x))
                return new JSNumber(DrainNonFinite(x));

            var used = 0;
            for (var idx = 0; idx < partials.Count; idx++)
            {
                var y = partials[idx];
                if (Math.Abs(x) < Math.Abs(y))
                    (x, y) = (y, x);

                var (hi, lo) = TwoSum(x, y);
                if (double.IsInfinity(hi))
                {
                    var sign = hi > 0 ? 1 : -1;
                    overflow += sign;
                    if (Math.Abs(overflow) >= Two53)
                        throw JSEngine.NewRangeError("Math.sumPrecise: intermediate overflow");

                    x = (x - sign * Two1023) - sign * Two1023;
                    if (Math.Abs(x) < Math.Abs(y))
                        (x, y) = (y, x);
                    (hi, lo) = TwoSum(x, y);
                }

                if (lo != 0)
                    partials[used++] = lo;
                x = hi;
            }

            partials.RemoveRange(used, partials.Count - used);
            if (x != 0)
                partials.Add(x);
        }

        // Compute the exact sum of the partials (plus the biased overflow), stopping once it becomes
        // inexact, then apply round-half-to-even.
        var n = partials.Count - 1;
        double resultHi = 0, resultLo = 0;

        if (overflow != 0)
        {
            var next = n >= 0 ? partials[n] : 0;
            --n;
            if (Math.Abs(overflow) > 1 || (overflow > 0 && next > 0) || (overflow < 0 && next < 0))
                return new JSNumber(overflow > 0 ? double.PositiveInfinity : double.NegativeInfinity);

            // |overflow| == 1: do the arithmetic with a factor of 2 dropped so it cannot overflow.
            (resultHi, resultLo) = TwoSum(overflow * Two1023, next / 2);
            resultLo *= 2;
            if (double.IsInfinity(2 * resultHi))
            {
                if (resultHi > 0)
                {
                    if (resultHi == Two1023 && resultLo == -(MaxUlp / 2) && n >= 0 && partials[n] < 0)
                        return new JSNumber(double.MaxValue);
                    return new JSNumber(double.PositiveInfinity);
                }

                if (resultHi == -Two1023 && resultLo == MaxUlp / 2 && n >= 0 && partials[n] > 0)
                    return new JSNumber(-double.MaxValue);
                return new JSNumber(double.NegativeInfinity);
            }

            if (resultLo != 0)
            {
                partials[n + 1] = resultLo;
                ++n;
                resultLo = 0;
            }

            resultHi *= 2;
        }

        while (n >= 0)
        {
            var x = resultHi;
            var y = partials[n];
            --n;
            (resultHi, resultLo) = TwoSum(x, y);
            if (resultLo != 0)
                break;
        }

        // When the roundoff is exactly half a ULP, the next partial's sign decides the rounding.
        if (n >= 0 && ((resultLo < 0.0 && partials[n] < 0.0) || (resultLo > 0.0 && partials[n] > 0.0)))
        {
            var y = resultLo * 2.0;
            var x = resultHi + y;
            var yr = x - resultHi;
            if (y == yr)
                resultHi = x;
        }

        return new JSNumber(resultHi);
    }
}
