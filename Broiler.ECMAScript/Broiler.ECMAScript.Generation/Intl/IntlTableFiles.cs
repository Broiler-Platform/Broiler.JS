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
// Resource impact:  3/10 max
// Unverified:       4
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Broiler.ECMAScript.Generation.Time;

namespace Broiler.ECMAScript.Generation.Intl;

/// <summary>
/// Writes <see cref="IntlTables"/> as raw files: one file per table, its bytes the table's, and a
/// <c>tables.manifest</c> naming the data versions and each table file's length and SHA-256 (decision
/// JSD-0082, decision D11). An engine that embeds the tables as resources - Broiler.JS, as it embeds the time
/// tables - embeds these files.
/// </summary>
/// <remarks>
/// A table's file is named for it in lower case, words separated by hyphens: <c>.txt</c> for a text table
/// (ASCII lines separated by LF, the last unterminated, as <see cref="IntlGeneratedTable.Data"/> holds them) and
/// <c>.bin</c> for a binary one - <c>LikelySubtags</c> is <c>likely-subtags.txt</c>, <c>CollationRoot</c> is
/// <c>collation-root.bin</c>. Nothing here reads or writes a file: the caller writes the bytes it is answered.
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=JSD-0082 D11 table files; IP=Low; Security=Low; Resources=3; Fingerprint=3CFE1A
// Broiler-Human:        PENDING
public static class IntlTableFiles
{
    /// <summary>The manifest's file name.</summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D11 table files; IP=Low; Security=Low; Resources=0; Fingerprint=2950E2
    // Broiler-Human:        PENDING
    public const string ManifestFile = "tables.manifest";

    /// <summary>The file name a table is written under: <c>LikelySubtags</c> as <c>likely-subtags.txt</c>.</summary>
    /// <param name="table">The table.</param>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D11 table files; IP=Low; Security=Low; Resources=1; Fingerprint=E2DC41
    // Broiler-Human:        PENDING
    public static string FileName(IntlGeneratedTable table)
    {
        ArgumentNullException.ThrowIfNull(table);

        var name = new StringBuilder();

        foreach (var c in table.Name)
        {
            if (c is >= 'A' and <= 'Z')
            {
                if (name.Length > 0)
                {
                    name.Append('-');
                }

                name.Append(ArchiveReading.AsciiLower(c));
            }
            else
            {
                name.Append(c);
            }
        }

        return name.Append(table.IsText ? ".txt" : ".bin").ToString();
    }

    /// <summary>Every file: one per table and the manifest, by file name, in ordinal order.</summary>
    /// <param name="tables">The tables, as <see cref="CldrTableGenerator.Generate"/> answers them.</param>
    /// <param name="header">The manifest's comment lines, each without its <c>#</c>: the caller's account of where the files come from.</param>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D11 table files; IP=Low; Security=Medium; Resources=3; Fingerprint=79C7E1
    // Broiler-Human:        PENDING
    public static IReadOnlyDictionary<string, byte[]> Write(IntlTables tables, IReadOnlyList<string> header)
    {
        ArgumentNullException.ThrowIfNull(tables);
        ArgumentNullException.ThrowIfNull(header);

        var files = new SortedDictionary<string, byte[]>(StringComparer.Ordinal);

        foreach (var table in tables.Tables)
        {
            if (!files.TryAdd(FileName(table), table.Data.ToArray()))
            {
                throw new ArgumentException("two tables are written to " + FileName(table), nameof(tables));
            }
        }

        var manifest = new StringBuilder();

        foreach (var line in header)
        {
            manifest.Append(line.Length == 0 ? "#" : "# " + line).Append('\n');
        }

        manifest.Append('\n');
        manifest.Append("cldr-version ").Append(tables.CldrVersion).Append('\n');
        manifest.Append("uca-version ").Append(tables.UcaVersion).Append('\n');
        manifest.Append("unicode-version ").Append(IntlTables.UnicodeVersion).Append('\n');
        manifest.Append('\n');

        foreach (var (name, bytes) in files)
        {
            manifest.Append("table ").Append(name).Append(' ')
                .Append(bytes.LongLength.ToString(System.Globalization.NumberFormatInfo.InvariantInfo)).Append(' ')
                .Append(ArchiveReading.Sha256(bytes)).Append('\n');
        }

        files.Add(ManifestFile, Encoding.ASCII.GetBytes(manifest.ToString()));
        return files.AsReadOnly();
    }
}
