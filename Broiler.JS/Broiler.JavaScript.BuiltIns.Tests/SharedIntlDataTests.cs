using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Broiler.ECMAScript.Intl;

namespace Broiler.JavaScript.BuiltIns.Tests;

// The Intl data home's tables, embedded (milestone SJL-9.5, task 8.1; Broiler.VM's JSD-0082, decisions D5, D8, D11 and
// D23): BuiltIns embeds every file of Broiler.ECMAScript/data/intl/generated/ as the time tables are, and hands them to
// Broiler.ECMAScript.Intl as one IIntlDataSet, each table read from the assembly once per process (the L3 test below, with
// its witness).
public class SharedIntlDataTests
{
    private const string ResourcePrefix = "Broiler.JavaScript.BuiltIns.IntlData.";

    private static Assembly BuiltIns => typeof(RegExp.JSRegExp).Assembly;

    private static Type DataSetType => BuiltIns.GetType("Broiler.JavaScript.BuiltIns.Intl.JSIntlData", throwOnError: true)!;

    // The engine's one data set, its decoder and its composite (internal; read by reflection, as the time tables' tests do).
    private static IIntlDataSet DataSet => (IIntlDataSet)Static("Embedded");

    private static ILocaleData Shared => (ILocaleData)Static("Shared");

    private static ILocaleData LocaleData => (ILocaleData)Static("LocaleData");

    private static object Static(string name) =>
        DataSetType.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;

    private static int Reads(IIntlDataSet data, IntlTable table) =>
        (int)DataSetType.GetMethod("Reads", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(data, [table])!;

    private static byte[] Resource(string file)
    {
        using var stream = BuiltIns.GetManifestResourceStream(ResourcePrefix + file);
        Assert.NotNull(stream);
        using var copy = new MemoryStream();
        stream.CopyTo(copy);
        return copy.ToArray();
    }

    private static string Generated()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "Broiler.ECMAScript", "data", "intl", "generated");
            if (File.Exists(Path.Combine(candidate, "tables.manifest")))
                return candidate;
        }

        throw new DirectoryNotFoundException("no Broiler.ECMAScript/data/intl/generated above " + AppContext.BaseDirectory);
    }

    public static TheoryData<string> HomeFiles()
    {
        var files = new TheoryData<string>();
        foreach (var path in Directory.GetFiles(Generated()).OrderBy(static p => p, StringComparer.Ordinal))
            files.Add(Path.GetFileName(path));
        return files;
    }

    // The tables are linked from the data home, not copied: every file of generated/ is embedded, byte for byte.
    [Theory]
    [MemberData(nameof(HomeFiles))]
    public void Each_file_of_the_data_home_is_embedded_as_it_is(string file)
        => Assert.Equal(File.ReadAllBytes(Path.Combine(Generated(), file)), Resource(file));

    // And nothing else is embedded under the prefix.
    [Fact]
    public void The_embedded_files_are_the_data_home_s_and_no_others()
    {
        var embedded = BuiltIns.GetManifestResourceNames()
            .Where(static name => name.StartsWith(ResourcePrefix, StringComparison.Ordinal))
            .Select(static name => name[ResourcePrefix.Length..])
            .OrderBy(static name => name, StringComparer.Ordinal);
        var home = Directory.GetFiles(Generated()).Select(Path.GetFileName).OrderBy(static name => name, StringComparer.Ordinal);
        Assert.Equal(home, embedded);
    }

    // Each embedded table is what the manifest's `table` line records, its length and its SHA-256, and the data set names
    // every one of them; its CLDR release is the manifest's.
    [Fact]
    public void Each_embedded_table_matches_the_manifest_and_the_data_set_reads_it()
    {
        var manifest = Encoding.ASCII.GetString(Resource("tables.manifest")).Split('\n');
        var lines = manifest.Where(static line => line.StartsWith("table ", StringComparison.Ordinal)).Select(static line => line.Split(' ')).ToList();
        Assert.Equal(Directory.GetFiles(Generated()).Length - 1, lines.Count);

        var fileName = DataSetType.GetMethod("FileName", BindingFlags.NonPublic | BindingFlags.Static)!;
        var named = Enum.GetValues<IntlTable>().Select(table => (string?)fileName.Invoke(null, [table])).Where(static f => f is not null).ToHashSet();
        foreach (var fields in lines)
        {
            var bytes = Resource(fields[1]);
            Assert.Equal(long.Parse(fields[2]), bytes.LongLength);
            Assert.Equal(fields[3], Convert.ToHexStringLower(SHA256.HashData(bytes)));
            Assert.Contains(fields[1], named);
        }

        Assert.Equal("48.2.0", DataSet.CldrVersion);
        Assert.Contains("cldr-version " + DataSet.CldrVersion, manifest);
    }

    // The data set answers each table as the home holds it, and an empty span for a table it does not carry (the time zone
    // and calendar tables, which TemporalTimeData embeds, and the retired numbers).
    [Fact]
    public void The_data_set_answers_the_home_s_tables_and_nothing_for_another()
    {
        Assert.Equal(File.ReadAllBytes(Path.Combine(Generated(), "likely-subtags.txt")), DataSet.Table(IntlTable.LikelySubtags).ToArray());
        Assert.Equal(File.ReadAllBytes(Path.Combine(Generated(), "collation-root.bin")), DataSet.Table(IntlTable.CollationRoot).ToArray());
        foreach (var table in (IntlTable[])[IntlTable.None, IntlTable.TimeZones, IntlTable.TimeZoneIds, IntlTable.TimeZoneRegions, IntlTable.Calendars, IntlTable.TimeZoneRelease, (IntlTable)7, (IntlTable)23, (IntlTable)99])
            Assert.True(DataSet.Table(table).IsEmpty, table.ToString());
    }

    // L3 (decision D23): each table is read from the assembly once per process, lazily and thread-safely, and the engine has
    // one decoder over them, which decodes each once (the library's own L3 test). Many questions on many threads read each
    // table they need exactly once.
    [Fact]
    public void Each_table_loads_once_lazily_and_thread_safely()
    {
        var data = DataSet;
        Assert.Same(data, DataSet);
        Assert.Same(Shared, Shared);
        Assert.Same(LocaleData, LocaleData);

        Parallel.For(0, 64, i =>
        {
            var locale = LocaleData;
            _ = locale.LikelySubtags("en");
            _ = locale.Alias(LocaleAliasKind.Language, "iw");
            _ = locale.IsExtensionKey('u', "ca");
            _ = locale.Locales;
            _ = locale.NumberValue("de", "symbols.decimal");
            _ = locale.PluralRules("fr", PluralRuleType.Cardinal);
            _ = locale.DateValue("ja", "gregory", "months.format.wide.1");
            _ = locale.Week("US");
            _ = locale.HourCycles("GB");
            _ = locale.IsRightToLeft("Arab");
            _ = data.Table(IntlTable.Aliases).Length;
        });

        foreach (var table in (IntlTable[])[IntlTable.LikelySubtags, IntlTable.Aliases, IntlTable.Extensions, IntlTable.Locales,
            IntlTable.NumberLocales, IntlTable.Plurals, IntlTable.DateLocales, IntlTable.WeekData, IntlTable.TimeData, IntlTable.Scripts])
        {
            Assert.Equal(1, Reads(data, table));
        }

        AssertEachTableLoadedAtMostOnce(data);
    }

    // The witness: a data set that reads a table's resource on every call is refused by the same assertion, so the test
    // above cannot pass by counting nothing.
    [Fact]
    public void The_load_once_assertion_refuses_a_table_read_twice()
    {
        var uncached = (IIntlDataSet)DataSetType.GetMethod("Uncached", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, null)!;
        Assert.Equal(0, Reads(uncached, IntlTable.LikelySubtags));
        Assert.Equal(uncached.Table(IntlTable.LikelySubtags).ToArray(), uncached.Table(IntlTable.LikelySubtags).ToArray());
        Assert.Equal(2, Reads(uncached, IntlTable.LikelySubtags));
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertEachTableLoadedAtMostOnce(uncached));
    }

    private static void AssertEachTableLoadedAtMostOnce(IIntlDataSet data)
    {
        foreach (var table in Enum.GetValues<IntlTable>())
            Assert.InRange(Reads(data, table), 0, 1);
    }
}
