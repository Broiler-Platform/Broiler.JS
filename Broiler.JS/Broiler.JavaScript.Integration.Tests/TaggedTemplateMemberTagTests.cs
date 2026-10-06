using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for a tagged template whose tag is a member expression: `o.m`x``, `o[key]`x``,
// `this.#m`x``, `super.m`x``. A tagged template calls its tag as a method -- with the object the tag was
// read from as `this` (EvaluateCall over the tag's Reference) -- and its first argument is the
// template object.
//
// The compiler handled a member tag only when its key was an identifier, a string or a number, or a
// plain member read: any other computed key -- `o[f()]`x``, `o[a + b]`x``, `o[`m`]`x``, a sequence,
// `null` -- failed to compile, with a NotImplementedException naming the key. A private name was looked
// up as a public property called "#m", and a `super` tag and a parenthesised chain's tag, `(o?.m)`x``,
// were called with `undefined` as `this`.
//
// The tests run their tags inside function bodies. The receiver and the method live in temps from a
// per-function pool, and a member call inside a computed key is handed the same pair unless the tag
// takes locals of its own: on pooled temps `o[f()]`x`` ran o's method with `this` being f. Code at a
// program's top level does not show that.
public class TaggedTemplateMemberTagTests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // Runs a program that assigns its answer to `globalThis.r`, pumping the event loop so an async body
    // completes.
    private static string Drive(string body)
    {
        using var ctx = new JSContext();
        ctx.Eval("globalThis.r = '<unset>';");
        ctx.Execute(body);
        return ctx.Eval("'' + globalThis.r").ToString();
    }

    // `m` answers which object it ran on, the template's raw strings and the substitutions:
    // `o:a|b:1` for o.m`a${1}b`. A tag called on the wrong object answers 'f' or 'undefined' first.
    private const string Objects =
        "var o = { name: 'o', m: function (strings) { return (this && this.name) + ':' + strings.raw.join('|') + ':' + " +
        "[].slice.call(arguments, 1).join(','); } }; " +
        "var f = { name: 'f', key: function () { return 'm'; }, g: function () { return 1; } }; " +
        "var p = { name: 'p' }; p['null'] = p['true'] = p['1'] = p['/r/'] = o.m; ";

    private static string InFunction(string expression) =>
        Eval(Objects + "(function () { return " + expression + "; })()");

    [Theory]
    // What compiled before, and must still answer the same.
    [InlineData("o.m`a${1}b`", "o:a|b:1")]
    [InlineData("o['m']`x`", "o:x:")]
    [InlineData("p[1]`x`", "p:x:")]
    // Computed keys of every other kind, which did not compile.
    [InlineData("o[f.key()]`a${1}b`", "o:a|b:1")]
    [InlineData("o[(f.g(), 'm')]`x`", "o:x:")]
    [InlineData("o[f.g() ? 'm' : 'n']`x`", "o:x:")]
    [InlineData("o['' + 'm']`x`", "o:x:")]
    [InlineData("o[`m`]`x`", "o:x:")]
    [InlineData("p[null]`x`", "p:x:")]
    [InlineData("p[true]`x`", "p:x:")]
    [InlineData("p[1n]`x`", "p:x:")]
    [InlineData("p[/r/]`x`", "p:x:")]
    [InlineData("(() => o[f.key()]`x`)()", "o:x:")]
    public void AMemberTagIsCalledOnTheObjectItWasReadFrom(string expression, string expected)
        => Assert.Equal(expected, InFunction(expression));

    [Fact(Timeout = 600000)]
    public void APrivateMethodTagIsTheClassesOwnMethod()
        // `#m` is the private method, not a public property named "#m", and it runs on the instance --
        // as a field holding a function and an accessor answering one do.
        => Assert.Equal("self,self,self", Eval(
            "class C { #m() { return this === c ? 'self' : 'other'; }" +
            " #f = function () { return this === c ? 'self' : 'other'; };" +
            " get #g() { return function () { return this === c ? 'self' : 'other'; }; }" +
            " t() { return [this.#m`x`, this.#f`x`, this.#g`x`].join(','); } }" +
            " var c = new C(); c.t()"));

    [Fact(Timeout = 600000)]
    public void ASuperTagRunsOnThis()
        // The superclass's method, called with the caller's `this` as a super call is -- also through a
        // computed key, and from an arrow function inside the method.
        => Assert.Equal("self,self,self", Eval(
            Objects +
            "class A { m() { return this === b ? 'self' : String(this); } }" +
            " class B extends A { t() { return [super.m`x`, super[(f.g(), 'm')]`x`, (() => super.m`x`)()].join(','); } }" +
            " var b = new B(); b.t()"));

    [Fact(Timeout = 600000)]
    public void AParenthesisedChainTagRunsOnItsObjectOrThrowsWhereItShortCircuits()
        // `(o?.m)`x`` calls o.m on o, as `(o?.m)()` does -- through a deeper link too. Where the chain
        // short-circuits, the parens give `undefined` as the tag, and calling it is a TypeError thrown
        // after the substitution has run, as Chromium throws it; it used to run with `this` undefined.
        => Assert.Equal("o:x:|o:x:|sub,TypeError|sub,TypeError", InFunction(
            "(function () { var q = { inner: o }, n = null, log = [], out = [(o?.m)`x`, (q?.inner.m)`x`];" +
            " try { (n?.m)`x${log.push('sub')}`; out.push('no throw'); } catch (e) { out.push(log.join(',') + ',' + e.name); }" +
            " log = []; try { (n?.inner.m)`x${log.push('sub')}`; out.push('no throw'); } catch (e) { out.push(log.join(',') + ',' + e.name); }" +
            " return out.join('|'); })()"));

    [Fact(Timeout = 600000)]
    public void TheTagIsReadBeforeTheSubstitutionsAreEvaluated()
        // The receiver, then the key, then the method read, then each substitution, then the call
        // (EvaluateCall after ArgumentListEvaluation of the template) -- with the key's call and the
        // substitution's call each run once.
        => Assert.Equal("recv,key,get,sub,call:R", Eval(
            "var log = [];" +
            " var target = { tag: 'R', get m() { log.push('get'); return function () { log.push('call:' + this.tag); }; } };" +
            " function recv() { log.push('recv'); return target; }" +
            " var s = { key: function () { log.push('key'); return 'm'; }, sub: function () { log.push('sub'); return 1; } };" +
            " (function () { recv()[s.key()]`a${s.sub()}b`; })(); log.join(',')"));

    [Fact(Timeout = 600000)]
    public void AComputedTagKeepsOneTemplateObjectPerSite()
        => Assert.Equal("true,false", Eval(
            Objects +
            "var seen = []; var t = { m: function (s) { seen.push(s); } };" +
            " function run() { t[f.key()]`x`; } run(); run(); t[f.key()]`x`;" +
            " [seen[0] === seen[1], seen[1] === seen[2]].join(',')"));

    [Theory]
    // A nested member call in a substitution or the key, which the generator rewrite hoists, and a
    // suspension in either.
    [InlineData("globalThis.r = o.m`${['a'].join('+')}`;", "o:|:a")]
    [InlineData("globalThis.r = o.m`${await Promise.resolve(2)}`;", "o:|:2")]
    [InlineData("globalThis.r = o[f.key()]`${await Promise.resolve(3)}`;", "o:|:3")]
    [InlineData("globalThis.r = o[await Promise.resolve('m')]`x`;", "o:x:")]
    public void AMemberTagInAnAsyncFunctionIsCalledOnItsObject(string statement, string expected)
        => Assert.Equal(expected, Drive(Objects + "(async function () { " + statement + " })();"));

    [Fact(Timeout = 600000)]
    public void TheOrderHoldsInAnAsyncFunction()
        // The rewrite that turns an async body into a state machine must not move the key or a
        // substitution ahead of the receiver.
        => Assert.Equal("recv,key,get,sub,call:R", Drive(
            "var log = [];" +
            " var target = { tag: 'R', get m() { log.push('get'); return function () { log.push('call:' + this.tag); }; } };" +
            " function recv() { log.push('recv'); return target; }" +
            " var s = { key: function () { log.push('key'); return 'm'; }, sub: function () { log.push('sub'); return Promise.resolve(1); } };" +
            " (async function () { recv()[s.key()]`a${await s.sub()}b`; globalThis.r = log.join(','); })();"));

    [Fact(Timeout = 600000)]
    public void AMemberTagInAGeneratorIsCalledOnItsObject()
        => Assert.Equal("o:|:5", Eval(
            Objects +
            "function* g() { return o[f.key()]`${yield 1}`; } var it = g(); it.next(); '' + it.next(5).value"));
}
