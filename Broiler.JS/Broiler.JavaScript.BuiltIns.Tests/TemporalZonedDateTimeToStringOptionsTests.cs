using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Temporal.ZonedDateTime.prototype.toString applies the precision (fractionalSecondDigits /
// smallestUnit / roundingMode) and the offset / timeZoneName display options. Issue #805 problem 48.
public class TemporalZonedDateTimeToStringOptionsTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string expr)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(expr).ToString();
    }

    // epoch 1000000000.1239875 s → "2001-09-09T01:46:40.1239875+00:00[UTC]" at full precision.
    private const string Zdt = "new Temporal.ZonedDateTime(1000000000123987500n, 'UTC')";

    [Fact]
    public void ToString_FullPrecisionByDefault()
        => Assert.Equal("2001-09-09T01:46:40.1239875+00:00[UTC]", Eval($"{Zdt}.toString()"));

    [Theory]
    [InlineData("{ smallestUnit: 'microsecond' }", "2001-09-09T01:46:40.123987+00:00[UTC]")]
    [InlineData("{ smallestUnit: 'millisecond' }", "2001-09-09T01:46:40.123+00:00[UTC]")]
    [InlineData("{ smallestUnit: 'second' }", "2001-09-09T01:46:40+00:00[UTC]")]
    [InlineData("{ smallestUnit: 'minute' }", "2001-09-09T01:46+00:00[UTC]")]
    [InlineData("{ fractionalSecondDigits: 6 }", "2001-09-09T01:46:40.123987+00:00[UTC]")]
    [InlineData("{ fractionalSecondDigits: 0 }", "2001-09-09T01:46:40+00:00[UTC]")]
    [InlineData("{ fractionalSecondDigits: 9 }", "2001-09-09T01:46:40.123987500+00:00[UTC]")]
    public void ToString_Precision(string options, string expected)
        => Assert.Equal(expected, Eval($"{Zdt}.toString({options})"));

    // smallestUnit / roundingMode given as an object with toString are coerced via ToString (problem 48).
    [Fact]
    public void ToString_SmallestUnit_ObjectWithToString_IsCoerced()
        => Assert.Equal("2001-09-09T01:46:40.123987+00:00[UTC]",
            Eval($"{Zdt}.toString({{ smallestUnit: {{ toString() {{ return 'microsecond'; }} }} }})"));

    [Theory]
    [InlineData("{ smallestUnit: 'microsecond', roundingMode: 'halfExpand' }", "2001-09-09T01:46:40.123988+00:00[UTC]")]
    [InlineData("{ smallestUnit: 'microsecond', roundingMode: 'ceil' }", "2001-09-09T01:46:40.123988+00:00[UTC]")]
    [InlineData("{ smallestUnit: 'microsecond', roundingMode: 'trunc' }", "2001-09-09T01:46:40.123987+00:00[UTC]")]
    public void ToString_RoundingMode(string options, string expected)
        => Assert.Equal(expected, Eval($"{Zdt}.toString({options})"));

    [Theory]
    [InlineData("{ offset: 'never' }", "2001-09-09T01:46:40.1239875[UTC]")]
    [InlineData("{ timeZoneName: 'never' }", "2001-09-09T01:46:40.1239875+00:00")]
    [InlineData("{ timeZoneName: 'critical' }", "2001-09-09T01:46:40.1239875+00:00[!UTC]")]
    [InlineData("{ offset: 'never', timeZoneName: 'never' }", "2001-09-09T01:46:40.1239875")]
    public void ToString_OffsetAndTimeZoneNameDisplay(string options, string expected)
        => Assert.Equal(expected, Eval($"{Zdt}.toString({options})"));

    [Theory]
    [InlineData("{ smallestUnit: 'hour' }", "RangeError")]
    [InlineData("{ fractionalSecondDigits: 10 }", "RangeError")]
    [InlineData("{ offset: 'bogus' }", "RangeError")]
    [InlineData("{ timeZoneName: 'bogus' }", "RangeError")]
    public void ToString_InvalidOptions_Throw(string options, string expected)
    {
        var result = Eval($$"""
            var err = "none";
            try { {{Zdt}}.toString({{options}}); }
            catch (e) { err = e.constructor.name; }
            err;
        """);
        Assert.Equal(expected, result);
    }
}
