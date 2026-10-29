using Broiler.JavaScript.BuiltIns;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Feature.Sample;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Storage;

namespace Broiler.JavaScript.BuiltIns.Tests;

public sealed class Phase4StartupAndPackagingTests
{
    private static readonly KeyString IntlKey = KeyStrings.GetOrCreate("Intl");
    private static readonly KeyString TemporalKey = KeyStrings.GetOrCreate("Temporal");

    [Fact]
    public void FullProfileInstallsUnrealizedDataCellsWithoutAffectingEnumeration()
    {
        using var context = new JSContext(options: new JSContextOptions
        {
            BootstrapProfile = JavaScriptBootstrapProfile.Full,
        });

        Assert.True(context.GetOwnProperties(false).TryGetValue(IntlKey.Key, out var intl));
        Assert.True(context.GetOwnProperties(false).TryGetValue(TemporalKey.Key, out var temporal));
        var intlCell = Assert.IsType<LazyDataPropertyCell>(intl.value);
        var temporalCell = Assert.IsType<LazyDataPropertyCell>(temporal.value);
        Assert.True(intl.IsConfigurable);
        Assert.True(temporal.IsConfigurable);
        Assert.False(intl.IsEnumerable);
        Assert.False(temporal.IsEnumerable);

        var keys = context.Eval("Reflect.ownKeys(globalThis).map(String).join('|')").ToString();
        Assert.Contains("Intl", keys);
        Assert.Contains("Temporal", keys);

        Assert.False(intlCell.IsRealized);
        Assert.False(temporalCell.IsRealized);
    }

    [Fact]
    public void ValueAndDescriptorAccessRealizeOncePerRealm()
    {
        using var first = new JSContext();
        var result = first.Eval("""
            var a = Intl;
            var b = Intl;
            var d = Object.getOwnPropertyDescriptor(globalThis, 'Temporal');
            [a === b, typeof d.value, d.get, d.enumerable, d.configurable, d.writable].join('|');
            """);

        Assert.Equal("true|object||false|true|true", result.ToString());
        var firstIntl = first[IntlKey];
        var firstTemporal = first[TemporalKey];

        using var second = new JSContext();
        Assert.NotSame(firstIntl, second[IntlKey]);
        Assert.NotSame(firstTemporal, second[TemporalKey]);
        Assert.Same(second[IntlKey], second[IntlKey]);
        Assert.Same(second[TemporalKey], second[TemporalKey]);
    }

    [Fact]
    public void DeleteAndRedefinitionCancelUnrealizedFactories()
    {
        using var context = new JSContext();
        context.GetOwnProperties(false).TryGetValue(IntlKey.Key, out var intlProperty);
        context.GetOwnProperties(false).TryGetValue(TemporalKey.Key, out var temporalProperty);
        var intlCell = Assert.IsType<LazyDataPropertyCell>(intlProperty.value);
        var temporalCell = Assert.IsType<LazyDataPropertyCell>(temporalProperty.value);

        var result = context.Eval("""
            delete globalThis.Intl;
            Object.defineProperty(globalThis, 'Temporal', {
              value: 17, writable: true, enumerable: false, configurable: true
            });
            [typeof Intl, Temporal].join('|');
            """);

        Assert.Equal("undefined|17", result.ToString());
        Assert.True(intlCell.IsCanceled);
        Assert.True(temporalCell.IsCanceled);
        Assert.False(intlCell.IsRealized);
        Assert.False(temporalCell.IsRealized);
    }

    [Fact]
    public void AttributeOnlyRedefinitionPreservesTheUnrealizedDataValue()
    {
        using var context = new JSContext();
        context.GetOwnProperties(false).TryGetValue(IntlKey.Key, out var property);
        var cell = Assert.IsType<LazyDataPropertyCell>(property.value);

        Assert.True(context.Eval("""
            Object.defineProperty(globalThis, 'Intl', { enumerable: true });
            Object.keys(globalThis).includes('Intl');
            """).BooleanValue);
        Assert.False(cell.IsRealized);
        Assert.Equal("object", context.Eval("typeof Intl").ToString());
        Assert.True(cell.IsRealized);
        context.GetOwnProperties(false).TryGetValue(IntlKey.Key, out var updated);
        Assert.True(updated.IsEnumerable);
    }

    [Fact]
    public void MinimalProfileOmitsHeavyNamespacesButRetainsCoreExecution()
    {
        using var context = JavaScriptBootstrap.CreateContextBuilder()
            .UseBuiltInRegistry(DefaultBuiltInRegistry.Instance)
            .UseProfile(JavaScriptBootstrapProfile.Minimal)
            .Build();

        Assert.Equal(
            "undefined|undefined|6|function|false|false",
            context.Eval(
                "[typeof Intl, typeof Temporal, [1,2,3].length + 3, typeof Promise, " +
                "Object.prototype.hasOwnProperty.call(globalThis,'Intl'), " +
                "Object.prototype.hasOwnProperty.call(globalThis,'Temporal')].join('|')").ToString());
    }

    [Fact]
    public void LazyAndEagerFullProfilesPreserveGlobalKeyOrder()
    {
        using var lazy = new JSContext(options: new JSContextOptions
        {
            BootstrapProfile = JavaScriptBootstrapProfile.Full,
        });
        var lazyKeys = lazy.Eval("Reflect.ownKeys(globalThis).map(String).join('|')").ToString();

        using var eager = new JSContext(options: new JSContextOptions
        {
            BootstrapProfile = JavaScriptBootstrapProfile.FullEager,
        });
        var eagerKeys = eager.Eval("Reflect.ownKeys(globalThis).map(String).join('|')").ToString();

        Assert.Equal(eagerKeys, lazyKeys);
    }

    [Fact]
    public void GeneratedManifestDescribesFeatureOwnershipWithoutRuntimeTypeScanning()
    {
        var manifest = DefaultBuiltInRegistry.Instance.Manifest;

        Assert.Equal("4.0.0", manifest.SemanticVersion);
        Assert.Contains(manifest.Features, static feature => feature.Id == BuiltInFeatureId.Intl && feature.SupportsLazyRealization);
        Assert.Contains(manifest.Features, static feature => feature.Id == BuiltInFeatureId.Temporal && feature.SupportsLazyRealization);
        Assert.Contains(manifest.Registrations, static item => item.Feature == BuiltInFeatureId.Temporal);
        Assert.Contains(manifest.Registrations, static item => item.TypeName.EndsWith("JSArray", StringComparison.Ordinal));
        Assert.DoesNotContain(manifest.Registrations, static item => string.IsNullOrWhiteSpace(item.TypeName));
    }

    [Fact]
    public void FeatureSatelliteRequiresExplicitCompositionAndRealizesLazily()
    {
        var registry = JavaScriptBootstrap.Compose(
            DefaultBuiltInRegistry.Instance,
            new SampleFeatureSatellite());
        var profile = new JavaScriptBootstrapProfile(
            "selective-host",
            BuiltInFeatures.Core | BuiltInFeatures.HostExtensions,
            BuiltInFeatures.HostExtensions);

        using var context = JavaScriptBootstrap.CreateContextBuilder()
            .UseBuiltInRegistry(registry)
            .UseProfile(profile)
            .Build();

        var key = KeyStrings.GetOrCreate("sampleFeature");
        Assert.True(context.GetOwnProperties(false).TryGetValue(key.Key, out var property));
        var cell = Assert.IsType<LazyDataPropertyCell>(property.value);
        Assert.False(cell.IsRealized);
        Assert.Contains(registry.Manifest.Features, static feature =>
            feature.Id == BuiltInFeatureId.HostExtensions
            && feature.AssemblyName == "Broiler.JavaScript.Feature.Sample");

        Assert.Equal("object", context.Eval("typeof sampleFeature").ToString());
        Assert.True(cell.IsRealized);
    }

    [Fact]
    public void RecursiveLazyInitializationFailsDeterministically()
    {
        var resolver = new RecursiveResolver();
        var cell = new LazyDataPropertyCell(resolver, BuiltInFeatureId.Intl);
        resolver.Cell = cell;

        var first = Assert.Throws<InvalidOperationException>(() => cell.Resolve());
        var second = Assert.Throws<InvalidOperationException>(() => cell.Resolve());
        Assert.Contains("Recursive lazy initialization", first.Message);
        Assert.Equal(first.Message, second.Message);
    }

    private sealed class RecursiveResolver : IJSFeatureResolver
    {
        public LazyDataPropertyCell Cell { get; set; } = null!;
        public JSValue ResolveBuiltInFeature(BuiltInFeatureId feature) => Cell.Resolve();
    }
}
