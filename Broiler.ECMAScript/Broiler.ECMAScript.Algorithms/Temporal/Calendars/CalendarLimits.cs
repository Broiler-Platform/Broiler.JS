// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   10
// Annotated:        10/10
// Exempt:           0
// Human-reviewed:   0/10
// IP risk:          Low
// Security risk:    Low
// Criteria:         0/0
// Resource impact:  1/10 max
// Unverified:       10
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using Broiler.ECMAScript.Time;

namespace Broiler.ECMAScript.Temporal;

/// <summary>
/// The Temporal proposal's limits as the calendars test them, over epoch days: ISODateWithinLimits (s3.5.12),
/// ISOYearMonthWithinLimits and CompareISODate (s3.5.13), ported from Broiler.VM's <c>JsTemporalCore</c>.
/// The records-and-limits sub-area publishes the limits themselves; the calendars keep these private copies so
/// that they take no dependency on it, and every check here answers a status (JSD-0078, decision D9).
/// </summary>
// Broiler-AI:           Origin=Ported; Spec=Temporal s3.5.12 and ISOYearMonthWithinLimits via Broiler.VM JsTemporalCore; IP=Low; Security=Low; Resources=1; Fingerprint=C08EE8
// Broiler-Human:        PENDING
internal static class CalendarLimits
{
    /// <summary>-271821-04-19, the first day ISODateWithinLimits admits: 10^8 days and one before the epoch.</summary>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s3.5.12 and ISOYearMonthWithinLimits via Broiler.VM JsTemporalCore.MinDay; IP=Low; Security=Low; Resources=0; Fingerprint=9EF800
    // Broiler-Human:        PENDING
    internal const long MinDay = -100_000_001;

    /// <summary>275760-09-13, the last day ISODateWithinLimits admits: 10^8 days after the epoch.</summary>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s3.5.12 and ISOYearMonthWithinLimits via Broiler.VM JsTemporalCore.MaxDay; IP=Low; Security=Low; Resources=0; Fingerprint=20AFB7
    // Broiler-Human:        PENDING
    internal const long MaxDay = 100_000_000;

    /// <summary>The first ISO year a calendar operation computes over: the year-month limit's.</summary>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s3.5.12 and ISOYearMonthWithinLimits via Broiler.VM JsTemporalCore.MinYear; IP=Low; Security=Low; Resources=0; Fingerprint=08719E
    // Broiler-Human:        PENDING
    internal const long MinYear = -271821;

    /// <summary>The last ISO year a calendar operation computes over: the year-month limit's.</summary>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s3.5.12 and ISOYearMonthWithinLimits via Broiler.VM JsTemporalCore.MaxYear; IP=Low; Security=Low; Resources=0; Fingerprint=31D1F3
    // Broiler-Human:        PENDING
    internal const long MaxYear = 275760;

    /// <summary>A year bound beyond every calendar's years within the ISO limits (the Hijri ones reach 283,583): the shared sources'.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0076 via Broiler.VM JsTemporal.YearBound; IP=Low; Security=Low; Resources=0; Fingerprint=8BC54B
    // Broiler-Human:        PENDING
    internal const long YearBound = CalendarYears.MaxYearMagnitude;

    /// <summary>ISODateWithinLimits (s3.5.12): the date at noon is a representable date-time, which is a day count from <see cref="MinDay"/> to <see cref="MaxDay"/>.</summary>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s3.5.12 via Broiler.VM JsTemporalCore.DateWithinLimits; IP=Low; Security=Low; Resources=0; Fingerprint=994BC5
    // Broiler-Human:        PENDING
    internal static bool DateWithinLimits(long epochDays) => epochDays is >= MinDay and <= MaxDay;

    /// <summary>ISOYearMonthWithinLimits: April -271821 to September 275760.</summary>
    // Broiler-AI:           Origin=Ported; Spec=Temporal ISOYearMonthWithinLimits via Broiler.VM JsTemporalCore.YearMonthWithinLimits; IP=Low; Security=Low; Resources=0; Fingerprint=407F85
    // Broiler-Human:        PENDING
    internal static bool YearMonthWithinLimits(IsoDate date) =>
        date.Year >= MinYear && date.Year <= MaxYear &&
        !(date.Year == MinYear && date.Month < 4) && !(date.Year == MaxYear && date.Month > 9);

    /// <summary>CompareISODate (s3.5.13).</summary>
    // Broiler-AI:           Origin=Ported; Spec=Temporal s3.5.13 via Broiler.VM JsTemporalCore.Compare; IP=Low; Security=Low; Resources=0; Fingerprint=7D369E
    // Broiler-Human:        PENDING
    internal static int Compare(IsoDate one, IsoDate two) =>
        one.Year != two.Year ? (one.Year > two.Year ? 1 : -1)
        : one.Month != two.Month ? (one.Month > two.Month ? 1 : -1)
        : one.Day != two.Day ? (one.Day > two.Day ? 1 : -1)
        : 0;

    /// <summary>
    /// Whether an ISO date is one a calendar operation computes over: a valid ISO date whose year is within
    /// <see cref="MinYear"/> to <see cref="MaxYear"/>, which holds every date and every year-month's reference date
    /// within the limits.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The month is outside 1 to 12, or the day is not one of the month's.</exception>
    // Broiler-AI:           Origin=AI; Spec=Temporal s3.5.12 and ISOYearMonthWithinLimits; IP=Low; Security=Low; Resources=1; Fingerprint=F17F99
    // Broiler-Human:        PENDING
    internal static bool IsComputable(IsoDate date)
    {
        if (date.Month is < 1 or > 12 || date.Day is < 1 or > 31)
            throw new ArgumentOutOfRangeException(nameof(date), date, "The month and day are not an ISO date's.");

        if (date.Year is < MinYear or > MaxYear)
            return false;

        if (date.Day > IsoCalendarMath.DaysInMonth(date.Year, date.Month))
            throw new ArgumentOutOfRangeException(nameof(date), date, "The day is not one of the month's.");

        return true;
    }
}
