// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   13
// Annotated:        13/13
// Exempt:           6
// Human-reviewed:   0/13
// IP risk:          Low
// Security risk:    High
// Criteria:         2/2
// Resource impact:  3/10 max
// Unverified:       13
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Globalization;
using System.Numerics;

namespace Broiler.ECMAScript.Numbers;

/// <summary>The kind of value a numeric literal denotes.</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 12.9.3; IP=Low; Security=Medium; Resources=2; Fingerprint=7D78FB
// Broiler-Human:        PENDING
public enum NumericLiteralKind
{
    /// <summary>A Number.</summary>
    Number = 0,

    /// <summary>A BigInt, the literal ending in <c>n</c>.</summary>
    BigInt = 1,
}

/// <summary>Which production of <c>NumericLiteral</c> (ECMA-262 12.9.3) a literal is, which is what strict-mode rules read.</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 12.9.3; IP=Low; Security=Medium; Resources=2; Fingerprint=D83BFE
// Broiler-Human:        PENDING
public enum NumericLiteralForm
{
    /// <summary>A <c>DecimalLiteral</c> or a <c>DecimalBigIntegerLiteral</c>.</summary>
    Decimal = 0,

    /// <summary>A <c>NonDecimalIntegerLiteral</c>, binary, octal or hexadecimal, with or without the BigInt suffix.</summary>
    NonDecimal = 1,

    /// <summary>A <c>LegacyOctalIntegerLiteral</c>, such as <c>017</c>: an early error in strict mode code.</summary>
    LegacyOctal = 2,

    /// <summary>A <c>NonOctalDecimalIntegerLiteral</c>, such as <c>08</c>, with or without a fraction or exponent: an early error in strict mode code.</summary>
    NonOctalDecimal = 3,
}

/// <summary>The value of one numeric literal.</summary>
/// <param name="Kind">Whether it is a Number or a BigInt.</param>
/// <param name="Form">Which production it is.</param>
/// <param name="Number">Its value when it is a Number; NaN otherwise.</param>
/// <param name="BigInt">Its value when it is a BigInt; zero otherwise.</param>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 12.9.3; IP=Low; Security=Medium; Resources=2; Fingerprint=FDAF96
// Broiler-Human:        PENDING
public readonly record struct NumericLiteral(NumericLiteralKind Kind, NumericLiteralForm Form, double Number, BigInteger BigInt);

/// <summary>
/// The values of numeric literals: the static semantics <c>MV</c> and <c>NumericValue</c>
/// (ECMA-262 12.9.3.2 and 12.9.3.3), over the literal's whole source text.
/// </summary>
/// <remarks>
/// <para>
/// <b>Only the value moves here</b> (milestone SJL-3's "Does not do"): each engine's front end still
/// finds where a literal ends, reports its own diagnostics and decides strictness. It hands this
/// class the literal's text exactly as the source spells it - prefix, separators, suffix and all -
/// and gets its value, or a refusal with the position the grammar stops at. A text that is not
/// exactly one <c>NumericLiteral</c> is refused, which is also what a script consisting of it is:
/// <c>3in</c>, <c>01n</c> and <c>1_</c> are each a <c>SyntaxError</c>.
/// </para>
/// <para>
/// A Number is correctly rounded, which is one of the two values <c>RoundMVResult</c> admits beyond
/// twenty significant digits. A BigInt's width is held to the caller's limit before it is built
/// (contract convention 5), so no conversion is asked of a text wider than the caller accepts.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 12.9.3; IP=Low; Security=Medium; Resources=2; Fingerprint=FE521E
// Broiler-Human:        PENDING
public static class NumericLiterals
{
    /// <summary>Evaluates the source text of one numeric literal.</summary>
    /// <param name="text">The literal's source text, nothing before or after it.</param>
    /// <param name="strict">Whether the literal is in strict mode code, where the legacy forms are early errors; a front end that rules on strictness itself passes <see langword="false"/> and reads <see cref="NumericLiteral.Form"/>.</param>
    /// <param name="bigIntBits">The widest BigInt the caller accepts, in bits.</param>
    /// <param name="literal">The literal's value, when the text is one.</param>
    /// <param name="failure">Why it is not, when the result is not <see cref="ResultStatus.Completed"/>.</param>
    /// <param name="charge">The caller's meter: charged the text's length first, and a BigInt's digit count again before it is built; <see langword="null"/> for none.</param>
    /// <returns>
    /// <see cref="ResultStatus.Completed"/>; <see cref="ResultStatus.Malformed"/> when the text is not
    /// exactly one <c>NumericLiteral</c>, or is a legacy form in strict mode code; or
    /// <see cref="ResultStatus.LimitExceeded"/> when a BigInt is wider than <paramref name="bigIntBits"/>.
    /// </returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 12.9.3; IP=Low; Security=High; Resources=3; Fingerprint=0957E2
    // Broiler-Falsified-If: a text that is not exactly one NumericLiteral completes, or a legacy form in strict mode code completes
    // Broiler-Human:        PENDING
    public static ResultStatus Evaluate(ReadOnlySpan<char> text, bool strict, LengthLimit bigIntBits, out NumericLiteral literal, out Failure failure, WorkCharge? charge = null)
    {
        literal = default;
        failure = default;
        if (charge is not null && !text.IsEmpty)
            charge((ulong)text.Length);

        if (text.IsEmpty)
            return Refuse(0, "number.literal-empty", out failure);

        // NonDecimalIntegerLiteral, with or without the BigInt suffix.
        if (text.Length >= 2 && text[0] == '0' && NumberParsing.PrefixRadix(text[1]) is var radix and not 0)
        {
            var end = Separated(text, 2, radix);
            if (end < 0)
                return Refuse(-end, "number.literal-digits", out failure);

            if (end == text.Length)
            {
                var digits = WithoutSeparators(text[2..end], stackalloc char[Math.Min(end, 512)]);
                literal = new NumericLiteral(NumericLiteralKind.Number, NumericLiteralForm.NonDecimal, NumberParsing.NonDecimal(digits, radix), BigInteger.Zero);
                return ResultStatus.Completed;
            }

            if (end == text.Length - 1 && text[end] == 'n')
                return BigInt(text[2..end], radix, NumericLiteralForm.NonDecimal, bigIntBits, charge, out literal, out failure);

            return Refuse(end, "number.literal-trailing", out failure);
        }

        // A leading zero followed by a decimal digit: LegacyOctalIntegerLiteral or
        // NonOctalDecimalIntegerLiteral, neither of which may hold a separator.
        if (text.Length >= 2 && text[0] == '0' && text[1] is >= '0' and <= '9')
        {
            var end = 1;
            var octal = true;
            while (end < text.Length && text[end] is >= '0' and <= '9')
            {
                octal &= text[end] <= '7';
                end++;
            }

            if (octal)
            {
                if (strict)
                    return Refuse(0, "number.legacy-octal-in-strict-code", out failure);

                if (end != text.Length)
                    return Refuse(end, "number.literal-trailing", out failure);

                literal = new NumericLiteral(NumericLiteralKind.Number, NumericLiteralForm.LegacyOctal, NumberParsing.NonDecimal(text[1..], 8), BigInteger.Zero);
                return ResultStatus.Completed;
            }

            if (strict)
                return Refuse(0, "number.non-octal-decimal-in-strict-code", out failure);

            return DecimalTail(text, end, NumericLiteralForm.NonOctalDecimal, allowBigInt: false, bigIntBits, charge, out literal, out failure);
        }

        if (text[0] == '0')
            return DecimalTail(text, 1, NumericLiteralForm.Decimal, allowBigInt: true, bigIntBits, charge, out literal, out failure);

        if (text[0] is >= '1' and <= '9')
        {
            var end = Separated(text, 0, 10);
            if (end < 0)
                return Refuse(-end, "number.literal-digits", out failure);

            return DecimalTail(text, end, NumericLiteralForm.Decimal, allowBigInt: true, bigIntBits, charge, out literal, out failure);
        }

        if (text[0] == '.')
        {
            var end = Separated(text, 1, 10);
            if (end < 0)
                return Refuse(-end, "number.literal-digits", out failure);

            return ExponentTail(text, end, out literal, out failure);
        }

        return Refuse(0, "number.literal-start", out failure);
    }

    /// <summary>After the integer digits of a decimal literal: a fraction, an exponent, the BigInt suffix, or nothing.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 12.9.3; IP=Low; Security=Medium; Resources=2; Fingerprint=B68FCA
    // Broiler-Human:        PENDING
    private static ResultStatus DecimalTail(ReadOnlySpan<char> text, int at, NumericLiteralForm form, bool allowBigInt, LengthLimit bigIntBits, WorkCharge? charge, out NumericLiteral literal, out Failure failure)
    {
        if (at < text.Length && text[at] == 'n')
        {
            if (allowBigInt && at == text.Length - 1)
                return BigInt(text[..at], 10, form, bigIntBits, charge, out literal, out failure);

            literal = default;
            return Refuse(at, "number.bigint-suffix", out failure);
        }

        var end = at;
        if (end < text.Length && text[end] == '.')
        {
            end++;
            if (end < text.Length && text[end] is >= '0' and <= '9')
            {
                end = Separated(text, end, 10);
                if (end < 0)
                {
                    literal = default;
                    return Refuse(-end, "number.literal-digits", out failure);
                }
            }
        }

        var status = ExponentTail(text, end, out literal, out failure);
        if (status == ResultStatus.Completed)
            literal = literal with { Form = form };

        return status;
    }

    /// <summary>An optional exponent and the end of the literal; then the decimal value of all of it.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 12.9.3; IP=Low; Security=Medium; Resources=2; Fingerprint=0B1F4A
    // Broiler-Human:        PENDING
    private static ResultStatus ExponentTail(ReadOnlySpan<char> text, int at, out NumericLiteral literal, out Failure failure)
    {
        literal = default;
        if (at < text.Length && text[at] is 'e' or 'E')
        {
            var digits = at + 1;
            if (digits < text.Length && text[digits] is '+' or '-')
                digits++;

            var end = Separated(text, digits, 10);
            if (end < 0)
                return Refuse(-end, "number.literal-exponent", out failure);

            at = end;
        }

        if (at != text.Length)
            return Refuse(at, "number.literal-trailing", out failure);

        failure = default;
        var clean = text.Contains('_') ? WithoutSeparators(text, text.Length <= 512 ? stackalloc char[text.Length] : new char[text.Length]) : text;
        if (!NumberParsing.TryExactDecimal(clean, out var value))
            value = double.Parse(clean, NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, NumberFormatInfo.InvariantInfo);

        literal = new NumericLiteral(NumericLiteralKind.Number, NumericLiteralForm.Decimal, value, BigInteger.Zero);
        return ResultStatus.Completed;
    }

    /// <summary>A BigInt's value, its width held to the caller's limit before and after it is built.</summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 12.9.3 via Broiler.VM JsParser.TryBigIntValue; IP=Low; Security=High; Resources=3; Fingerprint=E930F3
    // Broiler-Falsified-If: a BigInt literal wider than the caller's limit is converted before it is refused, or one within the limit is refused
    // Broiler-Human:        PENDING
    private static ResultStatus BigInt(ReadOnlySpan<char> digits, int radix, NumericLiteralForm form, LengthLimit bigIntBits, WorkCharge? charge, out NumericLiteral literal, out Failure failure)
    {
        literal = default;
        var buffer = digits.Length <= 512 ? stackalloc char[digits.Length] : new char[digits.Length];
        var clean = WithoutSeparators(digits, buffer).TrimStart('0');

        // A bound from the digit count alone: each digit carries at most log2(radix) bits, and a
        // number of d decimal digits has more than 3.32 x (d - 1) bits.
        var bitsPerDigit = radix switch { 2 => 1, 8 => 3, 16 => 4, _ => 0 };
        var lowerBound = bitsPerDigit != 0 ? ((long)clean.Length - 1) * bitsPerDigit + 1 : ((long)clean.Length - 1) * 3321928L / 1000000L;
        if (!clean.IsEmpty && !bigIntBits.Admits(lowerBound))
            return Refuse(0, "number.bigint-too-wide", ResultStatus.LimitExceeded, out failure);

        if (charge is not null && !clean.IsEmpty)
            charge((ulong)clean.Length);

        BigInteger value;
        if (clean.IsEmpty)
        {
            value = BigInteger.Zero;
        }
        else if (bitsPerDigit != 0)
        {
            // The bits are packed least significant first and converted once, so the cost is linear.
            var bytes = new byte[((clean.Length * bitsPerDigit) + 7) / 8 + 1];
            long bit = 0;
            for (var at = clean.Length - 1; at >= 0; at--)
            {
                var digit = NumberParsing.DigitValue(clean[at]);
                for (var within = 0; within < bitsPerDigit; within++, bit++)
                {
                    if (((digit >> within) & 1) != 0)
                        bytes[bit / 8] |= (byte)(1 << (int)(bit % 8));
                }
            }

            value = new BigInteger(bytes, isUnsigned: true, isBigEndian: false);
        }
        else
        {
            value = BigInteger.Parse(clean, NumberStyles.None, NumberFormatInfo.InvariantInfo);
        }

        if (!bigIntBits.Admits((long)value.GetBitLength()))
            return Refuse(0, "number.bigint-too-wide", ResultStatus.LimitExceeded, out failure);

        failure = default;
        literal = new NumericLiteral(NumericLiteralKind.BigInt, form, double.NaN, value);
        return ResultStatus.Completed;
    }

    /// <summary>
    /// <c>Digits[+Sep]</c> of a radix from <paramref name="at"/>: a digit, then digits each
    /// optionally after one separator. The end, or minus the position of the fault.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 12.9.3; IP=Low; Security=Medium; Resources=2; Fingerprint=C6F08F
    // Broiler-Human:        PENDING
    private static int Separated(ReadOnlySpan<char> text, int at, int radix)
    {
        if (at >= text.Length || !IsDigit(text[at], radix))
            return -at;

        at++;
        while (at < text.Length)
        {
            if (IsDigit(text[at], radix))
            {
                at++;
            }
            else if (text[at] == '_')
            {
                if (at + 1 >= text.Length || !IsDigit(text[at + 1], radix))
                    return -at;

                at += 2;
            }
            else
            {
                break;
            }
        }

        return at;
    }

    /// <summary>Whether a character is a digit of the radix; radix 10, the common one, by one comparison.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 12.9.3; IP=Low; Security=Medium; Resources=1; Fingerprint=6BC25D
    // Broiler-Human:        PENDING
    private static bool IsDigit(char c, int radix) =>
        radix == 10 ? (uint)(c - '0') <= 9 : NumberParsing.DigitValue(c) < radix;

    /// <summary>The text with every separator removed, in <paramref name="buffer"/> when it holds it.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 12.9.3; IP=Low; Security=Medium; Resources=2; Fingerprint=065F05
    // Broiler-Human:        PENDING
    private static ReadOnlySpan<char> WithoutSeparators(ReadOnlySpan<char> text, Span<char> buffer)
    {
        if (text.IndexOf('_') < 0)
            return text;

        var target = buffer.Length >= text.Length ? buffer : new char[text.Length];
        var length = 0;
        foreach (var c in text)
        {
            if (c != '_')
                target[length++] = c;
        }

        return target[..length];
    }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 12.9.3; IP=Low; Security=Medium; Resources=2; Fingerprint=210152
    // Broiler-Human:        PENDING
    private static ResultStatus Refuse(int position, string messageId, out Failure failure) =>
        Refuse(position, messageId, ResultStatus.Malformed, out failure);

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 12.9.3; IP=Low; Security=Medium; Resources=2; Fingerprint=D26D68
    // Broiler-Human:        PENDING
    private static ResultStatus Refuse(int position, string messageId, ResultStatus status, out Failure failure)
    {
        failure = new Failure(status, position, messageId);
        return status;
    }
}
