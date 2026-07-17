using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.Storage;

namespace Broiler.JavaScript.Engine.Benchmarks;

internal static class SparseStorageMetrics
{
    private static readonly int[] Sizes = [0, 1, 4, 16, 100, 10_000];

    internal static void Write()
    {
        // Warm JIT/type initialization before allocation samples.
        _ = MeasureRadix([1]);
        _ = MeasureDictionary([1]);
        _ = MeasureOrdered([1]);

        var rows = Sizes.Select(count =>
        {
            var keys = CreateKeys(count);
            var radix = Median(() => MeasureRadix(keys));
            var dictionary = Median(() => MeasureDictionary(keys));
            var ordered = Median(() => MeasureOrdered(keys));
            var denseElementBytes = MeasureElementStorage(count, sparse: false);
            var sparseElementBytes = MeasureElementStorage(count, sparse: true);
            var propertyBytes = MeasurePropertyStorage(count);
            return new
            {
                count,
                radixBytes = radix.Bytes,
                dictionaryBytes = dictionary.Bytes,
                orderedBytes = ordered.Bytes,
                radixNanoseconds = radix.Nanoseconds,
                dictionaryNanoseconds = dictionary.Nanoseconds,
                orderedNanoseconds = ordered.Nanoseconds,
                denseElementBytes,
                denseElementBytesPerEntry = count == 0 ? 0 : (double)denseElementBytes / count,
                sparseElementBytes,
                sparseElementBytesPerEntry = count == 0 ? 0 : (double)sparseElementBytes / count,
                propertyBytes,
                propertyBytesPerEntry = count == 0 ? 0 : (double)propertyBytes / count,
            };
        });

        Console.WriteLine(JsonSerializer.Serialize(new { sizes = rows }));
    }

    private static Sample Median(Func<Sample> measure)
    {
        var samples = new Sample[7];
        for (var i = 0; i < samples.Length; i++) samples[i] = measure();
        Array.Sort(samples, static (a, b) => a.Nanoseconds.CompareTo(b.Nanoseconds));
        return samples[samples.Length / 2];
    }

    private static Sample MeasureRadix(uint[] keys) => Measure(() =>
    {
        var map = new SAUint32Map<int>();
        for (var i = 0; i < keys.Length; i++) map.Save(keys[i], i);
        return map;
    });

    private static Sample MeasureDictionary(uint[] keys) => Measure(() =>
    {
        var map = new Dictionary<uint, int>(keys.Length);
        for (var i = 0; i < keys.Length; i++) map[keys[i]] = i;
        return map;
    });

    private static Sample MeasureOrdered(uint[] keys) => Measure(() =>
    {
        var map = new SortedDictionary<uint, int>();
        for (var i = 0; i < keys.Length; i++) map[keys[i]] = i;
        return map;
    });

    private static long MeasureElementStorage(int count, bool sparse)
    {
        var value = JSNumber.Zero;
        return Measure(() =>
        {
            var elements = new ElementArray();
            for (uint i = 0; i < count; i++)
                elements.Put(sparse ? 2_000_000u + i * 997u : i, value);
            return elements;
        }).Bytes;
    }

    private static long MeasurePropertyStorage(int count)
    {
        var names = new KeyString[count];
        for (var i = 0; i < count; i++) names[i] = KeyStrings.GetOrCreate($"phase2-metric-{i}");
        var value = JSNumber.Zero;
        return Measure(() =>
        {
            var properties = new PropertySequence();
            foreach (var name in names) properties.Put(name, value);
            return properties;
        }).Bytes;
    }

    private static Sample Measure<T>(Func<T> action)
    {
        var before = GC.GetAllocatedBytesForCurrentThread();
        var started = Stopwatch.GetTimestamp();
        var value = action();
        var elapsed = Stopwatch.GetTimestamp() - started;
        var bytes = GC.GetAllocatedBytesForCurrentThread() - before;
        GC.KeepAlive(value);
        return new Sample(bytes, elapsed * 1_000_000_000L / Stopwatch.Frequency);
    }

    private static uint[] CreateKeys(int count)
    {
        var keys = new uint[count];
        uint state = 0x9e37_79b9;
        for (var i = 0; i < keys.Length; i++)
        {
            state = state * 1_664_525 + 1_013_904_223;
            keys[i] = state;
        }
        return keys;
    }

    private readonly record struct Sample(long Bytes, long Nanoseconds);
}
