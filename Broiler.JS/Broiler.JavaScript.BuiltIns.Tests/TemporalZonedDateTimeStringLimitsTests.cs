using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.BuiltIns.Tests;

// ZonedDateTime / Duration-relativeTo string parsing at the representable-range edges and with
// time-zone annotations (issue #800, problems 13, 14, 17). A ZonedDateTime string is valid only when
// both its instant and its wall-clock time are in range; a Duration relativeTo string that carries a
// time-zone annotation parses as a ZonedDateTime (range-validated, Z / offset honoured) rather than
// being rejected by the calendar-only PlainDate parser. Vectors mirror the test262 *-string-limits
// and relativeto-string files.
public class TemporalZonedDateTimeStringLimitsTests
{
    private static void EnsureBuiltInsLoaded()
        => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static bool Throws(string source)
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        return ctx.Eval($$"""
            let threw = false;
            try { {{source}} } catch (e) { threw = e instanceof RangeError; }
            threw;
        """).BooleanValue;
    }

    // problem 17 — ZonedDateTime.prototype.equals argument-string-limits
    [Theory]
    [InlineData("-271821-04-20T00:00Z[UTC]")]
    [InlineData("+275760-09-13T00:00Z[UTC]")]
    [InlineData("+275760-09-13T01:00+01:00[+01:00]")]
    [InlineData("+275760-09-13T23:59+23:59[+23:59]")]
    public void ZonedEquals_ValidStringLimits_DoNotThrow(string arg)
        => Assert.False(Throws($"new Temporal.ZonedDateTime(0n, 'UTC').equals('{arg}');"));

    [Theory]
    [InlineData("-271821-04-19T23:00-01:00[-01:00]")]
    [InlineData("-271821-04-19T00:01-23:59[-23:59]")]
    [InlineData("-271821-04-19T23:59:59.999999999Z[UTC]")]
    [InlineData("+275760-09-13T00:00:00.000000001Z[UTC]")]
    [InlineData("+275760-09-13T01:00+00:59[+00:59]")]
    [InlineData("+275760-09-14T00:00+23:59[+23:59]")]
    public void ZonedEquals_OutOfRangeStringLimits_Throw(string arg)
        => Assert.True(Throws($"new Temporal.ZonedDateTime(0n, 'UTC').equals('{arg}');"));

    // problems 13/14 — Duration relativeTo strings with time-zone annotations
    [Theory]
    [InlineData("-271821-04-20T00:00Z[UTC]")]
    [InlineData("+275760-09-13T23:59+23:59[+23:59]")]
    public void DurationCompare_ValidRelativeToLimits_DoNotThrow(string arg)
        => Assert.False(Throws(
            $"Temporal.Duration.compare(new Temporal.Duration(0,0,0,0,0,5), new Temporal.Duration(), {{ relativeTo: '{arg}' }});"));

    [Theory]
    [InlineData("-271821-04-19T23:00-01:00[-01:00]")]
    [InlineData("+275760-09-14T00:00+23:59[+23:59]")]
    public void DurationCompare_OutOfRangeRelativeTo_Throw(string arg)
        => Assert.True(Throws(
            $"Temporal.Duration.compare(new Temporal.Duration(0,0,0,0,0,5), new Temporal.Duration(), {{ relativeTo: '{arg}' }});"));

    [Theory]
    // relativeto-string: every form (bare, offset, [UTC]/offset annotation, Z, basic date, date-only
    // with zone, calendar annotation) is a valid relativeTo and rounds 1y24h to 1y1d.
    [InlineData("2019-11-01T00:00[-07:00]")]
    [InlineData("2019-11-01T00:00Z[-07:00]")]
    [InlineData("2019-11-01T00:00+00:00[UTC]")]
    [InlineData("20200101")]
    [InlineData("2000-01-01[UTC]")]
    [InlineData("2000-01-01T00:00+00:00[UTC][u-ca=iso8601]")]
    public void DurationRound_RelativeToStringForms_Resolve(string relativeTo)
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval($$"""
            const d = new Temporal.Duration(1, 0, 0, 0, 24).round({ largestUnit: "years", relativeTo: "{{relativeTo}}" });
            [d.years, d.months, d.weeks, d.days].join(',');
        """);
        Assert.Equal("1,0,0,1", result.ToString());
    }

    [Fact]
    public void PlainDate_AcceptsBasicDateForm()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        Assert.Equal("2020-01-01", ctx.Eval("Temporal.PlainDate.from('20200101').toString();").ToString());
    }
}
