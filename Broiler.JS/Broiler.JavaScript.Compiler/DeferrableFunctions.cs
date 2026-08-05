using System;
using System.Threading;

namespace Broiler.JavaScript.Compiler;

/// <summary>
/// Why one function site could not have its body tree built on first call rather than at compile
/// time, without a capture mechanism (docs/performance-roadmap.md item 1-1's remaining half).
/// </summary>
/// <remarks>
/// <para>
/// A waterfall, on the same terms as <see cref="NumericLocalRejection"/>: a site is attributed to
/// the FIRST reason it fails, so the counts add up and each one answers "build this and that many
/// sites become deferrable" rather than overlapping with its neighbours.
/// </para>
/// <para>
/// <b>The order is the item's own.</b> <see cref="Dynamic"/> comes first because it is the one
/// refusal no mechanism removes — a direct <c>eval</c>, a <c>with</c> or a <c>debugger</c> reaches
/// bindings the text never mentions, so no free-name set describes the body and there is nothing
/// to make addressable. <see cref="CapturesEnclosingBinding"/> comes second because it is exactly
/// what the capture mechanism is for: the names are known, and what is missing is their index in
/// the enclosing lambda's <c>Box[]</c>, which <c>LambdaRewriter</c> derives from a tree the
/// deferred body does not have.
/// </para>
/// </remarks>
public enum DeferralRefusal
{
    /// <summary>
    /// Deferrable with no capture mechanism at all: nothing the body references resolves to a
    /// binding any enclosing scope holds, so its creation site needs no <c>Box[]</c> and the one
    /// obstacle item 1-1 names does not arise. Not a refusal; the head of the waterfall.
    /// </summary>
    CaptureFree,

    /// <summary>
    /// The body contains a direct <c>eval</c>, a <c>with</c> or a <c>debugger</c>, so it can reach
    /// a binding it never names. <see cref="FreeNameScan.Dynamic"/>.
    /// </summary>
    Dynamic,

    /// <summary>
    /// At least one free name resolves to a binding an enclosing scope holds. Deferrable only once
    /// the enclosing lambda's box layout can be addressed without the body's tree.
    /// </summary>
    CapturesEnclosingBinding,
}

/// <summary>
/// Item 1-1's remaining-half population counter: how many function sites could have their
/// expression tree deferred to first invocation <em>today</em>, with no capture mechanism built.
/// </summary>
/// <remarks>
/// <para>
/// <b>Counting before building, because this campaign has retired six designs on a precondition
/// count and lost once by skipping one.</b> The remaining half of item 1-1 is worth 33.6-63.9% of
/// compile over a population 84-99.7% of which is never invoked, and it has one named obstacle: a
/// captured name's index in the enclosing lambda's <c>Box[]</c> is decided by
/// <c>LambdaRewriter</c> <em>from the tree</em>, and a deferred body has no tree. <c>0101</c>
/// built and priced the walk that would make the layout addressable. What nobody has counted is
/// <b>how many sites need the layout at all</b> — a function whose free names resolve to nothing
/// an enclosing scope holds captures nothing, is handed no boxes, and can be deferred with the
/// mechanism that already exists.
/// </para>
/// <para>
/// <b>The instrument is deliberately not the one next to it.</b>
/// <c>NestedFunctionScanner</c> collects every identifier a nested function <em>mentions</em>, and
/// <c>NumericLocalAnalysis</c>' captured-name set answers a different question again (what the
/// enclosing function must not scalar-replace). Neither can tell a reference that resolves to an
/// enclosing binding from one that resolves to a global, and that difference is the whole of this
/// count: <c>function () { return jQuery; }</c> inside an IIFE that declares <c>jQuery</c> needs a
/// box, and the identical text at program top level does not.
/// </para>
/// <para>
/// <b>Off by default, and it must be turned on before the code being measured is compiled.</b> A
/// compile-time counter, like <see cref="SpeculativeNumericLocals"/> and
/// <see cref="ImportedOuterNumerics"/> and unlike the run-time censuses it is reported beside —
/// the mistake of enabling one of these among those has now been made twice in phase 3, so it is
/// written on the switch rather than left to the caller to remember. It also costs a
/// <see cref="FreeNameScan"/> per compiled function, which is the per-function shape <c>0101</c>
/// measured as superlinear in nesting depth; that is affordable for a counter nobody ships and is
/// not what a real implementation would do.
/// </para>
/// </remarks>
public static class DeferrableFunctions
{
    public const string EnvironmentVariable = "BROILER_JS_DEFER_TREE_COUNT";

    private static int counting = string.Equals(
        Environment.GetEnvironmentVariable(EnvironmentVariable),
        "1",
        StringComparison.Ordinal)
        ? 1
        : 0;

    /// <summary>Whether each compiled function site is classified for deferrability.</summary>
    public static bool Counting
    {
        get => Volatile.Read(ref counting) != 0;
        set => Volatile.Write(ref counting, value ? 1 : 0);
    }
}
