using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/711
//
// Fixed here:
//
//   Problems 8, 9 & 10 — NamedEvaluation in a `for` LexicalDeclaration head whose
//   binding uses destructuring with an anonymous-function initializer, e.g.
//   `for (const [f = () => {}] = []; ...)` / `for (const { c = class {} } = {}; ...)`.
//   The `for` desugarer renames the pattern's binding identifiers to synthetic
//   numeric temps (so the per-iteration copies don't collide), and that rename
//   happened BEFORE name inference, so the anonymous initializer was named after
//   the temp ("2") instead of the binding ("f"/"c"). The temp identifier now
//   carries the original binding name (AstIdentifier.InferenceName), which the
//   destructuring NamedEvaluation uses for the function's `name`.
//
// Out of scope (unchanged, documented in prior issues): sm grab-bag + CLDR (P1),
//   negative-SyntaxError parser/regex (P2), dynamic-super edge cases + TypedArray
//   object-defineproperty (P3), DateTimeFormat formatRange CLDR (P4/P6-stub),
//   direct-eval var into function/global var-env (P5/P7 eval + compound-assign),
//   per-evaluation private brand for static members via direct eval (P6).
public class Issue711Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // ---- Problems 8/9/10: for-head destructuring names anonymous initializers ----

    [Fact]
    public void ForConstArrayArrowName()
        => Assert.Equal("f", Eval("var r; for (const [f = () => {}] = []; r===undefined;){ r = f.name; } r;"));

    [Fact]
    public void ForConstObjectClassName()
        => Assert.Equal("cls", Eval("var r; for (const { cls = class {} } = {}; r===undefined;){ r = cls.name; } r;"));

    [Fact]
    public void ForLetArrayCoverName()
        => Assert.Equal("cover", Eval("var r; for (let [cover = (function () {})] = []; r===undefined;){ r = cover.name; } r;"));

    [Fact]
    public void ForLetObjectArrowName()
        => Assert.Equal("foo", Eval("var r; for (let { foo = () => {} } = {}; r===undefined;){ r = foo.name; } r;"));

    [Fact]
    public void ForConstArrayFunctionName()
        => Assert.Equal("fn", Eval("var r; for (const [fn = function () {}] = []; r===undefined;){ r = fn.name; } r;"));

    // A named class/function initializer keeps its own name (not the binding name).
    [Fact]
    public void ForNamedClassKeepsOwnName()
        => Assert.Equal("X", Eval("var r; for (const { xCls = class X {} } = {}; r===undefined;){ r = xCls.name; } r;"));

    [Fact]
    public void ForCoverNamedKeepsAnonymous()
        => Assert.Equal("", Eval("var r; for (let [xCover = (0, function () {})] = []; r===undefined;){ r = xCover.name; } r;"));

    // The binding still receives the destructured value, and the temp rename does
    // not leak the numeric temp name anywhere observable.
    [Fact]
    public void ForBindingStillBinds()
        => Assert.Equal("ok", Eval("var r; for (const [a = 'ok'] = []; r===undefined;){ r = a; } r;"));

    // Non-for destructuring (no temp rename) remains correct.
    [Fact]
    public void PlainConstDestructuringName()
        => Assert.Equal("g", Eval("const [g = () => {}] = []; g.name;"));

    // ---- Problem 3: strict-mode [[Construct]] and super.x= receiver semantics ----

    private static string Caught(string body)
        => Eval($"(function(){{ try {{ {body} return 'no throw'; }} catch (e) {{ return e.constructor.name; }} }})();");

    // A class constructor body is strict, so a failing property [[Set]] (adding a
    // property to a non-extensible object) throws a TypeError when run via `new`.
    // Previously [[Construct]] ran without entering strict mode, so it silently failed.
    [Fact]
    public void ClassConstructorIsStrictForPropertySet()
        => Assert.Equal("TypeError", Caught("class C { constructor() { var o = Object.preventExtensions({}); o.p = 0; } } new C();"));

    // A strict ordinary function invoked via `new` performs strict [[Set]] semantics too.
    [Fact]
    public void StrictFunctionConstructorIsStrictForPropertySet()
        => Assert.Equal("TypeError", Caught("function F(){ 'use strict'; var o = Object.preventExtensions({}); o.p = 0; } new F();"));

    // The strict level reflects the currently executing function: a sloppy function
    // invoked from a strict caller must NOT inherit the caller's strict [[Set]] semantics.
    [Fact]
    public void SloppyCalleeFromStrictCallerIsNotStrict()
        => Assert.Equal("no throw", Caught("function sloppy(){ var o = Object.preventExtensions({}); o.p = 0; } function strictF(){ 'use strict'; var z = sloppy(); } strictF();"));

    // `class extends null {}` is derived; its synthetic constructor runs super(...),
    // whose target (%Function.prototype%) is not a constructor -> TypeError.
    [Fact]
    public void ExtendsNullDefaultConstructorThrows()
        => Assert.Equal("TypeError", Caught("new (class extends null {})();"));

    [Fact]
    public void BaseAndDerivedClassesStillConstruct()
        => Assert.Equal("1", Eval("class B { constructor(){ this.x = 1; } } class D extends B {} new D().x;"));

    // super.x = on a base-class method (super base is Object.prototype, a data
    // resolution) where `this` (the receiver) has an own accessor: OrdinarySet returns
    // false, so in strict (class) code it throws -- the receiver's setter is NOT invoked.
    [Fact]
    public void SuperSetReceiverSetterOnlyAccessorThrows()
        => Assert.Equal("TypeError", Caught("class X { constructor(){ Object.defineProperty(this, 'p', { configurable: true, set: function(){} }); } f(){ super.p = 4; } } new X().f();"));

    [Fact]
    public void SuperSetReceiverGetterOnlyAccessorThrows()
        => Assert.Equal("TypeError", Caught("class X { constructor(){ Object.defineProperty(this, 'p', { configurable: true, get: function(){ return 1; } }); } f(){ super.p = 2; } } new X().f();"));

    // A non-writable own data property on the receiver also makes super.x= fail.
    [Fact]
    public void SuperSetReceiverNonWritableDataThrows()
        => Assert.Equal("TypeError", Caught("class X { constructor(){ Object.defineProperty(this, 'p', { configurable: true, writable: false, value: 1 } ); } f(){ super.p = 2; } } new X().f();"));

    // Reflect.set with a distinct receiver follows the same OrdinarySet rule: an
    // accessor own property on the receiver makes the write fail (returns false).
    [Fact]
    public void ReflectSetReceiverAccessorReturnsFalse()
        => Assert.Equal("false", Eval("var base = {}; var receiver = {}; Object.defineProperty(receiver, 'p', { set: function(){} }); '' + Reflect.set(base, 'p', 1, receiver);"));

    // ---- Problem 6: captured lexical binding read inside a body-direct-eval function ----

    // A function whose body contains a direct eval routes outer-resolving free names
    // through an EvalShadowVariable. A captured top-level `let`/`const` is a LEXICAL
    // binding (its own storage), not a global-object property, so the shadow must read
    // it via GetValue, not GlobalValue. Previously it observed undefined, which made
    // `eval(capturedLet)` evaluate `eval(undefined)` -> undefined (the crash root of the
    // private-static multiple-evaluations-direct-eval tests).
    [Fact]
    public void CapturedLetReadInsideBodyDirectEval()
        => Assert.Equal("hello", Eval("let s = 'hello'; (function () { eval(''); return s; })();"));

    [Fact]
    public void CapturedConstUsedAsEvalArgumentInsideFunction()
        => Assert.Equal("1", Eval("const s = '(1)'; (function () { return eval(s); })().toString();"));

    [Fact]
    public void CapturedVarStillReadInsideBodyDirectEval()
        => Assert.Equal("hi", Eval("var s = 'hi'; (function () { eval(''); return s; })();"));

    // The class-string-via-direct-eval pattern no longer throws "Cannot get property
    // access of undefined": each eval returns the class and access() works (the
    // per-evaluation private brand is a separate, still-open concern).
    [Fact]
    public void ClassStringEvaluatedTwiceViaDirectEvalAccessesPrivateStatic()
        => Assert.Equal("test262|test262", Eval(
            "let src = `(class { static #m = 'test262'; static access() { return this.#m; } })`;"
            + "let make = function () { return eval(src); };"
            + "let C1 = make(); let C2 = make();"
            + "C1.access() + '|' + C2.access();"));

    // ---- Problem 3: super[Expression] evaluation order ----

    // `super[key]` evaluates the key Expression BEFORE reading the super base
    // (MakeSuperPropertyReference: evaluate key, then GetSuperBase). With super resolved
    // dynamically, a key side effect that nulls the home prototype must be observed by
    // the super-base read, so the access throws a TypeError.
    private const string OrderingSetup =
        "class base { constructor(){} method(){ this.mc++; } }"
        + "class derived extends base { constructor(){ super(); this.mc = 0; }"
        + "  testElem(){ super[ruin()]; }"
        + "  testElemAssign(){ super['p'] = ruin(); } }"
        + "function ruin(){ Object.setPrototypeOf(derived.prototype, null); return 5; }"
        + "function reset(){ Object.setPrototypeOf(derived.prototype, base.prototype); }"
        + "var instance = new derived();";

    [Fact]
    public void SuperComputedReadEvaluatesKeyBeforeSuperBase()
        => Assert.Equal("TypeError", Eval(
            OrderingSetup + "(function(){ try { instance.testElem(); return 'no throw'; } catch (e) { return e.constructor.name; } })();"));

    // The assignment target path still works: the (literal) key and super base are read,
    // then the RHS runs; the write lands on `this`.
    [Fact]
    public void SuperComputedAssignTargetStillWrites()
        => Assert.Equal("5", Eval(OrderingSetup + "instance.testElemAssign(); reset(); '' + instance.p;"));

    [Fact]
    public void SuperComputedMethodCallReads()
        => Assert.Equal("7", Eval(
            "class B { m(){ return this.v; } } class D extends B { constructor(){ super(); this.v = 7; } g(){ return super['m'](); } } '' + new D().g();"));

    [Fact]
    public void SuperComputedCompoundAssign()
        => Assert.Equal("15", Eval(
            "class B { constructor(){ this.n = 10; } } class D extends B { constructor(){ super(); } t(){ let k = 'n'; super[k] += 5; return this.n; } } '' + new D().t();"));

    // ---- Problem 4: Intl.DateTimeFormat format / formatToParts / formatRange ----

    [Fact]
    public void DateTimeFormatBasicEnUs()
        => Assert.Equal("1/3/2019", Eval("new Intl.DateTimeFormat('en-US').format(new Date('2019-01-03T00:00:00'));"));

    [Fact]
    public void DateTimeFormatShortMonthComponents()
        => Assert.Equal("Jan 3, 2019", Eval(
            "new Intl.DateTimeFormat('en-US', { year: 'numeric', month: 'short', day: 'numeric' }).format(new Date('2019-01-03T00:00:00'));"));

    // formatRange collapses shared higher-order fields: same month/year, differing day.
    [Fact]
    public void DateTimeFormatRangeDayDifference()
        => Assert.Equal("Jan 3 – 5, 2019", Eval(
            "var f = new Intl.DateTimeFormat('en-US', { year: 'numeric', month: 'short', day: 'numeric' });"
            + "f.formatRange(new Date('2019-01-03T00:00:00'), new Date('2019-01-05T00:00:00'));"));

    // Differing month: month+day repeat per range, the year stays shared.
    [Fact]
    public void DateTimeFormatRangeMonthDifference()
        => Assert.Equal("Jan 3 – Mar 4, 2019", Eval(
            "var f = new Intl.DateTimeFormat('en-US', { year: 'numeric', month: 'short', day: 'numeric' });"
            + "f.formatRange(new Date('2019-01-03T00:00:00'), new Date('2019-03-04T00:00:00'));"));

    // Identical endpoints render once (no range separator).
    [Fact]
    public void DateTimeFormatRangeSameDateSingle()
        => Assert.Equal("1/3/2019", Eval(
            "var d = new Date('2019-01-03T00:00:00'); new Intl.DateTimeFormat('en-US').formatRange(d, d);"));

    [Fact]
    public void DateTimeFormatRangeUndefinedThrowsTypeError()
        => Assert.Equal("TypeError", Caught(
            "new Intl.DateTimeFormat('en-US').formatRange(new Date(0), undefined);"));

    // formatToParts with timeStyle:short and a UTC-offset time zone beyond ±14h
    // (ECMAScript permits it; .NET DateTimeOffset does not).
    [Fact]
    public void DateTimeFormatToPartsOffsetTimeZone()
        => Assert.Equal("5:36", Eval(
            "var p = new Intl.DateTimeFormat('en', { timeZone: '+1412', timeStyle: 'short' }).formatToParts(new Date('1995-12-17T03:24:56Z'));"
            + "p.filter(t => t.type === 'hour')[0].value + ':' + p.filter(t => t.type === 'minute')[0].value;"));

    // Out-of-.NET-range but valid JS date still formats to a string.
    [Fact]
    public void DateTimeFormatHandlesExtremeDate()
        => Assert.Equal("string", Eval("typeof new Intl.DateTimeFormat('en-US').format(8.64e15);"));

    // ---- Problem 5: direct eval inside a switch must not clobber an outer global ----

    // A function defined via indirect eval that holds an outer global var, contains a
    // `switch`, and performs an inner direct eval: the inner eval's captured-binding
    // teardown must not overwrite the live global property with the captured binding's
    // stale field value. (Reads/writes of a global var flow through its property; the
    // binding's own field can lag behind.)
    [Fact]
    public void DirectEvalInSwitchKeepsOuterGlobalLive()
        => Assert.Equal("9", Eval(
            "var x = 17; var ev = eval;"
            + "var code = \"var x = 4; function a(c){ switch(c){ case 0: return x; case 1: x = 9; return; case 2: return eval('1'); } } a;\";"
            + "var f = ev(code); f(1); f(2); '' + f(0);"));

    // The outer-scope global also reflects the live value after the inner eval.
    [Fact]
    public void DirectEvalInSwitchOuterReadStaysLive()
        => Assert.Equal("9", Eval(
            "var x = 17; var ev = eval;"
            + "var code = \"var x = 4; function a(c){ switch(c){ case 0: return x; case 1: x = 9; return; case 2: return eval('1'); } } a;\";"
            + "var f = ev(code); f(1); f(2); f(0); '' + x;"));
}
