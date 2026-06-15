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
    public JSValue Until(in Arguments a) => Difference(a.GetAt(0), a.GetAt(1), 1);

    [JSExport("since", Length = 1)]
    public JSValue Since(in Arguments a) => Difference(a.GetAt(0), a.GetAt(1), -1);

    // DifferenceTemporalInstant: the difference between two instants, expressed as a Duration of
    // time units. The options bag (largestUnit / smallestUnit / roundingIncrement / roundingMode)
    // is honored per GetDifferenceSettings; only time units are valid (an Instant has no date).
    private JSValue Difference(JSValue other, JSValue optionsArg, int sign)
    {
        var target = RequireInstant(ToTemporalInstant(other));

        var largestUnit = "auto";
        var smallestUnit = "nanosecond";
        var roundingMode = "trunc";
        var increment = 1;

        if (optionsArg != null && !optionsArg.IsUndefined)
        {
            if (optionsArg is not JSObject options)
                throw JSEngine.NewTypeError("Temporal.Instant difference options must be an object or undefined");

            // GetDifferenceSettings reads every option — largestUnit, roundingIncrement, roundingMode,
            // smallestUnit, in that order — and coerces each (calendar units included) before any is
            // validated against the allowed group, so a disallowed unit is reported only afterwards.
            var lu = options[KeyStrings.GetOrCreate("largestUnit")];
            if (!lu.IsUndefined)
                largestUnit = TemporalRoundingOptions.NormalizeAnyUnit(lu.StringValue, allowAuto: true);

            increment = TemporalRoundingOptions.GetRoundingIncrement(options);
            roundingMode = TemporalRoundingOptions.GetRoundingMode(options, "trunc");

            var su = options[KeyStrings.GetOrCreate("smallestUnit")];
            if (!su.IsUndefined)
                smallestUnit = TemporalRoundingOptions.NormalizeAnyUnit(su.StringValue, allowAuto: false);

            // Only time units (hour … nanosecond) are valid for an Instant difference.
            if (largestUnit != "auto" && TemporalRoundingOptions.UnitIndex(largestUnit) < TemporalRoundingOptions.UnitIndex("hour"))
                throw JSEngine.NewRangeError($"Temporal.Instant: invalid largestUnit \"{largestUnit}\"");
            if (TemporalRoundingOptions.UnitIndex(smallestUnit) < TemporalRoundingOptions.UnitIndex("hour"))
                throw JSEngine.NewRangeError($"Temporal.Instant: invalid smallestUnit \"{smallestUnit}\"");
        }

        // largestUnit defaults to the larger of "second" and the chosen smallestUnit.
        if (largestUnit == "auto")
            largestUnit = TemporalRoundingOptions.UnitIndex(smallestUnit) < TemporalRoundingOptions.UnitIndex("second")
                ? smallestUnit : "second";

        if (TemporalRoundingOptions.UnitIndex(largestUnit) > TemporalRoundingOptions.UnitIndex(smallestUnit))
            throw JSEngine.NewRangeError("Temporal.Instant: largestUnit must be larger than or equal to smallestUnit");

        var maxIncrement = smallestUnit switch
        {
            "hour" => 24L,
            "minute" or "second" => 60L,
            _ => 1000L, // millisecond / microsecond / nanosecond
        };
        TemporalRoundingOptions.ValidateRoundingIncrement(increment, maxIncrement, inclusive: false);

        // The "since" direction is the negation of "until"; rounding a negated value with the
        // requested mode is equivalent to negating both the mode and the rounded result.
        var diff = (target.epochNanoseconds - epochNanoseconds) * sign;
        var unitNs = (BigInteger)UnitNanoseconds(smallestUnit) * increment;
        var rounded = TemporalRoundingOptions.RoundToIncrement(diff, unitNs, roundingMode);
        return BalanceTimeDuration(rounded, largestUnit);
    }

    // BalanceTimeDuration: distribute a signed nanosecond total into time components, from
    // `largestUnit` (hour … nanosecond) downward.
    private static JSValue BalanceTimeDuration(BigInteger totalNs, string largestUnit)
    {
        var sign = totalNs.Sign;
        var ns = BigInteger.Abs(totalNs);
        var li = TemporalRoundingOptions.UnitIndex(largestUnit);

        BigInteger hours = 0, minutes = 0, seconds = 0, millis = 0, micros = 0;
        if (li <= TemporalRoundingOptions.UnitIndex("hour")) { hours = ns / 3_600_000_000_000; ns %= 3_600_000_000_000; }
        if (li <= TemporalRoundingOptions.UnitIndex("minute")) { minutes = ns / 60_000_000_000; ns %= 60_000_000_000; }
        if (li <= TemporalRoundingOptions.UnitIndex("second")) { seconds = ns / 1_000_000_000; ns %= 1_000_000_000; }
        if (li <= TemporalRoundingOptions.UnitIndex("millisecond")) { millis = ns / 1_000_000; ns %= 1_000_000; }
        if (li <= TemporalRoundingOptions.UnitIndex("microsecond")) { micros = ns / 1_000; ns %= 1_000; }
        var nanos = ns;

        double S(BigInteger v) => sign * (double)v;
        return new JSTemporalDuration(0, 0, 0, 0, S(hours), S(minutes), S(seconds), S(millis), S(micros), S(nanos),
            JSTemporalDuration.DurationPrototype);
    }

    [JSExport("round", Length = 1)]
    public JSValue Round(in Arguments a)
    {
        var roundTo = a.GetAt(0) ?? JSUndefined.Value;
        if (roundTo.IsUndefined)
            throw JSEngine.NewTypeError("Temporal.Instant.prototype.round requires an argument");

        string smallestUnit;
        var increment = 1;
        var roundingMode = "halfExpand";

        if (roundTo.IsString)
        {
            smallestUnit = TemporalRoundingOptions.NormalizeTimeUnit(roundTo.StringValue, allowAuto: false);
        }
        else if (roundTo is JSObject options)
        {
            // The increment and mode are read before smallestUnit, matching the spec ordering
            // (so a bad roundingIncrement / roundingMode is reported before "missing smallestUnit").
            increment = TemporalRoundingOptions.GetRoundingIncrement(options);
            roundingMode = TemporalRoundingOptions.GetRoundingMode(options, "halfExpand");

            var su = options[KeyStrings.GetOrCreate("smallestUnit")];
            if (su.IsUndefined)
                throw JSEngine.NewRangeError("Temporal.Instant.round requires a smallestUnit");
            smallestUnit = TemporalRoundingOptions.NormalizeTimeUnit(su.StringValue, allowAuto: false);
        }
        else throw JSEngine.NewTypeError("Temporal.Instant.round requires an options object or string");

        // Instant rounding is relative to a whole day, so the increment may go up to the number of
        // smallestUnits in a day (inclusive) and must divide it evenly.
        var dayUnits = smallestUnit switch
        {
            "hour" => 24L,
            "minute" => 1440L,
            "second" => 86400L,
            "millisecond" => 86_400_000L,
            "microsecond" => 86_400_000_000L,
            _ => 86_400_000_000_000L, // nanosecond
        };
        TemporalRoundingOptions.ValidateRoundingIncrement(increment, dayUnits, inclusive: true);

        var unitNs = (BigInteger)UnitNanoseconds(smallestUnit) * increment;
        var rounded = TemporalRoundingOptions.RoundToIncrement(epochNanoseconds, unitNs, roundingMode);
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
    public JSValue ToStringMethod(in Arguments a)
    {
        var optionsArg = a.GetAt(0);
        var digits = -1; // "auto"
        var roundingMode = "trunc";
        string smallestUnit = null;

        if (optionsArg != null && !optionsArg.IsUndefined)
        {
            if (optionsArg is not JSObject options)
                throw JSEngine.NewTypeError("Temporal.Instant.toString options must be an object or undefined");

            digits = TemporalRoundingOptions.GetFractionalSecondDigits(options);
            roundingMode = TemporalRoundingOptions.GetRoundingMode(options, "trunc");

            var su = options[KeyStrings.GetOrCreate("smallestUnit")];
            if (!su.IsUndefined)
            {
                smallestUnit = TemporalRoundingOptions.NormalizeTimeUnit(su.StringValue, allowAuto: false);
                if (smallestUnit == "hour")
                    throw JSEngine.NewRangeError("Temporal.Instant.toString: smallestUnit cannot be \"hour\"");
            }
        }

        // ToSecondsStringPrecisionRecord: precision -2 = minutes (no seconds), -1 = auto,
        // 0..9 = a fixed number of fractional-second digits; incrementNs is the rounding step.
        int precision;
        long incrementNs;
        if (smallestUnit != null)
        {
            (precision, incrementNs) = smallestUnit switch
            {
                "minute" => (-2, 60_000_000_000L),
                "second" => (0, 1_000_000_000L),
                "millisecond" => (3, 1_000_000L),
                "microsecond" => (6, 1_000L),
                _ => (9, 1L), // nanosecond
            };
        }
        else if (digits == -1) { precision = -1; incrementNs = 1; }
        else { precision = digits; incrementNs = TemporalRoundingOptions.Pow10(9 - digits); }

        var rounded = TemporalRoundingOptions.RoundToIncrement(epochNanoseconds, incrementNs, roundingMode);
        if (!IsValid(rounded))
            throw JSEngine.NewRangeError("Temporal.Instant.toString: result is out of range");

        return new JSString(FormatISO(rounded, precision));
    }

    [JSExport("toJSON", Length = 0)]
    public JSValue ToJSON(in Arguments a) => new JSString(ToISOString());

    [JSExport("toLocaleString", Length = 0)]
    public JSValue ToLocaleString(in Arguments a)
        => Intl.JSIntlDateTimeFormat.TemporalToLocaleString(this, a.GetAt(0), a.GetAt(1));

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

        // A ZonedDateTime is converted using its epoch nanoseconds.
        if (item is JSTemporalZonedDateTime zdt)
            return new JSTemporalInstant(zdt.epochNanoseconds, InstantPrototype);

        if (item is JSObject obj)
            item = new JSString(obj.StringValue);

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

    private static long UnitNanoseconds(string unit) => unit switch
    {
        "hour" => 3_600_000_000_000,
        "minute" => 60_000_000_000,
        "second" => 1_000_000_000,
        "millisecond" => 1_000_000,
        "microsecond" => 1_000,
        _ => 1,
    };

    private static BigInteger FloorDiv(BigInteger a, BigInteger b)
    {
        var q = BigInteger.DivRem(a, b, out var r);
        if (r != 0 && (r < 0) != (b < 0))
            q -= 1;
        return q;
    }

    // A Temporal.Instant string is a date-time that *must* carry a UTC designator — either Z or a
    // numeric UTC offset (which may run down to sub-minute precision with a fractional part). The
    // date may use the extended ("1976-11-18") or basic ("19761118") form, the minutes/seconds of
    // both the wall clock and the offset are optional, and any trailing RFC 9557 [..] annotations
    // (time-zone and/or key=value) are permitted and ignored here.
    private const string YearField = @"\d{4}|\+\d{6}|-(?!000000)\d{6}";
    private static readonly Regex InstantPattern = new(
        @"^(?:(?<y>" + YearField + @")-(?<mo>\d{2})-(?<d>\d{2})|(?<y>" + YearField + @")(?<mo>\d{2})(?<d>\d{2}))" +
        @"[Tt ](?<h>\d{2})(?::?(?<mi>\d{2})(?::?(?<s>\d{2})(?:[.,](?<f>\d{1,9}))?)?)?" +
        @"(?:(?<z>[Zz])|(?<off>(?<osign>[+-])(?<oh>\d{2})(?::?(?<om>\d{2})(?::?(?<os>\d{2})(?:[.,](?<of>\d{1,9}))?)?)?))" +
        TemporalIsoString.AnnotationsTail + "$",
        RegexOptions.CultureInvariant);

    private static JSValue ParseTemporalInstant(string text)
    {
        // The trailing annotations are validated even though their payload is ignored: more than one
        // calendar/time-zone annotation, a malformed key=value, or a critical unknown key is a
        // RangeError (shared with the other Temporal string parsers).
        TemporalIsoString.RejectMultipleCalendarAnnotations(text);
        TemporalIsoString.RejectMalformedAnnotations(text);
        TemporalIsoString.RejectInvalidAnnotations(text);

        var match = InstantPattern.Match(text);
        if (!match.Success)
            throw JSEngine.NewRangeError($"Cannot parse Temporal.Instant from \"{text}\"");

        var year = int.Parse(match.Groups["y"].Value, CultureInfo.InvariantCulture);
        var month = int.Parse(match.Groups["mo"].Value, CultureInfo.InvariantCulture);
        var day = int.Parse(match.Groups["d"].Value, CultureInfo.InvariantCulture);
        var hour = int.Parse(match.Groups["h"].Value, CultureInfo.InvariantCulture);
        var minute = match.Groups["mi"].Success ? int.Parse(match.Groups["mi"].Value, CultureInfo.InvariantCulture) : 0;
        var second = match.Groups["s"].Success ? int.Parse(match.Groups["s"].Value, CultureInfo.InvariantCulture) : 0;
        if (second == 60) second = 59; // a leap second collapses to :59

        if (month < 1 || month > 12 || day < 1 || day > DaysInMonthOf(year, month)
            || hour > 23 || minute > 59 || second > 59)
            throw JSEngine.NewRangeError($"Cannot parse Temporal.Instant from \"{text}\"");

        var fractionNs = BigInteger.Zero;
        if (match.Groups["f"].Success)
        {
            var digits = match.Groups["f"].Value.PadRight(9, '0');
            fractionNs = BigInteger.Parse(digits, CultureInfo.InvariantCulture);
        }

        var days = DaysFromCivil(year, month, day);
        var secondsOfDay = (long)hour * 3600 + (long)minute * 60 + second;
        var ns = (new BigInteger(days) * 86400 + secondsOfDay) * 1_000_000_000 + fractionNs;

        // Apply the UTC offset (subtract to convert local → UTC); a Z designator means a zero offset.
        if (match.Groups["off"].Success)
        {
            // The offset must use consistent separators (e.g. "+00:0000" mixes ':' with bare digits).
            if (!TemporalIsoString.IsStrictOffset(match.Groups["off"].Value))
                throw JSEngine.NewRangeError($"Cannot parse Temporal.Instant from \"{text}\"");

            var offsetSign = match.Groups["osign"].Value == "-" ? -1 : 1;
            var offsetHours = int.Parse(match.Groups["oh"].Value, CultureInfo.InvariantCulture);
            var offsetMinutes = match.Groups["om"].Success ? int.Parse(match.Groups["om"].Value, CultureInfo.InvariantCulture) : 0;
            var offsetSeconds = match.Groups["os"].Success ? int.Parse(match.Groups["os"].Value, CultureInfo.InvariantCulture) : 0;
            if (offsetHours > 23 || offsetMinutes > 59 || offsetSeconds > 59)
                throw JSEngine.NewRangeError($"Cannot parse Temporal.Instant from \"{text}\"");

            var offsetFractionNs = BigInteger.Zero;
            if (match.Groups["of"].Success)
                offsetFractionNs = BigInteger.Parse(match.Groups["of"].Value.PadRight(9, '0'), CultureInfo.InvariantCulture);

            var offsetNs = ((long)(offsetHours * 3600 + offsetMinutes * 60 + offsetSeconds) * 1_000_000_000 + offsetFractionNs) * offsetSign;
            ns -= offsetNs;
        }

        if (!IsValid(ns))
            throw JSEngine.NewRangeError("Temporal.Instant: parsed value is out of range");

        return new JSTemporalInstant(ns, InstantPrototype);
    }

    private static bool IsLeapYear(long y) => (y % 4 == 0 && y % 100 != 0) || y % 400 == 0;

    private static int DaysInMonthOf(long year, int month) => month switch
    {
        1 or 3 or 5 or 7 or 8 or 10 or 12 => 31,
        4 or 6 or 9 or 11 => 30,
        2 => IsLeapYear(year) ? 29 : 28,
        _ => 0,
    };

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

    private string ToISOString() => FormatISO(epochNanoseconds, precision: -1);

    // Renders epoch nanoseconds as an ISO-8601 UTC string. `precision` controls the seconds field:
    // -2 omits seconds (minute precision), -1 emits the minimal (auto) fractional part, and 0..9
    // emits exactly that many fractional digits.
    private static string FormatISO(BigInteger ns, int precision)
    {
        var totalSeconds = FloorDiv(ns, 1_000_000_000);
        var fraction = ns - totalSeconds * 1_000_000_000; // [0, 1e9)

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
          .Append(':').Append(minute.ToString("00", CultureInfo.InvariantCulture));

        if (precision != -2)
        {
            sb.Append(':').Append(second.ToString("00", CultureInfo.InvariantCulture));
            AppendFraction(sb, (long)fraction, precision);
        }

        sb.Append('Z');
        return sb.ToString();
    }

    // Appends the fractional-second part for a 0..999_999_999 nanosecond value. precision -1 trims
    // trailing zeros (and omits the field entirely when zero); 0 omits it; 1..9 zero-pads to width.
    internal static void AppendFraction(StringBuilder sb, long fractionNs, int precision)
    {
        if (precision == 0) return;
        if (precision < 0)
        {
            if (fractionNs == 0) return;
            var frac = fractionNs.ToString("000000000", CultureInfo.InvariantCulture).TrimEnd('0');
            sb.Append('.').Append(frac);
            return;
        }

        var digits = fractionNs.ToString("000000000", CultureInfo.InvariantCulture).Substring(0, precision);
        sb.Append('.').Append(digits);
    }
}
