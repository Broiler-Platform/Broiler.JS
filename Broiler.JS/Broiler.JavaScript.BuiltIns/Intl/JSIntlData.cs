using System;
using System.IO;
using System.Text;
using System.Threading;
using Broiler.ECMAScript.Intl;

namespace Broiler.JavaScript.BuiltIns.Intl;

/// <summary>
/// This engine's locale data for the shared Intl library (milestone SJL-9.5, task 8.1; Broiler.VM's JSD-0082, decisions
/// D5, D8 and D11, decision SJL-D-6 (c)): the tables the data home's generator writes for Broiler.JS's locale list into
/// <c>Broiler.ECMAScript/data/intl/generated/</c>, embedded in this assembly from there as the time tables are
/// (<see cref="Temporal.TemporalTimeData"/>), handed to <c>Broiler.ECMAScript.Intl</c> as an <see cref="IIntlDataSet"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>The files are linked from the home, never copied</b>, so the embedded bytes are the home's, which rule SL7 holds to a
/// regeneration; each file's logical name is <see cref="ResourcePrefix"/> and the home's file name, and the CLDR release is
/// the manifest's <c>cldr-version</c> line.
/// </para>
/// <para>
/// <b>Each table is read from the assembly once per process</b>, on first use, lazily and thread-safely, and kept; the
/// library's <see cref="TableLocaleData"/> then decodes each one once (decision D23's L3 reading).
/// <see cref="LocaleData"/> is the one <see cref="ILocaleData"/> this engine asks, the decoder in
/// <see cref="CompositeLocaleData"/>'s order: the shared tables, then the host-culture provider for a locale the shared data
/// lacks (<see cref="HostCultureLocaleData"/>, decision D6), then the locale's parent and root.
/// </para>
/// </remarks>
internal sealed class JSIntlData : IIntlDataSet
{
    // The embedded files' logical names: this prefix and the home's file name.
    internal const string ResourcePrefix = "Broiler.JavaScript.BuiltIns.IntlData.";

    private static readonly Lazy<JSIntlData> EmbeddedTables =
        new(static () => new JSIntlData(cache: true), LazyThreadSafetyMode.ExecutionAndPublication);

    private static readonly Lazy<TableLocaleData> SharedData =
        new(static () => new TableLocaleData(Embedded), LazyThreadSafetyMode.ExecutionAndPublication);

    private static readonly Lazy<ILocaleData> Composite =
        new(static () => new CompositeLocaleData(SharedData.Value, HostCultureLocaleData.Host), LazyThreadSafetyMode.ExecutionAndPublication);

    private readonly bool cache;

    private readonly Lazy<byte[]>[] tables;

    // How often each table's resource was read from the assembly, by table number.
    private readonly int[] reads;

    private readonly Lazy<string> cldrVersion;

    private JSIntlData(bool cache)
    {
        this.cache = cache;
        var values = Enum.GetValues<IntlTable>();
        var size = 0;
        foreach (var value in values)
            size = Math.Max(size, (int)value + 1);

        tables = new Lazy<byte[]>[size];
        reads = new int[size];
        foreach (var value in values)
        {
            var table = value;
            tables[(int)table] = new Lazy<byte[]>(() => Load(table), LazyThreadSafetyMode.ExecutionAndPublication);
        }

        cldrVersion = new Lazy<string>(() => ManifestValue("cldr-version"), LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <summary>The embedded tables, one instance per process.</summary>
    internal static JSIntlData Embedded => EmbeddedTables.Value;

    /// <summary>The library's decoder over the embedded tables, one per process.</summary>
    internal static TableLocaleData Shared => SharedData.Value;

    /// <summary>
    /// The locale data every Intl question of this engine reads: the shared tables, then the host-culture provider at a
    /// locale the shared data lacks, then the locale's parent and root, in <see cref="CompositeLocaleData"/>'s order. One per
    /// process.
    /// </summary>
    internal static ILocaleData LocaleData => Composite.Value;

    /// <inheritdoc/>
    public string CldrVersion => cldrVersion.Value;

    /// <inheritdoc/>
    public ReadOnlySpan<byte> Table(IntlTable table)
    {
        var index = (int)table;
        if (index <= 0 || index >= tables.Length || tables[index] is not { } lazy)
            return default;

        return cache ? lazy.Value : Load(table);
    }

    /// <summary>How often <paramref name="table"/>'s resource has been read from the assembly by this instance.</summary>
    internal int Reads(IntlTable table)
    {
        var index = (int)table;
        return index > 0 && index < reads.Length ? Volatile.Read(ref reads[index]) : 0;
    }

    /// <summary>
    /// A second instance that reads a table's resource on every call, never keeping it: the witness of the load-once test,
    /// whose count the test's own assertion refuses.
    /// </summary>
    internal static JSIntlData Uncached() => new(cache: false);

    /// <summary>A table's file name in the home's <c>generated/</c> directory, or <see langword="null"/> for a table this embedding does not carry.</summary>
    internal static string FileName(IntlTable table) => table switch
    {
        IntlTable.LikelySubtags => "likely-subtags.txt",
        IntlTable.Aliases => "aliases.txt",
        IntlTable.Extensions => "extensions.txt",
        IntlTable.Locales => "locales.txt",
        IntlTable.CollationRoot => "collation-root.bin",
        IntlTable.CollationTailorings => "collation-tailorings.bin",
        IntlTable.NumberLocales => "number-locales.txt",
        IntlTable.Currencies => "currencies.txt",
        IntlTable.CurrencyDigits => "currency-digits.txt",
        IntlTable.NumberingSystems => "numbering-systems.txt",
        IntlTable.Plurals => "plurals.txt",
        IntlTable.PluralRanges => "plural-ranges.txt",
        IntlTable.Units => "units.txt",
        IntlTable.DateLocales => "date-locales.txt",
        IntlTable.TimeData => "time-data.txt",
        IntlTable.DayPeriods => "day-periods.txt",
        IntlTable.WeekData => "week-data.txt",
        IntlTable.Scripts => "scripts.txt",
        IntlTable.Ordinals => "ordinals.txt",
        IntlTable.ListPatterns => "list-patterns.txt",
        IntlTable.RelativeTimes => "relative-times.txt",
        IntlTable.DisplayNames => "display-names.txt",
        IntlTable.MetaZones => "meta-zones.txt",
        IntlTable.CollationSettings => "collation-settings.txt",
        // CLDR's calendar preferences by region (added 2026-11-09, at SJL-9.5's stage 4, with the shared library's merge
        // Broiler-Platform/Broiler.JS#1108, which appends the table; DateTimeFormat.CalendarKeyValuesOf reads it).
        IntlTable.CalendarPreferences => "calendar-preferences.txt",
        // The time zone and calendar tables (26 to 29, 31) are the time data home's, which TemporalTimeData embeds.
        _ => null,
    };

    /// <summary>An embedded file's bytes, by its name in the home's <c>generated/</c> directory.</summary>
    internal static byte[] Read(string file)
    {
        using var stream = typeof(JSIntlData).Assembly.GetManifestResourceStream(ResourcePrefix + file)
            ?? throw new InvalidOperationException($"The embedded Intl data file {file} is missing.");
        using var copy = new MemoryStream();
        stream.CopyTo(copy);
        return copy.ToArray();
    }

    /// <summary>A <c>&lt;key&gt; &lt;value&gt;</c> line of the embedded manifest, for example <c>cldr-version</c>.</summary>
    internal static string ManifestValue(string key)
    {
        foreach (var line in Encoding.ASCII.GetString(Read("tables.manifest")).Split('\n'))
        {
            var space = line.IndexOf(' ');
            if (space > 0 && !line.StartsWith('#') && line.AsSpan(0, space).SequenceEqual(key))
                return line[(space + 1)..].Trim();
        }

        throw new InvalidOperationException($"The embedded Intl data manifest has no {key} line.");
    }

    private byte[] Load(IntlTable table)
    {
        if (FileName(table) is not { } file)
            return [];

        Interlocked.Increment(ref reads[(int)table]);
        return Read(file);
    }
}
