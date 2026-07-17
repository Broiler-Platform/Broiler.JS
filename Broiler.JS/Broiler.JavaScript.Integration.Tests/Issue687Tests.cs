using System;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/687
//
// Fixed here:
//   * Problem 10 — Under BROILER_SCRIPT_HOST, native iteration callbacks are
//                  invoked through JSFunction.InvokeCallback (added in #685 to
//                  resolve tail-call sentinels). That path bypassed the
//                  non-strict `this` coercion that InvokeFunction performs, so a
//                  sloppy-mode predicate called without a thisArg saw `this` ===
//                  undefined instead of the global object. find / findIndex /
//                  findLast / findLastIndex (and the TypedArray equivalents)
//                  failed predicate-call-this-non-strict. InvokeCallback now
//                  applies CoerceThisOnInvoke just like InvokeFunction.
//   * Problem 2 / 9 — Intl.NumberFormat resolved notation / signDisplay /
//                  compactDisplay / unitDisplay by copying the raw (live) option
//                  value at resolvedOptions() time, with no GetOption defaults,
//                  validation, or slot semantics. They are now resolved once at
//                  construction via GetOption (default + RangeError on bad value;
//                  compactDisplay only reflected when notation is "compact",
//                  unitDisplay only when style is "unit") and reflected from the
//                  stored slots, so getter call order is observed once.
//
// Out of scope (architectural / CLDR / deep parser, matching prior triage):
// the private-* brand-check families, super-*-reference-null, AnnexB eval
// binding re-init / skip-early-err, scope-param-elem-var, NumberFormat
// formatToParts signDisplay-currency / unit CLDR data, and the staging/sm
// negative SyntaxError grab-bag.
public class Issue687Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    private static string EvalScriptHost(string code)
    {
        using var ctx = new JSContext(options: new JSContextOptions { ScriptHostMode = true });
        return ctx.Eval(code).ToString();
    }

    // ---- Problem 10: non-strict callback `this` coerces to the global object ----

    [Fact]
    public void FindNonStrictPredicateThisIsGlobal()
        => Assert.Equal("true", EvalScriptHost(
            "var g = this; var seen;"
            + "[1].find(function(){ seen = this; });"
            + "String(seen === g)"));

    [Fact]
    public void FindIndexNonStrictPredicateThisIsGlobal()
        => Assert.Equal("true", EvalScriptHost(
            "var g = this; var seen;"
            + "[1].findIndex(function(){ seen = this; });"
            + "String(seen === g)"));

    [Fact]
    public void MapNonStrictCallbackThisIsGlobal()
        => Assert.Equal("true", EvalScriptHost(
            "var g = this; var seen;"
            + "[1].map(function(){ seen = this; return 1; });"
            + "String(seen === g)"));

    [Fact]
    public void StrictCallbackThisStaysUndefined()
        => Assert.Equal("true", EvalScriptHost(
            "'use strict'; var seen = 'x';"
            + "[1].find(function(){ seen = this; });"
            + "String(seen === undefined)"));

    [Fact]
    public void CallbackThisArgIsRespected()
        => Assert.Equal("true", EvalScriptHost(
            "var o = {}; var seen;"
            + "[1].find(function(){ seen = this; }, o);"
            + "String(seen === o)"));

    // ---- Problem 2 / 9: NumberFormat option resolution & reflection ----

    [Fact]
    public void NotationDefaultsToStandardAndIsAlwaysPresent()
        => Assert.Equal("standard|true", Eval(
            "var ro = new Intl.NumberFormat([], { notation: undefined }).resolvedOptions();"
            + "ro.notation + '|' + ('notation' in ro)"));

    [Fact]
    public void NotationReflectsProvidedValue()
        => Assert.Equal("scientific", Eval(
            "new Intl.NumberFormat([], { notation: 'scientific' }).resolvedOptions().notation"));

    [Fact]
    public void SignDisplayDefaultsToAuto()
        => Assert.Equal("auto", Eval(
            "new Intl.NumberFormat([], { signDisplay: undefined }).resolvedOptions().signDisplay"));

    [Fact]
    public void CompactDisplayReflectedOnlyWhenNotationIsCompact()
        => Assert.Equal("short|false", Eval(
            "var c = new Intl.NumberFormat([], { notation: 'compact' }).resolvedOptions();"
            + "var s = new Intl.NumberFormat([], { notation: 'standard', compactDisplay: 'long' }).resolvedOptions();"
            + "c.compactDisplay + '|' + ('compactDisplay' in s)"));

    [Fact]
    public void CompactDisplayGettersObservedInConstructionOrderOnce()
        => Assert.Equal("n,c", Eval(
            "var order = [];"
            + "var nf = new Intl.NumberFormat([], {"
            + "  get notation(){ order.push('n'); return 'compact'; },"
            + "  get compactDisplay(){ order.push('c'); return 'short'; } });"
            + "nf.resolvedOptions();"
            + "order.join(',')"));

    [Fact]
    public void UnitDisplayReflectedOnlyWhenStyleIsUnit()
        => Assert.Equal("short|false", Eval(
            "var u = new Intl.NumberFormat([], { style: 'unit', unit: 'hour', unitDisplay: undefined }).resolvedOptions();"
            + "var p = new Intl.NumberFormat([], { style: 'percent', unitDisplay: 'short' }).resolvedOptions();"
            + "u.unitDisplay + '|' + ('unitDisplay' in p)"));

    [Fact]
    public void InvalidEnumOptionsThrowRangeError()
        => Assert.Equal("true|true|true", Eval(
            "function bad(o){ try { new Intl.NumberFormat([], o); return false; } catch (e) { return e instanceof RangeError; } }"
            + "String(bad({ notation: 'bogus' })) + '|' +"
            + "String(bad({ signDisplay: 'nope' })) + '|' +"
            + "String(bad({ unitDisplay: 'Short' }))"));
}
