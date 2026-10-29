using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;

namespace Broiler.JavaScript.BuiltIns.Tests;

// The time-zone area on the shared library and the data home's tables (milestone SJL-8.6). Each case names
// the row of SJL-8.1's correspondence from code (Broiler.VM's docs/evidence/sjl-8-001/correspondence.md) that
// it holds: a correction or a policy change failed before the cut-over and passes after it. The suite runs in
// UTC (the TestCultureInitializer); the host-zone cases are in SharedTimeZoneHostTests below, and the calendar
// tables' in SharedCalendarTableTests.
public class SharedTimeZoneTests
{
    private static string Eval(string source) => SharedDateAlgorithmTests.Eval(source);

    private static string Error(string source) => Eval($"var e = 'none'; try {{ {source}; }} catch (x) {{ e = x.name; }} e");

    // ── The embedded tables ───────────────────────────────────────────────────────

    private const string ResourcePrefix = "Broiler.JavaScript.BuiltIns.TimeData.";

    public static TheoryData<string> Tables => new()
    {
        "time-zones.bin", "time-zone-ids.txt", "time-zone-regions.txt", "calendars.bin", "tables.manifest",
    };

    private static Assembly BuiltIns => typeof(RegExp.JSRegExp).Assembly;

    private static byte[] Embedded(string file)
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
            var candidate = Path.Combine(directory.FullName, "Broiler.ECMAScript", "data", "time", "generated");
            if (File.Exists(Path.Combine(candidate, "tables.manifest")))
                return candidate;
        }

        throw new DirectoryNotFoundException("no Broiler.ECMAScript/data/time/generated above " + AppContext.BaseDirectory);
    }

    // The tables are linked from the data home, not copied: each embedded file is the home's, byte for byte.
    [Theory]
    [MemberData(nameof(Tables))]
    public void Each_embedded_table_is_the_data_home_s(string file)
        => Assert.Equal(File.ReadAllBytes(Path.Combine(Generated(), file)), Embedded(file));

    // And each one is what the manifest's `table` line records: its length and SHA-256.
    [Fact]
    public void Each_embedded_table_matches_the_manifest()
    {
        var lines = Encoding.ASCII.GetString(Embedded("tables.manifest")).Split('\n')
            .Where(static line => line.StartsWith("table ", StringComparison.Ordinal))
            .Select(static line => line.Split(' '))
            .ToList();
        Assert.Equal(4, lines.Count);
        foreach (var fields in lines)
        {
            var bytes = Embedded(fields[1]);
            Assert.Equal(long.Parse(fields[2]), bytes.LongLength);
            Assert.Equal(fields[3], Convert.ToHexStringLower(SHA256.HashData(bytes)));
        }
    }

    // The deletion list: no tzdata.bin, and none of the types that read it (the exit gate's clause 7).
    [Fact]
    public void The_retired_time_zone_data_is_gone()
    {
        Assert.Equal(
            Tables.Select(static row => ResourcePrefix + row.Data).OrderBy(static name => name, StringComparer.Ordinal),
            BuiltIns.GetManifestResourceNames().Where(static name => name.Contains("TimeData", StringComparison.Ordinal) || name.Contains("tzdata", StringComparison.Ordinal))
                .OrderBy(static name => name, StringComparer.Ordinal));

        var referenced = TypeNames(BuiltIns);

        foreach (var name in new[] { "IanaTimeZone", "IanaTimeZoneDatabase", "TzifReader" })
            Assert.DoesNotContain(name, referenced);
    }

    // The names of every type an assembly defines or references.
    internal static HashSet<string> TypeNames(Assembly assembly)
    {
        using var file = File.OpenRead(assembly.Location);
        using var pe = new PEReader(file);
        var metadata = pe.GetMetadataReader();
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var handle in metadata.TypeReferences)
            names.Add(metadata.GetString(metadata.GetTypeReference(handle).Name));
        foreach (var handle in metadata.TypeDefinitions)
            names.Add(metadata.GetString(metadata.GetTypeDefinition(handle).Name));
        return names;
    }

    // ── The tz database: policy changes under SJL-D-20 ────────────────────────────

    private static string Offset(string instant, string zone)
        => Eval($"Temporal.Instant.from('{instant}').toZonedDateTimeISO('{zone}').offset");

    // Z1, Z2: tzdb 2026e's changes, which 2025b lacked.
    [Theory]
    [InlineData("2027-01-15T12:00Z", "America/Vancouver", "-07:00")]
    [InlineData("2027-01-15T12:00Z", "America/Edmonton", "-06:00")]
    [InlineData("2027-01-15T12:00Z", "Africa/Casablanca", "+00:00")]
    [InlineData("1925-09-25T12:00Z", "Europe/Dublin", "+00:00")]
    [InlineData("1992-05-02T12:00Z", "America/Bogota", "-04:00")]
    public void The_release_is_2026e(string instant, string zone, string offset)
        => Assert.Equal(offset, Offset(instant, zone));

    // Z3: no backzone; Amsterdam and Reykjavik read Brussels' and Abidjan's histories.
    [Theory]
    [InlineData("1850-01-01T00:00Z", "Europe/Amsterdam", "+00:17:30")]
    [InlineData("1937-07-01T00:00Z", "Europe/Amsterdam", "+01:00")]
    [InlineData("1939-01-15T00:00Z", "Europe/Amsterdam", "+00:00")]
    [InlineData("1900-01-01T00:00Z", "Atlantic/Reykjavik", "-00:16:08")]
    public void Backzone_is_excluded(string instant, string zone, string offset)
        => Assert.Equal(offset, Offset(instant, zone));

    // Z4: the recurring rules continue every zone after 2037, southern zones too.
    [Theory]
    [InlineData("2040-07-01T12:00Z", "America/New_York", "-04:00")]
    [InlineData("2040-07-01T12:00Z", "Europe/Berlin", "+02:00")]
    [InlineData("2400-07-01T12:00Z", "America/New_York", "-04:00")]
    [InlineData("2040-07-01T12:00Z", "America/Santiago", "-04:00")]
    [InlineData("2040-07-01T12:00Z", "Australia/Sydney", "+10:00")]
    public void Offsets_continue_after_2037(string instant, string zone, string offset)
        => Assert.Equal(offset, Offset(instant, zone));

    // A5, A7, A10: gaps, starts of day and transitions after 2037.
    [Theory]
    [InlineData("Temporal.ZonedDateTime.from('2040-03-11T02:30[America/New_York]').toString()", "2040-03-11T03:30:00-04:00[America/New_York]")]
    [InlineData("Temporal.ZonedDateTime.from('2040-03-11T12:00[America/New_York]').hoursInDay", "23")]
    [InlineData("Temporal.ZonedDateTime.from('2040-09-02T12:00[America/Santiago]').startOfDay().toString()", "2040-09-02T01:00:00-03:00[America/Santiago]")]
    [InlineData("Temporal.ZonedDateTime.from('2037-12-01T00:00[America/New_York]').getTimeZoneTransition('next').toString()", "2038-03-14T03:00:00-04:00[America/New_York]")]
    [InlineData("Temporal.ZonedDateTime.from('2100-01-01T00:00[America/New_York]').getTimeZoneTransition('previous').toString()", "2099-11-01T01:00:00-05:00[America/New_York]")]
    public void Zone_algorithms_continue_after_2037(string expression, string expected)
        => Assert.Equal(expected, Eval(expression));

    // The shared GetEpochNanosecondsFor refuses a wall time whose instant is outside the range, so a rounding that
    // requests nothing builds no next-day boundary past the limit (DifferenceZonedDateTimeWithRounding's early
    // return); one that rounds still needs it (test262 round/relativeto-date-limits and next-day-out-of-range).
    [Theory]
    [InlineData("new Temporal.Duration(0).round({ largestUnit: 'years', relativeTo: '+275760-09-12T23:59:60+00:00[UTC]' }).toString()", "PT0S")]
    [InlineData("new Temporal.Duration(0).round({ largestUnit: 'years', relativeTo: { year: 275760, month: 9, day: 12, hour: 23, minute: 59, second: 60, timeZone: 'UTC' } }).toString()", "PT0S")]
    [InlineData("var e = 'none'; try { new Temporal.Duration(0).round({ largestUnit: 'days', smallestUnit: 'hours', relativeTo: '+275760-09-12T23:59:59+00:00[UTC]' }); } catch (x) { e = x.name; } e", "RangeError")]
    public void Rounding_at_the_instant_limits_builds_no_unneeded_boundary(string expression, string expected)
        => Assert.Equal(expected, Eval(expression));

    // Z5: no Factory.
    [Fact]
    public void Factory_is_not_a_zone()
    {
        Assert.Equal("RangeError", Error("new Temporal.ZonedDateTime(0n, 'Factory')"));
        Assert.Equal("RangeError", Error("new Intl.DateTimeFormat('en', { timeZone: 'Factory' })"));
        Assert.Equal("false", Eval("Intl.supportedValuesOf('timeZone').includes('Factory')"));
    }

    // Z7: CLDR's primary identifiers.
    [Theory]
    [InlineData("Intl.supportedValuesOf('timeZone').length", "445")]
    [InlineData("Intl.supportedValuesOf('timeZone').includes('Europe/Bratislava')", "true")]
    [InlineData("new Temporal.ZonedDateTime(0n, 'Europe/Bratislava').equals(new Temporal.ZonedDateTime(0n, 'Europe/Prague'))", "false")]
    [InlineData("new Temporal.ZonedDateTime(0n, 'Asia/Calcutta').equals(new Temporal.ZonedDateTime(0n, 'Asia/Kolkata'))", "true")]
    [InlineData("new Temporal.ZonedDateTime(0n, 'EST5EDT').equals(new Temporal.ZonedDateTime(0n, 'America/New_York'))", "true")]
    public void Primaries_are_CLDR_s(string expression, string expected)
        => Assert.Equal(expected, Eval(expression));

    // ── Corrections under SJL-D-16 (a) ────────────────────────────────────────────

    // Z10: a name is matched by its ASCII case only; U+017F no longer folds to S.
    [Theory]
    [InlineData("new Temporal.ZonedDateTime(0n, 'America/Lo\\u017f_Angeles')")]
    [InlineData("new Intl.DateTimeFormat('en', { timeZone: 'America/Lo\\u017f_Angeles' })")]
    [InlineData("Temporal.Instant.from('2020-01-01T00:00Z').toZonedDateTimeISO('America/Lo\\u017f_Angeles')")]
    public void A_name_matches_by_ASCII_case_only(string expression)
        => Assert.Equal("RangeError", Error(expression));

    [Fact]
    public void A_name_still_matches_in_any_ASCII_case()
        => Assert.Equal("Europe/Berlin Europe/Berlin", Eval(
            "new Temporal.ZonedDateTime(0n, 'eUrOpE/bErLiN').timeZoneId + ' ' + new Intl.DateTimeFormat('en', { timeZone: 'EUROPE/BERLIN' }).resolvedOptions().timeZone"));

    // Z11: an ISO date-time string is not the formatter's zone; Temporal still reads one.
    [Theory]
    [InlineData("new Intl.DateTimeFormat('en', { timeZone: '2020-01-01T00:00[Europe/Berlin]' })", "RangeError")]
    [InlineData("new Intl.DateTimeFormat('en', { timeZone: '2020-01-01T00:00Z' })", "RangeError")]
    [InlineData("new Temporal.ZonedDateTime(0n, '2020-01-01T00:00[Europe/Berlin]')", "RangeError")]
    public void An_ISO_string_is_not_the_formatter_s_zone(string expression, string expected)
        => Assert.Equal(expected, Error(expression));

    [Fact]
    public void Temporal_reads_a_zone_from_an_ISO_string()
        => Assert.Equal("Europe/Berlin", Eval(
            "Temporal.Instant.from('2020-01-01T00:00Z').toZonedDateTimeISO('2020-01-01T00:00[Europe/Berlin]').timeZoneId"));

    // Z12: a region's zones are zone.tab's, and none for a region it does not list.
    [Theory]
    [InlineData("US", "29")]
    [InlineData("ZZ", "0")]
    public void A_region_s_zone_count_is_zone_tab_s(string region, string count)
        => Assert.Equal(count, Eval($"new Intl.Locale('en-{region}').getTimeZones().length"));

    [Theory]
    [InlineData("DE", "Europe/Berlin,Europe/Busingen")]
    [InlineData("AT", "Europe/Vienna")]
    [InlineData("UA", "Europe/Kyiv,Europe/Simferopol")]
    [InlineData("CN", "Asia/Shanghai,Asia/Urumqi")]
    public void A_region_s_zones_are_zone_tab_s(string region, string zones)
        => Assert.Equal(zones, Eval($"new Intl.Locale('en-{region}').getTimeZones().join()"));

    // A13: an offset with non-ASCII digits is no offset, a RangeError, not a .NET FormatException.
    [Theory]
    [InlineData("new Temporal.ZonedDateTime(0n, '+\\u0660\\u0665:\\u0663\\u0660')")]
    [InlineData("new Intl.DateTimeFormat('en', { timeZone: '+\\u0660\\u0665:\\u0663\\u0660' })")]
    [InlineData("Temporal.ZonedDateTime.from({ year: 2020, month: 1, day: 1, timeZone: 'Europe/Berlin', offset: '+\\u0660\\u0661:00' })")]
    public void An_offset_with_non_ASCII_digits_is_a_RangeError(string expression)
        => Assert.Equal("RangeError", Error(expression));

    // A6, A11: an unknown name never reads as offset zero; every entry point refuses it.
    [Theory]
    [InlineData("new Temporal.ZonedDateTime(0n, 'Mars/Olympus')")]
    [InlineData("Temporal.Instant.from('2020-01-01T00:00Z').toString({ timeZone: 'Etc/Unknown' })")]
    [InlineData("Temporal.PlainDate.from('2020-01-01').toZonedDateTime('Etc/GMT+13')")]
    [InlineData("new Intl.DateTimeFormat('en', { timeZone: 'America/New York' })")]
    public void An_unknown_name_is_a_RangeError(string expression)
        => Assert.Equal("RangeError", Error(expression));
}

// The host's zone (decision SJL-D-19; rows A15 and A16): Date's local time, Temporal.Now and the default
// Intl.DateTimeFormat zone read the host's zone from the shared database, and a host zone it does not hold
// reads as UTC. Each case sets the process's local zone as SharedDateAlgorithmNewYorkTests does, in the same
// collection, which runs alone, and gives UTC back.
[Collection(nameof(NewYorkZoneCollection))]
public sealed class SharedTimeZoneHostTests : IDisposable
{
    private static string Eval(string source) => SharedDateAlgorithmTests.Eval(source);

    private const string ReadAll =
        "Temporal.Now.timeZoneId() + ' ' + new Intl.DateTimeFormat().resolvedOptions().timeZone + ' ' + " +
        "Temporal.Now.zonedDateTimeISO().timeZoneId + ' ' + new Date(2040, 6, 1, 12).getTimezoneOffset()";

    private static void SetLocal(TimeZoneInfo zone)
    {
        TimeZoneInfo.ClearCachedData();
        var cache = typeof(TimeZoneInfo).GetField("s_cachedData", BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null);
        var local = cache?.GetType().GetField("_localTimeZone", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("TimeZoneInfo's cache of the local zone was not found.");
        lock (cache)
            local.SetValue(cache, zone);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("TZ", "UTC");
        TestCultureInitializer.SetLocalTimeZone(TimeZoneInfo.Utc);
    }

    // A15: Date and Temporal read one zone from one dataset; after 2037 both continue New York's rules.
    [Fact]
    public void Date_and_Temporal_read_the_host_s_zone_from_the_shared_data()
    {
        Environment.SetEnvironmentVariable("TZ", "America/New_York");
        SetLocal(TimeZoneInfo.FindSystemTimeZoneById("America/New_York"));
        Assert.Equal("America/New_York America/New_York America/New_York 240", Eval(ReadAll));
        Assert.Equal("-04:00 240", Eval(
            "Temporal.Instant.from('2040-07-01T16:00Z').toZonedDateTimeISO('America/New_York').offset + ' ' + new Date(Date.UTC(2040, 6, 1, 16)).getTimezoneOffset()"));
        // The offsets are the database's at full second precision: New York's local mean time is -4:56:02.
        Assert.Equal("296.03333333333336", Eval("new Date(1850, 0, 1).getTimezoneOffset()"));
    }

    // A16: a host zone the database does not hold is not returned unvalidated: every reader takes UTC.
    [Fact]
    public void A_host_zone_the_database_does_not_hold_reads_as_UTC()
    {
        SetLocal(TimeZoneInfo.CreateCustomTimeZone("posixrules", TimeSpan.FromHours(-5), "posixrules", "posixrules"));
        Assert.Equal("UTC UTC UTC 0", Eval(ReadAll));
    }
}
