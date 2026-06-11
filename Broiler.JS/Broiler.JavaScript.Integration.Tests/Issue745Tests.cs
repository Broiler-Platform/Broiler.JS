using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/745
//
// Fixed here:
//
//   Problem 11 (Object.entries / Object.values enumeration order) — the key set is
//   now snapshotted before any value is read, so a getter that adds a property during
//   enumeration cannot inject the freshly-added key into the result.
//
//   Problem 14/15/16 (Function.prototype.toString source spans):
//     * async function declarations/expressions now begin the reported source text at
//       the `async` keyword (the parser threads the consumed `async` token into the
//       FunctionExpression's start), so `async function* f(){}` round-trips intact.
//     * a class member's reported source no longer includes the leading line
//       terminator separating it from `{` or the previous member — the class body now
//       skips line terminators before each element, so the first member's source span
//       starts at its own first token (fixes private-method and CR/CRLF toString).
//
//   Problem 19 (Intl invalid-option RangeError) — Intl.NumberFormat now validates the
//   trailingZeroDisplay / roundingMode / roundingPriority enums, and
//   Intl.DateTimeFormat validates timeZoneName, so an out-of-range value is a RangeError.
//
//   Problem 22 (ArrayBuffer.prototype.transfer ToIndex) — a newLength above 2^53-1
//   throws a RangeError (ToIndex), not a plain allocation Error.
//
//   Problem 23 (Array.prototype.toSpliced length limit) — newLen is computed with long
//   arithmetic so it no longer wraps; newLen above 2^53-1 is a TypeError and above
//   2^32-1 (ArrayCreate) is a RangeError, both thrown before any element is read.
//
//   Problem 24 (Intl.NumberFormat significant digits) — maximumSignificantDigits: 0 is
//   a RangeError, and supplying one significant-digit bound defaults the other
//   (minimum → 1, maximum → 21) in resolvedOptions.
//
//   Problem 29 (JSON.stringify of a BigInt / wrapper) — a Number/String/Boolean/BigInt
//   wrapper object is unwrapped to its primitive before serialization, so Object(1n)
//   becomes a BigInt and is rejected with a TypeError; non-finite numbers serialize as
//   null.
//
// Out of scope (architectural / data): P1/P27/P28 eval-scope ReferenceError families;
// P2/P25 super/`this`-before-super; P5 Unicode Script_Extensions data; P10 duplicate
// named groups with `{n}`-quantified unnamed alternatives; P13 Promise.race custom
// constructor capability; P20 length-bound array iterator over arbitrary array-likes;
// P18 duplicate-named-group same-alternative SyntaxError; P24 Intl.Locale BCP-47 tag
// validation; P26/P30 and the remaining sm/staging cases.
public class Issue745Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // ---- Problem 11: entries/values snapshot keys before reading values ----

    [Fact]
    public void ObjectEntriesDoesNotEnumeratePropertyAddedByGetter()
        => Assert.Equal("a,b", Eval(
            "var o={a:1};Object.defineProperty(o,'b',{enumerable:true,get:function(){o.c=3;return 2;}});" +
            "Object.entries(o).map(function(e){return e[0];}).join(',')"));

    [Fact]
    public void ObjectValuesDoesNotEnumeratePropertyAddedByGetter()
        => Assert.Equal("1,2", Eval(
            "var o={a:1};Object.defineProperty(o,'b',{enumerable:true,get:function(){o.c=3;return 2;}});" +
            "Object.values(o).join(',')"));

    // ---- Problem 14: async keyword preserved in toString ----

    [Fact]
    public void AsyncGeneratorDeclarationToStringKeepsAsync()
        => Assert.Equal("async function* f(x,y){ }", Eval(
            "async function* f(x,y){ }\nf.toString()"));

    [Fact]
    public void AsyncGeneratorExpressionToStringKeepsAsync()
        => Assert.Equal("async function* g(x){ yield 1; }", Eval(
            "var h=(async function* g(x){ yield 1; });h.toString()"));

    // ---- Problem 16: first class member source span excludes leading trivia ----

    [Fact]
    public void FirstClassMethodToStringExcludesLeadingNewline()
        => Assert.Equal("m(){ return 1; }", Eval(
            "var C=class {\n  m(){ return 1; }\n};(new C()).m.toString()"));

    [Fact]
    public void FirstClassPrivateMethodToStringExcludesLeadingComment()
        => Assert.Equal("#f(){ return 2; }", Eval(
            "var C=class {\n  /* before */#f(){ return 2; }\n  g(){ return this.#f.toString(); }\n};(new C()).g()"));

    // ---- Problem 19: Intl invalid option RangeError ----

    [Fact]
    public void NumberFormatRejectsInvalidTrailingZeroDisplay()
        => Assert.Equal("RangeError", Eval(
            "var e;try{new Intl.NumberFormat([], {trailingZeroDisplay:''});}catch(x){e=x.constructor.name;}e"));

    [Fact]
    public void DateTimeFormatRejectsInvalidTimeZoneName()
        => Assert.Equal("RangeError", Eval(
            "var e;try{new Intl.DateTimeFormat('en', {timeZoneName:'offset'});}catch(x){e=x.constructor.name;}e"));

    // ---- Problem 22: ArrayBuffer.prototype.transfer ToIndex RangeError ----

    [Fact]
    public void ArrayBufferTransferRejectsExcessiveLength()
        => Assert.Equal("RangeError", Eval(
            "var e;try{new ArrayBuffer(0).transfer(9007199254740992);}catch(x){e=x.constructor.name;}e"));

    // ---- Problem 23: toSpliced enforces ArrayCreate limits in the right order ----

    [Fact]
    public void ToSplicedThrowsRangeErrorBeyond2Pow32()
        => Assert.Equal("RangeError", Eval(
            "var al={length:Math.pow(2,32)};var e;" +
            "try{Array.prototype.toSpliced.call(al,0,0,1);}catch(x){e=x.constructor.name;}e"));

    [Fact]
    public void ToSplicedThrowsTypeErrorBeyond2Pow53()
        => Assert.Equal("TypeError", Eval(
            "var al={length:Math.pow(2,53)};var e;" +
            "try{Array.prototype.toSpliced.call(al,0,0,1);}catch(x){e=x.constructor.name;}e"));

    // ---- Problem 24: significant-digit validation and defaults ----

    [Fact]
    public void NumberFormatRejectsZeroMaximumSignificantDigits()
        => Assert.Equal("RangeError", Eval(
            "var e;try{new Intl.NumberFormat(undefined,{maximumSignificantDigits:0});}catch(x){e=x.constructor.name;}e"));

    [Fact]
    public void NumberFormatDefaultsMinimumSignificantDigits()
        => Assert.Equal("1", Eval(
            "new Intl.NumberFormat(undefined,{maximumSignificantDigits:1}).resolvedOptions().minimumSignificantDigits + ''"));

    // ---- Problem 25: super[key] does GetThisBinding before evaluating the key ----

    [Fact]
    public void SuperComputedReadChecksThisBeforeKey()
        => Assert.Equal("ReferenceError", Eval(
            "class B{constructor(){throw new Error('base');}}" +
            "class D extends B{constructor(){return super[super()];}}" +
            "var e;try{new D();}catch(x){e=x.constructor.name;}e"));

    [Fact]
    public void DeleteSuperComputedChecksThisBeforeKey()
        => Assert.Equal("ReferenceError", Eval(
            "class B{constructor(){throw new Error('base');}}" +
            "class D extends B{constructor(){delete super[(super(),0)];}}" +
            "var e;try{new D();}catch(x){e=x.constructor.name;}e"));

    // ---- Problem 26: class name in its own heritage is a TDZ ReferenceError ----

    [Fact]
    public void ClassNameInOwnHeritageThrowsReferenceError()
        => Assert.Equal("ReferenceError", Eval(
            "var e;try{var x=(class x extends x {});}catch(t){e=t.constructor.name;}e"));

    [Fact]
    public void NamedClassStillResolvesOwnNameInBody()
        => Assert.Equal("true", Eval(
            "class B{} class C extends B { m(){ return C; } } (new C().m()===C)+''"));

    // ---- Problem 29: JSON.stringify unwraps wrappers; BigInt is a TypeError ----

    [Fact]
    public void JsonStringifyBigIntWrapperThrowsTypeError()
        => Assert.Equal("TypeError", Eval(
            "var e;try{JSON.stringify(Object(1n));}catch(x){e=x.constructor.name;}e"));

    [Fact]
    public void JsonStringifyNonFiniteNumbersBecomeNull()
        => Assert.Equal("[null,1.5,null]", Eval("JSON.stringify([NaN,1.5,Infinity])"));

    // ---- Problem 30: empty-description symbol method name is "[]" not "" ----

    [Fact]
    public void EmptyDescriptionSymbolMethodNameIsBrackets()
        => Assert.Equal("[]", Eval("var s=Symbol('');var o={[s](){}};o[s].name"));

    [Fact]
    public void UndefinedDescriptionSymbolMethodNameIsEmpty()
        => Assert.Equal("", Eval("var s=Symbol();var o={[s](){}};o[s].name"));

    [Fact]
    public void NonEmptyDescriptionSymbolMethodNameIsBracketed()
        => Assert.Equal("[foo]", Eval("var s=Symbol('foo');var o={[s](){}};o[s].name"));
}
