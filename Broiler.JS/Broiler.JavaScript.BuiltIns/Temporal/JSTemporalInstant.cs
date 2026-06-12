using System;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using Broiler.JavaScript.BuiltIns.BigInt;
using Broiler.JavaScript.BuiltIns.Function;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Temporal;

// Temporal.Instant (Temporal proposal §8): an exact point on the UTC timeline, stored as a
// signed integer count of nanoseconds since the epoch. Valid instants lie within ±10^8 days
// of the epoch (±8.64e21 ns). Registered under the Temporal namespace (Register = false).
[JSClassGenerator("Instant", Register = false)]
public partial class JSTemporalInstant : JSObject
{
    // ±10^8 days from epoch, in nanoseconds.
    private static readonly BigInteger MaxEpochNanoseconds = BigInteger.Parse("8640000000000000000000");
    private static readonly BigInteger MinEpochNanoseconds = -MaxEpochNanoseconds;

    internal readonly BigInteger epochNanoseconds;

    [JSExport(Length = 1)]
    public JSTemporalInstant(in Arguments a) : base(ResolvePrototype())
    {
        var ns = JSBigInt.Coerce(a.GetAt(0) ?? JSUndefined.Value).value;
        if (!IsValid(ns))
            throw JSEngine.NewRangeError("Temporal.Instant: epoch nanoseconds out of range");

        epochNanoseconds = ns;
    }

    internal JSTemporalInstant(BigInteger epochNanoseconds, JSObject prototype) : base(prototype)
        => this.epochNanoseconds = epochNanoseconds;

    private static JSObject ResolvePrototype()
    {
        if (JSEngine.NewTarget == null && (JSEngine.Current as IJSExecutionContext)?.CurrentNewTarget == null)
            throw JSEngine.NewTypeError("Constructor Temporal.Instant requires 'new'");

        return JSEngine.NewTargetPrototype ?? InstantPrototype;
    }

    internal static JSObject InstantPrototype
    {
        get
        {
            var temporal = (JSEngine.Current as JSObject)?[KeyStrings.GetOrCreate("Temporal")] as JSObject;
            return (temporal?[KeyStrings.GetOrCreate("Instant")] as JSFunction)?.prototype;
        }
    }

    private static bool IsValid(BigInteger ns) => ns >= MinEpochNanoseconds && ns <= MaxEpochNanoseconds;

    // ── accessors ───────────────────────────────────────────────────────────────

    [JSExport("epochMilliseconds")]
    public double EpochMilliseconds => (double)FloorDiv(epochNanoseconds, 1_000_000);

    [JSExport("epochNanoseconds")]
    public JSValue EpochNanoseconds => new JSBigInt(epochNanoseconds);

    // ── statics ─────────────────────────────────────────────────────────────────

    [JSExport("from", Length = 1)]
    internal static JSValue From(in Arguments a) => ToTemporalInstant(a.GetAt(0));

    [JSExport("fromEpochMilliseconds", Length = 1)]
    internal static JSValue FromEpochMilliseconds(in Arguments a)
    {
        var ms = (a.GetAt(0) ?? JSUndefined.Value).DoubleValue;
        if (double.IsNaN(ms) || double.IsInfinity(ms) || Math.Truncate(ms) != ms)
            throw JSEngine.NewRangeError("Temporal.Instant.fromEpochMilliseconds requires an integral Number");

        var ns = new BigInteger(ms) * 1_000_000;
        if (!IsValid(ns))
            throw JSEngine.NewRangeError("Temporal.Instant.fromEpochMilliseconds: out of range");

        return new JSTemporalInstant(ns, InstantPrototype);
    }

    [JSExport("fromEpochNanoseconds", Length = 1)]
    internal static JSValue FromEpochNanoseconds(in Arguments a)
    {
        var ns = JSBigInt.Coerce(a.GetAt(0) ?? JSUndefined.Value).value;
        if (!IsValid(ns))
            throw JSEngine.NewRangeError("Temporal.Instant.fromEpochNanoseconds: out of range");

        return new JSTemporalInstant(ns, InstantPrototype);
    }

    [JSExport("compare", Length = 2)]
    internal static JSValue Compare(in Arguments a)
    {
        var one = RequireInstant(ToTemporalInstant(a.GetAt(0)));
        var two = RequireInstant(ToTemporalInstant(a.GetAt(1)));
        return new JSNumber(one.epochNanoseconds < two.epochNanoseconds ? -1
            : one.epochNanoseconds > two.epochNanoseconds ? 1 : 0);
    }

    // ── methods ─────────────────────────────────────────────────────────────────

    [JSExport("add", Length = 1)]
    public JSValue Add(in Arguments a) => AddDuration(a.GetAt(0), 1);

    [JSExport("subtract", Length = 1)]
    public JSValue Subtract(in Arguments a) => AddDuration(a.GetAt(0), -1);

    private JSValue AddDuration(JSValue durationLike, int sign)
    {
        var duration = (JSTemporalDuration)JSTemporalDuration.ToTemporalDuration(durationLike);

        // Instant arithmetic is calendar- and zone-independent, so only time units are
        // allowed: any nonzero years/months/weeks/days is a RangeError.
        if (duration.YearsValue != 0 || duration.MonthsValue != 0 || duration.WeeksValue != 0 || duration.DaysValue != 0)
            throw JSEngine.NewRangeError("Temporal.Instant arithmetic does not support calendar units (years, months, weeks, days)");

        var deltaNs = TimeDurationNanoseconds(duration) * sign;
        var result = epochNanoseconds + deltaNs;
        if (!IsValid(result))
            throw JSEngine.NewRangeError("Temporal.Instant: result is out of range");

        return new JSTemporalInstant(result, InstantPrototype);
    }

    [JSExport("until", Length = 1)]
    public JSValue Until(in Arguments a) => Difference(a.GetAt(0), 1);

    [JSExport("since", Length = 1)]
    public JSValue Since(in Arguments a) => Difference(a.GetAt(0), -1);

    private JSValue Difference(JSValue other, int sign)
    {
        var target = RequireInstant(ToTemporalInstant(other));
        var diff = (target.epochNanoseconds - epochNanoseconds) * sign;

        // Default largest unit is "second"; express the difference as seconds + sub-second
        // components (no rounding options are honored in this minimal implementation).
        var seconds = FloorDiv(diff, 1_000_000_000);
        var rest = diff - seconds * 1_000_000_000;
        var milliseconds = rest / 1_000_000; rest %= 1_000_000;
        var microseconds = rest / 1_000; rest %= 1_000;
        var nanoseconds = rest;

        return new JSTemporalDuration(0, 0, 0, 0, 0, 0,
            (double)seconds, (double)milliseconds, (double)microseconds, (double)nanoseconds,
            JSTemporalDuration.DurationPrototype);
    }

    [JSExport("round", Length = 1)]
    public JSValue Round(in Arguments a)
    {
        var smallestUnit = ReadRoundingUnit(a.GetAt(0));
        var increment = UnitNanoseconds(smallestUnit);
        var rounded = RoundToIncrement(epochNanoseconds, increment);
        if (!IsValid(rounded))
            throw JSEngine.NewRangeError("Temporal.Instant.round: result is out of range");

        return new JSTemporalInstant(rounded, InstantPrototype);
    }

    [JSExport("equals", Length = 1)]
    public JSValue Equals(in Arguments a)
    {
        var other = RequireInstant(ToTemporalInstant(a.GetAt(0)));
        return epochNanoseconds == other.epochNanoseconds ? JSValue.BooleanTrue : JSValue.BooleanFalse;
    }

    [JSExport("toString", Length = 0)]
    public JSValue ToStringMethod(in Arguments a) => new JSString(ToISOString());

    [JSExport("toJSON", Length = 0)]
    public JSValue ToJSON(in Arguments a) => new JSString(ToISOString());

    [JSExport("toLocaleString", Length = 0)]
    public JSValue ToLocaleString(in Arguments a) => new JSString(ToISOString());

    [JSExport("valueOf", Length = 0)]
    public JSValue ValueOf(in Arguments a)
        => throw JSEngine.NewTypeError("Called Temporal.Instant.prototype.valueOf, which is not supported. Use Temporal.Instant.compare for comparison.");

    // toZonedDateTimeISO(timeZone): a ZonedDateTime at this instant with the ISO calendar.
    [JSExport("toZonedDateTimeISO", Length = 1)]
    public JSValue ToZonedDateTimeISO(in Arguments a)
    {
        var tz = a.GetAt(0);
        if (tz == null || !tz.IsString)
            throw JSEngine.NewTypeError("Temporal.Instant.prototype.toZonedDateTimeISO: time zone must be a string");
        return JSTemporalZonedDateTime.CreateChecked(epochNanoseconds, tz.ToString());
    }

    // toZonedDateTime({ timeZone, calendar }): a ZonedDateTime at this instant.
    [JSExport("toZonedDateTime", Length = 1)]
    public JSValue ToZonedDateTime(in Arguments a)
    {
        if (a.GetAt(0) is not JSObject obj)
            throw JSEngine.NewTypeError("Temporal.Instant.prototype.toZonedDateTime requires an object");

        var calendar = obj[KeyStrings.GetOrCreate("calendar")];
        if (calendar.IsUndefined)
            throw JSEngine.NewTypeError("Temporal.Instant.prototype.toZonedDateTime requires a calendar");
        if (!string.Equals(calendar.ToString(), "iso8601", StringComparison.OrdinalIgnoreCase))
            throw JSEngine.NewRangeError($"Temporal.Instant: unsupported calendar \"{calendar}\" (only iso8601 is implemented)");

        var tz = obj[KeyStrings.GetOrCreate("timeZone")];
        if (tz.IsUndefined || !tz.IsString)
            throw JSEngine.NewTypeError("Temporal.Instant.prototype.toZonedDateTime requires a timeZone string");
        return JSTemporalZonedDateTime.CreateChecked(epochNanoseconds, tz.ToString());
    }

    // ── helpers ─────────────────────────────────────────────────────────────────

    private static JSTemporalInstant RequireInstant(JSValue value)
        => value as JSTemporalInstant ?? throw JSEngine.NewTypeError("expected a Temporal.Instant");

    internal static JSValue ToTemporalInstant(JSValue item)
    {
        if (item is JSTemporalInstant instant)
            return new JSTemporalInstant(instant.epochNanoseconds, InstantPrototype);

        if (item.IsString)
            return ParseTemporalInstant(item.ToString());

        throw JSEngine.NewTypeError("Temporal.Instant: invalid value");
    }

    private static BigInteger TimeDurationNanoseconds(JSTemporalDuration d)
        => new BigInteger(d.HoursValue) * 3_600_000_000_000
         + new BigInteger(d.MinutesValue) * 60_000_000_000
         + new BigInteger(d.SecondsValue) * 1_000_000_000
         + new BigInteger(d.MillisecondsValue) * 1_000_000
         + new BigInteger(d.MicrosecondsValue) * 1_000
         + new BigInteger(d.NanosecondsValue);

    private static string ReadRoundingUnit(JSValue options)
    {
        string unit;
        if (options.IsString)
        {
            unit = options.ToString();
        }
        else if (options is JSObject optionsObject)
        {
            var v = optionsObject[KeyStrings.GetOrCreate("smallestUnit")];
            if (v.IsUndefined)
                throw JSEngine.NewRangeError("Temporal.Instant.round requires a smallestUnit");
            unit = v.ToString();
        }
        else
        {
            throw JSEngine.NewTypeError("Temporal.Instant.round requires an options object or string");
        }

        return unit switch
        {
            "hour" or "hours" => "hour",
            "minute" or "minutes" => "minute",
            "second" or "seconds" => "second",
            "millisecond" or "milliseconds" => "millisecond",
            "microsecond" or "microseconds" => "microsecond",
            "nanosecond" or "nanoseconds" => "nanosecond",
            _ => throw JSEngine.NewRangeError($"Temporal.Instant.round: invalid smallestUnit \"{unit}\""),
        };
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

    // RoundNumberToIncrement with the default half-expand rounding mode.
    private static BigInteger RoundToIncrement(BigInteger value, long increment)
    {
        if (increment <= 1) return value;

        var quotient = FloorDiv(value, increment);
        var remainder = value - quotient * increment;
        if (remainder * 2 >= increment)
            quotient += 1;

        return quotient * increment;
    }

    private static BigInteger FloorDiv(BigInteger a, BigInteger b)
    {
        var q = BigInteger.DivRem(a, b, out var r);
        if (r != 0 && (r < 0) != (b < 0))
            q -= 1;
        return q;
    }

    private static readonly Regex InstantPattern = new(
        @"^(\d{4}|[+-−]\d{6})-(\d{2})-(\d{2})[Tt ](\d{2}):(\d{2})(?::(\d{2})(?:[.,](\d{1,9}))?)?(?:[Zz]|([+-−])(\d{2}):?(\d{2}))$",
        RegexOptions.CultureInvariant);

    private static JSValue ParseTemporalInstant(string text)
    {
        var match = InstantPattern.Match(text);
        if (!match.Success)
            throw JSEngine.NewRangeError($"Cannot parse Temporal.Instant from \"{text}\"");

        int Year()
        {
            var raw = match.Groups[1].Value.Replace('−', '-');
            return int.Parse(raw, CultureInfo.InvariantCulture);
        }

        var year = Year();
        var month = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
        var day = int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
        var hour = int.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture);
        var minute = int.Parse(match.Groups[5].Value, CultureInfo.InvariantCulture);
        var second = match.Groups[6].Success ? int.Parse(match.Groups[6].Value, CultureInfo.InvariantCulture) : 0;

        var fractionNs = BigInteger.Zero;
        if (match.Groups[7].Success)
        {
            var digits = match.Groups[7].Value.PadRight(9, '0');
            fractionNs = BigInteger.Parse(digits, CultureInfo.InvariantCulture);
        }

        var days = DaysFromCivil(year, month, day);
        var secondsOfDay = (long)hour * 3600 + (long)minute * 60 + second;
        var ns = (new BigInteger(days) * 86400 + secondsOfDay) * 1_000_000_000 + fractionNs;

        // Apply the UTC offset (subtract to convert local → UTC).
        if (match.Groups[8].Success)
        {
            var offsetSign = match.Groups[8].Value is "-" or "−" ? -1 : 1;
            var offsetHours = int.Parse(match.Groups[9].Value, CultureInfo.InvariantCulture);
            var offsetMinutes = int.Parse(match.Groups[10].Value, CultureInfo.InvariantCulture);
            var offsetNs = (long)(offsetHours * 3600 + offsetMinutes * 60) * 1_000_000_000 * offsetSign;
            ns -= offsetNs;
        }

        if (!IsValid(ns))
            throw JSEngine.NewRangeError("Temporal.Instant: parsed value is out of range");

        return new JSTemporalInstant(ns, InstantPrototype);
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

    private string ToISOString()
    {
        var totalSeconds = FloorDiv(epochNanoseconds, 1_000_000_000);
        var fraction = epochNanoseconds - totalSeconds * 1_000_000_000; // [0, 1e9)

        var days = (long)FloorDiv(totalSeconds, 86400);
        var secondsOfDay = (long)(totalSeconds - new BigInteger(days) * 86400);

        var (y, m, d) = CivilFromDays(days);
        var hour = secondsOfDay / 3600;
        var minute = secondsOfDay % 3600 / 60;
        var second = secondsOfDay % 60;

        var sb = new StringBuilder();
        if (y < 0 || y > 9999)
            sb.Append(y < 0 ? '-' : '+').Append(Math.Abs(y).ToString("000000", CultureInfo.InvariantCulture));
        else
            sb.Append(y.ToString("0000", CultureInfo.InvariantCulture));

        sb.Append('-').Append(m.ToString("00", CultureInfo.InvariantCulture))
          .Append('-').Append(d.ToString("00", CultureInfo.InvariantCulture))
          .Append('T').Append(hour.ToString("00", CultureInfo.InvariantCulture))
          .Append(':').Append(minute.ToString("00", CultureInfo.InvariantCulture))
          .Append(':').Append(second.ToString("00", CultureInfo.InvariantCulture));

        if (fraction != 0)
        {
            var frac = fraction.ToString("000000000", CultureInfo.InvariantCulture).TrimEnd('0');
            sb.Append('.').Append(frac);
        }

        sb.Append('Z');
        return sb.ToString();
    }
}
