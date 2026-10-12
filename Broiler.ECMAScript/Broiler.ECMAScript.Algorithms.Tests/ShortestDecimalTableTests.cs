using System;
using System.Numerics;
using Broiler.ECMAScript.Numbers;
using Xunit;

namespace Broiler.ECMAScript.Algorithms.Tests;

/// <summary>
/// The shortest-digits multipliers are exact: every entry of both tables recomputed in big-integer
/// arithmetic from its definition, as ShortestDecimal's remarks state them.
/// </summary>
public class ShortestDecimalTableTests
{
    [Fact]
    public void Every_inverse_multiplier_is_floor_of_two_to_the_power_over_five_to_the_q_plus_one()
    {
        var table = ShortestDecimal.Pow5InverseSplit;
        Assert.Equal(2 * 342, table.Length);
        for (var q = 0; q < 342; q++)
        {
            var power = BigInteger.Pow(5, q);
            var expected = (BigInteger.One << ((int)power.GetBitLength() - 1 + 125)) / power + 1;
            Assert.Equal(expected, Join(table[2 * q], table[(2 * q) + 1]));
        }
    }

    [Fact]
    public void Every_multiplier_is_five_to_the_i_normalised_to_125_bits()
    {
        var table = ShortestDecimal.Pow5Split;
        Assert.Equal(2 * 326, table.Length);
        for (var i = 0; i < 326; i++)
        {
            var power = BigInteger.Pow(5, i);
            var shift = (int)power.GetBitLength() - 125;
            var expected = shift >= 0 ? power >> shift : power << -shift;
            Assert.Equal(expected, Join(table[2 * i], table[(2 * i) + 1]));
        }
    }

    private static BigInteger Join(ulong low, ulong high) => (new BigInteger(high) << 64) | new BigInteger(low);
}
