using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/818 — Problem 1
// (test/staging/sm/Symbol/as-base-value.js): Test262Error: Expected SameValue(«0», «1»).
//
// Assigning a property to a primitive base value (`sym.prop = v`, `(5)[k] = v`) did
// not invoke an inherited accessor's setter — the write was silently dropped. The
// primitive indexer setters threw/no-op'd directly instead of running OrdinarySet.
// They now route through SetValue, which walks the wrapper prototype chain and, when
// the resolved property is an accessor with a setter, invokes it with the primitive
// as the receiver. A data property (or no property) still cannot be created on a
// primitive, so those remain a no-op (non-strict) / TypeError (strict).
public class Issue818PrimitiveSetterTests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    [Fact]
    public void SymbolInheritedSetterIsInvokedWithPrimitiveReceiver()
        => Assert.Equal("1,nv,symbol,true", Eval(
            "var sym = Symbol('s'); var sets = 0, seen, thisType, thisIsPrim;" +
            "Object.defineProperty(Symbol.prototype, 'q', { set: function (v) {" +
            "  'use strict'; sets++; seen = v; thisType = typeof this; thisIsPrim = this === sym; } });" +
            "sym.q = 'nv';" +
            "sets + ',' + seen + ',' + thisType + ',' + thisIsPrim"));

    [Fact]
    public void NumberInheritedSetterIsInvoked()
        => Assert.Equal("1", Eval(
            "var n = 0; Object.defineProperty(Number.prototype, 'q', { set: function () { n++; } });" +
            "(5).q = 'x'; '' + n"));

    [Fact]
    public void StringInheritedSetterIsInvokedViaComputedKey()
        => Assert.Equal("1", Eval(
            "var n = 0; Object.defineProperty(String.prototype, 'q', { set: function () { n++; } });" +
            "var k = 'q'; 'abc'[k] = 'x'; '' + n"));

    [Fact]
    public void SymbolKeyedInheritedSetterIsInvoked()
        => Assert.Equal("1", Eval(
            "var sk = Symbol('k'); var n = 0;" +
            "Object.defineProperty(Symbol.prototype, sk, { set: function () { n++; } });" +
            "var s = Symbol(); s[sk] = 1; '' + n"));

    [Fact]
    public void AssignmentExpressionReturnsTheAssignedValue()
        => Assert.Equal("nv", Eval(
            "Object.defineProperty(Symbol.prototype, 'q', { set: function () {} });" +
            "var s = Symbol(); (s.q = 'nv')"));

    // A data property on the prototype cannot be created on the primitive receiver:
    // the assignment is a no-op and must not pollute the shared wrapper prototype.
    [Fact]
    public void AssigningOverAnInheritedDataPropertyDoesNotPolluteThePrototype()
        => Assert.Equal("function,undefined", Eval(
            "(5).toFixed = 1;" +
            "var s = Symbol(); s.zzz = 2;" +
            "typeof Number.prototype.toFixed + ',' + typeof s.zzz"));
}
