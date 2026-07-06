using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Engine.Benchmarks;

internal static class BenchmarkContext
{
    public static JSContext Create(ICodeCache codeCache = null)
    {
        var context = new JSContext(experimentalFeatures: JavaScriptFeatureFlags.AllExperimentalEs2026);
        if (codeCache != null)
            context.CodeCache = codeCache;
        return context;
    }
}
