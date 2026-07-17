using System.Threading;

namespace Broiler.JavaScript.ExpressionCompiler;

public readonly record struct ILSpecializationSnapshot(
    long DenseIntegerSwitches,
    long StringHashSwitches,
    long SwitchTableSlots);

public static class ILSpecializationDiagnostics
{
    private static long denseIntegerSwitches;
    private static long stringHashSwitches;
    private static long switchTableSlots;

    internal static void RecordDenseIntegerSwitch(int slots)
    {
        Interlocked.Increment(ref denseIntegerSwitches);
        Interlocked.Add(ref switchTableSlots, slots);
    }

    internal static void RecordStringHashSwitch(int slots)
    {
        Interlocked.Increment(ref stringHashSwitches);
        Interlocked.Add(ref switchTableSlots, slots);
    }

    public static ILSpecializationSnapshot Snapshot() => new(
        Interlocked.Read(ref denseIntegerSwitches),
        Interlocked.Read(ref stringHashSwitches),
        Interlocked.Read(ref switchTableSlots));

    public static void Reset()
    {
        Interlocked.Exchange(ref denseIntegerSwitches, 0);
        Interlocked.Exchange(ref stringHashSwitches, 0);
        Interlocked.Exchange(ref switchTableSlots, 0);
    }
}
