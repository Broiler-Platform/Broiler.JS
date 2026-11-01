using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Broiler.ECMAScript.Intl.Tests;

/// <summary>A data set of tables given as text, which counts how often each table is read.</summary>
internal sealed class TextDataSet : IIntlDataSet
{
    private readonly Dictionary<IntlTable, byte[]> tables;

    private readonly int[] reads = new int[64];

    internal TextDataSet(IReadOnlyDictionary<IntlTable, string> tables, string cldrVersion = "48.2.0")
    {
        this.tables = tables.ToDictionary(static p => p.Key, static p => Encoding.ASCII.GetBytes(p.Value));
        CldrVersion = cldrVersion;
    }

    public string CldrVersion { get; }

    public ReadOnlySpan<byte> Table(IntlTable table)
    {
        Interlocked.Increment(ref reads[(int)table]);
        return tables.TryGetValue(table, out var bytes) ? bytes : default;
    }

    /// <summary>How often <paramref name="table"/> was read.</summary>
    internal int Reads(IntlTable table) => Volatile.Read(ref reads[(int)table]);
}

/// <summary>
/// Broiler.JS's tables as the data home commits them, <c>Broiler.ECMAScript/data/intl/generated/</c>: each table's file
/// read from disk once, named as the generator's <c>IntlTableFiles</c> names it.
/// </summary>
internal sealed class HomeDataSet : IIntlDataSet
{
    private static readonly Lazy<HomeDataSet> Instance = new(static () => new HomeDataSet(Generated()));

    private readonly Dictionary<IntlTable, byte[]> tables = [];

    private HomeDataSet(string directory)
    {
        foreach (var table in Enum.GetValues<IntlTable>())
        {
            var path = Path.Combine(directory, FileName(table, text: true));
            var binary = Path.Combine(directory, FileName(table, text: false));
            if (File.Exists(path))
                tables[table] = File.ReadAllBytes(path);
            else if (File.Exists(binary))
                tables[table] = File.ReadAllBytes(binary);
        }

        var manifest = File.ReadAllLines(Path.Combine(directory, "tables.manifest"));
        CldrVersion = manifest.Single(static l => l.StartsWith("cldr-version ", StringComparison.Ordinal))["cldr-version ".Length..];
    }

    internal static HomeDataSet Current => Instance.Value;

    public string CldrVersion { get; }

    public ReadOnlySpan<byte> Table(IntlTable table) => tables.TryGetValue(table, out var bytes) ? bytes : default;

    /// <summary>The generated area: <c>Broiler.ECMAScript/data/intl/generated/</c>, found above the test assembly.</summary>
    internal static string Generated()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "Broiler.ECMAScript", "data", "intl", "generated");
            if (File.Exists(Path.Combine(candidate, "tables.manifest")))
                return candidate;
        }

        throw new DirectoryNotFoundException("no Broiler.ECMAScript/data/intl/generated/tables.manifest above " + AppContext.BaseDirectory);
    }

    /// <summary>A table's file name as the generator writes it: <c>LikelySubtags</c> as <c>likely-subtags.txt</c>.</summary>
    internal static string FileName(IntlTable table, bool text)
    {
        var name = new StringBuilder();
        foreach (var c in table.ToString())
        {
            if (c is >= 'A' and <= 'Z' && name.Length > 0)
                name.Append('-');
            name.Append(c is >= 'A' and <= 'Z' ? (char)(c + 32) : c);
        }

        return name.Append(text ? ".txt" : ".bin").ToString();
    }
}

/// <summary>
/// The shared time zone database over the time data home's tables as committed, <c>Broiler.ECMAScript/data/time/generated/</c>:
/// what an engine hands TimeZonesOfLocale and <c>Intl.supportedValuesOf("timeZone")</c>.
/// </summary>
internal static class HomeTimeZones
{
    private static readonly Lazy<Time.TimeZoneDatabase> Instance = new(Load);

    internal static Time.TimeZoneDatabase Current => Instance.Value;

    private static Time.TimeZoneDatabase Load()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var generated = Path.Combine(directory.FullName, "Broiler.ECMAScript", "data", "time", "generated");
            if (!File.Exists(Path.Combine(generated, "tables.manifest")))
                continue;

            var release = File.ReadAllLines(Path.Combine(generated, "tables.manifest"))
                .Select(static line => line.Split(' '))
                .Single(static fields => fields.Length == 2 && fields[0] == "tzdb-release")[1];
            return new Time.TimeZoneDatabase(
                release,
                File.ReadAllBytes(Path.Combine(generated, "time-zones.bin")),
                File.ReadAllBytes(Path.Combine(generated, "time-zone-ids.txt")),
                File.ReadAllBytes(Path.Combine(generated, "time-zone-regions.txt")));
        }

        throw new DirectoryNotFoundException("no Broiler.ECMAScript/data/time/generated/tables.manifest above " + AppContext.BaseDirectory);
    }
}
