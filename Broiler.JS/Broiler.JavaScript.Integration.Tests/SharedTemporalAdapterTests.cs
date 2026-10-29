using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Milestone SJL-10.5, after review: the engine's Temporal adapter reads a Temporal object's calendar from its slot,
// reads a PlainDate or PlainDateTime as a property bag where a ZonedDateTime is wanted, and interprets an offset in
// one place (InterpretISODateTimeOffset). Each test holds the answer of the specification and of Broiler.VM, which
// reaches the same shared library.
public class SharedTemporalAdapterTests
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

    // GetTemporalCalendarIdentifierWithISODefault step 1: a Temporal object read as a property bag lends its own
    // calendar, and ToTemporalDateTime takes a ZonedDateTime through its slots.
    [Theory]
    [InlineData("Temporal.PlainDateTime.from(Temporal.ZonedDateTime.from('2020-01-15T10:00[UTC][u-ca=hebrew]')).toString()", "2020-01-15T10:00:00[u-ca=hebrew]")]
    [InlineData("Temporal.PlainYearMonth.from(Temporal.PlainDate.from('2020-01-15[u-ca=hebrew]')).toString()", "2019-12-29[u-ca=hebrew]")]
    [InlineData("Temporal.PlainMonthDay.from(Temporal.PlainDate.from('2020-01-15[u-ca=hebrew]')).toString()", "1972-12-23[u-ca=hebrew]")]
    [InlineData("Temporal.PlainDateTime.compare(Temporal.ZonedDateTime.from('2020-01-15T10:00[UTC][u-ca=hebrew]'), Temporal.PlainDateTime.from('2020-01-15T10:00[u-ca=hebrew]'))", "0")]
    [InlineData("Temporal.PlainDateTime.from('2020-01-15T10:00[u-ca=hebrew]').until(Temporal.ZonedDateTime.from('2020-01-15T10:00[UTC][u-ca=hebrew]')).toString()", "PT0S")]
    public void ATemporalObjectLendsItsOwnCalendar(string code, string expected)
        => Assert.Equal(expected, Eval(code));

    [Fact]
    public void PlainDateTimeFromAZonedDateTimeCallsNoGetter()
        => Assert.Equal("2020-01-15T10:00:00|", Eval("""
            var reads = [];
            var d = Object.getOwnPropertyDescriptor(Temporal.ZonedDateTime.prototype, 'year');
            Object.defineProperty(Temporal.ZonedDateTime.prototype, 'year', { get() { reads.push('year'); return d.get.call(this); }, configurable: true });
            var r = Temporal.PlainDateTime.from(Temporal.ZonedDateTime.from('2020-01-15T10:00[UTC]')).toString();
            Object.defineProperty(Temporal.ZonedDateTime.prototype, 'year', d);
            r + '|' + reads.join();
            """));

    // ToTemporalZonedDateTime reads a PlainDate or PlainDateTime as a property bag: its missing timeZone is the
    // TypeError, never a .NET exception, and one it carries is read.
    [Theory]
    [InlineData("Temporal.ZonedDateTime.from(Temporal.PlainDate.from('2020-01-01'))")]
    [InlineData("Temporal.ZonedDateTime.from(Temporal.PlainDateTime.from('2020-01-01T12:00'))")]
    [InlineData("Temporal.ZonedDateTime.compare(Temporal.PlainDateTime.from('2020-01-01T12:00'), Temporal.ZonedDateTime.from('2020-01-01T00:00[UTC]'))")]
    [InlineData("Temporal.ZonedDateTime.from('2020-01-01T00:00[UTC]').until(Temporal.PlainDate.from('2020-01-01'))")]
    public void AZonedDateTimeFromAPlainTypeIsATypeError(string code)
        => Assert.Equal("TypeError", ErrorName(code));

    [Fact]
    public void APlainDateCarryingATimeZoneConverts()
        => Assert.Equal("2020-01-01T00:00:00+00:00[UTC]", Eval("""
            var d = Temporal.PlainDate.from('2020-01-01');
            Object.defineProperty(d, 'timeZone', { value: 'UTC' });
            Temporal.ZonedDateTime.from(d).toString();
            """));

    // InterpretISODateTimeOffset step 7: under the offset option prefer or reject, a wall clock whose date is outside
    // CheckISODaysRange is a RangeError, in every caller alike.
    [Theory]
    [InlineData("Temporal.ZonedDateTime.from({year: -271821, month: 4, day: 19, hour: 12, offset: '-12:00', timeZone: '-12:00'})")]
    [InlineData("Temporal.ZonedDateTime.from('-271821-04-19T12:00-12:00[-12:00]')")]
    [InlineData("Temporal.Duration.from('P1D').total({unit: 'hours', relativeTo: {year: -271821, month: 4, day: 19, hour: 12, offset: '-12:00', timeZone: '-12:00'}})")]
    [InlineData("new Temporal.ZonedDateTime(-8640000000000000000000n, '-12:00').with({minute: 1})")]
    [InlineData("new Temporal.ZonedDateTime(-8640000000000000000000n, '-12:00').round('hour')")]
    public void AnOffsetOnADateOutsideTheDaysRangeIsARangeError(string code)
        => Assert.Equal("RangeError", ErrorName(code));

    [Theory]
    [InlineData("Temporal.ZonedDateTime.from({year: -271821, month: 4, day: 19, hour: 12, offset: '-12:00', timeZone: '-12:00'}, {offset: 'use'}).toString()", "-271821-04-19T12:00:00-12:00[-12:00]")]
    [InlineData("new Temporal.ZonedDateTime(-8640000000000000000000n, '-12:00').with({minute: 1}, {offset: 'ignore'}).toString()", "-271821-04-19T12:01:00-12:00[-12:00]")]
    [InlineData("Temporal.ZonedDateTime.from({year: 2020, month: 11, day: 1, hour: 1, minute: 30, offset: '-05:00', timeZone: 'America/New_York'}).toString()", "2020-11-01T01:30:00-05:00[America/New_York]")]
    [InlineData("Temporal.ZonedDateTime.from('1970-01-01T00:00-00:45[Africa/Monrovia]').offset", "-00:44:30")]
    public void AnOffsetIsInterpretedAsBefore(string code, string expected)
        => Assert.Equal(expected, Eval(code));
}
