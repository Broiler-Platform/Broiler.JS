using System.Threading;

namespace Broiler.JavaScript.Runtime;

public readonly record struct StartupOptimizationSnapshot(
    long ContextsCreated,
    long FullProfileContexts,
    long MinimalProfileContexts,
    long LazyCellsCreated,
    long LazyCellsRealized,
    long LazyCellsCanceled,
    long LazyCellFailures,
    long FeatureResolutions,
    long CompatibilityAssemblyProbes);

public static class StartupOptimizationDiagnostics
{
    private static long contextsCreated;
    private static long fullProfileContexts;
    private static long minimalProfileContexts;
    private static long lazyCellsCreated;
    private static long lazyCellsRealized;
    private static long lazyCellsCanceled;
    private static long lazyCellFailures;
    private static long featureResolutions;
    private static long compatibilityAssemblyProbes;

    public static StartupOptimizationSnapshot Snapshot => new(
        Volatile.Read(ref contextsCreated),
        Volatile.Read(ref fullProfileContexts),
        Volatile.Read(ref minimalProfileContexts),
        Volatile.Read(ref lazyCellsCreated),
        Volatile.Read(ref lazyCellsRealized),
        Volatile.Read(ref lazyCellsCanceled),
        Volatile.Read(ref lazyCellFailures),
        Volatile.Read(ref featureResolutions),
        Volatile.Read(ref compatibilityAssemblyProbes));

    public static void Reset()
    {
        Interlocked.Exchange(ref contextsCreated, 0);
        Interlocked.Exchange(ref fullProfileContexts, 0);
        Interlocked.Exchange(ref minimalProfileContexts, 0);
        Interlocked.Exchange(ref lazyCellsCreated, 0);
        Interlocked.Exchange(ref lazyCellsRealized, 0);
        Interlocked.Exchange(ref lazyCellsCanceled, 0);
        Interlocked.Exchange(ref lazyCellFailures, 0);
        Interlocked.Exchange(ref featureResolutions, 0);
        Interlocked.Exchange(ref compatibilityAssemblyProbes, 0);
    }

    public static void RecordContext(bool conformant)
    {
        Interlocked.Increment(ref contextsCreated);
        if (conformant)
            Interlocked.Increment(ref fullProfileContexts);
        else
            Interlocked.Increment(ref minimalProfileContexts);
    }

    internal static void RecordLazyCellCreated() => Interlocked.Increment(ref lazyCellsCreated);
    internal static void RecordLazyCellRealized() => Interlocked.Increment(ref lazyCellsRealized);
    internal static void RecordLazyCellCanceled() => Interlocked.Increment(ref lazyCellsCanceled);
    internal static void RecordLazyCellFailure() => Interlocked.Increment(ref lazyCellFailures);
    public static void RecordFeatureResolution() => Interlocked.Increment(ref featureResolutions);
    public static void RecordCompatibilityAssemblyProbe() => Interlocked.Increment(ref compatibilityAssemblyProbes);
}
