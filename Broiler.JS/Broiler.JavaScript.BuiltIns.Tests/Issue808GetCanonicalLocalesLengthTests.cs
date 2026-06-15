using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// CanonicalizeLocaleList reads the array-like "length" with ToLength: ToNumber (a Symbol length is a
// TypeError), truncate toward zero, then clamp a negative result to 0 — so a negative length yields an
// empty list and the indexed getters are never read. Issue #808 problem 99
// (intl402/Intl/getCanonicalLocales/overriden-arg-length.js).
public class Issue808GetCanonicalLocalesLengthTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    [Fact]
    public void NegativeLength_ReturnsEmpty_AndDoesNotReadIndex()
        => Assert.Equal("[]", Eval("""
            var l = { length: -Math.pow(2, 32) + 1 };
            Object.defineProperty(l, "0", { get: function () { throw new Error("must not be gotten!"); } });
            JSON.stringify(Intl.getCanonicalLocales(l));
        """));

    [Fact]
    public void NegativeInfinityLength_ReturnsEmpty()
        => Assert.Equal("[]", Eval("""
            var l = { "0": "en-US" };
            Object.defineProperty(l, "length", { get: function () { return -Infinity; } });
            JSON.stringify(Intl.getCanonicalLocales(l));
        """));

    [Fact]
    public void SymbolLength_ThrowsTypeError()
        => Assert.Equal("TypeError", Eval("""
            var l = { "0": "en-US" };
            Object.defineProperty(l, "length", { get: function () { return Symbol(); } });
            var err = "none";
            try { Intl.getCanonicalLocales(l); }
            catch (e) { err = e.constructor.name; }
            err;
        """));

    [Theory]
    [InlineData("'1'", "[\"en-US\"]")]
    [InlineData("1.3", "[\"en-US\"]")]
    [InlineData("2", "[\"en-US\",\"pt-BR\"]")]
    public void FractionalAndStringLength_Truncates(string length, string expected)
        => Assert.Equal(expected, Eval($$"""
            var l = { "0": "en-US", "1": "pt-BR" };
            Object.defineProperty(l, "length", { get: function () { return {{length}}; } });
            JSON.stringify(Intl.getCanonicalLocales(l));
        """));

    [Fact]
    public void LengthGetter_ReadOnce()
        => Assert.Equal("[]|1", Eval("""
            var count = 0;
            var locs = { get length() { if (count++ > 0) throw 42; return 0; } };
            JSON.stringify(Intl.getCanonicalLocales(locs)) + "|" + count;
        """));
}
