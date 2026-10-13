// REFERENCE ARM V: a test-only copy of Broiler.VM's implementation before the cut-over, for the
// reference-arm comparison of bundle sjl-4-001 (docs/evidence/sjl-4-001/reference-arms.md). It is
// never referenced outside this test project, and it is retired at SJL-16.
//
// Copied from Broiler.VM at a04b2e5 (SJL-3's merge, bundle sjl-4-001's base):
//   src/Broiler.VM.Profile.JavaScript/JsTemporalCore.cs
//     blob 643db492de1101572c3a10c103d3acf060182f4b: the enum TemporalRounding; of the static class JsTemporalCore, the section from "---- Rounding" to ToDouble(BigInteger): Negate, the nested enum Unsigned, UnsignedMode, ApplyUnsigned, both RoundToIncrement, RoundToIncrementAsIfPositive, RoundsToSecond and both ToDouble
//
// Changes from the source, none of which changes a result or a charge:
//   - the namespace; the static class JsTemporalCore holds only the rounding section, its other members (and the types TemporalUnit, JsIsoDate, JsTimeRecord, JsIsoDateTime, JsDateDuration and JsInternalDuration) being the engine's; the copied members call no other member;
//   - the generated assurance header and the per-member assurance annotations (Broiler-AI / Broiler-Human / Broiler-Falsified-If lines) removed;
//   - the SPDX lines above the assurance header removed;
//   - the nested enum Unsigned, UnsignedMode and ApplyUnsigned made internal (private in the source) so that the comparison can call them.

using BigInteger = System.Numerics.BigInteger;

namespace Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.Numerics.V;

/// <summary>The rounding modes of the proposal's Table 28.</summary>
internal enum TemporalRounding
{
    Ceil,
    Floor,
    Expand,
    Trunc,
    HalfCeil,
    HalfFloor,
    HalfExpand,
    HalfTrunc,
    HalfEven,
}

/// <summary>
/// The ISO 8601 arithmetic Temporal is built on (phase F8, JSD-0054): dates and times as records,
/// epoch days and nanoseconds, the representable limits, exact rounding to an increment, and the
/// duration operations of the proposal's sections 3, 4, 5, 7 and 13.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every quantity the proposal calls a mathematical value is exact here.</b> A time duration and an
/// epoch-nanoseconds value are <see cref="BigInteger"/>s, a fraction is a numerator and a
/// denominator, and a Number is made from either only where the proposal applies 𝔽, correctly
/// rounded to nearest, ties to even. A duration's ten fields are doubles, because the proposal
/// stores them as float64-representable integers.
/// </para>
/// </remarks>
internal static class JsTemporalCore
{
    // ---- Rounding ---------------------------------------------------------------------------

    /// <summary>The proposal's NegateRoundingMode (s13.8).</summary>
    internal static TemporalRounding Negate(TemporalRounding mode) => mode switch
    {
        TemporalRounding.Ceil => TemporalRounding.Floor,
        TemporalRounding.Floor => TemporalRounding.Ceil,
        TemporalRounding.HalfCeil => TemporalRounding.HalfFloor,
        TemporalRounding.HalfFloor => TemporalRounding.HalfCeil,
        _ => mode,
    };

    internal enum Unsigned
    {
        Zero,
        Infinity,
        HalfZero,
        HalfInfinity,
        HalfEven,
    }

    /// <summary>The proposal's GetUnsignedRoundingMode (s13.27, Table 22).</summary>
    internal static Unsigned UnsignedMode(TemporalRounding mode, bool negative) => mode switch
    {
        TemporalRounding.Ceil => negative ? Unsigned.Zero : Unsigned.Infinity,
        TemporalRounding.Floor => negative ? Unsigned.Infinity : Unsigned.Zero,
        TemporalRounding.Expand => Unsigned.Infinity,
        TemporalRounding.Trunc => Unsigned.Zero,
        TemporalRounding.HalfCeil => negative ? Unsigned.HalfZero : Unsigned.HalfInfinity,
        TemporalRounding.HalfFloor => negative ? Unsigned.HalfInfinity : Unsigned.HalfZero,
        TemporalRounding.HalfExpand => Unsigned.HalfInfinity,
        TemporalRounding.HalfTrunc => Unsigned.HalfZero,
        _ => Unsigned.HalfEven,
    };

    /// <summary>
    /// The proposal's ApplyUnsignedRoundingMode (s13.28) for a non-negative fraction
    /// <paramref name="numerator"/> / <paramref name="denominator"/> between the integers
    /// <paramref name="r1"/> and <paramref name="r1"/> + 1: the integer it rounds to.
    /// </summary>
    internal static BigInteger ApplyUnsigned(BigInteger numerator, BigInteger denominator, BigInteger r1, Unsigned mode)
    {
        var remainder = numerator - (r1 * denominator);

        if (remainder.IsZero)
        {
            return r1;
        }

        var r2 = r1 + 1;

        switch (mode)
        {
            case Unsigned.Zero:
                return r1;
            case Unsigned.Infinity:
                return r2;
        }

        var twice = remainder * 2;

        if (twice < denominator)
        {
            return r1;
        }

        if (twice > denominator)
        {
            return r2;
        }

        return mode switch
        {
            Unsigned.HalfZero => r1,
            Unsigned.HalfInfinity => r2,
            _ => r1.IsEven ? r1 : r2,
        };
    }

    /// <summary>
    /// The proposal's RoundNumberToIncrement (s13.29) of a fraction <paramref name="numerator"/> /
    /// <paramref name="denominator"/>, the denominator positive: a multiple of <paramref name="increment"/>.
    /// </summary>
    internal static BigInteger RoundToIncrement(BigInteger numerator, BigInteger denominator, BigInteger increment, TemporalRounding mode)
    {
        var negative = numerator.Sign < 0;
        var magnitude = BigInteger.Abs(numerator);
        var scaled = denominator * increment;
        var r1 = BigInteger.Divide(magnitude, scaled);
        var rounded = ApplyUnsigned(magnitude, scaled, r1, UnsignedMode(mode, negative));
        return (negative ? -rounded : rounded) * increment;
    }

    /// <summary>The proposal's RoundNumberToIncrement (s13.29) of an integer.</summary>
    internal static BigInteger RoundToIncrement(BigInteger x, BigInteger increment, TemporalRounding mode) =>
        RoundToIncrement(x, BigInteger.One, increment, mode);

    /// <summary>The proposal's RoundNumberToIncrementAsIfPositive (s13.30) of an integer.</summary>
    internal static BigInteger RoundToIncrementAsIfPositive(BigInteger x, BigInteger increment, TemporalRounding mode)
    {
        var r1 = BigInteger.Divide(x, increment);

        if (x.Sign < 0 && !(r1 * increment).Equals(x))
        {
            r1 -= 1;
        }

        // THE FLOOR ABOVE MAKES x - r1 × increment NON-NEGATIVE, so the positive table applies, and
        // halfEven's tie goes to the even one of r1 and r1 + 1 themselves.
        return ApplyUnsigned(x, increment, r1, UnsignedMode(mode, negative: false)) * increment;
    }

    /// <summary>
    /// The unsigned rounding mode's choice between <paramref name="r1"/> and <paramref name="r2"/> for
    /// a value whose absolute value is the fraction given, which lies between them (s13.28, as
    /// NudgeToCalendarUnit applies it).
    /// </summary>
    internal static bool RoundsToSecond(
        BigInteger numerator, BigInteger denominator, BigInteger r1, BigInteger r2, TemporalRounding mode, bool negative)
    {
        // THE SPACING IS ONE INCREMENT, so the fraction (value - r1) / (r2 - r1) decides.
        var span = r2 - r1;
        var offset = numerator - (r1 * denominator);
        var unsignedMode = UnsignedMode(mode, negative);

        if (offset.IsZero)
        {
            return false;
        }

        switch (unsignedMode)
        {
            case Unsigned.Zero:
                return false;
            case Unsigned.Infinity:
                return true;
        }

        var twice = offset * 2;
        var whole = span * denominator;

        if (twice < whole)
        {
            return false;
        }

        if (twice > whole)
        {
            return true;
        }

        return unsignedMode switch
        {
            Unsigned.HalfZero => false,
            Unsigned.HalfInfinity => true,
            _ => !(BigInteger.Divide(r1, span).IsEven),
        };
    }

    // ---- Numbers ----------------------------------------------------------------------------

    /// <summary>𝔽 of a fraction: the nearest double, ties to even.</summary>
    internal static double ToDouble(BigInteger numerator, BigInteger denominator)
    {
        if (denominator.Sign < 0)
        {
            numerator = -numerator;
            denominator = -denominator;
        }

        if (numerator.IsZero)
        {
            return 0;
        }

        var negative = numerator.Sign < 0;
        var n = BigInteger.Abs(numerator);

        // SCALE SO THE QUOTIENT HAS 55 OR 56 BITS, then round to 53 with the remainder as sticky.
        var shift = 55 - (int)(n.GetBitLength() - denominator.GetBitLength());
        var quotient = shift >= 0
            ? BigInteger.DivRem(n << shift, denominator, out var remainder)
            : BigInteger.DivRem(n, denominator << -shift, out remainder);

        var extra = (int)quotient.GetBitLength() - 53;
        var mantissa = quotient >> extra;
        var dropped = quotient - (mantissa << extra);
        var half = BigInteger.One << (extra - 1);

        if (dropped > half || (dropped == half && (!remainder.IsZero || !mantissa.IsEven)))
        {
            mantissa += 1;
        }

        var result = System.Math.ScaleB((double)mantissa, extra - shift);
        return negative ? -result : result;
    }

    /// <summary>𝔽 of an integer: the nearest double, ties to even.</summary>
    internal static double ToDouble(BigInteger value) => ToDouble(value, BigInteger.One);
}
