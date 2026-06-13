using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/779
//
// Fixed here (option / calendar argument coercion ordering):
//   * Problem 7 — Temporal option values (overflow, largestUnit, smallestUnit, roundingMode, unit)
//     were read with the host ToString rather than the spec ToString abstract operation, so a Symbol
//     option value produced a RangeError ("invalid unit/overflow") instead of the TypeError the spec
//     requires (a Symbol cannot be converted to a string). They are now coerced through StringValue,
//     so a Symbol raises a TypeError while an unrecognized *string* still raises a RangeError.
//   * Problem 27 — a `calendar` argument (constructor argument, `calendar` property-bag field, or
//     withCalendar argument) was coerced with the host ToString, so non-string values such as null,
//     numbers, plain objects or a Temporal.Duration produced a RangeError ("unsupported calendar")
//     instead of a TypeError. ToTemporalCalendarSlotValue now accepts only a calendar-identifier
//     string or a calendar-bearing Temporal object; anything else is a TypeError.
//
// Out of scope: the non-ISO calendars that are still unimplemented for the calendar-independent types
// (Problems 5/8/10/12/13/15/18/22/24/25/29), relativeTo rounding (Problems 2/11/16) and
// ZonedDateTime with/round arithmetic (Problems 6/16).
public class Issue779Tests
{
    private static string ErrorName(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval($"let __t='NONE'; try {{ {code} }} catch (e) {{ __t = e.constructor.name; }} __t").ToString();
    }

    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // ───────────────────── Problem 7: Symbol option value → TypeError ─────────────────────

    [Theory]
    [InlineData("Temporal.Duration.from({hours:1}).round({largestUnit: Symbol()})")]
    [InlineData("Temporal.Duration.from({hours:1}).round({smallestUnit: Symbol()})")]
    [InlineData("Temporal.Duration.from({hours:1}).round({smallestUnit:'second', roundingMode: Symbol()})")]
    [InlineData("Temporal.Duration.from({hours:1}).total({unit: Symbol()})")]
    [InlineData("Temporal.PlainDate.from({year:2000,month:1,day:1}, {overflow: Symbol()})")]
    [InlineData("Temporal.PlainDate.from('2000-01-01').add({days:1}, {overflow: Symbol()})")]
    [InlineData("Temporal.PlainDate.from('2000-01-01').since(Temporal.PlainDate.from('2001-01-01'), {largestUnit: Symbol()})")]
    [InlineData("Temporal.PlainDate.from('2000-01-01').until(Temporal.PlainDate.from('2001-01-01'), {smallestUnit: Symbol()})")]
    [InlineData("Temporal.PlainDateTime.from('2000-01-01').round({smallestUnit: Symbol()})")]
    [InlineData("Temporal.PlainTime.from('12:00').round({smallestUnit: Symbol()})")]
    [InlineData("Temporal.PlainTime.from('12:00').round({smallestUnit:'second', roundingMode: Symbol()})")]
    public void SymbolOptionValueThrowsTypeError(string code)
        => Assert.Equal("TypeError", ErrorName(code));

    // An unrecognized *string* option value is still a RangeError, not a TypeError.
    [Theory]
    [InlineData("Temporal.Duration.from({hours:1}).round({largestUnit:'bogus'})")]
    [InlineData("Temporal.PlainDate.from('2000-01-01').add({days:1}, {overflow:'bogus'})")]
    [InlineData("Temporal.PlainDateTime.from('2000-01-01').round({smallestUnit:'bogus'})")]
    public void InvalidStringOptionValueThrowsRangeError(string code)
        => Assert.Equal("RangeError", ErrorName(code));

    // ──────────────────── Problem 27: non-string calendar argument → TypeError ────────────────────

    [Theory]
    [InlineData("null")]
    [InlineData("true")]
    [InlineData("1")]
    [InlineData("1n")]
    [InlineData("19970327")]
    [InlineData("Symbol()")]
    [InlineData("{}")]
    [InlineData("new Temporal.Duration()")]
    public void CalendarPropertyBagWrongTypeThrowsTypeError(string value)
        => Assert.Equal("TypeError",
            ErrorName($"Temporal.PlainDate.from({{year:2000, month:1, day:1, calendar: {value}}})"));

    [Theory]
    [InlineData("new Temporal.PlainDate(2000, 1, 1, null)")]
    [InlineData("new Temporal.PlainDate(2000, 1, 1, {})")]
    [InlineData("Temporal.PlainDate.from('2000-01-01').withCalendar(null)")]
    [InlineData("Temporal.PlainDate.from('2000-01-01').withCalendar(5)")]
    [InlineData("Temporal.PlainMonthDay.from({month:1, day:1, calendar: null})")]
    [InlineData("Temporal.PlainDateTime.from('2000-01-01').withCalendar(7)")]
    public void CalendarArgumentWrongTypeThrowsTypeError(string code)
        => Assert.Equal("TypeError", ErrorName(code));

    // An unrecognized calendar *string* remains a RangeError.
    [Fact]
    public void UnknownCalendarStringThrowsRangeError()
        => Assert.Equal("RangeError", ErrorName("Temporal.PlainDate.from('2000-01-01').withCalendar('bogus')"));

    // A calendar-identifier string and a calendar-bearing Temporal object are still accepted, and the
    // object's calendar is adopted directly.
    [Fact]
    public void ValidCalendarStringIsAccepted()
        => Assert.Equal("gregory", Eval("Temporal.PlainDate.from('2000-01-01').withCalendar('gregory').calendarId"));

    [Fact]
    public void CalendarFromTemporalObjectIsAdopted()
        => Assert.Equal("gregory",
            Eval(@"var base = Temporal.PlainDate.from('2000-01-01').withCalendar('gregory');
                   Temporal.PlainDate.from({year:2000, month:1, day:1, calendar: base}).calendarId"));
}
