// REFERENCE ARM V: a test-only copy of Broiler.VM's implementation before the cut-over, for the
// reference-arm comparison of bundle sjl-4-001 (docs/evidence/sjl-4-001/reference-arms.md). It is
// never referenced outside this test project, and it is retired at SJL-16.
//
// Copied from Broiler.VM at a04b2e5 (SJL-3's merge, bundle sjl-4-001's base):
//   src/Broiler.VM.Profile.JavaScript/JsBigInt.cs
//     blob 5179b1c8ceec25eea046c63e7b09df91d1f442a6: the sealed class JsBigInt whole
//
// Changes from the source, none of which changes a result or a charge:
//   - the namespace; no member is widened, and none takes an engine type (JsValue is named only in a doc comment), so none is replaced;
//   - the generated assurance header and the per-member assurance annotations (Broiler-AI / Broiler-Human / Broiler-Falsified-If lines) removed;
//   - the SPDX lines above the assurance header removed;
//   - TryParseStringInteger's call `JsNumberFormat.TrimWhiteSpace(text)` is unchanged and reaches this arm's copy of that engine member (JsNumberFormat.cs beside this file), which calls the shared library's public NumberParsing.TrimStrWhiteSpace(ReadOnlySpan<char>, TrimEnds) exactly as the engine's does; that member is the same at the library's 4899f9c, the version Broiler.VM used, and at the commit this project builds;
//   - every Action<ulong> charge delegate is kept as it is: every charge the source makes, the copy makes, in the same order and amount (the cost functions LinearCost, ProductCost, LeafCost and WordsOf, and MaximumBits, are verbatim).

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.Numerics.V;

/// <summary>
/// One BigInt: an exact signed integer of bounded width. <b>Internal</b>; the language's BigInt,
/// built on it, is the <c>broiler.javascript.bigint</c> surface since card B05.
/// </summary>
/// <remarks>
/// <para>
/// <b>The magnitude is a <see cref="BigInteger"/>, and that is a choice between
/// allowed things rather than an assumption that a library is permitted</b> (decision JSD-0033).
/// It is part of the base class library, it is trimming- and Native-AOT-safe, it reaches no
/// reflection, dynamic loading or unmanaged code (rule B5), and it adds no reference outside the
/// framework (rule B1). A hand-written limb array was weighed and refused: it would be a second
/// arbitrary-precision implementation for this profile to get right, and arithmetic (JSeal B03) is
/// where a wrong carry would show up as a plausible wrong answer.
/// </para>
/// <para>
/// <b>A class and not the struct itself, for two reasons.</b> <see cref="JsValue"/> already carries
/// a reference field, so a BigInt costs the value layout nothing: the tag says BigInt, the double is
/// unused, and the reference is this. And a class is where the width ceiling, the size a charge is
/// computed from, and the canonical decoding live, so no caller handles a bare
/// <see cref="BigInteger"/> it could forget to bound.
/// </para>
/// <para>
/// <b>Identity means nothing.</b> Two instances holding one integer are the same BigInt: equality
/// and hashing are mathematical, which is what <c>===</c>, <c>SameValueZero</c> and a Map key
/// require of a primitive.
/// </para>
/// </remarks>
internal sealed class JsBigInt : IEquatable<JsBigInt>
{
    /// <summary>
    /// The widest magnitude a BigInt value of this realm may hold, in bits: 1,048,576 (128 KiB).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The constant bound (65,536 bits) and this value bound are two numbers with one owner. Every
    /// operation that computes a BigInt (JSeal B03-B04) answers <see langword="null"/> rather than a
    /// value past it, and the engine turns that into a <c>RangeError</c>; an operation whose result
    /// is known to be too wide from its operands' widths refuses before it allocates.
    /// </para>
    /// <para>
    /// <b>The width is <see cref="BigInteger.GetBitLength"/>'s</b>: the shortest
    /// two's-complement length without the sign bit. For a non-negative value that is the
    /// magnitude's bit count; for a negative power of two it is one less, so <c>-(2n ** 1048576n)</c>
    /// is admitted and <c>2n ** 1048576n</c> is not (decision JSD-0033, as amended for B03).
    /// </para>
    /// </remarks>
    internal const long MaximumBits = 1L << 20;

    /// <summary>Creates a BigInt holding <paramref name="value"/>.</summary>
    internal JsBigInt(BigInteger value)
    {
        if (value.GetBitLength() > MaximumBits)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "a BigInt wider than the realm's ceiling was constructed");
        }

        Value = value;
    }

    /// <summary>The integer.</summary>
    internal BigInteger Value { get; }

    /// <summary>Whether this is <c>0n</c>, the one BigInt <c>ToBoolean</c> answers false for.</summary>
    internal bool IsZero => Value.IsZero;

    /// <summary>
    /// The size a charge over this value is computed from: its magnitude in 64-bit words, at least
    /// one.
    /// </summary>
    internal int Words => (int)((Value.GetBitLength() + 63) / 64) + 1;

    /// <summary>
    /// Decodes a constant's canonical payload: a sign and the magnitude's bytes, least significant
    /// first.
    /// </summary>
    /// <remarks>
    /// The verifier has already refused a non-canonical or over-wide payload; this is the one
    /// decoding, and it is linear in the payload.
    /// </remarks>
    internal static JsBigInt FromConstant(bool negative, ReadOnlySpan<byte> magnitude)
    {
        var value = new BigInteger(magnitude, isUnsigned: true, isBigEndian: false);
        return new JsBigInt(negative ? -value : value);
    }

    /// <summary>
    /// The abstract operation <c>BigInt::toString(x, 10)</c>: optional <c>-</c>, then decimal
    /// digits, and no <c>n</c>, with every step charged to <paramref name="charge"/> before it runs.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The conversion is divide and conquer, not the base class library's own.</b> Measured on
    /// 2026-09-21 (Release, this machine), <c>BigInteger.ToString</c> of a 1,048,576-bit value takes
    /// about 3.2 s in one call nothing can interrupt; splitting the value by squared powers of ten
    /// and converting leaves of at most <see cref="FormatLeafBits"/> bits takes about 0.21 s, and
    /// its longest single step - the first division - about 52 ms. Each division is charged with
    /// <see cref="ProductCost"/> before it runs and each leaf with its square, so the size-squared
    /// cost B01 measured is paid for and a charge between steps is where cancellation is seen.
    /// </para>
    /// <para>
    /// The digit bound is charged first, linearly, as B01 charged it.
    /// </para>
    /// </remarks>
    internal string ToDecimalString(Action<ulong> charge)
    {
        charge(LinearCost(DecimalDigitsBound));

        if (Value.GetBitLength() <= FormatLeafBits)
        {
            charge(LeafCost(Words));
            return Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        var negative = Value.Sign < 0;
        var magnitude = negative ? -Value : Value;
        var text = new StringBuilder(DecimalDigitsBound);

        if (negative)
        {
            text.Append('-');
        }

        // THE POWERS ARE 10^19, ITS SQUARE, ITS SQUARE'S SQUARE, ..., up to about the square root of
        // the value: a split by a power of half the value's width leaves two halves, and a split by
        // a larger one would leave a near-whole remainder to split again.
        var powers = new List<BigInteger> { TenToTheNineteen };
        var widths = new List<int> { 19 };

        while ((powers[^1].GetBitLength() * 2 - 1) * 2 <= magnitude.GetBitLength() + 1)
        {
            var words = WordsOf(powers[^1].GetBitLength());
            charge(ProductCost(words, words));
            powers.Add(powers[^1] * powers[^1]);
            widths.Add(widths[^1] * 2);
        }

        AppendDecimal(text, magnitude, 0, powers.Count - 1, powers, widths, charge);
        return text.ToString();
    }

    /// <summary>
    /// Appends the decimal digits of a non-negative <paramref name="value"/>, left-padded with zeros
    /// to <paramref name="width"/> when that is not zero, in which case the value is below the square
    /// of <c>powers[level]</c>.
    /// </summary>
    /// <remarks>
    /// An unpadded part is split by the largest power no wider than half of it, which is below it,
    /// so its quotient is not zero and has no leading zeros to write. A padded part is a remainder
    /// below <c>powers[level + 1]</c>, which is <c>powers[level]</c> squared, so it splits into two
    /// parts below <c>powers[level]</c> and the recursion is as deep as the list is long.
    /// </remarks>
    private static void AppendDecimal(StringBuilder text, BigInteger value, int width, int level, List<BigInteger> powers, List<int> widths, Action<ulong> charge)
    {
        var bits = value.GetBitLength();

        while (level >= 0 && width == 0 && powers[level].GetBitLength() * 2 > bits + 1)
        {
            level--;
        }

        if (level < 0 || bits <= FormatLeafBits)
        {
            charge(LeafCost(WordsOf(bits)));
            var leaf = value.ToString(System.Globalization.CultureInfo.InvariantCulture);

            if (leaf.Length < width)
            {
                text.Append('0', width - leaf.Length);
            }

            text.Append(leaf);
            return;
        }

        charge(ProductCost(WordsOf(bits), WordsOf(powers[level].GetBitLength())));
        var quotient = BigInteger.DivRem(value, powers[level], out var remainder);

        if (width == 0)
        {
            AppendDecimal(text, quotient, 0, level, powers, widths, charge);
        }
        else
        {
            AppendDecimal(text, quotient, width - widths[level], level - 1, powers, widths, charge);
        }

        AppendDecimal(text, remainder, widths[level], level - 1, powers, widths, charge);
    }

    /// <summary>An upper bound on the decimal digits <see cref="ToDecimalString"/> produces.</summary>
    /// <remarks>
    /// A charge is taken on this before the text exists, so a caller pays for the conversion rather
    /// than after it. <c>log10(2)</c> is below <c>0.30103</c>, and the one added covers the sign.
    /// </remarks>
    internal int DecimalDigitsBound => (int)(Value.GetBitLength() * 30103L / 100000L) + 2;

    /// <summary>Mathematical equality.</summary>
    public bool Equals(JsBigInt? other) => other is not null && Value.Equals(other.Value);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is JsBigInt other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => Value.GetHashCode();

    // ---- arithmetic (JSeal B03) and bitwise operations (JSeal B04) ---------------------------------
    //
    // EVERY OPERATION CHARGES BEFORE IT COMPUTES AND ANSWERS NULL RATHER THAN A VALUE PAST THE
    // CEILING. The charge goes to a delegate so that the arithmetic holds no engine and can be judged
    // on its own; the engine passes its fuel charge, which is also where cancellation is polled, and
    // turns a null into the RangeError the language gives a result it cannot represent. Argument
    // errors the specification names - a zero divisor, a negative exponent - are the engine's to
    // raise before it calls here; nothing here converts a BigInt through a double.

    /// <summary><c>0n</c>.</summary>
    internal static readonly JsBigInt Zero = new(BigInteger.Zero);

    /// <summary><c>1n</c>.</summary>
    internal static readonly JsBigInt One = new(BigInteger.One);

    /// <summary><c>-1n</c>.</summary>
    internal static readonly JsBigInt MinusOne = new(BigInteger.MinusOne);

    /// <summary>The widest part the decimal conversion hands to the base class library whole.</summary>
    /// <remarks>
    /// 4,096 bits (65 words): measured at about 21 to 38 microseconds a conversion, which its charge
    /// (<see cref="LeafCost"/>, 3,153 units at about 21 ns a unit of ordinary work) covers.
    /// </remarks>
    private const long FormatLeafBits = 4096;

    /// <summary>10^19, the largest power of ten a 64-bit word holds.</summary>
    private static readonly BigInteger TenToTheNineteen = new(10_000_000_000_000_000_000UL);

    /// <summary>
    /// The width this value's charges and ceiling are computed from: the shortest two's-complement
    /// length without the sign bit.
    /// </summary>
    internal long Bits => Value.GetBitLength();

    /// <summary>The word count <see cref="Words"/> would give a value <paramref name="bits"/> wide.</summary>
    internal static int WordsOf(long bits) => (int)((bits + 63) / 64) + 1;

    /// <summary>The fuel a pass over <paramref name="words"/> words costs: one unit a word.</summary>
    /// <remarks>
    /// Measured on 2026-09-21: an addition, a bitwise operation or a shift of wide operands takes
    /// about 3 ns a word, and a unit of ordinary work about 21 ns, so a unit a word over-charges.
    /// </remarks>
    internal static ulong LinearCost(int words) => (ulong)Math.Max(words, 0) + 1UL;

    /// <summary>
    /// The fuel a multiplication or a division over operands of these word counts costs: the
    /// product of the sizes over sixteen, plus thirty-two units a word.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The size-squared term is the one B01 asked for.</b> Measured on 2026-09-21 (Release, this
    /// machine), a multiplication of two equal operands took from about 0.6 microseconds (8 words)
    /// to 27 ms (8,192 words) and a division of a value by one half its width from about 0.4
    /// microseconds to 69 ms (16,384 words); at about 21 ns a unit of ordinary work (the default
    /// allowance of 50,000,000 units lasts about 1.05 s), this charge is between about 1.5 and 20
    /// times the measured time at every size from 2 to 16,384 words. The linear term covers the
    /// small sizes, where the fixed cost of a call dominates.
    /// </para>
    /// </remarks>
    internal static ulong ProductCost(int leftWords, int rightWords) =>
        (ulong)Math.Max(leftWords, 1) * (ulong)Math.Max(rightWords, 1) / 16UL +
        32UL * ((ulong)Math.Max(leftWords, 0) + (ulong)Math.Max(rightWords, 0));

    /// <summary>The fuel converting a part of <paramref name="words"/> words to text whole costs.</summary>
    private static ulong LeafCost(int words) => (ulong)words * (ulong)words / 2UL + 16UL * (ulong)Math.Max(words, 0) + 1UL;

    /// <summary>Wraps <paramref name="value"/>, or answers null when it is wider than the ceiling.</summary>
    private static JsBigInt? Bounded(BigInteger value) => value.GetBitLength() > MaximumBits ? null : new JsBigInt(value);

    /// <summary><c>BigInt::add</c>.</summary>
    internal static JsBigInt? Add(JsBigInt left, JsBigInt right, Action<ulong> charge)
    {
        charge(LinearCost(Math.Max(left.Words, right.Words)));
        return Bounded(left.Value + right.Value);
    }

    /// <summary><c>BigInt::subtract</c>.</summary>
    internal static JsBigInt? Subtract(JsBigInt left, JsBigInt right, Action<ulong> charge)
    {
        charge(LinearCost(Math.Max(left.Words, right.Words)));
        return Bounded(left.Value - right.Value);
    }

    /// <summary><c>BigInt::unaryMinus</c>; <c>-0n</c> is <c>0n</c>, since there is one zero.</summary>
    internal static JsBigInt? Negate(JsBigInt value, Action<ulong> charge)
    {
        charge(LinearCost(value.Words));
        return Bounded(-value.Value);
    }

    /// <summary><c>BigInt::bitwiseNOT</c>: <c>-x - 1</c>, which never widens a value.</summary>
    internal static JsBigInt? BitwiseNot(JsBigInt value, Action<ulong> charge)
    {
        charge(LinearCost(value.Words));
        return Bounded(-value.Value - BigInteger.One);
    }

    /// <summary><c>BigInt::multiply</c>, refused before it allocates when the product must be too wide.</summary>
    internal static JsBigInt? Multiply(JsBigInt left, JsBigInt right, Action<ulong> charge)
    {
        if (left.IsZero || right.IsZero)
        {
            charge(1);
            return Zero;
        }

        // |x| is at least 2^(Bits - 1), so the product is at least 2^(a + b - 2) in magnitude.
        if (left.Bits + right.Bits - 2 > MaximumBits)
        {
            charge(LinearCost(Math.Max(left.Words, right.Words)));
            return null;
        }

        charge(ProductCost(left.Words, right.Words));
        return Bounded(left.Value * right.Value);
    }

    /// <summary>
    /// <c>BigInt::divide</c>, truncating toward zero. <paramref name="right"/> is not zero: the
    /// engine raises the RangeError for that first.
    /// </summary>
    internal static JsBigInt? Divide(JsBigInt left, JsBigInt right, Action<ulong> charge)
    {
        charge(ProductCost(left.Words, right.Words));
        return Bounded(BigInteger.Divide(left.Value, right.Value));
    }

    /// <summary>
    /// <c>BigInt::remainder</c>, whose sign is the dividend's. <paramref name="right"/> is not zero.
    /// </summary>
    internal static JsBigInt? Remainder(JsBigInt left, JsBigInt right, Action<ulong> charge)
    {
        charge(ProductCost(left.Words, right.Words));
        return Bounded(BigInteger.Remainder(left.Value, right.Value));
    }

    /// <summary>
    /// <c>BigInt::exponentiate</c> for a non-negative <paramref name="exponent"/> (the engine raises
    /// the RangeError for a negative one first), by squaring, with every multiplication charged
    /// before it runs and a result that must pass the ceiling refused as soon as that is certain.
    /// </summary>
    /// <remarks>
    /// <c>0n ** 0n</c> is <c>1n</c>. A base of <c>0n</c>, <c>1n</c> or <c>-1n</c> answers at once for
    /// any exponent; any other base with an exponent above <see cref="MaximumBits"/> is refused
    /// without a multiplication, since its result is at least <c>2 ** exponent</c>.
    /// </remarks>
    internal static JsBigInt? Power(JsBigInt @base, JsBigInt exponent, Action<ulong> charge)
    {
        charge(LinearCost(Math.Max(@base.Words, exponent.Words)));

        if (exponent.IsZero)
        {
            return One;
        }

        if (@base.IsZero || @base.Value.IsOne)
        {
            return @base;
        }

        if (@base.Value == BigInteger.MinusOne)
        {
            return exponent.Value.IsEven ? One : MinusOne;
        }

        // |base| >= 2 from here, so the result is at least 2^exponent and at least
        // 2^(exponent * (Bits - 1)) in magnitude - one bit wider than that when it is positive,
        // since only a negative power of two is as narrow as its exponent.
        if (exponent.Value > MaximumBits)
        {
            return null;
        }

        var positive = @base.Value.Sign > 0 || exponent.Value.IsEven;

        if ((long)exponent.Value * (@base.Bits - 1) + (positive ? 1 : 0) > MaximumBits)
        {
            return null;
        }

        var remaining = (long)exponent.Value;
        var result = BigInteger.One;
        var square = @base.Value;

        while (true)
        {
            if ((remaining & 1) != 0)
            {
                // As for Multiply: operands this wide prove the product too wide before it exists.
                if (result.GetBitLength() + square.GetBitLength() - 2 > MaximumBits)
                {
                    return null;
                }

                charge(ProductCost(WordsOf(result.GetBitLength()), WordsOf(square.GetBitLength())));
                result *= square;

                if (result.GetBitLength() > MaximumBits)
                {
                    return null;
                }
            }

            remaining >>= 1;

            if (remaining == 0)
            {
                return new JsBigInt(result);
            }

            // A square past the ceiling with an exponent bit still to come is a result past it: the
            // result will be multiplied by at least this square, and every other factor is at least
            // one in magnitude. One the operand's width already proves too wide is not computed.
            if (2 * square.GetBitLength() - 2 > MaximumBits)
            {
                return null;
            }

            var words = WordsOf(square.GetBitLength());
            charge(ProductCost(words, words));
            square *= square;

            if (square.GetBitLength() > MaximumBits)
            {
                return null;
            }
        }
    }

    /// <summary><c>BigInt::bitwiseAND</c>, on two's-complement values of unbounded width.</summary>
    internal static JsBigInt? And(JsBigInt left, JsBigInt right, Action<ulong> charge)
    {
        charge(LinearCost(Math.Max(left.Words, right.Words)));
        return Bounded(left.Value & right.Value);
    }

    /// <summary><c>BigInt::bitwiseOR</c>, on two's-complement values of unbounded width.</summary>
    internal static JsBigInt? Or(JsBigInt left, JsBigInt right, Action<ulong> charge)
    {
        charge(LinearCost(Math.Max(left.Words, right.Words)));
        return Bounded(left.Value | right.Value);
    }

    /// <summary><c>BigInt::bitwiseXOR</c>, on two's-complement values of unbounded width.</summary>
    internal static JsBigInt? Xor(JsBigInt left, JsBigInt right, Action<ulong> charge)
    {
        charge(LinearCost(Math.Max(left.Words, right.Words)));
        return Bounded(left.Value ^ right.Value);
    }

    /// <summary>
    /// <c>BigInt::leftShift</c>: <c>value * 2 ** count</c>, which for a negative count is a right
    /// shift that rounds toward negative infinity. A result the widths prove too wide is refused
    /// before it allocates; a right shift past the value's width answers <c>0n</c> or <c>-1n</c>
    /// without shifting, however large the count.
    /// </summary>
    internal static JsBigInt? ShiftLeft(JsBigInt value, JsBigInt count, Action<ulong> charge) =>
        Shift(value, count.Value, charge);

    /// <summary>
    /// <c>BigInt::signedRightShift</c>: a left shift by the negated count, negated as an integer and
    /// not as a BigInt, so a count at the ceiling's edge needs no second value.
    /// </summary>
    internal static JsBigInt? ShiftRight(JsBigInt value, JsBigInt count, Action<ulong> charge) =>
        Shift(value, -count.Value, charge);

    /// <summary><c>value * 2 ** count</c>, rounding toward negative infinity for a negative count.</summary>
    private static JsBigInt? Shift(JsBigInt value, BigInteger count, Action<ulong> charge)
    {
        if (value.IsZero)
        {
            charge(1);
            return Zero;
        }

        if (count.Sign >= 0)
        {
            // A left shift appends zero bits and leaves the sign, so its width is exactly the sum.
            if (count > MaximumBits || value.Bits + (long)count > MaximumBits)
            {
                charge(LinearCost(value.Words));
                return null;
            }

            var shifted = (int)count;
            charge(LinearCost(WordsOf(value.Bits + shifted)));
            return new JsBigInt(value.Value << shifted);
        }

        charge(LinearCost(value.Words));
        var distance = -count;

        // Every bit is shifted out: what is left is the sign, which floor division by 2^distance
        // gives as 0 for a non-negative value and -1 for a negative one.
        if (distance >= value.Bits)
        {
            return value.Value.Sign < 0 ? MinusOne : Zero;
        }

        return new JsBigInt(value.Value >> (int)distance);
    }

    /// <summary>
    /// The operation behind <c>BigInt.asIntN(bits, value)</c>: <paramref name="value"/> modulo
    /// <c>2 ** bits</c>, read as a signed <paramref name="bits"/>-bit integer.
    /// </summary>
    /// <remarks>
    /// <paramref name="bits"/> is the result of <c>ToIndex</c>, so it is at most <c>2 ** 53 - 1</c>.
    /// The result is never wider than the value, so this never answers null. The <c>BigInt</c>
    /// global has exposed it since card B05, after <c>ToIndex</c> and <c>ToBigInt</c>.
    /// </remarks>
    internal static JsBigInt? AsIntN(ulong bits, JsBigInt value, Action<ulong> charge)
    {
        charge(LinearCost(value.Words));

        if (bits == 0)
        {
            return Zero;
        }

        // A value fits a signed width when its two's-complement length leaves room for the sign.
        if ((ulong)value.Bits < bits)
        {
            return value;
        }

        // Here bits <= value.Bits <= MaximumBits, so the modulus is bounded.
        var modulus = BigInteger.One << (int)bits;
        var wrapped = value.Value & (modulus - BigInteger.One);

        if (wrapped >= modulus >> 1)
        {
            wrapped -= modulus;
        }

        return Bounded(wrapped);
    }

    /// <summary>
    /// The operation behind <c>BigInt.asUintN(bits, value)</c>: <paramref name="value"/> modulo
    /// <c>2 ** bits</c>, or null when that is wider than the ceiling.
    /// </summary>
    /// <remarks>
    /// A negative value modulo a width past <see cref="MaximumBits"/> is at least
    /// <c>2 ** (bits - 1)</c>, so it is refused without allocating. The <c>BigInt</c> global
    /// has exposed it since card B05, after <c>ToIndex</c> and <c>ToBigInt</c>.
    /// </remarks>
    internal static JsBigInt? AsUintN(ulong bits, JsBigInt value, Action<ulong> charge)
    {
        charge(LinearCost(value.Words));

        if (bits == 0)
        {
            return Zero;
        }

        if (value.Value.Sign >= 0 && (ulong)value.Bits <= bits)
        {
            return value;
        }

        if (value.Value.Sign < 0 && bits > MaximumBits)
        {
            return null;
        }

        // Here bits <= MaximumBits, or the value is non-negative and wider than bits.
        charge(LinearCost(WordsOf((long)bits)));
        var mask = (BigInteger.One << (int)bits) - BigInteger.One;
        return Bounded(value.Value & mask);
    }

    // ---- conversion and comparison (JSeal B05) -----------------------------------------------------
    //
    // NOTHING HERE READS A BIGINT THROUGH A DOUBLE OR A DOUBLE THROUGH A ROUNDING. A Number meets a
    // BigInt in three places - `Number(x)`, `BigInt(x)` and a mixed comparison - and each is exact:
    // the Number is an integer times a power of two, so it has an exact BigInt when it is integral and
    // an exact floor when it is not, and the one rounding the language asks for (`Number(x)` of a
    // BigInt too wide for 53 bits) is done here, to nearest with ties to even, rather than by the
    // base class library's conversion, which truncates (measured 2026-09-21: it answers
    // `Number.MAX_VALUE` for `2n ** 1024n - 2n ** 970n`, where the language answers `Infinity`).

    /// <summary>The widest leaf the decimal parse hands to the base class library whole.</summary>
    /// <remarks>
    /// 1,024 digits (about 3,400 bits), below <see cref="FormatLeafBits"/>. Measured on 2026-09-21
    /// (Release, this machine), <c>BigInteger.Parse</c> of 315,653 digits - the widest decimal text a
    /// value may have - is one call of about 258 ms nothing can interrupt, so a long text is split
    /// and joined as the conversion to text splits it.
    /// </remarks>
    private const int ParseLeafDigits = 1024;

    /// <summary>
    /// The most decimal digits, without leading zeros, a value inside the ceiling can have:
    /// <c>2 ** 1048576</c> has 315,653, and a text of one more digit is always wider.
    /// </summary>
    private const int MaximumDecimalDigits = (int)(MaximumBits * 30103L / 100000L) + 1;

    /// <summary>
    /// The abstract operation <c>NumberToBigInt</c> for an integral finite <paramref name="number"/>:
    /// the exact integer, which a double always has.
    /// </summary>
    /// <remarks>The caller has refused a fraction, a NaN and an infinity with the RangeError first.</remarks>
    internal static JsBigInt FromIntegralNumber(double number) => new(new BigInteger(number));

    /// <summary>
    /// <c>𝔽(ℝ(x))</c>: the Number nearest this integer, ties to even, and an infinity past
    /// <c>Number.MAX_VALUE</c>'s rounding range.
    /// </summary>
    /// <remarks>
    /// The top 54 bits are taken with a sticky bit for everything below them, rounded once to 53,
    /// and scaled by the power of two they were shifted by; the scaling is exact until it overflows,
    /// which is the infinity the language gives. The work is one pass over the value.
    /// </remarks>
    internal double ToNumber(Action<ulong> charge)
    {
        charge(LinearCost(Words));

        if (Bits <= 53)
        {
            return (long)Value;
        }

        var magnitude = BigInteger.Abs(Value);
        var width = magnitude.GetBitLength();
        var shift = (int)(width - 54);
        var top = (ulong)(magnitude >> shift);
        var sticky = shift > 0 && BigInteger.TrailingZeroCount(magnitude) < shift;
        var half = (top & 1) != 0;
        top >>= 1;

        if (half && (sticky || (top & 1) != 0))
        {
            top++;
        }

        var rounded = Math.ScaleB(top, shift + 1);
        return Value.Sign < 0 ? -rounded : rounded;
    }

    /// <summary>
    /// Compares this integer with a finite <paramref name="number"/> exactly: negative, zero or
    /// positive as this is below, equal to or above it.
    /// </summary>
    /// <remarks>
    /// An integral Number is compared as the integer it is. A fraction lies strictly between its
    /// floor and the next integer, so an integer at or below the floor is below it and any other is
    /// above it. The Number is at most 1,024 bits wide, so the work is bounded by the value's width.
    /// </remarks>
    internal int CompareToNumber(double number, Action<ulong> charge)
    {
        charge(LinearCost(Math.Max(Words, WordsOf(1024))));
        var floor = Math.Floor(number);
        var order = Value.CompareTo(new BigInteger(floor));

        if (floor == number)
        {
            return order;
        }

        return order <= 0 ? -1 : 1;
    }

    /// <summary>
    /// The abstract operation <c>StringToBigInt</c>: <see langword="false"/> when
    /// <paramref name="text"/> is not a <c>StringIntegerLiteral</c>; otherwise the value, or
    /// <see langword="null"/> with its sign in <paramref name="tooWideSign"/> when the value is wider
    /// than the ceiling.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The grammar is the language's, not the literal's.</b> White space and line terminators
    /// around it are trimmed; an empty text is <c>0n</c>; a decimal integer may have a sign and
    /// leading zeros; a <c>0x</c>, <c>0o</c> or <c>0b</c> integer may have neither sign nor
    /// separator; and there is no <c>n</c>, no fraction and no exponent.
    /// </para>
    /// <para>
    /// <b>A value past the ceiling is reported, not refused</b>, because the three callers need
    /// different things from it: <c>BigInt(text)</c> raises the RangeError, <c>==</c> answers
    /// <c>false</c> (no value in the realm equals it), and a relational comparison answers by its
    /// sign - each exact. A decimal text of more digits than any value inside the ceiling has is
    /// known too wide without converting a digit.
    /// </para>
    /// <para>
    /// The text is charged for its length before it is read; a decimal conversion is then charged
    /// as <see cref="ToDecimalString"/> is, leaf by leaf and join by join, and the radix-prefixed
    /// forms are one pass.
    /// </para>
    /// </remarks>
    internal static bool TryParseStringInteger(string text, Action<ulong> charge, out JsBigInt? value, out int tooWideSign)
    {
        charge(LinearCost(text.Length / 8));
        value = null;
        tooWideSign = 0;

        var trimmed = JsNumberFormat.TrimWhiteSpace(text);

        if (trimmed.Length == 0)
        {
            value = Zero;
            return true;
        }

        var prefix = (char)(trimmed.Length > 2 && trimmed[0] == '0' ? trimmed[1] | 0x20 : 0);

        if (prefix is 'x' or 'o' or 'b')
        {
            var radix = prefix switch { 'x' => 16, 'o' => 8, _ => 2 };
            return TryParseRadix(MemoryExtensions.AsSpan(trimmed, 2), radix, charge, out value, out tooWideSign);
        }

        var negative = trimmed[0] == '-';
        var digits = trimmed[0] is '-' or '+' ? MemoryExtensions.AsSpan(trimmed, 1) : MemoryExtensions.AsSpan(trimmed);

        if (digits.Length == 0)
        {
            return false;
        }

        foreach (var digit in digits)
        {
            if (!char.IsAsciiDigit(digit))
            {
                return false;
            }
        }

        digits = MemoryExtensions.TrimStart(digits, '0');

        if (digits.Length > MaximumDecimalDigits)
        {
            tooWideSign = negative ? -1 : 1;
            return true;
        }

        if (digits.Length == 0)
        {
            value = Zero;
            return true;
        }

        var magnitude = ParseDecimal(digits, charge);
        var signed = negative ? -magnitude : magnitude;

        if (signed.GetBitLength() > MaximumBits)
        {
            tooWideSign = negative ? -1 : 1;
            return true;
        }

        value = new JsBigInt(signed);
        return true;
    }

    /// <summary>
    /// A <c>0x</c>, <c>0o</c> or <c>0b</c> text's digits: <see langword="false"/> for a digit outside
    /// the radix, otherwise the value, or <see langword="null"/> when it is wider than the ceiling.
    /// </summary>
    /// <remarks>
    /// The bits are packed least significant first and converted once, as the literal's are, so the
    /// cost is one pass however long the text.
    /// </remarks>
    private static bool TryParseRadix(ReadOnlySpan<char> digits, int radix, Action<ulong> charge, out JsBigInt? value, out int tooWideSign)
    {
        value = null;
        tooWideSign = 0;
        var bitsPerDigit = radix switch { 16 => 4, 8 => 3, _ => 1 };

        foreach (var digit in digits)
        {
            var digitValue = char.IsAsciiDigit(digit) ? digit - '0'
                : char.IsAsciiLetter(digit) ? (digit | 0x20) - 'a' + 10
                : radix;

            if (digitValue >= radix)
            {
                return false;
            }
        }

        digits = MemoryExtensions.TrimStart(digits, '0');

        if ((long)digits.Length * bitsPerDigit > MaximumBits + bitsPerDigit)
        {
            tooWideSign = 1;
            return true;
        }

        charge(LinearCost(WordsOf((long)digits.Length * bitsPerDigit)));
        var bytes = new byte[(((long)digits.Length * bitsPerDigit) + 7) / 8 + 1];
        var bit = 0L;

        for (var at = digits.Length - 1; at >= 0; at--)
        {
            var digit = digits[at];
            var digitValue = char.IsAsciiDigit(digit) ? digit - '0' : (digit | 0x20) - 'a' + 10;

            for (var within = 0; within < bitsPerDigit; within++, bit++)
            {
                if ((digitValue >> within & 1) != 0)
                {
                    bytes[bit / 8] |= (byte)(1 << (int)(bit % 8));
                }
            }
        }

        var magnitude = new BigInteger(bytes, isUnsigned: true, isBigEndian: false);

        if (magnitude.GetBitLength() > MaximumBits)
        {
            tooWideSign = 1;
            return true;
        }

        value = new JsBigInt(magnitude);
        return true;
    }

    /// <summary>
    /// The integer a run of decimal digits with no leading zero spells, split and joined by squared
    /// powers of ten with every step charged before it runs.
    /// </summary>
    /// <remarks>
    /// The counterpart of <see cref="ToDecimalString"/>: the powers are <c>10^1024</c>, its square,
    /// its square's square and so on, a text is split at the largest one shorter than it, the high
    /// part is multiplied by that power and the low part added, and leaves of at most
    /// <see cref="ParseLeafDigits"/> digits are handed to the base class library.
    /// </remarks>
    private static BigInteger ParseDecimal(ReadOnlySpan<char> digits, Action<ulong> charge)
    {
        var powers = new List<BigInteger>();
        var widths = new List<int>();

        if (digits.Length > ParseLeafDigits)
        {
            charge(ProductCost(WordsOf(ParseLeafDigits * 4L), WordsOf(ParseLeafDigits * 4L)));
            powers.Add(BigInteger.Pow(10, ParseLeafDigits));
            widths.Add(ParseLeafDigits);

            while ((long)widths[^1] * 2 < digits.Length)
            {
                var words = WordsOf(powers[^1].GetBitLength());
                charge(ProductCost(words, words));
                powers.Add(powers[^1] * powers[^1]);
                widths.Add(widths[^1] * 2);
            }
        }

        return ParseDecimalPart(digits, powers.Count - 1, powers, widths, charge);
    }

    /// <summary>One part of <see cref="ParseDecimal"/>, which may have leading zeros.</summary>
    private static BigInteger ParseDecimalPart(ReadOnlySpan<char> digits, int level, List<BigInteger> powers, List<int> widths, Action<ulong> charge)
    {
        while (level >= 0 && widths[level] >= digits.Length)
        {
            level--;
        }

        if (level < 0)
        {
            charge(LeafCost(WordsOf(digits.Length * 4L)));
            return digits.Length == 0
                ? BigInteger.Zero
                : BigInteger.Parse(
                    digits, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture);
        }

        var split = digits.Length - widths[level];
        var high = ParseDecimalPart(digits[..split], level, powers, widths, charge);
        var low = ParseDecimalPart(digits[split..], level - 1, powers, widths, charge);

        charge(ProductCost(WordsOf(high.GetBitLength()), WordsOf(powers[level].GetBitLength())));
        return high * powers[level] + low;
    }

    /// <summary>
    /// The abstract operation <c>BigInt::toString(x, radix)</c> for a radix from 2 to 36: optional
    /// <c>-</c>, then lower-case digits, charged step by step.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Radix 10 is <see cref="ToDecimalString"/>. A radix that is a power of two reads the bits
    /// directly, in one pass. Any other radix is divide and conquer, as the decimal conversion is,
    /// over squared powers of the largest power of the radix a 64-bit word holds, with leaves of at
    /// most <see cref="FormatLeafBits"/> bits converted a word at a time.
    /// </para>
    /// <para>The digit bound is charged first, linearly, as the decimal conversion charges it.</para>
    /// </remarks>
    internal string ToRadixString(int radix, Action<ulong> charge)
    {
        if (radix == 10)
        {
            return ToDecimalString(charge);
        }

        var digitBits = Math.Log2(radix);
        var bound = (int)(Bits / digitBits) + 2;
        charge(LinearCost(bound));

        if (IsZero)
        {
            return "0";
        }

        var negative = Value.Sign < 0;
        var magnitude = negative ? -Value : Value;
        var text = new StringBuilder(bound);

        if (negative)
        {
            text.Append('-');
        }

        if ((radix & (radix - 1)) == 0)
        {
            AppendPowerOfTwoRadix(text, magnitude, BitOperations.Log2((uint)radix));
            return text.ToString();
        }

        // THE CHUNK IS THE LARGEST POWER OF THE RADIX BELOW 2^63, so a leaf's word-at-a-time loop
        // never overflows, and it is the first of the powers the split squares.
        var chunkDigits = (int)(63 / digitBits);
        var chunk = BigInteger.Pow(radix, chunkDigits);
        var powers = new List<BigInteger> { chunk };
        var widths = new List<int> { chunkDigits };

        while ((powers[^1].GetBitLength() * 2 - 1) * 2 <= magnitude.GetBitLength() + 1)
        {
            var words = WordsOf(powers[^1].GetBitLength());
            charge(ProductCost(words, words));
            powers.Add(powers[^1] * powers[^1]);
            widths.Add(widths[^1] * 2);
        }

        AppendRadix(text, magnitude, 0, powers.Count - 1, radix, (ulong)chunk, chunkDigits, powers, widths, charge);
        return text.ToString();
    }

    /// <summary>The digits of a non-zero magnitude in a radix of <c>2 ** bitsPerDigit</c>, most significant first.</summary>
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
                var bit = digit * bitsPerDigit + within;
                var set = bit < width && (bytes[bit / 8] >> (int)(bit % 8) & 1) != 0;
                value = value << 1 | (set ? 1 : 0);
            }

            text.Append(RadixDigits[value]);
        }
    }

    /// <summary>
    /// Appends the digits of a non-negative <paramref name="value"/> in <paramref name="radix"/>,
    /// left-padded with zeros to <paramref name="width"/> when that is not zero; the shape of
    /// <see cref="AppendDecimal"/>, over the radix's own powers.
    /// </summary>
    private static void AppendRadix(StringBuilder text, BigInteger value, int width, int level, int radix, ulong chunk,
        int chunkDigits, List<BigInteger> powers, List<int> widths, Action<ulong> charge)
    {
        var bits = value.GetBitLength();

        while (level >= 0 && width == 0 && powers[level].GetBitLength() * 2 > bits + 1)
        {
            level--;
        }

        if (level < 0 || bits <= FormatLeafBits)
        {
            charge(LeafCost(WordsOf(bits)));
            var leaf = RadixLeaf(value, radix, chunk, chunkDigits);

            if (leaf.Length < width)
            {
                text.Append('0', width - leaf.Length);
            }

            text.Append(leaf);
            return;
        }

        charge(ProductCost(WordsOf(bits), WordsOf(powers[level].GetBitLength())));
        var quotient = BigInteger.DivRem(value, powers[level], out var remainder);

        AppendRadix(
            text, quotient, width == 0 ? 0 : width - widths[level], width == 0 ? level : level - 1,
            radix, chunk, chunkDigits, powers, widths, charge);

        AppendRadix(text, remainder, widths[level], level - 1, radix, chunk, chunkDigits, powers, widths, charge);
    }

    /// <summary>A leaf's digits, a word of <paramref name="chunkDigits"/> digits at a time, unpadded.</summary>
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
        {
            text.Append(WordDigits(words[at], radix, chunkDigits));
        }

        return text.ToString();
    }

    /// <summary>One word's digits in <paramref name="radix"/>, left-padded to <paramref name="width"/>.</summary>
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
        {
            buffer[--at] = '0';
        }

        return new string(buffer[at..]);
    }

    /// <summary>The thirty-six digits, lower case, as <c>Number.prototype.toString</c> writes them.</summary>
    private const string RadixDigits = "0123456789abcdefghijklmnopqrstuvwxyz";
}
