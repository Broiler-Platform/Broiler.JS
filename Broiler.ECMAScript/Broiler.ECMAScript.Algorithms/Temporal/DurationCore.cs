// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   8
// Annotated:        8/8
// Exempt:           0
// Human-reviewed:   0/8
// IP risk:          Low
// Security risk:    Low
// Criteria:         0/0
// Resource impact:  1/10 max
// Unverified:       8
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Numerics;
using Broiler.ECMAScript.Numbers;

namespace Broiler.ECMAScript.Temporal;

// The duration helpers the records, limits and serialization need: a unit's length, a duration's sign and default
// largest unit, its time part as exact nanoseconds, and TemporalDurationFromInternal's balancing. Internal at
// milestone SJL-10.3's part A; the durations sub-area (part C) publishes the operations it shares, and these stay
// the one implementation of each.
// Broiler-AI:           Origin=Ported; Spec=Temporal duration operations via Broiler.VM JsTemporalCore; IP=Low; Security=Low; Resources=1; Fingerprint=E1867A
// Broiler-Human:        PENDING
internal static class DurationCore
{
    // The "Length in Nanoseconds" of a time unit or day (Table 21): zero for the calendar units, which have none.
    // Broiler-AI:           Origin=Ported; Spec=Temporal Table 21 via Broiler.VM JsTemporalCore.UnitLength; IP=Low; Security=Low; Resources=0; Fingerprint=02B1EE
    // Broiler-Human:        PENDING
    internal static long UnitLength(TemporalUnit unit) => unit switch
    {
        TemporalUnit.Day => TemporalLimits.NanosecondsPerDay,
        TemporalUnit.Hour => 3_600_000_000_000L,
        TemporalUnit.Minute => 60_000_000_000L,
        TemporalUnit.Second => 1_000_000_000L,
        TemporalUnit.Millisecond => 1_000_000L,
        TemporalUnit.Microsecond => 1_000L,
        TemporalUnit.Nanosecond => 1L,
        _ => 0L,
    };

    // The proposal's LargerOfTwoTemporalUnits: the earlier in Table 21.
    // Broiler-AI:           Origin=Ported; Spec=Temporal LargerOfTwoTemporalUnits via Broiler.VM JsTemporalCore.Larger; IP=Low; Security=Low; Resources=0; Fingerprint=CC1E45
    // Broiler-Human:        PENDING
    internal static TemporalUnit Larger(TemporalUnit one, TemporalUnit two) => one <= two ? one : two;

    // The proposal's TimeDurationFromComponents over a duration's six time fields, exact.
    // Broiler-AI:           Origin=Ported; Spec=Temporal TimeDurationFromComponents via Broiler.VM JsTemporalCore.TimeFromComponents; IP=Low; Security=Low; Resources=1; Fingerprint=04B415
    // Broiler-Human:        PENDING
    internal static BigInteger TimeFromComponents(DurationFields duration) =>
        TimeFromComponents(duration.Hours, duration.Minutes, duration.Seconds, duration.Milliseconds, duration.Microseconds, duration.Nanoseconds);

    // The proposal's TimeDurationFromComponents, exact.
    // Broiler-AI:           Origin=Ported; Spec=Temporal TimeDurationFromComponents via Broiler.VM JsTemporalCore.TimeFromComponents; IP=Low; Security=Low; Resources=1; Fingerprint=BED123
    // Broiler-Human:        PENDING
    internal static BigInteger TimeFromComponents(double hours, double minutes, double seconds, double milliseconds, double microseconds, double nanoseconds) =>
        (new BigInteger(hours) * 3_600_000_000_000L) + (new BigInteger(minutes) * 60_000_000_000L) + (new BigInteger(seconds) * 1_000_000_000L) +
        (new BigInteger(milliseconds) * 1_000_000L) + (new BigInteger(microseconds) * 1000) + new BigInteger(nanoseconds);

    // The proposal's DurationSign: the sign of the first field that is not zero.
    // Broiler-AI:           Origin=Ported; Spec=Temporal DurationSign via Broiler.VM JsTemporalCore.DurationSign; IP=Low; Security=Low; Resources=0; Fingerprint=A74841
    // Broiler-Human:        PENDING
    internal static int Sign(DurationFields duration)
    {
        ReadOnlySpan<double> fields =
        [
            duration.Years, duration.Months, duration.Weeks, duration.Days, duration.Hours, duration.Minutes, duration.Seconds,
            duration.Milliseconds, duration.Microseconds, duration.Nanoseconds,
        ];
        foreach (var value in fields)
        {
            if (value < 0)
                return -1;
            if (value > 0)
                return 1;
        }

        return 0;
    }

    // The proposal's DefaultTemporalLargestUnit: the largest unit whose field is not zero, nanosecond for none.
    // Broiler-AI:           Origin=Ported; Spec=Temporal DefaultTemporalLargestUnit via Broiler.VM JsTemporalCore.DefaultLargestUnit; IP=Low; Security=Low; Resources=0; Fingerprint=0B3A90
    // Broiler-Human:        PENDING
    internal static TemporalUnit DefaultLargestUnit(DurationFields duration) =>
        duration.Years != 0 ? TemporalUnit.Year
        : duration.Months != 0 ? TemporalUnit.Month
        : duration.Weeks != 0 ? TemporalUnit.Week
        : duration.Days != 0 ? TemporalUnit.Day
        : duration.Hours != 0 ? TemporalUnit.Hour
        : duration.Minutes != 0 ? TemporalUnit.Minute
        : duration.Seconds != 0 ? TemporalUnit.Second
        : duration.Milliseconds != 0 ? TemporalUnit.Millisecond
        : duration.Microseconds != 0 ? TemporalUnit.Microsecond
        : TemporalUnit.Nanosecond;

    // The proposal's TemporalDurationFromInternal before CreateTemporalDuration: the time balanced up to the
    // largest unit, each field made a Number once.
    // Broiler-AI:           Origin=Ported; Spec=Temporal TemporalDurationFromInternal via Broiler.VM JsTemporalCore.FromInternal; IP=Low; Security=Low; Resources=1; Fingerprint=7A9F45
    // Broiler-Human:        PENDING
    internal static DurationFields FromInternal(DateDuration date, BigInteger time, TemporalUnit largestUnit)
    {
        var sign = time.Sign;
        var nanoseconds = BigInteger.Abs(time);
        BigInteger days = 0, hours = 0, minutes = 0, seconds = 0, milliseconds = 0, microseconds = 0;

        if (largestUnit <= TemporalUnit.Microsecond)
            microseconds = BigInteger.DivRem(nanoseconds, 1000, out nanoseconds);
        if (largestUnit <= TemporalUnit.Millisecond)
            milliseconds = BigInteger.DivRem(microseconds, 1000, out microseconds);
        if (largestUnit <= TemporalUnit.Second)
            seconds = BigInteger.DivRem(milliseconds, 1000, out milliseconds);
        if (largestUnit <= TemporalUnit.Minute)
            minutes = BigInteger.DivRem(seconds, 60, out seconds);
        if (largestUnit <= TemporalUnit.Hour)
            hours = BigInteger.DivRem(minutes, 60, out minutes);
        if (largestUnit <= TemporalUnit.Day)
            days = BigInteger.DivRem(hours, 24, out hours);

        double Signed(BigInteger value) => ExactRounding.ToDouble(value * sign, BigInteger.One);

        return new DurationFields(
            date.Years,
            date.Months,
            date.Weeks,
            ExactRounding.ToDouble(new BigInteger(date.Days) + (days * sign), BigInteger.One),
            Signed(hours),
            Signed(minutes),
            Signed(seconds),
            Signed(milliseconds),
            Signed(microseconds),
            Signed(nanoseconds));
    }
}
