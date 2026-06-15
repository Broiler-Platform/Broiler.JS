using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// A wall-clock Temporal type cannot be parsed from a string carrying a UTC (Z) designator, and the
// string is parsed before the options argument is validated — so an invalid string yields a
// RangeError even when options is a bad (non-object) type. Issue #805 problem 43.
public class TemporalFromZAndOptionsOrderTests
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
    [InlineData("Temporal.PlainDateTime.from('1976-11-18T12:34Z')", "RangeError")]
    [InlineData("Temporal.PlainTime.from('1976-11-18T12:34Z')", "RangeError")]
    // A numeric offset is allowed (and ignored) for these wall-clock types.
    [InlineData("Temporal.PlainDateTime.from('1976-11-18T12:34+01:00')", "none")]
    [InlineData("Temporal.PlainTime.from('12:34+01:00')", "none")]
    public void From_ZDesignator_Rejected(string expr, string expected)
        => Assert.Equal(expected, ErrorOf(expr));

    [Theory]
    // The string is parsed (and its Z rejected with RangeError) before the bad options type is observed.
    [InlineData("Temporal.PlainDateTime.from('1976-11-18T12:34Z', null)", "RangeError")]
    [InlineData("Temporal.PlainDateTime.from('1976-11-18T12:34Z', 42)", "RangeError")]
    [InlineData("Temporal.PlainTime.from('1976-11-18T12:34Z', null)", "RangeError")]
    [InlineData("Temporal.PlainTime.from('1976-11-18T12:34Z', 'bogus')", "RangeError")]
    public void From_InvalidString_ParsedBeforeOptionsValidated(string expr, string expected)
        => Assert.Equal(expected, ErrorOf(expr));

    [Theory]
    // A valid string with a bad options type still surfaces the options TypeError.
    [InlineData("Temporal.PlainDateTime.from('1976-11-18T12:34', null)", "TypeError")]
    [InlineData("Temporal.PlainTime.from('12:34', 42)", "TypeError")]
    public void From_ValidString_BadOptions_TypeError(string expr, string expected)
        => Assert.Equal(expected, ErrorOf(expr));
}
