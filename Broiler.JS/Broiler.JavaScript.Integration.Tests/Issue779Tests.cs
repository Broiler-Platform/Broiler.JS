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
//   * Problems 2/11 — Temporal.Duration.round / total with a relativeTo bailed out as "not yet
//     implemented". They now work for an ISO (or Gregorian-family) PlainDate relativeTo: the date
//     part is added to relativeTo and the time folded onto a 24h-day timeline, calendar units
//     (year/month) are rounded by nudging between the surrounding calendar boundaries, and
//     week/day/time units use their fixed nanosecond length. A ZonedDateTime relativeTo (DST) or a
//     non-ISO calendar relativeTo remains unimplemented.
//   * Problem 6 — Temporal.ZonedDateTime.prototype.with bailed out as "not yet implemented". It now
//     merges the date/time fields onto the local wall-clock datetime via the calendar-aware
//     PlainDateTime.with and re-resolves the instant in the zone, preserving the offset ("prefer")
//     and validating the overflow / offset / disambiguation options. (DST gap/overlap disambiguation
//     beyond "compatible" is still unimplemented.)
//   * Problem 16 — Temporal.ZonedDateTime.prototype.round now rounds to a day boundary using the
//     (DST-aware) local day length, or rounds the wall-clock time of day for hour/minute/second/
//     sub-second units (carrying day overflow and re-resolving the offset), validating smallestUnit
//     (day…nanosecond) and the increment.
//   * Problem 3 — the %TypedArray%.prototype methods ignored a view going out of bounds after its
//     backing resizable ArrayBuffer was shrunk (length returned 0, so they silently no-op'd). They
//     now call ValidateTypedArray and throw a TypeError when the view is out of bounds (or detached).
//   * Problems 19/23 — findLast / findLastIndex were inherited from Array.prototype (no
//     ValidateTypedArray); they are now %TypedArray% methods that validate the receiver and search
//     from the end, so an out-of-bounds view throws TypeError.
//
// Out of scope: DST gap/overlap disambiguation beyond "compatible",
// a ZonedDateTime/PlainDateTime or non-ISO-calendar relativeTo for Duration rounding, and the
// non-ISO calendars for PlainMonthDay (lunisolar/arithmetic families).
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

    [Theory] // the disambiguation / calendarName options are honoured for valid values.
    // Per TemporalMonthDayToString the reference year is emitted whenever a calendar
    // annotation is shown (calendarName 'always'/'critical') or the calendar is non-ISO.
    [InlineData("Temporal.PlainMonthDay.from('01-15').toString({calendarName:'always'})", "1972-01-15[u-ca=iso8601]")]
    [InlineData("Temporal.PlainMonthDay.from('01-15').toString({calendarName:'critical'})", "1972-01-15[!u-ca=iso8601]")]
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

    [Fact] // a lunisolar / arithmetic calendar is now supported for PlainMonthDay (issue #794):
           // it stores an ISO reference date whose chinese projection is M06-15.
    public void PlainMonthDayNonIsoCalendarAccepted()
        => Assert.Equal("M06,15", Eval(
            "(function(){var d=Temporal.PlainMonthDay.from({calendar:'chinese', monthCode:'M06', day:15});" +
            "return d.monthCode+','+d.day;})()"));

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

    // ───────── Problems 2/11: Duration.round / total with a PlainDate relativeTo ─────────
    // ISO (and Gregorian-family) PlainDate relativeTo, where a day is exactly 24h.

    private static string Dur(string ctor, string call)
        => Eval($"var d = new Temporal.Duration({ctor}); var r = d.{call}; [r.years,r.months,r.weeks,r.days,r.hours,r.minutes,r.seconds].join(',')");

    [Theory]
    [InlineData("0,11,0,396", "round({largestUnit:'years', relativeTo: new Temporal.PlainDate(2017,1,1)})", "2,0,0,0,0,0,0")]
    [InlineData("5,5,5,5,5,5,5,5,5,5", "round({smallestUnit:'years', relativeTo: new Temporal.PlainDate(2020,1,1)})", "6,0,0,0,0,0,0")]
    [InlineData("0,0,0,40", "round({smallestUnit:'months', relativeTo: new Temporal.PlainDate(2020,1,1)})", "0,1,0,0,0,0,0")]
    [InlineData("0,0,0,1,12", "round({smallestUnit:'days', largestUnit:'days', relativeTo: new Temporal.PlainDate(2020,1,1)})", "0,0,0,2,0,0,0")]
    [InlineData("0,0,0,10", "round({smallestUnit:'weeks', largestUnit:'weeks', relativeTo: new Temporal.PlainDate(2020,1,1)})", "0,0,1,0,0,0,0")]
    [InlineData("0,0,0,0,25", "round({largestUnit:'days', relativeTo: new Temporal.PlainDate(2020,1,1)})", "0,0,0,1,1,0,0")]
    public void DurationRoundRelativeTo(string ctor, string call, string expected)
        => Assert.Equal(expected, Dur(ctor, call));

    [Fact] // halfExpand is the default rounding mode, and negative durations round away from zero too
    public void DurationRoundRelativeToNegative()
        => Assert.Equal("-6,0,0,0,0,0,0",
            Dur("5,5,5,5,5,5,5,5,5,5", "negated().round({smallestUnit:'years', relativeTo: new Temporal.PlainDate(2020,1,1)})"));

    [Theory]
    [InlineData("0,1,0,0", "months", "1")]          // exactly one calendar month
    [InlineData("0,1,0,0", "days", "31")]           // January 2020 has 31 days
    [InlineData("0,1,0,15", "months", "1.5172413793103448")] // 1 + 15/29 (Feb 2020)
    public void DurationTotalRelativeTo(string ctor, string unit, string expected)
        => Assert.Equal(expected,
            Eval($"String(new Temporal.Duration({ctor}).total({{unit:'{unit}', relativeTo: new Temporal.PlainDate(2020,1,1)}}))"));

    [Fact] // a calendar-unit round without relativeTo is still a RangeError
    public void DurationRoundCalendarUnitNoRelativeToThrows()
        => Assert.Equal("RangeError", ErrorName("new Temporal.Duration(1,2,3).round({largestUnit:'years'})"));

    // ───────── Problem 6: ZonedDateTime.prototype.with ─────────

    private const string Zdt = "var z = Temporal.ZonedDateTime.from('2024-06-15T12:30:45+00:00[UTC]');";

    [Theory]
    [InlineData("z.with({year:2025})", "2025-06-15T12:30:45+00:00[UTC]")]
    [InlineData("z.with({month:1})", "2024-01-15T12:30:45+00:00[UTC]")]
    [InlineData("z.with({day:1})", "2024-06-01T12:30:45+00:00[UTC]")]
    [InlineData("z.with({hour:0})", "2024-06-15T00:30:45+00:00[UTC]")]
    [InlineData("z.with({year:2000,month:2,day:29})", "2000-02-29T12:30:45+00:00[UTC]")]
    [InlineData("z.with({month:2, day:31})", "2024-02-29T12:30:45+00:00[UTC]")]        // constrain (default)
    [InlineData("z.with({year:2025, offset:undefined})", "2025-06-15T12:30:45+00:00[UTC]")] // offset preserved
    public void ZonedDateTimeWith(string call, string expected)
        => Assert.Equal(expected, Eval(Zdt + call + ".toString()"));

    [Theory]
    [InlineData("z.with({month:2, day:31}, {overflow:'reject'})")] // day out of range
    [InlineData("z.with({calendar:'iso8601'})")]                   // calendar field rejected
    [InlineData("z.with({timeZone:'UTC'})")]                       // timeZone field rejected
    public void ZonedDateTimeWithThrows(string call)
    {
        var name = ErrorName(Zdt + call);
        Assert.True(name is "RangeError" or "TypeError", $"expected RangeError/TypeError, got {name}");
    }

    [Theory] // invalid offset / disambiguation options are RangeErrors
    [InlineData("z.with({year:2025}, {offset:'bogus'})")]
    [InlineData("z.with({year:2025}, {disambiguation:'bogus'})")]
    public void ZonedDateTimeWithInvalidOptions(string call)
        => Assert.Equal("RangeError", ErrorName(Zdt + call));

    [Fact] // a non-ISO Gregorian-family calendar year is resolved through the calendar (buddhist = ISO+543)
    public void ZonedDateTimeWithBuddhistYear()
        => Assert.Equal("2024-06-15T12:00:00+00:00[UTC][u-ca=buddhist]",
            Eval("Temporal.ZonedDateTime.from('2024-06-15T12:00:00+00:00[UTC][u-ca=buddhist]').with({year:2567}).toString()"));

    // ───────── Problem 16: ZonedDateTime.prototype.round ─────────

    private const string Zr = "var z = Temporal.ZonedDateTime.from('2024-06-15T12:34:56.789+00:00[UTC]');";

    [Theory]
    [InlineData("z.round('day')", "2024-06-16T00:00:00+00:00[UTC]")]                          // 12:34 → next midnight
    [InlineData("z.round({smallestUnit:'day', roundingMode:'floor'})", "2024-06-15T00:00:00+00:00[UTC]")]
    [InlineData("z.round('hour')", "2024-06-15T13:00:00+00:00[UTC]")]
    [InlineData("z.round('minute')", "2024-06-15T12:35:00+00:00[UTC]")]
    [InlineData("z.round('second')", "2024-06-15T12:34:57+00:00[UTC]")]
    [InlineData("z.round({smallestUnit:'minute', roundingIncrement:15})", "2024-06-15T12:30:00+00:00[UTC]")]
    public void ZonedDateTimeRound(string call, string expected)
        => Assert.Equal(expected, Eval(Zr + call + ".toString()"));

    [Fact] // rounding up at end of day carries into the next day
    public void ZonedDateTimeRoundDaySpill()
        => Assert.Equal("2024-06-16T00:00:00+00:00[UTC]",
            Eval("Temporal.ZonedDateTime.from('2024-06-15T23:59:59+00:00[UTC]').round('hour').toString()"));

    [Theory]
    [InlineData("z.round('year')")]                                       // calendar unit not allowed
    [InlineData("z.round('week')")]
    [InlineData("z.round({smallestUnit:'hour', roundingIncrement:7})")]    // 7 doesn't divide 24
    public void ZonedDateTimeRoundInvalid(string call)
        => Assert.Equal("RangeError", ErrorName(Zr + call));

    [Fact]
    public void ZonedDateTimeRoundRequiresSmallestUnit()
        => Assert.Equal("RangeError", ErrorName(Zr + "z.round({})"));

    // ───────── Problem 3: TypedArray methods on an out-of-bounds resizable buffer ─────────
    // A fixed-length view backed by a resizable ArrayBuffer goes out of bounds when the buffer is
    // shrunk below the view's extent; the %TypedArray%.prototype methods must then throw TypeError.

    private const string Oob =
        "var rab = new ArrayBuffer(16, {maxByteLength:32}); var ta = new Int32Array(rab, 0, 4); rab.resize(8); ";

    [Theory]
    [InlineData("ta.at(0)")]
    [InlineData("ta.fill(0)")]
    [InlineData("ta.every(()=>true)")]
    [InlineData("ta.forEach(()=>{})")]
    [InlineData("ta.indexOf(0)")]
    [InlineData("ta.join()")]
    [InlineData("ta.map(x=>x)")]
    [InlineData("ta.slice()")]
    [InlineData("ta.sort()")]
    [InlineData("ta.entries()")]
    [InlineData("ta.keys()")]
    [InlineData("ta.values()")]
    [InlineData("ta.copyWithin(0,1)")]
    [InlineData("ta.reverse()")]
    public void TypedArrayOutOfBoundsThrows(string call)
        => Assert.Equal("TypeError", ErrorName(Oob + call));

    [Fact] // out of bounds → length 0; regrowing the buffer restores the view
    public void TypedArrayOutOfBoundsLengthAndRegrow()
        => Assert.Equal("0,4", Eval(Oob + "var before = ta.length; rab.resize(16); [before, ta.length].join(',')"));

    [Fact] // a length-tracking view shrinks with the buffer instead of going out of bounds
    public void TypedArrayLengthTrackingShrinks()
        => Assert.Equal("4,2", Eval(
            "var rab = new ArrayBuffer(16, {maxByteLength:32}); var lt = new Int32Array(rab); var b = lt.length; rab.resize(8); [b, lt.length].join(',')"));

    [Fact] // methods on a detached buffer also throw TypeError
    public void TypedArrayDetachedThrows()
        => Assert.Equal("TypeError", ErrorName("var ab = new ArrayBuffer(8); var td = new Int32Array(ab); ab.transfer(); td.fill(0)"));

    // ───────── Problems 19/23: findLast / findLastIndex are %TypedArray% methods ─────────
    // They were inherited from Array.prototype (no ValidateTypedArray); now they are TypedArray
    // methods that validate the receiver, and they search from the end.

    [Theory]
    [InlineData("new Int32Array([5,10,15,20]).findLast(x=>x>12)", "20")]
    [InlineData("new Int32Array([5,10,15,20]).findLastIndex(x=>x>12)", "3")]
    [InlineData("new Int32Array([5,10,15,20]).findLastIndex(x=>x>99)", "-1")]
    public void TypedArrayFindLast(string call, string expected)
        => Assert.Equal(expected, Eval(call));

    [Theory] // out of bounds → TypeError (Problem 19/23 return-abrupt-from-this-out-of-bounds)
    [InlineData("ta.findLast(()=>true)")]
    [InlineData("ta.findLastIndex(()=>true)")]
    public void TypedArrayFindLastOutOfBoundsThrows(string call)
        => Assert.Equal("TypeError", ErrorName(Oob + call));
}
