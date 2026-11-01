// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   6
// Annotated:        6/6
// Exempt:           0
// Human-reviewed:   0/6
// IP risk:          Low
// Security risk:    High
// Criteria:         1/1
// Resource impact:  1/10 max
// Unverified:       6
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Broiler.ECMAScript.Intl;

/// <summary>
/// The readers of the text tables' format (<see cref="IntlTable"/>): their lines, a two-field table as a dictionary, a
/// line's fields, and a value's <c>\uXXXX</c> escapes. Ported from Broiler.VM's <c>JsIntlTables</c> at its SJL-9 rule commit
/// <c>4cf9ca44</c> (<c>Lines</c>, <c>Pairs</c>, <c>Unescape</c>), with its culture-invariant hexadecimal parse read through
/// <see cref="NumberFormatInfo.InvariantInfo"/> (JSD-0082, decision D19).
/// </summary>
// Broiler-AI:           Origin=Ported; Spec=JSD-0082 D8 table format via Broiler.VM JsIntlTables; IP=Low; Security=Medium; Resources=1; Fingerprint=F759FC
// Broiler-Human:        PENDING
internal static class IntlTableText
{
    /// <summary>A table value with its <c>\uXXXX</c> escapes resolved, each to the one UTF-16 code unit it names.</summary>
    /// <param name="value">The value, as a table field writes it.</param>
    /// <exception cref="FormatException">An escape's four characters are not hexadecimal digits: the table is not the generator's.</exception>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D8 table format via Broiler.VM JsIntlTables; IP=Low; Security=High; Resources=1; Fingerprint=9C6D2C
    // Broiler-Falsified-If: an escape \uXXXX resolves to another code unit than its four hexadecimal digits name, or a value without an escape is changed
    // Broiler-Human:        PENDING
    internal static string Unescape(string value)
    {
        if (value.IndexOf('\\', StringComparison.Ordinal) < 0)
        {
            return value;
        }

        var text = new StringBuilder(value.Length);

        for (var at = 0; at < value.Length; at++)
        {
            if (value[at] == '\\' && at + 5 < value.Length && value[at + 1] == 'u')
            {
                text.Append((char)int.Parse(value.AsSpan(at + 2, 4), NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo));
                at += 5;
                continue;
            }

            text.Append(value[at]);
        }

        return text.ToString();
    }

    /// <summary>The lines of a text table, the empty ones left out.</summary>
    /// <param name="table">The table's bytes, ASCII.</param>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D8 table format via Broiler.VM JsIntlTables; IP=Low; Security=Medium; Resources=1; Fingerprint=BDEB67
    // Broiler-Human:        PENDING
    internal static List<string> Lines(ReadOnlySpan<byte> table)
    {
        var text = Encoding.ASCII.GetString(table);
        var lines = new List<string>();

        foreach (var line in text.Split('\n'))
        {
            if (line.Length != 0)
            {
                lines.Add(line);
            }
        }

        return lines;
    }

    /// <summary>A two-field table as a dictionary: each line's text before its first <c>|</c>, mapped to the text after it; a later line wins.</summary>
    /// <param name="table">The table's bytes.</param>
    /// <param name="name">The table, for the message of a line with no <c>|</c>.</param>
    /// <exception cref="InvalidDataException">A line has no <c>|</c>.</exception>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D8 table format via Broiler.VM JsIntlTables; IP=Low; Security=Medium; Resources=1; Fingerprint=E5540D
    // Broiler-Human:        PENDING
    internal static Dictionary<string, string> Pairs(ReadOnlySpan<byte> table, IntlTable name)
    {
        var pairs = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var line in Lines(table))
        {
            var bar = Bar(line, name);
            pairs[line[..bar]] = line[(bar + 1)..];
        }

        return pairs;
    }

    /// <summary>A line's fields, separated by <c>|</c>; at least <paramref name="count"/> of them.</summary>
    /// <param name="line">The line.</param>
    /// <param name="count">The fields the table's format gives a line.</param>
    /// <param name="name">The table, for the message.</param>
    /// <exception cref="InvalidDataException">The line has fewer fields: the table is not in the format its <see cref="IntlTable"/> value states.</exception>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D8 table format via Broiler.VM JsIntlTables; IP=Low; Security=Medium; Resources=1; Fingerprint=0B3F68
    // Broiler-Human:        PENDING
    internal static string[] Fields(string line, int count, IntlTable name)
    {
        var fields = line.Split('|');

        if (fields.Length < count)
        {
            throw new InvalidDataException("a line of the " + name.ToString() + " table has fewer than its fields: " + line);
        }

        return fields;
    }

    /// <summary>The position of a line's first <c>|</c>.</summary>
    /// <param name="line">The line.</param>
    /// <param name="name">The table, for the message.</param>
    /// <exception cref="InvalidDataException">The line has no <c>|</c>.</exception>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D8 table format via Broiler.VM JsIntlTables; IP=Low; Security=Medium; Resources=1; Fingerprint=7FAC02
    // Broiler-Human:        PENDING
    internal static int Bar(string line, IntlTable name)
    {
        var bar = line.IndexOf('|', StringComparison.Ordinal);

        if (bar < 0)
        {
            throw new InvalidDataException("a line of the " + name.ToString() + " table has no field separator: " + line);
        }

        return bar;
    }
}
