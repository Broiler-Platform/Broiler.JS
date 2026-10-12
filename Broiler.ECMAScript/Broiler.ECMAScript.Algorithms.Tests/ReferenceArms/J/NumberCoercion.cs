// REFERENCE ARM J: a test-only copy of Broiler.JS's implementation before the cut-over, for the
// reference-arm comparison of bundle sjl-3-001 (docs/evidence/sjl-3-001/reference-arms.md). It is
// never referenced outside this test project, and it is retired at SJL-16.
//
// Copied from Broiler.JS at 44a214dc (bundle sjl-0-001's base):
//   Broiler.JS/Broiler.JavaScript.Parser/NumberCoercion.cs
//     blob 78360e1da8915f5d1fb40fe368c8f8fa747467f7: the whole class
//
// Changes from the source:
//   - line endings normalised to LF where the source has CRLF, as rule SL10 requires of the shared folder;
//   - the namespace; CoerceToNumber takes the string a StringSpan (an engine type) carries, and `input.AsSpan()` reads it;
//   - `char[] rented` annotated nullable for this project's nullable context; no result changes.

using System;
using System.Buffers;
using System.Globalization;
using System.Numerics;

namespace Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.J;

/// <summary>Allocation-free parsing for the scanner's validated numeric tokens.</summary>
internal static class NumberCoercion
{
    internal static double CoerceToNumber(string input)
    {
        var text = input.AsSpan();
        if (text.IsEmpty)
            return 0;

        if (IsLegacyOctal(text))
            return ParseSmallRadix(text[1..], 8);

        if (text.Length > 2 && text[0] == '0')
        {
            var radix = text[1] switch
            {
                'x' or 'X' => 16,
                'o' or 'O' => 8,
                'b' or 'B' => 2,
                _ => 0
            };
            if (radix != 0)
                return ParseRadix(text[2..], radix);
        }

        if (text.IndexOf('_') < 0)
            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var result)
                ? result
                : double.NaN;

        return ParseSeparatedDecimal(text);
    }

    private static bool IsLegacyOctal(ReadOnlySpan<char> text)
    {
        if (text.Length < 2 || text[0] != '0')
            return false;

        for (var i = 1; i < text.Length; i++)
        {
            if (text[i] is < '0' or > '7')
                return false;
        }
        return true;
    }

    private static double ParseRadix(ReadOnlySpan<char> text, int radix)
    {
        // The overwhelmingly common small literals stay entirely on the stack.
        if (text.Length <= 13)
            return ParseSmallRadix(text, radix);

        var value = BigInteger.Zero;
        foreach (var ch in text)
        {
            if (ch == '_')
                continue;
            value = value * radix + Digit(ch);
        }
        return (double)value;
    }

    private static double ParseSmallRadix(ReadOnlySpan<char> text, int radix)
    {
        double value = 0;
        foreach (var ch in text)
        {
            if (ch == '_')
                continue;
            value = value * radix + Digit(ch);
        }
        return value;
    }

    private static int Digit(char ch) => ch switch
    {
        >= '0' and <= '9' => ch - '0',
        >= 'a' and <= 'f' => ch - 'a' + 10,
        >= 'A' and <= 'F' => ch - 'A' + 10,
        _ => 0
    };

    private static double ParseSeparatedDecimal(ReadOnlySpan<char> text)
    {
        char[]? rented = null;
        Span<char> buffer = text.Length <= 256
            ? stackalloc char[text.Length]
            : (rented = ArrayPool<char>.Shared.Rent(text.Length));
        var length = 0;
        foreach (var ch in text)
        {
            if (ch != '_')
                buffer[length++] = ch;
        }

        var parsed = double.TryParse(buffer[..length], NumberStyles.Float, CultureInfo.InvariantCulture, out var result)
            ? result
            : double.NaN;
        if (rented != null)
            ArrayPool<char>.Shared.Return(rented);
        return parsed;
    }
}
