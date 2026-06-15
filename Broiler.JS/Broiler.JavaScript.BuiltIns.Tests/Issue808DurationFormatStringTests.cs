using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Intl.DurationFormat.prototype.format / formatToParts accept an ISO 8601 (Temporal) duration string
// argument, parsing it into the same duration record an equivalent object would produce. An invalid
// string is a RangeError; a non-string, non-object argument is a TypeError. Issue #808 problem 42.
public class Issue808DurationFormatStringTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    [Fact]
    public void Format_StringArg_MatchesEquivalentObject()
        => Assert.Equal("true", Eval("""
            var df = new Intl.DurationFormat("en");
            var s = df.format("P1Y2M3W4DT5H6M7.00800901S");
            var o = df.format({ years: 1, months: 2, weeks: 3, days: 4, hours: 5,
                minutes: 6, seconds: 7, milliseconds: 8, microseconds: 9, nanoseconds: 10 });
            String(s === o && s.length > 0);
        """));

    [Fact]
    public void FormatToParts_StringArg_ProducesParts()
        => Assert.Equal("true", Eval("""
            var df = new Intl.DurationFormat("en");
            String(df.formatToParts("P1Y2M").length > 0);
        """));

    [Fact]
    public void Format_InvalidString_ThrowsRangeError()
        => Assert.Equal("RangeError", Eval("""
            var df = new Intl.DurationFormat("en");
            var err = "none";
            try { df.format("not-a-duration"); }
            catch (e) { err = e.constructor.name; }
            err;
        """));

    [Theory]
    [InlineData("5")]
    [InlineData("true")]
    [InlineData("null")]
    public void Format_NonStringNonObject_ThrowsTypeError(string arg)
        => Assert.Equal("TypeError", Eval($$"""
            var df = new Intl.DurationFormat("en");
            var err = "none";
            try { df.format({{arg}}); }
            catch (e) { err = e.constructor.name; }
            err;
        """));
}
