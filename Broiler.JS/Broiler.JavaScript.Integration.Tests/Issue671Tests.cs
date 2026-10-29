using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/671
//
// Fixed here:
//   * Problem 1  (partial) — JSON.stringify dropped a plain object's
//                  integer-indexed own properties. The object serializer
//                  enumerated only the named-property storage
//                  (GetOwnProperties()) and never the separate element storage,
//                  so `JSON.stringify({0:'a', x:1})` produced `{"x":1}`. The
//                  serializer now also walks the integer-indexed elements, in
//                  ascending index order ahead of the string keys, matching
//                  OrdinaryOwnPropertyKeys / EnumerableOwnPropertyNames order.
//   * Problem 10 (partial) — `super.prop` inside a class static initialization
//                  block of a derived class threw "Cannot access 'this' before
//                  initialization". A static block always runs with `this` bound
//                  to the already-constructed class constructor (passed as the
//                  receiver); unlike a derived constructor there is no super()
//                  call that initializes `this`, so it is never in the temporal
//                  dead zone. The block was being compiled with
//                  thisIsUninitialized = hasSuperClass, which put `this` in the
//                  TDZ; it is now always initialized for static blocks.
//
// Out of scope (triaged in the issue):
//   * Problem 1 cases driven by the SpiderMonkey deepEqual harness, Problem 2
//     (IteratorClose count for a generator `return` completion routed through a
//     `yield` inside a destructuring rest target), Problem 3 (Intl
//     DateTimeFormat formatRange — needs CLDR data), Problem 4 (an abrupt
//     completion — `continue`/`break` — in a `finally` must override a pending
//     throw from the `try`; an IL try/finally lowering change), Problem 5
//     (compound-assignment PutValue must reuse the reference captured before the
//     RHS, even when a direct `eval` introduces a closer `var` binding), Problem
//     6 (several unrelated root causes grouped by message), Problems 7-9 (Unicode
//     15.1/16/17 ID_Start coverage — a data update in the Broiler.Unicode
//     submodule), and the Problem 10 eval-super cases (a direct `eval("super()")`
//     must initialize the enclosing derived constructor's `this` binding).
public class Issue671Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // ---- Problem 10: super.prop in a derived class's static block ----

    [Fact]
    public void StaticBlockSuperPropertyResolvesAgainstParentConstructor()
        => Assert.Equal("test262", Eval(
            "function Parent() {} Parent.test262 = 'test262';"
            + " var value; class C extends Parent { static { value = super.test262; } } value"));

    [Fact]
    public void StaticBlockCanReadAndWriteThisInDerivedClass()
        => Assert.Equal("pk/by", Eval(
            "class P {} P.k = 'pk';"
            + " class B extends P { static { this.y = 'by'; } static read() { return super.k + '/' + this.y; } }"
            + " B.read()"));

    [Fact]
    public void StaticBlockWithoutSuperclassStillWorks()
        => Assert.Equal("ax", Eval(
            "var out; class A { static x = 'ax'; static { out = A.x; } } out"));

    // ---- Problem 1: JSON.stringify includes integer-indexed own properties ----

    [Fact]
    public void JsonStringifyIncludesIntegerKeysBeforeStringKeys()
        => Assert.Equal("{\"0\":\"z\",\"2\":\"w\",\"a\":1}",
            Eval("JSON.stringify({a:1, 0:'z', 2:'w'})"));

    [Fact]
    public void JsonStringifyIntegerOnlyObject()
        => Assert.Equal("{\"0\":\"a\",\"1\":\"b\"}",
            Eval("JSON.stringify({0:'a', 1:'b'})"));

    [Fact]
    public void JsonStringifyOrdersIntegerKeysNumericallyAscending()
        => Assert.Equal("{\"0\":\"zero\",\"1\":\"one\",\"10\":\"ten\",\"foo\":\"bar\"}",
            Eval("JSON.stringify({1:'one', 0:'zero', foo:'bar', 10:'ten'})"));

    [Fact]
    public void JsonStringifySkipsUndefinedAndNonEnumerableIntegerKeys()
        => Assert.Equal("{\"3\":5,\"n\":null}",
            Eval("JSON.stringify({n:null, u:undefined, 2:undefined, 3:5})"));

    [Fact]
    public void JsonStringifyInvokesGetterOnIntegerKey()
        => Assert.Equal("{\"0\":\"G\",\"k\":1}",
            Eval("var g = {}; Object.defineProperty(g, 0, { get: function(){ return 'G'; }, enumerable: true });"
                + " g.k = 1; JSON.stringify(g)"));

    [Fact]
    public void JsonStringifyOmitsNonEnumerableIntegerKey()
        => Assert.Equal("{\"v\":2}",
            Eval("var o = {}; Object.defineProperty(o, 0, { value: 'hidden', enumerable: false });"
                + " o.v = 2; JSON.stringify(o)"));

    [Fact]
    public void JsonStringifyArraysStillWork()
        => Assert.Equal("[10,20]", Eval("JSON.stringify([10, 20])"));
}
