using System;
using System.Threading;

namespace Broiler.JavaScript.Compiler;

/// <summary>
/// Item 3-1's widening: admit a local the numeric tier refused because an ELEMENT READ reaches it,
/// and the cascade of locals refused only because that one was, into item 3-8a's dual
/// representation (docs/performance-roadmap.md item 3-1).
/// </summary>
/// <remarks>
/// <para>
/// <b>The population is chosen by measurement, which is the whole difference from 3-8a.</b> That
/// item built the same representation for the names refused because a value arrives from OUTSIDE
/// the function, and lost at 1.012–1.021× — it minted 393 705 boxes reading the speculative local
/// against ≈5 300 removed. §3.5 took the rule from that failure: a representation change is priced
/// by the read/write ratio of the code it targets, counted before the representation is built.
/// </para>
/// <para>
/// <b>That ratio has now been counted, and it selects a different population.</b> `0106` weighted
/// the tier's refusals by the boxes they cost rather than by the names they refuse, and `0108`
/// counted the cost side at the consumers, bounded below. Of 16 426 721 boxed writes,
/// <c>ElementRead</c> carries 6 908 985 at a cost/saving of <b>0.04</b> and <c>DroppedCandidate</c>
/// — the cascade — carries 7 300 576 at <b>0.03</b>, while <c>CallResult</c> (9.41),
/// <c>NeverOffered</c> (3.72) and <c>PropertyRead</c> (1.83) are refuted at the bound. So this
/// admits the first two and none of the others, by construction rather than by discovering it
/// later.
/// </para>
/// <para>
/// <b>Speculative, because an element read is not provably numeric.</b> <c>a[i]</c> can hold
/// anything, so the widened names are offered to <c>SpeculativeNumericLocals</c> — a raw
/// <c>double</c>, a flag saying whether it is live, and the <c>JSValue</c> slot to fall back on —
/// and never to the sound tier. The cascade needs no rule of its own: the second pass is a fixed
/// point like the first, so a name kept only because a widened one was kept is kept with it.
/// </para>
/// <para>
/// Off by default and set before the code being measured is compiled, like every other switch in
/// this neighbourhood. It turns on 3-8a's representation for the widened set whether or not
/// <see cref="SpeculativeNumericLocals2"/> is on, so the two populations can be measured apart.
/// </para>
/// </remarks>
public static class ElementReadNumericLocals
{
    public const string EnvironmentVariable = "BROILER_JS_ELEMENT_READ_NUMERIC_LOCALS";

    private static int enabled = string.Equals(
        Environment.GetEnvironmentVariable(EnvironmentVariable),
        "1",
        StringComparison.Ordinal)
        ? 1
        : 0;

    /// <summary>
    /// Whether a local refused for an element read — or by cascade from one — is held in item
    /// 3-8a's two representations.
    /// </summary>
    public static bool Enabled
    {
        get => Volatile.Read(ref enabled) != 0;
        set => Volatile.Write(ref enabled, value ? 1 : 0);
    }
}
