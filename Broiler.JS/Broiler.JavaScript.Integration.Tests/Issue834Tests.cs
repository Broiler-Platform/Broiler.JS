using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/834
//
// Fixed here:
//
//   Problem 85 (arr[4294967295] Expected SameValue(«undefined», «100»)) and the
//   related Problem 84 (arrObj.hasOwnProperty("4294967295")) — root cause shared.
//
//   4294967295 (== 2^32 - 1 == uint.MaxValue) is a valid uint, but per spec it is
//   NOT an array index: array indices are the integers 0 .. 2^32 - 2. Only those
//   live in an array/object's dense uint element storage; 2^32 - 1 must be an
//   ordinary string-keyed property "4294967295".
//
//   JSNumber.ToKey routed 4294967295 through the uint element path (`(uint)n == n`
//   is true for it), while the string path (JSString.ToKey / NumberParser.
//   TryGetArrayIndex) correctly treated it as a string key. The two paths then
//   disagreed: `o[4294967295] = v` stored a uint element that `o["4294967295"]`,
//   `Object.keys`, and `hasOwnProperty("4294967295")` could not see, and a value
//   defined under the string key was invisible to numeric access `o[4294967295]`.
//   ToKey now excludes uint.MaxValue, matching the string path.
//
//   Problems 4, 80, 98 (%ThrowTypeError% poison pills) — Function.prototype's
//   "caller" and "arguments" accessor properties were each built from four
//   separate native getter/setter functions named "caller"/"arguments" and left
//   extensible. Per §10.2.4 / §20.2.3 the [[Get]] and [[Set]] of both properties
//   must be the single per-realm %ThrowTypeError% intrinsic — the same object
//   across all four slots (and the same one an unmapped arguments object's
//   "callee" poison uses). That intrinsic is anonymous (name ""), length 0, and
//   non-extensible with frozen, non-configurable name/length. PatchFunction-
//   Prototype now installs the shared GetOrCreateThrowTypeError() function.
//
//   Problems 50, 51 (Iterator.from getPrototypeOf step) — Iterator.from decided
//   whether to wrap its resolved iterator with a CLR type check (`is
//   JSIteratorObject`) instead of OrdinaryHasInstance(%Iterator%, iterator). It
//   therefore skipped the observable [[GetPrototypeOf]] walk, so a Proxy iterator's
//   getPrototypeOf trap never fired. From now reads "next" (GetIteratorDirect) and
//   then walks the iterator's prototype chain via GetPrototypeOf looking for
//   %Iterator.prototype%, returning the iterator unwrapped when it is found.
//
// Out of scope: the remaining ~94 problems in the issue are unrelated engine
// areas (Temporal/Intl/CLDR ordering, RegExp/Unicode, with/Proxy env, etc.).
public class Issue834Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // ---- Problem 85: numeric access reaches a string-defined 2^32-1 property ----

    [Fact]
    public void NumericReadSeesPropertyDefinedAtMaxUintViaString()
        => Assert.Equal("100", Eval(
            "var a = []; Object.defineProperties(a, { '4294967295': " +
            "{ value: 100, writable: true, enumerable: true, configurable: true } }); String(a[4294967295])"));

    // ---- Problem 84: hasOwnProperty agrees with the define ----

    [Fact]
    public void HasOwnPropertyAtMaxUintIsTrueAndLengthUnaffected()
        => Assert.Equal("true,0", Eval(
            "var a = []; Object.defineProperty(a, '4294967295', " +
            "{ value: 1, writable: true, enumerable: true, configurable: true }); " +
            "a.hasOwnProperty('4294967295') + ',' + a.length"));

    // ---- the shared root cause: numeric and string access must agree at 2^32-1 ----

    [Fact]
    public void NumericSetIsVisibleViaStringKey()
        => Assert.Equal("true", Eval("var o = {}; o[4294967295] = 1; String(o.hasOwnProperty('4294967295'))"));

    [Fact]
    public void StringSetIsVisibleViaNumericAccess()
        => Assert.Equal("7", Eval("var o = {}; o['4294967295'] = 7; String(o[4294967295])"));

    [Fact]
    public void MaxUintKeyIsEnumeratedByObjectKeys()
        => Assert.Equal("4294967295", Eval("var o = {}; o[4294967295] = 1; Object.keys(o).join(',')"));

    [Fact]
    public void AssigningMaxUintIndexOnArrayDoesNotChangeLength()
        => Assert.Equal("0,5", Eval("var a = []; a[4294967295] = 5; a.length + ',' + String(a[4294967295])"));

    // ---- guard against over-correction: 2^32-2 is still a real array index ----

    [Fact]
    public void LargestValidArrayIndexStillExtendsLength()
        => Assert.Equal("4294967295", Eval("var a = []; a[4294967294] = 3; String(a.length)"));

    // ---- Problems 4/80/98: shared per-realm %ThrowTypeError% poison pill ----

    [Fact]
    public void CallerGetAndSetAreTheSameFunction()
        => Assert.Equal("true", Eval(
            "var d = Object.getOwnPropertyDescriptor(Function.prototype, 'caller'); String(d.get === d.set)"));

    [Fact]
    public void ArgumentsGetAndSetAreTheSameFunction()
        => Assert.Equal("true", Eval(
            "var d = Object.getOwnPropertyDescriptor(Function.prototype, 'arguments'); String(d.get === d.set)"));

    [Fact]
    public void CallerAndArgumentsShareTheSameThrowTypeError()
        => Assert.Equal("true", Eval(
            "var c = Object.getOwnPropertyDescriptor(Function.prototype, 'caller');" +
            "var a = Object.getOwnPropertyDescriptor(Function.prototype, 'arguments');" +
            "String(c.get === a.get && a.get === c.set && c.set === a.set)"));

    [Fact]
    public void UnmappedArgumentsCalleePoisonIsTheSameThrowTypeError()
        => Assert.Equal("true", Eval(
            "var args = (function () { 'use strict'; return arguments; })();" +
            "var callee = Object.getOwnPropertyDescriptor(args, 'callee');" +
            "var caller = Object.getOwnPropertyDescriptor(Function.prototype, 'caller');" +
            "String(callee.get === caller.get && callee.get === callee.set)"));

    [Fact]
    public void ThrowTypeErrorIsAnonymousZeroLengthAndNonExtensible()
        => Assert.Equal("\"\",0,false", Eval(
            "var t = Object.getOwnPropertyDescriptor(Function.prototype, 'caller').get;" +
            "JSON.stringify(t.name) + ',' + t.length + ',' + Object.isExtensible(t)"));

    [Fact]
    public void ThrowTypeErrorNameAndLengthAreFrozen()
        => Assert.Equal(
            "{\"value\":0,\"writable\":false,\"enumerable\":false,\"configurable\":false}|" +
            "{\"value\":\"\",\"writable\":false,\"enumerable\":false,\"configurable\":false}",
            Eval(
                "var t = Object.getOwnPropertyDescriptor(Function.prototype, 'caller').get;" +
                "JSON.stringify(Object.getOwnPropertyDescriptor(t, 'length')) + '|' +" +
                "JSON.stringify(Object.getOwnPropertyDescriptor(t, 'name'))"));

    [Fact]
    public void FunctionPrototypeCallerIsNonEnumerableConfigurableAccessor()
        => Assert.Equal("false,true", Eval(
            "var d = Object.getOwnPropertyDescriptor(Function.prototype, 'caller');" +
            "d.enumerable + ',' + d.configurable"));

    // ---- Problems 50/51: Iterator.from performs the OrdinaryHasInstance walk ----

    [Fact]
    public void IteratorFromWalksPrototypeChainOfProxyIterator()
        => Assert.Equal("get:Symbol(Symbol.iterator),get:next,getProto", Eval(
            "var log = [];" +
            "var inner = { next: function () { return { done: true }; } };" +
            "var p = new Proxy(inner, {" +
            "  get: function (t, k) { log.push('get:' + String(k)); return Reflect.get(t, k); }," +
            "  getPrototypeOf: function () { log.push('getProto'); return null; } });" +
            "Iterator.from(p); log.join(',')"));

    [Fact]
    public void IteratorFromReadsNextBeforeGetPrototypeOf()
        => Assert.Equal(
            "get:Symbol(Symbol.iterator),get:next,getProto,get:return,ret", Eval(
            "var log = [];" +
            "var inner = { next: function () { return { done: true }; }," +
            "  return: function () { log.push('ret'); return { done: true }; } };" +
            "var p = new Proxy(inner, {" +
            "  get: function (t, k) { log.push('get:' + String(k)); return Reflect.get(t, k); }," +
            "  getPrototypeOf: function () { log.push('getProto'); return null; } });" +
            "Iterator.from(p).return(); log.join(',')"));

    [Fact]
    public void IteratorFromReturnsUnwrappedWhenPrototypeChainHasIteratorPrototype()
        => Assert.Equal("true", Eval(
            "var proto = Object.getPrototypeOf([].values());" +
            "var inner = Object.create(proto); inner.next = function () { return { done: true }; };" +
            "var p = new Proxy(inner, { get: function (t, k) { return Reflect.get(t, k); }," +
            "  getPrototypeOf: function (t) { return Reflect.getPrototypeOf(t); } });" +
            "String(Iterator.from(p) === p)"));

    [Fact]
    public void IteratorFromStillWrapsAndIteratesPlainArray()
        => Assert.Equal("2,4,6", Eval("Iterator.from([1, 2, 3]).map(function (x) { return x * 2; }).toArray().join(',')"));
}
