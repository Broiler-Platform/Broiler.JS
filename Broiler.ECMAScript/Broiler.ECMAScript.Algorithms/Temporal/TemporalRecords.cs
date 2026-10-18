// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   6
// Annotated:        6/6
// Exempt:           19
// Human-reviewed:   0/6
// IP risk:          Low
// Security risk:    None
// Criteria:         0/0
// Resource impact:  0/10 max
// Unverified:       6
//
// GENERATED - DO NOT EDIT MANUALLY

using System.Numerics;

namespace Broiler.ECMAScript.Temporal;

/// <summary>
/// The Temporal proposal's units, in the order of its Table 21 ("Temporal units by descending magnitude"):
/// largest first, so that <c>LargerOfTwoTemporalUnits</c> is the smaller enumeration value, with the
/// specification's <c>unset</c> and <c>auto</c> after them.
/// </summary>
// Broiler-AI:           Origin=Ported; Spec=Temporal Table 21 via Broiler.VM TemporalUnit; IP=Low; Security=None; Resources=0; Fingerprint=9775F7
// Broiler-Human:        PENDING
public enum TemporalUnit
{
    /// <summary><c>year</c>, a calendar unit.</summary>
    Year = 0,

    /// <summary><c>month</c>, a calendar unit.</summary>
    Month = 1,

    /// <summary><c>week</c>, a calendar unit.</summary>
    Week = 2,

    /// <summary><c>day</c>, a date unit of 24 hours where a length is needed.</summary>
    Day = 3,

    /// <summary><c>hour</c>, 3.6 × 10^12 nanoseconds.</summary>
    Hour = 4,

    /// <summary><c>minute</c>, 6 × 10^10 nanoseconds.</summary>
    Minute = 5,

    /// <summary><c>second</c>, 10^9 nanoseconds.</summary>
    Second = 6,

    /// <summary><c>millisecond</c>, 10^6 nanoseconds.</summary>
    Millisecond = 7,

    /// <summary><c>microsecond</c>, 10^3 nanoseconds.</summary>
    Microsecond = 8,

    /// <summary><c>nanosecond</c>.</summary>
    Nanosecond = 9,

    /// <summary>The specification's <c>unset</c>: no unit was given.</summary>
    Unset = 10,

    /// <summary>The specification's <c>auto</c>.</summary>
    Auto = 11,
}

/// <summary>
/// How a Temporal records, limits or balancing operation ended (JSD-0078's X3: a failure is data). Each engine
/// maps a status other than <see cref="Completed"/> to its own error, a <c>RangeError</c> in both today, at the
/// moment the specification throws it.
/// </summary>
// Broiler-AI:           Origin=AI; Spec=JSD-0078 X3 and decision D9; IP=Low; Security=None; Resources=0; Fingerprint=E93601
// Broiler-Human:        PENDING
public enum TemporalStatus
{
    /// <summary>The operation completed, and its result is valid.</summary>
    Completed = 0,

    /// <summary>
    /// A date, date-time, year-month, instant or day count is outside the representable range the proposal's
    /// <c>ISODateWithinLimits</c>, <c>ISODateTimeWithinLimits</c>, <c>ISOYearMonthWithinLimits</c>,
    /// <c>IsValidEpochNanoseconds</c> or <c>CheckISODaysRange</c> gives, or an addition's operand is past the
    /// point where its result could be within it.
    /// </summary>
    OutOfRange = 1,

    /// <summary>
    /// The year, month and day are not a date of the ISO 8601 calendar (<c>IsValidISODate</c>): a regulation
    /// under <c>reject</c>, or a record handed to a limit check.
    /// </summary>
    InvalidDate = 2,

    /// <summary>The fields are not a time of day (<c>IsValidTime</c>): a regulation under <c>reject</c>, or a record handed to a limit check.</summary>
    InvalidTime = 3,

    /// <summary>The fields are not a valid duration (<c>IsValidDuration</c>): mixed signs, a value that is not finite, or a value too large.</summary>
    InvalidDuration = 4,
}

/// <summary>The proposal's <c>overflow</c> option (<c>GetTemporalOverflowOption</c>): how a field outside its range is treated.</summary>
// Broiler-AI:           Origin=Ported; Spec=Temporal GetTemporalOverflowOption via Broiler.VM JsTemporal.Reject; IP=Low; Security=None; Resources=0; Fingerprint=BFC8A2
// Broiler-Human:        PENDING
public enum TemporalOverflow
{
    /// <summary><c>"constrain"</c>, the default: each field is clamped to its range.</summary>
    Constrain = 0,

    /// <summary><c>"reject"</c>: a field outside its range is refused.</summary>
    Reject = 1,
}

/// <summary>
/// A Date Duration Record of the Temporal proposal: the calendar part of a duration, each field a float64-representable
/// integer, all of one sign.
/// </summary>
/// <param name="Years">The years.</param>
/// <param name="Months">The months.</param>
/// <param name="Weeks">The weeks.</param>
/// <param name="Days">The days.</param>
// Broiler-AI:           Origin=Ported; Spec=Temporal Date Duration Record via Broiler.VM JsDateDuration; IP=Low; Security=None; Resources=0; Fingerprint=27F1C9
// Broiler-Human:        PENDING
public readonly record struct DateDuration(double Years, double Months, double Weeks, double Days);

/// <summary>
/// An Internal Duration Record of the Temporal proposal: a date duration and an exact time duration, the time part
/// in nanoseconds of magnitude at most <see cref="TemporalLimits.MaxTimeDuration"/>.
/// </summary>
/// <param name="Date">The date duration.</param>
/// <param name="TimeNanoseconds">The time duration, in nanoseconds.</param>
// Broiler-AI:           Origin=Ported; Spec=Temporal Internal Duration Record via Broiler.VM JsInternalDuration; IP=Low; Security=None; Resources=0; Fingerprint=D4D151
// Broiler-Human:        PENDING
public readonly record struct InternalDuration(DateDuration Date, BigInteger TimeNanoseconds);

/// <summary>
/// The ten fields of a <c>Temporal.Duration</c>, in the proposal's Table 7 order: each a float64-representable
/// integer, all of one sign, as the grammar answers a duration string and the serialization writes one.
/// </summary>
/// <param name="Years">The years.</param>
/// <param name="Months">The months.</param>
/// <param name="Weeks">The weeks.</param>
/// <param name="Days">The days.</param>
/// <param name="Hours">The hours.</param>
/// <param name="Minutes">The minutes.</param>
/// <param name="Seconds">The seconds.</param>
/// <param name="Milliseconds">The milliseconds.</param>
/// <param name="Microseconds">The microseconds.</param>
/// <param name="Nanoseconds">The nanoseconds.</param>
// Broiler-AI:           Origin=Ported; Spec=Temporal Duration internal slots via Broiler.VM JsDurationObject.Fields; IP=Low; Security=None; Resources=0; Fingerprint=A8F9CC
// Broiler-Human:        PENDING
public readonly record struct DurationFields(
    double Years,
    double Months,
    double Weeks,
    double Days,
    double Hours,
    double Minutes,
    double Seconds,
    double Milliseconds,
    double Microseconds,
    double Nanoseconds);
