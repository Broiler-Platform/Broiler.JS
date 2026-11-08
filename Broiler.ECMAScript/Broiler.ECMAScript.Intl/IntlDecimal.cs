// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// HAIRS Code Assurance
// --------------------
// Relevant units:   27
// Annotated:        27/27
// Exempt:           10
// Human-reviewed:   0/27
// IP risk:          Low
// Security risk:    High
// Criteria:         3/3
// Resource impact:  3/10 max
// Unverified:       27
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Globalization;
using System.Numerics;
using System.Text;
using Broiler.ECMAScript.Numbers;

namespace Broiler.ECMAScript.Intl;

/// <summary>What an Intl mathematical value is (ECMA-402 ToIntlMathematicalValue, <c>sec-tointlmathematicalvalue</c>): a finite decimal, or one of the three others.</summary>
// Hairs-AI:           Origin=AI; Spec=ECMA-402 sec-tointlmathematicalvalue; IP=Low; Security=Low; Resources=0; Fingerprint=9380D6
// Hairs-Human:        PENDING
public enum IntlDecimalKind
{
    /// <summary>A finite value, negative zero among them.</summary>
    Finite = 0,

    /// <summary><c>not-a-number</c>.</summary>
    NaN = 1,

    /// <summary><c>positive-infinity</c>.</summary>
    PositiveInfinity = 2,

    /// <summary><c>negative-infinity</c>.</summary>
    NegativeInfinity = 3,
}

/// <summary>
/// An Intl mathematical value (ECMA-402 ToIntlMathematicalValue, <c>sec-tointlmathematicalvalue</c>): an exact decimal - a sign,
/// the significant digits and where the decimal point falls among them - or not-a-number, an infinity or negative zero, which
/// ECMA-402's number formatting works on (Broiler.VM's JSD-0082, sub-area (b), rows M1a to M3 and M8).
/// </summary>
/// <remarks>
/// <para>
/// <b>Digits, not a binary integer.</b> The value is <c>0.Digits * 10^Point</c>, with no leading or trailing zero in
/// <see cref="Digits"/>. Every operation ECMA-402 asks of it - scaling by a power of ten, rounding at a decimal position to a
/// multiple of an increment, and writing it out - is a walk over that string, linear in its length.
/// </para>
/// <para>
/// <b>The three sources are ECMA-402's.</b> A Number is its <c>Number::toString</c> digits (<see cref="FromNumber"/>, row M1a),
/// a BigInt its exact digits (<see cref="FromBigInt"/>, row M3), and a String its StringNumericLiteral read exactly and held to
/// the Number range as RoundMVResult holds it (<see cref="FromString"/>, rows M2a and M2b). ToPrimitive, which decides which of
/// them an argument is (row M1b), is the engine's.
/// </para>
/// <para>
/// <b>Ported from Broiler.VM's <c>JsDecimal</c></b> at its SJL-9 rule commit <c>4cf9ca44</c>, with
/// <see cref="NumberFormatInfo.InvariantInfo"/> where it parsed with the invariant culture (decision D19), the BigInt policy
/// a parameter where it read its engine's, and the caller's <see cref="WorkCharge"/> where it charged its engine's meter, in the
/// same units (decision D18).
/// </para>
/// </remarks>
// Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-tointlmathematicalvalue via Broiler.VM JsDecimal; IP=Low; Security=Medium; Resources=3; Fingerprint=C3145B
// Hairs-Human:        PENDING
public readonly record struct IntlDecimal
{
    private readonly string? digits;

    private IntlDecimal(IntlDecimalKind kind, bool isNegative, string digits, int point)
    {
        Kind = kind;
        IsNegative = isNegative;
        this.digits = digits;
        Point = point;
    }

    /// <summary>What the value is.</summary>
    public IntlDecimalKind Kind { get; }

    /// <summary>The sign of a finite value, negative zero's included, or of an infinity.</summary>
    public bool IsNegative { get; }

    /// <summary>The significant digits, ASCII, without leading or trailing zeros; empty for zero and for the three non-finite values.</summary>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-tointlmathematicalvalue via Broiler.VM JsDecimal; IP=Low; Security=Low; Resources=0; Fingerprint=C3319D
    // Hairs-Human:        PENDING
    public string Digits => digits ?? string.Empty;

    /// <summary>Where the decimal point falls: a finite value is <c>0.Digits * 10^Point</c>; 0 for zero.</summary>
    public int Point { get; }

    /// <summary><c>not-a-number</c>.</summary>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-tointlmathematicalvalue via Broiler.VM JsDecimal; IP=Low; Security=Low; Resources=0; Fingerprint=E64508
    // Hairs-Human:        PENDING
    public static IntlDecimal NaN => new(IntlDecimalKind.NaN, false, string.Empty, 0);

    /// <summary><c>positive-infinity</c>.</summary>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-tointlmathematicalvalue via Broiler.VM JsDecimal; IP=Low; Security=Low; Resources=0; Fingerprint=B67BA6
    // Hairs-Human:        PENDING
    public static IntlDecimal PositiveInfinity => new(IntlDecimalKind.PositiveInfinity, false, string.Empty, 0);

    /// <summary><c>negative-infinity</c>.</summary>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-tointlmathematicalvalue via Broiler.VM JsDecimal; IP=Low; Security=Low; Resources=0; Fingerprint=71EC81
    // Hairs-Human:        PENDING
    public static IntlDecimal NegativeInfinity => new(IntlDecimalKind.NegativeInfinity, true, string.Empty, 0);

    /// <summary>Zero.</summary>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-tointlmathematicalvalue via Broiler.VM JsDecimal; IP=Low; Security=Low; Resources=0; Fingerprint=8DAC69
    // Hairs-Human:        PENDING
    public static IntlDecimal Zero => new(IntlDecimalKind.Finite, false, string.Empty, 0);

    /// <summary><c>negative-zero</c>.</summary>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-tointlmathematicalvalue via Broiler.VM JsDecimal; IP=Low; Security=Low; Resources=0; Fingerprint=FC03B8
    // Hairs-Human:        PENDING
    public static IntlDecimal NegativeZero => new(IntlDecimalKind.Finite, true, string.Empty, 0);

    /// <summary>Whether the value is finite, negative zero included.</summary>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-tointlmathematicalvalue via Broiler.VM JsDecimal; IP=Low; Security=Low; Resources=0; Fingerprint=8068FD
    // Hairs-Human:        PENDING
    public bool IsFinite => Kind == IntlDecimalKind.Finite;

    /// <summary>Whether the value is zero or negative zero.</summary>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-tointlmathematicalvalue via Broiler.VM JsDecimal; IP=Low; Security=Low; Resources=0; Fingerprint=FE7914
    // Hairs-Human:        PENDING
    public bool IsZero => Kind == IntlDecimalKind.Finite && Digits.Length == 0;

    /// <summary>The power of ten of the leading digit, <c>floor(log10(|x|))</c>, for a nonzero finite value.</summary>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-tointlmathematicalvalue via Broiler.VM JsDecimal; IP=Low; Security=Low; Resources=0; Fingerprint=5E7EB6
    // Hairs-Human:        PENDING
    public int Magnitude => Point - 1;

    /// <summary>
    /// A finite value from ASCII digits that may carry leading and trailing zeros. Internal, as Broiler.VM's
    /// <c>JsDecimal.Finite</c> is: its one caller is FormatNumericToString's own rounding. A value's point is then bounded
    /// by the public sources alone - a Number's and a String's held to the Number range, a BigInt's charged for its
    /// digits - so that FormatNumericToString's <c>digits / 16 + 1</c> stays in proportion to the String it writes
    /// (decision D18).
    /// </summary>
    /// <param name="isNegative">The sign.</param>
    /// <param name="digits">The digits, ASCII <c>0</c> to <c>9</c>.</param>
    /// <param name="point">Where the decimal point falls: the value is <c>0.digits * 10^point</c>.</param>
    /// <returns>The value, its digits without leading or trailing zeros.</returns>
    /// <exception cref="ArgumentException"><paramref name="digits"/> holds a character that is not an ASCII digit.</exception>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-tointlmathematicalvalue via Broiler.VM JsDecimal; IP=Low; Security=Low; Resources=1; Fingerprint=4EFB5D
    // Hairs-Human:        PENDING
    internal static IntlDecimal Finite(bool isNegative, string digits, int point)
    {
        ArgumentNullException.ThrowIfNull(digits);

        foreach (var c in digits)
        {
            if (!char.IsAsciiDigit(c))
            {
                throw new ArgumentException("the digits are not ASCII digits", nameof(digits));
            }
        }

        return Trimmed(isNegative, digits, point);
    }

    /// <summary>The value without its sign.</summary>
    /// <returns>The value with a positive sign; <c>positive-infinity</c> for either infinity.</returns>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-tointlmathematicalvalue via Broiler.VM JsDecimal; IP=Low; Security=Low; Resources=1; Fingerprint=A57383
    // Hairs-Human:        PENDING
    public IntlDecimal Abs() => new(Kind == IntlDecimalKind.NegativeInfinity ? IntlDecimalKind.PositiveInfinity : Kind, false, Digits, Point);

    /// <summary>The value with its sign set; not-a-number is returned as it is.</summary>
    /// <param name="isNegative">The sign.</param>
    /// <returns>The value with that sign.</returns>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-tointlmathematicalvalue via Broiler.VM JsDecimal; IP=Low; Security=Low; Resources=1; Fingerprint=7B5F45
    // Hairs-Human:        PENDING
    public IntlDecimal WithSign(bool isNegative) =>
        Kind switch
        {
            IntlDecimalKind.Finite => new IntlDecimal(Kind, isNegative, Digits, Point),
            IntlDecimalKind.PositiveInfinity or IntlDecimalKind.NegativeInfinity => isNegative ? NegativeInfinity : PositiveInfinity,
            _ => this,
        };

    /// <summary>
    /// The value times <c>10^power</c>; zero and the non-finite values are returned as they are. Internal, as Broiler.VM's
    /// <c>JsDecimal.Scale</c> is: its callers pass a compact exponent ComputeExponent bounded by the value's own magnitude.
    /// </summary>
    /// <param name="power">The power of ten.</param>
    /// <returns>The scaled value.</returns>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-tointlmathematicalvalue via Broiler.VM JsDecimal; IP=Low; Security=Low; Resources=1; Fingerprint=ABBB03
    // Hairs-Human:        PENDING
    internal IntlDecimal Scale(int power) => IsZero || !IsFinite ? this : new IntlDecimal(Kind, IsNegative, Digits, Point + power);

    /// <summary>
    /// The value a Number is (ECMA-402 ToIntlMathematicalValue, <c>sec-tointlmathematicalvalue</c>, steps 3 to 6): its
    /// <c>Number::toString(x, 10)</c> digits, so that 1.15 is the decimal 1.15, not the exact binary value of the double
    /// (row M1a); <c>-0</c> is <c>negative-zero</c>.
    /// </summary>
    /// <param name="value">The Number.</param>
    /// <returns>The value.</returns>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-tointlmathematicalvalue via Broiler.VM JsDecimal; IP=Low; Security=High; Resources=1; Fingerprint=07012B
    // Hairs-Falsified-If: a Number's value differs from the decimal its Number::toString digits write
    // Hairs-Human:        PENDING
    public static IntlDecimal FromNumber(double value)
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

        return ParseDecimal(NumberText.ToString(value)) ?? NaN;
    }

    /// <summary>
    /// The value a BigInt is, exactly (ECMA-402 ToIntlMathematicalValue: "If _primValue_ is a BigInt, return the mathematical value of _primValue_";
    /// row M3).
    /// </summary>
    /// <param name="value">The BigInt.</param>
    /// <param name="charge">The caller's meter, charged by <c>BigIntText.ToString</c> for the decimal digits it writes, before it writes them.</param>
    /// <returns>The value.</returns>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-tointlmathematicalvalue via Broiler.VM JsDecimal; IP=Low; Security=Low; Resources=1; Fingerprint=0B17A4
    // Hairs-Human:        PENDING
    public static IntlDecimal FromBigInt(BigInteger value, WorkCharge? charge = null)
    {
        var text = BigIntText.ToString(value, 10, charge);
        var negative = text.StartsWith('-');
        var digits = negative ? text[1..] : text;
        return Trimmed(negative, digits, digits.Length);
    }

    /// <summary>
    /// The value a String is (ECMA-402 ToIntlMathematicalValue, steps 7 to 9, with StringIntlMV,
    /// <c>sec-runtime-semantics-stringintlmv</c>): its StringNumericLiteral read exactly, then held to the Number range as
    /// RoundMVResult holds it, a value whose Number would be an infinity or a zero being that infinity or zero (rows M2a and
    /// M2b). A text that is not a StringNumericLiteral is <c>not-a-number</c>.
    /// </summary>
    /// <param name="text">The String.</param>
    /// <param name="policy">The BigInt policy a <c>0x</c>, <c>0o</c> or <c>0b</c> literal is read under: one beyond it is
    /// <c>positive-infinity</c>, which every such literal past the Number range is anyway. Broiler.VM passes its BigInt policy,
    /// Broiler.JS <see cref="BigIntPolicy.Unbounded"/>.</param>
    /// <param name="charge">The caller's meter: <c>length / 8 + 1</c> units before anything is read (Broiler.VM's
    /// <c>JsDecimal.FromString</c>), and a prefixed literal's charges through <c>BigIntText</c>.</param>
    /// <returns>The value.</returns>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-tointlmathematicalvalue via Broiler.VM JsDecimal; IP=Low; Security=High; Resources=2; Fingerprint=2EE52F
    // Hairs-Falsified-If: a text outside StringNumericLiteral answers other than not-a-number, or a value that rounds to an infinity or a zero as a Number is kept as written
    // Hairs-Human:        PENDING
    public static IntlDecimal FromString(string text, BigIntPolicy policy, WorkCharge? charge = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        charge?.Invoke((ulong)text.Length / 8 + 1);
        var trimmed = NumberParsing.TrimStrWhiteSpace(text, TrimEnds.Both).ToString();

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
            switch (BigIntText.StringToBigInt(trimmed, policy, charge, out var integer, out _))
            {
                case ResultStatus.Completed:
                    return FromBigInt(integer, charge).HeldToNumberRange();
                case ResultStatus.LimitExceeded:
                    return PositiveInfinity;
                default:
                    return NaN;
            }
        }

        return ParseDecimal(trimmed)?.HeldToNumberRange() ?? NaN;
    }

    /// <summary>Whether two values are the same value: kind, sign, digits and point; <c>default</c> is <see cref="Zero"/>.</summary>
    /// <param name="other">The other value.</param>
    /// <returns>Whether they are the same.</returns>
    // Hairs-AI:           Origin=AI; Spec=ECMA-402 sec-tointlmathematicalvalue via Broiler.VM JsDecimal; IP=Low; Security=Low; Resources=1; Fingerprint=FACDB3
    // Hairs-Human:        PENDING
    public bool Equals(IntlDecimal other) =>
        Kind == other.Kind && IsNegative == other.IsNegative && Point == other.Point && string.Equals(Digits, other.Digits, StringComparison.Ordinal);

    /// <summary>A hash of the value, equal for values <see cref="Equals(IntlDecimal)"/> calls the same.</summary>
    /// <returns>The hash.</returns>
    public override int GetHashCode() => HashCode.Combine(Kind, IsNegative, Point, string.GetHashCode(Digits, StringComparison.Ordinal));

    /// <summary>The value as ECMA-402 writes a mathematical value in prose: digits with a point, an exponent where the point is far away, or the kind's name.</summary>
    /// <returns>For example <c>1.15</c>, <c>-0</c>, <c>1E+400</c>, <c>not-a-number</c>.</returns>
    // Hairs-AI:           Origin=AI; Spec=ECMA-402 sec-tointlmathematicalvalue via Broiler.VM JsDecimal; IP=Low; Security=Low; Resources=1; Fingerprint=3E71AD
    // Hairs-Human:        PENDING
    public override string ToString()
    {
        switch (Kind)
        {
            case IntlDecimalKind.NaN:
                return "not-a-number";
            case IntlDecimalKind.PositiveInfinity:
                return "positive-infinity";
            case IntlDecimalKind.NegativeInfinity:
                return "negative-infinity";
        }

        var sign = IsNegative ? "-" : string.Empty;

        if (IsZero)
        {
            return sign + "0";
        }

        if (Point is > 0 and <= 40 && Point >= Digits.Length)
        {
            return sign + Digits + new string('0', Point - Digits.Length);
        }

        if (Point is > 0 and <= 40)
        {
            return sign + Digits[..Point] + "." + Digits[Point..];
        }

        if (Point is <= 0 and > -40)
        {
            return sign + "0." + new string('0', -Point) + Digits;
        }

        return sign + "0." + Digits + "E" + (Point >= 0 ? "+" : string.Empty) + Point.ToString(NumberFormatInfo.InvariantInfo);
    }

    /// <summary>A finite value from ASCII digits that may carry leading and trailing zeros.</summary>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-tointlmathematicalvalue via Broiler.VM JsDecimal; IP=Low; Security=Low; Resources=1; Fingerprint=C3442B
    // Hairs-Human:        PENDING
    private static IntlDecimal Trimmed(bool negative, string digits, int point)
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
            ? new IntlDecimal(IntlDecimalKind.Finite, negative, string.Empty, 0)
            : new IntlDecimal(IntlDecimalKind.Finite, negative, digits[first..last], point - first);
    }

    /// <summary>
    /// A StrDecimalLiteral (ECMA-262 <c>sec-stringnumericliteral</c>): an optional sign, digits with an optional point, and an
    /// optional exponent; nothing else.
    /// </summary>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-tointlmathematicalvalue via Broiler.VM JsDecimal; IP=Low; Security=Medium; Resources=2; Fingerprint=791EDE
    // Hairs-Human:        PENDING
    private static IntlDecimal? ParseDecimal(string text)
    {
        var at = 0;
        var negative = false;

        if (at < text.Length && text[at] is '+' or '-')
        {
            negative = text[at] == '-';
            at++;
        }

        var digits = new StringBuilder();
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

                // AN EXPONENT PAST A BILLION is past every Number's range in either direction, and is held there rather
                // than read further.
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

        return Trimmed(negative, digits.ToString(), (int)Math.Clamp(point + exponent, -2_000_000_000L, 2_000_000_000L));
    }

    /// <summary>
    /// RoundMVResult's test (ToIntlMathematicalValue step 9): a value whose Number would be an infinity is that infinity, and
    /// one whose Number would be a zero is that zero; any other is kept exactly.
    /// </summary>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-tointlmathematicalvalue via Broiler.VM JsDecimal; IP=Low; Security=Medium; Resources=2; Fingerprint=01D323
    // Hairs-Human:        PENDING
    private IntlDecimal HeldToNumberRange()
    {
        if (!IsFinite || IsZero)
        {
            return this;
        }

        if (Magnitude > 309)
        {
            return IsNegative ? NegativeInfinity : PositiveInfinity;
        }

        if (Magnitude < -330)
        {
            return IsNegative ? NegativeZero : Zero;
        }

        // NEAR EITHER EDGE, THE NUMBER DECIDES: the digits are cut at 800 with a sticky 1 standing for any that were cut,
        // which keeps a correctly rounded parse on the same side of every tie.
        var shown = Digits.Length > 800 ? Digits[..800] + "1" : Digits;
        var number = double.Parse("0." + shown + "e" + Point.ToString(NumberFormatInfo.InvariantInfo), NumberStyles.Float, NumberFormatInfo.InvariantInfo);

        if (double.IsInfinity(number))
        {
            return IsNegative ? NegativeInfinity : PositiveInfinity;
        }

        return number == 0 ? (IsNegative ? NegativeZero : Zero) : this;
    }

    /// <summary>
    /// The count of units of <c>10^unit</c>, a multiple of <paramref name="increment"/>, that the rounding mode chooses for
    /// this nonnegative value: ECMA-402's choice between <c>r1</c> and <c>r2</c> in ToRawFixed and ToRawPrecision, through
    /// ApplyUnsignedRoundingMode (<c>sec-applyunsignedroundingmode</c>).
    /// </summary>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-tointlmathematicalvalue via Broiler.VM JsDecimal; IP=Low; Security=High; Resources=2; Fingerprint=9BA72E
    // Hairs-Falsified-If: a tie, a value just above or just below a tie, or an exact multiple is rounded otherwise than ApplyUnsignedRoundingMode states, for any admitted increment
    // Hairs-Human:        PENDING
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

        // THE REMAINDER BY THE INCREMENT AND THE QUOTIENT'S PARITY, from the last five digits: twice every admitted
        // increment divides 100,000.
        var low = int.Parse(units.Length > 5 ? units[^5..] : units, NumberStyles.None, NumberFormatInfo.InvariantInfo);
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
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-tointlmathematicalvalue via Broiler.VM JsDecimal; IP=Low; Security=Low; Resources=1; Fingerprint=F48FE4
    // Hairs-Human:        PENDING
    private static int Classify(string tail) =>
        tail.Length == 0 ? 0
        : tail[0] < '5' ? 1
        : tail[0] > '5' ? 3
        : tail.Length == 1 ? 2 : 3;

    /// <summary>A digit string minus a small nonnegative number no greater than it, without leading zeros.</summary>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-tointlmathematicalvalue via Broiler.VM JsDecimal; IP=Low; Security=Low; Resources=1; Fingerprint=C367C6
    // Hairs-Human:        PENDING
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
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-tointlmathematicalvalue via Broiler.VM JsDecimal; IP=Low; Security=Low; Resources=1; Fingerprint=A63E16
    // Hairs-Human:        PENDING
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
