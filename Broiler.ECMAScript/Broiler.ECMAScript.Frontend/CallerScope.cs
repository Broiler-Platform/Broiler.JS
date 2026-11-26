// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// HAIRS Code Assurance
// --------------------
// Relevant units:   3
// Annotated:        3/3
// Exempt:           1
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
/// One declarative Environment Record of a direct <c>eval</c>'s caller, as <see cref="InheritedContext.CallerScopes"/>
/// lists them (milestone SJL-15.2, stage 5c; the session's decision D-js-caller-scopes; ECMA-262 clause 19.2.1.1 PerformEval
/// steps 16.a and 16.b, and clause 19.2.1.3 EvalDeclarationInstantiation steps 3.d and 13.b.ii.4): every name the record
/// binds, as its HasBinding sees it, and whether it is the Environment Record for a <c>Catch</c> clause.
/// <i>(Added 2026-11-26, at SJL-15.2 stage 5c.)</i>
/// </summary>
/// <remarks>
/// <para>
/// <b>Every name the record binds</b>, not only the names it declares lexically: a block's, a <c>switch</c>'s CaseBlock's
/// or a <c>for</c> head's lexical declarations, its block functions and class declarations among them; a class's inner
/// name; a <c>catch</c> clause's parameter names, of either form; a function's top-level lexical declarations, the
/// LexicalEnvironment over its VariableEnvironment (FunctionDeclarationInstantiation step 32.b); and for a call in a sloppy
/// function's parameters, the parameters and the function's <c>arguments</c> binding (steps 20 and 22.d.i). A sloppy
/// function's own <c>var</c>s and top-level functions are in its VariableEnvironment, which no entry describes.
/// </para>
/// <para>
/// <b><see cref="IsCatch"/></b> marks "the Environment Record for a |Catch| clause" (EvalDeclarationInstantiation steps
/// 3.d.i.2.a.i.1 and 13.b.ii.4.a.i.1.a), whatever the form of the clause's parameter: Annex B.3.4 changes both steps with no
/// condition on that form.
/// </para>
/// <para>
/// <b>Init-only, and it copies <see cref="Names"/></b>, so that the caller's list can change afterwards without changing the
/// entry, and the semantic model can share one instance across the calls whose chains hold its record: immutable, and safe
/// to read concurrently (convention 10). Duplicates within one entry are kept as given.
/// </para>
/// </remarks>
// Hairs-AI:           Origin=AI; Spec=ECMA-262 s19.2.1.1 PerformEval steps 16.a 16.b, s19.2.1.3 EvalDeclarationInstantiation steps 3.d 13.b.ii.4, Annex B.3.4; IP=Low; Security=Medium; Resources=1; Fingerprint=F07894
// Hairs-Human:        PENDING
public sealed class CallerScope
{
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s9.1.1.1.1 HasBinding; IP=Low; Security=Low; Resources=0; Fingerprint=80AF8C
    // Hairs-Human:        PENDING
    private readonly IReadOnlyList<string> names = Array.Empty<string>();

    /// <summary>
    /// Every name the record binds, in the order the caller gives them; empty by default. Each name is its StringValue, the
    /// form a <c>var</c> of eval code names it by.
    /// </summary>
    /// <exception cref="ArgumentNullException">The list is null: an empty list states a record with no binding, which a null list would hide.</exception>
    /// <exception cref="ArgumentException">A name is null or empty.</exception>
    // Hairs-AI:           Origin=AI; Spec=ECMA-262 s9.1.1.1.1 HasBinding, s19.2.1.3 EvalDeclarationInstantiation step 3.d.i.2.a; IP=Low; Security=Medium; Resources=1; Fingerprint=B41024
    // Hairs-Human:        PENDING
    public IReadOnlyList<string> Names
    {
        get => names;
        init
        {
            ArgumentNullException.ThrowIfNull(value, nameof(Names));
            var copy = new string[value.Count];
            for (var i = 0; i < copy.Length; i++)
            {
                var name = value[i];
                if (string.IsNullOrEmpty(name))
                    throw new ArgumentException("A caller's record binds names of at least one code unit.", nameof(Names));
                copy[i] = name;
            }

            names = Array.AsReadOnly(copy);
        }
    }

    /// <summary>
    /// Whether the record is the Environment Record for a <c>Catch</c> clause (EvalDeclarationInstantiation steps
    /// 3.d.i.2.a.i.1 and 13.b.ii.4.a.i.1.a), of a lone BindingIdentifier or of a pattern alike; false by default.
    /// </summary>
    public bool IsCatch { get; init; }
}
