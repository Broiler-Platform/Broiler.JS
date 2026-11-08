// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// HAIRS Code Assurance
// --------------------
// Relevant units:   2
// Annotated:        2/2
// Exempt:           5
// Human-reviewed:   0/2
// IP risk:          Low
// Security risk:    Low
// Criteria:         0/0
// Resource impact:  1/10 max
// Unverified:       2
//
// GENERATED - DO NOT EDIT MANUALLY

using System;

namespace Broiler.ECMAScript.Syntax;

/// <summary>
/// The source a request parses: its text in UTF-16 code units, the name diagnostics carry, and where the text starts in a
/// larger source, for a sub-parse (Broiler.VM's JSD-0080, the request's "Source"; JSD-0083, decision D15).
/// </summary>
/// <remarks>
/// <para>
/// <b>Positions</b> (decision D15): offsets are 0-based indices in UTF-16 code units; lines and columns are 1-based,
/// columns in UTF-16 code units; every line terminator of ECMA-262 clause 12.3 (LF, CR, LS, PS) starts a line, and CR LF
/// is one. A span the library reports adds <see cref="BaseOffset"/> to its offset, and starts its lines at
/// <see cref="BaseLine"/> and the first line's columns at <see cref="BaseColumn"/>.
/// </para>
/// <para>
/// <b>The text is the source as the specification reads it.</b> A leading byte-order mark is the host's to drop (decision
/// D15); this type keeps every code unit it is given, a lone surrogate included, as ECMA-262 clause 11.1 lets a source
/// text hold one.
/// </para>
/// </remarks>
// Hairs-AI:           Origin=AI; Spec=ECMA-262 s11.1 source text, JSD-0083 D15; IP=Low; Security=Low; Resources=0; Fingerprint=FEBD3E
// Hairs-Human:        PENDING
public sealed class SourceText
{
    /// <summary>Creates a source.</summary>
    /// <param name="text">The source text, in UTF-16 code units.</param>
    /// <param name="name">The source's name, as diagnostics carry it; empty for none.</param>
    /// <param name="baseOffset">The offset of the text's first code unit in a larger source; zero or more.</param>
    /// <param name="baseLine">The line of the text's first code unit; one or more.</param>
    /// <param name="baseColumn">The column of the text's first code unit; one or more.</param>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> or <paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The base offset is negative, or the base line or column is below one.</exception>
    // Hairs-AI:           Origin=AI; Spec=JSD-0083 D15 positions; IP=Low; Security=Low; Resources=1; Fingerprint=4E6E1E
    // Hairs-Human:        PENDING
    public SourceText(string text, string name = "", int baseOffset = 0, int baseLine = 1, int baseColumn = 1)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentOutOfRangeException.ThrowIfNegative(baseOffset);
        ArgumentOutOfRangeException.ThrowIfLessThan(baseLine, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(baseColumn, 1);
        Text = text;
        Name = name;
        BaseOffset = baseOffset;
        BaseLine = baseLine;
        BaseColumn = baseColumn;
    }

    /// <summary>The source text, in UTF-16 code units (ECMA-262 clause 11.1).</summary>
    public string Text { get; }

    /// <summary>The source's name, as every diagnostic of a parse of it carries it; empty for none.</summary>
    public string Name { get; }

    /// <summary>The offset, in UTF-16 code units, of the text's first code unit in the source it was taken from.</summary>
    public int BaseOffset { get; }

    /// <summary>The 1-based line of the text's first code unit.</summary>
    public int BaseLine { get; }

    /// <summary>The 1-based column, in UTF-16 code units, of the text's first code unit.</summary>
    public int BaseColumn { get; }
}
