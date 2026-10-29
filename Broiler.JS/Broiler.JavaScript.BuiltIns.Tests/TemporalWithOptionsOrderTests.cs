using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Temporal .with() prepares (and validates) the partial-fields argument before the options argument:
// a non-positive day/month is a RangeError thrown during field processing, even when options is a bad
// (non-object) type. Issue #805 problem 49.
public class TemporalWithOptionsOrderTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string ErrorOf(string expr)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval($$"""
            var err = "none";
            try { {{expr}}; }
            catch (e) { err = e.constructor.name; }
            err;
        """).ToString();
    }

    [Theory]
    // A non-positive day/month is rejected while reading fields — before the bad options are observed.
    [InlineData("new Temporal.PlainDateTime(1976,11,18).with({ day: -1 }, null)", "RangeError")]
    [InlineData("new Temporal.PlainDateTime(1976,11,18).with({ month: 0 }, 42)", "RangeError")]
    [InlineData("new Temporal.ZonedDateTime(0n,'UTC').with({ day: -1 }, null)", "RangeError")]
    // A valid partial with bad options still surfaces the options TypeError.
    [InlineData("new Temporal.PlainDateTime(1976,11,18).with({ day: 5 }, null)", "TypeError")]
    [InlineData("new Temporal.ZonedDateTime(0n,'UTC').with({ day: 5 }, 42)", "TypeError")]
    public void With_PartialProcessedBeforeOptions(string expr, string expected)
        => Assert.Equal(expected, ErrorOf(expr));

    [Theory]
    // A non-positive day/month is a RangeError regardless of the overflow option (it is rejected
    // before overflow is even applied).
    [InlineData("new Temporal.PlainDateTime(1976,11,18).with({ day: -1 }, { overflow: 'constrain' })")]
    [InlineData("new Temporal.PlainDateTime(1976,11,18).with({ day: 0 })")]
    [InlineData("new Temporal.PlainDateTime(1976,11,18).with({ month: -3 }, { overflow: 'constrain' })")]
    public void With_NonPositiveDayOrMonth_AlwaysRangeError(string expr)
        => Assert.Equal("RangeError", ErrorOf(expr));

    [Fact]
    public void With_StillRejectsDisagreeingMonthAndMonthCode()
    {
        Assert.Equal("RangeError",
            ErrorOf("new Temporal.PlainDateTime(1976,11,18).with({ month: 2, monthCode: 'M03' })"));
    }

    [Fact]
    public void With_ValidPartial_Applies()
    {
        Load();
        using var ctx = new JSContext();
        var result = ctx.Eval("new Temporal.PlainDateTime(1976,11,18).with({ day: 5 }).toString();");
        Assert.Equal("1976-11-05T00:00:00", result.ToString());
    }
}
