// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   2
// Annotated:        2/2
// Exempt:           7
// Human-reviewed:   0/2
// IP risk:          Low
// Security risk:    Low
// Criteria:         0/0
// Resource impact:  1/10 max
// Unverified:       2
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Frozen;
using System.Collections.Generic;

namespace Broiler.ECMAScript.Generation.Time;

/// <summary>
/// What a source writer of this package makes of the C# file it writes around a generator's tables: the
/// namespace and class the caller chooses, the comment lines it states, and the assurance lines its
/// repository writes on each declaration.
/// </summary>
/// <remarks>
/// <para>
/// The writers (<see cref="TzdbTableSource"/>, <see cref="CalendarTableSource"/>) write one
/// <c>internal static class</c> with one member per table, each a <c>ReadOnlySpan&lt;byte&gt;</c> over
/// constant data, and nothing else of their own. Every line an engine's repository needs on that file -
/// its account of where the file comes from, the annotation its assurance scheme reads - comes from here,
/// so an engine that wrote the tables itself before (Broiler.VM, until its milestone SJL-8.5) can write the
/// same file, byte for byte, with this package.
/// </para>
/// <para>
/// A comment line is given without its <c>//</c>: the writer writes <c>// </c> before a line, and
/// <c>//</c> alone for an empty one.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=JSD-0076 SJL-D-6 (c) table source; IP=Low; Security=Low; Resources=1; Fingerprint=F6B78F
// Broiler-Human:        PENDING
public sealed record TableSourceOptions
{
    /// <summary>The options for a class <paramref name="className"/> in <paramref name="namespaceName"/>, with no comment lines and the generator's own summaries.</summary>
    /// <param name="namespaceName">The file-scoped namespace, e.g. <c>Broiler.JavaScript.BuiltIns.Time</c>.</param>
    /// <param name="className">The class's name, e.g. <c>TimeZoneTables</c>.</param>
    // Broiler-AI:           Origin=AI; Spec=JSD-0076 SJL-D-6 (c) table source; IP=Low; Security=Low; Resources=0; Fingerprint=4A6DE4
    // Broiler-Human:        PENDING
    public TableSourceOptions(string namespaceName, string className)
    {
        ArgumentException.ThrowIfNullOrEmpty(namespaceName);
        ArgumentException.ThrowIfNullOrEmpty(className);
        NamespaceName = namespaceName;
        ClassName = className;
    }

    /// <summary>The file-scoped namespace the class is declared in.</summary>
    public string NamespaceName { get; }

    /// <summary>The class's name.</summary>
    public string ClassName { get; }

    /// <summary>Comment lines written after the namespace and before the class, each without its <c>//</c>; none when empty.</summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0076 SJL-D-6 (c) table source; IP=Low; Security=Low; Resources=0; Fingerprint=5E4946
    // Broiler-Human:        PENDING
    public IReadOnlyList<string> Header { get; init; } = [];

    /// <summary>The class's summary, or null for the generator's own.</summary>
    public string? ClassSummary { get; init; }

    /// <summary>Comment lines written between the class's summary and its declaration, each without its <c>//</c>.</summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0076 SJL-D-6 (c) table source; IP=Low; Security=Low; Resources=0; Fingerprint=CA57DE
    // Broiler-Human:        PENDING
    public IReadOnlyList<string> ClassAnnotation { get; init; } = [];

    /// <summary>Comment lines written between each member's summary and its declaration, each without its <c>//</c>.</summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0076 SJL-D-6 (c) table source; IP=Low; Security=Low; Resources=0; Fingerprint=E34FCA
    // Broiler-Human:        PENDING
    public IReadOnlyList<string> MemberAnnotation { get; init; } = [];

    /// <summary>A member's summary in place of the generator's own, by the member's name; a member not named keeps the generator's.</summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0076 SJL-D-6 (c) table source; IP=Low; Security=Low; Resources=0; Fingerprint=32FF70
    // Broiler-Human:        PENDING
    public IReadOnlyDictionary<string, string> MemberSummaries { get; init; } = FrozenDictionary<string, string>.Empty;
}
