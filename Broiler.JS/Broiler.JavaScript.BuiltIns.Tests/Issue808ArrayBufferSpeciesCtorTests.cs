using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// SpeciesConstructor: ArrayBuffer.prototype.slice / SharedArrayBuffer.prototype.slice fall back to the
// default constructor only when the "constructor" property is undefined; any other non-object value
// (null, a number, …) is a TypeError. A non-null/undefined @@species that is not a constructor is also
// a TypeError, while a null/undefined @@species falls back to the default. Issue #808 problem 39.
public class Issue808ArrayBufferSpeciesCtorTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    [Theory]
    [InlineData("ArrayBuffer", "null", "TypeError")]
    [InlineData("ArrayBuffer", "123", "TypeError")]
    [InlineData("ArrayBuffer", "undefined", "4")]                       // default constructor
    [InlineData("ArrayBuffer", "{ [Symbol.species]: null }", "4")]      // null species -> default
    [InlineData("SharedArrayBuffer", "null", "TypeError")]
    [InlineData("SharedArrayBuffer", "123", "TypeError")]
    [InlineData("SharedArrayBuffer", "undefined", "4")]
    public void Slice_ConstructorProperty(string ctor, string value, string expected)
        => Assert.Equal(expected, Eval($$"""
            var ab = new {{ctor}}(8);
            ab.constructor = {{value}};
            var out = "none";
            try { out = String(ab.slice(0, 4).byteLength); }
            catch (e) { out = e.constructor.name; }
            out;
        """));

    [Fact]
    public void Slice_NoOverride_UsesDefault()
        => Assert.Equal("4", Eval("String(new ArrayBuffer(8).slice(0, 4).byteLength);"));
}
