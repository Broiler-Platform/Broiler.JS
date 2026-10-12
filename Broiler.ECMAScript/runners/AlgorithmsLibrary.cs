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
            _ => EmptyLibrary.NotImplemented,
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

    private static string Text(string chars) => "value: " + TypedValue.RenderString(chars);

    private static string Number(double value) => "value: " + TypedValue.RenderNumber(value);
}
