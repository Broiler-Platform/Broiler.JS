// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// HAIRS Code Assurance
// --------------------
// Relevant units:   3
// Annotated:        3/3
// Exempt:           6
// Human-reviewed:   0/3
// IP risk:          Low
// Security risk:    Medium
// Criteria:         0/0
// Resource impact:  1/10 max
// Unverified:       3
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Generic;

namespace Broiler.ECMAScript.Syntax;

/// <summary>
/// The context eval code inherits from its caller (ECMA-262 clause 19.2.1.1, PerformEval steps 6 to 11; Broiler.VM's
/// JSD-0083, decisions D16 and D44): whether the caller is in a function, a method, a derived constructor or a class
/// field initialiser, and optionally the private names visible at the call.
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
/// private name as undeclared; a list, empty or not, is every private name visible at the call, which an undeclared one
/// is checked against (PerformEval step 7, AllPrivateIdentifiersValid). At SJL-13 neither engine passes one, and the
/// stage table assigns that check (catalogue row EA0009) to SJL-15's model.
/// </para>
/// <para>
/// <b>No caller's lexical names</b> (decision D16): the conflict they serve is EvalDeclarationInstantiation's, a run-time
/// <c>SyntaxError</c> each engine raises, until SJL-15's model.
/// </para>
/// </remarks>
// Hairs-AI:           Origin=AI; Spec=ECMA-262 s19.2.1.1 PerformEval, JSD-0083 D16 D44; IP=Low; Security=Medium; Resources=0; Fingerprint=82941F
// Hairs-Human:        PENDING
public sealed class InheritedContext
{
    private readonly IReadOnlyList<string>? visiblePrivateNames;

    /// <summary>
    /// The context of eval code whose caller is outside every function: a script's or a module's top level, and an
    /// indirect eval's. No private-name set, so that check stays the engine's.
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
    /// undeclared private name to the engine.
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
}
