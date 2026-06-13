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
            // Complete a partial { era, eraYear } pair from the receiver's current era.
            var (curEra, curEraYear) = TemporalCalendarMath.Era(calendarId, c.y);
            var era = hasEra ? eraValue.StringValue : curEra;
            var eraYear = hasEraYear ? ToIntegerWithTruncation(eraYearValue) : curEraYear;
            year = TemporalCalendarMath.YearFromEra(calendarId, era, eraYear);
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
            if (!hasEra) { eraValue = Era; hasEra = !eraValue.IsUndefined; }
            if (!hasEraYear) { eraYearValue = EraYear; hasEraYear = !eraYearValue.IsUndefined; }
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
        var overflow = ReadOverflow(options);
        var d = (JSTemporalDuration)JSTemporalDuration.ToTemporalDuration(durationLike);

        // Balance the time + week + day components down to whole days.
        var days = (long)d.DaysValue + (long)d.WeeksValue * 7;
        var timeNs = (long)d.HoursValue * 3_600_000_000_000 + (long)d.MinutesValue * 60_000_000_000
            + (long)d.SecondsValue * 1_000_000_000 + (long)d.MillisecondsValue * 1_000_000
            + (long)d.MicrosecondsValue * 1_000 + (long)d.NanosecondsValue;
        days += timeNs / 86_400_000_000_000;

        var years = sign * (long)d.YearsValue;
        var months = sign * (long)d.MonthsValue;
        var extraDays = sign * days;

        // Operate on the first day of the month (for additions) or the last (for subtractions),
        // mirroring the spec so that whole-day movement lands in the intended month.
        if (NonIso)
        {
            var c = CalendarYmd();
            var startDayCal = sign < 0 ? TemporalCalendarMath.DaysInMonth(calendarId, c.y, c.m) : 1;
            var (ay, amo, ad) = TemporalNonIso.AddToIso(calendarId, c.y, c.m, startDayCal,
                years, months, 0, extraDays, overflow);
            return YearMonthFromIsoDate(ay, amo, ad, calendarId);
        }

        var startDay = sign < 0 ? DaysInMonthOf(isoYear, isoMonth) : 1;
        var total = (long)isoMonth - 1 + months;
        var ny = (int)(isoYear + years + FloorDiv(total, 12));
        var nm = (int)(((total % 12) + 12) % 12) + 1;
        var clampedDay = Math.Min(startDay, DaysInMonthOf(ny, nm));
        var epoch = DaysFromCivil(ny, nm, clampedDay) + extraDays;
        var (ry, rm, _) = CivilFromDays(epoch);

        return RegulateYearMonth((int)ry, (int)rm, overflow, calendarId);
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

    // TODO: until/since honor only `largestUnit` (year[default]/month); rounding is not applied.
    private JSValue Difference(JSValue other, JSValue options, int sign)
    {
        var target = Require(ToTemporalYearMonth(other, "constrain"));
        if (calendarId != target.calendarId)
            throw JSEngine.NewRangeError("Temporal.PlainYearMonth: cannot compute the difference between year-months of different calendars");
        var largestUnit = ReadDifferenceSettings(options);

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

        var (ay, am, by, bm) = sign == 1
            ? (isoYear, isoMonth, target.isoYear, target.isoMonth)
            : (target.isoYear, target.isoMonth, isoYear, isoMonth);

        long totalMonths = ((long)by * 12 + (bm - 1)) - ((long)ay * 12 + (am - 1));
        double years = 0, months;
        if (largestUnit == "year") { years = totalMonths / 12; months = totalMonths % 12; }
        else months = totalMonths;

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
    public JSValue ToLocaleString(in Arguments a) => new JSString(ToISOString());

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
    // (smallest). Validates largestUnit/smallestUnit (smallestUnit must not be larger than
    // largestUnit), roundingIncrement and roundingMode, and returns the resolved largestUnit. A
    // smallestUnit/increment finer than the default is validated here but the rounding is not yet
    // applied — see the TODO above Difference.
    private static string ReadDifferenceSettings(JSValue options)
    {
        if (options == null || options.IsUndefined) return "year";
        if (options is not JSObject o)
            throw JSEngine.NewTypeError("Temporal options must be an object or undefined");

        var largestUnit = ReadYearMonthUnit(o, "largestUnit");
        var smallestUnit = ReadYearMonthUnit(o, "smallestUnit") ?? "month";
        TemporalRoundingOptions.GetRoundingIncrement(o);
        TemporalRoundingOptions.GetRoundingMode(o, "trunc");

        // largestUnit "auto"/absent resolves to the larger of "year" and smallestUnit, i.e. "year".
        largestUnit ??= "year";

        // year ranks above month; smallestUnit must not be larger (rank smaller) than largestUnit.
        if (UnitRank(smallestUnit) < UnitRank(largestUnit))
            throw JSEngine.NewRangeError("Temporal.PlainYearMonth: smallestUnit must not be larger than largestUnit");

        return largestUnit;
    }

    private static int UnitRank(string unit) => unit == "year" ? 0 : 1;

    // Reads a year/month unit option (largestUnit / smallestUnit), normalizing the plural form;
    // returns null for an absent option (or "auto" for largestUnit) and throws a RangeError for any
    // value that is not "year" or "month".
    private static string ReadYearMonthUnit(JSObject options, string name)
    {
        var v = options[KeyStrings.GetOrCreate(name)];
        if (v.IsUndefined) return null;
        var s = v.StringValue;
        if (s == "auto" && name == "largestUnit") return null;
        return s switch
        {
            "year" or "years" => "year",
            "month" or "months" => "month",
            _ => throw JSEngine.NewRangeError($"Temporal.PlainYearMonth: invalid {name} \"{s}\""),
        };
    }

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

        var match = YearMonthPattern.Match(text);
        if (!match.Success)
            throw JSEngine.NewRangeError($"Cannot parse Temporal.PlainYearMonth from \"{text}\"");

        var year = int.Parse(match.Groups[1].Value.Replace('−', '-'), CultureInfo.InvariantCulture);
        var month = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);

        if (month is < 1 or > 12 || !ISOYearMonthWithinLimits(year, month))
            throw JSEngine.NewRangeError($"Cannot parse Temporal.PlainYearMonth from \"{text}\"");

        var calMatch = CalendarAnnotation.Match(text);
        var calendarId = calMatch.Success ? TemporalCalendar.Canonicalize(calMatch.Groups[1].Value, includeArithmetic: true) : "iso8601";

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
