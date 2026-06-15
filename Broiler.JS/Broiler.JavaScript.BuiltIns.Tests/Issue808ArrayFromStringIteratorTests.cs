using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Array.from looks up @@iterator on its argument (a String resolves String.prototype[@@iterator] via its
// wrapper) and iterates by code point; if that method is removed it falls back to the array-like path,
// iterating UTF-16 code units. A custom @@iterator is honoured. Issue #808 problem 84.
public class Issue808ArrayFromStringIteratorTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    // Each case runs in its own context because it mutates String.prototype[@@iterator].
    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    [Fact]
    public void From_String_IteratesByCodePoint()
        => Assert.Equal("a,b,c", Eval("Array.from('abc').join(',');"));

    [Fact]
    public void From_String_SurrogatePairIsOneCodePoint()
        => Assert.Equal("1", Eval("String(Array.from('\\u{1D11E}').length);"));

    [Fact]
    public void From_String_CustomIterator_IsHonoured()
        => Assert.Equal("X,Y", Eval("""
            String.prototype[Symbol.iterator] = function () { return ["X", "Y"][Symbol.iterator](); };
            Array.from("zz").join(",");
        """));

    [Fact]
    public void From_String_DeletedIterator_FallsBackToCodeUnits()
        => Assert.Equal("a,b,c", Eval("""
            delete String.prototype[Symbol.iterator];
            Array.from("abc").join(",");
        """));

    [Fact]
    public void From_String_DeletedIterator_SurrogateSplitsIntoTwoCodeUnits()
        => Assert.Equal("2", Eval("""
            delete String.prototype[Symbol.iterator];
            String(Array.from("\u{1D11E}").length);
        """));
}
