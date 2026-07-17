#if BROILER_FULL_HOST
using System;
using System.Runtime.CompilerServices;
using Broiler.JavaScript.Feature.Sample;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Storage;

internal sealed class DeferredSampleFeatureRegistration : IBuiltInFeatureSatellite
{
    private static readonly KeyString FeatureKey = KeyStrings.GetOrCreate("sampleFeature");
    private readonly object sync = new();
    private IBuiltInFeatureSatellite satellite;

    public BuiltInFeatureDescriptor Descriptor { get; } = new(
        BuiltInFeatureId.HostExtensions,
        "SampleHostFeature",
        "Broiler.JavaScript.Feature.Sample",
        BuiltInFeatures.Core,
        SupportsLazyRealization: true);

    public void Register(IJSContext context)
    {
        if (context is not JSObject global || context is not IJSFeatureResolver resolver)
            throw new ArgumentException("The sample satellite requires a JSObject realm.", nameof(context));

        global.FastAddLazyDataProperty(
            FeatureKey,
            resolver,
            BuiltInFeatureId.HostExtensions,
            JSPropertyAttributes.ConfigurableValue);
    }

    public JSValue Resolve(IJSContext context) => GetSatellite().Resolve(context);

    private IBuiltInFeatureSatellite GetSatellite()
    {
        if (satellite != null)
            return satellite;

        lock (sync)
            return satellite ??= SampleSatelliteFactory.Create();
    }
}

internal static class SampleSatelliteFactory
{
    // Keep the concrete feature type out of startup JIT. The assembly reference remains
    // trimming-visible, but the CLR loads it only when the lazy feature is resolved.
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    internal static IBuiltInFeatureSatellite Create() => new SampleFeatureSatellite();
}
#endif
