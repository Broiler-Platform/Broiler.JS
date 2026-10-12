using System.Text;

namespace Broiler.JavaScript.Vectors.Tests.Corpus;

/// <summary>A defect the reader or the validator found, at a line of a file.</summary>
/// <param name="Code">A stable name for the kind of defect, as the controls' file names give it.</param>
public sealed record VectorDiagnostic(string Code, string File, int Line, string Message)
{
    public override string ToString() => $"{File}:{Line}: {Code}: {Message}";
}

public sealed record VectorField(string Key, string Value, int Line);

/// <summary>One record of a vector file: its fields in file order, duplicates kept for the validator.</summary>
public sealed class VectorRecord(string file, IReadOnlyList<VectorField> fields)
{
    public string File { get; } = file;

    public IReadOnlyList<VectorField> Fields { get; } = fields;

    public int Line => Fields[0].Line;

    /// <summary>The value of the first field with <paramref name="key"/>, or null.</summary>
    public string? this[string key] => Fields.FirstOrDefault(f => f.Key == key)?.Value;

    public string Id => this["id"] ?? string.Empty;

    public string Class => this["class"] ?? string.Empty;

    public string Out => this["out"] ?? string.Empty;

    public string Script => this["script"] ?? string.Empty;

    /// <summary>A copy of this record with one field's value replaced, as a negative control makes.</summary>
    public VectorRecord With(string key, string value)
        => new(File, Fields.Select(f => f.Key == key ? f with { Value = value } : f).ToList());
}

/// <summary>A vector file as read: its records, and the defects the reader found in its structure.</summary>
public sealed class VectorFile(string path, IReadOnlyList<VectorRecord> records, IReadOnlyList<VectorDiagnostic> diagnostics)
{
    public string Path { get; } = path;

    public IReadOnlyList<VectorRecord> Records { get; } = records;

    public IReadOnlyList<VectorDiagnostic> Diagnostics { get; } = diagnostics;
}

/// <summary>
/// Reads the line format of the schema's section 1. It finds structural defects only: the encoding,
/// the schema line, and lines that are not <c>key: value</c>. What the keys and values mean is the
/// validator's.
/// </summary>
public static class VectorReader
{
    public const string SchemaLine = "schema: 1";

    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <param name="path">The file to read.</param>
    /// <param name="name">The name diagnostics and records give the file, relative to the corpus.</param>
    public static VectorFile Read(string path, string name)
    {
        var diagnostics = new List<VectorDiagnostic>();
        var records = new List<VectorRecord>();
        var bytes = System.IO.File.ReadAllBytes(path);
        if (bytes.AsSpan().StartsWith(new byte[] { 0xEF, 0xBB, 0xBF }))
        {
            diagnostics.Add(new("encoding", name, 1, "the file starts with a byte-order mark"));
            return new VectorFile(name, records, diagnostics);
        }

        string text;
        try
        {
            text = StrictUtf8.GetString(bytes);
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
            if (line.Contains('\r'))
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
                    diagnostics.Add(new("no-schema", name, number, $"the first line that is not a comment is not '{SchemaLine}'"));
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
                diagnostics.Add(new("syntax", name, number, $"'{line}' is not key: value"));
                continue;
            }

            current.Add(new VectorField(line[..separator], line[(separator + 2)..], number));
        }

        if (current.Count > 0)
            records.Add(new VectorRecord(name, current));
        if (!schemaSeen)
            diagnostics.Add(new("no-schema", name, lines.Length, $"the file has no '{SchemaLine}' line"));
        return new VectorFile(name, records, diagnostics);
    }
}
