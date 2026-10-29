using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Array.prototype.push on an array already at the maximum length (2^32 - 1) no longer wraps the new
// length through the int-typed fast `Length` setter: pushing nothing returns 2^32 - 1, and pushing an
// element that would exceed the cap throws a RangeError. Issue #810 problem 87.
public class Issue810ArrayPushMaxLengthTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    [Fact]
    public void Push_NoArguments_AtMaxLength_ReturnsLength()
        => Assert.Equal("4294967295", Eval("""
            var x = [];
            x.length = 4294967295;
            String(x.push());
        """));

    [Fact]
    public void Push_OverflowingMaxLength_ThrowsRangeError()
        => Assert.Equal("RangeError", Eval("""
            var x = [];
            x.length = 4294967295;
            try { x.push("x"); "no throw"; } catch (e) { e.constructor.name; }
        """));

    [Fact]
    public void Push_PreservesLength_WhenNoArguments()
        => Assert.Equal("4294967295", Eval("""
            var x = [];
            x.length = 4294967295;
            x.push();
            String(x.length);
        """));
}
