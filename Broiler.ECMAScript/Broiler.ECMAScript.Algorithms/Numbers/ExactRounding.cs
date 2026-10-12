// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   4
// Annotated:        4/4
// Exempt:           0
// Human-reviewed:   0/4
// IP risk:          Low
// Security risk:    Medium
// Criteria:         0/0
// Resource impact:  1/10 max
// Unverified:       4
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Numerics;

namespace Broiler.ECMAScript.Numbers;

/// <summary>
/// The specification's rounding to a Number (its <c>F</c> of a mathematical value) for an integer of any size: the nearest double, ties to even,
/// overflowing to +Infinity (ECMA-262 6.1.6.1, the Number type's rounding).
/// </summary>
/// <remarks>
/// A long integer is presented by its leading bits and whether anything below them is non-zero,
/// which is all round-to-nearest-even needs. Both engines rounded twice here before this library:
/// they accumulated digits in a double, so each digit beyond 2^53 rounded once and the result again.
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.6.1; IP=Low; Security=Medium; Resources=1; Fingerprint=7A378A
// Broiler-Human:        PENDING
internal static class ExactRounding
{
    /// <summary>
    /// Rounds the integer <c>leading x 2^shift + tail</c>, where <c>0 &lt;= tail &lt; 2^shift</c> and
    /// <paramref name="sticky"/> says whether the tail is non-zero.
    /// </summary>
    /// <param name="leading">The leading bits; zero only for the integer zero.</param>
    /// <param name="shift">The weight of the leading bits' last place; zero or more.</param>
    /// <param name="sticky">Whether any bit below the leading bits is set.</param>
    /// <returns>The nearest double, ties to even; +Infinity beyond the largest finite double's rounding interval.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.6.1; IP=Low; Security=Medium; Resources=1; Fingerprint=0B1151
    // Broiler-Human:        PENDING
    internal static double ToDouble(ulong leading, long shift, bool sticky)
    {
        if (leading == 0)
            return 0;

        // Normalise so the leading bits fill all 64 places.
        var zeros = BitOperations.LeadingZeroCount(leading);
        leading <<= zeros;
        shift -= zeros;

        // Keep 53 bits; the 54th decides, and everything below it is sticky.
        var kept = leading >> 11;
        var half = (leading >> 10) & 1;
        var below = (leading & 0x3FF) != 0 || sticky;
        if (half != 0 && (below || (kept & 1) != 0))
        {
            kept++;
            if (kept == 1UL << 53)
            {
                kept >>= 1;
                shift++;
            }
        }

        // The value is kept x 2^(shift + 11), with kept a 53-bit integer.
        var exponent = shift + 11 + 52;
        if (exponent > 1023)
            return double.PositiveInfinity;

        return BitConverter.UInt64BitsToDouble(((ulong)(exponent + 1023) << 52) | (kept & ((1UL << 52) - 1)));
    }

    /// <summary>Rounds a non-negative integer below 2^128.</summary>
    /// <param name="value">The integer.</param>
    /// <returns>The nearest double, ties to even; +0 for zero.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.6.1; IP=Low; Security=Medium; Resources=1; Fingerprint=96D8C0
    // Broiler-Human:        PENDING
    internal static double ToDouble(UInt128 value)
    {
        var high = (ulong)(value >> 64);
        if (high == 0)
            return (ulong)value <= TwoToThe53 ? (ulong)value : ToDouble((ulong)value, 0, false);

        var shift = 64 - BitOperations.LeadingZeroCount(high);
        var sticky = (value & ((UInt128.One << shift) - 1)) != 0;
        return ToDouble((ulong)(value >> shift), shift, sticky);
    }

    /// <summary>2^53: an integer up to it converts to a double exactly.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.6.1; IP=Low; Security=Medium; Resources=1; Fingerprint=428116
    // Broiler-Human:        PENDING
    private const ulong TwoToThe53 = 1UL << 53;

}
