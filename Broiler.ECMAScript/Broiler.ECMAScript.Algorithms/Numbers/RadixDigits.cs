// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   16
// Annotated:        16/16
// Exempt:           0
// Human-reviewed:   0/16
// IP risk:          Low
// Security risk:    Medium
// Criteria:         0/0
// Resource impact:  2/10 max
// Unverified:       16
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Numerics;

namespace Broiler.ECMAScript.Numbers;

/// <summary>
/// <c>Number::toString(x, radix)</c> for a radix other than 10 (ECMA-262 6.1.6.1.20): the fewest
/// digits in that radix that read back as <c>x</c>, placed positionally.
/// </summary>
/// <remarks>
/// <para>
/// <b>A radix that is a power of two needs no search.</b> A double is a finite binary fraction, so
/// its expansion in radix 2, 4, 8, 16 or 32 terminates; and no shorter string reads back as it,
/// because every other multiple of the double's last place lies outside its rounding interval. The
/// digits are read off the significand's bits directly.
/// </para>
/// <para>
/// <b>Any other radix is searched exactly</b>, by Steele and White's free-format digit generation
/// as Burger and Dybvig state it ("Printing Floating-Point Numbers Quickly and Accurately", 1996):
/// the value and its rounding interval are scaled to integers, a digit is produced per step, and
/// generation stops at the first digit after which the interval holds a shorter number. Where two
/// final digits are equally near, the one that makes the significand even is taken, as the
/// specification's note recommends for radix 10. Every step is exact, which is what both engines'
/// double-arithmetic loops were not.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.6.1.20; IP=Low; Security=Medium; Resources=2; Fingerprint=9A8AD7
// Broiler-Human:        PENDING
internal static class RadixDigits
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.6.1.20; IP=Low; Security=Medium; Resources=2; Fingerprint=ADE454
    // Broiler-Human:        PENDING
    private const string Alphabet = "0123456789abcdefghijklmnopqrstuvwxyz";

    /// <summary>The longest significand any radix from 2 to 36 needs for a double, with room for a carry.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.6.1.20; IP=Low; Security=Medium; Resources=2; Fingerprint=332249
    // Broiler-Human:        PENDING
    private const int MaximumDigits = 128;

    /// <summary>2^53, below which every integer a double holds is exact and its neighbours are one apart.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.6.1.20; IP=Low; Security=Medium; Resources=1; Fingerprint=F0E952
    // Broiler-Human:        PENDING
    private const double TwoToThe53 = 9007199254740992.0;

    /// <summary>The widest a quantity of the narrow path may grow to, in bits, with one bit to spare.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.6.1.20; IP=Low; Security=Medium; Resources=1; Fingerprint=6659F6
    // Broiler-Human:        PENDING
    private const int NarrowBits = 127;

    /// <summary>Formats a positive finite double in a radix from 2 to 36 other than 10.</summary>
    /// <param name="value">A double greater than zero and finite.</param>
    /// <param name="radix">The radix.</param>
    /// <returns>The specification's string, without a sign.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.6.1.20; IP=Low; Security=Medium; Resources=2; Fingerprint=3560F1
    // Broiler-Human:        PENDING
    internal static string Format(double value, int radix)
    {
        Span<char> digits = stackalloc char[MaximumDigits];
        int count;
        int integerDigits;
        if ((radix & (radix - 1)) == 0)
            count = PowerOfTwo(value, radix, digits, out integerDigits);
        else if (value < TwoToThe53 && value == Math.Floor(value))
            count = Integer((ulong)value, radix, digits, out integerDigits);
        else if (!TryFreeFormatNarrow(value, radix, digits, out count, out integerDigits))
            count = FreeFormat(value, radix, digits, out integerDigits);

        return Positional(digits[..count], integerDigits);
    }

    /// <summary>The big-number path alone, for any radix that is not a power of two: what the tests hold the integer and narrow paths to.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.6.1.20; IP=Low; Security=Medium; Resources=1; Fingerprint=F5A795
    // Broiler-Human:        PENDING
    internal static string FormatWide(double value, int radix)
    {
        Span<char> digits = stackalloc char[MaximumDigits];
        var count = FreeFormat(value, radix, digits, out var integerDigits);
        return Positional(digits[..count], integerDigits);
    }

    /// <summary>
    /// An integer below 2^53 is its own shortest digits in every radix: its rounding interval is
    /// at most one wide, and every other number with as few significant digits is at least one away.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.6.1.20; IP=Low; Security=Medium; Resources=1; Fingerprint=0C62BF
    // Broiler-Human:        PENDING
    private static int Integer(ulong value, int radix, Span<char> digits, out int integerDigits)
    {
        Span<char> reversed = stackalloc char[64];
        var count = 0;
        do
        {
            reversed[count++] = Alphabet[(int)(value % (ulong)radix)];
            value /= (ulong)radix;
        }
        while (value != 0);

        for (var at = 0; at < count; at++)
            digits[at] = reversed[count - 1 - at];

        integerDigits = count;
        return count;
    }

    /// <summary>The terminating expansion of a double in a radix that is a power of two.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.6.1.20; IP=Low; Security=Medium; Resources=2; Fingerprint=9FA21D
    // Broiler-Human:        PENDING
    private static int PowerOfTwo(double value, int radix, Span<char> digits, out int integerDigits)
    {
        var bits = DoubleBits.Of(value);
        var width = BitOperations.TrailingZeroCount(radix);
        var lowest = bits.Exponent + BitOperations.TrailingZeroCount(bits.Significand);
        var highest = bits.Exponent + 63 - BitOperations.LeadingZeroCount(bits.Significand);
        var top = FloorDivide(highest, width);
        var bottom = FloorDivide(lowest, width);
        var count = 0;
        for (var position = top; position >= bottom; position--)
        {
            var shift = (position * width) - bits.Exponent;
            var digit = shift >= 0 ? bits.Significand >> shift : bits.Significand << -shift;
            digits[count++] = Alphabet[(int)(digit & (ulong)(radix - 1))];
        }

        integerDigits = top + 1;
        return count;
    }

    /// <summary>Steele and White's free-format generation, in exact integer arithmetic.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.6.1.20; IP=Low; Security=Medium; Resources=2; Fingerprint=575996
    // Broiler-Human:        PENDING
    private static int FreeFormat(double value, int radix, Span<char> digits, out int integerDigits)
    {
        var bits = DoubleBits.Of(value);
        var b = (uint)radix;
        Span<uint> rStorage = stackalloc uint[BigNumber.Capacity];
        Span<uint> sStorage = stackalloc uint[BigNumber.Capacity];
        Span<uint> upStorage = stackalloc uint[BigNumber.Capacity];
        Span<uint> downStorage = stackalloc uint[BigNumber.Capacity];
        var r = new BigNumber(rStorage);
        var s = new BigNumber(sStorage);
        var up = new BigNumber(upStorage);
        var down = new BigNumber(downStorage);

        // value = r / s; the rounding interval reaches up / s above it and down / s below it.
        var lowerHalf = bits.LowerGapIsHalf ? 1 : 0;
        r.Set(bits.Significand);
        s.Set(1);
        up.Set(1);
        down.Set(1);
        if (bits.Exponent >= 0)
        {
            r.ShiftLeft(bits.Exponent + 1 + lowerHalf);
            s.ShiftLeft(1 + lowerHalf);
            up.ShiftLeft(bits.Exponent + lowerHalf);
            down.ShiftLeft(bits.Exponent);
        }
        else
        {
            r.ShiftLeft(1 + lowerHalf);
            s.ShiftLeft(1 - bits.Exponent + lowerHalf);
            up.ShiftLeft(lowerHalf);
        }

        // Scale so that the interval's top lies just below one (or at it, when the end belongs).
        var exponent = (int)Math.Ceiling((Math.Log(value) / Math.Log(radix)) - 1e-10);
        if (exponent >= 0)
        {
            s.MultiplyPower(b, exponent);
        }
        else
        {
            r.MultiplyPower(b, -exponent);
            up.MultiplyPower(b, -exponent);
            down.MultiplyPower(b, -exponent);
        }

        var inclusive = bits.IsEven;
        while (inclusive ? BigNumber.CompareSum(r, up, s) >= 0 : BigNumber.CompareSum(r, up, s) > 0)
        {
            s.Multiply(b);
            exponent++;
        }

        var count = 0;
        var parity = 0U;
        while (true)
        {
            r.Multiply(b);
            up.Multiply(b);
            down.Multiply(b);
            var digit = r.DivideRemainder(s);
            var low = inclusive ? BigNumber.Compare(r, down) <= 0 : BigNumber.Compare(r, down) < 0;
            var high = inclusive ? BigNumber.CompareSum(r, up, s) >= 0 : BigNumber.CompareSum(r, up, s) > 0;
            if (!low && !high)
            {
                digits[count++] = Alphabet[(int)digit];
                parity = ((parity * b) + digit) & 1;
                continue;
            }

            if (low && high)
            {
                // Both last digits read back as the value: the nearer, and of two equally near the
                // one that leaves the significand even.
                var twice = BigNumber.CompareSum(r, r, s);
                if (twice > 0 || (twice == 0 && (((parity * b) + digit) & 1) != 0))
                    digit++;
            }
            else if (high)
            {
                digit++;
            }

            count = Append(digits, count, digit, b, ref exponent);
            break;
        }

        return Finish(digits, count, exponent, out integerDigits);
    }

    /// <summary>
    /// The same generation as <see cref="FreeFormat"/>, step for step, in 128-bit integers: taken
    /// when every quantity it scales stays below 2^127, which a value of moderate magnitude in any
    /// radix meets. The bound is checked before anything is computed, from the bit lengths the
    /// scaling gives, so the path never overflows; when it does not hold, the caller uses the
    /// big-number path.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.6.1.20; IP=Low; Security=Medium; Resources=2; Fingerprint=61CB04
    // Broiler-Human:        PENDING
    private static bool TryFreeFormatNarrow(double value, int radix, Span<char> digits, out int count, out int integerDigits)
    {
        var bits = DoubleBits.Of(value);
        var exponent = (int)Math.Ceiling((Math.Log(value) / Math.Log(radix)) - 1e-10);
        var lowerHalf = bits.LowerGapIsHalf ? 1 : 0;

        // s's and r's bit lengths after the scaling below, before the fix-up; the fix-up and the
        // generation each multiply by the radix once more, and a sum of two needs one bit.
        var radixBits = 64 - BitOperations.LeadingZeroCount((ulong)radix);
        var sBits = (bits.Exponent >= 0 ? 1 + lowerHalf : 1 - bits.Exponent + lowerHalf) + (Math.Max(exponent, 0) * radixBits);
        var rBits = 53 + (bits.Exponent >= 0 ? bits.Exponent + 1 + lowerHalf : 1 + lowerHalf) + (Math.Max(-exponent, 0) * radixBits);
        if (Math.Max(sBits, rBits) + (3 * radixBits) + 2 > NarrowBits)
        {
            count = 0;
            integerDigits = 0;
            return false;
        }

        UInt128 b = (uint)radix;
        UInt128 r = bits.Significand;
        UInt128 s = 1;
        UInt128 up = 1;
        UInt128 down = 1;
        if (bits.Exponent >= 0)
        {
            r <<= bits.Exponent + 1 + lowerHalf;
            s <<= 1 + lowerHalf;
            up <<= bits.Exponent + lowerHalf;
            down <<= bits.Exponent;
        }
        else
        {
            r <<= 1 + lowerHalf;
            s <<= 1 - bits.Exponent + lowerHalf;
            up <<= lowerHalf;
        }

        UInt128 scale = 1;
        for (var step = Math.Abs(exponent); step > 0; step--)
            scale *= b;

        if (exponent >= 0)
        {
            s *= scale;
        }
        else
        {
            r *= scale;
            up *= scale;
            down *= scale;
        }

        var inclusive = bits.IsEven;
        while (inclusive ? r + up >= s : r + up > s)
        {
            s *= b;
            exponent++;
        }

        count = 0;
        var parity = 0U;
        while (true)
        {
            r *= b;
            up *= b;
            down *= b;
            var digit = Divide(ref r, s);
            var low = inclusive ? r <= down : r < down;
            var high = inclusive ? r + up >= s : r + up > s;
            if (!low && !high)
            {
                digits[count++] = Alphabet[(int)digit];
                parity = ((parity * (uint)radix) + digit) & 1;
                continue;
            }

            if (low && high)
            {
                var twice = r + r;
                if (twice > s || (twice == s && (((parity * (uint)radix) + digit) & 1) != 0))
                    digit++;
            }
            else if (high)
            {
                digit++;
            }

            count = Append(digits, count, digit, (uint)radix, ref exponent);
            break;
        }

        count = Finish(digits, count, exponent, out integerDigits);
        return true;
    }

    /// <summary>
    /// The quotient of <paramref name="r"/> by <paramref name="s"/>, below 37 here, leaving the
    /// remainder in <paramref name="r"/>: estimated in floating point and corrected exactly.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.6.1.20; IP=Low; Security=Medium; Resources=1; Fingerprint=B307BC
    // Broiler-Human:        PENDING
    private static uint Divide(ref UInt128 r, UInt128 s)
    {
        var quotient = (uint)((double)r / (double)s);
        var product = s * quotient;
        while (product > r)
        {
            quotient--;
            product -= s;
        }

        r -= product;
        while (r >= s)
        {
            quotient++;
            r -= s;
        }

        return quotient;
    }

    /// <summary>The digits generation leaves, without the leading and trailing zeros the specification's s never has.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.6.1.20; IP=Low; Security=Medium; Resources=1; Fingerprint=F75CBC
    // Broiler-Human:        PENDING
    private static int Finish(Span<char> digits, int count, int exponent, out int integerDigits)
    {
        // An estimate one too large leaves leading zero digits, which carry no value.
        var leading = 0;
        while (leading < count - 1 && digits[leading] == '0')
            leading++;

        if (leading != 0)
        {
            digits[leading..count].CopyTo(digits);
            count -= leading;
            exponent -= leading;
        }

        // A carry can leave trailing zeros, which the specification's s never has.
        while (count > 1 && digits[count - 1] == '0')
            count--;

        integerDigits = exponent;
        return count;
    }

    /// <summary>Appends the last digit, carrying into the digits before it when it reaches the radix.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.6.1.20; IP=Low; Security=Medium; Resources=2; Fingerprint=474655
    // Broiler-Human:        PENDING
    private static int Append(Span<char> digits, int count, uint digit, uint radix, ref int exponent)
    {
        if (digit < radix)
        {
            digits[count] = Alphabet[(int)digit];
            return count + 1;
        }

        // The last digit rounded up to the radix: zero here, and one more in the place before.
        var at = count - 1;
        while (at >= 0 && digits[at] == Alphabet[(int)radix - 1])
        {
            digits[at] = '0';
            at--;
        }

        if (at >= 0)
        {
            digits[at] = Alphabet[Alphabet.IndexOf(digits[at], StringComparison.Ordinal) + 1];
            return count;
        }

        digits[..count].CopyTo(digits[1..]);
        digits[0] = '1';
        exponent++;
        return count + 1;
    }

    /// <summary>Steps 6 and 7 of <c>Number::toString</c>: the digits placed positionally, without an exponent.</summary>
    /// <param name="digits">The significand's k digits.</param>
    /// <param name="integerDigits">The specification's n: how many places the point is after the first digit.</param>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.6.1.20; IP=Low; Security=Medium; Resources=2; Fingerprint=EB523B
    // Broiler-Human:        PENDING
    internal static string Positional(ReadOnlySpan<char> digits, int integerDigits)
    {
        var k = digits.Length;
        var n = integerDigits;
        if (n >= k)
        {
            var length = n;
            return string.Create(length, (Digits: digits.ToString(), k), static (span, state) =>
            {
                state.Digits.AsSpan().CopyTo(span);
                span[state.k..].Fill('0');
            });
        }

        if (n > 0)
            return string.Concat(digits[..n], ".", digits[n..]);

        return string.Create(2 - n + k, (Digits: digits.ToString(), Zeros: -n), static (span, state) =>
        {
            span[0] = '0';
            span[1] = '.';
            span.Slice(2, state.Zeros).Fill('0');
            state.Digits.AsSpan().CopyTo(span[(2 + state.Zeros)..]);
        });
    }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.6.1.20; IP=Low; Security=Medium; Resources=2; Fingerprint=ECEE4C
    // Broiler-Human:        PENDING
    private static int FloorDivide(int value, int divisor) =>
        value >= 0 ? value / divisor : -((-value + divisor - 1) / divisor);
}
