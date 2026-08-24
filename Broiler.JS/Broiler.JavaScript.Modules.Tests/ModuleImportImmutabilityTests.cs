using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Broiler.JavaScript.Modules;
using Broiler.JavaScript.Runtime;
using Xunit;

namespace Broiler.JavaScript.Modules.Tests;

/// <summary>
/// An imported binding is immutable (ES2024 16.2.1.5 creates it as an immutable binding). Assigning
/// to one used to silently overwrite the local snapshot; it now throws, the same runtime read-only
/// TypeError a reassigned <c>const</c> gives in the strict module code.
/// </summary>
/// <remarks>
/// The spec makes assignment to an import an early SyntaxError; this engine cannot raise that phase
/// without whole-module scope analysis across its deferred function bodies, so it matches its own
/// <c>const</c> treatment (a runtime read-only write) rather than leaving the write to succeed. The
/// separate, larger gap — imports are not yet live bindings to the exporter's variable — is recorded
/// under track 3 in the roadmap.
/// </remarks>
public class ModuleImportImmutabilityTests
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

    private static async Task<string> Run(Dictionary<string, string> files, string main)
    {
        using var ctx = new UrlModuleContext(files);
        await ctx.RunScriptAsync(main, "file:///app/main.js", uniqueModuleID: "file:///app/main.js");
        return ctx.Eval("String(globalThis.r)").ToString();
    }

    private static readonly Dictionary<string, string> Dep = new()
    {
        ["file:///app/a.js"] = "export let x = 1; export default 9;",
    };

    [Fact(Timeout = 600000)]
    public async Task AssigningANamedImportThrows()
        => await Assert.ThrowsAnyAsync<Exception>(() => Run(
            new(Dep), "import { x } from './a.js'; x = 2;"));

    [Fact(Timeout = 600000)]
    public async Task UpdatingANamedImportThrows()
        => await Assert.ThrowsAnyAsync<Exception>(() => Run(
            new(Dep), "import { x } from './a.js'; x++;"));

    [Fact(Timeout = 600000)]
    public async Task AssigningADefaultImportThrows()
        => await Assert.ThrowsAnyAsync<Exception>(() => Run(
            new(Dep), "import d from './a.js'; d = 2;"));

    [Fact(Timeout = 600000)]
    public async Task AssigningANamespaceImportThrows()
        => await Assert.ThrowsAnyAsync<Exception>(() => Run(
            new(Dep), "import * as ns from './a.js'; ns = 2;"));

    [Fact(Timeout = 600000)]
    public async Task ARenamedImportIsAlsoImmutable()
        => await Assert.ThrowsAnyAsync<Exception>(() => Run(
            new(Dep), "import { x as y } from './a.js'; y = 2;"));

    // Sealing the binding must not disturb the ordinary import: the seed value still arrives and is
    // readable through every import form.
    [Fact(Timeout = 600000)]
    public async Task OrdinaryImportsStillBindTheirValues()
        => Assert.Equal("11", await Run(new(Dep),
            "import d, { x } from './a.js'; import * as ns from './a.js'; globalThis.r = d + x + ns.x;"));
}
