// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   15
// Annotated:        15/15
// Exempt:           0
// Human-reviewed:   0/15
// IP risk:          Low
// Security risk:    Low
// Criteria:         0/0
// Resource impact:  2/10 max
// Unverified:       15
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Globalization;
using System.Numerics;

namespace Broiler.ECMAScript.Conformance;

/// <summary>
/// The character classes and number texts of the corpus notation, decided on ASCII alone.
/// </summary>
/// <remarks>
/// The corpus's grammar is ASCII, so nothing here asks the platform what a letter or a digit is
/// (convention 9), and nothing formats a number through a culture: every number the notation
/// writes goes through <see cref="NumberFormatInfo.InvariantInfo"/>.
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=2; Fingerprint=0CEF22
// Broiler-Human:        PENDING
internal static class Ascii
{
    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=DCCBAF
    // Broiler-Human:        PENDING
    internal static bool IsDigit(char c) => c >= '0' && c <= '9';

    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=0439B6
    // Broiler-Human:        PENDING
    internal static bool IsUpperHex(char c) => IsDigit(c) || (c >= 'A' && c <= 'F');

    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=9EAA98
    // Broiler-Human:        PENDING
    internal static bool IsLowerHex(char c) => IsDigit(c) || (c >= 'a' && c <= 'f');

    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=258913
    // Broiler-Human:        PENDING
    internal static bool IsLower(char c) => c >= 'a' && c <= 'z';

    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=2A5F20
    // Broiler-Human:        PENDING
    internal static bool IsLetter(char c) => IsLower(c) || (c >= 'A' && c <= 'Z');

    /// <summary>Whether every character of <paramref name="text"/> passes <paramref name="test"/>, and there is one.</summary>
    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=57A369
    // Broiler-Human:        PENDING
    internal static bool All(ReadOnlySpan<char> text, Func<char, bool> test)
    {
        if (text.IsEmpty)
            return false;
        foreach (var c in text)
        {
            if (!test(c))
                return false;
        }

        return true;
    }

    /// <summary>A decimal integer with no leading zero, no plus sign and no <c>-0</c>: <c>0|-?[1-9][0-9]*</c>.</summary>
    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=CA1F22
    // Broiler-Human:        PENDING
    internal static bool IsCanonicalInteger(ReadOnlySpan<char> text)
    {
        if (text.SequenceEqual("0"))
            return true;
        if (!text.IsEmpty && text[0] == '-')
            text = text[1..];
        return !text.IsEmpty && text[0] != '0' && All(text, IsDigit);
    }

    /// <summary>A positive decimal integer with no leading zero: <c>[1-9][0-9]*</c>.</summary>
    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=60C5BA
    // Broiler-Human:        PENDING
    internal static bool IsPositiveInteger(ReadOnlySpan<char> text)
        => !text.IsEmpty && text[0] != '0' && All(text, IsDigit);

    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=5E6669
    // Broiler-Human:        PENDING
    internal static ulong ParseUpperHex(ReadOnlySpan<char> digits)
        => ulong.Parse(digits, NumberStyles.AllowHexSpecifier, NumberFormatInfo.InvariantInfo);

    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=3EB0C3
    // Broiler-Human:        PENDING
    internal static string UpperHex(ulong value, int digits)
        => value.ToString(digits == 16 ? "X16" : "X4", NumberFormatInfo.InvariantInfo);

    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=53227C
    // Broiler-Human:        PENDING
    internal static BigInteger ParseInteger(ReadOnlySpan<char> digits)
        => BigInteger.Parse(digits, NumberStyles.AllowLeadingSign, NumberFormatInfo.InvariantInfo);

    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=0C5002
    // Broiler-Human:        PENDING
    internal static string Decimal(BigInteger value) => value.ToString(NumberFormatInfo.InvariantInfo);

    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=B4AB36
    // Broiler-Human:        PENDING
    internal static string Decimal(int value) => value.ToString(NumberFormatInfo.InvariantInfo);

    /// <summary>
    /// A positive decimal integer that fits an <see cref="int"/>, or <see langword="false"/>.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=6E47F3
    // Broiler-Human:        PENDING
    internal static bool TryParsePositive(ReadOnlySpan<char> text, out int value)
    {
        value = 0;
        return IsPositiveInteger(text)
            && int.TryParse(text, NumberStyles.None, NumberFormatInfo.InvariantInfo, out value);
    }
}
