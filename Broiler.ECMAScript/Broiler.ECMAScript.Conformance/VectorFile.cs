// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   18
// Annotated:        18/18
// Exempt:           5
// Human-reviewed:   0/18
// IP risk:          Low
// Security risk:    Low
// Criteria:         0/0
// Resource impact:  5/10 max
// Unverified:       18
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Generic;
using System.Text;

namespace Broiler.ECMAScript.Conformance;

/// <summary>A defect the reader or the validator found, at a line of a file.</summary>
/// <param name="Code">A stable name for the kind of defect, as the negative controls' file names give it.</param>
/// <param name="File">The file's name relative to the corpus, '/'-separated.</param>
/// <param name="Line">The one-based line the defect is at.</param>
/// <param name="Message">What is wrong, for a reader.</param>
// Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=DE3A99
// Broiler-Human:        PENDING
public sealed record VectorDiagnostic(string Code, string File, int Line, string Message)
{
    /// <summary>The diagnostic as <c>file:line: code: message</c>.</summary>
    /// <returns>The diagnostic's one-line form.</returns>
    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=DB6AF8
    // Broiler-Human:        PENDING
    public override string ToString() => File + ":" + Ascii.Decimal(Line) + ": " + Code + ": " + Message;
}

/// <summary>One <c>key: value</c> line of a record (schema section 1).</summary>
/// <param name="Key">The text before the first <c>": "</c>.</param>
/// <param name="Value">The text after it.</param>
/// <param name="Line">The one-based line the field is on.</param>
// Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=A3D134
// Broiler-Human:        PENDING
public sealed record VectorField(string Key, string Value, int Line);

/// <summary>One record of a vector file: its fields in file order, duplicates kept for the validator.</summary>
// Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=2; Fingerprint=59A512
// Broiler-Human:        PENDING
public sealed class VectorRecord
{
    /// <summary>Creates a record.</summary>
    /// <param name="file">The file's name relative to the corpus.</param>
    /// <param name="fields">The record's fields in file order; at least one.</param>
    /// <exception cref="ArgumentException">The record has no field.</exception>
    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=AD3D6A
    // Broiler-Human:        PENDING
    public VectorRecord(string file, IReadOnlyList<VectorField> fields)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(fields);
        if (fields.Count == 0)
            throw new ArgumentException("a record has at least one field", nameof(fields));
        File = file;
        Fields = fields;
    }

    /// <summary>The file's name relative to the corpus.</summary>
    public string File { get; }

    /// <summary>The record's fields in file order.</summary>
    public IReadOnlyList<VectorField> Fields { get; }

    /// <summary>The line of the record's first field.</summary>
    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=53E00F
    // Broiler-Human:        PENDING
    public int Line => Fields[0].Line;

    /// <summary>The value of the first field with <paramref name="key"/>, or <see langword="null"/>.</summary>
    /// <param name="key">The field's key.</param>
    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=1B43A3
    // Broiler-Human:        PENDING
    public string? this[string key] => Find(key)?.Value;

    /// <summary>The <c>id</c> field, or empty.</summary>
    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=DE4270
    // Broiler-Human:        PENDING
    public string Id => this["id"] ?? string.Empty;

    /// <summary>The <c>class</c> field, or empty.</summary>
    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=4F7A6A
    // Broiler-Human:        PENDING
    public string Class => this["class"] ?? string.Empty;

    /// <summary>The <c>out</c> field, or empty.</summary>
    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=B42D92
    // Broiler-Human:        PENDING
    public string Out => this["out"] ?? string.Empty;

    /// <summary>The <c>script</c> field, or empty.</summary>
    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=AF43EC
    // Broiler-Human:        PENDING
    public string Script => this["script"] ?? string.Empty;

    /// <summary>The first field with <paramref name="key"/>, or <see langword="null"/>.</summary>
    /// <param name="key">The field's key.</param>
    /// <returns>The field, with its line.</returns>
    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=C117C1
    // Broiler-Human:        PENDING
    public VectorField? Find(string key)
    {
        foreach (var field in Fields)
        {
            if (field.Key == key)
                return field;
        }

        return null;
    }

    /// <summary>A copy of this record with one field's value replaced, as a negative control makes.</summary>
    /// <param name="key">The key of the fields to change.</param>
    /// <param name="value">Their new value.</param>
    /// <returns>The changed copy; this record is unchanged.</returns>
    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=2C64BC
    // Broiler-Human:        PENDING
    public VectorRecord With(string key, string value)
    {
        var fields = new List<VectorField>(Fields.Count);
        foreach (var field in Fields)
            fields.Add(field.Key == key ? field with { Value = value } : field);
        return new VectorRecord(File, fields);
    }
}

/// <summary>A vector file as read: its records, and the defects the reader found in its structure.</summary>
// Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=E475A0
// Broiler-Human:        PENDING
public sealed class VectorFile
{
    /// <summary>Creates a file.</summary>
    /// <param name="path">The file's name relative to the corpus.</param>
    /// <param name="records">Its records, in file order.</param>
    /// <param name="diagnostics">The structural defects the reader found.</param>
    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=FF22FC
    // Broiler-Human:        PENDING
    public VectorFile(string path, IReadOnlyList<VectorRecord> records, IReadOnlyList<VectorDiagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(records);
        ArgumentNullException.ThrowIfNull(diagnostics);
        Path = path;
        Records = records;
        Diagnostics = diagnostics;
    }

    /// <summary>The file's name relative to the corpus.</summary>
    public string Path { get; }

    /// <summary>Its records, in file order.</summary>
    public IReadOnlyList<VectorRecord> Records { get; }

    /// <summary>The structural defects the reader found.</summary>
    public IReadOnlyList<VectorDiagnostic> Diagnostics { get; }
}

/// <summary>
/// Reads the line format of the schema's section 1. It finds structural defects only: the encoding,
/// the schema line, and lines that are not <c>key: value</c>. What the keys and values mean is the
/// validator's.
/// </summary>
/// <remarks>
/// It reads bytes its caller hands it and never a file (convention 8), so a runner reads the
/// corpus from wherever it keeps it.
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=3; Fingerprint=A4F4C0
// Broiler-Human:        PENDING
public static class VectorReader
{
    /// <summary>The line every vector file declares before its first record.</summary>
    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=445A45
    // Broiler-Human:        PENDING
    public const string SchemaLine = "schema: 1";

    /// <summary>Reads one vector file.</summary>
    /// <param name="content">The file's bytes, which must be UTF-8 without a byte-order mark.</param>
    /// <param name="name">The name diagnostics and records give the file, relative to the corpus.</param>
    /// <returns>The records and the structural defects; reading never throws on malformed content.</returns>
    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=5; Fingerprint=36CE17
    // Broiler-Human:        PENDING
    public static VectorFile Read(ReadOnlySpan<byte> content, string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        var diagnostics = new List<VectorDiagnostic>();
        var records = new List<VectorRecord>();
        ReadOnlySpan<byte> byteOrderMark = [0xEF, 0xBB, 0xBF];
        if (content.StartsWith(byteOrderMark))
        {
            diagnostics.Add(new("encoding", name, 1, "the file starts with a byte-order mark"));
            return new VectorFile(name, records, diagnostics);
        }

        string text;
        try
        {
            text = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).GetString(content);
        }
        catch (DecoderFallbackException)
        {
            diagnostics.Add(new("encoding", name, 1, "the file is not UTF-8"));
            return new VectorFile(name, records, diagnostics);
        }

        var lines = text.Split('\n');
        var schemaSeen = false;
        var current = new List<VectorField>();
        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index];
            var number = index + 1;
            if (line.Contains('\r', StringComparison.Ordinal))
            {
                diagnostics.Add(new("encoding", name, number, "the line ends in CR; the corpus uses LF"));
                return new VectorFile(name, records, diagnostics);
            }

            if (line.StartsWith('#'))
                continue;

            if (!schemaSeen)
            {
                if (line != SchemaLine)
                {
                    diagnostics.Add(new("no-schema", name, number, "the first line that is not a comment is not '" + SchemaLine + "'"));
                    return new VectorFile(name, records, diagnostics);
                }

                schemaSeen = true;
                continue;
            }

            if (line.Length == 0)
            {
                if (current.Count > 0)
                    records.Add(new VectorRecord(name, current));
                current = [];
                continue;
            }

            var separator = line.IndexOf(": ", StringComparison.Ordinal);
            if (separator <= 0)
            {
                diagnostics.Add(new("syntax", name, number, "'" + line + "' is not key: value"));
                continue;
            }

            current.Add(new VectorField(line[..separator], line[(separator + 2)..], number));
        }

        if (current.Count > 0)
            records.Add(new VectorRecord(name, current));
        if (!schemaSeen)
            diagnostics.Add(new("no-schema", name, lines.Length, "the file has no '" + SchemaLine + "' line"));
        return new VectorFile(name, records, diagnostics);
    }
}
