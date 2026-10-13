using System;
using System.Numerics;
using System.Collections.Generic;
using Broiler.ECMAScript.Numbers;
using Xunit;

namespace Broiler.ECMAScript.Algorithms.Tests;

/// <summary>
/// String to number: the white-space set, StringToNumber, parseFloat and parseInt, and the meter.
/// Every expected value is the exact reference computation's (scripts/conformance/numref.py).
/// </summary>
public class NumberParsingTests
{
    [Fact]
    public void The_white_space_set_is_the_specification_s_from_Unicode_17()
    {
        int[] members = [0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x20, 0xA0, 0x1680, 0x2000, 0x2005, 0x200A, 0x2028, 0x2029, 0x202F, 0x205F, 0x3000, 0xFEFF];
        for (var c = 0; c <= 0xFFFF; c++)
            Assert.Equal(Array.IndexOf(members, c) >= 0 || (c >= 0x2000 && c <= 0x200A), NumberParsing.IsStrWhiteSpaceChar((char)c));

        // U+0085 is .NET white space and not ECMAScript's; U+180E left Zs in Unicode 6.3.
        Assert.False(NumberParsing.IsStrWhiteSpaceChar('\u0085'));
        Assert.False(NumberParsing.IsStrWhiteSpaceChar('᠎'));
        Assert.Equal("17.0.0", NumberParsing.UnicodeVersion);
    }

    [Fact]
    public void TrimStrWhiteSpace_trims_the_chosen_ends()
    {
        const string text = "﻿\t x 　";
        Assert.Equal("x", NumberParsing.TrimStrWhiteSpace(text, TrimEnds.Both).ToString());
        Assert.Equal("x 　", NumberParsing.TrimStrWhiteSpace(text, TrimEnds.Start).ToString());
        Assert.Equal("﻿\t x", NumberParsing.TrimStrWhiteSpace(text, TrimEnds.End).ToString());
    }

    [Theory]
    [InlineData("", 0x0000000000000000UL)]
    [InlineData(" \n ", 0x0000000000000000UL)]
    [InlineData("  12  ", 0x4028000000000000UL)]
    [InlineData("-0", 0x8000000000000000UL)]
    [InlineData("0x1F", 0x403F000000000000UL)]
    [InlineData("0x200000000000011", 0x4380000000000001UL)]
    [InlineData("1e1000", 0x7FF0000000000000UL)]
    [InlineData("-Infinity", 0xFFF0000000000000UL)]
    [InlineData(".5", 0x3FE0000000000000UL)]
    [InlineData("5.", 0x4014000000000000UL)]
    [InlineData("+0x1F", 0x7FF8000000000000UL)]
    [InlineData("infinity", 0x7FF8000000000000UL)]
    [InlineData("1_000", 0x7FF8000000000000UL)]
    [InlineData(".", 0x7FF8000000000000UL)]
    [InlineData("1e", 0x7FF8000000000000UL)]
    [InlineData("0x", 0x7FF8000000000000UL)]
    [InlineData("\u00851", 0x7FF8000000000000UL)]
    public void StringToNumber(string text, ulong expected) => Assert.Equal(expected, Bits(NumberParsing.StringToNumber(text)));

    [Theory]
    [InlineData("3.14abc", 0x40091EB851EB851FUL)]
    [InlineData("Infinityx", 0x7FF0000000000000UL)]
    [InlineData("  -.5e-3x", 0xBF40624DD2F1A9FCUL)]
    [InlineData("1e", 0x3FF0000000000000UL)]
    [InlineData("e5", 0x7FF8000000000000UL)]
    [InlineData("﻿7", 0x401C000000000000UL)]
    public void ParseFloat(string text, ulong expected) => Assert.Equal(expected, Bits(NumberParsing.ParseFloat(text)));

    [Theory]
    [InlineData("0x10", 0, 0x4030000000000000UL)]
    [InlineData("0x10", 16, 0x4030000000000000UL)]
    [InlineData("0x10", 10, 0x0000000000000000UL)]
    [InlineData("  -0", 0, 0x8000000000000000UL)]
    [InlineData("10", 1, 0x7FF8000000000000UL)]
    [InlineData("10", 37, 0x7FF8000000000000UL)]
    [InlineData("zz", 36, 0x40943C0000000000UL)]
    [InlineData("200000000000011", 16, 0x4380000000000001UL)]
    [InlineData("9007199254740993", 10, 0x4340000000000000UL)]
    [InlineData("null", 36, 0x4130FAA900000000UL)]
    [InlineData("\u00851", 10, 0x7FF8000000000000UL)]
    public void ParseInt(string text, int radix, ulong expected) => Assert.Equal(expected, Bits(NumberParsing.ParseInt(text, radix)));

    [Fact]
    public void Each_parse_charges_the_input_s_length_once_before_it_reads()
    {
        var charges = new List<ulong>();
        NumberParsing.StringToNumber("12345", charges.Add);
        NumberParsing.ParseFloat("1.5", charges.Add);
        NumberParsing.ParseInt("ff", 16, charges.Add);
        NumberParsing.StringToNumber(string.Empty, charges.Add);
        Assert.Equal([5UL, 3UL, 2UL], charges);

        // A meter that throws stops the operation, which leaves nothing changed.
        Assert.Throws<InvalidOperationException>(() => NumberParsing.StringToNumber("1", static _ => throw new InvalidOperationException()));
        Assert.Equal(12, NumberParsing.StringToNumber("12", null));
    }

    private static ulong Bits(double value) => double.IsNaN(value) ? 0x7FF8000000000000UL : BitConverter.DoubleToUInt64Bits(value);

    [Fact]
    public void Random_digit_runs_in_every_radix_are_rounded_once()
    {
        var random = new SplitMix64(0x5041_5253_4549_4E54);
        const string alphabet = "0123456789abcdefghijklmnopqrstuvwxyz";
        for (var i = 0; i < 20_000; i++)
        {
            var radix = 2 + (int)(random.Next() % 35);
            var length = 1 + (int)(random.Next() % (i % 10 == 0 ? 400UL : 40UL));
            var text = new char[length];
            var value = BigInteger.Zero;
            for (var k = 0; k < length; k++)
            {
                var digit = (int)(random.Next() % (ulong)radix);
                text[k] = alphabet[digit];
                value = (value * radix) + digit;
            }

            var expected = value.GetBitLength() > 1100 ? double.PositiveInfinity : ExactRadix.Nearest(value, BigInteger.One);
            Assert.Equal(BitConverter.DoubleToUInt64Bits(expected), BitConverter.DoubleToUInt64Bits(NumberParsing.ParseInt(new string(text), radix)));
        }
    }
}
