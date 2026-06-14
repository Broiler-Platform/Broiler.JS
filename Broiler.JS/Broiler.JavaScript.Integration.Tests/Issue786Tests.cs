using Broiler.JavaScript.Engine;
using Xunit;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/786
//
// Fixed here:
//   * Problems 10/11 — Temporal ISO string parsing now applies the RFC 9557 annotation rules shared
//     by every Temporal type: at most one time-zone annotation (a second "[UTC][UTC]" is a
//     RangeError) and an annotation with the critical flag whose key is unrecognized
//     ("[!foo=bar]") is a RangeError. A non-critical unknown annotation ("[foo=bar]") is ignored.
//   * Problem 9 (subset) — the eager Iterator-helper terminals every/some/find perform IteratorClose
//     with a *normal* completion when they exit early, so an error thrown while reading or invoking
//     the underlying iterator's return method now propagates (it was swallowed). Iterator.prototype
//     .take now closes the underlying iterator when its limit is reached (IteratorClose on a normal
//     completion), so a throwing return propagates and the return method is actually invoked.
//   * Problem 1 (subset) — Temporal.ZonedDateTime.from(string) now validates the explicit numeric
//     offset against the named IANA time zone (InterpretISODateTimeOffset). The offset option for
//     from defaults to "reject" (not "prefer"), so an offset the zone does not yield for the local
//     time is a RangeError; "use" honours it verbatim and "ignore" drops it for the zone's offset.
//   * Problems 3/5/6/7 — Intl.DateTimeFormat now formats Temporal objects (HandleDateTimeValue):
//     format / formatToParts / formatRange accept PlainDate/PlainDateTime/PlainTime/PlainYearMonth/
//     PlainMonthDay/Instant, projecting each type's supported fields (dropping the rest, TypeError on
//     no overlap, ZonedDateTime → TypeError). Plain types format their own wall clock; Instant uses
//     the formatter's zone. Temporal.X.prototype.toLocaleString routes through the same path (so it
//     agrees with DateTimeFormat.format), and Date.prototype.toLocaleString now honours an Intl
//     options object. Engine gains weekday rendering, standalone fractional-seconds, and a
//     dateStyle/timeStyle-vs-component conflict check. (Out of scope: timeZoneName rendering, non-ISO
//     calendar/era output, the de/German default-locale style layout.)
//
// Out of scope (large, separate features documented in the issue): Problem 2 + the rest of Problem 1
// (Temporal.Duration with a ZonedDateTime relativeTo + RoundRelativeDuration; ZDT.hoursInDay
// start-of-day throw); Problems 3/6/7/8 (Intl.DateTimeFormat formatting Temporal objects —
// toLocaleString is still an ISO stub, exact-output CLDR feature); Problem 4 (resizable
// ArrayBuffer-backed TypedArrays; Atomics cannot-suspend conflicts with the engine's
// AgentCanSuspend=true design; the %IteratorHelperPrototype% brand check); Problem 5 (Temporal
// toLocaleString options); the flatMap slice of Problem 9 (lazy inner-iterator close).
public class Issue786Tests
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

    // ── Problem 10: more than one time-zone annotation ──────────────────────────

    [Theory]
    [InlineData("Temporal.PlainDateTime.from('1970-01-01T00:00[UTC][UTC]')")]
    [InlineData("Temporal.PlainDateTime.from('2020-01-01').since(Temporal.PlainDateTime.from('1970-01-01T00:00[UTC][UTC]'))")]
    [InlineData("Temporal.PlainYearMonth.from('1970-01-01T00:00[UTC][UTC]')")]
    [InlineData("Temporal.PlainMonthDay.from('1970-01-01T00:00[UTC][UTC]')")]
    [InlineData("Temporal.PlainDate.from('1970-01-01T00:00[UTC][UTC]')")]
    public void MultipleTimeZoneAnnotation_Throws(string code)
    {
        Assert.Equal("RangeError", ErrorName(code));
    }

    // ── Problem 11: unknown annotation with critical flag ───────────────────────

    [Theory]
    [InlineData("Temporal.PlainDateTime.from('1970-01-01T00:00[!foo=bar]')")]
    [InlineData("Temporal.PlainYearMonth.from('1970-01-01T00:00[!foo=bar]')")]
    [InlineData("Temporal.PlainMonthDay.from('1970-01-01T00:00[!foo=bar]')")]
    [InlineData("Temporal.PlainDate.from('1970-01-01T00:00[!foo=bar]')")]
    public void CriticalUnknownAnnotation_Throws(string code)
    {
        Assert.Equal("RangeError", ErrorName(code));
    }

    [Theory]
    // A non-critical unknown annotation is ignored; a critical u-ca annotation is recognized.
    [InlineData("Temporal.PlainDateTime.from('1970-01-01T00:00[foo=bar]').toString()", "1970-01-01T00:00:00")]
    [InlineData("Temporal.PlainDateTime.from('1970-01-01T00:00[!u-ca=iso8601]').toString()", "1970-01-01T00:00:00")]
    [InlineData("Temporal.PlainDateTime.from('1970-01-01T00:00[UTC]').toString()", "1970-01-01T00:00:00")]
    public void ValidAnnotations_Accepted(string code, string expected)
    {
        Assert.Equal(expected, Eval(code));
    }

    // ── Problem 9: Iterator-helper IteratorClose propagates a throwing return ────

    [Theory]
    [InlineData("every")]
    [InlineData("some")]
    [InlineData("find")]
    public void IteratorHelper_ReturnMethodThrows_Propagates(string method)
    {
        // The predicate makes each terminal exit early on the first element, performing IteratorClose
        // with a normal completion; the underlying iterator's return() throws and must propagate.
        var predicate = method == "every" ? "() => false" : "() => true";
        var code = $@"
            const it = {{
                next() {{ return {{ done: false, value: 1 }}; }},
                return() {{ throw new Test262Error(); }},
                [Symbol.iterator]() {{ return this; }},
            }};
            Iterator.from(it).{method}({predicate});";
        Assert.Equal("Test262Error", ErrorNameWithCustomError(code));
    }

    [Theory]
    [InlineData("every", "() => false")]
    [InlineData("some", "() => true")]
    [InlineData("find", "() => true")]
    public void IteratorHelper_GetReturnMethodThrows_Propagates(string method, string predicate)
    {
        var code = $@"
            const it = {{
                next() {{ return {{ done: false, value: 1 }}; }},
                get return() {{ throw new Test262Error(); }},
                [Symbol.iterator]() {{ return this; }},
            }};
            Iterator.from(it).{method}({predicate});";
        Assert.Equal("Test262Error", ErrorNameWithCustomError(code));
    }

    [Fact]
    public void Take_ReachingLimit_CallsUnderlyingReturn()
    {
        // take(1) reaches its limit after the first element; the underlying iterator's return() is
        // invoked (IteratorClose on a normal completion) and its throw propagates.
        var code = @"
            const it = {
                next() { return { done: false, value: 1 }; },
                return() { throw new Test262Error(); },
                [Symbol.iterator]() { return this; },
            };
            const helper = Iterator.from(it).take(1);
            helper.next();
            helper.next();";
        Assert.Equal("Test262Error", ErrorNameWithCustomError(code));
    }

    // ── Problem 1 (subset): ZonedDateTime.from offset must match the IANA time zone ──

    [Theory]
    // Default offset option for from is "reject": an explicit offset that is not one the zone yields
    // for the local time is a RangeError.
    [InlineData("Temporal.ZonedDateTime.from('2000-01-01T00:00+05:00[UTC]')")]
    [InlineData("Temporal.ZonedDateTime.from('2020-06-01T12:00-10:00[America/New_York]')")]
    [InlineData("Temporal.ZonedDateTime.from('2000-01-01T00:00+04:00[+05:00]')")]
    public void ZonedDateTimeFrom_OffsetMismatch_Throws(string code)
    {
        Assert.Equal("RangeError", ErrorName(code));
    }

    [Theory]
    // A matching offset is accepted; "use" / "ignore" bypass the match check.
    [InlineData("Temporal.ZonedDateTime.from('2000-01-01T00:00+00:00[UTC]').toString()", "2000-01-01T00:00:00+00:00[UTC]")]
    [InlineData("Temporal.ZonedDateTime.from('2020-06-01T12:00-04:00[America/New_York]').epochNanoseconds > 0n", "true")]
    [InlineData("Temporal.ZonedDateTime.from('2000-01-01T00:00+05:00[UTC]', { offset: 'use' }).toString()", "1999-12-31T19:00:00+00:00[UTC]")]
    [InlineData("Temporal.ZonedDateTime.from('2000-01-01T00:00+05:00[UTC]', { offset: 'ignore' }).toString()", "2000-01-01T00:00:00+00:00[UTC]")]
    public void ZonedDateTimeFrom_OffsetMatchOrOverride_Accepted(string code, string expected)
    {
        Assert.Equal(expected, Eval(code));
    }

    // ── Problems 3/5/6/7: Intl.DateTimeFormat formats Temporal objects ──────────

    [Theory]
    // Default en-US formatter projects each Temporal type's fields (exact CLDR output).
    [InlineData("new Intl.DateTimeFormat('en-US').format(new Temporal.PlainDate(2021, 8, 4))", "8/4/2021")]
    [InlineData("new Intl.DateTimeFormat('en-US').format(new Temporal.PlainYearMonth(2021, 8, 'gregory'))", "8/2021")]
    [InlineData("new Intl.DateTimeFormat('en-US').format(new Temporal.PlainMonthDay(8, 4, 'gregory'))", "8/4")]
    [InlineData("new Intl.DateTimeFormat('en-US').format(new Temporal.PlainDateTime(2021, 8, 4, 23, 30, 45))", "8/4/2021, 11:30:45 PM")]
    [InlineData("new Intl.DateTimeFormat('en-US').format(new Temporal.PlainTime(0, 30, 45))", "12:30:45 AM")]
    // A plain type uses its own wall clock, ignoring the formatter's time zone.
    [InlineData("new Intl.DateTimeFormat('en-US', {timeZone:'Pacific/Apia'}).format(new Temporal.PlainDate(2021, 8, 4))", "8/4/2021")]
    public void DateTimeFormat_FormatsTemporal(string code, string expected)
        => Assert.Equal(expected, Eval(code));

    [Fact]
    public void Temporal_ToLocaleString_AgreesWithDateTimeFormat()
    {
        Assert.Equal("true", Eval(@"
            const dt = new Temporal.PlainDateTime(1976, 11, 18, 15, 23, 30);
            const dtf = new Intl.DateTimeFormat('en-US', { timeZone: 'America/New_York' });
            '' + (dt.toLocaleString('en-US', { timeZone: 'America/New_York' }) === dtf.format(dt))"));
    }

    [Theory]
    // No field in common → TypeError.
    [InlineData("new Intl.DateTimeFormat('en-US', {hour:'numeric'}).format(new Temporal.PlainDate(2021,8,4))")]
    [InlineData("new Intl.DateTimeFormat('en-US', {year:'numeric'}).format(new Temporal.PlainTime(1,2,3))")]
    [InlineData("new Intl.DateTimeFormat('en-US', {day:'numeric'}).format(new Temporal.PlainYearMonth(2021,8,'gregory'))")]
    [InlineData("new Intl.DateTimeFormat('en-US', {year:'numeric'}).format(new Temporal.PlainMonthDay(8,4,'gregory'))")]
    // Style incompatible with the type → TypeError (even alongside the other style).
    [InlineData("new Temporal.PlainDate(2021,8,4).toLocaleString('en-US', {timeStyle:'short'})")]
    [InlineData("new Temporal.PlainTime(1,2,3).toLocaleString('en-US', {dateStyle:'full', timeStyle:'full'})")]
    // Component combined with a style → TypeError at construction.
    [InlineData("new Intl.DateTimeFormat('en-US', {weekday:'short', dateStyle:'short'})")]
    // A ZonedDateTime cannot be formatted directly.
    [InlineData("new Intl.DateTimeFormat('en-US').format(Temporal.ZonedDateTime.from('2021-08-04T00:00[UTC]'))")]
    // formatRange of two different Temporal types → TypeError.
    [InlineData("new Intl.DateTimeFormat('en-US').formatRange(new Temporal.PlainDate(2021,8,4), new Temporal.PlainTime(1,2,3))")]
    public void DateTimeFormat_Temporal_Throws(string code)
        => Assert.Equal("TypeError", ErrorName(code));

    [Fact]
    public void DateTimeFormat_FormatsTemporalRange()
        => Assert.Equal("8/4/2021 – 8/5/2021", Eval(
            "new Intl.DateTimeFormat('en-US').formatRange(new Temporal.PlainDate(2021,8,4), new Temporal.PlainDate(2021,8,5))"));

    [Fact]
    public void DateTimeFormat_WeekdayIsRendered()
        => Assert.Equal("Wednesday", Eval(
            "new Intl.DateTimeFormat('en-US', {weekday:'long'}).format(new Temporal.PlainDate(2021,8,4))"));

    [Fact]
    public void ZonedDateTime_ToLocaleString_FormatsInOwnZone()
        // Midnight in New York (epoch 05:00 UTC) renders as its own wall clock, proving the
        // instance's zone — not UTC — is applied.
        => Assert.Equal("1/1/1970, 12:00:00 AM", Eval(
            "Temporal.ZonedDateTime.from('1970-01-01T00:00[America/New_York]').toLocaleString('en-US')"));

    [Fact]
    public void Date_ToLocaleString_HonoursIntlOptions()
        => Assert.Equal("2021", Eval(
            "new Date(Date.UTC(2021,0,1)).toLocaleString('en-US', {year:'numeric', timeZone:'UTC'})"));

    [Fact]
    public void DateTimeFormat_TimeZoneNameOnlyOnPlainDate_DoesNotThrow()
        // timeZoneName is not a component: a plain date formats its default date, no overlap error.
        => Assert.Equal("1/5/2026", Eval(
            "new Intl.DateTimeFormat('en-US', {timeZoneName:'short'}).format(new Temporal.PlainDate(2026,1,5))"));

    private static string ErrorNameWithCustomError(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(
            "class Test262Error extends Error { get name() { return 'Test262Error'; } }" +
            $"let __t='NONE'; try {{ {code} }} catch (e) {{ __t = e.name; }} __t").ToString();
    }
}
