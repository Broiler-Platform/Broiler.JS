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

    // Dynamic import() — issue #771, Problems 12 & 28. In a module context the ImportCall is
    // wired to the host loader and evaluates to a Promise that settles to the module namespace
    // (or rejects for a missing module); that end-to-end async flow is exercised by the
    // file-based module runner. Here we lock in that it parses and compiles everywhere an
    // expression may appear.
    [Fact]
    public void DynamicImport_InsideUncalledFunction_CompilesWithoutExecuting()
    {
        // The syntax/valid test262 cases define (but never call) functions containing nested
        // import() — they only need to compile. A primitive Eval context has no module loader,
        // so this would throw if the import were executed; that it does not proves it is inert.
        using var ctx = new JSModuleContext();

        var result = ctx.Eval("typeof (() => import(import(import('./mod.js'))))");

        Assert.Equal("function", result.ToString());
    }
}
