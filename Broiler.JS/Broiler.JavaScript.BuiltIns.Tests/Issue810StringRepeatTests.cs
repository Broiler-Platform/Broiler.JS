using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// String.prototype.repeat follows ToIntegerOrInfinity(count): an empty receiver (or a zero count)
// yields the empty string for any finite count — including the maximum 32-bit integer 2^31 - 1, which
// the old `count == int.MaxValue` sentinel rejected — and only a negative or infinite count throws a
// RangeError. Issue #810 problem 76.
public class Issue810StringRepeatTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    [Theory]
    [InlineData("''.repeat(1)", "")]
    [InlineData("''.repeat(3)", "")]
    [InlineData("''.repeat(2147483647)", "")]   // maxSafe32bitInt: previously threw "Invalid count value"
    [InlineData("'abc'.repeat(0)", "")]
    [InlineData("'abc'.repeat()", "")]          // undefined count -> 0
    [InlineData("'abc'.repeat(NaN)", "")]
    [InlineData("'ab'.repeat(3)", "ababab")]
    [InlineData("'x'.repeat(2.9)", "xx")]       // truncates toward zero
    public void Repeat(string expr, string expected)
        => Assert.Equal(expected, Eval($"String({expr});"));

    [Theory]
    [InlineData("'a'.repeat(-1)")]
    [InlineData("'a'.repeat(Infinity)")]
    [InlineData("''.repeat(-1)")]               // empty receiver does not excuse a negative count
    public void Repeat_ThrowsRangeError(string expr)
        => Assert.Equal("RangeError", Eval("try { " + expr + "; 'no throw'; } catch (e) { e.constructor.name; }"));
}
