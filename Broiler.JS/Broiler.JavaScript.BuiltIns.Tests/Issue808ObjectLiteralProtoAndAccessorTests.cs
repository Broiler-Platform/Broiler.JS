using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Two fixes for issue #808 problem 3 (staging/sm/expressions/object-literal-__proto__.js):
//  - `__proto__: value` in an object literal mutates [[Prototype]] directly (B.3.1), independent of a
//    same-named own "__proto__" property defined with a computed key / accessor / method.
//  - Defining an accessor (get/set) for a key that already has a DATA property in the same literal
//    replaces it with a clean accessor; the discarded data value must not leak into the get/set slot.
public class Issue808ObjectLiteralProtoAndAccessorTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    [Fact]
    public void ProtoColon_AfterComputedProto_StillMutatesPrototype()
        => Assert.Equal("null|true", Eval("""
            var o = { ["__proto__"]: null, __proto__: null };
            (Object.getPrototypeOf(o) === null ? "null" : "other") + "|" +
                (Object.getOwnPropertyDescriptor(o, "__proto__") !== undefined);
        """));

    [Fact]
    public void ProtoColon_PrimitiveValueIgnored()
        => Assert.Equal("true|0", Eval("""
            var o = { __proto__: 5 };
            (Object.getPrototypeOf(o) === Object.prototype) + "|" + Object.getOwnPropertyNames(o).length;
        """));

    [Fact]
    public void DataThenSetter_ProducesCleanAccessor()
        => Assert.Equal("undefined|function", Eval("""
            var o = { ["x"]: null, set x(v) {} };
            var d = Object.getOwnPropertyDescriptor(o, "x");
            typeof d.get + "|" + typeof d.set;
        """));

    [Fact]
    public void DataThenGetter_ProducesCleanAccessor()
        => Assert.Equal("function|undefined|undefined", Eval("""
            var o = { x: 5, get x() { return 1; } };
            var d = Object.getOwnPropertyDescriptor(o, "x");
            typeof d.get + "|" + typeof d.set + "|" + d.value;
        """));

    [Fact]
    public void GetterThenSetter_MergesBothAccessors()
        => Assert.Equal("function|function", Eval("""
            var o = { get x() { return 1; }, set x(v) {} };
            var d = Object.getOwnPropertyDescriptor(o, "x");
            typeof d.get + "|" + typeof d.set;
        """));

    [Fact]
    public void DuplicateProtoColon_IsSyntaxError()
        => Assert.Equal("SyntaxError", Eval("""
            var err = "none";
            try { Function("return { __proto__: null, __proto__: Function.prototype }"); }
            catch (e) { err = e.constructor.name; }
            err;
        """));
}
