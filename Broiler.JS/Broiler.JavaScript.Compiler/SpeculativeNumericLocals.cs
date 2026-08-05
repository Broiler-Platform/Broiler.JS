using System;
using System.Threading;

namespace Broiler.JavaScript.Compiler;

/// <summary>
/// Item 3-8a's population counter: how many locals would be numeric if a name from outside their
/// function were known to hold a number.
/// </summary>
/// <remarks>
/// <para>
/// <b>Counting is not speculating, and only the counting is built.</b> Item 3-1's update-target
/// census put 98.1% of the corpus's <c>++</c>/<c>--</c> steps on a local the analysis did not prove
/// numeric, and <c>NumericLocalDefeatTests</c> reduced the defeat on that shape to a single
/// identifier — <c>var c = 2 * rowSize</c> is a slot where <c>var c = 2 * 10</c> is a raw double.
/// <see cref="NumericLocalAnalysis.AnalyzeSpeculative"/> reports how many names that costs.
/// </para>
/// <para>
/// <b>Off by default, and it must be turned on before the code being measured is compiled.</b>
/// This is a compile-time counter, unlike every other census in this file's neighbourhood, and the
/// first attempt at it was switched on beside the run-time ones — which run after the corpus has
/// finished compiling — and so reported zero. That is the second time the same mistake has been
/// made here (see item 3-1's <c>0083</c>), which is why it is written on the switch rather than
/// left to the caller to remember.
/// </para>
/// </remarks>
public static class SpeculativeNumericLocals
{
    public const string EnvironmentVariable = "BROILER_JS_SPECULATIVE_NUMERIC_COUNT";

    private static int counting = string.Equals(
        Environment.GetEnvironmentVariable(EnvironmentVariable),
        "1",
        StringComparison.Ordinal)
        ? 1
        : 0;

    /// <summary>Whether each compiled function also reports its speculative-candidate count.</summary>
    public static bool Counting
    {
        get => Volatile.Read(ref counting) != 0;
        set => Volatile.Write(ref counting, value ? 1 : 0);
    }
}
