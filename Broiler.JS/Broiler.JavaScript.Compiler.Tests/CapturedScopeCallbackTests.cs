using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Compiler.Tests;

// A closure carries the scopes it was CREATED in — the bindings of the direct eval that created
// it (see DirectEvalClosureScopeTests) and the `with` chain it was written inside. Neither is part
// of the compiled body: both are re-established for the duration of a call.
//
// [[Call]] from a native builtin took a separate, cheaper path that re-established neither. So the
// same function resolved its free names when called from JavaScript and threw "b is not defined"
// the moment it was handed to Array.prototype.map, Set/Map.prototype.forEach, a JSON reviver, or
// any other builtin that calls back into script — an error that names a binding the reader can see
// two lines up, raised only on the callback path.
//
// google.com reaches this: its bot-detection VM evaluates its own opcode handlers with
// `function(X){return eval(X)}(src)` and drives them through builtin callbacks.
public class CapturedScopeCallbackTests
{
    private static string Eval(string expression)
    {
        using var context = new JSContext();
        return context.Eval($"String({expression})", "t.js").ToString();
    }

    // `f` is created by a direct eval and closes over the caller's `b`; `body` then calls it
    // through one builtin or another.
    private static string ThroughCallback(string body)
        => Eval("(function(){ var b = 21;"
            + " var f = eval('0,function(x){ return b + (x|0); }');"
            + $" {body} }})()");

    [Theory]
    // The reported shape: a direct-eval closure handed straight to a builtin.
    [InlineData("return [1].map(f)[0];", "22")]
    [InlineData("var r; [1].forEach(function(x){ r = f(x); }); return r;", "22")]
    [InlineData("var r; [1].forEach(f); return 'ran';", "ran")]
    [InlineData("return [1].filter(f).length;", "1")]
    [InlineData("return [1].find(f);", "1")]
    [InlineData("return [1].some(f);", "true")]
    [InlineData("return [1].every(f);", "true")]
    [InlineData("return [1].flatMap(f)[0];", "22")]
    [InlineData("return [1].reduce(function(a, x){ return f(x); }, 0);", "22")]
    [InlineData("return [2,1].sort(f).length;", "2")]
    [InlineData("new Set([1]).forEach(f); return 'ran';", "ran")]
    [InlineData("new Map([[1,1]]).forEach(f); return 'ran';", "ran")]
    // A reviver/replacer is called with (key, value), so `f` adds b to the key (""|0 === 0).
    [InlineData("return JSON.parse('1', f);", "21")]
    [InlineData("return JSON.stringify(1, f);", "21")]
    [InlineData("return new Int32Array([1]).map(f)[0];", "22")]
    [InlineData("return new Int32Array([1]).filter(f).length;", "1")]
    [InlineData("return Array.from([1], f)[0];", "22")]
    public void ADirectEvalClosure_ResolvesItsBinding_WhenABuiltinCallsIt(string body, string expected)
    {
        Assert.Equal(expected, ThroughCallback(body));
    }

    // The same call, made from JavaScript — what already worked, and what the callback path
    // now agrees with.
    [Fact(Timeout = 600000)]
    public void ADirectEvalClosure_ResolvesItsBinding_WhenCalledDirectly()
    {
        Assert.Equal("22", ThroughCallback("return f(1);"));
    }

    // A `with`-captured closure loses its object environment the same way.
    [Fact(Timeout = 600000)]
    public void AWithClosure_ResolvesThroughItsObject_WhenABuiltinCallsIt()
    {
        Assert.Equal(
            "22",
            Eval("(function(){ var o = { b: 21 }; var f;"
                + " with (o) { f = function(x){ return b + x; }; }"
                + " return [1].map(f)[0]; })()"));
    }

    // The binding is shared, not snapshotted, on the callback path too: a write made after the
    // function was created is visible to the builtin's call of it.
    [Fact(Timeout = 600000)]
    public void TheBindingIsShared_OnTheCallbackPath()
    {
        Assert.Equal(
            "5",
            Eval("(function(){ var b = 1; var f = eval('0,function(){ return b; }'); b = 5;"
                + " return [0].map(f)[0]; })()"));
    }

    // ...and a write from inside the callback reaches the caller's binding rather than creating
    // a global.
    [Fact(Timeout = 600000)]
    public void AWriteFromTheCallback_ReachesTheCallersBinding()
    {
        Assert.Equal(
            "7,undefined",
            Eval("(function(){ var b = 1; var f = eval('0,function(){ b = 7; }');"
                + " [0].forEach(f); return b + ',' + typeof globalThis.b; })()"));
    }

    // What must NOT change: an ordinary closure keeps resolving through its own compiled body,
    // and a genuinely undeclared name still throws from a callback.
    [Fact(Timeout = 600000)]
    public void AnOrdinaryClosure_IsUnaffected()
    {
        Assert.Equal("22", Eval("(function(){ var b = 21; return [1].map(function(x){ return b + x; })[0]; })()"));
    }

    [Fact(Timeout = 600000)]
    public void AGenuinelyUndeclaredName_StillThrowsFromACallback()
    {
        Assert.Equal(
            "ReferenceError",
            Eval("(function(){ var b = 1; var f = eval('0,function(){ return zzNopeCallback; }');"
                + " try { return [0].map(f)[0]; } catch (e) { return e.constructor.name; } })()"));
    }
}
