// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   18
// Annotated:        18/18
// Exempt:           0
// Human-reviewed:   0/18
// IP risk:          Low
// Security risk:    Medium
// Criteria:         0/0
// Resource impact:  2/10 max
// Unverified:       18
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Numerics;
using Broiler.ECMAScript.Numbers;
using Broiler.ECMAScript.Time;

namespace Broiler.ECMAScript.Temporal;

// The relative machinery of Temporal s7.5.33 to s7.5.39, as Broiler.VM's JsTemporal.Durations has it: one path for every
// calendar, through ICalendarArithmetic's DateAdd and DateUntil, and through ITimeZoneArithmetic's instants where a zone
// is set. Its two named places (JSD-0054 section 4) stand, and the pinned text rules over its polyfill in the nudge after
// a shifted window (JSD-0078, decision D7; row D8).
// Broiler-AI:           Origin=Ported; Spec=Temporal s7.5 via Broiler.VM JsTemporal.Durations; IP=Low; Security=Medium; Resources=2; Fingerprint=B1BDBE
// Broiler-Human:        PENDING
public static partial class DurationMath
{
    /// <summary>
    /// A duration rounded relative to a date: the proposal's <c>RoundRelativeDuration</c> (s7.5.38) - nudged to the
    /// smallest unit (<c>NudgeToCalendarUnit</c> for a calendar unit, or a day with a zone; <c>NudgeToZonedTime</c> for a
    /// time unit with a zone; <c>NudgeToDayOrTime</c> otherwise), then bubbled up to the largest unit where the nudge
    /// expanded a unit.
    /// </summary>
    /// <param name="duration">The internal duration from the origin to the destination, as a difference gives it.</param>
    /// <param name="originEpochNanoseconds">The origin's epoch nanoseconds (UTC's reading of a plain date-time).</param>
    /// <param name="destinationEpochNanoseconds">The destination's epoch nanoseconds.</param>
    /// <param name="dateTime">The origin's date-time, the wall clock where a zone is set.</param>
    /// <param name="timeZone">The zone, or <see langword="null"/> for the specification's <c>unset</c> (a plain origin).</param>
    /// <param name="calendars">The calendar provider.</param>
    /// <param name="calendar">The calendar.</param>
    /// <param name="largestUnit">The largest unit, year to nanosecond.</param>
    /// <param name="roundingIncrement">The increment, positive and valid for the smallest unit.</param>
    /// <param name="smallestUnit">The smallest unit, no larger than the largest.</param>
    /// <param name="roundingMode">The rounding mode.</param>
    /// <param name="result">The rounded duration; the default unless the status is <see cref="TemporalStatus.Completed"/>.</param>
    /// <returns>
    /// <see cref="TemporalStatus.Completed"/>; <see cref="TemporalStatus.InvalidDuration"/> where a window's duration or the
    /// rounded time is not valid; <see cref="TemporalStatus.OutOfRange"/> where a window passes the limits, or where a
    /// week or day window does not hold the destination, which the specification asserts and a day the zone skips
    /// reaches (row D12: Pacific/Apia's 2011-12-30 makes a day window back from 2011-12-31 of no length).
    /// </returns>
    /// <exception cref="ArgumentNullException">The calendar provider is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A unit is unset or auto, the smallest is larger than the largest, or the increment is not positive.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s7.5.38, JSD-0078 decision D7 via Broiler.VM JsTemporal.RoundRelativeDuration; IP=Low; Security=Medium; Resources=2; Fingerprint=3660BA
    // Broiler-Human:        PENDING
    public static TemporalStatus RoundRelativeDuration(
        InternalDuration duration, BigInteger originEpochNanoseconds, BigInteger destinationEpochNanoseconds, IsoDateTime dateTime,
        ITimeZoneArithmetic? timeZone, ICalendarArithmetic calendars, CalendarId calendar, TemporalUnit largestUnit, long roundingIncrement,
        TemporalUnit smallestUnit, RoundingMode roundingMode, out InternalDuration result)
    {
        ArgumentNullException.ThrowIfNull(calendars);
        CheckUnits(largestUnit, smallestUnit, roundingIncrement);
        result = default;

        var irregular = IsCalendarUnit(smallestUnit) || (timeZone is not null && smallestUnit == TemporalUnit.Day);
        var sign = Sign(duration) < 0 ? -1 : 1;
        TemporalStatus status;
        Nudge nudge;

        if (irregular)
        {
            status = NudgeToCalendarUnit(
                sign, duration, originEpochNanoseconds, destinationEpochNanoseconds, dateTime, timeZone, calendars, calendar, roundingIncrement,
                smallestUnit, roundingMode, out nudge, out _);
        }
        else if (timeZone is not null)
        {
            status = NudgeToZonedTime(sign, duration, dateTime, timeZone, calendars, calendar, roundingIncrement, smallestUnit, roundingMode, out nudge);
        }
        else
        {
            status = NudgeToDayOrTime(duration, destinationEpochNanoseconds, largestUnit, roundingIncrement, smallestUnit, roundingMode, out nudge);
        }

        if (status != TemporalStatus.Completed)
            return status;

        duration = nudge.Duration;
        if (nudge.DidExpandCalendarUnit && smallestUnit != TemporalUnit.Week)
        {
            var startUnit = DurationCore.Larger(smallestUnit, TemporalUnit.Day);
            status = BubbleRelativeDuration(sign, duration, nudge.NudgedEpochNanoseconds, dateTime, timeZone, calendars, calendar, largestUnit, startUnit, out duration);
            if (status != TemporalStatus.Completed)
                return status;
        }

        result = duration;
        return TemporalStatus.Completed;
    }

    /// <summary>
    /// A duration's total of a unit relative to a date: the proposal's <c>TotalRelativeDuration</c> (s7.5.39) - for a
    /// calendar unit, or a day with a zone, the total <c>NudgeToCalendarUnit</c> computes as an exact ratio; otherwise the
    /// time with its days of 24 hours - rounded once to a Number.
    /// </summary>
    /// <param name="duration">The internal duration from the origin to the destination, as a difference gives it.</param>
    /// <param name="originEpochNanoseconds">The origin's epoch nanoseconds.</param>
    /// <param name="destinationEpochNanoseconds">The destination's epoch nanoseconds.</param>
    /// <param name="dateTime">The origin's date-time, the wall clock where a zone is set.</param>
    /// <param name="timeZone">The zone, or <see langword="null"/> for a plain origin.</param>
    /// <param name="calendars">The calendar provider.</param>
    /// <param name="calendar">The calendar.</param>
    /// <param name="unit">The unit, year to nanosecond.</param>
    /// <param name="total">The total; zero unless the status is <see cref="TemporalStatus.Completed"/>.</param>
    /// <returns>
    /// <see cref="TemporalStatus.Completed"/>; <see cref="TemporalStatus.InvalidDuration"/> or
    /// <see cref="TemporalStatus.OutOfRange"/> where a window is not valid or passes the limits, or where a week or day
    /// window does not hold the destination (row D12; see <see cref="RoundRelativeDuration"/>).
    /// </returns>
    /// <exception cref="ArgumentNullException">The calendar provider is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The unit is unset or auto.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s7.5.39 via Broiler.VM JsTemporal.TotalRelativeDuration; IP=Low; Security=Medium; Resources=2; Fingerprint=423228
    // Broiler-Human:        PENDING
    public static TemporalStatus TotalRelativeDuration(
        InternalDuration duration, BigInteger originEpochNanoseconds, BigInteger destinationEpochNanoseconds, IsoDateTime dateTime,
        ITimeZoneArithmetic? timeZone, ICalendarArithmetic calendars, CalendarId calendar, TemporalUnit unit, out double total)
    {
        ArgumentNullException.ThrowIfNull(calendars);
        CheckUnit(unit, nameof(unit));
        total = 0;

        if (IsCalendarUnit(unit) || (timeZone is not null && unit == TemporalUnit.Day))
        {
            var sign = Sign(duration) < 0 ? -1 : 1;
            var status = NudgeToCalendarUnit(
                sign, duration, originEpochNanoseconds, destinationEpochNanoseconds, dateTime, timeZone, calendars, calendar, 1, unit,
                RoundingMode.Trunc, out _, out var fraction);
            if (status != TemporalStatus.Completed)
                return status;

            total = ExactRounding.ToDouble(fraction.Numerator, fraction.Denominator);
            return TemporalStatus.Completed;
        }

        var timeStatus = Add24HourDaysToTimeDuration(duration.TimeNanoseconds, duration.Date.Days, out var time);
        if (timeStatus != TemporalStatus.Completed)
            return timeStatus;

        total = TotalTimeDuration(time, unit);
        return TemporalStatus.Completed;
    }

    // ---- The nudges (s7.5.33 to s7.5.37) -------------------------------------------------------------------------

    // A Duration Nudge Result Record (s7.5.32).
    // Broiler-AI:           Origin=Ported; Spec=Temporal s7.5.32 via Broiler.VM JsTemporal.Durations; IP=Low; Security=None; Resources=0; Fingerprint=D0E59A
    // Broiler-Human:        PENDING
    private readonly record struct Nudge(InternalDuration Duration, BigInteger NudgedEpochNanoseconds, bool DidExpandCalendarUnit);

    // An exact ratio, its denominator positive.
    // Broiler-AI:           Origin=AI; Spec=Temporal s7.5.34; IP=None; Security=None; Resources=0; Fingerprint=72A7B6
    // Broiler-Human:        PENDING
    private readonly record struct Fraction(BigInteger Numerator, BigInteger Denominator);

    // The window ComputeNudgeWindow answers: the unit counts at its ends, their instants and their durations.
    // Broiler-AI:           Origin=Ported; Spec=Temporal s7.5.33 via Broiler.VM JsTemporal.Durations; IP=Low; Security=None; Resources=0; Fingerprint=C532C7
    // Broiler-Human:        PENDING
    private readonly record struct NudgeWindow(
        long R1, long R2, BigInteger StartEpochNanoseconds, BigInteger EndEpochNanoseconds, InternalDuration StartDuration, InternalDuration EndDuration);

    // The proposal's ComputeNudgeWindow (s7.5.33).
    // Broiler-AI:           Origin=Ported; Spec=Temporal s7.5.33, JSD-0054 section 4 via Broiler.VM JsTemporal.ComputeNudgeWindow; IP=Low; Security=Medium; Resources=2; Fingerprint=034DB9
    // Broiler-Human:        PENDING
    private static TemporalStatus ComputeNudgeWindow(
        int sign, InternalDuration duration, BigInteger originEpochNanoseconds, IsoDateTime dateTime, ITimeZoneArithmetic? timeZone,
        ICalendarArithmetic calendars, CalendarId calendar, long increment, TemporalUnit unit, bool additionalShift, out NudgeWindow window)
    {
        window = default;
        long r1, r2;
        DateDuration startDate, endDate;
        TemporalStatus status;

        long Truncated(double value) => (long)ExactRounding.RoundNumberToIncrement(new BigInteger(value), BigInteger.One, increment, RoundingMode.Trunc);

        switch (unit)
        {
            case TemporalUnit.Year:
            {
                var years = Truncated(duration.Date.Years);
                r1 = additionalShift ? years + (increment * sign) : years;
                r2 = r1 + (increment * sign);
                status = CreateDateDurationRecord(r1, 0, 0, 0, out startDate);
                if (status == TemporalStatus.Completed)
                    status = CreateDateDurationRecord(r2, 0, 0, 0, out endDate);
                else
                    endDate = default;
                break;
            }

            case TemporalUnit.Month:
            {
                var months = Truncated(duration.Date.Months);
                r1 = additionalShift ? months + (increment * sign) : months;
                r2 = r1 + (increment * sign);
                status = CreateDateDurationRecord(duration.Date.Years, r1, 0, 0, out startDate);
                if (status == TemporalStatus.Completed)
                    status = CreateDateDurationRecord(duration.Date.Years, r2, 0, 0, out endDate);
                else
                    endDate = default;
                break;
            }

            case TemporalUnit.Week:
            {
                // ROW D12: the specification shifts years and months only, so a week window is never recomputed.
                var yearsMonths = duration.Date with { Weeks = 0, Days = 0 };
                status = CalendarDateAdd(calendars, calendar, dateTime.Date, yearsMonths, TemporalOverflow.Constrain, out var weeksStart);
                if (status != TemporalStatus.Completed)
                    return status;

                var weeksEnd = IsoArithmetic.AddDaysToIsoDate(weeksStart, (long)duration.Date.Days);
                status = CalendarDateUntil(calendars, calendar, weeksStart, weeksEnd, TemporalUnit.Week, out var until);
                if (status != TemporalStatus.Completed)
                    return status;

                var weeks = (long)ExactRounding.RoundNumberToIncrement(new BigInteger(duration.Date.Weeks + until.Weeks), BigInteger.One, increment, RoundingMode.Trunc);
                r1 = weeks;
                r2 = weeks + (increment * sign);
                status = CreateDateDurationRecord(duration.Date.Years, duration.Date.Months, r1, 0, out startDate);
                if (status == TemporalStatus.Completed)
                    status = CreateDateDurationRecord(duration.Date.Years, duration.Date.Months, r2, 0, out endDate);
                else
                    endDate = default;
                break;
            }

            default:
            {
                // ROW D12, as for weeks: a day window is never recomputed.
                var days = Truncated(duration.Date.Days);
                r1 = days;
                r2 = days + (increment * sign);
                status = CreateDateDurationRecord(duration.Date.Years, duration.Date.Months, duration.Date.Weeks, r1, out startDate);
                if (status == TemporalStatus.Completed)
                    status = CreateDateDurationRecord(duration.Date.Years, duration.Date.Months, duration.Date.Weeks, r2, out endDate);
                else
                    endDate = default;
                break;
            }
        }

        if (status != TemporalStatus.Completed)
            return status;

        BigInteger startEpochNanoseconds;

        // JSD-0054 SECTION 4'S FIRST NAMED PLACE: the start is the origin when the start duration is zero, as the
        // reference polyfill tests it. The draft's text tests r1 = 0, which a month window with whole years also meets,
        // and would then measure the window from the origin rather than from those years (decision D7's last item).
        if (Sign(startDate) == 0)
        {
            startEpochNanoseconds = originEpochNanoseconds;
        }
        else
        {
            status = CalendarDateAdd(calendars, calendar, dateTime.Date, startDate, TemporalOverflow.Constrain, out var start);
            if (status != TemporalStatus.Completed)
                return status;

            status = EpochFor(timeZone, new IsoDateTime(start, dateTime.Time), out startEpochNanoseconds);
            if (status != TemporalStatus.Completed)
                return status;
        }

        status = CalendarDateAdd(calendars, calendar, dateTime.Date, endDate, TemporalOverflow.Constrain, out var end);
        if (status != TemporalStatus.Completed)
            return status;

        status = EpochFor(timeZone, new IsoDateTime(end, dateTime.Time), out var endEpochNanoseconds);
        if (status != TemporalStatus.Completed)
            return status;

        window = new NudgeWindow(
            r1, r2, startEpochNanoseconds, endEpochNanoseconds, new InternalDuration(startDate, BigInteger.Zero), new InternalDuration(endDate, BigInteger.Zero));
        return TemporalStatus.Completed;
    }

    // The proposal's NudgeToCalendarUnit (s7.5.34): the nudge, and the exact total TotalRelativeDuration reads.
    // Broiler-AI:           Origin=Ported; Spec=Temporal s7.5.34, JSD-0078 decisions D7 and D12 via Broiler.VM JsTemporal.NudgeToCalendarUnit; IP=Low; Security=Medium; Resources=2; Fingerprint=E2CC43
    // Broiler-Human:        PENDING
    private static TemporalStatus NudgeToCalendarUnit(
        int sign, InternalDuration duration, BigInteger originEpochNanoseconds, BigInteger destinationEpochNanoseconds, IsoDateTime dateTime,
        ITimeZoneArithmetic? timeZone, ICalendarArithmetic calendars, CalendarId calendar, long increment, TemporalUnit unit, RoundingMode mode,
        out Nudge nudge, out Fraction total)
    {
        nudge = default;
        total = default;
        var didExpand = false;
        var status = ComputeNudgeWindow(sign, duration, originEpochNanoseconds, dateTime, timeZone, calendars, calendar, increment, unit, false, out var window);
        if (status != TemporalStatus.Completed)
            return status;

        bool Inside(NudgeWindow w) => sign == 1
            ? w.StartEpochNanoseconds <= destinationEpochNanoseconds && destinationEpochNanoseconds <= w.EndEpochNanoseconds
            : w.EndEpochNanoseconds <= destinationEpochNanoseconds && destinationEpochNanoseconds <= w.StartEpochNanoseconds;

        if (!Inside(window))
        {
            // ROW D12: the specification asserts that a week or day window holds the destination, and a day the zone
            // skips breaks it: back from Pacific/Apia's 2011-12-31, the day before is 2011-12-30, which the zone skipped,
            // so the window's end is moved onto its start. The reference polyfill fails its own assertion there and
            // Broiler.VM ends the invocation with a .NET exception; the library answers a status, a RangeError in both
            // engines (decision D12 as revised at SJL-10.3, part C).
            if (unit is TemporalUnit.Week or TemporalUnit.Day)
                return TemporalStatus.OutOfRange;

            status = ComputeNudgeWindow(sign, duration, originEpochNanoseconds, dateTime, timeZone, calendars, calendar, increment, unit, true, out window);
            if (status != TemporalStatus.Completed)
                return status;

            didExpand = true;
        }

        // TOTAL = r1 + progress × increment × sign, progress = (dest - start) / (end - start), exact.
        var span = window.EndEpochNanoseconds - window.StartEpochNanoseconds;
        var progressed = destinationEpochNanoseconds - window.StartEpochNanoseconds;
        var numerator = (window.R1 * span) + (progressed * increment * sign);
        var denominator = span;
        if (denominator.Sign < 0)
        {
            numerator = -numerator;
            denominator = -denominator;
        }

        total = new Fraction(numerator, denominator);
        bool roundUp;
        if (progressed == span)
        {
            roundUp = true;
        }
        else
        {
            // THE SPACING IS ONE INCREMENT, so the window's ends are |r1| and |r2| and the unsigned mode chooses between
            // them for the total's magnitude (s7.5.34 steps 10 to 12).
            var upper = new BigInteger(Math.Abs(window.R2));
            roundUp = ExactRounding.ApplyUnsignedRoundingMode(
                BigInteger.Abs(numerator), denominator, Math.Abs(window.R1), upper, ExactRounding.GetUnsignedRoundingMode(mode, sign < 0)) == upper;
        }

        // DECISION D7 (ROW D8): rounded down, the nudged instant is the window's start, as the pinned text says ("Else,
        // let resultDuration be startDuration; let nudgedEpochNs be startEpochNs"), even after a shifted window, where the
        // polyfill takes the end.
        nudge = roundUp
            ? new Nudge(window.EndDuration, window.EndEpochNanoseconds, true)
            : new Nudge(window.StartDuration, window.StartEpochNanoseconds, didExpand);
        return TemporalStatus.Completed;
    }

    // The proposal's NudgeToZonedTime (s7.5.35): the time rounded within the zone's real day, which may carry a day.
    // Broiler-AI:           Origin=Ported; Spec=Temporal s7.5.35 via Broiler.VM JsTemporal.NudgeToZonedTime; IP=Low; Security=Medium; Resources=2; Fingerprint=FF26E9
    // Broiler-Human:        PENDING
    private static TemporalStatus NudgeToZonedTime(
        int sign, InternalDuration duration, IsoDateTime dateTime, ITimeZoneArithmetic timeZone, ICalendarArithmetic calendars, CalendarId calendar,
        long increment, TemporalUnit unit, RoundingMode mode, out Nudge nudge)
    {
        nudge = default;
        var status = CalendarDateAdd(calendars, calendar, dateTime.Date, duration.Date, TemporalOverflow.Constrain, out var start);
        if (status != TemporalStatus.Completed)
            return status;

        var startDateTime = new IsoDateTime(start, dateTime.Time);
        var endDateTime = new IsoDateTime(IsoArithmetic.AddDaysToIsoDate(start, sign), dateTime.Time);
        status = EpochNanosecondsFor(timeZone, startDateTime, out var startEpochNanoseconds);
        if (status != TemporalStatus.Completed)
            return status;

        status = EpochNanosecondsFor(timeZone, endDateTime, out var endEpochNanoseconds);
        if (status != TemporalStatus.Completed)
            return status;

        var daySpan = endEpochNanoseconds - startEpochNanoseconds;
        var unitIncrement = (BigInteger)DurationCore.UnitLength(unit) * increment;
        status = RoundTimeDurationToIncrement(duration.TimeNanoseconds, unitIncrement, mode, out var rounded);
        if (status != TemporalStatus.Completed)
            return status;

        var beyondDaySpan = rounded - daySpan;
        bool didRoundBeyondDay;
        int dayDelta;
        BigInteger nudgedEpochNanoseconds;
        if (beyondDaySpan.Sign != -sign)
        {
            didRoundBeyondDay = true;
            dayDelta = sign;
            status = RoundTimeDurationToIncrement(beyondDaySpan, unitIncrement, mode, out rounded);
            if (status != TemporalStatus.Completed)
                return status;

            nudgedEpochNanoseconds = endEpochNanoseconds + rounded;
        }
        else
        {
            didRoundBeyondDay = false;
            dayDelta = 0;
            nudgedEpochNanoseconds = startEpochNanoseconds + rounded;
        }

        status = CreateDateDurationRecord(duration.Date.Years, duration.Date.Months, duration.Date.Weeks, duration.Date.Days + dayDelta, out var date);
        if (status != TemporalStatus.Completed)
            return status;

        nudge = new Nudge(new InternalDuration(date, rounded), nudgedEpochNanoseconds, didRoundBeyondDay);
        return TemporalStatus.Completed;
    }

    // The proposal's NudgeToDayOrTime (s7.5.36): the time with its days of 24 hours rounded, days split off again where the
    // largest unit is a date unit.
    // Broiler-AI:           Origin=Ported; Spec=Temporal s7.5.36 via Broiler.VM JsTemporal.NudgeToDayOrTime; IP=Low; Security=Medium; Resources=1; Fingerprint=D240B0
    // Broiler-Human:        PENDING
    private static TemporalStatus NudgeToDayOrTime(
        InternalDuration duration, BigInteger destinationEpochNanoseconds, TemporalUnit largestUnit, long increment, TemporalUnit smallestUnit,
        RoundingMode mode, out Nudge nudge)
    {
        nudge = default;
        var status = Add24HourDaysToTimeDuration(duration.TimeNanoseconds, duration.Date.Days, out var time);
        if (status != TemporalStatus.Completed)
            return status;

        status = RoundTimeDurationToIncrement(time, (BigInteger)DurationCore.UnitLength(smallestUnit) * increment, mode, out var rounded);
        if (status != TemporalStatus.Completed)
            return status;

        var difference = rounded - time;
        var wholeDays = BigInteger.Divide(time, Day);
        var roundedWholeDays = BigInteger.Divide(rounded, Day);
        var dayDelta = roundedWholeDays - wholeDays;
        var didExpandDays = dayDelta.Sign == time.Sign;
        var nudgedEpochNanoseconds = destinationEpochNanoseconds + difference;
        var days = BigInteger.Zero;
        var remainder = rounded;
        if (IsDateUnit(largestUnit))
        {
            days = roundedWholeDays;
            remainder = rounded - (roundedWholeDays * Day);
        }

        status = CreateDateDurationRecord(
            duration.Date.Years, duration.Date.Months, duration.Date.Weeks, ExactRounding.ToDouble(days, BigInteger.One), out var date);
        if (status != TemporalStatus.Completed)
            return status;

        nudge = new Nudge(new InternalDuration(date, remainder), nudgedEpochNanoseconds, didExpandDays);
        return TemporalStatus.Completed;
    }

    // The proposal's BubbleRelativeDuration (s7.5.37): each larger unit up to the largest taken whole while the nudged
    // instant reaches its next boundary.
    // Broiler-AI:           Origin=Ported; Spec=Temporal s7.5.37 via Broiler.VM JsTemporal.BubbleRelativeDuration; IP=Low; Security=Medium; Resources=2; Fingerprint=500E14
    // Broiler-Human:        PENDING
    private static TemporalStatus BubbleRelativeDuration(
        int sign, InternalDuration duration, BigInteger nudgedEpochNanoseconds, IsoDateTime dateTime, ITimeZoneArithmetic? timeZone,
        ICalendarArithmetic calendars, CalendarId calendar, TemporalUnit largestUnit, TemporalUnit smallestUnit, out InternalDuration result)
    {
        result = duration;
        if (smallestUnit == largestUnit)
            return TemporalStatus.Completed;

        for (var unit = smallestUnit - 1; unit >= largestUnit; unit--)
        {
            if (unit == TemporalUnit.Week && largestUnit != TemporalUnit.Week)
                continue;

            TemporalStatus status;
            DateDuration endDuration;
            if (unit == TemporalUnit.Year)
                status = CreateDateDurationRecord(result.Date.Years + sign, 0, 0, 0, out endDuration);
            else if (unit == TemporalUnit.Month)
                status = CreateDateDurationRecord(result.Date.Years, result.Date.Months + sign, 0, 0, out endDuration);
            else
                status = CreateDateDurationRecord(result.Date.Years, result.Date.Months, result.Date.Weeks + sign, 0, out endDuration);

            if (status != TemporalStatus.Completed)
                return status;

            status = CalendarDateAdd(calendars, calendar, dateTime.Date, endDuration, TemporalOverflow.Constrain, out var end);
            if (status != TemporalStatus.Completed)
                return status;

            status = EpochFor(timeZone, new IsoDateTime(end, dateTime.Time), out var endEpochNanoseconds);
            if (status != TemporalStatus.Completed)
                return status;

            var beyondEnd = nudgedEpochNanoseconds - endEpochNanoseconds;
            if (beyondEnd.Sign == -sign)
                break;

            result = new InternalDuration(endDuration, BigInteger.Zero);
        }

        return TemporalStatus.Completed;
    }

    // ---- The providers ---------------------------------------------------------------------------------------------

    // CalendarDateAdd through the provider, its fields converted exactly and its status mapped.
    // Broiler-AI:           Origin=Ported; Spec=Temporal s12.3.7 via Broiler.VM JsTemporal.DateAdd; IP=Low; Security=Low; Resources=1; Fingerprint=8A2D4A
    // Broiler-Human:        PENDING
    internal static TemporalStatus CalendarDateAdd(
        ICalendarArithmetic calendars, CalendarId calendar, IsoDate date, DateDuration duration, TemporalOverflow overflow, out IsoDate result)
    {
        result = default;
        if (!FitsCalendar(duration.Years) || !FitsCalendar(duration.Months) || !FitsCalendar(duration.Weeks) || !FitsCalendar(duration.Days))
            return TemporalStatus.OutOfRange;

        var status = calendars.DateAdd(
            calendar, date, new CalendarDuration((long)duration.Years, (long)duration.Months, (long)duration.Weeks, (long)duration.Days),
            overflow == TemporalOverflow.Reject ? CalendarOverflow.Reject : CalendarOverflow.Constrain, out result);
        return Map(status);
    }

    // CalendarDateUntil through the provider, its largest unit a date unit, its fields made Numbers.
    // Broiler-AI:           Origin=Ported; Spec=Temporal s12.3.9 via Broiler.VM JsTemporal.DateUntil; IP=Low; Security=Low; Resources=1; Fingerprint=D91258
    // Broiler-Human:        PENDING
    internal static TemporalStatus CalendarDateUntil(
        ICalendarArithmetic calendars, CalendarId calendar, IsoDate one, IsoDate two, TemporalUnit largestUnit, out DateDuration result)
    {
        result = default;
        var unit = largestUnit switch
        {
            TemporalUnit.Year => CalendarUnit.Year,
            TemporalUnit.Month => CalendarUnit.Month,
            TemporalUnit.Week => CalendarUnit.Week,
            TemporalUnit.Day => CalendarUnit.Day,
            _ => throw new ArgumentOutOfRangeException(nameof(largestUnit), largestUnit, "The largest unit is not a date unit."),
        };

        var status = calendars.DateUntil(calendar, one, two, unit, out var until);
        if (status != CalendarStatus.Completed)
            return Map(status);

        result = new DateDuration(until.Years + 0.0, until.Months + 0.0, until.Weeks + 0.0, until.Days + 0.0);
        return TemporalStatus.Completed;
    }

    // GetISODateTimeFor (s11.1.10): an instant's wall clock in a zone.
    // Broiler-AI:           Origin=Ported; Spec=Temporal s11.1.10 via Broiler.VM JsTemporal.IsoDateTimeFor; IP=Low; Security=Low; Resources=0; Fingerprint=511DE2
    // Broiler-Human:        PENDING
    internal static IsoDateTime IsoDateTimeFor(ITimeZoneArithmetic timeZone, BigInteger epochNanoseconds) =>
        IsoArithmetic.GetIsoPartsFromEpoch(epochNanoseconds + timeZone.GetOffsetNanosecondsFor(epochNanoseconds));

    // GetEpochNanosecondsFor (s11.1.11) under compatible, its failure out of range.
    // Broiler-AI:           Origin=Ported; Spec=Temporal s11.1.11 via Broiler.VM JsTemporal.EpochNsFor; IP=Low; Security=Low; Resources=0; Fingerprint=259961
    // Broiler-Human:        PENDING
    internal static TemporalStatus EpochNanosecondsFor(ITimeZoneArithmetic timeZone, IsoDateTime dateTime, out BigInteger epochNanoseconds) =>
        timeZone.GetEpochNanosecondsFor(dateTime, Disambiguation.Compatible, out epochNanoseconds) == TimeStatus.Completed
            ? TemporalStatus.Completed
            : TemporalStatus.OutOfRange;

    // A wall clock's instant: UTC's reading where no zone is set, the zone's otherwise.
    // Broiler-AI:           Origin=Ported; Spec=Temporal s7.5.33 via Broiler.VM JsTemporal.EpochFor; IP=Low; Security=Low; Resources=0; Fingerprint=2F5040
    // Broiler-Human:        PENDING
    private static TemporalStatus EpochFor(ITimeZoneArithmetic? timeZone, IsoDateTime dateTime, out BigInteger epochNanoseconds)
    {
        if (timeZone is null)
        {
            epochNanoseconds = IsoArithmetic.GetUtcEpochNanoseconds(dateTime);
            return TemporalStatus.Completed;
        }

        return EpochNanosecondsFor(timeZone, dateTime, out epochNanoseconds);
    }

    // A calendar's status as the durations sub-area's: a field refused under reject is an invalid date, any other
    // failure out of range (a RangeError in both engines).
    // Broiler-AI:           Origin=AI; Spec=JSD-0078 statuses; IP=None; Security=None; Resources=0; Fingerprint=B1F669
    // Broiler-Human:        PENDING
    internal static TemporalStatus Map(CalendarStatus status) => status switch
    {
        CalendarStatus.Completed => TemporalStatus.Completed,
        CalendarStatus.FieldOutOfRange or CalendarStatus.MonthNotInYear => TemporalStatus.InvalidDate,
        _ => TemporalStatus.OutOfRange,
    };

    // Whether a date-duration field converts to the calendar's exact integers.
    // Broiler-AI:           Origin=AI; Spec=JSD-0078 decision D12; IP=None; Security=Low; Resources=0; Fingerprint=1DC9E9
    // Broiler-Human:        PENDING
    private static bool FitsCalendar(double value) => double.IsFinite(value) && Math.Abs(value) <= MaxCalendarField;
}
