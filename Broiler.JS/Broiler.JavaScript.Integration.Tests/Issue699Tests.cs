using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/699
//
// Fixed here:
//
// Problem 8 — Object.prototype.toString did not recognise the [[ErrorData]]
//   internal slot, so `Object.prototype.toString.call(new TypeError)` returned
//   "[object Object]" instead of "[object Error]". The builtin tag is now computed
//   up front (Array / Error / Function / Object) and only a string-valued
//   @@toStringTag overrides it.
//
// Problem 9 — several operations dropped the sign of a zero result:
//   * Math.cbrt/asinh/atanh hand-rolled their formulas via Math.Pow/Math.Log, which
//     turn -0 into +0; they now use the sign-of-zero-correct .NET intrinsics.
//   * Math.expm1 returned +0 for an input of -0 (Math.Exp(-0)-1); ±0 (and NaN) now
//     pass through unchanged.
//   * Math.sumPrecise accumulated from +0 instead of the spec's -0, so an empty
//     list (or a list of only -0 values) produced +0.
//   * Map.prototype.getOrInsert / getOrInsertComputed did not apply
//     CanonicalizeKeyedCollectionKey, so a -0 key was surfaced to the callback and
//     stored as -0 instead of +0.
public class Issue699Tests
{
    private static JSValue Eval(string code)
    {
        using var ctx = new JSContext(experimentalFeatures: JavaScriptFeatureFlags.AllExperimentalEs2026);
        return ctx.Eval(code);
    }

    // Run `source`, reporting the thrown error's constructor name or "ok".
    private static string Catch(string source)
        => Eval("var r; try { " + source + " r = 'ok'; } catch (e) { r = e.constructor.name; } r;").ToString();

    // ---- Problem 8: Error objects tag as "[object Error]" ----

    [Theory]
    [InlineData("Error")]
    [InlineData("TypeError")]
    [InlineData("RangeError")]
    [InlineData("ReferenceError")]
    [InlineData("SyntaxError")]
    [InlineData("EvalError")]
    [InlineData("URIError")]
    public void ErrorObjects_Tag_As_Error(string ctor)
    {
        Assert.Equal("[object Error]",
            Eval($"Object.prototype.toString.call(new {ctor})").ToString());
    }

    [Fact]
    public void Plain_Object_And_Array_Still_Tag_Correctly()
    {
        Assert.Equal("[object Object]", Eval("Object.prototype.toString.call({})").ToString());
        Assert.Equal("[object Array]", Eval("Object.prototype.toString.call([])").ToString());
        Assert.Equal("[object Function]", Eval("Object.prototype.toString.call(function(){})").ToString());
    }

    [Fact]
    public void ToStringTag_Still_Overrides_Builtin_Tag()
    {
        // A string @@toStringTag wins even over the Error builtin tag.
        Assert.Equal("[object Custom]",
            Eval("var e = new Error; e[Symbol.toStringTag] = 'Custom'; Object.prototype.toString.call(e)").ToString());
    }

    // ---- Problem 9: sign-of-zero preservation ----

    [Theory]
    [InlineData("Math.cbrt(-0)")]
    [InlineData("Math.asinh(-0)")]
    [InlineData("Math.atanh(-0)")]
    [InlineData("Math.expm1(-0)")]
    public void Math_Preserves_Negative_Zero(string expr)
    {
        Assert.Equal("true", Eval($"Object.is({expr}, -0)").ToString());
    }

    [Theory]
    [InlineData("Math.cbrt(+0)")]
    [InlineData("Math.asinh(+0)")]
    [InlineData("Math.atanh(+0)")]
    [InlineData("Math.expm1(+0)")]
    public void Math_Preserves_Positive_Zero(string expr)
    {
        Assert.Equal("true", Eval($"Object.is({expr}, +0)").ToString());
    }

    [Theory]
    [InlineData("Math.cbrt(-8)", "-2")]
    [InlineData("Math.cbrt(27)", "3")]
    [InlineData("Math.atanh(-1)", "-Infinity")]
    [InlineData("Math.atanh(1)", "Infinity")]
    [InlineData("Math.atanh(2)", "NaN")]
    [InlineData("Math.expm1(-Infinity)", "-1")]
    [InlineData("Math.cbrt(-Infinity)", "-Infinity")]
    public void Math_Exact_Landmarks(string expr, string expected)
    {
        Assert.Equal(expected, Eval($"String({expr})").ToString());
    }

    [Theory]
    [InlineData("Math.sumPrecise([])", "true")]        // -0
    [InlineData("Math.sumPrecise([-0])", "true")]      // -0
    [InlineData("Math.sumPrecise([-0, -0])", "true")]  // -0
    public void SumPrecise_All_Negative_Zero_Is_Negative_Zero(string expr, string expected)
    {
        Assert.Equal(expected, Eval($"Object.is({expr}, -0)").ToString());
    }

    [Fact]
    public void SumPrecise_Mixed_Zero_Is_Positive_Zero()
    {
        Assert.Equal("true", Eval("Object.is(Math.sumPrecise([-0, 0]), 0)").ToString());
        // A genuine cancellation of non-zero values is +0, not -0.
        Assert.Equal("true", Eval("Object.is(Math.sumPrecise([1, -1]), 0)").ToString());
    }

    [Fact]
    public void SumPrecise_Still_Sums()
    {
        Assert.Equal("6", Eval("String(Math.sumPrecise([1, 2, 3]))").ToString());
    }

    // ---- Problem 9: Map canonicalizes -0 key to +0 ----

    [Fact]
    public void GetOrInsertComputed_Passes_Canonical_Key_To_Callback()
    {
        Assert.Equal("true", Eval(
            "var k; new Map().getOrInsertComputed(-0, function(a){ k = a; }); Object.is(k, 0)").ToString());
    }

    [Fact]
    public void GetOrInsert_Stores_Canonical_Key()
    {
        Assert.Equal("true", Eval(
            "var m = new Map(); m.getOrInsert(-0, 1); Object.is([...m.keys()][0], 0)").ToString());
    }

    // ---- Problem 10: Reflect.ownKeys lists symbol keys ----

    [Fact]
    public void OwnKeys_Lists_String_Then_Symbol_Keys()
    {
        // [[OwnPropertyKeys]] order: integer-index, string, then symbol keys.
        Assert.Equal("0,a,Symbol(s)", Eval(
            "var s = Symbol('s'); var o = {a:1}; o[0] = 0; o[s] = 2; " +
            "Reflect.ownKeys(o).map(String).join(',')").ToString());
    }

    [Fact]
    public void OwnKeys_Includes_Assigned_Symbol()
    {
        Assert.Equal("true", Eval(
            "var s = Symbol(); var o = {}; o[s] = 1; Reflect.ownKeys(o)[0] === s").ToString());
    }

    [Fact]
    public void Computed_Key_From_Symbol_Returning_ToString_Is_A_Symbol_Key()
    {
        // ToPropertyKey returns a Symbol from ToPrimitive directly (no ToString),
        // so the computed key is the symbol itself and Reflect.ownKeys surfaces it.
        Assert.Equal("true", Eval(
            "var sym = Symbol('x'); var key = { toString() { return sym; } }; " +
            "var obj = { [key]: 13 }; var found = Reflect.ownKeys(obj); " +
            "found.length === 1 && found[0] === sym && obj[sym] === 13").ToString());
    }

    [Fact]
    public void OwnKeys_On_Proxy_Still_Uses_Trap()
    {
        // The Proxy path must keep firing the ownKeys trap (not the ordinary helper).
        Assert.Equal("x,y", Eval(
            "var p = new Proxy({}, { ownKeys() { return ['x', 'y']; }, " +
            "getOwnPropertyDescriptor() { return { configurable: true, enumerable: true, value: 1 }; } }); " +
            "Reflect.ownKeys(p).join(',')").ToString());
    }

    // ---- Problem 3: TypedArray out-of-bounds element write semantics ----

    [Fact]
    public void TypedArray_OOB_Set_Calls_ToNumber_And_Returns_True()
    {
        // IntegerIndexedElementSet performs ToNumber BEFORE the bounds check and the
        // [[Set]] result is true even for an out-of-bounds (no-op) write.
        Assert.Equal("true", Eval(
            "var n = 0; var v = { valueOf() { n++; return 1; } }; var ta = new Int32Array(0); " +
            "var r = Reflect.set(ta, 0, v); r === true && n === 1").ToString());
    }

    [Fact]
    public void TypedArray_OOB_Plain_Assignment_Does_Not_Throw_In_Strict_Mode()
    {
        Assert.Equal("ok", Eval(
            "'use strict'; var ta = new Int32Array(0); ta[5] = 1; 'ok';").ToString());
    }

    [Fact]
    public void TypedArray_OOB_DefineProperty_Returns_False_Without_ToNumber()
    {
        // IntegerIndexedDefineOwnProperty checks IsValidIntegerIndex first and returns
        // false for an out-of-bounds index without ever converting the value.
        Assert.Equal("true", Eval(
            "var n = 0; var v = { valueOf() { n++; return 1; } }; var ta = new Int32Array(0); " +
            "var r = Reflect.defineProperty(ta, 0, { value: v, writable: true, enumerable: true, configurable: true }); " +
            "r === false && n === 0").ToString());
    }

    [Fact]
    public void TypedArray_InBounds_Write_Still_Works()
    {
        Assert.Equal("5", Eval("var ta = new Int32Array(2); ta[1] = 5; String(ta[1])").ToString());
        Assert.Equal("true", Eval(
            "var ta = new Int32Array(1); Reflect.set(ta, 0, 7) === true && ta[0] === 7").ToString());
        // A minimal value descriptor for an in-bounds index defines and writes.
        Assert.Equal("true", Eval(
            "var ta = new Int32Array(1); " +
            "Reflect.defineProperty(ta, 0, { value: 9 }) === true && ta[0] === 9").ToString());
    }

    // ---- Problem 10: CanonicalizeLocaleList accepts Intl.Locale objects ----

    [Fact]
    public void GetCanonicalLocales_Accepts_A_Single_Locale_Object()
    {
        Assert.Equal("ar", Eval(
            "Intl.getCanonicalLocales(new Intl.Locale('ar')).join(',')").ToString());
    }

    [Fact]
    public void GetCanonicalLocales_Uses_Locale_Slot_Not_ToString()
    {
        // A Locale subclass with a throwing toString must still canonicalize via the
        // [[Locale]] internal slot.
        Assert.Equal("fa", Eval(
            "class L extends Intl.Locale { toString() { throw new Error('nope'); } } " +
            "Intl.getCanonicalLocales(new L('fa')).join(',')").ToString());
    }

    [Fact]
    public void GetCanonicalLocales_Mixed_List_With_Locale_Objects()
    {
        Assert.Equal("ar,zh,fa", Eval(
            "var loc = new Intl.Locale('ar'); var ploc = new Intl.Locale('fa'); " +
            "Intl.getCanonicalLocales([loc, 'zh', ploc]).join(',')").ToString());
    }

    // ---- Problem 1: ArrayBuffer constructor requires `new` ----

    [Theory]
    [InlineData("ArrayBuffer()")]
    [InlineData("ArrayBuffer(1)")]
    [InlineData("ArrayBuffer.call(null)")]
    [InlineData("ArrayBuffer.apply(null, [])")]
    [InlineData("Reflect.apply(ArrayBuffer, null, [])")]
    public void ArrayBuffer_Called_Without_New_Throws_TypeError(string call)
    {
        Assert.Equal("TypeError", Catch(call + ";"));
    }

    [Fact]
    public void ArrayBuffer_With_New_Still_Constructs()
    {
        Assert.Equal("8", Eval("String(new ArrayBuffer(8).byteLength)").ToString());
        // Subclassing and internal allocations (typed array / slice) are unaffected.
        Assert.Equal("true", Eval(
            "class B extends ArrayBuffer {} var b = new B(4); " +
            "b instanceof ArrayBuffer && b.byteLength === 4").ToString());
        Assert.Equal("4", Eval("String(new ArrayBuffer(8).slice(0, 4).byteLength)").ToString());
    }

    // ---- Problem 4/7: per-evaluation private brand ----

    private static string PrivateEval(string body, string tail)
        => Eval("var __r; try { " + body + " __r = (" + tail + "); } catch (e) { __r = e.constructor.name; } __r;").ToString();

    [Theory]
    [InlineData("#m() { return 'x'; } access(o) { return o.#m(); }")]   // private method
    [InlineData("get #m() { return 'x'; } access(o) { return o.#m; }")] // private getter
    public void Private_Member_Brand_Differs_Per_Class_Evaluation(string members)
    {
        // Each call to the factory evaluates the class afresh, minting a distinct
        // private brand; an instance of one evaluation cannot reach another's member.
        var setup =
            "function make() { return new (class { " + members + " }); } " +
            "var c1 = make(); var c2 = make();";
        Assert.Equal("x", PrivateEval(setup, "c1.access(c1)"));
        Assert.Equal("x", PrivateEval(setup, "c2.access(c2)"));
        Assert.Equal("TypeError", PrivateEval(setup, "c1.access(c2)"));
        Assert.Equal("TypeError", PrivateEval(setup, "c2.access(c1)"));
    }

    [Fact]
    public void Nested_Class_Private_Name_Shadows_Outer()
    {
        // The inner class's `#x` is a distinct private name; an outer instance does
        // not carry it, so reaching it through the inner accessor is a TypeError.
        const string setup =
            "var outer = new (class Outer { #x = 'outer'; " +
            "  Inner = class { #x = 'inner'; reach(o) { return o.#x; } }; " +
            "  makeInner() { return new this.Inner(); } });";
        Assert.Equal("inner", PrivateEval(setup, "outer.makeInner().reach(outer.makeInner())"));
        Assert.Equal("TypeError", PrivateEval(setup, "outer.makeInner().reach(outer)"));
    }

    [Fact]
    public void Instance_Private_Field_And_Method_Still_Work()
    {
        Assert.Equal("42", Eval("class C { #f = 42; get() { return this.#f; } } new C().get()").ToString());
        Assert.Equal("7", Eval("class C { #m() { return 7; } call() { return this.#m(); } } new C().call()").ToString());
    }

    [Fact]
    public void Static_Private_Members_Still_Work()
    {
        // Static private members keep the stable key (one constructor per evaluation).
        Assert.Equal("1", Eval("class C { static #y = 1; static getY() { return this.#y; } } String(C.getY())").ToString());
        Assert.Equal("2", Eval("class C { static #m() { return 2; } static call() { return this.#m(); } } String(C.call())").ToString());
    }

    [Fact]
    public void Private_Name_Visible_To_Direct_Eval_In_Member()
    {
        // A class whose member uses direct eval falls back to the stable constant key
        // so the eval can still resolve the private name.
        Assert.Equal("5", Eval("class C { #m = 5; v = eval('this.#m'); } String(new C().v)").ToString());
    }
}
