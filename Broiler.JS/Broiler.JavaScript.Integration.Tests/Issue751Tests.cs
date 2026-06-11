using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/751
//
// Fixed here:
//
//   Problem 18 (numeric literal / Number()/parseFloat precision) — a <16-digit
//   significand was scaled by a single Math.Pow(10, exp) multiply (the Clinger fast
//   path), which is only correctly rounded for an exact power of ten (10^0..10^22).
//   Beyond |exp| > 22 the multiply accumulated rounding error (1.23456789e+34 →
//   1.2345678900000002e+34). Both number parsers (lexer NumberCoercion and runtime
//   NumberParser) now fall through to the arbitrary-precision RefineEstimate when the
//   exponent magnitude exceeds 22.
//
//   Problem 30 (Intl.NumberFormat resolvedOptions rounding slots) —
//   roundingIncrement/roundingMode/roundingPriority/trailingZeroDisplay are now always
//   present with their resolved defaults (validated at construction; an out-of-range
//   roundingIncrement is a RangeError), emitted in spec order (after signDisplay).
//
//   Problem 35 (Promise resolve/reject function length) — the resolving functions
//   passed to a Promise executor now have length 1 (and empty name) per spec.
//
//   Problem 36 (Array indexOf/lastIndexOf length-before-ToInteger) — both now return
//   -1 for a zero-length array BEFORE calling ToIntegerOrInfinity(fromIndex), so a
//   fromIndex valueOf side effect is not observed on an empty array.
//
//   Problem 38/41 (Map/Set prototype entries/values name) — the source generator named
//   a marshalled [JSExport] method after the C# method (GetEntries/Values) instead of
//   the JS export name. The function's `name` now matches its property key (entries/
//   values/keys/...).
//
//   Problem 39/40 (computed-key accessor name) — a `get [expr]()` / `set [expr]()` in
//   an object literal or class left the function anonymous ("native"). It is now named
//   "get "/"set " + the property key (symbols → "get [desc]" / "get " for an
//   undefined description), for object-literal and class accessors alike.
//
//   Problem 14 (Annex B block-level function hoisting) — Web Legacy Compatibility
//   (B.3.2) was applied to ALL block-nested function declarations, so a generator or
//   async function declared in a block leaked a function-scope var binding. It now
//   applies only to plain FunctionDeclarations; generators and async functions stay
//   block-scoped (no var copy-out, no pre-hoisted binding).
//
//   Problem 22 (bound function new.target remap) — BoundFunction [[Construct]] now
//   performs "If SameValue(F, newTarget), set newTarget to target" inside the bound
//   construct path (not only at the `new` site), so Reflect.construct(BF, args, BF)
//   builds the bound target with new.target = the target (an explicit, different
//   newTarget is still preserved).
//
//   Problem 32/33 (toUpperCase/toLowerCase special casing) — these used .NET's simple
//   1:1 mapping and missed the Unicode one-to-many expansions from SpecialCasing.txt
//   (ß → SS, ﬁ → FI, İ → i̇, the Greek/Armenian expansions, …). The unconditional,
//   locale-independent full mappings are now applied; toLocaleUpperCase combines them
//   with culture-specific simple mapping (e.g. Turkish i → İ).
public class Issue751Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // ---- Problem 18: number parsing precision ----

    [Fact]
    public void LiteralLargeExponentIsCorrectlyRounded()
        => Assert.Equal("true", Eval("1.23456789e+34 === 1.23456789e+34 && (1.23456789e+34).toString() === '1.23456789e+34'"));

    [Fact]
    public void NumberCtorLargeExponentMatchesLiteral()
        => Assert.Equal("true", Eval("Number('1.23456789e+34') === 1.23456789e+34"));

    [Fact]
    public void ParseFloatLargeExponentMatchesLiteral()
        => Assert.Equal("true", Eval("parseFloat('1.23456789e+34') === 1.23456789e+34"));

    [Fact]
    public void JsonParseLargeExponentMatchesLiteral()
        => Assert.Equal("true", Eval("JSON.parse('1.23456789e+34') === 1.23456789e+34"));

    [Fact]
    public void SmallExponentFastPathUnchanged()
        => Assert.Equal("true", Eval("1.5e3 === 1500 && 1e22 === 1e22 && 0.5 === 0.5"));

    // ---- Problem 30: NumberFormat resolvedOptions rounding slots ----

    [Fact]
    public void NumberFormatResolvedRoundingDefaults()
        => Assert.Equal("1,halfExpand,auto,auto", Eval(
            "var r=new Intl.NumberFormat('en').resolvedOptions();" +
            "[r.roundingIncrement,r.roundingMode,r.roundingPriority,r.trailingZeroDisplay].join(',')"));

    [Fact]
    public void NumberFormatResolvedRoundingCustom()
        => Assert.Equal("5,floor,stripIfInteger", Eval(
            "var r=new Intl.NumberFormat('en',{roundingIncrement:5,minimumFractionDigits:2,maximumFractionDigits:2,roundingMode:'floor',trailingZeroDisplay:'stripIfInteger'}).resolvedOptions();" +
            "[r.roundingIncrement,r.roundingMode,r.trailingZeroDisplay].join(',')"));

    [Fact]
    public void NumberFormatInvalidRoundingIncrementThrows()
        => Assert.Equal("RangeError", Eval(
            "var t='none'; try{ new Intl.NumberFormat('en',{roundingIncrement:3}); }catch(e){ t=e.constructor.name; } t"));

    [Fact]
    public void NumberFormatRoundingSlotsAfterSignDisplay()
        => Assert.Equal("true", Eval(
            "var k=Object.keys(new Intl.NumberFormat('en').resolvedOptions());" +
            "k.indexOf('roundingIncrement')>k.indexOf('signDisplay')"));

    // ---- Problem 35: Promise resolve/reject function length ----

    [Fact]
    public void PromiseResolvingFunctionsLengthIsOne()
        => Assert.Equal("1,1,,", Eval(
            "var L; new Promise(function(res,rej){ L=[res.length,rej.length,res.name,rej.name].join(','); }); L"));

    // ---- Problem 36: indexOf/lastIndexOf length checked before ToInteger ----

    [Fact]
    public void IndexOfEmptyDoesNotCoerceFromIndex()
        => Assert.Equal("false", Eval(
            "var seen=false; [].indexOf(1,{valueOf:function(){seen=true;return 0;}}); seen"));

    [Fact]
    public void LastIndexOfEmptyDoesNotCoerceFromIndex()
        => Assert.Equal("false", Eval(
            "var seen=false; [].lastIndexOf(1,{valueOf:function(){seen=true;return 0;}}); seen"));

    [Fact]
    public void IndexOfStillWorksOnNonEmpty()
        => Assert.Equal("1", Eval("[5,6,7].indexOf(6)"));

    // ---- Problem 38/41: Map/Set entries/values/keys name ----

    [Fact]
    public void MapPrototypeIteratorNames()
        => Assert.Equal("entries,values,keys", Eval(
            "[Map.prototype.entries.name,Map.prototype.values.name,Map.prototype.keys.name].join(',')"));

    [Fact]
    public void SetPrototypeIteratorNames()
        => Assert.Equal("entries,values,keys", Eval(
            "[Set.prototype.entries.name,Set.prototype.values.name,Set.prototype.keys.name].join(',')"));

    // ---- Problem 39/40: computed-key accessor name ----

    [Fact]
    public void ObjectComputedSymbolGetterName()
        => Assert.Equal("get [desc]", Eval(
            "var s=Symbol('desc'); var o={ get [s](){} }; Object.getOwnPropertyDescriptor(o,s).get.name"));

    [Fact]
    public void ObjectComputedSymbolSetterName()
        => Assert.Equal("set [desc]", Eval(
            "var s=Symbol('desc'); var o={ set [s](v){} }; Object.getOwnPropertyDescriptor(o,s).set.name"));

    [Fact]
    public void ObjectComputedUndefinedDescriptionSymbolGetterName()
        => Assert.Equal("get ", Eval(
            "var s=Symbol(); var o={ get [s](){} }; Object.getOwnPropertyDescriptor(o,s).get.name"));

    [Fact]
    public void ObjectComputedStringGetterName()
        => Assert.Equal("get ab", Eval(
            "var o={ get ['a'+'b'](){} }; Object.getOwnPropertyDescriptor(o,'ab').get.name"));

    [Fact]
    public void ClassComputedSymbolGetterName()
        => Assert.Equal("get [desc]", Eval(
            "var s=Symbol('desc'); class C { get [s](){} }; Object.getOwnPropertyDescriptor(C.prototype,s).get.name"));

    [Fact]
    public void ClassComputedStaticSymbolSetterName()
        => Assert.Equal("set [desc]", Eval(
            "var s=Symbol('desc'); class C { static set [s](v){} }; Object.getOwnPropertyDescriptor(C,s).set.name"));

    [Fact]
    public void NonComputedAccessorNamesStillWork()
        => Assert.Equal("get id,set id", Eval(
            "var o={ get id(){}, set id(v){} };" +
            "[Object.getOwnPropertyDescriptor(o,'id').get.name,Object.getOwnPropertyDescriptor(o,'id').set.name].join(',')"));

    // ---- Problem 14: Annex B block-level function hoisting ----

    [Fact]
    public void PlainFunctionInBlockStillHoists()
        => Assert.Equal("function", Eval("(function(){ { function f(){} } return typeof f; })()"));

    [Fact]
    public void GeneratorInBlockDoesNotHoist()
        => Assert.Equal("undefined", Eval("(function(){ { function* g(){} } return typeof g; })()"));

    [Fact]
    public void AsyncFunctionInBlockDoesNotHoist()
        => Assert.Equal("undefined", Eval("(function(){ { async function af(){} } return typeof af; })()"));

    [Fact]
    public void AsyncGeneratorInBlockDoesNotHoist()
        => Assert.Equal("undefined", Eval("(function(){ { async function* ag(){} } return typeof ag; })()"));

    [Fact]
    public void GeneratorInBlockIsVisibleInBlock()
        => Assert.Equal("function", Eval("(function(){ var r; { function* g(){} r = typeof g; } return r; })()"));

    [Fact]
    public void NestedBlockGeneratorDoesNotHoist()
        => Assert.Equal("undefined", Eval("(function(){ {{ function* g(){} }} return typeof g; })()"));

    // ---- Problem 22: bound function new.target remap ----

    [Fact]
    public void NewBoundFunctionRemapsNewTargetToTarget()
        => Assert.Equal("true", Eval(
            "function A(){ this.nt = new.target; } var BA = A.bind(null); (new BA()).nt === A"));

    [Fact]
    public void ReflectConstructBoundSelfRemapsNewTarget()
        => Assert.Equal("true", Eval(
            "function A(){ this.nt = new.target; } var BA = A.bind(null); Reflect.construct(BA, [], BA).nt === A"));

    [Fact]
    public void ReflectConstructBoundExplicitNewTargetPreserved()
        => Assert.Equal("true", Eval(
            "function A(){ this.nt = new.target; } function C(){} var BA = A.bind(null);" +
            "var o = Reflect.construct(BA, [], C); o.nt === C && Object.getPrototypeOf(o) === C.prototype"));

    [Fact]
    public void ChainedBoundConstructRemapsToOriginalTarget()
        => Assert.Equal("true", Eval(
            "function A(){ this.nt = new.target; } var BBA = A.bind(null).bind(null);" +
            "var o = new BBA(); o.nt === A && Object.getPrototypeOf(o) === A.prototype"));

    // ---- Problem 32/33: toUpperCase/toLowerCase special casing ----

    [Fact]
    public void SharpSUpperCasesToSS()
        => Assert.Equal("SS", Eval("'\\u00DF'.toUpperCase()"));

    [Fact]
    public void LatinLigaturesUpperCase()
        => Assert.Equal("FF,FI,FL,FFI,FFL,ST,ST", Eval(
            "['\\uFB00','\\uFB01','\\uFB02','\\uFB03','\\uFB04','\\uFB05','\\uFB06'].map(s=>s.toUpperCase()).join(',')"));

    [Fact]
    public void CapitalIWithDotLowerCases()
        => Assert.Equal("i̇", Eval("'\\u0130'.toLowerCase()"));

    [Fact]
    public void GreekIotaSubscriptUpperCase()
        => Assert.Equal("ΑΙ", Eval("'\\u1FB3'.toUpperCase()"));

    [Fact]
    public void ApostropheNAndArmenianUpperCase()
        => Assert.Equal("ʼN,ԵՒ", Eval(
            "['\\u0149','\\u0587'].map(s=>s.toUpperCase()).join(',')"));

    [Fact]
    public void LocaleUpperKeepsUnconditionalExpansion()
        => Assert.Equal("SS", Eval("'\\u00DF'.toLocaleUpperCase('en')"));

    [Fact]
    public void TurkishLocaleUpperDottedI()
        => Assert.Equal("İ", Eval("'i'.toLocaleUpperCase('tr')"));

    [Fact]
    public void OrdinaryCaseMappingUnaffected()
        => Assert.Equal("HELLO,hello", Eval("['Hello'.toUpperCase(),'Hello'.toLowerCase()].join(',')"));
}
