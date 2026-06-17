using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Temporal.Duration.prototype.round to a calendar smallestUnit relative to a PlainDate. Issue #828
// Problem 1: rounding a year/month/week duration to "weeks" with the default rounding increment of 1
// (largestUnit left to default to the duration's largest unit) must succeed and keep the coarser
// calendar units, returning years/months/weeks — it was incorrectly rejected with a RangeError that
// claimed "largestUnit must be week when the duration has years or months". The real spec rule is
// narrower: a roundingIncrement greater than 1 with a calendar smallestUnit requires largestUnit to
// equal smallestUnit (test262 round/balances-up-to-weeks); increment 1 rounds freely
// (test262 round/roundingmode-ceil and the other round/roundingmode-* cases).
public class Issue828DurationRoundWeeksTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    // test262 round/roundingmode-ceil: instance 5y6m7w8d40h30m20s123ms987µs500ns,
    // relativeTo 2020-04-01, roundingMode "ceil". smallestUnit -> expected positive result.
    [Theory]
    [InlineData("years", "6,0,0,0,0,0,0,0,0,0")]
    [InlineData("months", "5,8,0,0,0,0,0,0,0,0")]
    [InlineData("weeks", "5,7,4,0,0,0,0,0,0,0")]
    [InlineData("days", "5,7,0,28,0,0,0,0,0,0")]
    [InlineData("hours", "5,7,0,27,17,0,0,0,0,0")]
    public void RoundCeil_DefaultLargestUnit_KeepsCoarserUnits(string smallestUnit, string expected)
        => Assert.Equal(expected, Eval($$"""
            var d = new Temporal.Duration(5, 6, 7, 8, 40, 30, 20, 123, 987, 500);
            var r = d.round({ smallestUnit: "{{smallestUnit}}", roundingMode: "ceil",
                relativeTo: new Temporal.PlainDate(2020, 4, 1) });
            [r.years, r.months, r.weeks, r.days, r.hours, r.minutes, r.seconds,
             r.milliseconds, r.microseconds, r.nanoseconds].join(",");
        """));

    // The negated duration relative to 2020-12-01 mirrors the positive case (round/roundingmode-ceil).
    [Theory]
    [InlineData("weeks", "-5,-7,-3,0,0,0,0,0,0,0")]
    [InlineData("days", "-5,-7,0,-27,0,0,0,0,0,0")]
    public void RoundCeil_Negative_DefaultLargestUnit(string smallestUnit, string expected)
        => Assert.Equal(expected, Eval($$"""
            var d = new Temporal.Duration(5, 6, 7, 8, 40, 30, 20, 123, 987, 500).negated();
            var r = d.round({ smallestUnit: "{{smallestUnit}}", roundingMode: "ceil",
                relativeTo: new Temporal.PlainDate(2020, 12, 1) });
            [r.years, r.months, r.weeks, r.days, r.hours, r.minutes, r.seconds,
             r.milliseconds, r.microseconds, r.nanoseconds].join(",");
        """));

    // Increment 1 to weeks must NOT throw even though largestUnit defaults to the coarser "year".
    [Fact]
    public void RoundWeeks_IncrementOne_DefaultLargestUnit_DoesNotThrow()
        => Assert.Equal("5,7,4", Eval("""
            var d = new Temporal.Duration(5, 6, 7, 8, 40, 30, 20, 123, 987, 500);
            var r = d.round({ smallestUnit: "weeks", roundingMode: "ceil",
                relativeTo: new Temporal.PlainDate(2020, 4, 1) });
            [r.years, r.months, r.weeks].join(",");
        """));

    // Increment > 1 with a calendar smallestUnit still requires largestUnit == smallestUnit
    // (test262 round/balances-up-to-weeks).
    [Fact]
    public void RoundWeeks_IncrementNinetyNine_DefaultLargestUnit_Throws()
        => Assert.Equal("RangeError", Eval("""
            try {
              new Temporal.Duration(0, 1, 0, 1).round({
                relativeTo: new Temporal.PlainDate(2024, 1, 1),
                smallestUnit: "weeks", roundingIncrement: 99, roundingMode: "ceil" });
              "no throw";
            } catch (e) { e.constructor.name; }
        """));

    // An explicit largestUnit finer than smallestUnit is a RangeError.
    [Fact]
    public void Round_LargestUnitFinerThanSmallestUnit_Throws()
        => Assert.Equal("RangeError", Eval("""
            try {
              new Temporal.Duration(0, 0, 0, 0, 5).round({ smallestUnit: "hours", largestUnit: "minutes" });
              "no throw";
            } catch (e) { e.constructor.name; }
        """));
}
