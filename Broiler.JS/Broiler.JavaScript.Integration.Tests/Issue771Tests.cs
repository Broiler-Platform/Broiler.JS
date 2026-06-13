using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/771
//
// Fixed here (the calendar-independent, time-based Temporal types):
//
//   Problem 1 / Problem 5 — Temporal.Instant.prototype.{round,since,until,toString}
//   and Temporal.Duration.prototype.toString silently ignored their rounding options.
//   They now read and validate smallestUnit / largestUnit / roundingIncrement /
//   roundingMode / fractionalSecondDigits, throwing a RangeError for an out-of-range
//   or unrecognized value and a TypeError when ToString/ToNumber coercion fails (e.g.
//   a Symbol option value). The options are also honored when computing the result.
//
//   Problem 1 — Temporal.Duration.prototype.{add,subtract} now reject a result whose
//   balanced magnitude exceeds the valid-duration limits with a RangeError.
public class Issue771Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code)?.ToString();
    }

    private static string ErrorName(string body) => Eval(
        "let t='NONE'; try { " + body + " } catch (e) { t = e.constructor.name; } t");

    private const string Inst =
        "const i = Temporal.Instant.fromEpochNanoseconds(1234567890123456789n);";

    // --- Instant.round: option validation (RangeError) ---

    [Fact]
    public void InstantRoundRejectsNaNIncrement()
        => Assert.Equal("RangeError", ErrorName(Inst + "i.round({smallestUnit:'second', roundingIncrement: NaN});"));

    [Fact]
    public void InstantRoundRejectsOutOfRangeIncrement()
        => Assert.Equal("RangeError", ErrorName(Inst + "i.round({smallestUnit:'second', roundingIncrement: 86401});"));

    [Fact]
    public void InstantRoundRejectsIncrementThatDoesNotDivideEvenly()
        => Assert.Equal("RangeError", ErrorName(Inst + "i.round({smallestUnit:'second', roundingIncrement: 7});"));

    [Fact]
    public void InstantRoundRejectsInvalidRoundingMode()
        => Assert.Equal("RangeError", ErrorName(Inst + "i.round({smallestUnit:'second', roundingMode:'bogus'});"));

    [Fact]
    public void InstantRoundRejectsMissingSmallestUnit()
        => Assert.Equal("RangeError", ErrorName(Inst + "i.round({});"));

    // --- Instant.round: wrong-type options (TypeError) ---

    [Fact]
    public void InstantRoundSymbolIncrementThrowsTypeError()
        => Assert.Equal("TypeError", ErrorName(Inst + "i.round({smallestUnit:'second', roundingIncrement: Symbol()});"));

    [Fact]
    public void InstantRoundSymbolRoundingModeThrowsTypeError()
        => Assert.Equal("TypeError", ErrorName(Inst + "i.round({smallestUnit:'second', roundingMode: Symbol()});"));

    [Fact]
    public void InstantRoundSymbolSmallestUnitThrowsTypeError()
        => Assert.Equal("TypeError", ErrorName(Inst + "i.round({smallestUnit: Symbol()});"));

    // --- Instant.round: behavior ---

    [Fact]
    public void InstantRoundToSecondTruncatesSubsecond()
        => Assert.Equal("1234567890000000000", Eval(Inst + "i.round({smallestUnit:'second'}).epochNanoseconds + ''"));

    [Fact]
    public void InstantRoundToHalfHourHalfExpands()
        => Assert.Equal("2009-02-13T23:30:00Z", Eval(Inst + "i.round({smallestUnit:'minute', roundingIncrement:30}).toString()"));

    [Fact]
    public void InstantRoundAcceptsStringSmallestUnit()
        => Assert.Equal("2009-02-13T23:31:30Z", Eval(Inst + "i.round('second').toString()"));

    // --- Instant.since / until: option validation ---

    [Fact]
    public void InstantSinceRejectsLargestSmallestMismatch()
        => Assert.Equal("RangeError", ErrorName(Inst + "i.since(i, {largestUnit:'second', smallestUnit:'hour'});"));

    [Fact]
    public void InstantSinceRejectsInvalidIncrement()
        => Assert.Equal("RangeError", ErrorName(Inst + "i.since(i, {smallestUnit:'hour', roundingIncrement: 24});"));

    [Fact]
    public void InstantSinceSymbolLargestUnitThrowsTypeError()
        => Assert.Equal("TypeError", ErrorName(Inst + "i.since(i, {largestUnit: Symbol()});"));

    [Fact]
    public void InstantSinceNonObjectOptionsThrowsTypeError()
        => Assert.Equal("TypeError", ErrorName(Inst + "i.since(i, 'not-an-object');"));

    // --- Instant.since / until: behavior ---

    [Fact]
    public void InstantSinceReturnsPositiveDifference()
        => Assert.Equal("PT10S", Eval(
            Inst + "const j = Temporal.Instant.fromEpochNanoseconds(1234567880123456789n); i.since(j).toString()"));

    [Fact]
    public void InstantUntilHonorsLargestUnit()
        => Assert.Equal("PT1M5S", Eval(
            Inst + "const j = Temporal.Instant.fromEpochNanoseconds(1234567955123456789n);" +
            "i.until(j, {largestUnit:'minute'}).toString()"));

    [Fact]
    public void InstantUntilRoundsToSmallestUnit()
        => Assert.Equal("PT5M", Eval(
            Inst + "const j = Temporal.Instant.fromEpochNanoseconds(1234568190123456789n);" +
            "i.until(j, {smallestUnit:'minute', roundingMode:'halfExpand'}).toString()"));

    // --- Instant.toString: option validation ---

    [Fact]
    public void InstantToStringRejectsNaNFractionalDigits()
        => Assert.Equal("RangeError", ErrorName(Inst + "i.toString({fractionalSecondDigits: NaN});"));

    [Fact]
    public void InstantToStringRejectsInvalidRoundingMode()
        => Assert.Equal("RangeError", ErrorName(Inst + "i.toString({roundingMode:'bogus'});"));

    [Fact]
    public void InstantToStringSymbolRoundingModeThrowsTypeError()
        => Assert.Equal("TypeError", ErrorName(Inst + "i.toString({roundingMode: Symbol()});"));

    // --- Instant.toString: behavior ---

    [Fact]
    public void InstantToStringAutoTrimsFraction()
        => Assert.Equal("2009-02-13T23:31:30.123456789Z", Eval(Inst + "i.toString()"));

    [Fact]
    public void InstantToStringFixedFractionalDigits()
        => Assert.Equal("2009-02-13T23:31:30.123Z", Eval(Inst + "i.toString({fractionalSecondDigits:3})"));

    [Fact]
    public void InstantToStringZeroFractionalDigits()
        => Assert.Equal("2009-02-13T23:31:30Z", Eval(Inst + "i.toString({fractionalSecondDigits:0})"));

    [Fact]
    public void InstantToStringMinuteSmallestUnit()
        => Assert.Equal("2009-02-13T23:31Z", Eval(Inst + "i.toString({smallestUnit:'minute'})"));

    // --- Duration.toString: option validation ---

    private const string Dur =
        "const d = Temporal.Duration.from({seconds:1, milliseconds:234, microseconds:567, nanoseconds:891});";

    [Fact]
    public void DurationToStringRejectsNaNFractionalDigits()
        => Assert.Equal("RangeError", ErrorName(Dur + "d.toString({fractionalSecondDigits: NaN});"));

    [Fact]
    public void DurationToStringRejectsInvalidRoundingMode()
        => Assert.Equal("RangeError", ErrorName(Dur + "d.toString({roundingMode:'bogus'});"));

    [Fact]
    public void DurationToStringSymbolRoundingModeThrowsTypeError()
        => Assert.Equal("TypeError", ErrorName(Dur + "d.toString({roundingMode: Symbol()});"));

    [Fact]
    public void DurationToStringSymbolSmallestUnitThrowsTypeError()
        => Assert.Equal("TypeError", ErrorName(Dur + "d.toString({smallestUnit: Symbol()});"));

    [Fact]
    public void DurationToStringRejectsHourSmallestUnit()
        => Assert.Equal("RangeError", ErrorName(Dur + "d.toString({smallestUnit:'hour'});"));

    // --- Duration.toString: behavior ---

    [Fact]
    public void DurationToStringAuto()
        => Assert.Equal("PT1.234567891S", Eval(Dur + "d.toString()"));

    [Fact]
    public void DurationToStringFixedFractionalDigits()
        => Assert.Equal("PT1.234S", Eval(Dur + "d.toString({fractionalSecondDigits:3})"));

    [Fact]
    public void DurationToStringSecondSmallestUnitRoundsUp()
        => Assert.Equal("PT2S", Eval(Dur + "d.toString({smallestUnit:'second', roundingMode:'ceil'})"));

    [Fact]
    public void DurationToStringZeroFractionalDigitsTruncates()
        => Assert.Equal("PT1S", Eval(Dur + "d.toString({fractionalSecondDigits:0})"));

    // --- Duration.add / subtract: result range check ---

    [Fact]
    public void DurationAddRejectsOutOfRangeResult()
        => Assert.Equal("RangeError", ErrorName(
            "const a = Temporal.Duration.from({seconds: Number.MAX_SAFE_INTEGER});" +
            "a.add({seconds: Number.MAX_SAFE_INTEGER});"));

    // --- ZonedDateTime.until / since (Problems 2 & 3): DST-aware calendar difference ---

    private const string Zdt =
        "const a = Temporal.ZonedDateTime.from('2020-01-01T00:00:00+00:00[UTC]');" +
        "const b = Temporal.ZonedDateTime.from('2021-03-15T12:30:45+00:00[UTC]');";

    [Fact]
    public void ZonedUntilDefaultLargestUnitIsHour()
        => Assert.Equal("PT10548H30M45S", Eval(Zdt + "a.until(b).toString()"));

    [Fact]
    public void ZonedUntilLargestUnitDay()
        => Assert.Equal("P439DT12H30M45S", Eval(Zdt + "a.until(b, {largestUnit:'day'}).toString()"));

    [Fact]
    public void ZonedUntilLargestUnitWeek()
        => Assert.Equal("P62W5DT12H30M45S", Eval(Zdt + "a.until(b, {largestUnit:'week'}).toString()"));

    [Fact]
    public void ZonedUntilLargestUnitMonth()
        => Assert.Equal("P14M14DT12H30M45S", Eval(Zdt + "a.until(b, {largestUnit:'month'}).toString()"));

    [Fact]
    public void ZonedUntilLargestUnitYear()
        => Assert.Equal("P1Y2M14DT12H30M45S", Eval(Zdt + "a.until(b, {largestUnit:'year'}).toString()"));

    [Fact]
    public void ZonedSinceIsTheNegationOfUntil()
        => Assert.Equal("P1Y2M14DT12H30M45S", Eval(Zdt + "b.since(a, {largestUnit:'year'}).toString()"));

    [Fact]
    public void ZonedUntilSameInstantIsBlank()
        => Assert.Equal("PT0S", Eval(Zdt + "a.until(a).toString()"));

    [Fact]
    public void ZonedUntilNegativeDirection()
        => Assert.Equal("-P439DT12H30M45S", Eval(Zdt + "b.until(a, {largestUnit:'day'}).toString()"));

    [Fact]
    public void ZonedUntilNonObjectOptionsThrowsTypeError()
        => Assert.Equal("TypeError", ErrorName(Zdt + "a.until(b, 'nope');"));

    [Fact]
    public void ZonedUntilInvalidLargestUnitThrowsRangeError()
        => Assert.Equal("RangeError", ErrorName(Zdt + "a.until(b, {largestUnit:'bogus'});"));

    // Spring-forward: the wall clock advances two hours but only one real hour elapses.
    [Fact]
    public void ZonedUntilSpringForwardCountsRealHours()
        => Assert.Equal("PT1H", Eval(
            "const x = Temporal.ZonedDateTime.from('2020-03-08T01:30:00-05:00[America/New_York]');" +
            "const y = Temporal.ZonedDateTime.from('2020-03-08T03:30:00-04:00[America/New_York]');" +
            "x.until(y, {largestUnit:'hour'}).toString()"));

    // A calendar day spanning the spring-forward transition is still one day (a 23-hour day).
    [Fact]
    public void ZonedUntilSpringForwardDayIsOneDay()
        => Assert.Equal("P1D", Eval(
            "const x = Temporal.ZonedDateTime.from('2020-03-08T00:00:00-05:00[America/New_York]');" +
            "const y = Temporal.ZonedDateTime.from('2020-03-09T00:00:00-04:00[America/New_York]');" +
            "x.until(y, {largestUnit:'day'}).toString()"));

    // --- Problem 10: PlainYearMonth.toString honors the calendarName option ---

    [Fact]
    public void PlainYearMonthToStringAutoOmitsDayAndCalendar()
        => Assert.Equal("1976-11", Eval("new Temporal.PlainYearMonth(1976, 11).toString()"));

    [Fact]
    public void PlainYearMonthToStringAlwaysShowsReferenceDayAndCalendar()
        => Assert.Equal("1976-11-01[u-ca=iso8601]", Eval(
            "new Temporal.PlainYearMonth(1976, 11).toString({calendarName:'always'})"));

    [Fact]
    public void PlainYearMonthToStringCriticalUsesBang()
        => Assert.Equal("1976-11-01[!u-ca=iso8601]", Eval(
            "new Temporal.PlainYearMonth(1976, 11).toString({calendarName:'critical'})"));

    [Fact]
    public void PlainYearMonthToStringNeverOmitsCalendar()
        => Assert.Equal("1976-11", Eval(
            "new Temporal.PlainYearMonth(1976, 11).toString({calendarName:'never'})"));

    // The harness assertPlainYearMonth() expression that previously hit "slice of undefined".
    [Fact]
    public void PlainYearMonthHarnessReferenceDayExpressionWorks()
        => Assert.Equal("1", Eval(
            "const ym = new Temporal.PlainYearMonth(1976, 11);" +
            "(Number(ym.toString({ calendarName: 'always' }).slice(1).split('-')[2].slice(0, 2))) + ''"));

    [Fact]
    public void PlainYearMonthToStringRejectsInvalidCalendarName()
        => Assert.Equal("RangeError", ErrorName("new Temporal.PlainYearMonth(1976,11).toString({calendarName:'bogus'});"));

    [Fact]
    public void PlainYearMonthToStringRejectsNonObjectOptions()
        => Assert.Equal("TypeError", ErrorName("new Temporal.PlainYearMonth(1976,11).toString(42);"));

    // --- Problem 11: Duration.round validates roundingIncrement before the relativeTo path ---

    [Fact]
    public void DurationRoundOutOfRangeIncrementIsRangeError()
        => Assert.Equal("RangeError", ErrorName(
            "new Temporal.Duration(1).round({smallestUnit:'years', relativeTo:new Temporal.PlainDate(2000,1,1), roundingIncrement: 1e9 + 1});"));

    [Fact]
    public void DurationRoundNonDividingIncrementIsRangeError()
        => Assert.Equal("RangeError", ErrorName(
            "const d = new Temporal.Duration(5,5,5,5,5,5,5,5,5,5);" +
            "d.round({relativeTo: Temporal.PlainDate.from('2020-01-01'), smallestUnit:'hours', roundingIncrement: 11});"));

    [Fact]
    public void DurationRoundIncrementEqualToMaxIsRangeError()
        => Assert.Equal("RangeError", ErrorName(
            "const d = new Temporal.Duration(5,5,5,5,5,5,5,5,5,5);" +
            "d.round({relativeTo: Temporal.PlainDate.from('2020-01-01'), smallestUnit:'minutes', roundingIncrement: 60});"));

    // Pure-time rounding also rejects an increment that does not divide its unit evenly.
    [Fact]
    public void DurationRoundPureTimeNonDividingIncrementIsRangeError()
        => Assert.Equal("RangeError", ErrorName(
            "Temporal.Duration.from({seconds:30}).round({smallestUnit:'seconds', roundingIncrement: 7});"));

    // --- Problem 9 (Temporal subset): GetOptionsObject / ToTemporalTimeRecord TypeErrors ---

    [Fact]
    public void PlainTimeSinceRejectsPrimitiveOptions()
        => Assert.Equal("TypeError", ErrorName(
            "new Temporal.PlainTime(15,23,30).since(new Temporal.PlainTime(16,23,30), 'hello');"));

    [Fact]
    public void PlainTimeUntilRejectsNullOptions()
        => Assert.Equal("TypeError", ErrorName(
            "new Temporal.PlainTime(15,23,30).until(new Temporal.PlainTime(16,23,30), null);"));

    [Fact]
    public void PlainTimeToStringRejectsPrimitiveOptions()
        => Assert.Equal("TypeError", ErrorName("new Temporal.PlainTime(12,56,32).toString(1);"));

    [Fact]
    public void PlainTimeFromEmptyObjectIsTypeError()
        => Assert.Equal("TypeError", ErrorName("Temporal.PlainTime.from({});"));

    [Fact]
    public void PlainTimeFromOnlyPluralFieldIsTypeError()
        => Assert.Equal("TypeError", ErrorName("Temporal.PlainTime.from({minutes: 12});"));

    [Fact]
    public void PlainTimeFromIgnoresPluralFieldWhenSingularPresent()
        => Assert.Equal("00:30:00", Eval("Temporal.PlainTime.from({hours: 2, minute: 30}).toString()"));

    [Fact]
    public void PlainYearMonthWithRejectsTimeZoneField()
        => Assert.Equal("TypeError", ErrorName(
            "Temporal.PlainYearMonth.from('2019-10').with({year:2021, timeZone:'UTC'});"));
}
