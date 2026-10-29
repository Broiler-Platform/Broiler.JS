using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Milestone SJL-10.5, part C: Temporal's durations, rounding and differences are the shared library's
// (Broiler.ECMAScript.Temporal's DurationMath and TemporalDifferences). Each test holds a correction of bundle
// sjl-10-001's correspondence that falls out of the cut-over, at the answer Broiler.VM gives through the same
// library, or the owner's decision D11 (JSD-0078).
public class SharedTemporalDurationsTests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    private static string ErrorName(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval($"let __t='NONE'; try {{ {code} }} catch (e) {{ __t = e.constructor.name; }} __t").ToString();
    }

    // Row D3: add and subtract take one parameter; calendar units are a RangeError with or without a relativeTo,
    // and a second argument is never read.
    [Theory]
    [InlineData("Temporal.Duration.from({years: 1}).add({months: 1})")]
    [InlineData("Temporal.Duration.from({years: 1}).add({months: 1}, {relativeTo: '2020-01-01'})")]
    [InlineData("Temporal.Duration.from({years: 1}).subtract({months: 1}, {relativeTo: Temporal.ZonedDateTime.from('2020-01-01T00:00Z[UTC]')})")]
    public void AddWithCalendarUnitsIsARangeError(string code)
        => Assert.Equal("RangeError", ErrorName(code));

    [Fact]
    public void AddReadsNoSecondArgument()
        => Assert.Equal("PT2H,PT0S,", Eval("""
            var reads = [];
            var a = Temporal.Duration.from({hours: 1}).add({hours: 1}, {get relativeTo() { reads.push('relativeTo'); }});
            var b = Temporal.Duration.from({hours: 1}).subtract({hours: 1}, 5);
            [a, b, reads.join()].join();
            """));

    // Row D4: compare relative to a plain date adds only the years, months and weeks (DateDurationDays), so no
    // date-time limit is involved.
    [Fact]
    public void CompareRelativeToAPlainDateHasNoDateTimeLimit()
        => Assert.Equal("1,1", Eval("""
            [Temporal.Duration.compare({years: 1, seconds: 9e15}, {years: 1}, {relativeTo: '2020-01-01'}),
             Temporal.Duration.compare({years: 1, seconds: 9e15}, {years: 1, seconds: 9e15 - 1}, {relativeTo: '2020-01-01'})].join();
            """));

    // Rows D6 and D7: rounding to months keeps the nudge's end duration, not a difference re-derived through a
    // clamped month end.
    [Theory]
    [InlineData("P1Y2M3W4DT5H6M7.008009010S", "2020-01-31", "{smallestUnit: 'month', roundingMode: 'ceil'}", "P1Y3M")]
    [InlineData("P10M29DT12H", "2019-03-31", "{smallestUnit: 'month', largestUnit: 'year', roundingMode: 'trunc'}", "P11M")]
    public void RoundToMonthsRelativeToAPlainDate(string duration, string relativeTo, string options, string expected)
        => Assert.Equal(expected, Eval($"var o = {options}; o.relativeTo = '{relativeTo}'; Temporal.Duration.from('{duration}').round(o).toString()"));

    // Row D8 (decision D7): after a shifted window, a unit rounded down keeps the window's start, as the pinned text
    // says, for a zoned relativeTo and for the differences alike.
    [Theory]
    [InlineData("Temporal.Duration.from({months: 10, days: 29, hours: 12}).round({smallestUnit: 'month', largestUnit: 'year', roundingMode: 'trunc', relativeTo: '2019-03-31T00:00[UTC]'})")]
    [InlineData("Temporal.PlainDateTime.from('2019-03-31T00:00').until('2020-02-29T12:00', {smallestUnit: 'month', largestUnit: 'year'})")]
    [InlineData("Temporal.ZonedDateTime.from('2019-03-31T00:00[UTC]').until('2020-02-29T12:00[UTC]', {smallestUnit: 'month', largestUnit: 'year'})")]
    public void TheNudgeAfterAShiftedWindowKeepsTheStart(string code)
        => Assert.Equal("P11M", Eval($"({code}).toString()"));

    // Row D11: a relativeTo in another calendar rounds and totals in that calendar.
    [Theory]
    [InlineData("Temporal.Duration.from({days: 45}).round({largestUnit: 'month', relativeTo: Temporal.PlainDate.from('2020-01-01').withCalendar('hebrew')})", "P1M16D")]
    [InlineData("Temporal.Duration.from({days: 45}).total({unit: 'month', relativeTo: Temporal.PlainDate.from('2020-01-01').withCalendar('hebrew')})", "1.5333333333333334")]
    [InlineData("Temporal.Duration.from({days: 45}).round({largestUnit: 'month', relativeTo: '2020-01-01T00:00[UTC][u-ca=coptic]'})", "P1M15D")]
    [InlineData("Temporal.Duration.from({days: 45}).round({largestUnit: 'month', relativeTo: {year: 5780, month: 4, day: 1, calendar: 'hebrew'}})", "P1M16D")]
    [InlineData("Temporal.Duration.compare({months: 1}, {days: 29}, {relativeTo: Temporal.PlainDate.from({year: 5780, month: 5, day: 1, calendar: 'hebrew'})})", "1")]
    public void ARelativeToInAnotherCalendar(string code, string expected)
        => Assert.Equal(expected, Eval($"String({code})"));

    // Row F4: the rounding options of a difference apply in every calendar.
    [Theory]
    [InlineData("Temporal.PlainDate.from('2020-01-01[u-ca=hebrew]').until(Temporal.PlainDate.from('2020-07-20[u-ca=hebrew]'), {smallestUnit: 'month', roundingMode: 'halfExpand'})", "P7M")]
    [InlineData("Temporal.PlainDate.from('2020-01-01[u-ca=coptic]').until(Temporal.PlainDate.from('2020-03-20[u-ca=coptic]'), {smallestUnit: 'week', roundingMode: 'ceil'})", "P12W")]
    [InlineData("Temporal.PlainDateTime.from('2020-01-01T00:00[u-ca=hebrew]').until(Temporal.PlainDateTime.from('2020-01-16T00:00[u-ca=hebrew]'), {smallestUnit: 'month', roundingMode: 'halfExpand'})", "P1M")]
    public void RoundingOptionsApplyInAnotherCalendar(string code, string expected)
        => Assert.Equal(expected, Eval($"({code}).toString()"));

    // Row F7: a ZonedDateTime difference rounded to days measures the zone's real days (New York's 23-hour day).
    [Fact]
    public void ZonedDayRoundingMeasuresRealDays()
        => Assert.Equal("P1D", Eval("""
            Temporal.ZonedDateTime.from('2026-03-07T12:00-05:00[America/New_York]')
              .until('2026-03-07T23:45-05:00[America/New_York]', {smallestUnit: 'day', roundingMode: 'halfExpand'}).toString();
            """));

    // Decision D11 (row D12): a day nudge window with no length - Pacific/Apia skipped 2011-12-30 - is a RangeError;
    // the window forward from the earlier date does not reach the skipped day and answers as before.
    [Theory]
    [InlineData("apia.since('2011-12-31T00:00+14:00[Pacific/Apia]', {largestUnit: 'day', smallestUnit: 'day'})")]
    [InlineData("Temporal.Duration.from('-PT16H15M').round({smallestUnit: 'day', relativeTo: apia})")]
    [InlineData("Temporal.Duration.from('-PT16H15M').total({unit: 'day', relativeTo: apia})")]
    public void ADayWindowAZoneSkipsIsARangeError(string code)
        => Assert.Equal("RangeError", ErrorName($"var apia = Temporal.ZonedDateTime.from('2011-12-31T16:15+14:00[Pacific/Apia]'); {code};"));

    [Fact]
    public void ADayWindowThatDoesNotReachTheSkippedDayAnswers()
        => Assert.Equal("PT0S,PT16H15M", Eval("""
            var apia = Temporal.ZonedDateTime.from('2011-12-31T16:15+14:00[Pacific/Apia]');
            var earlier = Temporal.ZonedDateTime.from('2011-12-31T00:00+14:00[Pacific/Apia]');
            [earlier.until(apia, {largestUnit: 'day', smallestUnit: 'day'}), apia.since(earlier, {largestUnit: 'hour'})].join();
            """));
}
