using System;

namespace Broiler.JavaScript.Runtime;

/// <summary>
/// Contract interface for the ES2025 DisposableStack built-in.
/// Lives in Runtime so that the Compiler can reference it without
/// depending on the concrete <c>JSDisposableStack</c> implementation
/// in the BuiltIns assembly.
/// </summary>
public interface IJSDisposableStack
{
    /// <summary>
    /// Adds a disposable resource to the stack.
    /// </summary>
    void AddDisposableResource(JSValue value, bool async);

    /// <summary>
    /// Disposes all resources on the stack. Returns a <see cref="JSValue"/>
    /// (either <c>undefined</c> for sync disposal or a Promise for async).
    /// </summary>
    JSValue Dispose();

    /// <summary>
    /// Seeds the pending completion with an exception thrown by the guarded block,
    /// so a subsequent disposer error wraps it as the <c>suppressed</c> value of a
    /// SuppressedError (DisposeResources runs with the block's [[Completion]]). Called
    /// before <see cref="Dispose"/> when a lexical <c>using</c> block's body threw.
    /// Returns undefined (typed so the compiler can use it as a catch-clause value).
    /// </summary>
    JSValue SeedPendingError(System.Exception bodyException);

    /// <summary>
    /// Factory delegate used by the Compiler to create new instances
    /// without referencing the concrete type.
    /// Wired by the BuiltIns assembly via <c>[ModuleInitializer]</c>.
    /// </summary>
    static Func<IJSDisposableStack> CreateNew { get; set; }

    /// <summary>
    /// Creates a new <see cref="IJSDisposableStack"/> instance via the
    /// registered factory delegate.
    /// </summary>
    static IJSDisposableStack New() => CreateNew();
}
