using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/709
//
// Fixed here:
//
//   Problem 7 — Array.prototype.find / findIndex / findLast / findLastIndex
//   skipped array holes. Per spec these methods call Get(O, k) for EVERY index
//   in [0, len) (a hole reads as undefined); they do not skip holes the way
//   forEach/map/filter do. The predicate must be invoked once per index.
//
//   Problem 8 (bind) — `var f = <expr>` performed NamedEvaluation against the
//   value of an arbitrary expression, and the "is this still the anonymous
//   placeholder?" probe read the function's `name` via the public [[Get]]. When
//   a script had redefined `name` to an accessor
//   (`var t = Object.defineProperty(function(){}, 'name', { get(){throw} })`)
//   that getter fired during the declaration — observable and wrong. The probe
//   now inspects the own data property directly, never invoking an accessor.
//
//   Problem 8 (matchAll) — RegExp.prototype[@@matchAll] derived `global` /
//   `fullUnicode` by reading "global"/"unicode"/"unicodeSets" off the
//   species-constructed matcher. Per spec (steps 9-12) they come from the flags
//   STRING; reading them off the matcher is not observable, so a throwing
//   `global`/`unicode` getter on the constructed object must not fire.
//
//   Problems 2 & 6 (dynamic super) — `super` (super.x, super[...], super(), and
//   super.x =) was resolved against the superclass captured at class definition,
//   so it ignored a later Object.setPrototypeOf(C, X) / setPrototypeOf(C.prototype,
//   X). GetSuperBase now reads the home object's CURRENT [[Prototype]] at each
//   access: the home objects (the class and its prototype) are held in JSVariable
//   heap boxes captured by-reference and filled in once the class is built, and a
//   body-less default derived constructor resolves its super() target via the
//   class's live [[Prototype]] in JSClass.CreateInstance.
//
// Out of scope (unchanged, documented in prior issues): direct-eval var
//   introduction into a function/global var-env seen by later closures (P5/P6/
//   P9/P10 eval + compound-assignment + var-env), negative-SyntaxError parser
//   grab-bag (P3), DateTimeFormat formatRange/pattern CLDR (P1/P4), sm RegExp /
//   strict / generators grab-bag (P1/P2), and the remaining super* edge cases
//   (strict super.x= , superPropNoOverwriting/Ordering) which are separate issues.
public class Issue709Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // ---- Problem 7: find family visits holes ----

    [Fact]
    public void FindVisitsHoles()
        => Assert.Equal("4", Eval("var c=0; [undefined,,,'foo'].find(function(){c++;}); c;"));

    [Fact]
    public void FindIndexVisitsHoles()
        => Assert.Equal("4", Eval("var c=0; [undefined,,,'foo'].findIndex(function(){c++;}); c;"));

    [Fact]
    public void FindLastVisitsHoles()
        => Assert.Equal("4", Eval("var c=0; [undefined,,,'foo'].findLast(function(){c++;}); c;"));

    [Fact]
    public void FindLastIndexVisitsHoles()
        => Assert.Equal("4", Eval("var c=0; [undefined,,,'foo'].findLastIndex(function(){c++;}); c;"));

    [Fact]
    public void FindHoleReadsAsUndefined()
        => Assert.Equal("undefined,0,1,2,3", Eval(
            "var seen=[]; [10,,,40].find(function(v,i){ seen.push(i); return false; }); " +
            "typeof [10,,,40][1] + ',' + seen.join(',');"));

    // ---- Problem 8: NamedEvaluation must not observe a user `name` getter ----

    [Fact]
    public void AnonymousNamingDoesNotInvokeNameGetterDuringDeclaration()
        => Assert.Equal("ok", Eval(
            "var ran=false;" +
            "var target = Object.defineProperty(function(){}, 'name', { get:function(){ ran=true; throw new Error('x'); } });" +
            "ran ? 'getter-fired' : 'ok';"));

    [Fact]
    public void BindPropagatesTargetNameGetterError()
        => Assert.Equal("true", Eval(
            "var target = Object.defineProperty(function(){}, 'name', { get:function(){ throw new Error('G'); } });" +
            "var caught=false; try { target.bind(); } catch(e){ caught = e.message==='G'; } caught;"));

    [Fact]
    public void AnonymousFunctionStillNamedFromBinding()
        => Assert.Equal("f", Eval("var f = function(){}; f.name;"));

    [Fact]
    public void ArrowFunctionStillNamedFromBinding()
        => Assert.Equal("g", Eval("var g = () => {}; g.name;"));

    [Fact]
    public void NonAnonymousRhsKeepsEmptyName()
        => Assert.Equal("", Eval("var r = (0, function(){}); r.name;"));

    // ---- Problem 8: matchAll derives flags from the flags string ----

    [Fact]
    public void MatchAllDoesNotReadGlobalOffConstructedMatcher()
        => Assert.Equal("ok", Eval(
            "var re = /./;" +
            "re.constructor = { [Symbol.species]: function(){ " +
            "  return Object.defineProperty(/./, 'global', { get(){ throw new Error('no'); } }); } };" +
            "re[Symbol.matchAll](''); 'ok';"));

    [Fact]
    public void MatchAllDoesNotReadUnicodeOffConstructedMatcher()
        => Assert.Equal("ok", Eval(
            "var re = /./;" +
            "re.constructor = { [Symbol.species]: function(){ " +
            "  return Object.defineProperty(/./, 'unicode', { get(){ throw new Error('no'); } }); } };" +
            "re[Symbol.matchAll](''); 'ok';"));

    // ---- Problem 2: TypedArray constructors require `new` (uniformly) ----

    [Fact]
    public void TypedArrayCalledWithoutNewThrows()
        => Assert.Equal("TypeError,TypeError,TypeError,TypeError,TypeError", Eval(
            "var c = Int32Array, r = [];" +
            "function t(fn){ try{ fn(); r.push('no-throw'); }catch(e){ r.push(e.name); } }" +
            "t(function(){ Int32Array(); });" +
            "t(function(){ c(); });" +
            "t(function(){ c(1); });" +
            "t(function(){ c.apply(null, []); });" +
            "t(function(){ Reflect.apply(c, null, []); });" +
            "r.join(',');"));

    [Fact]
    public void TypedArrayNewStillConstructs()
        => Assert.Equal("3|5", Eval(
            "var a = new Int32Array(3); a[0] = 5; a.length + '|' + a[0];"));

    [Fact]
    public void TypedArrayFromAndOfStillWork()
        => Assert.Equal("8|9", Eval(
            "Int32Array.from([7,8])[1] + '|' + Int32Array.of(9,10)[0];"));

    // structuredClone of a multi-byte typed array reconstructs the right element
    // length (the constructor's third argument is in elements, not bytes).
    [Fact]
    public void StructuredCloneMultiByteTypedArray()
    {
        using var ctx = new JSContext(experimentalFeatures: JavaScriptFeatureFlags.StructuredClone);
        var result = ctx.Eval(
            "var a = new Int32Array([1,2,3,4]); var c = structuredClone(a);" +
            "(c instanceof Int32Array ? 'Int32Array' : 'no') + '|' + c.length + '|' + c[1] + '|' + c[3];").ToString();
        Assert.Equal("Int32Array|4|2|4", result);
    }

    // ---- Problems 2 & 6: dynamic super resolution ----

    // super() in an explicit derived constructor targets the class's CURRENT prototype.
    [Fact]
    public void SuperCallUsesCurrentPrototypeExplicitCtor()
        => Assert.Equal("1,2", Eval(
            "class B1 { constructor(){ this.b = 1; } }" +
            "class B2 { constructor(){ this.b = 2; } }" +
            "class C extends B1 { constructor(){ super(); } }" +
            "var before = new C().b;" +
            "Object.setPrototypeOf(C, B2);" +
            "before + ',' + new C().b;"));

    // ...and for a body-less default derived constructor (handled in JSClass at runtime).
    [Fact]
    public void SuperCallUsesCurrentPrototypeDefaultCtor()
        => Assert.Equal("1,2", Eval(
            "class B1 { constructor(){ this.b = 1; } }" +
            "class B2 { constructor(){ this.b = 2; } }" +
            "class C extends B1 {}" +
            "var before = new C().b;" +
            "Object.setPrototypeOf(C, B2);" +
            "before + ',' + new C().b;"));

    // Instance super.x reads the prototype's current [[Prototype]].
    [Fact]
    public void SuperPropertyReadIsDynamic()
        => Assert.Equal("1,2", Eval(
            "class D1 { m(){ return 1; } }" +
            "class D2 { m(){ return 2; } }" +
            "class D3 extends D1 { call(){ return super.m(); } }" +
            "var before = new D3().call();" +
            "Object.setPrototypeOf(D3.prototype, D2.prototype);" +
            "before + ',' + new D3().call();"));

    // A static super.x = on a class whose [[Prototype]] is null throws a TypeError,
    // and the RHS is evaluated first (target-super-*-reference-null).
    [Fact]
    public void StaticSuperAssignToNullProtoThrowsAfterEvaluatingRhs()
        => Assert.Equal("TypeError|1", Eval(
            "var count = 0;" +
            "class C { static m(){ super.x = (count += 1); } }" +
            "Object.setPrototypeOf(C, null);" +
            "var k; try { C.m(); k = 'no-throw'; } catch (e) { k = e.name; }" +
            "k + '|' + count;"));

    // Native subclassing brand is preserved (regression guard for the JSClass change).
    [Fact]
    public void DefaultDerivedNativeSubclassKeepsBrand()
        => Assert.Equal("T", Eval(
            "class T extends Error {}" +
            "var e = new T(); e.constructor.name;"));

    // Anonymous class still receives its binding name (the home-object holder must not).
    [Fact]
    public void AnonymousClassStillNamedFromBinding()
        => Assert.Equal("cls|foo", Eval(
            "var cls = class {}; var o = { foo: class {} }; cls.name + '|' + o.foo.name;"));

    // ---- Problem 10 / direct-eval body-var introduction ----

    // A function-local var in a function that CONTAINS a direct eval must not leak to
    // the global object once the function returns (it was overlaid for the eval and
    // published as a non-configurable global property that Dispose could not delete).
    [Fact]
    public void FunctionLocalWithBodyEvalDoesNotLeakToGlobal()
        => Assert.Equal("undefined,undefined,undefined", Eval(
            "(function(){ var x = 111; eval('1'); }());" +
            "(function(){ var y = 222; eval('y;'); }());" +
            "(function(){ var z = 333; eval('var z;'); }());" +
            "typeof x + ',' + typeof y + ',' + typeof z;"));

    // A sloppy direct eval introducing a var into the calling function's var-env updates
    // that binding (no global leak); accessing the name globally afterwards is undefined.
    [Fact]
    public void EvalVarStaysLocalToCallingFunction()
        => Assert.Equal("44443|undefined", Eval(
            "var initial;" +
            "(function(){ var v = 44443; eval('initial = v; var v;'); }());" +
            "initial + '|' + typeof v;"));

    // A var introduced by a body direct eval is observed by a closure created in the
    // same function, even after the function returns (sm/regress-554955-1).
    [Fact]
    public void BodyEvalVarSeenByReturnedClosure()
        => Assert.Equal("2", Eval(
            "function f(s){ eval(s); return function(){ return b; }; }" +
            "var b = 1;" +
            "var g = f('var b = 2;');" +
            "'' + g();"));

    // The body-eval shadow must not disturb ordinary outer-variable reads when the eval
    // introduces nothing.
    [Fact]
    public void BodyEvalWithoutIntroductionKeepsOuterValue()
        => Assert.Equal("7", Eval(
            "var n = 7;" +
            "function f(){ eval('1'); return n; }" +
            "'' + f();"));
}
