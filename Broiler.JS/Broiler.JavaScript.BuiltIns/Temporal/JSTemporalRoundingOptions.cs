using System;
using System.Numerics;
using Broiler.ECMAScript.Numbers;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Temporal;

// Shared option-reading helpers for the Temporal types. These mirror the abstract operations in the
// Temporal proposal: GetRoundingModeOption, GetRoundingIncrementOption,
// ValidateTemporalRoundingIncrement and GetTemporalFractionalSecondDigitsOption.
//
// The rounding itself is the shared library's (Broiler.ECMAScript.Algorithms, ExactRounding and
// RoundingMode; milestone SJL-4.5): RoundNumberToIncrement, RoundNumberToIncrementAsIfPositive,
// NegateRoundingMode, GetUnsignedRoundingMode and ApplyUnsignedRoundingMode. The engine's copies of
// them (NormalizeRoundingMode, NegateRoundingMode, UnsignedRoundingMode, ApplyRoundingPicksEnd,
// RoundToIncrement and RoundToIncrementAsIfPositive) were removed; what stays here is the option
// reading, its coercion and its RangeError, and NudgeToCalendarUnit's window in the library's terms.
//
// Option values are read through the spec coercions (ToString / ToNumber, exposed here as the
// StringValue / DoubleValue members) so that a Symbol option value raises a TypeError, an
// unrecognized string raises a RangeError, and so on — matching the test262 expectations for the
// "*-wrong-type" / "*-invalid-string" / "*-nan" cases.
internal static class TemporalRoundingOptions
{
    // year … nanosecond, ordered from largest to smallest magnitude.
    internal static readonly string[] UnitOrder =
        ["year", "month", "week", "day", "hour", "minute", "second", "millisecond", "microsecond", "nanosecond"];

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

    // GetRoundingModeOption (Temporal 13.8): undefined is the fallback; any other value is converted
    // with ToString (a Symbol is a TypeError) and must be one of the nine names, which the library
    // parses; any other string is the RangeError, raised here.
    internal static RoundingMode GetRoundingMode(JSObject options, RoundingMode fallback)
    {
        var v = options[KeyStrings.GetOrCreate("roundingMode")];
        if (v.IsUndefined) return fallback;

        var text = v.StringValue;
        if (!ExactRounding.TryParseRoundingMode(text, out var mode))
            throw JSEngine.NewRangeError($"Temporal: invalid roundingMode \"{text}\"");
        return mode;
    }

    // ToIntegerWithTruncation (Temporal): ToNumber (a Symbol or a BigInt raises the TypeError), a
    // RangeError with the caller's message for NaN and the infinities, then ToIntegerOrInfinity, the
    // shared library's (SJL-4.5). Each caller keeps its own narrowing of the integer.
    internal static double ToIntegerWithTruncation(JSValue value, string message)
    {
        var number = value.DoubleValue;
        if (double.IsNaN(number) || double.IsInfinity(number))
            throw JSEngine.NewRangeError(message);

        return IntegerConversions.ToIntegerOrInfinity(number);
    }

    // GetRoundingIncrementOption: ToIntegerWithTruncation, then a 1 … 10^9 range check.
    internal static int GetRoundingIncrement(JSObject options)
    {
        var v = options[KeyStrings.GetOrCreate("roundingIncrement")];
        if (v.IsUndefined) return 1;

        var increment = ToIntegerWithTruncation(v, "Temporal: roundingIncrement must be a finite number");
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

    // NudgeToCalendarUnit (Temporal): total = r1 + progress × increment × sign, as an exact ratio,
    // where progress = progressed / span is the destination's distance from the window's start over
    // the window's length (both magnitudes, span > 0).
    internal static (BigInteger numerator, BigInteger denominator) NudgeTotal(
        long r1, long increment, int sign, BigInteger progressed, BigInteger span)
        => ((BigInteger)r1 * span + sign * increment * progressed, span);

    // NudgeToCalendarUnit's rounding of a progress below 1 (a progress of 1 rounds to the end, which
    // the caller decides first, as the operation does): the library's ApplyUnsignedRoundingMode over
    // the window [|r1|, |r2|] for the total's magnitude, under the unsigned mode the library's
    // GetUnsignedRoundingMode gives for the sign. Whether the rounded unit is |r2|, the window's end.
    internal static bool NudgeRoundsToEnd(
        BigInteger totalNumerator, BigInteger totalDenominator, long r1, long r2, int sign, RoundingMode mode)
    {
        var end = BigInteger.Abs(r2);
        var rounded = ExactRounding.ApplyUnsignedRoundingMode(
            BigInteger.Abs(totalNumerator), totalDenominator, BigInteger.Abs(r1), end,
            ExactRounding.GetUnsignedRoundingMode(mode, sign < 0));
        return rounded == end;
    }
}
