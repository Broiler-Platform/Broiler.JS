// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using Broiler.ECMAScript.Conformance;
using Broiler.ECMAScript.Intl;
using Broiler.ECMAScript.Numbers;

namespace Broiler.ECMAScript.Runners;

/// <summary>
/// The Intl area's sub-area (c), number formatting, then list, relative-time and display-name formatting (corpus revision 27,
/// <c>conformance/vectors/intl/numbers-and-text.vectors</c>, and the <c>format</c> and <c>formatToParts</c> vectors of
/// <c>values-and-plurals.vectors</c>; stage SJL-9.3c): each operation as an engine's adapter calls <c>Broiler.ECMAScript.Intl</c>,
/// over the data home's generated tables and Broiler.VM's providers, as sub-areas (a) and (b) are
/// (<c>AlgorithmsLibrary.Intl.cs</c>, <c>AlgorithmsLibrary.IntlValues.cs</c>).
/// </summary>
/// <remarks>
/// <para>
/// <b>The vectors are read whole, with their scripts</b> (<see cref="IVectorRecordLibrary"/>), as sub-area (b)'s are: the
/// options, the arguments and what the script does with the answer are in <c>script</c> alone. <see cref="NumbersScript"/> reads
/// the forms the file's generator writes - a constructor of a String locale and an options bag of literals, one call of a
/// method, the arguments as literals, <c>$f64</c>, BigInt literals and their powers, arrays of Strings, <c>Array(n).fill(s)</c>,
/// <c>Object(v)</c> and objects whose <c>valueOf</c> or <c>toString</c> returns a literal - and what the scripts do around the call:
/// <c>JSON.stringify</c>, <c>Object.keys</c>, <c>join</c>, <c>length</c>, a String's <c>repeat</c> and <c>+</c>, and
/// <c>toLocaleString</c> through a NumberFormat. What an engine's adapter does - the options read in the constructor's order
/// and checked as GetOption and GetNumberOption check them, ToIntlMathematicalValue's ToPrimitive, ToNumber and ToString of the
/// arguments, the parts arrays - is written here once more; every step of the sub-area is the library's.
/// </para>
/// <para>
/// <b>Which vectors are answered</b> (<see cref="IsLibraryIntlNumbers"/>): of <c>numbers-and-text.vectors</c>, the operations
/// <see cref="IntlNumbersOperations"/> names; of <c>values-and-plurals.vectors</c>, <c>Intl.NumberFormat.prototype.format</c> and
/// <c>formatToParts</c>, the 93 vectors stage SJL-9.3b left to this stage. Not answered, as engine-level
/// (<see cref="IsEngineLevelNumbersScript"/>): a script that is a function expression - the bound <c>format</c>'s identity
/// (row N11), the legacy constructor (row N12), the reads after construction (row N10) and an object's <c>valueOf</c> count, each
/// an engine's object, reads or coercions - a constructor called without <c>new</c>, and a list StringListFromIterable refuses,
/// which is the engine's iteration.
/// </para>
/// <para>
/// <b>The data is read through a <see cref="CompositeLocaleData"/></b> over the tables, so that a data locale such as
/// <c>en-US</c> reads its parent's lines, as an engine's composite does; the providers, as for the other sub-areas, are
/// Broiler.VM's (decision D7), and the <c>nu</c> values each locale's (<see cref="NumberFormatter.NumberingSystemKeyValues"/>).
/// </para>
/// <para>
/// <b>No operation of another area moves.</b> Two of the names are another file's too, <c>Intl.NumberFormat.prototype.format</c>
/// and <c>resolvedOptions</c>, both of the two Intl files this part and sub-area (b)'s read; a vector is answered here only when
/// its identifier is one of the two files', which <c>LibraryRunTests</c> holds for every name across the corpus.
/// </para>
/// </remarks>
internal sealed partial class AlgorithmsLibrary
{
    /// <summary>The identifier prefix of the Intl area's sub-area (c) vectors, the file <c>intl/numbers-and-text.vectors</c>.</summary>
    internal const string IntlNumbersPrefix = "intl/numbers-and-text/";

    /// <summary>The operations of <c>numbers-and-text.vectors</c> this part answers.</summary>
    internal static readonly IReadOnlyList<string> IntlNumbersOperations =
    [
        "Intl.NumberFormat.prototype.format",
        "Intl.NumberFormat.prototype.formatToParts",
        "Intl.NumberFormat.prototype.formatRange",
        "Intl.NumberFormat.prototype.formatRangeToParts",
        "Intl.NumberFormat.prototype.resolvedOptions",
        "Intl.ListFormat.prototype.format",
        "Intl.ListFormat.prototype.formatToParts",
        "Intl.ListFormat (errors)",
        "Intl.RelativeTimeFormat.prototype.format",
        "Intl.RelativeTimeFormat.prototype.formatToParts",
        "Intl.RelativeTimeFormat (errors and plural units)",
        "Intl.DisplayNames.prototype.of",
        "Intl.DisplayNames (errors)",
    ];

    /// <summary>The operations of <c>values-and-plurals.vectors</c> this part answers: the formatting ones, sub-area (c)'s.</summary>
    internal static readonly IReadOnlyList<string> IntlValuesFormatOperations =
    [
        "Intl.NumberFormat.prototype.format",
        "Intl.NumberFormat.prototype.formatToParts",
    ];

    private static readonly Lazy<CompositeLocaleData> IntlComposite = new(static () => new CompositeLocaleData(IntlData));

    /// <summary>The locale data the formatters read: the tables, in the composite's order.</summary>
    private static CompositeLocaleData IntlFormatData => IntlComposite.Value;

    /// <summary>Whether a vector is sub-area (c)'s and one this runner answers.</summary>
    internal static bool IsLibraryIntlNumbers(VectorRecord vector)
    {
        var operation = vector["op"];
        if (IsEngineLevelNumbersScript(vector.Script))
            return false;

        return (vector.Id.StartsWith(IntlNumbersPrefix, StringComparison.Ordinal) && Contains(IntlNumbersOperations, operation))
            || (vector.Id.StartsWith(IntlValuesPrefix, StringComparison.Ordinal) && Contains(IntlValuesFormatOperations, operation));
    }

    /// <summary>
    /// Whether a script's outcome is an engine's alone: a function expression (the bound <c>format</c>, the legacy constructor,
    /// the reads after construction, a <c>valueOf</c> count), a constructor called without <c>new</c>, or a list that is not a
    /// list of Strings, which StringListFromIterable, the engine's iteration, refuses.
    /// </summary>
    internal static bool IsEngineLevelNumbersScript(string script) =>
        script.StartsWith("(function", StringComparison.Ordinal)
        || script.StartsWith("Intl.", StringComparison.Ordinal)
        || (script.Contains("Intl.ListFormat(", StringComparison.Ordinal) && script.Contains(".format", StringComparison.Ordinal) && !ListArgumentIsStrings(script));

    private static bool ListArgumentIsStrings(string script)
    {
        var at = script.IndexOf(".format", StringComparison.Ordinal);
        var open = script.IndexOf('(', at);
        var argument = script[(open + 1)..];
        if (argument.StartsWith("Array(", StringComparison.Ordinal))
            return true;
        if (!argument.StartsWith('['))
            return false;

        var close = argument.IndexOf(']', StringComparison.Ordinal);
        foreach (var item in argument[1..close].Split(", "))
        {
            if (item.Length < 2 || item[0] != '"' || item[^1] != '"')
                return false;
        }

        return true;
    }

    private static bool Contains(IReadOnlyList<string> names, string? operation)
    {
        foreach (var name in names)
        {
            if (name == operation)
                return true;
        }

        return false;
    }

    private static string IntlNumbersArea(VectorRecord vector)
    {
        try
        {
            var value = new NumbersScript(vector.Script).Evaluate();
            return Render(value);
        }
        catch (ScriptError error)
        {
            return "error: " + error.Kind;
        }
    }

    /// <summary>A value as the corpus writes an outcome.</summary>
    private static string Render(object? value) => value switch
    {
        string s => Text(s),
        double d => Number(d),
        int i => Number(i),
        bool b => "value: " + TypedValue.RenderBoolean(b),
        Undefined => "value: undefined",
        _ => throw new NotSupportedException("no sub-area (c) script answers a " + Describe(value)),
    };

    /// <summary>What kind of value a script value is, for a refusal's message, without reflection (rule SL3: the probe names none).</summary>
    private static string Describe(object? value) => value switch
    {
        null => "null",
        string => "String",
        double or int => "Number",
        BigInteger => "BigInt",
        bool => "Boolean",
        Undefined => "undefined",
        List<object?> => "array",
        ScriptObject => "object",
        _ => "value of another kind",
    };

    // ---- the script's values ----------------------------------------------------------------------------------------------

    /// <summary>ECMAScript's <c>undefined</c>.</summary>
    private sealed class Undefined
    {
        internal static readonly Undefined Value = new();
    }

    /// <summary>A thrown <c>RangeError</c> or <c>TypeError</c>.</summary>
    private sealed class ScriptError(string kind) : Exception(kind)
    {
        internal string Kind { get; } = kind;
    }

    /// <summary>An ordinary object of the script: an options bag, a part, or an object with <c>valueOf</c> and <c>toString</c>.</summary>
    private sealed class ScriptObject
    {
        internal List<KeyValuePair<string, object?>> Properties { get; } = [];

        internal object? Get(string name)
        {
            foreach (var (key, value) in Properties)
            {
                if (key == name)
                    return value;
            }

            return Undefined.Value;
        }

        internal bool Has(string name) => Properties.Exists(p => p.Key == name);
    }

    /// <summary><c>function () { return literal; }</c>: a function of the script that returns a value.</summary>
    private sealed record ScriptFunction(object? Result);

    /// <summary><c>Object(v)</c>: a primitive's wrapper, whose ToPrimitive is the primitive.</summary>
    private sealed record Wrapper(object? Primitive);

    /// <summary>An <c>Intl.NumberFormat</c> of the script.</summary>
    private sealed record NumberFormatObject(NumberFormatter Formatter);

    /// <summary>An <c>Intl.ListFormat</c> of the script.</summary>
    private sealed record ListFormatObject(ListFormatter Formatter);

    /// <summary>An <c>Intl.RelativeTimeFormat</c> of the script.</summary>
    private sealed record RelativeTimeFormatObject(RelativeTimeFormatter Formatter);

    /// <summary>An <c>Intl.DisplayNames</c> of the script.</summary>
    private sealed record DisplayNamesObject(DisplayNamesCore Names);

    /// <summary>A built-in function value the script names: <c>JSON</c>, <c>Object</c>, <c>Array</c>, <c>Intl</c>.</summary>
    private sealed record Global(string Name);

    // ---- the reader and evaluator -----------------------------------------------------------------------------------------

    /// <summary>
    /// A reader of the expressions <c>scripts/conformance/generate_intl_vectors.py</c> writes for sub-area (c), evaluated as it
    /// reads them: literals, arrays, objects of literals and literal-returning functions, <c>new Intl.*(...)</c>, member calls,
    /// <c>+</c>, unary <c>-</c> and <c>**</c>. Any other form is not the files', and is refused with <see cref="NotSupportedException"/>.
    /// </summary>
    private sealed class NumbersScript(string text)
    {
        private int at;

        internal object? Evaluate()
        {
            var value = Expression();
            Space();
            if (at != text.Length)
                throw Unsupported();
            return value;
        }

        private NotSupportedException Unsupported() => new("no sub-area (c) vector's script has this form at " + at + ": " + text);

        private void Space()
        {
            while (at < text.Length && text[at] == ' ')
                at++;
        }

        private bool Take(string literal)
        {
            Space();
            if (string.CompareOrdinal(text, at, literal, 0, literal.Length) != 0)
                return false;
            at += literal.Length;
            return true;
        }

        private void Expect(string literal)
        {
            if (!Take(literal))
                throw Unsupported();
        }

        private string Name()
        {
            Space();
            var begin = at;
            while (at < text.Length && (char.IsAsciiLetterOrDigit(text[at]) || text[at] is '$' or '_'))
                at++;
            if (at == begin)
                throw Unsupported();
            return text[begin..at];
        }

        // additive := unary ('+' unary)*
        private object? Expression()
        {
            var left = Unary();
            while (Take("+"))
            {
                var right = Unary();
                left = left is string || right is string ? ToStringOf(left) + ToStringOf(right) : (object)(ToNumberOf(left) + ToNumberOf(right));
            }

            return left;
        }

        // unary := '-' unary | power
        private object? Unary()
        {
            if (Take("-"))
            {
                var operand = Unary();
                return operand is BigInteger big ? -big : -ToNumberOf(operand);
            }

            return Power();
        }

        // power := postfix ('**' unary)?
        private object? Power()
        {
            var value = Postfix();
            if (!Take("**"))
                return value;

            var exponent = Unary();
            if (value is BigInteger big && exponent is BigInteger power && power >= 0 && power <= 100_000)
                return BigInteger.Pow(big, (int)power);
            throw Unsupported();
        }

        private object? Postfix()
        {
            var value = Primary();
            while (true)
            {
                if (Take("."))
                {
                    var name = Name();
                    if (Take("("))
                        value = Call(value, name, Arguments(")"));
                    else
                        value = Property(value, name);
                    continue;
                }

                if (Take("("))
                {
                    value = Apply(value, Arguments(")"));
                    continue;
                }

                return value;
            }
        }

        private List<object?> Arguments(string close)
        {
            var arguments = new List<object?>();
            if (Take(close))
                return arguments;
            while (true)
            {
                arguments.Add(Expression());
                if (Take(close))
                    return arguments;
                Expect(",");
            }
        }

        private object? Primary()
        {
            Space();
            if (at >= text.Length)
                throw Unsupported();

            var c = text[at];
            if (c == '"')
                return StringLiteral();
            if (char.IsAsciiDigit(c))
                return NumberLiteral();
            if (c == '(')
            {
                at++;
                var inner = Expression();
                Expect(")");
                return inner;
            }

            if (c == '[')
            {
                at++;
                return Arguments("]");
            }

            if (c == '{')
            {
                at++;
                return ObjectLiteral();
            }

            var name = Name();
            switch (name)
            {
                case "new":
                    return New();
                case "NaN":
                    return double.NaN;
                case "Infinity":
                    return double.PositiveInfinity;
                case "undefined":
                    return Undefined.Value;
                case "true":
                    return true;
                case "false":
                    return false;
                case "$f64":
                    Expect("(");
                    var halves = Arguments(")");
                    return BitConverter.UInt64BitsToDouble(((ulong)(uint)ToNumberOf(halves[0]) << 32) | (uint)ToNumberOf(halves[1]));
                case "function":
                    return FunctionLiteral();
                case "JSON" or "Object" or "Array" or "Intl":
                    return new Global(name);
                default:
                    throw Unsupported();
            }
        }

        private string StringLiteral()
        {
            at++;
            var value = new StringBuilder();
            while (text[at] != '"')
            {
                if (text[at] == '\\')
                {
                    if (text[at + 1] != 'u')
                        throw Unsupported();
                    value.Append((char)int.Parse(text.AsSpan(at + 2, 4), NumberStyles.AllowHexSpecifier, NumberFormatInfo.InvariantInfo));
                    at += 6;
                    continue;
                }

                value.Append(text[at++]);
            }

            at++;
            return value.ToString();
        }

        private object NumberLiteral()
        {
            var begin = at;
            if (text[at] == '0' && at + 1 < text.Length && text[at + 1] == 'x')
            {
                at += 2;
                while (at < text.Length && char.IsAsciiHexDigit(text[at]))
                    at++;
                return (double)ulong.Parse(text.AsSpan(begin + 2, at - begin - 2), NumberStyles.AllowHexSpecifier, NumberFormatInfo.InvariantInfo);
            }

            while (at < text.Length && (char.IsAsciiDigit(text[at]) || text[at] is '.' or 'e' or 'E' || (text[at] is '+' or '-' && text[at - 1] is 'e' or 'E')))
                at++;
            if (at < text.Length && text[at] == 'n')
                return BigInteger.Parse(text.AsSpan(begin, at++ - begin), NumberStyles.None, NumberFormatInfo.InvariantInfo);
            return NumberParsing.StringToNumber(text.AsSpan(begin, at - begin), null);
        }

        private ScriptObject ObjectLiteral()
        {
            var made = new ScriptObject();
            if (Take("}"))
                return made;
            while (true)
            {
                var name = Name();
                Expect(":");
                made.Properties.Add(new(name, Expression()));
                if (Take("}"))
                    return made;
                Expect(",");
            }
        }

        /// <summary><c>function () { return expression; }</c>, a function that returns a value; any other body is not the files'.</summary>
        private ScriptFunction FunctionLiteral()
        {
            Expect("(");
            Expect(")");
            Expect("{");
            Expect("return");
            var result = Expression();
            Expect(";");
            Expect("}");
            return new ScriptFunction(result);
        }

        private object? New()
        {
            if (Name() != "Intl")
                throw Unsupported();
            Expect(".");
            var service = Name();
            Expect("(");
            var arguments = Arguments(")");
            var locale = arguments.Count > 0 ? arguments[0] : Undefined.Value;
            var options = arguments.Count > 1 ? arguments[1] : Undefined.Value;
            return service switch
            {
                "NumberFormat" => new NumberFormatObject(NewNumberFormat(locale, options)),
                "ListFormat" => NewListFormat(locale, options),
                "RelativeTimeFormat" => NewRelativeTimeFormat(locale, options),
                "DisplayNames" => NewDisplayNames(locale, options),
                _ => throw Unsupported(),
            };
        }

        private object? Apply(object? callee, List<object?> arguments)
        {
            if (callee is Global { Name: "Object" })
                return arguments.Count == 0 ? new ScriptObject() : new Wrapper(arguments[0]);
            if (callee is Global { Name: "Array" })
            {
                var length = (int)ToNumberOf(arguments[0]);
                var array = new List<object?>(length);
                for (var i = 0; i < length; i++)
                    array.Add(Undefined.Value);
                return array;
            }

            throw Unsupported();
        }

        private object? Property(object? value, string name) => (value, name) switch
        {
            (string s, "length") => (double)s.Length,
            (List<object?> list, "length") => (double)list.Count,
            (ScriptObject o, _) => o.Get(name),
            _ => throw Unsupported(),
        };

        private object? Call(object? receiver, string name, List<object?> arguments)
        {
            object? Argument(int i) => i < arguments.Count ? arguments[i] : Undefined.Value;

            switch (receiver, name)
            {
                case (Global { Name: "JSON" }, "stringify"):
                    return Json(Argument(0));
                case (Global { Name: "Object" }, "keys"):
                    return Argument(0) is ScriptObject keyed ? keyed.Properties.ConvertAll(static p => (object?)p.Key) : throw Unsupported();
                case (List<object?> list, "join"):
                    var separator = Argument(0) is Undefined ? "," : ToStringOf(Argument(0));
                    var joined = new StringBuilder();
                    for (var i = 0; i < list.Count; i++)
                        joined.Append(i == 0 ? string.Empty : separator).Append(list[i] is Undefined or null ? string.Empty : ToStringOf(list[i]));
                    return joined.ToString();
                case (List<object?> list, "fill"):
                    for (var i = 0; i < list.Count; i++)
                        list[i] = Argument(0);
                    return list;
                case (string s, "repeat"):
                    var count = (int)ToNumberOf(Argument(0));
                    var repeated = new StringBuilder(s.Length * count);
                    for (var i = 0; i < count; i++)
                        repeated.Append(s);
                    return repeated.ToString();
                case (double or BigInteger, "toLocaleString"):
                    return NewNumberFormat(Argument(0), Argument(1)).Format(ToIntlMathematicalValue(receiver));
                case (NumberFormatObject nf, _):
                    return CallNumberFormat(nf.Formatter, name, Argument(0), Argument(1), arguments.Count);
                case (ListFormatObject lf, "format"):
                    return lf.Formatter.Format(StringList(Argument(0)));
                case (ListFormatObject lf, "formatToParts"):
                    return lf.Formatter.FormatToParts(StringList(Argument(0))).ToList().ConvertAll(static p => (object?)Part(("type", p.Type), ("value", p.Value)));
                case (RelativeTimeFormatObject rtf, "format" or "formatToParts"):
                {
                    var value = ToNumberOf(Argument(0));
                    var unit = ToStringOf(Argument(1));
                    var status = rtf.Formatter.FormatToParts(value, unit, out var parts);
                    if (status != IntlStatus.Completed)
                        throw new ScriptError("RangeError");
                    if (name == "format")
                    {
                        var formatted = new StringBuilder();
                        foreach (var part in parts)
                            formatted.Append(part.Value);
                        return formatted.ToString();
                    }

                    return parts.ToList().ConvertAll(static p => (object?)(p.Unit is null ? Part(("type", p.Type), ("value", p.Value)) : Part(("type", p.Type), ("value", p.Value), ("unit", p.Unit))));
                }

                case (DisplayNamesObject dn, "of"):
                    return dn.Names.Of(ToStringOf(Argument(0)), out var named) == IntlStatus.Completed
                        ? (named is null ? Undefined.Value : named)
                        : throw new ScriptError("RangeError");
            }

            throw Unsupported();
        }

        private object? CallNumberFormat(NumberFormatter formatter, string name, object? first, object? second, int count)
        {
            switch (name)
            {
                case "format":
                    return formatter.Format(ToIntlMathematicalValue(first));
                case "formatToParts":
                    return NumberParts(formatter.FormatToParts(ToIntlMathematicalValue(first)));
                case "formatRange" or "formatRangeToParts":
                {
                    // Intl.NumberFormat.prototype.formatRange steps 3 and 4: both ends present, each made a value.
                    if (count < 2 || first is Undefined || second is Undefined)
                        throw new ScriptError("TypeError");
                    var status = formatter.FormatRangeToParts(ToIntlMathematicalValue(first), ToIntlMathematicalValue(second), out var parts);
                    if (status != IntlStatus.Completed)
                        throw new ScriptError("RangeError");
                    if (name == "formatRangeToParts")
                        return NumberParts(parts);
                    var formatted = new StringBuilder();
                    foreach (var part in parts)
                        formatted.Append(part.Value);
                    return formatted.ToString();
                }

                case "resolvedOptions":
                    return NumberFormatOptions(formatter);
            }

            throw Unsupported();
        }

        private static List<object?> NumberParts(IReadOnlyList<NumberPart> parts) =>
            parts.ToList().ConvertAll(static p => (object?)(p.Source is null ? Part(("type", p.Type), ("value", p.Value)) : Part(("type", p.Type), ("value", p.Value), ("source", p.Source))));

        private static ScriptObject Part(params (string Name, string Value)[] fields)
        {
            var part = new ScriptObject();
            foreach (var (fieldName, value) in fields)
                part.Properties.Add(new(fieldName, value));
            return part;
        }

        /// <summary><c>resolvedOptions()</c> of a number format (<c>sec-intl.numberformat.prototype.resolvedoptions</c>): Table 15's properties in order, each one set.</summary>
        private static ScriptObject NumberFormatOptions(NumberFormatter formatter)
        {
            var options = new ScriptObject();
            void Put(string key, object? value)
            {
                if (value is not null)
                    options.Properties.Add(new(key, value));
            }

            var digits = formatter.DigitOptions;
            Put("locale", formatter.Locale);
            Put("numberingSystem", formatter.NumberingSystem);
            Put("style", formatter.Style switch { NumberStyle.Percent => "percent", NumberStyle.Currency => "currency", NumberStyle.Unit => "unit", _ => "decimal" });
            Put("currency", formatter.Currency);
            Put("currencyDisplay", formatter.CurrencyDisplay switch { null => null, CurrencyDisplay.Code => "code", CurrencyDisplay.NarrowSymbol => "narrowSymbol", CurrencyDisplay.Name => "name", _ => "symbol" });
            Put("currencySign", formatter.CurrencySign switch { null => null, CurrencySign.Accounting => "accounting", _ => "standard" });
            Put("unit", formatter.Unit);
            Put("unitDisplay", formatter.UnitDisplay switch { null => null, UnitDisplay.Long => "long", UnitDisplay.Narrow => "narrow", _ => "short" });
            Put("minimumIntegerDigits", (double)digits.MinimumIntegerDigits);
            Put("minimumFractionDigits", (double?)digits.MinimumFractionDigits);
            Put("maximumFractionDigits", (double?)digits.MaximumFractionDigits);
            Put("minimumSignificantDigits", (double?)digits.MinimumSignificantDigits);
            Put("maximumSignificantDigits", (double?)digits.MaximumSignificantDigits);
            Put("useGrouping", formatter.UseGrouping switch { NumberGrouping.Always => "always", NumberGrouping.Min2 => "min2", NumberGrouping.Off => false, _ => (object)"auto" });
            Put("notation", NotationName(formatter.Notation));
            Put("compactDisplay", formatter.CompactDisplay switch { null => null, CompactDisplay.Long => "long", _ => "short" });
            Put("signDisplay", formatter.SignDisplay switch { SignDisplay.Never => "never", SignDisplay.Always => "always", SignDisplay.ExceptZero => "exceptZero", SignDisplay.Negative => "negative", _ => "auto" });
            Put("roundingIncrement", (double)digits.RoundingIncrement);
            Put("roundingMode", RoundingModeName(digits.RoundingMode));
            Put("roundingPriority", PriorityName(digits.ComputedRoundingPriority));
            Put("trailingZeroDisplay", digits.TrailingZeroDisplay == TrailingZeroDisplay.StripIfInteger ? "stripIfInteger" : "auto");
            return options;
        }

        // ---- the constructors, their reads in order -------------------------------------------------------------------------

        /// <summary><c>new Intl.NumberFormat(locales, options)</c> (<c>sec-intl.numberformat</c>): the reads in order, then the library's formatter.</summary>
        private static NumberFormatter NewNumberFormat(object? locales, object? options)
        {
            var bag = CoerceOptionsToObject(options);
            _ = GetString(bag, "localeMatcher", ["lookup", "best fit"], "best fit");
            var numberingSystem = GetString(bag, "numberingSystem", null, null);
            if (numberingSystem is not null && !LocaleId.IsUnicodeType(numberingSystem))
                throw new ScriptError("RangeError");

            var resolved = Resolve(locales, locale => [new RelevantKey("nu", NumberFormatter.NumberingSystemKeyValues(IntlFormatData, locale)) { Option = numberingSystem }]);

            // SetNumberFormatUnitOptions.
            var style = GetString(bag, "style", ["decimal", "percent", "currency", "unit"], "decimal")!;
            var currency = GetString(bag, "currency", null, null);
            if (currency is null ? style == "currency" : !NumberFormatter.IsWellFormedCurrencyCode(currency))
                throw new ScriptError(currency is null ? "TypeError" : "RangeError");
            var currencyDisplay = GetString(bag, "currencyDisplay", ["code", "symbol", "narrowSymbol", "name"], "symbol")!;
            var currencySign = GetString(bag, "currencySign", ["standard", "accounting"], "standard")!;
            var unit = GetString(bag, "unit", null, null);
            if (unit is null ? style == "unit" : !NumberFormatter.IsWellFormedUnitIdentifier(unit))
                throw new ScriptError(unit is null ? "TypeError" : "RangeError");
            var unitDisplay = GetString(bag, "unitDisplay", ["short", "narrow", "long"], "short")!;
            var notationName = GetString(bag, "notation", ["standard", "scientific", "engineering", "compact"], "standard")!;

            var notation = NotationOf(notationName);
            var numberStyle = style switch { "percent" => NumberStyle.Percent, "currency" => NumberStyle.Currency, "unit" => NumberStyle.Unit, _ => NumberStyle.Decimal };
            if (DigitOptionsOf(DigitBag(bag), DigitOptionRules.NumberFormatDefaults(numberStyle, currency, notation, IntlData), notation, out var digits) is { } refused)
                throw new ScriptError(refused == "error: TypeError" ? "TypeError" : "RangeError");

            var compactDisplay = GetString(bag, "compactDisplay", ["short", "long"], "short")!;
            var grouping = GetGrouping(bag, notation == NumberNotation.Compact ? NumberGrouping.Min2 : NumberGrouping.Auto);
            var signDisplay = GetString(bag, "signDisplay", ["auto", "never", "always", "exceptZero", "negative"], "auto")!;

            return NumberFormatter.Create(IntlFormatData, resolved.Locale, resolved.DataLocale, resolved.KeyValues["nu"] ?? "latn", new NumberFormatOptions
            {
                Style = numberStyle,
                Currency = currency,
                CurrencyDisplay = currencyDisplay switch { "code" => CurrencyDisplay.Code, "narrowSymbol" => CurrencyDisplay.NarrowSymbol, "name" => CurrencyDisplay.Name, _ => CurrencyDisplay.Symbol },
                CurrencySign = currencySign == "accounting" ? CurrencySign.Accounting : CurrencySign.Standard,
                Unit = unit,
                UnitDisplay = unitDisplay switch { "long" => UnitDisplay.Long, "narrow" => UnitDisplay.Narrow, _ => UnitDisplay.Short },
                DigitOptions = digits!,
                Notation = notation,
                CompactDisplay = compactDisplay == "long" ? CompactDisplay.Long : CompactDisplay.Short,
                UseGrouping = grouping,
                SignDisplay = signDisplay switch { "never" => SignDisplay.Never, "always" => SignDisplay.Always, "exceptZero" => SignDisplay.ExceptZero, "negative" => SignDisplay.Negative, _ => SignDisplay.Auto },
            });
        }

        /// <summary><c>new Intl.ListFormat(locales, options)</c> (<c>sec-Intl.ListFormat</c>): GetOptionsObject, the reads in order.</summary>
        private static ListFormatObject NewListFormat(object? locales, object? options)
        {
            var bag = GetOptionsObject(options);
            _ = GetString(bag, "localeMatcher", ["lookup", "best fit"], "best fit");
            var resolved = Resolve(locales, static _ => []);
            var type = GetString(bag, "type", ["conjunction", "disjunction", "unit"], "conjunction")!;
            var style = GetString(bag, "style", ["long", "short", "narrow"], "long")!;
            return new ListFormatObject(ListFormatter.Create(IntlFormatData, resolved.Locale, resolved.DataLocale,
                type switch { "disjunction" => ListFormatType.Disjunction, "unit" => ListFormatType.Unit, _ => ListFormatType.Conjunction }, StyleOf(style)));
        }

        /// <summary><c>new Intl.RelativeTimeFormat(locales, options)</c> (<c>sec-Intl.RelativeTimeFormat</c>): the reads in order.</summary>
        private static RelativeTimeFormatObject NewRelativeTimeFormat(object? locales, object? options)
        {
            var bag = CoerceOptionsToObject(options);
            _ = GetString(bag, "localeMatcher", ["lookup", "best fit"], "best fit");
            var numberingSystem = GetString(bag, "numberingSystem", null, null);
            if (numberingSystem is not null && !LocaleId.IsUnicodeType(numberingSystem))
                throw new ScriptError("RangeError");

            var resolved = Resolve(locales, locale => [new RelevantKey("nu", NumberFormatter.NumberingSystemKeyValues(IntlFormatData, locale)) { Option = numberingSystem }]);
            var style = GetString(bag, "style", ["long", "short", "narrow"], "long")!;
            var numeric = GetString(bag, "numeric", ["always", "auto"], "always")!;
            return new RelativeTimeFormatObject(RelativeTimeFormatter.Create(IntlFormatData, resolved.Locale, resolved.DataLocale, StyleOf(style),
                numeric == "auto" ? RelativeTimeNumeric.Auto : RelativeTimeNumeric.Always, resolved.KeyValues["nu"] ?? "latn"));
        }

        /// <summary><c>new Intl.DisplayNames(locales, options)</c> (<c>sec-Intl.DisplayNames</c>): the options required, the reads in order.</summary>
        private static DisplayNamesObject NewDisplayNames(object? locales, object? options)
        {
            if (options is Undefined)
                throw new ScriptError("TypeError");
            var bag = GetOptionsObject(options);
            _ = GetString(bag, "localeMatcher", ["lookup", "best fit"], "best fit");
            var resolved = Resolve(locales, static _ => []);
            var style = GetString(bag, "style", ["narrow", "short", "long"], "long")!;
            var type = GetString(bag, "type", ["language", "region", "script", "currency", "calendar", "dateTimeField"], null)
                ?? throw new ScriptError("TypeError");
            var fallback = GetString(bag, "fallback", ["code", "none"], "code")!;
            var languageDisplay = GetString(bag, "languageDisplay", ["dialect", "standard"], "dialect")!;
            return new DisplayNamesObject(DisplayNamesCore.Create(IntlFormatData, resolved.Locale, resolved.DataLocale, StyleOf(style),
                type switch
                {
                    "region" => DisplayNamesType.Region,
                    "script" => DisplayNamesType.Script,
                    "currency" => DisplayNamesType.Currency,
                    "calendar" => DisplayNamesType.Calendar,
                    "dateTimeField" => DisplayNamesType.DateTimeField,
                    _ => DisplayNamesType.Language,
                },
                fallback == "none" ? DisplayNamesFallback.None : DisplayNamesFallback.Code,
                languageDisplay == "standard" ? LanguageDisplay.Standard : LanguageDisplay.Dialect));
        }

        private static FormatStyle StyleOf(string style) => style switch { "short" => FormatStyle.Short, "narrow" => FormatStyle.Narrow, _ => FormatStyle.Long };

        /// <summary>ResolveLocale over Broiler.VM's providers for a locales argument of one String, as each service's constructor runs it.</summary>
        private static ResolvedLocale Resolve(object? locales, Func<string, IReadOnlyList<RelevantKey>> keys)
        {
            var tags = locales switch
            {
                Undefined => new List<string>(),
                string s => [s],
                _ => throw new NotSupportedException("no sub-area (c) vector gives its locales as a " + Describe(locales)),
            };

            if (LocaleNegotiation.CanonicalizeLocaleList(tags, IntlData, out var requested, out _) != IntlStatus.Completed)
                throw new ScriptError("RangeError");
            var match = LocaleNegotiation.MatchLocale(IntlAvailable, requested, IntlDefaultLocale);
            return LocaleNegotiation.ResolveLocale(match, keys(match.Locale), IntlFormatData);
        }

        /// <summary>CoerceOptionsToObject: <c>undefined</c> is an empty bag; every bag of the files is an object of literals.</summary>
        private static ScriptObject CoerceOptionsToObject(object? options) => options switch
        {
            Undefined => new ScriptObject(),
            ScriptObject o => o,
            _ => throw new NotSupportedException("no sub-area (c) vector gives its options as a " + Describe(options)),
        };

        /// <summary>GetOptionsObject: <c>undefined</c> is an empty bag, an object itself; anything else a <c>TypeError</c>.</summary>
        private static ScriptObject GetOptionsObject(object? options) => options switch
        {
            Undefined => new ScriptObject(),
            ScriptObject o => o,
            _ => throw new ScriptError("TypeError"),
        };

        /// <summary>GetOption of a String option: absent is the fallback; ToString, then a value outside the list a <c>RangeError</c>.</summary>
        private static string? GetString(ScriptObject bag, string name, string[]? values, string? fallback)
        {
            var given = bag.Get(name);
            if (given is Undefined)
                return fallback;
            var value = ToStringOf(given);
            if (values is not null && Array.IndexOf(values, value) < 0)
                throw new ScriptError("RangeError");
            return value;
        }

        /// <summary>
        /// GetBooleanOrStringNumberFormatOption of <c>useGrouping</c>: absent is the fallback, <see langword="true"/> <c>always</c>,
        /// a false value <see langword="false"/>, <c>"true"</c> and <c>"false"</c> the fallback, another String from the list.
        /// </summary>
        private static NumberGrouping GetGrouping(ScriptObject bag, NumberGrouping fallback)
        {
            var given = bag.Get("useGrouping");
            switch (given)
            {
                case Undefined:
                    return fallback;
                case true:
                    return NumberGrouping.Always;
                case false or "":
                    return NumberGrouping.Off;
            }

            return ToStringOf(given) switch
            {
                "true" or "false" => fallback,
                "min2" => NumberGrouping.Min2,
                "auto" => NumberGrouping.Auto,
                "always" => NumberGrouping.Always,
                _ => throw new ScriptError("RangeError"),
            };
        }

        /// <summary>The digit options of a bag, as sub-area (b)'s reader of SetNumberFormatDigitOptions takes them.</summary>
        private static Dictionary<string, ScriptValue> DigitBag(ScriptObject bag)
        {
            var digits = new Dictionary<string, ScriptValue>(StringComparer.Ordinal);
            foreach (var (key, value) in bag.Properties)
            {
                if (key is "minimumIntegerDigits" or "minimumFractionDigits" or "maximumFractionDigits" or "minimumSignificantDigits"
                    or "maximumSignificantDigits" or "roundingIncrement" or "roundingMode" or "roundingPriority" or "trailingZeroDisplay")
                {
                    digits[key] = value switch
                    {
                        string s => ScriptValue.Of(s),
                        double d => ScriptValue.Of(d),
                        _ => throw new NotSupportedException("no sub-area (c) vector gives " + key + " as a " + Describe(value)),
                    };
                }
            }

            return digits;
        }

        /// <summary>StringListFromIterable over an array of Strings, which every answered vector gives.</summary>
        private static List<string> StringList(object? list) => list switch
        {
            Undefined => [],
            List<object?> items => items.ConvertAll(static item => item as string ?? throw new NotSupportedException("a list of the files holds a non-String")),
            _ => throw new NotSupportedException("no answered sub-area (c) vector gives its list as a " + Describe(list)),
        };

        // ---- the conversions -------------------------------------------------------------------------------------------------

        /// <summary>
        /// ToIntlMathematicalValue (<c>sec-tointlmathematicalvalue</c>) of a script value: ToPrimitive with hint number - a
        /// wrapper's primitive, an object's <c>valueOf</c> result, or where its <c>valueOf</c> is not a function its
        /// <c>toString</c> result - then a BigInt's digits, a String's value, or ToNumber.
        /// </summary>
        private static IntlDecimal ToIntlMathematicalValue(object? value) => ToPrimitive(value) switch
        {
            BigInteger big => IntlDecimal.FromBigInt(big),
            string s => IntlDecimal.FromString(s, BigIntPolicy.Unbounded),
            var primitive => IntlDecimal.FromNumber(ToNumberOf(primitive)),
        };

        private static object? ToPrimitive(object? value) => value switch
        {
            Wrapper w => w.Primitive,
            ScriptObject o when o.Get("valueOf") is ScriptFunction valueOf => valueOf.Result,
            ScriptObject o when o.Get("toString") is ScriptFunction toString => toString.Result,
            ScriptObject => throw new NotSupportedException("no sub-area (c) vector's object has neither valueOf nor toString"),
            _ => value,
        };

        private static double ToNumberOf(object? value) => ToPrimitive(value) switch
        {
            double d => d,
            int i => i,
            string s => NumberParsing.StringToNumber(s, null),
            bool b => b ? 1 : 0,
            Undefined => double.NaN,
            BigInteger => throw new ScriptError("TypeError"),
            var other => throw new NotSupportedException("no sub-area (c) vector converts a " + Describe(other) + " to a Number"),
        };

        private static string ToStringOf(object? value) => ToPrimitive(value) switch
        {
            string s => s,
            double d => NumberText.ToString(d),
            int i => NumberText.ToString(i),
            BigInteger big => big.ToString(NumberFormatInfo.InvariantInfo),
            bool b => b ? "true" : "false",
            Undefined => "undefined",
            var other => throw new NotSupportedException("no sub-area (c) vector converts a " + Describe(other) + " to a String"),
        };

        // ---- JSON.stringify --------------------------------------------------------------------------------------------------

        internal static string Json(object? value)
        {
            var text = new StringBuilder();
            Write(value);
            return text.ToString();

            void Write(object? item)
            {
                switch (item)
                {
                    case string s:
                        Quote(s);
                        break;
                    case double d:
                        text.Append(double.IsFinite(d) ? NumberText.ToString(d) : "null");
                        break;
                    case bool b:
                        text.Append(b ? "true" : "false");
                        break;
                    case List<object?> list:
                        text.Append('[');
                        for (var i = 0; i < list.Count; i++)
                        {
                            if (i > 0)
                                text.Append(',');
                            if (list[i] is Undefined)
                                text.Append("null");
                            else
                                Write(list[i]);
                        }

                        text.Append(']');
                        break;
                    case ScriptObject o:
                        text.Append('{');
                        var first = true;
                        foreach (var (key, field) in o.Properties)
                        {
                            if (field is Undefined)
                                continue;
                            if (!first)
                                text.Append(',');
                            first = false;
                            Quote(key);
                            text.Append(':');
                            Write(field);
                        }

                        text.Append('}');
                        break;
                    default:
                        throw new NotSupportedException("no sub-area (c) script stringifies a " + Describe(item));
                }
            }

            // QuoteJSONString (ECMA-262 25.5.2.3): the short escapes, other controls and lone surrogates as \u.
            void Quote(string s)
            {
                text.Append('"');
                for (var i = 0; i < s.Length; i++)
                {
                    var c = s[i];
                    switch (c)
                    {
                        case '"': text.Append("\\\""); break;
                        case '\\': text.Append("\\\\"); break;
                        case '\b': text.Append("\\b"); break;
                        case '\f': text.Append("\\f"); break;
                        case '\n': text.Append("\\n"); break;
                        case '\r': text.Append("\\r"); break;
                        case '\t': text.Append("\\t"); break;
                        default:
                            var lone = (char.IsHighSurrogate(c) && !(i + 1 < s.Length && char.IsLowSurrogate(s[i + 1])))
                                || (char.IsLowSurrogate(c) && !(i > 0 && char.IsHighSurrogate(s[i - 1])));
                            if (c < ' ' || lone)
                                text.Append("\\u").Append(((int)c).ToString("x4", NumberFormatInfo.InvariantInfo));
                            else
                                text.Append(c);
                            break;
                    }
                }

                text.Append('"');
            }
        }
    }
}
