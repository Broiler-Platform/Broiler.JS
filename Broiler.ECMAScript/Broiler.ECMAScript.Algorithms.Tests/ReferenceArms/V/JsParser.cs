// REFERENCE ARM V: a test-only copy of Broiler.VM's implementation before the cut-over, for the
// reference-arm comparison of bundle sjl-3-001 (docs/evidence/sjl-3-001/reference-arms.md). It is
// never referenced outside this test project, and it is retired at SJL-16.
//
// Copied from Broiler.VM at 0087e8d7 (bundle sjl-0-001's base):
//   src/Broiler.VM.Profile.JavaScript.Compiler/JsParser.cs
//     blob 56528f751c592a7288f9f7f496e9fd6b0e5895b9: TryBigIntValue and NumberText
//
// Changes from the source, none of which changes a result:
//   - the namespace; the two members sit in a static class of their own; both made internal so the arm can call them;
//   - the generated assurance header and the per-member assurance annotations removed;
//   - TryBigIntValue takes the token's raw text instead of the token (SliceToken, an engine type), and its ceiling, Format.JsFormat.CeilingBigIntConstantBytes * 8 (65536 bits at the base), as the parameter maximumBits instead of a constant: the rule's population passes 64, 1024 and 65536 bits as the caller's parameter.

namespace Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.V;

/// <summary>The compiler's own copies of number rendering and BigInt literal values.</summary>
internal static class JsParser
{
    /// <summary>
    /// The exact integer a well-formed BigInt literal token spells, or false when it is wider than
    /// a BigInt constant may be.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The tokenizer has already refused every malformed spelling</b>, so what is left is a
    /// decimal integer with no leading zero, or a <c>0x</c>, <c>0o</c> or <c>0b</c> integer, with
    /// separators only between digits, and the <c>n</c>.
    /// </para>
    /// <para>
    /// <b>The width is bounded before a digit is converted.</b> A decimal literal of more digits
    /// than the widest constant has is refused on its length, so no conversion is ever asked of a
    /// text the format could not carry; a prefixed literal's width is its digit count times the
    /// digit's bits. What passes is converted exactly and checked once more on its bit length.
    /// </para>
    /// </remarks>
    internal static bool TryBigIntValue(string rawText, long maximumBits, out System.Numerics.BigInteger value)
    {
        value = System.Numerics.BigInteger.Zero;
        var text = rawText[..^1].Replace("_", string.Empty, System.StringComparison.Ordinal);

        var radix = text.Length > 1 && text[0] == '0'
            ? text[1] switch { 'x' or 'X' => 16, 'o' or 'O' => 8, _ => 2 }
            : 10;

        if (radix != 10)
        {
            var digits = text[2..].TrimStart('0');
            var bitsPerDigit = radix switch { 16 => 4, 8 => 3, _ => 1 };

            if ((long)digits.Length * bitsPerDigit > maximumBits + bitsPerDigit)
            {
                return false;
            }

            // THE BITS ARE PACKED LEAST SIGNIFICANT FIRST AND CONVERTED ONCE, so the cost is linear
            // in the literal: shifting an accumulator per digit would copy it once per digit.
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

            value = new System.Numerics.BigInteger(bytes, isUnsigned: true, isBigEndian: false);
        }
        else
        {
            // 65,536 bits is 19,729 decimal digits, and one more is always wider.
            if (text.Length > (int)(maximumBits * 30103L / 100000L) + 1)
            {
                return false;
            }

            value = System.Numerics.BigInteger.Parse(
                text,
                System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture);
        }

        return value.GetBitLength() <= maximumBits;
    }

    /// <summary>
    /// The specification's <c>Number::toString</c> in radix 10, which is what a numeric property
    /// key names: <c>{ 0.0000001: 1 }</c> has the key <c>"1e-7"</c>.
    /// </summary>
    /// <remarks>
    /// The digits are the shortest that round-trip, read off the platform's <c>"R"</c> form, and
    /// placed by the specification's rules rather than the platform's: until 2026-10-03 a key was
    /// the platform's spelling, <c>"1E-07"</c>, which names a different property (JSC-253).
    /// </remarks>
    internal static string NumberText(double value)
    {
        if (double.IsNaN(value))
        {
            return "NaN";
        }

        if (value == 0)
        {
            return "0";
        }

        if (double.IsInfinity(value))
        {
            return value > 0 ? "Infinity" : "-Infinity";
        }

        if (value < 0)
        {
            return "-" + NumberText(-value);
        }

        var round = value.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
        string digits;
        int point;
        var e = round.IndexOfAny(['E', 'e']);

        if (e >= 0)
        {
            var mantissa = round[..e];
            var exponent = int.Parse(round[(e + 1)..], System.Globalization.CultureInfo.InvariantCulture);
            var dot = mantissa.IndexOf('.');
            digits = mantissa.Replace(".", string.Empty, System.StringComparison.Ordinal);
            point = (dot < 0 ? mantissa.Length : dot) + exponent;
        }
        else
        {
            var dot = round.IndexOf('.');
            var whole = dot < 0 ? round : round[..dot];
            var fraction = dot < 0 ? string.Empty : round[(dot + 1)..];
            digits = whole + fraction;
            point = whole.Length;
        }

        var lead = 0;

        while (lead < digits.Length - 1 && digits[lead] == '0')
        {
            lead++;
        }

        digits = digits[lead..].TrimEnd('0');
        point -= lead;

        var k = digits.Length;
        var n = point;

        if (k <= n && n <= 21)
        {
            return digits + new string('0', n - k);
        }

        if (0 < n && n <= 21)
        {
            return digits[..n] + "." + digits[n..];
        }

        if (-6 < n && n <= 0)
        {
            return "0." + new string('0', -n) + digits;
        }

        var shown = n - 1;
        var sign = shown < 0 ? "-" : "+";
        var magnitude = System.Math.Abs(shown).ToString(System.Globalization.CultureInfo.InvariantCulture);

        return k == 1
            ? digits + "e" + sign + magnitude
            : digits[..1] + "." + digits[1..] + "e" + sign + magnitude;
    }
}
