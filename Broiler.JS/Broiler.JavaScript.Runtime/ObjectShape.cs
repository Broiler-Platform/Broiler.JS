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

/// <summary>
/// Counters for validating shape/cache invalidation behavior.
/// <para>
/// Disabled by default. Every counter is an <see cref="Interlocked"/> increment on a
/// process-wide static, and the cache-hit and cache-miss counters sit on the engine's
/// hottest path — one per property read — where they turn a shared static into a
/// contended cache line. Recording is therefore opt-in: set <see cref="Enabled"/> before
/// the code you want to measure, and prefer <see cref="Enable"/> so it is restored.
/// </para>
/// </summary>
public static class PropertyOptimizationDiagnostics
{
    private static long shapeTransitions;
    private static long dictionaryFallbacks;
    private static long cacheHits;
    private static long cacheMisses;
    private static long polymorphicPromotions;
    private static long megamorphicSites;
    private static long prototypeInvalidations;

    /// <summary>
    /// Whether the counters below are recorded. Defaults to <c>false</c>; while it is
    /// <c>false</c> every <c>Record*</c> call is a predictable branch and the snapshot
    /// reports whatever was accumulated while it was last enabled.
    /// <see cref="Reset"/> does not change it.
    /// </summary>
    public static bool Enabled;

    /// <summary>
    /// Enables recording until the returned scope is disposed, restoring the previous
    /// setting. Does not reset the counters — call <see cref="Reset"/> for that.
    /// </summary>
    public static RecordingScope Enable() => new(true);

    /// <summary>Restores <see cref="Enabled"/> when disposed.</summary>
    public readonly struct RecordingScope : IDisposable
    {
        private readonly bool previous;

        internal RecordingScope(bool enabled)
        {
            previous = Enabled;
            Enabled = enabled;
        }

        public void Dispose() => Enabled = previous;
    }

    internal static void RecordShapeTransition() { if (Enabled) Interlocked.Increment(ref shapeTransitions); }
    internal static void RecordDictionaryFallback() { if (Enabled) Interlocked.Increment(ref dictionaryFallbacks); }
    internal static void RecordCacheHit() { if (Enabled) Interlocked.Increment(ref cacheHits); }
    internal static void RecordCacheMiss() { if (Enabled) Interlocked.Increment(ref cacheMisses); }
    internal static void RecordPolymorphicPromotion() { if (Enabled) Interlocked.Increment(ref polymorphicPromotions); }
    internal static void RecordMegamorphic() { if (Enabled) Interlocked.Increment(ref megamorphicSites); }
    internal static void RecordPrototypeInvalidation() { if (Enabled) Interlocked.Increment(ref prototypeInvalidations); }

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
                    if (entry.Holder == null)
                    {
                        if (receiver.TryReadShapeSlot(entry.ShapeId, entry.Slot, out var own))
                        {
                            PropertyOptimizationDiagnostics.RecordCacheHit();
                            return own;
                        }
                    }
                    else if (receiver.CurrentShapeId == entry.ShapeId
                        && ReferenceEquals(receiver.PrototypeChainObject, entry.ReceiverPrototype)
                        && entry.PrototypeVersion == JSObject.PrototypeMutationVersion
                        && entry.Holder.TryReadShapeSlot(entry.HolderShapeId, entry.Slot, out var inherited))
                    {
                        PropertyOptimizationDiagnostics.RecordCacheHit();
                        return inherited;
                    }
                }
            }

            PropertyOptimizationDiagnostics.RecordCacheMiss();
            var result = target[property];

            if (megamorphic || target is not JSObject ordinary || property.Metadata.IsPrivateName)
                return result;

            // A key that is also an array index names an ELEMENT, which the shape does not
            // track and [[Get]] resolves out of the element table instead. Never cache one.
            if (property.Metadata.IsArrayIndex || property.Metadata.IsCanonicalNumericIndex)
                return result;

            if (key == 0)
                key = property.Key;
            else if (key != property.Key)
            {
                BecomeMegamorphic();
                return result;
            }

            if (!TryDescribe(ordinary, in property, out var entryToAdd))
                return result;

            for (var i = 0; i < count; i++)
                if (entries[i].ShapeId == entryToAdd.ShapeId && entries[i].Holder == entryToAdd.Holder)
                    return result;

            if (count == MaxEntries)
            {
                BecomeMegamorphic();
                return result;
            }

            entries[count++] = entryToAdd;
            if (count == 2)
                PropertyOptimizationDiagnostics.RecordPolymorphicPromotion();
            return result;
        }

        /// <summary>Classifies where the property lives, as an own slot or on the chain.</summary>
        private static bool TryDescribe(JSObject receiver, in KeyString property, out Entry entry)
        {
            if (receiver.TryGetShapeSlot(in property, out var shapeId, out var slot))
            {
                entry = new Entry(shapeId, slot, null, null, 0, 0);
                return true;
            }

            // Not own: look for a shape-tracked data slot on the prototype chain.
            //
            // Three things have to hold at read time, and each gets its own guard:
            //  * nothing on the receiver shadows the key — its shape id, since a shape-mode
            //    object's tracked keys are exactly its own named properties;
            //  * the receiver still points at the same prototype — a reference compare,
            //    because two receivers can share a shape id and yet have been created with
            //    different prototypes (`Object.create(a).v=1` vs `Object.create(b).v=1`
            //    reach the same shape), which no mutation counter would ever catch;
            //  * nothing along the chain moved — the global prototype version, which both
            //    [[SetPrototypeOf]] and any property mutation on an object used as a
            //    prototype publish to. Deliberately coarse: one prototype mutation anywhere
            //    retires every prototype entry in the process.
            var receiverShapeId = receiver.GetPrototypeLookupShapeId(in property);
            if (receiverShapeId == 0)
            {
                entry = default;
                return false;
            }

            var receiverPrototype = receiver.PrototypeChainObject;
            var version = JSObject.PrototypeMutationVersion;

            // Walked through the raw chain link rather than the virtual GetPrototypeOf, which
            // would fire a Proxy's getPrototypeOf trap — a visible side effect that must not
            // happen just because a cache is warming up.
            for (var holder = receiverPrototype; holder != null; holder = holder.PrototypeChainObject)
            {
                // Only a plain object resolves by slot read. An exotic or proxied holder has
                // its own [[Get]], so stop rather than reason about what it would return.
                if (holder.GetType() != typeof(JSObject))
                    break;

                if (holder.TryGetShapeSlot(in property, out var holderShapeId, out var holderSlot))
                {
                    entry = new Entry(receiverShapeId, holderSlot, holder, receiverPrototype, holderShapeId, version);
                    return true;
                }

                // Present on this holder, but not as a plain tracked data slot (an accessor,
                // a non-default attribute set, or a lazily-realized cell). Reading it is not
                // a slot read, and it shadows anything further up, so stop here. Tested by
                // presence rather than by value: a holder whose own value IS undefined still
                // shadows, and must not be walked past.
                if (!holder.GetInternalProperty(property, inherited: false).IsEmpty)
                    break;
            }

            entry = default;
            return false;
        }

        private void BecomeMegamorphic()
        {
            if (megamorphic)
                return;
            megamorphic = true;
            PropertyOptimizationDiagnostics.RecordMegamorphic();
        }

        /// <summary>
        /// An own-slot entry has a null <see cref="Holder"/> and reads <see cref="Slot"/> off
        /// the receiver, guarded by <see cref="ShapeId"/> alone. A prototype entry reads
        /// <see cref="Slot"/> off <see cref="Holder"/>, guarded by the receiver's
        /// <see cref="ShapeId"/>, its <see cref="ReceiverPrototype"/> identity, and
        /// <see cref="PrototypeVersion"/>.
        /// </summary>
        private readonly record struct Entry(
            int ShapeId,
            int Slot,
            JSObject Holder,
            JSObject ReceiverPrototype,
            int HolderShapeId,
            long PrototypeVersion);
    }
}
