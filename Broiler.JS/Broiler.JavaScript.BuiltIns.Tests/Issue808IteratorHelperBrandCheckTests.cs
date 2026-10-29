using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// %IteratorHelperPrototype%.next / return require the Iterator Helper brand: calling them with a
// receiver that is not an iterator helper (e.g. a generator, which exposes its own next/return) is a
// TypeError. Issue #808 problem 5 (iterator-helper-methods-throw-on-generators).
public class Issue808IteratorHelperBrandCheckTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    [Theory]
    [InlineData("next")]
    [InlineData("return")]
    public void HelperMethod_OnGenerator_ThrowsTypeError(string method)
        => Assert.Equal("TypeError", Eval($$"""
            var helperProto = Object.getPrototypeOf([].values().map(function (x) { return x; }));
            function* gen() { yield 1; }
            var err = "none";
            try { helperProto.{{method}}.call(gen()); }
            catch (e) { err = e.constructor.name; }
            err;
        """));

    [Fact]
    public void Generator_OwnNext_StillWorks()
        => Assert.Equal("1", Eval("function* gen() { yield 1; } String(gen().next().value);"));

    [Fact]
    public void HelperMethod_OnRealHelper_StillWorks()
        => Assert.Equal("2,4,6", Eval("[...[1, 2, 3].values().map(function (x) { return x * 2; })].join(',');"));

    [Fact]
    public void WrapIterator_Next_StillWorks()
        => Assert.Equal("5", Eval("String(Iterator.from([5][Symbol.iterator]()).next().value);"));
}
