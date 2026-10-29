using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Temporal.ZonedDateTime.prototype.since/until apply calendar-unit rounding (year/month/week/day) to
// the DST-aware date difference, and reject an increment whose day boundary falls outside the
// representable range. Issue #805 problems 29 and 50. (Time-unit smallestUnits already rounded.)
public class TemporalZonedDateTimeRoundingTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);
    private static string E(string e) { Load(); using var c = new JSContext(); return c.Eval(e).ToString(); }

    // 2019-01-08T08:22:36.123456789Z .. 2021-09-07T12:39:40.987654289Z, both in UTC.
    private const string Pair = """
        var earlier = new Temporal.ZonedDateTime(1546935756123456789n, 'UTC');
        var later = new Temporal.ZonedDateTime(1631018380987654289n, 'UTC');
    """;

    [Theory]
    [InlineData("years", "P3Y")]
    [InlineData("months", "P32M")]
    [InlineData("weeks", "P140W")]
    [InlineData("days", "P974D")]
    public void Since_RoundsCalendarUnit_Ceil(string unit, string expected)
        => Assert.Equal(expected, E($"{Pair} later.since(earlier, {{ smallestUnit: '{unit}', roundingMode: 'ceil' }}).toString();"));

    [Theory]
    [InlineData("years", "-P2Y")]
    [InlineData("months", "-P31M")]
    [InlineData("days", "-P973D")]
    public void Since_NegativeDirection_Ceil(string unit, string expected)
        => Assert.Equal(expected, E($"{Pair} earlier.since(later, {{ smallestUnit: '{unit}', roundingMode: 'ceil' }}).toString();"));

    [Fact]
    public void Since_TruncIsDefault()
        => Assert.Equal("P2Y", E($"{Pair} later.since(earlier, {{ smallestUnit: 'years' }}).toString();"));

    [Theory]
    // An increment whose ending day boundary is past ±10^8 days from the epoch is a RangeError;
    // exactly 10^8 days is in range.
    [InlineData("1e8 + 1", "RangeError")]
    [InlineData("1e8", "none")]
    public void Since_DayIncrement_RangeCheck(string increment, string expected)
    {
        var actual = E($$"""
            var earlier = new Temporal.ZonedDateTime(0n, 'UTC');
            var later = new Temporal.ZonedDateTime(5n, 'UTC');
            var err = 'none';
            try { later.since(earlier, { smallestUnit: 'days', roundingIncrement: {{increment}}, roundingMode: 'expand' }); }
            catch (e) { err = e.constructor.name; }
            err;
        """);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Until_RoundsCalendarUnit_HalfExpand()
        => Assert.Equal("P3Y", E($"{Pair} earlier.until(later, {{ smallestUnit: 'years', roundingMode: 'halfExpand' }}).toString();"));
}
