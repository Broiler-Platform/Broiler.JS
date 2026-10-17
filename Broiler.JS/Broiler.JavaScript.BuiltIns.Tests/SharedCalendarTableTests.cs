namespace Broiler.JavaScript.BuiltIns.Tests;

// The table-backed calendars on the shared library's year sources and the data home's calendar tables
// (milestone SJL-8.6, decision SJL-D-21): chinese, dangi, islamic-umalqura and persian read each year from
// CalendarYears, ICU4X's, where they read .NET's lunisolar and persian calendars and the CLDR package's
// Chinese computation and Umm al-Qura table. Each case names the row of SJL-8.1's correspondence from code
// (Broiler.VM's docs/evidence/sjl-8-001/correspondence.md) that it holds.
public class SharedCalendarTableTests
{
    private static string Eval(string source) => SharedDateAlgorithmTests.Eval(source);

    // The deletion list and clause 8: no .NET calendar class and none of the CLDR package's calendars.
    [Fact]
    public void No_retired_calendar_is_referenced()
    {
        var referenced = SharedTimeZoneTests.TypeNames(typeof(RegExp.JSRegExp).Assembly);
        string[] retired =
        [
            "TemporalLunisolarCalendar", "TemporalSolarHijriCalendar", "CldrChineseCalendar", "CldrUmmAlQura",
            "ChineseLunisolarCalendar", "KoreanLunisolarCalendar", "EastAsianLunisolarCalendar", "PersianCalendar",
            "UmAlQuraCalendar", "HijriCalendar",
        ];
        foreach (var name in retired)
            Assert.DoesNotContain(name, referenced);
    }

    // K2: chinese 2057, inside Table 1's published range.
    [Theory]
    [InlineData("Temporal.PlainDate.from({ year: 2057, monthCode: 'M09', day: 1, calendar: 'chinese' }).toString()", "2057-09-28[u-ca=chinese]")]
    [InlineData("var d = Temporal.PlainDate.from('2057-09-28').withCalendar('chinese'); d.monthCode + '/' + d.day", "M09/1")]
    public void Chinese_2057_is_the_observatory_s(string expression, string expected)
        => Assert.Equal(expected, Eval(expression));

    // K4, K5, K6: the years outside Table 1's ranges are ICU4X's.
    [Theory]
    [InlineData("Temporal.PlainDate.from({ year: 2061, monthCode: 'M01', day: 1, calendar: 'dangi' }).toString()", "2061-01-22[u-ca=dangi]")]
    [InlineData("Temporal.PlainDate.from({ year: 2099, monthCode: 'M03L', day: 1, calendar: 'dangi' }).month", "4")]
    [InlineData("Temporal.PlainDate.from({ year: 918, monthCode: 'M01', day: 1, calendar: 'dangi' }).daysInYear", "354")]
    [InlineData("Temporal.PlainDate.from({ year: 2104, monthCode: 'M01', day: 1, calendar: 'chinese' }).toString()", "2104-01-27[u-ca=chinese]")]
    [InlineData("Temporal.PlainDate.from({ year: 2104, monthCode: 'M06L', day: 1, calendar: 'chinese' }).month", "7")]
    public void Lunisolar_years_outside_the_published_ranges_are_ICU4X_s(string expression, string expected)
        => Assert.Equal(expected, Eval(expression));

    // K7: far lunisolar dates are dates of 29- and 30-day months, where the CLDR computation threw or gave
    // impossible months.
    [Theory]
    [InlineData("var d = Temporal.PlainDate.from('-271821-04-19').withCalendar('chinese'); d.monthCode + '/' + d.day", "M03/17")]
    [InlineData("var d = Temporal.PlainDate.from('+275760-09-13').withCalendar('dangi'); d.daysInMonth + ' ' + d.daysInYear", "30 355")]
    [InlineData("var d = Temporal.PlainDate.from('+275000-01-01').withCalendar('chinese'); var ok = true; for (var i = 1; i <= d.monthsInYear; i++) { var n = d.with({ month: i, day: 1 }).daysInMonth; ok = ok && (n === 29 || n === 30); } ok", "true")]
    [InlineData("var d = Temporal.PlainDate.from('-150000-06-01').withCalendar('dangi'); d.daysInMonth === 29 || d.daysInMonth === 30", "true")]
    public void Far_lunisolar_dates_are_valid(string expression, string expected)
        => Assert.Equal(expected, Eval(expression));

    // K11: persian outside Table 1's range follows calendrical_calculations' corrections.
    [Theory]
    [InlineData("Temporal.PlainDate.from({ year: 1535, monthCode: 'M12', day: 30, calendar: 'persian' }).toString()", "2157-03-20[u-ca=persian]")]
    [InlineData("Temporal.PlainDate.from({ year: 1536, monthCode: 'M01', day: 1, calendar: 'persian' }).toString()", "2157-03-21[u-ca=persian]")]
    public void Persian_years_outside_Table_1_are_ICU4X_s(string expression, string expected)
        => Assert.Equal(expected, Eval(expression));

    // K12: one Persian date per ISO date at .NET's former bounds, where two days repeated a date.
    [Theory]
    [InlineData("0622-03-21", "1/M01/1")]
    [InlineData("0622-03-22", "1/M01/2")]
    [InlineData("9999-12-31", "9378/M10/10")]
    [InlineData("+010000-01-01", "9378/M10/11")]
    public void Each_ISO_date_is_one_Persian_date(string iso, string expected)
        => Assert.Equal(expected + " " + iso, Eval(
            $"var d = Temporal.PlainDate.from('{iso}').withCalendar('persian'); d.year + '/' + d.monthCode + '/' + d.day + ' ' + d.withCalendar('iso8601').toString()"));

    // K8, K9 (agreement): Umm al-Qura's table and the civil year beyond it, now from the shared tables.
    [Theory]
    [InlineData("Temporal.PlainDate.from({ year: 1300, monthCode: 'M01', day: 1, calendar: 'islamic-umalqura' }).toString()", "1882-11-12[u-ca=islamic-umalqura]")]
    [InlineData("Temporal.PlainDate.from({ year: 1600, monthCode: 'M01', day: 1, calendar: 'islamic-umalqura' }).toString()", "2173-12-07[u-ca=islamic-umalqura]")]
    [InlineData("Temporal.PlainDate.from({ year: 1601, monthCode: 'M01', day: 1, calendar: 'islamic-umalqura' }).toString()", "2174-11-26[u-ca=islamic-umalqura]")]
    [InlineData("var d = Temporal.PlainDate.from('1882-11-12').withCalendar('islamic-umalqura'); d.era + ' ' + d.eraYear + ' ' + d.daysInYear", "ah 1300 354")]
    public void Umm_al_Qura_reads_the_shared_table(string expression, string expected)
        => Assert.Equal(expected, Eval(expression));
}
