using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Broiler.JavaScript.Storage;

namespace Broiler.JavaScript.Runtime;

/// <summary>
/// What each emitted site actually saw at run time: the receiver shapes at a property read,
/// and the callee identities at a call (docs/performance-roadmap.md item 4-1).
/// </summary>
/// <remarks>
/// <para>
/// This is <em>feedback</em>, not a cache, and the distinction is the whole item. The property
/// inline cache already observes shapes, but it observes them in order to answer the current
/// read: it replaces entries when they go stale, and once a site passes four shapes it becomes
/// megamorphic and stops describing anything at all. Feedback has to <em>retain</em> — a
/// specializing tier needs to know that a site saw exactly one shape across a whole run, which
/// is a claim about history that a structure designed to be overwritten cannot make.
/// </para>
/// <para>
/// It buys nothing on its own. Item 4-1 exists because 4-2 (a specializing tier-2 compile) and
/// 4-4 (inlining small callees at monomorphic sites) both consume it, and neither can be
/// written against a guess. So the deliverable here is the collection plus <em>what it says
/// about the real corpus</em>: the share of executed reads and calls that happen at sites which
/// only ever saw one shape or one callee is the premise those two items rest on, and until this
/// existed nothing in the engine could report it.
/// </para>
/// <para>
/// <b>Two gates, deliberately.</b> Property feedback is recorded behind the runtime
/// <see cref="Enabled"/> flag, inside the site helper that already pays a predictable branch per
/// read for <see cref="PropertyOptimizationDiagnostics"/>. Call feedback is gated at
/// <em>compile</em> time instead: with the flag clear the compiler emits exactly the call it
/// emitted before, so a call carries no extra hop, no extra branch and no extra argument. A call
/// costs ~255 ns and is the path phase 4 exists to fix; adding anything to it unconditionally to
/// measure it would be self-defeating. The cost is that enabling the flag only affects code
/// compiled afterwards — which is what a measurement harness does anyway, and is stated here so
/// nobody reads a partial figure as a whole one.
/// </para>
/// <para>
/// <b>Callee entries hold their functions alive</b> while recording is on. That is acceptable
/// for a measurement run and unacceptable for a shipping default, which is a second reason this
/// is off by default; a consumer that wants feedback in production needs a weaker reference or
/// a stable per-function id first.
/// </para>
/// </remarks>
public static class TypeFeedback
{
    /// <summary>
    /// How many distinct observations a site records before it stops distinguishing them. Four
    /// matches the inline cache's own entry cap, so "overflowed" here and "megamorphic" there
    /// mean the same thing about the same site.
    /// </summary>
    public const int MaxTracked = 4;

    private const int MaxSites = 65_536;

    /// <summary>
    /// Whether feedback is recorded. Defaults to <c>false</c>. Set it before compiling and
    /// running the code under measurement — see the remarks about the compile-time gate on
    /// calls.
    /// </summary>
    public static bool Enabled;

    /// <summary>Enables recording until the returned scope is disposed.</summary>
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

    /// <summary>One site's history. Not thread-safe by design; see <see cref="Record"/>.</summary>
    /// <remarks>
    /// A site is written from whichever thread runs it. Racing writers can lose an observation
    /// or double-count a distinct id, which would matter for a correctness claim and does not
    /// matter for a distribution — and the alternative, a lock or an interlocked compare per
    /// property read, would cost more than the thing being measured. The engine runs
    /// JavaScript on one thread per context, so in the harness this is single-threaded anyway.
    /// Stated rather than assumed.
    /// </remarks>
    public sealed class Site
    {
        private readonly int[] shapeIds = new int[MaxTracked];
        private readonly object[] callees = new object[MaxTracked];

        /// <summary>Distinct observations retained, capped at <see cref="MaxTracked"/>.</summary>
        public int Distinct { get; private set; }

        /// <summary>Whether more distinct observations were seen than the cap retains.</summary>
        public bool Overflowed { get; private set; }

        /// <summary>Total times this site ran while recording was on.</summary>
        public long Observations { get; private set; }

        /// <summary>
        /// Where the FIRST observed shape put the key this site reads — the (key, slot) pair a
        /// specializing tier bakes in as constants (item 4-2b).
        /// </summary>
        /// <remarks>
        /// Only meaningful while the site is monomorphic, which <see cref="Classify"/> decides
        /// and <see cref="TryGetMonomorphicOwnSlot"/> checks before handing it out. Recorded once
        /// — the first time a shape id is admitted — rather than on every read, because an
        /// <c>ObjectShape</c> is immutable and interned by key set, so shape id S maps key K to
        /// slot N for as long as S exists. Re-resolving it per read would cost the trie lookup
        /// the whole item exists to remove.
        /// <para>
        /// <c>DescribedSlot</c> is -1 when the read did not resolve to an own shape slot: the
        /// receiver was not an object, the key lives on the prototype chain, or the key is an
        /// array index or private name (neither of which a shape tracks). A site like that is
        /// observed and classified like any other; it just cannot be specialized this way.
        /// </para>
        /// </remarks>
        public uint DescribedKey { get; private set; }

        /// <inheritdoc cref="DescribedKey"/>
        public int DescribedSlot { get; private set; } = -1;

        /// <summary>Records a shape id, and whether it was newly distinct.</summary>
        internal bool Record(int shapeId)
        {
            Observations++;
            for (var i = 0; i < Distinct; i++)
            {
                if (shapeIds[i] == shapeId)
                    return false;
            }

            if (Distinct == MaxTracked)
            {
                Overflowed = true;
                return false;
            }

            shapeIds[Distinct++] = shapeId;
            return true;
        }

        /// <summary>The single shape this site saw, or 0 when it saw none or more than one.</summary>
        internal int OnlyShapeId => Distinct == 1 && !Overflowed ? shapeIds[0] : 0;

        internal void Describe(uint key, int slot)
        {
            // The FIRST admitted shape only. A polymorphic site is not specialized, so a second
            // description would only ever be read by a site that is about to be refused — and
            // overwriting would make the retained pair depend on execution order, which is the
            // kind of thing that reads as a hit rate and behaves like a coin flip.
            if (DescribedSlot >= 0)
                return;

            DescribedKey = key;
            DescribedSlot = slot;
        }

        internal void RecordCallee(object callee)
        {
            Observations++;
            for (var i = 0; i < Distinct; i++)
            {
                if (ReferenceEquals(callees[i], callee))
                    return;
            }

            if (Distinct == MaxTracked)
            {
                Overflowed = true;
                return;
            }

            callees[Distinct++] = callee;
        }
    }

    /// <summary>
    /// How a site behaved over its whole recorded history — the question 4-2 and 4-4 ask, and
    /// the one the inline cache cannot answer because it forgets.
    /// </summary>
    public enum SiteKind
    {
        /// <summary>Never ran while recording was on.</summary>
        Cold,
        /// <summary>Exactly one shape or callee, ever. The only shape 4-4 can inline.</summary>
        Monomorphic,
        /// <summary>Two to <see cref="MaxTracked"/> distinct, all retained.</summary>
        Polymorphic,
        /// <summary>More distinct than the cap retains.</summary>
        Megamorphic,
    }

    /// <summary>Per-site totals plus the observation counts behind them.</summary>
    public readonly record struct Distribution(
        int ColdSites,
        int MonomorphicSites,
        int PolymorphicSites,
        int MegamorphicSites,
        long MonomorphicObservations,
        long PolymorphicObservations,
        long MegamorphicObservations)
    {
        public int LiveSites => MonomorphicSites + PolymorphicSites + MegamorphicSites;

        public long Observations
            => MonomorphicObservations + PolymorphicObservations + MegamorphicObservations;

        /// <summary>
        /// The share of executed operations that happened at a site which only ever saw one
        /// shape or callee. This is the number 4-2 and 4-4 rest on — weighted by execution, not
        /// by site count, because a specializing tier only pays off where the work is.
        /// </summary>
        public double MonomorphicObservationShare
            => Observations == 0 ? 0 : (double)MonomorphicObservations / Observations;

        public double MonomorphicSiteShare
            => LiveSites == 0 ? 0 : (double)MonomorphicSites / LiveSites;
    }

    private static readonly object allocationLock = new();
    private static Site[] propertySites = new Site[64];
    private static Site[] callSites = new Site[64];
    private static int nextCallSite;

    /// <summary>Allocates a call-site index. Only called while <see cref="Enabled"/>.</summary>
    public static int AllocateCallSite()
    {
        lock (allocationLock)
        {
            if (nextCallSite >= MaxSites)
                return -1;

            var site = nextCallSite++;
            callSites = EnsureCapacity(callSites, site);
            callSites[site] = new Site();
            return site;
        }
    }

    /// <summary>
    /// Records the receiver shape at a property read site, and — the first time a shape is
    /// admitted — where that shape puts the key being read. Called from the property site helper,
    /// which has already tested <see cref="Enabled"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void RecordPropertyRead(int site, JSValue target, in KeyString key)
    {
        if ((uint)site >= MaxSites)
            return;

        var shaped = target as JSObject;
        if (!RentProperty(site).Record(shaped?.CurrentShapeId ?? 0))
            return;

        // Only on the newly-distinct path, which is at most four times per site for the whole
        // run — so the trie lookup here is not on the hot path even while feedback is on.
        Describe(site, shaped, in key);
    }

    private static void Describe(int site, JSObject shaped, in KeyString key)
    {
        if (shaped == null)
            return;

        // The same two exclusions the inline cache applies before it will record an entry: an
        // array index or canonical numeric key names an ELEMENT, which no shape tracks, and a
        // private name is per-class-evaluation. Kept in step deliberately — a specialization the
        // cache would refuse to cache is one nobody has evidence for.
        if (key.Metadata.IsPrivateName || key.Metadata.IsArrayIndex || key.Metadata.IsCanonicalNumericIndex)
            return;

        if (shaped.TryGetShapeSlot(in key, out _, out var slot))
            RentProperty(site).Describe(key.Key, slot);
    }

    /// <summary>
    /// The own-slot read a site can be specialized into, when its whole history is one shape and
    /// that shape resolved the key to an own slot (item 4-2b consumes this).
    /// </summary>
    /// <param name="key">
    /// The key the site was observed reading. It is handed back so the emitted guard can compare
    /// it against the key actually being read: the tier-2 compile addresses a site by ORDINAL
    /// position, and if that mapping is ever off — two threads compiling at once is enough — the
    /// specialization would describe a different property. One integer compare in the guard makes
    /// that a missed optimization instead of a wrong answer.
    /// </param>
    public static bool TryGetMonomorphicOwnSlot(int site, out uint key, out int shapeId, out int slot)
    {
        key = 0;
        shapeId = 0;
        slot = -1;

        var table = Volatile.Read(ref propertySites);
        if ((uint)site >= (uint)table.Length)
            return false;

        var entry = table[site];
        if (entry == null || Classify(entry) != SiteKind.Monomorphic || entry.DescribedSlot < 0)
            return false;

        shapeId = entry.OnlyShapeId;
        if (shapeId == 0)
            return false;

        key = entry.DescribedKey;
        slot = entry.DescribedSlot;
        return true;
    }

    /// <summary>
    /// Records the callee at a call site and returns it, so the recording can sit in the
    /// emitted expression without a temporary.
    /// </summary>
    public static JSValue RecordCallee(int site, JSValue callee)
    {
        if (!Enabled || (uint)site >= MaxSites)
            return callee;

        RentCall(site).RecordCallee(callee);
        return callee;
    }

    private static Site RentProperty(int site)
    {
        var table = Volatile.Read(ref propertySites);
        if ((uint)site < (uint)table.Length && table[site] != null)
            return table[site];

        lock (allocationLock)
        {
            propertySites = EnsureCapacity(propertySites, site);
            return propertySites[site] ??= new Site();
        }
    }

    private static Site RentCall(int site)
    {
        var table = Volatile.Read(ref callSites);
        if ((uint)site < (uint)table.Length && table[site] != null)
            return table[site];

        lock (allocationLock)
        {
            callSites = EnsureCapacity(callSites, site);
            return callSites[site] ??= new Site();
        }
    }

    private static Site[] EnsureCapacity(Site[] table, int site)
    {
        if (site < table.Length)
            return table;

        var length = table.Length;
        while (length <= site)
            length = Math.Min(MaxSites, length * 2);
        var replacement = new Site[length];
        Array.Copy(table, replacement, table.Length);
        return replacement;
    }

    /// <summary>Classifies one site.</summary>
    public static SiteKind Classify(Site site)
        => site == null || site.Observations == 0 ? SiteKind.Cold
            : site.Overflowed ? SiteKind.Megamorphic
            : site.Distinct <= 1 ? SiteKind.Monomorphic
            : SiteKind.Polymorphic;

    /// <summary>The property read sites' distribution.</summary>
    public static Distribution PropertyDistribution() => Summarize(Volatile.Read(ref propertySites));

    /// <summary>The call sites' distribution.</summary>
    public static Distribution CallDistribution() => Summarize(Volatile.Read(ref callSites));

    private static Distribution Summarize(IReadOnlyList<Site> table)
    {
        int cold = 0, mono = 0, poly = 0, mega = 0;
        long monoObs = 0, polyObs = 0, megaObs = 0;

        for (var i = 0; i < table.Count; i++)
        {
            var site = table[i];
            switch (Classify(site))
            {
                case SiteKind.Cold:
                    cold++;
                    break;
                case SiteKind.Monomorphic:
                    mono++;
                    monoObs += site.Observations;
                    break;
                case SiteKind.Polymorphic:
                    poly++;
                    polyObs += site.Observations;
                    break;
                default:
                    mega++;
                    megaObs += site.Observations;
                    break;
            }
        }

        return new Distribution(cold, mono, poly, mega, monoObs, polyObs, megaObs);
    }

    /// <summary>Clears every site's history. Does not change <see cref="Enabled"/>.</summary>
    public static void Reset()
    {
        lock (allocationLock)
        {
            propertySites = new Site[64];
            callSites = new Site[64];
            nextCallSite = 0;
        }
    }
}
