using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/828 Problem 4.
//
// Two Proxy bugs surfaced while investigating the failing sm/Iterator/.../proxy-accesses cases:
//
//  1. An ordinary property write `obj.p = v` performs [[Set]](P, V, Receiver) with the object
//     itself as the Receiver. The KeyString and Symbol property-indexer setters passed a null
//     receiver; for a plain object `receiver ?? this` hid that, but a Proxy forwards the receiver
//     straight to its `set` trap, so the trap observed `undefined` instead of the proxy — the
//     failing test logged trap arguments and threw "Cannot get property toString of undefined".
//
//  2. A Proxy delegated `===` (StrictEquals) and `==` (Equals) to its target, so `p === p` was
//     false and `p === target` was true. A proxy has its own identity and must compare by
//     reference; it now inherits JSObject's identity comparison.
public class Issue828ProxySetReceiverTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    // A string-keyed write surfaces the proxy as the set trap's receiver.
    [Fact]
    public void StringKeyedSetTrapReceivesProxyAsReceiver()
        => Assert.Equal("true,object", Eval("""
            var seen;
            var p = new Proxy({}, { set: function(t, k, v, receiver){ seen = receiver; t[k] = v; return true; } });
            p.x = 1;
            (seen === p) + ',' + (typeof seen);
        """));

    // A symbol-keyed write likewise passes the proxy as the receiver.
    [Fact]
    public void SymbolKeyedSetTrapReceivesProxyAsReceiver()
        => Assert.Equal("true", Eval("""
            var s = Symbol('s');
            var seen;
            var p = new Proxy({}, { set: function(t, k, v, receiver){ seen = receiver; t[k] = v; return true; } });
            p[s] = 1;
            '' + (seen === p);
        """));

    // The receiver is the proxy even when the write happens via `this` inside a method
    // invoked through the proxy (the sm/Iterator proxy-accesses shape: `this.value++`).
    [Fact]
    public void ThisWriteThroughProxyReceivesProxyAsReceiver()
        => Assert.Equal("true", Eval("""
            var seen;
            var target = { bump: function(){ this.value = (this.value || 0) + 1; }, value: 0 };
            var p = new Proxy(target, { set: function(t, k, v, receiver){ seen = receiver; t[k] = v; return true; } });
            p.bump();
            '' + (seen === p);
        """));

    // A proxy compares equal only to itself, never to its target.
    [Fact]
    public void ProxyComparesByOwnIdentity()
        => Assert.Equal("true,false,false,true", Eval("""
            var t = {};
            var p = new Proxy(t, {});
            (p === p) + ',' + (p === t) + ',' + (p == t) + ',' + ([p].indexOf(p) === 0);
        """));

    // Distinct proxies over the same target are distinct values.
    [Fact]
    public void DistinctProxiesOverSameTargetAreNotEqual()
        => Assert.Equal("false", Eval("""
            var t = {};
            var a = new Proxy(t, {}), b = new Proxy(t, {});
            '' + (a === b);
        """));

    // The getOwnPropertyDescriptor trap result is converted with ToPropertyDescriptor:
    // each field is read through the result's own [[HasProperty]]/[[Get]] (observable) in the
    // fixed enumerable/configurable/value/writable/get/set order, and the caller gets a plain
    // record carrying those values — not the raw trap object.
    [Fact]
    public void GetOwnPropertyDescriptorReadsTrapResultFieldsInOrder()
        => Assert.Equal("enumerable,configurable,value,writable|1|true", Eval("""
            var reads = [];
            var p = new Proxy({}, {
              getOwnPropertyDescriptor: function(t, k){
                return new Proxy({ value: 1, writable: true, enumerable: true, configurable: true }, {
                  get: function(dt, dk, r){ reads.push(dk); return Reflect.get(dt, dk, r); }
                });
              }
            });
            var d = Object.getOwnPropertyDescriptor(p, 'x');
            reads.join(',') + '|' + d.value + '|' + d.writable;
        """));
}
