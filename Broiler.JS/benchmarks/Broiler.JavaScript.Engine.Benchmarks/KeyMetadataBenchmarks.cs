using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Broiler.JavaScript.Storage;

namespace Broiler.JavaScript.Engine.Benchmarks;

public class KeyMetadataBenchmarks
{
    private readonly KeyString hit = KeyStrings.GetOrCreate("phase2-key-hit");
    private int batch;

    [Benchmark(Baseline = true)]
    public KeyString InternHit()
        => KeyStrings.GetOrCreate("phase2-key-hit");

    [Benchmark]
    public KeyMetadata MetadataRead()
        => hit.Metadata;

    [Benchmark]
    public uint InternMissUnderContention()
    {
        var current = Interlocked.Increment(ref batch);
        int combined = 0;
        Parallel.For(0, 8, i =>
        {
            var key = KeyStrings.GetOrCreate($"phase2-contended-{current}-{i}");
            Interlocked.Add(ref combined, (int)key.Key);
        });
        return (uint)combined;
    }
}
