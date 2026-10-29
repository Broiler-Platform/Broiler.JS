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

    // ── update expressions go through both caches (item 2-4) ──────────────────────────
    //
    // `obj.name++` reads and writes the same property, and both halves used one assignable
    // index reference that reached NEITHER cache — measured at 0 hits and 0 misses, so the
    // counters never saw it at all, against 199 999 hits for the plain assignment beside it.
    // The read now goes through the read cache and the write-back through the store cache.
    // What must not change is the order and the count of the observable steps: the base once,
    // ToNumeric once, the getter once, the setter once, and the same value out.

    [Fact]
    public void AnIncrementOnAMemberGoesThroughBothCaches()
    {
        var (result, stats) = Measure("""
            var o = { x: 0 };
            for (var i = 0; i < 500; i++) o.x++;
            o.x;
            """);

        Assert.Equal("500", result);
        Assert.True(stats.CacheHits >= 499, $"expected read hits, got {stats.CacheHits}/{stats.CacheMisses}");
        Assert.True(stats.StoreCacheHits >= 499, $"expected store hits, got {stats.StoreCacheHits}/{stats.StoreCacheMisses}");
    }

    [Theory]
    [InlineData("var o = { x: 5 }; var r = o.x++; r + '|' + o.x;", "5|6")]
    [InlineData("var o = { x: 5 }; var r = ++o.x; r + '|' + o.x;", "6|6")]
    [InlineData("var o = { x: 5 }; var r = o.x--; r + '|' + o.x;", "5|4")]
    [InlineData("var o = { x: 5 }; var r = --o.x; r + '|' + o.x;", "4|4")]
    // ToNumeric coerces once, so a postfix update on a string yields the NUMBER.
    [InlineData("var o = { x: '1' }; var r = o.x++; (typeof r) + '|' + r + '|' + o.x;", "number|1|2")]
    [InlineData("var o = { x: 5n }; var r = o.x++; (typeof r) + '|' + r + '|' + o.x;", "bigint|5|6")]
    [InlineData("var o = { x: undefined }; var r = o.x++; r + '|' + o.x;", "NaN|NaN")]
    public void AnUpdateExpressionStillYieldsTheRightValues(string source, string expected)
        => Assert.Equal(expected, Eval(source));

    [Fact]
    public void AnUpdateThroughAWarmedSiteStillRunsTheAccessorsOnce()
        => Assert.Equal("400|400|400", Eval("""
            var gets = 0, sets = 0, last = 0;
            var proto = {
                get x() { gets++; return 1; },
                set x(v) { sets++; last = v; }
            };
            var o = Object.create(proto);
            o.own = 1;
            for (var i = 0; i < 400; i++) o.x++;
            [gets, sets, last === 2 ? 400 : last].join('|');
            """));

    [Fact]
    public void AnUpdateOnANonWritablePropertyIsRefused()
        => Assert.Equal("1|false", Eval("""
            var o = {};
            Object.defineProperty(o, 'x', { value: 1, writable: false, configurable: true });
            for (var i = 0; i < 300; i++) o.x++;
            o.x + '|' + Object.getOwnPropertyDescriptor(o, 'x').writable;
            """));

    [Fact]
    public void AnUpdateOnANonWritablePropertyThrowsInStrictMode()
        => Assert.Equal("200|200", Eval("""
            function bump(o) { 'use strict'; o.x++; }
            var made = 0, thrown = 0;
            for (var i = 0; i < 400; i++) {
                var o = { x: 1 };
                if (i >= 200) Object.freeze(o);
                try { bump(o); made++; } catch (e) { if (e instanceof TypeError) thrown++; }
            }
            made + '|' + thrown;
            """));

    [Fact]
    public void TheBaseOfAnUpdateIsEvaluatedExactlyOnce()
        => Assert.Equal("300|300", Eval("""
            var n = 0;
            var o = { x: 0 };
            function base() { n++; return o; }
            for (var i = 0; i < 300; i++) base().x++;
            n + '|' + o.x;
            """));

    [Fact]
    public void ExcludedUpdateFormsStillWork()
        => Assert.Equal("300|7,8,7|300|300", Eval("""
            // A computed key, a private field and a super member all keep the ordinary
            // reference; only their correctness is asserted here.
            var k = 'x';
            var computed = { x: 0 };
            for (var i = 0; i < 300; i++) computed[k]++;

            // `super.v++` reads from the home object's prototype and writes to the RECEIVER,
            // so every iteration reads 7 and rewrites an own 8 - the prototype never moves and
            // the read never sees what the write created.
            class P {}
            P.prototype.v = 7;
            class C extends P { bumpSuper() { return super.v++; } }
            var c = new C();
            var superLast = 0;
            for (var i = 0; i < 300; i++) superLast = c.bumpSuper();

            class Priv { #n = 0; bump() { return ++this.#n; } get n() { return this.#n; } }
            var p = new Priv();
            for (var i = 0; i < 300; i++) p.bump();

            var arr = []; arr.tag = 0;
            for (var i = 0; i < 300; i++) arr.tag++;

            [computed.x, superLast + ',' + c.v + ',' + P.prototype.v, p.n, arr.tag].join('|');
            """));

    [Fact]
    public void AnUpdateOnAProxyStillFiresBothTraps()
        => Assert.Equal("300|300|300", Eval("""
            var gets = 0, sets = 0;
            var target = { x: 0 };
            var p = new Proxy(target, {
                get: function (t, k, r) { gets++; return Reflect.get(t, k); },
                set: function (t, k, v, r) { sets++; return Reflect.set(t, k, v); }
            });
            for (var i = 0; i < 300; i++) p.x++;
            [gets, sets, target.x].join('|');
            """));

    [Fact]
    public void AnUpdateAndAPlainStoreOnTheSamePropertyAgree()
        => Assert.Equal("900", Eval("""
            var o = { x: 0 };
            for (var i = 0; i < 300; i++) { o.x++; o.x = o.x + 1; o.x++; }
            o.x;
            """));

    // ── compound assignment on a member: `obj.name op= rhs` (item 2-4) ────────────────
    //
    // The same read-modify-write as `++`, spelled with an operator and an arbitrary
    // right-hand side, and it reached neither cache for the same reason. It takes both caches
    // on exactly the twelve operators CompoundAssignmentToBinaryOperator maps; `&&=`, `||=`
    // and `??=` keep the ordinary reference, because for them the write is conditional and a
    // cached store would perform it unconditionally.
    //
    // §13.15.2 fixes the order: the old value is read BEFORE the right-hand side is
    // evaluated, so an RHS that writes the same property must not be visible to the read.

    [Fact]
    public void ACompoundAssignmentOnAMemberGoesThroughBothCaches()
    {
        var (result, stats) = Measure("""
            var o = { x: 0 };
            for (var i = 0; i < 500; i++) o.x += 2;
            o.x;
            """);

        Assert.Equal("1000", result);
        Assert.True(stats.CacheHits >= 499, $"expected read hits, got {stats.CacheHits}/{stats.CacheMisses}");
        Assert.True(stats.StoreCacheHits >= 499, $"expected store hits, got {stats.StoreCacheHits}/{stats.StoreCacheMisses}");
    }

    [Fact]
    public void AWarmedCompoundSiteKeepsTheSameArithmetic()
        => Assert.Equal("400", Eval("""
            var o = { x: 0 };
            for (var i = 0; i < 400; i++) { o.x += 2; o.x -= 1; }
            o.x;
            """));

    [Theory]
    [InlineData("var o = { x: 12 }; o.x += 3; o.x;", "15")]
    [InlineData("var o = { x: 12 }; o.x -= 3; o.x;", "9")]
    [InlineData("var o = { x: 12 }; o.x *= 3; o.x;", "36")]
    [InlineData("var o = { x: 12 }; o.x /= 3; o.x;", "4")]
    [InlineData("var o = { x: 12 }; o.x %= 5; o.x;", "2")]
    [InlineData("var o = { x: 12 }; o.x **= 2; o.x;", "144")]
    [InlineData("var o = { x: 12 }; o.x &= 10; o.x;", "8")]
    [InlineData("var o = { x: 12 }; o.x |= 3; o.x;", "15")]
    [InlineData("var o = { x: 12 }; o.x ^= 10; o.x;", "6")]
    [InlineData("var o = { x: 12 }; o.x <<= 2; o.x;", "48")]
    [InlineData("var o = { x: 12 }; o.x >>= 2; o.x;", "3")]
    [InlineData("var o = { x: -8 }; o.x >>>= 1; o.x;", "2147483644")]
    // The `+= <literal>` shapes keep the specialized helpers the uncached path used, so the
    // string/number asymmetry of `+` has to come out the same way it did before.
    [InlineData("var o = { x: 'a' }; o.x += 'b'; o.x;", "ab")]
    [InlineData("var o = { x: 'a' }; o.x += 1; o.x;", "a1")]
    [InlineData("var o = { x: 1 }; o.x += '2'; o.x;", "12")]
    [InlineData("var o = { x: 1 }; var v = 2; o.x += v; o.x;", "3")]
    [InlineData("var o = { x: 5n }; o.x *= 2n; (typeof o.x) + '|' + o.x;", "bigint|10")]
    [InlineData("var o = {}; o.x += 1; o.x;", "NaN")]
    // The assignment expression's value is the computed value.
    [InlineData("var o = { x: 5 }; var r = (o.x += 3); r + '|' + o.x;", "8|8")]
    public void ACompoundAssignmentStillYieldsTheRightValues(string source, string expected)
        => Assert.Equal(expected, Eval(source));

    [Fact]
    public void TheOldValueIsReadBeforeTheRightHandSide()
        => Assert.Equal("11|300", Eval("""
            // The RHS overwrites the very property being compounded. Reading first means the
            // write lands on 1 + 10; reading after it would land on 100 + 10.
            var o = { x: 1 };
            var n = 0;
            function rhs() { o.x = 100; n++; return 10; }
            for (var i = 0; i < 300; i++) { o.x = 1; o.x += rhs(); }
            o.x + '|' + n;
            """));

    [Fact]
    public void ARightHandSideThatMovesTheReceiversShapeStillWritesTheRightSlot()
        => Assert.Equal("11", Eval("""
            // Every iteration adds a new key, so the shape the store cache recorded on the way
            // in is never the shape it finds on the way out.
            var o = { x: 1 };
            var k = 0;
            function rhs() { o['f' + (k++)] = 1; return 10; }
            for (var i = 0; i < 300; i++) { o.x = 1; o.x += rhs(); }
            o.x;
            """));

    [Fact]
    public void ACompoundAssignmentThroughAWarmedSiteStillRunsTheAccessorsOnce()
        => Assert.Equal("400|400|400", Eval("""
            var gets = 0, sets = 0, last = 0;
            var proto = {
                get x() { gets++; return 1; },
                set x(v) { sets++; last = v; }
            };
            var o = Object.create(proto);
            o.own = 1;
            for (var i = 0; i < 400; i++) o.x += 2;
            [gets, sets, last === 3 ? 400 : last].join('|');
            """));

    [Fact]
    public void ACompoundAssignmentOnANonWritablePropertyIsRefused()
        => Assert.Equal("1|false", Eval("""
            var o = {};
            Object.defineProperty(o, 'x', { value: 1, writable: false, configurable: true });
            for (var i = 0; i < 300; i++) o.x += 5;
            o.x + '|' + Object.getOwnPropertyDescriptor(o, 'x').writable;
            """));

    [Fact]
    public void ARefusedCompoundAssignmentStillEvaluatesToTheComputedValue()
        => Assert.Equal("6|1", Eval("""
            var o = {};
            Object.defineProperty(o, 'x', { value: 1, writable: false, configurable: true });
            var r = 0;
            for (var i = 0; i < 300; i++) r = (o.x += 5);
            r + '|' + o.x;
            """));

    [Fact]
    public void ACompoundAssignmentOnANonWritablePropertyThrowsInStrictMode()
        => Assert.Equal("200|200", Eval("""
            function add(o) { 'use strict'; o.x += 1; }
            var made = 0, thrown = 0;
            for (var i = 0; i < 400; i++) {
                var o = { x: 1 };
                if (i >= 200) Object.freeze(o);
                try { add(o); made++; } catch (e) { if (e instanceof TypeError) thrown++; }
            }
            made + '|' + thrown;
            """));

    [Fact]
    public void TheBaseOfACompoundAssignmentIsEvaluatedExactlyOnce()
        => Assert.Equal("300|300", Eval("""
            var n = 0;
            var o = { x: 0 };
            function base() { n++; return o; }
            for (var i = 0; i < 300; i++) base().x += 1;
            n + '|' + o.x;
            """));

    [Fact]
    public void TheShortCircuitingCompoundFormsStillSkipTheWrite()
        => Assert.Equal("300|0|300|300", Eval("""
            // This is why `&&=`, `||=` and `??=` are excluded: whether the setter runs at all
            // depends on the value read. A cached store would run it every time.
            var g1 = 0, s1 = 0, g2 = 0, s2 = 0;
            var o = {
                get a() { g1++; return 0; }, set a(v) { s1++; },
                get b() { g2++; return 0; }, set b(v) { s2++; }
            };
            for (var i = 0; i < 300; i++) { o.a &&= 1; o.b ||= 1; }
            [g1, s1, g2, s2].join('|');
            """));

    [Fact]
    public void TheShortCircuitingCompoundFormsStillYieldTheRightValues()
        => Assert.Equal("2|7|5", Eval("""
            var o = { a: 1, b: 0, c: null };
            for (var i = 0; i < 300; i++) { o.a &&= 2; o.b &&= 99; }
            for (var i = 0; i < 300; i++) { o.a ||= 99; o.b ||= 7; }
            for (var i = 0; i < 300; i++) { o.c ??= 5; }
            [o.a, o.b, o.c].join('|');
            """));

    [Fact]
    public void ACompoundAssignmentOnAProxyStillFiresBothTraps()
        => Assert.Equal("300|300|300", Eval("""
            var gets = 0, sets = 0;
            var target = { x: 0 };
            var p = new Proxy(target, {
                get: function (t, k, r) { gets++; return Reflect.get(t, k); },
                set: function (t, k, v, r) { sets++; return Reflect.set(t, k, v); }
            });
            for (var i = 0; i < 300; i++) p.x += 1;
            [gets, sets, target.x].join('|');
            """));

    [Fact]
    public void ACompoundAssignmentShadowingAnInheritedDataPropertyReadsTheProtoOnlyOnce()
        => Assert.Equal("310|10", Eval("""
            // The first iteration reads the prototype's 10 and creates an own 11; every one
            // after that reads the own property it just made.
            function P() {}
            P.prototype.x = 10;
            var q = new P();
            for (var i = 0; i < 300; i++) q.x += 1;
            q.x + '|' + P.prototype.x;
            """));

    [Fact]
    public void ExcludedCompoundFormsStillWork()
        => Assert.Equal("300|8,8,7|300|300", Eval("""
            // A computed key, a private field and a super member keep the ordinary reference;
            // only their correctness is asserted here.
            var k = 'x';
            var computed = { x: 0 };
            for (var i = 0; i < 300; i++) computed[k] += 1;

            // As with `super.v++`: the read comes from the home object's prototype and the
            // write goes to the RECEIVER, so it reads 7 and rewrites an own 8 forever.
            class P {}
            P.prototype.v = 7;
            class C extends P { addSuper() { return super.v += 1; } }
            var c = new C();
            var superLast = 0;
            for (var i = 0; i < 300; i++) superLast = c.addSuper();

            class Priv { #n = 0; add() { return this.#n += 1; } get n() { return this.#n; } }
            var pv = new Priv();
            for (var i = 0; i < 300; i++) pv.add();

            var arr = []; arr.tag = 0;
            for (var i = 0; i < 300; i++) arr.tag += 1;

            [computed.x, superLast + ',' + c.v + ',' + P.prototype.v, pv.n, arr.tag].join('|');
            """));

    [Fact]
    public void ACompoundAssignmentOnANullishBaseThrowsBeforeTheRightHandSideRuns()
        => Assert.Equal("300|300|0", Eval("""
            // The read comes first, so a base that cannot be coerced fails before the RHS is
            // evaluated at all - the same order the assignable reference produced.
            var rhs = 0;
            function side() { rhs++; return 1; }
            var nulls = 0, undefineds = 0;
            for (var i = 0; i < 300; i++) {
                try { null.x += side(); } catch (e) { if (e instanceof TypeError) nulls++; }
                try { undefined.x += side(); } catch (e) { if (e instanceof TypeError) undefineds++; }
            }
            [nulls, undefineds, rhs].join('|');
            """));

    [Fact]
    public void ACompoundAssignmentOnAPrimitiveBaseIsRefusedThenThrowsInStrictMode()
        => Assert.Equal("2|NaN|300", Eval("""
            // The base is boxed per access, so the write lands on a wrapper nobody keeps -
            // silent in sloppy mode, a TypeError in strict.
            var s = 'ab';
            for (var i = 0; i < 300; i++) s.length += 1;

            var n = 5;
            var last = 0;
            for (var i = 0; i < 300; i++) last = (n.foo += 1);

            function add(v) { 'use strict'; v.foo += 1; }
            var thrown = 0;
            for (var i = 0; i < 300; i++) {
                try { add(5); } catch (e) { if (e instanceof TypeError) thrown++; }
            }
            [s.length, last, thrown].join('|');
            """));

    [Fact]
    public void ACompoundAssignmentOnAGetterOnlyPropertyIsRefusedThenThrowsInStrictMode()
        => Assert.Equal("300|0|300", Eval("""
            var gets = 0;
            var proto = { get x() { gets++; return 1; } };
            var o = Object.create(proto);
            for (var i = 0; i < 300; i++) o.x += 1;
            var own = Object.getOwnPropertyNames(o).length;

            function add(t) { 'use strict'; t.x += 1; }
            var thrown = 0;
            for (var i = 0; i < 300; i++) {
                try { add(Object.create(proto)); } catch (e) { if (e instanceof TypeError) thrown++; }
            }
            [gets - thrown, own, thrown].join('|');
            """));

    [Fact]
    public void NestedCompoundAssignmentsDoNotShareTheirBaseTemporary()
        => Assert.Equal("600|300|300", Eval("""
            // Two compound assignments in one expression, each with its own base temp; the
            // inner one is the outer one's right-hand side.
            var o = { x: 0, y: 0 };
            var p = { z: 0 };
            for (var i = 0; i < 300; i++) o.x += (o.y += 1) - (p.z += 1);
            [o.y + p.z, o.y, p.z].join('|');
            """));

    [Fact]
    public void ACompoundAssignmentAnUpdateAndAPlainStoreOnTheSamePropertyAgree()
        => Assert.Equal("900", Eval("""
            var o = { x: 0 };
            for (var i = 0; i < 300; i++) { o.x += 1; o.x = o.x + 1; o.x++; }
            o.x;
            """));

    // ── the transition form: a store that CREATES its property (item 2-1) ─────────────
    //
    // The entries above all describe overwriting a slot that exists. A store that adds one
    // could not hit at all — the shape recorded after the write never matches the shape the
    // next object presents before it — so a constructor assigning fields missed on every
    // field of every object it ever built (measured 0 hits against 600 000 misses). A second
    // entry form keyed on the shape BEFORE the write fixes that, and it is the more dangerous
    // of the two: replaying a creation means performing CreateDataProperty directly, which is
    // only what OrdinarySetWithOwnDescriptor would do while the receiver is extensible AND the
    // prototype chain supplies nothing for the key. Every way either can stop being true is
    // below, each after the site has been warmed on the fast path.

    [Fact]
    public void CreatingAPropertyInAConstructor_IsCached()
    {
        var (result, stats) = MeasureFirstRun("""
            function T(a, b, c) { this.a = a; this.b = b; this.c = c; }
            var last = null;
            for (var i = 0; i < 300; i++) last = new T(i, i + 1, i + 2);
            last.a + ',' + last.b + ',' + last.c;
            """);

        Assert.Equal("299,300,301", result);
        // Three creating sites, one cold miss each; everything after is a transition hit.
        Assert.True(
            stats.StoreCacheHits >= 3 * 300 - 6,
            $"expected creating stores to hit, got {stats.StoreCacheHits}/{stats.StoreCacheMisses}");
    }

    [Fact]
    public void ACreatedPropertyGetsTheDefaultAttributesAndOrder()
    {
        // CreateDataProperty means writable, enumerable and configurable — all true — and the
        // insertion order the generic path would have produced.
        Assert.Equal("true|true|true|1|a,b,c", Eval("""
            function T() { this.a = 1; this.b = 2; this.c = 3; }
            var last = null;
            for (var i = 0; i < 400; i++) last = new T();
            var d = Object.getOwnPropertyDescriptor(last, 'a');
            [d.writable, d.enumerable, d.configurable, d.value, Object.keys(last).join(',')].join('|');
            """));
    }

    [Fact]
    public void APrototypeSetterTakesTheCreationInsteadOfAnOwnProperty()
    {
        // Present from the start, so the site never installs a transition entry at all.
        Assert.Equal("400|false|99", Eval("""
            var taken = 0;
            function T() { this.x = 1; }
            Object.defineProperty(T.prototype, 'x', {
                set: function (v) { taken++; },
                get: function () { return 99; }
            });
            var last = null;
            for (var i = 0; i < 400; i++) last = new T();
            [taken, last.hasOwnProperty('x'), last.x].join('|');
            """));
    }

    [Fact]
    public void APrototypeSetterAddedAfterWarmUp_TakesTheCreation()
    {
        // The invalidation path that matters most: 200 objects get their own property, then a
        // setter appears on the chain and the remaining 200 must run it instead.
        Assert.Equal("true|true|false|false|200", Eval("""
            var taken = 0;
            function T() { this.x = 1; }
            var own = [];
            for (var i = 0; i < 400; i++) {
                if (i === 200)
                    Object.defineProperty(T.prototype, 'x', { set: function (v) { taken++; } });
                own.push(new T().hasOwnProperty('x'));
            }
            [own[0], own[199], own[200], own[399], taken].join('|');
            """));
    }

    [Fact]
    public void AnInheritedReadOnlyDataProperty_RefusesTheCreation()
    {
        Assert.Equal("false|5", Eval("""
            function T() { this.x = 1; }
            Object.defineProperty(T.prototype, 'x', { value: 5, writable: false });
            var last = null;
            for (var i = 0; i < 400; i++) last = new T();
            [last.hasOwnProperty('x'), last.x].join('|');
            """));
    }

    [Fact]
    public void AnInheritedReadOnlyDataPropertyAddedAfterWarmUp_ThrowsInStrictMode()
    {
        Assert.Equal("200|200", Eval("""
            function T() { 'use strict'; this.x = 1; }
            var made = 0, thrown = 0;
            for (var i = 0; i < 400; i++) {
                if (i === 200)
                    Object.defineProperty(T.prototype, 'x', { value: 5, writable: false });
                try { new T(); made++; } catch (e) { if (e instanceof TypeError) thrown++; }
            }
            made + '|' + thrown;
            """));
    }

    [Fact]
    public void ANonExtensibleReceiverRefusesTheCreation()
    {
        Assert.Equal("true|true|false|false", Eval("""
            function add(o) { o.x = 1; return o.hasOwnProperty('x'); }
            var own = [];
            for (var i = 0; i < 400; i++) {
                var o = {};
                if (i >= 200) Object.preventExtensions(o);
                own.push(add(o));
            }
            [own[0], own[199], own[200], own[399]].join('|');
            """));
    }

    [Fact]
    public void ANonExtensibleReceiverThrowsInStrictMode()
    {
        Assert.Equal("200|200", Eval("""
            function add(o) { 'use strict'; o.x = 1; }
            var made = 0, thrown = 0;
            for (var i = 0; i < 400; i++) {
                var o = {};
                if (i >= 200) Object.preventExtensions(o);
                try { add(o); made++; } catch (e) { if (e instanceof TypeError) thrown++; }
            }
            made + '|' + thrown;
            """));
    }

    [Fact]
    public void AFrozenReceiverRefusesTheCreation()
        => Assert.Equal("true|false", Eval("""
            function add(o) { o.x = 1; return o.hasOwnProperty('x'); }
            for (var i = 0; i < 400; i++) add({});
            var open = {}, frozen = Object.freeze({});
            add(open) + '|' + add(frozen);
            """));

    [Fact]
    public void TwoReceiversSharingAShapeButNotAPrototype_DoNotShareATransition()
    {
        // Both receivers are empty objects, so they present the same shape; only their
        // prototypes differ, and one of those prototypes has a setter for the key. An entry
        // guarded by the shape alone would create an own property on both.
        Assert.Equal("400|400|0", Eval("""
            var taken = 0;
            var plain = {};
            var trapped = {};
            Object.defineProperty(trapped, 'x', { set: function (v) { taken++; } });
            function add(o) { o.x = 1; return o.hasOwnProperty('x') ? 1 : 0; }
            var ownOnPlain = 0, ownOnTrapped = 0;
            for (var i = 0; i < 400; i++) {
                ownOnPlain += add(Object.create(plain));
                ownOnTrapped += add(Object.create(trapped));
            }
            [taken, ownOnPlain, ownOnTrapped].join('|');
            """));
    }

    [Fact]
    public void SwappingTheReceiversPrototypeAfterWarmUp_IsObserved()
        => Assert.Equal("true|false|200", Eval("""
            var taken = 0;
            var plain = {};
            var trapped = {};
            Object.defineProperty(trapped, 'x', { set: function (v) { taken++; } });
            function add(o) { o.x = 1; return o.hasOwnProperty('x'); }
            var own = [];
            for (var i = 0; i < 400; i++) {
                var o = {};
                if (i >= 200) Object.setPrototypeOf(o, trapped);
                own.push(add(o));
            }
            [own[0], own[399], taken].join('|');
            """));

    [Fact]
    public void ADunderProtoAssignmentStillRunsTheInheritedAccessor()
        => Assert.Equal("false|true|true", Eval("""
            // `__proto__` is an accessor on %Object.prototype%, so the chain is never free of
            // the key and no transition entry may be installed for it.
            var a = { marker: 1 };
            function setProto(o, v) { o.__proto__ = v; }
            var ownAny = false, allLinked = true, allMarked = true;
            for (var i = 0; i < 400; i++) {
                var o = {};
                setProto(o, a);
                if (o.hasOwnProperty('__proto__')) ownAny = true;
                if (Object.getPrototypeOf(o) !== a) allLinked = false;
                if (o.marker !== 1) allMarked = false;
            }
            [ownAny, allLinked, allMarked].join('|');
            """));

    [Fact]
    public void ADictionaryModeReceiverStillCreatesCorrectly()
        => Assert.Equal("400|400", Eval("""
            function add(o) { o.x = 1; return o.x; }
            var ok = 0, dict = 0;
            for (var i = 0; i < 400; i++) {
                ok += add({});
                var d = {};
                Object.defineProperty(d, 'k', { get: function () { return 1; } });
                dict += add(d);
            }
            ok + '|' + dict;
            """));

    [Fact]
    public void ACreatedPropertyIsVisibleToReadsEnumerationAndJson()
        => Assert.Equal("1|a|{\"a\":1}|true", Eval("""
            function T() { this.a = 1; }
            var last = null;
            for (var i = 0; i < 400; i++) last = new T();
            [last.a, Object.keys(last).join(','), JSON.stringify(last), 'a' in last].join('|');
            """));

    [Fact]
    public void DeletingACreatedPropertyAndRecreatingIt_Works()
        => Assert.Equal("1|false|1", Eval("""
            function add(o) { o.x = 1; }
            var o = null;
            for (var i = 0; i < 400; i++) { o = {}; add(o); }
            var before = o.x;
            delete o.x;
            var gone = o.hasOwnProperty('x');
            add(o);
            [before, gone, o.x].join('|');
            """));

    [Fact]
    public void AProxyReceiverStillFiresItsSetTrapForACreation()
        => Assert.Equal("400|1", Eval("""
            var traps = 0;
            function add(o) { o.x = 1; }
            for (var i = 0; i < 400; i++) add({});
            var target = {};
            var p = new Proxy(target, { set: function (t, k, v, r) { traps++; return Reflect.set(t, k, v); } });
            for (var i = 0; i < 400; i++) add(p);
            traps + '|' + target.x;
            """));

    [Fact]
    public void AProxyInThePrototypeChainStillFiresItsSetTrapForACreation()
        => Assert.Equal("400|false", Eval("""
            var traps = 0;
            var p = new Proxy({}, { set: function (t, k, v, r) { traps++; return true; } });
            function add(o) { o.x = 1; return o.hasOwnProperty('x'); }
            var own = false;
            for (var i = 0; i < 400; i++) { if (add(Object.create(p))) own = true; }
            traps + '|' + own;
            """));
}
