using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/695
//
// Fixed here:
//
// Problem 3 — Intl.NumberFormat rejected the "negative" signDisplay option with a
//   RangeError ("Invalid signDisplay option"). The "negative" value was added by
//   the Intl.NumberFormat v3 proposal and is part of the ECMA-402 GetOption list
//   « "auto", "never", "always", "exceptZero", "negative" », but the runtime's
//   SignDisplayValues table omitted it. So the whole
//   intl402/NumberFormat/prototype/format/signDisplay-negative-* family threw
//   during construction (GetOption) before any formatting assertion ran. The value
//   is now accepted and round-trips through resolvedOptions().
//
// Problem 10 — the multi-argument Date setters (setHours, setMinutes, setSeconds,
//   setMonth and their UTC counterparts) coerced their first argument twice. The
//   IsValid helper performs ToNumber on the first argument (reading [[DateValue]]
//   before coercion, per spec), but the setters then ignored that coerced value and
//   re-read .IntValue on the same argument — invoking its valueOf a second time. The
//   built-ins/Date/prototype/set*/date-value-read-before-tonumber-when-date-is-valid
//   tests assert valueOf is called exactly once. The setters now reuse the already
//   coerced value for the first slot.
//
// Problem 8 (subset) — two ClassBody early errors were not enforced, so several
//   staging/sm class-syntax tests saw no SyntaxError where one was required:
//     * a class with more than one `constructor` element;
//     * duplicate PrivateBoundIdentifiers — the same #name used by more than one
//       element, except a single getter/setter pair of matching static placement.
//   SyntaxValidation now reports both at parse time.
//
// Problem 8 (subset) — an object literal with more than one `__proto__: value`
//   data property (the colon PropertyDefinition form) is an early SyntaxError per
//   Annex B.3.1. It was accepted silently. Shorthand, methods, accessors and
//   computed `__proto__` keys remain exempt, and a single `__proto__:` still
//   performs prototype mutation.
//
// Problem 9 (root cause) — a function-local `var` whose name collided with a
//   binding in an enclosing function or the global scope was never registered in
//   the function's own scope: the parser's var-hoisting dedup search climbed past
//   the function boundary and treated the outer binding as satisfying the
//   declaration. As a result the inner `var` resolved to the outer binding —
//   including reads before its own declaration, which must instead see the
//   hoisted `undefined`. The search now stops at the function boundary. This also
//   fixes the `with` / @@unscopables family (unscopables-with), whose failures
//   were a downstream symptom: once the local `var` is properly hoisted, a read
//   inside a `with` that is blocked by @@unscopables resolves to it rather than
//   leaking to the same-named global.
//
// Problem 4 / Problem 7 — a B.3.4 `if`-clause FunctionDeclaration in direct eval
//   (no braces, e.g. `eval('… if (true) function f(){} …')`) mishandled the
//   Annex B var binding:
//     * Problem 7: when the eval body has a same-named lexical binding
//       (`let f = 123`), Annex B hoisting must be suppressed; the engine instead
//       wrote the function to the lexical binding. The if-clause function is now
//       block-scoped (via the implicit-block path) when its name is a genuine
//       top-level lexical of the eval body, leaving the lexical binding intact.
//     * Problem 4: when a same-named global already exists (`var f='x'`), the var
//       binding must be left in place (CreateGlobalVarBinding), so a read before
//       the declaration observes the existing value. The eval-root binding now
//       initialises from the current global value rather than `undefined`.
public class Issue695Tests
{
    private static JSValue Eval(string code)
    {
        using var ctx = new JSContext(experimentalFeatures: JavaScriptFeatureFlags.AllExperimentalEs2026);
        return ctx.Eval(code);
    }

    // Compile/run `source` via eval and report the thrown error's constructor name,
    // or "ok" when it completes without throwing.
    private static string EvalCatch(string source)
        => Eval("var r; try { eval(" + System.Text.Json.JsonSerializer.Serialize(source)
            + "); r = 'ok'; } catch (e) { r = e.constructor.name; } r;").ToString();

    // ---- Problem 3: signDisplay accepts the "negative" value ----

    // Constructing with signDisplay:"negative" no longer throws "Invalid signDisplay
    // option" — every spec-listed signDisplay value is accepted.
    [Theory]
    [InlineData("auto")]
    [InlineData("never")]
    [InlineData("always")]
    [InlineData("exceptZero")]
    [InlineData("negative")]
    public void SignDisplayValueIsAccepted(string value)
    {
        var code =
            "var t; try { new Intl.NumberFormat('en', { signDisplay: '" + value + "' });" +
            " t = 'ok'; } catch (e) { t = e.constructor.name + ':' + e.message; } t;";
        Assert.Equal("ok", Eval(code).ToString());
    }

    // The resolved value is reflected back by resolvedOptions().
    [Fact]
    public void SignDisplayNegativeRoundTripsThroughResolvedOptions()
        => Assert.Equal("negative", Eval(
            "new Intl.NumberFormat('en', { signDisplay: 'negative' }).resolvedOptions().signDisplay;").ToString());

    // An unknown signDisplay value is still a RangeError.
    [Fact]
    public void UnknownSignDisplayValueStillThrows()
        => Assert.Equal("RangeError", Eval(
            "var t; try { new Intl.NumberFormat('en', { signDisplay: 'sometimes' }); t = 'no throw'; }" +
            " catch (e) { t = e.constructor.name; } t;").ToString());

    // ---- Problem 10: Date multi-arg setters coerce the first argument once ----

    // On a valid date, the first argument's valueOf must run exactly once, and the
    // setter must still apply the coerced value (here 5) to the relevant component.
    [Theory]
    [InlineData("setHours", "getHours")]
    [InlineData("setMinutes", "getMinutes")]
    [InlineData("setSeconds", "getSeconds")]
    [InlineData("setMonth", "getMonth")]
    [InlineData("setUTCMinutes", "getUTCMinutes")]
    [InlineData("setUTCSeconds", "getUTCSeconds")]
    [InlineData("setUTCMonth", "getUTCMonth")]
    public void MultiArgSetterCoercesFirstArgumentOnce(string setter, string getter)
    {
        var code =
            "var calls = 0;" +
            "var arg = { valueOf: function () { calls++; return 5; } };" +
            "var d = new Date(2020, 0, 15, 10, 20, 30, 40);" +
            "d." + setter + "(arg);" +
            "calls + '|' + d." + getter + "();";
        Assert.Equal("1|5", Eval(code).ToString());
    }

    // The single-argument setters were already correct; guard against a regression.
    [Theory]
    [InlineData("setDate", "getDate")]
    [InlineData("setMilliseconds", "getMilliseconds")]
    [InlineData("setUTCDate", "getUTCDate")]
    public void SingleArgSetterCoercesArgumentOnce(string setter, string getter)
    {
        var code =
            "var calls = 0;" +
            "var arg = { valueOf: function () { calls++; return 7; } };" +
            "var d = new Date(2020, 0, 15, 10, 20, 30, 40);" +
            "d." + setter + "(arg);" +
            "calls + '|' + d." + getter + "();";
        Assert.Equal("1|7", Eval(code).ToString());
    }

    // ---- Problem 8: ClassBody early errors (constructor / private name) ----

    // Class shapes that must be rejected with a SyntaxError at parse time.
    [Theory]
    [InlineData("class C { constructor(){} constructor(){} }")] // two constructors
    [InlineData("(class { #x; #x; })")]                          // two private fields
    [InlineData("(class { #x; #x(){} })")]                       // field + method
    [InlineData("(class { get #x(){} get #x(){} })")]            // two getters
    [InlineData("(class { set #x(v){} set #x(v){} })")]          // two setters
    [InlineData("(class { #x; get #x(){} })")]                   // field + accessor
    [InlineData("(class { get #x(){} static set #x(v){} })")]    // get/set, mismatched placement
    [InlineData("(class { #x; static #x; })")]                   // instance + static field
    public void IllegalClassBodyThrowsSyntaxError(string source)
        => Assert.Equal("SyntaxError", EvalCatch(source));

    // Class shapes that remain legal and must keep compiling.
    [Theory]
    [InlineData("(class { get #x(){} set #x(v){} })")]            // instance accessor pair
    [InlineData("(class { static get #x(){} static set #x(v){} })")] // static accessor pair
    [InlineData("(class { #x; #y; m(){} static m(){} })")]       // distinct names
    [InlineData("class C { m(){} m(){} }")]                       // public duplicate methods are allowed
    [InlineData("class C { ['constructor'](){} constructor(){} }")] // computed key is not the constructor
    public void LegalClassBodyStillCompiles(string source)
        => Assert.Equal("ok", EvalCatch(source));

    // ---- Problem 8: duplicate __proto__ data properties in object literals ----

    [Theory]
    [InlineData("({ __proto__: 1, __proto__: 2 })")]
    [InlineData("({ '__proto__': 1, __proto__: 2 })")]
    [InlineData("({ __proto__: 1, x: 0, __proto__: 2 })")]
    public void DuplicateProtoSetterThrowsSyntaxError(string source)
        => Assert.Equal("SyntaxError", EvalCatch(source));

    [Theory]
    [InlineData("({ __proto__: 1, ['__proto__']: 2 })")]  // one computed
    [InlineData("({ __proto__: 1, __proto__() {} })")]    // one method
    [InlineData("({ __proto__: 1, get __proto__() {} })")] // one accessor
    [InlineData("({ x: 1, x: 2 })")]                       // non-proto duplicates allowed
    public void NonDuplicateProtoStillCompiles(string source)
        => Assert.Equal("ok", EvalCatch(source));

    // A single `__proto__:` still mutates the prototype; shorthand is an own property.
    [Fact]
    public void SingleProtoSetterMutatesPrototype()
        => Assert.Equal("1|true", Eval(
            "var p = { a: 1 }; var o = { __proto__: p }; o.a + '|' + (Object.getPrototypeOf(o) === p);").ToString());

    [Fact]
    public void ProtoShorthandIsOwnProperty()
        => Assert.Equal("true|5", Eval(
            "var __proto__ = 5; var o = { __proto__ }; o.hasOwnProperty('__proto__') + '|' + o.__proto__;").ToString());

    // ---- Problem 9: function-local var hoisting shadows same-named outer bindings ----

    // A function-local `var` read before its own declaration sees the hoisted
    // `undefined`, even when an outer `var`/`let` of the same name exists.
    [Theory]
    [InlineData("var x = 9; function h(){ var got = x; var x = 5; return got; } h();")]
    [InlineData("let x = 9; function h(){ var got = x; var x = 5; return got; } h();")]
    [InlineData("var x=9; function a(){ function b(){ var got=x; var x=1; return got; } return b(); } a();")]
    public void LocalVarShadowsOuterBindingBeforeDeclaration(string code)
        => Assert.Equal("undefined", Eval(code).ToString());

    // Writing the function-local `var` must not leak to the same-named global.
    [Fact]
    public void LocalVarDoesNotLeakToGlobal()
        => Assert.Equal("5|9", Eval(
            "var x = 9; function h(){ var x = 5; return x; } var inner = h(); inner + '|' + x;").ToString());

    // A `var` whose name matches a parameter is the same binding, not a new one.
    [Fact]
    public void VarDedupesWithParameter()
        => Assert.Equal("5|7", Eval(
            "function h(a){ var keep = a; var a = 7; return keep + '|' + a; } h(5);").ToString());

    // The original unscopables-with shape: a read inside a `with` blocked by
    // @@unscopables resolves to the hoisted function-local var, not the global.
    [Fact]
    public void UnscopablesBlockedReadResolvesToHoistedLocal()
        => Assert.Equal("undefined", Eval(
            "var v = 1; globalThis[Symbol.unscopables] = { v: true };" +
            "function f(){ var r; with (globalThis) { r = typeof v; } var v = 2; return r; }" +
            "f();").ToString());

    // ---- Problem 7: lexical binding suppresses annexB hoist of an eval if-clause fn ----

    // `if (true) function f(){}` in direct eval must not overwrite a same-named
    // lexical (let/const) binding of the eval body; the lexical value is preserved.
    [Theory]
    [InlineData("let f = 123;")]
    [InlineData("const f = 123;")]
    public void EvalIfClauseFnDoesNotClobberLexical(string decl)
    {
        var code = "var init, after; eval('" + decl
            + " init = f; if (true) function f() {} after = f;'); init + '/' + (typeof after);";
        Assert.Equal("123/number", Eval(code).ToString());
    }

    // The same inside a function-scoped direct eval.
    [Fact]
    public void EvalIfClauseFnDoesNotClobberLexicalInFunction()
        => Assert.Equal("123/number", Eval(
            "function g(){ var init, after;" +
            "eval('let f = 123; init = f; if (true) function f() {} after = f;');" +
            "return init + '/' + (typeof after); } g();").ToString());

    // A non-lexical name (a sibling block-scoped function's annexB var binding) must
    // still be updated by the if-clause function (last declaration wins).
    [Fact]
    public void EvalIfClauseFnUpdatesNonLexicalVarBinding()
        => Assert.Equal("2", Eval(
            "(function () { var updated;" +
            "eval('{ function h(){return 1;} } if (true) function h(){return 2;} else function _h(){} updated = h;');" +
            "return updated(); }())").ToString());

    // ---- Problem 4: existing global binding left in place by annexB hoisting ----

    // A read before the if-clause FunctionDeclaration observes the existing global
    // value (the binding is not reinitialized to undefined), and afterwards the
    // binding is the function.
    [Fact]
    public void EvalIfClauseFnLeavesExistingGlobalBinding()
        => Assert.Equal("x", Eval(
            "var f = 'x'; eval('var probe = f; if (true) function f() {} globalThis.__p = probe;'); globalThis.__p;").ToString());

    [Fact]
    public void EvalIfClauseFnLeavesNonConfigurableGlobalBinding()
        => Assert.Equal("x", Eval(
            "Object.defineProperty(globalThis, 'f', { value: 'x', enumerable: true, writable: true, configurable: false });" +
            "eval('var probe = f; if (true) function f() {} globalThis.__p = probe;'); globalThis.__p;").ToString());

    [Fact]
    public void EvalIfClauseFnFinalValueIsFunction()
        => Assert.Equal("function", Eval(
            "var f = 'x'; eval('if (true) function f() {}'); typeof f;").ToString());

    // An eval-created global function from an if-clause remains deletable.
    [Fact]
    public void EvalIfClauseGlobalFunctionIsDeletable()
        => Assert.Equal("true|undefined", Eval(
            "eval('if (true) function delme(){}'); (delete delme) + '|' + (typeof delme);").ToString());
}
