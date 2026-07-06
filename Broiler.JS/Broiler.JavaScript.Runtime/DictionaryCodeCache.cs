using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Broiler.JavaScript.ExpressionCompiler.Runtime;

namespace Broiler.JavaScript.Runtime;

public class DictionaryCodeCache : ICodeCache
{
    private static readonly ConcurrentDictionary<CodeCacheKey, Lazy<JSFunctionDelegate>> cache = new();
    private static readonly object compileLock = new();

    public static ICodeCache Current = new DictionaryCodeCache();

    public JSFunctionDelegate GetOrCreate(in JSCode code)
    {
        var compiler = code.Compiler;
        var key = new CodeCacheKey(in code);
        var entry = cache.GetOrAdd(
            key,
            static (_, compiler) => new Lazy<JSFunctionDelegate>(
                () =>
                {
                    lock (compileLock)
                        return compiler().CompileWithNestedLambdas();
                },
                LazyThreadSafetyMode.ExecutionAndPublication),
            compiler);

        return entry.Value;
    }

    private readonly struct CodeCacheKey : IEquatable<CodeCacheKey>
    {
        private readonly string source;
        private readonly int offset;
        private readonly int length;
        private readonly string argumentsKey;
        private readonly int hashCode;

        public CodeCacheKey(in JSCode code)
        {
            source = code.Code.Source ?? string.Empty;
            offset = code.Code.Source == null ? 0 : code.Code.Offset;
            length = code.Code.Source == null ? 0 : code.Code.Length;
            argumentsKey = CreateArgumentsKey(code.Arguments);
            hashCode = ComputeHashCode(source, offset, length, argumentsKey);
        }

        public bool Equals(CodeCacheKey other)
            => hashCode == other.hashCode
            && length == other.length
            && string.Equals(argumentsKey, other.argumentsKey, StringComparison.Ordinal)
            && source.AsSpan(offset, length).SequenceEqual(other.source.AsSpan(other.offset, other.length));

        public override bool Equals(object obj) => obj is CodeCacheKey other && Equals(other);

        public override int GetHashCode() => hashCode;

        private static string CreateArgumentsKey(IList<string> arguments)
        {
            if (arguments == null || arguments.Count == 0)
                return string.Empty;

            return string.Join(",", arguments);
        }

        private static int ComputeHashCode(string source, int offset, int length, string argumentsKey)
        {
            var hash = new HashCode();
            hash.Add(argumentsKey, StringComparer.Ordinal);

            var code = source.AsSpan(offset, length);
            for (var i = 0; i < code.Length; i++)
                hash.Add(code[i]);

            return hash.ToHashCode();
        }
    }
}
