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
//   Problem 92 (Date constructor non-string primitive coercion) — new Date(v)
//   only treated a Number primitive as a time value and forced every other
//   ToPrimitive result through string date-parsing, so new Date(true)/false/null
//   and a @@toPrimitive returning a boolean all became NaN. Per §21.4.2.1 only a
//   String is parsed; every other primitive goes through ToNumber, so the
//   constructor now branches on IsString (DoubleValue implements ToNumber:
//   true → 1, false → 0, null → 0, undefined → NaN).
//
//   Problem 88 (Atomics.store -0 return value) — Atomics.store returned
//   𝔽(ToIntegerOrInfinity(value)) but Math.Truncate leaves -0.0 (and values in
//   (-1, 0)) as -0.0, so Atomics.store(ta, 0, -0) returned -0. ToIntegerOrInfinity
//   normalizes those to +0; the result is now run through that normalization.
//
//   Problems 95, 96 (%TypedArray%.prototype.map species-create order) — map ran
//   the callback loop first and only then created the result via
//   TypedArraySpeciesCreate, so a throwing constructor / @@species getter still
//   invoked the callback for every element. §23.2.3.20 creates the result array
//   (step 4) before the callback loop (step 5); map now does the same, so an
//   abrupt species create aborts before any callback runs.
//
//   Problem 22 (optional-chaining short-circuit propagation) — once a `?.` link
//   appeared, the parser marked EVERY following link in the chain as coalescing, so
//   a trailing non-optional access short-circuited on its own undefined value:
//   `a?.b.c` returned undefined when `a.b` was undefined instead of throwing. The
//   short-circuit must instead propagate only from the `?.` link's nullish base. The
//   chain now lowers through a skip sentinel: a `?.` link yields it on a nullish base,
//   every later link propagates it (but a genuine undefined still throws), and the
//   chain root converts it back to undefined — so `a?.b.c` short-circuits when `a` is
//   nullish yet throws when `a.b` is undefined, while `a?.b()`, `a?.[k]`, parenthesised
//   resets and method `this` binding all keep working.
//
// Out of scope: the remaining ~89 problems in the issue are unrelated engine
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

    // ---- Problem 92: Date constructor coerces non-string primitives via ToNumber ----

    [Fact]
    public void DateFromBooleanTrueIsOne()
        => Assert.Equal("1", Eval("String(new Date(true).getTime())"));

    [Fact]
    public void DateFromBooleanFalseIsZero()
        => Assert.Equal("0", Eval("String(new Date(false).getTime())"));

    [Fact]
    public void DateFromNullIsZero()
        => Assert.Equal("0", Eval("String(new Date(null).getTime())"));

    [Fact]
    public void DateFromUndefinedIsNaN()
        => Assert.Equal("NaN", Eval("String(new Date(undefined).getTime())"));

    [Fact]
    public void DateFromToPrimitiveReturningBooleanIsCoercedViaToNumber()
        => Assert.Equal("1", Eval(
            "String(new Date({ [Symbol.toPrimitive]: function () { return true; } }).getTime())"));

    [Fact]
    public void DateFromNumericPrimitiveAndIsoStringStillWork()
        => Assert.Equal("5,5", Eval(
            "String(new Date(5).getTime()) + ',' + String(new Date('1970-01-01T00:00:00.005Z').getTime())"));

    // ---- Problem 88: Atomics.store normalizes -0 to +0 in its return value ----

    [Fact]
    public void AtomicsStoreNormalizesNegativeZeroReturnToPositiveZero()
        => Assert.Equal("Infinity", Eval("var t = new Int32Array(2); String(1 / Atomics.store(t, 0, -0))"));

    [Fact]
    public void AtomicsStoreNormalizesTruncatedToZeroReturnToPositiveZero()
        => Assert.Equal("Infinity", Eval("var t = new Int32Array(2); String(1 / Atomics.store(t, 0, -0.5))"));

    [Fact]
    public void AtomicsStoreStillReturnsIntegralValues()
        => Assert.Equal("5,-5,Infinity,0", Eval(
            "var t = new Int32Array(2);" +
            "String(Atomics.store(t, 0, 5)) + ',' + String(Atomics.store(t, 0, -5)) + ',' +" +
            "String(Atomics.store(t, 0, Infinity)) + ',' + String(Atomics.store(t, 0, NaN))"));

    // ---- Problems 95/96: TypedArray map creates the species result before looping ----

    [Fact]
    public void TypedArrayMapDoesNotCallCallbackWhenSpeciesGetterThrows()
        => Assert.Equal("0", Eval(
            "var calls = 0; var ta = new Float64Array([1, 2, 3, 4]);" +
            "function Ctor() {} Object.defineProperty(Ctor, Symbol.species, { get: function () { throw new Error('sp'); } });" +
            "ta.constructor = Ctor;" +
            "try { ta.map(function () { calls++; return 0; }); } catch (e) {}" +
            "String(calls)"));

    [Fact]
    public void BigIntTypedArrayMapDoesNotCallCallbackWhenSpeciesGetterThrows()
        => Assert.Equal("0", Eval(
            "var calls = 0; var ta = new BigInt64Array([1n, 2n, 3n, 4n]);" +
            "function Ctor() {} Object.defineProperty(Ctor, Symbol.species, { get: function () { throw new Error('sp'); } });" +
            "ta.constructor = Ctor;" +
            "try { ta.map(function () { calls++; return 0n; }); } catch (e) {}" +
            "String(calls)"));

    [Fact]
    public void TypedArrayMapStillMapsValuesAndPreservesType()
        => Assert.Equal("11,12,13,Int32Array", Eval(
            "var r = new Int32Array([1, 2, 3]).map(function (x) { return x + 10; });" +
            "r.join(',') + ',' + (r.constructor === Int32Array ? 'Int32Array' : r.constructor.name)"));

    // ---- Problem 22: optional-chaining short-circuit only propagates from the `?.` base ----

    [Fact]
    public void TrailingMemberAfterOptionalThrowsOnGenuineUndefined()
        => Assert.Equal("TypeError", Eval(
            "var o = { a: undefined }; try { o?.a.b; 'no-throw'; } catch (e) { e.constructor.name; }"));

    [Fact]
    public void TrailingMemberAfterOptionalThrowsWhenPropertyAbsent()
        => Assert.Equal("TypeError", Eval(
            "var o = {}; try { o?.a.b.c; 'no-throw'; } catch (e) { e.constructor.name; }"));

    [Fact]
    public void OptionalChainShortCircuitsWholeChainOnNullishBase()
        => Assert.Equal("undefined", Eval("var o = null; String(o?.a.b.c);"));

    [Fact]
    public void MultipleOptionalLinksShortCircuitIndependently()
        => Assert.Equal("undefined,undefined,7", Eval(
            "function g(o) { return String(o?.a?.b); }" +
            "g(null) + ',' + g({ a: null }) + ',' + g({ a: { b: 7 } })"));

    [Fact]
    public void TrailingComputedAfterOptionalThrowsOnGenuineUndefined()
        => Assert.Equal("TypeError", Eval(
            "var o = { x: undefined }, k = 'x'; try { o?.[k].y; 'no-throw'; } catch (e) { e.constructor.name; }"));

    [Fact]
    public void ParenthesisedOptionalResetsTheChain()
        => Assert.Equal("TypeError", Eval(
            "var o = null; try { (o?.a).b; 'no-throw'; } catch (e) { e.constructor.name; }"));

    [Fact]
    public void OptionalMethodCallChainShortCircuitsAndKeepsThisBinding()
        => Assert.Equal("undefined,4", Eval(
            "var n = null;" +
            "var o = { b: { c: function () { return this === o.b ? 4 : 5; } } };" +
            "String(n?.b.c()) + ',' + String(o?.b.c())"));

    [Fact]
    public void TrailingCallAfterOptionalThrowsWhenIntermediateUndefined()
        => Assert.Equal("TypeError", Eval(
            "var o = { b: undefined }; try { o?.b.c(); 'no-throw'; } catch (e) { e.constructor.name; }"));

    [Fact]
    public void DeleteOptionalChainShortCircuitsButDeletesWhenPresent()
        => Assert.Equal("true,true,false", Eval(
            "var n = null; var r1 = delete n?.b;" +
            "var o = { b: 1 }; var r2 = delete o?.b;" +
            "String(r1) + ',' + String(r2) + ',' + String('b' in o)"));
}
