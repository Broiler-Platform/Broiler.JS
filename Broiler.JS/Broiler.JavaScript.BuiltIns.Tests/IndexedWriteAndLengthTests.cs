using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// The indexed [[Set]] fast path and the array length-shrink strategy
// (docs/performance-roadmap.md P2). Two defects are guarded here:
//
//  * storing into an element that was not already present walked the prototype chain and
//    came back through the foreign-receiver path, allocating a JSNumber key and a
//    four-property descriptor per element — so filling a fresh array cost ~1 350 bytes an
//    element while overwriting an existing one cost nothing;
//  * shrinking `length` scanned and sorted the WHOLE element table, making every pop() an
//    O(n) operation and `while (a.length) a.pop()` quadratic.
//
// Both fast paths must stay invisible: exotic receivers (typed arrays, proxies, mapped
// arguments) keep the general path, and every attribute/integrity rule still holds.
public class IndexedWriteAndLengthTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    // ── ordinary element writes ───────────────────────────────────────────────────────

    [Fact]
    public void ANewElementGetsDefaultAttributes()
        => Assert.Equal("1,1|true,true,true", Eval("""
            var a = [];
            a[0] = 1;
            var d = Object.getOwnPropertyDescriptor(a, '0');
            a[0] + ',' + a.length + '|' + [d.writable, d.enumerable, d.configurable].join(',');
            """));

    [Fact]
    public void OverwritingAnElementPreservesItsAttributes()
        => Assert.Equal("2,true,false,true", Eval("""
            var a = [];
            Object.defineProperty(a, '0', { value: 1, writable: true, enumerable: false, configurable: true });
            a[0] = 2;
            var d = Object.getOwnPropertyDescriptor(a, '0');
            [a[0], d.writable, d.enumerable, d.configurable].join(',');
            """));

    [Fact]
    public void ASparseWriteExtendsLength()
        => Assert.Equal("6,undefined", Eval("var a = []; a[5] = 1; a.length + ',' + String(a[0]);"));

    [Fact]
    public void AReadOnlyElementRejectsTheWrite()
    {
        Assert.Equal("1", Eval("""
            var a = [];
            Object.defineProperty(a, '0', { value: 1, writable: false });
            a[0] = 2;
            a[0];
            """));

        Assert.Equal("TypeError", Eval("""
            'use strict';
            var a = [];
            Object.defineProperty(a, '0', { value: 1, writable: false });
            (function () { try { a[0] = 2; return 'no-throw'; } catch (e) { return e.constructor.name; } })();
            """));
    }

    [Theory]
    [InlineData("var a = Object.freeze([1, 2]); a[0] = 9;", "1")]
    [InlineData("var a = Object.seal([1, 2]); a[0] = 9;", "9")]
    public void IntegrityLevelsAreRespectedOnOverwrite(string source, string expected)
        => Assert.Equal(expected, Eval(source + " a[0];"));

    [Theory]
    [InlineData("var a = Object.seal([1, 2]); a[5] = 9;", "undefined,2")]
    [InlineData("var a = [1]; Object.preventExtensions(a); a[1] = 2;", "undefined,1")]
    [InlineData("var a = [1]; Object.defineProperty(a, 'length', { writable: false }); a[1] = 2;", "undefined,1")]
    public void GrowthIsBlockedWhereTheSpecSaysSo(string source, string expected)
        => Assert.Equal(expected, Eval(source + " String(a[a.length]) + ',' + a.length;"));

    [Fact]
    public void PlainObjectsWithNumericKeysBehaveTheSame()
        => Assert.Equal("1,2,01|true,true,true", Eval("""
            var o = {};
            o[0] = 1;
            o[1] = 2;
            var d = Object.getOwnPropertyDescriptor(o, '0');
            [o[0], o[1], Object.keys(o).join('')].join(',') + '|'
              + [d.writable, d.enumerable, d.configurable].join(',');
            """));

    // ── prototype interaction ─────────────────────────────────────────────────────────

    [Fact]
    public void AnInheritedIndexedSetterStillRuns()
        => Assert.Equal("s:5|false", Eval("""
            var log = [];
            var proto = {};
            Object.defineProperty(proto, '0', { set: function (v) { log.push('s:' + v); } });
            var o = Object.create(proto);
            o[0] = 5;
            log.join('') + '|' + Object.prototype.hasOwnProperty.call(o, '0');
            """));

    [Fact]
    public void AnInheritedReadOnlyIndexShadowsTheWrite()
        => Assert.Equal("1,false", Eval("""
            var proto = {};
            Object.defineProperty(proto, '0', { value: 1, writable: false });
            var o = Object.create(proto);
            o[0] = 5;
            [o[0], Object.prototype.hasOwnProperty.call(o, '0')].join(',');
            """));

    // ── foreign receivers ─────────────────────────────────────────────────────────────

    [Fact]
    public void ReflectSetWritesToTheReceiverNotTheBase()
        => Assert.Equal("true,1,1,false", Eval("""
            var base = {};
            var rcv = [];
            var r = Reflect.set(base, '0', 1, rcv);
            [r, rcv[0], rcv.length, Object.prototype.hasOwnProperty.call(base, '0')].join(',');
            """));

    [Theory]
    [InlineData("Object.defineProperty(rcv, '0', { value: 5, writable: false });")]
    [InlineData("Object.defineProperty(rcv, '0', { get: function () { return 9; } });")]
    [InlineData("Object.preventExtensions(rcv);")]
    public void ReflectSetFailsWhereTheReceiverForbidsIt(string prepare)
        => Assert.Equal("false", Eval("var base = { 0: 0 }; var rcv = []; " + prepare + " String(Reflect.set(base, '0', 1, rcv));"));

    // ── exotics must keep the general path ────────────────────────────────────────────

    [Fact]
    public void TypedArraysKeepIntegerIndexedSemantics()
        => Assert.Equal("5,undefined,2|44", Eval("""
            var t = new Int8Array(2);
            t[0] = 5;
            t[9] = 7;
            var clamped = [t[0], String(t[9]), t.length].join(',');
            var c = new Int8Array(1);
            c[0] = 300;
            clamped + '|' + c[0];
            """));

    [Fact]
    public void AProxyReceiverStillFiresDefinePropertyTrap()
        => Assert.Equal("dp:0", Eval("""
            var log = [];
            var p = new Proxy([], { defineProperty: function (t, k, d) { log.push('dp:' + k); return Reflect.defineProperty(t, k, d); } });
            Reflect.set({}, '0', 1, p);
            log.join(',');
            """));

    [Fact]
    public void AProxyInThePrototypeChainStillFiresSetTrap()
        => Assert.Equal("set:0|true", Eval("""
            var log = [];
            var p = new Proxy({}, { set: function (t, k, v, r) { log.push('set:' + k); return Reflect.set(t, k, v, r); } });
            var o = Object.create(p);
            o[0] = 1;
            log.join(',') + '|' + Object.prototype.hasOwnProperty.call(o, '0');
            """));

    [Fact]
    public void MappedArgumentsStayMapped()
        => Assert.Equal("9", Eval("function f(x) { arguments[0] = 9; return x; } f(1);"));

    // ── length shrink ─────────────────────────────────────────────────────────────────

    [Fact]
    public void ShrinkingLengthDeletesTheTail()
        => Assert.Equal("2|12|undefined", Eval("var a = [1,2,3,4,5]; a.length = 2; [a.length, a.join(''), String(a[4])].join('|');"));

    [Fact]
    public void ShrinkingHaltsAtTheFirstNonConfigurableElement()
        => Assert.Equal("3,3", Eval("""
            var a = [1,2,3,4,5];
            Object.defineProperty(a, '2', { value: 3, configurable: false });
            a.length = 0;
            [a.length, String(a[2])].join(',');
            """));

    [Fact]
    public void ShrinkingFromAHugeSparseLengthStaysCheap()
        // The range here spans the whole uint domain, so this must take the scan-the-stored-
        // elements strategy rather than walking [newLength, oldLength).
        => Assert.Equal("1,1,undefined", Eval("""
            var a = [];
            a[0] = 1;
            a[4294967294] = 2;
            a.length = 1;
            [a.length, a[0], String(a[1])].join(',');
            """));

    [Fact]
    public void PoppingEveryElementIsCorrect()
        // Guards the quadratic shrink: each pop used to scan and sort the entire element
        // table, so this loop took minutes at 200k. Kept small enough to stay a unit test.
        => Assert.Equal("1999000,0", Eval("""
            var a = [];
            for (var i = 0; i < 2000; i++) a.push(i);
            var s = 0;
            while (a.length) s += a.pop();
            s + ',' + a.length;
            """));

    [Fact]
    public void GrowingThenShrinkingLengthKeepsElements()
        => Assert.Equal("10,1,1", Eval("var a = [1,2,3]; a.length = 10; var g = a.length; a.length = 1; g + ',' + a.length + ',' + a.join('');"));

    // ── push ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void PushReturnsTheNewLength()
        => Assert.Equal("3,123", Eval("var a = [1]; a.push(2, 3) + ',' + a.join('');"));

    [Theory]
    [InlineData("var a = Object.freeze([]);")]
    [InlineData("var a = []; Object.defineProperty(a, 'length', { writable: false });")]
    public void PushRejectsAnUnwritableTarget(string prepare)
        => Assert.Equal("TypeError", Eval(prepare + " (function () { try { a.push(1); return 'no-throw'; } catch (e) { return e.constructor.name; } })();"));

    [Fact]
    public void PushWorksOnAPlainArrayLike()
        => Assert.Equal("3,x", Eval("var o = { length: 2 }; Array.prototype.push.call(o, 'x'); [o.length, o[2]].join(',');"));

    [Fact]
    public void PushKeepsHolesAsHoles()
        => Assert.Equal("3,undefined,9", Eval("var a = []; a.length = 2; a.push(9); [a.length, String(a[0]), a[2]].join(',');"));

    // ── the filled array still behaves ordinarily ─────────────────────────────────────

    [Fact]
    public void AFilledArrayEnumeratesAndSerializesNormally()
        => Assert.Equal("012|[0,1,2]|1,,3", Eval("""
            var a = [];
            for (var i = 0; i < 3; i++) a[i] = i;
            var keys = [];
            for (var k in a) keys.push(k);
            var sparse = [];
            sparse[0] = 1;
            sparse[2] = 3;
            keys.join('') + '|' + JSON.stringify(a) + '|' + sparse.join(',');
            """));
}
