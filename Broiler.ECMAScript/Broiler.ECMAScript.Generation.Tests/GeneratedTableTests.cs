using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Broiler.ECMAScript.Generation.Time;
using Broiler.ECMAScript.Runners;
using Xunit;

namespace Broiler.ECMAScript.Generation.Tests;

/// <summary>
/// The tables the generator writes from the home's pins, by digest: a change to any of them is a change
/// to the data both engines embed, and is seen here before anywhere else.
/// </summary>
public sealed class GeneratedTableTests
{
    /// <summary>
    /// Each generated table's length and SHA-256. <b>These digests are those of the data Broiler.VM's
    /// generated files hold at Broiler.VM <c>9508bd5</c></b>: the bytes of <c>JsTzdbTables.TimeZones</c>,
    /// <c>TimeZoneIds</c> and <c>TimeZoneRegions</c> in <c>src/Broiler.VM.Profile.JavaScript.Intl/JsTzdbTables.g.cs</c>,
    /// and of <c>JsCalendarTables.Calendars</c> in <c>JsCalendarTables.g.cs</c> beside it, each span decoded
    /// from its C# literal, as milestone SJL-8.2 compared them. The home's generator writes Broiler.VM's
    /// data byte for byte; a digest that moves here moves both engines' data, and needs the dated decision
    /// that moves both repositories' pins.
    /// </summary>
    private static readonly (string File, int Bytes, string Sha256)[] Digests =
    [
        (TimeDataHome.CalendarsFile, 1958, "dc368c81f83865b20a48bee7a6efc9458e07c8c0d04a27b7d048e7d80793ad47"),
        (TimeDataHome.TimeZoneIdsFile, 14056, "c68c5d8e9a5677f071017efbdd0bb1be3e8d58042022c05908f2fcf42c29f296"),
        (TimeDataHome.TimeZoneRegionsFile, 7627, "8cf869e9f19f00117a1cbb9899cf8130043688684d98996c3a56759e4d1f5045"),
        (TimeDataHome.TimeZonesFile, 78428, "f0993da914d6ffed2a4f3f3a4a3016af4d4e33ec213cf138656b43f0f0892853"),
    ];

    private static readonly Lazy<IReadOnlyDictionary<string, byte[]>> Generated = new(static () => TimeDataHome.Generate(TimeDataHome.Current));

    private static string Sha256(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    [Fact]
    public void Every_generated_table_has_the_digest_of_Broiler_VM_s_data()
    {
        Assert.Equal(Digests.Select(static d => d.File).Append(TimeDataHome.ManifestFile).Order(StringComparer.Ordinal), Generated.Value.Keys);

        foreach (var (file, bytes, sha256) in Digests)
        {
            Assert.Equal(bytes, Generated.Value[file].Length);
            Assert.Equal(sha256, Sha256(Generated.Value[file]));
        }
    }

    [Fact]
    public void The_manifest_states_the_versions_and_every_table_s_length_and_digest()
    {
        var manifest = Encoding.ASCII.GetString(Generated.Value[TimeDataHome.ManifestFile]).Split('\n');

        Assert.Contains("tzdb-release 2026e", manifest);
        Assert.Contains("cldr-version 48.2.0", manifest);
        Assert.Contains("icu_calendar 2.3.0", manifest);
        Assert.Contains("calendrical_calculations 0.2.4", manifest);
        Assert.Equal(
            Digests.Select(static d => "table " + d.File + " " + d.Bytes.ToString(System.Globalization.CultureInfo.InvariantCulture) + " " + d.Sha256),
            manifest.Where(static line => line.StartsWith("table ", StringComparison.Ordinal)));
        Assert.DoesNotContain('\r', Encoding.ASCII.GetString(Generated.Value[TimeDataHome.ManifestFile]));
    }

    [Fact]
    public void Two_generations_write_the_same_bytes()
    {
        var again = TimeDataHome.Generate(TimeDataHome.Current);
        Assert.Equal(Generated.Value.Keys, again.Keys);
        foreach (var (file, bytes) in Generated.Value)
            Assert.True(bytes.AsSpan().SequenceEqual(again[file]), file + " differs between two generations");
    }

    /// <summary>
    /// The identifier table's policy (decision SJL-D-20): 597 names, CLDR's primaries, the UTC family as
    /// <c>UTC</c>, no <c>Factory</c>, and every line's zone index inside the zone table.
    /// </summary>
    [Fact]
    public void The_identifier_table_carries_CLDR_s_primaries_and_no_Factory()
    {
        var lines = Encoding.ASCII.GetString(Generated.Value[TimeDataHome.TimeZoneIdsFile]).Split('\n');
        var rows = lines.Select(static line => line.Split('|')).ToDictionary(static fields => fields[0], StringComparer.Ordinal);

        Assert.Equal(597, lines.Length);
        Assert.All(rows.Values, static fields => Assert.Equal(3, fields.Length));
        var names = lines.Select(static line => line.Split('|')[0]).ToList();
        Assert.Equal(names.Order(StringComparer.Ordinal), names);
        Assert.False(rows.ContainsKey("Factory"));

        Assert.Equal("America/Chicago", rows["CST6CDT"][2]);
        Assert.Equal("America/New_York", rows["EST5EDT"][2]);
        Assert.Equal("Asia/Kolkata", rows["Asia/Calcutta"][2]);
        Assert.Equal(string.Empty, rows["Asia/Kolkata"][2]);

        // A Link CLDR names as its own key's primary stays primary, and reads its tzdb target's zone.
        Assert.Equal(string.Empty, rows["Europe/Bratislava"][2]);
        Assert.Equal(rows["Europe/Prague"][1], rows["Europe/Bratislava"][1]);
        Assert.Equal(string.Empty, rows["UTC"][2]);
        foreach (var utc in new[] { "Etc/UTC", "Etc/GMT", "GMT", "Etc/Universal", "Etc/Zulu" })
            Assert.Equal("UTC", rows[utc][2]);

        // A Link reads its target's zone; the primary's own line has an empty primary field.
        Assert.Equal(rows["Asia/Kolkata"][1], rows["Asia/Calcutta"][1]);
        Assert.All(rows.Values.Where(static fields => fields[2].Length > 0), fields => Assert.Equal(string.Empty, rows[fields[2]][2]));
    }

    [Fact]
    public void The_region_table_is_zone_tab_s_zones_of_each_country()
    {
        var lines = Encoding.ASCII.GetString(Generated.Value[TimeDataHome.TimeZoneRegionsFile]).Split('\n');
        var regions = lines.Select(static line => line.Split('|')).ToDictionary(static fields => fields[0], static fields => fields[1..], StringComparer.Ordinal);

        var codes = lines.Select(static line => line.Split('|')[0]).ToList();
        Assert.Equal(codes.Order(StringComparer.Ordinal), codes);
        Assert.Equal(["Europe/Berlin", "Europe/Busingen"], regions["DE"]);
        Assert.Contains("America/New_York", regions["US"]);
        Assert.All(regions.Values, static zones => Assert.Equal(zones.Order(StringComparer.Ordinal), zones));
    }
}
