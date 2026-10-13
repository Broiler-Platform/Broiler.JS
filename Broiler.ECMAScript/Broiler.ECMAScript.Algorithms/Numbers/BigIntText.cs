// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   17
// Annotated:        17/17
// Exempt:           0
// Human-reviewed:   0/17
// IP risk:          Low
// Security risk:    High
// Criteria:         9/9
// Resource impact:  3/10 max
// Unverified:       17
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace Broiler.ECMAScript.Numbers;

/// <summary>
/// A BigInt's text: <c>BigInt::toString</c> at every radix (ECMA-262 6.1.6.2.23) and
/// <c>StringToBigInt</c> (7.1.14), metered as <see cref="BigIntMath"/> is and culture-invariant by
/// construction.
/// </summary>
/// <remarks>
/// <para>
/// <b>Both directions are divide and conquer</b>, as Broiler.VM's <c>JsBigInt</c> had them: a value is
/// split by squared powers of the radix, and leaves of at most 4,096 bits (text) or 1,024 digits
/// (parse) are handed to the base class library with the invariant number format. Every division,
/// join and leaf is charged before it runs, in that class's units, so a long conversion is paid for and
/// its meter is where a caller sees cancellation.
/// </para>
/// <para>
/// <b>No culture reaches the text</b>: a negative value is written with U+002D, whatever the process's
/// culture, which corrects Broiler.JS's radix-10 text (JSD-0067, X2).
/// </para>
/// </remarks>
// Broiler-AI:           Origin=Ported; Spec=ECMA-262 6.1.6.2.23 via Broiler.VM JsBigInt; IP=Low; Security=High; Resources=3; Fingerprint=07A48D
// Broiler-Falsified-If: a conversion's text depends on the process's culture, or a conversion's work runs before it is charged
// Broiler-Human:        PENDING
public static class BigIntText
{
    /// <summary>The widest part the conversion to text hands to the base class library whole: 4,096 bits.</summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 6.1.6.2.23 via Broiler.VM JsBigInt; IP=Low; Security=Medium; Resources=1; Fingerprint=3FB93F
    // Broiler-Human:        PENDING
    private const long FormatLeafBits = 4096;

    /// <summary>The widest leaf the decimal parse hands to the base class library whole: 1,024 digits.</summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 7.1.14 via Broiler.VM JsBigInt; IP=Low; Security=Medium; Resources=1; Fingerprint=CEDA47
    // Broiler-Human:        PENDING
    private const int ParseLeafDigits = 1024;

    /// <summary>The thirty-six digits, lower case, as <c>BigInt.prototype.toString</c> writes them.</summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 6.1.6.2.23 via Broiler.VM JsBigInt; IP=Low; Security=Low; Resources=1; Fingerprint=EDCCA7
    // Broiler-Human:        PENDING
    private const string RadixDigits = "0123456789abcdefghijklmnopqrstuvwxyz";

    /// <summary>
    /// <c>BigInt::toString(x, radix)</c> (ECMA-262 6.1.6.2.23): an optional <c>-</c>, then the digits
    /// in lower case, and no <c>n</c>.
    /// </summary>
    /// <param name="value">The BigInt.</param>
    /// <param name="radix">The radix, from 2 to 36.</param>
    /// <param name="charge">The meter, or <see langword="null"/>.</param>
    /// <returns>The text.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The radix is outside 2 to 36; the engine raises the <c>RangeError</c> first.</exception>
    /// <remarks>
    /// The digit bound is charged first, linearly. A radix that is a power of two reads the bits in one
    /// pass; any other is divide and conquer.
    /// </remarks>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 6.1.6.2.23 via Broiler.VM JsBigInt; IP=Low; Security=High; Resources=3; Fingerprint=040A0D
    // Broiler-Falsified-If: a wider BigInt is converted for the same charge as a narrower one, or the digits differ from the integer in that radix
    // Broiler-Human:        PENDING
    public static string ToString(BigInteger value, int radix, WorkCharge? charge)
    {
        if (radix is < 2 or > 36)
            throw new ArgumentOutOfRangeException(nameof(radix), "the radix is outside 2 to 36");

        if (radix == 10)
            return ToDecimalString(value, charge);

        var bits = value.GetBitLength();
        var digitBits = Math.Log2(radix);
        var bound = (int)(bits / digitBits) + 2;
        BigIntMath.Charge(charge, BigIntMath.LinearCost(bound));
        if (value.IsZero)
            return "0";

        var negative = value.Sign < 0;
        var magnitude = negative ? -value : value;
        var text = new StringBuilder(bound);
        if (negative)
            text.Append('-');

        if ((radix & (radix - 1)) == 0)
        {
            AppendPowerOfTwoRadix(text, magnitude, BitOperations.Log2((uint)radix));
            return text.ToString();
        }

        // The chunk is the largest power of the radix below 2^63, so a leaf's word-at-a-time loop never
        // overflows, and it is the first of the powers the split squares.
        var chunkDigits = (int)(63 / digitBits);
        var chunk = BigInteger.Pow(radix, chunkDigits);
        var powers = new List<BigInteger> { chunk };
        var widths = new List<int> { chunkDigits };
        while (((powers[^1].GetBitLength() * 2) - 1) * 2 <= magnitude.GetBitLength() + 1)
        {
            var words = BigIntMath.WordsOf(powers[^1].GetBitLength());
            BigIntMath.Charge(charge, BigIntMath.ProductCost(words, words));
            powers.Add(powers[^1] * powers[^1]);
            widths.Add(widths[^1] * 2);
        }

        AppendRadix(text, magnitude, 0, powers.Count - 1, radix, (ulong)chunk, chunkDigits, powers, widths, charge);
        return text.ToString();
    }

    /// <summary>
    /// <c>StringToBigInt</c> (ECMA-262 7.1.14): the value of a <c>StringIntegerLiteral</c>, its white
    /// space and line terminators trimmed; an empty text is <c>0n</c>; a decimal integer may have a sign
    /// and leading zeros; a <c>0x</c>, <c>0o</c> or <c>0b</c> integer may have neither sign nor
    /// separator; and there is no <c>n</c>, fraction or exponent.
    /// </summary>
    /// <param name="text">The String.</param>
    /// <param name="policy">The width policy.</param>
    /// <param name="charge">The meter, or <see langword="null"/>.</param>
    /// <param name="value">The value, when the status is <see cref="ResultStatus.Completed"/>.</param>
    /// <param name="tooWideSign">The value's sign, 1 or -1, when the status is <see cref="ResultStatus.LimitExceeded"/>.</param>
    /// <returns>
    /// <see cref="ResultStatus.Completed"/>; <see cref="ResultStatus.Malformed"/> where the clause answers
    /// <c>undefined</c>; or <see cref="ResultStatus.LimitExceeded"/>, with the sign, for a value past the
    /// policy - reported, not refused, because <c>BigInt(text)</c> raises, <c>==</c> answers
    /// <see langword="false"/> and a relational comparison answers by the sign.
    /// </returns>
    /// <remarks>
    /// The text is charged for its length before it is read; a decimal conversion is then charged leaf
    /// by leaf and join by join, and the radix-prefixed forms are one pass. A decimal text of more
    /// digits than any value within the policy has is known too wide without converting a digit.
    /// </remarks>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 7.1.14 via Broiler.VM JsBigInt; IP=Low; Security=High; Resources=3; Fingerprint=BA8558
    // Broiler-Falsified-If: a text outside StringIntegerLiteral answers a value, a text inside it answers an integer other than its mathematical value, or a value past the policy is constructed
    // Broiler-Human:        PENDING
    public static ResultStatus StringToBigInt(ReadOnlySpan<char> text, BigIntPolicy policy, WorkCharge? charge, out BigInteger value, out int tooWideSign)
    {
        BigIntMath.Charge(charge, BigIntMath.LinearCost(text.Length / 8));
        value = BigInteger.Zero;
        tooWideSign = 0;

        var trimmed = NumberParsing.TrimStrWhiteSpace(text, TrimEnds.Both);
        if (trimmed.Length == 0)
            return ResultStatus.Completed;

        var prefix = (char)(trimmed.Length > 2 && trimmed[0] == '0' ? trimmed[1] | 0x20 : 0);
        if (prefix is 'x' or 'o' or 'b')
        {
            var radix = prefix switch { 'x' => 16, 'o' => 8, _ => 2 };
            return ParseRadix(trimmed[2..], radix, policy, charge, out value, out tooWideSign);
        }

        var negative = trimmed[0] == '-';
        var digits = trimmed[0] is '-' or '+' ? trimmed[1..] : trimmed;
        if (digits.Length == 0)
            return ResultStatus.Malformed;

        foreach (var digit in digits)
        {
            if (digit is < '0' or > '9')
                return ResultStatus.Malformed;
        }

        digits = digits.TrimStart('0');
        if (digits.Length > MaximumDecimalDigits(policy))
        {
            tooWideSign = negative ? -1 : 1;
            return ResultStatus.LimitExceeded;
        }

        if (digits.Length == 0)
            return ResultStatus.Completed;

        var magnitude = ParseDecimal(digits, charge);
        var signed = negative ? -magnitude : magnitude;
        if (!policy.Admits(signed))
        {
            tooWideSign = negative ? -1 : 1;
            return ResultStatus.LimitExceeded;
        }

        value = signed;
        return ResultStatus.Completed;
    }

    /// <summary><c>BigInt::toString(x, 10)</c>, divide and conquer over squared powers of 10^19.</summary>
    /// <param name="value">The BigInt.</param>
    /// <param name="charge">The meter, or <see langword="null"/>.</param>
    /// <returns>The decimal text.</returns>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 6.1.6.2.23 via Broiler.VM JsBigInt; IP=Low; Security=High; Resources=3; Fingerprint=DABCC7
    // Broiler-Falsified-If: the text of a value differs from its invariant decimal digits, or a split or a leaf runs before it is charged
    // Broiler-Human:        PENDING
    private static string ToDecimalString(BigInteger value, WorkCharge? charge)
    {
        var bound = (int)(value.GetBitLength() * 30103L / 100000L) + 2;
        BigIntMath.Charge(charge, BigIntMath.LinearCost(bound));
        if (value.GetBitLength() <= FormatLeafBits)
        {
            BigIntMath.Charge(charge, BigIntMath.LeafCost(BigIntMath.Words(value)));
            return value.ToString(NumberFormatInfo.InvariantInfo);
        }

        var negative = value.Sign < 0;
        var magnitude = negative ? -value : value;
        var text = new StringBuilder(bound);
        if (negative)
            text.Append('-');

        // The powers are 10^19, its square, its square's square and so on, up to about the square root
        // of the value: a split by a power of half the value's width leaves two halves.
        var powers = new List<BigInteger> { new(10_000_000_000_000_000_000UL) };
        var widths = new List<int> { 19 };
        while (((powers[^1].GetBitLength() * 2) - 1) * 2 <= magnitude.GetBitLength() + 1)
        {
            var words = BigIntMath.WordsOf(powers[^1].GetBitLength());
            BigIntMath.Charge(charge, BigIntMath.ProductCost(words, words));
            powers.Add(powers[^1] * powers[^1]);
            widths.Add(widths[^1] * 2);
        }

        AppendDecimal(text, magnitude, 0, powers.Count - 1, powers, widths, charge);
        return text.ToString();
    }

    /// <summary>
    /// Appends the decimal digits of a non-negative value, left-padded with zeros to
    /// <paramref name="width"/> when that is not zero, in which case the value is below the square of
    /// <c>powers[level]</c>.
    /// </summary>
    /// <param name="text">The text being built.</param>
    /// <param name="value">The part.</param>
    /// <param name="width">The padded width, or zero.</param>
    /// <param name="level">The largest power to split by.</param>
    /// <param name="powers">The squared powers.</param>
    /// <param name="widths">Their digit counts.</param>
    /// <param name="charge">The meter, or <see langword="null"/>.</param>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 6.1.6.2.23 via Broiler.VM JsBigInt; IP=Low; Security=High; Resources=3; Fingerprint=2E5194
    // Broiler-Falsified-If: a digit is dropped, duplicated or unpadded where a split leaves a remainder with leading zeros
    // Broiler-Human:        PENDING
    private static void AppendDecimal(StringBuilder text, BigInteger value, int width, int level, List<BigInteger> powers, List<int> widths, WorkCharge? charge)
    {
        var bits = value.GetBitLength();
        while (level >= 0 && width == 0 && powers[level].GetBitLength() * 2 > bits + 1)
            level--;

        if (level < 0 || bits <= FormatLeafBits)
        {
            BigIntMath.Charge(charge, BigIntMath.LeafCost(BigIntMath.WordsOf(bits)));
            var leaf = value.ToString(NumberFormatInfo.InvariantInfo);
            if (leaf.Length < width)
                text.Append('0', width - leaf.Length);

            text.Append(leaf);
            return;
        }

        BigIntMath.Charge(charge, BigIntMath.ProductCost(BigIntMath.WordsOf(bits), BigIntMath.WordsOf(powers[level].GetBitLength())));
        var quotient = BigInteger.DivRem(value, powers[level], out var remainder);
        if (width == 0)
            AppendDecimal(text, quotient, 0, level, powers, widths, charge);
        else
            AppendDecimal(text, quotient, width - widths[level], level - 1, powers, widths, charge);

        AppendDecimal(text, remainder, widths[level], level - 1, powers, widths, charge);
    }

    /// <summary>The digits of a non-zero magnitude in a radix of 2^<paramref name="bitsPerDigit"/>, most significant first.</summary>
    /// <param name="text">The text being built.</param>
    /// <param name="magnitude">The magnitude.</param>
    /// <param name="bitsPerDigit">The bits a digit holds.</param>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 6.1.6.2.23 via Broiler.VM JsBigInt; IP=Low; Security=Medium; Resources=2; Fingerprint=A576C9
    // Broiler-Human:        PENDING
    private static void AppendPowerOfTwoRadix(StringBuilder text, BigInteger magnitude, int bitsPerDigit)
    {
        var bytes = magnitude.ToByteArray(isUnsigned: true, isBigEndian: false);
        var width = magnitude.GetBitLength();
        var count = (width + bitsPerDigit - 1) / bitsPerDigit;
        for (var digit = count - 1; digit >= 0; digit--)
        {
            var value = 0;
            for (var within = bitsPerDigit - 1; within >= 0; within--)
            {
                var bit = (digit * bitsPerDigit) + within;
                var set = bit < width && ((bytes[bit / 8] >> (int)(bit % 8)) & 1) != 0;
                value = (value << 1) | (set ? 1 : 0);
            }

            text.Append(RadixDigits[value]);
        }
    }

    /// <summary>
    /// Appends the digits of a non-negative value in <paramref name="radix"/>, left-padded with zeros to
    /// <paramref name="width"/> when that is not zero: <see cref="AppendDecimal"/>'s shape over the
    /// radix's own powers.
    /// </summary>
    /// <param name="text">The text being built.</param>
    /// <param name="value">The part.</param>
    /// <param name="width">The padded width, or zero.</param>
    /// <param name="level">The largest power to split by.</param>
    /// <param name="radix">The radix.</param>
    /// <param name="chunk">The largest power of the radix below 2^63.</param>
    /// <param name="chunkDigits">Its digit count.</param>
    /// <param name="powers">The squared powers.</param>
    /// <param name="widths">Their digit counts.</param>
    /// <param name="charge">The meter, or <see langword="null"/>.</param>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 6.1.6.2.23 via Broiler.VM JsBigInt; IP=Low; Security=High; Resources=3; Fingerprint=D941DC
    // Broiler-Falsified-If: a digit is dropped, duplicated or unpadded where a split leaves a remainder with leading zeros
    // Broiler-Human:        PENDING
    private static void AppendRadix(StringBuilder text, BigInteger value, int width, int level, int radix, ulong chunk,
        int chunkDigits, List<BigInteger> powers, List<int> widths, WorkCharge? charge)
    {
        var bits = value.GetBitLength();
        while (level >= 0 && width == 0 && powers[level].GetBitLength() * 2 > bits + 1)
            level--;

        if (level < 0 || bits <= FormatLeafBits)
        {
            BigIntMath.Charge(charge, BigIntMath.LeafCost(BigIntMath.WordsOf(bits)));
            var leaf = RadixLeaf(value, radix, chunk, chunkDigits);
            if (leaf.Length < width)
                text.Append('0', width - leaf.Length);

            text.Append(leaf);
            return;
        }

        BigIntMath.Charge(charge, BigIntMath.ProductCost(BigIntMath.WordsOf(bits), BigIntMath.WordsOf(powers[level].GetBitLength())));
        var quotient = BigInteger.DivRem(value, powers[level], out var remainder);
        AppendRadix(
            text, quotient, width == 0 ? 0 : width - widths[level], width == 0 ? level : level - 1,
            radix, chunk, chunkDigits, powers, widths, charge);
        AppendRadix(text, remainder, widths[level], level - 1, radix, chunk, chunkDigits, powers, widths, charge);
    }

    /// <summary>A leaf's digits, a word of <paramref name="chunkDigits"/> digits at a time, unpadded.</summary>
    /// <param name="value">The leaf.</param>
    /// <param name="radix">The radix.</param>
    /// <param name="chunk">The largest power of the radix below 2^63.</param>
    /// <param name="chunkDigits">Its digit count.</param>
    /// <returns>The digits.</returns>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 6.1.6.2.23 via Broiler.VM JsBigInt; IP=Low; Security=Medium; Resources=2; Fingerprint=23B890
    // Broiler-Human:        PENDING
    private static string RadixLeaf(BigInteger value, int radix, ulong chunk, int chunkDigits)
    {
        var words = new List<ulong>();
        var divisor = new BigInteger(chunk);
        while (value >= divisor)
        {
            value = BigInteger.DivRem(value, divisor, out var remainder);
            words.Add((ulong)remainder);
        }

        var text = new StringBuilder();
        text.Append(WordDigits((ulong)value, radix, 0));
        for (var at = words.Count - 1; at >= 0; at--)
            text.Append(WordDigits(words[at], radix, chunkDigits));

        return text.ToString();
    }

    /// <summary>One word's digits in <paramref name="radix"/>, left-padded to <paramref name="width"/>.</summary>
    /// <param name="word">The word.</param>
    /// <param name="radix">The radix.</param>
    /// <param name="width">The padded width, or zero.</param>
    /// <returns>The digits.</returns>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 6.1.6.2.23 via Broiler.VM JsBigInt; IP=Low; Security=Low; Resources=1; Fingerprint=6F23B8
    // Broiler-Human:        PENDING
    private static string WordDigits(ulong word, int radix, int width)
    {
        Span<char> buffer = stackalloc char[64];
        var at = buffer.Length;
        do
        {
            buffer[--at] = RadixDigits[(int)(word % (ulong)radix)];
            word /= (ulong)radix;
        }
        while (word != 0);

        while (buffer.Length - at < width)
            buffer[--at] = '0';

        return new string(buffer[at..]);
    }

    /// <summary>
    /// The most decimal digits, without leading zeros, a value within the policy can have:
    /// <c>2 ** MaximumBits</c>'s digit count bound, so a text of more digits is always wider.
    /// </summary>
    /// <param name="policy">The width policy.</param>
    /// <returns>The digit count.</returns>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 7.1.14 via Broiler.VM JsBigInt; IP=Low; Security=Medium; Resources=1; Fingerprint=C85713
    // Broiler-Human:        PENDING
    private static long MaximumDecimalDigits(BigIntPolicy policy) => (policy.MaximumBits * 30103L / 100000L) + 1;

    /// <summary>
    /// A <c>0x</c>, <c>0o</c> or <c>0b</c> text's digits: <see cref="ResultStatus.Malformed"/> for a digit
    /// outside the radix, otherwise the value, or <see cref="ResultStatus.LimitExceeded"/> when it is
    /// wider than the policy. The bits are packed least significant first and converted once.
    /// </summary>
    /// <param name="digits">The digits after the prefix.</param>
    /// <param name="radix">16, 8 or 2.</param>
    /// <param name="policy">The width policy.</param>
    /// <param name="charge">The meter, or <see langword="null"/>.</param>
    /// <param name="value">The value.</param>
    /// <param name="tooWideSign">1 when the value is too wide.</param>
    /// <returns>The status.</returns>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 7.1.14 via Broiler.VM JsBigInt; IP=Low; Security=High; Resources=2; Fingerprint=9CD8E7
    // Broiler-Falsified-If: a digit outside the radix is admitted, or the packed bits spell an integer other than the digits do
    // Broiler-Human:        PENDING
    private static ResultStatus ParseRadix(ReadOnlySpan<char> digits, int radix, BigIntPolicy policy, WorkCharge? charge, out BigInteger value, out int tooWideSign)
    {
        value = BigInteger.Zero;
        tooWideSign = 0;
        var bitsPerDigit = radix switch { 16 => 4, 8 => 3, _ => 1 };
        foreach (var digit in digits)
        {
            if (DigitValue(digit) >= radix)
                return ResultStatus.Malformed;
        }

        digits = digits.TrimStart('0');
        if (((long)digits.Length * bitsPerDigit) > policy.MaximumBits + bitsPerDigit)
        {
            tooWideSign = 1;
            return ResultStatus.LimitExceeded;
        }

        BigIntMath.Charge(charge, BigIntMath.LinearCost(BigIntMath.WordsOf((long)digits.Length * bitsPerDigit)));
        var bytes = new byte[((((long)digits.Length * bitsPerDigit) + 7) / 8) + 1];
        var bit = 0L;
        for (var at = digits.Length - 1; at >= 0; at--)
        {
            var digitValue = DigitValue(digits[at]);
            for (var within = 0; within < bitsPerDigit; within++, bit++)
            {
                if (((digitValue >> within) & 1) != 0)
                    bytes[bit / 8] |= (byte)(1 << (int)(bit % 8));
            }
        }

        var magnitude = new BigInteger(bytes, isUnsigned: true, isBigEndian: false);
        if (!policy.Admits(magnitude))
        {
            tooWideSign = 1;
            return ResultStatus.LimitExceeded;
        }

        value = magnitude;
        return ResultStatus.Completed;
    }

    /// <summary>An ASCII digit's or letter's value, or 36 for any other character.</summary>
    /// <param name="digit">The character.</param>
    /// <returns>0 to 35, or 36.</returns>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 7.1.14 via Broiler.VM JsBigInt; IP=Low; Security=Medium; Resources=1; Fingerprint=31C289
    // Broiler-Human:        PENDING
    private static int DigitValue(char digit) => digit switch
    {
        >= '0' and <= '9' => digit - '0',
        >= 'a' and <= 'z' => digit - 'a' + 10,
        >= 'A' and <= 'Z' => digit - 'A' + 10,
        _ => 36,
    };

    /// <summary>
    /// The integer a run of decimal digits with no leading zero spells, split and joined by squared
    /// powers of 10^1024 with every step charged before it runs.
    /// </summary>
    /// <param name="digits">The digits.</param>
    /// <param name="charge">The meter, or <see langword="null"/>.</param>
    /// <returns>The integer.</returns>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 7.1.14 via Broiler.VM JsBigInt; IP=Low; Security=High; Resources=3; Fingerprint=96C969
    // Broiler-Falsified-If: a digit is dropped or misplaced in a split, or a join is not charged before it runs
    // Broiler-Human:        PENDING
    private static BigInteger ParseDecimal(ReadOnlySpan<char> digits, WorkCharge? charge)
    {
        var powers = new List<BigInteger>();
        var widths = new List<int>();
        if (digits.Length > ParseLeafDigits)
        {
            BigIntMath.Charge(charge, BigIntMath.ProductCost(BigIntMath.WordsOf(ParseLeafDigits * 4L), BigIntMath.WordsOf(ParseLeafDigits * 4L)));
            powers.Add(BigInteger.Pow(10, ParseLeafDigits));
            widths.Add(ParseLeafDigits);
            while ((long)widths[^1] * 2 < digits.Length)
            {
                var words = BigIntMath.WordsOf(powers[^1].GetBitLength());
                BigIntMath.Charge(charge, BigIntMath.ProductCost(words, words));
                powers.Add(powers[^1] * powers[^1]);
                widths.Add(widths[^1] * 2);
            }
        }

        return ParseDecimalPart(digits, powers.Count - 1, powers, widths, charge);
    }

    /// <summary>One part of <see cref="ParseDecimal"/>, which may have leading zeros.</summary>
    /// <param name="digits">The part's digits.</param>
    /// <param name="level">The largest power to split by.</param>
    /// <param name="powers">The squared powers.</param>
    /// <param name="widths">Their digit counts.</param>
    /// <param name="charge">The meter, or <see langword="null"/>.</param>
    /// <returns>The part's integer.</returns>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 7.1.14 via Broiler.VM JsBigInt; IP=Low; Security=High; Resources=3; Fingerprint=2B0663
    // Broiler-Falsified-If: the high part is scaled by a power other than ten to the low part's length
    // Broiler-Human:        PENDING
    private static BigInteger ParseDecimalPart(ReadOnlySpan<char> digits, int level, List<BigInteger> powers, List<int> widths, WorkCharge? charge)
    {
        while (level >= 0 && widths[level] >= digits.Length)
            level--;

        if (level < 0)
        {
            BigIntMath.Charge(charge, BigIntMath.LeafCost(BigIntMath.WordsOf(digits.Length * 4L)));
            return digits.Length == 0
                ? BigInteger.Zero
                : BigInteger.Parse(digits, NumberStyles.None, NumberFormatInfo.InvariantInfo);
        }

        var split = digits.Length - widths[level];
        var high = ParseDecimalPart(digits[..split], level, powers, widths, charge);
        var low = ParseDecimalPart(digits[split..], level - 1, powers, widths, charge);
        BigIntMath.Charge(charge, BigIntMath.ProductCost(BigIntMath.WordsOf(high.GetBitLength()), BigIntMath.WordsOf(powers[level].GetBitLength())));
        return (high * powers[level]) + low;
    }
}
