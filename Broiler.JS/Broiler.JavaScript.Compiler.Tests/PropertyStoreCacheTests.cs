using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Compiler.Tests;

// The store inline cache (docs/performance-roadmap.md P1-3). `obj.name = value` with a
// constant key lowers to a cache helper instead of an assignable index reference; a hit is a
// shape-id compare, one descriptor lookup, and two writes.
//
// A store cache is easier to get wrong than a read cache, because a shape answers "which
// slot" but never "may I write to it" — making a property non-writable rewrites its
// attributes in place and deliberately keeps the shape. So the tests below fall into two
// halves: that the hot shapes actually hit, and that every way a write can legally be
// refused or redirected still is, after the site has been warmed on the fast path.
//
// Shares Phase 3's collection because PropertyOptimizationDiagnostics is a process-wide
// counter set with a process-wide Enabled switch.
[Collection(Phase3DiagnosticsCollection.Name)]
public class PropertyStoreCacheTests
{
    private static (string Result, PropertyOptimizationSnapshot Stats) Measure(string source)
    {
        using var context = new JSContext();
        // Warm: the first evaluation compiles and populates. Measuring the second keeps
        // one-time compilation misses out of the counts.
        context.Eval(source);

        using var recording = PropertyOptimizationDiagnostics.Enable();
        PropertyOptimizationDiagnostics.Reset();
        var result = context.Eval(source).ToString();
        return (result, PropertyOptimizationDiagnostics.Snapshot());
    }

    // Recording around the FIRST evaluation. The transitions below happen once in a site's
    // life, so a warm-up run would consume them before the counters were even enabled. The
    // cost is that one-time compilation misses are included, which these do not assert on.
    private static (string Result, PropertyOptimizationSnapshot Stats) MeasureFirstRun(string source)
    {
        using var context = new JSContext();
        using var recording = PropertyOptimizationDiagnostics.Enable();
        PropertyOptimizationDiagnostics.Reset();
        var result = context.Eval(source).ToString();
        return (result, PropertyOptimizationDiagnostics.Snapshot());
    }

    private static string Eval(string source)
    {
        using var context = new JSContext();
        return context.Eval(source).ToString();
    }

    // ── the writes that should hit ────────────────────────────────────────────────────

    [Theory]
    [InlineData("var o = { x: 0 };")]
    [InlineData("var o = {}; o.x = 0;")]
    [InlineData("var o = { y: 9 }; o.x = 0;")]
    [InlineData("function C() { this.x = 0; } var o = new C();")]
    [InlineData("class C { constructor() { this.x = 0; } } var o = new C();")]
    [InlineData("var o = Object.create(null); o.x = 0;")]
    [InlineData("var o = {}; Object.defineProperty(o, 'x', { value: 0, writable: true, enumerable: false, configurable: true });")]
    public void OverwritingAnOwnDataProperty_IsCached(string setup)
    {
        var (result, stats) = Measure(setup + " for (var i = 0; i < 500; i++) o.x = i; o.x;");

        Assert.Equal("499", result);
        Assert.True(stats.StoreCacheHits >= 499,
            $"expected the site to hit, got {stats.StoreCacheHits} hits / {stats.StoreCacheMisses} misses");
    }

    [Fact]
    public void WritingThroughThis_IsCached()
    {
        var (result, stats) = Measure("""
            function P() { this.x = 0; }
            P.prototype.set = function (v) { this.x = v; };
            var p = new P();
            for (var i = 0; i < 500; i++) p.set(i);
            p.x;
            """);

        Assert.Equal("499", result);
        Assert.True(stats.StoreCacheHits >= 499, $"got {stats.StoreCacheHits} hits / {stats.StoreCacheMisses} misses");
    }

    [Fact]
    public void ShadowingAnInheritedDataProperty_IsCachedAfterTheFirstWrite()
    {
        // The first store creates the own property (and the shape it lives in); every store
        // after that overwrites it, which is what the cache is for.
        var (result, stats) = Measure("""
            function P() {}
            P.prototype.x = -1;
            var p = new P();
            for (var i = 0; i < 500; i++) p.x = i;
            p.x + ',' + P.prototype.x;
            """);

        Assert.Equal("499,-1", result);
        Assert.True(stats.StoreCacheHits >= 499, $"got {stats.StoreCacheHits} hits / {stats.StoreCacheMisses} misses");
    }

    [Fact]
    public void APolymorphicSiteKeepsHittingUpToFourShapes()
    {
        var (result, stats) = Measure("""
            function w(o, v) { o.x = v; return o.x; }
            var os = [{ x: 0 }, { a: 1, x: 0 }, { b: 1, c: 2, x: 0 }, { d: 1, e: 2, f: 3, x: 0 }];
            var s = 0;
            for (var i = 0; i < 400; i++) s += w(os[i % 4], 1);
            s;
            """);

        // A retired site never hits, so the hit count is itself the proof it stayed live.
        Assert.Equal("400", result);
        Assert.True(stats.StoreCacheHits >= 396, $"got {stats.StoreCacheHits} hits / {stats.StoreCacheMisses} misses");
    }

    [Fact]
    public void AFifthShapeMakesTheSiteMegamorphic()
    {
        var (result, stats) = MeasureFirstRun("""
            function w(o) { o.x = 1; return o.x; }
            var os = [{ x: 0 }, { a: 1, x: 0 }, { b: 1, c: 2, x: 0 }, { d: 1, e: 2, f: 3, x: 0 },
                      { g: 1, h: 2, i: 3, j: 4, x: 0 }];
            var s = 0;
            for (var k = 0; k < 100; k++) s += w(os[k % 5]);
            s;
            """);

        Assert.Equal("100", result);
        Assert.True(stats.StoreMegamorphicSites >= 1, "the fifth shape should retire the site");
    }

    [Fact]
    public void ASiteThatCanNeverCacheStopsProbing()
    {
        // An inherited setter means the write never lands in an own slot. Rather than pay a
        // failed guard and a failed install on every store forever, the site gives up.
        var (result, stats) = MeasureFirstRun("""
            var proto = { set x(v) { this.seen = v; } };
            var o = Object.create(proto);
            for (var i = 0; i < 500; i++) o.x = i;
            o.seen;
            """);

        Assert.Equal("499", result);
        Assert.True(stats.StoreMegamorphicSites >= 1, "a permanently uncacheable site should retire itself");
    }

    [Fact]
    public void TwoKeysOnOneSiteStayCorrect()
        // One emitted site, two keys: the site cannot describe both, so it retires. What
        // matters is that neither key ends up written through the other's slot.
        => Assert.Equal("200,1,1", Eval("""
            function w(o, useX) { if (useX) { o.x = 1; } else { o.y = 1; } return 1; }
            var o = { x: 0, y: 0 };
            var s = 0;
            for (var i = 0; i < 200; i++) s += w(o, i % 2 === 0);
            s + ',' + o.x + ',' + o.y;
            """));

    // ── every way a write can still be refused ────────────────────────────────────────

    [Theory]
    // Each warms the site on a plain writable property first, THEN takes the write away.
    [InlineData("Object.defineProperty(o, 'x', { writable: false });", "9")]
    [InlineData("Object.freeze(o);", "9")]
    public void AWriteRefusedAfterWarmUp_IsRefused(string revoke, string expected)
        => Assert.Equal(expected, Eval(
            "var o = { x: 0 }; for (var i = 0; i <= 9; i++) o.x = i; " + revoke + " o.x = 99; o.x;"));

    [Fact]
    public void AWriteRefusedAfterWarmUp_ThrowsInStrictMode()
        => Assert.Equal("TypeError", Eval("""
            'use strict';
            var o = { x: 0 };
            for (var i = 0; i <= 9; i++) o.x = i;
            Object.freeze(o);
            (function () { try { o.x = 99; return 'no-throw'; } catch (e) { return e.constructor.name; } })();
            """));

    [Fact]
    public void ASealedObjectStaysWritable()
        => Assert.Equal("99", Eval(
            "var o = { x: 0 }; for (var i = 0; i <= 9; i++) o.x = i; Object.seal(o); o.x = 99; o.x;"));

    [Fact]
    public void RedefiningAsAnAccessorAfterWarmUp_RunsTheSetter()
        => Assert.Equal("99,9", Eval("""
            var o = { x: 0 };
            for (var i = 0; i <= 9; i++) o.x = i;
            var seen = -1;
            var last = o.x;
            Object.defineProperty(o, 'x', { get: function () { return seen; }, set: function (v) { seen = v; }, configurable: true });
            o.x = 99;
            o.x + ',' + last;
            """));

    [Fact]
    public void AddingAPrototypeSetterAfterWarmUp_DoesNotStealTheOwnWrite()
        // An own data property wins over an inherited setter, so this must keep writing the
        // own slot — the cached entry stays correct precisely because it is an OWN entry.
        => Assert.Equal("99,-1", Eval("""
            var proto = {};
            var o = Object.create(proto);
            o.x = 0;
            for (var i = 0; i <= 9; i++) o.x = i;
            var seen = -1;
            Object.defineProperty(proto, 'x', { set: function (v) { seen = v; }, configurable: true });
            o.x = 99;
            o.x + ',' + seen;
            """));

    [Fact]
    public void DeletingAfterWarmUp_RecreatesTheProperty()
        => Assert.Equal("99,y,x", Eval("""
            var o = { x: 0, y: 1 };
            for (var i = 0; i <= 9; i++) o.x = i;
            delete o.x;
            o.x = 99;
            o.x + ',' + Object.keys(o).join(',');
            """));

    [Fact]
    public void PreventingExtensionsAfterWarmUp_StillAllowsTheExistingProperty()
        => Assert.Equal("99,undefined", Eval("""
            var o = { x: 0 };
            for (var i = 0; i <= 9; i++) o.x = i;
            Object.preventExtensions(o);
            o.x = 99;
            o.z = 1;
            o.x + ',' + String(o.z);
            """));

    [Fact]
    public void ADictionaryModeObjectStillWritesCorrectly()
        => Assert.Equal("99", Eval("""
            var o = { x: 0 };
            Object.defineProperty(o, 'g', { get: function () { return 1; } });
            for (var i = 0; i <= 9; i++) o.x = i;
            o.x = 99;
            o.x;
            """));

    // ── receivers the cache must not claim ────────────────────────────────────────────

    [Fact]
    public void AProxyReceiverStillFiresItsSetTrap()
        => Assert.Equal("10,9", Eval("""
            var log = 0;
            var target = {};
            var p = new Proxy(target, { set: function (t, k, v) { log++; t[k] = v; return true; } });
            for (var i = 0; i <= 9; i++) p.x = i;
            log + ',' + target.x;
            """));

    [Fact]
    public void AProxyInThePrototypeChainStillFiresItsSetTrap()
        // Once, not ten times: the first Reflect.set creates the own property on the
        // receiver, and every store after that resolves there without reaching the chain.
        => Assert.Equal("1,true,9", Eval("""
            var log = 0;
            var p = new Proxy({}, { set: function (t, k, v, r) { log++; return Reflect.set(t, k, v, r); } });
            var o = Object.create(p);
            for (var i = 0; i <= 9; i++) o.x = i;
            log + ',' + o.hasOwnProperty('x') + ',' + o.x;
            """));

    [Theory]
    [InlineData("var o = [1, 2, 3]; for (var i = 0; i <= 9; i++) o.length = 1; o.length + ',' + o.join(',');", "1,1")]
    [InlineData("var o = new Int32Array(4); for (var i = 0; i <= 9; i++) o.tag = i; o.tag + ',' + o.length;", "9,4")]
    [InlineData("function o() {} for (var i = 0; i <= 9; i++) o.tag = i; o.tag;", "9")]
    [InlineData("var o = 'abc'; for (var i = 0; i <= 9; i++) o.tag = i; String(o.tag);", "undefined")]
    public void ExoticReceiversKeepTheirOwnSemantics(string source, string expected)
        => Assert.Equal(expected, Eval(source));

    // ── keys the cache must not key on ────────────────────────────────────────────────

    [Fact]
    public void AComputedKeyIsNotCached()
    {
        var (result, stats) = Measure("var o = { x: 0 }; var k = 'x'; for (var i = 0; i < 200; i++) o[k] = i; o.x;");

        Assert.Equal("199", result);
        Assert.Equal(0, stats.StoreCacheHits);
    }

    [Fact]
    public void AnArrayIndexNamedAsAStringIsNotCached()
    {
        var (result, stats) = Measure("var o = {}; for (var i = 0; i < 200; i++) o['0'] = i; o[0] + ',' + Object.keys(o).join(',');");

        Assert.Equal("199,0", result);
        Assert.Equal(0, stats.StoreCacheHits);
    }

    [Fact]
    public void APrivateFieldWriteKeepsItsBrandCheck()
    {
        Assert.Equal("199", Eval("""
            class C { #p = 0; set(v) { this.#p = v; return this.#p; } }
            var c = new C();
            var last = 0;
            for (var i = 0; i < 200; i++) last = c.set(i);
            last;
            """));

        Assert.Equal("TypeError", Eval("""
            class C { #p = 0; static write(o, v) { o.#p = v; } }
            (function () { try { C.write({}, 1); return 'no-throw'; } catch (e) { return e.constructor.name; } })();
            """));
    }

    [Fact]
    public void ASuperAssignmentStillTargetsTheHomeObjectsPrototype()
        => Assert.Equal("9", Eval("""
            var proto = { set x(v) { this.got = v; } };
            var o = { __proto__: proto, run: function () { for (var i = 0; i <= 9; i++) super.x = i; return this.got; } };
            o.run();
            """));

    // ── the write is still observable everywhere else ─────────────────────────────────

    [Fact]
    public void AttributesAndOrderSurviveCachedWrites()
        => Assert.Equal("a,b,c|9,true,false,true", Eval("""
            var o = { a: 0, b: 0, c: 0 };
            Object.defineProperty(o, 'b', { enumerable: false });
            for (var i = 0; i <= 9; i++) { o.a = i; o.b = i; o.c = i; }
            var d = Object.getOwnPropertyDescriptor(o, 'b');
            Object.getOwnPropertyNames(o).join(',') + '|' + [d.value, d.writable, d.enumerable, d.configurable].join(',');
            """));

    [Fact]
    public void ACachedWriteIsVisibleToAReadCacheOnTheSameProperty()
        => Assert.Equal("45", Eval(
            "var o = { x: 0 }; var s = 0; for (var i = 0; i <= 9; i++) { o.x = i; s += o.x; } s;"));

    [Fact]
    public void ACachedWriteToAPrototypeIsVisibleThroughItsInstances()
        // Writing to an object that is in use as a prototype has to publish the mutation, or
        // a read cache further down would keep serving the old value.
        => Assert.Equal("0,9", Eval("""
            var proto = { x: 0 };
            var o = Object.create(proto);
            var before = o.x;
            for (var i = 0; i <= 9; i++) proto.x = i;
            before + ',' + o.x;
            """));

    [Fact]
    public void ACachedWriteIsVisibleToJsonAndEnumeration()
        => Assert.Equal("{\"a\":9,\"b\":18}|a9b18", Eval("""
            var o = { a: 0, b: 0 };
            for (var i = 0; i <= 9; i++) { o.a = i; o.b = i * 2; }
            var s = '';
            for (var k in o) s += k + o[k];
            JSON.stringify(o) + '|' + s;
            """));

    [Fact]
    public void ADestructuringMemberTargetWritesCorrectly()
        => Assert.Equal("5,1,2", Eval("var o = {}; ({ a: o.x } = { a: 5 }); [o.y, o.z] = [1, 2]; o.x + ',' + o.y + ',' + o.z;"));

    [Fact]
    public void AForOfMemberHeadWritesCorrectly()
        => Assert.Equal("123,3", Eval("var o = {}; var s = ''; for (o.x of [1, 2, 3]) s += o.x; s + ',' + o.x;"));

    [Fact]
    public void TheAssignmentStillEvaluatesToTheAssignedValue()
        => Assert.Equal("5,5,5", Eval("var a = {}, b = {}; var r = (a.x = b.x = 5); r + ',' + a.x + ',' + b.x;"));

    [Fact]
    public void TheBaseIsEvaluatedBeforeTheValue()
        => Assert.Equal("base,value", Eval("""
            var log = [];
            function base() { log.push('base'); return {}; }
            function value() { log.push('value'); return 1; }
            base().x = value();
            log.join(',');
            """));

    [Fact]
    public void TheBaseIsEvaluatedExactlyOnce()
        => Assert.Equal("10", Eval("""
            var n = 0;
            function base() { n++; return {}; }
            for (var i = 0; i <= 9; i++) base().x = i;
            n;
            """));
}
