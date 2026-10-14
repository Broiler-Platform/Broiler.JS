// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   9
// Annotated:        9/9
// Exempt:           0
// Human-reviewed:   0/9
// IP risk:          Low
// Security risk:    Medium
// Criteria:         0/0
// Resource impact:  1/10 max
// Unverified:       9
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Numerics;

namespace Broiler.ECMAScript.Numbers;

/// <summary>
/// The Math functions whose algorithm an engine would otherwise write itself (ECMA-262 21.3.2):
/// <c>Math.round</c>, <c>Number::exponentiate</c> with its special cases (6.1.6.1.3), <c>Math.hypot</c>,
/// <c>Math.cbrt</c>, <c>Math.expm1</c> and <c>Math.log1p</c>. <c>Math.sumPrecise</c>'s sum is
/// <see cref="PreciseSum"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every argument is a Number already</b>; the <c>ToNumber</c> of each argument, in order, is the
/// engine's. The functions the platform computes as specified - <c>fround</c>, <c>clz32</c>,
/// <c>imul</c>, <c>max</c>, <c>min</c>, <c>sign</c> and the trigonometric and logarithmic families -
/// are not here, since there is no algorithm to share (JSD-0067).
/// </para>
/// <para>
/// <b>The precision of the implementation-approximated functions is stated per function</b>, as
/// JSD-0067 decision 5 has it: <see cref="Cbrt"/> is correctly rounded; <see cref="Hypot"/> is
/// within one unit in the last place; <see cref="Expm1"/> and <see cref="Log1p"/> are within one unit
/// in the last place over their whole domain; and <see cref="Power"/>'s general case is the
/// platform's <c>Math.Pow</c>, after the clause's special cases.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 21.3.2; IP=Low; Security=Medium; Resources=1; Fingerprint=AD7233
// Broiler-Human:        PENDING
public static class MathKernels
{
    /// <summary><c>Math.round</c> (ECMA-262 21.3.2.28): the integral Number nearest the argument, a tie toward +Infinity.</summary>
    /// <param name="x">The Number.</param>
    /// <returns>The argument itself when it is not finite or already integral; -0 from -0.5 up to -0; +0 from above +0 below 0.5.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.3.2.28; IP=Low; Security=Medium; Resources=1; Fingerprint=C130E1
    // Broiler-Human:        PENDING
    public static double Round(double x)
    {
        if (!double.IsFinite(x) || Math.Truncate(x) == x)
            return x;
        if (x > 0 && x < 0.5)
            return 0;
        if (x < 0 && x >= -0.5)
            return -0.0;

        // Below 2^52 in magnitude, where every non-integral double lies, the floor and the difference are exact.
        var floor = Math.Floor(x);
        return x - floor >= 0.5 ? floor + 1 : floor;
    }

    /// <summary>
    /// <c>Number::exponentiate</c> (ECMA-262 6.1.6.1.3), the operation of <c>**</c> and
    /// <c>Math.pow</c> (21.3.2.26): the clause's special cases exactly, and otherwise the platform's
    /// <c>Math.Pow</c>, the implementation's approximation the clause's last step admits.
    /// </summary>
    /// <param name="base">The base.</param>
    /// <param name="exponent">The exponent.</param>
    /// <returns>The power. <c>1 ** NaN</c> and <c>(-1) ** ±Infinity</c> are NaN, as the clause says, where IEEE 754's <c>pow</c> answers 1.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.6.1.3; IP=Low; Security=Medium; Resources=1; Fingerprint=8E6A93
    // Broiler-Human:        PENDING
    public static double Power(double @base, double exponent)
    {
        if (double.IsNaN(exponent))
            return double.NaN;
        if (exponent == 0)
            return 1;
        if (double.IsNaN(@base))
            return double.NaN;

        if (double.IsInfinity(exponent))
        {
            var magnitude = Math.Abs(@base);
            if (magnitude == 1)
                return double.NaN;

            return (magnitude > 1) == (exponent > 0) ? double.PositiveInfinity : 0;
        }

        // Every remaining case - the zeros and infinities of the base, a negative base with a
        // fractional exponent, and the finite powers - IEEE 754-2019's pow (9.2.1) answers as the
        // clause does, and the platform's Math.Pow is that pow.
        return Math.Pow(@base, exponent);
    }

    /// <summary>
    /// <c>Math.hypot</c> (ECMA-262 21.3.2.18) of Numbers already converted: the square root of the
    /// sum of their squares, within one unit in the last place, without overflow or underflow in
    /// between.
    /// </summary>
    /// <param name="values">The arguments, after <c>ToNumber</c>.</param>
    /// <returns>+Infinity if any is infinite, else NaN if any is NaN, else +0 if every one is a zero, else the hypotenuse.</returns>
    /// <remarks>
    /// The terms are scaled by a power of two to the largest one's binade, squared exactly with a
    /// fused multiply-add, and summed in double-double; the root is corrected once from the residual
    /// and scaled back. The result is within one unit in the last place of the exact value, and is the
    /// exact value whenever that is a double.
    /// </remarks>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.3.2.18; IP=Low; Security=Medium; Resources=1; Fingerprint=F72AA3
    // Broiler-Human:        PENDING
    public static double Hypot(ReadOnlySpan<double> values)
    {
        var largest = 0.0;
        var nan = false;
        foreach (var value in values)
        {
            if (double.IsInfinity(value))
                return double.PositiveInfinity;
            if (double.IsNaN(value))
                nan = true;
            else
                largest = Math.Max(largest, Math.Abs(value));
        }

        if (nan)
            return double.NaN;
        if (largest == 0)
            return 0;

        var scale = Math.ILogB(largest);
        var high = 0.0;
        var low = 0.0;
        foreach (var value in values)
        {
            var scaled = Math.ScaleB(Math.Abs(value), -scale);
            var square = scaled * scaled;
            var squareError = Math.FusedMultiplyAdd(scaled, scaled, -square);
            var sum = high + square;
            var sumError = (high - (sum - (sum - high))) + (square - (sum - high));
            high = sum;
            low += sumError + squareError;
        }

        var total = high + low;
        var totalLow = low - (total - high);
        var root = Math.Sqrt(total);
        var residual = -Math.FusedMultiplyAdd(root, root, -total) + totalLow;
        root += residual / (2 * root);
        return Math.ScaleB(root, scale);
    }

    /// <summary><c>Math.cbrt</c> (ECMA-262 21.3.2.9): the cube root, correctly rounded.</summary>
    /// <param name="x">The Number.</param>
    /// <returns>The argument itself when it is NaN, an infinity or a zero; otherwise the double nearest its real cube root.</returns>
    /// <remarks>
    /// The platform's root is corrected by comparing the cubes of the midpoints beside it with the
    /// argument exactly. No midpoint between doubles is the cube root of a double, so there is never a
    /// tie to decide.
    /// </remarks>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.3.2.9; IP=Low; Security=Medium; Resources=1; Fingerprint=0103F3
    // Broiler-Human:        PENDING
    public static double Cbrt(double x)
    {
        if (!double.IsFinite(x) || x == 0)
            return x;

        var magnitude = Math.Abs(x);
        var root = Math.Cbrt(magnitude);
        var target = Exact(magnitude, out var targetExponent);

        while (true)
        {
            var above = Math.BitIncrement(root);
            if (CompareCube(root, above, target, targetExponent) < 0)
            {
                root = above;
                continue;
            }

            var below = Math.BitDecrement(root);
            if (CompareCube(below, root, target, targetExponent) > 0)
            {
                root = below;
                continue;
            }

            return x < 0 ? -root : root;
        }
    }

    /// <summary><c>Math.expm1</c> (ECMA-262 21.3.2.15): <c>e^x - 1</c>, accurate near zero, within one unit in the last place.</summary>
    /// <param name="x">The Number.</param>
    /// <returns>The argument itself when it is NaN, a zero or +Infinity; -1 for -Infinity; otherwise the approximation.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.3.2.15; IP=Low; Security=Medium; Resources=1; Fingerprint=252BCD
    // Broiler-Human:        PENDING
    public static double Expm1(double x) => Fdlibm.Expm1(x);

    /// <summary><c>Math.log1p</c> (ECMA-262 21.3.2.21): <c>ln(1 + x)</c>, accurate near zero, within one unit in the last place.</summary>
    /// <param name="x">The Number.</param>
    /// <returns>The argument itself when it is NaN, a zero or +Infinity; -Infinity for -1; NaN below -1; otherwise the approximation.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.3.2.21; IP=Low; Security=Medium; Resources=1; Fingerprint=57F9BF
    // Broiler-Human:        PENDING
    public static double Log1p(double x) => Fdlibm.Log1p(x);

    /// <summary>A positive finite double as its exact integer significand and the exponent of its last place.</summary>
    /// <param name="value">The double.</param>
    /// <param name="exponent">The exponent of the significand's last place.</param>
    /// <returns>The significand, the hidden bit included.</returns>
    // Broiler-AI:           Origin=AI; Spec=IEEE 754-2019 3.4; IP=Low; Security=Low; Resources=1; Fingerprint=4EF180
    // Broiler-Human:        PENDING
    private static BigInteger Exact(double value, out int exponent)
    {
        var bits = DoubleBits.Of(value);
        exponent = bits.Exponent;
        return bits.Significand;
    }

    /// <summary>
    /// The sign of the cube of the midpoint between adjacent positive doubles <paramref name="lower"/>
    /// and <paramref name="upper"/>, less the target <c>significand x 2^exponent</c>, computed exactly.
    /// </summary>
    /// <param name="lower">The lower double.</param>
    /// <param name="upper">The next double above it.</param>
    /// <param name="significand">The target's significand.</param>
    /// <param name="exponent">The target's exponent.</param>
    /// <returns>Negative when the midpoint's cube is below the target, positive when above.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.3.2.9; IP=Low; Security=Medium; Resources=1; Fingerprint=24619F
    // Broiler-Human:        PENDING
    private static int CompareCube(double lower, double upper, BigInteger significand, int exponent)
    {
        var a = Exact(lower, out var aExponent);
        var b = Exact(upper, out var bExponent);
        var common = Math.Min(aExponent, bExponent);
        var midpoint = (a << (aExponent - common)) + (b << (bExponent - common));

        // The midpoint is midpoint x 2^(common - 1), and its cube midpoint^3 x 2^(3 common - 3).
        var cube = midpoint * midpoint * midpoint;
        var shift = 3 * (common - 1) - exponent;
        return shift >= 0
            ? (cube << shift).CompareTo(significand)
            : cube.CompareTo(significand << -shift);
    }
}
