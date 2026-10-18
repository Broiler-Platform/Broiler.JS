// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   3
// Annotated:        3/3
// Exempt:           0
// Human-reviewed:   0/3
// IP risk:          None
// Security risk:    Low
// Criteria:         0/0
// Resource impact:  0/10 max
// Unverified:       3
//
// GENERATED - DO NOT EDIT MANUALLY

using Broiler.ECMAScript.Time;

namespace Broiler.ECMAScript.Temporal;

/// <summary>
/// A calendar's addition and difference, the two operations the duration arithmetic, the relative rounding and
/// the differences call (the area design's <c>ICalendarArithmetic</c>, JSD-0078's decision 1: "calendars and
/// zones are reached through provider interfaces"): CalendarDateAdd (Temporal s12.3.7) and CalendarDateUntil
/// (s12.3.9), over every calendar of <see cref="CalendarId"/>. <see cref="TemporalCalendars"/> implements it.
/// </summary>
/// <remarks>
/// Each member answers a <see cref="CalendarStatus"/>, never an exception, for an input an engine can reach;
/// the engine throws a <c>RangeError</c> for a status other than <see cref="CalendarStatus.Completed"/>.
/// Every member is safe to call concurrently.
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=Temporal s12.3.7 and s12.3.9, JSD-0078 decision 1; IP=None; Security=None; Resources=0; Fingerprint=EC1BCC
// Broiler-Human:        PENDING
public interface ICalendarArithmetic
{
    /// <summary>
    /// CalendarDateAdd (Temporal s12.3.7): a date moved by years and months in the calendar, its month code
    /// constrained or rejected in the year reached and its day regulated in the month reached, then by weeks and
    /// days. In a calendar other than ISO 8601 it is NonISODateAdd (the era proposal's 4.1.18).
    /// </summary>
    /// <param name="calendar">The calendar.</param>
    /// <param name="date">The date, a valid ISO date whose year is within -271821 to 275760.</param>
    /// <param name="duration">The years, months, weeks and days to add.</param>
    /// <param name="overflow">Whether a month code or day the target month lacks is constrained or rejected.</param>
    /// <param name="result">The date reached; <see langword="default"/> unless the status is <see cref="CalendarStatus.Completed"/>.</param>
    /// <returns>
    /// <see cref="CalendarStatus.Completed"/>; under <see cref="CalendarOverflow.Reject"/>,
    /// <see cref="CalendarStatus.MonthNotInYear"/> or <see cref="CalendarStatus.FieldOutOfRange"/>; or
    /// <see cref="CalendarStatus.OutOfRange"/> for a result outside ISODateWithinLimits.
    /// </returns>
    // Broiler-AI:           Origin=AI; Spec=Temporal s12.3.7; IP=None; Security=Low; Resources=0; Fingerprint=5ACF37
    // Broiler-Human:        PENDING
    CalendarStatus DateAdd(CalendarId calendar, IsoDate date, CalendarDuration duration, CalendarOverflow overflow, out IsoDate result);

    /// <summary>
    /// CalendarDateUntil (Temporal s12.3.9): the difference from one date to another in the calendar, the largest
    /// years and months that do not pass the second date (CompareSurpasses, s3.5.5, and NonISODateSurpasses, the era
    /// proposal's 4.1.17), then weeks and days. In a calendar other than ISO 8601 it is NonISODateUntil (4.1.19).
    /// </summary>
    /// <param name="calendar">The calendar.</param>
    /// <param name="one">The first date, within ISODateWithinLimits.</param>
    /// <param name="two">The second date, within ISODateWithinLimits.</param>
    /// <param name="largestUnit">The largest unit of the difference.</param>
    /// <param name="result">The difference, every field of one sign; <see langword="default"/> unless the status is <see cref="CalendarStatus.Completed"/>.</param>
    /// <returns><see cref="CalendarStatus.Completed"/>, or <see cref="CalendarStatus.OutOfRange"/> for a date outside the limits.</returns>
    // Broiler-AI:           Origin=AI; Spec=Temporal s12.3.9; IP=None; Security=Low; Resources=0; Fingerprint=25214D
    // Broiler-Human:        PENDING
    CalendarStatus DateUntil(CalendarId calendar, IsoDate one, IsoDate two, CalendarUnit largestUnit, out CalendarDuration result);
}
