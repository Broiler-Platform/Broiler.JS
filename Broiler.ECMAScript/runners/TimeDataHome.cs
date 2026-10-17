using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Broiler.ECMAScript.Generation.Time;

namespace Broiler.ECMAScript.Runners;

/// <summary>
/// The time data home, <c>Broiler.ECMAScript/data/time/</c> in this repository (milestone SJL-8.2): its
/// pins and archives read from disk, the tables <c>Broiler.ECMAScript.Generation</c> writes from them, and
/// the files of its generated area, <c>generated/</c>, as they are committed and as the generator writes
/// them.
/// </summary>
/// <remarks>
/// <b>This file is linked into the test projects that read the home from disk and is never compiled into
/// a library.</b> The generator takes each pin's text and each archive's bytes and does no I/O (convention
/// 8), so the reading lives here, once, for rule SL7 in the architecture tests and for the generator's own
/// tests.
/// </remarks>
internal static class TimeDataHome
{
    /// <summary>The environment variable that turns rule SL7's comparison of the generated area into the generator's write.</summary>
    internal const string WriteVariable = "BROILER_ECMASCRIPT_TIME_WRITE";

    /// <summary>The binary zone table, <see cref="TzdbTables.TimeZones"/>.</summary>
    internal const string TimeZonesFile = "time-zones.bin";

    /// <summary>The identifier table, <see cref="TzdbTables.TimeZoneIds"/>.</summary>
    internal const string TimeZoneIdsFile = "time-zone-ids.txt";

    /// <summary>The region table, <see cref="TzdbTables.TimeZoneRegions"/>.</summary>
    internal const string TimeZoneRegionsFile = "time-zone-regions.txt";

    /// <summary>The calendar year table, <see cref="CalendarTables.Calendars"/>.</summary>
    internal const string CalendarsFile = "calendars.bin";

    /// <summary>The manifest of the tables: their versions, lengths and SHA-256.</summary>
    internal const string ManifestFile = "tables.manifest";

    /// <summary>
    /// The home, found from <paramref name="start"/> upwards: the first directory with a
    /// Broiler.ECMAScript/data/time/tzdb/tzdb.pin below it.
    /// </summary>
    internal static string Home(string start)
    {
        for (var directory = new DirectoryInfo(start); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "Broiler.ECMAScript", "data", "time");
            if (File.Exists(Path.Combine(candidate, "tzdb", "tzdb.pin")))
                return candidate;
        }

        throw new DirectoryNotFoundException("no Broiler.ECMAScript/data/time/tzdb/tzdb.pin above " + start);
    }

    /// <summary>The home this test run reads: the one above the test assembly.</summary>
    internal static string Current => Home(AppContext.BaseDirectory);

    /// <summary>The generated area of <paramref name="home"/>.</summary>
    internal static string Generated(string home) => Path.Combine(home, "generated");

    internal static string TzdbPinPath(string home) => Path.Combine(home, "tzdb", "tzdb.pin");

    internal static string CldrPinPath(string home) => Path.Combine(home, "cldr", "cldr.pin");

    internal static string CalendarPinPath(string home) => Path.Combine(home, "calendars", "calendars.pin");

    internal static TzdbPin ReadTzdbPin(string home) => TzdbPin.Parse(File.ReadAllText(TzdbPinPath(home)));

    internal static CldrPin ReadCldrPin(string home) => CldrPin.Parse(File.ReadAllText(CldrPinPath(home)));

    internal static CalendarPin ReadCalendarPin(string home) => CalendarPin.Parse(File.ReadAllText(CalendarPinPath(home)));

    /// <summary>The tz tarball the pin names, off disk.</summary>
    internal static byte[] Tarball(string home, TzdbPin pin) => File.ReadAllBytes(Path.Combine(home, "tzdb", pin.ArchiveName));

    /// <summary>Every file the CLDR pin names, off disk, by the pin's own list and nothing else.</summary>
    internal static IReadOnlyDictionary<string, byte[]> CldrFiles(string home, CldrPin pin) =>
        pin.Entries.ToDictionary(
            static entry => entry.Name,
            entry => File.ReadAllBytes(Path.Combine(home, "cldr", entry.Name.Replace('/', Path.DirectorySeparatorChar))),
            StringComparer.Ordinal);

    /// <summary>Every crate the calendar pin names, off disk.</summary>
    internal static IReadOnlyDictionary<string, byte[]> Crates(string home, CalendarPin pin) =>
        pin.Archives.ToDictionary(
            static archive => archive.Name,
            archive => File.ReadAllBytes(Path.Combine(home, "calendars", archive.Name)),
            StringComparer.Ordinal);

    /// <summary>The time zone tables, generated from the home's pins.</summary>
    internal static TzdbTables GenerateTzdbTables(string home)
    {
        var tzdb = ReadTzdbPin(home);
        var cldr = ReadCldrPin(home);
        return TzdbTableGenerator.Generate(tzdb, tzdb.Extract(Tarball(home, tzdb)), cldr, CldrFiles(home, cldr));
    }

    /// <summary>The calendar table, generated from the home's pin.</summary>
    internal static CalendarTables GenerateCalendarTables(string home)
    {
        var pin = ReadCalendarPin(home);
        return CalendarTableGenerator.Generate(pin, pin.Extract(Crates(home, pin)));
    }

    /// <summary>Every file of the generated area, as the generator writes it from the home's pins, by file name.</summary>
    internal static IReadOnlyDictionary<string, byte[]> Generate(string home) => Files(GenerateTzdbTables(home), GenerateCalendarTables(home));

    /// <summary>Every file of the generated area for these tables: the four tables and their manifest, by file name.</summary>
    internal static IReadOnlyDictionary<string, byte[]> Files(TzdbTables tzdb, CalendarTables calendars)
    {
        var tables = new SortedDictionary<string, byte[]>(StringComparer.Ordinal)
        {
            [TimeZonesFile] = tzdb.TimeZones.ToArray(),
            [TimeZoneIdsFile] = tzdb.TimeZoneIds.ToArray(),
            [TimeZoneRegionsFile] = tzdb.TimeZoneRegions.ToArray(),
            [CalendarsFile] = calendars.Calendars.ToArray(),
        };

        var manifest = new StringBuilder();
        manifest.Append("# broiler-js time data tables 1\n");
        manifest.Append("#\n");
        manifest.Append("# GENERATED - DO NOT EDIT. Broiler.ECMAScript.Generation writes this file and the tables beside it\n");
        manifest.Append("# from the pins in ../tzdb/, ../cldr/ and ../calendars/. Rule SL7 regenerates every file of this\n");
        manifest.Append("# directory and compares it byte for byte; run the architecture tests with\n");
        manifest.Append("# " + WriteVariable + "=1 to write them instead. No package carries these files (decision\n");
        manifest.Append("# SJL-D-6 (c)): each engine embeds them. A `table` line is `table <file> <bytes> <sha256>`.\n");
        manifest.Append('\n');
        manifest.Append("tzdb-release ").Append(TimeDataVersions.TzdbRelease).Append('\n');
        manifest.Append("cldr-version ").Append(TimeDataVersions.CldrVersion).Append('\n');
        manifest.Append("icu_calendar ").Append(TimeDataVersions.IcuCalendarVersion).Append('\n');
        manifest.Append("calendrical_calculations ").Append(TimeDataVersions.CalendricalCalculationsVersion).Append('\n');
        manifest.Append('\n');

        foreach (var (name, bytes) in tables)
        {
            manifest.Append("table ").Append(name).Append(' ')
                .Append(bytes.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append(' ')
                .Append(Convert.ToHexStringLower(SHA256.HashData(bytes))).Append('\n');
        }

        var files = new SortedDictionary<string, byte[]>(tables, StringComparer.Ordinal)
        {
            [ManifestFile] = Encoding.ASCII.GetBytes(manifest.ToString()),
        };
        return files;
    }

    /// <summary>Every file committed in the generated area, by file name.</summary>
    internal static IReadOnlyDictionary<string, byte[]> Committed(string home) =>
        Directory.Exists(Generated(home))
            ? Directory.EnumerateFiles(Generated(home)).ToDictionary(static path => Path.GetFileName(path), File.ReadAllBytes, StringComparer.Ordinal)
            : new Dictionary<string, byte[]>(StringComparer.Ordinal);

    /// <summary>Writes the generated area: every file the generator writes, and no other file left beside them.</summary>
    internal static void Write(string home, IReadOnlyDictionary<string, byte[]> files)
    {
        Directory.CreateDirectory(Generated(home));
        foreach (var stale in Directory.EnumerateFiles(Generated(home)).Where(path => !files.ContainsKey(Path.GetFileName(path))))
            File.Delete(stale);
        foreach (var (name, bytes) in files)
            File.WriteAllBytes(Path.Combine(Generated(home), name), bytes);
    }
}
