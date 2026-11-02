// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Broiler.ECMAScript.Conformance;
using Broiler.ECMAScript.Intl;
using Broiler.ECMAScript.Numbers;
using Broiler.ECMAScript.Temporal;

namespace Broiler.ECMAScript.Runners;

/// <summary>
/// The Intl area's sub-area (e), duration formatting (corpus revision 27, <c>conformance/vectors/intl/durations.vectors</c>;
/// stage SJL-9.3e): each operation as an engine's adapter calls <c>Broiler.ECMAScript.Intl</c>'s <see cref="DurationFormatCore"/>,
/// over the data home's generated tables and Broiler.VM's providers, as sub-areas (a) to (d) are (<c>AlgorithmsLibrary.Intl.cs</c>
/// and its siblings).
/// </summary>
/// <remarks>
/// <para>
/// <b>The vectors are read whole, with their scripts</b> (<see cref="IVectorRecordLibrary"/>), as sub-areas (b) to (d)'s are.
/// <see cref="DurationsScript"/> reads the forms the file's generator writes: <c>new Intl.DurationFormat</c> of a String locale and
/// an options bag of literals; its <c>format</c>, <c>formatToParts</c> and <c>resolvedOptions</c>; <c>Intl.DurationFormat.supportedLocalesOf</c>;
/// <c>Temporal.Duration.from</c> of a property bag and its <c>toLocaleString</c>; and what the scripts do around the call -
/// <c>JSON.stringify</c>, <c>Object.keys</c>, <c>join</c>, <c>length</c>, <c>+</c>, and a function of one parameter applied to
/// <c>resolvedOptions()</c>. What an engine's adapter does - the options read in the constructor's order and checked as
/// GetOptionsObject, GetOption and GetNumberOption check them, ResolveLocale's inputs, ToTemporalDuration's reads of a property bag in
/// alphabetical order with ToIntegerIfIntegral, a String's ParseTemporalDurationString (Temporal's grammar), CreateTemporalDuration's
/// IsValidDuration (Temporal's limits), the parts arrays and <c>resolvedOptions</c>' object - is written here once more; every step
/// of the sub-area is the library's.
/// </para>
/// <para>
/// <b>Which vectors are answered</b> (<see cref="IsLibraryIntlDurations"/>): every vector of the file but the engine-level ones
/// (<see cref="IsEngineLevelDurationsScript"/>): a getter defined on the argument or on <c>Temporal.Duration.prototype</c>, which
/// observes how often and in which order an engine reads a script object (rows U7 and U9), and the constructor called without
/// <c>new</c> (row U18), the engine's function object.
/// </para>
/// <para>
/// <b>The engine's parameters are Broiler.VM's</b> (decision D7, rows L28 and L33): the default locale <c>en-US</c> and its available
/// locales; the <c>nu</c> values are each locale's, its default numbering system first (decision D13). A vector whose answer is that
/// policy is held to its <c>per-engine.broiler-vm</c> expectation (<see cref="MatchesBroilerVmExpectation"/>).
/// </para>
/// <para>
/// <b>No operation of another area moves.</b> The file's operation names are its own: no vector outside it carries one, which
/// <c>LibraryRunTests</c> holds for every name across the corpus, and a vector is answered here only when its identifier is this
/// file's.
/// </para>
/// </remarks>
internal sealed partial class AlgorithmsLibrary
{
    /// <summary>The identifier prefix of the Intl area's sub-area (e) vectors, the file <c>intl/durations.vectors</c>.</summary>
    internal const string IntlDurationsPrefix = "intl/durations/";

    /// <summary>The operations of <c>durations.vectors</c> this part answers, each but in the engine-level script forms.</summary>
    internal static readonly IReadOnlyList<string> IntlDurationsOperations =
    [
        "Intl.DurationFormat.prototype.format",
        "Intl.DurationFormat.prototype.formatToParts",
        "Intl.DurationFormat.prototype.resolvedOptions",
        "Intl.DurationFormat.supportedLocalesOf",
        "Intl.DurationFormat (refusals)",
        "Intl.DurationFormat (errors and resolvedOptions)",
        "Temporal.Duration.prototype.toLocaleString",
        "Temporal.Duration.prototype.toLocaleString (refusals)",
    ];

    /// <summary>Whether a vector is sub-area (e)'s and one this runner answers.</summary>
    internal static bool IsLibraryIntlDurations(VectorRecord vector) =>
        vector.Id.StartsWith(IntlDurationsPrefix, StringComparison.Ordinal)
        && Contains(IntlDurationsOperations, vector["op"])
        && !IsEngineLevelDurationsScript(vector.Script);

    /// <summary>
    /// Whether a script's outcome is an engine's alone: a getter it defines (<c>Object.defineProperty</c>) on the argument or on
    /// <c>Temporal.Duration.prototype</c>, which counts or taints the engine's reads of a script object, or the constructor called
    /// without <c>new</c>.
    /// </summary>
    internal static bool IsEngineLevelDurationsScript(string script) =>
        script.Contains("Object.defineProperty(", StringComparison.Ordinal)
        || script.StartsWith("Intl.DurationFormat(", StringComparison.Ordinal);

    private static string IntlDurationsArea(VectorRecord vector)
    {
        try
        {
            return Render(new DurationsScript(vector.Script, null).Evaluate());
        }
        catch (ScriptError error)
        {
            return "error: " + error.Kind;
        }
    }

    // ---- the script's values ----------------------------------------------------------------------------------------------

    /// <summary>An <c>Intl.DurationFormat</c> of the script.</summary>
    private sealed record DurationFormatObject(DurationFormatCore Format);

    /// <summary>A <c>Temporal.Duration</c> of the script: its slots.</summary>
    private sealed record DurationObject(DurationFields Fields);

    /// <summary>A function of the script of one <c>return</c> statement: its parameters and its returned expression's text.</summary>
    private sealed record ReturningFunction(IReadOnlyList<string> Parameters, string Returned);

    // ---- the reader and evaluator -----------------------------------------------------------------------------------------

    /// <summary>
    /// A reader of the expressions <c>scripts/conformance/generate_intl_vectors.py</c> writes for sub-area (e), evaluated as it reads
    /// them; a function's body is the one statement <c>return</c> an expression, in an environment of its parameters. Any other form
    /// is not the file's, and is refused with <see cref="NotSupportedException"/>.
    /// </summary>
    private sealed class DurationsScript(string text, Dictionary<string, object?>? environment)
    {
        private readonly Dictionary<string, object?> names = environment ?? new(StringComparer.Ordinal);

        private int at;

        internal object? Evaluate()
        {
            var value = Expression();
            Space();
            if (at != text.Length)
                throw Unsupported();
            return value;
        }

        private NotSupportedException Unsupported() => new("no sub-area (e) vector's script has this form at " + at + ": " + text);

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
                left = left is string || right is string ? ToStringOf(left) + ToStringOf(right) : ToNumberOf(left) + ToNumberOf(right);
            }

            return left;
        }

        // unary := '-' unary | postfix
        private object? Unary() => Take("-") ? -ToNumberOf(Unary()) : Postfix();

        private object? Postfix()
        {
            var value = Primary();
            while (true)
            {
                if (Take("."))
                {
                    var name = Name();
                    value = Take("(") ? Call(value, name, Arguments(")")) : Property(value, name);
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
                case "Infinity":
                    return double.PositiveInfinity;
                case "undefined":
                    return Undefined.Value;
                case "function":
                    return FunctionLiteral();
                case "JSON" or "Object" or "Intl" or "Temporal":
                    return new Global(name);
                default:
                    return names.TryGetValue(name, out var bound) ? bound : throw Unsupported();
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

        private double NumberLiteral()
        {
            var begin = at;
            while (at < text.Length && (char.IsAsciiDigit(text[at]) || text[at] is '.' or 'e' or 'E' || (text[at] is '+' or '-' && text[at - 1] is 'e' or 'E')))
                at++;
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

        /// <summary><c>function (parameters) { return expression; }</c>: its parameters and the returned expression's text.</summary>
        private ReturningFunction FunctionLiteral()
        {
            Expect("(");
            var parameters = new List<string>();
            if (!Take(")"))
            {
                while (true)
                {
                    parameters.Add(Name());
                    if (Take(")"))
                        break;
                    Expect(",");
                }
            }

            Expect("{");
            Expect("return ");
            var begin = at;
            var depth = 0;
            while (!(depth == 0 && text[at] == ';'))
            {
                var c = text[at++];
                if (c == '"')
                {
                    while (text[at] != '"')
                        at += text[at] == '\\' ? 2 : 1;
                    at++;
                }
                else if (c is '(' or '[' or '{')
                {
                    depth++;
                }
                else if (c is ')' or ']' or '}')
                {
                    depth--;
                }
            }

            var returned = text[begin..at];
            at++;
            Expect("}");
            return new ReturningFunction(parameters, returned);
        }

        private object? Apply(object? callee, List<object?> arguments)
        {
            if (callee is not ReturningFunction function)
                throw Unsupported();

            var scope = new Dictionary<string, object?>(names, StringComparer.Ordinal);
            for (var i = 0; i < function.Parameters.Count; i++)
                scope[function.Parameters[i]] = i < arguments.Count ? arguments[i] : Undefined.Value;
            return new DurationsScript(function.Returned, scope).Evaluate();
        }

        private object? New()
        {
            if (Name() != "Intl")
                throw Unsupported();
            Expect(".");
            if (Name() != "DurationFormat")
                throw Unsupported();
            Expect("(");
            var given = Arguments(")");
            return new DurationFormatObject(NewDurationFormat(given.Count > 0 ? given[0] : Undefined.Value, given.Count > 1 ? given[1] : Undefined.Value));
        }

        private object? Property(object? value, string name) => (value, name) switch
        {
            (string s, "length") => (double)s.Length,
            (List<object?> list, "length") => (double)list.Count,
            (ScriptObject o, _) => o.Get(name),
            (Global { Name: "Intl" }, "DurationFormat") => new Global("Intl.DurationFormat"),
            (Global { Name: "Temporal" }, "Duration") => new Global("Temporal.Duration"),
            _ => throw Unsupported(),
        };

        private object? Call(object? receiver, string name, List<object?> arguments)
        {
            object? Argument(int i) => i < arguments.Count ? arguments[i] : Undefined.Value;

            switch (receiver, name)
            {
                case (Global { Name: "JSON" }, "stringify"):
                    return NumbersScript.Json(Argument(0));
                case (Global { Name: "Object" }, "keys"):
                    return Argument(0) is ScriptObject keyed ? keyed.Properties.ConvertAll(static p => (object?)p.Key) : throw Unsupported();
                case (List<object?> list, "join"):
                    var separator = Argument(0) is Undefined ? "," : ToStringOf(Argument(0));
                    var joined = new StringBuilder();
                    for (var i = 0; i < list.Count; i++)
                        joined.Append(i == 0 ? string.Empty : separator).Append(list[i] is Undefined or null ? string.Empty : ToStringOf(list[i]));
                    return joined.ToString();

                case (Global { Name: "Intl.DurationFormat" }, "supportedLocalesOf"):
                {
                    // SUPPORTEDLOCALESOF: CanonicalizeLocaleList, then FilterLocales over Broiler.VM's available locales; the options
                    // the file gives none.
                    var requested = Requested(Argument(0));
                    return new List<object?>(LocaleNegotiation.FilterLocales(IntlAvailable, requested));
                }

                case (Global { Name: "Temporal.Duration" }, "from"):
                    return new DurationObject(ToTemporalDuration(Argument(0)));

                case (DurationObject duration, "toLocaleString"):
                {
                    // TEMPORAL.DURATION.PROTOTYPE.TOLOCALESTRING (e8cc03fc intl.html): a new DurationFormat of the arguments, and its
                    // partition of the duration's own fields.
                    var format = NewDurationFormat(Argument(0), Argument(1));
                    return format.Format(duration.Fields);
                }

                case (DurationFormatObject format, "format"):
                    return format.Format.Format(ToTemporalDuration(Argument(0)));
                case (DurationFormatObject format, "formatToParts"):
                    return Parts(format.Format.FormatToParts(ToTemporalDuration(Argument(0))));
                case (DurationFormatObject format, "resolvedOptions"):
                    return ResolvedOptions(format.Format);
            }

            throw Unsupported();
        }

        // ---- the engine's reads ------------------------------------------------------------------------------------------------

        /// <summary>CanonicalizeLocaleList of the forms the file gives: undefined, a String, or an array of Strings.</summary>
        private static IReadOnlyList<string> Requested(object? locales)
        {
            List<string> tags = locales switch
            {
                Undefined => [],
                string s => [s],
                List<object?> list => list.ConvertAll(static item => item as string ?? throw new NotSupportedException("no sub-area (e) vector gives a locale that is not a String")),
                _ => throw new NotSupportedException("no sub-area (e) vector gives its locales as a " + Describe(locales)),
            };

            return LocaleNegotiation.CanonicalizeLocaleList(tags, IntlData, out var requested, out _) == IntlStatus.Completed
                ? requested
                : throw new ScriptError("RangeError");
        }

        /// <summary>
        /// The <c>Intl.DurationFormat</c> constructor (<c>sec-Intl.DurationFormat</c>) as an engine runs it: the locale list,
        /// GetOptionsObject, the matcher and the numbering system read and checked, ResolveLocale over Broiler.VM's providers,
        /// [[DigitalFormat]] at the data locale, the style, each unit's two options in Table 20's order with GetDurationUnitOptions,
        /// and <c>fractionalDigits</c>.
        /// </summary>
        private static DurationFormatCore NewDurationFormat(object? locales, object? optionsValue)
        {
            var requested = Requested(locales);

            // GETOPTIONSOBJECT: undefined is an empty bag, an object itself, anything else a TypeError.
            var bag = optionsValue switch
            {
                Undefined => new ScriptObject(),
                ScriptObject o => o,
                _ => throw new ScriptError("TypeError"),
            };

            _ = GetString(bag, "localeMatcher", ["lookup", "best fit"], "best fit");
            var numberingSystem = GetString(bag, "numberingSystem", null, null);
            if (numberingSystem is not null && !LocaleId.IsUnicodeType(numberingSystem))
                throw new ScriptError("RangeError");

            var match = LocaleNegotiation.MatchLocale(IntlAvailable, requested, IntlDefaultLocale);
            var resolved = LocaleNegotiation.ResolveLocale(match,
                [new RelevantKey("nu", NumberFormatter.NumberingSystemKeyValues(IntlFormatData, match.Locale)) { Option = numberingSystem }], IntlFormatData);
            var digital = DurationFormatCore.DigitalFormat(IntlFormatData, resolved.DataLocale);

            var style = GetString(bag, "style", ["long", "short", "narrow", "digital"], "short") switch
            {
                "long" => DurationFormatStyle.Long,
                "narrow" => DurationFormatStyle.Narrow,
                "digital" => DurationFormatStyle.Digital,
                _ => DurationFormatStyle.Short,
            };

            var units = new List<DurationUnitOptions>();
            DurationUnitStyle? previous = null;
            for (var unit = DurationUnit.Years; unit <= DurationUnit.Nanoseconds; unit++)
            {
                var name = DurationFormatCore.UnitName(unit);
                var styles = DurationFormatCore.UnitStyles(unit);
                var names = new string[styles.Count];
                for (var i = 0; i < styles.Count; i++)
                    names[i] = UnitStyleName(styles[i]);

                var given = GetString(bag, name, names, null);
                var display = GetString(bag, name + "Display", ["auto", "always"], null);
                var status = DurationFormatCore.GetDurationUnitOptions(unit, given is null ? null : styles[Array.IndexOf(names, given)],
                    display is null ? null : display == "always" ? DurationUnitDisplay.Always : DurationUnitDisplay.Auto, style, previous, digital.TwoDigitHours, out var options);
                if (status != IntlStatus.Completed)
                    throw new ScriptError("RangeError");
                units.Add(options);
                if (unit is DurationUnit.Hours or DurationUnit.Minutes or DurationUnit.Seconds or DurationUnit.Milliseconds or DurationUnit.Microseconds)
                    previous = options.Style;
            }

            var fractionalDigits = GetNumber(bag, "fractionalDigits", 0, 9);
            return DurationFormatCore.Create(IntlFormatData, resolved.Locale, resolved.DataLocale, resolved.KeyValues["nu"] ?? "latn", style, units, fractionalDigits);
        }

        /// <summary>
        /// ToTemporalDuration (the proposal's <c>sec-temporal-totemporalduration</c>, as <c>e8cc03fc</c>'s <c>intl.html</c> has
        /// <c>format</c> call it): a Temporal.Duration's slots; a String through ParseTemporalDurationString; a property bag through
        /// ToTemporalPartialDurationRecord, its ten fields read in alphabetical order with ToIntegerIfIntegral, none a <c>TypeError</c>;
        /// then CreateTemporalDuration's IsValidDuration. Any other value is a <c>TypeError</c>.
        /// </summary>
        private static DurationFields ToTemporalDuration(object? item)
        {
            DurationFields fields;
            switch (item)
            {
                case DurationObject duration:
                    return duration.Fields;
                case string s:
                    if (!TemporalGrammar.TryParseDuration(s, out fields, out _))
                        throw new ScriptError("RangeError");
                    break;
                case ScriptObject bag:
                {
                    // TOTEMPORALPARTIALDURATIONRECORD: days, hours, microseconds, milliseconds, minutes, months, nanoseconds,
                    // seconds, weeks, years.
                    double? Read(string name)
                    {
                        var value = bag.Get(name);
                        if (value is Undefined)
                            return null;
                        var number = ToNumberOf(value);
                        return double.IsFinite(number) && Math.Truncate(number) == number ? number + 0.0 : throw new ScriptError("RangeError");
                    }

                    var days = Read("days");
                    var hours = Read("hours");
                    var microseconds = Read("microseconds");
                    var milliseconds = Read("milliseconds");
                    var minutes = Read("minutes");
                    var months = Read("months");
                    var nanoseconds = Read("nanoseconds");
                    var seconds = Read("seconds");
                    var weeks = Read("weeks");
                    var years = Read("years");
                    if (days is null && hours is null && microseconds is null && milliseconds is null && minutes is null && months is null
                        && nanoseconds is null && seconds is null && weeks is null && years is null)
                        throw new ScriptError("TypeError");

                    fields = new DurationFields(years ?? 0, months ?? 0, weeks ?? 0, days ?? 0, hours ?? 0, minutes ?? 0, seconds ?? 0,
                        milliseconds ?? 0, microseconds ?? 0, nanoseconds ?? 0);
                    break;
                }

                default:
                    throw new ScriptError("TypeError");
            }

            return TemporalLimits.CheckDuration(fields) == TemporalStatus.Completed ? fields : throw new ScriptError("RangeError");
        }

        private static List<object?> Parts(IReadOnlyList<DurationPart> parts)
        {
            var list = new List<object?>(parts.Count);
            foreach (var part in parts)
            {
                var item = new ScriptObject();
                item.Properties.Add(new("type", part.Type));
                item.Properties.Add(new("value", part.Value));
                if (part.Unit is not null)
                    item.Properties.Add(new("unit", part.Unit));
                list.Add(item);
            }

            return list;
        }

        /// <summary>
        /// <c>resolvedOptions()</c> (<c>sec-Intl.DurationFormat.prototype.resolvedOptions</c>): Table 21's properties in order, a
        /// fractional unit's style written <c>numeric</c>, and <c>fractionalDigits</c> where it was given.
        /// </summary>
        private static ScriptObject ResolvedOptions(DurationFormatCore format)
        {
            var options = new ScriptObject();
            options.Properties.Add(new("locale", format.Locale));
            options.Properties.Add(new("numberingSystem", format.NumberingSystem));
            options.Properties.Add(new("style", format.Style switch
            {
                DurationFormatStyle.Long => "long",
                DurationFormatStyle.Narrow => "narrow",
                DurationFormatStyle.Digital => "digital",
                _ => "short",
            }));

            for (var unit = DurationUnit.Years; unit <= DurationUnit.Nanoseconds; unit++)
            {
                var (style, display) = format.Units[(int)unit];
                var name = DurationFormatCore.UnitName(unit);
                options.Properties.Add(new(name, style == DurationUnitStyle.Fractional ? "numeric" : UnitStyleName(style)));
                options.Properties.Add(new(name + "Display", display == DurationUnitDisplay.Always ? "always" : "auto"));
            }

            if (format.FractionalDigits is { } digits)
                options.Properties.Add(new("fractionalDigits", (double)digits));
            return options;
        }

        private static string UnitStyleName(DurationUnitStyle style) => style switch
        {
            DurationUnitStyle.Long => "long",
            DurationUnitStyle.Short => "short",
            DurationUnitStyle.Narrow => "narrow",
            DurationUnitStyle.Numeric => "numeric",
            DurationUnitStyle.TwoDigit => "2-digit",
            _ => "fractional",
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

        /// <summary>GetNumberOption: absent is undefined; ToNumber, then not-a-number or outside the range a <c>RangeError</c>, else its floor.</summary>
        private static int? GetNumber(ScriptObject bag, string name, int minimum, int maximum)
        {
            var given = bag.Get(name);
            if (given is Undefined)
                return null;
            var value = ToNumberOf(given);
            if (double.IsNaN(value) || value < minimum || value > maximum)
                throw new ScriptError("RangeError");
            return (int)Math.Floor(value);
        }

        // ---- the conversions -------------------------------------------------------------------------------------------------

        private static double ToNumberOf(object? value) => value switch
        {
            double d => d,
            string s => NumberParsing.StringToNumber(s, null),
            bool b => b ? 1 : 0,
            Undefined => double.NaN,
            var other => throw new NotSupportedException("no sub-area (e) vector converts a " + Describe(other) + " to a Number"),
        };

        private static string ToStringOf(object? value) => value switch
        {
            string s => s,
            double d => NumberText.ToString(d),
            bool b => b ? "true" : "false",
            Undefined => "undefined",
            var other => throw new NotSupportedException("no sub-area (e) vector converts a " + Describe(other) + " to a String"),
        };
    }
}
