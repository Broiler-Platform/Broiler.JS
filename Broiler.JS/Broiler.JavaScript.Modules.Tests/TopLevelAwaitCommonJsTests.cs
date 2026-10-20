using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Broiler.JavaScript.Modules;
using Xunit;

namespace Broiler.JavaScript.Modules.Tests;

/// <summary>
/// Correction J1 of milestone SJL-12.5 (Broiler.VM's JSD-0080, decision D21; the changelog): a CommonJS
/// module a program loads while <c>EvalWithTopLevelAwaitAsync</c> runs it is compiled as under <c>Eval</c>,
/// without the program's permission to await at its top level. Before, the permission was an ambient
/// scope around the program and everything it ran, and the module's compile took it.
/// </summary>
public class TopLevelAwaitCommonJsTests
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

    // The program's completion is the import's outcome, which both methods wait for.
    private const string Program =
        "import('file:///app/module.cjs').then(function () { return 'loaded'; }, function (e) { return 'threw ' + e.name; })";

    private static UrlModuleContext Context(string module)
        => new(new Dictionary<string, string> { ["file:///app/module.cjs"] = module });

    [Fact(Timeout = 600000)]
    public async Task A_CommonJS_module_the_program_loads_may_not_await_at_its_own_top_level()
    {
        const string module = "module.exports = 1; await 1;";

        using (var ctx = Context(module))
            Assert.Equal("threw SyntaxError", (await ctx.ExecuteAsync(Program, "file:///app/main.js")).ToString());

        using (var ctx = Context(module))
            Assert.Equal("threw SyntaxError", (await ctx.EvalWithTopLevelAwaitAsync(Program, "file:///app/main.js")).ToString());
    }

    [Fact(Timeout = 600000)]
    public async Task A_CommonJS_module_that_does_not_await_loads_as_before()
    {
        using var ctx = Context("module.exports = 1;");
        Assert.Equal("loaded", (await ctx.EvalWithTopLevelAwaitAsync(Program, "file:///app/main.js")).ToString());
    }
}
