// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   15
// Annotated:        15/15
// Exempt:           0
// Human-reviewed:   0/15
// IP risk:          Low
// Security risk:    Medium
// Criteria:         0/0
// Resource impact:  1/10 max
// Unverified:       15
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Numerics;

namespace Broiler.ECMAScript.Numbers;

/// <summary>
/// The specification's conversions of a Number to an integer: the modular conversions to the typed
/// widths (ECMA-262 7.1.6 to 7.1.11), the clamp (7.1.12), <c>ToIntegerOrInfinity</c> (7.1.5),
/// <c>ToLength</c> (7.1.20) and <c>ToIndex</c>'s range rule (7.1.22), and the BigInt bit patterns of
/// <c>ToBigInt64</c> and <c>ToBigUint64</c> (7.1.15 and 7.1.16).
/// </summary>
/// <remarks>
/// <para>
/// <b>Every input is a Number already.</b> The <c>ToNumber</c> or <c>ToBigInt</c> each clause begins
/// with is the engine's, because it can run guest code; these are the steps after it.
/// </para>
/// <para>
/// <b>The modulo is exact for every double.</b> A double of magnitude 2^63 or more is an integer
/// whose low bits are zero, so its residue is its significand shifted into the width, and a residue
/// is never computed through a rounding.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 7.1.5-7.1.22; IP=Low; Security=Medium; Resources=1; Fingerprint=753413
// Broiler-Human:        PENDING
public static class IntegerConversions
{
    /// <summary>The largest index <c>ToIndex</c> admits, and the largest length <c>ToLength</c> gives: 2^53 - 1.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 7.1.20; IP=Low; Security=Low; Resources=1; Fingerprint=17B9FB
    // Broiler-Human:        PENDING
    public const long MaximumSafeInteger = 9007199254740991;

    /// <summary><c>ToInt32</c> (ECMA-262 7.1.6) of a Number.</summary>
    /// <param name="number">The Number.</param>
    /// <returns>The integer congruent to its truncation modulo 2^32, in [-2^31, 2^31); 0 for NaN, the infinities and the zeros.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 7.1.6; IP=Low; Security=Medium; Resources=1; Fingerprint=51E8C9
    // Broiler-Human:        PENDING
    public static int ToInt32(double number) => unchecked((int)Modulo64(number));

    /// <summary><c>ToUint32</c> (ECMA-262 7.1.7) of a Number.</summary>
    /// <param name="number">The Number.</param>
    /// <returns>Its truncation modulo 2^32; 0 for NaN, the infinities and the zeros.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 7.1.7; IP=Low; Security=Medium; Resources=1; Fingerprint=74FA6E
    // Broiler-Human:        PENDING
    public static uint ToUint32(double number) => unchecked((uint)Modulo64(number));

    /// <summary><c>ToInt16</c> (ECMA-262 7.1.8) of a Number.</summary>
    /// <param name="number">The Number.</param>
    /// <returns>The integer congruent to its truncation modulo 2^16, in [-2^15, 2^15).</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 7.1.8; IP=Low; Security=Medium; Resources=1; Fingerprint=D2CF66
    // Broiler-Human:        PENDING
    public static short ToInt16(double number) => unchecked((short)Modulo64(number));

    /// <summary><c>ToUint16</c> (ECMA-262 7.1.9) of a Number.</summary>
    /// <param name="number">The Number.</param>
    /// <returns>Its truncation modulo 2^16.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 7.1.9; IP=Low; Security=Medium; Resources=1; Fingerprint=9252B8
    // Broiler-Human:        PENDING
    public static ushort ToUint16(double number) => unchecked((ushort)Modulo64(number));

    /// <summary><c>ToInt8</c> (ECMA-262 7.1.10) of a Number.</summary>
    /// <param name="number">The Number.</param>
    /// <returns>The integer congruent to its truncation modulo 2^8, in [-128, 128).</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 7.1.10; IP=Low; Security=Medium; Resources=1; Fingerprint=D2C214
    // Broiler-Human:        PENDING
    public static sbyte ToInt8(double number) => unchecked((sbyte)Modulo64(number));

    /// <summary><c>ToUint8</c> (ECMA-262 7.1.11) of a Number.</summary>
    /// <param name="number">The Number.</param>
    /// <returns>Its truncation modulo 2^8.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 7.1.11; IP=Low; Security=Medium; Resources=1; Fingerprint=48067F
    // Broiler-Human:        PENDING
    public static byte ToUint8(double number) => unchecked((byte)Modulo64(number));

    /// <summary><c>ToUint8Clamp</c> (ECMA-262 7.1.12) of a Number: clamped to [0, 255] and rounded to nearest, ties to even.</summary>
    /// <param name="number">The Number.</param>
    /// <returns>0 for NaN and for anything at or below 0, 255 at or above 255, and otherwise the nearest integer, the even one at a tie.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 7.1.12; IP=Low; Security=Medium; Resources=1; Fingerprint=1CC766
    // Broiler-Human:        PENDING
    public static byte ToUint8Clamp(double number)
    {
        if (double.IsNaN(number) || number <= 0)
            return 0;
        if (number >= 255)
            return 255;

        // Below 256 the floor and the difference are exact.
        var floor = Math.Floor(number);
        var fraction = number - floor;
        if (fraction < 0.5)
            return (byte)floor;
        if (fraction > 0.5)
            return (byte)(floor + 1);

        return (byte)(((byte)floor & 1) == 0 ? floor : floor + 1);
    }

    /// <summary><c>ToIntegerOrInfinity</c> (ECMA-262 7.1.5) of a Number.</summary>
    /// <param name="number">The Number.</param>
    /// <returns>+0 for NaN and the zeros, the infinity itself for an infinity, and otherwise the truncation, never -0.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 7.1.5; IP=Low; Security=Medium; Resources=1; Fingerprint=0EEBB9
    // Broiler-Human:        PENDING
    public static double ToIntegerOrInfinity(double number)
    {
        if (double.IsNaN(number))
            return 0;
        if (double.IsInfinity(number))
            return number;

        var truncated = Math.Truncate(number);
        return truncated == 0 ? 0 : truncated;
    }

    /// <summary><c>ToLength</c> (ECMA-262 7.1.20) of a Number: its <c>ToIntegerOrInfinity</c> clamped to [0, 2^53 - 1].</summary>
    /// <param name="number">The Number.</param>
    /// <returns>The length, from 0 to <see cref="MaximumSafeInteger"/>.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 7.1.20; IP=Low; Security=Medium; Resources=1; Fingerprint=CB787E
    // Broiler-Human:        PENDING
    public static long ToLength(double number)
    {
        var integer = ToIntegerOrInfinity(number);
        if (integer <= 0)
            return 0;

        return integer >= MaximumSafeInteger ? MaximumSafeInteger : (long)integer;
    }

    /// <summary><c>ToIndex</c>'s range rule (ECMA-262 7.1.22) for a Number: its <c>ToIntegerOrInfinity</c> when that is in [0, 2^53 - 1].</summary>
    /// <param name="number">The Number.</param>
    /// <param name="index">The index, when the Number is a valid one.</param>
    /// <returns><see langword="false"/> where the clause throws a <c>RangeError</c>, which the engine raises.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 7.1.22; IP=Low; Security=Medium; Resources=1; Fingerprint=D491E1
    // Broiler-Human:        PENDING
    public static bool TryToIndex(double number, out long index)
    {
        var integer = ToIntegerOrInfinity(number);
        if (integer < 0 || integer > MaximumSafeInteger)
        {
            index = 0;
            return false;
        }

        index = (long)integer;
        return true;
    }

    /// <summary>The bit pattern <c>ToBigInt64</c> (ECMA-262 7.1.15) gives a BigInt: its value modulo 2^64, read as a signed 64-bit integer.</summary>
    /// <param name="value">The BigInt's value.</param>
    /// <returns>The signed 64-bit integer congruent to it.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 7.1.15; IP=Low; Security=Medium; Resources=1; Fingerprint=1C8CBC
    // Broiler-Human:        PENDING
    public static long ToBigInt64(BigInteger value) => unchecked((long)ToBigUint64(value));

    /// <summary>The bit pattern <c>ToBigUint64</c> (ECMA-262 7.1.16) gives a BigInt: its value modulo 2^64.</summary>
    /// <param name="value">The BigInt's value.</param>
    /// <returns>The unsigned 64-bit integer congruent to it.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 7.1.16; IP=Low; Security=Medium; Resources=1; Fingerprint=44E1AE
    // Broiler-Human:        PENDING
    public static ulong ToBigUint64(BigInteger value) => (ulong)(value & ulong.MaxValue);

    /// <summary>A Number's truncation modulo 2^64, exactly; 0 for NaN and the infinities.</summary>
    /// <param name="number">The Number.</param>
    /// <returns>The residue, as the low 64 bits of the integer's two's complement.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 7.1.6; IP=Low; Security=Medium; Resources=1; Fingerprint=CC765B
    // Broiler-Human:        PENDING
    private static ulong Modulo64(double number)
    {
        if (!double.IsFinite(number))
            return 0;

        var truncated = Math.Truncate(number);
        if (Math.Abs(truncated) < 9223372036854775808.0)
            return unchecked((ulong)(long)truncated);

        // At 2^63 or more the double is significand x 2^shift with shift >= 11, so its residue is the
        // significand shifted, or zero once the shift passes the width.
        var bits = BitConverter.DoubleToUInt64Bits(truncated);
        var shift = (int)((bits >> 52) & 0x7FF) - 1075;
        if (shift >= 64)
            return 0;

        var residue = ((bits & ((1UL << 52) - 1)) | (1UL << 52)) << shift;
        return truncated < 0 ? unchecked(0 - residue) : residue;
    }
}
