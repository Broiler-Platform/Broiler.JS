using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Broiler.JavaScript.Modules;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Storage;
using Xunit;

namespace Broiler.JavaScript.Modules.Tests;

/// <summary>
/// ECMAScript module semantics, driven through an in-memory <see cref="JSModuleContext"/>: live
/// import bindings, the module namespace exotic object, link-time errors, cycles with a
/// cross-module TDZ, top-level-await ordering and evaluation-error caching, module-code semantics,
/// the declaration forms of <c>export</c>, and host resolution of every specifier.
/// </summary>
/// <remarks>
/// Node v24 gives the reference answer for each case. Every case here failed before the module
/// linker existed: imports were copies taken when the <c>import</c> statement ran, the namespace was
/// an ordinary object, a missing export read <c>undefined</c>, and a raw specifier equal to a cache
/// key never reached <c>Resolve</c>.
/// </remarks>
public class ModuleSemanticsTests
{
    private sealed class MemoryModuleContext(Dictionary<string, string> files) : JSModuleContext
    {
        public readonly List<string> Resolved = [];
        public readonly List<string> Loaded = [];

        protected override string Resolve(string dirPath, string relativePath)
        {
            Resolved.Add(relativePath);
            if (Uri.TryCreate(relativePath, UriKind.Absolute, out var abs)) return abs.AbsoluteUri;
            if (!relativePath.StartsWith('.')) return null;
            return Uri.TryCreate(new Uri(dirPath), relativePath, out var rel) ? rel.AbsoluteUri : null;
        }

        protected override string GetModuleDirectory(string fullPath) => fullPath;

        protected override Task<string> ReadModuleSourceAsync(JSModule module)
        {
            Loaded.Add(module.filePath);
            return Task.FromResult(files.TryGetValue(module.filePath, out var src)
                ? src
                : throw new FileNotFoundException(module.filePath));
        }
    }

    private const string Main = "file:///app/main.js";

    private static async Task<string> Run(Dictionary<string, string> files, string main, Action<MemoryModuleContext>? inspect = null)
    {
        files[Main] = main;
        using var ctx = new MemoryModuleContext(files);
        try
        {
            await ctx.RunScriptAsync(main, Main, uniqueModuleID: Main);
            return ctx.Eval("String(globalThis.r)").ToString();
        }
        finally
        {
            inspect?.Invoke(ctx);
        }
    }

    /// <summary>Runs <paramref name="main"/> and returns the JavaScript error name it failed with,
    /// followed by what <c>globalThis.r</c> held at that point.</summary>
    private static async Task<string> RunExpectingError(Dictionary<string, string> files, string main)
    {
        files[Main] = main;
        using var ctx = new MemoryModuleContext(files);
        try
        {
            await ctx.RunScriptAsync(main, Main, uniqueModuleID: Main);
        }
        catch (Exception ex)
        {
            var error = (ex as JSException) ?? (ex.InnerException as JSException);
            var name = error?.Error is JSObject o ? o[KeyStrings.GetOrCreate("name")].ToString() : ex.GetType().Name;
            return name + ":" + ctx.Eval("String(globalThis.r)");
        }

        return "no error:" + ctx.Eval("String(globalThis.r)");
    }

    // (1) Live bindings.

    [Fact(Timeout = 600000)]
    public async Task AnImportObservesALaterAssignmentByTheExporter()
        => Assert.Equal("1:2", await Run(
            new() { ["file:///app/dep.js"] = "export let x = 1; export function bump() { x++; }" },
            "import { x, bump } from './dep.js'; const before = x; bump(); globalThis.r = before + ':' + x;"));

    [Fact(Timeout = 600000)]
    public async Task ARenamedAndADefaultImportAreLiveToo()
        => Assert.Equal("a:b:A:B", await Run(
            new()
            {
                ["file:///app/dep.js"] =
                    "let v = 'a'; export { v as renamed }; let d = 'b'; export { d as default };" +
                    " export function set() { v = 'A'; d = 'B'; }",
            },
            "import def, { renamed, set } from './dep.js'; const b = renamed + ':' + def; set();" +
            " globalThis.r = b + ':' + renamed + ':' + def;"));

    [Fact(Timeout = 600000)]
    public async Task AnImportIsAnImmutableBindingInTheImporter()
        => Assert.Equal("TypeError:1", await Run(
            new() { ["file:///app/dep.js"] = "export let x = 1;" },
            "import { x } from './dep.js'; try { x = 2; } catch (e) { globalThis.r = e.name + ':' + x; }"));

    // (2) The module namespace exotic object.

    [Fact(Timeout = 600000)]
    public async Task TheNamespaceIsAModuleExoticObject()
        => Assert.Equal("[object Module]|Module|null|false|a,b,z|true", await Run(
            new() { ["file:///app/dep.js"] = "export const z = 1, b = 2; export function a() {}" },
            "import * as ns from './dep.js';" +
            " globalThis.r = [Object.prototype.toString.call(ns), ns[Symbol.toStringTag], String(Object.getPrototypeOf(ns))," +
            " Object.isExtensible(ns), Object.keys(ns).join(), Object.isSealed(ns)].join('|');"));

    [Fact(Timeout = 600000)]
    public async Task TheNamespaceRefusesWritesDefinitionsAndDeletes()
        => Assert.Equal("TypeError,TypeError,TypeError,TypeError,false,true,1", await Run(
            new() { ["file:///app/dep.js"] = "export let x = 1;" },
            "import * as ns from './dep.js'; const out = [];" +
            " for (const f of [() => { ns.x = 2; }, () => { ns.extra = 1; }, () => { delete ns.x; }," +
            "   () => Object.defineProperty(ns, 'x', { value: 5 })])" +
            "   try { f(); out.push('none'); } catch (e) { out.push(e.name); }" +
            " out.push(Reflect.set(ns, 'x', 3), Reflect.defineProperty(ns, 'x', { value: 1, writable: true, enumerable: true, configurable: false }), ns.x);" +
            " globalThis.r = out.join();"));

    [Fact(Timeout = 600000)]
    public async Task TheNamespaceReadsLiveValues()
        => Assert.Equal("1,2,{\"value\":2,\"writable\":true,\"enumerable\":true,\"configurable\":false}", await Run(
            new() { ["file:///app/dep.js"] = "export let x = 1; export function bump() { x++; }" },
            "import * as ns from './dep.js'; const a = ns.x; ns.bump();" +
            " globalThis.r = [a, ns.x, JSON.stringify(Object.getOwnPropertyDescriptor(ns, 'x'))].join();"));

    [Fact(Timeout = 600000)]
    public async Task TheNamespaceReportsEachOwnKeyOnce()
        => Assert.Equal("a,b,x,Symbol(Symbol.toStringTag)|1|a,b,x,Symbol(Symbol.toStringTag)", await Run(
            new() { ["file:///app/dep.js"] = "export let x = 1, b = 2; export function a() {}" },
            "import * as ns from './dep.js';" +
            " const passThrough = new Proxy(ns, { ownKeys(t) { return Reflect.ownKeys(t); } });" +
            " globalThis.r = [Reflect.ownKeys(ns).map(String).join(), Object.getOwnPropertySymbols(ns).length," +
            " Reflect.ownKeys(passThrough).map(String).join()].join('|');"));

    [Fact(Timeout = 600000)]
    public async Task JsonStringifySerializesTheNamespacesExports()
        => Assert.Equal("{\"default\":3,\"x\":1}", await Run(
            new() { ["file:///app/dep.js"] = "export let x = 1; export default 3;" },
            "import * as ns from './dep.js'; globalThis.r = JSON.stringify(ns);"));

    [Fact(Timeout = 600000)]
    public async Task AProxyOwnKeysTrapMustReportEveryExportOfANamespaceTarget()
        => Assert.Equal("TypeError", await Run(
            new() { ["file:///app/dep.js"] = "export let x = 1, y = 2;" },
            "import * as ns from './dep.js';" +
            " try { Reflect.ownKeys(new Proxy(ns, { ownKeys() { return ['x', Symbol.toStringTag]; } })); globalThis.r = 'none'; }" +
            " catch (e) { globalThis.r = e.name; }"));

    [Fact(Timeout = 600000)]
    public async Task StaticAndDynamicImportsShareOneNamespaceAndOneEvaluation()
        => Assert.Equal("true,true,1", await Run(
            new() { ["file:///app/dep.js"] = "globalThis.runs = (globalThis.runs || 0) + 1; export const x = 1;" },
            "import * as ns from './dep.js'; const [a, b] = await Promise.all([import('./dep.js'), import('./dep.js')]);" +
            " globalThis.r = [a === ns, b === ns, globalThis.runs].join();"));

    [Fact(Timeout = 600000)]
    public async Task ScriptCodeImportsThroughTheSameLoader()
        => Assert.Equal("1,true", await Run(
            new() { ["file:///app/dep.js"] = "export const x = 1;" },
            "import * as ns from './dep.js'; const viaScript = await (0, eval)(\"import('./dep.js')\");" +
            " globalThis.r = [viaScript.x, viaScript === ns].join();"));

    [Fact(Timeout = 600000)]
    public async Task ADynamicImportEvaluatesItsModuleAfterTheCallReturns()
        => Assert.Equal("call,body", await Run(
            new() { ["file:///app/dep.js"] = "globalThis.log.push('body');" },
            "globalThis.log = []; const p = import('./dep.js'); globalThis.log.push('call'); await p;" +
            " globalThis.r = globalThis.log.join();"));

    // (3) Link errors are SyntaxErrors raised before any module body runs.

    [Fact(Timeout = 600000)]
    public async Task AMissingImportIsASyntaxErrorBeforeAnyModuleEvaluates()
        => Assert.Equal("SyntaxError:undefined", await RunExpectingError(
            new() { ["file:///app/dep.js"] = "globalThis.r = 'dep ran'; export const y = 1;" },
            "globalThis.r = 'main ran'; import { nope } from './dep.js';"));

    [Fact(Timeout = 600000)]
    public async Task AnAmbiguousStarImportIsASyntaxError()
        => Assert.Equal("SyntaxError:undefined", await RunExpectingError(
            new()
            {
                ["file:///app/a.js"] = "export const x = 'a';",
                ["file:///app/b.js"] = "export const x = 'b';",
                ["file:///app/both.js"] = "globalThis.r = 'both ran'; export * from './a.js'; export * from './b.js';",
            },
            "import { x } from './both.js';"));

    [Fact(Timeout = 600000)]
    public async Task AnAmbiguousStarNameIsOmittedFromTheNamespace()
        => Assert.Equal("y", await Run(
            new()
            {
                ["file:///app/a.js"] = "export const x = 'a', y = 1;",
                ["file:///app/b.js"] = "export const x = 'b';",
                ["file:///app/both.js"] = "export * from './a.js'; export * from './b.js';",
            },
            "import * as ns from './both.js'; globalThis.r = Object.keys(ns).join();"));

    // (4) Cycles and the cross-module TDZ.

    [Fact(Timeout = 600000)]
    public async Task ACycleSeesHoistedFunctionsAndTheTemporalDeadZone()
        => Assert.Equal("ReferenceError|fn|a1", await Run(
            new()
            {
                ["file:///app/a.js"] =
                    "import { early, later } from './b.js'; export let a = 'a1'; export function f() { return 'fn'; }" +
                    " globalThis.r = early + '|' + later();",
                ["file:///app/b.js"] =
                    "import { a, f } from './a.js'; let seen; try { seen = a; } catch (e) { seen = e.name; }" +
                    " export const early = seen + '|' + f(); export function later() { return a; }",
            },
            "import './a.js';"));

    // (5) Top-level await ordering and cached evaluation errors.

    [Fact(Timeout = 600000)]
    public async Task ASiblingOfAnAsyncModuleIsNotBlockedByIt()
        => Assert.Equal("slow:start,quick,slow:end,main", await Run(
            new()
            {
                ["file:///app/log.js"] = "export const log = [];",
                ["file:///app/slow.js"] =
                    "import { log } from './log.js'; log.push('slow:start'); await 0; await 0; log.push('slow:end');",
                ["file:///app/quick.js"] = "import { log } from './log.js'; log.push('quick');",
            },
            "import { log } from './log.js'; import './slow.js'; import './quick.js'; log.push('main');" +
            " globalThis.r = log.join();"));

    [Fact(Timeout = 600000)]
    public async Task AnImporterSeesAnAsyncDependencysFinalValue()
        => Assert.Equal("after", await Run(
            new() { ["file:///app/dep.js"] = "export let v = 'before'; await null; v = 'after';" },
            "import { v } from './dep.js'; globalThis.r = v;"));

    [Fact(Timeout = 600000)]
    public async Task AnAsyncRejectionRejectsTheModulesOwnImportBeforeItsParents()
        => Assert.Equal("a,b", await Run(
            new()
            {
                ["file:///app/a.js"] = "await new Promise(r => { globalThis.release = r; }); throw new Error('x');",
                ["file:///app/b.js"] = "import './a.js';",
            },
            // ES2026 AsyncModuleExecutionRejected: the module's own top-level capability is
            // rejected before its async parents, leaf to root (test262
            // module-code/top-level-await/rejection-order.js).
            "const order = [];" +
            " const pa = import('./a.js').catch(() => order.push('a'));" +
            " const pb = import('./b.js').catch(() => order.push('b'));" +
            " for (let i = 0; i < 10000 && !globalThis.release; i++) await null;" +
            " for (let i = 0; i < 100; i++) await null;" +
            " globalThis.release(); await Promise.all([pa, pb]); globalThis.r = order.join();"));

    [Fact(Timeout = 600000)]
    public async Task AnErroredModuleRejectsEveryLaterImportWithTheSameErrorAndNeverRunsAgain()
        => Assert.Equal("1|true|true", await Run(
            new()
            {
                ["file:///app/bad.js"] =
                    "globalThis.runs = (globalThis.runs || 0) + 1; throw new Error('boom');",
            },
            "let e1; try { await import('./bad.js'); } catch (e) { e1 = e; }" +
            " const e2 = await import('./bad.js').then(() => 'resolved', e => e);" +
            " globalThis.r = [globalThis.runs, e1 === e2, e1 instanceof Error].join('|');"));

    // (6) Module code.

    [Fact(Timeout = 600000)]
    public async Task ModuleCodeIsStrictWithAnUndefinedThis()
        => Assert.Equal("undefined|ReferenceError|undefined", await Run(
            new(),
            "let assigned; try { undeclaredName = 1; assigned = 'none'; } catch (e) { assigned = e.name; }" +
            " globalThis.r = [typeof this, assigned, (function () { return typeof this; })()].join('|');"));

    [Fact(Timeout = 600000)]
    public async Task TopLevelDeclarationsDoNotReachTheGlobalObject()
        => Assert.Equal("undefined,undefined,1,function", await Run(
            new(),
            "var count = 1; function helper() {}" +
            " globalThis.r = [typeof globalThis.count, typeof globalThis.helper, count, typeof helper].join();"));

    [Fact(Timeout = 600000)]
    public async Task ModuleCodeSeesNoCommonJsBindings()
        => Assert.Equal("undefined,undefined,undefined,undefined,undefined", await Run(
            new(),
            "globalThis.r = [typeof module, typeof exports, typeof require, typeof __dirname, typeof __filename].join();"));

    // (7) Declaration forms of import and export.

    [Fact(Timeout = 600000)]
    public async Task ASideEffectOnlyImportEvaluatesTheModule()
        => Assert.Equal("side", await Run(
            new() { ["file:///app/side.js"] = "globalThis.r = 'side';" },
            "import './side.js';"));

    [Fact(Timeout = 600000)]
    public async Task ADefaultFunctionDeclarationMayBeFollowedByOtherExports()
        => Assert.Equal("f,1,function", await Run(
            new() { ["file:///app/dep.js"] = "export default function f() {}\nexport const y = 1;" },
            "import d, { y } from './dep.js'; globalThis.r = [d.name, y, typeof d].join();"));

    [Fact(Timeout = 600000)]
    public async Task AnExportedFunctionMayBeReExportedUnderAnotherName()
        => Assert.Equal("true", await Run(
            new() { ["file:///app/dep.js"] = "export function f() {}\nexport { f as g };" },
            "import { f, g } from './dep.js'; globalThis.r = String(f === g);"));

    [Fact(Timeout = 600000)]
    public async Task AnAnonymousDefaultExportIsNamedDefault()
        => Assert.Equal("default,default,default", await Run(
            new()
            {
                ["file:///app/f.js"] = "export default function () {}",
                ["file:///app/c.js"] = "export default class {}",
                ["file:///app/e.js"] = "export default (function () {});",
            },
            "import f from './f.js'; import c from './c.js'; import e from './e.js';" +
            " globalThis.r = [f.name, c.name, e.name].join();"));

    // (8) Every specifier reaches Resolve.

    private sealed class CountingContext(bool registerBuiltInModules) : JSModuleContext(null, false, registerBuiltInModules)
    {
        public readonly List<string> Resolved = [];

        protected override string Resolve(string dirPath, string relativePath)
        {
            Resolved.Add(relativePath);
            return base.Resolve(dirPath, relativePath);
        }
    }

    [Fact(Timeout = 600000)]
    public async Task ATopLevelAwaitUsingMakesTheModuleAsyncAndAwaitsForANullResource()
        // DisposeResources performs Await(undefined) for `await using b = null` before disposing the
        // sync resource `a`, so one job (t1) runs first. The module used to be compiled as a sync
        // module (an `await using` did not mark the program async) that disposed without awaiting.
        => Assert.Equal("body,t1,a,after", await Run(
            new(),
            "globalThis.log = []; Promise.resolve().then(() => log.push('t1'));" +
            "{ using a = { [Symbol.dispose]() { log.push('a'); } }; await using b = null; log.push('body'); }" +
            "log.push('after'); globalThis.r = log.join(',');"));

    [Theory(Timeout = 600000)]
    [InlineData(true, "function")]
    [InlineData(false, "TypeError")]
    public async Task ARegisteredModuleIsReachedThroughResolveAndOnlyWhenRegistered(bool registerBuiltInModules, string expected)
    {
        using var ctx = new CountingContext(registerBuiltInModules);
        await ctx.RunScriptAsync(
            "globalThis.r = await import('module').then(m => typeof m.default, e => e.name);",
            Environment.CurrentDirectory);

        Assert.Equal(expected, ctx.Eval("String(globalThis.r)").ToString());
        Assert.Equal(new[] { "module" }, ctx.Resolved);
    }

    [Fact(Timeout = 600000)]
    public async Task EverySpecifierIsResolvedThroughTheHost()
    {
        List<string>? resolved = null;
        var error = await RunExpectingError(new(), "import 'module';");
        Assert.StartsWith("TypeError", error);

        await Run(
            new() { ["file:///app/dep.js"] = "export const x = 1;" },
            "import { x } from './dep.js'; const ns = await import('file:///app/dep.js'); globalThis.r = ns.x + x;",
            ctx => resolved = ctx.Resolved);

        Assert.Equal(new[] { "./dep.js", "file:///app/dep.js" }, resolved);
    }
}
