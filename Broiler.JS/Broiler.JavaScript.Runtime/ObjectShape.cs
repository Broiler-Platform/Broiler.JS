using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Broiler.JavaScript.Storage;

namespace Broiler.JavaScript.Runtime;

/// <summary>
/// Immutable named-data-property layout shared by ordinary objects. The existing
/// PropertySequence remains the descriptor/order slow-path; contiguous slots are the
/// guarded fast-path used by emitted property inline caches.
/// </summary>
internal sealed class ObjectShape
{
    private static int nextId;
    private readonly Dictionary<uint, int> slots;
    private readonly ConcurrentDictionary<uint, ObjectShape> transitions = new();

    public static readonly ObjectShape Empty = new(new Dictionary<uint, int>(), false);
    public static readonly ObjectShape Dictionary = new(new Dictionary<uint, int>(), true);

    private ObjectShape(Dictionary<uint, int> slots, bool isDictionary)
    {
        Id = Interlocked.Increment(ref nextId);
        this.slots = slots;
        IsDictionary = isDictionary;
    }

    public int Id { get; }
    public int SlotCount => slots.Count;
    public bool IsDictionary { get; }

    public bool TryGetSlot(uint key, out int slot) => slots.TryGetValue(key, out slot);

    public ObjectShape Add(uint key)
    {
        if (IsDictionary || slots.ContainsKey(key))
            return this;

        return transitions.GetOrAdd(key, static (propertyKey, parent) =>
        {
            var next = new Dictionary<uint, int>(parent.slots)
            {
                [propertyKey] = parent.slots.Count,
            };
            PropertyOptimizationDiagnostics.RecordShapeTransition();
            return new ObjectShape(next, false);
        }, this);
    }
}

public readonly record struct PropertyOptimizationSnapshot(
    long ShapeTransitions,
    long DictionaryFallbacks,
    long CacheHits,
    long CacheMisses,
    long PolymorphicPromotions,
    long MegamorphicSites,
    long PrototypeInvalidations,
    long PrototypeVersion);

/// <summary>Low-overhead counters for validating shape/cache invalidation behavior.</summary>
public static class PropertyOptimizationDiagnostics
{
    private static long shapeTransitions;
    private static long dictionaryFallbacks;
    private static long cacheHits;
    private static long cacheMisses;
    private static long polymorphicPromotions;
    private static long megamorphicSites;
    private static long prototypeInvalidations;

    internal static void RecordShapeTransition() => Interlocked.Increment(ref shapeTransitions);
    internal static void RecordDictionaryFallback() => Interlocked.Increment(ref dictionaryFallbacks);
    internal static void RecordCacheHit() => Interlocked.Increment(ref cacheHits);
    internal static void RecordCacheMiss() => Interlocked.Increment(ref cacheMisses);
    internal static void RecordPolymorphicPromotion() => Interlocked.Increment(ref polymorphicPromotions);
    internal static void RecordMegamorphic() => Interlocked.Increment(ref megamorphicSites);
    internal static void RecordPrototypeInvalidation() => Interlocked.Increment(ref prototypeInvalidations);

    public static PropertyOptimizationSnapshot Snapshot() => new(
        Interlocked.Read(ref shapeTransitions),
        Interlocked.Read(ref dictionaryFallbacks),
        Interlocked.Read(ref cacheHits),
        Interlocked.Read(ref cacheMisses),
        Interlocked.Read(ref polymorphicPromotions),
        Interlocked.Read(ref megamorphicSites),
        Interlocked.Read(ref prototypeInvalidations),
        JSObject.PrototypeMutationVersion);

    public static void Reset()
    {
        Interlocked.Exchange(ref shapeTransitions, 0);
        Interlocked.Exchange(ref dictionaryFallbacks, 0);
        Interlocked.Exchange(ref cacheHits, 0);
        Interlocked.Exchange(ref cacheMisses, 0);
        Interlocked.Exchange(ref polymorphicPromotions, 0);
        Interlocked.Exchange(ref megamorphicSites, 0);
        Interlocked.Exchange(ref prototypeInvalidations, 0);
    }
}

/// <summary>
/// A constant-key, bounded polymorphic own-data-property cache. Each emitted site uses
/// a compact integer side-table index; four receiver shapes are retained before the
/// site becomes megamorphic and permanently uses the generic lookup.
/// </summary>
public static class PropertyInlineCacheSite
{
    private const int MaxSites = 65_536;
    private static readonly object allocationLock = new();
    private static PropertyInlineCache[] sites = new PropertyInlineCache[64];
    private static int nextSite;

    public static int Allocate()
    {
        lock (allocationLock)
        {
            if (nextSite >= MaxSites)
                return -1;

            var site = nextSite++;
            EnsureCapacity(site);
            sites[site] = new PropertyInlineCache();
            return site;
        }
    }

    public static JSValue Get(int site, JSValue target, KeyString key)
    {
        if ((uint)site >= MaxSites)
            return target[key];

        var table = Volatile.Read(ref sites);
        if ((uint)site >= (uint)table.Length || table[site] == null)
        {
            lock (allocationLock)
            {
                EnsureCapacity(site);
                table = sites;
                table[site] ??= new PropertyInlineCache();
                if (nextSite <= site)
                    nextSite = site + 1;
            }
        }

        return table[site].Get(target, in key);
    }

    private static void EnsureCapacity(int site)
    {
        if (site < sites.Length)
            return;

        var length = sites.Length;
        while (length <= site)
            length = Math.Min(MaxSites, length * 2);
        var replacement = new PropertyInlineCache[length];
        Array.Copy(sites, replacement, sites.Length);
        Volatile.Write(ref sites, replacement);
    }

    private sealed class PropertyInlineCache
    {
        private const int MaxEntries = 4;
        private readonly Entry[] entries = new Entry[MaxEntries];
        private uint key;
        private int count;
        private bool megamorphic;

        public JSValue Get(JSValue target, in KeyString property)
        {
            if (!megamorphic && target is JSObject receiver && key == property.Key)
            {
                for (var i = 0; i < count; i++)
                {
                    ref readonly var entry = ref entries[i];
                    if (receiver.TryReadShapeSlot(entry.ShapeId, entry.Slot, property.Key, out var value))
                    {
                        PropertyOptimizationDiagnostics.RecordCacheHit();
                        return value;
                    }
                }
            }

            PropertyOptimizationDiagnostics.RecordCacheMiss();
            var result = target[property];

            if (megamorphic || target is not JSObject ordinary || property.Metadata.IsPrivateName)
                return result;

            if (key == 0)
                key = property.Key;
            else if (key != property.Key)
            {
                BecomeMegamorphic();
                return result;
            }

            if (!ordinary.TryGetShapeSlot(in property, out var shapeId, out var slot))
                return result;

            for (var i = 0; i < count; i++)
                if (entries[i].ShapeId == shapeId)
                    return result;

            if (count == MaxEntries)
            {
                BecomeMegamorphic();
                return result;
            }

            entries[count++] = new Entry(shapeId, slot);
            if (count == 2)
                PropertyOptimizationDiagnostics.RecordPolymorphicPromotion();
            return result;
        }

        private void BecomeMegamorphic()
        {
            if (megamorphic)
                return;
            megamorphic = true;
            PropertyOptimizationDiagnostics.RecordMegamorphic();
        }

        private readonly record struct Entry(int ShapeId, int Slot);
    }
}
