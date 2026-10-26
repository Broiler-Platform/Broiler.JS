using System;
using System.Collections.Generic;
using System.IO;
using Broiler.ECMAScript.Conformance;

namespace Broiler.ECMAScript.Runners;

/// <summary>
/// The corpus in its home directory, conformance/vectors/ in this repository, read from disk for a
/// runner. The runners read no other copy.
/// </summary>
/// <remarks>
/// <b>This file is linked into each runner that reads the corpus from disk and is never compiled
/// into a library.</b> The Conformance library takes the corpus as bytes and does no I/O
/// (convention 8), so the reading lives here, once, for the shared test project, the Native AOT
/// probe and Broiler.JS's engine-level runner.
/// </remarks>
internal static class CorpusDirectory
{
    /// <summary>
    /// The corpus's home, found from <paramref name="start"/> upwards: the first directory with a
    /// conformance/vectors/SCHEMA.md below it.
    /// </summary>
    internal static string Home(string start)
    {
        for (var directory = new DirectoryInfo(start); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "conformance", "vectors");
            if (File.Exists(Path.Combine(candidate, "SCHEMA.md")))
                return candidate;
        }

        throw new DirectoryNotFoundException("no conformance/vectors/SCHEMA.md above " + start);
    }

    /// <summary>What the corpus at <paramref name="root"/> pins; sources/ecma402.pin from corpus revision 27.</summary>
    internal static VectorSources Sources(string root)
    {
        var ecma402 = Path.Combine(root, "sources", "ecma402.pin");
        return VectorSources.Parse(
            File.ReadAllText(Path.Combine(root, "REVISIONS.md")),
            File.ReadAllText(Path.Combine(root, "sources", "ecma262.pin")),
            File.ReadAllText(Path.Combine(root, "sources", "unicode.pin")),
            File.Exists(ecma402) ? File.ReadAllText(ecma402) : null);
    }

    /// <summary>The corpus at <paramref name="root"/>, read and validated.</summary>
    internal static VectorCorpus Load(string root) => VectorCorpus.Load(Sources(root), Files(root));

    /// <summary>The corpus's prelude, which every engine-level runner evaluates before a vector's script.</summary>
    internal static string Prelude(string root) => File.ReadAllText(Path.Combine(root, "prelude.js"));

    /// <summary>
    /// The corpus's vector files relative to <paramref name="root"/>, '/'-separated and in ordinal
    /// order, the negative controls excluded.
    /// </summary>
    internal static IReadOnlyList<string> VectorFileNames(string root)
    {
        var names = new List<string>();
        foreach (var path in Directory.EnumerateFiles(root, "*.vectors", SearchOption.AllDirectories))
        {
            var name = Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/');
            if (VectorCorpus.IsVectorFile(name))
                names.Add(name);
        }

        names.Sort(StringComparer.Ordinal);
        return names;
    }

    /// <summary>One file of the corpus, by its name relative to <paramref name="root"/>.</summary>
    internal static CorpusFile Read(string root, string name)
        => new(name, File.ReadAllBytes(Path.Combine(root, name.Replace('/', Path.DirectorySeparatorChar))));

    private static IEnumerable<CorpusFile> Files(string root)
    {
        foreach (var name in VectorFileNames(root))
            yield return Read(root, name);
    }
}
