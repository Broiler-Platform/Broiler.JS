// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   5
// Annotated:        5/5
// Exempt:           6
// Human-reviewed:   0/5
// IP risk:          Low
// Security risk:    High
// Criteria:         1/1
// Resource impact:  1/10 max
// Unverified:       5
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Generic;

namespace Broiler.ECMAScript.Syntax;

/// <summary>How a parse ended (Broiler.VM's JSD-0083, X3: "a refusal is data").</summary>
/// <remarks>The numbers are stable; a later stage that withdraws <see cref="NotImplemented"/> reuses no number.</remarks>
// Broiler-AI:           Origin=AI; Spec=JSD-0083 X3 a refusal is data; IP=Low; Security=Low; Resources=0; Fingerprint=177077
// Broiler-Human:        PENDING
public enum ParseOutcome
{
    /// <summary>The source parses under the request: no diagnostic.</summary>
    Accepted = 0,

    /// <summary>The source does not: the result's diagnostics say why, in the library's order (decision D11).</summary>
    Refused = 1,

    /// <summary>
    /// The library cannot parse yet: stage SJL-13.2a's answer for every source, before the tokenizer (stage 4b) and the
    /// parser (stage 4c) exist. No diagnostic; a caller reads it as neither an acceptance nor a refusal. Stage 4c withdraws it.
    /// </summary>
    NotImplemented = 2,
}

/// <summary>
/// What a parse answers (Broiler.VM's JSD-0080, "Output"; JSD-0083, X3 and decision D11): its outcome and its diagnostics.
/// The tree, the token stream and the side tables join it at the stages that build them (4c, 4b and 4d).
/// </summary>
// Broiler-AI:           Origin=AI; Spec=JSD-0080 output, JSD-0083 X3 D11; IP=Low; Security=Low; Resources=0; Fingerprint=54BFD3
// Broiler-Human:        PENDING
public sealed class ParseResult
{
    // Broiler-AI:           Origin=AI; Spec=JSD-0080 output; IP=Low; Security=Low; Resources=1; Fingerprint=F97E3D
    // Broiler-Human:        PENDING
    private ParseResult(ParseOutcome outcome, IReadOnlyList<SyntaxDiagnostic> diagnostics)
    {
        Outcome = outcome;
        Diagnostics = diagnostics;
    }

    /// <summary>How the parse ended.</summary>
    public ParseOutcome Outcome { get; }

    /// <summary>
    /// The refusals, in the library's order (decision D11): empty for an accepted source and for
    /// <see cref="ParseOutcome.NotImplemented"/>; under <see cref="ErrorReporting.FirstError"/>, one for a refused source.
    /// </summary>
    public IReadOnlyList<SyntaxDiagnostic> Diagnostics { get; }

    /// <summary>The answer of a parse the library cannot make yet.</summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0083 SJL-13.2a skeleton; IP=Low; Security=Low; Resources=1; Fingerprint=63CB80
    // Broiler-Human:        PENDING
    internal static ParseResult NotImplemented() => new(ParseOutcome.NotImplemented, Array.Empty<SyntaxDiagnostic>());
}

/// <summary>
/// The library's parse entry (Broiler.VM's JSD-0083, decision D6: the parser in <c>Broiler.ECMAScript.Syntax</c>): one
/// request in, one result out, deterministic and with no state of its own (the contract's properties; rules SL2 and SL9).
/// </summary>
/// <remarks>
/// <para>
/// <b>At stage SJL-13.2a it answers <see cref="ParseOutcome.NotImplemented"/> for every source</b>: the tokenizer is stage
/// 4b's and the tree and the parser stage 4c's. It reads no provider and charges no meter, because it reads none of the
/// source; from stage 4b it charges the request's meter before it reads the first code unit
/// (<see cref="ParseRequest.Meter"/>).
/// </para>
/// <para>
/// <b>Safe to call concurrently</b> (convention 10): a parse shares nothing with another but its request's providers,
/// which the request's caller makes safe.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s16.1 ParseScript s16.2 ParseModule, JSD-0083 D6; IP=Low; Security=Medium; Resources=1; Fingerprint=85477C
// Broiler-Human:        PENDING
public static class SyntaxParser
{
    /// <summary>
    /// Parses a request's source under its goal (ECMA-262 clauses 16.1 ParseScript, 16.2 ParseModule, 19.2.1.1 PerformEval
    /// step 11 and 20.2.1.1.1 CreateDynamicFunction steps 18 to 20, as the goal says).
    /// </summary>
    /// <param name="request">The request.</param>
    /// <returns>The result: at this stage, <see cref="ParseOutcome.NotImplemented"/> with no diagnostic, for every request.</returns>
    /// <exception cref="ArgumentNullException">The request is null.</exception>
    // Broiler-AI:           Origin=AI; Spec=JSD-0083 SJL-13.2a skeleton parse entry; IP=Low; Security=High; Resources=1; Fingerprint=A3B55F
    // Broiler-Falsified-If: a request is answered Accepted or Refused before stage 4c gives the library a parser, or a meter is charged or a provider read before stage 4b gives it a tokenizer
    // Broiler-Human:        PENDING
    public static ParseResult Parse(ParseRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return ParseResult.NotImplemented();
    }
}
