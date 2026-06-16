using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/818 — Problem 16
// (test/intl402/DateTimeFormat/prototype/{formatToParts,formatRangeToParts}/
//  temporal-objects-format-with-era.js): Test262Error: formatting a PlainDate should work.
//
// Intl.DateTimeFormat dropped the `era` option entirely: the 'G' field was only
// emitted for era-using calendars (buddhist/islamic), never when the user requested
// `era`, and the 'G' formatter ignored its width (always "AD"). So
// `new Intl.DateTimeFormat("en", { era: "narrow" })` produced "M/d/y" with no era
// part. The era option now appends the era field (when the pattern shows a year) at
// the requested width, and the gregorian era renders AD/BC narrow/short/long.
public class Issue818EraOptionTests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    private const string CheckEra =
        "function checkEra(parts){ for (var i = 0; i < parts.length; i++) " +
        "if (parts[i].type === 'era' && parts[i].value.indexOf('A') === 0) return true; return false; }";

    [Fact]
    public void EraOptionAddsEraPartForDate()
        => Assert.Equal("11/4/2025 A", Eval(
            "new Intl.DateTimeFormat('en', { era: 'narrow' }).format(new Date(2025, 10, 4))"));

    [Theory]
    [InlineData("narrow", "A")]
    [InlineData("short", "AD")]
    [InlineData("long", "Anno Domini")]
    public void EraWidthIsHonored(string style, string expectedEra)
        => Assert.Equal($"11/4/2025 {expectedEra}", Eval(
            $"new Intl.DateTimeFormat('en', {{ era: '{style}' }}).format(new Date(2025, 10, 4))"));

    [Fact]
    public void TemporalPlainDateGetsEra()
        => Assert.Equal("true", Eval(
            CheckEra +
            "String(checkEra(new Intl.DateTimeFormat(['en'], { era: 'narrow' })" +
            ".formatToParts(new Temporal.PlainDate(2025, 11, 4))))"));

    [Fact]
    public void TemporalPlainYearMonthGetsEra()
        => Assert.Equal("true", Eval(
            CheckEra +
            "String(checkEra(new Intl.DateTimeFormat(['en'], { era: 'narrow' })" +
            ".formatToParts(new Temporal.PlainYearMonth(2025, 11, 'gregory'))))"));

    // PlainMonthDay has no year, so the era must NOT appear.
    [Fact]
    public void TemporalPlainMonthDayHasNoEra()
        => Assert.Equal("false", Eval(
            CheckEra +
            "String(checkEra(new Intl.DateTimeFormat(['en'], { era: 'narrow' })" +
            ".formatToParts(new Temporal.PlainMonthDay(11, 4, 'gregory'))))"));

    // PlainTime has no date, so the era must NOT appear.
    [Fact]
    public void TemporalPlainTimeHasNoEra()
        => Assert.Equal("false", Eval(
            CheckEra +
            "String(checkEra(new Intl.DateTimeFormat(['en'], { era: 'narrow' })" +
            ".formatToParts(new Temporal.PlainTime(14, 46))))"));

    // An era-using calendar keeps showing its era when no era option is given.
    [Fact]
    public void EraCalendarStillShowsEraWithoutEraOption()
        => Assert.Equal("11/4/2568 BE", Eval(
            "new Intl.DateTimeFormat('en-u-ca-buddhist', { year: 'numeric', month: 'numeric', day: 'numeric' })" +
            ".format(new Date(2025, 10, 4))"));

    // No era option and a gregorian calendar: unchanged.
    [Fact]
    public void NoEraOptionIsUnchanged()
        => Assert.Equal("11/4/2025", Eval(
            "new Intl.DateTimeFormat('en').format(new Date(2025, 10, 4))"));
}
