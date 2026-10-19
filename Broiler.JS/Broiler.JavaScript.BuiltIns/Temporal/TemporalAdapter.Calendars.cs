using System;
using System.Collections.Generic;
using System.Threading;
using Broiler.ECMAScript.Temporal;
using Broiler.ECMAScript.Time;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Temporal;

// The calendars: the identifiers of the Intl era and month code proposal's Table 1, the formula calendars, the
// table calendars over SJL-8's calendar years, month codes, eras and the month-day reference years are the shared
// library's TemporalCalendars, with the calendar operations of Temporal s12.3 over them (JSD-0078's X6, decision
// D3). What stays here is this engine's: a Temporal object's [[Calendar]] slot, which holds the canonical
// identifier as a string; the calendar argument's coercion; PrepareCalendarFields' property reads and their
// conversions, in the order the specification gives; and the TypeError or RangeError each CalendarStatus is.
internal static partial class TemporalAdapter
{
    private static readonly Lazy<TemporalCalendars> CalendarSystems =
        new(() => new TemporalCalendars(TemporalTimeData.Calendars), LazyThreadSafetyMode.ExecutionAndPublication);

    // Each canonical identifier a [[Calendar]] slot holds, by its string.
    private static readonly Dictionary<string, CalendarId> CanonicalCalendars = CanonicalIdentifiers();

    /// <summary>
    /// The shared library's calendars over the calendar tables this engine embeds (<see cref="TemporalTimeData.Calendars"/>),
    /// made once, on first use; immutable and safe to call concurrently.
    /// </summary>
    internal static TemporalCalendars Calendars => CalendarSystems.Value;

    private static Dictionary<string, CalendarId> CanonicalIdentifiers()
    {
        var identifiers = new Dictionary<string, CalendarId>(StringComparer.Ordinal);
        for (var calendar = CalendarId.Iso8601; calendar <= CalendarId.Roc; calendar++)
            identifiers.Add(TemporalCalendars.Identifier(calendar), calendar);
        return identifiers;
    }

    // ── identifiers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// CanonicalizeCalendar, the shared library's: Table 1's calendar types and its two aliases (<c>ethiopic-amete-alem</c>
    /// and <c>islamicc</c>) under ASCII case folding alone. <c>gregorian</c> and <c>minguo</c> are not calendars (row C2,
    /// decision D5), nor is <c>islamic</c> (row C3).
    /// </summary>
    internal static bool TryCanonicalizeCalendar(string identifier, out string calendarId)
    {
        if (TemporalCalendars.Canonicalize(identifier, out var calendar) == CalendarStatus.Completed)
        {
            calendarId = TemporalCalendars.Identifier(calendar);
            return true;
        }

        calendarId = null;
        return false;
    }

    /// <summary>CanonicalizeCalendar, as <see cref="TryCanonicalizeCalendar"/>; an identifier it does not know is the <c>RangeError</c>.</summary>
    internal static string CanonicalizeCalendar(string identifier) =>
        TryCanonicalizeCalendar(identifier, out var calendarId)
            ? calendarId
            : throw JSEngine.NewRangeError($"Temporal: unsupported calendar \"{identifier}\"");

    /// <summary>The library's identity of the canonical identifier a Temporal object holds.</summary>
    internal static CalendarId CalendarIdOf(string calendarId) =>
        CanonicalCalendars.TryGetValue(calendarId, out var calendar)
            ? calendar
            : throw new InvalidOperationException($"A Temporal object holds a calendar that is not canonical: {calendarId}");

    /// <summary>CalendarSupportsEra: whether <c>era</c> and <c>eraYear</c> are read with <c>year</c> (CalendarExtraFields).</summary>
    internal static bool SupportsEra(string calendarId) => TemporalCalendars.SupportsEra(CalendarIdOf(calendarId));

    /// <summary>
    /// A constructor's calendar argument: undefined is <c>iso8601</c>, a value that is not a String is the
    /// <c>TypeError</c>, and the String is canonicalized as an identifier, never parsed as an ISO string.
    /// </summary>
    internal static string CalendarArgument(JSValue calendar, string typeName)
    {
        if (calendar == null || calendar.IsUndefined)
            return "iso8601";
        if (!calendar.IsString)
            throw JSEngine.NewTypeError($"{typeName}: calendar must be a calendar identifier string");
        return CanonicalizeCalendar(calendar.StringValue);
    }

    /// <summary>
    /// The calendar a Temporal object holds in its <c>[[Calendar]]</c> slot, or <see langword="null"/> for any other
    /// value. A <c>PlainMonthDay</c> lends its own calendar (row C13), as every other type with one does.
    /// </summary>
    internal static string CalendarSlotOf(JSValue value) => value switch
    {
        JSTemporalPlainDate date => date.calendarId,
        JSTemporalPlainDateTime dateTime => dateTime.calendarId,
        JSTemporalPlainMonthDay monthDay => monthDay.calendarId,
        JSTemporalPlainYearMonth yearMonth => yearMonth.calendarId,
        JSTemporalZonedDateTime zoned => zoned.calendarId,
        _ => null,
    };

    /// <summary>
    /// ToTemporalCalendarIdentifier (s12.3.10): a Temporal object's own calendar; else a String, read by the grammar's
    /// ParseTemporalCalendarString and canonicalized; anything else is the <c>TypeError</c>.
    /// </summary>
    internal static string ToCalendarIdentifier(JSValue calendar)
    {
        if (CalendarSlotOf(calendar) is { } held)
            return held;
        if (calendar == null || !calendar.IsString)
            throw JSEngine.NewTypeError("Temporal: calendar must be a calendar identifier string or a Temporal object with a calendar");
        return CanonicalizeCalendar(ParseCalendarString(calendar.StringValue));
    }

    /// <summary>
    /// GetTemporalCalendarIdentifierWithISODefault (s12.3.11) of a property bag: its <c>calendar</c> property, read once,
    /// <c>iso8601</c> where it is undefined.
    /// </summary>
    internal static string CalendarWithIsoDefault(JSObject item)
    {
        var calendar = item[KeyStrings.GetOrCreate("calendar")];
        return calendar == null || calendar.IsUndefined ? "iso8601" : ToCalendarIdentifier(calendar);
    }

    /// <summary>
    /// IsPartialTemporalObject's refusals, each a <c>TypeError</c>: a Temporal object (it carries its own calendar or time
    /// zone), or an object with a <c>calendar</c> or a <c>timeZone</c> property, own or inherited.
    /// </summary>
    internal static void RejectObjectWithCalendarOrTimeZone(JSObject obj)
    {
        if (obj is JSTemporalPlainDate or JSTemporalPlainDateTime or JSTemporalPlainMonthDay
            or JSTemporalPlainTime or JSTemporalPlainYearMonth or JSTemporalZonedDateTime)
            throw JSEngine.NewTypeError("Temporal: a Temporal object is not a valid fields object");

        if (!obj[KeyStrings.GetOrCreate("calendar")].IsUndefined)
            throw JSEngine.NewTypeError("Temporal: a fields object must not have a calendar property");

        if (!obj[KeyStrings.GetOrCreate("timeZone")].IsUndefined)
            throw JSEngine.NewTypeError("Temporal: a fields object must not have a timeZone property");
    }

    // ── PrepareCalendarFields ────────────────────────────────────────────────────

    /// <summary>
    /// PrepareCalendarFields (s12.3.3) of the calendar fields: the type calls one member per field, in Table 19's
    /// property order, with its own time, <c>offset</c> and <c>timeZone</c> reads between them - <c>day</c>, <c>era</c>
    /// and <c>eraYear</c>, the time fields, <c>month</c>, <c>monthCode</c>, the rest, <c>year</c>. Each value is converted
    /// as it is read: <c>era</c> by ToString; <c>eraYear</c> and <c>year</c> by ToIntegerWithTruncation, not narrowed;
    /// <c>month</c> and <c>day</c> by ToPositiveIntegerWithTruncation; <c>monthCode</c> by ToPrimitive, a
    /// <c>TypeError</c> for a value that is not a String, then the library's ParseMonthCode, ASCII digits only (row C6).
    /// The checks against the calendar are the library's, after the <c>overflow</c> option.
    /// </summary>
    internal sealed class CalendarFieldReader
    {
        private readonly JSObject source;
        private readonly string typeName;
        private readonly bool eras;
        private CalendarFields fields;

        /// <param name="source">The property bag.</param>
        /// <param name="calendarId">The calendar the fields are read for.</param>
        /// <param name="typeName">The type the messages name, for example <c>Temporal.PlainDate</c>.</param>
        /// <param name="readsYear">Whether <c>year</c> is one of the fields, so that CalendarExtraFields adds the era fields.</param>
        internal CalendarFieldReader(JSObject source, string calendarId, string typeName, bool readsYear = true)
        {
            this.source = source;
            this.typeName = typeName;
            eras = readsYear && SupportsEra(calendarId);
        }

        /// <summary>The calendar fields read; each unset where the property was undefined.</summary>
        internal CalendarFields Fields => fields;

        /// <summary>Whether any field read was not undefined: a partial bag needs one.</summary>
        internal bool Any { get; private set; }

        private JSValue Get(string name)
        {
            var value = source[KeyStrings.GetOrCreate(name)];
            if (value == null || value.IsUndefined)
                return null;
            Any = true;
            return value;
        }

        /// <summary>The <c>day</c> field.</summary>
        internal void Day()
        {
            if (Get("day") is { } value)
                fields = fields with { Day = PositiveInteger(value) };
        }

        /// <summary>The <c>era</c> and <c>eraYear</c> fields, read only where the calendar has eras and <c>year</c> is read.</summary>
        internal void Era()
        {
            if (!eras)
                return;
            if (Get("era") is { } era)
                fields = fields with { Era = era.StringValue };
            if (Get("eraYear") is { } eraYear)
                fields = fields with { EraYear = Integer(eraYear) };
        }

        /// <summary>The <c>month</c> field.</summary>
        internal void Month()
        {
            if (Get("month") is { } value)
                fields = fields with { Month = PositiveInteger(value) };
        }

        /// <summary>The <c>monthCode</c> field: a String, then a month code by the grammar, else the <c>RangeError</c>.</summary>
        internal void MonthCode()
        {
            var code = RequireMonthCodeString(Get("monthCode"), typeName);
            if (code == null)
                return;
            if (Broiler.ECMAScript.Temporal.MonthCode.Parse(code, out var monthCode) != CalendarStatus.Completed)
                throw JSEngine.NewRangeError($"{typeName}: invalid monthCode \"{code}\"");
            fields = fields with { MonthCode = monthCode };
        }

        /// <summary>The <c>year</c> field.</summary>
        internal void Year()
        {
            if (Get("year") is { } value)
                fields = fields with { Year = Integer(value) };
        }

        /// <summary>The date fields alone, in their order: <c>day</c>, the era fields, <c>month</c>, <c>monthCode</c>, <c>year</c>.</summary>
        internal void DateFields()
        {
            Day();
            Era();
            Month();
            MonthCode();
            Year();
        }

        private double Integer(JSValue value) =>
            TemporalRoundingOptions.ToIntegerWithTruncation(value, $"{typeName}: date component must be finite");

        private double PositiveInteger(JSValue value)
        {
            var integer = Integer(value);
            if (integer < 1)
                throw JSEngine.NewRangeError($"{typeName}: month and day must be positive");
            return integer;
        }
    }

    // ── the calendar operations ──────────────────────────────────────────────────

    /// <summary>The <c>overflow</c> option, as the calendars take it.</summary>
    private static CalendarOverflow CalendarOverflowOf(TemporalOverflow overflow) =>
        overflow == TemporalOverflow.Reject ? CalendarOverflow.Reject : CalendarOverflow.Constrain;

    /// <summary>
    /// Each status but <see cref="CalendarStatus.Completed"/> as this engine's error: a missing field is the
    /// <c>TypeError</c> (<see cref="TemporalCalendars.IsTypeError"/>), every other status the <c>RangeError</c>, with
    /// <paramref name="outOfRange"/> the message of a result or a field beyond the limits or the calendar's.
    /// </summary>
    private static void CheckCalendar(CalendarStatus status, string calendarId, string typeName, string outOfRange)
    {
        if (status == CalendarStatus.Completed)
            return;

        throw status switch
        {
            CalendarStatus.MissingYear => JSEngine.NewTypeError($"{typeName}: missing year (or era and eraYear)"),
            CalendarStatus.MissingMonth => JSEngine.NewTypeError($"{typeName}: missing month / monthCode"),
            CalendarStatus.MissingDay => JSEngine.NewTypeError($"{typeName}: missing day"),
            CalendarStatus.IncompleteEra => JSEngine.NewTypeError("Temporal: era and eraYear must be provided together"),
            CalendarStatus.InvalidMonthCode => JSEngine.NewRangeError($"{typeName}: invalid monthCode for the {calendarId} calendar"),
            CalendarStatus.UnknownEra => JSEngine.NewRangeError($"Temporal: invalid era for the {calendarId} calendar"),
            CalendarStatus.EraYearMismatch => JSEngine.NewRangeError("Temporal: year and era/eraYear do not agree"),
            CalendarStatus.MonthCodeMismatch => JSEngine.NewRangeError($"{typeName}: month and monthCode disagree"),
            CalendarStatus.MonthNotInYear => JSEngine.NewRangeError($"{typeName}: the year has no such leap month in the {calendarId} calendar"),
            CalendarStatus.NoReferenceDate => JSEngine.NewRangeError($"{typeName}: month-day is out of range"),
            _ => JSEngine.NewRangeError(outOfRange),
        };
    }

    /// <summary>CalendarISOToDate (s12.3.26): the calendar's fields of a date within the limits, every getter's record.</summary>
    internal static CalendarDate CalendarDateOf(string calendarId, IsoDate date)
    {
        CheckCalendar(Calendars.IsoToDate(CalendarIdOf(calendarId), date, out var result), calendarId, "Temporal", "Temporal: date is out of range");
        return result;
    }

    /// <summary>The <c>era</c> getter's value: the era code, undefined where the calendar has none.</summary>
    internal static JSValue EraValue(CalendarDate date) => date.Era is null ? JSUndefined.Value : new JSString(date.Era);

    /// <summary>The <c>eraYear</c> getter's value: undefined where the calendar has no eras.</summary>
    internal static JSValue EraYearValue(CalendarDate date) => date.EraYear is { } eraYear ? new JSNumber(eraYear) : JSUndefined.Value;

    /// <summary>The <c>weekOfYear</c> getter's value: undefined outside ISO 8601.</summary>
    internal static JSValue WeekOfYearValue(CalendarDate date) => date.WeekOfYear is { } week ? new JSNumber(week) : JSUndefined.Value;

    /// <summary>The <c>yearOfWeek</c> getter's value: undefined outside ISO 8601.</summary>
    internal static JSValue YearOfWeekValue(CalendarDate date) => date.YearOfWeek is { } year ? new JSNumber(year) : JSUndefined.Value;

    /// <summary>ISODateToFields (s13.42): the fields <c>with</c> merges onto.</summary>
    internal static CalendarFields DateToFields(string calendarId, IsoDate date, CalendarFieldsType type)
    {
        CheckCalendar(Calendars.DateToFields(CalendarIdOf(calendarId), date, type, out var result), calendarId, "Temporal", "Temporal: date is out of range");
        return result;
    }

    /// <summary>CalendarMergeFields (s12.3.5) with CalendarFieldKeysToIgnore, the library's.</summary>
    internal static CalendarFields MergeFields(string calendarId, CalendarFields fields, CalendarFields additional) =>
        TemporalCalendars.MergeFields(CalendarIdOf(calendarId), fields, additional);

    /// <summary>CalendarDateFromFields (s12.3.12) under the <c>overflow</c> option, then ISODateWithinLimits.</summary>
    internal static IsoDate DateFromFields(string calendarId, CalendarFields fields, TemporalOverflow overflow, string typeName)
    {
        CheckCalendar(Calendars.DateFromFields(CalendarIdOf(calendarId), fields, CalendarOverflowOf(overflow), out var result),
            calendarId, typeName, $"{typeName}: date is out of range");
        return result;
    }

    /// <summary>CalendarYearMonthFromFields (s12.3.13): the first day of the calendar month, then ISOYearMonthWithinLimits.</summary>
    internal static IsoDate YearMonthFromFields(string calendarId, CalendarFields fields, TemporalOverflow overflow, string typeName)
    {
        CheckCalendar(Calendars.YearMonthFromFields(CalendarIdOf(calendarId), fields, CalendarOverflowOf(overflow), out var result),
            calendarId, typeName, $"{typeName}: year-month is out of range");
        return result;
    }

    /// <summary>
    /// CalendarMonthDayFromFields (s12.3.14): the reference date, ISO 8601's year 1972 or Table 6's and the 1900 to
    /// 2035 rule elsewhere (row C10).
    /// </summary>
    internal static IsoDate MonthDayFromFields(string calendarId, CalendarFields fields, TemporalOverflow overflow, string typeName)
    {
        CheckCalendar(Calendars.MonthDayFromFields(CalendarIdOf(calendarId), fields, CalendarOverflowOf(overflow), out var result),
            calendarId, typeName, $"{typeName}: month-day is out of range");
        return result;
    }

    /// <summary>
    /// CalendarDateAdd (s12.3.7), the library's <see cref="ICalendarArithmetic"/> for every calendar: the years and
    /// months in the calendar, the day regulated under the <c>overflow</c> option, then the weeks and days, the result
    /// within the limits. Each field is an integer of a valid duration, held exactly.
    /// </summary>
    internal static IsoDate CalendarDateAdd(string calendarId, IsoDate date, double years, double months, double weeks, double days,
        TemporalOverflow overflow, string typeName)
    {
        var duration = new CalendarDuration((long)years, (long)months, (long)weeks, (long)days);
        var status = Calendars.DateAdd(CalendarIdOf(calendarId), date, duration, CalendarOverflowOf(overflow), out var result);
        if (status == CalendarStatus.FieldOutOfRange)
            throw JSEngine.NewRangeError($"{typeName}: day out of range for resulting month");
        CheckCalendar(status, calendarId, typeName, $"{typeName}: result is out of range");
        return result;
    }

    /// <summary><c>PlainDate.prototype.toPlainYearMonth</c>'s calendar steps: the first day of the calendar month (row C15).</summary>
    internal static IsoDate DateToYearMonth(string calendarId, IsoDate date)
    {
        CheckCalendar(Calendars.DateToYearMonth(CalendarIdOf(calendarId), date, out var result),
            calendarId, "Temporal.PlainYearMonth", "Temporal.PlainYearMonth: year-month is out of range");
        return result;
    }

    /// <summary><c>PlainDate.prototype.toPlainMonthDay</c>'s calendar steps: the reference date in the date's calendar (row C16).</summary>
    internal static IsoDate DateToMonthDay(string calendarId, IsoDate date)
    {
        CheckCalendar(Calendars.DateToMonthDay(CalendarIdOf(calendarId), date, out var result),
            calendarId, "Temporal.PlainMonthDay", "Temporal.PlainMonthDay: month-day is out of range");
        return result;
    }

    /// <summary>
    /// <c>PlainMonthDay.prototype.toPlainDate</c>'s calendar steps: the month-day's fields merged with those read from
    /// the argument, under <c>constrain</c>; the year is the calendar's, never an ISO year (row C17).
    /// </summary>
    internal static IsoDate MonthDayToDate(string calendarId, IsoDate monthDay, CalendarFields fields)
    {
        CheckCalendar(Calendars.MonthDayToDate(CalendarIdOf(calendarId), monthDay, fields, out var result),
            calendarId, "Temporal.PlainMonthDay.prototype.toPlainDate", "Temporal.PlainMonthDay.prototype.toPlainDate: date is out of range");
        return result;
    }

    /// <summary><c>PlainYearMonth.prototype.toPlainDate</c>'s calendar steps: the year-month's fields merged with the day read, under <c>constrain</c>.</summary>
    internal static IsoDate YearMonthToDate(string calendarId, IsoDate yearMonth, CalendarFields fields)
    {
        CheckCalendar(Calendars.YearMonthToDate(CalendarIdOf(calendarId), yearMonth, fields, out var result),
            calendarId, "Temporal.PlainYearMonth.prototype.toPlainDate",
            "Temporal.PlainYearMonth.prototype.toPlainDate: result is outside the supported ISO date range");
        return result;
    }

    /// <summary>
    /// ToTemporalYearMonth's steps after a string's limits: ISODateToFields as a year-month and CalendarYearMonthFromFields
    /// under <c>constrain</c>, so that the reference day is the calendar month's first.
    /// </summary>
    internal static IsoDate YearMonthOfDate(string calendarId, IsoDate date) =>
        YearMonthFromFields(calendarId, DateToFields(calendarId, date, CalendarFieldsType.YearMonth), TemporalOverflow.Constrain, "Temporal.PlainYearMonth");

    /// <summary>
    /// ToTemporalMonthDay's steps after a string's limits, outside ISO 8601: ISODateToFields as a month-day and
    /// CalendarMonthDayFromFields under <c>constrain</c>, so that the reference year is the calendar's.
    /// </summary>
    internal static IsoDate MonthDayOfDate(string calendarId, IsoDate date) =>
        MonthDayFromFields(calendarId, DateToFields(calendarId, date, CalendarFieldsType.MonthDay), TemporalOverflow.Constrain, "Temporal.PlainMonthDay");

    /// <summary>
    /// A year-month's first day, as AddDurationToYearMonth and DifferenceTemporalPlainYearMonth form it: ISODateToFields
    /// as a year-month, its day set to 1, and CalendarDateFromFields under <c>constrain</c>, within the date limits.
    /// </summary>
    internal static IsoDate YearMonthFirstDay(string calendarId, IsoDate yearMonth, string typeName) =>
        DateFromFields(calendarId, DateToFields(calendarId, yearMonth, CalendarFieldsType.YearMonth) with { Day = 1 }, TemporalOverflow.Constrain, typeName);
}
