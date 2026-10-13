// REFERENCE ARM J: a test-only copy of Broiler.JS's implementation before the cut-over, for the
// reference-arm comparison of bundle sjl-4-001 (docs/evidence/sjl-4-001/reference-arms.md). It is
// never referenced outside this test project, and it is retired at SJL-16.
//
// Copied from Broiler.JS at 4899f9c (SJL-3's merge, bundle sjl-4-001's base):
//   Broiler.JS/Broiler.JavaScript.BuiltIns/Objects/JSMath.cs
//     blob 2a886f800921f506a36be355e461a294514f7de5: Round (:48-79), Cbrt (:200-206), CorrectlyRoundedCbrt
//     (:208-231), Expm1 (:291-313), Hypot(in Arguments) (:339-365), Hypot(double, double) (:367-388), Log1p
//     (:422-440), Pow (:491-511), Two1023, Two53 and MaxUlp (:602-605), TwoSum (:607-613), SameNonFinite
//     (:615-616) and SumPrecise (:618-791)
//
// Changes from the source, none of which changes a result:
//   - line endings normalised to LF where the source has CRLF;
//   - the namespace; the members sit in a static class of their own instead of the JSObject JSMath; the
//     private members are internal;
//   - each built-in takes the double(s) of ToNumber (`args.Get1().DoubleValue` and the like; the local the
//     source initialises is the parameter) and returns the Number's double: `new JSNumber(x)` is `x`,
//     `JSNumber.NaN` double.NaN, `JSNumber.Zero` 0d and `JSNumber.One` 1d;
//   - Round's `first.IsUndefined` (NaN), `first.IsNull` (+0) and `first.IsDecimal` (a Decimal value, not a
//     Number) branches are the adapter's; for undefined and null they return what the double path returns
//     for ToNumber's NaN and +0;
//   - Hypot(in Arguments) takes the arguments' doubles in order as a ReadOnlySpan<double>: `args.Length` is
//     `args.Length`, `args.Get1()` / `args.Get2()` / `args.GetAt(i)` with `.DoubleValue` are `args[0]`,
//     `args[1]` and `args[i]` (the source converts the third and later arguments after Hypot(d1, d2), which is
//     the adapter's to reproduce);
//   - SumPrecise takes the iterable's Numbers in order as an IReadOnlyList<double>: the iterable's
//     null/undefined TypeError, GetIterableEnumerator, the skipping of holes, the TypeError (and iterator close)
//     for a non-Number and the MathSumPrecise feature flag are the adapter's, and the local function Next reads
//     the next element of the list (`numbers`) where the source reads the enumerator; its RangeError
//     (intermediate overflow) is a null result (the return type is double?), the throw kept in a comment;
//   - the [JSExport...] attributes are not copied.

using System;
using System.Collections.Generic;

namespace Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.Numerics.J;

/// <summary>The Math built-ins whose numerics the shared library takes over.</summary>
internal static class JSMath
{
    internal static double Round(double number)
    {
        if (double.IsNaN(number) || double.IsInfinity(number) || number == 0.0)
            return number;

        // §21.3.2.28 Math.round: the result is the integer closest to x, with halves
        // rounded toward +Infinity. Use floor(x) + (frac >= 0.5 ? 1 : 0) rather than
        // floor(x + 0.5): adding 0.5 loses precision, rounding 0.5 - ε/4 up to 1 and
        // perturbing large odd integers. This also yields -0 for x in (-0.5, -0]
        // (test262: Math/round/S15.8.2.15_A7).
        var floor = Math.Floor(number);
        var result = number - floor >= 0.5 ? floor + 1.0 : floor;
        if (result == 0.0 && number < 0.0)
            return -0.0D;

        return result;
    }

    internal static double Cbrt(double d)
    {
        return CorrectlyRoundedCbrt(d);
    }

    // System.Math.Cbrt can be off by one ULP (e.g. Math.Cbrt(27) is
    // 3.0000000000000004), which breaks perfect-cube landmarks. Choose whichever of
    // the result and its two neighbours cubes closest to the input. The ±0, ±∞ and
    // NaN cases are returned as-is (Math.Cbrt already preserves the sign of zero and
    // propagates non-finite values).
    internal static double CorrectlyRoundedCbrt(double d)
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

    internal static double Expm1(double d)
    {
        // expm1(±0) === ±0: Math.Exp(-0)-1 would lose the sign of zero, so pass
        // ±0 (and NaN) through unchanged.
        if (d == 0.0 || double.IsNaN(d))
            return d;
        var u = Math.Exp(d);
        // |d| tiny: e^d rounds to 1, so e^d − 1 underflows to 0 and loses all precision; expm1(d) ≈ d.
        if (u == 1.0)
            return d;
        var um1 = u - 1.0;
        var logU = Math.Log(u);
        // When e^d overflowed to +∞ (logU = +∞) or underflowed to 0 (logU = −∞), the correction is
        // undefined but u − 1 (= +∞ or −1) is already exact.
        if (double.IsInfinity(logU))
            return um1;
        // Accurate expm1 = (e^d − 1) · d / ln(e^d): the relative errors of (u − 1) and ln(u) cancel,
        // giving a result good to ~1 ulp where the direct e^d − 1 would not.
        return um1 * d / logU;
    }

    internal static double Hypot(ReadOnlySpan<double> args)
    {
        int length = args.Length;

        if (length == 0)
            return 0d;

        if (length == 1)
            return Math.Abs(args[0]);

        double d1 = args[0];
        double d2 = args[1];

        if (length == 2)
            return Hypot(d1, d2);

        double result = Hypot(d1, d2);
        for (int i = 2; i < length; i++)
        {
            double val = args[i];
            result = Hypot(result, val);
        }

        return result;
    }

    internal static double Hypot(double number1, double number2)
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

    internal static double Log1p(double d)
    {
        double r;

        if (Math.Abs(d) < 0.01)
        {
            // For small numbers, use a taylor series approximation.
            r = d * (1.0 + d * (-1.0 / 2.0 + d * (1.0 / 3.0 + d *
                (-1.0 / 4.0 + d * (1.0 / 5.0 + d * (-1.0 / 6.0 + d * (1.0 / 7.0)))))));
            return r;
        }

        // Otherwise just use the normal log function.
        r = Math.Log(1.0 + d);
        return r;
    }

    internal static double Pow(double @base, double exponent)
    {
        // Number::exponentiate step 1: a NaN exponent always yields NaN, even for
        // base 1 where IEEE pow(1, NaN) would return 1.
        if (double.IsNaN(exponent))
            return double.NaN;

        if ((@base == 1.0 || @base == -1) && double.IsInfinity(exponent))
            return double.NaN;

        if (double.IsNaN(@base) && exponent == 0.0)
            return 1d;

        var r = Math.Pow(@base, exponent);
        return r;
    }

    /// <summary>
    /// ES2026 §4.2 — Math.sumPrecise(iterable)
    /// Returns the sum of values from an iterable using Neumaier compensated
    /// summation for improved floating-point precision.
    /// </summary>
    // 2**1023, 2**53 and 2**(1023-52); computed with ScaleB so no decimal-literal rounding creeps in.
    internal static readonly double Two1023 = Math.ScaleB(1.0, 1023);
    internal static readonly double Two53 = Math.ScaleB(1.0, 53);
    internal static readonly double MaxUlp = Math.ScaleB(1.0, 971); // == double.MaxValue - PENULTIMATE_DOUBLE

    // (hi, lo) such that hi + lo == x + y exactly and hi == fl(x + y). Requires |x| >= |y|.
    internal static (double hi, double lo) TwoSum(double x, double y)
    {
        var hi = x + y;
        var lo = y - (hi - x);
        return (hi, lo);
    }

    // Object.is for two non-finite operands: NaN matches NaN; ±Infinity match only themselves.
    internal static bool SameNonFinite(double a, double b) => (double.IsNaN(a) && double.IsNaN(b)) || a == b;

    /// <summary>
    /// Math.sumPrecise — the maximally precise (correctly rounded) sum of a finite list of Numbers.
    /// Ports the TC39 reference algorithm (Shewchuk/Python-fsum exact addition, as used by
    /// CPython's math.fsum, extended with a "biased" partial that tracks multiples of 2**1024 so
    /// intermediate overflow does not lose precision). See
    /// https://github.com/tc39/proposal-math-sum.
    /// </summary>
    internal static double? SumPrecise(IReadOnlyList<double> numbers)
    {
        var index = 0;

        // Pull the next Number summand (skipping holes); a non-Number element is a TypeError after
        // closing the iterator. Returns false at end of iteration.
        bool Next(out double value)
        {
            if (index < numbers.Count)
            {
                value = numbers[index++];
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
                return -0.0;

            if (!(value == 0.0 && double.IsNegative(value)))
            {
                if (!double.IsFinite(value))
                    return DrainNonFinite(value);
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
                return DrainNonFinite(x);

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
                        return null; // throw JSEngine.NewRangeError("Math.sumPrecise: intermediate overflow");

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
                return overflow > 0 ? double.PositiveInfinity : double.NegativeInfinity;

            // |overflow| == 1: do the arithmetic with a factor of 2 dropped so it cannot overflow.
            (resultHi, resultLo) = TwoSum(overflow * Two1023, next / 2);
            resultLo *= 2;
            if (double.IsInfinity(2 * resultHi))
            {
                if (resultHi > 0)
                {
                    if (resultHi == Two1023 && resultLo == -(MaxUlp / 2) && n >= 0 && partials[n] < 0)
                        return double.MaxValue;
                    return double.PositiveInfinity;
                }

                if (resultHi == -Two1023 && resultLo == MaxUlp / 2 && n >= 0 && partials[n] > 0)
                    return -double.MaxValue;
                return double.NegativeInfinity;
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

        return resultHi;
    }
}
