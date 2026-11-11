using Broiler.JavaScript.ExpressionCompiler;
using System;
using System.Globalization;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.BuiltIns.Function;
using Broiler.JavaScript.Engine.Core;
using Broiler.ECMAScript.Intl;
using Broiler.ECMAScript.Numbers;
using Broiler.JavaScript.BuiltIns.Intl;

namespace Broiler.JavaScript.BuiltIns.Decimal;

static class JSDecimalExtensions
{
    public static decimal AsDecimalOnly(this JSValue @this) => @this is JSDecimal v ? v.value : throw JSDecimal.CannotMix();
}

[JSBaseClass("Object")]
[JSFunctionGenerator("Decimal")]
public partial class JSDecimal : JSPrimitive
{
    public static JSException CannotMix() => JSEngine.NewTypeError("Cannot mix BigInt and other types, use explicit conversions");

    internal readonly decimal value;

    public override bool IsDecimal => true;

    public override decimal DecimalValue => value;

    public override bool BooleanValue => value != 0;

    public override double DoubleValue => throw CannotMix();

    public override long BigIntValue => throw CannotMix();

    [JSExport(IsConstructor = true)]
    public static JSValue Constructor(in Arguments a)
    {
        var f = a[0];

        switch (f)
        {
            case JSNumber number:
                return new JSDecimal((decimal)number.value);
            case JSDecimal bigint:
                return bigint;
        }

        var text = f.ToString();
        text = text.TrimEnd('m').Replace("_", "");

        if (!decimal.TryParse(text, out var v))
            throw JSEngine.NewTypeError($"{f} is not a valid big integer");

        return new JSDecimal(v);
    }

    public JSDecimal(decimal value) => this.value = value;
    public JSDecimal(string stringValue)
    {
        var v = stringValue.TrimEnd('m').Replace("_", "");

        if (!decimal.TryParse(v, out var n))
            throw JSEngine.NewTypeError($"{stringValue} is not a valid big integer");

        value = n;
    }

    public override bool Equals(JSValue value)
    {
        if (value is JSDecimal bigint)
            return this.value == bigint.value;

        var n = (long)value.DoubleValue;
        return this.value == n;
    }

    public override string ToString() => value.ToString() + "n";

    public override JSValue InvokeFunction(in Arguments a) => throw JSEngine.NewTypeError($"{this} is not a function");

    public override JSValue CreateInstance(in Arguments a)
    {
        if (a.Length == 0)
            return new JSDecimal(0);

        var value = a[0];
        if (value is JSDecimal d)
            return d;

        if (value.IsNumber)
            return new JSDecimal((long)value.DoubleValue);

        var v = long.Parse(value.ToString());
        return new JSDecimal(v);
    }

    public override bool StrictEquals(JSValue value)
    {
        if (value is not JSDecimal bigint)
            return false;

        return this.value == bigint.value;
    }

    public override bool EqualsLiteral(string value) => this.value.ToString() == value;
    public override bool EqualsLiteral(double value) => (double)this.value == value;
    public override JSValue TypeOf() => JSConstants.Decimal;
    protected override JSValue GetPrototype() => Intrinsics.Prototype(Names.Decimal);
    internal override PropertyKey ToKey(bool create = true) => (uint)value;

    public override bool ConvertTo(Type type, out object value)
    {
        if (type == typeof(long))
        {
            value = this.value;
            return true;
        }

        if (type == typeof(ulong))
        {
            value = (ulong)this.value;
            return true;
        }

        if (type.IsAssignableFrom(typeof(JSDecimal)))
        {
            value = this;
            return true;
        }

        if (type == typeof(object))
        {
            value = this.value;
            return true;
        }

        return base.ConvertTo(type, out value);
    }

    public override JSValue Negate() => new JSDecimal(-value);

    public override JSValue BitwiseAnd(JSValue value) => throw CannotMix();

    public override JSValue BitwiseOr(JSValue value) => throw CannotMix();

    public override JSValue BitwiseXor(JSValue value) => throw CannotMix();

    public override JSValue LeftShift(JSValue value) => throw CannotMix();

    public override JSValue RightShift(JSValue value) => throw CannotMix();

    public override JSValue UnsignedRightShift(JSValue value) => throw CannotMix();

    public override JSValue Multiply(JSValue value) => new JSDecimal(this.value * value.AsDecimalOnly());

    public override JSValue Divide(JSValue value) => new JSDecimal(this.value / value.AsDecimalOnly());

    public override JSValue Subtract(JSValue value) => new JSDecimal(this.value - value.AsDecimalOnly());

    public override JSValue AddValue(double value) => throw CannotMix();

    public override JSValue AddValue(string value) => new JSString(this.value + value);

    public override JSValue AddValue(JSValue value)
    {
        value = value is JSObject obj ? obj.ToDefaultPrimitive() : value;
        
        if (value is JSPrimitiveObject primitive)
            value = primitive.value;
        
        if (value is JSDecimal b)
            return new JSDecimal(this.value + b.value);

        if (value.IsBoolean || value.IsNumber)
            throw CannotMix();

        if (value is JSString @string)
            return new JSString(this.value.ToString() + "n" + @string.ToString());

        if (value is JSObject @object)
            return new JSString(this.value + @object.StringValue);

        return new JSDecimal(this.value + value.BigIntValue);
    }

    [JSExport("toString")]
    public JSValue JSToString() => new JSString(value.ToString());


    [JSExport("toFixed")]
    public JSValue JSToFixed(in Arguments a)
    {
        var nv = value;
        
        if (a.Get1() is JSNumber n1)
        {
            if (double.IsNaN(n1.value) || n1.value > 20 || n1.value < 0)
                throw JSEngine.NewRangeError("toFixed() digits argument must be between 0 and 100");

            var i = (int)n1.value;
            if (nv > 999999999999999.0m && i <= 15)
                return new JSString(nv.ToString("g21"));

            return new JSString(nv.ToString($"F{i}"));
        }

        if (nv > 999999999999999.0m)
            return new JSString(nv.ToString("g21"));

        return new JSString(nv.ToString("F0"));
    }

    // Decimal.prototype.toLocaleString(locales, options), as ECMA-402's Number.prototype.toLocaleString
    // (sec-number.prototype.tolocalestring) reads its two arguments: a new Intl.NumberFormat over `locales` and
    // `options`, which formats this value's exact decimal digits (ECMA-402's Intl mathematical value, the shared
    // library's IntlDecimal, sub-area (b)) through the shared NumberFormatter (sub-area (c)). With no `locales` the
    // locale is the default-locale provider's (Intl.JSIntlLocaleProviders.DefaultLocale, JSD-0082's decision D7, row
    // L35), as every Intl service's is, and the host's culture is read only behind that provider and the
    // host-culture provider (decision D6). Revised 2026-11-11, on the owner's ruling of that day on SJL-9's clause 8,
    // "Fix in code"; until then the method was
    // `public JSValue ToLocaleString(in Arguments a) => new JSString(value.ToString(CultureInfo.CurrentCulture));`,
    // with the comment "This toLocale* method reads the host's culture directly, not the default-locale provider
    // (Intl.JSIntlLocaleProviders.DefaultLocale) every Intl service reads. Under JSD-0082's decision D7, one default
    // (row L35), it is a reader of DefaultLocale; it is listed for task 8.4, the number sub-area, which moves it onto
    // that provider or records why it stays (listed 2026-11-04, at SJL-9.5)."
    // Its length stays 0, as Number.prototype.toLocaleString's is; the generator would infer 2 from the body.
    [JSExport("toLocaleString", Length = 0)]
    public JSValue ToLocaleString(in Arguments a)
    {
        var (locales, options) = a.Get2();
        var numberFormat = new JSIntlNumberFormat(new Arguments(JSUndefined.Value, locales, options));
        return new JSString(numberFormat.Formatter.Format(ToIntlDecimal(value), null));
    }

    // The value's exact decimal as the shared library's IntlDecimal: its invariant digits, read exactly (a .NET decimal
    // has at most 29 significant digits and lies well inside the Number range, so IntlDecimal.FromString keeps every
    // digit), with the sign of a negative zero kept.
    internal static IntlDecimal ToIntlDecimal(decimal value)
    {
        var text = value.ToString(CultureInfo.InvariantCulture);
        if (value == 0m && decimal.IsNegative(value))
            text = "-" + text;
        return IntlDecimal.FromString(text, BigIntPolicy.Unbounded);
    }

    [JSExport("valueOf")]
    public override JSValue ValueOf() => this;
}
