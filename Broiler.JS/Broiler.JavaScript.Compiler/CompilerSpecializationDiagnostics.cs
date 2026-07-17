using System.Threading;

namespace Broiler.JavaScript.Compiler;

public readonly record struct CompilerSpecializationSnapshot(long ScalarLocals);

/// <summary>Compilation counters used by Phase 3 tests and benchmark reports.</summary>
public static class CompilerSpecializationDiagnostics
{
    private static long scalarLocals;

    internal static void RecordScalarLocal() => Interlocked.Increment(ref scalarLocals);

    public static CompilerSpecializationSnapshot Snapshot()
        => new(Interlocked.Read(ref scalarLocals));

    public static void Reset() => Interlocked.Exchange(ref scalarLocals, 0);
}
