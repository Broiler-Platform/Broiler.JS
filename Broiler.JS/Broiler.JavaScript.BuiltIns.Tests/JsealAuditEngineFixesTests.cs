using System.Runtime.CompilerServices;
using Broiler.JavaScript.BuiltIns.Array.Typed;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Storage;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Engine defects found by the Broiler.JSeal provider audits (JSeal roadmap, "Follow-up audits"):
//
//  1. J05 follow-up — a Proxy whose target is not callable has no [[Call]] (ProxyCreate,
//     §10.5.14). Calling it must throw TypeError at IsCallable before any trap runs; the engine
//     ran the handler's `apply` trap and returned its result.
//  2. J09 follow-up — own-key enumeration (the engine's GetAllKeys enumerator, which
//     Object.getOwnPropertyNames, Object.keys, for-in and a host's own-key listing are built
//     on) read every indexed element's value and so ran an accessor's getter. Listing keys has
//     no such side effect (§10.1.11 OrdinaryOwnPropertyKeys).
//  3. A buffer minted by the engine or its host (JSArrayBuffer(byte[]) / JSArrayBuffer(int)) is
//     AllocateArrayBuffer(%ArrayBuffer%, …) and takes the realm's intrinsic
//     %ArrayBuffer.prototype%, not whatever the global `ArrayBuffer` binding names now.
public class JsealAuditEngineFixesTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    // ---- 1. noncallable Proxy with an apply trap -------------------------------------------

    [Theory]
    [InlineData("p()")]
    [InlineData("Function.prototype.call.call(p)")]
    [InlineData("Function.prototype.apply.call(p, null, [])")]
    [InlineData("Reflect.apply(p, null, [])")]
    [InlineData("(p.call = Function.prototype.call, p.call())")]
    public void CallingNoncallableProxyThrowsTypeErrorWithoutRunningApplyTrap(string call)
        => Assert.Equal("TypeError,0", Eval($$"""
            var trapRuns = 0;
            var p = new Proxy({}, { apply: function () { trapRuns++; return 42; } });
            var outcome;
            try { outcome = String({{call}}); } catch (e) { outcome = e.constructor.name; }
            outcome + ',' + trapRuns;
        """));

    [Fact(Timeout = 600000)]
    public void NoncallableProxyStaysAnObjectAndCallableProxyStillRunsItsTrap()
        => Assert.Equal("object,function,43", Eval("""
            var p = new Proxy({}, { apply: function () { return 42; } });
            var f = new Proxy(function () { return 1; }, { apply: function () { return 43; } });
            [typeof p, typeof f, f()].join(',');
        """));

    // ---- 2. key enumeration runs no getter -------------------------------------------------

    [Theory]
    [InlineData("Object.getOwnPropertyNames(o)", "0,x")]
    [InlineData("Object.keys(o)", "0,x")]
    [InlineData("Reflect.ownKeys(o)", "0,x")]
    [InlineData("(function(){ var r = []; for (var k in o) r.push(k); return r; })()", "0,x")]
    [InlineData("Object.getOwnPropertyNames(a)", "0,1,length")]
    [InlineData("Object.keys(a)", "0,1")]
    [InlineData("Reflect.ownKeys(a)", "0,1,length")]
    [InlineData("(function(){ var r = []; for (var k in a) r.push(k); return r; })()", "0,1")]
    public void OwnKeyEnumerationDoesNotInvokeIndexedGetters(string keys, string expected)
        => Assert.Equal(expected + "|0", Eval($$"""
            var calls = 0;
            var getter = { get: function () { calls++; return 1; }, enumerable: true, configurable: true };
            var o = {}; Object.defineProperty(o, '0', getter); Object.defineProperty(o, 'x', getter);
            var a = [, 2]; Object.defineProperty(a, '0', getter);
            String({{keys}}) + '|' + calls;
        """));

    [Theory]
    [InlineData("({})")]
    [InlineData("([])")]
    public void HostOwnKeyEnumeratorDoesNotInvokeIndexedGetters(string literal)
    {
        Load();
        using var ctx = new JSContext();
        var target = ctx.Eval($$"""
            var calls = 0;
            var t = {{literal}};
            Object.defineProperty(t, '0', { get: function () { calls++; return 1; }, enumerable: true, configurable: true });
            t;
        """);

        // The traversal a host's own-name listing performs (Broiler.JSeal's OwnPropertyNames).
        var names = new List<string>();
        var keys = target.GetAllKeys(showEnumerableOnly: true, inherited: false);
        while (keys.MoveNext(out var key))
            names.Add(key.ToString());

        // And the three-value form the engine's own for-in snapshot uses.
        var indices = target.GetAllKeys(showEnumerableOnly: false, inherited: false);
        while (indices.MoveNext(out var hasValue, out var _, out var _))
            Assert.True(hasValue);

        Assert.Equal(new[] { "0" }, names);
        Assert.Equal("0", ctx.Eval("String(calls)").ToString());
    }

    [Fact(Timeout = 600000)]
    public void OwnKeyEnumerationOfAnIteratorHelperDoesNotAdvanceIt()
        => Assert.Equal("|||0|1,2,3", Eval("""
            var nextCalls = 0;
            var it = [1, 2, 3].values().map(function (v) { nextCalls++; return v; });
            var keys = [Object.keys(it), Object.getOwnPropertyNames(it)];
            var forIn = []; for (var k in it) forIn.push(k);
            keys.concat([forIn]).join('|') + '|' + nextCalls + '|' + [...it].join(',');
        """));

    [Fact(Timeout = 600000)]
    public void ValueReadingWalksStillRunGetters()
        => Assert.Equal("1,1|2", Eval("""
            var calls = 0;
            var o = {}; Object.defineProperty(o, '0', { get: function () { calls++; return 1; }, enumerable: true });
            var a = []; Object.defineProperty(a, '0', { get: function () { calls++; return 1; }, enumerable: true });
            [Object.values(o), Object.values(a)].join(',') + '|' + calls;
        """));

    // ---- truncated source reaching the compiler through eval / Function ----------------------

    [Theory]
    [InlineData("z +")]
    [InlineData("1 +")]
    [InlineData("`\n\n` + ;")]
    [InlineData("a &&")]
    [InlineData("f(1,")]
    [InlineData("a[1")]
    [InlineData("x = {a")]
    [InlineData("`a${1")]
    [InlineData("/* open")]
    [InlineData("'open\n'")]
    public void TruncatedEvalSourceInsideAFunctionIsASyntaxError(string source)
        => Assert.Equal("SyntaxError,SyntaxError", Eval($$"""
            var z = 1, a = 1, x;
            function f() {}
            function viaEval(s) { try { eval(s); return 'accepted'; } catch (e) { return e.constructor.name; } }
            function viaFunction(s) { try { Function(s); return 'accepted'; } catch (e) { return e.constructor.name; } }
            viaEval({{System.Text.Json.JsonSerializer.Serialize(source)}}) + ',' + viaFunction({{System.Text.Json.JsonSerializer.Serialize(source)}});
        """));

    // ---- 3. host/engine-minted ArrayBuffers take the intrinsic prototype --------------------

    [Fact(Timeout = 600000)]
    public void HostMintedArrayBufferTakesIntrinsicPrototypeAfterGlobalIsReplaced()
    {
        Load();
        using var ctx = new JSContext();
        ctx.Eval("""
            var intrinsic = ArrayBuffer.prototype;
            globalThis.ArrayBuffer = function Impostor() {};
            ArrayBuffer.prototype = { impostor: true };
        """);

        ctx[KeyStrings.GetOrCreate("fromBytes")] = new JSArrayBuffer(new byte[] { 1, 2, 3 });
        ctx[KeyStrings.GetOrCreate("fromLength")] = new JSArrayBuffer(4);

        Assert.Equal("true,true,3,4,true", ctx.Eval("""
            [Object.getPrototypeOf(fromBytes) === intrinsic,
             Object.getPrototypeOf(fromLength) === intrinsic,
             fromBytes.byteLength,
             fromLength.byteLength,
             Object.getPrototypeOf(new Uint8Array(2).buffer) === intrinsic].join(',');
        """).ToString());
    }

    [Fact(Timeout = 600000)]
    public void HostMintedArrayBufferDoesNotReadTheGlobalBinding()
    {
        Load();
        using var ctx = new JSContext();
        ctx.Eval("""
            var intrinsic = ArrayBuffer.prototype;
            var reads = 0;
            var original = ArrayBuffer;
            Object.defineProperty(globalThis, 'ArrayBuffer',
                { get: function () { reads++; return original; }, configurable: true });
            reads = 0;
        """);

        ctx[KeyStrings.GetOrCreate("minted")] = new JSArrayBuffer(new byte[] { 7 });

        Assert.Equal("0,true", ctx.Eval("[reads, Object.getPrototypeOf(minted) === intrinsic].join(',')").ToString());
    }

    [Fact(Timeout = 600000)]
    public void SharedArrayBufferSliceKeepsSharedPrototype()
        => Assert.Equal("true,false", Eval("""
            var s = new SharedArrayBuffer(4).slice(1);
            [Object.getPrototypeOf(s) === SharedArrayBuffer.prototype,
             Object.getPrototypeOf(s) === ArrayBuffer.prototype].join(',');
        """));

    [Fact(Timeout = 600000)]
    public void ScriptConstructedArrayBufferStillFollowsNewTarget()
        => Assert.Equal("true,true", Eval("""
            class Sub extends ArrayBuffer {}
            var b = new Sub(2);
            [Object.getPrototypeOf(b) === Sub.prototype, b instanceof ArrayBuffer].join(',');
        """));
}
