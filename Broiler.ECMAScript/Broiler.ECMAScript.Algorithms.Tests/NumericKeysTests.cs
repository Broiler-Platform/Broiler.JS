using System;
using Broiler.ECMAScript.Numbers;
using Xunit;

namespace Broiler.ECMAScript.Algorithms.Tests;

/// <summary>Array indices and CanonicalNumericIndexString.</summary>
public class NumericKeysTests
{
    [Theory]
    [InlineData("0", true, 0U)]
    [InlineData("1", true, 1U)]
    [InlineData("4294967294", true, 4294967294U)]
    [InlineData("4294967295", false, 0U)]
    [InlineData("01", false, 0U)]
    [InlineData("-0", false, 0U)]
    [InlineData("1.0", false, 0U)]
    [InlineData("", false, 0U)]
    [InlineData("99999999999", false, 0U)]
    public void Array_index(string key, bool expected, uint index)
    {
        Assert.Equal(expected, NumericKeys.TryParseArrayIndex(key, out var parsed));
        Assert.Equal(index, parsed);
    }

    [Theory]
    [InlineData("-0", true)]
    [InlineData("0", true)]
    [InlineData("0.1", true)]
    [InlineData("1e+21", true)]
    [InlineData("1e21", false)]
    [InlineData("Infinity", true)]
    [InlineData("-Infinity", true)]
    [InlineData("NaN", true)]
    [InlineData("+1", false)]
    [InlineData(" 1", false)]
    [InlineData("1.0", false)]
    [InlineData("01", false)]
    [InlineData("4294967295", true)]
    public void CanonicalNumericIndexString(string key, bool expected)
    {
        Assert.Equal(expected, NumericKeys.IsCanonicalNumericString(key, out var value));
        if (key == "-0")
            Assert.True(value == 0 && double.IsNegative(value));
    }
}
