// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   14
// Annotated:        14/14
// Exempt:           2
// Human-reviewed:   0/14
// IP risk:          Low
// Security risk:    Low
// Criteria:         0/0
// Resource impact:  1/10 max
// Unverified:       14
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using Broiler.ECMAScript.Time;

namespace Broiler.ECMAScript.Temporal;

/// <summary>
/// The Hebrew calendar, <c>hebrew</c>, civil year from Tishrei: the molad of Tishrei and the four postponements
/// decide the new year, and Adar I is inserted after Shevat in years 3, 6, 8, 11, 14, 17 and 19 of each 19
/// (Calendrical Calculations, ch. 8). Ported from Broiler.VM's <c>JsHebrewCalendar</c>.
/// </summary>
// Broiler-AI:           Origin=Ported; Spec=Calendrical Calculations ch. 8 via Broiler.VM JsHebrewCalendar; IP=Low; Security=Low; Resources=1; Fingerprint=84891E
// Broiler-Human:        PENDING
internal sealed class HebrewCalendarSystem : CalendarSystem
{
    // R.D. -1373427, Julian 3761 BCE 10-07: the epoch, in days since 1970-01-01.
    // Broiler-AI:           Origin=Ported; Spec=Calendrical Calculations ch. 8 via Broiler.VM JsHebrewCalendar.Epoch; IP=Low; Security=Low; Resources=1; Fingerprint=0E196E
    // Broiler-Human:        PENDING
    private const long Epoch = -1373427 - RataDieOfEpoch;

    // Broiler-AI:           Origin=Ported; Spec=Calendrical Calculations ch. 8 via Broiler.VM JsHebrewCalendar..ctor; IP=Low; Security=Low; Resources=1; Fingerprint=81C81D
    // Broiler-Human:        PENDING
    internal HebrewCalendarSystem()
        : base(CalendarId.Hebrew)
    {
    }

    internal override bool HasEras => true;

    internal override bool SkipBackward => false;

    // Broiler-AI:           Origin=Ported; Spec=Calendrical Calculations ch. 8 via Broiler.VM JsHebrewCalendar.MeanMonths; IP=Low; Security=Low; Resources=1; Fingerprint=961EA9
    // Broiler-Human:        PENDING
    internal override double MeanMonths => 235.0 / 19;

    // Broiler-AI:           Origin=Ported; Spec=Calendrical Calculations ch. 8 via Broiler.VM JsHebrewCalendar.IsAdditionalMonthCode; IP=Low; Security=Low; Resources=1; Fingerprint=BDC023
    // Broiler-Human:        PENDING
    internal override bool IsAdditionalMonthCode(MonthCode monthCode) => monthCode == new MonthCode(5, true);

    // Broiler-AI:           Origin=Ported; Spec=Calendrical Calculations ch. 8 via Broiler.VM JsHebrewCalendar.MonthsBefore; IP=Low; Security=Low; Resources=1; Fingerprint=1EFA59
    // Broiler-Human:        PENDING
    internal override long MonthsBefore(long year) => EcmaTime.FloorDivide((235 * year) - 234, 19);

    // Broiler-AI:           Origin=Ported; Spec=Calendrical Calculations ch. 8 via Broiler.VM JsHebrewCalendar.Year; IP=Low; Security=Low; Resources=1; Fingerprint=002F7E
    // Broiler-Human:        PENDING
    internal override CalendarSystemYear Year(long year)
    {
        var start = NewYear(year);
        var days = (int)(NewYear(year + 1) - start);
        var heshvan = days % 10 == 5 ? 30 : 29;
        var kislev = days % 10 == 3 ? 29 : 30;

        if (IsLeap(year))
        {
            ReadOnlySpan<int> leap = [30, heshvan, kislev, 29, 30, 30, 29, 30, 29, 30, 29, 30, 29];
            return new CalendarSystemYear(year, start, CalendarSystemYear.FromLengths(leap), leapMonth: 6);
        }

        ReadOnlySpan<int> common = [30, heshvan, kislev, 29, 30, 29, 30, 29, 30, 29, 30, 29];
        return new CalendarSystemYear(year, start, CalendarSystemYear.FromLengths(common));
    }

    // Broiler-AI:           Origin=Ported; Spec=Calendrical Calculations ch. 8 via Broiler.VM JsHebrewCalendar.YearOf; IP=Low; Security=Low; Resources=1; Fingerprint=27C242
    // Broiler-Human:        PENDING
    internal override long YearOf(long epochDays) =>
        AdjustYear(EcmaTime.FloorDivide((epochDays - Epoch) * 98496, 35975351) + 1, epochDays);

    // Broiler-AI:           Origin=Ported; Spec=Calendrical Calculations ch. 8 via Broiler.VM JsHebrewCalendar.InLeapYear; IP=Low; Security=Low; Resources=1; Fingerprint=E74E87
    // Broiler-Human:        PENDING
    internal override bool InLeapYear(CalendarSystemYear year) => year.Months == 13;

    // Broiler-AI:           Origin=Ported; Spec=Intl era and month code proposal 4.1.10, Table 2 via Broiler.VM JsHebrewCalendar.EraOf; IP=Low; Security=Low; Resources=1; Fingerprint=3C2953
    // Broiler-Human:        PENDING
    internal override (string? Era, long? EraYear) EraOf(long year, long epochDays) => ("am", year);

    // Broiler-AI:           Origin=Ported; Spec=Intl era and month code proposal 4.1.12, Table 2 via Broiler.VM JsHebrewCalendar.YearForEra; IP=Low; Security=Low; Resources=1; Fingerprint=5C8D27
    // Broiler-Human:        PENDING
    internal override long? YearForEra(string era, long eraYear) => era == "am" ? eraYear : null;

    // Broiler-AI:           Origin=Ported; Spec=Calendrical Calculations ch. 8 via Broiler.VM JsHebrewCalendar.ElapsedDays; IP=Low; Security=Low; Resources=1; Fingerprint=E42A32
    // Broiler-Human:        PENDING
    private static long ElapsedDays(long year)
    {
        var months = EcmaTime.FloorDivide((235 * year) - 234, 19);
        var parts = 12084 + (13753 * months);
        var days = (29 * months) + EcmaTime.FloorDivide(parts, 25920);
        return ((((3 * (days + 1)) % 7) + 7) % 7) < 3 ? days + 1 : days;
    }

    // Broiler-AI:           Origin=Ported; Spec=Calendrical Calculations ch. 8 via Broiler.VM JsHebrewCalendar.NewYear; IP=Low; Security=Low; Resources=1; Fingerprint=98A807
    // Broiler-Human:        PENDING
    private static long NewYear(long year)
    {
        var previous = ElapsedDays(year - 1);
        var current = ElapsedDays(year);
        var next = ElapsedDays(year + 1);
        var correction = next - current == 356 ? 2 : current - previous == 382 ? 1 : 0;
        return Epoch + current + correction;
    }

    // Broiler-AI:           Origin=Ported; Spec=Calendrical Calculations ch. 8 via Broiler.VM JsHebrewCalendar.IsLeap; IP=Low; Security=Low; Resources=1; Fingerprint=ED0B33
    // Broiler-Human:        PENDING
    private static bool IsLeap(long year) => ((((7 * year) + 1) % 19) + 19) % 19 < 7;
}
