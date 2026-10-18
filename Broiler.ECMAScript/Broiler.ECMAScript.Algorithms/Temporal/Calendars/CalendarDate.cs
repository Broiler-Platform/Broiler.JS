// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   1
// Annotated:        1/1
// Exempt:           1
// Human-reviewed:   0/1
// IP risk:          None
// Security risk:    None
// Criteria:         0/0
// Resource impact:  0/10 max
// Unverified:       1
//
// GENERATED - DO NOT EDIT MANUALLY

namespace Broiler.ECMAScript.Temporal;

/// <summary>
/// A Calendar Date Record (Temporal s12.3.1): what a calendar makes of an ISO date, which CalendarISOToDate
/// (s12.3.26) answers and the getters of every date-bearing Temporal type read.
/// </summary>
/// <param name="Era">The era's code of Table 2 (<c>era</c>), or <see langword="null"/> in a calendar without eras.</param>
/// <param name="EraYear">The year within the era (<c>eraYear</c>), or <see langword="null"/> in a calendar without eras.</param>
/// <param name="Year">The arithmetic year (<c>year</c>).</param>
/// <param name="Month">The ordinal month, from 1 (<c>month</c>).</param>
/// <param name="MonthCode">The month code (<c>monthCode</c>).</param>
/// <param name="Day">The day of the month, from 1 (<c>day</c>).</param>
/// <param name="DayOfWeek">The ISO weekday, Monday 1 to Sunday 7 (<c>dayOfWeek</c>).</param>
/// <param name="DayOfYear">The day of the calendar year, from 1 (<c>dayOfYear</c>).</param>
/// <param name="WeekOfYear">The ISO week in ISO 8601 (<c>weekOfYear</c>); <see langword="null"/> in every other calendar.</param>
/// <param name="YearOfWeek">The ISO week's year in ISO 8601 (<c>yearOfWeek</c>); <see langword="null"/> in every other calendar.</param>
/// <param name="DaysInMonth">The days of the date's month (<c>daysInMonth</c>).</param>
/// <param name="DaysInYear">The days of the date's year (<c>daysInYear</c>).</param>
/// <param name="MonthsInYear">The months of the date's year (<c>monthsInYear</c>).</param>
/// <param name="InLeapYear">Whether the year is a leap year (<c>inLeapYear</c>, the era proposal's 4.1.14).</param>
// Broiler-AI:           Origin=Ported; Spec=Temporal s12.3.1 via Broiler.VM JsCalendarDate; IP=None; Security=None; Resources=0; Fingerprint=E58A3E
// Broiler-Human:        PENDING
public readonly record struct CalendarDate(
    string? Era,
    long? EraYear,
    long Year,
    int Month,
    MonthCode MonthCode,
    int Day,
    int DayOfWeek,
    int DayOfYear,
    int? WeekOfYear,
    long? YearOfWeek,
    int DaysInMonth,
    int DaysInYear,
    int MonthsInYear,
    bool InLeapYear)
{
    /// <summary>The days of a week (<c>daysInWeek</c>), 7 in every calendar Table 1 lists.</summary>
    public int DaysInWeek => 7;
}
