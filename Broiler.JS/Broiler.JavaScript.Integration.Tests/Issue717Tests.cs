using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/717
//
// Fixed here:
//
//   Problem 5 (descriptor should be enumerable) — Object.prototype.
//   propertyIsEnumerable read the receiver's directly-stored property slots and
//   reported `false` for properties that are synthesized by an exotic object's
//   [[GetOwnProperty]] (the index characters of a String wrapper, typed-array
//   elements, Proxy traps). It now falls back to GetOwnPropertyDescriptor when no
//   slot is stored, so `propertyIsEnumerable("0")` on `new String("abc")` is true.
//
//   Problem 8 (descriptor field order) — FromPropertyDescriptor (the object
//   produced by Object/Reflect.getOwnPropertyDescriptor) inserted its fields in
//   the wrong order (configurable, enumerable, writable, value). Per §6.2.5.4 the
//   order is value, writable, enumerable, configurable for a data descriptor and
//   get, set, enumerable, configurable for an accessor descriptor.
//
//   Problem 9 (proxy ownKeys order in object spread / rest) — `{ ...proxy }` and
//   `let { ...rest } = proxy` copied the proxy's internal slots directly, never
//   invoking the ownKeys / getOwnPropertyDescriptor / get traps. CopyDataProperties
//   now walks [[OwnPropertyKeys]] in order and reads each property through the
//   traps for objects that require observable copying (Proxy).
//
//   Problem 10 (Error cause own property) — the Error/AggregateError constructors
//   never performed InstallErrorCause, so `new Error(msg, { cause }).cause` was
//   not an own property. The constructors now copy `options.cause` (when options
//   is an object that has a "cause" property) as a non-enumerable, writable,
//   configurable own data property.
//
//   Problem 1 (SameValue false/true — sm grab-bag) — two unrelated files:
//   * Symbol/conversions: `sym == Object(sym)` must be true (abstract equality
//     coerces the object operand), and the relational/additive operators on a
//     boxed Symbol must honour an overridden Symbol.prototype.valueOf / @@toPrimitive
//     instead of short-circuiting through the raw CLR ValueOf() to the un-coercible
//     symbol. JSSymbolObject no longer overrides ValueOf(); the +/< coercion paths
//     go through ToPrimitive (default hint).
//   * extensions/arguments-property-access: a non-strict function's `f.arguments`
//     is the live arguments object while `f` is executing (Annex B web reality),
//     and null otherwise; strict functions still throw via the inherited poison pill.
//   * generators/yield-non-regexp: in sloppy code outside a generator `yield` is an
//     ordinary identifier, so `yield /a/g` is division, not a regex literal. The
//     scanner now only treats a `/` after `yield` as a regex when the parser is
//     inside a generator body (it threads that context to the scanner, which must be
//     set before the body's `{` is consumed because of the one-token lookahead).
//
// Out of scope (architectural / deep regex / CLDR): P1 DateTimeFormat CLDR (de
// format, calendars, related-year — the CLDR pipeline has no date patterns / month
// names yet), the RegExp Symbol.match/replace lastIndex grab-bag, P1 with-scope
// strict write under script-host (8.7.2), P2 with/@@unscopables in nested fn, P3 private getter/method
// shadowed by setter (per-evaluation private brand), P4 indirect-eval / with
// var-env, P6 sm RegExp grab-bag, P7 duplicate named capture groups (requires
// distinct .NET group numbering + backreference rewrite).
public class Issue717Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // ---- Problem 10: Error cause own property ----

    [Fact]
    public void ErrorCauseIsNonEnumerableOwnProperty()
        => Assert.Equal(
            "{\"value\":42,\"writable\":true,\"enumerable\":false,\"configurable\":true}",
            Eval("JSON.stringify(Object.getOwnPropertyDescriptor(new Error('m', { cause: 42 }), 'cause'))"));

    [Fact]
    public void ErrorWithoutOptionsHasNoCause()
        => Assert.Equal("false", Eval("Object.prototype.hasOwnProperty.call(new Error('m'), 'cause')"));

    [Fact]
    public void ErrorWithUndefinedCauseStillInstallsProperty()
        => Assert.Equal("true,undefined",
            Eval("var d = Object.getOwnPropertyDescriptor(new Error('m', { cause: undefined }), 'cause'); ('value' in d) + ',' + String(d.value)"));

    [Fact]
    public void TypeErrorCauseIsInstalled()
        => Assert.Equal("9", Eval("new TypeError('m', { cause: 9 }).cause + ''"));

    [Fact]
    public void AggregateErrorCauseIsInstalled()
        => Assert.Equal(
            "{\"value\":7,\"writable\":true,\"enumerable\":false,\"configurable\":true}",
            Eval("JSON.stringify(Object.getOwnPropertyDescriptor(new AggregateError([], 'm', { cause: 7 }), 'cause'))"));

    [Fact]
    public void ErrorCauseReadFromInheritedProperty()
        => Assert.Equal("5", Eval("var p = { cause: 5 }; var o = Object.create(p); new Error('m', o).cause + ''"));

    // ---- Problem 8: FromPropertyDescriptor field order ----

    [Fact]
    public void DataDescriptorFieldOrder()
        => Assert.Equal("value,writable,enumerable,configurable",
            Eval("Object.getOwnPropertyNames(Reflect.getOwnPropertyDescriptor({ p: 'foo' }, 'p')).join(',')"));

    [Fact]
    public void AccessorDescriptorFieldOrder()
        => Assert.Equal("get,set,enumerable,configurable",
            Eval("Object.getOwnPropertyNames(Object.getOwnPropertyDescriptor({ get x() { return 1; } }, 'x')).join(',')"));

    [Fact]
    public void SymbolPropertyDescriptorFieldOrder()
        => Assert.Equal("value,writable,enumerable,configurable",
            Eval("var s = Symbol(); var o = {}; o[s] = 1; Object.getOwnPropertyNames(Reflect.getOwnPropertyDescriptor(o, s)).join(',')"));

    [Fact]
    public void ProxyDefinePropertyTrapReceivesCanonicalDescriptor()
        => Assert.Equal("set,configurable",
            Eval(@"
var seen;
var proxy = new Proxy({}, {
  defineProperty: function (_t, _k, desc) { seen = Object.getOwnPropertyNames(desc).join(','); return true; }
});
Object.defineProperty(proxy, 'foo', { configurable: true, set: function () {} });
seen"));

    // ---- Problem 9: proxy ownKeys order through object spread / rest ----

    private const string ProxySetup = @"
var getOwnKeys = [];
var ownKeysResult = [Symbol(), 'foo', '0'];
var proxy = new Proxy({}, {
  getOwnPropertyDescriptor: function (_t, k) { getOwnKeys.push(k); },
  ownKeys: function () { return ownKeysResult; }
});";

    [Fact]
    public void ObjectSpreadVisitsProxyOwnKeysInOrder()
        => Assert.Equal("Symbol(),foo,0",
            Eval(ProxySetup + "({ ...proxy }); getOwnKeys.map(String).join(',')"));

    [Fact]
    public void ObjectRestVisitsProxyOwnKeysInOrder()
        => Assert.Equal("Symbol(),foo,0",
            Eval(ProxySetup + "let { ...$ } = proxy; getOwnKeys.map(String).join(',')"));

    [Fact]
    public void ObjectSpreadWithLeadingPropertiesVisitsProxyOwnKeys()
        => Assert.Equal("Symbol(),foo,0",
            Eval(ProxySetup + "var sym = ownKeysResult[0]; ({ [sym]: 0, foo: 0, [0]: 0, ...proxy }); getOwnKeys.map(String).join(',')"));

    [Fact]
    public void ObjectSpreadCopiesEnumerableProxyProperties()
        => Assert.Equal("a,1",
            Eval(@"
var proxy = new Proxy({}, {
  ownKeys: function () { return ['a']; },
  getOwnPropertyDescriptor: function (_t, k) { return { value: 1, enumerable: true, configurable: true }; },
  get: function (_t, k) { return 1; }
});
var o = { ...proxy };
Object.keys(o).join(',') + ',' + o.a"));

    [Fact]
    public void ObjectSpreadSkipsNonEnumerableProxyProperties()
        => Assert.Equal("",
            Eval(@"
var proxy = new Proxy({}, {
  ownKeys: function () { return ['a']; },
  getOwnPropertyDescriptor: function (_t, k) { return { value: 1, enumerable: false, configurable: true }; }
});
Object.keys({ ...proxy }).join(',')"));

    // ---- Problem 5: propertyIsEnumerable for exotic synthesized properties ----

    [Fact]
    public void StringWrapperIndexIsEnumerable()
        => Assert.Equal("true",
            Eval("Object.prototype.propertyIsEnumerable.call(new String('abc'), '0')"));

    [Fact]
    public void StringWrapperLengthIsNotEnumerable()
        => Assert.Equal("false",
            Eval("Object.prototype.propertyIsEnumerable.call(new String('abc'), 'length')"));

    [Fact]
    public void StringWrapperIndexDescriptor()
        => Assert.Equal(
            "{\"value\":\"a\",\"writable\":false,\"enumerable\":true,\"configurable\":false}",
            Eval("JSON.stringify(Object.getOwnPropertyDescriptor(new String('abc'), '0'))"));

    [Fact]
    public void TypedArrayElementIsEnumerable()
        => Assert.Equal("true",
            Eval("Object.prototype.propertyIsEnumerable.call(new Int8Array([1, 2, 3]), '0')"));

    // ---- Problem 1: Symbol abstract equality & operator coercion ----

    [Fact]
    public void SymbolEqualsItsBoxedWrapper()
        => Assert.Equal("true", Eval("var s = Symbol(); s == Object(s)"));

    [Fact]
    public void SymbolDoesNotEqualUnrelatedWrapper()
        => Assert.Equal("false", Eval("Symbol() == Object(Symbol())"));

    [Fact]
    public void BoxedSymbolNotLooselyEqualToNumber()
        => Assert.Equal("false", Eval("Object(Symbol()) == 0"));

    [Fact]
    public void BoxedSymbolRelationalUsesOverriddenValueOf()
        => Assert.Equal("false", Eval(
            "delete Symbol.prototype[Symbol.toPrimitive]; Symbol.prototype.valueOf = function () { return 117.25; }; Object(Symbol()) < 0"));

    [Fact]
    public void BoxedSymbolAdditionUsesOverriddenValueOf()
        => Assert.Equal("118.25", Eval(
            "delete Symbol.prototype[Symbol.toPrimitive]; Symbol.prototype.valueOf = function () { return 117.25; }; Object(Symbol()) + 1 + ''"));

    [Fact]
    public void BoxedSymbolStringConcatUsesDefaultHint()
        => Assert.Equal("117.25", Eval(
            "delete Symbol.prototype[Symbol.toPrimitive]; Symbol.prototype.valueOf = function () { return 117.25; }; Symbol.prototype.toString = function () { return 'TS'; }; '' + Object(Symbol())"));

    [Fact]
    public void BoxedSymbolToNumberUsesOverriddenValueOf()
        => Assert.Equal("117.25", Eval(
            "delete Symbol.prototype[Symbol.toPrimitive]; Symbol.prototype.valueOf = function () { return 117.25; }; Number(Object(Symbol())) + ''"));

    [Fact]
    public void BoxedSymbolToStringUsesOverriddenToString()
        => Assert.Equal("TS", Eval(
            "delete Symbol.prototype[Symbol.toPrimitive]; Symbol.prototype.toString = function () { return 'TS'; }; String(Object(Symbol()))"));

    [Fact]
    public void SymbolToPrimitiveStillWorksOnBoxedReceiver()
        => Assert.Equal("true", Eval(
            "var s = Symbol('d'); Object(s)[Symbol.toPrimitive]('default') === s"));

    [Fact]
    public void BoxedSymbolDescriptionStillWorks()
        => Assert.Equal("d", Eval("Object(Symbol('d')).description"));

    // ---- Problem 1: legacy function.arguments (Annex B) ----

    [Fact]
    public void NonStrictFunctionArgumentsIsLiveDuringInvocation()
        => Assert.Equal("true,5,undefined,2", Eval(@"
var obj = { test: function () {
  var args = obj.test.arguments;
  return (args !== null) + ',' + args[0] + ',' + args[1] + ',' + args.length;
} };
obj.test(5, undefined);"));

    [Fact]
    public void FunctionArgumentsIsNullWhenNotExecuting()
        => Assert.Equal("true", Eval("function f() {} f(1, 2); f.arguments === null"));

    [Fact]
    public void StrictFunctionArgumentsThrows()
        => Assert.Equal("true", Eval(@"
var sobj = { test: function () {
  'use strict';
  try { sobj.test.arguments; return false; } catch (e) { return e instanceof TypeError; }
} };
sobj.test();"));

    // ---- Problem 1: `yield` as identifier — `/` is division, not a regex ----

    [Fact]
    public void SloppyYieldIsIdentifierSoSlashIsDivision()
        => Assert.Equal("true", Eval(
            "var yield = 12, a = 3, b = 6, g = 2, r = false; yield /a; r = true; b/g; r"));

    [Fact]
    public void GeneratorYieldFollowedBySlashIsRegex()
        => Assert.Equal("ab", Eval("function* g() { yield /ab/.source; } g().next().value"));

    [Fact]
    public void GeneratorMethodYieldFollowedBySlashIsRegex()
        => Assert.Equal("cd", Eval("class C { *g() { yield /cd/.source; } } new C().g().next().value"));

    [Fact]
    public void YieldAsIdentifierInNestedFunctionInsideGenerator()
        => Assert.Equal("5", Eval(
            "function* g() { function h() { var yield = 10, a = 2; return yield /a; } return h(); } g().next().value + ''"));
}
