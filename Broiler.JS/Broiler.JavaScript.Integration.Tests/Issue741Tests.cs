using Broiler.JavaScript.Engine;
using Xunit;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/741
//
// Fixed here:
//
//   Problems 5/8/11/12 (String.prototype match/replace/search/split with a null
//   custom symbol method) — String.prototype.{match,replace,search,split} look up the
//   custom Symbol.{match,replace,search,split} method via the GetMethod abstract op,
//   which returns undefined for BOTH undefined AND null. A null custom method must be
//   treated as absent (fall through to the default RegExp path), not raise
//   "@@... is not callable". The guards now test IsNullOrUndefined.
//
//   Problem 13 (JSON.stringify of a BigInt with a toJSON method) — SerializeJSONProperty
//   reads toJSON when Type(value) is Object OR BigInt (the BigInt proposal). ToJson only
//   handled objects, so a BigInt always hit "Do not know how to serialize a BigInt".
//   It now resolves BigInt.prototype.toJSON via GetV (with the BigInt as receiver) and
//   serializes the returned value.
//
//   Problem 14 (Promise.prototype.then with non-callable handlers) — PerformPromiseThen
//   treats a non-callable onFulfilled/onRejected as undefined (an identity / re-thrower
//   pass-through); it must NOT throw "Parameter for then is not a function".
//
//   Problems 16/17 (String.prototype.indexOf/lastIndexOf coercion order) — the spec
//   order is ToString(this), then ToString(searchString), then ToInteger/ToNumber(position).
//   Both methods coerced position before reading searchString, so an exception from the
//   search argument was masked by one from the position argument.
//
//   Problem 18 (`return` in eval/global code) — a `return` statement is only valid inside
//   a function body. At the top level of a Script/Module/eval (parser functionDepth == 0)
//   it is now an early SyntaxError, even for a direct eval invoked from within a function
//   (eval code is not itself a function body). `new Function('return …')` is unaffected
//   because its body is parsed inside a synthesised function expression.
//
// Out of scope (architectural / generated-code / Stage-3 / CLDR): P1 sm/eval
// exhaustive ReferenceError; P2 super-call-in-arrow-eval this-init; P6 Unicode
// Script_Extensions data; P15 DateTimeFormat hour12 derivation (Intl resolution stub);
// P24 duplicate named-groups; P26-P30 property-enumeration order; P19 labeled-continue
// nested-loop codegen.
public class Issue741Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // ---- Problem 5: String.prototype.match with a null Symbol.match ----

    [Fact]
    public void MatchWithNullSymbolMatchFallsThroughToRegExp()
        => Assert.Equal("true|3", Eval(
            "var re={};re[Symbol.match]=null;re.toString=function(){return '\\\\d';};" +
            "(String('abc').match(re)===null)+'|'+String('ab3c').match(re)[0]"));

    // ---- Problem 8: String.prototype.replace with a null Symbol.replace ----

    [Fact]
    public void ReplaceWithNullSymbolReplaceFallsThrough()
        => Assert.Equal("aXc", Eval(
            "var re={};re[Symbol.replace]=null;re.toString=function(){return 'b';};" +
            "'abc'.replace(re,'X')"));

    // ---- Problem 11: String.prototype.search with a null Symbol.search ----

    [Fact]
    public void SearchWithNullSymbolSearchFallsThrough()
        => Assert.Equal("-1|2", Eval(
            "var re={};re[Symbol.search]=null;re.toString=function(){return '\\\\d';};" +
            "[String('abc').search(re), String('ab3c').search(re)].join('|')"));

    // ---- Problem 12: String.prototype.split with a null Symbol.split ----

    [Fact]
    public void SplitWithNullSymbolSplitFallsThrough()
        => Assert.Equal("a,c", Eval(
            "var re={};re[Symbol.split]=null;re.toString=function(){return 'b';};" +
            "'abc'.split(re).join(',')"));

    // ---- Problem 13: JSON.stringify of a BigInt honours toJSON ----

    [Fact]
    public void JsonStringifyBigIntUsesToJson()
        => Assert.Equal("\"0\"", Eval(
            "BigInt.prototype.toJSON=function(){return this.toString();};JSON.stringify(0n)"));

    // (The companion test value-bigint-tojson-receiver.js, which asserts a strict
    // getter on BigInt.prototype sees the unboxed BigInt as `this`, depends on
    // primitive-this not being boxed for a strict accessor — a separate concern from
    // the BigInt-toJSON lookup fixed here.)

    // ---- Problem 14: Promise.prototype.then ignores non-callable handlers ----

    [Fact]
    public async System.Threading.Tasks.Task PromiseThenNonCallableHandlersAreIgnored()
    {
        using var ctx = new JSContext();
        // then(3, 5): both handlers non-callable => identity pass-through, no throw.
        var result = ctx.Eval(
            "Promise.resolve('arg').then(3, 5).then(function(v){ return 'got:' + v; })");
        var promise = Assert.IsType<Broiler.JavaScript.BuiltIns.Promise.JSPromise>(result);
        var settled = await promise.Task;
        Assert.Equal("got:arg", settled.ToString());
    }

    // ---- Problems 16/17: indexOf/lastIndexOf coerce searchString before position ----

    [Fact]
    public void IndexOfCoercesSearchStringBeforePosition()
        => Assert.Equal("intostr", Eval(
            "var s={toString:function(){throw 'intostr';}};" +
            "var p={valueOf:function(){throw 'intoint';}};" +
            "try{'abc'.indexOf(s,p);'no throw';}catch(e){e}"));

    [Fact]
    public void LastIndexOfCoercesSearchStringBeforePosition()
        => Assert.Equal("intostr", Eval(
            "var s={toString:function(){throw 'intostr';}};" +
            "var p={valueOf:function(){throw 'intoint';}};" +
            "try{'abc'.lastIndexOf(s,p);'no throw';}catch(e){e}"));

    // ---- Problem 18: `return` at the top level of eval/global code is a SyntaxError ----

    [Fact]
    public void ReturnInDirectEvalThrowsSyntaxError()
        => Assert.Equal("true", Eval(
            "try{eval('return;');'no throw';}catch(e){(e instanceof SyntaxError)+''}"));

    [Fact]
    public void ReturnInEvalFromWithinFunctionThrowsSyntaxError()
        => Assert.Equal("true", Eval(
            "function f(){return eval('return;');}" +
            "try{f();'no throw';}catch(e){(e instanceof SyntaxError)+''}"));

    [Fact]
    public void ReturnInIndirectEvalThrowsSyntaxError()
        => Assert.Equal("true", Eval(
            "var g=eval;try{g('return 1;');'no throw';}catch(e){(e instanceof SyntaxError)+''}"));

    // `return` remains valid inside every kind of function body.

    [Fact]
    public void ReturnStillValidInsideFunctionsArrowsAndMethods()
        => Assert.Equal("1|2|3|4", Eval(
            "function a(){return 1;}" +
            "var b=()=>{return 2;};" +
            "var o={m(){return 3;}};" +
            "var c=new Function('return 4;');" +
            "[a(),b(),o.m(),c()].join('|')"));
}
