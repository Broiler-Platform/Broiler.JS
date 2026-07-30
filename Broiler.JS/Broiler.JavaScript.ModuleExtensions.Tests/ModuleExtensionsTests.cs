using System.IO;
using System.Threading.Tasks;
using Broiler.JavaScript.BuiltIns.String;
using Broiler.JavaScript.ModuleExtensions;
using Broiler.JavaScript.Modules;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.ModuleExtensions.Tests;

public class ModuleExtensionsTests
{
    [Fact]
    public void ModuleBuilder_Create_Succeeds()
    {
        var builder = new ModuleBuilder("testmod");
        Assert.NotNull(builder);
    }

    [Fact]
    public void ModuleBuilder_ExportValue_ReturnsSelf()
    {
        var builder = new ModuleBuilder("testmod");
        var result = builder.ExportValue("version", "1.0");
        Assert.Same(builder, result);
    }

    [Fact]
    public void ModuleBuilder_ExportFunction_ReturnsSelf()
    {
        var builder = new ModuleBuilder("testmod");
        var result = builder.ExportFunction("greet", (in Arguments a) => new JSString("hello"));
        Assert.Same(builder, result);
    }

    // ExportValue records the .NET value and marshals it in AddModuleToContext. Marshalling at
    // record time reached JSValue.CreateString before the BuiltIns [ModuleInitializer] that wires
    // it had run — building a module without first touching the engine threw NullReferenceException.
    [Fact]
    public void ModuleBuilder_ExportValue_AcceptsPrimitivesBeforeEngineInitialization()
    {
        var builder = new ModuleBuilder("testmod");
        Assert.Same(builder, builder.ExportValue("version", "1.0"));
        Assert.Same(builder, builder.ExportValue("count", 42));
        Assert.Same(builder, builder.ExportValue("enabled", true));
        Assert.Same(builder, builder.ExportValue("missing", null));
    }

    // ...and the deferred marshalling still produces the right JS values once a context exists.
    [Fact]
    public async Task ModuleBuilder_ExportedValues_AreVisibleToImportingScript()
    {
        using var ctx = new TestModuleContext();
        new ModuleBuilder("testmod")
            .ExportValue("version", "1.0")
            .ExportValue("count", 42)
            .ExportValue("enabled", true)
            .ExportFunction("greet", (in Arguments a) => new JSString("hello"))
            .AddModuleToContext(ctx);

        await ctx.RunScriptAsync(
            "import { version, count, enabled, greet } from 'testmod';\n" +
            "globalThis.result = version + '|' + count + '|' + enabled + '|' + greet();",
            "file:///app/main.js",
            uniqueModuleID: "file:///app/main.js");

        Assert.Equal("1.0|42|true|hello", ctx.Eval("globalThis.result").ToString());
    }

    private sealed class TestModuleContext : JSModuleContext
    {
        protected override string Resolve(string dirPath, string relativePath) => relativePath;
        protected override string GetModuleDirectory(string fullPath) => fullPath;
        protected override Task<string> ReadModuleSourceAsync(JSModule module)
            => throw new FileNotFoundException(module.filePath);
    }
}
