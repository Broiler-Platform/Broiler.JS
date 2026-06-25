using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/912 (Problem 1).
//
// A class expression with a COMPUTED accessor/method/field name (`class { get [k]() {} }`)
// threw System.InvalidProgramException whenever the class expression was evaluated in
// value position (as a call argument, a for-init, ...). The computed key was always
// wrapped in a runtime strict-mode try/finally (#867), and the custom IL generator has
// no stack-spiller, so entering that try region while the surrounding evaluation stack
// was non-empty produced invalid IL. The wrap is now emitted only when the key actually
// contains an inline store whose throw is gated by the live strict flag.
public class Issue912Tests
{
    private static JSValue Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code);
    }

    // test/built-ins/Function/prototype/toString/getter-class-expression.js — three
    // getter class expressions inline as Object.getOwnPropertyDescriptor arguments,
    // including computed string and identifier names.
    [Fact]
    public void GetterClassExpressionInline()
    {
        var code =
            "let x = \"h\";\n" +
            "let f = Object.getOwnPropertyDescriptor(class { get f() {} }.prototype, \"f\").get;\n" +
            "let g = Object.getOwnPropertyDescriptor(class { get [\"g\"]() {} }.prototype, \"g\").get;\n" +
            "let h = Object.getOwnPropertyDescriptor(class { get [x]() {} }.prototype, \"h\").get;\n" +
            "'' + (typeof f) + (typeof g) + (typeof h);";
        Assert.Equal("functionfunctionfunction", Eval(code).ToString());
    }

    [Fact]
    public void SetterClassExpressionInline()
    {
        var code = "'' + typeof Object.getOwnPropertyDescriptor(class { set [\"f\"](v) {} }.prototype, \"f\").set;";
        Assert.Equal("function", Eval(code).ToString());
    }

    // test/language/expressions/class/accessor-name-inst-computed-in.js — a computed
    // accessor name using `in`, with the class expression in a for-statement init.
    [Fact]
    public void AccessorNameComputedInForInit()
    {
        var code =
            "var empty = Object.create(null);\n" +
            "var C, value;\n" +
            "for (C = class { get ['x' in empty]() { return 'via get'; } }; ; ) {\n" +
            "  value = C.prototype.false;\n" +
            "  break;\n" +
            "}\n" +
            "'' + value;";
        Assert.Equal("via get", Eval(code).ToString());
    }

    // Computed method and static computed accessor in value position also compile.
    [Theory]
    [InlineData("'' + typeof Object.getOwnPropertyDescriptor(class { static get ['g']() { return 1; } }, 'g').get;")]
    [InlineData("'' + typeof (class { ['m']() { return 1; } }).prototype.m;")]
    public void ComputedMemberInValuePosition(string code)
    {
        Assert.Equal("function", Eval(code).ToString());
    }

    // #867 regression guard: a computed key with an inline member store to a
    // non-extensible object must STILL throw TypeError (the runtime strict-mode scope
    // is kept whenever the key contains an inline store).
    [Fact]
    public void ComputedKeyStoreToFrozenStillThrows()
    {
        var code = "var o = Object.preventExtensions({});"
            + "var threw = 'no';"
            + "try { var C = class { [o.x = 1]() {} }; } catch (e) { threw = e.constructor.name; }"
            + "threw;";
        Assert.Equal("TypeError", Eval(code).ToString());
    }

    // Problem 14: SerializeJSONProperty consults toJSON for a BigInt value (the BigInt
    // proposal). When that toJSON ends in `return f()` (a tail call), the script host
    // returns a JSTailCall sentinel from the native Delegate call; JSON.stringify must
    // trampoline it to its value instead of serializing the sentinel object as "{}".
    [Fact]
    public void BigIntToJsonTailCallResolved()
    {
        var code = "BigInt.prototype.toJSON = function () { return this.toString(); };"
            + "JSON.stringify(0n);";
        Assert.Equal("\"0\"", Eval(code).ToString());
    }

    // The same trampoline for an ordinary object's toJSON ending in a tail call.
    [Fact]
    public void ObjectToJsonTailCallResolved()
    {
        var code = "var o = { toJSON() { return (function () { return 7; })(); } };"
            + "JSON.stringify(o);";
        Assert.Equal("7", Eval(code).ToString());
    }
}
