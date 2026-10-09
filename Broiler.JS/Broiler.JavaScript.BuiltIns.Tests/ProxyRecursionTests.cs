using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

public class ProxyRecursionTests
{
    private static string Eval(string source)
    {
        RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    // CreepJS creates a cycle through an exotic object. The prototype assignment
    // may succeed; exhausting the lookup must be a catchable JS error, not a
    // native stack overflow. Each case also checks finally and repeated recovery.
    [Theory]
    [InlineData("Object.setPrototypeOf(p, Object.create(p))", "p.toString()")]
    [InlineData("Reflect.setPrototypeOf(p, Object.create(p))", "p.missing")]
    [InlineData("Object.setPrototypeOf(p, Object.create(p))", "p[42]")]
    [InlineData("Object.setPrototypeOf(p, Object.create(p))", "p[Symbol.iterator]")]
    [InlineData("Reflect.setPrototypeOf(p, Object.create(p))", "'missing' in p")]
    [InlineData("Reflect.setPrototypeOf(p, Object.create(p))", "42 in p")]
    [InlineData("Reflect.setPrototypeOf(p, Object.create(p))", "Symbol.iterator in p")]
    [InlineData("p.__proto__ = p", "p++")]
    [InlineData("Object.setPrototypeOf(p, Object.create(p))", "p.missing = 1")]
    [InlineData("Object.setPrototypeOf(p, Object.create(p))", "p[42] = 1")]
    [InlineData("Object.setPrototypeOf(p, Object.create(p))", "p[Symbol.iterator] = 1")]
    public void PrototypeCycleThrowsRangeErrorAndRecovers(string setup, string operation)
    {
        Assert.Equal("3:3:42", Eval($$"""
            let caught = 0, cleaned = 0;
            for (let i = 0; i < 3; i++) {
                const target = function example() {};
                const original = Object.getPrototypeOf(target);
                let p = new Proxy(target, {});
                {{setup}};
                try { {{operation}}; }
                catch (e) { if (e instanceof RangeError) caught++; else throw e; }
                finally { Object.setPrototypeOf(target, original); cleaned++; }
                if (typeof target.toString() !== 'string') throw new Error('restore failed');
            }
            caught + ':' + cleaned + ':' + new Proxy({answer: 42}, {}).answer;
            """));
    }

    // Reading the handler's trap can itself recurse without invoking JS code.
    [Fact]
    public void CyclicHandlerLookupThrowsRangeError()
        => Assert.Equal("RangeError:42", Eval("""
            const handler = {};
            const p = new Proxy({}, handler);
            Object.setPrototypeOf(handler, p);
            let result;
            try { p.x; } catch (e) { result = e.name; }
            finally { Object.setPrototypeOf(handler, null); }
            result + ':' + new Proxy({x: 42}, {}).x;
            """));

    [Theory]
    [InlineData("getPrototypeOf", "Object.getPrototypeOf(p)")]
    [InlineData("setPrototypeOf", "Reflect.setPrototypeOf(p, null)")]
    [InlineData("getOwnPropertyDescriptor", "Object.getOwnPropertyDescriptor(p, 'x')")]
    [InlineData("defineProperty", "Reflect.defineProperty(p, 'x', {value: 1})")]
    [InlineData("deleteProperty", "delete p.x")]
    [InlineData("ownKeys", "Reflect.ownKeys(p)")]
    [InlineData("isExtensible", "Object.isExtensible(p)")]
    [InlineData("preventExtensions", "Reflect.preventExtensions(p)")]
    public void RecursiveTrapThrowsRangeError(string trap, string operation)
        => Assert.Equal("RangeError", Eval($$"""
            let p;
            p = new Proxy({}, { {{trap}}() { return {{operation}}; } });
            let result;
            try { {{operation}}; } catch (e) { result = e.name; }
            result;
            """));

    [Fact]
    public void DeepAcyclicProxyChainPreservesReceiverAndKeys()
        => Assert.Equal("true:7:8:9:11", Eval("""
            const key = Symbol('key');
            const target = {x: 7, 42: 8, [key]: 9, get receiver() { return this; }};
            let p = target;
            for (let i = 0; i < 128; i++) p = new Proxy(p, {});
            const values = [p.receiver === p, p.x, p[42], p[key]];
            p.x = 11;
            values.push(target.x);
            values.join(':');
            """));

    [Fact]
    public void FiniteReentrantTrapIsAllowed()
        => Assert.Equal("42", Eval("""
            let depth = 0, p;
            p = new Proxy({}, {get() { return ++depth < 40 ? p.x : 42; }});
            p.x;
            """));

    [Fact]
    public void OrdinaryCycleRejectionAndRevocationAreUnchanged()
        => Assert.Equal("TypeError:false:TypeError", Eval("""
            const target = {};
            const child = Object.create(target);
            const result = [];
            try { Object.setPrototypeOf(target, child); } catch (e) { result.push(e.name); }
            result.push(Reflect.setPrototypeOf(target, child));
            const revocable = Proxy.revocable({}, {});
            revocable.revoke();
            try { revocable.proxy.x; } catch (e) { result.push(e.name); }
            result.join(':');
            """));

    // Browser callbacks often run on a smaller stack than the standalone shell.
    // Exercise the CreepJS pattern with native functions and JS calls in both the
    // catch and finally paths, not only a catch that reads the exception name.
    [Theory]
    [InlineData(512 * 1024)]
    [InlineData(1024 * 1024)]
    public void NativeFunctionProbeLeavesStackForCatchAndFinally(int stackSize)
    {
        string? result = null;
        ExceptionDispatchInfo? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                result = Eval("""
                    function classify(e) { return e instanceof RangeError ? 'range' : e.name; }
                    function runProbe(action, restore) {
                        try { action(); return 'missing'; }
                        catch (e) { return classify(e); }
                        finally { restore(); }
                    }
                    const target = Function.prototype.toString;
                    const proto = Object.getPrototypeOf(target);
                    const results = [];
                    for (let i = 0; i < 5; i++) {
                        const p = new Proxy(target, {});
                        results.push(runProbe(
                            () => Object.setPrototypeOf(p, Object.create(p)).toString(),
                            () => Object.setPrototypeOf(p, proto)));
                    }
                    results.push(new Proxy({ok: 42}, {}).ok);
                    results.join(':');
                    """);
            }
            catch (Exception e) { failure = ExceptionDispatchInfo.Capture(e); }
        }, stackSize) { IsBackground = true };
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "Proxy probe did not terminate");
        failure?.Throw();
        Assert.Equal("range:range:range:range:range:42", result);
    }
}
