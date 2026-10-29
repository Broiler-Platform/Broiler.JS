using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/818 — Problem 9
// (test/intl402/Temporal/Duration/prototype/round/{adjust-rounded-duration-days,
//  rounding-with-largestunit}.js): Test262Error: days result:
//  Expected SameValue(«1», «0») to be true.
//
// Rounding a time duration relative to a ZonedDateTime mishandled the day/time split:
//   * NudgeToZonedTime rolled a time value that landed exactly on (or, with a time
//     largestUnit, anywhere near) a day boundary into a day, so 13 hours rounded up to
//     the 12-hour increment came back as 1 day instead of 24 hours.
//   * BuildDuration then re-split the sub-day remainder into 24-hour days, so in a
//     25-hour DST day a 24-hour remainder became 1 day instead of staying 24 hours.
// A time largestUnit now keeps the rounded value entirely as time, and the day count
// comes solely from the DST-aware calendar difference.
public class Issue818ZonedRoundingTests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    private static string Round(string dur, string zdt, string options)
        => Eval(
            $"var r = {dur}.round(Object.assign({{ relativeTo: {zdt} }}, {options}));" +
            "r.days + 'd' + r.hours + 'h'");

    // 13h, UTC (24-hour day), largestUnit hours: rounds up to 24 hours, NOT 1 day.
    [Fact]
    public void TimeLargestUnitKeepsFullDayAsHours()
        => Assert.Equal("0d24h", Round(
            "new Temporal.Duration(0, 0, 0, 0, 13)",
            "new Temporal.ZonedDateTime(0n, 'UTC')",
            "{ largestUnit: 'hours', smallestUnit: 'hours', roundingIncrement: 12, roundingMode: 'ceil' }"));

    private const string SpringForward = "Temporal.ZonedDateTime.from('2024-03-10T00:00:00[America/New_York]')"; // 23h day
    private const string FallBack = "Temporal.ZonedDateTime.from('2024-11-03T00:00:00[America/New_York]')";       // 25h day

    // 13h over a 23-hour day, largestUnit years: an extra day is added -> 1 day 12 hours.
    [Fact]
    public void ShortDstDayAddsADay()
        => Assert.Equal("1d12h", Round(
            "new Temporal.Duration(0, 0, 0, 0, 13)", SpringForward,
            "{ largestUnit: 'years', smallestUnit: 'hours', roundingIncrement: 12, roundingMode: 'ceil' }"));

    // 24h over a 25-hour day, largestUnit years: 24h < one day here, so it stays 24 hours.
    [Fact]
    public void TwentyFourHoursIsNotAFullDayInA25HourDay()
        => Assert.Equal("0d24h", Round(
            "new Temporal.Duration(0, 0, 0, 0, 24)", FallBack,
            "{ largestUnit: 'years', smallestUnit: 'hours', roundingIncrement: 12, roundingMode: 'ceil' }"));

    // A 1-day duration over a 25-hour day, largestUnit hours: 25 hours rounds up to 36.
    [Fact]
    public void OneDayBecomesActualHoursForTimeLargestUnit()
        => Assert.Equal("0d36h", Round(
            "new Temporal.Duration(0, 0, 0, 1)", FallBack,
            "{ largestUnit: 'hours', smallestUnit: 'hours', roundingIncrement: 12, roundingMode: 'ceil' }"));

    // Normal (non-DST) ZonedDateTime rounding is unchanged.
    [Theory]
    [InlineData("days", "2d2h")]
    [InlineData("hours", "0d50h")]
    public void NormalDayRoundingIsUnchanged(string largestUnit, string expected)
        => Assert.Equal(expected, Eval(
            "var z1 = new Temporal.ZonedDateTime(0n, 'UTC');" +
            "var z2 = new Temporal.ZonedDateTime(180000000000000n, 'UTC');" + // 50 hours
            $"var r = z2.since(z1, {{ largestUnit: '{largestUnit}' }});" +
            "r.days + 'd' + r.hours + 'h'"));
}
