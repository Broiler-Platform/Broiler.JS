using System.Threading;

namespace Broiler.JavaScript.BuiltIns.Number;

/// <summary>
/// Counts the JavaScript numbers the engine boxes, so phase 3's remaining prize can be bounded
/// rather than argued (docs/performance-roadmap.md item 3-8).
/// </summary>
/// <remarks>
/// <para>
/// Every phase 3 item so far has been sized by a per-shape figure — 31.98 bytes an iteration,
/// over and over — and every one of them has then moved the whole corpus by nothing. The number
/// that would have predicted that is the one nobody had: how much of a real run's allocation is
/// number boxing at all. A raw <c>double</c> local can only ever remove a box, so this count times
/// the box size is the ceiling on 3-0, 3-3, 3-5, 3-7 and 3-8 together — and on 3-1 and 3-2's
/// throughput halves as well.
/// </para>
/// <para>
/// <c>Requests</c> counts every call to <see cref="JSNumber.Create"/>, which is what the compiler
/// emits for a boxing conversion; <c>Cached</c> is the share the small-integer table answers
/// without allocating, and <c>Allocated</c> is the rest. The split matters because a cached hit
/// costs no memory at all, so a probe that counts boxing *operations* and calls the answer bytes
/// overstates it by whatever the table absorbs.
/// </para>
/// <para>
/// Off by default. The check is a plain static read on a branch that predicts perfectly while it
/// is off, which is the same bargain <c>CallPathDiagnostics</c> makes — and unlike that one this
/// sits on an allocation path, so a counter that costs a fraction of the allocation it counts is
/// not worth gating more elaborately.
/// </para>
/// </remarks>
public static class NumberBoxingDiagnostics
{
    private static long requests;
    private static long cached;
    private static long allocated;

    /// <summary>Whether boxing is counted. Off by default.</summary>
    public static bool Enabled;

    internal static void RecordCached()
    {
        Interlocked.Increment(ref requests);
        Interlocked.Increment(ref cached);
    }

    internal static void RecordAllocated()
    {
        Interlocked.Increment(ref requests);
        Interlocked.Increment(ref allocated);
    }

    public static NumberBoxingSnapshot Snapshot()
        => new(
            Interlocked.Read(ref requests),
            Interlocked.Read(ref cached),
            Interlocked.Read(ref allocated));

    public static void Reset()
    {
        Interlocked.Exchange(ref requests, 0);
        Interlocked.Exchange(ref cached, 0);
        Interlocked.Exchange(ref allocated, 0);
    }
}

/// <param name="Requests">Boxing conversions asked for.</param>
/// <param name="Cached">Those the small-integer table answered without allocating.</param>
/// <param name="Allocated">Those that allocated a fresh <see cref="JSNumber"/>.</param>
public readonly record struct NumberBoxingSnapshot(long Requests, long Cached, long Allocated);
