using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Broiler.JavaScript.Runtime;

/// <summary>Per-site totals for speculative guards emitted by the in-method fallback.</summary>
public readonly record struct SpeculationSnapshot(
    int Sites,
    long GuardsMissed,
    int PoisonedSites);

/// <summary>
/// The runtime half of the in-method bailout (docs/performance-roadmap.md item 4-3b): whether a
/// speculation site may still speculate, and what happened when its guard failed.
/// </summary>
/// <remarks>
/// <para>
/// Item 4-3's design spike established that this engine can express two transfers, and only
/// two. <b>Restart</b> — re-enter the unoptimized function from the top — is what the tiering
/// pilot uses, and item 4-3a states the contract it runs under. Restart's limit is that it is
/// only legal while nothing observable has happened yet, which excludes speculating *inside* a
/// body and therefore excludes everything 4-2 and 4-4 want to do.
/// </para>
/// <para>
/// The other transfer is this one: compile the specialized and generic forms into <b>one
/// method</b> and make a failed guard a <b>branch</b>. The CLR locals are shared because it is
/// the same method, so no transfer exists to get wrong; nothing is re-entered, so effects
/// already performed are never repeated; and no <c>CallFrameStack</c> slot changes hands, so
/// the three invariants 4-3a has to preserve are not engaged at all. That is the whole
/// argument, and it is why the branch — not restart — is what 4-4's inlining needs.
/// </para>
/// <para>
/// <b>Poisoning is not a nicety.</b> A guard that keeps failing costs its own evaluation on
/// every execution *plus* the generic path, which is strictly worse than never having
/// speculated. After <see cref="PoisonThreshold"/> failures a site stops speculating and takes
/// the generic path directly. This is a stand-in, and deliberately a cheap one: the right
/// answer once 4-2 exists is to <em>re-emit the method without the guard</em>, since a poisoned
/// site still pays one static array read here. Recorded so the successor knows it is owed.
/// </para>
/// </remarks>
public static class Speculation
{
    /// <summary>
    /// Guard failures a site tolerates before it stops speculating. Four matches the inline
    /// cache's entry cap and item 4-1's tracking cap, so "this site went polymorphic" means the
    /// same thing in all three places.
    /// </summary>
    public const int PoisonThreshold = 4;

    private const int MaxSites = 65_536;

    private static readonly object allocationLock = new();

    // One entry per site. Negative means poisoned; otherwise it is the failure count. Kept as a
    // plain int array so the emitted guard's poison check is a bounds-checked array read rather
    // than a call into a dictionary.
    private static int[] failures = new int[64];
    private static int nextSite;

    private static long guardsFailed;

    /// <summary>Allocates one speculation site index for the compiler to embed as a constant.</summary>
    public static int Allocate()
    {
        lock (allocationLock)
        {
            if (nextSite >= MaxSites)
                return -1;

            var site = nextSite++;
            EnsureCapacity(site);
            return site;
        }
    }

    /// <summary>
    /// Whether <paramref name="site"/> may still speculate. Emitted ahead of the guard so a
    /// poisoned site short-circuits without evaluating it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Allows(int site)
    {
        var table = Volatile.Read(ref failures);
        return (uint)site < (uint)table.Length && table[site] >= 0;
    }

    /// <summary>
    /// Records that <paramref name="site"/>'s guard did not hold, poisoning the site once it
    /// has missed <see cref="PoisonThreshold"/> times. Always returns <c>true</c> so it can sit
    /// in an expression tree without needing a statement position.
    /// </summary>
    /// <remarks>
    /// Only misses are counted, and only while the site is live. Counting the <em>takes</em>
    /// would put an interlocked increment on the specialized path — the one this whole item
    /// exists to make faster — so the snapshot reports misses and poisoned sites and does not
    /// pretend to a hit rate. A poisoned site stops counting too, so the figure stays a count
    /// of real transitions rather than growing with every later execution.
    /// </remarks>
    public static bool OnGuardMissed(int site)
    {
        var table = Volatile.Read(ref failures);
        if ((uint)site >= (uint)table.Length)
            return true;

        // Racing writers can lose a count or poison a site twice. Neither changes an answer —
        // both paths compute the same thing — and the alternative is an interlocked
        // compare-exchange on a path that is already the slow one. Stated rather than assumed.
        var count = table[site];
        if (count < 0)
            return true;

        Interlocked.Increment(ref guardsFailed);
        table[site] = count + 1 >= PoisonThreshold ? -1 : count + 1;
        return true;
    }

    /// <summary>Whether <paramref name="site"/> has stopped speculating.</summary>
    public static bool IsPoisoned(int site)
    {
        var table = Volatile.Read(ref failures);
        return (uint)site < (uint)table.Length && table[site] < 0;
    }

    public static SpeculationSnapshot Snapshot()
    {
        var table = Volatile.Read(ref failures);
        var poisoned = 0;
        var sites = Volatile.Read(ref nextSite);
        for (var i = 0; i < sites && i < table.Length; i++)
        {
            if (table[i] < 0)
                poisoned++;
        }

        return new SpeculationSnapshot(sites, Interlocked.Read(ref guardsFailed), poisoned);
    }

    /// <summary>Clears every site's history and the counters.</summary>
    public static void Reset()
    {
        lock (allocationLock)
        {
            failures = new int[64];
            nextSite = 0;
            Interlocked.Exchange(ref guardsFailed, 0);
        }
    }

    private static void EnsureCapacity(int site)
    {
        if (site < failures.Length)
            return;

        var length = failures.Length;
        while (length <= site)
            length = Math.Min(MaxSites, length * 2);
        var replacement = new int[length];
        Array.Copy(failures, replacement, failures.Length);
        Volatile.Write(ref failures, replacement);
    }
}
