using System.Text.RegularExpressions;

namespace Broiler.JavaScript.Vectors.Tests.Corpus;

/// <summary>
/// What the corpus's own documents pin: its revision (REVISIONS.md) and the specification and Unicode
/// versions its provenance may name (sources/*.pin).
/// </summary>
public sealed partial record VectorSources(int Revision, string SpecificationRevision, string UnicodeVersion)
{
    public static VectorSources Read(string root)
    {
        var revisions = File.ReadAllLines(Path.Combine(root, "REVISIONS.md"))
            .Select(line => RevisionRow().Match(line))
            .Where(match => match.Success)
            .Select(match => int.Parse(match.Groups[1].Value))
            .ToList();
        if (revisions.Count == 0 || !revisions.SequenceEqual(Enumerable.Range(1, revisions.Count)))
            throw new InvalidDataException("REVISIONS.md does not list revisions 1, 2, ... in order");

        return new VectorSources(
            revisions[^1],
            PinValue(Path.Combine(root, "sources", "ecma262.pin"), "revision"),
            PinValue(Path.Combine(root, "sources", "unicode.pin"), "version"));
    }

    private static string PinValue(string path, string key)
        => File.ReadLines(path)
            .Where(line => !line.StartsWith('#'))
            .Select(line => line.Split(' ', 2))
            .FirstOrDefault(parts => parts.Length == 2 && parts[0] == key)?[1]
            ?? throw new InvalidDataException($"{path} has no '{key}' line");

    [GeneratedRegex(@"^\| ([1-9][0-9]*) \| [0-9]{4}-[0-9]{2}-[0-9]{2} \|")]
    private static partial System.Text.RegularExpressions.Regex RevisionRow();
}

/// <summary>
/// The corpus in its home directory: every <c>.vectors</c> file below it except the negative
/// controls, read and validated.
/// </summary>
public sealed class VectorCorpus
{
    public const string ControlsDirectory = "controls";

    private VectorCorpus(string root, VectorSources sources, IReadOnlyList<VectorFile> files, IReadOnlyList<VectorDiagnostic> diagnostics)
    {
        Root = root;
        Sources = sources;
        Files = files;
        Diagnostics = diagnostics;
    }

    public string Root { get; }

    public VectorSources Sources { get; }

    public IReadOnlyList<VectorFile> Files { get; }

    public IReadOnlyList<VectorDiagnostic> Diagnostics { get; }

    public IEnumerable<VectorRecord> Records => Files.SelectMany(f => f.Records);

    /// <summary>The script every engine-level runner evaluates before a vector's own.</summary>
    public string Prelude => File.ReadAllText(Path.Combine(Root, "prelude.js"));

    public static VectorCorpus Load(string root)
    {
        var sources = VectorSources.Read(root);
        var validator = new VectorValidator(sources);
        var files = new List<VectorFile>();
        foreach (var name in VectorFileNames(root))
        {
            var file = VectorReader.Read(Path.Combine(root, name), name);
            validator.Validate(file);
            files.Add(file);
        }

        return new VectorCorpus(root, sources, files, validator.Diagnostics);
    }

    /// <summary>
    /// The corpus's vector files relative to <paramref name="root"/>, '/'-separated and in ordinal
    /// order, the negative controls excluded.
    /// </summary>
    public static IReadOnlyList<string> VectorFileNames(string root)
        => Directory.EnumerateFiles(root, "*.vectors", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/'))
            .Where(name => !name.StartsWith(ControlsDirectory + "/", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToList();

    /// <summary>
    /// The corpus's home: conformance/vectors/ in this repository, found from the test assembly's
    /// directory. The runners read no other copy.
    /// </summary>
    public static string Home()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "conformance", "vectors");
            if (File.Exists(Path.Combine(candidate, "SCHEMA.md")))
                return candidate;
        }

        throw new DirectoryNotFoundException($"no conformance/vectors/SCHEMA.md above {AppContext.BaseDirectory}");
    }
}
