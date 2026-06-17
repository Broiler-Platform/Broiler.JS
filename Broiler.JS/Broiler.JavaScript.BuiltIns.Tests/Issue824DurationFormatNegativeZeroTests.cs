using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Issue #824 (problem 60): Intl.DurationFormat.format with a negative-zero field must format the same
// as a positive-zero field — DurationSign treats -0 as 0, so no minus sign appears. Mirrors test262
// DurationFormat/prototype/format/negative-zero.js.
public class Issue824DurationFormatNegativeZeroTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    [Fact]
    public void NegativeZeroMatchesPositiveZero()
    {
        var r = Eval("""
            const units = ["years","months","weeks","days","hours","minutes","seconds",
                           "milliseconds","microseconds","nanoseconds"];
            const problems = [];
            for (const unit of units) {
              const auto = new Intl.DurationFormat("en", { [unit + "Display"]: "auto" });
              if (auto.format({ [unit]: +0 }) !== "") problems.push("auto+0:" + unit);
              if (auto.format({ [unit]: -0 }) !== "") problems.push("auto-0:" + unit);

              const always = new Intl.DurationFormat("en", { [unit + "Display"]: "always" });
              const pos = always.format({ [unit]: +0 });
              const neg = always.format({ [unit]: -0 });
              if (neg !== pos) problems.push(`${unit}: '${neg}' vs '${pos}'`);
              if (neg.indexOf("-") !== -1) problems.push("has-minus:" + unit + ":" + neg);
            }
            problems.length === 0 ? "ok" : problems.join(" | ");
        """);
        Assert.Equal("ok", r);
    }

    [Theory]
    [InlineData("new Intl.DurationFormat('en', { yearsDisplay: 'always' }).format({ years: -0 })", "0 yrs")]
    [InlineData("new Intl.DurationFormat('en', { yearsDisplay: 'always' }).format({ years: +0 })", "0 yrs")]
    // A genuinely negative duration still carries the sign on the first DISPLAYED unit. With default
    // "auto" display the -0 years is hidden, so the sign lands on months.
    [InlineData("new Intl.DurationFormat('en').format({ years: -0, months: -5 })", "-5 mths")]
    [InlineData("new Intl.DurationFormat('en').format({ years: -1, months: -5 })", "-1 yr, 5 mths")]
    // When the -0 years unit is forced visible, it carries the negative duration's sign.
    [InlineData("new Intl.DurationFormat('en', { yearsDisplay: 'always' }).format({ years: -0, months: -5 })", "-0 yrs, 5 mths")]
    public void SpotChecks(string expr, string expected)
        => Assert.Equal(expected, Eval(expr));
}
