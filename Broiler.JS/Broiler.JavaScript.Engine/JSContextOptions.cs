using Broiler.JavaScript.ExpressionCompiler.Runtime;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Engine;

/// <summary>Immutable host choices for one JavaScript realm.</summary>
public sealed class JSContextOptions
{
    public static JSContextOptions Default { get; } = new();

    /// <summary>Enables the script-host proper-tail-call trampoline.</summary>
    public bool ScriptHostMode { get; init; }

    /// <summary>Uses the compatibility process-wide cache instead of realm-local storage.</summary>
    public bool UseProcessSharedCodeCache { get; init; }

    public DictionaryCodeCacheOptions CodeCache { get; init; } = new();

    /// <summary>Opt-in, per-realm hot-function promotion limits.</summary>
    public FunctionTieringOptions FunctionTiering { get; init; } = FunctionTieringOptions.Disabled;

    public ExpressionCompilationBackend CompilationBackend { get; init; }
        = ExpressionCompilationBackend.DynamicMethod;

    /// <summary>Controls the standard feature surface and which values are lazy.</summary>
    public JavaScriptBootstrapProfile BootstrapProfile { get; init; }
        = JavaScriptBootstrapProfile.Full;

    /// <summary>Optional per-realm registry, avoiding process-wide bootstrap state.</summary>
    public IBuiltInRegistry BuiltInRegistry { get; init; }

    public JSContextOptions WithBootstrapProfile(JavaScriptBootstrapProfile profile) => new()
    {
        ScriptHostMode = ScriptHostMode,
        UseProcessSharedCodeCache = UseProcessSharedCodeCache,
        CodeCache = CodeCache,
        FunctionTiering = FunctionTiering,
        CompilationBackend = CompilationBackend,
        BootstrapProfile = profile ?? throw new System.ArgumentNullException(nameof(profile)),
        BuiltInRegistry = BuiltInRegistry,
    };

    public JSContextOptions WithBuiltInRegistry(IBuiltInRegistry registry) => new()
    {
        ScriptHostMode = ScriptHostMode,
        UseProcessSharedCodeCache = UseProcessSharedCodeCache,
        CodeCache = CodeCache,
        FunctionTiering = FunctionTiering,
        CompilationBackend = CompilationBackend,
        BootstrapProfile = BootstrapProfile,
        BuiltInRegistry = registry ?? throw new System.ArgumentNullException(nameof(registry)),
    };

    public JSContextOptions WithFunctionTiering(FunctionTieringOptions functionTiering) => new()
    {
        ScriptHostMode = ScriptHostMode,
        UseProcessSharedCodeCache = UseProcessSharedCodeCache,
        CodeCache = CodeCache,
        FunctionTiering = functionTiering ?? throw new System.ArgumentNullException(nameof(functionTiering)),
        CompilationBackend = CompilationBackend,
        BootstrapProfile = BootstrapProfile,
        BuiltInRegistry = BuiltInRegistry,
    };
}
