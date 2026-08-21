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

    [Fact(Timeout = 600000)]
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

    [Fact(Timeout = 600000)]
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

    [Fact(Timeout = 600000)]
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

    [Fact(Timeout = 600000)]
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

    [Fact(Timeout = 600000)]
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

    [Fact(Timeout = 600000)]
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

    [Fact(Timeout = 600000)]
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

    [Fact(Timeout = 600000)]
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

    [Fact(Timeout = 600000)]
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

    [Fact(Timeout = 600000)]
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

    [Fact(Timeout = 600000)]
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

    [Fact(Timeout = 600000)]
    public void AnInheritedAccessorIsNotSlotCached()
    {
        // The prototype is linked by `__proto__` in the literal rather than by
        // Object.create, so the script performs no other cacheable read. `Object.create` is a
        // named read on a FUNCTION object, which item 2-8 made cacheable, and it was quietly
        // supplying the one hit this exact assertion is here to rule out.
        var (result, stats) = Measure("""
            var calls = 0;
            var proto = { get k() { calls++; return 5; } };
            var o = { __proto__: proto, own: 1 };
            var s = 0;
            for (var i = 0; i < 400; i++) s += o.k;
            s + '|' + calls;
            """);

        // The getter must run every time; a slot read would skip it.
        Assert.Equal("2000|400", result);
        Assert.Equal(0, stats.CacheHits);
    }

    [Fact(Timeout = 600000)]
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

    [Fact(Timeout = 600000)]
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

    [Fact(Timeout = 600000)]
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

    [Fact(Timeout = 600000)]
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

    [Fact(Timeout = 600000)]
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

    [Fact(Timeout = 600000)]
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

    [Fact(Timeout = 600000)]
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

    [Fact(Timeout = 600000)]
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

    [Fact(Timeout = 600000)]
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

    [Fact(Timeout = 600000)]
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

    [Fact(Timeout = 600000)]
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

    [Fact(Timeout = 600000)]
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

    [Fact(Timeout = 600000)]
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

    [Fact(Timeout = 600000)]
    public void GrowingAnArrayThroughABuiltInKeepsItsNamedShape()
    {
        // `length` is computed from the element store rather than held as a data property, so
        // it can never occupy a slot however wide eligibility gets, and it must keep tracking
        // the elements — which it does.
        //
        // This used to pin the OPPOSITE, and the old comment was right that the fallback bounded
        // what item 2-2 buys: push reached the property store through GetOwnProperties(create:
        // true), which abandons the shape, so an array that grew through the built-ins stopped
        // hitting the named-property cache for good. That bound is gone. SetLengthWritable no
        // longer records a descriptor for a WRITABLE length, because an absent entry already
        // means writable and the stored value was never read back — so push, pop and concat
        // no longer cost an array its shape.
        //
        // The hit assertion is the point, not the fallback count: the fallback was only ever
        // interesting because of what it did to the cache.
        var (result, stats) = Measure("""
            var a = [];
            a.tag = 1;
            var seen = '';
            for (var i = 0; i < 5; i++) { a.push(i); seen += a.length; }
            var s = 0;
            for (var i = 0; i < 500; i++) s += a.tag;
            seen + '|' + s;
            """);

        Assert.Equal("12345|500", result);
        Assert.Equal(0, stats.DictionaryFallbacks);
        Assert.True(stats.CacheHits >= 499, $"expected the named read to keep hitting after growth, got {stats.CacheHits}/{stats.CacheMisses}");
    }

    [Fact(Timeout = 600000)]
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

    [Fact(Timeout = 600000)]
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

    [Fact(Timeout = 600000)]
    public void DeletingANamedPropertyOnAnArray_RevealsTheInheritedOne()
        => Assert.Equal("1200", Eval("""
            Array.prototype.tag = 5;
            var a = [];
            a.tag = 1;
            var s = 0;
            for (var i = 0; i < 400; i++) { if (i === 200) delete a.tag; s += a.tag; }
            s;
            """));

    [Fact(Timeout = 600000)]
    public void AnArrayPrototypePropertyMutatedMidLoop_IsObserved()
        => Assert.Equal("800", Eval("""
            Array.prototype.tag = 1;
            var a = [];
            a.own = 1;
            var s = 0;
            for (var i = 0; i < 400; i++) { if (i === 200) Array.prototype.tag = 3; s += a.tag; }
            s;
            """));

    [Fact(Timeout = 600000)]
    public void ArrayMethodsStillWorkAfterNamedPropertiesAreTracked()
        => Assert.Equal("1,2,3|6|3,2,1|[1,2,3]|2", Eval("""
            var a = [1, 2, 3];
            a.tag = 9;
            for (var i = 0; i < 300; i++) a.tag = i;
            var sum = 0;
            a.forEach(function (v) { sum += v; });
            [a.join(','), sum, a.slice().reverse().join(','), JSON.stringify(a), a.indexOf(3)].join('|');
            """));

    [Fact(Timeout = 600000)]
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

    [Fact(Timeout = 600000)]
    public void ASparseArrayWithNamedPropertiesKeepsItsHoles()
        => Assert.Equal("5|false|true|7", Eval("""
            var a = [];
            a[4] = 1;
            a.tag = 7;
            for (var i = 0; i < 300; i++) a.tag = 7;
            [a.length, 2 in a, 4 in a, a.tag].join('|');
            """));

    [Fact(Timeout = 600000)]
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

    [Fact(Timeout = 600000)]
    public void ATypedArrayIsStillNotShapeTracked()
        => Assert.Equal("4|9|0", Eval("""
            var a = new Float64Array(4);
            a.tag = 9;
            var s = 0;
            for (var i = 0; i < 300; i++) s = a.tag;
            [a.length, s, a[0]].join('|');
            """));

    [Fact(Timeout = 600000)]
    public void TwoArraysReachingTheSameShapeStayDistinct()
        => Assert.Equal("1,2|3,4", Eval("""
            var x = []; x.tag = 1; x.other = 2;
            var y = []; y.tag = 3; y.other = 4;
            var rx = '', ry = '';
            for (var i = 0; i < 300; i++) { rx = x.tag + ',' + x.other; ry = y.tag + ',' + y.other; }
            rx + '|' + ry;
            """));

    // ── functions track their named properties by shape (item 2-8) ────────────────────
    //
    // Statics on a constructor function were a 100% cache miss — 0 hits out of 200 000 — and
    // that is how DeltaBlue reads `Strength.REQUIRED` and `Strength.stronger` in its hot path,
    // at 601× off. Two things blocked it. The function's own `length`, `name` and `prototype`
    // went in through a bare property-store put that never told the shape, so its key set would
    // have been missing three keys every function has. And every ordinary NON-strict function
    // carries the Annex B `caller`/`arguments` as deferred cells from birth, which abandoned the
    // shape outright — so what is pinned below is mostly that those two still behave.

    [Theory]
    [InlineData("function S() {}")]
    [InlineData("'use strict'; function S() {}")]
    [InlineData("var S = function () {};")]
    [InlineData("class S {}")]
    public void AStaticOnAFunction_IsCached(string declaration)
    {
        var (result, stats) = Measure(declaration + """

            S.REQUIRED = 7;
            var s = 0;
            for (var i = 0; i < 500; i++) s += S.REQUIRED;
            s;
            """);

        Assert.Equal("3500", result);
        Assert.True(stats.CacheHits >= 499, $"expected hits, got {stats.CacheHits}/{stats.CacheMisses}");
        Assert.Equal(0, stats.DictionaryFallbacks);
    }

    [Fact(Timeout = 600000)]
    public void AFunctionsOwnPropertiesAreUnchangedAndComplete()
        // length, name and prototype now go through the shape tracker. Their values, their
        // attributes and their enumeration order are what must not have moved.
        => Assert.Equal("2|f|true|false,false,true|true,false,false|length,name,prototype", Eval("""
            function f(a, b) {}
            var d = Object.getOwnPropertyDescriptor(f, 'length');
            var p = Object.getOwnPropertyDescriptor(f, 'prototype');
            [
                f.length,
                f.name,
                f.prototype.constructor === f,
                [d.writable, d.enumerable, d.configurable].join(','),
                [p.writable, p.enumerable, p.configurable].join(','),
                Object.getOwnPropertyNames(f).filter(function (k) { return k !== 'caller' && k !== 'arguments'; }).join(',')
            ].join('|');
            """));

    [Fact(Timeout = 600000)]
    public void TheAnnexBCallerAndArgumentsKeepTheirDescriptorShape()
        // A deferred cell is now recorded in the shape with no slot value instead of abandoning
        // it. The cell still has to be the thing that answers, with the Annex B descriptor P0-3
        // preserved: a non-writable, non-enumerable, non-configurable DATA property.
        => Assert.Equal("true|||false,false,false|false,false,false", Eval("""
            function f() {}
            var c = Object.getOwnPropertyDescriptor(f, 'caller');
            var a = Object.getOwnPropertyDescriptor(f, 'arguments');
            [
                ('value' in c) && ('value' in a) && !('get' in c) && !('get' in a),
                f.caller,
                f.arguments,
                [c.writable, c.enumerable, c.configurable].join(','),
                [a.writable, a.enumerable, a.configurable].join(',')
            ].join('|');
            """));

    [Fact(Timeout = 600000)]
    public void ANullSlotKeyStillReadsThroughTheGenericPath()
        // The read site for `caller` is warmed against a key the shape carries with no slot
        // value, so every read must decline the fast path and realize the cell.
        => Assert.Equal("300|null", Eval("""
            function f() {}
            f.tag = 1;
            var n = 0, last = 'x';
            for (var i = 0; i < 300; i++) { last = f.caller; n++; }
            n + '|' + last;
            """));

    [Fact(Timeout = 600000)]
    public void ReadingCallerWhileTheFunctionIsOnTheStackStillWorks()
        => Assert.Equal("true|true", Eval("""
            function inner() { return inner.caller === outer; }
            function outer() { return inner(); }
            var first = outer();
            var second = outer();
            first + '|' + second;
            """));

    [Fact(Timeout = 600000)]
    public void AStaticRedefinedAsAnAccessorMidLoop_IsObserved()
        => Assert.Equal("800", Eval("""
            function S() {}
            S.k = 1;
            var s = 0;
            for (var i = 0; i < 400; i++) {
                if (i === 200) Object.defineProperty(S, 'k', { get: function () { return 3; } });
                s += S.k;
            }
            s;
            """));

    [Fact(Timeout = 600000)]
    public void OverwritingAFunctionsOwnLengthAndNameStillWorks()
        => Assert.Equal("9|renamed|7", Eval("""
            function f(a, b) {}
            f.tag = 7;
            for (var i = 0; i < 300; i++) f.tag = 7;
            Object.defineProperty(f, 'length', { value: 9 });
            Object.defineProperty(f, 'name', { value: 'renamed' });
            [f.length, f.name, f.tag].join('|');
            """));

    [Fact(Timeout = 600000)]
    public void ABoundFunctionKeepsItsNameAndLength()
        => Assert.Equal("bound f|1|3", Eval("""
            function f(a, b) { return a + b; }
            var b = f.bind(null, 1);
            [b.name, b.length, b(2)].join('|');
            """));

    [Fact(Timeout = 600000)]
    public void ThePoisonPillThrowTypeErrorStillThrows()
        => Assert.Equal("TypeError|TypeError", Eval("""
            'use strict';
            function f() {}
            var d = Object.getOwnPropertyDescriptor(Function.prototype, 'caller');
            var r = [];
            try { d.get.call(f); } catch (e) { r.push(e.constructor.name); }
            try { d.set.call(f, 1); } catch (e) { r.push(e.constructor.name); }
            r.join('|');
            """));

    [Fact(Timeout = 600000)]
    public void DeletingAStaticRevealsTheInheritedOne()
        => Assert.Equal("1200", Eval("""
            function S() {}
            Object.setPrototypeOf(S, { k: 5 });
            S.k = 1;
            var s = 0;
            for (var i = 0; i < 400; i++) { if (i === 200) delete S.k; s += S.k; }
            s;
            """));

    [Fact(Timeout = 600000)]
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

    // ── a function's `prototype` is withheld from the shape WRITE paths ───────────────
    //
    // Item 2-8 admitted functions to shape tracking, and that opened a hole these cover.
    // JSFunction keeps a cached `prototype` FIELD — the one [[Construct]] reads — and syncs it
    // by overriding every observable write path. A shape fast path is none of them: it writes
    // ownProperties and shapeSlots and returns. So a *cached* store to `f.prototype` updated the
    // observable property and left construction building instances on the previous object.
    //
    // The shape it broke is DeltaBlue's. `inheritsFrom` does `this.prototype = new Inheriter()`
    // from one emitted site, called once per class: the first call missed and was right, every
    // call after it hit and was wrong, and the second level of every inheritance chain came out
    // unlinked. The loop that measured 2-8's win never wrote a prototype, so it saw none of it.

    [Fact(Timeout = 600000)]
    public void AWarmedPrototypeWriteSiteStillRelinksTheWholeChain()
        => Assert.Equal("true|true|function|function|true", Eval("""
            // DeltaBlue's exact idiom, three levels deep. One store site, many functions.
            Object.defineProperty(Object.prototype, 'inheritsFrom', {
                value: function (shuper) {
                    function Inheriter() { }
                    Inheriter.prototype = shuper.prototype;
                    this.prototype = new Inheriter();
                    this.superConstructor = shuper;
                }
            });

            function Constraint(s) { this.strength = s; }
            Constraint.prototype.addConstraint = function () { this.added = true; };
            function BinaryConstraint() { this.addConstraint(); }
            BinaryConstraint.inheritsFrom(Constraint);
            function EqualityConstraint() { EqualityConstraint.superConstructor.call(this); }
            EqualityConstraint.inheritsFrom(BinaryConstraint);

            var e = new EqualityConstraint();
            [
                Object.getPrototypeOf(BinaryConstraint.prototype) === Constraint.prototype,
                Object.getPrototypeOf(EqualityConstraint.prototype) === BinaryConstraint.prototype,
                typeof EqualityConstraint.prototype.addConstraint,
                typeof e.addConstraint,
                e.added === true
            ].join('|');
            """));

    [Fact(Timeout = 600000)]
    public void OneWarmedSiteWritingManyFunctionsPrototypes_EveryNewGetsItsOwn()
        => Assert.Equal("300|300", Eval("""
            // The store site is warm from the second iteration on; every instance after that
            // must still land on the prototype its own constructor was given.
            function install(f, proto) { f.prototype = proto; }
            var right = 0, wrong = 0;
            for (var i = 0; i < 300; i++) {
                var ctor = new Function('return function C() {};')();
                var proto = { tag: i };
                install(ctor, proto);
                var instance = new ctor();
                if (Object.getPrototypeOf(instance) === proto && instance.tag === i) right++; else wrong++;
            }
            right + '|' + (300 - wrong);
            """));

    [Fact(Timeout = 600000)]
    public void APrototypeWriteThroughAWarmedSiteAgreesWithTheObservableProperty()
        => Assert.Equal("400|400", Eval("""
            // The property and [[Construct]] must never disagree — that disagreement WAS the bug.
            function F() {}
            var agreed = 0, constructed = 0;
            for (var i = 0; i < 400; i++) {
                var proto = { n: i };
                F.prototype = proto;
                if (F.prototype === proto) agreed++;
                if (Object.getPrototypeOf(new F()) === proto) constructed++;
            }
            agreed + '|' + constructed;
            """));

    [Fact(Timeout = 600000)]
    public void AClassPrototypeWriteThroughAWarmedSiteIsStillRefused()
        => Assert.Equal("300|true|true", Eval("""
            // JSClass derives from JSFunction, so it inherits the same cached field and the same
            // exclusion. A class's `prototype` is non-writable and non-configurable, so every one
            // of these writes must be refused — including the ones after the site is warm, which
            // is the case a cached store could have got wrong.
            //
            // Scoped deliberately to the observable PROPERTY. What `new C()` produces after a
            // refused write is wrong on this engine at the pinned pointer too — the indexer syncs
            // JSFunction's cached prototype field before the write, so a refusal still redirects
            // [[Construct]] — and asserting it here would either encode that defect as correct or
            // fail for a reason this item does not own. It is reported separately in
            // docs/performance-roadmap.md item 2-8.
            class C {}
            var original = C.prototype;
            var kept = 0;
            for (var i = 0; i < 300; i++) {
                try { C.prototype = { n: i }; } catch (e) { /* sloppy: silently refused */ }
                if (C.prototype === original) kept++;
            }
            var d = Object.getOwnPropertyDescriptor(C, 'prototype');
            kept + '|' + (d.writable === false) + '|' + (d.configurable === false);
            """));

    [Fact(Timeout = 600000)]
    public void AssigningANonObjectPrototypeThroughAWarmedSiteKeepsConstructability()
        => Assert.Equal("300|300", Eval("""
            // AssignPrototypeField's other half: a function that HAS had a prototype object is a
            // constructor for good, even after `prototype` is set to a primitive. Reflect.construct
            // consults that, so a warmed site must not lose it either.
            function F() { this.ok = 1; }
            var constructed = 0, accepted = 0;
            for (var i = 0; i < 300; i++) {
                F.prototype = 7;
                if (new F().ok === 1) constructed++;
                try { Reflect.construct(Object, [], F); accepted++; } catch (e) { /* would be the bug */ }
            }
            constructed + '|' + accepted;
            """));

    [Fact(Timeout = 600000)]
    public void AFunctionsOtherNamedPropertiesStillTakeTheStoreCache()
    {
        // The exclusion is one key wide. Statics are where item 2-8's win is, so they must
        // still hit — this is the assertion that stops the fix from quietly undoing it.
        var (result, stats) = Measure("""
            function S() {}
            S.count = 0;
            for (var i = 0; i < 500; i++) S.count = i;
            S.count;
            """);

        Assert.Equal("499", result);
        Assert.True(stats.StoreCacheHits >= 499,
            $"a function's statics must still hit, got {stats.StoreCacheHits}/{stats.StoreCacheMisses}");
    }

    [Fact(Timeout = 600000)]
    public void ReadingAFunctionsPrototypeIsStillCached()
    {
        // Only the WRITE paths are gated: a read has no field to keep in sync, and gating it
        // would give up cache coverage for nothing.
        var (result, stats) = Measure("""
            function F() {}
            var p = null;
            for (var i = 0; i < 500; i++) p = F.prototype;
            (p === F.prototype).toString();
            """);

        Assert.Equal("true", result);
        Assert.True(stats.CacheHits >= 499,
            $"reading `prototype` should still hit, got {stats.CacheHits}/{stats.CacheMisses}");
    }
}
