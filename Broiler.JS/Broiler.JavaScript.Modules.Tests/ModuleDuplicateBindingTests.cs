using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Broiler.JavaScript.Modules;
using Broiler.JavaScript.Runtime;
using Xunit;

namespace Broiler.JavaScript.Modules.Tests;

/// <summary>
/// Module early errors that follow from a module's own top level (ES2024 16.2.1.5): the
/// ImportedBoundNames of a Module must contain no duplicate entries and must not intersect its
/// VarDeclaredNames, and its ExportedNames must contain no duplicate entries either — which
/// subsumes "at most one default export", since <c>default</c> is simply an exported name.
/// </summary>
/// <remarks>
/// These were accepted and RUN. A collision with a top-level <c>let</c>/<c>const</c>/class was
/// already caught by the scope machinery, which masked the rest: a duplicate among the imports
/// themselves and a collision with a hoisted <c>var</c> both slipped through, so
/// <c>import { a, a } from 'm'</c> and <c>import { a } from 'm'; var a;</c> ran with one binding
/// quietly winning.
/// <para>
/// The check needs no module parse goal — which the parser does not have — because an
/// ImportDeclaration is only ever legal in module code, so a program without one is left alone.
/// The early error that DOES need the goal is <c>await</c> as an identifier, reserved from a
/// module's first token; it is still outstanding.
/// </para>
/// </remarks>
public class ModuleDuplicateBindingTests
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

    private static async Task<string> Run(string main)
    {
        var files = new Dictionary<string, string>
        {
            ["file:///app/dep.js"] = "export const a = 1; export const b = 2; export default 9;",
        };

        using var ctx = new UrlModuleContext(files);
        await ctx.RunScriptAsync(main, "file:///app/main.js", uniqueModuleID: "file:///app/main.js");
        return ctx.Eval("String(globalThis.r)").ToString();
    }

    private static async Task AssertRejected(string main, string expectedInMessage)
    {
        var error = await Assert.ThrowsAnyAsync<Exception>(() => Run(main));
        Assert.Contains(expectedInMessage, error.Message);
    }

    // ---- No duplicate among the ImportedBoundNames ----

    [Theory(Timeout = 600000)]
    [InlineData("import { a, a } from './dep.js';")]                                 // twice in one clause
    [InlineData("import { a, b as a } from './dep.js';")]                            // renamed onto an earlier one
    [InlineData("import a, { b as a } from './dep.js';")]                            // default and a named
    [InlineData("import { a } from './dep.js'; import * as a from './dep.js';")]     // named and a namespace
    [InlineData("import { a } from './dep.js'; import { a } from './dep.js';")]      // two statements
    public async Task ADuplicateImportedBindingIsRejected(string main)
        => await AssertRejected(main, "already been declared");

    // ---- ImportedBoundNames must not intersect VarDeclaredNames ----

    [Theory(Timeout = 600000)]
    [InlineData("import { a } from './dep.js'; var a = 1;")]
    [InlineData("import { a } from './dep.js'; { var a; }")]            // a nested `var` still hoists here
    [InlineData("import { a } from './dep.js'; function a() {}")]
    [InlineData("var a = 1; import { a } from './dep.js';")]            // either order
    public async Task AnImportedBindingCollidingWithAVarIsRejected(string main)
        => await AssertRejected(main, "already been declared");

    // ---- The ExportedNames of a module must contain no duplicate entries ----
    //
    // "Only one default export" is not a rule of its own: `default` is just an exported name,
    // which is why a second `export default` and an `export { x as default }` beside one are the
    // same error.

    [Theory(Timeout = 600000)]
    [InlineData("export default 1; export default 2;")]
    [InlineData("export default function f() {};\nexport default 2;")]
    [InlineData("const x = 1; export { x as default }; export default 2;")]
    [InlineData("export default 1; const x = 2; export { x as default };")]
    public async Task ASecondDefaultExportIsRejected(string main)
        => await AssertRejected(main, "Duplicate export name 'default'");

    [Theory(Timeout = 600000)]
    [InlineData("const x = 1, y = 2; export { x, y as x };")]                 // twice in one clause
    [InlineData("const x = 1, y = 2; export { x }; export { y as x };")]      // across two clauses
    [InlineData("export const x = 1; const y = 2; export { y as x };")]       // declaration then clause
    [InlineData("export function f() {} const y = 1; export { y as f };")]
    [InlineData("export class C {} const y = 1; export { y as C };")]
    [InlineData("export { a as x } from './dep.js'; const y = 1; export { y as x };")]
    public async Task ADuplicateExportedNameIsRejected(string main)
        => await AssertRejected(main, "Duplicate export name");

    // ---- Valid neighbours: none of these may be caught by the checks above ----

    [Fact(Timeout = 600000)]
    public async Task DistinctImportedNamesAreAccepted()
        => Assert.Equal("3", await Run("import { a, b } from './dep.js'; globalThis.r = a + b;"));

    [Fact(Timeout = 600000)]
    public async Task ADefaultAndANamedImportAreAccepted()
        => Assert.Equal("10", await Run("import d, { a } from './dep.js'; globalThis.r = d + a;"));

    [Fact(Timeout = 600000)]
    public async Task ANamespaceImportIsAccepted()
        => Assert.Equal("1", await Run("import * as ns from './dep.js'; globalThis.r = ns.a;"));

    // The name is only reserved at the MODULE's top level: a nested function may declare its own.
    [Fact(Timeout = 600000)]
    public async Task ANestedFunctionMayShadowAnImportedName()
        => Assert.Equal("2", await Run(
            "import { a } from './dep.js'; function g() { var a = 2; return a; } globalThis.r = g();"));

    [Fact(Timeout = 600000)]
    public async Task AnImportAlongsideAnUnrelatedVarIsAccepted()
        => Assert.Equal("2", await Run("import { a } from './dep.js'; var b = 1; globalThis.r = a + b;"));

    [Fact(Timeout = 600000)]
    public async Task ASingleDefaultExportIsAccepted()
        => Assert.Equal("1", await Run("export default 1; globalThis.r = 1;"));

    // Distinct exported names, across every form, must all still be accepted together.
    [Fact(Timeout = 600000)]
    public async Task DistinctExportedNamesAcrossEveryFormAreAccepted()
        => Assert.Equal("1", await Run(
            "export const c = 1;"
            + "export function fn() {}"
            + "export class Cl {}"
            + "const local = 2; export { local, local as aliased };"
            + "export { a as reexported } from './dep.js';"
            + "export * as ns from './dep.js';"
            + "export default 3;"
            + "globalThis.r = 1;"));

    // `export * from` names nothing statically, so two of them do not collide with each other.
    [Fact(Timeout = 600000)]
    public async Task TwoStarReExportsAreAccepted()
        => Assert.Equal("1", await Run(
            "export * from './dep.js'; export * from './dep.js'; globalThis.r = 1;"));

    // A program with no import declaration is not module code for this purpose, so an ordinary
    // duplicate `var` — legal in a script — must stay legal.
    [Fact(Timeout = 600000)]
    public async Task ADuplicateVarWithoutAnyImportIsStillAccepted()
        => Assert.Equal("2", await Run("var x = 1; var x = 2; globalThis.r = x;"));
}
