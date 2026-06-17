using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/828
//
// A direct eval that runs inside another function captures the enclosing globals
// into a transient overlay so the eval body can resolve them. On teardown the
// overlay must leave a pre-existing global-object property exactly as the eval left
// it — the property is the binding's true store, so a write the eval performed must
// persist and a `delete` the eval performed must stick. The teardown previously
// restored the property's pre-overlay value unconditionally, which silently reverted
// the write and resurrected a deleted global.
//
// Out of scope (still a deferred, separate issue, see Issue709Tests): a closure
// DEFINED INSIDE the eval that resolves such a name reads it as a stale captured
// binding rather than re-resolving it through the (now-mutated) global environment,
// so reading a deleted global through such a closure yields undefined instead of a
// ReferenceError. These tests therefore observe the global object directly.
public class Issue828EvalGlobalMutationTests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // A nested eval that assigns to an eval-created global must persist that write to
    // the global object (it was reverted to the pre-overlay value on teardown).
    [Fact]
    public void NestedEvalWriteToGlobalPersists()
        => Assert.Equal("42", Eval(
            "var fns = eval(\"var y = 5; function setY(v){ eval('y = ' + v); } [setY];\");" +
            "fns[0](42);" +
            "'' + globalThis.y;"));

    // A nested eval that deletes an eval-created (configurable) global must actually
    // remove the global-object property (it was resurrected on teardown).
    [Fact]
    public void NestedEvalDeleteOfGlobalRemovesProperty()
        => Assert.Equal("true,false,undefined", Eval(
            "var fns = eval(\"var y = 5; function delY(){ return eval('delete y'); } [delY];\");" +
            "var deleted = fns[0]();" +
            "deleted + ',' + ('y' in globalThis) + ',' + (typeof globalThis.y);"));

    // A top-level (script) `var` is non-configurable: deleting it returns false and the
    // binding stays (regression guard — the teardown change must not make script vars deletable).
    [Fact]
    public void EvalCreatedGlobalIsConfigurableButScriptVarIsNot()
        => Assert.Equal("true,false", Eval(
            "var configurable = eval(\"var a = 1; delete a;\");" +
            "var b = 2;" +
            "var nonConfigurable = delete b;" +
            "configurable + ',' + nonConfigurable;"));
}
