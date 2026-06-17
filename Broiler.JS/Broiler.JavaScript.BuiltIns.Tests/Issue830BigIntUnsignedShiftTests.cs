using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Issue #830 (problem 35): the ">>>" operator coerces both operands with ToNumeric before
// applying the operation, so a BigInt left operand with an object right operand runs the
// object's Symbol.toPrimitive (which may throw) ahead of the "BigInts have no unsigned right
// shift" TypeError. Mirrors test262 unsigned-right-shift/bigint-toprimitive.
public class Issue830BigIntUnsignedShiftTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    [Theory]
    // The right operand's toPrimitive throws first → that error propagates, not the BigInt TypeError.
    [InlineData("""
        class MyError extends Error {}
        const obj = { [Symbol.toPrimitive]() { throw new MyError(); } };
        try { 0n >>> obj; "no-throw"; } catch (e) { e instanceof MyError ? "MyError" : e.constructor.name; }
        """, "MyError")]
    // Symmetric case (object on the left) already coerced the left operand first.
    [InlineData("""
        class MyError extends Error {}
        const obj = { [Symbol.toPrimitive]() { throw new MyError(); } };
        try { obj >>> 0n; "no-throw"; } catch (e) { e instanceof MyError ? "MyError" : e.constructor.name; }
        """, "MyError")]
    // With a coercible right operand, ">>>" on a BigInt is still a TypeError.
    [InlineData("""try { 0n >>> 2n; "no-throw"; } catch (e) { e.constructor.name; }""", "TypeError")]
    [InlineData("""try { 0n >>> 2; "no-throw"; } catch (e) { e.constructor.name; }""", "TypeError")]
    public void UnsignedShiftCoercesBeforeThrowing(string source, string expected)
        => Assert.Equal(expected, Eval(source));
}
