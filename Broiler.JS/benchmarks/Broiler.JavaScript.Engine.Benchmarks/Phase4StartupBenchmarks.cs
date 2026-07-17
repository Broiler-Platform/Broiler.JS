using BenchmarkDotNet.Attributes;
using Broiler.JavaScript.BuiltIns;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Engine.Benchmarks;

[MemoryDiagnoser]
public class Phase4StartupBenchmarks
{
    private static readonly JSContextOptions FullEager = new()
    {
        BootstrapProfile = JavaScriptBootstrapProfile.FullEager,
        BuiltInRegistry = DefaultBuiltInRegistry.Instance,
    };

    private static readonly JSContextOptions FullLazy = new()
    {
        BootstrapProfile = JavaScriptBootstrapProfile.Full,
        BuiltInRegistry = DefaultBuiltInRegistry.Instance,
    };

    private static readonly JSContextOptions Minimal = new()
    {
        BootstrapProfile = JavaScriptBootstrapProfile.Minimal,
        BuiltInRegistry = DefaultBuiltInRegistry.Instance,
    };

    [Benchmark(Baseline = true)]
    public long CreateFullEager() => CreateAndDispose(FullEager);

    [Benchmark]
    public long CreateFullLazy() => CreateAndDispose(FullLazy);

    [Benchmark]
    public long CreateMinimal() => CreateAndDispose(Minimal);

    [Benchmark]
    public string CreateFullLazyAndUseIntl()
    {
        using var context = new JSContext(options: FullLazy);
        return context.Eval("Intl.NumberFormat('en').format(42)").ToString();
    }

    [Benchmark]
    public string CreateFullLazyAndUseTemporal()
    {
        using var context = new JSContext(options: FullLazy);
        return context.Eval("typeof Temporal.Instant").ToString();
    }

    private static long CreateAndDispose(JSContextOptions options)
    {
        using var context = new JSContext(options: options);
        return context.ID;
    }
}
