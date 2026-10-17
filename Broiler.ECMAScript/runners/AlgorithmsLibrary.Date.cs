using System;
using System.Collections.Generic;
using System.Globalization;
using Broiler.ECMAScript.Conformance;
using Broiler.ECMAScript.Time;

namespace Broiler.ECMAScript.Runners;

/// <summary>
/// The Date area (corpus revision 14, with its strings outside the format at revision 16): each operation as an engine's adapter calls the library, with the
/// script's own reading of the result where a vector's script shows it through a built-in. A string outside the format is read as both
/// engines' adapters ask the library to read it, in the strict dialect and then, for what that refuses, in Broiler.VM's (since corpus
/// revision 17, where both engines' expectations of two such strings agree on that dialect's reading).
/// </summary>
/// <remarks>
/// <para>
/// What a vector's script does around the shared steps is written here once more, as each engine's
/// adapter does it: <c>Date.UTC</c>'s <c>MakeFullYear</c> and composition, a setter's reading of the
/// fields it keeps, a getter's join of the local fields, <c>toJSON</c>'s <c>null</c>, and
/// <c>PlainMonthDay.from</c>'s regulation. None of it is arithmetic, a grammar or a rendering; every such
/// step is the library's. <b>Local time is a zone at offset zero named UTC</b>, as Broiler.VM's is: no
/// Date vector's outcome depends on the zone, and a library run has none of its own.
/// </para>
/// <para>
/// <b>The engine-level vectors are not answered</b>: those whose outcome is the setters' argument
/// conversion and its order, and those whose input is a sequence of local setter calls.
/// </para>
/// </remarks>
internal sealed partial class AlgorithmsLibrary
{
    private static readonly FixedOffsetLocalTimeProvider DateZone = new(0, "Coordinated Universal Time");

    /// <summary>Whether an operation of the Date area is the engine's, not the library's, so that its vectors are engine-level only.</summary>
    internal static bool IsEngineLevelDate(string operation) =>
        operation == "local setters, read back"
        || (operation.StartsWith("Date.prototype.set", StringComparison.Ordinal)
            && (operation.EndsWith(", order", StringComparison.Ordinal) || operation.EndsWith(", conversions on an invalid date", StringComparison.Ordinal)));

    private static string DateArea(string operation, IReadOnlyList<TypedValue> inputs)
    {
        if (IsEngineLevelDate(operation))
            return EmptyLibrary.NotImplemented;

        switch (operation)
        {
            // Date.UTC and the constructor, which these vectors' scripts call.
            case "MakeTime": return Number(DateUtc(1970, 0, 1, inputs[0].Number, inputs[1].Number, inputs[2].Number, inputs[3].Number));
            case "MakeFullYear": return Number(DateUtc(inputs[0].Number, 0, 1, 0, 0, 0, 0));
            case "MakeDay": return Number(SetUtcFullYear(0, inputs[0].Number, inputs[1].Number, inputs[2].Number));
            case "TimeClip": return Number(EcmaTime.TimeClip(inputs[0].Number));

            // The UTC setters, each reading the fields it keeps from the time value first.
            case "Date.prototype.setUTCHours": return Number(SetUtcHours(inputs));
            case "Date.prototype.setUTCDate": return Number(SetUtcDate(inputs[0].Number, inputs[1].Number));
            case "Date.prototype.setUTCMonth": return Number(SetUtcMonth(inputs));
            case "Date.prototype.setUTCFullYear":
                return Number(SetUtcFullYear(inputs[0].Number, inputs[1].Number, Optional(inputs, 2), Optional(inputs, 3)));
            case "Date.prototype.setUTCMilliseconds": return Number(SetUtcMilliseconds(inputs[0].Number, inputs[1].Number));

            // The UTC getters.
            case "YearFromTime": return Number(EcmaTime.YearFromTime(inputs[0].Number));
            case "MonthFromTime": return Number(EcmaTime.MonthFromTime(inputs[0].Number));
            case "DateFromTime": return Number(EcmaTime.DateFromTime(inputs[0].Number));
            case "WeekDay": return Number(EcmaTime.WeekDay(inputs[0].Number));
            case "HourFromTime": return Number(EcmaTime.HourFromTime(inputs[0].Number));
            case "MinFromTime": return Number(EcmaTime.MinFromTime(inputs[0].Number));
            case "SecFromTime": return Number(EcmaTime.SecFromTime(inputs[0].Number));
            case "msFromTime": return Number(EcmaTime.MsFromTime(inputs[0].Number));

            // Date.parse and the renderings it reads back.
            case "Date.parse, the Date Time String Format": return Number(DateParse(inputs[0].Chars, DateParseDialect.Strict));
            case "Date.parse, a date-time form without an offset":
                return Text(LocalFields(DateParse(inputs[0].Chars, DateParseDialect.Strict), 7));
            case "Date.parse, a numeric slash date": return Text(LocalFields(DateParse(inputs[0].Chars, DateParseDialect.Strict), 5));
            case "Date.parse, a string outside the format": return Number(DateParseOutsideFormat(inputs[0].Chars));
            case "Date.parse, a string outside the format, its local fields":
                return Text(LocalFields(DateParseOutsideFormat(inputs[0].Chars), 5));
            case "Date.parse of toISOString":
                return Number(EcmaDateFormatter.TryToIsoString(inputs[0].Number, out var iso) ? DateParse(iso, DateParseDialect.Strict) : double.NaN);
            case "Date.parse of toUTCString": return Number(DateParse(EcmaDateFormatter.ToUtcString(inputs[0].Number), DateParseDialect.BroilerVM));
            case "Date.parse of toString":
                return Number(DateParse(EcmaDateFormatter.ToDateString(inputs[0].Number, DateZone), DateParseDialect.BroilerVM));
            case "Date.parse, a date-only form is UTC":
                return Boolean(DateParse(inputs[0].Chars, DateParseDialect.Strict) == UtcOfLocalFields(DateParse(inputs[0].Chars, DateParseDialect.Strict)));
            case "Date.parse, a date-time form is local time":
                return Boolean(DateParse(inputs[0].Chars, DateParseDialect.Strict) == ConstructedFromLocalFields(DateParse(inputs[0].Chars, DateParseDialect.Strict)));

            // The renderings.
            case "Date.prototype.toUTCString":
            case "Date.prototype.toUTCString, an invalid date": return Text(EcmaDateFormatter.ToUtcString(inputs[0].Number));
            case "Date.prototype.toString, an invalid date": return Text(EcmaDateFormatter.ToDateString(inputs[0].Number, DateZone));
            case "Date.prototype.toDateString, an invalid date":
            case "Date.prototype.toTimeString, an invalid date": return Text(EcmaDateFormatter.InvalidDate);
            case "Date.prototype.toISOString":
            case "Date.prototype.toISOString, an invalid date":
                return EcmaDateFormatter.TryToIsoString(inputs[0].Number, out iso) ? Text(iso) : "error: RangeError";
            case "Date.prototype.toJSON":
            case "Date.prototype.toJSON, an invalid date":
                return EcmaDateFormatter.TryToIsoString(inputs[0].Number, out iso) ? Text(iso) : "value: null";
            case "Date.prototype.toDateString, a local date": return Text(EcmaDateFormatter.DateString(LocalDate(inputs)));
            case "Date.prototype.toString, a local date":
                return Text(EcmaDateFormatter.DateString(LocalDate(inputs)) + " " + ClockOf(LocalDate(inputs)));
            case "Date.prototype.toTimeString, a local date": return Text(ClockOf(LocalDate(inputs)));
            case "Date.prototype.toString, the zone's form":
                return Boolean(EndsWithZoneForm(EcmaDateFormatter.ToDateString(Constructed(inputs[0].Chars), DateZone)));
            case "Date.prototype.toTimeString, the zone's form":
            {
                var tv = Constructed(inputs[0].Chars);
                var local = EcmaLocalTime.LocalTime(tv, DateZone);
                return Boolean(EndsWithZoneForm(EcmaDateFormatter.TimeString(local) + EcmaDateFormatter.TimeZoneString(tv, DateZone)));
            }

            // Local time, through the relations its vectors test.
            case "LocalTime and getTimezoneOffset agree": return Boolean(OffsetAgrees(inputs[0].Number));
            case "toString and the local fields agree": return Boolean(ToStringAgrees(inputs[0].Number));

            // The ISO calendar, through Temporal.PlainDate and Temporal.PlainMonthDay.
            case "Temporal.PlainDate.prototype.weekOfYear": return Number(IsoCalendarMath.WeekOfYear(Year(inputs), Month(inputs), Day(inputs)).Week);
            case "Temporal.PlainDate.prototype.yearOfWeek": return Number(IsoCalendarMath.WeekOfYear(Year(inputs), Month(inputs), Day(inputs)).Year);
            case "Temporal.PlainDate.prototype.dayOfWeek": return Number(IsoCalendarMath.DayOfWeek(Year(inputs), Month(inputs), Day(inputs)));
            case "Temporal.PlainDate.prototype.dayOfYear": return Number(IsoCalendarMath.DayOfYear(Year(inputs), Month(inputs), Day(inputs)));
            case "Temporal.PlainDate.prototype.daysInMonth": return Number(IsoCalendarMath.DaysInMonth(Year(inputs), Month(inputs)));
            case "Temporal.PlainDate.prototype.daysInYear": return Number(IsoCalendarMath.DaysInYear(Year(inputs)));
            case "Temporal.PlainDate.prototype.inLeapYear": return Boolean(IsoCalendarMath.IsLeapYear(Year(inputs)));
            case "Temporal.PlainMonthDay.from, a year's leap day": return PlainMonthDayFrom(inputs);
            case "Temporal.PlainDate.prototype.add, days":
            {
                var epochDays = IsoCalendarMath.EpochDays(2000, 1, 1) + (long)inputs[0].Integer;
                if (!IsoDateWithinLimits(epochDays))
                    return "error: RangeError";

                var (year, month, day) = IsoCalendarMath.FromEpochDays(epochDays);
                return Text(IsoDateText(year, month, day));
            }

            case "Temporal.PlainDate.prototype.until, days":
                return Number(IsoCalendarMath.EpochDays((long)inputs[3].Integer, (int)inputs[4].Integer, (int)inputs[5].Integer)
                    - IsoCalendarMath.EpochDays((long)inputs[0].Integer, (int)inputs[1].Integer, (int)inputs[2].Integer));
        }

        return TimeZoneArea(operation, inputs);
    }

    // ---------------------------------------------------------------------------------------------
    // Date.UTC, the constructor and the setters: argument defaults and composition around the library.

    private static double DateUtc(double year, double month, double date, double hour, double minute, double second, double ms) =>
        EcmaTime.TimeClip(EcmaTime.MakeDate(EcmaTime.MakeDay(EcmaTime.MakeFullYear(year), month, date), EcmaTime.MakeTime(hour, minute, second, ms)));

    // An argument the script passes, or null for one it leaves out; a NaN it passes is not left out.
    private static double? Optional(IReadOnlyList<TypedValue> inputs, int at) => at < inputs.Count ? inputs[at].Number : null;

    private static double SetUtcHours(IReadOnlyList<TypedValue> inputs)
    {
        var t = inputs[0].Number;
        var h = inputs[1].Number;
        if (double.IsNaN(t))
            return double.NaN;

        var m = inputs.Count > 2 ? inputs[2].Number : EcmaTime.MinFromTime(t);
        var s = inputs.Count > 3 ? inputs[3].Number : EcmaTime.SecFromTime(t);
        var milli = inputs.Count > 4 ? inputs[4].Number : EcmaTime.MsFromTime(t);
        return EcmaTime.TimeClip(EcmaTime.MakeDate(EcmaTime.Day(t), EcmaTime.MakeTime(h, m, s, milli)));
    }

    private static double SetUtcDate(double t, double dt) =>
        double.IsNaN(t)
            ? double.NaN
            : EcmaTime.TimeClip(EcmaTime.MakeDate(EcmaTime.MakeDay(EcmaTime.YearFromTime(t), EcmaTime.MonthFromTime(t), dt), EcmaTime.TimeWithinDay(t)));

    private static double SetUtcMonth(IReadOnlyList<TypedValue> inputs)
    {
        var t = inputs[0].Number;
        if (double.IsNaN(t))
            return double.NaN;

        var dt = inputs.Count > 2 ? inputs[2].Number : EcmaTime.DateFromTime(t);
        return EcmaTime.TimeClip(EcmaTime.MakeDate(EcmaTime.MakeDay(EcmaTime.YearFromTime(t), inputs[1].Number, dt), EcmaTime.TimeWithinDay(t)));
    }

    private static double SetUtcFullYear(double t, double y, double? m, double? dt)
    {
        // setUTCFullYear reads an invalid date as +0 (21.4.4.29 step 4), and an absent month or date as t's.
        if (double.IsNaN(t))
            t = 0;

        var month = m ?? EcmaTime.MonthFromTime(t);
        var date = dt ?? EcmaTime.DateFromTime(t);
        return EcmaTime.TimeClip(EcmaTime.MakeDate(EcmaTime.MakeDay(y, month, date), EcmaTime.TimeWithinDay(t)));
    }

    private static double SetUtcMilliseconds(double t, double ms) =>
        double.IsNaN(t)
            ? double.NaN
            : EcmaTime.TimeClip(EcmaTime.MakeDate(EcmaTime.Day(t), EcmaTime.MakeTime(EcmaTime.HourFromTime(t), EcmaTime.MinFromTime(t), EcmaTime.SecFromTime(t), ms)));

    // ---------------------------------------------------------------------------------------------
    // Date.parse and the local fields a script reads back.

    private static double DateParse(string text, DateParseDialect dialect)
    {
        var parsed = EcmaDateParser.Parse(text, dialect);
        return parsed.Kind switch
        {
            EcmaParsedKind.Utc => parsed.Value,
            EcmaParsedKind.Local => EcmaTime.TimeClip(EcmaLocalTime.Utc(parsed.Value, DateZone)),
            _ => double.NaN,
        };
    }

    // The library's part of either engine's Date.parse for a string outside the format: the strict dialect, then Broiler.VM's for
    // what it refuses. Broiler.JS's own grammar, between the two, is that engine's, not the library's.
    private static double DateParseOutsideFormat(string text)
    {
        var strict = DateParse(text, DateParseDialect.Strict);
        return double.IsNaN(strict) ? DateParse(text, DateParseDialect.BroilerVM) : strict;
    }

    // [getFullYear(), getMonth(), getDate(), getHours(), getMinutes(), getSeconds(), getMilliseconds()].join(), its first count.
    private static string LocalFields(double tv, int count)
    {
        var local = EcmaLocalTime.LocalTime(tv, DateZone);
        var fields = new[]
        {
            EcmaTime.YearFromTime(local), EcmaTime.MonthFromTime(local), EcmaTime.DateFromTime(local), EcmaTime.HourFromTime(local),
            EcmaTime.MinFromTime(local), EcmaTime.SecFromTime(local), EcmaTime.MsFromTime(local),
        };
        var parts = new string[count];
        for (var i = 0; i < count; i++)
            parts[i] = double.IsNaN(fields[i]) ? "NaN" : ((long)fields[i]).ToString(NumberFormatInfo.InvariantInfo);
        return string.Join(",", parts);
    }

    // Date.UTC of a parsed time's local year, month and date.
    private static double UtcOfLocalFields(double tv)
    {
        var local = EcmaLocalTime.LocalTime(tv, DateZone);
        return DateUtc(EcmaTime.YearFromTime(local), EcmaTime.MonthFromTime(local), EcmaTime.DateFromTime(local), 0, 0, 0, 0);
    }

    // new Date(y, m, d, h, mi) of a parsed time's local fields: the constructor's UTC of its local time.
    private static double ConstructedFromLocalFields(double tv)
    {
        var local = EcmaLocalTime.LocalTime(tv, DateZone);
        return Constructor(EcmaTime.YearFromTime(local), EcmaTime.MonthFromTime(local), EcmaTime.DateFromTime(local),
            EcmaTime.HourFromTime(local), EcmaTime.MinFromTime(local));
    }

    private static double Constructor(double year, double month, double date, double hour, double minute) =>
        EcmaTime.TimeClip(EcmaLocalTime.Utc(
            EcmaTime.MakeDate(EcmaTime.MakeDay(EcmaTime.MakeFullYear(year), month, date), EcmaTime.MakeTime(hour, minute, 0, 0)),
            DateZone));

    // new Date(...) of a vector's comma-separated year, month, date and hour.
    private static double Constructed(string arguments)
    {
        var parts = arguments.Split(',');
        double Part(int at) => at < parts.Length ? double.Parse(parts[at].Trim(), NumberStyles.Float, NumberFormatInfo.InvariantInfo) : 0;
        return EcmaTime.TimeClip(EcmaLocalTime.Utc(
            EcmaTime.MakeDate(EcmaTime.MakeDay(EcmaTime.MakeFullYear(Part(0)), Part(1), parts.Length > 2 ? Part(2) : 1), EcmaTime.MakeTime(Part(3), Part(4), Part(5), Part(6))),
            DateZone));
    }

    // The local date the "a local date" vectors put in: year, month and date set on new Date(2000, 0, 1, 12, 30, 15).
    private static double LocalDate(IReadOnlyList<TypedValue> inputs) =>
        EcmaTime.MakeDate(EcmaTime.MakeDay(inputs[0].Number, inputs[1].Number, inputs[2].Number), EcmaTime.MakeTime(12, 30, 15, 0));

    // TimeString's clock without its " GMT", which the scripts split off.
    private static string ClockOf(double local) => EcmaDateFormatter.TimeString(local)[..8];

    // / GMT[+-][0-9]{4}( \(.+\))?$/, read by hand.
    private static bool EndsWithZoneForm(string text)
    {
        var at = text.LastIndexOf(" GMT", StringComparison.Ordinal);
        if (at < 0 || at + 9 > text.Length || text[at + 4] is not ('+' or '-'))
            return false;
        for (var i = at + 5; i < at + 9; i++)
        {
            if (text[i] is < '0' or > '9')
                return false;
        }

        var rest = text[(at + 9)..];
        return rest.Length == 0 || (rest.StartsWith(" (", StringComparison.Ordinal) && rest.EndsWith(')') && rest.Length > 3);
    }

    // (Date.UTC(local fields) - t) / 60000 === -getTimezoneOffset(), where getTimezoneOffset is (t - LocalTime(t)) / msPerMinute.
    private static bool OffsetAgrees(double tv)
    {
        var local = EcmaLocalTime.LocalTime(tv, DateZone);
        var utcOfFields = DateUtc(EcmaTime.YearFromTime(local), EcmaTime.MonthFromTime(local), EcmaTime.DateFromTime(local),
            EcmaTime.HourFromTime(local), EcmaTime.MinFromTime(local), EcmaTime.SecFromTime(local), EcmaTime.MsFromTime(local));
        var offset = (tv - local) / EcmaTime.MsPerMinute;
        return (utcOfFields - tv) / 60000 == -offset;
    }

    // toString's clock and GMT offset against the local fields and getTimezoneOffset.
    private static bool ToStringAgrees(double tv)
    {
        var text = EcmaDateFormatter.ToDateString(tv, DateZone);
        var at = text.IndexOf(" GMT", StringComparison.Ordinal);
        var local = EcmaLocalTime.LocalTime(tv, DateZone);
        var clock = text.Substring(at - 8, 8);
        var expected = Two(EcmaTime.HourFromTime(local)) + ":" + Two(EcmaTime.MinFromTime(local)) + ":" + Two(EcmaTime.SecFromTime(local));
        var sign = text[at + 4] == '-' ? 1 : -1;
        var minutes = (int.Parse(text.AsSpan(at + 5, 2), NumberStyles.None, NumberFormatInfo.InvariantInfo) * 60)
            + int.Parse(text.AsSpan(at + 7, 2), NumberStyles.None, NumberFormatInfo.InvariantInfo);
        return clock == expected && sign * minutes == (tv - local) / EcmaTime.MsPerMinute;
    }

    private static string Two(double value) => ((long)value).ToString(NumberFormatInfo.InvariantInfo).PadLeft(2, '0');

    // ---------------------------------------------------------------------------------------------
    // Temporal's ISO calendar around the library's civil-day helpers.

    private static long Year(IReadOnlyList<TypedValue> inputs) => (long)inputs[0].Integer;

    private static int Month(IReadOnlyList<TypedValue> inputs) => (int)inputs[1].Integer;

    private static int Day(IReadOnlyList<TypedValue> inputs) => (int)inputs[2].Integer;

    // PlainMonthDay.from({year, month, day}, {overflow}) in the ISO calendar: RegulateISODate in the year given.
    private static string PlainMonthDayFrom(IReadOnlyList<TypedValue> inputs)
    {
        var year = Year(inputs);
        var month = Month(inputs);
        var day = Day(inputs);
        if (inputs[3].Chars == "reject")
        {
            if (!IsoCalendarMath.IsValidIsoDate(year, month, day))
                return "error: RangeError";
        }
        else
        {
            month = Math.Clamp(month, 1, 12);
            day = Math.Clamp(day, 1, IsoCalendarMath.DaysInMonth(year, month));
        }

        return Text(month.ToString("00", NumberFormatInfo.InvariantInfo) + "-" + day.ToString("00", NumberFormatInfo.InvariantInfo));
    }

    // Temporal's ISODateWithinLimits over a date's epoch days: its noon lies within a day of the instants'
    // range, which holds every date from -271821-04-19 to +275760-09-13. The limits stay Temporal's (JSD-0072).
    private static bool IsoDateWithinLimits(long epochDays) => epochDays is >= -100_000_001 and <= 100_000_000;

    private static string IsoDateText(long year, int month, int day)
    {
        var yearText = year is >= 0 and <= 9999
            ? year.ToString("0000", NumberFormatInfo.InvariantInfo)
            : (year < 0 ? "-" : "+") + Math.Abs(year).ToString("000000", NumberFormatInfo.InvariantInfo);
        return yearText + "-" + month.ToString("00", NumberFormatInfo.InvariantInfo) + "-" + day.ToString("00", NumberFormatInfo.InvariantInfo);
    }

    private static string Boolean(bool value) => "value: " + TypedValue.RenderBoolean(value);
}
