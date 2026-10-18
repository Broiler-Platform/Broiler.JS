// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   10
// Annotated:        10/10
// Exempt:           4
// Human-reviewed:   0/10
// IP risk:          Low
// Security risk:    Low
// Criteria:         0/0
// Resource impact:  1/10 max
// Unverified:       10
//
// GENERATED - DO NOT EDIT MANUALLY

using Broiler.ECMAScript.Time;

namespace Broiler.ECMAScript.Temporal;

/// <summary>
/// The Coptic and Ethiopian calendars, <c>coptic</c>, <c>ethiopic</c> and <c>ethioaa</c>: twelve months of 30
/// days and a thirteenth of five, or six in the year before a year divisible by four (Calendrical Calculations,
/// ch. 4), ported from Broiler.VM's <c>JsCopticCalendar</c>.
/// </summary>
// Broiler-AI:           Origin=Ported; Spec=Calendrical Calculations ch. 4, Intl era and month code proposal Table 2 via Broiler.VM JsCopticCalendar; IP=Low; Security=Low; Resources=1; Fingerprint=F39DBA
// Broiler-Human:        PENDING
internal sealed class CopticCalendarSystem : CalendarSystem
{
    // The first day of year 1 of the reckoning, in days since 1970-01-01.
    private readonly long epoch;

    // What the calendar adds to the reckoning's year: 5500 for ethioaa.
    private readonly long yearShift;

    /// <param name="id">The calendar.</param>
    /// <param name="epochRataDie">The R.D. of the first day of year 1 of the reckoning.</param>
    /// <param name="yearShift">What the calendar adds to the reckoning's year: 5500 for <c>ethioaa</c>.</param>
    // Broiler-AI:           Origin=Ported; Spec=Calendrical Calculations ch. 4, Intl era and month code proposal Table 2 via Broiler.VM JsCopticCalendar..ctor; IP=Low; Security=Low; Resources=1; Fingerprint=0EF029
    // Broiler-Human:        PENDING
    internal CopticCalendarSystem(CalendarId id, long epochRataDie, long yearShift)
        : base(id)
    {
        epoch = epochRataDie - RataDieOfEpoch;
        this.yearShift = yearShift;
    }

    internal override bool HasEras => true;

    internal override double MeanMonths => 13;

    // Broiler-AI:           Origin=Ported; Spec=Calendrical Calculations ch. 4, Intl era and month code proposal Table 2 via Broiler.VM JsCopticCalendar.IsAdditionalMonthCode; IP=Low; Security=Low; Resources=1; Fingerprint=E9FADC
    // Broiler-Human:        PENDING
    internal override bool IsAdditionalMonthCode(MonthCode monthCode) => monthCode == new MonthCode(13, false);

    // Broiler-AI:           Origin=Ported; Spec=Calendrical Calculations ch. 4, Intl era and month code proposal Table 2 via Broiler.VM JsCopticCalendar.MonthsBefore; IP=Low; Security=Low; Resources=1; Fingerprint=9E57C4
    // Broiler-Human:        PENDING
    internal override long MonthsBefore(long year) => 13 * year;

    // Broiler-AI:           Origin=Ported; Spec=Calendrical Calculations ch. 4, Intl era and month code proposal Table 2 via Broiler.VM JsCopticCalendar.Year; IP=Low; Security=Low; Resources=1; Fingerprint=2AFF6C
    // Broiler-Human:        PENDING
    internal override CalendarSystemYear Year(long year)
    {
        var days = (int)(NewYear(year + 1) - NewYear(year));
        var offsets = new int[14];
        for (var month = 1; month <= 13; month++)
            offsets[month] = month == 13 ? days : 30 * month;

        return new CalendarSystemYear(year, NewYear(year), offsets);
    }

    // Broiler-AI:           Origin=Ported; Spec=Calendrical Calculations ch. 4, Intl era and month code proposal Table 2 via Broiler.VM JsCopticCalendar.YearOf; IP=Low; Security=Low; Resources=1; Fingerprint=417B72
    // Broiler-Human:        PENDING
    internal override long YearOf(long epochDays) =>
        EcmaTime.FloorDivide((4 * (epochDays - epoch)) + 1463, 1461) + yearShift;

    // Broiler-AI:           Origin=Ported; Spec=Calendrical Calculations ch. 4, Intl era and month code proposal Table 2 via Broiler.VM JsCopticCalendar.InLeapYear; IP=Low; Security=Low; Resources=1; Fingerprint=28D322
    // Broiler-Human:        PENDING
    internal override bool InLeapYear(CalendarSystemYear year) => year.Days == 366;

    // Broiler-AI:           Origin=Ported; Spec=Intl era and month code proposal 4.1.10, Table 2 via Broiler.VM JsCopticCalendar.EraOf; IP=Low; Security=Low; Resources=1; Fingerprint=F05E4A
    // Broiler-Human:        PENDING
    internal override (string? Era, long? EraYear) EraOf(long year, long epochDays) => Id switch
    {
        CalendarId.Coptic => ("am", year),
        CalendarId.Ethioaa => ("aa", year),
        _ => year >= 1 ? ("am", year) : ("aa", year + 5500),
    };

    // Broiler-AI:           Origin=Ported; Spec=Intl era and month code proposal 4.1.12, Table 2 via Broiler.VM JsCopticCalendar.YearForEra; IP=Low; Security=Low; Resources=1; Fingerprint=B97027
    // Broiler-Human:        PENDING
    internal override long? YearForEra(string era, long eraYear) => (Id, era) switch
    {
        (CalendarId.Coptic, "am") => eraYear,
        (CalendarId.Ethioaa, "aa") => eraYear,
        (CalendarId.Ethiopic, "am") => eraYear,
        (CalendarId.Ethiopic, "aa") => -5499 + eraYear - 1,
        _ => null,
    };

    // Broiler-AI:           Origin=Ported; Spec=Calendrical Calculations ch. 4, Intl era and month code proposal Table 2 via Broiler.VM JsCopticCalendar.NewYear; IP=Low; Security=Low; Resources=1; Fingerprint=EC06A1
    // Broiler-Human:        PENDING
    private long NewYear(long year)
    {
        var y = year - yearShift;
        return epoch + (365 * (y - 1)) + EcmaTime.FloorDivide(y, 4);
    }
}
