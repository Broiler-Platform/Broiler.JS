using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Broiler.JavaScript.Modules;
using Broiler.JavaScript.Runtime;
using Xunit;

namespace Broiler.JavaScript.Modules.Tests;

/// <summary>
/// A module has its own module environment: its top-level <c>let</c>/<c>const</c>/<c>class</c>
/// bindings are module-scoped, not entries in the realm's shared global lexical environment.
/// </summary>
/// <remarks>
/// They used to be published into the global lexical environment the way a script's top-level
/// lexicals are, so every module shared one realm-wide slot per name. A module that declared a
/// top-level <c>const x</c> and, while still running, triggered a transitive import of another
/// module that also declared a top-level <c>const x</c> then hit the first module's read-only
/// binding and threw "Cannot assign to read only variable". (Sibling imports at one level never
/// collided only because each module body had returned before the next ran, so the shared slot was
/// re-declared, not double-occupied.) The fix keeps a module's top-level lexicals local to its
/// compiled body — for exported and non-exported declarations alike — which is also the
/// spec-correct scoping.
/// </remarks>
public class ModuleScopeIsolationTests
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
        files["file:///app/main.js"] = main;
        using var ctx = new UrlModuleContext(files);
        await ctx.RunScriptAsync(main, "file:///app/main.js", uniqueModuleID: "file:///app/main.js");
        return ctx.Eval("String(globalThis.r)").ToString();
    }

    // The regression: a module imports a second module WHILE its own body is mid-execution, and both
    // declare a top-level exported `const` of the same name. Before the fix the transitive import's
    // `const x` collided with the still-live outer module's read-only `x`.
    [Fact(Timeout = 600000)]
    public async Task ATransitiveImportDoesNotCollideWithTheImportersExportedConst()
        => Assert.Equal("MA", await Run(
            new()
            {
                ["file:///app/a.js"] = "export const x = 'A';",
                ["file:///app/mod.js"] =
                    "export const x = 'M'; import { x as ax } from './a.js'; export const seen = ax;",
            },
            "import { x, seen } from './mod.js'; globalThis.r = x + seen;"));

    // The same, for `let` and a class declaration exported from the transitively imported module.
    [Fact(Timeout = 600000)]
    public async Task ATransitiveImportDoesNotCollideForLetOrClass()
        => Assert.Equal("MA2", await Run(
            new()
            {
                ["file:///app/a.js"] = "export let y = 1; export class C { v() { return 'A'; } }",
                ["file:///app/mod.js"] =
                    "let y = 2; class C { v() { return 'M'; } } import { C as AC } from './a.js';" +
                    " export const r = (new C()).v() + (new AC()).v() + y;",
            },
            "import { r } from './mod.js'; globalThis.r = r;"));

    // Two independent sibling modules each declaring the same top-level const name stay isolated —
    // this always worked, and must keep working after the fix.
    [Fact(Timeout = 600000)]
    public async Task TwoSiblingModulesWithTheSameConstNameStayIsolated()
        => Assert.Equal("AB", await Run(
            new()
            {
                ["file:///app/a.js"] = "export const x = 'A';",
                ["file:///app/b.js"] = "export const x = 'B';",
            },
            "import { x as ax } from './a.js'; import { x as bx } from './b.js'; globalThis.r = ax + bx;"));

    // An explicit local export shadows an `export *` of the same name (ES2024 16.2.1.5.3 ResolveExport
    // consults local/indirect entries before star entries), even when the star follows the local
    // export in source order — which previously threw "Cannot assign to read only variable" as the
    // runtime star-copy tried to overwrite the read-only local binding.
    [Fact(Timeout = 600000)]
    public async Task ALocalExportShadowsAStarReExportOfTheSameName()
        => Assert.Equal("local", await Run(
            new()
            {
                ["file:///app/a.js"] = "export const x = 'star';",
                ["file:///app/mod.js"] = "export const x = 'local'; export * from './a.js';",
            },
            "import { x } from './mod.js'; globalThis.r = x;"));

    // A named re-export likewise shadows an `export *` of the same name, independent of source order.
    [Fact(Timeout = 600000)]
    public async Task ANamedReExportShadowsAStarReExportOfTheSameName()
        => Assert.Equal("named", await Run(
            new()
            {
                ["file:///app/c.js"] = "export const seed = 'named';",
                ["file:///app/a.js"] = "export const x = 'star';",
                ["file:///app/mod.js"] = "export { seed as x } from './c.js'; export * from './a.js';",
            },
            "import { x } from './mod.js'; globalThis.r = x;"));
}
