using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Temporal.Duration.prototype.toString rounding can cross unit boundaries.
// Issue #826: rounding the fractional seconds up (e.g. expand) left the carry in the
// seconds field ("PT1H59M60S") instead of balancing it up through minutes/hours and,
// when the duration's largest unit is day or larger, into days. The balancing is
// capped at LargerOfTwoTemporalUnits(DefaultTemporalLargestUnit, "second"), so it never
// reaches weeks/months/years and stays at the seconds field when seconds is the largest
// unit. Mirrors test262 built-ins/Temporal/Duration/prototype/toString/round-cross-unit-boundary.
public class Issue826DurationToStringRoundingTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    [Fact]
    public void PositiveTimeUnits_BalanceUpToHours()
        => Assert.Equal("PT2H0S", Eval("""
            new Temporal.Duration(0, 0, 0, 0, 1, 59, 59, 900)
                .toString({ fractionalSecondDigits: 0, roundingMode: "expand" });
        """));

    [Fact]
    public void NegativeTimeUnits_BalanceUpToHours()
        => Assert.Equal("-PT2H0S", Eval("""
            new Temporal.Duration(0, 0, 0, 0, -1, -59, -59, -900)
                .toString({ fractionalSecondDigits: 0, roundingMode: "expand" });
        """));

    [Fact]
    public void DateAndTimeUnits_BalanceUpToDays()
        => Assert.Equal("P1Y11M31DT0.00000000S", Eval("""
            new Temporal.Duration(1, 11, 0, 30, 23, 59, 59, 999, 999, 999)
                .toString({ fractionalSecondDigits: 8, roundingMode: "expand" });
        """));

    [Fact]
    public void NegativeDateAndTimeUnits_BalanceUpToDays()
        => Assert.Equal("-P1Y11M31DT0.00000000S", Eval("""
            new Temporal.Duration(-1, -11, 0, -30, -23, -59, -59, -999, -999, -999)
                .toString({ fractionalSecondDigits: 8, roundingMode: "expand" });
        """));

    [Fact]
    public void NoBalancingWhenSecondIsLargestUnit()
        => Assert.Equal("PT60S", Eval("""
            new Temporal.Duration(0, 0, 0, 0, 0, 0, 59, 900)
                .toString({ fractionalSecondDigits: 0, roundingMode: "expand" });
        """));

    // The largest unit is "minute", so balancing caps at minutes (60M), not hours.
    [Fact]
    public void BalancingCapsAtLargestUnitMinute()
        => Assert.Equal("PT60M0S", Eval("""
            new Temporal.Duration(0, 0, 0, 0, 0, 59, 59, 500)
                .toString({ smallestUnit: "second", roundingMode: "expand" });
        """));

    // Without rounding the stored components are emitted as-is (no balancing).
    [Fact]
    public void NoRounding_LeavesComponentsUnbalanced()
        => Assert.Equal("PT1H59M60S", Eval("""
            new Temporal.Duration(0, 0, 0, 0, 1, 59, 60).toString();
        """));
}
