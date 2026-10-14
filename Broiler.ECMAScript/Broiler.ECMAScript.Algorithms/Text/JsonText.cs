// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   7
// Annotated:        7/7
// Exempt:           1
// Human-reviewed:   0/7
// IP risk:          Low
// Security risk:    Medium
// Criteria:         3/0
// Resource impact:  2/10 max
// Unverified:       7
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using Broiler.ECMAScript.Numbers;

namespace Broiler.ECMAScript.Text;

/// <summary>
/// The text <c>JSON.stringify</c> writes that does not depend on the values it walks:
/// <c>QuoteJSONString</c> (ECMA-262 25.5.2.3) and the gap (25.5.2.1, steps 6 to 8).
/// </summary>
/// <remarks>
/// <b>The driver is the engine's</b> (contract convention 2): <c>toJSON</c>, the replacer, the
/// property list, the cycle check, the raw-JSON brand and the joining of members all read engine
/// objects. A line break the driver writes between members is a line feed, never the platform's.
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 25.5.2; IP=Low; Security=Medium; Resources=1; Fingerprint=2BC6BB
// Broiler-Human:        PENDING
public static class JsonText
{
    /// <summary>The longest gap: ten code units.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 25.5.2.1; IP=Low; Security=Low; Resources=1; Fingerprint=4ED3AF
    // Broiler-Human:        PENDING
    public const int MaximumGap = 10;

    /// <summary>
    /// <c>QuoteJSONString</c> (ECMA-262 25.5.2.3): the String in double quotes, with the short escapes,
    /// every other code unit below U+0020 and every lone surrogate as a lower-case <c>\u</c> escape.
    /// </summary>
    /// <param name="value">The String.</param>
    /// <param name="charge">The caller's meter: the String's length plus one, first; <see langword="null"/> for none.</param>
    /// <returns>The quoted String.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 25.5.2.3; IP=Low; Security=Medium; Resources=2; Fingerprint=EF4D2A
    // Broiler-Falsified-If: a code unit is escaped where the clause copies it, copied where the clause escapes it, or escaped in upper-case hexadecimal
    // Broiler-Human:        PENDING
    public static string Quote(ReadOnlySpan<char> value, WorkCharge? charge = null)
    {
        charge?.Invoke((ulong)value.Length + 1);
        var length = 2L;
        for (var at = 0; at < value.Length; at++)
            length += EscapedLength(value, at);

        ArgumentOutOfRangeException.ThrowIfGreaterThan(length, StringPadding.MaximumDotNetString, nameof(value));
        Span<char> output = length <= 1024 ? stackalloc char[(int)length] : new char[length];
        var to = 0;
        output[to++] = '"';
        for (var at = 0; at < value.Length; at++)
        {
            var unit = value[at];
            var shortEscape = ShortEscape(unit);
            if (shortEscape != 0)
            {
                output[to++] = '\\';
                output[to++] = shortEscape;
            }
            else if (unit < ' ' || Utf16.IsLoneSurrogateAt(value, at))
            {
                output[to++] = '\\';
                output[to++] = 'u';
                output[to++] = LowerHexDigits[(unit >> 12) & 0xF];
                output[to++] = LowerHexDigits[(unit >> 8) & 0xF];
                output[to++] = LowerHexDigits[(unit >> 4) & 0xF];
                output[to++] = LowerHexDigits[unit & 0xF];
            }
            else
            {
                output[to++] = unit;
            }
        }

        output[to++] = '"';
        return new string(output[..to]);
    }

    /// <summary>The gap a Number <c>space</c> gives (ECMA-262 25.5.2.1, step 7): that many spaces, from none to ten.</summary>
    /// <param name="space">The Number, after a Number object is unwrapped with <c>ToNumber</c>.</param>
    /// <returns><c>min(10, ToIntegerOrInfinity(space))</c> spaces, or the empty String when that is below one.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 25.5.2.1; IP=Low; Security=Low; Resources=1; Fingerprint=98AD82
    // Broiler-Falsified-If: the gap is not min(10, ToIntegerOrInfinity(space)) spaces, or is not empty below one
    // Broiler-Human:        PENDING
    public static string GapFromNumber(double space)
    {
        var count = Math.Min(MaximumGap, IntegerConversions.ToIntegerOrInfinity(space));
        return count < 1 ? string.Empty : new string(' ', (int)count);
    }

    /// <summary>The gap a String <c>space</c> gives (ECMA-262 25.5.2.1, step 8): its first ten code units.</summary>
    /// <param name="space">The String, after a String object is unwrapped with <c>ToString</c>.</param>
    /// <returns><paramref name="space"/> itself when it has ten code units or fewer; otherwise its first ten.</returns>
    /// <exception cref="ArgumentNullException">The String is <see langword="null"/>.</exception>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 25.5.2.1; IP=Low; Security=Low; Resources=1; Fingerprint=299CC8
    // Broiler-Falsified-If: the gap is longer than ten code units or is not the String's prefix
    // Broiler-Human:        PENDING
    public static string GapFromString(string space)
    {
        ArgumentNullException.ThrowIfNull(space);
        return space.Length <= MaximumGap ? space : space[..MaximumGap];
    }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 25.5.2.3; IP=Low; Security=Low; Resources=1; Fingerprint=113515
    // Broiler-Human:        PENDING
    private static int EscapedLength(ReadOnlySpan<char> value, int at)
    {
        var unit = value[at];
        if (ShortEscape(unit) != 0)
            return 2;

        return unit < ' ' || Utf16.IsLoneSurrogateAt(value, at) ? 6 : 1;
    }

    /// <summary>The character after the backslash of a short escape (ECMA-262 Table 76), or zero.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 25.5.2.3; IP=Low; Security=Low; Resources=1; Fingerprint=19A8D3
    // Broiler-Human:        PENDING
    private static char ShortEscape(char unit) => unit switch
    {
        '\b' => 'b',
        '\t' => 't',
        '\n' => 'n',
        '\f' => 'f',
        '\r' => 'r',
        '"' => '"',
        '\\' => '\\',
        _ => (char)0,
    };

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 25.5.2.3; IP=Low; Security=Low; Resources=1; Fingerprint=13F4E9
    // Broiler-Human:        PENDING
    private static ReadOnlySpan<char> LowerHexDigits => "0123456789abcdef";
}
