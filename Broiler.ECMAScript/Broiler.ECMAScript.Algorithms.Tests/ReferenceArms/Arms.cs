using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Broiler.ECMAScript.Numbers;
using J = Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.J;
using V = Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.V;

namespace Broiler.ECMAScript.Algorithms.Tests.ReferenceArms;

/// <summary>An input of an operation: a double, a string or a single code unit, and its integer parameter when it has one.</summary>
internal readonly record struct Input(double Number, string Text, int Parameter, bool HasParameter, bool Strict, long Bits);

/// <summary>One arm's implementation of one operation. An arm can have more than one: an engine with two copies of an operation has two variants, and both are compared.</summary>
internal sealed record Variant(string Arm, string Name, Func<Input, string> Run);

/// <summary>
/// The operations of bundle sjl-3-001's reference-arm rule, and for each the shared implementation
/// (arm S) and the arms' variants that implement it. Every result is rendered as text, so that the
/// comparison is exact: a string is itself, a double is <c>f64:</c> and its bit pattern with every
/// NaN as <c>NaN</c>, a BigInt is <c>bigint:</c> and its decimal text, and a refusal is
/// <c>error:</c> and its kind. An arm that throws gives <c>exception:</c> and the exception's type,
/// and arm J's step cap gives <c>unfinished:</c> and the estimate it had reached.
/// </summary>
/// <remarks>
/// <para>
/// <b>Where an arm's entry point holds a line of the engine's built-in, it says so.</b> Broiler.VM's
/// toExponential and toPrecision answer <c>ToJsString(value)</c> for a non-finite value in the
/// built-in method, before the copied digit generation is called; those lines are the built-in's,
/// verbatim, and are part of the operation. No coercion, argument validation or error is compared.
/// </para>
/// <para>
/// <b>What an arm does not implement is not called</b>, and <see cref="NotImplemented"/> says why, so
/// the bundle can name it.
/// </para>
/// </remarks>
internal static class Arms
{
    public const string Shared = "S";

    public static IReadOnlyList<Operation> Operations { get; } = Build();

    /// <summary>The operations an arm does not implement, with the reason the bundle gives.</summary>
    public static IReadOnlyList<(string Arm, string Operation, string Reason)> NotImplemented { get; } =
    [
        ("J", "CanonicalNumericString", "no member of arm J's list implements CanonicalNumericIndexString; at the base it is Storage/KeyStrings, which the rule does not copy"),
        ("V", "ArrayIndex", "no member of arm V's list implements the array-index test; at the base it is JsObject.IsArrayIndex, which the rule does not copy"),
        ("V", "CanonicalNumericString", "no member of arm V's list implements CanonicalNumericIndexString; at the base it is JsRealm.Object.ObjectIsCanonicalNumeric, which the rule does not copy"),
        ("J", "NumericLiteral refusals", "arm J's literal copies (NumberCoercion and the two BigInt readers) read tokens the scanner has already accepted, and the scanner is not in arm J's list; J is called for the literal texts S completes, and its refusals are not compared"),
        ("J", "TryCoerceToUInt32", "copied with NumberParser but called for no operation: it answers whether ToNumber of a string is an integer below 2^32 - 1, which no clause the rule names defines, and it has no caller at the base"),
    ];

    private static List<Operation> Build() =>
    [
        new("Number::toString", ["D1", "D2", "D3", "D4", "D5", "D6"], null,
            static i => NumberText.ToString(i.Number),
            [
                new("J", "JSNumber.ToECMAString", static i => J.JSNumber.ToECMAString(i.Number)),
                new("V", "JsNumberFormat.ToJsString", static i => V.JsNumberFormat.ToJsString(i.Number)),
                new("V", "JsParser.NumberText", static i => V.JsParser.NumberText(i.Number)),
            ]),
        new("Number::toString(radix)", ["D1", "D2", "D3", "D4", "D5", "D6:16384"], Range(2, 36),
            static i => NumberText.ToRadixString(i.Number, i.Parameter),
            [
                new("J", "JSNumberPrototype.ToString", static i => J.JSNumberPrototype.ToStringRadix(i.Number, i.Parameter)),
                new("V", "JsNumberFormat.ToRadixString", static i => V.JsNumberFormat.ToRadixString(i.Number, i.Parameter)),
            ]),
        new("toFixed", ["D1", "D2", "D3", "D4", "D5", "D6", "T1"], Range(0, 100),
            static i => NumberText.ToFixed(i.Number, i.Parameter),
            [
                new("J", "JSNumberPrototype.ToFixed", static i => J.JSNumberPrototype.ToFixed(i.Number, i.Parameter)),
                new("V", "JsRealm.NumberFixedText", static i => V.JsRealmNumber.NumberFixedText(i.Number, i.Parameter)),
            ]),
        // -1 stands for an absent fractionDigits.
        new("toExponential", ["D1", "D2", "D3", "D4", "D5", "D6", "T2"], [-1, .. Range(0, 100)],
            static i => i.Parameter < 0 ? NumberText.ToExponential(i.Number) : NumberText.ToExponential(i.Number, i.Parameter),
            [
                new("J", "JSNumberPrototype.ToExponential", static i => J.JSNumberPrototype.ToExponential(i.Number, i.Parameter < 0 ? null : i.Parameter)),
                // The built-in's lines: `if (!double.IsFinite(value)) return JsValue.String(JsNumberFormat.ToJsString(value));`
                // and then `NumberExponentialText(value, automatic ? -1 : (int)digits)`.
                new("V", "JsRealm.NumberExponentialText", static i => !double.IsFinite(i.Number)
                    ? V.JsNumberFormat.ToJsString(i.Number)
                    : V.JsRealmNumber.NumberExponentialText(i.Number, i.Parameter)),
            ]),
        new("toPrecision", ["D1", "D2", "D3", "D4", "D5", "D6", "T2"], Range(1, 100),
            static i => NumberText.ToPrecision(i.Number, i.Parameter),
            [
                new("J", "JSNumberPrototype.ToPrecision", static i => J.JSNumberPrototype.ToPrecision(i.Number, i.Parameter)),
                // The built-in's line: `if (!double.IsFinite(value)) return JsValue.String(JsNumberFormat.ToJsString(value));`
                new("V", "JsRealm.NumberPrecisionText", static i => !double.IsFinite(i.Number)
                    ? V.JsNumberFormat.ToJsString(i.Number)
                    : V.JsRealmNumber.NumberPrecisionText(i.Number, i.Parameter)),
            ]),
        new("StringToNumber", ["P1", "P2", "P3", "P4", "P5"], null,
            static i => Render(NumberParsing.StringToNumber(i.Text)),
            [
                new("J", "NumberParser.CoerceToNumber", static i => Render(J.NumberParser.CoerceToNumber(i.Text))),
                new("V", "JsNumberFormat.ToNumber", static i => Render(V.JsNumberFormat.ToNumber(i.Text))),
            ]),
        new("parseFloat", ["P1", "P2", "P3", "P4", "P5"], null,
            static i => Render(NumberParsing.ParseFloat(i.Text)),
            [
                new("J", "NumberParser.ParseFloat", static i => Render(J.NumberParser.ParseFloat(i.Text))),
                new("V", "JsNumberFormat.ParseFloat", static i => Render(V.JsNumberFormat.ParseFloat(i.Text))),
            ]),
        // 0 stands for an absent radix.
        new("parseInt", ["P1", "P3", "P4", "P5"], [0, .. Range(2, 36)],
            static i => Render(NumberParsing.ParseInt(i.Text, i.Parameter)),
            [
                new("J", "JSGlobal.ParseInt", static i => Render(J.Globals.ParseInt(i.Text, i.Parameter))),
                new("V", "JsNumberFormat.ParseInt", static i => Render(V.JsNumberFormat.ParseInt(i.Text, i.Parameter))),
            ]),
        new("NumericLiteral", ["L1"], null, SharedLiteral,
            [
                new("J", "NumberCoercion and the BigInt readers", JLiteral),
                new("V", "SliceTokenizer and JsParser.TryBigIntValue", VLiteral),
            ]),
        new("ArrayIndex", ["K1"], null,
            static i => NumericKeys.TryParseArrayIndex(i.Text, out var index) ? "index:" + index.ToString(CultureInfo.InvariantCulture) : "none",
            [
                new("J", "NumberParser.TryGetArrayIndex", static i => J.NumberParser.TryGetArrayIndex(i.Text, out var index) ? "index:" + index.ToString(CultureInfo.InvariantCulture) : "none"),
            ]),
        new("CanonicalNumericString", ["K1"], null,
            static i => NumericKeys.IsCanonicalNumericString(i.Text, out var value) ? Render(value) : "undefined",
            []),
        new("StrWhiteSpaceChar", ["P4:single"], null,
            static i => NumberParsing.IsStrWhiteSpaceChar(i.Text[0]) ? "true" : "false",
            [
                new("J", "NumberParser.IsWhiteSpaceOrLineTerminator", static i => J.NumberParser.IsWhiteSpaceOrLineTerminator(i.Text[0]) ? "true" : "false"),
                new("J", "JSStringPrototype.trimCharacters", static i => J.Globals.trimCharacters.Contains(i.Text[0]) ? "true" : "false"),
                new("V", "JsNumberFormat.IsWhiteSpace", static i => V.JsNumberFormat.IsWhiteSpace(i.Text[0]) ? "true" : "false"),
            ]),
    ];

    private static int[] Range(int from, int to) => Enumerable.Range(from, to - from + 1).ToArray();

    /// <summary>A double's rendering: its bit pattern, with every NaN as one.</summary>
    public static string Render(double value) =>
        double.IsNaN(value) ? "NaN" : "f64:" + BitConverter.DoubleToUInt64Bits(value).ToString("X16", CultureInfo.InvariantCulture);

    private static string RenderBigInt(BigInteger value) => "bigint:" + value.ToString(CultureInfo.InvariantCulture);

    private static string SharedLiteral(Input i)
    {
        var status = NumericLiterals.Evaluate(i.Text, i.Strict, new LengthLimit(i.Bits), out var literal, out _);
        return status switch
        {
            ResultStatus.Completed => literal.Kind == NumericLiteralKind.BigInt ? RenderBigInt(literal.BigInt) : Render(literal.Number),
            ResultStatus.LimitExceeded => "error:limit",
            _ => "error:SyntaxError",
        };
    }

    /// <summary>
    /// Arm J: the value its copies give a literal text S completes, and nothing for the rest (see
    /// <see cref="NotImplemented"/>). A Number literal is NumberCoercion's; a BigInt literal has two
    /// readers at the base, which are both compared under one name by joining their results when they
    /// differ from each other.
    /// </summary>
    private static string JLiteral(Input i)
    {
        if (!i.Text.EndsWith('n'))
            return Render(J.NumberCoercion.CoerceToNumber(i.Text));

        var compiler = RenderBigInt(J.CreateClass.BigIntLiteralToValue(i.Text));
        var builtIn = J.JSBigInt.TryParseBigIntLiteral(i.Text, out var value) ? RenderBigInt(value) : "error:SyntaxError";
        return compiler == builtIn ? compiler : "CreateClass=" + compiler + " JSBigInt=" + builtIn;
    }

    /// <summary>J is called only where S completes: its copies read accepted tokens.</summary>
    public static bool JLiteralApplies(string shared) => !shared.StartsWith("error:", StringComparison.Ordinal);

    /// <summary>
    /// Arm V: the tokenizer's scan and value of one literal, the parser's strict-mode refusal of the
    /// legacy forms the token records, and the compiler's BigInt value under the caller's ceiling.
    /// </summary>
    /// <remarks>
    /// A script of exactly one literal is refused when the tokenizer refuses it or stops before the
    /// text's end (the rest would be another token the grammar does not admit after a literal). The
    /// strict-mode refusal is the parser's (the token's LegacyOctal fact, which the tokenizer records
    /// and does not rule on), written here because the parser is not in the arm.
    /// </remarks>
    private static string VLiteral(Input i)
    {
        // The tokenizer's dispatch to ReadNumericLiteral, its condition verbatim: anything else is
        // not scanned as a numeric literal at all.
        if (i.Text.Length == 0 || !(char.IsAsciiDigit(i.Text[0]) || (i.Text[0] == '.' && i.Text.Length > 1 && char.IsAsciiDigit(i.Text[1]))))
            return "error:SyntaxError";

        var tokenizer = new V.SliceTokenizer(i.Text);
        var token = tokenizer.ReadNumericLiteral(1, 1, false);
        if (tokenizer.Refusal is not null || tokenizer.Index != i.Text.Length)
            return "error:SyntaxError";

        if (i.Strict && token.LegacyOctal)
            return "error:SyntaxError";

        if (token.RawText.EndsWith('n'))
            return V.JsParser.TryBigIntValue(token.RawText, i.Bits, out var value) ? RenderBigInt(value) : "error:limit";

        return Render(token.NumericValue);
    }
}

/// <summary>An operation: its classes, its parameter values, S, and the arms' variants.</summary>
/// <param name="Name">The operation's name in the output.</param>
/// <param name="Classes">The population classes it runs over; <c>D6:16384</c> is D6's first 2^14 lines, and <c>P4:single</c> P4's one-character lines.</param>
/// <param name="Parameters">The integer parameter's values, or null when it takes none.</param>
/// <param name="Shared">Arm S.</param>
/// <param name="Variants">The arms' variants.</param>
internal sealed record Operation(string Name, string[] Classes, int[]? Parameters, Func<Input, string> Shared, Variant[] Variants);
