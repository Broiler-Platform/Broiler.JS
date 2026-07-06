using BenchmarkDotNet.Attributes;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Engine.Benchmarks;

[MemoryDiagnoser]
[ShortRunJob]
public class ScriptEvaluationBenchmarks
{
    private const string ArithmeticScript = """
        (function () {
            var total = 0;
            for (var i = 0; i < 1000; i++) {
                total += ((i * 31) ^ (i >>> 1)) & 255;
            }
            return total;
        })()
        """;

    private JSContext cacheHitContext;
    private JSContext noCacheContext;

    [GlobalSetup]
    public void Setup()
    {
        cacheHitContext = BenchmarkContext.Create(new LocalDictionaryCodeCache());
        noCacheContext = BenchmarkContext.Create(new NoCodeCache());

        cacheHitContext.Eval(ArithmeticScript, "cache-hit.js");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        cacheHitContext?.Dispose();
        noCacheContext?.Dispose();
    }

    [Benchmark(Baseline = true)]
    public JSValue EvalCacheHit()
        => cacheHitContext.Eval(ArithmeticScript, "cache-hit.js");

    [Benchmark]
    public JSValue EvalWithoutCache()
        => noCacheContext.Eval(ArithmeticScript, "no-cache.js");
}
