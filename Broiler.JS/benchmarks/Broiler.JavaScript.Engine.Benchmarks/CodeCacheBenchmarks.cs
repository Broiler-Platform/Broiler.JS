using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Engine.Benchmarks;

public class CodeCacheBenchmarks
{
    private const string Source = "(function (a, b) { return a + b; })(20, 22)";

    private JSContext context;
    private LocalDictionaryCodeCache legacyCache;
    private JSCode keyProbe;
    private int missId;

    [GlobalSetup]
    public void Setup()
    {
        context = BenchmarkContext.Create(DictionaryCodeCache.Current);
        legacyCache = new LocalDictionaryCodeCache();

        CoreScript.Compile(Source, "production-cache.js", codeCache: DictionaryCodeCache.Current);
        CoreScript.Compile(Source, "legacy-cache.js", codeCache: legacyCache);

        var span = new StringSpan(Source);
        keyProbe = new JSCode(
            "key-only.js",
            in span,
            new List<string> { "a", "b" },
            () => throw new InvalidOperationException("The key-only benchmark must not compile."));
    }

    [GlobalCleanup]
    public void Cleanup() => context?.Dispose();

    [Benchmark(Baseline = true)]
    public JSFunctionDelegate ProductionStructuralHit()
        => CoreScript.Compile(Source, "production-cache.js", codeCache: DictionaryCodeCache.Current);

    [Benchmark]
    public JSFunctionDelegate LegacyStringKeyHit()
        => CoreScript.Compile(Source, "legacy-cache.js", codeCache: legacyCache);

    [Benchmark]
    public string LegacyKeyMaterialization()
        => keyProbe.Key;

    [Benchmark]
    public void ConcurrentProductionMiss()
    {
        var id = Interlocked.Increment(ref missId);
        var source = $"(function () {{ return {id}; }})()";
        var location = $"concurrent-miss-{id}.js";
        var tasks = new Task[4];
        for (var i = 0; i < tasks.Length; i++)
        {
            tasks[i] = Task.Run(() =>
                CoreScript.Compile(source, location, codeCache: DictionaryCodeCache.Current));
        }

        Task.WaitAll(tasks);
    }
}
