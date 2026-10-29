using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Calendar / time-zone identifier canonicalization (issue #798 problems 24, 25).
public class TemporalIdentifierCanonicalizationTests
{
    private static void EnsureBuiltInsLoaded()
        => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    [Theory]
    // An IANA time-zone identifier is matched case-insensitively and normalized (problem 25).
    [InlineData("Africa/CAIRO", "Africa/Cairo")]
    [InlineData("africa/cairo", "Africa/Cairo")]
    [InlineData("Africa/Cairo", "Africa/Cairo")]
    public void ZonedDateTime_TimeZoneId_IsCaseNormalized(string input, string expected)
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval($$"""
            new Temporal.ZonedDateTime(0n, "{{input}}").timeZoneId;
        """);
        Assert.Equal(expected, result.ToString());
    }

    [Theory]
    // A bare time string used as a calendar argument parses (no u-ca annotation) to iso8601
    // rather than being mis-read as an invalid year-month identifier (problem 24).
    [InlineData("152330")]
    [InlineData("15:23:30")]
    [InlineData("15:23")]
    [InlineData("T15:23:30")]
    [InlineData("152330-08")]
    public void PlainDate_WithCalendar_TimeString_ResolvesToIso8601(string arg)
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval($$"""
            new Temporal.PlainDate(2020, 1, 1).withCalendar("{{arg}}").calendarId;
        """);
        Assert.Equal("iso8601", result.ToString());
    }

    [Fact]
    public void YearMonthString_StillParsesWhenValid()
    {
        // A valid no-separator year-month must still parse as a year-month, not a time.
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("""Temporal.PlainYearMonth.from("152012").toString();""");
        Assert.Equal("1520-12", result.ToString());
    }
}
