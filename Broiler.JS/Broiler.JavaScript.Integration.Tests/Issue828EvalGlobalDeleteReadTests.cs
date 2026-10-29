using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/828 Problem 3.
//
// An eval-introduced global `var` is configurable and deletable. Once `delete` removes it, a
// later read — including from a closure created in the eval — must throw a ReferenceError rather
// than observe the now-absent global-object property as `undefined` (test262
// staging/sm/eval/exhaustive-global-*). The read goes through the throwing global resolution,
// which reads the live property (so an eval reassigning an existing global is still seen) before
// the possibly-stale globalVars mirror; `typeof` keeps the non-throwing path.
public class Issue828EvalGlobalDeleteReadTests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // A closure defined in the eval reads the eval-created global; after delete the read throws.
    [Fact]
    public void ClosureReadOfDeletedEvalGlobalThrows()
        => Assert.Equal("1,true,ReferenceError", Eval("""
            var fns = eval("var z = 1; function gz(){ return z; } function delZ(){ return delete z; } [gz, delZ];");
            var gz = fns[0], delZ = fns[1];
            var before = gz();
            var deleted = delZ();
            var after;
            try { after = gz(); } catch (e) { after = e.name; }
            before + ',' + deleted + ',' + after;
        """));

    // `typeof` of a deleted eval global is "undefined", never a ReferenceError.
    [Fact]
    public void TypeofDeletedEvalGlobalIsUndefined()
        => Assert.Equal("undefined", Eval("""
            var fns = eval("var z = 1; function tz(){ return typeof z; } function delZ(){ delete z; } [tz, delZ];");
            fns[1]();
            fns[0]();
        """));

    // An eval that reassigns an existing global var is observed by later reads (no stale mirror).
    [Fact]
    public void EvalReassignmentOfExistingGlobalIsVisible()
        => Assert.Equal("4,4", Eval("""
            var x = 17;
            var actX = eval("var x = 4; function actX(){ return x; } actX;");
            actX() + ',' + x;
        """));

    // A never-declared free reference still throws (the fix must not make undeclared reads silent).
    [Fact]
    public void NeverDeclaredEvalReferenceThrows()
        => Assert.Equal("ReferenceError", Eval("""
            var f = eval("(function(){ return neverDeclaredName; })");
            try { f(); "no-throw"; } catch (e) { e.name; }
        """));
}
