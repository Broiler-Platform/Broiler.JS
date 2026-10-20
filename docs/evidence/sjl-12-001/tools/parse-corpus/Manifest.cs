using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ParseCorpus;

/// <summary>One row of the corpus manifest (format 1, <c>conformance/parse/README.md</c>).</summary>
internal sealed record ManifestRow(
    string Id,
    string Origin,
    string Path,
    string Sha256,
    string Goal,
    IReadOnlyList<string> Variants,
    string SourceName);

/// <summary>
/// The parse corpus's manifest, read and checked as format 1 says: the header fields, the column
/// line, the row count, and each row's nine fields.
/// </summary>
internal sealed class Manifest
{
    internal const string ColumnLine = "id\torigin\tpath\tsha256\tgoal\tvariants\tnegative\teval-family\tprovenance";

    private static readonly string[] Goals = ["script", "module", "json"];
    private static readonly string[] KnownVariants = ["strict", "sloppy", "raw", "module", "json"];

    internal required string File { get; init; }
    internal required string Digest { get; init; }
    internal required string CorpusDirectory { get; init; }
    internal required string Corpus { get; init; }
    internal required string StrictPrologue { get; init; }
    internal required string VirtualRoot { get; init; }
    internal required string StoredRoot { get; init; }
    internal required string PinTest262 { get; init; }
    internal required IReadOnlyList<ManifestRow> Rows { get; init; }

    internal static Manifest Read(string file)
    {
        var bytes = System.IO.File.ReadAllBytes(file);
        var digest = Convert.ToHexStringLower(SHA256.HashData(bytes));
        var text = StrictUtf8.GetString(bytes);

        if (text.Length > 0 && text[0] == '\uFEFF')
            throw new InvalidDataException($"{file}: the manifest begins with a byte-order mark");
        if (text.Contains('\r'))
            throw new InvalidDataException($"{file}: the manifest contains a carriage return");
        if (!text.EndsWith('\n'))
            throw new InvalidDataException($"{file}: the manifest does not end with a line feed");

        var lines = text[..^1].Split('\n');
        var fields = new Dictionary<string, string[]>(StringComparer.Ordinal);
        var index = 0;

        for (; index < lines.Length && lines[index].StartsWith('#'); index++)
        {
            var line = lines[index];
            if (!line.StartsWith("#% ", StringComparison.Ordinal))
                continue;

            var parts = line[3..].Split('\t');
            // eval-flags repeats once per flag set; only Broiler.VM reads it.
            if (parts[0] == "eval-flags")
                continue;
            if (!fields.TryAdd(parts[0], parts[1..]))
                throw new InvalidDataException($"{file}: the header field {parts[0]} appears twice");
        }

        string Field(string key)
        {
            if (!fields.TryGetValue(key, out var values) || values.Length == 0)
                throw new InvalidDataException($"{file}: the header has no field {key}");
            return values[0];
        }

        var format = fields.TryGetValue("format", out var formatValues) ? string.Join("\t", formatValues) : "";
        if (format != "broiler-parse-corpus\t1")
            throw new InvalidDataException($"{file}: the header's format is \"{format}\", not \"broiler-parse-corpus\\t1\"");

        if (index >= lines.Length || lines[index] != ColumnLine)
            throw new InvalidDataException($"{file}: the column line is not \"{ColumnLine.Replace("\t", "\\t", StringComparison.Ordinal)}\"");
        index++;

        var expectedRows = int.Parse(Field("rows"), NumberStyles.None, CultureInfo.InvariantCulture);
        var strictPrologue = JsonSerializer.Deserialize<string>(Field("strict-prologue"))
            ?? throw new InvalidDataException($"{file}: the strict prologue is null");
        var virtualRoot = Field("virtual-root");
        var storedRoot = Field("stored-root");

        var rows = new List<ManifestRow>(expectedRows);
        var ids = new HashSet<string>(StringComparer.Ordinal);

        for (; index < lines.Length; index++)
        {
            var line = lines[index];
            var where = $"{file}:{(index + 1).ToString(CultureInfo.InvariantCulture)}";
            if (line.StartsWith('#'))
                throw new InvalidDataException($"{where}: a header line after the column line");

            var parts = line.Split('\t');
            if (parts.Length != 9 || parts.Any(part => part.Length == 0))
                throw new InvalidDataException($"{where}: a row has {parts.Length.ToString(CultureInfo.InvariantCulture)} fields, or an empty one; format 1 has nine, none empty");

            var (id, origin, path, sha256, goal, variants) = (parts[0], parts[1], parts[2], parts[3], parts[4], parts[5]);
            if (!ids.Add(id))
                throw new InvalidDataException($"{where}: the id {id} appears twice");
            if (Array.IndexOf(Goals, goal) < 0)
                throw new InvalidDataException($"{where}: the goal \"{goal}\" is not script, module or json");
            if (sha256.Length != 64 || sha256.Any(c => !Uri.IsHexDigit(c) || char.IsUpper(c)))
                throw new InvalidDataException($"{where}: the sha256 is not 64 lower-case hex digits");
            if (path.Split('/').Any(segment => segment is "" or "." or ".."))
                throw new InvalidDataException($"{where}: the path \"{path}\" has an empty, . or .. segment");

            var variantList = variants.Split(',');
            if (variantList.Any(variant => Array.IndexOf(KnownVariants, variant) < 0) || variantList.Distinct().Count() != variantList.Length)
                throw new InvalidDataException($"{where}: the variants \"{variants}\" are not a list of strict, sloppy, raw, module and json");

            var consistent = goal switch
            {
                "script" => variantList.All(variant => variant is "strict" or "sloppy" or "raw"),
                "module" => variantList is ["module"],
                _ => variantList is ["json"],
            };
            if (!consistent)
                throw new InvalidDataException($"{where}: the variants \"{variants}\" do not fit the goal {goal}");

            var sourceName = origin switch
            {
                "test262" => virtualRoot + path,
                "broiler-vm" or "broiler-js" => storedRoot + path,
                _ => throw new InvalidDataException($"{where}: the origin \"{origin}\" is not test262, broiler-vm or broiler-js"),
            };

            rows.Add(new ManifestRow(id, origin, path, sha256, goal, variantList, sourceName));
        }

        if (rows.Count != expectedRows)
            throw new InvalidDataException($"{file}: the header says {expectedRows.ToString(CultureInfo.InvariantCulture)} rows and the manifest has {rows.Count.ToString(CultureInfo.InvariantCulture)}");

        return new Manifest
        {
            File = file,
            Digest = digest,
            CorpusDirectory = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(file))!,
            Corpus = Field("corpus"),
            StrictPrologue = strictPrologue,
            VirtualRoot = virtualRoot,
            StoredRoot = storedRoot,
            PinTest262 = Field("pin-test262"),
            Rows = rows,
        };
    }

    /// <summary>
    /// Checks every row's input before anything is parsed: it exists, its bytes have the manifest's
    /// SHA-256, and they are strict UTF-8. A run is refused if any input fails.
    /// </summary>
    internal void Verify(string test262Root, IReadOnlyList<ManifestRow> rows, TextWriter log)
    {
        var failures = new List<string>();
        foreach (var row in rows)
        {
            try
            {
                Load(test262Root, row);
            }
            catch (Exception exception) when (exception is IOException or InvalidDataException or UnauthorizedAccessException)
            {
                failures.Add($"{row.Id} {row.Path}: {exception.Message}");
            }
        }

        if (failures.Count > 0)
        {
            foreach (var failure in failures.Take(50))
                log.WriteLine("parse-corpus: " + failure);
            throw new InvalidDataException($"{failures.Count.ToString(CultureInfo.InvariantCulture)} input(s) are missing or differ from the manifest; nothing was run");
        }
    }

    /// <summary>
    /// A row's text: read from the suite (test262 rows) or the corpus directory (the others),
    /// checked against the manifest's SHA-256, and decoded as strict UTF-8, unchanged (format 1:
    /// no line-ending translation, no trimming).
    /// </summary>
    internal string Load(string test262Root, ManifestRow row)
    {
        var root = row.Origin == "test262" ? test262Root : CorpusDirectory;
        var path = System.IO.Path.Combine(root, row.Path.Replace('/', System.IO.Path.DirectorySeparatorChar));
        var bytes = System.IO.File.ReadAllBytes(path);
        var actual = Convert.ToHexStringLower(SHA256.HashData(bytes));
        if (actual != row.Sha256)
            throw new InvalidDataException($"SHA-256 {actual}, the manifest says {row.Sha256}");

        try
        {
            return StrictUtf8.GetString(bytes);
        }
        catch (DecoderFallbackException exception)
        {
            throw new InvalidDataException("not UTF-8: " + exception.Message);
        }
    }

    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
}
