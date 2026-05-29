using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Storage;

namespace Broiler.JavaScript.Modules.Tests;

public class ModulesTests
{
    [Fact]
    public void JSModuleContext_Create_Succeeds()
    {
        var ctx = new JSModuleContext();
        Assert.NotNull(ctx);
    }

    [Fact]
    public void JSModuleContext_RegisterModule_Succeeds()
    {
        var ctx = new JSModuleContext();
        var exports = new JSObject();
        ctx.RegisterModule(KeyStrings.GetOrCreate(new StringSpan("testmod")), exports);
    }

    [Fact]
    public void JSModule_Create_WithExports()
    {
        var ctx = new JSModuleContext();
        var exports = new JSObject();
        var module = new JSModule(ctx, exports, "mymod");
        Assert.NotNull(module);
    }

    [Fact]
    public void JSAssertThrows_InvokesCallbackWithoutPassingAssertionArguments()
    {
        using var ctx = new JSModuleContext();

        var result = ctx.Eval("""
            var argc = -1;
            assert.throws(function () {
                argc = arguments.length;
                throw 'boom';
            }, undefined);
            argc;
            """);

        Assert.Equal(0.0, result.DoubleValue);
    }

    [Fact]
    public async Task JSModuleContext_RunScriptAsync_AllowsTopLevelAwait()
    {
        using var ctx = new JSModuleContext();

        var result = await ctx.RunScriptAsync("""
            await Promise.resolve('ready');
            """, Environment.CurrentDirectory);

        Assert.NotNull(result);
    }
}
