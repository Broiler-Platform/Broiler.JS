using System;
using System.Collections.Generic;
using System.Globalization;
using Broiler.ECMAScript.Numbers;
using Xunit;

namespace Broiler.ECMAScript.Algorithms.Tests;

/// <summary>
/// Number to string: each operation's edges and preconditions, and properties over random doubles.
/// Every expected string is the exact reference computation's (scripts/conformance/numref.py).
/// </summary>
public class NumberTextTests
{
    [Theory]
    [InlineData(2.9802322387695312e-8, "2.9802322387695312e-8")]
    [InlineData(1e21, "1e+21")]
    [InlineData(123e-20, "1.23e-18")]
    [InlineData(0.000001, "0.000001")]
    [InlineData(1e-7, "1e-7")]
    [InlineData(5e-324, "5e-324")]
    [InlineData(1.7976931348623157e308, "1.7976931348623157e+308")]
    [InlineData(1.0 / 3, "0.3333333333333333")]
    [InlineData(9007199254740994.0, "9007199254740994")]
    [InlineData(4294967295.5, "4294967295.5")]
    [InlineData(-0.0, "0")]
    [InlineData(double.NegativeInfinity, "-Infinity")]
    [InlineData(double.NaN, "NaN")]
    public void Number_toString_at_radix_10(double value, string expected)
    {
        Assert.Equal(expected, NumberText.ToString(value));
        Assert.Equal(expected, NumberText.ToRadixString(value, 10));
    }

    [Theory]
    [InlineData(18446744073709551616.0, 2, "10000000000000000000000000000000000000000000000000000000000000000")]
    [InlineData(0.5, 2, "0.1")]
    [InlineData(255.5, 16, "ff.8")]
    [InlineData(-255.0, 36, "-73")]
    [InlineData(0.1, 3, "0.0022002200220022002200220022002201")]
    [InlineData(123.456, 7, "234.312256641535441")]
    [InlineData(1e100, 7, "16201341553122251064000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")]
    public void Number_toString_at_other_radixes(double value, int radix, string expected) =>
        Assert.Equal(expected, NumberText.ToRadixString(value, radix));

    [Theory]
    [InlineData(2.5, 0, "3")]
    [InlineData(0.5, 0, "1")]
    [InlineData(1.005, 2, "1.00")]
    [InlineData(1.45, 1, "1.4")]
    [InlineData(-1e-10, 2, "-0.00")]
    [InlineData(-0.0, 2, "0.00")]
    [InlineData(1e21, 2, "1e+21")]
    [InlineData(double.NaN, 2, "NaN")]
    public void ToFixed(double value, int digits, string expected) => Assert.Equal(expected, NumberText.ToFixed(value, digits));

    [Fact]
    public void ToExponential()
    {
        Assert.Equal("1.23e+5", NumberText.ToExponential(123456, 2));
        Assert.Equal("1.3e+0", NumberText.ToExponential(1.25, 1));
        Assert.Equal("0e+0", NumberText.ToExponential(0));
        Assert.Equal("0.00e+0", NumberText.ToExponential(-0.0, 2));
        Assert.Equal("2.9802322387695312e-8", NumberText.ToExponential(2.9802322387695312e-8));
        Assert.Equal("-Infinity", NumberText.ToExponential(double.NegativeInfinity, 3));
    }

    [Theory]
    [InlineData(2745.0, 3, "2.75e+3")]
    [InlineData(0.000001, 2, "0.0000010")]
    [InlineData(1e-7, 1, "1e-7")]
    [InlineData(123.456, 4, "123.5")]
    [InlineData(999.95, 4, "1000")]
    [InlineData(0.0, 3, "0.00")]
    public void ToPrecision(double value, int precision, string expected) => Assert.Equal(expected, NumberText.ToPrecision(value, precision));

    [Fact]
    public void A_broken_precondition_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => NumberText.ToRadixString(1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => NumberText.ToRadixString(1, 37));
        Assert.Throws<ArgumentOutOfRangeException>(() => NumberText.ToFixed(1, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => NumberText.ToFixed(1, 101));
        Assert.Throws<ArgumentOutOfRangeException>(() => NumberText.ToExponential(1, 101));
        Assert.Throws<ArgumentOutOfRangeException>(() => NumberText.ToPrecision(1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => NumberText.ToPrecision(1, 101));
    }

    [Fact]
    public void TryFormat_writes_nothing_into_a_short_buffer()
    {
        Span<char> buffer = stackalloc char[4];
        Assert.False(NumberText.TryFormat(123.456, buffer, out var written));
        Assert.Equal(0, written);
        Span<char> enough = stackalloc char[NumberText.MaximumLength];
        Assert.True(NumberText.TryFormat(-1.7976931348623157e308, enough, out written));
        Assert.Equal("-1.7976931348623157e+308", enough[..written].ToString());
    }

    [Fact]
    public void The_generator_is_SplitMix64_by_its_published_reference_outputs()
    {
        var random = new SplitMix64(1234567);
        Assert.Equal([6457827717110365317UL, 3203168211198807973UL, 9817491932198370423UL, 4593380528125082431UL, 16408922859458223821UL],
            [random.Next(), random.Next(), random.Next(), random.Next(), random.Next()]);
    }

    [Fact]
    public void Random_doubles_round_trip_through_the_shortest_digits()
    {
        var random = new SplitMix64(0x5348_4F52_5445_5354);
        for (var i = 0; i < 100_000; i++)
        {
            var bits = random.Next();
            var x = BitConverter.UInt64BitsToDouble(bits);
            if (!double.IsFinite(x) || x == 0)
                continue;

            var text = NumberText.ToString(x);
            Assert.Equal(bits, BitConverter.DoubleToUInt64Bits(double.Parse(text, NumberStyles.Float, NumberFormatInfo.InvariantInfo)));

            // No string of one digit fewer reads back as x.
            var digits = Significant(text);
            if (digits > 1)
            {
                var shorter = Math.Abs(x).ToString("E" + (digits - 2).ToString(CultureInfo.InvariantCulture), NumberFormatInfo.InvariantInfo);
                Assert.NotEqual(Math.Abs(x), double.Parse(shorter, NumberStyles.Float, NumberFormatInfo.InvariantInfo));
            }
        }
    }

    [Fact]
    public void Random_doubles_round_trip_through_every_radix()
    {
        var random = new SplitMix64(0x5241_4449_5853_4554);
        for (var i = 0; i < 20_000; i++)
        {
            var bits = random.Next();
            var x = BitConverter.UInt64BitsToDouble(bits);
            if (!double.IsFinite(x) || x == 0)
                continue;

            // Radix 10 is Number::toString, whose exponent form the platform reads exactly.
            var radix = 2 + (int)(random.Next() % 35);
            var text = NumberText.ToRadixString(x, radix);
            var back = radix == 10 ? double.Parse(text, NumberStyles.Float, NumberFormatInfo.InvariantInfo) : ExactRadix.Read(text, radix);
            Assert.Equal(bits, BitConverter.DoubleToUInt64Bits(back));
        }
    }

    [Fact]
    public void The_integer_and_narrow_radix_paths_agree_with_the_big_number_path()
    {
        // Moderate magnitudes, where the narrow path is taken; integers below and above 2^53; and
        // the values the measurement workloads scale.
        var random = new SplitMix64(0x4E41_5252_4F57_0001);
        var values = new List<double>();
        for (var i = 0; i < 40_000; i++)
        {
            var magnitude = Math.Pow(2, (int)(random.Next() % 200) - 100);
            values.Add(magnitude * (1 + ((random.Next() >> 11) / 9007199254740992.0)));
            values.Add(random.Next() >> (int)(random.Next() % 64));
        }

        foreach (var x in (double[])[0.1, 0.5, 1.0 / 3, 2.0 / 3, 123.456, 987.654321, 1e21, 1.5e-7, 9007199254740992, 9007199254740994,
                                      123456789012345680000.0, 0.000001, 1e-7, 255, 4294967295, Math.PI, Math.E])
        {
            for (var step = 0; step < 400; step++)
                values.Add(x * (1 + (step / 1024.0)));
        }

        foreach (var x in values)
        {
            if (x == 0)
                continue;

            for (var radix = 3; radix <= 36; radix++)
            {
                if ((radix & (radix - 1)) == 0 || radix == 10)
                    continue;

                Assert.Equal(RadixDigits.FormatWide(x, radix), NumberText.ToRadixString(x, radix));
            }
        }
    }

    private static int Significant(string text)
    {
        var mantissa = text.TrimStart('-');
        var e = mantissa.IndexOf('e', StringComparison.Ordinal);
        if (e >= 0)
            mantissa = mantissa[..e];

        return mantissa.Replace(".", string.Empty, StringComparison.Ordinal).TrimStart('0').TrimEnd('0').Length;
    }
}
