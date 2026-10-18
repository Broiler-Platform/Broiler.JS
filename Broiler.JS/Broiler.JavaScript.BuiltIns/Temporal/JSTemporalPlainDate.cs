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
// date and calendar identifier. Its calendar fields, field resolution, addition and the calendar
// half of its differences are the shared library's calendars, through TemporalAdapter.
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

    // Whether the calendar's dates are not the ISO dates (neither iso8601 nor the Gregorian family): the differences'
    // rounding, part C's, still runs on ISO arithmetic for those alone.
    private bool NonIso => !TemporalAdapter.SharesIsoArithmetic(calendarId);

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

        var overflow = ReadOverflow(a.GetAt(1));
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
        var overflow = ReadOverflow(options);

        // ToDateDurationRecordWithoutTime: the days and the time fields as one exact time duration of
        // 24-hour days, its whole days truncated toward zero (row R7: no field is narrowed first).
        var days = (long)(TemporalAdapter.TimeDurationWith24HourDays(d) / TemporalLimits.NanosecondsPerDay);

        // CalendarDateAdd, the shared library's, in the date's calendar (row R7: no year is narrowed first).
        var added = TemporalAdapter.CalendarDateAdd(calendarId, IsoDate,
            sign * d.YearsValue, sign * d.MonthsValue, sign * d.WeeksValue, sign * days, overflow, "Temporal.PlainDate");
        return new JSTemporalPlainDate(added, calendarId, PlainDatePrototype);
    }

    [JSExport("until", Length = 1)]
    public JSValue Until(in Arguments a) => Difference(a.GetAt(0), a.GetAt(1), 1);

    [JSExport("since", Length = 1)]
    public JSValue Since(in Arguments a) => Difference(a.GetAt(0), a.GetAt(1), -1);

    // until/since honor largestUnit (day[default]/week/month/year) and round to smallestUnit at the
    // given roundingIncrement / roundingMode (GetDifferenceSettings + RoundRelativeDuration). Rounding
    // to a unit coarser than "day" (or with a non-1 increment) is applied for iso8601 and the Gregorian
    // family only; the other calendars return CalendarDateUntil's unrounded difference (row F4, part C's).
    private JSValue Difference(JSValue other, JSValue options, int sign)
    {
        var target = RequireDate(ToTemporalDate(other));
        if (calendarId != target.calendarId)
            throw JSEngine.NewRangeError("Temporal.PlainDate: cannot compute the difference between dates of different calendars");
        var (largestUnit, smallestUnit, increment, roundingMode) = ReadDifferenceSettings(options);

        // `since` is `-(this.until(other))`: the difference is always measured *from the receiver*
        // (so the year/month balancing anchors on the receiver's day-of-month), then negated for
        // since. Swapping the operands instead would re-anchor on the other date and give the wrong
        // result at month boundaries. CalendarDateUntil, the shared library's, outside the ISO dates.
        double years, months, weeks, days;
        if (NonIso)
        {
            (years, months, weeks, days) = TemporalAdapter.CalendarDateUntil(calendarId, IsoDate, target.IsoDate, largestUnit);
        }
        else
        {
            // The rounding mode is negated for "since" (the operands are negated), then the rounded
            // result is negated back below.
            var mode = sign == -1 ? ExactRounding.Negate(roundingMode) : roundingMode;
            (years, months, weeks, days) = DifferenceISODateRounded(isoYear, isoMonth, isoDay, target.isoYear, target.isoMonth, target.isoDay,
                largestUnit, smallestUnit, increment, mode);
        }

        if (sign == -1) { years = Negate(years); months = Negate(months); weeks = Negate(weeks); days = Negate(days); }

        return new JSTemporalDuration(years, months, weeks, days, 0, 0, 0, 0, 0, 0, JSTemporalDuration.DurationPrototype);
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

    private static string ReadOverflow(JSValue options)
    {
        if (options == null || options.IsUndefined)
            return "constrain";

        if (options is not JSObject optionsObject)
            throw JSEngine.NewTypeError("Temporal options must be an object or undefined");

        var v = optionsObject[KeyStrings.GetOrCreate("overflow")];
        if (v.IsUndefined) return "constrain";

        var overflow = v.StringValue;
        if (overflow is not ("constrain" or "reject"))
            throw JSEngine.NewRangeError($"Temporal: invalid overflow \"{overflow}\"");

        return overflow;
    }

    // GetDifferenceSettings for a date difference (until/since): validates largestUnit, smallestUnit,
    // roundingIncrement and roundingMode and returns the resolved largestUnit. The valid date units
    // are year > month > week > day; smallestUnit must not be larger than largestUnit. (Rounding
    // with a smallestUnit/increment beyond "day"/1 is validated here but not yet applied — see TODO.)
    private static readonly string[] DateUnits = ["year", "month", "week", "day"];

    private static (string largestUnit, string smallestUnit, int increment, RoundingMode roundingMode) ReadDifferenceSettings(JSValue options)
    {
        if (options == null || options.IsUndefined)
            return ("day", "day", 1, RoundingMode.Trunc);
        if (options is not JSObject o)
            throw JSEngine.NewTypeError("Temporal options must be an object or undefined");

        // GetDifferenceSettings reads largestUnit, roundingIncrement, roundingMode, smallestUnit in that
        // order, coercing each unit (time units included) before any is validated against the allowed
        // group, so a disallowed-unit RangeError is reported only after every option has been read.
        var largestRaw = o[KeyStrings.GetOrCreate("largestUnit")];
        var largestUnit = largestRaw.IsUndefined ? null : TemporalRoundingOptions.NormalizeAnyUnit(largestRaw.StringValue, allowAuto: true);
        if (largestUnit == "auto") largestUnit = null;

        var increment = TemporalRoundingOptions.GetRoundingIncrement(o);
        var roundingMode = TemporalRoundingOptions.GetRoundingMode(o, RoundingMode.Trunc);

        var smallestRaw = o[KeyStrings.GetOrCreate("smallestUnit")];
        var smallestUnit = smallestRaw.IsUndefined ? null : TemporalRoundingOptions.NormalizeAnyUnit(smallestRaw.StringValue, allowAuto: false);

        // Only date units (year > month > week > day) are valid for a PlainDate difference.
        if (largestUnit != null && System.Array.IndexOf(DateUnits, largestUnit) < 0)
            throw JSEngine.NewRangeError($"Temporal.PlainDate: invalid largestUnit \"{largestUnit}\"");
        if (smallestUnit != null && System.Array.IndexOf(DateUnits, smallestUnit) < 0)
            throw JSEngine.NewRangeError($"Temporal.PlainDate: invalid smallestUnit \"{smallestUnit}\"");

        smallestUnit ??= "day";
        largestUnit ??= System.Array.IndexOf(DateUnits, smallestUnit) < System.Array.IndexOf(DateUnits, "day")
            ? smallestUnit : "day";

        if (System.Array.IndexOf(DateUnits, smallestUnit) < System.Array.IndexOf(DateUnits, largestUnit))
            throw JSEngine.NewRangeError("Temporal.PlainDate: smallestUnit must not be larger than largestUnit");

        // ValidateTemporalRoundingIncrement: a date unit's rounding increment must be a positive integer
        // (the increment never reaches the next unit, so only the 1…∞ check from GetRoundingIncrement
        // applies — there is no fixed divisor as there is for the sub-day units).
        return (largestUnit, smallestUnit, increment, roundingMode);
    }

    private static JSValue ToTemporalDate(JSValue item) => ToTemporalDate(item, JSUndefined.Value);

    // `options` is the raw options argument; the overflow option is read at the spec-mandated point
    // (after the item's type is validated and its slots / fields / string are read) so an invalid
    // primitive item throws a TypeError before the options bag is ever observed.
    private static JSValue ToTemporalDate(JSValue item, JSValue options)
    {
        if (item is JSTemporalPlainDate d)
        {
            ReadOverflow(options);
            return new JSTemporalPlainDate(d.isoYear, d.isoMonth, d.isoDay, d.calendarId, PlainDatePrototype);
        }

        // A Temporal.ZonedDateTime / PlainDateTime is consumed via its internal slots (its wall-clock
        // ISO date + calendar), NOT via its observable getters — GetISODateTimeFor / the stored ISO
        // date + CreateTemporalDate. (Spec ToTemporalDate, ZonedDateTime / PlainDateTime branches.)
        if (item is JSTemporalZonedDateTime zdt)
        {
            var result = zdt.ToPlainDateFromSlots();
            ReadOverflow(options);
            return result;
        }

        if (item is JSTemporalPlainDateTime pdt)
        {
            ReadOverflow(options);
            return new JSTemporalPlainDate(pdt.isoYear, pdt.isoMonth, pdt.isoDay, pdt.calendarId, PlainDatePrototype);
        }

        if (item.IsString)
        {
            var parsed = ParseTemporalDateString(item.ToString());
            ReadOverflow(options);
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

        var overflow = ReadOverflow(options);
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

    // ── ISO 8601 calendar arithmetic ────────────────────────────────────────────

    // DifferenceISODate from a (start) to b (end), both with a >= b or a <= b resolved by sign.
    private static (double years, double months, double weeks, double days) DifferenceISODate(
        int ay, int am, int ad, int by, int bm, int bd, string largestUnit)
    {
        var startEpoch = IsoCalendarMath.EpochDays(ay, am, ad);
        var endEpoch = IsoCalendarMath.EpochDays(by, bm, bd);

        if (largestUnit is "day" or "week")
        {
            var totalDays = endEpoch - startEpoch;
            if (largestUnit == "week")
                return (0, 0, totalDays / 7, totalDays % 7);
            return (0, 0, 0, totalDays);
        }

        // largestUnit year or month: count whole years, then whole months, then the residual days.
        // Mirrors the Temporal reference's iso8601 dateUntil: a candidate year/month count "surpasses"
        // the end when the *unconstrained* (start.day-preserving) date passes it, so e.g. Jan 29 + 1
        // month is treated as Feb 29 (which surpasses Feb 28) and does not count as a whole month —
        // the residual is then measured from the day-constrained intermediate.
        var sign = -IsoArithmetic.CompareIsoDate(new IsoDate(ay, am, ad), new IsoDate(by, bm, bd)); // +1 when end is after start
        if (sign == 0) return (0, 0, 0, 0);

        long years = 0;
        var candidateYears = (long)by - ay;
        if (candidateYears != 0) candidateYears -= sign;
        while (!ISODateSurpasses(sign, ay, am, ad, by, bm, bd, candidateYears, 0))
        {
            years = candidateYears;
            candidateYears += sign;
        }

        long months = 0;
        var candidateMonths = (long)sign;
        while (!ISODateSurpasses(sign, ay, am, ad, by, bm, bd, years, candidateMonths))
        {
            months = candidateMonths;
            candidateMonths += sign;
        }

        if (largestUnit == "month")
        {
            months += years * 12;
            years = 0;
        }

        var (iy, im) = IsoCalendarMath.BalanceYearMonth(ay + years, (long)am + months);
        var cd = Math.Min(ad, IsoCalendarMath.DaysInMonth(iy, im));
        var days = IsoCalendarMath.EpochDays(by, bm, bd) - IsoCalendarMath.EpochDays(iy, im, cd);

        return (years, months, 0, days);
    }

    // ── RoundRelativeDuration over the ISO day axis (no time / no time zone) ─────

    // The ISO date difference start→end balanced to largestUnit, then rounded to smallestUnit at the
    // given increment / rounding mode. For the default (smallestUnit "day", increment 1) the exact
    // largestUnit-balanced difference is returned unchanged. Shared with Temporal.PlainYearMonth.
    internal static (double years, double months, double weeks, double days) DifferenceISODateRounded(
        int sy, int sm, int sd, int ey, int em, int ed,
        string largestUnit, string smallestUnit, int increment, RoundingMode roundingMode)
    {
        var (years, months, weeks, days) = DifferenceISODate(sy, sm, sd, ey, em, ed, largestUnit);
        if (smallestUnit == "day" && increment == 1)
            return (years, months, weeks, days);

        var (ry, rmo, rw, rd) = RoundDateDifference(sy, sm, sd, (long)years, (long)months, (long)weeks, (long)days,
            ey, em, ed, largestUnit, smallestUnit, increment, roundingMode);
        return (ry, rmo, rw, rd);
    }

    // Temporal.PlainDateTime calendar/day rounding: rounds an already-computed (day-borrowed) date
    // duration toward `smallestUnit`, with the start (`startTimeNs`) and destination (`destTimeNs`)
    // wall-clock times of day folding into the boundary progress (measured in nanoseconds). The end
    // date `ey/em/ed` is the destination's actual ISO date. Returns only date units (the sub-unit
    // time is consumed by the rounding).
    internal static (long y, long mo, long w, long d) RoundDateTimeDateDifference(
        int sy, int sm, int sd, long years, long months, long weeks, long days,
        int ey, int em, int ed, long startTimeNs, long destTimeNs,
        string largestUnit, string smallestUnit, int increment, RoundingMode roundingMode)
        => RoundDateDifference(sy, sm, sd, years, months, weeks, days, ey, em, ed,
            largestUnit, smallestUnit, increment, roundingMode, startTimeNs, destTimeNs);

    private static int DateDurationSign(long y, long mo, long w, long d)
        => y != 0 ? Math.Sign(y) : mo != 0 ? Math.Sign(mo) : w != 0 ? Math.Sign(w) : d != 0 ? Math.Sign(d) : 0;

    // The epoch-day number reached by adding a calendar date duration to the start date (constrain).
    internal static long AddDateGetDays(int sy, int sm, int sd, long y, long mo, long w, long d)
    {
        var r = TemporalAdapter.AddIsoDate(new IsoDate(sy, sm, sd), new DateDuration(y, mo, w, d), "constrain", "Temporal.PlainDate");
        return IsoCalendarMath.EpochDays(r.Year, r.Month, r.Day);
    }

    // NudgeToCalendarUnit + BubbleRelativeDuration: bracket the difference between the start (r1) and end
    // (r2) boundaries in the smallest unit (measured on the day axis), pick one per the rounding mode,
    // then roll an expanded unit up toward largestUnit.
    private static (long y, long mo, long w, long d) RoundDateDifference(
        int sy, int sm, int sd, long years, long months, long weeks, long days,
        int ey, int em, int ed, string largestUnit, string smallestUnit, int increment, RoundingMode roundingMode,
        long startTimeNs = 0, long destTimeNs = 0)
    {
        const long NsPerDay = 86_400_000_000_000;
        var destDays = IsoCalendarMath.EpochDays(ey, em, ed);

        // The overall sign is taken on the nanosecond axis so a same-date, time-only PlainDateTime
        // difference (date duration zero) still rounds in the right direction.
        var aNs = (System.Numerics.BigInteger)IsoCalendarMath.EpochDays(sy, sm, sd) * NsPerDay + startTimeNs;
        var fullDestNs = (System.Numerics.BigInteger)destDays * NsPerDay + destTimeNs;
        var sign = fullDestNs > aNs ? 1 : fullDestNs < aNs ? -1 : 0;
        if (sign == 0) return (0, 0, 0, 0);

        // r1 is RoundNumberToIncrement(units, increment, "trunc"), the shared library's.
        long TruncateToIncrement(long units)
            => (long)ExactRounding.RoundNumberToIncrement(units, System.Numerics.BigInteger.One, increment, RoundingMode.Trunc);

        long r1;
        (long y, long mo, long w, long d) startDur, endDur;
        switch (smallestUnit)
        {
            case "year":
                r1 = TruncateToIncrement(years);
                startDur = (r1, 0, 0, 0); endDur = (r1 + increment * sign, 0, 0, 0);
                break;
            case "month":
                r1 = TruncateToIncrement(months);
                startDur = (years, r1, 0, 0); endDur = (years, r1 + increment * sign, 0, 0);
                break;
            case "week":
            {
                // The weeks count includes the days residual beyond the years+months part.
                var afterYM = AddDateGetDays(sy, sm, sd, years, months, 0, 0);
                var (wy, wm, wd) = IsoCalendarMath.FromEpochDays(afterYM);
                var (wey, wem, wed) = IsoCalendarMath.FromEpochDays(afterYM + days);
                var until = DifferenceISODate((int)wy, wm, wd, (int)wey, wem, wed, "week");
                var totalWeeks = weeks + (long)until.weeks;
                r1 = TruncateToIncrement(totalWeeks);
                startDur = (years, months, r1, 0); endDur = (years, months, r1 + increment * sign, 0);
                break;
            }
            default: // day
                r1 = TruncateToIncrement(days);
                startDur = (years, months, weeks, r1); endDur = (years, months, weeks, r1 + (long)increment * sign);
                break;
        }

        // The years+months+weeks anchor of the start (r1) boundary on the epoch-day axis.
        long startDays;
        long anchorDays = 0;
        if (smallestUnit == "day")
        {
            // Day boundaries are linear on the epoch-day axis from the anchor (years+months+weeks), so
            // they are computed arithmetically below — a large increment must not build an out-of-range
            // intermediate date (the rounded result is just a day count, e.g. P1000000000D).
            anchorDays = AddDateGetDays(sy, sm, sd, years, months, weeks, 0);
            startDays = anchorDays + r1;
        }
        else
        {
            startDays = DateDurationSign(startDur.y, startDur.mo, startDur.w, startDur.d) == 0
                ? IsoCalendarMath.EpochDays(sy, sm, sd)
                : AddDateGetDays(sy, sm, sd, startDur.y, startDur.mo, startDur.w, startDur.d);
        }

        // Progress between the boundaries is measured on the nanosecond axis (the start time of day
        // is baked into both boundaries, the destination time into `destNs`), so a PlainDateTime's
        // sub-day component participates in the rounding; a pure-date PlainDate passes time 0 and the
        // ratio reduces to the day axis.
        var startNs = (System.Numerics.BigInteger)startDays * NsPerDay + startTimeNs;

        // Build the end (next-increment) boundary. Per spec (ComputeNudgeWindow → CalendarDateAdd with
        // ?), this is computed unconditionally, so a rounding increment large enough to push the boundary
        // past the valid ISO range surfaces the RangeError that AddDateGetDays throws here — even when the
        // difference itself is exact (test262 …/since|until/throws-if-rounded-date-outside-valid-iso-range).
        var endDays = smallestUnit == "day"
            ? anchorDays + r1 + (long)increment * sign
            : AddDateGetDays(sy, sm, sd, endDur.y, endDur.mo, endDur.w, endDur.d);
        var endNs = (System.Numerics.BigInteger)endDays * NsPerDay + startTimeNs;

        // An exact difference (the destination sits on the start boundary) rounds to the start boundary.
        if (startNs == fullDestNs)
            return startDur;

        var didExpand = false;
        if (smallestUnit is "year" or "month")
        {
            // A calendar irregularity (e.g. a leap month) may put the end outside the bracket; shift it.
            var inBounds = sign == 1
                ? startNs <= fullDestNs && fullDestNs <= endNs
                : endNs <= fullDestNs && fullDestNs <= startNs;
            if (!inBounds)
            {
                r1 += increment * sign;
                if (smallestUnit == "year") { startDur = (r1, 0, 0, 0); endDur = (r1 + increment * sign, 0, 0, 0); }
                else { startDur = (years, r1, 0, 0); endDur = (years, r1 + increment * sign, 0, 0); }
                startDays = AddDateGetDays(sy, sm, sd, startDur.y, startDur.mo, startDur.w, startDur.d);
                endDays = AddDateGetDays(sy, sm, sd, endDur.y, endDur.mo, endDur.w, endDur.d);
                startNs = (System.Numerics.BigInteger)startDays * NsPerDay + startTimeNs;
                endNs = (System.Numerics.BigInteger)endDays * NsPerDay + startTimeNs;
                didExpand = true;
            }
        }

        // NudgeToCalendarUnit: progress = (dest − start) / (end − start), total = r1 + progress ×
        // increment × sign. A progress of 1 is the window's end; any other progress is rounded over the
        // window [|r1|, |r2|] by the shared library's ApplyUnsignedRoundingMode.
        var progressed = System.Numerics.BigInteger.Abs(fullDestNs - startNs);
        var span = System.Numerics.BigInteger.Abs(endNs - startNs);
        var (totalNumerator, totalDenominator) = TemporalRoundingOptions.NudgeTotal(r1, increment, sign, progressed, span);
        var isEnd = progressed == span
            || TemporalRoundingOptions.NudgeRoundsToEnd(totalNumerator, totalDenominator, r1, r1 + (long)increment * sign, sign, roundingMode);

        didExpand = didExpand || isEnd;
        var chosen = isEnd ? endDur : startDur;
        var nudgedDays = isEnd ? endDays : startDays;

        if (didExpand && smallestUnit != "week")
            chosen = BubbleDateDuration(sign, sy, sm, sd, chosen, nudgedDays, largestUnit,
                System.Array.IndexOf(DateUnits, smallestUnit) <= System.Array.IndexOf(DateUnits, "day") ? smallestUnit : "day");

        return chosen;
    }

    private static (long y, long mo, long w, long d) BubbleDateDuration(
        int sign, int sy, int sm, int sd, (long y, long mo, long w, long d) dur, long nudgedDays,
        string largestUnit, string smallestUnit)
    {
        if (smallestUnit == largestUnit) return dur;

        var largestIdx = System.Array.IndexOf(DateUnits, largestUnit);
        var smallestIdx = System.Array.IndexOf(DateUnits, smallestUnit);
        for (var idx = smallestIdx - 1; idx >= largestIdx; idx--)
        {
            var unit = DateUnits[idx];
            if (unit == "week" && largestUnit != "week") continue;

            (long y, long mo, long w, long d) endDur = unit switch
            {
                "year" => (dur.y + sign, 0, 0, 0),
                "month" => (dur.y, dur.mo + sign, 0, 0),
                "week" => (dur.y, dur.mo, dur.w + sign, 0),
                _ => dur,
            };

            var endDays = AddDateGetDays(sy, sm, sd, endDur.y, endDur.mo, endDur.w, endDur.d);
            var didExpandToEnd = nudgedDays.CompareTo(endDays) != -sign;
            if (didExpandToEnd) dur = endDur;
            else break;
        }
        return dur;
    }

    // ISODateSurpasses: true when adding `years` (and then `months`) to the start date — keeping the
    // *unconstrained* start day — produces a date that passes the end in the direction of `sign`.
    private static bool ISODateSurpasses(int sign, int sy, int sm, int sd, int ey, int em, int ed, long years, long months)
    {
        var y0 = sy + years;
        if (CompareSurpasses(sign, y0, sm, sd, ey, em, ed)) return true;
        if (months == 0) return false;
        var (my, mm) = IsoCalendarMath.BalanceYearMonth(y0, (long)sm + months);
        return CompareSurpasses(sign, my, mm, sd, ey, em, ed);
    }

    // True when (year, month, day) lies strictly beyond (ty, tm, td) in the direction of `sign`.
    private static bool CompareSurpasses(int sign, long year, int month, int day, int ty, int tm, int td)
    {
        if (year != ty) return sign * (year - ty) > 0;
        if (month != tm) return sign * (month - tm) > 0;
        if (day != td) return sign * (day - td) > 0;
        return false;
    }

    // ── internal calendar primitives reused by Temporal.Duration relativeTo rounding ──────────
    // These expose the (already-tested) ISO epoch-day arithmetic for the ISO and Gregorian-family
    // calendars, which share the ISO month/day structure.

    // Converts a Temporal.Duration relativeTo option (a PlainDate, or a string / property bag that
    // resolves to one) to a PlainDate.
    internal static JSTemporalPlainDate ToRelativeDate(JSValue item)
        => (JSTemporalPlainDate)ToTemporalDate(item);

    // A PlainDate from ISO fields (used when a Temporal.Duration relativeTo is a PlainDateTime, whose
    // date is taken with a fixed 24-hour day).
    internal static JSTemporalPlainDate FromIso(int year, int month, int day, string calendarId)
        => new JSTemporalPlainDate(year, month, day, calendarId, PlainDatePrototype);

    internal static long EpochDaysFor(int year, int month, int day) => IsoCalendarMath.EpochDays(year, month, day);

    internal static (int y, int m, int d) DateFromEpochDays(long epoch)
    {
        var (y, m, d) = IsoCalendarMath.FromEpochDays(epoch);
        return ((int)y, m, d);
    }

    // relativeTo + (years, months, weeks, days) using the ISO "constrain" overflow; throws a
    // RangeError if the result leaves the representable range.
    internal static (int y, int m, int d) AddCalendarDate(int year, int month, int day,
        long years, long months, long weeks, long days)
    {
        var r = TemporalAdapter.AddIsoDate(new IsoDate(year, month, day), new DateDuration(years, months, weeks, days), "constrain", "Temporal.PlainDate");
        return ((int)r.Year, r.Month, r.Day);
    }

    internal static (double years, double months, double weeks, double days) DiffCalendarDate(
        int ay, int am, int ad, int by, int bm, int bd, string largestUnit)
        => DifferenceISODate(ay, am, ad, by, bm, bd, largestUnit);

    // Negates a duration component, mapping +0 to +0 (rather than -0) so a zero field of a `since`
    // result compares SameValue with the +0 the spec produces.
    internal static double Negate(double v) => v == 0 ? 0d : -v;
}
