using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Broiler.JavaScript.Modules;
using Xunit;

namespace Broiler.JavaScript.Modules.Tests;

/// <summary>
/// <c>import.meta</c>, and the one member of it that is deliberately absent.
/// </summary>
/// <remarks>
/// <para>
/// It used to be a SyntaxError — "import.meta not supported" — from the compiler's meta-property
/// path, which handled only <c>new.target</c>. That was deterministic rather than a crash, so it was
/// carried as a capability decision rather than a defect. This is the decision: <c>import.meta</c>
/// is implemented, with <c>url</c> on it, and <c>resolve</c> is not.
/// </para>
/// <para>
/// It compiles to a read of <c>meta</c> off the module record the body already receives as its
/// <c>module</c> parameter, so identity, lazy creation and the URL all belong to the module host.
/// Every expectation below except <c>resolve</c> is Chromium's measured answer for a module in a
/// page: identity stable, prototype null, <c>url</c> the module's own absolute URL. Chromium also
/// carries <c>resolve</c>; see the test at the end for why this does not.
/// </para>
/// </remarks>
public class ImportMetaTests
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

    private static async Task<string> Run(string main, Dictionary<string, string> extra = null)
    {
        var files = extra ?? new Dictionary<string, string>();
        files["file:///app/main.js"] = main;

        using var ctx = new UrlModuleContext(files);
        await ctx.RunScriptAsync(main, "file:///app/main.js", uniqueModuleID: "file:///app/main.js");
        return ctx.Eval("String(globalThis.r)").ToString();
    }

    /// <summary><c>url</c> is the module's own absolute URL, and each module reports its own rather
    /// than the entry point's.</summary>
    [Fact(Timeout = 600000)]
    public async Task UrlIsTheModulesOwnUrl()
        => Assert.Equal("file:///app/dep.js|file:///app/main.js", await Run(
            "import { u } from './dep.js'; globalThis.r = u + '|' + import.meta.url;",
            new Dictionary<string, string> { ["file:///app/dep.js"] = "export const u = import.meta.url;" }));

    /// <summary>One object per module, created once and stable — a module is entitled to hang state
    /// off it, which only works if every read is the same object.</summary>
    [Fact(Timeout = 600000)]
    public async Task TheObjectIsOneObjectAndTakesNewProperties()
        => Assert.Equal("true|7", await Run(
            "var first = import.meta; import.meta.mine = 7; "
            + "globalThis.r = (first === import.meta) + '|' + import.meta.mine;"));

    /// <summary>A null prototype, so a property a module adds cannot be confused with one inherited
    /// from <c>Object.prototype</c>. Chromium answers the same.</summary>
    [Fact(Timeout = 600000)]
    public async Task ThePrototypeIsNull()
        => Assert.Equal("null|url", await Run(
            "globalThis.r = String(Object.getPrototypeOf(import.meta)) + '|' + Object.keys(import.meta).join(',');"));

    /// <summary>It is readable wherever module code runs, not only at the module's top level.</summary>
    [Fact(Timeout = 600000)]
    public async Task ItIsReadableFromNestedFunctionsAndArrows()
        => Assert.Equal("file:///app/main.js|file:///app/main.js", await Run(
            "function f() { return import.meta.url; } "
            + "globalThis.r = f() + '|' + (() => import.meta.url)();"));

    /// <summary>
    /// Outside module code it stays an early SyntaxError, which is what ES2025 §13.3.12 asks for and
    /// what a <c>try { eval('import.meta') }</c> feature-detect expects to see.
    /// </summary>
    [Theory(Timeout = 600000)]
    [InlineData("import.meta.url")]
    [InlineData("eval('import.meta.url')")]
    [InlineData("Function('return import.meta.url')()")]
    public void OutsideAModuleItIsASyntaxError(string code)
    {
        using var ctx = new UrlModuleContext([]);
        var error = Assert.ThrowsAny<Exception>(() => ctx.Eval(code));
        Assert.Contains("import.meta", error.Message);
    }

    /// <summary>
    /// <c>resolve</c> is absent, and pinned absent so adding it is a decision rather than a drift.
    /// This context's resolver is existence-based — it probes for the file and answers null when
    /// nothing is there — while <c>import.meta.resolve</c> resolves a specifier to a URL whether or
    /// not anything is at it. Built on this resolver it would throw where a browser answers, which a
    /// page cannot feature-detect; its absence, which this pins, a page can.
    /// </summary>
    [Fact(Timeout = 600000)]
    public async Task ResolveIsAbsentAndDetectablySo()
        => Assert.Equal("undefined|false", await Run(
            "globalThis.r = (typeof import.meta.resolve) + '|' + ('resolve' in import.meta);"));
}
