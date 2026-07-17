using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Broiler.JavaScript.Storage;

namespace Broiler.JavaScript.Engine.Benchmarks;

/// <summary>
/// Phase 2 sparse-storage comparison. The Build methods expose bytes/entry through
/// MemoryDiagnoser; lookup, churn, and enumeration cover the operational trade-offs.
/// </summary>
public class SparseMapBenchmarks
{
    private SAUint32Map<int> radix;
    private Dictionary<uint, int> dictionary;
    private SortedDictionary<uint, int> ordered;
    private InlineThenDictionaryMap inline;
    private SegmentedMap segmented;
    private uint[] keys;
    private uint[] misses;

    [Params(0, 1, 4, 16, 100, 10_000)]
    public int Count { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        radix = new SAUint32Map<int>();
        dictionary = new Dictionary<uint, int>(Count);
        ordered = new SortedDictionary<uint, int>();
        inline = new InlineThenDictionaryMap();
        segmented = new SegmentedMap();
        keys = CreateKeys(Count);
        misses = new uint[keys.Length];
        for (var i = 0; i < keys.Length; i++) misses[i] = keys[i] ^ 0x8000_0000u;
        for (var i = 0; i < keys.Length; i++)
        {
            var key = keys[i];
            radix.Save(key, i);
            dictionary[key] = i;
            ordered[key] = i;
            inline.Set(key, i);
            segmented.Set(key, i);
        }
    }

    [Benchmark(Baseline = true)]
    public int RadixHits()
    {
        var sum = 0;
        foreach (var key in keys)
            if (radix.TryGetValue(key, out var value)) sum += value;
        return sum;
    }

    [Benchmark] public int DictionaryHits() => LookupDictionary(dictionary, keys);
    [Benchmark] public int OrderedHits() => LookupOrdered(ordered, keys);
    [Benchmark] public int InlineHits() => inline.Lookup(keys);
    [Benchmark] public int SegmentedHits() => segmented.Lookup(keys);

    [Benchmark] public int RadixMisses() => LookupRadix(radix, misses);
    [Benchmark] public int DictionaryMisses() => LookupDictionary(dictionary, misses);
    [Benchmark] public int OrderedMisses() => LookupOrdered(ordered, misses);
    [Benchmark] public int InlineMisses() => inline.Lookup(misses);
    [Benchmark] public int SegmentedMisses() => segmented.Lookup(misses);

    [Benchmark]
    public int RadixEnumerate()
    {
        var sum = 0;
        foreach (var (_, value) in radix.AllValues()) sum += value;
        return sum;
    }

    [Benchmark]
    public int DictionaryEnumerate()
    {
        var sum = 0;
        foreach (var pair in dictionary) sum += pair.Value;
        return sum;
    }

    [Benchmark]
    public int OrderedEnumerate()
    {
        var sum = 0;
        foreach (var pair in ordered) sum += pair.Value;
        return sum;
    }

    [Benchmark] public int InlineEnumerate() => inline.EnumerateSum();
    [Benchmark] public int SegmentedEnumerate() => segmented.EnumerateSum();

    [Benchmark]
    public bool RadixChurn()
    {
        const uint key = 4_000_000_001;
        radix.Save(key, 1);
        return radix.RemoveAt(key);
    }

    [Benchmark]
    public bool DictionaryChurn()
    {
        const uint key = 4_000_000_001;
        dictionary[key] = 1;
        return dictionary.Remove(key);
    }

    [Benchmark]
    public bool OrderedChurn()
    {
        const uint key = 4_000_000_001;
        ordered[key] = 1;
        return ordered.Remove(key);
    }

    [Benchmark]
    public bool InlineChurn()
    {
        const uint key = 4_000_000_001;
        inline.Set(key, 1);
        return inline.Remove(key);
    }

    [Benchmark]
    public bool SegmentedChurn()
    {
        const uint key = 4_000_000_001;
        segmented.Set(key, 1);
        return segmented.Remove(key);
    }

    [Benchmark] public SAUint32Map<int> BuildRadix() => CreateRadix(keys);
    [Benchmark] public Dictionary<uint, int> BuildDictionary() => CreateDictionary(keys);
    [Benchmark] public SortedDictionary<uint, int> BuildOrdered() => CreateOrdered(keys);
    [Benchmark] public object BuildInline() => CreateInline(keys);
    [Benchmark] public object BuildSegmented() => CreateSegmented(keys);

    private static uint[] CreateKeys(int count)
    {
        var result = new uint[count];
        uint state = 0x9e37_79b9;
        for (var i = 0; i < count; i++)
        {
            state = state * 1_664_525 + 1_013_904_223;
            result[i] = state;
        }
        return result;
    }

    private static int LookupDictionary(Dictionary<uint, int> map, uint[] source)
    {
        var sum = 0;
        foreach (var key in source)
            if (map.TryGetValue(key, out var value)) sum += value;
        return sum;
    }

    private static int LookupRadix(SAUint32Map<int> map, uint[] source)
    {
        var sum = 0;
        foreach (var key in source)
            if (map.TryGetValue(key, out var value)) sum += value;
        return sum;
    }

    private static int LookupOrdered(SortedDictionary<uint, int> map, uint[] source)
    {
        var sum = 0;
        foreach (var key in source)
            if (map.TryGetValue(key, out var value)) sum += value;
        return sum;
    }

    private static SAUint32Map<int> CreateRadix(uint[] source)
    {
        var map = new SAUint32Map<int>();
        for (var i = 0; i < source.Length; i++) map.Save(source[i], i);
        return map;
    }

    private static Dictionary<uint, int> CreateDictionary(uint[] source)
    {
        var map = new Dictionary<uint, int>(source.Length);
        for (var i = 0; i < source.Length; i++) map[source[i]] = i;
        return map;
    }

    private static SortedDictionary<uint, int> CreateOrdered(uint[] source)
    {
        var map = new SortedDictionary<uint, int>();
        for (var i = 0; i < source.Length; i++) map[source[i]] = i;
        return map;
    }

    private static InlineThenDictionaryMap CreateInline(uint[] source)
    {
        var map = new InlineThenDictionaryMap();
        for (var i = 0; i < source.Length; i++) map.Set(source[i], i);
        return map;
    }

    private static SegmentedMap CreateSegmented(uint[] source)
    {
        var map = new SegmentedMap();
        for (var i = 0; i < source.Length; i++) map.Set(source[i], i);
        return map;
    }

    private sealed class InlineThenDictionaryMap
    {
        private readonly uint[] inlineKeys = new uint[8];
        private readonly int[] inlineValues = new int[8];
        private int count;
        private Dictionary<uint, int> overflow;

        internal void Set(uint key, int value)
        {
            for (var i = 0; i < count; i++)
            {
                if (inlineKeys[i] == key) { inlineValues[i] = value; return; }
            }
            if (count < inlineKeys.Length)
            {
                inlineKeys[count] = key;
                inlineValues[count++] = value;
                return;
            }
            (overflow ??= new Dictionary<uint, int>())[key] = value;
        }

        internal int Lookup(uint[] source)
        {
            var sum = 0;
            foreach (var key in source)
            {
                var found = false;
                for (var i = 0; i < count; i++)
                {
                    if (inlineKeys[i] != key) continue;
                    sum += inlineValues[i];
                    found = true;
                    break;
                }
                if (!found && overflow != null && overflow.TryGetValue(key, out var value)) sum += value;
            }
            return sum;
        }

        internal bool Remove(uint key)
        {
            for (var i = 0; i < count; i++)
            {
                if (inlineKeys[i] != key) continue;
                count--;
                inlineKeys[i] = inlineKeys[count];
                inlineValues[i] = inlineValues[count];
                return true;
            }
            return overflow != null && overflow.Remove(key);
        }

        internal int EnumerateSum()
        {
            var sum = 0;
            for (var i = 0; i < count; i++) sum += inlineValues[i];
            if (overflow != null)
                foreach (var pair in overflow) sum += pair.Value;
            return sum;
        }
    }

    private sealed class SegmentedMap
    {
        private const int Shift = 8;
        private readonly Dictionary<uint, int[]> segments = [];
        private readonly HashSet<uint> present = [];

        internal void Set(uint key, int value)
        {
            var segment = key >> Shift;
            if (!segments.TryGetValue(segment, out var values))
                segments[segment] = values = new int[1 << Shift];
            values[key & ((1 << Shift) - 1)] = value;
            present.Add(key);
        }

        internal int Lookup(uint[] source)
        {
            var sum = 0;
            foreach (var key in source)
                if (present.Contains(key)) sum += segments[key >> Shift][key & ((1 << Shift) - 1)];
            return sum;
        }

        internal bool Remove(uint key)
        {
            if (!present.Remove(key))
                return false;
            segments[key >> Shift][key & ((1 << Shift) - 1)] = 0;
            return true;
        }

        internal int EnumerateSum()
        {
            var sum = 0;
            foreach (var key in present) sum += segments[key >> Shift][key & ((1 << Shift) - 1)];
            return sum;
        }
    }
}
