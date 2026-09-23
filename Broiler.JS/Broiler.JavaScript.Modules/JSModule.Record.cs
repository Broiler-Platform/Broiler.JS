using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Modules;

/// <summary>The kind of module a record holds.</summary>
public enum ModuleKind
{
    /// <summary>Not loaded yet.</summary>
    Unknown,

    /// <summary>An ECMAScript module (a Source Text Module Record).</summary>
    SourceText,

    /// <summary>A CommonJS module: one export, <c>default</c>, which is its <c>module.exports</c>.</summary>
    CommonJs,

    /// <summary>A JSON module: one export, <c>default</c>, which is the parsed value.</summary>
    Json,

    /// <summary>A module the host built: its exports are the own properties of an exports object.</summary>
    Host,
}

/// <summary>The [[Status]] of a module record (ES2024 16.2.1.5, plus <c>New</c> for a record whose
/// source has not been loaded).</summary>
public enum ModuleStatus
{
    New,
    Unlinked,
    Linking,
    Linked,
    Evaluating,
    EvaluatingAsync,
    Evaluated,
}

partial class JSModule : IJSModuleEnvironment
{
    /// <summary>A ResolvedBinding Record: the module that defines an export and the name of its
    /// binding there, or no name for the namespace of that module.</summary>
    internal sealed class ResolvedBinding(JSModule module, string bindingName)
    {
        public readonly JSModule Module = module;
        public readonly string BindingName = bindingName;
        public bool IsNamespace => BindingName == null;

        /// <summary>The ~ambiguous~ answer of ResolveExport.</summary>
        public static readonly ResolvedBinding Ambiguous = new(null, "*ambiguous*");
    }

    /// <summary>The kind of module this record holds.</summary>
    public ModuleKind Kind { get; internal set; }

    /// <summary>The module's [[Status]].</summary>
    public ModuleStatus Status { get; internal set; }

    /// <summary>The ImportEntries, ExportEntries and requests of an ECMAScript module.</summary>
    internal ModuleSourceAnalysis Analysis;

    /// <summary>The compiled body of an ECMAScript or CommonJS module.</summary>
    internal JSFunctionDelegate Body;

    /// <summary>The parsed value of a JSON module.</summary>
    internal JSValue JsonValue;

    /// <summary>[[LoadedModules]]: the module each request of this module resolved to.</summary>
    internal readonly Dictionary<string, JSModule> LoadedModules = new(StringComparer.Ordinal);

    /// <summary>The source load shared by every request for this key while it is in flight.</summary>
    internal Task LoadTask;

    // The module environment: the bindings the body published, and the import bindings the
    // linker created for it. Null until InitializeEnvironment.
    private Dictionary<string, JSVariable> localBindings;
    private Dictionary<string, JSVariable> importBindings;
    private IJSGenerator body;
    private JSModuleNamespace @namespace;
    private Dictionary<string, JSVariable> hostBindings;

    // Link() and Evaluate() bookkeeping (ES2024 16.2.1.5, Cyclic Module Records).
    internal int DFSAncestorIndex;
    internal JSModule CycleRoot;

    /// <summary>[[AsyncEvaluationOrder]]: 0 is ~unset~, -1 is ~done~, anything else the order.</summary>
    internal long AsyncEvaluationOrder;
    internal List<JSModule> AsyncParentModules = [];
    internal int PendingAsyncDependencies;
    internal ModuleCapability TopLevelCapability;
    internal bool HasEvaluationError;
    internal JSValue EvaluationError;

    internal bool IsCyclic => Kind == ModuleKind.SourceText;

    internal bool HasTLA => Analysis?.HasTopLevelAwait == true;

    /// <summary>
    /// Returns the record to the state a fresh load leaves it in, so that a failed link can be
    /// retried: ES2024 16.2.1.5.1 Link returns every module of the failed graph to ~unlinked~.
    /// </summary>
    internal void ResetToUnlinked()
    {
        Status = ModuleStatus.Unlinked;
        localBindings = null;
        importBindings = null;
        body = null;
        @namespace = null;
    }

    // ---------------------------------------------------------------- IJSModuleEnvironment

    JSVariable IJSModuleEnvironment.GetImportBinding(string localName)
        => importBindings != null && importBindings.TryGetValue(localName, out var binding)
            ? binding
            : throw new InvalidOperationException($"The module '{filePath}' has no import binding '{localName}'.");

    void IJSModuleEnvironment.PublishBinding(string localName, JSVariable binding)
        => (localBindings ??= new(StringComparer.Ordinal))[localName] = binding;

    JSValue IJSModuleEnvironment.ImportMeta => Meta;

    // ---------------------------------------------------------------- environment

    /// <summary>Creates the import bindings and instantiates the body up to its first statement
    /// (InitializeEnvironment, after the linker has checked every indirect export).</summary>
    internal void InitializeEnvironment(Dictionary<string, JSVariable> imports, JSValue dynamicImport)
    {
        importBindings = imports;
        localBindings = new(StringComparer.Ordinal);

        // The body is a generator (see FastCompiler's module branch): calling it binds the
        // arguments, and the first step runs the prologue — hoisting, import bindings, the
        // published exports — and suspends before the first statement.
        var generator = Body(new Arguments(JSUndefined.Value, dynamicImport, this)) as IJSGenerator
            ?? throw new InvalidOperationException($"The module '{filePath}' was not compiled as a module body.");

        if (!generator.MoveNext(JSUndefined.Value, out _))
            throw new InvalidOperationException($"The module '{filePath}' completed during instantiation.");

        body = generator;
    }

    /// <summary>The module body, suspended after its prologue.</summary>
    internal IJSGenerator InstantiatedBody => body ?? throw new InvalidOperationException($"The module '{filePath}' is not instantiated.");

    /// <summary>
    /// The binding <paramref name="bindingName"/> of this module's environment, or null while the
    /// module has no environment ([[Environment]] is ~empty~).
    /// </summary>
    internal JSVariable GetBindingCell(string bindingName)
    {
        switch (Kind)
        {
            case ModuleKind.SourceText:
                if (localBindings == null)
                    return null;

                if (localBindings.TryGetValue(bindingName, out var local))
                    return local;

                // A local export of an imported namespace (`import * as ns; export { ns }`) was
                // rewritten to an indirect export by ParseModule, so every exported local name is
                // one the body published.
                return importBindings != null && importBindings.TryGetValue(bindingName, out var imported)
                    ? imported
                    : null;

            default:
                hostBindings ??= new(StringComparer.Ordinal);
                if (!hostBindings.TryGetValue(bindingName, out var cell))
                    hostBindings[bindingName] = cell = new SyntheticExportBinding(this, bindingName);
                return cell;
        }
    }

    // ---------------------------------------------------------------- exports

    /// <summary>The names a synthetic (CommonJS, JSON or host) module exports.</summary>
    private IEnumerable<string> SyntheticExportNames()
    {
        yield return "default";

        if (Kind != ModuleKind.Host || exports is not JSObject exportsObject)
            yield break;

        var keys = exportsObject.GetAllKeys(showEnumerableOnly: true, inherited: false);
        while (keys.MoveNext(out var hasValue, out var key, out _))
        {
            if (hasValue && key.IsString && key.ToString() != "default")
                yield return key.ToString();
        }
    }

    /// <summary>The current value of a synthetic module's export.</summary>
    internal JSValue ReadSyntheticExport(string name)
    {
        switch (Kind)
        {
            case ModuleKind.Json:
                return JsonValue ?? throw JSEngine.NewReferenceError("Cannot access 'default' before initialization");

            case ModuleKind.CommonJs:
                return exports;

            default:
                if (exports is not JSObject exportsObject)
                    return JSUndefined.Value;

                if (name == "default" && exportsObject.GetOwnPropertyDescriptor(CreateString("default")).IsUndefined)
                    return exportsObject;

                return exportsObject[KeyStrings.GetOrCreate(name)];
        }
    }

    /// <summary>GetExportedNames (ES2024 16.2.1.6.2).</summary>
    internal List<string> GetExportedNames(HashSet<JSModule> exportStarSet = null)
    {
        if (!IsCyclic)
            return [.. SyntheticExportNames()];

        exportStarSet ??= [];
        if (!exportStarSet.Add(this))
            return [];

        var names = new List<string>();
        foreach (var entry in Analysis.LocalExportEntries)
            names.Add(entry.ExportName);

        foreach (var entry in Analysis.IndirectExportEntries)
            names.Add(entry.ExportName);

        foreach (var entry in Analysis.StarExportEntries)
        {
            var requested = GetImportedModule(entry.Request);
            foreach (var name in requested.GetExportedNames(exportStarSet))
            {
                if (name != "default" && !names.Contains(name))
                    names.Add(name);
            }
        }

        return names;
    }

    /// <summary>ResolveExport (ES2024 16.2.1.6.3): a binding, null (not found or circular), or
    /// <see cref="ResolvedBinding.Ambiguous"/>.</summary>
    internal ResolvedBinding ResolveExport(string exportName, List<(JSModule Module, string ExportName)> resolveSet = null)
    {
        if (!IsCyclic)
        {
            foreach (var name in SyntheticExportNames())
            {
                if (name == exportName)
                    return new ResolvedBinding(this, exportName);
            }

            return null;
        }

        resolveSet ??= [];
        foreach (var (module, name) in resolveSet)
        {
            if (ReferenceEquals(module, this) && name == exportName)
                return null;
        }

        resolveSet.Add((this, exportName));

        foreach (var entry in Analysis.LocalExportEntries)
        {
            if (entry.ExportName == exportName)
                return new ResolvedBinding(this, entry.LocalName);
        }

        foreach (var entry in Analysis.IndirectExportEntries)
        {
            if (entry.ExportName != exportName)
                continue;

            var imported = GetImportedModule(entry.Request);
            return entry.ImportName == null
                ? new ResolvedBinding(imported, null)
                : imported.ResolveExport(entry.ImportName, resolveSet);
        }

        if (exportName == "default")
            return null;

        ResolvedBinding starResolution = null;
        foreach (var entry in Analysis.StarExportEntries)
        {
            var imported = GetImportedModule(entry.Request);
            var resolution = imported.ResolveExport(exportName, resolveSet);
            if (ReferenceEquals(resolution, ResolvedBinding.Ambiguous))
                return ResolvedBinding.Ambiguous;

            if (resolution == null)
                continue;

            if (starResolution == null)
            {
                starResolution = resolution;
                continue;
            }

            if (!ReferenceEquals(resolution.Module, starResolution.Module)
                || resolution.BindingName != starResolution.BindingName)
                return ResolvedBinding.Ambiguous;
        }

        return starResolution;
    }

    /// <summary>GetModuleNamespace (ES2024 16.2.1.10).</summary>
    public JSModuleNamespace GetNamespace()
    {
        if (@namespace != null)
            return @namespace;

        var resolved = new List<(string, ResolvedBinding)>();
        foreach (var name in GetExportedNames())
        {
            var resolution = ResolveExport(name);
            if (resolution != null && !ReferenceEquals(resolution, ResolvedBinding.Ambiguous))
                resolved.Add((name, resolution));
        }

        return @namespace = new JSModuleNamespace(resolved);
    }

    /// <summary>GetImportedModule: the module a request of this module was loaded as.</summary>
    internal JSModule GetImportedModule(ModuleRequest request)
        => LoadedModules.TryGetValue(request.CacheKey, out var module)
            ? module
            : throw new InvalidOperationException($"The module '{filePath}' has not loaded '{request.Specifier}'.");
}

/// <summary>
/// The binding a synthetic (CommonJS, JSON or host) module exports under a name: never
/// initialized itself, it reads the module's current export on every access.
/// </summary>
internal sealed class SyntheticExportBinding : JSVariable
{
    private readonly JSModule module;
    private readonly string name;

    public SyntheticExportBinding(JSModule module, string name) : base(JSUndefined.Value, name, initialized: false)
    {
        this.module = module;
        this.name = name;
        IsReadOnly = true;
        ThrowOnReadOnlyWrite = true;
    }

    internal override JSValue ReadUnavailable() => module.ReadSyntheticExport(name);

    public override JSValue GetValue() => module.ReadSyntheticExport(name);
}

/// <summary>A PromiseCapability Record for a promise the linker settles.</summary>
internal sealed class ModuleCapability
{
    private Action<JSValue> resolve;
    private Action<JSValue> reject;

    public readonly BuiltIns.Promise.JSPromise Promise;

    public ModuleCapability()
    {
        Promise = new BuiltIns.Promise.JSPromise((resolve, reject) =>
        {
            this.resolve = resolve;
            this.reject = reject;
        });
    }

    public void Resolve(JSValue value) => resolve(value);

    public void Reject(JSValue value) => reject(value);
}
