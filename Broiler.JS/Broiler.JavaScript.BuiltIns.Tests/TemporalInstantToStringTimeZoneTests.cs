using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Temporal.Instant.prototype.toString honours and validates the timeZone option (issue #798
// problems 10, 17).
public class TemporalInstantToStringTimeZoneTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string expr)
    {
        using var ctx = new JSContext();
        return ctx.Eval(expr).ToString();
    }

    [Theory]
    [InlineData("\"2021-08-19T17:30\"")]      // bare date-time string is not a time zone (problem 10)
    [InlineData("\"2021-08-19\"")]
    // A negative-zero extended year (-000000) is invalid (issue #805 problem 10).
    [InlineData("\"-000000-10-31T17:45Z\"")]
    [InlineData("\"-000000-10-31T17:45+00:00[UTC]\"")]
    public void ToString_BareDateTimeTimeZone_ThrowsRangeError(string tz)
    {
        Load();
        var result = Eval($$"""
            var threw = false;
            try { Temporal.Instant.fromEpochMilliseconds(0).toString({ timeZone: {{tz}} }); }
            catch (e) { threw = e instanceof RangeError; }
            threw;
        """);
        Assert.Equal("true", result);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("42")]
    [InlineData("true")]
    [InlineData("{}")]
    public void ToString_WrongTypeTimeZone_ThrowsTypeError(string tz)
    {
        Load();
        var result = Eval($$"""
            var threw = false;
            try { Temporal.Instant.fromEpochMilliseconds(0).toString({ timeZone: {{tz}} }); }
            catch (e) { threw = e instanceof TypeError; }
            threw;
        """);
        Assert.Equal("true", result);
    }

    [Fact]
    public void ToString_UtcTimeZone_AppendsZeroOffset()
    {
        Load();
        var result = Eval("""Temporal.Instant.fromEpochMilliseconds(0).toString({ timeZone: "UTC" });""");
        Assert.Equal("1970-01-01T00:00:00+00:00", result);
    }

    [Fact]
    public void ToString_OffsetTimeZone_ShiftsLocalAndAppendsOffset()
    {
        Load();
        var result = Eval("""Temporal.Instant.fromEpochMilliseconds(0).toString({ timeZone: "+05:30" });""");
        Assert.Equal("1970-01-01T05:30:00+05:30", result);
    }

    [Fact]
    public void ToString_NoTimeZone_StillUsesZ()
    {
        Load();
        var result = Eval("""Temporal.Instant.fromEpochMilliseconds(0).toString();""");
        Assert.Equal("1970-01-01T00:00:00Z", result);
    }
}
