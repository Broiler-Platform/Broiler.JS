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
// Out of scope (architectural / generated-code / Stage-3): P1 sm/eval ReferenceError;
// P2 IL-backend super-property destructuring target; P3-P12 class decorators and
// `accessor` auto-accessors; P15 String.prototype as a String exotic (length getter);
// P16 super-call-in-arrow-eval this-init; P18 callable %Function.prototype%.
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
}
