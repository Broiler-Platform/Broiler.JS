// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   2
// Annotated:        2/2
// Exempt:           4
// Human-reviewed:   0/2
// IP risk:          Low
// Security risk:    Low
// Criteria:         0/0
// Resource impact:  1/10 max
// Unverified:       2
//
// GENERATED - DO NOT EDIT MANUALLY

namespace Broiler.ECMAScript.Syntax;

/// <summary>
/// The request's switches (Broiler.VM's JSD-0080, the request's "Switches"; JSD-0083, decisions D10 and D17): forced
/// strictness, top-level <c>await</c>, Annex B, and HTML-like comments.
/// </summary>
/// <remarks>
/// <para>
/// <b>Each switch is the request's, and none comes from anything ambient</b> (the contract's "no ambient state"). The
/// defaults for a goal are <see cref="For"/>'s, which a request takes when its caller states none; a caller that states
/// switches states all four.
/// </para>
/// <para>
/// <b>The switches a goal does not admit are refused when the request is made</b>
/// (<see cref="ParseRequest.Switches"/>): top-level <c>await</c> for any goal but <see cref="ParseGoal.Script"/> and
/// <see cref="ParseGoal.Module"/>, and HTML-like comments for <see cref="ParseGoal.Module"/>, whose grammar has none
/// (ECMA-262 Annex B.1.1).
/// </para>
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=JSD-0083 D10 D17 switches; IP=Low; Security=Low; Resources=0; Fingerprint=10FF6A
// Broiler-Human:        PENDING
public readonly record struct ParseSwitches
{
    /// <summary>
    /// Whether the code is strict from its first code unit, whatever its directive prologue says (ECMA-262 clause 11.2.2,
    /// strict mode code): the harness's strict variant, the eval code of a strict caller (PerformEval step 5, decision D16),
    /// or a recompile an engine makes of code it knows to be strict. Module code is strict either way.
    /// </summary>
    public bool ForcedStrict { get; init; }

    /// <summary>
    /// Whether <c>await</c> may stand at the top level (decision D10): with it on, the top level is read with the
    /// <c>[Await]</c> parameter, as ECMA-262 clause 16.2 reads a module's; with it off, the library reads the top level as
    /// it reads a non-async function's body of the same goal and reports what it reports there, so catalogue row EX0023
    /// has no diagnostic of its own. It applies to whichever of <see cref="ParseGoal.Script"/> and
    /// <see cref="ParseGoal.Module"/> the request sets it for: Broiler.VM sets it for the Module goal only, Broiler.JS for
    /// a module and for a script whose top level may await.
    /// </summary>
    public bool TopLevelAwait { get; init; }

    /// <summary>
    /// Whether the grammar of ECMA-262 Annex B (Additional ECMAScript Features for Web Browsers) and its relaxations of
    /// sloppy code apply: legacy octal literals and escapes, the regular-expression extensions, labelled function
    /// declarations, function declarations in <c>if</c> clauses and the block-level function semantics' early errors.
    /// On for scripts, as decision D17 says ("Annex B on for scripts"), and on by default for every other goal too, the
    /// <see cref="ParseGoal.Module"/> goal included, which is the library's own choice beyond D17's words: ECMA-262
    /// applies Annex B's regular-expression grammar (B.1.2) in every goal, and the strict-only exclusions follow from
    /// strictness, whatever the switch.
    /// </summary>
    public bool AnnexB { get; init; }

    /// <summary>
    /// Whether <c>&lt;!--</c> and <c>--&gt;</c> start comments (ECMA-262 Annex B.1.1, HTML-like comments): by goal
    /// (decision D17), on for every goal but <see cref="ParseGoal.Module"/>, which admits none. An engine may set it off
    /// for a script, as Broiler.VM's slice path does.
    /// </summary>
    public bool HtmlLikeComments { get; init; }

    /// <summary>The switches a request for <paramref name="goal"/> takes when its caller states none.</summary>
    /// <param name="goal">The goal.</param>
    /// <returns>
    /// Not forced strict; top-level <c>await</c> on for <see cref="ParseGoal.Module"/> alone, as both engines' module
    /// options set it; Annex B on; HTML-like comments on for every goal but <see cref="ParseGoal.Module"/>.
    /// </returns>
    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D10 D17 switch defaults; IP=Low; Security=Low; Resources=1; Fingerprint=DF9001
    // Broiler-Human:        PENDING
    public static ParseSwitches For(ParseGoal goal) => new()
    {
        ForcedStrict = false,
        TopLevelAwait = goal == ParseGoal.Module,
        AnnexB = true,
        HtmlLikeComments = goal != ParseGoal.Module,
    };
}
