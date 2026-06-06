using System;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/685
//
// Fixed here:
//   * Problem 10 — Under BROILER_SCRIPT_HOST (proper tail calls), a JS callback
//                  invoked by native code (Array.prototype.map / filter / some /
//                  every / find / forEach / reduce*, TypedArray equivalents, and
//                  the JSON reviver / replacer / getter calls) was invoked via the
//                  raw delegate `fn.f(...)`, which does NOT trampoline a tail call.
//                  A callback ending in `return g()` therefore returned a JSTailCall
//                  sentinel object instead of g()'s real value: map produced
//                  [object Object] elements, and filter/some/every/find treated the
//                  (truthy) sentinel as `true`. Routed those sites through the new
//                  JSFunction.InvokeCallback, which resolves the sentinel.
//   * Problem 9  — Intl.ListFormat.prototype.format / formatToParts were stubs that
//                  returned "" / []. Implemented CLDR list assembly (start / middle /
//                  end / pair patterns) for en and es across conjunction / disjunction
//                  / unit and long / short / narrow, iterating the argument as an
//                  iterable (arrays, custom iterators, and strings by code point).
//
// Out of scope (architectural / CLDR / deep parser work, matching prior triage):
// the private-* brand families, super-null, AnnexB eval re-init, scope-param-elem-var,
// Intl signDisplay-currency / pattern-on-calendar CLDR data, and the staging/sm
// negative SyntaxError grab-bag (regexp u-mode strictness, object/class method-def
// syntax, destructuring early errors).
public class Issue685Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // Proper tail calls only emit under BROILER_SCRIPT_HOST, and the decision is
    // made at compile time, so the flag must be set before Eval compiles the body.
    private static string EvalScriptHost(string code)
    {
        var previous = Environment.GetEnvironmentVariable("BROILER_SCRIPT_HOST");
        Environment.SetEnvironmentVariable("BROILER_SCRIPT_HOST", "1");
        try
        {
            using var ctx = new JSContext();
            return ctx.Eval(code).ToString();
        }
        finally
        {
            Environment.SetEnvironmentVariable("BROILER_SCRIPT_HOST", previous);
        }
    }

    // ---- Problem 10: tail-call callback must not leak a JSTailCall sentinel ----

    [Fact]
    public void MapCallbackWithTailCallReturnsRealValue()
        => Assert.Equal("number|5", EvalScriptHost(
            "function id(x){ return x; }"
            + "var r = [0].map(function(){ return id(5); });"
            + "(typeof r[0]) + '|' + r[0]"));

    [Fact]
    public void FilterCallbackWithFalseTailCallExcludesElement()
        => Assert.Equal("0", EvalScriptHost(
            "function id(x){ return x; }"
            + "String([1, 2, 3].filter(function(){ return id(false); }).length)"));

    [Fact]
    public void SomeAndEveryCallbackWithTailCallUseRealBoolean()
        => Assert.Equal("false|false", EvalScriptHost(
            "function id(x){ return x; }"
            + "String([0].some(function(){ return id(false); })) + '|' +"
            + "String([0].every(function(){ return id(false); }))"));

    [Fact]
    public void ForEachCallbackWithTailCallStillRunsSideEffects()
        => Assert.Equal("6", EvalScriptHost(
            "var sum = 0; function add(x){ sum += x; }"
            + "[1, 2, 3].forEach(function(x){ return add(x); });"
            + "String(sum)"));

    [Fact]
    public void GetTimezoneOffsetThroughMapIsNumeric()
        => Assert.Equal("number", EvalScriptHost(
            "function tz(t){ var d = new Date(NaN); d.setTime(t); return d.getTimezoneOffset(); }"
            + "typeof [0].map(tz)[0] === 'number' &&"
            + "  typeof [0].map(function(){ return tz(0); })[0] === 'number'"
            + "    ? 'number' : 'object'"));

    // ---- Problem 9: Intl.ListFormat.format performs CLDR list assembly ----

    [Fact]
    public void ListFormatEnglishConjunction()
        => Assert.Equal("|foo|foo and bar|foo, bar, and baz", Eval(
            "var lf = new Intl.ListFormat('en-US');"
            + "lf.format([]) + '|' + lf.format(['foo']) + '|' +"
            + "lf.format(['foo','bar']) + '|' + lf.format(['foo','bar','baz'])"));

    [Fact]
    public void ListFormatEnglishDisjunctionAndShort()
        => Assert.Equal("foo, bar, or baz|foo, bar, & baz", Eval(
            "new Intl.ListFormat('en-US', { type: 'disjunction' }).format(['foo','bar','baz']) + '|' +"
            + "new Intl.ListFormat('en-US', { style: 'short' }).format(['foo','bar','baz'])"));

    [Fact]
    public void ListFormatSpanishUnit()
        => Assert.Equal("foo, bar y baz|foo bar baz", Eval(
            "new Intl.ListFormat('es-ES', { type: 'unit', style: 'long' }).format(['foo','bar','baz']) + '|' +"
            + "new Intl.ListFormat('es-ES', { type: 'unit', style: 'narrow' }).format(['foo','bar','baz'])"));

    [Fact]
    public void ListFormatIteratesStringByCharacter()
        => Assert.Equal("f, o, and o", Eval(
            "new Intl.ListFormat('en-US').format('foo')"));
}
