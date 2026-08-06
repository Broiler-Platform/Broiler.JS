using System;
using System.Threading;

namespace Broiler.JavaScript.Compiler;

/// <summary>
/// Item 3-1 candidate population: a local the numeric tier refused because a named property read, `o.x` reaches it
/// (docs/performance-roadmap.md item 3-1).
/// </summary>
/// <remarks>
/// <para>
/// <b>Re-opened rather than new.</b> <c>0108</c> refused this cause on a consumer-side bound, and
/// <c>0110</c> established that the bound was on a different quantity — it counted five consumer
/// positions while the cost is a box at every read of a speculative local that is not one of the
/// three raw-capable consumers. So the refusal was withdrawn rather than confirmed, and this exists
/// to answer it by <c>0110</c>'s method: build the representation behind a flag and read
/// <c>boxingSpeculativeReadRequests</c> before anything else.
/// </para>
/// <para>
/// The refusal is worth 925 292 boxed writes (<c>0106</c>), and <c>0107</c> measured its locals at
/// 0.90 free leaf reads per write, the second-lowest of any cause.
/// </para>
/// <para>
/// Off by default, set before the code being measured is compiled, and measured apart from every
/// other population — the read cost is a property of a population and a run that mixes two cannot
/// attribute it.
/// </para>
/// </remarks>
public static class PropertyReadNumericLocals
{
    public const string EnvironmentVariable = "BROILER_JS_PROPERTY_READ_NUMERIC_LOCALS";

    private static int enabled = string.Equals(
        Environment.GetEnvironmentVariable(EnvironmentVariable),
        "1",
        StringComparison.Ordinal)
        ? 1
        : 0;

    /// <summary>Whether such a local is held in item 3-8a's two representations.</summary>
    public static bool Enabled
    {
        get => Volatile.Read(ref enabled) != 0;
        set => Volatile.Write(ref enabled, value ? 1 : 0);
    }
}
