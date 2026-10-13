// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   5
// Annotated:        5/5
// Exempt:           1
// Human-reviewed:   0/5
// IP risk:          Low
// Security risk:    High
// Criteria:         6/6
// Resource impact:  1/10 max
// Unverified:       5
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Numerics;

namespace Broiler.ECMAScript.Numbers;

/// <summary>
/// The widest BigInt an operation of <see cref="BigIntMath"/> may produce, in bits (contract
/// convention 5): an operation whose result would be wider answers
/// <see cref="ResultStatus.LimitExceeded"/>, and never builds the value.
/// </summary>
/// <remarks>
/// <para>
/// <b>The width is <see cref="BigInteger.GetBitLength"/>'s</b>: the shortest two's-complement length
/// without the sign bit. For a non-negative value that is the magnitude's bit count; for a negative
/// power of two it is one less, so under a policy of 2^20 bits <c>-(2n ** 1048576n)</c> is admitted and
/// <c>2n ** 1048576n</c> is not. ECMA-262 names no width limit (6.1.6.2); the limit is each engine's
/// policy (decision SJL-D-15), and refusing past it is the engine's <c>RangeError</c>.
/// </para>
/// <para>
/// <b>Broiler.VM passes 2^20 bits</b>, its <c>JsBigInt.MaximumBits</c>. <b>Broiler.JS passes
/// <see cref="Unbounded"/></b> (decision SJL-D-15 (a)), which is the representation's own limit,
/// <see cref="RepresentationLimit"/> bits: no operation builds a wider value, so no .NET exception
/// reaches a script.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.6.2; IP=Low; Security=High; Resources=1; Fingerprint=BE9B4E
// Broiler-Falsified-If: a value exactly MaximumBits wide is refused, or one a bit wider is admitted
// Broiler-Human:        PENDING
public readonly record struct BigIntPolicy
{
    /// <summary>The widest value the library builds under any policy: 2^31 - 1 bits.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.6.2; IP=Low; Security=High; Resources=1; Fingerprint=426F36
    // Broiler-Falsified-If: an operation under any policy builds a value wider than 2^31 - 1 bits, so a .NET size exception can reach a script
    // Broiler-Human:        PENDING
    public const long RepresentationLimit = int.MaxValue;

    /// <summary>Creates a policy.</summary>
    /// <param name="maximumBits">The widest admitted value, from 1 to <see cref="RepresentationLimit"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException">The width is outside that range.</exception>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.6.2; IP=Low; Security=High; Resources=1; Fingerprint=998980
    // Broiler-Falsified-If: a width below 1 or above RepresentationLimit makes a policy
    // Broiler-Human:        PENDING
    public BigIntPolicy(long maximumBits)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumBits, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maximumBits, RepresentationLimit);
        MaximumBits = maximumBits;
    }

    /// <summary>The policy of decision SJL-D-15 (a): no limit but the representation's.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.6.2; IP=Low; Security=High; Resources=1; Fingerprint=42A7C2
    // Broiler-Falsified-If: Unbounded is narrower or wider than RepresentationLimit
    // Broiler-Human:        PENDING
    public static BigIntPolicy Unbounded => new(RepresentationLimit);

    /// <summary>The widest admitted value, in bits.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.6.2; IP=Low; Security=High; Resources=1; Fingerprint=EE644C
    // Broiler-Falsified-If: MaximumBits is not the width the policy was made with
    // Broiler-Human:        PENDING
    public long MaximumBits { get; }

    /// <summary>Whether <paramref name="value"/> is within the policy.</summary>
    /// <param name="value">A value.</param>
    /// <returns><see langword="true"/> when its width is at most <see cref="MaximumBits"/>.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.6.2; IP=Low; Security=High; Resources=1; Fingerprint=5675C8
    // Broiler-Falsified-If: a value wider than MaximumBits is admitted, so an operation under the policy answers a value past the engine's ceiling
    // Broiler-Human:        PENDING
    public bool Admits(BigInteger value) => value.GetBitLength() <= MaximumBits;
}
