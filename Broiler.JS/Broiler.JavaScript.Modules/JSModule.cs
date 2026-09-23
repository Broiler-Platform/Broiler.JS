using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.Runtime;
using System;
using System.Threading.Tasks;

namespace Broiler.JavaScript.Modules;

/// <summary>
/// A module record: one ECMAScript module, CommonJS module, JSON module or host-provided module,
/// identified by the key the host's <c>Resolve</c> produced.
/// </summary>
/// <remarks>
/// It is also the <c>module</c> object a CommonJS module body receives. An ECMAScript module body
/// never sees it: it receives only its module environment (see <see cref="IJSModuleEnvironment"/>),
/// which this record implements. The linking and evaluation state lives in JSModule.Record.cs.
/// </remarks>
[JSBaseClass("Object")]
[JSFunctionGenerator("Module", Register = false)]
public partial class JSModule : JSObject
{
    public readonly string filePath;
    internal readonly string dirPath;

    [JSPrototypeMethod]
    [JSExport("code")]
    public string Code { get; set; }

    public JSModule(in Arguments a) => throw new NotSupportedException();

    /// <summary>The context that loaded this module, kept so <see cref="Meta"/> can ask it what a
    /// module key's URL is — the one part of <c>import.meta</c> only the host can answer.</summary>
    private readonly JSModuleContext moduleContext;

    /// <summary>
    /// A host-provided module whose exports object already exists. An import sees each own
    /// property of <paramref name="exports"/> as an export of that name, read live, and
    /// <c>default</c> as the exports object itself unless it has an own <c>default</c>.
    /// </summary>
    public JSModule(JSModuleContext context, JSObject exports, string name, bool isMain = false) : this(context.ModulePrototype)
    {
        moduleContext = context;
        filePath = name;
        dirPath = "./";
        this.exports = exports;
        Kind = ModuleKind.Host;
    }

    internal JSModule(JSModuleContext context, string name, string code = null) : this(context.ModulePrototype)
    {
        moduleContext = context;
        filePath = name;
        dirPath = System.IO.Path.GetDirectoryName(dirPath);
        Code = code;
    }

    internal JSModuleContext Context => moduleContext;

    /// <summary>The base this module's own relative specifiers resolve against.</summary>
    internal string BaseDirectory { get; set; }

    [JSPrototypeMethod]
    [JSExport("id")]
    public JSValue Id => CreateString(filePath);

    JSValue exports;

    /// <summary>
    /// What the module exports: the <c>module.exports</c> object of a CommonJS or host module, and
    /// the module namespace object of an ECMAScript or JSON module once it has been linked.
    /// </summary>
    [JSPrototypeMethod]
    [JSExport("exports")]
    public JSValue Exports
    {
        get
        {
            if ((Kind == ModuleKind.SourceText && Status >= ModuleStatus.Linked)
                || (Kind == ModuleKind.Json && JsonValue != null))
                return GetNamespace();

            return exports;
        }
        // `module.exports` may be set to any value, null and undefined included, as in Node: it
        // is the value `require` returns and the `default` export an import sees.
        set => exports = value ?? JSUndefined.Value;
    }

    /// <summary>The CommonJS <c>module.exports</c> value, without the namespace view.</summary>
    internal JSValue CommonJsExports
    {
        get => exports;
        set => exports = value;
    }

    private JSObject meta;

    /// <summary>
    /// The module's <c>import.meta</c> object — what <c>import.meta</c> compiles to a read of.
    /// </summary>
    /// <remarks>
    /// Created once and then stable, because <c>import.meta === import.meta</c> and a module is
    /// entitled to hang its own state off it: per ES2025 §16.2.1.9 the object is created on first
    /// access and the same object is returned to every later evaluation in that module. It is an
    /// ordinary extensible object with a <b>null prototype</b>, so a property a module adds cannot
    /// be confused with one inherited from <c>Object.prototype</c>.
    /// <para>
    /// It carries <c>url</c> and nothing else. <c>import.meta.resolve</c> is deliberately absent,
    /// and the reason is this context's resolver rather than the amount of code: <see
    /// cref="JSModuleContext.Resolve"/> is existence-based — it probes the filesystem and returns
    /// null for a specifier that does not name a file that is there — while
    /// <c>import.meta.resolve</c> is specified to resolve a specifier to a URL whether or not
    /// anything is at it. Building it on this resolver would throw for a path that a browser answers,
    /// which is a wrong answer to a resolution question rather than a missing one; a page can feature
    /// -detect the absence and cannot detect the wrongness. Making the resolver able to answer
    /// without loading is its own change. Node's <c>dirname</c>/<c>filename</c> are Node-specific and
    /// are not part of the web platform's <c>import.meta</c> at all.
    /// </para>
    /// </remarks>
    [JSPrototypeMethod]
    [JSExport("meta")]
    public JSValue Meta
    {
        get
        {
            if (meta != null)
                return meta;

            meta = new JSObject { BasePrototypeObject = null };
            var url = moduleContext?.GetModuleUrl(filePath);
            if (url != null)
                meta.FastAddValue("url", CreateString(url), JSPropertyAttributes.EnumerableConfigurableValue);

            return meta;
        }
    }

    [JSPrototypeMethod]
    [JSExport("require")]
    public JSValue Require { get; set; }

    [JSPrototypeMethod]
    [JSExport("import")]
    public JSValue Import { get; set; }

    public Task<JSValue> ImportAsync(string name)
    {
        var result = Import.InvokeFunction(new Arguments(JSUndefined.Value, CreateString(name)));
        return (result as IJSPromise).Task;
    }

    [JSPrototypeMethod]
    [JSExport("compile")]
    public JSValue Compile { get; set; }
}
