using BenchmarkDotNet.Running;

namespace Broiler.JavaScript.Engine.Benchmarks;

public static class Program
{
    public static void Main(string[] args)
    {
        BenchmarkSwitcher
            .FromTypes([
                typeof(ContextStartupBenchmarks),
                typeof(ScriptEvaluationBenchmarks),
                typeof(ObjectAndArrayBenchmarks),
                typeof(PromiseBenchmarks),
                typeof(BuiltInHeavyBenchmarks),
                typeof(JIntSmokeBenchmarks),
            ])
            .Run(args);
    }
}
