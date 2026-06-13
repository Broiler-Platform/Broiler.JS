using System;
using System.Numerics;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Temporal;

// Temporal.Duration arithmetic: add / subtract / round / total.
//
// The calendar-independent cases — durations whose years/months/weeks are all zero — are
// implemented exactly here (days are treated as 24-hour periods, the whole computation done in
// BigInteger nanoseconds). The calendar-dependent cases (nonzero years/months/weeks, or a
// `relativeTo` whose day length varies with a time zone) require calendar / DST arithmetic that
// is not yet wired; those paths throw a clear "not yet implemented" error (or a RangeError when
// the spec requires a relativeTo that is absent).
public partial class JSTemporalDuration
{
    private const long NanosecondsPerDay = 86_400_000_000_000;

    private bool HasCalendarUnits => years != 0 || months != 0 || weeks != 0;

    // days treated as 24h, in nanoseconds.
    private BigInteger TimePlusDaysNanoseconds() => TotalNanoseconds();

    private static readonly string[] UnitOrder =
        { "year", "month", "week", "day", "hour", "minute", "second", "millisecond", "microsecond", "nanosecond" };

    private static int UnitIndex(string unit) => System.Array.IndexOf(UnitOrder, unit);

    private static long TimeUnitNanoseconds(string unit) => unit switch
    {
        "day" => NanosecondsPerDay,
        "hour" => 3_600_000_000_000,
        "minute" => 60_000_000_000,
        "second" => 1_000_000_000,
        "millisecond" => 1_000_000,
        "microsecond" => 1_000,
        "nanosecond" => 1,
        _ => 0, // year/month/week have no fixed length
    };

    private static string NormalizeUnit(string u, bool allowAuto) => u switch
    {
        "auto" when allowAuto => "auto",
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
        _ => throw JSEngine.NewRangeError($"Temporal.Duration: invalid unit \"{u}\""),
    };

    // The largest non-zero unit of this duration (defaults to "nanosecond" for a zero duration).
    private string DefaultLargestUnit()
    {
        if (years != 0) return "year";
        if (months != 0) return "month";
        if (weeks != 0) return "week";
        if (days != 0) return "day";
        if (hours != 0) return "hour";
        if (minutes != 0) return "minute";
        if (seconds != 0) return "second";
        if (milliseconds != 0) return "millisecond";
        if (microseconds != 0) return "microsecond";
        return "nanosecond";
    }

    private static bool TryGetRelativeTo(JSValue options, out JSValue relativeTo)
    {
        relativeTo = JSUndefined.Value;
        if (options == null || options.IsUndefined) return false;
        if (options is not JSObject optionsObject)
            throw JSEngine.NewTypeError("Temporal.Duration options must be an object or undefined");
        relativeTo = optionsObject[KeyStrings.GetOrCreate("relativeTo")];
        return relativeTo != null && !relativeTo.IsNullOrUndefined;
    }

    // ── add / subtract ────────────────────────────────────────────────────────────

    private JSValue AddSubtract(JSValue otherValue, JSValue options, int sign)
    {
        var other = (JSTemporalDuration)ToTemporalDuration(otherValue);
        var hasRelativeTo = TryGetRelativeTo(options, out _);

        if (HasCalendarUnits || other.HasCalendarUnits)
        {
            if (!hasRelativeTo)
                throw JSEngine.NewRangeError(
                    "Temporal.Duration.prototype.add/subtract with calendar units (years, months, weeks) requires a relativeTo option");
            throw JSEngine.NewError(
                "Temporal.Duration.prototype.add/subtract with a relativeTo calendar/time-zone is not yet implemented");
        }

        var totalNs = TimePlusDaysNanoseconds() + sign * other.TimePlusDaysNanoseconds();
        var largest = MaxUnit(DefaultLargestUnit(), other.DefaultLargestUnit());
        var result = (JSTemporalDuration)BalanceFromNanoseconds(totalNs, largest);

        // The balanced result must itself be a valid duration (e.g. the combined time must not
        // overflow the 2^53-seconds limit); otherwise this is a RangeError.
        RejectDuration(result.years, result.months, result.weeks, result.days, result.hours,
            result.minutes, result.seconds, result.milliseconds, result.microseconds, result.nanoseconds);
        return result;
    }

    private static string MaxUnit(string a, string b) => UnitIndex(a) <= UnitIndex(b) ? a : b;

    // ── round ─────────────────────────────────────────────────────────────────────

    internal JSValue RoundImpl(JSValue roundTo)
    {
        string smallestUnit = null;
        string largestUnit = "auto";
        var increment = 1d;
        var roundingMode = "halfExpand";
        JSValue options = roundTo;

        if (roundTo == null || roundTo.IsUndefined)
            throw JSEngine.NewTypeError("Temporal.Duration.prototype.round requires an options argument");

        if (roundTo.IsString)
        {
            smallestUnit = NormalizeUnit(roundTo.StringValue, allowAuto: false);
            options = JSUndefined.Value;
        }
        else if (roundTo is JSObject obj)
        {
            var su = obj[KeyStrings.GetOrCreate("smallestUnit")];
            var lu = obj[KeyStrings.GetOrCreate("largestUnit")];
            if (!lu.IsUndefined) largestUnit = NormalizeUnit(lu.StringValue, allowAuto: true);

            var inc = obj[KeyStrings.GetOrCreate("roundingIncrement")];
            if (!inc.IsUndefined)
            {
                var n = inc.DoubleValue;
                if (double.IsNaN(n) || double.IsInfinity(n) || n < 1 || n > 1_000_000_000 || Math.Truncate(n) != n)
                    throw JSEngine.NewRangeError("Temporal.Duration.round: invalid roundingIncrement");
                increment = n;
            }

            var rm = obj[KeyStrings.GetOrCreate("roundingMode")];
            if (!rm.IsUndefined) roundingMode = NormalizeRoundingMode(rm.StringValue);

            if (!su.IsUndefined) smallestUnit = NormalizeUnit(su.StringValue, allowAuto: false);
            if (smallestUnit == null && largestUnit == "auto")
                throw JSEngine.NewRangeError("Temporal.Duration.round requires either smallestUnit or largestUnit");
        }
        else throw JSEngine.NewTypeError("Temporal.Duration.prototype.round requires an options object or string");

        var hasRelativeTo = TryGetRelativeTo(options, out _);
        smallestUnit ??= "nanosecond";

        if (largestUnit == "auto")
            largestUnit = MaxUnit(DefaultLargestUnit(), smallestUnit);

        // A time-unit smallestUnit caps the rounding increment at the number of those units in the
        // next-larger unit, and the increment must divide it evenly. This validation is independent
        // of the calendar/relativeTo machinery, so run it up front — a bad increment is a RangeError
        // even on the paths that later bail out as "not yet implemented".
        if (UnitIndex(smallestUnit) >= UnitIndex("hour"))
        {
            var maxIncrement = smallestUnit switch
            {
                "hour" => 24L,
                "minute" or "second" => 60L,
                _ => 1000L, // millisecond / microsecond / nanosecond
            };
            TemporalRoundingOptions.ValidateRoundingIncrement((long)increment, maxIncrement, inclusive: false);
        }

        if (HasCalendarUnits || UnitIndex(smallestUnit) < UnitIndex("day") || UnitIndex(largestUnit) < UnitIndex("day"))
        {
            if (!hasRelativeTo)
                throw JSEngine.NewRangeError(
                    "Temporal.Duration.prototype.round with calendar units (years, months, weeks) requires a relativeTo option");
            throw JSEngine.NewError(
                "Temporal.Duration.prototype.round with a relativeTo calendar/time-zone is not yet implemented");
        }
        if (hasRelativeTo)
            throw JSEngine.NewError(
                "Temporal.Duration.prototype.round with a relativeTo is not yet implemented");

        var unitNs = TimeUnitNanoseconds(smallestUnit) * (long)increment;
        var rounded = RoundToIncrement(TimePlusDaysNanoseconds(), unitNs, roundingMode);
        return BalanceFromNanoseconds(rounded, largestUnit);
    }

    // ── total ───────────────────────────────────────────────────────────────────

    internal JSValue TotalImpl(JSValue totalOf)
    {
        string unit;
        JSValue options = totalOf;

        if (totalOf == null || totalOf.IsUndefined)
            throw JSEngine.NewTypeError("Temporal.Duration.prototype.total requires an options argument");

        if (totalOf.IsString)
        {
            unit = NormalizeUnit(totalOf.StringValue, allowAuto: false);
            options = JSUndefined.Value;
        }
        else if (totalOf is JSObject obj)
        {
            var u = obj[KeyStrings.GetOrCreate("unit")];
            if (u.IsUndefined)
                throw JSEngine.NewRangeError("Temporal.Duration.prototype.total requires a unit");
            unit = NormalizeUnit(u.StringValue, allowAuto: false);
        }
        else throw JSEngine.NewTypeError("Temporal.Duration.prototype.total requires an options object or string");

        var hasRelativeTo = TryGetRelativeTo(options, out _);

        if (HasCalendarUnits || UnitIndex(unit) < UnitIndex("day"))
        {
            if (!hasRelativeTo)
                throw JSEngine.NewRangeError(
                    "Temporal.Duration.prototype.total with calendar units (years, months, weeks) requires a relativeTo option");
            throw JSEngine.NewError(
                "Temporal.Duration.prototype.total with a relativeTo calendar/time-zone is not yet implemented");
        }
        if (hasRelativeTo)
            throw JSEngine.NewError(
                "Temporal.Duration.prototype.total with a relativeTo is not yet implemented");

        var totalNs = TimePlusDaysNanoseconds();
        var unitNs = TimeUnitNanoseconds(unit);
        var whole = BigInteger.DivRem(totalNs, unitNs, out var remainder);
        var result = (double)whole + (double)remainder / unitNs;
        return new JSNumber(result);
    }

    // ── balancing / rounding helpers ──────────────────────────────────────────────

    // Builds a Duration from a signed nanosecond total, distributing into components from
    // `largestUnit` (day…nanosecond) downward.
    private static JSValue BalanceFromNanoseconds(BigInteger totalNs, string largestUnit)
    {
        var sign = totalNs.Sign;
        var ns = BigInteger.Abs(totalNs);

        BigInteger days = 0, hours = 0, minutes = 0, seconds = 0, millis = 0, micros = 0;

        if (UnitIndex(largestUnit) <= UnitIndex("day"))
        {
            days = ns / NanosecondsPerDay; ns %= NanosecondsPerDay;
        }
        if (UnitIndex(largestUnit) <= UnitIndex("hour"))
        {
            hours = ns / 3_600_000_000_000; ns %= 3_600_000_000_000;
        }
        if (UnitIndex(largestUnit) <= UnitIndex("minute"))
        {
            minutes = ns / 60_000_000_000; ns %= 60_000_000_000;
        }
        if (UnitIndex(largestUnit) <= UnitIndex("second"))
        {
            seconds = ns / 1_000_000_000; ns %= 1_000_000_000;
        }
        if (UnitIndex(largestUnit) <= UnitIndex("millisecond"))
        {
            millis = ns / 1_000_000; ns %= 1_000_000;
        }
        if (UnitIndex(largestUnit) <= UnitIndex("microsecond"))
        {
            micros = ns / 1_000; ns %= 1_000;
        }
        var nanos = ns;

        double S(BigInteger v) => sign * (double)v;
        return new JSTemporalDuration(0, 0, 0, S(days), S(hours), S(minutes), S(seconds), S(millis), S(micros), S(nanos), DurationPrototype);
    }

    private static string NormalizeRoundingMode(string mode) => mode switch
    {
        "ceil" or "floor" or "expand" or "trunc"
            or "halfCeil" or "halfFloor" or "halfExpand" or "halfTrunc" or "halfEven" => mode,
        _ => throw JSEngine.NewRangeError($"Temporal.Duration: invalid roundingMode \"{mode}\""),
    };

    // RoundNumberToIncrement: rounds `value` to a multiple of `increment` per `roundingMode`.
    private static BigInteger RoundToIncrement(BigInteger value, BigInteger increment, string roundingMode)
    {
        if (increment <= 1 && roundingMode is "halfExpand" or "trunc") return value;

        var quotient = BigInteger.DivRem(value, increment, out var remainder);
        if (remainder == 0) return value;

        var sign = value.Sign; // remainder has the same sign as value here
        var absRem2 = BigInteger.Abs(remainder) * 2;
        var absInc = BigInteger.Abs(increment);

        bool roundUp = roundingMode switch
        {
            "trunc" => false,
            "expand" => true,
            "ceil" => sign > 0,
            "floor" => sign < 0,
            "halfExpand" => absRem2 >= absInc,
            "halfTrunc" => absRem2 > absInc,
            "halfCeil" => sign > 0 ? absRem2 >= absInc : absRem2 > absInc,
            "halfFloor" => sign < 0 ? absRem2 >= absInc : absRem2 > absInc,
            "halfEven" => absRem2 > absInc || (absRem2 == absInc && !BigInteger.Abs(quotient).IsEven),
            _ => absRem2 >= absInc,
        };

        if (roundUp)
            quotient += sign >= 0 ? 1 : -1;

        return quotient * increment;
    }
}
