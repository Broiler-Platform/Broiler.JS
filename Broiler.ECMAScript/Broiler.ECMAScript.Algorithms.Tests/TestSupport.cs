using System;
using System.Numerics;

namespace Broiler.ECMAScript.Algorithms.Tests;

/// <summary>SplitMix64 (Vigna), the generator the reference-arm rule names, for reproducible random doubles.</summary>
internal sealed class SplitMix64(ulong seed)
{
    private ulong state = seed;

    public ulong Next()
    {
        var z = state += 0x9E3779B97F4A7C15;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EB;
        return z ^ (z >> 31);
    }
}

/// <summary>Reads a positional string in a radix exactly and rounds it to the nearest double, ties to even.</summary>
internal static class ExactRadix
{
    public static double Read(string text, int radix)
    {
        var negative = text.StartsWith('-');
        if (negative)
            text = text[1..];

        var point = text.IndexOf('.', StringComparison.Ordinal);
        var integer = point >= 0 ? text[..point] : text;
        var fraction = point >= 0 ? text[(point + 1)..] : string.Empty;
        var numerator = BigInteger.Zero;
        foreach (var c in integer + fraction)
            numerator = (numerator * radix) + (c <= '9' ? c - '0' : c - 'a' + 10);

        var value = Nearest(numerator, BigInteger.Pow(radix, fraction.Length));
        return negative ? -value : value;
    }

    /// <summary>The double nearest numerator / denominator (the denominator positive), ties to even.</summary>
    public static double Nearest(BigInteger numerator, BigInteger denominator)
    {
        if (numerator.IsZero)
            return 0;

        if (numerator.Sign < 0)
            return -Nearest(-numerator, denominator);

        // Scale so the quotient has 64 or 65 bits, then keep 53 (fewer below the normal range).
        var log = (long)numerator.GetBitLength() - (long)denominator.GetBitLength();
        var shift = (int)(64 - log);
        var quotient = shift >= 0
            ? BigInteger.DivRem(numerator << shift, denominator, out var remainder)
            : BigInteger.DivRem(numerator, denominator << -shift, out remainder);
        var drop = (int)quotient.GetBitLength() - 53;
        if (-shift + drop < -1074)
            drop += -1074 - (-shift + drop);

        var kept = quotient >> drop;
        var rest = quotient & ((BigInteger.One << drop) - 1);
        var half = BigInteger.One << (drop - 1);
        if (rest > half || (rest == half && (!remainder.IsZero || !kept.IsEven)))
            kept += 1;

        return Math.ScaleB((double)kept, -shift + drop);
    }
}
