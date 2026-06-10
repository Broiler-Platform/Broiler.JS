using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/737
//
// Fixed here:
//
//   Problem 13 (multi-line comment ASI) — a MultiLineComment containing a line
//   terminator must act as a LineTerminator for ASI. The scanner emitted the right
//   token type but SkipMultilineComment returned via ReadSymbol, which Consume()'d
//   one extra character — swallowing the first char of the following token (e.g. the
//   `v` of `var`, or a closing `'`). It now commits the token directly.
//
//   Problem 14 (Promise.prototype.finally / catch on a thenable) — these are generic
//   per spec (require only that `this` is an Object, then Invoke this.then). They were
//   instance methods, so the generated wrapper cast `this` to JSPromise and threw
//   "Failed to convert this to JSPromise". Now [JSPrototypeMethod] statics.
//
//   Problem 17 (Array.from element store) — must CreateDataPropertyOrThrow, not Set,
//   so a non-writable existing target property is overwritten by a fresh
//   writable/enumerable/configurable data property instead of throwing.
//
//   Problem 19 (JSON.stringify toJSON) — a present-but-non-callable `toJSON` must be
//   ignored, not throw "toJSON is not a function". SerializeJSONProperty uses Get +
//   IsCallable, not the GetMethod abstract op.
//
//   Problem 20 (private field after optional chain, `o?.c.#f`) — an optional chain
//   short-circuits the whole rest of the chain when the base is nullish. The parser
//   now propagates the optional flag to every subsequent member/index/call link, so a
//   trailing `.#f` / `.d` / `()` after a `?.` yields undefined on a nullish base
//   instead of reading through it ("Cannot get property ... of undefined").
//
//   Problem 15 (String.prototype.length, "Failed to convert this to JSString") —
//   String.prototype is a String exotic object whose [[StringData]] is "" (typeof
//   "object", own length 0). It was an ordinary object, so reading length invoked the
//   JSString length accessor whose `this` cast rejected the plain-object prototype.
//   The JSClassGenerator now makes String.prototype a primitive String wrapper (as it
//   already does for Array.prototype). JSString also resolves its own "length" on the
//   dynamic-key read path so `s["length"]` no longer falls through to that prototype.
//
//   Problem 18 (callable %Function.prototype%, "Object is not a function") —
//   Function.prototype must itself be a built-in function object: typeof "function",
//   callable, accepts any arguments and returns undefined, not a constructor, with own
//   length 0 and name "". It was an ordinary object. The JSClassGenerator now makes it
//   a no-op callable JSFunction (mirroring the Array/String prototype special-cases);
//   its [[Prototype]] (Object.prototype) is wired up later in JSContext.
//
//   Problem 2 (computed super-property as a destructuring / for-head assignment
//   target, NotImplementedException in ILCodeGenerator.VisitAssign) — a `super[key]`
//   read spills the key into a temp and yields a Block (key-before-GetSuperBase
//   ordering), which is not an assignable reference, so the IL backend rejected the
//   Block-typed assignment left. The destructuring lowering now builds the super-index
//   reference directly for a computed-super leaf (as the direct `super[key] = v` path
//   already did), so `for (super[k] of/in …)` and `({a: super[k]} = …)` work.
//
// Out of scope (architectural / generated-code / Stage-3): P1 sm/eval ReferenceError;
// P3-P12 class decorators and `accessor` auto-accessors; P16 super-call-in-arrow-eval
// this-init. (sm/destructuring/order-super.js no longer crashes but still fails on a
// pre-existing, general IteratorClose behaviour — the engine calls a not-yet-done
// iterator's `return` after a fixed-length array pattern, which that SpiderMonkey test
// predates; it is independent of super and of this fix.)
public class Issue737Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // ---- Problem 13: multi-line comment with a line terminator triggers ASI ----

    [Fact]
    public void MultiLineCommentActsAsLineTerminatorBetweenStrings()
        => Assert.Equal("b", Eval("'a'/*\n*/'b'"));

    [Fact]
    public void MultiLineCommentDoesNotSwallowFollowingVarKeyword()
        => Assert.Equal("3", Eval("var a=1/*\n*/var b=2\na+b"));

    // ---- Problem 14: finally / catch work on any thenable, not just JSPromise ----

    [Fact]
    public void PromiseFinallyOnThenableReturnsThenResult()
        => Assert.Equal("true", Eval(
            "var t={};var T=function(){};T.prototype.then=function(){return t;};" +
            "(Promise.prototype.finally.call(new T())===t)+''"));

    [Fact]
    public void PromiseCatchOnThenableForwardsToThen()
        => Assert.Equal("true", Eval(
            "var T=function(){};T.prototype.then=function(s,f){return f;};" +
            "(Promise.prototype.catch.call(new T(), 7)===7)+''"));

    // ---- Problem 17: Array.from overwrites a non-writable target property ----

    [Fact]
    public void ArrayFromOverwritesNonWritableElementWithDataProperty()
        => Assert.Equal("2,true,true,true", Eval(
            "var items=function*(){yield 2;};" +
            "var A=function(){Object.defineProperty(this,'0',{value:1,writable:false,enumerable:false,configurable:true});};" +
            "var res=Array.from.call(A, items());" +
            "var d=Object.getOwnPropertyDescriptor(res,'0');" +
            "[res[0],d.writable,d.enumerable,d.configurable].join(',')"));

    // ---- Problem 19: non-callable toJSON is ignored ----

    [Fact]
    public void JsonStringifyIgnoresNonCallableToJSON()
        => Assert.Equal("{\"toJSON\":null}|{\"toJSON\":false}|{\"toJSON\":[]}|{\"toJSON\":{}}", Eval(
            "[JSON.stringify({toJSON:null})," +
            "JSON.stringify({toJSON:false})," +
            "JSON.stringify({toJSON:[]})," +
            "JSON.stringify({toJSON:/re/})].join('|')"));

    // ---- Problem 20: optional chain short-circuit propagates to a trailing access ----

    [Fact]
    public void PrivateFieldAfterOptionalChainShortCircuits()
        => Assert.Equal("Test262,,", Eval(
            "var C=class{#f='Test262';method(o){return o?.c.#f;}};var c=new C();" +
            "[c.method({c:c}), c.method(null), c.method(undefined)].join(',')"));

    [Fact]
    public void PrivateFieldAfterOptionalChainBrandChecksNonNullishBase()
        => Assert.Equal("true", Eval(
            "var C=class{#f=1;method(o){return o?.c.#f;}};var c=new C();" +
            "var threw=false;try{c.method({c:{}});}catch(e){threw=(e instanceof TypeError);}threw+''"));

    [Fact]
    public void OptionalChainTrailingMemberShortCircuits()
        => Assert.Equal("undefined,undefined", Eval(
            "[String(undefined?.c.d), String(null?.c.d)].join(',')"));

    // ---- Problem 15: String.prototype is a String exotic (typeof object, length 0) ----

    [Fact]
    public void StringPrototypeIsObjectWithNumberLength()
        => Assert.Equal("object,number,0", Eval(
            "[typeof String.prototype, typeof String.prototype.length, String.prototype.length].join(',')"));

    [Fact]
    public void StringPrototypeMethodsRemainCallable()
        => Assert.Equal("function,b,3", Eval(
            "[typeof String.prototype.charAt, 'abc'.charAt(1), 'abc'.length].join(',')"));

    [Fact]
    public void StringDynamicLengthKeyResolvesOwnLength()
        => Assert.Equal("function 3", Eval(
            "var k='charAt'; var l='length'; typeof 'abc'[k] + ' ' + 'abc'[l]"));

    [Fact]
    public void StringPrototypeLengthIsReadonlyDataPropertyZero()
        => Assert.Equal("0,false", Eval(
            "var d=Object.getOwnPropertyDescriptor(String.prototype,'length'); [d.value, d.writable].join(',')"));

    [Fact]
    public void StringPrototypeInheritsObjectPrototype()
        => Assert.Equal("true", Eval(
            "(Object.getPrototypeOf(String.prototype)===Object.prototype)+''"));

    // ---- Problem 18: %Function.prototype% is a callable no-op function ----

    [Fact]
    public void FunctionPrototypeIsCallableReturningUndefined()
        => Assert.Equal("undefined,undefined", Eval(
            "var x; [String(Function.prototype(x)), String(Function.prototype(1,2,3))].join(',')"));

    [Fact]
    public void FunctionPrototypeTypeofIsFunctionWithEmptyNameZeroLength()
        => Assert.Equal("function,,0", Eval(
            "[typeof Function.prototype, Function.prototype.name, Function.prototype.length].join(',')"));

    [Fact]
    public void FunctionPrototypeIsNotAConstructor()
        => Assert.Equal("true", Eval(
            "var t=false; try{ new Function.prototype(); }catch(e){ t=(e instanceof TypeError); } t+''"));

    [Fact]
    public void FunctionPrototypeHasNoOwnPrototypeAndInheritsObjectPrototype()
        => Assert.Equal("false,true", Eval(
            "[Function.prototype.hasOwnProperty('prototype'), Object.getPrototypeOf(Function.prototype)===Object.prototype].join(',')"));

    [Fact]
    public void FunctionsStillInheritFromFunctionPrototype()
        => Assert.Equal("true,true", Eval(
            "function f(){} [Object.getPrototypeOf(f)===Function.prototype, typeof f.call==='function'].join(',')"));

    // ---- Problem 2: computed super-property as a destructuring / for-head target ----

    [Fact]
    public void ComputedSuperPropertyAsForOfTarget()
        => Assert.Equal("2,2", Eval(
            "var obj={ m(){ var hits=0; for (super['prop'] of [1,2]) hits++; return this.prop+','+hits; } }; obj.m()"));

    [Fact]
    public void ComputedSuperPropertyAsForInTarget()
        => Assert.Equal("b,2", Eval(
            "var obj={ m(){ var hits=0; for (super['prop'] in {a:1,b:2}) hits++; return this.prop+','+hits; } }; obj.m()"));

    [Fact]
    public void ComputedSuperPropertyAsObjectDestructuringTarget()
        => Assert.Equal("9", Eval(
            "var obj={ m(){ ({a: super['prop']} = {a: 9}); return this.prop; } }; String(obj.m())"));

    [Fact]
    public void ComputedSuperPropertyAsArrayDestructuringTarget()
        => Assert.Equal("5", Eval(
            "var obj={ m(){ [super['prop']] = [5]; return this.prop; } }; String(obj.m())"));
}
