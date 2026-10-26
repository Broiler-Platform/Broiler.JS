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
// Resource impact:  3/10 max
// Unverified:       2
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using Broiler.ECMAScript.Generation.Time;

namespace Broiler.ECMAScript.Generation.Intl;

/// <summary>
/// Writes <see cref="IntlTables"/> as C#: a <c>Version</c> constant and one <c>ReadOnlySpan&lt;byte&gt;</c>
/// property over constant data per table, in a static class the caller names.
/// </summary>
/// <remarks>
/// <para>
/// <b>PORTED from Broiler.VM's <c>CldrTableGenerator</c></b>, its source half (<c>Header</c>, <c>TextTable</c>
/// and <c>ByteTable</c>): the members, their order, their summaries and their layout are the ones it writes into
/// <c>JsCldrTables.g.cs</c>, through the same writer as SJL-8's time tables, so that with that file's namespace,
/// class, header and annotation lines in <see cref="TableSourceOptions"/> this writes Broiler.VM's file as its
/// own generator does, before its assurance pass (decision JSD-0082, decision D11). A text table is a UTF-8 raw
/// string literal, one source line per table line; a binary table is decimal bytes, 32 to a line.
/// </para>
/// <para>
/// The text is deterministic: LF line endings, numbers through the invariant format, and nothing recording a
/// time, a machine or a path.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 table source via Broiler.VM CldrTableGenerator; IP=Low; Security=Low; Resources=3; Fingerprint=E864E5
// Broiler-Human:        PENDING
public static class IntlTableSource
{
    /// <summary>The C# text of a static class holding <paramref name="tables"/>.</summary>
    /// <param name="tables">The tables, as <see cref="CldrTableGenerator.Generate"/> answers them.</param>
    /// <param name="options">The namespace, the class, and the comment lines around them.</param>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 table source via Broiler.VM CldrTableGenerator; IP=Low; Security=Low; Resources=3; Fingerprint=0227D6
    // Broiler-Human:        PENDING
    public static string Write(IntlTables tables, TableSourceOptions options)
    {
        ArgumentNullException.ThrowIfNull(tables);
        ArgumentNullException.ThrowIfNull(options);

        var text = TableSourceWriter.Open(options, "The CLDR " + tables.CldrVersion + " tables, as the generator wrote them.");
        TableSourceWriter.Constant(text, options, "Version", "The CLDR release the tables were generated from.", tables.CldrVersion);

        foreach (var table in tables.Tables)
        {
            if (table.IsText)
            {
                TableSourceWriter.TextTable(text, options, table.Name, table.Summary, table.Data.Span);
            }
            else
            {
                TableSourceWriter.ByteTable(text, options, table.Name, table.Summary, table.Data.Span);
            }
        }

        return TableSourceWriter.Close(text);
    }
}
