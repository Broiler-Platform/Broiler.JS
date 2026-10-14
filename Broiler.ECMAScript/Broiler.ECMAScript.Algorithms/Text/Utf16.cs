// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   14
// Annotated:        14/14
// Exempt:           0
// Human-reviewed:   0/14
// IP risk:          Low
// Security risk:    Medium
// Criteria:         0/0
// Resource impact:  2/10 max
// Unverified:       14
//
// GENERATED - DO NOT EDIT MANUALLY

using System;

namespace Broiler.ECMAScript.Text;

/// <summary>What <c>CodePointAt</c> (ECMA-262 11.1.4) answers about the code point at one index of a String.</summary>
/// <param name="CodePoint">The code point: a pair's supplementary code point, or the code unit itself.</param>
/// <param name="CodeUnitCount">How many code units it occupies, 1 or 2.</param>
/// <param name="IsUnpairedSurrogate">Whether the code unit is a surrogate without its partner.</param>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 11.1.4; IP=Low; Security=Low; Resources=1; Fingerprint=07CBBA
// Broiler-Human:        PENDING
public readonly record struct CodePointRecord(int CodePoint, int CodeUnitCount, bool IsUnpairedSurrogate);

/// <summary>
/// A String as the language sees it, a sequence of UTF-16 code units: <c>CodePointAt</c> (ECMA-262
/// 11.1.4), <c>IsStringWellFormedUnicode</c> (6.1.4), <c>String.prototype.toWellFormed</c>'s
/// replacement (22.1.3.31) and <c>AdvanceStringIndex</c> (22.2.7.3).
/// </summary>
/// <remarks>
/// <para>
/// <b>A lone surrogate is a legal String</b> and an illegal sequence of scalar values, so nothing
/// here decodes UTF-16 through the base class library, which replaces or refuses one: every member
/// reads code units.
/// </para>
/// <para>
/// <c>TrimString</c> (22.1.3.32.1) is <see cref="Numbers.NumberParsing.TrimStrWhiteSpace"/>, beside
/// the <c>StrWhiteSpaceChar</c> set it trims, which milestone SJL-3 moved here.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.4; IP=Low; Security=Medium; Resources=1; Fingerprint=C81133
// Broiler-Human:        PENDING
public static class Utf16
{
    /// <summary>The code unit <c>toWellFormed</c> writes for each lone surrogate, U+FFFD REPLACEMENT CHARACTER.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 22.1.3.31; IP=Low; Security=Low; Resources=1; Fingerprint=524898
    // Broiler-Human:        PENDING
    public const char ReplacementCharacter = (char)0xFFFD;

    /// <summary><c>CodePointAt</c> (ECMA-262 11.1.4): the code point at <paramref name="position"/>.</summary>
    /// <param name="text">The String.</param>
    /// <param name="position">An index of a code unit of <paramref name="text"/>.</param>
    /// <returns>The code point, how many code units it occupies, and whether it is a lone surrogate.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The position is not an index of the text.</exception>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 11.1.4; IP=Low; Security=Medium; Resources=1; Fingerprint=3261BC
    // Broiler-Human:        PENDING
    public static CodePointRecord CodePointAt(ReadOnlySpan<char> text, int position)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(position);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(position, text.Length);

        var first = text[position];
        if (!IsSurrogate(first))
            return new CodePointRecord(first, 1, false);

        if (IsTrailing(first) || position + 1 == text.Length || !IsTrailing(text[position + 1]))
            return new CodePointRecord(first, 1, true);

        return new CodePointRecord(Combine(first, text[position + 1]), 2, false);
    }

    /// <summary><c>IsStringWellFormedUnicode</c> (ECMA-262 6.1.4): whether every surrogate is half of a pair.</summary>
    /// <param name="text">The String.</param>
    /// <param name="charge">The caller's meter: charged the text's length plus one, first; <see langword="null"/> for none.</param>
    /// <returns><see langword="true"/> when the text holds no lone surrogate.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.4; IP=Low; Security=Medium; Resources=1; Fingerprint=66D914
    // Broiler-Human:        PENDING
    public static bool IsWellFormed(ReadOnlySpan<char> text, WorkCharge? charge = null)
    {
        charge?.Invoke((ulong)text.Length + 1);
        return FirstLoneSurrogate(text) < 0;
    }

    /// <summary>
    /// <c>String.prototype.toWellFormed</c>'s result (ECMA-262 22.1.3.31, steps 4 to 7): the String
    /// with each lone surrogate replaced by <see cref="ReplacementCharacter"/>.
    /// </summary>
    /// <param name="text">The String.</param>
    /// <param name="charge">The caller's meter: charged the text's length plus one, first; <see langword="null"/> for none.</param>
    /// <returns><paramref name="text"/> itself when it is well formed; otherwise a new String of the same length.</returns>
    /// <exception cref="ArgumentNullException">The text is <see langword="null"/>.</exception>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 22.1.3.31; IP=Low; Security=Medium; Resources=2; Fingerprint=E58D75
    // Broiler-Human:        PENDING
    public static string ToWellFormed(string text, WorkCharge? charge = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        charge?.Invoke((ulong)text.Length + 1);

        var first = FirstLoneSurrogate(text);
        if (first < 0)
            return text;

        return string.Create(text.Length, (text, first), static (destination, state) =>
        {
            var (source, from) = state;
            source.AsSpan(0, from).CopyTo(destination);
            for (var at = from; at < source.Length; at++)
            {
                var unit = source[at];
                if (!IsSurrogate(unit))
                {
                    destination[at] = unit;
                }
                else if (IsLeading(unit) && at + 1 < source.Length && IsTrailing(source[at + 1]))
                {
                    destination[at] = unit;
                    destination[at + 1] = source[at + 1];
                    at++;
                }
                else
                {
                    destination[at] = ReplacementCharacter;
                }
            }
        });
    }

    /// <summary>
    /// <c>AdvanceStringIndex</c> (ECMA-262 22.2.7.3): the index after <paramref name="index"/>,
    /// past a whole surrogate pair when <paramref name="unicode"/> is set.
    /// </summary>
    /// <param name="text">The String.</param>
    /// <param name="index">An index, from zero to 2^53 - 1; it may be past the end of the text.</param>
    /// <param name="unicode">Whether the regular expression reads code points (its <c>u</c> or <c>v</c> flag).</param>
    /// <returns><paramref name="index"/> plus one, or plus two at the start of a pair in Unicode mode.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is negative or above 2^53 - 1.</exception>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 22.2.7.3; IP=Low; Security=Medium; Resources=1; Fingerprint=86BFC9
    // Broiler-Human:        PENDING
    public static long AdvanceStringIndex(ReadOnlySpan<char> text, long index, bool unicode)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, MaximumSafeInteger);

        if (!unicode || index + 1 >= text.Length)
            return index + 1;

        return index + CodePointAt(text, (int)index).CodeUnitCount;
    }

    /// <summary>The index of the first lone surrogate, or -1.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.4; IP=Low; Security=Medium; Resources=1; Fingerprint=3E3D39
    // Broiler-Human:        PENDING
    internal static int FirstLoneSurrogate(ReadOnlySpan<char> text)
    {
        for (var at = 0; at < text.Length; at++)
        {
            var unit = text[at];
            if (!IsSurrogate(unit))
                continue;

            if (IsTrailing(unit) || at + 1 == text.Length || !IsTrailing(text[at + 1]))
                return at;

            at++;
        }

        return -1;
    }

    /// <summary>Whether the code unit at <paramref name="at"/> is a surrogate without its partner.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 11.1.4; IP=Low; Security=Medium; Resources=1; Fingerprint=C131B0
    // Broiler-Human:        PENDING
    internal static bool IsLoneSurrogateAt(ReadOnlySpan<char> text, int at)
    {
        var unit = text[at];
        if (IsLeading(unit))
            return at + 1 == text.Length || !IsTrailing(text[at + 1]);

        if (IsTrailing(unit))
            return at == 0 || !IsLeading(text[at - 1]);

        return false;
    }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 11.1.3; IP=Low; Security=Low; Resources=1; Fingerprint=05B269
    // Broiler-Human:        PENDING
    internal static bool IsSurrogate(char unit) => unit is >= (char)0xD800 and <= (char)0xDFFF;

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 11.1.3; IP=Low; Security=Low; Resources=1; Fingerprint=01D73A
    // Broiler-Human:        PENDING
    internal static bool IsLeading(char unit) => unit is >= (char)0xD800 and <= (char)0xDBFF;

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 11.1.3; IP=Low; Security=Low; Resources=1; Fingerprint=A9EECF
    // Broiler-Human:        PENDING
    internal static bool IsTrailing(char unit) => unit is >= (char)0xDC00 and <= (char)0xDFFF;

    /// <summary><c>UTF16SurrogatePairToCodePoint</c> (ECMA-262 11.1.3).</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 11.1.3; IP=Low; Security=Low; Resources=1; Fingerprint=D175CF
    // Broiler-Human:        PENDING
    internal static int Combine(char lead, char trail) => ((lead - 0xD800) * 0x400) + (trail - 0xDC00) + 0x10000;

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 6.1.6.1; IP=Low; Security=Low; Resources=1; Fingerprint=A9573C
    // Broiler-Human:        PENDING
    private const long MaximumSafeInteger = (1L << 53) - 1;
}
