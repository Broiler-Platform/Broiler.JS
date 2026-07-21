using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Broiler.JavaScript.Modules;
using Broiler.JavaScript.Runtime;
using Xunit;

namespace Broiler.JavaScript.Modules.Tests;

/// <summary>
/// Drives the engine's own ES-module machinery over an in-memory URL store (no filesystem, no Clr
/// assembly) and asserts that a static import actually <em>binds its value</em> — not just that the
/// dependency ran. This exercises the module-orchestration completion path: the whole init runs under one
/// pumped <c>AsyncPump</c> loop, <c>InitAsync</c> awaits compilation directly (no Task→promise→Task
/// double-marshal that stranded the body at its first top-level-await), and <c>import()</c> converts its
/// module task to a promise through the engine-native factory rather than the Clr-only interop (whose
/// fallback returned <c>undefined</c>). Before this, an imported binding resolved to <c>undefined</c>/0.
/// </summary>
public class EngineModuleImportBindingTests
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

    private static async Task<double> RunMain(Dictionary<string, string> files, string main)
    {
        using var ctx = new UrlModuleContext(files);
        await ctx.RunScriptAsync(main, "file:///app/main.js", uniqueModuleID: "file:///app/main.js");
        return ctx.Eval("globalThis.result|0").DoubleValue;
    }

    [Fact]
    public async Task Named_Import_Binds_Value()
    {
        var files = new Dictionary<string, string>
        {
            ["file:///app/lib/dep.js"] = "export const d = 7;\nexport function add(a,b){ return a+b; }",
        };
        Assert.Equal(12.0, await RunMain(files,
            "import { d, add } from './lib/dep.js';\nglobalThis.result = add(d, 5);"));
    }

    [Fact]
    public async Task Namespace_Import_Binds_Value()
    {
        var files = new Dictionary<string, string> { ["file:///app/lib/dep.js"] = "export const d = 7;" };
        Assert.Equal(7.0, await RunMain(files,
            "import * as ns from './lib/dep.js';\nglobalThis.result = ns.d;"));
    }

    [Fact]
    public async Task Default_Import_Binds_Value()
    {
        var files = new Dictionary<string, string> { ["file:///app/lib/dep.js"] = "export default 41;" };
        Assert.Equal(42.0, await RunMain(files,
            "import v from './lib/dep.js';\nglobalThis.result = v + 1;"));
    }

    [Fact]
    public async Task Transitive_Chain_Binds()
    {
        var files = new Dictionary<string, string>
        {
            ["file:///app/a.js"] = "export const a = 1;",
            ["file:///app/b.js"] = "import { a } from './a.js';\nexport const b = a + 10;",
        };
        Assert.Equal(11.0, await RunMain(files,
            "import { b } from './b.js';\nglobalThis.result = b;"));
    }

    [Fact]
    public async Task Diamond_Shared_Dependency_Evaluated_Once()
    {
        var files = new Dictionary<string, string>
        {
            ["file:///app/shared.js"] = "globalThis.evalCount=(globalThis.evalCount||0)+1;\nexport const s = 5;",
            ["file:///app/x.js"] = "import { s } from './shared.js';\nexport const x = s + 1;",
            ["file:///app/y.js"] = "import { s } from './shared.js';\nexport const y = s + 2;",
        };
        using var ctx = new UrlModuleContext(files);
        await ctx.RunScriptAsync(
            "import { x } from './x.js';\nimport { y } from './y.js';\nglobalThis.result = x + y;",
            "file:///app/main.js", uniqueModuleID: "file:///app/main.js");
        Assert.Equal(13.0, ctx.Eval("globalThis.result|0").DoubleValue);
        Assert.Equal(1.0, ctx.Eval("globalThis.evalCount|0").DoubleValue);
    }

    [Fact]
    public async Task TopLevelAwait_In_Dependency_Completes()
    {
        var files = new Dictionary<string, string>
        {
            ["file:///app/dep.js"] = "export const v = await Promise.resolve(42);",
        };
        Assert.Equal(43.0, await RunMain(files,
            "import { v } from './dep.js';\nglobalThis.result = v + 1;"));
    }
}
