using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Storage;

namespace Broiler.JavaScript.Modules.Tests;

public class ModulesTests
{
    /// <summary>
    /// A host that loads modules by URL from an in-memory map instead of the filesystem, driving the
    /// <see cref="JSModuleContext"/> resolution/read seams (Resolve / GetModuleDirectory /
    /// ReadModuleSourceAsync). This is the shape a browser bridge uses to resolve URLs against a base and
    /// fetch them under a content-security policy while reusing the engine's real module compilation.
    /// </summary>
    private sealed class UrlModuleContext(Dictionary<string, string> files) : JSModuleContext
    {
        protected override string Resolve(string dirPath, string relativePath)
        {
            if (Uri.TryCreate(relativePath, UriKind.Absolute, out var abs))
                return abs.AbsoluteUri;
            return Uri.TryCreate(new Uri(dirPath), relativePath, out var rel) ? rel.AbsoluteUri : null;
        }

        // A module's own relative imports resolve against its full URL (URL relative-reference semantics),
        // not a filesystem directory.
        protected override string GetModuleDirectory(string fullPath) => fullPath;

        protected override Task<string> ReadModuleSourceAsync(JSModule module) =>
            Task.FromResult(files.TryGetValue(module.filePath, out var src)
                ? src
                : throw new FileNotFoundException(module.filePath));
    }

    [Fact]
    public async Task HostUrlSeams_Resolve_Fetch_And_Execute_A_Url_Dependency()
    {
        // A URL entry imports a dependency living under a different sub-path; the specifier './lib/dep.js'
        // must be resolved against the entry's URL base (Resolve, relative→absolute) and the dependency
        // fetched from the in-memory store (ReadModuleSourceAsync) — no filesystem involved. This is the
        // host seam a browser bridge needs to reuse the engine's module machinery over URLs/CSP fetch.
        var files = new Dictionary<string, string>
        {
            ["file:///app/lib/dep.js"] =
                "globalThis.ranDep = (globalThis.ranDep || 0) + 1;\n" +
                "export const d = 7;",
        };
        using var ctx = new UrlModuleContext(files);

        await ctx.RunScriptAsync(
            "globalThis.ranMain = 1;\n" +
            "import { d } from './lib/dep.js';",
            "file:///app/main.js", uniqueModuleID: "file:///app/main.js");

        // The entry ran, and its URL dependency was resolved + fetched through the seams and executed
        // exactly once. (The static import *value* binding and nested/transitive async module ordering are
        // separate, pre-existing engine concerns — see the module-graph linker in the HtmlBridge; here we
        // validate only the host resolution/fetch/execution path these seams add.)
        Assert.Equal(1.0, ctx.Eval("globalThis.ranMain|0").DoubleValue);
        Assert.Equal(1.0, ctx.Eval("globalThis.ranDep|0").DoubleValue);
    }

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
