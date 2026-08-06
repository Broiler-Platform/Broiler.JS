using System;
using System.Threading;

namespace Broiler.JavaScript.Compiler;

/// <summary>
/// Item 3-1's fourth candidate population: a local the numeric tier refused because a PARAMETER of
/// the enclosing function reaches it (docs/performance-roadmap.md item 3-1).
/// </summary>
/// <remarks>
/// <para>
/// <b>Chosen to test the read cost first, which is the one thing the previous three attempts each
/// measured last.</b> Item 3-8a priced a dual representation at the local and lost on reads;
/// <c>0109</c> priced one on a measured ratio and collected nothing; <c>0110</c> completed the
/// mechanism and lost on the same reads. Every time the cost was the boxes minted reading the
/// dual-representation local, and every time it arrived after the build. <c>0110</c>'s closing note
/// is that the only reliable way found to count it is to build the representation for a candidate
/// population and read <c>boxingSpeculativeReadRequests</c> — so that is what this exists for, and
/// nothing about it is a proposal to ship.
/// </para>
/// <para>
/// <b>Why parameters.</b> Item 3-3 records that <c>parameter</c> is its one category that "cannot
/// reach the numeric tier at all", and item 3-8a deliberately excluded them — "they want a guard at
/// entry rather than at an initializer". <c>0106</c> weighted the refusal at 775 877 boxed writes
/// and <c>0107</c> found their locals unusually tree-resident, 12.89 free leaf reads per write, the
/// most favourable shape of any cause. That last number is exactly the kind that has flattered every
/// previous attempt, which is the reason to count the cost before believing it.
/// </para>
/// <para>
/// <b>`0108`'s refutations of the other causes are void, not confirmed.</b> That patch refused
/// <c>PropertyRead</c>, <c>CallResult</c> and <c>NeverOffered</c> on a consumer-side bound, and
/// <c>0110</c> established the bound was on a different quantity. They are un-measured again, not
/// eliminated.
/// </para>
/// <para>
/// Off by default. Set before the code being measured is compiled, like every switch here.
/// </para>
/// </remarks>
public static class ParameterNumericLocals
{
    public const string EnvironmentVariable = "BROILER_JS_PARAMETER_NUMERIC_LOCALS";

    private static int enabled = string.Equals(
        Environment.GetEnvironmentVariable(EnvironmentVariable),
        "1",
        StringComparison.Ordinal)
        ? 1
        : 0;

    /// <summary>
    /// Whether a local refused because a parameter reaches it is held in item 3-8a's two
    /// representations.
    /// </summary>
    public static bool Enabled
    {
        get => Volatile.Read(ref enabled) != 0;
        set => Volatile.Write(ref enabled, value ? 1 : 0);
    }
}
