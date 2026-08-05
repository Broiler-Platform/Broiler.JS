using System;
using System.Threading;

namespace Broiler.JavaScript.Compiler;

/// <summary>
/// Item 3-9's population counter: how many locals would be numeric if the analysis carried an
/// enclosing function's proven-numeric conclusion across the closure boundary.
/// </summary>
/// <remarks>
/// <para>
/// <b>The item's own specification makes this count its precondition</b>, and the reason is a
/// prediction it also makes: 3-9 <em>does not reach NavierStokes</em>. There the readers of
/// <c>rowSize</c> are hoisted function <em>declarations</em>, so the outer binding is held by item
/// 3-7's correctness conjunct and is not a raw double no matter how far the analysis reaches. So
/// the population is names whose enclosing binding is captured only by function
/// <em>expressions</em> — and nobody has counted how many of those the corpus has.
/// </para>
/// <para>
/// <b>Counting is not building.</b> Five designs in this document have now been retired by their
/// own precondition count, and the sixth — item 3-8a — was built instead and lost 1.2%. What is
/// different here is that a 3-9 name needs no run-time test: the enclosing scope has already
/// <em>proved</em> the outer name is a <c>double</c>, so the local it types is an ordinary numeric
/// local with no flag, no fallback slot, and no read that has to box a raw half back up. 3-8a's
/// failure mode is structurally absent. What is not yet known is whether there is anything to
/// type, which is what this counts.
/// </para>
/// <para>
/// <b>Off by default, and it must be turned on before the code being measured is compiled.</b> A
/// compile-time counter, like <see cref="SpeculativeNumericLocals"/> and unlike the run-time
/// censuses it is reported beside — the mistake of enabling one of these among those has now been
/// made twice in this phase (item 3-1's <c>0083</c>, then item 3-8a's first population instrument),
/// so it is written on the switch rather than left to the caller to remember.
/// </para>
/// </remarks>
public static class ImportedOuterNumerics
{
    public const string EnvironmentVariable = "BROILER_JS_OUTER_NUMERIC_COUNT";

    private static int counting = string.Equals(
        Environment.GetEnvironmentVariable(EnvironmentVariable),
        "1",
        StringComparison.Ordinal)
        ? 1
        : 0;

    /// <summary>Whether each compiled function also reports its imported-outer candidate count.</summary>
    public static bool Counting
    {
        get => Volatile.Read(ref counting) != 0;
        set => Volatile.Write(ref counting, value ? 1 : 0);
    }
}
