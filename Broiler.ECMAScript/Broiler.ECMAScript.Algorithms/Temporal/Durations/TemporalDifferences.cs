// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   22
// Annotated:        22/22
// Exempt:           0
// Human-reviewed:   0/22
// IP risk:          Low
// Security risk:    Medium
// Criteria:         0/0
// Resource impact:  2/10 max
// Unverified:       22
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Numerics;
using Broiler.ECMAScript.Numbers;
using Broiler.ECMAScript.Time;

namespace Broiler.ECMAScript.Temporal;

/// <summary>
/// The Temporal proposal's differences (JSD-0078's differences sub-area): <c>until</c> and <c>since</c> of every type
/// after its reads - <c>DifferenceTemporalPlainDate</c>, <c>…PlainDateTime</c>, <c>…PlainTime</c>, <c>…Instant</c>,
/// <c>…ZonedDateTime</c> and <c>…PlainYearMonth</c> - and the operations under them, over the calendar provider's
/// <c>DateUntil</c> and the time-zone provider's instants, the rounding <see cref="DurationMath.RoundRelativeDuration"/>
/// for every calendar. Ported from Broiler.VM's <c>JsTemporal.Durations</c> and the difference arithmetic of its realm
/// files, with their behaviour unchanged.
/// </summary>
/// <remarks>
/// <para>
/// <b>What the engine does first</b>: the argument's <c>ToTemporal…</c> conversion, the calendars' equality
/// (<c>CalendarEquals</c>, a <c>RangeError</c>), the options' reads in alphabetical order and their rules
/// (<see cref="TemporalOptionRules.TryGetDifferenceSettings(TemporalType, DifferenceOperation, TemporalUnit, long, RoundingMode, TemporalUnit, out DifferenceSettings)"/>),
/// and for a <c>ZonedDateTime</c> whose largest unit is a date unit the zones' equality (SJL-8's
/// <see cref="ZonedTime.TimeZoneEquals"/>, a <c>RangeError</c>).
/// </para>
/// <para>
/// <b>Failure is data</b>: a result past a limit or a duration that is not valid answers a <see cref="TemporalStatus"/>,
/// each a <c>RangeError</c> in both engines. Every member is safe to call concurrently; none charges.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=Ported; Spec=Temporal s3.5.14, s4.5.17, s5.5.15, s6.5.9, s8.5.9 and s9.5.7 via Broiler.VM JsTemporal.Durations and JsRealm.Temporal*; IP=Low; Security=Medium; Resources=2; Fingerprint=06D60E
// Broiler-Human:        PENDING
public static class TemporalDifferences
{
    // ---- The methods ---------------------------------------------------------------------------------------------

    /// <summary>
    /// <c>Temporal.PlainDate.prototype.until</c> and <c>since</c> after their reads: the proposal's
    /// <c>DifferenceTemporalPlainDate</c> (s3.5.14), the calendar's <c>DateUntil</c> rounded relative to the receiver.
    /// </summary>
    /// <param name="operation">Until or since.</param>
    /// <param name="calendars">The calendar provider.</param>
    /// <param name="calendar">Both dates' calendar.</param>
    /// <param name="one">The receiver's date, within the limits.</param>
    /// <param name="two">The other date, within the limits.</param>
    /// <param name="settings">The settings, for <see cref="TemporalType.PlainDate"/>.</param>
    /// <param name="result">The difference, negated for <c>since</c>; the default unless the status is <see cref="TemporalStatus.Completed"/>.</param>
    /// <returns><see cref="TemporalStatus.Completed"/>, or <see cref="TemporalStatus.OutOfRange"/> or <see cref="TemporalStatus.InvalidDuration"/> where a rounding window passes a limit.</returns>
    /// <exception cref="ArgumentNullException">The provider is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A setting is outside its domain (the largest unit not a date unit, among others).</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s3.5.14 via Broiler.VM JsRealm.TemporalPlainDate DifferenceTemporalPlainDate; IP=Low; Security=Medium; Resources=2; Fingerprint=CA1B48
    // Broiler-Human:        PENDING
    public static TemporalStatus DifferenceTemporalPlainDate(
        DifferenceOperation operation, ICalendarArithmetic calendars, CalendarId calendar, IsoDate one, IsoDate two, DifferenceSettings settings,
        out DurationFields result)
    {
        ArgumentNullException.ThrowIfNull(calendars);
        CheckSettings(settings, dateOnly: true);
        result = default;
        if (IsoArithmetic.CompareIsoDate(one, two) == 0)
            return TemporalStatus.Completed;

        var status = DurationMath.CalendarDateUntil(calendars, calendar, one, two, settings.LargestUnit, out var date);
        if (status != TemporalStatus.Completed)
            return status;

        var duration = new InternalDuration(date, BigInteger.Zero);
        if (settings.SmallestUnit != TemporalUnit.Day || settings.RoundingIncrement != 1)
        {
            var origin = new IsoDateTime(one, default);
            var destination = new IsoDateTime(two, default);
            status = DurationMath.RoundRelativeDuration(
                duration, IsoArithmetic.GetUtcEpochNanoseconds(origin), IsoArithmetic.GetUtcEpochNanoseconds(destination), origin, null, calendars, calendar,
                settings.LargestUnit, settings.RoundingIncrement, settings.SmallestUnit, settings.RoundingMode, out duration);
            if (status != TemporalStatus.Completed)
                return status;
        }

        return Result(operation, duration, TemporalUnit.Day, out result);
    }

    /// <summary>
    /// <c>Temporal.PlainDateTime.prototype.until</c> and <c>since</c> after their reads: the proposal's
    /// <c>DifferenceTemporalPlainDateTime</c> (s5.5.15).
    /// </summary>
    /// <param name="operation">Until or since.</param>
    /// <param name="calendars">The calendar provider.</param>
    /// <param name="calendar">Both date-times' calendar.</param>
    /// <param name="one">The receiver's date-time, within the limits.</param>
    /// <param name="two">The other date-time, within the limits.</param>
    /// <param name="settings">The settings, for <see cref="TemporalType.PlainDateTime"/>.</param>
    /// <param name="result">The difference, negated for <c>since</c>; the default unless the status is <see cref="TemporalStatus.Completed"/>.</param>
    /// <returns><see cref="TemporalStatus.Completed"/>, or <see cref="TemporalStatus.OutOfRange"/> or <see cref="TemporalStatus.InvalidDuration"/>.</returns>
    /// <exception cref="ArgumentNullException">The provider is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A setting is outside its domain.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s5.5.15 via Broiler.VM JsRealm.TemporalPlainDateTime DifferenceTemporalPlainDateTime; IP=Low; Security=Medium; Resources=2; Fingerprint=991D2F
    // Broiler-Human:        PENDING
    public static TemporalStatus DifferenceTemporalPlainDateTime(
        DifferenceOperation operation, ICalendarArithmetic calendars, CalendarId calendar, IsoDateTime one, IsoDateTime two, DifferenceSettings settings,
        out DurationFields result)
    {
        ArgumentNullException.ThrowIfNull(calendars);
        CheckSettings(settings, dateOnly: false);
        result = default;
        if (IsoArithmetic.CompareIsoDateTime(one, two) == 0)
            return TemporalStatus.Completed;

        var status = DifferencePlainDateTimeWithRounding(
            calendars, calendar, one, two, settings.LargestUnit, settings.RoundingIncrement, settings.SmallestUnit, settings.RoundingMode, out var duration);
        return status != TemporalStatus.Completed ? status : Result(operation, duration, settings.LargestUnit, out result);
    }

    /// <summary>
    /// <c>Temporal.PlainTime.prototype.until</c> and <c>since</c> after their reads: the proposal's
    /// <c>DifferenceTemporalPlainTime</c> (s4.5.17), the time difference rounded.
    /// </summary>
    /// <param name="operation">Until or since.</param>
    /// <param name="one">The receiver's time, valid.</param>
    /// <param name="two">The other time, valid.</param>
    /// <param name="settings">The settings, for <see cref="TemporalType.PlainTime"/>.</param>
    /// <param name="result">The difference, negated for <c>since</c>; the default unless the status is <see cref="TemporalStatus.Completed"/>.</param>
    /// <returns><see cref="TemporalStatus.Completed"/>; a time difference rounds within the limits.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A setting is outside its domain (a unit not a time unit, among others).</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s4.5.17 via Broiler.VM JsRealm.TemporalPlainTime DifferenceTemporalPlainTime; IP=Low; Security=Medium; Resources=1; Fingerprint=4E7DDB
    // Broiler-Human:        PENDING
    public static TemporalStatus DifferenceTemporalPlainTime(DifferenceOperation operation, IsoTime one, IsoTime two, DifferenceSettings settings, out DurationFields result)
    {
        CheckSettings(settings, dateOnly: false);
        CheckTimeUnits(settings);
        result = default;
        var status = DurationMath.RoundTimeDuration(
            IsoArithmetic.DifferenceTime(one, two), settings.RoundingIncrement, settings.SmallestUnit, settings.RoundingMode, out var time);
        return status != TemporalStatus.Completed ? status : Result(operation, new InternalDuration(default, time), settings.LargestUnit, out result);
    }

    /// <summary>
    /// <c>Temporal.Instant.prototype.until</c> and <c>since</c> after their reads: the proposal's
    /// <c>DifferenceTemporalInstant</c> (s8.5.9).
    /// </summary>
    /// <param name="operation">Until or since.</param>
    /// <param name="one">The receiver's epoch nanoseconds, valid.</param>
    /// <param name="two">The other epoch nanoseconds, valid.</param>
    /// <param name="settings">The settings, for <see cref="TemporalType.Instant"/>.</param>
    /// <param name="result">The difference, negated for <c>since</c>; the default unless the status is <see cref="TemporalStatus.Completed"/>.</param>
    /// <returns><see cref="TemporalStatus.Completed"/>, or <see cref="TemporalStatus.InvalidDuration"/> where the rounded difference is past the limits.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A setting is outside its domain (a unit not a time unit, among others).</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s8.5.9 via Broiler.VM JsRealm.TemporalInstant DifferenceTemporalInstant; IP=Low; Security=Medium; Resources=1; Fingerprint=C68214
    // Broiler-Human:        PENDING
    public static TemporalStatus DifferenceTemporalInstant(DifferenceOperation operation, BigInteger one, BigInteger two, DifferenceSettings settings, out DurationFields result)
    {
        CheckSettings(settings, dateOnly: false);
        CheckTimeUnits(settings);
        result = default;
        var status = DifferenceInstant(one, two, settings.RoundingIncrement, settings.SmallestUnit, settings.RoundingMode, out var duration);
        return status != TemporalStatus.Completed ? status : Result(operation, duration, settings.LargestUnit, out result);
    }

    /// <summary>
    /// <c>Temporal.ZonedDateTime.prototype.until</c> and <c>since</c> after their reads: the proposal's
    /// <c>DifferenceTemporalZonedDateTime</c> (s6.5.9) - an instant difference for a time largest unit, otherwise the
    /// wall clocks' difference in the zone, rounded over the zone's real days.
    /// </summary>
    /// <param name="operation">Until or since.</param>
    /// <param name="one">The receiver's epoch nanoseconds, valid.</param>
    /// <param name="two">The other epoch nanoseconds, valid.</param>
    /// <param name="timeZone">The receiver's zone; where the largest unit is a date unit, the engine has checked that the other's equals it.</param>
    /// <param name="calendars">The calendar provider.</param>
    /// <param name="calendar">Both values' calendar.</param>
    /// <param name="settings">The settings, for <see cref="TemporalType.ZonedDateTime"/>.</param>
    /// <param name="result">The difference, negated for <c>since</c>; the default unless the status is <see cref="TemporalStatus.Completed"/>.</param>
    /// <returns><see cref="TemporalStatus.Completed"/>, or <see cref="TemporalStatus.OutOfRange"/> or <see cref="TemporalStatus.InvalidDuration"/>.</returns>
    /// <exception cref="ArgumentNullException">A provider is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A setting is outside its domain.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s6.5.9 via Broiler.VM JsRealm.TemporalZonedDateTime DifferenceTemporalZonedDateTime; IP=Low; Security=Medium; Resources=2; Fingerprint=6F19A6
    // Broiler-Human:        PENDING
    public static TemporalStatus DifferenceTemporalZonedDateTime(
        DifferenceOperation operation, BigInteger one, BigInteger two, ITimeZoneArithmetic timeZone, ICalendarArithmetic calendars, CalendarId calendar,
        DifferenceSettings settings, out DurationFields result)
    {
        ArgumentNullException.ThrowIfNull(timeZone);
        ArgumentNullException.ThrowIfNull(calendars);
        CheckSettings(settings, dateOnly: false);
        result = default;
        TemporalStatus status;

        if (!IsDateUnit(settings.LargestUnit))
        {
            status = DifferenceInstant(one, two, settings.RoundingIncrement, settings.SmallestUnit, settings.RoundingMode, out var instantDifference);
            return status != TemporalStatus.Completed ? status : Result(operation, instantDifference, settings.LargestUnit, out result);
        }

        if (one == two)
            return TemporalStatus.Completed;

        status = DifferenceZonedDateTimeWithRounding(
            one, two, timeZone, calendars, calendar, settings.LargestUnit, settings.RoundingIncrement, settings.SmallestUnit, settings.RoundingMode, out var duration);
        return status != TemporalStatus.Completed ? status : Result(operation, duration, TemporalUnit.Hour, out result);
    }

    /// <summary>
    /// <c>Temporal.PlainYearMonth.prototype.until</c> and <c>since</c> after their reads: the proposal's
    /// <c>DifferenceTemporalPlainYearMonth</c> (s9.5.7) - the first days of both months in the calendar
    /// (<c>CalendarDateToFields</c> with the day 1, then <c>CalendarDateFromFields</c> under constrain), their
    /// <c>DateUntil</c> in years and months, rounded relative to the first.
    /// </summary>
    /// <param name="operation">Until or since.</param>
    /// <param name="calendars">The calendars.</param>
    /// <param name="calendar">Both year-months' calendar.</param>
    /// <param name="one">The receiver's ISO reference date, a year-month within the limits.</param>
    /// <param name="two">The other's ISO reference date, a year-month within the limits.</param>
    /// <param name="settings">The settings, for <see cref="TemporalType.PlainYearMonth"/>.</param>
    /// <param name="result">The difference, negated for <c>since</c>; the default unless the status is <see cref="TemporalStatus.Completed"/>.</param>
    /// <returns>
    /// <see cref="TemporalStatus.Completed"/>; <see cref="TemporalStatus.OutOfRange"/> where a month's first day or a rounding
    /// window passes the limits (the lower limit's year-month, whose first day is not a date within them).
    /// </returns>
    /// <exception cref="ArgumentNullException">The calendars are <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A setting is outside its domain (a largest or smallest unit of week or day, among others).</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s9.5.7 via Broiler.VM JsRealm.TemporalYearMonthDay DifferenceTemporalPlainYearMonth; IP=Low; Security=Medium; Resources=2; Fingerprint=D8B743
    // Broiler-Human:        PENDING
    public static TemporalStatus DifferenceTemporalPlainYearMonth(
        DifferenceOperation operation, TemporalCalendars calendars, CalendarId calendar, IsoDate one, IsoDate two, DifferenceSettings settings,
        out DurationFields result)
    {
        ArgumentNullException.ThrowIfNull(calendars);
        CheckSettings(settings, dateOnly: true);
        if (settings.LargestUnit is TemporalUnit.Week or TemporalUnit.Day || settings.SmallestUnit is TemporalUnit.Week or TemporalUnit.Day)
            throw new ArgumentOutOfRangeException(nameof(settings), settings, "A year-month's difference has no week or day unit.");

        result = default;
        if (IsoArithmetic.CompareIsoDate(one, two) == 0)
            return TemporalStatus.Completed;

        var status = FirstOfMonth(calendars, calendar, one, out var thisDate);
        if (status != TemporalStatus.Completed)
            return status;

        status = FirstOfMonth(calendars, calendar, two, out var otherDate);
        if (status != TemporalStatus.Completed)
            return status;

        status = DurationMath.CalendarDateUntil(calendars, calendar, thisDate, otherDate, settings.LargestUnit, out var dateDifference);
        if (status != TemporalStatus.Completed)
            return status;

        status = DurationMath.CreateDateDurationRecord(dateDifference.Years, dateDifference.Months, 0, 0, out var yearsMonths);
        if (status != TemporalStatus.Completed)
            return status;

        var duration = new InternalDuration(yearsMonths, BigInteger.Zero);
        if (settings.SmallestUnit != TemporalUnit.Month || settings.RoundingIncrement != 1)
        {
            var origin = new IsoDateTime(thisDate, default);
            var destination = new IsoDateTime(otherDate, default);
            status = DurationMath.RoundRelativeDuration(
                duration, IsoArithmetic.GetUtcEpochNanoseconds(origin), IsoArithmetic.GetUtcEpochNanoseconds(destination), origin, null, calendars, calendar,
                settings.LargestUnit, settings.RoundingIncrement, settings.SmallestUnit, settings.RoundingMode, out duration);
            if (status != TemporalStatus.Completed)
                return status;
        }

        return Result(operation, duration, TemporalUnit.Day, out result);
    }

    // ---- The operations under them --------------------------------------------------------------------------------

    /// <summary>
    /// The difference of two instants rounded: the proposal's <c>DifferenceInstant</c> (s8.5.6), a time duration rounded
    /// to the smallest unit.
    /// </summary>
    /// <param name="one">The first epoch nanoseconds.</param>
    /// <param name="two">The second epoch nanoseconds.</param>
    /// <param name="roundingIncrement">The increment, positive.</param>
    /// <param name="smallestUnit">A time unit, or <see cref="TemporalUnit.Day"/> of 24 hours.</param>
    /// <param name="roundingMode">The rounding mode.</param>
    /// <param name="result">The internal duration, its date part zero; the default unless the status is <see cref="TemporalStatus.Completed"/>.</param>
    /// <returns><see cref="TemporalStatus.Completed"/>, or <see cref="TemporalStatus.InvalidDuration"/> past <see cref="TemporalLimits.MaxTimeDuration"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The increment is not positive, or the unit is a calendar unit, unset or auto.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s8.5.6 via Broiler.VM JsTemporal.DifferenceInstant; IP=Low; Security=Low; Resources=1; Fingerprint=7CC157
    // Broiler-Human:        PENDING
    public static TemporalStatus DifferenceInstant(BigInteger one, BigInteger two, long roundingIncrement, TemporalUnit smallestUnit, RoundingMode roundingMode, out InternalDuration result)
    {
        result = default;
        var status = DurationMath.RoundTimeDuration(two - one, roundingIncrement, smallestUnit, roundingMode, out var time);
        if (status == TemporalStatus.Completed)
            result = new InternalDuration(default, time);

        return status;
    }

    /// <summary>
    /// The difference of two date-times, unrounded: the proposal's <c>DifferenceISODateTime</c> (s5.5.12) - the time
    /// difference borrowed into the dates where its sign is theirs, the dates' <c>DateUntil</c>, and the days made 24 hours
    /// where the largest unit is a time unit.
    /// </summary>
    /// <param name="calendars">The calendar provider.</param>
    /// <param name="calendar">The calendar.</param>
    /// <param name="one">The first date-time, within the limits.</param>
    /// <param name="two">The second date-time, within the limits.</param>
    /// <param name="largestUnit">The largest unit, year to nanosecond.</param>
    /// <param name="result">The internal duration; the default unless the status is <see cref="TemporalStatus.Completed"/>.</param>
    /// <returns><see cref="TemporalStatus.Completed"/>, or <see cref="TemporalStatus.OutOfRange"/> where the calendar cannot reach a date.</returns>
    /// <exception cref="ArgumentNullException">The provider is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The unit is unset or auto.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s5.5.12 via Broiler.VM JsTemporal.DifferenceIsoDateTime; IP=Low; Security=Low; Resources=1; Fingerprint=186131
    // Broiler-Human:        PENDING
    public static TemporalStatus DifferenceIsoDateTime(
        ICalendarArithmetic calendars, CalendarId calendar, IsoDateTime one, IsoDateTime two, TemporalUnit largestUnit, out InternalDuration result)
    {
        ArgumentNullException.ThrowIfNull(calendars);
        CheckUnit(largestUnit, nameof(largestUnit));
        result = default;

        var time = IsoArithmetic.DifferenceTime(one.Time, two.Time);
        var timeSign = time.Sign;
        var dateSign = IsoArithmetic.CompareIsoDate(one.Date, two.Date);
        var adjusted = two.Date;
        if (timeSign == dateSign)
        {
            adjusted = IsoArithmetic.AddDaysToIsoDate(adjusted, timeSign);
            time -= timeSign * new BigInteger(TemporalLimits.NanosecondsPerDay);
        }

        var dateLargest = Larger(TemporalUnit.Day, largestUnit);
        var status = DurationMath.CalendarDateUntil(calendars, calendar, one.Date, adjusted, dateLargest, out var date);
        if (status != TemporalStatus.Completed)
            return status;

        if (largestUnit != dateLargest)
        {
            status = DurationMath.Add24HourDaysToTimeDuration(time, date.Days, out time);
            if (status != TemporalStatus.Completed)
                return status;

            date = date with { Days = 0 };
        }

        result = new InternalDuration(date, time);
        return TemporalStatus.Completed;
    }

    /// <summary>
    /// The difference of two date-times, rounded: the proposal's <c>DifferencePlainDateTimeWithRounding</c> (s5.5.13),
    /// <see cref="DifferenceIsoDateTime"/> rounded relative to the first by <see cref="DurationMath.RoundRelativeDuration"/>.
    /// </summary>
    /// <param name="calendars">The calendar provider.</param>
    /// <param name="calendar">The calendar.</param>
    /// <param name="one">The first date-time, valid.</param>
    /// <param name="two">The second date-time, valid.</param>
    /// <param name="largestUnit">The largest unit.</param>
    /// <param name="roundingIncrement">The increment, valid for the smallest unit.</param>
    /// <param name="smallestUnit">The smallest unit.</param>
    /// <param name="roundingMode">The rounding mode.</param>
    /// <param name="result">The internal duration; the default unless the status is <see cref="TemporalStatus.Completed"/>.</param>
    /// <returns>
    /// <see cref="TemporalStatus.Completed"/>; <see cref="TemporalStatus.OutOfRange"/> where either date-time is outside the
    /// limits or a window passes them; <see cref="TemporalStatus.InvalidDuration"/> where a window is not a valid duration.
    /// </returns>
    /// <exception cref="ArgumentNullException">The provider is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A unit is unset or auto, the smallest is larger than the largest, or the increment is not positive.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s5.5.13 via Broiler.VM JsTemporal.DifferencePlainDateTimeWithRounding; IP=Low; Security=Medium; Resources=2; Fingerprint=821347
    // Broiler-Human:        PENDING
    public static TemporalStatus DifferencePlainDateTimeWithRounding(
        ICalendarArithmetic calendars, CalendarId calendar, IsoDateTime one, IsoDateTime two, TemporalUnit largestUnit, long roundingIncrement,
        TemporalUnit smallestUnit, RoundingMode roundingMode, out InternalDuration result)
    {
        ArgumentNullException.ThrowIfNull(calendars);
        CheckUnits(largestUnit, smallestUnit, roundingIncrement);
        result = default;
        if (IsoArithmetic.CompareIsoDateTime(one, two) == 0)
            return TemporalStatus.Completed;

        if (TemporalLimits.CheckIsoDateTime(one) != TemporalStatus.Completed || TemporalLimits.CheckIsoDateTime(two) != TemporalStatus.Completed)
            return TemporalStatus.OutOfRange;

        var status = DifferenceIsoDateTime(calendars, calendar, one, two, largestUnit, out var difference);
        if (status != TemporalStatus.Completed)
            return status;

        if (smallestUnit == TemporalUnit.Nanosecond && roundingIncrement == 1)
        {
            result = difference;
            return TemporalStatus.Completed;
        }

        return DurationMath.RoundRelativeDuration(
            difference, IsoArithmetic.GetUtcEpochNanoseconds(one), IsoArithmetic.GetUtcEpochNanoseconds(two), one, null, calendars, calendar,
            largestUnit, roundingIncrement, smallestUnit, roundingMode, out result);
    }

    /// <summary>
    /// The total of a unit between two date-times: the proposal's <c>DifferencePlainDateTimeWithTotal</c> (s5.5.14), an
    /// exact ratio rounded once.
    /// </summary>
    /// <param name="calendars">The calendar provider.</param>
    /// <param name="calendar">The calendar.</param>
    /// <param name="one">The first date-time, valid.</param>
    /// <param name="two">The second date-time, valid.</param>
    /// <param name="unit">The unit, year to nanosecond.</param>
    /// <param name="total">The total; zero unless the status is <see cref="TemporalStatus.Completed"/>.</param>
    /// <returns><see cref="TemporalStatus.Completed"/>, or <see cref="TemporalStatus.OutOfRange"/> or <see cref="TemporalStatus.InvalidDuration"/>.</returns>
    /// <exception cref="ArgumentNullException">The provider is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The unit is unset or auto.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s5.5.14 via Broiler.VM JsTemporal.DifferencePlainDateTimeWithTotal; IP=Low; Security=Medium; Resources=2; Fingerprint=31967E
    // Broiler-Human:        PENDING
    public static TemporalStatus DifferencePlainDateTimeWithTotal(
        ICalendarArithmetic calendars, CalendarId calendar, IsoDateTime one, IsoDateTime two, TemporalUnit unit, out double total)
    {
        ArgumentNullException.ThrowIfNull(calendars);
        CheckUnit(unit, nameof(unit));
        total = 0;
        if (IsoArithmetic.CompareIsoDateTime(one, two) == 0)
            return TemporalStatus.Completed;

        if (TemporalLimits.CheckIsoDateTime(one) != TemporalStatus.Completed || TemporalLimits.CheckIsoDateTime(two) != TemporalStatus.Completed)
            return TemporalStatus.OutOfRange;

        var status = DifferenceIsoDateTime(calendars, calendar, one, two, unit, out var difference);
        if (status != TemporalStatus.Completed)
            return status;

        if (unit == TemporalUnit.Nanosecond)
        {
            total = ExactRounding.ToDouble(difference.TimeNanoseconds, BigInteger.One);
            return TemporalStatus.Completed;
        }

        return DurationMath.TotalRelativeDuration(
            difference, IsoArithmetic.GetUtcEpochNanoseconds(one), IsoArithmetic.GetUtcEpochNanoseconds(two), one, null, calendars, calendar, unit, out total);
    }

    /// <summary>
    /// The difference of two instants' wall clocks in a zone, unrounded: the proposal's <c>DifferenceZonedDateTime</c>
    /// (s6.5.6) - the end's date corrected by a day or two until the remaining time has the difference's sign, then the
    /// calendar's <c>DateUntil</c> and the exact time left.
    /// </summary>
    /// <param name="one">The first epoch nanoseconds, valid.</param>
    /// <param name="two">The second epoch nanoseconds, valid.</param>
    /// <param name="timeZone">The zone.</param>
    /// <param name="calendars">The calendar provider.</param>
    /// <param name="calendar">The calendar.</param>
    /// <param name="largestUnit">The largest unit, a date unit.</param>
    /// <param name="result">The internal duration; the default unless the status is <see cref="TemporalStatus.Completed"/>.</param>
    /// <returns><see cref="TemporalStatus.Completed"/>, or <see cref="TemporalStatus.OutOfRange"/> where a wall clock's instant passes the limits.</returns>
    /// <exception cref="ArgumentNullException">A provider is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The unit is not a date unit.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s6.5.6 via Broiler.VM JsTemporal.DifferenceZonedDateTime; IP=Low; Security=Medium; Resources=2; Fingerprint=7B616B
    // Broiler-Human:        PENDING
    public static TemporalStatus DifferenceZonedDateTime(
        BigInteger one, BigInteger two, ITimeZoneArithmetic timeZone, ICalendarArithmetic calendars, CalendarId calendar, TemporalUnit largestUnit,
        out InternalDuration result)
    {
        ArgumentNullException.ThrowIfNull(timeZone);
        ArgumentNullException.ThrowIfNull(calendars);
        if (!IsDateUnit(largestUnit))
            throw new ArgumentOutOfRangeException(nameof(largestUnit), largestUnit, "The largest unit is not a date unit.");

        result = default;
        if (one == two)
            return TemporalStatus.Completed;

        var start = DurationMath.IsoDateTimeFor(timeZone, one);
        var end = DurationMath.IsoDateTimeFor(timeZone, two);
        if (IsoArithmetic.CompareIsoDate(start.Date, end.Date) == 0)
        {
            result = new InternalDuration(default, two - one);
            return TemporalStatus.Completed;
        }

        var sign = two - one < 0 ? 1 : -1;
        var maxDayCorrection = sign == -1 ? 2 : 1;
        var dayCorrection = 0;
        var time = IsoArithmetic.DifferenceTime(start.Time, end.Time);
        if (time.Sign == sign)
            dayCorrection++;

        var success = false;
        var intermediate = default(IsoDateTime);
        while (dayCorrection <= maxDayCorrection && !success)
        {
            var intermediateDate = IsoArithmetic.AddDaysToIsoDate(end.Date, dayCorrection * sign);
            intermediate = new IsoDateTime(intermediateDate, start.Time);
            var status = DurationMath.EpochNanosecondsFor(timeZone, intermediate, out var intermediateNs);
            if (status != TemporalStatus.Completed)
                return status;

            time = two - intermediateNs;
            if (time.Sign != sign)
                success = true;

            dayCorrection++;
        }

        var dateLargest = Larger(largestUnit, TemporalUnit.Day);
        var untilStatus = DurationMath.CalendarDateUntil(calendars, calendar, start.Date, intermediate.Date, dateLargest, out var date);
        if (untilStatus != TemporalStatus.Completed)
            return untilStatus;

        result = new InternalDuration(date, time);
        return TemporalStatus.Completed;
    }

    /// <summary>
    /// The difference of two zoned instants, rounded: the proposal's <c>DifferenceZonedDateTimeWithRounding</c> (s6.5.7),
    /// an instant difference for a time largest unit, otherwise <see cref="DifferenceZonedDateTime"/> rounded relative to
    /// the first by <see cref="DurationMath.RoundRelativeDuration"/> over the zone's real days (rows D9 and F7).
    /// </summary>
    /// <param name="one">The first epoch nanoseconds, valid.</param>
    /// <param name="two">The second epoch nanoseconds, valid.</param>
    /// <param name="timeZone">The zone.</param>
    /// <param name="calendars">The calendar provider.</param>
    /// <param name="calendar">The calendar.</param>
    /// <param name="largestUnit">The largest unit.</param>
    /// <param name="roundingIncrement">The increment, valid for the smallest unit.</param>
    /// <param name="smallestUnit">The smallest unit.</param>
    /// <param name="roundingMode">The rounding mode.</param>
    /// <param name="result">The internal duration; the default unless the status is <see cref="TemporalStatus.Completed"/>.</param>
    /// <returns><see cref="TemporalStatus.Completed"/>, or <see cref="TemporalStatus.OutOfRange"/> or <see cref="TemporalStatus.InvalidDuration"/>.</returns>
    /// <exception cref="ArgumentNullException">A provider is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A unit is unset or auto, the smallest is larger than the largest, or the increment is not positive.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s6.5.7 via Broiler.VM JsTemporal.DifferenceZonedDateTimeWithRounding; IP=Low; Security=Medium; Resources=2; Fingerprint=7B0220
    // Broiler-Human:        PENDING
    public static TemporalStatus DifferenceZonedDateTimeWithRounding(
        BigInteger one, BigInteger two, ITimeZoneArithmetic timeZone, ICalendarArithmetic calendars, CalendarId calendar, TemporalUnit largestUnit,
        long roundingIncrement, TemporalUnit smallestUnit, RoundingMode roundingMode, out InternalDuration result)
    {
        ArgumentNullException.ThrowIfNull(timeZone);
        ArgumentNullException.ThrowIfNull(calendars);
        CheckUnits(largestUnit, smallestUnit, roundingIncrement);
        if (!IsDateUnit(largestUnit))
            return DifferenceInstant(one, two, roundingIncrement, smallestUnit, roundingMode, out result);

        var status = DifferenceZonedDateTime(one, two, timeZone, calendars, calendar, largestUnit, out var difference);
        if (status != TemporalStatus.Completed)
        {
            result = default;
            return status;
        }

        if (smallestUnit == TemporalUnit.Nanosecond && roundingIncrement == 1)
        {
            result = difference;
            return TemporalStatus.Completed;
        }

        var dateTime = DurationMath.IsoDateTimeFor(timeZone, one);
        return DurationMath.RoundRelativeDuration(
            difference, one, two, dateTime, timeZone, calendars, calendar, largestUnit, roundingIncrement, smallestUnit, roundingMode, out result);
    }

    /// <summary>
    /// The total of a unit between two zoned instants: the proposal's <c>DifferenceZonedDateTimeWithTotal</c> (s6.5.8), an
    /// exact ratio rounded once.
    /// </summary>
    /// <param name="one">The first epoch nanoseconds, valid.</param>
    /// <param name="two">The second epoch nanoseconds, valid.</param>
    /// <param name="timeZone">The zone.</param>
    /// <param name="calendars">The calendar provider.</param>
    /// <param name="calendar">The calendar.</param>
    /// <param name="unit">The unit, year to nanosecond.</param>
    /// <param name="total">The total; zero unless the status is <see cref="TemporalStatus.Completed"/>.</param>
    /// <returns><see cref="TemporalStatus.Completed"/>, or <see cref="TemporalStatus.OutOfRange"/> or <see cref="TemporalStatus.InvalidDuration"/>.</returns>
    /// <exception cref="ArgumentNullException">A provider is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The unit is unset or auto.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s6.5.8 via Broiler.VM JsTemporal.DifferenceZonedDateTimeWithTotal; IP=Low; Security=Medium; Resources=2; Fingerprint=2A5629
    // Broiler-Human:        PENDING
    public static TemporalStatus DifferenceZonedDateTimeWithTotal(
        BigInteger one, BigInteger two, ITimeZoneArithmetic timeZone, ICalendarArithmetic calendars, CalendarId calendar, TemporalUnit unit, out double total)
    {
        ArgumentNullException.ThrowIfNull(timeZone);
        ArgumentNullException.ThrowIfNull(calendars);
        CheckUnit(unit, nameof(unit));
        total = 0;
        if (!IsDateUnit(unit))
        {
            total = DurationMath.TotalTimeDuration(two - one, unit);
            return TemporalStatus.Completed;
        }

        var status = DifferenceZonedDateTime(one, two, timeZone, calendars, calendar, unit, out var difference);
        if (status != TemporalStatus.Completed)
            return status;

        var dateTime = DurationMath.IsoDateTimeFor(timeZone, one);
        return DurationMath.TotalRelativeDuration(difference, one, two, dateTime, timeZone, calendars, calendar, unit, out total);
    }

    // ---- Helpers ---------------------------------------------------------------------------------------------------

    // TemporalDurationFromInternal, then for since CreateNegatedTemporalDuration.
    // Broiler-AI:           Origin=Ported; Spec=Temporal s3.5.14 via Broiler.VM JsRealm.Temporal* Difference; IP=Low; Security=Low; Resources=0; Fingerprint=68078D
    // Broiler-Human:        PENDING
    private static TemporalStatus Result(DifferenceOperation operation, InternalDuration duration, TemporalUnit largestUnit, out DurationFields result)
    {
        var status = DurationMath.FromInternal(duration, largestUnit, out result);
        if (status == TemporalStatus.Completed && operation == DifferenceOperation.Since)
            result = DurationMath.Negate(result);

        return status;
    }

    // The first day of a year-month's month in its calendar: its fields with the day 1, made a date under constrain.
    // Broiler-AI:           Origin=Ported; Spec=Temporal s9.5.7 via Broiler.VM JsRealm.TemporalYearMonthDay DifferenceTemporalPlainYearMonth; IP=Low; Security=Low; Resources=1; Fingerprint=1EE205
    // Broiler-Human:        PENDING
    private static TemporalStatus FirstOfMonth(TemporalCalendars calendars, CalendarId calendar, IsoDate yearMonth, out IsoDate date)
    {
        date = default;
        var status = calendars.DateToFields(calendar, yearMonth, CalendarFieldsType.YearMonth, out var fields);
        if (status != CalendarStatus.Completed)
            return DurationMath.Map(status);

        return DurationMath.Map(calendars.DateFromFields(calendar, fields with { Day = 1 }, CalendarOverflow.Constrain, out date));
    }

    // LargerOfTwoTemporalUnits.
    // Broiler-AI:           Origin=Ported; Spec=Temporal s13.20 via Broiler.VM JsTemporalCore.Larger; IP=Low; Security=None; Resources=0; Fingerprint=F1BCC9
    // Broiler-Human:        PENDING
    private static TemporalUnit Larger(TemporalUnit one, TemporalUnit two) => one <= two ? one : two;

    // Whether a unit's category is date (Table 21).
    // Broiler-AI:           Origin=Ported; Spec=Temporal Table 21 via Broiler.VM JsTemporalCore.IsDateUnit; IP=Low; Security=None; Resources=0; Fingerprint=C6E39D
    // Broiler-Human:        PENDING
    private static bool IsDateUnit(TemporalUnit unit) => unit <= TemporalUnit.Day;

    // The precondition of a unit: year to nanosecond.
    // Broiler-AI:           Origin=AI; Spec=JSD-0078 preconditions; IP=None; Security=Low; Resources=0; Fingerprint=8F8097
    // Broiler-Human:        PENDING
    private static void CheckUnit(TemporalUnit unit, string name)
    {
        if (unit is < TemporalUnit.Year or > TemporalUnit.Nanosecond)
            throw new ArgumentOutOfRangeException(name, unit, "The unit is not year to nanosecond.");
    }

    // The precondition of a rounding: two units, the smallest no larger than the largest, and a positive increment.
    // Broiler-AI:           Origin=AI; Spec=JSD-0078 preconditions; IP=None; Security=Low; Resources=0; Fingerprint=637CE2
    // Broiler-Human:        PENDING
    private static void CheckUnits(TemporalUnit largestUnit, TemporalUnit smallestUnit, long increment)
    {
        CheckUnit(largestUnit, nameof(largestUnit));
        CheckUnit(smallestUnit, nameof(smallestUnit));
        if (smallestUnit < largestUnit)
            throw new ArgumentOutOfRangeException(nameof(smallestUnit), smallestUnit, "The smallest unit is larger than the largest.");
        if (increment < 1)
            throw new ArgumentOutOfRangeException(nameof(increment), increment, "The increment is not positive.");
    }

    // The precondition of a method's settings: GetDifferenceSettings' record, for a date type its units date units.
    // Broiler-AI:           Origin=AI; Spec=JSD-0078 preconditions; IP=None; Security=Low; Resources=0; Fingerprint=DD68F7
    // Broiler-Human:        PENDING
    private static void CheckSettings(DifferenceSettings settings, bool dateOnly)
    {
        CheckUnits(settings.LargestUnit, settings.SmallestUnit, settings.RoundingIncrement);
        if (settings.RoundingMode is < RoundingMode.Ceil or > RoundingMode.HalfEven)
            throw new ArgumentOutOfRangeException(nameof(settings), settings, "The rounding mode is not one of the nine.");
        if (dateOnly && settings.SmallestUnit > TemporalUnit.Day)
            throw new ArgumentOutOfRangeException(nameof(settings), settings, "A date's difference has no time unit.");
    }

    // The precondition of a time type's settings: both units time units.
    // Broiler-AI:           Origin=AI; Spec=JSD-0078 preconditions; IP=None; Security=Low; Resources=0; Fingerprint=CE7B19
    // Broiler-Human:        PENDING
    private static void CheckTimeUnits(DifferenceSettings settings)
    {
        if (IsDateUnit(settings.LargestUnit))
            throw new ArgumentOutOfRangeException(nameof(settings), settings, "A time's difference has no date unit.");
    }
}
