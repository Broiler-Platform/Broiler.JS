// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   18
// Annotated:        18/18
// Exempt:           9
// Human-reviewed:   0/18
// IP risk:          Low
// Security risk:    Medium
// Criteria:         0/0
// Resource impact:  0/10 max
// Unverified:       18
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using Broiler.ECMAScript.Numbers;

namespace Broiler.ECMAScript.Temporal;

/// <summary>The <c>calendarName</c> option of the <c>toString</c> methods (<c>GetTemporalShowCalendarNameOption</c>).</summary>
// Broiler-AI:           Origin=Ported; Spec=Temporal GetTemporalShowCalendarNameOption via Broiler.VM JsTemporal.ShowCalendar; IP=Low; Security=None; Resources=0; Fingerprint=C65215
// Broiler-Human:        PENDING
public enum ShowCalendarName
{
    /// <summary><c>"auto"</c>, the default: the annotation for every calendar but <c>iso8601</c>.</summary>
    Auto = 0,

    /// <summary><c>"always"</c>: the annotation for every calendar.</summary>
    Always = 1,

    /// <summary><c>"never"</c>: no annotation.</summary>
    Never = 2,

    /// <summary><c>"critical"</c>: the annotation for every calendar, with the critical flag.</summary>
    Critical = 3,
}

/// <summary>The <c>timeZoneName</c> option of <c>ZonedDateTime.prototype.toString</c> (<c>GetTemporalShowTimeZoneNameOption</c>).</summary>
// Broiler-AI:           Origin=Ported; Spec=Temporal GetTemporalShowTimeZoneNameOption via Broiler.VM JsTemporal.ShowTimeZone; IP=Low; Security=None; Resources=0; Fingerprint=AF3A5D
// Broiler-Human:        PENDING
public enum ShowTimeZoneName
{
    /// <summary><c>"auto"</c>, the default: the time zone annotation.</summary>
    Auto = 0,

    /// <summary><c>"never"</c>: no time zone annotation.</summary>
    Never = 1,

    /// <summary><c>"critical"</c>: the time zone annotation with the critical flag.</summary>
    Critical = 2,
}

/// <summary>The <c>offset</c> option of <c>ZonedDateTime.prototype.toString</c> (<c>GetTemporalShowOffsetOption</c>).</summary>
// Broiler-AI:           Origin=Ported; Spec=Temporal GetTemporalShowOffsetOption via Broiler.VM JsTemporal.ShowOffset; IP=Low; Security=None; Resources=0; Fingerprint=7FE9A9
// Broiler-Human:        PENDING
public enum ShowOffset
{
    /// <summary><c>"auto"</c>, the default: the offset, rounded to the minute.</summary>
    Auto = 0,

    /// <summary><c>"never"</c>: no offset.</summary>
    Never = 1,
}

/// <summary>
/// A ToSecondsStringPrecisionRecord of the Temporal proposal: how many digits of the seconds a string form writes, and the
/// unit and increment the value is rounded to first.
/// </summary>
/// <param name="Precision">
/// The digits after the seconds' point, 0 to 9; <see cref="TemporalFormat.AutoPrecision"/> for as many as are not zero;
/// <see cref="TemporalFormat.MinutePrecision"/> for no seconds at all.
/// </param>
/// <param name="Unit">The unit rounded to: minute, second, millisecond, microsecond or nanosecond.</param>
/// <param name="Increment">The increment of that unit: 1, 10 or 100.</param>
// Broiler-AI:           Origin=Ported; Spec=Temporal ToSecondsStringPrecisionRecord via Broiler.VM JsPrecision; IP=Low; Security=None; Resources=0; Fingerprint=378538
// Broiler-Human:        PENDING
public readonly record struct SecondsStringPrecision(int Precision, TemporalUnit Unit, long Increment);

/// <summary>
/// The rules of the Temporal options the records, balancing and serialization sub-areas use (JSD-0078, "The options'
/// rules"): each option's allowed values and default, as data. The <b>reading</b> stays each engine's - <c>Get</c>,
/// <c>ToString</c> and the order the specification reads the options in - and the engine calls these after its reads,
/// mapping a refusal to its <c>RangeError</c>.
/// </summary>
/// <remarks>
/// The options here: <c>overflow</c> (B3); and the <c>toString</c> options <c>calendarName</c>, <c>timeZoneName</c>,
/// <c>offset</c>, <c>fractionalSecondDigits</c>, <c>smallestUnit</c> and <c>roundingMode</c>, with their precision
/// (S2, S4, S5, S7, S11). The class is partial: the durations sub-area adds the rules of the rounding and difference
/// options to it.
/// </remarks>
// Broiler-AI:           Origin=Ported; Spec=JSD-0078 options rules, Temporal option operations via Broiler.VM JsTemporal.Options; IP=Low; Security=Medium; Resources=0; Fingerprint=F3C848
// Broiler-Human:        PENDING
public static partial class TemporalOptionRules
{
    /// <summary>The <c>overflow</c> option's default, <c>"constrain"</c>.</summary>
    // Broiler-AI:           Origin=Ported; Spec=Temporal GetTemporalOverflowOption via Broiler.VM JsTemporal.Reject; IP=Low; Security=None; Resources=0; Fingerprint=AD6D2B
    // Broiler-Human:        PENDING
    public const TemporalOverflow DefaultOverflow = TemporalOverflow.Constrain;

    /// <summary>The <c>roundingMode</c> default of every <c>toString</c>, <c>"trunc"</c>; the names are SJL-4's <see cref="ExactRounding.TryParseRoundingMode"/>.</summary>
    // Broiler-AI:           Origin=Ported; Spec=Temporal GetRoundingModeOption via Broiler.VM JsTemporal.RoundingModeOption; IP=Low; Security=None; Resources=0; Fingerprint=EA4364
    // Broiler-Human:        PENDING
    public const RoundingMode DefaultToStringRoundingMode = RoundingMode.Trunc;

    /// <summary>Reads the <c>overflow</c> option's string: <c>GetTemporalOverflowOption</c>'s allowed values, compared exactly.</summary>
    /// <param name="value">The option's string, after the engine's <c>ToString</c>.</param>
    /// <param name="overflow">The overflow; <see cref="DefaultOverflow"/> when the string is not one.</param>
    /// <returns>Whether the string is <c>"constrain"</c> or <c>"reject"</c>; any other is the specification's <c>RangeError</c>.</returns>
    // Broiler-AI:           Origin=Ported; Spec=Temporal GetTemporalOverflowOption via Broiler.VM JsTemporal.Reject; IP=Low; Security=Medium; Resources=0; Fingerprint=29A46F
    // Broiler-Human:        PENDING
    public static bool TryParseOverflow(ReadOnlySpan<char> value, out TemporalOverflow overflow)
    {
        overflow = value switch
        {
            "constrain" => TemporalOverflow.Constrain,
            "reject" => TemporalOverflow.Reject,
            _ => (TemporalOverflow)(-1),
        };

        if (overflow >= 0)
            return true;

        overflow = DefaultOverflow;
        return false;
    }

    /// <summary>Reads the <c>calendarName</c> option's string: <c>GetTemporalShowCalendarNameOption</c>'s allowed values, compared exactly.</summary>
    /// <param name="value">The option's string, after the engine's <c>ToString</c>.</param>
    /// <param name="show">The display; <see cref="ShowCalendarName.Auto"/>, the default, when the string is not one.</param>
    /// <returns>Whether the string is <c>"auto"</c>, <c>"always"</c>, <c>"never"</c> or <c>"critical"</c>.</returns>
    // Broiler-AI:           Origin=Ported; Spec=Temporal GetTemporalShowCalendarNameOption via Broiler.VM JsTemporal.ShowCalendar; IP=Low; Security=Medium; Resources=0; Fingerprint=436AD4
    // Broiler-Human:        PENDING
    public static bool TryParseShowCalendarName(ReadOnlySpan<char> value, out ShowCalendarName show)
    {
        show = value switch
        {
            "auto" => ShowCalendarName.Auto,
            "always" => ShowCalendarName.Always,
            "never" => ShowCalendarName.Never,
            "critical" => ShowCalendarName.Critical,
            _ => (ShowCalendarName)(-1),
        };

        if (show >= 0)
            return true;

        show = ShowCalendarName.Auto;
        return false;
    }

    /// <summary>Reads the <c>timeZoneName</c> option's string: <c>GetTemporalShowTimeZoneNameOption</c>'s allowed values, compared exactly.</summary>
    /// <param name="value">The option's string, after the engine's <c>ToString</c>.</param>
    /// <param name="show">The display; <see cref="ShowTimeZoneName.Auto"/>, the default, when the string is not one.</param>
    /// <returns>Whether the string is <c>"auto"</c>, <c>"never"</c> or <c>"critical"</c>.</returns>
    // Broiler-AI:           Origin=Ported; Spec=Temporal GetTemporalShowTimeZoneNameOption via Broiler.VM JsTemporal.ShowTimeZone; IP=Low; Security=Medium; Resources=0; Fingerprint=D69B1A
    // Broiler-Human:        PENDING
    public static bool TryParseShowTimeZoneName(ReadOnlySpan<char> value, out ShowTimeZoneName show)
    {
        show = value switch
        {
            "auto" => ShowTimeZoneName.Auto,
            "never" => ShowTimeZoneName.Never,
            "critical" => ShowTimeZoneName.Critical,
            _ => (ShowTimeZoneName)(-1),
        };

        if (show >= 0)
            return true;

        show = ShowTimeZoneName.Auto;
        return false;
    }

    /// <summary>Reads the <c>toString</c> <c>offset</c> option's string: <c>GetTemporalShowOffsetOption</c>'s allowed values, compared exactly.</summary>
    /// <param name="value">The option's string, after the engine's <c>ToString</c>.</param>
    /// <param name="show">The display; <see cref="ShowOffset.Auto"/>, the default, when the string is not one.</param>
    /// <returns>Whether the string is <c>"auto"</c> or <c>"never"</c>.</returns>
    // Broiler-AI:           Origin=Ported; Spec=Temporal GetTemporalShowOffsetOption via Broiler.VM JsTemporal.ShowOffset; IP=Low; Security=Medium; Resources=0; Fingerprint=3A282F
    // Broiler-Human:        PENDING
    public static bool TryParseShowOffset(ReadOnlySpan<char> value, out ShowOffset show)
    {
        show = value switch
        {
            "auto" => ShowOffset.Auto,
            "never" => ShowOffset.Never,
            _ => (ShowOffset)(-1),
        };

        if (show >= 0)
            return true;

        show = ShowOffset.Auto;
        return false;
    }

    /// <summary>
    /// The <c>fractionalSecondDigits</c> option when its value is a Number: <c>GetTemporalFractionalSecondDigitsOption</c>'s
    /// steps for a Number - finite, floored, and from 0 to 9.
    /// </summary>
    /// <param name="value">The option's value, a Number.</param>
    /// <param name="digits">The digit count; <see cref="TemporalFormat.AutoPrecision"/> when the value is refused.</param>
    /// <returns>Whether the value is accepted; NaN, an infinity, or a floor outside 0 to 9 is the specification's <c>RangeError</c>.</returns>
    /// <remarks>
    /// A value that is not a Number is <c>ToString</c>'d by the engine and accepted only as <c>"auto"</c>
    /// (<see cref="IsFractionalSecondDigitsAuto"/>); an undefined value is <see cref="TemporalFormat.AutoPrecision"/>.
    /// </remarks>
    // Broiler-AI:           Origin=Ported; Spec=Temporal GetTemporalFractionalSecondDigitsOption via Broiler.VM JsTemporal.FractionalSecondDigits; IP=Low; Security=Medium; Resources=0; Fingerprint=B83F0E
    // Broiler-Human:        PENDING
    public static bool TryParseFractionalSecondDigits(double value, out int digits)
    {
        digits = TemporalFormat.AutoPrecision;
        if (!double.IsFinite(value))
            return false;

        var count = Math.Floor(value);
        if (count is < 0 or > 9)
            return false;

        digits = (int)count;
        return true;
    }

    /// <summary>
    /// The <c>fractionalSecondDigits</c> option when its value is not a Number: <c>GetTemporalFractionalSecondDigitsOption</c>
    /// accepts the string <c>"auto"</c> only, compared exactly.
    /// </summary>
    /// <param name="value">The option's string, after the engine's <c>ToString</c>.</param>
    /// <returns>Whether it is <c>"auto"</c>, whose digits are <see cref="TemporalFormat.AutoPrecision"/>; any other is the <c>RangeError</c>.</returns>
    // Broiler-AI:           Origin=Ported; Spec=Temporal GetTemporalFractionalSecondDigitsOption via Broiler.VM JsTemporal.FractionalSecondDigits; IP=Low; Security=Medium; Resources=0; Fingerprint=2A06D8
    // Broiler-Human:        PENDING
    public static bool IsFractionalSecondDigitsAuto(ReadOnlySpan<char> value) => value.SequenceEqual("auto");

    /// <summary>
    /// Reads a unit-valued option's string: <c>GetTemporalUnitValuedOption</c>'s allowed values - the singular and plural
    /// names of Table 21 and <c>"auto"</c>, compared exactly.
    /// </summary>
    /// <param name="value">The option's string, after the engine's <c>ToString</c>.</param>
    /// <param name="unit">The unit, or <see cref="TemporalUnit.Auto"/>; <see cref="TemporalUnit.Unset"/> when the string is not one.</param>
    /// <returns>Whether the string is allowed; any other is the <c>RangeError</c> the read throws. The unit's group is validated afterwards.</returns>
    // Broiler-AI:           Origin=Ported; Spec=Temporal GetTemporalUnitValuedOption via Broiler.VM JsTemporal.UnitOption; IP=Low; Security=Medium; Resources=0; Fingerprint=C86BBE
    // Broiler-Human:        PENDING
    public static bool TryParseUnit(ReadOnlySpan<char> value, out TemporalUnit unit)
    {
        unit = value switch
        {
            "year" or "years" => TemporalUnit.Year,
            "month" or "months" => TemporalUnit.Month,
            "week" or "weeks" => TemporalUnit.Week,
            "day" or "days" => TemporalUnit.Day,
            "hour" or "hours" => TemporalUnit.Hour,
            "minute" or "minutes" => TemporalUnit.Minute,
            "second" or "seconds" => TemporalUnit.Second,
            "millisecond" or "milliseconds" => TemporalUnit.Millisecond,
            "microsecond" or "microseconds" => TemporalUnit.Microsecond,
            "nanosecond" or "nanoseconds" => TemporalUnit.Nanosecond,
            "auto" => TemporalUnit.Auto,
            _ => TemporalUnit.Unset,
        };
        return unit != TemporalUnit.Unset;
    }

    /// <summary>
    /// Whether a <c>toString</c>'s <c>smallestUnit</c> is allowed, after every option is read: <c>ValidateTemporalUnitValue</c>
    /// of the time group with no extra value, then the method's own refusal of <c>hour</c>, and for
    /// <c>Duration.prototype.toString</c> of <c>minute</c> too.
    /// </summary>
    /// <param name="smallestUnit">The unit read, or <see cref="TemporalUnit.Unset"/> where the option was undefined.</param>
    /// <param name="duration">Whether the method is <c>Duration.prototype.toString</c>.</param>
    /// <returns>
    /// Whether the unit is unset, minute (but for a duration), second, millisecond, microsecond or nanosecond; a date unit,
    /// <c>auto</c> and hour are the specification's <c>RangeError</c>.
    /// </returns>
    // Broiler-AI:           Origin=Ported; Spec=Temporal ValidateTemporalUnitValue and the toString methods via Broiler.VM JsTemporal.ValidateUnit; IP=Low; Security=Medium; Resources=0; Fingerprint=BB699B
    // Broiler-Human:        PENDING
    public static bool IsValidToStringSmallestUnit(TemporalUnit smallestUnit, bool duration) => smallestUnit switch
    {
        TemporalUnit.Unset or TemporalUnit.Second or TemporalUnit.Millisecond or TemporalUnit.Microsecond or TemporalUnit.Nanosecond => true,
        TemporalUnit.Minute => !duration,
        _ => false,
    };

    /// <summary>
    /// The precision a <c>toString</c> writes: the proposal's <c>ToSecondsStringPrecisionRecord</c> - the smallest unit's
    /// where one is given, else the digit count's.
    /// </summary>
    /// <param name="smallestUnit">Minute, second, millisecond, microsecond, nanosecond or <see cref="TemporalUnit.Unset"/>.</param>
    /// <param name="fractionalSecondDigits">0 to 9, or <see cref="TemporalFormat.AutoPrecision"/>.</param>
    /// <returns>The precision, the unit and the increment.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The unit or the digit count is outside its domain.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal ToSecondsStringPrecisionRecord via Broiler.VM JsTemporal.Precision; IP=Low; Security=Low; Resources=0; Fingerprint=760507
    // Broiler-Human:        PENDING
    public static SecondsStringPrecision ToSecondsStringPrecisionRecord(TemporalUnit smallestUnit, int fractionalSecondDigits)
    {
        if (fractionalSecondDigits is < TemporalFormat.AutoPrecision or > 9)
            throw new ArgumentOutOfRangeException(nameof(fractionalSecondDigits), fractionalSecondDigits, "The digits are auto or 0 to 9.");

        return smallestUnit switch
        {
            TemporalUnit.Minute => new SecondsStringPrecision(TemporalFormat.MinutePrecision, TemporalUnit.Minute, 1),
            TemporalUnit.Second => new SecondsStringPrecision(0, TemporalUnit.Second, 1),
            TemporalUnit.Millisecond => new SecondsStringPrecision(3, TemporalUnit.Millisecond, 1),
            TemporalUnit.Microsecond => new SecondsStringPrecision(6, TemporalUnit.Microsecond, 1),
            TemporalUnit.Nanosecond => new SecondsStringPrecision(9, TemporalUnit.Nanosecond, 1),
            TemporalUnit.Unset => fractionalSecondDigits switch
            {
                TemporalFormat.AutoPrecision => new SecondsStringPrecision(TemporalFormat.AutoPrecision, TemporalUnit.Nanosecond, 1),
                0 => new SecondsStringPrecision(0, TemporalUnit.Second, 1),
                <= 3 => new SecondsStringPrecision(fractionalSecondDigits, TemporalUnit.Millisecond, Pow10(3 - fractionalSecondDigits)),
                <= 6 => new SecondsStringPrecision(fractionalSecondDigits, TemporalUnit.Microsecond, Pow10(6 - fractionalSecondDigits)),
                _ => new SecondsStringPrecision(fractionalSecondDigits, TemporalUnit.Nanosecond, Pow10(9 - fractionalSecondDigits)),
            },
            _ => throw new ArgumentOutOfRangeException(nameof(smallestUnit), smallestUnit, "The unit is not minute or smaller, or unset."),
        };
    }

    /// <summary>
    /// Whether a rounding increment divides its dividend: the proposal's <c>ValidateTemporalRoundingIncrement</c> - at most
    /// the dividend (less than it unless <paramref name="inclusive"/>), and dividing it evenly.
    /// </summary>
    /// <param name="increment">The increment, positive.</param>
    /// <param name="dividend">The dividend, positive: a unit's maximum increment, or the units in a day.</param>
    /// <param name="inclusive">Whether the increment may equal the dividend.</param>
    /// <returns>Whether the increment is valid; otherwise the specification's <c>RangeError</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The increment or the dividend is not positive.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal ValidateTemporalRoundingIncrement via Broiler.VM JsTemporal.ValidateIncrement; IP=Low; Security=Low; Resources=0; Fingerprint=C94BDA
    // Broiler-Human:        PENDING
    public static bool IsValidRoundingIncrement(long increment, long dividend, bool inclusive)
    {
        if (increment < 1)
            throw new ArgumentOutOfRangeException(nameof(increment), increment, "The increment is not positive.");
        if (dividend < 1)
            throw new ArgumentOutOfRangeException(nameof(dividend), dividend, "The dividend is not positive.");

        var maximum = inclusive ? dividend : dividend - 1;
        return increment <= maximum && dividend % increment == 0;
    }

    // 10 to a small power.
    // Broiler-AI:           Origin=Ported; Spec=Temporal ToSecondsStringPrecisionRecord via Broiler.VM JsTemporal.Pow10; IP=Low; Security=Low; Resources=0; Fingerprint=B73D51
    // Broiler-Human:        PENDING
    private static long Pow10(int exponent)
    {
        var result = 1L;
        for (var i = 0; i < exponent; i++)
            result *= 10;

        return result;
    }
}
