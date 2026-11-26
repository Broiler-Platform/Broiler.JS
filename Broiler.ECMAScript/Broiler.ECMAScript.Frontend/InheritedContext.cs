// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// HAIRS Code Assurance
// --------------------
// Relevant units:   5
// Annotated:        5/5
// Exempt:           7
// Human-reviewed:   0/5
// IP risk:          Low
// Security risk:    Medium
// Criteria:         0/0
// Resource impact:  1/10 max
// Unverified:       5
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Generic;

namespace Broiler.ECMAScript.Syntax;

/// <summary>
/// The context eval code inherits from its caller (ECMA-262 clause 19.2.1.1, PerformEval steps 6 to 11; Broiler.VM's
/// JSD-0083, decisions D16 and D44): whether the caller is in a function, a method, a derived constructor or a class
/// field initialiser, and optionally the private names visible at the call.
/// From SJL-15.2 stage 5c it also optionally describes a direct <c>eval</c>'s caller's records, the declarative
/// Environment Records between the call and its VariableEnvironment (<see cref="CallerScopes"/>). <i>(Added 2026-11-26, at
/// SJL-15.2 stage 5c.)</i>
/// </summary>
/// <remarks>
/// <para>
/// <b>Strictness is not here.</b> The caller's strictness makes eval code strict from its start (PerformEval step 5), so
/// it reaches the request as its <see cref="ParseSwitches.ForcedStrict"/> switch, where decision D16 maps Broiler.VM's
/// <c>Strict</c> evaluation flag and decision D44 Broiler.JS's strict caller.
/// </para>
/// <para>
/// <b>The private names are optional, and their absence leaves the check to the engine</b> (decisions D16 and D44):
/// <see langword="null"/> means the request does not say which private names the caller sees, so the library refuses no
/// private name as undeclared; a list, empty or not, is every private name visible at the call. From SJL-15.2 stage 4,
/// with <see cref="ParseRequest.BuildSemanticModel"/> on, the semantic model checks an undeclared one against the list
/// (EvalDeclarationInstantiation step 7, AllPrivateIdentifiersValid; catalogue row EA0009); with the member off nothing
/// reads the list, and the engine keeps the check. <i>(Revised 2026-11-25, at SJL-15.2 stage 4, when the model began to
/// check the list, and to give the catalogue's anchor; it read "a list, empty or not, is every private name visible at the
/// call, which an undeclared one is checked against (PerformEval step 7, AllPrivateIdentifiersValid). At SJL-13 neither
/// engine passes one, and the stage table assigns that check (catalogue row EA0009) to SJL-15's model.")</i>
/// </para>
/// <para>
/// <b>The caller's records are optional, and their absence leaves EvalDeclarationInstantiation's step 3 to the engine</b>:
/// <see cref="CallerScopes"/> describes the declarative Environment Records between a direct <c>eval</c> and its
/// caller's VariableEnvironment, each with every name it binds and whether it is a <c>Catch</c> clause's, from which the
/// semantic model refuses a <c>var</c> of eval code that one of them binds (EvalDeclarationInstantiation step 3.d; catalogue
/// rows EA0007 and EA0008) and decides eval code's Annex B aliases; <see langword="null"/>, the default, means the request
/// does not describe the caller, and the engine keeps those checks. It is the member decision D16 leaves "until SJL-15's
/// model"; decisions D16 and D44 are Broiler.VM's and unchanged, and neither engine passes the list yet. <i>(Revised
/// 2026-11-26, at SJL-15.2 stage 5c, when the caller's records joined the request; it read "<b>No caller's lexical
/// names</b> (decision D16): the conflict they serve is EvalDeclarationInstantiation's, a run-time <c>SyntaxError</c> each
/// engine raises, until SJL-15's model.")</i>
/// </para>
/// </remarks>
// Hairs-AI:           Origin=AI; Spec=ECMA-262 s19.2.1.1 PerformEval, JSD-0083 D16 D44; IP=Low; Security=Medium; Resources=0; Fingerprint=82941F
// Hairs-Human:        PENDING
public sealed class InheritedContext
{
    private readonly IReadOnlyList<string>? visiblePrivateNames;
    private readonly IReadOnlyList<CallerScope>? callerScopes;

    /// <summary>
    /// The context of eval code whose caller is outside every function: a script's or a module's top level, and an
    /// indirect eval's. No private-name set, so that check stays the engine's.
    /// It carries no caller's records either (<see cref="CallerScopes"/> is null), so that EvalDeclarationInstantiation's
    /// step 3 stays the engine's too: a direct <c>eval</c> in a Script's top-level block has these flags and a non-empty list.
    /// <i>(Added 2026-11-26, at SJL-15.2 stage 5c.)</i>
    /// </summary>
    public static InheritedContext TopLevel { get; } = new();

    /// <summary>
    /// Whether the caller is in a non-arrow function, so that <c>new.target</c> is admitted (PerformEval steps 7 and 11.e,
    /// <c>inFunction</c>; catalogue row EA0002).
    /// </summary>
    public bool InFunction { get; init; }

    /// <summary>
    /// Whether the caller's environment has a super binding, so that a <c>super</c> property is admitted (PerformEval steps
    /// 7 and 11.f, <c>inMethod</c>; catalogue row EA0003).
    /// </summary>
    public bool InMethod { get; init; }

    /// <summary>
    /// Whether the caller is a derived class's constructor, so that <c>super()</c> is admitted (PerformEval steps 7 and
    /// 11.g, <c>inDerivedConstructor</c>; catalogue row EA0004). Decision D44: computed from the class's heritage, not from
    /// the constructor having member initialisers.
    /// </summary>
    public bool InDerivedConstructor { get; init; }

    /// <summary>
    /// Whether the caller is a class field initialiser, so that <c>arguments</c> is an early error (PerformEval steps 7 and
    /// 11.h, <c>inClassFieldInitializer</c>; catalogue row EA0005).
    /// </summary>
    public bool InClassFieldInitializer { get; init; }

    /// <summary>
    /// The private names visible at the call, each as its StringValue (ECMA-262 clause 15.7.1, <c>#</c> and the
    /// IdentifierName), in the order the caller gives them; or <see langword="null"/>, which leaves the check of an
    /// undeclared private name to the engine. From SJL-15.2 stage 4 a list is read only with
    /// <see cref="ParseRequest.BuildSemanticModel"/> on, where the semantic model refuses eval code's private name that
    /// neither the list nor a class of the eval code declares (catalogue row EA0009). <i>(Added 2026-11-25, at SJL-15.2
    /// stage 4.)</i>
    /// </summary>
    /// <exception cref="ArgumentException">A name is null, or is not <c>#</c> followed by at least one code unit.</exception>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s15.7.1 PrivateIdentifier, JSD-0083 D16 D44 private names; IP=Low; Security=Medium; Resources=1; Fingerprint=EE18DD
    // Hairs-Human:        PENDING
    public IReadOnlyList<string>? VisiblePrivateNames
    {
        get => visiblePrivateNames;
        init => visiblePrivateNames = value is null ? null : Copy(value);
    }

    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s15.7.1 PrivateIdentifier StringValue; IP=Low; Security=Medium; Resources=1; Fingerprint=D72E55
    // Hairs-Human:        PENDING
    private static IReadOnlyList<string> Copy(IReadOnlyList<string> names)
    {
        var copy = new string[names.Count];
        for (var i = 0; i < copy.Length; i++)
        {
            var name = names[i];
            if (name is null || name.Length < 2 || name[0] != '#')
                throw new ArgumentException("A visible private name is '#' followed by its IdentifierName.", nameof(VisiblePrivateNames));
            copy[i] = name;
        }

        return Array.AsReadOnly(copy);
    }

    /// <summary>
    /// The records of a direct <c>eval</c>'s caller (PerformEval steps 16.a and 16.b; EvalDeclarationInstantiation steps 3.d
    /// and 13.b.ii.4; the session's decision D-js-caller-scopes): the declarative Environment Records from the caller's
    /// running LexicalEnvironment outwards, innermost first, up to and not including its VariableEnvironment, each a
    /// <see cref="CallerScope"/> with every name it binds as HasBinding sees it and whether it is a <c>Catch</c> clause's
    /// record; or <see langword="null"/>, the default, which means the request does not describe the caller.
    /// <i>(Added 2026-11-26, at SJL-15.2 stage 5c.)</i>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>What a list holds.</b> Object records are left out, since both EvalDeclarationInstantiation walks skip them (steps
    /// 3.d.i and 13.b.ii.4.a). Each entry carries every name its record binds, not only its lexically declared names: for a
    /// call in a sloppy function's parameter initialiser, the parameters and <c>arguments</c> (FunctionDeclarationInstantiation
    /// steps 20 and 22.d.i); a class's inner name; a <c>catch</c> parameter of either form; a block's functions. A sloppy
    /// function's own <c>var</c>s and top-level functions are in its VariableEnvironment and are never listed (step 32.b).
    /// The order is innermost first, the chain both engines hold; the library's checks do not depend on it.
    /// </para>
    /// <para>
    /// <b>Null, an empty list, and what stays the engine's.</b> With <see langword="null"/> the library refuses no
    /// <c>var</c> for the caller, decides eval code's Annex B aliases from the eval body alone, and leaves
    /// EvalDeclarationInstantiation's steps 3 and 13.b.ii.4 to the engine, as a null <see cref="VisiblePrivateNames"/> leaves
    /// step 7. An empty list is a caller whose LexicalEnvironment is its VariableEnvironment, with no record between; the
    /// request does not tell an indirect <c>eval</c> from a direct one, so an indirect one passes an empty list or null, and
    /// both decide the same. Even with a list these stay the engine's, since they read the realm, not the caller's records:
    /// step 3.a.i.1 (catalogue row EA0006), the TypeErrors of steps 10.a.iv.1 and 12.a.i.1.a, step 13.b.ii.5, and whether a
    /// caller's <c>with</c> object binds a name. Strict eval code reads none of it, whether it is strict by
    /// <see cref="ParseSwitches.ForcedStrict"/> or by its own directive (step 3; PerformEval step 18).
    /// </para>
    /// <para>
    /// The list is copied, and each <see cref="CallerScope"/> copies its names, so that the context stays immutable and the
    /// semantic model may share the instances it is given. With <see cref="ParseRequest.BuildSemanticModel"/> off nothing
    /// reads it.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentException">An element is null.</exception>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s19.2.1.1 PerformEval steps 16.a 16.b, s19.2.1.3 EvalDeclarationInstantiation steps 3.d 13.b.ii.4, JSD-0083 D16; IP=Low; Security=Medium; Resources=1; Fingerprint=DCC51C
    // Hairs-Human:        PENDING
    public IReadOnlyList<CallerScope>? CallerScopes
    {
        get => callerScopes;
        init => callerScopes = value is null ? null : CopyScopes(value);
    }

    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s19.2.1.3 EvalDeclarationInstantiation step 3.d; IP=Low; Security=Medium; Resources=1; Fingerprint=3BD120
    // Hairs-Human:        PENDING
    private static IReadOnlyList<CallerScope> CopyScopes(IReadOnlyList<CallerScope> scopes)
    {
        var copy = new CallerScope[scopes.Count];
        for (var i = 0; i < copy.Length; i++)
            copy[i] = scopes[i] ?? throw new ArgumentException("A caller's record is not null.", nameof(CallerScopes));
        return Array.AsReadOnly(copy);
    }
}
