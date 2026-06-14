using Broiler.JavaScript.Engine;
using Xunit;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/794 — Problem 1 (Temporal.Duration
// range / rounding RangeErrors).
//
//   * add / subtract reject a result that overflows the 2^53-second duration limit. The component
//     totals are computed in BigInteger and converted to Number via round-to-nearest (matching the
//     spec's ℝ→𝔽), so a component that rounds up over the limit is rejected.
//   * toString rejects a duration whose rounding (ceil/expand to a coarse precision) overflows.
//   * round / total reject a relativeTo string whose [time-zone] annotation is invalid (e.g. a
//     sub-minute offset zone).
//   * round with a ZonedDateTime relativeTo rejects a near-maximum nanoseconds duration (the total
//     time is computed in BigInteger, not Int64).
public class Issue794DurationRangeTests
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

    [Fact]
    public void Add_ResultOverflowsLimit_Throws()
        => Assert.Equal("RangeError", ErrorName(
            "Temporal.Duration.from({nanoseconds: 9.007199254740991e+24}).add(Temporal.Duration.from({microseconds: 1000000}))"));

    [Fact]
    public void Subtract_ResultOverflowsLimit_Throws()
        => Assert.Equal("RangeError", ErrorName(
            "Temporal.Duration.from({nanoseconds: -9.007199254740991e+24}).subtract(Temporal.Duration.from({microseconds: 1000000}))"));

    [Fact] // a normal add is unaffected
    public void Add_Normal_Works()
        => Assert.Equal("PT2H15M", Eval("Temporal.Duration.from({hours:1, minutes:30}).add({minutes:45}).toString()"));

    [Fact]
    public void ToString_RoundedDurationInvalid_Throws()
        => Assert.Equal("RangeError", ErrorName(
            "Temporal.Duration.from({seconds: Number.MAX_SAFE_INTEGER, milliseconds: 999}).toString({smallestUnit:'seconds', roundingMode:'ceil'})"));

    [Fact]
    public void ToString_TotalUnitsOutOfRange_Throws()
        => Assert.Equal("RangeError", ErrorName(
            "new Temporal.Duration(0,0,0,1,0,0,Math.pow(2,53)-1-86400,0,0,999999999).toString({roundingMode:'ceil', fractionalSecondDigits:7})"));

    [Fact] // a normal toString with precision/rounding is unaffected
    public void ToString_Normal_Works()
        => Assert.Equal("PT5.25S", Eval("Temporal.Duration.from({seconds:5, milliseconds:250}).toString({fractionalSecondDigits:2})"));

    [Theory] // a relativeTo string with an invalid (sub-minute offset) time-zone annotation is rejected
    [InlineData("round")]
    [InlineData("total")]
    public void RelativeToStringWrongOffset_Throws(string method)
    {
        var opts = method == "total"
            ? "{ unit: 'seconds', relativeTo: '1971-01-01T00:00+02:00[-00:44:30]' }"
            : "{ smallestUnit: 'seconds', relativeTo: '1971-01-01T00:00+02:00[-00:44:30]' }";
        Assert.Equal("RangeError", ErrorName(
            $"new Temporal.Duration(5,5,5,5,5,5,5,5,5,5).{method}({opts})"));
    }

    [Theory] // a near-maximum duration rounded with a ZonedDateTime relativeTo overflows the limit
    [InlineData("{ seconds: Number.MAX_SAFE_INTEGER }")]
    [InlineData("{ milliseconds: 9007199254740991487 }")]
    [InlineData("{ nanoseconds: 9007199254740991463129087 }")]
    public void RoundWithZonedRelative_TooLarge_Throws(string dur)
        => Assert.Equal("RangeError", ErrorName(
            $"Temporal.Duration.from({dur}).round({{ smallestUnit:'day', largestUnit:'day', relativeTo: new Temporal.ZonedDateTime(0n, 'UTC') }})"));
}
