// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   4
// Annotated:        4/4
// Exempt:           0
// Human-reviewed:   0/4
// IP risk:          Low
// Security risk:    Medium
// Criteria:         0/0
// Resource impact:  1/10 max
// Unverified:       4
//
// GENERATED - DO NOT EDIT MANUALLY

using System;

namespace Broiler.ECMAScript.Numbers;

/// <summary>
/// The numeric readings of a property key: array indices (ECMA-262 6.1.7) and
/// <c>CanonicalNumericIndexString</c> (7.1.21).
/// </summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 7.1.21; IP=Low; Security=Medium; Resources=1; Fingerprint=09C8FC
// Broiler-Human:        PENDING
public static class NumericKeys
{
    /// <summary>The largest array index, 2^32 - 2.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 7.1.21; IP=Low; Security=Medium; Resources=1; Fingerprint=56C4FD
    // Broiler-Human:        PENDING
    public const uint MaximumArrayIndex = 4294967294;

    /// <summary>Whether a key is an array index (ECMA-262 6.1.7): the canonical string of an integral Number from 0 to 2^32 - 2.</summary>
    /// <param name="key">The property key's string.</param>
    /// <param name="index">The index, when it is one.</param>
    /// <returns><see langword="true"/> for <c>"0"</c> and for a decimal integer with no leading zero up to 4294967294; nothing else.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 7.1.21; IP=Low; Security=Medium; Resources=1; Fingerprint=3C4B33
    // Broiler-Human:        PENDING
    public static bool TryParseArrayIndex(ReadOnlySpan<char> key, out uint index)
    {
        index = 0;
        if (key.IsEmpty || key.Length > 10)
            return false;

        if (key[0] == '0')
            return key.Length == 1;

        ulong value = 0;
        foreach (var c in key)
        {
            if (c is < '0' or > '9')
                return false;

            value = (value * 10) + (uint)(c - '0');
        }

        if (value > MaximumArrayIndex)
            return false;

        index = (uint)value;
        return true;
    }

    /// <summary><c>CanonicalNumericIndexString</c> (ECMA-262 7.1.21): whether a key is the canonical string of a Number.</summary>
    /// <param name="key">The property key's string.</param>
    /// <param name="value">The Number it names, when it names one: -0 for <c>"-0"</c>, and otherwise the key's <c>StringToNumber</c>.</param>
    /// <returns><see langword="true"/> when the key is <c>"-0"</c> or is <c>Number::toString</c> of its own <c>StringToNumber</c>.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 7.1.21; IP=Low; Security=Medium; Resources=1; Fingerprint=FE8951
    // Broiler-Human:        PENDING
    public static bool IsCanonicalNumericString(ReadOnlySpan<char> key, out double value)
    {
        value = double.NaN;
        if (key.SequenceEqual("-0"))
        {
            value = -0.0;
            return true;
        }

        // Number::toString writes a digit, "-", "I" (Infinity) or "N" (NaN) first and nothing
        // longer than its maximum: anything else cannot be canonical, and is refused unparsed.
        if (key.IsEmpty || key.Length > NumberText.MaximumLength || !(key[0] is (>= '0' and <= '9') or '-' or 'I' or 'N'))
            return false;

        if (TryParseArrayIndex(key, out var index))
        {
            value = index;
            return true;
        }

        var number = NumberParsing.StringToNumber(key);
        Span<char> canonical = stackalloc char[NumberText.MaximumLength];
        NumberText.TryFormat(number, canonical, out var written);
        if (!key.SequenceEqual(canonical[..written]))
            return false;

        value = number;
        return true;
    }
}
