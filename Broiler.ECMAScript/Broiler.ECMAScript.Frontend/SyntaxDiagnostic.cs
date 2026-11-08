// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// HAIRS Code Assurance
// --------------------
// Relevant units:   7
// Annotated:        7/7
// Exempt:           13
// Human-reviewed:   0/7
// IP risk:          Low
// Security risk:    Low
// Criteria:         0/0
// Resource impact:  1/10 max
// Unverified:       7
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Generic;

namespace Broiler.ECMAScript.Syntax;

/// <summary>
/// The stage of the front end that reports a diagnostic (decision SJL-D-11; Broiler.VM's JSD-0080, "Diagnostics"): the
/// tokenizer, the parser, or the static semantics of the syntax half.
/// </summary>
// Hairs-AI:           Origin=AI; Spec=SJL-D-11 diagnostics phase; IP=Low; Security=Low; Resources=0; Fingerprint=68B0E2
// Hairs-Human:        PENDING
public enum DiagnosticPhase
{
    /// <summary>The lexical grammar (ECMA-262 clause 12): a code unit no token admits, an unterminated literal, a malformed escape.</summary>
    Tokenizer = 0,

    /// <summary>The syntactic grammar (ECMA-262 clauses 13 to 16), with its cover grammars' refinements and the request's limits.</summary>
    Parser = 1,

    /// <summary>The early errors of the syntax half's declarations walk (Broiler.VM's JSD-0083, decision D10).</summary>
    Semantics = 2,
}

/// <summary>
/// Where in the source a node or a diagnostic is (Broiler.VM's JSD-0083, decision D15): its offset and length, and the line
/// and column it starts at.
/// </summary>
/// <remarks>
/// Offsets are 0-based and lengths in UTF-16 code units; lines and columns are 1-based, columns in UTF-16 code units; every
/// line terminator of ECMA-262 clause 12.3 starts a line, and CR LF is one. Each includes the source's base offset, line
/// and column (<see cref="SourceText"/>).
/// </remarks>
// Hairs-AI:           Origin=AI; Spec=JSD-0083 D15 positions; IP=Low; Security=Low; Resources=0; Fingerprint=8F2BC2
// Hairs-Human:        PENDING
public readonly record struct SourceSpan
{
    /// <summary>Creates a span.</summary>
    /// <param name="offset">The 0-based offset of its first code unit.</param>
    /// <param name="length">Its length in code units; zero or more.</param>
    /// <param name="line">The 1-based line it starts on.</param>
    /// <param name="column">The 1-based column it starts at.</param>
    /// <exception cref="ArgumentOutOfRangeException">The offset or the length is negative, or the line or the column is below one.</exception>
    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D15 positions; IP=Low; Security=Low; Resources=1; Fingerprint=FAC039
    // Hairs-Human:        PENDING
    public SourceSpan(int offset, int length, int line, int column)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        ArgumentOutOfRangeException.ThrowIfLessThan(line, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(column, 1);
        Offset = offset;
        Length = length;
        Line = line;
        Column = column;
    }

    /// <summary>The 0-based offset of the span's first code unit.</summary>
    public int Offset { get; }

    /// <summary>The span's length, in UTF-16 code units.</summary>
    public int Length { get; }

    /// <summary>The 1-based line the span starts on.</summary>
    public int Line { get; }

    /// <summary>The 1-based column, in UTF-16 code units, the span starts at.</summary>
    public int Column { get; }
}

/// <summary>
/// One refusal (decision SJL-D-11; Broiler.VM's JSD-0083, decisions D11 and D15): a shared code of the early-error
/// catalogue, a message identifier with its arguments, a phase, a span and the source's name.
/// </summary>
/// <remarks>
/// <para>
/// <b>The code and the identifier are the contract; no text is.</b> Each engine renders its own message from the identifier
/// and its arguments (Broiler.JS its V8-style texts, Broiler.VM the texts it renders today for a code whose meaning stays)
/// and maps the code onto its own registry (Broiler.VM's <c>frontend-mapping.tsv</c>). The identifiers and their arguments
/// are defined at stage 4d, from the catalogue's section 6 (<c>Broiler.ECMAScript/docs/frontend/early-errors.md</c>).
/// </para>
/// <para>
/// <b>The span starts where the refusal is anchored</b> (decision D15): for each refusal the library takes from Broiler.VM's
/// wide front end, where that front end anchors it at the base.
/// </para>
/// </remarks>
// Hairs-AI:           Origin=AI; Spec=SJL-D-11, JSD-0083 D11 D15 diagnostics; IP=Low; Security=Low; Resources=0; Fingerprint=A1F926
// Hairs-Human:        PENDING
public sealed class SyntaxDiagnostic
{
    /// <summary>Creates a diagnostic.</summary>
    /// <param name="code">The catalogue's shared code: <c>JSE</c> and four decimal digits.</param>
    /// <param name="messageId">The message identifier: two to four segments joined by <c>.</c>, each a lower-case ASCII letter, then letters and digits, hyphens joining them (the catalogue's section 6).</param>
    /// <param name="arguments">The identifier's arguments, in order; copied.</param>
    /// <param name="phase">The stage that reports it.</param>
    /// <param name="span">Where it is anchored.</param>
    /// <param name="sourceName">The source's name (<see cref="SourceText.Name"/>).</param>
    /// <exception cref="ArgumentNullException">An argument that is a reference is null, or one of the arguments is.</exception>
    /// <exception cref="ArgumentException">The code or the message identifier is not of its form.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The phase is not a <see cref="DiagnosticPhase"/>.</exception>
    // Hairs-AI:           Origin=AI; Spec=SJL-D-11, catalogue shared codes and message identifiers; IP=Low; Security=Low; Resources=1; Fingerprint=D1807F
    // Hairs-Human:        PENDING
    public SyntaxDiagnostic(string code, string messageId, IReadOnlyList<string> arguments, DiagnosticPhase phase, SourceSpan span, string sourceName)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(messageId);
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(sourceName);
        if (!IsSharedCode(code))
            throw new ArgumentException("A shared code is JSE and four decimal digits.", nameof(code));
        if (!IsMessageId(messageId))
            throw new ArgumentException("A message identifier is two to four lower-case ASCII segments joined by dots (the catalogue's section 6).", nameof(messageId));
        if (phase is not (DiagnosticPhase.Tokenizer or DiagnosticPhase.Parser or DiagnosticPhase.Semantics))
            throw new ArgumentOutOfRangeException(nameof(phase), phase, "Not a diagnostic phase.");

        var copy = new string[arguments.Count];
        for (var i = 0; i < copy.Length; i++)
            copy[i] = arguments[i] ?? throw new ArgumentNullException(nameof(arguments), "A diagnostic argument is null.");

        Code = code;
        MessageId = messageId;
        Arguments = Array.AsReadOnly(copy);
        Phase = phase;
        Span = span;
        SourceName = sourceName;
    }

    /// <summary>The catalogue's shared code, <c>JSE</c> and four digits, grouped by clause (catalogue decision D30).</summary>
    public string Code { get; }

    /// <summary>The message identifier, dotted, such as <c>scope.lexical.duplicate</c> (the catalogue's section 6).</summary>
    public string MessageId { get; }

    /// <summary>The identifier's arguments, in order.</summary>
    public IReadOnlyList<string> Arguments { get; }

    /// <summary>The stage that reports it.</summary>
    public DiagnosticPhase Phase { get; }

    /// <summary>Where it is anchored.</summary>
    public SourceSpan Span { get; }

    /// <summary>The name of the source it is in.</summary>
    public string SourceName { get; }

    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes, early-errors.md section 5; IP=Low; Security=Low; Resources=1; Fingerprint=3C5B5C
    // Hairs-Human:        PENDING
    private static bool IsSharedCode(string code)
    {
        if (code.Length != 7 || !code.StartsWith("JSE", StringComparison.Ordinal))
            return false;
        for (var i = 3; i < 7; i++)
        {
            if (code[i] is < '0' or > '9')
                return false;
        }

        return true;
    }

    // Hairs-AI:           Origin=AI; Spec=catalogue message identifiers, early-errors.md section 6; IP=Low; Security=Low; Resources=1; Fingerprint=A508F8
    // Hairs-Human:        PENDING
    private static bool IsMessageId(string messageId)
    {
        // Two to four segments joined by '.', each [a-z][a-z0-9]*(-[a-z0-9]+)*.
        var segments = 1;
        var atSegmentStart = true;
        var afterHyphen = false;
        foreach (var c in messageId)
        {
            if (atSegmentStart)
            {
                if (c is < 'a' or > 'z')
                    return false;
                atSegmentStart = false;
            }
            else if (c == '.')
            {
                if (afterHyphen)
                    return false;
                segments++;
                atSegmentStart = true;
            }
            else if (c == '-')
            {
                if (afterHyphen)
                    return false;
                afterHyphen = true;
            }
            else if (c is (>= 'a' and <= 'z') or (>= '0' and <= '9'))
            {
                afterHyphen = false;
            }
            else
            {
                return false;
            }
        }

        return !atSegmentStart && !afterHyphen && segments is >= 2 and <= 4;
    }
}
