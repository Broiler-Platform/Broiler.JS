using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Issue #824:
//  - 56: the unsigned right shift ">>>" is unsupported for any BigInt operand (a TypeError).
//  - 78: Intl.NumberFormat roundingIncrement ≠ 1 requires fraction-digit rounding (TypeError for
//        significant digits or a morePrecision/lessPrecision priority) and equal max/min fraction
//        digits (RangeError otherwise).
public class Issue824BigIntShiftAndRoundingIncrementTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Outcome(string expr)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval($$"""
            var out = "no-throw";
            try { {{expr}}; }
            catch (e) { out = e.constructor.name; }
            out;
        """).ToString();
    }

    [Theory]
    [InlineData("2n >>> 0n")]
    [InlineData("0n >>> 2n")]
    [InlineData("({ [Symbol.toPrimitive]() { return 2n; } }) >>> 0n")]
    [InlineData("0n >>> ({ [Symbol.toPrimitive]() { return 2n; } })")]
    [InlineData("({ valueOf() { return 2n; } }) >>> 0n")]
    [InlineData("5 >>> 2n")]
    [InlineData("2n >>> 5")]
    public void UnsignedRightShiftWithBigIntThrowsTypeError(string expr)
        => Assert.Equal("TypeError", Outcome(expr));

    [Fact]
    public void NumberShiftStillWorks()
        => Assert.Equal("no-throw", Outcome("var x = 8 >>> 1; if (x !== 4) throw new Error('bad')"));

    [Theory]
    // Invalid (unsanctioned) increments are RangeError.
    [InlineData("new Intl.NumberFormat([], { roundingIncrement: 3 })", "RangeError")]
    [InlineData("new Intl.NumberFormat([], { roundingIncrement: 5001 })", "RangeError")]
    // Increment with significant digits or a non-auto priority is TypeError.
    [InlineData("new Intl.NumberFormat([], { roundingIncrement: 2, roundingPriority: 'morePrecision' })", "TypeError")]
    [InlineData("new Intl.NumberFormat([], { roundingIncrement: 2, roundingPriority: 'lessPrecision' })", "TypeError")]
    [InlineData("new Intl.NumberFormat([], { roundingIncrement: 2, minimumSignificantDigits: 1 })", "TypeError")]
    // Increment with mismatched fraction-digit bounds is RangeError.
    [InlineData("new Intl.NumberFormat([], { roundingIncrement: 2, maximumFractionDigits: 3, minimumFractionDigits: 2 })", "RangeError")]
    // Valid uses do not throw.
    [InlineData("new Intl.NumberFormat([], { roundingIncrement: 2 })", "no-throw")]
    [InlineData("new Intl.NumberFormat([], { roundingIncrement: 5, maximumFractionDigits: 2, minimumFractionDigits: 2 })", "no-throw")]
    public void RoundingIncrementValidation(string expr, string expected)
        => Assert.Equal(expected, Outcome(expr));
}
