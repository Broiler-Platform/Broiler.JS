using System.Numerics;

namespace Broiler.JavaScript.Runtime;

public static class BigIntegerExtensions
{
    // FromDouble, a Number's exact integer, had no caller and is deleted (SJL-4.5): a Number becomes a
    // BigInt through the shared library's BigIntMath.FromIntegralNumber, after JSBigInt's RangeError.

    /// <summary>
    /// Shifts a BigInteger value a specified number of bits to the left.
    /// </summary>
    /// <param name="value"> The value whose bits are to be shifted. </param>
    /// <param name="shift"> The number of bits to shift <paramref name="value"/> to the left.
    /// Can be negative to shift to the right. </param>
    /// <returns> A value that has been shifted to the left by the specified number of bits. </returns>
    public static BigInteger LeftShift(this BigInteger value, int shift) => value << shift;
}
