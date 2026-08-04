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

        if (args.Length == 1 && args[0] == "--cache-metrics")
        {
            InlineCacheMetrics.Write();
            return;
        }

        if (args.Length == 1 && args[0] == "--object-alloc")
        {
            ObjectAllocationMetrics.Write();
            return;
        }

        if (args.Length == 1 && args[0] == "--element-alloc")
        {
            ElementAllocationMetrics.Write();
            return;
        }

        if (args.Length == 1 && args[0] == "--regex-profile")
        {
            RegexProfileMetrics.Write();
            return;
        }

        if (args.Length == 1 && args[0] == "--local-alloc")
        {
            LocalAllocationMetrics.Write();
            return;
        }

        if (args.Length == 1 && args[0] == "--deferred-cell-cost")
        {
            DeferredCellCostMetrics.Write();
            return;
        }

        if (args.Length == 2 && args[0] == "--suite-cache-metrics")
        {
            SuiteCacheMetrics.Write(args[1]);
            return;
        }

        if (args.Length == 2 && args[0] == "--type-feedback")
        {
            TypeFeedbackMetrics.Write(args[1]);
            return;
        }

        if (args.Length >= 1 && args[0] == "--specializing-read-probe")
        {
            SpecializingTierMetrics.WriteReadProbe(
                args.Length >= 2 ? int.Parse(args[1]) : 20_000_000,
                args.Length >= 3 ? int.Parse(args[2]) : 6);
            return;
        }

        if (args.Length >= 2 && args[0] == "--specializing-tier")
        {
            SpecializingTierMetrics.Write(
                args[1],
                args.Length >= 3 && Enum.TryParse<SpecializingTierMetrics.Arm>(args[2], true, out var arm)
                    ? arm
                    : SpecializingTierMetrics.Arm.Specializing,
                counters: args.Length < 4 || args[3] != "timing");
            return;
        }

        if (args.Length == 2 && args[0] == "--property-map-distribution")
        {
            PropertyMapDistributionMetrics.Write(args[1]);
            return;
        }

        if (args.Length == 1 && args[0] == "--compile-scaling")
        {
            CompileScalingMetrics.Write();
            return;
        }

        if (args.Length >= 2 && args[0] == "--compile-profile")
        {
            CompileProfileMetrics.Write(
                args[1],
                args.Length >= 3 ? int.Parse(args[2]) : 3,
                args.Length >= 4 ? args[3] : null);
            return;
        }

        BenchmarkSwitcher
            .FromTypes([
                typeof(ContextStartupBenchmarks),
                typeof(ScriptEvaluationBenchmarks),
                typeof(HotPathProbeBenchmarks),
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
