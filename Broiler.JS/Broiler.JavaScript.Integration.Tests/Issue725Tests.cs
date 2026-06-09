using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/725
//
// Fixed here:
//
//   Problem 10 (JSON.stringify replacer array) — an array replacer (PropertyList)
//   was converted into a function-style replacer and applied to *every* visited
//   value, including the root holder (key "") and each array element. Because the
//   empty-string root key is never a member of the PropertyList, the root was
//   replaced with undefined and `JSON.stringify({a:1}, [])` produced "null"
//   instead of "{}". Array elements were likewise filtered out by index.
//
//   Per sec-serializejsonobject a PropertyList only restricts which *object* keys
//   are serialized — it never touches the root value or array elements, and it is
//   consulted via [[Get]] (so listed keys are read in list order). The PropertyList
//   is now threaded separately from a function replacer, and only String/Number
//   entries (and their wrapper objects) become keys, de-duplicated in first-seen
//   order (undefined entries and array holes are ignored).
//
// Out of scope for this change: the other nine problems in the issue (RegExp
// duplicate named groups, several Intl/CLDR gaps, Annex B eval function hoisting,
// class static field anonymous-function naming) are unrelated subsystems.
public class Issue725Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // ---- replacer-array-empty.js ----

    [Fact]
    public void EmptyReplacerArraySerializesObjectToEmptyBraces()
        => Assert.Equal("{}", Eval("JSON.stringify({a: 1, b: 2}, [])"));

    [Fact]
    public void EmptyReplacerArrayAppliesToNestedObjects()
        => Assert.Equal("{}", Eval("JSON.stringify({a: 1, b: {c: 2}}, [])"));

    [Fact]
    public void EmptyReplacerArrayDoesNotFilterArrayElements()
        => Assert.Equal("[1,{}]", Eval("JSON.stringify([1, {a: 2}], [])"));

    // ---- replacer-array-undefined.js ----

    [Fact]
    public void UndefinedReplacerArrayEntriesAreIgnored()
        => Assert.Equal("{}", Eval("JSON.stringify({undefined: 1}, [undefined])"));

    [Fact]
    public void SparseReplacerArrayHolesAreIgnored()
        => Assert.Equal("{}", Eval("JSON.stringify({key: 1, undefined: 2}, [,,,])"));

    [Fact]
    public void SparseReplacerArrayKeepsRealKeys()
        => Assert.Equal(
            "{\"key\":2}",
            Eval("var sparse = new Array(3); sparse[1] = 'key'; JSON.stringify({undefined: 1, key: 2}, sparse)"));

    // ---- additional PropertyList semantics ----

    [Fact]
    public void ReplacerArrayKeepsListedKeysInListOrder()
        => Assert.Equal(
            "{\"b\":2,\"a\":1}",
            Eval("JSON.stringify({a: 1, b: 2, c: 3}, ['b', 'a'])"));

    [Fact]
    public void ReplacerArrayDeduplicatesKeys()
        => Assert.Equal(
            "{\"a\":1}",
            Eval("JSON.stringify({a: 1, b: 2}, ['a', 'a'])"));

    [Fact]
    public void NumericReplacerArrayEntriesBecomeStringKeys()
        => Assert.Equal(
            "{\"1\":\"one\"}",
            Eval("JSON.stringify({1: 'one', 2: 'two'}, [1])"));

    [Fact]
    public void BooleanReplacerArrayEntriesAreIgnored()
        => Assert.Equal("{}", Eval("JSON.stringify({a: 1, true: 2}, [true])"));

    [Fact]
    public void FunctionReplacerStillAppliesToRootAndArrayElements()
        => Assert.Equal(
            "[2,4]",
            Eval("JSON.stringify([1, 2], function (k, v) { return typeof v === 'number' ? v * 2 : v; })"));

    // ---- Problem 6: grandfathered language tag canonicalization ----
    //
    // Intl.getCanonicalLocales / Intl.Locale must replace a regular grandfathered
    // tag with its preferred UTS #35 form. Covers
    // intl402/Intl/getCanonicalLocales/grandfathered.js and preferred-grandfathered.js.

    [Theory]
    [InlineData("art-lojban", "jbo")]
    [InlineData("cel-gaulish", "xtg")]
    [InlineData("zh-guoyu", "zh")]
    [InlineData("zh-hakka", "hak")]
    [InlineData("zh-xiang", "hsn")]
    public void GetCanonicalLocalesMapsRegularGrandfatheredTags(string tag, string canonical)
        => Assert.Equal(canonical, Eval($"Intl.getCanonicalLocales('{tag}')[0]"));

    [Fact]
    public void GrandfatheredCanonicalizationIsCaseInsensitive()
        => Assert.Equal("jbo", Eval("Intl.getCanonicalLocales('Art-LojBan')[0]"));

    [Fact]
    public void IntlLocaleCanonicalizesGrandfatheredTag()
        => Assert.Equal("jbo", Eval("new Intl.Locale('art-lojban').toString()"));

    [Theory]
    [InlineData("i-klingon")]
    [InlineData("en-GB-oed")]
    [InlineData("sgn-BE-FR")]
    [InlineData("no-bok")]
    [InlineData("zh-min-nan")]
    public void IrregularAndInvalidGrandfatheredTagsAreRejected(string tag)
        => Assert.Equal(
            "RangeError",
            Eval($"try {{ Intl.getCanonicalLocales('{tag}'); 'no throw'; }} catch (e) {{ e.constructor.name; }}"));

    // ---- Problem 9: anonymous function naming for class fields (NamedEvaluation) ----
    //
    // Covers language/{statements,expressions}/class/elements/
    // static-field-anonymous-function-name.js. The function assigned to a field
    // takes the field's name; a private field uses the "#name" form.

    [Fact]
    public void StaticPublicFieldAnonymousFunctionGetsFieldName()
        => Assert.Equal(
            "field",
            Eval("class C { static field = function () { return 42; }; } C.field.name"));

    [Fact]
    public void StaticPrivateFieldAnonymousArrowGetsHashName()
        => Assert.Equal(
            "#field",
            Eval("class C { static #field = () => 'x'; static getName() { return this.#field.name; } } C.getName()"));

    [Fact]
    public void InstancePublicFieldAnonymousFunctionGetsFieldName()
        => Assert.Equal(
            "f",
            Eval("class C { f = function () {}; } new C().f.name"));

    [Fact]
    public void InstancePrivateFieldAnonymousFunctionGetsHashName()
        => Assert.Equal(
            "#m",
            Eval("class C { #m = function () {}; getName() { return this.#m.name; } } new C().getName()"));

    [Fact]
    public void ComputedFieldAnonymousFunctionGetsComputedName()
        => Assert.Equal(
            "computed",
            Eval("var k = 'computed'; class C { static [k] = function () {}; } C.computed.name"));

    [Fact]
    public void NamedFunctionFieldKeepsItsOwnName()
        => Assert.Equal(
            "original",
            Eval("class C { static field = function original() {}; } C.field.name"));

    [Fact]
    public void StaticFieldAnonymousClassGetsFieldName()
        => Assert.Equal(
            "field",
            Eval("class C { static field = class {}; } C.field.name"));
}
