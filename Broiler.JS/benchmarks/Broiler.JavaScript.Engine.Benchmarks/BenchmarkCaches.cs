using System.Collections.Generic;
using Broiler.JavaScript.ExpressionCompiler.Runtime;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Engine.Benchmarks;

internal sealed class LocalDictionaryCodeCache : ICodeCache
{
    private readonly Dictionary<string, JSFunctionDelegate> cache = [];

    public JSFunctionDelegate GetOrCreate(in JSCode code)
    {
        var key = code.Key;
        if (cache.TryGetValue(key, out var compiled))
            return compiled;

        compiled = code.Compiler().CompileWithNestedLambdas();
        cache.Add(key, compiled);
        return compiled;
    }
}

internal sealed class NoCodeCache : ICodeCache
{
    public JSFunctionDelegate GetOrCreate(in JSCode code)
        => code.Compiler().CompileWithNestedLambdas();
}
