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

using System;
using Broiler.ECMAScript.Time;

namespace Broiler.ECMAScript.Temporal;

/// <summary>
/// The Chinese and Korean calendars, <c>chinese</c> and <c>dangi</c>: lunar months, a leap month inserted in a
/// year without a major solar term. Their years are SJL-8's <see cref="CalendarYears"/>: the published ones the
/// calendar tables hold, and ICU4X's mean-motion approximation outside them, as Table 1 allows. The arithmetic
/// over them, and Table 6's reference years, are ported from Broiler.VM's <c>JsEastAsianCalendar</c>.
/// </summary>
// Broiler-AI:           Origin=Ported; Spec=Intl era and month code proposal Table 1 and Table 6 via Broiler.VM JsEastAsianCalendar; IP=Low; Security=Low; Resources=1; Fingerprint=50FD62
// Broiler-Human:        PENDING
internal sealed class EastAsianCalendarSystem : CalendarSystem
{
    // The mean synodic month in days.
    // Broiler-AI:           Origin=Ported; Spec=Intl era and month code proposal Table 1 and Table 6 via Broiler.VM JsEastAsianCalendar.MeanSynodicMonth; IP=Low; Security=Low; Resources=1; Fingerprint=FEF03A
    // Broiler-Human:        PENDING
    private const double MeanSynodicMonth = 29.530588853;

    private readonly bool korean;

    private readonly ICalendarTables tables;

    // The first day of the related ISO year 2000's year, the origin of the month count.
    private readonly long anchor;

    // Broiler-AI:           Origin=Ported; Spec=Intl era and month code proposal Table 1 and Table 6 via Broiler.VM JsEastAsianCalendar..ctor; IP=Low; Security=Low; Resources=1; Fingerprint=65FA28
    // Broiler-Human:        PENDING
    internal EastAsianCalendarSystem(CalendarId id, ICalendarTables tables)
        : base(id)
    {
        korean = id == CalendarId.Dangi;
        this.tables = tables;
        anchor = Year(2000).Start;
    }

    // Broiler-AI:           Origin=Ported; Spec=Intl era and month code proposal Table 1 and Table 6 via Broiler.VM JsEastAsianCalendar.MeanMonths; IP=Low; Security=Low; Resources=1; Fingerprint=DFB48C
    // Broiler-Human:        PENDING
    internal override double MeanMonths => 365.2425 / MeanSynodicMonth;

    internal override bool MonthsBeforeReadsYears => true;

    // Broiler-AI:           Origin=Ported; Spec=Intl era and month code proposal Table 1 and Table 6 via Broiler.VM JsEastAsianCalendar.IsAdditionalMonthCode; IP=Low; Security=Low; Resources=1; Fingerprint=EF3F3C
    // Broiler-Human:        PENDING
    internal override bool IsAdditionalMonthCode(MonthCode monthCode) => monthCode.IsLeapMonth && monthCode.Number is >= 1 and <= 12;

    // Broiler-AI:           Origin=Ported; Spec=Intl era and month code proposal Table 1 and Table 6 via Broiler.VM JsEastAsianCalendar.MonthsBefore; IP=Low; Security=Low; Resources=1; Fingerprint=26019C
    // Broiler-Human:        PENDING
    internal override long MonthsBefore(long year) =>
        (long)Math.Round((Year(year).Start - anchor) / MeanSynodicMonth, MidpointRounding.AwayFromZero);

    // Broiler-AI:           Origin=Ported; Spec=Intl era and month code proposal Table 1 and Table 6 via Broiler.VM JsEastAsianCalendar.Year; IP=Low; Security=Low; Resources=1; Fingerprint=311ED6
    // Broiler-Human:        PENDING
    internal override CalendarSystemYear Year(long year) =>
        SharedYear(tables, korean ? TableCalendar.Dangi : TableCalendar.Chinese, year);

    // Broiler-AI:           Origin=Ported; Spec=Intl era and month code proposal Table 1 and Table 6 via Broiler.VM JsEastAsianCalendar.YearOf; IP=Low; Security=Low; Resources=1; Fingerprint=4E7249
    // Broiler-Human:        PENDING
    internal override long YearOf(long epochDays)
    {
        var related = IsoCalendarMath.FromEpochDays(epochDays).Year;
        return Year(related).Start > epochDays ? related - 1 : related;
    }

    // Broiler-AI:           Origin=Ported; Spec=Intl era and month code proposal Table 1 and Table 6 via Broiler.VM JsEastAsianCalendar.InLeapYear; IP=Low; Security=Low; Resources=1; Fingerprint=E74E87
    // Broiler-Human:        PENDING
    internal override bool InLeapYear(CalendarSystemYear year) => year.Months == 13;

    // Broiler-AI:           Origin=Ported; Spec=Intl era and month code proposal Table 6 via Broiler.VM JsEastAsianCalendar.TableReferenceYear; IP=Low; Security=Low; Resources=1; Fingerprint=E82DEE
    // Broiler-Human:        PENDING
    internal override int? TableReferenceYear(MonthCode monthCode, int day)
    {
        // TABLE 6 (the era proposal's ad5028b): the reference year for days 1 to 29, then for day 30. -1 IS ITS DASH.
        var (common, thirtieth) = (monthCode.Number, monthCode.IsLeapMonth) switch
        {
            (1, false) => (1972, 1970),
            (1, true) => (-1, -1),
            (2, false) => (1972, 1972),
            (2, true) => (1947, -1),
            (3, false) => (1972, korean ? 1968 : 1966),
            (3, true) => (1966, 1955),
            (4, false) => (1972, 1970),
            (4, true) => (1963, 1944),
            (5, false) => (1972, 1972),
            (5, true) => (1971, 1952),
            (6, false) => (1972, 1971),
            (6, true) => (1960, 1941),
            (7, false) => (1972, 1972),
            (7, true) => (1968, 1938),
            (8, false) => (1972, 1971),
            (8, true) => (1957, -1),
            (9, false) => (1972, 1972),
            (9, true) => (2014, -1),
            (10, false) => (1972, 1972),
            (10, true) => (1984, -1),
            (11, false) => (1972, 1970),
            (11, true) => (day <= 10 ? 2033 : 2034, -1),
            (12, false) => (1972, 1972),
            _ => (-1, -1),
        };

        return day >= 30 ? thirtieth : common;
    }
}
