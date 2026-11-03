// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   9
// Annotated:        9/9
// Exempt:           5
// Human-reviewed:   0/9
// IP risk:          Low
// Security risk:    High
// Criteria:         4/4
// Resource impact:  1/10 max
// Unverified:       9
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
/// <b>From stage 4c the parser charges both limits before the stack</b>: the nesting depth at each site Broiler.VM's
/// <c>JsParser</c> charges, and also at each right operand of <c>**</c> and each class heritage; the tree depth for each
/// node a binary chain or a member or call chain adds, from the spine of the chain's first operand. Stage 4e derives the
/// stack the tree-depth bound requires, as Broiler.VM's JSD-0022 derived Broiler.VM's, and states it beside the constant
/// (decision D14). <i>(Revised 2026-11-01, at stage SJL-13.2c, when the parser was added; it read "<b>The parse charges no
/// limit at this stage</b>: the tokenizer and the parser that check them are stages 4b and 4c's, and stage 4e derives the
/// stack the tree-depth bound requires, as Broiler.VM's JSD-0022 derived Broiler.VM's, and states it beside the constant
/// (decision D14).")</i> <i>(Noted 2026-11-02, at stage SJL-13.2e: "Stage 4e derives the stack the tree-depth bound
/// requires ... and states it beside the constant" is done. The derivation, retained in
/// <c>docs/frontend/limits/derivation.md</c>, finds that the parse spends no stack per level of tree depth, and that the
/// nesting bound is what the stack pays for; <see cref="StackBytes"/> states the stack beside the constants.)</i>
/// <i>(Noted 2026-11-02, at stage SJL-13.2f, by the owner's decision of that day on decision D14's residual left-operand
/// shapes: "the tree depth for each node a binary chain or a member or call chain adds, from the spine of the chain's
/// first operand" no longer holds. Each chain node is charged its height, one more than the highest of all its children,
/// and refused when that height passes <see cref="MaximumTreeDepth"/>, so that a tree deeper than the constant is refused
/// wherever it is built: under a right operand, a call's argument, a computed key, a prefix operator's operand, an array
/// or object literal's element, a sequence, a conditional or a function body, as along a chain's left spine; and a tree
/// within it is accepted, however its chains stand beside or below one another.)</i>
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
    /// a left operand's spine adding its own length to the chain it begins. <i>(Noted 2026-11-02, at stage SJL-13.2f, by
    /// the owner's decision of that day: the depth counted is the tree's, the most chain nodes on any path from the root to
    /// a leaf, through every child, not a left operand's spine alone; every tree deeper than this constant is refused, and
    /// no tree within it.)</i>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Derived at stage SJL-13.2e as Broiler.VM's JSD-0022 derived Broiler.VM's</b> (<c>docs/frontend/limits/derivation.md</c>):
    /// the measured failure point on a stack, scaled to the declared stack, divided by a margin. The library has no failure
    /// point in tree depth: it builds a binary chain, a member or call chain and a decorator's member chain in a loop, and
    /// its declarations walk keeps a stack of its own, so a level of tree depth costs it no frame. At the tree-depth refusal
    /// the parse holds the same frames whether the chain is 10,001 nodes long or 100,000, and however a left operand's spine
    /// and its chain divide them (for a binary chain 20 at the JIT's first tier, 18 when the JIT has optimised it, 21 under
    /// Native AOT); and every tree of the home rule's deep-nesting corpus, at this bound, one past it and far past it, ends
    /// as its row expects on a thread of 64 KiB. So nothing in the library bounds the constant, and it is Broiler.VM's bound, which decision D14
    /// holds equal to this one, admitted on <see cref="StackBytes"/>, the stack Broiler.VM's <c>CompilationStack</c>
    /// declares. A walk of the tree that recursed once per level, as Broiler.VM's lowering does, is the consumer's to
    /// derive against its own stack, as JSD-0022 did.
    /// </para>
    /// <para>
    /// <i>(Revised 2026-11-02, at stage SJL-13.2e, when the derivation was made; it read "The derivation must admit this
    /// bound on the stack Broiler.VM's <c>CompilationStack</c> declares; if it cannot, the library is reworked with explicit
    /// stacks, and the bound is not lowered (decision D14). Stage 4e states the stack.")</i>
    /// </para>
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
    /// raises it; it is never set below Broiler.VM's. <i>(Noted 2026-11-02, at stage SJL-13.2e: the measurement confirms it
    /// and does not raise it. A source nested to this bound through the costliest construct measured, an object literal's
    /// computed key, alone or after the grammar's ten binary precedences, needs at most 3,416,352 bytes of stack under the
    /// JIT (every method at its optimised tier with profile-guided optimisation; 2,638,720 at its first tier and in its
    /// default runs) and 1,682,944 under Native AOT, a margin of about 4.9 and 10.0 on <see cref="StackBytes"/>; twice the
    /// bound would leave the JIT less than three.)</i>
    /// </remarks>
    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D14 D47 nesting bound; IP=Low; Security=High; Resources=0; Fingerprint=2C4111
    // Broiler-Falsified-If: the constant is below Broiler.VM's SliceParseOptions.MaximumSupportedNestingDepth, so a nesting depth Broiler.VM passes today is refused when the request is made
    // Broiler-Human:        PENDING
    public const int MaximumSupportedNestingDepth = 512;

    /// <summary>
    /// The stack the library documents for a parse (decision D14, "with the stack it requires stated beside it"): a caller
    /// parses on a thread of at least this many bytes, on which every source within the limits is parsed and every source
    /// past them refused, never a lost process. It is the stack Broiler.VM's <c>CompilationStack</c> declares, sixteen
    /// mebibytes, so that Broiler.VM's bound is admitted on Broiler.VM's stack.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>What the stack pays for is the nesting bound, not the tree depth</b> (<c>docs/frontend/limits/derivation.md</c>,
    /// stage SJL-13.2e). The parser recurses once per level of nesting through each construct, each level charged before
    /// the recursion, and refuses at the request's bound; between two charged levels it recurses only where the grammar
    /// bounds it: the precedence climb reads the right operands of a higher precedence with an explicit stack, not by
    /// recursion, and the right operand of <c>#x in</c> by one recursion, which cannot repeat without a level of nesting
    /// between. It recurses not at all per level of tree depth. Measured at the refusal, the deepest point of the
    /// recursion, a level of nesting costs from 1 to 10 frames and from 303 to 5,168 bytes at the JIT's first tier, its
    /// optimised tier and under Native AOT, by construct; the costliest construct measured, an object literal's computed
    /// key (<c>{[{[...]}]}</c>), alone or after the grammar's ten binary precedences, costs at the supported maximum, 512
    /// levels, 2,638,720 bytes at the JIT's first tier and in its default runs, 3,416,352 with every method at its
    /// optimised tier with profile-guided optimisation, and 1,682,944 under Native AOT. Each source of the home rule's
    /// deep-nesting corpus, all 54 measured (the derivation's section 3), parsed alone in its process, ends as its row
    /// expects on a thread of 1,744 KiB under the JIT and of 912 KiB under Native AOT, and every tree among them on 64 KiB.
    /// </para>
    /// <para>
    /// <b>The margin</b> is the declared stack over the most the parse was measured to need: about 4.9 under the JIT, with
    /// every method promoted (6.4 at its first tier and in its default runs), and 10.0 under Native AOT: the JIT's least
    /// below the six JSD-0022 took for Broiler.VM's tree depth, and every one above the three Broiler.VM's
    /// <c>MaximumSupportedNestingDepth</c> keeps. A construct that grew a frame per level would move these figures, and the
    /// derivation is to be measured again with it (<c>Broiler.ECMAScript.Probe --measure-frames</c>).
    /// </para>
    /// </remarks>
    // Broiler-AI:           Origin=AI; Spec=JSD-0083 D14 the stack the tree-depth constant requires, JSD-0022; IP=Low; Security=High; Resources=0; Fingerprint=2F9878
    // Broiler-Falsified-If: a source within the limits, parsed on a thread of this stack, ends the process rather than answering
    // Broiler-Human:        PENDING
    public const int StackBytes = 16 * 1024 * 1024;

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
