using System;
using Broiler.ECMAScript.Numbers;
using Broiler.ECMAScript.Temporal;
using Broiler.ECMAScript.Time;
using Broiler.JavaScript.BuiltIns.Function;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Temporal;

// Temporal.PlainDate (Temporal proposal §3): a calendar date with no time or zone, held as its ISO
// date and calendar identifier. Its calendar fields, field resolution and addition are the shared
// library's calendars, and its differences, rounded in any calendar, the shared library's
// TemporalDifferences, through TemporalAdapter.
//
// Registered under the Temporal namespace (not as a global) via Register = false.
[JSClassGenerator("PlainDate", Register = false)]
public partial class JSTemporalPlainDate : JSObject
{
    internal readonly int isoYear, isoMonth, isoDay;
    internal readonly string calendarId;

    [JSExport(Length = 3)]
    public JSTemporalPlainDate(in Arguments a) : base(ResolvePrototype())
    {
        isoYear = ToIntegerWithTruncationArgument(a.GetAt(0));
        isoMonth = ToIntegerWithTruncationArgument(a.GetAt(1));
        isoDay = ToIntegerWithTruncationArgument(a.GetAt(2));
        calendarId = TemporalAdapter.CalendarArgument(a.GetAt(3), "Temporal.PlainDate");

        if (TemporalLimits.CheckIsoDate(IsoDate) != TemporalStatus.Completed)
            throw JSEngine.NewRangeError("Temporal.PlainDate: invalid ISO date");
    }

    internal JSTemporalPlainDate(int isoYear, int isoMonth, int isoDay, JSObject prototype)
        : this(isoYear, isoMonth, isoDay, "iso8601", prototype) { }

    internal JSTemporalPlainDate(int isoYear, int isoMonth, int isoDay, string calendarId, JSObject prototype) : base(prototype)
    {
        this.isoYear = isoYear;
        this.isoMonth = isoMonth;
        this.isoDay = isoDay;
        this.calendarId = calendarId;
    }

    internal JSTemporalPlainDate(IsoDate date, string calendarId, JSObject prototype)
        : this((int)date.Year, date.Month, date.Day, calendarId, prototype) { }

    // The date's slots as the shared library's record.
    internal IsoDate IsoDate => new(isoYear, isoMonth, isoDay);

    private static JSObject ResolvePrototype()
    {
        if (JSEngine.NewTarget == null && (JSEngine.Current as IJSExecutionContext)?.CurrentNewTarget == null)
            throw JSEngine.NewTypeError("Constructor Temporal.PlainDate requires 'new'");

        return JSEngine.NewTargetPrototype ?? PlainDatePrototype;
    }

    private static readonly KeyString PlainDateIntrinsicKey = KeyStrings.GetOrCreate("Temporal.PlainDate");

    // %Temporal.PlainDate.prototype% of the current realm, never the prototype of whatever
    // `globalThis.Temporal.PlainDate` holds now: guest code may replace the namespace or its members.
    internal static JSObject PlainDatePrototype => Intrinsics.TemporalPrototype(PlainDateIntrinsicKey);

    // ── accessors ───────────────────────────────────────────────────────────────

    [JSExport("calendarId")] public JSValue CalendarId => new JSString(calendarId);

    // CalendarISOToDate, the shared library's: the calendar's fields of the date, which every getter reads.
    private CalendarDate CalendarDate => TemporalAdapter.CalendarDateOf(calendarId, IsoDate);

    // era and eraYear are undefined in the calendars without eras (iso8601, chinese, dangi).
    [JSExport("era")] public JSValue Era => TemporalAdapter.EraValue(CalendarDate);
    [JSExport("eraYear")] public JSValue EraYear => TemporalAdapter.EraYearValue(CalendarDate);
    [JSExport("year")] public double YearValue => CalendarDate.Year;
    [JSExport("month")] public double MonthValue => CalendarDate.Month;
    [JSExport("monthCode")] public JSValue MonthCode => new JSString(CalendarDate.MonthCode.ToString());
    [JSExport("day")] public double DayValue => CalendarDate.Day;

    [JSExport("dayOfWeek")] public double DayOfWeek => CalendarDate.DayOfWeek;
    [JSExport("dayOfYear")] public double DayOfYear => CalendarDate.DayOfYear;
    [JSExport("daysInWeek")] public double DaysInWeek => 7;
    [JSExport("daysInMonth")] public double DaysInMonth => CalendarDate.DaysInMonth;
    [JSExport("daysInYear")] public double DaysInYear => CalendarDate.DaysInYear;
    [JSExport("monthsInYear")] public double MonthsInYear => CalendarDate.MonthsInYear;
    [JSExport("inLeapYear")] public bool InLeapYear => CalendarDate.InLeapYear;

    // Only the ISO calendar defines a week-numbering system; every other calendar returns undefined.
    [JSExport("weekOfYear")] public JSValue WeekOfYear => TemporalAdapter.WeekOfYearValue(CalendarDate);
    [JSExport("yearOfWeek")] public JSValue YearOfWeek => TemporalAdapter.YearOfWeekValue(CalendarDate);

    // ── statics ─────────────────────────────────────────────────────────────────

    [JSExport("from", Length = 1)]
    internal static JSValue From(in Arguments a)
        => ToTemporalDate(a.GetAt(0), a.GetAt(1));

    [JSExport("compare", Length = 2)]
    internal static JSValue Compare(in Arguments a)
    {
        var one = RequireDate(ToTemporalDate(a.GetAt(0)));
        var two = RequireDate(ToTemporalDate(a.GetAt(1)));
        return new JSNumber(IsoArithmetic.CompareIsoDate(one.IsoDate, two.IsoDate));
    }

    // ── methods ─────────────────────────────────────────────────────────────────

    [JSExport("with", Length = 1)]
    public JSValue With(in Arguments a)
    {
        if (a.GetAt(0) is not JSObject obj)
            throw JSEngine.NewTypeError("Temporal.PlainDate.prototype.with requires an object");

        // The with()-bag must be a plain fields object: not a Temporal object and without a
        // calendar / timeZone property (IsPartialTemporalObject).
        TemporalAdapter.RejectObjectWithCalendarOrTimeZone(obj);

        // ISODateToFields, then PrepareCalendarFields of the partial bag in property order - day, [era, eraYear,]
        // month, monthCode, year - each converted as it is read; then the overflow option, after every field; then
        // CalendarMergeFields and CalendarDateFromFields, the shared library's.
        var fields = TemporalAdapter.DateToFields(calendarId, IsoDate, CalendarFieldsType.Date);
        var partial = new TemporalAdapter.CalendarFieldReader(obj, calendarId, "Temporal.PlainDate");
        partial.DateFields();
        if (!partial.Any)
            throw JSEngine.NewTypeError("Temporal.PlainDate.prototype.with requires at least one date property");

        var overflow = TemporalAdapter.ReadOverflow(a.GetAt(1));
        var date = TemporalAdapter.DateFromFields(calendarId, TemporalAdapter.MergeFields(calendarId, fields, partial.Fields), overflow,
            "Temporal.PlainDate");
        return new JSTemporalPlainDate(date, calendarId, PlainDatePrototype);
    }

    [JSExport("withCalendar", Length = 1)]
    public JSValue WithCalendar(in Arguments a)
    {
        var arg = a.GetAt(0);
        if (arg == null || arg.IsUndefined)
            throw JSEngine.NewTypeError("Temporal.PlainDate.prototype.withCalendar requires a calendar");
        return new JSTemporalPlainDate(isoYear, isoMonth, isoDay, TemporalAdapter.ToCalendarIdentifier(arg), PlainDatePrototype);
    }

    [JSExport("add", Length = 1)]
    public JSValue Add(in Arguments a) => AddDuration(a.GetAt(0), a.GetAt(1), 1);

    [JSExport("subtract", Length = 1)]
    public JSValue Subtract(in Arguments a) => AddDuration(a.GetAt(0), a.GetAt(1), -1);

    private JSValue AddDuration(JSValue durationLike, JSValue options, int sign)
    {
        // Spec reads the duration argument before the overflow option.
        var d = (JSTemporalDuration)JSTemporalDuration.ToTemporalDuration(durationLike);
        var overflow = TemporalAdapter.ReadOverflow(options);

        // ToDateDurationRecordWithoutTime: the days and the time fields as one exact time duration of
        // 24-hour days, its whole days truncated toward zero (row R7: no field is narrowed first).
        var days = (long)(TemporalAdapter.TimeDurationWith24HourDays(d) / TemporalLimits.NanosecondsPerDay);

        // CalendarDateAdd, the shared library's, in the date's calendar (row R7: no year is narrowed first).
        var added = TemporalAdapter.CalendarDateAdd(calendarId, IsoDate,
            sign * d.YearsValue, sign * d.MonthsValue, sign * d.WeeksValue, sign * days, overflow, "Temporal.PlainDate");
        return new JSTemporalPlainDate(added, calendarId, PlainDatePrototype);
    }

    [JSExport("until", Length = 1)]
    public JSValue Until(in Arguments a) => Difference(a.GetAt(0), a.GetAt(1), DifferenceOperation.Until);

    [JSExport("since", Length = 1)]
    public JSValue Since(in Arguments a) => Difference(a.GetAt(0), a.GetAt(1), DifferenceOperation.Since);

    // DifferenceTemporalPlainDate (s3.5.14): ToTemporalDate, the calendars equal, GetDifferenceSettings' reads and rules,
    // then the shared library's difference: the calendar's DateUntil anchored on the receiver, rounded relative to it
    // where the smallest unit or the increment asks, in every calendar (row F4), and negated for since.
    private JSValue Difference(JSValue other, JSValue options, DifferenceOperation operation)
    {
        const string typeName = "Temporal.PlainDate";
        var target = RequireDate(ToTemporalDate(other));
        if (calendarId != target.calendarId)
            throw JSEngine.NewRangeError("Temporal.PlainDate: cannot compute the difference between dates of different calendars");

        var settings = TemporalAdapter.DifferenceSettings(TemporalType.PlainDate, operation, options, typeName);
        return TemporalAdapter.DurationOf(TemporalDifferences.DifferenceTemporalPlainDate(operation, TemporalAdapter.Calendars,
            TemporalAdapter.CalendarIdOf(calendarId), IsoDate, target.IsoDate, settings, out var result), result, typeName);
    }

    [JSExport("equals", Length = 1)]
    public JSValue Equals(in Arguments a)
    {
        var other = RequireDate(ToTemporalDate(a.GetAt(0)));
        return isoYear == other.isoYear && isoMonth == other.isoMonth && isoDay == other.isoDay
            && calendarId == other.calendarId
            ? BooleanTrue : BooleanFalse;
    }

    // TemporalDateToString, the shared library's, with the calendarName option.
    [JSExport("toString", Length = 0)]
    public JSValue ToStringMethod(in Arguments a)
        => new JSString(TemporalFormat.DateToString(IsoDate, calendarId, TemporalAdapter.ReadShowCalendarName(a.GetAt(0))));

    [JSExport("toJSON", Length = 0)]
    public JSValue ToJSON(in Arguments a) => new JSString(TemporalFormat.DateToString(IsoDate, calendarId, ShowCalendarName.Auto));

    [JSExport("toLocaleString", Length = 0)]
    public JSValue ToLocaleString(in Arguments a)
        => Intl.JSIntlDateTimeFormat.TemporalToLocaleString(this, a.GetAt(0), a.GetAt(1));

    [JSExport("valueOf", Length = 0)]
    public JSValue ValueOf(in Arguments a)
        => throw JSEngine.NewTypeError("Called Temporal.PlainDate.prototype.valueOf, which is not supported. Use Temporal.PlainDate.compare for comparison.");

    // toPlainDateTime combines this date with a PlainTime (default midnight).
    [JSExport("toPlainDateTime", Length = 0)]
    public JSValue ToPlainDateTime(in Arguments a)
    {
        // The resulting PlainDateTime keeps this date's calendar (CreateTemporalDateTime with the
        // PlainDate's [[Calendar]]), so e.g. a buddhist PlainDate yields a buddhist PlainDateTime.
        var arg = a.GetAt(0);
        if (arg == null || arg.IsUndefined)
            return JSTemporalPlainDateTime.Create(isoYear, isoMonth, isoDay, 0, 0, 0, 0, 0, 0, calendarId, JSTemporalPlainDateTime.PlainDateTimePrototype);

        var t = JSTemporalPlainTime.From(new Arguments(JSUndefined.Value, arg)) as JSTemporalPlainTime
            ?? throw JSEngine.NewTypeError("expected a Temporal.PlainTime");
        return JSTemporalPlainDateTime.Create(isoYear, isoMonth, isoDay,
            t.hour, t.minute, t.second, t.millisecond, t.microsecond, t.nanosecond, calendarId, JSTemporalPlainDateTime.PlainDateTimePrototype);
    }

    // toPlainYearMonth: ISODateToFields and CalendarYearMonthFromFields under constrain, the shared library's - the
    // first day of the calendar month, in the date's calendar (row C15).
    [JSExport("toPlainYearMonth", Length = 0)]
    public JSValue ToPlainYearMonth(in Arguments a)
        => new JSTemporalPlainYearMonth(TemporalAdapter.DateToYearMonth(calendarId, IsoDate), calendarId,
            JSTemporalPlainYearMonth.PlainYearMonthPrototype);

    // toPlainMonthDay: ISODateToFields and CalendarMonthDayFromFields under constrain, the shared library's - the
    // calendar's reference date, and the date's calendar, the Gregorian family's included (row C16).
    [JSExport("toPlainMonthDay", Length = 0)]
    public JSValue ToPlainMonthDay(in Arguments a)
        => new JSTemporalPlainMonthDay(TemporalAdapter.DateToMonthDay(calendarId, IsoDate), calendarId,
            JSTemporalPlainMonthDay.PlainMonthDayPrototype);

    // toZonedDateTime(timeZone | { timeZone, plainTime }): combine this date with a time
    // (default midnight) and interpret it in the given zone.
    [JSExport("toZonedDateTime", Length = 1)]
    public JSValue ToZonedDateTime(in Arguments a)
    {
        var item = a.GetAt(0);
        string timeZone;
        var h = 0; var mi = 0; var s = 0; var ms = 0; var us = 0; var ns = 0;
        var hasPlainTime = false;

        if (item != null && item.IsString)
            timeZone = item.ToString();
        else if (item is JSObject obj)
        {
            var tzValue = obj[KeyStrings.GetOrCreate("timeZone")];
            if (tzValue.IsUndefined || !tzValue.IsString)
                throw JSEngine.NewTypeError("Temporal.PlainDate.prototype.toZonedDateTime: timeZone must be a string");
            timeZone = tzValue.ToString();

            var plainTimeValue = obj[KeyStrings.GetOrCreate("plainTime")];
            if (!plainTimeValue.IsUndefined)
            {
                var t = JSTemporalPlainTime.From(new Arguments(JSUndefined.Value, plainTimeValue)) as JSTemporalPlainTime
                    ?? throw JSEngine.NewTypeError("expected a Temporal.PlainTime");
                h = t.hour; mi = t.minute; s = t.second; ms = t.millisecond; us = t.microsecond; ns = t.nanosecond;
                hasPlainTime = true;
            }
        }
        else throw JSEngine.NewTypeError("Temporal.PlainDate.prototype.toZonedDateTime: invalid argument");

        // With no explicit plainTime, the result is the zone's start of day (GetStartOfDay), which
        // across a gap covering midnight is the transition instant — not midnight.
        return hasPlainTime
            ? JSTemporalZonedDateTime.FromLocal(isoYear, isoMonth, isoDay, h, mi, s, ms, us, ns, timeZone, calendarId)
            : JSTemporalZonedDateTime.StartOfDayFor(isoYear, isoMonth, isoDay, timeZone, calendarId);
    }

    // ── value coercion / validation ─────────────────────────────────────────────

    private static JSTemporalPlainDate RequireDate(JSValue value)
        => value as JSTemporalPlainDate ?? throw JSEngine.NewTypeError("expected a Temporal.PlainDate");

    // The PlainDate constructor's positional arguments are required: each is coerced with
    // ToIntegerWithTruncation, which does ToNumber first, so an absent / undefined argument (NaN) — or
    // a non-numeric string — is a RangeError rather than silently defaulting to 0.
    private static int ToIntegerWithTruncationArgument(JSValue value)
        => (int)TemporalRoundingOptions.ToIntegerWithTruncation(value ?? JSUndefined.Value, "Temporal.PlainDate: date component must be finite");

    private static JSValue ToTemporalDate(JSValue item) => ToTemporalDate(item, JSUndefined.Value);

    // `options` is the raw options argument; the overflow option is read at the spec-mandated point
    // (after the item's type is validated and its slots / fields / string are read) so an invalid
    // primitive item throws a TypeError before the options bag is ever observed.
    private static JSValue ToTemporalDate(JSValue item, JSValue options)
    {
        if (item is JSTemporalPlainDate d)
        {
            TemporalAdapter.ReadOverflow(options);
            return new JSTemporalPlainDate(d.isoYear, d.isoMonth, d.isoDay, d.calendarId, PlainDatePrototype);
        }

        // A Temporal.ZonedDateTime / PlainDateTime is consumed via its internal slots (its wall-clock
        // ISO date + calendar), NOT via its observable getters — GetISODateTimeFor / the stored ISO
        // date + CreateTemporalDate. (Spec ToTemporalDate, ZonedDateTime / PlainDateTime branches.)
        if (item is JSTemporalZonedDateTime zdt)
        {
            var result = zdt.ToPlainDateFromSlots();
            TemporalAdapter.ReadOverflow(options);
            return result;
        }

        if (item is JSTemporalPlainDateTime pdt)
        {
            TemporalAdapter.ReadOverflow(options);
            return new JSTemporalPlainDate(pdt.isoYear, pdt.isoMonth, pdt.isoDay, pdt.calendarId, PlainDatePrototype);
        }

        if (item.IsString)
        {
            var parsed = ParseTemporalDateString(item.ToString());
            TemporalAdapter.ReadOverflow(options);
            return parsed;
        }

        if (item is not JSObject obj)
            throw JSEngine.NewTypeError("Temporal.PlainDate: invalid value");

        // GetTemporalCalendarIdentifierWithISODefault, then PrepareCalendarFields in property order - day,
        // [era, eraYear,] month, monthCode, year - each converted as it is read; then the overflow option; then
        // CalendarDateFromFields, the shared library's, whose missing-field TypeErrors and RangeErrors follow the
        // option.
        var calendarId = TemporalAdapter.CalendarWithIsoDefault(obj);
        var fields = new TemporalAdapter.CalendarFieldReader(obj, calendarId, "Temporal.PlainDate");
        fields.DateFields();

        var overflow = TemporalAdapter.ReadOverflow(options);
        return new JSTemporalPlainDate(TemporalAdapter.DateFromFields(calendarId, fields.Fields, overflow, "Temporal.PlainDate"),
            calendarId, PlainDatePrototype);
    }

    // ParseISODateTime with the TemporalDateTimeString[~Zoned] goal: the shared library's grammar.
    private static JSValue ParseTemporalDateString(string text)
        => FromParsed(TemporalAdapter.ParseIsoString(text, [TemporalGoal.DateTime], "Temporal.PlainDate"), text);

    // ToTemporalDate's steps after ParseISODateTime (a PlainDate string, or a relativeTo string without a time
    // zone): the calendar annotation canonicalized, then CreateTemporalDate's ISODateWithinLimits.
    internal static JSTemporalPlainDate FromParsed(IsoParseResult parsed, string text)
    {
        var calendarId = TemporalAdapter.CalendarOf(parsed);
        var date = TemporalAdapter.DateOf(parsed);
        if (TemporalLimits.CheckIsoDate(date) != TemporalStatus.Completed)
            throw JSEngine.NewRangeError($"Cannot parse Temporal.PlainDate from \"{text}\"");

        return new JSTemporalPlainDate((int)date.Year, date.Month, date.Day, calendarId, PlainDatePrototype);
    }
}
