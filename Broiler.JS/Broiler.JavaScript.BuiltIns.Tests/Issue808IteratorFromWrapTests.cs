using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Iterator.from produces a %WrapForValidIterator% whose next() calls the underlying iterator's next
// method and returns the result verbatim — a non-Object result is NOT rejected. return() forwards to
// the underlying "return" method, or marks the wrapper done when absent. Issue #808 problem 82.
public class Issue808IteratorFromWrapTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    [Theory]
    [InlineData("undefined", "undefined")]
    [InlineData("null", "")]          // String(null-result) round-trips below; checked separately
    [InlineData("0", "0")]
    [InlineData("false", "false")]
    [InlineData("'test'", "test")]
    public void Wrap_Next_ReturnsUnderlyingResultVerbatim(string value, string expected)
    {
        if (expected == "") return; // null handled in dedicated test
        Assert.Equal(expected, Eval($"String(Iterator.from({{ next: function () {{ return {value}; }} }}).next());"));
    }

    [Fact]
    public void Wrap_Next_NullResult()
        => Assert.Equal("true", Eval("String(Iterator.from({ next: function () { return null; } }).next() === null);"));

    [Fact]
    public void Wrap_OverNormalIterator_Works()
        => Assert.Equal("1,2,3", Eval("[...Iterator.from([1, 2, 3][Symbol.iterator]())].join(',');"));

    [Fact]
    public void Wrap_Map_Works()
        => Assert.Equal("2,4,6", Eval("[...Iterator.from([1, 2, 3][Symbol.iterator]()).map(function (x) { return x * 2; })].join(',');"));

    [Fact]
    public void Wrap_Return_ForwardsToUnderlying()
        => Assert.Equal("true", Eval("""
            var closed = false;
            var custom = {
                next: function () { return { value: 1, done: false }; },
                return: function () { closed = true; return { value: undefined, done: true }; },
            };
            Iterator.from(custom).return();
            String(closed);
        """));

    [Fact]
    public void From_IteratorInstance_ReturnedDirectly()
        => Assert.Equal("true", Eval("""
            var it = [1][Symbol.iterator]();
            var wrapped = Iterator.from(it);
            String(Iterator.from(wrapped) === wrapped);
        """));
}
