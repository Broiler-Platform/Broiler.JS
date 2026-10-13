// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   12
// Annotated:        12/12
// Exempt:           0
// Human-reviewed:   0/12
// IP risk:          Low
// Security risk:    Medium
// Criteria:         0/0
// Resource impact:  2/10 max
// Unverified:       12
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Numerics;

namespace Broiler.ECMAScript.Numbers;

/// <summary>
/// Rounding over exact values: the Number type's rounding of a mathematical value to the nearest
/// double (ECMA-262 6.1.6.1), and the rounding modes and increment roundings Temporal and ECMA-402
/// apply to exact quantities (Temporal 13.21 to 13.27, <c>RoundNumberToIncrement</c> and its
/// relatives; ECMA-402 15.5.11 to 15.5.14).
/// </summary>
/// <remarks>
/// <para>
/// <b>A value is a ratio of two integers and is never approximated.</b> A quantity Temporal rounds
/// is a count of nanoseconds over a unit's length, and one ECMA-402 rounds is a decimal; both are
/// exact rationals, and the modes decide their ties exactly.
/// </para>
/// <para>
/// The number area's internal members round a long integer to a double once; both engines rounded
/// twice there before this library, accumulating digits in a double.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.6.1; IP=Low; Security=Medium; Resources=1; Fingerprint=4D0AD3
// Broiler-Human:        PENDING
public static class ExactRounding
{
    /// <summary>
    /// Reads a rounding mode's option name (Temporal, <c>GetRoundingModeOption</c>, 13.8; ECMA-402
    /// 15.1.1): one of the nine names of <see cref="RoundingMode"/>, compared exactly.
    /// </summary>
    /// <param name="name">The option's string, after the engine's <c>ToString</c>.</param>
    /// <param name="mode">The mode, when the name is one.</param>
    /// <returns><see langword="false"/> for any other string, where the clause throws a <c>RangeError</c>, which the engine raises.</returns>
    // Broiler-AI:           Origin=AI; Spec=Temporal 13.8; IP=Low; Security=Low; Resources=1; Fingerprint=879DA8
    // Broiler-Human:        PENDING
    public static bool TryParseRoundingMode(ReadOnlySpan<char> name, out RoundingMode mode)
    {
        mode = name switch
        {
            "ceil" => RoundingMode.Ceil,
            "floor" => RoundingMode.Floor,
            "expand" => RoundingMode.Expand,
            "trunc" => RoundingMode.Trunc,
            "halfCeil" => RoundingMode.HalfCeil,
            "halfFloor" => RoundingMode.HalfFloor,
            "halfExpand" => RoundingMode.HalfExpand,
            "halfTrunc" => RoundingMode.HalfTrunc,
            "halfEven" => RoundingMode.HalfEven,
            _ => (RoundingMode)(-1),
        };
        return mode >= 0;
    }

    /// <summary><c>NegateRoundingMode</c> (Temporal 13.10): the mode that rounds a negated quantity as this mode rounds the quantity, which <c>since</c> applies.</summary>
    /// <param name="mode">The mode.</param>
    /// <returns><c>Ceil</c> and <c>Floor</c> exchanged, <c>HalfCeil</c> and <c>HalfFloor</c> exchanged, every other mode itself.</returns>
    // Broiler-AI:           Origin=AI; Spec=Temporal 13.10; IP=Low; Security=Low; Resources=1; Fingerprint=7FFF4E
    // Broiler-Human:        PENDING
    public static RoundingMode Negate(RoundingMode mode) => mode switch
    {
        RoundingMode.Ceil => RoundingMode.Floor,
        RoundingMode.Floor => RoundingMode.Ceil,
        RoundingMode.HalfCeil => RoundingMode.HalfFloor,
        RoundingMode.HalfFloor => RoundingMode.HalfCeil,
        _ => mode,
    };

    /// <summary><c>GetUnsignedRoundingMode</c> (Temporal 13.25, Table 23; ECMA-402 15.5.13): the unsigned mode a signed one is for a quantity of the given sign.</summary>
    /// <param name="mode">The mode.</param>
    /// <param name="negative">Whether the quantity is negative.</param>
    /// <returns>The unsigned mode to apply to the quantity's magnitude.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The mode is not one of the nine.</exception>
    // Broiler-AI:           Origin=AI; Spec=Temporal 13.25; IP=Low; Security=Medium; Resources=1; Fingerprint=0A0574
    // Broiler-Human:        PENDING
    public static UnsignedRoundingMode GetUnsignedRoundingMode(RoundingMode mode, bool negative) => mode switch
    {
        RoundingMode.Ceil => negative ? UnsignedRoundingMode.Zero : UnsignedRoundingMode.Infinity,
        RoundingMode.Floor => negative ? UnsignedRoundingMode.Infinity : UnsignedRoundingMode.Zero,
        RoundingMode.Expand => UnsignedRoundingMode.Infinity,
        RoundingMode.Trunc => UnsignedRoundingMode.Zero,
        RoundingMode.HalfCeil => negative ? UnsignedRoundingMode.HalfZero : UnsignedRoundingMode.HalfInfinity,
        RoundingMode.HalfFloor => negative ? UnsignedRoundingMode.HalfInfinity : UnsignedRoundingMode.HalfZero,
        RoundingMode.HalfExpand => UnsignedRoundingMode.HalfInfinity,
        RoundingMode.HalfTrunc => UnsignedRoundingMode.HalfZero,
        RoundingMode.HalfEven => UnsignedRoundingMode.HalfEven,
        _ => throw new ArgumentOutOfRangeException(nameof(mode)),
    };

    /// <summary>
    /// <c>ApplyUnsignedRoundingMode</c> (Temporal 13.26; ECMA-402 15.5.14): the end of the window
    /// [<paramref name="lower"/>, <paramref name="upper"/>] the value
    /// <paramref name="numerator"/> / <paramref name="denominator"/> rounds to.
    /// </summary>
    /// <param name="numerator">The value's numerator.</param>
    /// <param name="denominator">The value's denominator; positive.</param>
    /// <param name="lower">The lower end, <c>r1</c>; at most the value.</param>
    /// <param name="upper">The upper end, <c>r2</c>; above <paramref name="lower"/> and at least the value. For <see cref="UnsignedRoundingMode.HalfEven"/>, <paramref name="lower"/> is a multiple of the window's width.</param>
    /// <param name="mode">The unsigned mode.</param>
    /// <returns><paramref name="lower"/> or <paramref name="upper"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A precondition does not hold.</exception>
    // Broiler-AI:           Origin=AI; Spec=Temporal 13.26; IP=Low; Security=Medium; Resources=1; Fingerprint=D8ECA1
    // Broiler-Human:        PENDING
    public static BigInteger ApplyUnsignedRoundingMode(
        BigInteger numerator, BigInteger denominator, BigInteger lower, BigInteger upper, UnsignedRoundingMode mode)
    {
        if (denominator.Sign <= 0)
            throw new ArgumentOutOfRangeException(nameof(denominator), "the denominator is not positive");
        if (lower >= upper)
            throw new ArgumentOutOfRangeException(nameof(upper), "the window is empty");

        // d1 and d2 are the distances to the ends, scaled by the denominator.
        var below = numerator - lower * denominator;
        var above = upper * denominator - numerator;
        if (below.Sign < 0 || above.Sign < 0)
            throw new ArgumentOutOfRangeException(nameof(numerator), "the value is outside the window");

        if (below.IsZero)
            return lower;

        switch (mode)
        {
            case UnsignedRoundingMode.Zero:
                return lower;
            case UnsignedRoundingMode.Infinity:
                return upper;
        }

        var order = below.CompareTo(above);
        if (order < 0)
            return lower;
        if (order > 0)
            return upper;

        switch (mode)
        {
            case UnsignedRoundingMode.HalfZero:
                return lower;
            case UnsignedRoundingMode.HalfInfinity:
                return upper;
            case UnsignedRoundingMode.HalfEven:
                var cardinality = BigInteger.DivRem(lower, upper - lower, out var rest);
                if (!rest.IsZero)
                    throw new ArgumentOutOfRangeException(nameof(lower), "the lower end is not a multiple of the window's width");

                return cardinality.IsEven ? lower : upper;
            default:
                throw new ArgumentOutOfRangeException(nameof(mode));
        }
    }

    /// <summary>
    /// <c>RoundNumberToIncrement</c> (Temporal 13.27): the value <paramref name="numerator"/> /
    /// <paramref name="denominator"/> rounded to a multiple of <paramref name="increment"/> under
    /// <paramref name="mode"/>, the mode applied to the value's magnitude by its sign.
    /// </summary>
    /// <param name="numerator">The value's numerator.</param>
    /// <param name="denominator">The value's denominator; positive.</param>
    /// <param name="increment">The increment; positive.</param>
    /// <param name="mode">The mode.</param>
    /// <returns>The rounded value, a multiple of the increment.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The denominator or the increment is not positive.</exception>
    // Broiler-AI:           Origin=AI; Spec=Temporal 13.27; IP=Low; Security=Medium; Resources=1; Fingerprint=F5CF54
    // Broiler-Human:        PENDING
    public static BigInteger RoundNumberToIncrement(BigInteger numerator, BigInteger denominator, BigInteger increment, RoundingMode mode)
    {
        if (increment.Sign <= 0)
            throw new ArgumentOutOfRangeException(nameof(increment), "the increment is not positive");
        if (denominator.Sign <= 0)
            throw new ArgumentOutOfRangeException(nameof(denominator), "the denominator is not positive");

        // The quotient is |numerator| / (denominator x increment), and its sign decides the unsigned mode.
        var negative = numerator.Sign < 0;
        var magnitude = BigInteger.Abs(numerator);
        var scale = denominator * increment;
        var lower = BigInteger.Divide(magnitude, scale);
        var rounded = ApplyUnsignedRoundingMode(magnitude, scale, lower, lower + 1, GetUnsignedRoundingMode(mode, negative));
        return (negative ? -rounded : rounded) * increment;
    }

    /// <summary>
    /// <c>RoundNumberToIncrementAsIfPositive</c> (Temporal 13.28): as <see cref="RoundNumberToIncrement"/>,
    /// but every value is rounded as a positive one is, so <c>Floor</c> goes toward -Infinity at
    /// either sign. An instant rounds so.
    /// </summary>
    /// <param name="numerator">The value's numerator.</param>
    /// <param name="denominator">The value's denominator; positive.</param>
    /// <param name="increment">The increment; positive.</param>
    /// <param name="mode">The mode.</param>
    /// <returns>The rounded value, a multiple of the increment.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The denominator or the increment is not positive.</exception>
    // Broiler-AI:           Origin=AI; Spec=Temporal 13.28; IP=Low; Security=Medium; Resources=1; Fingerprint=F80327
    // Broiler-Human:        PENDING
    public static BigInteger RoundNumberToIncrementAsIfPositive(BigInteger numerator, BigInteger denominator, BigInteger increment, RoundingMode mode)
    {
        if (increment.Sign <= 0)
            throw new ArgumentOutOfRangeException(nameof(increment), "the increment is not positive");
        if (denominator.Sign <= 0)
            throw new ArgumentOutOfRangeException(nameof(denominator), "the denominator is not positive");

        var scale = denominator * increment;
        var lower = FloorDivide(numerator, scale);
        var rounded = ApplyUnsignedRoundingMode(numerator, scale, lower, lower + 1, GetUnsignedRoundingMode(mode, negative: false));
        return rounded * increment;
    }

    /// <summary>
    /// The Number nearest the exact value <paramref name="numerator"/> / <paramref name="denominator"/>:
    /// ECMA-262's rounding of a mathematical value (6.1.6.1), to nearest with ties to even over the
    /// whole range, subnormals included, and to an infinity at or past the largest finite double's
    /// rounding range. Temporal's <c>DivideTimeDuration</c> (7.5.25) is this conversion.
    /// </summary>
    /// <param name="numerator">The numerator.</param>
    /// <param name="denominator">The denominator; not zero.</param>
    /// <returns>The nearest double, signed as the quotient; +0 for a zero numerator.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The denominator is zero.</exception>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.6.1; IP=Low; Security=Medium; Resources=2; Fingerprint=107719
    // Broiler-Human:        PENDING
    public static double ToDouble(BigInteger numerator, BigInteger denominator)
    {
        if (denominator.IsZero)
            throw new ArgumentOutOfRangeException(nameof(denominator), "the denominator is zero");
        if (numerator.IsZero)
            return 0;

        var negative = numerator.Sign != denominator.Sign;
        var top = BigInteger.Abs(numerator);
        var bottom = BigInteger.Abs(denominator);

        // 2^exponent <= top / bottom < 2^(exponent + 1), exactly.
        var exponent = top.GetBitLength() - bottom.GetBitLength();
        if ((exponent >= 0 ? top : top << (int)-exponent) < (exponent >= 0 ? bottom << (int)exponent : bottom))
            exponent--;

        if (exponent > 1024)
            return negative ? double.NegativeInfinity : double.PositiveInfinity;

        // The quotient in units of the result's last place: 53 bits for a normal result, fewer for a subnormal one.
        var quantum = Math.Max(exponent - 52, -1074);
        BigInteger units, remainder, scale;
        if (quantum >= 0)
        {
            scale = bottom << (int)quantum;
            units = BigInteger.DivRem(top, scale, out remainder);
        }
        else
        {
            scale = bottom;
            units = BigInteger.DivRem(top << (int)-quantum, bottom, out remainder);
        }

        var twice = remainder << 1;
        var order = twice.CompareTo(scale);
        if (order > 0 || (order == 0 && !units.IsEven))
            units++;

        var significand = (ulong)units;
        if (significand == 1UL << 53)
        {
            significand >>= 1;
            quantum++;
        }

        ulong bits;
        if (significand < 1UL << 52)
        {
            bits = significand;
        }
        else
        {
            var field = quantum + 1075;
            bits = field >= 0x7FF ? 0x7FF0000000000000UL : ((ulong)field << 52) | (significand - (1UL << 52));
        }

        return BitConverter.UInt64BitsToDouble(negative ? bits | (1UL << 63) : bits);
    }

    /// <summary>The floor of <paramref name="numerator"/> / <paramref name="denominator"/> for a positive denominator.</summary>
    /// <param name="numerator">The numerator.</param>
    /// <param name="denominator">The denominator; positive.</param>
    /// <returns>The largest integer at most the quotient.</returns>
    // Broiler-AI:           Origin=AI; Spec=Temporal 13.28; IP=Low; Security=Low; Resources=1; Fingerprint=9DD5CC
    // Broiler-Human:        PENDING
    private static BigInteger FloorDivide(BigInteger numerator, BigInteger denominator)
    {
        var quotient = BigInteger.DivRem(numerator, denominator, out var remainder);
        return remainder.Sign < 0 ? quotient - 1 : quotient;
    }

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
