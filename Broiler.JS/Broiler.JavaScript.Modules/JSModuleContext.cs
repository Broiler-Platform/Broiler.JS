using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.BuiltIns.Function;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.Modules;

public delegate Task JSModuleDelegate(JSModule module);

/// <summary>
/// Enables Modules, both CommonJS and ES Modules
/// </summary>
public class JSModuleContext : JSContext
{
    internal readonly JSObject ModulePrototype;
    internal readonly JSFunction Module;

    public JSModuleContext(SynchronizationContext ctx = null, bool enableClrIntegration = true) : base(ctx ?? new SynchronizationContext())
    {
        // this.CreateSharedObject(KeyStrings.assert, typeof(JSAssert), true);
        this[KeyStrings.assert] = JSAssert.CreateClass(this, false);

        Module = JSModule.CreateClass(this, false); // this.Create<JSModule>(KeyStrings.Module, null, false);
        ModulePrototype = Module.prototype;

        if (enableClrIntegration && JSEngine.ClrModuleProvider != null)
            moduleCache[ModuleCache.clr] = new JSModule(this, JSEngine.ClrModuleProvider(), "clr");

        moduleCache[ModuleCache.module] = new JSModule(this, Module, "module");

        this[KeyStrings.globalThis] = this;
        this[KeyStrings.global] = this;
    }


    /// <summary>
    /// Pass Exports as Module with unique name
    /// After register module can get in script
    /// <example>
    ///  //in js script
    /// import module from "module_name_that_used_in_name_arg";
    /// import {prop in export object} from "module_name";
    /// const module = require("module_name_that_used_in_name_arg");
    /// const {some_prop} = require("module_name_that_used_in_name_arg");
    /// </example>
    /// </summary>
    /// <param name="name">Unique module name</param>
    /// <param name="exports">JSObject, that you import by import or require</param>
    public void RegisterModule(in KeyString name, JSObject exports)
    {
        var n = name.ToString();
        moduleCache.GetOrCreate(name.Value, () => new JSModule(this, exports, n));
    }

    /// <summary>
    /// Modules are isolated by Context and are identified by Id.
    /// 
    /// Specially in server environment with multiple context, module names
    /// are identified by unique id present in ModuleName.
    /// </summary>
    readonly ModuleCache moduleCache = ModuleCache.Create();

    [Browsable(false)]
    public IEnumerable<JSModule> All => moduleCache.All;

    private string[] paths;

    protected string[] extensions = [".js"];

    /// <summary>
    /// Resolves an import specifier to a module key. The default is a filesystem/node_modules resolution.
    /// A host that loads modules over another scheme (e.g. a browser resolving URLs against a base and
    /// fetching them) overrides this to return its own key form (typically an absolute URL).
    /// </summary>
    protected virtual string Resolve(string dirPath, string relativePath)
    {
        bool Exists(string folder, string file, out string path)
        {
            string fullName = Path.Combine(folder, file);
            if (!file.StartsWith("."))
            {
                if (Directory.Exists(fullName))
                {
                    var pkgJson = fullName + "/package.json";

                    if (File.Exists(pkgJson))
                    {
                        var json = File.ReadAllText(pkgJson);
                        var pkg = JsonNode.Parse(json) as JsonObject;

                        if (pkg.TryGetPropertyValue("main", out var token))
                        {
                            var v = token.GetValue<string>();
                            path = Path.Combine(fullName, v);

                            if (File.Exists(path))
                                return true;

                            foreach (var ext in extensions)
                            {
                                var np = path + ext;
                                if (File.Exists(np))
                                {
                                    path = np;
                                    return true;
                                }
                            }

                            throw new FileNotFoundException(path);
                        }
                    }
                }
            }

            if (File.Exists(fullName))
            {
                path = fullName;
                return true;
            }

            path = null;
            return false;
        }

        foreach (var ext in extensions)
        {
            if (relativePath.StartsWith("."))
            {
                if (Exists(dirPath, relativePath, out var path))
                    return path;

                if (Exists(dirPath, relativePath + ext, out path))
                    return path;

                continue;
            }

            foreach (var folder in paths)
            {
                if (Exists(folder, relativePath, out var path))
                    return path;

                if (Exists(folder, relativePath + ext, out path))
                    return path;

                if (Exists(folder, relativePath + "/index" + ext, out path))
                    return path;

                // check if package.json exists...
            }
        }

        return null;
    }

    void UpdatePaths(string[] paths = null)
    {
        if (paths != null)
        {
            var np = new string[paths.Length + 2];
            np[0] = CurrentPath;
            np[1] = CurrentPath + "/node_modules";

            Array.Copy(paths, 0, np, 2, paths.Length);
            paths = np;
        }

        this.paths = paths ??
        [
            CurrentPath,
            CurrentPath + "/node_modules",
            // system npm paths...
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/broilerjs/node_modules"
        ];
    }

    /// <summary>
    /// Converts a .NET <see cref="Task{JSValue}"/> into a JS promise using the engine-native promise
    /// factory rather than <c>JSEngine.ClrInterop.Marshal</c>. The CLR interop is only the full
    /// <see cref="DefaultClrInterop"/> when the optional <c>Broiler.JavaScript.Clr</c> assembly is loaded;
    /// otherwise it is the fallback, whose <c>Marshal</c> returns <c>undefined</c> for a <see cref="Task"/>.
    /// Modules does not reference Clr, so routing <c>import()</c>/<c>require()</c> module tasks through the
    /// interop made an imported module resolve to <c>undefined</c> whenever Clr was absent — the exports
    /// bound but their value was lost. This factory (the same one <see cref="DefaultClrInterop"/> uses for a
    /// <c>Task&lt;JSValue&gt;</c>) is populated by the always-referenced BuiltIns assembly, so it works
    /// regardless of Clr.
    /// </summary>
    private static JSValue TaskToPromise(Task<JSValue> task) => JSValue.CreatePromiseFromTask(task);

    private static IDisposable CreateSynchronizationContext()
    {
        if (SynchronizationContext.Current == null)
        {
            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
            return new DisposableAction(() => { SynchronizationContext.SetSynchronizationContext(null); });
        }

        return DisposableAction.Empty;
    }

    public Task<JSValue> RunAsync(string folder, string relativeFile, string[] paths = null)
        // As with RunScriptAsync: pump the whole module init on one AsyncPump loop so top-level-await
        // module bodies run to completion instead of stalling on the default un-pumped context.
        => Task.Run(() => AsyncPump.Run(() => RunCoreAsync(folder, relativeFile, paths)));

    private async Task<JSValue> RunCoreAsync(string folder, string relativeFile, string[] paths)
    {
        CurrentPath = folder;
        UpdatePaths(paths);

        var filePath = Resolve(folder, relativeFile.StartsWith(".") ? relativeFile : ("./" + relativeFile)) ?? throw new FileNotFoundException($"{relativeFile} not found");
        var r = await LoadModuleAsync(null, filePath);
        var w = WaitTask;
        if (w != null)
            await w;

        return r;
    }

    /// <summary>
    /// Run JavaScript module from string
    /// </summary>
    /// <param name="script">string of code</param>
    /// <param name="moduleFolder">base folder for searching modules in import function</param>
    /// <param name="paths"></param>
    /// <param name="uniqueModuleID">Module ID if you want get this module later (in <see cref="ImportModule"/> or import in js)</param>
    /// <returns>Module as JSObject</returns>
    /// <exception cref="JSException"></exception>
    public Task<JSValue> RunScriptAsync(string script, string moduleFolder, string[] paths = null, string uniqueModuleID = null)
        // Run the whole module init under one pumped AsyncPump loop on a worker thread — exactly like
        // JSContext.ExecuteAsync. A module body compiled with top-level await suspends its async
        // continuation onto the ambient SynchronizationContext; on the default (un-pumped) context those
        // continuations never drain, so the body stalls at its first await (which every static import is).
        // The async-local current context flows across the Task.Run boundary, so engine state resolves on
        // the worker thread.
        => Task.Run(() => AsyncPump.Run(() => RunScriptCoreAsync(script, moduleFolder, paths, uniqueModuleID)));

    private async Task<JSValue> RunScriptCoreAsync(string script, string moduleFolder, string[] paths, string uniqueModuleID)
    {
        CurrentPath = moduleFolder;
        UpdatePaths(paths);
        uniqueModuleID ??= Guid.NewGuid().ToString("N") + ".js";

        var newModule = new JSModule(this, uniqueModuleID, script);
        var dirPath = moduleFolder;

        newModule.Import = new JSFunction((in Arguments a) =>
        {
            var name = a[0];
            if (!name.IsString)
                throw NewTypeError("import method's parameter must be a string");

            return TaskToPromise(LoadModuleAsync(dirPath, name.StringValue));
        });

        newModule.Require = new JSFunction((in Arguments a) =>
        {
            var name = a[0];
            if (!name.IsString)
                throw NewTypeError("require method's parameter must be a string");

            var result = LoadModuleAsync(dirPath, name.StringValue, esModule: false);
            return AsyncPump.Run(() => result);
        });

        newModule.Compile = new JSFunction((in Arguments a) =>
        {
            var task = CompileModuleAsync(newModule);
            return JSEngine.ClrInterop.Marshal(task);
        });
        // Prefer the direct (non-marshalled) compile path so a top-level-await body runs to completion on
        // this pumped loop instead of settling at its first suspension behind a Task→promise→Task marshal.
        newModule.CompileDirect = () => CompileModuleAsync(newModule);

        await newModule.InitAsync();

        var w = WaitTask;
        if (w != null)
            await w;

        return newModule.Exports;
    }

    public async static Task<JSValue> RunExportsAsync(string folder, string relativeFile, string exportedFunctionName, Arguments a, string[] paths = null)
    {
        using var m = new JSModuleContext();
        m.CurrentPath = folder;
        m.UpdatePaths(paths);

        var filePath = m.Resolve(folder, relativeFile.StartsWith(".") ? relativeFile : ("./" + relativeFile));
        if (filePath == null)
            throw new FileNotFoundException($"{filePath} not found");
        
        var main = await m.LoadModuleAsync(m.CurrentPath, filePath);
        var exported = main[exportedFunctionName];
        if (exported.IsUndefined)
            throw new KeyNotFoundException($"{exportedFunctionName} not found on the module");
        
        var rv = exported.InvokeFunction(a);
        if (rv is IJSPromise promise)
            return await promise.Task;

        if (m.WaitTask != null)
            await m.WaitTask;

        return rv;
    }

    public string CurrentPath { get; set; }

    public JSModule Main { get; set; }

    /// <summary>
    /// Whether a resolved module key names a JSON module. JSON is the one format this context serves
    /// to <c>import</c> and to <c>require</c> in two different shapes, because the two specifications
    /// disagree about what a JSON file exports.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ES2025 gives a JSON module exactly one export, <c>default</c>, holding the parsed value, and no
    /// named exports. CommonJS <c>require('./x.json')</c> hands back the parsed value itself. Both are
    /// right for their own caller and they cannot be the same object, so the module *stores* the ES
    /// namespace — <c>{ default: value }</c> — and the CommonJS view unwraps it.
    /// </para>
    /// <para>
    /// Before this, one wrapper served both: <c>module.exports = &lt;json&gt;</c>. <c>require</c> was
    /// correct and every <c>import</c> was not — a default import read <c>.default</c> off the parsed
    /// value and found nothing, so <c>import d from './data.json'</c> was <c>undefined</c> for every
    /// JSON shape, which made JSON modules unusable.
    /// </para>
    /// <para>
    /// <b>Deviation, deliberate:</b> <c>import { a } from './x.json'</c> used to read <c>a</c> off the
    /// parsed object and now reads <c>undefined</c>. Per spec it is a link error — a JSON module has no
    /// named exports — and raising that needs whole-module link analysis this engine does not do, so
    /// the nearer of the two available answers is taken. Browsers and Node both reject the form, so no
    /// portable code writes it.
    /// </para>
    /// </remarks>
    private static bool IsJsonModule(string moduleKey) =>
        moduleKey != null && moduleKey.EndsWith(".json", StringComparison.Ordinal);

    /// <summary>
    /// The value a loaded module presents to its importer: the module's own exports, except that a
    /// JSON module's stored ES namespace is unwrapped to the parsed value for a CommonJS
    /// <c>require</c>.
    /// </summary>
    private static JSValue ViewOf(JSModule module, string moduleKey, bool esModule) =>
        esModule || !IsJsonModule(moduleKey)
            ? module.Exports
            : module.Exports[KeyStrings.@default];

    /// <param name="esModule">
    /// Whether the caller is <c>import</c> (the ES view) rather than <c>require</c> (the CommonJS
    /// view). It changes nothing except the shape a JSON module is presented in — see
    /// <see cref="IsJsonModule"/>.
    /// </param>
    protected virtual async Task<JSValue> LoadModuleAsync(string currentPath, string name, bool esModule = true)
    {
        var relativePath = name;

        // fetch system modules 
        if (moduleCache.TryGetValue(relativePath, out var m))
            return ViewOf(m, relativePath, esModule);

        // resolve full name..
        var fullPath = Resolve(currentPath, relativePath) ?? throw NewTypeError($"{relativePath} module not found");
        m = moduleCache.GetOrCreate(fullPath, () =>
        {
            var newModule = new JSModule(this, fullPath);
            var dirPath = GetModuleDirectory(fullPath);

            newModule.Import = new JSFunction((in Arguments a) =>
            {
                var name = a[0];
                if (!name.IsString)
                    throw NewTypeError("import method's parameter must be a string");

                return TaskToPromise(LoadModuleAsync(dirPath, name.StringValue));
            });

            newModule.Require = new JSFunction((in Arguments a) =>
            {
                var name = a[0];
                if (!name.IsString)
                    throw NewTypeError("require method's parameter must be a string");

                var result = LoadModuleAsync(dirPath, name.StringValue, esModule: false);
                return AsyncPump.Run(() => result);
            });

            newModule.Compile = new JSFunction((in Arguments a) =>
            {
                var task = CompileModuleAsync(newModule);
                return JSEngine.ClrInterop.Marshal(task);
            });
            // Direct compile so a nested/transitive module's top-level-await body also runs to completion
            // on the pumped loop rather than stalling behind the Task→promise→Task marshal.
            newModule.CompileDirect = () => CompileModuleAsync(newModule);

            return newModule;
        });

        await m.InitAsync();
        return ViewOf(m, fullPath, esModule);
    }

    /// <summary>
    /// Returns the base directory a resolved module's own relative imports resolve against. The default is
    /// the filesystem directory of <paramref name="fullPath"/>; a URL-loading host overrides this to return
    /// the module URL's base so nested relative imports resolve as URLs rather than being mangled by
    /// filesystem path semantics.
    /// </summary>
    protected virtual string GetModuleDirectory(string fullPath) => Path.GetDirectoryName(fullPath);

    /// <summary>
    /// The absolute URL of a resolved module key — what that module's <c>import.meta.url</c> reports.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Only the host knows what its keys are. The default handles the two forms this context itself
    /// produces: a key that is already an absolute URI (what a URL-loading host resolves to) is
    /// reported verbatim, and a filesystem path is converted to a <c>file://</c> URL, because
    /// <c>import.meta.url</c> is specified as a URL and a bare path is not one. A host with keys of
    /// another shape overrides this.
    /// </para>
    /// <para>
    /// Returning <see langword="null"/> is meaningful: the module's <c>import.meta</c> then carries
    /// no <c>url</c> at all rather than an invented one, so a module whose key cannot be expressed as
    /// a URL reads <c>undefined</c> — which a script can detect — instead of a plausible lie.
    /// </para>
    /// </remarks>
    protected internal virtual string GetModuleUrl(string moduleKey)
    {
        if (string.IsNullOrEmpty(moduleKey))
            return null;

        if (Uri.TryCreate(moduleKey, UriKind.Absolute, out var absolute))
            return absolute.AbsoluteUri;

        try
        {
            return new Uri(Path.GetFullPath(moduleKey)).AbsoluteUri;
        }
        catch (ArgumentException) { return null; }
        catch (NotSupportedException) { return null; }
        catch (PathTooLongException) { return null; }
    }

    /// <summary>
    /// Reads the source text of a resolved module whose <see cref="JSModule.Code"/> has not been supplied.
    /// The default reads the file at <see cref="JSModule.filePath"/>; a host that fetches modules over
    /// another transport (e.g. HTTP/data URLs under a content-security policy) overrides this.
    /// </summary>
    protected virtual async Task<string> ReadModuleSourceAsync(JSModule module)
    {
        using var reader = new StreamReader(module.filePath, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }

    internal protected virtual async Task CompileModuleAsync(JSModule module)
    {
        // Console.WriteLine($"{DateTime.Now} - Compiling module {module.filePath}");
        var filePath = module.filePath;

        // if this is a json file... then pad with module.exports =
        if (module.Code == null)
            module.Code = await ReadModuleSourceAsync(module);

        var code = module.Code;

        // A JSON module's ONE export is `default` (ES2025 16.2.1.6.1: a JSON module record has
        // exactly one export, `default`, and no named exports), so what is stored is the namespace
        // rather than the parsed value. The CommonJS view unwraps it in LoadModuleAsync; see
        // IsJsonModule for why the two views have to differ at all.
        //
        // Storing the namespace rather than the value is also what lets `null` through: JSModule's
        // Exports setter refuses null or undefined, so the old `module.exports = null;` wrapper made
        // a file whose whole content is `null` throw on load rather than import as `null`.
        if (IsJsonModule(filePath))
            code = $"module.exports = {{ default: ({code}) }};";

        // var factory = FastEval(code, filePath);
        JSFunctionDelegate factory;
        using (CoreScript.AllowTopLevelAwaitScope())
        {
            factory = CoreScript.Compile(code, module.filePath,
            [
                "exports",
                "require",
                "module",
                "import",
                "__fileame",
                "__dirname"
                // Parsed with the module goal symbol, which is what makes `await` a reserved word
                // here. A .json file is wrapped as CommonJS above and is not module source, so it
                // keeps the script goal.
            ], codeCache: CodeCache, isModule: !IsJsonModule(filePath));
        }

        if (factory(new Arguments(module,
        [
            module.Exports,
            module.Require,
            module,
            module.Import,
            module.Id,
            CreateString(module.dirPath)
        ])) is IJSPromise result)
        {
            await result.Task;
        }
    }
}