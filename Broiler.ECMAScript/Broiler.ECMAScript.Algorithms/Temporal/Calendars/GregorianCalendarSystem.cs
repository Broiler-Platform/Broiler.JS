// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   7
// Annotated:        7/7
// Exempt:           2
// Human-reviewed:   0/7
// IP risk:          Low
// Security risk:    Low
// Criteria:         0/0
// Resource impact:  1/10 max
// Unverified:       7
//
// GENERATED - DO NOT EDIT MANUALLY

using Broiler.ECMAScript.Time;

namespace Broiler.ECMAScript.Temporal;

/// <summary>
/// The calendars whose months and days are ISO 8601's: <c>gregory</c>, <c>buddhist</c>, <c>roc</c> and
/// <c>japanese</c>, each with its own year numbering and eras (Table 2), ported from Broiler.VM's
/// <c>JsGregorianCalendar</c>. Japanese eras run from Meiji 6 (1873), by the date each began; Gregorian years
/// before.
/// </summary>
// Broiler-AI:           Origin=Ported; Spec=Intl era and month code proposal Table 2 via Broiler.VM JsGregorianCalendar; IP=Low; Security=Low; Resources=1; Fingerprint=236AC3
// Broiler-Human:        PENDING
internal sealed class GregorianCalendarSystem : CalendarSystem
{
    // What the calendar's year adds to give the ISO year: -543 for buddhist, 1911 for roc.
    private readonly long isoOffset;

    // Broiler-AI:           Origin=Ported; Spec=Intl era and month code proposal Table 2 via Broiler.VM JsGregorianCalendar..ctor; IP=Low; Security=Low; Resources=1; Fingerprint=D5C3FD
    // Broiler-Human:        PENDING
    internal GregorianCalendarSystem(CalendarId id, long isoOffset)
        : base(id)
    {
        this.isoOffset = isoOffset;
    }

    internal override bool HasEras => true;

    // Broiler-AI:           Origin=Ported; Spec=Intl era and month code proposal Table 2 via Broiler.VM JsGregorianCalendar.Year; IP=Low; Security=Low; Resources=1; Fingerprint=37838C
    // Broiler-Human:        PENDING
    internal override CalendarSystemYear Year(long year)
    {
        var iso = year + isoOffset;
        var lengths = new int[12];
        for (var month = 1; month <= 12; month++)
            lengths[month - 1] = IsoCalendarMath.DaysInMonth(iso, month);

        return new CalendarSystemYear(year, IsoCalendarMath.EpochDays(iso, 1, 1), CalendarSystemYear.FromLengths(lengths));
    }

    // Broiler-AI:           Origin=Ported; Spec=Intl era and month code proposal Table 2 via Broiler.VM JsGregorianCalendar.YearOf; IP=Low; Security=Low; Resources=1; Fingerprint=136932
    // Broiler-Human:        PENDING
    internal override long YearOf(long epochDays) => IsoCalendarMath.FromEpochDays(epochDays).Year - isoOffset;

    // Broiler-AI:           Origin=Ported; Spec=Intl era and month code proposal Table 2 via Broiler.VM JsGregorianCalendar.InLeapYear; IP=Low; Security=Low; Resources=1; Fingerprint=28D322
    // Broiler-Human:        PENDING
    internal override bool InLeapYear(CalendarSystemYear year) => year.Days == 366;

    // Broiler-AI:           Origin=Ported; Spec=Intl era and month code proposal 4.1.10, Table 2 via Broiler.VM JsGregorianCalendar.EraOf; IP=Low; Security=Low; Resources=1; Fingerprint=FE50CF
    // Broiler-Human:        PENDING
    internal override (string? Era, long? EraYear) EraOf(long year, long epochDays)
    {
        switch (Id)
        {
            case CalendarId.Buddhist:
                return ("be", year);
            case CalendarId.Roc:
                return year >= 1 ? ("roc", year) : ("broc", 1 - year);
            case CalendarId.Japanese:
                // THE MODERN ERAS by the date each began (Table 2's offsets), then Gregorian years.
                if (epochDays >= IsoCalendarMath.EpochDays(2019, 5, 1))
                    return ("reiwa", year - 2018);
                if (epochDays >= IsoCalendarMath.EpochDays(1989, 1, 8))
                    return ("heisei", year - 1988);
                if (epochDays >= IsoCalendarMath.EpochDays(1926, 12, 25))
                    return ("showa", year - 1925);
                if (epochDays >= IsoCalendarMath.EpochDays(1912, 7, 30))
                    return ("taisho", year - 1911);
                if (epochDays >= IsoCalendarMath.EpochDays(1873, 1, 1))
                    return ("meiji", year - 1867);

                goto default;
            default:
                return year >= 1 ? ("ce", year) : ("bce", 1 - year);
        }
    }

    // Broiler-AI:           Origin=Ported; Spec=Intl era and month code proposal 4.1.12, Table 2 via Broiler.VM JsGregorianCalendar.YearForEra; IP=Low; Security=Low; Resources=1; Fingerprint=C60B33
    // Broiler-Human:        PENDING
    internal override long? YearForEra(string era, long eraYear) => (Id, era) switch
    {
        (CalendarId.Buddhist, "be") => eraYear,
        (CalendarId.Roc, "roc") => eraYear,
        (CalendarId.Roc, "broc") => 1 - eraYear,
        (CalendarId.Japanese, "reiwa") => 2019 + eraYear - 1,
        (CalendarId.Japanese, "heisei") => 1989 + eraYear - 1,
        (CalendarId.Japanese, "showa") => 1926 + eraYear - 1,
        (CalendarId.Japanese, "taisho") => 1912 + eraYear - 1,
        (CalendarId.Japanese, "meiji") => 1868 + eraYear - 1,
        (CalendarId.Gregory or CalendarId.Japanese, "ce" or "ad") => eraYear,
        (CalendarId.Gregory or CalendarId.Japanese, "bce" or "bc") => 1 - eraYear,
        _ => null,
    };
}
