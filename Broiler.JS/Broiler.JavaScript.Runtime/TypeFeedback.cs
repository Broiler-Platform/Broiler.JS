using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

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

        internal void Record(int shapeId)
        {
            Observations++;
            for (var i = 0; i < Distinct; i++)
            {
                if (shapeIds[i] == shapeId)
                    return;
            }

            if (Distinct == MaxTracked)
            {
                Overflowed = true;
                return;
            }

            shapeIds[Distinct++] = shapeId;
        }

        internal void Record(object callee)
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
    /// Records the receiver shape at a property read site. Called from the property site helper,
    /// which has already tested <see cref="Enabled"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void RecordPropertyShape(int site, int shapeId)
    {
        if ((uint)site >= MaxSites)
            return;

        RentProperty(site).Record(shapeId);
    }

    /// <summary>
    /// Records the callee at a call site and returns it, so the recording can sit in the
    /// emitted expression without a temporary.
    /// </summary>
    public static JSValue RecordCallee(int site, JSValue callee)
    {
        if (!Enabled || (uint)site >= MaxSites)
            return callee;

        RentCall(site).Record(callee);
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
