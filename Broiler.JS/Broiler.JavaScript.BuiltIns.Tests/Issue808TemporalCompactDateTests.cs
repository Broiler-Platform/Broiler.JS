using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Temporal ISO parsing accepts the basic (compact) calendar-date form YYYYMMDD in addition to the
// extended YYYY-MM-DD form, for both Temporal.PlainDateTime.from and the time extracted by
// Temporal.PlainTime.from. Issue #808 problems 93 & 94.
public class Issue808TemporalCompactDateTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    [Theory]
    [InlineData("19761118T15:23:30.1+00:00", "1976-11-18T15:23:30.1")]
    [InlineData("19761118T152330", "1976-11-18T15:23:30")]
    [InlineData("19761118", "1976-11-18T00:00:00")]
    [InlineData("1976-11-18T15:23:30", "1976-11-18T15:23:30")] // extended form still works
    public void PlainDateTime_From_CompactDate(string input, string expected)
        => Assert.Equal(expected, Eval($"Temporal.PlainDateTime.from('{input}').toString();"));

    [Theory]
    [InlineData("19761118T15:23:30.1+00:00", "15:23:30.1")]
    [InlineData("19761118T152330", "15:23:30")]
    [InlineData("15:23:30", "15:23:30")] // bare time still works
    public void PlainTime_From_CompactDate(string input, string expected)
        => Assert.Equal(expected, Eval($"Temporal.PlainTime.from('{input}').toString();"));

    [Fact]
    public void PlainDateTime_From_BareZDesignator_StillRejected()
        => Assert.Equal("RangeError", Eval("""
            var err = "none";
            try { Temporal.PlainDateTime.from('19761118T15:23:30Z'); }
            catch (e) { err = e.constructor.name; }
            err;
        """));
}
