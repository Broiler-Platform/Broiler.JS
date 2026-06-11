using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/753
//
// Fixed here:
//
//   Problem 5/19 (object-literal method super in a default parameter) — the
//   home-object usage detector now scans a method's (and arrow's) parameter list,
//   so `{ m(a = super.x) {} }` builds a [[HomeObject]] and super resolves against
//   the object's prototype rather than falling back to `this`.
//
//   Problem 29 (class computed-key method name) — a computed-key class method now
//   has its `name` set from the runtime key value (SetFunctionName), e.g.
//   `[Symbol('x')]() {}` → "[x]", `[1]() {}` → "1", with the standard
//   {writable:false} attribute.
//
//   Problem 30 (computed-number class method enumeration) — a non-enumerable
//   integer-indexed own property (e.g. a computed-number class method `[1]() {}`)
//   no longer leaks into Object.keys / for-in; the key enumerator honours the
//   enumerable attribute for element-stored properties.
//
//   Problem 31 (Number(string) non-decimal grammar) — Number("0b")/Number("0o")
//   with no digits is NaN, and a leading sign rejects the non-decimal prefixes
//   (`Number("+0b1")` → NaN).
//
//   Problem 36 (TypedArray.prototype.set return value) — set returns undefined.
//
//   Problem 37 (JSON.stringify replacer / serialization) — a Number/String wrapper
//   in an array replacer is keyed via ToString(v) (the object's toString, hint
//   String), and serializing a plain object no longer invokes its own `valueOf`
//   property.
public class Issue753Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // ---- Problem 5/19: super in a method's default parameter ----

    [Fact]
    public void SuperInMethodDefaultParameterResolvesToHomeObject()
        => Assert.Equal("true", Eval(
            "var o={ m(a = super.toString){ return a; } };" +
            "o.toString=null;" +
            "(o.m()===Object.prototype.toString).toString()"));

    [Fact]
    public void SuperInGeneratorMethodDefaultParameterResolvesToHomeObject()
        => Assert.Equal("true", Eval(
            "var o={ *m(a = super.toString){ return a; } };" +
            "o.toString=null;" +
            "(o.m().next().value===Object.prototype.toString).toString()"));

    // ---- Problem 29: computed-key class method name ----

    [Fact]
    public void ComputedSymbolMethodNameUsesBracketedDescription()
        => Assert.Equal("[test262]", Eval(
            "var s=Symbol('test262');class A{ [s](){} }A.prototype[s].name"));

    [Fact]
    public void ComputedNumberMethodNameUsesKey()
        => Assert.Equal("1", Eval("class A{ [1](){} }A.prototype[1].name"));

    [Fact]
    public void ComputedMethodNamePropertyIsNonWritable()
        => Assert.Equal("false", Eval(
            "var s=Symbol('x');class A{ [s](){} }" +
            "Object.getOwnPropertyDescriptor(A.prototype[s],'name').writable.toString()"));

    // ---- Problem 30: computed-number class methods are non-enumerable ----

    [Fact]
    public void ComputedNumberMethodIsNotEnumerable()
        => Assert.Equal("0", Eval(
            "class C{ a(){} [1](){} c(){} [2](){} }" +
            "Object.keys(C.prototype).length.toString()"));

    // ---- Problem 31: Number(string) non-decimal grammar ----

    [Fact]
    public void NumberOfEmptyBinaryPrefixIsNaN()
        => Assert.Equal("true", Eval("isNaN(Number('0b')).toString()"));

    [Fact]
    public void NumberOfEmptyOctalPrefixIsNaN()
        => Assert.Equal("true", Eval("isNaN(Number('0o')).toString()"));

    [Fact]
    public void NumberOfSignedBinaryLiteralIsNaN()
        => Assert.Equal("true", Eval("isNaN(Number('+0b1')).toString()"));

    [Fact]
    public void NumberOfValidBinaryLiteralIsParsed()
        => Assert.Equal("5", Eval("Number('0b101').toString()"));

    // ---- Problem 36: TypedArray.prototype.set returns undefined ----

    [Fact]
    public void TypedArraySetReturnsUndefined()
        => Assert.Equal("undefined", Eval(
            "(typeof new Int8Array(3).set([1,2])).toString()"));

    // ---- Problem 37: JSON.stringify replacer wrapper / no spurious valueOf ----

    [Fact]
    public void JsonReplacerArrayNumberWrapperUsesToString()
        => Assert.Equal("{\"toString\":2}", Eval(
            "var num=new Number(10);num.toString=function(){return 'toString';};" +
            "num.valueOf=function(){throw new Error('nope');};" +
            "JSON.stringify({10:1,toString:2,valueOf:3},[num])"));

    [Fact]
    public void JsonStringifyDoesNotCallObjectValueOfProperty()
        => Assert.Equal("{\"valueOf\":3}", Eval(
            "JSON.stringify({valueOf:3})"));

    // ---- Problem 33: Date.parse rejects -000000 (negative-zero extended year) ----

    [Fact]
    public void DateParseRejectsNegativeZeroExtendedYear()
        => Assert.Equal("true", Eval(
            "isNaN(Date.parse('-000000-03-31T00:45Z')).toString()"));

    // ---- Problem 17: Set union / symmetricDifference snapshot order ----

    [Fact]
    public void SetUnionReadsKeysIteratorNextBeforeCopyingThis()
        => Assert.Equal("4", Eval(
            "var set=new Set([1,2,3]);" +
            "var setLike={size:0,has(){throw new Error('no has');}," +
            "keys(){return {get next(){set.clear();set.add(4);return function(){return {done:true};};}};}};" +
            "[...set.union(setLike)].join(',')"));

    [Fact]
    public void SetSymmetricDifferenceReadsKeysIteratorNextBeforeCopyingThis()
        => Assert.Equal("4", Eval(
            "var set=new Set([1,2,3]);" +
            "var setLike={size:0,has(){throw new Error('no has');}," +
            "keys(){return {get next(){set.clear();set.add(4);return function(){return {done:true};};}};}};" +
            "[...set.symmetricDifference(setLike)].join(',')"));

    // ---- Problem 32: private-use-only locale tag is structurally invalid ----

    [Fact]
    public void IntlRejectsPrivateUseOnlyLocaleTag()
        => Assert.Equal("RangeError", Eval(
            "try{new Intl.ListFormat('x-private');'no throw';}catch(e){e.constructor.name;}"));

    [Fact]
    public void IntlAcceptsLanguageWithPrivateUseExtension()
        => Assert.Equal("ok", Eval(
            "try{new Intl.ListFormat('en-x-foo');'ok';}catch(e){'threw';}"));

    // ---- Problem 18: DateTimeFormat dateStyle/timeStyle coerced to string ----

    [Fact]
    public void DateTimeFormatDateStyleObjectOptionCoercedToString()
        => Assert.Equal("full", Eval(
            "new Intl.DateTimeFormat('en',{dateStyle:{toString(){return 'full';}}})" +
            ".resolvedOptions().dateStyle"));

    [Fact]
    public void DateTimeFormatTimeStyleResolvedOptionsIsString()
        => Assert.Equal("string", Eval(
            "typeof new Intl.DateTimeFormat('en',{timeStyle:'short'}).resolvedOptions().timeStyle"));

    [Fact]
    public void DateTimeFormatInvalidDateStyleThrowsRangeError()
        => Assert.Equal("RangeError", Eval(
            "try{new Intl.DateTimeFormat('en',{dateStyle:'bogus'});'no throw';}catch(e){e.constructor.name;}"));

    // ---- Problem 38/39: Greek Final_Sigma in toLowerCase / toLocaleLowerCase ----

    [Fact]
    public void ToLowerCaseUsesFinalSigmaWhenPrecededByCasedLetter()
        => Assert.Equal("aς", Eval("'A\\u03A3'.toLowerCase()"));

    [Fact]
    public void ToLowerCaseUsesRegularSigmaWhenFollowedByCasedLetter()
        => Assert.Equal("aσb", Eval("'A\\u03A3B'.toLowerCase()"));

    [Fact]
    public void ToLowerCaseFinalSigmaSkipsCaseIgnorableFullStop()
        => Assert.Equal("a.ς", Eval("'A.\\u03A3'.toLowerCase()"));

    [Fact]
    public void ToLowerCaseLoneSigmaIsRegular()
        => Assert.Equal("σ", Eval("'\\u03A3'.toLowerCase()"));

    [Fact]
    public void GreekWordFinalSigmaLowercases()
        => Assert.Equal("σοφος", Eval("'\\u03A3\\u039F\\u03A6\\u039F\\u03A3'.toLowerCase()"));

    [Fact]
    public void ToLocaleLowerCaseAppliesFinalSigma()
        => Assert.Equal("aς", Eval("'A\\u03A3'.toLocaleLowerCase('en')"));
}
