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

// Temporal.PlainDateTime (Temporal proposal §5): a calendar date plus a wall-clock time, with
// no time zone. It is the combination of PlainDate (year/month/day) and PlainTime (hour …
// nanosecond); its calendar fields, field resolution and date addition are the shared library's
// calendars, and its rounding and differences, rounded in any calendar, the shared library's
// IsoArithmetic and TemporalDifferences, through TemporalAdapter. Registered under the Temporal
// namespace (not as a global) via Register = false.
[JSClassGenerator("PlainDateTime", Register = false)]
public partial class JSTemporalPlainDateTime : JSObject
{
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
    internal JSValue WithCore(in Arguments a, Action afterNanosecond, Func<TemporalOverflow> overflowReader, Func<bool> hasExtraField)
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

        var overflow = overflowReader != null ? overflowReader() : TemporalAdapter.ReadOverflow(a.GetAt(1));

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
        var overflow = TemporalAdapter.ReadOverflow(options);

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
    public JSValue Until(in Arguments a) => Difference(a.GetAt(0), a.GetAt(1), DifferenceOperation.Until);

    [JSExport("since", Length = 1)]
    public JSValue Since(in Arguments a) => Difference(a.GetAt(0), a.GetAt(1), DifferenceOperation.Since);

    // DifferenceTemporalPlainDateTime (s5.5.15): ToTemporalDateTime, the calendars equal, GetDifferenceSettings' reads and
    // rules, then the shared library's difference: DifferenceISODateTime with its borrowed day, the calendar's DateUntil,
    // then DifferencePlainDateTimeWithRounding relative to the receiver, in every calendar (row F4), negated for since.
    private JSValue Difference(JSValue other, JSValue options, DifferenceOperation operation)
    {
        const string typeName = "Temporal.PlainDateTime";
        var target = RequireDateTime(ToTemporalDateTime(other));
        if (calendarId != target.calendarId)
            throw JSEngine.NewRangeError("Temporal.PlainDateTime: cannot compute the difference between date-times of different calendars");

        var settings = TemporalAdapter.DifferenceSettings(TemporalType.PlainDateTime, operation, options, typeName);
        return TemporalAdapter.DurationOf(TemporalDifferences.DifferenceTemporalPlainDateTime(operation, TemporalAdapter.Calendars,
            TemporalAdapter.CalendarIdOf(calendarId), IsoDateTime, target.IsoDateTime, settings, out var result), result, typeName);
    }

    [JSExport("round", Length = 1)]
    public JSValue Round(in Arguments a)
    {
        // The reads - roundingIncrement, roundingMode, the required smallestUnit - then the library's rule (a time unit
        // whose increment divides the next unit, or a day of increment 1), all before any rounding (test262
        // round/options-read-before-algorithmic-validation).
        var (increment, roundingMode, unit) = TemporalAdapter.RoundOptions(TemporalType.PlainDateTime, a.GetAt(0), "Temporal.PlainDateTime");

        // RoundISODateTime, the shared library's: the time rounded and its days carried into the date. The
        // result can pass the representable range (test262 round/limits), which is the RangeError.
        var rounded = IsoArithmetic.RoundIsoDateTime(IsoDateTime, increment, unit, roundingMode);
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
        var disambiguation = TemporalAdapter.ReadDisambiguation(a.GetAt(1));
        return JSTemporalZonedDateTime.FromLocal(isoYear, isoMonth, isoDay, hour, minute, second, millisecond, microsecond, nanosecond, tz.ToString(), calendarId, disambiguation);
    }

    // ── helpers ─────────────────────────────────────────────────────────────────

    internal JSTemporalPlainDateTime Clone()
        => new JSTemporalPlainDateTime(isoYear, isoMonth, isoDay, hour, minute, second, millisecond, microsecond, nanosecond, calendarId, PlainDateTimePrototype);

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
        Action afterNanosecond = null, Action afterSecond = null, Func<TemporalOverflow> overflowReader = null)
    {
        if (item is JSTemporalPlainDateTime dt)
        {
            TemporalAdapter.ReadOverflow(options);
            return dt.Clone();
        }

        if (item is JSTemporalPlainDate d)
        {
            TemporalAdapter.ReadOverflow(options);
            return new JSTemporalPlainDateTime(d.isoYear, d.isoMonth, d.isoDay, 0, 0, 0, 0, 0, 0, d.calendarId, PlainDateTimePrototype);
        }

        if (item.IsString)
        {
            var parsed = ParseTemporalDateTimeString(item.ToString());
            TemporalAdapter.ReadOverflow(options);
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

        var overflow = overflowReader != null ? overflowReader() : TemporalAdapter.ReadOverflow(options);

        var date = TemporalAdapter.DateFromFields(calendarId, fields.Fields, overflow, "Temporal.PlainDateTime");
        return RegulateDateTime(date, hour, minute, second, millisecond, microsecond, nanosecond, overflow, calendarId);
    }

    // The date the calendar resolved, then RegulateTime and ISODateTimeWithinLimits: the shared library's.
    private static JSValue RegulateDateTime(IsoDate date, int h, int mi, int s, int ms, int us, int ns, TemporalOverflow overflow, string calendarId)
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
}
