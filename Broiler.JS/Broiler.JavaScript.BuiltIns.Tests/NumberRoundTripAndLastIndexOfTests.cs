using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Two conformance defects found by differentially fuzzing the engine against V8 while chasing a
// Google Search failure. Neither is exotic: both are answers a page gets for ordinary input.
public class NumberRoundTripAndLastIndexOfTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    // Number::toString is defined to produce the SHORTEST string that reads back as the same Number
    // (no shorter string may denote it). .NET's "R" specifier is documented as unreliable and, for
    // 2**-25, drops the seventeenth significant digit: it renders 2.980232238769531E-08, which parses
    // back as 0x1.fffffffffffffp-26 — a different double. So `String(2**-25)` named a number that was
    // not the one it was asked about, and any page round-tripping a value through a string got a
    // different value back than it put in.
    [Theory]
    [InlineData("String(Math.pow(2, -25))", "2.9802322387695312e-8")]
    [InlineData("String(0.5 / 16777216)", "2.9802322387695312e-8")]
    [InlineData("String(1 / 33554432)", "2.9802322387695312e-8")]
    [InlineData("String(64 / 2147483648)", "2.9802322387695312e-8")]
    [InlineData("JSON.stringify(Math.pow(2, -25))", "2.9802322387695312e-8")]
    [InlineData("String(-Math.pow(2, -25))", "-2.9802322387695312e-8")]
    public void AValueNeedingSeventeenDigits_KeepsThemAll(string source, string expected)
    {
        Assert.Equal(expected, Eval(source));
    }

    /// <summary>The property that makes it a defect rather than a preference.</summary>
    [Fact(Timeout = 600000)]
    public void TheRenderedNumber_ReadsBackAsItself()
    {
        Assert.Equal("true", Eval("String(Number(String(Math.pow(2, -25))) === Math.pow(2, -25))"));
    }

    // ...and the shortest form is still the shortest: widening happens only where the short one fails
    // to round-trip, so nothing that already worked grows digits.
    [Theory]
    [InlineData("String(0.1)", "0.1")]
    [InlineData("String(0.1 + 0.2)", "0.30000000000000004")]
    [InlineData("String(1 / 3)", "0.3333333333333333")]
    [InlineData("String(Math.PI)", "3.141592653589793")]
    [InlineData("String(1e21)", "1e+21")]
    [InlineData("String(5e-324)", "5e-324")]
    [InlineData("String(1.7976931348623157e308)", "1.7976931348623157e+308")]
    [InlineData("String(100)", "100")]
    [InlineData("(0.1).toFixed(20)", "0.10000000000000000555")]
    [InlineData("(255).toString(16)", "ff")]
    public void EveryOtherValue_KeepsItsShortestForm(string source, string expected)
    {
        Assert.Equal(expected, Eval(source));
    }

    // String.prototype.lastIndexOf clamps its position into [0, length] — NOT [0, length - 1]. The
    // distinction is only visible for the empty search string, which is found AT every position
    // including one past the end. Clamping to length - 1 answered 2 for "abc".lastIndexOf(""), and a
    // negative position answered -1: "not found", for a string that is always found.
    [Theory]
    [InlineData("'abc'.lastIndexOf('')", "3")]
    [InlineData("'abc'.lastIndexOf('', 99)", "3")]
    [InlineData("'abc'.lastIndexOf('', NaN)", "3")]
    [InlineData("'abc'.lastIndexOf('', -1)", "0")]
    [InlineData("'abc'.lastIndexOf('', 1)", "1")]
    [InlineData("''.lastIndexOf('')", "0")]
    public void TheEmptySearchString_IsFoundAtEveryPositionUpToTheLength(string source, string expected)
    {
        Assert.Equal(expected, Eval(source));
    }

    // What must not change: an ordinary search still answers where the match starts, and a search
    // that cannot fit still answers -1.
    [Theory]
    [InlineData("'abc'.lastIndexOf('c')", "2")]
    [InlineData("'abc'.lastIndexOf('a')", "0")]
    [InlineData("'aXbXc'.lastIndexOf('X')", "3")]
    [InlineData("'aXbXc'.lastIndexOf('X', 2)", "1")]
    [InlineData("'abc'.lastIndexOf('d')", "-1")]
    [InlineData("'abc'.lastIndexOf('abcd')", "-1")]
    [InlineData("''.lastIndexOf('a')", "-1")]
    [InlineData("'abc'.lastIndexOf('bc')", "1")]
    [InlineData("'abc'.lastIndexOf('bc', 0)", "-1")]
    [InlineData("'abcabc'.lastIndexOf('abc')", "3")]
    [InlineData("'abcabc'.lastIndexOf('abc', 2)", "0")]
    public void AnOrdinarySearch_IsUnchanged(string source, string expected)
    {
        Assert.Equal(expected, Eval(source));
    }
}
