// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   36
// Annotated:        36/36
// Exempt:           0
// Human-reviewed:   0/36
// IP risk:          Low
// Security risk:    Medium
// Criteria:         0/0
// Resource impact:  2/10 max
// Unverified:       36
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Numerics;
using Broiler.ECMAScript.Numbers;
using Broiler.ECMAScript.Time;

namespace Broiler.ECMAScript.Temporal;

/// <summary>
/// The Temporal proposal's duration arithmetic (JSD-0078's durations sub-area): validity, sign, the default largest
/// unit and the internal records; addition, <c>compare</c>, rounding and totals, with and without <c>relativeTo</c>;
/// and the relative machinery - <c>RoundRelativeDuration</c> with its nudges and bubbling, <c>TotalRelativeDuration</c>
/// - over the calendar and time-zone providers, with exact rationals. Ported from Broiler.VM's
/// <c>JsTemporal.Durations</c>, the duration members of <c>JsTemporalCore</c> and the arithmetic of its
/// <c>Temporal.Duration</c> methods after their option reads, with their behaviour unchanged.
/// </summary>
/// <remarks>
/// <para>
/// <b>What no member does</b>: no option read (each method's options are the engine's, validated by
/// <see cref="TemporalOptionRules"/>), no <c>relativeTo</c> resolution (the engine hands a <see cref="RelativeTo"/>),
/// no <c>ToTemporalDuration</c> of an argument, no <c>RangeError</c>.
/// </para>
/// <para>
/// <b>Failure is data</b> (JSD-0078's X3): an invalid duration, a time duration past
/// <see cref="TemporalLimits.MaxTimeDuration"/>, a calendar unit without <c>relativeTo</c>, a result past a limit, and a
/// provider's failure each answer a <see cref="TemporalStatus"/>, in the order the specification meets them. A
/// calendar provider's <see cref="CalendarStatus.FieldOutOfRange"/> or <see cref="CalendarStatus.MonthNotInYear"/>
/// (an addition under <c>reject</c>) is <see cref="TemporalStatus.InvalidDate"/>, its other failures and every time-zone
/// failure <see cref="TemporalStatus.OutOfRange"/>: each a <c>RangeError</c> in both engines.
/// </para>
/// <para>
/// <b>Exactness</b>: time durations are exact nanoseconds, totals exact ratios rounded once to a Number with SJL-4's
/// <see cref="ExactRounding.ToDouble(BigInteger, BigInteger)"/>, and every field a Number made once.
/// </para>
/// <para>
/// <b>Charges</b>: none. Every member's work is bounded by the limits: a relative rounding adds a duration to a date
/// at most a few times, and a bubble walks at most the four date units (X3: there is no class C2).
/// </para>
/// </remarks>
// Broiler-AI:           Origin=Ported; Spec=Temporal s7.5 via Broiler.VM JsTemporal.Durations; IP=Low; Security=Medium; Resources=2; Fingerprint=B1BDBE
// Broiler-Human:        PENDING
public static partial class DurationMath
{
    // A day in nanoseconds, as an exact integer.
    // Broiler-AI:           Origin=Ported; Spec=Temporal s7.5 via Broiler.VM JsTemporalCore; IP=None; Security=None; Resources=0; Fingerprint=055C53
    // Broiler-Human:        PENDING
    private static readonly BigInteger Day = new(TemporalLimits.NanosecondsPerDay);

    // A date-duration field past this cannot be handed to a calendar exactly; no such field is a valid duration's,
    // and no result that far from a date within the limits is within them.
    // Broiler-AI:           Origin=AI; Spec=Temporal s7.5.2, JSD-0078 decision D12; IP=None; Security=Low; Resources=0; Fingerprint=AF151B
    // Broiler-Human:        PENDING
    private const double MaxCalendarField = 4.0e18;

    // ---- The basic operations ----------------------------------------------------------------------------------

    /// <summary>The sign of a duration: the proposal's <c>DurationSign</c> (s7.5.13), the sign of its first field that is not zero.</summary>
    /// <param name="duration">The duration.</param>
    /// <returns>-1, 0 or 1.</returns>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s7.5.13 to s7.5.15 via Broiler.VM JsTemporalCore.Sign; IP=Low; Security=None; Resources=0; Fingerprint=E51F1F
    // Broiler-Human:        PENDING
    public static int Sign(DurationFields duration) => DurationCore.Sign(duration);

    /// <summary>The sign of a date duration: the proposal's <c>DateDurationSign</c> (s7.5.14).</summary>
    /// <param name="duration">The date duration.</param>
    /// <returns>-1, 0 or 1.</returns>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s7.5.13 to s7.5.15 via Broiler.VM JsTemporalCore.Sign; IP=Low; Security=None; Resources=0; Fingerprint=38FC09
    // Broiler-Human:        PENDING
    public static int Sign(DateDuration duration) =>
        duration.Years != 0 ? Math.Sign(duration.Years)
        : duration.Months != 0 ? Math.Sign(duration.Months)
        : duration.Weeks != 0 ? Math.Sign(duration.Weeks)
        : duration.Days != 0 ? Math.Sign(duration.Days)
        : 0;

    /// <summary>The sign of an internal duration: the proposal's <c>InternalDurationSign</c> (s7.5.15), the date part's, else the time's.</summary>
    /// <param name="duration">The internal duration.</param>
    /// <returns>-1, 0 or 1.</returns>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s7.5.13 to s7.5.15 via Broiler.VM JsTemporalCore.Sign; IP=Low; Security=None; Resources=0; Fingerprint=A5F0B8
    // Broiler-Human:        PENDING
    public static int Sign(InternalDuration duration)
    {
        var date = Sign(duration.Date);
        return date != 0 ? date : duration.TimeNanoseconds.Sign;
    }

    /// <summary>The largest unit whose field is not zero: the proposal's <c>DefaultTemporalLargestUnit</c> (s7.5.17), nanosecond for a zero duration.</summary>
    /// <param name="duration">The duration.</param>
    /// <returns>The unit, year to nanosecond.</returns>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s7.5.17 via Broiler.VM JsTemporalCore.DefaultLargestUnit; IP=Low; Security=None; Resources=0; Fingerprint=DFC930
    // Broiler-Human:        PENDING
    public static TemporalUnit DefaultLargestUnit(DurationFields duration) => DurationCore.DefaultLargestUnit(duration);

    /// <summary>
    /// A duration as an internal record: the proposal's <c>ToInternalDurationRecord</c> (s7.5.5), the date fields kept
    /// and the six time fields made one exact time duration.
    /// </summary>
    /// <param name="duration">The duration, valid.</param>
    /// <returns>The internal record.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A field is not finite.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s7.5.5 via Broiler.VM JsTemporalCore.ToInternal; IP=Low; Security=Low; Resources=1; Fingerprint=BDC5AD
    // Broiler-Human:        PENDING
    public static InternalDuration ToInternal(DurationFields duration)
    {
        CheckFinite(duration);
        return new InternalDuration(
            new DateDuration(duration.Years, duration.Months, duration.Weeks, duration.Days),
            DurationCore.TimeFromComponents(duration));
    }

    /// <summary>
    /// A duration as an internal record with its days made 24 hours each: the proposal's
    /// <c>ToInternalDurationRecordWith24HourDays</c> (s7.5.6). A valid duration's days and time together are within
    /// <see cref="TemporalLimits.MaxTimeDuration"/>.
    /// </summary>
    /// <param name="duration">The duration, valid.</param>
    /// <returns>The internal record, its date part's days zero.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A field is not finite.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s7.5.6 via Broiler.VM JsTemporalCore.ToInternalWith24HourDays; IP=Low; Security=Low; Resources=1; Fingerprint=94CFC1
    // Broiler-Human:        PENDING
    public static InternalDuration ToInternalWith24HourDays(DurationFields duration)
    {
        CheckFinite(duration);
        return new InternalDuration(
            new DateDuration(duration.Years, duration.Months, duration.Weeks, 0),
            DurationCore.TimeFromComponents(duration) + (new BigInteger(duration.Days) * Day));
    }

    /// <summary>
    /// An internal duration as a duration's ten fields: the proposal's <c>TemporalDurationFromInternal</c> (s7.5.8), the
    /// time balanced up to the largest unit, each field made a Number once, and then <c>CreateTemporalDuration</c>'s
    /// validity test.
    /// </summary>
    /// <param name="duration">The internal duration; its time and date parts have one sign.</param>
    /// <param name="largestUnit">The largest unit the time is balanced to: year to nanosecond (a date unit balances to days).</param>
    /// <param name="result">The fields, every zero a positive zero; the default unless the status is <see cref="TemporalStatus.Completed"/>.</param>
    /// <returns><see cref="TemporalStatus.Completed"/>, or <see cref="TemporalStatus.InvalidDuration"/> where the fields are not a valid duration.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The unit is unset or auto, or a date field is not finite.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s7.5.8 via Broiler.VM JsTemporal.FromInternal; IP=Low; Security=Low; Resources=1; Fingerprint=1CCB09
    // Broiler-Human:        PENDING
    public static TemporalStatus FromInternal(InternalDuration duration, TemporalUnit largestUnit, out DurationFields result)
    {
        CheckUnit(largestUnit, nameof(largestUnit));
        CheckFinite(duration.Date, nameof(duration));
        return Create(DurationCore.FromInternal(duration.Date, duration.TimeNanoseconds, largestUnit), out result);
    }

    /// <summary>A duration with every field negated: the proposal's <c>CreateNegatedTemporalDuration</c> (s7.5.20), every zero a positive zero.</summary>
    /// <param name="duration">The duration.</param>
    /// <returns>The negated duration.</returns>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s7.5.20 via Broiler.VM JsTemporal.Negated; IP=Low; Security=None; Resources=0; Fingerprint=D2B357
    // Broiler-Human:        PENDING
    public static DurationFields Negate(DurationFields duration) => new(
        Negated(duration.Years), Negated(duration.Months), Negated(duration.Weeks), Negated(duration.Days), Negated(duration.Hours),
        Negated(duration.Minutes), Negated(duration.Seconds), Negated(duration.Milliseconds), Negated(duration.Microseconds),
        Negated(duration.Nanoseconds));

    /// <summary>A duration with every field's magnitude: the fields <c>Temporal.Duration.prototype.abs</c> creates.</summary>
    /// <param name="duration">The duration.</param>
    /// <returns>The duration's absolute value.</returns>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s7.3.17 via Broiler.VM JsRealm.TemporalDuration abs; IP=Low; Security=None; Resources=0; Fingerprint=C0B6A1
    // Broiler-Human:        PENDING
    public static DurationFields Abs(DurationFields duration) => new(
        Math.Abs(duration.Years), Math.Abs(duration.Months), Math.Abs(duration.Weeks), Math.Abs(duration.Days), Math.Abs(duration.Hours),
        Math.Abs(duration.Minutes), Math.Abs(duration.Seconds), Math.Abs(duration.Milliseconds), Math.Abs(duration.Microseconds),
        Math.Abs(duration.Nanoseconds));

    /// <summary>
    /// A date duration, validated: the proposal's <c>CreateDateDurationRecord</c> (s7.5.9), each field made a Number and
    /// its zeros positive.
    /// </summary>
    /// <param name="years">The years, an integer.</param>
    /// <param name="months">The months, an integer.</param>
    /// <param name="weeks">The weeks, an integer.</param>
    /// <param name="days">The days, an integer.</param>
    /// <param name="result">The record; the default unless the status is <see cref="TemporalStatus.Completed"/>.</param>
    /// <returns><see cref="TemporalStatus.Completed"/>, or <see cref="TemporalStatus.InvalidDuration"/>.</returns>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s7.5.9 via Broiler.VM JsTemporal.DateDurationRecord; IP=Low; Security=Low; Resources=0; Fingerprint=F9B203
    // Broiler-Human:        PENDING
    public static TemporalStatus CreateDateDurationRecord(double years, double months, double weeks, double days, out DateDuration result)
    {
        result = default;
        if (TemporalLimits.CheckDuration(new DurationFields(years, months, weeks, days, 0, 0, 0, 0, 0, 0)) != TemporalStatus.Completed)
            return TemporalStatus.InvalidDuration;

        result = new DateDuration(years + 0.0, months + 0.0, weeks + 0.0, days + 0.0);
        return TemporalStatus.Completed;
    }

    /// <summary>Two time durations added: the proposal's <c>AddTimeDuration</c> (s7.5.22).</summary>
    /// <param name="one">The first time duration, in nanoseconds.</param>
    /// <param name="two">The second time duration, in nanoseconds.</param>
    /// <param name="result">The sum; zero unless the status is <see cref="TemporalStatus.Completed"/>.</param>
    /// <returns><see cref="TemporalStatus.Completed"/>, or <see cref="TemporalStatus.InvalidDuration"/> past <see cref="TemporalLimits.MaxTimeDuration"/>.</returns>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s7.5.22 via Broiler.VM JsTemporal.AddTimeDuration; IP=Low; Security=Low; Resources=1; Fingerprint=E8B15A
    // Broiler-Human:        PENDING
    public static TemporalStatus AddTimeDuration(BigInteger one, BigInteger two, out BigInteger result)
    {
        result = one + two;
        if (BigInteger.Abs(result) <= TemporalLimits.MaxTimeDuration)
            return TemporalStatus.Completed;

        result = BigInteger.Zero;
        return TemporalStatus.InvalidDuration;
    }

    /// <summary>Days of 24 hours added to a time duration: the proposal's <c>Add24HourDaysToTimeDuration</c> (s7.5.23).</summary>
    /// <param name="time">The time duration, in nanoseconds.</param>
    /// <param name="days">The days, an integer.</param>
    /// <param name="result">The sum; zero unless the status is <see cref="TemporalStatus.Completed"/>.</param>
    /// <returns><see cref="TemporalStatus.Completed"/>, or <see cref="TemporalStatus.InvalidDuration"/> past <see cref="TemporalLimits.MaxTimeDuration"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The days are not finite.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s7.5.23 via Broiler.VM JsTemporal.Add24HourDays; IP=Low; Security=Low; Resources=1; Fingerprint=B1C324
    // Broiler-Human:        PENDING
    public static TemporalStatus Add24HourDaysToTimeDuration(BigInteger time, double days, out BigInteger result)
    {
        if (!double.IsFinite(days))
            throw new ArgumentOutOfRangeException(nameof(days), days, "The days are not finite.");

        return AddTimeDuration(time, new BigInteger(days) * Day, out result);
    }

    /// <summary>
    /// A time duration rounded to an increment of nanoseconds: the proposal's <c>RoundTimeDurationToIncrement</c>
    /// (s7.5.27), exact, under the rounding mode by the value's sign.
    /// </summary>
    /// <param name="duration">The time duration, in nanoseconds.</param>
    /// <param name="increment">The increment in nanoseconds, positive.</param>
    /// <param name="mode">The rounding mode.</param>
    /// <param name="result">The rounded duration; zero unless the status is <see cref="TemporalStatus.Completed"/>.</param>
    /// <returns><see cref="TemporalStatus.Completed"/>, or <see cref="TemporalStatus.InvalidDuration"/> past <see cref="TemporalLimits.MaxTimeDuration"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The increment is not positive.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s7.5.28 via Broiler.VM JsTemporal.RoundTimeDurationToIncrement; IP=Low; Security=Low; Resources=1; Fingerprint=0C5CCE
    // Broiler-Human:        PENDING
    public static TemporalStatus RoundTimeDurationToIncrement(BigInteger duration, BigInteger increment, RoundingMode mode, out BigInteger result)
    {
        result = ExactRounding.RoundNumberToIncrement(duration, BigInteger.One, increment, mode);
        if (BigInteger.Abs(result) <= TemporalLimits.MaxTimeDuration)
            return TemporalStatus.Completed;

        result = BigInteger.Zero;
        return TemporalStatus.InvalidDuration;
    }

    /// <summary>A time duration rounded to an increment of a unit: the proposal's <c>RoundTimeDuration</c> (s7.5.30).</summary>
    /// <param name="duration">The time duration, in nanoseconds.</param>
    /// <param name="increment">The increment, positive.</param>
    /// <param name="unit">A time unit or <see cref="TemporalUnit.Day"/>, a day of 24 hours.</param>
    /// <param name="mode">The rounding mode.</param>
    /// <param name="result">The rounded duration; zero unless the status is <see cref="TemporalStatus.Completed"/>.</param>
    /// <returns><see cref="TemporalStatus.Completed"/>, or <see cref="TemporalStatus.InvalidDuration"/> past <see cref="TemporalLimits.MaxTimeDuration"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The increment is not positive, or the unit is a calendar unit, unset or auto.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s7.5.30 via Broiler.VM JsTemporal.RoundTimeDuration; IP=Low; Security=Low; Resources=1; Fingerprint=0992C3
    // Broiler-Human:        PENDING
    public static TemporalStatus RoundTimeDuration(BigInteger duration, long increment, TemporalUnit unit, RoundingMode mode, out BigInteger result)
    {
        CheckTimeUnit(unit, nameof(unit));
        if (increment < 1)
            throw new ArgumentOutOfRangeException(nameof(increment), increment, "The increment is not positive.");

        return RoundTimeDurationToIncrement(duration, (BigInteger)DurationCore.UnitLength(unit) * increment, mode, out result);
    }

    /// <summary>
    /// A time duration as a total of a unit: the proposal's <c>TotalTimeDuration</c> (s7.5.31), the exact ratio rounded
    /// once to a Number.
    /// </summary>
    /// <param name="duration">The time duration, in nanoseconds.</param>
    /// <param name="unit">A time unit or <see cref="TemporalUnit.Day"/>, a day of 24 hours.</param>
    /// <returns>The total.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The unit is a calendar unit, unset or auto.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s7.5.31 via Broiler.VM JsTemporal.TotalTimeDuration; IP=Low; Security=Low; Resources=1; Fingerprint=5E1E7B
    // Broiler-Human:        PENDING
    public static double TotalTimeDuration(BigInteger duration, TemporalUnit unit)
    {
        CheckTimeUnit(unit, nameof(unit));
        return ExactRounding.ToDouble(duration, DurationCore.UnitLength(unit));
    }

    // ---- The methods' arithmetic, after their reads -------------------------------------------------------------

    /// <summary>
    /// Two durations added: the proposal's <c>AddDurations</c> (s7.5.41) after its <c>ToTemporalDuration</c>, which the
    /// engine does, negating the second for <c>subtract</c> with <see cref="Negate"/>. Calendar units are refused, as
    /// the pinned revision refuses them whatever the second argument (row D3).
    /// </summary>
    /// <param name="one">The receiver's fields, a valid duration.</param>
    /// <param name="two">The other duration's fields, a valid duration.</param>
    /// <param name="result">The sum, balanced to the larger of the two default largest units; the default unless the status is <see cref="TemporalStatus.Completed"/>.</param>
    /// <returns>
    /// <see cref="TemporalStatus.Completed"/>; <see cref="TemporalStatus.RelativeToRequired"/> where either has years,
    /// months or weeks; <see cref="TemporalStatus.InvalidDuration"/> where the sum is past the limits.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">A field is not finite.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s7.5.41 via Broiler.VM JsRealm.TemporalDuration add; IP=Low; Security=Medium; Resources=1; Fingerprint=C93666
    // Broiler-Human:        PENDING
    public static TemporalStatus AddDurations(DurationFields one, DurationFields two, out DurationFields result)
    {
        result = default;
        var largest = DurationCore.Larger(DefaultLargestUnit(one), DefaultLargestUnit(two));
        if (IsCalendarUnit(largest))
            return TemporalStatus.RelativeToRequired;

        var status = AddTimeDuration(ToInternalWith24HourDays(one).TimeNanoseconds, ToInternalWith24HourDays(two).TimeNanoseconds, out var time);
        return status != TemporalStatus.Completed ? status : FromInternal(new InternalDuration(default, time), largest, out result);
    }

    /// <summary>
    /// Two durations compared: <c>Temporal.Duration.compare</c> (s7.2.3) after its reads - relative to a zoned instant
    /// by adding each to it, relative to a plain date by its days (<c>DateDurationDays</c>, which adds only the years,
    /// months and weeks, row D4), and otherwise in days of 24 hours.
    /// </summary>
    /// <param name="one">The first duration, valid.</param>
    /// <param name="two">The second duration, valid.</param>
    /// <param name="relativeTo">The resolved <c>relativeTo</c>, or <see cref="RelativeTo.None"/>.</param>
    /// <param name="result">-1, 0 or 1; zero unless the status is <see cref="TemporalStatus.Completed"/>.</param>
    /// <returns>
    /// <see cref="TemporalStatus.Completed"/>; <see cref="TemporalStatus.RelativeToRequired"/> where either has calendar
    /// units and <paramref name="relativeTo"/> is not a plain date or a zoned instant; <see cref="TemporalStatus.OutOfRange"/>
    /// or <see cref="TemporalStatus.InvalidDuration"/> where an addition passes a limit.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">A field is not finite.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s7.2.3 via Broiler.VM JsRealm.TemporalDuration DurationCompare; IP=Low; Security=Medium; Resources=2; Fingerprint=998079
    // Broiler-Human:        PENDING
    public static TemporalStatus Compare(DurationFields one, DurationFields two, RelativeTo relativeTo, out int result)
    {
        result = 0;
        if (SameFields(one, two))
            return TemporalStatus.Completed;

        var largest1 = DefaultLargestUnit(one);
        var largest2 = DefaultLargestUnit(two);
        var duration1 = ToInternal(one);
        var duration2 = ToInternal(two);

        if (relativeTo.IsZoned && (IsDateUnit(largest1) || IsDateUnit(largest2)))
        {
            var status = AddZonedDateTime(relativeTo.EpochNanoseconds, relativeTo.TimeZone!, relativeTo.Calendars!, relativeTo.Calendar, duration1, TemporalOverflow.Constrain, out var after1);
            if (status != TemporalStatus.Completed)
                return status;

            status = AddZonedDateTime(relativeTo.EpochNanoseconds, relativeTo.TimeZone!, relativeTo.Calendars!, relativeTo.Calendar, duration2, TemporalOverflow.Constrain, out var after2);
            if (status != TemporalStatus.Completed)
                return status;

            result = after1.CompareTo(after2) switch { > 0 => 1, < 0 => -1, _ => 0 };
            return TemporalStatus.Completed;
        }

        double days1, days2;
        if (IsCalendarUnit(largest1) || IsCalendarUnit(largest2))
        {
            if (!relativeTo.IsPlain)
                return TemporalStatus.RelativeToRequired;

            var status = DateDurationDays(relativeTo.Calendars!, relativeTo.Calendar, duration1.Date, relativeTo.PlainDate, out days1);
            if (status != TemporalStatus.Completed)
                return status;

            status = DateDurationDays(relativeTo.Calendars!, relativeTo.Calendar, duration2.Date, relativeTo.PlainDate, out days2);
            if (status != TemporalStatus.Completed)
                return status;
        }
        else
        {
            days1 = one.Days;
            days2 = two.Days;
        }

        var timeStatus = Add24HourDaysToTimeDuration(duration1.TimeNanoseconds, days1, out var time1);
        if (timeStatus != TemporalStatus.Completed)
            return timeStatus;

        timeStatus = Add24HourDaysToTimeDuration(duration2.TimeNanoseconds, days2, out var time2);
        if (timeStatus != TemporalStatus.Completed)
            return timeStatus;

        result = time1.CompareTo(time2) switch { > 0 => 1, < 0 => -1, _ => 0 };
        return TemporalStatus.Completed;
    }

    /// <summary>
    /// A duration rounded: <c>Temporal.Duration.prototype.round</c> (s7.3.20) after its reads and its units' rules
    /// (<see cref="TemporalOptionRules.TryGetDurationRoundUnits"/>) - relative to a zoned instant through
    /// <c>DifferenceZonedDateTimeWithRounding</c>, relative to a plain date through
    /// <c>DifferencePlainDateTimeWithRounding</c>, and otherwise in days of 24 hours.
    /// </summary>
    /// <param name="duration">The receiver's fields, a valid duration.</param>
    /// <param name="largestUnit">The resolved largest unit, year to nanosecond.</param>
    /// <param name="roundingIncrement">The increment, valid for the smallest unit.</param>
    /// <param name="smallestUnit">The resolved smallest unit, year to nanosecond, no larger than the largest.</param>
    /// <param name="roundingMode">The rounding mode.</param>
    /// <param name="relativeTo">The resolved <c>relativeTo</c>, or <see cref="RelativeTo.None"/>.</param>
    /// <param name="result">The rounded duration; the default unless the status is <see cref="TemporalStatus.Completed"/>.</param>
    /// <returns>
    /// <see cref="TemporalStatus.Completed"/>; <see cref="TemporalStatus.RelativeToRequired"/> where the duration or the
    /// largest unit is a calendar unit with no <c>relativeTo</c>; <see cref="TemporalStatus.OutOfRange"/> or
    /// <see cref="TemporalStatus.InvalidDuration"/> where an addition or the result passes a limit.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">A unit is unset or auto, the smallest is larger than the largest, the increment is not positive, or a field is not finite.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s7.3.20 via Broiler.VM JsRealm.TemporalDuration DurationRound; IP=Low; Security=Medium; Resources=2; Fingerprint=B02B42
    // Broiler-Human:        PENDING
    public static TemporalStatus Round(
        DurationFields duration, TemporalUnit largestUnit, long roundingIncrement, TemporalUnit smallestUnit, RoundingMode roundingMode,
        RelativeTo relativeTo, out DurationFields result)
    {
        CheckUnits(largestUnit, smallestUnit, roundingIncrement);
        result = default;
        TemporalStatus status;

        if (relativeTo.IsZoned)
        {
            var internalDuration = ToInternal(duration);
            var origin = relativeTo.EpochNanoseconds;
            status = AddZonedDateTime(origin, relativeTo.TimeZone!, relativeTo.Calendars!, relativeTo.Calendar, internalDuration, TemporalOverflow.Constrain, out var target);
            if (status != TemporalStatus.Completed)
                return status;

            status = TemporalDifferences.DifferenceZonedDateTimeWithRounding(
                origin, target, relativeTo.TimeZone!, relativeTo.Calendars!, relativeTo.Calendar, largestUnit, roundingIncrement, smallestUnit, roundingMode,
                out internalDuration);
            if (status != TemporalStatus.Completed)
                return status;

            return FromInternal(internalDuration, IsDateUnit(largestUnit) ? TemporalUnit.Hour : largestUnit, out result);
        }

        if (relativeTo.IsPlain)
        {
            status = PlainTarget(duration, relativeTo, out var start, out var target);
            if (status != TemporalStatus.Completed)
                return status;

            status = TemporalDifferences.DifferencePlainDateTimeWithRounding(
                relativeTo.Calendars!, relativeTo.Calendar, start, target, largestUnit, roundingIncrement, smallestUnit, roundingMode, out var rounded);
            return status != TemporalStatus.Completed ? status : FromInternal(rounded, largestUnit, out result);
        }

        if (IsCalendarUnit(DefaultLargestUnit(duration)) || IsCalendarUnit(largestUnit))
            return TemporalStatus.RelativeToRequired;

        var balanced = ToInternalWith24HourDays(duration);
        InternalDuration roundedDuration;
        if (smallestUnit == TemporalUnit.Day)
        {
            var days = ExactRounding.RoundNumberToIncrement(balanced.TimeNanoseconds, Day, roundingIncrement, roundingMode);
            status = CreateDateDurationRecord(0, 0, 0, ExactRounding.ToDouble(days, BigInteger.One), out var dateDuration);
            if (status != TemporalStatus.Completed)
                return status;

            roundedDuration = new InternalDuration(dateDuration, BigInteger.Zero);
        }
        else
        {
            status = RoundTimeDuration(balanced.TimeNanoseconds, roundingIncrement, smallestUnit, roundingMode, out var time);
            if (status != TemporalStatus.Completed)
                return status;

            roundedDuration = new InternalDuration(default, time);
        }

        return FromInternal(roundedDuration, largestUnit, out result);
    }

    /// <summary>
    /// A duration's total of a unit: <c>Temporal.Duration.prototype.total</c> (s7.3.21) after its reads - relative to a
    /// zoned instant through <c>DifferenceZonedDateTimeWithTotal</c>, relative to a plain date through
    /// <c>DifferencePlainDateTimeWithTotal</c>, and otherwise in days of 24 hours - an exact ratio rounded once.
    /// </summary>
    /// <param name="duration">The receiver's fields, a valid duration.</param>
    /// <param name="unit">The unit, year to nanosecond (<see cref="TemporalOptionRules.IsValidTotalUnit"/>).</param>
    /// <param name="relativeTo">The resolved <c>relativeTo</c>, or <see cref="RelativeTo.None"/>.</param>
    /// <param name="total">The total; zero unless the status is <see cref="TemporalStatus.Completed"/>.</param>
    /// <returns>
    /// <see cref="TemporalStatus.Completed"/>; <see cref="TemporalStatus.RelativeToRequired"/> where the duration or the
    /// unit is a calendar unit with no <c>relativeTo</c>; <see cref="TemporalStatus.OutOfRange"/> or
    /// <see cref="TemporalStatus.InvalidDuration"/> where an addition passes a limit.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">The unit is unset or auto, or a field is not finite.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s7.3.21 via Broiler.VM JsRealm.TemporalDuration DurationTotal; IP=Low; Security=Medium; Resources=2; Fingerprint=B5563E
    // Broiler-Human:        PENDING
    public static TemporalStatus Total(DurationFields duration, TemporalUnit unit, RelativeTo relativeTo, out double total)
    {
        CheckUnit(unit, nameof(unit));
        total = 0;
        TemporalStatus status;

        if (relativeTo.IsZoned)
        {
            var origin = relativeTo.EpochNanoseconds;
            status = AddZonedDateTime(origin, relativeTo.TimeZone!, relativeTo.Calendars!, relativeTo.Calendar, ToInternal(duration), TemporalOverflow.Constrain, out var target);
            return status != TemporalStatus.Completed
                ? status
                : TemporalDifferences.DifferenceZonedDateTimeWithTotal(origin, target, relativeTo.TimeZone!, relativeTo.Calendars!, relativeTo.Calendar, unit, out total);
        }

        if (relativeTo.IsPlain)
        {
            status = PlainTarget(duration, relativeTo, out var start, out var target);
            return status != TemporalStatus.Completed
                ? status
                : TemporalDifferences.DifferencePlainDateTimeWithTotal(relativeTo.Calendars!, relativeTo.Calendar, start, target, unit, out total);
        }

        if (IsCalendarUnit(DefaultLargestUnit(duration)) || IsCalendarUnit(unit))
            return TemporalStatus.RelativeToRequired;

        total = TotalTimeDuration(ToInternalWith24HourDays(duration).TimeNanoseconds, unit);
        return TemporalStatus.Completed;
    }

    /// <summary>
    /// A date duration's length in days from a date: the proposal's <c>DateDurationDays</c> (s7.5.29), its years, months
    /// and weeks added to the date in the calendar and its days added after.
    /// </summary>
    /// <param name="calendars">The calendar provider.</param>
    /// <param name="calendar">The calendar.</param>
    /// <param name="duration">The date duration, valid.</param>
    /// <param name="relativeTo">The date, within the limits.</param>
    /// <param name="days">The days; zero unless the status is <see cref="TemporalStatus.Completed"/>.</param>
    /// <returns><see cref="TemporalStatus.Completed"/>, or <see cref="TemporalStatus.OutOfRange"/> where the addition passes the limits.</returns>
    /// <exception cref="ArgumentNullException">The provider is <see langword="null"/>.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s7.5.29 via Broiler.VM JsTemporal.DateDurationDays; IP=Low; Security=Low; Resources=1; Fingerprint=39FA0C
    // Broiler-Human:        PENDING
    public static TemporalStatus DateDurationDays(ICalendarArithmetic calendars, CalendarId calendar, DateDuration duration, IsoDate relativeTo, out double days)
    {
        ArgumentNullException.ThrowIfNull(calendars);
        days = 0;
        var yearsMonthsWeeks = duration with { Days = 0 };
        if (Sign(yearsMonthsWeeks) == 0)
        {
            days = duration.Days;
            return TemporalStatus.Completed;
        }

        var status = CalendarDateAdd(calendars, calendar, relativeTo, yearsMonthsWeeks, TemporalOverflow.Constrain, out var later);
        if (status != TemporalStatus.Completed)
            return status;

        days = duration.Days + (EpochDays(later) - EpochDays(relativeTo));
        return TemporalStatus.Completed;
    }

    /// <summary>
    /// A duration added to a zoned instant: the proposal's <c>AddZonedDateTime</c> (s6.5.5) - the date part added to the
    /// wall clock in the calendar, the wall clock made an instant again, then the time part added exactly.
    /// </summary>
    /// <param name="epochNanoseconds">The instant, valid.</param>
    /// <param name="timeZone">The instant's time zone.</param>
    /// <param name="calendars">The calendar provider.</param>
    /// <param name="calendar">The instant's calendar.</param>
    /// <param name="duration">The internal duration; its date part valid.</param>
    /// <param name="overflow">The overflow the calendar addition takes.</param>
    /// <param name="result">The instant; zero unless the status is <see cref="TemporalStatus.Completed"/>.</param>
    /// <returns>
    /// <see cref="TemporalStatus.Completed"/>; <see cref="TemporalStatus.InvalidDate"/> where the calendar refuses a day
    /// under <see cref="TemporalOverflow.Reject"/>; <see cref="TemporalStatus.OutOfRange"/> where the wall clock or the
    /// instant passes the limits.
    /// </returns>
    /// <exception cref="ArgumentNullException">A provider is <see langword="null"/>.</exception>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s6.5.5 via Broiler.VM JsTemporal.AddZonedDateTime; IP=Low; Security=Medium; Resources=1; Fingerprint=9DC1C0
    // Broiler-Human:        PENDING
    public static TemporalStatus AddZonedDateTime(
        BigInteger epochNanoseconds, ITimeZoneArithmetic timeZone, ICalendarArithmetic calendars, CalendarId calendar, InternalDuration duration,
        TemporalOverflow overflow, out BigInteger result)
    {
        ArgumentNullException.ThrowIfNull(timeZone);
        ArgumentNullException.ThrowIfNull(calendars);
        result = BigInteger.Zero;

        if (Sign(duration.Date) == 0)
            return IsoArithmetic.AddInstant(epochNanoseconds, duration.TimeNanoseconds, out result);

        var dateTime = IsoDateTimeFor(timeZone, epochNanoseconds);
        var status = CalendarDateAdd(calendars, calendar, dateTime.Date, duration.Date, overflow, out var added);
        if (status != TemporalStatus.Completed)
            return status;

        var intermediate = new IsoDateTime(added, dateTime.Time);
        if (TemporalLimits.CheckIsoDateTime(intermediate) != TemporalStatus.Completed)
            return TemporalStatus.OutOfRange;

        status = EpochNanosecondsFor(timeZone, intermediate, out var intermediateNs);
        return status != TemporalStatus.Completed ? status : IsoArithmetic.AddInstant(intermediateNs, duration.TimeNanoseconds, out result);
    }

    // ---- Helpers ---------------------------------------------------------------------------------------------------

    // The start and end a plain relativeTo gives round and total: the date at midnight, and the date the duration
    // reaches with its time balanced into days (Temporal.Duration.prototype.round steps 30 to 34).
    // Broiler-AI:           Origin=Ported; Spec=Temporal s7.3.20 and s7.3.21 via Broiler.VM JsRealm.TemporalDuration DurationRound; IP=Low; Security=Low; Resources=1; Fingerprint=A0802F
    // Broiler-Human:        PENDING
    private static TemporalStatus PlainTarget(DurationFields duration, RelativeTo relativeTo, out IsoDateTime start, out IsoDateTime target)
    {
        start = new IsoDateTime(relativeTo.PlainDate, default);
        target = default;
        var internalDuration = ToInternalWith24HourDays(duration);
        var targetTime = IsoArithmetic.AddTime(default, internalDuration.TimeNanoseconds, out var carried);
        var status = CreateDateDurationRecord(internalDuration.Date.Years, internalDuration.Date.Months, internalDuration.Date.Weeks, carried, out var dateDuration);
        if (status != TemporalStatus.Completed)
            return status;

        status = CalendarDateAdd(relativeTo.Calendars!, relativeTo.Calendar, relativeTo.PlainDate, dateDuration, TemporalOverflow.Constrain, out var targetDate);
        if (status != TemporalStatus.Completed)
            return status;

        target = new IsoDateTime(targetDate, targetTime);
        return TemporalStatus.Completed;
    }

    // CreateTemporalDuration's validity test of fields TemporalDurationFromInternal made, every zero made positive.
    // Broiler-AI:           Origin=Ported; Spec=Temporal s7.5.19 via Broiler.VM JsTemporal.CreateDuration; IP=Low; Security=Low; Resources=0; Fingerprint=D40BF3
    // Broiler-Human:        PENDING
    private static TemporalStatus Create(DurationFields fields, out DurationFields result)
    {
        result = default;
        if (TemporalLimits.CheckDuration(fields) != TemporalStatus.Completed)
            return TemporalStatus.InvalidDuration;

        result = new DurationFields(
            fields.Years + 0.0, fields.Months + 0.0, fields.Weeks + 0.0, fields.Days + 0.0, fields.Hours + 0.0, fields.Minutes + 0.0,
            fields.Seconds + 0.0, fields.Milliseconds + 0.0, fields.Microseconds + 0.0, fields.Nanoseconds + 0.0);
        return TemporalStatus.Completed;
    }

    // A field negated, a zero staying positive.
    // Broiler-AI:           Origin=Ported; Spec=Temporal s7.5.20 via Broiler.VM JsTemporal.Negated; IP=Low; Security=None; Resources=0; Fingerprint=14D3AA
    // Broiler-Human:        PENDING
    private static double Negated(double value) => value == 0 ? 0 : -value;

    // Whether two durations' fields are equal as Numbers, the proposal's first step of compare: +0 and -0 are equal.
    // Broiler-AI:           Origin=Ported; Spec=Temporal s7.2.3 via Broiler.VM JsRealm.TemporalDuration DurationCompare; IP=Low; Security=None; Resources=0; Fingerprint=435CB0
    // Broiler-Human:        PENDING
    private static bool SameFields(DurationFields one, DurationFields two) =>
        one.Years == two.Years && one.Months == two.Months && one.Weeks == two.Weeks && one.Days == two.Days &&
        one.Hours == two.Hours && one.Minutes == two.Minutes && one.Seconds == two.Seconds &&
        one.Milliseconds == two.Milliseconds && one.Microseconds == two.Microseconds && one.Nanoseconds == two.Nanoseconds;

    // A date's epoch days.
    // Broiler-AI:           Origin=AI; Spec=Temporal s3.5.9; IP=None; Security=None; Resources=0; Fingerprint=9380FB
    // Broiler-Human:        PENDING
    private static long EpochDays(IsoDate date) => IsoCalendarMath.EpochDays(date.Year, date.Month, date.Day);

    // Whether a unit is a calendar unit: the proposal's IsCalendarUnit.
    // Broiler-AI:           Origin=Ported; Spec=Temporal s13.21 via Broiler.VM JsTemporalCore.IsCalendarUnit; IP=Low; Security=None; Resources=0; Fingerprint=D84F87
    // Broiler-Human:        PENDING
    private static bool IsCalendarUnit(TemporalUnit unit) => unit is TemporalUnit.Year or TemporalUnit.Month or TemporalUnit.Week;

    // Whether a unit's category is date (Table 21).
    // Broiler-AI:           Origin=Ported; Spec=Temporal Table 21 via Broiler.VM JsTemporalCore.IsDateUnit; IP=Low; Security=None; Resources=0; Fingerprint=C6E39D
    // Broiler-Human:        PENDING
    private static bool IsDateUnit(TemporalUnit unit) => unit <= TemporalUnit.Day;

    // The precondition of every member taking fields: each finite.
    // Broiler-AI:           Origin=AI; Spec=JSD-0078 preconditions; IP=None; Security=Low; Resources=0; Fingerprint=D33CBE
    // Broiler-Human:        PENDING
    private static void CheckFinite(DurationFields duration)
    {
        if (!double.IsFinite(duration.Years) || !double.IsFinite(duration.Months) || !double.IsFinite(duration.Weeks) ||
            !double.IsFinite(duration.Days) || !double.IsFinite(duration.Hours) || !double.IsFinite(duration.Minutes) ||
            !double.IsFinite(duration.Seconds) || !double.IsFinite(duration.Milliseconds) || !double.IsFinite(duration.Microseconds) ||
            !double.IsFinite(duration.Nanoseconds))
            throw new ArgumentOutOfRangeException(nameof(duration), "A duration field is not finite.");
    }

    // The precondition of a date duration: each field finite.
    // Broiler-AI:           Origin=AI; Spec=JSD-0078 preconditions; IP=None; Security=Low; Resources=0; Fingerprint=C8275F
    // Broiler-Human:        PENDING
    private static void CheckFinite(DateDuration duration, string name)
    {
        if (!double.IsFinite(duration.Years) || !double.IsFinite(duration.Months) || !double.IsFinite(duration.Weeks) || !double.IsFinite(duration.Days))
            throw new ArgumentOutOfRangeException(name, "A date duration field is not finite.");
    }

    // The precondition of a unit: year to nanosecond.
    // Broiler-AI:           Origin=AI; Spec=JSD-0078 preconditions; IP=None; Security=Low; Resources=0; Fingerprint=8F8097
    // Broiler-Human:        PENDING
    private static void CheckUnit(TemporalUnit unit, string name)
    {
        if (unit is < TemporalUnit.Year or > TemporalUnit.Nanosecond)
            throw new ArgumentOutOfRangeException(name, unit, "The unit is not year to nanosecond.");
    }

    // The precondition of a time unit or a day of 24 hours.
    // Broiler-AI:           Origin=AI; Spec=JSD-0078 preconditions; IP=None; Security=Low; Resources=0; Fingerprint=4014ED
    // Broiler-Human:        PENDING
    private static void CheckTimeUnit(TemporalUnit unit, string name)
    {
        if (unit is < TemporalUnit.Day or > TemporalUnit.Nanosecond)
            throw new ArgumentOutOfRangeException(name, unit, "The unit is not day to nanosecond.");
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
}
