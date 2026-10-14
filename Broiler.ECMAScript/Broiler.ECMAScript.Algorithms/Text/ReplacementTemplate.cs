// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   4
// Annotated:        4/4
// Exempt:           7
// Human-reviewed:   0/4
// IP risk:          Low
// Security risk:    Medium
// Criteria:         0/0
// Resource impact:  2/10 max
// Unverified:       4
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Generic;

namespace Broiler.ECMAScript.Text;

/// <summary>What one part of a replacement template stands for (ECMA-262 22.1.3.19.1 <c>GetSubstitution</c>).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 22.1.3.19.1; IP=Low; Security=Low; Resources=1; Fingerprint=A5A661
// Broiler-Human:        PENDING
public enum ReplacementPartKind
{
    /// <summary>Text of the template, copied as it is: <see cref="ReplacementPart.Start"/> and <see cref="ReplacementPart.Length"/> name it.</summary>
    Literal = 0,

    /// <summary><c>$$</c>: one dollar sign.</summary>
    Dollar = 1,

    /// <summary><c>$&amp;</c>: the matched substring.</summary>
    Match = 2,

    /// <summary><c>$`</c>: the String before the match.</summary>
    Before = 3,

    /// <summary><c>$'</c>: the String after the match, from the end of the matched substring but not past the String's end.</summary>
    After = 4,

    /// <summary><c>$n</c> or <c>$nn</c>: the capture <see cref="ReplacementPart.CaptureIndex"/>, from one; nothing when it is undefined.</summary>
    Capture = 5,

    /// <summary><c>$&lt;name&gt;</c>: the named capture whose name <see cref="ReplacementPart.Start"/> and <see cref="ReplacementPart.Length"/> give.</summary>
    NamedCapture = 6,
}

/// <summary>One part of a parsed replacement template.</summary>
/// <param name="Kind">What the part stands for.</param>
/// <param name="Start">For a literal, where its text starts in the template; for a named capture, where the name starts; zero otherwise.</param>
/// <param name="Length">The length of that text or name; zero otherwise.</param>
/// <param name="CaptureIndex">For a capture, its index, from one; zero otherwise.</param>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 22.1.3.19.1; IP=Low; Security=Low; Resources=1; Fingerprint=C48A6F
// Broiler-Human:        PENDING
public readonly record struct ReplacementPart(ReplacementPartKind Kind, int Start, int Length, int CaptureIndex);

/// <summary>
/// The template half of <c>GetSubstitution</c> (ECMA-262 22.1.3.19.1): a replacement String
/// parsed into the parts its <c>$</c> forms name, which the engine then evaluates in order.
/// </summary>
/// <remarks>
/// <para>
/// <b>The parse depends only on the template, the capture count and whether the result has named
/// captures</b>, so it is shared; <b>the evaluation is the engine's</b> (contract convention 2): a
/// named capture is read with <c>Get</c> on the result's <c>groups</c> object and converted with
/// <c>ToString</c>, once per occurrence and left to right, and a getter there is observable.
/// </para>
/// <para>
/// The digit rule is the pinned edition's: <c>$nn</c> is taken as two digits, and falls back to one
/// when the two-digit index exceeds the capture count; an index of zero, or one above the count,
/// stays literal, so <c>$0</c>, <c>$00</c> and <c>$1</c> with no captures are text. <c>$&lt;</c> is
/// literal when the result has no named captures or no later <c>&gt;</c> closes it, and a <c>$</c>
/// before anything else is a dollar sign. A String pattern has no captures and no named captures.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 22.1.3.19.1; IP=Low; Security=Medium; Resources=2; Fingerprint=536537
// Broiler-Human:        PENDING
public static class ReplacementTemplate
{
    /// <summary>Parses a replacement template into the parts <c>GetSubstitution</c> evaluates.</summary>
    /// <param name="template">The replacement String.</param>
    /// <param name="captureCount">The number of captures, <c>m</c>: zero for a String pattern.</param>
    /// <param name="hasNamedCaptures">Whether the result's named captures are not <c>undefined</c>.</param>
    /// <param name="charge">The caller's meter: charged the template's length plus one, first; <see langword="null"/> for none.</param>
    /// <returns>The parts in order; adjacent literal text is one part, and an empty template has none.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The capture count is negative.</exception>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 22.1.3.19.1; IP=Low; Security=Medium; Resources=2; Fingerprint=BA3A42
    // Broiler-Human:        PENDING
    public static ReplacementPart[] Parse(ReadOnlySpan<char> template, int captureCount, bool hasNamedCaptures, WorkCharge? charge = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(captureCount);
        charge?.Invoke((ulong)template.Length + 1);

        var first = template.IndexOf('$');
        if (first < 0)
            return template.IsEmpty ? [] : [new ReplacementPart(ReplacementPartKind.Literal, 0, template.Length, 0)];

        var parts = new List<ReplacementPart>();
        var literal = 0;
        var at = first;
        while (at < template.Length)
        {
            if (template[at] != '$' || at + 1 == template.Length)
            {
                at++;
                continue;
            }

            var next = template[at + 1];
            ReplacementPart part;
            int consumed;
            switch (next)
            {
                case '$':
                    (part, consumed) = (new ReplacementPart(ReplacementPartKind.Dollar, 0, 0, 0), 2);
                    break;
                case '&':
                    (part, consumed) = (new ReplacementPart(ReplacementPartKind.Match, 0, 0, 0), 2);
                    break;
                case '`':
                    (part, consumed) = (new ReplacementPart(ReplacementPartKind.Before, 0, 0, 0), 2);
                    break;
                case '\'':
                    (part, consumed) = (new ReplacementPart(ReplacementPartKind.After, 0, 0, 0), 2);
                    break;
                case >= '0' and <= '9':
                {
                    var digits = at + 2 < template.Length && template[at + 2] is >= '0' and <= '9' ? 2 : 1;
                    var index = digits == 2 ? ((next - '0') * 10) + (template[at + 2] - '0') : next - '0';
                    if (digits == 2 && index > captureCount)
                    {
                        digits = 1;
                        index = next - '0';
                    }

                    if (index < 1 || index > captureCount)
                    {
                        // The reference itself is the replacement: it stays in the literal run.
                        at += 1 + digits;
                        continue;
                    }

                    (part, consumed) = (new ReplacementPart(ReplacementPartKind.Capture, 0, 0, index), 1 + digits);
                    break;
                }

                case '<':
                {
                    var close = hasNamedCaptures ? template[(at + 2)..].IndexOf('>') : -1;
                    if (close < 0)
                    {
                        at += 2;
                        continue;
                    }

                    (part, consumed) = (new ReplacementPart(ReplacementPartKind.NamedCapture, at + 2, close, 0), close + 3);
                    break;
                }

                default:
                    at++;
                    continue;
            }

            if (at > literal)
                parts.Add(new ReplacementPart(ReplacementPartKind.Literal, literal, at - literal, 0));

            parts.Add(part);
            at += consumed;
            literal = at;
        }

        if (template.Length > literal)
            parts.Add(new ReplacementPart(ReplacementPartKind.Literal, literal, template.Length - literal, 0));

        return [.. parts];
    }
}
