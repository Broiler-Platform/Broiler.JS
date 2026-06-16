using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/818 — Problem 8
// (test/built-ins/Temporal/Duration/prototype/{total,round}/precision-exact-*.js):
//   Test262Error: check expected value (µs)
//   Expected SameValue(«8.69228866946552e+21», «8.692288669465521e+21») to be true.
//
// Two precision bugs surfaced by these fixtures:
//   * Number(bigint) used .NET's (double)BigInteger, which truncates toward zero, so
//     Number(8692288669465520373761n) rounded down instead of to the nearest double.
//   * Temporal.Duration.prototype.total computed a unit total as
//     (double)wholePart + (double)remainder/unit (two roundings) and the calendar-unit
//     progress as whole + (double)progress/(double)unitLen, landing on the adjacent
//     double. Both now round the exact mathematical value once (ℝ→𝔽).
public class Issue818ExactPrecisionTests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    [Theory]
    [InlineData("8692288669465520373761")] // exceeds 2^63, ulp ~ 2^20
    [InlineData("18446744073709551616")]   // 2^64
    [InlineData("18446744073709551617")]   // 2^64 + 1
    public void NumberOfBigIntRoundsToNearestDouble(string literal)
        => Assert.Equal("true", Eval(
            $"String(Number({literal}n) === {literal})"));

    [Fact]
    public void TotalMicrosecondsUsesExactMathematicalValue()
        => Assert.Equal("true", Eval(
            "var d = new Temporal.Duration(0, 0, 0, 0, 0, 0, 8692288669465520, 0, 373761);" +
            "var expected = Number(8692288669465520n * 1000000n + 373761n);" +
            "String(d.total({ unit: 'microseconds' }) === expected && expected === 8692288669465520373761)"));

    [Fact]
    public void TotalMillisecondsUsesExactMathematicalValue()
        => Assert.Equal("true", Eval(
            "var d = new Temporal.Duration(0, 0, 0, 0, 0, 0, 8692288669465520, 513);" +
            "String(d.total({ unit: 'milliseconds' }) === 8692288669465520513)"));

    [Fact]
    public void TotalNanosecondsUsesExactMathematicalValue()
        => Assert.Equal("true", Eval(
            "var d = new Temporal.Duration(0, 0, 0, 0, 0, 0, 8692288669465520, 0, 0, 321414345);" +
            "String(d.total({ unit: 'nanoseconds' }) === 8692288669465520321414345)"));

    // Calendar-unit total: the exact rational 1 + 950400000000000/2678400000000000 must
    // round to 1.3548387096774193, not the adjacent 1.3548387096774195.
    [Fact]
    public void TotalMonthsRoundsTheExactRationalOnce()
        => Assert.Equal("1.3548387096774193", Eval(
            "'' + new Temporal.Duration(0, 0, 5, 5).total({ unit: 'months', relativeTo: '1972-01-31' })"));
}
