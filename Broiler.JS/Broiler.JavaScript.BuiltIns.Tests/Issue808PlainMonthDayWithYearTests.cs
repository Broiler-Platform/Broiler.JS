using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Temporal.PlainMonthDay.prototype.with recognises « year, month, monthCode, day »; "year" is a valid
// field (used only for overflow resolution), so supplying it alone satisfies the "at least one field"
// requirement and, for the ISO calendar, returns the original month/day. An empty object or only
// unrecognised (misspelled) fields is still a TypeError. Issue #808 problem 43.
public class Issue808PlainMonthDayWithYearTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    [Theory]
    [InlineData("{ year: 2000 }", "01-22")]
    [InlineData("{ year: 2000, month: 12 }", "12-22")]
    [InlineData("{ day: 5 }", "01-05")]
    public void With_RecognisedFields_Succeed(string fields, string expected)
        => Assert.Equal(expected, Eval($"Temporal.PlainMonthDay.from('01-22').with({fields}).toString();"));

    [Theory]
    [InlineData("{}")]
    [InlineData("{ months: 1 }")]      // misspelled field name
    [InlineData("{ foo: 1 }")]
    public void With_NoRecognisedField_Throws(string fields)
        => Assert.Equal("TypeError", Eval($$"""
            var md = Temporal.PlainMonthDay.from('01-22');
            var err = "none";
            try { md.with({{fields}}); }
            catch (e) { err = e.constructor.name; }
            err;
        """));
}
