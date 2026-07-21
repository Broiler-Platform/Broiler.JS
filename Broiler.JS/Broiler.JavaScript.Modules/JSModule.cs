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

    public JSModule(JSModuleContext context, JSObject exports, string name, bool isMain = false) : this(context.ModulePrototype)
    {
        filePath = name;
        dirPath = "./";
        this.exports = exports;
    }

    internal JSModule(JSModuleContext context, string name, string code = null) : this(context.ModulePrototype)
    {
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
