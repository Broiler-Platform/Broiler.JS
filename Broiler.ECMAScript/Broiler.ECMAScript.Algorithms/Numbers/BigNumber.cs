// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   20
// Annotated:        20/20
// Exempt:           2
// Human-reviewed:   0/20
// IP risk:          Low
// Security risk:    High
// Criteria:         1/1
// Resource impact:  3/10 max
// Unverified:       20
//
// GENERATED - DO NOT EDIT MANUALLY

using System;

namespace Broiler.ECMAScript.Numbers;

/// <summary>
/// A non-negative integer of at most <see cref="Capacity"/> 32-bit limbs, held in caller-owned
/// storage and changed in place: the exact arithmetic of digit generation (Steele and White's
/// free-format algorithm) without an allocation per step.
/// </summary>
/// <remarks>
/// Every quantity digit generation needs for a double fits: the value and its interval ends scaled
/// to integers stay below 2^1100 for every finite double and radix up to 36. An operation that would
/// leave the storage is a defect of the caller and throws.
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.6.1.20; IP=Low; Security=Medium; Resources=2; Fingerprint=D0D4E0
// Broiler-Human:        PENDING
internal ref struct BigNumber
{
    /// <summary>The storage, in 32-bit limbs, every caller provides.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.6.1.20; IP=Low; Security=Medium; Resources=2; Fingerprint=42DA60
    // Broiler-Human:        PENDING
    internal const int Capacity = 40;

    private readonly Span<uint> limbs;
    private int length;

    /// <summary>Creates a zero over caller-owned storage of <see cref="Capacity"/> limbs.</summary>
    /// <param name="storage">The limbs, least significant first.</param>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.6.1.20; IP=Low; Security=Medium; Resources=2; Fingerprint=2D7F3F
    // Broiler-Human:        PENDING
    internal BigNumber(Span<uint> storage)
    {
        limbs = storage;
        limbs.Clear();
        length = 0;
    }

    /// <summary>Whether the number is zero.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.6.1.20; IP=Low; Security=Medium; Resources=2; Fingerprint=D4DBD2
    // Broiler-Human:        PENDING
    internal readonly bool IsZero => length == 0;

    /// <summary>Sets the number to <paramref name="value"/>.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.6.1.20; IP=Low; Security=Medium; Resources=2; Fingerprint=B88FF9
    // Broiler-Human:        PENDING
    internal void Set(ulong value)
    {
        limbs[..length].Clear();
        limbs[0] = (uint)value;
        limbs[1] = (uint)(value >> 32);
        length = value == 0 ? 0 : value >> 32 == 0 ? 1 : 2;
    }

    /// <summary>Sets the number to <paramref name="other"/>'s value.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.6.1.20; IP=Low; Security=Medium; Resources=2; Fingerprint=1289FC
    // Broiler-Human:        PENDING
    internal void Set(in BigNumber other)
    {
        limbs[..length].Clear();
        other.limbs[..other.length].CopyTo(limbs);
        length = other.length;
    }

    /// <summary>Multiplies by 2^<paramref name="bits"/>.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.6.1.20; IP=Low; Security=High; Resources=3; Fingerprint=E4CE1E
    // Broiler-Falsified-If: a shift that would leave the storage writes past it instead of throwing
    // Broiler-Human:        PENDING
    internal void ShiftLeft(int bits)
    {
        if (length == 0 || bits == 0)
            return;

        var whole = bits / 32;
        var part = bits % 32;
        var top = length + whole + 1;
        if (top > Capacity)
            throw new InvalidOperationException("A digit-generation quantity exceeded its storage.");

        if (part == 0)
        {
            for (var at = length - 1; at >= 0; at--)
                limbs[at + whole] = limbs[at];
        }
        else
        {
            limbs[length + whole] = 0;
            for (var at = length - 1; at >= 0; at--)
            {
                limbs[at + whole + 1] |= limbs[at] >> (32 - part);
                limbs[at + whole] = limbs[at] << part;
            }
        }

        limbs[..whole].Clear();
        length = top;
        Trim();
    }

    /// <summary>Multiplies by a small factor.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.6.1.20; IP=Low; Security=Medium; Resources=2; Fingerprint=4F3465
    // Broiler-Human:        PENDING
    internal void Multiply(uint factor)
    {
        ulong carry = 0;
        for (var at = 0; at < length; at++)
        {
            var product = ((ulong)limbs[at] * factor) + carry;
            limbs[at] = (uint)product;
            carry = product >> 32;
        }

        if (carry != 0)
        {
            if (length == Capacity)
                throw new InvalidOperationException("A digit-generation quantity exceeded its storage.");
            limbs[length++] = (uint)carry;
        }
    }

    /// <summary>Multiplies by <paramref name="radix"/>^<paramref name="exponent"/>.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.6.1.20; IP=Low; Security=Medium; Resources=2; Fingerprint=8BB173
    // Broiler-Human:        PENDING
    internal void MultiplyPower(uint radix, int exponent)
    {
        // The largest power of the radix that fits a limb, applied as often as it goes in.
        uint chunk = radix;
        var chunkExponent = 1;
        while ((ulong)chunk * radix <= uint.MaxValue)
        {
            chunk *= radix;
            chunkExponent++;
        }

        while (exponent >= chunkExponent)
        {
            Multiply(chunk);
            exponent -= chunkExponent;
        }

        while (exponent-- > 0)
            Multiply(radix);
    }

    /// <summary>Adds a value below 2^32.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 19.2.5; IP=Low; Security=Medium; Resources=1; Fingerprint=0B637C
    // Broiler-Human:        PENDING
    internal void AddSmall(uint addend)
    {
        ulong carry = addend;
        for (var at = 0; carry != 0 && at < length; at++)
        {
            var sum = limbs[at] + carry;
            limbs[at] = (uint)sum;
            carry = sum >> 32;
        }

        if (carry != 0)
        {
            if (length == Capacity)
                throw new InvalidOperationException("A digit-generation quantity exceeded its storage.");
            limbs[length++] = (uint)carry;
        }
    }

    /// <summary>The nearest double, ties to even: the leading 64 bits rounded once, everything below them sticky.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.6.1; IP=Low; Security=Medium; Resources=1; Fingerprint=3553C3
    // Broiler-Human:        PENDING
    internal readonly double ToDouble()
    {
        var bits = BitLength;
        if (bits <= 64)
            return ExactRounding.ToDouble(TopBits(0, 64), 0, false);

        var shift = bits - 64;
        var sticky = false;
        for (var limb = 0; limb < shift / 32 && !sticky; limb++)
            sticky = limbs[limb] != 0;

        if (!sticky && shift % 32 != 0)
            sticky = (limbs[shift / 32] & ((1U << (shift % 32)) - 1)) != 0;

        return ExactRounding.ToDouble(TopBits(shift, 64), shift, sticky);
    }

    /// <summary>Adds <paramref name="other"/>.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.6.1.20; IP=Low; Security=Medium; Resources=2; Fingerprint=632F41
    // Broiler-Human:        PENDING
    internal void Add(in BigNumber other)
    {
        var count = Math.Max(length, other.length);
        ulong carry = 0;
        for (var at = 0; at < count; at++)
        {
            var sum = (ulong)limbs[at] + (at < other.length ? other.limbs[at] : 0U) + carry;
            limbs[at] = (uint)sum;
            carry = sum >> 32;
        }

        length = count;
        if (carry != 0)
        {
            if (length == Capacity)
                throw new InvalidOperationException("A digit-generation quantity exceeded its storage.");
            limbs[length++] = (uint)carry;
        }
    }

    /// <summary>Subtracts <paramref name="other"/>, which is no greater than this number.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.6.1.20; IP=Low; Security=Medium; Resources=2; Fingerprint=5667BA
    // Broiler-Human:        PENDING
    internal void Subtract(in BigNumber other)
    {
        long borrow = 0;
        for (var at = 0; at < length; at++)
        {
            var difference = (long)limbs[at] - (at < other.length ? other.limbs[at] : 0U) - borrow;
            borrow = difference < 0 ? 1 : 0;
            limbs[at] = (uint)difference;
        }

        Trim();
    }

    /// <summary>The bit length of the number, zero for zero.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.6.1.20; IP=Low; Security=Medium; Resources=2; Fingerprint=CE7C1D
    // Broiler-Human:        PENDING
    internal readonly int BitLength => length == 0 ? 0 : (32 * length) - System.Numerics.BitOperations.LeadingZeroCount(limbs[length - 1]);

    /// <summary>Compares two numbers.</summary>
    /// <returns>Negative, zero or positive as <paramref name="left"/> is below, equal to or above <paramref name="right"/>.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.6.1.20; IP=Low; Security=Medium; Resources=2; Fingerprint=BDD984
    // Broiler-Human:        PENDING
    internal static int Compare(in BigNumber left, in BigNumber right)
    {
        if (left.length != right.length)
            return left.length < right.length ? -1 : 1;

        for (var at = left.length - 1; at >= 0; at--)
        {
            if (left.limbs[at] != right.limbs[at])
                return left.limbs[at] < right.limbs[at] ? -1 : 1;
        }

        return 0;
    }

    /// <summary>Compares <paramref name="left"/> + <paramref name="addend"/> with <paramref name="right"/>, without forming the sum.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.6.1.20; IP=Low; Security=Medium; Resources=2; Fingerprint=13D396
    // Broiler-Human:        PENDING
    internal static int CompareSum(in BigNumber left, in BigNumber addend, in BigNumber right)
    {
        var count = Math.Max(Math.Max(left.length, addend.length), right.length) + 1;

        // The difference right - (left + addend), limb by limb from the top, decides as soon as it can:
        // a running comparison carries a borrow upward, so it is computed from the bottom first.
        long borrow = 0;
        var nonZero = false;
        for (var at = 0; at < count; at++)
        {
            var difference = (long)(at < right.length ? right.limbs[at] : 0U)
                - (at < left.length ? left.limbs[at] : 0U)
                - (at < addend.length ? addend.limbs[at] : 0U)
                - borrow;
            borrow = 0;
            while (difference < 0)
            {
                difference += 1L << 32;
                borrow++;
            }

            nonZero |= difference != 0;
        }

        if (borrow != 0)
            return 1;

        return nonZero ? -1 : 0;
    }

    /// <summary>
    /// Divides by <paramref name="divisor"/>, leaving the remainder here, for a quotient known to be
    /// below 2^32.
    /// </summary>
    /// <returns>The quotient.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.6.1.20; IP=Low; Security=Medium; Resources=2; Fingerprint=E18736
    // Broiler-Human:        PENDING
    internal uint DivideRemainder(in BigNumber divisor)
    {
        if (Compare(this, divisor) < 0)
            return 0;

        // An estimate from the top 64 bits of this number over the top 32 bits of the divisor, one
        // more, is never above the quotient; the remainder is then corrected by whole divisors.
        var shift = divisor.BitLength - 32;
        var top = TopBits(shift, 64);
        var divisorTop = divisor.TopBits(shift, 32);
        var estimate = (uint)Math.Min(top / (divisorTop + 1), uint.MaxValue);
        if (estimate != 0)
            SubtractMultiple(divisor, estimate);

        var quotient = estimate;
        while (Compare(this, divisor) >= 0)
        {
            Subtract(divisor);
            quotient++;
        }

        return quotient;
    }

    /// <summary>The bits of this number from bit <paramref name="from"/> upward, at most <paramref name="count"/> of them.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.6.1.20; IP=Low; Security=Medium; Resources=2; Fingerprint=7CC67D
    // Broiler-Human:        PENDING
    private readonly ulong TopBits(int from, int count)
    {
        // The bits [start, end) span at most three limbs, since count is at most 64.
        var start = Math.Max(from, 0);
        var end = Math.Min(from + count, 32 * length);
        if (end <= start)
            return 0;

        var firstLimb = start / 32;
        UInt128 window = 0;
        for (var limb = (end - 1) / 32; limb >= firstLimb; limb--)
            window = (window << 32) | limbs[limb];

        window >>= start - (32 * firstLimb);
        var result = (ulong)(window & ((UInt128.One << (end - start)) - 1));
        return from < 0 ? result << -from : result;
    }

    /// <summary>Subtracts <paramref name="divisor"/> x <paramref name="factor"/>, which is no greater than this number.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.6.1.20; IP=Low; Security=Medium; Resources=2; Fingerprint=1FA402
    // Broiler-Human:        PENDING
    private void SubtractMultiple(in BigNumber divisor, uint factor)
    {
        ulong carry = 0;
        long borrow = 0;
        for (var at = 0; at < length; at++)
        {
            var product = (at < divisor.length ? (ulong)divisor.limbs[at] * factor : 0UL) + carry;
            carry = product >> 32;
            var difference = (long)limbs[at] - (uint)product - borrow;
            borrow = difference < 0 ? 1 : 0;
            limbs[at] = (uint)difference;
        }

        Trim();
    }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.6.1.20; IP=Low; Security=Medium; Resources=2; Fingerprint=776D00
    // Broiler-Human:        PENDING
    private void Trim()
    {
        while (length > 0 && limbs[length - 1] == 0)
            length--;
    }
}
