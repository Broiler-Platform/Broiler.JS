using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// The ArrayBuffer / SharedArrayBuffer length argument is converted with ToIndex: ToIntegerOrInfinity
// truncates toward zero FIRST, so a fractional value in (-1, 0) becomes 0 rather than throwing; only
// the resulting integer is then range-checked (negative or > 2^53-1 is a RangeError). Issue #808
// problem 40.
public class Issue808ArrayBufferToIndexTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    [Theory]
    [InlineData("-0.5", "0")]
    [InlineData("-0.9", "0")]
    [InlineData("0.5", "0")]
    [InlineData("2.9", "2")]
    [InlineData("-0", "0")]
    [InlineData("4", "4")]
    [InlineData("NaN", "0")]
    [InlineData("undefined", "0")]
    [InlineData("{ valueOf: function () { return 3.7; } }", "3")]
    [InlineData("-1", "RangeError")]
    [InlineData("-1.5", "RangeError")]
    [InlineData("Math.pow(2, 53)", "RangeError")]
    [InlineData("Infinity", "RangeError")]
    public void ArrayBuffer_Length_ToIndex(string arg, string expected)
        => Assert.Equal(expected, Eval($$"""
            var out = "none";
            try { out = String(new ArrayBuffer({{arg}}).byteLength); }
            catch (e) { out = e.constructor.name; }
            out;
        """));

    [Theory]
    [InlineData("-0.5", "0")]
    [InlineData("4", "4")]
    [InlineData("-1", "RangeError")]
    public void SharedArrayBuffer_Length_ToIndex(string arg, string expected)
        => Assert.Equal(expected, Eval($$"""
            var out = "none";
            try { out = String(new SharedArrayBuffer({{arg}}).byteLength); }
            catch (e) { out = e.constructor.name; }
            out;
        """));
}
