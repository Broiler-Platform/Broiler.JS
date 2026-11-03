using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// JSContext.Execute returns after every job the script queued on the pump has run, including jobs of
// a promise chain the script does not return. The pump used to stop when a thread-pool continuation
// saw the script's completion value settle and refuse the jobs queued after that, so how much of an
// unreturned chain ran was decided by a race (AsyncPumpTests hold the pump-level cases).
public sealed class ExecuteDrainsJobQueueTests
{
    private static string Drive(string body)
    {
        using var ctx = new JSContext();
        ctx.Eval("globalThis.r = '<unset>';");
        ctx.Execute(body);
        return ctx.Eval("'' + globalThis.r").ToString();
    }

    [Fact]
    public void An_unreturned_promise_chain_runs_to_the_end()
        => Assert.Equal("1,2", Drive(
            "Promise.resolve(1).then(a => {"
            + " Promise.resolve(2).then(x => x).then(x => x).then(x => x).then(b => { globalThis.r = a + ',' + b; });"
            + " });"));

    [Fact]
    public void Unreturned_async_generator_steps_run_to_the_end()
        => Assert.Equal("i1,i2,i3|done=true", Drive(
            "async function* inner(){ yield 'i1'; yield 'i2'; yield 'i3'; }"
            + " async function* g(){ yield* inner(); }"
            + " var it = g();"
            + " it.next().then(a => { it.next().then(b => { it.next().then(c => { it.next().then(d => {"
            + "   globalThis.r = [a.value, b.value, c.value].join(',') + '|done=' + d.done; }); }); }); });"));

    [Fact]
    public void An_unreturned_async_function_runs_to_the_end()
        => Assert.Equal("3", Drive(
            "async function f(){ var n = 0; for (var i = 0; i < 3; i++) { await null; n++; } globalThis.r = '' + n; }"
            + " Promise.resolve().then(() => { f(); });"));

    [Fact]
    public void Execute_returns_when_nothing_is_queued()
        => Assert.Equal("done", Drive("globalThis.r = 'done';"));
}
