using Broiler.JavaScript.ExpressionCompiler;
using System;
using System.Globalization;
using System.Numerics;
using System.Text;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.BuiltIns.Boolean;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.BuiltIns.Function;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.BigInt;

static class JSBigIntExtensions
{
    internal const int NaNComparison = int.MinValue;

    public static BigInteger AsBigIntegerOnly(this JSValue @this)
    {
        @this = @this is JSPrimitiveObject primitiveObject ? primitiveObject.ValueOf() : @this;
        @this = @this is JSObject @object ? @object.ToDefaultPrimitive() : @this;
        return @this is JSBigInt v ? v.value : throw JSBigInt.CannotMix();
    }

    public static JSValue UnwrapPrimitive(this JSValue value)
        => value is JSPrimitiveObject primitiveObject ? primitiveObject.ValueOf() : value;

    public static int CompareToNumber(this BigInteger left, double right)
    {
        if (double.IsNaN(right))
            return NaNComparison;

        if (double.IsPositiveInfinity(right))
            return -1;

        if (double.IsNegativeInfinity(right))
            return 1;

        var truncated = Math.Truncate(right);
        var integerComparison = left.CompareTo(new BigInteger(truncated));
        if (truncated == right || integerComparison != 0)
            return integerComparison;

        return right > 0 ? -1 : 1;
    }
}

[JSBaseClass("Object")]
[JSFunctionGenerator("BigInt")]
public partial class JSBigInt : JSPrimitive
{
    public static JSException CannotMix() => JSEngine.NewTypeError("Cannot mix BigInt and other types, use explicit conversions");

    internal readonly BigInteger value;

    public override bool BooleanValue => value != 0;

    public override bool IsBigInt => true;

    public override double DoubleValue => throw CannotMix();

    public override long BigIntValue => (long)value;

    // BigInt::toNumber — 𝔽(ℝ(x)): the nearest IEEE-754 double, ties to even. .NET's
    // (double)BigInteger truncates toward zero, so it must never back the result Number
    // (e.g. Number(8692288669465520373761n) must round up, not down).
    public static double ToNumber(BigInteger v)
    {
        var negative = v.Sign < 0;
        var magnitude = BigInteger.Abs(v);
        var lo = (double)magnitude; // .NET truncates toward zero, so lo <= magnitude
        if (double.IsInfinity(lo))
            return negative ? double.NegativeInfinity : double.PositiveInfinity;

        double chosen;
        var loBig = new BigInteger(lo);
        if (loBig == magnitude)
        {
            chosen = lo;
        }
        else
        {
            var hi = Math.BitIncrement(lo);
            if (double.IsInfinity(hi))
                return negative ? -lo : lo;

            var distanceLo = magnitude - loBig;
            var distanceHi = new BigInteger(hi) - magnitude;
            chosen = distanceLo < distanceHi ? lo
                : distanceHi < distanceLo ? hi
                : (BitConverter.DoubleToInt64Bits(lo) & 1) == 0 ? lo : hi; // tie → even
        }

        return negative ? -chosen : chosen;
    }

    [JSExport(IsConstructor = true, Length = 1)]
    public static JSValue Constructor(in Arguments a)
    {
        var f = a[0];
        if (f is JSObject objectValue)
            f = objectValue.ToDefaultPrimitive();

        switch (f)
        {
            case JSNumber number:
                return NumberToBigInt(number.value);

            case JSBigInt bigint:
                return bigint;

            case JSBoolean boolean:
                return new JSBigInt(boolean.BooleanValue ? BigInteger.One : BigInteger.Zero);
        }

        var text = f.ToString();
        // StringToBigInt of an empty or whitespace-only string is 0n, not an error
        // (e.g. BigInt(""), new BigInt64Array([""])).
        if (f.IsString && string.IsNullOrWhiteSpace(text))
            return new JSBigInt(BigInteger.Zero);
        if (!TryParseBigIntString(text, out var v))
            throw (f.IsString || f.IsObject)
                ? JSEngine.NewSyntaxError($"{f} is not a valid big integer")
                : JSEngine.NewTypeError($"{f} is not a valid big integer");

        return new JSBigInt(v);
    }

    private static JSValue NumberToBigInt(double number)
    {
        if (double.IsNaN(number)
            || double.IsInfinity(number)
            || Math.Truncate(number) != number)
        {
            throw JSEngine.NewRangeError("The number cannot be converted to a BigInt because it is not an integer");
        }

        return new JSBigInt(new BigInteger(number));
    }

    public JSBigInt(BigInteger value) => this.value = value;
    public JSBigInt(string stringValue)
    {
        if (!TryParseBigIntLiteral(stringValue, out var n))
            throw JSEngine.NewTypeError($"{stringValue} is not a valid big integer");
        value = n;
    }

    private static bool TryParseBigIntString(string value, out BigInteger result)
    {
        var text = value.Trim();
        if (text.Length == 0 || text.Contains('_') || text.EndsWith('n'))
        {
            result = default;
            return false;
        }

        if (text.StartsWith("+", StringComparison.Ordinal) || text.StartsWith("-", StringComparison.Ordinal))
        {
            var rest = text[1..];
            if (rest.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                || rest.StartsWith("0b", StringComparison.OrdinalIgnoreCase)
                || rest.StartsWith("0o", StringComparison.OrdinalIgnoreCase))
            {
                result = default;
                return false;
            }

            return BigInteger.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out result);
        }

        if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            return TryParsePrefixedDigits(text.AsSpan(2), 16, 1, out result);

        if (text.StartsWith("0b", StringComparison.OrdinalIgnoreCase))
            return TryParsePrefixedDigits(text.AsSpan(2), 2, 1, out result);

        if (text.StartsWith("0o", StringComparison.OrdinalIgnoreCase))
            return TryParsePrefixedDigits(text.AsSpan(2), 8, 1, out result);

        return BigInteger.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out result);
    }

    private static bool TryParseBigIntLiteral(string value, out BigInteger result)
    {
        var text = value.Trim().TrimEnd('n').Replace("_", "");
        var sign = 1;

        if (text.StartsWith("+", StringComparison.Ordinal))
            text = text[1..];
        else if (text.StartsWith("-", StringComparison.Ordinal))
        {
            sign = -1;
            text = text[1..];
        }

        if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            return TryParsePrefixedDigits(text.AsSpan(2), 16, sign, out result);

        if (text.StartsWith("0b", StringComparison.OrdinalIgnoreCase))
            return TryParsePrefixedDigits(text.AsSpan(2), 2, sign, out result);

        if (text.StartsWith("0o", StringComparison.OrdinalIgnoreCase))
            return TryParsePrefixedDigits(text.AsSpan(2), 8, sign, out result);

        return BigInteger.TryParse(sign < 0 ? "-" + text : text, out result);
    }

    private static bool TryParsePrefixedDigits(ReadOnlySpan<char> digits, int numberBase, int sign, out BigInteger result)
    {
        if (digits.Length == 0)
        {
            result = default;
            return false;
        }

        result = BigInteger.Zero;

        foreach (var ch in digits)
        {
            int digit = ch switch
            {
                >= '0' and <= '9' => ch - '0',
                >= 'a' and <= 'f' => ch - 'a' + 10,
                >= 'A' and <= 'F' => ch - 'A' + 10,
                _ => -1
            };

            if (digit < 0 || digit >= numberBase)
            {
                result = default;
                return false;
            }

            result = (result * numberBase) + digit;
        }

        if (sign < 0)
            result = BigInteger.Negate(result);

        return true;
    }

    public override bool Equals(JSValue value)
    {
        if (value is JSPrimitiveObject primitiveObject)
            value = primitiveObject.ValueOf();

        if (value is JSBigInt bigint)
            return this.value == bigint.value;

        if (value is JSString str && BigInteger.TryParse(str.ToString(), out var bigintFromString))
            return this.value == bigintFromString;

        // A Boolean operand is converted with ToNumber first (true -> 1, false -> 0)
        // and then compared by mathematical value, per abstract equality.
        if (!value.IsNumber && !value.IsBoolean)
            return false;

        var number = value.DoubleValue;
        if (double.IsNaN(number) || double.IsInfinity(number) || Math.Truncate(number) != number)
            return false;

        return this.value == new BigInteger(number);
    }

    public override string ToString() => value.ToString();

    public override string ToDetailString() => value.ToString() + "n";

    public override JSValue InvokeFunction(in Arguments a) => throw JSEngine.NewTypeError($"{this} is not a function");

    public override JSValue CreateInstance(in Arguments a)
    {
        if (a.Length == 0)
            return new JSBigInt(0);

        var value = a[0];
        if (value is JSBigInt bigint)
            return bigint;

        if (value.IsNumber)
            return NumberToBigInt(value.DoubleValue);

        var v = long.Parse(value.ToString());
        return new JSBigInt(v);
    }

    public override bool StrictEquals(JSValue value)
    {
        if (value is not JSBigInt bigint)
            return false;

        return this.value == bigint.value;
    }

    private bool TryCompare(JSValue value, out int comparison)
    {
        value = value.UnwrapPrimitive();

        switch (value)
        {
            case JSBigInt bigint:
                comparison = this.value.CompareTo(bigint.value);
                return true;
            case var _ when value.IsNumber || value.IsBoolean || value.IsNull || value.IsString:
                comparison = this.value.CompareToNumber(value.DoubleValue);
                return true;
            default:
                comparison = default;
                return false;
        }
    }

    private static bool IsValidComparison(int comparison) => comparison != JSBigIntExtensions.NaNComparison;

    public override bool Less(JSValue value)
        => TryCompare(value, out var comparison) ? IsValidComparison(comparison) && comparison < 0 : base.Less(value);

    public override bool LessOrEqual(JSValue value)
        => TryCompare(value, out var comparison) ? IsValidComparison(comparison) && comparison <= 0 : base.LessOrEqual(value);

    public override bool Greater(JSValue value)
        => TryCompare(value, out var comparison) ? IsValidComparison(comparison) && comparison > 0 : base.Greater(value);

    public override bool GreaterOrEqual(JSValue value)
        => TryCompare(value, out var comparison) ? IsValidComparison(comparison) && comparison >= 0 : base.GreaterOrEqual(value);

    public override bool EqualsLiteral(string value) => this.value.ToString() == value;

    public override bool EqualsLiteral(double value) => (double)this.value == value;


    public override JSValue TypeOf() => JSConstants.BigInt;

    protected override JSValue GetPrototype() => ((JSEngine.Current as JSObject)?[Names.BigInt] as JSFunction).prototype;

    internal override PropertyKey ToKey(bool create = true) => (uint)value;

    public override bool ConvertTo(Type type, out object value)
    {
        if (type == typeof(long))
        {
            value = (long)this.value;
            return true;
        }

        if (type == typeof(ulong))
        {
            value = (ulong)this.value;
            return true;
        }

        if (type.IsAssignableFrom(typeof(JSBigInt)))
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

    public override JSValue Negate() => new JSBigInt(-value);

    public override JSValue Increment() => new JSBigInt(value + BigInteger.One);

    public override JSValue Decrement() => new JSBigInt(value - BigInteger.One);

    public override JSValue BitwiseNot() => new JSBigInt(~value);

    public override JSValue Power(JSValue a)
    {
        var exponent = a.AsBigIntegerOnly();
        if (exponent.Sign < 0)
            throw JSEngine.NewRangeError("Exponent must be positive");

        return new JSBigInt(BigInteger.Pow(value, (int)exponent));
    }

    public override JSValue BitwiseAnd(JSValue value) => new JSBigInt(this.value & value.AsBigIntegerOnly());

    public override JSValue BitwiseOr(JSValue value) => new JSBigInt(this.value | value.AsBigIntegerOnly());

    public override JSValue BitwiseXor(JSValue value) => new JSBigInt(this.value ^ value.AsBigIntegerOnly());

    public override JSValue LeftShift(JSValue value) => new JSBigInt(this.value << (int)value.AsBigIntegerOnly());

    public override JSValue RightShift(JSValue value) => new JSBigInt(this.value >> (byte)value.AsBigIntegerOnly());

    public override JSValue UnsignedRightShift(JSValue value) => new JSBigInt(this.value >> (int)value.AsBigIntegerOnly());

    public override JSValue Multiply(JSValue value) => new JSBigInt(this.value * value.AsBigIntegerOnly());

    public override JSValue Divide(JSValue value)
    {
        var divisor = value.AsBigIntegerOnly();
        if (divisor.IsZero)
            throw JSEngine.NewRangeError("Division by zero");
        return new JSBigInt(this.value / divisor);
    }

    public override JSValue Subtract(JSValue value) => new JSBigInt(this.value - value.AsBigIntegerOnly());

    public override JSValue Modulo(JSValue value)
    {
        var divisor = value.AsBigIntegerOnly();
        if (divisor.IsZero)
            throw JSEngine.NewRangeError("Division by zero");
        return new JSBigInt(this.value % divisor);
    }

    public override JSValue AddValue(double value) => throw CannotMix();

    public override JSValue AddValue(string value) => new JSString(this.value + value);

    public override JSValue AddValue(JSValue value)
    {
        value = value is JSObject obj ? obj.ToDefaultPrimitive() : value;

        if (value is JSPrimitiveObject primitive)
            value = primitive.value;

        if (value is JSBigInt b)
            return new JSBigInt(this.value + b.value);

        if (value.IsBoolean || value.IsNumber)
            throw CannotMix();

        if (value is JSString @string)
            return new JSString(this.value.ToString() + @string.ToString());

        if (value is JSObject @object)
            return new JSString(this.value + @object.StringValue);

        return new JSBigInt(this.value + value.BigIntValue);
    }

    // BigInt.prototype methods accept either a BigInt primitive or a boxed
    // BigInt object (e.g. Object(1n)) as the receiver. Unwrap the wrapper and
    // reject any other receiver with a TypeError (thisBigIntValue per spec).
    private static JSBigInt ThisBigInt(JSValue value)
    {
        var unwrapped = value is JSPrimitiveObject primitiveObject ? primitiveObject.ValueOf() : value;
        return unwrapped as JSBigInt
            ?? throw JSEngine.NewTypeError("BigInt.prototype method called on incompatible receiver");
    }

    [JSPrototypeMethod]
    [JSExport("toString")]
    public static JSValue JSToString(in Arguments a)
    {
        var bigint = ThisBigInt(a.This);
        var radixArg = a.Get1();

        // BigInt.prototype.toString(radix): undefined radix defaults to 10, any other
        // value is coerced with ToIntegerOrInfinity and must land in [2, 36].
        var radix = 10;
        if (!radixArg.IsUndefined)
        {
            var radixMV = Math.Truncate(radixArg.DoubleValue);
            if (double.IsNaN(radixMV))
                radixMV = 0;
            if (radixMV < 2 || radixMV > 36)
                throw JSEngine.NewRangeError("toString() radix must be between 2 and 36");
            radix = (int)radixMV;
        }

        return new JSString(BigIntegerToRadixString(bigint.value, radix));
    }

    private static string BigIntegerToRadixString(BigInteger value, int radix)
    {
        if (radix == 10)
            return value.ToString();

        if (value.IsZero)
            return "0";

        const string digits = "0123456789abcdefghijklmnopqrstuvwxyz";
        var negative = value.Sign < 0;
        var magnitude = BigInteger.Abs(value);
        var builder = new StringBuilder();
        var bigRadix = new BigInteger(radix);

        while (!magnitude.IsZero)
        {
            magnitude = BigInteger.DivRem(magnitude, bigRadix, out var remainder);
            builder.Insert(0, digits[(int)remainder]);
        }

        if (negative)
            builder.Insert(0, '-');

        return builder.ToString();
    }

    [JSPrototypeMethod]
    [JSExport("toLocaleString")]
    public static JSValue ToLocaleString(in Arguments a) => new JSString(ThisBigInt(a.This).value.ToString(CultureInfo.CurrentCulture));

    [JSPrototypeMethod]
    [JSExport("valueOf")]
    public static JSValue ValueOf(in Arguments a) => ThisBigInt(a.This);

    // ToIndex(value): coerce to an integer index in [0, 2^53-1], else RangeError.
    // Performed before ToBigInt so that side effects occur in spec order
    // (BigInt.asIntN/asUintN step 1 runs before step 2).
    private static long ToBitsIndex(JSValue value)
    {
        if (value.IsUndefined)
            return 0;

        var number = value.DoubleValue;
        var integer = double.IsNaN(number) ? 0 : Math.Truncate(number);
        if (integer < 0 || integer > 9007199254740991.0)
            throw JSEngine.NewRangeError("Invalid range for bits");

        return (long)integer;
    }

    // ToBigInt(value): ECMAScript abstract operation. Coerces booleans, BigInts
    // and BigInt-parseable strings; Numbers/Symbols/undefined/null throw TypeError.
    /// <summary>
    /// Performs the ToBigInt abstract operation, returning a <see cref="JSBigInt"/>.
    /// Throws a TypeError for values that cannot be converted (undefined, null,
    /// Number, Symbol) and a SyntaxError for an unparseable string.
    /// </summary>
    internal static JSBigInt Coerce(JSValue value) => new JSBigInt(ToBigInt(value));

    private static BigInteger ToBigInt(JSValue value)
    {
        var primitive = value is JSObject @object ? @object.ToDefaultPrimitive() : value;

        switch (primitive)
        {
            case JSBigInt bigint:
                return bigint.value;

            case JSBoolean boolean:
                return boolean.BooleanValue ? BigInteger.One : BigInteger.Zero;

            case JSString str:
                // StringToBigInt: empty/whitespace-only strings are 0n; an
                // otherwise unparseable string is a SyntaxError, not a TypeError.
                var text = str.ToString();
                if (string.IsNullOrWhiteSpace(text))
                    return BigInteger.Zero;
                if (!TryParseBigIntString(text, out var parsed))
                    throw JSEngine.NewSyntaxError($"Cannot convert {text} to a BigInt");
                return parsed;

            default:
                throw JSEngine.NewTypeError($"Cannot convert {primitive.TypeOf()} to a BigInt");
        }
    }

    [JSExport("asIntN", Length = 2)]
    public static JSValue AsIntN(in Arguments a)
    {
        var bits = ToBitsIndex(a.GetAt(0));
        var n = ToBigInt(a.GetAt(1));

        var buffer = n.ToByteArray();

        if (buffer.Length * 8 < bits)
            return new JSBigInt(n);

        var reminderBits = bits % 8;
        var length = (int)(bits / 8);

        if (reminderBits > 0)
            length++;

        var copy = new byte[length];
        Buffer.BlockCopy(buffer, 0, copy, 0, length);

        if (reminderBits > 0)
        {
            // here we need to pad leftmost bits as 1s
            // as BigInteger uses bytes and only if the
            // eighth bit is 1, it will consider it as a
            // negative integer

            // so we need to create mask to first remove
            // bits as byte contains eight bits

            // then check the most significant digit
            // if it is negative, then we need to pad
            // 1s before it

            ref byte last = ref copy[copy.Length - 1];

            byte padMask = 0xFF;

            byte mask = 1;
            byte start = 1;
            
            reminderBits--;
            
            while (reminderBits > 0)
            {
                padMask &= (byte)~start;
                start <<= 1;
                start |= 1;
                mask <<= 1;
                reminderBits--;
            }
            
            last &= start;
            var lastValue = last;

            if ((mask & lastValue) > 0)
                last |= padMask;
        }

        var r = new BigInteger(copy);
        return new JSBigInt(r);
    }

    [JSExport("asUintN", Length = 2)]
    public static JSValue AsUintN(in Arguments a)
    {
        var bits = ToBitsIndex(a.GetAt(0));
        var original = ToBigInt(a.GetAt(1));

        var n = original;
        if (n.Sign == BigInteger.MinusOne.Sign)
            n = -n;

        var buffer = n.ToByteArray();
        if (buffer.Length * 8 < bits)
            return new JSBigInt(original);

        var reminderBits = bits % 8;

        var length = (int)(bits / 8);
        if (reminderBits > 0)
            length++;

        // extra pad will result in a UInt
        var copy = new byte[length + 1];
        Buffer.BlockCopy(buffer, 0, copy, 0, length);

        if (reminderBits > 0)
        {
            ref byte last = ref copy[length - 1];
            byte start = 1;
            reminderBits--;
            
            while (reminderBits > 0)
            {
                start <<= 1;
                start |= 1;
                reminderBits--;
            }

            last &= start;
        }

        var r = new BigInteger(copy);
        return new JSBigInt(r);
    }
}
