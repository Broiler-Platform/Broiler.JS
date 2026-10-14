using Broiler.ECMAScript;
using Broiler.ECMAScript.Numbers;
using Broiler.JavaScript.ExpressionCompiler;
using System;
using System.Numerics;
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

    // A BigInt compared with a Number is JSBigInt.OrderAgainstNumber: the engine answers NaN and the
    // infinities, and the exact comparison is the shared library's BigIntMath.CompareToNumber (SJL-4.5).
}

// Every BigInt operation, conversion and text is the shared library's BigInt kernel (BigIntMath and
// BigIntText, milestone SJL-4.5); what stays here are the adapters, which keep the engine's coercions,
// its errors and its value wrapping. The kernel runs under decision SJL-D-15 (a): no width but the
// representation's, no meter, and an over-width status is a RangeError, never a .NET exception.
[JSBaseClass("Object")]
[JSFunctionGenerator("BigInt")]
public partial class JSBigInt : JSPrimitive
{
    public static JSException CannotMix() => JSEngine.NewTypeError("Cannot mix BigInt and other types, use explicit conversions");

    // Decision SJL-D-15 (a): Broiler.JS sets no BigInt width of its own.
    private static readonly BigIntPolicy Policy = BigIntPolicy.Unbounded;

    // The kernel's LimitExceeded: a result wider than the representation can hold.
    private static JSException TooWide() => JSEngine.NewRangeError("Maximum BigInt size exceeded");

    // A kernel result as a BigInt value, or the RangeError of an over-width status.
    private static JSBigInt Of(ResultStatus status, BigInteger result)
        => status == ResultStatus.Completed ? new JSBigInt(result) : throw TooWide();

    internal readonly BigInteger value;

    public override bool BooleanValue => value != 0;

    public override bool IsBigInt => true;

    public override double DoubleValue => throw CannotMix();

    public override long BigIntValue => (long)value;

    // A BigInt's Number (Number(value), 21.1.1.1 step 2.a) is the shared library's
    // BigIntMath.ToNumber, the nearest double with ties to even (SJL-4.5). The public member stays
    // as a forwarder (decision SJL-D-25 (b)).
    [Obsolete("Use Broiler.ECMAScript.Numbers.BigIntMath.ToNumber, which this forwards to. To be removed in the next major version.")]
    public static double ToNumber(BigInteger v) => BigIntMath.ToNumber(v, null);

    // BigInt(value) (21.2.1.1): ToPrimitive with the number hint; a Number is NumberToBigInt, whose
    // RangeError for a Number that is not integral is the engine's and whose integer is the shared
    // library's BigIntMath.FromIntegralNumber; anything else is ToBigInt.
    [JSExport(IsConstructor = true, Length = 1)]
    public static JSValue Constructor(in Arguments a)
    {
        var primitive = a[0] is JSObject @object ? @object.ToNumberPrimitive() : a[0];
        if (primitive is JSNumber number)
        {
            if (!double.IsFinite(number.value) || Math.Truncate(number.value) != number.value)
                throw JSEngine.NewRangeError("The number cannot be converted to a BigInt because it is not an integer");

            return new JSBigInt(BigIntMath.FromIntegralNumber(number.value));
        }

        return primitive as JSBigInt ?? new JSBigInt(ToBigInt(primitive));
    }

    public JSBigInt(BigInteger value) => this.value = value;
    // The value of a BigInt literal's source text, `n` included, after the "-" the compiler folds
    // into a negated literal (`-5n`). The value is the shared library's (SJL-3.5); Broiler.JS sets
    // no width on a BigInt literal beyond the source's own length.
    public JSBigInt(string stringValue)
    {
        var text = stringValue.AsSpan();
        var negative = !text.IsEmpty && text[0] == '-';
        if (negative)
            text = text[1..];

        if (NumericLiterals.Evaluate(text, strict: false, new LengthLimit(long.MaxValue), out var literal, out _) != ResultStatus.Completed
            || literal.Kind != NumericLiteralKind.BigInt)
            throw JSEngine.NewTypeError($"{stringValue} is not a valid big integer");
        value = negative ? -literal.BigInt : literal.BigInt;
    }

    // StringToBigInt (7.1.14) is the shared library's BigIntText.StringToBigInt, which trims the
    // specification's white space, reads "" as 0n and answers Malformed where the clause answers
    // undefined. Abstract equality reads undefined, and a value past the representation, as unequal
    // to every BigInt.
    private bool EqualsString(string text)
        => BigIntText.StringToBigInt(text, Policy, null, out var parsed, out _) == ResultStatus.Completed && value == parsed;

    // IsLessThan (7.2.13) of a BigInt and a String: undefined is NaNComparison, so every relational
    // operator answers false, and a value past the representation is ordered by its sign.
    private int CompareToString(string text) => BigIntText.StringToBigInt(text, Policy, null, out var parsed, out var tooWideSign) switch
    {
        ResultStatus.Completed => value.CompareTo(parsed),
        ResultStatus.LimitExceeded => -tooWideSign,
        _ => JSBigIntExtensions.NaNComparison,
    };

    // IsLooselyEqual (7.2.14) steps 12-13: NaN and the infinities equal no BigInt, and a finite Number
    // is compared exactly by the shared library, never through a conversion of either side.
    private bool EqualsNumber(double number)
        => double.IsFinite(number) && BigIntMath.CompareToNumber(value, number, null) == 0;

    // IsLessThan (7.2.13) steps 3-4 of a BigInt and a Number: NaN is NaNComparison, +Infinity is above
    // and -Infinity below every BigInt, and a finite Number is compared exactly by the shared library.
    internal static int OrderAgainstNumber(BigInteger value, double number)
    {
        if (double.IsNaN(number))
            return JSBigIntExtensions.NaNComparison;

        if (double.IsInfinity(number))
            return number > 0 ? -1 : 1;

        return BigIntMath.CompareToNumber(value, number, null);
    }

    public override bool Equals(JSValue value)
    {
        // Abstract equality with an Object operand first applies ToPrimitive (default
        // hint) and re-compares; a boxed primitive unwraps via ValueOf.
        if (value is JSPrimitiveObject primitiveObject)
            value = primitiveObject.ValueOf();
        else if (value is JSObject @object)
            value = @object.ToDefaultPrimitive();

        if (value is JSBigInt bigint)
            return this.value == bigint.value;

        // BigInt == String compares via StringToBigInt: an empty/whitespace string is 0n,
        // and a non-integer string (e.g. "0.") is not equal to any BigInt.
        if (value is JSString str)
            return EqualsString(str.ToString());

        // A Boolean operand is converted with ToNumber first (true -> 1, false -> 0)
        // and then compared by mathematical value, per abstract equality.
        if (!value.IsNumber && !value.IsBoolean)
            return false;

        return EqualsNumber(value.DoubleValue);
    }

    // BigInt::toString(x, 10), the shared library's BigIntText.ToString: culture-invariant, so a
    // negative BigInt is written with U+002D whatever the current culture's negative sign. It is
    // every radix-10 text of a BigInt: String(x), concatenation, template literals and keys.
    public override string ToString() => BigIntText.ToString(value, 10, null);

    public override string ToDetailString() => ToString() + "n";

    public override JSValue InvokeFunction(in Arguments a) => throw JSEngine.NewTypeError($"{this} is not a function");

    // A BigInt is not a constructor: `new` of one is the base class's TypeError. (It read its argument
    // as a 64-bit integer and answered a BigInt, before SJL-4.5.)

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
            case JSString str:
                // BigInt vs String relational comparison uses StringToBigInt for exact
                // comparison; a non-integer string is incomparable (yields undefined),
                // signalled with NaNComparison so every relational operator returns false.
                comparison = CompareToString(str.ToString());
                return true;
            case var _ when value.IsNumber || value.IsBoolean || value.IsNull:
                comparison = OrderAgainstNumber(this.value, value.DoubleValue);
                return true;
            default:
                comparison = default;
                return false;
        }
    }

    private static bool IsValidComparison(int comparison) => comparison != JSBigIntExtensions.NaNComparison;

    public override bool Less(JSValue value)
    {
        using var counted = Runtime.ArithmeticOperandDiagnostics.Enabled
            ? Runtime.ArithmeticOperandDiagnostics.Relate(false, value.IsNumber)
            : default;
        return TryCompare(value, out var comparison)
            ? IsValidComparison(comparison) && comparison < 0
            : base.Less(value);
    }

    public override bool LessOrEqual(JSValue value)
    {
        using var counted = Runtime.ArithmeticOperandDiagnostics.Enabled
            ? Runtime.ArithmeticOperandDiagnostics.Relate(false, value.IsNumber)
            : default;
        return TryCompare(value, out var comparison)
            ? IsValidComparison(comparison) && comparison <= 0
            : base.LessOrEqual(value);
    }

    public override bool Greater(JSValue value)
    {
        using var counted = Runtime.ArithmeticOperandDiagnostics.Enabled
            ? Runtime.ArithmeticOperandDiagnostics.Relate(false, value.IsNumber)
            : default;
        return TryCompare(value, out var comparison)
            ? IsValidComparison(comparison) && comparison > 0
            : base.Greater(value);
    }

    public override bool GreaterOrEqual(JSValue value)
    {
        using var counted = Runtime.ArithmeticOperandDiagnostics.Enabled
            ? Runtime.ArithmeticOperandDiagnostics.Relate(false, value.IsNumber)
            : default;
        return TryCompare(value, out var comparison)
            ? IsValidComparison(comparison) && comparison >= 0
            : base.GreaterOrEqual(value);
    }

    // BigInt == String literal uses StringToBigInt, not a textual comparison: an
    // empty/whitespace string is 0n (so `0n == ""` is true) and a non-integer string
    // (e.g. "0.") equals no BigInt.
    public override bool EqualsLiteral(string value) => EqualsString(value);

    // The compiled `==` against a Number literal is the same exact comparison as Equals: through no
    // conversion of the BigInt to a double, which made 2n ** 53n + 1n == 2 ** 53 + 1 true.
    public override bool EqualsLiteral(double value) => EqualsNumber(value);

    public override JSValue TypeOf() => JSConstants.BigInt;

    protected override JSValue GetPrototype() => Intrinsics.Prototype(Names.BigInt);

    // ToPropertyKey (7.1.19) of a BigInt is its ToString: the key of its canonical decimal text, an
    // array index only where that text is one, so 4294967295n, 2n ** 40n and -1n key exactly as the
    // Strings "4294967295", "1099511627776" and "-1" do.
    internal override PropertyKey ToKey(bool create = true)
    {
        var text = ToString();
        if (NumericKeys.TryParseArrayIndex(text, out var index))
            return index;

        return KeyStrings.GetOrCreate(text);
    }

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

    // The BigInt:: operations (6.1.6.2) are the shared library's BigIntMath; each adapter keeps the
    // engine's ToNumeric of the other operand (AsBigIntegerOnly, whose TypeError is mixing a BigInt
    // with another type) and the errors the specification raises before the operation: a zero divisor
    // and a negative exponent. Increment and decrement are BigInt::add and BigInt::subtract with 1n.
    public override JSValue Negate() => Of(BigIntMath.Negate(value, Policy, null, out var result), result);

    public override JSValue Increment() => Of(BigIntMath.Add(value, BigInteger.One, Policy, null, out var result), result);

    public override JSValue Decrement() => Of(BigIntMath.Subtract(value, BigInteger.One, Policy, null, out var result), result);

    public override JSValue BitwiseNot() => Of(BigIntMath.BitwiseNot(value, Policy, null, out var result), result);

    public override JSValue Power(JSValue a)
    {
        var exponent = a.AsBigIntegerOnly();
        if (exponent.Sign < 0)
            throw JSEngine.NewRangeError("Exponent must be positive");

        return Of(BigIntMath.Power(value, exponent, Policy, null, out var result), result);
    }

    public override JSValue BitwiseAnd(JSValue value) => Of(BigIntMath.BitwiseAnd(this.value, value.AsBigIntegerOnly(), Policy, null, out var result), result);

    public override JSValue BitwiseOr(JSValue value) => Of(BigIntMath.BitwiseOr(this.value, value.AsBigIntegerOnly(), Policy, null, out var result), result);

    public override JSValue BitwiseXor(JSValue value) => Of(BigIntMath.BitwiseXor(this.value, value.AsBigIntegerOnly(), Policy, null, out var result), result);

    // BigInt::leftShift and BigInt::signedRightShift take a count of any size: a negative count shifts
    // the other way, a right shift past the value's width is 0n or -1n, and a left shift past the
    // representation is the kernel's over-width status, a RangeError.
    public override JSValue LeftShift(JSValue value) => Of(BigIntMath.LeftShift(this.value, value.AsBigIntegerOnly(), Policy, null, out var result), result);

    public override JSValue RightShift(JSValue value) => Of(BigIntMath.SignedRightShift(this.value, value.AsBigIntegerOnly(), Policy, null, out var result), result);

    // BigInt::unsignedRightShift always throws — BigInts are arbitrary-precision and have no fixed
    // width, so ">>>" is unsupported for any BigInt operand (a TypeError, not a RangeError).
    // ApplyStringOrNumericBinaryOperator coerces both operands (ToNumeric) before the operation,
    // so a right operand's Symbol.toPrimitive runs — and may throw — ahead of this TypeError
    // (test262 unsigned-right-shift/bigint-toprimitive).
    public override JSValue UnsignedRightShift(JSValue value)
    {
        _ = ToNumericPrimitive(value);
        throw JSEngine.NewTypeError("BigInts have no unsigned right shift, use >> instead");
    }

    public override JSValue Multiply(JSValue value) => Of(BigIntMath.Multiply(this.value, value.AsBigIntegerOnly(), Policy, null, out var result), result);

    public override JSValue Divide(JSValue value)
    {
        var divisor = value.AsBigIntegerOnly();
        if (divisor.IsZero)
            throw JSEngine.NewRangeError("Division by zero");
        return Of(BigIntMath.Divide(this.value, divisor, Policy, null, out var result), result);
    }

    public override JSValue Subtract(JSValue value) => Of(BigIntMath.Subtract(this.value, value.AsBigIntegerOnly(), Policy, null, out var result), result);

    public override JSValue Modulo(JSValue value)
    {
        var divisor = value.AsBigIntegerOnly();
        if (divisor.IsZero)
            throw JSEngine.NewRangeError("Division by zero");
        return Of(BigIntMath.Remainder(this.value, divisor, Policy, null, out var result), result);
    }

    public override JSValue AddValue(double value) => throw CannotMix();

    // String concatenation reads the BigInt's text through ToString, the shared library's.
    public override JSValue AddValue(string value) => new JSString(ToString() + value);

    public override JSValue AddValue(JSValue value)
    {
        value = value is JSObject obj ? obj.ToDefaultPrimitive() : value;

        if (value is JSPrimitiveObject primitive)
            value = primitive.value;

        if (value is JSBigInt b)
            return Of(BigIntMath.Add(this.value, b.value, Policy, null, out var result), result);

        if (value.IsBoolean || value.IsNumber)
            throw CannotMix();

        if (value is JSString @string)
            return new JSString(ToString() + @string.ToString());

        if (value is JSObject @object)
            return new JSString(ToString() + @object.StringValue);

        // After ToPrimitive, the only operands that combine with a BigInt are another BigInt
        // (numeric add, handled above) or a String (concatenation, handled above). Everything
        // else — Number, Boolean, null, undefined, Symbol — is a numeric type mismatch:
        // ApplyStringOrNumericBinaryOperator throws a TypeError (e.g. `1n + null`).
        throw CannotMix();
    }

    // BigInt.prototype methods accept either a BigInt primitive or a boxed
    // BigInt object (e.g. Object(1n)) as the receiver. Unwrap the wrapper and
    // reject any other receiver with a TypeError (thisBigIntValue per spec).
    private static JSBigInt ThisBigInt(JSValue value)
    {
        // thisBigIntValue reads the wrapper's internal [[BigIntData]] slot DIRECTLY — it
        // must not run the (possibly user-overridden) valueOf/toString, which would recurse
        // when an override calls the original BigInt.prototype.valueOf
        // (test262 BigInt/wrapper-object-ordinary-toprimitive).
        var unwrapped = value is JSPrimitiveObject primitiveObject ? primitiveObject.value : value;
        return unwrapped as JSBigInt
            ?? throw JSEngine.NewTypeError("BigInt.prototype method called on incompatible receiver");
    }

    [JSPrototypeMethod]
    [JSExport("toString", Length = 0)]
    public static JSValue JSToString(in Arguments a)
    {
        var bigint = ThisBigInt(a.This);
        var radixArg = a.Get1();

        // BigInt.prototype.toString(radix): undefined radix defaults to 10, any other
        // value is coerced with ToIntegerOrInfinity and must land in [2, 36]. The digits are
        // BigInt::toString, the shared library's BigIntText.ToString.
        var radix = 10;
        if (!radixArg.IsUndefined)
        {
            var radixMV = IntegerConversions.ToIntegerOrInfinity(radixArg.DoubleValue);
            if (radixMV < 2 || radixMV > 36)
                throw JSEngine.NewRangeError("toString() radix must be between 2 and 36");
            radix = (int)radixMV;
        }

        return new JSString(BigIntText.ToString(bigint.value, radix, null));
    }

    [JSPrototypeMethod]
    [JSExport("toLocaleString", Length = 0)]
    public static JSValue ToLocaleString(in Arguments a)
    {
        var self = ThisBigInt(a.This);
        // §BigInt.prototype.toLocaleString(locales, options) ≡
        // Intl.NumberFormat(locales, options).format(this).
        var nf = new Intl.JSIntlNumberFormat(new Arguments(JSUndefined.Value, a.GetAt(0), a.GetAt(1)));
        return new JSString(nf.Format(new Arguments(JSUndefined.Value, self)).StringValue);
    }

    [JSPrototypeMethod]
    [JSExport("valueOf")]
    public static JSValue ValueOf(in Arguments a) => ThisBigInt(a.This);

    // ToIndex (7.1.22) of BigInt.asIntN's and asUintN's bits, before ToBigInt of the value so that
    // side effects occur in the specification's order (21.2.2.1 steps 1-2): undefined is 0, the
    // argument's ToNumber is the engine's, and the range rule is the shared library's.
    private static ulong ToIndex(JSValue value)
    {
        if (value.IsUndefined)
            return 0;

        if (!IntegerConversions.TryToIndex(value.DoubleValue, out var index))
            throw JSEngine.NewRangeError("Invalid range for bits");

        return (ulong)index;
    }

    /// <summary>
    /// Performs the ToBigInt abstract operation, returning a <see cref="JSBigInt"/>: <see cref="ToBigInt"/>
    /// with the value wrapped, for the callers that store or return it.
    /// </summary>
    internal static JSBigInt Coerce(JSValue value) => value as JSBigInt ?? new JSBigInt(ToBigInt(value));

    /// <summary>
    /// The abstract operation ToBigInt (ECMA-262 7.1.13), the one every caller converts with:
    /// ToPrimitive with the number hint, then a BigInt is itself, a Boolean is 1n or 0n, and a String
    /// is StringToBigInt (the shared library's), a SyntaxError where that answers undefined and the
    /// RangeError of decision SJL-D-15 (a) past the representation. Undefined, null, a Number and a
    /// Symbol are a TypeError.
    /// </summary>
    internal static BigInteger ToBigInt(JSValue value)
    {
        var primitive = value is JSObject @object ? @object.ToNumberPrimitive() : value;

        switch (primitive)
        {
            case JSBigInt bigint:
                return bigint.value;

            case JSBoolean boolean:
                return boolean.BooleanValue ? BigInteger.One : BigInteger.Zero;

            case JSString str:
                var text = str.ToString();
                return BigIntText.StringToBigInt(text, Policy, null, out var parsed, out _) switch
                {
                    ResultStatus.Completed => parsed,
                    ResultStatus.LimitExceeded => throw TooWide(),
                    _ => throw JSEngine.NewSyntaxError($"Cannot convert {text} to a BigInt"),
                };

            default:
                throw JSEngine.NewTypeError($"Cannot convert {primitive.TypeOf()} to a BigInt");
        }
    }

    // BigInt.asIntN and BigInt.asUintN (21.2.2.1, 21.2.2.2): ToIndex, then ToBigInt, then the shared
    // library's BigIntMath.AsIntN and AsUintN; a negative value modulo a width past the representation
    // is the kernel's over-width status, a RangeError.
    [JSExport("asIntN", Length = 2)]
    public static JSValue AsIntN(in Arguments a)
    {
        var bits = ToIndex(a.GetAt(0));
        var n = ToBigInt(a.GetAt(1));
        return Of(BigIntMath.AsIntN(bits, n, Policy, null, out var result), result);
    }

    [JSExport("asUintN", Length = 2)]
    public static JSValue AsUintN(in Arguments a)
    {
        var bits = ToIndex(a.GetAt(0));
        var n = ToBigInt(a.GetAt(1));
        return Of(BigIntMath.AsUintN(bits, n, Policy, null, out var result), result);
    }
}
