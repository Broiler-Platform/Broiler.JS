// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   7
// Annotated:        7/7
// Exempt:           10
// Human-reviewed:   0/7
// IP risk:          Low
// Security risk:    Medium
// Criteria:         0/0
// Resource impact:  1/10 max
// Unverified:       7
//
// GENERATED - DO NOT EDIT MANUALLY

using System;

namespace Broiler.ECMAScript.Syntax;

/// <summary>
/// The request's regular-expression provider (Broiler.VM's JSD-0083, decision D7): the validity of a regular-expression
/// literal, which ECMA-262 clause 13.2.7.1 makes an early error (catalogue row EE0019, IsValidRegularExpressionLiteral of
/// clause 13.2.7.2), asked of a validator each engine supplies.
/// </summary>
/// <remarks>
/// <para>
/// <b>An interface, with no package edge</b> (decision D7): the library references no regular-expression package.
/// Broiler.VM wraps the validator its binder's <c>CheckRegExp</c> calls today, or Broiler.Regex's validate entry once
/// SJL-11.4 has landed; Broiler.JS supplies its own at SJL-14. The shape is the validate entry's, which a wrapper calls
/// without translation: the literal's pattern and flags as written, and an answer of a status with a code, a message
/// identifier, its argument and a position, in the pattern or in the flags (<see cref="RegularExpressionVerdict"/>). The
/// engine binds what else its validator takes: its Unicode tables and its own nesting limit.
/// </para>
/// <para>
/// <b>The library reports a refusal at the literal's span</b> (decision D7), with the verdict's message identifier and
/// argument; the verdict's position and code are the validator's, kept for the engine.
/// </para>
/// <para>
/// <b>The library calls it from the parse alone</b>, once per literal it reaches, in source order, on the thread the
/// parse runs on. It must be safe to call concurrently from parses on other threads (convention 10). An exception it
/// throws leaves the parse, which changes no shared state.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s13.2.7.2 IsValidRegularExpressionLiteral, JSD-0083 D7; IP=Low; Security=Medium; Resources=0; Fingerprint=3808AD
// Broiler-Human:        PENDING
public interface IRegularExpressionValidator
{
    /// <summary>Answers whether a regular-expression literal is valid (ECMA-262 clause 13.2.7.2, IsValidRegularExpressionLiteral).</summary>
    /// <param name="pattern">The literal's body, its source text between the two slashes, as written (BodyText).</param>
    /// <param name="flags">The literal's flags, its source text after the second slash, as written (FlagText); empty for none.</param>
    /// <returns>The validator's verdict.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13.2.7.2 IsValidRegularExpressionLiteral; IP=Low; Security=Medium; Resources=1; Fingerprint=001FDC
    // Broiler-Human:        PENDING
    RegularExpressionVerdict Validate(string pattern, string flags);
}

/// <summary>How a regular-expression validator answers (Broiler.VM's JSD-0083, decision D7).</summary>
// Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 provider answer; IP=Low; Security=Low; Resources=0; Fingerprint=D5A086
// Broiler-Human:        PENDING
public enum RegularExpressionStatus
{
    /// <summary>The literal is valid: IsValidRegularExpressionLiteral is <see langword="true"/>.</summary>
    Valid = 0,

    /// <summary>The literal is not valid: an early error, which the library reports at the literal's span.</summary>
    Invalid = 1,

    /// <summary>
    /// The validator's own nesting bound was passed before any refusal (catalogue row EX0026,
    /// <c>limit.regexp.too-deep</c>): an engine's limit, which the library reports at the literal's span.
    /// </summary>
    LimitExceeded = 2,
}

/// <summary>
/// A regular-expression validator's verdict on one literal (Broiler.VM's JSD-0083, decision D7): valid, or invalid with
/// the failing condition's message identifier where the validator can tell it and EE0019's
/// <c>regexp.literal.invalid</c> where it cannot, with an argument, the validator's own code and a position.
/// </summary>
/// <remarks>
/// A validator that tells conditions apart (Broiler.Regex's validate entry) gives the condition's identifier, its argument
/// and its code; one that tells them apart only by message text (Broiler.VM's matcher) gives
/// <c>regexp.literal.invalid</c>, its message as the argument, and code 0. Revision 19 of Broiler.VM's registry maps every
/// regular-expression condition to one registry code (decision D12), so the condition named changes no code it renders.
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 provider answer; IP=Low; Security=Medium; Resources=0; Fingerprint=50A016
// Broiler-Human:        PENDING
public readonly record struct RegularExpressionVerdict
{
    /// <summary>The verdict of a valid literal.</summary>
    public static RegularExpressionVerdict Valid => default;

    /// <summary>Valid, invalid or past the validator's nesting bound.</summary>
    public RegularExpressionStatus Status { get; private init; }

    /// <summary>The validator's own code for the condition, as its contract numbers it (Broiler.Regex's <c>RegexDiagnosticCode</c>); 0 for none.</summary>
    public int Code { get; private init; }

    /// <summary>The condition's message identifier, of the catalogue's form (<c>early-errors.md</c> section 6); null for a valid literal.</summary>
    public string? MessageId { get; private init; }

    /// <summary>The identifier's argument, as the validator gives it; null for none.</summary>
    public string? Argument { get; private init; }

    /// <summary>The validator's position for the condition: a code-unit offset into the pattern, or into the flags when <see cref="InFlags"/> holds.</summary>
    public int Position { get; private init; }

    /// <summary>Whether <see cref="Position"/> is in the flags rather than the pattern.</summary>
    public bool InFlags { get; private init; }

    /// <summary>The verdict of an invalid literal.</summary>
    /// <param name="messageId">The failing condition's message identifier, or <c>regexp.literal.invalid</c>.</param>
    /// <param name="argument">The identifier's argument; null for none.</param>
    /// <param name="code">The validator's own code for the condition; 0 for none.</param>
    /// <param name="position">The code-unit offset of the condition, in the pattern or in the flags; zero or more.</param>
    /// <param name="inFlags">Whether the position is in the flags.</param>
    /// <returns>The verdict.</returns>
    /// <exception cref="ArgumentException">The message identifier is null or empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The position is negative.</exception>
    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 provider answer; IP=Low; Security=Medium; Resources=1; Fingerprint=113BAF
    // Broiler-Human:        PENDING
    public static RegularExpressionVerdict Invalid(string messageId, string? argument, int code, int position, bool inFlags) =>
        Refusal(RegularExpressionStatus.Invalid, messageId, argument, code, position, inFlags);

    /// <summary>The verdict of a literal nested past the validator's own bound (catalogue row EX0026).</summary>
    /// <param name="messageId">The message identifier, <c>limit.regexp.too-deep</c> or the validator's own.</param>
    /// <param name="argument">The identifier's argument; null for none.</param>
    /// <param name="code">The validator's own code for its nesting limit; 0 for none.</param>
    /// <param name="position">The code-unit offset, in the pattern, of the group or class that opens the level past the bound; zero or more.</param>
    /// <returns>The verdict.</returns>
    /// <exception cref="ArgumentException">The message identifier is null or empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The position is negative.</exception>
    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 provider answer, catalogue EX0026; IP=Low; Security=Medium; Resources=1; Fingerprint=BEED96
    // Broiler-Human:        PENDING
    public static RegularExpressionVerdict NestedTooDeep(string messageId, string? argument, int code, int position) =>
        Refusal(RegularExpressionStatus.LimitExceeded, messageId, argument, code, position, inFlags: false);

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D7 provider answer; IP=Low; Security=Medium; Resources=1; Fingerprint=B948BF
    // Broiler-Human:        PENDING
    private static RegularExpressionVerdict Refusal(RegularExpressionStatus status, string messageId, string? argument, int code, int position, bool inFlags)
    {
        ArgumentException.ThrowIfNullOrEmpty(messageId);
        ArgumentOutOfRangeException.ThrowIfNegative(position);
        return new RegularExpressionVerdict
        {
            Status = status,
            MessageId = messageId,
            Argument = argument,
            Code = code,
            Position = position,
            InFlags = inFlags,
        };
    }
}
