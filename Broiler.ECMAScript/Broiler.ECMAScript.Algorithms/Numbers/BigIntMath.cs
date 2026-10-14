// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   27
// Annotated:        27/27
// Exempt:           1
// Human-reviewed:   0/27
// IP risk:          Low
// Security risk:    High
// Criteria:         17/17
// Resource impact:  3/10 max
// Unverified:       27
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Numerics;

namespace Broiler.ECMAScript.Numbers;

/// <summary>
/// The BigInt kernel: the <c>BigInt::</c> operations of ECMA-262 6.1.6.2, <c>BigInt.asIntN</c> and
/// <c>BigInt.asUintN</c> (21.2.2.1 and 21.2.2.2), and a BigInt's conversion to and comparison with a
/// Number, each under a width policy and a meter. Its text conversions are <see cref="BigIntText"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every operation charges before it computes and answers <see cref="ResultStatus.LimitExceeded"/>
/// rather than a value past the policy</b>; an operation whose operands' widths already prove its
/// result too wide refuses before it allocates. This is Broiler.VM's <c>JsBigInt</c>, whose charges
/// and refusals are kept unit for unit (JSD-0067 decision 3): its fuel-parity twins and exhaustion
/// verdicts depend on them.
/// </para>
/// <para>
/// <b>The charges are in Broiler.VM's units</b>: a value's size is its width in 64-bit words, plus
/// one (<see cref="WordsOf"/>); a pass over it costs <see cref="LinearCost"/>, a multiplication or a
/// division <see cref="ProductCost"/>, and a text leaf <see cref="LeafCost"/>. A caller with no budget
/// passes no meter.
/// </para>
/// <para>
/// <b>The engine raises the specification's errors</b>: mixing a BigInt with a Number, a zero
/// divisor and a negative exponent are refused before the call, and an over-width status becomes the
/// engine's <c>RangeError</c>.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=Ported; Spec=ECMA-262 6.1.6.2 via Broiler.VM JsBigInt; IP=Low; Security=High; Resources=3; Fingerprint=CC2B29
// Broiler-Falsified-If: an operation builds a value wider than the policy before refusing it, or charges other units than Broiler.VM's JsBigInt charges for the same operands
// Broiler-Human:        PENDING
public static class BigIntMath
{
    /// <summary><c>BigInt::add</c> (ECMA-262 6.1.6.2.7).</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <param name="policy">The width policy.</param>
    /// <param name="charge">The meter, or <see langword="null"/>.</param>
    /// <param name="result">The sum, when the status is <see cref="ResultStatus.Completed"/>.</param>
    /// <returns><see cref="ResultStatus.Completed"/>, or <see cref="ResultStatus.LimitExceeded"/> past the policy.</returns>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 6.1.6.2.7 via Broiler.VM JsBigInt; IP=Low; Security=Medium; Resources=2; Fingerprint=758061
    // Broiler-Human:        PENDING
    public static ResultStatus Add(BigInteger left, BigInteger right, BigIntPolicy policy, WorkCharge? charge, out BigInteger result)
    {
        Charge(charge, LinearCost(Math.Max(Words(left), Words(right))));
        return Bounded(left + right, policy, out result);
    }

    /// <summary><c>BigInt::subtract</c> (ECMA-262 6.1.6.2.8).</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <param name="policy">The width policy.</param>
    /// <param name="charge">The meter, or <see langword="null"/>.</param>
    /// <param name="result">The difference, when the status is <see cref="ResultStatus.Completed"/>.</param>
    /// <returns><see cref="ResultStatus.Completed"/>, or <see cref="ResultStatus.LimitExceeded"/> past the policy.</returns>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 6.1.6.2.8 via Broiler.VM JsBigInt; IP=Low; Security=Medium; Resources=2; Fingerprint=EFF29F
    // Broiler-Human:        PENDING
    public static ResultStatus Subtract(BigInteger left, BigInteger right, BigIntPolicy policy, WorkCharge? charge, out BigInteger result)
    {
        Charge(charge, LinearCost(Math.Max(Words(left), Words(right))));
        return Bounded(left - right, policy, out result);
    }

    /// <summary><c>BigInt::unaryMinus</c> (ECMA-262 6.1.6.2.1); <c>-0n</c> is <c>0n</c>, since there is one zero.</summary>
    /// <param name="value">The operand.</param>
    /// <param name="policy">The width policy.</param>
    /// <param name="charge">The meter, or <see langword="null"/>.</param>
    /// <param name="result">The negation, when the status is <see cref="ResultStatus.Completed"/>.</param>
    /// <returns><see cref="ResultStatus.Completed"/>, or <see cref="ResultStatus.LimitExceeded"/> past the policy.</returns>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 6.1.6.2.1 via Broiler.VM JsBigInt; IP=Low; Security=Medium; Resources=2; Fingerprint=D4376F
    // Broiler-Human:        PENDING
    public static ResultStatus Negate(BigInteger value, BigIntPolicy policy, WorkCharge? charge, out BigInteger result)
    {
        Charge(charge, LinearCost(Words(value)));
        return Bounded(-value, policy, out result);
    }

    /// <summary><c>BigInt::bitwiseNOT</c> (ECMA-262 6.1.6.2.2): <c>-x - 1</c>.</summary>
    /// <param name="value">The operand.</param>
    /// <param name="policy">The width policy.</param>
    /// <param name="charge">The meter, or <see langword="null"/>.</param>
    /// <param name="result">The complement, when the status is <see cref="ResultStatus.Completed"/>.</param>
    /// <returns><see cref="ResultStatus.Completed"/>, or <see cref="ResultStatus.LimitExceeded"/> past the policy.</returns>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 6.1.6.2.2 via Broiler.VM JsBigInt; IP=Low; Security=Medium; Resources=2; Fingerprint=C0F8D7
    // Broiler-Human:        PENDING
    public static ResultStatus BitwiseNot(BigInteger value, BigIntPolicy policy, WorkCharge? charge, out BigInteger result)
    {
        Charge(charge, LinearCost(Words(value)));
        return Bounded(-value - BigInteger.One, policy, out result);
    }

    /// <summary><c>BigInt::multiply</c> (ECMA-262 6.1.6.2.4), refused before it allocates when the operands' widths prove the product too wide.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <param name="policy">The width policy.</param>
    /// <param name="charge">The meter, or <see langword="null"/>.</param>
    /// <param name="result">The product, when the status is <see cref="ResultStatus.Completed"/>.</param>
    /// <returns><see cref="ResultStatus.Completed"/>, or <see cref="ResultStatus.LimitExceeded"/> past the policy.</returns>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 6.1.6.2.4 via Broiler.VM JsBigInt; IP=Low; Security=High; Resources=3; Fingerprint=672BB1
    // Broiler-Falsified-If: a product whose operands already prove it wider than the policy is computed, or a product is charged other than Broiler.VM's JsBigInt charges it
    // Broiler-Human:        PENDING
    public static ResultStatus Multiply(BigInteger left, BigInteger right, BigIntPolicy policy, WorkCharge? charge, out BigInteger result)
    {
        if (left.IsZero || right.IsZero)
        {
            Charge(charge, 1);
            result = BigInteger.Zero;
            return ResultStatus.Completed;
        }

        // |x| is at least 2^(width - 1), so the product is at least 2^(a + b - 2) in magnitude.
        if (left.GetBitLength() + right.GetBitLength() - 2 > policy.MaximumBits)
        {
            Charge(charge, LinearCost(Math.Max(Words(left), Words(right))));
            return Refused(out result);
        }

        Charge(charge, ProductCost(Words(left), Words(right)));
        return Bounded(left * right, policy, out result);
    }

    /// <summary><c>BigInt::divide</c> (ECMA-262 6.1.6.2.5), truncating toward zero.</summary>
    /// <param name="left">The dividend.</param>
    /// <param name="right">The divisor; not zero.</param>
    /// <param name="policy">The width policy.</param>
    /// <param name="charge">The meter, or <see langword="null"/>.</param>
    /// <param name="result">The quotient, when the status is <see cref="ResultStatus.Completed"/>.</param>
    /// <returns><see cref="ResultStatus.Completed"/>, or <see cref="ResultStatus.LimitExceeded"/> past the policy.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The divisor is zero; the engine raises the <c>RangeError</c> first.</exception>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 6.1.6.2.5 via Broiler.VM JsBigInt; IP=Low; Security=High; Resources=3; Fingerprint=2597AE
    // Broiler-Falsified-If: a quotient is not truncated toward zero, or a zero divisor reaches the division
    // Broiler-Human:        PENDING
    public static ResultStatus Divide(BigInteger left, BigInteger right, BigIntPolicy policy, WorkCharge? charge, out BigInteger result)
    {
        if (right.IsZero)
            throw new ArgumentOutOfRangeException(nameof(right), "the divisor is zero");

        Charge(charge, ProductCost(Words(left), Words(right)));
        return Bounded(BigInteger.Divide(left, right), policy, out result);
    }

    /// <summary><c>BigInt::remainder</c> (ECMA-262 6.1.6.2.6), whose sign is the dividend's.</summary>
    /// <param name="left">The dividend.</param>
    /// <param name="right">The divisor; not zero.</param>
    /// <param name="policy">The width policy.</param>
    /// <param name="charge">The meter, or <see langword="null"/>.</param>
    /// <param name="result">The remainder, when the status is <see cref="ResultStatus.Completed"/>.</param>
    /// <returns><see cref="ResultStatus.Completed"/>, or <see cref="ResultStatus.LimitExceeded"/> past the policy.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The divisor is zero; the engine raises the <c>RangeError</c> first.</exception>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 6.1.6.2.6 via Broiler.VM JsBigInt; IP=Low; Security=High; Resources=3; Fingerprint=6B4557
    // Broiler-Falsified-If: a remainder's sign is not the dividend's, or a zero divisor reaches the division
    // Broiler-Human:        PENDING
    public static ResultStatus Remainder(BigInteger left, BigInteger right, BigIntPolicy policy, WorkCharge? charge, out BigInteger result)
    {
        if (right.IsZero)
            throw new ArgumentOutOfRangeException(nameof(right), "the divisor is zero");

        Charge(charge, ProductCost(Words(left), Words(right)));
        return Bounded(BigInteger.Remainder(left, right), policy, out result);
    }

    /// <summary>
    /// <c>BigInt::exponentiate</c> (ECMA-262 6.1.6.2.3) for a non-negative exponent, by squaring, every
    /// multiplication charged before it runs and a result past the policy refused as soon as that is
    /// certain.
    /// </summary>
    /// <param name="base">The base.</param>
    /// <param name="exponent">The exponent; not negative.</param>
    /// <param name="policy">The width policy.</param>
    /// <param name="charge">The meter, or <see langword="null"/>.</param>
    /// <param name="result">The power, when the status is <see cref="ResultStatus.Completed"/>.</param>
    /// <returns><see cref="ResultStatus.Completed"/>, or <see cref="ResultStatus.LimitExceeded"/> past the policy.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The exponent is negative; the engine raises the <c>RangeError</c> first.</exception>
    /// <remarks>
    /// <c>0n ** 0n</c> is <c>1n</c>. A base of <c>0n</c>, <c>1n</c> or <c>-1n</c> answers at once for any
    /// exponent; any other base with an exponent above the policy's width is refused without a
    /// multiplication, since its result is at least <c>2 ** exponent</c>.
    /// </remarks>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 6.1.6.2.3 via Broiler.VM JsBigInt; IP=Low; Security=High; Resources=3; Fingerprint=DFE64E
    // Broiler-Falsified-If: a power is computed without charging each multiplication, or a result past the policy is answered, or a huge exponent of a trivial base is refused
    // Broiler-Human:        PENDING
    public static ResultStatus Power(BigInteger @base, BigInteger exponent, BigIntPolicy policy, WorkCharge? charge, out BigInteger result)
    {
        if (exponent.Sign < 0)
            throw new ArgumentOutOfRangeException(nameof(exponent), "the exponent is negative");

        Charge(charge, LinearCost(Math.Max(Words(@base), Words(exponent))));
        result = BigInteger.One;
        if (exponent.IsZero)
            return ResultStatus.Completed;

        if (@base.IsZero || @base.IsOne)
        {
            result = @base;
            return ResultStatus.Completed;
        }

        if (@base == BigInteger.MinusOne)
        {
            result = exponent.IsEven ? BigInteger.One : BigInteger.MinusOne;
            return ResultStatus.Completed;
        }

        // |base| >= 2 from here, so the result is at least 2^exponent and at least
        // 2^(exponent * (width - 1)) in magnitude - one bit wider than that when it is positive, since
        // only a negative power of two is as narrow as its exponent.
        if (exponent > policy.MaximumBits)
            return Refused(out result);

        var positive = @base.Sign > 0 || exponent.IsEven;
        if (((long)exponent * (@base.GetBitLength() - 1)) + (positive ? 1 : 0) > policy.MaximumBits)
            return Refused(out result);

        var remaining = (long)exponent;
        var square = @base;
        while (true)
        {
            if ((remaining & 1) != 0)
            {
                // As for Multiply: operands this wide prove the product too wide before it exists.
                if (result.GetBitLength() + square.GetBitLength() - 2 > policy.MaximumBits)
                    return Refused(out result);

                Charge(charge, ProductCost(WordsOf(result.GetBitLength()), WordsOf(square.GetBitLength())));
                result *= square;
                if (result.GetBitLength() > policy.MaximumBits)
                    return Refused(out result);
            }

            remaining >>= 1;
            if (remaining == 0)
                return ResultStatus.Completed;

            // A square past the policy with an exponent bit still to come is a result past it: the
            // result will be multiplied by at least this square, and every other factor is at least
            // one in magnitude. One the operand's width already proves too wide is not computed.
            if ((2 * square.GetBitLength()) - 2 > policy.MaximumBits)
                return Refused(out result);

            var words = WordsOf(square.GetBitLength());
            Charge(charge, ProductCost(words, words));
            square *= square;
            if (square.GetBitLength() > policy.MaximumBits)
                return Refused(out result);
        }
    }

    /// <summary><c>BigInt::bitwiseAND</c> (ECMA-262 6.1.6.2.18), on two's-complement values of unbounded width.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <param name="policy">The width policy.</param>
    /// <param name="charge">The meter, or <see langword="null"/>.</param>
    /// <param name="result">The conjunction, when the status is <see cref="ResultStatus.Completed"/>.</param>
    /// <returns><see cref="ResultStatus.Completed"/>, or <see cref="ResultStatus.LimitExceeded"/> past the policy.</returns>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 6.1.6.2.18 via Broiler.VM JsBigInt; IP=Low; Security=Medium; Resources=2; Fingerprint=02E913
    // Broiler-Human:        PENDING
    public static ResultStatus BitwiseAnd(BigInteger left, BigInteger right, BigIntPolicy policy, WorkCharge? charge, out BigInteger result)
    {
        Charge(charge, LinearCost(Math.Max(Words(left), Words(right))));
        return Bounded(left & right, policy, out result);
    }

    /// <summary><c>BigInt::bitwiseOR</c> (ECMA-262 6.1.6.2.20), on two's-complement values of unbounded width.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <param name="policy">The width policy.</param>
    /// <param name="charge">The meter, or <see langword="null"/>.</param>
    /// <param name="result">The disjunction, when the status is <see cref="ResultStatus.Completed"/>.</param>
    /// <returns><see cref="ResultStatus.Completed"/>, or <see cref="ResultStatus.LimitExceeded"/> past the policy.</returns>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 6.1.6.2.20 via Broiler.VM JsBigInt; IP=Low; Security=Medium; Resources=2; Fingerprint=B176BA
    // Broiler-Human:        PENDING
    public static ResultStatus BitwiseOr(BigInteger left, BigInteger right, BigIntPolicy policy, WorkCharge? charge, out BigInteger result)
    {
        Charge(charge, LinearCost(Math.Max(Words(left), Words(right))));
        return Bounded(left | right, policy, out result);
    }

    /// <summary><c>BigInt::bitwiseXOR</c> (ECMA-262 6.1.6.2.19), on two's-complement values of unbounded width.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <param name="policy">The width policy.</param>
    /// <param name="charge">The meter, or <see langword="null"/>.</param>
    /// <param name="result">The exclusive disjunction, when the status is <see cref="ResultStatus.Completed"/>.</param>
    /// <returns><see cref="ResultStatus.Completed"/>, or <see cref="ResultStatus.LimitExceeded"/> past the policy.</returns>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 6.1.6.2.19 via Broiler.VM JsBigInt; IP=Low; Security=Medium; Resources=2; Fingerprint=5FF1D2
    // Broiler-Human:        PENDING
    public static ResultStatus BitwiseXor(BigInteger left, BigInteger right, BigIntPolicy policy, WorkCharge? charge, out BigInteger result)
    {
        Charge(charge, LinearCost(Math.Max(Words(left), Words(right))));
        return Bounded(left ^ right, policy, out result);
    }

    /// <summary>
    /// <c>BigInt::leftShift</c> (ECMA-262 6.1.6.2.9): <c>value * 2 ** count</c>, which for a negative
    /// count is a right shift rounding toward negative infinity. A result the widths prove too wide is
    /// refused before it allocates; a right shift past the value's width answers <c>0n</c> or
    /// <c>-1n</c> without shifting, however large the count.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="count">The count, of any size.</param>
    /// <param name="policy">The width policy.</param>
    /// <param name="charge">The meter, or <see langword="null"/>.</param>
    /// <param name="result">The shifted value, when the status is <see cref="ResultStatus.Completed"/>.</param>
    /// <returns><see cref="ResultStatus.Completed"/>, or <see cref="ResultStatus.LimitExceeded"/> past the policy.</returns>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 6.1.6.2.9 via Broiler.VM JsBigInt; IP=Low; Security=High; Resources=2; Fingerprint=E44EA4
    // Broiler-Falsified-If: a left shift allocates a result its widths already prove wider than the policy, a right shift rounds toward zero, or an oversized count reaches the base class library
    // Broiler-Human:        PENDING
    public static ResultStatus LeftShift(BigInteger value, BigInteger count, BigIntPolicy policy, WorkCharge? charge, out BigInteger result) =>
        Shift(value, count, policy, charge, out result);

    /// <summary>
    /// <c>BigInt::signedRightShift</c> (ECMA-262 6.1.6.2.10): a left shift by the negated count,
    /// negated as an integer and not as a BigInt, so a count at the policy's edge needs no second value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="count">The count, of any size.</param>
    /// <param name="policy">The width policy.</param>
    /// <param name="charge">The meter, or <see langword="null"/>.</param>
    /// <param name="result">The shifted value, when the status is <see cref="ResultStatus.Completed"/>.</param>
    /// <returns><see cref="ResultStatus.Completed"/>, or <see cref="ResultStatus.LimitExceeded"/> past the policy.</returns>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 6.1.6.2.10 via Broiler.VM JsBigInt; IP=Low; Security=High; Resources=2; Fingerprint=2A8A8C
    // Broiler-Falsified-If: x >> n differs from the floor of x / 2^n for any count, or a negative count is not a left shift held to the policy
    // Broiler-Human:        PENDING
    public static ResultStatus SignedRightShift(BigInteger value, BigInteger count, BigIntPolicy policy, WorkCharge? charge, out BigInteger result) =>
        Shift(value, -count, policy, charge, out result);

    /// <summary>
    /// <c>BigInt.asIntN</c>'s operation (ECMA-262 21.2.2.1, steps 3 and 4): <paramref name="value"/>
    /// modulo 2^<paramref name="bits"/>, read as a signed integer of that width.
    /// </summary>
    /// <param name="bits">The width, <c>ToIndex</c> of the argument: at most 2^53 - 1.</param>
    /// <param name="value">The value.</param>
    /// <param name="policy">The width policy.</param>
    /// <param name="charge">The meter, or <see langword="null"/>.</param>
    /// <param name="result">The wrapped value, when the status is <see cref="ResultStatus.Completed"/>.</param>
    /// <returns><see cref="ResultStatus.Completed"/>; the result is never wider than the value.</returns>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 21.2.2.1 via Broiler.VM JsBigInt; IP=Low; Security=High; Resources=2; Fingerprint=67F831
    // Broiler-Falsified-If: a value that fits the signed width is changed, or a wrapped value takes the wrong sign
    // Broiler-Human:        PENDING
    public static ResultStatus AsIntN(ulong bits, BigInteger value, BigIntPolicy policy, WorkCharge? charge, out BigInteger result)
    {
        Charge(charge, LinearCost(Words(value)));
        if (bits == 0)
        {
            result = BigInteger.Zero;
            return ResultStatus.Completed;
        }

        // A value fits a signed width when its two's-complement length leaves room for the sign.
        if ((ulong)value.GetBitLength() < bits)
        {
            result = value;
            return ResultStatus.Completed;
        }

        // Here bits <= the value's width, so the modulus is bounded by the value.
        var modulus = BigInteger.One << (int)bits;
        var wrapped = value & (modulus - BigInteger.One);
        if (wrapped >= modulus >> 1)
            wrapped -= modulus;

        return Bounded(wrapped, policy, out result);
    }

    /// <summary>
    /// <c>BigInt.asUintN</c>'s operation (ECMA-262 21.2.2.2, step 3): <paramref name="value"/> modulo
    /// 2^<paramref name="bits"/>.
    /// </summary>
    /// <param name="bits">The width, <c>ToIndex</c> of the argument: at most 2^53 - 1.</param>
    /// <param name="value">The value.</param>
    /// <param name="policy">The width policy.</param>
    /// <param name="charge">The meter, or <see langword="null"/>.</param>
    /// <param name="result">The residue, when the status is <see cref="ResultStatus.Completed"/>.</param>
    /// <returns><see cref="ResultStatus.Completed"/>, or <see cref="ResultStatus.LimitExceeded"/> past the policy.</returns>
    /// <remarks>A negative value modulo a width past the policy is at least <c>2 ** (bits - 1)</c>, so it is refused without allocating.</remarks>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 21.2.2.2 via Broiler.VM JsBigInt; IP=Low; Security=High; Resources=2; Fingerprint=C84AF8
    // Broiler-Falsified-If: a non-negative value that fits the width is changed, a negative value is answered negative, or a modulus past the policy is allocated
    // Broiler-Human:        PENDING
    public static ResultStatus AsUintN(ulong bits, BigInteger value, BigIntPolicy policy, WorkCharge? charge, out BigInteger result)
    {
        Charge(charge, LinearCost(Words(value)));
        if (bits == 0)
        {
            result = BigInteger.Zero;
            return ResultStatus.Completed;
        }

        if (value.Sign >= 0 && (ulong)value.GetBitLength() <= bits)
        {
            result = value;
            return ResultStatus.Completed;
        }

        if (value.Sign < 0 && bits > (ulong)policy.MaximumBits)
            return Refused(out result);

        // Here bits is within the policy, or the value is non-negative and wider than bits.
        Charge(charge, LinearCost(WordsOf((long)bits)));
        var mask = (BigInteger.One << (int)bits) - BigInteger.One;
        return Bounded(value & mask, policy, out result);
    }

    /// <summary>
    /// <c>NumberToBigInt</c> (ECMA-262 21.2.1.1.1) of an integral Number: the exact integer, which a
    /// double always has.
    /// </summary>
    /// <param name="number">An integral, finite Number.</param>
    /// <returns>The integer.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The Number is NaN, infinite or not integral; the engine raises the <c>RangeError</c> first.</exception>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 21.2.1.1.1 via Broiler.VM JsBigInt; IP=Low; Security=High; Resources=1; Fingerprint=F50D3A
    // Broiler-Falsified-If: an integral Number converts to an integer other than its exact value
    // Broiler-Human:        PENDING
    public static BigInteger FromIntegralNumber(double number)
    {
        if (!double.IsFinite(number) || Math.Truncate(number) != number)
            throw new ArgumentOutOfRangeException(nameof(number), "the Number is not an integer");

        return new BigInteger(number);
    }

    /// <summary>
    /// A BigInt's Number, <c>F(R(x))</c> (ECMA-262 21.1.1.1, <c>Number(value)</c> step 2.a): the Number
    /// nearest the integer, ties to even, and an infinity at or past <c>2^1024 - 2^970</c>.
    /// </summary>
    /// <param name="value">The BigInt.</param>
    /// <param name="charge">The meter, or <see langword="null"/>.</param>
    /// <returns>The nearest Number.</returns>
    /// <remarks>
    /// The top 54 bits are taken with a sticky bit for everything below them, rounded once to 53, and
    /// scaled by the power of two they were shifted by; the scaling is exact until it overflows, which
    /// is the infinity the language gives. The base class library's conversion truncates instead.
    /// </remarks>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 21.1.1.1 via Broiler.VM JsBigInt; IP=Low; Security=High; Resources=2; Fingerprint=7C0D46
    // Broiler-Falsified-If: a BigInt converts to a Number other than the nearest one with ties to even, or a value at or past 2 ** 1024 - 2 ** 970 converts to a finite Number
    // Broiler-Human:        PENDING
    public static double ToNumber(BigInteger value, WorkCharge? charge)
    {
        Charge(charge, LinearCost(Words(value)));
        if (value.GetBitLength() <= 53)
            return (long)value;

        var magnitude = BigInteger.Abs(value);
        var width = magnitude.GetBitLength();
        var shift = (int)(width - 54);
        var top = (ulong)(magnitude >> shift);
        var sticky = shift > 0 && BigInteger.TrailingZeroCount(magnitude) < shift;
        var half = (top & 1) != 0;
        top >>= 1;
        if (half && (sticky || (top & 1) != 0))
            top++;

        var rounded = Math.ScaleB(top, shift + 1);
        return value.Sign < 0 ? -rounded : rounded;
    }

    /// <summary>
    /// Compares a BigInt with a finite Number exactly, as <c>IsLessThan</c> and <c>IsLooselyEqual</c>
    /// do for a BigInt and a Number (ECMA-262 7.2.13 and 7.2.14); NaN and the infinities are the
    /// engine's to answer before the call.
    /// </summary>
    /// <param name="value">The BigInt.</param>
    /// <param name="number">A finite Number.</param>
    /// <param name="charge">The meter, or <see langword="null"/>.</param>
    /// <returns>Negative, zero or positive as the BigInt is below, equal to or above the Number.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The Number is NaN or infinite.</exception>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 7.2.13 via Broiler.VM JsBigInt; IP=Low; Security=High; Resources=2; Fingerprint=B22823
    // Broiler-Falsified-If: a BigInt and a Number compare other than their mathematical values do, or either is rounded to compare
    // Broiler-Human:        PENDING
    public static int CompareToNumber(BigInteger value, double number, WorkCharge? charge)
    {
        if (!double.IsFinite(number))
            throw new ArgumentOutOfRangeException(nameof(number), "the Number is not finite");

        Charge(charge, LinearCost(Math.Max(Words(value), WordsOf(1024))));
        var floor = Math.Floor(number);
        var order = value.CompareTo(new BigInteger(floor));
        if (floor == number)
            return order;

        // A fraction lies strictly between its floor and the next integer.
        return order <= 0 ? -1 : 1;
    }

    /// <summary>The size a charge over <paramref name="value"/> is computed from: its width in 64-bit words, plus one.</summary>
    /// <param name="value">The value.</param>
    /// <returns><see cref="WordsOf"/> of its width.</returns>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 6.1.6.2 via Broiler.VM JsBigInt; IP=Low; Security=Medium; Resources=1; Fingerprint=B02C68
    // Broiler-Human:        PENDING
    public static int Words(BigInteger value) => WordsOf(value.GetBitLength());

    /// <summary>The word count a value <paramref name="bits"/> wide is charged as: <c>ceil(bits / 64) + 1</c>.</summary>
    /// <param name="bits">The width.</param>
    /// <returns>The words, at least one.</returns>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 6.1.6.2 via Broiler.VM JsBigInt; IP=Low; Security=Medium; Resources=1; Fingerprint=B41A21
    // Broiler-Human:        PENDING
    public static int WordsOf(long bits) => (int)((bits + 63) / 64) + 1;

    /// <summary>The charge of a pass over <paramref name="words"/> words: one unit a word, plus one.</summary>
    /// <param name="words">The words.</param>
    /// <returns>The units.</returns>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 6.1.6.2 via Broiler.VM JsBigInt; IP=Low; Security=High; Resources=1; Fingerprint=EFBBAF
    // Broiler-Falsified-If: an operation linear in its operands is charged less for wider operands
    // Broiler-Human:        PENDING
    public static ulong LinearCost(int words) => (ulong)Math.Max(words, 0) + 1UL;

    /// <summary>
    /// The charge of a multiplication or a division over operands of these word counts: their product
    /// over sixteen, plus thirty-two units a word.
    /// </summary>
    /// <param name="leftWords">The left operand's words.</param>
    /// <param name="rightWords">The right operand's words.</param>
    /// <returns>The units.</returns>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 6.1.6.2 via Broiler.VM JsBigInt; IP=Low; Security=High; Resources=1; Fingerprint=E26CC9
    // Broiler-Falsified-If: a multiplication or division of wider operands is charged less than one of narrower operands
    // Broiler-Human:        PENDING
    public static ulong ProductCost(int leftWords, int rightWords) =>
        ((ulong)Math.Max(leftWords, 1) * (ulong)Math.Max(rightWords, 1) / 16UL) +
        (32UL * ((ulong)Math.Max(leftWords, 0) + (ulong)Math.Max(rightWords, 0)));

    /// <summary>The charge of converting a part of <paramref name="words"/> words to or from text whole.</summary>
    /// <param name="words">The part's words.</param>
    /// <returns>The units.</returns>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 6.1.6.2.23 via Broiler.VM JsBigInt; IP=Low; Security=High; Resources=1; Fingerprint=4CB044
    // Broiler-Falsified-If: a wider part is converted for a smaller charge
    // Broiler-Human:        PENDING
    public static ulong LeafCost(int words) => ((ulong)words * (ulong)words / 2UL) + (16UL * (ulong)Math.Max(words, 0)) + 1UL;

    /// <summary>Charges <paramref name="units"/> to the meter, when there is one.</summary>
    /// <param name="charge">The meter, or <see langword="null"/>.</param>
    /// <param name="units">The units.</param>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 6.1.6.2 via Broiler.VM JsBigInt; IP=Low; Security=Medium; Resources=1; Fingerprint=88B593
    // Broiler-Human:        PENDING
    internal static void Charge(WorkCharge? charge, ulong units) => charge?.Invoke(units);

    /// <summary>A computed value, or <see cref="ResultStatus.LimitExceeded"/> when it is wider than the policy.</summary>
    /// <param name="value">The value.</param>
    /// <param name="policy">The width policy.</param>
    /// <param name="result">The value, when it is within the policy.</param>
    /// <returns>The status.</returns>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 6.1.6.2 via Broiler.VM JsBigInt; IP=Low; Security=High; Resources=1; Fingerprint=1D392B
    // Broiler-Falsified-If: a value wider than the policy is answered as Completed, or one within it is refused
    // Broiler-Human:        PENDING
    private static ResultStatus Bounded(BigInteger value, BigIntPolicy policy, out BigInteger result)
    {
        if (!policy.Admits(value))
            return Refused(out result);

        result = value;
        return ResultStatus.Completed;
    }

    /// <summary>The refusal of a value past the policy.</summary>
    /// <param name="result">Zero; it is not a result.</param>
    /// <returns><see cref="ResultStatus.LimitExceeded"/>.</returns>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 6.1.6.2 via Broiler.VM JsBigInt; IP=Low; Security=Medium; Resources=1; Fingerprint=4B04F2
    // Broiler-Human:        PENDING
    private static ResultStatus Refused(out BigInteger result)
    {
        result = BigInteger.Zero;
        return ResultStatus.LimitExceeded;
    }

    /// <summary><c>value * 2 ** count</c>, rounding toward negative infinity for a negative count.</summary>
    /// <param name="value">The value.</param>
    /// <param name="count">The count.</param>
    /// <param name="policy">The width policy.</param>
    /// <param name="charge">The meter, or <see langword="null"/>.</param>
    /// <param name="result">The shifted value.</param>
    /// <returns>The status.</returns>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-262 6.1.6.2.9 via Broiler.VM JsBigInt; IP=Low; Security=High; Resources=2; Fingerprint=6DB2EF
    // Broiler-Falsified-If: a left shift the widths prove too wide is computed before it is refused, or a right shift past the value's width answers other than 0n or -1n
    // Broiler-Human:        PENDING
    private static ResultStatus Shift(BigInteger value, BigInteger count, BigIntPolicy policy, WorkCharge? charge, out BigInteger result)
    {
        if (value.IsZero)
        {
            Charge(charge, 1);
            result = BigInteger.Zero;
            return ResultStatus.Completed;
        }

        var width = value.GetBitLength();
        if (count.Sign >= 0)
        {
            // A left shift appends zero bits and leaves the sign, so its width is exactly the sum.
            if (count > policy.MaximumBits || width + (long)count > policy.MaximumBits)
            {
                Charge(charge, LinearCost(Words(value)));
                return Refused(out result);
            }

            var shifted = (int)count;
            Charge(charge, LinearCost(WordsOf(width + shifted)));
            result = value << shifted;
            return ResultStatus.Completed;
        }

        Charge(charge, LinearCost(Words(value)));
        var distance = -count;

        // Every bit is shifted out: what is left is the sign, which floor division by 2^distance
        // gives as 0 for a non-negative value and -1 for a negative one.
        if (distance >= width)
        {
            result = value.Sign < 0 ? BigInteger.MinusOne : BigInteger.Zero;
            return ResultStatus.Completed;
        }

        result = value >> (int)distance;
        return ResultStatus.Completed;
    }
}
