using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Broiler.JavaScript.ExpressionCompiler.Runtime;

namespace Broiler.JavaScript.Runtime;

public sealed class DictionaryCodeCacheOptions
{
    public int MaxEntries { get; init; } = 1_024;
    public long MaxRetainedSourceBytes { get; init; } = 64L * 1024 * 1024;
    public long MaxEstimatedCodeBytes { get; init; } = 64L * 1024 * 1024;

    internal void Validate()
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaxEntries);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaxRetainedSourceBytes);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaxEstimatedCodeBytes);
    }
}

public readonly record struct DictionaryCodeCacheMetrics(
    long Hits,
    long Misses,
    long DuplicateWaits,
    long Compilations,
    long CompilationDurationTicks,
    long Evictions,
    int Entries,
    long RetainedSourceBytes,
    long EstimatedCodeBytes);

/// <summary>
/// Bounded, observable LRU cache. Compilation is serialized per structural key by
/// <see cref="Lazy{T}"/>; unrelated scripts compile concurrently.
/// </summary>
public class DictionaryCodeCache : ICodeCache
{
    private readonly ConcurrentDictionary<CodeCacheKey, CacheEntry> cache = new();
    private readonly LinkedList<CodeCacheKey> lru = new();
    private readonly object lruGate = new();
    private readonly DictionaryCodeCacheOptions options;

    private long hits;
    private long misses;
    private long duplicateWaits;
    private long compilations;
    private long compilationDurationTicks;
    private long evictions;
    private long retainedSourceBytes;
    private long estimatedCodeBytes;

    /// <summary>Compatibility process-wide cache. Contexts use isolated caches by default.</summary>
    public static ICodeCache Current = new DictionaryCodeCache();

    public DictionaryCodeCache(DictionaryCodeCacheOptions options = null)
    {
        this.options = options ?? new DictionaryCodeCacheOptions();
        this.options.Validate();
    }

    public DictionaryCodeCacheMetrics Metrics => new(
        Volatile.Read(ref hits),
        Volatile.Read(ref misses),
        Volatile.Read(ref duplicateWaits),
        Volatile.Read(ref compilations),
        Volatile.Read(ref compilationDurationTicks),
        Volatile.Read(ref evictions),
        cache.Count,
        Volatile.Read(ref retainedSourceBytes),
        Volatile.Read(ref estimatedCodeBytes));

    public JSFunctionDelegate GetOrCreate(in JSCode code)
    {
        var key = new CodeCacheKey(in code);
        if (cache.TryGetValue(key, out var cached))
        {
            Interlocked.Increment(ref hits);
            if (!cached.Function.IsValueCreated)
                Interlocked.Increment(ref duplicateWaits);
            Touch(cached);
            return GetValueOrRemove(in key, cached);
        }

        var candidate = CreateEntry(in code);
        var entry = cache.GetOrAdd(key, candidate);
        if (ReferenceEquals(entry, candidate))
        {
            Interlocked.Increment(ref misses);
            Register(in key, entry);
        }
        else
        {
            Interlocked.Increment(ref hits);
            if (!entry.Function.IsValueCreated)
                Interlocked.Increment(ref duplicateWaits);
            Touch(entry);
        }

        return GetValueOrRemove(in key, entry);
    }

    public void Clear()
    {
        lock (lruGate)
        {
            cache.Clear();
            lru.Clear();
            retainedSourceBytes = 0;
            estimatedCodeBytes = 0;
        }
    }

    private CacheEntry CreateEntry(in JSCode code)
    {
        var compiler = code.Compiler;
        var compilationOptions = code.Options;
        var sourceBytes = checked((long)(code.Code.Source?.Length ?? 0) * sizeof(char));
        var estimatedBytes = Math.Max(256L, checked((long)code.Code.Length * 4));

        return new CacheEntry(
            new Lazy<JSFunctionDelegate>(
                () => Compile(compiler, compilationOptions),
                LazyThreadSafetyMode.ExecutionAndPublication),
            sourceBytes,
            estimatedBytes);
    }

    private JSFunctionDelegate Compile(JSCodeCompiler compiler, JSCompilationOptions compilationOptions)
    {
        var started = Stopwatch.GetTimestamp();
        try
        {
            return compiler().CompileWithNestedLambdas(new ExpressionCompilationOptions
            {
                Backend = compilationOptions.Backend,
                EnableJavaScriptTailCalls = compilationOptions.ScriptHostMode
            }).Value;
        }
        finally
        {
            Interlocked.Increment(ref compilations);
            Interlocked.Add(
                ref compilationDurationTicks,
                Stopwatch.GetElapsedTime(started).Ticks);
        }
    }

    private JSFunctionDelegate GetValueOrRemove(in CodeCacheKey key, CacheEntry entry)
    {
        try
        {
            return entry.Function.Value;
        }
        catch
        {
            Remove(in key, entry, countEviction: false);
            throw;
        }
    }

    private void Register(in CodeCacheKey key, CacheEntry entry)
    {
        lock (lruGate)
        {
            if (!cache.TryGetValue(key, out var current) || !ReferenceEquals(current, entry))
                return;

            entry.Node = lru.AddLast(key);
            retainedSourceBytes += entry.SourceBytes;
            estimatedCodeBytes += entry.EstimatedCodeBytes;

            while (cache.Count > options.MaxEntries
                || retainedSourceBytes > options.MaxRetainedSourceBytes
                || estimatedCodeBytes > options.MaxEstimatedCodeBytes)
            {
                var oldest = lru.First;
                if (oldest == null)
                    break;

                if (!cache.TryRemove(oldest.Value, out var removed))
                {
                    lru.RemoveFirst();
                    continue;
                }

                lru.RemoveFirst();
                removed.Node = null;
                retainedSourceBytes -= removed.SourceBytes;
                estimatedCodeBytes -= removed.EstimatedCodeBytes;
                Interlocked.Increment(ref evictions);
            }
        }
    }

    private void Touch(CacheEntry entry)
    {
        lock (lruGate)
        {
            if (entry.Node?.List != lru)
                return;
            lru.Remove(entry.Node);
            lru.AddLast(entry.Node);
        }
    }

    private void Remove(in CodeCacheKey key, CacheEntry entry, bool countEviction)
    {
        lock (lruGate)
        {
            if (!cache.TryGetValue(key, out var current) || !ReferenceEquals(current, entry))
                return;
            if (!cache.TryRemove(key, out _))
                return;

            if (entry.Node?.List == lru)
                lru.Remove(entry.Node);
            entry.Node = null;
            retainedSourceBytes -= entry.SourceBytes;
            estimatedCodeBytes -= entry.EstimatedCodeBytes;
            if (countEviction)
                Interlocked.Increment(ref evictions);
        }
    }

    private sealed class CacheEntry(
        Lazy<JSFunctionDelegate> function,
        long sourceBytes,
        long estimatedCodeBytes)
    {
        internal readonly Lazy<JSFunctionDelegate> Function = function;
        internal readonly long SourceBytes = sourceBytes;
        internal readonly long EstimatedCodeBytes = estimatedCodeBytes;
        internal LinkedListNode<CodeCacheKey> Node;
    }

    private readonly struct CodeCacheKey : IEquatable<CodeCacheKey>
    {
        private readonly string source;
        private readonly int offset;
        private readonly int length;
        private readonly string location;
        private readonly string[] arguments;
        private readonly JSCompilationOptions compilationOptions;
        private readonly int hashCode;

        public CodeCacheKey(in JSCode code)
        {
            source = code.Code.Source ?? string.Empty;
            offset = code.Code.Source == null ? 0 : code.Code.Offset;
            length = code.Code.Source == null ? 0 : code.Code.Length;
            location = code.Location ?? string.Empty;
            compilationOptions = code.Options;
            arguments = CopyArguments(code.Arguments);
            hashCode = ComputeHashCode(
                source,
                offset,
                length,
                location,
                arguments,
                compilationOptions);
        }

        public bool Equals(CodeCacheKey other)
        {
            if (hashCode != other.hashCode
                || length != other.length
                || compilationOptions != other.compilationOptions
                || !string.Equals(location, other.location, StringComparison.Ordinal)
                || arguments.Length != other.arguments.Length
                || !source.AsSpan(offset, length).SequenceEqual(other.source.AsSpan(other.offset, other.length)))
            {
                return false;
            }

            for (var i = 0; i < arguments.Length; i++)
            {
                if (!string.Equals(arguments[i], other.arguments[i], StringComparison.Ordinal))
                    return false;
            }
            return true;
        }

        public override bool Equals(object obj) => obj is CodeCacheKey other && Equals(other);

        public override int GetHashCode() => hashCode;

        private static string[] CopyArguments(IList<string> values)
        {
            if (values == null || values.Count == 0)
                return Array.Empty<string>();
            var copy = new string[values.Count];
            for (var i = 0; i < values.Count; i++)
                copy[i] = values[i] ?? string.Empty;
            return copy;
        }

        private static int ComputeHashCode(
            string source,
            int offset,
            int length,
            string location,
            string[] arguments,
            JSCompilationOptions compilationOptions)
        {
            var hash = new HashCode();
            hash.Add(location, StringComparer.Ordinal);
            hash.Add(compilationOptions);
            foreach (var argument in arguments)
                hash.Add(argument, StringComparer.Ordinal);
            foreach (var ch in source.AsSpan(offset, length))
                hash.Add(ch);
            return hash.ToHashCode();
        }
    }
}
