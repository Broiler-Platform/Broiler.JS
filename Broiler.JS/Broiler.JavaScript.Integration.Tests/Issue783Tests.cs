using System;
using System.Globalization;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/783
//
// Fixed here:
//   * Problem 7 — a top-level / block-scoped `using` declaration crashed with a
//     NullReferenceException: FastCompiler.Scoped read scope.Function.Async when emitting the
//     disposable try/finally, but Function is null at the program / block root. A root `using` now
//     disposes synchronously (at end of script for a top-level one).
//   * Problems 6/8 — a numeric UTC offset used as a *time-zone identifier* must be minute precision
//     with valid components. A sub-minute (seconds / fractional) offset (-07:00:01, -07:00:00.1) or
//     a leap-second offset designator (the annotation [+23:59:60]) is now a RangeError; a leap
//     second in the *time* part with a valid zone designator still succeeds.
//   * Problem 5 — the persian calendar now covers the whole supported ISO range: .NET's
//     PersianCalendar (Persian years 1-9378, whose Nowruz dates match the Iranian authority table
//     used by test262's persian-new-year-dates fixture) in range, ICU's arithmetic 33-year-cycle
//     algorithm outside it (the extreme / non-positive-year cases need only round-trip + year
//     arithmetic, which the authority table does not cover that far out).
//   * Problem 1 (subset) — PlainDateTime ISODateTimeWithinLimits now compares strictly (was
//     inclusive), so a date-time exactly one day outside the instant limits — midnight at the
//     boundary date — is out of range (with({nanosecond:0}) / withPlainTime(midnight) on the minimum
//     boundary throw RangeError; withPlainTime now validates the combined date-time). PlainYearMonth
//     string parsing rejects the bare year-month format with a non-iso8601 calendar and rejects
//     annotations with a malformed (e.g. uppercase) key.
//   * Problem 9 — adding/subtracting whole years on the lunisolar calendars (chinese / dangi) now
//     preserves the month *code*, so a leap month (M03L) constrains to the matching common month
//     (M03) under overflow "constrain" and is a RangeError under "reject", instead of keeping the
//     bare ordinal (which produced M04).
//
// Out of scope (large, separate features): Problem 2 (Temporal.Duration with a ZonedDateTime
// relativeTo — round/total/compare) and the date-component RoundRelativeDuration the since/until
// "throws-if-rounded-date-outside-valid-iso-range" cases need; Problem 3 (Intl.DateTimeFormat
// formatting Temporal objects — toLocaleString is still an ISO stub, so the PlainDate.valueOf path
// is never reached); Problem 4 (resizable-ArrayBuffer-backed TypedArray TypeErrors, and the Temporal
// toLocaleString dateStyle+timeStyle validation that belongs with the Intl-Temporal integration).
public class Issue783Tests
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

    // ───────────── Problem 7: top-level / block-scoped `using` no longer NREs ─────────────

    [Theory]
    [InlineData("let log=[]; { using x = { [Symbol.dispose](){ log.push('d'); } }; log.push('b'); } log.join(',')", "b,d")]
    [InlineData("let o=[]; { using a={[Symbol.dispose](){o.push(1);}}; using b={[Symbol.dispose](){o.push(2);}}; } o.join(',')", "2,1")]
    public void UsingDeclarationDisposes(string code, string expected)
        => Assert.Equal(expected, Eval(code));

    // A top-level `using` disposes at the end of script evaluation (observed in a later evaluation).
    [Fact]
    public void TopLevelUsingDisposesAtScriptEnd()
    {
        using var ctx = new JSContext();
        ctx.Eval("globalThis.r=[]; using x = { [Symbol.dispose](){ r.push('g'); } }; r.push('a');");
        Assert.Equal("a,g", ctx.Eval("r.join(',')").ToString());
    }

    [Fact]
    public void UsingAllowsNullAndUndefinedInitializer()
        => Assert.Equal("ok", Eval("{ using a = null; using b = undefined; } 'ok'"));

    // ───────────── Problems 6/8: invalid time-zone designators in an ISO string ─────────────

    // A leap second inside a time-zone offset designator (annotation or bare) is not a valid zone.
    [Theory]
    [InlineData("Temporal.ZonedDateTime.from('2000-05-02T12:34:56+23:59[+23:59:60]')")]
    [InlineData("Temporal.ZonedDateTime.from({year:2000,month:5,day:2,hour:12,timeZone:'2000-05-02T12:34:56+23:59[+23:59:60]'})")]
    public void LeapSecondInTimeZoneOffsetThrows(string code)
        => Assert.Equal("RangeError", ErrorName(code));

    // A sub-minute (seconds / fractional) offset is not a valid time-zone identifier.
    [Theory]
    [InlineData("'2021-08-19T17:30-07:00:01'")]
    [InlineData("'2021-08-19T17:30-07:00:00.1'")]
    [InlineData("'2021-08-19T17:30'")] // no zone designator at all
    public void SubMinuteOffsetTimeZoneThrows(string s)
        => Assert.Equal("RangeError", ErrorName($"Temporal.Instant.fromEpochMilliseconds(0).toZonedDateTimeISO({s})"));

    // A leap second in the *time* with a valid zone designator is still fine (collapses to :59).
    [Fact]
    public void LeapSecondInTimeWithValidZoneSucceeds()
        => Assert.Equal("UTC", Eval("Temporal.ZonedDateTime.from('2016-12-31T23:59:60+00:00[UTC]').timeZoneId"));

    // A minute-precision offset designator is a valid offset time zone.
    [Theory]
    [InlineData("'2021-08-19T17:30-07:00'", "-07:00")]
    [InlineData("'2021-08-19T17:30Z'", "UTC")]
    public void MinutePrecisionOffsetTimeZoneSucceeds(string s, string expected)
        => Assert.Equal(expected, Eval($"Temporal.Instant.fromEpochMilliseconds(0).toZonedDateTimeISO({s}).timeZoneId"));

    // ───────────── Problem 4 (subset): toLocaleString style incompatibility ─────────────

    [Theory]
    [InlineData("Temporal.PlainDate.from('2020-01-01').toLocaleString('en', {dateStyle:'full', timeStyle:'full'})")]
    [InlineData("Temporal.PlainDate.from('2020-01-01').toLocaleString('en', {timeStyle:'short'})")]
    [InlineData("Temporal.PlainYearMonth.from('2020-01').toLocaleString('en', {dateStyle:'full', timeStyle:'full'})")]
    [InlineData("Temporal.PlainMonthDay.from('01-01').toLocaleString('en', {timeStyle:'short'})")]
    [InlineData("Temporal.PlainTime.from('12:30').toLocaleString('en', {dateStyle:'full', timeStyle:'full'})")]
    [InlineData("Temporal.PlainTime.from('12:30').toLocaleString('en', {dateStyle:'short'})")]
    public void ToLocaleStringIncompatibleStyleThrows(string code)
        => Assert.Equal("TypeError", ErrorName(code));

    // A compatible style (or no options) does not throw.
    [Theory]
    [InlineData("Temporal.PlainDate.from('2020-01-01').toLocaleString('en', {dateStyle:'full'})")]
    [InlineData("Temporal.PlainTime.from('12:30').toLocaleString('en', {timeStyle:'short'})")]
    [InlineData("Temporal.PlainDate.from('2020-01-01').toLocaleString()")]
    public void ToLocaleStringCompatibleStyleSucceeds(string code)
        => Assert.Equal("NONE", ErrorName(code));

    // ───────────── Problem 4 (subset): Iterator.prototype.constructor setter ─────────────

    [Theory]
    [InlineData("Object.getOwnPropertyDescriptor(Iterator.prototype,'constructor').set.call(undefined,'')")]
    [InlineData("Object.getOwnPropertyDescriptor(Iterator.prototype,'constructor').set.call(null,'')")]
    [InlineData("Object.getOwnPropertyDescriptor(Iterator.prototype,'constructor').set.call(true,'')")]
    [InlineData("Object.getOwnPropertyDescriptor(Iterator.prototype,'constructor').set.call(Iterator.prototype,'')")]
    [InlineData("Iterator.prototype.constructor = ''")]
    public void IteratorConstructorSetterThrows(string code)
        => Assert.Equal("TypeError", ErrorName(code));

    // On any other object the setter creates an own data property (it does not recurse through the
    // inherited accessor).
    [Fact]
    public void IteratorConstructorSetterCreatesOwnProperty()
        => Assert.Equal("x", Eval(
            "let o=Object.create(Iterator.prototype);" +
            "Object.getOwnPropertyDescriptor(Iterator.prototype,'constructor').set.call(o,'x');" +
            "o.constructor"));

    // ───────────── Problem 5: persian calendar over the full ISO range ─────────────

    [Fact]
    public void PersianExtremeDatesRoundTrip()
    {
        // Minimum / maximum of the supported ISO range, expressed in the persian calendar.
        Assert.Equal("-272442-1-9",
            Eval("let d=Temporal.PlainDate.from({year:-272442,monthCode:'M01',day:9,calendar:'persian'});" +
                 "[d.year,d.month,d.day].join('-')"));
        Assert.Equal("275139-7-12",
            Eval("let d=Temporal.PlainDate.from({year:275139,monthCode:'M07',day:12,calendar:'persian'});" +
                 "[d.year,d.month,d.day].join('-')"));
    }

    [Fact]
    public void PersianNegativeYearRoundTrips()
        => Assert.Equal("-621-10-11", // numbers print unpadded
            Eval("let d=Temporal.PlainDate.from({era:'ap',eraYear:-621,month:10,day:11,calendar:'persian'});" +
                 "[d.year,d.month,d.day].join('-')"));

    // ───────────── Problem 1 (subset): combined date-time out of ISO range ─────────────

    [Theory]
    [InlineData("new Temporal.PlainDateTime(-271821,4,19,0,0,0,0,0,1).withPlainTime(new Temporal.PlainTime())")]
    [InlineData("new Temporal.PlainDateTime(-271821,4,19,0,0,0,0,0,1).withPlainTime()")]
    [InlineData("new Temporal.PlainDateTime(-271821,4,19,0,0,0,0,0,1).with({nanosecond:0})")]
    public void CombinedDateTimeOutOfRangeThrows(string code)
        => Assert.Equal("RangeError", ErrorName(code));

    // A within-range withPlainTime still works.
    [Fact]
    public void WithPlainTimeInRangeSucceeds()
        => Assert.Equal("2020-01-01T12:30:00",
            Eval("new Temporal.PlainDateTime(2020,1,1).withPlainTime(new Temporal.PlainTime(12,30)).toString()"));

    // ───────────── Problem 1 (subset): invalid PlainYearMonth strings ─────────────

    [Theory]
    [InlineData("2020-13")]
    [InlineData("1976-11[u-ca=gregory]")]   // bare year-month + non-iso calendar
    [InlineData("1976-11[u-ca=hebrew]")]
    [InlineData("1976-11[U-CA=iso8601]")]    // malformed (uppercase) annotation key
    [InlineData("1976-11[u-CA=iso8601]")]
    [InlineData("1976-11[FOO=bar]")]
    [InlineData("+999999-01")]
    [InlineData("-999999-01")]
    public void InvalidYearMonthStringThrows(string s)
        => Assert.Equal("RangeError", ErrorName($"Temporal.PlainYearMonth.from('{s}')"));

    [Theory]
    [InlineData("1976-11", "1976-11")]
    [InlineData("1976-11[u-ca=iso8601]", "1976-11")]
    [InlineData("1976-11-18[u-ca=gregory]", "1976-11-01[u-ca=gregory]")] // full date + non-iso calendar is allowed
    public void ValidYearMonthStringSucceeds(string s, string expected)
        => Assert.Equal(expected, Eval($"Temporal.PlainYearMonth.from('{s}').toString()"));

    // ───────────── Problem 9: leap-month year arithmetic (chinese / dangi) ─────────────

    private static string ChineseAdd(string from, string dur, string opts = "")
        => Eval($"let d=Temporal.PlainDate.from({{{from},calendar:'chinese'}}).add({dur}{opts});" +
                "[d.year,d.monthCode,d.day].join('/')");

    [Fact]
    public void AddYearToLeapMonthConstrainsToCommonMonth()
        // 1966-M03L lands in 1967, which has no leap third month, so it constrains to M03.
        => Assert.Equal("1967/M03/1", ChineseAdd("year:1966,monthCode:'M03L',day:1", "{years:1}"));

    [Fact]
    public void SubtractYearFromLeapMonthConstrainsToCommonMonth()
        => Assert.Equal("1965/M03/1",
            Eval("let d=Temporal.PlainDate.from({year:1966,monthCode:'M03L',day:1,calendar:'chinese'}).subtract({years:1});" +
                 "[d.year,d.monthCode,d.day].join('/')"));

    [Fact]
    public void AddYearToLeapMonthDayConstrained()
        // 1938-M07L-30 -> 1939-M07-29 (the common seventh month of 1939 has 29 days).
        => Assert.Equal("1939/M07/29", ChineseAdd("year:1938,monthCode:'M07L',day:30", "{years:1}"));

    [Fact]
    public void AddYearToLeapMonthRejectThrows()
        => Assert.Equal("RangeError",
            ErrorName("Temporal.PlainDate.from({year:1966,monthCode:'M03L',day:1,calendar:'chinese'}).add({years:1}, {overflow:'reject'})"));

    [Fact]
    public void AddMonthsAcrossLeapMonth()
        // 2019-M04 + 13 months -> 2020-M04L (2020 has a leap fourth month).
        => Assert.Equal("2020/M04L/1", ChineseAdd("year:2019,monthCode:'M04',day:1", "{months:13}"));

    // The arithmetic algorithm must agree with System.Globalization.PersianCalendar (the previous
    // backend, verified against the Iranian authority table) across its supported overlap, so the
    // persian-calendar-authority Nowrúz fixtures keep passing.
    [Fact]
    public void PersianMatchesDotNetOverOverlap()
    {
        var pc = new PersianCalendar();
        var epoch1970 = new DateTime(1970, 1, 1);
        using var ctx = new JSContext();
        // Sample one date per year across the .NET-supported range.
        for (int y = 1; y <= 9377; y += 7)
        {
            var dt = pc.ToDateTime(y, 1, 1, 0, 0, 0, 0);
            long iso = (long)(dt.Date - epoch1970).TotalDays;
            var isoStr = epoch1970.AddDays(iso).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var got = ctx.Eval(
                $"let d=Temporal.PlainDate.from({{year:{y},monthCode:'M01',day:1,calendar:'persian'}});" +
                "d.withCalendar('iso8601').toString()").ToString();
            Assert.Equal(isoStr, got);
        }
    }
}
