using BenchmarkDotNet.Attributes;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Engine.Benchmarks;

[MemoryDiagnoser]
[ShortRunJob]
public class PromiseBenchmarks
{
    private const string PromiseChainScript = """
        Promise.resolve(1)
            .then(function (value) { return value + 1; })
            .then(function (value) { return value * 3; })
            .then(function (value) { return value - 4; })
        """;

    private JSContext context;

    [GlobalSetup]
    public void Setup()
    {
        context = BenchmarkContext.Create(new LocalDictionaryCodeCache());
        context.Execute(PromiseChainScript, "promise-chain.js");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        context?.Dispose();
    }

    [Benchmark]
    public JSValue ExecuteResolvedPromiseChain()
        => context.Execute(PromiseChainScript, "promise-chain.js");
}
