// REFERENCE ARM J: a test-only copy of Broiler.JS's implementation before the cut-over, for the
// reference-arm comparison of bundle sjl-4-001 (docs/evidence/sjl-4-001/reference-arms.md). It is
// never referenced outside this test project, and it is retired at SJL-16.
//
// Copied from Broiler.JS at 4899f9c (SJL-3's merge, bundle sjl-4-001's base):
//   Broiler.JS/Broiler.JavaScript.BuiltIns/BigInt/JSBigInt.cs
//     blob 1d2bf1c1a37c5604bdd6776ada12ae184be276ba: JSBigIntExtensions' NaNComparison (:18) and CompareToNumber
//     (:30-47); JSBigInt's ToNumber (:66-110), NumberToBigInt (:144-154), TryStringToBigInt (:173-186),
//     IsStrWhiteSpaceOnly (:188-189), TryParseBigIntString (:191-224), TryParsePrefixedDigits (:226-259),
//     ToString (:290), EqualsLiteral(double) (:393), Negate (:431), BitwiseNot (:437), Power (:439-446),
//     BitwiseAnd, BitwiseOr and BitwiseXor (:448-452), LeftShift (:454-458), RightShift (:460), Shift
//     (:462-481), Multiply (:494), Divide (:496-502), Subtract (:504), Modulo (:506-512), AddValue(JSValue)'s
//     BigInt + BigInt path (:525-526), BigIntegerToRadixString (:581-605), AsIntN (:674-735) and AsUintN
//     (:737-760)
//   IsStrWhiteSpaceOnly and TryParseBigIntString call the shared library's NumberParsing.TrimStrWhiteSpace
//   (Broiler.ECMAScript/Broiler.ECMAScript.Algorithms/Numbers/NumberParsing.cs, blob
//   be68bfe6512673cca13a3f8d8e89ce82d9a4af7f, and StrWhiteSpace.cs, blob 83852ddb1331a10567b137890b989ca89de86482),
//   as the source does; both blobs are the same at 4899f9c and in this tree, so the call is to the same code.
//
// Changes from the source, none of which changes a result:
//   - line endings normalised to LF where the source has CRLF;
//   - the namespace; JSBigIntExtensions keeps its name and is internal (CompareToNumber stays an extension
//     method of BigInteger); JSBigInt's members sit in a static class of the same name instead of the
//     JSPrimitive JSBigInt, and every copied member is internal and static;
//   - a BigInt is the BigInteger it carries: an instance member's `this.value` (or `value` where the source
//     reads the field) is the parameter left (or value, for the unary members), the right operand's
//     `value.AsBigIntegerOnly()` / `a.AsBigIntegerOnly()` / `b.value` is the parameter right, and a returned
//     `new JSBigInt(x)` is x. AsBigIntegerOnly's ToPrimitive and its TypeError for a non-BigInt, and AddValue's
//     ToPrimitive, unwrapping and its String and mixing (TypeError) branches, are the adapter's;
//   - AsIntN and AsUintN take bits (ToBitsIndex's long; see IntegerHelpers.JSBigInt_ToBitsIndex) and n (ToBigInt's
//     BigInteger); their argument reading and coercion are the adapter's;
//   - NumberToBigInt takes the Number's double (the constructor's `number.value`) and returns the BigInteger;
//   - EqualsLiteral(double) takes the BigInt's value as left;
//   - a JS error the source throws (JSEngine.NewRangeError) is a JsErrorException, a type this arm defines, of
//     kind "RangeError" with the source's message; .NET exceptions (an explicit BigInteger-to-int conversion's
//     OverflowException, an allocation failure) are thrown exactly where the source throws them;
//   - ToString and BigIntegerToRadixString's radix-10 path call the culture-sensitive BigInteger.ToString(),
//     as the source does;
//   - the [JSExport...] attributes on AsIntN and AsUintN are not copied.

using System;
using System.Globalization;
using System.Numerics;
using System.Text;
using Broiler.ECMAScript.Numbers;

namespace Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.Numerics.J;

/// <summary>This arm's stand-in for a JS error the source throws: Kind names the error ("RangeError").</summary>
internal sealed class JsErrorException(string kind, string message) : Exception(message)
{
    internal string Kind { get; } = kind;
}

internal static class JSBigIntExtensions
{
    internal const int NaNComparison = int.MinValue;

    internal static int CompareToNumber(this BigInteger left, double right)
    {
        if (double.IsNaN(right))
            return NaNComparison;

        if (double.IsPositiveInfinity(right))
            return -1;

        if (double.IsNegativeInfinity(right))
            return 1;

        var truncated = Math.Truncate(right);
        var integerComparison = left.CompareTo(new BigInteger(truncated));
        if (truncated == right || integerComparison != 0)
            return integerComparison;

        return right > 0 ? -1 : 1;
    }
}

/// <summary>The BigInt built-in's arithmetic, conversions and text, as functions of BigInteger.</summary>
internal static class JSBigInt
{
    // BigInt::toNumber — 𝔽(ℝ(x)): the nearest IEEE-754 double, ties to even. .NET's
    // (double)BigInteger truncates toward zero, so it must never back the result Number
    // (e.g. Number(8692288669465520373761n) must round up, not down).
    internal static double ToNumber(BigInteger v)
    {
        var negative = v.Sign < 0;
        var magnitude = BigInteger.Abs(v);
        var lo = (double)magnitude; // .NET truncates toward zero, so lo <= magnitude
        if (double.IsInfinity(lo))
            return negative ? double.NegativeInfinity : double.PositiveInfinity;

        double chosen;
        var loBig = new BigInteger(lo);
        if (loBig == magnitude)
        {
            chosen = lo;
        }
        else
        {
            var hi = Math.BitIncrement(lo);
            if (double.IsInfinity(hi))
            {
                // `lo` is the largest finite double (MAX_VALUE = 2^1024 - 2^971); its
                // conceptual successor is 2^1024. IEEE-754 roundTiesToEven overflows to
                // Infinity once the magnitude reaches the midpoint 2^1024 - 2^970, and a
                // tie also rounds up (2^1024 has the even significand). Below the midpoint
                // it rounds down to MAX_VALUE.
                var conceptualHi = BigInteger.One << 1024;
                var overflowDistanceLo = magnitude - loBig;
                var overflowDistanceHi = conceptualHi - magnitude;
                if (overflowDistanceLo < overflowDistanceHi)
                    return negative ? -lo : lo;

                return negative ? double.NegativeInfinity : double.PositiveInfinity;
            }

            var distanceLo = magnitude - loBig;
            var distanceHi = new BigInteger(hi) - magnitude;
            chosen = distanceLo < distanceHi ? lo
                : distanceHi < distanceLo ? hi
                : (BitConverter.DoubleToInt64Bits(lo) & 1) == 0 ? lo : hi; // tie → even
        }

        return negative ? -chosen : chosen;
    }

    internal static BigInteger NumberToBigInt(double number)
    {
        if (double.IsNaN(number)
            || double.IsInfinity(number)
            || Math.Truncate(number) != number)
        {
            throw new JsErrorException("RangeError", "The number cannot be converted to a BigInt because it is not an integer");
        }

        return new BigInteger(number);
    }

    // StringToBigInt (ECMA-262 §7.1.14): an empty or whitespace-only string maps to 0n;
    // any other string is parsed as a BigInt integer literal. A string that is not a valid
    // integer (e.g. "0.", "1.5", "abc") yields undefined — signalled here by returning false —
    // so abstract equality treats it as not-equal and relational comparison as incomparable.
    internal static bool TryStringToBigInt(string value, out BigInteger result)
    {
        if (IsStrWhiteSpaceOnly(value))
        {
            result = BigInteger.Zero;
            return true;
        }

        return TryParseBigIntString(value, out result);
    }

    // StringToBigInt trims StrWhiteSpaceChar, the shared library's set (SJL-3.5), not .NET's.
    internal static bool IsStrWhiteSpaceOnly(string value) => NumberParsing.TrimStrWhiteSpace(value, TrimEnds.Both).IsEmpty;

    internal static bool TryParseBigIntString(string value, out BigInteger result)
    {
        var text = NumberParsing.TrimStrWhiteSpace(value, TrimEnds.Both).ToString();
        if (text.Length == 0 || text.Contains('_') || text.EndsWith('n'))
        {
            result = default;
            return false;
        }

        if (text.StartsWith("+", StringComparison.Ordinal) || text.StartsWith("-", StringComparison.Ordinal))
        {
            var rest = text[1..];
            if (rest.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                || rest.StartsWith("0b", StringComparison.OrdinalIgnoreCase)
                || rest.StartsWith("0o", StringComparison.OrdinalIgnoreCase))
            {
                result = default;
                return false;
            }

            return BigInteger.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out result);
        }

        if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            return TryParsePrefixedDigits(text.AsSpan(2), 16, 1, out result);

        if (text.StartsWith("0b", StringComparison.OrdinalIgnoreCase))
            return TryParsePrefixedDigits(text.AsSpan(2), 2, 1, out result);

        if (text.StartsWith("0o", StringComparison.OrdinalIgnoreCase))
            return TryParsePrefixedDigits(text.AsSpan(2), 8, 1, out result);

        return BigInteger.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out result);
    }

    internal static bool TryParsePrefixedDigits(ReadOnlySpan<char> digits, int numberBase, int sign, out BigInteger result)
    {
        if (digits.Length == 0)
        {
            result = default;
            return false;
        }

        result = BigInteger.Zero;

        foreach (var ch in digits)
        {
            int digit = ch switch
            {
                >= '0' and <= '9' => ch - '0',
                >= 'a' and <= 'f' => ch - 'a' + 10,
                >= 'A' and <= 'F' => ch - 'A' + 10,
                _ => -1
            };

            if (digit < 0 || digit >= numberBase)
            {
                result = default;
                return false;
            }

            result = (result * numberBase) + digit;
        }

        if (sign < 0)
            result = BigInteger.Negate(result);

        return true;
    }

    // public override string ToString() => value.ToString();
    internal static string ToString(BigInteger value) => value.ToString();

    // public override bool EqualsLiteral(double value) => (double)this.value == value;
    internal static bool EqualsLiteral(BigInteger left, double value) => (double)left == value;

    internal static BigInteger Negate(BigInteger value) => -value;

    internal static BigInteger BitwiseNot(BigInteger value) => ~value;

    internal static BigInteger Power(BigInteger left, BigInteger right)
    {
        var exponent = right;
        if (exponent.Sign < 0)
            throw new JsErrorException("RangeError", "Exponent must be positive");

        return BigInteger.Pow(left, (int)exponent);
    }

    internal static BigInteger BitwiseAnd(BigInteger left, BigInteger right) => left & right;

    internal static BigInteger BitwiseOr(BigInteger left, BigInteger right) => left | right;

    internal static BigInteger BitwiseXor(BigInteger left, BigInteger right) => left ^ right;

    // BigInt::leftShift(x, y): y > 0 shifts left (x × 2^y), y < 0 shifts right (arithmetic floor
    // division by 2^|y|). BigInt::signedRightShift(x, y) is leftShift(x, -y). BigInts are
    // arbitrary precision, so the shift count can exceed Int32 — the previous `(int)` / `(byte)`
    // casts overflowed (e.g. `123n >> 9999999999999n`) or mangled negative counts (`5n >> -1n`).
    internal static BigInteger LeftShift(BigInteger left, BigInteger right) => Shift(left, right);

    internal static BigInteger RightShift(BigInteger left, BigInteger right) => Shift(left, -right);

    internal static BigInteger Shift(BigInteger v, BigInteger bits)
    {
        if (bits.IsZero || v.IsZero)
            return v;

        if (bits.Sign > 0)
        {
            // Left shift. A count beyond Int32 would allocate an unusable BigInt.
            if (bits > int.MaxValue)
                throw new JsErrorException("RangeError", "Maximum BigInt size exceeded");
            return v << (int)bits;
        }

        // Right shift by |bits| (arithmetic, sign-preserving). Beyond the magnitude's bit length
        // the result collapses to 0 (non-negative) or -1 (negative); .NET's >> needs an Int32.
        var count = -bits;
        if (count > int.MaxValue)
            return v.Sign < 0 ? BigInteger.MinusOne : BigInteger.Zero;
        return v >> (int)count;
    }

    internal static BigInteger Multiply(BigInteger left, BigInteger right) => left * right;

    internal static BigInteger Divide(BigInteger left, BigInteger right)
    {
        var divisor = right;
        if (divisor.IsZero)
            throw new JsErrorException("RangeError", "Division by zero");
        return left / divisor;
    }

    internal static BigInteger Subtract(BigInteger left, BigInteger right) => left - right;

    internal static BigInteger Modulo(BigInteger left, BigInteger right)
    {
        var divisor = right;
        if (divisor.IsZero)
            throw new JsErrorException("RangeError", "Division by zero");
        return left % divisor;
    }

    // AddValue(JSValue value)'s BigInt + BigInt path: `if (value is JSBigInt b) return new JSBigInt(this.value + b.value);`
    internal static BigInteger AddValue(BigInteger left, BigInteger right) => left + right;

    internal static string BigIntegerToRadixString(BigInteger value, int radix)
    {
        if (radix == 10)
            return value.ToString();

        if (value.IsZero)
            return "0";

        const string digits = "0123456789abcdefghijklmnopqrstuvwxyz";
        var negative = value.Sign < 0;
        var magnitude = BigInteger.Abs(value);
        var builder = new StringBuilder();
        var bigRadix = new BigInteger(radix);

        while (!magnitude.IsZero)
        {
            magnitude = BigInteger.DivRem(magnitude, bigRadix, out var remainder);
            builder.Insert(0, digits[(int)remainder]);
        }

        if (negative)
            builder.Insert(0, '-');

        return builder.ToString();
    }

    internal static BigInteger AsIntN(long bits, BigInteger n)
    {
        var buffer = n.ToByteArray();

        if (buffer.Length * 8 < bits)
            return n;

        var reminderBits = bits % 8;
        var length = (int)(bits / 8);

        if (reminderBits > 0)
            length++;

        var copy = new byte[length];
        Buffer.BlockCopy(buffer, 0, copy, 0, length);

        if (reminderBits > 0)
        {
            // here we need to pad leftmost bits as 1s
            // as BigInteger uses bytes and only if the
            // eighth bit is 1, it will consider it as a
            // negative integer

            // so we need to create mask to first remove
            // bits as byte contains eight bits

            // then check the most significant digit
            // if it is negative, then we need to pad
            // 1s before it

            ref byte last = ref copy[copy.Length - 1];

            byte padMask = 0xFF;

            byte mask = 1;
            byte start = 1;

            reminderBits--;

            while (reminderBits > 0)
            {
                padMask &= (byte)~start;
                start <<= 1;
                start |= 1;
                mask <<= 1;
                reminderBits--;
            }

            last &= start;
            var lastValue = last;

            if ((mask & lastValue) > 0)
                last |= padMask;
        }

        var r = new BigInteger(copy);
        return r;
    }

    internal static BigInteger AsUintN(long bits, BigInteger n)
    {
        // BigInt.asUintN(bits, n): return n modulo 2**bits as a non-negative BigInt
        // (spec step 3). A negative n wraps into [0, 2**bits), e.g. asUintN(2, -1n) is
        // 3n and asUintN(64, -1n) is 18446744073709551615n.
        if (bits == 0)
            return BigInteger.Zero;

        // A non-negative value already below 2**bits needs no reduction; this also
        // avoids materialising 2**bits for the common in-range case.
        if (n.Sign >= 0 && n.GetBitLength() <= bits)
            return n;

        var modulus = BigInteger.One << (int)bits;
        var r = n % modulus;
        if (r.Sign < 0)
            r += modulus;

        return r;
    }
}
