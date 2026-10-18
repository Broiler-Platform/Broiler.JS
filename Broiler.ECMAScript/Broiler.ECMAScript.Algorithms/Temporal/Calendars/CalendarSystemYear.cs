// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   8
// Annotated:        8/8
// Exempt:           5
// Human-reviewed:   0/8
// IP risk:          Low
// Security risk:    Low
// Criteria:         0/0
// Resource impact:  1/10 max
// Unverified:       8
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using Broiler.ECMAScript.Time;

namespace Broiler.ECMAScript.Temporal;

/// <summary>
/// One year of a calendar other than ISO 8601, as the arithmetic reads it: its arithmetic year, its first day
/// in days since 1970-01-01, the days before each of its months, and its leap month's ordinal (0 for none).
/// </summary>
// Broiler-AI:           Origin=Ported; Spec=Intl era and month code proposal 4.1 via Broiler.VM JsCalendarYear; IP=Low; Security=Low; Resources=1; Fingerprint=BEA95B
// Broiler-Human:        PENDING
internal sealed class CalendarSystemYear
{
    // Offsets[m - 1] is the days before month m; the last element is the year's length.
    private readonly int[] offsets;

    internal CalendarSystemYear(long year, long start, int[] offsets, int leapMonth = 0)
    {
        Year = year;
        Start = start;
        this.offsets = offsets;
        LeapMonth = leapMonth;
    }

    /// <summary>The arithmetic year (the Calendar Date Record's [[Year]]).</summary>
    internal long Year { get; }

    /// <summary>The year's first day, in days since 1970-01-01.</summary>
    internal long Start { get; }

    /// <summary>The ordinal of the year's leap month, or 0.</summary>
    internal int LeapMonth { get; }

    /// <summary>The months of the year.</summary>
    // Broiler-AI:           Origin=Ported; Spec=Intl era and month code proposal 4.1 via Broiler.VM JsCalendarYear.Months; IP=Low; Security=Low; Resources=1; Fingerprint=19A094
    // Broiler-Human:        PENDING
    internal int Months => offsets.Length - 1;

    /// <summary>The days of the year.</summary>
    // Broiler-AI:           Origin=Ported; Spec=Intl era and month code proposal 4.1 via Broiler.VM JsCalendarYear.Days; IP=Low; Security=Low; Resources=1; Fingerprint=B00915
    // Broiler-Human:        PENDING
    internal int Days => offsets[^1];

    /// <summary>The days of a month, by its ordinal.</summary>
    // Broiler-AI:           Origin=Ported; Spec=Intl era and month code proposal 4.1 via Broiler.VM JsCalendarYear.DaysInMonth; IP=Low; Security=Low; Resources=1; Fingerprint=114C09
    // Broiler-Human:        PENDING
    internal int DaysInMonth(int month) => offsets[month] - offsets[month - 1];

    /// <summary>The days of the year before a month, by its ordinal.</summary>
    // Broiler-AI:           Origin=Ported; Spec=Intl era and month code proposal 4.1 via Broiler.VM JsCalendarYear.DaysBefore; IP=Low; Security=Low; Resources=1; Fingerprint=49644D
    // Broiler-Human:        PENDING
    internal int DaysBefore(int month) => offsets[month - 1];

    /// <summary>The month and day of a day of the year, both from 1.</summary>
    // Broiler-AI:           Origin=Ported; Spec=Intl era and month code proposal 4.1 via Broiler.VM JsCalendarYear.MonthDay; IP=Low; Security=Low; Resources=1; Fingerprint=F413A1
    // Broiler-Human:        PENDING
    internal (int Month, int Day) MonthDay(int dayOfYear)
    {
        var month = 1;
        while (month < Months && offsets[month] < dayOfYear)
            month++;

        return (month, dayOfYear - offsets[month - 1]);
    }

    /// <summary>A year the shared calendar year sources answer (JSD-0076), as the arithmetic reads it.</summary>
    // Broiler-AI:           Origin=Ported; Spec=Intl era and month code proposal 4.1 via Broiler.VM JsCalendarYear.Of; IP=Low; Security=Low; Resources=1; Fingerprint=930C7B
    // Broiler-Human:        PENDING
    internal static CalendarSystemYear Of(CalendarYear year)
    {
        Span<int> lengths = stackalloc int[year.MonthsInYear];
        for (var month = 1; month <= year.MonthsInYear; month++)
            lengths[month - 1] = year.DaysInMonth(month);

        return new CalendarSystemYear(year.Year, year.NewYearEpochDays, FromLengths(lengths), year.LeapMonth);
    }

    /// <summary>The days before each month, and the year's length last, from the months' lengths.</summary>
    // Broiler-AI:           Origin=Ported; Spec=Intl era and month code proposal 4.1 via Broiler.VM JsCalendarYear.FromLengths; IP=Low; Security=Low; Resources=1; Fingerprint=BD49AB
    // Broiler-Human:        PENDING
    internal static int[] FromLengths(ReadOnlySpan<int> lengths)
    {
        var result = new int[lengths.Length + 1];
        for (var i = 0; i < lengths.Length; i++)
            result[i + 1] = result[i] + lengths[i];

        return result;
    }
}
