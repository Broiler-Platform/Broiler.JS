using System;
using BenchmarkDotNet.Attributes;
using Broiler.JavaScript.BuiltIns;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Engine.Benchmarks;

[MemoryDiagnoser]
public class Phase5TieringBenchmarks
{
    private JSContext baselineContext;
    private JSContext tieredContext;
    private JSValue baselineFunction;
    private JSValue tieredFunction;
    private Arguments arguments;

    [Params(false, true)]
    public bool MixedInput { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        baselineContext = CreateContext(FunctionTieringOptions.Disabled);
        tieredContext = CreateContext(new FunctionTieringOptions
        {
            Enabled = true,
            InvocationThreshold = 1,
            MaxRecompilations = 4,
            MaxRetainedCodeBytes = 1024 * 1024,
        });
        const string source = """
            (function sum(n) {
              var total = 0;
              for (var i = 0; i < n; i++) total += i;
              return total;
            })
            """;
        baselineFunction = baselineContext.Eval(source, "phase5-tiering-benchmark.js");
        tieredFunction = tieredContext.Eval(source, "phase5-tiering-benchmark.js");
        arguments = new Arguments(
            JSValue.UndefinedValue,
            MixedInput ? JSValue.CreateString("20000") : JSValue.CreateNumber(20_000));

        // Promote outside the measured operation. A mixed input deliberately takes
        // the specialized guard's baseline-deoptimization path.
        tieredFunction.InvokeFunction(in arguments);
        if (baselineContext.FunctionTiering.Snapshot().Candidates != 0)
            throw new InvalidOperationException("The disabled benchmark realm unexpectedly enabled tiering.");
        if (tieredContext.FunctionTiering.Snapshot().Recompilations != 1)
            throw new InvalidOperationException("The tiered benchmark function did not promote during setup.");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        baselineContext?.Dispose();
        tieredContext?.Dispose();
    }

    [Benchmark(Baseline = true)]
    public JSValue Baseline() => baselineFunction.InvokeFunction(in arguments);

    [Benchmark]
    public JSValue Tiered() => tieredFunction.InvokeFunction(in arguments);

    private static JSContext CreateContext(FunctionTieringOptions tiering)
        => JavaScriptBootstrap.CreateContextBuilder()
            .UseBuiltInRegistry(DefaultBuiltInRegistry.Instance)
            .UseFunctionTiering(tiering)
            .Build();
}

[MemoryDiagnoser]
public class Phase5TaggedValueFeasibilityBenchmarks
{
    private JSValue[] boxed;
    private TaggedValuePrototype[] tagged;

    [GlobalSetup]
    public void Setup()
    {
        using var context = new JSContext();
        boxed = new JSValue[1024];
        tagged = new TaggedValuePrototype[boxed.Length];
        for (var index = 0; index < boxed.Length; index++)
        {
            boxed[index] = JSValue.CreateNumber(index + 0.25);
            TaggedValuePrototype.TryFromJSValue(boxed[index], out tagged[index]);
        }
    }

    [Benchmark(Baseline = true)]
    public double BoxedReferences()
    {
        var result = 0d;
        foreach (var value in boxed)
            result += value.DoubleValue;
        return result;
    }

    [Benchmark]
    public double EightByteTaggedScalars()
    {
        var result = 0d;
        foreach (var value in tagged)
            result += value.DoubleValue;
        return result;
    }
}
