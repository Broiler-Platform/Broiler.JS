using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/769
//
// Implements the remaining calendar-dependent Temporal types that previously had throwing
// stub constructors: Temporal.PlainDateTime, Temporal.PlainYearMonth, Temporal.PlainMonthDay
// (full ISO-calendar surface) and Temporal.ZonedDateTime (construction / from / accessors /
// conversions over UTC, fixed-offset and named IANA zones). Only the ISO 8601 calendar is
// supported; ZonedDateTime's rounding/arithmetic methods remain unimplemented.
public class Issue769Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code)?.ToString();
    }

    // ── Temporal.PlainDateTime ────────────────────────────────────────────────────

    [Fact]
    public void PlainDateTimeConstructs()
        => Assert.Equal("1976-11-18T15:23:30", Eval(
            "new Temporal.PlainDateTime(1976, 11, 18, 15, 23, 30).toString()"));

    [Fact]
    public void PlainDateTimeAccessors()
        => Assert.Equal("1976/11/18/15/23/30/123/456/789", Eval(
            "var d = new Temporal.PlainDateTime(1976, 11, 18, 15, 23, 30, 123, 456, 789);" +
            "[d.year,d.month,d.day,d.hour,d.minute,d.second,d.millisecond,d.microsecond,d.nanosecond].join('/')"));

    [Fact]
    public void PlainDateTimeFromString()
        => Assert.Equal("2020-01-02T03:04:05.678", Eval(
            "Temporal.PlainDateTime.from('2020-01-02T03:04:05.678').toString()"));

    [Fact]
    public void PlainDateTimeFromObject()
        => Assert.Equal("2021-07-01T12:30:00", Eval(
            "Temporal.PlainDateTime.from({year:2021,month:7,day:1,hour:12,minute:30}).toString()"));

    [Fact]
    public void PlainDateTimeFromLengthAndName()
        => Assert.Equal("1function from", Eval(
            "Temporal.PlainDateTime.from.length + 'function ' + Temporal.PlainDateTime.from.name"));

    [Fact]
    public void PlainDateTimeCompare()
        => Assert.Equal("-1", Eval(
            "Temporal.PlainDateTime.compare('2020-01-01T00:00','2020-01-01T00:01') + ''"));

    [Fact]
    public void PlainDateTimeAdd()
        => Assert.Equal("2020-01-02T01:00:00", Eval(
            "new Temporal.PlainDateTime(2020,1,1,23,0,0).add({hours:2}).toString()"));

    [Fact]
    public void PlainDateTimeToPlainDateAndTime()
        => Assert.Equal("2020-05-06|07:08:09", Eval(
            "var d = Temporal.PlainDateTime.from('2020-05-06T07:08:09');" +
            "d.toPlainDate().toString() + '|' + d.toPlainTime().toString()"));

    [Fact]
    public void PlainDateTimeValueOfThrows()
        => Assert.Equal("TypeError", Eval(
            "var t; try { new Temporal.PlainDateTime(2020,1,1) + 1; } catch(e){ t = e.constructor.name; } t"));

    [Fact]
    public void PlainDateTimeBranding()
        => Assert.Equal("[object Temporal.PlainDateTime]", Eval(
            "Object.prototype.toString.call(new Temporal.PlainDateTime(2020,1,1))"));

    // ── Temporal.PlainYearMonth ───────────────────────────────────────────────────

    [Fact]
    public void PlainYearMonthConstructs()
        => Assert.Equal("2024-06", Eval("new Temporal.PlainYearMonth(2024, 6).toString()"));

    [Fact]
    public void PlainYearMonthFromString()
        => Assert.Equal("2019-11", Eval("Temporal.PlainYearMonth.from('2019-11-30').toString()"));

    [Fact]
    public void PlainYearMonthAccessors()
        => Assert.Equal("2024/2/M02/29", Eval(
            "var ym = new Temporal.PlainYearMonth(2024, 2);" +
            "[ym.year, ym.month, ym.monthCode, ym.daysInMonth].join('/')"));

    [Fact]
    public void PlainYearMonthCompare()
        => Assert.Equal("1", Eval(
            "Temporal.PlainYearMonth.compare('2024-07','2024-06') + ''"));

    [Fact]
    public void PlainYearMonthFromObject()
        => Assert.Equal("2000-12", Eval(
            "Temporal.PlainYearMonth.from({year:2000, monthCode:'M12'}).toString()"));

    [Fact]
    public void PlainYearMonthToPlainDate()
        => Assert.Equal("2024-06-15", Eval(
            "new Temporal.PlainYearMonth(2024,6).toPlainDate({day:15}).toString()"));

    // ── Temporal.PlainMonthDay ────────────────────────────────────────────────────

    [Fact]
    public void PlainMonthDayConstructs()
        => Assert.Equal("06-15", Eval("new Temporal.PlainMonthDay(6, 15).toString()"));

    [Fact]
    public void PlainMonthDayFromString()
        => Assert.Equal("02-29", Eval("Temporal.PlainMonthDay.from('--02-29').toString()"));

    [Fact]
    public void PlainMonthDayAccessors()
        => Assert.Equal("M06/15", Eval(
            "var md = new Temporal.PlainMonthDay(6, 15); md.monthCode + '/' + md.day"));

    [Fact]
    public void PlainMonthDayFromObject()
        => Assert.Equal("01-31", Eval(
            "Temporal.PlainMonthDay.from({monthCode:'M01', day:31}).toString()"));

    [Fact]
    public void PlainMonthDayToPlainDate()
        => Assert.Equal("2024-02-29", Eval(
            "Temporal.PlainMonthDay.from('--02-29').toPlainDate({year:2024}).toString()"));

    [Fact]
    public void PlainMonthDayBranding()
        => Assert.Equal("[object Temporal.PlainMonthDay]", Eval(
            "Object.prototype.toString.call(new Temporal.PlainMonthDay(1,1))"));

    // ── Temporal.ZonedDateTime ────────────────────────────────────────────────────

    [Fact]
    public void ZonedDateTimeConstructsUtc()
        => Assert.Equal("1970-01-01T00:00:00+00:00[UTC]", Eval(
            "new Temporal.ZonedDateTime(0n, 'UTC').toString()"));

    [Fact]
    public void ZonedDateTimeEpochAccessors()
        => Assert.Equal("0/0", Eval(
            "var z = new Temporal.ZonedDateTime(0n, 'UTC'); z.epochMilliseconds + '/' + z.epochNanoseconds"));

    [Fact]
    public void ZonedDateTimeFixedOffset()
        => Assert.Equal("1970-01-01T01:00:00+01:00[+01:00]", Eval(
            "new Temporal.ZonedDateTime(0n, '+01:00').toString()"));

    [Fact]
    public void ZonedDateTimeFromString()
        => Assert.Equal("2020-01-01T00:00:00+00:00[UTC]", Eval(
            "Temporal.ZonedDateTime.from('2020-01-01T00:00:00+00:00[UTC]').toString()"));

    [Fact]
    public void ZonedDateTimeFromStringFixedOffset()
        => Assert.Equal("2020-03-08T09:00:00-08:00[-08:00]", Eval(
            "Temporal.ZonedDateTime.from('2020-03-08T09:00:00-08:00[-08:00]').toString()"));

    [Fact]
    public void ZonedDateTimeLocalAccessors()
        => Assert.Equal("2020/1/1/0/0", Eval(
            "var z = Temporal.ZonedDateTime.from('2020-01-01T00:00+00:00[UTC]');" +
            "[z.year,z.month,z.day,z.hour,z.minute].join('/')"));

    [Fact]
    public void ZonedDateTimeFromPropertyBag()
        => Assert.Equal("2021-06-15T12:00:00+00:00[UTC]", Eval(
            "Temporal.ZonedDateTime.from({year:2021,month:6,day:15,hour:12,timeZone:'UTC'}).toString()"));

    [Fact]
    public void ZonedDateTimeToInstant()
        => Assert.Equal("1970-01-01T00:00:00Z", Eval(
            "new Temporal.ZonedDateTime(0n, '+05:00').toInstant().toString()"));

    [Fact]
    public void ZonedDateTimeFromLengthAndName()
        => Assert.Equal("1from", Eval(
            "Temporal.ZonedDateTime.from.length + Temporal.ZonedDateTime.from.name"));

    [Fact]
    public void ZonedDateTimeBranding()
        => Assert.Equal("[object Temporal.ZonedDateTime]", Eval(
            "Object.prototype.toString.call(new Temporal.ZonedDateTime(0n,'UTC'))"));

    [Fact]
    public void ZonedDateTimeTypeofFunction()
        => Assert.Equal("function", Eval("typeof Temporal.ZonedDateTime"));

    // ── conversions to ZonedDateTime / Instant ────────────────────────────────────

    [Fact]
    public void DateToTemporalInstant()
        => Assert.Equal("2020-01-01T00:00:00Z", Eval(
            "new Date(Date.UTC(2020,0,1)).toTemporalInstant().toString()"));

    [Fact]
    public void InstantToZonedDateTimeISO()
        => Assert.Equal("1970-01-01T01:00:00+01:00[+01:00]", Eval(
            "new Temporal.Instant(0n).toZonedDateTimeISO('+01:00').toString()"));

    [Fact]
    public void PlainDateToZonedDateTime()
        => Assert.Equal("2020-06-15T00:00:00+00:00[UTC]", Eval(
            "Temporal.PlainDate.from('2020-06-15').toZonedDateTime('UTC').toString()"));

    [Fact]
    public void PlainDateTimeToZonedDateTime()
        => Assert.Equal("2020-06-15T12:30:00+00:00[UTC]", Eval(
            "Temporal.PlainDateTime.from('2020-06-15T12:30').toZonedDateTime('UTC').toString()"));

    [Fact]
    public void PlainTimeToZonedDateTime()
        => Assert.Equal("2020-06-15T09:00:00+00:00[UTC]", Eval(
            "Temporal.PlainTime.from('09:00').toZonedDateTime({plainDate: Temporal.PlainDate.from('2020-06-15'), timeZone:'UTC'}).toString()"));

    // ── Temporal.Now ──────────────────────────────────────────────────────────────

    [Fact]
    public void NowInstantIsInstant()
        => Assert.Equal("[object Temporal.Instant]", Eval(
            "Object.prototype.toString.call(Temporal.Now.instant())"));

    [Fact]
    public void NowZonedDateTimeISOAcceptsTimeZone()
        => Assert.Equal("[object Temporal.ZonedDateTime]/UTC", Eval(
            "var z = Temporal.Now.zonedDateTimeISO('UTC');" +
            "Object.prototype.toString.call(z) + '/' + z.timeZoneId"));

    [Fact]
    public void NowPlainDateISOIsPlainDate()
        => Assert.Equal("[object Temporal.PlainDate]", Eval(
            "Object.prototype.toString.call(Temporal.Now.plainDateISO('UTC'))"));

    [Fact]
    public void NowTimeZoneIdIsString()
        => Assert.Equal("string", Eval("typeof Temporal.Now.timeZoneId()"));

    // ── Duration arithmetic (calendar-independent) ─────────────────────────────────

    [Fact]
    public void DurationTotalSeconds()
        => Assert.Equal("3661", Eval(
            "new Temporal.Duration(0,0,0,0,1,1,1).total('seconds') + ''"));

    [Fact]
    public void DurationTotalHoursFractional()
        => Assert.Equal("1.5", Eval(
            "new Temporal.Duration(0,0,0,0,1,30).total({unit:'hours'}) + ''"));

    [Fact]
    public void DurationTotalDaysFromHours()
        => Assert.Equal("2", Eval(
            "new Temporal.Duration(0,0,0,0,48).total('days') + ''"));

    [Fact]
    public void DurationRoundToHours()
        => Assert.Equal("PT2H", Eval(
            "new Temporal.Duration(0,0,0,0,1,30).round({smallestUnit:'hour'}).toString()"));

    [Fact]
    public void DurationRoundLargestUnit()
        => Assert.Equal("P1DT1H", Eval(
            "new Temporal.Duration(0,0,0,0,25).round({largestUnit:'day',smallestUnit:'hour'}).toString()"));

    [Fact]
    public void DurationRoundHalfExpandSeconds()
        => Assert.Equal("PT1M", Eval(
            "new Temporal.Duration(0,0,0,0,0,0,30).round({smallestUnit:'minute'}).toString()"));

    [Fact]
    public void DurationRoundFloor()
        => Assert.Equal("PT0S", Eval(
            "new Temporal.Duration(0,0,0,0,0,0,30).round({smallestUnit:'minute',roundingMode:'floor'}).toString()"));

    [Fact]
    public void DurationAddTimeUnits()
        => Assert.Equal("PT3H", Eval(
            "new Temporal.Duration(0,0,0,0,1).add(new Temporal.Duration(0,0,0,0,2)).toString()"));

    [Fact]
    public void DurationAddBalances()
        // largestUnit defaults to the larger operand's largest unit (minute), so the sum stays
        // in minutes rather than rolling up into hours.
        => Assert.Equal("PT90M", Eval(
            "new Temporal.Duration(0,0,0,0,0,45).add({minutes:45}).toString()"));

    [Fact]
    public void DurationSubtract()
        => Assert.Equal("PT1H", Eval(
            "new Temporal.Duration(0,0,0,0,3).subtract({hours:2}).toString()"));

    [Fact]
    public void DurationRoundCalendarUnitsNeedsRelativeTo()
        => Assert.Equal("RangeError", Eval(
            "let t; try { new Temporal.Duration(1).round({smallestUnit:'day'}); } catch(e){ t=e.constructor.name; } t"));

    [Fact]
    public void DurationTotalBlankIsZero()
        => Assert.Equal("0", Eval(
            "new Temporal.Duration().total('nanoseconds') + ''"));
}
