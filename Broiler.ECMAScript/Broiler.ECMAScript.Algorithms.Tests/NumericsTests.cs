using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using Broiler.ECMAScript.Numbers;
using Xunit;

namespace Broiler.ECMAScript.Algorithms.Tests;

/// <summary>
/// The numerics area (milestone SJL-4.3), each member against an independent computation: exact
/// integer arithmetic over <see cref="BigInteger"/>, the platform's binary16 conversion, CPython's
/// reference values written in, and <c>double.Parse</c>'s correctly rounded reading of an exact
/// decimal. The area's vectors are its own tests; these add populations the vectors do not hold.
/// </summary>
public class NumericsTests
{
    private static IEnumerable<double> Doubles(ulong seed, int count)
    {
        var random = new SplitMix64(seed);
        for (var i = 0; i < count; i++)
            yield return BitConverter.UInt64BitsToDouble(random.Next());
    }

    private static BigInteger Truncated(double value) => new(Math.Truncate(value));

    [Fact]
    public void The_modular_conversions_are_exact_residues()
    {
        foreach (var x in Doubles(0x534A4C34_4E554D31, 200_000))
        {
            if (!double.IsFinite(x))
            {
                Assert.Equal(0, IntegerConversions.ToInt32(x));
                continue;
            }

            var residue = (ulong)(Truncated(x) & ulong.MaxValue);
            Assert.Equal(unchecked((int)residue), IntegerConversions.ToInt32(x));
            Assert.Equal(unchecked((uint)residue), IntegerConversions.ToUint32(x));
            Assert.Equal(unchecked((short)residue), IntegerConversions.ToInt16(x));
            Assert.Equal(unchecked((ushort)residue), IntegerConversions.ToUint16(x));
            Assert.Equal(unchecked((sbyte)residue), IntegerConversions.ToInt8(x));
            Assert.Equal(unchecked((byte)residue), IntegerConversions.ToUint8(x));
        }
    }

    [Theory]
    [InlineData(double.NaN, 0)]
    [InlineData(-1.0, 0)]
    [InlineData(0.5, 0)]
    [InlineData(1.5, 2)]
    [InlineData(2.5, 2)]
    [InlineData(254.5, 254)]
    [InlineData(253.5, 254)]
    [InlineData(254.50000000000003, 255)]
    [InlineData(300.0, 255)]
    [InlineData(double.PositiveInfinity, 255)]
    public void The_clamp_rounds_ties_to_even(double x, byte expected) =>
        Assert.Equal(expected, IntegerConversions.ToUint8Clamp(x));

    [Fact]
    public void ToIntegerOrInfinity_ToLength_and_ToIndex()
    {
        Assert.Equal(0.0, IntegerConversions.ToIntegerOrInfinity(-0.9));
        Assert.False(double.IsNegative(IntegerConversions.ToIntegerOrInfinity(-0.9)));
        Assert.Equal(double.NegativeInfinity, IntegerConversions.ToIntegerOrInfinity(double.NegativeInfinity));
        Assert.Equal(-3.0, IntegerConversions.ToIntegerOrInfinity(-3.7));
        Assert.Equal(0L, IntegerConversions.ToLength(-5));
        Assert.Equal(IntegerConversions.MaximumSafeInteger, IntegerConversions.ToLength(1e300));
        Assert.True(IntegerConversions.TryToIndex(-0.5, out var index));
        Assert.Equal(0L, index);
        Assert.False(IntegerConversions.TryToIndex(-1, out _));
        Assert.False(IntegerConversions.TryToIndex(9007199254740992, out _));
        Assert.True(IntegerConversions.TryToIndex(9007199254740991, out index));
        Assert.Equal(9007199254740991L, index);
        Assert.Equal(long.MinValue, IntegerConversions.ToBigInt64(BigInteger.Pow(2, 63)));
        Assert.Equal(ulong.MaxValue, IntegerConversions.ToBigUint64(BigInteger.MinusOne));
    }

    [Fact]
    public void Float16_agrees_with_the_platform_s_binary16_and_round_trips_every_pattern()
    {
        foreach (var x in Doubles(0x534A4C34_4E554D32, 200_000))
        {
            if (double.IsNaN(x))
                continue;

            Assert.Equal(BitConverter.HalfToUInt16Bits((Half)x), Float16.Encode(x, Float16NanPolicy.Canonical));
        }

        for (var bits = 0; bits <= 0xFFFF; bits++)
        {
            var value = Float16.Decode((ushort)bits);
            var isNaN = (bits & 0x7C00) == 0x7C00 && (bits & 0x3FF) != 0;
            Assert.Equal(isNaN, double.IsNaN(value));
            if (!isNaN)
            {
                Assert.Equal((double)BitConverter.UInt16BitsToHalf((ushort)bits), value);
                Assert.Equal((ushort)bits, Float16.Encode(value, Float16NanPolicy.Canonical));
            }
        }

        // Directly to binary16, never through binary32.
        Assert.Equal(0x3C01, Float16.Encode(1 + Math.ScaleB(1, -11) + Math.ScaleB(1, -40), Float16NanPolicy.Canonical));
        Assert.Equal(0x7C00, Float16.Encode(65520, Float16NanPolicy.Canonical));
        Assert.Equal(0x7BFF, Float16.Encode(65519.99, Float16NanPolicy.Canonical));
    }

    [Fact]
    public void Float16_writes_a_NaN_under_its_policy()
    {
        var signaling = BitConverter.UInt64BitsToDouble(0xFFF4000000000000);
        Assert.Equal(Float16.CanonicalNaN, Float16.Encode(signaling, Float16NanPolicy.Canonical));
        Assert.Equal(0xFF00, Float16.Encode(signaling, Float16NanPolicy.PreservePayload));
        Assert.Equal(BitConverter.HalfToUInt16Bits((Half)signaling), Float16.Encode(signaling, Float16NanPolicy.PreservePayload));
        Assert.True(double.IsNaN(Float16.Round(double.NaN)));
    }

    [Theory]
    [InlineData(-0.5, -0.0)]
    [InlineData(-2.5, -2.0)]
    [InlineData(2.5, 3.0)]
    [InlineData(0.49999999999999994, 0.0)]
    [InlineData(4503599627370495.5, 4503599627370496.0)]
    [InlineData(-4503599627370495.5, -4503599627370495.0)]
    public void Round_ties_toward_positive_infinity(double x, double expected)
    {
        var rounded = MathKernels.Round(x);
        Assert.Equal(expected, rounded);
        Assert.Equal(double.IsNegative(expected), double.IsNegative(rounded));
    }

    [Fact]
    public void Power_has_the_clause_s_special_cases()
    {
        Assert.True(double.IsNaN(MathKernels.Power(1, double.NaN)));
        Assert.True(double.IsNaN(MathKernels.Power(-1, double.PositiveInfinity)));
        Assert.True(double.IsNaN(MathKernels.Power(1, double.NegativeInfinity)));
        Assert.Equal(1.0, MathKernels.Power(double.NaN, -0.0));
        Assert.Equal(double.NegativeInfinity, MathKernels.Power(-0.0, -3));
        Assert.Equal(0.0, MathKernels.Power(0.5, double.PositiveInfinity));
        Assert.Equal(1024.0, MathKernels.Power(2, 10));
    }

    [Fact]
    public void Cbrt_is_correctly_rounded()
    {
        foreach (var x in Doubles(0x534A4C34_4E554D33, 20_000))
        {
            if (!double.IsFinite(x) || x == 0)
                continue;

            var root = MathKernels.Cbrt(x);
            Assert.Equal(Math.Sign(x), Math.Sign(root));
            var magnitude = Math.Abs(root);

            // The exact cube root lies between the midpoints around the result.
            Assert.True(CubeCompare(Math.BitDecrement(magnitude), magnitude, Math.Abs(x)) <= 0, x.ToString("R", CultureInfo.InvariantCulture));
            Assert.True(CubeCompare(magnitude, Math.BitIncrement(magnitude), Math.Abs(x)) >= 0, x.ToString("R", CultureInfo.InvariantCulture));
        }

        for (var n = -2000; n <= 2000; n++)
            Assert.Equal((double)n, MathKernels.Cbrt((double)n * n * n));
    }

    /// <summary>The sign of (the midpoint of a and b)^3 - x, exactly, over integers scaled by 2^1200.</summary>
    private static int CubeCompare(double a, double b, double x)
    {
        static BigInteger Scaled(double value, int scale)
        {
            var bits = BitConverter.DoubleToUInt64Bits(value);
            var field = (int)((bits >> 52) & 0x7FF);
            var significand = (bits & ((1UL << 52) - 1)) | (field == 0 ? 0 : 1UL << 52);
            var exponent = (field == 0 ? -1074 : field - 1075) + scale;
            return exponent >= 0 ? new BigInteger(significand) << exponent : new BigInteger(significand) >> -exponent;
        }

        var midpointTwice = Scaled(a, 1200) + Scaled(b, 1200);
        var cube = midpointTwice * midpointTwice * midpointTwice;
        return cube.CompareTo(Scaled(x, 3600) << 3);
    }

    [Fact]
    public void Hypot_gives_exact_hypotenuses_and_avoids_overflow()
    {
        foreach (var k in new[] { 0, 1000, -1074, 1018, -1060 })
        {
            Assert.Equal(Math.ScaleB(5, k), MathKernels.Hypot([Math.ScaleB(3, k), Math.ScaleB(4, k)]));
            Assert.Equal(Math.ScaleB(15, k), MathKernels.Hypot([Math.ScaleB(2, k), Math.ScaleB(10, k), Math.ScaleB(11, k)]));
        }

        Assert.Equal(0.0, MathKernels.Hypot([]));
        Assert.Equal(double.PositiveInfinity, MathKernels.Hypot([double.NaN, double.NegativeInfinity]));
        Assert.True(double.IsNaN(MathKernels.Hypot([1, double.NaN])));
        Assert.Equal(1.4142135623730951e300, MathKernels.Hypot([1e300, 1e300]));
        Assert.Equal(double.PositiveInfinity, MathKernels.Hypot([double.MaxValue, double.MaxValue]));
    }

    [Theory]
    // CPython's mpmath-free decimal module, at 60 digits, rounded to the nearest double.
    [InlineData(1e-300, 1e-300)]
    [InlineData(1e-10, 1.00000000005e-10)]
    [InlineData(-745, -1.0)]
    [InlineData(1.0, 1.718281828459045)]
    [InlineData(709.78, 1.7928227943945155e308)]
    public void Expm1_is_accurate_near_zero_and_across_the_range(double x, double expected) =>
        Assert.InRange(MathKernels.Expm1(x), Math.BitDecrement(expected), Math.BitIncrement(expected));

    [Theory]
    [InlineData(1e-300, 1e-300)]
    [InlineData(1e-10, 9.9999999995e-11)]
    [InlineData(-0.5, -0.6931471805599453)]
    [InlineData(1.0, 0.6931471805599453)]
    [InlineData(1e300, 690.7755278982137)]
    public void Log1p_is_accurate_near_zero_and_across_the_range(double x, double expected) =>
        Assert.InRange(MathKernels.Log1p(x), Math.BitDecrement(expected), Math.BitIncrement(expected));

    [Fact]
    public void Expm1_and_Log1p_have_the_clause_s_special_values()
    {
        Assert.True(double.IsNegative(MathKernels.Expm1(-0.0)));
        Assert.Equal(-1.0, MathKernels.Expm1(double.NegativeInfinity));
        Assert.Equal(double.PositiveInfinity, MathKernels.Expm1(double.PositiveInfinity));
        Assert.Equal(double.NegativeInfinity, MathKernels.Log1p(-1));
        Assert.True(double.IsNaN(MathKernels.Log1p(-1.5)));
        Assert.True(double.IsNegative(MathKernels.Log1p(-0.0)));
    }

    [Fact]
    public void PreciseSum_is_the_exact_sum_rounded_once()
    {
        var random = new SplitMix64(0x534A4C34_4E554D34);
        for (var trial = 0; trial < 2000; trial++)
        {
            var sum = new PreciseSum();
            var exact = BigInteger.Zero;
            var count = (int)(random.Next() % 40) + 1;
            for (var i = 0; i < count; i++)
            {
                var bits = random.Next() & ~(0x7FFUL << 52) | ((random.Next() % 2046 + 1) << 52);
                var x = BitConverter.UInt64BitsToDouble(bits);
                sum.Add(x);
                exact += Units(x);
            }

            var expected = exact.IsZero ? 0 : ExactRadix.Nearest(exact, BigInteger.One << 1074);
            Assert.Equal(BitConverter.DoubleToUInt64Bits(expected), BitConverter.DoubleToUInt64Bits(sum.Result()));
        }

        var overflow = new PreciseSum();
        foreach (var x in new[] { double.MaxValue, double.MaxValue, -double.MaxValue, 1.0 })
            overflow.Add(x);

        Assert.Equal(double.MaxValue, overflow.Result());
        Assert.True(double.IsNegative(new PreciseSum().Result()));
    }

    private static BigInteger Units(double value)
    {
        var bits = BitConverter.DoubleToUInt64Bits(Math.Abs(value));
        var field = (int)(bits >> 52);
        var significand = (bits & ((1UL << 52) - 1)) | (field == 0 ? 0 : 1UL << 52);
        var units = new BigInteger(significand) << ((field == 0 ? 1 : field) - 1);
        return value < 0 ? -units : units;
    }

    [Fact]
    public void ToDouble_rounds_a_rational_once_over_the_whole_range()
    {
        var random = new SplitMix64(0x534A4C34_4E554D35);
        for (var trial = 0; trial < 5000; trial++)
        {
            var numerator = new BigInteger(random.Next()) << (int)(random.Next() % 1100);
            var denominator = (new BigInteger(random.Next()) << (int)(random.Next() % 1100)) + 1;
            if (random.Next() % 2 == 0)
                numerator = -numerator;

            Assert.Equal(ExactRadix.Nearest(numerator, denominator), ExactRounding.ToDouble(numerator, denominator));
        }

        // A midpoint between doubles rounds to even, a subnormal included; past the threshold is an infinity.
        Assert.Equal(9007199254740992.0, ExactRounding.ToDouble(9007199254740993, 1));
        Assert.Equal(2 * double.Epsilon, ExactRounding.ToDouble(3, BigInteger.One << 1075));
        Assert.Equal(double.Epsilon, ExactRounding.ToDouble(3, BigInteger.One << 1076));
        Assert.Equal(0.0, ExactRounding.ToDouble(1, BigInteger.One << 1075));
        Assert.True(double.IsNegative(ExactRounding.ToDouble(-1, BigInteger.One << 1076)));
        Assert.Equal(double.PositiveInfinity, ExactRounding.ToDouble((BigInteger.One << 1024) - (BigInteger.One << 970), 1));
        Assert.Equal(double.MaxValue, ExactRounding.ToDouble((BigInteger.One << 1024) - (BigInteger.One << 970) - 1, 1));
    }

    [Fact]
    public void The_rounding_modes_at_a_tie_and_off_it()
    {
        // mode: (5 by 10, -5 by 10, 4 by 10, -6 by 10)
        var expected = new Dictionary<RoundingMode, int[]>
        {
            [RoundingMode.Ceil] = [10, 0, 10, 0], [RoundingMode.Floor] = [0, -10, 0, -10],
            [RoundingMode.Expand] = [10, -10, 10, -10], [RoundingMode.Trunc] = [0, 0, 0, 0],
            [RoundingMode.HalfCeil] = [10, 0, 0, -10], [RoundingMode.HalfFloor] = [0, -10, 0, -10],
            [RoundingMode.HalfExpand] = [10, -10, 0, -10], [RoundingMode.HalfTrunc] = [0, 0, 0, -10],
            [RoundingMode.HalfEven] = [0, 0, 0, -10],
        };
        foreach (var (mode, results) in expected)
        {
            int[] inputs = [5, -5, 4, -6];
            for (var i = 0; i < 4; i++)
                Assert.Equal(new BigInteger(results[i]), ExactRounding.RoundNumberToIncrement(inputs[i], 1, 10, mode));

            Assert.True(ExactRounding.TryParseRoundingMode(Name(mode), out var parsed));
            Assert.Equal(mode, parsed);
        }

        Assert.False(ExactRounding.TryParseRoundingMode("HalfEven", out _));
        Assert.Equal(new BigInteger(-20), ExactRounding.RoundNumberToIncrementAsIfPositive(-15, 1, 10, RoundingMode.Floor));
        Assert.Equal(new BigInteger(-10), ExactRounding.RoundNumberToIncrementAsIfPositive(-15, 1, 10, RoundingMode.HalfExpand));
        Assert.Equal(RoundingMode.HalfFloor, ExactRounding.Negate(RoundingMode.HalfCeil));

        // An exact end is itself under every mode, the upper one included (milestone SJL-4.3's
        // reference-arm comparison found the upper end answered by the lower under "zero").
        foreach (var unsigned in Enum.GetValues<UnsignedRoundingMode>())
        {
            Assert.Equal(new BigInteger(20), ExactRounding.ApplyUnsignedRoundingMode(20, 1, 10, 20, unsigned));
            Assert.Equal(new BigInteger(10), ExactRounding.ApplyUnsignedRoundingMode(10, 1, 10, 20, unsigned));
        }

        // A window wider than one step, as NudgeToCalendarUnit applies one: 15 of [10, 20] at half-even is 20 (20 / 10 is even).
        Assert.Equal(new BigInteger(20), ExactRounding.ApplyUnsignedRoundingMode(15, 1, 10, 20, UnsignedRoundingMode.HalfEven));
        // 7 / 2 and 5 / 2 are ties of increment 1: half-even takes 4 and 2.
        Assert.Equal(new BigInteger(4), ExactRounding.RoundNumberToIncrement(7, 2, 1, RoundingMode.HalfEven));
        Assert.Equal(new BigInteger(2), ExactRounding.RoundNumberToIncrement(5, 2, 1, RoundingMode.HalfEven));
    }

    private static string Name(RoundingMode mode)
    {
        var name = mode.ToString();
        return char.ToLowerInvariant(name[0]) + name[1..];
    }

    [Fact]
    public void BigInt_text_round_trips_at_every_radix_and_matches_the_platform_at_radix_10()
    {
        var random = new SplitMix64(0x534A4C34_4E554D36);
        for (var trial = 0; trial < 300; trial++)
        {
            var words = (int)(random.Next() % 120) + 1;
            var value = BigInteger.Zero;
            for (var i = 0; i < words; i++)
                value = (value << 64) | random.Next();
            if (random.Next() % 2 == 0)
                value = -value;

            Assert.Equal(value.ToString(CultureInfo.InvariantCulture), BigIntText.ToString(value, 10, null));
            var radix = (int)(random.Next() % 35) + 2;
            var text = BigIntText.ToString(value, radix, null);
            Assert.Equal(value, FromRadix(text, radix));
        }
    }

    private static BigInteger FromRadix(string text, int radix)
    {
        var negative = text.StartsWith('-');
        var value = BigInteger.Zero;
        foreach (var c in negative ? text[1..] : text)
            value = (value * radix) + (c <= '9' ? c - '0' : c - 'a' + 10);

        return negative ? -value : value;
    }

    [Fact]
    public void StringToBigInt_reads_the_StringIntegerLiteral_grammar()
    {
        var policy = BigIntPolicy.Unbounded;
        Assert.Equal(ResultStatus.Completed, BigIntText.StringToBigInt(" \uFEFF\u3000-0012\u2029", policy, null, out var value, out _));
        Assert.Equal(new BigInteger(-12), value);
        Assert.Equal(ResultStatus.Completed, BigIntText.StringToBigInt("0X1f", policy, null, out value, out _));
        Assert.Equal(new BigInteger(31), value);
        Assert.Equal(ResultStatus.Completed, BigIntText.StringToBigInt("", policy, null, out value, out _));
        Assert.Equal(BigInteger.Zero, value);
        foreach (var text in new[] { "-0x1", "0x", "1e3", "1_0", "\u180E1", "1n", "\u0661", "1.0", "+-1", "0b2" })
            Assert.Equal(ResultStatus.Malformed, BigIntText.StringToBigInt(text, policy, null, out _, out _));

        var narrow = new BigIntPolicy(64);
        Assert.Equal(ResultStatus.LimitExceeded, BigIntText.StringToBigInt("-" + new string('9', 30), narrow, null, out _, out var sign));
        Assert.Equal(-1, sign);
    }

    [Fact]
    public void BigInt_operations_refuse_past_the_policy_at_its_exact_edge()
    {
        var policy = new BigIntPolicy(1L << 20);
        var top = BigInteger.One << ((1 << 20) - 1);
        Assert.Equal(ResultStatus.Completed, BigIntMath.Add(top - 1, top, policy, null, out _));
        Assert.Equal(ResultStatus.LimitExceeded, BigIntMath.Add(top, top, policy, null, out _));
        Assert.Equal(ResultStatus.Completed, BigIntMath.LeftShift(BigInteger.MinusOne, 1 << 20, policy, null, out var negative));
        Assert.Equal(-(BigInteger.One << (1 << 20)), negative);
        Assert.Equal(ResultStatus.LimitExceeded, BigIntMath.LeftShift(BigInteger.One, 1 << 20, policy, null, out _));
        Assert.Equal(ResultStatus.LimitExceeded, BigIntMath.Power(2, 1 << 20, policy, null, out _));
        Assert.Equal(ResultStatus.Completed, BigIntMath.Power(BigInteger.MinusOne, BigInteger.Pow(2, 64) + 1, policy, null, out var power));
        Assert.Equal(BigInteger.MinusOne, power);
        Assert.Equal(ResultStatus.LimitExceeded, BigIntMath.AsUintN(1UL << 32, BigInteger.MinusOne, BigIntPolicy.Unbounded, null, out _));
        Assert.Equal(ResultStatus.Completed, BigIntMath.AsIntN((1UL << 53) - 1, BigInteger.MinusOne, BigIntPolicy.Unbounded, null, out var wrapped));
        Assert.Equal(BigInteger.MinusOne, wrapped);
        Assert.Equal(ResultStatus.LimitExceeded, BigIntMath.LeftShift(BigInteger.One, BigInteger.Pow(2, 64), BigIntPolicy.Unbounded, null, out _));
    }

    [Fact]
    public void BigInt_charges_are_Broiler_VM_s_units()
    {
        ulong total = 0;
        void Meter(ulong units) => total += units;

        var left = BigInteger.One << 1000;
        var right = (BigInteger.One << 500) + 1;
        BigIntMath.Multiply(left, right, BigIntPolicy.Unbounded, Meter, out _);

        // 1001 and 501 bits are 17 and 9 words (64-bit words, plus one): 17 x 9 / 16 + 32 x (17 + 9).
        Assert.Equal(17UL * 9 / 16 + (32UL * 26), total);
        Assert.Equal(17, BigIntMath.Words(left));
        Assert.Equal(3UL, BigIntMath.LinearCost(2));
        Assert.Equal(2UL * 2 / 2 + 32 + 1, BigIntMath.LeafCost(2));
    }

    [Fact]
    public void A_BigInt_and_a_Number_convert_and_compare_exactly()
    {
        Assert.Equal(9007199254740992.0, BigIntMath.ToNumber((BigInteger.One << 53) + 1, null));
        Assert.Equal(9007199254740996.0, BigIntMath.ToNumber((BigInteger.One << 53) + 3, null));
        Assert.Equal(double.PositiveInfinity, BigIntMath.ToNumber((BigInteger.One << 1024) - (BigInteger.One << 970), null));
        Assert.Equal(-1, BigIntMath.CompareToNumber(BigInteger.One << 53, 9007199254740994.0, null));
        Assert.Equal(1, BigIntMath.CompareToNumber(2, 1.5, null));
        Assert.Equal(-1, BigIntMath.CompareToNumber(1, 1.5, null));
        Assert.Equal(BigInteger.Pow(2, 1023), BigIntMath.FromIntegralNumber(Math.ScaleB(1, 1023)));
        Assert.Throws<ArgumentOutOfRangeException>(() => BigIntMath.FromIntegralNumber(0.5));
    }
}
