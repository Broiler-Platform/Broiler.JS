using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/847
public class Issue847Tests
{
    private static JSValue Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code);
    }

    // Problems 72/85/86/87/95/96/99: String.prototype.padStart / padEnd built the
    // padding from only the first character of the fill string (PadLeft/PadRight
    // with fillString[0]) instead of the StringPad filler, which is the whole fill
    // string repeated and truncated to the required width.
    [Theory]
    [InlineData("'abc'.padEnd(7, 'def')", "abcdefd")]
    [InlineData("'abc'.padStart(7, 'def')", "defdabc")]
    [InlineData("'abc'.padEnd(11, 'def')", "abcdefdefde")]
    [InlineData("'abc'.padStart(11, 'def')", "defdefdeabc")]
    [InlineData("'42'.padEnd(7, 'bloop')", "42bloop")]
    [InlineData("'abc'.padEnd(10, false)", "abcfalsefa")]
    [InlineData("'abc'.padStart(10, false)", "falsefaabc")]
    public void PadRepeatsWholeFillString(string code, string expected)
    {
        Assert.Equal(expected, Eval(code).ToString());
    }

    // The default fill string is a single space, and a maxLength that does not
    // exceed the current length returns the string unchanged.
    [Theory]
    [InlineData("'abc'.padStart(6)", "   abc")]
    [InlineData("'abc'.padEnd(6)", "abc   ")]
    [InlineData("'abc'.padStart(2, 'x')", "abc")]
    [InlineData("'abc'.padEnd(2, 'x')", "abc")]
    [InlineData("'abc'.padEnd(7, '')", "abc")]
    public void PadEdgeCases(string code, string expected)
    {
        Assert.Equal(expected, Eval(code).ToString());
    }

    // maxLength is coerced (ToLength) before fillString is coerced (ToString), and
    // fillString must not be coerced at all when no padding is required.
    [Fact]
    public void FillStringNotCoercedWhenNoPaddingNeeded()
    {
        var code = "var coerced = false;"
            + "var fill = { toString() { coerced = true; return 'x'; } };"
            + "'abc'.padEnd(2, fill);"
            + "coerced;";
        Assert.False(Eval(code).BooleanValue);
    }
}
