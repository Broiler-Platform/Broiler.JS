using System;
using System.Linq;
using System.Numerics;
using Broiler.ECMAScript.Time;
using Xunit;

namespace Broiler.ECMAScript.Algorithms.Tests;

/// <summary>
/// The zone algorithms (milestone SJL-8.3): possible instants by sampling, the four disambiguations with
/// statuses for <c>reject</c>, the start of a day with a status where no instant exists, the transitions
/// in nanoseconds, zone equality, and the offset zones. Gaps and overlaps are the correspondence's rows A3
/// to A9, which both engines answered.
/// </summary>
public class ZonedTimeTests
{
    private static readonly BigInteger NsPerSecond = 1_000_000_000;

    private static TimeZoneDatabase Db => TimeTablesFixture.Database;

    private static ITimeZoneRules Rules(string identifier) => TimeZoneTests.Rules(identifier);

    private static BigInteger Ns(long year, int month, int day, int hour = 0, int minute = 0, int second = 0) =>
        TimeZoneTests.Utc(year, month, day, hour, minute, second) * NsPerSecond;

    private static IsoDateTime Wall(long year, int month, int day, int hour = 0, int minute = 0, int second = 0, int subsecond = 0) =>
        new(new IsoDate(year, month, day), new IsoTime(hour, minute, second, subsecond / 1_000_000, subsecond / 1000 % 1000, subsecond % 1000));

    private static (TimeStatus Status, BigInteger[] Instants) Possible(ITimeZoneRules rules, IsoDateTime local)
    {
        var buffer = new BigInteger[ZonedTime.MaxPossibleEpochNanoseconds];
        var status = ZonedTime.GetPossibleEpochNanoseconds(rules, local, buffer, out var count);
        return (status, buffer[..count]);
    }

    private static void AssertPossible(ITimeZoneRules rules, IsoDateTime local, TimeStatus status, params BigInteger[] instants)
    {
        var (actual, found) = Possible(rules, local);
        Assert.Equal(status, actual);
        Assert.Equal(instants, found);
    }

    private static (TimeStatus Status, BigInteger Instant) For(ITimeZoneRules rules, IsoDateTime local, Disambiguation mode)
    {
        var status = ZonedTime.GetEpochNanosecondsFor(rules, local, mode, out var instant);
        return (status, instant);
    }

    // ---- the offset at an instant --------------------------------------------------------------------

    [Fact]
    public void The_offset_in_nanoseconds_reads_the_floor_second()
    {
        var newYork = Rules("America/New_York");
        var transition = Ns(2026, 3, 8, 7);
        Assert.Equal(-14_400_000_000_000L, ZonedTime.GetOffsetNanosecondsFor(newYork, transition));
        Assert.Equal(-18_000_000_000_000L, ZonedTime.GetOffsetNanosecondsFor(newYork, transition - 1));
        Assert.Equal(-2_670_000_000_000L, ZonedTime.GetOffsetNanosecondsFor(Rules("Africa/Monrovia"), Ns(1970, 6, 1)));
        Assert.Equal(-17_762_000_000_000L, ZonedTime.GetOffsetNanosecondsFor(newYork, ZonedTime.MinEpochNanoseconds));
        Assert.Equal(19_800_000_000_000L, ZonedTime.GetOffsetNanosecondsFor(new FixedOffsetTimeZoneRules(19800), Ns(2026, 1, 1)));
        Assert.Throws<ArgumentNullException>(() => ZonedTime.GetOffsetNanosecondsFor(null!, BigInteger.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() => ZonedTime.GetOffsetNanosecondsFor(newYork, (BigInteger)TimeZoneDatabase.MaxEpochSeconds * NsPerSecond + 1));
    }

    [Fact]
    public void The_instant_range_is_the_proposals()
    {
        Assert.Equal(BigInteger.Parse("8640000000000000000000", System.Globalization.CultureInfo.InvariantCulture), ZonedTime.MaxEpochNanoseconds);
        Assert.Equal(-ZonedTime.MaxEpochNanoseconds, ZonedTime.MinEpochNanoseconds);
        Assert.Equal(3, ZonedTime.MaxPossibleEpochNanoseconds);
    }

    // ---- possible instants ---------------------------------------------------------------------------

    [Fact]
    public void A_wall_time_names_one_instant_two_in_an_overlap_and_none_in_a_gap()
    {
        var newYork = Rules("America/New_York");
        AssertPossible(newYork, Wall(2026, 7, 1, 12), TimeStatus.Completed, Ns(2026, 7, 1, 16));
        AssertPossible(newYork, Wall(2026, 11, 1, 1, 30), TimeStatus.Completed, Ns(2026, 11, 1, 5, 30), Ns(2026, 11, 1, 6, 30));
        AssertPossible(newYork, Wall(2026, 3, 8, 2, 30), TimeStatus.Completed);

        // A ONE-NANOSECOND GAP EDGE: 02:00 is skipped, 01:59:59.999999999 and 03:00 are not.
        Assert.Single(Possible(newYork, Wall(2026, 3, 8, 1, 59, 59, 999_999_999)).Instants);
        Assert.Empty(Possible(newYork, Wall(2026, 3, 8, 2)).Instants);
        Assert.Single(Possible(newYork, Wall(2026, 3, 8, 3)).Instants);

        // EUROPE/LONDON'S GAP AND OVERLAP.
        var london = Rules("Europe/London");
        Assert.Empty(Possible(london, Wall(2026, 3, 29, 1, 30)).Instants);
        Assert.Equal([Ns(2026, 10, 25, 0, 30), Ns(2026, 10, 25, 1, 30)], Possible(london, Wall(2026, 10, 25, 1, 30)).Instants);

        // AFTER 2037, from the recurring rules (A5).
        Assert.Empty(Possible(newYork, Wall(2040, 3, 11, 2, 30)).Instants);
        Assert.Equal(2, Possible(newYork, Wall(2040, 11, 4, 1, 30)).Instants.Length);
    }

    [Fact]
    public void The_offset_at_the_wall_time_itself_is_sampled_as_well_as_a_day_either_side()
    {
        // AN OFFSET IN FORCE FOR FOUR HOURS around the wall time, which the samples a day either side miss (A2).
        var wall = Ns(2030, 6, 1, 12);
        var rules = new StepRules((BigInteger.Zero, -21600), (wall - (2 * 3600 * NsPerSecond), 0), (wall + (2 * 3600 * NsPerSecond), 21600));
        AssertPossible(rules, Wall(2030, 6, 1, 12), TimeStatus.Completed, wall);
    }

    [Fact]
    public void An_instant_outside_the_range_is_a_status()
    {
        var utc = Rules("UTC");
        AssertPossible(utc, Wall(-271821, 4, 20), TimeStatus.Completed, ZonedTime.MinEpochNanoseconds);
        Assert.Equal(TimeStatus.OutOfRange, Possible(utc, Wall(-271821, 4, 19, 23, 59, 59, 999_999_999)).Status);
        AssertPossible(utc, Wall(275760, 9, 13), TimeStatus.Completed, ZonedTime.MaxEpochNanoseconds);
        Assert.Equal(TimeStatus.OutOfRange, Possible(utc, Wall(275760, 9, 13, 0, 0, 0, 1)).Status);

        // WEST OF UTC, A WALL TIME BEFORE THE FIRST DAY can still name a valid instant, which the named branch keeps.
        var west = Rules("Etc/GMT+1");
        AssertPossible(west, Wall(-271821, 4, 19, 23), TimeStatus.Completed, ZonedTime.MinEpochNanoseconds);
        Assert.Equal(TimeStatus.OutOfRange, Possible(Rules("Etc/GMT-1"), Wall(-271821, 4, 20)).Status);

        // A WALL TIME FAR OUTSIDE is refused before any sampling.
        Assert.Equal(TimeStatus.OutOfRange, Possible(utc, Wall(1_000_000, 1, 1)).Status);
        Assert.Equal(TimeStatus.OutOfRange, Possible(Rules("America/New_York"), Wall(-1_000_000_000, 3, 1)).Status);
    }

    [Fact]
    public void A_broken_precondition_throws()
    {
        var utc = Rules("UTC");
        var buffer = new BigInteger[3];
        Assert.Throws<ArgumentNullException>(() => ZonedTime.GetPossibleEpochNanoseconds(null!, Wall(2026, 1, 1), buffer, out _));
        Assert.Throws<ArgumentException>(() => ZonedTime.GetPossibleEpochNanoseconds(utc, Wall(2026, 1, 1), new BigInteger[2], out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => ZonedTime.GetPossibleEpochNanoseconds(utc, Wall(2026, 2, 29), buffer, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => ZonedTime.GetPossibleEpochNanoseconds(utc, Wall(2026, 13, 1), buffer, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => ZonedTime.GetPossibleEpochNanoseconds(utc, Wall(2026, 1, 1, 24), buffer, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => ZonedTime.GetPossibleEpochNanoseconds(utc, new IsoDateTime(new IsoDate(2026, 1, 1), new IsoTime(0, 0, 0, 1000, 0, 0)), buffer, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => ZonedTime.GetPossibleEpochNanoseconds(utc, default, buffer, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => ZonedTime.GetEpochNanosecondsFor(utc, Wall(2026, 1, 1), (Disambiguation)4, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => ZonedTime.DisambiguatePossibleEpochNanoseconds(utc, [], Wall(2026, 1, 1), (Disambiguation)(-1), out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => ZonedTime.GetStartOfDay(utc, new IsoDate(2026, 4, 31), out _));
    }

    // ---- disambiguation ------------------------------------------------------------------------------

    public static TheoryData<string, int, int, int, int, int, int, long, long> Gaps => new()
    {
        // zone, wall time, then the instant "earlier" and "later" (= "compatible") give, as UTC seconds.
        { "America/New_York", 2026, 3, 8, 2, 30, 0, TimeZoneTests.Utc(2026, 3, 8, 6, 30), TimeZoneTests.Utc(2026, 3, 8, 7, 30) },
        { "Europe/London", 2026, 3, 29, 1, 30, 0, TimeZoneTests.Utc(2026, 3, 29, 0, 30), TimeZoneTests.Utc(2026, 3, 29, 1, 30) },
        { "Europe/Berlin", 2026, 3, 29, 2, 30, 0, TimeZoneTests.Utc(2026, 3, 29, 0, 30), TimeZoneTests.Utc(2026, 3, 29, 1, 30) },
        { "Australia/Lord_Howe", 2026, 10, 4, 2, 15, 0, TimeZoneTests.Utc(2026, 10, 3, 15, 15), TimeZoneTests.Utc(2026, 10, 3, 15, 45) }, // 30 minutes
        { "Pacific/Kiritimati", 1994, 12, 31, 12, 0, 0, TimeZoneTests.Utc(1994, 12, 30, 22), TimeZoneTests.Utc(1994, 12, 31, 22) }, // a skipped day
        { "Pacific/Apia", 2011, 12, 30, 12, 0, 0, TimeZoneTests.Utc(2011, 12, 29, 22), TimeZoneTests.Utc(2011, 12, 30, 22) }, // 2011-12-30 skipped
        { "Africa/Monrovia", 1972, 1, 7, 0, 20, 0, TimeZoneTests.Utc(1972, 1, 7, 0, 20), TimeZoneTests.Utc(1972, 1, 7, 1, 4, 30) }, // 44 min 30 s
        { "America/New_York", 2040, 3, 11, 2, 30, 0, TimeZoneTests.Utc(2040, 3, 11, 6, 30), TimeZoneTests.Utc(2040, 3, 11, 7, 30) }, // after 2037 (A5)
    };

    [Theory]
    [MemberData(nameof(Gaps))]
    public void A_skipped_wall_time_moves_by_the_gap_and_reject_is_a_status(string zone, int year, int month, int day, int hour, int minute, int second, long earlier, long later)
    {
        var rules = Rules(zone);
        var local = Wall(year, month, day, hour, minute, second);
        Assert.Empty(Possible(rules, local).Instants);
        Assert.Equal((TimeStatus.Completed, earlier * NsPerSecond), For(rules, local, Disambiguation.Earlier));
        Assert.Equal((TimeStatus.Completed, later * NsPerSecond), For(rules, local, Disambiguation.Later));
        Assert.Equal((TimeStatus.Completed, later * NsPerSecond), For(rules, local, Disambiguation.Compatible));
        Assert.Equal((TimeStatus.Skipped, BigInteger.Zero), For(rules, local, Disambiguation.Reject));
    }

    public static TheoryData<string, int, int, int, int, int, long, long> Overlaps => new()
    {
        // zone, wall time, then the earlier ("compatible", "earlier") and the later ("later") instant, as UTC seconds.
        { "America/New_York", 2026, 11, 1, 1, 30, TimeZoneTests.Utc(2026, 11, 1, 5, 30), TimeZoneTests.Utc(2026, 11, 1, 6, 30) },
        { "Europe/London", 2026, 10, 25, 1, 30, TimeZoneTests.Utc(2026, 10, 25, 0, 30), TimeZoneTests.Utc(2026, 10, 25, 1, 30) },
        { "Europe/Berlin", 2026, 10, 25, 2, 30, TimeZoneTests.Utc(2026, 10, 25, 0, 30), TimeZoneTests.Utc(2026, 10, 25, 1, 30) },
        { "Australia/Lord_Howe", 2026, 4, 5, 1, 45, TimeZoneTests.Utc(2026, 4, 4, 14, 45), TimeZoneTests.Utc(2026, 4, 4, 15, 15) }, // 30 minutes
        { "America/Juneau", 1867, 10, 19, 12, 0, TimeZoneTests.Utc(1867, 10, 19, 12) - 54139, TimeZoneTests.Utc(1867, 10, 19, 12) + 32261 }, // a day
        { "America/New_York", 2040, 11, 4, 1, 30, TimeZoneTests.Utc(2040, 11, 4, 5, 30), TimeZoneTests.Utc(2040, 11, 4, 6, 30) }, // after 2037
    };

    [Theory]
    [MemberData(nameof(Overlaps))]
    public void A_repeated_wall_time_is_its_earlier_or_later_instant_and_reject_is_a_status(string zone, int year, int month, int day, int hour, int minute, long earlier, long later)
    {
        var rules = Rules(zone);
        var local = Wall(year, month, day, hour, minute);
        Assert.Equal([earlier * NsPerSecond, later * NsPerSecond], Possible(rules, local).Instants);
        Assert.Equal((TimeStatus.Completed, earlier * NsPerSecond), For(rules, local, Disambiguation.Compatible));
        Assert.Equal((TimeStatus.Completed, earlier * NsPerSecond), For(rules, local, Disambiguation.Earlier));
        Assert.Equal((TimeStatus.Completed, later * NsPerSecond), For(rules, local, Disambiguation.Later));
        Assert.Equal((TimeStatus.Ambiguous, BigInteger.Zero), For(rules, local, Disambiguation.Reject));
    }

    [Fact]
    public void Disambiguation_takes_the_instants_it_is_given()
    {
        var newYork = Rules("America/New_York");
        var local = Wall(2026, 7, 1, 12);
        foreach (var mode in new[] { Disambiguation.Compatible, Disambiguation.Earlier, Disambiguation.Later, Disambiguation.Reject })
        {
            Assert.Equal(TimeStatus.Completed, ZonedTime.DisambiguatePossibleEpochNanoseconds(newYork, [Ns(2026, 7, 1, 16)], local, mode, out var one));
            Assert.Equal(Ns(2026, 7, 1, 16), one);
            Assert.Equal((TimeStatus.Completed, Ns(2026, 7, 1, 16)), For(newYork, local, mode));
        }

        Assert.Equal(TimeStatus.Completed, ZonedTime.DisambiguatePossibleEpochNanoseconds(newYork, [1, 2], local, Disambiguation.Later, out var last));
        Assert.Equal(2, last);
        Assert.Equal(TimeStatus.Ambiguous, ZonedTime.DisambiguatePossibleEpochNanoseconds(newYork, [1, 2], local, Disambiguation.Reject, out _));
        Assert.Equal(TimeStatus.Skipped, ZonedTime.DisambiguatePossibleEpochNanoseconds(newYork, [], Wall(2026, 3, 8, 2, 30), Disambiguation.Reject, out _));
        Assert.Equal(TimeStatus.Completed, ZonedTime.DisambiguatePossibleEpochNanoseconds(newYork, [], Wall(2026, 3, 8, 2, 30), Disambiguation.Earlier, out var moved));
        Assert.Equal(Ns(2026, 3, 8, 6, 30), moved);
        Assert.Throws<ArgumentNullException>(() => ZonedTime.DisambiguatePossibleEpochNanoseconds(null!, [], local, Disambiguation.Later, out _));
    }

    [Fact]
    public void A_gap_moved_past_the_range_is_a_status()
    {
        // A GAP whose later side is past nsMaxInstant: the moved time names an instant outside the range.
        var limit = ZonedTime.MaxEpochNanoseconds;
        var rules = new StepRules((BigInteger.Zero, -3600), (limit - (1800 * NsPerSecond), 3600));
        var local = Wall(275760, 9, 12, 23, 45);
        Assert.Empty(Possible(rules, local).Instants);
        Assert.Equal(TimeStatus.OutOfRange, For(rules, local, Disambiguation.Compatible).Status);
        Assert.Equal(TimeStatus.Completed, For(rules, local, Disambiguation.Earlier).Status);
        Assert.Equal(TimeStatus.Skipped, For(rules, local, Disambiguation.Reject).Status);
    }

    // ---- the start of a day --------------------------------------------------------------------------

    [Theory]
    [InlineData("America/Santiago", 2026, 9, 6, 2026, 9, 6, 4, 0)] // 01:00-03:00
    [InlineData("America/Havana", 2026, 3, 8, 2026, 3, 8, 5, 0)] // 01:00-04:00
    [InlineData("Asia/Beirut", 2026, 3, 29, 2026, 3, 28, 22, 0)] // 01:00+03:00
    [InlineData("Asia/Beirut", 1920, 3, 28, 1920, 3, 27, 22, 0)]
    [InlineData("America/Toronto", 1919, 3, 31, 1919, 3, 31, 4, 30)] // 00:30-04:00, 23:30 to 00:30 skipped
    [InlineData("America/Sao_Paulo", 2018, 11, 4, 2018, 11, 4, 3, 0)] // 01:00-02:00
    [InlineData("America/Asuncion", 2024, 10, 6, 2024, 10, 6, 4, 0)] // 01:00-03:00
    [InlineData("Pacific/Apia", 2011, 12, 30, 2011, 12, 30, 10, 0)] // the skipped day starts 2011-12-31T00:00+14:00
    [InlineData("America/Santiago", 2040, 9, 2, 2040, 9, 2, 4, 0)] // after 2037 (A7)
    [InlineData("America/Havana", 2040, 3, 11, 2040, 3, 11, 5, 0)]
    [InlineData("Asia/Beirut", 2040, 3, 25, 2040, 3, 24, 22, 0)]
    [InlineData("America/New_York", 2026, 3, 8, 2026, 3, 8, 5, 0)] // midnight exists: 00:00-05:00
    [InlineData("Europe/London", 2026, 10, 25, 2026, 10, 24, 23, 0)] // midnight exists: 00:00+01:00
    public void A_day_starts_at_midnight_or_at_the_transition_that_skips_it(string zone, int year, int month, int day, int y, int m, int d, int hour, int minute)
    {
        Assert.Equal(TimeStatus.Completed, ZonedTime.GetStartOfDay(Rules(zone), new IsoDate(year, month, day), out var start));
        Assert.Equal(Ns(y, m, d, hour, minute), start);
    }

    [Fact]
    public void A_day_outside_the_range_or_with_no_instant_is_a_status_never_local_midnight()
    {
        Assert.Equal(TimeStatus.Completed, ZonedTime.GetStartOfDay(Rules("UTC"), new IsoDate(-271821, 4, 20), out var first));
        Assert.Equal(ZonedTime.MinEpochNanoseconds, first);
        Assert.Equal(TimeStatus.Completed, ZonedTime.GetStartOfDay(Rules("UTC"), new IsoDate(275760, 9, 13), out var last));
        Assert.Equal(ZonedTime.MaxEpochNanoseconds, last);
        Assert.Equal(TimeStatus.OutOfRange, ZonedTime.GetStartOfDay(Rules("UTC"), new IsoDate(-271821, 4, 19), out var none));
        Assert.Equal(BigInteger.Zero, none);
        Assert.Equal(TimeStatus.OutOfRange, ZonedTime.GetStartOfDay(Rules("Etc/GMT-1"), new IsoDate(-271821, 4, 20), out _));
        Assert.Equal(TimeStatus.OutOfRange, ZonedTime.GetStartOfDay(Rules("Etc/GMT+1"), new IsoDate(275760, 9, 13), out _));
        Assert.Equal(TimeStatus.Completed, ZonedTime.GetStartOfDay(Rules("Etc/GMT+1"), new IsoDate(-271821, 4, 20), out _));

        // MIDNIGHT SKIPPED with the transition past nsMaxInstant: no instant of the day exists.
        var limit = ZonedTime.MaxEpochNanoseconds;
        var rules = new StepRules((BigInteger.Zero, -3600), (limit + (1800 * NsPerSecond), 3600));
        Assert.Equal(TimeStatus.NoStartOfDay, ZonedTime.GetStartOfDay(rules, new IsoDate(275760, 9, 13), out var missing));
        Assert.Equal(BigInteger.Zero, missing);
        Assert.Throws<ArgumentNullException>(() => ZonedTime.GetStartOfDay(null!, new IsoDate(2026, 1, 1), out _));
    }

    // ---- transitions in nanoseconds ------------------------------------------------------------------

    [Fact]
    public void The_transitions_in_nanoseconds_are_strictly_after_and_before()
    {
        var newYork = Rules("America/New_York");
        var march = Ns(2026, 3, 8, 7);
        Assert.Equal(Ns(2026, 11, 1, 6), ZonedTime.GetNextTransition(newYork, Ns(2026, 7, 1)));
        Assert.Equal(Ns(2026, 11, 1, 6), ZonedTime.GetNextTransition(newYork, march));
        Assert.Equal(march, ZonedTime.GetNextTransition(newYork, march - 1));
        Assert.Equal(Ns(2025, 11, 2, 6), ZonedTime.GetPreviousTransition(newYork, march));
        Assert.Equal(march, ZonedTime.GetPreviousTransition(newYork, march + 500_000_000)); // half a second after
        Assert.Equal(march, ZonedTime.GetPreviousTransition(newYork, march + 1));
        Assert.Equal(Ns(2038, 3, 14, 7), ZonedTime.GetNextTransition(newYork, Ns(2037, 12, 1)));
        Assert.Equal(Ns(275760, 3, 9, 7), ZonedTime.GetPreviousTransition(newYork, ZonedTime.MaxEpochNanoseconds));
        Assert.Null(ZonedTime.GetNextTransition(newYork, ZonedTime.MaxEpochNanoseconds));
        Assert.Null(ZonedTime.GetPreviousTransition(newYork, ZonedTime.MinEpochNanoseconds));
        Assert.Equal(Ns(1883, 11, 18, 17), ZonedTime.GetNextTransition(newYork, ZonedTime.MinEpochNanoseconds));

        var offset = new FixedOffsetTimeZoneRules(-18000);
        Assert.Null(ZonedTime.GetNextTransition(offset, BigInteger.Zero));
        Assert.Null(ZonedTime.GetPreviousTransition(offset, BigInteger.Zero));
        Assert.Null(ZonedTime.GetNextTransition(Rules("Etc/GMT+5"), BigInteger.Zero));
        Assert.Throws<ArgumentNullException>(() => ZonedTime.GetNextTransition(null!, BigInteger.Zero));
        Assert.Throws<ArgumentNullException>(() => ZonedTime.GetPreviousTransition(null!, BigInteger.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() => ZonedTime.GetNextTransition(newYork, BigInteger.Pow(10, 30)));
    }

    // ---- zone equality -------------------------------------------------------------------------------

    [Theory]
    [InlineData("Europe/Bratislava", "Europe/Prague", false)] // Z7: CLDR keeps both
    [InlineData("Arctic/Longyearbyen", "Europe/Oslo", false)]
    [InlineData("Europe/Busingen", "Europe/Zurich", false)]
    [InlineData("Asia/Calcutta", "Asia/Kolkata", true)]
    [InlineData("Europe/Kiev", "Europe/Kyiv", true)]
    [InlineData("EST5EDT", "America/New_York", true)]
    [InlineData("CET", "Europe/Brussels", true)]
    [InlineData("Etc/GMT", "UTC", true)]
    [InlineData("utc", "Etc/UTC", true)]
    [InlineData("America/New_York", "America/Chicago", false)]
    [InlineData("+05:30", "+05:30", true)]
    [InlineData("+05:30", "-05:30", false)]
    [InlineData("+00:00", "UTC", false)]
    [InlineData("UTC", "+00:00", false)]
    [InlineData("-05:00", "Etc/GMT+5", false)] // A14
    [InlineData("Mars/Olympus", "Mars/Olympus", true)] // the same string, as the proposal's first step
    public void Two_zones_are_equal_as_one_string_or_one_primary(string one, string two, bool equal)
    {
        Assert.Equal(equal, ZonedTime.TimeZoneEquals(Db, one, two));
        Assert.Equal(equal, ZonedTime.TimeZoneEquals(Db, two, one));
    }

    [Fact]
    public void Equality_of_an_unknown_name_is_a_broken_precondition()
    {
        Assert.Throws<ArgumentException>(() => ZonedTime.TimeZoneEquals(Db, "Mars/Olympus", "UTC"));
        Assert.False(ZonedTime.TimeZoneEquals(Db, "Mars/Olympus", "+01:00"));
        Assert.Throws<ArgumentNullException>(() => ZonedTime.TimeZoneEquals(null!, "UTC", "UTC"));
    }

    // ---- offset zones --------------------------------------------------------------------------------

    [Theory]
    [InlineData("+05:30", 330, "+05:30")]
    [InlineData("-05:30", -330, "-05:30")]
    [InlineData("+0530", 330, "+05:30")]
    [InlineData("+05", 300, "+05:00")]
    [InlineData("-00:00", 0, "+00:00")]
    [InlineData("+00:00", 0, "+00:00")]
    [InlineData("-00", 0, "+00:00")]
    [InlineData("+23:59", 1439, "+23:59")]
    [InlineData("-23:59", -1439, "-23:59")]
    [InlineData("-1200", -720, "-12:00")]
    public void An_offset_identifier_is_read_and_normalised(string text, int minutes, string normalised)
    {
        Assert.True(OffsetTimeZones.TryParseOffsetIdentifier(text, out var offset));
        Assert.Equal(minutes, offset);
        Assert.Equal(normalised, OffsetTimeZones.FormatOffsetIdentifier(offset));
        Assert.True(OffsetTimeZones.IsTimeZoneOffsetString(text));
    }

    [Theory]
    [InlineData("+24:00")]
    [InlineData("+05:60")]
    [InlineData("+05:30:15")]
    [InlineData("+05:30:00")]
    [InlineData("+053015")]
    [InlineData("+05:30:15.5")]
    [InlineData("−05:00")] // U+2212 MINUS SIGN
    [InlineData("+05:3")]
    [InlineData("05:30")]
    [InlineData("+5:30")]
    [InlineData("+٠٥:٣٠")] // A13: Arabic-Indic digits
    [InlineData("+0٥:30")]
    [InlineData("+05:3٠")]
    [InlineData("+０５:30")] // fullwidth digits
    [InlineData("")]
    [InlineData("+")]
    [InlineData("+0")]
    [InlineData("+05:")]
    [InlineData("+05:30 ")]
    [InlineData(" +05:30")]
    [InlineData("+0530:")]
    [InlineData("+05:3015")]
    [InlineData("+0530:15")]
    [InlineData("UTC")]
    public void A_string_that_is_not_an_offset_identifier_is_refused(string text)
    {
        Assert.False(OffsetTimeZones.TryParseOffsetIdentifier(text, out var minutes));
        Assert.Equal(0, minutes);
    }

    [Theory]
    [InlineData("+05:30:15", true)]
    [InlineData("+053015", true)]
    [InlineData("+05:30:15.5", true)]
    [InlineData("+05:30:15,123456789", true)]
    [InlineData("+053015.123456789", true)]
    [InlineData("-23:59:59.999999999", true)]
    [InlineData("+05:30:15.1234567890", false)] // ten fraction digits
    [InlineData("+05:30:15.", false)]
    [InlineData("+05:30:15.5a", false)]
    [InlineData("+05:30:15.٥", false)]
    [InlineData("+05:30:60", false)]
    [InlineData("+05:3015", false)]
    [InlineData("+0530:15", false)]
    [InlineData("+05:30.5", false)]
    [InlineData("+٠٥:30", false)]
    public void An_offset_string_may_have_seconds_and_a_fraction(string text, bool isOffset)
    {
        Assert.Equal(isOffset, OffsetTimeZones.IsTimeZoneOffsetString(text));
        Assert.False(OffsetTimeZones.TryParseOffsetIdentifier(text, out _));
    }

    [Fact]
    public void An_offset_identifier_is_formatted_as_the_proposal_writes_it()
    {
        Assert.Equal("+00:00", OffsetTimeZones.FormatOffsetIdentifier(0));
        Assert.Equal("-00:01", OffsetTimeZones.FormatOffsetIdentifier(-1));
        Assert.Equal("+10:05", OffsetTimeZones.FormatOffsetIdentifier(605));
        for (var minutes = -1439; minutes <= 1439; minutes++)
        {
            var text = OffsetTimeZones.FormatOffsetIdentifier(minutes);
            Assert.True(OffsetTimeZones.TryParseOffsetIdentifier(text, out var back));
            Assert.Equal(minutes, back);
        }

        Assert.Throws<ArgumentOutOfRangeException>(() => OffsetTimeZones.FormatOffsetIdentifier(1440));
        Assert.Throws<ArgumentOutOfRangeException>(() => OffsetTimeZones.FormatOffsetIdentifier(-1440));
    }

    [Fact]
    public void The_Etc_GMT_zones_are_fixed_offsets_with_POSIXs_inverted_sign()
    {
        var found = 0;
        for (var hours = 1; hours <= 14; hours++)
        {
            foreach (var sign in new[] { '+', '-' })
            {
                var name = "Etc/GMT" + sign + hours.ToString(System.Globalization.CultureInfo.InvariantCulture);
                var expected = sign == '+' ? -hours * 60 : hours * 60;
                var known = sign == '-' || hours <= 12;
                Assert.Equal(known, OffsetTimeZones.TryGetEtcGmtOffset(name, out var minutes));
                Assert.Equal(known ? expected : 0, minutes);
                Assert.Equal(known, Db.TryFind(name, out var id));
                if (!known)
                    continue;

                found++;
                Assert.Equal(name, Db.Primary(id).Identifier);
                Assert.Equal(expected * 60, Db.Rules(id).OffsetSecondsAt(TimeZoneTests.Utc(1850, 1, 1)));
                Assert.Null(Db.Rules(id).NextTransition(-8_640_000_000_000L, 8_640_000_000_000L, true));
            }
        }

        Assert.Equal(26, found);
        foreach (var other in new[] { "Etc/GMT", "Etc/GMT0", "Etc/GMT+0", "Etc/GMT-0", "Etc/GMT+05", "etc/gmt+5", "Etc/GMT+5 ", "GMT+5", "Etc/GMT+1a", "Etc/GMT*5", "UTC", "" })
        {
            Assert.False(OffsetTimeZones.TryGetEtcGmtOffset(other, out var minutes));
            Assert.Equal(0, minutes);
        }
    }

    [Fact]
    public void Fixed_offset_rules_have_one_offset_no_daylight_and_no_transition()
    {
        var rules = new FixedOffsetTimeZoneRules(-86399);
        Assert.Equal(-86399, rules.OffsetSecondsAt(long.MinValue));
        Assert.False(rules.IsDaylightAt(0));
        Assert.Null(rules.NextTransition(0, long.MaxValue, true));
        Assert.Null(rules.PreviousTransition(0, true));
        Assert.Equal(86399, new FixedOffsetTimeZoneRules(86399).OffsetSecondsAt(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FixedOffsetTimeZoneRules(86400));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FixedOffsetTimeZoneRules(-86400));

        // AN OFFSET ZONE'S WALL TIME names one instant, refused outside the range (A12).
        var india = new FixedOffsetTimeZoneRules(19800);
        AssertPossible(india, Wall(2020, 1, 1), TimeStatus.Completed, Ns(2020, 1, 1) - (19800 * NsPerSecond));
        Assert.Equal((TimeStatus.Completed, Ns(2020, 1, 1, 12)), For(new FixedOffsetTimeZoneRules(-43200), Wall(2020, 1, 1), Disambiguation.Reject));
        Assert.Equal(TimeStatus.OutOfRange, Possible(india, Wall(-271821, 4, 20)).Status);
        Assert.Equal(TimeStatus.Completed, ZonedTime.GetStartOfDay(india, new IsoDate(-271821, 4, 21), out _));
    }

    /// <summary>A zone of steps: each offset in force from its instant to the next step's, the first from the start of time.</summary>
    private sealed class StepRules(params (BigInteger From, int Offset)[] steps) : ITimeZoneRules
    {
        public int OffsetSecondsAt(long epochSeconds)
        {
            var offset = steps[0].Offset;
            foreach (var (from, value) in steps.Skip(1))
            {
                if (epochSeconds * NsPerSecond >= from)
                    offset = value;
            }

            return offset;
        }

        public bool IsDaylightAt(long epochSeconds) => false;

        public long? NextTransition(long afterEpochSeconds, long limitEpochSeconds, bool daylightFlag)
        {
            foreach (var (from, _) in steps.Skip(1))
            {
                var seconds = (long)(from / NsPerSecond);
                if (seconds > afterEpochSeconds)
                    return seconds <= limitEpochSeconds ? seconds : null;
            }

            return null;
        }

        public long? PreviousTransition(long beforeEpochSeconds, bool daylightFlag)
        {
            foreach (var (from, _) in steps.Skip(1).Reverse())
            {
                var seconds = (long)(from / NsPerSecond);
                if (seconds < beforeEpochSeconds)
                    return seconds;
            }

            return null;
        }
    }
}
