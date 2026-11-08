// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// HAIRS Code Assurance
// --------------------
// Relevant units:   2
// Annotated:        2/2
// Exempt:           9
// Human-reviewed:   0/2
// IP risk:          Low
// Security risk:    Low
// Criteria:         0/0
// Resource impact:  0/10 max
// Unverified:       2
//
// GENERATED - DO NOT EDIT MANUALLY

using System;

namespace Broiler.ECMAScript.Syntax;

/// <summary>
/// The proposals beyond ECMA-262 ES2026 a request admits (Broiler.VM's JSD-0083, decision D17): one flag per form, each
/// its own catalogue row and its own switch of the stage table (<c>proposal:</c> and the form). Off by default.
/// </summary>
/// <remarks>
/// <para>
/// <b>An engine admits what its own manifest admits.</b> Broiler.VM admits none (a decorator is refused where the parse
/// reaches it); Broiler.JS admits what it parses today, <see cref="Decorators"/>, <see cref="AutoAccessors"/> and the two
/// call forms, not the two declaration forms (the correspondence's rows R09 and P06b; configuration J of the home's rule).
/// </para>
/// <para>The values are bits, stable once published, and a request refuses one this type does not define.</para>
/// </remarks>
// Hairs-AI:           Origin=AI; Spec=JSD-0083 D17 proposal flags; IP=Low; Security=Low; Resources=0; Fingerprint=E2F461
// Hairs-Human:        PENDING
[Flags]
public enum ProposalFlags
{
    /// <summary>No proposal: the pinned edition's grammar alone.</summary>
    None = 0,

    /// <summary>Decorators (catalogue row EP0009, switch <c>proposal:decorators</c>; the TC39 decorators proposal, test262 feature <c>decorators</c>).</summary>
    Decorators = 1,

    /// <summary>Auto-accessors, the class element <c>accessor</c> (catalogue row EP0010, switch <c>proposal:decorators-accessor</c>).</summary>
    AutoAccessors = 2,

    /// <summary>The source-phase import declaration <c>import source x from</c> (catalogue row EP0011, switch <c>proposal:import-source-declaration</c>).</summary>
    ImportSourceDeclarations = 4,

    /// <summary>The source-phase dynamic import <c>import.source(…)</c> (catalogue row EP0012, switch <c>proposal:import-source-call</c>).</summary>
    ImportSourceCalls = 8,

    /// <summary>The deferred namespace import <c>import defer * as ns from</c> (catalogue row EP0013, switch <c>proposal:import-defer-declaration</c>).</summary>
    ImportDeferDeclarations = 16,

    /// <summary>The deferred dynamic import <c>import.defer(…)</c> (catalogue row EP0014, switch <c>proposal:import-defer-call</c>).</summary>
    ImportDeferCalls = 32,
}

/// <summary>
/// The lexer extensions a request admits (Broiler.VM's JSD-0080, the request's "Lexer extensions"; JSD-0083, decision
/// D17): the only way a non-standard literal suffix reaches the library. Off by default.
/// </summary>
/// <remarks>
/// The library tokenizes what an extension admits and gives it no meaning: decision SJL-D-17 keeps the meaning in the
/// engine that asks for it. The values are bits, and a request refuses one this type does not define.
/// </remarks>
// Hairs-AI:           Origin=AI; Spec=JSD-0083 D17 lexer extension, SJL-D-17; IP=Low; Security=Low; Resources=0; Fingerprint=5F9094
// Hairs-Human:        PENDING
[Flags]
public enum LexerExtensions
{
    /// <summary>None: a numeric literal is ECMA-262 clause 12.9.3's alone. Broiler.VM passes none (<c>1m</c> stays its refusal).</summary>
    None = 0,

    /// <summary>
    /// The <c>m</c> suffix directly after a decimal literal, which Broiler.JS reads as its <c>Decimal</c> (SJL-D-17; the
    /// correspondence's rows T18 and R10).
    /// </summary>
    DecimalSuffix = 1,
}
