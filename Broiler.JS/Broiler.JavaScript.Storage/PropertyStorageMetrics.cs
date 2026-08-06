using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Broiler.JavaScript.Storage;

/// <summary>
/// How many node groups each <see cref="SAUint32Map{T}"/> ends up allocating, bucketed per
/// value type.
/// </summary>
/// <remarks>
/// <para>
/// Written for docs/performance-roadmap.md item 2-7, which cannot be decided without it. A
/// property map's memory is dominated by a fixed block rather than by per-field storage —
/// <see cref="VirtualMemory{T}.Allocate"/> rounds its first request up to 16 nodes, so one
/// property reserves sixteen descriptors' worth and uses one — and the choice of floor is a
/// trade: a smaller one halves a one-field object and makes an eight-field object worse by
/// paying repeated resize-and-copy. Which side wins is a question about the *distribution* of
/// real objects, and no synthetic probe can answer it.
/// </para>
/// <para>
/// So the shape recorded here is the final group count per map, not a running total. Each
/// allocation moves its map out of the previous bucket and into the next, which leaves
/// <c>histogram[k]</c> holding the number of maps whose life ended at <c>k</c> groups. A map
/// that never allocates — an object with no named properties — appears nowhere, which is
/// correct: it never pays the floor.
/// </para>
/// <para>
/// Disabled by default, and while <see cref="Enabled"/> is <c>false</c> the recording path is
/// not reached at all: <see cref="SAUint32Map{T}"/> tests the flag before reading the counters
/// an allocation would need. Process-wide statics with interlocked updates, so a snapshot taken
/// while other threads are running is a smear rather than a tear.
/// </para>
/// </remarks>
public static class PropertyStorageMetrics
{
    /// <summary>
    /// Group counts above this land in one overflow bucket. 64 groups is 256 nodes; a map that
    /// large is a dictionary-shaped object whose floor is rounding error either way.
    /// </summary>
    public const int MaxTrackedGroups = 64;

    private const int OverflowBucket = MaxTrackedGroups + 1;

    private static readonly ConcurrentDictionary<string, long[]> histograms = new();

    private static long groupAllocations;
    private static long backingArrayResizes;
    private static long nodesCopiedByResizes;

    /// <summary>
    /// Whether the histogram below is recorded. Defaults to <c>false</c>.
    /// <see cref="Reset"/> does not change it.
    /// </summary>
    public static bool Enabled;

    /// <summary>
    /// Enables recording until the returned scope is disposed, restoring the previous setting.
    /// Does not reset the histogram — call <see cref="Reset"/> for that.
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

    /// <summary>
    /// The histogram array for one value type, created on first use. Held by
    /// <see cref="SAUint32Map{T}"/> in a per-instantiation static so the recording path costs a
    /// field read rather than a dictionary lookup.
    /// </summary>
    public static long[] HistogramFor(Type valueType)
        => histograms.GetOrAdd(Describe(valueType), static _ => new long[OverflowBucket + 1]);

    /// <summary>
    /// Moves one map from the bucket for <paramref name="groupsAfter"/> - 1 into the bucket for
    /// <paramref name="groupsAfter"/>. Called only while <see cref="Enabled"/>.
    /// </summary>
    /// <param name="nodesCopiedByResize">
    /// Nodes copied because this allocation had to grow the backing array, or 0 if it fitted.
    /// This is the cost a smaller floor trades *for*, so it is measured rather than assumed.
    /// </param>
    public static void RecordGroupAllocation(long[] histogram, int groupsAfter, int nodesCopiedByResize)
    {
        Interlocked.Increment(ref groupAllocations);

        if (nodesCopiedByResize > 0)
        {
            Interlocked.Increment(ref backingArrayResizes);
            Interlocked.Add(ref nodesCopiedByResizes, nodesCopiedByResize);
        }

        if (histogram == null || groupsAfter < 1)
            return;

        Interlocked.Increment(ref histogram[Math.Min(groupsAfter, OverflowBucket)]);

        if (groupsAfter > 1)
        {
            // Clamping both ends means a map already in the overflow bucket increments and
            // decrements the same slot — it stays counted exactly once, however far it grows.
            //
            // **Clamped at zero, and the clamps counted.** This histogram counts each map once, in
            // the bucket its life ended at, by moving it out of the previous bucket as it grows —
            // which assumes the map's arrival in that bucket was itself counted. A map that was
            // already at `groupsAfter - 1` when `Reset` zeroed the table breaks the assumption: the
            // decrement has nothing to cancel and the bucket goes negative. That was surfaced as
            // `negativeBucketCounts` and left, and it is small (15 against 47 M maps) — but a
            // negative count is not a distribution, and every share computed from the table is
            // divided by a total that includes it. Clamping keeps the table a valid distribution,
            // and ResetStraddlingMaps says exactly how much was dropped doing so — the same bargain
            // the negative count was making, made correctly.
            var slot = Math.Min(groupsAfter - 1, OverflowBucket);
            while (true)
            {
                var current = Interlocked.Read(ref histogram[slot]);
                if (current <= 0)
                {
                    Interlocked.Increment(ref resetStraddlingMaps);
                    break;
                }

                if (Interlocked.CompareExchange(ref histogram[slot], current - 1, current) == current)
                    break;
            }
        }
    }

    private static long resetStraddlingMaps;

    /// <summary>
    /// Decrements refused because the bucket was already empty — maps whose life straddled a
    /// <see cref="Reset"/>, and the exact amount by which this histogram under-counts.
    /// </summary>
    public static long ResetStraddlingMaps => Interlocked.Read(ref resetStraddlingMaps);

    private static long denseWrites;
    private static long denseNumericWrites;
    private static long denseReads;
    private static long denseNumericReads;

    /// <summary>
    /// Classifies a stored element as a JavaScript number. Set by whoever knows what one is —
    /// this assembly does not, since <c>IPropertyValue</c> is an empty marker.
    /// </summary>
    /// <remarks>
    /// A delegate rather than a type test so the Storage assembly keeps no dependency on the
    /// Runtime one, and it is invoked only while <see cref="Enabled"/>.
    /// </remarks>
    public static Func<object, bool> IsNumericValue;

    /// <summary>Writes that landed on the dense backing array.</summary>
    /// <remarks>
    /// <para>
    /// <b>Item 3-1's verdict turns on a ratio nobody had measured.</b> The item trades a write
    /// allocation for a read allocation — a typed <c>double[]</c> stores a number without boxing
    /// and cannot hand one back without boxing — so it is *"a wash at a 1:1 read/write ratio, a
    /// win only when writes dominate, and a loss on read-heavy code"*. The section then asserts
    /// that its named targets *"read each element many times per write, which is the unfavourable
    /// direction"*, which is a claim about the corpus and was never counted.
    /// </para>
    /// <para>
    /// Counted here, on the dense path only, because that is the population a typed store would
    /// serve — a dictionary-kind array is outside the item entirely — and split by whether the
    /// value is a number, because an array of strings or objects would make a corpus-wide ratio
    /// describe arrays the item cannot help.
    /// </para>
    /// </remarks>
    public static long DenseWrites => Interlocked.Read(ref denseWrites);

    /// <summary>Those whose stored value was a number — what a typed store would hold unboxed.</summary>
    public static long DenseNumericWrites => Interlocked.Read(ref denseNumericWrites);

    /// <summary>Reads answered from the dense backing array.</summary>
    public static long DenseReads => Interlocked.Read(ref denseReads);

    /// <summary>Those that handed back a number — what a typed store would have to box.</summary>
    public static long DenseNumericReads => Interlocked.Read(ref denseNumericReads);

    internal static void RecordDenseWrite(object value)
    {
        Interlocked.Increment(ref denseWrites);
        if (IsNumericValue != null && IsNumericValue(value))
            Interlocked.Increment(ref denseNumericWrites);
    }

    internal static void RecordDenseRead(object value)
    {
        Interlocked.Increment(ref denseReads);
        if (IsNumericValue != null && IsNumericValue(value))
            Interlocked.Increment(ref denseNumericReads);
    }

    /// <summary>Zeroes the histogram and the totals. Does not change <see cref="Enabled"/>.</summary>
    public static void Reset()
    {
        foreach (var histogram in histograms.Values)
            Array.Clear(histogram);

        Interlocked.Exchange(ref groupAllocations, 0);
        Interlocked.Exchange(ref backingArrayResizes, 0);
        Interlocked.Exchange(ref nodesCopiedByResizes, 0);
        Interlocked.Exchange(ref resetStraddlingMaps, 0);
        Interlocked.Exchange(ref denseWrites, 0);
        Interlocked.Exchange(ref denseNumericWrites, 0);
        Interlocked.Exchange(ref denseReads, 0);
        Interlocked.Exchange(ref denseNumericReads, 0);
    }

    public static PropertyStorageSnapshot Snapshot()
    {
        var perType = new Dictionary<string, long[]>(histograms.Count);
        foreach (var (valueType, histogram) in histograms)
            perType[valueType] = (long[])histogram.Clone();

        return new PropertyStorageSnapshot(
            Interlocked.Read(ref groupAllocations),
            Interlocked.Read(ref backingArrayResizes),
            Interlocked.Read(ref nodesCopiedByResizes),
            perType);
    }

    /// <summary>
    /// A short name for a generic value type, so `SAUint32Map&lt;JSObjectProperty&gt;` — the one
    /// item 2-7 is about — is distinguishable in the output from the symbol, prototype, global
    /// and event-handler maps that share the implementation.
    /// </summary>
    private static string Describe(Type valueType)
    {
        if (!valueType.IsGenericType)
            return valueType.Name;

        var arguments = valueType.GetGenericArguments();
        var name = valueType.Name;
        var tick = name.IndexOf('`');
        if (tick > 0)
            name = name[..tick];

        return $"{name}<{string.Join(",", Array.ConvertAll(arguments, Describe))}>";
    }
}

/// <param name="GroupAllocations">Total four-node groups handed out while recording.</param>
/// <param name="BackingArrayResizes">
/// Allocations that had to grow the backing array. Under the current 16-node floor the first
/// four groups of a map are free of this; a smaller floor trades memory for more of it.
/// </param>
/// <param name="NodesCopiedByResizes">Nodes copied by those resizes — the trade, in nodes.</param>
/// <param name="FinalGroupCountsByValueType">
/// Per value type, <c>histogram[k]</c> = maps whose life ended at <c>k</c> node groups. Index 0
/// is unused (a map with no allocation is not a map that paid anything) and the last index is
/// the overflow bucket for counts above <see cref="PropertyStorageMetrics.MaxTrackedGroups"/>.
/// </param>
public readonly record struct PropertyStorageSnapshot(
    long GroupAllocations,
    long BackingArrayResizes,
    long NodesCopiedByResizes,
    IReadOnlyDictionary<string, long[]> FinalGroupCountsByValueType);
