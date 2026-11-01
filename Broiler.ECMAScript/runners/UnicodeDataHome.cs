using System;
using System.Collections.Generic;
using System.IO;
using Broiler.Unicode.Algorithms;
using Broiler.Unicode.Generation;

namespace Broiler.ECMAScript.Runners;

/// <summary>
/// This repository's Unicode tables as the owner's generator writes them: the pin <c>data/unicode/pins/unicode.pin</c>
/// and the files it names, read from disk, and the tables <c>Broiler.Unicode.Generation</c> writes from them, served as
/// Broiler.Unicode's <see cref="IUnicodeTables"/> (milestone SJL-13.2b; Broiler.VM's JSD-0083, decision D8).
/// </summary>
/// <remarks>
/// <para>
/// <b>Why the runner generates them.</b> The frontend's tokenizer reads its identifier and white-space classes through the
/// request's Unicode provider, which has no default (decision D8), and the home carries no table (decision SJL-D-6 (c):
/// each engine embeds its own). The pin is the one Broiler.JS's engine embeds its tables from, which Broiler.VM's rule I4
/// and bundle <c>sjl-7-001</c> hold equal to Broiler.VM's, so the runner's classes are both engines'. The generator checks
/// every file's length and SHA-256 against the pin before it reads it.
/// </para>
/// <para>
/// <b>This file is linked into the test projects and the probe, and is never compiled into a library</b>: it reads files,
/// which convention 8 keeps out of the libraries. The tables are generated once per process, on first use.
/// </para>
/// </remarks>
internal static class UnicodeDataHome
{
    /// <summary>The pin, relative to the repository root.</summary>
    internal const string PinPath = "data/unicode/pins/unicode.pin";

    private static readonly Lazy<GeneratedTables> tables = new(static () => Generate(Root(AppContext.BaseDirectory)));

    /// <summary>The tables generated from this repository's pin.</summary>
    internal static IUnicodeTables Tables => tables.Value;

    /// <summary>The repository root above <paramref name="start"/>: the first directory with the pin below it.</summary>
    internal static string Root(string start)
    {
        for (var directory = new DirectoryInfo(start); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, PinPath.Replace('/', Path.DirectorySeparatorChar))))
                return directory.FullName;
        }

        throw new DirectoryNotFoundException("no " + PinPath + " above " + start);
    }

    /// <summary>Reads the pin and its files under <paramref name="root"/> and generates every table from them.</summary>
    internal static GeneratedTables Generate(string root)
    {
        var pinFile = Path.Combine(root, PinPath.Replace('/', Path.DirectorySeparatorChar));
        var pin = UnicodePin.Parse(File.ReadAllText(pinFile));
        var directory = Path.GetDirectoryName(pinFile)!;
        var files = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        foreach (var entry in pin.Entries)
        {
            var relative = entry.Name.Replace('/', Path.DirectorySeparatorChar);
            files[entry.Name] = File.ReadAllBytes(entry.Kind == "file" ? Path.Combine(directory, relative) : Path.Combine(root, relative));
        }

        var generated = new Dictionary<UnicodeTable, byte[]>();
        foreach (var (table, bytes) in UnicodeTableGenerator.Generate(pin, files))
            generated[table] = bytes;

        return new GeneratedTables(pin.Version, generated);
    }

    /// <summary>The generated tables, served by table; a table the generator does not write is empty.</summary>
    internal sealed class GeneratedTables(string version, IReadOnlyDictionary<UnicodeTable, byte[]> tables) : IUnicodeTables
    {
        /// <inheritdoc/>
        public string UnicodeVersion => version;

        /// <inheritdoc/>
        public ReadOnlySpan<byte> Table(UnicodeTable table) => tables.TryGetValue(table, out var bytes) ? bytes : ReadOnlySpan<byte>.Empty;
    }
}
