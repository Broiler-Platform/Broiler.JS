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
// of the epoch (±8.64e21 ns). Its addition, rounding and differences are the shared library's,
// through TemporalAdapter. Registered under the Temporal namespace (Register = false).
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
    public JSValue Until(in Arguments a) => Difference(a.GetAt(0), a.GetAt(1), DifferenceOperation.Until);

    [JSExport("since", Length = 1)]
    public JSValue Since(in Arguments a) => Difference(a.GetAt(0), a.GetAt(1), DifferenceOperation.Since);

    // DifferenceTemporalInstant (s8.5.9): ToTemporalInstant, then GetDifferenceSettings' reads and rules (time units
    // only, largest second by default), then the shared library's DifferenceInstant: the nanoseconds rounded to the
    // smallest unit and increment and balanced up to the largest unit, negated for since.
    private JSValue Difference(JSValue other, JSValue options, DifferenceOperation operation)
    {
        const string typeName = "Temporal.Instant";
        var target = RequireInstant(ToTemporalInstant(other));
        var settings = TemporalAdapter.DifferenceSettings(TemporalType.Instant, operation, options, typeName);
        return TemporalAdapter.DurationOf(TemporalDifferences.DifferenceTemporalInstant(operation, epochNanoseconds, target.epochNanoseconds, settings,
            out var result), result, typeName);
    }

    [JSExport("round", Length = 1)]
    public JSValue Round(in Arguments a)
    {
        // The reads - roundingIncrement, roundingMode, the required smallestUnit - then the library's rule: a time unit
        // whose increment divides a day, a day's worth of the unit included.
        var (increment, roundingMode, smallestUnit) = TemporalAdapter.RoundOptions(TemporalType.Instant, a.GetAt(0), "Temporal.Instant");

        // RoundTemporalInstant, the shared library's.
        var rounded = IsoArithmetic.RoundTemporalInstant(epochNanoseconds, increment, smallestUnit, roundingMode);
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

    // ParseTemporalInstant, the shared library's grammar: a date-time that must carry Z or a UTC offset (to
    // sub-minute precision), its annotations checked and their payload ignored, the offset applied, and the
    // instant within the representable range.
    private static JSValue ParseTemporalInstant(string text)
        => new JSTemporalInstant(TemporalAdapter.ParseInstant(text), InstantPrototype);
}
