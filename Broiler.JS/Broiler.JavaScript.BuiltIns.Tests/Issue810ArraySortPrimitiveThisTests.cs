using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Array.prototype.sort performs ToObject(this value) per §23.1.3.30: a primitive receiver is boxed into
// its wrapper object (and returned), while null/undefined throw a TypeError, instead of unconditionally
// throwing for every non-object receiver. Issue #810 problem 89.
public class Issue810ArraySortPrimitiveThisTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    [Theory]
    [InlineData("[].sort.call(false) instanceof Boolean", "true")]
    [InlineData("[].sort.call(0) instanceof Number", "true")]
    [InlineData("[].sort.call('') instanceof String", "true")]
    public void Sort_BoxesPrimitiveReceiver(string expr, string expected)
        => Assert.Equal(expected, Eval($"String({expr});"));

    [Theory]
    [InlineData("[].sort.call(undefined)")]
    [InlineData("[].sort.call(null)")]
    public void Sort_NullOrUndefined_ThrowsTypeError(string expr)
        => Assert.Equal("TypeError", Eval("try { " + expr + "; 'no throw'; } catch (e) { e.constructor.name; }"));

    [Fact]
    public void Sort_NonCallableComparator_ThrowsTypeError()
        => Assert.Equal("TypeError", Eval("""
            try { [3, 1, 2].sort(42); "no throw"; } catch (e) { e.constructor.name; }
        """));
}
