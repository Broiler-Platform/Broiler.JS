using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Issue #824 problem 30: Temporal.PlainTime.from must reject ISO time / offset strings that mix
// colon and bare-digit separators (e.g. "00:0000", "0000:00", "00:00:00+00:0000"). The lenient
// parse pattern previously accepted these; the time-of-day and UTC-offset grammars both require
// consistent separators.
public class Issue824PlainTimeStringTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string ErrorOf(string expr)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval($$"""
            var err = "none";
            try { {{expr}}; }
            catch (e) { err = e.constructor.name; }
            err;
        """).ToString();
    }

    [Theory]
    // Mixed time-of-day separators.
    [InlineData("Temporal.PlainTime.from('0000:00')")]
    [InlineData("Temporal.PlainTime.from('00:0000')")]
    // Mixed UTC-offset separators.
    [InlineData("Temporal.PlainTime.from('00:00:00+00:0000')")]
    [InlineData("Temporal.PlainTime.from('00:00:00+0000:00')")]
    // Pre-existing rejections that must keep throwing.
    [InlineData("Temporal.PlainTime.from('00:00-24:00')")]
    [InlineData("Temporal.PlainTime.from('00:00+24:00')")]
    [InlineData("Temporal.PlainTime.from('25:00:00Z')")]
    [InlineData("Temporal.PlainTime.from('01:60:00Z')")]
    public void InvalidStringsThrowRangeError(string expr)
        => Assert.Equal("RangeError", ErrorOf(expr));

    [Theory]
    // Consistent colon separators.
    [InlineData("String(Temporal.PlainTime.from('12:34:56'))", "12:34:56")]
    [InlineData("String(Temporal.PlainTime.from('12:34'))", "12:34:00")]
    // Consistent bare separators.
    [InlineData("String(Temporal.PlainTime.from('123456'))", "12:34:56")]
    [InlineData("String(Temporal.PlainTime.from('1234'))", "12:34:00")]
    // Bare hour only.
    [InlineData("String(Temporal.PlainTime.from('12'))", "12:00:00")]
    // Fractional seconds.
    [InlineData("String(Temporal.PlainTime.from('12:34:56.789'))", "12:34:56.789")]
    // A well-formed numeric offset is accepted (and ignored) for both separator styles.
    [InlineData("String(Temporal.PlainTime.from('12:34:56+01:00'))", "12:34:56")]
    [InlineData("String(Temporal.PlainTime.from('12:34:56+0100'))", "12:34:56")]
    public void ValidStringsParse(string expr, string expected)
        => Assert.Equal(expected, Eval(expr));

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }
}
