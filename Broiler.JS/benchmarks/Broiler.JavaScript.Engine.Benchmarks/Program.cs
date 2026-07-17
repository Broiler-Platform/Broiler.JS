using System;
using BenchmarkDotNet.Running;

namespace Broiler.JavaScript.Engine.Benchmarks;

public static class Program
{
    public static void Main(string[] args)
    {
        if (args.Length >= 1 && args[0] == "--lifecycle-child")
        {
            LifecycleHarness.Run(args.Length >= 2 ? args[1] : "all");
            return;
        }

        if (args.Length >= 2 && args[0] == "--profile")
        {
            var iterations = args.Length >= 3 ? int.Parse(args[2]) : 25;
            ProfileScenarios.Run(args[1], iterations);
            return;
        }

        if (args.Length == 2 && args[0] == "--assembly-metrics")
        {
            AssemblyMetrics.Write(args[1]);
            return;
        }

        if (args.Length == 1 && args[0] == "--sparse-metrics")
        {
            SparseStorageMetrics.Write();
            return;
        }

        BenchmarkSwitcher
            .FromTypes([
                typeof(ContextStartupBenchmarks),
                typeof(ScriptEvaluationBenchmarks),
                typeof(CodeCacheBenchmarks),
                typeof(FunctionCallBenchmarks),
                typeof(PropertyOperationBenchmarks),
                typeof(KeyMetadataBenchmarks),
                typeof(ArrayPrimitiveBenchmarks),
                typeof(SparseMapBenchmarks),
                typeof(MapSetBenchmarks),
                typeof(BinaryDataBenchmarks),
                typeof(ParserCompilerBenchmarks),
                typeof(SwitchDispatchBenchmarks),
                typeof(ScalarAndPropertySpecializationBenchmarks),
                typeof(Phase4StartupBenchmarks),
                typeof(Phase5TieringBenchmarks),
                typeof(Phase5TaggedValueFeasibilityBenchmarks),
                typeof(ObjectAndArrayBenchmarks),
                typeof(PromiseBenchmarks),
                typeof(BuiltInHeavyBenchmarks),
                typeof(JIntSmokeBenchmarks),
            ])
            .Run(args, BenchmarkConfiguration.Create());
    }
}
