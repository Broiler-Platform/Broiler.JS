using System;
using System.IO;
using System.Linq;
using Broiler.ECMAScript.Time;

namespace Broiler.ECMAScript.Algorithms.Tests;

/// <summary>
/// The one place the time-zone and calendar tests take their table bytes from (milestone SJL-8.3).
/// </summary>
/// <remarks>
/// <para>
/// The bytes are the data home's generated tables, <c>Broiler.ECMAScript/data/time/generated/</c>, read from
/// disk as committed: the tables an engine embeds and hands to the decoders (decision SJL-D-6 (c)). Rule SL7
/// regenerates them from the home's pins byte for byte, and the generator's tests hold each one to the data
/// Broiler.VM's <c>JsTzdbTables.g.cs</c> and <c>JsCalendarTables.g.cs</c> hold at its commit <c>9508bd5</c>.
/// </para>
/// <para>
/// The tables' SHA-256 (tzdb 2026e; <c>icu_calendar</c> 2.3.0 and <c>calendrical_calculations</c> 0.2.4), which
/// the tests check:
/// </para>
/// <list type="bullet">
/// <item><c>TimeZones</c>, 78,428 bytes: <c>f0993da914d6ffed2a4f3f3a4a3016af4d4e33ec213cf138656b43f0f0892853</c></item>
/// <item><c>TimeZoneIds</c>, 14,056 bytes: <c>c68c5d8e9a5677f071017efbdd0bb1be3e8d58042022c05908f2fcf42c29f296</c></item>
/// <item><c>TimeZoneRegions</c>, 7,627 bytes: <c>8cf869e9f19f00117a1cbb9899cf8130043688684d98996c3a56759e4d1f5045</c></item>
/// <item><c>Calendars</c>, 1,958 bytes: <c>dc368c81f83865b20a48bee7a6efc9458e07c8c0d04a27b7d048e7d80793ad47</c></item>
/// </list>
/// </remarks>
internal static class TimeTablesFixture
{
    /// <summary>The tzdb release of the zone tables, as the tables' manifest names it.</summary>
    internal static readonly string Release = ManifestValue("tzdb-release");

    /// <summary>The SHA-256 of each table, in the order of the members below, as recorded above.</summary>
    internal static readonly (string Table, int Length, string Sha256)[] Digests =
    [
        ("TimeZones", 78428, "f0993da914d6ffed2a4f3f3a4a3016af4d4e33ec213cf138656b43f0f0892853"),
        ("TimeZoneIds", 14056, "c68c5d8e9a5677f071017efbdd0bb1be3e8d58042022c05908f2fcf42c29f296"),
        ("TimeZoneRegions", 7627, "8cf869e9f19f00117a1cbb9899cf8130043688684d98996c3a56759e4d1f5045"),
        ("Calendars", 1958, "dc368c81f83865b20a48bee7a6efc9458e07c8c0d04a27b7d048e7d80793ad47"),
    ];

    private static readonly string Generated = FindGenerated(AppContext.BaseDirectory);

    private static readonly byte[] TimeZonesBytes = File.ReadAllBytes(Path.Combine(Generated, "time-zones.bin"));

    private static readonly byte[] TimeZoneIdsBytes = File.ReadAllBytes(Path.Combine(Generated, "time-zone-ids.txt"));

    private static readonly byte[] TimeZoneRegionsBytes = File.ReadAllBytes(Path.Combine(Generated, "time-zone-regions.txt"));

    private static readonly byte[] CalendarsBytes = File.ReadAllBytes(Path.Combine(Generated, "calendars.bin"));

    private static readonly Lazy<TimeZoneDatabase> Zones = new(() => new TimeZoneDatabase(Release, TimeZones, TimeZoneIds, TimeZoneRegions));

    private static readonly Lazy<CalendarTables> CalendarYears = new(() => new CalendarTables(Calendars));

    /// <summary>The binary zone table.</summary>
    internal static ReadOnlySpan<byte> TimeZones => TimeZonesBytes;

    /// <summary>The identifier table.</summary>
    internal static ReadOnlySpan<byte> TimeZoneIds => TimeZoneIdsBytes;

    /// <summary>The region table.</summary>
    internal static ReadOnlySpan<byte> TimeZoneRegions => TimeZoneRegionsBytes;

    /// <summary>The calendar table.</summary>
    internal static ReadOnlySpan<byte> Calendars => CalendarsBytes;

    /// <summary>One decoded database over the tables, shared by the tests.</summary>
    internal static TimeZoneDatabase Database => Zones.Value;

    /// <summary>One decoded set of calendar tables, shared by the tests.</summary>
    internal static CalendarTables CalendarTables => CalendarYears.Value;

    /// <summary>A table's bytes by its name in <see cref="Digests"/>.</summary>
    internal static byte[] Table(string name) => name switch
    {
        "TimeZones" => TimeZones.ToArray(),
        "TimeZoneIds" => TimeZoneIds.ToArray(),
        "TimeZoneRegions" => TimeZoneRegions.ToArray(),
        "Calendars" => Calendars.ToArray(),
        _ => throw new ArgumentOutOfRangeException(nameof(name), name, "No such table."),
    };

    /// <summary>The home's generated area, found from <paramref name="start"/> upwards.</summary>
    private static string FindGenerated(string start)
    {
        for (var directory = new DirectoryInfo(start); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "Broiler.ECMAScript", "data", "time", "generated");
            if (File.Exists(Path.Combine(candidate, "tables.manifest")))
                return candidate;
        }

        throw new DirectoryNotFoundException("no Broiler.ECMAScript/data/time/generated/tables.manifest above " + start);
    }

    /// <summary>A <c>&lt;key&gt; &lt;value&gt;</c> line of the tables' manifest.</summary>
    private static string ManifestValue(string key) =>
        File.ReadAllLines(Path.Combine(FindGenerated(AppContext.BaseDirectory), "tables.manifest"))
            .Select(static line => line.Split(' ', 2))
            .Where(fields => fields.Length == 2 && fields[0] == key)
            .Select(static fields => fields[1])
            .Single();
}
