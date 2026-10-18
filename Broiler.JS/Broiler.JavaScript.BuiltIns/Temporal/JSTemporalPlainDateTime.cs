using System;
using System.Numerics;
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

// Temporal.PlainDateTime (Temporal proposal §5): a calendar date plus a wall-clock time, with
// no time zone. It is the combination of PlainDate (year/month/day) and PlainTime (hour …
// nanosecond); its calendar fields, field resolution and date addition are the shared library's
// calendars, through TemporalAdapter. Registered under the Temporal namespace (not as a global)
// via Register = false.
[JSClassGenerator("PlainDateTime", Register = false)]
public partial class JSTemporalPlainDateTime : JSObject
{
    private const long NanosecondsPerDay = 86_400_000_000_000;

    internal readonly int isoYear, isoMonth, isoDay;
    internal readonly int hour, minute, second, millisecond, microsecond, nanosecond;
    internal readonly string calendarId;

    [JSExport(Length = 3)]
    public JSTemporalPlainDateTime(in Arguments a) : base(ResolvePrototype())
    {
        isoYear = ToIntegerWithTruncationArgument(a.GetAt(0));
        isoMonth = ToIntegerWithTruncationArgument(a.GetAt(1));
        isoDay = ToIntegerWithTruncationArgument(a.GetAt(2));
        hour = ToIntegerWithTruncation(a.GetAt(3));
        minute = ToIntegerWithTruncation(a.GetAt(4));
        second = ToIntegerWithTruncation(a.GetAt(5));
        millisecond = ToIntegerWithTruncation(a.GetAt(6));
        microsecond = ToIntegerWithTruncation(a.GetAt(7));
        nanosecond = ToIntegerWithTruncation(a.GetAt(8));
        calendarId = TemporalAdapter.CalendarArgument(a.GetAt(9), "Temporal.PlainDateTime");

        if (!TemporalLimits.IsValidTime(IsoTime))
            throw JSEngine.NewRangeError("Temporal.PlainDateTime: time component out of range");
        if (!TemporalAdapter.IsoDateTimeWithinLimits(IsoDateTime))
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

    internal JSTemporalPlainDateTime(IsoDateTime dateTime, string calendarId, JSObject prototype)
        : this((int)dateTime.Date.Year, dateTime.Date.Month, dateTime.Date.Day,
            dateTime.Time.Hour, dateTime.Time.Minute, dateTime.Time.Second,
            dateTime.Time.Millisecond, dateTime.Time.Microsecond, dateTime.Time.Nanosecond, calendarId, prototype) { }

    // The slots as the shared library's records.
    internal IsoDate IsoDate => new(isoYear, isoMonth, isoDay);
    internal IsoTime IsoTime => new(hour, minute, second, millisecond, microsecond, nanosecond);
    internal IsoDateTime IsoDateTime => new(IsoDate, IsoTime);

    // CreateTemporalDateTime (Temporal §5.5.x): build a PlainDateTime, throwing a RangeError when
    // the ISO date-time falls outside the representable limits. Internal constructors do not run
    // this check, so call sites that combine fields into a new value (e.g. PlainDate.toPlainDateTime
    // at the boundary date) must go through here.
    internal static JSTemporalPlainDateTime Create(
        int isoYear, int isoMonth, int isoDay,
        int hour, int minute, int second, int millisecond, int microsecond, int nanosecond,
        string calendarId, JSObject prototype)
    {
        if (!TemporalAdapter.IsoDateTimeWithinLimits(TemporalAdapter.IsoDateTimeOf(isoYear, isoMonth, isoDay, hour, minute, second, millisecond, microsecond, nanosecond)))
            throw JSEngine.NewRangeError("Temporal.PlainDateTime: date-time is out of range");
        return new JSTemporalPlainDateTime(
            isoYear, isoMonth, isoDay, hour, minute, second, millisecond, microsecond, nanosecond, calendarId, prototype);
    }

    private static JSObject ResolvePrototype()
    {
        if (JSEngine.NewTarget == null && (JSEngine.Current as IJSExecutionContext)?.CurrentNewTarget == null)
            throw JSEngine.NewTypeError("Constructor Temporal.PlainDateTime requires 'new'");

        return JSEngine.NewTargetPrototype ?? PlainDateTimePrototype;
    }

    private static readonly KeyString PlainDateTimeIntrinsicKey = KeyStrings.GetOrCreate("Temporal.PlainDateTime");

    // %Temporal.PlainDateTime.prototype% of the current realm, never the prototype of whatever
    // `globalThis.Temporal.PlainDateTime` holds now: guest code may replace the namespace or its members.
    internal static JSObject PlainDateTimePrototype => Intrinsics.TemporalPrototype(PlainDateTimeIntrinsicKey);

    // ── accessors ───────────────────────────────────────────────────────────────

    [JSExport("calendarId")] public JSValue CalendarId => new JSString(calendarId);

    // CalendarISOToDate, the shared library's: the calendar's fields of the date, which every date getter reads.
    private CalendarDate CalendarDate => TemporalAdapter.CalendarDateOf(calendarId, IsoDate);

    // Whether the calendar's dates are not the ISO dates (neither iso8601 nor the Gregorian family): the
    // differences' rounding, part C's, still runs on ISO arithmetic for those alone.
    private bool NonIso => !TemporalAdapter.SharesIsoArithmetic(calendarId);

    // era and eraYear are undefined in the calendars without eras (iso8601, chinese, dangi).
    [JSExport("era")] public JSValue Era => TemporalAdapter.EraValue(CalendarDate);
    [JSExport("eraYear")] public JSValue EraYear => TemporalAdapter.EraYearValue(CalendarDate);
    [JSExport("year")] public double YearValue => CalendarDate.Year;
    [JSExport("month")] public double MonthValue => CalendarDate.Month;
    [JSExport("monthCode")] public JSValue MonthCode => new JSString(CalendarDate.MonthCode.ToString());
    [JSExport("day")] public double DayValue => CalendarDate.Day;
    [JSExport("hour")] public double HourValue => hour;
    [JSExport("minute")] public double MinuteValue => minute;
    [JSExport("second")] public double SecondValue => second;
    [JSExport("millisecond")] public double MillisecondValue => millisecond;
    [JSExport("microsecond")] public double MicrosecondValue => microsecond;
    [JSExport("nanosecond")] public double NanosecondValue => nanosecond;

    [JSExport("dayOfWeek")] public double DayOfWeek => CalendarDate.DayOfWeek;
    [JSExport("dayOfYear")] public double DayOfYear => CalendarDate.DayOfYear;
    [JSExport("daysInWeek")] public double DaysInWeek => 7;
    [JSExport("daysInMonth")] public double DaysInMonth => CalendarDate.DaysInMonth;
    [JSExport("daysInYear")] public double DaysInYear => CalendarDate.DaysInYear;
    [JSExport("monthsInYear")] public double MonthsInYear => CalendarDate.MonthsInYear;
    [JSExport("inLeapYear")] public bool InLeapYear => CalendarDate.InLeapYear;
    // Only the ISO calendar defines a week-numbering system; other calendars return undefined.
    [JSExport("weekOfYear")] public JSValue WeekOfYear => TemporalAdapter.WeekOfYearValue(CalendarDate);
    [JSExport("yearOfWeek")] public JSValue YearOfWeek => TemporalAdapter.YearOfWeekValue(CalendarDate);

    // ── statics ─────────────────────────────────────────────────────────────────

    [JSExport("from", Length = 1)]
    internal static JSValue From(in Arguments a)
        => ToTemporalDateTime(a.GetAt(0), a.GetAt(1));

    [JSExport("compare", Length = 2)]
    internal static JSValue Compare(in Arguments a)
    {
        var one = RequireDateTime(ToTemporalDateTime(a.GetAt(0)));
        var two = RequireDateTime(ToTemporalDateTime(a.GetAt(1)));
        return new JSNumber(one.CompareTo(two));
    }

    // ── methods ─────────────────────────────────────────────────────────────────

    [JSExport("with", Length = 1)]
    public JSValue With(in Arguments a) => WithCore(in a, null, null, null);

    // The ISO-calendar `with` field reader, with optional hooks so Temporal.ZonedDateTime.prototype.with
    // can reuse the exact same single alphabetical PrepareCalendarFields pass:
    //  - afterNanosecond runs at the alphabetical position of `offset` (between nanosecond and second),
    //    letting ZDT read its extra `offset` field in spec order;
    //  - hasExtraField makes the "at least one field" requirement count that extra field (an
    //    offset-only ZDT bag is valid);
    //  - overflowReader replaces the trailing GetTemporalOverflowOption so the caller can read the
    //    disambiguation / offset / overflow options together, after every field has been read.
    // (test262 ZonedDateTime/prototype/with order-of-operations.)
    internal JSValue WithCore(in Arguments a, Action afterNanosecond, Func<string> overflowReader, Func<bool> hasExtraField)
    {
        if (a.GetAt(0) is not JSObject obj)
            throw JSEngine.NewTypeError("Temporal.PlainDateTime.prototype.with requires an object");
        // IsPartialTemporalObject: a branded Temporal object is rejected with a TypeError via the
        // internal-slot check BEFORE any "calendar"/"timeZone" property Get fires — observable when
        // those are throwing getters (test262 .../with/calendar-temporal-object-throws). The shared
        // helper does the brand check first, then the calendar/timeZone own/inherited-property check.
        TemporalAdapter.RejectObjectWithCalendarOrTimeZone(obj);

        // ISODateToFields, then PrepareCalendarFields (partial) of the date and time field names in one
        // alphabetical pass — day, [era, eraYear,] hour, microsecond, millisecond, minute, month, monthCode,
        // nanosecond, second, year — each converted as it is read; at least one field must be present. The
        // overflow option comes afterwards, then CalendarMergeFields and InterpretTemporalDateTimeFields: the
        // shared library's CalendarDateFromFields, then RegulateTime.
        var fields = TemporalAdapter.DateToFields(calendarId, IsoDate, CalendarFieldsType.Date);
        var partial = new TemporalAdapter.CalendarFieldReader(obj, calendarId, "Temporal.PlainDateTime");
        var any = false;

        int Read(string name, int current) { var v = obj[KeyStrings.GetOrCreate(name)]; if (v.IsUndefined) return current; any = true; return ToIntegerWithTruncation(v); }

        partial.Day();
        partial.Era();
        var h = Read("hour", hour);
        var us = Read("microsecond", microsecond);
        var ms = Read("millisecond", millisecond);
        var mi = Read("minute", minute);
        partial.Month();
        partial.MonthCode();
        var ns = Read("nanosecond", nanosecond);

        // `offset` sorts between `nanosecond` and `second`; a ZonedDateTime caller reads it here.
        afterNanosecond?.Invoke();

        var s = Read("second", second);
        partial.Year();

        if (!any && !partial.Any && (hasExtraField == null || !hasExtraField()))
            throw JSEngine.NewTypeError("Temporal.PlainDateTime.prototype.with requires at least one field");

        var overflow = overflowReader != null ? overflowReader() : ReadOverflow(a.GetAt(1));

        var date = TemporalAdapter.DateFromFields(calendarId, TemporalAdapter.MergeFields(calendarId, fields, partial.Fields), overflow,
            "Temporal.PlainDateTime");
        return RegulateDateTime(date, h, mi, s, ms, us, ns, overflow, calendarId);
    }

    [JSExport("withPlainTime", Length = 0)]
    public JSValue WithPlainTime(in Arguments a)
    {
        var arg = a.GetAt(0);
        if (arg == null || arg.IsUndefined)
            return CombineWithTime(0, 0, 0, 0, 0, 0);

        var t = RequireTime(JSTemporalPlainTime.From(new Arguments(JSUndefined.Value, arg)));
        return CombineWithTime(t.hour, t.minute, t.second, t.millisecond, t.microsecond, t.nanosecond);
    }

    // Replaces the wall-clock time on the receiver's calendar date, validating that the combined
    // date-time is still within the supported ISO range (a date at the range boundary can have its
    // earliest in-range time, so a midnight time may push it out of range).
    private JSValue CombineWithTime(int h, int mi, int s, int ms, int us, int ns)
    {
        if (!TemporalAdapter.IsoDateTimeWithinLimits(TemporalAdapter.IsoDateTimeOf(isoYear, isoMonth, isoDay, h, mi, s, ms, us, ns)))
            throw JSEngine.NewRangeError("Temporal.PlainDateTime: date-time is out of range");
        return new JSTemporalPlainDateTime(isoYear, isoMonth, isoDay, h, mi, s, ms, us, ns, calendarId, PlainDateTimePrototype);
    }

    [JSExport("withCalendar", Length = 1)]
    public JSValue WithCalendar(in Arguments a)
    {
        var arg = a.GetAt(0);
        if (arg == null || arg.IsUndefined)
            throw JSEngine.NewTypeError("Temporal.PlainDateTime.prototype.withCalendar requires a calendar");
        return new JSTemporalPlainDateTime(isoYear, isoMonth, isoDay, hour, minute, second, millisecond, microsecond, nanosecond,
            TemporalAdapter.ToCalendarIdentifier(arg), PlainDateTimePrototype);
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

        // AddDurationToDateTime: the days and time fields as one exact time duration of 24-hour days (row R7: no
        // field is narrowed first), added to the wall-clock time by AddTime, whose whole days carry into the date
        // arithmetic; then the result is tested against ISODateTimeWithinLimits. The shared library's.
        var time = IsoArithmetic.AddTime(IsoTime, sign * TemporalAdapter.TimeDurationWith24HourDays(d), out var days);

        // CalendarDateAdd, the shared library's, in the date-time's calendar.
        var date = TemporalAdapter.CalendarDateAdd(calendarId, IsoDate,
            sign * d.YearsValue, sign * d.MonthsValue, sign * d.WeeksValue, days, overflow, "Temporal.PlainDateTime");

        var result = new IsoDateTime(date, time);
        if (!TemporalAdapter.IsoDateTimeWithinLimits(result))
            throw JSEngine.NewRangeError("Temporal.PlainDateTime: result is out of range");

        return new JSTemporalPlainDateTime(result, calendarId, PlainDateTimePrototype);
    }

    [JSExport("until", Length = 1)]
    public JSValue Until(in Arguments a) => Difference(a.GetAt(0), a.GetAt(1), 1);

    [JSExport("since", Length = 1)]
    public JSValue Since(in Arguments a) => Difference(a.GetAt(0), a.GetAt(1), -1);

    // until/since honor largestUnit and round the difference to smallestUnit at the given
    // roundingIncrement / roundingMode. A calendar (year/month/week/day) smallestUnit rounds the date
    // part with the wall-clock time folded into the boundary progress; a time smallestUnit rounds the
    // time, carrying any full-day overflow into the date and re-balancing.
    private JSValue Difference(JSValue other, JSValue options, int sign)
    {
        var target = RequireDateTime(ToTemporalDateTime(other));
        if (calendarId != target.calendarId)
            throw JSEngine.NewRangeError("Temporal.PlainDateTime: cannot compute the difference between date-times of different calendars");
        var (largestUnit, smallestUnit, increment, roundingMode) = ReadDifferenceSettings(options);

        // `since` is `-(this.until(other))`: always measure from the receiver (so the year/month
        // balancing anchors on the receiver) and negate for since, rather than swapping the operands.
        var (years, months, weeks, days, h, mi, s, ms, us, ns) = DifferenceISODateTime(this, target, largestUnit);

        if (!(smallestUnit == "nanosecond" && increment == 1))
        {
            // `since` negates the operands, so it negates the rounding mode, rounds, then negates back.
            var mode = sign == -1 ? ExactRounding.Negate(roundingMode) : roundingMode;

            if (smallestUnit is "year" or "month" or "week" or "day")
            {
                var (ry, rmo, rw, rd) = JSTemporalPlainDate.RoundDateTimeDateDifference(
                    isoYear, isoMonth, isoDay, (long)years, (long)months, (long)weeks, (long)days,
                    target.isoYear, target.isoMonth, target.isoDay, TimeNanoseconds(), target.TimeNanoseconds(),
                    largestUnit, smallestUnit, increment, mode);
                years = ry; months = rmo; weeks = rw; days = rd;
                h = mi = s = ms = us = ns = 0;
            }
            else
            {
                (years, months, weeks, days, h, mi, s, ms, us, ns) = RoundTimeDifference(
                    (long)years, (long)months, (long)weeks, (long)days, h, mi, s, ms, us, ns,
                    largestUnit, smallestUnit, increment, mode);
            }
        }

        if (sign == -1)
        {
            years = JSTemporalPlainDate.Negate(years); months = JSTemporalPlainDate.Negate(months);
            weeks = JSTemporalPlainDate.Negate(weeks); days = JSTemporalPlainDate.Negate(days);
            h = JSTemporalPlainDate.Negate(h); mi = JSTemporalPlainDate.Negate(mi); s = JSTemporalPlainDate.Negate(s);
            ms = JSTemporalPlainDate.Negate(ms); us = JSTemporalPlainDate.Negate(us); ns = JSTemporalPlainDate.Negate(ns);
        }
        return new JSTemporalDuration(years, months, weeks, days, h, mi, s, ms, us, ns, JSTemporalDuration.DurationPrototype);
    }

    // Rounds a date-time difference whose smallestUnit is a time unit: the residual time is rounded to
    // the increment; any full-day overflow carries into the date part (re-balanced to largestUnit when
    // that is coarser than a day). When largestUnit is itself a time unit there is no date part and
    // the whole (day-free) time total is rounded and re-balanced.
    private (double, double, double, double, double, double, double, double, double, double) RoundTimeDifference(
        long years, long months, long weeks, long days,
        double h, double mi, double s, double ms, double us, double ns,
        string largestUnit, string smallestUnit, int increment, RoundingMode roundingMode)
    {
        const long NsPerDayL = NanosecondsPerDay;
        long unitNs = smallestUnit switch
        {
            "hour" => 3_600_000_000_000L,
            "minute" => 60_000_000_000L,
            "second" => 1_000_000_000L,
            "millisecond" => 1_000_000L,
            "microsecond" => 1_000L,
            _ => 1L, // nanosecond
        } * increment;

        var timeNs = (BigInteger)(long)h * 3_600_000_000_000 + (long)mi * 60_000_000_000
                   + (long)s * 1_000_000_000 + (long)ms * 1_000_000 + (long)us * 1_000 + (long)ns;

        var isTimeLargest = largestUnit is "hour" or "minute" or "second" or "millisecond" or "microsecond" or "nanosecond";
        if (isTimeLargest)
        {
            // A time largestUnit has no date part: fold the whole-day component into the total, round
            // it, and balance from largestUnit downward.
            var total = (BigInteger)days * NsPerDayL + timeNs;
            var roundedTotal = ExactRounding.RoundNumberToIncrement(total, BigInteger.One, unitNs, roundingMode);
            var (th, tm, ts, tms, tus, tns) = BalanceTime(roundedTotal, largestUnit);
            return (0, 0, 0, 0, th, tm, ts, tms, tus, tns);
        }

        var rounded = ExactRounding.RoundNumberToIncrement(timeNs, BigInteger.One, unitNs, roundingMode);

        // Calendar largestUnit: carry any whole days the rounding spilled into the date part.
        var carryDays = (long)(rounded / NsPerDayL);
        var residual = (long)(rounded - carryDays * NsPerDayL);
        if (carryDays != 0)
        {
            var endDays = JSTemporalPlainDate.AddDateGetDays(isoYear, isoMonth, isoDay, years, months, weeks, days + carryDays);
            var (ey, em, ed) = IsoCalendarMath.FromEpochDays(endDays);
            var (by, bm, bw, bd) = JSTemporalPlainDate.DiffCalendarDate(isoYear, isoMonth, isoDay, (int)ey, em, ed, largestUnit);
            years = (long)by; months = (long)bm; weeks = (long)bw; days = (long)bd;
        }

        var r = IsoArithmetic.BalanceTime(Math.Abs(residual), out _);
        var sgn = Math.Sign(residual);
        return (years, months, weeks, days, sgn * r.Hour, sgn * r.Minute, sgn * r.Second, sgn * r.Millisecond, sgn * r.Microsecond, sgn * r.Nanosecond);
    }

    // Distributes a signed nanosecond total into time components from largestUnit downward.
    private static (double h, double mi, double s, double ms, double us, double ns) BalanceTime(BigInteger totalNs, string largestUnit)
    {
        var sgn = totalNs.Sign;
        var t = BigInteger.Abs(totalNs);
        bool At(string u) => TemporalRoundingOptions.UnitIndex(largestUnit) <= TemporalRoundingOptions.UnitIndex(u);
        BigInteger hr = 0, mn = 0, sc = 0, msv = 0, usv = 0;
        if (At("hour")) { hr = t / 3_600_000_000_000; t %= 3_600_000_000_000; }
        if (At("minute")) { mn = t / 60_000_000_000; t %= 60_000_000_000; }
        if (At("second")) { sc = t / 1_000_000_000; t %= 1_000_000_000; }
        if (At("millisecond")) { msv = t / 1_000_000; t %= 1_000_000; }
        if (At("microsecond")) { usv = t / 1_000; t %= 1_000; }
        // ℝ→𝔽 nearest double (ties to even), not .NET's truncating (double)BigInteger
        // (#818 Problems 18/19).
        double S(BigInteger v) => sgn * ExactRounding.ToDouble(v, BigInteger.One);
        return (S(hr), S(mn), S(sc), S(msv), S(usv), sgn * ExactRounding.ToDouble(t, BigInteger.One));
    }

    [JSExport("round", Length = 1)]
    public JSValue Round(in Arguments a)
    {
        var (unit, increment, roundingMode) = ReadRoundTo(a.GetAt(0));

        // RoundISODateTime, the shared library's: the time rounded and its days carried into the date. The
        // result can pass the representable range (test262 round/limits), which is the RangeError.
        var rounded = IsoArithmetic.RoundIsoDateTime(IsoDateTime, increment, TemporalAdapter.Unit(unit), roundingMode);
        if (!TemporalAdapter.IsoDateTimeWithinLimits(rounded))
            throw JSEngine.NewRangeError("Temporal.PlainDateTime: date-time is out of range");
        return new JSTemporalPlainDateTime(rounded, calendarId, PlainDateTimePrototype);
    }

    [JSExport("equals", Length = 1)]
    public JSValue Equals(in Arguments a)
    {
        var other = RequireDateTime(ToTemporalDateTime(a.GetAt(0)));
        return CompareTo(other) == 0 && calendarId == other.calendarId ? BooleanTrue : BooleanFalse;
    }

    [JSExport("toString", Length = 0)]
    public JSValue ToStringMethod(in Arguments a)
    {
        var options = a.GetAt(0);
        if (options == null || options.IsUndefined)
            return ToJSON(a);
        if (options is not JSObject o)
            throw JSEngine.NewTypeError("Temporal options must be an object or undefined");

        // The options are read in alphabetical order — calendarName, fractionalSecondDigits, roundingMode,
        // smallestUnit — each validated by its rule as it is read, the unit's group after the last read.
        var showCalendar = TemporalAdapter.ReadShowCalendarName(o);
        var digits = TemporalAdapter.ReadFractionalSecondDigits(o);
        var roundingMode = TemporalRoundingOptions.GetRoundingMode(o, RoundingMode.Trunc);
        var precision = TemporalAdapter.ToStringPrecision(TemporalAdapter.ReadSmallestUnitText(o), digits, duration: false, "Temporal.PlainDateTime");

        // RoundISODateTime, ISODateTimeWithinLimits and ISODateTimeToString: the shared library's. Rounding can
        // carry the wall clock past the representable range (test262 toString/rounding-edge-of-range).
        TemporalAdapter.Check(
            TemporalFormat.TryPlainDateTimeToString(IsoDateTime, calendarId, precision, roundingMode, showCalendar, out var text),
            "Temporal.PlainDateTime.toString: result is out of range");
        return new JSString(text);
    }

    [JSExport("toJSON", Length = 0)]
    public JSValue ToJSON(in Arguments a)
        => new JSString(TemporalFormat.DateTimeToString(IsoDateTime, calendarId, TemporalFormat.AutoPrecision, ShowCalendarName.Auto));

    [JSExport("toLocaleString", Length = 0)]
    public JSValue ToLocaleString(in Arguments a)
        => Intl.JSIntlDateTimeFormat.TemporalToLocaleString(this, a.GetAt(0), a.GetAt(1));

    [JSExport("valueOf", Length = 0)]
    public JSValue ValueOf(in Arguments a)
        => throw JSEngine.NewTypeError("Called Temporal.PlainDateTime.prototype.valueOf, which is not supported. Use Temporal.PlainDateTime.compare for comparison.");

    [JSExport("toPlainDate", Length = 0)]
    public JSValue ToPlainDate(in Arguments a)
        => new JSTemporalPlainDate(isoYear, isoMonth, isoDay, calendarId, JSTemporalPlainDate.PlainDatePrototype);

    [JSExport("toPlainTime", Length = 0)]
    public JSValue ToPlainTime(in Arguments a)
        => JSTemporalPlainTime.Create(hour, minute, second, millisecond, microsecond, nanosecond);

    // Note: Temporal.PlainDateTime.prototype.toPlainYearMonth and toPlainMonthDay were
    // removed from the Temporal proposal (June 2024 TC39 consensus). See
    // test/staging/Temporal/removed-methods.js.

    // toZonedDateTime(timeZone): interpret this wall-clock datetime in the given zone.
    [JSExport("toZonedDateTime", Length = 1)]
    public JSValue ToZonedDateTime(in Arguments a)
    {
        var tz = a.GetAt(0);
        if (tz == null || !tz.IsString)
            throw JSEngine.NewTypeError("Temporal.PlainDateTime.prototype.toZonedDateTime: time zone must be a string");
        var disambiguation = ReadDisambiguation(a.GetAt(1));
        return JSTemporalZonedDateTime.FromLocal(isoYear, isoMonth, isoDay, hour, minute, second, millisecond, microsecond, nanosecond, tz.ToString(), calendarId, disambiguation);
    }

    // GetTemporalDisambiguationOption: compatible (default) / earlier / later / reject. An invalid
    // value is a RangeError, a Symbol a TypeError. The chosen mode is applied by FromLocal when the
    // wall-clock time is ambiguous (a fold) or skipped (a spring-forward gap).
    private static string ReadDisambiguation(JSValue options)
    {
        if (options == null || options.IsUndefined) return "compatible";
        if (options is not JSObject o)
            throw JSEngine.NewTypeError("Temporal options must be an object or undefined");
        var v = o[KeyStrings.GetOrCreate("disambiguation")];
        if (v.IsUndefined) return "compatible";
        var d = v.StringValue;
        if (d is not ("compatible" or "earlier" or "later" or "reject"))
            throw JSEngine.NewRangeError($"Temporal: invalid disambiguation \"{d}\"");
        return d;
    }

    // ── helpers ─────────────────────────────────────────────────────────────────

    internal JSTemporalPlainDateTime Clone()
        => new JSTemporalPlainDateTime(isoYear, isoMonth, isoDay, hour, minute, second, millisecond, microsecond, nanosecond, calendarId, PlainDateTimePrototype);

    // The wall-clock time's nanoseconds since midnight, the shared library's.
    internal long TimeNanoseconds() => IsoArithmetic.TimeToNanoseconds(IsoTime);

    private int CompareTo(JSTemporalPlainDateTime other) => IsoArithmetic.CompareIsoDateTime(IsoDateTime, other.IsoDateTime);

    private static JSTemporalPlainDateTime RequireDateTime(JSValue value)
        => value as JSTemporalPlainDateTime ?? throw JSEngine.NewTypeError("expected a Temporal.PlainDateTime");

    private static JSTemporalPlainTime RequireTime(JSValue value)
        => value as JSTemporalPlainTime ?? throw JSEngine.NewTypeError("expected a Temporal.PlainTime");

    private static int ToIntegerWithTruncation(JSValue value)
    {
        if (value == null || value.IsUndefined)
            return 0;
        return (int)TemporalRoundingOptions.ToIntegerWithTruncation(value, "Temporal.PlainDateTime: component must be finite");
    }

    // The constructor's required date arguments (isoYear/isoMonth/isoDay) are coerced with
    // ToIntegerWithTruncation (ToNumber first), so an absent / undefined argument (NaN) is a
    // RangeError rather than silently defaulting to 0 (which is only correct for the optional time
    // components handled by the overload above).
    private static int ToIntegerWithTruncationArgument(JSValue value)
        => (int)TemporalRoundingOptions.ToIntegerWithTruncation(value ?? JSUndefined.Value, "Temporal.PlainDateTime: component must be finite");

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

    // GetDifferenceSettings for until/since: reads largestUnit, roundingIncrement, roundingMode and
    // smallestUnit (spec order), validates that largestUnit is not finer than smallestUnit and that a
    // sub-day smallestUnit's increment divides its unit evenly, and resolves the defaults
    // (smallestUnit "nanosecond"; largestUnit = the larger of "day" and smallestUnit).
    private static (string largestUnit, string smallestUnit, int increment, RoundingMode roundingMode) ReadDifferenceSettings(JSValue options)
    {
        if (options == null || options.IsUndefined)
            return ("day", "nanosecond", 1, RoundingMode.Trunc);
        if (options is not JSObject o)
            throw JSEngine.NewTypeError("Temporal options must be an object or undefined");

        var largestRaw = o[KeyStrings.GetOrCreate("largestUnit")];
        // ToString the option exactly once (GetTemporalUnit) — reading StringValue twice would invoke
        // a non-string option's toString twice, which the order-of-operations tests observe.
        string largestUnit = null;
        if (!largestRaw.IsUndefined)
        {
            var largestText = largestRaw.StringValue;
            largestUnit = largestText is "auto" ? null : NormalizeUnit(largestText);
        }

        var increment = TemporalRoundingOptions.GetRoundingIncrement(o);
        var roundingMode = TemporalRoundingOptions.GetRoundingMode(o, RoundingMode.Trunc);

        var smallestRaw = o[KeyStrings.GetOrCreate("smallestUnit")];
        var smallestUnit = smallestRaw.IsUndefined ? "nanosecond" : NormalizeUnit(smallestRaw.StringValue);

        // largestUnit "auto" resolves to the larger of "day" and smallestUnit.
        largestUnit ??= TemporalRoundingOptions.UnitIndex(smallestUnit) < TemporalRoundingOptions.UnitIndex("day")
            ? smallestUnit : "day";

        if (TemporalRoundingOptions.UnitIndex(smallestUnit) < TemporalRoundingOptions.UnitIndex(largestUnit))
            throw JSEngine.NewRangeError("Temporal.PlainDateTime: smallestUnit must not be larger than largestUnit");

        var max = TemporalRoundingOptions.MaximumRoundingIncrement(smallestUnit);
        if (max > 0)
            TemporalRoundingOptions.ValidateRoundingIncrement(increment, max, inclusive: false);

        return (largestUnit, smallestUnit, increment, roundingMode);
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

    private static JSValue ToTemporalDateTime(JSValue item) => ToTemporalDateTime(item, JSUndefined.Value);

    // `options` is the raw options argument; the overflow option is read at the spec-mandated point
    // (after the item's type is validated and its slots / fields / string are read) so an invalid
    // primitive item throws a TypeError before the options bag is ever observed.
    //
    // ToTemporalZonedDateTime reads the SAME alphabetical field list with two extra fields — `offset`
    // (between nanosecond and second) and `timeZone` (between second and year). The two optional
    // callbacks let it read those at their correct positions in this single pass (#818 Problem 7),
    // so the property-access order matches PrepareCalendarFields exactly.
    // overflowReader (ZonedDateTime.from) replaces the GetTemporalOverflowOption read at
    // the end of the property-bag pass, so the caller can read the disambiguation / offset
    // / overflow options together at that point (after all the bag's fields). When null,
    // overflow is read directly.
    internal static JSValue ToTemporalDateTime(JSValue item, JSValue options,
        Action afterNanosecond = null, Action afterSecond = null, Func<string> overflowReader = null)
    {
        if (item is JSTemporalPlainDateTime dt)
        {
            ReadOverflow(options);
            return dt.Clone();
        }

        if (item is JSTemporalPlainDate d)
        {
            ReadOverflow(options);
            return new JSTemporalPlainDateTime(d.isoYear, d.isoMonth, d.isoDay, 0, 0, 0, 0, 0, 0, d.calendarId, PlainDateTimePrototype);
        }

        if (item.IsString)
        {
            var parsed = ParseTemporalDateTimeString(item.ToString());
            ReadOverflow(options);
            return parsed;
        }

        if (item is not JSObject obj)
            throw JSEngine.NewTypeError("Temporal.PlainDateTime: invalid value");

        var calendarId = TemporalAdapter.CalendarWithIsoDefault(obj);

        int Field(string name) { var v = obj[KeyStrings.GetOrCreate(name)]; return v.IsUndefined ? 0 : ToIntegerWithTruncation(v); }

        // PrepareCalendarFields merges the date and time field names and reads them in one alphabetical
        // pass — day, [era, eraYear,] hour, microsecond, millisecond, minute, month, monthCode,
        // nanosecond, second, year — converting each as it is read (era / eraYear only for calendars that
        // have eras). The calendar's checks are the shared library's CalendarDateFromFields, after the
        // overflow option (InterpretTemporalDateTimeFields), then RegulateTime.
        var fields = new TemporalAdapter.CalendarFieldReader(obj, calendarId, "Temporal.PlainDateTime");
        fields.Day();
        fields.Era();
        var hour = Field("hour");
        var microsecond = Field("microsecond");
        var millisecond = Field("millisecond");
        var minute = Field("minute");
        fields.Month();
        fields.MonthCode();
        var nanosecond = Field("nanosecond");
        afterNanosecond?.Invoke(); // ZonedDateTime reads `offset` here
        var second = Field("second");
        afterSecond?.Invoke(); // ZonedDateTime reads `timeZone` here
        fields.Year();

        var overflow = overflowReader != null ? overflowReader() : ReadOverflow(options);

        var date = TemporalAdapter.DateFromFields(calendarId, fields.Fields, overflow, "Temporal.PlainDateTime");
        return RegulateDateTime(date, hour, minute, second, millisecond, microsecond, nanosecond, overflow, calendarId);
    }

    // The date the calendar resolved, then RegulateTime and ISODateTimeWithinLimits: the shared library's.
    private static JSValue RegulateDateTime(IsoDate date, int h, int mi, int s, int ms, int us, int ns, string overflow, string calendarId)
    {
        const string message = "Temporal.PlainDateTime: date-time is out of range";
        var time = TemporalAdapter.RegulateTime(h, mi, s, ms, us, ns, overflow, message);

        var dateTime = new IsoDateTime(date, time);
        if (!TemporalAdapter.IsoDateTimeWithinLimits(dateTime))
            throw JSEngine.NewRangeError(message);

        return new JSTemporalPlainDateTime(dateTime, calendarId, PlainDateTimePrototype);
    }

    // ParseISODateTime with the TemporalDateTimeString[~Zoned] goal (the shared library's grammar), the calendar
    // annotation canonicalized, then CreateTemporalDateTime's ISODateTimeWithinLimits.
    private static JSValue ParseTemporalDateTimeString(string text)
    {
        var parsed = TemporalAdapter.ParseIsoString(text, [TemporalGoal.DateTime], "Temporal.PlainDateTime");
        var calendarId = TemporalAdapter.CalendarOf(parsed);
        var dateTime = new IsoDateTime(TemporalAdapter.DateOf(parsed), parsed.Time ?? default);
        if (!TemporalAdapter.IsoDateTimeWithinLimits(dateTime))
            throw JSEngine.NewRangeError($"Cannot parse Temporal.PlainDateTime from \"{text}\"");

        return new JSTemporalPlainDateTime(dateTime, calendarId, PlainDateTimePrototype);
    }

    private static (double, double, double, double, double, double, double, double, double, double) DifferenceISODateTime(
        JSTemporalPlainDateTime a, JSTemporalPlainDateTime b, string largestUnit)
    {
        // a <= b expected by caller's sign handling; compute end - start.
        var timeDiff = b.TimeNanoseconds() - a.TimeNanoseconds();
        var dateSign = IsoArithmetic.CompareIsoDate(a.IsoDate, b.IsoDate);

        var by = b.isoYear; var bm = b.isoMonth; var bd = b.isoDay;
        if (timeDiff < 0 && dateSign < 0)
        {
            // borrow a day from the date difference
            var ep = IsoCalendarMath.EpochDays(b.isoYear, b.isoMonth, b.isoDay) - 1;
            var (yy, mm, dd) = IsoCalendarMath.FromEpochDays(ep);
            by = (int)yy; bm = mm; bd = dd;
            timeDiff += NanosecondsPerDay;
        }
        else if (timeDiff > 0 && dateSign > 0)
        {
            var ep = IsoCalendarMath.EpochDays(b.isoYear, b.isoMonth, b.isoDay) + 1;
            var (yy, mm, dd) = IsoCalendarMath.FromEpochDays(ep);
            by = (int)yy; bm = mm; bd = dd;
            timeDiff -= NanosecondsPerDay;
        }

        var dateLargestUnit = largestUnit is "hour" or "minute" or "second" or "millisecond" or "microsecond" or "nanosecond" ? "day" : largestUnit;
        var (years, months, weeks, days) = a.NonIso
            ? NonIsoDateDifference(a.calendarId, a.isoYear, a.isoMonth, a.isoDay, by, bm, bd, dateLargestUnit)
            : JSTemporalPlainDate.DiffCalendarDate(a.isoYear, a.isoMonth, a.isoDay, by, bm, bd, dateLargestUnit);

        // When largestUnit is a time unit, the difference has no date part (BalanceTimeDuration):
        // fold the whole-day date difference into the time total and balance from largestUnit
        // downward, so e.g. `until(…, { largestUnit: "milliseconds" })` reports the days as
        // milliseconds (days result 0) rather than leaving a stray days component.
        if (dateLargestUnit == "day" && largestUnit != "day")
        {
            var totalNs = (BigInteger)(long)days * NanosecondsPerDay + timeDiff;
            var (bh, bmi, bs, bms, bus, bns) = BalanceTime(totalNs, largestUnit);
            return (0, 0, 0, 0, bh, bmi, bs, bms, bus, bns);
        }

        // Distribute the residual time into hours..nanoseconds (the date part keeps its days).
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

    // The difference of two dates in a calendar whose dates are not the ISO dates: CalendarDateUntil, the shared library's.
    private static (double years, double months, double weeks, double days) NonIsoDateDifference(
        string calendarId, int aIsoY, int aIsoM, int aIsoD, int bIsoY, int bIsoM, int bIsoD, string largestUnit)
        => TemporalAdapter.CalendarDateUntil(calendarId, new IsoDate(aIsoY, aIsoM, aIsoD), new IsoDate(bIsoY, bIsoM, bIsoD), largestUnit);

    private static (string unit, int increment, RoundingMode roundingMode) ReadRoundTo(JSValue roundTo)
    {
        string smallestUnit;
        var increment = 1;
        var roundingMode = RoundingMode.HalfExpand;

        if (roundTo.IsString) smallestUnit = roundTo.ToString();
        else if (roundTo is JSObject obj)
        {
            // GetRoundingIncrementOption, then GetRoundingModeOption, then the REQUIRED
            // smallestUnit are read (and coerced) in that order, and all BEFORE the increment
            // is validated against the unit, so a bad increment is reported only after every
            // option getter has fired (test262 round/options-read-before-algorithmic-validation).
            increment = TemporalRoundingOptions.GetRoundingIncrement(obj);
            roundingMode = TemporalRoundingOptions.GetRoundingMode(obj, RoundingMode.HalfExpand);

            var unitValue = obj[KeyStrings.GetOrCreate("smallestUnit")];
            if (unitValue.IsUndefined)
                throw JSEngine.NewRangeError("Temporal.PlainDateTime.round requires a smallestUnit");
            smallestUnit = unitValue.StringValue;
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

        // ValidateTemporalRoundingIncrement: the increment must divide the unit's maximum (a "day"
        // allows only 1); e.g. roundingIncrement 25 with smallestUnit "hour" is a RangeError.
        var maximum = smallestUnit == "day" ? 1 : TemporalRoundingOptions.MaximumRoundingIncrement(smallestUnit);
        TemporalRoundingOptions.ValidateRoundingIncrement(increment, maximum, inclusive: smallestUnit == "day");

        return (smallestUnit, increment, roundingMode);
    }
}
