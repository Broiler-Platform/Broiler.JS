// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   12
// Annotated:        12/12
// Exempt:           6
// Human-reviewed:   0/12
// IP risk:          Low
// Security risk:    Low
// Criteria:         0/0
// Resource impact:  2/10 max
// Unverified:       12
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Generic;

namespace Broiler.ECMAScript.Conformance;

/// <summary>
/// What the corpus's own documents pin: its revision (REVISIONS.md) and the specification and
/// Unicode versions its provenance may name (sources/*.pin).
/// </summary>
/// <param name="Revision">The corpus revision, the last row of REVISIONS.md.</param>
/// <param name="SpecificationRevision">The ECMA-262 commit sources/ecma262.pin names.</param>
/// <param name="UnicodeVersion">The Unicode version sources/unicode.pin names.</param>
// Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=2; Fingerprint=C10269
// Broiler-Human:        PENDING
public sealed record VectorSources(int Revision, string SpecificationRevision, string UnicodeVersion)
{
    /// <summary>
    /// The ECMA-402 commit sources/ecma402.pin names, which a <c>spec:ecma402@</c> provenance entry must
    /// name; <see langword="null"/> when the corpus pins no ECMA-402 text, as before corpus revision 27.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=73B7D2
    // Broiler-Human:        PENDING
    public string? InternationalizationRevision { get; init; }

    /// <summary>Reads the pins from the text of the three documents that hold them.</summary>
    /// <param name="revisions">REVISIONS.md.</param>
    /// <param name="ecma262Pin">sources/ecma262.pin.</param>
    /// <param name="unicodePin">sources/unicode.pin.</param>
    /// <returns>The revision and the two pins.</returns>
    /// <exception cref="FormatException">REVISIONS.md does not list revisions 1, 2, ... in order, or a pin has no value.</exception>
    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=2; Fingerprint=181CBB
    // Broiler-Human:        PENDING
    public static VectorSources Parse(string revisions, string ecma262Pin, string unicodePin) =>
        Parse(revisions, ecma262Pin, unicodePin, null);

    /// <summary>
    /// Reads the pins from the text of the documents that hold them, sources/ecma402.pin among them when
    /// the corpus has one (corpus revision 27).
    /// </summary>
    /// <param name="revisions">REVISIONS.md.</param>
    /// <param name="ecma262Pin">sources/ecma262.pin.</param>
    /// <param name="unicodePin">sources/unicode.pin.</param>
    /// <param name="ecma402Pin">sources/ecma402.pin, or <see langword="null"/> when the corpus has none.</param>
    /// <returns>The revision and the pins.</returns>
    /// <exception cref="FormatException">REVISIONS.md does not list revisions 1, 2, ... in order, or a pin has no value.</exception>
    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=2; Fingerprint=FB6703
    // Broiler-Human:        PENDING
    public static VectorSources Parse(string revisions, string ecma262Pin, string unicodePin, string? ecma402Pin)
    {
        ArgumentNullException.ThrowIfNull(revisions);
        ArgumentNullException.ThrowIfNull(ecma262Pin);
        ArgumentNullException.ThrowIfNull(unicodePin);

        var last = 0;
        foreach (var line in Lines(revisions))
        {
            if (!TryReadRevisionRow(line, out var revision))
                continue;
            if (revision != last + 1)
                throw new FormatException("REVISIONS.md does not list revisions 1, 2, ... in order");
            last = revision;
        }

        if (last == 0)
            throw new FormatException("REVISIONS.md lists no revision");

        return new VectorSources(last, PinValue(ecma262Pin, "ecma262.pin", "revision"), PinValue(unicodePin, "unicode.pin", "version"))
        {
            InternationalizationRevision = ecma402Pin is null ? null : PinValue(ecma402Pin, "ecma402.pin", "revision"),
        };
    }

    /// <summary>A row of REVISIONS.md's table: <c>| N | YYYY-MM-DD |</c> at the start of the line.</summary>
    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=E5E4F1
    // Broiler-Human:        PENDING
    private static bool TryReadRevisionRow(string line, out int revision)
    {
        revision = 0;
        if (!line.StartsWith("| ", StringComparison.Ordinal))
            return false;
        var end = line.IndexOf(' ', 2);
        if (end < 0 || !Ascii.TryParsePositive(line.AsSpan(2, end - 2), out revision))
            return false;
        var rest = line.AsSpan(end);
        if (rest.Length < 15 ||!rest.StartsWith(" | ", StringComparison.Ordinal) || !rest[13..].StartsWith(" |", StringComparison.Ordinal))
            return false;
        var date = rest.Slice(3, 10);
        return Ascii.All(date[..4], Ascii.IsDigit) && date[4] == '-' && Ascii.All(date.Slice(5, 2), Ascii.IsDigit)
            && date[7] == '-' && Ascii.All(date[8..], Ascii.IsDigit);
    }

    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=E4F1BB
    // Broiler-Human:        PENDING
    private static string PinValue(string text, string name, string key)
    {
        foreach (var line in Lines(text))
        {
            if (line.StartsWith('#'))
                continue;
            var space = line.IndexOf(' ', StringComparison.Ordinal);
            if (space > 0 && line.AsSpan(0, space).SequenceEqual(key))
                return line[(space + 1)..];
        }

        throw new FormatException(name + " has no '" + key + "' line");
    }

    /// <summary>The lines of a text, with LF or CRLF endings.</summary>
    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=0E055D
    // Broiler-Human:        PENDING
    private static IEnumerable<string> Lines(string text)
    {
        foreach (var line in text.Split('\n'))
            yield return line.EndsWith('\r') ? line[..^1] : line;
    }
}

/// <summary>One file of the corpus, as its caller read it.</summary>
/// <param name="Name">The file's name relative to the corpus root, '/'-separated.</param>
/// <param name="Content">The file's bytes.</param>
// Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=4A93B8
// Broiler-Human:        PENDING
public readonly record struct CorpusFile(string Name, ReadOnlyMemory<byte> Content);

/// <summary>
/// The corpus: every <c>.vectors</c> file of it except the negative controls, read and validated.
/// </summary>
// Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=2; Fingerprint=C5D895
// Broiler-Human:        PENDING
public sealed class VectorCorpus
{
    /// <summary>The directory, relative to the corpus root, that holds the negative controls.</summary>
    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=77EB14
    // Broiler-Human:        PENDING
    public const string ControlsDirectory = "controls";

    private readonly List<VectorRecord> records;

    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=0C26CC
    // Broiler-Human:        PENDING
    private VectorCorpus(VectorSources sources, IReadOnlyList<VectorFile> files, IReadOnlyList<VectorDiagnostic> diagnostics)
    {
        Sources = sources;
        Files = files;
        Diagnostics = diagnostics;
        records = [];
        foreach (var file in files)
            records.AddRange(file.Records);
    }

    /// <summary>What the corpus's own documents pin.</summary>
    public VectorSources Sources { get; }

    /// <summary>The vector files, in ordinal order of their names.</summary>
    public IReadOnlyList<VectorFile> Files { get; }

    /// <summary>Every defect the reader and the validator found; empty for a valid corpus.</summary>
    public IReadOnlyList<VectorDiagnostic> Diagnostics { get; }

    /// <summary>Every record of every file, in file order.</summary>
    public IReadOnlyList<VectorRecord> Records => records;

    /// <summary>
    /// Whether a file of the corpus is one of its vector files: a <c>.vectors</c> file outside the
    /// negative controls.
    /// </summary>
    /// <param name="name">The file's name relative to the corpus root, '/'-separated.</param>
    /// <returns><see langword="true"/> for a vector file the corpus loads.</returns>
    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=53B43D
    // Broiler-Human:        PENDING
    public static bool IsVectorFile(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return name.EndsWith(".vectors", StringComparison.Ordinal)
            && !name.StartsWith(ControlsDirectory + "/", StringComparison.Ordinal);
    }

    /// <summary>
    /// Reads and validates the corpus's vector files. Files that are not vector files are ignored,
    /// and the rest are read in ordinal order of their names whatever order they are given in.
    /// </summary>
    /// <param name="sources">What the corpus's own documents pin.</param>
    /// <param name="files">The corpus's files.</param>
    /// <returns>The corpus, with every defect found.</returns>
    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=2; Fingerprint=070F1A
    // Broiler-Human:        PENDING
    public static VectorCorpus Load(VectorSources sources, IEnumerable<CorpusFile> files)
    {
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(files);
        var ordered = new List<CorpusFile>();
        foreach (var file in files)
        {
            if (IsVectorFile(file.Name))
                ordered.Add(file);
        }

        ordered.Sort(static (a, b) => string.CompareOrdinal(a.Name, b.Name));
        var validator = new VectorValidator(sources);
        var read = new List<VectorFile>(ordered.Count);
        foreach (var file in ordered)
        {
            var vectorFile = VectorReader.Read(file.Content.Span, file.Name);
            validator.Validate(vectorFile);
            read.Add(vectorFile);
        }

        return new VectorCorpus(sources, read, validator.Diagnostics);
    }
}
