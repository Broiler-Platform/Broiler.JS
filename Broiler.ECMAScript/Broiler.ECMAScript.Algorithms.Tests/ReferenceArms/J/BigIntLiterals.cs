// REFERENCE ARM J: a test-only copy of Broiler.JS's implementation before the cut-over, for the
// reference-arm comparison of bundle sjl-3-001 (docs/evidence/sjl-3-001/reference-arms.md). It is
// never referenced outside this test project, and it is retired at SJL-16.
//
// Copied from Broiler.JS at 44a214dc (bundle sjl-0-001's base):
//   Broiler.JS/Broiler.JavaScript.BuiltIns/BigInt/JSBigInt.cs
//     blob 95e8e45863b279eb232187d0d6fed229c2779913: TryParseBigIntLiteral and TryParsePrefixedDigits
//   Broiler.JS/Broiler.JavaScript.Compiler/Declarations/FastCompiler.CreateClass.cs
//     blob 0b21426a4621b12536aa3334911e01398ca46dac: BigIntLiteralToValue and ParseBigIntRadix
//
// Changes from the source:
//   - line endings normalised to LF where the source has CRLF, as rule SL10 requires of the shared folder;
//   - the namespace; each pair sits in a static class named for its source class; the two entry points made internal; no result changes.

using System;
using System.Numerics;

namespace Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.J;

/// <summary>The BigInt built-in's literal reader.</summary>
internal static class JSBigInt
{
    internal static bool TryParseBigIntLiteral(string value, out BigInteger result)
    {
        var text = value.Trim().TrimEnd('n').Replace("_", "");
        var sign = 1;

        if (text.StartsWith("+", StringComparison.Ordinal))
            text = text[1..];
        else if (text.StartsWith("-", StringComparison.Ordinal))
        {
            sign = -1;
            text = text[1..];
        }

        if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            return TryParsePrefixedDigits(text.AsSpan(2), 16, sign, out result);

        if (text.StartsWith("0b", StringComparison.OrdinalIgnoreCase))
            return TryParsePrefixedDigits(text.AsSpan(2), 2, sign, out result);

        if (text.StartsWith("0o", StringComparison.OrdinalIgnoreCase))
            return TryParsePrefixedDigits(text.AsSpan(2), 8, sign, out result);

        return BigInteger.TryParse(sign < 0 ? "-" + text : text, out result);
    }

    private static bool TryParsePrefixedDigits(ReadOnlySpan<char> digits, int numberBase, int sign, out BigInteger result)
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
}

/// <summary>The compiler's BigInt literal reader for class property names.</summary>
internal static class CreateClass
{
    // A BigInt literal property name uses the canonical value of its numeric part
    // (`0n` → 0, `0x10n` → 16); the `n` suffix only marks the literal as a BigInt.
    // Handles every radix prefix and `_` separators.
    internal static System.Numerics.BigInteger BigIntLiteralToValue(string literalText)
    {
        var text = literalText.Trim();
        if (text.EndsWith('n'))
            text = text[..^1];
        text = text.Replace("_", "");

        System.Numerics.BigInteger value;
        if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            value = ParseBigIntRadix(text.AsSpan(2), 16);
        else if (text.StartsWith("0o", StringComparison.OrdinalIgnoreCase))
            value = ParseBigIntRadix(text.AsSpan(2), 8);
        else if (text.StartsWith("0b", StringComparison.OrdinalIgnoreCase))
            value = ParseBigIntRadix(text.AsSpan(2), 2);
        else
            value = text.Length == 0
                ? System.Numerics.BigInteger.Zero
                : System.Numerics.BigInteger.Parse(text, System.Globalization.CultureInfo.InvariantCulture);

        return value;
    }

    private static System.Numerics.BigInteger ParseBigIntRadix(ReadOnlySpan<char> digits, int radix)
    {
        System.Numerics.BigInteger value = System.Numerics.BigInteger.Zero;
        foreach (var c in digits)
        {
            int d = c <= '9' ? c - '0' : char.ToLowerInvariant(c) - 'a' + 10;
            value = value * radix + d;
        }

        return value;
    }
}
