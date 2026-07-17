using System;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Storage;

namespace Broiler.JavaScript.Feature.Sample;

/// <summary>
/// Prototype satellite: no module initializer and no registration side effects. A host
/// must explicitly compose this instance into its bootstrap registry.
/// </summary>
public sealed class SampleFeatureSatellite : IBuiltInFeatureSatellite
{
    private static readonly KeyString FeatureKey = KeyStrings.GetOrCreate("sampleFeature");

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

    public JSValue Resolve(IJSContext context)
        => new JSObject();
}
