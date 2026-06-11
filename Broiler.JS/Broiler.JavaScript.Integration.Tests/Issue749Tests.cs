using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/749
//
// Fixed here:
//
//   Problem 29 (for-of/for-in into an existing variable) — a bare-identifier loop
//   head that resolved to a known binding was passed as the enumerator MoveNext
//   out-argument (the binding's boxed JSVariable.Value). That write did not persist,
//   so `for (x of …)` / `for (x in …)` / `for ((x) of …)` over an existing var/let
//   left the variable unchanged after the loop. Such heads now go through the same
//   per-iteration assignment path used by member-expression and free-identifier heads.
//
//   Problem 28 (for-in over a string yielded Number keys) — JSString.GetAllKeys
//   returned an IntKeyEnumerator (Number indices). Property-key enumeration must
//   yield String keys, so destructuring a for-in key (`for (var {length:x} in "foo")`)
//   reads `.length` off a string. The string primitive now enumerates "0","1",… keys.
//
//   Problem 16 (switch discriminant scope) — the discriminant Expression is compiled
//   before the CaseBlock's lexical scope is pushed, so a closure in it captures the
//   enclosing binding rather than a block-scoped `let` in the switch body.
//
//   Problem 33 (`this` in a static field arrow) — a static field initializer rebinds
//   `this` to the class constructor via the home-object box (capturable by the deferred
//   arrow), and an arrow scope now binds its `this` to the enclosing `this` expression
//   directly instead of falling back to a GetVariable("this") lookup that skipped the
//   override. So `static f = () => this` captures the constructor.
//
//   Problem 32 (Map/Set forEach callback `this`) — forEach passed its own receiver
//   (the Map/Set) as the callback's `this`. The thisArg is the SECOND argument
//   (default undefined), and the callback is invoked via InvokeCallback so a sloppy
//   callback's undefined `this` coerces to the global object (a strict one stays
//   undefined). forEach.length stays 1.
//
//   Problem 17 (Object.prototype.toString on a raw primitive) — ToObject(this) runs
//   before the builtin tag is computed, so a raw Boolean/Number/String receiver tags
//   as "[object Boolean/Number/String]" (not "[object Object]"); raw Symbol/BigInt tag
//   via @@toStringTag.
public class Issue749Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // ---- Problem 29: for-of/for-in target persists after the loop ----

    [Fact]
    public void ForOfIntoExistingVarPersists()
        => Assert.Equal("9", Eval("var x; for (x of [9]) {} x"));

    [Fact]
    public void ForOfTargetAssignedInBodyPersists()
        => Assert.Equal("99", Eval("var x; for (x of [9]) { x = 99; } x"));

    [Fact]
    public void ForOfMultiElementLeavesLastValue()
        => Assert.Equal("3", Eval("var x; for (x of [1,2,3]) {} x"));

    [Fact]
    public void ForOfParenthesisedIdentifierTarget()
        => Assert.Equal("7", Eval("var a; for ((a) of [7]) {} a"));

    [Fact]
    public void ForInIntoExistingVarPersists()
        => Assert.Equal("b", Eval("var k; for (k in {a:1,b:2}) {} k"));

    [Fact]
    public void ForOfLetHeadStillWorks()
        => Assert.Equal("6", Eval("var s=0; for (let v of [1,2,3]) { s+=v; } s"));

    [Fact]
    public void ForOfMemberTargetStillWorks()
        => Assert.Equal("9", Eval("var o={}; for (o.p of [9]) {} o.p"));

    // ---- Problem 28: for-in over a string yields string keys ----

    [Fact]
    public void ForInStringKeyIsString()
        => Assert.Equal("string", Eval("var t; for (var k in 'foo') { t = typeof k; break; } t"));

    [Fact]
    public void ForInStringKeyDestructuringLength()
        => Assert.Equal("1", Eval("var t; for (var {length:x} in 'foo') { t = x; break; } t"));

    [Fact]
    public void ForInStringComputedKeyDestructuring()
        => Assert.Equal("1,0", Eval(
            "var t; for (var {length:x, [x-1]:y} in 'foo') { t = x + ',' + y; break; } t"));

    [Fact]
    public void ObjectKeysOfStringAreStrings()
        => Assert.Equal("string", Eval("typeof Object.keys('foo')[0]"));

    [Fact]
    public void ArrayKeysIteratorStillNumeric()
        => Assert.Equal("number", Eval("typeof [10,20].keys().next().value"));

    // ---- Problem 16: switch discriminant evaluated in the outer environment ----

    [Fact]
    public void SwitchDiscriminantCapturesOuterLexical()
        => Assert.Equal("outside,inside,inside", Eval(
            "let x = 'outside';" +
            "var probeExpr, probeSelector, probeStmt;" +
            "switch (probeExpr = function() { return x; }, null) {" +
            "  case probeSelector = function() { return x; }, null:" +
            "    probeStmt = function() { return x; };" +
            "    let x = 'inside';" +
            "}" +
            "probeExpr() + ',' + probeSelector() + ',' + probeStmt()"));

    [Fact]
    public void SwitchDiscriminantSeesOuterBindingValue()
        => Assert.Equal("outer", Eval(
            "let v = 'outer'; let r;" +
            "switch (r = v, 0) { case 0: let v = 'inner'; }" +
            "r"));

    // ---- Problem 33: `this` inside an arrow in a static field initializer ----

    [Fact]
    public void StaticFieldArrowCapturesConstructorThis()
        => Assert.Equal("true", Eval(
            "var C = class { static f = () => this; }; (C.f() === C).toString()"));

    [Fact]
    public void StaticFieldDirectThisStillConstructor()
        => Assert.Equal("true", Eval(
            "var C = class { static g = this; }; (C.g === C).toString()"));

    [Fact]
    public void InstanceFieldArrowStillCapturesInstance()
        => Assert.Equal("7", Eval(
            "class A { v = 7; f = () => this.v; } new A().f()"));

    [Fact]
    public void MethodNestedArrowThisUnaffected()
        => Assert.Equal("true", Eval(
            "var o = { m(){ return (()=>(()=>this)())(); } }; (o.m() === o).toString()"));

    // ---- Problem 32: Map/Set forEach callback `this` is the thisArg, not the receiver ----

    [Fact]
    public void MapForEachNoThisArgIsGlobalInSloppy()
        => Assert.Equal("true", Eval(
            "var t=[]; new Map([[1,1]]).forEach(function(){t.push(this);}); (t[0]===globalThis).toString()"));

    [Fact]
    public void MapForEachUsesThisArgWhenSupplied()
        => Assert.Equal("true", Eval(
            "var t=[],o={}; new Map([[1,1]]).forEach(function(){t.push(this);}, o); (t[0]===o).toString()"));

    [Fact]
    public void SetForEachUsesThisArgWhenSupplied()
        => Assert.Equal("true", Eval(
            "var t=[],o={}; new Set([5]).forEach(function(){t.push(this);}, o); (t[0]===o).toString()"));

    [Fact]
    public void MapForEachStrictCallbackThisIsUndefined()
        => Assert.Equal("true", Eval(
            "'use strict'; var t=[]; new Map([[1,1]]).forEach(function(){t.push(this);}); (t[0]===undefined).toString()"));

    [Fact]
    public void MapAndSetForEachLengthIsOne()
        => Assert.Equal("1,1", Eval(
            "Map.prototype.forEach.length + ',' + Set.prototype.forEach.length"));

    // ---- Problem 17: Object.prototype.toString classifies raw primitives ----

    [Fact]
    public void ObjectToStringTagsRawPrimitives()
        => Assert.Equal(
            "[object Boolean],[object Number],[object String],[object Null],[object Undefined]",
            Eval("var t=Object.prototype.toString;" +
                 "[t.call(true),t.call(0),t.call('x'),t.call(null),t.call(undefined)].join(',')"));

    [Fact]
    public void ObjectToStringTagsRawSymbolAndBigInt()
        => Assert.Equal("[object Symbol],[object BigInt]", Eval(
            "var t=Object.prototype.toString; [t.call(Symbol()),t.call(1n)].join(',')"));
}
