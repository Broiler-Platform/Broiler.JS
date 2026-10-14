// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   7
// Annotated:        7/7
// Exempt:           2
// Human-reviewed:   0/7
// IP risk:          Low
// Security risk:    Medium
// Criteria:         0/0
// Resource impact:  1/10 max
// Unverified:       7
//
// GENERATED - DO NOT EDIT MANUALLY

using System;

namespace Broiler.ECMAScript.Numbers;

/// <summary>
/// How <see cref="Float16.Encode"/> writes a NaN, which ECMA-262 leaves to the implementation
/// (<c>NumericToRawBytes</c>, 25.1.3.17, step 1.b: "any implementation chosen IEEE 754-2019 binary16
/// format Not-a-Number encoding").
/// </summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 25.1.3.17; IP=Low; Security=Low; Resources=1; Fingerprint=3B058E
// Broiler-Human:        PENDING
public enum Float16NanPolicy
{
    /// <summary>Every NaN is written as the one quiet NaN <see cref="Float16.CanonicalNaN"/>, sign cleared. Broiler.VM's policy.</summary>
    Canonical = 0,

    /// <summary>
    /// A NaN keeps its sign and the ten most significant bits of the double's payload, and is made
    /// quiet. Broiler.JS's policy, which does not normalise.
    /// </summary>
    PreservePayload = 1,
}

/// <summary>
/// The IEEE 754-2019 binary16 format as ECMA-262 reads and writes it: <c>NumericToRawBytes</c> and
/// <c>RawBytesToNumeric</c> for <c>Float16</c> (25.1.3.17 and 25.1.3.15) and <c>Math.f16round</c>
/// (21.3.2.17).
/// </summary>
/// <remarks>
/// <b>The rounding is the library's own, from the double's bits</b>, to nearest with ties to even and
/// overflowing to an infinity at 65,520, the midpoint above the largest finite binary16. Rounding
/// through binary32 first would round twice; this rounds once.
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 25.1.3.17; IP=Low; Security=Medium; Resources=1; Fingerprint=ACE8D1
// Broiler-Human:        PENDING
public static class Float16
{
    /// <summary>The canonical quiet NaN, <c>0x7E00</c>.</summary>
    // Broiler-AI:           Origin=AI; Spec=IEEE 754-2019 3.4; IP=Low; Security=Low; Resources=1; Fingerprint=0B3E21
    // Broiler-Human:        PENDING
    public const ushort CanonicalNaN = 0x7E00;

    /// <summary>
    /// <c>NumericToRawBytes(Float16, value)</c>'s bits (ECMA-262 25.1.3.17): the binary16 nearest the
    /// Number, ties to even, or a NaN encoded under <paramref name="nanPolicy"/>.
    /// </summary>
    /// <param name="value">The Number.</param>
    /// <param name="nanPolicy">How a NaN is written.</param>
    /// <returns>The binary16 bits, sign included.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 25.1.3.17; IP=Low; Security=Medium; Resources=1; Fingerprint=3507E6
    // Broiler-Human:        PENDING
    public static ushort Encode(double value, Float16NanPolicy nanPolicy)
    {
        var bits = BitConverter.DoubleToUInt64Bits(value);
        var sign = (ushort)((bits >> 48) & 0x8000);
        var field = (int)((bits >> 52) & 0x7FF);
        var fraction = bits & ((1UL << 52) - 1);

        if (field == 0x7FF)
        {
            if (fraction == 0)
                return (ushort)(sign | 0x7C00);

            return nanPolicy == Float16NanPolicy.PreservePayload
                ? (ushort)(sign | CanonicalNaN | (ushort)(fraction >> 42))
                : CanonicalNaN;
        }

        var exponent = field - 1023;
        if (exponent > 15)
            return (ushort)(sign | 0x7C00);

        if (exponent >= -14)
        {
            // A normal binary16, unless rounding carries into the exponent - up to the infinity at
            // 65,520, which is what the carry from 0x7BFF makes.
            var encoded = (uint)((exponent + 15) << 10) | (uint)(fraction >> 42);
            return (ushort)(sign | RoundAway(encoded, fraction & ((1UL << 42) - 1), 1UL << 41));
        }

        // A subnormal binary16 or zero: the value in units of 2^-24, rounded once. A double below
        // 2^-1022 is far below the smallest binary16 and rounds to zero.
        if (field == 0 || exponent < -25)
            return sign;

        var significand = fraction | (1UL << 52);
        var shift = 28 - exponent;
        var units = (uint)(significand >> shift);
        var dropped = significand & ((1UL << shift) - 1);
        return (ushort)(sign | RoundAway(units, dropped, 1UL << (shift - 1)));
    }

    /// <summary><c>RawBytesToNumeric(Float16, bits)</c> (ECMA-262 25.1.3.15): the Number binary16 bits denote, exactly.</summary>
    /// <param name="bits">The binary16 bits.</param>
    /// <returns>The value, NaN for every NaN payload, and -0 for the negative zero.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 25.1.3.15; IP=Low; Security=Medium; Resources=1; Fingerprint=959A11
    // Broiler-Human:        PENDING
    public static double Decode(ushort bits)
    {
        var negative = (bits & 0x8000) != 0;
        var field = (bits >> 10) & 0x1F;
        var fraction = bits & 0x3FF;

        double magnitude;
        if (field == 0x1F)
        {
            if (fraction != 0)
                return double.NaN;

            magnitude = double.PositiveInfinity;
        }
        else if (field == 0)
        {
            magnitude = Math.ScaleB(fraction, -24);
        }
        else
        {
            magnitude = Math.ScaleB(fraction | 0x400, field - 25);
        }

        return negative ? -magnitude : magnitude;
    }

    /// <summary><c>Math.f16round</c> (ECMA-262 21.3.2.17): the Number rounded to the nearest binary16 and widened back.</summary>
    /// <param name="value">The Number.</param>
    /// <returns>The rounded value; NaN for NaN, and the zeros and infinities themselves.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.3.2.17; IP=Low; Security=Medium; Resources=1; Fingerprint=1AC49D
    // Broiler-Human:        PENDING
    public static double Round(double value) =>
        double.IsNaN(value) ? double.NaN : Decode(Encode(value, Float16NanPolicy.Canonical));

    /// <summary>Adds one to <paramref name="kept"/> when the dropped bits are above half, or half with <paramref name="kept"/> odd.</summary>
    /// <param name="kept">The truncated result.</param>
    /// <param name="dropped">The bits below it.</param>
    /// <param name="half">The weight of half a unit of <paramref name="kept"/>.</param>
    /// <returns>The result rounded to nearest, ties to even.</returns>
    // Broiler-AI:           Origin=AI; Spec=IEEE 754-2019 4.3.1; IP=Low; Security=Medium; Resources=1; Fingerprint=47696A
    // Broiler-Human:        PENDING
    private static uint RoundAway(uint kept, ulong dropped, ulong half) =>
        dropped > half || (dropped == half && (kept & 1) != 0) ? kept + 1 : kept;
}
