using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// How many jobs an `await using` block takes to resume its function after the disposal, counted
// against a chain of promise reactions queued right after the call. DisposeResources (§ Explicit
// Resource Management) does one Await per resource whose hint is async-dispose, so the function
// resumes one job after a disposer returned undefined or a settled promise, and two jobs after a
// thenable (NewPromiseResolveThenableJob, then the reaction). The @@dispose fallback of an `await
// using` resource discards what the method returned and awaits undefined. Expected strings are
// Node 24's output for the same scripts.
//
// Moving the disposal onto promise jobs (JobQueueSettlementTests) first made it take a reaction
// for the disposer's result, then settle a wrapper promise that the function awaited in turn:
// three jobs where the spec takes one. Before that, the thread-pool version took two.
public class AwaitUsingJobOrderTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Order(string block)
    {
        Load();
        using var ctx = new JSContext();
        ctx.Eval("var log = [];");
        ctx.Eval($$"""
            (async function () {
                try {
                    {{block}}
                } catch (e) {
                    log.push('caught');
                }
                log.push('after');
            })();
            Promise.resolve()
                .then(function () { log.push('t1'); })
                .then(function () { log.push('t2'); })
                .then(function () { log.push('t3'); })
                .then(function () { log.push('t4'); });
            """);
        return ctx.Eval("log.join(',')").ToString();
    }

    private static string Resource(string symbol, string result)
        => $"{{ await using x = {{ [Symbol.{symbol}]() {{ log.push('disp'); return {result}; }} }}; }}";

    [Theory]
    [InlineData("asyncDispose", "undefined", "disp,after,t1,t2,t3,t4")]
    [InlineData("asyncDispose", "Promise.resolve(1)", "disp,after,t1,t2,t3,t4")]
    [InlineData("asyncDispose", "Promise.reject(1)", "disp,caught,after,t1,t2,t3,t4")]
    [InlineData("asyncDispose", "{ then: function (r) { r(1); } }", "disp,t1,after,t2,t3,t4")]
    [InlineData("asyncDispose", "{ then: function (r) { Promise.resolve().then(function () { r(1); }); } }", "disp,t1,t2,after,t3,t4")]
    [InlineData("dispose", "undefined", "disp,after,t1,t2,t3,t4")]
    [InlineData("dispose", "Promise.resolve(1)", "disp,after,t1,t2,t3,t4")]
    [InlineData("dispose", "{ then: function (r) { r(1); } }", "disp,after,t1,t2,t3,t4")]
    [InlineData("dispose", "{ then: function (r) { Promise.resolve().then(function () { r(1); }); } }", "disp,after,t1,t2,t3,t4")]
    public void OneResourceTakesOneAwait(string symbol, string result, string expected)
        => Assert.Equal(expected, Order(Resource(symbol, result)));

    [Fact(Timeout = 600000)]
    public void EachAwaitedResourceTakesOneJob()
        => Assert.Equal("dispB,dispA,t1,after,t2,t3,t4", Order("""
            {
                await using a = { [Symbol.asyncDispose]() { log.push('dispA'); } };
                await using b = { [Symbol.asyncDispose]() { log.push('dispB'); return Promise.resolve(); } };
            }
            """));

    [Fact(Timeout = 600000)]
    public void FallbackDisposeResultIsNotAwaited()
    {
        // A rejected promise returned by a @@dispose fallback is discarded, not thrown.
        Assert.Equal("disp,after,t1,t2,t3,t4", Order(Resource("dispose", "Promise.reject(2)")));
    }
}
