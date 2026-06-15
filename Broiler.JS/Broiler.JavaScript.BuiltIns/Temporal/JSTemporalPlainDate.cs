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

// Temporal.PlainDate (Temporal proposal §3): a calendar date with no time or zone. Only the
// ISO 8601 calendar is supported — its arithmetic (leap years, month lengths, date add /
// difference) is pure, data-free math, so the full date surface is implementable. The
// CLDR submodule has no calendar arithmetic (only day-period formatting strings), and the
// non-ISO calendars / localized output it could feed are out of scope.
//
// Registered under the Temporal namespace (not as a global) via Register = false.
[JSClassGenerator("PlainDate", Register = false)]
public partial class JSTemporalPlainDate : JSObject
{
    // ISODateTimeWithinLimits bounds: -271821-04-19 … +275760-09-13 (±~10^8 days).
    private static readonly long MinEpochDays = DaysFromCivil(-271821, 4, 19);
    private static readonly long MaxEpochDays = DaysFromCivil(275760, 9, 13);

    internal readonly int isoYear, isoMonth, isoDay;
    internal readonly string calendarId;

    [JSExport(Length = 3)]
    public JSTemporalPlainDate(in Arguments a) : base(ResolvePrototype())
    {
        isoYear = ToIntegerWithTruncation(a.GetAt(0));
        isoMonth = ToIntegerWithTruncation(a.GetAt(1));
        isoDay = ToIntegerWithTruncation(a.GetAt(2));
        calendarId = CanonicalizeCalendar(a.GetAt(3));

        if (!IsValidISODate(isoYear, isoMonth, isoDay))
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

    private static JSObject ResolvePrototype()
    {
        if (JSEngine.NewTarget == null && (JSEngine.Current as IJSExecutionContext)?.CurrentNewTarget == null)
            throw JSEngine.NewTypeError("Constructor Temporal.PlainDate requires 'new'");

        return JSEngine.NewTargetPrototype ?? PlainDatePrototype;
    }

    internal static JSObject PlainDatePrototype
    {
        get
        {
            var temporal = (JSEngine.Current as JSObject)?[KeyStrings.GetOrCreate("Temporal")] as JSObject;
            return (temporal?[KeyStrings.GetOrCreate("PlainDate")] as JSFunction)?.prototype;
        }
    }

    // ── accessors ───────────────────────────────────────────────────────────────

    [JSExport("calendarId")] public JSValue CalendarId => new JSString(calendarId);

    // True for the non-Gregorian calendars whose month/day structure differs from ISO (the
    // arithmetic family coptic / ethiopic / islamic-* / hebrew and the lunisolar chinese / dangi),
    // so every field is derived from the epoch day via TemporalCalendarMath.
    private bool NonIso => TemporalCalendarMath.IsNonIso(calendarId);

    // The stored ISO date projected into the active calendar's (year, month, day) — identical to
    // the ISO fields for the Gregorian family.
    private (int y, int m, int d) CalendarYmd() => NonIso
        ? TemporalCalendarMath.YmdFromEpochDays(calendarId, DaysFromCivil(isoYear, isoMonth, isoDay))
        : (isoYear, isoMonth, isoDay);

    // The ISO 8601 calendar has no eras (era / eraYear are undefined); the gregory / buddhist / roc /
    // japanese calendars and the arithmetic non-Gregorian calendars map the year onto an era + era-
    // year, while the lunisolar chinese / dangi calendars are also era-less.
    [JSExport("era")] public JSValue Era
    {
        get
        {
            if (!NonIso) return TemporalCalendar.Era(calendarId, isoYear, isoMonth, isoDay);
            if (!TemporalCalendarMath.HasEra(calendarId)) return JSUndefined.Value;
            return new JSString(TemporalCalendarMath.Era(calendarId, CalendarYmd().y).code);
        }
    }
    [JSExport("eraYear")] public JSValue EraYear
    {
        get
        {
            if (!NonIso) return TemporalCalendar.EraYear(calendarId, isoYear, isoMonth, isoDay);
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
    [JSExport("day")] public double DayValue => NonIso ? CalendarYmd().d : isoDay;

    [JSExport("dayOfWeek")] public double DayOfWeek => IsoDayOfWeek(isoYear, isoMonth, isoDay);
    [JSExport("dayOfYear")] public double DayOfYear
    {
        get
        {
            if (NonIso) { var c = CalendarYmd(); return TemporalCalendarMath.DayOfYear(calendarId, c.y, c.m, c.d); }
            return (int)(DaysFromCivil(isoYear, isoMonth, isoDay) - DaysFromCivil(isoYear, 1, 1)) + 1;
        }
    }
    [JSExport("daysInWeek")] public double DaysInWeek => 7;
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

    [JSExport("weekOfYear")] public double WeekOfYear => IsoWeek(isoYear, isoMonth, isoDay).week;
    [JSExport("yearOfWeek")] public double YearOfWeek => IsoWeek(isoYear, isoMonth, isoDay).year;

    // ── statics ─────────────────────────────────────────────────────────────────

    [JSExport("from", Length = 1)]
    internal static JSValue From(in Arguments a)
        => ToTemporalDate(a.GetAt(0), a.GetAt(1));

    [JSExport("compare", Length = 2)]
    internal static JSValue Compare(in Arguments a)
    {
        var one = RequireDate(ToTemporalDate(a.GetAt(0)));
        var two = RequireDate(ToTemporalDate(a.GetAt(1)));
        return new JSNumber(CompareISODate(one.isoYear, one.isoMonth, one.isoDay, two.isoYear, two.isoMonth, two.isoDay));
    }

    // ── methods ─────────────────────────────────────────────────────────────────

    [JSExport("with", Length = 1)]
    public JSValue With(in Arguments a)
    {
        if (a.GetAt(0) is not JSObject obj)
            throw JSEngine.NewTypeError("Temporal.PlainDate.prototype.with requires an object");

        // A calendar field is not allowed in a with() bag.
        if (!obj[KeyStrings.GetOrCreate("calendar")].IsUndefined)
            throw JSEngine.NewTypeError("Temporal.PlainDate.prototype.with does not accept a calendar field");

        var overflow = ReadOverflow(a.GetAt(1));

        // The non-Gregorian calendars merge the with-bag fields onto the receiver's calendar fields
        // and re-resolve in calendar space (see TemporalNonIso.WithToIso).
        if (NonIso)
        {
            var (ny, nm, nd) = TemporalNonIso.WithToIso(obj, calendarId, overflow, "Temporal.PlainDate", isoYear, isoMonth, isoDay);
            return new JSTemporalPlainDate(ny, nm, nd, calendarId, PlainDatePrototype);
        }

        var any = false;
        var month = isoMonth;
        var day = isoDay;

        var newIsoYear = ResolveWithYear(obj, ref any);

        var monthCodeValue = obj[KeyStrings.GetOrCreate("monthCode")];
        var monthValue = obj[KeyStrings.GetOrCreate("month")];
        if (!monthCodeValue.IsUndefined) { month = MonthFromCode(monthCodeValue.ToString()); any = true; }
        if (!monthValue.IsUndefined)
        {
            var m = ToIntegerWithTruncation(monthValue);
            if (!monthCodeValue.IsUndefined && m != month)
                throw JSEngine.NewRangeError("Temporal.PlainDate.with: month and monthCode disagree");
            month = m;
            any = true;
        }

        var dayValue = obj[KeyStrings.GetOrCreate("day")];
        if (!dayValue.IsUndefined) { day = ToIntegerWithTruncation(dayValue); any = true; }

        if (!any)
            throw JSEngine.NewTypeError("Temporal.PlainDate.prototype.with requires at least one date property");

        return RegulateISODate(newIsoYear, month, day, overflow, calendarId);
    }

    // Resolves the receiver's new ISO year from a with()-bag's year / era / eraYear fields,
    // defaulting to the receiver's current year when none are present. Providing a calendar
    // `year` overrides any era pair (and vice versa); era and eraYear must be supplied *together* —
    // a partial pair is a TypeError (it is not completed from the receiver).
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
            hasEra = hasEraYear = false; // a provided calendar year supersedes the era pair
        }
        else if (hasEra || hasEraYear)
        {
            era = hasEra ? eraValue.StringValue : null;
            eraYear = hasEraYear ? ToIntegerWithTruncation(eraYearValue) : 0;
        }

        return TemporalCalendar.ResolveIsoYear(calendarId,
            hasYear, hasYear ? ToIntegerWithTruncation(yearValue) : 0,
            hasEra, era, hasEraYear, eraYear);
    }

    [JSExport("withCalendar", Length = 1)]
    public JSValue WithCalendar(in Arguments a)
    {
        var arg = a.GetAt(0);
        if (arg == null || arg.IsUndefined)
            throw JSEngine.NewTypeError("Temporal.PlainDate.prototype.withCalendar requires a calendar");
        return new JSTemporalPlainDate(isoYear, isoMonth, isoDay, TemporalCalendar.ToSlotValue(arg, includeArithmetic: true), PlainDatePrototype);
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

        // A PlainDate balances the duration's time components down to whole (24 h) days, then
        // performs calendar date arithmetic. The running nanosecond total uses BigInteger because a
        // near-maximum duration (e.g. ~1.7e13 seconds) overflows Int64 once scaled to nanoseconds.
        var timeNanoseconds =
            (System.Numerics.BigInteger)(long)d.HoursValue * 3_600_000_000_000
            + (System.Numerics.BigInteger)(long)d.MinutesValue * 60_000_000_000
            + (System.Numerics.BigInteger)(long)d.SecondsValue * 1_000_000_000
            + (System.Numerics.BigInteger)(long)d.MillisecondsValue * 1_000_000
            + (System.Numerics.BigInteger)(long)d.MicrosecondsValue * 1_000
            + (long)d.NanosecondsValue;
        var extraDays = (long)(timeNanoseconds / 86_400_000_000_000);

        if (NonIso)
        {
            var c = CalendarYmd();
            var (ny, nm, nd) = TemporalNonIso.AddToIso(calendarId, c.y, c.m, c.d,
                sign * (long)d.YearsValue, sign * (long)d.MonthsValue,
                sign * (long)d.WeeksValue, sign * ((long)d.DaysValue + extraDays), overflow);
            return new JSTemporalPlainDate(ny, nm, nd, calendarId, PlainDatePrototype);
        }

        return AddISODate(isoYear, isoMonth, isoDay,
            sign * (long)d.YearsValue, sign * (long)d.MonthsValue,
            sign * (long)d.WeeksValue, sign * ((long)d.DaysValue + extraDays), overflow, calendarId);
    }

    [JSExport("until", Length = 1)]
    public JSValue Until(in Arguments a) => Difference(a.GetAt(0), a.GetAt(1), 1);

    [JSExport("since", Length = 1)]
    public JSValue Since(in Arguments a) => Difference(a.GetAt(0), a.GetAt(1), -1);

    // until/since honor largestUnit (day[default]/week/month/year) and round to smallestUnit at the
    // given roundingIncrement / roundingMode (GetDifferenceSettings + RoundRelativeDuration). Rounding
    // to a unit coarser than "day" (or with a non-1 increment) is applied for the ISO calendar only;
    // the arithmetic calendars still return the unrounded largestUnit-balanced difference.
    private JSValue Difference(JSValue other, JSValue options, int sign)
    {
        var target = RequireDate(ToTemporalDate(other));
        if (calendarId != target.calendarId)
            throw JSEngine.NewRangeError("Temporal.PlainDate: cannot compute the difference between dates of different calendars");
        var (largestUnit, smallestUnit, increment, roundingMode) = ReadDifferenceSettings(options);

        // `since` is `-(this.until(other))`: the difference is always measured *from the receiver*
        // (so the year/month balancing anchors on the receiver's day-of-month), then negated for
        // since. Swapping the operands instead would re-anchor on the other date and give the wrong
        // result at month boundaries. Computed in calendar space for the arithmetic calendars.
        var self = CalendarYmd();
        var othr = target.CalendarYmd();

        double years, months, weeks, days;
        if (NonIso)
        {
            (years, months, weeks, days) = TemporalNonIso.Difference(calendarId, self.y, self.m, self.d, othr.y, othr.m, othr.d, largestUnit);
        }
        else
        {
            // The rounding mode is negated for "since" (the operands are negated), then the rounded
            // result is negated back below.
            var mode = sign == -1 ? TemporalRoundingOptions.NegateRoundingMode(roundingMode) : roundingMode;
            (years, months, weeks, days) = DifferenceISODateRounded(self.y, self.m, self.d, othr.y, othr.m, othr.d,
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
            ? JSValue.BooleanTrue : JSValue.BooleanFalse;
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
        => throw JSEngine.NewTypeError("Called Temporal.PlainDate.prototype.valueOf, which is not supported. Use Temporal.PlainDate.compare for comparison.");

    // toPlainDateTime combines this date with a PlainTime (default midnight).
    [JSExport("toPlainDateTime", Length = 0)]
    public JSValue ToPlainDateTime(in Arguments a)
    {
        var arg = a.GetAt(0);
        if (arg == null || arg.IsUndefined)
            return new JSTemporalPlainDateTime(isoYear, isoMonth, isoDay, 0, 0, 0, 0, 0, 0, JSTemporalPlainDateTime.PlainDateTimePrototype);

        var t = JSTemporalPlainTime.From(new Arguments(JSUndefined.Value, arg)) as JSTemporalPlainTime
            ?? throw JSEngine.NewTypeError("expected a Temporal.PlainTime");
        return new JSTemporalPlainDateTime(isoYear, isoMonth, isoDay,
            t.hour, t.minute, t.second, t.millisecond, t.microsecond, t.nanosecond, JSTemporalPlainDateTime.PlainDateTimePrototype);
    }

    [JSExport("toPlainYearMonth", Length = 0)]
    public JSValue ToPlainYearMonth(in Arguments a)
        => new JSTemporalPlainYearMonth(isoYear, isoMonth, isoDay, calendarId, JSTemporalPlainYearMonth.PlainYearMonthPrototype);

    [JSExport("toPlainMonthDay", Length = 0)]
    public JSValue ToPlainMonthDay(in Arguments a)
        => new JSTemporalPlainMonthDay(isoMonth, isoDay, 1972, JSTemporalPlainMonthDay.PlainMonthDayPrototype);

    // toZonedDateTime(timeZone | { timeZone, plainTime }): combine this date with a time
    // (default midnight) and interpret it in the given zone.
    [JSExport("toZonedDateTime", Length = 1)]
    public JSValue ToZonedDateTime(in Arguments a)
    {
        var item = a.GetAt(0);
        string timeZone;
        var h = 0; var mi = 0; var s = 0; var ms = 0; var us = 0; var ns = 0;

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
            }
        }
        else throw JSEngine.NewTypeError("Temporal.PlainDate.prototype.toZonedDateTime: invalid argument");

        return JSTemporalZonedDateTime.FromLocal(isoYear, isoMonth, isoDay, h, mi, s, ms, us, ns, timeZone);
    }

    // ── value coercion / validation ─────────────────────────────────────────────

    private static JSTemporalPlainDate RequireDate(JSValue value)
        => value as JSTemporalPlainDate ?? throw JSEngine.NewTypeError("expected a Temporal.PlainDate");

    private static int ToIntegerWithTruncation(JSValue value)
    {
        if (value == null || value.IsUndefined)
            return 0;

        var number = value.DoubleValue; // ToNumber (throws TypeError for BigInt/Symbol)
        if (double.IsNaN(number) || double.IsInfinity(number))
            throw JSEngine.NewRangeError("Temporal.PlainDate: date component must be finite");

        return (int)Math.Truncate(number);
    }

    // Resolves a calendar argument to a canonical id (the proleptic-Gregorian family: iso8601,
    // gregory, buddhist, roc). A missing calendar defaults to iso8601; anything else is a RangeError.
    private static string CanonicalizeCalendar(JSValue calendar)
    {
        if (calendar == null || calendar.IsUndefined)
            return "iso8601";

        return TemporalCalendar.ToSlotValue(calendar, includeArithmetic: true);
    }

    private static int MonthFromCode(string code)
    {
        var match = Regex.Match(code, @"^M(\d{2})$");
        if (!match.Success)
            throw JSEngine.NewRangeError($"Temporal: invalid monthCode \"{code}\"");

        return int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
    }

    // A monthCode for the ISO calendar must be "M01".."M12"; a well-formed code whose number is
    // outside that range (e.g. "M00", "M13", "M19") references a month that does not exist.
    private static int MonthFromCodeIso(string code)
    {
        var month = MonthFromCode(code);
        if (month is < 1 or > 12)
            throw JSEngine.NewRangeError($"Temporal: invalid monthCode \"{code}\" for the iso8601 calendar");
        return month;
    }

    // month / day fields must be a positive (≥ 1) integer (RangeError otherwise, regardless of the
    // overflow option); only the upper bound is subject to constrain / reject.
    private static int ToPositiveIntegerWithTruncation(JSValue value)
    {
        var n = ToIntegerWithTruncation(value);
        if (n < 1)
            throw JSEngine.NewRangeError("Temporal.PlainDate: month and day must be positive");
        return n;
    }

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
    private static readonly string[] DateUnits = { "year", "month", "week", "day" };

    private static (string largestUnit, string smallestUnit, int increment, string roundingMode) ReadDifferenceSettings(JSValue options)
    {
        if (options == null || options.IsUndefined)
            return ("day", "day", 1, "trunc");
        if (options is not JSObject o)
            throw JSEngine.NewTypeError("Temporal options must be an object or undefined");

        // GetDifferenceSettings reads largestUnit, roundingIncrement, roundingMode, smallestUnit in that
        // order, coercing each unit (time units included) before any is validated against the allowed
        // group, so a disallowed-unit RangeError is reported only after every option has been read.
        var largestRaw = o[KeyStrings.GetOrCreate("largestUnit")];
        var largestUnit = largestRaw.IsUndefined ? null : TemporalRoundingOptions.NormalizeAnyUnit(largestRaw.StringValue, allowAuto: true);
        if (largestUnit == "auto") largestUnit = null;

        var increment = TemporalRoundingOptions.GetRoundingIncrement(o);
        var roundingMode = TemporalRoundingOptions.GetRoundingMode(o, "trunc");

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

        var calendarId = CanonicalizeCalendar(obj[KeyStrings.GetOrCreate("calendar")]);

        if (TemporalCalendarMath.IsNonIso(calendarId))
        {
            var nonIsoOverflow = ReadOverflow(options);
            return ToNonIsoCalendarDate(obj, calendarId, nonIsoOverflow);
        }

        var yearValue = obj[KeyStrings.GetOrCreate("year")];
        var eraValue = obj[KeyStrings.GetOrCreate("era")];
        var eraYearValue = obj[KeyStrings.GetOrCreate("eraYear")];
        var monthValue = obj[KeyStrings.GetOrCreate("month")];
        var monthCodeValue = obj[KeyStrings.GetOrCreate("monthCode")];
        var dayValue = obj[KeyStrings.GetOrCreate("day")];

        var hasYear = !yearValue.IsUndefined;
        var hasEra = !eraValue.IsUndefined;
        var hasEraYear = !eraYearValue.IsUndefined;
        if (!hasYear && !(hasEra && hasEraYear))
            throw JSEngine.NewTypeError("Temporal.PlainDate: missing year (or era and eraYear)");
        if (dayValue.IsUndefined)
            throw JSEngine.NewTypeError("Temporal.PlainDate: missing day");
        if (monthValue.IsUndefined && monthCodeValue.IsUndefined)
            throw JSEngine.NewTypeError("Temporal.PlainDate: missing month / monthCode");

        var overflow = ReadOverflow(options);

        var month = monthCodeValue.IsUndefined ? ToPositiveIntegerWithTruncation(monthValue) : MonthFromCodeIso(monthCodeValue.ToString());
        if (!monthValue.IsUndefined && !monthCodeValue.IsUndefined && ToPositiveIntegerWithTruncation(monthValue) != month)
            throw JSEngine.NewRangeError("Temporal.PlainDate: month and monthCode disagree");

        var isoYear = TemporalCalendar.ResolveIsoYear(calendarId,
            hasYear, hasYear ? ToIntegerWithTruncation(yearValue) : 0,
            hasEra, hasEra ? eraValue.StringValue : null,
            hasEraYear, hasEraYear ? ToIntegerWithTruncation(eraYearValue) : 0);

        return RegulateISODate(isoYear, month, ToPositiveIntegerWithTruncation(dayValue), overflow, calendarId);
    }

    private static readonly Regex DatePattern = new(
        @"^(\d{4}|\+\d{6}|-(?!000000)\d{6})-(\d{2})-(\d{2})" +
        TemporalIsoString.TimeAndOffsetTail + TemporalIsoString.AnnotationsTail + "$",
        RegexOptions.CultureInvariant);

    private static readonly Regex CalendarAnnotation = new(@"\[!?u-ca=([^\]]+)\]", RegexOptions.CultureInvariant);

    private static JSValue ParseTemporalDateString(string text)
    {
        // Only the ASCII hyphen-minus is a valid sign; the date itself is parsed strictly below, but
        // the time/offset tail would otherwise let a U+2212 variant minus sign slip through.
        if (text.Contains('−'))
            throw JSEngine.NewRangeError($"Cannot parse Temporal.PlainDate from \"{text}\"");

        TemporalIsoString.RejectMultipleCalendarAnnotations(text);
        TemporalIsoString.RejectMalformedAnnotations(text);
        TemporalIsoString.RejectInvalidAnnotations(text);

        var match = DatePattern.Match(text);
        if (!match.Success)
            throw JSEngine.NewRangeError($"Cannot parse Temporal.PlainDate from \"{text}\"");

        TemporalIsoString.RejectTimeTailForCalendarOnly(match, text);

        var year = int.Parse(match.Groups[1].Value.Replace('−', '-'), CultureInfo.InvariantCulture);
        var month = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
        var day = int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);

        if (!IsValidISODate(year, month, day))
            throw JSEngine.NewRangeError($"Cannot parse Temporal.PlainDate from \"{text}\"");

        var calMatch = CalendarAnnotation.Match(text);
        var calendarId = calMatch.Success ? TemporalCalendar.Canonicalize(calMatch.Groups[1].Value, includeArithmetic: true) : "iso8601";

        return new JSTemporalPlainDate(year, month, day, calendarId, PlainDatePrototype);
    }

    // RegulateISODate: constrain each field into range, or reject an out-of-range value.
    private static JSValue RegulateISODate(int year, int month, int day, string overflow, string calendarId = "iso8601")
    {
        if (overflow == "reject")
        {
            if (!IsValidISODate(year, month, day))
                throw JSEngine.NewRangeError("Temporal.PlainDate: date is out of range");
        }
        else
        {
            month = Math.Clamp(month, 1, 12);
            day = Math.Clamp(day, 1, DaysInMonthOf(year, month));
            if (!IsValidISODate(year, month, day))
                throw JSEngine.NewRangeError("Temporal.PlainDate: date is out of range");
        }

        return new JSTemporalPlainDate(year, month, day, calendarId, PlainDatePrototype);
    }

    // ── non-Gregorian calendar resolution / arithmetic ──────────────────────────

    // Resolves a property bag for one of the non-Gregorian calendars to the stored ISO date
    // (see TemporalNonIso for the shared resolution shared with Temporal.PlainDateTime).
    private static JSValue ToNonIsoCalendarDate(JSObject obj, string calendarId, string overflow)
    {
        var (y, m, d) = TemporalNonIso.ToIsoFromBag(obj, calendarId, overflow, "Temporal.PlainDate");
        return new JSTemporalPlainDate(y, m, d, calendarId, PlainDatePrototype);
    }

    // ── ISO 8601 calendar arithmetic ────────────────────────────────────────────

    private static bool IsLeapYear(long y) => (y % 4 == 0 && y % 100 != 0) || y % 400 == 0;

    private static int DaysInMonthOf(long year, int month) => month switch
    {
        1 or 3 or 5 or 7 or 8 or 10 or 12 => 31,
        4 or 6 or 9 or 11 => 30,
        2 => IsLeapYear(year) ? 29 : 28,
        _ => 0,
    };

    private static bool IsValidISODate(int year, int month, int day)
    {
        if (month is < 1 or > 12) return false;
        if (day < 1 || day > DaysInMonthOf(year, month)) return false;

        var epoch = DaysFromCivil(year, month, day);
        return epoch >= MinEpochDays && epoch <= MaxEpochDays;
    }

    // Whether an epoch-day count lands on a representable ISO date (used by Temporal.Duration relativeTo
    // rounding to reject a result whose implied instant falls outside the ISO date range).
    internal static bool IsEpochDayInIsoRange(System.Numerics.BigInteger epochDays)
        => epochDays >= MinEpochDays && epochDays <= MaxEpochDays;

    private static int CompareISODate(int y1, int m1, int d1, int y2, int m2, int d2)
    {
        if (y1 != y2) return y1 < y2 ? -1 : 1;
        if (m1 != m2) return m1 < m2 ? -1 : 1;
        if (d1 != d2) return d1 < d2 ? -1 : 1;
        return 0;
    }

    private static JSValue AddISODate(int year, int month, int day, long years, long months, long weeks, long days, string overflow, string calendarId = "iso8601")
    {
        // Add years and months, balancing the month into 1..12, then constrain/reject the day.
        var total = (long)month - 1 + months;
        var newYear = year + years + FloorDiv(total, 12);
        var newMonth = (int)(((total % 12) + 12) % 12) + 1;

        var maxDay = DaysInMonthOf(newYear, newMonth);
        var regulatedDay = day;
        if (overflow == "reject")
        {
            if (day > maxDay)
                throw JSEngine.NewRangeError("Temporal.PlainDate: day out of range for resulting month");
        }
        else
        {
            regulatedDay = Math.Min(day, maxDay);
        }

        // Add weeks + days as a plain day offset via the epoch-day axis.
        var epoch = DaysFromCivil(newYear, newMonth, regulatedDay) + days + weeks * 7;
        if (epoch < MinEpochDays || epoch > MaxEpochDays)
            throw JSEngine.NewRangeError("Temporal.PlainDate: result is out of range");

        var (ry, rm, rd) = CivilFromDays(epoch);
        return new JSTemporalPlainDate((int)ry, (int)rm, (int)rd, calendarId, PlainDatePrototype);
    }

    // DifferenceISODate from a (start) to b (end), both with a >= b or a <= b resolved by sign.
    private static (double years, double months, double weeks, double days) DifferenceISODate(
        int ay, int am, int ad, int by, int bm, int bd, string largestUnit)
    {
        var startEpoch = DaysFromCivil(ay, am, ad);
        var endEpoch = DaysFromCivil(by, bm, bd);

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
        var sign = -CompareISODate(ay, am, ad, by, bm, bd); // +1 when end is after start
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

        var (iy, im) = BalanceISOYearMonth(ay + years, (long)am + months);
        var cd = Math.Min(ad, DaysInMonthOf(iy, im));
        var days = DaysFromCivil(by, bm, bd) - DaysFromCivil(iy, im, cd);

        return (years, months, 0, days);
    }

    // ── RoundRelativeDuration over the ISO day axis (no time / no time zone) ─────

    // The ISO date difference start→end balanced to largestUnit, then rounded to smallestUnit at the
    // given increment / rounding mode. For the default (smallestUnit "day", increment 1) the exact
    // largestUnit-balanced difference is returned unchanged. Shared with Temporal.PlainYearMonth.
    internal static (double years, double months, double weeks, double days) DifferenceISODateRounded(
        int sy, int sm, int sd, int ey, int em, int ed,
        string largestUnit, string smallestUnit, int increment, string roundingMode)
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
        string largestUnit, string smallestUnit, int increment, string roundingMode)
        => RoundDateDifference(sy, sm, sd, years, months, weeks, days, ey, em, ed,
            largestUnit, smallestUnit, increment, roundingMode, startTimeNs, destTimeNs);

    private static int DateDurationSign(long y, long mo, long w, long d)
        => y != 0 ? Math.Sign(y) : mo != 0 ? Math.Sign(mo) : w != 0 ? Math.Sign(w) : d != 0 ? Math.Sign(d) : 0;

    // The epoch-day number reached by adding a calendar date duration to the start date (constrain).
    internal static long AddDateGetDays(int sy, int sm, int sd, long y, long mo, long w, long d)
    {
        var r = (JSTemporalPlainDate)AddISODate(sy, sm, sd, y, mo, w, d, "constrain");
        return DaysFromCivil(r.isoYear, r.isoMonth, r.isoDay);
    }

    // NudgeToCalendarUnit + BubbleRelativeDuration: bracket the difference between the start (r1) and end
    // (r2) boundaries in the smallest unit (measured on the day axis), pick one per the rounding mode,
    // then roll an expanded unit up toward largestUnit.
    private static (long y, long mo, long w, long d) RoundDateDifference(
        int sy, int sm, int sd, long years, long months, long weeks, long days,
        int ey, int em, int ed, string largestUnit, string smallestUnit, int increment, string roundingMode,
        long startTimeNs = 0, long destTimeNs = 0)
    {
        const long NsPerDay = 86_400_000_000_000;
        var destDays = DaysFromCivil(ey, em, ed);

        // The overall sign is taken on the nanosecond axis so a same-date, time-only PlainDateTime
        // difference (date duration zero) still rounds in the right direction.
        var aNs = (System.Numerics.BigInteger)DaysFromCivil(sy, sm, sd) * NsPerDay + startTimeNs;
        var fullDestNs = (System.Numerics.BigInteger)destDays * NsPerDay + destTimeNs;
        var sign = fullDestNs > aNs ? 1 : fullDestNs < aNs ? -1 : 0;
        if (sign == 0) return (0, 0, 0, 0);

        long r1;
        (long y, long mo, long w, long d) startDur, endDur;
        switch (smallestUnit)
        {
            case "year":
                r1 = years / increment * increment;
                startDur = (r1, 0, 0, 0); endDur = (r1 + increment * sign, 0, 0, 0);
                break;
            case "month":
                r1 = months / increment * increment;
                startDur = (years, r1, 0, 0); endDur = (years, r1 + increment * sign, 0, 0);
                break;
            case "week":
            {
                // The weeks count includes the days residual beyond the years+months part.
                var afterYM = AddDateGetDays(sy, sm, sd, years, months, 0, 0);
                var (wy, wm, wd) = CivilFromDays(afterYM);
                var (wey, wem, wed) = CivilFromDays(afterYM + days);
                var until = DifferenceISODate((int)wy, (int)wm, (int)wd, (int)wey, (int)wem, (int)wed, "week");
                var totalWeeks = weeks + (long)until.weeks;
                r1 = totalWeeks / increment * increment;
                startDur = (years, months, r1, 0); endDur = (years, months, r1 + increment * sign, 0);
                break;
            }
            default: // day
                r1 = days / increment * increment;
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
                ? DaysFromCivil(sy, sm, sd)
                : AddDateGetDays(sy, sm, sd, startDur.y, startDur.mo, startDur.w, startDur.d);
        }

        // Progress between the boundaries is measured on the nanosecond axis (the start time of day
        // is baked into both boundaries, the destination time into `destNs`), so a PlainDateTime's
        // sub-day component participates in the rounding; a pure-date PlainDate passes time 0 and the
        // ratio reduces to the day axis.
        var startNs = (System.Numerics.BigInteger)startDays * NsPerDay + startTimeNs;

        // An exact difference (the destination sits on the start boundary) needs no rounding, and the
        // end boundary must not be built: for a coarse smallestUnit at the ISO limit (e.g.
        // PlainYearMonth.since at +275760-09) the next-unit boundary is out of range.
        if (startNs == fullDestNs)
            return startDur;

        var endDays = smallestUnit == "day"
            ? anchorDays + r1 + (long)increment * sign
            : AddDateGetDays(sy, sm, sd, endDur.y, endDur.mo, endDur.w, endDur.d);
        var endNs = (System.Numerics.BigInteger)endDays * NsPerDay + startTimeNs;

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

        var numerator = fullDestNs - startNs;
        var denominator = endNs - startNs;
        var absNum = System.Numerics.BigInteger.Abs(numerator);
        var absDen = System.Numerics.BigInteger.Abs(denominator);

        bool isEnd;
        if (numerator == 0) isEnd = false;
        else if (absNum == absDen) isEnd = true;
        else
        {
            var cmp = (absNum * 2).CompareTo(absDen);
            var even = Math.Abs(r1) / increment % 2 == 0;
            isEnd = TemporalRoundingOptions.ApplyRoundingPicksEnd(cmp, even,
                TemporalRoundingOptions.UnsignedRoundingMode(roundingMode, sign < 0));
        }

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

    // Normalizes a (year, month) where month may be outside 1..12 into a canonical year + 1..12 month.
    private static (int year, int month) BalanceISOYearMonth(long year, long month)
    {
        var y = year + FloorDiv(month - 1, 12);
        var m = (int)(((month - 1) % 12 + 12) % 12) + 1;
        return ((int)y, m);
    }

    // ISODateSurpasses: true when adding `years` (and then `months`) to the start date — keeping the
    // *unconstrained* start day — produces a date that passes the end in the direction of `sign`.
    private static bool ISODateSurpasses(int sign, int sy, int sm, int sd, int ey, int em, int ed, long years, long months)
    {
        var y0 = sy + years;
        if (CompareSurpasses(sign, y0, sm, sd, ey, em, ed)) return true;
        if (months == 0) return false;
        var (my, mm) = BalanceISOYearMonth(y0, (long)sm + months);
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

    // Adds whole years and months with the ISO "constrain" overflow (clamp day to month).
    private static (int y, int m, int d) AddYearsMonths(int year, int month, int day, long years, long months)
    {
        var total = (long)month - 1 + months;
        var newYear = (int)(year + years + FloorDiv(total, 12));
        var newMonth = (int)(((total % 12) + 12) % 12) + 1;
        var newDay = Math.Min(day, DaysInMonthOf(newYear, newMonth));
        return (newYear, newMonth, newDay);
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

    internal static long EpochDaysFor(int year, int month, int day) => DaysFromCivil(year, month, day);

    internal static (int y, int m, int d) DateFromEpochDays(long epoch)
    {
        var (y, m, d) = CivilFromDays(epoch);
        return ((int)y, (int)m, (int)d);
    }

    // relativeTo + (years, months, weeks, days) using the ISO "constrain" overflow; throws a
    // RangeError if the result leaves the representable range.
    internal static (int y, int m, int d) AddCalendarDate(int year, int month, int day,
        long years, long months, long weeks, long days)
    {
        var r = (JSTemporalPlainDate)AddISODate(year, month, day, years, months, weeks, days, "constrain");
        return (r.isoYear, r.isoMonth, r.isoDay);
    }

    internal static (double years, double months, double weeks, double days) DiffCalendarDate(
        int ay, int am, int ad, int by, int bm, int bd, string largestUnit)
        => DifferenceISODate(ay, am, ad, by, bm, bd, largestUnit);

    // Negates a duration component, mapping +0 to +0 (rather than -0) so a zero field of a `since`
    // result compares SameValue with the +0 the spec produces.
    internal static double Negate(double v) => v == 0 ? 0d : -v;

    private static int IsoDayOfWeek(int year, int month, int day)
    {
        var epoch = DaysFromCivil(year, month, day);
        // 1970-01-01 (epoch day 0) is a Thursday (ISO weekday 4).
        return (int)(((epoch + 3) % 7 + 7) % 7) + 1;
    }

    // ISO 8601 week-of-year and its associated week-numbering year.
    private static (int week, int year) IsoWeek(int year, int month, int day)
    {
        var ordinal = (int)(DaysFromCivil(year, month, day) - DaysFromCivil(year, 1, 1)) + 1;
        var weekday = IsoDayOfWeek(year, month, day);
        var week = (ordinal - weekday + 10) / 7;

        if (week < 1)
            return (WeeksInIsoYear(year - 1), year - 1);

        if (week > WeeksInIsoYear(year))
            return (1, year + 1);

        return (week, year);
    }

    private static int WeeksInIsoYear(int year)
    {
        int P(int y) => (y + y / 4 - y / 100 + y / 400) % 7;
        return P(year) == 4 || P(year - 1) == 3 ? 53 : 52;
    }

    private static long FloorDiv(long a, long b)
    {
        var q = a / b;
        if (a % b != 0 && (a < 0) != (b < 0)) q -= 1;
        return q;
    }

    // Days from 1970-01-01 (Howard Hinnant's days_from_civil).
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

    // YYYY-MM-DD, with a ±YYYYYY expanded year outside 0000–9999. The ISO calendar gets no
    // "[u-ca=…]" annotation under the default calendarName "auto".
    private string ToISOString(string showCalendar = "auto")
    {
        var sb = new StringBuilder();
        if (isoYear < 0 || isoYear > 9999)
            sb.Append(isoYear < 0 ? '-' : '+').Append(Math.Abs(isoYear).ToString("000000", CultureInfo.InvariantCulture));
        else
            sb.Append(isoYear.ToString("0000", CultureInfo.InvariantCulture));

        sb.Append('-').Append(isoMonth.ToString("00", CultureInfo.InvariantCulture))
          .Append('-').Append(isoDay.ToString("00", CultureInfo.InvariantCulture));

        sb.Append(FormatCalendarAnnotation(calendarId, showCalendar));
        return sb.ToString();
    }

    // FormatCalendarAnnotation: the [u-ca=…] suffix shown for a non-ISO calendar (auto), or always
    // / never / critically per the calendarName option.
    internal static string FormatCalendarAnnotation(string calendarId, string showCalendar) => showCalendar switch
    {
        "never" => "",
        "always" => $"[u-ca={calendarId}]",
        "critical" => $"[!u-ca={calendarId}]",
        _ => calendarId == "iso8601" ? "" : $"[u-ca={calendarId}]", // auto
    };
}
