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
//
// Out of scope (large, separate features documented across the prior Temporal issues): Problems 1/2
// (Temporal.Duration with a ZonedDateTime relativeTo + RoundRelativeDuration; the since/until
// rounded-date-out-of-range cases); Problem 3 (resizable-ArrayBuffer-backed TypedArrays;
// Atomics cannot-suspend; the %IteratorHelperPrototype% brand check); Problem 4 (Iterator-helper
// map/filter/flatMap close-iterator-once and the dynamic-import / DateTimeFormat slices); Problem 7
// (PlainYearMonth.subtract reference-day + overflow:reject leap-day semantics, per non-ISO
// calendar); Problem 9 (missing ZDT getTimeZoneTransition/withPlainTime and iterator Symbol.dispose
// surfaces); Problem 10 (Promise.all/race iterator no-close).
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
}
