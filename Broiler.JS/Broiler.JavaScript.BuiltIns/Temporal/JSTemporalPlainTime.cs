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

// Temporal.PlainTime (Temporal proposal §4): a wall-clock time of day with no date, calendar,
// or time zone — six integer fields (hour…nanosecond). Its regulation, addition, rounding and
// differences are the shared library's, through TemporalAdapter. Arithmetic wraps modulo 24 h (the
// day overflow is discarded, since a PlainTime has no date).
//
// Registered under the Temporal namespace (not as a global) via Register = false.
[JSClassGenerator("PlainTime", Register = false)]
public partial class JSTemporalPlainTime : JSObject
{
    internal readonly int hour, minute, second, millisecond, microsecond, nanosecond;

    [JSExport(Length = 0)]
    public JSTemporalPlainTime(in Arguments a) : base(ResolvePrototype())
    {
        hour = ToIntegerWithTruncation(a.GetAt(0));
        minute = ToIntegerWithTruncation(a.GetAt(1));
        second = ToIntegerWithTruncation(a.GetAt(2));
        millisecond = ToIntegerWithTruncation(a.GetAt(3));
        microsecond = ToIntegerWithTruncation(a.GetAt(4));
        nanosecond = ToIntegerWithTruncation(a.GetAt(5));

        if (!TemporalLimits.IsValidTime(IsoTime))
            throw JSEngine.NewRangeError("Temporal.PlainTime: time component out of range");
    }

    // Factory used by sibling Temporal types (PlainDateTime.toPlainTime) to build a
    // PlainTime from already-validated components without going through `new`.
    internal static JSTemporalPlainTime Create(int h, int mi, int s, int ms, int us, int ns)
        => new JSTemporalPlainTime(new IsoTime(h, mi, s, ms, us, ns), PlainTimePrototype);

    private JSTemporalPlainTime(IsoTime time, JSObject prototype) : base(prototype)
    {
        hour = time.Hour; minute = time.Minute; second = time.Second;
        millisecond = time.Millisecond; microsecond = time.Microsecond; nanosecond = time.Nanosecond;
    }

    // The slots as the shared library's record.
    internal IsoTime IsoTime => new(hour, minute, second, millisecond, microsecond, nanosecond);

    private static JSObject ResolvePrototype()
    {
        if (JSEngine.NewTarget == null && (JSEngine.Current as IJSExecutionContext)?.CurrentNewTarget == null)
            throw JSEngine.NewTypeError("Constructor Temporal.PlainTime requires 'new'");

        return JSEngine.NewTargetPrototype ?? PlainTimePrototype;
    }

    private static readonly KeyString PlainTimeIntrinsicKey = KeyStrings.GetOrCreate("Temporal.PlainTime");

    // %Temporal.PlainTime.prototype% of the current realm, never the prototype of whatever
    // `globalThis.Temporal.PlainTime` holds now: guest code may replace the namespace or its members.
    internal static JSObject PlainTimePrototype => Intrinsics.TemporalPrototype(PlainTimeIntrinsicKey);

    // ── accessors ───────────────────────────────────────────────────────────────

    [JSExport("hour")] public double HourValue => hour;
    [JSExport("minute")] public double MinuteValue => minute;
    [JSExport("second")] public double SecondValue => second;
    [JSExport("millisecond")] public double MillisecondValue => millisecond;
    [JSExport("microsecond")] public double MicrosecondValue => microsecond;
    [JSExport("nanosecond")] public double NanosecondValue => nanosecond;

    // ── statics ─────────────────────────────────────────────────────────────────

    [JSExport("from", Length = 1)]
    internal static JSValue From(in Arguments a)
    {
        var item = a.GetAt(0);
        var options = a.GetAt(1);

        if (item is JSTemporalPlainTime t)
        {
            TemporalAdapter.ReadOverflow(options); // GetTemporalOverflowOption still validates the options bag.
            return new JSTemporalPlainTime(t.IsoTime, PlainTimePrototype);
        }

        // ParseISODateTime runs before the options are validated, so an invalid string — e.g. one
        // carrying a UTC (Z) designator — is a RangeError even when options is a bad (non-object) type.
        if (item.IsString)
        {
            var parsed = ParseTemporalTimeString(item.ToString());
            TemporalAdapter.ReadOverflow(options);
            return parsed;
        }

        // The recognized time fields are read (alphabetically) before the overflow
        // option, so the option getter runs last (test262 PlainTime/from
        // order-of-operations).
        return ToTemporalTime(item, () => TemporalAdapter.ReadOverflow(options));
    }

    [JSExport("compare", Length = 2)]
    internal static JSValue Compare(in Arguments a)
    {
        var one = RequireTime(ToTemporalTime(a.GetAt(0), TemporalOverflow.Constrain));
        var two = RequireTime(ToTemporalTime(a.GetAt(1), TemporalOverflow.Constrain));
        return new JSNumber(IsoArithmetic.CompareTime(one.IsoTime, two.IsoTime));
    }

    // ── methods ─────────────────────────────────────────────────────────────────

    [JSExport("with", Length = 1)]
    public JSValue With(in Arguments a)
    {
        if (a.GetAt(0) is not JSObject obj)
            throw JSEngine.NewTypeError("Temporal.PlainTime.prototype.with requires an object");

        // IsPartialTemporalObject reads (and rejects) calendar / timeZone before the time
        // fields, which ToTemporalTimeRecord then reads in alphabetical order — hour,
        // microsecond, millisecond, minute, nanosecond, second — coercing each as read;
        // the overflow option is read last (test262 PlainTime/prototype/with order-of-operations).
        TemporalAdapter.RejectObjectWithCalendarOrTimeZone(obj);

        var any = false;
        int Read(string name, int current)
        {
            var v = obj[KeyStrings.GetOrCreate(name)];
            if (v.IsUndefined) return current;
            any = true;
            return ToIntegerWithTruncation(v);
        }

        var h = Read("hour", hour);
        var us = Read("microsecond", microsecond);
        var ms = Read("millisecond", millisecond);
        var mi = Read("minute", minute);
        var ns = Read("nanosecond", nanosecond);
        var s = Read("second", second);

        if (!any)
            throw JSEngine.NewTypeError("Temporal.PlainTime.prototype.with requires at least one time property");

        var overflow = TemporalAdapter.ReadOverflow(a.GetAt(1));

        return RegulateTime(h, mi, s, ms, us, ns, overflow);
    }

    [JSExport("add", Length = 1)]
    public JSValue Add(in Arguments a) => AddDuration(a.GetAt(0), 1);

    [JSExport("subtract", Length = 1)]
    public JSValue Subtract(in Arguments a) => AddDuration(a.GetAt(0), -1);

    private JSValue AddDuration(JSValue durationLike, int sign)
    {
        var d = (JSTemporalDuration)JSTemporalDuration.ToTemporalDuration(durationLike);

        // AddDurationToTime: only the time components participate (years/months/weeks/days are
        // ignored because a PlainTime has no date), added by the shared library's AddTime, whose
        // carried days are dropped.
        return new JSTemporalPlainTime(IsoArithmetic.AddTime(IsoTime, sign * TemporalAdapter.TimeDuration(d), out _), PlainTimePrototype);
    }

    [JSExport("until", Length = 1)]
    public JSValue Until(in Arguments a) => Difference(a.GetAt(0), a.GetAt(1), DifferenceOperation.Until);

    [JSExport("since", Length = 1)]
    public JSValue Since(in Arguments a) => Difference(a.GetAt(0), a.GetAt(1), DifferenceOperation.Since);

    // DifferenceTemporalPlainTime (s4.5.17): ToTemporalTime, then GetDifferenceSettings' reads and rules (time units
    // only), then the shared library's difference: the time difference rounded to the smallest unit and increment and
    // balanced up to the largest unit, negated for since.
    private JSValue Difference(JSValue other, JSValue options, DifferenceOperation operation)
    {
        const string typeName = "Temporal.PlainTime";
        var target = RequireTime(ToTemporalTime(other, TemporalOverflow.Constrain));
        var settings = TemporalAdapter.DifferenceSettings(TemporalType.PlainTime, operation, options, typeName);
        return TemporalAdapter.DurationOf(TemporalDifferences.DifferenceTemporalPlainTime(operation, IsoTime, target.IsoTime, settings, out var result),
            result, typeName);
    }

    [JSExport("round", Length = 1)]
    public JSValue Round(in Arguments a)
    {
        // The reads - roundingIncrement, roundingMode, the required smallestUnit - then the library's rule: a time unit
        // whose increment divides the next unit and does not reach it (round to hours allows 1, 2, 3, 4, 6, 8 and 12).
        var (increment, roundingMode, unit) = TemporalAdapter.RoundOptions(TemporalType.PlainTime, a.GetAt(0), "Temporal.PlainTime");

        // RoundTime, the shared library's; a time rounded up to midnight wraps (a PlainTime has no date).
        return new JSTemporalPlainTime(IsoArithmetic.RoundTime(IsoTime, increment, unit, roundingMode, out _), PlainTimePrototype);
    }

    [JSExport("equals", Length = 1)]
    public JSValue Equals(in Arguments a)
    {
        var other = RequireTime(ToTemporalTime(a.GetAt(0), TemporalOverflow.Constrain));
        return IsoArithmetic.CompareTime(IsoTime, other.IsoTime) == 0 ? BooleanTrue : BooleanFalse;
    }

    [JSExport("toString", Length = 0)]
    public JSValue ToStringMethod(in Arguments a)
    {
        var options = a.GetAt(0);
        if (options == null || options.IsUndefined)
            return ToJSON(a);
        if (options is not JSObject o)
            throw JSEngine.NewTypeError("Temporal options must be an object or undefined");

        // The options are read in alphabetical order — fractionalSecondDigits, roundingMode, smallestUnit — each
        // validated by its rule as it is read, the unit's group after the last read.
        var digits = TemporalAdapter.ReadFractionalSecondDigits(o);
        var roundingMode = TemporalRoundingOptions.GetRoundingMode(o, RoundingMode.Trunc);
        var precision = TemporalAdapter.ToStringPrecision(TemporalAdapter.ReadSmallestUnitText(o), digits, duration: false, "Temporal.PlainTime");

        // RoundTime then TimeRecordToString, the shared library's: a time rounded up to midnight wraps (a
        // PlainTime has no date to carry into).
        return new JSString(TemporalFormat.PlainTimeToString(IsoTime, precision, roundingMode));
    }

    [JSExport("toJSON", Length = 0)]
    public JSValue ToJSON(in Arguments a) => new JSString(TemporalFormat.TimeToString(IsoTime, TemporalFormat.AutoPrecision));

    [JSExport("toLocaleString", Length = 0)]
    public JSValue ToLocaleString(in Arguments a)
        => Intl.JSIntlDateTimeFormat.TemporalToLocaleString(this, a.GetAt(0), a.GetAt(1));

    [JSExport("valueOf", Length = 0)]
    public JSValue ValueOf(in Arguments a)
        => throw JSEngine.NewTypeError("Called Temporal.PlainTime.prototype.valueOf, which is not supported. Use Temporal.PlainTime.compare for comparison.");

    // Note: Temporal.PlainTime.prototype.toPlainDateTime and toZonedDateTime were removed
    // from the Temporal proposal (June 2024 TC39 consensus). Build a PlainDateTime or
    // ZonedDateTime from the date side instead. See test/staging/Temporal/removed-methods.js.

    // ── helpers ─────────────────────────────────────────────────────────────────

    private static JSTemporalPlainTime RequireTime(JSValue value)
        => value as JSTemporalPlainTime ?? throw JSEngine.NewTypeError("expected a Temporal.PlainTime");

    // ToTemporalTime for callers in other Temporal types (e.g. ZonedDateTime.withPlainTime). Accepts
    // a PlainTime, a property bag, or a time string and returns a PlainTime instance.
    internal static JSTemporalPlainTime ToTemporalTimeObject(JSValue item)
        => RequireTime(ToTemporalTime(item, TemporalOverflow.Constrain));

    private static int ToIntegerWithTruncation(JSValue value)
    {
        if (value == null || value.IsUndefined)
            return 0;

        return (int)TemporalRoundingOptions.ToIntegerWithTruncation(value, "Temporal.PlainTime: time component must be finite");
    }

    // RegulateTime, the shared library's: constrain each component into range, or reject an out-of-range value.
    private static JSValue RegulateTime(int h, int mi, int s, int ms, int us, int ns, TemporalOverflow overflow)
        => new JSTemporalPlainTime(
            TemporalAdapter.RegulateTime(h, mi, s, ms, us, ns, overflow, "Temporal.PlainTime: time component out of range"), PlainTimePrototype);

    private static JSValue ToTemporalTime(JSValue item, TemporalOverflow overflow)
        => ToTemporalTime(item, () => overflow);

    // The overflow option is supplied as a callback so a caller (PlainTime.from) can
    // defer reading it until *after* the time fields have been read, matching the spec
    // order; callers with a fixed overflow (constrain) pass a constant.
    private static JSValue ToTemporalTime(JSValue item, Func<TemporalOverflow> readOverflow)
    {
        if (item is JSTemporalPlainTime t)
        {
            // GetTemporalOverflowOption is still performed (and the options bag validated) for
            // every source kind, including those whose time comes from internal slots — only the
            // property-bag form reads time fields (test262 PlainTime/from order-of-operations).
            readOverflow();
            return new JSTemporalPlainTime(t.IsoTime, PlainTimePrototype);
        }

        // ToTemporalTime fast path: a PlainDateTime carries its time in internal slots, so
        // the time components are read directly rather than through observable property
        // getters (test262 PlainTime/from/argument-plaindatetime checks no getters run).
        if (item is JSTemporalPlainDateTime pdt)
        {
            var pdtResult = Create(pdt.hour, pdt.minute, pdt.second, pdt.millisecond, pdt.microsecond, pdt.nanosecond);
            readOverflow();
            return pdtResult;
        }

        // A ZonedDateTime is projected onto its wall-clock PlainTime via internal slots (no
        // observable getters), then the overflow option is read like every other source kind.
        if (item is JSTemporalZonedDateTime zdt)
        {
            var zdtResult = zdt.ToPlainTimeFromSlots();
            readOverflow();
            return zdtResult;
        }

        if (item.IsString)
            return ParseTemporalTimeString(item.ToString());

        if (item is not JSObject obj)
            throw JSEngine.NewTypeError("Temporal.PlainTime: invalid value");

        // ToTemporalTimeRecord: absent components default to 0, but at least one recognized
        // (singular) field must be present — an empty bag, or one with only unrecognized keys
        // like the plural "minutes", is a TypeError.
        var any = false;
        int Field(string name)
        {
            var v = obj[KeyStrings.GetOrCreate(name)];
            if (v.IsUndefined) return 0;
            any = true;
            return ToIntegerWithTruncation(v);
        }

        // ToTemporalTimeRecord reads the time fields in alphabetical order (each is read and coerced
        // immediately), so the property accesses are observed as hour, microsecond, millisecond,
        // minute, nanosecond, second.
        var h = Field("hour"); var us = Field("microsecond"); var ms = Field("millisecond");
        var mi = Field("minute"); var ns = Field("nanosecond"); var s = Field("second");

        if (!any)
            throw JSEngine.NewTypeError("Temporal.PlainTime: object has no time properties");

        return RegulateTime(h, mi, s, ms, us, ns, readOverflow());
    }

    // ParseTemporalTimeString: ParseISODateTime with the TemporalTimeString goal, the shared library's grammar. Its
    // early errors refuse a designator-less time that is also a year-month or a month-day, a Z (a PlainTime has no
    // time zone) and an offset out of range; a leap second (:60) reads as :59, and an offset's value is unused.
    private static JSValue ParseTemporalTimeString(string text)
        => new JSTemporalPlainTime(TemporalAdapter.ParseIsoString(text, [TemporalGoal.Time], "Temporal.PlainTime").Time!.Value, PlainTimePrototype);
}
