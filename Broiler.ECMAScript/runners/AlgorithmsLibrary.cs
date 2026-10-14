using System;
using System.Collections.Generic;
using System.Numerics;
using Broiler.ECMAScript.Conformance;
using Broiler.ECMAScript.Numbers;

namespace Broiler.ECMAScript.Runners;

/// <summary>
/// <c>Broiler.ECMAScript.Algorithms</c> as the library-level runner calls it: each operation the
/// corpus names, with the argument conversions an engine's adapter performs before the library is
/// reached.
/// </summary>
/// <remarks>
/// <para>
/// A vector pins a built-in such as <c>Number.prototype.toString</c>, whose argument may be any value;
/// the library takes the converted, validated argument. The conversions here -
/// <c>ToIntegerOrInfinity</c>, <c>ToInt32</c>, <c>ToString</c> of a primitive, and the
/// <c>RangeError</c> checks in the specification's order - are what each engine's adapter does,
/// written once more for the runner over the corpus's typed values. They contain no digit
/// generation, rounding, grammar or white-space table; every such step is the library's.
/// </para>
/// <para>
/// An operation the library does not have answers "not implemented", which the runner reports as
/// such rather than as a difference. <b>This file is linked into the shared test project and the
/// Native AOT probe</b>, beside <c>CorpusDirectory.cs</c>: the Conformance library references no
/// other shared library (architecture section 1), so the adapter to one lives with the runners.
/// </para>
/// </remarks>
internal sealed class AlgorithmsLibrary : IVectorLibrary
{
    /// <inheritdoc/>
    public string Name => "Broiler.ECMAScript.Algorithms";

    /// <inheritdoc/>
    public string Call(string operation, IReadOnlyList<TypedValue> inputs)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(inputs);
        return operation switch
        {
            "Number::toString" => Text(NumberText.ToString(inputs[0].Number)),
            "Number.prototype.toString" => RadixToString(inputs[0].Number, Argument(inputs, 1)),
            "Number.prototype.toFixed" => ToFixed(inputs[0].Number, Argument(inputs, 1)),
            "Number.prototype.toExponential" => ToExponential(inputs[0].Number, Argument(inputs, 1)),
            "Number.prototype.toPrecision" => ToPrecision(inputs[0].Number, Argument(inputs, 1)),
            "StringToNumber" => Number(NumberParsing.StringToNumber(ToStringOf(inputs[0]))),
            "parseFloat" => Number(NumberParsing.ParseFloat(ToStringOf(inputs[0]))),
            "parseInt" => Number(NumberParsing.ParseInt(ToStringOf(inputs[0]), ToInt32(Argument(inputs, 1)))),
            "NumericLiteral" => Literal(inputs[0].Chars, inputs[1].Boolean),
            "array index" => Number(NumericKeys.TryParseArrayIndex(inputs[0].Chars, out var index) ? index + 1.0 : 0),
            "CanonicalNumericIndexString" => Number(CanonicalScript(inputs[0].Chars)),
            _ => Numerics(operation, inputs),
        };
    }

    // ---------------------------------------------------------------------------------------------
    // The built-ins, argument conversion in the specification's order around one library call.

    private static string RadixToString(double x, TypedValue radix)
    {
        // Number.prototype.toString steps 2 to 6.
        var r = radix.Kind == TypedValueKind.Undefined ? 10 : ToIntegerOrInfinity(radix);
        if (r is < 2 or > 36)
            return "error: RangeError";

        return Text(NumberText.ToRadixString(x, (int)r));
    }

    private static string ToFixed(double x, TypedValue fractionDigits)
    {
        // Number.prototype.toFixed steps 2 to 5: the range is checked before x is looked at.
        var f = ToIntegerOrInfinity(fractionDigits);
        if (f is < 0 or > 100)
            return "error: RangeError";

        return Text(NumberText.ToFixed(x, (int)f));
    }

    private static string ToExponential(double x, TypedValue fractionDigits)
    {
        // Number.prototype.toExponential steps 2 to 5: a non-finite x answers before the range check.
        var f = ToIntegerOrInfinity(fractionDigits);
        if (!double.IsFinite(x))
            return Text(NumberText.ToString(x));

        if (f is < 0 or > 100)
            return "error: RangeError";

        return Text(fractionDigits.Kind == TypedValueKind.Undefined ? NumberText.ToExponential(x) : NumberText.ToExponential(x, (int)f));
    }

    private static string ToPrecision(double x, TypedValue precision)
    {
        // Number.prototype.toPrecision steps 2 to 5.
        if (precision.Kind == TypedValueKind.Undefined)
            return Text(NumberText.ToString(x));

        var p = ToIntegerOrInfinity(precision);
        if (!double.IsFinite(x))
            return Text(NumberText.ToString(x));

        if (p is < 1 or > 100)
            return "error: RangeError";

        return Text(NumberText.ToPrecision(x, (int)p));
    }

    private static string Literal(string text, bool strict)
    {
        var status = NumericLiterals.Evaluate(text, strict, new LengthLimit(long.MaxValue), out var literal, out _);
        if (status != ResultStatus.Completed)
            return "error: SyntaxError";

        return literal.Kind == NumericLiteralKind.BigInt
            ? "value: " + TypedValue.RenderBigInt(literal.BigInt)
            : Number(literal.Number);
    }

    /// <summary>The script a canonical-key vector runs: a typed array of length 2 written and read through the key.</summary>
    private static double CanonicalScript(string key)
    {
        if (!NumericKeys.IsCanonicalNumericString(key, out var n))
            return 13;

        var validIndex = !double.IsNaN(n) && double.IsFinite(n) && !(n == 0 && double.IsNegative(n)) && n is 0 or 1;
        return validIndex ? 12 : 2;
    }

    // ---------------------------------------------------------------------------------------------
    // The abstract operations an adapter runs, over the corpus's typed primitives.

    private static TypedValue Argument(IReadOnlyList<TypedValue> inputs, int at) =>
        at < inputs.Count ? inputs[at] : new TypedValue(TypedValueKind.Undefined, "undefined");

    /// <summary>ToNumber of a primitive.</summary>
    private static double ToNumber(TypedValue value) => value.Kind switch
    {
        TypedValueKind.Number => value.Number,
        TypedValueKind.Integer => (double)value.Integer,
        TypedValueKind.Undefined => double.NaN,
        TypedValueKind.Null => 0,
        TypedValueKind.Boolean => value.Boolean ? 1 : 0,
        TypedValueKind.String => NumberParsing.StringToNumber(value.Chars),
        _ => throw new NotSupportedException("No number vector converts a " + value.Kind + "."),
    };

    /// <summary>ToIntegerOrInfinity (ECMA-262 7.1.5).</summary>
    private static double ToIntegerOrInfinity(TypedValue value)
    {
        var number = ToNumber(value);
        if (double.IsNaN(number) || number == 0)
            return 0;

        return double.IsInfinity(number) ? number : Math.Truncate(number);
    }

    /// <summary>ToInt32 (ECMA-262 7.1.6).</summary>
    private static int ToInt32(TypedValue value)
    {
        if (value.Kind == TypedValueKind.Integer)
            return (int)(uint)(value.Integer & uint.MaxValue);

        var number = ToNumber(value);
        if (!double.IsFinite(number) || number == 0)
            return 0;

        var integer = new BigInteger(Math.Truncate(number));
        return (int)(uint)(integer & uint.MaxValue);
    }

    /// <summary>ToString of a primitive.</summary>
    private static string ToStringOf(TypedValue value) => value.Kind switch
    {
        TypedValueKind.String => value.Chars,
        TypedValueKind.Undefined => "undefined",
        TypedValueKind.Null => "null",
        TypedValueKind.Boolean => value.Boolean ? "true" : "false",
        TypedValueKind.Number => NumberText.ToString(value.Number),
        TypedValueKind.Integer => NumberText.ToString((double)value.Integer),
        _ => throw new NotSupportedException("No number vector converts a " + value.Kind + "."),
    };

    // ---------------------------------------------------------------------------------------------
    // The numerics area (corpus revision 6): each operation as the engine's adapter calls it, after the
    // conversions and checks the engine runs, and the script's own reading of the result where a
    // vector's script shows it through a built-in. A BigInt operation runs under the unbounded policy,
    // whose results are the specification's.

    /// <summary>Whether an operation of the area is one the library does not share, whose vectors are engine-level only.</summary>
    private static bool NotShared(string operation) => operation is "Math.fround" or "Math.clz32" or "Math.imul" or "ToPropertyKey";

    private static string Numerics(string operation, IReadOnlyList<TypedValue> inputs)
    {
        if (NotShared(operation))
            return EmptyLibrary.NotImplemented;

        switch (operation)
        {
            case "ToInt32": return Number(IntegerConversions.ToInt32(inputs[0].Number));
            case "ToUint32": return Number(IntegerConversions.ToUint32(inputs[0].Number));
            case "ToInt16": return Number(IntegerConversions.ToInt16(inputs[0].Number));
            case "ToUint16": return Number(IntegerConversions.ToUint16(inputs[0].Number));
            case "ToInt8": return Number(IntegerConversions.ToInt8(inputs[0].Number));
            case "ToUint8": return Number(IntegerConversions.ToUint8(inputs[0].Number));
            case "ToUint8Clamp": return Number(IntegerConversions.ToUint8Clamp(inputs[0].Number));
            case "ToIntegerOrInfinity": return Number(LastIndexFrom(IntegerConversions.ToIntegerOrInfinity(inputs[0].Number)));
            case "ToLength": return Number(IntegerConversions.ToLength(inputs[0].Number) - 1.0);
            case "ToIndex": return IntegerConversions.TryToIndex(inputs[0].Number, out _) ? "value: " + TypedValue.RenderBigInt(0) : "error: RangeError";
            case "ToBigInt64": return BigIntValue(IntegerConversions.ToBigInt64(inputs[0].Integer));
            case "ToBigUint64": return BigIntValue(IntegerConversions.ToBigUint64(inputs[0].Integer));
            case "Math.f16round": return Number(Float16.Round(inputs[0].Number));
            case "Float16 encode": return Number(Float16.Encode(DoubleOf(inputs[0]), Float16NanPolicy.Canonical));
            case "Float16 encode is a NaN":
                var encoded = Float16.Encode(DoubleOf(inputs[0]), Float16NanPolicy.Canonical);
                return "value: " + TypedValue.RenderBoolean((encoded & 0x7C00) == 0x7C00 && (encoded & 0x3FF) != 0);
            case "Float16 decode": return Number(Float16.Decode((ushort)inputs[0].Integer));
            case "Math.round": return Number(MathKernels.Round(inputs[0].Number));
            case "Math.pow":
            case "Number::exponentiate": return Number(MathKernels.Power(inputs[0].Number, inputs[1].Number));
            case "Math.hypot": return Number(MathKernels.Hypot(Numbers(inputs)));
            case "Math.cbrt": return Number(MathKernels.Cbrt(inputs[0].Number));
            case "Math.expm1": return Number(MathKernels.Expm1(inputs[0].Number));
            case "Math.log1p": return Number(MathKernels.Log1p(inputs[0].Number));
            case "Math.sumPrecise": return SumPrecise(inputs);
            case "Math.sumPrecise argument": return SumPreciseArgument(inputs[0]);
            case "Temporal.Duration.prototype.round":
                return Number(ZeroIsPositive((double)ExactRounding.RoundNumberToIncrement(inputs[0].Integer, 1, inputs[1].Integer, Mode(inputs[2]))));
            case "Temporal.Instant.prototype.round":
                return BigIntValue(ExactRounding.RoundNumberToIncrementAsIfPositive(
                    inputs[0].Integer, 1, inputs[2].Integer * UnitLength(inputs[1].Chars), Mode(inputs[3])));
            case "Temporal.Instant.prototype.since":
                var difference = ExactRounding.RoundNumberToIncrement(
                    inputs[1].Integer - inputs[0].Integer, 1, inputs[2].Integer, ExactRounding.Negate(Mode(inputs[3])));
                return Number(ZeroIsPositive(-(double)difference));
            case "Temporal.Duration.prototype.total": return Number(ExactRounding.ToDouble(inputs[0].Integer, UnitLength(inputs[1].Chars)));
        }

        // The width-policy vectors pin each engine's policy, and give their operands as descriptions:
        // they are engine-level, and the library's policy is the reference-arm comparison's to judge.
        if (operation.EndsWith(" (width policy)", StringComparison.Ordinal))
            return EmptyLibrary.NotImplemented;

        var policy = BigIntPolicy.Unbounded;
        BigInteger result;
        switch (operation)
        {
            case "BigInt::add": return BigIntOutcome(BigIntMath.Add(inputs[0].Integer, inputs[1].Integer, policy, null, out result), result);
            case "BigInt::subtract": return BigIntOutcome(BigIntMath.Subtract(inputs[0].Integer, inputs[1].Integer, policy, null, out result), result);
            case "BigInt::multiply": return BigIntOutcome(BigIntMath.Multiply(inputs[0].Integer, inputs[1].Integer, policy, null, out result), result);
            case "BigInt::divide":
                return inputs[1].Integer.IsZero ? "error: RangeError" : BigIntOutcome(BigIntMath.Divide(inputs[0].Integer, inputs[1].Integer, policy, null, out result), result);
            case "BigInt::remainder":
                return inputs[1].Integer.IsZero ? "error: RangeError" : BigIntOutcome(BigIntMath.Remainder(inputs[0].Integer, inputs[1].Integer, policy, null, out result), result);
            case "BigInt::exponentiate":
                return inputs[1].Integer.Sign < 0 ? "error: RangeError" : BigIntOutcome(BigIntMath.Power(inputs[0].Integer, inputs[1].Integer, policy, null, out result), result);
            case "BigInt::bitwiseAND": return BigIntOutcome(BigIntMath.BitwiseAnd(inputs[0].Integer, inputs[1].Integer, policy, null, out result), result);
            case "BigInt::bitwiseOR": return BigIntOutcome(BigIntMath.BitwiseOr(inputs[0].Integer, inputs[1].Integer, policy, null, out result), result);
            case "BigInt::bitwiseXOR": return BigIntOutcome(BigIntMath.BitwiseXor(inputs[0].Integer, inputs[1].Integer, policy, null, out result), result);
            case "BigInt::unaryMinus": return BigIntOutcome(BigIntMath.Negate(inputs[0].Integer, policy, null, out result), result);
            case "BigInt::bitwiseNOT": return BigIntOutcome(BigIntMath.BitwiseNot(inputs[0].Integer, policy, null, out result), result);
            case "BigInt::leftShift": return BigIntOutcome(BigIntMath.LeftShift(inputs[0].Integer, inputs[1].Integer, policy, null, out result), result);
            case "BigInt::signedRightShift": return BigIntOutcome(BigIntMath.SignedRightShift(inputs[0].Integer, inputs[1].Integer, policy, null, out result), result);
            case "BigInt::unsignedRightShift": return "error: TypeError";
            case "BigInt.asIntN":
            case "BigInt.asUintN":
            case "BigInt.asUintN, top 64 bits":
                if (!IntegerConversions.TryToIndex(BitsArgument(inputs[0]), out var bits))
                    return "error: RangeError";

                var status = operation == "BigInt.asIntN"
                    ? BigIntMath.AsIntN((ulong)bits, inputs[1].Integer, policy, null, out result)
                    : BigIntMath.AsUintN((ulong)bits, inputs[1].Integer, policy, null, out result);
                if (operation == "BigInt.asUintN, top 64 bits" && status == ResultStatus.Completed)
                    result >>= (int)(bits - 64);

                return BigIntOutcome(status, result);
            case "BigInt::toString": return Text(BigIntText.ToString(inputs[0].Integer, (int)inputs[1].Integer, null));
            case "BigInt::toString, length and ends":
                var text = BigIntText.ToString(inputs[0].Integer, (int)inputs[1].Integer, null);
                return Text(text.Length.ToString(System.Globalization.NumberFormatInfo.InvariantInfo) + ":" + text[..40] + ":" + text[^40..]);
            case "StringToBigInt":
                return BigIntText.StringToBigInt(inputs[0].Chars, policy, null, out result, out _) switch
                {
                    ResultStatus.Completed => BigIntValue(result),
                    ResultStatus.Malformed => "error: SyntaxError",
                    _ => "error: RangeError",
                };
            case "Number(BigInt)": return Number(BigIntMath.ToNumber(inputs[0].Integer, null));
            case "NumberToBigInt":
                var number = inputs[0].Number;
                return double.IsFinite(number) && Math.Truncate(number) == number ? BigIntValue(BigIntMath.FromIntegralNumber(number)) : "error: RangeError";
            case "IsLessThan": return "value: " + TypedValue.RenderBoolean(LessThan(inputs[0], inputs[1]));
            case "IsLooselyEqual": return "value: " + TypedValue.RenderBoolean(LooselyEqual(inputs[0], inputs[1]));
        }

        return EmptyLibrary.NotImplemented;
    }

    /// <summary>The index lastIndexOf starts from on an array-like of length 2^53 - 1 with every index, given its fromIndex's ToIntegerOrInfinity.</summary>
    private static double LastIndexFrom(double integer)
    {
        const double Length = 9007199254740991;
        if (double.IsNegativeInfinity(integer))
            return -1;
        if (integer >= 0)
            return Math.Min(integer, Length - 1);

        return Length + integer < 0 ? -1 : Length + integer;
    }

    /// <summary>A Number input, or the double NaN of the bits a <c>NaN XXXXXXXXXXXXXXXX</c> description names.</summary>
    private static double DoubleOf(TypedValue value) =>
        value.Kind == TypedValueKind.String && value.Chars.StartsWith("NaN ", StringComparison.Ordinal)
            ? BitConverter.UInt64BitsToDouble(ulong.Parse(value.Chars.AsSpan(4), System.Globalization.NumberStyles.AllowHexSpecifier, System.Globalization.NumberFormatInfo.InvariantInfo))
            : value.Number;

    /// <summary>The Number inputs of a variadic call; <c>undefined</c> alone is no argument at all.</summary>
    private static double[] Numbers(IReadOnlyList<TypedValue> inputs)
    {
        if (inputs.Count == 1 && inputs[0].Kind == TypedValueKind.Undefined)
            return [];

        var numbers = new double[inputs.Count];
        for (var at = 0; at < inputs.Count; at++)
            numbers[at] = inputs[at].Number;

        return numbers;
    }

    /// <summary>Math.sumPrecise over an array literal: the engine refuses an element that is not a Number, and the accumulator sums.</summary>
    private static string SumPrecise(IReadOnlyList<TypedValue> inputs)
    {
        if (inputs.Count == 1 && inputs[0].Kind == TypedValueKind.Undefined)
            return Number(new PreciseSum().Result());

        var sum = new PreciseSum();
        foreach (var input in inputs)
        {
            if (input.Kind != TypedValueKind.Number)
                return "error: TypeError";

            sum.Add(input.Number);
        }

        return Number(sum.Result());
    }

    /// <summary>Math.sumPrecise's items argument as the engine handles it: a String iterates its code units, anything else here is refused.</summary>
    private static string SumPreciseArgument(TypedValue items) =>
        items.Kind == TypedValueKind.String && items.Chars.Length == 0 ? Number(new PreciseSum().Result()) : "error: TypeError";

    /// <summary>A rounding mode's option name.</summary>
    private static RoundingMode Mode(TypedValue name) =>
        ExactRounding.TryParseRoundingMode(name.Chars, out var mode) ? mode : throw new FormatException("no rounding mode is named " + name.Chars);

    /// <summary>A Temporal unit's length in nanoseconds.</summary>
    private static BigInteger UnitLength(string unit) => unit switch
    {
        "nanosecond" => 1,
        "microsecond" => 1_000,
        "millisecond" => 1_000_000,
        "second" => 1_000_000_000,
        "minute" => 60_000_000_000,
        "hour" => 3_600_000_000_000,
        _ => throw new FormatException("no unit is named " + unit),
    };

    /// <summary>A Duration's field is a mathematical value, so its zero is +0.</summary>
    private static double ZeroIsPositive(double value) => value == 0 ? 0 : value;

    /// <summary>asIntN's and asUintN's width argument, before ToIndex.</summary>
    private static double BitsArgument(TypedValue value) => value.Kind == TypedValueKind.Integer ? (double)value.Integer : value.Number;

    /// <summary>A BigInt result, or the RangeError an engine raises for one past its policy.</summary>
    private static string BigIntOutcome(ResultStatus status, BigInteger value) =>
        status == ResultStatus.Completed ? BigIntValue(value) : "error: RangeError";

    private static string BigIntValue(BigInteger value) => "value: " + TypedValue.RenderBigInt(value);

    /// <summary>IsLessThan of a BigInt and a Number or a String, either way round.</summary>
    private static bool LessThan(TypedValue left, TypedValue right)
    {
        if (left.Kind == TypedValueKind.BigInt)
            return Order(left.Integer, right) is < 0;

        var reversed = Order(right.Integer, left);
        return reversed is > 0;
    }

    /// <summary>IsLooselyEqual of a BigInt and a Number or a String.</summary>
    private static bool LooselyEqual(TypedValue left, TypedValue right) =>
        left.Kind == TypedValueKind.BigInt ? Order(left.Integer, right) == 0 : Order(right.Integer, left) == 0;

    /// <summary>The order of a BigInt and a Number or a String, or null where the comparison is undefined.</summary>
    private static int? Order(BigInteger value, TypedValue other)
    {
        if (other.Kind == TypedValueKind.String)
        {
            return BigIntText.StringToBigInt(other.Chars, BigIntPolicy.Unbounded, null, out var parsed, out _) == ResultStatus.Completed
                ? value.CompareTo(parsed)
                : null;
        }

        var number = other.Number;
        if (double.IsNaN(number))
            return null;
        if (double.IsInfinity(number))
            return number > 0 ? -1 : 1;

        return BigIntMath.CompareToNumber(value, number, null);
    }

    private static string Text(string chars) => "value: " + TypedValue.RenderString(chars);

    private static string Number(double value) => "value: " + TypedValue.RenderNumber(value);
}
