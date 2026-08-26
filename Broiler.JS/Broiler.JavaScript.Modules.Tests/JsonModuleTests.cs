using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Broiler.JavaScript.Modules;
using Xunit;

namespace Broiler.JavaScript.Modules.Tests;

/// <summary>
/// JSON modules, and the one place where <c>import</c> and <c>require</c> deliberately see a
/// different shape of the same file.
/// </summary>
/// <remarks>
/// <para>
/// <c>import d from './data.json'</c> was <c>undefined</c>. One wrapper served both callers —
/// <c>module.exports = &lt;json&gt;</c> — which is what CommonJS <c>require</c> wants and is exactly
/// wrong for ESM: a default import reads <c>.default</c> off the parsed value and finds nothing. So
/// every JSON import was <c>undefined</c>, for every JSON shape, and JSON modules were unusable.
/// A file whose whole content was <c>null</c> was worse than unusable — <c>JSModule</c>'s exports
/// setter refuses null, so it threw on load.
/// </para>
/// <para>
/// The decision this pins: the module <b>stores the ES namespace</b>, <c>{ default: value }</c> —
/// ES2025 gives a JSON module exactly one export, <c>default</c>, and no named exports — and the
/// CommonJS view unwraps it. Both callers get what their own specification says, which one object
/// cannot do.
/// </para>
/// <para>
/// The deliberate deviation is <c>import { a } from './x.json'</c>: per spec a link error, here
/// <c>undefined</c>, because raising the link error needs whole-module analysis this engine does not
/// do. It used to read <c>a</c> off the parsed object. Browsers and Node both reject the form, so
/// nothing portable is losing a behaviour it could rely on.
/// </para>
/// </remarks>
public class JsonModuleTests
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

    private static async Task<string> Run(string json, string main)
    {
        var files = new Dictionary<string, string>
        {
            ["file:///app/data.json"] = json,
            ["file:///app/main.js"] = main,
        };

        using var ctx = new UrlModuleContext(files);
        await ctx.RunScriptAsync(main, "file:///app/main.js", uniqueModuleID: "file:///app/main.js");
        return ctx.Eval("String(globalThis.r)").ToString();
    }

    /// <summary>The default import is the parsed value — for every JSON shape, not only objects.
    /// Each of these was <c>undefined</c>, and <c>null</c> threw on load.</summary>
    [Theory(Timeout = 600000)]
    [InlineData("{\"a\":1}", "{\"a\":1}")]
    [InlineData("[1,2]", "[1,2]")]
    [InlineData("5", "5")]
    [InlineData("\"s\"", "\"s\"")]
    [InlineData("true", "true")]
    [InlineData("null", "null")]
    public async Task ADefaultImportIsTheParsedValue(string json, string expected)
        => Assert.Equal(expected, await Run(
            json, "import d from './data.json'; globalThis.r = JSON.stringify(d);"));

    /// <summary>The namespace holds exactly one name, <c>default</c> — not the JSON's own keys,
    /// which is what it used to expose.</summary>
    [Fact(Timeout = 600000)]
    public async Task TheNamespaceHoldsOnlyDefault()
        => Assert.Equal("default|{\"a\":1}", await Run(
            "{\"a\":1,\"b\":2}",
            "import * as ns from './data.json'; "
            + "globalThis.r = Object.keys(ns).join(',') + '|' + JSON.stringify(ns.default).replace(',\"b\":2', '');"));

    /// <summary>A dynamic import resolves to the same namespace a static one binds against.</summary>
    [Fact(Timeout = 600000)]
    public async Task ADynamicImportResolvesToTheSameNamespace()
        => Assert.Equal("default|{\"a\":1}", await Run(
            "{\"a\":1}",
            "globalThis.r = 'pending'; import('./data.json').then(function (m) { "
            + "globalThis.r = Object.keys(m).join(',') + '|' + JSON.stringify(m.default); });"));

    /// <summary><c>require</c> is unchanged: it hands back the parsed value itself, which is what
    /// CommonJS says and what it already did.</summary>
    [Theory(Timeout = 600000)]
    [InlineData("{\"a\":1}", "{\"a\":1}")]
    [InlineData("[1,2]", "[1,2]")]
    [InlineData("null", "null")]
    public async Task RequireStillHandsBackTheParsedValue(string json, string expected)
        => Assert.Equal(expected, await Run(
            json, "globalThis.r = JSON.stringify(require('./data.json'));"));

    /// <summary>The two views of one file coexist in one module, and neither disturbs the other —
    /// the point of splitting them rather than picking a winner.</summary>
    [Fact(Timeout = 600000)]
    public async Task BothViewsOfOneFileAgreeOnTheValue()
        => Assert.Equal("{\"a\":1}|{\"a\":1}|true", await Run(
            "{\"a\":1}",
            "import d from './data.json'; var r = require('./data.json'); "
            + "globalThis.r = JSON.stringify(d) + '|' + JSON.stringify(r) + '|' + (d === r);"));

    /// <summary>
    /// The deliberate deviation, pinned so it is a decision rather than a drift: a named import from
    /// a JSON module is <c>undefined</c> rather than the link error the spec asks for. It used to
    /// read the property off the parsed object.
    /// </summary>
    [Fact(Timeout = 600000)]
    public async Task ANamedImportFromAJsonModuleIsUndefined()
        => Assert.Equal("undefined", await Run(
            "{\"a\":1}", "import { a } from './data.json'; globalThis.r = String(a);"));

    /// <summary>A JavaScript module is untouched by any of this — the split is JSON-only.</summary>
    [Fact(Timeout = 600000)]
    public async Task AJavaScriptModuleIsUnaffected()
    {
        var files = new Dictionary<string, string>
        {
            ["file:///app/dep.js"] = "export const a = 1; export default 9;",
            ["file:///app/main.js"] =
                "import d, { a } from './dep.js'; import * as ns from './dep.js'; "
                + "globalThis.r = d + '|' + a + '|' + Object.keys(ns).sort().join(',');",
        };

        using var ctx = new UrlModuleContext(files);
        await ctx.RunScriptAsync(files["file:///app/main.js"], "file:///app/main.js",
            uniqueModuleID: "file:///app/main.js");
        Assert.Equal("9|1|a,default", ctx.Eval("String(globalThis.r)").ToString());
    }
}
