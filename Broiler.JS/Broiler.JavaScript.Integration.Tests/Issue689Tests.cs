using System;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/689
//
// Fixed here (general engine bug surfaced via Problem 9 — optional-chaining
// iteration-statement-for, and many other loop tests):
//
//   A `break`/`continue`/`return` statement nested inside a try/finally region
//   (which every `for`-loop body statement is wrapped in for completion tracking)
//   is compiled as a DEFERRED finally-jump: it stores a state value into a temp
//   int local, `leave`s to the finally, and after the finally a dispatch checks
//   `if (state == N) goto <target>`. That state local is a REUSED temp, and the
//   CLR only zero-inits locals once at method entry — so when one loop took its
//   break (leaving the temp == 1) the NEXT `for`-loop in the same method reused
//   the same local without resetting it. On the next loop's first iteration the
//   stale `state == 1` made the post-finally dispatch fire the break target
//   immediately: the body ran exactly once, the update expression never ran, and
//   the loop exited early.
//
//   ILTryBlock.MarkHasFinally now zero-initialises the finally-jump state local
//   at the top of each try body, so the dispatch only branches when THIS block
//   actually requested a deferred jump.
//
// Out of scope (architectural / CLDR / deep parser, matching prior triage in
// #683 / #685 / #687): the private-* brand-check families, super-*-reference-null,
// AnnexB eval binding re-init / skip-early-err, scope-param-elem-var,
// derived-class-return-override, computed-property-abrupt-completion, NumberFormat
// signDisplay "negative" currency CLDR formatting, and the staging/sm negative
// SyntaxError grab-bag.
public class Issue689Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    private static string EvalScriptHost(string code)
    {
        using var ctx = new JSContext(options: new JSContextOptions { ScriptHostMode = true });
        return ctx.Eval(code).ToString();
    }

    // ---- A breaking for-loop must not corrupt a subsequent for-loop ----

    // Each loop counts to one via its update expression, then breaks. Before the
    // fix only the first loop behaved; later loops saw the stale break-state and
    // exited after one body iteration (count stayed 0, update never ran).
    [Fact]
    public void ConsecutiveBreakingForLoopsEachReachCountOne()
        => Assert.Equal("1,1,1,1", Eval(
            "var r = [];"
            + "for (var n = 0; n < 4; n++) {"
            + "  var c = 0;"
            + "  for (c = 0; true; c++) { if (c > 0) break; }"
            + "  r.push(c);"
            + "}"
            + "r.join(',')"));

    [Fact]
    public void ConsecutiveBreakingForLoopsEachReachCountOne_ScriptHost()
        => Assert.Equal("1,1,1,1", EvalScriptHost(
            "var r = [];"
            + "for (var n = 0; n < 4; n++) {"
            + "  var c = 0;"
            + "  for (c = 0; true; c++) { if (c > 0) break; }"
            + "  r.push(c);"
            + "}"
            + "r.join(',')"));

    // The exact shape from optional-chaining/iteration-statement-for.js: the
    // update expression has the side effect that drives the break condition.
    [Fact]
    public void ForUpdateSideEffectRunsAfterAnEarlierLoopBroke()
        => Assert.Equal("1|1", EvalScriptHost(
            "var first = 0;"
            + "for (first = 0; true; first++) { if (first > 0) break; }"
            + "var count = 0;"
            + "var obj = { get a() { count++; return undefined; } };"
            + "for (count = 0; true; obj.a) { if (count > 0) break; }"
            + "first + '|' + count"));

    // ---- Other loop control flow stays correct after the fix ----

    [Fact]
    public void ContinueAfterABreakingLoopStillSkips()
        => Assert.Equal("4", Eval(
            "for (var i = 0; i < 3; i++) { if (i == 1) break; }"
            + "var b = 0;"
            + "for (var j = 0; j < 5; j++) { if (j == 2) continue; b++; }"
            + "String(b)"));

    [Fact]
    public void LabeledBreakAcrossNestedForLoops()
        => Assert.Equal("4", Eval(
            "var s = 0;"
            + "outer: for (var i = 0; i < 3; i++) {"
            + "  for (var j = 0; j < 3; j++) { if (i == 1 && j == 1) break outer; s++; }"
            + "}"
            + "String(s)"));

    [Fact]
    public void ReturnInsideForLoopAfterAnotherLoopBroke()
        => Assert.Equal("3", Eval(
            "for (var i = 0; i < 3; i++) { if (i == 1) break; }"
            + "function f(){ for (var k = 0; k < 10; k++) { if (k == 3) return k; } return -1; }"
            + "String(f())"));

    [Fact]
    public void SwitchBreakInsideLoopThenForLoopReachesCountOne()
        => Assert.Equal("3|1", Eval(
            "var s = 0;"
            + "for (var i = 0; i < 4; i++) { switch (i) { case 2: break; default: s++; } }"
            + "var c = 0;"
            + "for (c = 0; true; c++) { if (c > 0) break; }"
            + "s + '|' + c"));
}
