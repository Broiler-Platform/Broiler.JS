using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Broiler.ECMAScript.Generation.Time;
using Broiler.ECMAScript.Runners;
using Xunit;

namespace Broiler.ECMAScript.Generation.Tests;

/// <summary>
/// The compiler read back against histories the release's own commentary states, as Broiler.VM's rule N30
/// reads them: a table nobody reads is only as right as the compiler that wrote it.
/// </summary>
public sealed class TzdbCompilerTests
{
    private static readonly Lazy<IReadOnlyDictionary<string, TzdbZone>> Zones = new(static () =>
    {
        var pin = TimeDataHome.ReadTzdbPin(TimeDataHome.Current);
        var source = TzdbCompiler.Parse(pin.Extract(TimeDataHome.Tarball(TimeDataHome.Current, pin)));
        return TzdbTableGenerator.Zones(source).ToDictionary(static zone => zone.Name, static zone => zone.Zone, StringComparer.Ordinal);
    });

    private static long Utc(int year, int month, int day, int hour, int minute = 0) =>
        (TzdbCompiler.DaysFromCivil(year, month, day) * 86400) + (hour * 3600) + (minute * 60);

    /// <summary>
    /// New York's daylight saving time in 2026 and its rule after the listed years, Kolkata's half hour,
    /// Lord Howe's half-hour daylight saving, Kiritimati's skipped day, and the local mean time before
    /// standard time.
    /// </summary>
    [Fact]
    public void The_compiler_reads_known_histories()
    {
        var zones = Zones.Value;
        var newYork = zones["America/New_York"];

        Assert.Equal(-5 * 3600, TzdbCompiler.OffsetAt(newYork, Utc(2026, 3, 8, 6, 59)));
        Assert.Equal(-4 * 3600, TzdbCompiler.OffsetAt(newYork, Utc(2026, 3, 8, 7)));
        Assert.Equal(-4 * 3600, TzdbCompiler.OffsetAt(newYork, Utc(2026, 11, 1, 5, 59)));
        Assert.Equal(-5 * 3600, TzdbCompiler.OffsetAt(newYork, Utc(2026, 11, 1, 6)));
        Assert.Equal(-4 * 3600, TzdbCompiler.OffsetAt(newYork, Utc(2400, 7, 1, 0)));
        Assert.Equal(-4 * 3600, TzdbCompiler.OffsetAt(newYork, Utc(2040, 7, 1, 0)));
        Assert.Equal(-((4 * 3600) + (56 * 60) + 2), newYork.InitialOffset);
        Assert.NotNull(newYork.Final);

        Assert.Equal((5 * 3600) + 1800, TzdbCompiler.OffsetAt(zones["Asia/Kolkata"], Utc(2026, 1, 1, 0)));
        Assert.Equal(11 * 3600, TzdbCompiler.OffsetAt(zones["Australia/Lord_Howe"], Utc(2026, 1, 1, 0)));
        Assert.Equal((10 * 3600) + 1800, TzdbCompiler.OffsetAt(zones["Australia/Lord_Howe"], Utc(2026, 7, 1, 0)));
        Assert.Equal(-10 * 3600, TzdbCompiler.OffsetAt(zones["Pacific/Kiritimati"], Utc(1994, 12, 30, 0)));
        Assert.Equal(14 * 3600, TzdbCompiler.OffsetAt(zones["Pacific/Kiritimati"], Utc(1995, 1, 2, 0)));
        Assert.Equal(0, TzdbCompiler.OffsetAt(zones["Etc/UTC"], Utc(2026, 1, 1, 0)));
        Assert.Null(zones["Asia/Kolkata"].Final);

        // Extrapolation without limit (SJL-D-20): southern zones keep their seasons after 2037.
        Assert.Equal(-4 * 3600, TzdbCompiler.OffsetAt(zones["America/Santiago"], Utc(2040, 7, 1, 0)));
        Assert.Equal(10 * 3600, TzdbCompiler.OffsetAt(zones["Australia/Sydney"], Utc(2040, 7, 1, 0)));
        Assert.Equal(2 * 3600, TzdbCompiler.OffsetAt(zones["Europe/Berlin"], Utc(2040, 7, 1, 0)));
    }

    /// <summary>
    /// The daylight flag the tables keep for CLDR's names (Broiler.VM's JSD-0058) is the rearguard one:
    /// Ireland's and Morocco's summers are daylight time though their winters save a negative amount,
    /// Namibia's summer is until its negative saves end in 2017, and a zone that moves to standard time at
    /// its daylight offset (America/Chihuahua in 2022, Europe/Istanbul in 2016) keeps that change.
    /// </summary>
    [Fact]
    public void The_daylight_flag_is_the_rearguard_one()
    {
        var zones = Zones.Value;

        bool Daylight(string zone, int year, int month)
        {
            var at = (TzdbCompiler.DaysFromCivil(year, month, 15) * 86400) + (12 * 3600);
            var listed = zones[zone].Transitions.Where(transition => transition.At <= at).ToList();
            return listed.Count > 0 && listed[^1].Daylight;
        }

        Assert.True(Daylight("Europe/Dublin", 1990, 7));
        Assert.False(Daylight("Europe/Dublin", 1990, 1));
        Assert.True(Daylight("Africa/Windhoek", 2010, 1));
        Assert.False(Daylight("Africa/Windhoek", 2010, 7));
        Assert.False(Daylight("Africa/Windhoek", 2020, 1));
        Assert.True(Daylight("Africa/Casablanca", 2024, 1));
        Assert.False(Daylight("Africa/Casablanca", 1995, 7));
        Assert.True(Daylight("America/Chihuahua", 2022, 7));
        Assert.False(Daylight("America/Chihuahua", 2023, 7));
        Assert.False(Daylight("Europe/Istanbul", 2024, 7));
        Assert.True(Daylight("Europe/London", 1990, 7));
    }

    [Fact]
    public void The_default_build_has_the_zones_and_links_of_its_nine_files()
    {
        var pin = TimeDataHome.ReadTzdbPin(TimeDataHome.Current);
        var source = TzdbCompiler.Parse(pin.Extract(TimeDataHome.Tarball(TimeDataHome.Current, pin)));

        Assert.Equal(["africa", "antarctica", "asia", "australasia", "europe", "northamerica", "southamerica", "etcetera", "backward"], TzdbCompiler.SourceFiles);
        Assert.Equal(597, source.Zones.Count + source.Links.Count);
        Assert.False(source.Zones.ContainsKey("Factory"));
        Assert.Equal("America/Chicago", source.Links.GetValueOrDefault("US/Central"));
        Assert.True(source.Zones.ContainsKey("CST6CDT"), "2026e's backward file states CST6CDT as a Zone of its own");
    }

    [Fact]
    public void The_field_readers_read_tzdb_s_forms()
    {
        Assert.Equal((2 * 3600, 's'), TzdbCompiler.Time("2:00s"));
        Assert.Equal((-((4 * 3600) + (56 * 60) + 2), 'w'), TzdbCompiler.Time("-4:56:02"));
        Assert.Equal((3600, 'u'), TzdbCompiler.Time("1:00u"));
        Assert.Equal((0, 'u'), TzdbCompiler.Time("0z"));
        Assert.Equal((0, 'w'), TzdbCompiler.Time("-"));
        Assert.Equal(3, TzdbCompiler.Month("Mar"));
        Assert.Equal(3, TzdbCompiler.Month("MARCH"));
        Assert.Equal(6, TzdbCompiler.Weekday("Sun"));
        Assert.Throws<InvalidDataException>(() => TzdbCompiler.Month("Foo"));
        Assert.Throws<InvalidDataException>(() => TzdbCompiler.Weekday("Xyz"));

        // ON fields, 2026: the second Sunday of March, the last Sunday of October, the Sunday on or before the 25th.
        Assert.Equal(TzdbCompiler.DaysFromCivil(2026, 3, 8), TzdbCompiler.OnDay(2026, 3, "Sun>=8"));
        Assert.Equal(TzdbCompiler.DaysFromCivil(2026, 10, 25), TzdbCompiler.OnDay(2026, 10, "lastSun"));
        Assert.Equal(TzdbCompiler.DaysFromCivil(2026, 10, 25), TzdbCompiler.OnDay(2026, 10, "Sun<=25"));
        Assert.Equal((2, 6, 8), TzdbTableGenerator.OnField("Sun>=8"));
        Assert.Equal((1, 6, 0), TzdbTableGenerator.OnField("lastSun"));
        Assert.Equal((0, 0, 15), TzdbTableGenerator.OnField("15"));
        Assert.Equal(0, TzdbCompiler.DaysFromCivil(1970, 1, 1));
    }
}
