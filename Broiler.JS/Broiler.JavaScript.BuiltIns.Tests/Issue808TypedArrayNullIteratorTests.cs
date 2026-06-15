using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// A TypedArray constructed from an object whose @@iterator is null (or undefined) treats the source as
// an array-like — reading "length" and indexed elements — rather than throwing because @@iterator is
// not callable. GetMethod treats a null method the same as an absent one. Issue #808 problem 77.
public class Issue808TypedArrayNullIteratorTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    [Fact]
    public void Constructor_NullIterator_TreatedAsArrayLike()
        => Assert.Equal("2|1|2", Eval("""
            var o = { length: 2, 0: 1, 1: 2 };
            o[Symbol.iterator] = null;
            var ta = new Float64Array(o);
            ta.length + "|" + ta[0] + "|" + ta[1];
        """));

    [Fact]
    public void Constructor_AbsentIterator_TreatedAsArrayLike()
        => Assert.Equal("5,6,7", Eval("""
            var o = { length: 3, 0: 5, 1: 6, 2: 7 };
            Array.from(new Int32Array(o)).join(",");
        """));

    [Fact]
    public void From_NullIterator_TreatedAsArrayLike()
        => Assert.Equal("1,2", Eval("""
            var o = { length: 2, 0: 1, 1: 2 };
            o[Symbol.iterator] = null;
            Array.from(Float64Array.from(o)).join(",");
        """));

    [Fact]
    public void Constructor_IterableObject_StillIterates()
        => Assert.Equal("0,1", Eval("""
            var it = { [Symbol.iterator]() { var i = 0; return { next() {
                return i < 2 ? { value: i++, done: false } : { value: undefined, done: true }; } }; } };
            Array.from(new Float64Array(it)).join(",");
        """));

    [Fact]
    public void Constructor_FromArray_StillWorks()
        => Assert.Equal("10,20,30", Eval("Array.from(new Float64Array([10, 20, 30])).join(',');"));
}
