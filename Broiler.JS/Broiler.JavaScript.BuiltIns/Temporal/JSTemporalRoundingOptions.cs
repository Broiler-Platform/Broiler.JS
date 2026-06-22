using System;
using System.Numerics;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Temporal;

// Shared option-reading and rounding helpers for the calendar-independent (time-based) Temporal
// types — Temporal.Instant and Temporal.Duration. These mirror the abstract operations in the
// Temporal proposal: GetRoundingModeOption, GetRoundingIncrementOption,
// ValidateTemporalRoundingIncrement, GetTemporalFractionalSecondDigitsOption and
// RoundNumberToIncrement.
//
// Option values are read through the spec coercions (ToString / ToNumber, exposed here as the
// StringValue / DoubleValue members) so that a Symbol option value raises a TypeError, an
// unrecognized string raises a RangeError, and so on — matching the test262 expectations for the
// "*-wrong-type" / "*-invalid-string" / "*-nan" cases.
internal static class TemporalRoundingOptions
{
    // year … nanosecond, ordered from largest to smallest magnitude.
    internal static readonly string[] UnitOrder =
        { "year", "month", "week", "day", "hour", "minute", "second", "millisecond", "microsecond", "nanosecond" };

    internal static int UnitIndex(string unit) => System.Array.IndexOf(UnitOrder, unit);

    // Canonicalizes a (possibly plural) time-unit name to its singular form. Calendar units
    // (year/month/week/day) are rejected, since these helpers only serve the time-based types.
    // "auto" is accepted only when allowAuto is set (used for the largestUnit option).
    internal static string NormalizeTimeUnit(string u, bool allowAuto) => u switch
    {
        "auto" when allowAuto => "auto",
        "hour" or "hours" => "hour",
        "minute" or "minutes" => "minute",
        "second" or "seconds" => "second",
        "millisecond" or "milliseconds" => "millisecond",
        "microsecond" or "microseconds" => "microsecond",
        "nanosecond" or "nanoseconds" => "nanosecond",
        _ => throw JSEngine.NewRangeError($"Temporal: invalid unit \"{u}\""),
    };

    // Canonicalizes any (possibly plural) temporal unit name — calendar units included — to its singular
    // form, accepting "auto" only when allowAuto is set. GetTemporalUnitValuedOption performs only this
    // value coercion; the difference-settings readers must coerce *every* unit option (and read the
    // rounding options between them) before rejecting one that is outside the type's allowed unit group,
    // so a disallowed-but-recognized unit (e.g. "week" for Temporal.Instant) must not throw here.
    internal static string NormalizeAnyUnit(string u, bool allowAuto) => u switch
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
        _ => throw JSEngine.NewRangeError($"Temporal: invalid unit \"{u}\""),
    };

    // GetRoundingModeOption.
    internal static string GetRoundingMode(JSObject options, string fallback)
    {
        var v = options[KeyStrings.GetOrCreate("roundingMode")];
        if (v.IsUndefined) return fallback;
        return NormalizeRoundingMode(v.StringValue);
    }

    internal static string NormalizeRoundingMode(string mode) => mode switch
    {
        "ceil" or "floor" or "expand" or "trunc"
            or "halfCeil" or "halfFloor" or "halfExpand" or "halfTrunc" or "halfEven" => mode,
        _ => throw JSEngine.NewRangeError($"Temporal: invalid roundingMode \"{mode}\""),
    };

    // NegateRoundingMode: a "since" difference negates the operands, so the rounding mode is mirrored
    // (ceil ↔ floor, halfCeil ↔ halfFloor); the symmetric modes are unchanged.
    internal static string NegateRoundingMode(string mode) => mode switch
    {
        "ceil" => "floor",
        "floor" => "ceil",
        "halfCeil" => "halfFloor",
        "halfFloor" => "halfCeil",
        _ => mode,
    };

    // GetUnsignedRoundingMode: collapses the nine signed rounding modes to the direction they imply for a
    // value of the given sign (zero = toward zero, infinity = away from zero, half-* = the tie-break).
    internal static string UnsignedRoundingMode(string mode, bool negative) => mode switch
    {
        "ceil" => negative ? "zero" : "infinity",
        "floor" => negative ? "infinity" : "zero",
        "expand" => "infinity",
        "trunc" => "zero",
        "halfCeil" => negative ? "half-zero" : "half-infinity",
        "halfFloor" => negative ? "half-infinity" : "half-zero",
        "halfExpand" => "half-infinity",
        "halfTrunc" => "half-zero",
        "halfEven" => "half-even",
        _ => "half-infinity",
    };

    // Given cmp = sign(2·|numerator| − |denominator|) and whether the lower boundary's unit count is even,
    // whether ApplyUnsignedRoundingMode picks the END (upper) boundary rather than the start (lower) one.
    internal static bool ApplyRoundingPicksEnd(int cmp, bool even, string unsignedMode) => unsignedMode switch
    {
        "zero" => false,
        "infinity" => true,
        "half-zero" => cmp > 0,
        "half-infinity" => cmp >= 0,
        "half-even" => cmp > 0 || (cmp == 0 && !even),
        _ => false,
    };

    // GetRoundingIncrementOption: ToIntegerWithTruncation, then a 1 … 10^9 range check.
    internal static int GetRoundingIncrement(JSObject options)
    {
        var v = options[KeyStrings.GetOrCreate("roundingIncrement")];
        if (v.IsUndefined) return 1;

        var n = v.DoubleValue; // ToNumber: a Symbol raises a TypeError here.
        if (double.IsNaN(n) || double.IsInfinity(n))
            throw JSEngine.NewRangeError("Temporal: roundingIncrement must be a finite number");

        var increment = Math.Truncate(n);
        if (increment < 1 || increment > 1_000_000_000)
            throw JSEngine.NewRangeError("Temporal: roundingIncrement out of range");
        return (int)increment;
    }

    // MaximumTemporalDurationRoundingIncrement: the fixed divisor a time unit's rounding increment
    // must divide into (24 h/day boundary for hours, 60 for minutes/seconds, 1000 for the sub-second
    // units). The calendar units (year/month/week/day) have no fixed divisor and return 0 (the
    // caller skips the divides-evenly check for them).
    internal static long MaximumRoundingIncrement(string unit) => unit switch
    {
        "hour" => 24,
        "minute" or "second" => 60,
        "millisecond" or "microsecond" or "nanosecond" => 1000,
        _ => 0,
    };

    // ValidateTemporalRoundingIncrement: the increment must not exceed `dividend` (or dividend − 1
    // when not inclusive) and must divide it evenly.
    internal static void ValidateRoundingIncrement(long increment, long dividend, bool inclusive)
    {
        var maximum = inclusive ? dividend : dividend - 1;
        if (increment > maximum)
            throw JSEngine.NewRangeError($"Temporal: roundingIncrement {increment} is out of range");
        if (dividend % increment != 0)
            throw JSEngine.NewRangeError($"Temporal: roundingIncrement {increment} does not divide evenly into the next unit");
    }

    // GetTemporalFractionalSecondDigitsOption: returns -1 for "auto", otherwise 0 … 9.
    internal static int GetFractionalSecondDigits(JSObject options)
    {
        var v = options[KeyStrings.GetOrCreate("fractionalSecondDigits")];
        if (v.IsUndefined) return -1;

        if (!v.IsNumber)
        {
            if (v.StringValue != "auto")
                throw JSEngine.NewRangeError("Temporal: fractionalSecondDigits must be \"auto\" or an integer 0..9");
            return -1;
        }

        var n = v.DoubleValue;
        if (double.IsNaN(n) || double.IsInfinity(n))
            throw JSEngine.NewRangeError("Temporal: fractionalSecondDigits must be finite");

        var digits = (int)Math.Floor(n);
        if (digits < 0 || digits > 9)
            throw JSEngine.NewRangeError("Temporal: fractionalSecondDigits out of range (0..9)");
        return digits;
    }

    // 10^exponent for 0 ≤ exponent ≤ 9, as a nanosecond count.
    internal static long Pow10(int exponent)
    {
        long result = 1;
        for (var i = 0; i < exponent; i++) result *= 10;
        return result;
    }

    // RoundNumberToIncrement: rounds `value` to the nearest multiple of `increment` under the
    // requested rounding mode (the same nine modes as RoundDuration).
    internal static BigInteger RoundToIncrement(BigInteger value, BigInteger increment, string roundingMode)
    {
        var quotient = BigInteger.DivRem(value, increment, out var remainder);
        if (remainder == 0) return value;

        var sign = value.Sign; // remainder shares the sign of value
        var absRem2 = BigInteger.Abs(remainder) * 2;
        var absInc = BigInteger.Abs(increment);

        var roundUp = roundingMode switch
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

    // RoundNumberToIncrementAsIfPositive: rounds toward the two surrounding multiples as though the
    // value were non-negative, so the direction of "floor"/"trunc" (down, toward the Big Bang) and
    // "ceil"/"expand" (up) never depends on the sign. Temporal.Instant and Temporal.ZonedDateTime
    // round an absolute epoch-nanosecond count this way (test262 .../rounding-direction).
    internal static BigInteger RoundToIncrementAsIfPositive(BigInteger value, BigInteger increment, string roundingMode)
    {
        var quotient = BigInteger.DivRem(value, increment, out var remainder);
        if (remainder == 0) return value;

        // Floor the quotient so 0 < remainder < increment (the lower surrounding multiple).
        if (remainder < 0) { quotient -= 1; remainder += increment; }
        var rem2 = remainder * 2;

        var roundUp = roundingMode switch
        {
            "trunc" or "floor" => false,
            "expand" or "ceil" => true,
            "halfExpand" or "halfCeil" => rem2 >= increment,
            "halfTrunc" or "halfFloor" => rem2 > increment,
            "halfEven" => rem2 > increment || (rem2 == increment && !quotient.IsEven),
            _ => rem2 >= increment,
        };

        if (roundUp)
            quotient += 1;

        return quotient * increment;
    }
}
