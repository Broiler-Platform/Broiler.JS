using Broiler.ECMAScript.Numbers;
using Broiler.JavaScript.ExpressionCompiler;
using System;
using System.Numerics;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.Engine;

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

    // Math.round, pow, hypot, cbrt, expm1, log1p, f16round and sumPrecise are adapters (SJL-4.5): the
    // kernels are the shared library's MathKernels, Float16 and PreciseSum, and these built-ins keep
    // the coercion of each argument, in the specification's order, the errors, the iteration and the
    // value wrapping. The deleted kernels were JSMath.Round's and Pow's bodies, both Hypot overloads,
    // CorrectlyRoundedCbrt, Expm1's Kahan form, Log1p's series and SumPrecise's expansion with TwoSum,
    // SameNonFinite and their constants.

    /// <summary><c>Math.round</c> (ECMA-262 21.3.2.28): <c>ToNumber</c> of the argument, then <c>MathKernels.Round</c>.</summary>
    [JSExport]
    public static JSValue Round(in Arguments args)
    {
        var first = args.Get1();

        // The engine's decimal extension (the `m` suffix, SJL-D-17) is not a Number and stays here.
        if (first.IsDecimal)
            return CreateDecimal(Math.Floor(first.DecimalValue + 0.5m));

        return new JSNumber(MathKernels.Round(first.DoubleValue));
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
            return CreateDecimal(Math.Floor(first.DecimalValue));

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
            return CreateDecimal(Math.Abs(first.DecimalValue));

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

    /// <summary><c>Math.cbrt</c> (ECMA-262 21.3.2.9): <c>ToNumber</c> of the argument, then <c>MathKernels.Cbrt</c>, correctly rounded.</summary>
    [JSExport]
    public static JSValue Cbrt(in Arguments args) => new JSNumber(MathKernels.Cbrt(args.Get1().DoubleValue));

    [JSExport]
    public static JSValue Ceil(in Arguments args)
    {
        var first = args.Get1();
        if (first.IsDecimal)
            return CreateDecimal(Math.Ceiling(first.DecimalValue));

        var d = first.DoubleValue;
        var r = new JSNumber(Math.Ceiling(d));
        return r;
    }

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
        var x = ToUint32(d);

        return new JSNumber(BitOperations.LeadingZeroCount(x));
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

    /// <summary><c>Math.expm1</c> (ECMA-262 21.3.2.15): <c>ToNumber</c> of the argument, then <c>MathKernels.Expm1</c>.</summary>
    [JSExport]
    public static JSValue Expm1(in Arguments args) => new JSNumber(MathKernels.Expm1(args.Get1().DoubleValue));

    [JSExport]
    public static JSValue Fround(in Arguments args)
    {
        var first = args.Get1();
        var d = first.DoubleValue;
        var r = (double)(float)d;

        return new JSNumber(r);
    }

    /// <summary>
    /// <c>Math.f16round</c> (ECMA-262 21.3.2.17): <c>ToNumber</c> of the argument, then
    /// <c>Float16.Round</c>, which rounds once from the double's bits.
    /// </summary>
    [JSExport("f16round")]
    public static JSValue F16round(in Arguments args) => new JSNumber(Float16.Round(args.Get1().DoubleValue));

    /// <summary>
    /// <c>Math.hypot</c> (ECMA-262 21.3.2.18): <c>ToNumber</c> of every argument, in order, before any
    /// value is looked at (steps 2-3), then <c>MathKernels.Hypot</c> over the converted values.
    /// </summary>
    [JSExport(Length = 2)]
    public static JSValue Hypot(in Arguments args)
    {
        var length = args.Length;
        Span<double> numbers = length <= 16 ? stackalloc double[length] : new double[length];
        for (var i = 0; i < length; i++)
            numbers[i] = args.GetAt(i).DoubleValue;

        return new JSNumber(MathKernels.Hypot(numbers));
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


    /// <summary><c>Math.log1p</c> (ECMA-262 21.3.2.21): <c>ToNumber</c> of the argument, then <c>MathKernels.Log1p</c>.</summary>
    [JSExport]
    public static JSValue Log1p(in Arguments args) => new JSNumber(MathKernels.Log1p(args.Get1().DoubleValue));

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

    /// <summary>
    /// <c>Math.pow</c> (ECMA-262 21.3.2.26): <c>ToNumber</c> of the base, then of the exponent, then
    /// <c>MathKernels.Power</c>, the one <c>Number::exponentiate</c> of <c>**</c> and <c>Math.pow</c>.
    /// </summary>
    [JSExport]
    public static JSValue Pow(in Arguments args)
    {
        var (first, second) = args.Get2();
        var @base = first.DoubleValue;
        var exponent = second.DoubleValue;
        return new JSNumber(MathKernels.Power(@base, exponent));
    }

    [JSExport]
    public static JSValue Sign(in Arguments args)
    {
        var first = args.Get1();

        if (first.IsDecimal)
            return CreateDecimal(Math.Sign(first.DecimalValue));

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
            return CreateDecimal(Math.Truncate(first.DecimalValue));

        var d = first.DoubleValue;
        var r = new JSNumber(Math.Truncate(d));
        return r;
    }

    /// <summary>The count of values at which <c>Math.sumPrecise</c> refuses its input with a <c>RangeError</c>, 2^53.</summary>
    private const long SumPreciseCountLimit = 1L << 53;

    /// <summary>
    /// <c>Math.sumPrecise</c> (ECMA-262 21.3.2.34): the iteration is this engine's, as the clause has
    /// it, and the sum is the shared <c>PreciseSum</c>'s, the exact sum rounded once.
    /// </summary>
    /// <remarks>
    /// RequireObjectCoercible, then <c>GetIterator(items, sync)</c>, then <c>IteratorStepValue</c>
    /// until the iterator is done. A count reaching 2^53 is a <c>RangeError</c>, and a value that is
    /// not a Number a <c>TypeError</c>; each closes the iterator first, and is what propagates
    /// whatever <c>return()</c> does (<c>IteratorClose</c> with a throw completion). Present only
    /// under <see cref="JavaScriptFeatureFlags.MathSumPrecise"/>, this engine's policy.
    /// </remarks>
    [JSExport("sumPrecise", Length = 1, Feature = (int)JavaScriptFeatureFlags.MathSumPrecise)]
    public static JSValue SumPrecise(in Arguments args)
    {
        var items = args.Get1();
        if (items.IsNullOrUndefined)
            throw JSEngine.NewTypeError("Math.sumPrecise requires an iterable argument");

        var iterator = items.GetIterableEnumerator();
        var sum = new PreciseSum();
        long count = 0;
        while (iterator.MoveNext(out var next))
        {
            if (++count >= SumPreciseCountLimit)
            {
                var error = JSEngine.NewRangeError("Math.sumPrecise accepts fewer than 2^53 values");
                CloseIteratorAfterError(iterator);
                throw error;
            }

            if (next is not JSNumber number)
            {
                var error = JSEngine.NewTypeError("Math.sumPrecise only accepts Number values");
                CloseIteratorAfterError(iterator);
                throw error;
            }

            sum.Add(number.value);
        }

        return new JSNumber(sum.Result());
    }

    /// <summary>
    /// <c>IteratorClose(iteratorRecord, completion)</c> for a throw completion: calls
    /// <c>return()</c>, and discards what it throws or answers, so the completion's error propagates.
    /// </summary>
    private static void CloseIteratorAfterError(IElementEnumerator iterator)
    {
        if (iterator is not IReturnableEnumerator returnable)
            return;

        try
        {
            returnable.Return();
        }
        catch
        {
            // The throw completion wins over anything return() does.
        }
    }
}
