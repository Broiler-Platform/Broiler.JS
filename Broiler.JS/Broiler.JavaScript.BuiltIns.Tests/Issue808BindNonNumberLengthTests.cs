using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Function.prototype.bind / SetFunctionLength: the bound function's "length" is derived from the
// target's "length" only when that value is a Number; a non-Number (Symbol, String, …) is ignored and
// the length defaults to 0. Reading the target length must not attempt a ToNumber coercion that would
// throw (e.g. for a Symbol). Issue #808 problem 72.
public class Issue808BindNonNumberLengthTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    [Theory]
    [InlineData("Symbol()", "0")]
    [InlineData("'15'", "0")]
    [InlineData("{}", "0")]
    [InlineData("5", "5")]
    public void Bind_Length_OnlyUsesNumberTargetLength(string lengthValue, string expected)
    {
        var actual = Eval($$"""
            var fn = function () {};
            Object.defineProperty(fn, "length", { value: {{lengthValue}} });
            var out = "none";
            try { out = String(fn.bind().length); }
            catch (e) { out = e.constructor.name + ": " + e.message; }
            out;
        """);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Bind_Length_SubtractsBoundArguments()
        => Assert.Equal("2", Eval("(function (a, b, c) {}).bind(null, 1).length;"));

    [Fact]
    public void Bind_Length_NeverNegative()
        => Assert.Equal("0", Eval("(function (a) {}).bind(null, 1, 2, 3).length;"));

    [Fact]
    public void Bind_Length_InfinitePreserved()
        => Assert.Equal("Infinity", Eval("""
            var fn = function () {};
            Object.defineProperty(fn, "length", { value: Infinity });
            String(fn.bind().length);
        """));

    [Fact]
    public void Bind_Length_InheritedLengthIgnored()
        // After deleting the own "length", the inherited one (42) must not be used: HasOwnProperty is
        // false, so the bound length defaults to 0.
        => Assert.Equal("0", Eval("""
            function bar(a, b) {}
            Object.setPrototypeOf(bar, { length: 42 });
            delete bar.length;
            String(Function.prototype.bind.call(bar, null, 1).length);
        """));
}
