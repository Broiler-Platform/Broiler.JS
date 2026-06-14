using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using Broiler.JavaScript.BuiltIns.BigInt;
using Broiler.JavaScript.BuiltIns.Function;
using Broiler.JavaScript.BuiltIns.Null;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Temporal;

// Temporal.ZonedDateTime (Temporal proposal §6): an exact instant (epoch nanoseconds) paired
// with a time zone and the ISO 8601 calendar. Time-zone offsets are resolved through three
// kinds of zone: "UTC", a fixed numeric offset ("+01:00"), and a named IANA zone (resolved
// via .NET TimeZoneInfo, which handles DST transitions for ordinary years). Construction, from(),
// compare(), the accessor surface, the to* conversions and the DST-aware add / subtract / until /
// since arithmetic are implemented; round / with (and difference rounding) are not yet wired and
// throw. Registered under the Temporal namespace via Register = false.
[JSClassGenerator("ZonedDateTime", Register = false)]
public partial class JSTemporalZonedDateTime : JSObject
{
    private const long NanosecondsPerDay = 86_400_000_000_000;
    private static readonly BigInteger MaxEpochNanoseconds = BigInteger.Parse("8640000000000000000000");
    private static readonly BigInteger MinEpochNanoseconds = -MaxEpochNanoseconds;

    internal readonly BigInteger epochNanoseconds;
    internal readonly string timeZoneId;
    internal readonly string calendarId;

    [JSExport(Length = 2)]
    public JSTemporalZonedDateTime(in Arguments a) : base(ResolvePrototype())
    {
        var ns = JSBigInt.Coerce(a.GetAt(0) ?? JSUndefined.Value).value;
        if (!IsValid(ns))
            throw JSEngine.NewRangeError("Temporal.ZonedDateTime: epoch nanoseconds out of range");

        var tzArg = a.GetAt(1);
        if (tzArg == null || tzArg.IsUndefined || !tzArg.IsString)
            throw JSEngine.NewTypeError("Temporal.ZonedDateTime: time zone must be a string");

        epochNanoseconds = ns;
        timeZoneId = CanonicalizeTimeZone(tzArg.ToString());
        calendarId = CanonicalizeCalendar(a.GetAt(2));
    }

    internal JSTemporalZonedDateTime(BigInteger epochNanoseconds, string timeZoneId, JSObject prototype)
        : this(epochNanoseconds, timeZoneId, "iso8601", prototype) { }

    internal JSTemporalZonedDateTime(BigInteger epochNanoseconds, string timeZoneId, string calendarId, JSObject prototype) : base(prototype)
    {
        this.epochNanoseconds = epochNanoseconds;
        this.timeZoneId = timeZoneId;
        this.calendarId = calendarId;
    }

    private static JSObject ResolvePrototype()
    {
        if (JSEngine.NewTarget == null && (JSEngine.Current as IJSExecutionContext)?.CurrentNewTarget == null)
            throw JSEngine.NewTypeError("Constructor Temporal.ZonedDateTime requires 'new'");

        return JSEngine.NewTargetPrototype ?? ZonedDateTimePrototype;
    }

    internal static JSObject ZonedDateTimePrototype
    {
        get
        {
            var temporal = (JSEngine.Current as JSObject)?[KeyStrings.GetOrCreate("Temporal")] as JSObject;
            return (temporal?[KeyStrings.GetOrCreate("ZonedDateTime")] as JSFunction)?.prototype;
        }
    }

    private static bool IsValid(BigInteger ns) => ns >= MinEpochNanoseconds && ns <= MaxEpochNanoseconds;

    // Factory used by sibling Temporal types (Instant/PlainDate*.to*ZonedDateTime) to build a
    // ZonedDateTime from an epoch instant + a time-zone string, canonicalizing the zone.
    internal static JSValue CreateChecked(BigInteger epochNs, string timeZone)
    {
        if (!IsValid(epochNs))
            throw JSEngine.NewRangeError("Temporal.ZonedDateTime: epoch nanoseconds out of range");
        return new JSTemporalZonedDateTime(epochNs, CanonicalizeTimeZone(timeZone), ZonedDateTimePrototype);
    }

    // Builds a ZonedDateTime from a local wall-clock datetime in the given zone (used by the
    // PlainDate/PlainDateTime/PlainTime → ZonedDateTime conversions; "compatible" disambiguation).
    internal static JSValue FromLocal(int y, int mo, int d, int h, int mi, int s, int ms, int us, int ns, string timeZone)
    {
        var tz = CanonicalizeTimeZone(timeZone);
        var localNs = LocalNanoseconds(y, mo, d, h, mi, s, ms, us, ns);
        var offset = GetOffsetForLocal(tz, y, mo, d, h, mi, s);
        var epochNs = localNs - offset;
        if (!IsValid(epochNs))
            throw JSEngine.NewRangeError("Temporal.ZonedDateTime: out of range");
        return new JSTemporalZonedDateTime(epochNs, tz, ZonedDateTimePrototype);
    }

    // ── accessors ───────────────────────────────────────────────────────────────

    [JSExport("calendarId")] public JSValue CalendarId => new JSString(calendarId);
    [JSExport("timeZoneId")] public JSValue TimeZoneId => new JSString(timeZoneId);
    // Whether the active calendar is one of the non-ISO (arithmetic / lunisolar / solar-hijri)
    // calendars, whose date fields are derived from the local wall-clock date via TemporalCalendarMath
    // rather than read straight off the ISO fields.
    private bool NonIso => TemporalCalendarMath.IsNonIso(calendarId);

    // The local wall-clock ISO date projected into the active calendar's (year, month, day) —
    // identical to the ISO fields for the ISO/Gregorian family.
    private (int y, int m, int d) CalendarYmd()
    {
        var l = Local();
        return NonIso
            ? TemporalCalendarMath.YmdFromEpochDays(calendarId, DaysFromCivil(l.y, l.mo, l.d))
            : (l.y, l.mo, l.d);
    }

    // The ISO 8601 calendar has no eras; era / eraYear are present but undefined. The Gregorian family
    // maps the year onto an era + era-year; the lunisolar calendars are era-less.
    [JSExport("era")] public JSValue Era
    {
        get
        {
            var l = Local();
            if (!NonIso) return TemporalCalendar.Era(calendarId, l.y, l.mo, l.d);
            if (!TemporalCalendarMath.HasEra(calendarId)) return JSUndefined.Value;
            return new JSString(TemporalCalendarMath.Era(calendarId, CalendarYmd().y).code);
        }
    }
    [JSExport("eraYear")] public JSValue EraYear
    {
        get
        {
            var l = Local();
            if (!NonIso) return TemporalCalendar.EraYear(calendarId, l.y, l.mo, l.d);
            if (!TemporalCalendarMath.HasEra(calendarId)) return JSUndefined.Value;
            return new JSNumber(TemporalCalendarMath.Era(calendarId, CalendarYmd().y).eraYear);
        }
    }

    [JSExport("epochMilliseconds")] public double EpochMilliseconds => (double)FloorDiv(epochNanoseconds, 1_000_000);
    [JSExport("epochNanoseconds")] public JSValue EpochNanoseconds => new JSBigInt(epochNanoseconds);

    [JSExport("year")] public double YearValue => NonIso ? CalendarYmd().y : TemporalCalendar.Year(calendarId, Local().y);
    [JSExport("month")] public double MonthValue => NonIso ? CalendarYmd().m : Local().mo;
    [JSExport("monthCode")] public JSValue MonthCode
    {
        get { if (NonIso) { var c = CalendarYmd(); return new JSString(TemporalCalendarMath.MonthCode(calendarId, c.y, c.m)); } return new JSString($"M{Local().mo:00}"); }
    }
    [JSExport("day")] public double DayValue => NonIso ? CalendarYmd().d : Local().d;
    [JSExport("hour")] public double HourValue => Local().h;
    [JSExport("minute")] public double MinuteValue => Local().mi;
    [JSExport("second")] public double SecondValue => Local().s;
    [JSExport("millisecond")] public double MillisecondValue => Local().ms;
    [JSExport("microsecond")] public double MicrosecondValue => Local().us;
    [JSExport("nanosecond")] public double NanosecondValue => Local().ns;

    [JSExport("dayOfWeek")] public double DayOfWeek { get { var l = Local(); return IsoDayOfWeek(l.y, l.mo, l.d); } }
    [JSExport("dayOfYear")] public double DayOfYear
    {
        get
        {
            if (NonIso) { var c = CalendarYmd(); return TemporalCalendarMath.DayOfYear(calendarId, c.y, c.m, c.d); }
            var l = Local(); return (int)(DaysFromCivil(l.y, l.mo, l.d) - DaysFromCivil(l.y, 1, 1)) + 1;
        }
    }
    [JSExport("daysInWeek")] public double DaysInWeek => 7;
    [JSExport("daysInMonth")] public double DaysInMonth
    {
        get { if (NonIso) { var c = CalendarYmd(); return TemporalCalendarMath.DaysInMonth(calendarId, c.y, c.m); } var l = Local(); return DaysInMonthOf(l.y, l.mo); }
    }
    [JSExport("daysInYear")] public double DaysInYear => NonIso
        ? TemporalCalendarMath.DaysInYear(calendarId, CalendarYmd().y)
        : (IsLeapYear(Local().y) ? 366 : 365);
    [JSExport("monthsInYear")] public double MonthsInYear => NonIso
        ? TemporalCalendarMath.MonthsInYear(calendarId, CalendarYmd().y)
        : 12;
    [JSExport("inLeapYear")] public bool InLeapYear => NonIso
        ? TemporalCalendarMath.InLeapYear(calendarId, CalendarYmd().y)
        : IsLeapYear(Local().y);
    [JSExport("weekOfYear")] public double WeekOfYear { get { var l = Local(); return IsoWeek(l.y, l.mo, l.d).week; } }
    [JSExport("yearOfWeek")] public double YearOfWeek { get { var l = Local(); return IsoWeek(l.y, l.mo, l.d).year; } }

    [JSExport("offsetNanoseconds")] public double OffsetNanosecondsValue => OffsetNanoseconds();
    [JSExport("offset")] public JSValue Offset => new JSString(FormatOffset(OffsetNanoseconds()));

    [JSExport("hoursInDay")]
    public double HoursInDay
    {
        get
        {
            var l = Local();
            var startEpoch = DaysFromCivil(l.y, l.mo, l.d);
            var todayStart = StartOfDayEpochNs(l.y, l.mo, l.d);
            var (ty, tm, td) = CivilFromDays(startEpoch + 1);
            var tomorrowStart = StartOfDayEpochNs((int)ty, (int)tm, (int)td);
            return (double)(tomorrowStart - todayStart) / 3_600_000_000_000d;
        }
    }

    // ── statics ─────────────────────────────────────────────────────────────────

    [JSExport("from", Length = 1)]
    internal static JSValue From(in Arguments a)
    {
        var item = a.GetAt(0);
        // Validate the options bag (overflow / disambiguation / offset) for every input form; an
        // invalid value is a RangeError and a Symbol a TypeError, even when the offset / disambiguation
        // is not subsequently applied (only "compatible" / the offset field are honoured below).
        ReadOverflow(a.GetAt(1));
        ReadDisambiguation(a.GetAt(1));
        var offsetOption = ReadOffsetOption(a.GetAt(1), "reject");

        if (item is JSTemporalZonedDateTime zdt)
            return new JSTemporalZonedDateTime(zdt.epochNanoseconds, zdt.timeZoneId, zdt.calendarId, ZonedDateTimePrototype);

        if (item.IsString)
            return ParseZonedDateTimeString(item.ToString(), offsetOption);

        if (item is not JSObject obj)
            throw JSEngine.NewTypeError("Temporal.ZonedDateTime.from: invalid value");

        return FromPropertyBag(obj, a.GetAt(1));
    }

    [JSExport("compare", Length = 2)]
    internal static JSValue Compare(in Arguments a)
    {
        var one = Require(ToZonedDateTime(a.GetAt(0)));
        var two = Require(ToZonedDateTime(a.GetAt(1)));
        return new JSNumber(one.epochNanoseconds < two.epochNanoseconds ? -1
            : one.epochNanoseconds > two.epochNanoseconds ? 1 : 0);
    }

    // ── methods ─────────────────────────────────────────────────────────────────

    [JSExport("equals", Length = 1)]
    public JSValue Equals(in Arguments a)
    {
        var other = Require(ToZonedDateTime(a.GetAt(0)));
        return epochNanoseconds == other.epochNanoseconds && timeZoneId == other.timeZoneId
            && calendarId == other.calendarId
            ? JSValue.BooleanTrue : JSValue.BooleanFalse;
    }

    [JSExport("withTimeZone", Length = 1)]
    public JSValue WithTimeZone(in Arguments a)
    {
        var tz = a.GetAt(0);
        if (tz == null || !tz.IsString)
            throw JSEngine.NewTypeError("Temporal.ZonedDateTime.prototype.withTimeZone: time zone must be a string");
        return new JSTemporalZonedDateTime(epochNanoseconds, CanonicalizeTimeZone(tz.ToString()), calendarId, ZonedDateTimePrototype);
    }

    [JSExport("withCalendar", Length = 1)]
    public JSValue WithCalendar(in Arguments a)
    {
        var arg = a.GetAt(0);
        if (arg == null || arg.IsUndefined)
            throw JSEngine.NewTypeError("Temporal.ZonedDateTime.prototype.withCalendar requires a calendar");
        return new JSTemporalZonedDateTime(epochNanoseconds, timeZoneId, TemporalCalendar.ToSlotValue(arg, includeArithmetic: true), ZonedDateTimePrototype);
    }

    [JSExport("startOfDay", Length = 0)]
    public JSValue StartOfDay(in Arguments a)
    {
        var l = Local();
        return new JSTemporalZonedDateTime(StartOfDayEpochNs(l.y, l.mo, l.d), timeZoneId, calendarId, ZonedDateTimePrototype);
    }

    // Replaces the wall-clock time of day, keeping the date, calendar and time zone. An absent argument
    // means the start of the day (midnight, after disambiguation); otherwise the argument is coerced to
    // a Temporal.PlainTime. The new local datetime is re-resolved in the zone with "compatible"
    // disambiguation (GetOffsetForLocal handles gaps / folds).
    [JSExport("withPlainTime", Length = 0)]
    public JSValue WithPlainTime(in Arguments a)
    {
        var l = Local();
        var arg = a.GetAt(0);
        if (arg == null || arg.IsUndefined)
            return new JSTemporalZonedDateTime(StartOfDayEpochNs(l.y, l.mo, l.d), timeZoneId, calendarId, ZonedDateTimePrototype);

        var t = JSTemporalPlainTime.ToTemporalTimeObject(arg);
        var localNs = LocalNanoseconds(l.y, l.mo, l.d, t.hour, t.minute, t.second, t.millisecond, t.microsecond, t.nanosecond);
        var epoch = localNs - GetOffsetForLocal(timeZoneId, l.y, l.mo, l.d, t.hour, t.minute, t.second);
        if (!IsValid(epoch)) throw JSEngine.NewRangeError("Temporal.ZonedDateTime.prototype.withPlainTime: result is out of range");
        return new JSTemporalZonedDateTime(epoch, timeZoneId, calendarId, ZonedDateTimePrototype);
    }

    // Returns the ZonedDateTime of the nearest UTC-offset transition of the time zone after ("next") or
    // before ("previous") this instant, or null when the zone has no such transition (a fixed-offset or
    // non-DST zone, or none within the search horizon).
    [JSExport("getTimeZoneTransition", Length = 1)]
    public JSValue GetTimeZoneTransition(in Arguments a)
    {
        var arg = a.GetAt(0);
        if (arg == null || arg.IsUndefined)
            throw JSEngine.NewTypeError("Temporal.ZonedDateTime.prototype.getTimeZoneTransition requires a direction");

        string direction;
        if (arg.IsString)
            direction = arg.ToString();
        else if (arg is JSObject o)
        {
            var dv = o[KeyStrings.GetOrCreate("direction")];
            if (dv.IsUndefined)
                throw JSEngine.NewRangeError("Temporal.ZonedDateTime.prototype.getTimeZoneTransition: direction is required");
            direction = dv.StringValue;
        }
        else
            throw JSEngine.NewTypeError("Temporal.ZonedDateTime.prototype.getTimeZoneTransition: invalid direction");

        if (direction is not ("next" or "previous"))
            throw JSEngine.NewRangeError($"Temporal.ZonedDateTime.prototype.getTimeZoneTransition: invalid direction \"{direction}\"");

        // Offset (fixed and UTC) zones have no transitions.
        if (TryFixedOffset(timeZoneId, out _)) return JSNull.Value;
        var tz = ResolveNamedZone(timeZoneId);
        if (tz == null) return JSNull.Value;

        var transition = FindTransition(tz, epochNanoseconds, direction == "next");
        if (transition == null) return JSNull.Value;
        return new JSTemporalZonedDateTime(transition.Value, timeZoneId, calendarId, ZonedDateTimePrototype);
    }

    // Finds the first UTC-offset change strictly after (forward) or before (!forward) startNs by scanning
    // in day steps to bracket a transition, then binary-searching to nanosecond precision. The search
    // horizon (~50 years) covers every real IANA zone's transitions; beyond it the method reports none.
    private static BigInteger? FindTransition(TimeZoneInfo tz, BigInteger startNs, bool forward)
    {
        long OffsetAt(BigInteger ns) => (long)tz.GetUtcOffset(EpochNsToUtcDateTime(ns)).Ticks * 100;

        var dayNs = (BigInteger)86_400_000_000_000L;
        var dir = forward ? BigInteger.One : BigInteger.MinusOne;
        var lastT = startNs;
        var lastOffset = OffsetAt(startNs);

        const int maxDays = 366 * 50;
        for (var i = 1; i <= maxDays; i++)
        {
            var t = startNs + dir * dayNs * i;
            if (!IsValid(t)) return null;
            var off = OffsetAt(t);
            if (off != lastOffset)
            {
                // The transition lies between the lower and upper of {lastT, t}; binary-search the first
                // nanosecond carrying the upper bound's offset.
                var lo = BigInteger.Min(lastT, t);
                var hi = BigInteger.Max(lastT, t);
                var loOffset = OffsetAt(lo);
                while (hi - lo > 1)
                {
                    var mid = (lo + hi) / 2;
                    if (OffsetAt(mid) == loOffset) lo = mid; else hi = mid;
                }
                return hi;
            }
            lastT = t;
            lastOffset = off;
        }
        return null;
    }

    [JSExport("toInstant", Length = 0)]
    public JSValue ToInstant(in Arguments a)
        => new JSTemporalInstant(epochNanoseconds, JSTemporalInstant.InstantPrototype);

    [JSExport("toPlainDate", Length = 0)]
    public JSValue ToPlainDate(in Arguments a)
    {
        var l = Local();
        return new JSTemporalPlainDate(l.y, l.mo, l.d, calendarId, JSTemporalPlainDate.PlainDatePrototype);
    }

    [JSExport("toPlainTime", Length = 0)]
    public JSValue ToPlainTime(in Arguments a)
    {
        var l = Local();
        return JSTemporalPlainTime.Create(l.h, l.mi, l.s, l.ms, l.us, l.ns);
    }

    [JSExport("toPlainDateTime", Length = 0)]
    public JSValue ToPlainDateTime(in Arguments a)
    {
        var l = Local();
        return new JSTemporalPlainDateTime(l.y, l.mo, l.d, l.h, l.mi, l.s, l.ms, l.us, l.ns, calendarId,
            JSTemporalPlainDateTime.PlainDateTimePrototype);
    }

    [JSExport("toString", Length = 0)]
    public JSValue ToStringMethod(in Arguments a)
    {
        var options = a.GetAt(0);
        var showCalendar = ReadCalendarName(options);
        ValidateToStringOptions(options); // fractionalSecondDigits / smallestUnit / roundingMode / offset / timeZoneName
        return new JSString(ToISOString(showCalendar));
    }

    // Reads and validates the remaining toString options. The precision (fractionalSecondDigits /
    // smallestUnit / roundingMode) and the offset / time-zone-name display options are validated (an
    // invalid value is a RangeError, a Symbol a TypeError) but not yet applied — the serialization
    // below always shows the full sub-second precision, offset and time-zone name.
    private static void ValidateToStringOptions(JSValue options)
    {
        if (options is not JSObject o) return; // a non-object was already rejected by ReadCalendarName

        TemporalRoundingOptions.GetFractionalSecondDigits(o);
        var smallestUnit = o[KeyStrings.GetOrCreate("smallestUnit")];
        if (!smallestUnit.IsUndefined) TemporalRoundingOptions.NormalizeTimeUnit(smallestUnit.StringValue, allowAuto: false);
        TemporalRoundingOptions.GetRoundingMode(o, "trunc");

        var offset = o[KeyStrings.GetOrCreate("offset")];
        if (!offset.IsUndefined && offset.StringValue is not ("auto" or "never"))
            throw JSEngine.NewRangeError($"Temporal: invalid offset display option \"{offset.StringValue}\"");

        var timeZoneName = o[KeyStrings.GetOrCreate("timeZoneName")];
        if (!timeZoneName.IsUndefined && timeZoneName.StringValue is not ("auto" or "never" or "critical"))
            throw JSEngine.NewRangeError($"Temporal: invalid timeZoneName display option \"{timeZoneName.StringValue}\"");
    }

    [JSExport("toJSON", Length = 0)]
    public JSValue ToJSON(in Arguments a) => new JSString(ToISOString());

    // A ZonedDateTime formats in its own time zone with the zone name shown by default; the zone is
    // injected into the formatter options (a conflicting timeZone option is a RangeError). Direct
    // DateTimeFormat.format on a ZonedDateTime is a TypeError, but toLocaleString is allowed.
    [JSExport("toLocaleString", Length = 0)]
    public JSValue ToLocaleString(in Arguments a)
        => Intl.JSIntlDateTimeFormat.TemporalToLocaleString(this, a.GetAt(0), a.GetAt(1), defaultTimeZone: timeZoneId);

    [JSExport("valueOf", Length = 0)]
    public JSValue ValueOf(in Arguments a)
        => throw JSEngine.NewTypeError("Called Temporal.ZonedDateTime.prototype.valueOf, which is not supported. Use Temporal.ZonedDateTime.compare for comparison.");

    // Calendar + DST arithmetic. add/subtract/until/since are implemented (DST-aware); round/with
    // remain to be wired.
    [JSExport("add", Length = 1)]
    public JSValue Add(in Arguments a) => AddDuration(a.GetAt(0), a.GetAt(1), 1);
    [JSExport("subtract", Length = 1)]
    public JSValue Subtract(in Arguments a) => AddDuration(a.GetAt(0), a.GetAt(1), -1);

    [JSExport("until", Length = 1)]
    public JSValue Until(in Arguments a) => Difference(a.GetAt(0), a.GetAt(1), 1);

    [JSExport("since", Length = 1)]
    public JSValue Since(in Arguments a) => Difference(a.GetAt(0), a.GetAt(1), -1);

    [JSExport("round", Length = 1)]
    public JSValue Round(in Arguments a)
    {
        var roundTo = a.GetAt(0);
        if (roundTo == null || roundTo.IsUndefined)
            throw JSEngine.NewTypeError("Temporal.ZonedDateTime.prototype.round requires an options argument");

        string smallestUnit;
        var increment = 1;
        var roundingMode = "halfExpand";
        if (roundTo.IsString)
            smallestUnit = roundTo.StringValue;
        else if (roundTo is JSObject obj)
        {
            var unitValue = obj[KeyStrings.GetOrCreate("smallestUnit")];
            if (unitValue.IsUndefined)
                throw JSEngine.NewRangeError("Temporal.ZonedDateTime.round requires a smallestUnit");
            smallestUnit = unitValue.StringValue;
            increment = TemporalRoundingOptions.GetRoundingIncrement(obj);
            roundingMode = TemporalRoundingOptions.GetRoundingMode(obj, "halfExpand");
        }
        else throw JSEngine.NewTypeError("Temporal.ZonedDateTime.round requires an options object or string");

        smallestUnit = smallestUnit switch
        {
            "day" or "days" => "day",
            "hour" or "hours" => "hour",
            "minute" or "minutes" => "minute",
            "second" or "seconds" => "second",
            "millisecond" or "milliseconds" => "millisecond",
            "microsecond" or "microseconds" => "microsecond",
            "nanosecond" or "nanoseconds" => "nanosecond",
            _ => throw JSEngine.NewRangeError($"Temporal.ZonedDateTime.round: invalid smallestUnit \"{smallestUnit}\""),
        };

        var l = Local();
        var dayStart = StartOfDayEpochNs(l.y, l.mo, l.d);

        if (smallestUnit == "day")
        {
            // Round to a day boundary using the actual (DST-aware) length of the local day.
            var (ty, tm, td) = CivilFromDays(DaysFromCivil(l.y, l.mo, l.d) + 1);
            var dayLengthNs = StartOfDayEpochNs((int)ty, (int)tm, (int)td) - dayStart;
            var rounded = TemporalRoundingOptions.RoundToIncrement(epochNanoseconds - dayStart, dayLengthNs, roundingMode);
            var epoch = dayStart + rounded;
            if (!IsValid(epoch)) throw JSEngine.NewRangeError("Temporal.ZonedDateTime: out of range");
            return new JSTemporalZonedDateTime(epoch, timeZoneId, calendarId, ZonedDateTimePrototype);
        }

        // Time unit: cap the increment at the next unit and require it to divide evenly.
        var maxIncrement = smallestUnit switch
        {
            "hour" => 24L,
            "minute" or "second" => 60L,
            _ => 1000L, // millisecond / microsecond / nanosecond
        };
        TemporalRoundingOptions.ValidateRoundingIncrement(increment, maxIncrement, inclusive: false);

        // Round the wall-clock time of day, carrying any overflow into the date, then re-resolve in
        // the zone keeping the current offset when it is still valid ("prefer").
        var wallNs = ((((((long)l.h * 60 + l.mi) * 60 + l.s) * 1000L + l.ms) * 1000 + l.us) * 1000) + l.ns;
        var unitNs = smallestUnit switch
        {
            "hour" => 3_600_000_000_000L,
            "minute" => 60_000_000_000L,
            "second" => 1_000_000_000L,
            "millisecond" => 1_000_000L,
            "microsecond" => 1_000L,
            _ => 1L, // nanosecond
        } * increment;
        var roundedNs = (long)TemporalRoundingOptions.RoundToIncrement(wallNs, unitNs, roundingMode);
        var dayspill = FloorDiv(roundedNs, NanosecondsPerDay);
        var wrapped = roundedNs - (long)dayspill * NanosecondsPerDay;
        var (ny, nm, nd) = CivilFromDays(DaysFromCivil(l.y, l.mo, l.d) + (long)dayspill);

        var nh = (int)(wrapped / 3_600_000_000_000);
        var nmi = (int)(wrapped / 60_000_000_000 % 60);
        var ns2 = (int)(wrapped / 1_000_000_000 % 60);
        var nms = (int)(wrapped / 1_000_000 % 1000);
        var nus = (int)(wrapped / 1_000 % 1000);
        var nns = (int)(wrapped % 1000);

        var localNs = LocalNanoseconds((int)ny, (int)nm, (int)nd, nh, nmi, ns2, nms, nus, nns);
        var oldOffset = OffsetNanoseconds();
        var candidate = localNs - oldOffset;
        var resolved = GetOffsetNanosecondsFor(candidate) == oldOffset
            ? candidate
            : localNs - GetOffsetForLocal(timeZoneId, (int)ny, (int)nm, (int)nd, nh, nmi, ns2);
        if (!IsValid(resolved)) throw JSEngine.NewRangeError("Temporal.ZonedDateTime: out of range");
        return new JSTemporalZonedDateTime(resolved, timeZoneId, calendarId, ZonedDateTimePrototype);
    }

    [JSExport("with", Length = 1)]
    public JSValue With(in Arguments a)
    {
        if (a.GetAt(0) is not JSObject fields)
            throw JSEngine.NewTypeError("Temporal.ZonedDateTime.prototype.with requires an object");

        // RejectObjectWithCalendarOrTimeZone: the fields argument must be a plain property bag, not
        // a Temporal object that carries its own calendar / time zone (a Temporal date-ish type, or
        // an object with a `calendar` or `timeZone` property). The calendar / timeZone property
        // checks are also enforced by the PlainDateTime.with delegation below.
        if (fields is JSTemporalPlainDate or JSTemporalPlainDateTime or JSTemporalPlainTime
            or JSTemporalPlainYearMonth or JSTemporalPlainMonthDay or JSTemporalZonedDateTime)
            throw JSEngine.NewTypeError(
                "Temporal.ZonedDateTime.prototype.with: argument must be a plain object, not a Temporal object");

        var options = a.GetAt(1);

        // Merge the date/time fields onto the current local wall-clock datetime, reusing the
        // calendar-aware PlainDateTime.with (which rejects calendar/timeZone fields, applies the
        // overflow option and validates era / monthCode / partial era pairs).
        var l = Local();
        var current = new JSTemporalPlainDateTime(l.y, l.mo, l.d, l.h, l.mi, l.s, l.ms, l.us, l.ns, calendarId,
            JSTemporalPlainDateTime.PlainDateTimePrototype);
        var updated = (JSTemporalPlainDateTime)current.With(new Arguments(JSUndefined.Value, fields, options));

        var offsetOption = ReadOffsetOption(options);
        ReadDisambiguation(options); // validated; only "compatible" behaviour is applied below

        // The receiver's offset is part of the merged fields, so an absent (or undefined) offset means
        // "keep the current offset" (offsetOption "prefer"); an explicit string overrides it.
        long candidateOffset;
        var offsetField = fields[KeyStrings.GetOrCreate("offset")];
        if (offsetField.IsUndefined)
            candidateOffset = OffsetNanoseconds();
        else if (!offsetField.IsString || !TryParseOffsetString(offsetField.StringValue, out candidateOffset))
            throw JSEngine.NewRangeError("Temporal.ZonedDateTime.prototype.with: invalid offset");

        var localNs = LocalNanoseconds(updated.isoYear, updated.isoMonth, updated.isoDay,
            updated.hour, updated.minute, updated.second, updated.millisecond, updated.microsecond, updated.nanosecond);

        BigInteger epoch;
        if (offsetOption == "use")
            epoch = localNs - candidateOffset;
        else if (offsetOption == "ignore")
            epoch = localNs - WallOffset(updated);
        else
        {
            // "prefer" / "reject": keep the candidate offset if it is valid for the new local time,
            // otherwise fall back (prefer) or throw (reject).
            var candidateEpoch = localNs - candidateOffset;
            if (GetOffsetNanosecondsFor(candidateEpoch) == candidateOffset)
                epoch = candidateEpoch;
            else if (offsetOption == "reject")
                throw JSEngine.NewRangeError("Temporal.ZonedDateTime.prototype.with: offset does not match the time zone");
            else
                epoch = localNs - WallOffset(updated);
        }

        if (!IsValid(epoch))
            throw JSEngine.NewRangeError("Temporal.ZonedDateTime: out of range");
        return new JSTemporalZonedDateTime(epoch, timeZoneId, calendarId, ZonedDateTimePrototype);
    }

    private long WallOffset(JSTemporalPlainDateTime dt)
        => GetOffsetForLocal(timeZoneId, dt.isoYear, dt.isoMonth, dt.isoDay, dt.hour, dt.minute, dt.second);

    // GetTemporalOffsetOption: use / ignore / prefer / reject. The fallback differs by caller —
    // ZonedDateTime.from defaults to "reject", prototype.with to "prefer".
    private static string ReadOffsetOption(JSValue options, string @default = "prefer")
    {
        if (options == null || options.IsUndefined) return @default;
        if (options is not JSObject o)
            throw JSEngine.NewTypeError("Temporal options must be an object or undefined");
        var v = o[KeyStrings.GetOrCreate("offset")];
        if (v.IsUndefined) return @default;
        var s = v.StringValue;
        return s is "prefer" or "use" or "ignore" or "reject" ? s
            : throw JSEngine.NewRangeError($"Temporal: invalid offset option \"{s}\"");
    }

    // GetTemporalDisambiguationOption: compatible (default) / earlier / later / reject.
    private static string ReadDisambiguation(JSValue options)
    {
        if (options == null || options.IsUndefined) return "compatible";
        if (options is not JSObject o)
            throw JSEngine.NewTypeError("Temporal options must be an object or undefined");
        var v = o[KeyStrings.GetOrCreate("disambiguation")];
        if (v.IsUndefined) return "compatible";
        var s = v.StringValue;
        return s is "compatible" or "earlier" or "later" or "reject" ? s
            : throw JSEngine.NewRangeError($"Temporal: invalid disambiguation \"{s}\"");
    }

    private static JSException NotImplementedArithmetic(string method)
        => JSEngine.NewError($"Temporal.ZonedDateTime.prototype.{method} is not yet implemented (calendar/DST arithmetic)");

    // AddDurationToZonedDateTime: the date part (years/months/weeks/days) is added to the wall-clock
    // date in this zone — keeping the same clock time and re-resolving the zone offset, so a day is a
    // calendar day that absorbs any DST transition — and then the time part (hours … nanoseconds) is
    // added to the resulting instant on the exact (epoch-nanosecond) timeline. When the duration has
    // no date part the whole addition is a pure instant shift (matching the spec's fast path, which
    // also avoids re-resolving the offset across a DST gap/overlap).
    private JSValue AddDuration(JSValue durationLike, JSValue options, int sign)
    {
        // Per spec the duration is coerced before the options object is read.
        var d = (JSTemporalDuration)JSTemporalDuration.ToTemporalDuration(durationLike);
        var overflow = ReadOverflow(options);

        var years = sign * (long)d.YearsValue;
        var months = sign * (long)d.MonthsValue;
        var weeks = sign * (long)d.WeeksValue;
        var days = sign * (long)d.DaysValue;
        var timeNs = sign * DurationTimeNanoseconds(d);

        BigInteger resultNs;
        if (years == 0 && months == 0 && weeks == 0 && days == 0)
        {
            resultNs = epochNanoseconds + timeNs;
        }
        else
        {
            var l = Local();
            var (ry, rm, rd) = AddISODate(l.y, l.mo, l.d, years, months, weeks, days, overflow);
            var intermediateNs = EpochNsForLocal(ry, rm, rd, l.h, l.mi, l.s, l.ms, l.us, l.ns);
            resultNs = intermediateNs + timeNs;
        }

        if (!IsValid(resultNs))
            throw JSEngine.NewRangeError("Temporal.ZonedDateTime: result is out of range");
        return new JSTemporalZonedDateTime(resultNs, timeZoneId, calendarId, ZonedDateTimePrototype);
    }

    private static BigInteger DurationTimeNanoseconds(JSTemporalDuration d)
        => (BigInteger)(long)d.HoursValue * 3_600_000_000_000
         + (BigInteger)(long)d.MinutesValue * 60_000_000_000
         + (BigInteger)(long)d.SecondsValue * 1_000_000_000
         + (BigInteger)(long)d.MillisecondsValue * 1_000_000
         + (BigInteger)(long)d.MicrosecondsValue * 1_000
         + (long)d.NanosecondsValue;

    // Adds whole years + months (balancing the month and clamping/rejecting the day) then weeks +
    // days on the epoch-day axis, for the ISO calendar. Years/days far outside the representable ISO
    // range raise a RangeError (the intermediate date-time must be within limits).
    private static (int y, int m, int d) AddISODate(int year, int month, int day, long years, long months, long weeks, long days, string overflow)
    {
        var totalMonths = (long)month - 1 + months;
        var newYear = year + years + (long)FloorDiv(totalMonths, 12);
        var newMonth = (int)(((totalMonths % 12) + 12) % 12) + 1;
        if (newYear < -400000 || newYear > 400000)
            throw JSEngine.NewRangeError("Temporal.ZonedDateTime: result is out of range");

        var maxDay = DaysInMonthOf(newYear, newMonth);
        long regulatedDay;
        if (overflow == "reject")
        {
            if (day > maxDay)
                throw JSEngine.NewRangeError("Temporal.ZonedDateTime: day out of range for resulting month");
            regulatedDay = day;
        }
        else regulatedDay = Math.Min((long)day, maxDay);

        var epochDay = DaysFromCivil(newYear, newMonth, regulatedDay) + days + weeks * 7;
        if (epochDay < -100_000_001 || epochDay > 100_000_001)
            throw JSEngine.NewRangeError("Temporal.ZonedDateTime: result is out of range");

        var (ry, rm, rd) = CivilFromDays(epochDay);
        return ((int)ry, (int)rm, (int)rd);
    }

    // DifferenceTemporalZonedDateTime: the signed difference between two instants in this zone,
    // expressed as a Duration. When `largestUnit` is a time unit the result is the plain
    // epoch-nanosecond difference balanced into time components; otherwise it is the DST-aware
    // calendar difference (a "day" may be 23 h or 25 h across a transition). The smallestUnit /
    // roundingIncrement / roundingMode options are validated only via `largestUnit` here — like
    // the PlainDate/PlainDateTime difference methods, no rounding is applied yet.
    private JSValue Difference(JSValue otherValue, JSValue options, int sign)
    {
        var other = Require(ToZonedDateTime(otherValue));
        if (calendarId != other.calendarId)
            throw JSEngine.NewRangeError("Temporal.ZonedDateTime: cannot compute the difference between date-times of different calendars");
        var largestUnit = ReadLargestUnit(options, "hour");
        ValidateDifferenceRounding(options); // smallestUnit / roundingIncrement / roundingMode

        var result = IsTimeUnit(largestUnit)
            ? DifferenceTimeOnly(epochNanoseconds, other.epochNanoseconds, largestUnit)
            : DifferenceCalendar(epochNanoseconds, other.epochNanoseconds, largestUnit);

        if (sign < 0)
            result = new JSTemporalDuration(
                -result.YearsValue, -result.MonthsValue, -result.WeeksValue, -result.DaysValue,
                -result.HoursValue, -result.MinutesValue, -result.SecondsValue,
                -result.MillisecondsValue, -result.MicrosecondsValue, -result.NanosecondsValue,
                JSTemporalDuration.DurationPrototype);

        return result;
    }

    private static bool IsTimeUnit(string unit) => unit is "hour" or "minute" or "second"
        or "millisecond" or "microsecond" or "nanosecond";

    // The difference as a pure time duration (no calendar units), balanced from `largestUnit` down.
    private static JSTemporalDuration DifferenceTimeOnly(BigInteger ns1, BigInteger ns2, string largestUnit)
        => BalanceTimeDuration(ns2 - ns1, largestUnit);

    // DifferenceZonedDateTime for a calendar `largestUnit` (year/month/week/day). Follows the
    // proposal's day-correction loop: the wall-clock time-of-day difference is combined with a
    // calendar date difference, correcting the intermediate date until the residual real time
    // runs in the same direction as the overall difference (which absorbs DST offset shifts).
    private JSTemporalDuration DifferenceCalendar(BigInteger ns1, BigInteger ns2, string largestUnit)
    {
        if (ns1 == ns2)
            return new JSTemporalDuration(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, JSTemporalDuration.DurationPrototype);

        var start = LocalAt(ns1);
        var end = LocalAt(ns2);
        var sign = ns2 > ns1 ? 1 : -1;

        var timeNs = TimeOfDayNs(end) - TimeOfDayNs(start);
        var dayCorrection = Math.Sign(timeNs) == -sign ? 1 : 0;

        int iy = end.y, im = end.mo, id = end.d;
        BigInteger residual;
        while (true)
        {
            var epochDay = DaysFromCivil(end.y, end.mo, end.d) - (long)dayCorrection * sign;
            var (cy, cm, cd) = CivilFromDays(epochDay);
            iy = (int)cy; im = (int)cm; id = (int)cd;

            var intermediateNs = EpochNsForLocal(iy, im, id, start.h, start.mi, start.s, start.ms, start.us, start.ns);
            residual = ns2 - intermediateNs;
            if (residual == 0 || residual.Sign == sign)
                break;

            dayCorrection++;
            if (dayCorrection > 3) // the loop is guaranteed to settle within two corrections
                break;
        }

        var (years, months, weeks, days) = DifferenceISODate(start.y, start.mo, start.d, iy, im, id, largestUnit);
        var time = BalanceTimeDuration(residual, "hour");

        return new JSTemporalDuration(years, months, weeks, days,
            time.HoursValue, time.MinutesValue, time.SecondsValue,
            time.MillisecondsValue, time.MicrosecondsValue, time.NanosecondsValue,
            JSTemporalDuration.DurationPrototype);
    }

    // Distributes a signed nanosecond total into time components, from `largestUnit` downward.
    private static JSTemporalDuration BalanceTimeDuration(BigInteger totalNs, string largestUnit)
    {
        var sign = totalNs.Sign;
        var ns = BigInteger.Abs(totalNs);

        BigInteger hours = 0, minutes = 0, seconds = 0, millis = 0, micros = 0;
        bool At(string u) => UnitRank(largestUnit) <= UnitRank(u);
        if (At("hour")) { hours = ns / 3_600_000_000_000; ns %= 3_600_000_000_000; }
        if (At("minute")) { minutes = ns / 60_000_000_000; ns %= 60_000_000_000; }
        if (At("second")) { seconds = ns / 1_000_000_000; ns %= 1_000_000_000; }
        if (At("millisecond")) { millis = ns / 1_000_000; ns %= 1_000_000; }
        if (At("microsecond")) { micros = ns / 1_000; ns %= 1_000; }
        var nanos = ns;

        double S(BigInteger v) => sign * (double)v;
        return new JSTemporalDuration(0, 0, 0, 0, S(hours), S(minutes), S(seconds), S(millis), S(micros), S(nanos),
            JSTemporalDuration.DurationPrototype);
    }

    private static readonly string[] UnitRankOrder =
        { "year", "month", "week", "day", "hour", "minute", "second", "millisecond", "microsecond", "nanosecond" };

    private static int UnitRank(string unit) => System.Array.IndexOf(UnitRankOrder, unit);

    // Reads and validates the remaining since/until difference options. These are validated (an
    // invalid value is a RangeError, a Symbol a TypeError) even though the rounding they request is
    // not yet applied to the ZonedDateTime difference — matching the calendar-difference TODO.
    private static void ValidateDifferenceRounding(JSValue options)
    {
        if (options is not JSObject o) return; // a non-object was already rejected by ReadLargestUnit
        var smallestUnit = o[KeyStrings.GetOrCreate("smallestUnit")];
        if (!smallestUnit.IsUndefined) NormalizeUnit(smallestUnit.StringValue);
        TemporalRoundingOptions.GetRoundingIncrement(o);
        TemporalRoundingOptions.GetRoundingMode(o, "trunc");
    }

    private static string ReadLargestUnit(JSValue options, string defaultUnit)
    {
        if (options == null || options.IsUndefined)
            return defaultUnit;
        if (options is not JSObject optionsObject)
            throw JSEngine.NewTypeError("Temporal.ZonedDateTime difference options must be an object or undefined");
        var v = optionsObject[KeyStrings.GetOrCreate("largestUnit")];
        if (v.IsUndefined || v.StringValue == "auto")
            return defaultUnit;
        return NormalizeUnit(v.StringValue);
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
        _ => throw JSEngine.NewRangeError($"Temporal.ZonedDateTime: invalid unit \"{u}\""),
    };

    // The wall-clock time of day, in nanoseconds since local midnight.
    private static long TimeOfDayNs((int y, int mo, int d, int h, int mi, int s, int ms, int us, int ns) l)
        => ((((long)l.h * 60 + l.mi) * 60 + l.s) * 1000L + l.ms) * 1_000_000L + (long)l.us * 1000 + l.ns;

    // Local wall-clock components for an arbitrary epoch instant in this zone.
    private (int y, int mo, int d, int h, int mi, int s, int ms, int us, int ns) LocalAt(BigInteger epochNs)
    {
        var local = epochNs + GetOffsetNanosecondsFor(epochNs);
        var totalSeconds = FloorDiv(local, 1_000_000_000);
        var fraction = (long)(local - totalSeconds * 1_000_000_000);

        var days = (long)FloorDiv(totalSeconds, 86400);
        var secondsOfDay = (long)(totalSeconds - new BigInteger(days) * 86400);
        var (y, m, d) = CivilFromDays(days);

        var h = (int)(secondsOfDay / 3600);
        var mi = (int)(secondsOfDay % 3600 / 60);
        var s = (int)(secondsOfDay % 60);
        var ms = (int)(fraction / 1_000_000);
        var us = (int)(fraction / 1000 % 1000);
        var ns = (int)(fraction % 1000);
        return ((int)y, (int)m, (int)d, h, mi, s, ms, us, ns);
    }

    // The epoch instant for a local wall-clock datetime in this zone ("compatible" disambiguation).
    private BigInteger EpochNsForLocal(int y, int mo, int d, int h, int mi, int s, int ms, int us, int ns)
        => LocalNanoseconds(y, mo, d, h, mi, s, ms, us, ns) - GetOffsetForLocal(timeZoneId, y, mo, d, h, mi, s);

    // DifferenceISODate from a start to an end date (ISO calendar), per the chosen largestUnit.
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

    private static int CompareISODate(int y1, int m1, int d1, int y2, int m2, int d2)
    {
        if (y1 != y2) return y1 < y2 ? -1 : 1;
        if (m1 != m2) return m1 < m2 ? -1 : 1;
        if (d1 != d2) return d1 < d2 ? -1 : 1;
        return 0;
    }

    private static (int y, int m, int d) AddYearsMonths(int year, int month, int day, long years, long months)
    {
        var total = (long)month - 1 + months;
        var newYear = (int)(year + years + FloorDiv(total, 12));
        var newMonth = (int)(((total % 12) + 12) % 12) + 1;
        var newDay = Math.Min(day, DaysInMonthOf(newYear, newMonth));
        return (newYear, newMonth, newDay);
    }

    // ── time-zone offset resolution ───────────────────────────────────────────────

    private static JSTemporalZonedDateTime Require(JSValue value)
        => value as JSTemporalZonedDateTime ?? throw JSEngine.NewTypeError("expected a Temporal.ZonedDateTime");

    private static JSValue ToZonedDateTime(JSValue item)
    {
        if (item is JSTemporalZonedDateTime zdt)
            return new JSTemporalZonedDateTime(zdt.epochNanoseconds, zdt.timeZoneId, zdt.calendarId, ZonedDateTimePrototype);
        if (item.IsString)
            return ParseZonedDateTimeString(item.ToString());
        if (item is JSObject obj)
            return FromPropertyBag(obj, JSUndefined.Value); // default overflow ("constrain")
        throw JSEngine.NewTypeError("Temporal.ZonedDateTime: invalid value");
    }

    // The offset of this zone at the instant `epochNanoseconds`, in nanoseconds.
    private long OffsetNanoseconds() => GetOffsetNanosecondsFor(epochNanoseconds);

    private long GetOffsetNanosecondsFor(BigInteger epochNs)
    {
        if (TryFixedOffset(timeZoneId, out var fixedNs))
            return fixedNs;

        var tz = ResolveNamedZone(timeZoneId);
        if (tz == null) return 0;

        // Convert epoch ns → UTC DateTime (clamped to the representable DateTime range so the
        // offset lookup never throws for extreme years).
        var utc = EpochNsToUtcDateTime(epochNs);
        try { return (long)tz.GetUtcOffset(utc).Ticks * 100; }
        catch { return 0; }
    }

    // The offset to apply to a *local* wall-clock datetime (used when building from fields).
    private static long GetOffsetForLocal(string timeZoneId, int y, int mo, int d, int h, int mi, int s)
    {
        if (TryFixedOffset(timeZoneId, out var fixedNs))
            return fixedNs;

        var tz = ResolveNamedZone(timeZoneId);
        if (tz == null) return 0;

        try
        {
            var clampedYear = Math.Clamp(y, 1, 9999);
            var local = new DateTime(clampedYear, mo, d, h, mi, s, DateTimeKind.Unspecified);
            return (long)tz.GetUtcOffset(local).Ticks * 100;
        }
        catch { return 0; }
    }

    // GetPossibleEpochNanoseconds, expressed as the distinct UTC offsets (ns) the zone yields for a
    // local wall-clock time: two around a fall-back transition, one normally, none inside a
    // spring-forward gap. Used by InterpretISODateTimeOffset to validate an explicit offset.
    private static List<long> CandidateOffsetsForLocal(string timeZoneId, int y, int mo, int d, int h, int mi, int s)
    {
        if (TryFixedOffset(timeZoneId, out var fixedNs))
            return [fixedNs];

        var tz = ResolveNamedZone(timeZoneId);
        if (tz == null) return [0];

        DateTime local;
        try
        {
            var clampedYear = Math.Clamp(y, 1, 9999);
            local = new DateTime(clampedYear, mo, d, h, mi, s, DateTimeKind.Unspecified);
        }
        catch { return [0]; }

        if (tz.IsAmbiguousTime(local))
        {
            var result = new List<long>();
            foreach (var off in tz.GetAmbiguousTimeOffsets(local))
                result.Add((long)off.Ticks * 100);
            return result;
        }

        if (tz.IsInvalidTime(local))
            return []; // a spring-forward gap: no instant has this local time

        return [(long)tz.GetUtcOffset(local).Ticks * 100];
    }

    private (int y, int mo, int d, int h, int mi, int s, int ms, int us, int ns) Local()
    {
        var local = epochNanoseconds + GetOffsetNanosecondsFor(epochNanoseconds);
        var totalSeconds = FloorDiv(local, 1_000_000_000);
        var fraction = (long)(local - totalSeconds * 1_000_000_000);

        var days = (long)FloorDiv(totalSeconds, 86400);
        var secondsOfDay = (long)(totalSeconds - new BigInteger(days) * 86400);
        var (y, m, d) = CivilFromDays(days);

        var h = (int)(secondsOfDay / 3600);
        var mi = (int)(secondsOfDay % 3600 / 60);
        var s = (int)(secondsOfDay % 60);
        var ms = (int)(fraction / 1_000_000);
        var us = (int)(fraction / 1000 % 1000);
        var ns = (int)(fraction % 1000);
        return ((int)y, (int)m, (int)d, h, mi, s, ms, us, ns);
    }

    private BigInteger StartOfDayEpochNs(int y, int mo, int d)
    {
        var localMidnightNs = new BigInteger(DaysFromCivil(y, mo, d)) * NanosecondsPerDay;
        var offset = GetOffsetForLocal(timeZoneId, y, mo, d, 0, 0, 0);
        return localMidnightNs - offset;
    }

    private static DateTime EpochNsToUtcDateTime(BigInteger epochNs)
    {
        // Unix epoch is 621355968000000000 ticks; 1 tick = 100 ns.
        var ticks = (BigInteger)621355968000000000L + epochNs / 100;
        if (ticks < DateTime.MinValue.Ticks) ticks = DateTime.MinValue.Ticks;
        if (ticks > DateTime.MaxValue.Ticks) ticks = DateTime.MaxValue.Ticks;
        return new DateTime((long)ticks, DateTimeKind.Utc);
    }

    private static bool TryFixedOffset(string id, out long offsetNs)
    {
        offsetNs = 0;
        if (string.Equals(id, "UTC", StringComparison.OrdinalIgnoreCase))
            return true;

        var m = OffsetIdPattern.Match(id);
        if (!m.Success) return false;

        var sign = m.Groups[1].Value is "-" or "−" ? -1 : 1;
        var hours = int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
        var minutes = m.Groups[3].Success ? int.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture) : 0;
        var seconds = m.Groups[4].Success ? int.Parse(m.Groups[4].Value, CultureInfo.InvariantCulture) : 0;
        offsetNs = (long)sign * ((long)hours * 3600 + minutes * 60 + seconds) * 1_000_000_000;
        return true;
    }

    private static readonly Regex OffsetIdPattern = new(
        @"^([+-])(\d{2})(?::?(\d{2})(?::?(\d{2}))?)?$", RegexOptions.CultureInvariant);

    private static TimeZoneInfo ResolveNamedZone(string id)
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
        catch { return null; }
    }

    // ToTemporalTimeZoneIdentifier: a time-zone slot value supplied as a String is a bare time-zone
    // identifier (UTC, a numeric offset, or an IANA name) or a full Temporal ISO string, in which
    // case its time-zone designator — a [TimeZone] annotation, a Z (UTC) designator, or a numeric
    // UTC offset — is extracted and canonicalized.
    private static string CanonicalizeTimeZone(string id)
    {
        if (TryCanonicalizeTimeZoneIdentifier(id, out var canonical))
            return canonical;

        if (TemporalIsoString.TryExtractTimeZone(id, out var designator) &&
            TryCanonicalizeTimeZoneIdentifier(designator, out canonical))
            return canonical;

        throw JSEngine.NewRangeError($"Temporal: unknown time zone \"{id}\"");
    }

    // Resolves a *bare* time-zone identifier (no surrounding ISO string): UTC, a numeric UTC offset,
    // or a named IANA zone. Returns false for anything else rather than throwing.
    private static bool TryCanonicalizeTimeZoneIdentifier(string id, out string canonical)
    {
        if (string.Equals(id, "UTC", StringComparison.OrdinalIgnoreCase)) { canonical = "UTC"; return true; }
        if (TryOffsetTimeZoneIdentifier(id, out var offsetNs)) { canonical = FormatOffset(offsetNs); return true; }
        if (ResolveNamedZone(id) != null) { canonical = id; return true; }

        canonical = null;
        return false;
    }

    // A numeric UTC offset used as a *time-zone identifier* must be minute precision (±HH[:MM]) with
    // valid component ranges; a sub-minute (seconds / fractional) offset or an out-of-range component
    // (e.g. the leap-second offset +23:59:60) is not a valid time zone and is rejected by the caller.
    private static readonly Regex OffsetTimeZoneIdentifierPattern = new(
        @"^([+-])(\d{2})(?::?(\d{2}))?$", RegexOptions.CultureInvariant);

    private static bool TryOffsetTimeZoneIdentifier(string id, out long offsetNs)
    {
        offsetNs = 0;
        var m = OffsetTimeZoneIdentifierPattern.Match(id);
        if (!m.Success) return false;
        var hours = int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
        var minutes = m.Groups[3].Success ? int.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture) : 0;
        if (hours > 23 || minutes > 59) return false;
        var sign = m.Groups[1].Value is "-" or "−" ? -1 : 1;
        offsetNs = (long)sign * ((long)hours * 3600 + minutes * 60) * 1_000_000_000;
        return true;
    }

    private static string CanonicalizeCalendar(JSValue calendar)
    {
        if (calendar == null || calendar.IsUndefined) return "iso8601";
        return TemporalCalendar.ToSlotValue(calendar, includeArithmetic: true);
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

    private static int ToIntegerWithTruncation(JSValue value)
    {
        if (value == null || value.IsUndefined) return 0;
        var number = value.DoubleValue;
        if (double.IsNaN(number) || double.IsInfinity(number))
            throw JSEngine.NewRangeError("Temporal.ZonedDateTime: component must be finite");
        return (int)Math.Truncate(number);
    }

    private static int MonthFromCode(string code)
    {
        var match = Regex.Match(code, @"^M(\d{2})$");
        if (!match.Success)
            throw JSEngine.NewRangeError($"Temporal: invalid monthCode \"{code}\"");
        return int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
    }

    // ── building from a property bag ──────────────────────────────────────────────

    private static JSValue FromPropertyBag(JSObject obj, JSValue options)
    {
        var tzValue = obj[KeyStrings.GetOrCreate("timeZone")];
        if (tzValue.IsUndefined)
            throw JSEngine.NewTypeError("Temporal.ZonedDateTime: missing timeZone");
        if (tzValue is JSTemporalZonedDateTime nestedZdt)
            tzValue = new JSString(nestedZdt.timeZoneId);
        if (!tzValue.IsString)
            throw JSEngine.NewTypeError("Temporal.ZonedDateTime: timeZone must be a string");
        var timeZoneId = CanonicalizeTimeZone(tzValue.ToString());

        // Resolve the wall-clock date-time by reusing Temporal.PlainDateTime's property-bag
        // resolution: it reads the same year / era / month(Code) / day / time / calendar fields
        // (ignoring the timeZone / offset ones), applies the overflow option, and handles the
        // non-Gregorian calendars — so an out-of-range month/day is constrained rather than rejected
        // with "invalid ISO date", and a non-ISO calendar resolves correctly.
        var pdt = (JSTemporalPlainDateTime)JSTemporalPlainDateTime.From(new Arguments(JSUndefined.Value, obj, options));

        var localNs = LocalNanoseconds(pdt.isoYear, pdt.isoMonth, pdt.isoDay,
            pdt.hour, pdt.minute, pdt.second, pdt.millisecond, pdt.microsecond, pdt.nanosecond);

        long offsetNs;
        var offsetValue = obj[KeyStrings.GetOrCreate("offset")];
        if (!offsetValue.IsUndefined && offsetValue.IsString && TryParseOffsetString(offsetValue.ToString(), out var explicitOffset))
            offsetNs = explicitOffset;
        else
            offsetNs = GetOffsetForLocal(timeZoneId, pdt.isoYear, pdt.isoMonth, pdt.isoDay, pdt.hour, pdt.minute, pdt.second);

        var epochNs = localNs - offsetNs;
        if (!IsValid(epochNs))
            throw JSEngine.NewRangeError("Temporal.ZonedDateTime: out of range");

        return new JSTemporalZonedDateTime(epochNs, timeZoneId, pdt.calendarId, ZonedDateTimePrototype);
    }

    private static BigInteger LocalNanoseconds(int y, int mo, int d, int h, int mi, int s, int ms, int us, int ns)
        => new BigInteger(DaysFromCivil(y, mo, d)) * NanosecondsPerDay
         + ((((long)h * 60 + mi) * 60 + s) * 1000L + ms) * 1_000_000L + (long)us * 1000 + ns;

    private static bool TryParseOffsetString(string text, out long offsetNs)
    {
        offsetNs = 0;
        if (string.Equals(text, "Z", StringComparison.OrdinalIgnoreCase)) return true;
        var m = OffsetIdPattern.Match(text);
        if (!m.Success) return false;
        var sign = m.Groups[1].Value is "-" or "−" ? -1 : 1;
        var hours = int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
        var minutes = m.Groups[3].Success ? int.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture) : 0;
        var seconds = m.Groups[4].Success ? int.Parse(m.Groups[4].Value, CultureInfo.InvariantCulture) : 0;
        offsetNs = (long)sign * ((long)hours * 3600 + minutes * 60 + seconds) * 1_000_000_000;
        return true;
    }

    // ── parsing the ISO string ────────────────────────────────────────────────────

    private static readonly Regex ZonedPattern = new(
        @"^(\d{4}|\+\d{6}|-(?!000000)\d{6})-(\d{2})-(\d{2})[Tt ](\d{2})(?::?(\d{2})(?::?(\d{2})(?:[.,](\d{1,9}))?)?)?" +
        @"(?:([Zz])|([+-])(\d{2})(?::?(\d{2})(?::?(\d{2}))?)?)?" +
        @"\[(?!u-ca=)([^\]]+)\](?:\[u-ca=([^\]]+)\])?$",
        RegexOptions.CultureInvariant);

    private static JSValue ParseZonedDateTimeString(string text, string offsetOption = "reject")
    {
        var match = ZonedPattern.Match(text);
        if (!match.Success)
            throw JSEngine.NewRangeError($"Cannot parse Temporal.ZonedDateTime from \"{text}\"");

        var calendarId = match.Groups[14].Success ? TemporalCalendar.Canonicalize(match.Groups[14].Value, includeArithmetic: true) : "iso8601";

        var year = int.Parse(match.Groups[1].Value.Replace('−', '-'), CultureInfo.InvariantCulture);
        var month = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
        var day = int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
        var hour = int.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture);
        var minute = match.Groups[5].Success ? int.Parse(match.Groups[5].Value, CultureInfo.InvariantCulture) : 0;
        var second = match.Groups[6].Success ? int.Parse(match.Groups[6].Value, CultureInfo.InvariantCulture) : 0;
        if (second == 60) second = 59; // leap second collapses

        int ms = 0, us = 0, ns = 0;
        if (match.Groups[7].Success)
        {
            var digits = match.Groups[7].Value.PadRight(9, '0');
            ms = int.Parse(digits.Substring(0, 3), CultureInfo.InvariantCulture);
            us = int.Parse(digits.Substring(3, 3), CultureInfo.InvariantCulture);
            ns = int.Parse(digits.Substring(6, 3), CultureInfo.InvariantCulture);
        }

        if (!IsValidISODate(year, month, day) || hour > 23 || minute > 59 || second > 59)
            throw JSEngine.NewRangeError($"Cannot parse Temporal.ZonedDateTime from \"{text}\"");

        // A time-zone annotation may carry the RFC 9557 critical flag "!" (e.g. [!UTC]); strip it
        // before canonicalizing the identifier.
        var timeZoneAnnotation = match.Groups[13].Value;
        if (timeZoneAnnotation.StartsWith("!", StringComparison.Ordinal))
            timeZoneAnnotation = timeZoneAnnotation.Substring(1);
        var timeZoneId = CanonicalizeTimeZone(timeZoneAnnotation);
        var localNs = LocalNanoseconds(year, month, day, hour, minute, second, ms, us, ns);

        long offsetNs;
        var hasZ = match.Groups[8].Success;
        var hasNumericOffset = match.Groups[9].Success;
        if (hasZ) offsetNs = 0;
        else if (hasNumericOffset)
        {
            var sign = match.Groups[9].Value is "-" or "−" ? -1 : 1;
            var oh = int.Parse(match.Groups[10].Value, CultureInfo.InvariantCulture);
            var om = match.Groups[11].Success ? int.Parse(match.Groups[11].Value, CultureInfo.InvariantCulture) : 0;
            var os = match.Groups[12].Success ? int.Parse(match.Groups[12].Value, CultureInfo.InvariantCulture) : 0;
            var explicitOffset = (long)sign * ((long)oh * 3600 + om * 60 + os) * 1_000_000_000;

            // InterpretISODateTimeOffset (offsetBehaviour "option"): the explicit offset is honoured
            // verbatim only for "use"; "ignore" drops it for the zone's own offset; "prefer"/"reject"
            // require it to be one of the zone's offsets for the local time — a mismatch is a
            // RangeError under "reject" (the default for from) and falls back to the zone under "prefer".
            if (offsetOption == "use")
                offsetNs = explicitOffset;
            else if (offsetOption == "ignore")
                offsetNs = GetOffsetForLocal(timeZoneId, year, month, day, hour, minute, second);
            else
            {
                var candidates = CandidateOffsetsForLocal(timeZoneId, year, month, day, hour, minute, second);
                if (candidates.Contains(explicitOffset))
                    offsetNs = explicitOffset;
                else if (offsetOption == "reject")
                    throw JSEngine.NewRangeError($"Temporal.ZonedDateTime: offset does not match the time zone in \"{text}\"");
                else // "prefer": no match — use the zone's own offset
                    offsetNs = GetOffsetForLocal(timeZoneId, year, month, day, hour, minute, second);
            }
        }
        else offsetNs = GetOffsetForLocal(timeZoneId, year, month, day, hour, minute, second);

        var epochNs = localNs - offsetNs;
        if (!IsValid(epochNs))
            throw JSEngine.NewRangeError("Temporal.ZonedDateTime: parsed value is out of range");

        return new JSTemporalZonedDateTime(epochNs, timeZoneId, calendarId, ZonedDateTimePrototype);
    }

    // ── ISO helpers ───────────────────────────────────────────────────────────────

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

    private static BigInteger FloorDiv(BigInteger a, BigInteger b)
    {
        var q = BigInteger.DivRem(a, b, out var r);
        if (r != 0 && (r < 0) != (b < 0)) q -= 1;
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

    private static string FormatOffset(long offsetNs)
    {
        var sign = offsetNs < 0 ? '-' : '+';
        var abs = Math.Abs(offsetNs);
        var totalSeconds = abs / 1_000_000_000;
        var hours = totalSeconds / 3600;
        var minutes = totalSeconds % 3600 / 60;
        var seconds = totalSeconds % 60;
        var sb = new StringBuilder();
        sb.Append(sign).Append(hours.ToString("00", CultureInfo.InvariantCulture))
          .Append(':').Append(minutes.ToString("00", CultureInfo.InvariantCulture));
        if (seconds != 0)
            sb.Append(':').Append(seconds.ToString("00", CultureInfo.InvariantCulture));
        return sb.ToString();
    }

    // YYYY-MM-DDTHH:MM:SS[.fff]±HH:MM[timeZoneId]
    private string ToISOString(string showCalendar = "auto")
    {
        var l = Local();
        var offsetNs = OffsetNanoseconds();

        var sb = new StringBuilder();
        if (l.y < 0 || l.y > 9999)
            sb.Append(l.y < 0 ? '-' : '+').Append(Math.Abs(l.y).ToString("000000", CultureInfo.InvariantCulture));
        else
            sb.Append(l.y.ToString("0000", CultureInfo.InvariantCulture));

        sb.Append('-').Append(l.mo.ToString("00", CultureInfo.InvariantCulture))
          .Append('-').Append(l.d.ToString("00", CultureInfo.InvariantCulture))
          .Append('T').Append(l.h.ToString("00", CultureInfo.InvariantCulture))
          .Append(':').Append(l.mi.ToString("00", CultureInfo.InvariantCulture))
          .Append(':').Append(l.s.ToString("00", CultureInfo.InvariantCulture));

        var fraction = l.ms * 1_000_000 + l.us * 1_000 + l.ns;
        if (fraction != 0)
        {
            var digits = fraction.ToString("000000000", CultureInfo.InvariantCulture).TrimEnd('0');
            sb.Append('.').Append(digits);
        }

        sb.Append(FormatOffset(offsetNs));
        sb.Append('[').Append(timeZoneId).Append(']');
        sb.Append(JSTemporalPlainDate.FormatCalendarAnnotation(calendarId, showCalendar));
        return sb.ToString();
    }

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
}
