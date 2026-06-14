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

// Temporal.PlainTime (Temporal proposal §4): a wall-clock time of day with no date, calendar,
// or time zone — six integer fields (hour…nanosecond). Everything here is calendar- and
// zone-independent, so the full surface (construction, from/compare, with/add/subtract/
// until/since/round/equals, ISO string round-tripping) is implementable. Arithmetic wraps
// modulo 24 h (the day overflow is discarded, since a PlainTime has no date).
//
// Registered under the Temporal namespace (not as a global) via Register = false.
[JSClassGenerator("PlainTime", Register = false)]
public partial class JSTemporalPlainTime : JSObject
{
    private const long NanosecondsPerDay = 86_400_000_000_000;

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

        if (!IsValidTime(hour, minute, second, millisecond, microsecond, nanosecond))
            throw JSEngine.NewRangeError("Temporal.PlainTime: time component out of range");
    }

    // Factory used by sibling Temporal types (PlainDateTime.toPlainTime) to build a
    // PlainTime from already-validated components without going through `new`.
    internal static JSTemporalPlainTime Create(int h, int mi, int s, int ms, int us, int ns)
        => new JSTemporalPlainTime(
            ((((long)h * 60 + mi) * 60 + s) * 1000L + ms) * 1_000_000L + (long)us * 1000 + ns,
            PlainTimePrototype);

    private JSTemporalPlainTime(long totalNanoseconds, JSObject prototype) : base(prototype)
    {
        nanosecond = (int)(totalNanoseconds % 1000); totalNanoseconds /= 1000;
        microsecond = (int)(totalNanoseconds % 1000); totalNanoseconds /= 1000;
        millisecond = (int)(totalNanoseconds % 1000); totalNanoseconds /= 1000;
        second = (int)(totalNanoseconds % 60); totalNanoseconds /= 60;
        minute = (int)(totalNanoseconds % 60); totalNanoseconds /= 60;
        hour = (int)totalNanoseconds;
    }

    private static JSObject ResolvePrototype()
    {
        if (JSEngine.NewTarget == null && (JSEngine.Current as IJSExecutionContext)?.CurrentNewTarget == null)
            throw JSEngine.NewTypeError("Constructor Temporal.PlainTime requires 'new'");

        return JSEngine.NewTargetPrototype ?? PlainTimePrototype;
    }

    internal static JSObject PlainTimePrototype
    {
        get
        {
            var temporal = (JSEngine.Current as JSObject)?[KeyStrings.GetOrCreate("Temporal")] as JSObject;
            return (temporal?[KeyStrings.GetOrCreate("PlainTime")] as JSFunction)?.prototype;
        }
    }

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
        var overflow = ReadOverflow(a.GetAt(1));

        if (item is JSTemporalPlainTime t)
            return new JSTemporalPlainTime(t.TotalNanoseconds(), PlainTimePrototype);

        return ToTemporalTime(item, overflow);
    }

    [JSExport("compare", Length = 2)]
    internal static JSValue Compare(in Arguments a)
    {
        var one = RequireTime(ToTemporalTime(a.GetAt(0), "constrain"));
        var two = RequireTime(ToTemporalTime(a.GetAt(1), "constrain"));
        var n1 = one.TotalNanoseconds();
        var n2 = two.TotalNanoseconds();
        return new JSNumber(n1 < n2 ? -1 : n1 > n2 ? 1 : 0);
    }

    // ── methods ─────────────────────────────────────────────────────────────────

    [JSExport("with", Length = 1)]
    public JSValue With(in Arguments a)
    {
        if (a.GetAt(0) is not JSObject obj)
            throw JSEngine.NewTypeError("Temporal.PlainTime.prototype.with requires an object");

        var overflow = ReadOverflow(a.GetAt(1));

        var any = false;
        int Read(string name, int current)
        {
            var v = obj[KeyStrings.GetOrCreate(name)];
            if (v.IsUndefined) return current;
            any = true;
            return ToIntegerWithTruncation(v);
        }

        var h = Read("hour", hour);
        var mi = Read("minute", minute);
        var s = Read("second", second);
        var ms = Read("millisecond", millisecond);
        var us = Read("microsecond", microsecond);
        var ns = Read("nanosecond", nanosecond);

        if (!any)
            throw JSEngine.NewTypeError("Temporal.PlainTime.prototype.with requires at least one time property");

        return RegulateTime(h, mi, s, ms, us, ns, overflow);
    }

    [JSExport("add", Length = 1)]
    public JSValue Add(in Arguments a) => AddDuration(a.GetAt(0), 1);

    [JSExport("subtract", Length = 1)]
    public JSValue Subtract(in Arguments a) => AddDuration(a.GetAt(0), -1);

    private JSValue AddDuration(JSValue durationLike, int sign)
    {
        var d = (JSTemporalDuration)JSTemporalDuration.ToTemporalDuration(durationLike);

        // Only the time components participate; years/months/weeks/days are ignored because
        // a PlainTime has no date, and any 24 h overflow wraps.
        var durationNs = new BigInteger(d.HoursValue) * 3_600_000_000_000
            + new BigInteger(d.MinutesValue) * 60_000_000_000
            + new BigInteger(d.SecondsValue) * 1_000_000_000
            + new BigInteger(d.MillisecondsValue) * 1_000_000
            + new BigInteger(d.MicrosecondsValue) * 1_000
            + new BigInteger(d.NanosecondsValue);

        var total = TotalNanoseconds() + durationNs * sign;
        var wrapped = (long)(((total % NanosecondsPerDay) + NanosecondsPerDay) % NanosecondsPerDay);
        return new JSTemporalPlainTime(wrapped, PlainTimePrototype);
    }

    [JSExport("until", Length = 1)]
    public JSValue Until(in Arguments a) { ReadDifferenceSettings(a.GetAt(1)); return Difference(a.GetAt(0), 1); }

    [JSExport("since", Length = 1)]
    public JSValue Since(in Arguments a) { ReadDifferenceSettings(a.GetAt(1)); return Difference(a.GetAt(0), -1); }

    // GetDifferenceSettings for until/since: validates largestUnit / smallestUnit (time units only —
    // a calendar unit is a RangeError, and smallestUnit must not be larger than largestUnit),
    // roundingIncrement (finite integer in 1 … the unit maximum, dividing it evenly) and
    // roundingMode. (The options are validated here but the difference still uses largestUnit "hour"
    // with no rounding applied — see the TODO above Difference.)
    private static void ReadDifferenceSettings(JSValue options)
    {
        if (options == null || options.IsUndefined) return;
        if (options is not JSObject o)
            throw JSEngine.NewTypeError("Temporal options must be an object or undefined");

        var largestRaw = o[KeyStrings.GetOrCreate("largestUnit")];
        string largestUnit = largestRaw.IsUndefined ? null : TemporalRoundingOptions.NormalizeTimeUnit(largestRaw.StringValue, allowAuto: true);
        if (largestUnit == "auto") largestUnit = null;

        var smallestRaw = o[KeyStrings.GetOrCreate("smallestUnit")];
        var smallestUnit = smallestRaw.IsUndefined ? "nanosecond" : TemporalRoundingOptions.NormalizeTimeUnit(smallestRaw.StringValue, allowAuto: false);

        var increment = TemporalRoundingOptions.GetRoundingIncrement(o);
        TemporalRoundingOptions.GetRoundingMode(o, "trunc");

        largestUnit ??= TemporalRoundingOptions.UnitIndex(smallestUnit) < TemporalRoundingOptions.UnitIndex("hour")
            ? smallestUnit : "hour";
        if (TemporalRoundingOptions.UnitIndex(smallestUnit) < TemporalRoundingOptions.UnitIndex(largestUnit))
            throw JSEngine.NewRangeError("Temporal.PlainTime: smallestUnit must not be larger than largestUnit");

        TemporalRoundingOptions.ValidateRoundingIncrement(increment, MaximumRoundingIncrement(smallestUnit), inclusive: false);
    }

    // MaximumTemporalDurationRoundingIncrement for a time unit.
    private static long MaximumRoundingIncrement(string unit) => unit switch
    {
        "hour" => 24,
        "minute" or "second" => 60,
        _ => 1000, // millisecond / microsecond / nanosecond
    };

    private JSValue Difference(JSValue other, int sign)
    {
        var target = RequireTime(ToTemporalTime(other, "constrain"));
        var diff = (target.TotalNanoseconds() - TotalNanoseconds()) * sign;

        var s = Math.Sign(diff);
        var abs = Math.Abs(diff);

        var ns = abs % 1000; abs /= 1000;
        var us = abs % 1000; abs /= 1000;
        var ms = abs % 1000; abs /= 1000;
        var sec = abs % 60; abs /= 60;
        var min = abs % 60; abs /= 60;
        var hr = abs;

        return new JSTemporalDuration(0, 0, 0, 0, s * hr, s * min, s * sec, s * ms, s * us, s * ns,
            JSTemporalDuration.DurationPrototype);
    }

    [JSExport("round", Length = 1)]
    public JSValue Round(in Arguments a)
    {
        var (unit, increment, roundingMode) = ReadRoundTo(a.GetAt(0));
        var unitNs = UnitNanoseconds(unit) * increment;
        var rounded = (long)TemporalRoundingOptions.RoundToIncrement(TotalNanoseconds(), unitNs, roundingMode) % NanosecondsPerDay;
        if (rounded < 0) rounded += NanosecondsPerDay;
        return new JSTemporalPlainTime(rounded, PlainTimePrototype);
    }

    [JSExport("equals", Length = 1)]
    public JSValue Equals(in Arguments a)
    {
        var other = RequireTime(ToTemporalTime(a.GetAt(0), "constrain"));
        return TotalNanoseconds() == other.TotalNanoseconds() ? JSValue.BooleanTrue : JSValue.BooleanFalse;
    }

    [JSExport("toString", Length = 0)]
    public JSValue ToStringMethod(in Arguments a)
    {
        var options = a.GetAt(0);
        if (options == null || options.IsUndefined)
            return new JSString(ToISOString());
        if (options is not JSObject o)
            throw JSEngine.NewTypeError("Temporal options must be an object or undefined");

        // Option-reading order per TemporalTimeToString: fractionalSecondDigits, roundingMode, then
        // smallestUnit.
        var digits = TemporalRoundingOptions.GetFractionalSecondDigits(o);
        var roundingMode = TemporalRoundingOptions.GetRoundingMode(o, "trunc");

        var smallestRaw = o[KeyStrings.GetOrCreate("smallestUnit")];
        string smallestUnit = null;
        if (!smallestRaw.IsUndefined)
        {
            smallestUnit = TemporalRoundingOptions.NormalizeTimeUnit(smallestRaw.StringValue, allowAuto: false);
            if (smallestUnit == "hour")
                throw JSEngine.NewRangeError("Temporal.PlainTime.toString: smallestUnit \"hour\" is not allowed");
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
            return new JSString(ToISOString());
        }
        else
        {
            fracDigits = digits;
            incrementNs = TemporalRoundingOptions.Pow10(9 - digits);
        }

        // Round the time-of-day to the increment; an overflow past midnight wraps (PlainTime has no
        // date component to carry into).
        var wrapped = TotalNanoseconds();
        if (incrementNs > 1)
        {
            wrapped = (long)TemporalRoundingOptions.RoundToIncrement(TotalNanoseconds(), incrementNs, roundingMode) % NanosecondsPerDay;
            if (wrapped < 0) wrapped += NanosecondsPerDay;
        }

        return new JSString(FormatTime(wrapped, fracDigits, minutePrecision));
    }

    // TemporalTimeToString with an explicit precision: "HH:MM" (minute precision) or "HH:MM:SS" plus a
    // fixed number of fractional-second digits.
    private static string FormatTime(long ns, int fracDigits, bool minutePrecision)
    {
        var totalSeconds = ns / 1_000_000_000;
        var fraction = ns % 1_000_000_000;
        var h = (int)(totalSeconds / 3600);
        var mi = (int)(totalSeconds / 60 % 60);
        var s = (int)(totalSeconds % 60);

        var sb = new StringBuilder();
        sb.Append(h.ToString("00", CultureInfo.InvariantCulture))
          .Append(':').Append(mi.ToString("00", CultureInfo.InvariantCulture));
        if (minutePrecision) return sb.ToString();

        sb.Append(':').Append(s.ToString("00", CultureInfo.InvariantCulture));
        if (fracDigits > 0)
            sb.Append('.').Append(fraction.ToString("000000000", CultureInfo.InvariantCulture).Substring(0, fracDigits));
        return sb.ToString();
    }

    [JSExport("toJSON", Length = 0)]
    public JSValue ToJSON(in Arguments a) => new JSString(ToISOString());

    [JSExport("toLocaleString", Length = 0)]
    public JSValue ToLocaleString(in Arguments a)
        => Intl.JSIntlDateTimeFormat.TemporalToLocaleString(this, a.GetAt(0), a.GetAt(1));

    [JSExport("valueOf", Length = 0)]
    public JSValue ValueOf(in Arguments a)
        => throw JSEngine.NewTypeError("Called Temporal.PlainTime.prototype.valueOf, which is not supported. Use Temporal.PlainTime.compare for comparison.");

    // toPlainDateTime combines this time with a PlainDate argument.
    [JSExport("toPlainDateTime", Length = 1)]
    public JSValue ToPlainDateTime(in Arguments a)
    {
        var d = JSTemporalPlainDate.From(new Arguments(JSUndefined.Value, a.GetAt(0))) as JSTemporalPlainDate
            ?? throw JSEngine.NewTypeError("expected a Temporal.PlainDate");
        return new JSTemporalPlainDateTime(d.isoYear, d.isoMonth, d.isoDay,
            hour, minute, second, millisecond, microsecond, nanosecond, JSTemporalPlainDateTime.PlainDateTimePrototype);
    }

    // toZonedDateTime({ plainDate, timeZone }): combine this time with a date and interpret
    // it in the given zone.
    [JSExport("toZonedDateTime", Length = 1)]
    public JSValue ToZonedDateTime(in Arguments a)
    {
        if (a.GetAt(0) is not JSObject obj)
            throw JSEngine.NewTypeError("Temporal.PlainTime.prototype.toZonedDateTime requires an object");

        var tzValue = obj[KeyStrings.GetOrCreate("timeZone")];
        if (tzValue.IsUndefined || !tzValue.IsString)
            throw JSEngine.NewTypeError("Temporal.PlainTime.prototype.toZonedDateTime: timeZone must be a string");

        var plainDateValue = obj[KeyStrings.GetOrCreate("plainDate")];
        var d = JSTemporalPlainDate.From(new Arguments(JSUndefined.Value, plainDateValue)) as JSTemporalPlainDate
            ?? throw JSEngine.NewTypeError("expected a Temporal.PlainDate");

        return JSTemporalZonedDateTime.FromLocal(d.isoYear, d.isoMonth, d.isoDay,
            hour, minute, second, millisecond, microsecond, nanosecond, tzValue.ToString());
    }

    // ── helpers ─────────────────────────────────────────────────────────────────

    internal long TotalNanoseconds()
        => ((((long)hour * 60 + minute) * 60 + second) * 1000L + millisecond) * 1000L * 1000L
           + (long)microsecond * 1000 + nanosecond;

    private static JSTemporalPlainTime RequireTime(JSValue value)
        => value as JSTemporalPlainTime ?? throw JSEngine.NewTypeError("expected a Temporal.PlainTime");

    private static int ToIntegerWithTruncation(JSValue value)
    {
        if (value == null || value.IsUndefined)
            return 0;

        var number = value.DoubleValue; // ToNumber (throws TypeError for BigInt/Symbol)
        if (double.IsNaN(number) || double.IsInfinity(number))
            throw JSEngine.NewRangeError("Temporal.PlainTime: time component must be finite");

        return (int)Math.Truncate(number);
    }

    private static bool IsValidTime(int h, int mi, int s, int ms, int us, int ns)
        => h is >= 0 and <= 23 && mi is >= 0 and <= 59 && s is >= 0 and <= 59
           && ms is >= 0 and <= 999 && us is >= 0 and <= 999 && ns is >= 0 and <= 999;

    private static string ReadOverflow(JSValue options)
    {
        if (options.IsUndefined)
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

    // RegulateTime: either constrain each component into range, or reject an out-of-range value.
    private static JSValue RegulateTime(int h, int mi, int s, int ms, int us, int ns, string overflow)
    {
        if (overflow == "reject")
        {
            if (!IsValidTime(h, mi, s, ms, us, ns))
                throw JSEngine.NewRangeError("Temporal.PlainTime: time component out of range");
        }
        else
        {
            h = Math.Clamp(h, 0, 23);
            mi = Math.Clamp(mi, 0, 59);
            s = Math.Clamp(s, 0, 59);
            ms = Math.Clamp(ms, 0, 999);
            us = Math.Clamp(us, 0, 999);
            ns = Math.Clamp(ns, 0, 999);
        }

        var total = ((((long)h * 60 + mi) * 60 + s) * 1000L + ms) * 1000L * 1000L + (long)us * 1000 + ns;
        return new JSTemporalPlainTime(total, PlainTimePrototype);
    }

    private static JSValue ToTemporalTime(JSValue item, string overflow)
    {
        if (item is JSTemporalPlainTime t)
            return new JSTemporalPlainTime(t.TotalNanoseconds(), PlainTimePrototype);

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

        var h = Field("hour"); var mi = Field("minute"); var s = Field("second");
        var ms = Field("millisecond"); var us = Field("microsecond"); var ns = Field("nanosecond");

        if (!any)
            throw JSEngine.NewTypeError("Temporal.PlainTime: object has no time properties");

        return RegulateTime(h, mi, s, ms, us, ns, overflow);
    }

    private static readonly Regex TimePattern = new(
        @"^(?:(?:\d{4}|\+\d{6}|-(?!000000)\d{6})-\d{2}-\d{2})?[Tt ]?(\d{2})(?::?(\d{2})(?::?(\d{2})(?:[.,](\d{1,9}))?)?)?(?:[Zz]|[+-]\d{2}:?\d{2})?$",
        RegexOptions.CultureInvariant);

    private static JSValue ParseTemporalTimeString(string text)
    {
        TemporalIsoString.RejectInvalidAnnotations(text);

        var match = TimePattern.Match(text);
        if (!match.Success)
            throw JSEngine.NewRangeError($"Cannot parse Temporal.PlainTime from \"{text}\"");

        var h = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        var mi = match.Groups[2].Success ? int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture) : 0;
        var s = match.Groups[3].Success ? int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture) : 0;

        int ms = 0, us = 0, ns = 0;
        if (match.Groups[4].Success)
        {
            var digits = match.Groups[4].Value.PadRight(9, '0');
            ms = int.Parse(digits.Substring(0, 3), CultureInfo.InvariantCulture);
            us = int.Parse(digits.Substring(3, 3), CultureInfo.InvariantCulture);
            ns = int.Parse(digits.Substring(6, 3), CultureInfo.InvariantCulture);
        }

        // A time string is parsed strictly (no constrain); 24:00 etc. is invalid.
        if (!IsValidTime(h, mi, s, ms, us, ns))
            throw JSEngine.NewRangeError($"Cannot parse Temporal.PlainTime from \"{text}\"");

        return RegulateTime(h, mi, s, ms, us, ns, "reject");
    }

    private static (string unit, int increment, string roundingMode) ReadRoundTo(JSValue roundTo)
    {
        string smallestUnit;
        var increment = 1;
        var roundingMode = "halfExpand";

        if (roundTo.IsString)
        {
            smallestUnit = roundTo.ToString();
        }
        else if (roundTo is JSObject obj)
        {
            // GetRoundingIncrementOption and GetRoundingModeOption are read before the (required)
            // smallestUnit, matching the spec's option-reading order.
            increment = TemporalRoundingOptions.GetRoundingIncrement(obj);
            roundingMode = TemporalRoundingOptions.GetRoundingMode(obj, "halfExpand");

            var unitValue = obj[KeyStrings.GetOrCreate("smallestUnit")];
            if (unitValue.IsUndefined)
                throw JSEngine.NewRangeError("Temporal.PlainTime.round requires a smallestUnit");
            smallestUnit = unitValue.StringValue;
        }
        else
        {
            throw JSEngine.NewTypeError("Temporal.PlainTime.round requires an options object or string");
        }

        smallestUnit = TemporalRoundingOptions.NormalizeTimeUnit(smallestUnit, allowAuto: false);

        // ValidateTemporalRoundingIncrement: the increment must divide the next unit evenly and not
        // reach it (e.g. round to "hour" allows 1,2,3,4,6,8,12 but not 24).
        TemporalRoundingOptions.ValidateRoundingIncrement(increment, MaximumRoundingIncrement(smallestUnit), inclusive: false);

        return (smallestUnit, increment, roundingMode);
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

    // TemporalTimeToString with auto precision: "HH:MM:SS" plus the trimmed fractional second.
    private string ToISOString()
    {
        var sb = new StringBuilder();
        sb.Append(hour.ToString("00", CultureInfo.InvariantCulture))
          .Append(':').Append(minute.ToString("00", CultureInfo.InvariantCulture))
          .Append(':').Append(second.ToString("00", CultureInfo.InvariantCulture));

        var fraction = millisecond * 1_000_000 + microsecond * 1_000 + nanosecond;
        if (fraction != 0)
        {
            var digits = fraction.ToString("000000000", CultureInfo.InvariantCulture).TrimEnd('0');
            sb.Append('.').Append(digits);
        }

        return sb.ToString();
    }
}
