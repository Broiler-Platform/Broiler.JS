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
//   * Problem 1 (subset) — three "no exception thrown" gaps:
//       - Temporal.PlainYearMonth.since/until ignored smallestUnit / roundingIncrement / roundingMode
//         and the largestUnit/smallestUnit ordering, so invalid options passed silently; they are now
//         validated (RangeError), mirroring PlainDate.
//       - Temporal.PlainTime.prototype.toString ignored its precision options; it now honours
//         fractionalSecondDigits / smallestUnit / roundingMode (rounding the time of day) and rejects
//         invalid values with a RangeError.
//       - Temporal.PlainYearMonth.from/with accepted a non-positive `month` field (constrain clamped
//         it to 1); a month below 1 is now a RangeError regardless of overflow.
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

    // ───────── Problem 1: PlainYearMonth.since/until option validation (RangeError) ─────────

    private const string Ym = "var later = Temporal.PlainYearMonth.from('2000-06'); var earlier = Temporal.PlainYearMonth.from('1999-01');";

    [Theory]
    [InlineData("later.since(earlier, {largestUnit:'month', smallestUnit:'year'})")]
    [InlineData("later.since(earlier, {roundingIncrement:-1})")]
    [InlineData("later.since(earlier, {roundingIncrement:0})")]
    [InlineData("later.since(earlier, {roundingIncrement:NaN})")]
    [InlineData("later.since(earlier, {roundingIncrement:1e9+1})")]
    [InlineData("later.since(earlier, {roundingMode:'bogus'})")]
    [InlineData("later.since(earlier, {smallestUnit:'day'})")]
    [InlineData("later.until(earlier, {largestUnit:'month', smallestUnit:'year'})")]
    public void YearMonthDifferenceOptionsValidated(string call)
        => Assert.Equal("RangeError", ErrorName(Ym + call));

    [Fact] // default difference is unaffected (largestUnit year)
    public void YearMonthDefaultDifference()
        => Assert.Equal("1,5", Eval(Ym + "var d = later.since(earlier); [d.years, d.months].join(',')"));

    [Fact] // largestUnit month keeps everything in months
    public void YearMonthDifferenceLargestUnitMonth()
        => Assert.Equal("17", Eval(Ym + "String(later.since(earlier, {largestUnit:'month'}).months)"));

    // ───────── Problem 1: PlainTime.toString precision options ─────────

    private const string T = "var t = Temporal.PlainTime.from('12:34:56.789123456');";

    [Theory]
    [InlineData("t.toString()", "12:34:56.789123456")]
    [InlineData("t.toString({fractionalSecondDigits:0})", "12:34:56")]
    [InlineData("t.toString({fractionalSecondDigits:3})", "12:34:56.789")]
    [InlineData("t.toString({fractionalSecondDigits:6})", "12:34:56.789123")]
    [InlineData("t.toString({smallestUnit:'minute'})", "12:34")]
    [InlineData("t.toString({smallestUnit:'second'})", "12:34:56")]
    [InlineData("t.toString({smallestUnit:'millisecond'})", "12:34:56.789")]
    [InlineData("t.toString({fractionalSecondDigits:0, roundingMode:'halfExpand'})", "12:34:57")]
    public void PlainTimeToStringPrecision(string call, string expected)
        => Assert.Equal(expected, Eval(T + "String(" + call + ")"));

    [Theory]
    [InlineData("t.toString({fractionalSecondDigits:NaN})")]
    [InlineData("t.toString({fractionalSecondDigits:10})")]
    [InlineData("t.toString({roundingMode:'bogus'})")]
    [InlineData("t.toString({smallestUnit:'hour'})")]
    [InlineData("t.toString({smallestUnit:'bogus'})")]
    public void PlainTimeToStringInvalidOptionsThrowRangeError(string call)
        => Assert.Equal("RangeError", ErrorName(T + call));

    // ───────── Problem 1: PlainYearMonth non-positive month is a RangeError ─────────

    [Theory]
    [InlineData("Temporal.PlainYearMonth.from({year:1, month:-1})")]
    [InlineData("Temporal.PlainYearMonth.from({year:1, month:0})")]
    public void YearMonthNonPositiveMonthThrowsRangeError(string code)
        => Assert.Equal("RangeError", ErrorName(code));

    [Fact] // an out-of-range *upper* month still constrains by default
    public void YearMonthHighMonthConstrains()
        => Assert.Equal("0001-12", Eval("Temporal.PlainYearMonth.from({year:1, month:13}).toString()"));
}
