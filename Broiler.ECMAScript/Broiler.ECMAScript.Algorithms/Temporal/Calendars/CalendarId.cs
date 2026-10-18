// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   1
// Annotated:        1/1
// Exempt:           16
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
/// A calendar Temporal supports: the "Calendar Type" of every row of the Intl era and month code proposal's
/// Table 1 (its 1.1.1, which amends the Temporal proposal's AvailableCalendars, s12.1.2), at the era proposal's
/// <c>5833eae</c> (JSD-0078, decision D1). The two aliases Table 1 lists, <c>ethiopic-amete-alem</c> and
/// <c>islamicc</c>, are not members: <see cref="TemporalCalendars.Canonicalize"/> answers their canonical types.
/// </summary>
/// <remarks>
/// <see cref="TemporalCalendars.Identifier"/> gives each member's canonical string, the value of a Temporal
/// object's <c>calendarId</c>. No member stands for <c>islamic</c>, <c>islamic-rgsa</c>, <c>gregorian</c> or
/// <c>minguo</c>, which no row of Table 1 names (JSD-0078, decision D5; the correspondence's rows C2 and C3).
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=Temporal s12.1.2, Intl era and month code proposal 1.1.1 Table 1; IP=None; Security=None; Resources=0; Fingerprint=6B133F
// Broiler-Human:        PENDING
public enum CalendarId
{
    /// <summary><c>iso8601</c>: the ISO 8601 calendar, the Temporal proposal's own.</summary>
    Iso8601 = 0,

    /// <summary><c>buddhist</c>: the Thai solar calendar, ISO 8601's months with the Buddhist era's years.</summary>
    Buddhist = 1,

    /// <summary><c>chinese</c>: the Chinese lunisolar calendar, its years from the calendar tables.</summary>
    Chinese = 2,

    /// <summary><c>coptic</c>: the Coptic calendar.</summary>
    Coptic = 3,

    /// <summary><c>dangi</c>: the Korean lunisolar calendar, its years from the calendar tables.</summary>
    Dangi = 4,

    /// <summary><c>ethioaa</c>: the Ethiopian calendar counted in the Amete Alem era; <c>ethiopic-amete-alem</c> is its alias.</summary>
    Ethioaa = 5,

    /// <summary><c>ethiopic</c>: the Ethiopian calendar counted in the Amete Mihret era.</summary>
    Ethiopic = 6,

    /// <summary><c>gregory</c>: the Gregorian calendar with its two eras.</summary>
    Gregory = 7,

    /// <summary><c>hebrew</c>: the Hebrew calendar.</summary>
    Hebrew = 8,

    /// <summary><c>indian</c>: the Indian national calendar.</summary>
    Indian = 9,

    /// <summary><c>islamic-civil</c>: the tabular Hijri calendar from the civil (Friday) epoch; <c>islamicc</c> is its alias.</summary>
    IslamicCivil = 10,

    /// <summary><c>islamic-tbla</c>: the tabular Hijri calendar from the astronomical (Thursday) epoch.</summary>
    IslamicTbla = 11,

    /// <summary><c>islamic-umalqura</c>: the Umm al-Qura calendar, its years from the calendar tables.</summary>
    IslamicUmalqura = 12,

    /// <summary><c>japanese</c>: ISO 8601's months with the Japanese eras.</summary>
    Japanese = 13,

    /// <summary><c>persian</c>: the Solar Hijri calendar, its corrections from the calendar tables.</summary>
    Persian = 14,

    /// <summary><c>roc</c>: ISO 8601's months with the years of the Republic of China.</summary>
    Roc = 15,
}
