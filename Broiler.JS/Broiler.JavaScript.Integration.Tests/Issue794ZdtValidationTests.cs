using Broiler.JavaScript.Engine;
using Xunit;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/794 — Problem 1 (ZonedDateTime
// since / until / round / startOfDay / withPlainTime RangeError validation).
//
//   * since / until validate that largestUnit is not finer than smallestUnit and that a sub-day
//     smallestUnit's rounding increment divides its unit evenly.
//   * round validates the rounding increment (a "day" increment must be exactly 1) and that the
//     day's start/end instants are representable.
//   * startOfDay / withPlainTime() (and round to "day") throw when the start-of-day instant falls
//     outside the representable range.
public class Issue794ZdtValidationTests
{
    private static string ErrorName(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval($"var c='no-throw'; try {{ {code}; }} catch (e) {{ c = e.constructor.name; }} c").ToString();
    }

    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    private const string Pair =
        "var earlier = new Temporal.ZonedDateTime(1546935756123456789n, '+01:00');" +
        "var later = new Temporal.ZonedDateTime(1631018380987654321n, '+01:00');";

    [Theory] // a sub-day smallestUnit increment must divide its unit evenly
    [InlineData("{ smallestUnit: 'hours', roundingIncrement: 11 }")]
    [InlineData("{ smallestUnit: 'minutes', roundingIncrement: 29 }")]
    [InlineData("{ smallestUnit: 'seconds', roundingIncrement: 29 }")]
    [InlineData("{ smallestUnit: 'milliseconds', roundingIncrement: 29 }")]
    public void SinceUntil_InvalidRoundingIncrement_Throws(string opts)
    {
        Assert.Equal("RangeError", ErrorName(Pair + $"later.since(earlier, {opts})"));
        Assert.Equal("RangeError", ErrorName(Pair + $"later.until(earlier, {opts})"));
    }

    [Theory] // a smallestUnit coarser than largestUnit is a mismatch
    [InlineData("years", "months")]
    [InlineData("hours", "minutes")]
    [InlineData("weeks", "days")]
    public void SinceUntil_LargestSmallestMismatch_Throws(string smallest, string largest)
    {
        var opts = $"{{ largestUnit: '{largest}', smallestUnit: '{smallest}' }}";
        Assert.Equal("RangeError", ErrorName(Pair + $"later.since(earlier, {opts})"));
        Assert.Equal("RangeError", ErrorName(Pair + $"later.until(earlier, {opts})"));
    }

    [Theory] // round: a "day" increment must be exactly 1; sub-day increments must divide their unit
    [InlineData("{ smallestUnit: 'day', roundingIncrement: 29 }")]
    [InlineData("{ smallestUnit: 'hour', roundingIncrement: 29 }")]
    [InlineData("{ smallestUnit: 'minute', roundingIncrement: 29 }")]
    public void Round_InvalidIncrement_Throws(string opts)
        => Assert.Equal("RangeError", ErrorName(
            "new Temporal.ZonedDateTime(217175010123456789n, '+01:00').round(" + opts + ")"));

    [Theory] // start-of-day outside the representable range is a RangeError
    [InlineData("new Temporal.ZonedDateTime(-864n * 10n**19n, '-01').startOfDay()")]
    [InlineData("new Temporal.ZonedDateTime(-864n * 10n**19n, '+01').startOfDay()")]
    [InlineData("new Temporal.ZonedDateTime(-864n * 10n**19n, '-01').withPlainTime()")]
    [InlineData("new Temporal.ZonedDateTime(864n * 10n**19n, '+01').round({ smallestUnit: 'days' })")]
    public void StartOfDay_OutsideRange_Throws(string code)
        => Assert.Equal("RangeError", ErrorName(code));

    [Fact] // startOfDay at the exact boundary (UTC midnight) is still valid
    public void StartOfDay_AtBoundary_Succeeds()
        => Assert.Equal("true", Eval(
            "var z = new Temporal.ZonedDateTime(-864n * 10n**19n, '+00'); '' + z.startOfDay().equals(z)"));

    [Fact] // a valid since with a dividing increment still works (no over-throwing)
    public void SinceUntil_ValidIncrement_Succeeds()
        => Assert.Equal("no-throw", ErrorName(
            Pair + "later.since(earlier, { smallestUnit: 'hours', roundingIncrement: 12 })"));
}
