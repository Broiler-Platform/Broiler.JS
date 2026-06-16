using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/814 — Problem 14
// (test/language/statements/let/syntax/let-closure-inside-initialization.js and
// test/staging/sm/lexical-environment/bug-1216623.js).
//
// A closure created inside a C-style `for (let …; …; …)` head's *initializer* must
// capture the loop variable. The lowering previously evaluated the initializer into a
// synthetic carrier that did not bind the original name, so `() => i` resolved against
// nothing and threw "i is not defined". The loop variable is now also bound in the loop's
// own lexical scope (the head's init declaration), which the per-iteration body copies
// shadow — so body closures keep their per-iteration values while an initializer closure
// captures the (never-reassigned) loop-environment binding.
public class Issue814Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // ---- closures created in the initializer (the bug) ----

    [Fact]
    public void ClosureInInitializerCapturesLoopVariable()
        => Assert.Equal("0", Eval(
            "var g; for (let i = (g = () => i, 0); i < 3; i++) {} '' + g()"));

    [Fact]
    public void ClosureInInitializerSeesInitialValueNotFinalValue()
        // The loop-environment binding holds the initializer's value (0) and is never
        // reassigned by the per-iteration copies, so it is not the post-loop value (3).
        => Assert.Equal("true", Eval(
            "var g; for (let i = (g = () => i, 0); i < 3; i++) {} '' + (g() === 0)"));

    [Fact]
    public void ClosureInInitializerWithMultipleBindings()
        => Assert.Equal("11", Eval(
            "var g; for (let a = 1, b = (g = () => a + b, 10); a < 3; a++) {} '' + g()"));

    [Fact]
    public void InitializerSideEffectRunsExactlyOnce()
        => Assert.Equal("1", Eval(
            "var n = 0; for (let i = (n++, 0); i < 3; i++) {} '' + n"));

    // ---- the per-iteration body binding still works (must not regress) ----

    [Fact]
    public void BodyClosuresCapturePerIterationBinding()
        => Assert.Equal("0,1,2", Eval(
            "var f = []; for (let i = 0; i < 3; i++) { f.push(() => i); } f.map(g => g()).join(',')"));

    [Fact]
    public void InitializerAndBodyClosuresAreIndependent()
        => Assert.Equal("0|0,1,2", Eval(
            "var g, f = []; for (let i = (g = () => i, 0); i < 3; i++) { f.push(() => i); } " +
            "'' + g() + '|' + f.map(x => x()).join(',')"));

    // ---- ordinary control flow is unaffected ----

    [Theory]
    [InlineData("var s=0; for (let i=0;i<5;i++){ s+=i; } ''+s", "10")]
    [InlineData("var s=0; for (let i=0;i<5;i++){ if(i===2) continue; s+=i; } ''+s", "8")]
    [InlineData("var s=0; for (let i=0;i<10;i++){ if(i===3) break; s+=i; } ''+s", "3")]
    [InlineData("var f=[]; for(let i=0;i<2;i++) for(let j=0;j<2;j++) f.push(()=>''+i+j); f.map(x=>x()).join(',')", "00,01,10,11")]
    [InlineData("var out=[]; outer: for(let i=0;i<3;i++){ for(let j=0;j<3;j++){ if(j===1) continue outer; out.push(''+i+j);} } out.join(',')", "00,10,20")]
    [InlineData("for(let i=0;i<1;i++){ var z=42; } ''+z", "42")]
    public void ControlFlowUnaffected(string code, string expected)
        => Assert.Equal(expected, Eval(code));
}
