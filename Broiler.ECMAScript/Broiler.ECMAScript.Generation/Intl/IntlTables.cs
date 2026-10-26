// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   3
// Annotated:        3/3
// Exempt:           6
// Human-reviewed:   0/3
// IP risk:          Low
// Security risk:    Low
// Criteria:         0/0
// Resource impact:  1/10 max
// Unverified:       3
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Broiler.ECMAScript.Generation.Intl;

/// <summary>One table the CLDR table generator writes: its name, its summary, whether it is text, and its bytes.</summary>
/// <param name="Name">The table's name, as Broiler.VM's <c>JsCldrTables</c> names its member, e.g. <c>LikelySubtags</c>.</param>
/// <param name="Summary">What the table holds, field by field, as the generator's summary states it.</param>
/// <param name="IsText">True for a text table - ASCII lines of fields separated by <c>|</c>, separated by LF, the last unterminated - and false for a binary one.</param>
/// <param name="Data">The table's bytes: what an engine embeds.</param>
// Broiler-AI:           Origin=AI; Spec=JSD-0082 D11 generated table; IP=Low; Security=Low; Resources=0; Fingerprint=15B3E2
// Broiler-Human:        PENDING
public sealed record IntlGeneratedTable(string Name, string Summary, bool IsText, ReadOnlyMemory<byte> Data);

/// <summary>
/// Every table the CLDR table generator writes from one pin, archive and set of options, in the order
/// Broiler.VM's <c>JsCldrTables.g.cs</c> declares them, with the data versions they were written from.
/// </summary>
// Broiler-AI:           Origin=AI; Spec=JSD-0082 D11 generated tables; IP=Low; Security=Low; Resources=1; Fingerprint=6CBF40
// Broiler-Human:        PENDING
public sealed class IntlTables
{
    internal IntlTables(string cldrVersion, string ucaVersion, IReadOnlyList<IntlGeneratedTable> tables, IReadOnlyList<string> absentFiles)
    {
        CldrVersion = cldrVersion;
        UcaVersion = ucaVersion;
        Tables = tables;
        AbsentFiles = absentFiles;
    }

    /// <summary>The CLDR release the tables were written from, the pin's <c>version</c>.</summary>
    public string CldrVersion { get; }

    /// <summary>The UCA version of the collation tables, the pin's <c>uca-version</c>.</summary>
    public string UcaVersion { get; }

    /// <summary>The Unicode version of the UCD files read.</summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D12 generated tables; IP=Low; Security=Low; Resources=0; Fingerprint=D8142D
    // Broiler-Human:        PENDING
    public static string UnicodeVersion => IntlDataVersions.UnicodeVersion;

    /// <summary>Every table, in the order Broiler.VM's generated file declares them.</summary>
    public IReadOnlyList<IntlGeneratedTable> Tables { get; }

    /// <summary>
    /// The optional main-data files the options asked for and the pin does not name, in ordinal order: a
    /// <c>languages.json</c>, <c>territories.json</c>, <c>scripts.json</c> or <c>variants.json</c> that CLDR's
    /// <c>cldr-localenames-full</c> package carries for no such locale, whose names the display-name table then
    /// lacks. Every other file a table reads must be pinned.
    /// </summary>
    public IReadOnlyList<string> AbsentFiles { get; }

    /// <summary>The table named <paramref name="name"/>.</summary>
    /// <param name="name">The table's name, e.g. <c>LikelySubtags</c>.</param>
    /// <exception cref="InvalidDataException">No table has that name.</exception>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D11 generated tables; IP=Low; Security=Low; Resources=1; Fingerprint=489206
    // Broiler-Human:        PENDING
    public IntlGeneratedTable this[string name] =>
        Tables.SingleOrDefault(table => string.Equals(table.Name, name, StringComparison.Ordinal))
        ?? throw new InvalidDataException("the generator writes no table `" + name + "`");
}
