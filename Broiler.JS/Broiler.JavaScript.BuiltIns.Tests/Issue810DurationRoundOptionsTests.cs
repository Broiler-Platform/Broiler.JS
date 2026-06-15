using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Temporal.Duration.prototype.round option parsing: an explicit largestUnit: "auto" counts as a
// supplied largestUnit (so the receiver is not "under-specified"), and a non-integer roundingIncrement
// is truncated toward zero rather than rejected. Issue #810 problems 84 & 85.
public class Issue810DurationRoundOptionsTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    [Fact]
    public void Round_LargestUnitAuto_Succeeds()
        => Assert.Equal("PT25H", Eval("""
            var d = new Temporal.Duration(0, 0, 0, 0, 25);
            d.round({ largestUnit: "auto" }).toString();
        """));

    [Theory]
    [InlineData("2.5", "P2D")]    // truncates to an increment of 2
    [InlineData("1.9", "P2D")]    // truncates to an increment of 1
    public void Round_NonIntegerRoundingIncrement_Truncates(string increment, string expected)
        => Assert.Equal(expected, Eval($$"""
            var d = new Temporal.Duration(0, 0, 0, 2);
            d.round({ smallestUnit: "days", roundingIncrement: {{increment}} }).toString();
        """));

    [Fact]
    public void Round_NoSmallestOrLargestUnit_ThrowsRangeError()
        => Assert.Equal("RangeError", Eval("""
            var d = new Temporal.Duration(0, 0, 0, 0, 25);
            try { d.round({}); "no throw"; } catch (e) { e.constructor.name; }
        """));

    [Fact]
    public void Round_RoundingIncrementBelowOne_ThrowsRangeError()
        => Assert.Equal("RangeError", Eval("""
            var d = new Temporal.Duration(0, 0, 0, 2);
            try { d.round({ smallestUnit: "days", roundingIncrement: 0.5 }); "no throw"; }
            catch (e) { e.constructor.name; }
        """));
}
