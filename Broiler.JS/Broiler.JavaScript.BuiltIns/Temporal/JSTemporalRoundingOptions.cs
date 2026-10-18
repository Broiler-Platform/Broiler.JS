using Broiler.ECMAScript.Numbers;
using Broiler.ECMAScript.Temporal;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Temporal;

// The reads of the Temporal rounding options: GetRoundingModeOption, GetRoundingIncrementOption,
// GetTemporalUnitValuedOption and ToIntegerWithTruncation. Each reads its option with Get, converts it with the
// specification's coercion (ToString or ToNumber, exposed here as the StringValue / DoubleValue members, so that a
// Symbol is a TypeError) and refuses a value with this engine's RangeError.
//
// The rules are the shared library's (Broiler.ECMAScript.Algorithms): the rounding is ExactRounding (milestone
// SJL-4.5), and the units, their groups, the increments and their maxima, and the rules of each round, until, since
// and total are TemporalOptionRules (milestone SJL-10.5, part C). The engine's copies of them (the unit order and
// normalisation, the maximum increments, ValidateTemporalRoundingIncrement, and NudgeToCalendarUnit's window) were
// removed with the relative machinery they served.
internal static class TemporalRoundingOptions
{
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

    // GetRoundingIncrementOption: undefined is 1; any other value is converted with ToNumber, then the library's rule
    // (TemporalOptionRules.TryParseRoundingIncrement): NaN and the infinities refused, the integer truncated, 1 to 10^9.
    internal static long GetRoundingIncrement(JSObject options)
    {
        var v = options[KeyStrings.GetOrCreate("roundingIncrement")];
        if (v.IsUndefined) return TemporalOptionRules.DefaultRoundingIncrement;

        var number = v.DoubleValue;
        if (!TemporalOptionRules.TryParseRoundingIncrement(number, out var increment))
        {
            throw JSEngine.NewRangeError(double.IsFinite(number)
                ? "Temporal: roundingIncrement out of range"
                : "Temporal: roundingIncrement must be a finite number");
        }

        return increment;
    }

    // GetTemporalUnitValuedOption: undefined is unset (or, where the option is required, the RangeError); any other
    // value is converted with ToString and must be a unit's singular or plural name or "auto" (the library's
    // TemporalOptionRules.TryParseUnit), else the RangeError. Which units an operation allows is its rule's, checked
    // after its last read.
    internal static TemporalUnit GetUnit(JSObject options, string key, string typeName, bool required = false)
    {
        var v = options[KeyStrings.GetOrCreate(key)];
        if (v.IsUndefined)
            return required ? throw JSEngine.NewRangeError($"{typeName}: {key} is required") : TemporalUnit.Unset;

        return ParseUnit(v.StringValue, key, typeName);
    }

    // The value of a unit-valued option already converted to a String (a round or total argument that is a String names
    // its one option): a unit's singular or plural name or "auto", else the RangeError.
    internal static TemporalUnit ParseUnit(string text, string key, string typeName) =>
        TemporalOptionRules.TryParseUnit(text, out var unit) ? unit : throw JSEngine.NewRangeError($"{typeName}: invalid {key} \"{text}\"");
}
