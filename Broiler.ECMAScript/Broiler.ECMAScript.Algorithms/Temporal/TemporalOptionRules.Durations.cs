// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   16
// Annotated:        16/16
// Exempt:           0
// Human-reviewed:   0/16
// IP risk:          Low
// Security risk:    Medium
// Criteria:         0/0
// Resource impact:  0/10 max
// Unverified:       16
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using Broiler.ECMAScript.Numbers;

namespace Broiler.ECMAScript.Temporal;

// The rules of the rounding and difference options (rows D5 and F1): the units each operation allows, their groups and
// defaults, the rounding increments and their maxima, and GetDifferenceSettings' validation after its four reads.
// Ported from the rule half of Broiler.VM's JsTemporal.Options and the option steps of its realm files' round, total,
// until and since; the reads, in the specification's order, stay each engine's.
// Broiler-AI:           Origin=Ported; Spec=JSD-0078 options rules, Temporal s13 via Broiler.VM JsTemporal.Options; IP=Low; Security=Medium; Resources=0; Fingerprint=F3C848
// Broiler-Human:        PENDING
public static partial class TemporalOptionRules
{
    /// <summary>The <c>roundingIncrement</c> option's default, 1 (<c>GetRoundingIncrementOption</c>).</summary>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s14.5.2.3 via Broiler.VM JsTemporal.RoundingIncrement; IP=None; Security=None; Resources=0; Fingerprint=A02056
    // Broiler-Human:        PENDING
    public const long DefaultRoundingIncrement = 1;

    /// <summary>The largest <c>roundingIncrement</c> the option admits, 10^9 (<c>GetRoundingIncrementOption</c>).</summary>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s14.5.2.3 via Broiler.VM JsTemporal.RoundingIncrement; IP=None; Security=None; Resources=0; Fingerprint=EF08B1
    // Broiler-Human:        PENDING
    public const long MaxRoundingIncrement = 1_000_000_000;

    /// <summary>The <c>roundingMode</c> default of every <c>round</c>, <c>"halfExpand"</c>.</summary>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s7.3.20 via Broiler.VM JsRealm.TemporalDuration DurationRound; IP=None; Security=None; Resources=0; Fingerprint=CC82E1
    // Broiler-Human:        PENDING
    public const RoundingMode DefaultRoundRoundingMode = RoundingMode.HalfExpand;

    /// <summary>The <c>roundingMode</c> default of every <c>until</c> and <c>since</c>, <c>"trunc"</c> (<c>GetDifferenceSettings</c>).</summary>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s13.43 via Broiler.VM JsTemporal.DifferenceSettings; IP=None; Security=None; Resources=0; Fingerprint=971009
    // Broiler-Human:        PENDING
    public const RoundingMode DefaultDifferenceRoundingMode = RoundingMode.Trunc;

    /// <summary>
    /// The <c>roundingIncrement</c> option's rule after the engine's <c>ToNumber</c>: <c>GetRoundingIncrementOption</c>
    /// (s14.5.2.3) - <c>ToIntegerWithTruncation</c>'s refusal of NaN and the infinities, the truncation, and the range 1 to
    /// 10^9.
    /// </summary>
    /// <param name="value">The option's value, a Number (an undefined option is <see cref="DefaultRoundingIncrement"/>).</param>
    /// <param name="increment">The increment; <see cref="DefaultRoundingIncrement"/> when the value is refused.</param>
    /// <returns>Whether the value is accepted; any other is the specification's <c>RangeError</c>.</returns>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s14.5.2.3 via Broiler.VM JsTemporal.RoundingIncrement; IP=Low; Security=Medium; Resources=0; Fingerprint=5A9FDB
    // Broiler-Human:        PENDING
    public static bool TryParseRoundingIncrement(double value, out long increment)
    {
        increment = DefaultRoundingIncrement;
        if (!double.IsFinite(value))
            return false;

        var integer = Math.Truncate(value);
        if (integer is < 1 or > MaxRoundingIncrement)
            return false;

        increment = (long)integer;
        return true;
    }

    /// <summary>Whether a unit is a calendar unit: the proposal's <c>IsCalendarUnit</c> (s13.21), year, month or week.</summary>
    /// <param name="unit">The unit.</param>
    /// <returns>Whether it is a calendar unit.</returns>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s13.21 via Broiler.VM JsTemporalCore.IsCalendarUnit; IP=Low; Security=None; Resources=0; Fingerprint=4FEB79
    // Broiler-Human:        PENDING
    public static bool IsCalendarUnit(TemporalUnit unit) => unit is TemporalUnit.Year or TemporalUnit.Month or TemporalUnit.Week;

    /// <summary>Whether a unit's category is date: the proposal's <c>TemporalUnitCategory</c> (Table 21), year to day.</summary>
    /// <param name="unit">The unit, year to nanosecond.</param>
    /// <returns>Whether it is a date unit.</returns>
    // Broiler-AI:           Origin=Ported; Spec=Temporal Table 21 via Broiler.VM JsTemporalCore.IsDateUnit; IP=Low; Security=None; Resources=0; Fingerprint=50BB4F
    // Broiler-Human:        PENDING
    public static bool IsDateUnit(TemporalUnit unit) => unit is >= TemporalUnit.Year and <= TemporalUnit.Day;

    /// <summary>The larger of two units: the proposal's <c>LargerOfTwoTemporalUnits</c> (s13.20), the earlier in Table 21.</summary>
    /// <param name="one">The first unit, year to nanosecond.</param>
    /// <param name="two">The second unit, year to nanosecond.</param>
    /// <returns>The larger.</returns>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s13.20 via Broiler.VM JsTemporalCore.Larger; IP=Low; Security=None; Resources=0; Fingerprint=425B92
    // Broiler-Human:        PENDING
    public static TemporalUnit LargerOfTwoUnits(TemporalUnit one, TemporalUnit two) => one <= two ? one : two;

    /// <summary>
    /// The largest increment a unit rounds by: the proposal's <c>MaximumTemporalDurationRoundingIncrement</c> (s13.23) - 24
    /// for hours, 60 for minutes and seconds, 1,000 for the sub-second units, and none (unset) for a date unit.
    /// </summary>
    /// <param name="unit">The unit, year to nanosecond.</param>
    /// <returns>The maximum, or <see langword="null"/> for unset.</returns>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s13.23 via Broiler.VM JsTemporalCore.MaximumIncrement; IP=Low; Security=None; Resources=0; Fingerprint=B29393
    // Broiler-Human:        PENDING
    public static long? MaximumRoundingIncrement(TemporalUnit unit) => unit switch
    {
        TemporalUnit.Hour => 24,
        TemporalUnit.Minute or TemporalUnit.Second => 60,
        TemporalUnit.Millisecond or TemporalUnit.Microsecond or TemporalUnit.Nanosecond => 1000,
        _ => null,
    };

    /// <summary>
    /// Whether a unit-valued option's value is allowed in a group: the proposal's <c>ValidateTemporalUnitValue</c> (s13.18),
    /// unset always, <c>auto</c> where the operation allows it, and otherwise a unit of the group.
    /// </summary>
    /// <param name="unit">The value read: a unit, <see cref="TemporalUnit.Unset"/> or <see cref="TemporalUnit.Auto"/>.</param>
    /// <param name="group">The operation's unit group.</param>
    /// <param name="allowAuto">Whether <c>auto</c> is among the operation's extra values.</param>
    /// <returns>Whether the value is allowed; otherwise the specification's <c>RangeError</c>.</returns>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s13.18 via Broiler.VM JsTemporal.ValidateUnit; IP=Low; Security=Medium; Resources=0; Fingerprint=178DFF
    // Broiler-Human:        PENDING
    public static bool IsValidUnit(TemporalUnit unit, TemporalUnitGroup group, bool allowAuto)
    {
        if (unit == TemporalUnit.Unset || (allowAuto && unit == TemporalUnit.Auto))
            return true;
        if (unit is < TemporalUnit.Year or > TemporalUnit.Nanosecond)
            return false;

        return group switch
        {
            TemporalUnitGroup.Date => IsDateUnit(unit),
            TemporalUnitGroup.Time => !IsDateUnit(unit),
            _ => true,
        };
    }

    /// <summary>
    /// The settings of a type's <c>until</c> or <c>since</c> after its four reads: the proposal's
    /// <c>GetDifferenceSettings</c> (s13.43) with the type's unit group, disallowed units and defaults - <c>PlainDate</c>
    /// (date; day, day), <c>PlainDateTime</c> (datetime; nanosecond, day), <c>PlainTime</c> (time; nanosecond, hour),
    /// <c>PlainYearMonth</c> (date without week and day; month, year), <c>Instant</c> (time; nanosecond, second) and
    /// <c>ZonedDateTime</c> (datetime; nanosecond, hour).
    /// </summary>
    /// <param name="type">The receiver's type.</param>
    /// <param name="operation">Until or since; since negates the rounding mode.</param>
    /// <param name="largestUnit">The <c>largestUnit</c> read: a unit, <see cref="TemporalUnit.Unset"/> or <see cref="TemporalUnit.Auto"/>.</param>
    /// <param name="roundingIncrement">The <c>roundingIncrement</c> read (<see cref="TryParseRoundingIncrement"/>).</param>
    /// <param name="roundingMode">The <c>roundingMode</c> read, <see cref="DefaultDifferenceRoundingMode"/> where undefined.</param>
    /// <param name="smallestUnit">The <c>smallestUnit</c> read: a unit or <see cref="TemporalUnit.Unset"/> (<c>auto</c> is refused).</param>
    /// <param name="settings">The settings; the default when refused.</param>
    /// <returns>Whether the options are valid together; otherwise the specification's <c>RangeError</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The type or the operation is not one of the enumeration's.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s13.43 via Broiler.VM JsTemporal.DifferenceSettings; IP=Low; Security=Medium; Resources=0; Fingerprint=7EE40C
    // Broiler-Human:        PENDING
    public static bool TryGetDifferenceSettings(
        TemporalType type, DifferenceOperation operation, TemporalUnit largestUnit, long roundingIncrement, RoundingMode roundingMode,
        TemporalUnit smallestUnit, out DifferenceSettings settings)
    {
        Span<TemporalUnit> yearMonth = stackalloc TemporalUnit[] { TemporalUnit.Week, TemporalUnit.Day };
        return type switch
        {
            TemporalType.PlainDate => TryGetDifferenceSettings(
                operation, TemporalUnitGroup.Date, [], TemporalUnit.Day, TemporalUnit.Day, largestUnit, roundingIncrement, roundingMode, smallestUnit, out settings),
            TemporalType.PlainDateTime => TryGetDifferenceSettings(
                operation, TemporalUnitGroup.DateTime, [], TemporalUnit.Nanosecond, TemporalUnit.Day, largestUnit, roundingIncrement, roundingMode, smallestUnit, out settings),
            TemporalType.PlainTime => TryGetDifferenceSettings(
                operation, TemporalUnitGroup.Time, [], TemporalUnit.Nanosecond, TemporalUnit.Hour, largestUnit, roundingIncrement, roundingMode, smallestUnit, out settings),
            TemporalType.PlainYearMonth => TryGetDifferenceSettings(
                operation, TemporalUnitGroup.Date, yearMonth, TemporalUnit.Month, TemporalUnit.Year, largestUnit, roundingIncrement, roundingMode, smallestUnit, out settings),
            TemporalType.Instant => TryGetDifferenceSettings(
                operation, TemporalUnitGroup.Time, [], TemporalUnit.Nanosecond, TemporalUnit.Second, largestUnit, roundingIncrement, roundingMode, smallestUnit, out settings),
            TemporalType.ZonedDateTime => TryGetDifferenceSettings(
                operation, TemporalUnitGroup.DateTime, [], TemporalUnit.Nanosecond, TemporalUnit.Hour, largestUnit, roundingIncrement, roundingMode, smallestUnit, out settings),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "The type is not one of the enumeration's."),
        };
    }

    /// <summary>
    /// The proposal's <c>GetDifferenceSettings</c> (s13.43) after its four reads, in its own terms: the largest unit
    /// validated with <c>auto</c>, refused where disallowed and defaulted; the mode negated for <c>since</c>; the smallest
    /// unit validated, defaulted and refused where disallowed; the largest at least the smallest; and the increment valid
    /// for the smallest unit's maximum.
    /// </summary>
    /// <param name="operation">Until or since.</param>
    /// <param name="group">The unit group.</param>
    /// <param name="disallowedUnits">The units the type refuses.</param>
    /// <param name="fallbackSmallestUnit">The smallest unit where none is given.</param>
    /// <param name="smallestLargestDefaultUnit">The largest unit's default's floor.</param>
    /// <param name="largestUnit">The <c>largestUnit</c> read.</param>
    /// <param name="roundingIncrement">The <c>roundingIncrement</c> read, 1 to 10^9.</param>
    /// <param name="roundingMode">The <c>roundingMode</c> read.</param>
    /// <param name="smallestUnit">The <c>smallestUnit</c> read.</param>
    /// <param name="settings">The settings; the default when refused.</param>
    /// <returns>Whether the options are valid together; otherwise the specification's <c>RangeError</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The operation, the group, a default unit, the increment or the mode is outside its domain.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s13.43 via Broiler.VM JsTemporal.DifferenceSettings; IP=Low; Security=Medium; Resources=0; Fingerprint=1A3190
    // Broiler-Human:        PENDING
    public static bool TryGetDifferenceSettings(
        DifferenceOperation operation, TemporalUnitGroup group, ReadOnlySpan<TemporalUnit> disallowedUnits, TemporalUnit fallbackSmallestUnit,
        TemporalUnit smallestLargestDefaultUnit, TemporalUnit largestUnit, long roundingIncrement, RoundingMode roundingMode, TemporalUnit smallestUnit,
        out DifferenceSettings settings)
    {
        if (operation is < DifferenceOperation.Until or > DifferenceOperation.Since)
            throw new ArgumentOutOfRangeException(nameof(operation), operation, "The operation is not until or since.");
        if (group is < TemporalUnitGroup.Date or > TemporalUnitGroup.DateTime)
            throw new ArgumentOutOfRangeException(nameof(group), group, "The group is not one of the three.");
        if (fallbackSmallestUnit is < TemporalUnit.Year or > TemporalUnit.Nanosecond)
            throw new ArgumentOutOfRangeException(nameof(fallbackSmallestUnit), fallbackSmallestUnit, "The fallback is not a unit.");
        if (smallestLargestDefaultUnit is < TemporalUnit.Year or > TemporalUnit.Nanosecond)
            throw new ArgumentOutOfRangeException(nameof(smallestLargestDefaultUnit), smallestLargestDefaultUnit, "The default is not a unit.");
        if (roundingIncrement is < 1 or > MaxRoundingIncrement)
            throw new ArgumentOutOfRangeException(nameof(roundingIncrement), roundingIncrement, "The increment is not one the option admits.");
        if (roundingMode is < RoundingMode.Ceil or > RoundingMode.HalfEven)
            throw new ArgumentOutOfRangeException(nameof(roundingMode), roundingMode, "The mode is not one of the nine.");

        settings = default;
        if (!IsValidUnit(largestUnit, group, allowAuto: true))
            return false;

        if (largestUnit == TemporalUnit.Unset)
            largestUnit = TemporalUnit.Auto;

        if (disallowedUnits.Contains(largestUnit))
            return false;

        if (operation == DifferenceOperation.Since)
            roundingMode = ExactRounding.Negate(roundingMode);

        if (!IsValidUnit(smallestUnit, group, allowAuto: false))
            return false;

        if (smallestUnit == TemporalUnit.Unset)
            smallestUnit = fallbackSmallestUnit;

        if (disallowedUnits.Contains(smallestUnit))
            return false;

        var defaultLargestUnit = LargerOfTwoUnits(smallestLargestDefaultUnit, smallestUnit);
        if (largestUnit == TemporalUnit.Auto)
            largestUnit = defaultLargestUnit;

        if (LargerOfTwoUnits(largestUnit, smallestUnit) != largestUnit)
            return false;

        if (MaximumRoundingIncrement(smallestUnit) is { } maximum && !IsValidRoundingIncrement(roundingIncrement, maximum, inclusive: false))
            return false;

        settings = new DifferenceSettings(smallestUnit, largestUnit, roundingMode, roundingIncrement);
        return true;
    }

    /// <summary>
    /// The units of <c>Temporal.Duration.prototype.round</c> after its reads (s7.3.20 steps 13 to 24): the smallest unit
    /// in the datetime group, defaulted to nanosecond; the largest defaulted (or <c>auto</c>) to the larger of the
    /// duration's default largest unit and the smallest; one of the two given; the largest at least the smallest; the
    /// increment valid for the smallest unit's maximum; and an increment above 1 on a date unit only where it is also the
    /// largest.
    /// </summary>
    /// <param name="duration">The receiver's fields.</param>
    /// <param name="largestUnit">The <c>largestUnit</c> read: a unit, <see cref="TemporalUnit.Unset"/> or <see cref="TemporalUnit.Auto"/>.</param>
    /// <param name="roundingIncrement">The <c>roundingIncrement</c> read, 1 to 10^9.</param>
    /// <param name="smallestUnit">The <c>smallestUnit</c> read: a unit or <see cref="TemporalUnit.Unset"/>.</param>
    /// <param name="resolvedLargestUnit">The largest unit to round to; <see cref="TemporalUnit.Unset"/> when refused.</param>
    /// <param name="resolvedSmallestUnit">The smallest unit to round to; <see cref="TemporalUnit.Unset"/> when refused.</param>
    /// <returns>Whether the options are valid together; otherwise the specification's <c>RangeError</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The increment is outside 1 to 10^9.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s7.3.20 via Broiler.VM JsRealm.TemporalDuration DurationRound; IP=Low; Security=Medium; Resources=0; Fingerprint=F19768
    // Broiler-Human:        PENDING
    public static bool TryGetDurationRoundUnits(
        DurationFields duration, TemporalUnit largestUnit, long roundingIncrement, TemporalUnit smallestUnit,
        out TemporalUnit resolvedLargestUnit, out TemporalUnit resolvedSmallestUnit)
    {
        if (roundingIncrement is < 1 or > MaxRoundingIncrement)
            throw new ArgumentOutOfRangeException(nameof(roundingIncrement), roundingIncrement, "The increment is not one the option admits.");

        resolvedLargestUnit = TemporalUnit.Unset;
        resolvedSmallestUnit = TemporalUnit.Unset;
        if (!IsValidUnit(smallestUnit, TemporalUnitGroup.DateTime, allowAuto: false) ||
            largestUnit is not (>= TemporalUnit.Year and <= TemporalUnit.Nanosecond or TemporalUnit.Unset or TemporalUnit.Auto))
            return false;

        var smallestPresent = smallestUnit != TemporalUnit.Unset;
        if (!smallestPresent)
            smallestUnit = TemporalUnit.Nanosecond;

        var defaultLargestUnit = LargerOfTwoUnits(DurationCore.DefaultLargestUnit(duration), smallestUnit);
        var largestPresent = largestUnit != TemporalUnit.Unset;
        if (largestUnit is TemporalUnit.Unset or TemporalUnit.Auto)
            largestUnit = defaultLargestUnit;

        if (!smallestPresent && !largestPresent)
            return false;
        if (LargerOfTwoUnits(largestUnit, smallestUnit) != largestUnit)
            return false;
        if (MaximumRoundingIncrement(smallestUnit) is { } maximum && !IsValidRoundingIncrement(roundingIncrement, maximum, inclusive: false))
            return false;
        if (roundingIncrement > 1 && largestUnit != smallestUnit && IsDateUnit(smallestUnit))
            return false;

        resolvedLargestUnit = largestUnit;
        resolvedSmallestUnit = smallestUnit;
        return true;
    }

    /// <summary>
    /// Whether <c>Temporal.Duration.prototype.total</c>'s unit is allowed (s7.3.21): the required <c>unit</c> option
    /// validated in the datetime group, so a unit, never unset or <c>auto</c>.
    /// </summary>
    /// <param name="unit">The <c>unit</c> read, <see cref="TemporalUnit.Unset"/> where undefined.</param>
    /// <returns>Whether the unit is allowed; otherwise the specification's <c>RangeError</c>.</returns>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s7.3.21 via Broiler.VM JsRealm.TemporalDuration DurationTotal; IP=Low; Security=Low; Resources=0; Fingerprint=5125AF
    // Broiler-Human:        PENDING
    public static bool IsValidTotalUnit(TemporalUnit unit) => unit is >= TemporalUnit.Year and <= TemporalUnit.Nanosecond;

    /// <summary>
    /// Whether the options of a type's <c>round</c> are valid together, after its reads: the required <c>smallestUnit</c>
    /// and the increment - <c>PlainTime</c> a time unit, its increment dividing the unit's maximum; <c>PlainDateTime</c> and
    /// <c>ZonedDateTime</c> a time unit or day, a day's increment 1; <c>Instant</c> a time unit, its increment dividing a
    /// day (inclusive).
    /// </summary>
    /// <param name="type">The receiver's type: <see cref="TemporalType.PlainTime"/>, <see cref="TemporalType.PlainDateTime"/>, <see cref="TemporalType.Instant"/> or <see cref="TemporalType.ZonedDateTime"/>.</param>
    /// <param name="smallestUnit">The <c>smallestUnit</c> read, <see cref="TemporalUnit.Unset"/> where undefined.</param>
    /// <param name="roundingIncrement">The <c>roundingIncrement</c> read, 1 to 10^9.</param>
    /// <returns>Whether the options are valid; otherwise the specification's <c>RangeError</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The type has no <c>round</c>, or the increment is outside 1 to 10^9.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s4.3.13, s5.3.32, s6.3.39 and s8.3.11 via Broiler.VM JsTemporal.ValidateIncrement; IP=Low; Security=Medium; Resources=0; Fingerprint=BBDAF4
    // Broiler-Human:        PENDING
    public static bool IsValidRoundOptions(TemporalType type, TemporalUnit smallestUnit, long roundingIncrement)
    {
        if (type is not (TemporalType.PlainTime or TemporalType.PlainDateTime or TemporalType.Instant or TemporalType.ZonedDateTime))
            throw new ArgumentOutOfRangeException(nameof(type), type, "The type has no round.");
        if (roundingIncrement is < 1 or > MaxRoundingIncrement)
            throw new ArgumentOutOfRangeException(nameof(roundingIncrement), roundingIncrement, "The increment is not one the option admits.");

        if (smallestUnit == TemporalUnit.Day && type is TemporalType.PlainDateTime or TemporalType.ZonedDateTime)
            return IsValidRoundingIncrement(roundingIncrement, 1, inclusive: true);

        if (smallestUnit is < TemporalUnit.Hour or > TemporalUnit.Nanosecond)
            return false;

        return type == TemporalType.Instant
            ? IsValidRoundingIncrement(roundingIncrement, TemporalLimits.NanosecondsPerDay / DurationCore.UnitLength(smallestUnit), inclusive: true)
            : IsValidRoundingIncrement(roundingIncrement, MaximumRoundingIncrement(smallestUnit)!.Value, inclusive: false);
    }
}
