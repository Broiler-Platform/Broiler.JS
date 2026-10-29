using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/836
//
// Fixed here:
//
//   Problems 88-90 (Temporal.PlainDateTime until/since with a sub-day largestUnit) —
//   DifferenceISODateTime computed the date part as whole days and the time part
//   separately, but when largestUnit is a time unit (hour..nanosecond) it never folded
//   those days into the time total. So `feb20.until(feb21, { largestUnit: "milliseconds" })`
//   left a stray days component (days = 1) instead of expressing the whole span in the
//   requested unit (days = 0, milliseconds = 86400250). The difference now balances the
//   days into the time total via BalanceTimeDuration when largestUnit is finer than a day.
public class Issue836PlainDateTimeDifferenceTests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    private const string Setup =
        "var a = new Temporal.PlainDateTime(2020, 2, 1, 0, 0);" +
        "var b = new Temporal.PlainDateTime(2020, 2, 2, 0, 0, 0, 250, 250, 250);";

    [Fact]
    public void UntilMillisecondsFoldsDays()
        => Assert.Equal("0,86400250", Eval(Setup +
            "var d = a.until(b, { largestUnit: 'milliseconds' });" +
            "d.days + ',' + d.milliseconds"));

    [Fact]
    public void UntilHoursFoldsDays()
        => Assert.Equal("0,24", Eval(
            "var a = new Temporal.PlainDateTime(2020, 1, 1);" +
            "var b = new Temporal.PlainDateTime(2020, 1, 2);" +
            "var d = a.until(b, { largestUnit: 'hours' });" +
            "d.days + ',' + d.hours"));

    [Fact]
    public void UntilHoursOverFullYear()
        => Assert.Equal("0,8784", Eval(
            "var a = new Temporal.PlainDateTime(2020, 1, 1);" +
            "var b = new Temporal.PlainDateTime(2021, 1, 1);" +
            "var d = a.until(b, { largestUnit: 'hours' });" +
            "d.days + ',' + d.hours"));

    [Fact]
    public void SinceMillisecondsFoldsDays()
        => Assert.Equal("0,-86400250", Eval(Setup +
            "var d = a.since(b, { largestUnit: 'milliseconds' });" +
            "d.days + ',' + d.milliseconds"));

    // A day (or coarser) largestUnit still keeps the days component.
    [Fact]
    public void UntilDaysKeepsDaysComponent()
        => Assert.Equal("1,0", Eval(Setup +
            "var d = a.until(b, { largestUnit: 'days' });" +
            "d.days + ',' + d.hours"));
}
