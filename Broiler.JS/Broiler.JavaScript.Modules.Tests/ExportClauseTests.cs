using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Broiler.JavaScript.Modules;
using Broiler.JavaScript.Runtime;
using Xunit;

namespace Broiler.JavaScript.Modules.Tests;

/// <summary>
/// NamedExports — <c>export { a, b as c }</c>, with and without a <c>from</c> clause (ES2024
/// 16.2.3).
/// </summary>
/// <remarks>
/// The whole clause family used to be rejected, because the parser had no ExportClause production:
/// it read the braces as an object DESTRUCTURING pattern that declared each name as a <c>var</c>,
/// and then required a <c>from</c>. Both halves were wrong, and between them no clause form
/// worked — <c>const x = 1; export { x }</c> failed as "x is already defined in current scope"
/// (the pattern redeclared it) and <c>var x = 1; export { x }</c> failed as "Expecting keyword
/// from". Only <c>export &lt;declaration&gt;</c> and <c>export default</c> ever worked.
/// <para>
/// These tests assert the exported VALUE arrives at an importer, not merely that the source
/// parses: the defect was as much about what the clause compiled to as about the grammar.
/// </para>
/// </remarks>
public class ExportClauseTests
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

    // Runs `main` as a module against a `./dep.js` carrying `dep`, and returns whatever it left in
    // globalThis.r. `./base.js` exists so a dep can re-export from something.
    private static async Task<string> Run(string dep, string main)
    {
        var files = new Dictionary<string, string>
        {
            ["file:///app/dep.js"] = dep,
            ["file:///app/base.js"] = "export const one = 1; export const two = 2;",
            ["file:///app/withdefault.js"] = "export const named = 5; export default 9;",
        };

        using var ctx = new UrlModuleContext(files);
        await ctx.RunScriptAsync(main, "file:///app/main.js", uniqueModuleID: "file:///app/main.js");
        return ctx.Eval("String(globalThis.r)").ToString();
    }

    // ---- A clause exports an existing binding, under its own name or a new one ----

    [Fact(Timeout = 600000)]
    public async Task AClauseExportsAConstBinding()
        => Assert.Equal("42", await Run(
            "const x = 41; export { x };",
            "import { x } from './dep.js'; globalThis.r = x + 1;"));

    [Fact(Timeout = 600000)]
    public async Task AClauseRenamesWithAs()
        => Assert.Equal("5", await Run(
            "const x = 5; export { x as y };",
            "import { y } from './dep.js'; globalThis.r = y;"));

    [Fact(Timeout = 600000)]
    public async Task AClauseExportsAFunctionDeclaration()
        => Assert.Equal("9", await Run(
            "function f() { return 9; } export { f };",
            "import { f } from './dep.js'; globalThis.r = f();"));

    [Fact(Timeout = 600000)]
    public async Task AClauseExportsAClassDeclaration()
        => Assert.Equal("3", await Run(
            "class C { v() { return 3; } } export { C };",
            "import { C } from './dep.js'; globalThis.r = new C().v();"));

    // A `var` is the case that used to fail with "Expecting keyword from" rather than a
    // redeclaration error, because the pattern could redeclare it without conflict.
    [Fact(Timeout = 600000)]
    public async Task AClauseExportsSeveralVarBindings()
        => Assert.Equal("3", await Run(
            "var a = 1, b = 2; export { a, b };",
            "import { a, b } from './dep.js'; globalThis.r = a + b;"));

    // The exported name is a property of the namespace, not a binding, so a keyword is legal
    // there: this is how an existing binding becomes the default export.
    [Fact(Timeout = 600000)]
    public async Task AClauseCanExportAsDefault()
        => Assert.Equal("7", await Run(
            "const x = 7; export { x as default };",
            "import d from './dep.js'; globalThis.r = d;"));

    [Fact(Timeout = 600000)]
    public async Task AnEmptyClauseIsLegalAndPublishesNothing()
        => Assert.Equal("1", await Run(
            "const x = 1; export {}; export { x };",
            "import { x } from './dep.js'; globalThis.r = x;"));

    [Fact(Timeout = 600000)]
    public async Task ARenamedExportIsVisibleOnTheNamespace()
        => Assert.Equal("4", await Run(
            "const x = 4; export { x as z };",
            "import * as ns from './dep.js'; globalThis.r = ns.z;"));

    // ---- With a `from` clause the names come from the other module ----

    [Fact(Timeout = 600000)]
    public async Task AClauseCanReExportFromAnotherModule()
        => Assert.Equal("1", await Run(
            "export { one } from './base.js';",
            "import { one } from './dep.js'; globalThis.r = one;"));

    [Fact(Timeout = 600000)]
    public async Task AReExportCanRename()
        => Assert.Equal("3", await Run(
            "export { one as uno, two } from './base.js';",
            "import { uno, two } from './dep.js'; globalThis.r = uno + two;"));

    // ---- Diagnosable failures ----

    // Every ReferencedBindings of a NamedExports must be declared; exporting a name the module
    // does not have is an early error rather than a read of a binding that is not there.
    [Fact(Timeout = 600000)]
    public async Task ExportingAnUndeclaredNameIsAnError()
    {
        var error = await Assert.ThrowsAnyAsync<Exception>(() => Run(
            "export { nope };",
            "import * as ns from './dep.js'; globalThis.r = 1;"));

        Assert.Contains("nope", error.Message);
    }

    // ---- `export * from` republishes the source's named exports ----

    [Fact(Timeout = 600000)]
    public async Task ExportStarFromRepublishesEveryNamedExport()
        => Assert.Equal("3", await Run(
            "export * from './base.js';",
            "import { one, two } from './dep.js'; globalThis.r = one + two;"));

    [Fact(Timeout = 600000)]
    public async Task ExportStarFromIsVisibleOnTheNamespace()
        => Assert.Equal("1", await Run(
            "export * from './base.js';",
            "import * as ns from './dep.js'; globalThis.r = ns.one;"));

    // A star re-export forwards NAMED exports only — the star entry's [[ImportName]] is
    // all-but-default — so a barrel file does not inherit the source's default.
    [Fact(Timeout = 600000)]
    public async Task ExportStarFromDoesNotForwardTheDefault()
        => Assert.Equal("undefined", await Run(
            "export * from './withdefault.js';",
            "import * as ns from './dep.js'; globalThis.r = typeof ns.default;"));

    // A star re-export composes with the module's own exports.
    [Fact(Timeout = 600000)]
    public async Task ExportStarFromComposesWithLocalExports()
        => Assert.Equal("6", await Run(
            "export * from './base.js'; const three = 3; export { three };",
            "import { one, two, three } from './dep.js'; globalThis.r = one + two + three;"));

    // A ModuleExportName is an IdentifierName, so the name after `as` may be any reserved word —
    // it is a property of the namespace. But WITHOUT a `from` clause the local name is an
    // IdentifierReference to a binding here, so a reserved word cannot appear there.
    // (`export { a as in } from 'm'` is covered in the parser's own suite.)
    [Theory(Timeout = 600000)]
    [InlineData("export { null };")]
    [InlineData("export { in };")]
    [InlineData("export { null as x };")]
    public async Task AReservedWordCannotBeALocalExportName(string dep)
    {
        var error = await Assert.ThrowsAnyAsync<Exception>(() => Run(
            dep, "import * as ns from './dep.js'; globalThis.r = 1;"));

        Assert.DoesNotContain("NullReference", error.GetType().Name);
    }

    [Fact(Timeout = 600000)]
    public async Task ExportStarAsNamespaceFromStillWorks()
        => Assert.Equal("1", await Run(
            "export * as base from './base.js';",
            "import { base } from './dep.js'; globalThis.r = base.one;"));
}
