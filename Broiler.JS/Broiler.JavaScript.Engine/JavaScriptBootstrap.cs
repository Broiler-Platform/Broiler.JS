using System;
using System.Threading;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Engine;

/// <summary>Explicit host API for selecting a built-in manifest and realm profile.</summary>
public static class JavaScriptBootstrap
{
    public static BuiltInManifest CurrentManifest
        => JSEngine.BuiltInRegistry?.Manifest ?? BuiltInManifest.Empty;

    public static void ConfigureDefault(IBuiltInRegistry registry)
    {
        JSEngine.BuiltInRegistry = registry ?? throw new ArgumentNullException(nameof(registry));
        JSEngine.HasExplicitBuiltInRegistry = true;
    }

    public static CompositeBuiltInRegistry Compose(
        IBuiltInRegistry baseRegistry,
        params IBuiltInFeatureSatellite[] satellites)
        => new(baseRegistry, satellites);

    public static JavaScriptContextBuilder CreateContextBuilder() => new();
}

public sealed class JavaScriptContextBuilder
{
    private JSContextOptions options = JSContextOptions.Default;
    private SynchronizationContext synchronizationContext;
    private JavaScriptFeatureFlags experimentalFeatures;

    public JavaScriptContextBuilder UseOptions(JSContextOptions value)
    {
        options = value ?? throw new ArgumentNullException(nameof(value));
        return this;
    }

    public JavaScriptContextBuilder UseProfile(JavaScriptBootstrapProfile profile)
    {
        options = options.WithBootstrapProfile(profile);
        return this;
    }

    public JavaScriptContextBuilder UseBuiltInRegistry(IBuiltInRegistry registry)
    {
        options = options.WithBuiltInRegistry(registry);
        return this;
    }

    public JavaScriptContextBuilder UseFunctionTiering(FunctionTieringOptions functionTiering)
    {
        options = options.WithFunctionTiering(functionTiering);
        return this;
    }

    public JavaScriptContextBuilder UseSynchronizationContext(SynchronizationContext value)
    {
        synchronizationContext = value;
        return this;
    }

    public JavaScriptContextBuilder EnableExperimentalFeatures(JavaScriptFeatureFlags features)
    {
        experimentalFeatures = features;
        return this;
    }

    public JSContext Build() => new(synchronizationContext, experimentalFeatures, options);
}
