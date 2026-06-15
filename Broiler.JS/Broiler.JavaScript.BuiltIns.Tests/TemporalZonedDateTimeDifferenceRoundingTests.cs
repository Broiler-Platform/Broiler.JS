using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Temporal.ZonedDateTime.prototype.since/until apply the time-unit rounding options (issue #798
// problems 18-21), coercing object-valued options via toString.
public class TemporalZonedDateTimeDifferenceRoundingTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string expr)
    {
        using var ctx = new JSContext();
        return ctx.Eval(expr).ToString();
    }

    [Fact]
    public void Since_TimeUnit_RoundsToSmallestUnit()
    {
        Load();
        // 987654321 ns truncated to microseconds → nanoseconds component 0.
        var result = Eval("""
            var a = new Temporal.ZonedDateTime(0n, "UTC");
            var b = new Temporal.ZonedDateTime(987654321n, "UTC");
            var d = b.since(a, { smallestUnit: "microsecond" });
            [d.microseconds, d.nanoseconds].join(',');
        """);
        Assert.Equal("654,0", result);
    }

    [Fact]
    public void Since_SmallestUnit_ObjectWithToString_IsCoerced()
    {
        Load();
        var result = Eval("""
            var a = new Temporal.ZonedDateTime(0n, "UTC");
            var b = new Temporal.ZonedDateTime(987654321n, "UTC");
            var d = b.since(a, { smallestUnit: { toString() { return "microsecond"; } } });
            d.nanoseconds;
        """);
        Assert.Equal("0", result);
    }

    [Fact]
    public void Until_DefaultNanosecond_IsUnchanged()
    {
        Load();
        var result = Eval("""
            var a = new Temporal.ZonedDateTime(0n, "UTC");
            var b = new Temporal.ZonedDateTime(987654321n, "UTC");
            var d = a.until(b);
            [d.milliseconds, d.microseconds, d.nanoseconds].join(',');
        """);
        Assert.Equal("987,654,321", result);
    }
}
