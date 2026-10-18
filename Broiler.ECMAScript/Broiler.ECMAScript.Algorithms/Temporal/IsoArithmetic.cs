// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   27
// Annotated:        27/27
// Exempt:           0
// Human-reviewed:   0/27
// IP risk:          Low
// Security risk:    Medium
// Criteria:         0/0
// Resource impact:  1/10 max
// Unverified:       27
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Numerics;
using Broiler.ECMAScript.Numbers;
using Broiler.ECMAScript.Time;

namespace Broiler.ECMAScript.Temporal;

/// <summary>
/// The ISO 8601 arithmetic Temporal is built on, over SJL-8's records (JSD-0078, decision D8): comparison, balancing,
/// regulation and addition of dates and times, the date-time's epoch nanoseconds in both directions, the rounding of a
/// time, a date-time and an instant, and the addition of a time duration to an instant.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every quantity is exact.</b> A time duration and an epoch-nanoseconds value are <see cref="BigInteger"/>s, and the
/// civil-day arithmetic is SJL-6's <see cref="IsoCalendarMath"/>. A time balanced past midnight answers its overflow days
/// beside the <see cref="IsoTime"/>, as an <c>out</c> argument, never in it (decision D8).
/// </para>
/// <para>
/// <b>A failure the specification throws is a status</b> (X3): a regulation under <see cref="TemporalOverflow.Reject"/>
/// answers <see cref="TemporalStatus.InvalidDate"/> or <see cref="TemporalStatus.InvalidTime"/>, and an addition past a
/// limit <see cref="TemporalStatus.OutOfRange"/>. A library exception is a broken precondition, which each member lists;
/// the preconditions are far outside every limit Temporal sets, so an adapter that checks the limits never meets them.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=Ported; Spec=JSD-0078, Temporal ISO arithmetic via Broiler.VM JsTemporalCore; IP=Low; Security=Medium; Resources=1; Fingerprint=9F1F64
// Broiler-Human:        PENDING
public static class IsoArithmetic
{
    // The bound of a day count the balancing takes, 2^60, the civil-day arithmetic's own.
    // Broiler-AI:           Origin=AI; Spec=Temporal ISO 8601 calendar; IP=Low; Security=Low; Resources=0; Fingerprint=ACF87E
    // Broiler-Human:        PENDING
    private const long DayCountLimit = 1L << 60;

    // A day in nanoseconds, as an exact integer.
    // Broiler-AI:           Origin=Ported; Spec=Temporal nsPerDay via Broiler.VM JsTemporalCore.NsPerDay; IP=Low; Security=None; Resources=0; Fingerprint=055C53
    // Broiler-Human:        PENDING
    private static readonly BigInteger Day = new(TemporalLimits.NanosecondsPerDay);

    /// <summary>Compares two dates: the proposal's <c>CompareISODate</c>.</summary>
    /// <param name="one">The first date.</param>
    /// <param name="two">The second date.</param>
    /// <returns>-1, 0 or 1 as the first is before, equal to or after the second.</returns>
    // Broiler-AI:           Origin=Ported; Spec=Temporal CompareISODate via Broiler.VM JsTemporalCore.Compare; IP=Low; Security=Low; Resources=0; Fingerprint=9AE35B
    // Broiler-Human:        PENDING
    public static int CompareIsoDate(IsoDate one, IsoDate two) =>
        one.Year != two.Year ? (one.Year > two.Year ? 1 : -1)
        : one.Month != two.Month ? (one.Month > two.Month ? 1 : -1)
        : one.Day != two.Day ? (one.Day > two.Day ? 1 : -1)
        : 0;

    /// <summary>Compares two times of day: the proposal's <c>CompareTimeRecord</c>.</summary>
    /// <param name="one">The first time.</param>
    /// <param name="two">The second time.</param>
    /// <returns>-1, 0 or 1 as the first is before, equal to or after the second.</returns>
    // Broiler-AI:           Origin=Ported; Spec=Temporal CompareTimeRecord via Broiler.VM JsTemporalCore.Compare; IP=Low; Security=Low; Resources=0; Fingerprint=1ECC13
    // Broiler-Human:        PENDING
    public static int CompareTime(IsoTime one, IsoTime two) => Math.Sign(TimeToNanoseconds(one).CompareTo(TimeToNanoseconds(two)));

    /// <summary>Compares two date-times: the proposal's <c>CompareISODateTime</c>.</summary>
    /// <param name="one">The first date-time.</param>
    /// <param name="two">The second date-time.</param>
    /// <returns>-1, 0 or 1 as the first is before, equal to or after the second.</returns>
    // Broiler-AI:           Origin=Ported; Spec=Temporal CompareISODateTime via Broiler.VM JsTemporalCore.Compare; IP=Low; Security=Low; Resources=0; Fingerprint=EE5667
    // Broiler-Human:        PENDING
    public static int CompareIsoDateTime(IsoDateTime one, IsoDateTime two)
    {
        var date = CompareIsoDate(one.Date, two.Date);
        return date != 0 ? date : CompareTime(one.Time, two.Time);
    }

    /// <summary>A time's nanoseconds since midnight, as <c>TimeDurationFromComponents</c> reckons a time record's fields.</summary>
    /// <param name="time">The time, valid.</param>
    /// <returns>((((hour × 60 + minute) × 60 + second) × 1000 + millisecond) × 1000 + microsecond) × 1000 + nanosecond, below 8.64 × 10^13.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The time is not valid (<see cref="TemporalLimits.IsValidTime"/>).</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal TimeDurationFromComponents via Broiler.VM JsTemporalCore.TimeNs; IP=Low; Security=Low; Resources=0; Fingerprint=5D335F
    // Broiler-Human:        PENDING
    public static long TimeToNanoseconds(IsoTime time)
    {
        if (!TemporalLimits.IsValidTime(time))
            throw new ArgumentOutOfRangeException(nameof(time), "The time is not a valid time.");

        return ((((((long)time.Hour * 60) + time.Minute) * 60 + time.Second) * 1000 + time.Millisecond) * 1000 + time.Microsecond) * 1000 +
            time.Nanosecond;
    }

    /// <summary>
    /// The date a year, a month and a day that may lie outside their ranges name: the area design's
    /// <c>BalanceISODate</c>, which the pinned text writes inline as <c>BalanceISOYearMonth</c> and then
    /// <c>AddDaysToISODate</c>'s <c>ISODateToEpochDays</c> and <c>EpochDaysToISODate</c>.
    /// </summary>
    /// <param name="year">The year, at most 2^53 in magnitude after the month is carried into it.</param>
    /// <param name="month">The one-based month, any integer of magnitude at most 2^62; it carries into the year.</param>
    /// <param name="day">The one-based day, any integer of magnitude at most 2^60; it carries into the month.</param>
    /// <returns>The date, valid in the ISO 8601 calendar and not necessarily within Temporal's limits.</returns>
    /// <exception cref="ArgumentOutOfRangeException">An argument's magnitude is above its bound, or the result is further than 2^60 days from the epoch.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal BalanceISOYearMonth and AddDaysToISODate via Broiler.VM JsTemporalCore.AddDays; IP=Low; Security=Medium; Resources=1; Fingerprint=B32444
    // Broiler-Human:        PENDING
    public static IsoDate BalanceIsoDate(long year, long month, long day)
    {
        CheckDayCount(day, nameof(day));
        var (balancedYear, balancedMonth) = IsoCalendarMath.BalanceYearMonth(year, month);
        return FromEpochDays(IsoCalendarMath.EpochDays(balancedYear, balancedMonth, 1) + day - 1);
    }

    /// <summary>A date moved by a number of days: the proposal's <c>AddDaysToISODate</c>.</summary>
    /// <param name="date">The date, its month from 1 to 12 and its year at most 2^53 in magnitude.</param>
    /// <param name="days">The days, at most 2^60 in magnitude.</param>
    /// <returns>The date, not necessarily within Temporal's limits.</returns>
    /// <exception cref="ArgumentOutOfRangeException">An argument is outside its bound, or the result is further than 2^60 days from the epoch.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal AddDaysToISODate via Broiler.VM JsTemporalCore.AddDays; IP=Low; Security=Medium; Resources=1; Fingerprint=5359D6
    // Broiler-Human:        PENDING
    public static IsoDate AddDaysToIsoDate(IsoDate date, long days)
    {
        CheckDayCount(days, nameof(days));
        return FromEpochDays(IsoCalendarMath.EpochDays(date.Year, date.Month, date.Day) + days);
    }

    /// <summary>
    /// A count of nanoseconds from a midnight, as a time of day and the whole days it passes: the proposal's
    /// <c>BalanceTime</c>, its overflow days answered beside the time (decision D8).
    /// </summary>
    /// <param name="nanoseconds">The nanoseconds from midnight, of either sign.</param>
    /// <param name="days">The days carried: floor(<paramref name="nanoseconds"/> / 8.64 × 10^13).</param>
    /// <returns>The time of day.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The days carried do not fit a <see cref="long"/>.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal BalanceTime via Broiler.VM JsTemporalCore.BalanceTime; IP=Low; Security=Medium; Resources=1; Fingerprint=24ED0B
    // Broiler-Human:        PENDING
    public static IsoTime BalanceTime(BigInteger nanoseconds, out long days)
    {
        var whole = BigInteger.DivRem(nanoseconds, Day, out var within);
        if (within.Sign < 0)
        {
            whole -= 1;
            within += Day;
        }

        if (whole < long.MinValue || whole > long.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(nanoseconds), "The days carried do not fit a 64-bit integer.");

        days = (long)whole;
        var ns = (long)within;
        return new IsoTime(
            (int)(ns / 3_600_000_000_000L),
            (int)(ns / 60_000_000_000L % 60),
            (int)(ns / 1_000_000_000L % 60),
            (int)(ns / 1_000_000L % 1000),
            (int)(ns / 1000 % 1000),
            (int)(ns % 1000));
    }

    /// <summary>
    /// Six time fields of any magnitude and sign, balanced: the proposal's <c>BalanceTime</c> of its six components,
    /// each carried into the next larger unit, the overflow days answered beside the time.
    /// </summary>
    /// <param name="hour">The hours, an integer.</param>
    /// <param name="minute">The minutes, an integer.</param>
    /// <param name="second">The seconds, an integer.</param>
    /// <param name="millisecond">The milliseconds, an integer.</param>
    /// <param name="microsecond">The microseconds, an integer.</param>
    /// <param name="nanosecond">The nanoseconds, an integer.</param>
    /// <param name="days">The days carried.</param>
    /// <returns>The time of day.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A field is not finite, or the days carried do not fit a <see cref="long"/>.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal BalanceTime via Broiler.VM JsTemporalCore.BalanceTime; IP=Low; Security=Medium; Resources=1; Fingerprint=4CF3D5
    // Broiler-Human:        PENDING
    public static IsoTime BalanceTime(double hour, double minute, double second, double millisecond, double microsecond, double nanosecond, out long days)
    {
        if (!double.IsFinite(hour) || !double.IsFinite(minute) || !double.IsFinite(second) ||
            !double.IsFinite(millisecond) || !double.IsFinite(microsecond) || !double.IsFinite(nanosecond))
            throw new ArgumentOutOfRangeException(nameof(hour), "A time field is not finite.");

        return BalanceTime(DurationCore.TimeFromComponents(hour, minute, second, millisecond, microsecond, nanosecond), out days);
    }

    /// <summary>A time duration added to a time of day: the proposal's <c>AddTime</c>, exact, the overflow days answered beside the time.</summary>
    /// <param name="time">The time, valid.</param>
    /// <param name="timeDuration">The time duration in nanoseconds, of either sign.</param>
    /// <param name="days">The days carried, of either sign.</param>
    /// <returns>The time of day.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The days carried do not fit a <see cref="long"/>.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal AddTime via Broiler.VM JsTemporalCore.AddTime; IP=Low; Security=Medium; Resources=1; Fingerprint=D71591
    // Broiler-Human:        PENDING
    public static IsoTime AddTime(IsoTime time, BigInteger timeDuration, out long days) =>
        BalanceTime(TimeToNanoseconds(time) + timeDuration, out days);

    /// <summary>The time duration from one time of day to another: the proposal's <c>DifferenceTime</c>.</summary>
    /// <param name="one">The earlier operand.</param>
    /// <param name="two">The later operand.</param>
    /// <returns>The nanoseconds from <paramref name="one"/> to <paramref name="two"/>, of either sign and below a day in magnitude.</returns>
    // Broiler-AI:           Origin=Ported; Spec=Temporal DifferenceTime via Broiler.VM JsTemporalCore.DifferenceTime; IP=Low; Security=Low; Resources=0; Fingerprint=5D166D
    // Broiler-Human:        PENDING
    public static BigInteger DifferenceTime(IsoTime one, IsoTime two) => TimeToNanoseconds(two) - TimeToNanoseconds(one);

    /// <summary>
    /// A year, a month and a day made a date under an overflow: the proposal's <c>RegulateISODate</c>. Under
    /// <see cref="TemporalOverflow.Constrain"/> the month is clamped to 1 to 12 and then the day to the month's length;
    /// under <see cref="TemporalOverflow.Reject"/> fields that are not a date are refused.
    /// </summary>
    /// <param name="year">The year, any; the caller's limit check follows (the year is never clamped).</param>
    /// <param name="month">The month, an integer of any magnitude.</param>
    /// <param name="day">The day, an integer of any magnitude.</param>
    /// <param name="overflow">The overflow.</param>
    /// <param name="result">The date; the default unless the status is <see cref="TemporalStatus.Completed"/>.</param>
    /// <returns><see cref="TemporalStatus.Completed"/>, or <see cref="TemporalStatus.InvalidDate"/> under <see cref="TemporalOverflow.Reject"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The month or the day is NaN, or the overflow is not one of the two.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal RegulateISODate via Broiler.VM JsTemporal.RegulateDate; IP=Low; Security=Medium; Resources=0; Fingerprint=6743F7
    // Broiler-Human:        PENDING
    public static TemporalStatus RegulateIsoDate(long year, double month, double day, TemporalOverflow overflow, out IsoDate result)
    {
        if (double.IsNaN(month) || double.IsNaN(day))
            throw new ArgumentOutOfRangeException(nameof(month), "A date field is NaN.");
        CheckOverflow(overflow);

        result = default;
        if (overflow == TemporalOverflow.Constrain)
        {
            var clampedMonth = (int)Math.Clamp(month, 1, 12);
            var clampedDay = (int)Math.Clamp(day, 1, IsoCalendarMath.DaysInMonth(year, clampedMonth));
            result = new IsoDate(year, clampedMonth, clampedDay);
            return TemporalStatus.Completed;
        }

        if (month is < 1 or > 12 || day < 1 || day > IsoCalendarMath.DaysInMonth(year, (int)month))
            return TemporalStatus.InvalidDate;

        result = new IsoDate(year, (int)month, (int)day);
        return TemporalStatus.Completed;
    }

    /// <summary>
    /// Six fields made a time of day under an overflow: the proposal's <c>RegulateTime</c>. Under
    /// <see cref="TemporalOverflow.Constrain"/> each field is clamped to its range; under
    /// <see cref="TemporalOverflow.Reject"/> fields that are not a time are refused.
    /// </summary>
    /// <param name="hour">The hour, an integer of any magnitude.</param>
    /// <param name="minute">The minute, an integer of any magnitude.</param>
    /// <param name="second">The second, an integer of any magnitude.</param>
    /// <param name="millisecond">The millisecond, an integer of any magnitude.</param>
    /// <param name="microsecond">The microsecond, an integer of any magnitude.</param>
    /// <param name="nanosecond">The nanosecond, an integer of any magnitude.</param>
    /// <param name="overflow">The overflow.</param>
    /// <param name="result">The time; the default unless the status is <see cref="TemporalStatus.Completed"/>.</param>
    /// <returns><see cref="TemporalStatus.Completed"/>, or <see cref="TemporalStatus.InvalidTime"/> under <see cref="TemporalOverflow.Reject"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A field is NaN, or the overflow is not one of the two.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal RegulateTime via Broiler.VM JsTemporal.RegulateTime; IP=Low; Security=Medium; Resources=0; Fingerprint=FDDCBE
    // Broiler-Human:        PENDING
    public static TemporalStatus RegulateTime(
        double hour, double minute, double second, double millisecond, double microsecond, double nanosecond, TemporalOverflow overflow, out IsoTime result)
    {
        if (double.IsNaN(hour) || double.IsNaN(minute) || double.IsNaN(second) ||
            double.IsNaN(millisecond) || double.IsNaN(microsecond) || double.IsNaN(nanosecond))
            throw new ArgumentOutOfRangeException(nameof(hour), "A time field is NaN.");
        CheckOverflow(overflow);

        result = default;
        if (overflow == TemporalOverflow.Constrain)
        {
            result = new IsoTime(
                (int)Math.Clamp(hour, 0, 23),
                (int)Math.Clamp(minute, 0, 59),
                (int)Math.Clamp(second, 0, 59),
                (int)Math.Clamp(millisecond, 0, 999),
                (int)Math.Clamp(microsecond, 0, 999),
                (int)Math.Clamp(nanosecond, 0, 999));
            return TemporalStatus.Completed;
        }

        if (hour is < 0 or > 23 || minute is < 0 or > 59 || second is < 0 or > 59 ||
            millisecond is < 0 or > 999 || microsecond is < 0 or > 999 || nanosecond is < 0 or > 999)
            return TemporalStatus.InvalidTime;

        result = new IsoTime((int)hour, (int)minute, (int)second, (int)millisecond, (int)microsecond, (int)nanosecond);
        return TemporalStatus.Completed;
    }

    /// <summary>
    /// A date duration added to a date in the ISO 8601 calendar: the proposal's <c>CalendarDateAdd</c> for
    /// <c>"iso8601"</c> - the years and months balanced with <c>BalanceISOYearMonth</c>, the day regulated under the
    /// overflow, then the weeks and days added and the result checked against <c>ISODateWithinLimits</c>.
    /// </summary>
    /// <param name="date">The date, valid, of any year.</param>
    /// <param name="duration">The date duration, each field a finite integer.</param>
    /// <param name="overflow">The overflow the day is regulated under.</param>
    /// <param name="result">The date; the default unless the status is <see cref="TemporalStatus.Completed"/>.</param>
    /// <returns>
    /// <see cref="TemporalStatus.Completed"/>; <see cref="TemporalStatus.InvalidDate"/> where the day does not exist in
    /// the month reached under <see cref="TemporalOverflow.Reject"/>; <see cref="TemporalStatus.OutOfRange"/> where the
    /// result is outside the limits.
    /// </returns>
    /// <remarks>
    /// An operand past the point where any result could be within the limits - years beyond 10^9, months or weeks
    /// beyond 10^10, days beyond 10^11, as Broiler.VM refuses them, or a date more than 4 × 10^9 years from the epoch
    /// - is refused first, with <see cref="TemporalStatus.OutOfRange"/>. The limit check that follows would refuse
    /// each of them too, so the answer is the specification's; the early refusal keeps the civil-day arithmetic
    /// within its preconditions.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">A field of the duration is not finite, the date is not valid, or the overflow is not one of the two.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal CalendarDateAdd ISO 8601 branch via Broiler.VM JsTemporal.DateAdd; IP=Low; Security=Medium; Resources=1; Fingerprint=F71EC3
    // Broiler-Human:        PENDING
    public static TemporalStatus AddIsoDate(IsoDate date, DateDuration duration, TemporalOverflow overflow, out IsoDate result)
    {
        if (!double.IsFinite(duration.Years) || !double.IsFinite(duration.Months) || !double.IsFinite(duration.Weeks) || !double.IsFinite(duration.Days))
            throw new ArgumentOutOfRangeException(nameof(duration), "A field of the date duration is not finite.");
        if (!IsoCalendarMath.IsValidIsoDate(date.Year, date.Month, date.Day))
            throw new ArgumentOutOfRangeException(nameof(date), "The date is not a valid ISO date.");
        CheckOverflow(overflow);

        // The early refusals: past each, no result is within the limits (a year moves at most 10^9 + 10^10 / 12 +
        // 8 × 10^11 / 365 < 2.1 × 10^9 years), and the arithmetic below stays within its 64-bit preconditions.
        result = default;
        if (Math.Abs(duration.Years) > 1e9 || Math.Abs(duration.Months) > 1e10 || Math.Abs(duration.Weeks) > 1e10 ||
            Math.Abs(duration.Days) > 1e11 || date.Year is < -4_000_000_000L or > 4_000_000_000L)
            return TemporalStatus.OutOfRange;

        var (year, month) = IsoCalendarMath.BalanceYearMonth(date.Year + (long)duration.Years, date.Month + (long)duration.Months);
        var status = RegulateIsoDate(year, month, date.Day, overflow, out var intermediate);
        if (status != TemporalStatus.Completed)
            return status;

        var added = AddDaysToIsoDate(intermediate, (long)duration.Days + (7 * (long)duration.Weeks));
        if (TemporalLimits.CheckIsoDate(added) != TemporalStatus.Completed)
            return TemporalStatus.OutOfRange;

        result = added;
        return TemporalStatus.Completed;
    }

    /// <summary>
    /// A date-time read as UTC, in nanoseconds since the epoch: the proposal's <c>GetUTCEpochNanoseconds</c>, exact.
    /// </summary>
    /// <param name="dateTime">The date-time; its month from 1 to 12 and its year at most 2^53 in magnitude.</param>
    /// <returns>The epoch nanoseconds, not necessarily an instant.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The year's magnitude is above 2^53, or the month is outside 1 to 12.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal GetUTCEpochNanoseconds via Broiler.VM JsTemporalCore.UtcEpochNs; IP=Low; Security=Low; Resources=1; Fingerprint=C24830
    // Broiler-Human:        PENDING
    public static BigInteger GetUtcEpochNanoseconds(IsoDateTime dateTime) =>
        ((BigInteger)IsoCalendarMath.EpochDays(dateTime.Date.Year, dateTime.Date.Month, dateTime.Date.Day) * Day) + TimeToNanoseconds(dateTime.Time);

    /// <summary>The UTC date-time of an epoch-nanoseconds value: the proposal's <c>GetISOPartsFromEpoch</c>, for any integer.</summary>
    /// <param name="epochNanoseconds">The nanoseconds since the epoch; the wall clock of a zone is this plus its offset.</param>
    /// <returns>The date-time.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The value is further than 2^60 days from the epoch.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal GetISOPartsFromEpoch via Broiler.VM JsTemporalCore.FromEpochNs; IP=Low; Security=Low; Resources=1; Fingerprint=DAB427
    // Broiler-Human:        PENDING
    public static IsoDateTime GetIsoPartsFromEpoch(BigInteger epochNanoseconds)
    {
        var time = BalanceTime(epochNanoseconds, out var days);
        CheckDayCount(days, nameof(epochNanoseconds));
        return new IsoDateTime(FromEpochDays(days), time);
    }

    /// <summary>
    /// A date and a count of nanoseconds from its midnight, balanced into a date-time: the proposal's
    /// <c>BalanceISODateTime</c>, its time fields given as one exact time duration.
    /// </summary>
    /// <param name="date">The date, valid.</param>
    /// <param name="timeNanoseconds">The nanoseconds from the date's midnight, of either sign.</param>
    /// <returns>The date-time, not necessarily within Temporal's limits.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The days carried or the result are past the civil-day arithmetic's bounds.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal BalanceISODateTime via Broiler.VM JsTemporalCore.BalanceDateTime; IP=Low; Security=Medium; Resources=1; Fingerprint=0E97A1
    // Broiler-Human:        PENDING
    public static IsoDateTime BalanceIsoDateTime(IsoDate date, BigInteger timeNanoseconds)
    {
        var time = BalanceTime(timeNanoseconds, out var days);
        return new IsoDateTime(AddDaysToIsoDate(date, days), time);
    }

    /// <summary>
    /// A time of day rounded to an increment of a unit: the proposal's <c>RoundTime</c>, the quantity below the unit's
    /// parent rounded exactly, the overflow days answered beside the time (one for a time rounded up to midnight).
    /// </summary>
    /// <param name="time">The time, valid.</param>
    /// <param name="increment">The rounding increment, positive; the caller has validated it for the unit.</param>
    /// <param name="unit">A time unit or <see cref="TemporalUnit.Day"/>.</param>
    /// <param name="mode">The rounding mode.</param>
    /// <param name="days">The days carried: 0 or 1.</param>
    /// <returns>The rounded time.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The increment is not positive, or the unit is a calendar unit, unset or auto.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal RoundTime via Broiler.VM JsTemporalCore.RoundTime; IP=Low; Security=Medium; Resources=1; Fingerprint=315CCF
    // Broiler-Human:        PENDING
    public static IsoTime RoundTime(IsoTime time, long increment, TemporalUnit unit, RoundingMode mode, out long days)
    {
        CheckRounding(increment, unit);
        var length = DurationCore.UnitLength(unit);
        long quantity = unit switch
        {
            TemporalUnit.Day or TemporalUnit.Hour => TimeToNanoseconds(time),
            TemporalUnit.Minute => TimeToNanoseconds(time with { Hour = 0 }),
            TemporalUnit.Second => TimeToNanoseconds(time with { Hour = 0, Minute = 0 }),
            TemporalUnit.Millisecond => TimeToNanoseconds(time with { Hour = 0, Minute = 0, Second = 0 }),
            TemporalUnit.Microsecond => ((long)time.Microsecond * 1000) + time.Nanosecond,
            _ => time.Nanosecond,
        };

        var result = ExactRounding.RoundNumberToIncrement(quantity, BigInteger.One, (BigInteger)increment * length, mode) / length;
        if (unit == TemporalUnit.Day)
        {
            days = (long)result;
            return default;
        }

        var nanoseconds = unit switch
        {
            TemporalUnit.Hour => result * length,
            TemporalUnit.Minute => ((BigInteger)time.Hour * 60 + result) * length,
            TemporalUnit.Second => TimeToNanoseconds(time with { Second = 0, Millisecond = 0, Microsecond = 0, Nanosecond = 0 }) + (result * length),
            TemporalUnit.Millisecond => TimeToNanoseconds(time with { Millisecond = 0, Microsecond = 0, Nanosecond = 0 }) + (result * length),
            TemporalUnit.Microsecond => TimeToNanoseconds(time with { Microsecond = 0, Nanosecond = 0 }) + (result * length),
            _ => TimeToNanoseconds(time with { Nanosecond = 0 }) + result,
        };
        return BalanceTime(nanoseconds, out days);
    }

    /// <summary>
    /// A date-time rounded to an increment of a unit: the proposal's <c>RoundISODateTime</c>, its time rounded and the
    /// days carried into its date.
    /// </summary>
    /// <param name="dateTime">The date-time, valid and within the limits.</param>
    /// <param name="increment">The rounding increment, positive; the caller has validated it for the unit.</param>
    /// <param name="unit">A time unit or <see cref="TemporalUnit.Day"/>.</param>
    /// <param name="mode">The rounding mode.</param>
    /// <returns>The rounded date-time, which may be a day past the limits: the caller checks them with <see cref="TemporalLimits.CheckIsoDateTime"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The increment is not positive, or the unit is a calendar unit, unset or auto.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal RoundISODateTime via Broiler.VM JsTemporalCore.RoundDateTime; IP=Low; Security=Medium; Resources=1; Fingerprint=1149B8
    // Broiler-Human:        PENDING
    public static IsoDateTime RoundIsoDateTime(IsoDateTime dateTime, long increment, TemporalUnit unit, RoundingMode mode)
    {
        var rounded = RoundTime(dateTime.Time, increment, unit, mode, out var days);
        return new IsoDateTime(AddDaysToIsoDate(dateTime.Date, days), rounded);
    }

    /// <summary>
    /// An instant rounded to an increment of a unit: the proposal's <c>RoundTemporalInstant</c>, every value rounded as a
    /// positive one is (<c>RoundNumberToIncrementAsIfPositive</c>), so that <c>floor</c> moves toward the past at either
    /// sign.
    /// </summary>
    /// <param name="epochNanoseconds">The nanoseconds since the epoch.</param>
    /// <param name="increment">The rounding increment, positive; the caller has validated it for the unit.</param>
    /// <param name="unit">A time unit or <see cref="TemporalUnit.Day"/>, a day of 24 hours.</param>
    /// <param name="mode">The rounding mode.</param>
    /// <returns>The rounded epoch nanoseconds.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The increment is not positive, or the unit is a calendar unit, unset or auto.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal RoundTemporalInstant via Broiler.VM JsTemporal.RoundInstant; IP=Low; Security=Low; Resources=1; Fingerprint=32F615
    // Broiler-Human:        PENDING
    public static BigInteger RoundTemporalInstant(BigInteger epochNanoseconds, long increment, TemporalUnit unit, RoundingMode mode)
    {
        CheckRounding(increment, unit);
        return ExactRounding.RoundNumberToIncrementAsIfPositive(epochNanoseconds, BigInteger.One, (BigInteger)DurationCore.UnitLength(unit) * increment, mode);
    }

    /// <summary>A time duration added to an instant: the proposal's <c>AddInstant</c>, refused past the instant range.</summary>
    /// <param name="epochNanoseconds">The instant, in nanoseconds since the epoch.</param>
    /// <param name="timeDuration">The time duration, in nanoseconds.</param>
    /// <param name="result">The instant reached; zero unless the status is <see cref="TemporalStatus.Completed"/>.</param>
    /// <returns><see cref="TemporalStatus.Completed"/>, or <see cref="TemporalStatus.OutOfRange"/> where the sum is not an instant.</returns>
    // Broiler-AI:           Origin=Ported; Spec=Temporal AddInstant via Broiler.VM JsTemporal.AddInstant; IP=Low; Security=Low; Resources=1; Fingerprint=2387C8
    // Broiler-Human:        PENDING
    public static TemporalStatus AddInstant(BigInteger epochNanoseconds, BigInteger timeDuration, out BigInteger result)
    {
        var sum = epochNanoseconds + timeDuration;
        if (TemporalLimits.CheckEpochNanoseconds(sum) != TemporalStatus.Completed)
        {
            result = BigInteger.Zero;
            return TemporalStatus.OutOfRange;
        }

        result = sum;
        return TemporalStatus.Completed;
    }

    // EpochDaysToISODate, as the record.
    // Broiler-AI:           Origin=Ported; Spec=Temporal EpochDaysToISODate via Broiler.VM JsIsoDate; IP=Low; Security=Low; Resources=0; Fingerprint=598B10
    // Broiler-Human:        PENDING
    private static IsoDate FromEpochDays(long days)
    {
        var (year, month, day) = IsoCalendarMath.FromEpochDays(days);
        return new IsoDate(year, month, day);
    }

    // A day count within the civil-day arithmetic's bound.
    // Broiler-AI:           Origin=AI; Spec=Temporal ISO 8601 calendar; IP=Low; Security=Low; Resources=0; Fingerprint=A1A28C
    // Broiler-Human:        PENDING
    private static void CheckDayCount(long days, string name)
    {
        if (days is < -DayCountLimit or > DayCountLimit)
            throw new ArgumentOutOfRangeException(name, days, "A day count is at most 2^60 in magnitude.");
    }

    // An overflow that is one of the two.
    // Broiler-AI:           Origin=AI; Spec=Temporal GetTemporalOverflowOption; IP=Low; Security=Low; Resources=0; Fingerprint=CCD45C
    // Broiler-Human:        PENDING
    private static void CheckOverflow(TemporalOverflow overflow)
    {
        if (overflow is not (TemporalOverflow.Constrain or TemporalOverflow.Reject))
            throw new ArgumentOutOfRangeException(nameof(overflow), overflow, "The overflow is not one of the two.");
    }

    // A positive increment of a unit with a length: a time unit or day.
    // Broiler-AI:           Origin=AI; Spec=Temporal RoundTime; IP=Low; Security=Low; Resources=0; Fingerprint=AA96D6
    // Broiler-Human:        PENDING
    private static void CheckRounding(long increment, TemporalUnit unit)
    {
        if (increment < 1)
            throw new ArgumentOutOfRangeException(nameof(increment), increment, "The increment is not positive.");
        if (unit is < TemporalUnit.Day or > TemporalUnit.Nanosecond)
            throw new ArgumentOutOfRangeException(nameof(unit), unit, "The unit is not a time unit or day.");
    }
}
