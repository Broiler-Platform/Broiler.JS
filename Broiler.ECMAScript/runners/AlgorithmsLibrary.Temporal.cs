using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using Broiler.ECMAScript.Conformance;
using Broiler.ECMAScript.Temporal;
using Broiler.ECMAScript.Time;

namespace Broiler.ECMAScript.Runners;

/// <summary>
/// The Temporal area (corpus revision 25, milestone SJL-10.3): each operation as an engine's adapter calls
/// <c>Broiler.ECMAScript.Algorithms</c>'s Temporal core, through the runner's <see cref="TemporalAdapter"/>, over the
/// data home's generated tables.
/// </summary>
/// <remarks>
/// <para>
/// <b>A vector's inputs are its call's values in order</b> (the corpus's schema): a property bag is its keys and
/// values, an object the script writes as source is the inputs that source stands for, and the options of
/// <c>Duration.prototype.round</c> and of the difference methods are fixed slots, <c>undefined</c> where absent
/// (<c>scripts/conformance/generate_temporal_vectors.py</c>). Each operation's decoding below reads them back into the
/// call: a bag's keys are told from the next value's by the keys each kind of bag can have, and the rest of a call is
/// what the operation's name and the generator fix. A vector whose inputs are not consumed exactly is a defect of the
/// decoding, and throws.
/// </para>
/// <para>
/// <b>What the vector's script does around the call is written here once more</b>, as an engine's adapter or a
/// script does it: the getters it reads and the strings it joins. Every Temporal step is the adapter's, which is
/// Broiler.VM's realm code around the library's operations, and every computation is the library's.
/// </para>
/// <para>
/// <b>The tables are the data home's</b>, <c>Broiler.ECMAScript/data/time/generated/</c>, the calendar year table
/// beside the zone tables, held to the lengths and SHA-256 their manifest records, as the time-zone area reads them.
/// </para>
/// </remarks>
internal sealed partial class AlgorithmsLibrary
{
    private static readonly Lazy<TemporalAdapter> TemporalTables = new(static () => new TemporalAdapter(LoadCalendars(), Zones));

    /// <summary>The adapter every Temporal vector runs through.</summary>
    private static TemporalAdapter Temporal => TemporalTables.Value;

    /// <summary>
    /// Whether a Temporal operation's vectors are engine-level: answered by no library, because their outcome is an
    /// engine's alone.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><c>Function.prototype.length</c>: a built-in function's own property, which the engine defines.</item>
    /// <item><c>Temporal.Duration.prototype.add, the second argument's reads</c>: which properties the engine reads
    /// (a getter's calls), which only a script observes.</item>
    /// <item><c>ToTemporalCalendarIdentifier (an object), …</c>: the calendar an object lends is read from its internal
    /// slot, which is the engine's (row C13, as part B's correspondence tests name it); the vector gives the object by
    /// a description.</item>
    /// </list>
    /// </remarks>
    internal static bool IsEngineLevelTemporal(string operation) =>
        operation is "Function.prototype.length" or "Temporal.Duration.prototype.add, the second argument's reads"
        || operation.StartsWith("ToTemporalCalendarIdentifier (an object), ", StringComparison.Ordinal);

    private static string TemporalArea(string operation, IReadOnlyList<TypedValue> inputs)
    {
        if (IsEngineLevelTemporal(operation))
            return EmptyLibrary.NotImplemented;

        var reader = new TemporalInputs(inputs);
        object? answer;
        try
        {
            answer = TemporalCall(operation, reader);
        }
        catch (TemporalAdapter.RangeError)
        {
            return RangeError;
        }
        catch (TemporalAdapter.TypeError)
        {
            return "error: TypeError";
        }

        if (answer is NotTemporal)
            return EmptyLibrary.NotImplemented;

        reader.End(operation);
        return answer switch
        {
            string s => Text(s),
            double d => Number(d),
            bool b => Boolean(b),
            _ => Text(Temporal.Render(answer)),
        };
    }

    /// <summary>The answer of an operation the area does not have.</summary>
    private sealed class NotTemporal
    {
        internal static readonly NotTemporal Value = new();
    }

    // The keys of each kind of bag the vectors give: a duration's fields, a date's, time's or zoned value's fields,
    // and the options.
    private enum Keys
    {
        Duration,
        Fields,
        Options,
    }

    private static bool IsKey(Keys kind, string key) => kind switch
    {
        Keys.Duration => DurationRank(key) >= 0,
        Keys.Fields => key is "calendar" or "era" or "eraYear" or "year" or "month" or "monthCode" or "day" or "hour" or "minute" or "second"
            or "millisecond" or "microsecond" or "nanosecond" or "offset" or "timeZone",
        _ => key is "overflow" or "disambiguation" or "offset" or "largestUnit" or "smallestUnit" or "roundingIncrement" or "roundingMode"
            or "relativeTo" or "unit" or "calendarName",
    };

    // A duration field's place from the largest unit down, or -1 for another key.
    private static int DurationRank(string key) => key switch
    {
        "years" => 0, "months" => 1, "weeks" => 2, "days" => 3, "hours" => 4, "minutes" => 5, "seconds" => 6, "milliseconds" => 7,
        "microseconds" => 8, "nanoseconds" => 9, _ => -1,
    };

    private static object? TemporalCall(string operation, TemporalInputs at)
    {
        var a = Temporal;
        switch (operation)
        {
            // ---- Construction and the limits (balancing.vectors, rows R1 to R8) --------------------------------
            case "Temporal.PlainDate (constructor), toString": return TemporalAdapter.NewPlainDate(at.Rest());
            case "Temporal.PlainDateTime (constructor), toString": return TemporalAdapter.NewPlainDateTime(at.Rest());
            case "Temporal.PlainTime (constructor), toString": return TemporalAdapter.NewPlainTime(at.Rest());
            case "Temporal.PlainYearMonth (constructor), toString": return TemporalAdapter.NewPlainYearMonth(at.Rest());
            case "Temporal.PlainMonthDay (constructor), toString": return TemporalAdapter.NewPlainMonthDay(at.Rest());
            case "Temporal.Duration (constructor), toString": return TemporalAdapter.NewDuration(at.Rest());
            case "Temporal.Duration (constructor), years is +0":
            {
                var years = TemporalAdapter.NewDuration(at.Rest()).Fields.Years;
                return years == 0 && !double.IsNegative(years);
            }

            case "Temporal.ZonedDateTime (constructor), toString": return a.NewZonedDateTime(at.Value(), at.Value());
            case "Temporal.ZonedDateTime (constructor), toPlainDateTime": return a.ZonedToPlainDateTime(a.NewZonedDateTime(at.Value(), at.Value()));
            case "Temporal.ZonedDateTime (constructor), toPlainDate": return a.ZonedToPlainDate(a.NewZonedDateTime(at.Value(), at.Value()));
            case "Temporal.ZonedDateTime (constructor), timeZoneId": return a.NewZonedDateTime(at.Value(), at.Value()).TimeZone;
            case "Temporal.Instant.fromEpochNanoseconds, toString": return TemporalAdapter.InstantFromEpochNanoseconds(at.Value());
            case "Temporal.Instant.fromEpochMilliseconds, toString": return TemporalAdapter.InstantFromEpochMilliseconds(at.Value());

            case "Temporal.PlainDate.from (property bag), toString": return a.ToPlainDate(at.Bag(Keys.Fields));
            case "Temporal.PlainDate.from (property bag, options), toString": return a.ToPlainDate(at.Bag(Keys.Fields), at.Bag(Keys.Options));
            case "Temporal.PlainDateTime.from (property bag), toString": return a.ToPlainDateTime(at.Bag(Keys.Fields));
            case "Temporal.PlainDateTime.from (property bag, options), toString": return a.ToPlainDateTime(at.Bag(Keys.Fields), at.Bag(Keys.Options));
            case "Temporal.PlainTime.from (property bag), toString": return a.ToPlainTime(at.Bag(Keys.Fields));
            case "Temporal.PlainTime.from (property bag, options), toString": return a.ToPlainTime(at.Bag(Keys.Fields), at.Bag(Keys.Options));
            case "Temporal.PlainYearMonth.from (property bag), toString": return a.ToPlainYearMonth(at.Bag(Keys.Fields));
            case "Temporal.PlainYearMonth.from (property bag, options), toString": return a.ToPlainYearMonth(at.Bag(Keys.Fields), at.Bag(Keys.Options));
            case "Temporal.PlainMonthDay.from (property bag), toString": return a.ToPlainMonthDay(at.Bag(Keys.Fields));
            case "Temporal.PlainMonthDay.from (property bag, options), toString": return a.ToPlainMonthDay(at.Bag(Keys.Fields), at.Bag(Keys.Options));
            case "Temporal.Duration.from (property bag), toString": return a.ToDuration(at.Bag(Keys.Duration));
            case "Temporal.Duration.from (property bag), nanoseconds": return a.ToDuration(at.Bag(Keys.Duration)).Fields.Nanoseconds;

            case "Temporal.PlainDate.prototype.toPlainDateTime, toString": return a.ToPlainDateTime(a.ToPlainDate(at.Str()), at.Optional());
            case "Temporal.PlainDate.prototype.toPlainYearMonth, toString": return a.DateToPlainYearMonth(a.ToPlainDate(at.Str()));

            // ---- with ---------------------------------------------------------------------------------------------
            case "Temporal.PlainDate.prototype.with, toString": return a.With(a.ToPlainDate(at.Str()), at.Bag(Keys.Fields), at.OptionalBag());
            case "Temporal.PlainDateTime.prototype.with, toString": return a.With(a.ToPlainDateTime(at.Str()), at.Bag(Keys.Fields), at.OptionalBag());
            case "Temporal.PlainTime.prototype.with, toString": return a.With(a.ToPlainTime(at.Str()), at.Bag(Keys.Fields), at.OptionalBag());
            case "Temporal.PlainMonthDay.prototype.with, toString": return a.With(a.ToPlainMonthDay(at.Str()), at.Bag(Keys.Fields), at.OptionalBag());
            case "Temporal.Duration.prototype.with, toString": return TemporalAdapter.With(a.ToDuration(at.Str()), at.DurationArgument());
            case "Temporal.Duration.prototype.with, sign": return (double)DurationMath.Sign(TemporalAdapter.With(a.ToDuration(at.Str()), at.DurationArgument()).Fields);

            // ---- Addition of points (rows R7, B1 to B8) ------------------------------------------------------------
            case "Temporal.PlainDate.prototype.add, toString":
            case "Temporal.PlainDate.prototype.subtract, toString":
                return a.Add(a.ToPlainDate(at.Str()), at.DurationArgument(), at.OptionalBag(), Subtract(operation));
            case "Temporal.PlainDateTime.prototype.add, toString":
            case "Temporal.PlainDateTime.prototype.subtract, toString":
                return a.Add(a.ToPlainDateTime(at.Str()), at.DurationArgument(), at.OptionalBag(), Subtract(operation));
            case "Temporal.PlainTime.prototype.add, toString":
            case "Temporal.PlainTime.prototype.subtract, toString":
                return a.Add(a.ToPlainTime(at.Str()), at.DurationArgument(), Subtract(operation));
            case "Temporal.Instant.prototype.add, toString":
            case "Temporal.Instant.prototype.subtract, toString":
                return a.Add(a.ToInstant(at.Str()), at.DurationArgument(), Subtract(operation));
            case "Temporal.ZonedDateTime.prototype.add, toString":
            case "Temporal.ZonedDateTime.prototype.subtract, toString":
                return a.Add(Zoned(at), at.DurationArgument(), at.OptionalBag(), Subtract(operation));
            case "Temporal.PlainYearMonth.prototype.add, toString":
            case "Temporal.PlainYearMonth.prototype.subtract, toString":
                return a.Add(a.ToPlainYearMonth(at.Str()), at.DurationArgument(), at.OptionalBag(), Subtract(operation));

            // ---- Rounding of points (row B9, and F7's round) --------------------------------------------------------
            case "Temporal.PlainTime.prototype.round, toString": return a.Round(a.ToPlainTime(at.Str()), at.Bag(Keys.Options));
            case "Temporal.PlainDateTime.prototype.round, toString": return a.Round(a.ToPlainDateTime(at.Str()), at.Bag(Keys.Options));
            case "Temporal.Instant.prototype.round, toString":
                return a.Round(at.Peek().Kind == TypedValueKind.BigInt ? TemporalAdapter.InstantFromEpochNanoseconds(at.Value()) : a.ToInstant(at.Str()), at.Bag(Keys.Options));
            case "Temporal.ZonedDateTime.prototype.round, toString": return a.Round(a.ToZonedDateTime(at.Str()), at.Bag(Keys.Options));
            case "Temporal.ZonedDateTime.prototype.hoursInDay (string)": return a.HoursInDay(a.ToZonedDateTime(at.Str()));

            // ---- Durations (rows D1 to D4) ------------------------------------------------------------------------
            case "Temporal.Duration.prototype.sign": return (double)DurationMath.Sign(a.ToDuration(at.Str()).Fields);
            case "Temporal.Duration.prototype.blank": return DurationMath.Sign(a.ToDuration(at.Str()).Fields) == 0;
            case "Temporal.Duration.prototype.negated, toString": return new TemporalAdapter.Duration(DurationMath.Negate(a.ToDuration(at.Str()).Fields));
            case "Temporal.Duration.prototype.negated, hours is +0": return IsPositiveZero(DurationMath.Negate(a.ToDuration(at.Str()).Fields).Hours);
            case "Temporal.Duration.prototype.negated, years is +0": return IsPositiveZero(DurationMath.Negate(a.ToDuration(at.Str()).Fields).Years);
            case "Temporal.Duration.prototype.abs, toString": return new TemporalAdapter.Duration(DurationMath.Abs(a.ToDuration(at.Str()).Fields));
            case "Temporal.Duration.prototype.add, toString":
            case "Temporal.Duration.prototype.subtract, toString":
            case "Temporal.Duration.prototype.add, nanoseconds":
            case "Temporal.Duration.prototype.add, milliseconds":
            {
                // Duration.from(bag).add(bag[, second]): two bags of duration fields, and a second argument add never reads.
                var (one, two) = at.TwoDurationBags();
                if (!at.AtEnd)
                    at.SecondArgument();

                var sum = a.AddDurations(a.ToDuration(one), two, operation.StartsWith("Temporal.Duration.prototype.subtract", StringComparison.Ordinal));
                return operation switch
                {
                    "Temporal.Duration.prototype.add, nanoseconds" => sum.Fields.Nanoseconds,
                    "Temporal.Duration.prototype.add, milliseconds" => sum.Fields.Milliseconds,
                    _ => sum,
                };
            }

            case "Temporal.Duration.compare":
            {
                var (one, two) = at.TwoDurationBags();
                var relativeTo = RelativeToValue(at);
                return (double)a.Compare(one, two, relativeTo is null ? null : new TemporalAdapter.Bag(("relativeTo", relativeTo)));
            }

            // ---- Rounding and totals (rows D5 to D12) ----------------------------------------------------------------
            case "Temporal.Duration.prototype.round, toString":
            {
                var duration = DurationReceiver(at);
                var options = new TemporalAdapter.Bag();
                foreach (var key in new[] { "smallestUnit", "largestUnit", "roundingIncrement", "roundingMode" })
                {
                    if (at.Value() is { } value)
                        options.Set(key, value);
                }

                if (RelativeToValue(at) is { } relative)
                    options.Set("relativeTo", relative);
                return a.Round(duration, options);
            }

            case "Temporal.Duration.prototype.round (string)": return a.Round(DurationReceiver(at), at.Str());
            case "Temporal.Duration.prototype.round (no argument)": return a.Round(DurationReceiver(at), null);
            case "Temporal.Duration.prototype.total (options)":
            {
                var duration = DurationReceiver(at);
                var options = new TemporalAdapter.Bag(("unit", at.Str()));
                if (RelativeToValue(at) is { } relative)
                    options.Set("relativeTo", relative);
                return a.Total(duration, options);
            }

            case "GetTemporalRelativeToOption (string), Duration.prototype.round":
            {
                var duration = a.ToDuration(at.Str());
                return a.Round(duration, new TemporalAdapter.Bag(("smallestUnit", at.Str()), ("relativeTo", at.Str())));
            }

            case "GetTemporalRelativeToOption (string), Duration.prototype.total":
            {
                var duration = a.ToDuration(at.Str());
                return a.Total(duration, new TemporalAdapter.Bag(("unit", at.Str()), ("relativeTo", at.Str())));
            }

            case "Temporal.PlainDate.prototype.until, options not an object":
                return a.Difference(DifferenceOperation.Until, a.ToPlainDate(at.Str()), at.Str(), at.Value());
        }

        if (DifferenceOf(operation) is { } difference)
            return Difference(at, difference.Kind, difference.Operation, difference.Render);

        return Strings(operation, at);
    }

    // The receivers' strings and the string-valued scripts: the grammar, the calendars and their conversions.
    private static object? Strings(string operation, TemporalInputs at)
    {
        var a = Temporal;
        const string Never = "never";
        var always = new TemporalAdapter.Bag(("calendarName", "always"));
        switch (operation)
        {
            // ---- The grammar (grammar.vectors, rows G1 to G23) ------------------------------------------------------
            case "Temporal.PlainDate.from (string), toString": return a.ToPlainDate(at.Str());
            case "Temporal.PlainDateTime.from (string), toString": return a.ToPlainDateTime(at.Str());
            case "Temporal.PlainTime.from (string), toString": return a.ToPlainTime(at.Str());
            case "Temporal.PlainYearMonth.from (string), toString": return a.ToPlainYearMonth(at.Str());
            case "Temporal.PlainMonthDay.from (string), toString": return a.ToPlainMonthDay(at.Str());
            case "Temporal.ZonedDateTime.from (string), toString": return a.ToZonedDateTime(at.Str());
            case "Temporal.ZonedDateTime.from (string, offset), toString": return a.ToZonedDateTime(at.Str(), new TemporalAdapter.Bag(("offset", at.Str())));
            case "Temporal.Instant.from (string), toString": return a.ToInstant(at.Str());
            case "Temporal.Duration.from (string), toString": return a.ToDuration(at.Str());
            case "Temporal.Duration.from (string), time fields":
            {
                var d = a.ToDuration(at.Str()).Fields;
                return string.Join(",", Js(d.Hours), Js(d.Minutes), Js(d.Seconds), Js(d.Milliseconds), Js(d.Microseconds), Js(d.Nanoseconds));
            }

            case "Temporal.PlainDate.from (string), calendarId": return CalendarName(a.ToPlainDate(at.Str()).Calendar);
            case "ToTemporalCalendarIdentifier (string), a property bag's calendar":
                return CalendarName(a.ToPlainDate(new TemporalAdapter.Bag(("year", 2020.0), ("monthCode", "M01"), ("day", 1.0), ("calendar", at.Str()))).Calendar);
            case "ToTemporalCalendarIdentifier (string), withCalendar":
                return CalendarName(TemporalAdapter.WithCalendar(TemporalAdapter.NewPlainDate(2020.0, 1.0, 1.0), at.Str()).Calendar);
            case "CanonicalizeCalendar, the constructor's calendar": return CalendarName(TemporalAdapter.NewPlainDate(2020.0, 1.0, 1.0, at.Str()).Calendar);
            case "ToTemporalTimeZoneIdentifier (string), toZonedDateTimeISO":
                return a.ToZonedDateTimeIso(a.ToInstant("2020-01-01T00:00Z"), at.Str()).TimeZone;
            case "ToTemporalTimeZoneIdentifier (string), a property bag's timeZone":
                return a.ToZonedDateTime(new TemporalAdapter.Bag(("year", 2020.0), ("month", 1.0), ("day", 1.0), ("timeZone", at.Str()))).TimeZone;
            case "ToOffsetString, ZonedDateTime.prototype.with":
            {
                var offset = at.Str();
                return a.With(a.ToZonedDateTime("2020-01-01T00:00[UTC]"), new TemporalAdapter.Bag(("offset", offset)), new TemporalAdapter.Bag(("offset", at.Str())));
            }

            case "ToOffsetString, ZonedDateTime.prototype.with, not a string":
                return a.With(a.ToZonedDateTime("2020-01-01T00:00[UTC]"), new TemporalAdapter.Bag(("offset", at.Value())), null);
            case "ToOffsetString, a property bag's offset":
            {
                var offset = at.Str();
                return a.ToZonedDateTime(
                    new TemporalAdapter.Bag(("year", 2020.0), ("month", 1.0), ("day", 1.0), ("offset", offset), ("timeZone", "UTC")),
                    new TemporalAdapter.Bag(("offset", at.Str())));
            }

            // ---- The calendars (calendars.vectors, rows C1 to C18) ---------------------------------------------------
            case "Temporal.PlainDate.from (property bag), calendar fields":
            case "Temporal.PlainDate.from (property bag, options), calendar fields":
            {
                var d = a.ToPlainDate(at.Bag(Keys.Fields), operation.Contains("options", StringComparison.Ordinal) ? at.Bag(Keys.Options) : null);
                var f = a.CalendarFieldsOf(d.Date, d.Calendar);
                return Js(f.Year) + "/" + f.MonthCode + "/" + Js(f.Day) + ";" + Js(f.Month) + ";" + a.ToString(d, new TemporalAdapter.Bag(("calendarName", Never)));
            }

            case "Temporal.PlainDate.from (property bag), era fields":
            {
                var d = a.ToPlainDate(at.Bag(Keys.Fields));
                var f = a.CalendarFieldsOf(d.Date, d.Calendar);
                return (f.Era ?? "undefined") + ";" + Js(f.EraYear) + ";" + Js(f.Year) + ";" + a.ToString(d, new TemporalAdapter.Bag(("calendarName", Never)));
            }

            case "Temporal.PlainYearMonth.from (property bag, options), calendar fields":
            {
                var ym = a.ToPlainYearMonth(at.Bag(Keys.Fields), at.Bag(Keys.Options));
                var f = a.CalendarFieldsOf(ym.Date, ym.Calendar);
                return Js(f.Year) + "/" + f.MonthCode + ";" + a.Render(ym);
            }

            case "Temporal.PlainMonthDay.from (property bag), reference date":
            case "Temporal.PlainMonthDay.from (property bag, options), reference date":
            {
                var md = a.ToPlainMonthDay(at.Bag(Keys.Fields), operation.Contains("options", StringComparison.Ordinal) ? at.Bag(Keys.Options) : null);
                var f = a.CalendarFieldsOf(md.Date, md.Calendar);
                return f.MonthCode + "/" + Js(f.Day) + ";" + a.ToString(md, new TemporalAdapter.Bag(("calendarName", Never)));
            }

            case "Temporal.PlainDate.prototype.withCalendar, era fields":
            {
                var d = TemporalAdapter.WithCalendar(a.ToPlainDate(at.Str()), at.Str());
                var f = a.CalendarFieldsOf(d.Date, d.Calendar);
                return (f.Era ?? "undefined") + ";" + Js(f.EraYear) + ";" + Js(f.Year) + ";" + f.MonthCode + ";" + Js(f.Day);
            }

            case "Temporal.PlainDate.prototype.withCalendar, every field":
            {
                var d = TemporalAdapter.WithCalendar(a.ToPlainDate(at.Str()), at.Str());
                var f = a.CalendarFieldsOf(d.Date, d.Calendar);
                string[] fields =
                [
                    f.Era ?? string.Empty, f.EraYear is { } eraYear ? Js(eraYear) : string.Empty, Js(f.Year), Js(f.Month), f.MonthCode.ToString(), Js(f.Day),
                    Js(f.DayOfYear), Js(f.DaysInMonth), Js(f.DaysInYear), Js(f.MonthsInYear), f.InLeapYear ? "true" : "false",
                    f.WeekOfYear is { } week ? Js(week) : string.Empty, f.YearOfWeek is { } weekYear ? Js(weekYear) : string.Empty,
                ];
                return string.Join(";", fields);
            }

            case "CalendarISOToDate, the month codes of a year":
            {
                var calendar = at.Str();
                var year = at.Number();
                var codes = new List<string>();
                var months = MonthOf(calendar, year, 1).MonthsInYear;
                for (var m = 1; m <= months; m++)
                    codes.Add(MonthOf(calendar, year, m).MonthCode.ToString());
                return string.Join(",", codes);
            }

            case "CalendarISOToDate, a Hebrew year's months":
            {
                var calendar = at.Str();
                var year = at.Number();
                var first = a.ToPlainDate(MonthBag(calendar, year, 1));
                var f = a.CalendarFieldsOf(first.Date, first.Calendar);
                var lengths = new List<string>();
                for (var m = 1; m <= f.MonthsInYear; m++)
                    lengths.Add(Js(MonthOf(calendar, year, m).DaysInMonth));
                return Js(f.DaysInYear) + ";" + a.ToString(first, new TemporalAdapter.Bag(("calendarName", Never))) + ";" + string.Join(",", lengths);
            }

            case "CalendarISOToDate, a year's structure":
            {
                var calendar = at.Str();
                var year = at.Number();
                var first = a.ToPlainDate(MonthBag(calendar, year, 1));
                var f = a.CalendarFieldsOf(first.Date, first.Calendar);
                var lengths = new StringBuilder();
                var leap = new List<string>();
                for (var m = 1; m <= f.MonthsInYear; m++)
                {
                    var month = MonthOf(calendar, year, m);
                    lengths.Append(month.DaysInMonth switch { 30 => "l", 29 => "s", var n => Js(n) });
                    var code = month.MonthCode.ToString();
                    if (code.Length == 4)
                        leap.Add(code + "@" + Js(m));
                }

                return a.ToString(first, new TemporalAdapter.Bag(("calendarName", Never))) + ";" + Js(f.DaysInYear) + ";" + Js(f.MonthsInYear) + ";" +
                    lengths + ";" + string.Join(",", leap);
            }

            case "CalendarDateToISO, the round trip of a date's fields":
            case "CalendarDateToISO, a day past the ISO limit":
            {
                var iso = at.Str();
                var calendar = at.Str();
                var d = TemporalAdapter.WithCalendar(a.ToPlainDate(iso), calendar);
                var f = a.CalendarFieldsOf(d.Date, d.Calendar);
                var past = operation.EndsWith("limit", StringComparison.Ordinal);
                var step = iso.StartsWith('+') ? 1 : -1;
                var bag = new TemporalAdapter.Bag(("calendar", calendar), ("year", (double)f.Year), ("monthCode", f.MonthCode.ToString()), ("day", (double)(f.Day + (past ? step : 0))));
                return a.ToPlainDate(bag, past ? new TemporalAdapter.Bag(("overflow", "reject")) : null);
            }

            case "Temporal.PlainDate.prototype.add and until across a table's seam":
            {
                var d = TemporalAdapter.WithCalendar(a.ToPlainDate(at.Str()), at.Str());
                var e = a.Add(d, at.Bag(Keys.Duration), null, subtract: false);
                var f = a.CalendarFieldsOf(e.Date, e.Calendar);
                var until = a.Difference(DifferenceOperation.Until, d, e, new TemporalAdapter.Bag(("largestUnit", "months")));
                return Js(f.Year) + "/" + f.MonthCode + "/" + Js(f.Day) + ";" + a.ToString(e, new TemporalAdapter.Bag(("calendarName", Never))) +
                    " until months " + a.Render(until);
            }

            // Rows C15 to C17: a date of 2023-03-15 in each calendar, converted.
            case "Temporal.PlainDate.prototype.toPlainYearMonth":
                return a.ToString(a.DateToPlainYearMonth(Base(at)), always);
            case "Temporal.PlainDate.prototype.toPlainMonthDay":
            {
                var md = a.DateToPlainMonthDay(Base(at));
                var f = a.CalendarFieldsOf(md.Date, md.Calendar);
                return CalendarName(md.Calendar) + ";" + f.MonthCode + "/" + Js(f.Day) + ";" + a.ToString(md, always);
            }

            case "Temporal.PlainDateTime.prototype.toPlainDate, toPlainYearMonth":
            {
                var dateTime = a.ToPlainDateTime(at.Str() + "T12:00") with { Calendar = TemporalAdapter.ToTemporalCalendarIdentifier(at.Str()) };
                return a.ToString(a.DateToPlainYearMonth(a.ToPlainDate(dateTime)), always);
            }

            case "Temporal.ZonedDateTime.prototype.toPlainDate, toPlainMonthDay":
            {
                var zoned = a.ToZonedDateTime(at.Str() + "T12:00[UTC]") with { Calendar = TemporalAdapter.ToTemporalCalendarIdentifier(at.Str()) };
                return a.ToString(a.DateToPlainMonthDay(a.ZonedToPlainDate(zoned)), always);
            }

            case "Temporal.PlainMonthDay.prototype.toPlainDate, the calendar year of 2001-06-01":
            {
                at.Str();
                var calendar = at.Str();
                var md = a.ToPlainMonthDay(new TemporalAdapter.Bag(("calendar", calendar), ("monthCode", "M02"), ("day", 10.0)));
                var reference = TemporalAdapter.WithCalendar(a.ToPlainDate("2001-06-01"), calendar);
                var year = (double)a.CalendarFieldsOf(reference.Date, reference.Calendar).Year;
                return a.ToString(a.MonthDayToPlainDate(md, new TemporalAdapter.Bag(("year", year))), always);
            }

            case "Temporal.PlainYearMonth.prototype.toPlainDate":
                return a.ToString(a.YearMonthToPlainDate(a.DateToPlainYearMonth(Base(at)), new TemporalAdapter.Bag(("day", 10.0))), always);
            case "Temporal.PlainYearMonth.from (fields), the reference day":
            {
                var d = Base(at);
                var f = a.CalendarFieldsOf(d.Date, d.Calendar);
                var bag = new TemporalAdapter.Bag(("calendar", CalendarName(d.Calendar)), ("year", (double)f.Year), ("monthCode", f.MonthCode.ToString()));
                return a.ToString(a.ToPlainYearMonth(bag), always);
            }

            case "Temporal.PlainYearMonth.prototype.with":
                return a.ToString(a.With(a.DateToPlainYearMonth(Base(at)), new TemporalAdapter.Bag(("monthCode", "M01")), null), always);
            case "Temporal.PlainMonthDay.prototype.with":
                return a.ToString(a.With(a.DateToPlainMonthDay(Base(at)), new TemporalAdapter.Bag(("day", 1.0)), null), always);
        }

        return NotTemporal.Value;
    }

    // ---- The differences (differences.vectors, rows F1 to F8) ---------------------------------------------------

    private sealed record DifferenceOp(string Kind, DifferenceOperation Operation, string Render);

    // "Temporal.<Kind>.prototype.<until|since>, <render>".
    private static DifferenceOp? DifferenceOf(string operation)
    {
        if (!operation.StartsWith("Temporal.", StringComparison.Ordinal))
            return null;

        var prototype = operation.IndexOf(".prototype.", StringComparison.Ordinal);
        var comma = operation.IndexOf(", ", StringComparison.Ordinal);
        if (prototype < 0 || comma < prototype)
            return null;

        var name = operation[(prototype + ".prototype.".Length)..comma];
        if (name is not ("until" or "since"))
            return null;

        return new DifferenceOp(operation["Temporal.".Length..prototype], name == "until" ? DifferenceOperation.Until : DifferenceOperation.Since, operation[(comma + 2)..]);
    }

    // a.until(b, options) or since: the two receivers, then the four options' slots largestUnit, smallestUnit,
    // roundingIncrement and roundingMode (undefined where absent; no options at all where every slot is).
    private static object? Difference(TemporalInputs at, string kind, DifferenceOperation operation, string render)
    {
        var a = Temporal;
        var receivers = at.Remaining - 4;
        object one, two;
        switch (kind)
        {
            case "PlainDate":
                one = DateReceiver(at, receivers == 4 ? 2 : 1);
                two = DateReceiver(at, receivers == 3 || receivers == 4 ? 2 : 1);
                break;
            case "PlainDateTime":
                one = a.ToPlainDateTime(at.Str());
                two = at.Str();
                break;
            case "PlainTime":
                one = a.ToPlainTime(at.Str());
                two = at.Str();
                break;
            case "Instant":
                one = a.ToInstant(at.Str());
                two = at.Str();
                break;
            case "ZonedDateTime":
                one = a.ToZonedDateTime(at.Str());
                two = at.Str();
                break;
            case "PlainYearMonth":
                one = YearMonthReceiver(at, receivers == 6);
                two = YearMonthReceiver(at, receivers == 6);
                break;
            default:
                throw new InvalidDataException("no difference of a " + kind);
        }

        var options = new TemporalAdapter.Bag();
        var any = false;
        foreach (var key in new[] { "largestUnit", "smallestUnit", "roundingIncrement", "roundingMode" })
        {
            if (at.Value() is { } value)
            {
                options.Set(key, value);
                any = true;
            }
        }

        var optionsValue = any ? options : null;
        var result = one switch
        {
            TemporalAdapter.PlainDate date => a.Difference(operation, date, two, optionsValue),
            TemporalAdapter.PlainDateTime dateTime => a.Difference(operation, dateTime, two, optionsValue),
            TemporalAdapter.PlainTime time => a.Difference(operation, time, two, optionsValue),
            TemporalAdapter.Instant instant => a.Difference(operation, instant, two, optionsValue),
            TemporalAdapter.ZonedDateTime zoned => a.Difference(operation, zoned, two, optionsValue),
            TemporalAdapter.PlainYearMonth yearMonth => a.Difference(operation, yearMonth, two, optionsValue),
            _ => throw new InvalidDataException("no difference of a " + kind),
        };

        return render switch
        {
            "toString" => result,
            "microseconds" => result.Fields.Microseconds,
            _ => throw new InvalidDataException("no rendering " + render),
        };
    }

    // A PlainDate receiver: Temporal.PlainDate.from(string), or .withCalendar(calendar) of one (two inputs).
    private static TemporalAdapter.PlainDate DateReceiver(TemporalInputs at, int inputs)
    {
        var date = Temporal.ToPlainDate(at.Str());
        return inputs == 2 ? TemporalAdapter.WithCalendar(date, at.Str()) : date;
    }

    // A PlainYearMonth receiver: from a string, or Temporal.PlainYearMonth.from({year, monthCode, calendar}) given
    // as the calendar, the year and the month code.
    private static TemporalAdapter.PlainYearMonth YearMonthReceiver(TemporalInputs at, bool fields)
    {
        if (!fields)
            return Temporal.ToPlainYearMonth(at.Str());

        var calendar = at.Str();
        var year = at.Number();
        return Temporal.ToPlainYearMonth(new TemporalAdapter.Bag(("year", year), ("monthCode", at.Str()), ("calendar", calendar)));
    }

    // ---- Decoding helpers ---------------------------------------------------------------------------------------

    private static bool Subtract(string operation) => operation.Contains(".prototype.subtract", StringComparison.Ordinal);

    // A ZonedDateTime receiver: from a string, or new Temporal.ZonedDateTime(epochNanoseconds, zone).
    private static TemporalAdapter.ZonedDateTime Zoned(TemporalInputs at) =>
        at.Peek().Kind == TypedValueKind.BigInt ? Temporal.NewZonedDateTime(at.Value(), at.Value()) : Temporal.ToZonedDateTime(at.Str());

    // A Duration receiver: Temporal.Duration.from of a string or of a bag of duration fields.
    private static TemporalAdapter.Duration DurationReceiver(TemporalInputs at) =>
        at.Peek().Kind == TypedValueKind.String && !IsKey(Keys.Duration, at.Peek().Chars)
            ? Temporal.ToDuration(at.Str())
            : Temporal.ToDuration(at.Bag(Keys.Duration));

    // The base date of rows C15 to C17: Temporal.PlainDate.from(iso).withCalendar(calendar).
    private static TemporalAdapter.PlainDate Base(TemporalInputs at) => TemporalAdapter.WithCalendar(Temporal.ToPlainDate(at.Str()), at.Str());

    // A relativeTo as the vector gives it, the rest of the inputs: undefined; a string (a date or zoned string, or
    // the string a PlainDate.from or ZonedDateTime.from reads, which resolves alike); new PlainDate(y, m, d); a
    // PlainDate.from(string).withCalendar(calendar); PlainDate.from({year, monthCode, day, calendar}) as the calendar,
    // the year, the month code and the day; or a property bag.
    private static object? RelativeToValue(TemporalInputs at)
    {
        var a = Temporal;
        switch (at.Remaining)
        {
            case 1:
                return at.Value() switch
                {
                    null => null,
                    string text => text,
                    var other => other,
                };
            case 2:
                return TemporalAdapter.WithCalendar(a.ToPlainDate(at.Str()), at.Str());
            case 3:
                return TemporalAdapter.NewPlainDate(at.Value(), at.Value(), at.Value());
            case 4 when at.Peek().Kind == TypedValueKind.String && !IsKey(Keys.Fields, at.Peek().Chars):
            {
                var calendar = at.Str();
                var year = at.Number();
                var code = at.Str();
                return a.ToPlainDate(new TemporalAdapter.Bag(("year", year), ("monthCode", code), ("day", at.Number()), ("calendar", calendar)));
            }

            default:
                return at.Bag(Keys.Fields);
        }
    }

    // The bag of a calendar's year's month, the first day.
    private static TemporalAdapter.Bag MonthBag(string calendar, double year, int month) =>
        new(("calendar", calendar), ("year", year), ("month", (double)month), ("day", 1.0));

    private static CalendarDate MonthOf(string calendar, double year, int month)
    {
        var date = Temporal.ToPlainDate(MonthBag(calendar, year, month));
        return Temporal.CalendarFieldsOf(date.Date, date.Calendar);
    }

    private static string CalendarName(CalendarId calendar) => TemporalCalendars.Identifier(calendar);

    private static bool IsPositiveZero(double value) => value == 0 && !double.IsNegative(value);

    private static string Js(double value) => Broiler.ECMAScript.Numbers.NumberText.ToString(value);

    private static string Js(long value) => value.ToString(NumberFormatInfo.InvariantInfo);

    private static string Js(long? value) => value is { } v ? Js(v) : "undefined";

    /// <summary>A vector's inputs read in order.</summary>
    private sealed class TemporalInputs(IReadOnlyList<TypedValue> inputs)
    {
        private int at;

        internal bool AtEnd => at == inputs.Count;

        internal int Remaining => inputs.Count - at;

        internal TypedValue Peek() => at < inputs.Count ? inputs[at] : throw new InvalidDataException("the inputs end early");

        /// <summary>The next input as the adapter's JavaScript value.</summary>
        internal object? Value()
        {
            var value = Peek();
            at++;
            return value.Kind switch
            {
                TypedValueKind.Undefined => null,
                TypedValueKind.Null => TemporalAdapter.JsNull.Value,
                TypedValueKind.Boolean => value.Boolean,
                TypedValueKind.Number => value.Number,
                TypedValueKind.Integer => (double)value.Integer,
                TypedValueKind.String => value.Chars,
                TypedValueKind.BigInt => value.Integer,
                _ => throw new InvalidDataException("no Temporal vector gives a " + value.Kind),
            };
        }

        internal string Str() => Value() as string ?? throw new InvalidDataException("not a string input");

        internal double Number() => Value() is double number ? number : throw new InvalidDataException("not a number input");

        /// <summary>The rest of the inputs, as a call's arguments.</summary>
        internal object?[] Rest()
        {
            var values = new List<object?>();
            while (!AtEnd)
                values.Add(Value());
            return [.. values];
        }

        /// <summary>The next input, or undefined where the inputs end.</summary>
        internal object? Optional() => AtEnd ? null : Value();

        /// <summary>A property bag: its keys and values while the next input is one of the kind's keys.</summary>
        internal TemporalAdapter.Bag Bag(Keys keys)
        {
            var bag = new TemporalAdapter.Bag();
            while (Remaining >= 2 && Peek().Kind == TypedValueKind.String && IsKey(keys, Peek().Chars))
            {
                var key = Str();
                bag.Set(key, Value());
            }

            return bag;
        }

        /// <summary>An options argument where one follows: a bag, or null.</summary>
        internal object? OptionalBag()
        {
            if (AtEnd)
                return null;
            if (Peek().Kind == TypedValueKind.Null)
                return Value();

            return Bag(Keys.Options);
        }

        /// <summary>A duration argument: a duration string, a bag of duration fields, or the empty bag the generator writes as "{}".</summary>
        internal object? DurationArgument()
        {
            if (Peek().Kind == TypedValueKind.String && !IsKey(Keys.Duration, Peek().Chars))
            {
                var text = Str();
                return text == "{}" ? new TemporalAdapter.Bag() : text;
            }

            return Bag(Keys.Duration);
        }

        /// <summary>
        /// Two adjacent bags of duration fields. The generator writes a bag's fields from the largest unit down, so a
        /// field that is not smaller than the one before starts the second bag; two fields with no such break are a
        /// field each.
        /// </summary>
        internal (TemporalAdapter.Bag One, TemporalAdapter.Bag Two) TwoDurationBags()
        {
            var start = at;
            var pairs = new List<(string Key, object? Value)>();
            while (Remaining >= 2 && Peek().Kind == TypedValueKind.String && IsKey(Keys.Duration, Peek().Chars))
                pairs.Add((Str(), Value()));

            var split = -1;
            for (var i = 1; i < pairs.Count && split < 0; i++)
            {
                if (DurationRank(pairs[i].Key) <= DurationRank(pairs[i - 1].Key))
                    split = i;
            }

            if (split < 0)
                split = pairs.Count == 2 ? 1 : throw new InvalidDataException("two duration bags cannot be told apart at input " + start);

            var one = new TemporalAdapter.Bag();
            var two = new TemporalAdapter.Bag();
            for (var i = 0; i < pairs.Count; i++)
                (i < split ? one : two).Set(pairs[i].Key, pairs[i].Value);

            return (one, two);
        }

        /// <summary>Duration.prototype.add's second argument, which it never reads: an options bag, a Number or null.</summary>
        internal void SecondArgument()
        {
            if (Peek().Kind == TypedValueKind.String)
                Bag(Keys.Options);
            else
                Value();
        }

        /// <summary>That every input was read.</summary>
        internal void End(string operation)
        {
            if (!AtEnd)
                throw new InvalidDataException(operation + ": " + Remaining + " inputs were not read");
        }
    }

    /// <summary>
    /// The data home's calendar year table, read from disk and held to the length and SHA-256 its manifest records,
    /// as an engine decodes the table it embeds.
    /// </summary>
    private static TemporalCalendars LoadCalendars()
    {
        var generated = TimeZoneTablesDirectory(AppContext.BaseDirectory);
        foreach (var line in File.ReadAllLines(Path.Combine(generated, "tables.manifest")))
        {
            var fields = line.Split(' ');
            if (fields.Length != 4 || fields[0] != "table" || fields[1] != "calendars.bin")
                continue;

            var bytes = File.ReadAllBytes(Path.Combine(generated, fields[1]));
            if (bytes.Length != long.Parse(fields[2], NumberStyles.None, NumberFormatInfo.InvariantInfo)
                || fields[3] != Convert.ToHexStringLower(SHA256.HashData(bytes)))
                throw new InvalidDataException("calendars.bin is not the table its manifest records");

            return new TemporalCalendars(new CalendarTables(bytes));
        }

        throw new InvalidDataException("the tables' manifest records no calendars.bin");
    }
}
