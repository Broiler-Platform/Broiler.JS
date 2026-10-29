using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// The parser names the temporaries of its for (let …) / for (const …) desugaring by number, and since
// milestone SJL-12.5 item 2 it numbers them from 2 in each parse. Code a direct or indirect eval
// compiles is parsed on its own, so its temporaries take the same names as the calling code's. These
// cases are the shapes in which the calling code reads its temporary again after the eval ran: a
// for (const …;;) loop with no test and no update re-reads its `var` carrier at the top of every
// iteration, and a let loop's update runs on its carrier. The results are those of the parser that
// numbered temporaries with one counter for the process, whose names never coincided: an eval's
// temporary is its own binding, never the caller's of the same name.
public class ForLoopTemporaryEvalTests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    [Theory]
    [InlineData("(function(){ var r=[], c=0; for (const a = [1,2]; ;) { r.push(String(a)); eval(\"for (const y of [9]) {}\"); if (++c == 2) break; } return r.join('|') })()", "1,2|1,2")]
    [InlineData("(function(){ var r=[]; for (const a = 'A'; ;) { r.push(a); eval(\"for (const y of ['Y']) {}\"); if (r.length === 2) break; } return r.join() })()", "A,A")]
    [InlineData("(function(){ var r=[]; for (const a = 'A'; ;) { r.push(a); eval(\"for (let k in {K:1}) {}\"); if (r.length === 2) break; } return r.join() })()", "A,A")]
    [InlineData("(function(){ var r=[]; for (const a = 'A'; ;) { r.push(a); eval(\"for (const y = 'Y'; ;) { break; }\"); if (r.length === 2) break; } return r.join() })()", "A,A")]
    [InlineData("(function(){ var r=[]; for (const a = 'A'; ;) { r.push(a); (0,eval)(\"for (const y of ['Y']) {}\"); if (r.length === 2) break; } return r.join() })()", "A,A")]
    [InlineData("(function(){ function* g(){ for (const a = 'A'; ;) { yield a; eval(\"for (const y of ['Y']) {}\"); } } var it = g(); return [it.next().value, it.next().value, it.next().value].join() })()", "A,A,A")]
    [InlineData("(function(){ var r=[]; for (let i = 0; i < 3; i++, eval(\"for (const z of [7]) {}\")) r.push(i); return r.join() })()", "0,1,2")]
    [InlineData("(function(){ var a=[7,8,9,10], r=[]; with (a) { for (const q = 'A'; ;) { r.push(q); eval(\"for (const y of ['Y']) {}\"); if (r.length === 2) break; } } return r.join() + '|' + a.join() })()", "A,A|7,8,9,10")]
    public void A_functions_temporary_survives_an_eval_that_numbers_its_own_alike(string source, string expected)
        => Assert.Equal(expected, Eval(source));

    [Theory]
    [InlineData("var r=[]; for (const a = 'A'; ;) { r.push(a); eval(\"for (const y of ['Y']) {}\"); if (r.length === 2) break; } r.join()", "A,A")]
    [InlineData("var r=[]; for (const a = 'A'; ;) { r.push(a); (0,eval)(\"for (const y of ['Y']) {}\"); if (r.length === 2) break; } r.join()", "A,A")]
    [InlineData("var r=[]; for (let i = 0; i < 3; i++, eval(\"for (const z of [7]) {}\")) r.push(i); r.join()", "0,1,2")]
    public void A_scripts_temporary_survives_an_eval_that_numbers_its_own_alike(string source, string expected)
        => Assert.Equal(expected, Eval(source));

    [Fact]
    public void An_async_functions_temporary_survives_an_eval_after_an_await()
    {
        using var ctx = new JSContext();
        ctx.Eval("var out = 'pending'; (async function(){ var r=[]; for (const a = 'A'; ;) { r.push(a); await 0; eval(\"for (const y of ['Y']) {}\"); if (r.length === 2) break; } out = r.join() })(); 0");
        Assert.Equal("A,A", ctx.Eval("out").ToString());
    }
}
