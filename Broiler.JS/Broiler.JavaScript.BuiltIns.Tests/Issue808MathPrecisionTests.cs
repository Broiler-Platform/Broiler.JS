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
}
