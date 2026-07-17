using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;

namespace Broiler.JavaScript.Engine.Benchmarks;

internal static class BenchmarkConfiguration
{
    public const string ProfileEnvironmentVariable = "BROILER_BENCHMARK_PROFILE";
    public const string ArtifactsEnvironmentVariable = "BROILER_BENCHMARK_ARTIFACTS";

    public static IConfig Create()
    {
        var profile = Environment.GetEnvironmentVariable(ProfileEnvironmentVariable)?.Trim().ToLowerInvariant()
            ?? "baseline";

        var config = ManualConfig.Create(DefaultConfig.Instance)
            .AddDiagnoser(MemoryDiagnoser.Default)
            .AddExporter(JsonExporter.Full, CsvExporter.Default);

        var artifactsPath = Environment.GetEnvironmentVariable(ArtifactsEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(artifactsPath))
            config.ArtifactsPath = artifactsPath;

        switch (profile)
        {
            case "smoke":
                config.AddJob(Job.ShortRun.WithId("Smoke"));
                break;

            case "disassembly":
                config.AddJob(CreateBaselineJob("Disassembly"));
                config.AddDiagnoser(new DisassemblyDiagnoser(new DisassemblyDiagnoserConfig(
                    maxDepth: 3,
                    exportCombinedDisassemblyReport: true)));
                break;

            case "baseline":
                config.AddJob(CreateBaselineJob("Baseline"));
                break;

            default:
                throw new InvalidOperationException(
                    $"Unknown benchmark profile '{profile}'. Expected smoke, baseline, or disassembly.");
        }

        return config;
    }

    private static Job CreateBaselineJob(string id)
        => Job.Default
            .WithId(id)
            .WithLaunchCount(1)
            .WithWarmupCount(5)
            .WithIterationCount(10);
}
