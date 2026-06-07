using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/693
//
// Fixed here:
//
// Problem 10 — Intl `*.supportedLocalesOf` returned every requested locale
//   instead of only the supported ones. SupportedLocales / LookupSupportedLocales
//   (ECMA-402) must narrow the canonicalized request list to the locales the
//   runtime can actually serve, using BestAvailableLocale (strip extension
//   sequences, then trim trailing subtags). The implementation skipped this step
//   and echoed the whole list back, so the test262 `supportedLocalesOf/basic.js`
//   family — which passes `[defaultLocale, "zxx"]` ("zxx" = "no linguistic
//   content") and asserts the result length is 1 — saw length 2. AvailableLocales
//   is sourced from the host .NET globalization (ICU/CLDR) culture list.
//
// Problem 1 (Intl subset) — `Intl.NumberFormat.prototype.formatRange` and
//   `formatRangeToParts` silently coerced a Symbol operand to a string instead of
//   throwing. ToIntlMathematicalValue cannot convert a Symbol to a number, so the
//   `formatRange/argument-to-Intlmathematicalvalue-throws.js` and
//   `formatRangeToParts/...` tests expect a TypeError. CoerceRangeValue now throws
//   it.
//
// Problem 9 (subclass return-override) — a class constructor that explicitly
//   returns a distinct object had that object's [[Prototype]] overwritten with the
//   class prototype, so `derived-class-return-override-with-object.js` saw the
//   returned `{}` reported as `instanceof Derived`. Two coupled causes were fixed:
//   (a) an explicit `super()` ran with a cleared CurrentNewTarget (the call-stack
//   item had consumed it), so the superclass [[Construct]] used its own prototype
//   instead of the most-derived one — InvokeSuperConstructor now restores the
//   active new target; (b) JSClass.CreateInstance unconditionally rewrote the
//   prototype — it now does so only for the engine-allocated `this`, leaving an
//   explicitly returned object untouched, while body-less default-derived classes
//   (which inherit a native/derived delegate) keep the previous behaviour.
//
// Problem 9 (computed-property-abrupt-completion) — class computed property keys
//   were not all evaluated in source order: an instance field's key ran before a
//   preceding static element's key, so `static [throws()]; [neverExecuted=true];`
//   still evaluated the second key. ClassDefinitionEvaluation evaluates every
//   ClassElementName in source order before the elements are installed; CreateClass
//   now pre-evaluates each computed key in order into a class-scope variable that
//   methods/accessors/static fields consume, instead of re-evaluating it inside the
//   deferred MemberInit.
//
// Problem 9 (classPrototype) — a class constructor's "prototype" property was
//   writable. A class's "prototype" is a non-writable, non-enumerable,
//   non-configurable data property (MakeClassConstructor), unlike an ordinary
//   function's writable prototype. JSClass now installs it read-only. (The same
//   file's `static ["prototype"]` rejection already passes via the computed-key
//   change above, and string-keyed `"constructor"(){ return {} }` via the
//   constructor return-override fix.)
//
// Problem 9 (TypedArray seal-and-freeze) — a typed array's own [[OwnPropertyKeys]]
//   enumeration returned only the integer indices, dropping ordinary string-keyed
//   own properties (e.g. `ta.foo = 1`). So getOwnPropertyNames / for-in / Object.keys
//   missed them and Object.isFrozen / isSealed (which walk the own keys) wrongly
//   reported a non-extensible empty typed array with a still-writable extra property
//   as frozen. JSTypedArray.GetAllKeys now defers to the base enumerator (indices
//   first, then string keys), matching ordinary objects.
//
// Problem 9 (TypedArray has-property-op) — `index in typedArray` for an out-of-range
//   canonical numeric index walked the prototype chain, so an element placed on the
//   prototype made `50 in ta` true. An integer-indexed exotic object's [[HasProperty]]
//   resolves a numeric index to IsValidIntegerIndex and never consults the prototype;
//   JSTypedArray now overrides HasProperty accordingly (non-numeric keys still
//   inherit).
//
// Problem 3 (Array.from iterator close) — when the map callback or the
//   element-store (CreateDataPropertyOrThrow) threw, Array.from let the error
//   propagate without closing the source iterator. It now performs IteratorClose on
//   such an error (suppressing a secondary completion from return()), while an error
//   from the iterator's own next()/value/done still propagates without a close.
//
// Out of scope (architectural / CLDR / deep parser, matching the triage carried
// in #683 / #685 / #687 / #689 / #691, and confirmed by probing the engine for
// this issue): the private-* brand-check and double-initialisation families,
// super-*-reference-null, the proxy default-handler TypeError tests, AnnexB eval
// binding re-init / skip-early-err, scope-param-elem-var, NumberFormat signDisplay
// "negative" currency CLDR formatting, and the staging/sm negative SyntaxError
// grab-bag.
public class Issue693Tests
{
    private static JSValue Eval(string code)
    {
        using var ctx = new JSContext(experimentalFeatures: JavaScriptFeatureFlags.AllExperimentalEs2026);
        return ctx.Eval(code);
    }

    // The exact shape of intl402/<Service>/supportedLocalesOf/basic.js: a request
    // mixing the default (supported) locale with the unsupported "zxx" tag must
    // resolve to a one-element list containing only the default locale.
    [Theory]
    [InlineData("Collator")]
    [InlineData("DateTimeFormat")]
    [InlineData("NumberFormat")]
    [InlineData("PluralRules")]
    [InlineData("RelativeTimeFormat")]
    [InlineData("ListFormat")]
    [InlineData("Segmenter")]
    public void UnsupportedLocaleIsDropped(string ctor)
    {
        var code =
            "var C = Intl." + ctor + ";" +
            "var def = new C().resolvedOptions().locale;" +
            "var s = C.supportedLocalesOf([def, 'zxx']);" +
            "s.length + '|' + (s[0] === def);";
        Assert.Equal("1|true", Eval(code).ToString());
    }

    // "und" (undetermined) and other tags with no available match are dropped.
    [Fact]
    public void UndeterminedLocaleIsDropped()
        => Assert.Equal("0", Eval("Intl.NumberFormat.supportedLocalesOf(['und']).length;").ToString());

    // A genuinely supported locale survives, and a script subtag still matches via
    // the BestAvailableLocale language fallback.
    [Fact]
    public void SupportedLocalesAreKept()
        => Assert.Equal("3", Eval("Intl.NumberFormat.supportedLocalesOf(['de-DE', 'fr', 'en-Latn-US']).length;").ToString());

    // A Unicode extension sequence is stripped before matching but preserved on
    // the locale that is returned.
    [Fact]
    public void ExtensionSequenceIsPreservedOnSupportedLocale()
        => Assert.Equal("de-DE-u-co-phonebk", Eval(
            "Intl.NumberFormat.supportedLocalesOf(['de-DE-u-co-phonebk'])[0];").ToString());

    // An empty request stays an empty array (not undefined / not throwing).
    [Fact]
    public void EmptyRequestReturnsEmptyArray()
        => Assert.Equal("true|0", Eval(
            "var s = Intl.NumberFormat.supportedLocalesOf([]); Array.isArray(s) + '|' + s.length;").ToString());

    // ---- Problem 1: formatRange / formatRangeToParts reject Symbol operands ----

    // ToIntlMathematicalValue throws a TypeError for a Symbol, in either position,
    // for both the string-producing formatRange and the parts-producing variant.
    [Theory]
    [InlineData("formatRange(Symbol(), 1)")]
    [InlineData("formatRange(1, Symbol())")]
    [InlineData("formatRangeToParts(Symbol(), 1)")]
    [InlineData("formatRangeToParts(1, Symbol())")]
    public void FormatRangeRejectsSymbolOperand(string call)
    {
        var code =
            "var nf = new Intl.NumberFormat('en');" +
            "var t; try { nf." + call + "; t = 'no throw'; } catch (e) { t = e.constructor.name; } t;";
        Assert.Equal("TypeError", Eval(code).ToString());
    }

    // A NaN operand remains a RangeError, and ordinary numeric ranges still format.
    [Fact]
    public void FormatRangeStillRejectsNaNAndFormatsNumbers()
    {
        Assert.Equal("RangeError", Eval(
            "var nf = new Intl.NumberFormat('en');" +
            "var t; try { nf.formatRange(NaN, 1); t = 'no'; } catch (e) { t = e.constructor.name; } t;").ToString());
        Assert.Equal("1–2", Eval("new Intl.NumberFormat('en').formatRange(1, 2);").ToString());
    }

    // ---- Problem 9: constructor return-override preserves the returned object ----

    // The exact shape of derived-class-return-override-with-object.js: a derived
    // constructor that returns an object after super() yields that object as-is,
    // discarding the super-constructed `this` (and its class identity).
    [Fact]
    public void DerivedConstructorReturnOverrideDiscardsThis()
        => Assert.Equal("1|undefined|false|false", Eval(
            "var calls = 0;"
            + "class Base { constructor(){ this.prop = 1; calls++; } }"
            + "class Derived extends Base { constructor(){ super(); return {}; } }"
            + "var o = new Derived();"
            + "calls + '|' + (typeof o.prop) + '|' + (o instanceof Derived) + '|' + (o instanceof Base);").ToString());

    // A base class returning a distinct object keeps that object's own prototype.
    [Fact]
    public void BaseConstructorReturnOverrideKeepsObjectPrototype()
        => Assert.Equal("true|false|true", Eval(
            "class Base { constructor(){ return { tag: 'x' }; } }"
            + "var o = new Base();"
            + "('tag' in o) + '|' + (o instanceof Base) + '|' + (Object.getPrototypeOf(o) === Object.prototype);").ToString());

    // Regression guard: a normal derived instance (no object return) still gets the
    // most-derived prototype — the explicit super() call must thread the new target.
    [Fact]
    public void NormalDerivedConstructionKeepsMostDerivedPrototype()
        => Assert.Equal("1|2|true|true", Eval(
            "class A { constructor(){ this.x = 1; } }"
            + "class B extends A { constructor(){ super(); this.y = 2; } }"
            + "var b = new B();"
            + "b.x + '|' + b.y + '|' + (b instanceof B) + '|' + (b instanceof A);").ToString());

    // Regression guard: new.target observed inside a superclass constructor is the
    // original (most-derived) constructor, not the immediate superclass.
    [Fact]
    public void NewTargetIsThreadedThroughExplicitSuper()
        => Assert.Equal("B", Eval(
            "var seen;"
            + "class A { constructor(){ seen = new.target.name; } }"
            + "class B extends A { constructor(){ super(); } }"
            + "new B(); seen;").ToString());

    // Regression guard: a body-less subclass of a native constructor still produces
    // instances branded with the subclass — even when constructed during another
    // [[Construct]] (the throwing-getter idiom from the test262 abrupt-completion
    // suites). This is the case the previous unconditional prototype rewrite covered.
    [Fact]
    public void NativeSubclassThrownDuringConstructionKeepsBrand()
        => Assert.Equal("T", Eval(
            "class T extends Error {}"
            + "function F(){ throw new T(); }"
            + "var r; try { new F(); } catch (e) { r = e.constructor.name; } r;").ToString());

    // ---- Problem 9: class computed keys evaluate in source order ----

    // The exact shape of computed-property-abrupt-completition.js: a throwing
    // computed key on an earlier element must abort before a later element's key
    // (here an assignment) is evaluated, for both element orderings.
    [Theory]
    [InlineData("[ac()]; [n = true];")]
    [InlineData("static [ac()]; [n = true];")]
    [InlineData("static [ac()]; static [n = true];")]
    public void ComputedKeyAbruptCompletionShortCircuits(string body)
    {
        var code =
            "function ac(){ throw new Test262Error(); }"
            + "function Test262Error(){} var n = false; var t;"
            + "try { eval('class C { " + body + " }'); t = 'no-throw'; }"
            + "catch (e) { t = e.constructor.name; }"
            + "t + '|' + n;";
        Assert.Equal("Test262Error|false", Eval(code).ToString());
    }

    // Every computed key — instance/static, field/method/accessor — is evaluated
    // exactly once, in source order.
    [Fact]
    public void ComputedKeysEvaluateInSourceOrder()
        => Assert.Equal("a,b,c,d,e,f", Eval(
            "var log = []; function k(n){ log.push(n); return n; }"
            + "class C {"
            + "  [k('a')](){}"
            + "  static [k('b')](){}"
            + "  [k('c')] = 1;"
            + "  static [k('d')] = 2;"
            + "  get [k('e')](){ return 1; }"
            + "  static get [k('f')](){ return 1; }"
            + "}"
            + "log.join(',');").ToString());

    // A computed static element named "prototype" is a TypeError (it would clobber
    // the non-configurable prototype). Covers the three accessor/method forms.
    [Theory]
    [InlineData("static [\"prototype\"](){}")]
    [InlineData("static get [\"prototype\"](){}")]
    [InlineData("static set [\"prototype\"](x){}")]
    public void StaticComputedPrototypeNameThrows(string element)
    {
        var code = "var t; try { eval('(class a { constructor(){}; " + element + " })'); t = 'no-throw'; }"
            + " catch (e) { t = e.constructor.name; } t;";
        Assert.Equal("TypeError", Eval(code).ToString());
    }

    // ---- Problem 9: a class's "prototype" property is read-only ----

    // ECMA-262 MakeClassConstructor: a class's "prototype" is non-writable,
    // non-enumerable, non-configurable — for base, derived, and class expressions.
    [Theory]
    [InlineData("class A { constructor(){} }", "A")]
    [InlineData("var E = class { constructor(){} };", "E")]
    [InlineData("class A {} class B extends A {}", "B")]
    public void ClassPrototypeIsReadOnly(string setup, string name)
        => Assert.Equal("false|false|false", Eval(
            setup + " var d = Object.getOwnPropertyDescriptor(" + name + ", 'prototype');"
            + " d.writable + '|' + d.configurable + '|' + d.enumerable;").ToString());

    // An ordinary function's "prototype" stays writable — the change is class-only.
    [Fact]
    public void OrdinaryFunctionPrototypeStaysWritable()
        => Assert.Equal("true|false|false", Eval(
            "var d = Object.getOwnPropertyDescriptor(function f(){}, 'prototype');"
            + "d.writable + '|' + d.configurable + '|' + d.enumerable;").ToString());

    // The read-only prototype cannot be reassigned: a strict write throws.
    [Fact]
    public void ClassPrototypeReassignmentThrowsInStrictMode()
        => Assert.Equal("TypeError", Eval(
            "'use strict'; class A {} var t; try { A.prototype = {}; t = 'no-throw'; }"
            + " catch (e) { t = e.constructor.name; } t;").ToString());

    // A string-keyed "constructor" method is the class constructor; returning an
    // object from it discards the instance (staging/sm/class/stringConstructor.js).
    [Fact]
    public void StringKeyedConstructorActsAsConstructor()
        => Assert.Equal("false|false", Eval(
            "class A { \"constructor\"() { return {}; } }"
            + "class B extends class {} { \"constructor\"() { return {}; } }"
            + "(new A() instanceof A) + '|' + (new B() instanceof B);").ToString());

    // ---- Problem 9: typed-array own keys include extra string properties ----

    // A typed array's own keys are the integer indices followed by ordinary
    // string-keyed own properties, surfaced through getOwnPropertyNames/keys/for-in.
    [Fact]
    public void TypedArrayOwnKeysIncludeExtraProperties()
    {
        Assert.Equal("0,1,b", Eval(
            "var a = new Int32Array(2); a.b = 't'; Object.getOwnPropertyNames(a).join(',');").ToString());
        Assert.Equal("0,1,foo", Eval(
            "var a = new Int32Array(2); a.foo = 9; Object.keys(a).join(',');").ToString());
        Assert.Equal("0,1,foo", Eval(
            "var a = new Int32Array(2); a.foo = 9; var r = []; for (var k in a) r.push(k); r.join(',');").ToString());
    }

    // staging/sm/TypedArray/seal-and-freeze.js: an empty non-extensible typed array
    // with a still-writable extra own property is NOT frozen; only once that
    // property is made non-writable & non-configurable does it become frozen.
    [Fact]
    public void TypedArrayWithWritableExtraPropertyIsNotFrozen()
    {
        Assert.Equal("false", Eval(
            "var a = new Int32Array(0); a.b = 't'; Object.preventExtensions(a); String(Object.isFrozen(a));").ToString());
        Assert.Equal("true", Eval(
            "var a = new Int32Array(0); a.b = 't'; Object.preventExtensions(a);"
            + "Object.defineProperty(a, 'b', { configurable: false, writable: false });"
            + "String(Object.isFrozen(a));").ToString());
    }

    // The fix must not disturb element iteration, spread, or symbol-keyed lookups.
    [Fact]
    public void TypedArrayIterationAndSymbolsUnaffected()
    {
        Assert.Equal("10,20", Eval("var a = new Int32Array([10,20]); a.foo = 9; Array.from(a).join(',');").ToString());
        Assert.Equal("1,2,3", Eval("[...new Int32Array([1,2,3])].join(',');").ToString());
        Assert.Equal("1", Eval("var a = new Int32Array(2); a[Symbol('x')] = 1; String(Object.getOwnPropertySymbols(a).length);").ToString());
    }

    // ---- Problem 9: `in` on a typed array does not consult the prototype for indices ----

    // An out-of-range numeric index is absent even when the prototype carries a
    // same-named element, but inherited non-index properties are still visible.
    [Fact]
    public void TypedArrayInOperatorIgnoresInheritedIndices()
    {
        // out-of-range index on the prototype is still not "in" the typed array
        Assert.Equal("false", Eval("var o = new Int32Array(5); o.__proto__[50] = 'x'; String(50 in o);").ToString());
        // negative canonical numeric index likewise does not consult the prototype
        Assert.Equal("false", Eval("var o = new Int32Array(5); o.__proto__[-1] = 'x'; String(-1 in o);").ToString());
        // a valid own index is present
        Assert.Equal("true", Eval("var o = new Int32Array(5); String(3 in o);").ToString());
        // an inherited NON-index property is visible
        Assert.Equal("true", Eval("var o = new Int32Array(5); o.__proto__.a = 'world'; String('a' in o);").ToString());
    }

    // hasOwnProperty is unchanged: own indices and own string properties are present,
    // out-of-range indices are not.
    [Fact]
    public void TypedArrayHasOwnPropertyUnaffected()
        => Assert.Equal("true|true|false", Eval(
            "var o = new Int32Array(5); o.b = 1;"
            + "o.hasOwnProperty('b') + '|' + o.hasOwnProperty(0) + '|' + o.hasOwnProperty(50);").ToString());

    // ---- Problem 3: Array.from closes the iterator on a map/store error ----

    private const string IterableHarness =
        "var closed = false;"
        + "var iterable = { [Symbol.iterator]() { var first = true; return {"
        + "  next() { if (first) { first = false; return { value: 1, done: false }; } return { value: undefined, done: true }; },"
        + "  return() { closed = true; return {}; } }; } };";

    // A throwing map callback closes the iterator and propagates the original error.
    [Fact]
    public void ArrayFromClosesIteratorWhenMapThrows()
        => Assert.Equal("map throws|true", Eval(
            IterableHarness
            + "var msg; try { Array.from(iterable, function(){ throw 'map throws'; }); } catch (e) { msg = e; }"
            + "msg + '|' + closed;").ToString());

    // A throwing element store (here a defineProperty trap on the constructed object)
    // also closes the iterator.
    [Fact]
    public void ArrayFromClosesIteratorWhenStoreThrows()
        => Assert.Equal("defineProperty throws|true", Eval(
            "class MyArray extends Array { constructor(){ return new Proxy({}, { defineProperty(){ throw 'defineProperty throws'; } }); } }"
            + IterableHarness
            + "var msg; try { MyArray.from(iterable); } catch (e) { msg = e; }"
            + "msg + '|' + closed;").ToString());

    // An error from the iterator's own next() does NOT close it.
    [Fact]
    public void ArrayFromDoesNotCloseWhenNextThrows()
        => Assert.Equal("next throws|false", Eval(
            "var closed = false;"
            + "var iterable = { [Symbol.iterator]() { return {"
            + "  next() { throw 'next throws'; }, return() { closed = true; return {}; } }; } };"
            + "var msg; try { Array.from(iterable); } catch (e) { msg = e; }"
            + "msg + '|' + closed;").ToString());

    // A secondary error from return() during the close is suppressed; the original
    // map/store error is what propagates.
    [Fact]
    public void ArrayFromSuppressesSecondaryCloseError()
        => Assert.Equal("defineProperty throws|true", Eval(
            "class MyArray extends Array { constructor(){ return new Proxy({}, { defineProperty(){ throw 'defineProperty throws'; } }); } }"
            + "var closed = false;"
            + "var iterable = { [Symbol.iterator]() { var first = true; return {"
            + "  next() { if (first) { first = false; return { value: 1, done: false }; } return { value: undefined, done: true }; },"
            + "  return() { closed = true; throw 'return throws'; } }; } };"
            + "var msg; try { MyArray.from(iterable); } catch (e) { msg = e; }"
            + "msg + '|' + closed;").ToString());
}
