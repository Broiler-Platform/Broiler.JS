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
//   * Problem 28 — the ISO string parsers accepted the U+2212 minus sign (in the year sign and the
//     UTC offset). Only the ASCII hyphen-minus is valid, so a U+2212 now yields a RangeError.
//   * Problem 17 — a six-digit extended year of "-000000" (negative zero) was accepted; it is now a
//     RangeError, while year zero ("0000" / "+000000") and ordinary negative years stay valid.
//   * Problem 30 — Temporal.PlainDate/PlainDateTime.prototype.with on a Gregorian-family non-ISO
//     calendar completed a partial era pair ({era} or {eraYear} alone) from the receiver instead of
//     rejecting it; era and eraYear must be supplied together, so a partial pair is now a TypeError.
//   * Problem 26 — two remaining string-option paths ignored their option entirely:
//     PlainDateTime.toZonedDateTime (disambiguation) and PlainMonthDay.toString (calendarName) now
//     read and validate the option (null/invalid → RangeError, Symbol → TypeError) and honour valid
//     values. (The PlainTime.toString and PlainYearMonth.since/until cases were fixed above.)
//   * Problem 9 — Temporal.PlainMonthDay rejected every non-ISO calendar. The Gregorian-family
//     calendars (gregory, buddhist, roc, japanese) share the ISO month/day structure, so the
//     month-day now carries the calendar id (from / with / constructor / parsing / calendarId /
//     equals / toString). The lunisolar and arithmetic non-ISO calendars remain unimplemented here.
//   * Problems 5/8/10/12/13/15/18/22/24/25/29 (accessor half) — Temporal.ZonedDateTime rejected the
//     arithmetic / lunisolar calendars at Canonicalize. It now accepts them and derives its date
//     fields (era/eraYear/year/month/monthCode/day/dayOfYear/daysInMonth/daysInYear/monthsInYear/
//     inLeapYear) through the same TemporalCalendarMath as PlainDate. Calendar/DST arithmetic
//     (add/subtract/with/round) on ZonedDateTime remains unimplemented (Problems 6/16).
//
// Out of scope: calendar/DST arithmetic on ZonedDateTime (add/subtract/with/round — Problems 6/16),
// relativeTo duration rounding (Problems 2/11/14) and the non-ISO calendars for PlainMonthDay
// (lunisolar/arithmetic families).
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

    // ───────── Problem 28: U+2212 variant minus sign is rejected (RangeError) ─────────
    // − appears in both the UTC offset and the (extended) year sign position.

    [Theory]
    [InlineData("Temporal.PlainDate.from('1976-11-18T15:23:30.12−02:00')")]
    [InlineData("Temporal.PlainDate.from('−009999-11-18T15:23:30.12')")]
    [InlineData("Temporal.PlainDateTime.from('−009999-11-18')")]
    [InlineData("Temporal.PlainYearMonth.from('−009999-11')")]
    [InlineData("Temporal.PlainMonthDay.from('−009999-11-18')")]
    [InlineData("Temporal.Instant.from('1976-11-18T15:23:30.12−02:00')")]
    [InlineData("Temporal.Duration.from('−PT1H')")]
    public void VariantMinusSignThrowsRangeError(string code)
        => Assert.Equal("RangeError", ErrorName(code));

    [Fact] // the ASCII hyphen-minus offset is still accepted
    public void AsciiMinusOffsetAccepted()
        => Assert.Equal("1976-11-18", Eval("Temporal.PlainDate.from('1976-11-18T15:23:30.12-02:00').toString()"));

    // ───────── Problem 17: minus-zero extended year (-000000) is rejected ─────────

    [Theory]
    [InlineData("Temporal.PlainDate.from('-000000-10-31')")]
    [InlineData("Temporal.PlainDate.from('-000000-10-31T00:45')")]
    [InlineData("Temporal.PlainDateTime.from('-000000-10-31')")]
    [InlineData("Temporal.PlainYearMonth.from('-000000-10')")]
    [InlineData("Temporal.Instant.from('-000000-10-31T00:45+01:00')")]
    public void MinusZeroExtendedYearThrowsRangeError(string code)
        => Assert.Equal("RangeError", ErrorName(code));

    [Theory] // year zero is representable; only the *minus-zero* six-digit form is invalid
    [InlineData("Temporal.PlainDate.from('0000-10-31').toString()", "0000-10-31")]
    [InlineData("Temporal.PlainDate.from('+000000-10-31').toString()", "0000-10-31")]
    [InlineData("Temporal.PlainDate.from('-009999-11-18').toString()", "-009999-11-18")]
    public void YearZeroAndNegativeYearsAccepted(string call, string expected)
        => Assert.Equal(expected, Eval(call));

    // ───────── Problem 30: era / eraYear must be supplied together in .with() ─────────
    // On a Gregorian-family non-ISO calendar, a partial era pair must NOT be completed from the
    // receiver — it is a TypeError.

    private const string Greg = "var d = Temporal.PlainDate.from({year:2024, month:6, day:15, calendar:'gregory'});";

    [Theory]
    [InlineData("d.with({eraYear:1})")]
    [InlineData("d.with({era:'bce'})")]
    public void PartialEraPairInWithThrowsTypeError(string call)
        => Assert.Equal("TypeError", ErrorName(Greg + call));

    [Fact] // era + eraYear together is accepted (bce 1 == ISO year 0)
    public void EraAndEraYearTogetherAccepted()
        => Assert.Equal("0,bce,1", Eval(Greg + "var r = d.with({era:'bce', eraYear:1}); [r.year, r.era, r.eraYear].join(',')"));

    [Fact] // a plain year supersedes the era pair
    public void YearSupersedesEraPair()
        => Assert.Equal("-2,bce,3", Eval(Greg + "var r = d.with({year:-2}); [r.year, r.era, r.eraYear].join(',')"));

    [Fact] // PlainDateTime behaves the same
    public void PlainDateTimePartialEraPairThrowsTypeError()
        => Assert.Equal("TypeError",
            ErrorName("Temporal.PlainDateTime.from({year:2024, month:6, day:15, calendar:'gregory'}).with({eraYear:1})"));

    // ───────── Problem 26: null (and other non-string) option values → RangeError ─────────
    // null stringifies to "null", an invalid value for these string options (a Symbol is a TypeError,
    // covered above). These paths previously ignored the option entirely.

    [Theory]
    [InlineData("Temporal.PlainTime.from('12:00').toString({roundingMode: null})")]
    [InlineData("Temporal.PlainTime.from('12:00').toString({smallestUnit: null})")]
    [InlineData("var l=Temporal.PlainYearMonth.from('2000-06'),e=Temporal.PlainYearMonth.from('1999-01'); l.since(e,{roundingIncrement:null})")]
    [InlineData("var l=Temporal.PlainYearMonth.from('2000-06'),e=Temporal.PlainYearMonth.from('1999-01'); l.since(e,{roundingMode:null})")]
    [InlineData("var l=Temporal.PlainYearMonth.from('2000-06'),e=Temporal.PlainYearMonth.from('1999-01'); l.until(e,{smallestUnit:null})")]
    [InlineData("Temporal.PlainDateTime.from('2000-01-01T12:00').toZonedDateTime('UTC', {disambiguation: null})")]
    [InlineData("Temporal.PlainMonthDay.from('01-15').toString({calendarName: null})")]
    public void NullOptionValueThrowsRangeError(string code)
        => Assert.Equal("RangeError", ErrorName(code));

    [Theory] // the disambiguation / calendarName options are honoured for valid values
    [InlineData("Temporal.PlainMonthDay.from('01-15').toString({calendarName:'always'})", "01-15[u-ca=iso8601]")]
    [InlineData("Temporal.PlainMonthDay.from('01-15').toString({calendarName:'critical'})", "01-15[!u-ca=iso8601]")]
    [InlineData("Temporal.PlainMonthDay.from('01-15').toString({calendarName:'never'})", "01-15")]
    public void PlainMonthDayToStringCalendarName(string call, string expected)
        => Assert.Equal(expected, Eval(call));

    [Fact] // disambiguation:'earlier' is accepted (compatible behaviour)
    public void ToZonedDateTimeDisambiguationAccepted()
        => Assert.Equal("2000-01-01T12:00:00+00:00[UTC]",
            Eval("Temporal.PlainDateTime.from('2000-01-01T12:00').toZonedDateTime('UTC', {disambiguation:'earlier'}).toString()"));

    // ───────── Problem 9: PlainMonthDay Gregorian-family calendar support ─────────
    // gregory/buddhist/roc/japanese share the ISO month/day structure, so the month-day carries the
    // calendar id (the lunisolar / arithmetic non-ISO calendars remain unimplemented for MonthDay).

    [Fact]
    public void PlainMonthDayCalendarIdPreserved()
        => Assert.Equal("gregory", Eval("Temporal.PlainMonthDay.from({calendar:'gregory', monthCode:'M01', day:1}).calendarId"));

    [Fact]
    public void PlainMonthDayConstructorCalendar()
        => Assert.Equal("gregory", Eval("new Temporal.PlainMonthDay(6, 15, 'gregory').calendarId"));

    [Fact]
    public void PlainMonthDayGregoryMonthAndCode()
        => Assert.Equal("M06,15,gregory",
            Eval("var m=Temporal.PlainMonthDay.from({calendar:'gregory', year:1972, month:6, day:15}); [m.monthCode, m.day, m.calendarId].join(',')"));

    [Theory] // overflow handling matches the ISO calendar (Feb 29/30)
    [InlineData("Temporal.PlainMonthDay.from({calendar:'gregory', monthCode:'M02', day:29}).day", "29")]
    [InlineData("Temporal.PlainMonthDay.from({calendar:'gregory', monthCode:'M02', day:30}).day", "29")]
    public void PlainMonthDayGregoryOverflowConstrain(string call, string expected)
        => Assert.Equal(expected, Eval(call));

    [Fact]
    public void PlainMonthDayGregoryOverflowReject()
        => Assert.Equal("RangeError",
            ErrorName("Temporal.PlainMonthDay.from({calendar:'gregory', monthCode:'M02', day:30}, {overflow:'reject'})"));

    [Theory] // toString shows the reference year + calendar annotation for non-ISO calendars
    [InlineData("Temporal.PlainMonthDay.from({calendar:'gregory', monthCode:'M06', day:15}).toString()", "1972-06-15[u-ca=gregory]")]
    [InlineData("Temporal.PlainMonthDay.from({monthCode:'M06', day:15}).toString()", "06-15")]
    [InlineData("Temporal.PlainMonthDay.from('1972-06-15[u-ca=gregory]').calendarId", "gregory")]
    public void PlainMonthDayToStringAndParse(string call, string expected)
        => Assert.Equal(expected, Eval(call));

    [Fact] // equals is calendar-sensitive
    public void PlainMonthDayEqualsCalendarSensitive()
        => Assert.Equal("false",
            Eval(@"var a=Temporal.PlainMonthDay.from({monthCode:'M06',day:15});
                   var b=Temporal.PlainMonthDay.from({calendar:'gregory',monthCode:'M06',day:15});
                   String(a.equals(b))"));

    [Fact] // a lunisolar / arithmetic calendar is still rejected for PlainMonthDay
    public void PlainMonthDayNonIsoCalendarRejected()
        => Assert.Equal("RangeError",
            ErrorName("Temporal.PlainMonthDay.from({calendar:'chinese', monthCode:'M06', day:15})"));

    // ───────── Problems 5/8/…: ZonedDateTime non-ISO calendar accessors ─────────
    // ZonedDateTime accepts the arithmetic / lunisolar calendars and derives its date fields through
    // the same calendar math as PlainDate (projecting the local wall-clock date). (Calendar/DST
    // arithmetic — add/subtract/with/round — remains unimplemented; that is Problems 6/16.)

    [Fact]
    public void ZonedDateTimeNonIsoCalendarAccepted()
        => Assert.Equal("chinese",
            Eval("Temporal.ZonedDateTime.from('2024-06-15T12:00:00+00:00[UTC][u-ca=chinese]').calendarId"));

    [Fact] // the ZonedDateTime fields match the PlainDateTime fields for the same local date/calendar
    public void ZonedDateTimeNonIsoFieldsMatchPlainDate()
        => Assert.Equal("true", Eval(
            @"var z = Temporal.ZonedDateTime.from('2024-06-15T12:00:00+00:00[UTC][u-ca=hebrew]');
              var d = Temporal.PlainDate.from('2024-06-15').withCalendar('hebrew');
              String(z.year===d.year && z.month===d.month && z.monthCode===d.monthCode && z.day===d.day
                     && z.daysInMonth===d.daysInMonth && z.daysInYear===d.daysInYear
                     && z.monthsInYear===d.monthsInYear && z.inLeapYear===d.inLeapYear)"));

    [Fact] // Gregorian-family era/eraYear still resolve on ZonedDateTime
    public void ZonedDateTimeGregoryEra()
        => Assert.Equal("ce,2024", Eval(
            "var z = Temporal.ZonedDateTime.from('2024-06-15T12:00:00+00:00[UTC][u-ca=gregory]'); [z.era, z.eraYear].join(',')"));

    [Fact] // an unknown calendar identifier is still a RangeError
    public void ZonedDateTimeUnknownCalendarRejected()
        => Assert.Equal("RangeError", ErrorName("Temporal.ZonedDateTime.from('2024-06-15T12:00+00:00[UTC][u-ca=not-a-calendar]')"));
}
