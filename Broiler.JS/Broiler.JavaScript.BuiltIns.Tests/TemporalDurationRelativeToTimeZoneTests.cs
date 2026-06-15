using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Temporal.Duration relativeTo property bag validates its timeZone field (issue #798
// problems 10, 17): a bare date-time string is a RangeError, a wrong-type value a TypeError.
public class TemporalDurationRelativeToTimeZoneTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string expr)
    {
        using var ctx = new JSContext();
        return ctx.Eval(expr).ToString();
    }

    [Theory]
    [InlineData("\"2020-01-01T00:00\"", "RangeError")]   // bare date-time string is not a time zone (problem 10)
    [InlineData("null", "TypeError")]                     // wrong type (problem 17)
    [InlineData("42", "TypeError")]
    [InlineData("Symbol()", "TypeError")]
    public void Round_RelativeToPropertyBagTimeZone_Throws(string tz, string errorType)
    {
        Load();
        var result = Eval($$"""
            var threw = "";
            try {
                Temporal.Duration.from({ hours: 24 }).round({
                    largestUnit: "days",
                    relativeTo: { year: 2020, month: 1, day: 1, timeZone: {{tz}} }
                });
            } catch (e) { threw = e.constructor.name; }
            threw;
        """);
        Assert.Equal(errorType, result);
    }
}
