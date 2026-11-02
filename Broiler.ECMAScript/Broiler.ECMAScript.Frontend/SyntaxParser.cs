// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   6
// Annotated:        6/6
// Exempt:           9
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
// Broiler-AI:           Origin=AI; Spec=JSD-0083 SJL-13.2c parse entry, D11; IP=Low; Security=Medium; Resources=1; Fingerprint=177077
// Broiler-Human:        PENDING
public enum ParseOutcome
{
    /// <summary>
    /// The source parses under the request: the result carries its tree, the declarations walk's side tables and no
    /// diagnostic. The grammar admits it, no static-semantic early error of a row the stage table assigns <c>syntax</c>
    /// refuses it, and the request's regular-expression provider admits each of its literals. <i>(Revised 2026-11-02, at
    /// stage SJL-13.2d, when the declarations walk was added; it read "The source parses under the request: the result
    /// carries its tree and no diagnostic. From stage 4c the grammar alone decides it; the static-semantic early errors
    /// join at stage 4d, so a source only such an error refuses is accepted until then.")</i> <i>(Revised 2026-11-01, at
    /// stage SJL-13.2c, when the parser was added; it read "The source parses under the request: no diagnostic.")</i>
    /// </summary>
    Accepted = 0,

    /// <summary>The source does not: the result's diagnostics say why, in the library's order (decision D11).</summary>
    Refused = 1,

    /// <summary>
    /// The library cannot judge the source. No parse answers it from stage 4c, whose parser judges every source; the member
    /// stays so that no number is reused. <i>(Revised 2026-11-01, at stage SJL-13.2c, when the parser was added; it read "The
    /// library cannot judge the source yet. Before the parser exists (stage 4c), it is the answer for every source the
    /// tokenizer (stage 4b) does not refuse: one it reads to its end, and one where it stops at a token whose lexical goal
    /// only the parser can name. No diagnostic; a caller reads it as neither an acceptance nor a refusal. Stage 4c withdraws
    /// it.")</i>
    /// </summary>
    NotImplemented = 2,
}

/// <summary>
/// What a parse answers (Broiler.VM's JSD-0080, "Output"; JSD-0083, X3 and decision D11): its outcome, its tree, the
/// declarations walk's side tables and its diagnostics. <i>(Revised 2026-11-02, at stage SJL-13.2d, when the side tables
/// joined it; it read "its outcome, its tree and its diagnostics. The side tables join it at stage 4d.")</i>
/// <i>(Revised 2026-11-01, at stage SJL-13.2c, when the tree joined it; it read "its outcome and its diagnostics. The tree
/// and the token stream join it at stage 4c, whose parser names each token's lexical goal, and the side tables at stage
/// 4d.")</i>
/// </summary>
/// <remarks>
/// The token stream the parse read is kept beside the tree inside the library and is not public at stage 4c or 4d: which of its
/// raw facts the result publishes, for Broiler.VM's tokenizers-agreement row and Broiler.JS's bridged token chain
/// (decisions D21 and D44), is fixed when a consumer first reads it.
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=JSD-0083 SJL-13.2c parse entry, D11; IP=Low; Security=Medium; Resources=1; Fingerprint=54BFD3
// Broiler-Human:        PENDING
public sealed class ParseResult
{
    private ParseResult(ParseOutcome outcome, ProgramNode? tree, IReadOnlyList<Token> tokens, IReadOnlyList<SyntaxDiagnostic> diagnostics, Semantics.DeclarationTables? declarations)
    {
        Outcome = outcome;
        Tree = tree;
        Tokens = tokens;
        Diagnostics = diagnostics;
        Declarations = declarations;
    }

    /// <summary>How the parse ended.</summary>
    public ParseOutcome Outcome { get; }

    /// <summary>
    /// The tree of an accepted source (JSD-0080's output table: immutable nodes with full spans and raw facts, no
    /// desugaring); null for a refused source.
    /// </summary>
    public ProgramNode? Tree { get; }

    /// <summary>The refusals, in the library's order (decision D11): empty for an accepted source; for a refused source, one under <see cref="ErrorReporting.FirstError"/>, and up to <see cref="ParseLimits.DiagnosticCap"/> under <see cref="ErrorReporting.AllErrors"/>.</summary>
    public IReadOnlyList<SyntaxDiagnostic> Diagnostics { get; }

    /// <summary>The tokens the parse consumed, in source order, each read under the goal the parser named; empty for a refused source.</summary>
    internal IReadOnlyList<Token> Tokens { get; }

    /// <summary>
    /// The declarations walk's side tables (stage SJL-13.2d; Broiler.VM's JSD-0083, decision D10), ordered by source position:
    /// every scope's kind and declarations, and the program's and every function's strictness and parameter names; null for a
    /// refused source.
    /// </summary>
    public Semantics.DeclarationTables? Declarations { get; }

    /// <summary>The answer of a parse that accepts the source.</summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0083 SJL-13.2c parse entry, D11; IP=Low; Security=Medium; Resources=1; Fingerprint=F137FE
    // Broiler-Human:        PENDING
    internal static ParseResult Accepted(ProgramNode tree, List<Token> tokens, Semantics.DeclarationTables declarations) =>
        new(ParseOutcome.Accepted, tree, tokens.AsReadOnly(), Array.Empty<SyntaxDiagnostic>(), declarations);

    /// <summary>The answer of a parse that refuses the source, with the diagnostics the request asks for, in the library's order (decision D11).</summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0083 SJL-13.2d parse entry, D11; IP=Low; Security=Medium; Resources=1; Fingerprint=398E8C
    // Broiler-Human:        PENDING
    internal static ParseResult Refused(List<SyntaxDiagnostic> diagnostics) =>
        new(ParseOutcome.Refused, null, Array.Empty<Token>(), diagnostics.AsReadOnly(), null);
}

/// <summary>
/// The library's parse entry (Broiler.VM's JSD-0083, decision D6: the parser in <c>Broiler.ECMAScript.Syntax</c>): one
/// request in, one result out, deterministic and with no state of its own (the contract's properties; rules SL2 and SL9).
/// </summary>
/// <remarks>
/// <para>
/// <b>From stage SJL-13.2d it parses the syntactic grammar</b> of the request's goal (clauses 13 to 16), then walks the
/// tree it admitted for the static-semantic early errors of every row the stage table assigns <c>syntax</c>, asking the
/// request's regular-expression provider about each literal (decisions D7 and D10). It answers
/// <see cref="ParseOutcome.Accepted"/> with the tree and the side tables, or <see cref="ParseOutcome.Refused"/> with its
/// refusals in the library's order (decision D11), each with its catalogue row's code: a lexical refusal with the
/// tokenizer's diagnostic, a grammar refusal, one of the contract's two limits, an early error, or the provider's verdict.
/// The result carries one diagnostic under <see cref="ErrorReporting.FirstError"/>, and up to
/// <see cref="ParseLimits.DiagnosticCap"/> under <see cref="ErrorReporting.AllErrors"/>. It charges the request's meter for
/// the whole text before it reads the first code unit (<see cref="ParseRequest.Meter"/>), and reads the Unicode provider.
/// <i>(Revised 2026-11-02, at stage SJL-13.2d, when the declarations walk was added; it read "At stage SJL-13.2c it parses
/// the syntactic grammar of the request's goal (clauses 13 to 16) and answers Accepted with the tree, or Refused with the
/// first refusal: a lexical refusal with the tokenizer's diagnostic, a grammar refusal with a provisional code stage 4d
/// replaces by the catalogue's, or one of the contract's two limits. The static-semantic early errors and the
/// regular-expression provider's answer are stage 4d's, so a source only they refuse is accepted here. It charges the
/// request's meter for the whole text before it reads the first code unit, and reads the Unicode provider; it asks the
/// regular-expression provider nothing.")</i> <i>(Revised 2026-11-01, at stage SJL-13.2c, when the parser was added; it
/// read "At stage SJL-13.2b it answers <see cref="ParseOutcome.Refused"/> for a source the lexical grammar refuses, and
/// <see cref="ParseOutcome.NotImplemented"/> for every other: the tree and the parser are stage 4c's, so the parse tokenizes
/// the text as far as the lexical grammar decides it without the parser and refuses only what no reading of the text can
/// tokenize, with the tokenizer's diagnostic (<c>LexicalScan</c>). It charges the request's meter for the whole text
/// before it reads the first code unit (<see cref="ParseRequest.Meter"/>), and reads the Unicode provider; it asks the
/// regular-expression provider nothing, the literal's validity being stage 4d's.")</i>
/// </para>
/// <para>
/// <b>Its recursion is bounded by the request's nesting depth</b>, which is checked before each recursive production
/// (decision D14). The stack a parse at the library's supported maximum nesting requires is stage 4e's to derive and
/// state; until then a caller that admits deep input parses on a thread with a large stack, as Broiler.VM's
/// <c>CompilationStack</c> does.
/// </para>
/// <para>
/// <b>Safe to call concurrently</b> (convention 10): a parse shares nothing with another but its request's providers,
/// which the request's caller makes safe.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=JSD-0083 SJL-13.2c parse entry, D11; IP=Low; Security=Medium; Resources=1; Fingerprint=85477C
// Broiler-Human:        PENDING
public static class SyntaxParser
{
    /// <summary>
    /// Parses a request's source under its goal (ECMA-262 clauses 16.1 ParseScript, 16.2 ParseModule, 19.2.1.1 PerformEval
    /// step 11 and 20.2.1.1.1 CreateDynamicFunction steps 18 to 20, as the goal says).
    /// </summary>
    /// <param name="request">The request.</param>
    /// <returns>
    /// The result: <see cref="ParseOutcome.Accepted"/> with the tree and the side tables where the grammar admits the source
    /// and no early error or provider's verdict refuses it, and <see cref="ParseOutcome.Refused"/> otherwise, with the first
    /// refusal under <see cref="ErrorReporting.FirstError"/> and every one up to the request's cap under
    /// <see cref="ErrorReporting.AllErrors"/>. <i>(Revised 2026-11-02, at stage SJL-13.2d; it read "The result: Accepted
    /// with the tree where the grammar admits the source, and Refused with the first refusal where it does not.")</i>
    /// <i>(Revised 2026-11-01, at stage
    /// SJL-13.2c; it read "The result: at this stage, <see cref="ParseOutcome.Refused"/> with the tokenizer's one diagnostic
    /// for a source the lexical grammar refuses, and <see cref="ParseOutcome.NotImplemented"/> with none for every
    /// other.")</i>
    /// </returns>
    /// <exception cref="ArgumentNullException">The request is null.</exception>
    /// <exception cref="ArgumentException">The request's Unicode provider is not of the library's Unicode version or lacks a table the lexical classes read.</exception>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s16.1 s16.2 s19.2.1.1 s20.2.1.1.1 goals, JSD-0083 SJL-13.2c parse entry D32; IP=Low; Security=High; Resources=2; Fingerprint=6F6645
    // Broiler-Falsified-If: a source the grammar or a syntax row's early error refuses is answered Accepted, a source neither refuses is answered Refused, or the meter is charged after a code unit of the source is read or for other than the text's length
    // Broiler-Human:        PENDING
    public static ParseResult Parse(ParseRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var length = request.Source.Text.Length;
        if (request.Meter is not null && length > 0)
            request.Meter((ulong)length);

        return Parser.Run(request);
    }
}
