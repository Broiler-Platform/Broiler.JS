using System.Threading;

namespace Broiler.JavaScript.Runtime;

/// <summary>
/// Reads of a numeric-tier-refused local that a raw <c>double</c> representation would serve
/// without boxing — the free half of the read/write ratio item 3-1 needs
/// (docs/performance-roadmap.md item 3-1).
/// </summary>
/// <remarks>
/// <para>
/// <b>Only the half that can be counted safely, and the previous attempt is why.</b> Counting every
/// read meant wrapping the local's read expression, and <c>variable.Expression</c> is <em>also</em>
/// the assignment target — so <c>x++</c> compiled to an assignment whose target was a method call
/// and the IL backend rejected it outright. That instrument did not bias the corpus, it crashed it,
/// and it is reverted.
/// </para>
/// <para>
/// <b>The guarded tree's leaf save is the one read position with neither problem.</b> It is the
/// right-hand side of an assignment into a fresh temporary — a value, never a target — and it is
/// emitted by <c>BuildOrderedTree</c>, which runs only <em>after</em> every refusal has been decided
/// on the syntax in <c>TryCreateSpeculativeNumericTree</c>. So a counter here can change neither
/// what compiles nor which trees specialize, and both claims are checked rather than argued: a run
/// with this on must reproduce the roots-consumed-by-a-refused-local count of a run without it.
/// </para>
/// <para>
/// <b>What it establishes, and what it does not.</b> A guarded leaf is exactly the read a raw
/// <c>double</c> would serve for free — the tree already tests the operand for <c>IsNumber</c> and
/// calls <c>DoubleValue</c> on it, so a local that already held a double would skip both. Counting
/// these gives the share of a refused local's traffic that a representation change would <em>not</em>
/// have to pay for. It does not give the ratio: the cost side is every OTHER read, which has no safe
/// hook, so this is a lower bound on reads and says nothing on its own about whether the change
/// wins. It is quoted as what it is.
/// </para>
/// <para>
/// Gated at COMPILE time, like <c>SpeculativeNumericLocals.Counting</c>: with it on, every guarded
/// leaf over a refused local carries a call, so such a run's wall clock means nothing and only its
/// counts do.
/// </para>
/// </remarks>
public static class NumericLocalLeafReadCensus
{
    private const int Buckets = (int)NumericLocalMiss.NeverOffered + 1;

    private static readonly long[] leafReads = new long[Buckets];

    /// <summary>
    /// Whether guarded-leaf reads of a refused local are counted. Read at COMPILE time, so it must
    /// be set before the corpus is compiled. Off by default.
    /// </summary>
    public static bool Enabled;

    /// <summary>
    /// Records one guarded-leaf read of a refused local and hands the value straight back, so the
    /// counter can sit on the right-hand side of the tree's own leaf save.
    /// </summary>
    public static JSValue Record(JSValue value, int miss)
    {
        Interlocked.Increment(ref leafReads[(uint)miss < Buckets ? miss : 0]);
        return value;
    }

    /// <summary>Guarded-leaf reads attributed to one refusal.</summary>
    public static long At(NumericLocalMiss miss)
        => (uint)(int)miss < Buckets ? Interlocked.Read(ref leafReads[(int)miss]) : 0;

    public static void Reset()
    {
        for (var i = 0; i < Buckets; i++)
            Interlocked.Exchange(ref leafReads[i], 0);
    }
}
