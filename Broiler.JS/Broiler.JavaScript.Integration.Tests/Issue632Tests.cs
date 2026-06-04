using Broiler.JavaScript.BuiltIns.Promise;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/632
public class Issue632Tests
{
    private static JSValue Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code);
    }

    // Problem 3: AsyncGenerator.prototype.{next,return,throw} called with a receiver
    // that is not an async generator must NOT throw synchronously. They must return a
    // promise that rejects with a TypeError (AsyncGeneratorValidate happens after the
    // promise capability is created).
    [Theory]
    [InlineData("next")]
    [InlineData("return")]
    [InlineData("throw")]
    public void AsyncGeneratorMethodOnBadThisReturnsRejectedPromiseNotSyncThrow(string method)
    {
        var code =
            "async function* g(){}\n" +
            "var AGP = Object.getPrototypeOf(g.prototype);\n" +
            "var m = AGP['" + method + "'];\n" +
            // non-object receivers and a plain object and a sync generator
            "function* sg(){}\n" +
            "var receivers = [undefined, null, 42, 'x', true, {}, sg()];\n" +
            "var threwSync = false; var allPromises = true;\n" +
            "for (var r of receivers) {\n" +
            "  try { var p = m.call(r); if (!(p instanceof Promise)) allPromises = false; }\n" +
            "  catch (e) { threwSync = true; }\n" +
            "}\n" +
            "threwSync + '|' + allPromises;";
        Assert.Equal("false|true", Eval(code).ToString());
    }

    [Theory]
    [InlineData("next")]
    [InlineData("return")]
    [InlineData("throw")]
    public async Task AsyncGeneratorMethodOnBadThisRejectsWithTypeError(string method)
    {
        using var ctx = new JSContext();
        var result = ctx.Eval(
            "async function* g(){}\n" +
            "var AGP = Object.getPrototypeOf(g.prototype);\n" +
            "AGP['" + method + "'].call({}).then(() => 'resolved', e => e.constructor.name);");
        var promise = Assert.IsType<JSPromise>(result);
        var settled = await promise.Task;
        Assert.Equal("TypeError", settled.ToString());
    }

    [Fact]
    public async Task AsyncGeneratorNextStillResolvesForRealAsyncGenerator()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval(
            "async function* g(){ yield 1; }\n" +
            "g().next().then(r => r.value + ':' + r.done);");
        var promise = Assert.IsType<JSPromise>(result);
        var settled = await promise.Task;
        Assert.Equal("1:false", settled.ToString());
    }
}
