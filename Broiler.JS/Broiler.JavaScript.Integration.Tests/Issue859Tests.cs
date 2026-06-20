using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/859 — test262 script-host
// failures fixed in this pass:
//
//  • Problem 3: the bare "islamic" calendar identifier is an Intl.DateTimeFormat-only locale
//    fallback. Temporal requires an unambiguous variant ("islamic-civil", "islamic-tbla",
//    "islamic-umalqura"), so "islamic" alone in Temporal must be a RangeError.
//  • Problem 4: the hebrew calendar has 12 regular months ("M01".."M12") plus the leap
//    "M05L" (Adar I); "M13" never exists and was silently being accepted, returning a
//    nonsensical month ordinal. It is now a RangeError.
//  • Problem 5: in a Temporal property bag, monthCode is a String-typed field
//    (PrepareCalendarFields "to-monthcode" conversion). A non-string value (e.g. monthCode:5)
//    is a TypeError before the format / calendar-suitability checks that raise a RangeError.
//  • Problem 14: a relativeTo property bag passed to Duration.round / Duration.total used to
//    drop the era / eraYear fields when normalising the bag, so a non-finite eraYear (e.g.
//    Infinity) never triggered its RangeError and the downstream date-fields resolution
//    raised a "missing year (or era and eraYear)" TypeError instead. They are now copied
//    (and coerced) in alphabetical order alongside the other date fields.
//  • Problem 21: BigInt.prototype.toString (radix) and BigInt.prototype.toLocaleString
//    (reserved) both have an optional parameter, so per spec their "length" property is 0
//    (not 1 / 2 as inferred from the C# argument-accessor calls).
public class Issue859Tests
{
    private static JSValue Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code);
    }

    private static string ErrorName(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(
            "(function(){ try { " + code + "; return 'no throw'; } catch (e) { return e.constructor.name; } })()")
            .ToString();
    }

    // ───────────── Problem 21: BigInt.prototype.toString / toLocaleString length ─────────────

    [Fact]
    public void BigIntPrototypeToStringLengthIsZero()
        => Assert.Equal("0", Eval("'' + BigInt.prototype.toString.length").ToString());

    [Fact]
    public void BigIntPrototypeToLocaleStringLengthIsZero()
        => Assert.Equal("0", Eval("'' + BigInt.prototype.toLocaleString.length").ToString());

    // ───────────── Problem 3: bare "islamic" calendar is rejected by Temporal ─────────────

    [Theory]
    [InlineData("Temporal.PlainDate.from({ year: 1446, month: 7, day: 1, calendar: 'islamic' })")]
    [InlineData("Temporal.PlainDateTime.from({ year: 1446, month: 7, day: 1, calendar: 'islamic' })")]
    [InlineData("Temporal.PlainMonthDay.from({ monthCode: 'M07', day: 1, calendar: 'islamic' })")]
    [InlineData("Temporal.PlainYearMonth.from({ year: 1446, month: 7, calendar: 'islamic' })")]
    public void BareIslamicCalendarIsRejected(string call)
        => Assert.Equal("RangeError", ErrorName(call));

    [Fact]
    public void SuffixedIslamicCalendarIsAccepted()
        => Assert.Equal("islamic-civil", Eval(
            "Temporal.PlainDate.from({ year: 1446, month: 7, day: 1, calendar: 'islamic-civil' }).calendarId").ToString());

    // ───────────── Problem 4: hebrew "M13" is invalid (the leap month is "M05L") ─────────────

    [Theory]
    [InlineData(5784)] // a leap year (5784 = 2023–2024)
    [InlineData(5783)] // a non-leap year
    public void HebrewMonthCodeThirteenIsRejected(int year)
        => Assert.Equal("RangeError", ErrorName(
            $"Temporal.PlainDate.from({{ year: {year}, monthCode: 'M13', day: 1, calendar: 'hebrew' }})"));

    [Fact]
    public void HebrewLeapMonthCodeIsAccepted()
        => Assert.Equal("M05L", Eval(
            "Temporal.PlainDate.from({ year: 5784, monthCode: 'M05L', day: 1, calendar: 'hebrew' }).monthCode").ToString());

    // ───────────── Problem 5: a non-string monthCode is a TypeError ─────────────

    [Theory]
    [InlineData("Temporal.PlainDate.from({ year: 2024, monthCode: 5, day: 1 })")]
    [InlineData("Temporal.PlainDateTime.from({ year: 2024, monthCode: 5, day: 1 })")]
    [InlineData("Temporal.PlainMonthDay.from({ monthCode: 5, day: 1 })")]
    [InlineData("Temporal.PlainYearMonth.from({ year: 2024, monthCode: 5 })")]
    [InlineData("Temporal.PlainDate.from({ year: 2024, monthCode: true, day: 1 })")]
    [InlineData("Temporal.PlainDate.from({ year: 1446, monthCode: 7, day: 1, calendar: 'islamic-civil' })")]
    public void NonStringMonthCodeIsTypeError(string call)
        => Assert.Equal("TypeError", ErrorName(call));

    // ───────────── Problem 14: Duration relativeTo eraYear Infinity is a RangeError ─────────────

    [Theory]
    [InlineData("Temporal.Duration.from({ months: 1 }).round({ largestUnit: 'days', relativeTo: { era: 'ce', eraYear: Infinity, month: 1, day: 1, calendar: 'gregory' } })")]
    [InlineData("Temporal.Duration.from({ months: 1 }).total({ unit: 'days', relativeTo: { era: 'ce', eraYear: Infinity, month: 1, day: 1, calendar: 'gregory' } })")]
    [InlineData("Temporal.Duration.from({ months: 1 }).round({ largestUnit: 'days', relativeTo: { era: 'ce', eraYear: -Infinity, month: 1, day: 1, calendar: 'gregory' } })")]
    [InlineData("Temporal.Duration.from({ months: 1 }).round({ largestUnit: 'days', relativeTo: { era: 'ce', eraYear: NaN, month: 1, day: 1, calendar: 'gregory' } })")]
    public void DurationRelativeToInfiniteEraYearIsRangeError(string call)
        => Assert.Equal("RangeError", ErrorName(call));
}
