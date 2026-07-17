using System;

namespace Broiler.JavaScript.Runtime;

/// <summary>
/// Defines the contract for registering built-in JavaScript objects
/// (Array, String, Number, Date, Promise, etc.) into a context.
/// Implementations allow swapping the set of built-in objects that are
/// available at runtime, enabling isolation and testability.
/// </summary>
/// <remarks>
/// The parameter type is <see cref="IJSContext"/> so that this interface
/// can live in the Runtime assembly without depending on the concrete
/// <c>JSContext</c> class in Core.  Implementations that need full access
/// to <c>JSContext</c> members should cast the parameter.
/// </remarks>
public interface IBuiltInRegistry
{
    /// <summary>Generated, trimming-visible feature and registration metadata.</summary>
    BuiltInManifest Manifest => BuiltInManifest.Empty;

    /// <summary>
    /// Registers built-in objects into the specified context.
    /// Implementations should call <c>CreateClass</c> for each built-in type
    /// that needs to be available in the context.
    /// </summary>
    /// <param name="context">The context to populate with built-in objects.</param>
    void Register(IJSContext context);

    /// <summary>Realizes one lazy feature for a specific realm.</summary>
    JSValue ResolveFeature(IJSContext context, BuiltInFeatureId feature)
        => throw new InvalidOperationException(
            $"Built-in feature '{feature}' is not provided by manifest '{Manifest.Name}'.");
}
