// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   15
// Annotated:        15/15
// Exempt:           0
// Human-reviewed:   0/15
// IP risk:          Low
// Security risk:    Medium
// Criteria:         0/0
// Resource impact:  1/10 max
// Unverified:       15
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Numerics;
using Broiler.ECMAScript.Time;

namespace Broiler.ECMAScript.Temporal;

/// <summary>
/// The Temporal proposal's representable limits, each a check that takes any record an engine hands it and answers a
/// <see cref="TemporalStatus"/>: dates, date-times and year-months (<c>ISODateWithinLimits</c>,
/// <c>ISODateTimeWithinLimits</c>, <c>ISOYearMonthWithinLimits</c>), instants (<c>IsValidEpochNanoseconds</c>), the
/// day range (<c>CheckISODaysRange</c>) and durations (<c>IsValidDuration</c>).
/// </summary>
/// <remarks>
/// <para>
/// <b>No check throws</b> (JSD-0078, decision D9). A year is any <see cref="long"/>: a year outside -271821 to
/// 275760 is out of every range and is refused before any civil-day arithmetic, whose precondition is a year of
/// magnitude at most 2^53. So the faulting constructors of Broiler.VM's row R6 (a year of 2^53 + 2 or ±10^20) answer
/// <see cref="TemporalStatus.OutOfRange"/> here. A record that is not a date or a time answers
/// <see cref="TemporalStatus.InvalidDate"/> or <see cref="TemporalStatus.InvalidTime"/> where the specification
/// asserts one.
/// </para>
/// <para>
/// The instant limits themselves are SJL-8's <see cref="ZonedTime.MinEpochNanoseconds"/> and
/// <see cref="ZonedTime.MaxEpochNanoseconds"/>, ±10^8 days from the epoch.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=Ported; Spec=JSD-0078 decision D9, Temporal limits via Broiler.VM JsTemporalCore; IP=Low; Security=Medium; Resources=1; Fingerprint=C11743
// Broiler-Human:        PENDING
public static class TemporalLimits
{
    /// <summary>The nanoseconds in a 24-hour day, the proposal's <c>nsPerDay</c>.</summary>
    // Broiler-AI:           Origin=Ported; Spec=Temporal nsPerDay via Broiler.VM JsTemporalCore.NsPerDay; IP=Low; Security=None; Resources=0; Fingerprint=3BBE72
    // Broiler-Human:        PENDING
    public const long NanosecondsPerDay = 86_400_000_000_000L;

    /// <summary>The first year any representable date, date-time or year-month has: -271821.</summary>
    // Broiler-AI:           Origin=AI; Spec=Temporal ISODateTimeWithinLimits; IP=Low; Security=None; Resources=0; Fingerprint=8728C4
    // Broiler-Human:        PENDING
    public const long MinYear = -271821;

    /// <summary>The last year any representable date, date-time or year-month has: 275760.</summary>
    // Broiler-AI:           Origin=AI; Spec=Temporal ISODateTimeWithinLimits; IP=Low; Security=None; Resources=0; Fingerprint=F19122
    // Broiler-Human:        PENDING
    public const long MaxYear = 275760;

    /// <summary>The epoch days from 1970-01-01 to either end of the instant range: 10^8.</summary>
    // Broiler-AI:           Origin=Ported; Spec=Temporal CheckISODaysRange via Broiler.VM JsTemporalCore.DaysWithinRange; IP=Low; Security=None; Resources=0; Fingerprint=ED9DFC
    // Broiler-Human:        PENDING
    public const long MaxEpochDays = 100_000_000;

    /// <summary>The bound a duration's years, months and weeks stay below in magnitude: 2^32.</summary>
    // Broiler-AI:           Origin=Ported; Spec=Temporal IsValidDuration via Broiler.VM JsTemporalCore.IsValidDuration; IP=Low; Security=None; Resources=0; Fingerprint=6CD076
    // Broiler-Human:        PENDING
    public const double CalendarDurationLimit = 4294967296.0;

    /// <summary>The proposal's <c>maxTimeDuration</c>: 2^53 × 10^9 - 1 nanoseconds, the largest magnitude of a time duration.</summary>
    // Broiler-AI:           Origin=Ported; Spec=Temporal maxTimeDuration via Broiler.VM JsTemporalCore.MaxTimeDuration; IP=Low; Security=None; Resources=0; Fingerprint=0AA33F
    // Broiler-Human:        PENDING
    public static readonly BigInteger MaxTimeDuration = (BigInteger.One << 53) * 1_000_000_000 - 1;

    // A day in nanoseconds, as an exact integer.
    // Broiler-AI:           Origin=Ported; Spec=Temporal nsPerDay via Broiler.VM JsTemporalCore.NsPerDay; IP=Low; Security=None; Resources=0; Fingerprint=576D12
    // Broiler-Human:        PENDING
    private static readonly BigInteger Day = new(NanosecondsPerDay);

    /// <summary>
    /// Whether a date is a representable <c>Temporal.PlainDate</c>: the proposal's <c>ISODateWithinLimits</c>, the
    /// date at noon a representable date-time, from -271821-04-19 to +275760-09-13.
    /// </summary>
    /// <param name="date">The date, of any year.</param>
    /// <returns>
    /// <see cref="TemporalStatus.Completed"/>; <see cref="TemporalStatus.InvalidDate"/> where the fields are not a date;
    /// <see cref="TemporalStatus.OutOfRange"/> where the date is outside the limits.
    /// </returns>
    // Broiler-AI:           Origin=Ported; Spec=Temporal ISODateWithinLimits via Broiler.VM JsTemporalCore.DateWithinLimits; IP=Low; Security=Medium; Resources=1; Fingerprint=7B46C3
    // Broiler-Human:        PENDING
    public static TemporalStatus CheckIsoDate(IsoDate date) => CheckIsoDateTime(new IsoDateTime(date, new IsoTime(12, 0, 0, 0, 0, 0)));

    /// <summary>
    /// Whether a date-time is a representable <c>Temporal.PlainDateTime</c>: the proposal's
    /// <c>ISODateTimeWithinLimits</c>, its UTC reading strictly within a day either side of the instant range.
    /// </summary>
    /// <param name="dateTime">The date-time, of any year.</param>
    /// <returns>
    /// <see cref="TemporalStatus.Completed"/>; <see cref="TemporalStatus.InvalidDate"/> or
    /// <see cref="TemporalStatus.InvalidTime"/> where the fields are not a date or a time;
    /// <see cref="TemporalStatus.OutOfRange"/> where the date-time is outside the limits.
    /// </returns>
    // Broiler-AI:           Origin=Ported; Spec=Temporal ISODateTimeWithinLimits via Broiler.VM JsTemporalCore.DateTimeWithinLimits, corrected by JSD-0078 decision D9 (row R6); IP=Low; Security=Medium; Resources=1; Fingerprint=8DBE15
    // Broiler-Human:        PENDING
    public static TemporalStatus CheckIsoDateTime(IsoDateTime dateTime)
    {
        var (date, time) = (dateTime.Date, dateTime.Time);
        if (!IsoCalendarMath.IsValidIsoDate(date.Year, date.Month, date.Day))
            return TemporalStatus.InvalidDate;
        if (!IsValidTime(time))
            return TemporalStatus.InvalidTime;

        // ROW R6: a year outside the limits' years is refused before the civil-day arithmetic, whose precondition
        // is a year of magnitude at most 2^53; every year inside them answers as Broiler.VM's exact check does.
        if (date.Year is < MinYear or > MaxYear)
            return TemporalStatus.OutOfRange;

        var ns = IsoArithmetic.GetUtcEpochNanoseconds(dateTime);
        return ns > ZonedTime.MinEpochNanoseconds - Day && ns < ZonedTime.MaxEpochNanoseconds + Day
            ? TemporalStatus.Completed
            : TemporalStatus.OutOfRange;
    }

    /// <summary>
    /// Whether a year and month are a representable <c>Temporal.PlainYearMonth</c>: the proposal's
    /// <c>ISOYearMonthWithinLimits</c>, from April -271821 to September 275760, whatever the reference day.
    /// </summary>
    /// <param name="yearMonth">The year-month's ISO date, its day the reference day, of any year.</param>
    /// <returns>
    /// <see cref="TemporalStatus.Completed"/>; <see cref="TemporalStatus.InvalidDate"/> where the fields are not a date;
    /// <see cref="TemporalStatus.OutOfRange"/> where the year and month are outside the limits.
    /// </returns>
    // Broiler-AI:           Origin=Ported; Spec=Temporal ISOYearMonthWithinLimits via Broiler.VM JsTemporalCore.YearMonthWithinLimits; IP=Low; Security=Low; Resources=1; Fingerprint=5DBEDC
    // Broiler-Human:        PENDING
    public static TemporalStatus CheckIsoYearMonth(IsoDate yearMonth)
    {
        if (!IsoCalendarMath.IsValidIsoDate(yearMonth.Year, yearMonth.Month, yearMonth.Day))
            return TemporalStatus.InvalidDate;

        var within = yearMonth.Year is >= MinYear and <= MaxYear &&
            !(yearMonth.Year == MinYear && yearMonth.Month < 4) && !(yearMonth.Year == MaxYear && yearMonth.Month > 9);
        return within ? TemporalStatus.Completed : TemporalStatus.OutOfRange;
    }

    /// <summary>
    /// Whether a date is within 10^8 days of 1970-01-01: the proposal's <c>CheckISODaysRange</c>, which guards
    /// <c>GetUTCEpochNanoseconds</c> where an offset is applied, as in parsing an instant.
    /// </summary>
    /// <param name="date">The date, of any year.</param>
    /// <returns>
    /// <see cref="TemporalStatus.Completed"/>; <see cref="TemporalStatus.InvalidDate"/> where the fields are not a date;
    /// <see cref="TemporalStatus.OutOfRange"/> where the date is further than 10^8 days from the epoch (the
    /// specification's <c>RangeError</c>).
    /// </returns>
    // Broiler-AI:           Origin=Ported; Spec=Temporal CheckISODaysRange via Broiler.VM JsTemporalCore.DaysWithinRange; IP=Low; Security=Medium; Resources=1; Fingerprint=A75656
    // Broiler-Human:        PENDING
    public static TemporalStatus CheckIsoDaysRange(IsoDate date)
    {
        if (!IsoCalendarMath.IsValidIsoDate(date.Year, date.Month, date.Day))
            return TemporalStatus.InvalidDate;
        if (date.Year is < MinYear or > MaxYear)
            return TemporalStatus.OutOfRange;

        return Math.Abs(IsoCalendarMath.EpochDays(date.Year, date.Month, date.Day)) <= MaxEpochDays
            ? TemporalStatus.Completed
            : TemporalStatus.OutOfRange;
    }

    /// <summary>Whether an exact count of nanoseconds since the epoch is an instant: the proposal's <c>IsValidEpochNanoseconds</c>.</summary>
    /// <param name="epochNanoseconds">The nanoseconds since the epoch.</param>
    /// <returns><see cref="TemporalStatus.Completed"/>, or <see cref="TemporalStatus.OutOfRange"/> beyond ±8.64 × 10^21.</returns>
    // Broiler-AI:           Origin=Ported; Spec=Temporal IsValidEpochNanoseconds via Broiler.VM JsTemporalCore.IsValidEpochNs; IP=Low; Security=Low; Resources=1; Fingerprint=9221C2
    // Broiler-Human:        PENDING
    public static TemporalStatus CheckEpochNanoseconds(BigInteger epochNanoseconds) =>
        epochNanoseconds >= ZonedTime.MinEpochNanoseconds && epochNanoseconds <= ZonedTime.MaxEpochNanoseconds
            ? TemporalStatus.Completed
            : TemporalStatus.OutOfRange;

    /// <summary>
    /// Whether six fields form a time of day: the proposal's <c>IsValidTime</c>, each field within its range, with no
    /// leap second.
    /// </summary>
    /// <param name="time">The time.</param>
    /// <returns>Whether the hour is 0 to 23, the minute and second 0 to 59, and the sub-second fields 0 to 999.</returns>
    // Broiler-AI:           Origin=Ported; Spec=Temporal IsValidTime via Broiler.VM JsTemporalCore.IsValidTime; IP=Low; Security=Low; Resources=0; Fingerprint=587DD6
    // Broiler-Human:        PENDING
    public static bool IsValidTime(IsoTime time) =>
        time.Hour is >= 0 and <= 23 && time.Minute is >= 0 and <= 59 && time.Second is >= 0 and <= 59 &&
        time.Millisecond is >= 0 and <= 999 && time.Microsecond is >= 0 and <= 999 && time.Nanosecond is >= 0 and <= 999;

    /// <summary>
    /// Whether ten fields are a valid duration: the proposal's <c>IsValidDuration</c> - every field finite, no two of
    /// opposite signs, the years, months and weeks below 2^32 in magnitude, and the days and time fields together at
    /// most <see cref="MaxTimeDuration"/> nanoseconds, counted exactly.
    /// </summary>
    /// <param name="duration">The fields, each an integer.</param>
    /// <returns><see cref="TemporalStatus.Completed"/>, or <see cref="TemporalStatus.InvalidDuration"/>.</returns>
    // Broiler-AI:           Origin=Ported; Spec=Temporal IsValidDuration via Broiler.VM JsTemporalCore.IsValidDuration; IP=Low; Security=Medium; Resources=1; Fingerprint=3A6B12
    // Broiler-Human:        PENDING
    public static TemporalStatus CheckDuration(DurationFields duration)
    {
        ReadOnlySpan<double> fields =
        [
            duration.Years, duration.Months, duration.Weeks, duration.Days, duration.Hours, duration.Minutes, duration.Seconds,
            duration.Milliseconds, duration.Microseconds, duration.Nanoseconds,
        ];
        var sign = 0;
        foreach (var value in fields)
        {
            if (!double.IsFinite(value))
                return TemporalStatus.InvalidDuration;

            var valueSign = value < 0 ? -1 : value > 0 ? 1 : 0;
            if (valueSign != 0)
            {
                if (sign != 0 && sign != valueSign)
                    return TemporalStatus.InvalidDuration;
                sign = valueSign;
            }
        }

        if (Math.Abs(duration.Years) >= CalendarDurationLimit || Math.Abs(duration.Months) >= CalendarDurationLimit ||
            Math.Abs(duration.Weeks) >= CalendarDurationLimit)
            return TemporalStatus.InvalidDuration;

        var normalized = new BigInteger(duration.Days) * Day + DurationCore.TimeFromComponents(duration);
        return BigInteger.Abs(normalized) <= MaxTimeDuration ? TemporalStatus.Completed : TemporalStatus.InvalidDuration;
    }
}
