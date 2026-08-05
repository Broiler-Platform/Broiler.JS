using System;
using System.Threading;

namespace Broiler.JavaScript.Compiler;

/// <summary>
/// Process-wide switch for item 3-1's order-preserving guard placement.
/// </summary>
/// <remarks>
/// <para>
/// The guarded numeric tree (<see cref="NumericSpeculation"/>) hoists every leaf into a temporary
/// ahead of one combined type test, which is only sound when the leaves that move cannot observe
/// anything — so it refuses any tree with an impure leaf after the first internal node. Setting
/// <c>BROILER_JS_NUMERIC_TREE_ORDER=0</c> restores that hoisting form, gate and all, which is the
/// arm every figure in item 3-1's order-preserving section is measured against.
/// </para>
/// <para>
/// Two emitters rather than one because the difference has to be attributable: the switch that
/// already exists turns the whole specialization off, and comparing against <em>that</em> would
/// charge this change for everything the guarded tree does.
/// </para>
/// </remarks>
public static class NumericTreeOrdering
{
    public const string EnvironmentVariable = "BROILER_JS_NUMERIC_TREE_ORDER";

    private static int enabled = ReadConfigured();

    /// <summary>
    /// Whether a guard is emitted where the coercion it replaces would have run, rather than
    /// hoisted ahead of every leaf.
    /// </summary>
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
