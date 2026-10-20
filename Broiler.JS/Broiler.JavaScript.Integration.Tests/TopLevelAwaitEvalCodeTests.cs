using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Correction J1 of milestone SJL-12.5 (Broiler.VM's JSD-0080, decision D21; the changelog): code compiled
// while EvalWithTopLevelAwaitAsync runs, but not as its own program, is compiled as the specification's goal
// without top-level await, as under Eval. Before, the permission was an ambient scope around the program and
// everything it ran, so direct and indirect eval, ShadowRealm.prototype.evaluate and the Function constructor
// compiled with it too: the survey's probes at 24510b0e saw EvalWithTopLevelAwaitAsync("eval('await 1')")
// return 1, where Eval threw a SyntaxError. Each case here answers under EvalWithTopLevelAwaitAsync as it
// answers under Eval.
public class TopLevelAwaitEvalCodeTests
{
    // What the code does, caught inside the program so that the answer is a string in both modes.
    private static string Probe(string code)
        => "var outcome; try { outcome = 'returned ' + String(" + code + "); } catch (e) { outcome = 'threw ' + e.name; } outcome";

    private static string Eval(string program)
    {
        using var context = new JSContext();
        return context.Eval(program).ToString();
    }

    private static async Task<string> EvalWithTopLevelAwait(string program)
    {
        using var context = new JSContext();
        return (await context.EvalWithTopLevelAwaitAsync(program)).ToString();
    }

    [Theory(Timeout = 600000)]
    [InlineData("eval('await 1')")]
    [InlineData("(0, eval)('await 1')")]
    [InlineData("eval('for await (const v of []) {}')")]
    [InlineData("eval(\"eval('await 1')\")")]
    [InlineData("new ShadowRealm().evaluate('await 1')")]
    [InlineData("Function('}); await 1; ({')")]
    public async Task Code_a_program_with_top_level_await_compiles_may_not_await_at_its_own_top_level(string code)
    {
        var program = Probe(code);
        Assert.Equal("threw SyntaxError", Eval(program));
        Assert.Equal("threw SyntaxError", await EvalWithTopLevelAwait(program));
    }

    // After the program's own await, in the continuation the permission's scope used to reach.
    [Fact(Timeout = 600000)]
    public async Task Eval_after_the_programs_own_await_may_not_await_either()
        => Assert.Equal("threw SyntaxError", await EvalWithTopLevelAwait("await 0; " + Probe("eval('await 1')")));

    // The program itself keeps its permission, and eval code that does not await is unchanged.
    [Fact(Timeout = 600000)]
    public async Task The_program_keeps_its_own_permission()
    {
        Assert.Equal("3", await EvalWithTopLevelAwait("var a = await Promise.resolve(1); a + eval('2')"));
        Assert.Equal("returned 3", await EvalWithTopLevelAwait(Probe("eval('1 + 2')")));
        Assert.Equal("returned 3", await EvalWithTopLevelAwait(Probe("new ShadowRealm().evaluate('1 + 2')")));
        Assert.Equal("returned 3", await EvalWithTopLevelAwait(Probe("Function('return 1 + 2')()")));
    }
}
