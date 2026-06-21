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
        // §6.1.1: the constructor runs ToTemporalTimeZoneIdentifier → ParseTimeZoneIdentifier,
        // which accepts only a bare time-zone identifier (IANA name or minute-precision UTC
        // offset). A full ISO date-time string such as "1997-12-04T12:34[+01:00]" is valid
        // input for from(), but not for the constructor, where it is a RangeError.
        if (!TryCanonicalizeTimeZoneIdentifier(tzArg.ToString(), out var canonicalTimeZone))
            throw JSEngine.NewRangeError($"Temporal.ZonedDateTime: invalid time zone identifier \"{tzArg}\"");
        timeZoneId = canonicalTimeZone;
        calendarId = TemporalCalendar.ResolveCalendarIdentifierArgument(a.GetAt(2), "Temporal.ZonedDateTime");
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
    // PlainDate/PlainDateTime/PlainTime → ZonedDateTime conversions; "compatible" disambiguation by
    // default, or the caller-supplied disambiguation (earlier / later / reject) for an ambiguous or
    // gap wall-clock time.
    internal static JSValue FromLocal(int y, int mo, int d, int h, int mi, int s, int ms, int us, int ns, string timeZone, string calendarId = "iso8601", string disambiguation = "compatible")
    {
        var tz = CanonicalizeTimeZone(timeZone);
        var epochNs = DisambiguateEpochNs(tz, y, mo, d, h, mi, s, ms, us, ns, disambiguation);
        if (!IsValid(epochNs))
            throw JSEngine.NewRangeError("Temporal.ZonedDateTime: out of range");
        return new JSTemporalZonedDateTime(epochNs, tz, calendarId, ZonedDateTimePrototype);
    }

    // DisambiguatePossibleEpochNanoseconds: resolve a local wall-clock time to a single epoch instant.
    // A normal time has one candidate; a fall-back fold has two (earlier = larger offset, later =
    // smaller offset); a spring-forward gap has none. "compatible" matches "later" for a gap and
    // "earlier" for a fold; "reject" throws a RangeError whenever the time is ambiguous or skipped.
    private static BigInteger DisambiguateEpochNs(string tz, int y, int mo, int d, int h, int mi, int s, int ms, int us, int ns, string disambiguation)
    {
        var localNs = LocalNanoseconds(y, mo, d, h, mi, s, ms, us, ns);
        var candidates = CandidateOffsetsForLocal(tz, y, mo, d, h, mi, s);

        if (candidates.Count == 1)
            return localNs - candidates[0];

        if (candidates.Count >= 2)
        {
            if (disambiguation == "reject")
                throw JSEngine.NewRangeError("Temporal: wall-clock time is ambiguous in this time zone");
            // earlier instant = the larger (least-negative) offset; later = the smaller; compatible = earlier.
            var earlier = Math.Max(candidates[0], candidates[1]);
            var later = Math.Min(candidates[0], candidates[1]);
            return localNs - (disambiguation == "later" ? later : earlier);
        }

        // A spring-forward gap: no candidate offset.
        if (disambiguation == "reject")
            throw JSEngine.NewRangeError("Temporal: wall-clock time does not exist in this time zone");

        const long dayNs = 86_400_000_000_000L;
        var offsetBefore = OffsetNanosecondsForInstant(tz, localNs - dayNs);
        var offsetAfter = OffsetNanosecondsForInstant(tz, localNs + dayNs);
        var gap = offsetAfter - offsetBefore;
        // "earlier": shift the wall clock back across the gap (offset before the transition); "later" /
        // "compatible": shift it forward (offset after) — both reduce to localNs minus the chosen offset.
        return disambiguation == "earlier"
            ? localNs - gap - offsetBefore
            : localNs + gap - offsetAfter;
    }

    // GetStartOfDay for a date in a zone (Temporal.PlainDate.prototype.toZonedDateTime with no
    // plainTime): the first instant of the day, which across a gap covering midnight is the transition
    // instant rather than midnight.
    internal static JSValue StartOfDayFor(int y, int mo, int d, string timeZone, string calendarId = "iso8601")
    {
        var tz = CanonicalizeTimeZone(timeZone);
        var epochNs = StartOfDayEpochNs(tz, y, mo, d);
        if (!IsValid(epochNs))
            throw JSEngine.NewRangeError("Temporal.ZonedDateTime: start of day is out of range");
        return new JSTemporalZonedDateTime(epochNs, tz, calendarId, ZonedDateTimePrototype);
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
    // Only the ISO calendar defines a week-numbering system; other calendars return undefined.
    [JSExport("weekOfYear")] public JSValue WeekOfYear
        { get { if (calendarId != "iso8601") return JSUndefined.Value; var l = Local(); return new JSNumber(IsoWeek(l.y, l.mo, l.d).week); } }
    [JSExport("yearOfWeek")] public JSValue YearOfWeek
        { get { if (calendarId != "iso8601") return JSUndefined.Value; var l = Local(); return new JSNumber(IsoWeek(l.y, l.mo, l.d).year); } }

    [JSExport("offsetNanoseconds")] public double OffsetNanosecondsValue => OffsetNanoseconds();
    [JSExport("offset")] public JSValue Offset => new JSString(FormatOffset(OffsetNanoseconds()));

    [JSExport("hoursInDay")]
    public double HoursInDay
    {
        get
        {
            var l = Local();
            var startEpoch = DaysFromCivil(l.y, l.mo, l.d);
            var todayStart = StartOfDayEpochNs(timeZoneId, l.y, l.mo, l.d);
            var (ty, tm, td) = CivilFromDays(startEpoch + 1);
            var tomorrowStart = StartOfDayEpochNs(timeZoneId, (int)ty, (int)tm, (int)td);
            // GetStartOfDay for either boundary may fall outside the representable instant range at the
            // extreme epoch limits (a RangeError, not a saturated value).
            if (!IsValid(todayStart) || !IsValid(tomorrowStart))
                throw JSEngine.NewRangeError("Temporal.ZonedDateTime.prototype.hoursInDay: start of day is out of range");
            return (double)(tomorrowStart - todayStart) / 3_600_000_000_000d;
        }
    }

    // ── statics ─────────────────────────────────────────────────────────────────

    [JSExport("from", Length = 1)]
    internal static JSValue From(in Arguments a)
    {
        var item = a.GetAt(0);
        var options = a.GetAt(1);

        // The options (disambiguation, offset, overflow) are read in alphabetical order
        // and only after the input has been read/parsed: an invalid string is a RangeError
        // *before* any option is read (test262 ZonedDateTime/from
        // observable-get-overflow-argument-string-invalid), and a property bag's fields are
        // read first (order-of-operations). The object/string branches read the options at
        // the right point; for a ZonedDateTime input they are read (and validated) here.
        if (item is JSTemporalZonedDateTime zdt)
        {
            ReadDisambiguation(options);
            ReadOffsetOption(options, "reject");
            ReadOverflow(options);
            return new JSTemporalZonedDateTime(zdt.epochNanoseconds, zdt.timeZoneId, zdt.calendarId, ZonedDateTimePrototype);
        }

        if (item.IsString)
            return ParseZonedDateTimeString(item.ToString(), options);

        if (item is not JSObject obj)
            throw JSEngine.NewTypeError("Temporal.ZonedDateTime.from: invalid value");

        return FromPropertyBag(obj, options);
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
        return epochNanoseconds == other.epochNanoseconds && TimeZoneEquals(timeZoneId, other.timeZoneId)
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
        return new JSTemporalZonedDateTime(ValidStartOfDayEpochNs(l.y, l.mo, l.d), timeZoneId, calendarId, ZonedDateTimePrototype);
    }

    // GetStartOfDay for the local date, validating that the resulting instant is representable —
    // near the instant-range boundary midnight in this zone can fall outside the valid limits.
    private BigInteger ValidStartOfDayEpochNs(int y, int mo, int d)
    {
        var epoch = StartOfDayEpochNs(timeZoneId, y, mo, d);
        if (!IsValid(epoch))
            throw JSEngine.NewRangeError("Temporal.ZonedDateTime: start of day is outside the representable range");
        return epoch;
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
            return new JSTemporalZonedDateTime(ValidStartOfDayEpochNs(l.y, l.mo, l.d), timeZoneId, calendarId, ZonedDateTimePrototype);

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

    // The first offset transition strictly after (forward) or before (!forward) startNs, read directly
    // from the zone's bundled transition table at full (whole-second) precision, or null when the zone
    // has no such transition (e.g. an instant before its first recorded transition).
    private static BigInteger? FindTransition(Tz.IanaTimeZone tz, BigInteger startNs, bool forward)
    {
        var transition = tz.FindTransition(startNs, forward);
        return transition.HasValue && IsValid(transition.Value) ? transition : null;
    }

    [JSExport("toInstant", Length = 0)]
    public JSValue ToInstant(in Arguments a)
        => new JSTemporalInstant(epochNanoseconds, JSTemporalInstant.InstantPrototype);

    [JSExport("toPlainDate", Length = 0)]
    public JSValue ToPlainDate(in Arguments a) => ToPlainDateFromSlots();

    // Projects this ZonedDateTime onto a Temporal.PlainDate purely from its internal slots
    // ([[EpochNanoseconds]], [[TimeZone]], [[Calendar]]) — GetISODateTimeFor + CreateTemporalDate —
    // without reading any observable property. Used by ToTemporalDate so e.g. Temporal.PlainDate.from
    // / compare / equals / since / until consume a ZonedDateTime via its slots (no getter calls).
    internal JSTemporalPlainDate ToPlainDateFromSlots()
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

        // TemporalZonedDateTimeToString reads the options in alphabetical order —
        // calendarName, fractionalSecondDigits, offset, roundingMode, smallestUnit,
        // timeZoneName — coercing each as it is read. All options are read before any
        // algorithmic validation (the smallestUnit ≠ "hour" check) runs (test262
        // ZonedDateTime/prototype/toString order-of-operations and
        // options-read-before-algorithmic-validation).
        var showCalendar = ReadCalendarName(options);
        var digits = -1; // "auto"
        var roundingMode = "trunc";
        string smallestUnit = null;
        var showOffset = true;
        var timeZoneNameMode = "auto";

        if (options is JSObject o)
        {
            digits = TemporalRoundingOptions.GetFractionalSecondDigits(o);

            var offset = o[KeyStrings.GetOrCreate("offset")];
            if (!offset.IsUndefined)
                showOffset = offset.StringValue switch
                {
                    "auto" => true,
                    "never" => false,
                    _ => throw JSEngine.NewRangeError($"Temporal: invalid offset display option \"{offset.StringValue}\""),
                };

            roundingMode = TemporalRoundingOptions.GetRoundingMode(o, "trunc");

            // Coerce smallestUnit to a string now (firing its toString); defer the
            // time-unit validation until after timeZoneName is read, so a date smallestUnit
            // is rejected only once every option getter has fired.
            var su = o[KeyStrings.GetOrCreate("smallestUnit")];
            var smallestUnitRaw = su.IsUndefined ? null : su.StringValue;

            var timeZoneName = o[KeyStrings.GetOrCreate("timeZoneName")];
            if (!timeZoneName.IsUndefined)
            {
                timeZoneNameMode = timeZoneName.StringValue;
                if (timeZoneNameMode is not ("auto" or "never" or "critical"))
                    throw JSEngine.NewRangeError($"Temporal: invalid timeZoneName display option \"{timeZoneNameMode}\"");
            }

            // Algorithmic validation runs only after every option has been read.
            if (smallestUnitRaw != null)
                smallestUnit = TemporalRoundingOptions.NormalizeTimeUnit(smallestUnitRaw, allowAuto: false);

            if (smallestUnit is "hour")
                throw JSEngine.NewRangeError("Temporal.ZonedDateTime.toString: smallestUnit cannot be \"hour\"");
        }

        // ToSecondsStringPrecisionRecord: precision -2 = minutes (no seconds), -1 = auto,
        // 0..9 = a fixed number of fractional-second digits; incrementNs is the rounding step.
        int precision;
        long incrementNs;
        if (smallestUnit != null)
            (precision, incrementNs) = smallestUnit switch
            {
                "minute" => (-2, 60_000_000_000L),
                "second" => (0, 1_000_000_000L),
                "millisecond" => (3, 1_000_000L),
                "microsecond" => (6, 1_000L),
                _ => (9, 1L), // nanosecond
            };
        else if (digits == -1) { precision = -1; incrementNs = 1; }
        else { precision = digits; incrementNs = TemporalRoundingOptions.Pow10(9 - digits); }

        // Round the instant to the requested precision, then resolve the wall clock / offset from the
        // rounded instant.
        var rounded = TemporalRoundingOptions.RoundToIncrement(epochNanoseconds, incrementNs, roundingMode);
        if (!IsValid(rounded))
            throw JSEngine.NewRangeError("Temporal.ZonedDateTime.toString: result is out of range");

        return new JSString(FormatToString(rounded, showCalendar, precision, showOffset, timeZoneNameMode));
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
            // GetRoundingIncrementOption, then GetRoundingModeOption, then the REQUIRED
            // smallestUnit are read (and coerced) in that order, and all of them BEFORE any
            // algorithmic validation of the increment — so an invalid roundingIncrement for
            // the smallestUnit still observes every option getter first (test262
            // round/options-read-before-algorithmic-validation).
            increment = TemporalRoundingOptions.GetRoundingIncrement(obj);
            roundingMode = TemporalRoundingOptions.GetRoundingMode(obj, "halfExpand");
            var unitValue = obj[KeyStrings.GetOrCreate("smallestUnit")];
            if (unitValue.IsUndefined)
                throw JSEngine.NewRangeError("Temporal.ZonedDateTime.round requires a smallestUnit");
            smallestUnit = unitValue.StringValue;
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

        // ValidateTemporalRoundingIncrement: a "day" increment must be exactly 1 (maximum 1,
        // inclusive); a time-unit increment must divide its next unit (24/60/1000, exclusive).
        TemporalRoundingOptions.ValidateRoundingIncrement(
            increment,
            smallestUnit == "day" ? 1 : TemporalRoundingOptions.MaximumRoundingIncrement(smallestUnit),
            inclusive: smallestUnit == "day");

        var l = Local();
        var dayStart = StartOfDayEpochNs(timeZoneId, l.y, l.mo, l.d);

        if (smallestUnit == "day")
        {
            // Round to a day boundary using the actual (DST-aware) length of the local day. Both the
            // day's start and the next day's start (GetStartOfDay) must be representable instants.
            if (!IsValid(dayStart)) throw JSEngine.NewRangeError("Temporal.ZonedDateTime: start of day is out of range");
            var (ty, tm, td) = CivilFromDays(DaysFromCivil(l.y, l.mo, l.d) + 1);
            var tomorrowStart = StartOfDayEpochNs(timeZoneId, (int)ty, (int)tm, (int)td);
            if (!IsValid(tomorrowStart)) throw JSEngine.NewRangeError("Temporal.ZonedDateTime: start of day is out of range");
            var dayLengthNs = tomorrowStart - dayStart;
            var rounded = TemporalRoundingOptions.RoundToIncrement(epochNanoseconds - dayStart, dayLengthNs, roundingMode);
            var epoch = dayStart + rounded;
            if (!IsValid(epoch)) throw JSEngine.NewRangeError("Temporal.ZonedDateTime: out of range");
            return new JSTemporalZonedDateTime(epoch, timeZoneId, calendarId, ZonedDateTimePrototype);
        }


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

        // PrepareCalendarFields reads and validates the offset field before the date-time fields are
        // interpreted, so parse it up front: an absent offset keeps the current one, a present
        // non-string offset is a TypeError (ToOffsetString requires a String after ToPrimitive), and
        // an unparseable string such as "00:00" / "+0" is a RangeError — independent of whether the
        // remaining partial fields form a recognized date-time set.
        long candidateOffset;
        var offsetField = fields[KeyStrings.GetOrCreate("offset")];
        if (offsetField.IsUndefined)
        {
            candidateOffset = OffsetNanoseconds();
        }
        else
        {
            var offsetPrimitive = offsetField is JSObject offsetObj ? offsetObj.ToStringPrimitive() : offsetField;
            if (!offsetPrimitive.IsString)
                throw JSEngine.NewTypeError("Temporal.ZonedDateTime.prototype.with: the offset field must be a string");
            if (!TryParseOffsetString(offsetPrimitive.StringValue, out candidateOffset))
                throw JSEngine.NewRangeError("Temporal.ZonedDateTime.prototype.with: invalid offset");
        }

        // Read the with() options — disambiguation, then offset — before interpreting the
        // fields, so every option getter has fired before any field validation can throw
        // (overflow is read third, inside PlainDateTime.With below). test262
        // ZonedDateTime/prototype/with options-read-before-algorithmic-validation requires
        // that e.g. an invalid monthCode is rejected only after all options are read.
        //
        // A non-object (and non-undefined) options bag is a TypeError, but the partial
        // date-time fields are processed first (PlainDateTime.With raises it via ReadOverflow
        // below), so skip the getter reads here and let that path throw — keeping
        // `with({ day: -1 }, primitive)` a RangeError (options-wrong-type).
        var offsetOption = "prefer";
        if (options is JSObject || options == null || options.IsUndefined)
        {
            ReadDisambiguation(options); // validated; only "compatible" behaviour is applied below
            offsetOption = ReadOffsetOption(options);
        }

        // Merge the date/time fields onto the current local wall-clock datetime, reusing the
        // calendar-aware PlainDateTime.with (which rejects calendar/timeZone fields, applies the
        // overflow option and validates era / monthCode / partial era pairs).
        var l = Local();
        var current = new JSTemporalPlainDateTime(l.y, l.mo, l.d, l.h, l.mi, l.s, l.ms, l.us, l.ns, calendarId,
            JSTemporalPlainDateTime.PlainDateTimePrototype);

        // For ZonedDateTime the field list also includes `offset`, so a bag carrying only an
        // offset (already validated above) satisfies the "at least one field" requirement and
        // leaves the wall-clock date-time unchanged. PlainDateTime.with counts only date-time
        // fields, so delegating an offset-only bag would wrongly raise a TypeError instead of
        // letting the offset interpretation below run (and, here, throw a RangeError when the
        // result is out of range). The calendar/timeZone-field rejection and the overflow
        // option that PlainDateTime.with performs are reproduced for that case.
        JSTemporalPlainDateTime updated;
        if (HasAnyDateTimeField(fields))
        {
            updated = (JSTemporalPlainDateTime)current.With(new Arguments(JSUndefined.Value, fields, options));
        }
        else
        {
            if (!fields[KeyStrings.GetOrCreate("calendar")].IsUndefined)
                throw JSEngine.NewTypeError("Temporal.ZonedDateTime.prototype.with does not accept a calendar field");
            if (!fields[KeyStrings.GetOrCreate("timeZone")].IsUndefined)
                throw JSEngine.NewTypeError("Temporal.ZonedDateTime.prototype.with does not accept a timeZone field");
            if (offsetField.IsUndefined)
                throw JSEngine.NewTypeError("Temporal.ZonedDateTime.prototype.with requires at least one field");
            ReadOverflow(options);
            updated = current;
        }

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
            int ry, rm, rd;
            if (NonIso)
            {
                // The wall-clock date arithmetic runs in the calendar's own space (year/month
                // arithmetic honouring leap months and variable month lengths), mirroring
                // Temporal.PlainDate; the resulting ISO date is then re-anchored to the zone.
                var c = CalendarYmd();
                (ry, rm, rd) = TemporalNonIso.AddToIso(calendarId, c.y, c.m, c.d, years, months, weeks, days, overflow);
            }
            else
            {
                (ry, rm, rd) = AddISODate(l.y, l.mo, l.d, years, months, weeks, days, overflow);
            }
            var intermediateNs = EpochNsForLocal(ry, rm, rd, l.h, l.mi, l.s, l.ms, l.us, l.ns);
            resultNs = intermediateNs + timeNs;
        }

        if (!IsValid(resultNs))
            throw JSEngine.NewRangeError("Temporal.ZonedDateTime: result is out of range");
        return new JSTemporalZonedDateTime(resultNs, timeZoneId, calendarId, ZonedDateTimePrototype);
    }

    // Each component is converted via BigInteger (not (long)) because a near-maximum duration's
    // nanoseconds (~9e24) overflows Int64 before the multiply.
    private static BigInteger DurationTimeNanoseconds(JSTemporalDuration d)
        => new BigInteger(d.HoursValue) * 3_600_000_000_000
         + new BigInteger(d.MinutesValue) * 60_000_000_000
         + new BigInteger(d.SecondsValue) * 1_000_000_000
         + new BigInteger(d.MillisecondsValue) * 1_000_000
         + new BigInteger(d.MicrosecondsValue) * 1_000
         + new BigInteger(d.NanosecondsValue);

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
    // epoch-nanosecond difference, rounded to smallestUnit × roundingIncrement and balanced into
    // time components; otherwise it is the DST-aware calendar difference (a "day" may be 23 h or
    // 25 h across a transition), to which the rounding options are not yet applied.
    private JSValue Difference(JSValue otherValue, JSValue options, int sign)
    {
        var other = Require(ToZonedDateTime(otherValue));
        if (calendarId != other.calendarId)
            throw JSEngine.NewRangeError("Temporal.ZonedDateTime: cannot compute the difference between date-times of different calendars");
        var (largestUnit, smallestUnit, increment, roundingMode) = ReadZonedDifferenceSettings(options);

        // A calendar-unit largestUnit (year/month/week/day) differences the dates in the receiver's
        // time zone, which is only meaningful when both operands share that zone; the IANA identifiers
        // are compared canonically (so e.g. "Asia/Calcutta" and "Asia/Kolkata" are the same zone).
        if (!IsTimeUnit(largestUnit) && !TimeZoneEquals(timeZoneId, other.timeZoneId))
            throw JSEngine.NewRangeError("Temporal.ZonedDateTime: cannot compute a calendar-unit difference between date-times in different time zones");

        // `since` is `-(until)`, so it rounds the (this→other) difference with the negated mode and
        // negates the result below — matching the PlainDate/PlainDateTime difference.
        var diffMode = sign < 0 ? TemporalRoundingOptions.NegateRoundingMode(roundingMode) : roundingMode;
        var result = IsTimeUnit(largestUnit)
            ? DifferenceTimeOnly(epochNanoseconds, other.epochNanoseconds, largestUnit, smallestUnit, increment, diffMode)
            : DifferenceCalendar(epochNanoseconds, other.epochNanoseconds, largestUnit, smallestUnit, increment, diffMode);

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

    // The difference as a pure time duration (no calendar units): the (ns2 − ns1) nanosecond
    // difference rounded to smallestUnit × increment (with the caller's — for "since", negated —
    // rounding mode), then balanced from `largestUnit` down.
    private static JSTemporalDuration DifferenceTimeOnly(BigInteger ns1, BigInteger ns2, string largestUnit,
        string smallestUnit, int increment, string roundingMode)
    {
        var unitNs = (BigInteger)TimeUnitNanoseconds(smallestUnit) * increment;
        var rounded = TemporalRoundingOptions.RoundToIncrement(ns2 - ns1, unitNs, roundingMode);
        return BalanceTimeDuration(rounded, largestUnit);
    }

    private static long TimeUnitNanoseconds(string unit) => unit switch
    {
        "hour" => 3_600_000_000_000,
        "minute" => 60_000_000_000,
        "second" => 1_000_000_000,
        "millisecond" => 1_000_000,
        "microsecond" => 1_000,
        _ => 1, // nanosecond
    };

    // DifferenceZonedDateTime for a calendar `largestUnit` (year/month/week/day). Follows the
    // proposal's day-correction loop: the wall-clock time-of-day difference is combined with a
    // calendar date difference, correcting the intermediate date until the residual real time
    // runs in the same direction as the overall difference (which absorbs DST offset shifts).
    private JSTemporalDuration DifferenceCalendar(BigInteger ns1, BigInteger ns2, string largestUnit,
        string smallestUnit = "nanosecond", int increment = 1, string roundingMode = "trunc")
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

        // The date portion is differenced in the instance's calendar: the ISO start / intermediate
        // dates are projected to the calendar's (year, month, day) so a non-ISO calendar counts whole
        // years/months by its own month lengths and leap months, not the ISO ones.
        double years, months, weeks, days;
        if (TemporalCalendarMath.IsNonIso(calendarId))
        {
            var s = TemporalNonIso.CalendarYmd(calendarId, start.y, start.mo, start.d);
            var e = TemporalNonIso.CalendarYmd(calendarId, iy, im, id);
            (years, months, weeks, days) = TemporalNonIso.Difference(calendarId, s.y, s.m, s.d, e.y, e.m, e.d, largestUnit);
        }
        else
        {
            (years, months, weeks, days) = DifferenceISODate(start.y, start.mo, start.d, iy, im, id, largestUnit);

            // RoundRelativeDuration (calendar smallestUnit): round the date difference toward the
            // requested unit, with the time-of-day folded into the boundary progress. This reuses the
            // PlainDateTime day-axis nudge, which is exact when the day is 24 h (UTC / fixed-offset
            // zones); a DST day length is not yet modelled here. Range-check the rounded boundary so an
            // increment that pushes the end date past the representable range is a RangeError.
            if (smallestUnit is "year" or "month" or "week" or "day")
            {
                if (smallestUnit == "day")
                {
                    // The nudge consults the end boundary (anchor + the next day-increment multiple)
                    // only when the difference does not land exactly on it; building that boundary as a
                    // date past the representable range (±10^8 days from the epoch) is a RangeError, per
                    // AddDateTime in NudgeToCalendarUnit.
                    var exact = residual == 0 && years == 0 && months == 0 && weeks == 0
                        && (long)days % increment == 0;
                    if (!exact)
                    {
                        var r1Days = JSTemporalPlainDate.AddDateGetDays(start.y, start.mo, start.d,
                            (long)years, (long)months, (long)weeks, (long)days);
                        var boundaryDays = r1Days + (long)increment * sign;
                        if (boundaryDays is < -100_000_000 or > 100_000_000)
                            throw JSEngine.NewRangeError("Temporal.ZonedDateTime: rounding result is out of range");
                    }
                }

                var (ry, rmo, rw, rd) = JSTemporalPlainDate.RoundDateTimeDateDifference(
                    start.y, start.mo, start.d, (long)years, (long)months, (long)weeks, (long)days,
                    end.y, end.mo, end.d, TimeOfDayNs(start), TimeOfDayNs(end),
                    largestUnit, smallestUnit, increment, roundingMode);
                return new JSTemporalDuration(ry, rmo, rw, rd, 0, 0, 0, 0, 0, 0, JSTemporalDuration.DurationPrototype);
            }
        }
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

        // ℝ→𝔽 nearest double (ties to even), not .NET's truncating (double)BigInteger
        // (#818 Problems 18/19).
        double S(BigInteger v) => sign * JSTemporalDuration.NearestDouble(v);
        return new JSTemporalDuration(0, 0, 0, 0, S(hours), S(minutes), S(seconds), S(millis), S(micros), S(nanos),
            JSTemporalDuration.DurationPrototype);
    }

    private static readonly string[] UnitRankOrder =
        { "year", "month", "week", "day", "hour", "minute", "second", "millisecond", "microsecond", "nanosecond" };

    private static int UnitRank(string unit) => System.Array.IndexOf(UnitRankOrder, unit);

    // GetDifferenceSettings for a ZonedDateTime since/until: reads largestUnit, roundingIncrement,
    // roundingMode and smallestUnit (in that spec order, every unit coerced before validation), then
    // validates that largestUnit is not finer than smallestUnit and that a sub-day smallestUnit's
    // increment divides its unit evenly. The defaults are smallestUnit "nanosecond" and largestUnit
    // = the larger of "hour" and smallestUnit. (The rounding the options request is validated here
    // but, like the PlainDate/PlainDateTime difference, not yet applied to the result.)
    private static (string largestUnit, string smallestUnit, int increment, string roundingMode) ReadZonedDifferenceSettings(JSValue options)
    {
        if (options == null || options.IsUndefined)
            return ("hour", "nanosecond", 1, "trunc");
        if (options is not JSObject o)
            throw JSEngine.NewTypeError("Temporal.ZonedDateTime difference options must be an object or undefined");

        var largestRaw = o[KeyStrings.GetOrCreate("largestUnit")];
        var largestUnit = largestRaw.IsUndefined ? null : TemporalRoundingOptions.NormalizeAnyUnit(largestRaw.StringValue, allowAuto: true);
        if (largestUnit == "auto") largestUnit = null;

        var increment = TemporalRoundingOptions.GetRoundingIncrement(o);
        var roundingMode = TemporalRoundingOptions.GetRoundingMode(o, "trunc");

        var smallestRaw = o[KeyStrings.GetOrCreate("smallestUnit")];
        var smallestUnit = smallestRaw.IsUndefined ? "nanosecond" : TemporalRoundingOptions.NormalizeAnyUnit(smallestRaw.StringValue, allowAuto: false);

        var hourIndex = TemporalRoundingOptions.UnitIndex("hour");
        largestUnit ??= TemporalRoundingOptions.UnitIndex(smallestUnit) < hourIndex ? smallestUnit : "hour";

        if (TemporalRoundingOptions.UnitIndex(smallestUnit) < TemporalRoundingOptions.UnitIndex(largestUnit))
            throw JSEngine.NewRangeError("Temporal.ZonedDateTime: smallestUnit must not be larger than largestUnit");

        var max = TemporalRoundingOptions.MaximumRoundingIncrement(smallestUnit);
        if (max > 0)
            TemporalRoundingOptions.ValidateRoundingIncrement(increment, max, inclusive: false);

        return (largestUnit, smallestUnit, increment, roundingMode);
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
        var step = -sign; // +1 when the end is after the start

        // Each candidate year/month offset is measured from the *original* start date, keeping its
        // *unconstrained* day, not by stepping a constrained intermediate forward. This both avoids
        // stranding the day at a short month in between (e.g. Dec 30 + N months landing on Feb 28 and
        // over-counting the residual days) and makes a wrap such as Jan 29 + 1 month = Feb 29 surpass
        // Feb 28, so it is not counted as a whole month. (Mirrors PlainDate's ISODateSurpasses.)
        bool Surpasses(long yy, long mm)
        {
            var total = (long)am - 1 + mm;
            var ny = ay + yy + FloorDiv(total, 12);
            var nm = (int)(((total % 12) + 12) % 12) + 1;
            if (ny != by) return step * (ny - by) > 0;
            if (nm != bm) return step * (nm - bm) > 0;
            return step * (ad - bd) > 0;
        }

        long years = 0;
        var candidateYears = (long)by - ay;
        if (candidateYears != 0) candidateYears -= step;
        while (!Surpasses(candidateYears, 0)) { years = candidateYears; candidateYears += step; }

        long months = 0;
        var candidateMonths = (long)step;
        while (!Surpasses(years, candidateMonths)) { months = candidateMonths; candidateMonths += step; }

        if (largestUnit == "month") { months += years * 12; years = 0; }

        var (iy, im, id) = AddYearsMonths(ay, am, ad, years, months);
        var days = DaysFromCivil(by, bm, bd) - DaysFromCivil(iy, im, id);
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

    private long GetOffsetNanosecondsFor(BigInteger epochNs) => OffsetNanosecondsForInstant(timeZoneId, epochNs);

    // GetOffsetNanosecondsFor(timeZone, instant) as a static helper (used by Temporal.Instant.toString):
    // the UTC offset, in nanoseconds, that the given zone applies at the instant.
    internal static long OffsetNanosecondsForInstant(string timeZoneId, BigInteger epochNs)
    {
        if (TryFixedOffset(timeZoneId, out var fixedNs))
            return fixedNs;

        var tz = ResolveNamedZone(timeZoneId);
        if (tz == null) return 0;

        // The bundled table records offsets at full IANA precision (seconds), so historical sub-minute
        // offsets such as Africa/Monrovia's -00:44:30 are exact without any post-correction.
        return tz.GetOffsetNanoseconds(epochNs);
    }

    // ToTemporalTimeZoneIdentifier for a slot value supplied as a JSValue: a ZonedDateTime contributes
    // its own zone; a String is a bare identifier or an ISO string carrying a time-zone designator;
    // anything else is a TypeError and an unrecognized identifier a RangeError.
    internal static string ToTimeZoneIdentifier(JSValue value)
    {
        if (value is JSTemporalZonedDateTime zdt) return zdt.timeZoneId;
        if (value == null || !value.IsString)
            throw JSEngine.NewTypeError("Temporal: time zone must be a string identifier");
        return CanonicalizeTimeZone(value.ToString());
    }

    // Formats a UTC offset (nanoseconds) as ±HH:MM[:SS] for display.
    internal static string FormatOffsetString(long offsetNs) => FormatOffset(offsetNs);

    // The offset to apply to a *local* wall-clock datetime (used when building from fields).
    private static long GetOffsetForLocal(string timeZoneId, int y, int mo, int d, int h, int mi, int s)
    {
        if (TryFixedOffset(timeZoneId, out var fixedNs))
            return fixedNs;

        if (ResolveNamedZone(timeZoneId) == null) return 0;

        // Default ("compatible") disambiguation. A fall-back fold yields two candidate offsets: choose
        // the EARLIER instant, i.e. the largest (least negative) offset (epoch = localNs - offset). An
        // unambiguous wall time has a single candidate. A spring-forward gap yields no candidate; per
        // "compatible" the later instant is taken — interpret the wall clock with the pre-transition
        // offset (a day earlier), which lands just past the gap.
        var candidates = CandidateOffsetsForLocal(timeZoneId, y, mo, d, h, mi, s);
        if (candidates.Count >= 2)
        {
            var earliest = candidates[0];
            for (var i = 1; i < candidates.Count; i++)
                if (candidates[i] > earliest)
                    earliest = candidates[i];
            return earliest;
        }
        if (candidates.Count == 1)
            return candidates[0];

        const long dayNs = 86_400_000_000_000L;
        var localNs = LocalNanoseconds(y, mo, d, h, mi, s, 0, 0, 0);
        return OffsetNanosecondsForInstant(timeZoneId, localNs - dayNs);
    }

    // GetPossibleEpochNanoseconds, expressed as the distinct UTC offsets (ns) the zone yields for a
    // local wall-clock time: two around a fall-back transition, one normally, none inside a
    // spring-forward gap. Used by InterpretISODateTimeOffset to validate an explicit offset.
    //
    // Implements the spec's GetNamedTimeZoneEpochNanoseconds: treat the wall clock as a UTC instant,
    // take the zone's offset a day before and a day after, and keep each candidate offset only if the
    // instant it implies actually has that offset (a round-trip). This resolves folds/gaps at full
    // precision using the (sub-minute-corrected) offset function — unlike .NET's IsAmbiguousTime, which
    // is whole-minute and misses e.g. Pacific/Niue's 20-second 1952 fold.
    private static List<long> CandidateOffsetsForLocal(string timeZoneId, int y, int mo, int d, int h, int mi, int s)
    {
        if (TryFixedOffset(timeZoneId, out var fixedNs))
            return [fixedNs];

        if (ResolveNamedZone(timeZoneId) == null)
            return [0];

        var localNs = LocalNanoseconds(y, mo, d, h, mi, s, 0, 0, 0);
        const long dayNs = 86_400_000_000_000L;
        var offsetBefore = OffsetNanosecondsForInstant(timeZoneId, localNs - dayNs);
        var offsetAfter = OffsetNanosecondsForInstant(timeZoneId, localNs + dayNs);

        var result = new List<long>(2);
        void TryAdd(long candidateOffset)
        {
            if (result.Contains(candidateOffset)) return;
            // The candidate offset is valid for this wall clock only when the instant it implies
            // actually reports it back (so a gap yields none, a fold both).
            if (OffsetNanosecondsForInstant(timeZoneId, localNs - candidateOffset) == candidateOffset)
                result.Add(candidateOffset);
        }
        TryAdd(offsetBefore);
        TryAdd(offsetAfter);
        return result;
    }

    // ToRelativeTemporalObject for a property-bag relativeTo that carries a timeZone: the offset field,
    // when present, must EXACTLY match one of the zone's offsets for the given local wall clock — a
    // property bag uses match-exactly, with no minute rounding (unlike the string form, where
    // ParseZonedDateTimeString allows the match-minutes fallback). A mismatch is a RangeError, so e.g.
    // { offset: "-00:45", timeZone: "Africa/Monrovia" } is rejected (the zone offset is -00:44:30).
    internal static void ValidateBagOffsetMatchesZone(string timeZoneId, int y, int mo, int d, int h, int mi, int s, string offsetString)
    {
        if (!TryParseOffsetString(offsetString, out var offsetNs))
            throw JSEngine.NewRangeError($"Temporal: invalid offset string \"{offsetString}\"");
        if (!CandidateOffsetsForLocal(timeZoneId, y, mo, d, h, mi, s).Contains(offsetNs))
            throw JSEngine.NewRangeError($"Temporal: offset \"{offsetString}\" does not match the time zone \"{timeZoneId}\"");
    }

    // InterpretISODateTimeOffset offset-matching for the "prefer"/"reject" options: does the explicit
    // offset parsed from the string match one of the zone's candidate offsets for the local time?
    // A minute-precision offset string (no sub-minute component) uses the match-minutes behaviour —
    // a candidate matches when it equals the explicit offset after being rounded to the nearest minute
    // (half away from zero) — so Africa/Monrovia's -00:44:30 offset is matched by the string "-00:45".
    // The matched candidate (the real instant's offset), not the rounded value, is returned.
    private static bool TryMatchZoneOffset(List<long> candidates, long explicitOffset, bool matchMinutes, out long matched)
    {
        foreach (var candidate in candidates)
        {
            if (candidate == explicitOffset) { matched = candidate; return true; }
            if (matchMinutes && RoundOffsetToMinute(candidate) == explicitOffset) { matched = candidate; return true; }
        }
        matched = 0;
        return false;
    }

    // RoundNumberToIncrement(offsetNs, 60e9, "halfExpand"): round to the nearest whole minute, with
    // ties rounded away from zero.
    private static long RoundOffsetToMinute(long offsetNs)
    {
        const long minute = 60_000_000_000L;
        var q = offsetNs / minute;
        var r = offsetNs % minute;
        if (Math.Abs(r) * 2 >= minute) q += Math.Sign(offsetNs);
        return q * minute;
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

    // GetStartOfDay: the first instant of the calendar day `y-mo-d` in the zone. Normally this is local
    // midnight; across a spring-forward whose gap covers midnight it is the transition instant (the
    // first existing local time of the day) — which need be neither 00:00 nor 01:00 (e.g.
    // America/Toronto 1919-03-31 sprang forward 23:30→00:30, so the day starts at 00:30).
    private static BigInteger StartOfDayEpochNs(string timeZoneId, int y, int mo, int d)
    {
        var localMidnightNs = new BigInteger(DaysFromCivil(y, mo, d)) * NanosecondsPerDay;

        // The earliest instant whose local time is this midnight: local midnight minus the largest of
        // the zone's candidate offsets (a fall-back fold yields two; pick the earlier instant).
        var candidates = CandidateOffsetsForLocal(timeZoneId, y, mo, d, 0, 0, 0);
        if (candidates.Count > 0)
        {
            var maxOffset = candidates[0];
            foreach (var o in candidates)
                if (o > maxOffset) maxOffset = o;
            return localMidnightNs - maxOffset;
        }

        // Midnight is inside a spring-forward gap: the day starts at the offset transition itself.
        var tz = ResolveNamedZone(timeZoneId);
        if (tz == null) return localMidnightNs;
        var transition = FindTransition(tz, localMidnightNs - NanosecondsPerDay, forward: true);
        return transition ?? localMidnightNs;
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

    // Named IANA zones resolve through the bundled tz database (Temporal/Tz), so offset and
    // transition computation does not depend on the host OS tz data. A link/alias resolves to its
    // target's transition data while keeping its own identifier; an unknown id yields null.
    private static Tz.IanaTimeZone ResolveNamedZone(string id) => Tz.IanaTimeZoneDatabase.Get(id);

    // TimeZoneEquals (used by ZonedDateTime.prototype.equals): two identifiers denote the same zone when
    // they are the same string or resolve to the same primary IANA identifier. IANA keeps backward
    // aliases (e.g. "Asia/Calcutta" for "Asia/Kolkata"); the identifier is preserved on the instance, but
    // equality compares the primary. The bundled tz database resolves an alias/link to its target zone
    // (a numeric offset / UTC is not a link, so it falls back to its already-canonical identifier —
    // these only ever match an identical string).
    private static bool TimeZoneEquals(string a, string b)
        => a == b || PrimaryTimeZoneIdentifier(a) == PrimaryTimeZoneIdentifier(b);

    private static string PrimaryTimeZoneIdentifier(string id) => Tz.IanaTimeZoneDatabase.GetPrimary(id);

    // ToTemporalTimeZoneIdentifier: a time-zone slot value supplied as a String is a bare time-zone
    // identifier (UTC, a numeric offset, or an IANA name) or a full Temporal ISO string, in which
    // case its time-zone designator — a [TimeZone] annotation, a Z (UTC) designator, or a numeric
    // UTC offset — is extracted and canonicalized.
    // Validates a time-zone identifier (the [tz] annotation of a relativeTo string), throwing a
    // RangeError for an unknown zone or a sub-minute offset zone such as "-00:44:30". This only
    // checks the identifier — it computes no instant — so it is safe for a relativeTo string that is
    // otherwise consumed as a (24-hour-day) PlainDate.
    internal static void ValidateTimeZoneIdentifier(string id) => CanonicalizeTimeZone(id);

    // Validates a bare time-zone identifier (UTC / numeric offset / named IANA zone) and returns it
    // case-normalized, preserving an IANA backward alias's own name (e.g. "asia/calcutta" stays
    // "Asia/Calcutta" rather than resolving to "Asia/Kolkata"); an unknown name is a RangeError.
    // Shared with Intl.DateTimeFormat's timeZone option handling.
    internal static string CanonicalizeTimeZoneId(string id) => CanonicalizeTimeZone(id);

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

        // IANA identifiers (zones and backward-alias links alike) match case-insensitively and are
        // returned case-normalized, keeping their OWN name — a link is NOT replaced by its primary
        // zone (e.g. "africa/cairo" → "Africa/Cairo", "asia/ulan_bator" → "Asia/Ulan_Bator"). The
        // bundled tz database is the single source of valid identifiers, so this is host-independent.
        if (Tz.IanaTimeZoneDatabase.TryCanonicalize(id, out canonical))
            return true;

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

    // The date-time field names PrepareCalendarFields recognizes for PlainDateTime.with;
    // `offset` (and `timeZone`) are ZonedDateTime-only and handled separately.
    private static readonly string[] DateTimeFieldNames =
        ["year", "month", "monthCode", "day", "hour", "minute", "second", "millisecond", "microsecond", "nanosecond", "era", "eraYear"];

    private static bool HasAnyDateTimeField(JSObject bag)
    {
        foreach (var name in DateTimeFieldNames)
            if (!bag[KeyStrings.GetOrCreate(name)].IsUndefined)
                return true;
        return false;
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
        // ToTemporalZonedDateTime reads the bag's fields in one alphabetical pass — the same
        // PrepareCalendarFields order Temporal.PlainDateTime uses, with `offset` read between
        // nanosecond and second and `timeZone` between second and year. The two callbacks read those
        // at exactly those points so the property-access order matches the spec (#818 Problem 7).
        string timeZoneId = null;
        long explicitOffset = 0;
        var hasExplicitOffset = false;

        void ReadOffset()
        {
            var offsetValue = obj[KeyStrings.GetOrCreate("offset")];
            if (offsetValue.IsUndefined)
                return;
            // ToPrimitiveAndRequireString: an object offset is coerced with ToPrimitive (string hint,
            // so its toString is observed), then the result must be a String (a non-string is a
            // TypeError) and a valid UTC offset (an unparseable value such as "00:00" / "+0" is a
            // RangeError) — it is not silently ignored.
            var offsetPrimitive = offsetValue is JSObject offsetObj ? offsetObj.ToStringPrimitive() : offsetValue;
            if (!offsetPrimitive.IsString)
                throw JSEngine.NewTypeError("Temporal.ZonedDateTime: the offset field must be a string");
            if (!TryParseOffsetString(offsetPrimitive.StringValue, out explicitOffset))
                throw JSEngine.NewRangeError($"Temporal.ZonedDateTime: invalid offset string \"{offsetPrimitive.StringValue}\"");
            hasExplicitOffset = true;
        }

        void ReadTimeZone()
        {
            var tzValue = obj[KeyStrings.GetOrCreate("timeZone")];
            if (tzValue.IsUndefined)
                throw JSEngine.NewTypeError("Temporal.ZonedDateTime: missing timeZone");
            if (tzValue is JSTemporalZonedDateTime nestedZdt)
                tzValue = new JSString(nestedZdt.timeZoneId);
            if (!tzValue.IsString)
                throw JSEngine.NewTypeError("Temporal.ZonedDateTime: timeZone must be a string");
            timeZoneId = CanonicalizeTimeZone(tzValue.ToString());
        }

        // After all the bag's fields are read, the options are read in alphabetical order —
        // disambiguation, offset, overflow — and only then is the wall-clock date-time
        // interpreted with the overflow option (test262 ZonedDateTime/from
        // order-of-operations / options-read-before-algorithmic-validation). The
        // overflowReader runs at ToTemporalDateTime's overflow point (after the fields).
        var offsetOption = "reject";
        string ReadOptionsReturningOverflow()
        {
            ReadDisambiguation(options);                  // disambiguation (validated)
            offsetOption = ReadOffsetOption(options, "reject");
            return ReadOverflow(options);                 // overflow
        }

        // Resolve the wall-clock date-time by reusing Temporal.PlainDateTime's property-bag
        // resolution (non-Gregorian calendars), with offset/timeZone read inline.
        var pdt = (JSTemporalPlainDateTime)JSTemporalPlainDateTime.ToTemporalDateTime(
            obj, options, ReadOffset, ReadTimeZone, ReadOptionsReturningOverflow);

        var localNs = LocalNanoseconds(pdt.isoYear, pdt.isoMonth, pdt.isoDay,
            pdt.hour, pdt.minute, pdt.second, pdt.millisecond, pdt.microsecond, pdt.nanosecond);

        long offsetNs;
        long ZoneOffset() => GetOffsetForLocal(timeZoneId, pdt.isoYear, pdt.isoMonth, pdt.isoDay, pdt.hour, pdt.minute, pdt.second);

        if (!hasExplicitOffset)
            offsetNs = ZoneOffset();
        else
        {
            // InterpretISODateTimeOffset: the explicit offset is honoured verbatim only for "use";
            // "ignore" drops it; "prefer"/"reject" (the latter is the default for from) require it to be
            // one of the zone's offsets for the local time — a mismatch is a RangeError under "reject".
            // offsetOption was already read (in alphabetical order with the other options) above.
            if (offsetOption == "use")
                offsetNs = explicitOffset;
            else if (offsetOption == "ignore")
                offsetNs = ZoneOffset();
            else
            {
                var candidates = CandidateOffsetsForLocal(timeZoneId, pdt.isoYear, pdt.isoMonth, pdt.isoDay, pdt.hour, pdt.minute, pdt.second);
                if (candidates.Contains(explicitOffset))
                    offsetNs = explicitOffset;
                else if (offsetOption == "reject")
                    throw JSEngine.NewRangeError("Temporal.ZonedDateTime: offset does not match the time zone");
                else
                    offsetNs = ZoneOffset(); // "prefer"
            }
        }

        var epochNs = localNs - offsetNs;
        if (!IsValid(epochNs))
            throw JSEngine.NewRangeError("Temporal.ZonedDateTime: out of range");

        return new JSTemporalZonedDateTime(epochNs, timeZoneId, pdt.calendarId, ZonedDateTimePrototype);
    }

    private static BigInteger LocalNanoseconds(int y, int mo, int d, int h, int mi, int s, int ms, int us, int ns)
        => new BigInteger(DaysFromCivil(y, mo, d)) * NanosecondsPerDay
         + ((((long)h * 60 + mi) * 60 + s) * 1000L + ms) * 1_000_000L + (long)us * 1000 + ns;

    // A UTC offset *value* (the offset field of a property bag, or a "with" offset): ±HH[:MM[:SS[.fff]]],
    // which — unlike an offset time-zone *identifier* (OffsetIdPattern) — may carry sub-minute and
    // fractional-second precision.
    // The minute/second separators must be used consistently: an offset is either fully
    // colon-separated (±HH:MM[:SS[.fff]]) or fully unseparated (±HHMM[SS[.fff]]). A mixed form
    // such as "+00:0000" is not a valid UTCOffset (test262 from/offset-string-invalid), so the
    // two forms are spelled as separate alternatives sharing the m/s/f capture names.
    private static readonly Regex OffsetValuePattern = new(
        @"^(?<sign>[+-])(?<h>\d{2})(?::(?<m>\d{2})(?::(?<s>\d{2})(?:[.,](?<f>\d{1,9}))?)?|(?<m>\d{2})(?:(?<s>\d{2})(?:[.,](?<f>\d{1,9}))?)?)?$",
        RegexOptions.CultureInvariant);

    private static bool TryParseOffsetString(string text, out long offsetNs)
    {
        offsetNs = 0;
        if (string.Equals(text, "Z", StringComparison.OrdinalIgnoreCase)) return true;
        var m = OffsetValuePattern.Match(text);
        if (!m.Success) return false;
        var sign = m.Groups["sign"].Value is "-" or "−" ? -1 : 1;
        var hours = int.Parse(m.Groups["h"].Value, CultureInfo.InvariantCulture);
        var minutes = m.Groups["m"].Success ? int.Parse(m.Groups["m"].Value, CultureInfo.InvariantCulture) : 0;
        var seconds = m.Groups["s"].Success ? int.Parse(m.Groups["s"].Value, CultureInfo.InvariantCulture) : 0;
        var fractionNs = m.Groups["f"].Success ? long.Parse(m.Groups["f"].Value.PadRight(9, '0'), CultureInfo.InvariantCulture) : 0L;
        offsetNs = (long)sign * (((long)hours * 3600 + minutes * 60 + seconds) * 1_000_000_000 + fractionNs);
        return true;
    }

    // ── parsing the ISO string ────────────────────────────────────────────────────

    // The date-time + optional UTC-offset core of a ZonedDateTime string (the trailing RFC 9557
    // annotations are peeled off and validated separately). The date may use the extended
    // ("1976-11-18") or basic ("19761118") form, and the offset may carry sub-minute precision,
    // including a fractional-seconds part (e.g. +01:35:00.000000000).
    private const string YearField = @"\d{4}|\+\d{6}|-(?!000000)\d{6}";
    private static readonly Regex ZonedCorePattern = new(
        @"^(?:(?<y>" + YearField + @")-(?<mo>\d{2})-(?<d>\d{2})|(?<y>" + YearField + @")(?<mo>\d{2})(?<d>\d{2}))" +
        @"(?:[Tt ](?<h>\d{2})(?::?(?<mi>\d{2})(?::?(?<s>\d{2})(?:[.,](?<f>\d{1,9}))?)?)?" +
        @"(?:(?<z>[Zz])|(?<off>(?<osign>[+-])(?<oh>\d{2})(?::?(?<om>\d{2})(?::?(?<os>\d{2})(?:[.,](?<of>\d{1,9}))?)?)?))?)?$",
        RegexOptions.CultureInvariant);

    private static readonly Regex ZonedTrailingAnnotation = new(@"\[(!?)([^\]]*)\]$", RegexOptions.CultureInvariant);

    // `options` (ZonedDateTime.from) is read *after* the string has been fully parsed and
    // validated — an invalid string is a RangeError before any option getter runs (test262
    // ZonedDateTime/from observable-get-overflow-argument-string-invalid). When null (internal
    // callers) no options are read and the offset behaviour defaults to "reject".
    private static JSValue ParseZonedDateTimeString(string text, JSValue options = null)
    {
        // Validate the trailing annotations (multiple/critical calendar, malformed key=value, critical
        // unknown key, sub-minute offset annotation) before decomposing them.
        TemporalIsoString.RejectMultipleCalendarAnnotations(text);
        TemporalIsoString.RejectMalformedAnnotations(text);
        TemporalIsoString.RejectInvalidAnnotations(text);

        // Peel the trailing [..] annotations off the date/time core. A ZonedDateTime requires exactly
        // one time-zone annotation (the bracket whose contents carry no '='); the first [u-ca=…] sets
        // the calendar and any further (non-critical) annotations are ignored.
        var core = text;
        var annotations = new List<(bool Critical, string Content)>();
        while (true)
        {
            var am = ZonedTrailingAnnotation.Match(core);
            if (!am.Success || am.Index + am.Length != core.Length) break;
            annotations.Add((am.Groups[1].Value == "!", am.Groups[2].Value));
            core = core.Substring(0, am.Index);
        }
        annotations.Reverse(); // restore left-to-right order so the first annotation of each kind wins

        string timeZoneAnnotation = null, calendarValue = null;
        foreach (var (_, content) in annotations)
        {
            var eq = content.IndexOf('=');
            if (eq < 0) timeZoneAnnotation ??= content;
            else if (content.Substring(0, eq).Equals("u-ca", StringComparison.Ordinal))
                calendarValue ??= content.Substring(eq + 1);
        }

        if (timeZoneAnnotation == null)
            throw JSEngine.NewRangeError($"Cannot parse Temporal.ZonedDateTime from \"{text}\"");

        var match = ZonedCorePattern.Match(core);
        if (!match.Success)
            throw JSEngine.NewRangeError($"Cannot parse Temporal.ZonedDateTime from \"{text}\"");

        var calendarId = calendarValue != null ? TemporalCalendar.Canonicalize(calendarValue, includeArithmetic: true) : "iso8601";

        var year = int.Parse(match.Groups["y"].Value.Replace('−', '-'), CultureInfo.InvariantCulture);
        var month = int.Parse(match.Groups["mo"].Value, CultureInfo.InvariantCulture);
        var day = int.Parse(match.Groups["d"].Value, CultureInfo.InvariantCulture);
        var hour = match.Groups["h"].Success ? int.Parse(match.Groups["h"].Value, CultureInfo.InvariantCulture) : 0;
        var minute = match.Groups["mi"].Success ? int.Parse(match.Groups["mi"].Value, CultureInfo.InvariantCulture) : 0;
        var second = match.Groups["s"].Success ? int.Parse(match.Groups["s"].Value, CultureInfo.InvariantCulture) : 0;
        if (second == 60) second = 59; // leap second collapses

        int ms = 0, us = 0, ns = 0;
        if (match.Groups["f"].Success)
        {
            var digits = match.Groups["f"].Value.PadRight(9, '0');
            ms = int.Parse(digits.Substring(0, 3), CultureInfo.InvariantCulture);
            us = int.Parse(digits.Substring(3, 3), CultureInfo.InvariantCulture);
            ns = int.Parse(digits.Substring(6, 3), CultureInfo.InvariantCulture);
        }

        if (!IsValidISODate(year, month, day) || hour > 23 || minute > 59 || second > 59)
            throw JSEngine.NewRangeError($"Cannot parse Temporal.ZonedDateTime from \"{text}\"");

        var timeZoneId = CanonicalizeTimeZone(timeZoneAnnotation);
        var localNs = LocalNanoseconds(year, month, day, hour, minute, second, ms, us, ns);

        // A date-only ZonedDateTime string (no time component, hence no offset/Z) resolves to the
        // zone's start of day — GetStartOfDay, which across a gap covering midnight is the transition
        // instant, not midnight-with-disambiguation.
        var hasTime = match.Groups["h"].Success;

        var hasZ = match.Groups["z"].Success;
        var hasNumericOffset = match.Groups["off"].Success;

        // Parse and validate the explicit numeric offset (if any) — an invalid offset string
        // is a RangeError here, before any option getter runs.
        long explicitOffset = 0;
        var matchMinutes = false;
        if (hasNumericOffset)
        {
            // The offset must use consistent separators (e.g. "+00:0000" mixes ':' with bare digits).
            if (!TemporalIsoString.IsStrictOffset(match.Groups["off"].Value))
                throw JSEngine.NewRangeError($"Cannot parse Temporal.ZonedDateTime from \"{text}\"");

            var sign = match.Groups["osign"].Value is "-" or "−" ? -1 : 1;
            var oh = int.Parse(match.Groups["oh"].Value, CultureInfo.InvariantCulture);
            var om = match.Groups["om"].Success ? int.Parse(match.Groups["om"].Value, CultureInfo.InvariantCulture) : 0;
            var os = match.Groups["os"].Success ? int.Parse(match.Groups["os"].Value, CultureInfo.InvariantCulture) : 0;
            if (oh > 23 || om > 59 || os > 59)
                throw JSEngine.NewRangeError($"Cannot parse Temporal.ZonedDateTime from \"{text}\"");
            var offsetFractionNs = match.Groups["of"].Success
                ? long.Parse(match.Groups["of"].Value.PadRight(9, '0'), CultureInfo.InvariantCulture) : 0L;
            explicitOffset = (long)sign * ((long)(oh * 3600 + om * 60 + os) * 1_000_000_000 + offsetFractionNs);
            // An offset string carrying a seconds (or fractional-seconds) component must match a
            // candidate exactly; one with only hours/minutes precision uses match-minutes rounding.
            matchMinutes = !match.Groups["os"].Success && !match.Groups["of"].Success;
        }

        // The string is now fully parsed and validated; read the options (disambiguation,
        // offset, overflow) in alphabetical order before resolving the offset.
        var offsetOption = "reject";
        if (options != null)
        {
            ReadDisambiguation(options);
            offsetOption = ReadOffsetOption(options, "reject");
            ReadOverflow(options);
        }

        long offsetNs;
        if (hasZ) offsetNs = 0;
        else if (hasNumericOffset)
        {
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
                if (TryMatchZoneOffset(candidates, explicitOffset, matchMinutes, out var matchedOffset))
                    offsetNs = matchedOffset;
                else if (offsetOption == "reject")
                    throw JSEngine.NewRangeError($"Temporal.ZonedDateTime: offset does not match the time zone in \"{text}\"");
                else // "prefer": no match — use the zone's own offset
                    offsetNs = GetOffsetForLocal(timeZoneId, year, month, day, hour, minute, second);
            }
        }
        else offsetNs = GetOffsetForLocal(timeZoneId, year, month, day, hour, minute, second);

        var epochNs = hasTime ? localNs - offsetNs : StartOfDayEpochNs(timeZoneId, year, month, day);

        // InterpretISODateTimeOffset: only the "prefer"/"reject" matching paths inspect the *raw*
        // wall-clock date (CheckISODaysRange) before resolving the offset. A wall clock whose own
        // epoch day lies outside ±10^8 — e.g. -271821-04-19, the minimum PlainDate yet one day before
        // the minimum instant's date — is a RangeError there even when the resolved instant is itself
        // representable. The "exact" (Z), "use" and "ignore" paths (and the date-only start-of-day
        // path) only require the resolved instant to be in range.
        if (hasTime && hasNumericOffset && offsetOption is "prefer" or "reject"
            && Math.Abs(JSTemporalPlainDate.EpochDaysFor(year, month, day)) > 100_000_000)
            throw JSEngine.NewRangeError("Temporal.ZonedDateTime: wall-clock time is outside the representable range");

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

    // YYYY-MM-DDTHH:MM:SS[.fff]±HH:MM[timeZoneId] at full (auto) precision.
    private string ToISOString(string showCalendar = "auto")
        => FormatToString(epochNanoseconds, showCalendar, precision: -1, showOffset: true, timeZoneNameMode: "auto");

    // Serializes the instant `epoch` in this zone's wall clock. precision controls the seconds field
    // (-2 minutes only, -1 auto, 0..9 fixed); showOffset and timeZoneNameMode ("auto"/"never"/
    // "critical") control the trailing ±HH:MM offset and [timeZone] annotation.
    private string FormatToString(BigInteger epoch, string showCalendar, int precision, bool showOffset, string timeZoneNameMode)
    {
        var offsetNs = GetOffsetNanosecondsFor(epoch);
        var localNs = epoch + offsetNs;

        var totalSeconds = FloorDiv(localNs, 1_000_000_000);
        var fraction = (long)(localNs - totalSeconds * 1_000_000_000);
        var days = (long)FloorDiv(totalSeconds, 86400);
        var secondsOfDay = (long)(totalSeconds - new BigInteger(days) * 86400);
        var (y, mo, d) = CivilFromDays(days);
        var h = secondsOfDay / 3600;
        var mi = secondsOfDay % 3600 / 60;
        var s = secondsOfDay % 60;

        var sb = new StringBuilder();
        if (y < 0 || y > 9999)
            sb.Append(y < 0 ? '-' : '+').Append(Math.Abs(y).ToString("000000", CultureInfo.InvariantCulture));
        else
            sb.Append(y.ToString("0000", CultureInfo.InvariantCulture));

        sb.Append('-').Append(mo.ToString("00", CultureInfo.InvariantCulture))
          .Append('-').Append(d.ToString("00", CultureInfo.InvariantCulture))
          .Append('T').Append(h.ToString("00", CultureInfo.InvariantCulture))
          .Append(':').Append(mi.ToString("00", CultureInfo.InvariantCulture));

        if (precision != -2)
        {
            sb.Append(':').Append(s.ToString("00", CultureInfo.InvariantCulture));
            JSTemporalInstant.AppendFraction(sb, fraction, precision);
        }

        if (showOffset)
            sb.Append(FormatOffset(offsetNs));

        if (timeZoneNameMode != "never")
        {
            sb.Append('[');
            if (timeZoneNameMode == "critical") sb.Append('!');
            sb.Append(timeZoneId).Append(']');
        }

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
