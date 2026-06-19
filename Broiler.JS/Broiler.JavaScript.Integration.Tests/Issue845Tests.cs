using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/845
// Number.prototype.toExponential / toFixed / toPrecision must round from the
// exact binary value of the receiver (not the shortest round-trip string) and
// must not count the sign as a significant digit. Expected values match V8.
public class Issue845Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // ---- toPrecision: sign must not be counted as a significant digit ----

    [Theory]
    // Problem 89 (return-values.js): a single integer digit padded to p digits.
    [InlineData("(-7).toPrecision(2)", "-7.0")]
    [InlineData("(7).toPrecision(2)", "7.0")]
    // Problem 88 (exponential.js): trailing significant zero in exponential form.
    [InlineData("(-1.2345e27).toPrecision(6)", "-1.23450e+27")]
    [InlineData("(1.2345e27).toPrecision(6)", "1.23450e+27")]
    [InlineData("(123).toPrecision(2)", "1.2e+2")]
    [InlineData("(0.00001).toPrecision(2)", "0.000010")]
    [InlineData("(1).toPrecision(2)", "1.0")]
    public void ToPrecisionFormatting(string code, string expected)
        => Assert.Equal(expected, Eval(code));

    // Problem 87 (toPrecision-values.js): negative zero renders without a sign.
    [Theory]
    [InlineData("(-0).toPrecision(1)", "0")]
    [InlineData("(-0).toPrecision(2)", "0.0")]
    public void ToPrecisionNegativeZero(string code, string expected)
        => Assert.Equal(expected, Eval(code));

    // ---- toExponential: round from the exact binary value ----

    [Theory]
    // Problem 95 (return-values.js): exact expansion, not the shortest string.
    [InlineData("(123.456).toExponential(17)", "1.23456000000000003e+2")]
    // Problem 100 (toExponential-values.js): undefined digits uses full round-trip.
    [InlineData("Math.PI.toExponential()", "3.141592653589793e+0")]
    [InlineData("(0).toExponential()", "0e+0")]
    public void ToExponentialFormatting(string code, string expected)
        => Assert.Equal(expected, Eval(code));

    // ---- toFixed: spec threshold is 10^21, not 10^15 ----

    [Theory]
    // Problem 96 (toFixed-values.js): 1e20 < 1e21 keeps fixed notation.
    [InlineData("(1e20).toFixed(1)", "100000000000000000000.0")]
    [InlineData("(1.005).toFixed(2)", "1.00")]
    [InlineData("(0).toFixed(2)", "0.00")]
    [InlineData("(-0.0001).toFixed(2)", "-0.00")]
    public void ToFixedFormatting(string code, string expected)
        => Assert.Equal(expected, Eval(code));
}
