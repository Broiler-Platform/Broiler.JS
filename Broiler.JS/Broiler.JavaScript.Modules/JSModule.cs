using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.Runtime;
using System;
using System.Threading.Tasks;

namespace Broiler.JavaScript.Modules;

/// <summary>
/// Create and load a module
/// </summary>

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

    public JSModule(JSModuleContext context, JSObject exports, string name, bool isMain = false) : this(context.ModulePrototype)
    {
        moduleContext = context;
        filePath = name;
        dirPath = "./";
        this.exports = exports;
    }

    internal JSModule(JSModuleContext context, string name, string code = null) : this(context.ModulePrototype)
    {
        moduleContext = context;
        filePath = name;
        dirPath = System.IO.Path.GetDirectoryName(dirPath);
        Code = code;
    }

    [JSPrototypeMethod]
    [JSExport("id")]
    public JSValue Id => CreateString(filePath);

    JSValue exports;

    [JSPrototypeMethod]
    [JSExport("exports")]
    public JSValue Exports
    {
        get
        {
            return exports;
        }
        set
        {
            if (value == null || value.IsNullOrUndefined)
                throw JSEngine.NewTypeError("Exports cannot be set to null or undefined");

            exports = value;
        }
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
                meta.FastAddValue((KeyString)"url", CreateString(url), JSPropertyAttributes.EnumerableConfigurableValue);

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

    /// <summary>
    /// Direct (non-marshalled) compile hook. When set, <see cref="InitAsync"/> awaits this .NET task
    /// instead of invoking the <see cref="Compile"/> JS function, which would marshal the compile task
    /// into a JS promise and re-await it (<c>Task → IJSPromise → Task</c>). That double-marshal re-posts
    /// the module body's async continuation off the running event loop, so a body that suspends at a
    /// top-level <c>await</c> (which every static <c>import</c> desugars to) settles at the first
    /// suspension and never runs to completion — leaving the module's exports unbound. Awaiting the compile
    /// task directly keeps the whole init on one pumped loop. Falls back to the JS-function path when null.
    /// </summary>
    internal Func<Task> CompileDirect { get; set; }

    internal async Task InitAsync()
    {
        if (exports != null)
            return;

        exports = new JSObject();

        if (CompileDirect != null)
        {
            await CompileDirect();
            return;
        }

        var result = Compile.InvokeFunction(new Arguments(this));
        if (result is IJSPromise promise)
            await promise.Task;
    }
}
