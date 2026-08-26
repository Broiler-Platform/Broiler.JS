using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Broiler.JavaScript.Modules;
using Xunit;

namespace Broiler.JavaScript.Modules.Tests;

/// <summary>
/// What an import attribute <em>means</em>. The grammar — which clause shapes compile, and where a
/// clause may appear — is <see cref="ModuleAttributeClauseTests"/>.
/// </summary>
/// <remarks>
/// <para>
/// Attributes parsed everywhere the grammar allows and then nothing acted on them: nothing read
/// <c>AstImportStatement.Attributes</c>, the export forms discarded theirs outright, and the
/// compiler's call to the loader passed only the specifier. So <c>with { type: 'json' }</c> — the
/// portable form, and the only one a browser accepts on a JSON module — was accepted and ignored,
/// and so was <c>with { flavour: 'nonsense' }</c>. Silence is the one answer an assertion mechanism
/// must never give.
/// </para>
/// <para>
/// <b>Where each failure is raised is measured from Chromium, not chosen.</b> On a static
/// declaration the keys are literals, so a bad key and a duplicate key are early
/// <b>SyntaxError</b>s; whether the <c>type</c> <em>value</em> names a module type, and whether the
/// module it resolves to is of that type, depends on the module and is a load-time
/// <b>TypeError</b>. A dynamic <c>import()</c>'s keys are a runtime value, so it reports both as
/// TypeErrors. The messages are Chromium's own, measured from the same probe.
/// </para>
/// <para>
/// <b>One deliberate divergence.</b> A <c>.json</c> module imported with no attribute at all loads
/// here; a browser rejects it. There the attribute defends against a server returning JSON where
/// script was expected — a mismatch that cannot arise in this host, where the key resolved locally
/// is itself the type. This context also serves <c>require</c>, which has no attributes at all, so
/// demanding one from <c>import</c> would make the two halves of one host disagree about one file.
/// Pinned below so it stays a decision.
/// </para>
/// </remarks>
public class ImportAttributeEnforcementTests
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

    private static Dictionary<string, string> Files() => new()
    {
        ["file:///app/data.json"] = "{\"a\":1}",
        ["file:///app/mod.js"] = "export const x = 1; export default 9;",
    };

    private static async Task<string> Run(string main)
    {
        var files = Files();
        files["file:///app/main.js"] = main;

        using var ctx = new UrlModuleContext(files);
        await ctx.RunScriptAsync(main, "file:///app/main.js", uniqueModuleID: "file:///app/main.js");
        return ctx.Eval("String(globalThis.r)").ToString();
    }

    /// <summary>
    /// Loads <paramref name="moduleSource"/> through a dynamic import so its failure is caught in
    /// JavaScript and the error's NAME is visible — which is the whole point for the static cases,
    /// where the difference between SyntaxError and TypeError is what is being pinned.
    /// </summary>
    private static async Task<string> ErrorFrom(string moduleSource)
    {
        var files = Files();
        files["file:///app/bad.js"] = moduleSource;
        var main =
            "globalThis.r = 'pending'; (async () => { try { await import('./bad.js'); "
            + "globalThis.r = 'NO_THROW'; } catch (e) { globalThis.r = e.name + '|' + e.message; } })();";
        files["file:///app/main.js"] = main;

        using var ctx = new UrlModuleContext(files);
        await ctx.RunScriptAsync(main, "file:///app/main.js", uniqueModuleID: "file:///app/main.js");
        return ctx.Eval("String(globalThis.r)").ToString();
    }

    private static async Task<string> DynamicError(string call)
    {
        var main =
            "globalThis.r = 'pending'; (async () => { try { const m = " + call
            + "; globalThis.r = 'OK:' + Object.keys(m).sort().join(','); } "
            + "catch (e) { globalThis.r = e.name + '|' + e.message; } })();";
        return await Run(main);
    }

    // ---------------------------------------------------------------- what a clause may say

    /// <summary><c>type</c> is the only key the platform defines. An unknown one is an early
    /// SyntaxError on a static declaration, exactly as in a browser.</summary>
    [Theory(Timeout = 600000)]
    [InlineData("import d from './mod.js' with { flavour: 'x' };", "flavour")]
    [InlineData("import d from './data.json' with { type: 'json', extra: '1' };", "extra")]
    [InlineData("import d from './mod.js' with { assert: 'json' };", "assert")]
    [InlineData("export { x } from './mod.js' with { flavour: 'x' };", "flavour")]
    [InlineData("export * from './mod.js' with { flavour: 'x' };", "flavour")]
    public async Task AnUnknownAttributeKeyIsAnEarlySyntaxError(string source, string key)
        => Assert.Equal($"SyntaxError|Invalid attribute key \"{key}\".", await ErrorFrom(source));

    /// <summary>A duplicate key is a Syntax Error in the proposal itself, whichever spelling the
    /// two keys use.</summary>
    [Theory(Timeout = 600000)]
    [InlineData("import d from './mod.js' with { type: 'json', type: 'json' };")]
    [InlineData("import d from './mod.js' with { type: 'json', \"type\": 'json' };")]
    [InlineData("export { x } from './mod.js' with { \"type\": 'json', type: 'json' };")]
    public async Task ADuplicateAttributeKeyIsAnEarlySyntaxError(string source)
        => Assert.Equal("SyntaxError|Import attribute has duplicate key 'type'", await ErrorFrom(source));

    /// <summary>The module-type vocabulary is the platform's, so a value outside it is a typo and
    /// says so — at load time, because only the module can settle it.</summary>
    [Theory(Timeout = 600000)]
    [InlineData("import d from './mod.js' with { type: 'bogus' };", "bogus")]
    [InlineData("import d from './data.json' with { type: 'javascript' };", "javascript")]
    [InlineData("export * as n from './mod.js' with { type: 'nope' };", "nope")]
    public async Task AnUnknownModuleTypeIsALoadTimeTypeError(string source, string type)
        => Assert.Equal($"TypeError|\"{type}\" is not a valid module type.", await ErrorFrom(source));

    /// <summary>
    /// <c>css</c> is a real module type and a real capability this engine does not have, so it is
    /// told apart from a typo: a page can distinguish "not implemented here" from "not a thing".
    /// </summary>
    [Fact(Timeout = 600000)]
    public async Task ACssModuleTypeIsRejectedAsUnimplementedRatherThanAsATypo()
        => Assert.Equal("TypeError|CSS module scripts are not implemented.",
            await ErrorFrom("import d from './mod.js' with { type: 'css' };"));

    // ---------------------------------------------------------------- and whether it is true

    /// <summary>A <c>type</c> that does not match the resolved module fails the load. The web
    /// checks the response MIME type; this host has no MIME types and checks the resolved module
    /// key, which is the same fact by the only means available — it is what decided the module
    /// would be parsed as JSON in the first place.</summary>
    [Theory(Timeout = 600000)]
    [InlineData("import d from './mod.js' with { type: 'json' };")]
    [InlineData("export { x } from './mod.js' with { type: 'json' };")]
    [InlineData("export * from './mod.js' with { type: 'json' };")]
    public async Task AssertingJsonForAModuleThatIsNotJsonFailsTheLoad(string source)
    {
        var error = await ErrorFrom(source);
        Assert.StartsWith("TypeError|Failed to load module \"file:///app/mod.js\"", error);
        Assert.Contains("it was imported with type: \"json\" but it is not a JSON module.", error);
    }

    /// <summary>A matching assertion loads, on every import form that takes a clause.</summary>
    [Theory(Timeout = 600000)]
    [InlineData("import d from './data.json' with { type: 'json' }; globalThis.r = d.a;", "1")]
    [InlineData("import * as ns from './data.json' with { type: 'json' }; globalThis.r = Object.keys(ns).join(',');", "default")]
    [InlineData("import d from './mod.js' with { }; globalThis.r = d;", "9")]
    [InlineData("import d from './mod.js'; globalThis.r = d;", "9")]
    public async Task AMatchingOrEmptyClauseLoads(string main, string expected)
        => Assert.Equal(expected, await Run(main));

    /// <summary>
    /// The deliberate divergence: a JSON module with no attribute loads here and would not in a
    /// browser. Pinned so turning it into a rejection is a decision rather than a drift — and note
    /// the same file read through <c>require</c>, which has no attributes at all and must keep
    /// agreeing with it.
    /// </summary>
    [Fact(Timeout = 600000)]
    public async Task AJsonModuleWithNoAttributeStillLoads()
        => Assert.Equal("1|1", await Run(
            "import d from './data.json'; globalThis.r = d.a + '|' + require('./data.json').a;"));

    // ---------------------------------------------------------------- the dynamic form

    /// <summary>A dynamic import's keys are a runtime value, so the key check that is an early
    /// SyntaxError on a declaration is a TypeError here — as in a browser.</summary>
    [Fact(Timeout = 600000)]
    public async Task ADynamicImportReportsABadKeyAsATypeError()
        => Assert.Equal("TypeError|Invalid attribute key \"flavour\".",
            await DynamicError("await import('./mod.js', { with: { flavour: 'x' } })"));

    [Theory(Timeout = 600000)]
    [InlineData("await import('./mod.js', 'nonsense')", "The second argument to import() must be an object")]
    [InlineData("await import('./mod.js', null)", "The second argument to import() must be an object")]
    [InlineData("await import('./mod.js', { with: 5 })", "The 'with' option must be an object")]
    [InlineData("await import('./mod.js', { with: null })", "The 'with' option must be an object")]
    [InlineData("await import('./mod.js', { with: { type: 123 } })", "Import attribute value must be a string")]
    [InlineData("await import('./mod.js', { with: { type: undefined } })", "Import attribute value must be a string")]
    [InlineData("await import('./mod.js', { with: { type: 'bogus' } })", "\"bogus\" is not a valid module type.")]
    public async Task ADynamicImportValidatesItsOptionsObject(string call, string message)
        => Assert.Equal("TypeError|" + message, await DynamicError(call));

    /// <summary>Options that assert nothing are not an error: no second argument, an options object
    /// with no <c>with</c>, and a matching assertion all load.</summary>
    [Theory(Timeout = 600000)]
    [InlineData("await import('./mod.js')", "OK:default,x")]
    [InlineData("await import('./mod.js', { other: 1 })", "OK:default,x")]
    [InlineData("await import('./mod.js', { with: { } })", "OK:default,x")]
    [InlineData("await import('./data.json', { with: { type: 'json' } })", "OK:default")]
    public async Task ADynamicImportThatAssertsNothingOrAssertsTrulyLoads(string call, string expected)
        => Assert.Equal(expected, await DynamicError(call));

    /// <summary><c>require</c> takes no attributes and is untouched by any of this.</summary>
    [Fact(Timeout = 600000)]
    public async Task RequireIsUnaffected()
        => Assert.Equal("{\"a\":1}|9", await Run(
            "globalThis.r = JSON.stringify(require('./data.json')) + '|' + require('./mod.js').default;"));
}
