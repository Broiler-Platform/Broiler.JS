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
// Out of scope (architectural / CLDR / deep parser, matching the triage carried
// in #683 / #685 / #687 / #689 / #691, and confirmed by probing the engine for
// this issue): the private-* brand-check and double-initialisation families,
// super-*-reference-null, the proxy default-handler TypeError tests, AnnexB eval
// binding re-init / skip-early-err, scope-param-elem-var,
// computed-property-abrupt-completion, NumberFormat signDisplay "negative"
// currency CLDR formatting, and the staging/sm negative SyntaxError grab-bag.
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
}
