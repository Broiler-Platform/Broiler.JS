// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   2
// Annotated:        2/2
// Exempt:           0
// Human-reviewed:   0/2
// IP risk:          Low
// Security risk:    Low
// Criteria:         0/0
// Resource impact:  4/10 max
// Unverified:       2
//
// GENERATED - DO NOT EDIT MANUALLY

using System;

namespace Broiler.ECMAScript.Generation.Time;

/// <summary>
/// Writes <see cref="TzdbTables"/> as C#: a <c>Version</c> constant and one <c>ReadOnlySpan&lt;byte&gt;</c>
/// property over constant data per table, in a static class the caller names.
/// </summary>
/// <remarks>
/// <para>
/// <b>PORTED from Broiler.VM's <c>TzdbTableGenerator</c></b>, its source half: the members, their order,
/// their summaries and their layout are the ones it writes into <c>JsTzdbTables.g.cs</c>, so that with
/// that file's namespace, class, header and annotation lines in <see cref="TableSourceOptions"/> this
/// writes Broiler.VM's file as its own generator does. <c>TimeZones</c> is a byte table;
/// <c>TimeZoneIds</c> and <c>TimeZoneRegions</c> are UTF-8 raw string literals whose bytes are the tables'.
/// </para>
/// <para>
/// The text is deterministic: LF line endings, numbers through the invariant format, and nothing
/// recording a time, a machine or a path. A test that regenerates the tables compares it with the
/// committed file byte for byte.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=Ported; Spec=JSD-0076 SJL-D-6 (c) table source via Broiler.VM TzdbTableGenerator; IP=Low; Security=Low; Resources=4; Fingerprint=70FA20
// Broiler-Human:        PENDING
public static class TzdbTableSource
{
    /// <summary>The C# text of a static class holding <paramref name="tables"/>.</summary>
    /// <param name="tables">The tables, as <see cref="TzdbTableGenerator.Generate"/> answers them.</param>
    /// <param name="options">The namespace, the class, and the comment lines around them.</param>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0076 SJL-D-6 (c) table source via Broiler.VM TzdbTableGenerator; IP=Low; Security=Low; Resources=4; Fingerprint=0F1FA0
    // Broiler-Human:        PENDING
    public static string Write(TzdbTables tables, TableSourceOptions options)
    {
        ArgumentNullException.ThrowIfNull(tables);
        ArgumentNullException.ThrowIfNull(options);

        var text = TableSourceWriter.Open(options, "The tzdb " + tables.Release + " tables, as the generator wrote them.");
        TableSourceWriter.Constant(text, options, "Version", "The tzdb release the tables were generated from.", tables.Release);
        TableSourceWriter.ByteTable(
            text, options, "TimeZones",
            "Each Zone's UTC offsets, in Zone name order: its first offset, its transitions, and the rules that continue them.",
            tables.TimeZones.Span);
        TableSourceWriter.TextTable(
            text, options, "TimeZoneIds",
            "Every Zone and Link name, in ordinal order: identifier, the index of the Zone whose offsets it reads, and its primary identifier where that is another.",
            tables.TimeZoneIds.Span);
        TableSourceWriter.TextTable(
            text, options, "TimeZoneRegions",
            "zone.tab's zones of each country, in ordinal order: region, zones.",
            tables.TimeZoneRegions.Span);
        return TableSourceWriter.Close(text);
    }
}
