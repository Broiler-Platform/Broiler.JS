using System;
using System.Numerics;
using Broiler.ECMAScript.Numbers;
using Broiler.ECMAScript.Temporal;
using Broiler.ECMAScript.Time;
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
// with a time zone and a calendar. The time zone is an offset identifier ("+01:00") or an
// available named zone; its offsets, possible instants, disambiguation, start of day and
// transitions are the shared library's (Broiler.ECMAScript.Time's ZonedTime), over the time zone
// tables this engine embeds, through TemporalTimeZones (milestone SJL-8.6). Its addition and
// differences are the shared Temporal core's DurationMath and TemporalDifferences over the zone's
// provider, and its day lengths and rounding to a day the core's arithmetic over GetStartOfDay
// (milestone SJL-10.5, part C). Registered under the Temporal namespace via Register = false.
[JSClassGenerator("ZonedDateTime", Register = false)]
public partial class JSTemporalZonedDateTime : JSObject
{
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
        if (!TemporalTimeZones.TryCanonicalize(tzArg.ToString(), out var canonicalTimeZone))
            throw JSEngine.NewRangeError($"Temporal.ZonedDateTime: invalid time zone identifier \"{tzArg}\"");
        timeZoneId = canonicalTimeZone;
        calendarId = TemporalAdapter.CalendarArgument(a.GetAt(2), "Temporal.ZonedDateTime");
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

    private static readonly KeyString ZonedDateTimeIntrinsicKey = KeyStrings.GetOrCreate("Temporal.ZonedDateTime");

    // %Temporal.ZonedDateTime.prototype% of the current realm, never the prototype of whatever
    // `globalThis.Temporal.ZonedDateTime` holds now: guest code may replace the namespace or its members.
    internal static JSObject ZonedDateTimePrototype => Intrinsics.TemporalPrototype(ZonedDateTimeIntrinsicKey);

    // IsValidEpochNanoseconds, the shared library's: within ±10^8 days of the epoch.
    private static bool IsValid(BigInteger ns) => TemporalAdapter.IsValidEpochNanoseconds(ns);

    // Factory used by sibling Temporal types (Instant/PlainDate*.to*ZonedDateTime) to build a
    // ZonedDateTime from an epoch instant + a time-zone string, canonicalizing the zone.
    internal static JSValue CreateChecked(BigInteger epochNs, string timeZone)
    {
        if (!IsValid(epochNs))
            throw JSEngine.NewRangeError("Temporal.ZonedDateTime: epoch nanoseconds out of range");
        return new JSTemporalZonedDateTime(epochNs, TemporalAdapter.ToTimeZoneIdentifier(timeZone), ZonedDateTimePrototype);
    }

    // Builds a ZonedDateTime from a local wall-clock datetime in the given zone (used by the
    // PlainDate/PlainDateTime/PlainTime → ZonedDateTime conversions; "compatible" disambiguation by
    // default, or the caller-supplied disambiguation (earlier / later / reject) for an ambiguous or
    // gap wall-clock time.
    internal static JSValue FromLocal(int y, int mo, int d, int h, int mi, int s, int ms, int us, int ns, string timeZone, string calendarId = "iso8601", string disambiguation = "compatible")
    {
        var tz = TemporalAdapter.ToTimeZoneIdentifier(timeZone);
        var epochNs = DisambiguateEpochNs(tz, y, mo, d, h, mi, s, ms, us, ns, disambiguation);
        if (!IsValid(epochNs))
            throw JSEngine.NewRangeError("Temporal.ZonedDateTime: out of range");
        return new JSTemporalZonedDateTime(epochNs, tz, calendarId, ZonedDateTimePrototype);
    }

    // GetEpochNanosecondsFor: resolve a local wall-clock time to a single epoch instant with the
    // disambiguation option (compatible / earlier / later / reject), the shared library's
    // GetPossibleEpochNanoseconds and DisambiguatePossibleEpochNanoseconds.
    private static BigInteger DisambiguateEpochNs(string tz, int y, int mo, int d, int h, int mi, int s, int ms, int us, int ns, string disambiguation)
        => TemporalTimeZones.EpochNanosecondsFor(tz, IsoDateTimeOf(y, mo, d, h, mi, s, ms, us, ns), disambiguation);

    private static IsoDateTime IsoDateTimeOf(int y, int mo, int d, int h, int mi, int s, int ms, int us, int ns)
        => TemporalAdapter.IsoDateTimeOf(y, mo, d, h, mi, s, ms, us, ns);

    // GetStartOfDay for a date in a zone (Temporal.PlainDate.prototype.toZonedDateTime with no
    // plainTime): the first instant of the day, which across a gap covering midnight is the transition
    // instant rather than midnight.
    internal static JSValue StartOfDayFor(int y, int mo, int d, string timeZone, string calendarId = "iso8601")
    {
        var tz = TemporalAdapter.ToTimeZoneIdentifier(timeZone);
        return new JSTemporalZonedDateTime(StartOfDayEpochNs(tz, new IsoDate(y, mo, d)), tz, calendarId, ZonedDateTimePrototype);
    }

    // GetStartOfDay (s11.1.14), the shared library's over the zone's rules (TemporalTimeZones.StartOfDay): the first
    // instant of a day, a RangeError where it is outside the representable instants. Every start of day this type
    // reads - startOfDay, withPlainTime, hoursInDay, the rounding to a day, PlainDate.prototype.toZonedDateTime and a
    // date-only string - is this one.
    private static BigInteger StartOfDayEpochNs(string timeZone, IsoDate date)
    {
        var epochNs = TemporalTimeZones.StartOfDay(timeZone, date);
        if (!IsValid(epochNs))
            throw JSEngine.NewRangeError("Temporal.ZonedDateTime: start of day is outside the representable range");
        return epochNs;
    }

    // The starts of the local day and of the next day: the day's real length is the time between them.
    private (BigInteger Today, BigInteger Tomorrow) DayStarts()
    {
        var l = Local();
        var today = new IsoDate(l.y, l.mo, l.d);
        return (StartOfDayEpochNs(timeZoneId, today), StartOfDayEpochNs(timeZoneId, IsoArithmetic.AddDaysToIsoDate(today, 1)));
    }

    // ── accessors ───────────────────────────────────────────────────────────────

    [JSExport("calendarId")] public JSValue CalendarId => new JSString(calendarId);
    [JSExport("timeZoneId")] public JSValue TimeZoneId => new JSString(timeZoneId);
    // CalendarISOToDate of the local wall-clock date, the shared library's: the calendar's fields, which every
    // date getter reads.
    private CalendarDate CalendarDate
    {
        get { var l = Local(); return TemporalAdapter.CalendarDateOf(calendarId, new IsoDate(l.y, l.mo, l.d)); }
    }

    // era and eraYear are undefined in the calendars without eras (iso8601, chinese, dangi).
    [JSExport("era")] public JSValue Era => TemporalAdapter.EraValue(CalendarDate);
    [JSExport("eraYear")] public JSValue EraYear => TemporalAdapter.EraYearValue(CalendarDate);

    [JSExport("epochMilliseconds")] public double EpochMilliseconds => TemporalAdapter.EpochMilliseconds(epochNanoseconds);
    [JSExport("epochNanoseconds")] public JSValue EpochNanoseconds => new JSBigInt(epochNanoseconds);

    [JSExport("year")] public double YearValue => CalendarDate.Year;
    [JSExport("month")] public double MonthValue => CalendarDate.Month;
    [JSExport("monthCode")] public JSValue MonthCode => new JSString(CalendarDate.MonthCode.ToString());
    [JSExport("day")] public double DayValue => CalendarDate.Day;
    [JSExport("hour")] public double HourValue => Local().h;
    [JSExport("minute")] public double MinuteValue => Local().mi;
    [JSExport("second")] public double SecondValue => Local().s;
    [JSExport("millisecond")] public double MillisecondValue => Local().ms;
    [JSExport("microsecond")] public double MicrosecondValue => Local().us;
    [JSExport("nanosecond")] public double NanosecondValue => Local().ns;

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

    [JSExport("offsetNanoseconds")] public double OffsetNanosecondsValue => OffsetNanoseconds();
    // FormatUTCOffsetNanoseconds, the shared library's: to the minute, or with the seconds where the offset has them.
    [JSExport("offset")] public JSValue Offset => new JSString(TemporalFormat.FormatUtcOffsetNanoseconds(OffsetNanoseconds()));

    // The day's real length: GetStartOfDay of today and of tomorrow (either may fall outside the representable
    // instant range at the extreme epoch limits, a RangeError), then TotalTimeDuration in hours, the shared library's
    // exact ratio rounded once.
    [JSExport("hoursInDay")]
    public double HoursInDay
    {
        get
        {
            var (todayStart, tomorrowStart) = DayStarts();
            return DurationMath.TotalTimeDuration(tomorrowStart - todayStart, TemporalUnit.Hour);
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
        return epochNanoseconds == other.epochNanoseconds && TemporalTimeZones.Equal(timeZoneId, other.timeZoneId)
            && calendarId == other.calendarId
            ? BooleanTrue : BooleanFalse;
    }

    [JSExport("withTimeZone", Length = 1)]
    public JSValue WithTimeZone(in Arguments a)
    {
        var tz = a.GetAt(0);
        if (tz == null || !tz.IsString)
            throw JSEngine.NewTypeError("Temporal.ZonedDateTime.prototype.withTimeZone: time zone must be a string");
        return new JSTemporalZonedDateTime(epochNanoseconds, TemporalAdapter.ToTimeZoneIdentifier(tz.ToString()), calendarId, ZonedDateTimePrototype);
    }

    [JSExport("withCalendar", Length = 1)]
    public JSValue WithCalendar(in Arguments a)
    {
        var arg = a.GetAt(0);
        if (arg == null || arg.IsUndefined)
            throw JSEngine.NewTypeError("Temporal.ZonedDateTime.prototype.withCalendar requires a calendar");
        return new JSTemporalZonedDateTime(epochNanoseconds, timeZoneId, TemporalAdapter.ToCalendarIdentifier(arg), ZonedDateTimePrototype);
    }

    [JSExport("startOfDay", Length = 0)]
    public JSValue StartOfDay(in Arguments a)
    {
        var l = Local();
        return new JSTemporalZonedDateTime(StartOfDayEpochNs(timeZoneId, new IsoDate(l.y, l.mo, l.d)), timeZoneId, calendarId, ZonedDateTimePrototype);
    }

    // Replaces the wall-clock time of day, keeping the date, calendar and time zone. An absent argument
    // means the start of the day (midnight, after disambiguation); otherwise the argument is coerced to
    // a Temporal.PlainTime. The new local datetime is re-resolved in the zone with "compatible"
    // disambiguation, which reads a gap or an overlap as GetEpochNanosecondsFor does.
    [JSExport("withPlainTime", Length = 0)]
    public JSValue WithPlainTime(in Arguments a)
    {
        var l = Local();
        var arg = a.GetAt(0);
        if (arg == null || arg.IsUndefined)
            return new JSTemporalZonedDateTime(StartOfDayEpochNs(timeZoneId, new IsoDate(l.y, l.mo, l.d)), timeZoneId, calendarId, ZonedDateTimePrototype);

        var t = JSTemporalPlainTime.ToTemporalTimeObject(arg);
        var epoch = EpochNsForLocal(l.y, l.mo, l.d, t.hour, t.minute, t.second, t.millisecond, t.microsecond, t.nanosecond);
        if (!IsValid(epoch)) throw JSEngine.NewRangeError("Temporal.ZonedDateTime.prototype.withPlainTime: result is out of range");
        return new JSTemporalZonedDateTime(epoch, timeZoneId, calendarId, ZonedDateTimePrototype);
    }

    // Returns the ZonedDateTime of the nearest UTC-offset transition of the time zone after ("next") or
    // before ("previous") this instant, or null when the zone has no such transition within the instant
    // range (an offset zone has none). A named zone's transitions continue by its recurring rules after
    // its last listed one, with no 2037 cliff.
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

        var transition = direction == "next"
            ? TemporalTimeZones.NextTransition(timeZoneId, epochNanoseconds)
            : TemporalTimeZones.PreviousTransition(timeZoneId, epochNanoseconds);
        if (transition == null) return JSNull.Value;
        return new JSTemporalZonedDateTime(transition.Value, timeZoneId, calendarId, ZonedDateTimePrototype);
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

    // Projects this ZonedDateTime's wall-clock time onto a Temporal.PlainTime purely from its
    // internal slots, without reading any observable property. Used by ToTemporalTime so
    // Temporal.PlainTime.from consumes a ZonedDateTime via its slots (no getter calls).
    internal JSValue ToPlainTimeFromSlots()
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
        // timeZoneName — coercing each as it is read. All options are read before the
        // smallestUnit's group is validated (test262 ZonedDateTime/prototype/toString
        // order-of-operations and options-read-before-algorithmic-validation).
        var showCalendar = TemporalAdapter.ReadShowCalendarName(options);
        var digits = TemporalFormat.AutoPrecision;
        var roundingMode = RoundingMode.Trunc;
        string smallestUnit = null;
        var showOffset = ShowOffset.Auto;
        var showTimeZone = ShowTimeZoneName.Auto;

        if (options is JSObject o)
        {
            digits = TemporalAdapter.ReadFractionalSecondDigits(o);
            showOffset = TemporalAdapter.ReadShowOffset(o);
            roundingMode = TemporalRoundingOptions.GetRoundingMode(o, RoundingMode.Trunc);
            smallestUnit = TemporalAdapter.ReadSmallestUnitText(o);
            showTimeZone = TemporalAdapter.ReadShowTimeZoneName(o);
        }

        var precision = TemporalAdapter.ToStringPrecision(smallestUnit, digits, duration: false, "Temporal.ZonedDateTime");

        // RoundTemporalInstant, then the wall clock and offset of the rounded instant written by
        // TemporalZonedDateTimeToString: the shared library's. The epoch count is rounded as if non-negative so
        // the direction never depends on the sign (toString/negative-zoneddatetime-rounding,
        // toString/rounding-direction).
        var rounded = IsoArithmetic.RoundTemporalInstant(epochNanoseconds, precision.Increment, precision.Unit, roundingMode);
        if (!IsValid(rounded))
            throw JSEngine.NewRangeError("Temporal.ZonedDateTime.toString: result is out of range");

        return new JSString(TemporalFormat.ZonedDateTimeToString(rounded, GetOffsetNanosecondsFor(rounded), timeZoneId, calendarId,
            precision.Precision, showCalendar, showTimeZone, showOffset));
    }

    [JSExport("toJSON", Length = 0)]
    public JSValue ToJSON(in Arguments a)
        => new JSString(TemporalFormat.ZonedDateTimeToString(epochNanoseconds, OffsetNanoseconds(), timeZoneId, calendarId,
            TemporalFormat.AutoPrecision, ShowCalendarName.Auto, ShowTimeZoneName.Auto, ShowOffset.Auto));

    // A ZonedDateTime formats in its own time zone with the zone name shown by default; the zone is
    // injected into the formatter options (a conflicting timeZone option is a RangeError). Direct
    // DateTimeFormat.format on a ZonedDateTime is a TypeError, but toLocaleString is allowed.
    [JSExport("toLocaleString", Length = 0)]
    public JSValue ToLocaleString(in Arguments a)
        => Intl.JSIntlDateTimeFormat.TemporalToLocaleString(this, a.GetAt(0), a.GetAt(1), defaultTimeZone: timeZoneId);

    [JSExport("valueOf", Length = 0)]
    public JSValue ValueOf(in Arguments a)
        => throw JSEngine.NewTypeError("Called Temporal.ZonedDateTime.prototype.valueOf, which is not supported. Use Temporal.ZonedDateTime.compare for comparison.");

    // Calendar and zone arithmetic: add, subtract, until and since are the shared library's, over the calendars and the
    // zone's provider (DST-aware).
    [JSExport("add", Length = 1)]
    public JSValue Add(in Arguments a) => AddDuration(a.GetAt(0), a.GetAt(1), subtract: false);
    [JSExport("subtract", Length = 1)]
    public JSValue Subtract(in Arguments a) => AddDuration(a.GetAt(0), a.GetAt(1), subtract: true);

    [JSExport("until", Length = 1)]
    public JSValue Until(in Arguments a) => Difference(a.GetAt(0), a.GetAt(1), DifferenceOperation.Until);

    [JSExport("since", Length = 1)]
    public JSValue Since(in Arguments a) => Difference(a.GetAt(0), a.GetAt(1), DifferenceOperation.Since);

    [JSExport("round", Length = 1)]
    public JSValue Round(in Arguments a)
    {
        // The reads - roundingIncrement, roundingMode, the required smallestUnit - each converted as it is read, all
        // before the library's rule (a time unit, or a day of increment 1) is applied (test262
        // round/options-read-before-algorithmic-validation).
        const string typeName = "Temporal.ZonedDateTime";
        var (increment, roundingMode, smallestUnit) = TemporalAdapter.RoundOptions(TemporalType.ZonedDateTime, a.GetAt(0), typeName);

        var l = Local();

        if (smallestUnit == TemporalUnit.Day)
        {
            // Round to a day boundary over the day's real length: GetStartOfDay of today and tomorrow, both
            // representable instants, then RoundTimeDurationToIncrement within the day.
            var (dayStart, tomorrowStart) = DayStarts();
            TemporalAdapter.CheckArithmetic(
                DurationMath.RoundTimeDurationToIncrement(epochNanoseconds - dayStart, tomorrowStart - dayStart, roundingMode, out var rounded), typeName);
            var epoch = dayStart + rounded;
            if (!IsValid(epoch)) throw JSEngine.NewRangeError("Temporal.ZonedDateTime: out of range");
            return new JSTemporalZonedDateTime(epoch, timeZoneId, calendarId, ZonedDateTimePrototype);
        }

        // Round the wall-clock time of day, carrying any overflow into the date (RoundISODateTime, the
        // shared library's), then re-resolve in the zone keeping the current offset when it is still
        // valid ("prefer").
        var roundedLocal = IsoArithmetic.RoundIsoDateTime(
            IsoDateTimeOf(l.y, l.mo, l.d, l.h, l.mi, l.s, l.ms, l.us, l.ns), increment, smallestUnit, roundingMode);
        var (rd, rt) = (roundedLocal.Date, roundedLocal.Time);

        var localNs = IsoArithmetic.GetUtcEpochNanoseconds(roundedLocal);
        var oldOffset = OffsetNanoseconds();
        var candidate = localNs - oldOffset;
        var resolved = GetOffsetNanosecondsFor(candidate) == oldOffset
            ? candidate
            : EpochNsForLocal((int)rd.Year, rd.Month, rd.Day, rt.Hour, rt.Minute, rt.Second, rt.Millisecond, rt.Microsecond, rt.Nanosecond);
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

        // Merge the partial fields onto the current local wall-clock datetime, reusing the
        // calendar-aware PlainDateTime.with (which rejects calendar/timeZone fields, applies the
        // overflow option and validates era / monthCode / partial era pairs).
        var l = Local();
        var current = new JSTemporalPlainDateTime(l.y, l.mo, l.d, l.h, l.mi, l.s, l.ms, l.us, l.ns, calendarId,
            JSTemporalPlainDateTime.PlainDateTimePrototype);

        // PrepareCalendarFields reads the ZonedDateTime field list — the PlainDateTime fields plus
        // `offset` (which sorts alphabetically between nanosecond and second) — in a single pass, and
        // the disambiguation / offset / overflow options are all read afterwards. Drive that exact
        // order through PlainDateTime.WithCore's hooks rather than reading offset/options up front
        // (test262 ZonedDateTime/prototype/with order-of-operations), in every calendar.

        long candidateOffset = OffsetNanoseconds();
        var offsetPresent = false;
        string offsetString = null;
        var offsetOption = "prefer";
        var disambiguation = "compatible";

        // `offset` is read (and ToString-coerced) at its alphabetical position; a present non-string
        // offset is a TypeError there. Parsing/validation (RangeError) is deferred until after every
        // field and option has been read, matching InterpretISODateTimeOffset.
        void ReadOffsetField()
        {
            var offsetField = fields[KeyStrings.GetOrCreate("offset")];
            if (offsetField.IsUndefined)
                return;
            offsetPresent = true;
            var offsetPrimitive = offsetField is JSObject offsetObj ? offsetObj.ToStringPrimitive() : offsetField;
            if (!offsetPrimitive.IsString)
                throw JSEngine.NewTypeError("Temporal.ZonedDateTime.prototype.with: the offset field must be a string");
            offsetString = offsetPrimitive.StringValue;
        }

        string ReadOptions()
        {
            // disambiguation, then offset, then overflow — every option getter fires after all fields.
            disambiguation = ReadDisambiguation(options);
            offsetOption = ReadOffsetOption(options);
            return ReadOverflow(options);
        }

        var updated = (JSTemporalPlainDateTime)current.WithCore(
            new Arguments(JSUndefined.Value, fields, options),
            afterNanosecond: ReadOffsetField,
            overflowReader: ReadOptions,
            hasExtraField: () => offsetPresent);

        if (offsetPresent && !TemporalAdapter.TryParseOffsetString(offsetString, out candidateOffset))
            throw JSEngine.NewRangeError("Temporal.ZonedDateTime.prototype.with: invalid offset");

        return MergeWith(updated, candidateOffset, offsetOption, disambiguation);
    }

    // Re-anchors the updated wall-clock date-time onto the epoch timeline using the candidate offset
    // and the GetTemporalOffsetOption mode (use / ignore / prefer / reject).
    private JSValue MergeWith(JSTemporalPlainDateTime updated, long candidateOffset, string offsetOption, string disambiguation)
    {
        var localNs = LocalNanoseconds(updated.isoYear, updated.isoMonth, updated.isoDay,
            updated.hour, updated.minute, updated.second, updated.millisecond, updated.microsecond, updated.nanosecond);

        // The "wall" cases ("ignore", or an unmatched "prefer") re-anchor the new wall clock through the
        // DISAMBIGUATION option (earlier/later/compatible/reject) so a fold/gap honours `disambiguation`
        // (test262 ZonedDateTime/prototype/with/dst-option-offset-disambiguation-combinations).
        BigInteger Wall() => DisambiguateEpochNs(timeZoneId, updated.isoYear, updated.isoMonth, updated.isoDay,
            updated.hour, updated.minute, updated.second, updated.millisecond, updated.microsecond, updated.nanosecond, disambiguation);

        BigInteger epoch;
        if (offsetOption == "use")
            epoch = localNs - candidateOffset;
        else if (offsetOption == "ignore")
            epoch = Wall();
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
                epoch = Wall();
        }

        if (!IsValid(epoch))
            throw JSEngine.NewRangeError("Temporal.ZonedDateTime: out of range");
        return new JSTemporalZonedDateTime(epoch, timeZoneId, calendarId, ZonedDateTimePrototype);
    }

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

    // AddDurationToZonedDateTime (s6.5.10): ToTemporalDuration, then the overflow option, then the shared library's
    // AddZonedDateTime - the date part added to the wall clock in the calendar, the wall clock made an instant again
    // ("compatible"), then the exact time part (row R7) - the duration negated for subtract.
    private JSValue AddDuration(JSValue durationLike, JSValue options, bool subtract)
    {
        var fields = ((JSTemporalDuration)JSTemporalDuration.ToTemporalDuration(durationLike)).Fields;
        if (subtract)
            fields = DurationMath.Negate(fields);
        var overflow = TemporalAdapter.Overflow(ReadOverflow(options));

        TemporalAdapter.CheckArithmetic(DurationMath.AddZonedDateTime(epochNanoseconds, TemporalTimeZones.Arithmetic(timeZoneId), TemporalAdapter.Calendars,
            TemporalAdapter.CalendarIdOf(calendarId), DurationMath.ToInternal(fields), overflow, out var result), "Temporal.ZonedDateTime");
        return new JSTemporalZonedDateTime(result, timeZoneId, calendarId, ZonedDateTimePrototype);
    }

    // DifferenceTemporalZonedDateTime (s6.5.9): ToTemporalZonedDateTime, the calendars equal, GetDifferenceSettings' reads
    // and rules, and - for a date largest unit - the zones equal (TimeZoneEquals, by primary identifier, so that
    // Asia/Calcutta and Asia/Kolkata are one zone); then the shared library's difference: an instant's below a day, and from
    // a day up the wall clocks' in the zone, rounded over its real days (row F7) in the calendar (row F4).
    private JSValue Difference(JSValue otherValue, JSValue options, DifferenceOperation operation)
    {
        const string typeName = "Temporal.ZonedDateTime";
        var other = Require(ToZonedDateTime(otherValue));
        if (calendarId != other.calendarId)
            throw JSEngine.NewRangeError("Temporal.ZonedDateTime: cannot compute the difference between date-times of different calendars");

        var settings = TemporalAdapter.DifferenceSettings(TemporalType.ZonedDateTime, operation, options, typeName);
        if (TemporalOptionRules.IsDateUnit(settings.LargestUnit) && !TemporalTimeZones.Equal(timeZoneId, other.timeZoneId))
            throw JSEngine.NewRangeError("Temporal.ZonedDateTime: cannot compute a calendar-unit difference between date-times in different time zones");

        return TemporalAdapter.DurationOf(TemporalDifferences.DifferenceTemporalZonedDateTime(operation, epochNanoseconds, other.epochNanoseconds,
            TemporalTimeZones.Arithmetic(timeZoneId), TemporalAdapter.Calendars, TemporalAdapter.CalendarIdOf(calendarId), settings, out var result), result, typeName);
    }

    // Local wall-clock components for an arbitrary epoch instant in this zone: GetISODateTimeFor, the
    // shared library's GetISOPartsFromEpoch of the instant plus its offset.
    private (int y, int mo, int d, int h, int mi, int s, int ms, int us, int ns) LocalAt(BigInteger epochNs)
    {
        var local = IsoArithmetic.GetIsoPartsFromEpoch(epochNs + GetOffsetNanosecondsFor(epochNs));
        var (date, time) = (local.Date, local.Time);
        return ((int)date.Year, date.Month, date.Day, time.Hour, time.Minute, time.Second, time.Millisecond, time.Microsecond, time.Nanosecond);
    }

    // The epoch instant for a local wall-clock datetime in this zone ("compatible" disambiguation).
    private BigInteger EpochNsForLocal(int y, int mo, int d, int h, int mi, int s, int ms, int us, int ns)
        => DisambiguateEpochNs(timeZoneId, y, mo, d, h, mi, s, ms, us, ns, "compatible");

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

    // GetOffsetNanosecondsFor: the shared library's, over this zone's rules.
    private long GetOffsetNanosecondsFor(BigInteger epochNs) => TemporalTimeZones.OffsetNanoseconds(timeZoneId, epochNs);

    // ToTemporalTimeZoneIdentifier for a slot value supplied as a JSValue: a ZonedDateTime contributes
    // its own zone; a String is a bare identifier or an ISO string carrying a time-zone designator;
    // anything else is a TypeError and an unrecognized identifier a RangeError.
    internal static string ToTimeZoneIdentifier(JSValue value)
    {
        if (value is JSTemporalZonedDateTime zdt) return zdt.timeZoneId;
        if (value == null || !value.IsString)
            throw JSEngine.NewTypeError("Temporal: time zone must be a string identifier");
        return TemporalAdapter.ToTimeZoneIdentifier(value.ToString());
    }

    // InterpretISODateTimeOffset offset-matching for the "prefer"/"reject" options: does the explicit
    // offset match the offset of one of the wall clock's possible instants (GetPossibleEpochNanoseconds,
    // each offset being the wall clock read as UTC minus the instant)? A minute-precision offset string
    // (no sub-minute component) uses the match-minutes behaviour — a candidate matches when it equals the
    // explicit offset after being rounded to the nearest minute (half away from zero) — so
    // Africa/Monrovia's -00:44:30 offset is matched by the string "-00:45". The matched instant (whose
    // offset is the real one, not the rounded value) is returned. The rounding is
    // RoundNumberToIncrement(offsetNs, 60e9, "halfExpand"), the shared library's.
    private static bool TryMatchZoneOffset(BigInteger[] possible, BigInteger localNs, long explicitOffset, bool matchMinutes, out BigInteger matched)
    {
        foreach (var candidate in possible)
        {
            var candidateOffset = localNs - candidate;
            if (candidateOffset == explicitOffset
                || (matchMinutes && ExactRounding.RoundNumberToIncrement(candidateOffset, BigInteger.One, NanosecondsPerMinute, RoundingMode.HalfExpand) == explicitOffset))
            {
                matched = candidate;
                return true;
            }
        }
        matched = BigInteger.Zero;
        return false;
    }

    private const long NanosecondsPerMinute = 60_000_000_000L;

    private (int y, int mo, int d, int h, int mi, int s, int ms, int us, int ns) Local() => LocalAt(epochNanoseconds);

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
        return (int)TemporalRoundingOptions.ToIntegerWithTruncation(value, "Temporal.ZonedDateTime: component must be finite");
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
            if (!TemporalAdapter.TryParseOffsetString(offsetPrimitive.StringValue, out explicitOffset))
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
            timeZoneId = TemporalAdapter.ToTimeZoneIdentifier(tzValue.ToString());
        }

        // After all the bag's fields are read, the options are read in alphabetical order —
        // disambiguation, offset, overflow — and only then is the wall-clock date-time
        // interpreted with the overflow option (test262 ZonedDateTime/from
        // order-of-operations / options-read-before-algorithmic-validation). The
        // overflowReader runs at ToTemporalDateTime's overflow point (after the fields).
        var offsetOption = "reject";
        var disambiguation = "compatible";
        string ReadOptionsReturningOverflow()
        {
            disambiguation = ReadDisambiguation(options); // disambiguation
            offsetOption = ReadOffsetOption(options, "reject");
            return ReadOverflow(options);                 // overflow
        }

        // Resolve the wall-clock date-time by reusing Temporal.PlainDateTime's property-bag
        // resolution (non-Gregorian calendars), with offset/timeZone read inline.
        var pdt = (JSTemporalPlainDateTime)JSTemporalPlainDateTime.ToTemporalDateTime(
            obj, options, ReadOffset, ReadTimeZone, ReadOptionsReturningOverflow);

        var localNs = LocalNanoseconds(pdt.isoYear, pdt.isoMonth, pdt.isoDay,
            pdt.hour, pdt.minute, pdt.second, pdt.millisecond, pdt.microsecond, pdt.nanosecond);

        // InterpretISODateTimeOffset: the "wall" cases (no offset, "ignore", unmatched "prefer")
        // resolve through the DISAMBIGUATION option (earlier/later/compatible/reject) so a fall-back
        // fold or spring-forward gap honours `disambiguation` (test262 ZonedDateTime/from
        // argument-propertybag-dst-option{,s}-{,offset-}disambiguation*). An exact offset gives the
        // epoch directly; a property-bag offset must match a candidate EXACTLY (no minute rounding).
        BigInteger ResolveWall() => DisambiguateEpochNs(timeZoneId, pdt.isoYear, pdt.isoMonth, pdt.isoDay, pdt.hour, pdt.minute, pdt.second, pdt.millisecond, pdt.microsecond, pdt.nanosecond, disambiguation);

        BigInteger epochNs;
        if (!hasExplicitOffset)
            epochNs = ResolveWall();
        else if (offsetOption == "use")
            epochNs = localNs - explicitOffset;
        else if (offsetOption == "ignore")
            epochNs = ResolveWall();
        else
        {
            var possible = TemporalTimeZones.PossibleEpochNanoseconds(timeZoneId, IsoDateTimeOf(pdt.isoYear, pdt.isoMonth, pdt.isoDay,
                pdt.hour, pdt.minute, pdt.second, pdt.millisecond, pdt.microsecond, pdt.nanosecond));
            if (TryMatchZoneOffset(possible, localNs, explicitOffset, matchMinutes: false, out var matched))
                epochNs = matched;
            else if (offsetOption == "reject")
                throw JSEngine.NewRangeError("Temporal.ZonedDateTime: offset does not match the time zone");
            else
                epochNs = ResolveWall(); // "prefer"
        }

        if (!IsValid(epochNs))
            throw JSEngine.NewRangeError("Temporal.ZonedDateTime: out of range");

        return new JSTemporalZonedDateTime(epochNs, timeZoneId, pdt.calendarId, ZonedDateTimePrototype);
    }

    // A wall clock read as UTC: GetUTCEpochNanoseconds, the shared library's.
    private static BigInteger LocalNanoseconds(int y, int mo, int d, int h, int mi, int s, int ms, int us, int ns)
        => IsoArithmetic.GetUtcEpochNanoseconds(IsoDateTimeOf(y, mo, d, h, mi, s, ms, us, ns));

    // ── parsing the ISO string ────────────────────────────────────────────────────

    // ParseISODateTime with the TemporalDateTimeString[+Zoned] goal, the shared library's grammar, whose
    // annotations are checked (a time zone annotation is required, and stands first). `options`
    // (ZonedDateTime.from) is read *after* the string has been fully parsed and validated — an invalid
    // string is a RangeError before any option getter runs (test262 ZonedDateTime/from
    // observable-get-overflow-argument-string-invalid). When null (internal callers) no options are read
    // and the offset behaviour defaults to "reject".
    private static JSValue ParseZonedDateTimeString(string text, JSValue options = null)
        => FromParsed(TemporalAdapter.ParseIsoString(text, [TemporalGoal.ZonedDateTime], "Temporal.ZonedDateTime"), text, options);

    // ToTemporalZonedDateTime's steps after ParseISODateTime (a ZonedDateTime string, or a relativeTo string
    // with a time zone annotation): ToTemporalTimeZoneIdentifier of the annotation, the calendar annotation
    // canonicalized, the options, then InterpretISODateTimeOffset.
    internal static JSTemporalZonedDateTime FromParsed(IsoParseResult parsed, string text, JSValue options)
    {
        var timeZoneId = TemporalAdapter.ToTimeZoneIdentifier(parsed.TimeZone!);
        var calendarId = TemporalAdapter.CalendarOf(parsed);

        var date = TemporalAdapter.DateOf(parsed);
        var (year, month, day) = ((int)date.Year, date.Month, date.Day);
        var time = parsed.Time ?? default;
        var (hour, minute, second, ms, us, ns) = (time.Hour, time.Minute, time.Second, time.Millisecond, time.Microsecond, time.Nanosecond);
        var localNs = LocalNanoseconds(year, month, day, hour, minute, second, ms, us, ns);

        // A date-only ZonedDateTime string (no time component, hence no offset/Z) resolves to the
        // zone's start of day — GetStartOfDay, which across a gap covering midnight is the transition
        // instant, not midnight-with-disambiguation.
        var hasTime = parsed.Time.HasValue;

        var hasZ = parsed.Z;
        var hasNumericOffset = parsed.Offset != null;

        // The explicit numeric offset's value; an offset string carrying a seconds (or fractional-seconds)
        // component must match a candidate exactly, one with only hours/minutes precision uses
        // match-minutes rounding.
        var explicitOffset = parsed.OffsetNanoseconds;
        var matchMinutes = !parsed.OffsetHasSeconds;

        // The string is now fully parsed and validated; read the options (disambiguation,
        // offset, overflow) in alphabetical order before resolving the offset.
        var offsetOption = "reject";
        var disambiguation = "compatible";
        if (options != null)
        {
            disambiguation = ReadDisambiguation(options);
            offsetOption = ReadOffsetOption(options, "reject");
            ReadOverflow(options);
        }

        // InterpretISODateTimeOffset. The "wall" cases (no numeric offset, or an offset dropped by
        // "ignore", or an unmatched "prefer") resolve the wall clock through the DISAMBIGUATION option
        // (earlier/later/compatible/reject) — not always "compatible" — so a fall-back fold or a
        // spring-forward gap honours `disambiguation` (test262 ZonedDateTime/from
        // argument-string-dst-option-{,offset-}disambiguation*). An exact offset gives the epoch directly.
        BigInteger ResolveWall() => DisambiguateEpochNs(timeZoneId, year, month, day, hour, minute, second, ms, us, ns, disambiguation);

        BigInteger epochNs;
        if (!hasTime)
            epochNs = StartOfDayEpochNs(timeZoneId, new IsoDate(year, month, day));
        else if (hasZ)
            epochNs = localNs;
        else if (!hasNumericOffset)
            epochNs = ResolveWall();
        else if (offsetOption == "use")
            epochNs = localNs - explicitOffset;
        else if (offsetOption == "ignore")
            epochNs = ResolveWall();
        else
        {
            var possible = TemporalTimeZones.PossibleEpochNanoseconds(timeZoneId, IsoDateTimeOf(year, month, day, hour, minute, second, ms, us, ns));
            if (TryMatchZoneOffset(possible, localNs, explicitOffset, matchMinutes, out var matched))
                epochNs = matched;
            else if (offsetOption == "reject")
                throw JSEngine.NewRangeError($"Temporal.ZonedDateTime: offset does not match the time zone in \"{text}\"");
            else // "prefer": no match — resolve the wall clock via disambiguation
                epochNs = ResolveWall();
        }

        // InterpretISODateTimeOffset: only the "prefer"/"reject" matching paths inspect the *raw*
        // wall-clock date (CheckISODaysRange) before resolving the offset. A wall clock whose own
        // epoch day lies outside ±10^8 — e.g. -271821-04-19, the minimum PlainDate yet one day before
        // the minimum instant's date — is a RangeError there even when the resolved instant is itself
        // representable. The "exact" (Z), "use" and "ignore" paths (and the date-only start-of-day
        // path) only require the resolved instant to be in range.
        if (hasTime && hasNumericOffset && offsetOption is "prefer" or "reject"
            && TemporalLimits.CheckIsoDaysRange(new IsoDate(year, month, day)) != TemporalStatus.Completed)
            throw JSEngine.NewRangeError("Temporal.ZonedDateTime: wall-clock time is outside the representable range");

        if (!IsValid(epochNs))
            throw JSEngine.NewRangeError("Temporal.ZonedDateTime: parsed value is out of range");

        return new JSTemporalZonedDateTime(epochNs, timeZoneId, calendarId, ZonedDateTimePrototype);
    }
}
