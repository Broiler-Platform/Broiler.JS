using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Issue #830 (problems 92, 93, 94): a `with` object environment record's HasBinding probes
// the binding object's [[HasProperty]] exactly once and then consults @@unscopables — it must
// not re-probe afterwards. The subsequent read/write (GetBindingValue / SetMutableBinding)
// performs its own single HasProperty. Previously the resolver fired one extra `has` trap.
// Mirrors test262 with/{has-binding-call,get-binding-value-idref,set-mutable-binding-idref}-with-proxy-env.
public class Issue830WithProxyEnvTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    // A `with` over this proxy logs has/get/set trap calls. The trap bodies capture everything
    // lexically (log, US) and use only their parameters, so no free global identifier is resolved
    // through the active `with` scope while a trap runs.
    private const string Pre = """
        const log = [];
        const US = Symbol.unscopables;
        const p = new Proxy({ x: 1 }, {
          has(t, k) { log.push("has"); return k in t; },
          get(t, k) { log.push(k === US ? "unscopables" : "get"); return t[k]; },
          set(t, k, v) { log.push("set"); t[k] = v; return true; },
        });
        """;

    [Fact]
    // Reading: HasBinding [has, unscopables] then GetBindingValue [has, get] — a single has each.
    public void GetBindingValueSequence()
        => Assert.Equal("has,unscopables,has,get", Eval(Pre + "with (p) { x; } log.join(\",\");"));

    [Fact]
    // Writing: HasBinding [has, unscopables] then SetMutableBinding [has, set].
    public void SetMutableBindingSequence()
        => Assert.Equal("has,unscopables,has,set", Eval(Pre + "with (p) { x = 2; } log.join(\",\");"));

    [Theory]
    // A name the with-object lacks falls through to the outer binding.
    [InlineData("var a = 5; var r; with ({ b: 1 }) { r = a + 1; } r;", "6")]
    // Reading and writing observe the with-object's property.
    [InlineData("var r; with ({ x: 10 }) { x = x + 1; r = x; } r;", "11")]
    // @@unscopables blocks the binding, so the reference falls through (here to a ReferenceError).
    [InlineData("var hit = false; with ({ a: 1, [Symbol.unscopables]: { a: true } }) { try { a; } catch (e) { hit = e instanceof ReferenceError; } } hit;", "true")]
    public void Behavior(string source, string expected)
        => Assert.Equal(expected, Eval(source));
}
