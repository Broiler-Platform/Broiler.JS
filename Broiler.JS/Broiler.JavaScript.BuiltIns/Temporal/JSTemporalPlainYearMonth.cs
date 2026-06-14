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
        isoYear = ToIntegerWithTruncation(a.GetAt(0));
        isoMonth = ToIntegerWithTruncation(a.GetAt(1));
        calendarId = CanonicalizeCalendar(a.GetAt(2));
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
    {
        var item = a.GetAt(0);
        var overflow = ReadOverflow(a.GetAt(1));

        if (item is JSTemporalPlainYearMonth ym)
            return new JSTemporalPlainYearMonth(ym.isoYear, ym.isoMonth, ym.referenceISODay, ym.calendarId, PlainYearMonthPrototype);

        return ToTemporalYearMonth(item, overflow);
    }

    [JSExport("compare", Length = 2)]
    internal static JSValue Compare(in Arguments a)
    {
        var one = Require(ToTemporalYearMonth(a.GetAt(0), "constrain"));
        var two = Require(ToTemporalYearMonth(a.GetAt(1), "constrain"));
        return new JSNumber(CompareISODate(one.isoYear, one.isoMonth, one.referenceISODay,
            two.isoYear, two.isoMonth, two.referenceISODay));
    }

    // ── methods ─────────────────────────────────────────────────────────────────

    [JSExport("with", Length = 1)]
    public JSValue With(in Arguments a)
    {
        if (a.GetAt(0) is not JSObject obj)
            throw JSEngine.NewTypeError("Temporal.PlainYearMonth.prototype.with requires an object");
        if (!obj[KeyStrings.GetOrCreate("calendar")].IsUndefined)
            throw JSEngine.NewTypeError("Temporal.PlainYearMonth.prototype.with does not accept a calendar field");
        if (!obj[KeyStrings.GetOrCreate("timeZone")].IsUndefined)
            throw JSEngine.NewTypeError("Temporal.PlainYearMonth.prototype.with does not accept a timeZone field");

        var overflow = ReadOverflow(a.GetAt(1));

        if (NonIso)
            return WithNonIso(obj, overflow);

        var any = false;
        var month = isoMonth;

        var year = ResolveWithYear(obj, ref any);

        var monthCodeValue = obj[KeyStrings.GetOrCreate("monthCode")];
        var monthValue = obj[KeyStrings.GetOrCreate("month")];
        if (!monthCodeValue.IsUndefined) { month = MonthFromCode(monthCodeValue.ToString()); any = true; }
        if (!monthValue.IsUndefined)
        {
            var m = ToPositiveIntegerWithTruncation(monthValue);
            if (!monthCodeValue.IsUndefined && m != month)
                throw JSEngine.NewRangeError("Temporal.PlainYearMonth.with: month and monthCode disagree");
            month = m; any = true;
        }

        if (!any)
            throw JSEngine.NewTypeError("Temporal.PlainYearMonth.prototype.with requires at least one field");

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
        var monthValue = obj[KeyStrings.GetOrCreate("month")];
        int month = c.m;
        if (!monthCodeValue.IsUndefined)
        {
            var (codeNumber, leapMonth) = ParseMonthCodeLeap(monthCodeValue.ToString());
            month = TemporalCalendarMath.OrdinalFromMonthCode(calendarId, year, codeNumber, leapMonth);
            any = true;
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

        var (iy, im, id) = TemporalNonIso.RegulateToIso(calendarId, year, month, 1, overflow);
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
        var (iy, im, id) = TemporalNonIso.RegulateToIso(calendarId, c.y, c.m, 1, "constrain");
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
        var target = Require(ToTemporalYearMonth(other, "constrain"));
        if (calendarId != target.calendarId)
            throw JSEngine.NewRangeError("Temporal.PlainYearMonth: cannot compute the difference between year-months of different calendars");
        var (largestUnit, smallestUnit, increment, roundingMode) = ReadDifferenceSettings(options);

        if (NonIso)
        {
            var self = CalendarYmd();
            var oth = target.CalendarYmd();
            var (cay, cam, cby, cbm) = sign == 1
                ? (self.y, self.m, oth.y, oth.m)
                : (oth.y, oth.m, self.y, self.m);
            var (cy, cm, _, _) = TemporalNonIso.Difference(calendarId, cay, cam, 1, cby, cbm, 1, largestUnit);
            return new JSTemporalDuration(cy, cm, 0, 0, 0, 0, 0, 0, 0, 0, JSTemporalDuration.DurationPrototype);
        }

        // The difference is measured from the receiver (day 1) and negated for "since"; the rounding mode
        // is correspondingly negated before rounding (GetDifferenceSettings).
        var mode = sign == -1 ? TemporalRoundingOptions.NegateRoundingMode(roundingMode) : roundingMode;
        var (years, months, _, _) = JSTemporalPlainDate.DifferenceISODateRounded(
            isoYear, isoMonth, 1, target.isoYear, target.isoMonth, 1, largestUnit, smallestUnit, increment, mode);

        if (sign == -1) { years = -years; months = -months; }

        return new JSTemporalDuration(years, months, 0, 0, 0, 0, 0, 0, 0, 0, JSTemporalDuration.DurationPrototype);
    }

    [JSExport("equals", Length = 1)]
    public JSValue Equals(in Arguments a)
    {
        var other = Require(ToTemporalYearMonth(a.GetAt(0), "constrain"));
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
            return new JSTemporalPlainDate(iy, im, id, calendarId, JSTemporalPlainDate.PlainDatePrototype);
        }
        day = Math.Clamp(day, 1, DaysInMonthOf(isoYear, isoMonth));
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
        return int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
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

    private static JSValue ToTemporalYearMonth(JSValue item, string overflow)
    {
        if (item is JSTemporalPlainYearMonth ym)
            return new JSTemporalPlainYearMonth(ym.isoYear, ym.isoMonth, ym.referenceISODay, ym.calendarId, PlainYearMonthPrototype);

        if (item.IsString)
            return ParseTemporalYearMonthString(item.ToString());

        if (item is not JSObject obj)
            throw JSEngine.NewTypeError("Temporal.PlainYearMonth: invalid value");

        var calendarId = CanonicalizeCalendar(obj[KeyStrings.GetOrCreate("calendar")]);

        if (TemporalCalendarMath.IsNonIso(calendarId))
        {
            var (iy, im, id) = TemporalNonIso.YearMonthToIso(obj, calendarId, overflow, "Temporal.PlainYearMonth");
            return new JSTemporalPlainYearMonth(iy, im, id, calendarId, PlainYearMonthPrototype);
        }

        var yearValue = obj[KeyStrings.GetOrCreate("year")];
        var eraValue = obj[KeyStrings.GetOrCreate("era")];
        var eraYearValue = obj[KeyStrings.GetOrCreate("eraYear")];
        var monthValue = obj[KeyStrings.GetOrCreate("month")];
        var monthCodeValue = obj[KeyStrings.GetOrCreate("monthCode")];

        var hasYear = !yearValue.IsUndefined;
        var hasEra = !eraValue.IsUndefined;
        var hasEraYear = !eraYearValue.IsUndefined;
        if (!hasYear && !(hasEra && hasEraYear))
            throw JSEngine.NewTypeError("Temporal.PlainYearMonth: missing year (or era and eraYear)");
        if (monthValue.IsUndefined && monthCodeValue.IsUndefined)
            throw JSEngine.NewTypeError("Temporal.PlainYearMonth: missing month / monthCode");

        var month = monthCodeValue.IsUndefined ? ToPositiveIntegerWithTruncation(monthValue) : MonthFromCode(monthCodeValue.ToString());
        if (!monthValue.IsUndefined && !monthCodeValue.IsUndefined && ToIntegerWithTruncation(monthValue) != month)
            throw JSEngine.NewRangeError("Temporal.PlainYearMonth: month and monthCode disagree");

        var isoYear = TemporalCalendar.ResolveIsoYear(calendarId,
            hasYear, hasYear ? ToIntegerWithTruncation(yearValue) : 0,
            hasEra, hasEra ? eraValue.StringValue : null,
            hasEraYear, hasEraYear ? ToIntegerWithTruncation(eraYearValue) : 0);

        return RegulateYearMonth(isoYear, month, overflow, calendarId);
    }

    private static readonly Regex YearMonthPattern = new(
        @"^(\d{4}|\+\d{6}|-(?!000000)\d{6})-(\d{2})(?:-(\d{2}))?" +
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

        var year = int.Parse(match.Groups[1].Value.Replace('−', '-'), CultureInfo.InvariantCulture);
        var month = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);

        if (month is < 1 or > 12 || !ISOYearMonthWithinLimits(year, month))
            throw JSEngine.NewRangeError($"Cannot parse Temporal.PlainYearMonth from \"{text}\"");

        var calMatch = CalendarAnnotation.Match(text);
        var calendarId = calMatch.Success ? TemporalCalendar.Canonicalize(calMatch.Groups[1].Value, includeArithmetic: true) : "iso8601";

        // The bare year-month format (no day component) can only carry the iso8601 calendar; any
        // other calendar needs a reference day, i.e. the full date format (YYYY-MM-DD[u-ca=…]).
        if (!match.Groups[3].Success && calendarId != "iso8601")
            throw JSEngine.NewRangeError($"Temporal.PlainYearMonth: a non-iso8601 calendar requires a day in \"{text}\"");

        // For a non-Gregorian calendar the parsed ISO date (with its reference day) is projected to
        // the calendar year-month and re-anchored on day 1 of that month.
        if (TemporalCalendarMath.IsNonIso(calendarId))
        {
            var day = match.Groups[3].Success ? int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture) : 1;
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

    // TemporalYearMonthToString: "YYYY-MM" (expanding to ±YYYYYY outside 0000–9999). When the
    // calendar is shown ("always"/"critical") the reference ISO day and a [u-ca=…] annotation are
    // appended — the round-trippable form needed because YYYY-MM alone omits the day.
    private string ToISOString(string showCalendar = "auto")
    {
        var sb = new StringBuilder();
        if (isoYear < 0 || isoYear > 9999)
            sb.Append(isoYear < 0 ? '-' : '+').Append(Math.Abs(isoYear).ToString("000000", CultureInfo.InvariantCulture));
        else
            sb.Append(isoYear.ToString("0000", CultureInfo.InvariantCulture));

        sb.Append('-').Append(isoMonth.ToString("00", CultureInfo.InvariantCulture));

        // The calendar is displayed for always / critical, or whenever it is non-ISO (auto). When
        // shown, the reference ISO day is appended too (YYYY-MM alone cannot round-trip the day).
        var showCal = showCalendar is "always" or "critical" || (showCalendar != "never" && calendarId != "iso8601");
        if (showCal)
        {
            sb.Append('-').Append(referenceISODay.ToString("00", CultureInfo.InvariantCulture));
            sb.Append(JSTemporalPlainDate.FormatCalendarAnnotation(calendarId,
                showCalendar == "critical" ? "critical" : "always"));
        }

        return sb.ToString();
    }
}
