using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Compiler.Tests;

// Roadmap item 2-9: a shape-tracked object keeps its named properties in the shape alone and
// the radix trie holds nothing until something needs a real descriptor, at which point the trie
// is rebuilt and the object goes back to behaving exactly as it did before.
//
// What is pinned here is the MATERIALIZATION BOUNDARY, not the hit rates — those are
// PropertyShapeCacheTests' job and item 2-9 leaves them byte-identical. Every test below either
// crosses the boundary and checks that nothing was lost, or stays on the shape-only side and
// checks that it answers the same way the trie used to. The two things a shape cannot carry are
// the ones to watch: per-object ATTRIBUTES (a shape is shared, so writable/enumerable/
// configurable vary between objects at the same shape) and ORDER across a rebuild.
public class ShapeOnlyPropertyStorageTests
{
    private static string Eval(string source)
    {
        using var context = new JSContext();
        return context.Eval(source).ToString();
    }

    // ── order survives the rebuild ────────────────────────────────────────────────────

    [Theory]
    [InlineData("var o = {}; o.a = 1; o.b = 2; o.c = 3;", "a,b,c")]
    [InlineData("var o = { a: 1, b: 2, c: 3 };", "a,b,c")]
    [InlineData("function C() { this.a = 1; this.b = 2; this.c = 3; } var o = new C();", "a,b,c")]
    [InlineData("var o = {}; o.z = 1; o.a = 2; o.m = 3;", "z,a,m")]
    [InlineData("var o = Object.create(null); o.q = 1; o.p = 2;", "q,p")]
    public void EnumerationOrder_IsInsertionOrder_AfterTheTrieIsRebuilt(string setup, string expected)
    {
        // Object.keys is the boundary: it takes a mutable ref to the property map, so it is the
        // first thing that forces the shape back into the trie. Slot order IS insertion order
        // (ObjectShape.Add assigns slots[key] = slots.Count), which is what makes the rebuild
        // reproduce the chain the eager path would have built.
        Assert.Equal(expected, Eval($"{setup} Object.keys(o).join(',');"));
    }

    [Fact(Timeout = 600000)]
    public void EnumerationOrder_IsStable_AcrossTheBoundary()
    {
        // Properties added AFTER materialization must continue the same order, not restart it.
        Assert.Equal(
            "a,b,c,d,e",
            Eval("var o = {}; o.a = 1; o.b = 2; o.c = 3; Object.keys(o); o.d = 4; o.e = 5; Object.keys(o).join(',');"));
    }

    [Fact(Timeout = 600000)]
    public void DeletedThenRecreatedProperty_MovesToTheEnd_AcrossTheBoundary()
    {
        // A delete materializes (it needs a real descriptor to check configurability), so this
        // pins that the rebuilt chain then behaves like the original: a recreated property is a
        // NEW property and goes to the tail.
        Assert.Equal("a,c,b", Eval("var o = { a: 1, b: 2, c: 3 }; delete o.b; o.b = 9; Object.keys(o).join(',');"));
    }

    // ── attributes survive the rebuild ────────────────────────────────────────────────

    [Fact(Timeout = 600000)]
    public void NonEnumerableProperty_StaysHidden_FromKeysAfterMaterialization()
    {
        Assert.Equal(
            "e|ne,e",
            Eval("""
                var o = {};
                Object.defineProperty(o, 'ne', { value: 1, enumerable: false, writable: true, configurable: true });
                o.e = 2;
                Object.keys(o).join(',') + '|' + Object.getOwnPropertyNames(o).join(',');
                """));
    }

    [Theory]
    [InlineData("writable", "false")]
    [InlineData("enumerable", "false")]
    [InlineData("configurable", "false")]
    public void ANonDefaultAttribute_SurvivesMaterialization(string attribute, string expected)
    {
        // The attribute is set while the object is shape-only, then read back through a
        // descriptor query, which is on the far side of the boundary. A shape is shared by every
        // object that reaches it, so this is precisely the fact the parallel attribute array
        // exists to carry.
        Assert.Equal(
            expected,
            Eval($$"""
                var o = {};
                o.x = 1;
                Object.defineProperty(o, 'y', { value: 2, writable: true, enumerable: true, configurable: true, {{attribute}}: false });
                String(Object.getOwnPropertyDescriptor(o, 'y').{{attribute}});
                """));
    }

    [Fact(Timeout = 600000)]
    public void TwoObjectsAtTheSameShape_KeepTheirOwnAttributes()
    {
        // The reason attributes cannot live in the shape, stated as a test: same key set, same
        // shape, different flags.
        Assert.Equal(
            "true,false",
            Eval("""
                var a = {}; a.x = 1;
                var b = {}; b.x = 1;
                Object.defineProperty(b, 'x', { writable: false });
                String(Object.getOwnPropertyDescriptor(a, 'x').writable) + ',' +
                    String(Object.getOwnPropertyDescriptor(b, 'x').writable);
                """));
    }

    // ── a refused write is still refused on the shape-only path ───────────────────────

    [Fact(Timeout = 600000)]
    public void AWarmedStoreSite_StillRefusesAFrozenReceiver()
    {
        // The site is warmed on 300 ordinary receivers first, so the store inline cache is live
        // when the frozen one arrives. Freezing rewrites attributes in place and keeps the
        // shape, so this is the shape-only read of the Readonly bit doing its job.
        Assert.Equal(
            "0",
            Eval("""
                function W() { this.v = 0; }
                for (var i = 0; i < 300; i++) { var w = new W(); w.v = i; }
                var frozen = new W();
                Object.freeze(frozen);
                frozen.v = 99;
                String(frozen.v);
                """));
    }

    [Fact(Timeout = 600000)]
    public void AWarmedStoreSite_StillRefusesAPropertyMadeNonWritable()
    {
        Assert.Equal(
            "299",
            Eval("""
                function W() { this.v = 0; }
                var o = new W();
                for (var i = 0; i < 300; i++) o.v = i;
                Object.defineProperty(o, 'v', { writable: false });
                o.v = 1234;
                String(o.v);
                """));
    }

    [Fact(Timeout = 600000)]
    public void ASealedObject_TakesAnOverwriteAndRefusesAnAddition()
    {
        Assert.Equal("2,undefined", Eval("var s = { a: 1 }; Object.seal(s); s.a = 2; s.b = 3; s.a + ',' + s.b;"));
    }

    // ── the descriptor kinds a shape slot cannot hold ─────────────────────────────────

    [Fact(Timeout = 600000)]
    public void AnAccessorRedefinedOnAWarmedSite_TakesOverBothDirections()
    {
        // An accessor abandons the shape, which materializes first. Both halves are asserted
        // because a stale slot would keep answering reads with the old value.
        Assert.Equal(
            "55,1",
            Eval("""
                var o = { x: 1 };
                for (var i = 0; i < 300; i++) o.x = i;
                var setterCalls = 0;
                Object.defineProperty(o, 'x', {
                    get: function () { return 55; },
                    set: function (v) { setterCalls++; },
                    configurable: true
                });
                o.x = 3;
                o.x + ',' + setterCalls;
                """));
    }

    [Fact(Timeout = 600000)]
    public void AFunctionsAnnexBDeferredCells_KeepTheirDataDescriptor()
    {
        // Every ordinary non-strict function carries `caller`/`arguments` as deferred cells
        // (P0-3), which is the null-slot case 2-8 introduced. A null slot cannot be described
        // from the shape, so recording one materializes — this pins that the descriptor P0-3
        // preserved is still what comes back out.
        Assert.Equal(
            "false,false,false",
            Eval("""
                function f() {}
                var d = Object.getOwnPropertyDescriptor(f, 'caller');
                String(d.writable) + ',' + String(d.enumerable) + ',' + String(d.configurable);
                """));
    }

    [Fact(Timeout = 600000)]
    public void APrivateFieldDoesNotAppearAsAnOwnKey()
    {
        Assert.Equal(
            "3,4,pub",
            Eval("""
                class C { #p = 3; constructor() { this.pub = 4; } read() { return this.#p; } }
                var i = new C();
                i.read() + ',' + i.pub + ',' + Object.keys(i).join(',');
                """));
    }

    // ── the boundary as other assemblies reach it ─────────────────────────────────────

    [Fact(Timeout = 600000)]
    public void ObjectAssign_CopiesEveryPropertyOfAShapeOnlySource()
    {
        Assert.Equal(
            "{\"m\":1,\"n\":2,\"o\":3}",
            Eval("var src = {}; src.m = 1; src.n = 2; src.o = 3; JSON.stringify(Object.assign({}, src));"));
    }

    [Fact(Timeout = 600000)]
    public void SpreadCopiesEveryPropertyOfAShapeOnlySource()
    {
        Assert.Equal(
            "{\"m\":1,\"n\":2,\"o\":3}",
            Eval("var src = {}; src.m = 1; src.n = 2; src.o = 3; JSON.stringify({ ...src });"));
    }

    [Fact(Timeout = 600000)]
    public void AProxyOverAShapeOnlyTarget_SeesEveryProperty()
    {
        Assert.Equal(
            "10,20,q,r",
            Eval("""
                var t = {}; t.q = 1; t.r = 2;
                var p = new Proxy(t, { get: function (tt, k) { return k in tt ? tt[k] * 10 : undefined; } });
                p.q + ',' + p.r + ',' + Object.keys(t).join(',');
                """));
    }

    [Fact(Timeout = 600000)]
    public void ForIn_WalksOwnThenInheritedProperties()
    {
        Assert.Equal(
            "own,inh",
            Eval("""
                var proto = {}; proto.inh = 1;
                var child = Object.create(proto); child.own = 2;
                var seen = [];
                for (var k in child) seen.push(k);
                seen.join(',');
                """));
    }

    [Fact(Timeout = 600000)]
    public void HasOwnPropertyAndIn_AgreeOnAShapeOnlyObject()
    {
        Assert.Equal(
            "true,false,true,false",
            Eval("""
                var proto = {}; proto.p = 1;
                var o = Object.create(proto); o.own = 2;
                String(o.hasOwnProperty('own')) + ',' + String(o.hasOwnProperty('p')) + ',' +
                    String('p' in o) + ',' + String('nope' in o);
                """));
    }

    // ── past the slot array's growth steps ────────────────────────────────────────────

    [Fact(Timeout = 600000)]
    public void ManyProperties_KeepTheirValuesAndOrderAcrossEveryGrowth()
    {
        // The slot array and the parallel attribute array grow together; a resize that copied one
        // and not the other would surface here as a wrong value or a lost attribute rather than a
        // crash, which is why every value is checked and not just the count.
        Assert.Equal(
            "true,40,true",
            Eval("""
                var o = {};
                for (var i = 0; i < 40; i++) o['k' + i] = i;
                var valuesOk = true;
                for (var i = 0; i < 40; i++) if (o['k' + i] !== i) valuesOk = false;
                var keys = Object.keys(o);
                var orderOk = true;
                for (var i = 0; i < 40; i++) if (keys[i] !== 'k' + i) orderOk = false;
                String(valuesOk) + ',' + keys.length + ',' + String(orderOk);
                """));
    }

    [Fact(Timeout = 600000)]
    public void ReadsAgreeOnBothSidesOfTheBoundary()
    {
        // The same property read shape-only, then again after a rebuild, has to give the same
        // answer — and a write in between has to land where both can see it.
        Assert.Equal(
            "1,1,7,7",
            Eval("""
                var o = {}; o.x = 1;
                var beforeShapeOnly = o.x;
                var beforeDescriptor = Object.getOwnPropertyDescriptor(o, 'x').value;
                o.x = 7;
                var afterWrite = o.x;
                var afterDescriptor = Object.getOwnPropertyDescriptor(o, 'x').value;
                beforeShapeOnly + ',' + beforeDescriptor + ',' + afterWrite + ',' + afterDescriptor;
                """));
    }
}
