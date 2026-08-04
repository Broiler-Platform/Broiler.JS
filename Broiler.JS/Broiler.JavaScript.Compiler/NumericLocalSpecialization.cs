using System;
using System.Threading;

namespace Broiler.JavaScript.Compiler;

/// <summary>
/// Process-wide switch for the numeric-local tier as a whole — every raw CLR <c>double</c> local,
/// not one item's increment to them (docs/performance-roadmap.md item 3-8).
/// </summary>
/// <remarks>
/// <para>
/// Items 3-0, 3-3, 3-5 and 3-7 were each measured as a <em>delta</em> against the tier as it stood,
/// and each came out invisible on the Octane corpus: 0.997×, 1.0001×, and so on. Four such
/// readings look like evidence that the mechanism does not matter, and they are not — they are
/// evidence that *eight more names* do not matter. Nobody had ever measured the tier itself,
/// because there was no way to turn it off.
/// </para>
/// <para>
/// This is that control. With it off, every name the analysis proved numeric keeps a
/// <c>JSValue</c>, so the difference between the arms is the whole of what the raw-double
/// representation is worth on a real workload — which is the number item 3-8 has to be sized
/// against, since 3-8 proposes to widen the same tier by an order of magnitude.
/// </para>
/// </remarks>
public static class NumericLocalSpecialization
{
    public const string EnvironmentVariable = "BROILER_JS_NUMERIC_LOCALS";

    private static int enabled = ReadConfigured();

    /// <summary>Whether a local the analysis proved numeric is held in a raw <c>double</c>.</summary>
    public static bool Enabled
    {
        get => Volatile.Read(ref enabled) != 0;
        set => Volatile.Write(ref enabled, value ? 1 : 0);
    }

    private static int ReadConfigured()
        => string.Equals(
            Environment.GetEnvironmentVariable(EnvironmentVariable),
            "0",
            StringComparison.Ordinal)
            ? 0
            : 1;
}
