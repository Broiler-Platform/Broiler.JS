// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   8
// Annotated:        8/8
// Exempt:           2
// Human-reviewed:   0/8
// IP risk:          Low
// Security risk:    Low
// Criteria:         0/0
// Resource impact:  1/10 max
// Unverified:       8
//
// GENERATED - DO NOT EDIT MANUALLY

using Broiler.ECMAScript.Time;

namespace Broiler.ECMAScript.Temporal;

/// <summary>
/// The Persian (Solar Hijri) calendar, <c>persian</c>: six months of 31 days, five of 30 and Esfand of 29, or 30
/// in a leap year, which the 33-year rule decides, corrected by the years the calendar tables list (Calendrical
/// Calculations, ch. 15). Its years are SJL-8's <see cref="CalendarYears"/>; the arithmetic over them is ported
/// from Broiler.VM's <c>JsPersianCalendar</c>.
/// </summary>
// Broiler-AI:           Origin=Ported; Spec=Calendrical Calculations ch. 15 via Broiler.VM JsPersianCalendar; IP=Low; Security=Low; Resources=1; Fingerprint=C38840
// Broiler-Human:        PENDING
internal sealed class PersianCalendarSystem : CalendarSystem
{
    // R.D. 226896, Julian 622-03-19, the first day of 1 AP, in days since 1970-01-01.
    // Broiler-AI:           Origin=Ported; Spec=Calendrical Calculations ch. 15 via Broiler.VM JsPersianCalendar.Epoch; IP=Low; Security=Low; Resources=1; Fingerprint=970A38
    // Broiler-Human:        PENDING
    private const long Epoch = 226896 - RataDieOfEpoch;

    private readonly ICalendarTables tables;

    // Broiler-AI:           Origin=Ported; Spec=Calendrical Calculations ch. 15 via Broiler.VM JsPersianCalendar..ctor; IP=Low; Security=Low; Resources=1; Fingerprint=03C59F
    // Broiler-Human:        PENDING
    internal PersianCalendarSystem(ICalendarTables tables)
        : base(CalendarId.Persian)
    {
        this.tables = tables;
    }

    internal override bool HasEras => true;

    // Broiler-AI:           Origin=Ported; Spec=Calendrical Calculations ch. 15 via Broiler.VM JsPersianCalendar.Year; IP=Low; Security=Low; Resources=1; Fingerprint=FCFB0F
    // Broiler-Human:        PENDING
    internal override CalendarSystemYear Year(long year) => SharedYear(tables, TableCalendar.Persian, year);

    // Broiler-AI:           Origin=Ported; Spec=Calendrical Calculations ch. 15 via Broiler.VM JsPersianCalendar.YearOf; IP=Low; Security=Low; Resources=1; Fingerprint=4FF3D0
    // Broiler-Human:        PENDING
    internal override long YearOf(long epochDays) =>
        AdjustYear(1 + EcmaTime.FloorDivide((33 * (epochDays - Epoch + 1)) + 3, 12053), epochDays);

    // Broiler-AI:           Origin=Ported; Spec=Calendrical Calculations ch. 15 via Broiler.VM JsPersianCalendar.InLeapYear; IP=Low; Security=Low; Resources=1; Fingerprint=28D322
    // Broiler-Human:        PENDING
    internal override bool InLeapYear(CalendarSystemYear year) => year.Days == 366;

    // Broiler-AI:           Origin=Ported; Spec=Intl era and month code proposal 4.1.10, Table 2 via Broiler.VM JsPersianCalendar.EraOf; IP=Low; Security=Low; Resources=1; Fingerprint=8C64D5
    // Broiler-Human:        PENDING
    internal override (string? Era, long? EraYear) EraOf(long year, long epochDays) => ("ap", year);

    // Broiler-AI:           Origin=Ported; Spec=Intl era and month code proposal 4.1.12, Table 2 via Broiler.VM JsPersianCalendar.YearForEra; IP=Low; Security=Low; Resources=1; Fingerprint=56AC9C
    // Broiler-Human:        PENDING
    internal override long? YearForEra(string era, long eraYear) => era == "ap" ? eraYear : null;
}
