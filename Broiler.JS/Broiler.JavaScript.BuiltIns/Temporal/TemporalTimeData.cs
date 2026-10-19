using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Broiler.ECMAScript.Time;

namespace Broiler.JavaScript.BuiltIns.Temporal;

/// <summary>
/// This engine's provider of the time data home's tables (milestone SJL-8.6, decision SJL-D-6 (c)): the IANA
/// time zone tables and the calendar tables that <c>Broiler.ECMAScript.Generation</c> writes into
/// <c>Broiler.ECMAScript/data/time/generated/</c>, embedded in this assembly from there, handed to the shared
/// decoders <see cref="TimeZoneDatabase"/> and <see cref="CalendarTables"/> once, on first use.
/// </summary>
/// <remarks>
/// The embedded files are linked from the home, not copied, so they are the home's bytes; the tables' release
/// is the manifest's <c>tzdb-release</c> line. Every member is safe to call concurrently.
/// </remarks>
internal static class TemporalTimeData
{
    // The embedded files' logical names: this prefix and the home's file name.
    internal const string ResourcePrefix = "Broiler.JavaScript.BuiltIns.TimeData.";

    private static readonly Lazy<TimeZoneDatabase> TimeZoneTables =
        new(LoadTimeZones, LazyThreadSafetyMode.ExecutionAndPublication);

    private static readonly Lazy<CalendarTables> CalendarYearTables =
        new(() => new CalendarTables(Read("calendars.bin")), LazyThreadSafetyMode.ExecutionAndPublication);

    private static readonly Lazy<string[]> IdentifierList =
        new(LoadIdentifiers, LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>The time zone database over the embedded tables: lookup, primaries, regions and each zone's rules.</summary>
    internal static ITimeZoneDatabase TimeZones => TimeZoneTables.Value;

    /// <summary>The calendar tables: the Chinese, Korean and Qing years, the Umm al-Qura years and the Persian corrections.</summary>
    internal static ICalendarTables Calendars => CalendarYearTables.Value;

    /// <summary>
    /// Every available named time zone identifier, primary or not, in the table's order: the names the formatter
    /// tries when it looks for a zone's display name under another identifier of the same primary.
    /// </summary>
    internal static IReadOnlyList<string> Identifiers => IdentifierList.Value;

    /// <summary>An embedded file's bytes, by its name in the home's <c>generated/</c> directory.</summary>
    internal static byte[] Read(string file)
    {
        using var stream = typeof(TemporalTimeData).Assembly.GetManifestResourceStream(ResourcePrefix + file)
            ?? throw new InvalidOperationException($"The embedded time data file {file} is missing.");
        using var copy = new MemoryStream();
        stream.CopyTo(copy);
        return copy.ToArray();
    }

    /// <summary>A <c>&lt;key&gt; &lt;value&gt;</c> line of the embedded manifest, for example <c>tzdb-release</c>.</summary>
    internal static string ManifestValue(string key)
    {
        foreach (var line in Encoding.ASCII.GetString(Read("tables.manifest")).Split('\n'))
        {
            var space = line.IndexOf(' ');
            if (space > 0 && !line.StartsWith('#') && line.AsSpan(0, space).SequenceEqual(key))
                return line[(space + 1)..].Trim();
        }

        throw new InvalidOperationException($"The embedded time data manifest has no {key} line.");
    }

    private static TimeZoneDatabase LoadTimeZones() =>
        new(ManifestValue("tzdb-release"), Read("time-zones.bin"), Read("time-zone-ids.txt"), Read("time-zone-regions.txt"));

    // The identifier table's first field, `identifier|zone|primary`, of each line; the decoder reads the rest.
    private static string[] LoadIdentifiers()
    {
        var names = new List<string>();
        foreach (var line in Encoding.ASCII.GetString(Read("time-zone-ids.txt")).Split('\n'))
        {
            var bar = line.IndexOf('|');
            if (bar > 0)
                names.Add(line[..bar]);
        }

        return names.ToArray();
    }
}
