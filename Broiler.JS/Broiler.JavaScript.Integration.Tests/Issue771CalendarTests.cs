using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for the Gregorian-family non-ISO calendars on Temporal.PlainDate
// (issue #771, Problems 6/14/30 — gregory, buddhist, roc). These calendars share the ISO 8601
// day/month arithmetic and differ only in how the proleptic-Gregorian year is numbered and split
// into an era + era-year. The lunisolar / other Intl calendars remain unsupported (RangeError).
public class Issue771CalendarTests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code)?.ToString();
    }

    private static string ErrorName(string body) => Eval(
        "let t='NONE'; try { " + body + " } catch (e) { t = e.constructor.name; } t");

    // --- gregory ---

    [Fact]
    public void GregoryEraAndYear()
        => Assert.Equal("gregory:ce:2000:2000", Eval(
            "const d = new Temporal.PlainDate(2000, 3, 6, 'gregory');" +
            "d.calendarId + ':' + d.era + ':' + d.eraYear + ':' + d.year"));

    [Fact]
    public void GregoryBceEra()
        => Assert.Equal("bce:1", Eval(
            "const d = new Temporal.PlainDate(0, 3, 6, 'gregory'); d.era + ':' + d.eraYear"));

    [Fact]
    public void GregoryFromCeEraYearZeroResolvesToBce1()
        => Assert.Equal("0:bce:1", Eval(
            "const d = Temporal.PlainDate.from({ era:'ce', eraYear:0, monthCode:'M01', day:1, calendar:'gregory' }, { overflow:'reject' });" +
            "d.year + ':' + d.era + ':' + d.eraYear"));

    [Fact]
    public void GregoryFromBceEraYearZeroResolvesToCe1()
        => Assert.Equal("1:ce:1", Eval(
            "const d = Temporal.PlainDate.from({ era:'bce', eraYear:0, monthCode:'M01', day:1, calendar:'gregory' }, { overflow:'reject' });" +
            "d.year + ':' + d.era + ':' + d.eraYear"));

    [Fact]
    public void GregoryToStringShowsAnnotationByDefault()
        => Assert.Equal("2000-03-06[u-ca=gregory]", Eval("new Temporal.PlainDate(2000, 3, 6, 'gregory').toString()"));

    [Fact]
    public void GregoryToStringNeverHidesAnnotation()
        => Assert.Equal("2000-03-06", Eval("new Temporal.PlainDate(2000, 3, 6, 'gregory').toString({ calendarName:'never' })"));

    [Fact]
    public void GregoryToStringCriticalUsesBang()
        => Assert.Equal("2000-03-06[!u-ca=gregory]", Eval("new Temporal.PlainDate(2000, 3, 6, 'gregory').toString({ calendarName:'critical' })"));

    [Fact]
    public void AddPreservesCalendar()
        => Assert.Equal("gregory:2001", Eval(
            "const d = new Temporal.PlainDate(2000, 3, 6, 'gregory').add({ years: 1 });" +
            "d.calendarId + ':' + d.year"));

    [Fact]
    public void WithCalendarSwitchesToGregory()
        => Assert.Equal("ce", Eval("new Temporal.PlainDate(2000, 3, 6).withCalendar('gregory').era"));

    // --- buddhist (year = isoYear + 543) ---

    [Fact]
    public void BuddhistYearAndEra()
        => Assert.Equal("2543:be:2543", Eval(
            "const d = new Temporal.PlainDate(2000, 1, 1, 'buddhist'); d.year + ':' + d.era + ':' + d.eraYear"));

    [Fact]
    public void BuddhistFromYearMapsToIso()
        => Assert.Equal("2000-01-01", Eval(
            "Temporal.PlainDate.from({ year:2543, month:1, day:1, calendar:'buddhist' }).toString({ calendarName:'never' })"));

    // --- roc (year = isoYear - 1911) ---

    [Fact]
    public void RocFromYearMapsToIso()
        => Assert.Equal("1960-02-16:roc", Eval(
            "const d = Temporal.PlainDate.from({ year:49, monthCode:'M02', day:16, calendar:'roc' });" +
            "d.toString({ calendarName:'never' }) + ':' + d.era"));

    [Fact]
    public void RocBeforeEra()
        => Assert.Equal("broc", Eval("new Temporal.PlainDate(1911, 1, 1, 'roc').era"));

    // --- shared behavior ---

    [Fact]
    public void SinceSameCalendarWorks()
        => Assert.Equal("P1Y", Eval(
            "const a = new Temporal.PlainDate(2000, 3, 6, 'gregory');" +
            "new Temporal.PlainDate(2001, 3, 6, 'gregory').since(a, { largestUnit:'year' }).toString()"));

    [Fact]
    public void SinceDifferentCalendarsThrows()
        => Assert.Equal("RangeError", ErrorName(
            "new Temporal.PlainDate(2001, 3, 6, 'gregory').since(new Temporal.PlainDate(2000, 1, 1, 'buddhist'));"));

    [Fact]
    public void ParsingCalendarAnnotation()
        => Assert.Equal("gregory:ce", Eval(
            "const d = Temporal.PlainDate.from('2000-03-06[u-ca=gregory]'); d.calendarId + ':' + d.era"));

    [Fact]
    public void UnsupportedCalendarThrowsRangeError()
        => Assert.Equal("RangeError", ErrorName("new Temporal.PlainDate(2000, 1, 1, 'hebrew');"));

    [Fact]
    public void IsoCalendarHasNoEra()
        => Assert.Equal("undefined:undefined", Eval(
            "const d = new Temporal.PlainDate(2000, 3, 6); typeof d.era + ':' + typeof d.eraYear"));

    [Fact]
    public void EqualsConsidersCalendar()
        => Assert.Equal("false", Eval(
            "new Temporal.PlainDate(2000, 3, 6, 'gregory').equals(new Temporal.PlainDate(2000, 3, 6)) + ''"));

    // --- PlainDateTime (Problem 7) ---

    [Fact]
    public void DateTimeGregoryEraAndYear()
        => Assert.Equal("gregory:ce:2000:2000", Eval(
            "const d = new Temporal.PlainDateTime(2000, 3, 6, 12, 0, 0, 0, 0, 0, 'gregory');" +
            "d.calendarId + ':' + d.era + ':' + d.eraYear + ':' + d.year"));

    [Fact]
    public void DateTimeToStringShowsAnnotation()
        => Assert.Equal("2000-03-06T12:30:45[u-ca=gregory]", Eval(
            "new Temporal.PlainDateTime(2000, 3, 6, 12, 30, 45, 0, 0, 0, 'gregory').toString()"));

    [Fact]
    public void DateTimeAddPreservesCalendar()
        => Assert.Equal("gregory:2001", Eval(
            "const d = new Temporal.PlainDateTime(2000, 3, 6, 12, 0, 0, 0, 0, 0, 'gregory').add({ years: 1 });" +
            "d.calendarId + ':' + d.year"));

    [Fact]
    public void DateTimeBuddhistFromYear()
        => Assert.Equal("2000-01-01T05:00:00:be", Eval(
            "const d = Temporal.PlainDateTime.from({ year: 2543, month: 1, day: 1, hour: 5, calendar: 'buddhist' });" +
            "d.toString({ calendarName: 'never' }) + ':' + d.era"));

    [Fact]
    public void DateTimeToPlainDatePreservesCalendar()
        => Assert.Equal("gregory:ce", Eval(
            "const d = new Temporal.PlainDateTime(2000, 3, 6, 12, 0, 0, 0, 0, 0, 'gregory').toPlainDate();" +
            "d.calendarId + ':' + d.era"));

    [Fact]
    public void DateTimeParsesCalendarAnnotation()
        => Assert.Equal("roc:89", Eval(
            "const d = Temporal.PlainDateTime.from('2000-03-06T12:00[u-ca=roc]'); d.calendarId + ':' + d.year"));

    [Fact]
    public void DateTimeSinceDifferentCalendarsThrows()
        => Assert.Equal("RangeError", ErrorName(
            "new Temporal.PlainDateTime(2000, 3, 6, 0, 0, 0, 0, 0, 0, 'gregory')" +
            ".since(new Temporal.PlainDateTime(2000, 1, 1, 0, 0, 0, 0, 0, 0, 'buddhist'));"));

    [Fact]
    public void DateTimeUnsupportedCalendarThrows()
        => Assert.Equal("RangeError", ErrorName(
            "new Temporal.PlainDateTime(2000, 1, 1, 0, 0, 0, 0, 0, 0, 'hebrew');"));

    // --- PlainYearMonth (Problem 8) ---

    [Fact]
    public void YearMonthGregoryEraAndYear()
        => Assert.Equal("gregory:ce:2000:2000", Eval(
            "const d = new Temporal.PlainYearMonth(2000, 3, 'gregory');" +
            "d.calendarId + ':' + d.era + ':' + d.eraYear + ':' + d.year"));

    [Fact]
    public void YearMonthToStringShowsReferenceDayAndCalendar()
        => Assert.Equal("2000-03-01[u-ca=gregory]", Eval("new Temporal.PlainYearMonth(2000, 3, 'gregory').toString()"));

    [Fact]
    public void YearMonthToStringNeverHidesCalendar()
        => Assert.Equal("2000-03", Eval("new Temporal.PlainYearMonth(2000, 3, 'gregory').toString({ calendarName:'never' })"));

    [Fact]
    public void YearMonthBuddhistFromYear()
        => Assert.Equal("2000-01:be", Eval(
            "const d = Temporal.PlainYearMonth.from({ year: 2543, month: 1, calendar: 'buddhist' });" +
            "d.toString({ calendarName: 'never' }) + ':' + d.era"));

    [Fact]
    public void YearMonthAddPreservesCalendar()
        => Assert.Equal("gregory:2001", Eval(
            "const d = new Temporal.PlainYearMonth(2000, 3, 'gregory').add({ years: 1 });" +
            "d.calendarId + ':' + d.year"));

    [Fact]
    public void PlainDateToPlainYearMonthPreservesCalendar()
        => Assert.Equal("gregory:ce", Eval(
            "const ym = Temporal.PlainDate.from('2000-03-15[u-ca=gregory]').toPlainYearMonth();" +
            "ym.calendarId + ':' + ym.era"));

    [Fact]
    public void YearMonthHarnessReferenceDayExpressionWorks()
        => Assert.Equal("1", Eval(
            "const ym = new Temporal.PlainYearMonth(2000, 3, 'gregory');" +
            "(Number(ym.toString({ calendarName: 'always' }).slice(1).split('-')[2].slice(0, 2))) + ''"));

    [Fact]
    public void YearMonthSinceDifferentCalendarsThrows()
        => Assert.Equal("RangeError", ErrorName(
            "new Temporal.PlainYearMonth(2000, 3, 'gregory').since(Temporal.PlainYearMonth.from({ year: 2543, month: 1, calendar: 'buddhist' }));"));

    [Fact]
    public void YearMonthUnsupportedCalendarThrows()
        => Assert.Equal("RangeError", ErrorName("new Temporal.PlainYearMonth(2000, 1, 'hebrew');"));

    // --- ZonedDateTime (Problem 4) ---

    [Fact]
    public void ZonedGregoryEraAndYear()
        => Assert.Equal("gregory:ce:1970:1970", Eval(
            "const d = new Temporal.ZonedDateTime(0n, 'UTC', 'gregory');" +
            "d.calendarId + ':' + d.era + ':' + d.eraYear + ':' + d.year"));

    [Fact]
    public void ZonedToStringShowsAnnotation()
        => Assert.Equal("1970-01-01T00:00:00+00:00[UTC][u-ca=gregory]", Eval(
            "new Temporal.ZonedDateTime(0n, 'UTC', 'gregory').toString()"));

    [Fact]
    public void ZonedToStringNeverHidesAnnotation()
        => Assert.Equal("1970-01-01T00:00:00+00:00[UTC]", Eval(
            "new Temporal.ZonedDateTime(0n, 'UTC', 'gregory').toString({ calendarName:'never' })"));

    [Fact]
    public void ZonedBuddhistFromYear()
        => Assert.Equal("2543:be", Eval(
            "const d = Temporal.ZonedDateTime.from({ year: 2543, month: 1, day: 1, timeZone: 'UTC', calendar: 'buddhist' });" +
            "d.year + ':' + d.era"));

    [Fact]
    public void ZonedWithCalendarSwitches()
        => Assert.Equal("59", Eval("new Temporal.ZonedDateTime(0n, 'UTC', 'gregory').withCalendar('roc').year + ''"));

    [Fact]
    public void ZonedWithTimeZonePreservesCalendar()
        => Assert.Equal("gregory", Eval("new Temporal.ZonedDateTime(0n, 'UTC', 'gregory').withTimeZone('UTC').calendarId"));

    [Fact]
    public void ZonedToPlainDatePreservesCalendar()
        => Assert.Equal("gregory:ce", Eval(
            "const d = new Temporal.ZonedDateTime(0n, 'UTC', 'gregory').toPlainDate(); d.calendarId + ':' + d.era"));

    [Fact]
    public void ZonedParsesCalendarAnnotation()
        => Assert.Equal("roc:89", Eval(
            "const d = Temporal.ZonedDateTime.from('2000-03-06T00:00+00:00[UTC][u-ca=roc]'); d.calendarId + ':' + d.year"));

    [Fact]
    public void ZonedEqualsConsidersCalendar()
        => Assert.Equal("false", Eval(
            "new Temporal.ZonedDateTime(0n, 'UTC', 'gregory').equals(new Temporal.ZonedDateTime(0n, 'UTC')) + ''"));

    [Fact]
    public void ZonedSinceDifferentCalendarsThrows()
        => Assert.Equal("RangeError", ErrorName(
            "new Temporal.ZonedDateTime(0n, 'UTC', 'gregory')" +
            ".since(Temporal.ZonedDateTime.from({ year: 2543, month: 1, day: 1, timeZone: 'UTC', calendar: 'buddhist' }));"));

    [Fact]
    public void ZonedUnsupportedCalendarThrows()
        => Assert.Equal("RangeError", ErrorName("new Temporal.ZonedDateTime(0n, 'UTC', 'hebrew');"));
}
