using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Temporal.PlainTime.prototype.since/until apply the smallestUnit / roundingIncrement /
// roundingMode options (issue #798 problems 18-21), coercing object-valued options via toString.
public class TemporalPlainTimeDifferenceRoundingTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string expr)
    {
        using var ctx = new JSContext();
        return ctx.Eval(expr).ToString();
    }

    [Fact]
    public void Since_RoundsToSmallestUnit()
    {
        Load();
        // 01:01:01.987654321 since 00:00:00, truncated to microseconds → ...654us, 0ns.
        var result = Eval("""
            var later = Temporal.PlainTime.from("01:01:01.987654321");
            var earlier = Temporal.PlainTime.from("00:00:00");
            var d = later.since(earlier, { smallestUnit: "microsecond" });
            [d.milliseconds, d.microseconds, d.nanoseconds].join(',');
        """);
        Assert.Equal("987,654,0", result);
    }

    [Fact]
    public void Since_SmallestUnit_ObjectWithToString_IsCoerced()
    {
        Load();
        // The object smallestUnit must be coerced through toString to "microsecond".
        var result = Eval("""
            var later = Temporal.PlainTime.from("01:01:01.987654321");
            var earlier = Temporal.PlainTime.from("00:00:00");
            var d = later.since(earlier, { smallestUnit: { toString() { return "microsecond"; } } });
            d.nanoseconds;
        """);
        Assert.Equal("0", result);
    }

    [Fact]
    public void Until_RoundsWithIncrementAndMode()
    {
        Load();
        // 30 minutes until, rounded to nearest hour (halfExpand) → 1 hour.
        var result = Eval("""
            var a = Temporal.PlainTime.from("00:00:00");
            var b = Temporal.PlainTime.from("00:30:00");
            var d = a.until(b, { smallestUnit: "hour", roundingMode: "halfExpand" });
            [d.hours, d.minutes].join(',');
        """);
        Assert.Equal("1,0", result);
    }

    [Fact]
    public void Since_NegatesRoundingMode()
    {
        Load();
        // since with ceil: this.since(other) = this - other; ceil rounds toward +∞.
        // 00:30 since 00:00 = +30min; ceil to hour → 1 hour.
        var result = Eval("""
            var a = Temporal.PlainTime.from("00:30:00");
            var b = Temporal.PlainTime.from("00:00:00");
            var d = a.since(b, { smallestUnit: "hour", roundingMode: "ceil" });
            d.hours;
        """);
        Assert.Equal("1", result);
    }

    [Fact]
    public void Until_LargestUnit_FoldsIntoLargestUnit()
    {
        Load();
        // largestUnit "minute" → no hours; 1h30m becomes 90 minutes.
        var result = Eval("""
            var a = Temporal.PlainTime.from("00:00:00");
            var b = Temporal.PlainTime.from("01:30:00");
            var d = a.until(b, { largestUnit: "minute" });
            [d.hours, d.minutes].join(',');
        """);
        Assert.Equal("0,90", result);
    }
}
