// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   6
// Annotated:        6/6
// Exempt:           6
// Human-reviewed:   0/6
// IP risk:          Low
// Security risk:    High
// Criteria:         1/1
// Resource impact:  2/10 max
// Unverified:       6
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
    /// The library cannot judge the source yet. Before the parser exists (stage 4c), it is the answer for every source the
    /// tokenizer (stage 4b) does not refuse: one it reads to its end, and one where it stops at a token whose lexical goal
    /// only the parser can name. No diagnostic; a caller reads it as neither an acceptance nor a refusal. Stage 4c withdraws it.
    /// <i>(Revised 2026-11-01, at stage SJL-13.2b, when the tokenizer began to refuse a source; the summary began "The library
    /// cannot parse yet: stage SJL-13.2a's answer for every source, before the tokenizer (stage 4b) and the parser (stage 4c)
    /// exist.")</i>
    /// </summary>
    NotImplemented = 2,
}

/// <summary>
/// What a parse answers (Broiler.VM's JSD-0080, "Output"; JSD-0083, X3 and decision D11): its outcome and its diagnostics.
/// The tree and the token stream join it at stage 4c, whose parser names each token's lexical goal, and the side tables at
/// stage 4d. <i>(Revised 2026-11-01, at stage SJL-13.2b, whose tokenizer is internal to the library and reads a text whole only under the
/// parser's goals; it read "The tree, the token stream and the side tables join it at the stages that build them (4c, 4b
/// and 4d).")</i>
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

    /// <summary>The answer of a parse that refuses the source with one diagnostic, under the first error (decision D11).</summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0083 X3 D11 a refusal is data; IP=Low; Security=Medium; Resources=1; Fingerprint=B76368
    // Broiler-Human:        PENDING
    internal static ParseResult Refused(SyntaxDiagnostic diagnostic) => new(ParseOutcome.Refused, Array.AsReadOnly(new[] { diagnostic }));
}

/// <summary>
/// The library's parse entry (Broiler.VM's JSD-0083, decision D6: the parser in <c>Broiler.ECMAScript.Syntax</c>): one
/// request in, one result out, deterministic and with no state of its own (the contract's properties; rules SL2 and SL9).
/// </summary>
/// <remarks>
/// <para>
/// <b>At stage SJL-13.2b it answers <see cref="ParseOutcome.Refused"/> for a source the lexical grammar refuses, and
/// <see cref="ParseOutcome.NotImplemented"/> for every other</b>: the tree and the parser are stage 4c's, so the parse
/// tokenizes the text as far as the lexical grammar decides it without the parser and refuses only what no reading of the
/// text can tokenize, with the tokenizer's diagnostic (<c>LexicalScan</c>). It charges the request's meter for the whole
/// text before it reads the first code unit (<see cref="ParseRequest.Meter"/>), and reads the Unicode provider; it asks the
/// regular-expression provider nothing, the literal's validity being stage 4d's. <i>(Revised 2026-11-01, at stage
/// SJL-13.2b, when the tokenizer was added; it read "At stage SJL-13.2a it answers
/// <see cref="ParseOutcome.NotImplemented"/> for every source: the tokenizer is stage 4b's and the tree and the parser stage
/// 4c's. It reads no provider and charges no meter, because it reads none of the source; from stage 4b it charges the
/// request's meter before it reads the first code unit (<see cref="ParseRequest.Meter"/>).")</i>
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
    /// <returns>
    /// The result: at this stage, <see cref="ParseOutcome.Refused"/> with the tokenizer's one diagnostic for a source the
    /// lexical grammar refuses, and <see cref="ParseOutcome.NotImplemented"/> with none for every other.
    /// <i>(Revised 2026-11-01, at stage SJL-13.2b; it read "The result: at this stage,
    /// <see cref="ParseOutcome.NotImplemented"/> with no diagnostic, for every request.")</i>
    /// </returns>
    /// <exception cref="ArgumentNullException">The request is null.</exception>
    /// <exception cref="ArgumentException">The request's Unicode provider is not of the library's Unicode version or lacks a table the lexical classes read.</exception>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s12 lexical grammar, JSD-0083 SJL-13.2b parse entry D32; IP=Low; Security=High; Resources=2; Fingerprint=8A4BA8
    // Broiler-Falsified-If: a request is answered Accepted before stage 4c gives the library a parser, a source the specification accepts is answered Refused, or the meter is charged after a code unit of the source is read or for other than the text's length
    // Broiler-Human:        PENDING
    public static ParseResult Parse(ParseRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var length = request.Source.Text.Length;
        if (request.Meter is not null && length > 0)
            request.Meter((ulong)length);

        return LexicalScan.Run(request);
    }
}
