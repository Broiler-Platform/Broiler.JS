using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/657
// Covers the cleanly-reproducible subset of the test262 common-failure triage:
//   Problem 6/7  - %TypedArray%.prototype.{filter,map,slice,subarray} must use the
//                  realm-intrinsic default constructor (obtained non-observably)
//                  when constructor / @@species is absent or undefined.
//   Problem 8    - invoking a class/object method whose computed name is a
//                  canonical array-index string ("1") via the string-literal key.
//   Problem 9    - inherited methods (hasOwnProperty, ...) are reachable on the
//                  native Function.prototype methods (their [[Prototype]] is
//                  Function.prototype).
//   Problem 10   - dynamic string-key and symbol-key reads on primitive strings
//                  resolve the String prototype (e.g. ""[Symbol.iterator]).
//
// Problems 1/2 (\p{Emoji_Keycap_Sequence} / \p{RGI_Emoji} string-properties),
// 3 (sm "structurally equal" harness), 4/5 (Intl.DateTimeFormat formatRange /
// Date.toISOString extended range) are out of scope here — see the issue-654
// triage memo for why.
public class Issue657Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // ---- Problem 10: primitive string dynamic / symbol property reads ----

    // ""[Symbol.iterator] must resolve to String.prototype[Symbol.iterator]
    // even when it is the first property access on a freshly-boxed string.
    [Fact]
    public void EmptyStringHasSymbolIterator()
        => Assert.Equal("function", Eval("typeof (''[Symbol.iterator])"));

    // The String iterator protocol works end-to-end off an empty string literal.
    [Fact]
    public void EmptyStringIteratorIsUsable()
        => Assert.Equal("object", Eval("typeof ('' + '')[Symbol.iterator] === 'undefined' ? 'x' : typeof (''[Symbol.iterator]())"));

    // Dynamic (non-constant-folded) string-key reads on a primitive string
    // resolve inherited members and array indices alike.
    [Fact]
    public void DynamicStringKeyOnPrimitiveResolvesPrototype()
        => Assert.Equal("function 3", Eval("var k='charAt'; var l='length'; typeof 'abc'[k] + ' ' + 'abc'[l]"));

    // StringIteratorPrototype inherits %IteratorPrototype%[Symbol.iterator].
    [Fact]
    public void StringIteratorPrototypeHasIteratorSymbol()
        => Assert.Equal("function", Eval(
            "var it = ''[Symbol.iterator](); typeof Object.getPrototypeOf(it)[Symbol.iterator]"));

    // ---- Problem 9: native Function.prototype methods inherit correctly ----

    // Function.prototype.toString (a native function) has Function.prototype as
    // its [[Prototype]], so inherited Object.prototype methods are callable.
    [Fact]
    public void NativeFunctionPrototypeMethodInheritsFunctionPrototype()
        => Assert.Equal("true", Eval("'' + (Object.getPrototypeOf(Function.prototype.toString) === Function.prototype)"));

    [Fact]
    public void NativeFunctionMethodHasInheritedHasOwnProperty()
        => Assert.Equal("function true", Eval(
            "typeof Function.prototype.toString.hasOwnProperty + ' ' + Function.prototype.toString.hasOwnProperty('length')"));

    // call/apply/bind likewise.
    [Fact]
    public void FunctionPrototypeCallInheritsFunctionPrototype()
        => Assert.Equal("true true true", Eval(
            "var e=f=>Object.getPrototypeOf(f)===Function.prototype;"
            + "'' + e(Function.prototype.call) + ' ' + e(Function.prototype.apply) + ' ' + e(Function.prototype.bind)"));

    // ---- Problem 8: computed method name that is a canonical numeric string ----

    [Fact]
    public void ClassComputedNumericStringMethodInvokableBothWays()
        => Assert.Equal("m1 m1", Eval(
            "class C { ['1']() { return 'm1'; } } var c = new C(); c[1]() + ' ' + c['1']()"));

    [Fact]
    public void ObjectComputedNumericStringMethodInvokable()
        => Assert.Equal("L L", Eval(
            "var o = { ['1']() { return 'L'; } }; o[1]() + ' ' + o['1']()"));

    // Non-numeric computed names are unaffected.
    [Fact]
    public void ClassComputedStringMethodInvokable()
        => Assert.Equal("ma", Eval("class C { ['a']() { return 'ma'; } } new C()['a']()"));

    // ---- Problems 6/7: TypedArray species default constructor ----

    // When TA.prototype.constructor is overridden with a getter returning
    // undefined, map/filter/slice/subarray must fall back to the realm-intrinsic
    // constructor rather than trying to construct `undefined`.
    [Fact]
    public void TypedArraySpeciesUsesIntrinsicWhenConstructorUndefined()
        => Assert.Equal("number 2", Eval(
            "var sample = new Float64Array([1,2]);"
            + "Object.defineProperty(Float64Array.prototype,'constructor',{get(){return undefined;},configurable:true});"
            + "var r = sample.map(v=>v); typeof r.length + ' ' + r.length"));

    [Fact]
    public void BigIntTypedArraySpeciesUsesIntrinsicWhenConstructorUndefined()
        => Assert.Equal("2", Eval(
            "var sample = new BigInt64Array([10n,20n]);"
            + "Object.defineProperty(BigInt64Array.prototype,'constructor',{get(){return undefined;},configurable:true});"
            + "'' + sample.filter(()=>true).length"));

    // Genuine subclass species is still honoured.
    [Fact]
    public void TypedArraySpeciesHonoursSubclass()
        => Assert.Equal("true 6", Eval(
            "class MyF64 extends Float64Array {} var s = new MyF64([1,2,3]);"
            + "var r = s.map(v=>v*2); '' + (r instanceof MyF64) + ' ' + r[2]"));
}
