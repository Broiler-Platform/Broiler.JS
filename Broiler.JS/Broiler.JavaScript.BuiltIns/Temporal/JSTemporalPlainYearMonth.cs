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

// Temporal.PlainYearMonth (Temporal proposal §9): a calendar year and month (e.g. "2024-06"),
// stored as the ISO date of day 1 of that calendar month (the reference ISO day round-trips the
// month for the non-ISO calendars). Its calendar fields, field resolution and addition are the
// shared library's calendars, and its differences, rounded in any calendar, the shared library's
// TemporalDifferences, through TemporalAdapter. Registered under the Temporal namespace via
// Register = false.
[JSClassGenerator("PlainYearMonth", Register = false)]
public partial class JSTemporalPlainYearMonth : JSObject
{
    internal readonly int isoYear, isoMonth, referenceISODay;
    internal readonly string calendarId;

    [JSExport(Length = 2)]
    public JSTemporalPlainYearMonth(in Arguments a) : base(ResolvePrototype())
    {
        isoYear = ToIntegerWithTruncationArgument(a.GetAt(0));
        isoMonth = ToIntegerWithTruncationArgument(a.GetAt(1));
        calendarId = TemporalAdapter.CalendarArgument(a.GetAt(2), "Temporal.PlainYearMonth");
        var refDay = a.GetAt(3);
        referenceISODay = refDay == null || refDay.IsUndefined ? 1 : ToIntegerWithTruncation(refDay);

        if (TemporalLimits.CheckIsoYearMonth(IsoDate) != TemporalStatus.Completed)
            throw JSEngine.NewRangeError("Temporal.PlainYearMonth: invalid ISO year-month");
    }

    internal JSTemporalPlainYearMonth(int isoYear, int isoMonth, int referenceISODay, JSObject prototype)
        : this(isoYear, isoMonth, referenceISODay, "iso8601", prototype) { }

    internal JSTemporalPlainYearMonth(int isoYear, int isoMonth, int referenceISODay, string calendarId, JSObject prototype) : base(prototype)
    {
        this.isoYear = isoYear; this.isoMonth = isoMonth; this.referenceISODay = referenceISODay;
        this.calendarId = calendarId;
    }

    internal JSTemporalPlainYearMonth(IsoDate date, string calendarId, JSObject prototype)
        : this((int)date.Year, date.Month, date.Day, calendarId, prototype) { }

    // The year-month's ISO date, its day the reference day, as the shared library's record.
    internal IsoDate IsoDate => new(isoYear, isoMonth, referenceISODay);

    private static JSObject ResolvePrototype()
    {
        if (JSEngine.NewTarget == null && (JSEngine.Current as IJSExecutionContext)?.CurrentNewTarget == null)
            throw JSEngine.NewTypeError("Constructor Temporal.PlainYearMonth requires 'new'");

        return JSEngine.NewTargetPrototype ?? PlainYearMonthPrototype;
    }

    private static readonly KeyString PlainYearMonthIntrinsicKey = KeyStrings.GetOrCreate("Temporal.PlainYearMonth");

    // %Temporal.PlainYearMonth.prototype% of the current realm, never the prototype of whatever
    // `globalThis.Temporal.PlainYearMonth` holds now: guest code may replace the namespace or its members.
    internal static JSObject PlainYearMonthPrototype => Intrinsics.TemporalPrototype(PlainYearMonthIntrinsicKey);

    // ── accessors ───────────────────────────────────────────────────────────────

    [JSExport("calendarId")] public JSValue CalendarId => new JSString(calendarId);

    // CalendarISOToDate of the reference date, the shared library's: the calendar's fields, which every getter reads.
    private CalendarDate CalendarDate => TemporalAdapter.CalendarDateOf(calendarId, IsoDate);

    [JSExport("era")] public JSValue Era => TemporalAdapter.EraValue(CalendarDate);
    [JSExport("eraYear")] public JSValue EraYear => TemporalAdapter.EraYearValue(CalendarDate);
    [JSExport("year")] public double YearValue => CalendarDate.Year;
    [JSExport("month")] public double MonthValue => CalendarDate.Month;
    [JSExport("monthCode")] public JSValue MonthCode => new JSString(CalendarDate.MonthCode.ToString());
    [JSExport("daysInMonth")] public double DaysInMonth => CalendarDate.DaysInMonth;
    [JSExport("daysInYear")] public double DaysInYear => CalendarDate.DaysInYear;
    [JSExport("monthsInYear")] public double MonthsInYear => CalendarDate.MonthsInYear;
    [JSExport("inLeapYear")] public bool InLeapYear => CalendarDate.InLeapYear;

    // ── statics ─────────────────────────────────────────────────────────────────

    [JSExport("from", Length = 1)]
    internal static JSValue From(in Arguments a)
        => ToTemporalYearMonth(a.GetAt(0), a.GetAt(1));

    [JSExport("compare", Length = 2)]
    internal static JSValue Compare(in Arguments a)
    {
        var one = Require(ToTemporalYearMonth(a.GetAt(0)));
        var two = Require(ToTemporalYearMonth(a.GetAt(1)));
        return new JSNumber(IsoArithmetic.CompareIsoDate(one.IsoDate, two.IsoDate));
    }

    // ── methods ─────────────────────────────────────────────────────────────────

    [JSExport("with", Length = 1)]
    public JSValue With(in Arguments a)
    {
        if (a.GetAt(0) is not JSObject obj)
            throw JSEngine.NewTypeError("Temporal.PlainYearMonth.prototype.with requires an object");
        TemporalAdapter.RejectObjectWithCalendarOrTimeZone(obj);

        // ISODateToFields, then PrepareCalendarFields of the partial bag in property order - [era, eraYear,]
        // month, monthCode, year - each converted as it is read; then the overflow option; then
        // CalendarMergeFields and CalendarYearMonthFromFields, the shared library's.
        var fields = TemporalAdapter.DateToFields(calendarId, IsoDate, CalendarFieldsType.YearMonth);
        var partial = new TemporalAdapter.CalendarFieldReader(obj, calendarId, "Temporal.PlainYearMonth");
        partial.Era();
        partial.Month();
        partial.MonthCode();
        partial.Year();
        if (!partial.Any)
            throw JSEngine.NewTypeError("Temporal.PlainYearMonth.prototype.with requires at least one field");

        var overflow = TemporalAdapter.ReadOverflow(a.GetAt(1));
        var date = TemporalAdapter.YearMonthFromFields(calendarId, TemporalAdapter.MergeFields(calendarId, fields, partial.Fields), overflow,
            "Temporal.PlainYearMonth");
        return new JSTemporalPlainYearMonth(date, calendarId, PlainYearMonthPrototype);
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

        // AddDurationToYearMonth: only the year and month components may be non-zero — a duration that
        // carries any weeks, days or sub-day time is a RangeError (the result is a year-month, so there
        // is nowhere for finer units to go).
        if (d.WeeksValue != 0 || d.DaysValue != 0 || d.HoursValue != 0 || d.MinutesValue != 0
            || d.SecondsValue != 0 || d.MillisecondsValue != 0 || d.MicrosecondsValue != 0 || d.NanosecondsValue != 0)
            throw JSEngine.NewRangeError("Temporal.PlainYearMonth: only years and months can be added or subtracted");

        // The years and months are added to the first day of the receiver's calendar month (ISODateToFields,
        // its day set to 1, CalendarDateFromFields under constrain, within the date limits: row B5's
        // -271821-05 minus a month is -271821-04-01, before the first date), by CalendarDateAdd under the
        // overflow option; the year-month is then CalendarYearMonthFromFields of the result's fields under it.
        // The shared library's, in every calendar; a year is never narrowed before the limits are tested (row R7).
        var start = TemporalAdapter.YearMonthFirstDay(calendarId, IsoDate, "Temporal.PlainYearMonth");
        var added = TemporalAdapter.CalendarDateAdd(calendarId, start, sign * d.YearsValue, sign * d.MonthsValue, 0, 0, overflow,
            "Temporal.PlainYearMonth");
        var yearMonth = TemporalAdapter.YearMonthFromFields(calendarId,
            TemporalAdapter.DateToFields(calendarId, added, CalendarFieldsType.YearMonth), overflow, "Temporal.PlainYearMonth");
        return new JSTemporalPlainYearMonth(yearMonth, calendarId, PlainYearMonthPrototype);
    }

    [JSExport("until", Length = 1)]
    public JSValue Until(in Arguments a) => Difference(a.GetAt(0), a.GetAt(1), DifferenceOperation.Until);

    [JSExport("since", Length = 1)]
    public JSValue Since(in Arguments a) => Difference(a.GetAt(0), a.GetAt(1), DifferenceOperation.Since);

    // DifferenceTemporalPlainYearMonth (s9.5.7): ToTemporalYearMonth, the calendars equal, GetDifferenceSettings' reads and
    // rules (years and months only), then the shared library's difference: equal ISO dates are zero; otherwise each
    // year-month's first day in the calendar (within the date limits: row B5), the calendar's DateUntil from the
    // receiver's, rounded relative to it where the smallest unit or the increment asks, in every calendar (row F4), and
    // negated for since.
    private JSValue Difference(JSValue other, JSValue options, DifferenceOperation operation)
    {
        const string typeName = "Temporal.PlainYearMonth";
        var target = Require(ToTemporalYearMonth(other));
        if (calendarId != target.calendarId)
            throw JSEngine.NewRangeError("Temporal.PlainYearMonth: cannot compute the difference between year-months of different calendars");

        var settings = TemporalAdapter.DifferenceSettings(TemporalType.PlainYearMonth, operation, options, typeName);
        return TemporalAdapter.DurationOf(TemporalDifferences.DifferenceTemporalPlainYearMonth(operation, TemporalAdapter.Calendars,
            TemporalAdapter.CalendarIdOf(calendarId), IsoDate, target.IsoDate, settings, out var result), result, typeName);
    }

    [JSExport("equals", Length = 1)]
    public JSValue Equals(in Arguments a)
    {
        var other = Require(ToTemporalYearMonth(a.GetAt(0)));
        return isoYear == other.isoYear && isoMonth == other.isoMonth && referenceISODay == other.referenceISODay
            && calendarId == other.calendarId
            ? BooleanTrue : BooleanFalse;
    }

    [JSExport("toPlainDate", Length = 1)]
    public JSValue ToPlainDate(in Arguments a)
    {
        if (a.GetAt(0) is not JSObject obj)
            throw JSEngine.NewTypeError("Temporal.PlainYearMonth.prototype.toPlainDate requires an object");

        // PrepareCalendarFields of the argument's « day », then the year-month's fields merged with it and
        // CalendarDateFromFields under constrain: the shared library's.
        var fields = new TemporalAdapter.CalendarFieldReader(obj, calendarId, "Temporal.PlainYearMonth.prototype.toPlainDate", readsYear: false);
        fields.Day();
        return new JSTemporalPlainDate(TemporalAdapter.YearMonthToDate(calendarId, IsoDate, fields.Fields), calendarId,
            JSTemporalPlainDate.PlainDatePrototype);
    }

    // TemporalYearMonthToString, the shared library's, with the calendarName option: the reference day is
    // written where the calendar is not iso8601 or the annotation is forced.
    [JSExport("toString", Length = 0)]
    public JSValue ToStringMethod(in Arguments a)
        => new JSString(TemporalFormat.YearMonthToString(IsoDate, calendarId, TemporalAdapter.ReadShowCalendarName(a.GetAt(0))));

    [JSExport("toJSON", Length = 0)]
    public JSValue ToJSON(in Arguments a) => new JSString(TemporalFormat.YearMonthToString(IsoDate, calendarId, ShowCalendarName.Auto));

    [JSExport("toLocaleString", Length = 0)]
    public JSValue ToLocaleString(in Arguments a)
        => Intl.JSIntlDateTimeFormat.TemporalToLocaleString(this, a.GetAt(0), a.GetAt(1));

    [JSExport("valueOf", Length = 0)]
    public JSValue ValueOf(in Arguments a)
        => throw JSEngine.NewTypeError("Called Temporal.PlainYearMonth.prototype.valueOf, which is not supported. Use Temporal.PlainYearMonth.compare for comparison.");

    // ── helpers ─────────────────────────────────────────────────────────────────

    private static JSTemporalPlainYearMonth Require(JSValue value)
        => value as JSTemporalPlainYearMonth ?? throw JSEngine.NewTypeError("expected a Temporal.PlainYearMonth");

    private static int ToIntegerWithTruncation(JSValue value)
    {
        if (value == null || value.IsUndefined) return 0;
        return (int)TemporalRoundingOptions.ToIntegerWithTruncation(value, "Temporal.PlainYearMonth: component must be finite");
    }

    // The constructor's required isoYear/isoMonth arguments are coerced with ToNumber first, so an
    // absent / undefined argument (NaN) is a RangeError rather than silently defaulting to 0.
    private static int ToIntegerWithTruncationArgument(JSValue value)
        => (int)TemporalRoundingOptions.ToIntegerWithTruncation(value ?? JSUndefined.Value, "Temporal.PlainYearMonth: component must be finite");

    private static JSValue ToTemporalYearMonth(JSValue item) => ToTemporalYearMonth(item, JSUndefined.Value);

    // `options` is the raw options argument; the overflow option is read at the spec-mandated point
    // (after the item's type is validated and its fields / string are read) so an invalid primitive
    // item throws a TypeError before the options bag is ever observed.
    private static JSValue ToTemporalYearMonth(JSValue item, JSValue options)
    {
        if (item is JSTemporalPlainYearMonth ym)
        {
            TemporalAdapter.ReadOverflow(options);
            return new JSTemporalPlainYearMonth(ym.isoYear, ym.isoMonth, ym.referenceISODay, ym.calendarId, PlainYearMonthPrototype);
        }

        if (item.IsString)
        {
            var parsed = ParseTemporalYearMonthString(item.ToString());
            TemporalAdapter.ReadOverflow(options);
            return parsed;
        }

        if (item is not JSObject obj)
            throw JSEngine.NewTypeError("Temporal.PlainYearMonth: invalid value");

        // GetTemporalCalendarIdentifierWithISODefault, then PrepareCalendarFields in property order - [era,
        // eraYear,] month, monthCode, year - each converted as it is read; then the overflow option; then
        // CalendarYearMonthFromFields, the shared library's.
        var calendarId = TemporalAdapter.CalendarWithIsoDefault(obj);
        var fields = new TemporalAdapter.CalendarFieldReader(obj, calendarId, "Temporal.PlainYearMonth");
        fields.Era();
        fields.Month();
        fields.MonthCode();
        fields.Year();

        var overflow = TemporalAdapter.ReadOverflow(options);
        return new JSTemporalPlainYearMonth(TemporalAdapter.YearMonthFromFields(calendarId, fields.Fields, overflow, "Temporal.PlainYearMonth"),
            calendarId, PlainYearMonthPrototype);
    }

    // ParseISODateTime with the TemporalYearMonthString goal (the shared library's grammar, which refuses a
    // year-month without a day in a calendar other than iso8601), the calendar annotation canonicalized, then
    // ISOYearMonthWithinLimits.
    private static JSValue ParseTemporalYearMonthString(string text)
    {
        var parsed = TemporalAdapter.ParseIsoString(text, [TemporalGoal.YearMonth], "Temporal.PlainYearMonth");
        var calendarId = TemporalAdapter.CalendarOf(parsed);
        var date = TemporalAdapter.DateOf(parsed);
        if (TemporalLimits.CheckIsoYearMonth(date) != TemporalStatus.Completed)
            throw JSEngine.NewRangeError($"Cannot parse Temporal.PlainYearMonth from \"{text}\"");

        // ISODateToFields as a year-month, then CalendarYearMonthFromFields under constrain, the shared library's:
        // the reference day is the first of the calendar month, in every calendar.
        return new JSTemporalPlainYearMonth(TemporalAdapter.YearMonthOfDate(calendarId, date), calendarId, PlainYearMonthPrototype);
    }
}
