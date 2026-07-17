using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Text.Json;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Engine.Benchmarks;

internal static class LifecycleHarness
{
    public static void Run(string scenario)
    {
        if (!string.Equals(scenario, "all", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"Unknown lifecycle scenario '{scenario}'. Expected 'all'.");

        var process = Process.GetCurrentProcess();
        var processToMainMilliseconds = (System.DateTime.UtcNow - process.StartTime.ToUniversalTime()).TotalMilliseconds;
        var allocatedAtMain = GC.GetTotalAllocatedBytes(precise: true);
        var assembliesAtMain = AppDomain.CurrentDomain.GetAssemblies().Length;

        var firstContext = Measure(() => BenchmarkContext.Create());
        using var context = firstContext.Value;

        var firstParseCompileEvaluate = Measure(() => context.Eval(
            "(function () { var total = 0; for (var i = 0; i < 100; i++) total += i; return total; })()",
            "lifecycle-first-script.js"));

        var cacheHitEvaluate = Measure(() => context.Eval(
            "(function () { var total = 0; for (var i = 0; i < 100; i++) total += i; return total; })()",
            "lifecycle-first-script.js"));

        var subsequentContext = Measure(() => BenchmarkContext.Create());
        subsequentContext.Value.Dispose();

        process.Refresh();
        var result = new
        {
            schemaVersion = "1.0.0",
            kind = "lifecycle",
            scenario = "all",
            timestampUtc = DateTimeOffset.UtcNow,
            environment = new
            {
                runtime = RuntimeInformation.FrameworkDescription,
                os = RuntimeInformation.OSDescription,
                rid = RuntimeInformation.RuntimeIdentifier,
                processArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
                processorCount = Environment.ProcessorCount,
                serverGc = GCSettings.IsServerGC,
                latencyMode = GCSettings.LatencyMode.ToString(),
                stopwatchFrequency = Stopwatch.Frequency,
            },
            measurements = new
            {
                processToMainMilliseconds,
                allocatedBytesAtMain = allocatedAtMain,
                assembliesAtMain,
                firstContextMilliseconds = firstContext.Elapsed.TotalMilliseconds,
                firstContextAllocatedBytes = firstContext.AllocatedBytes,
                firstParseCompileEvaluateMilliseconds = firstParseCompileEvaluate.Elapsed.TotalMilliseconds,
                firstParseCompileEvaluateAllocatedBytes = firstParseCompileEvaluate.AllocatedBytes,
                cacheHitEvaluateMilliseconds = cacheHitEvaluate.Elapsed.TotalMilliseconds,
                cacheHitEvaluateAllocatedBytes = cacheHitEvaluate.AllocatedBytes,
                subsequentContextMilliseconds = subsequentContext.Elapsed.TotalMilliseconds,
                subsequentContextAllocatedBytes = subsequentContext.AllocatedBytes,
                assembliesAfterScenario = AppDomain.CurrentDomain.GetAssemblies().Length,
                workingSetBytes = process.WorkingSet64,
            },
        };

        Console.WriteLine(JsonSerializer.Serialize(result));
    }

    private static Measurement<T> Measure<T>(Func<T> action)
    {
        var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
        var stopwatch = Stopwatch.StartNew();
        var value = action();
        stopwatch.Stop();
        return new Measurement<T>(value, stopwatch.Elapsed, GC.GetTotalAllocatedBytes(precise: true) - allocatedBefore);
    }

    private readonly record struct Measurement<T>(T Value, TimeSpan Elapsed, long AllocatedBytes);
}
