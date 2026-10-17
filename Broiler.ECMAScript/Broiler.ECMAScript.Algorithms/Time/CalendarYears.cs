// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;

namespace Broiler.ECMAScript.Time;

/// <summary>
/// The calendars of the Intl era and month code proposal's Table 1 whose years come from published tables
/// (JSD-0076, the calendar tables; decision SJL-D-21): their years are the shared part, and their
/// arithmetic is each engine's until SJL-10.
/// </summary>
// Broiler-AI:           Origin=Derived; Spec=JSD-0076, Intl era and month code proposal Table 1; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
// Broiler-Human:        PENDING
public enum TableCalendar
{
    /// <summary><c>chinese</c>: its year is the related ISO year in which it begins.</summary>
    Chinese = 0,

    /// <summary><c>dangi</c>, the Korean calendar: its year is the related ISO year in which it begins.</summary>
    Dangi = 1,

    /// <summary><c>islamic-umalqura</c>: its year is the year of the Hijra (AH).</summary>
    IslamicUmmAlQura = 2,

    /// <summary><c>persian</c>, the Solar Hijri calendar: its year is the year of the Hijra (AP).</summary>
    Persian = 3,
}

/// <summary>
/// One year of a table-backed calendar: its arithmetic year, its first day, its months and their lengths,
/// and its leap month (JSD-0076: "a year here is its new year, its months and their lengths, and its leap
/// month").
/// </summary>
// Broiler-AI:           Origin=Derived; Spec=JSD-0076, Intl era and month code proposal Table 1 via Broiler.VM JsCalendarYear; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
// Broiler-Human:        PENDING
public readonly record struct CalendarYear
{
    // Each month's length less 29, two bits a month from the first month up.
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 via Broiler.VM JsCalendarYear.Offsets; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private readonly int lengths;

    /// <summary>Creates a year.</summary>
    /// <param name="year">The arithmetic year (the Calendar Date Record's <c>[[Year]]</c>).</param>
    /// <param name="newYearEpochDays">The year's first day, in days since 1970-01-01.</param>
    /// <param name="monthLengths">The length of each month in order, 12 or 13 of them, each from 29 to 31 days.</param>
    /// <param name="leapMonth">The ordinal of the leap month, from 1 to 13, in a year of 13 months; 0 in a year of 12.</param>
    /// <exception cref="ArgumentOutOfRangeException">A month count, a length or the leap month is outside what a table-backed calendar has.</exception>
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 via Broiler.VM JsCalendarYear..ctor; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    public CalendarYear(long year, long newYearEpochDays, ReadOnlySpan<int> monthLengths, int leapMonth)
    {
        if (monthLengths.Length is not (12 or 13))
            throw new ArgumentOutOfRangeException(nameof(monthLengths), monthLengths.Length, "A year has 12 or 13 months.");

        if (monthLengths.Length == 12 ? leapMonth != 0 : leapMonth is < 1 or > 13)
            throw new ArgumentOutOfRangeException(nameof(leapMonth), leapMonth, "A year of 13 months has a leap month, and one of 12 none.");

        var packed = 0;
        var days = 0;
        for (var i = 0; i < monthLengths.Length; i++)
        {
            if (monthLengths[i] is < 29 or > 31)
                throw new ArgumentOutOfRangeException(nameof(monthLengths), monthLengths[i], "A month has 29 to 31 days.");

            packed |= (monthLengths[i] - 29) << (2 * i);
            days += monthLengths[i];
        }

        Year = year;
        NewYearEpochDays = newYearEpochDays;
        MonthsInYear = monthLengths.Length;
        LeapMonth = leapMonth;
        DaysInYear = days;
        lengths = packed;
    }

    /// <summary>The arithmetic year: the related ISO year for <c>chinese</c> and <c>dangi</c>, AH or AP for the Hijri calendars.</summary>
    public long Year { get; }

    /// <summary>The year's first day, in days since 1970-01-01 (<see cref="IsoCalendarMath.FromEpochDays"/> gives its ISO date).</summary>
    public long NewYearEpochDays { get; }

    /// <summary>The number of months, 12 or 13.</summary>
    public int MonthsInYear { get; }

    /// <summary>The ordinal of the leap month, or 0 where the year has none.</summary>
    public int LeapMonth { get; }

    /// <summary>The number of days.</summary>
    public int DaysInYear { get; }

    /// <summary>The length of a month.</summary>
    /// <param name="month">The month's ordinal, from 1 to <see cref="MonthsInYear"/>.</param>
    /// <returns>From 29 to 31 days.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The ordinal is outside the year.</exception>
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 via Broiler.VM JsCalendarYear.DaysInMonth; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    public int DaysInMonth(int month)
    {
        if (month < 1 || month > MonthsInYear)
            throw new ArgumentOutOfRangeException(nameof(month), month, "The month is not one of the year's.");

        return 29 + ((lengths >> (2 * (month - 1))) & 3);
    }
}

/// <summary>
/// The year tables of the table-backed calendars, as an engine hands them in (JSD-0076; the area design's
/// <c>ICalendarTables</c>): the published Chinese, Korean and Qing years, the Umm al-Qura years, and the
/// Persian calendar's corrections to the 33-year rule. <see cref="CalendarYears"/> answers a year from them
/// inside their ranges and from the ported sources outside.
/// </summary>
// Broiler-AI:           Origin=Derived; Spec=JSD-0076, JSD-0056 via Broiler.VM JsCalendarData; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
// Broiler-Human:        PENDING
public interface ICalendarTables
{
    /// <summary>
    /// A <c>chinese</c> or <c>dangi</c> year the tables hold: China's or Korea's from 1912, and the Qing
    /// years before for both.
    /// </summary>
    /// <param name="calendar"><see cref="TableCalendar.Chinese"/> or <see cref="TableCalendar.Dangi"/>.</param>
    /// <param name="relatedIsoYear">The related ISO year.</param>
    /// <param name="year">The year; <see langword="default"/> when the tables do not hold it.</param>
    /// <returns>Whether the tables hold it.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The calendar is not a Chinese or Korean one.</exception>
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076, JSD-0056 via Broiler.VM JsCalendarData.EastAsianYear; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    bool TryGetEastAsianYear(TableCalendar calendar, long relatedIsoYear, out CalendarYear year);

    /// <summary>An <c>islamic-umalqura</c> year the table holds: KACST's, 1300 to 1600 AH in the pinned data.</summary>
    /// <param name="year">The year, AH.</param>
    /// <param name="calendarYear">The year; <see langword="default"/> when the table does not hold it.</param>
    /// <returns>Whether the table holds it.</returns>
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076, JSD-0056 via Broiler.VM JsCalendarData.UmmAlQuraYear; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    bool TryGetUmmAlQuraYear(long year, out CalendarYear calendarYear);

    /// <summary>
    /// Whether the corrections list a Persian year: one the 33-year rule makes leap and the calendar does
    /// not (<c>calendrical_calculations</c>' corrections, 1502 to 2987 AP in the pinned data).
    /// </summary>
    /// <param name="year">The year, AP.</param>
    /// <returns>Whether it is listed.</returns>
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076, JSD-0056 via Broiler.VM JsPersianCalendar.Corrected; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    bool IsPersianNonLeapYear(long year);
}

/// <summary>
/// The decoder of the data home's calendar table (JSD-0076; the layout of Broiler.VM's
/// <c>CalendarTableGenerator</c>, JSD-0056): an <see cref="ICalendarTables"/> over the bytes an engine hands
/// in. The library embeds no table.
/// </summary>
/// <remarks>
/// <para>
/// <b>The layout</b>: four sections - China's years, Korea's, the Qing years and the Umm al-Qura years - each
/// a first year and a count, both little-endian 16-bit, and a record per year; then a 16-bit count and that
/// many 16-bit Persian years. A Chinese, Korean or Qing year is three bytes, little-endian: thirteen bits of
/// month lengths (set for 30 days), four bits of the leap month's ordinal, and the new year's offset from 19
/// January. An Umm al-Qura year is two bytes: twelve bits of month lengths and the new year's offset, plus 8,
/// from the civil tabular new year.
/// </para>
/// <para>
/// <b>The bytes are copied and checked once</b>, when the tables are made: a table that does not hold that
/// layout to its last byte is refused with <see cref="ArgumentException"/>, as a broken precondition. A year
/// is decoded each time it is asked for, and nothing is cached. Every member is safe to call concurrently.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=Derived; Spec=JSD-0076, JSD-0056 via Broiler.VM JsCalendarData; IP=Low; Security=Medium; Resources=2; Fingerprint=TBF
// Broiler-Human:        PENDING
public sealed class CalendarTables : ICalendarTables
{
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 via Broiler.VM JsCalendarData; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private readonly byte[] china;

    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 via Broiler.VM JsCalendarData; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private readonly byte[] korea;

    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 via Broiler.VM JsCalendarData; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private readonly byte[] qing;

    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 via Broiler.VM JsCalendarData; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private readonly byte[] ummAlQura;

    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 via Broiler.VM JsCalendarData; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private readonly int chinaStart;

    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 via Broiler.VM JsCalendarData; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private readonly int koreaStart;

    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 via Broiler.VM JsCalendarData; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private readonly int qingStart;

    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 via Broiler.VM JsCalendarData; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private readonly int ummAlQuraStart;

    // The Persian years the 33-year rule makes leap and the calendar does not, ascending.
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 via Broiler.VM JsCalendarData.PersianNonLeapYears; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private readonly int[] persianNonLeapYears;

    /// <summary>Reads the calendar table an engine hands in.</summary>
    /// <param name="table">The table's bytes.</param>
    /// <exception cref="ArgumentException">The table does not hold the generator's layout.</exception>
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076, JSD-0056 via Broiler.VM JsCalendarData..ctor; IP=Low; Security=Medium; Resources=2; Fingerprint=TBF
    // Broiler-Human:        PENDING
    public CalendarTables(ReadOnlySpan<byte> table)
    {
        var at = 0;
        (chinaStart, china) = Section(table, ref at, 3);
        (koreaStart, korea) = Section(table, ref at, 3);
        (qingStart, qing) = Section(table, ref at, 3);
        (ummAlQuraStart, ummAlQura) = Section(table, ref at, 2);

        var count = U16(table, ref at);
        persianNonLeapYears = new int[count];
        for (var i = 0; i < count; i++)
        {
            persianNonLeapYears[i] = U16(table, ref at);
            if (i > 0 && persianNonLeapYears[i] <= persianNonLeapYears[i - 1])
                throw TableReader.Malformed("the Persian corrections are not ascending");
        }

        if (at != table.Length)
            throw TableReader.Malformed("the calendar table holds bytes after its last section");

        // EVERY RECORD MUST DECODE to a year, so that a year asked for later never fails.
        for (var i = 0; i < china.Length / 3; i++)
            _ = EastAsian(china, chinaStart, chinaStart + i);
        for (var i = 0; i < korea.Length / 3; i++)
            _ = EastAsian(korea, koreaStart, koreaStart + i);
        for (var i = 0; i < qing.Length / 3; i++)
            _ = EastAsian(qing, qingStart, qingStart + i);
    }

    /// <inheritdoc/>
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076, JSD-0056 via Broiler.VM JsCalendarData.EastAsianYear; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    public bool TryGetEastAsianYear(TableCalendar calendar, long relatedIsoYear, out CalendarYear year)
    {
        if (calendar is not (TableCalendar.Chinese or TableCalendar.Dangi))
            throw new ArgumentOutOfRangeException(nameof(calendar), calendar, "Only the Chinese and Korean calendars have these tables.");

        var (start, records) = relatedIsoYear >= 1912
            ? (calendar == TableCalendar.Dangi ? (koreaStart, korea) : (chinaStart, china))
            : (qingStart, qing);
        var index = relatedIsoYear - start;
        if (index < 0 || index >= records.Length / 3)
        {
            year = default;
            return false;
        }

        year = EastAsian(records, start, relatedIsoYear);
        return true;
    }

    /// <inheritdoc/>
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076, JSD-0056 via Broiler.VM JsCalendarData.UmmAlQuraYear; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    public bool TryGetUmmAlQuraYear(long year, out CalendarYear calendarYear)
    {
        var index = year - ummAlQuraStart;
        if (index < 0 || index >= ummAlQura.Length / 2)
        {
            calendarYear = default;
            return false;
        }

        var packed = ummAlQura[(int)index * 2] | (ummAlQura[((int)index * 2) + 1] << 8);
        Span<int> lengths = stackalloc int[12];
        for (var month = 0; month < 12; month++)
            lengths[month] = 29 + ((packed >> month) & 1);

        var start = CalendarYears.TabularNewYear(CalendarYears.CivilEpoch, year) + ((packed >> 12) - 8);
        calendarYear = new CalendarYear(year, start, lengths, 0);
        return true;
    }

    /// <inheritdoc/>
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076, JSD-0056 via Broiler.VM JsPersianCalendar.Corrected; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    public bool IsPersianNonLeapYear(long year) =>
        persianNonLeapYears.Length > 0 && year >= persianNonLeapYears[0] && year <= persianNonLeapYears[^1] &&
        Array.BinarySearch(persianNonLeapYears, (int)year) >= 0;

    // A Chinese, Korean or Qing record as a year.
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076, JSD-0056 via Broiler.VM JsCalendarData.EastAsianYear; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private static CalendarYear EastAsian(byte[] records, int start, long relatedIsoYear)
    {
        var at = (int)(relatedIsoYear - start) * 3;
        var packed = records[at] | (records[at + 1] << 8) | (records[at + 2] << 16);
        var leapMonth = (packed >> 13) & 0xF;
        var months = leapMonth == 0 ? 12 : 13;
        Span<int> lengths = stackalloc int[13];
        for (var month = 0; month < months; month++)
            lengths[month] = 29 + ((packed >> month) & 1);

        var newYear = IsoCalendarMath.EpochDays(relatedIsoYear, 1, 19) + (packed >> 17);
        return new CalendarYear(relatedIsoYear, newYear, lengths[..months], leapMonth);
    }

    // A little-endian 16-bit integer.
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 via Broiler.VM JsCalendarData.U16; IP=Low; Security=Medium; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private static int U16(ReadOnlySpan<byte> table, ref int at)
    {
        if (at + 2 > table.Length)
            throw TableReader.Malformed("the calendar table ends inside a field");

        var value = table[at] | (table[at + 1] << 8);
        at += 2;
        return value;
    }

    // A section: its first year, its count, and its records.
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 via Broiler.VM JsCalendarData.Section; IP=Low; Security=Medium; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private static (int Start, byte[] Records) Section(ReadOnlySpan<byte> table, ref int at, int size)
    {
        var start = U16(table, ref at);
        var count = U16(table, ref at);
        if (at + (count * size) > table.Length)
            throw TableReader.Malformed("the calendar table ends inside a section");

        var records = table.Slice(at, count * size).ToArray();
        at += count * size;
        return (start, records);
    }
}

/// <summary>
/// The year sources of the table-backed calendars (JSD-0076, SJL-D-21's "Broiler.VM's ICU4X sources" in both
/// directions): each calendar's year read from its table inside the table's range, and outside it from
/// ICU4X's approximation for <c>chinese</c> and <c>dangi</c>, the civil tabular year for
/// <c>islamic-umalqura</c>, and the 33-year rule for <c>persian</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>The sources answer years of magnitude up to <see cref="MaxYearMagnitude"/></b>, beyond every year that
/// holds a date within the Temporal proposal's ISO limits (the Hijri years reach 283,583); a year outside is
/// <see cref="TimeStatus.OutOfRange"/>, the year outside every source of X3.
/// </para>
/// <para>
/// <b>ICU4X's approximation</b> is ported from <c>icu_calendar</c> 2.3.0's
/// <c>src/cal/east_asian_traditional/simple.rs</c> (Unicode-3.0), as Broiler.VM ported it: mean solar terms
/// and mean new moons, in R.D. days and milliseconds within them, reckoned at UTC+8 for <c>chinese</c> and
/// UTC+9 for <c>dangi</c> after 1912 and at Beijing's local mean time before. Nothing here charges: a year
/// takes a table lookup or a few dozen additions (X3).
/// </para>
/// </remarks>
// Broiler-AI:           Origin=Derived; Spec=JSD-0076, Intl era and month code proposal Table 1 via Broiler.VM JsCalendars; IP=Medium; Security=Medium; Resources=1; Fingerprint=TBF
// Broiler-Human:        PENDING
public static class CalendarYears
{
    /// <summary>The largest magnitude of a year the sources answer: Broiler.VM's bound, beyond every calendar's years within the ISO limits.</summary>
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 X3 via Broiler.VM JsTemporal.YearBound; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    public const long MaxYearMagnitude = 300_000;

    // R.D. 719163 is 1970-01-01.
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 via Broiler.VM JsCalendarSystem.RataDieOfEpoch; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private const long RataDieOfEpoch = 719163;

    // R.D. 227015, Julian 622-07-16, a Friday: the civil Hijri epoch, in days since 1970-01-01.
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 via Broiler.VM JsHijriCalendar.CivilEpoch; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    internal const long CivilEpoch = 227015 - RataDieOfEpoch;

    // R.D. 226896, Julian 622-03-19, the first day of 1 AP, in days since 1970-01-01.
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 via Broiler.VM JsPersianCalendar.Epoch; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private const long PersianEpoch = 226896 - RataDieOfEpoch;

    // A day in milliseconds.
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 via Broiler.VM JsEastAsianCalendar.Day; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private const long Day = 86_400_000;

    // UTC+8, Beijing time.
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 via Broiler.VM JsEastAsianCalendar.UtcPlus8; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private const long UtcPlus8 = 28_800_000;

    // UTC+9, Korean time.
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 via Broiler.VM JsEastAsianCalendar.UtcPlus9; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private const long UtcPlus9 = 32_400_000;

    // UTC+(1397/180) hours, Beijing's local mean time.
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 via Broiler.VM JsEastAsianCalendar.BeijingMeanTime; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private const long BeijingMeanTime = 27_940_000;

    // The mean Gregorian year, 146097/400 days, in milliseconds.
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 via Broiler.VM JsEastAsianCalendar.MeanYear; IP=Medium; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private const long MeanYear = 31_556_952_000;

    // The mean Gregorian solar term, a twelfth of the mean year.
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 via Broiler.VM JsEastAsianCalendar.MeanSolarTerm; IP=Medium; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private const long MeanSolarTerm = 2_629_746_000;

    // The mean synodic month, 29.5305888531 days, truncated to milliseconds.
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 via Broiler.VM JsEastAsianCalendar.MeanSynodicMonth; IP=Medium; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private const long MeanSynodicMonth = 2_551_442_876;

    /// <summary>
    /// A year of a table-backed calendar: from its table inside the table's range, and from the ported
    /// source outside it.
    /// </summary>
    /// <param name="tables">The calendar tables the engine handed in.</param>
    /// <param name="calendar">The calendar.</param>
    /// <param name="year">The arithmetic year: the related ISO year for <c>chinese</c> and <c>dangi</c>, AH or AP for the Hijri calendars.</param>
    /// <param name="result">The year; <see langword="default"/> unless the status is <see cref="TimeStatus.Completed"/>.</param>
    /// <returns><see cref="TimeStatus.Completed"/>, or <see cref="TimeStatus.OutOfRange"/> for a year of magnitude above <see cref="MaxYearMagnitude"/>.</returns>
    /// <exception cref="ArgumentNullException">The tables are <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The calendar is not one of the four.</exception>
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076, Intl era and month code proposal Table 1 via Broiler.VM JsCalendarSystem.Year; IP=Low; Security=High; Resources=1; Fingerprint=TBF
    // Broiler-Falsified-If: a year inside a table's range is not the table's, a year outside it is not its source's, or a year of magnitude above the bound completes
    // Broiler-Human:        PENDING
    public static TimeStatus GetYear(ICalendarTables tables, TableCalendar calendar, long year, out CalendarYear result)
    {
        ArgumentNullException.ThrowIfNull(tables);
        if (calendar is < TableCalendar.Chinese or > TableCalendar.Persian)
            throw new ArgumentOutOfRangeException(nameof(calendar), calendar, "The calendar is not one of the four.");

        if (year is < -MaxYearMagnitude or > MaxYearMagnitude)
        {
            result = default;
            return TimeStatus.OutOfRange;
        }

        result = calendar switch
        {
            TableCalendar.Chinese or TableCalendar.Dangi => EastAsian(tables, calendar == TableCalendar.Dangi, year),
            TableCalendar.IslamicUmmAlQura => tables.TryGetUmmAlQuraYear(year, out var table) ? table : Tabular(year),
            _ => Persian(tables, year),
        };
        return TimeStatus.Completed;
    }

    // The tabular Hijri new year of a year from an epoch.
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 via Broiler.VM JsHijriCalendar.TabularNewYear; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    internal static long TabularNewYear(long epoch, long year) =>
        epoch + ((year - 1) * 354) + EcmaTime.FloorDivide(3 + (11 * year), 30);

    // The civil tabular year, Umm al-Qura's outside its table: months of 30 and 29 days alternately, the twelfth
    // of 30 in years 2, 5, 7, 10, 13, 16, 18, 21, 24, 26 and 29 of each 30.
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076, Intl era and month code proposal Table 1 via Broiler.VM JsHijriCalendar.Year; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private static CalendarYear Tabular(long year)
    {
        Span<int> lengths = stackalloc int[12];
        for (var month = 1; month <= 12; month++)
            lengths[month - 1] = month % 2 == 1 ? 30 : 29;

        if ((((14 + (11 * year)) % 30) + 30) % 30 < 11)
            lengths[11] = 30;

        return new CalendarYear(year, TabularNewYear(CivilEpoch, year), lengths, 0);
    }

    // The Persian year: the 33-year rule, a year the corrections list being common.
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076, Calendrical Calculations ch. 15 via Broiler.VM JsPersianCalendar.Year; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private static CalendarYear Persian(ICalendarTables tables, long year)
    {
        var start = PersianNewYear(tables, year);
        var days = (int)(PersianNewYear(tables, year + 1) - start);
        Span<int> lengths = [31, 31, 31, 31, 31, 31, 30, 30, 30, 30, 30, days - 336];
        return new CalendarYear(year, start, lengths, 0);
    }

    // The first day of a Persian year by the 33-year rule, a day later where the year before is a listed correction.
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076, Calendrical Calculations ch. 15 via Broiler.VM JsPersianCalendar.NewYear; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private static long PersianNewYear(ICalendarTables tables, long year) =>
        PersianEpoch - 1 + (365 * (year - 1)) + EcmaTime.FloorDivide((8 * year) + 21, 33) - (tables.IsPersianNonLeapYear(year - 1) ? 1 : 0);

    // A Chinese or Korean year: the table's, or ICU4X's approximation.
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 via Broiler.VM JsEastAsianCalendar.Year; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private static CalendarYear EastAsian(ICalendarTables tables, bool korean, long year)
    {
        if (tables.TryGetEastAsianYear(korean ? TableCalendar.Dangi : TableCalendar.Chinese, year, out var table))
            return table;

        return Approximate(year > 1912 ? (korean ? UtcPlus9 : UtcPlus8) : BeijingMeanTime, year);
    }

    // A moment: a day, R.D., and milliseconds within it.
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076, icu_calendar 2.3.0 simple.rs via Broiler.VM JsEastAsianCalendar.Moment; IP=Medium; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private readonly record struct Moment(long RataDie, long Milliseconds)
    {
        // Broiler-AI:           Origin=Derived; Spec=JSD-0076, icu_calendar 2.3.0 simple.rs via Broiler.VM JsEastAsianCalendar.Moment; IP=Medium; Security=Low; Resources=1; Fingerprint=TBF
        // Broiler-Human:        PENDING
        public static Moment operator +(Moment moment, long duration)
        {
            var total = moment.Milliseconds + duration;
            return new Moment(moment.RataDie + EcmaTime.FloorDivide(total, Day), total - (EcmaTime.FloorDivide(total, Day) * Day));
        }
    }

    // The latest moment start + n * period on or before the day rataDie.
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076, icu_calendar 2.3.0 simple.rs via Broiler.VM JsEastAsianCalendar.OnOrBefore; IP=Medium; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private static Moment OnOrBefore(long rataDie, Moment start, long period)
    {
        var difference = ((rataDie - start.RataDie) * Day) - start.Milliseconds;
        var periods = EcmaTime.FloorDivide(difference + Day - 1, period);
        var milliseconds = (start.RataDie * Day) + start.Milliseconds + (periods * period);
        var day = (milliseconds / Day) - (milliseconds < 0 ? 1 : 0);
        var within = (milliseconds % Day) + (milliseconds < 0 ? Day : 0);
        return new Moment(day, within);
    }

    // The R.D. day of an ISO date.
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076 via Broiler.VM JsEastAsianCalendar.RataDie; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private static long RataDie(long year, int month, int day) => IsoCalendarMath.EpochDays(year, month, day) + RataDieOfEpoch;

    // A year by mean solar terms and mean new moons at a UTC offset, after the pingqi rule.
    // Broiler-AI:           Origin=Derived; Spec=JSD-0076, icu_calendar 2.3.0 simple.rs via Broiler.VM JsEastAsianCalendar.Approximate; IP=Medium; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private static CalendarYear Approximate(long offset, long relatedIso)
    {
        // 1999-12-22T07:44Z, THE SOLSTICE; 2000-01-06T18:14Z, THE NEW MOON.
        var solstice = new Moment(RataDie(1999, 12, 22), ((7 * 60) + 44) * 60 * 1000L) + offset;
        var newMoonBase = new Moment(RataDie(2000, 1, 6), ((18 * 60) + 14) * 60 * 1000L) + offset;

        var majorSolarTerm = OnOrBefore(RataDie(relatedIso, 1, 1) - 1, solstice, MeanYear);
        var newMoon = OnOrBefore(majorSolarTerm.RataDie, newMoonBase, MeanSynodicMonth);
        var nextNewMoon = newMoon + MeanSynodicMonth;
        var solarTerm = -2;
        var hadLeap = false;

        while (solarTerm < 0 || (nextNewMoon.RataDie <= majorSolarTerm.RataDie && !hadLeap))
        {
            if (nextNewMoon.RataDie <= majorSolarTerm.RataDie && !hadLeap)
            {
                hadLeap = true;
            }
            else
            {
                solarTerm++;
                majorSolarTerm += MeanSolarTerm;
            }

            (newMoon, nextNewMoon) = (nextNewMoon, nextNewMoon + MeanSynodicMonth);
        }

        var newYear = newMoon.RataDie;
        var lengths = new List<int>(13);
        var leapMonth = 0;

        while (solarTerm < 12 || (nextNewMoon.RataDie <= majorSolarTerm.RataDie && !hadLeap))
        {
            lengths.Add((int)(nextNewMoon.RataDie - newMoon.RataDie));

            if (nextNewMoon.RataDie <= majorSolarTerm.RataDie && !hadLeap)
            {
                hadLeap = true;
                leapMonth = solarTerm + 1;
            }
            else
            {
                solarTerm++;
                majorSolarTerm += MeanSolarTerm;
            }

            (newMoon, nextNewMoon) = (nextNewMoon, nextNewMoon + MeanSynodicMonth);
        }

        return new CalendarYear(relatedIso, newYear - RataDieOfEpoch, lengths.ToArray(), leapMonth);
    }
}
