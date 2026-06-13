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
}
