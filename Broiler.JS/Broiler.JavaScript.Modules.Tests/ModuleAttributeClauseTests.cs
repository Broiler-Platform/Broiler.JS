using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Broiler.JavaScript.Modules;
using Broiler.JavaScript.Runtime;
using Xunit;

namespace Broiler.JavaScript.Modules.Tests;

/// <summary>
/// Import attributes (the <c>with { … }</c> clause) on both import and export declarations.
/// </summary>
/// <remarks>
/// Two gaps, both in the grammar rather than the semantics. <c>AttributeKey</c> is
/// <c>IdentifierName | StringLiteral</c>, but only the identifier half was implemented, so the
/// quoted form the proposal's own examples use — <c>with { "type": "json" }</c> — was rejected as
/// an unexpected token. And an <c>ExportDeclaration</c> with a <c>FromClause</c> takes a
/// <c>WithClause</c> exactly as an <c>ImportDeclaration</c> does, but none of the three export
/// <c>from</c> forms accepted one.
/// <para>
/// The attributes are parsed and not yet acted on — nothing reads
/// <c>AstImportStatement.Attributes</c> either — so what this fixes is valid source failing to
/// compile, not attribute enforcement, which remains a separate capability.
/// </para>
/// </remarks>
public class ModuleAttributeClauseTests
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

    // ---- AttributeKey may be a StringLiteral, not only an identifier ----

    [Theory(Timeout = 600000)]
    [InlineData("import { a } from './dep.js' with { \"type\": 'javascript' }; globalThis.r = a;")]
    [InlineData("import { a } from './dep.js' with { type: 'javascript' }; globalThis.r = a;")]
    [InlineData("import { a } from './dep.js' with { \"type\": 'javascript', \"other\": 'x' }; globalThis.r = a;")]
    public async Task AnAttributeKeyMayBeAStringOrAnIdentifier(string main)
        => Assert.Equal("1", await Run(main));

    [Fact(Timeout = 600000)]
    public async Task ADefaultImportTakesAttributes()
        => Assert.Equal("9", await Run(
            "import d from './dep.js' with { \"type\": 'javascript' }; globalThis.r = d;"));

    [Fact(Timeout = 600000)]
    public async Task ANamespaceImportTakesAttributes()
        => Assert.Equal("1", await Run(
            "import * as ns from './dep.js' with { type: 'javascript' }; globalThis.r = ns.a;"));

    // ---- Every export `from` form takes a WithClause too ----

    [Fact(Timeout = 600000)]
    public async Task AReExportClauseTakesAttributes()
        => Assert.Equal("1", await Run(
            "export { a } from './dep.js' with { type: 'javascript' };"
            + "import { a } from './dep.js'; globalThis.r = a;"));

    [Fact(Timeout = 600000)]
    public async Task AStarReExportTakesAttributes()
        => Assert.Equal("3", await Run(
            "export * from './dep.js' with { \"type\": 'javascript' };"
            + "import { a, b } from './dep.js'; globalThis.r = a + b;"));

    [Fact(Timeout = 600000)]
    public async Task AStarAsNamespaceReExportTakesAttributes()
        => Assert.Equal("1", await Run(
            "export * as ns from './dep.js' with { type: 'javascript' };"
            + "import { a } from './dep.js'; globalThis.r = a;"));

    // A malformed clause is still rejected: the value must be a string literal.
    [Theory(Timeout = 600000)]
    [InlineData("import { a } from './dep.js' with { type: javascript };")]
    [InlineData("import { a } from './dep.js' with { type };")]
    public async Task AMalformedAttributeClauseIsRejected(string main)
        => await Assert.ThrowsAnyAsync<Exception>(() => Run(main));
}
