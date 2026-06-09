using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/719
//
// Fixed here:
//
//   Problem 2 (async function with a nested lambda that captures a variable) — the
//   reported failure was the with/@@unscopables logic, but that is already correct
//   (the equivalent SYNC function works). The real bug: an async function's body is
//   pre-rewritten in isolation, and that pass descended into nested lambdas and
//   finalised their closure repositories against an incomplete scope chain, so a
//   nested IIFE/arrow capturing an outer variable mis-compiled (KeyNotFoundException
//   at IL-gen, or a dropped capture). The async pre-rewrite now rewrites only the
//   function's own body (RewriteRootOnly); nested lambdas are threaded by the later
//   full top-down rewrite.
//
//   Problem 3 (reading a private accessor with only a setter) — must throw TypeError
//   (PrivateGet of an accessor without a getter), not return undefined. Gated on the
//   private-name marker so public getterless accessors still yield undefined.
//
//   Problem 4 (sloppy for-in/for-of with an undeclared identifier head) — must
//   create/assign the global like `x = key`, not throw "x is not defined". A free
//   identifier head is now routed through the per-iteration assignment path.
//
//   Problem 8 (Function.prototype.toString line-terminator normalisation) — a
//   function whose source was written with CR or CRLF line endings must toString
//   with LF (U+000A) separators. toString now normalises CRLF and CR to LF.
//
//   Problem 9 (Date setMonth/setFullYear/setUTCMonth day overflow) — the setters
//   applied the day offset before the month offset, so .NET's AddMonths clamped the
//   day-of-month to the target month's last valid day. setMonth(5, 31) wrongly
//   produced Jun 30 instead of Jul 1. The month offset is now applied first (per
//   ECMAScript MakeDay), so day overflow rolls into the following month.
//
//   Problem 10 (Number.NaN / POSITIVE_INFINITY / NEGATIVE_INFINITY descriptor) —
//   these were defined as accessor properties (mutable C# static fields), so
//   getOwnPropertyDescriptor reported writable: undefined and configurable: true.
//   They are now read-only data properties (writable/enumerable/configurable all
//   false), matching the spec.
//
// Out of scope (feature/architectural): P1 DateTimeFormat "de" CLDR date pipeline,
// P5 sm RegExp unicode-mode surrogate matching, P6 duplicate named capture groups
// (.NET merges same-named groups), P7 NumberFormat compact-decimal CLDR. P4's other
// two files (indirect-eval / with-closures) remain eval var-env.
public class Issue719Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // ---- Problem 9: Date setter day-overflow rolls into next month ----

    [Fact]
    public void SetMonthDayOverflowRollsIntoNextMonth()
        => Assert.Equal("2016,6,1",
            Eval("var d = new Date(2016, 0, 15, 10, 30); d.setMonth(5, 31); [d.getFullYear(), d.getMonth(), d.getDate()].join(',')"));

    [Fact]
    public void SetFullYearDayOverflowRollsIntoNextMonth()
        => Assert.Equal("2016,6,1",
            Eval("var d = new Date(2016, 0, 15); d.setFullYear(2016, 5, 31); [d.getFullYear(), d.getMonth(), d.getDate()].join(',')"));

    [Fact]
    public void SetUTCMonthDayOverflowRollsIntoNextMonth()
        => Assert.Equal("2016,6,1",
            Eval("var d = new Date(Date.UTC(2016, 0, 15)); d.setUTCMonth(5, 31); [d.getUTCFullYear(), d.getUTCMonth(), d.getUTCDate()].join(',')"));

    [Fact]
    public void SetMonthNoOverflowStillWorks()
        => Assert.Equal("2016,6,15",
            Eval("var d = new Date(2016, 0, 15); d.setMonth(6, 15); [d.getFullYear(), d.getMonth(), d.getDate()].join(',')"));

    // ---- Problem 10: Number constants are read-only data properties ----

    [Fact]
    public void NumberNaNIsReadonlyDataProperty()
        => Assert.Equal("false,false,false,false,false",
            Eval("var d = Object.getOwnPropertyDescriptor(Number, 'NaN'); [d.writable, d.enumerable, d.configurable, ('get' in d), ('set' in d)].join(',')"));

    [Fact]
    public void NumberPositiveInfinityIsReadonlyDataProperty()
        => Assert.Equal("false,false,false",
            Eval("var d = Object.getOwnPropertyDescriptor(Number, 'POSITIVE_INFINITY'); [d.writable, d.enumerable, d.configurable].join(',')"));

    [Fact]
    public void NumberNegativeInfinityIsReadonlyDataProperty()
        => Assert.Equal("false,false,false",
            Eval("var d = Object.getOwnPropertyDescriptor(Number, 'NEGATIVE_INFINITY'); [d.writable, d.enumerable, d.configurable].join(',')"));

    [Fact]
    public void NumberNaNStillReadsCorrectValue()
        => Assert.Equal("true", Eval("Number.isNaN(Number.NaN) + ''"));

    // ---- Problem 2: async function with a nested lambda that captures a variable ----
    // An async function body runs synchronously up to the first await. A nested
    // IIFE/arrow/function inside that body that captures an outer variable used to
    // mis-compile (KeyNotFoundException at IL-gen, or a silently dropped capture)
    // because the async pre-rewrite finalized the nested lambda's closures against an
    // incomplete scope. Now nested lambdas are threaded by the full top-down rewrite.

    [Fact]
    public void AsyncNestedFunctionCapturesGlobal()
        => Assert.Equal("ran",
            Eval("var out='no'; async function f(){ (function(){ out='ran'; })(); } f(); out"));

    [Fact]
    public void AsyncNestedFunctionCapturesLocal()
        => Assert.Equal("5",
            Eval("var out='no'; async function f(){ var y=5; (function(){ out=y; })(); } f(); out+''"));

    [Fact]
    public void AsyncNestedFunctionCapturesParameter()
        => Assert.Equal("9",
            Eval("var out='no'; async function f(p){ (function(){ out=p; })(); } f(9); out+''"));

    [Fact]
    public void AsyncNestedArrowCapturesLocal()
        => Assert.Equal("7",
            Eval("var out='no'; async function f(){ var y=7; (()=>{ out=y; })(); } f(); out+''"));

    [Fact]
    public void AsyncDirectGlobalWriteStillWorks()
        => Assert.Equal("1", Eval("var x=0; async function f(){ x=1; } f(); x+''"));

    [Fact]
    public void AsyncArrowOuterWithNestedCapture()
        => Assert.Equal("ran",
            Eval("var out='no'; var f = async () => { (function(){ out='ran'; })(); }; f(); out"));

    [Fact]
    public void AsyncMethodWithNestedCapture()
        => Assert.Equal("6",
            Eval("var out='no'; var o = { async m(){ var y=6; (function(){ out=y; })(); } }; o.m(); out+''"));

    [Fact]
    public void AsyncNestedCaptureMutationIsShared()
        => Assert.Equal("inner",
            Eval("var box='outer'; async function f(){ var v='start'; (function(){ v='inner'; })(); box=v; } f(); box"));

    // The exact #719 P2 scenario: ref() is async but has no await, so its body — two
    // IIFEs with `with`/@@unscopables — runs synchronously; count must reach 6.
    [Fact]
    public void AsyncWithUnscopablesInNestedFnCountIsSix()
        => Assert.Equal("6", Eval(@"
var count=0; var v=1; globalThis[Symbol.unscopables]={v:true};
async function ref(x){
  (function(){ count++; with(globalThis){ count++; if(v!==1) throw new Error('v!=1'); } })();
  (function(){ count++; var v=x; with(globalThis){ count++; if(v!==10) throw new Error('v!=10'); v=20; } if(v!==20) throw new Error('v!=20'); })();
}
{ count++; ref(10); count++; }
count+''"));

    // ---- Problem 3: reading a private accessor without a getter throws TypeError ----

    private const string PrivateShadowClasses = @"
class C {
  get #m() { return 'outer class'; }
  method() { return this.#m; }
  B = class {
    method(o) { return o.#m; }
    set #m(v) { this._v = v; }
  }
}
var c = new C();
var innerB = new c.B();
";

    [Fact]
    public void PrivateAccessorWithoutGetterThrowsOnRead()
        => Assert.Equal("TypeError", Eval(PrivateShadowClasses +
            "try { innerB.method(innerB); 'no throw' } catch (e) { e.constructor.name }"));

    [Fact]
    public void PrivateGetterOnDeclaringInstanceStillReads()
        => Assert.Equal("outer class", Eval(PrivateShadowClasses + "c.method()"));

    [Fact]
    public void PrivateAccessorBrandCheckAcrossNestedClasses()
        => Assert.Equal("TypeError", Eval(PrivateShadowClasses +
            "try { innerB.method(c); 'no throw' } catch (e) { e.constructor.name }"));

    private const string PrivateMethodShadowClasses = @"
class C {
  #m() { return 'outer class'; }
  method() { return this.#m(); }
  B = class {
    method(o) { return o.#m; }
    set #m(v) { this._v = v; }
  }
}
var c = new C();
var innerB = new c.B();
";

    [Fact]
    public void PrivateMethodShadowedBySetterThrowsOnRead()
        => Assert.Equal("TypeError", Eval(PrivateMethodShadowClasses +
            "try { innerB.method(innerB); 'no throw' } catch (e) { e.constructor.name }"));

    [Fact]
    public void PrivateMethodOnDeclaringInstanceStillCalls()
        => Assert.Equal("outer class", Eval(PrivateMethodShadowClasses + "c.method()"));

    [Fact]
    public void PrivateMethodShadowBrandCheckAcrossNestedClasses()
        => Assert.Equal("TypeError", Eval(PrivateMethodShadowClasses +
            "try { innerB.method(c); 'no throw' } catch (e) { e.constructor.name }"));

    [Fact]
    public void PrivateAccessorWithGetterStillReads()
        => Assert.Equal("42", Eval(
            "class D { get #x() { return 42; } read() { return this.#x; } } new D().read() + ''"));

    // ---- Problem 4: for-in / for-of with an undeclared (sloppy global) head ----

    [Fact]
    public void ForInUndeclaredIdentifierCreatesGlobal()
        => Assert.Equal("a,b",
            Eval("var out = []; for (k in { a: 1, b: 2 }) out.push(k); out.join(',')"));

    [Fact]
    public void ForInUndeclaredOverObjectWithNoEnumerableKeys()
        => Assert.Equal("ok", Eval("for (k in Boolean) {} 'ok'"));

    [Fact]
    public void ForOfUndeclaredIdentifierCreatesGlobal()
        => Assert.Equal("1,2,3",
            Eval("var out = []; for (k of [1, 2, 3]) out.push(k); out.join(',')"));

    [Fact]
    public void ForInDeclaredVariableStillWorks()
        => Assert.Equal("a,b",
            Eval("var out = []; var k; for (k in { a: 1, b: 2 }) out.push(k); out.join(',')"));

    [Fact]
    public void StrictForInUndeclaredIdentifierThrowsReferenceError()
        => Assert.Equal("ReferenceError",
            Eval("'use strict'; try { for (k in { a: 1 }) {} 'no throw' } catch (e) { e.constructor.name }"));

    // ---- Problem 8: Function.prototype.toString normalises CR / CRLF to LF ----

    [Fact]
    public void FunctionToStringNormalisesCrlfToLf()
    {
        using var ctx = new JSContext();
        ctx.Eval("var f = function a(\r\nx\r\n){\r\n;\r\n};");
        var s = ctx.Eval("f.toString()").ToString();
        Assert.DoesNotContain("\r", s);
        Assert.Contains("\n", s);
    }

    [Fact]
    public void FunctionToStringNormalisesLoneCrToLf()
    {
        using var ctx = new JSContext();
        ctx.Eval("var f = function a(\rx\r){\r;\r};");
        var s = ctx.Eval("f.toString()").ToString();
        Assert.DoesNotContain("\r", s);
        Assert.Contains("\n", s);
    }

    // Mirrors the exact test262 line-terminator-normalisation source shape (named
    // function expression with interleaved comments) to confirm the full source span
    // — including the leading `function` and comments — is captured verbatim.
    [Fact]
    public void FunctionToStringCapturesFullSourceWithComments()
    {
        const string expected = "function\n// a\nf\n// b\n(\n// c\nx\n// d\n,\n// e\ny\n// f\n)\n// g\n{\n// h\n;\n// i\n;\n// j\n}";
        using var ctx = new JSContext();
        // CRLF source must normalise to the LF expected.
        var src = ("var f = " + expected + "\n;").Replace("\n", "\r\n");
        ctx.Eval(src);
        Assert.Equal(expected, ctx.Eval("f.toString()").ToString());
    }
}
