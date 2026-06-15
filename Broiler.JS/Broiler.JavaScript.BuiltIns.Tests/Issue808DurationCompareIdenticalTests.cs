using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Temporal.Duration.compare returns +0 when the two durations are identical in every field. The spec
// applies this short-circuit before the calendar-units requirement, so comparing a duration that has
// calendar units (years/months/weeks) to itself returns 0 rather than throwing for a missing
// relativeTo. Issue #808 problem 45.
public class Issue808DurationCompareIdenticalTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    [Theory]
    [InlineData("{ years: 1 }")]
    [InlineData("{ months: 1 }")]
    [InlineData("{ weeks: 1 }")]
    [InlineData("{ years: 1, months: 2, weeks: 3, days: 4 }")]
    [InlineData("{ hours: 3 }")]
    public void Compare_IdenticalDuration_ReturnsZero(string fields)
    {
        var actual = Eval($$"""
            var d = Temporal.Duration.from({{fields}});
            var out = "none";
            try { out = String(Temporal.Duration.compare(d, d)); }
            catch (e) { out = e.constructor.name + ": " + e.message; }
            out;
        """);
        Assert.Equal("0", actual);
    }

    [Fact]
    public void Compare_DifferentCalendarDurations_StillRequiresRelativeTo()
    {
        var actual = Eval("""
            var a = Temporal.Duration.from({ months: 1 });
            var b = Temporal.Duration.from({ months: 2 });
            var err = "none";
            try { Temporal.Duration.compare(a, b); }
            catch (e) { err = e.constructor.name; }
            err;
        """);
        Assert.Equal("RangeError", actual);
    }

    [Fact]
    public void Compare_IdenticalDuration_StillValidatesRelativeTo()
    {
        // The spec resolves relativeTo before the identical-durations short-circuit, so an invalid
        // relativeTo still throws even when the two durations are identical.
        var actual = Eval("""
            var d = Temporal.Duration.from({ months: 1 });
            var err = "none";
            try { Temporal.Duration.compare(d, d, { relativeTo: "not-a-date" }); }
            catch (e) { err = e.constructor.name; }
            err;
        """);
        Assert.Equal("RangeError", actual);
    }
}
