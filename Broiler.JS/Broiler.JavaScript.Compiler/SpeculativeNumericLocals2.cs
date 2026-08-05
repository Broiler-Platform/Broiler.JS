using System;
using System.Threading;

namespace Broiler.JavaScript.Compiler;

/// <summary>
/// Process-wide switch for item 3-8a's dual-representation local.
/// </summary>
/// <remarks>
/// <para>
/// A speculative numeric local is held as a raw <c>double</c> AND a <c>JSValue</c> slot, with a
/// flag saying which is live. It exists because item 3-1's update-target census put 98.1% of the
/// corpus's <c>++</c>/<c>--</c> steps on a local the analysis did not prove numeric, and the
/// population count (<see cref="NumericLocalAnalysis.AnalyzeSpeculative"/>) put 26 such names on
/// the corpus with 15 of them in NavierStokes, which carries 6.76 M of the 7.05 M real update
/// boxes.
/// </para>
/// <para>
/// <b>Off by default.</b> This is the phase's first speculation on a value <em>representation</em>
/// rather than on an operator, so it wants an arm that differs in nothing else before it can be
/// believed — and until it is measured there is no case for paying its losing side, which is a box
/// on any read that cannot consume a raw double.
/// </para>
/// </remarks>
public static class SpeculativeNumericLocals2
{
    public const string EnvironmentVariable = "BROILER_JS_SPECULATIVE_NUMERIC_LOCALS";

    private static int enabled = string.Equals(
        Environment.GetEnvironmentVariable(EnvironmentVariable),
        "1",
        StringComparison.Ordinal)
        ? 1
        : 0;

    /// <summary>Whether a speculative numeric local is held in two representations.</summary>
    public static bool Enabled
    {
        get => Volatile.Read(ref enabled) != 0;
        set => Volatile.Write(ref enabled, value ? 1 : 0);
    }
}
