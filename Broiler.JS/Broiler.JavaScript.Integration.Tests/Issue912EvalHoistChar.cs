using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Integration.Tests;

// Characterization of block-level function hoisting across direct/eval compilation.
// Used to drive the eval block-function value-hoisting fix (#912) while protecting the
// existing invariants (AnnexB var copy-out, self-reassignment isolation, gen/async no-leak).
public class Issue912EvalHoistChar
{
    private static string Raw(string code)
    {
        using var ctx = new JSContext();
        try { return "" + ctx.Eval(code); }
        catch (System.Exception e) { return e.GetType().Name + (e is JSException je ? ":" + je.Message : ""); }
    }

    // ---- use-before-decl inside an eval block (the #912 fix) ----
    [Fact] public void T1_eval_topblock_before() => Assert.Equal("5", Raw("eval('{ var e = g(); function g(){return 5;} globalThis.__t1 = e; }'); ''+globalThis.__t1;"));
    [Fact] public void T3_eval_topblock_typeof() => Assert.Equal("function", Raw("eval('{ globalThis.__t3 = typeof g; function g(){} }'); ''+globalThis.__t3;"));
    // multi-block "last function wins" + per-block update must still hold
    [Fact] public void T4_eval_two_blocks_last_wins() => Assert.Equal("AB", Raw("(function(){ return eval('{ function f(){ return \"A\"; } } var t1 = f(); { function f(){ return \"B\"; } } var t2 = f(); t1 + t2;'); }())"));

    // ---- INVARIANTS that must stay green ----
    // AnnexB var copy-out + self-reassignment isolation (Issue619 AnnexBEvalFuncBlockScoping)
    [Fact] public void INV_annexB_blockscoping() => Assert.Equal("decl|123|decl", Raw(
        "var initialBV, currentBV, varBinding;"
        + "(function() { eval('{ function f() { initialBV = f; f = 123; currentBV = f; return \"decl\"; } }varBinding = f; f();'); }());"
        + "initialBV() + '|' + currentBV + '|' + varBinding();"));
    // block fn called AFTER block via AnnexB var hoist (Issue619)
    [Fact] public void INV_annexB_after_block() => Assert.Equal("1", Raw("var ran=0; eval('{ function f(){ ran++; } } f();'); ''+ran;"));
    // generator block fn must NOT leak to eval var env
    [Fact] public void INV_generator_noleak() => Assert.Equal("undefined", Raw("eval('{ function* g(){} }'); typeof g;"));
    // async block fn must NOT leak
    [Fact] public void INV_async_noleak() => Assert.Equal("undefined", Raw("eval('{ async function g(){} }'); typeof g;"));
    // direct-compile cases must stay green
    [Fact] public void INV_direct_before() => Assert.Equal("3", Raw("(function(){ let g; { var e = g(); function g(){return 3;} return e; } })()"));
    [Fact] public void INV_direct_inblock() => Assert.Equal("2", Raw("(function(){ { var e = g(); function g(){return 2;} return e; } })()"));
    // strict eval: block fn does not leak; top-level fn stays in eval scope
    [Fact] public void INV_strict_eval_noleak() => Assert.Equal("undefined", Raw("'use strict'; eval('{ function f(){} }'); typeof f;"));
}
