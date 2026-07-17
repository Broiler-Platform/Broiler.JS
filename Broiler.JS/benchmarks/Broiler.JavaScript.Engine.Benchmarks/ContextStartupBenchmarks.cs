using BenchmarkDotNet.Attributes;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Storage;

namespace Broiler.JavaScript.Engine.Benchmarks;

public class ContextStartupBenchmarks
{
    [Benchmark]
    public JSValue CreateContext()
    {
        using var context = BenchmarkContext.Create();
        return context[KeyStrings.globalThis];
    }
}
