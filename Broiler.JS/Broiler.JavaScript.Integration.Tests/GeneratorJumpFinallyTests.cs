using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// A break, continue or labelled break that leaves a try statement runs its `finally` first, and
// leaves the statement behind. In a generator or async body a try statement holding a yield or
// await is lowered to a try region, and a jump out of it skipped the finally and left the region
// on the stack. The finally then ran late (when the function returned), or never. A catch that
// was left behind could also take an exception thrown after the loop. Loops that jumped out
// on every iteration piled up regions until unwinding them overflowed the stack and ended the
// process. Expected values match V8.
public class GeneratorJumpFinallyTests
{
    // Runs the script (Execute pumps its jobs) and returns the global `r`.
    private static string Run(string code)
    {
        using var ctx = new JSContext();
        ctx.Execute("var r;\n" + code);
        return ctx.Eval("'' + globalThis.r").ToString();
    }

    [Theory(Timeout = 600000)]
    // break and continue out of a try run its finally before going on.
    [InlineData("var log = []; function* g(){ for (;;) { try { yield 1; break; } finally { log.push('fin'); } } log.push('after'); } var it = g(); it.next(); it.next(); r = JSON.stringify(log);",
        "[\"fin\",\"after\"]")]
    [InlineData("var log = []; function* g(){ for (let i = 0; i < 3; i++) { try { yield i; continue; } finally { log.push('fin' + i); } } } r = JSON.stringify([...g(), log]);",
        "[0,1,2,[\"fin0\",\"fin1\",\"fin2\"]]")]
    [InlineData("var log = []; function* g(){ L: { try { yield 1; break L; } finally { log.push('fin'); } } log.push('after'); } var it = g(); it.next(); it.next(); r = JSON.stringify(log);",
        "[\"fin\",\"after\"]")]
    // A labelled break unwinds through every finally, innermost first, closing a for-of too.
    [InlineData("var log = []; function* g(){ outer: for (;;) { try { for (;;) { try { yield 1; break outer; } finally { log.push('inner'); } } } finally { log.push('outer'); } } log.push('after'); } var it = g(); it.next(); it.next(); r = JSON.stringify(log);",
        "[\"inner\",\"outer\",\"after\"]")]
    [InlineData("var log = []; function* src(){ try { yield 1; yield 2; } finally { log.push('closed'); } } function* g(){ outer: for (;;) { try { for (var x of src()) { yield x; break outer; } } finally { log.push('fin'); } } log.push('after'); } var it = g(); it.next(); it.next(); r = JSON.stringify(log);",
        "[\"closed\",\"fin\",\"after\"]")]
    [InlineData("var log = []; function* g(){ outer: for (let i = 0; i < 2; i++) { try { for (let j = 0; j < 2; j++) { try { yield i + '' + j; continue outer; } finally { log.push('in' + i + j); } } } finally { log.push('out' + i); } } } r = JSON.stringify([...g(), log]);",
        "[\"00\",\"10\",[\"in00\",\"out0\",\"in10\",\"out1\"]]")]
    // A break out of the catch block runs the finally too.
    [InlineData("var log = []; function* g(){ for (;;) { try { yield 1; throw 'x'; } catch (e) { log.push('c'); break; } finally { log.push('f'); } } log.push('after'); } var it = g(); it.next(); it.next(); r = JSON.stringify(log);",
        "[\"c\",\"f\",\"after\"]")]
    // The finally may yield on the way, or override the jump with its own completion.
    [InlineData("var log = []; function* g(){ for (;;) { try { yield 1; break; } finally { log.push('f1'); yield 'fy'; log.push('f2'); } } log.push('after'); } var it = g(); it.next(); r = JSON.stringify([it.next(), it.next(), log]);",
        "[{\"value\":\"fy\",\"done\":false},{\"done\":true},[\"f1\",\"f2\",\"after\"]]")]
    [InlineData("var log = []; function* g(){ for (;;) { try { yield 1; break; } finally { return 'fr'; } } log.push('no'); } var it = g(); it.next(); r = JSON.stringify([it.next(), log]);",
        "[{\"value\":\"fr\",\"done\":true},[]]")]
    [InlineData("var log = []; function* g(){ a: for (;;) { b: for (;;) { try { yield 1; break b; } finally { log.push('f'); break a; } } log.push('no'); } log.push('after'); } var it = g(); it.next(); it.next(); r = JSON.stringify(log);",
        "[\"f\",\"after\"]")]
    [InlineData("var log = []; function* g(){ for (;;) { try { yield 1; break; } finally { log.push('f1'); yield 'fy'; log.push('no'); } } log.push('no'); } var it = g(); it.next(); it.next(); r = JSON.stringify([it.return('R'), log]);",
        "[{\"value\":\"R\",\"done\":true},[\"f1\"]]")]
    // A try statement that was left no longer catches what is thrown after it.
    [InlineData("var log = []; function* g(){ for (;;) { try { yield 1; break; } catch (e) { log.push('wrong'); } } throw 'outside'; } var it = g(); it.next(); try { it.next(); } catch (e) { log.push('escaped ' + e); } r = JSON.stringify(log);",
        "[\"escaped outside\"]")]
    [InlineData("var log = []; function* g(){ for (let i = 0; i < 1000; i++) { try { if (i % 2) yield i; continue; } catch (e) { log.push('wrong'); } } throw 'outside'; } var n = 0; try { for (var x of g()) n++; } catch (e) { log.push('escaped ' + e); } r = JSON.stringify([n, log]);",
        "[500,[\"escaped outside\"]]")]
    // Async functions.
    [InlineData("var log = []; async function f(){ for (;;) { try { await null; break; } finally { log.push('f1'); await null; log.push('f2'); } } log.push('after'); return 'done'; } f().then(v => r = JSON.stringify([v, log]));",
        "[\"done\",[\"f1\",\"f2\",\"after\"]]")]
    [InlineData("async function f(){ var n = 0, m = 0; for (var i = 0; i < 1000; i++) { try { await null; if (i % 3) continue; n++; } finally { m++; } } return n + ':' + m; } f().then(v => r = v);",
        "334:1000")]
    public void JumpOutOfTryRunsFinally(string code, string expected)
        => Assert.Equal(expected, Run(code));
}
