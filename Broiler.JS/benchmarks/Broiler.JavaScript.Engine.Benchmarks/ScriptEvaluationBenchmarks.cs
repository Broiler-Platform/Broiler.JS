using BenchmarkDotNet.Attributes;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Engine.Benchmarks;

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
    private JSContext legacyCacheHitContext;
    private JSContext noCacheContext;

    [GlobalSetup]
    public void Setup()
    {
        cacheHitContext = BenchmarkContext.Create(DictionaryCodeCache.Current);
        legacyCacheHitContext = BenchmarkContext.Create(new LocalDictionaryCodeCache());
        noCacheContext = BenchmarkContext.Create(new NoCodeCache());

        cacheHitContext.Eval(ArithmeticScript, "cache-hit.js");
        legacyCacheHitContext.Eval(ArithmeticScript, "legacy-cache-hit.js");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        cacheHitContext?.Dispose();
        legacyCacheHitContext?.Dispose();
        noCacheContext?.Dispose();
    }

    [Benchmark(Baseline = true)]
    public JSValue EvalProductionCacheHit()
        => cacheHitContext.Eval(ArithmeticScript, "cache-hit.js");

    [Benchmark]
    public JSValue EvalLegacyStringKeyCacheHit()
        => legacyCacheHitContext.Eval(ArithmeticScript, "legacy-cache-hit.js");

    [Benchmark]
    public JSValue EvalWithoutCache()
        => noCacheContext.Eval(ArithmeticScript, "no-cache.js");
}
