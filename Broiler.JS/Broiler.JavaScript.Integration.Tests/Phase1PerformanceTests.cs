using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Integration.Tests;

public class Phase1PerformanceTests
{
    [Fact]
    public void ContextsUseIsolatedBoundedCachesByDefault()
    {
        using var first = new JSContext();
        using var second = new JSContext();

        Assert.IsType<DictionaryCodeCache>(first.CodeCache);
        Assert.IsType<DictionaryCodeCache>(second.CodeCache);
        Assert.NotSame(first.CodeCache, second.CodeCache);
    }

    [Fact]
    public void ProcessSharedCacheRequiresExplicitOptIn()
    {
        using var context = new JSContext(options: new JSContextOptions
        {
            UseProcessSharedCodeCache = true
        });

        Assert.Same(DictionaryCodeCache.Current, context.CodeCache);
    }

    [Fact]
    public void CacheKeyIncludesLocationAndCompilationOptions()
    {
        var cache = new DictionaryCodeCache();

        using (var ordinary = new JSContext())
        {
            ordinary.CodeCache = cache;
            ordinary.Eval("40 + 2", "first.js");
            ordinary.Eval("40 + 2", "first.js");
            ordinary.Eval("40 + 2", "second.js");
        }

        using (var scriptHost = new JSContext(options: new JSContextOptions { ScriptHostMode = true }))
        {
            scriptHost.CodeCache = cache;
            scriptHost.Eval("40 + 2", "first.js");
        }

        var metrics = cache.Metrics;
        Assert.Equal(1, metrics.Hits);
        Assert.Equal(3, metrics.Misses);
        Assert.Equal(3, metrics.Compilations);
    }

    [Fact]
    public void CacheEvictsToConfiguredEntryLimit()
    {
        var cache = new DictionaryCodeCache(new DictionaryCodeCacheOptions
        {
            MaxEntries = 2,
            MaxRetainedSourceBytes = 1_000_000,
            MaxEstimatedCodeBytes = 1_000_000
        });

        using var context = new JSContext();
        context.CodeCache = cache;
        context.Eval("1", "one.js");
        context.Eval("2", "two.js");
        context.Eval("3", "three.js");

        Assert.Equal(2, cache.Metrics.Entries);
        Assert.Equal(1, cache.Metrics.Evictions);
    }

    [Fact]
    public void EnvironmentChangesDoNotMutateHostMode()
    {
        var previous = Environment.GetEnvironmentVariable("BROILER_SCRIPT_HOST");
        try
        {
            Environment.SetEnvironmentVariable("BROILER_SCRIPT_HOST", "1");
            using var ordinary = new JSContext();
            Environment.SetEnvironmentVariable("BROILER_SCRIPT_HOST", null);

            Assert.False(ordinary.Options.ScriptHostMode);

            using var scriptHost = new JSContext(options: new JSContextOptions { ScriptHostMode = true });
            Environment.SetEnvironmentVariable("BROILER_SCRIPT_HOST", "0");
            Assert.True(scriptHost.Options.ScriptHostMode);
        }
        finally
        {
            Environment.SetEnvironmentVariable("BROILER_SCRIPT_HOST", previous);
        }
    }
}
