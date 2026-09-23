using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
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
/// Hosts ECMAScript modules, and CommonJS modules for code the host loads as CommonJS.
/// </summary>
/// <remarks>
/// <para>
/// An ECMAScript module graph is loaded, linked and evaluated as ES2024 16.2.1.5 specifies. Every
/// static request of every module is resolved through <see cref="Resolve"/> and loaded before
/// anything links; linking resolves every import and indirect export, so a missing or ambiguous
/// name is a SyntaxError before any module body runs; each module's environment is then
/// instantiated — its function declarations exist, its imports are live bindings to the
/// exporters' own bindings — and only then are the bodies evaluated, in post-order, with
/// top-level <c>await</c> handled by the asynchronous evaluation algorithm (a sibling of an
/// asynchronous module is not blocked by it) and an evaluation error cached on every module it
/// reached.
/// </para>
/// <para>
/// Module code sees only its own module environment. The CommonJS bindings — <c>module</c>,
/// <c>exports</c>, <c>require</c>, <c>__filename</c>, <c>__dirname</c> — exist only in a
/// CommonJS module: one loaded by <c>require</c>, or one whose key <see cref="IsCommonJsModule"/>
/// says is CommonJS (by default a <c>.cjs</c> file). Importing a CommonJS module gives a namespace
/// whose one export, <c>default</c>, is its <c>module.exports</c>.
/// </para>
/// <para>
/// Identity: a module is identified by the key <see cref="Resolve"/> returns, and nothing else. A
/// specifier is never looked up as a key: each (referrer, specifier, type) is resolved through
/// <see cref="Resolve"/> once and remembered by that referrer, and a failed resolution or load is
/// not remembered, so a later import asks the host again. The modules the context registers
/// itself (<c>module</c>, <c>clr</c>, and any passed to <see cref="RegisterModule"/>) are reached
/// the same way: the default <see cref="Resolve"/> answers their names, and a host that overrides
/// <see cref="Resolve"/> decides whether they are reachable at all.
/// </para>
/// </remarks>
public class JSModuleContext : JSContext
{
    internal readonly JSObject ModulePrototype;
    internal readonly JSFunction Module;

    /// <summary>Modules registered under a name rather than loaded from a key.</summary>
    private readonly Dictionary<string, JSModule> registeredModules = new(StringComparer.Ordinal);

    /// <summary>Every loaded module, by the key <see cref="Resolve"/> returned for it.</summary>
    private readonly Dictionary<string, JSModule> modules = new(StringComparer.Ordinal);

    /// <summary>[[ModuleAsyncEvaluationCount]] (IncrementModuleAsyncEvaluationCount).</summary>
    private long asyncEvaluationCount;

    private static readonly string[] ModuleParameters = ["import", "#module"];
    private static readonly string[] CommonJsParameters = ["exports", "require", "module", "__filename", "__dirname", "import"];

    public JSModuleContext(SynchronizationContext ctx = null, bool enableClrIntegration = true)
        : this(ctx, enableClrIntegration, registerBuiltInModules: true)
    {
    }

    /// <param name="ctx">The synchronization context JavaScript jobs run on.</param>
    /// <param name="enableClrIntegration">Whether the <c>clr</c> module is registered.</param>
    /// <param name="registerBuiltInModules">Whether the context registers its own <c>module</c>
    /// (and <c>clr</c>) modules at all. A host that wants every module to come from its own
    /// resolver passes false.</param>
    public JSModuleContext(SynchronizationContext ctx, bool enableClrIntegration, bool registerBuiltInModules) : base(ctx ?? new SynchronizationContext())
    {
        this[KeyStrings.assert] = JSAssert.CreateClass(this, false);

        Module = JSModule.CreateClass(this, false);
        ModulePrototype = Module.prototype;

        if (registerBuiltInModules)
        {
            if (enableClrIntegration && JSEngine.ClrModuleProvider != null)
                registeredModules["clr"] = new JSModule(this, JSEngine.ClrModuleProvider(), "clr");

            registeredModules["module"] = new JSModule(this, Module, "module");
        }

        this[KeyStrings.globalThis] = this;
        this[KeyStrings.global] = this;

        // `import()` in script code — a classic script, or eval code — resolves against the
        // context's current path through the same Resolve, map and linker as a module's own.
        // The compiler reaches it through the realm's non-enumerable `import` loader.
        FastAddValue(
            KeyStrings.GetOrCreate("import"),
            CreateFunction((in Arguments a) => DynamicImport(null, a)),
            JSPropertyAttributes.ConfigurableValue);
    }

    /// <summary>
    /// Registers a host module under <paramref name="name"/>. The default <see cref="Resolve"/>
    /// answers that name with itself, so <c>import m from 'name'</c> reaches it, as does
    /// <c>require('name')</c>; an import sees each own property of <paramref name="exports"/> as
    /// a named export and the object itself as <c>default</c>.
    /// </summary>
    public void RegisterModule(in KeyString name, JSObject exports)
    {
        var n = name.ToString();
        if (!registeredModules.ContainsKey(n))
            registeredModules[n] = new JSModule(this, exports, n);
    }

    [Browsable(false)]
    public IEnumerable<JSModule> All => registeredModules.Values.Concat(modules.Values);

    private string[] paths;

    protected string[] extensions = [".js"];

    /// <summary>
    /// Resolves an import specifier to a module key. The default answers a registered module's
    /// name with that name, and otherwise resolves a filesystem/node_modules path. A host that loads
    /// modules over another scheme (e.g. a browser resolving URLs against a base and fetching them)
    /// overrides this to return its own key form (typically an absolute URL). Returning null means
    /// the module does not exist.
    /// </summary>
    protected virtual string Resolve(string dirPath, string relativePath)
    {
        if (registeredModules.ContainsKey(relativePath))
            return relativePath;

        bool Exists(string folder, string file, out string path)
        {
            // The key must be canonical: it is the module's identity, and `./a.js` resolved from
            // two directories that name the same folder differently must give one module.
            string fullName = Path.GetFullPath(Path.Combine(folder, file));
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
                if (dirPath == null)
                    continue;

                if (Exists(dirPath, relativePath, out var path))
                    return path;

                if (Exists(dirPath, relativePath + ext, out path))
                    return path;

                continue;
            }

            foreach (var folder in paths ?? [])
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

    /// <summary>
    /// Whether the module with <paramref name="moduleKey"/> is a CommonJS module when it is
    /// imported. The default is a <c>.cjs</c> key. A module loaded by <c>require</c> is always
    /// CommonJS; everything else is an ECMAScript module (or JSON, by its key).
    /// </summary>
    protected virtual bool IsCommonJsModule(string moduleKey)
        => moduleKey != null && moduleKey.EndsWith(".cjs", StringComparison.OrdinalIgnoreCase);

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
    /// Loads, links and evaluates the module file <paramref name="relativeFile"/> resolved against
    /// <paramref name="folder"/>, and returns its namespace object once it has evaluated and the
    /// work it queued has run.
    /// </summary>
    public Task<JSValue> RunAsync(string folder, string relativeFile, string[] paths = null)
        // Pump the whole load, link and evaluation on one AsyncPump loop, so the jobs a module body
        // queues — its top-level awaits above all — run on the loop the body runs on.
        => Task.Run(() => AsyncPump.Run(() => RunCoreAsync(folder, relativeFile, paths)));

    private async Task<JSValue> RunCoreAsync(string folder, string relativeFile, string[] paths)
    {
        CurrentPath = folder;
        UpdatePaths(paths);

        var filePath = Resolve(folder, relativeFile.StartsWith(".") ? relativeFile : ("./" + relativeFile)) ?? throw new FileNotFoundException($"{relativeFile} not found");
        var module = GetOrCreateModule(filePath, commonJs: IsCommonJsModule(filePath));
        var r = await LoadLinkAndEvaluateAsync(module);
        await DrainAsync();
        var w = WaitTask;
        if (w != null)
            await w;

        return r;
    }

    /// <summary>
    /// Runs <paramref name="script"/> as an ECMAScript module.
    /// </summary>
    /// <param name="script">The module's source text.</param>
    /// <param name="moduleFolder">The base its relative imports resolve against.</param>
    /// <param name="paths">Extra folders the default resolver searches for bare specifiers.</param>
    /// <param name="uniqueModuleID">The module's key. When no module with that key has been loaded
    /// yet, the module is registered under it, so another module that imports the key reaches this
    /// one rather than loading it again.</param>
    /// <returns>The module's namespace object, once the module has evaluated.</returns>
    /// <exception cref="JSException">The module failed to load, link or evaluate.</exception>
    public Task<JSValue> RunScriptAsync(string script, string moduleFolder, string[] paths = null, string uniqueModuleID = null)
        => Task.Run(() => AsyncPump.Run(() => RunScriptCoreAsync(script, moduleFolder, paths, uniqueModuleID)));

    private async Task<JSValue> RunScriptCoreAsync(string script, string moduleFolder, string[] paths, string uniqueModuleID)
    {
        CurrentPath = moduleFolder;
        UpdatePaths(paths);
        uniqueModuleID ??= Guid.NewGuid().ToString("N") + ".js";

        var module = new JSModule(this, uniqueModuleID, script) { BaseDirectory = moduleFolder };
        if (!modules.ContainsKey(uniqueModuleID) && !registeredModules.ContainsKey(uniqueModuleID))
            modules[uniqueModuleID] = module;

        var result = await LoadLinkAndEvaluateAsync(module);
        await DrainAsync();

        var w = WaitTask;
        if (w != null)
            await w;

        return result;
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

    /// <summary>The one module type this host implements, and the value <c>with { type: … }</c>
    /// may name.</summary>
    private const string JsonModuleType = "json";

    /// <summary>
    /// Rejects a resolved module whose type is not the one an import attribute asserted.
    /// </summary>
    /// <remarks>
    /// The web checks the assertion against the response's MIME type; this host has no MIME types,
    /// so it checks the resolved module key, which is the same fact by the only means available —
    /// the key is what decided the module would be parsed as JSON in the first place, so the check
    /// and the parse cannot disagree.
    /// <para>
    /// The converse is deliberately <b>not</b> enforced: a <c>.json</c> module imported with no
    /// attribute at all loads. A browser rejects that, because there the attribute defends against a
    /// server returning JSON where script was expected — a mismatch that cannot arise here, since
    /// the key resolved locally is itself the type. This context also serves <c>require</c>, where
    /// no attribute exists at all, so demanding one from <c>import</c> would make the two halves of
    /// the same host disagree about the same file. Stated as a divergence rather than left to be
    /// discovered.
    /// </para>
    /// </remarks>
    private void CheckModuleType(string moduleKey, string requiredType)
    {
        if (requiredType == JsonModuleType && !IsJsonModule(moduleKey))
        {
            throw NewTypeError(
                $"Failed to load module \"{moduleKey}\": it was imported with " +
                "type: \"json\" but it is not a JSON module.");
        }
    }

    /// <summary>
    /// Whether a resolved module key names a JSON module. A JSON module's one export (ES2025
    /// 16.2.1.7) is <c>default</c>, the parsed value; <c>require</c> hands back the parsed value
    /// itself.
    /// </summary>
    private static bool IsJsonModule(string moduleKey) =>
        moduleKey != null && moduleKey.EndsWith(".json", StringComparison.Ordinal);

    /// <summary>
    /// The module type an import attribute clause asked for — <c>"json"</c>, or null when no
    /// <c>type</c> was asserted — after validating the clause itself.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A dynamic <c>import(specifier, options)</c> passes its runtime options object in
    /// <paramref name="options"/>; a static declaration's <c>with { … }</c> clause was validated by
    /// the parser and reaches the linker as its module request.
    /// </para>
    /// <para>
    /// The clause is checked <b>before the specifier is resolved</b>, which is the order a browser
    /// uses: an unknown key or an unknown module type is a fault in the source, not in what the
    /// specifier turned out to name, so it must not depend on whether the module exists. The
    /// messages are Chromium's own, measured, so a page that matches on them sees what it expects.
    /// </para>
    /// </remarks>
    private string ValidatedModuleType(JSValue options)
    {
        string requested = null;

        if (options == null || options.IsUndefined)
            return null;

        // `import(spec, null)` is rejected too: null is not an object, and a browser says so with
        // this same message rather than treating it as "no options".
        if (options is not JSObject || options.IsNull)
            throw NewTypeError("The second argument to import() must be an object");

        var with = options[(KeyString)"with"];
        if (with == null || with.IsUndefined)
            return null;

        if (with is not JSObject || with.IsNull)
            throw NewTypeError("The 'with' option must be an object");

        // Own enumerable string keys, in the object's own order, so a `with` built at runtime is
        // read the way any other options bag is.
        var keys = with.GetAllKeys(showEnumerableOnly: true, inherited: false);
        while (keys.MoveNext(out var key))
        {
            var value = with[key];
            if (value == null || !value.IsString)
                throw NewTypeError("Import attribute value must be a string");

            requested = CheckAttribute(key.ToString(), value.StringValue) ?? requested;
        }

        return requested;
    }

    /// <summary>
    /// Validates one attribute, returning the module type it asks for or null when it asks for none.
    /// </summary>
    private string CheckAttribute(string key, string value)
    {
        // `type` is the only attribute key the platform defines. A static declaration's keys are
        // literals the parser already rejected; a dynamic import's keys are a runtime value and
        // can only be judged here, where a browser reports the same thing as a TypeError.
        if (key != "type")
            throw NewTypeError($"Invalid attribute key \"{key}\".");

        // The module-type vocabulary is the platform's, not this host's, so that a page asking for a
        // type Broiler does not implement is told *that* rather than told it made a typo.
        if (value == JsonModuleType)
            return JsonModuleType;

        if (value == "css")
            throw NewTypeError("CSS module scripts are not implemented.");

        throw NewTypeError($"\"{value}\" is not a valid module type.");
    }

    // ---------------------------------------------------------------- records and loading

    /// <summary>The record for <paramref name="key"/>, created when the key is new.</summary>
    private JSModule GetOrCreateModule(string key, bool commonJs)
    {
        if (registeredModules.TryGetValue(key, out var registered))
            return registered;

        if (modules.TryGetValue(key, out var module))
            return module;

        module = new JSModule(this, key) { BaseDirectory = GetModuleDirectory(key) };
        if (commonJs && !IsJsonModule(key))
            module.Kind = ModuleKind.CommonJs;

        modules[key] = module;
        return module;
    }

    /// <summary>Resolves <paramref name="request"/> for a referrer whose base is
    /// <paramref name="referrerDirectory"/>, through the host's <see cref="Resolve"/>.</summary>
    private JSModule ResolveRequest(JSModule referrer, string referrerDirectory, ModuleRequest request)
    {
        if (referrer != null && referrer.LoadedModules.TryGetValue(request.CacheKey, out var loaded))
            return loaded;

        // The asserted type is judged before the specifier is resolved: an unknown type is a fault
        // in the request, whatever the specifier names.
        if (request.Type != null)
            CheckAttribute("type", request.Type);

        var key = Resolve(referrerDirectory, request.Specifier)
            ?? throw NewTypeError($"{request.Specifier} module not found");
        CheckModuleType(key, request.Type);
        return GetOrCreateModule(key, commonJs: IsCommonJsModule(key));
    }

    /// <summary>Loads the source of <paramref name="module"/> once; concurrent requests share the
    /// load. A failed load is forgotten, so the next request loads the key again.</summary>
    private Task EnsureLoadedAsync(JSModule module)
    {
        if (module.Kind == ModuleKind.Host)
            return Task.CompletedTask;

        return module.LoadTask ??= LoadSourceAsync(module);
    }

    private async Task LoadSourceAsync(JSModule module)
    {
        try
        {
            // What a host's CompileModuleAsync that builds the module itself (a .csx module, for
            // one) fills in, as the CommonJS `module.exports` it has always received.
            module.CommonJsExports ??= new JSObject();
            await CompileModuleAsync(module);

            if (module.Kind == ModuleKind.Unknown)
                module.Kind = ModuleKind.Host;

            module.Status = ModuleStatus.Unlinked;
        }
        catch
        {
            Forget(module);
            throw;
        }
    }

    /// <summary>Removes a module whose load failed, so the key is loaded afresh next time.</summary>
    private void Forget(JSModule module)
    {
        module.LoadTask = null;
        if (modules.TryGetValue(module.filePath, out var cached) && ReferenceEquals(cached, module))
            modules.Remove(module.filePath);
    }

    /// <summary>
    /// Loads every module <paramref name="root"/> statically requests, transitively
    /// (LoadRequestedModules). Each request is resolved through the host and recorded in its
    /// referrer's [[LoadedModules]] only once the requested module has loaded.
    /// </summary>
    private async Task LoadGraphAsync(JSModule root)
    {
        var visited = new HashSet<JSModule>();
        await VisitAsync(root);

        async Task VisitAsync(JSModule module)
        {
            if (!visited.Add(module))
                return;

            await EnsureLoadedAsync(module);
            if (module.Kind != ModuleKind.SourceText)
                return;

            foreach (var request in module.Analysis.RequestedModules)
            {
                var requested = ResolveRequest(module, module.BaseDirectory, request);
                await VisitAsync(requested);
                module.LoadedModules[request.CacheKey] = requested;
            }
        }
    }

    /// <summary>Loads, links and evaluates <paramref name="module"/>; returns its namespace (or,
    /// for a host module, its exports object).</summary>
    private async Task<JSValue> LoadLinkAndEvaluateAsync(JSModule module)
    {
        await LoadGraphAsync(module);
        Link(module);

        var evaluation = Evaluate(module);
        await evaluation.Task;
        return module.GetNamespace();
    }

    // ---------------------------------------------------------------- Link (ES2024 16.2.1.5.1)

    private void Link(JSModule module)
    {
        var stack = new List<JSModule>();
        try
        {
            InnerModuleLinking(module, stack, 0);
        }
        catch
        {
            foreach (var m in stack)
                m.ResetToUnlinked();

            throw;
        }
    }

    private int InnerModuleLinking(JSModule module, List<JSModule> stack, int index)
    {
        if (!module.IsCyclic)
            return index;

        if (module.Status is ModuleStatus.Linking or ModuleStatus.Linked or ModuleStatus.Evaluating
            or ModuleStatus.EvaluatingAsync or ModuleStatus.Evaluated)
            return index;

        module.Status = ModuleStatus.Linking;
        var moduleIndex = index;
        module.DFSAncestorIndex = index;
        index++;
        stack.Add(module);

        foreach (var request in module.Analysis.RequestedModules)
        {
            var required = module.GetImportedModule(request);
            index = InnerModuleLinking(required, stack, index);
            if (required.IsCyclic && required.Status == ModuleStatus.Linking)
                module.DFSAncestorIndex = Math.Min(module.DFSAncestorIndex, required.DFSAncestorIndex);
        }

        InitializeEnvironment(module);

        if (module.DFSAncestorIndex == moduleIndex)
        {
            JSModule popped;
            do
            {
                popped = stack[^1];
                stack.RemoveAt(stack.Count - 1);
                popped.Status = ModuleStatus.Linked;
            }
            while (!ReferenceEquals(popped, module));
        }

        return index;
    }

    /// <summary>InitializeEnvironment (ES2024 16.2.1.6.4).</summary>
    private void InitializeEnvironment(JSModule module)
    {
        var analysis = module.Analysis;
        foreach (var entry in analysis.IndirectExportEntries)
        {
            var resolution = module.ResolveExport(entry.ExportName);
            if (resolution == null || ReferenceEquals(resolution, JSModule.ResolvedBinding.Ambiguous))
                throw LinkError(module, entry.Request, entry.ImportName ?? "*", resolution != null);
        }

        var imports = new Dictionary<string, JSVariable>(StringComparer.Ordinal);
        foreach (var entry in analysis.ImportEntries)
        {
            var imported = module.GetImportedModule(entry.Request);
            if (entry.ImportName == null)
            {
                imports[entry.LocalName] = ImmutableBinding(entry.LocalName, imported.GetNamespace());
                continue;
            }

            var resolution = imported.ResolveExport(entry.ImportName);
            if (resolution == null || ReferenceEquals(resolution, JSModule.ResolvedBinding.Ambiguous))
                throw LinkError(module, entry.Request, entry.ImportName, resolution != null);

            if (resolution.IsNamespace)
            {
                imports[entry.LocalName] = ImmutableBinding(entry.LocalName, resolution.Module.GetNamespace());
                continue;
            }

            var target = resolution;
            imports[entry.LocalName] = new JSModuleImportBinding(entry.LocalName, () => target.Module.GetBindingCell(target.BindingName));
        }

        module.InitializeEnvironment(imports, DynamicImportFunction(module));
    }

    private static JSVariable ImmutableBinding(string name, JSValue value)
        => new(value, name) { IsReadOnly = true, ThrowOnReadOnlyWrite = true };

    private JSException LinkError(JSModule module, ModuleRequest request, string name, bool ambiguous)
        => JSEngine.NewSyntaxError(ambiguous
            ? $"The requested module '{request.Specifier}' contains conflicting star exports for name '{name}' (imported by '{module.filePath}')"
            : $"The requested module '{request.Specifier}' does not provide an export named '{name}' (imported by '{module.filePath}')");

    // ---------------------------------------------------------------- Evaluate (ES2024 16.2.1.5.3)

    private BuiltIns.Promise.JSPromise Evaluate(JSModule module)
    {
        if (!module.IsCyclic)
        {
            // A synthetic module evaluates synchronously; its promise is already settled.
            var synthetic = new ModuleCapability();
            try
            {
                EvaluateModuleSync(module);
                synthetic.Resolve(JSUndefined.Value);
            }
            catch (Exception ex)
            {
                synthetic.Reject(JSException.ErrorFrom(ex));
            }

            return synthetic.Promise;
        }

        if (module.Status is ModuleStatus.EvaluatingAsync or ModuleStatus.Evaluated && module.CycleRoot != null)
            module = module.CycleRoot;

        if (module.TopLevelCapability != null)
            return module.TopLevelCapability.Promise;

        var stack = new List<JSModule>();
        var capability = new ModuleCapability();
        module.TopLevelCapability = capability;

        try
        {
            InnerModuleEvaluation(module, stack, 0);
        }
        catch (Exception ex)
        {
            var error = JSException.ErrorFrom(ex);
            foreach (var m in stack)
            {
                m.Status = ModuleStatus.Evaluated;
                m.HasEvaluationError = true;
                m.EvaluationError = error;
            }

            capability.Reject(module.HasEvaluationError ? module.EvaluationError : error);
            return capability.Promise;
        }

        if (module.Status == ModuleStatus.Evaluated)
            capability.Resolve(JSUndefined.Value);

        return capability.Promise;
    }

    private int InnerModuleEvaluation(JSModule module, List<JSModule> stack, int index)
    {
        if (!module.IsCyclic)
        {
            EvaluateModuleSync(module);
            return index;
        }

        if (module.Status is ModuleStatus.EvaluatingAsync or ModuleStatus.Evaluated)
        {
            if (!module.HasEvaluationError)
                return index;

            throw JSException.FromValue(module.EvaluationError);
        }

        if (module.Status == ModuleStatus.Evaluating)
            return index;

        if (module.Status != ModuleStatus.Linked)
            throw new InvalidOperationException($"The module '{module.filePath}' is not linked.");

        module.Status = ModuleStatus.Evaluating;
        var moduleIndex = index;
        module.DFSAncestorIndex = index;
        module.PendingAsyncDependencies = 0;
        index++;
        stack.Add(module);

        foreach (var request in module.Analysis.RequestedModules)
        {
            var required = module.GetImportedModule(request);
            index = InnerModuleEvaluation(required, stack, index);
            if (!required.IsCyclic)
                continue;

            if (required.Status == ModuleStatus.Evaluating)
            {
                module.DFSAncestorIndex = Math.Min(module.DFSAncestorIndex, required.DFSAncestorIndex);
            }
            else
            {
                required = required.CycleRoot;
                if (required.HasEvaluationError)
                    throw JSException.FromValue(required.EvaluationError);
            }

            if (required.AsyncEvaluationOrder > 0)
            {
                module.PendingAsyncDependencies++;
                required.AsyncParentModules.Add(module);
            }
        }

        if (module.PendingAsyncDependencies > 0 || module.HasTLA)
        {
            module.AsyncEvaluationOrder = ++asyncEvaluationCount;
            if (module.PendingAsyncDependencies == 0)
                ExecuteAsyncModule(module);
        }
        else
        {
            ExecuteModuleSync(module);
        }

        if (module.DFSAncestorIndex == moduleIndex)
        {
            JSModule popped;
            do
            {
                popped = stack[^1];
                stack.RemoveAt(stack.Count - 1);
                popped.Status = popped.AsyncEvaluationOrder == 0 ? ModuleStatus.Evaluated : ModuleStatus.EvaluatingAsync;
                popped.CycleRoot = module;
            }
            while (!ReferenceEquals(popped, module));
        }

        return index;
    }

    /// <summary>ExecuteModule for a module without top-level await: runs the rest of its body.</summary>
    private static void ExecuteModuleSync(JSModule module)
    {
        if (module.InstantiatedBody.MoveNext(JSUndefined.Value, out _))
            throw new InvalidOperationException($"The module '{module.filePath}' suspended although it has no top-level await.");
    }

    /// <summary>ExecuteAsyncModule (ES2024 16.2.1.5.3.3).</summary>
    private void ExecuteAsyncModule(JSModule module)
    {
        var completion = JSAsyncFunction.ResumeAsyncBody(module.InstantiatedBody);
        if (completion is not BuiltIns.Promise.JSPromise promise)
            throw new InvalidOperationException("An async module body did not produce a promise.");

        promise.AddReactions(
            (in Arguments a) =>
            {
                AsyncModuleExecutionFulfilled(module);
                return JSUndefined.Value;
            },
            (in Arguments a) =>
            {
                AsyncModuleExecutionRejected(module, a.Get1());
                return JSUndefined.Value;
            });
    }

    /// <summary>GatherAvailableAncestors (ES2024 16.2.1.5.3.4).</summary>
    private static void GatherAvailableAncestors(JSModule module, List<JSModule> execList)
    {
        foreach (var m in module.AsyncParentModules)
        {
            if (execList.Contains(m) || m.CycleRoot.HasEvaluationError)
                continue;

            m.PendingAsyncDependencies--;
            if (m.PendingAsyncDependencies == 0)
            {
                execList.Add(m);
                if (!m.HasTLA)
                    GatherAvailableAncestors(m, execList);
            }
        }
    }

    /// <summary>AsyncModuleExecutionFulfilled (ES2024 16.2.1.5.3.5).</summary>
    private void AsyncModuleExecutionFulfilled(JSModule module)
    {
        if (module.Status == ModuleStatus.Evaluated)
            return;

        module.AsyncEvaluationOrder = -1;
        module.Status = ModuleStatus.Evaluated;
        module.TopLevelCapability?.Resolve(JSUndefined.Value);

        var execList = new List<JSModule>();
        GatherAvailableAncestors(module, execList);
        execList.Sort(static (a, b) => a.AsyncEvaluationOrder.CompareTo(b.AsyncEvaluationOrder));

        foreach (var m in execList)
        {
            if (m.Status == ModuleStatus.Evaluated)
                continue;

            if (m.HasTLA)
            {
                ExecuteAsyncModule(m);
                continue;
            }

            try
            {
                ExecuteModuleSync(m);
            }
            catch (Exception ex)
            {
                AsyncModuleExecutionRejected(m, JSException.ErrorFrom(ex));
                continue;
            }

            m.AsyncEvaluationOrder = -1;
            m.Status = ModuleStatus.Evaluated;
            m.TopLevelCapability?.Resolve(JSUndefined.Value);
        }
    }

    /// <summary>AsyncModuleExecutionRejected (ES2024 16.2.1.5.3.6).</summary>
    private static void AsyncModuleExecutionRejected(JSModule module, JSValue error)
    {
        if (module.Status == ModuleStatus.Evaluated)
            return;

        module.HasEvaluationError = true;
        module.EvaluationError = error;
        module.Status = ModuleStatus.Evaluated;
        module.AsyncEvaluationOrder = -1;
        module.TopLevelCapability?.Reject(error);

        foreach (var m in module.AsyncParentModules)
            AsyncModuleExecutionRejected(m, error);
    }

    /// <summary>Evaluates a synthetic module: runs a CommonJS body once, and caches its error.</summary>
    private void EvaluateModuleSync(JSModule module)
    {
        if (module.HasEvaluationError)
            throw JSException.FromValue(module.EvaluationError);

        if (module.Kind != ModuleKind.CommonJs || module.Status == ModuleStatus.Evaluated)
            return;

        // Marked first, as Node does, so a require cycle sees the partly filled exports.
        module.Status = ModuleStatus.Evaluated;
        try
        {
            ExecuteCommonJs(module);
        }
        catch (Exception ex)
        {
            module.HasEvaluationError = true;
            module.EvaluationError = JSException.ErrorFrom(ex);
            throw;
        }
    }

    // ---------------------------------------------------------------- import() and require()

    /// <summary>The function a module's <c>import(specifier, options)</c> calls.</summary>
    private JSFunction DynamicImportFunction(JSModule referrer)
        => (JSFunction)(referrer.Import ??= new JSFunction((in Arguments a) => DynamicImport(referrer, a)));

    /// <summary>Dynamic imports whose promise has not settled yet; see <see cref="DrainAsync"/>.</summary>
    private readonly List<Task> pendingImports = [];

    /// <summary>
    /// EvaluateImportCall for a module: always a promise, which a bad specifier or options object
    /// rejects rather than throws. The promise is settled on the job pump, by the import's own
    /// continuation, so its reactions are ordered with every other job.
    /// </summary>
    private JSValue DynamicImport(JSModule referrer, in Arguments a)
    {
        var capability = new ModuleCapability();
        string specifier;
        string type;
        try
        {
            specifier = a[0].StringValue;
            type = ValidatedModuleType(a[1]);
        }
        catch (Exception ex)
        {
            capability.Reject(JSException.ErrorFrom(ex));
            return capability.Promise;
        }

        var settling = SettleImportAsync(capability, referrer, specifier, type);
        if (!settling.IsCompleted)
            pendingImports.Add(settling);

        return capability.Promise;
    }

    private async Task SettleImportAsync(ModuleCapability capability, JSModule referrer, string specifier, string type)
    {
        JSValue @namespace;
        try
        {
            @namespace = await ImportAsync(referrer, referrer?.BaseDirectory ?? CurrentPath, specifier, type);
        }
        catch (Exception ex)
        {
            capability.Reject(JSException.ErrorFrom(ex));
            return;
        }

        capability.Resolve(@namespace);
    }

    /// <summary>
    /// Lets the work a module run started finish before the run reports completion: every
    /// dynamic import still loading, and every job already queued, including the jobs those
    /// queue in turn. It returns once a turn of the job pump passes with nothing left to run.
    /// A promise that never settles does not hold it up; only queued work does.
    /// </summary>
    private async Task DrainAsync()
    {
        while (true)
        {
            pendingImports.RemoveAll(static t => t.IsCompleted);
            if (pendingImports.Count > 0)
            {
                try
                {
                    await Task.WhenAll(pendingImports.ToArray());
                }
                catch
                {
                    // Each import settles its own promise; its failure is JavaScript's to observe.
                }

                continue;
            }

            var before = JobsRun;
            await Task.Yield();
            if (JobsRun == before && pendingImports.TrueForAll(static t => t.IsCompleted))
                return;
        }
    }

    /// <summary>
    /// Completes after one job has run on the job queue: the promise reaction ContinueDynamicImport
    /// links and evaluates in, so a dynamic import never evaluates its module inside the
    /// <c>import()</c> call that requested it.
    /// </summary>
    private static Task NextJob()
    {
        var job = new TaskCompletionSource();
        PostJob(() => job.SetResult());
        return job.Task;
    }

    /// <summary>
    /// HostLoadImportedModule followed by ContinueDynamicImport: resolves the request through the
    /// host, loads the graph, links it and evaluates it, and returns the module's namespace. Every
    /// failure is an exception: a TypeError for resolution and loading, a SyntaxError for a parse or
    /// link error, and the thrown value for an evaluation error — the same value on every import of
    /// a module whose evaluation failed.
    /// </summary>
    private async Task<JSValue> ImportAsync(JSModule referrer, string referrerDirectory, string specifier, string type)
    {
        var request = new ModuleRequest(specifier, type);
        var module = ResolveRequest(referrer, referrerDirectory, request);
        await LoadGraphAsync(module);
        if (referrer != null)
            referrer.LoadedModules[request.CacheKey] = module;

        await NextJob();
        Link(module);
        await Evaluate(module).Task;
        return module.GetNamespace();
    }

    /// <summary>
    /// Imports <paramref name="name"/> for a caller that is not itself a module — the host — or,
    /// with <paramref name="esModule"/> false, requires it as CommonJS.
    /// </summary>
    /// <param name="currentPath">The base the specifier resolves against.</param>
    /// <param name="name">The specifier. It is resolved through <see cref="Resolve"/>.</param>
    /// <param name="esModule">
    /// Whether the caller is <c>import</c> (the module's namespace) rather than <c>require</c>
    /// (its CommonJS view: <c>module.exports</c>, or a JSON module's parsed value).
    /// </param>
    /// <param name="requiredType">
    /// The module type an import attribute asserted, or null when none was. A mismatch is a load
    /// failure.
    /// </param>
    protected virtual Task<JSValue> LoadModuleAsync(
        string currentPath, string name, bool esModule = true, string requiredType = null)
        => esModule
            ? ImportAsync(null, currentPath, name, requiredType)
            : Task.FromResult(RequireModule(currentPath, name));

    /// <summary>
    /// <c>require(specifier)</c> from a CommonJS module: loads the module as CommonJS (or JSON, or a
    /// host module), synchronously, evaluates it once, and returns its CommonJS view.
    /// </summary>
    private JSValue RequireModule(string referrerDirectory, string specifier)
    {
        var key = Resolve(referrerDirectory, specifier)
            ?? throw NewTypeError($"{specifier} module not found");

        var module = GetOrCreateModule(key, commonJs: true);
        if (module.LoadTask == null || !module.LoadTask.IsCompleted)
            AsyncPump.Run(async () =>
            {
                await EnsureLoadedAsync(module);
                return JSUndefined.Value;
            });

        module.LoadTask?.GetAwaiter().GetResult();

        switch (module.Kind)
        {
            case ModuleKind.Json:
                return module.JsonValue;

            case ModuleKind.Host:
                return module.CommonJsExports;

            case ModuleKind.CommonJs:
                EvaluateModuleSync(module);
                return module.CommonJsExports;

            default:
                throw NewTypeError($"require() of the ECMAScript module '{key}' is not supported; use import()");
        }
    }

    /// <summary>Runs a CommonJS module body with its CommonJS bindings.</summary>
    private void ExecuteCommonJs(JSModule module)
    {
        var dirPath = module.BaseDirectory;
        module.Require ??= new JSFunction((in Arguments a) =>
        {
            var name = a[0];
            if (!name.IsString)
                throw NewTypeError("require method's parameter must be a string");

            return RequireModule(dirPath, name.StringValue);
        });
        DynamicImportFunction(module);

        var exports = module.CommonJsExports;
        module.Body(new Arguments(exports,
        [
            exports,
            module.Require,
            module,
            module.Id,
            CreateString(dirPath ?? string.Empty),
            module.Import
        ]));
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

    /// <summary>
    /// Prepares a loaded module: reads its source and parses it as JSON, as a CommonJS module or
    /// as an ECMAScript module (compiling it and reading its import and export entries). A host
    /// that builds a module itself — filling <see cref="JSModule.Exports"/> — overrides this for
    /// that module and does not call the base; such a module is a host module whose exports are
    /// the properties of that object.
    /// </summary>
    internal protected virtual async Task CompileModuleAsync(JSModule module)
    {
        var filePath = module.filePath;

        if (module.Code == null)
            module.Code = await ReadModuleSourceAsync(module);

        var code = module.Code;

        // ParseJSONModule (ES2025 16.2.1.7): JSON.parse of the source, whose one export is
        // `default`. Invalid JSON is a SyntaxError when the module loads.
        if (IsJsonModule(filePath))
        {
            module.JsonValue = BuiltIns.Json.JSJSON.Parse(new Arguments(JSUndefined.Value, CreateString(code)));
            module.Kind = ModuleKind.Json;
            return;
        }

        if (module.Kind == ModuleKind.CommonJs)
        {
            module.Body = CoreScript.Compile(code, filePath, CommonJsParameters, codeCache: CodeCache);
            return;
        }

        // Parsed with the module goal symbol and compiled as module code: strict, `await`
        // reserved and allowed at the top level, `this` undefined, and a module environment of
        // its own (see FastCompiler's module prologue).
        using (CoreScript.AllowTopLevelAwaitScope())
            module.Body = CoreScript.Compile(code, filePath, ModuleParameters, codeCache: CodeCache, isModule: true);

        module.Analysis = ModuleSourceAnalysis.Parse(code);
        module.Kind = ModuleKind.SourceText;
    }
}