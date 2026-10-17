// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   8
// Annotated:        8/8
// Exempt:           0
// Human-reviewed:   0/8
// IP risk:          Low
// Security risk:    Low
// Criteria:         0/0
// Resource impact:  4/10 max
// Unverified:       8
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Broiler.ECMAScript.Generation.Time;

/// <summary>
/// The C# text both source writers write: the file's opening, a byte table, a text table and a constant,
/// each with its summary and the caller's annotation lines, and the class's close.
/// </summary>
/// <remarks>
/// <b>PORTED from Broiler.VM's <c>TzdbTableGenerator</c> and <c>CalendarTableGenerator</c></b>, whose
/// <c>Header</c>, <c>ByteTable</c> and <c>TextTable</c> wrote this layout: a byte table as decimal bytes, 32
/// to a line, each followed by a comma; a text table as a UTF-8 raw string literal, one line per table
/// line; members separated by one empty line; LF line endings throughout. What those wrote as constants -
/// the namespace, the class, the header and the assurance lines - now comes from
/// <see cref="TableSourceOptions"/>.
/// </remarks>
// Broiler-AI:           Origin=Ported; Spec=JSD-0076 SJL-D-6 (c) table source via Broiler.VM TzdbTableGenerator; IP=Low; Security=Low; Resources=4; Fingerprint=A4D2B2
// Broiler-Human:        PENDING
internal static class TableSourceWriter
{
    /// <summary>The usings, the namespace, the header, and the class's summary, annotation and opening brace.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0076 SJL-D-6 (c) table source via Broiler.VM TzdbTableGenerator; IP=Low; Security=Low; Resources=2; Fingerprint=842AF6
    // Broiler-Human:        PENDING
    internal static StringBuilder Open(TableSourceOptions options, string defaultSummary)
    {
        ArgumentNullException.ThrowIfNull(options);

        var text = new StringBuilder();
        text.Append("using System;\n\n");
        text.Append("namespace ").Append(options.NamespaceName).Append(";\n\n");

        if (options.Header.Count > 0)
        {
            Comments(text, string.Empty, options.Header);
            text.Append('\n');
        }

        text.Append("/// <summary>").Append(options.ClassSummary ?? defaultSummary).Append("</summary>\n");
        Comments(text, string.Empty, options.ClassAnnotation);
        text.Append("internal static class ").Append(options.ClassName).Append('\n');
        text.Append("{\n");
        return text;
    }

    /// <summary>A string constant, followed by an empty line.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0076 SJL-D-6 (c) table source via Broiler.VM TzdbTableGenerator; IP=Low; Security=Low; Resources=1; Fingerprint=6F5A4B
    // Broiler-Human:        PENDING
    internal static void Constant(StringBuilder text, TableSourceOptions options, string name, string summary, string value)
    {
        Member(text, options, name, summary);
        text.Append("    internal const string ").Append(name).Append(" = \"").Append(value).Append("\";\n\n");
    }

    /// <summary>A byte table, decimal bytes 32 to a line, each followed by a comma, followed by an empty line.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0076 SJL-D-6 (c) table source via Broiler.VM TzdbTableGenerator; IP=Low; Security=Low; Resources=4; Fingerprint=8F99A3
    // Broiler-Human:        PENDING
    internal static void ByteTable(StringBuilder text, TableSourceOptions options, string name, string summary, ReadOnlySpan<byte> data)
    {
        Member(text, options, name, summary);
        text.Append("    internal static ReadOnlySpan<byte> ").Append(name).Append(" => new byte[]\n    {\n");

        for (var at = 0; at < data.Length; at += 32)
        {
            text.Append("        ");
            var end = Math.Min(at + 32, data.Length);

            for (var i = at; i < end; i++)
            {
                text.Append(data[i].ToString(NumberFormatInfo.InvariantInfo)).Append(',');
                text.Append(i + 1 < end ? " " : string.Empty);
            }

            text.Append('\n');
        }

        text.Append("    };\n\n");
    }

    /// <summary>A text table as a UTF-8 raw string literal, one source line per table line, followed by an empty line.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0076 SJL-D-6 (c) table source via Broiler.VM TzdbTableGenerator; IP=Low; Security=Low; Resources=4; Fingerprint=F81332
    // Broiler-Human:        PENDING
    internal static void TextTable(StringBuilder text, TableSourceOptions options, string name, string summary, ReadOnlySpan<byte> data)
    {
        Member(text, options, name, summary);
        text.Append("    internal static ReadOnlySpan<byte> ").Append(name).Append(" =>\n");
        text.Append("        \"\"\"\n");

        foreach (var line in Encoding.ASCII.GetString(data).Split('\n'))
        {
            text.Append("        ").Append(line).Append('\n');
        }

        text.Append("        \"\"\"u8;\n\n");
    }

    /// <summary>The class's closing brace, after the last member's empty line is taken back.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0076 SJL-D-6 (c) table source via Broiler.VM TzdbTableGenerator; IP=Low; Security=Low; Resources=1; Fingerprint=F84C24
    // Broiler-Human:        PENDING
    internal static string Close(StringBuilder text)
    {
        while (text.Length > 2 && text[^1] == '\n' && text[^2] == '\n')
        {
            text.Length--;
        }

        text.Append("}\n");
        return text.ToString();
    }

    /// <summary>A member's summary, the caller's in place of the generator's where it names one, and the caller's annotation lines.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0076 SJL-D-6 (c) table source via Broiler.VM TzdbTableGenerator; IP=Low; Security=Low; Resources=1; Fingerprint=F05E23
    // Broiler-Human:        PENDING
    private static void Member(StringBuilder text, TableSourceOptions options, string name, string summary)
    {
        text.Append("    /// <summary>")
            .Append(options.MemberSummaries.TryGetValue(name, out var chosen) ? chosen : summary)
            .Append("</summary>\n");
        Comments(text, "    ", options.MemberAnnotation);
    }

    /// <summary>Comment lines at an indentation: <c>// </c> before a line, <c>//</c> alone for an empty one.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0076 SJL-D-6 (c) table source via Broiler.VM TzdbTableGenerator; IP=Low; Security=Low; Resources=1; Fingerprint=0E41B8
    // Broiler-Human:        PENDING
    private static void Comments(StringBuilder text, string indent, IReadOnlyList<string> lines)
    {
        foreach (var line in lines)
        {
            text.Append(indent).Append(line.Length == 0 ? "//" : "// " + line).Append('\n');
        }
    }
}
