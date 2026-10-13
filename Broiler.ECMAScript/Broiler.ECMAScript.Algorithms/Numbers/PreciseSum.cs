// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   7
// Annotated:        7/7
// Exempt:           8
// Human-reviewed:   0/7
// IP risk:          Low
// Security risk:    Medium
// Criteria:         0/0
// Resource impact:  2/10 max
// Unverified:       7
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Generic;
using System.Numerics;

namespace Broiler.ECMAScript.Numbers;

/// <summary>
/// The sum <c>Math.sumPrecise</c> (ECMA-262 21.3.2.34) computes, as an accumulator the engine
/// feeds: the exact sum of the Numbers added, rounded once to the nearest Number, with the clause's
/// states for NaN, the infinities and -0.
/// </summary>
/// <remarks>
/// <para>
/// <b>The engine iterates, the accumulator sums.</b> Getting the iterator, stepping it, refusing a
/// value that is not a Number with a <c>TypeError</c>, counting past 2^53 - 1 and closing the iterator
/// are the engine's steps, because they run guest code; it hands each Number to <see cref="Add"/>
/// and reads <see cref="Result"/> at the end.
/// </para>
/// <para>
/// <b>The sum is exact.</b> The finite values are kept as a non-overlapping expansion of doubles
/// (Shewchuk's grow-expansion, which the clause's note names), whose length is bounded by the
/// exponent range, and a count of multiples of 2^1024 that a partial overflowed into, so no partial
/// is ever an infinity; the result is that exact value rounded once, ties to even.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 21.3.2.34; IP=Low; Security=Medium; Resources=2; Fingerprint=326634
// Broiler-Human:        PENDING
public sealed class PreciseSum
{
    /// <summary>2^1023, half of the unit an overflowing partial is split into.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.3.2.34; IP=Low; Security=Low; Resources=1; Fingerprint=976965
    // Broiler-Human:        PENDING
    private const double TwoTo1023 = 8.98846567431157953865e+307;

    /// <summary>The non-overlapping partials, smallest magnitude first.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.3.2.34; IP=Low; Security=Low; Resources=1; Fingerprint=9214DB
    // Broiler-Human:        PENDING
    private readonly List<double> partials = new();

    /// <summary>The multiples of 2^1024 split off overflowing partials, signed.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.3.2.34; IP=Low; Security=Low; Resources=1; Fingerprint=24228D
    // Broiler-Human:        PENDING
    private long overflow;

    /// <summary>The clause's state: minus-zero, finite, plus-infinity, minus-infinity or not-a-number.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.3.2.34; IP=Low; Security=Low; Resources=1; Fingerprint=F91CA1
    // Broiler-Human:        PENDING
    private State state = State.MinusZero;

    /// <summary>The states of the clause's steps 3 to 11.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.3.2.34; IP=Low; Security=Low; Resources=1; Fingerprint=B81E40
    // Broiler-Human:        PENDING
    private enum State
    {
        MinusZero,
        Finite,
        PlusInfinity,
        MinusInfinity,
        NotANumber,
    }

    /// <summary>Adds one Number, as the clause's loop does once its value is known to be a Number (step 9.d to 9.f).</summary>
    /// <param name="value">The Number.</param>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.3.2.34; IP=Low; Security=Medium; Resources=2; Fingerprint=2404BB
    // Broiler-Human:        PENDING
    public void Add(double value)
    {
        if (state == State.NotANumber)
            return;

        if (double.IsNaN(value))
        {
            state = State.NotANumber;
        }
        else if (double.IsPositiveInfinity(value))
        {
            state = state == State.MinusInfinity ? State.NotANumber : State.PlusInfinity;
        }
        else if (double.IsNegativeInfinity(value))
        {
            state = state == State.PlusInfinity ? State.NotANumber : State.MinusInfinity;
        }
        else if (!(value == 0 && double.IsNegative(value)) && state is State.MinusZero or State.Finite)
        {
            state = State.Finite;
            if (value != 0)
                Grow(value);
        }
    }

    /// <summary>The clause's result (steps 10 to 15): NaN, an infinity, -0, or the exact sum rounded to the nearest Number.</summary>
    /// <returns>The sum of every Number added so far.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.3.2.34; IP=Low; Security=Medium; Resources=2; Fingerprint=EB5782
    // Broiler-Human:        PENDING
    public double Result()
    {
        switch (state)
        {
            case State.NotANumber:
                return double.NaN;
            case State.PlusInfinity:
                return double.PositiveInfinity;
            case State.MinusInfinity:
                return double.NegativeInfinity;
            case State.MinusZero:
                return -0.0;
        }

        if (overflow == 0 && partials.Count <= 1)
            return partials.Count == 0 ? 0 : partials[0];

        // The exact sum in units of 2^-1074, every partial an integer there, rounded once.
        var total = new BigInteger(overflow) << (1024 + 1074);
        foreach (var partial in partials)
            total += Units(partial);

        return total.IsZero ? 0 : ExactRounding.ToDouble(total, BigInteger.One << 1074);
    }

    /// <summary>Grows the expansion by one finite non-zero double, splitting off 2^1024 wherever a partial would overflow.</summary>
    /// <param name="value">The double.</param>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.3.2.34; IP=Low; Security=Medium; Resources=2; Fingerprint=5D5C4E
    // Broiler-Human:        PENDING
    private void Grow(double value)
    {
        var kept = 0;
        var x = value;
        for (var at = 0; at < partials.Count; at++)
        {
            var y = partials[at];
            if (Math.Abs(x) < Math.Abs(y))
                (x, y) = (y, x);

            var high = x + y;
            if (double.IsInfinity(high))
            {
                // |x| >= |y| and the sum overflows, so x is at least 2^1023 in magnitude and the same
                // sign as y: subtracting 2^1024 from it in two exact halves leaves a sum that does not
                // overflow, and the 2^1024 is counted.
                var sign = x > 0 ? 1 : -1;
                overflow += sign;
                x = (x - (sign * TwoTo1023)) - (sign * TwoTo1023);
                high = x + y;
            }

            // Knuth's two-sum, exact whichever operand is larger.
            var virtualY = high - x;
            var low = (x - (high - virtualY)) + (y - virtualY);
            if (low != 0)
                partials[kept++] = low;

            x = high;
        }

        partials.RemoveRange(kept, partials.Count - kept);
        if (x != 0)
            partials.Add(x);
    }

    /// <summary>A finite double as an integer count of 2^-1074.</summary>
    /// <param name="value">The double.</param>
    /// <returns>Its exact value times 2^1074.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.6.1; IP=Low; Security=Low; Resources=1; Fingerprint=DFE2BB
    // Broiler-Human:        PENDING
    private static BigInteger Units(double value)
    {
        if (value == 0)
            return BigInteger.Zero;

        var bits = DoubleBits.Of(Math.Abs(value));
        var units = new BigInteger(bits.Significand) << (bits.Exponent + 1074);
        return value < 0 ? -units : units;
    }
}
