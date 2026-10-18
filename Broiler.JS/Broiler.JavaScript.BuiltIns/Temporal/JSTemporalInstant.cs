using System;
using System.Numerics;
using Broiler.ECMAScript.Numbers;
using Broiler.ECMAScript.Temporal;
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

    private static readonly KeyString InstantIntrinsicKey = KeyStrings.GetOrCreate("Temporal.Instant");

    // %Temporal.Instant.prototype% of the current realm, never the prototype of whatever
    // `globalThis.Temporal.Instant` holds now: guest code may replace the namespace or its members.
    internal static JSObject InstantPrototype => Intrinsics.TemporalPrototype(InstantIntrinsicKey);

    // IsValidEpochNanoseconds, the shared library's: within ±10^8 days of the epoch.
    private static bool IsValid(BigInteger ns) => TemporalAdapter.IsValidEpochNanoseconds(ns);

    // ── accessors ───────────────────────────────────────────────────────────────

    [JSExport("epochMilliseconds")]
    public double EpochMilliseconds => TemporalAdapter.EpochMilliseconds(epochNanoseconds);

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

        // NumberToBigInt(epochMilliseconds) × 10^6: the shared library's BigIntMath.FromIntegralNumber
        // (SJL-4.5), after this engine's RangeError for a Number that is not integral.
        var ns = BigIntMath.FromIntegralNumber(ms) * 1_000_000;
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

        // AddInstant, the shared library's, of the exact time duration.
        TemporalAdapter.Check(IsoArithmetic.AddInstant(epochNanoseconds, sign * TemporalAdapter.TimeDuration(duration), out var result),
            "Temporal.Instant: result is out of range");

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
        var roundingMode = RoundingMode.Trunc;
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
            roundingMode = TemporalRoundingOptions.GetRoundingMode(options, RoundingMode.Trunc);

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
        var rounded = ExactRounding.RoundNumberToIncrement(diff, BigInteger.One, unitNs, roundingMode);
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

        // Each component is the ℝ→𝔽 nearest double (ties to even), not .NET's
        // (double)BigInteger which truncates toward zero — observable for the very large
        // components a near-limit difference produces (#818 Problems 18/19).
        double S(BigInteger v) => sign * ExactRounding.ToDouble(v, BigInteger.One);
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
        var roundingMode = RoundingMode.HalfExpand;

        if (roundTo.IsString)
        {
            smallestUnit = TemporalRoundingOptions.NormalizeTimeUnit(roundTo.StringValue, allowAuto: false);
        }
        else if (roundTo is JSObject options)
        {
            // The increment and mode are read before smallestUnit, matching the spec ordering
            // (so a bad roundingIncrement / roundingMode is reported before "missing smallestUnit").
            increment = TemporalRoundingOptions.GetRoundingIncrement(options);
            roundingMode = TemporalRoundingOptions.GetRoundingMode(options, RoundingMode.HalfExpand);

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

        // RoundTemporalInstant, the shared library's.
        var rounded = IsoArithmetic.RoundTemporalInstant(epochNanoseconds, increment, TemporalAdapter.Unit(smallestUnit), roundingMode);
        if (!IsValid(rounded))
            throw JSEngine.NewRangeError("Temporal.Instant.round: result is out of range");

        return new JSTemporalInstant(rounded, InstantPrototype);
    }

    [JSExport("equals", Length = 1)]
    public JSValue Equals(in Arguments a)
    {
        var other = RequireInstant(ToTemporalInstant(a.GetAt(0)));
        return epochNanoseconds == other.epochNanoseconds ? BooleanTrue : BooleanFalse;
    }

    [JSExport("toString", Length = 0)]
    public JSValue ToStringMethod(in Arguments a)
    {
        var optionsArg = a.GetAt(0);
        var digits = TemporalFormat.AutoPrecision;
        var roundingMode = RoundingMode.Trunc;
        string smallestUnit = null;
        string timeZoneId = null;

        if (optionsArg != null && !optionsArg.IsUndefined)
        {
            if (optionsArg is not JSObject options)
                throw JSEngine.NewTypeError("Temporal.Instant.toString options must be an object or undefined");

            // The options are read in alphabetical order — fractionalSecondDigits, roundingMode,
            // smallestUnit, timeZone — and EVERY option is read (its getter fired and value coerced)
            // before the unit's group is validated, so a date smallestUnit such as "month" is rejected
            // only after timeZone has been read (test262 Instant/prototype/toString
            // options-read-before-algorithmic-validation).
            digits = TemporalAdapter.ReadFractionalSecondDigits(options);
            roundingMode = TemporalRoundingOptions.GetRoundingMode(options, RoundingMode.Trunc);
            smallestUnit = TemporalAdapter.ReadSmallestUnitText(options);

            // A timeZone option displays the instant in that zone (with its numeric offset) instead of
            // UTC; it must be a valid time-zone identifier — a bare date-time string is a RangeError.
            var tz = options[KeyStrings.GetOrCreate("timeZone")];
            if (!tz.IsUndefined)
                timeZoneId = JSTemporalZonedDateTime.ToTimeZoneIdentifier(tz);
        }

        var precision = TemporalAdapter.ToStringPrecision(smallestUnit, digits, duration: false, "Temporal.Instant");

        // RoundTemporalInstant, then TemporalInstantToString with the offset in force at the rounded instant:
        // the shared library's.
        var rounded = IsoArithmetic.RoundTemporalInstant(epochNanoseconds, precision.Increment, precision.Unit, roundingMode);
        if (!IsValid(rounded))
            throw JSEngine.NewRangeError("Temporal.Instant.toString: result is out of range");

        long? offsetNs = timeZoneId == null ? null : TemporalTimeZones.OffsetNanoseconds(timeZoneId, rounded);
        return new JSString(TemporalFormat.InstantToString(rounded, offsetNs, precision.Precision));
    }

    [JSExport("toJSON", Length = 0)]
    public JSValue ToJSON(in Arguments a) => new JSString(TemporalFormat.InstantToString(epochNanoseconds, null, TemporalFormat.AutoPrecision));

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

    // Note: Temporal.Instant.prototype.toZonedDateTime was removed from the Temporal
    // proposal (June 2024 TC39 consensus); only toZonedDateTimeISO remains. See
    // test/staging/Temporal/removed-methods.js.

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

    private static long UnitNanoseconds(string unit) => unit switch
    {
        "hour" => 3_600_000_000_000,
        "minute" => 60_000_000_000,
        "second" => 1_000_000_000,
        "millisecond" => 1_000_000,
        "microsecond" => 1_000,
        _ => 1,
    };

    // ParseTemporalInstant, the shared library's grammar: a date-time that must carry Z or a UTC offset (to
    // sub-minute precision), its annotations checked and their payload ignored, the offset applied, and the
    // instant within the representable range.
    private static JSValue ParseTemporalInstant(string text)
        => new JSTemporalInstant(TemporalAdapter.ParseInstant(text), InstantPrototype);
}
