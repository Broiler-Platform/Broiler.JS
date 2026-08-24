using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Modules;
using Broiler.JavaScript.Runtime;
using Xunit;

namespace Broiler.JavaScript.Modules.Tests;

/// <summary>
/// <c>await</c> is a reserved word in module code, so it may not be a BindingIdentifier there.
/// </summary>
/// <remarks>
/// Every ModuleItem is parsed <c>[~Await]</c>, which reserves the name from a module's first
/// token — before any import or export has been seen. That is why this one could not be inferred
/// from the AST the way the duplicate-name rules were, and needed a real parse goal: the goal now
/// travels in <c>JSCompilationOptions.IsModule</c>, so it is part of the code-cache key and a
/// module cannot share a compile with a script of identical text.
/// <para>
/// The rule is about BINDINGS only. <c>await</c> remains legal as a property name, a method name
/// and an operator, and script code is untouched — the parser applies the rule only under the
/// module goal, so nothing outside a module compile changes.
/// </para>
/// </remarks>
public class ModuleAwaitReservedTests
{
    private sealed class UrlModuleContext(Dictionary<string, string> files) : JSModuleContext
    {
        protected override string Resolve(string dirPath, string relativePath)
        {
            if (Uri.TryCreate(relativePath, UriKind.Absolute, out var abs)) return abs.AbsoluteUri;
            return Uri.TryCreate(new Uri(dirPath), relativePath, out var rel) ? rel.AbsoluteUri : null;
        }

        protected override string GetModuleDirectory(string fullPath) => fullPath;

        protected override Task<string> ReadModuleSourceAsync(JSModule module) =>
            Task.FromResult(files.TryGetValue(module.filePath, out var src)
                ? src
                : throw new FileNotFoundException(module.filePath));
    }

    private static async Task<string> RunModule(string main)
    {
        var files = new Dictionary<string, string> { ["file:///app/dep.js"] = "export const a = 1;" };
        using var ctx = new UrlModuleContext(files);
        await ctx.RunScriptAsync(main, "file:///app/main.js", uniqueModuleID: "file:///app/main.js");
        return ctx.Eval("String(globalThis.r)").ToString();
    }

    // ---- Reserved as a binding, in every binding position ----

    [Theory(Timeout = 600000)]
    [InlineData("var await = 1;")]
    [InlineData("let await = 1;")]
    [InlineData("const await = 1;")]
    [InlineData("function await() {}")]
    [InlineData("function f(await) {}")]
    [InlineData("class await {}")]
    [InlineData("try {} catch (await) {}")]
    [InlineData("const { await } = { await: 1 };")]
    [InlineData("const [await] = [1];")]
    [InlineData("await: 1;")]                                  // a LabelIdentifier is one too
    [InlineData("import { a as await } from './dep.js';")]
    [InlineData("import await from './dep.js';")]
    [InlineData("import * as await from './dep.js';")]
    public async Task AwaitCannotBeABindingInModuleCode(string main)
        => await Assert.ThrowsAnyAsync<Exception>(() => RunModule(main));

    // ---- Still legal in module code: not a binding ----

    [Fact(Timeout = 600000)]
    public async Task TopLevelAwaitStillWorks()
        => Assert.Equal("1", await RunModule("const x = await Promise.resolve(1); globalThis.r = x;"));

    [Fact(Timeout = 600000)]
    public async Task AwaitInsideAnAsyncFunctionStillWorks()
        => Assert.Equal("1", await RunModule(
            "async function f() { return await Promise.resolve(1); } f().then(v => { globalThis.r = v; });"));

    [Fact(Timeout = 600000)]
    public async Task AwaitIsStillLegalAsAPropertyName()
        => Assert.Equal("1", await RunModule("const o = { await: 1 }; globalThis.r = o.await;"));

    [Fact(Timeout = 600000)]
    public async Task AwaitIsStillLegalAsAMethodName()
        => Assert.Equal("1", await RunModule("class K { await() { return 1; } } globalThis.r = new K().await();"));

    [Fact(Timeout = 600000)]
    public async Task AnOrdinaryImportIsUnaffected()
        => Assert.Equal("1", await RunModule("import { a } from './dep.js'; globalThis.r = a;"));

    // ---- SCRIPT code is untouched: the rule applies only under the module goal ----

    [Theory(Timeout = 600000)]
    [InlineData("var await = 1; await")]
    [InlineData("(function () { var await = 2; return await; })()")]
    [InlineData("function await() { return 3; } await()")]
    [InlineData("(function (await) { return await; })(4)")]
    [InlineData("try { throw 5; } catch (await) { await }")]
    public void AwaitIsStillAnOrdinaryNameInScriptCode(string script)
    {
        using var ctx = new JSContext(options: new JSContextOptions { ScriptHostMode = true });
        Assert.False(ctx.Eval(script).IsUndefined);
    }
}
