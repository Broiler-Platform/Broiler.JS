using Broiler.JavaScript.Engine;
using Xunit;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/788
//
// Fixed here:
//   * Problems 11/12/13/14 — Temporal.PlainTime string parsing now accepts the same trailing
//     UTC-offset forms and RFC 9557 annotations that the other Temporal types already accepted: a
//     bare-hour offset ("+00"), hour:minute(:second(.fraction)) offsets, a time-zone annotation
//     ("[Asia/Kolkata]"), and unknown/calendar annotations ("[foo=bar]", "[u-ca=iso8601]"). The
//     TimePattern regex was widened to match the PlainDateTime pattern's offset/annotation tail. A
//     date-only string (no time-of-day) is still a RangeError.
//   * Problem 8 — a time-zone annotation in a ZonedDateTime string may carry the RFC 9557 critical
//     flag ("[!UTC]"); the leading "!" is now stripped before the identifier is canonicalized
//     (previously "!UTC" was treated as an unknown zone and threw).
//   * Problems 5/6 — a Temporal.Duration field is never negative zero. Operations that compute a
//     component as "-1 * 0" (negated(), since/until differences, balancing) produced -0; the internal
//     Duration constructor now canonicalizes every component so e.g.
//     `new Temporal.Duration(0).negated().years` is +0. This fixes the Problem 6 group (ISO-calendar
//     ZonedDateTime since/until + Duration) outright. The Problem 5 intl402 "ZonedDateTime since
//     basic-<calendar>" tests get past their first (years -0) assertion but still fail on a separate,
//     deeper non-ISO-calendar day-difference bug (out of scope here).
//   * Problem 7 — Temporal.PlainYearMonth.prototype.add/subtract followed the obsolete spec, choosing
//     a reference day of "last day of month" for negative durations; that day could overflow the
//     resulting month and throw ("day out of range for resulting month"). AddDurationToYearMonth now
//     always uses a reference day of 1 (so the day never overflows; overflow only governs how the
//     year/month resolve in the target calendar — e.g. a leap month absent in the target year) and a
//     duration carrying any non-zero weeks/days/time is a RangeError ("only years and months").
//   * Problem 9 — added the missing method surfaces: ZonedDateTime.prototype.withPlainTime (replace the
//     wall-clock time, re-resolving in the zone) and getTimeZoneTransition (next/previous UTC-offset
//     transition, or null); %Iterator.prototype%[@@dispose] (closes via return()) and its accessor
//     %Iterator.prototype%[@@toStringTag] (get "Iterator" / SetterThatIgnoresPrototypeProperties);
//     %AsyncIteratorPrototype%[@@asyncDispose].
//
// Out of scope (large, separate features documented across the prior Temporal issues): Problems 1/2
// (Temporal.Duration with a ZonedDateTime relativeTo + RoundRelativeDuration; the since/until
// rounded-date-out-of-range cases); Problem 3 (resizable-ArrayBuffer-backed TypedArrays;
// Atomics cannot-suspend; the %IteratorHelperPrototype% brand check); Problem 4 (Iterator-helper
// map/filter/flatMap close-iterator-once and the dynamic-import / DateTimeFormat slices); Problem 10
// (Promise.all/race iterator no-close).
public class Issue788Tests
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

    // ── Problems 11–14: PlainTime parses offset + annotation tails ───────────────

    [Theory]
    [InlineData("Temporal.PlainTime.from('12:34:56.987654321+00').toString()", "12:34:56.987654321")]
    [InlineData("Temporal.PlainTime.from('12:34:56.987654321+01:00').toString()", "12:34:56.987654321")]
    [InlineData("Temporal.PlainTime.from('12:34:56.987654321[Asia/Kolkata]').toString()", "12:34:56.987654321")]
    [InlineData("Temporal.PlainTime.from('12:34:56.987654321[foo=bar]').toString()", "12:34:56.987654321")]
    [InlineData("Temporal.PlainTime.from('12:34:56.987654321[u-ca=iso8601]').toString()", "12:34:56.987654321")]
    [InlineData("Temporal.PlainTime.from('12:34:56.987654321+00[Asia/Kolkata]').toString()", "12:34:56.987654321")]
    public void PlainTime_OffsetAndAnnotation_Accepted(string code, string expected)
    {
        Assert.Equal(expected, Eval(code));
    }

    [Theory]
    // A date-only string has no time-of-day, so it is not a valid PlainTime.
    [InlineData("Temporal.PlainTime.from('2024-12-15')")]
    [InlineData("Temporal.PlainTime.from('2024-12-15[u-ca=iso8601]')")]
    public void PlainTime_DateOnly_Throws(string code)
    {
        Assert.Equal("RangeError", ErrorName(code));
    }

    // ── Problem 8: critical flag on a ZonedDateTime time-zone annotation ─────────

    [Theory]
    [InlineData("Temporal.ZonedDateTime.from('1970-01-01T00:00[!UTC]').timeZoneId", "UTC")]
    [InlineData("Temporal.ZonedDateTime.from('1970-01-01T00:00+00:00[!UTC]').timeZoneId", "UTC")]
    public void ZonedDateTime_CriticalTimeZoneAnnotation_Accepted(string code, string expected)
    {
        Assert.Equal(expected, Eval(code));
    }

    // ── Problems 5/6: Temporal.Duration components are never negative zero ───────

    [Theory]
    [InlineData("Object.is(new Temporal.Duration(0).negated().years, 0)")]
    [InlineData("Object.is(new Temporal.Duration(0, 1).negated().years, 0)")]
    [InlineData("Object.is(Temporal.Duration.from('PT1S').years, 0)")]
    [InlineData("Object.is(Temporal.Duration.from('-PT1S').years, 0)")]
    [InlineData("Object.is(new Temporal.Duration(1, 2, 3).negated().abs().weeks, 3)")]
    public void Duration_NoNegativeZero(string code)
    {
        Assert.Equal("true", Eval(code));
    }

    // ── Problem 7: PlainYearMonth.add/subtract reference day is always 1 ─────────

    [Theory]
    // Epagomenal (13th) month arithmetic with overflow:reject no longer overflows the reference day.
    [InlineData("Temporal.PlainYearMonth.from({year:1739, monthCode:'M13', calendar:'coptic'}, {overflow:'reject'})" +
        ".subtract(new Temporal.Duration(-1), {overflow:'reject'}).toString()", "2024-09-06[u-ca=coptic]")]
    // Adding a year onto a chinese leap month constrains to the common month under "constrain".
    [InlineData("Temporal.PlainYearMonth.from({year:1966, monthCode:'M03L', calendar:'chinese'})" +
        ".subtract(new Temporal.Duration(-1)).monthCode", "M03")]
    public void YearMonth_AddSubtract_NoDayOverflow(string code, string expected)
    {
        Assert.Equal(expected, Eval(code));
    }

    [Theory]
    // A leap month absent in the target year is a RangeError under overflow:reject.
    [InlineData("Temporal.PlainYearMonth.from({year:1966, monthCode:'M03L', calendar:'chinese'})" +
        ".subtract(new Temporal.Duration(-1), {overflow:'reject'})")]
    // Only years and months can be added to a year-month; any finer unit is a RangeError.
    [InlineData("new Temporal.PlainYearMonth(2020, 6).add({ days: 1 })")]
    [InlineData("new Temporal.PlainYearMonth(2020, 6).add({ weeks: 1 })")]
    [InlineData("new Temporal.PlainYearMonth(2020, 6).subtract({ hours: 1 })")]
    public void YearMonth_AddSubtract_RangeError(string code)
    {
        Assert.Equal("RangeError", ErrorName(code));
    }

    // ── Problem 9: previously-missing method surfaces ────────────────────────────

    [Theory]
    [InlineData("typeof Temporal.ZonedDateTime.prototype.withPlainTime", "function")]
    [InlineData("Temporal.ZonedDateTime.prototype.withPlainTime.length", "0")]
    [InlineData("Temporal.ZonedDateTime.prototype.withPlainTime.name", "withPlainTime")]
    [InlineData("typeof Temporal.ZonedDateTime.prototype.getTimeZoneTransition", "function")]
    [InlineData("Temporal.ZonedDateTime.prototype.getTimeZoneTransition.length", "1")]
    [InlineData("Temporal.ZonedDateTime.prototype.getTimeZoneTransition.name", "getTimeZoneTransition")]
    // Functional: withPlainTime keeps the date / zone and replaces the time (undefined → start of day).
    [InlineData("Temporal.ZonedDateTime.from('2020-03-15T08:30-04:00[America/New_York]')" +
        ".withPlainTime('13:45').toString()", "2020-03-15T13:45:00-04:00[America/New_York]")]
    [InlineData("Temporal.ZonedDateTime.from('2020-03-15T08:30-04:00[America/New_York]')" +
        ".withPlainTime().toPlainTime().toString()", "00:00:00")]
    // Functional: the 2020 US DST transitions are found correctly; a fixed zone has none.
    [InlineData("Temporal.ZonedDateTime.from('2020-06-01T00:00-04:00[America/New_York]')" +
        ".getTimeZoneTransition('next').toPlainDate().toString()", "2020-11-01")]
    [InlineData("Temporal.ZonedDateTime.from('2020-06-01T00:00-04:00[America/New_York]')" +
        ".getTimeZoneTransition('previous').toPlainDate().toString()", "2020-03-08")]
    [InlineData("String(Temporal.ZonedDateTime.from('2020-01-01T00:00[UTC]').getTimeZoneTransition('next'))", "null")]
    public void ZonedDateTime_NewMethods(string code, string expected)
    {
        Assert.Equal(expected, Eval(code));
    }

    [Theory]
    // %Iterator.prototype%[@@dispose] exists, closes the iterator, and has the right shape.
    [InlineData("typeof [][Symbol.iterator]()[Symbol.dispose]", "function")]
    [InlineData("[][Symbol.iterator]()[Symbol.dispose].name", "[Symbol.dispose]")]
    [InlineData("[][Symbol.iterator]()[Symbol.dispose].length", "0")]
    // @@toStringTag accessor reports "Iterator".
    [InlineData("Object.getPrototypeOf(Object.getPrototypeOf([][Symbol.iterator]()))[Symbol.toStringTag]", "Iterator")]
    public void Iterator_DisposeAndToStringTag(string code, string expected)
    {
        Assert.Equal(expected, Eval(code));
    }

    [Fact]
    public void Iterator_Dispose_CallsReturn()
    {
        // Disposing a live iterator invokes its return method (the generator finally runs).
        var code = @"
            var closed = false;
            function* g() { try { yield 1; yield 2; } finally { closed = true; } }
            var it = g();
            it.next();
            it[Symbol.dispose]();
            closed;";
        Assert.Equal("true", Eval(code));
    }

    [Fact]
    public void Iterator_ToStringTag_SetterIgnoresPrototype()
    {
        // The setter is SetterThatIgnoresPrototypeProperties: assigning on %Iterator.prototype% itself
        // throws, but assigning on a derived object creates an own property.
        var thrown = ErrorName("Object.getPrototypeOf(Object.getPrototypeOf([][Symbol.iterator]()))[Symbol.toStringTag] = 'x'");
        Assert.Equal("TypeError", thrown);

        var derived = Eval("var o = Object.create(Object.getPrototypeOf(Object.getPrototypeOf([][Symbol.iterator]())));" +
            "o[Symbol.toStringTag] = 'mine'; o[Symbol.toStringTag];");
        Assert.Equal("mine", derived);
    }
}
