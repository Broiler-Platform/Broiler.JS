using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// %TypedArray%.prototype.lastIndexOf computes the start index from fromIndex: a negative fromIndex is
// added to the length, and if the result is still < 0 there is nothing to search, so it returns -1
// rather than wrapping the negative index. Issue #808 problem 27.
public class Issue808TypedArrayLastIndexOfTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    [Theory]
    [InlineData("new Int32Array([1,2,3,4,5]).lastIndexOf(1, -100)", "-1")] // fromIndex < -length
    [InlineData("new Int32Array([1,2,3,4,5]).lastIndexOf(1, -5)", "0")]    // exactly -length
    [InlineData("new Int32Array([1,2,1,2,1]).lastIndexOf(1, -2)", "2")]
    [InlineData("new Int32Array([10,20,10]).lastIndexOf(10)", "2")]        // absent fromIndex -> from end
    [InlineData("new Int32Array([10,20,10]).lastIndexOf(10, undefined)", "0")] // undefined -> 0
    [InlineData("new Int32Array([1,2,3,4,5]).lastIndexOf(3, 100)", "2")]   // fromIndex > length
    [InlineData("new Int32Array([1,2,3,4,5]).lastIndexOf(0)", "-1")]
    public void LastIndexOf_FromIndex(string expr, string expected)
        => Assert.Equal(expected, Eval($"String({expr});"));

    [Theory]
    [InlineData("new Int32Array([1,2,3,4,5]).indexOf(1, -100)", "0")]      // indexOf clamps to 0
    [InlineData("new Int32Array([1,2,3,4,5]).indexOf(3, 100)", "-1")]
    public void IndexOf_FromIndex_Unaffected(string expr, string expected)
        => Assert.Equal(expected, Eval($"String({expr});"));
}
