// DIFFERENTIAL CHECK, BROILER.VM'S ARM: a test-only copy of Broiler.VM's code at its SJL-9 rule commit, the
// reference the differential check of Broiler.VM's JSD-0082, decision D22, compares the library with. It is compiled
// into this test project and nowhere else, never ships, and runs under the JIT only; see Differential/Vm/README.md.
//
// Copied from Broiler.VM at 4cf9ca449fc96a1a5be810f6d0ca31c42b855c8f:
//   src/Broiler.VM.Profile.JavaScript/JsDecimal.cs
//     blob de0c9461fc759cb63a07024cc7cbb211a9725c83: the whole file: JsDecimalKind, JsDecimal, JsRawFormat and JsRawDecimal
//
// Changes from the source, none of which changes a result:
//   - the namespace;
//   - the generated SPDX and assurance header and the per-member assurance annotations removed.

using System;
using System.Globalization;
using Broiler.ECMAScript;
using Broiler.ECMAScript.Numbers;

namespace Broiler.ECMAScript.Intl.Tests.Differential.Vm;

/// <summary>What an Intl mathematical value is: a finite decimal, or one of ECMA-402's three others.</summary>
internal enum JsDecimalKind
{
    /// <summary>A finite value, negative zero among them.</summary>
    Finite,

    /// <summary><c>not-a-number</c>.</summary>
    NaN,

    /// <summary><c>positive-infinity</c>.</summary>
    PositiveInfinity,

    /// <summary><c>negative-infinity</c>.</summary>
    NegativeInfinity,
}

/// <summary>
/// An exact decimal value, the "Intl mathematical value" ECMA-402's number formatting works on: a
/// sign, the significant digits and where the decimal point falls among them (JSD-0044).
/// </summary>
/// <remarks>
/// <para>
/// <b>Digits, not a binary integer.</b> The value is <c>0.Digits * 10^Point</c>, with no leading or
/// trailing zero in <see cref="Digits"/>. Every operation ECMA-402 asks of it - scaling by a power
/// of ten, rounding at a decimal position to a multiple of an increment, and writing it out - is a
/// walk over that string, linear in its length, so a BigInt of a million bits formats in time
/// proportional to its digits and charges for each.
/// </para>
/// <para>
/// <b>The increment needs only the last four digits</b>: every increment ECMA-402 admits divides
/// 10,000, so the remainder of a count of units by the increment, and the parity of the quotient,
/// are read from at most five trailing digits.
/// </para>
/// </remarks>
internal readonly struct JsDecimal
{
    private JsDecimal(JsDecimalKind kind, bool negative, string digits, int point)
    {
        Kind = kind;
        Negative = negative;
        Digits = digits;
        Point = point;
    }

    internal JsDecimalKind Kind { get; }

    /// <summary>The sign of a finite value, negative zero's included.</summary>
    internal bool Negative { get; }

    /// <summary>The significant digits, without leading or trailing zeros; empty for zero.</summary>
    internal string Digits { get; }

    /// <summary>Where the decimal point falls: the value is <c>0.Digits * 10^Point</c>.</summary>
    internal int Point { get; }

    internal static JsDecimal NaN => new(JsDecimalKind.NaN, false, string.Empty, 0);

    internal static JsDecimal PositiveInfinity => new(JsDecimalKind.PositiveInfinity, false, string.Empty, 0);

    internal static JsDecimal NegativeInfinity => new(JsDecimalKind.NegativeInfinity, true, string.Empty, 0);

    internal static JsDecimal Zero => new(JsDecimalKind.Finite, false, string.Empty, 0);

    internal static JsDecimal NegativeZero => new(JsDecimalKind.Finite, true, string.Empty, 0);

    internal bool IsFinite => Kind == JsDecimalKind.Finite;

    internal bool IsZero => Kind == JsDecimalKind.Finite && Digits.Length == 0;

    /// <summary>The power of ten of the leading digit, <c>floor(log10(|x|))</c>, for a nonzero value.</summary>
    internal int Magnitude => Point - 1;

    /// <summary>A finite value from digits that may carry leading and trailing zeros.</summary>
    internal static JsDecimal Finite(bool negative, string digits, int point)
    {
        var first = 0;

        while (first < digits.Length && digits[first] == '0')
        {
            first++;
        }

        var last = digits.Length;

        while (last > first && digits[last - 1] == '0')
        {
            last--;
        }

        return first == last
            ? new JsDecimal(JsDecimalKind.Finite, negative, string.Empty, 0)
            : new JsDecimal(JsDecimalKind.Finite, negative, digits[first..last], point - first);
    }

    /// <summary>The value without its sign.</summary>
    internal JsDecimal Abs() => new(Kind == JsDecimalKind.NegativeInfinity ? JsDecimalKind.PositiveInfinity : Kind, false, Digits, Point);

    /// <summary>The value with its sign set.</summary>
    internal JsDecimal WithSign(bool negative) =>
        Kind switch
        {
            JsDecimalKind.Finite => new JsDecimal(Kind, negative, Digits, Point),
            JsDecimalKind.PositiveInfinity or JsDecimalKind.NegativeInfinity =>
                negative ? NegativeInfinity : PositiveInfinity,
            _ => this,
        };

    /// <summary>The value times <c>10^power</c>.</summary>
    internal JsDecimal Scale(int power) => IsZero || !IsFinite ? this : new JsDecimal(Kind, Negative, Digits, Point + power);

    /// <summary>Whether two values are the same value: kind, sign and digits.</summary>
    internal bool SameAs(JsDecimal other) =>
        Kind == other.Kind && Negative == other.Negative && Point == other.Point && string.Equals(Digits, other.Digits, StringComparison.Ordinal);

    /// <summary>The value a Number is: its shortest round-trip decimal, as ECMA-402's ToIntlMathematicalValue reads it.</summary>
    internal static JsDecimal FromNumber(double value)
    {
        if (double.IsNaN(value))
        {
            return NaN;
        }

        if (double.IsInfinity(value))
        {
            return value > 0 ? PositiveInfinity : NegativeInfinity;
        }

        if (value == 0)
        {
            return double.IsNegative(value) ? NegativeZero : Zero;
        }

        return ParseDecimal(JsNumberFormat.NumberToString(value)) ?? NaN;
    }

    /// <summary>The value a BigInt is, exactly.</summary>
    internal static JsDecimal FromBigInt(JsBigInt value, WorkCharge charge)
    {
        var text = value.ToDecimalString(charge);
        var negative = text.StartsWith('-');
        var digits = negative ? text[1..] : text;
        return Finite(negative, digits, digits.Length);
    }

    /// <summary>
    /// ECMA-402's ToIntlMathematicalValue for a String: its StringNumericLiteral read exactly, then
    /// held to the Number range as RoundMVResult rounds it.
    /// </summary>
    internal static JsDecimal FromString(string text, WorkCharge charge)
    {
        charge((ulong)text.Length / 8 + 1);
        var trimmed = JsNumberFormat.TrimWhiteSpace(text);

        if (trimmed.Length == 0)
        {
            return Zero;
        }

        switch (trimmed)
        {
            case "Infinity":
            case "+Infinity":
                return PositiveInfinity;
            case "-Infinity":
                return NegativeInfinity;
        }

        if (trimmed.Length > 2 && trimmed[0] == '0' && (trimmed[1] | 0x20) is 'x' or 'o' or 'b')
        {
            // A PREFIXED TEXT HAS NO SIGN, so one past the BigInt ceiling is past the Number range too.
            if (!JsBigInt.TryParseStringInteger(trimmed, charge, out var integer, out _))
            {
                return NaN;
            }

            return integer is null ? PositiveInfinity : FromBigInt(integer, charge).HeldToNumberRange();
        }

        return ParseDecimal(trimmed)?.HeldToNumberRange() ?? NaN;
    }

    /// <summary>
    /// A StrDecimalLiteral: an optional sign, digits with an optional point, and an optional
    /// exponent; nothing else.
    /// </summary>
    private static JsDecimal? ParseDecimal(string text)
    {
        var at = 0;
        var negative = false;

        if (at < text.Length && text[at] is '+' or '-')
        {
            negative = text[at] == '-';
            at++;
        }

        var digits = new System.Text.StringBuilder();
        var point = 0;
        var seenPoint = false;
        var anyDigit = false;

        for (; at < text.Length; at++)
        {
            var c = text[at];

            if (char.IsAsciiDigit(c))
            {
                anyDigit = true;
                digits.Append(c);

                if (!seenPoint)
                {
                    point++;
                }
            }
            else if (c == '.' && !seenPoint)
            {
                seenPoint = true;
            }
            else
            {
                break;
            }
        }

        if (!anyDigit)
        {
            return null;
        }

        long exponent = 0;

        if (at < text.Length && text[at] is 'e' or 'E')
        {
            at++;
            var exponentNegative = false;

            if (at < text.Length && text[at] is '+' or '-')
            {
                exponentNegative = text[at] == '-';
                at++;
            }

            var exponentDigits = 0;

            for (; at < text.Length && char.IsAsciiDigit(text[at]); at++)
            {
                exponentDigits++;

                // AN EXPONENT PAST A BILLION is past every Number's range in either direction, and is
                // held there rather than read further.
                if (exponent < 1_000_000_000)
                {
                    exponent = (exponent * 10) + (text[at] - '0');
                }
            }

            if (exponentDigits == 0)
            {
                return null;
            }

            if (exponentNegative)
            {
                exponent = -exponent;
            }
        }

        if (at != text.Length)
        {
            return null;
        }

        return Finite(negative, digits.ToString(), (int)Math.Clamp(point + exponent, -2_000_000_000L, 2_000_000_000L));
    }

    /// <summary>
    /// RoundMVResult's test: a value whose Number would be an infinity is that infinity, and one whose
    /// Number would be a zero is that zero; any other is kept exactly.
    /// </summary>
    private JsDecimal HeldToNumberRange()
    {
        if (!IsFinite || IsZero)
        {
            return this;
        }

        if (Magnitude > 309)
        {
            return Negative ? NegativeInfinity : PositiveInfinity;
        }

        if (Magnitude < -330)
        {
            return Negative ? NegativeZero : Zero;
        }

        // NEAR EITHER EDGE, THE NUMBER DECIDES: the digits are cut at 800 with a sticky 1 standing for
        // any that were cut, which keeps a correctly rounded parse on the same side of every tie.
        var shown = Digits.Length > 800 ? Digits[..800] + "1" : Digits;
        var number = double.Parse("0." + shown + "e" + Point.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);

        if (double.IsInfinity(number))
        {
            return Negative ? NegativeInfinity : PositiveInfinity;
        }

        return number == 0 ? (Negative ? NegativeZero : Zero) : this;
    }

    /// <summary>
    /// The count of units of <c>10^unit</c>, a multiple of <paramref name="increment"/>, that the
    /// rounding mode chooses for this nonnegative value: ECMA-402's choice between r1 and r2 in
    /// ToRawFixed and ToRawPrecision, through ApplyUnsignedRoundingMode.
    /// </summary>
    internal string RoundToUnits(int unit, int increment, UnsignedRoundingMode mode)
    {
        if (IsZero)
        {
            return "0";
        }

        // THE UNITS AT OR ABOVE THE POSITION, AND WHAT IS LEFT BELOW IT.
        var above = (long)Point - unit;
        string units;
        int tail;

        if (above <= 0)
        {
            units = "0";

            // EVERY DIGIT IS BELOW THE POSITION: the first one a zero when the point is further down.
            tail = above < 0 ? 1 : Classify(Digits);
        }
        else if (above < Digits.Length)
        {
            units = Digits[..(int)above];
            tail = Classify(Digits[(int)above..]);
        }
        else
        {
            units = Digits + new string('0', (int)(above - Digits.Length));
            tail = 0;
        }

        // THE REMAINDER BY THE INCREMENT AND THE QUOTIENT'S PARITY, from the last five digits:
        // twice every admitted increment divides 100,000.
        var low = int.Parse(units.Length > 5 ? units[^5..] : units, CultureInfo.InvariantCulture);
        var remainder = low % increment;
        var odd = (low % (2 * increment)) >= increment;

        bool up;

        if (remainder == 0 && tail == 0)
        {
            up = false;
        }
        else
        {
            var half = 2 * remainder > increment ? 1
                : 2 * remainder == increment ? (tail == 0 ? 0 : 1)
                : 2 * remainder == increment - 1 ? tail switch { 0 or 1 => -1, 2 => 0, _ => 1 }
                : -1;

            up = mode switch
            {
                UnsignedRoundingMode.Zero => false,
                UnsignedRoundingMode.Infinity => true,
                _ when half < 0 => false,
                _ when half > 0 => true,
                UnsignedRoundingMode.HalfZero => false,
                UnsignedRoundingMode.HalfInfinity => true,
                _ => odd,
            };
        }

        var result = Subtract(units, remainder);
        return up ? Add(result, increment) : result;
    }

    /// <summary>A tail of digits, with no trailing zero, against one half: 0 none, 1 below, 2 exactly, 3 above.</summary>
    private static int Classify(string tail) =>
        tail.Length == 0 ? 0
        : tail[0] < '5' ? 1
        : tail[0] > '5' ? 3
        : tail.Length == 1 ? 2 : 3;

    /// <summary>A digit string minus a small nonnegative number no greater than it, without leading zeros.</summary>
    private static string Subtract(string digits, int value)
    {
        if (value == 0)
        {
            return digits.TrimStart('0') is { Length: > 0 } trimmed ? trimmed : "0";
        }

        var chars = digits.ToCharArray();
        var borrow = value;

        for (var at = chars.Length - 1; at >= 0 && borrow != 0; at--)
        {
            var d = chars[at] - '0' - (borrow % 10);
            borrow /= 10;

            if (d < 0)
            {
                d += 10;
                borrow++;
            }

            chars[at] = (char)('0' + d);
        }

        var text = new string(chars).TrimStart('0');
        return text.Length == 0 ? "0" : text;
    }

    /// <summary>A digit string plus a small nonnegative number, without leading zeros.</summary>
    private static string Add(string digits, int value)
    {
        var chars = new System.Collections.Generic.List<char>(digits.Length + 5);
        chars.AddRange(digits);
        var carry = value;

        for (var at = chars.Count - 1; at >= 0 && carry != 0; at--)
        {
            var d = chars[at] - '0' + (carry % 10);
            carry /= 10;

            if (d >= 10)
            {
                d -= 10;
                carry++;
            }

            chars[at] = (char)('0' + d);
        }

        while (carry != 0)
        {
            chars.Insert(0, (char)('0' + (carry % 10)));
            carry /= 10;
        }

        var text = new string([.. chars]).TrimStart('0');
        return text.Length == 0 ? "0" : text;
    }
}

/// <summary>What ToRawFixed and ToRawPrecision answer.</summary>
internal readonly record struct JsRawFormat(string Formatted, JsDecimal Rounded, int IntegerDigits, int RoundingMagnitude);

/// <summary>ECMA-402's raw decimal formats of a nonnegative value.</summary>
internal static class JsRawDecimal
{
    /// <summary>ECMA-402's ToRawFixed.</summary>
    internal static JsRawFormat ToRawFixed(JsDecimal x, int minFraction, int maxFraction, int increment, UnsignedRoundingMode mode)
    {
        var f = maxFraction;
        var n = x.RoundToUnits(-f, increment, mode);
        var rounded = JsDecimal.Finite(false, n, n.Length - f);
        var m = n;
        int integerDigits;

        if (f != 0)
        {
            var k = m.Length;

            if (k <= f)
            {
                m = new string('0', f + 1 - k) + m;
                k = f + 1;
            }

            m = m[..(k - f)] + "." + m[(k - f)..];
            integerDigits = k - f;
        }
        else
        {
            integerDigits = m.Length;
        }

        m = Cut(m, maxFraction - minFraction);
        return new JsRawFormat(m, rounded, integerDigits, -f);
    }

    /// <summary>ECMA-402's ToRawPrecision.</summary>
    internal static JsRawFormat ToRawPrecision(JsDecimal x, int minPrecision, int maxPrecision, UnsignedRoundingMode mode)
    {
        var p = maxPrecision;
        string n;
        int e;
        JsDecimal rounded;

        if (x.IsZero)
        {
            n = new string('0', p);
            e = 0;
            rounded = JsDecimal.Zero;
        }
        else
        {
            e = x.Magnitude;
            n = x.RoundToUnits(e - p + 1, 1, mode);

            // ROUNDING UP CARRIED INTO A NEW DIGIT: the same value one magnitude higher.
            if (n.Length > p)
            {
                e++;
                n = n[..p];
            }

            rounded = JsDecimal.Finite(false, n, e + 1);
        }

        string m;
        int integerDigits;

        if (e >= p - 1)
        {
            m = n + new string('0', e - p + 1);
            integerDigits = e + 1;
        }
        else if (e >= 0)
        {
            m = n[..(e + 1)] + "." + n[(e + 1)..];
            integerDigits = e + 1;
        }
        else
        {
            m = "0." + new string('0', -(e + 1)) + n;
            integerDigits = 1;
        }

        if (m.Contains('.') && maxPrecision > minPrecision)
        {
            m = Cut(m, maxPrecision - minPrecision);
        }

        return new JsRawFormat(m, rounded, integerDigits, e - p + 1);
    }

    /// <summary>Removes up to <paramref name="cut"/> trailing zeros after a point, then a trailing point.</summary>
    private static string Cut(string m, int cut)
    {
        var end = m.Length;

        if (m.Contains('.'))
        {
            while (cut > 0 && end > 0 && m[end - 1] == '0')
            {
                end--;
                cut--;
            }

            if (end > 0 && m[end - 1] == '.')
            {
                end--;
            }
        }

        return m[..end];
    }
}
