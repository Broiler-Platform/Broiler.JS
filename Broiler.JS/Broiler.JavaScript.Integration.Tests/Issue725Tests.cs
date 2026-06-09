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

    // ---- Problem 7: Annex B block-scoped function hoisting in direct eval ----
    //
    // Covers annexB/language/eval-code/direct/func-{block,switch-*}-decl-eval-func-
    // existing-block-fn-update.js. Each block-scoped function declaration in a
    // direct eval must copy its value to the eval var-environment binding, so the
    // LAST block wins.

    [Fact]
    public void DirectEvalTwoBlocksLastFunctionWins()
        => Assert.Equal(
            "function|second declaration",
            Eval(@"
                var updated;
                (function() {
                  eval('{ function f() { return ""first declaration""; } }{ function f() { return ""second declaration""; } }updated = f;');
                }());
                typeof updated + '|' + (typeof updated === 'function' ? updated() : 'n/a');"));

    [Fact]
    public void DirectEvalThreeBlocksLastFunctionWins()
        => Assert.Equal(
            "C",
            Eval(@"(function(){ return eval('{ function f(){ return ""A""; } }{ function f(){ return ""B""; } }{ function f(){ return ""C""; } } f();'); }());"));

    [Fact]
    public void DirectEvalBlockFunctionUpdatesBindingAfterEachBlock()
        => Assert.Equal(
            "AB",
            Eval(@"(function(){ return eval('{ function f(){ return ""A""; } } var t1 = f(); { function f(){ return ""B""; } } var t2 = f(); t1 + t2;'); }());"));

    [Fact]
    public void DirectEvalSingleBlockFunctionStillHoists()
        => Assert.Equal(
            "A",
            Eval(@"(function(){ return eval('{ function f(){ return ""A""; } } f();'); }());"));

    [Fact]
    public void GlobalEvalTwoBlocksLastFunctionWinsUnchanged()
        => Assert.Equal(
            "B",
            Eval(@"var updated; eval('{ function f(){ return ""A""; } }{ function f(){ return ""B""; } }updated = f;'); updated();"));

    // ---- Problem 8: strict eval must not leak function declarations ----
    //
    // Covers language/eval-code/indirect/var-env-func-strict.js. A strict-mode
    // eval (direct or indirect) has its own variable environment, so a top-level
    // function declaration must not be created on the caller/global environment.

    [Fact]
    public void IndirectStrictEvalDoesNotLeakFunctionToGlobal()
        => Assert.Equal(
            "undefined|undefined",
            Eval(@"
                var typeofInside;
                (function() {
                  (0,eval)(""'use strict'; function fun(){}"");
                  typeofInside = typeof fun;
                }());
                typeofInside + '|' + (typeof fun);"));

    [Fact]
    public void DirectStrictEvalDoesNotLeakFunction()
        => Assert.Equal(
            "undefined",
            Eval(@"var t; (function(){ eval(""'use strict'; function dfn(){}""); t = typeof dfn; }()); t;"));

    [Fact]
    public void IndirectSloppyEvalStillDeclaresGlobalFunction()
        => Assert.Equal(
            "function|7",
            Eval(@"(0,eval)('function gfn(){ return 7; }'); typeof gfn + '|' + gfn();"));

    [Fact]
    public void StrictEvalVarStillIsolated()
        => Assert.Equal(
            "undefined|undefined",
            Eval(@"var t; (function(){ (0,eval)(""'use strict'; var v=1;""); t = typeof v; }()); t + '|' + (typeof v);"));

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

    // ---- Problem 5: missing TypeErrors ----
    //
    // staging/sm/Symbol/constructor.js: Symbol.prototype.valueOf on a non-symbol
    // receiver must throw (it fell through to Object.prototype.valueOf).

    [Fact]
    public void SymbolPrototypeValueOfOnPrototypeThrows()
        => Assert.Equal(
            "TypeError",
            Eval("try { Symbol.prototype.valueOf(); 'no throw'; } catch (e) { e.constructor.name; }"));

    [Fact]
    public void SymbolPrototypeValueOfReturnsWrappedSymbol()
        => Assert.Equal("true", Eval("var s = Symbol('x'); Object(s).valueOf() === s"));

    [Fact]
    public void SymbolConstructorWithSymbolArgumentThrows()
        => Assert.Equal(
            "TypeError",
            Eval("var s = Symbol(); try { Symbol(s); 'no throw'; } catch (e) { e.constructor.name; }"));

    [Fact]
    public void SymbolConstructorWithBoxedSymbolArgumentThrows()
        => Assert.Equal(
            "TypeError",
            Eval("var s = Symbol(); try { Symbol(Object(s)); 'no throw'; } catch (e) { e.constructor.name; }"));

    // staging/sm/TypedArray/object-defineproperty.js: typed-array elements are
    // configurable:true since ES2021; defineProperty with a conflicting attribute
    // or an accessor must throw, while matching attributes succeed.

    [Theory]
    [InlineData("{get: function(){}}")]
    [InlineData("{set: function(){}}")]
    [InlineData("{configurable: false}")]
    [InlineData("{enumerable: false}")]
    [InlineData("{writable: false}")]
    [InlineData("{configurable: false, value: 15}")]
    public void TypedArrayDefinePropertyRejectsConflictingDescriptor(string desc)
        => Assert.Equal(
            "TypeError",
            Eval($"var a = new Int32Array(2); try {{ Object.defineProperty(a, 0, {desc}); 'no throw'; }} catch (e) {{ e.constructor.name; }}"));

    [Fact]
    public void TypedArrayDefinePropertyAllowsMatchingAttributes()
        => Assert.Equal(
            "15",
            Eval("var a = new Int32Array(2); Object.defineProperty(a, 0, {configurable: true}); Object.defineProperty(a, 0, {value: 15}); String(a[0])"));

    [Fact]
    public void TypedArrayElementDescriptorIsConfigurable()
        => Assert.Equal(
            "true,true,true",
            Eval("var a = new Int32Array(2); a[0] = 1; var d = Object.getOwnPropertyDescriptor(a, 0); [d.configurable, d.enumerable, d.writable].join(',')"));

    // ---- Problem 2: en dayPeriod must use contiguous flexible periods ----
    //
    // Covers intl402/DateTimeFormat/prototype/{format,formatToParts}/
    // dayPeriod-{long,short}-en.js. The observed periods across 24h must be the
    // five {in the morning, noon, in the afternoon, in the evening, at night};
    // midnight must NOT surface and the periods must be contiguous.

    [Theory]
    [InlineData(0, "at night")]
    [InlineData(5, "at night")]
    [InlineData(6, "in the morning")]
    [InlineData(11, "in the morning")]
    [InlineData(12, "noon")]
    [InlineData(13, "in the afternoon")]
    [InlineData(17, "in the afternoon")]
    [InlineData(18, "in the evening")]
    [InlineData(20, "in the evening")]
    [InlineData(21, "at night")]
    [InlineData(23, "at night")]
    public void EnDayPeriodLongResolvesEachHour(int hour, string expected)
        => Assert.Equal(
            expected,
            Eval($"new Intl.DateTimeFormat('en', {{dayPeriod:'long'}}).format(new Date(2017,11,12,{hour},0));"));

    [Fact]
    public void EnDayPeriodAcrossDayUsesOnlyTheFiveExpectedPeriods()
        => Assert.Equal(
            "in the morning,noon,in the afternoon,in the evening,at night",
            Eval(@"
                var f = new Intl.DateTimeFormat('en', {dayPeriod:'long'});
                var seen = [];
                for (var h = 0; h < 24; h++) {
                  var p = f.format(new Date(2017,11,12,h,0));
                  if (seen.indexOf(p) < 0) seen.push(p);
                }
                seen.sort(function(a,b){
                  var order=['in the morning','noon','in the afternoon','in the evening','at night'];
                  return order.indexOf(a)-order.indexOf(b);
                }).join(',');"));

    [Fact]
    public void EnDayPeriodShortMidnightIsAtNight()
        => Assert.Equal(
            "at night",
            Eval("new Intl.DateTimeFormat('en', {dayPeriod:'short'}).format(new Date(2017,11,12,0,0));"));

    [Fact]
    public void EnDayPeriodFormatToPartsMidnightIsAtNight()
        => Assert.Equal(
            "dayPeriod:at night",
            Eval(@"
                var parts = new Intl.DateTimeFormat('en', {dayPeriod:'long'}).formatToParts(new Date(2017,11,12,0,0));
                parts.map(function(p){ return p.type + ':' + p.value; }).join('|');"));

    // ---- Problem 4: compact notation for CJK locales ----
    //
    // Covers intl402/NumberFormat/prototype/formatToParts/notation-compact-
    // {ja-JP,ko-KR,zh-TW}.js. The value is reduced by the largest applicable compact
    // unit (万/億/兆, 천/만/억/조, 萬/億/兆) and rounded with the default morePrecision
    // rule (2 significant digits below 100, else integer); the suffix is a "compact"
    // part. Numbers below the smallest unit are not compacted.

    [Theory]
    [InlineData("ja-JP", 987654321, "9.9億")]
    [InlineData("ja-JP", 98765432, "9877万")]
    [InlineData("ja-JP", 98765, "9.9万")]
    [InlineData("ja-JP", 9876, "9876")]
    [InlineData("ja-JP", 159, "159")]
    [InlineData("ja-JP", 15.9, "16")]
    [InlineData("ja-JP", 1.59, "1.6")]
    [InlineData("ja-JP", 0.00159, "0.0016")]
    [InlineData("ko-KR", 987654321, "9.9억")]
    [InlineData("ko-KR", 98765432, "9877만")]
    [InlineData("ko-KR", 98765, "9.9만")]
    [InlineData("ko-KR", 9876, "9.9천")]
    [InlineData("ko-KR", 159, "159")]
    [InlineData("zh-TW", 987654321, "9.9億")]
    [InlineData("zh-TW", 98765432, "9877萬")]
    [InlineData("zh-TW", 98765, "9.9萬")]
    [InlineData("zh-TW", 9876, "9876")]
    public void CompactNotationFormatsCjkLocales(string loc, double n, string expected)
        => Assert.Equal(expected, Eval($"new Intl.NumberFormat('{loc}',{{notation:'compact'}}).format({n});"));

    [Fact]
    public void CompactNotationFormatToPartsHasFourParts()
        => Assert.Equal(
            "integer:9|decimal:.|fraction:9|compact:億",
            Eval(@"new Intl.NumberFormat('ja-JP',{notation:'compact'}).formatToParts(987654321).map(function(p){return p.type+':'+p.value;}).join('|');"));

    [Fact]
    public void CompactNotationNegativeKeepsSign()
        => Assert.Equal(
            "minusSign:-|integer:9|decimal:.|fraction:9|compact:億",
            Eval(@"new Intl.NumberFormat('ja-JP',{notation:'compact'}).formatToParts(-987654321).map(function(p){return p.type+':'+p.value;}).join('|');"));

    // ---- Problem 1: RegExp semantics (unicode class escapes + self-backrefs) ----
    //
    // unicode-character-class-escape.js: with the u flag, \D/\S/\W (and their
    // [\D]/[\S]/[\W] class forms) match a whole surrogate pair as one code point and
    // a LONE surrogate as a single code unit.
    //
    // regress-576828.js: /(z\1){3}/ on "zzz" → ["zzz","z"]. A backreference inside
    // the group it references is always empty in ECMAScript (the group resets each
    // quantifier iteration); .NET keeps the prior capture, so such self/ancestor
    // references are rewritten to an empty group.

    [Fact]
    public void SelfBackreferenceInQuantifierMatchesEmpty()
        => Assert.Equal(
            "zzz|z|0|zzz",
            Eval(@"var m = /(z\1){3}/.exec('zzz'); m[0] + '|' + m[1] + '|' + m.index + '|' + m.input;"));

    [Theory]
    [InlineData(@"/\D/u", @"\uD83D\uDBFF", "d83d")]   // two high surrogates: lone, single unit
    [InlineData(@"/\D/u", @"🐀", "d83d,dc00")]            // valid pair: whole code point
    [InlineData(@"/\S/u", @"𐀸", "d800,dc38")]
    [InlineData(@"/\S/u", @"\uD83D", "d83d")]           // lone high surrogate
    [InlineData(@"/\S/u", @"\uDC00\uDC38", "dc00")]     // lone low surrogate
    [InlineData(@"/\W/u", @"􏰸", "dbff,dc38")]
    [InlineData(@"/[\D]/u", @"𐀸", "d800,dc38")]          // class form, valid pair
    [InlineData(@"/[\S]/u", @"𐀸", "d800,dc38")]
    [InlineData(@"/[\W]/u", @"􏰸", "dbff,dc38")]
    [InlineData(@"/[\D]/u", @"\uDC00\uDC38", "dc00")]   // class form, lone surrogate
    [InlineData(@"/[\W]/u", @"퟿\uDC38", "d7ff")]
    [InlineData(@"/[\D]+/u", "abc012", "61,62,63")]       // BMP behaviour preserved
    public void UnicodeClassEscapeMatchesCodePoints(string pat, string input, string expectedHex)
        => Assert.Equal(
            expectedHex,
            Eval($"var m = {pat}.exec('{input}'); m===null?'NULL':Array.prototype.map.call(m[0],function(ch){{return ch.charCodeAt(0).toString(16);}}).join(',');"));

    [Theory]
    [InlineData(@"/(z\1){3}/", "zzz", "zzz,z")]                 // self-ref in quantifier
    [InlineData(@"/(z\1)/", "z", "z,z")]                        // self-ref, unquantified
    [InlineData(@"/(a)\1/", "aa", "aa,a")]                      // normal backref still works
    [InlineData(@"/(a)(b)\2\1/", "abba", "abba,a,b")]           // multiple normal backrefs
    [InlineData(@"/((a)\2)/", "aa", "aa,aa,a")]                 // nested: \2 refs closed inner group
    [InlineData(@"/(a(b\1))/", "ab", "ab,ab,b")]                // ancestor ref \1 -> empty
    [InlineData(@"/\1(a)/", "a", "a,a")]                        // forward ref -> empty
    [InlineData(@"/(a)|(b)\2/", "bb", "bb,,b")]                 // \2 refs closed group in alt
    public void SelfBackreferenceMatchesEmpty(string pat, string input, string expected)
        => Assert.Equal(expected, Eval($"var m = {pat}.exec('{input}'); m===null?'NULL':Array.prototype.map.call(m,function(x){{return x===undefined?'':x;}}).join(',');"));

    // ---- Problem 3: locale-aware numeric date layout ----
    //
    // Covers staging/sm/String/internalUsage.js: Intl.DateTimeFormat("de").format
    // must produce a German date ("2.1.1970"), not the en M/d/y form, and must not
    // be affected by a user override of String.prototype[Symbol.split].

    [Theory]
    [InlineData("de", "2.1.1970")]
    [InlineData("en", "1/2/1970")]
    public void NumericDateUsesLocaleLayout(string locale, string expected)
        => Assert.Equal(expected, Eval($"Intl.DateTimeFormat('{locale}', {{}}).format(24*60*60*1000);"));

    [Fact]
    public void GermanDateFormatRobustToSplitPrototypePatch()
        => Assert.Equal(
            "true,true,true,true",
            Eval(@"
                var t = 24*60*60*1000;
                var ok = ['1.1.1970','2.1.1970','3.1.1970'];
                var r = [];
                ['', 'x-foo', 'de-u-co', 'en-US'].forEach(function (v) {
                  String.prototype[Symbol.split] = function () { return [v]; };
                  r.push(ok.indexOf(Intl.DateTimeFormat('de', {}).format(t)) >= 0);
                });
                r.join(',');"));
}
