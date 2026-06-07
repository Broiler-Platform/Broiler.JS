using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/697
//
// Fixed here:
//
// Problem 8 — encodeURI / encodeURIComponent did not raise URIError on an
//   unpaired surrogate code unit. They delegated to .NET's Uri.EscapeUriString /
//   Uri.EscapeDataString, which silently re-encode (or drop) lone surrogates and
//   never throw, and whose unescaped-character sets do not match the spec's. The
//   abstract Encode operation (§19.2.6.5) is now implemented directly: code units
//   in the function-specific unescaped set are copied verbatim, everything else is
//   UTF-8 encoded and percent-escaped, and an unpaired high or low surrogate is a
//   URIError. (encodeURI/S15.1.3.3_A1.2/A1.3 and the encodeURIComponent siblings.)
//
// Problem 10 (subset) — RegExp.prototype[Symbol.matchAll] read the source
//   regexp's flags by inspecting each individual flag accessor (hasIndices,
//   global, …) instead of `Get(R, "flags")`, and copied `lastIndex` without
//   ToLength coercion. So a `flags` getter, a `flags` value whose toString throws,
//   or a `lastIndex` valueOf that throws never fired — the abrupt completion the
//   spec requires (steps 5 and 7) was lost. The JSRegExp fast path now reads
//   `R.flags` (ToString) and `R.lastIndex` (ToLength) observably.
//
// Problem 6 — a `with` statement reused the direct-eval overlay machinery to
//   keep in-scope locals resolvable inside the body. For a function-local `var`
//   that shadows a same-named global (with the global blocked by @@unscopables),
//   that overlay leaked: an assignment to the shadowed name wrote through to the
//   global-object property (and teardown propagated the local's value back to the
//   global binding), so `globalThis.v` became the inner value. Two parts: (1) the
//   `with` fallback now captures only *function-owned* bindings (a program-level
//   global var is left to the normal dual-binding path) and installs them as a
//   *shadowing* overlay that never publishes to / propagates back to the global
//   object; (2) `typeof` of a dynamic name now resolves through the overlay
//   (ResolveIdentifierOrUndefined) rather than reading the bare global property.
public class Issue697Tests
{
    private static JSValue Eval(string code)
    {
        using var ctx = new JSContext(experimentalFeatures: JavaScriptFeatureFlags.AllExperimentalEs2026);
        return ctx.Eval(code);
    }

    // Run `source`, reporting the thrown error's constructor name or "ok".
    private static string Catch(string source)
        => Eval("var r; try { " + source + " r = 'ok'; } catch (e) { r = e.constructor.name; } r;").ToString();

    // ---- Problem 8: encodeURI/encodeURIComponent reject unpaired surrogates ----

    [Theory]
    [InlineData("encodeURI('\\uD800')")]            // lone high surrogate
    [InlineData("encodeURI('\\uDC00')")]            // lone low surrogate
    [InlineData("encodeURI('\\uD800\\u0041')")]     // high not followed by low
    [InlineData("encodeURIComponent('a\\uD834b')")] // lone high surrogate, mid-string
    [InlineData("encodeURIComponent('\\uDFFF')")]   // lone low surrogate
    public void EncodeRejectsUnpairedSurrogate(string expr)
        => Assert.Equal("URIError", Catch(expr + ";"));

    // A valid surrogate pair is UTF-8 encoded, not rejected.
    [Fact]
    public void EncodeURIEncodesValidSurrogatePair()
        => Assert.Equal("%F0%9F%98%80", Eval("encodeURI('\\uD83D\\uDE00');").ToString());

    // encodeURI leaves the reserved/unescaped marks intact.
    [Fact]
    public void EncodeURIPreservesReservedCharacters()
        => Assert.Equal("http://a.b/c%20d?e=1#f", Eval("encodeURI('http://a.b/c d?e=1#f');").ToString());

    // encodeURIComponent escapes the reserved characters that encodeURI keeps.
    [Fact]
    public void EncodeURIComponentEscapesReserved()
        => Assert.Equal("a%20b%2Fc%3Fd%3D1", Eval("encodeURIComponent('a b/c?d=1');").ToString());

    // ---- Problem 10: matchAll propagates abrupt completions ----

    // A throwing `flags` getter on the receiver propagates.
    [Fact]
    public void MatchAllPropagatesFlagsGetterThrow()
        => Assert.Equal("Error", Catch(
            "var re = /./; Object.defineProperty(re, 'flags', { get() { throw new Error(); } });" +
            " re[Symbol.matchAll]('');"));

    // A `flags` value whose toString throws propagates (valueOf must not be called).
    [Fact]
    public void MatchAllPropagatesFlagsToStringThrow()
        => Assert.Equal("Error", Catch(
            "var re = /\\w/; Object.defineProperty(re, 'flags', { value: { toString() { throw new Error(); } } });" +
            " re[Symbol.matchAll]('');"));

    // A `lastIndex` whose valueOf throws propagates (ToLength coercion).
    [Fact]
    public void MatchAllPropagatesLastIndexValueOfThrow()
        => Assert.Equal("Error", Catch(
            "var re = /./; re.lastIndex = { valueOf() { throw new Error(); } };" +
            " re[Symbol.matchAll]('');"));

    // Ordinary matchAll still yields the expected matches.
    [Fact]
    public void MatchAllStillIterates()
        => Assert.Equal("a:0,a:1", Eval(
            "var r = []; for (var m of 'aabb'.matchAll(/(a)/g)) r.push(m[0] + ':' + m.index); r.join(',');").ToString());

    // ---- Problem 6: `with` + @@unscopables write isolation ----

    // A write to a function-local `var` that is shadowed (and @@unscopables-blocked
    // in the with object) stays local; the same-named global is untouched.
    [Fact]
    public void WithUnscopablesWriteStaysLocal()
        => Assert.Equal("20|1", Eval(
            "var v = 1; globalThis[Symbol.unscopables] = { v: true };" +
            "function f(){ var v; with (globalThis) { v = 20; } return v; }" +
            "var local = f();" +
            "var out = local + '|' + globalThis.v; delete globalThis[Symbol.unscopables]; out;").ToString());

    // A blocked read resolves to the hoisted local (undefined), not the global.
    [Fact]
    public void WithUnscopablesReadResolvesToLocal()
        => Assert.Equal("undefined", Eval(
            "var v = 1; globalThis[Symbol.unscopables] = { v: true };" +
            "function f(){ var r; with (globalThis) { r = typeof v; } var v = 2; return r; }" +
            "var out = f(); delete globalThis[Symbol.unscopables]; out;").ToString());

    // A genuine global `var` written inside a global-scope `with` still updates the
    // global (and its property) — the shadowing isolation must not apply to it.
    [Fact]
    public void GlobalVarWriteInWithStillSyncs()
        => Assert.Equal("5|5", Eval(
            "var gg = 1; var o = {}; with (o) { gg = 5; } gg + '|' + globalThis.gg;").ToString());

    // A `with`-object property of a non-blocked name wins over the local, and the
    // local stays untouched.
    [Fact]
    public void WithObjectPropertyWinsOverLocal()
        => Assert.Equal("7|99", Eval(
            "(function(){ var k = 99; var o = { k: 1 }; with (o) { k = 7; } return o.k + '|' + k; })();").ToString());

    // ---- Problem 10 (subset): private field read on a primitive receiver ----

    // `this.#p` with a primitive `this` (set via .call) is a brand-check TypeError —
    // ToObject yields a fresh wrapper that has no private field. (Previously it
    // returned undefined.)
    [Theory]
    [InlineData("15")]
    [InlineData("'Test262'")]
    [InlineData("Symbol('x')")]
    [InlineData("10n")]
    [InlineData("true")]
    public void PrivateFieldReadOnPrimitiveReceiverThrows(string primitive)
        => Assert.Equal("TypeError", Eval(
            "var t; class C { #p = 1; m() { return this.#p; } }" +
            "try { C.prototype.m.call(" + primitive + "); t = 'no throw'; } catch (e) { t = e.constructor.name; } t;").ToString());

    // A genuine instance still reads its private field.
    [Fact]
    public void PrivateFieldReadOnInstanceStillWorks()
        => Assert.Equal("1", Eval("class C { #p = 1; m() { return this.#p; } } new C().m();").ToString());

    // ---- Problem 1 (subset): the class name is a const binding inside the body ----

    // Assigning to the class name from the constructor / a method / an accessor is
    // a TypeError (the inner binding is immutable). Covers declaration and
    // expression forms.
    [Theory]
    [InlineData("class C { constructor() { C = 42; } }; new C();")]
    [InlineData("new (class C { constructor() { C = 42; } });")]
    [InlineData("class C { m() { C = 42; } }; new C().m();")]
    [InlineData("new (class C { m() { C = 42; } }).m();")]
    [InlineData("class C { get x() { C = 42; } }; new C().x;")]
    [InlineData("class C { set x(_) { C = 42; } }; new C().x = 15;")]
    public void ClassNameIsConstInsideBody(string source)
        => Assert.Equal("TypeError", Catch(source));

    // The outer declaration binding stays mutable: reassigning the class name
    // *after* the declaration is allowed.
    [Fact]
    public void ClassDeclarationNameIsReassignableOutside()
        => Assert.Equal("99", Eval("class C {}; C = 99; C;").ToString());

    // The name is still readable inside the body (it resolves to the class).
    [Fact]
    public void ClassNameReadableInsideBody()
        => Assert.Equal("C", Eval("class C { m() { return C.name; } } new C().m();").ToString());

    // Static recursion through the class name still works.
    [Fact]
    public void ClassNameStaticRecursionWorks()
        => Assert.Equal("120", Eval("class F { static run(n) { return n <= 1 ? 1 : n * F.run(n - 1); } } F.run(5);").ToString());

    // A named class EXPRESSION does not leak its name to the enclosing scope.
    [Fact]
    public void NamedClassExpressionDoesNotLeakName()
        => Assert.Equal("undefined", Eval("(class C {}); typeof C;").ToString());

    // The expression's name is still usable inside the body (self-reference).
    [Fact]
    public void NamedClassExpressionNameVisibleInside()
        => Assert.Equal("E", Eval("var D = class E { who() { return E.name; } }; new D().who();").ToString());

    // A class DECLARATION still binds (and the binding is reassignable).
    [Fact]
    public void ClassDeclarationStillBindsName()
        => Assert.Equal("function", Eval("class Decl {} typeof Decl;").ToString());

    // ---- Problem 1 (subset): class heritage is strict mode code ----

    // A function expression in the heritage is strict, so its `arguments` /
    // `caller` are poison pills (TypeError on access).
    [Fact]
    public void ClassHeritageFunctionArgumentsIsPoisonPill()
        => Assert.Equal("TypeError", Catch(
            "var D = class extends function () {} {}; Object.getPrototypeOf(D).arguments;"));

    // Running that heritage function as the [[Construct]] target (via super)
    // executes its strict body, where `arguments.callee` throws.
    [Fact]
    public void ClassHeritageArgumentsCalleeThrows()
        => Assert.Equal("TypeError", Catch(
            "var D = class extends function () { arguments.callee; } {}; new D;"));

    // A class declaration is still in its temporal dead zone before the declaration.
    [Fact]
    public void ClassDeclarationTdzBeforeDeclaration()
        => Assert.Equal("ReferenceError", Eval(
            "var t; try { DZ; t = 'no throw'; } catch (e) { t = e.constructor.name; } class DZ {} t;").ToString());
}
