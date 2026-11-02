// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;
using Broiler.ECMAScript.Conformance;
using Broiler.ECMAScript.Intl;
using Broiler.ECMAScript.Numbers;
using Broiler.ECMAScript.Temporal;
using Broiler.ECMAScript.Time;

namespace Broiler.ECMAScript.Runners;

/// <summary>
/// The Intl area's sub-area (d), date-time formatting, intervals, day periods, hour cycles, zone names and Intl over Temporal
/// (corpus revision 27, <c>conformance/vectors/intl/date-times.vectors</c>; stage SJL-9.3d): each operation as an engine's adapter
/// calls <c>Broiler.ECMAScript.Intl</c>, over the data home's generated tables, the time data home's zones and calendars, and
/// Broiler.VM's providers, as sub-areas (a) to (c) are (<c>AlgorithmsLibrary.Intl.cs</c> and its siblings).
/// </summary>
/// <remarks>
/// <para>
/// <b>The vectors are read whole, with their scripts</b> (<see cref="IVectorRecordLibrary"/>), as sub-areas (b) and (c)'s are.
/// <see cref="DateTimesScript"/> reads the forms the file's generator writes: <c>new Intl.DateTimeFormat</c> of a String locale
/// and an options bag of literals; its <c>format</c>, <c>formatToParts</c>, <c>formatRange</c>, <c>formatRangeToParts</c> and
/// <c>resolvedOptions</c>; <c>Date.UTC</c>, <c>new Date(n)</c> and its three <c>toLocale*String</c> methods; the
/// <c>from</c> of a Temporal type over the ISO strings and the property bag the file names, and their <c>toLocaleString</c>; and
/// what the scripts do around the call - <c>JSON.stringify</c>, <c>Object.keys</c>, <c>join</c>, <c>length</c>, <c>===</c>,
/// <c>in</c>, and the functions that bind values and look a part up by its type in a <c>for</c> loop. What an engine's adapter
/// does - the options read in CreateDateTimeFormat's order and checked as GetOption, GetBooleanOption and GetNumberOption check
/// them, ToDateTimeFormattable's ToNumber, a Temporal object's slots, the parts arrays and <c>resolvedOptions</c>' object - is
/// written here once more; every step of the sub-area is the library's.
/// </para>
/// <para>
/// <b>Which vectors are answered</b> (<see cref="IsLibraryIntlDateTimes"/>): every vector of the file but the engine-level ones
/// (<see cref="IsEngineLevelDateTimesScript"/>): a spy over the options' reads (rows T1 and T2), the bound <c>format</c>'s
/// identity, name and length (row T4), the legacy constructor (row T5) and the host's zone through <c>Temporal.Now</c> (row T11),
/// each an engine's object, reads or provider.
/// </para>
/// <para>
/// <b>The engine's parameters are Broiler.VM's</b> (decision D7, rows L28, L33, T11): the default locale <c>en-US</c>, its
/// available locales, and UTC as the zone of a format without a <c>timeZone</c> option. The resolved zone is the identifier as
/// given, and the zone is named by its primary (decision D15).
/// </para>
/// <para>
/// <b>No operation of another area moves.</b> The file's operation names are its own but for none: a vector is answered here only
/// when its identifier is this file's, which <c>LibraryRunTests</c> holds for every name across the corpus.
/// </para>
/// </remarks>
internal sealed partial class AlgorithmsLibrary
{
    /// <summary>The identifier prefix of the Intl area's sub-area (d) vectors, the file <c>intl/date-times.vectors</c>.</summary>
    internal const string IntlDateTimesPrefix = "intl/date-times/";

    /// <summary>The operations of <c>date-times.vectors</c> this part answers, each but in the engine-level script forms.</summary>
    internal static readonly IReadOnlyList<string> IntlDateTimesOperations =
    [
        "Intl.DateTimeFormat.prototype.format",
        "Intl.DateTimeFormat.prototype.formatToParts",
        "Intl.DateTimeFormat.prototype.formatRange",
        "Intl.DateTimeFormat.prototype.formatRangeToParts",
        "Intl.DateTimeFormat.prototype.resolvedOptions",
        "Intl.DateTimeFormat (timeZoneName)",
        "Intl.DateTimeFormat, resolvedOptions().timeZone",
        "Intl.DateTimeFormat (names of the default zone)",
        "Intl.DateTimeFormat (default time zone)",
        "Intl over Temporal",
        "Temporal toLocaleString",
        "Date.prototype.toLocale* (required and defaults)",
        "Date.prototype.toLocaleString",
        "Date.prototype.toLocaleDateString (String options)",
    ];

    private static readonly Lazy<DateTimeData> IntlDateTimeTables = new(static () => new DateTimeData(IntlFormatData, Zones, LoadCalendars()));

    /// <summary>The date-time data the formats read: the tables in the composite's order, the zones and the calendars.</summary>
    private static DateTimeData IntlDateTimeData => IntlDateTimeTables.Value;

    /// <summary>Whether a vector is sub-area (d)'s and one this runner answers.</summary>
    internal static bool IsLibraryIntlDateTimes(VectorRecord vector) =>
        vector.Id.StartsWith(IntlDateTimesPrefix, StringComparison.Ordinal)
        && Contains(IntlDateTimesOperations, vector["op"])
        && !IsEngineLevelDateTimesScript(vector.Script);

    /// <summary>
    /// Whether a script's outcome is an engine's alone: a <c>Proxy</c> that logs the options' reads, the bound <c>format</c>'s
    /// identity, name or length, the legacy constructor's <c>call</c>, or the host's zone through <c>Temporal.Now</c>.
    /// </summary>
    internal static bool IsEngineLevelDateTimesScript(string script) =>
        script.Contains("new Proxy(", StringComparison.Ordinal)
        || script.Contains("f.format === f.format", StringComparison.Ordinal)
        || script.Contains("(function (g) {", StringComparison.Ordinal)
        || script.Contains(".call(", StringComparison.Ordinal)
        || script.Contains("Temporal.Now", StringComparison.Ordinal);

    private static string IntlDateTimesArea(VectorRecord vector)
    {
        try
        {
            return Render(new DateTimesScript(vector.Script, null).Evaluate());
        }
        catch (ScriptError error)
        {
            return "error: " + error.Kind;
        }
    }

    // ---- the script's values ----------------------------------------------------------------------------------------------

    /// <summary>An <c>Intl.DateTimeFormat</c> of the script.</summary>
    private sealed record DateTimeFormatObject(DateTimeFormat Format);

    /// <summary>A <c>Date</c> of the script: its time value.</summary>
    private sealed record DateObject(double Time);

    /// <summary>A Temporal object of the script: its slots, as ToDateTimeFormattable reads them.</summary>
    private sealed record TemporalObject(DateTimeValue Value);

    /// <summary>A function of the script: its parameters and its body's text, applied by <see cref="DateTimesScript"/>.</summary>
    private sealed record FunctionValue(IReadOnlyList<string> Parameters, string Body);

    // ---- the reader and evaluator -----------------------------------------------------------------------------------------

    /// <summary>
    /// A reader of the expressions and statements <c>scripts/conformance/generate_intl_vectors.py</c> writes for sub-area (d),
    /// evaluated as it reads them. A function's body is run statement by statement - <c>var</c>, <c>return</c>, <c>if</c> and a
    /// <c>for</c> loop over an index - in an environment of its parameters and variables. Any other form is not the file's, and
    /// is refused with <see cref="NotSupportedException"/>.
    /// </summary>
    private sealed class DateTimesScript(string text, Dictionary<string, object?>? environment)
    {
        private readonly Dictionary<string, object?> names = environment ?? new(StringComparer.Ordinal);

        private int at;

        /// <summary>The value a <c>return</c> statement gave, once one has run.</summary>
        private bool returned;

        private object? result = Undefined.Value;

        internal object? Evaluate()
        {
            var value = Expression();
            Space();
            if (at != text.Length)
                throw Unsupported();
            return value;
        }

        private NotSupportedException Unsupported() => new("no sub-area (d) vector's script has this form at " + at + ": " + text);

        private void Space()
        {
            while (at < text.Length && text[at] == ' ')
                at++;
        }

        private bool Peek(string literal)
        {
            Space();
            return string.CompareOrdinal(text, at, literal, 0, literal.Length) == 0;
        }

        private bool Take(string literal)
        {
            if (!Peek(literal))
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

        // ---- statements ---------------------------------------------------------------------------------------------------

        /// <summary>Runs a function's body: its statements in order, until one returns.</summary>
        private object? Run()
        {
            while (!returned)
            {
                Space();
                if (at >= text.Length)
                    break;
                Statement(execute: true);
            }

            return result;
        }

        /// <summary>One statement, run or, where <paramref name="execute"/> is false, read past.</summary>
        private void Statement(bool execute)
        {
            if (!execute)
            {
                SkipStatement();
                return;
            }

            if (Take("var "))
            {
                var name = Name();
                Expect("=");
                names[name] = Expression();
                Expect(";");
                return;
            }

            if (Take("return "))
            {
                result = Expression();
                returned = true;
                Expect(";");
                return;
            }

            if (Take("if ("))
            {
                var condition = Truthy(Expression());
                Expect(")");
                Statement(condition);
                return;
            }

            if (Take("for (var "))
            {
                // for (var i = start; i < bound; i++) statement
                var index = Name();
                Expect("=");
                names[index] = Expression();
                Expect(";");
                var condition = at;
                while (true)
                {
                    at = condition;
                    if (Name() != index)
                        throw Unsupported();
                    Expect("<");
                    var bound = ToNumberOf(Postfix());
                    Expect(";");
                    if (Name() != index)
                        throw Unsupported();
                    Expect("++");
                    Expect(")");
                    var go = ToNumberOf(names[index]) < bound;
                    Statement(go);
                    if (!go || returned)
                        return;
                    names[index] = ToNumberOf(names[index]) + 1;
                }
            }

            throw Unsupported();
        }

        /// <summary>Reads past one statement: to its <c>;</c> outside brackets and strings.</summary>
        private void SkipStatement()
        {
            var depth = 0;
            while (at < text.Length)
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
                else if (c == ';' && depth == 0)
                {
                    return;
                }
            }

            throw Unsupported();
        }

        // ---- expressions --------------------------------------------------------------------------------------------------

        // equality := relational ('===' relational)?
        private object? Expression()
        {
            var left = Relational();
            if (!Take("==="))
                return left;
            var right = Relational();
            return StrictEquals(left, right);
        }

        // relational := additive ('in' additive)?
        private object? Relational()
        {
            var left = Additive();
            if (!Take("in "))
                return left;
            var right = Additive();
            return right is ScriptObject o ? o.Has(ToStringOf(left)) : throw new ScriptError("TypeError");
        }

        // additive := unary ('+' unary)*
        private object? Additive()
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
                if (Peek("++"))
                    return value;

                if (Take("."))
                {
                    var name = Name();
                    value = Take("(") ? Call(value, name, Arguments(")")) : Property(value, name);
                    continue;
                }

                if (Take("["))
                {
                    var key = Expression();
                    Expect("]");
                    value = value is List<object?> list && key is double d ? (d >= 0 && d < list.Count ? list[(int)d] : Undefined.Value) : throw Unsupported();
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
                case "undefined":
                    return Undefined.Value;
                case "true":
                    return true;
                case "false":
                    return false;
                case "function":
                    return FunctionLiteral();
                case "JSON" or "Object" or "Date" or "Temporal":
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

        /// <summary><c>function (parameters) { body }</c>: its parameters and its body's text, run when it is applied.</summary>
        private FunctionValue FunctionLiteral()
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
            var begin = at;
            var depth = 1;
            while (depth > 0)
            {
                var c = text[at++];
                if (c == '"')
                {
                    while (text[at] != '"')
                        at += text[at] == '\\' ? 2 : 1;
                    at++;
                }
                else if (c == '{')
                {
                    depth++;
                }
                else if (c == '}')
                {
                    depth--;
                }
            }

            return new FunctionValue(parameters, text[begin..(at - 1)].Trim(' '));
        }

        private object? New()
        {
            var name = Name();
            if (name == "Date")
            {
                Expect("(");
                var arguments = Arguments(")");
                return new DateObject(EcmaTime.TimeClip(ToNumberOf(arguments[0])));
            }

            if (name != "Intl")
                throw Unsupported();
            Expect(".");
            if (Name() != "DateTimeFormat")
                throw Unsupported();
            Expect("(");
            var given = Arguments(")");
            return new DateTimeFormatObject(NewDateTimeFormat(
                given.Count > 0 ? given[0] : Undefined.Value, given.Count > 1 ? given[1] : Undefined.Value, DateTimeRequired.Any, DateTimeDefaults.Date, null));
        }

        private object? Apply(object? callee, List<object?> arguments)
        {
            if (callee is not FunctionValue function)
                throw Unsupported();

            var scope = new Dictionary<string, object?>(names, StringComparer.Ordinal);
            for (var i = 0; i < function.Parameters.Count; i++)
                scope[function.Parameters[i]] = i < arguments.Count ? arguments[i] : Undefined.Value;
            return new DateTimesScript(function.Body, scope).Run();
        }

        private object? Property(object? value, string name) => (value, name) switch
        {
            (string s, "length") => (double)s.Length,
            (List<object?> list, "length") => (double)list.Count,
            (ScriptObject o, _) => o.Get(name),
            (Global { Name: "Temporal" }, _) => new TemporalType(name),
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
                case (Global { Name: "Date" }, "UTC"):
                    return DateUtc(arguments);
                case (TemporalType type, "from"):
                    return TemporalFrom(type.Name, Argument(0));
                case (List<object?> list, "join"):
                    var separator = Argument(0) is Undefined ? "," : ToStringOf(Argument(0));
                    var joined = new StringBuilder();
                    for (var i = 0; i < list.Count; i++)
                        joined.Append(i == 0 ? string.Empty : separator).Append(list[i] is Undefined or null ? string.Empty : ToStringOf(list[i]));
                    return joined.ToString();
                case (DateObject date, "toLocaleString" or "toLocaleDateString" or "toLocaleTimeString"):
                {
                    // DATE.PROTOTYPE.TOLOCALE*STRING (sup-date.prototype.tolocalestring and the two others): an invalid date's
                    // text, else a format made for the call with the method's required and defaults.
                    if (double.IsNaN(date.Time))
                        return "Invalid Date";
                    var (required, defaults) = name switch
                    {
                        "toLocaleDateString" => (DateTimeRequired.Date, DateTimeDefaults.Date),
                        "toLocaleTimeString" => (DateTimeRequired.Time, DateTimeDefaults.Time),
                        _ => (DateTimeRequired.Any, DateTimeDefaults.All),
                    };
                    var format = NewDateTimeFormat(Argument(0), Argument(1), required, defaults, null);
                    return FormatValue(format, DateTimeValue.Number(date.Time));
                }

                case (TemporalObject temporal, "toLocaleString"):
                {
                    var (required, defaults) = temporal.Value.Kind switch
                    {
                        DateTimeValueKind.PlainDate or DateTimeValueKind.PlainYearMonth or DateTimeValueKind.PlainMonthDay => (DateTimeRequired.Date, DateTimeDefaults.Date),
                        DateTimeValueKind.PlainTime => (DateTimeRequired.Time, DateTimeDefaults.Time),
                        _ => (DateTimeRequired.Any, DateTimeDefaults.All),
                    };
                    var format = NewDateTimeFormat(Argument(0), Argument(1), required, defaults, null);
                    return FormatValue(format, temporal.Value);
                }

                case (DateTimeFormatObject dtf, "format"):
                    return FormatValue(dtf.Format, Formattable(Argument(0)));
                case (DateTimeFormatObject dtf, "formatToParts"):
                {
                    var status = dtf.Format.HandleDateTimeValue(Formattable(Argument(0)), out var formatter, out var time);
                    Refuse(status);
                    return Parts(formatter!.FormatToParts(time));
                }

                case (DateTimeFormatObject dtf, "formatRange" or "formatRangeToParts"):
                {
                    if (Argument(0) is Undefined || Argument(1) is Undefined)
                        throw new ScriptError("TypeError");
                    var x = Formattable(Argument(0));
                    var y = Formattable(Argument(1));
                    var status = dtf.Format.HandleDateTimeRange(x, y, out var formatter, out var xTime, out var yTime);
                    Refuse(status);
                    return name == "formatRange" ? formatter!.FormatRange(xTime, yTime) : Parts(formatter!.FormatRangeToParts(xTime, yTime));
                }

                case (DateTimeFormatObject dtf, "resolvedOptions"):
                    return ResolvedOptions(dtf.Format);
            }

            throw Unsupported();
        }

        /// <summary>A Temporal type the script names, whose <c>from</c> it calls.</summary>
        private sealed record TemporalType(string Name);

        /// <summary>Date.UTC (ECMA-262 21.4.3.4) of the literal fields the files give: a year and a month, then the others or their defaults.</summary>
        private static double DateUtc(List<object?> arguments)
        {
            double Field(int i, double fallback) => i < arguments.Count ? ToNumberOf(arguments[i]) : fallback;
            var year = EcmaTime.MakeFullYear(Field(0, double.NaN));
            var day = EcmaTime.MakeDay(year, Field(1, 0), Field(2, 1));
            var time = EcmaTime.MakeTime(Field(3, 0), Field(4, 0), Field(5, 0), Field(6, 0));
            return EcmaTime.TimeClip(EcmaTime.MakeDate(day, time));
        }

        /// <summary>
        /// The <c>from</c> of a Temporal type over the forms the file gives: an ISO date (<c>2024-01-05</c>), year-month
        /// (<c>2024-03</c>), month-day (<c>03-10</c>), time (<c>15:07</c>) or UTC instant (<c>2024-01-05T15:07:09Z</c>), in the ISO
        /// 8601 calendar; and a PlainYearMonth's bag of a year, a month and the Gregorian calendar, whose reference day is the 1st.
        /// </summary>
        private object TemporalFrom(string type, object? item)
        {
            static int Digits(string s, int at, int length) => int.Parse(s.AsSpan(at, length), NumberStyles.None, NumberFormatInfo.InvariantInfo);

            switch (type, item)
            {
                case ("PlainDate", string s) when s.Length == 10:
                    return new TemporalObject(DateTimeValue.PlainDate(new IsoDate(Digits(s, 0, 4), Digits(s, 5, 2), Digits(s, 8, 2)), "iso8601"));
                case ("PlainYearMonth", string s) when s.Length == 7:
                    return new TemporalObject(DateTimeValue.PlainYearMonth(new IsoDate(Digits(s, 0, 4), Digits(s, 5, 2), 1), "iso8601"));
                case ("PlainYearMonth", ScriptObject bag) when bag.Get("calendar") is "gregory":
                    return new TemporalObject(DateTimeValue.PlainYearMonth(new IsoDate((long)ToNumberOf(bag.Get("year")), (int)ToNumberOf(bag.Get("month")), 1), "gregory"));
                case ("PlainMonthDay", string s) when s.Length == 5:
                    return new TemporalObject(DateTimeValue.PlainMonthDay(new IsoDate(1972, Digits(s, 0, 2), Digits(s, 3, 2)), "iso8601"));
                case ("PlainTime", string s) when s.Length == 5:
                    return new TemporalObject(DateTimeValue.PlainTime(new IsoTime(Digits(s, 0, 2), Digits(s, 3, 2), 0, 0, 0, 0)));
                case ("Instant", string s) when s.Length == 20 && s[^1] == 'Z':
                {
                    var date = new IsoDate(Digits(s, 0, 4), Digits(s, 5, 2), Digits(s, 8, 2));
                    var time = new IsoTime(Digits(s, 11, 2), Digits(s, 14, 2), Digits(s, 17, 2), 0, 0, 0);
                    return new TemporalObject(DateTimeValue.Instant(IsoArithmetic.GetUtcEpochNanoseconds(new IsoDateTime(date, time))));
                }

                default:
                    throw Unsupported();
            }
        }

        /// <summary>ToDateTimeFormattable (the proposal's <c>sec-todatetimeformattable</c>): a Temporal object as itself, anything else by ToNumber.</summary>
        private static DateTimeValue Formattable(object? value) => value switch
        {
            TemporalObject temporal => temporal.Value,
            DateObject date => DateTimeValue.Number(date.Time),
            _ => DateTimeValue.Number(ToNumberOf(value)),
        };

        private static string FormatValue(DateTimeFormat format, DateTimeValue value)
        {
            var status = format.HandleDateTimeValue(value, out var formatter, out var time);
            Refuse(status);
            return formatter!.Format(time);
        }

        /// <summary>A status as the engine throws it: each of sub-area (d)'s a <c>RangeError</c> or a <c>TypeError</c>.</summary>
        private static void Refuse(IntlStatus status)
        {
            switch (status)
            {
                case IntlStatus.Completed:
                    return;
                case IntlStatus.TimeZoneOffsetHasSeconds or IntlStatus.InvalidTimeZone or IntlStatus.TimeValueOutOfRange or IntlStatus.TemporalCalendarMismatch:
                    throw new ScriptError("RangeError");
                default:
                    throw new ScriptError("TypeError");
            }
        }

        private static List<object?> Parts(IReadOnlyList<DateTimePart> parts)
        {
            var list = new List<object?>(parts.Count);
            foreach (var part in parts)
            {
                var item = new ScriptObject();
                item.Properties.Add(new("type", part.Type));
                item.Properties.Add(new("value", part.Value));
                if (part.Source is not null)
                    item.Properties.Add(new("source", part.Source));
                list.Add(item);
            }

            return list;
        }

        /// <summary><c>resolvedOptions()</c> (<c>sec-intl.datetimeformat.prototype.resolvedoptions</c>): Table 17's properties in order, each one set.</summary>
        private static ScriptObject ResolvedOptions(DateTimeFormat format)
        {
            var options = new ScriptObject();
            options.Properties.Add(new("locale", format.Locale));
            options.Properties.Add(new("calendar", format.Calendar));
            options.Properties.Add(new("numberingSystem", format.NumberingSystem));
            options.Properties.Add(new("timeZone", format.TimeZone));
            if (format.HourCycle is { } hourCycle)
            {
                options.Properties.Add(new("hourCycle", hourCycle));
                options.Properties.Add(new("hour12", format.Hour12));
            }

            foreach (var (name, value) in format.ResolvedComponents)
                options.Properties.Add(new(name, name == "fractionalSecondDigits" ? double.Parse(value, NumberFormatInfo.InvariantInfo) : value));
            if (format.DateStyle is { } dateStyle)
                options.Properties.Add(new("dateStyle", dateStyle));
            if (format.TimeStyle is { } timeStyle)
                options.Properties.Add(new("timeStyle", timeStyle));
            return options;
        }

        // ---- the constructor, as an engine reads its options -----------------------------------------------------------------

        /// <summary>
        /// CreateDateTimeFormat (<c>sec-createdatetimeformat</c>, as <c>e8cc03fc</c> amends it) as an engine runs it: the locale
        /// list, the options read in the operation's order and checked, ResolveLocale over Broiler.VM's providers, the time zone,
        /// then the library's steps.
        /// </summary>
        private static DateTimeFormat NewDateTimeFormat(object? locales, object? optionsValue, DateTimeRequired required, DateTimeDefaults defaults, string? zonedTimeZone)
        {
            var tags = locales switch
            {
                Undefined => new List<string>(),
                string s => [s],
                _ => throw new NotSupportedException("no sub-area (d) vector gives its locales as a " + Describe(locales)),
            };

            if (LocaleNegotiation.CanonicalizeLocaleList(tags, IntlData, out var requested, out _) != IntlStatus.Completed)
                throw new ScriptError("RangeError");

            // COERCEOPTIONSTOOBJECT: undefined is an empty bag; a String is its wrapper, which names no option.
            var bag = optionsValue switch
            {
                Undefined => new ScriptObject(),
                ScriptObject o => o,
                string => new ScriptObject(),
                _ => throw new NotSupportedException("no sub-area (d) vector gives its options as a " + Describe(optionsValue)),
            };

            _ = GetString(bag, "localeMatcher", ["lookup", "best fit"], "best fit");
            var calendar = GetString(bag, "calendar", null, null);
            if (calendar is not null && !LocaleId.IsUnicodeType(calendar))
                throw new ScriptError("RangeError");
            var numberingSystem = GetString(bag, "numberingSystem", null, null);
            if (numberingSystem is not null && !LocaleId.IsUnicodeType(numberingSystem))
                throw new ScriptError("RangeError");
            var hour12 = bag.Get("hour12") is Undefined ? (bool?)null : Truthy(bag.Get("hour12"));
            var hourCycle = GetString(bag, "hourCycle", ["h11", "h12", "h23", "h24"], null);

            var match = LocaleNegotiation.MatchLocale(IntlAvailable, requested, IntlDefaultLocale);
            var resolved = LocaleNegotiation.ResolveLocale(match,
            [
                new RelevantKey("ca", DateTimeFormat.CalendarKeyValues) { Option = calendar },
                new RelevantKey("hc", DateTimeFormat.HourCycleKeyValues) { Option = hour12 is null ? hourCycle : null, OptionIsNull = hour12 is not null },
                new RelevantKey("nu", DateTimeFormat.NumberingSystemKeyValues(IntlFormatData, match.Locale)) { Option = numberingSystem },
            ], IntlFormatData);

            // THE TIME ZONE: Temporal.ZonedDateTime.prototype.toLocaleString names its own and refuses the option; otherwise
            // Broiler.VM's default, UTC, where the option is absent.
            var zoneOption = bag.Get("timeZone");
            if (zonedTimeZone is not null)
            {
                if (zoneOption is not Undefined)
                    throw new ScriptError("TypeError");
                zoneOption = zonedTimeZone;
            }

            var zone = ResolvedTimeZone.Utc;
            if (zoneOption is not Undefined)
            {
                var status = ResolvedTimeZone.Resolve(ToStringOf(zoneOption), Zones, out var found);
                Refuse(status);
                zone = found!;
            }

            var components = new DateTimeComponents
            {
                Weekday = GetString(bag, "weekday", ["narrow", "short", "long"], null),
                Era = GetString(bag, "era", ["narrow", "short", "long"], null),
                Year = GetString(bag, "year", ["2-digit", "numeric"], null),
                Month = GetString(bag, "month", ["2-digit", "numeric", "narrow", "short", "long"], null),
                Day = GetString(bag, "day", ["2-digit", "numeric"], null),
                DayPeriod = GetString(bag, "dayPeriod", ["narrow", "short", "long"], null),
                Hour = GetString(bag, "hour", ["2-digit", "numeric"], null),
                Minute = GetString(bag, "minute", ["2-digit", "numeric"], null),
                Second = GetString(bag, "second", ["2-digit", "numeric"], null),
                FractionalSecondDigits = GetNumber(bag, "fractionalSecondDigits", 1, 3),
                TimeZoneName = GetString(bag, "timeZoneName", ["short", "long", "shortOffset", "longOffset", "shortGeneric", "longGeneric"], null),
            };

            _ = GetString(bag, "formatMatcher", ["basic", "best fit"], "best fit");
            var dateStyle = GetString(bag, "dateStyle", ["full", "long", "medium", "short"], null);
            var timeStyle = GetString(bag, "timeStyle", ["full", "long", "medium", "short"], null);

            var created = DateTimeFormat.Create(IntlDateTimeData, resolved, new DateTimeFormatOptions
            {
                Hour12 = hour12,
                TimeZone = zone,
                Components = components,
                DateStyle = dateStyle,
                TimeStyle = timeStyle,
                Required = required,
                Defaults = defaults,
                ForZonedDateTime = zonedTimeZone is not null,
            }, out var format);
            Refuse(created);
            return format!;
        }

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

        private static bool Truthy(object? value) => value switch
        {
            bool b => b,
            double d => d is not (0 or double.NaN),
            string s => s.Length != 0,
            Undefined or null => false,
            _ => true,
        };

        private static bool StrictEquals(object? left, object? right) => (left, right) switch
        {
            (string a, string b) => a == b,
            (double a, double b) => a == b,
            (bool a, bool b) => a == b,
            (Undefined, Undefined) => true,
            _ => ReferenceEquals(left, right),
        };

        private static double ToNumberOf(object? value) => value switch
        {
            double d => d,
            string s => NumberParsing.StringToNumber(s, null),
            bool b => b ? 1 : 0,
            Undefined => double.NaN,
            DateObject date => date.Time,
            var other => throw new NotSupportedException("no sub-area (d) vector converts a " + Describe(other) + " to a Number"),
        };

        private static string ToStringOf(object? value) => value switch
        {
            string s => s,
            double d => NumberText.ToString(d),
            bool b => b ? "true" : "false",
            Undefined => "undefined",
            var other => throw new NotSupportedException("no sub-area (d) vector converts a " + Describe(other) + " to a String"),
        };
    }
}
