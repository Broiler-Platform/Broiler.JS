using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Compiler.Tests;

// The object-shape layout and the property inline cache (docs/performance-roadmap.md P1).
// Two things are pinned here: that ordinary writes no longer drop an object into dictionary
// mode — so constructor-assigned fields and inherited methods are cacheable at all — and that
// every way a cached lookup can go stale still produces the right value.
//
// Shares Phase 3's collection because PropertyOptimizationDiagnostics is a process-wide
// counter set with a process-wide Enabled switch: two classes reading it concurrently would
// see each other's hits, and either one's Reset() would zero the other's.
[Collection(Phase3DiagnosticsCollection.Name)]
public class PropertyShapeCacheTests
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

    private static string Eval(string source)
    {
        using var context = new JSContext();
        return context.Eval(source).ToString();
    }

    // ── the shape must survive the ways a property actually gets created ──────────────

    [Theory]
    // An object literal was the ONLY one of these that stayed cacheable before P1-1.
    [InlineData("var o = { x: 1 };")]
    [InlineData("var o = {}; o.x = 1;")]
    [InlineData("var o = {}; Object.defineProperty(o, 'x', { value: 1, writable: true, enumerable: true, configurable: true });")]
    [InlineData("function C() { this.x = 1; } var o = new C();")]
    [InlineData("class C { constructor() { this.x = 1; } } var o = new C();")]
    [InlineData("var o = { y: 0 }; o.x = 1;")]
    [InlineData("var o = Object.create(null); o.x = 1;")]
    public void OwnPropertyRead_IsCached_HoweverThePropertyWasCreated(string setup)
    {
        var (result, stats) = Measure(setup + " var s = 0; for (var i = 0; i < 500; i++) s += o.x; s;");

        Assert.Equal("500", result);
        Assert.True(stats.CacheHits >= 499, $"expected the site to hit, got {stats.CacheHits} hits / {stats.CacheMisses} misses");
    }

    [Fact]
    public void InheritedDataRead_IsCached()
    {
        var (result, stats) = Measure("""
            var proto = { k: 5 };
            var o = Object.create(proto);
            o.own = 1;
            var s = 0;
            for (var i = 0; i < 500; i++) s += o.k;
            s;
            """);

        Assert.Equal("2500", result);
        Assert.True(stats.CacheHits >= 499, $"expected hits, got {stats.CacheHits}/{stats.CacheMisses}");
    }

    [Fact]
    public void InheritedMethodCall_IsCached()
    {
        // The callee read of `p.get()` goes through the same cache a bare `p.get` does.
        var (result, stats) = Measure("""
            function P(v) { this.v = v; }
            P.prototype.get = function () { return this.v; };
            var p = new P(7);
            var s = 0;
            for (var i = 0; i < 500; i++) s += p.get();
            s;
            """);

        Assert.Equal("3500", result);
        // Two cached reads per iteration: the `get` callee, and `this.v` inside the body.
        Assert.True(stats.CacheHits >= 999, $"expected hits, got {stats.CacheHits}/{stats.CacheMisses}");
    }

    [Fact]
    public void ClassMethodCall_IsCached()
    {
        var (result, stats) = Measure("""
            class C { constructor(v) { this.v = v; } getV() { return this.v; } }
            var c = new C(3);
            var s = 0;
            for (var i = 0; i < 500; i++) s += c.getV();
            s;
            """);

        Assert.Equal("1500", result);
        Assert.True(stats.CacheHits >= 999, $"expected hits, got {stats.CacheHits}/{stats.CacheMisses}");
    }

    [Fact]
    public void OrdinaryWrite_DoesNotFallBackToDictionaryMode()
    {
        var (_, stats) = Measure("""
            function C() { this.a = 1; this.b = 2; }
            var last;
            for (var i = 0; i < 200; i++) last = new C();
            last.a + last.b;
            """);

        Assert.Equal(0, stats.DictionaryFallbacks);
    }

    // ── every way a cached lookup can go stale ────────────────────────────────────────

    [Fact]
    public void OwnPropertyAddedMidLoop_ShadowsTheInheritedOne()
    {
        Assert.Equal("800", Eval("""
            var proto = { k: 1 };
            var o = Object.create(proto);
            o.own = 1;
            var s = 0;
            for (var i = 0; i < 400; i++) { if (i === 200) o.k = 3; s += o.k; }
            s;
            """));
    }

    [Fact]
    public void PrototypePropertyMutatedMidLoop_IsObserved()
    {
        Assert.Equal("800", Eval("""
            var proto = { k: 1 };
            var o = Object.create(proto);
            o.own = 1;
            var s = 0;
            for (var i = 0; i < 400; i++) { if (i === 200) proto.k = 3; s += o.k; }
            s;
            """));
    }

    [Fact]
    public void SetPrototypeOfMidLoop_IsObserved()
    {
        Assert.Equal("800", Eval("""
            var a = { k: 1 };
            var b = { k: 3 };
            var o = Object.create(a);
            o.own = 1;
            var s = 0;
            for (var i = 0; i < 400; i++) { if (i === 200) Object.setPrototypeOf(o, b); s += o.k; }
            s;
            """));
    }

    [Fact]
    public void DeletingAnOwnProperty_RevealsTheInheritedOne()
    {
        Assert.Equal("1200", Eval("""
            var proto = { k: 5 };
            var o = Object.create(proto);
            o.k = 1;
            o.own = 1;
            var s = 0;
            for (var i = 0; i < 400; i++) { if (i === 200) delete o.k; s += o.k; }
            s;
            """));
    }

    [Fact]
    public void MethodRedefinedOnThePrototypeMidLoop_IsObserved()
    {
        Assert.Equal("600", Eval("""
            function P() {}
            P.prototype.m = function () { return 1; };
            var p = new P();
            var s = 0;
            for (var i = 0; i < 400; i++) { if (i === 200) P.prototype.m = function () { return 2; }; s += p.m(); }
            s;
            """));
    }

    [Fact]
    public void OwnMethodShadowingThePrototypeMidLoop_IsObserved()
    {
        Assert.Equal("1200", Eval("""
            function P() {}
            P.prototype.m = function () { return 1; };
            var p = new P();
            var s = 0;
            for (var i = 0; i < 400; i++) { if (i === 200) p.m = function () { return 5; }; s += p.m(); }
            s;
            """));
    }

    [Fact]
    public void TwoReceiversSharingAShapeButNotAPrototype_StayDistinct()
    {
        // x and y reach the same shape by the same key sequence, so a guard keyed on the
        // shape id alone would confuse their prototypes.
        Assert.Equal("1200", Eval("""
            var a = { k: 1 };
            var b = { k: 3 };
            var x = Object.create(a); x.v = 0;
            var y = Object.create(b); y.v = 0;
            var s = 0;
            for (var i = 0; i < 300; i++) s += x.k + y.k;
            s;
            """));
    }

    [Fact]
    public void AnInheritedAccessorIsNotSlotCached()
    {
        var (result, stats) = Measure("""
            var calls = 0;
            var proto = { get k() { calls++; return 5; } };
            var o = Object.create(proto);
            o.own = 1;
            var s = 0;
            for (var i = 0; i < 400; i++) s += o.k;
            s + '|' + calls;
            """);

        // The getter must run every time; a slot read would skip it.
        Assert.Equal("2000|400", result);
        Assert.Equal(0, stats.CacheHits);
    }

    [Fact]
    public void APrototypeOwnValueOfUndefined_ShadowsFurtherUpTheChain()
    {
        Assert.Equal("undefined", Eval("""
            var base = { k: 7 };
            var mid = Object.create(base);
            mid.k = undefined;
            var o = Object.create(mid);
            o.own = 1;
            var r;
            for (var i = 0; i < 400; i++) r = o.k;
            String(r);
            """));
    }

    [Fact]
    public void FreezingThePrototypeMidLoop_KeepsReadsCorrect()
    {
        Assert.Equal("2000", Eval("""
            var proto = { k: 5 };
            var o = Object.create(proto);
            o.own = 1;
            var s = 0;
            for (var i = 0; i < 400; i++) { if (i === 200) Object.freeze(proto); s += o.k; }
            s;
            """));
    }

    [Fact]
    public void RedefiningAsAnAccessorMidLoop_IsObserved()
    {
        Assert.Equal("1200", Eval("""
            var o = { k: 1 };
            var s = 0;
            for (var i = 0; i < 400; i++) {
                if (i === 200) Object.defineProperty(o, 'k', { get: function () { return 5; }, configurable: true });
                s += o.k;
            }
            s;
            """));
    }

    [Fact]
    public void APolymorphicSiteStaysCorrectAcrossFourShapes()
    {
        Assert.Equal("10", Eval("""
            function read(o) { return o.x; }
            var objects = [{ x: 1 }, { a: 0, x: 2 }, { a: 0, b: 0, x: 3 }, { a: 0, b: 0, c: 0, x: 4 }];
            var s = 0;
            for (var i = 0; i < 4; i++) s += read(objects[i]);
            s;
            """));
    }

    [Fact]
    public void AMegamorphicSiteStaysCorrect()
    {
        Assert.Equal("21", Eval("""
            function read(o) { return o.x; }
            var objects = [
                { x: 1 }, { a: 0, x: 2 }, { a: 0, b: 0, x: 3 },
                { a: 0, b: 0, c: 0, x: 4 }, { a: 0, b: 0, c: 0, d: 0, x: 5 },
                { a: 0, b: 0, c: 0, d: 0, e: 0, x: 6 }
            ];
            var s = 0;
            for (var i = 0; i < 6; i++) s += read(objects[i]);
            s;
            """));
    }

    [Fact]
    public void ANumericStringKeyIsNeverSlotCached()
    {
        // "3" names an ELEMENT, which lives outside the shape; caching it as a named slot
        // would let the two disagree.
        Assert.Equal("7,7,7", Eval("""
            var o = {};
            o['3'] = 7;
            var r = [];
            for (var i = 0; i < 200; i++) r = [o[3], o['3'], o[3.0]];
            r.join(',');
            """));
    }

    // ── allocation in the loop no longer retires the cache, so it must not mask anything ──
    //
    // `new C()` used to publish a global prototype-mutation notice per allocation, because
    // OrdinaryCreateFromConstructor installed the instance prototype by overwriting the one
    // the JSObject constructor had just set — a second write, which the guard could only read
    // as a [[SetPrototypeOf]] on a live object. Every prototype-keyed entry in the process was
    // therefore retired on every allocation, which is a correctness-preserving accident: it
    // made all of the staleness tests above pass for a reason unrelated to what they check.
    // Now that a construct installs the prototype once, the same paths have to be re-checked
    // with an allocation in the loop, where before there was nothing left to invalidate.

    [Fact]
    public void InheritedReadInAnAllocatingLoop_IsCached()
    {
        // The fix itself. Identical to InheritedMethodCall_IsCached with one allocation added
        // per iteration: that used to halve the hit rate (measured 199 999 hits / 200 002
        // misses against 399 998 / 3 for the same site with the allocation hoisted out).
        var (result, stats) = Measure("""
            function P(v) { this.v = v; }
            P.prototype.get = function () { return this.v; };
            var p = new P(7);
            var s = 0;
            var last = null;
            for (var i = 0; i < 500; i++) { s += p.get(); last = new P(i); }
            s + '|' + last.v;
            """);

        Assert.Equal("3500|499", result);
        Assert.True(stats.CacheHits >= 999, $"expected hits, got {stats.CacheHits}/{stats.CacheMisses}");
        // One notice for P.prototype becoming a prototype, not one per allocation.
        Assert.True(
            stats.PrototypeInvalidations <= 4,
            $"expected allocation to stop invalidating, got {stats.PrototypeInvalidations}");
    }

    [Fact]
    public void PrototypePropertyMutatedMidLoop_IsObserved_WhileAllocating()
    {
        Assert.Equal("800", Eval("""
            function C() { this.own = 1; }
            var proto = { k: 1 };
            var o = Object.create(proto);
            o.own = 1;
            var s = 0;
            for (var i = 0; i < 400; i++) { if (i === 200) proto.k = 3; s += o.k; new C(); }
            s;
            """));
    }

    [Fact]
    public void SetPrototypeOfMidLoop_IsObserved_WhileAllocating()
    {
        Assert.Equal("800", Eval("""
            function C() { this.own = 1; }
            var a = { k: 1 };
            var b = { k: 3 };
            var o = Object.create(a);
            o.own = 1;
            var s = 0;
            for (var i = 0; i < 400; i++) { if (i === 200) Object.setPrototypeOf(o, b); s += o.k; new C(); }
            s;
            """));
    }

    [Fact]
    public void OwnPropertyAddedMidLoop_ShadowsTheInheritedOne_WhileAllocating()
    {
        Assert.Equal("800", Eval("""
            function C() { this.own = 1; }
            var proto = { k: 1 };
            var o = Object.create(proto);
            o.own = 1;
            var s = 0;
            for (var i = 0; i < 400; i++) { if (i === 200) o.k = 3; s += o.k; new C(); }
            s;
            """));
    }

    [Fact]
    public void RedefiningAsAnAccessorMidLoop_IsObserved_WhileAllocating()
    {
        Assert.Equal("800", Eval("""
            function C() { this.own = 1; }
            var proto = { k: 1 };
            var o = Object.create(proto);
            o.own = 1;
            var s = 0;
            for (var i = 0; i < 400; i++) {
                if (i === 200) Object.defineProperty(proto, 'k', { get: function () { return 3; } });
                s += o.k;
                new C();
            }
            s;
            """));
    }

    [Fact]
    public void AClassInstanceStillGetsItsNewTargetPrototype()
    {
        // The construct paths changed to install the prototype by construction rather than by
        // overwriting it, so pin that the prototype they install is unchanged - including the
        // subclass and Reflect.construct forms, where it comes from newTarget rather than from
        // the callee, and the primitive-`prototype` fallback to %Object.prototype%.
        Assert.Equal("true|true|true|true|true", Eval("""
            class A {}
            class B extends A {}
            function F() {}
            function G() {}
            G.prototype = 1;
            var r = [
                Object.getPrototypeOf(new B()) === B.prototype,
                Object.getPrototypeOf(new B()) instanceof Object === false || A.prototype.isPrototypeOf(new B()),
                Object.getPrototypeOf(Reflect.construct(F, [], B)) === B.prototype,
                Object.getPrototypeOf(new F()) === F.prototype,
                Object.getPrototypeOf(new G()) === Object.prototype
            ];
            r.join('|');
            """));
    }

    // ── arrays track their NAMED properties by shape (item 2-2) ───────────────────────
    //
    // Shape eligibility used to be an exact `GetType() == typeof(JSObject)` test, so every
    // named property on an array was a 100% cache miss — measured 0 hits against 200 000.
    // Arrays now opt in. What must not change is everything an array does that is NOT a named
    // data property: its elements, its exotic `length`, and the interaction between the two.

    [Fact]
    public void ANamedPropertyOnAnArray_IsCached()
    {
        var (result, stats) = Measure("""
            var a = [1, 2, 3];
            a.tag = 7;
            var s = 0;
            for (var i = 0; i < 500; i++) s += a.tag;
            s;
            """);

        Assert.Equal("3500", result);
        Assert.True(stats.CacheHits >= 499, $"expected hits, got {stats.CacheHits}/{stats.CacheMisses}");
    }

    [Fact]
    public void GrowingAnArrayThroughABuiltInAbandonsItsNamedShape()
    {
        // `length` is computed from the element store rather than held as a data property, so
        // it can never occupy a slot however wide eligibility gets, and it must keep tracking
        // the elements — which it does.
        //
        // The dictionary fallback is the part worth pinning, because it bounds what item 2-2
        // buys. `push` reaches the property store through GetOwnProperties(create: true), and
        // that abandons the shape by design: a mutable ref handed to another assembly could add
        // a named property without telling the tracker, so the fast layout is dropped rather
        // than trusted. Exactly one fallback for five pushes — the first drops the shape and the
        // rest find it already gone. Correctness is unaffected; the named-property cache is, so
        // an array that grows through the built-ins stops hitting. Contrast
        // ANamedPropertyOnAnArray_IsCached, which does not grow and keeps its shape.
        var (result, stats) = Measure("""
            var a = [];
            a.tag = 1;
            var seen = '';
            for (var i = 0; i < 5; i++) { a.push(i); seen += a.length; }
            seen;
            """);

        Assert.Equal("12345", result);
        Assert.Equal(1, stats.DictionaryFallbacks);
    }

    [Fact]
    public void ElementsAndNamedPropertiesOnAnArrayStayDistinct()
        => Assert.Equal("10|9|2|tag,other", Eval("""
            var a = [];
            a.tag = 9;
            a.other = 1;
            for (var i = 0; i < 10; i++) a[i] = i;
            var named = [];
            for (var k in a) { if (!/^[0-9]+$/.test(k)) named.push(k); }
            [a.length, a.tag, Object.keys(a).length - a.length, named.join(',')].join('|');
            """));

    [Fact]
    public void MaterializingLengthDoesNotConfuseTheShape()
        => Assert.Equal("3|false|7|3", Eval("""
            var a = [1, 2, 3];
            a.tag = 7;
            for (var i = 0; i < 300; i++) a.tag = 7;
            Object.defineProperty(a, 'length', { value: 3, writable: false });
            var threw = false;
            try { a.push(4); } catch (e) { threw = true; }
            [a.length, Object.getOwnPropertyDescriptor(a, 'length').writable, a.tag, a[2]].join('|');
            """));

    [Fact]
    public void DeletingANamedPropertyOnAnArray_RevealsTheInheritedOne()
        => Assert.Equal("1200", Eval("""
            Array.prototype.tag = 5;
            var a = [];
            a.tag = 1;
            var s = 0;
            for (var i = 0; i < 400; i++) { if (i === 200) delete a.tag; s += a.tag; }
            s;
            """));

    [Fact]
    public void AnArrayPrototypePropertyMutatedMidLoop_IsObserved()
        => Assert.Equal("800", Eval("""
            Array.prototype.tag = 1;
            var a = [];
            a.own = 1;
            var s = 0;
            for (var i = 0; i < 400; i++) { if (i === 200) Array.prototype.tag = 3; s += a.tag; }
            s;
            """));

    [Fact]
    public void ArrayMethodsStillWorkAfterNamedPropertiesAreTracked()
        => Assert.Equal("1,2,3|6|3,2,1|[1,2,3]|2", Eval("""
            var a = [1, 2, 3];
            a.tag = 9;
            for (var i = 0; i < 300; i++) a.tag = i;
            var sum = 0;
            a.forEach(function (v) { sum += v; });
            [a.join(','), sum, a.slice().reverse().join(','), JSON.stringify(a), a.indexOf(3)].join('|');
            """));

    [Fact]
    public void AFrozenArrayRefusesBothKinds()
        => Assert.Equal("1|7|false|false", Eval("""
            var a = [1];
            a.tag = 7;
            for (var i = 0; i < 300; i++) a.tag = 7;
            Object.freeze(a);
            a.tag = 99;
            a[0] = 99;
            [a[0], a.tag, Object.isExtensible(a), Object.getOwnPropertyDescriptor(a, 'tag').writable].join('|');
            """));

    [Fact]
    public void ASparseArrayWithNamedPropertiesKeepsItsHoles()
        => Assert.Equal("5|false|true|7", Eval("""
            var a = [];
            a[4] = 1;
            a.tag = 7;
            for (var i = 0; i < 300; i++) a.tag = 7;
            [a.length, 2 in a, 4 in a, a.tag].join('|');
            """));

    [Fact]
    public void AnArraySubclassInstanceIsStillAnArray()
        => Assert.Equal("3|9|true|4", Eval("""
            class MyArray extends Array {}
            var a = new MyArray();
            a.push(1, 2, 3);
            a.tag = 9;
            var s = 0;
            for (var i = 0; i < 300; i++) s = a.tag;
            [a.length, s, Array.isArray(a), (a.push(4), a.length)].join('|');
            """));

    [Fact]
    public void ATypedArrayIsStillNotShapeTracked()
        => Assert.Equal("4|9|0", Eval("""
            var a = new Float64Array(4);
            a.tag = 9;
            var s = 0;
            for (var i = 0; i < 300; i++) s = a.tag;
            [a.length, s, a[0]].join('|');
            """));

    [Fact]
    public void TwoArraysReachingTheSameShapeStayDistinct()
        => Assert.Equal("1,2|3,4", Eval("""
            var x = []; x.tag = 1; x.other = 2;
            var y = []; y.tag = 3; y.other = 4;
            var rx = '', ry = '';
            for (var i = 0; i < 300; i++) { rx = x.tag + ',' + x.other; ry = y.tag + ',' + y.other; }
            rx + '|' + ry;
            """));

    [Fact]
    public void AProxyInThePrototypeChainStillTraps()
    {
        Assert.Equal("400|5", Eval("""
            var traps = 0;
            var p = new Proxy({ k: 5 }, { get: function (t, key, r) { traps++; return Reflect.get(t, key, r); } });
            var o = Object.create(p);
            o.own = 1;
            var last;
            for (var i = 0; i < 400; i++) last = o.k;
            traps + '|' + last;
            """));
    }
}
