using System;
using System.Linq;
using Broiler.ECMAScript.Time;
using Xunit;

namespace Broiler.ECMAScript.Algorithms.Tests;

/// <summary>
/// The table-backed calendars' years (milestone SJL-8.3): the decoder of the calendar table and the year
/// sources outside it - ICU4X's approximation for <c>chinese</c> and <c>dangi</c>, the civil tabular year for
/// Umm al-Qura and the 33-year rule for Persian - at both edges of each table and just outside, with the
/// years the correspondence's rows K1 to K13 name.
/// </summary>
public class CalendarYearTests
{
    private static CalendarTables Tables => TimeTablesFixture.CalendarTables;

    private static CalendarYear Year(TableCalendar calendar, long year)
    {
        Assert.Equal(TimeStatus.Completed, CalendarYears.GetYear(Tables, calendar, year, out var result));
        Assert.Equal(year, result.Year);
        Assert.Equal(result.DaysInYear, Enumerable.Range(1, result.MonthsInYear).Sum(result.DaysInMonth));
        return result;
    }

    private static long Days(long year, int month, int day) => IsoCalendarMath.EpochDays(year, month, day);

    // The first day of an ordinal month.
    private static long MonthStart(CalendarYear year, int month) =>
        year.NewYearEpochDays + Enumerable.Range(1, month - 1).Sum(year.DaysInMonth);

    // ---- the Chinese and Korean years ----------------------------------------------------------------

    [Theory]
    [InlineData(TableCalendar.Chinese)]
    [InlineData(TableCalendar.Dangi)]
    public void The_lunisolar_tables_hold_1900_to_2102_and_the_approximation_answers_outside(TableCalendar calendar)
    {
        foreach (var year in new[] { 1900, 1911, 1912, 2050, 2051, 2101, 2102 })
        {
            Assert.True(Tables.TryGetEastAsianYear(calendar, year, out var table), year.ToString(System.Globalization.CultureInfo.InvariantCulture));
            Assert.Equal(table, Year(calendar, year));
        }

        foreach (var year in new[] { 1899, 2103, 1, -2000, 5000 })
        {
            Assert.False(Tables.TryGetEastAsianYear(calendar, year, out var none));
            Assert.Equal(default, none);
            var approximate = Year(calendar, year);
            Assert.InRange(approximate.MonthsInYear, 12, 13);
            Assert.Equal(approximate.MonthsInYear == 13, approximate.LeapMonth != 0);
            Assert.InRange(approximate.DaysInYear, 353, 385);
        }

        // THE QING YEARS ARE BOTH CALENDARS' before 1912.
        Tables.TryGetEastAsianYear(TableCalendar.Chinese, 1905, out var chinese);
        Tables.TryGetEastAsianYear(TableCalendar.Dangi, 1905, out var korean);
        Assert.Equal(chinese, korean);
    }

    [Fact]
    public void A_year_runs_from_its_new_year_to_the_next_within_each_source()
    {
        // THE SEAMS between a table and the source outside it are each source's own: a year either side of one
        // is that side's, and they need not meet (Broiler.VM's behaviour, which the differential check holds).
        var seams = new[]
        {
            (TableCalendar.Chinese, 1899L), (TableCalendar.Chinese, 2102L), (TableCalendar.Dangi, 1899L), (TableCalendar.Dangi, 2102L),
            (TableCalendar.IslamicUmmAlQura, 1299L), (TableCalendar.IslamicUmmAlQura, 1600L),
        };
        foreach (var calendar in new[] { TableCalendar.Chinese, TableCalendar.Dangi, TableCalendar.IslamicUmmAlQura, TableCalendar.Persian })
        {
            for (var year = -3000L; year <= 3000; year++)
            {
                if (seams.Contains((calendar, year)))
                    continue;

                CalendarYears.GetYear(Tables, calendar, year, out var current);
                CalendarYears.GetYear(Tables, calendar, year + 1, out var next);
                Assert.Equal(next.NewYearEpochDays, current.NewYearEpochDays + current.DaysInYear);
            }
        }
    }

    [Theory]
    [InlineData(1900, 9, 1900, 9, 24)] // M08L, its first day
    [InlineData(1987, 7, 1987, 7, 26)] // M06L
    [InlineData(2012, 5, 2012, 5, 21)] // M04L
    [InlineData(2017, 7, 0, 0, 0)] // M06L
    [InlineData(2020, 5, 0, 0, 0)] // M04L
    [InlineData(2023, 3, 0, 0, 0)] // M02L
    [InlineData(2025, 7, 0, 0, 0)] // M06L
    [InlineData(2028, 6, 0, 0, 0)] // M05L
    [InlineData(2033, 12, 2033, 12, 22)] // M11L
    [InlineData(2052, 9, 0, 0, 0)] // M08L
    [InlineData(2099, 3, 0, 0, 0)] // M02L
    [InlineData(2101, 8, 2101, 8, 25)] // M07L
    public void A_chinese_leap_month_is_the_tables(int year, int leapOrdinal, int isoYear, int isoMonth, int isoDay)
    {
        var chinese = Year(TableCalendar.Chinese, year);
        Assert.Equal(13, chinese.MonthsInYear);
        Assert.Equal(leapOrdinal, chinese.LeapMonth);
        if (isoYear != 0)
            Assert.Equal(Days(isoYear, isoMonth, isoDay), MonthStart(chinese, leapOrdinal));
    }

    [Theory]
    [InlineData(2012, 4)] // M03L, where chinese has M04L (K3)
    [InlineData(2023, 3)] // M02L
    [InlineData(2033, 12)] // M11L
    [InlineData(2052, 9)] // M08L
    [InlineData(2101, 8)] // M07L
    public void A_dangi_leap_month_is_the_tables(int year, int leapOrdinal)
    {
        var dangi = Year(TableCalendar.Dangi, year);
        Assert.Equal(13, dangi.MonthsInYear);
        Assert.Equal(leapOrdinal, dangi.LeapMonth);
    }

    [Fact]
    public void The_known_lunisolar_years_are_ICU4Xs()
    {
        // K1: chinese 2033 begins 2033-01-31, has 13 months and M11L as its twelfth; 1987-08-17 is M06L 23.
        var y2033 = Year(TableCalendar.Chinese, 2033);
        Assert.Equal(Days(2033, 1, 31), y2033.NewYearEpochDays);
        Assert.Equal(12, y2033.LeapMonth);
        Assert.Equal(Days(1987, 8, 17), MonthStart(Year(TableCalendar.Chinese, 1987), 7) + 22);

        // K2: chinese 2057, inside Table 1's published range, is ICU4X's (and the Hong Kong Observatory's): months
        // sllslslslsls and its ninth month beginning on 2057-09-28.
        var y2057 = Year(TableCalendar.Chinese, 2057);
        Assert.Equal(new[] { 29, 30, 30, 29, 30, 29, 30, 29, 30, 29, 30, 29 }, Enumerable.Range(1, 12).Select(y2057.DaysInMonth));
        Assert.Equal(0, y2057.LeapMonth);
        Assert.Equal(Days(2057, 9, 28), MonthStart(y2057, 9));

        // K6: ICU4X's approximation after the table: chinese 2104 begins 2104-01-27 with M06L.
        var y2104 = Year(TableCalendar.Chinese, 2104);
        Assert.Equal(Days(2104, 1, 27), y2104.NewYearEpochDays);
        Assert.Equal(7, y2104.LeapMonth);

        // K5: dangi 918, ICU4X's approximation, is 354 days.
        Assert.Equal(354, Year(TableCalendar.Dangi, 918).DaysInYear);

        // THE TWO CALENDARS DIFFER where Korea's meridian moves a new moon across midnight: 2012's leap month.
        Assert.NotEqual(Year(TableCalendar.Chinese, 2012).LeapMonth, Year(TableCalendar.Dangi, 2012).LeapMonth);
    }

    // ---- Umm al-Qura ---------------------------------------------------------------------------------

    [Fact]
    public void Umm_al_Qura_is_the_table_from_1300_to_1600_AH_and_the_civil_tabular_year_outside()
    {
        foreach (var year in new[] { 1300, 1301, 1446, 1599, 1600 })
        {
            Assert.True(Tables.TryGetUmmAlQuraYear(year, out var table));
            Assert.Equal(table, Year(TableCalendar.IslamicUmmAlQura, year));
            Assert.Equal(12, table.MonthsInYear);
            Assert.Equal(0, table.LeapMonth);
        }

        var first = Year(TableCalendar.IslamicUmmAlQura, 1300);
        Assert.Equal(Days(1882, 11, 12), first.NewYearEpochDays);
        Assert.Equal(new[] { 30, 29, 30, 29, 30, 29, 30, 29, 30, 29, 30, 29 }, Enumerable.Range(1, 12).Select(first.DaysInMonth));
        Assert.Equal(Days(2173, 12, 7), Year(TableCalendar.IslamicUmmAlQura, 1600).NewYearEpochDays);

        foreach (var year in new[] { 1299, 1601, 1, 0, -100, 5000 })
        {
            Assert.False(Tables.TryGetUmmAlQuraYear(year, out _));
            var tabular = Year(TableCalendar.IslamicUmmAlQura, year);
            Assert.Equal(TabularNewYear(year), tabular.NewYearEpochDays);
            Assert.Equal(TabularLeap(year) ? 355 : 354, tabular.DaysInYear);
            for (var month = 1; month <= 11; month++)
                Assert.Equal(month % 2 == 1 ? 30 : 29, tabular.DaysInMonth(month));
        }

        Assert.Equal(Days(2174, 11, 26), Year(TableCalendar.IslamicUmmAlQura, 1601).NewYearEpochDays); // as islamic-civil 1601 (K9)
    }

    // The civil tabular calendar from R.D. 227015 (Calendrical Calculations' islamic-from-fixed, inverted).
    private static long TabularNewYear(long year) =>
        (227015 - 719163) + ((year - 1) * 354) + (long)Math.Floor((3 + (11 * year)) / 30.0);

    private static bool TabularLeap(long year) => ((14 + (11 * year)) % 30 + 30) % 30 < 11;

    // ---- Persian -------------------------------------------------------------------------------------

    [Fact]
    public void The_Persian_corrections_run_from_1502_to_2987_AP()
    {
        Assert.False(Tables.IsPersianNonLeapYear(1501));
        Assert.True(Tables.IsPersianNonLeapYear(1502));
        Assert.True(Tables.IsPersianNonLeapYear(2987));
        Assert.False(Tables.IsPersianNonLeapYear(2988));
        Assert.Equal(78, Enumerable.Range(1400, 1700).Count(y => Tables.IsPersianNonLeapYear(y)));
        Assert.False(Tables.IsPersianNonLeapYear(long.MaxValue));
        Assert.False(Tables.IsPersianNonLeapYear(long.MinValue));
    }

    [Theory]
    [InlineData(1501)]
    [InlineData(1502)]
    [InlineData(1503)]
    [InlineData(2986)]
    [InlineData(2987)]
    [InlineData(2988)]
    [InlineData(2989)]
    [InlineData(1)]
    [InlineData(1404)]
    [InlineData(-5000)]
    [InlineData(9400)]
    public void A_Persian_year_is_the_33_year_rule_a_listed_year_being_common(int year)
    {
        var persian = Year(TableCalendar.Persian, year);
        var start = RuleNewYear(year) - (Tables.IsPersianNonLeapYear(year - 1) ? 1 : 0);
        var next = RuleNewYear(year + 1) - (Tables.IsPersianNonLeapYear(year) ? 1 : 0);
        Assert.Equal(start, persian.NewYearEpochDays);
        Assert.Equal(next - start, persian.DaysInYear);
        Assert.Equal(12, persian.MonthsInYear);
        Assert.Equal(0, persian.LeapMonth);
        Assert.Equal(new[] { 31, 31, 31, 31, 31, 31, 30, 30, 30, 30, 30 }, Enumerable.Range(1, 11).Select(persian.DaysInMonth));
        Assert.Equal(persian.DaysInYear - 336, persian.DaysInMonth(12));

        if (Tables.IsPersianNonLeapYear(year))
            Assert.Equal(365, persian.DaysInYear);
    }

    // The 33-year rule's new year from R.D. 226896 (1 AP), with no correction.
    private static long RuleNewYear(long year) =>
        (226896 - 719163) - 1 + (365 * (year - 1)) + (long)Math.Floor(((8 * year) + 21) / 33.0);

    [Fact]
    public void The_known_Persian_years()
    {
        // K11: 1535 is leap, Esfand 30 being 2157-03-20, and 1536 begins 2157-03-21.
        var y1535 = Year(TableCalendar.Persian, 1535);
        Assert.Equal(366, y1535.DaysInYear);
        Assert.Equal(Days(2157, 3, 21), Year(TableCalendar.Persian, 1536).NewYearEpochDays);

        // K12: year 1 begins 0622-03-21 and is leap.
        var y1 = Year(TableCalendar.Persian, 1);
        Assert.Equal(Days(622, 3, 21), y1.NewYearEpochDays);
        Assert.Equal(366, y1.DaysInYear);

        // 1404 AP began on 2025-03-21.
        Assert.Equal(Days(2025, 3, 21), Year(TableCalendar.Persian, 1404).NewYearEpochDays);
    }

    // ---- the range and the preconditions -------------------------------------------------------------

    [Theory]
    [InlineData(TableCalendar.Chinese)]
    [InlineData(TableCalendar.Dangi)]
    [InlineData(TableCalendar.IslamicUmmAlQura)]
    [InlineData(TableCalendar.Persian)]
    public void A_year_outside_every_source_is_a_status(TableCalendar calendar)
    {
        Year(calendar, CalendarYears.MaxYearMagnitude);
        Year(calendar, -CalendarYears.MaxYearMagnitude);
        foreach (var year in new[] { CalendarYears.MaxYearMagnitude + 1, -CalendarYears.MaxYearMagnitude - 1, long.MaxValue, long.MinValue })
        {
            Assert.Equal(TimeStatus.OutOfRange, CalendarYears.GetYear(Tables, calendar, year, out var none));
            Assert.Equal(default, none);
        }
    }

    [Fact]
    public void A_broken_precondition_throws()
    {
        Assert.Throws<ArgumentNullException>(() => CalendarYears.GetYear(null!, TableCalendar.Chinese, 2000, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => CalendarYears.GetYear(Tables, (TableCalendar)4, 2000, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => CalendarYears.GetYear(Tables, (TableCalendar)(-1), 2000, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => Tables.TryGetEastAsianYear(TableCalendar.Persian, 2000, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => Tables.TryGetEastAsianYear(TableCalendar.IslamicUmmAlQura, 2000, out _));
    }

    [Fact]
    public void A_year_holds_what_a_table_backed_calendar_has()
    {
        int[] twelve = [30, 29, 30, 29, 30, 29, 30, 29, 30, 29, 30, 29];
        int[] thirteen = [.. twelve, 30];
        var year = new CalendarYear(2000, 10957, thirteen, 5);
        Assert.Equal(2000, year.Year);
        Assert.Equal(10957, year.NewYearEpochDays);
        Assert.Equal(13, year.MonthsInYear);
        Assert.Equal(5, year.LeapMonth);
        Assert.Equal(384, year.DaysInYear);
        Assert.Equal(thirteen, Enumerable.Range(1, 13).Select(year.DaysInMonth));
        Assert.Equal(year, new CalendarYear(2000, 10957, thirteen, 5));
        Assert.NotEqual(year, new CalendarYear(2000, 10957, [.. twelve, 29], 5));
        Assert.Equal(31, new CalendarYear(1, 1, [31, 31, 31, 31, 31, 31, 30, 30, 30, 30, 30, 29], 0).DaysInMonth(1));

        Assert.Throws<ArgumentOutOfRangeException>(() => year.DaysInMonth(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => year.DaysInMonth(14));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CalendarYear(2000, 0, twelve, 0).DaysInMonth(13));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CalendarYear(2000, 0, twelve[..11], 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CalendarYear(2000, 0, [.. thirteen, 30], 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CalendarYear(2000, 0, twelve, 3));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CalendarYear(2000, 0, thirteen, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CalendarYear(2000, 0, thirteen, 14));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CalendarYear(2000, 0, [28, .. twelve[1..]], 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CalendarYear(2000, 0, [32, .. twelve[1..]], 0));
    }

    [Fact]
    public void A_calendar_table_that_does_not_hold_the_layout_is_refused()
    {
        var table = TimeTablesFixture.Calendars.ToArray();
        Assert.Throws<ArgumentException>(() => new CalendarTables(table.AsSpan(0, table.Length - 1)));
        Assert.Throws<ArgumentException>(() => new CalendarTables([.. table, 0]));
        Assert.Throws<ArgumentException>(() => new CalendarTables(table.AsSpan(0, 10)));
        Assert.Throws<ArgumentException>(() => new CalendarTables(ReadOnlySpan<byte>.Empty));

        // THE PERSIAN YEARS MUST ASCEND: swap the last two.
        var swapped = (byte[])table.Clone();
        (swapped[^4], swapped[^3], swapped[^2], swapped[^1]) = (swapped[^2], swapped[^1], swapped[^4], swapped[^3]);
        Assert.Throws<ArgumentException>(() => new CalendarTables(swapped));

        // AN EMPTY TABLE OF EVERY SECTION is a table: every year then comes from the ported sources.
        var empty = new CalendarTables(new byte[18]);
        Assert.False(empty.TryGetEastAsianYear(TableCalendar.Chinese, 2000, out _));
        Assert.False(empty.TryGetUmmAlQuraYear(1446, out _));
        Assert.False(empty.IsPersianNonLeapYear(1502));
        Assert.Equal(TimeStatus.Completed, CalendarYears.GetYear(empty, TableCalendar.Chinese, 2000, out var approximate));
        Assert.InRange(approximate.MonthsInYear, 12, 13);
    }
}
