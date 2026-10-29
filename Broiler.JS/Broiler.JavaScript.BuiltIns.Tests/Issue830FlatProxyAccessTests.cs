using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Issue #830 (problems 64, 96): Array.prototype.flat/flatMap read "length" before
// "constructor" (ArraySpeciesCreate) and observe their source through HasProperty + Get,
// so a Proxy/array-like source's traps fire for each index. Mirrors test262
// Array/prototype/{flat,flatMap}/proxy-access-count.
public class Issue830FlatProxyAccessTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    [Fact]
    public void FlatReadsLengthThenConstructorThenElements()
    {
        var r = Eval("""
            const log = [];
            const target = [1, 2, 3, 4];
            const p = new Proxy(target, {
              get(t, k, r) { if (typeof k === "string") log.push(k); return Reflect.get(t, k, r); }
            });
            Array.prototype.flat.call(p);
            log.join(",");
        """);
        Assert.Equal("length,constructor,0,1,2,3", r);
    }

    [Fact]
    public void FlatMapReadsLengthThenConstructorThenElements()
    {
        var r = Eval("""
            const log = [];
            const target = [1, 2, 3];
            const p = new Proxy(target, {
              get(t, k, r) { if (typeof k === "string") log.push(k); return Reflect.get(t, k, r); }
            });
            Array.prototype.flatMap.call(p, x => x);
            log.join(",");
        """);
        Assert.Equal("length,constructor,0,1,2", r);
    }

    [Theory]
    // The flattening result itself is unchanged.
    [InlineData("JSON.stringify([1, [2, 3], 4].flat())", "[1,2,3,4]")]
    [InlineData("JSON.stringify([1, [2, [3]]].flat(2))", "[1,2,3]")]
    // flat reads inherited indexed properties (HasProperty walks the prototype).
    [InlineData("""
        const a = [0];
        a.length = 2;            // index 1 is a hole on the array itself
        Array.prototype[1] = 9;  // inherited index
        try { JSON.stringify(a.flat()); } finally { delete Array.prototype[1]; }
        """, "[0,9]")]
    public void Behavior(string expr, string expected)
        => Assert.Equal(expected, Eval(expr));
}
