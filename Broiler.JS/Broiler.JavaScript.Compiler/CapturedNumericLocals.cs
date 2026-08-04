using System;
using System.Threading;

namespace Broiler.JavaScript.Compiler;

/// <summary>
/// Process-wide switch for item 3-7: whether a numeric local a nested function captures may
/// live in a raw CLR <c>double</c> cell instead of a <see cref="Runtime.JSVariable"/>
/// (docs/performance-roadmap.md item 3-7).
/// </summary>
/// <remarks>
/// <para>
/// Present for the same reason <c>BROILER_JS_DEFER_IL</c> and
/// <c>BROILER_JS_REWRITER_INDEX_THRESHOLD</c> are, and the reason is sharper here: the change
/// has a losing side that no single-arm run can show. A raw double is cheaper to WRITE (no box)
/// and dearer to READ in a generic context (a box per read, where a <c>JSVariable</c> hands back
/// the JSValue it already holds), so a captured local that is read far more often than it is
/// written can come out worse. That trade can only be read off a pair of runs whose builds
/// differ in nothing else.
/// </para>
/// <para>
/// The switch does <em>not</em> gate the soundness condition that goes with the widening — a
/// name a hoisted function declaration mentions is refused on both settings, because that
/// refusal is a correctness rule and not an optimization policy.
/// </para>
/// </remarks>
public static class CapturedNumericLocals
{
    public const string EnvironmentVariable = "BROILER_JS_CAPTURED_NUMERIC_LOCALS";

    private static int enabled = ReadConfigured();

    /// <summary>Whether a captured numeric local is held in a raw <c>double</c>.</summary>
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
