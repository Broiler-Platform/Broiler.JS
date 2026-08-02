using System.ComponentModel;

namespace Broiler.JavaScript.Storage;

public struct VirtualMemory<T>
{
    /// <summary>
    /// Smallest reservation worth making — the group size <see cref="SAUint32Map{T}"/> allocates
    /// in, so a map that needs one group gets exactly one.
    /// </summary>
    private const int NodeBlock = 4;

    private T[] nodes = null;
    private int last = 0;

    public readonly bool IsEmpty => Count == 0;

    /// <summary>Number of slots handed out by <see cref="Allocate"/>.</summary>
    public readonly int Count => last;

    public readonly int UsedCount => last;

    public readonly int HighWaterMark => last;

    /// <summary>Number of reserved backing-array slots.</summary>
    public readonly int Capacity => nodes?.Length ?? 0;

    public VirtualMemory() { }

    public readonly ref T this[VirtualArray a, int index] => ref nodes[a.Offset + index];

    [Browsable(false)]
    public readonly ref T GetAt(int index) => ref nodes[index];

    /// <summary>
    /// Reserves <paramref name="length"/> consecutive slots, growing the backing array when they
    /// do not fit.
    /// </summary>
    /// <remarks>
    /// Growth is geometric from the smallest useful size, rather than rounded up to a fixed
    /// multiple. The old rule was <c>((max / 16) + 1) * 16</c>, which reserved 16 slots for the
    /// first request of any size — and since a <see cref="SAUint32Map{T}"/> node is 56 bytes,
    /// that made the first property of any object cost <b>920 B</b> of trie it did not use, and
    /// made one field cost the same as three. Worse, the formula only applies while
    /// <c>last * 2 &lt;= max</c>, so for a map past the first block it grew by a fixed increment
    /// — linearly — instead of doubling.
    /// <para>
    /// Measured over Octane 2.0 (<c>--property-map-distribution</c>, 47 M property maps):
    /// <b>43.9% of all maps end at one four-node group and 87.3% within the old floor</b>, so the
    /// reservation was almost never used. This rule cuts live map bytes to <b>0.56×</b> and
    /// allocated to <b>0.82×</b> across that distribution. Both shares are converged — tripling
    /// the sample moves them by hundredths of a point.
    /// </para>
    /// <para>
    /// It is a real trade and the losing side is real: an eight-field object pays about 27% more
    /// bytes and ~19% more time, because it now resizes and copies where it used to fit. That
    /// tail is small enough to be worth it — no Octane suite regresses measurably, and
    /// Typescript, which has by far the worst tail (a third of its maps outgrow the old floor),
    /// is the one that gains most at 0.92×. See docs/performance-roadmap.md item 2-7.
    /// </para>
    /// </remarks>
    public VirtualArray Allocate(int length)
    {
        var max = last + length;

        if (nodes == null || nodes.Length <= max)
        {
            // we need to resize...
            var capacity = last * 2;
            if (capacity <= max)
                capacity = max < NodeBlock ? NodeBlock : max;

            SetCapacity(capacity);
        }

        var offset = last;
        last += length;
        return new VirtualArray(offset, length);
    }

    public void SetCapacity(int max)
    {
        if (max <= 0)
            return;

        if (nodes == null)
        {
            nodes = new T[max];
            return;
        }

        if (nodes.Length >= max)
            return;

        System.Array.Resize(ref nodes, max);
    }
}

public readonly struct VirtualArray(int offset, int length)
{
    public readonly int Offset = offset;
    public readonly int Length = length;

    public bool IsEmpty => Length == 0;
}
