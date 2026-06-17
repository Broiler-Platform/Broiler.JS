using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Issue #824: assorted test262 failures in Math and BigInt.
// - Math.sign preserves the sign of zero (problem 68).
// - Math.clz32 applies ToUint32 so values >= 2^32 wrap (problem 76).
// - Math.pow returns NaN for a NaN exponent even when the base is 1 (problem 71).
// - BigInt division / modulo by zero throws RangeError (problems 74 & 75).
// - BigInt.prototype.toString honours the radix argument and validates it (problem 70).
public class Issue824MathBigIntTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    [Theory]
    [InlineData("Object.is(Math.sign(0), 0)", "true")]
    [InlineData("Object.is(Math.sign(-0), -0)", "true")]
    [InlineData("Object.is(Math.sign(0), -0)", "false")]
    [InlineData("Math.sign(3)", "1")]
    [InlineData("Math.sign(-3)", "-1")]
    [InlineData("String(Math.sign(NaN))", "NaN")]
    public void Sign(string expr, string expected)
        => Assert.Equal(expected, Eval($"String({expr});"));

    [Theory]
    [InlineData("Math.clz32(Math.pow(2, 32))", "32")] // ToUint32(2^32) === 0
    [InlineData("Math.clz32(0)", "32")]
    [InlineData("Math.clz32(1)", "31")]
    [InlineData("Math.clz32(Math.pow(2, 32) + 1)", "31")] // wraps to 1
    public void Clz32(string expr, string expected)
        => Assert.Equal(expected, Eval($"String({expr});"));

    [Theory]
    [InlineData("Math.pow(1, NaN)", "NaN")]
    [InlineData("Math.pow(-1, NaN)", "NaN")]
    [InlineData("Math.pow(2, NaN)", "NaN")]
    [InlineData("Math.pow(NaN, 0)", "1")]
    [InlineData("Math.pow(1, Infinity)", "NaN")]
    public void Pow(string expr, string expected)
        => Assert.Equal(expected, Eval($"String({expr});"));

    [Theory]
    [InlineData("1n / 0n")]
    [InlineData("1n % 0n")]
    public void BigIntDivideByZeroThrowsRangeError(string expr)
        => Assert.Equal("true", Eval(
            "var ok = false; try { " + expr + "; } catch (e) { ok = e instanceof RangeError; } String(ok);"));

    [Theory]
    [InlineData("255n.toString(16)", "ff")]
    [InlineData("255n.toString(2)", "11111111")]
    [InlineData("(-255n).toString(16)", "-ff")]
    [InlineData("0n.toString(16)", "0")]
    [InlineData("123456789n.toString()", "123456789")]
    public void BigIntToStringRadix(string expr, string expected)
        => Assert.Equal(expected, Eval($"String({expr});"));

    [Theory]
    [InlineData("0n.toString(0)")]
    [InlineData("0n.toString(1)")]
    [InlineData("0n.toString(37)")]
    public void BigIntToStringInvalidRadixThrowsRangeError(string expr)
        => Assert.Equal("true", Eval(
            "var ok = false; try { " + expr + "; } catch (e) { ok = e instanceof RangeError; } String(ok);"));
}
