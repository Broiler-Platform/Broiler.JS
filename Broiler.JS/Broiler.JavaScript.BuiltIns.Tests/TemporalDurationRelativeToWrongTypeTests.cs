using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// ToRelativeTemporalObject: the relativeTo value itself, when present and not undefined, is converted
// before any unit validation. A non-string primitive (null, boolean, number, bigint, symbol) is a
// TypeError, while a string / undefined follows ISO-string rules (RangeError). Issue #805 problem 47.
public class TemporalDurationRelativeToWrongTypeTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string ErrorOf(string method)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval($$"""
            var d = new Temporal.Duration(1, 0, 0, 1);
            var err = "none";
            try { d.{{method}}; }
            catch (e) { err = e.constructor.name; }
            err;
        """).ToString();
    }

    [Theory]
    [InlineData("null", "TypeError")]
    [InlineData("true", "TypeError")]
    [InlineData("1", "TypeError")]
    [InlineData("1n", "TypeError")]
    [InlineData("Symbol()", "TypeError")]
    [InlineData("''", "RangeError")]       // empty string is an unparsable ISO string
    [InlineData("'invalid'", "RangeError")]
    public void Round_RelativeTo_WrongType(string relativeTo, string expected)
    {
        var actual = ErrorOf($"round({{ largestUnit: 'years', relativeTo: {relativeTo} }})");
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("null", "TypeError")]
    [InlineData("true", "TypeError")]
    [InlineData("1", "TypeError")]
    [InlineData("1n", "TypeError")]
    [InlineData("Symbol()", "TypeError")]
    [InlineData("''", "RangeError")]
    public void Total_RelativeTo_WrongType(string relativeTo, string expected)
    {
        var actual = ErrorOf($"total({{ unit: 'years', relativeTo: {relativeTo} }})");
        Assert.Equal(expected, actual);
    }
}
