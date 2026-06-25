using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/914
//
// P4 (test/staging/sm/Proxy/global-receiver.js): the global object can be the receiver
// passed to the get/set traps of a Proxy installed as its prototype. Unqualified global
// identifier resolution must go through the global object's [[HasProperty]] / [[Get]] /
// [[Set]] (which walk the prototype chain and fire the Proxy traps with the global as the
// receiver), not a structural lookup.
public class Issue914ProxyGlobalReceiverTests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // The proxy installed as the global's prototype, with a `has` trap that claims
    // "bareword" and get/set traps that count calls and assert the receiver is the global.
    private const string Setup =
        "var global = this;" +
        "var proto = Object.getPrototypeOf(global);" +
        "var gets = 0, sets = 0;" +
        "Object.setPrototypeOf(global, new Proxy(proto, {" +
        "  has(t, id) { return id === 'bareword' || Reflect.has(t, id); }," +
        "  get(t, id, r) { gets++; if (r !== global) throw new Error('get receiver not global'); return Reflect.get(t, id, r); }," +
        "  set(t, id, v, r) { sets++; if (r !== global) throw new Error('set receiver not global'); return Reflect.set(t, id, v, r); }" +
        "}));";

    // Reading an unqualified name the prototype proxy claims yields the proxy's value
    // (undefined here) and fires the `get` trap exactly once with the global as receiver.
    [Fact]
    public void UnqualifiedReadGoesThroughProxyGetTrap()
    {
        Assert.Equal("undefined", Eval(Setup + "'' + bareword"));
        Assert.Equal("1", Eval(Setup + "bareword; '' + gets"));
    }

    // A sloppy assignment to an unresolved name is Set(global, ...): it fires the proxy
    // `set` trap with the global as receiver and creates the own property on the global.
    [Fact]
    public void UnqualifiedSloppyWriteGoesThroughProxySetTrap()
    {
        Assert.Equal("1", Eval(Setup + "bareword = 12; '' + sets"));
        Assert.Equal("12", Eval(Setup + "bareword = 12; '' + global.bareword"));
    }

    // The full sequence of the test262 file's four assertions.
    [Fact]
    public void FullGlobalReceiverSequence()
        => Assert.Equal("true,1,1,12", Eval(Setup +
            "var r = (bareword === undefined);" +
            "var g = gets;" +
            "bareword = 12;" +
            "var s = sets;" +
            "r + ',' + g + ',' + s + ',' + global.bareword"));

    // A genuinely undeclared name (ordinary prototype, no proxy) still throws.
    [Fact]
    public void GenuinelyUndeclaredNameStillThrowsReferenceError()
        => Assert.Equal("ReferenceError", Eval(
            "var t; try { undeclaredNameNowhere; t = 'no-throw'; } catch (e) { t = e.constructor.name; } t"));

    // typeof of a truly-undeclared name remains undefined (no throw).
    [Fact]
    public void TypeofUndeclaredRemainsUndefined()
        => Assert.Equal("undefined", Eval("typeof anotherUndeclaredName"));
}
