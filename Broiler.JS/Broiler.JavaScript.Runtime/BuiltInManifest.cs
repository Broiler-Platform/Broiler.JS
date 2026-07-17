using System;
using System.Collections.Generic;

namespace Broiler.JavaScript.Runtime;

/// <summary>Stable feature identifiers used by generated manifests and lazy cells.</summary>
public enum BuiltInFeatureId : byte
{
    Core = 0,
    Intl = 1,
    Temporal = 2,
    HostExtensions = 3,
}

[Flags]
public enum BuiltInFeatures : uint
{
    None = 0,
    Core = 1 << (int)BuiltInFeatureId.Core,
    Intl = 1 << (int)BuiltInFeatureId.Intl,
    Temporal = 1 << (int)BuiltInFeatureId.Temporal,
    HostExtensions = 1 << (int)BuiltInFeatureId.HostExtensions,
    Standard = Core | Intl | Temporal,
    All = Standard | HostExtensions,
}

public readonly record struct BuiltInRegistrationDescriptor(
    BuiltInFeatureId Feature,
    string TypeName,
    bool RegistersGlobal);

public readonly record struct BuiltInFeatureDescriptor(
    BuiltInFeatureId Id,
    string Name,
    string AssemblyName,
    BuiltInFeatures Dependencies,
    bool SupportsLazyRealization);

/// <summary>Immutable, trimming-visible description of a built-in distribution.</summary>
public sealed class BuiltInManifest
{
    public static BuiltInManifest Empty { get; } = new(
        "empty",
        "0.0.0",
        Array.Empty<BuiltInFeatureDescriptor>(),
        Array.Empty<BuiltInRegistrationDescriptor>());

    public BuiltInManifest(
        string name,
        string semanticVersion,
        IReadOnlyList<BuiltInFeatureDescriptor> features,
        IReadOnlyList<BuiltInRegistrationDescriptor> registrations)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        SemanticVersion = semanticVersion ?? throw new ArgumentNullException(nameof(semanticVersion));
        Features = Copy(features);
        Registrations = Copy(registrations);
    }

    public string Name { get; }
    public string SemanticVersion { get; }
    public IReadOnlyList<BuiltInFeatureDescriptor> Features { get; }
    public IReadOnlyList<BuiltInRegistrationDescriptor> Registrations { get; }

    private static IReadOnlyList<T> Copy<T>(IReadOnlyList<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var result = new T[source.Count];
        for (var i = 0; i < result.Length; i++)
            result[i] = source[i];
        return Array.AsReadOnly(result);
    }
}

/// <summary>Implemented by a realm that can realize a core-owned feature ID.</summary>
public interface IJSFeatureResolver
{
    JSValue ResolveBuiltInFeature(BuiltInFeatureId feature);
}

/// <summary>
/// Explicit, host-selected feature contribution. Implementations must not use module
/// initializers; merely deploying a satellite must not register or root it.
/// </summary>
public interface IBuiltInFeatureSatellite
{
    BuiltInFeatureDescriptor Descriptor { get; }
    void Register(IJSContext context);
    JSValue Resolve(IJSContext context);
}

/// <summary>Composes a base registry with explicitly supplied feature satellites.</summary>
public sealed class CompositeBuiltInRegistry : IBuiltInRegistry
{
    private readonly IBuiltInRegistry baseRegistry;
    private readonly IBuiltInFeatureSatellite[] satellites;
    private readonly BuiltInManifest manifest;

    public CompositeBuiltInRegistry(
        IBuiltInRegistry baseRegistry,
        params IBuiltInFeatureSatellite[] satellites)
    {
        this.baseRegistry = baseRegistry ?? throw new ArgumentNullException(nameof(baseRegistry));
        this.satellites = satellites is { Length: > 0 }
            ? (IBuiltInFeatureSatellite[])satellites.Clone()
            : Array.Empty<IBuiltInFeatureSatellite>();

        var baseManifest = baseRegistry.Manifest;
        var features = new BuiltInFeatureDescriptor[baseManifest.Features.Count + this.satellites.Length];
        for (var i = 0; i < baseManifest.Features.Count; i++)
            features[i] = baseManifest.Features[i];
        for (var i = 0; i < this.satellites.Length; i++)
            features[baseManifest.Features.Count + i] = this.satellites[i].Descriptor;

        manifest = new BuiltInManifest(
            baseManifest.Name + "+host",
            baseManifest.SemanticVersion,
            features,
            baseManifest.Registrations);
    }

    public BuiltInManifest Manifest => manifest;

    public void Register(IJSContext context)
    {
        baseRegistry.Register(context);
        foreach (var satellite in satellites)
            satellite.Register(context);
    }

    public JSValue ResolveFeature(IJSContext context, BuiltInFeatureId feature)
    {
        foreach (var satellite in satellites)
            if (satellite.Descriptor.Id == feature)
                return satellite.Resolve(context);

        return baseRegistry.ResolveFeature(context, feature);
    }
}
