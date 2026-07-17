using System;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Engine;

/// <summary>Immutable feature and realization policy for one JavaScript realm.</summary>
public sealed class JavaScriptBootstrapProfile
{
    public static JavaScriptBootstrapProfile Full { get; } = new(
        "full",
        BuiltInFeatures.Standard,
        BuiltInFeatures.Intl | BuiltInFeatures.Temporal,
        isConformant: true);

    public static JavaScriptBootstrapProfile FullEager { get; } = new(
        "full-eager",
        BuiltInFeatures.Standard,
        BuiltInFeatures.None,
        isConformant: true);

    public static JavaScriptBootstrapProfile Minimal { get; } = new(
        "minimal",
        BuiltInFeatures.Core,
        BuiltInFeatures.None,
        isConformant: false);

    public JavaScriptBootstrapProfile(
        string name,
        BuiltInFeatures features,
        BuiltInFeatures lazyFeatures,
        bool isConformant = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("A bootstrap profile requires a name.", nameof(name));
        if ((features & BuiltInFeatures.Core) == 0)
            throw new ArgumentException("Every bootstrap profile must include Core.", nameof(features));
        if ((lazyFeatures & ~features) != 0)
            throw new ArgumentException("Lazy features must also be enabled by the profile.", nameof(lazyFeatures));

        Name = name;
        Features = features;
        LazyFeatures = lazyFeatures;
        IsConformant = isConformant;
    }

    public string Name { get; }
    public BuiltInFeatures Features { get; }
    public BuiltInFeatures LazyFeatures { get; }
    public bool IsConformant { get; }

    public bool Includes(BuiltInFeatureId feature)
        => (Features & FeatureFlag(feature)) != 0;

    public bool IsLazy(BuiltInFeatureId feature)
        => (LazyFeatures & FeatureFlag(feature)) != 0;

    private static BuiltInFeatures FeatureFlag(BuiltInFeatureId feature)
        => (BuiltInFeatures)(1u << (int)feature);
}
