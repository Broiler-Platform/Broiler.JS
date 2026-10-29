using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/683
//
// Fixed here (a subset of the problems grouped in the issue by their
// Test262Error message):
//
//   * P7 — WeakMap.prototype.has / WeakSet.prototype.has returned `undefined`
//     instead of `false` for an absent key, and WeakMap.prototype.set never
//     updated the value of an already-present key. Covers the WeakMap/WeakSet
//     has/delete "returns false" tests and getOrInsertComputed state-after-throw.
//
//   * P8 — a bound function used with `new` now performs the BoundFunction
//     [[Construct]]: it constructs the target directly with boundArgs ++ args
//     (ignoring the bound `this`) instead of [[Call]]ing it and clobbering the
//     returned object's prototype. Covers Function.prototype.bind 15.3.4.5.2-4-*.
//
//   * P1 — a function invoked from within a constructor body now observes the
//     constructor as its (legacy) caller, because [[Construct]] tracks the
//     executing function just as [[Call]] does. A strict-mode caller is reported
//     as `null`, so reading a property off it throws. Covers Function
//     15.3.5.4_2-*gs.
//
//   * P9 — concise methods, getters and setters no longer expose the legacy
//     `caller`/`arguments` own data properties (only ordinary
//     FunctionDeclaration/FunctionExpression objects do). Covers the
//     method-definition forbidden-ext direct-access-prop-{arguments,caller}.
//
// Out of scope (triaged in the issue): private-name brand identity across class
// evaluations and private-vs-computed-property collisions (architectural — shared
// string keys); AnnexB eval-code global/func block-function-declaration binding
// re-initialization; eval-in-parameter-default var scoping; cross-element computed
// class key evaluation ordering; `\S` inside a character class matching the full
// Unicode whitespace complement; and the CLDR/Intl-dependent NumberFormat cases.
public class Issue683Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // ---- Problem 7: WeakMap / WeakSet has / set ----

    [Fact]
    public void WeakMapHasReturnsFalseForAbsentKey()
        => Assert.Equal("false|false|false|false", Eval(
            "var m = new WeakMap();"
            + "String(m.has(1)) + '|' + String(m.has('')) + '|' +"
            + "String(m.has(null)) + '|' + String(m.has(undefined))"));

    [Fact]
    public void WeakMapHasReturnsFalseAfterDelete()
        => Assert.Equal("true|false", Eval(
            "var k = {}; var m = new WeakMap(); m.set(k, 42);"
            + "var r = m.delete(k);"
            + "String(r) + '|' + String(m.has(k))"));

    [Fact]
    public void WeakSetHasReturnsFalseForAbsentValue()
        => Assert.Equal("false|true|false", Eval(
            "var a = {}, b = {}; var s = new WeakSet(); s.add(a);"
            + "String(s.has(b)) + '|' + String(s.has(a)) + '|' + String(s.delete(b))"));

    [Fact]
    public void WeakMapSetUpdatesExistingKey()
        => Assert.Equal("one|mutated", Eval(
            "var k = {}; var m = new WeakMap(); m.set(k, 'one');"
            + "var before = m.get(k); m.set(k, 'mutated');"
            + "before + '|' + m.get(k)"));

    // ---- Problem 8: bound function [[Construct]] ----

    [Fact]
    public void BoundFunctionConstructPreservesTargetReturn()
        => Assert.Equal("true", Eval(
            "var func = function() { return new Boolean(arguments.length === 0); };"
            + "var NewFunc = Function.prototype.bind.call(func);"
            + "String(new NewFunc().valueOf())"));

    [Fact]
    public void BoundFunctionConstructPrependsBoundArguments()
        => Assert.Equal("1,2,3", Eval(
            "function F() { this.args = Array.prototype.slice.call(arguments).join(','); }"
            + "var B = F.bind(null, 1, 2);"
            + "new B(3).args"));

    // ---- Problem 1: strict caller observed from a constructor body ----

    [Fact]
    public void StrictCallerFromConstructorBodyIsNotLeaked()
        => Assert.Equal("TypeError", Eval(
            "function thr(f){ try { f(); return 'none'; } catch (e) { return e.constructor.name; } }"
            + "function f() { 'use strict'; gNonStrict(); }"
            + "function gNonStrict() { return gNonStrict.caller || gNonStrict.caller.x; }"
            + "thr(function(){ new f(); })"));

    // ---- Problem 9: concise methods have no legacy caller/arguments ----

    [Fact]
    public void ConciseMethodHasNoArgumentsOwnProperty()
        => Assert.Equal("false", Eval(
            "var obj = { method() { return this.method.hasOwnProperty('arguments'); } };"
            + "String(obj.method())"));

    [Fact]
    public void ConciseMethodHasNoCallerOwnProperty()
        => Assert.Equal("false", Eval(
            "var obj = { method() { return this.method.hasOwnProperty('caller'); } };"
            + "String(obj.method())"));

    [Fact]
    public void OrdinaryFunctionStillHasLegacyCallerAndArguments()
        => Assert.Equal("true|true", Eval(
            "function f() {}"
            + "String(f.hasOwnProperty('caller')) + '|' + String(f.hasOwnProperty('arguments'))"));
}
