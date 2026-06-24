using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Broiler.JavaScript.BuiltIns.Function;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Temporal;

// Temporal.PlainYearMonth (Temporal proposal §9): a calendar year and month (e.g. "2024-06"),
// stored as the ISO date of day 1 of that calendar month (the reference ISO day round-trips the
// month for the non-ISO calendars). Supports the proleptic-Gregorian family plus the non-Gregorian
// calendars wired in via TemporalCalendarMath / TemporalNonIso (shared with PlainDate /
// PlainDateTime). Registered under the Temporal namespace via Register = false.
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
        calendarId = TemporalCalendar.ResolveCalendarIdentifierArgument(a.GetAt(2), "Temporal.PlainYearMonth");
        var refDay = a.GetAt(3);
        referenceISODay = refDay == null || refDay.IsUndefined ? 1 : ToIntegerWithTruncation(refDay);

        if (!IsValidISODate(isoYear, isoMonth, referenceISODay) || !ISOYearMonthWithinLimits(isoYear, isoMonth))
            throw JSEngine.NewRangeError("Temporal.PlainYearMonth: invalid ISO year-month");
    }

    internal JSTemporalPlainYearMonth(int isoYear, int isoMonth, int referenceISODay, JSObject prototype)
        : this(isoYear, isoMonth, referenceISODay, "iso8601", prototype) { }

    internal JSTemporalPlainYearMonth(int isoYear, int isoMonth, int referenceISODay, string calendarId, JSObject prototype) : base(prototype)
    {
        this.isoYear = isoYear; this.isoMonth = isoMonth; this.referenceISODay = referenceISODay;
        this.calendarId = calendarId;
    }

    private static JSObject ResolvePrototype()
    {
        if (JSEngine.NewTarget == null && (JSEngine.Current as IJSExecutionContext)?.CurrentNewTarget == null)
            throw JSEngine.NewTypeError("Constructor Temporal.PlainYearMonth requires 'new'");

        return JSEngine.NewTargetPrototype ?? PlainYearMonthPrototype;
    }

    internal static JSObject PlainYearMonthPrototype
    {
        get
        {
            var temporal = (JSEngine.Current as JSObject)?[KeyStrings.GetOrCreate("Temporal")] as JSObject;
            return (temporal?[KeyStrings.GetOrCreate("PlainYearMonth")] as JSFunction)?.prototype;
        }
    }

    // ── accessors ───────────────────────────────────────────────────────────────

    [JSExport("calendarId")] public JSValue CalendarId => new JSString(calendarId);

    // True for the non-Gregorian calendars whose month/day structure differs from ISO; every field
    // is then derived from the stored ISO date via TemporalCalendarMath (see PlainDate).
    private bool NonIso => TemporalCalendarMath.IsNonIso(calendarId);

    // The stored ISO date projected into the active calendar's (year, month, day).
    private (int y, int m, int d) CalendarYmd() => NonIso
        ? TemporalNonIso.CalendarYmd(calendarId, isoYear, isoMonth, referenceISODay)
        : (isoYear, isoMonth, referenceISODay);

    [JSExport("era")] public JSValue Era
    {
        get
        {
            if (!NonIso) return TemporalCalendar.Era(calendarId, isoYear, isoMonth, referenceISODay);
            if (!TemporalCalendarMath.HasEra(calendarId)) return JSUndefined.Value;
            return new JSString(TemporalCalendarMath.Era(calendarId, CalendarYmd().y).code);
        }
    }
    [JSExport("eraYear")] public JSValue EraYear
    {
        get
        {
            if (!NonIso) return TemporalCalendar.EraYear(calendarId, isoYear, isoMonth, referenceISODay);
            if (!TemporalCalendarMath.HasEra(calendarId)) return JSUndefined.Value;
            return new JSNumber(TemporalCalendarMath.Era(calendarId, CalendarYmd().y).eraYear);
        }
    }
    [JSExport("year")] public double YearValue => NonIso ? CalendarYmd().y : TemporalCalendar.Year(calendarId, isoYear);
    [JSExport("month")] public double MonthValue => NonIso ? CalendarYmd().m : isoMonth;
    [JSExport("monthCode")] public JSValue MonthCode
    {
        get { if (NonIso) { var c = CalendarYmd(); return new JSString(TemporalCalendarMath.MonthCode(calendarId, c.y, c.m)); } return new JSString($"M{isoMonth:00}"); }
    }
    [JSExport("daysInMonth")] public double DaysInMonth
    {
        get { if (NonIso) { var c = CalendarYmd(); return TemporalCalendarMath.DaysInMonth(calendarId, c.y, c.m); } return DaysInMonthOf(isoYear, isoMonth); }
    }
    [JSExport("daysInYear")] public double DaysInYear => NonIso
        ? TemporalCalendarMath.DaysInYear(calendarId, CalendarYmd().y)
        : (IsLeapYear(isoYear) ? 366 : 365);
    [JSExport("monthsInYear")] public double MonthsInYear => NonIso
        ? TemporalCalendarMath.MonthsInYear(calendarId, CalendarYmd().y)
        : 12;
    [JSExport("inLeapYear")] public bool InLeapYear => NonIso
        ? TemporalCalendarMath.InLeapYear(calendarId, CalendarYmd().y)
        : IsLeapYear(isoYear);

    // ── statics ─────────────────────────────────────────────────────────────────

    [JSExport("from", Length = 1)]
    internal static JSValue From(in Arguments a)
        => ToTemporalYearMonth(a.GetAt(0), a.GetAt(1));

    [JSExport("compare", Length = 2)]
    internal static JSValue Compare(in Arguments a)
    {
        var one = Require(ToTemporalYearMonth(a.GetAt(0)));
        var two = Require(ToTemporalYearMonth(a.GetAt(1)));
        return new JSNumber(CompareISODate(one.isoYear, one.isoMonth, one.referenceISODay,
            two.isoYear, two.isoMonth, two.referenceISODay));
    }

    // ── methods ─────────────────────────────────────────────────────────────────

    [JSExport("with", Length = 1)]
    public JSValue With(in Arguments a)
    {
        if (a.GetAt(0) is not JSObject obj)
            throw JSEngine.NewTypeError("Temporal.PlainYearMonth.prototype.with requires an object");
        TemporalCalendar.RejectObjectWithCalendarOrTimeZone(obj);

        if (NonIso)
            return WithNonIso(obj, ReadOverflow(a.GetAt(1)));

        var any = false;
        var month = isoMonth;

        // PrepareCalendarFields reads the recognised fields alphabetically — month,
        // monthCode, year — coercing each as it is read. (ResolveWithYear reads year,
        // and era/eraYear for calendars with eras, last.)
        var monthValue = obj[KeyStrings.GetOrCreate("month")];
        var monthFromMonth = -1;
        if (!monthValue.IsUndefined) { monthFromMonth = ToPositiveIntegerWithTruncation(monthValue); any = true; }

        // Coerce monthCode now; defer parsing/validating it against the calendar until after
        // the overflow option is read (test262 .../with options-read-before-algorithmic-validation).
        var monthCodeValue = obj[KeyStrings.GetOrCreate("monthCode")];
        var monthCodeStr = TemporalIsoString.RequireMonthCodeString(monthCodeValue, "Temporal.PlainYearMonth");
        if (monthCodeStr != null) any = true;

        var year = ResolveWithYear(obj, ref any);

        if (!any)
            throw JSEngine.NewTypeError("Temporal.PlainYearMonth.prototype.with requires at least one field");

        // GetTemporalOverflowOption runs only after the partial fields have been read and coerced.
        var overflow = ReadOverflow(a.GetAt(1));

        if (monthCodeStr != null) month = MonthFromCode(monthCodeStr);

        if (monthFromMonth != -1)
        {
            if (monthCodeStr != null && monthFromMonth != month)
                throw JSEngine.NewRangeError("Temporal.PlainYearMonth.with: month and monthCode disagree");
            month = monthFromMonth;
        }

        return RegulateYearMonth(year, month, overflow, calendarId);
    }

    // with() for a non-Gregorian calendar: merge the provided year / era / eraYear / month /
    // monthCode fields over the receiver's calendar (year, month), then re-anchor on day 1.
    private JSValue WithNonIso(JSObject obj, string overflow)
    {
        var c = CalendarYmd();
        var any = false;

        var yearValue = obj[KeyStrings.GetOrCreate("year")];
        var eraValue = obj[KeyStrings.GetOrCreate("era")];
        var eraYearValue = obj[KeyStrings.GetOrCreate("eraYear")];
        var hasYear = !yearValue.IsUndefined;
        var hasEra = !eraValue.IsUndefined;
        var hasEraYear = !eraYearValue.IsUndefined;

        int year;
        if (!hasYear && !hasEra && !hasEraYear)
            year = c.y;
        else if (hasYear)
        {
            any = true;
            year = ToIntegerWithTruncation(yearValue);
        }
        else
        {
            any = true;
            if (!TemporalCalendarMath.HasEra(calendarId))
                throw JSEngine.NewTypeError($"Temporal: the {calendarId} calendar does not use eras");
            // era and eraYear must be supplied *together* — providing eraYear ignores (does not
            // complete) the receiver's era, so a partial pair is a TypeError.
            if (!hasEra || !hasEraYear)
                throw JSEngine.NewTypeError("Temporal: era and eraYear must be provided together");
            year = TemporalCalendarMath.YearFromEra(calendarId, eraValue.StringValue, ToIntegerWithTruncation(eraYearValue));
        }

        var monthCodeValue = obj[KeyStrings.GetOrCreate("monthCode")];
        var monthCodeStrW = TemporalIsoString.RequireMonthCodeString(monthCodeValue, "Temporal.PlainYearMonth");
        var monthValue = obj[KeyStrings.GetOrCreate("month")];
        int month;
        if (monthCodeStrW != null)
        {
            var (codeNumber, leapMonth) = ParseMonthCodeLeap(monthCodeStrW);
            month = TemporalCalendarMath.OrdinalFromMonthCode(calendarId, year, codeNumber, leapMonth);
            any = true;
        }
        else
        {
            // No month/monthCode in the bag: the receiver's *monthCode* (not its ordinal) carries
            // over and is re-resolved against the (possibly changed) year. In a lunisolar calendar a
            // leap month shifts later ordinals, so e.g. monthCode "M12" is ordinal 12 in a common year
            // but 13 in a leap year — keeping the bare ordinal would silently pick the wrong month
            // when era/eraYear/year moves to a year with a different month structure (test262
            // PlainYearMonth/prototype/with mutually-exclusive-fields-hebrew). A *leap* receiver month
            // (e.g. hebrew "M05L", chinese/dangi "MnnL") whose leap month is absent in the target year
            // constrains to the matching regular month under overflow "constrain" and is a RangeError
            // under "reject" (test262 .../with/leap-months-{hebrew,chinese,dangi}); this is exactly the
            // year-shift code-preservation handled by ResolveMonthAfterYearShift.
            month = TemporalNonIso.ResolveMonthAfterYearShift(calendarId, c.y, c.m, year, overflow);
        }
        if (!monthValue.IsUndefined)
        {
            var m = ToPositiveIntegerWithTruncation(monthValue);
            if (!monthCodeValue.IsUndefined && m != month)
                throw JSEngine.NewRangeError("Temporal.PlainYearMonth.with: month and monthCode disagree");
            month = m; any = true;
        }

        if (!any)
            throw JSEngine.NewTypeError("Temporal.PlainYearMonth.prototype.with requires at least one field");

        var (iy, im, id) = TemporalNonIso.RegulateToIso(calendarId, year, month, 1, overflow, enforceIsoDateRange: false);
        if (!ISOYearMonthWithinLimits(iy, im))
            throw JSEngine.NewRangeError("Temporal.PlainYearMonth: year-month is out of range");
        return new JSTemporalPlainYearMonth(iy, im, id, calendarId, PlainYearMonthPrototype);
    }

    // Parses "M01".."M13" with an optional trailing "L" leap-month marker (for the non-ISO path).
    private static (int number, bool leap) ParseMonthCodeLeap(string code)
    {
        var match = Regex.Match(code, @"^M(\d{2})(L?)$");
        if (!match.Success)
            throw JSEngine.NewRangeError($"Temporal: invalid monthCode \"{code}\"");
        return (int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture), match.Groups[2].Value == "L");
    }

    private int ResolveWithYear(JSObject obj, ref bool any)
    {
        var yearValue = obj[KeyStrings.GetOrCreate("year")];

        // The ISO calendar has no eras: era / eraYear are not fields, so they are ignored and only
        // `year` (defaulting to the receiver's) resolves the year.
        if (calendarId == "iso8601")
        {
            if (yearValue.IsUndefined) return isoYear;
            any = true;
            return ToIntegerWithTruncation(yearValue);
        }

        var eraValue = obj[KeyStrings.GetOrCreate("era")];
        var eraYearValue = obj[KeyStrings.GetOrCreate("eraYear")];

        if (yearValue.IsUndefined && eraValue.IsUndefined && eraYearValue.IsUndefined)
            return isoYear;

        any = true;
        var hasYear = !yearValue.IsUndefined;
        var hasEra = !eraValue.IsUndefined;
        var hasEraYear = !eraYearValue.IsUndefined;

        string era = null;
        var eraYear = 0;
        if (hasYear)
        {
            hasEra = hasEraYear = false;
        }
        else if (hasEra || hasEraYear)
        {
            // era and eraYear must be supplied *together* — providing eraYear ignores (does not
            // complete) the receiver's era, so a partial pair is a TypeError (via ResolveIsoYear).
            era = hasEra ? eraValue.StringValue : null;
            eraYear = hasEraYear ? ToIntegerWithTruncation(eraYearValue) : 0;
        }

        return TemporalCalendar.ResolveIsoYear(calendarId,
            hasYear, hasYear ? ToIntegerWithTruncation(yearValue) : 0,
            hasEra, era, hasEraYear, eraYear);
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

        // AddDurationToYearMonth: only the year and month components may be non-zero — a duration that
        // carries any weeks, days or sub-day time is a RangeError (the result is a year-month, so there
        // is nowhere for finer units to go).
        if (d.WeeksValue != 0 || d.DaysValue != 0 || d.HoursValue != 0 || d.MinutesValue != 0
            || d.SecondsValue != 0 || d.MillisecondsValue != 0 || d.MicrosecondsValue != 0 || d.NanosecondsValue != 0)
            throw JSEngine.NewRangeError("Temporal.PlainYearMonth: only years and months can be added or subtracted");

        var years = sign * (long)d.YearsValue;
        var months = sign * (long)d.MonthsValue;

        // The years/months are always added to the *first* day of the receiver's month (the spec uses a
        // reference day of 1), so the day can never overflow the resulting month; overflow only governs
        // how the year/month themselves are resolved in the target calendar (e.g. a leap month absent in
        // the target year is constrained or rejected).
        if (NonIso)
        {
            var c = CalendarYmd();
            var (ay, amo, ad) = TemporalNonIso.AddToIso(calendarId, c.y, c.m, 1,
                years, months, 0, 0, overflow);
            return YearMonthFromIsoDate(ay, amo, ad, calendarId);
        }

        // The reference date is the first of the receiver's month. It must itself be a valid ISO date
        // within the representable range — the boundary months -271821-04 and +275760-09 only partially
        // overlap it, so e.g. (-271821-04).add(zero) starts from -271821-04-01, which is before the
        // minimum ISO date and is a RangeError.
        const int startDay = 1;
        if (!IsWithinIsoDateLimits(isoYear, isoMonth, startDay))
            throw JSEngine.NewRangeError("Temporal.PlainYearMonth: date is out of range");

        var total = (long)isoMonth - 1 + months;
        var ny = (int)(isoYear + years + FloorDiv(total, 12));
        var nm = (int)(((total % 12) + 12) % 12) + 1;

        return RegulateYearMonth(ny, nm, overflow, calendarId);
    }

    // Builds a PlainYearMonth from an ISO date by taking the calendar (year, month) that ISO date
    // falls in and re-anchoring on day 1 of that calendar month (the stored reference ISO day).
    private static JSValue YearMonthFromIsoDate(int isoY, int isoM, int isoD, string calendarId)
    {
        var c = TemporalNonIso.CalendarYmd(calendarId, isoY, isoM, isoD);
        var (iy, im, id) = TemporalNonIso.RegulateToIso(calendarId, c.y, c.m, 1, "constrain", enforceIsoDateRange: false);
        if (!ISOYearMonthWithinLimits(iy, im))
            throw JSEngine.NewRangeError("Temporal.PlainYearMonth: year-month is out of range");
        return new JSTemporalPlainYearMonth(iy, im, id, calendarId, PlainYearMonthPrototype);
    }

    [JSExport("until", Length = 1)]
    public JSValue Until(in Arguments a) => Difference(a.GetAt(0), a.GetAt(1), 1);

    [JSExport("since", Length = 1)]
    public JSValue Since(in Arguments a) => Difference(a.GetAt(0), a.GetAt(1), -1);

    // until/since honor largestUnit (year[default]/month) and round to smallestUnit at the given
    // roundingIncrement / roundingMode. Both year-months are anchored at day 1 of their month (per
    // DifferenceTemporalPlainYearMonth), so the difference reduces to a date difference. Rounding is
    // applied for the ISO calendar only; the arithmetic calendars return the unrounded difference.
    private JSValue Difference(JSValue other, JSValue options, int sign)
    {
        var target = Require(ToTemporalYearMonth(other));
        if (calendarId != target.calendarId)
            throw JSEngine.NewRangeError("Temporal.PlainYearMonth: cannot compute the difference between year-months of different calendars");
        var (largestUnit, smallestUnit, increment, roundingMode) = ReadDifferenceSettings(options);

        if (NonIso)
        {
            // The difference is always measured *from the receiver* (anchoring the year/month count on
            // the receiver's month-day), then negated for "since" — swapping the operands instead would
            // re-anchor on the other year-month and miscount at leap-month boundaries. Both year-months
            // sit at day 1.
            var self = CalendarYmd();
            var oth = target.CalendarYmd();
            var (cy, cm, _, _) = TemporalNonIso.Difference(calendarId, self.y, self.m, 1, oth.y, oth.m, 1, largestUnit);
            if (sign == -1) { cy = JSTemporalPlainDate.Negate(cy); cm = JSTemporalPlainDate.Negate(cm); }
            return new JSTemporalDuration(cy, cm, 0, 0, 0, 0, 0, 0, 0, 0, JSTemporalDuration.DurationPrototype);
        }

        // DifferenceTemporalPlainYearMonth anchors both year-months at day 1 and forms an intermediate
        // PlainDate for each, which must lie within the representable ISO date range. At the boundary
        // months the year-month range is wider than the date range (e.g. -271821-04 is a valid
        // year-month but -271821-04-01 is not a valid date), so a difference against a different
        // year-month throws RangeError. Equal year-months short-circuit to a zero duration below
        // (no intermediate date is formed), so they never reach this check.
        var sameYearMonth = isoYear == target.isoYear && isoMonth == target.isoMonth;
        if (!sameYearMonth
            && (!IsWithinIsoDateLimits(isoYear, isoMonth, 1) || !IsWithinIsoDateLimits(target.isoYear, target.isoMonth, 1)))
            throw JSEngine.NewRangeError("Temporal.PlainYearMonth: difference is outside the supported ISO date range");

        // The difference is measured from the receiver (day 1) and negated for "since"; the rounding mode
        // is correspondingly negated before rounding (GetDifferenceSettings).
        var mode = sign == -1 ? TemporalRoundingOptions.NegateRoundingMode(roundingMode) : roundingMode;

        double years, months;
        if (smallestUnit == "month" && increment == 1)
        {
            // DifferenceTemporalPlainYearMonth step 16 only invokes RoundRelativeDuration when
            // smallestUnit ≠ month OR roundingIncrement ≠ 1. Both year-months sit on day 1, so the
            // difference is already exact in whole months: skip rounding entirely. This also avoids
            // building the next-increment rounding boundary, which for a year-month at the ISO limit
            // (e.g. since('+275760-09')) would be out of range (issue #794) — whereas a coarser unit or
            // a larger increment DOES round and a boundary past the limit then throws (issue #857).
            (years, months, _, _) = JSTemporalPlainDate.DiffCalendarDate(
                isoYear, isoMonth, 1, target.isoYear, target.isoMonth, 1, largestUnit);
        }
        else
        {
            (years, months, _, _) = JSTemporalPlainDate.DifferenceISODateRounded(
                isoYear, isoMonth, 1, target.isoYear, target.isoMonth, 1, largestUnit, smallestUnit, increment, mode);
        }

        if (sign == -1) { years = -years; months = -months; }

        return new JSTemporalDuration(years, months, 0, 0, 0, 0, 0, 0, 0, 0, JSTemporalDuration.DurationPrototype);
    }

    [JSExport("equals", Length = 1)]
    public JSValue Equals(in Arguments a)
    {
        var other = Require(ToTemporalYearMonth(a.GetAt(0)));
        return isoYear == other.isoYear && isoMonth == other.isoMonth && referenceISODay == other.referenceISODay
            && calendarId == other.calendarId
            ? JSValue.BooleanTrue : JSValue.BooleanFalse;
    }

    [JSExport("toPlainDate", Length = 1)]
    public JSValue ToPlainDate(in Arguments a)
    {
        if (a.GetAt(0) is not JSObject obj)
            throw JSEngine.NewTypeError("Temporal.PlainYearMonth.prototype.toPlainDate requires an object");
        var dayValue = obj[KeyStrings.GetOrCreate("day")];
        if (dayValue.IsUndefined)
            throw JSEngine.NewTypeError("Temporal.PlainYearMonth.prototype.toPlainDate requires a day");

        var day = ToIntegerWithTruncation(dayValue);
        if (NonIso)
        {
            var c = CalendarYmd();
            var (iy, im, id) = TemporalNonIso.RegulateToIso(calendarId, c.y, c.m, day, "constrain");
            if (!IsWithinIsoDateLimits(iy, im, id))
                throw JSEngine.NewRangeError("Temporal.PlainYearMonth.prototype.toPlainDate: result is outside the supported ISO date range");
            return new JSTemporalPlainDate(iy, im, id, calendarId, JSTemporalPlainDate.PlainDatePrototype);
        }
        day = Math.Clamp(day, 1, DaysInMonthOf(isoYear, isoMonth));
        // The combined date can fall outside the representable ISO range even when the
        // year-month itself is in range (the year-month limit is wider than the date limit
        // at the boundary months, e.g. -271821-04 is valid but -271821-04-18 is not).
        if (!IsWithinIsoDateLimits(isoYear, isoMonth, day))
            throw JSEngine.NewRangeError("Temporal.PlainYearMonth.prototype.toPlainDate: result is outside the supported ISO date range");
        return new JSTemporalPlainDate(isoYear, isoMonth, day, calendarId, JSTemporalPlainDate.PlainDatePrototype);
    }

    [JSExport("toString", Length = 0)]
    public JSValue ToStringMethod(in Arguments a) => new JSString(ToISOString(ReadCalendarName(a.GetAt(0))));

    [JSExport("toJSON", Length = 0)]
    public JSValue ToJSON(in Arguments a) => new JSString(ToISOString());

    [JSExport("toLocaleString", Length = 0)]
    public JSValue ToLocaleString(in Arguments a)
        => Intl.JSIntlDateTimeFormat.TemporalToLocaleString(this, a.GetAt(0), a.GetAt(1));

    // GetTemporalShowCalendarNameOption.
    private static string ReadCalendarName(JSValue options)
    {
        if (options == null || options.IsUndefined) return "auto";
        if (options is not JSObject optionsObject)
            throw JSEngine.NewTypeError("Temporal options must be an object or undefined");

        var v = optionsObject[KeyStrings.GetOrCreate("calendarName")];
        if (v.IsUndefined) return "auto";

        var name = v.StringValue;
        return name switch
        {
            "auto" or "always" or "never" or "critical" => name,
            _ => throw JSEngine.NewRangeError($"Temporal: invalid calendarName \"{name}\""),
        };
    }

    [JSExport("valueOf", Length = 0)]
    public JSValue ValueOf(in Arguments a)
        => throw JSEngine.NewTypeError("Called Temporal.PlainYearMonth.prototype.valueOf, which is not supported. Use Temporal.PlainYearMonth.compare for comparison.");

    // ── helpers ─────────────────────────────────────────────────────────────────

    private static JSTemporalPlainYearMonth Require(JSValue value)
        => value as JSTemporalPlainYearMonth ?? throw JSEngine.NewTypeError("expected a Temporal.PlainYearMonth");

    private static int ToIntegerWithTruncation(JSValue value)
    {
        if (value == null || value.IsUndefined) return 0;
        var number = value.DoubleValue;
        if (double.IsNaN(number) || double.IsInfinity(number))
            throw JSEngine.NewRangeError("Temporal.PlainYearMonth: component must be finite");
        return (int)Math.Truncate(number);
    }

    // The constructor's required isoYear/isoMonth arguments are coerced with ToNumber first, so an
    // absent / undefined argument (NaN) is a RangeError rather than silently defaulting to 0.
    private static int ToIntegerWithTruncationArgument(JSValue value)
    {
        var number = value == null ? double.NaN : value.DoubleValue;
        if (double.IsNaN(number) || double.IsInfinity(number))
            throw JSEngine.NewRangeError("Temporal.PlainYearMonth: component must be finite");
        return (int)Math.Truncate(number);
    }

    // ToPositiveIntegerWithTruncation: the `month` field must be a positive integer (≥ 1) regardless
    // of the overflow option — only the upper bound is subject to constrain/reject.
    private static int ToPositiveIntegerWithTruncation(JSValue value)
    {
        var n = ToIntegerWithTruncation(value);
        if (n < 1)
            throw JSEngine.NewRangeError("Temporal.PlainYearMonth: month must be a positive integer");
        return n;
    }

    private static string CanonicalizeCalendar(JSValue calendar)
    {
        if (calendar == null || calendar.IsUndefined) return "iso8601";
        return TemporalCalendar.ToSlotValue(calendar, includeArithmetic: true);
    }

    private static int MonthFromCode(string code)
    {
        var match = Regex.Match(code, @"^M(\d{2})$");
        if (!match.Success)
            throw JSEngine.NewRangeError($"Temporal: invalid monthCode \"{code}\"");

        // These ISO-based (12-month) calendar paths reject a well-formed but out-of-range code such
        // as "M00" / "M13" up front — monthCode is never subject to the overflow option, so it must
        // throw rather than be constrained.
        var month = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        if (month is < 1 or > 12)
            throw JSEngine.NewRangeError($"Temporal: invalid monthCode \"{code}\"");
        return month;
    }

    private static string ReadOverflow(JSValue options)
    {
        if (options == null || options.IsUndefined) return "constrain";
        if (options is not JSObject optionsObject)
            throw JSEngine.NewTypeError("Temporal options must be an object or undefined");
        var v = optionsObject[KeyStrings.GetOrCreate("overflow")];
        if (v.IsUndefined) return "constrain";
        var overflow = v.StringValue;
        if (overflow is not ("constrain" or "reject"))
            throw JSEngine.NewRangeError($"Temporal: invalid overflow \"{overflow}\"");
        return overflow;
    }

    // GetDifferenceSettings for until/since: the only valid units are year (largest) and month
    // (smallest). Validates largestUnit/smallestUnit (smallestUnit must not be larger than largestUnit),
    // roundingIncrement and roundingMode, and returns all four (rounding is applied in Difference for the
    // ISO calendar).
    private static (string largestUnit, string smallestUnit, int increment, string roundingMode) ReadDifferenceSettings(JSValue options)
    {
        if (options == null || options.IsUndefined) return ("year", "month", 1, "trunc");
        if (options is not JSObject o)
            throw JSEngine.NewTypeError("Temporal options must be an object or undefined");

        // GetDifferenceSettings reads largestUnit, roundingIncrement, roundingMode, smallestUnit in that
        // order, coercing each unit (week/day/time units included) before any is validated against the
        // allowed group, so a disallowed-unit RangeError is reported only after every option is read.
        var largestRaw = o[KeyStrings.GetOrCreate("largestUnit")];
        var largestUnit = largestRaw.IsUndefined ? null : TemporalRoundingOptions.NormalizeAnyUnit(largestRaw.StringValue, allowAuto: true);
        if (largestUnit == "auto") largestUnit = null;

        var increment = TemporalRoundingOptions.GetRoundingIncrement(o);
        var roundingMode = TemporalRoundingOptions.GetRoundingMode(o, "trunc");

        var smallestRaw = o[KeyStrings.GetOrCreate("smallestUnit")];
        var smallestUnit = smallestRaw.IsUndefined ? "month" : TemporalRoundingOptions.NormalizeAnyUnit(smallestRaw.StringValue, allowAuto: false);

        // Only "year" and "month" are valid for a PlainYearMonth difference.
        if (largestUnit is not (null or "year" or "month"))
            throw JSEngine.NewRangeError($"Temporal.PlainYearMonth: invalid largestUnit \"{largestUnit}\"");
        if (smallestUnit is not ("year" or "month"))
            throw JSEngine.NewRangeError($"Temporal.PlainYearMonth: invalid smallestUnit \"{smallestUnit}\"");

        // largestUnit "auto"/absent resolves to the larger of "year" and smallestUnit, i.e. "year".
        largestUnit ??= "year";

        // year ranks above month; smallestUnit must not be larger (rank smaller) than largestUnit.
        if (UnitRank(smallestUnit) < UnitRank(largestUnit))
            throw JSEngine.NewRangeError("Temporal.PlainYearMonth: smallestUnit must not be larger than largestUnit");

        return (largestUnit, smallestUnit, increment, roundingMode);
    }

    private static int UnitRank(string unit) => unit == "year" ? 0 : 1;

    private static JSValue ToTemporalYearMonth(JSValue item) => ToTemporalYearMonth(item, JSUndefined.Value);

    // `options` is the raw options argument; the overflow option is read at the spec-mandated point
    // (after the item's type is validated and its fields / string are read) so an invalid primitive
    // item throws a TypeError before the options bag is ever observed.
    private static JSValue ToTemporalYearMonth(JSValue item, JSValue options)
    {
        if (item is JSTemporalPlainYearMonth ym)
        {
            ReadOverflow(options);
            return new JSTemporalPlainYearMonth(ym.isoYear, ym.isoMonth, ym.referenceISODay, ym.calendarId, PlainYearMonthPrototype);
        }

        if (item.IsString)
        {
            var parsed = ParseTemporalYearMonthString(item.ToString());
            ReadOverflow(options);
            return parsed;
        }

        if (item is not JSObject obj)
            throw JSEngine.NewTypeError("Temporal.PlainYearMonth: invalid value");

        var calendarId = CanonicalizeCalendar(obj[KeyStrings.GetOrCreate("calendar")]);

        if (TemporalCalendarMath.IsNonIso(calendarId))
        {
            var nonIsoOverflow = ReadOverflow(options);
            var (iy, im, id) = TemporalNonIso.YearMonthToIso(obj, calendarId, nonIsoOverflow, "Temporal.PlainYearMonth");
            // A PlainYearMonth is bounded by ISOYearMonthWithinLimits (year/month), not the day-precise
            // ISO date range, so this is validated here rather than during the reference-day conversion.
            if (!ISOYearMonthWithinLimits(iy, im))
                throw JSEngine.NewRangeError("Temporal.PlainYearMonth: year-month is out of range");
            return new JSTemporalPlainYearMonth(iy, im, id, calendarId, PlainYearMonthPrototype);
        }

        // PrepareCalendarFields: read each recognised field in alphabetical order —
        // [era, eraYear,] month, monthCode, year — coercing it as it is read. The
        // iso8601 calendar has no eras, so era / eraYear are not fields and are not
        // read for it (test262 PlainYearMonth/from/order-of-operations).
        var hasEras = calendarId != "iso8601";

        JSValue eraValue = JSUndefined.Value;
        JSValue eraYearValue = JSUndefined.Value;
        string eraStr = null;
        var eraYearInt = 0;
        if (hasEras)
        {
            eraValue = obj[KeyStrings.GetOrCreate("era")];
            eraStr = eraValue.IsUndefined ? null : eraValue.StringValue;
            eraYearValue = obj[KeyStrings.GetOrCreate("eraYear")];
            eraYearInt = eraYearValue.IsUndefined ? 0 : ToIntegerWithTruncation(eraYearValue);
        }

        var monthValue = obj[KeyStrings.GetOrCreate("month")];
        var monthFromMonth = monthValue.IsUndefined ? -1 : ToPositiveIntegerWithTruncation(monthValue);

        // Coerce monthCode now; defer parsing/validating it against the calendar until after
        // the overflow option is read (test262 from/options-read-before-algorithmic-validation).
        var monthCodeValue = obj[KeyStrings.GetOrCreate("monthCode")];
        var monthCodeStr = TemporalIsoString.RequireMonthCodeString(monthCodeValue, "Temporal.PlainYearMonth");
        // Validate monthCode *syntax* (well-formedness) as soon as it is read — before the `year`
        // field below is coerced — so an ill-formed code is a RangeError regardless of the year's
        // type (test262 .../from/monthcode-invalid "syntax is validated before year type").
        TemporalIsoString.RequireWellFormedMonthCode(monthCodeStr, "Temporal.PlainYearMonth");

        var yearValue = obj[KeyStrings.GetOrCreate("year")];
        var yearInt = yearValue.IsUndefined ? 0 : ToIntegerWithTruncation(yearValue);

        var hasYear = !yearValue.IsUndefined;
        var hasEra = !eraValue.IsUndefined;
        var hasEraYear = !eraYearValue.IsUndefined;
        if (!hasYear && !(hasEra && hasEraYear))
            throw JSEngine.NewTypeError("Temporal.PlainYearMonth: missing year (or era and eraYear)");
        if (monthValue.IsUndefined && monthCodeValue.IsUndefined)
            throw JSEngine.NewTypeError("Temporal.PlainYearMonth: missing month / monthCode");

        var overflow = ReadOverflow(options);

        var monthFromCode = monthCodeStr == null ? -1 : MonthFromCode(monthCodeStr);
        var month = monthCodeValue.IsUndefined ? monthFromMonth : monthFromCode;
        if (monthFromMonth != -1 && !monthCodeValue.IsUndefined && monthFromMonth != month)
            throw JSEngine.NewRangeError("Temporal.PlainYearMonth: month and monthCode disagree");

        var isoYear = TemporalCalendar.ResolveIsoYear(calendarId,
            hasYear, yearInt, hasEra, eraStr, hasEraYear, eraYearInt);

        return RegulateYearMonth(isoYear, month, overflow, calendarId);
    }

    // The date hyphens are all-or-nothing: the extended form "1976-11(-18)?" and the basic form
    // "197611(18)?" are both valid, but a mixed "1976-1118" is not. Two alternatives (with / without
    // separators) capture the year/month/day under shared names so a mixed form matches neither.
    private const string Year = @"\d{4}|\+\d{6}|-(?!000000)\d{6}";
    private static readonly Regex YearMonthPattern = new(
        @"^(?:(?<y>" + Year + @")-(?<mo>\d{2})(?:-(?<d>\d{2}))?|(?<y>" + Year + @")(?<mo>\d{2})(?<d>\d{2})?)" +
        TemporalIsoString.TimeAndOffsetTail + TemporalIsoString.AnnotationsTail + "$",
        RegexOptions.CultureInvariant);

    private static JSValue ParseTemporalYearMonthString(string text)
    {
        // Only the ASCII hyphen-minus is a valid sign; reject the U+2212 variant the time/offset
        // tail would otherwise accept.
        if (text.Contains('−'))
            throw JSEngine.NewRangeError($"Cannot parse Temporal.PlainYearMonth from \"{text}\"");

        TemporalIsoString.RejectMultipleCalendarAnnotations(text);
        TemporalIsoString.RejectMalformedAnnotations(text);
        TemporalIsoString.RejectInvalidAnnotations(text);

        var match = YearMonthPattern.Match(text);
        if (!match.Success)
            throw JSEngine.NewRangeError($"Cannot parse Temporal.PlainYearMonth from \"{text}\"");

        TemporalIsoString.RejectTimeTailForCalendarOnly(match, text);

        var year = int.Parse(match.Groups["y"].Value.Replace('−', '-'), CultureInfo.InvariantCulture);
        var month = int.Parse(match.Groups["mo"].Value, CultureInfo.InvariantCulture);

        if (month is < 1 or > 12 || !ISOYearMonthWithinLimits(year, month))
            throw JSEngine.NewRangeError($"Cannot parse Temporal.PlainYearMonth from \"{text}\"");

        var calMatch = CalendarAnnotation.Match(text);
        var calendarId = calMatch.Success ? TemporalCalendar.Canonicalize(calMatch.Groups[1].Value, includeArithmetic: true) : "iso8601";

        // The bare year-month format (no day component) can only carry the iso8601 calendar; any
        // other calendar needs a reference day, i.e. the full date format (YYYY-MM-DD[u-ca=…]).
        if (!match.Groups["d"].Success && calendarId != "iso8601")
            throw JSEngine.NewRangeError($"Temporal.PlainYearMonth: a non-iso8601 calendar requires a day in \"{text}\"");

        // For a non-Gregorian calendar the parsed ISO date (with its reference day) is projected to
        // the calendar year-month and re-anchored on day 1 of that month.
        if (TemporalCalendarMath.IsNonIso(calendarId))
        {
            var day = match.Groups["d"].Success ? int.Parse(match.Groups["d"].Value, CultureInfo.InvariantCulture) : 1;
            if (!IsValidISODate(year, month, day))
                throw JSEngine.NewRangeError($"Cannot parse Temporal.PlainYearMonth from \"{text}\"");
            return YearMonthFromIsoDate(year, month, day, calendarId);
        }

        return new JSTemporalPlainYearMonth(year, month, 1, calendarId, PlainYearMonthPrototype);
    }

    private static readonly Regex CalendarAnnotation = new(@"\[!?u-ca=([^\]]+)\]", RegexOptions.CultureInvariant);

    private static JSValue RegulateYearMonth(int year, int month, string overflow, string calendarId = "iso8601")
    {
        if (overflow == "reject")
        {
            if (month is < 1 or > 12)
                throw JSEngine.NewRangeError("Temporal.PlainYearMonth: month is out of range");
        }
        else month = Math.Clamp(month, 1, 12);

        if (!ISOYearMonthWithinLimits(year, month))
            throw JSEngine.NewRangeError("Temporal.PlainYearMonth: year-month is out of range");

        return new JSTemporalPlainYearMonth(year, month, 1, calendarId, PlainYearMonthPrototype);
    }

    // ── ISO arithmetic ───────────────────────────────────────────────────────────

    private static bool IsLeapYear(long y) => (y % 4 == 0 && y % 100 != 0) || y % 400 == 0;

    private static int DaysInMonthOf(long year, int month) => month switch
    {
        1 or 3 or 5 or 7 or 8 or 10 or 12 => 31,
        4 or 6 or 9 or 11 => 30,
        2 => IsLeapYear(year) ? 29 : 28,
        _ => 0,
    };

    private static bool IsValidISODate(int year, int month, int day)
        => month is >= 1 and <= 12 && day >= 1 && day <= DaysInMonthOf(year, month);

    // ISODateWithinLimits: the full date (not just the year-month) must lie within the representable
    // instant range, −271821-04-19 … +275760-09-13.
    private static readonly long MinIsoEpochDays = DaysFromCivil(-271821, 4, 19);
    private static readonly long MaxIsoEpochDays = DaysFromCivil(275760, 9, 13);
    private static bool IsWithinIsoDateLimits(int year, int month, int day)
    {
        if (!IsValidISODate(year, month, day)) return false;
        var epoch = DaysFromCivil(year, month, day);
        return epoch >= MinIsoEpochDays && epoch <= MaxIsoEpochDays;
    }

    // ISOYearMonthWithinLimits: the reference-day-1 of the month must lie within the
    // representable instant range, i.e. -271821-04 … +275760-09.
    private static bool ISOYearMonthWithinLimits(int year, int month)
    {
        if (year is < -271821 or > 275760) return false;
        if (year == -271821 && month < 4) return false;
        if (year == 275760 && month > 9) return false;
        return true;
    }

    private static int CompareISODate(int y1, int m1, int d1, int y2, int m2, int d2)
    {
        if (y1 != y2) return y1 < y2 ? -1 : 1;
        if (m1 != m2) return m1 < m2 ? -1 : 1;
        if (d1 != d2) return d1 < d2 ? -1 : 1;
        return 0;
    }

    private static long FloorDiv(long a, long b)
    {
        var q = a / b;
        if (a % b != 0 && (a < 0) != (b < 0)) q -= 1;
        return q;
    }

    private static long DaysFromCivil(long y, long m, long d)
    {
        y -= m <= 2 ? 1 : 0;
        var era = (y >= 0 ? y : y - 399) / 400;
        var yoe = y - era * 400;
        var doy = (153 * (m > 2 ? m - 3 : m + 9) + 2) / 5 + d - 1;
        var doe = yoe * 365 + yoe / 4 - yoe / 100 + doy;
        return era * 146097 + doe - 719468;
    }

    private static (long y, long m, long d) CivilFromDays(long z)
    {
        z += 719468;
        var era = (z >= 0 ? z : z - 146096) / 146097;
        var doe = z - era * 146097;
        var yoe = (doe - doe / 1460 + doe / 36524 - doe / 146096) / 365;
        var y = yoe + era * 400;
        var doy = doe - (365 * yoe + yoe / 4 - yoe / 100);
        var mp = (5 * doy + 2) / 153;
        var d = doy - (153 * mp + 2) / 5 + 1;
        var m = mp < 10 ? mp + 3 : mp - 9;
        return (m <= 2 ? y + 1 : y, m, d);
    }

    // TemporalYearMonthToString: "YYYY-MM" (expanding to ±YYYYYY outside 0000–9999). For a
    // non-ISO calendar the reference ISO day is ALWAYS appended (the day is required to
    // round-trip a YYYY-MM date through the calendar's month-day projection) regardless of
    // the calendarName option; only the optional [u-ca=…] annotation responds to
    // showCalendar (always / critical / auto / never).
    private string ToISOString(string showCalendar = "auto")
    {
        var sb = new StringBuilder();
        if (isoYear < 0 || isoYear > 9999)
            sb.Append(isoYear < 0 ? '-' : '+').Append(Math.Abs(isoYear).ToString("000000", CultureInfo.InvariantCulture));
        else
            sb.Append(isoYear.ToString("0000", CultureInfo.InvariantCulture));

        sb.Append('-').Append(isoMonth.ToString("00", CultureInfo.InvariantCulture));

        var isNonIso = calendarId != "iso8601";
        var showAnnotation = showCalendar is "always" or "critical" || (showCalendar == "auto" && isNonIso);
        // The reference day is appended whenever the calendar is shown OR when the calendar
        // is non-ISO (so the day round-trips even with calendarName: "never").
        if (showAnnotation || isNonIso)
            sb.Append('-').Append(referenceISODay.ToString("00", CultureInfo.InvariantCulture));
        if (showAnnotation)
            sb.Append(JSTemporalPlainDate.FormatCalendarAnnotation(calendarId,
                showCalendar == "critical" ? "critical" : "always"));

        return sb.ToString();
    }
}
