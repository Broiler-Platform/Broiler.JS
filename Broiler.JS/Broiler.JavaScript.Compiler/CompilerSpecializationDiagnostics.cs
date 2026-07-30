using System.Threading;

namespace Broiler.JavaScript.Compiler;

public readonly record struct CompilerSpecializationSnapshot(long ScalarLocals, long NumericLocals);

/// <summary>Compilation counters used by Phase 3 tests and benchmark reports.</summary>
public static class CompilerSpecializationDiagnostics
{
    private static long scalarLocals;
    private static long numericLocals;

    internal static void RecordScalarLocal() => Interlocked.Increment(ref scalarLocals);

    /// <summary>A local held as a raw CLR <c>double</c> (docs/performance-roadmap.md P2-2 item 3).</summary>
    internal static void RecordNumericLocal() => Interlocked.Increment(ref numericLocals);

    public static CompilerSpecializationSnapshot Snapshot()
        => new(Interlocked.Read(ref scalarLocals), Interlocked.Read(ref numericLocals));

    public static void Reset()
    {
        Interlocked.Exchange(ref scalarLocals, 0);
        Interlocked.Exchange(ref numericLocals, 0);
    }
}
