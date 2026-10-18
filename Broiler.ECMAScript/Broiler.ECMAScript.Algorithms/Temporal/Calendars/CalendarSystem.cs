// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   12
// Annotated:        12/12
// Exempt:           8
// Human-reviewed:   0/12
// IP risk:          Low
// Security risk:    Low
// Criteria:         0/0
// Resource impact:  1/10 max
// Unverified:       12
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using Broiler.ECMAScript.Time;

namespace Broiler.ECMAScript.Temporal;

/// <summary>
/// The arithmetic of one calendar the Intl era and month code proposal requires (its Table 1), other than
/// ISO 8601: how its years divide into months, which month codes and eras it has, and how its dates map to days
/// (JSD-0056, ported from Broiler.VM's <c>JsCalendarSystem</c>).
/// </summary>
/// <remarks>
/// <para>
/// <b>Everything is in days since 1970-01-01</b>, as ISO dates are elsewhere; the algorithms stated in
/// Calendrical Calculations' fixed days (R.D.) convert at <see cref="RataDieOfEpoch"/>.
/// </para>
/// <para>
/// <b>A year is computed when it is asked for</b> and nothing is cached: the arithmetic calendars take a few
/// multiplications, and the table calendars a table lookup or a few dozen additions. Every instance is
/// immutable, so every member is safe to call concurrently.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=Ported; Spec=Intl era and month code proposal 4.1 via Broiler.VM JsCalendarSystem; IP=Low; Security=Low; Resources=1; Fingerprint=59BB74
// Broiler-Human:        PENDING
internal abstract class CalendarSystem(CalendarId id)
{
    /// <summary>R.D. 719163 is 1970-01-01.</summary>
    // Broiler-AI:           Origin=Ported; Spec=Intl era and month code proposal 4.1 via Broiler.VM JsCalendarSystem.RataDieOfEpoch; IP=Low; Security=Low; Resources=1; Fingerprint=A9315E
    // Broiler-Human:        PENDING
    internal const long RataDieOfEpoch = 719163;

    /// <summary>The calendar.</summary>
    internal CalendarId Id { get; } = id;

    /// <summary>Whether a missing leap month becomes the common month of its number (Table 3's skip-backward) rather than <c>M06</c>.</summary>
    internal virtual bool SkipBackward => true;

    /// <summary>
    /// Whether <see cref="MonthsBefore"/> reads the calendar's years, which the shared sources answer only within
    /// <see cref="CalendarYears.MaxYearMagnitude"/>: the Chinese and Korean calendars.
    /// </summary>
    internal virtual bool MonthsBeforeReadsYears => false;

    /// <summary>The mean months in a year, for estimating which year a month count falls in.</summary>
    internal virtual double MeanMonths => 12;

    /// <summary>Whether the calendar has eras (CalendarSupportsEra, the era proposal's 4.1.1).</summary>
    internal virtual bool HasEras => false;

    /// <summary>The year whose arithmetic year is <paramref name="year"/>.</summary>
    // Broiler-AI:           Origin=Ported; Spec=Intl era and month code proposal 4.1 via Broiler.VM JsCalendarSystem.Year; IP=Low; Security=Low; Resources=1; Fingerprint=8C929E
    // Broiler-Human:        PENDING
    internal abstract CalendarSystemYear Year(long year);

    /// <summary>The arithmetic year containing a day.</summary>
    // Broiler-AI:           Origin=Ported; Spec=Intl era and month code proposal 4.1 via Broiler.VM JsCalendarSystem.YearOf; IP=Low; Security=Low; Resources=1; Fingerprint=F451E6
    // Broiler-Human:        PENDING
    internal abstract long YearOf(long epochDays);

    /// <summary>Whether a year is a leap year (the era proposal's 4.1.14, note 7).</summary>
    // Broiler-AI:           Origin=Ported; Spec=Intl era and month code proposal 4.1 via Broiler.VM JsCalendarSystem.InLeapYear; IP=Low; Security=Low; Resources=1; Fingerprint=052273
    // Broiler-Human:        PENDING
    internal abstract bool InLeapYear(CalendarSystemYear year);

    /// <summary>Whether a month code is one of Table 3's beyond <c>M01</c> to <c>M12</c> for this calendar.</summary>
    internal virtual bool IsAdditionalMonthCode(MonthCode monthCode) => false;

    /// <summary>The months between a fixed origin and the first month of a year, for balancing months into years.</summary>
    // Broiler-AI:           Origin=Ported; Spec=Intl era and month code proposal 4.1 via Broiler.VM JsCalendarSystem.MonthsBefore; IP=Low; Security=Low; Resources=1; Fingerprint=2E17F4
    // Broiler-Human:        PENDING
    internal virtual long MonthsBefore(long year) => 12 * year;

    /// <summary>The era and era year of a date (the era proposal's 4.1.9 and 4.1.10).</summary>
    // Broiler-AI:           Origin=Ported; Spec=Intl era and month code proposal 4.1.10, Table 2 via Broiler.VM JsCalendarSystem.EraOf; IP=Low; Security=Low; Resources=1; Fingerprint=05A6DD
    // Broiler-Human:        PENDING
    internal virtual (string? Era, long? EraYear) EraOf(long year, long epochDays) => (null, null);

    /// <summary>
    /// CalendarDateArithmeticYearForEraYear (the era proposal's 4.1.12) after CanonicalizeEraInCalendar (4.1.2):
    /// the arithmetic year, or <see langword="null"/> when the era is not one of the calendar's.
    /// </summary>
    internal virtual long? YearForEra(string era, long eraYear) => null;

    /// <summary>The ISO reference year Table 6 states for the lunisolar calendars it covers; <see langword="null"/> elsewhere, and -1 for its dash.</summary>
    internal virtual int? TableReferenceYear(MonthCode monthCode, int day) => null;

    /// <summary>The year, ordinal month and day of a day since the epoch.</summary>
    // Broiler-AI:           Origin=Ported; Spec=Intl era and month code proposal 4.1 via Broiler.VM JsCalendarSystem.FromEpochDays; IP=Low; Security=Low; Resources=1; Fingerprint=9BEFAC
    // Broiler-Human:        PENDING
    internal (CalendarSystemYear Year, int Month, int Day) FromEpochDays(long epochDays)
    {
        var year = Year(YearOf(epochDays));
        var (month, day) = year.MonthDay((int)(epochDays - year.Start) + 1);
        return (year, month, day);
    }

    /// <summary>The month code of an ordinal month (the era proposal's 4.1.14, [[MonthCode]]).</summary>
    // Broiler-AI:           Origin=Ported; Spec=Intl era and month code proposal 4.1.14 via Broiler.VM JsCalendarSystem.MonthCodeOf; IP=Low; Security=Low; Resources=1; Fingerprint=F718A1
    // Broiler-Human:        PENDING
    internal static MonthCode MonthCodeOf(CalendarSystemYear year, int month)
    {
        var leap = year.LeapMonth;
        if (leap == 0 || month < leap)
            return new MonthCode(month, false);

        return month == leap ? new MonthCode(month - 1, true) : new MonthCode(month - 1, false);
    }

    /// <summary>MonthCodeToOrdinal (the era proposal's 4.1.7), or 0 where the year does not contain the code (4.1.5).</summary>
    // Broiler-AI:           Origin=Ported; Spec=Intl era and month code proposal 4.1.7 via Broiler.VM JsCalendarSystem.Ordinal; IP=Low; Security=Low; Resources=1; Fingerprint=D99B1A
    // Broiler-Human:        PENDING
    internal static int Ordinal(CalendarSystemYear year, MonthCode monthCode)
    {
        var number = monthCode.Number;
        var leap = year.LeapMonth;
        if (monthCode.IsLeapMonth)
            return leap != 0 && leap - 1 == number ? leap : 0;

        var ordinal = leap != 0 && number >= leap ? number + 1 : number;
        return ordinal <= year.Months ? ordinal : 0;
    }

    /// <summary>
    /// A table-backed calendar's year from the shared sources (JSD-0076): the table the engine hands in inside
    /// its range, and ICU4X's approximation, the civil tabular year or the 33-year rule outside it.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The year is beyond the sources' bound. Every caller keeps it within, as Broiler.VM's do: no date within the
    /// ISO limits lies in a year beyond it, and each operation answers <see cref="CalendarStatus.OutOfRange"/>
    /// before it would ask for one.
    /// </exception>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0076, Intl era and month code proposal Table 1 via Broiler.VM JsCalendarSystem.SharedYear; IP=Low; Security=Low; Resources=1; Fingerprint=84072B
    // Broiler-Human:        PENDING
    protected static CalendarSystemYear SharedYear(ICalendarTables tables, TableCalendar calendar, long year) =>
        CalendarYears.GetYear(tables, calendar, year, out var result) == TimeStatus.Completed
            ? CalendarSystemYear.Of(result)
            : throw new InvalidOperationException("The year is beyond the calendar year sources' bound, which every caller keeps.");

    /// <summary>The year an estimate names, moved until it contains the day.</summary>
    // Broiler-AI:           Origin=Ported; Spec=Intl era and month code proposal 4.1 via Broiler.VM JsCalendarSystem.AdjustYear; IP=Low; Security=Low; Resources=1; Fingerprint=D1467E
    // Broiler-Human:        PENDING
    protected long AdjustYear(long estimate, long epochDays)
    {
        var year = estimate;
        while (Year(year).Start > epochDays)
            year--;

        while (Year(year + 1).Start <= epochDays)
            year++;

        return year;
    }
}
