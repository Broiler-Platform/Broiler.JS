using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Correction J2 of milestone SJL-12.5 (Broiler.VM's JSD-0080, decision D21; the changelog): the code cache
// keys a compilation on its top-level-await permission, so one context no longer answers a compilation from
// the other mode's entry. In a script `await (21)` calls the function named `await`, and with the
// permission it awaits 21; the same text answered 42 or 21 (or a Promise) by which mode compiled it first in
// the context. The witnessed probe was taken at 24510b0e (the SJL-12 survey's critic). Each test has a text
// of its own, since the default code cache is the process's.
public class TopLevelAwaitCodeCacheTests
{
    private static string Source(string test) => "/* " + test + " */ globalThis.await = function (x) { return x * 2; }; await (21)";

    private static string Eval(JSContext context, string source) => context.Eval(source).ToString();

    private static async Task<string> EvalWithTopLevelAwait(JSContext context, string source)
        => (await context.EvalWithTopLevelAwaitAsync(source)).ToString();

    [Fact]
    public async Task A_script_compiled_after_the_same_text_with_top_level_await_calls_await()
    {
        var source = Source(nameof(A_script_compiled_after_the_same_text_with_top_level_await_calls_await));
        using var context = new JSContext();
        Assert.Equal("21", await EvalWithTopLevelAwait(context, source));
        Assert.Equal("42", Eval(context, source));
    }

    [Fact]
    public async Task A_program_with_top_level_await_compiled_after_the_same_script_awaits()
    {
        var source = Source(nameof(A_program_with_top_level_await_compiled_after_the_same_script_awaits));
        using var context = new JSContext();
        Assert.Equal("42", Eval(context, source));
        Assert.Equal("21", await EvalWithTopLevelAwait(context, source));
    }

    [Fact]
    public async Task Each_mode_answers_alone_as_it_answers_after_the_other()
    {
        var source = Source(nameof(Each_mode_answers_alone_as_it_answers_after_the_other));
        using (var script = new JSContext())
            Assert.Equal("42", Eval(script, source));
        using (var program = new JSContext())
            Assert.Equal("21", await EvalWithTopLevelAwait(program, source));
        using (var script = new JSContext())
            Assert.Equal("42", Eval(script, source));
    }
}
