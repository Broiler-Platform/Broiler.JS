// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   7
// Annotated:        7/7
// Exempt:           1
// Human-reviewed:   0/7
// IP risk:          Low
// Security risk:    Low
// Criteria:         0/0
// Resource impact:  1/10 max
// Unverified:       7
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using Broiler.ECMAScript.Time;

namespace Broiler.ECMAScript.Temporal;

/// <summary>
/// The Indian national calendar, <c>indian</c>: Saka years beginning on 22 March, or 21 March in a Gregorian
/// leap year, of the ISO year 78 later; Chaitra has 30 days, or 31 in a leap year. Ported from Broiler.VM's
/// <c>JsIndianCalendar</c>.
/// </summary>
// Broiler-AI:           Origin=Ported; Spec=Intl era and month code proposal Table 1 via Broiler.VM JsIndianCalendar; IP=Low; Security=Low; Resources=1; Fingerprint=25DDC3
// Broiler-Human:        PENDING
internal sealed class IndianCalendarSystem : CalendarSystem
{
    // Broiler-AI:           Origin=Ported; Spec=Intl era and month code proposal Table 1 via Broiler.VM JsIndianCalendar..ctor; IP=Low; Security=Low; Resources=1; Fingerprint=F9CF0F
    // Broiler-Human:        PENDING
    internal IndianCalendarSystem()
        : base(CalendarId.Indian)
    {
    }

    internal override bool HasEras => true;

    // Broiler-AI:           Origin=Ported; Spec=Intl era and month code proposal Table 1 via Broiler.VM JsIndianCalendar.Year; IP=Low; Security=Low; Resources=1; Fingerprint=508EEB
    // Broiler-Human:        PENDING
    internal override CalendarSystemYear Year(long year)
    {
        var iso = year + 78;
        var leap = IsoCalendarMath.IsLeapYear(iso);
        ReadOnlySpan<int> lengths = [leap ? 31 : 30, 31, 31, 31, 31, 31, 30, 30, 30, 30, 30, 30];
        return new CalendarSystemYear(year, IsoCalendarMath.EpochDays(iso, 3, leap ? 21 : 22), CalendarSystemYear.FromLengths(lengths));
    }

    // Broiler-AI:           Origin=Ported; Spec=Intl era and month code proposal Table 1 via Broiler.VM JsIndianCalendar.YearOf; IP=Low; Security=Low; Resources=1; Fingerprint=6628AC
    // Broiler-Human:        PENDING
    internal override long YearOf(long epochDays) => AdjustYear(IsoCalendarMath.FromEpochDays(epochDays).Year - 78, epochDays);

    // Broiler-AI:           Origin=Ported; Spec=Intl era and month code proposal Table 1 via Broiler.VM JsIndianCalendar.InLeapYear; IP=Low; Security=Low; Resources=1; Fingerprint=28D322
    // Broiler-Human:        PENDING
    internal override bool InLeapYear(CalendarSystemYear year) => year.Days == 366;

    // Broiler-AI:           Origin=Ported; Spec=Intl era and month code proposal 4.1.10, Table 2 via Broiler.VM JsIndianCalendar.EraOf; IP=Low; Security=Low; Resources=1; Fingerprint=11003C
    // Broiler-Human:        PENDING
    internal override (string? Era, long? EraYear) EraOf(long year, long epochDays) => ("shaka", year);

    // Broiler-AI:           Origin=Ported; Spec=Intl era and month code proposal 4.1.12, Table 2 via Broiler.VM JsIndianCalendar.YearForEra; IP=Low; Security=Low; Resources=1; Fingerprint=EAFE18
    // Broiler-Human:        PENDING
    internal override long? YearForEra(string era, long eraYear) => era == "shaka" ? eraYear : null;
}
