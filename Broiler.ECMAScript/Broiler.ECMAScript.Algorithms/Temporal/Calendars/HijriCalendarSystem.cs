// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   9
// Annotated:        9/9
// Exempt:           3
// Human-reviewed:   0/9
// IP risk:          Low
// Security risk:    Low
// Criteria:         0/0
// Resource impact:  1/10 max
// Unverified:       9
//
// GENERATED - DO NOT EDIT MANUALLY

using Broiler.ECMAScript.Time;

namespace Broiler.ECMAScript.Temporal;

/// <summary>
/// The Hijri calendars, ported from Broiler.VM's <c>JsHijriCalendar</c>: the tabular one with leap years 2, 5,
/// 7, 10, 13, 16, 18, 21, 24, 26 and 29 of each 30, from the civil (Friday) epoch, <c>islamic-civil</c>, or the
/// astronomical (Thursday) one, <c>islamic-tbla</c>; and Umm al-Qura, <c>islamic-umalqura</c>, whose years are
/// SJL-8's <see cref="CalendarYears"/>: KACST's from the calendar tables, and the civil tabular ones outside them.
/// </summary>
// Broiler-AI:           Origin=Ported; Spec=Calendrical Calculations ch. 6, Intl era and month code proposal Table 1 via Broiler.VM JsHijriCalendar; IP=Low; Security=Low; Resources=1; Fingerprint=6FBF2B
// Broiler-Human:        PENDING
internal sealed class HijriCalendarSystem : CalendarSystem
{
    // R.D. 227015, Julian 622-07-16, a Friday: the civil epoch, in days since 1970-01-01.
    // Broiler-AI:           Origin=Ported; Spec=Calendrical Calculations ch. 6, Intl era and month code proposal Table 1 via Broiler.VM JsHijriCalendar.CivilEpoch; IP=Low; Security=Low; Resources=1; Fingerprint=A395E1
    // Broiler-Human:        PENDING
    private const long CivilEpoch = 227015 - RataDieOfEpoch;

    private readonly long epoch;

    private readonly ICalendarTables? ummAlQura;

    /// <param name="id">The calendar.</param>
    /// <param name="thursday">Whether the epoch is the astronomical one, a day before the civil.</param>
    /// <param name="ummAlQura">The tables, for <c>islamic-umalqura</c>; <see langword="null"/> for a tabular calendar.</param>
    // Broiler-AI:           Origin=Ported; Spec=Calendrical Calculations ch. 6, Intl era and month code proposal Table 1 via Broiler.VM JsHijriCalendar..ctor; IP=Low; Security=Low; Resources=1; Fingerprint=3D5524
    // Broiler-Human:        PENDING
    internal HijriCalendarSystem(CalendarId id, bool thursday, ICalendarTables? ummAlQura = null)
        : base(id)
    {
        epoch = CivilEpoch - (thursday ? 1 : 0);
        this.ummAlQura = ummAlQura;
    }

    internal override bool HasEras => true;

    // Broiler-AI:           Origin=Ported; Spec=Calendrical Calculations ch. 6, Intl era and month code proposal Table 1 via Broiler.VM JsHijriCalendar.Year; IP=Low; Security=Low; Resources=1; Fingerprint=B98601
    // Broiler-Human:        PENDING
    internal override CalendarSystemYear Year(long year)
    {
        if (ummAlQura is not null)
            return SharedYear(ummAlQura, TableCalendar.IslamicUmmAlQura, year);

        var lengths = new int[12];
        for (var month = 1; month <= 12; month++)
            lengths[month - 1] = month % 2 == 1 ? 30 : 29;

        if ((((14 + (11 * year)) % 30) + 30) % 30 < 11)
            lengths[11] = 30;

        return new CalendarSystemYear(year, TabularNewYear(epoch, year), CalendarSystemYear.FromLengths(lengths));
    }

    // Broiler-AI:           Origin=Ported; Spec=Calendrical Calculations ch. 6, Intl era and month code proposal Table 1 via Broiler.VM JsHijriCalendar.YearOf; IP=Low; Security=Low; Resources=1; Fingerprint=8D2770
    // Broiler-Human:        PENDING
    internal override long YearOf(long epochDays) =>
        AdjustYear(EcmaTime.FloorDivide((30 * (epochDays - epoch)) + 10646, 10631), epochDays);

    // Broiler-AI:           Origin=Ported; Spec=Calendrical Calculations ch. 6, Intl era and month code proposal Table 1 via Broiler.VM JsHijriCalendar.InLeapYear; IP=Low; Security=Low; Resources=1; Fingerprint=9CC507
    // Broiler-Human:        PENDING
    internal override bool InLeapYear(CalendarSystemYear year) => year.Days == 355;

    // Broiler-AI:           Origin=Ported; Spec=Intl era and month code proposal 4.1.10, Table 2 via Broiler.VM JsHijriCalendar.EraOf; IP=Low; Security=Low; Resources=1; Fingerprint=1EE6E0
    // Broiler-Human:        PENDING
    internal override (string? Era, long? EraYear) EraOf(long year, long epochDays) =>
        year >= 1 ? ("ah", year) : ("bh", 1 - year);

    // Broiler-AI:           Origin=Ported; Spec=Intl era and month code proposal 4.1.12, Table 2 via Broiler.VM JsHijriCalendar.YearForEra; IP=Low; Security=Low; Resources=1; Fingerprint=CBE1BB
    // Broiler-Human:        PENDING
    internal override long? YearForEra(string era, long eraYear) => era switch
    {
        "ah" => eraYear,
        "bh" => 1 - eraYear,
        _ => null,
    };

    // The tabular new year of a year from an epoch.
    // Broiler-AI:           Origin=Ported; Spec=Calendrical Calculations ch. 6, Intl era and month code proposal Table 1 via Broiler.VM JsHijriCalendar.TabularNewYear; IP=Low; Security=Low; Resources=1; Fingerprint=131AF1
    // Broiler-Human:        PENDING
    private static long TabularNewYear(long epoch, long year) =>
        epoch + ((year - 1) * 354) + EcmaTime.FloorDivide(3 + (11 * year), 30);
}
