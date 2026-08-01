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

    /// <summary>
    /// Native stack, in bytes, one JavaScript execution may consume before
    /// "Maximum call stack size exceeded" is raised. 0 (the default) leaves the CLR's own
    /// probe as the only limit, which is the historical behaviour.
    /// </summary>
    /// <remarks>
    /// Set this BELOW the real stack size of the thread that runs JavaScript: the difference
    /// is a reserve that survives the throw, so a `catch` can call functions, build a message
    /// and report the error the way it can in a browser. Without it the RangeError arrives with
    /// the stack already spent — and because .NET runs catch handlers as funclets on the
    /// still-live stack, the handler's first call throws again and escapes the same `try`. A
    /// host that does not control its JavaScript thread's stack size should leave this at 0
    /// rather than guess: too high is merely inert, but too low turns legitimate deep recursion
    /// into a spurious RangeError.
    /// </remarks>
    public long MaxStackUsageBytes { get; init; }

    public JSContextOptions WithBootstrapProfile(JavaScriptBootstrapProfile profile) => new()
    {
        ScriptHostMode = ScriptHostMode,
        UseProcessSharedCodeCache = UseProcessSharedCodeCache,
        CodeCache = CodeCache,
        FunctionTiering = FunctionTiering,
        CompilationBackend = CompilationBackend,
        BootstrapProfile = profile ?? throw new System.ArgumentNullException(nameof(profile)),
        BuiltInRegistry = BuiltInRegistry,
        MaxStackUsageBytes = MaxStackUsageBytes,
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
        MaxStackUsageBytes = MaxStackUsageBytes,
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
        MaxStackUsageBytes = MaxStackUsageBytes,
    };
}
