using Broiler.ECMAScript.Numbers;
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
                && ReferenceEquals(@object, Intrinsics.Prototype(Names.Number)))
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
        using var counted = Runtime.ArithmeticOperandDiagnostics.Enabled
            ? Runtime.ArithmeticOperandDiagnostics.Relate(true, value.IsNumber)
            : default;
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
        using var counted = Runtime.ArithmeticOperandDiagnostics.Enabled
            ? Runtime.ArithmeticOperandDiagnostics.Relate(true, value.IsNumber)
            : default;
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
        using var counted = Runtime.ArithmeticOperandDiagnostics.Enabled
            ? Runtime.ArithmeticOperandDiagnostics.Relate(true, value.IsNumber)
            : default;
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
        using var counted = Runtime.ArithmeticOperandDiagnostics.Enabled
            ? Runtime.ArithmeticOperandDiagnostics.Relate(true, value.IsNumber)
            : default;
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

    // The digits are the shared library's (SJL-3.5); these built-ins keep the coercion order, the
    // RangeErrors and the string creation.

    [JSPrototypeMethod]
    [JSExport("toString", Length = 1)]

    public static JSString ToString(in Arguments a)
    {
        var value = a.This.ToNumber().value;
        var radix = a.Get1();
        if (radix.IsUndefined)
            return new JSString(NumberText.ToString(value));

        // §21.1.3.6 steps 3-4: ToIntegerOrInfinity(radix), so null, NaN and 0 are 0 and out of range.
        var radixMV = ToIntegerOrInfinity(radix);
        if (radixMV < 2 || radixMV > 36)
            throw JSEngine.NewRangeError("The radix must be between 2 and 36, inclusive.");

        return new JSString(NumberText.ToRadixString(value, (int)radixMV));
    }

    [JSPrototypeMethod]
    [JSExport("toExponential", Length = 1)]
    public static JSValue ToExponential(in Arguments a)
    {
        var nv = a.This.ToNumber().value;
        var fractionDigits = a.Get1();
        var hasFractionDigits = !fractionDigits.IsUndefined;

        // Step 2: ToIntegerOrInfinity(fractionDigits), with any observable coercion, before the
        // non-finite short-circuit of step 4 (NaN.toExponential(-1) === "NaN").
        var f = hasFractionDigits ? ToIntegerOrInfinity(fractionDigits) : 0;
        if (!double.IsFinite(nv))
            return new JSString(NumberText.ToString(nv));

        if (f < 0 || f > 100)
            throw JSEngine.NewRangeError("toExponential() digits argument is out of range");

        return new JSString(hasFractionDigits ? NumberText.ToExponential(nv, (int)f) : NumberText.ToExponential(nv));
    }

    [JSPrototypeMethod]
    [JSExport("toFixed", Length = 1)]
    public static JSValue ToFixed(in Arguments a)
    {
        var nv = a.This.ToNumber().value;

        // Steps 2-5: ToIntegerOrInfinity(fractionDigits), and the range check, which an infinity
        // fails, before a non-finite x is looked at.
        var f = ToIntegerOrInfinity(a.Get1());
        if (f < 0 || f > 100)
            throw JSEngine.NewRangeError("toFixed() digits argument must be between 0 and 100");

        return new JSString(NumberText.ToFixed(nv, (int)f));
    }

    [JSPrototypeMethod]
    [JSExport("toPrecision", Length = 1)]
    public static JSValue ToPrecision(in Arguments a)
    {
        var nv = a.This.ToNumber().value;
        var precision = a.Get1();
        if (precision.IsUndefined)
            return new JSString(NumberText.ToString(nv));

        // Step 3 coerces the precision before step 4 looks at x, so a valueOf that throws throws
        // even for Infinity; step 5 checks the truncated value, so 100.5 is 100.
        var p = ToIntegerOrInfinity(precision);
        if (!double.IsFinite(nv))
            return new JSString(NumberText.ToString(nv));

        if (p < 1 || p > 100)
            throw JSEngine.NewRangeError("toPrecision() digits argument must be between 1 and 100");

        return new JSString(NumberText.ToPrecision(nv, (int)p));
    }

    // ToIntegerOrInfinity (ECMA-262 7.1.5): ToNumber, which throws on a Symbol or a BigInt, then
    // NaN is 0 and the rest is truncated toward zero.
    private static double ToIntegerOrInfinity(JSValue value)
    {
        var number = value.DoubleValue;
        return double.IsNaN(number) ? 0 : Math.Truncate(number);
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

    /// <summary>Number::toString(x, radix): the shortest digits that read back as the value.</summary>
    [Obsolete("Use Broiler.ECMAScript.Numbers.NumberText.ToRadixString, which this forwards to. To be removed in the next major version.")]
    public static string DecimalToBase(double number, int radix) => NumberText.ToRadixString(number, radix);

    /// <summary>
    /// The value in the radix (2 to 36). It forwards to Number::toString, so a value beyond 2^53 is
    /// first rounded to the nearest double.
    /// </summary>
    [Obsolete("Use Broiler.ECMAScript.Numbers.NumberText.ToRadixString, which this forwards to. To be removed in the next major version.")]
    public static string DecimalToArbitrarySystem(long decimalNumber, int radix) => NumberText.ToRadixString(decimalNumber, radix);
}
