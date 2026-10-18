using Broiler.ECMAScript.Temporal;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Temporal;

// Serialization: every Temporal string form is the shared library's TemporalFormat, and the rules of the
// toString options (calendarName, fractionalSecondDigits, offset, timeZoneName and the smallestUnit precision)
// its TemporalOptionRules. What stays here is each option's read - Get, ToString, in the order each method
// reads them - and this engine's RangeError for a value the rule refuses.
internal static partial class TemporalAdapter
{
    /// <summary>
    /// GetTemporalShowCalendarNameOption of an options argument (GetOptionsObject first): undefined is
    /// <c>auto</c>, a value that is neither undefined nor an object is the <c>TypeError</c>, and a
    /// <c>calendarName</c> the rule refuses is the <c>RangeError</c>.
    /// </summary>
    internal static ShowCalendarName ReadShowCalendarName(JSValue options)
    {
        if (options == null || options.IsUndefined)
            return ShowCalendarName.Auto;
        if (options is not JSObject optionsObject)
            throw JSEngine.NewTypeError("Temporal options must be an object or undefined");

        var value = optionsObject[KeyStrings.GetOrCreate("calendarName")];
        if (value.IsUndefined)
            return ShowCalendarName.Auto;

        var name = value.StringValue;
        if (!TemporalOptionRules.TryParseShowCalendarName(name, out var show))
            throw JSEngine.NewRangeError($"Temporal: invalid calendarName \"{name}\"");
        return show;
    }

    /// <summary>
    /// GetTemporalFractionalSecondDigitsOption: undefined is <see cref="TemporalFormat.AutoPrecision"/>; a Number is
    /// floored and must be 0 to 9 (NaN and the infinities refused); anything else is converted with <c>ToString</c> and
    /// must be <c>"auto"</c>. A refusal is the <c>RangeError</c>.
    /// </summary>
    internal static int ReadFractionalSecondDigits(JSObject options)
    {
        var value = options[KeyStrings.GetOrCreate("fractionalSecondDigits")];
        if (value.IsUndefined)
            return TemporalFormat.AutoPrecision;

        if (!value.IsNumber)
        {
            if (!TemporalOptionRules.IsFractionalSecondDigitsAuto(value.StringValue))
                throw JSEngine.NewRangeError("Temporal: fractionalSecondDigits must be \"auto\" or an integer 0..9");
            return TemporalFormat.AutoPrecision;
        }

        if (!TemporalOptionRules.TryParseFractionalSecondDigits(value.DoubleValue, out var digits))
            throw JSEngine.NewRangeError("Temporal: fractionalSecondDigits must be a finite number from 0 to 9");
        return digits;
    }

    /// <summary>GetTemporalShowOffsetOption, the <c>offset</c> option of <c>ZonedDateTime.prototype.toString</c>: <c>auto</c> or <c>never</c>.</summary>
    internal static ShowOffset ReadShowOffset(JSObject options)
    {
        var value = options[KeyStrings.GetOrCreate("offset")];
        if (value.IsUndefined)
            return ShowOffset.Auto;

        var text = value.StringValue;
        if (!TemporalOptionRules.TryParseShowOffset(text, out var show))
            throw JSEngine.NewRangeError($"Temporal: invalid offset display option \"{text}\"");
        return show;
    }

    /// <summary>GetTemporalShowTimeZoneNameOption, the <c>timeZoneName</c> option: <c>auto</c>, <c>never</c> or <c>critical</c>.</summary>
    internal static ShowTimeZoneName ReadShowTimeZoneName(JSObject options)
    {
        var value = options[KeyStrings.GetOrCreate("timeZoneName")];
        if (value.IsUndefined)
            return ShowTimeZoneName.Auto;

        var text = value.StringValue;
        if (!TemporalOptionRules.TryParseShowTimeZoneName(text, out var show))
            throw JSEngine.NewRangeError($"Temporal: invalid timeZoneName display option \"{text}\"");
        return show;
    }

    /// <summary>
    /// A <c>toString</c>'s <c>smallestUnit</c>, read and converted with <c>ToString</c> at its place in the reads, but not
    /// yet validated: <see cref="ToStringPrecision"/> validates it after the method's last read.
    /// </summary>
    internal static string ReadSmallestUnitText(JSObject options)
    {
        var value = options[KeyStrings.GetOrCreate("smallestUnit")];
        return value.IsUndefined ? null : value.StringValue;
    }

    /// <summary>
    /// The precision a <c>toString</c> writes: <c>ValidateTemporalUnitValue</c> of the <c>smallestUnit</c> read (a time
    /// unit; neither <c>hour</c>, nor <c>minute</c> for a duration), then <c>ToSecondsStringPrecisionRecord</c> - the
    /// shared library's rules. A unit the rules refuse is the <c>RangeError</c>.
    /// </summary>
    /// <param name="smallestUnit">The <c>smallestUnit</c> read, or <see langword="null"/> where it was undefined.</param>
    /// <param name="fractionalSecondDigits">The digits read, or <see cref="TemporalFormat.AutoPrecision"/>.</param>
    /// <param name="duration">Whether the method is <c>Duration.prototype.toString</c>.</param>
    /// <param name="typeName">The type the message names, for example <c>Temporal.PlainTime</c>.</param>
    internal static SecondsStringPrecision ToStringPrecision(string smallestUnit, int fractionalSecondDigits, bool duration, string typeName)
    {
        var unit = TemporalUnit.Unset;
        if (smallestUnit != null &&
            (!TemporalOptionRules.TryParseUnit(smallestUnit, out unit) || !TemporalOptionRules.IsValidToStringSmallestUnit(unit, duration)))
            throw JSEngine.NewRangeError($"{typeName}.toString: invalid smallestUnit \"{smallestUnit}\"");

        return TemporalOptionRules.ToSecondsStringPrecisionRecord(unit, fractionalSecondDigits);
    }
}
