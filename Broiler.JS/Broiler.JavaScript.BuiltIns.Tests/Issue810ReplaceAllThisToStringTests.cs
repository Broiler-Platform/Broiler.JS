using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// String.prototype.replaceAll coerces its `this` value with the spec ToString abstract operation
// (ToPrimitive with a string hint, honouring Symbol.toPrimitive) rather than the receiver's own
// toString/valueOf directly. Issue #810 problem 92.
public class Issue810ReplaceAllThisToStringTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    [Fact]
    public void ToPrimitive_IsUsedBeforeToStringAndValueOf()
        => Assert.Equal("zz|1", Eval("""
            var called = 0;
            var thisValue = {
                [Symbol.toPrimitive]() { called += 1; return "aa"; },
                toString() { throw "poison"; },
                valueOf() { throw "poison"; }
            };
            var result = "".replaceAll.call(thisValue, "a", "z");
            result + "|" + called;
        """));

    [Fact]
    public void FallsBackToToString_WhenNoToPrimitive()
        => Assert.Equal("zz|1", Eval("""
            var called = 0;
            var thisValue = {
                [Symbol.toPrimitive]: undefined,
                toString() { called += 1; return "aa"; },
                valueOf() { throw "poison"; }
            };
            var result = "".replaceAll.call(thisValue, "a", "z");
            result + "|" + called;
        """));

    [Theory]
    [InlineData("4244", "'4'", "'z'", "z2zz")]
    [InlineData("true", "'ru'", "'o m'", "to me")]
    [InlineData("false", "'al'", "'on'", "fonse")]
    public void PrimitiveReceivers(string receiver, string search, string replace, string expected)
        => Assert.Equal(expected, Eval($"\"\".replaceAll.call({receiver}, {search}, {replace});"));
}
