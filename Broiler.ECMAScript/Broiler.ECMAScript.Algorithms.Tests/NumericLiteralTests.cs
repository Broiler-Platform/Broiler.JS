using System;
using System.Collections.Generic;
using System.Numerics;
using Broiler.ECMAScript.Numbers;
using Xunit;

namespace Broiler.ECMAScript.Algorithms.Tests;

/// <summary>Numeric literal values: every form, every refusal with its position and identifier, strictness, and the BigInt width limit.</summary>
public class NumericLiteralTests
{
    private static readonly LengthLimit Unbounded = new(long.MaxValue);

    [Theory]
    [InlineData("0", 0.0, NumericLiteralForm.Decimal)]
    [InlineData("1_000.5e-1_0", 1000.5e-10, NumericLiteralForm.Decimal)]
    [InlineData(".5", 0.5, NumericLiteralForm.Decimal)]
    [InlineData("5.", 5.0, NumericLiteralForm.Decimal)]
    [InlineData("0x1_F", 31.0, NumericLiteralForm.NonDecimal)]
    [InlineData("0B101", 5.0, NumericLiteralForm.NonDecimal)]
    [InlineData("0o17", 15.0, NumericLiteralForm.NonDecimal)]
    [InlineData("017", 15.0, NumericLiteralForm.LegacyOctal)]
    [InlineData("08.5", 8.5, NumericLiteralForm.NonOctalDecimal)]
    [InlineData("09e1", 90.0, NumericLiteralForm.NonOctalDecimal)]
    [InlineData("0x200000000000011", 144115188075855904.0, NumericLiteralForm.NonDecimal)]
    public void A_Number_literal_s_value(string text, double expected, NumericLiteralForm form)
    {
        Assert.Equal(ResultStatus.Completed, NumericLiterals.Evaluate(text, false, Unbounded, out var literal, out _));
        Assert.Equal(NumericLiteralKind.Number, literal.Kind);
        Assert.Equal(form, literal.Form);
        Assert.Equal(BitConverter.DoubleToUInt64Bits(expected), BitConverter.DoubleToUInt64Bits(literal.Number));
    }

    [Theory]
    [InlineData("0n", "0")]
    [InlineData("1_000n", "1000")]
    [InlineData("0xFFFFFFFFFFFFFFFFFn", "295147905179352825855")]
    [InlineData("0b11n", "3")]
    [InlineData("123456789012345678901234567890n", "123456789012345678901234567890")]
    public void A_BigInt_literal_s_value(string text, string expected)
    {
        Assert.Equal(ResultStatus.Completed, NumericLiterals.Evaluate(text, true, Unbounded, out var literal, out _));
        Assert.Equal(NumericLiteralKind.BigInt, literal.Kind);
        Assert.Equal(BigInteger.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), literal.BigInt);
    }

    [Theory]
    [InlineData("01n", false, 2, "number.literal-trailing")]
    [InlineData("08n", false, 2, "number.bigint-suffix")]
    [InlineData("1.5n", false, 3, "number.literal-trailing")]
    [InlineData(".5n", false, 2, "number.literal-trailing")]
    [InlineData("07.5", false, 2, "number.literal-trailing")]
    [InlineData("0_1", false, 1, "number.literal-trailing")]
    [InlineData("1__0", false, 1, "number.literal-digits")]
    [InlineData("1_", false, 1, "number.literal-digits")]
    [InlineData("1e", false, 2, "number.literal-exponent")]
    [InlineData("0x", false, 2, "number.literal-digits")]
    [InlineData("3in", false, 1, "number.literal-trailing")]
    [InlineData("010", true, 0, "number.legacy-octal-in-strict-code")]
    [InlineData("09", true, 0, "number.non-octal-decimal-in-strict-code")]
    [InlineData("", false, 0, "number.literal-empty")]
    public void A_text_that_is_not_one_literal_is_refused_where_the_grammar_stops(string text, bool strict, int position, string messageId)
    {
        Assert.Equal(ResultStatus.Malformed, NumericLiterals.Evaluate(text, strict, Unbounded, out _, out var failure));
        Assert.Equal(new Failure(ResultStatus.Malformed, position, messageId), failure);
    }

    [Fact]
    public void A_BigInt_wider_than_the_caller_s_limit_is_refused_before_it_is_built()
    {
        var limit = new LengthLimit(64);
        Assert.Equal(ResultStatus.Completed, NumericLiterals.Evaluate("18446744073709551615n", false, limit, out _, out _));
        Assert.Equal(ResultStatus.LimitExceeded, NumericLiterals.Evaluate("18446744073709551616n", false, limit, out _, out var failure));
        Assert.Equal("number.bigint-too-wide", failure.MessageId);

        // Ten thousand digits are refused on their count alone: the meter sees the text and nothing more.
        var charges = new List<ulong>();
        var wide = new string('9', 10_000) + "n";
        Assert.Equal(ResultStatus.LimitExceeded, NumericLiterals.Evaluate(wide, false, limit, out _, out _, charges.Add));
        Assert.Equal([10_001UL], charges);
    }

    [Fact]
    public void A_BigInt_charges_its_text_and_then_its_digits()
    {
        var charges = new List<ulong>();
        Assert.Equal(ResultStatus.Completed, NumericLiterals.Evaluate("0x00ff_ffn", false, Unbounded, out _, out _, charges.Add));
        Assert.Equal([10UL, 4UL], charges);
    }
}
