using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Broiler.ECMAScript.Time;
using Xunit;

namespace Broiler.ECMAScript.Algorithms.Tests;

/// <summary>
/// The time-zone database and its rules (milestone SJL-8.3): the decoder of the compact tables over
/// Broiler.VM's tzdb 2026e bytes, identifier lookup under SJL-D-20's case rule, primaries, regions, offsets,
/// the daylight flag and the transitions with no 2037 cliff. The values are the correspondence's
/// (bundle sjl-8-001, rows Z and A), which both engines answered when it was run; the differential check
/// against Broiler.VM's own code is these tests at scale.
/// </summary>
public class TimeZoneTests
{
    private static TimeZoneDatabase Db => TimeTablesFixture.Database;

    internal static long Utc(long year, int month, int day, int hour = 0, int minute = 0, int second = 0) =>
        (IsoCalendarMath.EpochDays(year, month, day) * 86400L) + (hour * 3600L) + (minute * 60L) + second;

    internal static ITimeZoneRules Rules(string identifier)
    {
        Assert.True(Db.TryFind(identifier, out var id), identifier + " is not found");
        return Db.Rules(id);
    }

    private static string[] Identifiers() =>
        Encoding.ASCII.GetString(TimeTablesFixture.TimeZoneIds).Split('\n').Where(static l => l.Length > 0).Select(static l => l.Split('|')[0]).ToArray();

    // ---- the fixture ---------------------------------------------------------------------------------

    [Fact]
    public void The_tables_are_the_ones_whose_digests_the_fixture_records()
    {
        foreach (var (table, length, sha256) in TimeTablesFixture.Digests)
        {
            var bytes = TimeTablesFixture.Table(table);
            Assert.Equal(length, bytes.Length);
            Assert.Equal(sha256, Convert.ToHexStringLower(SHA256.HashData(bytes)));
        }

        Assert.Equal("2026e", TimeTablesFixture.Release);
    }

    // ---- the database --------------------------------------------------------------------------------

    [Fact]
    public void The_release_is_the_one_handed_in_and_an_empty_one_is_refused()
    {
        Assert.Equal("2026e", Db.Release);
        Assert.Throws<ArgumentException>(static () => new TimeZoneDatabase(string.Empty, TimeTablesFixture.TimeZones, TimeTablesFixture.TimeZoneIds, TimeTablesFixture.TimeZoneRegions));
        Assert.ThrowsAny<ArgumentException>(static () => new TimeZoneDatabase(null!, TimeTablesFixture.TimeZones, TimeTablesFixture.TimeZoneIds, TimeTablesFixture.TimeZoneRegions));
    }

    [Fact]
    public void Every_identifier_is_found_in_its_own_case_and_in_every_ASCII_case()
    {
        var identifiers = Identifiers();
        Assert.Equal(597, identifiers.Length);
        foreach (var identifier in identifiers)
        {
            foreach (var form in new[] { identifier, identifier.ToUpperInvariant(), identifier.ToLowerInvariant(), Swap(identifier) })
            {
                Assert.True(Db.TryFind(form, out var id), form);
                Assert.Equal(identifier, id.Identifier);
            }
        }

        static string Swap(string text) => new(text.Select(static c => char.IsAsciiLetterUpper(c) ? char.ToLowerInvariant(c) : char.ToUpperInvariant(c)).ToArray());
    }

    [Theory]
    [InlineData("europe/berlin", "Europe/Berlin")]
    [InlineData("EUROPE/BERLIN", "Europe/Berlin")]
    [InlineData("eUrOpE/bErLiN", "Europe/Berlin")]
    [InlineData("utc", "UTC")]
    [InlineData("etc/utc", "Etc/UTC")]
    [InlineData("etc/gmt+5", "Etc/GMT+5")]
    [InlineData("AMERICA/ARGENTINA/BUENOS_AIRES", "America/Argentina/Buenos_Aires")]
    [InlineData("america/port-au-prince", "America/Port-au-Prince")]
    public void A_mixed_case_name_is_found_in_the_databases_case(string name, string expected)
    {
        Assert.True(Db.TryFind(name, out var id));
        Assert.Equal(expected, id.Identifier);
    }

    [Theory]
    [InlineData("America/Loſ_Angeles")] // U+017F, which invariant upper-casing maps to S (Z10)
    [InlineData("Europe/Berlın")] // U+0131
    [InlineData("Europe/Kıev")]
    [InlineData("Europe/İstanbul")] // U+0130
    [InlineData("Europe/Berlin ")]
    [InlineData("Europe/Berlin\u0000")]
    [InlineData(" Europe/Berlin")]
    [InlineData("Mars/Olympus")]
    [InlineData("Etc/Unknown")]
    [InlineData("")]
    [InlineData("America/New York")]
    [InlineData("EST5")]
    [InlineData("Etc/GMT+13")]
    [InlineData("Etc/GMT-15")]
    [InlineData("Etc/GMT+05")]
    [InlineData("Factory")]
    [InlineData("factory")]
    [InlineData("+05:30")]
    [InlineData("America/")]
    [InlineData("America/New_York/")]
    public void An_unknown_name_or_one_outside_ASCII_is_a_false_from_the_lookup(string name)
    {
        Assert.False(Db.TryFind(name, out var id));
        Assert.Equal(default, id);
    }

    [Theory]
    [InlineData("Asia/Calcutta", "Asia/Kolkata")]
    [InlineData("Europe/Kiev", "Europe/Kyiv")]
    [InlineData("US/Eastern", "America/New_York")]
    [InlineData("EST5EDT", "America/New_York")]
    [InlineData("CET", "Europe/Brussels")]
    [InlineData("Etc/UTC", "UTC")]
    [InlineData("Etc/GMT", "UTC")]
    [InlineData("GMT", "UTC")]
    [InlineData("Etc/GMT0", "UTC")]
    [InlineData("Zulu", "UTC")]
    [InlineData("UTC", "UTC")]
    [InlineData("Europe/Bratislava", "Europe/Bratislava")] // a CLDR key that tzdb links away (Z7)
    [InlineData("Arctic/Longyearbyen", "Arctic/Longyearbyen")]
    [InlineData("America/New_York", "America/New_York")]
    public void An_identifier_resolves_to_CLDRs_primary(string identifier, string primary)
    {
        Assert.True(Db.TryFind(identifier, out var id));
        var resolved = Db.Primary(id);
        Assert.Equal(primary, resolved.Identifier);
        Assert.Equal(resolved, Db.Primary(resolved));
    }

    [Fact]
    public void The_primary_identifiers_are_the_445_that_are_their_own_primary_in_ordinal_order()
    {
        var primaries = Db.PrimaryIdentifiers;
        Assert.Equal(445, primaries.Count);
        Assert.Equal(primaries.Order(StringComparer.Ordinal), primaries);
        Assert.Contains("UTC", primaries);
        Assert.DoesNotContain("Etc/UTC", primaries);
        Assert.DoesNotContain("Asia/Calcutta", primaries);
        Assert.Contains("Europe/Bratislava", primaries);
        foreach (var primary in primaries)
        {
            Assert.True(Db.TryFind(primary, out var id));
            Assert.Equal(primary, Db.Primary(id).Identifier);
        }

        foreach (var identifier in Identifiers())
        {
            Db.TryFind(identifier, out var id);
            Assert.Contains(Db.Primary(id).Identifier, primaries);
        }

        Assert.Throws<NotSupportedException>(() => ((IList<string>)primaries).Add("Mars/Olympus"));
    }

    [Fact]
    public void A_region_answers_zone_tabs_zones_and_an_unknown_region_none()
    {
        var us = Db.RegionTimeZones("US");
        Assert.Equal(29, us.Count);
        Assert.Contains("America/New_York", us);
        Assert.Equal(us.Order(StringComparer.Ordinal), us);
        Assert.Equal(["Europe/Berlin", "Europe/Busingen"], Db.RegionTimeZones("DE"));
        Assert.Equal(["Europe/Vienna"], Db.RegionTimeZones("AT"));
        Assert.Equal(["Asia/Kolkata"], Db.RegionTimeZones("IN"));
        Assert.Equal(["Europe/Kyiv", "Europe/Simferopol"], Db.RegionTimeZones("UA"));
        Assert.Equal(["Europe/London"], Db.RegionTimeZones("GB"));
        Assert.Equal(10, Db.RegionTimeZones("AQ").Count);
        Assert.Equal(2, Db.RegionTimeZones("CN").Count);
        Assert.Empty(Db.RegionTimeZones("ZZ"));
        Assert.Empty(Db.RegionTimeZones("us"));
        Assert.Empty(Db.RegionTimeZones(string.Empty));
        Assert.Empty(Db.RegionTimeZones("USA"));
    }

    [Fact]
    public void An_identifier_reads_its_primarys_zone()
    {
        foreach (var (link, primary) in new[] { ("EST5EDT", "America/New_York"), ("US/Eastern", "America/New_York"), ("Etc/GMT", "UTC"), ("Asia/Calcutta", "Asia/Kolkata") })
        {
            var a = Rules(link);
            var b = Rules(primary);
            Assert.Same(b, a);
        }

        // EST5EDT READS NEW YORK'S HISTORY, local mean time included (the four POSIX names differ from zic up to 1966).
        Assert.Equal(-17762, Rules("EST5EDT").OffsetSecondsAt(Utc(1883, 1, 1)));
    }

    [Fact]
    public void An_identifier_another_database_gave_is_refused()
    {
        Assert.True(Db.TryFind("Europe/Berlin", out var id));
        Assert.Throws<ArgumentException>(() => Db.Rules(id with { Identifier = "Europe/Paris" }));
        Assert.Throws<ArgumentException>(() => Db.Rules(new TimeZoneId(-1, "Europe/Berlin")));
        Assert.Throws<ArgumentException>(() => Db.Primary(new TimeZoneId(100_000, "Europe/Berlin")));
        Assert.Throws<ArgumentException>(() => Db.Rules(default));
    }

    [Fact]
    public void Rules_decoded_by_many_threads_at_once_answer_alike()
    {
        var database = new TimeZoneDatabase("2026e", TimeTablesFixture.TimeZones, TimeTablesFixture.TimeZoneIds, TimeTablesFixture.TimeZoneRegions);
        var identifiers = Identifiers();
        var answers = new long[identifiers.Length];
        Parallel.For(0, identifiers.Length * 4, i =>
        {
            var index = i % identifiers.Length;
            database.TryFind(identifiers[index], out var id);
            var offset = database.Rules(id).OffsetSecondsAt(Utc(2026, 7, 1));
            Interlocked.Exchange(ref answers[index], offset);
        });

        for (var i = 0; i < identifiers.Length; i++)
            Assert.Equal(Rules(identifiers[i]).OffsetSecondsAt(Utc(2026, 7, 1)), answers[i]);
    }

    // ---- offsets -------------------------------------------------------------------------------------

    [Theory]
    [InlineData("America/New_York", 2026, 3, 8, 6, 59, 59, -18000)]
    [InlineData("America/New_York", 2026, 3, 8, 7, 0, 0, -14400)]
    [InlineData("America/New_York", 2026, 11, 1, 5, 59, 59, -14400)]
    [InlineData("America/New_York", 2026, 11, 1, 6, 0, 0, -18000)]
    [InlineData("America/New_York", 1883, 1, 1, 0, 0, 0, -17762)] // local mean time, -04:56:02
    [InlineData("Asia/Kolkata", 2026, 1, 1, 0, 0, 0, 19800)]
    [InlineData("Australia/Lord_Howe", 2026, 1, 15, 0, 0, 0, 39600)] // +11:00
    [InlineData("Australia/Lord_Howe", 2026, 7, 15, 0, 0, 0, 37800)] // +10:30, a 30-minute shift
    [InlineData("Pacific/Kiritimati", 1994, 12, 30, 12, 0, 0, -36000)]
    [InlineData("Pacific/Kiritimati", 1995, 1, 2, 12, 0, 0, 50400)]
    [InlineData("Africa/Monrovia", 1970, 6, 1, 0, 0, 0, -2670)] // -00:44:30
    [InlineData("Europe/Paris", 1900, 6, 1, 0, 0, 0, 561)] // +00:09:21
    [InlineData("Asia/Kathmandu", 2026, 1, 1, 0, 0, 0, 20700)]
    [InlineData("America/St_Johns", 2026, 7, 1, 0, 0, 0, -9000)]
    [InlineData("Pacific/Apia", 2011, 12, 29, 12, 0, 0, -36000)]
    [InlineData("Pacific/Apia", 2011, 12, 31, 12, 0, 0, 50400)] // 2011-12-30 skipped
    [InlineData("Europe/Dublin", 1925, 9, 25, 12, 0, 0, 0)] // 2026e's correction (Z1)
    [InlineData("America/Vancouver", 2027, 1, 15, 12, 0, 0, -25200)] // 2026b's British Columbia (Z1)
    [InlineData("Africa/Casablanca", 2027, 1, 15, 12, 0, 0, 0)]
    public void The_offset_at_an_instant_is_the_zones(string zone, int year, int month, int day, int hour, int minute, int second, int expected)
    {
        Assert.Equal(expected, Rules(zone).OffsetSecondsAt(Utc(year, month, day, hour, minute, second)));
    }

    [Theory]
    [InlineData("America/New_York", 2040, -14400)] // no 2037 cliff (Z4)
    [InlineData("Europe/Berlin", 2040, 7200)]
    [InlineData("America/Santiago", 2040, -14400)]
    [InlineData("Australia/Sydney", 2040, 36000)]
    [InlineData("America/New_York", 2400, -14400)]
    [InlineData("Europe/London", 2100, 3600)]
    public void Recurring_rules_continue_a_zone_after_2037(string zone, int year, int expected)
    {
        Assert.Equal(expected, Rules(zone).OffsetSecondsAt(Utc(year, 7, 1, 12)));
    }

    [Fact]
    public void The_first_and_last_instants_of_the_range_read_the_zones_offsets()
    {
        var newYork = Rules("America/New_York");
        Assert.Equal(-17762, newYork.OffsetSecondsAt(-8_640_000_000_000L));
        Assert.Equal(-14400, newYork.OffsetSecondsAt(8_640_000_000_000L)); // +275760-09-13, daylight time (A1)
        Assert.Equal(0, Rules("UTC").OffsetSecondsAt(8_640_000_000_000L));
        Assert.Equal(-18000, Rules("Etc/GMT+5").OffsetSecondsAt(-8_640_000_000_000L));
        Assert.Equal(50400, Rules("Etc/GMT-14").OffsetSecondsAt(0));
    }

    [Fact]
    public void An_instant_beyond_two_to_the_53_seconds_is_refused()
    {
        var rules = Rules("America/New_York");
        Assert.Contains(rules.OffsetSecondsAt(TimeZoneDatabase.MaxEpochSeconds), new[] { -18000, -14400 });
        Assert.Equal(-17762, rules.OffsetSecondsAt(-TimeZoneDatabase.MaxEpochSeconds));
        Assert.Equal(rules.OffsetSecondsAt(TimeZoneDatabase.MaxEpochSeconds) == -14400, rules.IsDaylightAt(TimeZoneDatabase.MaxEpochSeconds));
        Assert.Throws<ArgumentOutOfRangeException>(() => rules.OffsetSecondsAt(TimeZoneDatabase.MaxEpochSeconds + 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => rules.IsDaylightAt(long.MinValue));
        Assert.Throws<ArgumentOutOfRangeException>(() => rules.NextTransition(long.MaxValue, long.MaxValue, false));
        Assert.Throws<ArgumentOutOfRangeException>(() => rules.PreviousTransition(-TimeZoneDatabase.MaxEpochSeconds - 1, false));
        Assert.NotNull(rules.NextTransition(0, long.MaxValue, false));
        Assert.NotNull(rules.PreviousTransition(TimeZoneDatabase.MaxEpochSeconds, false));

        // EVERY ZONE ANSWERS at both bounds, its recurring rules included.
        foreach (var identifier in Identifiers())
        {
            var zone = Rules(identifier);
            foreach (var bound in new[] { -TimeZoneDatabase.MaxEpochSeconds, TimeZoneDatabase.MaxEpochSeconds })
            {
                Assert.InRange(zone.OffsetSecondsAt(bound), -86399, 86399);
                _ = zone.IsDaylightAt(bound);
                _ = zone.NextTransition(bound, long.MaxValue, true);
                _ = zone.PreviousTransition(bound, true);
            }
        }
    }

    [Fact]
    public void The_daylight_flag_is_the_compiled_one_and_continues_after_2037()
    {
        var newYork = Rules("America/New_York");
        Assert.True(newYork.IsDaylightAt(Utc(2026, 7, 1)));
        Assert.False(newYork.IsDaylightAt(Utc(2026, 1, 1)));
        Assert.True(newYork.IsDaylightAt(Utc(2040, 7, 1)));
        Assert.False(newYork.IsDaylightAt(Utc(2040, 1, 1)));

        // EUROPE/DUBLIN, whose tzdb rules save negatively in winter: daylight time in summer, as CLDR's names read it.
        var dublin = Rules("Europe/Dublin");
        foreach (var year in new[] { 2026, 2040, 2100 })
        {
            Assert.True(dublin.IsDaylightAt(Utc(year, 7, 1)));
            Assert.Equal(3600, dublin.OffsetSecondsAt(Utc(year, 7, 1)));
            Assert.False(dublin.IsDaylightAt(Utc(year, 1, 15)));
            Assert.Equal(0, dublin.OffsetSecondsAt(Utc(year, 1, 15)));
        }

        Assert.False(Rules("UTC").IsDaylightAt(Utc(2026, 7, 1)));
        Assert.False(Rules("Asia/Kolkata").IsDaylightAt(Utc(2026, 7, 1)));
    }

    // ---- transitions ---------------------------------------------------------------------------------

    private const long Limit = 8_640_000_000_000L;

    [Fact]
    public void The_next_and_previous_transitions_of_New_York_are_strictly_after_and_before()
    {
        var rules = Rules("America/New_York");
        Assert.Equal(Utc(2026, 11, 1, 6), rules.NextTransition(Utc(2026, 7, 1), Limit, false));
        Assert.Equal(Utc(2026, 11, 1, 6), rules.NextTransition(Utc(2026, 3, 8, 7), Limit, false));
        Assert.Equal(Utc(2026, 3, 8, 7), rules.NextTransition(Utc(2026, 3, 8, 6, 59, 59), Limit, false));
        Assert.Equal(Utc(2025, 11, 2, 6), rules.PreviousTransition(Utc(2026, 3, 8, 7), false));
        Assert.Equal(Utc(2026, 3, 8, 7), rules.PreviousTransition(Utc(2026, 3, 8, 7, 0, 1), false));

        // THE FIRST TRANSITION, from local mean time in 1883, and none before it.
        Assert.Equal(Utc(1883, 11, 18, 17), rules.NextTransition(Utc(1800, 1, 1), Limit, false));
        Assert.Null(rules.PreviousTransition(Utc(1883, 11, 18, 17), false));
        Assert.Null(rules.PreviousTransition(-Limit, false));
    }

    [Fact]
    public void There_is_no_2037_cliff()
    {
        var newYork = Rules("America/New_York");
        Assert.Equal(Utc(2038, 3, 14, 7), newYork.NextTransition(Utc(2037, 12, 1), Limit, false));
        Assert.Equal(Utc(2040, 3, 11, 7), newYork.PreviousTransition(Utc(2040, 7, 1), false));
        Assert.Equal(Utc(2099, 11, 1, 6), newYork.PreviousTransition(Utc(2100, 1, 1), false));
        Assert.Equal(Utc(275760, 3, 9, 7), newYork.PreviousTransition(Utc(275760, 9, 1), false));
        Assert.Equal(Utc(2038, 4, 4, 3), Rules("America/Santiago").NextTransition(Utc(2037, 12, 1), Limit, false));
        Assert.Equal(Utc(2040, 10, 28, 1), Rules("Europe/Berlin").NextTransition(Utc(2040, 7, 1), Limit, false));
        Assert.Null(newYork.NextTransition(Limit, Limit, false));
    }

    [Fact]
    public void A_transition_after_the_limit_is_not_answered()
    {
        var rules = Rules("America/New_York");
        var next = Utc(2026, 11, 1, 6);
        Assert.Equal(next, rules.NextTransition(Utc(2026, 7, 1), next, false));
        Assert.Null(rules.NextTransition(Utc(2026, 7, 1), next - 1, false));
        var recurring = Utc(2040, 3, 11, 7);
        Assert.Equal(recurring, rules.NextTransition(Utc(2040, 1, 1), recurring, false));
        Assert.Null(rules.NextTransition(Utc(2040, 1, 1), recurring - 1, false));
        Assert.Null(rules.NextTransition(Utc(2040, 1, 1), Utc(1990, 1, 1), false));
    }

    [Fact]
    public void The_transitions_of_Lord_Howe_Riyadh_and_the_fixed_zones()
    {
        Assert.Equal(Utc(2026, 4, 4, 15), Rules("Australia/Lord_Howe").NextTransition(Utc(2026, 1, 1), Limit, false));
        Assert.Null(Rules("Asia/Riyadh").NextTransition(Utc(2000, 1, 1), Limit, false));
        Assert.Equal(Utc(1947, 3, 13, 20, 53, 8), Rules("Asia/Riyadh").PreviousTransition(Utc(2000, 1, 1), false));
        foreach (var zone in new[] { "Etc/GMT+5", "UTC", "Etc/GMT-14" })
        {
            Assert.Null(Rules(zone).NextTransition(-Limit, Limit, false));
            Assert.Null(Rules(zone).PreviousTransition(Limit, true));
        }
    }

    [Fact]
    public void A_change_of_the_daylight_flag_alone_is_a_transition_only_where_asked()
    {
        // BRITISH STANDARD TIME, 1968 TO 1971: +01:00 all year, the flag changing alone in October 1968 (A9).
        var london = Rules("Europe/London");
        Assert.Equal(Utc(1971, 10, 31, 2), london.NextTransition(Utc(1968, 7, 1), Limit, false));
        Assert.Equal(Utc(1968, 2, 18, 2), london.PreviousTransition(Utc(1971, 7, 1), false));

        var flagChange = london.NextTransition(Utc(1968, 7, 1), Limit, true);
        Assert.NotNull(flagChange);
        Assert.True(flagChange < Utc(1971, 10, 31, 2));
        Assert.Equal(3600, london.OffsetSecondsAt(flagChange!.Value - 1));
        Assert.Equal(3600, london.OffsetSecondsAt(flagChange.Value));
        Assert.NotEqual(london.IsDaylightAt(flagChange.Value - 1), london.IsDaylightAt(flagChange.Value));
        Assert.Equal(flagChange, london.PreviousTransition(flagChange.Value + 1, true));
    }

    [Fact]
    public void Walking_the_transitions_forward_and_back_meets_the_same_instants()
    {
        foreach (var zone in new[] { "America/New_York", "Europe/London", "Australia/Lord_Howe", "Pacific/Apia", "Europe/Dublin", "America/Santiago" })
        {
            var rules = Rules(zone);
            var forward = new List<long>();
            for (var at = rules.NextTransition(Utc(1800, 1, 1), Limit, false); at is { } t && t < Utc(2200, 1, 1); at = rules.NextTransition(t, Limit, false))
            {
                Assert.NotEqual(rules.OffsetSecondsAt(t - 1), rules.OffsetSecondsAt(t));
                forward.Add(t);
            }

            var back = new List<long>();
            for (var at = rules.PreviousTransition(Utc(2200, 1, 1), false); at is { } t; at = rules.PreviousTransition(t, false))
                back.Add(t);

            back.Reverse();
            Assert.Equal(forward, back);
            Assert.True(forward.Count > 10, zone);
        }
    }

    // ---- the compact format, over a table written here -----------------------------------------------

    /// <summary>The generator's encoding, for the decoder's own tests.</summary>
    private sealed class TableWriter
    {
        private readonly List<byte> bytes = [];

        public TableWriter Unsigned(ulong value)
        {
            do
            {
                var b = (byte)(value & 0x7F);
                value >>= 7;
                bytes.Add(value == 0 ? b : (byte)(b | 0x80));
            }
            while (value != 0);
            return this;
        }

        public TableWriter Signed(long value) => Unsigned((ulong)((value << 1) ^ (value >> 63)));

        public TableWriter Byte(int value)
        {
            bytes.Add((byte)value);
            return this;
        }

        // A transition: a delta of whole minutes, or of seconds, and a palette index.
        public TableWriter Transition(long delta, bool seconds, int palette)
        {
            var zigzag = (ulong)((delta << 1) ^ (delta >> 63));
            return Unsigned((zigzag << 1) | (seconds ? 1UL : 0UL)).Byte(palette);
        }

        public byte[] ToArray() => [.. bytes];
    }

    // Zone 0: +01:00 for ever. Zone 1: -05:00, then -04:00 daylight at minute 1000, back 30 seconds later, then
    // from 1971 two recurring rules: the second Sunday of March at 02:00 wall, saving an hour, and the first
    // Sunday of November at 02:00 wall, saving none.
    private static byte[] SmallZones(int weekday = 6, int palette = 1, bool trailing = false)
    {
        var writer = new TableWriter().Unsigned(2)
            .Signed(3600).Unsigned(0).Unsigned(0).Byte(0)
            .Signed(-18000).Unsigned(2).Signed(-36000).Signed((-14400 * 2) + 1).Unsigned(2)
            .Transition(1000, false, palette).Transition(30, true, 0)
            .Byte(2).Signed(-18000).Signed(1970)
            .Byte(3).Byte(2).Byte(weekday).Byte(8).Signed(7200).Byte(0).Signed(3600)
            .Byte(11).Byte(2).Byte(weekday).Byte(1).Signed(7200).Byte(0).Signed(0);
        if (trailing)
            writer.Byte(0);
        return writer.ToArray();
    }

    private static readonly byte[] SmallIds = Encoding.ASCII.GetBytes("A/One|0|\nA/Two|1|\n\nB/Link|1|A/Two\n");

    private static readonly byte[] SmallRegions = Encoding.ASCII.GetBytes("XX|A/One|A/Two\nYY|A/Two");

    [Fact]
    public void The_decoder_reads_the_generators_layout()
    {
        var database = new TimeZoneDatabase("test", SmallZones(), SmallIds, SmallRegions);
        Assert.Equal(["A/One", "A/Two"], database.PrimaryIdentifiers);
        Assert.Equal(["A/One", "A/Two"], database.RegionTimeZones("XX"));
        Assert.Equal(["A/Two"], database.RegionTimeZones("YY"));
        Assert.True(database.TryFind("b/LINK", out var link));
        Assert.Equal("A/Two", database.Primary(link).Identifier);

        Assert.True(database.TryFind("A/One", out var one));
        var fixedZone = database.Rules(one);
        Assert.Equal(3600, fixedZone.OffsetSecondsAt(-TimeZoneDatabase.MaxEpochSeconds));
        Assert.Null(fixedZone.NextTransition(0, Limit, true));

        var zone = database.Rules(link);
        Assert.Equal(-18000, zone.OffsetSecondsAt(59_999));
        Assert.Equal(-14400, zone.OffsetSecondsAt(60_000));
        Assert.True(zone.IsDaylightAt(60_000));
        Assert.Equal(-14400, zone.OffsetSecondsAt(60_029));
        Assert.Equal(-18000, zone.OffsetSecondsAt(60_030));
        Assert.False(zone.IsDaylightAt(60_030));
        Assert.Equal(60_000, zone.NextTransition(0, Limit, false));
        Assert.Equal(60_030, zone.PreviousTransition(Utc(1971, 1, 1), false));

        // THE RECURRING RULES from 1971: 1971-03-14 07:00Z and 1971-11-07 06:00Z.
        Assert.Equal(Utc(1971, 3, 14, 7), zone.NextTransition(60_030, Limit, false));
        Assert.Equal(Utc(1971, 11, 7, 6), zone.NextTransition(Utc(1971, 3, 14, 7), Limit, false));
        Assert.Equal(-14400, zone.OffsetSecondsAt(Utc(1971, 7, 1)));
        Assert.True(zone.IsDaylightAt(Utc(1971, 7, 1)));
        Assert.Equal(-18000, zone.OffsetSecondsAt(Utc(2500, 1, 1)));
        var march = Enumerable.Range(8, 7).Single(static day => IsoCalendarMath.DayOfWeek(2500, 3, day) == 7);
        Assert.Equal(Utc(2500, 3, march, 7), zone.NextTransition(Utc(2500, 1, 1), Limit, false));
        Assert.Equal(Utc(2500, 3, march, 7), zone.PreviousTransition(Utc(2500, 3, march, 7, 0, 1), false));
    }

    [Fact]
    public void A_table_that_does_not_hold_the_layout_is_refused()
    {
        var zones = SmallZones();
        Assert.Throws<ArgumentException>(() => new TimeZoneDatabase("t", zones.AsSpan(0, zones.Length - 1), SmallIds, SmallRegions));
        Assert.Throws<ArgumentException>(() => new TimeZoneDatabase("t", SmallZones(trailing: true), SmallIds, SmallRegions));
        Assert.Throws<ArgumentException>(() => new TimeZoneDatabase("t", SmallZones(weekday: 7), SmallIds, SmallRegions));
        Assert.Throws<ArgumentException>(() => new TimeZoneDatabase("t", SmallZones(palette: 2), SmallIds, SmallRegions));
        Assert.Throws<ArgumentException>(() => new TimeZoneDatabase("t", [0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x01], SmallIds, SmallRegions));
        Assert.Throws<ArgumentException>(() => new TimeZoneDatabase("t", [0x05], SmallIds, SmallRegions));

        foreach (var ids in new[] { "A/One|2|\n", "A/One|x|\n", "A/One|0\n", "A/One|0|\na/one|1|\n", "A/One|0|A/Two\nA/Two|1|B/Three\nB/Three|1|\n",
                     "A/One|0|Mars/Olympus\n", "A/One|0|a/two\nA/Two|1|\n", "A/é|0|\n", "|0|\n" })
        {
            Assert.Throws<ArgumentException>(() => new TimeZoneDatabase("t", SmallZones(), Encoding.UTF8.GetBytes(ids), SmallRegions));
        }
    }
}
