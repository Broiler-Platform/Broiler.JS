using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Math.expm1 / Math.acosh / Math.log2 use accurate algorithms (the cancellation-free expm1 formula and
// the correctly-rounded .NET Math.Acosh / Math.Log2) instead of the naive forms that lose precision for
// small / extreme arguments. Issue #808 problems 96, 97 & 98.
public class Issue808MathPrecisionTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    [Theory]
    [InlineData("Math.expm1(1e-300)", "1e-300")]       // would underflow to 0 with e^x - 1
    [InlineData("Math.expm1(-1e-300)", "-1e-300")]
    [InlineData("Math.expm1(0)", "0")]
    [InlineData("Math.expm1(1)", "1.718281828459045")]
    [InlineData("Math.expm1(-1000)", "-1")]            // e^x underflows; result is still -1
    [InlineData("Math.expm1(710)", "Infinity")]        // overflow
    [InlineData("Math.log2(1024)", "10")]
    [InlineData("Math.log2(1)", "0")]
    [InlineData("Math.log2(0)", "-Infinity")]
    [InlineData("Math.log2(-1)", "NaN")]
    [InlineData("Math.acosh(1)", "0")]
    [InlineData("Math.acosh(0.5)", "NaN")]
    [InlineData("Math.acosh(Infinity)", "Infinity")]
    public void Accurate(string expr, string expected)
        => Assert.Equal(expected, Eval($"String({expr});"));

    [Fact]
    public void Expm1_PreservesNegativeZeroSign()
        => Assert.Equal("true", Eval("String(Object.is(Math.expm1(-0), -0));"));

    [Theory]
    [InlineData("Math.LOG2E", "1.4426950408889634")]   // base-2 log of E, not ln(E)=1
    [InlineData("Math.LOG10E", "0.4342944819032518")]
    [InlineData("Math.LN2", "0.6931471805599453")]
    [InlineData("Math.LN10", "2.302585092994046")]
    [InlineData("Math.E", "2.718281828459045")]
    public void Constants(string expr, string expected)
        => Assert.Equal(expected, Eval($"String({expr});"));

    [Fact]
    public void Log2_OfPowersOfTwo_AreExact()
        => Assert.Equal("true", Eval("""
            var ok = true;
            for (var i = -50; i < 50; i++) if (Math.log2(Math.pow(2, i)) !== i) ok = false;
            String(ok);
        """));
}
