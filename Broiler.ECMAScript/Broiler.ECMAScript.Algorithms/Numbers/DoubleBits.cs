// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   3
// Annotated:        3/3
// Exempt:           0
// Human-reviewed:   0/3
// IP risk:          Low
// Security risk:    Low
// Criteria:         0/0
// Resource impact:  1/10 max
// Unverified:       3
//
// GENERATED - DO NOT EDIT MANUALLY

using System;

namespace Broiler.ECMAScript.Numbers;

/// <summary>
/// A positive finite double read as its exact value <c>Significand x 2^Exponent</c>, the form every
/// exact algorithm of the area starts from (ECMA-262 6.1.6.1, the Number type's values).
/// </summary>
/// <param name="Significand">The integer significand, the hidden bit included for a normal double; below 2^53.</param>
/// <param name="Exponent">The binary exponent of the significand's last place, -1074 for a subnormal.</param>
/// <param name="IsEven">Whether the significand is even, which is when the ends of the double's rounding interval round to it.</param>
/// <param name="LowerGapIsHalf">Whether the gap to the next smaller double is half the gap to the next larger, at a power of two above the subnormals.</param>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.6.1; IP=Low; Security=Low; Resources=1; Fingerprint=4802F0
// Broiler-Human:        PENDING
internal readonly record struct DoubleBits(ulong Significand, int Exponent, bool IsEven, bool LowerGapIsHalf)
{
    /// <summary>The bits of the significand field.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.6.1; IP=Low; Security=Low; Resources=1; Fingerprint=81D371
    // Broiler-Human:        PENDING
    internal const int MantissaBits = 52;

    /// <summary>Decomposes a positive finite double.</summary>
    /// <param name="value">A double greater than zero and finite.</param>
    /// <returns>Its exact significand and exponent.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.6.1; IP=Low; Security=Low; Resources=1; Fingerprint=03C2B3
    // Broiler-Human:        PENDING
    internal static DoubleBits Of(double value)
    {
        var bits = BitConverter.DoubleToUInt64Bits(value);
        var field = (int)((bits >> MantissaBits) & 0x7FF);
        var fraction = bits & ((1UL << MantissaBits) - 1);
        return field == 0
            ? new DoubleBits(fraction, -1074, (fraction & 1) == 0, false)
            : new DoubleBits(fraction | (1UL << MantissaBits), field - 1075, (fraction & 1) == 0, fraction == 0 && field > 1);
    }
}
