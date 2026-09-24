using System.Runtime.ExceptionServices;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.LinqExpressions.LinqExpressions.GeneratorsV2;

namespace Broiler.JavaScript.Integration.Tests;

// A return completion in a generator or async body (its own `return`, a return() request from
// for-of `break`, destructuring or IteratorClose, or a finally resuming a pending return) is
// ordinary control flow, and is delivered without throwing. It used to be raised as a
// GeneratorReturnCompletion exception: every body was wrapped in the frame-pop try/finally, so
// each `return` had a finally to run, and running it threw four exceptions per return. That
// made a generator with a `return` about 16x slower and flooded a debugger with first-chance
// exceptions. These tests pin the completion semantics and that no exception is raised for them.
public class GeneratorReturnCompletionTests
{
    // Runs the script (Execute pumps its jobs on this thread) and returns the global `r`, with
    // the number of GeneratorReturnCompletion exceptions raised on this thread meanwhile.
    private static (string Result, int Thrown) Run(string code)
    {
        var thread = Environment.CurrentManagedThreadId;
        var thrown = 0;
        void OnFirstChance(object sender, FirstChanceExceptionEventArgs e)
        {
            if (e.Exception is GeneratorReturnCompletion && Environment.CurrentManagedThreadId == thread)
                thrown++;
        }

        AppDomain.CurrentDomain.FirstChanceException += OnFirstChance;
        try
        {
            using var ctx = new JSContext();
            ctx.Execute("var r;\n" + code);
            return (ctx.Eval("'' + globalThis.r").ToString(), thrown);
        }
        finally
        {
            AppDomain.CurrentDomain.FirstChanceException -= OnFirstChance;
        }
    }

    [Theory(Timeout = 600000)]
    // A generator's own `return`, with and without a value.
    [InlineData("function* g(){ yield 1; return 2; } var it = g(); it.next(); r = JSON.stringify(it.next());",
        "{\"value\":2,\"done\":true}")]
    [InlineData("function* g(){ yield 1; return; } var it = g(); it.next(); r = JSON.stringify([it.next(), it.next()]);",
        "[{\"done\":true},{\"done\":true}]")]
    // The finally runs, and the return value survives it.
    [InlineData("var log = []; function* g(){ try { yield 1; return 2; } finally { log.push('f'); } } var it = g(); it.next(); r = JSON.stringify([it.next(), log]);",
        "[{\"value\":2,\"done\":true},[\"f\"]]")]
    // Nested finallies run inner to outer, one of them yielding on the way.
    [InlineData("var log = []; function* g(){ try { try { return 'v'; } finally { log.push(1); yield 'y'; log.push(2); } } finally { log.push(3); } } var it = g(); r = JSON.stringify([it.next(), it.next(), log]);",
        "[{\"value\":\"y\",\"done\":false},{\"value\":\"v\",\"done\":true},[1,2,3]]")]
    // A finally's own return overrides the pending one.
    [InlineData("function* g(){ try { return 1; } finally { return 2; } } r = JSON.stringify(g().next());",
        "{\"value\":2,\"done\":true}")]
    // return() at a yield, including through a finally that yields.
    [InlineData("function* g(){ yield 1; yield 2; } var it = g(); it.next(); r = JSON.stringify([it.return(9), it.next()]);",
        "[{\"value\":9,\"done\":true},{\"done\":true}]")]
    [InlineData("var log = []; function* g(){ try { yield 1; } finally { yield 'f'; log.push('after'); } } var it = g(); it.next(); r = JSON.stringify([it.return(9), it.next(), log]);",
        "[{\"value\":\"f\",\"done\":false},{\"value\":9,\"done\":true},[\"after\"]]")]
    // IteratorClose from for-of `break` and from destructuring.
    [InlineData("var log = []; function* g(){ try { yield 1; yield 2; } finally { log.push('closed'); } } for (var x of g()) break; var [a] = g(); r = JSON.stringify(log);",
        "[\"closed\",\"closed\"]")]
    // An async function's return, the expression body of an async arrow, and a rejection
    // handled by the body's catch.
    [InlineData("async function f(){ await null; return 3; } f().then(v => r = v);", "3")]
    [InlineData("var f = async () => 'arrow'; f().then(v => r = v);", "arrow")]
    [InlineData("async function f(){ try { await Promise.reject(new Error('boom')); } catch (e) { return 'caught ' + e.message; } } f().then(v => r = v);",
        "caught boom")]
    // A finally that awaits before the pending return completes the promise.
    [InlineData("var log = []; async function f(){ try { return 'v'; } finally { await null; log.push('f'); } } f().then(v => r = v + log);",
        "vf")]
    // Many returns, as in a hot loop.
    [InlineData("function* g(n){ yield n; return n + 1; } var s = 0; for (var i = 0; i < 500; i++) { var it = g(i); it.next(); s += it.next().value; } r = s;",
        "125250")]
    public void ReturnCompletion_IsDeliveredWithoutThrowing(string code, string expected)
    {
        var (result, thrown) = Run(code);
        Assert.Equal(expected, result);
        Assert.Equal(0, thrown);
    }
}
