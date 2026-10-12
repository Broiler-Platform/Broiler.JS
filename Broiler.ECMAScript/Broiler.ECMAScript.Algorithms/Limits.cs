// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   6
// Annotated:        6/6
// Exempt:           2
// Human-reviewed:   0/6
// IP risk:          Low
// Security risk:    High
// Criteria:         2/2
// Resource impact:  1/10 max
// Unverified:       6
//
// GENERATED - DO NOT EDIT MANUALLY

using System;

namespace Broiler.ECMAScript;

/// <summary>
/// The longest result an operation may produce, in the units it documents: UTF-16 code units for a
/// string, bits for a BigInt (contract convention 5).
/// </summary>
/// <remarks>
/// Exceeding it is <see cref="ResultStatus.LimitExceeded"/>, never a shortened result. A library's
/// own constant ceilings are documented with the operation and set no lower than either engine
/// needs.
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=SJL architecture s4; IP=Low; Security=Medium; Resources=1; Fingerprint=FBF437
// Broiler-Human:        PENDING
public readonly record struct LengthLimit
{
    /// <summary>Creates a length limit.</summary>
    /// <param name="maximum">The longest permitted result; zero or more.</param>
    /// <exception cref="ArgumentOutOfRangeException">The maximum is negative.</exception>
    // Broiler-AI:           Origin=AI; Spec=SJL architecture s4; IP=Low; Security=Medium; Resources=1; Fingerprint=0D48D9
    // Broiler-Human:        PENDING
    public LengthLimit(long maximum)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maximum);
        Maximum = maximum;
    }

    /// <summary>The longest permitted result.</summary>
    public long Maximum { get; }

    /// <summary>Whether a result of <paramref name="length"/> units is within the limit.</summary>
    /// <param name="length">The length of the result an operation is about to produce.</param>
    /// <returns><see langword="true"/> when the length does not exceed <see cref="Maximum"/>.</returns>
    // Broiler-AI:           Origin=AI; Spec=SJL architecture s4; IP=Low; Security=High; Resources=1; Fingerprint=CD14A4
    // Broiler-Falsified-If: Admits returns true for a length greater than Maximum, so an operation that consults it produces a result longer than its caller allowed
    // Broiler-Human:        PENDING
    public bool Admits(long length) => length <= Maximum;
}

/// <summary>
/// The deepest nesting an operation may reach: of a syntax tree, a regular-expression pattern or a
/// JSON value (contract convention 5).
/// </summary>
/// <remarks>
/// <b>A limit is never the stack.</b> How deep the stack lets code recurse differs between JIT and
/// Native AOT, so only an explicit limit gives every mode the same answer; an operation checks this
/// before the stack would be exhausted, and exceeding it is <see cref="ResultStatus.LimitExceeded"/>.
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=SJL architecture s4; IP=Low; Security=Medium; Resources=1; Fingerprint=995D61
// Broiler-Human:        PENDING
public readonly record struct DepthLimit
{
    /// <summary>Creates a depth limit.</summary>
    /// <param name="maximum">The deepest permitted nesting; one or more.</param>
    /// <exception cref="ArgumentOutOfRangeException">The maximum is below one.</exception>
    // Broiler-AI:           Origin=AI; Spec=SJL architecture s4; IP=Low; Security=Medium; Resources=1; Fingerprint=14BC4C
    // Broiler-Human:        PENDING
    public DepthLimit(int maximum)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maximum, 1);
        Maximum = maximum;
    }

    /// <summary>The deepest permitted nesting.</summary>
    public int Maximum { get; }

    /// <summary>Whether nesting to <paramref name="depth"/> is within the limit.</summary>
    /// <param name="depth">The depth an operation is about to reach.</param>
    /// <returns><see langword="true"/> when the depth does not exceed <see cref="Maximum"/>.</returns>
    // Broiler-AI:           Origin=AI; Spec=SJL architecture s4; IP=Low; Security=High; Resources=1; Fingerprint=F3198B
    // Broiler-Falsified-If: Admits returns true for a depth greater than Maximum, so an operation that consults it nests deeper than its caller allowed and can exhaust the stack
    // Broiler-Human:        PENDING
    public bool Admits(int depth) => depth <= Maximum;
}
