using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Issue #830 (problems 40, 66): a Proxy with no "set" trap forwards [[Set]] straight to its
// target with the original receiver, so the target's getOwnPropertyDescriptor is consulted
// exactly once per assignment (not twice). Mirrors test262
// Proxy/set/trap-is-missing-receiver-multiple-calls(-index).
public class Issue830ProxySetForwardTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    [Theory]
    // String key: three assignments through a trap-less proxy → three gopd on the target.
    [InlineData("""
        const log = [];
        const target = new Proxy({ foo: 1 }, {
          getOwnPropertyDescriptor(t, k) { log.push(String(k)); return Reflect.getOwnPropertyDescriptor(t, k); } });
        const p = new Proxy(target, {});
        p.foo = 2; p.foo = 3; p.foo = 4;
        log.join(",");
        """, "foo,foo,foo")]
    // Integer-index key behaves the same.
    [InlineData("""
        const log = [];
        const target = new Proxy({ 0: 1 }, {
          getOwnPropertyDescriptor(t, k) { log.push(String(k)); return Reflect.getOwnPropertyDescriptor(t, k); } });
        const p = new Proxy(target, {});
        p[0] = 2; p[0] = 3; p[0] = 4;
        log.join(",");
        """, "0,0,0")]
    public void GetOwnPropertyDescriptorOncePerSet(string source, string expected)
        => Assert.Equal(expected, Eval(source));

    [Theory]
    // Forwarding still works: the write reaches the target.
    [InlineData("const t = { x: 1 }; const p = new Proxy(t, {}); p.x = 5; t.x;", "5")]
    // An accessor on the target runs its setter with the proxy as receiver.
    [InlineData("let seen; const t = { set x(v) { seen = this; } }; const p = new Proxy(t, {}); p.x = 1; seen === p;", "true")]
    // A getter-only accessor rejects the assignment in strict mode.
    [InlineData("'use strict'; const t = { get x() { return 1; } }; const p = new Proxy(t, {}); try { p.x = 2; 'no-throw'; } catch (e) { e.constructor.name; }", "TypeError")]
    public void ForwardingSemanticsPreserved(string expr, string expected)
        => Assert.Equal(expected, Eval(expr));
}
