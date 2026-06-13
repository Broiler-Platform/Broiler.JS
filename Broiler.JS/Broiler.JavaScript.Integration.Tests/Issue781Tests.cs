using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/781
//
// Fixed here (a calendar / time-zone slot value supplied as a full Temporal ISO string):
//   * Problems 5/8 — ToTemporalCalendarSlotValue rejected a `calendar` field that was an ISO
//     date/date-time string (e.g. "2020-01-01" or the leap-second "2016-12-31T23:59:60") with
//     "unsupported calendar". Per ParseTemporalCalendarString such a string is now parsed and its
//     [u-ca=…] annotation adopted (defaulting to iso8601); a bare unrecognized string remains a
//     RangeError.
//   * Problems 16/17 — ToTemporalTimeZoneIdentifier rejected a `timeZone` field that was an ISO
//     date-time string (e.g. "2021-08-19T17:30Z" or "2016-12-31T23:59:60+00:00[UTC]") with
//     "unknown time zone". Per ParseTemporalTimeZoneString its time-zone designator — a [TimeZone]
//     annotation, a Z (UTC) designator, or a numeric UTC offset — is now extracted and canonicalized.
//   * Problem 22 — the PlainDate / PlainYearMonth / PlainMonthDay (and Duration relativeTo) string
//     parsers used a lenient ".*" time tail that accepted a fraction on the minutes or hours. The
//     tail is now the strict Temporal time grammar (a fraction only on the seconds component), so
//     fractional minutes / hours are a RangeError.
//   * Problem 15 — those parsers accepted more than one [u-ca=…] calendar annotation; a Temporal
//     string may carry at most one, so two or more (critical or not) are now a RangeError.
public class Issue781Tests
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

    // ───────────── Problems 5/8: calendar property-bag field as an ISO string ─────────────

    [Theory]
    [InlineData("2020-01-01")]                       // a bare ISO date
    [InlineData("2016-12-31T23:59:60")]              // a leap-second date-time
    [InlineData("2016-12-31T23:59:59.999999999")]    // sub-second precision
    [InlineData("2016-12-31T23:59:59+00:00[UTC]")]   // offset + time-zone annotation
    [InlineData("1970-01-01[u-ca=iso8601]")]         // an explicit iso8601 annotation
    public void CalendarPropertyBagIsoStringResolvesToIso(string calendar)
        => Assert.Equal("iso8601",
            Eval($"Temporal.PlainDate.from({{year:2019, monthCode:'M11', day:18, calendar:'{calendar}'}}).calendarId"));

    // The [u-ca=…] annotation of a calendar string is honoured, not just the iso8601 default.
    [Fact]
    public void CalendarPropertyBagIsoStringAdoptsAnnotation()
        => Assert.Equal("gregory",
            Eval("Temporal.PlainDate.from({year:2019, monthCode:'M11', day:18, calendar:'1970-01-01[u-ca=gregory]'}).calendarId"));

    // withCalendar takes the same slot value, so an ISO string works there too.
    [Theory]
    [InlineData("2020-01-01")]
    [InlineData("2016-12-31T23:59:60")]
    public void WithCalendarIsoStringResolvesToIso(string calendar)
        => Assert.Equal("iso8601",
            Eval($"Temporal.PlainDate.from('1976-11-18').withCalendar('{calendar}').calendarId"));

    // A bare, unrecognized calendar string is still a RangeError (not silently parsed).
    [Theory]
    [InlineData("Temporal.PlainDate.from({year:2019, month:11, day:18, calendar:'bogus'})")]
    [InlineData("Temporal.PlainDate.from('1976-11-18').withCalendar('not-a-calendar')")]
    public void UnknownCalendarStringThrowsRangeError(string code)
        => Assert.Equal("RangeError", ErrorName(code));

    // ───────────── Problems 16/17: timeZone slot value as an ISO string ─────────────

    [Theory]
    [InlineData("2021-08-19T17:30Z", "UTC")]                          // a Z (UTC) designator
    [InlineData("2016-12-31T23:59:60+00:00[UTC]", "UTC")]             // a [TimeZone] annotation (leap second)
    [InlineData("2021-08-19T17:30+01:00", "+01:00")]                  // a numeric UTC offset
    [InlineData("2021-08-19T17:30-08:00[America/Vancouver]", "America/Vancouver")] // annotation wins over offset
    public void TimeZoneConstructorIsoStringExtractsDesignator(string timeZone, string expected)
        => Assert.Equal(expected, Eval($"new Temporal.ZonedDateTime(0n, '{timeZone}').timeZoneId"));

    // The property-bag timeZone field accepts the same ISO strings.
    [Fact]
    public void TimeZonePropertyBagIsoStringExtractsDesignator()
        => Assert.Equal("UTC",
            Eval("Temporal.PlainDate.from('2021-08-19').toZonedDateTime('2021-08-19T17:30Z').timeZoneId"));

    // A bare, unrecognized time-zone string is still a RangeError.
    [Fact]
    public void UnknownTimeZoneStringThrowsRangeError()
        => Assert.Equal("RangeError", ErrorName("new Temporal.ZonedDateTime(0n, 'Not/AZone')"));

    // ───────────── Problem 22: a fraction is allowed only on the seconds component ─────────────

    [Theory]
    [InlineData("Temporal.PlainDate.from('2000-05-02T00:00.123456789')")]      // fractional minutes
    [InlineData("Temporal.PlainDate.from('2000-05-02T00.123456789')")]         // fractional hours
    [InlineData("Temporal.PlainYearMonth.from('2000-05-02T00:00.5')")]
    [InlineData("Temporal.PlainMonthDay.from('2000-05-02T12.5')")]
    public void FractionalMinutesOrHoursThrowsRangeError(string code)
        => Assert.Equal("RangeError", ErrorName(code));

    // A fraction on the seconds (the smallest component) is still accepted and discarded.
    [Theory]
    [InlineData("Temporal.PlainDate.from('2000-05-02T12:34:56.789').toString()", "2000-05-02")]
    [InlineData("Temporal.PlainYearMonth.from('2000-05-02T12:34:56.789').toString()", "2000-05")]
    public void FractionalSecondsAreAccepted(string code, string expected)
        => Assert.Equal(expected, Eval(code));

    // ───────────── Problem 15: more than one calendar annotation → RangeError ─────────────

    [Theory]
    [InlineData("Temporal.PlainDate.from('1970-01-01[u-ca=iso8601][u-ca=iso8601]')")]
    [InlineData("Temporal.PlainDate.from('1970-01-01[u-ca=iso8601][!u-ca=iso8601]')")]
    [InlineData("Temporal.PlainDate.from('1970-01-01[u-ca=iso8601][u-ca=gregory]')")]
    [InlineData("Temporal.PlainDateTime.from('1970-01-01T00:00[u-ca=iso8601][u-ca=iso8601]')")]
    [InlineData("Temporal.PlainYearMonth.from('1970-01[u-ca=iso8601][u-ca=iso8601]')")]
    [InlineData("Temporal.PlainMonthDay.from('01-01[u-ca=iso8601][u-ca=iso8601]')")]
    public void MultipleCalendarAnnotationsThrowsRangeError(string code)
        => Assert.Equal("RangeError", ErrorName(code));

    // A single (even critical) calendar annotation remains valid.
    [Theory]
    [InlineData("Temporal.PlainDate.from('1970-01-01[u-ca=iso8601]').calendarId", "iso8601")]
    [InlineData("Temporal.PlainDate.from('1970-01-01[!u-ca=gregory]').calendarId", "gregory")]
    public void SingleCalendarAnnotationIsAccepted(string code, string expected)
        => Assert.Equal(expected, Eval(code));
}
