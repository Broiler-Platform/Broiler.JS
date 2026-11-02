// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   8
// Annotated:        8/8
// Exempt:           5
// Human-reviewed:   0/8
// IP risk:          Low
// Security risk:    High
// Criteria:         3/3
// Resource impact:  1/10 max
// Unverified:       8
//
// GENERATED - DO NOT EDIT MANUALLY

using System;

namespace Broiler.ECMAScript.Syntax;

/// <summary>
/// The request's limits (Broiler.VM's JSD-0080, the request's "Limits" and its decision D27; JSD-0083, decisions D11 and
/// D14): the nesting depth, a bounded parameter; the diagnostic cap; and first error or all errors. The tree depth is a
/// constant of the library, <see cref="MaximumTreeDepth"/>, which no request sets.
/// </summary>
/// <remarks>
/// <para>
/// <b>Limits are checked before the stack is</b> (the contract's properties; convention 5): a source nested deeper than
/// <see cref="NestingDepth"/> is refused with the contract's nesting diagnostic (catalogue row EX0011), and a tree deeper
/// than <see cref="MaximumTreeDepth"/> with its tree-depth diagnostic (row EX0012), each a refusal, never a lost process.
/// </para>
/// <para>
/// <b>The parse charges no limit at this stage</b>: the tokenizer and the parser that check them are stages 4b and 4c's,
/// and stage 4e derives the stack the tree-depth bound requires, as Broiler.VM's JSD-0022 derived Broiler.VM's, and states
/// it beside the constant (decision D14).
/// </para>
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=JSD-0080 D27, JSD-0083 D11 D14 limits; IP=Low; Security=Medium; Resources=0; Fingerprint=8281B1
// Broiler-Human:        PENDING
public sealed class ParseLimits
{
    /// <summary>
    /// The deepest tree the library builds (catalogue row EX0012), a constant of the library and no request's (decision
    /// D14): Broiler.VM's bound, <c>SliceParseOptions.MaximumTreeDepth</c>, which a test of Broiler.VM holds equal to this
    /// one so that its rule N19's text checks stand. Tree depth is charged as Broiler.VM's parser charges it, one level for
    /// each node a binary chain or a member or call chain adds, and none for a parenthesis, a template or a substitution,
    /// a left operand's spine adding its own length to the chain it begins.
    /// </summary>
    /// <remarks>
    /// The derivation must admit this bound on the stack Broiler.VM's <c>CompilationStack</c> declares; if it cannot, the
    /// library is reworked with explicit stacks, and the bound is not lowered (decision D14). Stage 4e states the stack.
    /// </remarks>
    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D14 tree-depth bound, JSD-0022; IP=Low; Security=High; Resources=0; Fingerprint=6DB2AB
    // Broiler-Falsified-If: the constant differs from Broiler.VM's SliceParseOptions.MaximumTreeDepth, or a tree deeper than it is built rather than refused once the parser exists
    // Broiler-Human:        PENDING
    public const int MaximumTreeDepth = 10_000;

    /// <summary>
    /// The deepest nesting a request may ask the library to admit (catalogue row EX0011). Broiler.VM passes its own default
    /// within its own supported maximum, <c>SliceParseOptions.DefaultMaximumNestingDepth</c> and
    /// <c>MaximumSupportedNestingDepth</c>, and Broiler.JS this constant (decisions D14 and D47).
    /// </summary>
    /// <remarks>
    /// <b>At least Broiler.VM's supported maximum</b>, which its options admit and pass, so that no request Broiler.VM
    /// makes today is refused when it is made. Stage 4e's measurement on the stack the library documents confirms it, or
    /// raises it; it is never set below Broiler.VM's.
    /// </remarks>
    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D14 D47 nesting bound; IP=Low; Security=High; Resources=0; Fingerprint=2C4111
    // Broiler-Falsified-If: the constant is below Broiler.VM's SliceParseOptions.MaximumSupportedNestingDepth, so a nesting depth Broiler.VM passes today is refused when the request is made
    // Broiler-Human:        PENDING
    public const int MaximumSupportedNestingDepth = 512;

    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D14 nesting default; IP=Low; Security=Low; Resources=0; Fingerprint=2961CA
    // Broiler-Human:        PENDING
    private readonly DepthLimit nestingDepth = new(MaximumSupportedNestingDepth);
    private readonly int diagnosticCap = int.MaxValue;
    private readonly ErrorReporting reporting = ErrorReporting.FirstError;

    /// <summary>
    /// The limits a request takes when its caller states none: the library's supported maximum nesting, first error, and
    /// no cap but the source's own.
    /// </summary>
    public static ParseLimits Default { get; } = new();

    /// <summary>The deepest syntactic nesting the parse admits: from 1 to <see cref="MaximumSupportedNestingDepth"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The depth is below 1 or above <see cref="MaximumSupportedNestingDepth"/>.</exception>
    // Broiler-AI:           Origin=AI; Spec=JSD-0080 D27, JSD-0083 D14 nesting parameter; IP=Low; Security=High; Resources=1; Fingerprint=32674F
    // Broiler-Falsified-If: a nesting depth below 1 or above MaximumSupportedNestingDepth is admitted
    // Broiler-Human:        PENDING
    public DepthLimit NestingDepth
    {
        get => nestingDepth;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value.Maximum, 1, nameof(NestingDepth));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Maximum, MaximumSupportedNestingDepth, nameof(NestingDepth));
            nestingDepth = value;
        }
    }

    /// <summary>
    /// The most diagnostics a parse reports (decision D11; Broiler.VM passes the cap its wide compiler applies today). Under
    /// <see cref="ErrorReporting.FirstError"/> a parse reports one at most, whatever the cap.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The cap is below 1.</exception>
    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D11 diagnostic cap; IP=Low; Security=Low; Resources=1; Fingerprint=E26853
    // Broiler-Human:        PENDING
    public int DiagnosticCap
    {
        get => diagnosticCap;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1, nameof(DiagnosticCap));
            diagnosticCap = value;
        }
    }

    /// <summary>First error or all errors (decision D11): both engines ask for the first error at SJL-13 and SJL-14.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is not an <see cref="ErrorReporting"/>.</exception>
    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D11 first error or all errors; IP=Low; Security=Low; Resources=1; Fingerprint=597F79
    // Broiler-Human:        PENDING
    public ErrorReporting Reporting
    {
        get => reporting;
        init
        {
            if (value is not (ErrorReporting.FirstError or ErrorReporting.AllErrors))
                throw new ArgumentOutOfRangeException(nameof(Reporting), value, "Not an error-reporting mode.");
            reporting = value;
        }
    }
}

/// <summary>Whether a parse stops at its first refusal or goes on (Broiler.VM's JSD-0083, decision D11).</summary>
// Broiler-AI:           Origin=AI; Spec=JSD-0083 D11 first error or all errors; IP=Low; Security=Low; Resources=0; Fingerprint=19A95B
// Broiler-Human:        PENDING
public enum ErrorReporting
{
    /// <summary>
    /// The parse stops at its first refusal, in the library's order: a refusal as the parse reaches its construct, and a
    /// scope's declared-name conflicts when the scope closes, in the source order of the conflicting declarations.
    /// </summary>
    FirstError = 0,

    /// <summary>
    /// The parse reports every refusal, up to the cap, with the library's own recovery, which reproduces neither engine's;
    /// a parameter for an engine that asks for it later, SJL-15's model among them.
    /// </summary>
    AllErrors = 1,
}
