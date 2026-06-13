using System;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using Broiler.JavaScript.BuiltIns.Function;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Temporal;

// Temporal.PlainDateTime (Temporal proposal §5): a calendar date plus a wall-clock time, with
// no time zone. It is the combination of PlainDate (year/month/day) and PlainTime (hour …
// nanosecond). Only the ISO 8601 calendar is supported, so its arithmetic is pure, data-free
// math. Registered under the Temporal namespace (not as a global) via Register = false.
[JSClassGenerator("PlainDateTime", Register = false)]
public partial class JSTemporalPlainDateTime : JSObject
{
    private const long NanosecondsPerDay = 86_400_000_000_000;

    // ISODateTimeWithinLimits: |epoch ns| <= 8.64e21 + one day of slack.
    private static readonly BigInteger MaxEpochNanoseconds =
        BigInteger.Parse("8640000086400000000000");
    private static readonly BigInteger MinEpochNanoseconds = -MaxEpochNanoseconds;

    internal readonly int isoYear, isoMonth, isoDay;
    internal readonly int hour, minute, second, millisecond, microsecond, nanosecond;
    internal readonly string calendarId;

    [JSExport(Length = 3)]
    public JSTemporalPlainDateTime(in Arguments a) : base(ResolvePrototype())
    {
        isoYear = ToIntegerWithTruncation(a.GetAt(0));
        isoMonth = ToIntegerWithTruncation(a.GetAt(1));
        isoDay = ToIntegerWithTruncation(a.GetAt(2));
        hour = ToIntegerWithTruncation(a.GetAt(3));
        minute = ToIntegerWithTruncation(a.GetAt(4));
        second = ToIntegerWithTruncation(a.GetAt(5));
        millisecond = ToIntegerWithTruncation(a.GetAt(6));
        microsecond = ToIntegerWithTruncation(a.GetAt(7));
        nanosecond = ToIntegerWithTruncation(a.GetAt(8));
        calendarId = CanonicalizeCalendar(a.GetAt(9));

        if (!IsValidTime(hour, minute, second, millisecond, microsecond, nanosecond))
            throw JSEngine.NewRangeError("Temporal.PlainDateTime: time component out of range");
        if (!IsValidISODateTime(isoYear, isoMonth, isoDay, hour, minute, second, millisecond, microsecond, nanosecond))
            throw JSEngine.NewRangeError("Temporal.PlainDateTime: invalid ISO date-time");
    }

    internal JSTemporalPlainDateTime(
        int isoYear, int isoMonth, int isoDay,
        int hour, int minute, int second, int millisecond, int microsecond, int nanosecond,
        JSObject prototype)
        : this(isoYear, isoMonth, isoDay, hour, minute, second, millisecond, microsecond, nanosecond, "iso8601", prototype) { }

    internal JSTemporalPlainDateTime(
        int isoYear, int isoMonth, int isoDay,
        int hour, int minute, int second, int millisecond, int microsecond, int nanosecond,
        string calendarId, JSObject prototype) : base(prototype)
    {
        this.isoYear = isoYear; this.isoMonth = isoMonth; this.isoDay = isoDay;
        this.hour = hour; this.minute = minute; this.second = second;
        this.millisecond = millisecond; this.microsecond = microsecond; this.nanosecond = nanosecond;
        this.calendarId = calendarId;
    }

    private static JSObject ResolvePrototype()
    {
        if (JSEngine.NewTarget == null && (JSEngine.Current as IJSExecutionContext)?.CurrentNewTarget == null)
            throw JSEngine.NewTypeError("Constructor Temporal.PlainDateTime requires 'new'");

        return JSEngine.NewTargetPrototype ?? PlainDateTimePrototype;
    }

    internal static JSObject PlainDateTimePrototype
    {
        get
        {
            var temporal = (JSEngine.Current as JSObject)?[KeyStrings.GetOrCreate("Temporal")] as JSObject;
            return (temporal?[KeyStrings.GetOrCreate("PlainDateTime")] as JSFunction)?.prototype;
        }
    }

    // ── accessors ───────────────────────────────────────────────────────────────

    [JSExport("calendarId")] public JSValue CalendarId => new JSString(calendarId);

    // True for the non-Gregorian calendars whose month/day structure differs from ISO; every date
    // field is then derived from the stored ISO date via TemporalCalendarMath (see PlainDate).
    private bool NonIso => TemporalCalendarMath.IsNonIso(calendarId);
    private (int y, int m, int d) CalendarYmd() => NonIso
        ? TemporalNonIso.CalendarYmd(calendarId, isoYear, isoMonth, isoDay)
        : (isoYear, isoMonth, isoDay);

    // The ISO 8601 calendar has no eras; era / eraYear are present but undefined.
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
    [JSExport("hour")] public double HourValue => hour;
    [JSExport("minute")] public double MinuteValue => minute;
    [JSExport("second")] public double SecondValue => second;
    [JSExport("millisecond")] public double MillisecondValue => millisecond;
    [JSExport("microsecond")] public double MicrosecondValue => microsecond;
    [JSExport("nanosecond")] public double NanosecondValue => nanosecond;

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
    {
        var item = a.GetAt(0);
        var overflow = ReadOverflow(a.GetAt(1));

        if (item is JSTemporalPlainDateTime dt)
            return dt.Clone();

        return ToTemporalDateTime(item, overflow);
    }

    [JSExport("compare", Length = 2)]
    internal static JSValue Compare(in Arguments a)
    {
        var one = RequireDateTime(ToTemporalDateTime(a.GetAt(0), "constrain"));
        var two = RequireDateTime(ToTemporalDateTime(a.GetAt(1), "constrain"));
        return new JSNumber(one.CompareTo(two));
    }

    // ── methods ─────────────────────────────────────────────────────────────────

    [JSExport("with", Length = 1)]
    public JSValue With(in Arguments a)
    {
        if (a.GetAt(0) is not JSObject obj)
            throw JSEngine.NewTypeError("Temporal.PlainDateTime.prototype.with requires an object");
        if (!obj[KeyStrings.GetOrCreate("calendar")].IsUndefined)
            throw JSEngine.NewTypeError("Temporal.PlainDateTime.prototype.with does not accept a calendar field");
        if (!obj[KeyStrings.GetOrCreate("timeZone")].IsUndefined)
            throw JSEngine.NewTypeError("Temporal.PlainDateTime.prototype.with does not accept a timeZone field");

        var overflow = ReadOverflow(a.GetAt(1));

        var any = false;
        var month = isoMonth; var day = isoDay;

        var year = ResolveWithYear(obj, ref any);

        var monthCodeValue = obj[KeyStrings.GetOrCreate("monthCode")];
        var monthValue = obj[KeyStrings.GetOrCreate("month")];
        if (!monthCodeValue.IsUndefined) { month = MonthFromCode(monthCodeValue.ToString()); any = true; }
        if (!monthValue.IsUndefined)
        {
            var m = ToIntegerWithTruncation(monthValue);
            if (!monthCodeValue.IsUndefined && m != month)
                throw JSEngine.NewRangeError("Temporal.PlainDateTime.with: month and monthCode disagree");
            month = m; any = true;
        }

        var dayValue = obj[KeyStrings.GetOrCreate("day")];
        if (!dayValue.IsUndefined) { day = ToIntegerWithTruncation(dayValue); any = true; }

        int Read(string name, int current) { var v = obj[KeyStrings.GetOrCreate(name)]; if (v.IsUndefined) return current; any = true; return ToIntegerWithTruncation(v); }
        var h = Read("hour", hour);
        var mi = Read("minute", minute);
        var s = Read("second", second);
        var ms = Read("millisecond", millisecond);
        var us = Read("microsecond", microsecond);
        var ns = Read("nanosecond", nanosecond);

        if (!any)
            throw JSEngine.NewTypeError("Temporal.PlainDateTime.prototype.with requires at least one field");

        return RegulateDateTime(year, month, day, h, mi, s, ms, us, ns, overflow, calendarId);
    }

    // Resolves the new ISO year from a with()-bag's year / era / eraYear fields (see PlainDate).
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
            // era and eraYear must be supplied together; a partial pair is a TypeError (raised by
            // ResolveIsoYear) rather than being completed from the receiver.
            era = hasEra ? eraValue.StringValue : null;
            eraYear = hasEraYear ? ToIntegerWithTruncation(eraYearValue) : 0;
        }

        return TemporalCalendar.ResolveIsoYear(calendarId,
            hasYear, hasYear ? ToIntegerWithTruncation(yearValue) : 0,
            hasEra, era, hasEraYear, eraYear);
    }

    [JSExport("withPlainTime", Length = 0)]
    public JSValue WithPlainTime(in Arguments a)
    {
        var arg = a.GetAt(0);
        if (arg == null || arg.IsUndefined)
            return new JSTemporalPlainDateTime(isoYear, isoMonth, isoDay, 0, 0, 0, 0, 0, 0, calendarId, PlainDateTimePrototype);

        var t = RequireTime(JSTemporalPlainTime.From(new Arguments(JSUndefined.Value, arg)));
        return new JSTemporalPlainDateTime(isoYear, isoMonth, isoDay,
            t.hour, t.minute, t.second, t.millisecond, t.microsecond, t.nanosecond, calendarId, PlainDateTimePrototype);
    }

    [JSExport("withCalendar", Length = 1)]
    public JSValue WithCalendar(in Arguments a)
    {
        var arg = a.GetAt(0);
        if (arg == null || arg.IsUndefined)
            throw JSEngine.NewTypeError("Temporal.PlainDateTime.prototype.withCalendar requires a calendar");
        return new JSTemporalPlainDateTime(isoYear, isoMonth, isoDay, hour, minute, second, millisecond, microsecond, nanosecond,
            TemporalCalendar.ToSlotValue(arg, includeArithmetic: true), PlainDateTimePrototype);
    }

    [JSExport("add", Length = 1)]
    public JSValue Add(in Arguments a) => AddDuration(a.GetAt(0), a.GetAt(1), 1);

    [JSExport("subtract", Length = 1)]
    public JSValue Subtract(in Arguments a) => AddDuration(a.GetAt(0), a.GetAt(1), -1);

    private JSValue AddDuration(JSValue durationLike, JSValue options, int sign)
    {
        var overflow = ReadOverflow(options);
        var d = (JSTemporalDuration)JSTemporalDuration.ToTemporalDuration(durationLike);

        // AddDateTime: add the time first (carrying any day overflow into the date arithmetic). The
        // time total is computed in BigInteger so large sub-second durations (e.g. milliseconds at
        // Number.MAX_SAFE_INTEGER) do not overflow before the result range is validated.
        var timeNs = (BigInteger)TimeNanoseconds() + sign * DurationTimeNanosecondsBig(d);
        var dayspillBig = FloorDiv(timeNs, NanosecondsPerDay);
        var wrapped = (long)(timeNs - dayspillBig * NanosecondsPerDay);
        if (dayspillBig < long.MinValue || dayspillBig > long.MaxValue)
            throw JSEngine.NewRangeError("Temporal.PlainDateTime: result is out of range");
        var dayspill = (long)dayspillBig;

        int ny, nm, nd;
        if (NonIso)
        {
            var c = CalendarYmd();
            (ny, nm, nd) = TemporalNonIso.AddToIso(calendarId, c.y, c.m, c.d,
                sign * (long)d.YearsValue, sign * (long)d.MonthsValue,
                sign * (long)d.WeeksValue, sign * (long)d.DaysValue + dayspill, overflow);
        }
        else
        {
            (ny, nm, nd) = AddISODate(isoYear, isoMonth, isoDay,
                sign * (long)d.YearsValue, sign * (long)d.MonthsValue,
                sign * (long)d.WeeksValue, sign * (long)d.DaysValue + dayspill, overflow);
        }

        var (h, mi, s, ms, us, ns) = FromTimeNanoseconds(wrapped);
        if (!IsValidISODateTime(ny, nm, nd, h, mi, s, ms, us, ns))
            throw JSEngine.NewRangeError("Temporal.PlainDateTime: result is out of range");

        return new JSTemporalPlainDateTime(ny, nm, nd, h, mi, s, ms, us, ns, calendarId, PlainDateTimePrototype);
    }

    [JSExport("until", Length = 1)]
    public JSValue Until(in Arguments a) => Difference(a.GetAt(0), a.GetAt(1), 1);

    [JSExport("since", Length = 1)]
    public JSValue Since(in Arguments a) => Difference(a.GetAt(0), a.GetAt(1), -1);

    // TODO: until/since honor only `largestUnit`; smallestUnit/roundingIncrement/roundingMode
    // rounding is not yet applied.
    private JSValue Difference(JSValue other, JSValue options, int sign)
    {
        var target = RequireDateTime(ToTemporalDateTime(other, "constrain"));
        if (calendarId != target.calendarId)
            throw JSEngine.NewRangeError("Temporal.PlainDateTime: cannot compute the difference between date-times of different calendars");
        var largestUnit = ReadDifferenceSettings(options);

        var (a1, a2) = sign == 1 ? (this, target) : (target, this);
        var (years, months, weeks, days, h, mi, s, ms, us, ns) = DifferenceISODateTime(a1, a2, largestUnit);
        return new JSTemporalDuration(years, months, weeks, days, h, mi, s, ms, us, ns, JSTemporalDuration.DurationPrototype);
    }

    [JSExport("round", Length = 1)]
    public JSValue Round(in Arguments a)
    {
        var (unit, increment) = ReadRoundTo(a.GetAt(0));

        if (unit == "day")
        {
            var total = TimeNanoseconds();
            var roundedDays = RoundHalfExpand(total, NanosecondsPerDay) / NanosecondsPerDay;
            var epoch = DaysFromCivil(isoYear, isoMonth, isoDay) + roundedDays;
            var (ry, rm, rd) = CivilFromDays(epoch);
            return new JSTemporalPlainDateTime((int)ry, (int)rm, (int)rd, 0, 0, 0, 0, 0, 0, calendarId, PlainDateTimePrototype);
        }

        var unitNs = UnitNanoseconds(unit) * increment;
        var rounded = RoundHalfExpand(TimeNanoseconds(), unitNs);
        var dayspill = rounded / NanosecondsPerDay;
        var wrapped = rounded - dayspill * NanosecondsPerDay;
        var ep = DaysFromCivil(isoYear, isoMonth, isoDay) + dayspill;
        var (cy, cm, cd) = CivilFromDays(ep);
        var (hh, mm, ss, l, mc, nn) = FromTimeNanoseconds(wrapped);
        return new JSTemporalPlainDateTime((int)cy, (int)cm, (int)cd, hh, mm, ss, l, mc, nn, calendarId, PlainDateTimePrototype);
    }

    [JSExport("equals", Length = 1)]
    public JSValue Equals(in Arguments a)
    {
        var other = RequireDateTime(ToTemporalDateTime(a.GetAt(0), "constrain"));
        return CompareTo(other) == 0 && calendarId == other.calendarId ? JSValue.BooleanTrue : JSValue.BooleanFalse;
    }

    [JSExport("toString", Length = 0)]
    public JSValue ToStringMethod(in Arguments a)
    {
        var options = a.GetAt(0);
        if (options == null || options.IsUndefined)
            return new JSString(ToISOString());
        if (options is not JSObject o)
            throw JSEngine.NewTypeError("Temporal options must be an object or undefined");

        // Option-reading order per TemporalDateTimeToString: calendarName, fractionalSecondDigits,
        // roundingMode, then smallestUnit.
        var showCalendar = ReadCalendarName(options);
        var digits = TemporalRoundingOptions.GetFractionalSecondDigits(o);
        var roundingMode = TemporalRoundingOptions.GetRoundingMode(o, "trunc");

        var smallestRaw = o[KeyStrings.GetOrCreate("smallestUnit")];
        string smallestUnit = null;
        if (!smallestRaw.IsUndefined)
        {
            smallestUnit = TemporalRoundingOptions.NormalizeTimeUnit(smallestRaw.StringValue, allowAuto: false);
            if (smallestUnit == "hour")
                throw JSEngine.NewRangeError("Temporal.PlainDateTime.toString: smallestUnit \"hour\" is not allowed");
        }

        // ToSecondsStringPrecisionRecord: a smallestUnit fixes both the displayed precision and the
        // rounding increment; otherwise fractionalSecondDigits ("auto" → trimmed) is used.
        long incrementNs; int fracDigits; var minutePrecision = false;
        if (smallestUnit != null)
        {
            switch (smallestUnit)
            {
                case "minute": minutePrecision = true; incrementNs = 60_000_000_000; fracDigits = 0; break;
                case "second": incrementNs = 1_000_000_000; fracDigits = 0; break;
                case "millisecond": incrementNs = 1_000_000; fracDigits = 3; break;
                case "microsecond": incrementNs = 1_000; fracDigits = 6; break;
                default: incrementNs = 1; fracDigits = 9; break; // nanosecond
            }
        }
        else if (digits < 0) // "auto"
        {
            return new JSString(ToISOString(showCalendar));
        }
        else
        {
            fracDigits = digits;
            incrementNs = TemporalRoundingOptions.Pow10(9 - digits);
        }

        // RoundISODateTime: round the wall-clock time to the increment, carrying any day overflow
        // into the date.
        int ry = isoYear, rm = isoMonth, rd = isoDay;
        var wrapped = TimeNanoseconds();
        if (incrementNs > 1)
        {
            var rounded = (long)TemporalRoundingOptions.RoundToIncrement(TimeNanoseconds(), incrementNs, roundingMode);
            var dayspill = FloorDiv(rounded, NanosecondsPerDay);
            wrapped = rounded - dayspill * NanosecondsPerDay;
            var (cy, cm, cd) = CivilFromDays(DaysFromCivil(isoYear, isoMonth, isoDay) + dayspill);
            ry = (int)cy; rm = (int)cm; rd = (int)cd;
        }

        return new JSString(FormatISOString(ry, rm, rd, wrapped, showCalendar, fracDigits, minutePrecision));
    }

    [JSExport("toJSON", Length = 0)]
    public JSValue ToJSON(in Arguments a) => new JSString(ToISOString());

    [JSExport("toLocaleString", Length = 0)]
    public JSValue ToLocaleString(in Arguments a) => new JSString(ToISOString());

    // GetTemporalShowCalendarNameOption (shared shape with PlainDate).
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
        => throw JSEngine.NewTypeError("Called Temporal.PlainDateTime.prototype.valueOf, which is not supported. Use Temporal.PlainDateTime.compare for comparison.");

    [JSExport("toPlainDate", Length = 0)]
    public JSValue ToPlainDate(in Arguments a)
        => new JSTemporalPlainDate(isoYear, isoMonth, isoDay, calendarId, JSTemporalPlainDate.PlainDatePrototype);

    [JSExport("toPlainTime", Length = 0)]
    public JSValue ToPlainTime(in Arguments a)
        => JSTemporalPlainTime.Create(hour, minute, second, millisecond, microsecond, nanosecond);

    [JSExport("toPlainYearMonth", Length = 0)]
    public JSValue ToPlainYearMonth(in Arguments a)
        => new JSTemporalPlainYearMonth(isoYear, isoMonth, isoDay, calendarId, JSTemporalPlainYearMonth.PlainYearMonthPrototype);

    [JSExport("toPlainMonthDay", Length = 0)]
    public JSValue ToPlainMonthDay(in Arguments a)
        => new JSTemporalPlainMonthDay(isoMonth, isoDay, 1972, JSTemporalPlainMonthDay.PlainMonthDayPrototype);

    // toZonedDateTime(timeZone): interpret this wall-clock datetime in the given zone.
    [JSExport("toZonedDateTime", Length = 1)]
    public JSValue ToZonedDateTime(in Arguments a)
    {
        var tz = a.GetAt(0);
        if (tz == null || !tz.IsString)
            throw JSEngine.NewTypeError("Temporal.PlainDateTime.prototype.toZonedDateTime: time zone must be a string");
        ReadDisambiguation(a.GetAt(1));
        return JSTemporalZonedDateTime.FromLocal(isoYear, isoMonth, isoDay, hour, minute, second, millisecond, microsecond, nanosecond, tz.ToString());
    }

    // GetTemporalDisambiguationOption. The "compatible" behaviour is what FromLocal already applies;
    // this validates the option (an invalid value is a RangeError, a Symbol a TypeError).
    private static void ReadDisambiguation(JSValue options)
    {
        if (options == null || options.IsUndefined) return;
        if (options is not JSObject o)
            throw JSEngine.NewTypeError("Temporal options must be an object or undefined");
        var v = o[KeyStrings.GetOrCreate("disambiguation")];
        if (v.IsUndefined) return;
        var d = v.StringValue;
        if (d is not ("compatible" or "earlier" or "later" or "reject"))
            throw JSEngine.NewRangeError($"Temporal: invalid disambiguation \"{d}\"");
    }

    // ── helpers ─────────────────────────────────────────────────────────────────

    internal JSTemporalPlainDateTime Clone()
        => new JSTemporalPlainDateTime(isoYear, isoMonth, isoDay, hour, minute, second, millisecond, microsecond, nanosecond, calendarId, PlainDateTimePrototype);

    internal long TimeNanoseconds()
        => ((((long)hour * 60 + minute) * 60 + second) * 1000L + millisecond) * 1_000_000L
           + (long)microsecond * 1000 + nanosecond;

    private int CompareTo(JSTemporalPlainDateTime other)
    {
        var c = CompareISODate(isoYear, isoMonth, isoDay, other.isoYear, other.isoMonth, other.isoDay);
        if (c != 0) return c;
        var t1 = TimeNanoseconds();
        var t2 = other.TimeNanoseconds();
        return t1 < t2 ? -1 : t1 > t2 ? 1 : 0;
    }

    private static (int h, int mi, int s, int ms, int us, int ns) FromTimeNanoseconds(long t)
    {
        var ns = (int)(t % 1000); t /= 1000;
        var us = (int)(t % 1000); t /= 1000;
        var ms = (int)(t % 1000); t /= 1000;
        var s = (int)(t % 60); t /= 60;
        var mi = (int)(t % 60); t /= 60;
        return ((int)t, mi, s, ms, us, ns);
    }

    private static JSTemporalPlainDateTime RequireDateTime(JSValue value)
        => value as JSTemporalPlainDateTime ?? throw JSEngine.NewTypeError("expected a Temporal.PlainDateTime");

    private static JSTemporalPlainTime RequireTime(JSValue value)
        => value as JSTemporalPlainTime ?? throw JSEngine.NewTypeError("expected a Temporal.PlainTime");

    private static int ToIntegerWithTruncation(JSValue value)
    {
        if (value == null || value.IsUndefined)
            return 0;
        var number = value.DoubleValue;
        if (double.IsNaN(number) || double.IsInfinity(number))
            throw JSEngine.NewRangeError("Temporal.PlainDateTime: component must be finite");
        return (int)Math.Truncate(number);
    }

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

    // A monthCode for the ISO calendar must be "M01".."M12" — a well-formed code whose number is
    // outside that range (e.g. "M00", "M13") references a month that does not exist.
    private static int MonthFromCodeIso(string code)
    {
        var month = MonthFromCode(code);
        if (month is < 1 or > 12)
            throw JSEngine.NewRangeError($"Temporal: invalid monthCode \"{code}\" for the iso8601 calendar");
        return month;
    }

    // month / day fields must be a positive (≥ 1) integer.
    private static int ToPositiveIntegerWithTruncation(JSValue value)
    {
        var n = ToIntegerWithTruncation(value);
        if (n < 1)
            throw JSEngine.NewRangeError("Temporal.PlainDateTime: month and day must be positive");
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

    // GetDifferenceSettings for until/since: validates largestUnit, smallestUnit, roundingIncrement
    // and roundingMode and returns the resolved largestUnit (year … nanosecond). smallestUnit must
    // not be larger than largestUnit. (A smallestUnit/increment finer than "day"/1 is validated here
    // but the rounding is not yet applied — see TODO above Difference.)
    private static string ReadDifferenceSettings(JSValue options)
    {
        if (options == null || options.IsUndefined)
            return "day";
        if (options is not JSObject o)
            throw JSEngine.NewTypeError("Temporal options must be an object or undefined");

        var largestRaw = o[KeyStrings.GetOrCreate("largestUnit")];
        string largestUnit = largestRaw.IsUndefined ? null
            : largestRaw.StringValue is "auto" ? null : NormalizeUnit(largestRaw.StringValue);

        var smallestRaw = o[KeyStrings.GetOrCreate("smallestUnit")];
        var smallestUnit = smallestRaw.IsUndefined ? "nanosecond" : NormalizeUnit(smallestRaw.StringValue);

        TemporalRoundingOptions.GetRoundingIncrement(o);
        TemporalRoundingOptions.GetRoundingMode(o, "trunc");

        // largestUnit "auto" resolves to the larger of "day" and smallestUnit.
        largestUnit ??= TemporalRoundingOptions.UnitIndex(smallestUnit) < TemporalRoundingOptions.UnitIndex("day")
            ? smallestUnit : "day";

        if (TemporalRoundingOptions.UnitIndex(smallestUnit) < TemporalRoundingOptions.UnitIndex(largestUnit))
            throw JSEngine.NewRangeError("Temporal.PlainDateTime: smallestUnit must not be larger than largestUnit");

        return largestUnit;
    }

    private static string NormalizeUnit(string u) => u switch
    {
        "year" or "years" => "year",
        "month" or "months" => "month",
        "week" or "weeks" => "week",
        "day" or "days" => "day",
        "hour" or "hours" => "hour",
        "minute" or "minutes" => "minute",
        "second" or "seconds" => "second",
        "millisecond" or "milliseconds" => "millisecond",
        "microsecond" or "microseconds" => "microsecond",
        "nanosecond" or "nanoseconds" => "nanosecond",
        _ => throw JSEngine.NewRangeError($"Temporal.PlainDateTime: invalid unit \"{u}\""),
    };

    private static JSValue ToTemporalDateTime(JSValue item, string overflow)
    {
        if (item is JSTemporalPlainDateTime dt)
            return dt.Clone();

        if (item is JSTemporalPlainDate d)
            return new JSTemporalPlainDateTime(d.isoYear, d.isoMonth, d.isoDay, 0, 0, 0, 0, 0, 0, d.calendarId, PlainDateTimePrototype);

        if (item.IsString)
            return ParseTemporalDateTimeString(item.ToString());

        if (item is not JSObject obj)
            throw JSEngine.NewTypeError("Temporal.PlainDateTime: invalid value");

        var calendarId = CanonicalizeCalendar(obj[KeyStrings.GetOrCreate("calendar")]);

        int Field(string name) { var v = obj[KeyStrings.GetOrCreate(name)]; return v.IsUndefined ? 0 : ToIntegerWithTruncation(v); }

        // The non-Gregorian calendars resolve the date in calendar space (shared with PlainDate)
        // before the wall-clock time is attached.
        if (TemporalCalendarMath.IsNonIso(calendarId))
        {
            var (cy, cm, cd) = TemporalNonIso.ToIsoFromBag(obj, calendarId, overflow, "Temporal.PlainDateTime");
            return RegulateDateTime(cy, cm, cd,
                Field("hour"), Field("minute"), Field("second"), Field("millisecond"), Field("microsecond"), Field("nanosecond"),
                overflow, calendarId, dateAlreadyResolved: true);
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
            throw JSEngine.NewTypeError("Temporal.PlainDateTime: missing year (or era and eraYear)");
        if (dayValue.IsUndefined)
            throw JSEngine.NewTypeError("Temporal.PlainDateTime: missing day");
        if (monthValue.IsUndefined && monthCodeValue.IsUndefined)
            throw JSEngine.NewTypeError("Temporal.PlainDateTime: missing month / monthCode");

        var month = monthCodeValue.IsUndefined ? ToPositiveIntegerWithTruncation(monthValue) : MonthFromCodeIso(monthCodeValue.ToString());
        if (!monthValue.IsUndefined && !monthCodeValue.IsUndefined && ToPositiveIntegerWithTruncation(monthValue) != month)
            throw JSEngine.NewRangeError("Temporal.PlainDateTime: month and monthCode disagree");

        var isoYear = TemporalCalendar.ResolveIsoYear(calendarId,
            hasYear, hasYear ? ToIntegerWithTruncation(yearValue) : 0,
            hasEra, hasEra ? eraValue.StringValue : null,
            hasEraYear, hasEraYear ? ToIntegerWithTruncation(eraYearValue) : 0);

        return RegulateDateTime(isoYear, month, ToPositiveIntegerWithTruncation(dayValue),
            Field("hour"), Field("minute"), Field("second"), Field("millisecond"), Field("microsecond"), Field("nanosecond"), overflow, calendarId);
    }

    // When dateAlreadyResolved is true the (year, month, day) are an ISO date already regulated in
    // calendar space (the non-Gregorian path), so only the time component is constrained here.
    private static JSValue RegulateDateTime(int year, int month, int day, int h, int mi, int s, int ms, int us, int ns, string overflow, string calendarId = "iso8601", bool dateAlreadyResolved = false)
    {
        if (overflow == "reject")
        {
            if (!IsValidISODate(year, month, day) || !IsValidTime(h, mi, s, ms, us, ns))
                throw JSEngine.NewRangeError("Temporal.PlainDateTime: date-time is out of range");
        }
        else
        {
            if (!dateAlreadyResolved)
            {
                month = Math.Clamp(month, 1, 12);
                day = Math.Clamp(day, 1, DaysInMonthOf(year, month));
            }
            h = Math.Clamp(h, 0, 23); mi = Math.Clamp(mi, 0, 59); s = Math.Clamp(s, 0, 59);
            ms = Math.Clamp(ms, 0, 999); us = Math.Clamp(us, 0, 999); ns = Math.Clamp(ns, 0, 999);
        }

        if (!IsValidISODateTime(year, month, day, h, mi, s, ms, us, ns))
            throw JSEngine.NewRangeError("Temporal.PlainDateTime: date-time is out of range");

        return new JSTemporalPlainDateTime(year, month, day, h, mi, s, ms, us, ns, calendarId, PlainDateTimePrototype);
    }

    private static readonly Regex DateTimePattern = new(
        @"^(\d{4}|\+\d{6}|-(?!000000)\d{6})-(\d{2})-(\d{2})(?:[Tt ](\d{2})(?::?(\d{2})(?::?(\d{2})(?:[.,](\d{1,9}))?)?)?)?(?:\[[^\]]*\])*$",
        RegexOptions.CultureInvariant);

    private static JSValue ParseTemporalDateTimeString(string text)
    {
        TemporalIsoString.RejectMultipleCalendarAnnotations(text);

        var match = DateTimePattern.Match(text);
        if (!match.Success)
            throw JSEngine.NewRangeError($"Cannot parse Temporal.PlainDateTime from \"{text}\"");

        var year = int.Parse(match.Groups[1].Value.Replace('−', '-'), CultureInfo.InvariantCulture);
        var month = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
        var day = int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
        var h = match.Groups[4].Success ? int.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture) : 0;
        var mi = match.Groups[5].Success ? int.Parse(match.Groups[5].Value, CultureInfo.InvariantCulture) : 0;
        var s = match.Groups[6].Success ? int.Parse(match.Groups[6].Value, CultureInfo.InvariantCulture) : 0;

        int ms = 0, us = 0, ns = 0;
        if (match.Groups[7].Success)
        {
            var digits = match.Groups[7].Value.PadRight(9, '0');
            ms = int.Parse(digits.Substring(0, 3), CultureInfo.InvariantCulture);
            us = int.Parse(digits.Substring(3, 3), CultureInfo.InvariantCulture);
            ns = int.Parse(digits.Substring(6, 3), CultureInfo.InvariantCulture);
        }

        if (s == 60) s = 59; // ISO leap second collapses to :59.
        if (!IsValidISODate(year, month, day) || !IsValidTime(h, mi, s, ms, us, ns)
            || !IsValidISODateTime(year, month, day, h, mi, s, ms, us, ns))
            throw JSEngine.NewRangeError($"Cannot parse Temporal.PlainDateTime from \"{text}\"");

        var calMatch = CalendarAnnotation.Match(text);
        var calendarId = calMatch.Success ? TemporalCalendar.Canonicalize(calMatch.Groups[1].Value, includeArithmetic: true) : "iso8601";

        return new JSTemporalPlainDateTime(year, month, day, h, mi, s, ms, us, ns, calendarId, PlainDateTimePrototype);
    }

    private static readonly Regex CalendarAnnotation = new(@"\[!?u-ca=([^\]]+)\]", RegexOptions.CultureInvariant);

    private static BigInteger DurationTimeNanosecondsBig(JSTemporalDuration d)
        => (BigInteger)(long)d.HoursValue * 3_600_000_000_000 + (BigInteger)(long)d.MinutesValue * 60_000_000_000
         + (BigInteger)(long)d.SecondsValue * 1_000_000_000 + (BigInteger)(long)d.MillisecondsValue * 1_000_000
         + (BigInteger)(long)d.MicrosecondsValue * 1_000 + (long)d.NanosecondsValue;

    private static (double, double, double, double, double, double, double, double, double, double) DifferenceISODateTime(
        JSTemporalPlainDateTime a, JSTemporalPlainDateTime b, string largestUnit)
    {
        // a <= b expected by caller's sign handling; compute end - start.
        var timeDiff = b.TimeNanoseconds() - a.TimeNanoseconds();
        var dateSign = CompareISODate(a.isoYear, a.isoMonth, a.isoDay, b.isoYear, b.isoMonth, b.isoDay);

        var by = b.isoYear; var bm = b.isoMonth; var bd = b.isoDay;
        if (timeDiff < 0 && dateSign < 0)
        {
            // borrow a day from the date difference
            var ep = DaysFromCivil(b.isoYear, b.isoMonth, b.isoDay) - 1;
            var (yy, mm, dd) = CivilFromDays(ep);
            by = (int)yy; bm = (int)mm; bd = (int)dd;
            timeDiff += NanosecondsPerDay;
        }
        else if (timeDiff > 0 && dateSign > 0)
        {
            var ep = DaysFromCivil(b.isoYear, b.isoMonth, b.isoDay) + 1;
            var (yy, mm, dd) = CivilFromDays(ep);
            by = (int)yy; bm = (int)mm; bd = (int)dd;
            timeDiff -= NanosecondsPerDay;
        }

        var dateLargestUnit = largestUnit is "hour" or "minute" or "second" or "millisecond" or "microsecond" or "nanosecond" ? "day" : largestUnit;
        var (years, months, weeks, days) = a.NonIso
            ? NonIsoDateDifference(a.calendarId, a.isoYear, a.isoMonth, a.isoDay, by, bm, bd, dateLargestUnit)
            : DifferenceISODate(a.isoYear, a.isoMonth, a.isoDay, by, bm, bd, dateLargestUnit);

        // Distribute days into hours when largestUnit is a time unit.
        var t = timeDiff;
        var sgn = Math.Sign(t); t = Math.Abs(t);
        var ns = t % 1000; t /= 1000;
        var us = t % 1000; t /= 1000;
        var ms = t % 1000; t /= 1000;
        var sec = t % 60; t /= 60;
        var min = t % 60; t /= 60;
        var hr = t;

        return (years, months, weeks, days, sgn * hr, sgn * min, sgn * sec, sgn * ms, sgn * us, sgn * ns);
    }

    // Difference between two ISO dates projected into a non-Gregorian calendar (shared math).
    private static (double years, double months, double weeks, double days) NonIsoDateDifference(
        string calendarId, int aIsoY, int aIsoM, int aIsoD, int bIsoY, int bIsoM, int bIsoD, string largestUnit)
    {
        var a = TemporalNonIso.CalendarYmd(calendarId, aIsoY, aIsoM, aIsoD);
        var b = TemporalNonIso.CalendarYmd(calendarId, bIsoY, bIsoM, bIsoD);
        return TemporalNonIso.Difference(calendarId, a.y, a.m, a.d, b.y, b.m, b.d, largestUnit);
    }

    // ── ISO 8601 calendar arithmetic (shared with PlainDate) ──────────────────────

    private static bool IsLeapYear(long y) => (y % 4 == 0 && y % 100 != 0) || y % 400 == 0;

    private static int DaysInMonthOf(long year, int month) => month switch
    {
        1 or 3 or 5 or 7 or 8 or 10 or 12 => 31,
        4 or 6 or 9 or 11 => 30,
        2 => IsLeapYear(year) ? 29 : 28,
        _ => 0,
    };

    private static bool IsValidTime(int h, int mi, int s, int ms, int us, int ns)
        => h is >= 0 and <= 23 && mi is >= 0 and <= 59 && s is >= 0 and <= 59
           && ms is >= 0 and <= 999 && us is >= 0 and <= 999 && ns is >= 0 and <= 999;

    private static bool IsValidISODate(int year, int month, int day)
        => month is >= 1 and <= 12 && day >= 1 && day <= DaysInMonthOf(year, month);

    private static bool IsValidISODateTime(int year, int month, int day, int h, int mi, int s, int ms, int us, int ns)
    {
        if (!IsValidISODate(year, month, day)) return false;
        var dayNs = new BigInteger(DaysFromCivil(year, month, day)) * NanosecondsPerDay
                    + ((((long)h * 60 + mi) * 60 + s) * 1000L + ms) * 1_000_000L + (long)us * 1000 + ns;
        return dayNs >= MinEpochNanoseconds && dayNs <= MaxEpochNanoseconds;
    }

    private static int CompareISODate(int y1, int m1, int d1, int y2, int m2, int d2)
    {
        if (y1 != y2) return y1 < y2 ? -1 : 1;
        if (m1 != m2) return m1 < m2 ? -1 : 1;
        if (d1 != d2) return d1 < d2 ? -1 : 1;
        return 0;
    }

    private static (int y, int m, int d) AddISODate(int year, int month, int day, long years, long months, long weeks, long days, string overflow)
    {
        var total = (long)month - 1 + months;
        var newYear = (int)(year + years + FloorDiv(total, 12));
        var newMonth = (int)(((total % 12) + 12) % 12) + 1;

        var maxDay = DaysInMonthOf(newYear, newMonth);
        var regulatedDay = day;
        if (overflow == "reject")
        {
            if (day > maxDay)
                throw JSEngine.NewRangeError("Temporal.PlainDateTime: day out of range for resulting month");
        }
        else regulatedDay = Math.Min(day, maxDay);

        var epoch = DaysFromCivil(newYear, newMonth, regulatedDay) + days + weeks * 7;
        var (ry, rm, rd) = CivilFromDays(epoch);
        return ((int)ry, (int)rm, (int)rd);
    }

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

        var sign = CompareISODate(ay, am, ad, by, bm, bd);
        if (sign == 0) return (0, 0, 0, 0);
        var step = -sign;

        long years = by - ay;
        var (my, mm, md) = AddYearsMonths(ay, am, ad, years, 0);
        if (CompareISODate(my, mm, md, by, bm, bd) == step)
        {
            years -= step;
            (my, mm, md) = AddYearsMonths(ay, am, ad, years, 0);
        }

        long months = 0;
        while (true)
        {
            var (ny, nm, nd) = AddYearsMonths(my, mm, md, 0, step);
            var cmp = CompareISODate(ny, nm, nd, by, bm, bd);
            if (cmp == step) break;
            months += step; my = ny; mm = nm; md = nd;
            if (cmp == 0) break;
        }

        var days = DaysFromCivil(by, bm, bd) - DaysFromCivil(my, mm, md);

        if (largestUnit == "month") { months += years * 12; years = 0; }
        return (years, months, 0, days);
    }

    private static (int y, int m, int d) AddYearsMonths(int year, int month, int day, long years, long months)
    {
        var total = (long)month - 1 + months;
        var newYear = (int)(year + years + FloorDiv(total, 12));
        var newMonth = (int)(((total % 12) + 12) % 12) + 1;
        var newDay = Math.Min(day, DaysInMonthOf(newYear, newMonth));
        return (newYear, newMonth, newDay);
    }

    private static int IsoDayOfWeek(int year, int month, int day)
    {
        var epoch = DaysFromCivil(year, month, day);
        return (int)(((epoch + 3) % 7 + 7) % 7) + 1;
    }

    private static (int week, int year) IsoWeek(int year, int month, int day)
    {
        var ordinal = (int)(DaysFromCivil(year, month, day) - DaysFromCivil(year, 1, 1)) + 1;
        var weekday = IsoDayOfWeek(year, month, day);
        var week = (ordinal - weekday + 10) / 7;
        if (week < 1) return (WeeksInIsoYear(year - 1), year - 1);
        if (week > WeeksInIsoYear(year)) return (1, year + 1);
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

    private static BigInteger FloorDiv(BigInteger a, BigInteger b)
    {
        var q = BigInteger.DivRem(a, b, out var r);
        if (r != 0 && (a.Sign < 0) != (b.Sign < 0)) q -= 1;
        return q;
    }

    private static long UnitNanoseconds(string unit) => unit switch
    {
        "hour" => 3_600_000_000_000,
        "minute" => 60_000_000_000,
        "second" => 1_000_000_000,
        "millisecond" => 1_000_000,
        "microsecond" => 1_000,
        _ => 1,
    };

    private static long RoundHalfExpand(long value, long increment)
    {
        if (increment <= 1) return value;
        var quotient = value / increment;
        var remainder = value - quotient * increment;
        if (remainder * 2 >= increment) quotient += 1;
        return quotient * increment;
    }

    private static (string unit, int increment) ReadRoundTo(JSValue roundTo)
    {
        string smallestUnit;
        var increment = 1;

        if (roundTo.IsString) smallestUnit = roundTo.ToString();
        else if (roundTo is JSObject obj)
        {
            var unitValue = obj[KeyStrings.GetOrCreate("smallestUnit")];
            if (unitValue.IsUndefined)
                throw JSEngine.NewRangeError("Temporal.PlainDateTime.round requires a smallestUnit");
            smallestUnit = unitValue.StringValue;

            // GetRoundingIncrementOption (finite, integer, 1 … 10^9) and GetRoundingModeOption.
            increment = TemporalRoundingOptions.GetRoundingIncrement(obj);
            TemporalRoundingOptions.GetRoundingMode(obj, "halfExpand");
        }
        else throw JSEngine.NewTypeError("Temporal.PlainDateTime.round requires an options object or string");

        smallestUnit = smallestUnit switch
        {
            "day" or "days" => "day",
            "hour" or "hours" => "hour",
            "minute" or "minutes" => "minute",
            "second" or "seconds" => "second",
            "millisecond" or "milliseconds" => "millisecond",
            "microsecond" or "microseconds" => "microsecond",
            "nanosecond" or "nanoseconds" => "nanosecond",
            _ => throw JSEngine.NewRangeError($"Temporal.PlainDateTime.round: invalid smallestUnit \"{smallestUnit}\""),
        };

        return (smallestUnit, increment);
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

    // YYYY-MM-DDTHH:MM:SS with auto fractional-second precision, plus the calendar annotation.
    private string ToISOString(string showCalendar = "auto")
        => FormatISOString(isoYear, isoMonth, isoDay, TimeNanoseconds(), showCalendar, fracDigits: -1, minutePrecision: false);

    // Formats an ISO date-time string. fracDigits < 0 trims trailing zeros (auto precision);
    // fracDigits == 0 omits the fractional second; a positive count pads/truncates to that many
    // digits. minutePrecision omits the seconds component entirely.
    private string FormatISOString(int y, int mo, int d, long timeNs, string showCalendar, int fracDigits, bool minutePrecision)
    {
        var (h, mi, s, ms, us, ns) = FromTimeNanoseconds(timeNs);

        var sb = new StringBuilder();
        if (y < 0 || y > 9999)
            sb.Append(y < 0 ? '-' : '+').Append(Math.Abs(y).ToString("000000", CultureInfo.InvariantCulture));
        else
            sb.Append(y.ToString("0000", CultureInfo.InvariantCulture));

        sb.Append('-').Append(mo.ToString("00", CultureInfo.InvariantCulture))
          .Append('-').Append(d.ToString("00", CultureInfo.InvariantCulture))
          .Append('T').Append(h.ToString("00", CultureInfo.InvariantCulture))
          .Append(':').Append(mi.ToString("00", CultureInfo.InvariantCulture));

        if (!minutePrecision)
        {
            sb.Append(':').Append(s.ToString("00", CultureInfo.InvariantCulture));

            var fraction = ms * 1_000_000 + us * 1_000 + ns;
            if (fracDigits < 0)
            {
                if (fraction != 0)
                    sb.Append('.').Append(fraction.ToString("000000000", CultureInfo.InvariantCulture).TrimEnd('0'));
            }
            else if (fracDigits > 0)
            {
                sb.Append('.').Append(fraction.ToString("000000000", CultureInfo.InvariantCulture).Substring(0, fracDigits));
            }
        }

        sb.Append(JSTemporalPlainDate.FormatCalendarAnnotation(calendarId, showCalendar));
        return sb.ToString();
    }
}
