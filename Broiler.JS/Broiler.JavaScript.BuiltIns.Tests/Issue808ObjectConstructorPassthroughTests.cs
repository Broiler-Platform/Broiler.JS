using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// `new Object(value)` / `Object(value)` performs ToObject: an existing object is returned unchanged
// (same identity AND same prototype). The construct path must not force %Object.prototype% onto the
// returned object, which previously corrupted e.g. a passed-in Date's prototype so its methods became
// unavailable. Boxed primitives keep their type-specific prototype; subclassing and the native exotic
// constructors are unaffected. Issue #808 problem 87.
public class Issue808ObjectConstructorPassthroughTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    [Fact]
    public void NewObject_OfDate_PreservesPrototypeAndMethods()
        => Assert.Equal("true|true|0", Eval("""
            var d = new Date(0);
            var o = new Object(d);
            [o === d, Object.getPrototypeOf(o) === Date.prototype, o.getFullYear() - d.getFullYear()].join("|");
        """));

    [Fact]
    public void NewObject_OfArray_StaysArray()
        => Assert.Equal("true|true", Eval("""
            var a = [1, 2, 3];
            var o = new Object(a);
            [o === a, Array.isArray(o)].join("|");
        """));

    [Fact]
    public void NewObject_OfPrimitive_BoxesWithTypePrototype()
        => Assert.Equal("true|true|true", Eval("""
            [new Object(1) instanceof Number,
             new Object(1).constructor === Number,
             new Object("s") instanceof String].join("|");
        """));

    [Fact]
    public void NativeConstructors_StillGetCorrectPrototype()
        => Assert.Equal("true|true|true|true", Eval("""
            [Object.getPrototypeOf(new Date()) === Date.prototype,
             Object.getPrototypeOf(new Array(3)) === Array.prototype,
             Object.getPrototypeOf(new Map()) === Map.prototype,
             Object.getPrototypeOf(new RegExp("a")) === RegExp.prototype].join("|");
        """));

    [Fact]
    public void Subclassing_StillAppliesSubclassPrototype()
        => Assert.Equal("true|true|true", Eval("""
            class MyDate extends Date {}
            class MyArr extends Array {}
            var md = new MyDate(0);
            var ma = new MyArr(1, 2, 3);
            [md instanceof MyDate, md instanceof Date, (ma instanceof MyArr) && ma.length === 3].join("|");
        """));
}
