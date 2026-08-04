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
    private uint[] keysInSlotOrder;

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

    /// <summary>
    /// This shape's keys indexed by slot, which is also their insertion order.
    /// </summary>
    /// <remarks>
    /// <see cref="Add"/> assigns <c>slots[key] = slots.Count</c>, so slot order IS the order
    /// the properties were created in — which is the order OrdinaryOwnPropertyKeys has to
    /// report. That is what lets an object keep its named properties in the shape alone and
    /// rebuild the descriptor map on demand (roadmap item 2-9): the shape supplies the keys
    /// and their order, the object supplies the values and attributes.
    /// <para>
    /// Built once and cached, because a shape is immutable and shared by every object that
    /// reaches it — so the cost is per shape, not per object. Two threads racing here compute
    /// identical arrays and the reference assignment is atomic, so the race is benign and
    /// costs at most one duplicate array.
    /// </para>
    /// </remarks>
    public ReadOnlySpan<uint> KeysInSlotOrder
    {
        get
        {
            var cached = Volatile.Read(ref keysInSlotOrder);
            if (cached != null)
                return cached;

            var keys = new uint[slots.Count];
            foreach (var pair in slots)
                keys[pair.Value] = pair.Key;

            Volatile.Write(ref keysInSlotOrder, keys);
            return keys;
        }
    }

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
    long PrototypeVersion,
    long StoreCacheHits,
    long StoreCacheMisses,
    long StoreMegamorphicSites,
    long NamedPropertiesMaterializations,
    long MissMegamorphic,
    long MissNonObject,
    long MissCold,
    long MissKeyMismatch,
    long MissShape,
    long MissNotDescribable,
    long MissEntryAlreadyPresent);

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
    private static long storeCacheHits;
    private static long storeCacheMisses;
    private static long storeMegamorphicSites;
    private static long namedPropertiesMaterializations;
    private static long missMegamorphic;
    private static long missNonObject;
    private static long missCold;
    private static long missKeyMismatch;
    private static long missShape;
    private static long missNotDescribable;
    private static long missEntryAlreadyPresent;

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
    internal static void RecordStoreCacheHit() { if (Enabled) Interlocked.Increment(ref storeCacheHits); }
    internal static void RecordStoreCacheMiss() { if (Enabled) Interlocked.Increment(ref storeCacheMisses); }
    internal static void RecordStoreMegamorphic() { if (Enabled) Interlocked.Increment(ref storeMegamorphicSites); }
    // Item 2-9's losing-side hypothesis is that a deferred cell forces the trie to be
    // rebuilt; counting the rebuilds is what turns that from a reading of the code into a
    // measurement, and a strict function is the control that says whether the Annex B
    // cells are what causes them.
    internal static void RecordNamedPropertiesMaterialized() { if (Enabled) Interlocked.Increment(ref namedPropertiesMaterializations); }
    // Why a read MISSED, which the bare miss counter cannot say. DeltaBlue sits at a 69%
    // read hit rate against Richards's 99.97%, with megamorphism, dictionary mode and
    // prototype invalidation all ruled out, so the remaining question is which of the
    // lookup's own exits it takes. Recorded only while Enabled, like every counter here.
    internal static void RecordMissMegamorphic() { if (Enabled) Interlocked.Increment(ref missMegamorphic); }
    internal static void RecordMissNonObject() { if (Enabled) Interlocked.Increment(ref missNonObject); }
    internal static void RecordMissCold() { if (Enabled) Interlocked.Increment(ref missCold); }
    internal static void RecordMissKeyMismatch() { if (Enabled) Interlocked.Increment(ref missKeyMismatch); }
    internal static void RecordMissShape() { if (Enabled) Interlocked.Increment(ref missShape); }
    // A miss whose entry could NOT be built: TryDescribe found the key neither in an own
    // shape slot nor on a shape-tracked prototype, so nothing is cached and the same read
    // misses again forever. That is the difference between a site warming up and a site that
    // never can.
    internal static void RecordMissNotDescribable() { if (Enabled) Interlocked.Increment(ref missNotDescribable); }
    internal static void RecordMissEntryAlreadyPresent() { if (Enabled) Interlocked.Increment(ref missEntryAlreadyPresent); }

    public static PropertyOptimizationSnapshot Snapshot() => new(
        Interlocked.Read(ref shapeTransitions),
        Interlocked.Read(ref dictionaryFallbacks),
        Interlocked.Read(ref cacheHits),
        Interlocked.Read(ref cacheMisses),
        Interlocked.Read(ref polymorphicPromotions),
        Interlocked.Read(ref megamorphicSites),
        Interlocked.Read(ref prototypeInvalidations),
        JSObject.PrototypeMutationVersion,
        Interlocked.Read(ref storeCacheHits),
        Interlocked.Read(ref storeCacheMisses),
        Interlocked.Read(ref storeMegamorphicSites),
        Interlocked.Read(ref namedPropertiesMaterializations),
        Interlocked.Read(ref missMegamorphic),
        Interlocked.Read(ref missNonObject),
        Interlocked.Read(ref missCold),
        Interlocked.Read(ref missKeyMismatch),
        Interlocked.Read(ref missShape),
        Interlocked.Read(ref missNotDescribable),
        Interlocked.Read(ref missEntryAlreadyPresent));

    public static void Reset()
    {
        Interlocked.Exchange(ref shapeTransitions, 0);
        Interlocked.Exchange(ref dictionaryFallbacks, 0);
        Interlocked.Exchange(ref cacheHits, 0);
        Interlocked.Exchange(ref cacheMisses, 0);
        Interlocked.Exchange(ref polymorphicPromotions, 0);
        Interlocked.Exchange(ref megamorphicSites, 0);
        Interlocked.Exchange(ref prototypeInvalidations, 0);
        Interlocked.Exchange(ref storeCacheHits, 0);
        Interlocked.Exchange(ref storeCacheMisses, 0);
        Interlocked.Exchange(ref storeMegamorphicSites, 0);
        Interlocked.Exchange(ref namedPropertiesMaterializations, 0);
        Interlocked.Exchange(ref missMegamorphic, 0);
        Interlocked.Exchange(ref missNonObject, 0);
        Interlocked.Exchange(ref missCold, 0);
        Interlocked.Exchange(ref missKeyMismatch, 0);
        Interlocked.Exchange(ref missShape, 0);
        Interlocked.Exchange(ref missNotDescribable, 0);
        Interlocked.Exchange(ref missEntryAlreadyPresent, 0);
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

    public static int Allocate() => SiteTable<PropertyInlineCache>.Allocate();

    /// <summary>
    /// The index <see cref="Allocate"/> would hand out next. Read either side of one function's
    /// body compilation to bound the read sites it emitted, which is how a tier-2 recompile finds
    /// the tier-1 sites whose feedback it consumes (item 4-2b).
    /// </summary>
    /// <remarks>
    /// A bound, not an inventory: the counter is process-wide, so another thread compiling at the
    /// same time interleaves its own sites into the range. That is why the emitted guard compares
    /// the key it was built for against the key actually read — a range that picked up a foreign
    /// site costs a poisoned speculation, never an answer.
    /// </remarks>
    public static int NextReadSite => SiteTable<PropertyInlineCache>.Next;

    public static JSValue Get(int site, JSValue target, KeyString key)
    {
        // Item 4-1. The cache below observes shapes to answer THIS read and forgets them —
        // entries are replaced when stale and dropped entirely at megamorphic — so a
        // specializing tier cannot ask it what a site saw over a whole run. Recorded here,
        // where the site index is in hand, behind the same predictable branch the cache-hit
        // counter on the line below already pays.
        if (TypeFeedback.Enabled)
            TypeFeedback.RecordPropertyRead(site, target, in key);

        if ((uint)site >= MaxSites)
            return target[key];

        return SiteTable<PropertyInlineCache>.Rent(site).Get(target, in key);
    }

    /// <summary>Allocates one bounded store-cache side-table entry for an emitted constant-key write.</summary>
    public static int AllocateStore() => SiteTable<PropertyStoreInlineCache>.Allocate();

    /// <summary>
    /// Performs <c>target[key] = value</c> through the site's store cache and returns
    /// <paramref name="value"/>, so it can stand in for the assignment expression itself.
    /// </summary>
    public static JSValue Set(int site, JSValue target, KeyString key, JSValue value)
    {
        if ((uint)site >= MaxSites)
        {
            target[key] = value;
            return value;
        }

        SiteTable<PropertyStoreInlineCache>.Rent(site).Set(target, in key, value);
        return value;
    }

    /// <summary>
    /// Growable side table of per-emission-site caches, indexed by a compact integer the
    /// compiler embeds as a constant. One table per cache type, so read and write sites are
    /// numbered independently.
    /// </summary>
    private static class SiteTable<TCache>
        where TCache : class, new()
    {
        private static readonly object allocationLock = new();
        private static TCache[] sites = new TCache[64];
        private static int nextSite;

        public static int Next => Volatile.Read(ref nextSite);

        public static int Allocate()
        {
            lock (allocationLock)
            {
                if (nextSite >= MaxSites)
                    return -1;

                var site = nextSite++;
                EnsureCapacity(site);
                sites[site] = new TCache();
                return site;
            }
        }

        /// <summary>
        /// The cache for a site, materializing it if this table was replaced by a code cache
        /// that persisted the site index across compilations.
        /// </summary>
        public static TCache Rent(int site)
        {
            var table = Volatile.Read(ref sites);
            if ((uint)site < (uint)table.Length && table[site] != null)
                return table[site];

            lock (allocationLock)
            {
                EnsureCapacity(site);
                table = sites;
                table[site] ??= new TCache();
                if (nextSite <= site)
                    nextSite = site + 1;
                return table[site];
            }
        }

        private static void EnsureCapacity(int site)
        {
            if (site < sites.Length)
                return;

            var length = sites.Length;
            while (length <= site)
                length = Math.Min(MaxSites, length * 2);
            var replacement = new TCache[length];
            Array.Copy(sites, replacement, sites.Length);
            Volatile.Write(ref sites, replacement);
        }
    }

    /// <summary>
    /// A constant-key, bounded polymorphic own-data-property STORE cache: the write twin of
    /// <see cref="PropertyInlineCache"/>.
    /// </summary>
    /// <remarks>
    /// Only the case a hot loop actually repeats is cached — overwriting an existing own data
    /// property on a receiver whose shape has been seen before. Everything else (creating the
    /// property, an accessor anywhere on the chain, an exotic or proxied receiver, a
    /// dictionary-mode object) falls through to the ordinary indexer, which is also what
    /// installs the entry that makes the NEXT store a hit.
    /// <para>
    /// A hit deliberately never consults strict mode. The flag only decides how a REJECTED
    /// write is reported, and an entry is only taken when the write is known to succeed —
    /// which is what lets the hit path skip the indexer, and with it the
    /// <c>AsyncLocal</c> read that resolving the ambient strict flag costs on every store.
    /// </para>
    /// </remarks>
    private sealed class PropertyStoreInlineCache
    {
        private const int MaxEntries = 4;

        /// <summary>
        /// How many times a site may resolve somewhere the cache cannot describe before it
        /// stops trying. Without it a store that always runs an inherited setter, or always
        /// targets a read-only property, would re-probe and re-attempt an install on every
        /// single write and end up slower than no cache at all.
        /// </summary>
        private const int MaxDeclinedInstalls = 4;

        private readonly Entry[] entries = new Entry[MaxEntries];
        private uint key;
        private int count;
        private int declinedInstalls;
        private bool megamorphic;

        public void Set(JSValue target, in KeyString property, JSValue value)
        {
            if (!megamorphic && target is JSObject receiver && key == property.Key)
            {
                for (var i = 0; i < count; i++)
                {
                    ref readonly var entry = ref entries[i];
                    if (entry.FromShape == null)
                    {
                        if (receiver.TryWriteShapeSlot(entry.ShapeId, entry.Slot, in property, value))
                        {
                            PropertyOptimizationDiagnostics.RecordStoreCacheHit();
                            return;
                        }
                    }
                    else if (ReferenceEquals(receiver.PrototypeChainObject, entry.ReceiverPrototype)
                        && entry.PrototypeVersion == JSObject.PrototypeMutationVersion
                        && receiver.TryCreateShapeSlot(entry.FromShape, entry.ToShape, entry.Slot, in property, value))
                    {
                        PropertyOptimizationDiagnostics.RecordStoreCacheHit();
                        return;
                    }
                }
            }

            // The shape the receiver presents BEFORE the store, so a store that turns out to
            // have created a property can be recorded as the transition it performed. Read
            // here because after the store it is gone.
            var shapeBeforeStore = (target as JSObject)?.TransitionShape;

            PropertyOptimizationDiagnostics.RecordStoreCacheMiss();
            target[property] = value;

            if (megamorphic || target is not JSObject ordinary || property.Metadata.IsPrivateName)
                return;

            // A key that is also an array index names an ELEMENT, which the shape does not
            // track and [[Set]] resolves through the element table instead. Never cache one.
            if (property.Metadata.IsArrayIndex || property.Metadata.IsCanonicalNumericIndex)
                return;

            if (key == 0)
                key = property.Key;
            else if (key != property.Key)
            {
                BecomeMegamorphic();
                return;
            }

            // Read back where the write actually landed. A store that did NOT leave an own,
            // writable, tracked data slot — it ran an inherited setter, was rejected by a
            // read-only or frozen target, or created something the shape cannot describe —
            // has nothing worth recording.
            if (!ordinary.TryGetWritableShapeSlot(in property, out var shapeId, out var slot))
            {
                if (count == 0 && ++declinedInstalls >= MaxDeclinedInstalls)
                    BecomeMegamorphic();
                return;
            }

            var overwriteForm = new Entry(shapeId, slot);
            var entryToAdd = shapeBeforeStore != null && !ReferenceEquals(shapeBeforeStore, ordinary.TransitionShape)
                ? DescribeTransition(ordinary, shapeBeforeStore, in property, slot, overwriteForm)
                : overwriteForm;

            for (var i = 0; i < count; i++)
                if (entries[i].ShapeId == entryToAdd.ShapeId && entries[i].FromShape == entryToAdd.FromShape)
                    return;

            if (count == MaxEntries)
            {
                BecomeMegamorphic();
                return;
            }

            entries[count++] = entryToAdd;
            if (count == 2)
                PropertyOptimizationDiagnostics.RecordPolymorphicPromotion();
        }

        /// <summary>
        /// Describes a store that CREATED its property as the shape transition it performed,
        /// falling back to the plain overwrite form when the creation cannot be replayed safely.
        /// </summary>
        /// <remarks>
        /// The shape changing across the store is what identifies a creation — a shape is
        /// immutable, so a receiver reporting a different one has gained a tracked property, and
        /// the only one it can have gained at this site is this key.
        /// <para>
        /// Replaying it means performing CreateDataProperty directly, which is only the right
        /// answer while OrdinarySetWithOwnDescriptor would still reach it. It would not if the
        /// prototype chain supplied the key: a setter there has to run, and an inherited
        /// non-writable data property has to reject the write. So the chain is walked once here
        /// and required to be free of the key, and two guards keep that answer true at every
        /// later hit — the receiver still pointing at the same prototype, by reference, and the
        /// global prototype-mutation version, which any addition to any object used as a
        /// prototype publishes. Those are the same two the read cache's prototype form uses, and
        /// they are only affordable because 2-0 stopped `new` from advancing the version once
        /// per allocation; before that a transition entry retired on the next object built.
        /// </para>
        /// </remarks>
        private static Entry DescribeTransition(
            JSObject receiver,
            ObjectShape fromShape,
            in KeyString property,
            int slot,
            Entry overwriteForm)
        {
            // Not the shape the transition landed on — the receiver left shape mode during the
            // store, so there is no transition to record. Unreachable as things stand, because
            // the caller has already resolved a writable tracked slot on this receiver, which
            // dictionary mode cannot supply; kept because that is the caller's invariant to
            // hold, not this method's to assume.
            var toShape = receiver.TransitionShape;
            if (toShape == null || !toShape.TryGetSlot(property.Key, out var toSlot) || toSlot != slot)
                return overwriteForm;

            // Walked through the raw chain link rather than GetPrototypeOf, which would fire a
            // Proxy's getPrototypeOf trap — a visible side effect that must not happen just
            // because a cache is warming up.
            for (var holder = receiver.PrototypeChainObject; holder != null; holder = holder.PrototypeChainObject)
            {
                // An exotic or proxied holder has its own [[GetOwnProperty]] and [[Set]], so
                // stop rather than reason about what a creation past it would mean.
                if (holder.GetType() != typeof(JSObject))
                    return overwriteForm;

                // Present anywhere on the chain, in any form: a setter, a read-only data
                // property, or a plain writable one. Only the first two change the outcome, but
                // tested by presence because the cheap test is the conservative one.
                if (!holder.GetInternalProperty(property, inherited: false).IsEmpty)
                    return overwriteForm;
            }

            return new Entry(
                toShape.Id,
                slot,
                fromShape,
                toShape,
                receiver.PrototypeChainObject,
                JSObject.PrototypeMutationVersion);
        }

        private void BecomeMegamorphic()
        {
            if (megamorphic)
                return;
            megamorphic = true;
            PropertyOptimizationDiagnostics.RecordStoreMegamorphic();
        }

        /// <summary>
        /// Where the key's value lives on a receiver of shape <see cref="ShapeId"/>. Unlike a
        /// read entry there is no prototype form: a write that resolves on the chain either
        /// runs a setter or creates an own property, and neither is a slot write on a holder.
        /// <para>
        /// Two forms, discriminated by <see cref="FromShape"/>. An <em>overwrite</em> entry
        /// (null) writes <see cref="Slot"/> on a receiver already of shape
        /// <see cref="ShapeId"/>. A <em>transition</em> entry creates it instead: the receiver
        /// presents <see cref="FromShape"/>, which does not carry the key, and the write moves
        /// it to <see cref="ToShape"/> — guarded additionally by
        /// <see cref="ReceiverPrototype"/> identity and <see cref="PrototypeVersion"/>, since a
        /// creation is only correct while the chain supplies nothing for the key.
        /// </para>
        /// </summary>
        private readonly record struct Entry(
            int ShapeId,
            int Slot,
            ObjectShape FromShape = null,
            ObjectShape ToShape = null,
            JSObject ReceiverPrototype = null,
            long PrototypeVersion = 0);
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
            if (PropertyOptimizationDiagnostics.Enabled)
            {
                if (megamorphic)
                    PropertyOptimizationDiagnostics.RecordMissMegamorphic();
                else if (target is not JSObject)
                    PropertyOptimizationDiagnostics.RecordMissNonObject();
                else if (key == 0)
                    PropertyOptimizationDiagnostics.RecordMissCold();
                else if (key != property.Key)
                    PropertyOptimizationDiagnostics.RecordMissKeyMismatch();
                else
                    PropertyOptimizationDiagnostics.RecordMissShape();
            }
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
            {
                PropertyOptimizationDiagnostics.RecordMissNotDescribable();
                return result;
            }

            for (var i = 0; i < count; i++)
                if (entries[i].ShapeId == entryToAdd.ShapeId && entries[i].Holder == entryToAdd.Holder)
                {
                    PropertyOptimizationDiagnostics.RecordMissEntryAlreadyPresent();

                    // REFRESH, do not decline. ShapeId and Holder identify the entry, but
                    // they are not what a hit checks: the prototype version, the receiver's
                    // prototype identity, and the holder's shape and slot are all guards too,
                    // and any of them can go stale while these two stay equal. Returning here
                    // left the stale entry in place with no way back — the site could never
                    // re-describe it, so it missed on this receiver for the rest of the
                    // process. entryToAdd was just built from the live receiver, so it is by
                    // construction the correct replacement.
                    entries[i] = entryToAdd;
                    return result;
                }

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
