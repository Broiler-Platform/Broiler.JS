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
/// Writes <see cref="CalendarTables"/> as C#: one <c>ReadOnlySpan&lt;byte&gt;</c> property over constant
/// data, in a static class the caller names.
/// </summary>
/// <remarks>
/// <b>PORTED from Broiler.VM's <c>CalendarTableGenerator</c></b>, its source half: the member, its layout
/// and the class's summary are the ones it writes into <c>JsCalendarTables.g.cs</c>, so that with that
/// file's namespace, class, header, annotation lines and member summary in <see cref="TableSourceOptions"/>
/// this writes Broiler.VM's file as its own generator does. The member's own summary names this package's
/// generator where Broiler.VM's named its reader. The text is deterministic: LF line endings, numbers
/// through the invariant format, and nothing recording a time, a machine or a path.
/// </remarks>
// Broiler-AI:           Origin=Ported; Spec=JSD-0076 SJL-D-6 (c) table source via Broiler.VM CalendarTableGenerator; IP=Low; Security=Low; Resources=4; Fingerprint=576407
// Broiler-Human:        PENDING
public static class CalendarTableSource
{
    /// <summary>The C# text of a static class holding <paramref name="tables"/>.</summary>
    /// <param name="tables">The table, as <see cref="CalendarTableGenerator.Generate"/> answers it.</param>
    /// <param name="options">The namespace, the class, and the comment lines around them.</param>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0076 SJL-D-6 (c) table source via Broiler.VM CalendarTableGenerator; IP=Low; Security=Low; Resources=4; Fingerprint=40C3CA
    // Broiler-Human:        PENDING
    public static string Write(CalendarTables tables, TableSourceOptions options)
    {
        ArgumentNullException.ThrowIfNull(tables);
        ArgumentNullException.ThrowIfNull(options);

        var text = TableSourceWriter.Open(options, "The calendar tables, as the generator wrote them.");
        TableSourceWriter.ByteTable(
            text, options, "Calendars",
            "The Chinese, Korean and Qing years, the Umm al-Qura years, and the Persian calendar's non-leap corrections, in the layout CalendarTables states.",
            tables.Calendars.Span);
        return TableSourceWriter.Close(text);
    }
}
