using System.Threading;

namespace Broiler.JavaScript.Compiler;

public readonly record struct CompilerSpecializationSnapshot(
    long ScalarLocals,
    long NumericLocals,
    long MixedNumericComparisons,
    long BoxedNumericComparisons);

/// <summary>Compilation counters used by Phase 3 tests and benchmark reports.</summary>
public static class CompilerSpecializationDiagnostics
{
    private static long scalarLocals;
    private static long numericLocals;
    private static long mixedNumericComparisons;
    private static long boxedNumericComparisons;

    internal static void RecordScalarLocal() => Interlocked.Increment(ref scalarLocals);

    /// <summary>A local held as a raw CLR <c>double</c> (docs/performance-roadmap.md P2-2 item 3).</summary>
    internal static void RecordNumericLocal() => Interlocked.Increment(ref numericLocals);

    /// <summary>
    /// A <c>&lt;</c> or <c>&gt;</c> emitted with one operand an unboxed double and the other a
    /// <c>JSValue</c>, taking item 3-5's unbox-the-other-side form.
    /// </summary>
    internal static void RecordMixedNumericComparison() => Interlocked.Increment(ref mixedNumericComparisons);

    /// <summary>
    /// A relational comparison that still boxes, because neither operand is an unboxed double —
    /// the denominator for the one above, and what says whether item 3-5's shape is common or rare.
    /// </summary>
    internal static void RecordBoxedNumericComparison() => Interlocked.Increment(ref boxedNumericComparisons);

    public static CompilerSpecializationSnapshot Snapshot()
        => new(
            Interlocked.Read(ref scalarLocals),
            Interlocked.Read(ref numericLocals),
            Interlocked.Read(ref mixedNumericComparisons),
            Interlocked.Read(ref boxedNumericComparisons));

    public static void Reset()
    {
        Interlocked.Exchange(ref scalarLocals, 0);
        Interlocked.Exchange(ref numericLocals, 0);
        Interlocked.Exchange(ref mixedNumericComparisons, 0);
        Interlocked.Exchange(ref boxedNumericComparisons, 0);
    }
}
