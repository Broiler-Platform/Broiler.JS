using System;
using System.Collections.Generic;
using System.Globalization;
using Broiler.ECMAScript.Time;
using Xunit;

namespace Broiler.ECMAScript.Algorithms.Tests;

/// <summary>
/// The Date area (milestone SJL-6.3): each member against .NET's own proleptic Gregorian calendar where
/// both are defined, against the calendar's 400-year period where .NET's is not, and at the edges the
/// specification and correspondence rows of bundle sjl-6-001 name. The area's vectors and the
/// reference-arm comparison are its tests at scale; these pin the cases each member is responsible for.
/// </summary>
public class DateTests
{
    private static readonly DateTime Epoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static double Utc(int year, int month, int day, int hour = 0, int minute = 0, int second = 0, int ms = 0) =>
        EcmaTime.TimeClip(EcmaTime.MakeDate(EcmaTime.MakeDay(year, month - 1, day), EcmaTime.MakeTime(hour, minute, second, ms)));

    private static bool IsNegativeZero(double x) => x == 0 && double.IsNegative(x);

    private sealed class Meter
    {
        public List<ulong> Charges { get; } = [];

        public void Charge(ulong units) => Charges.Add(units);
    }

    // A local time zone over the host's TimeZoneInfo, as Broiler.JS's provider reads it.
    private sealed class HostZone(TimeZoneInfo zone) : ILocalTimeProvider
    {
        public double GetOffsetMilliseconds(double epochMilliseconds)
        {
            var clamped = Math.Clamp(epochMilliseconds, -62135596800000, 253402300799999);
            var offset = zone.GetUtcOffset(DateTimeOffset.FromUnixTimeMilliseconds((long)Math.Floor(clamped)));
            return Math.Truncate(offset.TotalMilliseconds);
        }

        public string? GetName(double epochMilliseconds) => zone.Id;
    }

    private static readonly ILocalTimeProvider NewYork = new HostZone(TimeZoneInfo.FindSystemTimeZoneById("America/New_York"));

    // ---- time arithmetic -----------------------------------------------------------------------

    [Fact]
    public void The_fields_of_drawn_time_values_agree_with_the_platform_calendar_from_year_1_to_9999()
    {
        var draws = new SplitMix64(0x534A4C362D544553);
        const long Low = -62135596800000;
        const long High = 253402300799999;
        for (var i = 0; i < 20000; i++)
        {
            var tv = (double)(Low + (long)(draws.Next() % (ulong)(High - Low + 1)));
            var when = Epoch.AddMilliseconds(tv);
            Assert.Equal(when.Year, EcmaTime.YearFromTime(tv));
            Assert.Equal(when.Month - 1, EcmaTime.MonthFromTime(tv));
            Assert.Equal(when.Day, EcmaTime.DateFromTime(tv));
            Assert.Equal((int)when.DayOfWeek, EcmaTime.WeekDay(tv));
            Assert.Equal(when.Hour, EcmaTime.HourFromTime(tv));
            Assert.Equal(when.Minute, EcmaTime.MinFromTime(tv));
            Assert.Equal(when.Second, EcmaTime.SecFromTime(tv));
            Assert.Equal(when.Millisecond, EcmaTime.MsFromTime(tv));
            Assert.Equal(when.DayOfYear - 1, EcmaTime.DayWithinYear(tv));
            Assert.Equal(DateTime.IsLeapYear(when.Year) ? 1 : 0, EcmaTime.InLeapYear(tv));
            Assert.Equal(tv, Utc(when.Year, when.Month, when.Day, when.Hour, when.Minute, when.Second, when.Millisecond));
        }
    }

    [Fact]
    public void The_range_ends_and_the_years_around_zero_are_the_specification_s()
    {
        Assert.Equal(8.64e15, Utc(275760, 9, 13));
        Assert.Equal(-8.64e15, Utc(-271821, 4, 20));
        Assert.Equal(275760, EcmaTime.YearFromTime(8.64e15));
        Assert.Equal(-271821, EcmaTime.YearFromTime(-8.64e15));
        Assert.Equal(-62167219200000, Utc(0, 1, 1));
        Assert.Equal(6, EcmaTime.WeekDay(Utc(0, 1, 1)));
        Assert.Equal(366, EcmaTime.DaysInYear(0));
        Assert.Equal(365, EcmaTime.DaysInYear(-1));
        Assert.Equal(365, EcmaTime.DaysInYear(1900));
        Assert.Equal(366, EcmaTime.DaysInYear(2000));
        Assert.Equal(-1, EcmaTime.YearFromTime(-62167219200001));
        Assert.Equal(11, EcmaTime.MonthFromTime(-62167219200001));
        Assert.Equal(31, EcmaTime.DateFromTime(-62167219200001));
        Assert.Equal(86399999, EcmaTime.TimeWithinDay(-1));
        Assert.Equal(23, EcmaTime.HourFromTime(-1));
        Assert.Equal(999, EcmaTime.MsFromTime(-1));
    }

    [Fact]
    public void The_fields_repeat_every_400_years_where_the_platform_calendar_does_not_reach()
    {
        const double Period = 146097 * EcmaTime.MsPerDay;
        var draws = new SplitMix64(0x534A4C362D343030);
        for (var i = 0; i < 5000; i++)
        {
            var t = -8.64e15 + (draws.Next() % (ulong)(2 * 8.64e15 - Period));
            var shifted = t + Period;
            Assert.Equal(EcmaTime.YearFromTime(t) + 400, EcmaTime.YearFromTime(shifted));
            Assert.Equal(EcmaTime.MonthFromTime(t), EcmaTime.MonthFromTime(shifted));
            Assert.Equal(EcmaTime.DateFromTime(t), EcmaTime.DateFromTime(shifted));
            Assert.Equal(EcmaTime.WeekDay(t), EcmaTime.WeekDay(shifted));
            Assert.Equal(EcmaTime.DayWithinYear(t), EcmaTime.DayWithinYear(shifted));
        }
    }

    [Fact]
    public void A_field_of_NaN_or_an_infinity_is_NaN_and_a_magnitude_past_2_to_the_62_is_refused()
    {
        foreach (var t in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity })
        {
            Assert.True(double.IsNaN(EcmaTime.Day(t)));
            Assert.True(double.IsNaN(EcmaTime.YearFromTime(t)));
            Assert.True(double.IsNaN(EcmaTime.MonthFromTime(t)));
            Assert.True(double.IsNaN(EcmaTime.WeekDay(t)));
            Assert.True(double.IsNaN(EcmaTime.MsFromTime(t)));
        }

        Assert.Throws<ArgumentOutOfRangeException>(() => EcmaTime.YearFromTime(1e19));
        Assert.True(double.IsNaN(EcmaTime.DayFromYear(1.5)));
        Assert.True(double.IsNaN(EcmaTime.DaysInYear(double.NaN)));
    }

    [Fact]
    public void TimeClip_folds_negative_zero_and_refuses_past_the_range()
    {
        Assert.False(IsNegativeZero(EcmaTime.TimeClip(-0.0)));
        Assert.False(IsNegativeZero(EcmaTime.TimeClip(-0.5)));
        Assert.Equal(1, EcmaTime.TimeClip(1.9));
        Assert.Equal(8.64e15, EcmaTime.TimeClip(8.64e15));
        Assert.True(double.IsNaN(EcmaTime.TimeClip(8.64e15 + 1)));
        Assert.True(double.IsNaN(EcmaTime.TimeClip(double.NegativeInfinity)));
        Assert.True(double.IsNaN(EcmaTime.TimeClip(double.NaN)));
    }

    [Fact]
    public void MakeTime_truncates_each_component_and_refuses_a_non_finite_one()
    {
        Assert.Equal(EcmaTime.MsPerHour, EcmaTime.MakeTime(1.9, 0, 0, 0));
        Assert.Equal(-EcmaTime.MsPerHour, EcmaTime.MakeTime(-1.9, 0, 0, 0));
        Assert.Equal(-1, EcmaTime.MakeTime(0, 0, 0, -1.5));
        Assert.False(IsNegativeZero(EcmaTime.MakeTime(-0.0, -0.0, -0.0, -0.0)));
        Assert.True(double.IsNaN(EcmaTime.MakeTime(0, double.PositiveInfinity, 0, 0)));
        Assert.True(double.IsNaN(EcmaTime.MakeTime(0, 0, double.NaN, 0)));
        Assert.Equal((1e20 * EcmaTime.MsPerHour) + 1, EcmaTime.MakeTime(1e20, 0, 0, 1));
    }

    [Fact]
    public void MakeDay_takes_the_month_s_modulo_exactly_where_both_engines_did_not()
    {
        // Correspondence row T4: May 1 of year 0. Broiler.VM's floating modulo read January, Broiler.JS's
        // saturating cast August.
        Assert.Equal(-62156764800000, EcmaTime.TimeClip(EcmaTime.MakeDate(EcmaTime.MakeDay(-833333333333333333.0, 1e19, 1), 0)));
        Assert.Equal(EcmaTime.MakeDay(1971, 0, 1), EcmaTime.MakeDay(1970, 12, 1));
        Assert.Equal(EcmaTime.MakeDay(1969, 11, 1), EcmaTime.MakeDay(1970, -1, 1));
        Assert.Equal(EcmaTime.MakeDay(2000, 2, 1), EcmaTime.MakeDay(2000, 1, 30));
        Assert.Equal(EcmaTime.MakeDay(1900, 2, 1), EcmaTime.MakeDay(1900, 1, 29));
        Assert.Equal(0, EcmaTime.MakeDay(1970, 0, 1));
        Assert.True(double.IsNaN(EcmaTime.MakeDay(double.MaxValue, 12, 1)));
        Assert.True(double.IsNaN(EcmaTime.MakeDay(1970, double.NaN, 1)));
        Assert.True(double.IsNaN(EcmaTime.MakeDay(1e308, 1e308, 1)));
    }

    [Fact]
    public void MakeDate_and_MakeFullYear_follow_their_clauses()
    {
        Assert.Equal(EcmaTime.MsPerDay + 1, EcmaTime.MakeDate(1, 1));
        Assert.True(double.IsNaN(EcmaTime.MakeDate(1e308, 0)));
        Assert.True(double.IsNaN(EcmaTime.MakeDate(double.NaN, 0)));
        Assert.Equal(1900, EcmaTime.MakeFullYear(0));
        Assert.Equal(1999, EcmaTime.MakeFullYear(99.9));
        Assert.Equal(100, EcmaTime.MakeFullYear(100));
        Assert.Equal(1900, EcmaTime.MakeFullYear(-0.5));
        Assert.Equal(-1, EcmaTime.MakeFullYear(-1));
        Assert.Equal(double.PositiveInfinity, EcmaTime.MakeFullYear(double.PositiveInfinity));
        Assert.True(double.IsNaN(EcmaTime.MakeFullYear(double.NaN)));
    }

    // ---- the civil-day helpers ------------------------------------------------------------------

    [Fact]
    public void The_civil_days_agree_with_the_platform_calendar_from_year_1_to_9999()
    {
        var epoch = DateOnly.FromDateTime(Epoch);
        for (var number = DateOnly.MinValue.DayNumber; number <= DateOnly.MaxValue.DayNumber; number += 7)
        {
            var d = DateOnly.FromDayNumber(number);
            var days = d.DayNumber - epoch.DayNumber;
            Assert.Equal(days, IsoCalendarMath.EpochDays(d.Year, d.Month, d.Day));
            Assert.Equal((d.Year, d.Month, d.Day), IsoCalendarMath.FromEpochDays(days));
            Assert.Equal(d.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)d.DayOfWeek, IsoCalendarMath.DayOfWeek(d.Year, d.Month, d.Day));
            Assert.Equal(d.DayOfYear, IsoCalendarMath.DayOfYear(d.Year, d.Month, d.Day));
            var dt = d.ToDateTime(TimeOnly.MinValue);
            Assert.Equal((ISOWeek.GetWeekOfYear(dt), (long)ISOWeek.GetYear(dt)), IsoCalendarMath.WeekOfYear(d.Year, d.Month, d.Day));
            Assert.Equal(DateTime.DaysInMonth(d.Year, d.Month), IsoCalendarMath.DaysInMonth(d.Year, d.Month));
            Assert.Equal(days * EcmaTime.MsPerDay, Utc(d.Year, d.Month, d.Day));
        }
    }

    [Fact]
    public void The_weeks_of_negative_years_repeat_those_400_years_later()
    {
        // Correspondence row G2: -000002-12-28 is week 53 of -2, where Broiler.JS's truncating division
        // answered week 1 of -1.
        Assert.Equal((53, -2L), IsoCalendarMath.WeekOfYear(-2, 12, 28));
        Assert.Equal((53, -2L), IsoCalendarMath.WeekOfYear(-1, 1, 1));
        Assert.Equal((53, -8L), IsoCalendarMath.WeekOfYear(-8, 12, 28));
        for (long year = -271821; year < 275760; year += 397)
        {
            foreach (var (month, day) in new[] { (1, 1), (1, 4), (2, 28), (12, 28), (12, 31) })
            {
                var (week, weekYear) = IsoCalendarMath.WeekOfYear(year, month, day);
                Assert.Equal((week, weekYear + 400), IsoCalendarMath.WeekOfYear(year + 400, month, day));
                Assert.Equal(IsoCalendarMath.EpochDays(year, month, day) + 146097, IsoCalendarMath.EpochDays(year + 400, month, day));
                Assert.Equal((year, month, day), IsoCalendarMath.FromEpochDays(IsoCalendarMath.EpochDays(year, month, day)));
            }
        }
    }

    [Fact]
    public void BalanceYearMonth_and_IsValidIsoDate_follow_the_proposal()
    {
        Assert.Equal((2021L, 12), IsoCalendarMath.BalanceYearMonth(2021, 12));
        Assert.Equal((2022L, 1), IsoCalendarMath.BalanceYearMonth(2021, 13));
        Assert.Equal((2020L, 12), IsoCalendarMath.BalanceYearMonth(2021, 0));
        Assert.Equal((2018L, 11), IsoCalendarMath.BalanceYearMonth(2021, -25));
        Assert.Equal((750599937895082L, 8), IsoCalendarMath.BalanceYearMonth(0, 9007199254740992));
        Assert.True(IsoCalendarMath.IsValidIsoDate(2020, 2, 29));
        Assert.False(IsoCalendarMath.IsValidIsoDate(2021, 2, 29));
        // Correspondence row G3: the year given decides February's length, whatever its size.
        Assert.False(IsoCalendarMath.IsValidIsoDate(1000000100, 2, 29));
        Assert.True(IsoCalendarMath.IsValidIsoDate(1000000128, 2, 29));
        Assert.False(IsoCalendarMath.IsValidIsoDate(2021, 0, 1));
        Assert.False(IsoCalendarMath.IsValidIsoDate(2021, 13, 1));
        Assert.False(IsoCalendarMath.IsValidIsoDate(2021, 1, 0));
        Assert.False(IsoCalendarMath.IsValidIsoDate(2021, 1, 32));
        Assert.Throws<ArgumentOutOfRangeException>(() => IsoCalendarMath.DaysInMonth(2021, 13));
        Assert.Throws<ArgumentOutOfRangeException>(() => IsoCalendarMath.EpochDays(long.MinValue, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => IsoCalendarMath.FromEpochDays(long.MinValue));
    }

    // ---- local time -------------------------------------------------------------------------------

    [Fact]
    public void Utc_reads_a_skipped_local_time_at_the_offset_before_and_a_repeated_one_at_its_earlier_instant()
    {
        // Correspondence rows L2 and L3, New York: 2021-03-14 02:30 is skipped and reads as 07:30Z; 2021-11-07
        // 01:30 is repeated and reads as 05:30Z; 03:30 after the spring transition reads as 07:30Z.
        Assert.Equal(Utc(2021, 3, 14, 7, 30), EcmaLocalTime.Utc(Utc(2021, 3, 14, 2, 30), NewYork));
        Assert.Equal(Utc(2021, 11, 7, 5, 30), EcmaLocalTime.Utc(Utc(2021, 11, 7, 1, 30), NewYork));
        Assert.Equal(Utc(2021, 3, 14, 7, 30), EcmaLocalTime.Utc(Utc(2021, 3, 14, 3, 30), NewYork));
        Assert.Equal(Utc(2021, 7, 1, 4), EcmaLocalTime.Utc(Utc(2021, 7, 1), NewYork));
        Assert.Equal(Utc(2021, 7, 1, 8), EcmaLocalTime.LocalTime(Utc(2021, 7, 1, 12), NewYork));
        Assert.True(double.IsNaN(EcmaLocalTime.Utc(double.NaN, NewYork)));
        Assert.True(double.IsNaN(EcmaLocalTime.LocalTime(double.PositiveInfinity, NewYork)));
    }

    [Fact]
    public void A_local_time_read_back_is_itself_except_in_a_skipped_hour()
    {
        var draws = new SplitMix64(0x534A4C362D4C4F43);
        for (var i = 0; i < 20000; i++)
        {
            var t = Utc(1900, 1, 1) + (draws.Next() % (ulong)(Utc(2100, 1, 1) - Utc(1900, 1, 1)));
            var local = EcmaLocalTime.LocalTime(t, NewYork);
            var back = EcmaLocalTime.Utc(local, NewYork);
            // A UTC instant's local time names it, or names it and the instant one transition earlier.
            Assert.True(back == t || EcmaLocalTime.LocalTime(back, NewYork) == local, $"{t}");
            Assert.True(back <= t);
        }
    }

    [Fact]
    public void A_fixed_offset_zone_holds_its_offset_and_refuses_one_that_is_not_a_whole_millisecond_below_a_day()
    {
        var kolkata = new FixedOffsetLocalTimeProvider(19800000, null);
        Assert.Equal(19800000, EcmaLocalTime.LocalTime(0, kolkata));
        Assert.Equal(-19800000, EcmaLocalTime.Utc(0, kolkata));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FixedOffsetLocalTimeProvider(EcmaTime.MsPerDay, null));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FixedOffsetLocalTimeProvider(0.5, null));
        Assert.Throws<InvalidOperationException>(() => EcmaLocalTime.LocalTime(0, new BrokenZone()));
    }

    private sealed class BrokenZone : ILocalTimeProvider
    {
        public double GetOffsetMilliseconds(double epochMilliseconds) => 0.25;

        public string? GetName(double epochMilliseconds) => null;
    }

    // ---- the parser ---------------------------------------------------------------------------------

    public static TheoryData<string, double> FormatInstances => new()
    {
        { "2020", Utc(2020, 1, 1) },
        { "2020-02", Utc(2020, 2, 1) },
        { "2020-02-03", Utc(2020, 2, 3) },
        { "2020-02-03T04:05Z", Utc(2020, 2, 3, 4, 5) },
        { "2020-02-03T04:05:06Z", Utc(2020, 2, 3, 4, 5, 6) },
        { "2020-02-03T04:05:06.789Z", Utc(2020, 2, 3, 4, 5, 6, 789) },
        { "2020-02-03T04:05+01:30", Utc(2020, 2, 3, 2, 35) },
        { "2020-02-03T04:05-00:30", Utc(2020, 2, 3, 4, 35) },
        { "2020T04:05Z", Utc(2020, 1, 1, 4, 5) },
        { "+002020-02-03", Utc(2020, 2, 3) },
        { "-000001-01-01T00:00Z", Utc(-1, 1, 1) },
        { "+000000", Utc(0, 1, 1) },
        { "2020-02-03T24:00Z", Utc(2020, 2, 4) },
        { "2020-02-29", Utc(2020, 2, 29) },
        { "+275760-09-13T00:00:00.000Z", 8.64e15 },
        { "-271821-04-20T00:00:00.000Z", -8.64e15 },
    };

    [Theory]
    [MemberData(nameof(FormatInstances))]
    public void Both_dialects_read_every_omission_of_the_format_as_UTC(string text, double expected)
    {
        Assert.Equal(new EcmaParsedDate(EcmaParsedKind.Utc, expected), EcmaDateParser.Parse(text, DateParseDialect.Strict));
        Assert.Equal(new EcmaParsedDate(EcmaParsedKind.Utc, expected), EcmaDateParser.Parse(text, DateParseDialect.BroilerVM));
    }

    [Theory]
    [InlineData("2020-02-03T04:05")]
    [InlineData("2020-02-03T04:05:06.789")]
    [InlineData("2020T04:05")]
    [InlineData("+002020-02T00:00")]
    public void A_date_time_form_without_an_offset_is_local_time_and_unclipped(string text)
    {
        var parsed = EcmaDateParser.Parse(text, DateParseDialect.Strict);
        Assert.Equal(EcmaParsedKind.Local, parsed.Kind);
        Assert.Equal(parsed, EcmaDateParser.Parse(text, DateParseDialect.BroilerVM));
        Assert.Equal(new EcmaParsedDate(EcmaParsedKind.Local, 8.64e15 + 1), EcmaDateParser.Parse("+275760-09-13T00:00:00.001", DateParseDialect.Strict));
    }

    [Theory]
    [InlineData("-000000")]
    [InlineData("-000000-01-01T00:00Z")]
    [InlineData("2020-02-03T04:05:06.7Z")]
    [InlineData("2020-02-03T04:05:06.7890Z")]
    [InlineData("2020-02-03T04:05:06.Z")]
    [InlineData("2020-02-03T24:00:01Z")]
    [InlineData("2020-02-03T24:01Z")]
    [InlineData("2020-13-01")]
    [InlineData("2020-00-01")]
    [InlineData("2020-01-32")]
    [InlineData("2020-01-00")]
    [InlineData("2020-02-03T04")]
    [InlineData("2020-02-03T04:60Z")]
    [InlineData("2020-02-03T04:05:60Z")]
    [InlineData("2020-02-03 04:05Z")]
    [InlineData("2020-02-03T04:05+0100")]
    [InlineData("2020-02-03T04:05+24:00")]
    [InlineData("2020-02-03t04:05Z")]
    [InlineData("2020-02-03T04:05z")]
    [InlineData("2021-03-04Z")]
    [InlineData(" 2020-02-03")]
    [InlineData("2020-02-03 ")]
    [InlineData("+20200-02-03")]
    [InlineData("2020-2-3")]
    [InlineData("+275760-09-13T00:00:00.001Z")]
    [InlineData("-271821-04-19T23:59:59.999Z")]
    public void What_is_not_an_instance_of_the_format_is_refused_by_both_dialects(string text)
    {
        Assert.Equal(EcmaParsedKind.Invalid, EcmaDateParser.Parse(text, DateParseDialect.Strict).Kind);
        Assert.True(double.IsNaN(EcmaDateParser.Parse(text, DateParseDialect.Strict).Value));
        Assert.Equal(EcmaParsedKind.Invalid, EcmaDateParser.Parse(text, DateParseDialect.BroilerVM).Kind);
    }

    [Fact]
    public void A_day_past_its_month_s_length_is_refused_strictly_and_carried_in_Broiler_VM_s_dialect()
    {
        // Correspondence row P5.
        Assert.Equal(EcmaParsedKind.Invalid, EcmaDateParser.Parse("2021-02-30", DateParseDialect.Strict).Kind);
        Assert.Equal(EcmaParsedKind.Invalid, EcmaDateParser.Parse("2021-04-31", DateParseDialect.Strict).Kind);
        Assert.Equal(new EcmaParsedDate(EcmaParsedKind.Utc, Utc(2021, 3, 2)), EcmaDateParser.Parse("2021-02-30", DateParseDialect.BroilerVM));
        Assert.Equal(new EcmaParsedDate(EcmaParsedKind.Utc, Utc(2021, 5, 1)), EcmaDateParser.Parse("2021-04-31", DateParseDialect.BroilerVM));
    }

    [Fact]
    public void Broiler_VM_s_dialect_reads_its_own_renderings_back_negative_years_included()
    {
        var utc = new FixedOffsetLocalTimeProvider(0, "Coordinated Universal Time");
        foreach (var tv in new[] { 0, -1, 8.64e15, -8.64e15, Utc(-1, 1, 1), Utc(-1, 7, 15, 13, 45, 30), Utc(0, 1, 1), Utc(10000, 1, 1), Utc(275759, 7, 15, 13, 45, 30) })
        {
            var seconds = tv - EcmaTime.MsFromTime(tv);
            Assert.Equal(new EcmaParsedDate(EcmaParsedKind.Utc, seconds), EcmaDateParser.Parse(EcmaDateFormatter.ToUtcString(tv), DateParseDialect.BroilerVM));
            Assert.Equal(new EcmaParsedDate(EcmaParsedKind.Utc, seconds), EcmaDateParser.Parse(EcmaDateFormatter.ToDateString(tv, utc), DateParseDialect.BroilerVM));
            Assert.True(EcmaDateFormatter.TryToIsoString(tv, out var iso));
            Assert.Equal(new EcmaParsedDate(EcmaParsedKind.Utc, tv), EcmaDateParser.Parse(iso, DateParseDialect.BroilerVM));
            Assert.Equal(new EcmaParsedDate(EcmaParsedKind.Utc, tv), EcmaDateParser.Parse(iso, DateParseDialect.Strict));
        }

        // Correspondence row P9: the renderings of year -1 read back. Before the dialect they gave NaN.
        Assert.Equal(new EcmaParsedDate(EcmaParsedKind.Utc, Utc(-1, 1, 1)), EcmaDateParser.Parse("Fri, 01 Jan -0001 00:00:00 GMT", DateParseDialect.BroilerVM));
    }

    [Fact]
    public void Broiler_VM_s_textual_reading_keeps_the_engine_s_quirks()
    {
        // Correspondence row P7: a two-digit year after the day is literal, an offset's hours and minutes
        // are not bounded, a weekday is not checked, and a date without a zone is local time.
        Assert.Equal(new EcmaParsedDate(EcmaParsedKind.Local, EcmaTime.MakeDate(EcmaTime.MakeDay(70, 0, 1), 0)), EcmaDateParser.Parse("Jan 1 70", DateParseDialect.BroilerVM));
        Assert.Equal(new EcmaParsedDate(EcmaParsedKind.Utc, -((99 * 60) + 99) * EcmaTime.MsPerMinute), EcmaDateParser.Parse("Thu Jan 01 1970 00:00:00 GMT+9999", DateParseDialect.BroilerVM));
        Assert.Equal(new EcmaParsedDate(EcmaParsedKind.Utc, 0), EcmaDateParser.Parse("Fri Jan 01 1970 00:00:00 GMT+0000", DateParseDialect.BroilerVM));
        Assert.Equal(new EcmaParsedDate(EcmaParsedKind.Local, Utc(2021, 3, 2)), EcmaDateParser.Parse("Feb 30 2021", DateParseDialect.BroilerVM));
        Assert.Equal(new EcmaParsedDate(EcmaParsedKind.Local, Utc(1970, 1, 1)), EcmaDateParser.Parse("Thu Jan 01 1970", DateParseDialect.BroilerVM));
        Assert.Equal(new EcmaParsedDate(EcmaParsedKind.Local, Utc(1970, 1, 2, 0, 30)), EcmaDateParser.Parse("1 Jan 1970 24:30", DateParseDialect.BroilerVM));
        Assert.Equal(EcmaParsedKind.Invalid, EcmaDateParser.Parse("1 Jan 1970 24:60", DateParseDialect.BroilerVM).Kind);
        Assert.Equal(EcmaParsedKind.Invalid, EcmaDateParser.Parse("1970 Jan 01", DateParseDialect.BroilerVM).Kind);
        Assert.Equal(EcmaParsedKind.Invalid, EcmaDateParser.Parse("03/04/2021 10:00", DateParseDialect.BroilerVM).Kind);
        Assert.Equal(EcmaParsedKind.Invalid, EcmaDateParser.Parse("1:2:3:4 Jan 1 1970", DateParseDialect.BroilerVM).Kind);
        Assert.Equal(EcmaParsedKind.Invalid, EcmaDateParser.Parse("Jan 1 1970 GMT+05:30", DateParseDialect.BroilerVM).Kind);
        Assert.Equal(EcmaParsedKind.Invalid, EcmaDateParser.Parse(string.Empty, DateParseDialect.BroilerVM).Kind);
        Assert.Equal(new EcmaParsedDate(EcmaParsedKind.Utc, Utc(1970, 1, 1)), EcmaDateParser.Parse("Thu, 01 Jan 1970 00:00:00 UTC (anything after)", DateParseDialect.BroilerVM));
        Assert.Equal(EcmaParsedKind.Invalid, EcmaDateParser.Parse("Thu Jan 01 1970", DateParseDialect.Strict).Kind);
    }

    [Fact]
    public void The_meter_is_charged_the_text_s_length_before_reading_and_nothing_for_an_empty_text()
    {
        var meter = new Meter();
        EcmaDateParser.Parse("2020-01-01", DateParseDialect.Strict, meter.Charge);
        EcmaDateParser.Parse("nonsense, refused", DateParseDialect.BroilerVM, meter.Charge);
        EcmaDateParser.Parse(string.Empty, DateParseDialect.BroilerVM, meter.Charge);
        Assert.Equal([10UL, 17UL], meter.Charges);
        Assert.Throws<ArgumentOutOfRangeException>(() => EcmaDateParser.Parse("2020", (DateParseDialect)2));
    }

    // ---- the renderings -----------------------------------------------------------------------------

    [Fact]
    public void The_renderings_write_the_specification_s_text_at_the_range_ends_and_for_negative_years()
    {
        var utc = new FixedOffsetLocalTimeProvider(0, "Coordinated Universal Time");
        Assert.Equal("Thu, 01 Jan 1970 00:00:00 GMT", EcmaDateFormatter.ToUtcString(0));
        Assert.Equal("Sat, 13 Sep 275760 00:00:00 GMT", EcmaDateFormatter.ToUtcString(8.64e15));
        Assert.Equal("Tue, 20 Apr -271821 00:00:00 GMT", EcmaDateFormatter.ToUtcString(-8.64e15));
        Assert.Equal("Fri, 01 Jan -0001 00:00:00 GMT", EcmaDateFormatter.ToUtcString(Utc(-1, 1, 1)));
        Assert.Equal("Sat, 01 Jan 0000 00:00:00 GMT", EcmaDateFormatter.ToUtcString(Utc(0, 1, 1)));
        Assert.Equal("Thu Jan 01 1970 00:00:00 GMT+0000 (Coordinated Universal Time)", EcmaDateFormatter.ToDateString(0, utc));
        Assert.Equal("Fri Jan 01 -0001", EcmaDateFormatter.DateString(Utc(-1, 1, 1)));
        Assert.Equal("23:59:59 GMT", EcmaDateFormatter.TimeString(-1));
        Assert.Equal(EcmaDateFormatter.InvalidDate, EcmaDateFormatter.ToUtcString(double.NaN));
        Assert.Equal(EcmaDateFormatter.InvalidDate, EcmaDateFormatter.ToDateString(double.NaN, utc));
        Assert.True(EcmaDateFormatter.TryToIsoString(8.64e15, out var iso));
        Assert.Equal("+275760-09-13T00:00:00.000Z", iso);
        Assert.True(EcmaDateFormatter.TryToIsoString(-8.64e15, out iso));
        Assert.Equal("-271821-04-20T00:00:00.000Z", iso);
        Assert.True(EcmaDateFormatter.TryToIsoString(Utc(-1, 1, 1), out iso));
        Assert.Equal("-000001-01-01T00:00:00.000Z", iso);
        Assert.True(EcmaDateFormatter.TryToIsoString(Utc(10000, 1, 1), out iso));
        Assert.Equal("+010000-01-01T00:00:00.000Z", iso);
        Assert.False(EcmaDateFormatter.TryToIsoString(double.NaN, out iso));
        Assert.Equal(string.Empty, iso);
    }

    [Fact]
    public void TimeZoneString_writes_the_offset_s_sign_hours_and_minutes_and_the_zone_s_name()
    {
        // Correspondence row R3: the sign is U+002D whatever the culture; R4: the name is the provider's.
        Assert.Equal("-0500", EcmaDateFormatter.TimeZoneString(0, new FixedOffsetLocalTimeProvider(-18000000, null)));
        Assert.Equal("+0530 (IST)", EcmaDateFormatter.TimeZoneString(0, new FixedOffsetLocalTimeProvider(19800000, "IST")));
        Assert.Equal("-0456 (LMT)", EcmaDateFormatter.TimeZoneString(0, new FixedOffsetLocalTimeProvider(-17762000, "LMT")));
        Assert.Equal("+2359 (Zeit ä)", EcmaDateFormatter.TimeZoneString(0, new FixedOffsetLocalTimeProvider(86340000, "Zeit ä")));
        Assert.Equal("-0001", EcmaDateFormatter.TimeZoneString(0, new FixedOffsetLocalTimeProvider(-60000, null)));
        Assert.Equal("Wed Jun 30 2021 20:00:00 GMT-0400 (America/New_York)", EcmaDateFormatter.ToDateString(Utc(2021, 7, 1), NewYork));
    }

    [Fact]
    public void The_renderings_agree_with_the_platform_s_invariant_text_from_year_1_to_9999()
    {
        var draws = new SplitMix64(0x534A4C362D524E44);
        for (var i = 0; i < 5000; i++)
        {
            var tv = (double)(-62135596800000 + (long)(draws.Next() % 315537897600000UL));
            var when = Epoch.AddMilliseconds(tv);
            Assert.Equal(when.ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'", CultureInfo.InvariantCulture), EcmaDateFormatter.ToUtcString(tv));
            Assert.True(EcmaDateFormatter.TryToIsoString(tv, out var iso));
            Assert.Equal(when.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture), iso);
            Assert.Equal(when.ToString("ddd MMM dd yyyy", CultureInfo.InvariantCulture), EcmaDateFormatter.DateString(tv));
        }

        Assert.Throws<ArgumentOutOfRangeException>(() => EcmaDateFormatter.DateString(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => EcmaDateFormatter.ToUtcString(double.PositiveInfinity));
        Assert.Throws<ArgumentNullException>(() => EcmaDateFormatter.TimeZoneString(0, null!));
    }
}
