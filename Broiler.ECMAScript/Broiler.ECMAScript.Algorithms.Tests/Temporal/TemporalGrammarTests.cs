using System;
using System.Collections.Generic;
using System.Numerics;
using Broiler.ECMAScript.Temporal;
using Broiler.ECMAScript.Time;
using Xunit;
using static Broiler.ECMAScript.Algorithms.Tests.TemporalLimitsTests;

namespace Broiler.ECMAScript.Algorithms.Tests;

/// <summary>
/// The Temporal grammar (milestone SJL-10.3, part A): the rows G1 to G24 of bundle sjl-10-001's correspondence, each
/// accepted or refused as Broiler.VM's grammar does (its run files, <c>correspondence-runs/syntax/g*.vm.txt</c>). Each
/// engine's <c>from()</c> parses with one goal: <c>PlainDate</c> and <c>PlainDateTime</c>'s is
/// <see cref="TemporalGoal.DateTime"/>, <c>relativeTo</c>'s the zoned goal then that one.
/// </summary>
public class TemporalGrammarTests
{
    private static readonly TemporalGoal[] Plain = [TemporalGoal.DateTime, TemporalGoal.Time, TemporalGoal.YearMonth, TemporalGoal.MonthDay];

    private static readonly TemporalGoal[] Every =
        [TemporalGoal.DateTime, TemporalGoal.Time, TemporalGoal.YearMonth, TemporalGoal.MonthDay, TemporalGoal.ZonedDateTime, TemporalGoal.Instant];

    private static readonly TemporalGoal[] RelativeTo = [TemporalGoal.ZonedDateTime, TemporalGoal.DateTime];

    private static IsoParseResult Accept(string text, params TemporalGoal[] goals)
    {
        Assert.True(TemporalGrammar.TryParse(text, goals, out var result, out var error), text + " was refused: " + error);
        Assert.Equal(TemporalParseError.None, error);
        return result;
    }

    private static TemporalParseError Refuse(string text, params TemporalGoal[] goals)
    {
        Assert.False(TemporalGrammar.TryParse(text, goals, out var result, out var error), text + " was accepted");
        Assert.Null(result);
        Assert.NotEqual(TemporalParseError.None, error);
        return error;
    }

    private static void RefusedByEach(string text, IEnumerable<TemporalGoal> goals)
    {
        foreach (var goal in goals)
            Refuse(text, goal);
    }

    // ---- G1, G2: years and date separators -----------------------------------------------------------------

    [Theory]
    [InlineData("2020-01-01", 2020)]
    [InlineData("+002020-01-01", 2020)]
    [InlineData("-002020-01-01", -2020)]
    [InlineData("+000000-01-01", 0)]
    [InlineData("0000-01-01", 0)]
    [InlineData("-000001-12-31", -1)]
    [InlineData("+999999-12-31", 999999)]
    [InlineData("-999999-01-01", -999999)]
    [InlineData("20200101", 2020)]
    [InlineData("+0020200101", 2020)]
    public void G1_G2_a_year_is_four_digits_or_a_sign_and_six_with_both_separators_or_neither(string text, long year)
    {
        var date = Accept(text, TemporalGoal.DateTime);
        Assert.Equal(year, date.Year);
        Assert.True(date.HasDay);
        Assert.Null(date.Time);
        Assert.Equal(TemporalGoal.DateTime, date.Goal);
    }

    [Theory]
    [InlineData("-000000-01-01")]
    [InlineData("-0000000101")]
    [InlineData("+0020-01-01")]
    [InlineData("02020-01-01")]
    [InlineData("020-01-01")]
    [InlineData("2020-1-01")]
    [InlineData("2020-01-1")]
    [InlineData("2020-0101")]
    [InlineData("202001-01")]
    [InlineData(" 2020-01-01")]
    [InlineData("2020-01-01 ")]
    [InlineData("2020-01-01\u0000")]
    [InlineData("")]
    public void G1_G2_other_years_and_mixed_separators_are_refused_by_every_date_goal(string text)
    {
        foreach (var goal in new[] { TemporalGoal.DateTime, TemporalGoal.YearMonth, TemporalGoal.MonthDay })
            Assert.Equal(TemporalParseError.Malformed, Refuse(text, goal));
        Refuse(text + "[UTC]", TemporalGoal.ZonedDateTime);
        Refuse(text + "T00:00Z", TemporalGoal.Instant);
    }

    [Fact]
    public void G1_the_limits_parse_and_the_record_has_no_range()
    {
        Assert.Equal(-271821, Accept("-271821-04-18", TemporalGoal.DateTime).Year);
        Assert.Equal(275760, Accept("+275760-09-14", TemporalGoal.DateTime).Year);

        // A month-day reads the date's month and day, and keeps its year for another calendar.
        var monthDay = Accept("+999999-12-31", TemporalGoal.MonthDay);
        Assert.Equal((999999L, 12, 31), (monthDay.Year!.Value, monthDay.Month, monthDay.Day));
    }

    [Fact]
    public void G2_a_date_with_mixed_separators_can_still_be_a_time()
    {
        // PlainTime.from("2020-0101") is 20:20:00 with the offset -01:01, and "202001-01" is 20:20:01 with -01.
        var time = Accept("2020-0101", TemporalGoal.Time);
        Assert.Equal(T(20, 20), time.Time);
        Assert.Equal("-0101", time.Offset);
        Assert.Equal(T(20, 20, 1), Accept("202001-01", TemporalGoal.Time).Time);
    }

    // ---- G3: a year-month whose day is not a date ---------------------------------------------------------

    [Theory]
    [InlineData("2020-02-30")]
    [InlineData("2020-01-00")]
    [InlineData("2020-01-32")]
    [InlineData("2021-02-29")]
    [InlineData("2020-04-31")]
    [InlineData("1900-02-29")]
    [InlineData("2020-02-30[u-ca=gregory]")]
    public void G3_a_year_month_string_with_a_day_that_is_not_a_date_is_refused(string text) =>
        Assert.Equal(TemporalParseError.Malformed, Refuse(text, TemporalGoal.YearMonth));

    [Fact]
    public void G3_a_year_month_string_with_a_valid_day_reads_the_day()
    {
        var yearMonth = Accept("2020-02-29", TemporalGoal.YearMonth);
        Assert.Equal((2020L, 2, 29, true), (yearMonth.Year!.Value, yearMonth.Month, yearMonth.Day, yearMonth.HasDay));
        var bare = Accept("2020-02", TemporalGoal.YearMonth);
        Assert.Equal((2020L, 2, 1, false), (bare.Year!.Value, bare.Month, bare.Day, bare.HasDay));
    }

    // ---- G4: a time from a date-time whose date is not a date ----------------------------------------------

    [Theory]
    [InlineData("2021-13-45T12:00")]
    [InlineData("2020-13-01T12:00")]
    [InlineData("2020-02-30T12:00")]
    [InlineData("2021-02-29T12:00")]
    [InlineData("2020-00-01T12:00")]
    [InlineData("2020-01-32T12:00")]
    [InlineData("-000000-01-01T12:00")]
    [InlineData("2020-0101T12:00")]
    public void G4_a_time_string_with_a_date_that_is_not_a_date_is_refused(string text) =>
        Assert.Equal(TemporalParseError.Malformed, Refuse(text, TemporalGoal.Time));

    // ---- G5: times ---------------------------------------------------------------------------------------

    [Theory]
    [InlineData("12", 12, 0, 0, 0)]
    [InlineData("T12", 12, 0, 0, 0)]
    [InlineData("t12:00", 12, 0, 0, 0)]
    [InlineData("1230", -1, 0, 0, 0)] // also a month-day: refused (G7)
    [InlineData("T1230", 12, 30, 0, 0)]
    [InlineData("12:30:45", 12, 30, 45, 0)]
    [InlineData("123045", 12, 30, 45, 0)]
    [InlineData("12:30:45,5", 12, 30, 45, 500_000_000)]
    [InlineData("12:30:45.1", 12, 30, 45, 100_000_000)]
    [InlineData("12:30:45.123456789", 12, 30, 45, 123_456_789)]
    [InlineData("12:30:60", 12, 30, 59, 0)]
    [InlineData("23:59:60.999999999", 23, 59, 59, 999_999_999)]
    public void G5_a_time_alone_is_the_grammars(string text, int hour, int minute, int second, int fraction)
    {
        if (hour < 0)
        {
            Refuse(text, TemporalGoal.Time);
            return;
        }

        Assert.Equal(T(hour, minute, second, fraction / 1_000_000, fraction / 1000 % 1000, fraction % 1000), Accept(text, TemporalGoal.Time).Time);
    }

    [Theory]
    [InlineData("12:3045")]
    [InlineData("1230:45")]
    [InlineData("24:00")]
    [InlineData("12:60")]
    [InlineData("12:30:61")]
    [InlineData("12:30.5")]
    [InlineData("12.5")]
    [InlineData("12:30:45.1234567890")]
    [InlineData("12:30:45.")]
    [InlineData("T")]
    [InlineData("TT12:00")]
    [InlineData("1")]
    public void G5_times_off_the_grammar_are_refused(string text)
    {
        Refuse(text, TemporalGoal.Time);
        Refuse("2020-01-01T" + (text.StartsWith('T') ? text[1..] : text), TemporalGoal.DateTime);
    }

    [Theory]
    [InlineData("2020-01-01T12:30")]
    [InlineData("2020-01-01t12:30")]
    [InlineData("2020-01-01 12:30")]
    [InlineData("20200101T1230")]
    public void G5_a_time_after_a_date_takes_t_or_a_space(string text)
    {
        foreach (var goal in Plain)
            Assert.Equal(T(12, 30), Accept(text, goal).Time);
        Assert.Equal(T(12, 30), Accept(text + "[UTC]", TemporalGoal.ZonedDateTime).Time);
        Assert.Equal(T(12, 30), Accept(text + "Z", TemporalGoal.Instant).Time);
        Refuse("2020-01-01TT12:30", TemporalGoal.DateTime);
    }

    // ---- G6: one form per time --------------------------------------------------------------------------

    [Theory]
    [InlineData("2020-01-01T12:3045Z")]
    [InlineData("2020-01-01T1230:45Z")]
    public void G6_an_instant_with_mixed_time_separators_is_refused(string text)
    {
        Assert.Equal(TemporalParseError.Malformed, Refuse(text, TemporalGoal.Instant));
        Assert.False(TemporalGrammar.TryParseInstant(text, out _, out var error));
        Assert.Equal(TemporalParseError.Malformed, error);
        Assert.True(TemporalGrammar.TryParseInstant("2020-01-01T12:30:45Z", out _, out _));
    }

    // ---- G7: designator-less times that are also month-days or year-months -----------------------------------

    [Theory]
    [InlineData("2021-12")]
    [InlineData("202112")]
    [InlineData("1214")]
    [InlineData("0229")]
    [InlineData("1230")]
    [InlineData("--1214")]
    [InlineData("1214Z")]
    public void G7_a_time_that_is_also_a_month_day_or_year_month_is_refused(string text) =>
        Refuse(text, TemporalGoal.Time);

    [Theory]
    [InlineData("0230", 2, 30, null)]
    [InlineData("1232", 12, 32, null)]
    [InlineData("2021-13", 20, 21, "-13")]
    [InlineData("2020-0101", 20, 20, "-0101")]
    [InlineData("T1214", 12, 14, null)]
    [InlineData("1214[UTC]", -1, 0, null)]
    public void G7_a_time_that_is_neither_is_accepted(string text, int hour, int minute, string? offset)
    {
        if (hour < 0)
        {
            Refuse(text, TemporalGoal.Time);
            return;
        }

        var time = Accept(text, TemporalGoal.Time);
        Assert.Equal(T(hour, minute), time.Time);
        Assert.Equal(offset, time.Offset);
    }

    // ---- G8: offsets after a time --------------------------------------------------------------------------

    [Theory]
    [InlineData("Z", true, 0)]
    [InlineData("z", true, 0)]
    [InlineData("+00:00", false, 0)]
    [InlineData("-00", false, 0)]
    [InlineData("+01", false, 3_600_000_000_000)]
    [InlineData("+0100", false, 3_600_000_000_000)]
    [InlineData("+01:00:00", false, 3_600_000_000_000)]
    [InlineData("+01:00:00.5", false, 3_600_500_000_000)]
    [InlineData("+01:00:00,5", false, 3_600_500_000_000)]
    [InlineData("-01:00:00.123456789", false, -3_600_123_456_789)]
    [InlineData("+235959", false, 86_399_000_000_000)]
    public void G8_an_offset_after_a_time_is_the_grammars_and_z_only_where_allowed(string offset, bool z, long nanoseconds)
    {
        var text = "2020-01-01T12:00" + offset;
        if (z)
        {
            foreach (var goal in Plain)
                Refuse(text, goal);
            Assert.True(Accept(text, TemporalGoal.Instant).Z);
            Assert.True(Accept(text + "[UTC]", TemporalGoal.ZonedDateTime).Z);
            return;
        }

        foreach (var goal in Every)
        {
            var parsed = Accept(goal == TemporalGoal.ZonedDateTime ? text + "[UTC]" : text, goal);
            Assert.False(parsed.Z);
            Assert.Equal(offset, parsed.Offset);
            Assert.Equal(nanoseconds, parsed.OffsetNanoseconds);
            Assert.Equal(offset.Length > 6, parsed.OffsetHasSeconds);
        }
    }

    [Theory]
    [InlineData("+24:00")]
    [InlineData("+23:60")]
    [InlineData("+23:59:60")]
    [InlineData("+01:0000")]
    [InlineData("+0100:00")]
    [InlineData("+1")]
    [InlineData("+001")]
    [InlineData("+01:00Z")]
    [InlineData("+01:00:00.1234567890")]
    public void G8_an_offset_out_of_range_or_off_the_grammar_is_refused_in_every_goal(string offset)
    {
        foreach (var goal in Every)
            Refuse("2020-01-01T12:00" + offset + (goal == TemporalGoal.ZonedDateTime ? "[UTC]" : string.Empty), goal);
        Refuse("12:00" + offset, TemporalGoal.Time);
    }

    // ---- G9, G10: digits other than ASCII, and U+2212 --------------------------------------------------------

    [Theory]
    [InlineData("٢٠٢٠-01-01")] // Arabic-Indic year
    [InlineData("２０２０-01-01")] // fullwidth year
    [InlineData("2020-01-0１")]
    [InlineData("2020-01-01T12:3٠")]
    [InlineData("2020-01-01T12:30:45.٥")]
    [InlineData("2020-01-01T12:00+٠١:٠٠")]
    [InlineData("−002020-01-01")]
    [InlineData("2020-01-01T12:00−01:00")]
    [InlineData("2020-01-01T12:00[−01:00]")]
    [InlineData("2020-01-01[u-ca=١]")]
    public void G9_G10_a_digit_other_than_ascii_or_a_minus_sign_is_refused_everywhere(string text)
    {
        foreach (var goal in Every)
            Refuse(text, goal);
        Refuse(text, RelativeTo);
    }

    [Theory]
    [InlineData("P١Y")]
    [InlineData("PT١H")]
    [InlineData("−P1D")]
    [InlineData("−PT1H")]
    public void G9_G10_a_duration_with_such_a_digit_or_sign_is_refused(string text) =>
        Assert.False(TemporalGrammar.TryParseDuration(text, out _, out _));

    [Theory]
    [InlineData("+٠١:٠٠")]
    [InlineData("−01:00")]
    public void G9_G10_an_offset_string_with_such_a_digit_or_sign_is_refused(string text)
    {
        Assert.False(TemporalGrammar.TryParseOffsetString(text, out _));
        Assert.False(TemporalGrammar.IsTimeZoneIdentifier(text));
    }

    // ---- G11 to G14: annotations -------------------------------------------------------------------------

    [Theory]
    [InlineData("[]")]
    [InlineData("[!]")]
    public void G11_an_empty_or_flag_only_annotation_is_refused_in_each_goal(string annotation)
    {
        foreach (var goal in Plain)
            Refuse((goal == TemporalGoal.Time ? "12:00" : "2020-01-01T12:00") + annotation, goal);
        Refuse("2020-01-01T12:00[UTC]" + annotation, TemporalGoal.ZonedDateTime);
        Refuse("2020-01-01T12:00Z" + annotation, TemporalGoal.Instant);
        Refuse("2020-01-01" + annotation, TemporalGoal.DateTime);
        Refuse("2020-01-01" + annotation, RelativeTo);
    }

    [Theory]
    [InlineData("[Mars Olympus]")]
    [InlineData("[ UTC]")]
    [InlineData("[UTC ]")]
    [InlineData("[/a]")]
    [InlineData("[a/]")]
    [InlineData("[a//b]")]
    [InlineData("[9a]")]
    [InlineData("[+24:00]")]
    [InlineData("[+01:00:00]")]
    [InlineData("[-07:00:01]")]
    public void G12_a_time_zone_annotation_the_grammar_refuses_is_refused(string annotation)
    {
        foreach (var goal in Plain)
            Refuse((goal == TemporalGoal.Time ? "12:00" : "2020-01-01T12:00") + annotation, goal);
        Refuse("2020-01-01T12:00Z" + annotation, TemporalGoal.Instant);
        Refuse("2020-01-01T12:00" + annotation, TemporalGoal.ZonedDateTime);
    }

    [Theory]
    [InlineData("[Mars/Olympus]", "Mars/Olympus")]
    [InlineData("[.]", ".")]
    [InlineData("[..]", "..")]
    [InlineData("[a..b]", "a..b")]
    [InlineData("[Etc/GMT+5]", "Etc/GMT+5")]
    [InlineData("[a-b+c_d.e]", "a-b+c_d.e")]
    [InlineData("[+01:00]", "+01:00")]
    [InlineData("[+0100]", "+0100")]
    [InlineData("[!UTC]", "UTC")]
    public void G12_a_name_the_grammar_admits_is_accepted_whether_or_not_a_zone_has_it(string annotation, string zone) =>
        Assert.Equal(zone, Accept("2020-01-01T12:00" + annotation, TemporalGoal.DateTime).TimeZone);

    [Theory]
    [InlineData("[u-ca=iso8601][UTC]")]
    [InlineData("[foo=bar][UTC]")]
    [InlineData("[UTC][UTC]")]
    public void G13_a_time_zone_annotation_after_another_annotation_is_refused(string annotations)
    {
        foreach (var goal in Plain)
            Refuse((goal == TemporalGoal.Time ? "12:00" : "2020-01-01T12:00") + annotations, goal);
        Refuse("2020-01-01T00:00" + annotations, TemporalGoal.ZonedDateTime);
        Refuse("2020-01-01T00:00Z" + annotations, TemporalGoal.Instant);
        Refuse("2020-01-01T00:00" + annotations, RelativeTo);
    }

    [Fact]
    public void G14_the_first_calendar_wins_and_a_critical_one_among_several_or_an_unknown_critical_key_is_refused()
    {
        var critical = Accept("2020-01-01[!u-ca=gregory]", TemporalGoal.DateTime);
        Assert.Equal("gregory", critical.Calendar);
        Assert.Equal(new[] { new TemporalAnnotation(true, "u-ca", "gregory") }, critical.Annotations);

        var two = Accept("2020-01-01[u-ca=gregory][u-ca=japanese]", TemporalGoal.DateTime);
        Assert.Equal("gregory", two.Calendar);
        Assert.Equal(2, two.Annotations.Count);
        Assert.Equal("japanese", Accept("2020-01-01[u-ca=japanese][u-ca=gregory]", TemporalGoal.DateTime).Calendar);

        Assert.Equal(TemporalParseError.CriticalCalendarConflict, Refuse("2020-01-01[u-ca=gregory][!u-ca=japanese]", TemporalGoal.DateTime));
        Assert.Equal(TemporalParseError.CriticalCalendarConflict, Refuse("2020-01-01[!u-ca=gregory][u-ca=japanese]", TemporalGoal.DateTime));
        Assert.Equal(TemporalParseError.CriticalCalendarConflict, Refuse("2020-01-01[u-ca=iso8601][!u-ca=iso8601]", TemporalGoal.DateTime));
        Assert.Equal(TemporalParseError.UnknownCriticalAnnotation, Refuse("2020-01-01[!foo=bar]", TemporalGoal.DateTime));
        Assert.Equal(TemporalParseError.UnknownCriticalAnnotation, Refuse("2020-01-01[!foo=bar][u-ca=iso8601]", TemporalGoal.DateTime));

        var unknown = Accept("2020-01-01[UTC][foo=bar][u-ca=hebrew]", TemporalGoal.DateTime);
        Assert.Equal(("UTC", "hebrew"), (unknown.TimeZone, unknown.Calendar));
        Assert.Equal(new[] { new TemporalAnnotation(false, "foo", "bar"), new TemporalAnnotation(false, "u-ca", "hebrew") }, unknown.Annotations);
        Assert.Equal("UTC", Accept("2020-01-01T12:00[!UTC]", TemporalGoal.DateTime).TimeZone);
    }

    [Theory]
    [InlineData("[U-CA=iso8601]")]
    [InlineData("[u-ca=iso_8601]")]
    [InlineData("[u-ca=]")]
    [InlineData("[=iso8601]")]
    [InlineData("[1a=b]")]
    [InlineData("[-a=b]")]
    [InlineData("[aBc=d]")]
    [InlineData("[a=b--c]")]
    [InlineData("[a=-b]")]
    [InlineData("[a=b-]")]
    [InlineData("[a=b_c]")]
    [InlineData("[a=b=c]")]
    [InlineData("[u-ca=iso8601")]
    [InlineData("[u-ca=iso8601]]")]
    [InlineData("[[u-ca=iso8601]")]
    [InlineData("[u-ca=gregory] ")]
    public void G14_a_key_or_value_off_the_grammar_is_refused(string annotation) =>
        Assert.Equal(TemporalParseError.Malformed, Refuse("2020-01-01T12:00" + annotation, TemporalGoal.DateTime));

    [Theory]
    [InlineData("[a=b]")]
    [InlineData("[_x=y]")]
    [InlineData("[x-y=z]")]
    [InlineData("[a=b-c]")]
    [InlineData("[u-ca=ISO8601]")]
    public void G14_a_key_and_value_of_the_grammar_are_accepted(string annotation) =>
        Assert.Single(Accept("2020-01-01T12:00" + annotation, TemporalGoal.DateTime).Annotations);

    // ---- G15: goal by goal --------------------------------------------------------------------------------

    [Theory]
    [InlineData("2020-01", 2020, 1)]
    [InlineData("202001", 2020, 1)]
    [InlineData("+00202001", 2020, 1)]
    [InlineData("+002020-01", 2020, 1)]
    [InlineData("-00202001", -2020, 1)]
    [InlineData("2020-01[u-ca=iso8601]", 2020, 1)]
    [InlineData("2020-01[!u-ca=iso8601]", 2020, 1)]
    [InlineData("2020-01[u-ca=ISO8601]", 2020, 1)]
    [InlineData("2020-01[UTC]", 2020, 1)]
    [InlineData("2020-01[foo=bar]", 2020, 1)]
    [InlineData("+275760-10", 275760, 10)]
    public void G15_a_year_month_string(string text, long year, int month)
    {
        var yearMonth = Accept(text, TemporalGoal.YearMonth);
        Assert.Equal((year, month, false), (yearMonth.Year!.Value, yearMonth.Month, yearMonth.HasDay));
    }

    [Theory]
    [InlineData("2020-1")]
    [InlineData("2020-001")]
    [InlineData("2020-00")]
    [InlineData("2020-13")]
    [InlineData("2020-01Z")]
    public void G15_a_year_month_string_off_the_grammar(string text) => Refuse(text, TemporalGoal.YearMonth);

    [Theory]
    [InlineData("2020-01[u-ca=gregory]")]
    [InlineData("202001[u-ca=gregory]")]
    [InlineData("2020-01[u-ca=chinese]")]
    public void G15_a_year_month_without_a_day_must_be_iso_8601(string text)
    {
        Assert.Equal(TemporalParseError.YearMonthOutsideIsoCalendar, Refuse(text, TemporalGoal.YearMonth));
        Assert.Equal("gregory", Accept("2020-01-01[u-ca=gregory]", TemporalGoal.YearMonth).Calendar);
    }

    [Theory]
    [InlineData("01-01", 1, 1)]
    [InlineData("0101", 1, 1)]
    [InlineData("--01-01", 1, 1)]
    [InlineData("--0101", 1, 1)]
    [InlineData("02-29", 2, 29)]
    [InlineData("--01-01[u-ca=iso8601]", 1, 1)]
    [InlineData("11-18[+01:00][u-ca=ISO8601]", 11, 18)]
    [InlineData("01-01[!u-ca=iso8601]", 1, 1)]
    [InlineData("01-01[UTC]", 1, 1)]
    public void G15_a_month_day_string(string text, int month, int day)
    {
        var monthDay = Accept(text, TemporalGoal.MonthDay);
        Assert.Equal((null, month, day), (monthDay.Year, monthDay.Month, monthDay.Day));
    }

    [Theory]
    [InlineData("-01-01")]
    [InlineData("---01-01")]
    [InlineData("--1-01")]
    [InlineData("02-30")]
    [InlineData("04-31")]
    [InlineData("00-01")]
    [InlineData("13-01")]
    [InlineData("01-00")]
    [InlineData("01-32")]
    [InlineData("2021-02-29")]
    [InlineData("01-01T12:00")]
    [InlineData("--01-01T12:00")]
    [InlineData("01-01Z")]
    public void G15_a_month_day_string_off_the_grammar(string text) => Refuse(text, TemporalGoal.MonthDay);

    [Fact]
    public void G15_a_month_day_without_a_year_must_be_iso_8601_and_a_date_keeps_its_year()
    {
        Assert.Equal(TemporalParseError.MonthDayOutsideIsoCalendar, Refuse("01-01[u-ca=gregory]", TemporalGoal.MonthDay));
        var withYear = Accept("2020-01-01[u-ca=gregory]", TemporalGoal.MonthDay);
        Assert.Equal((2020L, "gregory"), (withYear.Year!.Value, withYear.Calendar));
        Assert.Equal(2020, Accept("2020-02-29", TemporalGoal.MonthDay).Year);
    }

    [Fact]
    public void G15_zoned_strings_and_precise_offsets()
    {
        Assert.Null(Accept("2020-01-01[UTC]", TemporalGoal.ZonedDateTime).Time);
        Assert.True(Accept("2020-01-01T00:00Z[UTC]", TemporalGoal.ZonedDateTime).Z);
        Assert.True(Accept("2020-01-01T00:00z[UTC]", TemporalGoal.ZonedDateTime).Z);
        Refuse("2020-01-01T00:00", TemporalGoal.ZonedDateTime);
        Refuse("2020-01-01Z[UTC]", TemporalGoal.ZonedDateTime);
        Refuse("2020-01-01T00:00Z", TemporalGoal.ZonedDateTime);

        // A precise offset is kept to the nanosecond, with its seconds noted for minute matching (Monrovia, -00:44:30).
        var monrovia = Accept("1970-01-01T00:00-00:44:30[Africa/Monrovia]", TemporalGoal.ZonedDateTime);
        Assert.Equal((-2_670_000_000_000L, true), (monrovia.OffsetNanoseconds, monrovia.OffsetHasSeconds));
        var minutes = Accept("1970-01-01T00:00-00:44[Africa/Monrovia]", TemporalGoal.ZonedDateTime);
        Assert.Equal((-2_640_000_000_000L, false), (minutes.OffsetNanoseconds, minutes.OffsetHasSeconds));
        Assert.Equal(3_600_000_000_001L, Accept("1970-01-01T00:00+01:00:00.000000001[UTC]", TemporalGoal.ZonedDateTime).OffsetNanoseconds);

        // An instant needs Z or an offset, and a time.
        Refuse("2020-01-01T00:00", TemporalGoal.Instant);
        Refuse("2020-01-01Z", TemporalGoal.Instant);
        Refuse("2020-01-01", TemporalGoal.Instant);
        Assert.True(TemporalGrammar.TryParseInstant("2020-01-01T00:00+01:00", out var instant, out _));
        Assert.Equal(-(BigInteger)3_600_000_000_000 + IsoArithmetic.GetUtcEpochNanoseconds(Dt(2020, 1, 1)), instant);
    }

    // ---- G16: a time on a year-month ---------------------------------------------------------------------

    [Theory]
    [InlineData("2020-01T12:00")]
    [InlineData("202001T1200")]
    [InlineData("2020-01 12:00")]
    [InlineData("2020-01T12:00Z")]
    [InlineData("2020-01T12:00[u-ca=iso8601]")]
    [InlineData("2020-01T25:00")]
    public void G16_a_year_month_has_no_time(string text)
    {
        Refuse(text, TemporalGoal.YearMonth);
        Refuse("01-01T12:00", TemporalGoal.MonthDay);
    }

    // ---- G17: calendar strings ---------------------------------------------------------------------------

    [Theory]
    [InlineData("T10:00Z")]
    [InlineData("10:00Z")]
    [InlineData("2020-01[u-ca=gregory]")]
    [InlineData("01-01[u-ca=gregory]")]
    [InlineData("2020-01-01[]")]
    [InlineData("2020-01-01[u-ca=gregory][UTC]")]
    [InlineData("2020-01-01[!foo=bar]")]
    [InlineData("2020-01-01[u-ca=gregory][!u-ca=japanese]")]
    [InlineData("2020-01-01T12:00+25:00[u-ca=gregory]")]
    [InlineData("2020-01-01[Mars Olympus][u-ca=gregory]")]
    [InlineData("1214[u-ca=gregory]")]
    [InlineData("")]
    [InlineData("-gregory")]
    public void G17_a_string_that_is_not_a_calendar_string_is_refused(string text)
    {
        Assert.False(TemporalGrammar.TryParseCalendarString(text, out var calendar, out var error));
        Assert.Null(calendar);
        Assert.Equal(TemporalParseError.Malformed, error);
    }

    [Theory]
    [InlineData("iso8601", "iso8601")]
    [InlineData("ISO8601", "ISO8601")]
    [InlineData("gregory", "gregory")]
    [InlineData("islamic-umalqura", "islamic-umalqura")]
    [InlineData("2020-01-01", "iso8601")]
    [InlineData("2020-01-01[u-ca=gregory]", "gregory")]
    [InlineData("2020-01-01T00:00Z[UTC][u-ca=hebrew]", "hebrew")]
    [InlineData("2020-01-01T00:00Z[u-ca=hebrew]", "hebrew")]
    [InlineData("T10:00[u-ca=hebrew]", "hebrew")]
    [InlineData("10:00[u-ca=hebrew]", "hebrew")]
    [InlineData("2020-01[u-ca=iso8601]", "iso8601")]
    [InlineData("--01-01", "iso8601")]
    [InlineData("2020-01-01[UTC][u-ca=gregory]", "gregory")]
    [InlineData("2020-01-01[u-ca=gregory][u-ca=japanese]", "gregory")]
    [InlineData("2020-01-01T00:00Z[UTC][u-ca=hebrew][u-ca=coptic]", "hebrew")]
    public void G17_a_calendar_string_answers_its_first_calendar_or_iso_8601_or_itself(string text, string calendar)
    {
        Assert.True(TemporalGrammar.TryParseCalendarString(text, out var read, out var error));
        Assert.Equal(calendar, read);
        Assert.Equal(TemporalParseError.None, error);
    }

    // ---- G18: time zone strings --------------------------------------------------------------------------

    [Theory]
    [InlineData("T10:00Z", TemporalParseError.Malformed)]
    [InlineData("10:00Z", TemporalParseError.Malformed)]
    [InlineData("1214Z", TemporalParseError.Malformed)]
    [InlineData("2020-01-01[u-ca=gregory][Europe/Berlin]", TemporalParseError.Malformed)]
    [InlineData("2020-01-01[foo=bar][Europe/Berlin]", TemporalParseError.Malformed)]
    [InlineData("2020-01-01T00:00Z[u-ca=gregory][!foo=bar]", TemporalParseError.UnknownCriticalAnnotation)]
    [InlineData("2020-01-01T00:00Z[Europe/Berlin][UTC]", TemporalParseError.Malformed)]
    [InlineData("2020-01-01[]", TemporalParseError.Malformed)]
    [InlineData("2020-01-01T00:00+25:00", TemporalParseError.Malformed)]
    [InlineData("2020-01-01", TemporalParseError.NoTimeZone)]
    [InlineData("2020-01-01T00:00", TemporalParseError.NoTimeZone)]
    [InlineData("2020-01-01T00:00[u-ca=gregory]", TemporalParseError.NoTimeZone)]
    [InlineData("2020-01-01T00:00+01:00:01", TemporalParseError.SubMinuteTimeZoneOffset)]
    [InlineData("2020-01-01T00:00+01:00:00", TemporalParseError.SubMinuteTimeZoneOffset)]
    [InlineData("+01:00:01", TemporalParseError.Malformed)]
    [InlineData("", TemporalParseError.Malformed)]
    public void G18_a_string_that_names_no_zone_is_refused(string text, TemporalParseError expected)
    {
        Assert.False(TemporalGrammar.TryParseTimeZoneString(text, out var zone, out var error));
        Assert.Equal(expected, error);
        Assert.Equal(default, zone);
    }

    [Theory]
    [InlineData("UTC", "UTC", 0)]
    [InlineData("utc", "utc", 0)]
    [InlineData("Europe/Berlin", "Europe/Berlin", 0)]
    [InlineData("Mars/Olympus", "Mars/Olympus", 0)]
    [InlineData("+01:00", null, 60)]
    [InlineData("-0530", null, -330)]
    [InlineData("-00:00", null, 0)]
    [InlineData("2020-01-01T00:00Z", "UTC", 0)]
    [InlineData("2020-01-01T00:00z", "UTC", 0)]
    [InlineData("2020-01-01T00:00+01:00", null, 60)]
    [InlineData("2020-01-01[Europe/Berlin]", "Europe/Berlin", 0)]
    [InlineData("2020-01-01T00:00Z[Europe/Berlin]", "Europe/Berlin", 0)]
    [InlineData("2020-01-01T00:00Z[Europe/Berlin][u-ca=iso8601]", "Europe/Berlin", 0)]
    [InlineData("2020-01-01T00:00+05:00[-03:30]", null, -210)]
    [InlineData("10:00+01:00", null, 60)]
    [InlineData("T10:00+01:00", null, 60)]
    [InlineData("10:00[Europe/Berlin]", "Europe/Berlin", 0)]
    [InlineData("2020-01[Europe/Berlin]", "Europe/Berlin", 0)]
    [InlineData("01-01[Europe/Berlin]", "Europe/Berlin", 0)]
    public void G18_a_time_zone_string_answers_its_annotation_then_z_then_its_offset(string text, string? name, int minutes)
    {
        Assert.True(TemporalGrammar.TryParseTimeZoneString(text, out var zone, out var error));
        Assert.Equal(TemporalParseError.None, error);
        Assert.Equal(new TimeZoneIdentifierRecord(name, minutes), zone);
    }

    [Theory]
    [InlineData("UTC", "UTC", 0)]
    [InlineData("+01:00", null, 60)]
    [InlineData("+0530", null, 330)]
    [InlineData("-01", null, -60)]
    public void G18_the_constructor_reads_a_time_zone_identifier_only(string text, string? name, int minutes)
    {
        Assert.True(TemporalGrammar.TryParseTimeZoneIdentifier(text, out var zone));
        Assert.Equal(new TimeZoneIdentifierRecord(name, minutes), zone);
        Assert.False(TemporalGrammar.TryParseTimeZoneIdentifier("2020-01-01T00:00Z", out _));
        Assert.False(TemporalGrammar.TryParseTimeZoneIdentifier("+01:00:00", out _));
        Assert.False(TemporalGrammar.TryParseTimeZoneIdentifier("Mars Olympus", out _));
    }

    // ---- G19: offset strings -----------------------------------------------------------------------------

    [Theory]
    [InlineData("Z")]
    [InlineData("z")]
    [InlineData("+25:00")]
    [InlineData("+24:00")]
    [InlineData("+01:00:60")]
    [InlineData("+01:0000")]
    [InlineData("+01:00:00.1234567890")]
    [InlineData("+1")]
    [InlineData("+01:00Z")]
    [InlineData("")]
    [InlineData("UTC")]
    public void G19_z_and_offsets_off_the_grammar_are_not_offset_strings(string text) =>
        Assert.False(TemporalGrammar.TryParseOffsetString(text, out _));

    [Theory]
    [InlineData("+00:00", 0)]
    [InlineData("-00:00", 0)]
    [InlineData("+23:59", 86_340_000_000_000)]
    [InlineData("+01:00:00.5", 3_600_500_000_000)]
    [InlineData("+01:00:00.123456789", 3_600_123_456_789)]
    [InlineData("+0100", 3_600_000_000_000)]
    [InlineData("+01", 3_600_000_000_000)]
    [InlineData("-00", 0)]
    [InlineData("-05:30:15,25", -19_815_250_000_000)]
    public void G19_an_offset_string_is_the_grammars_with_sub_minute_precision(string text, long nanoseconds)
    {
        Assert.True(TemporalGrammar.TryParseOffsetString(text, out var offset));
        Assert.Equal(nanoseconds, offset);
        Assert.True(TemporalGrammar.IsUtcOffset(text, allowSubMinute: true));
        Assert.Equal(text.Length <= 6, TemporalGrammar.IsUtcOffset(text, allowSubMinute: false));
        Assert.Equal(OffsetTimeZones.IsTimeZoneOffsetString(text), TemporalGrammar.IsUtcOffset(text, allowSubMinute: true));
    }

    // ---- G20: relativeTo strings -------------------------------------------------------------------------

    [Theory]
    [InlineData("2020-01-01T00:00Z")]
    [InlineData("2020-01-01[]")]
    [InlineData("2020-01-01T00:00[u-ca=iso8601][UTC]")]
    [InlineData("2020-01-01T00:00+25:00[UTC]")]
    [InlineData("2020-01-01T00:00+01:0000")]
    [InlineData("2020-01")]
    [InlineData("01-01")]
    [InlineData("12:00")]
    [InlineData("2020-02-30")]
    [InlineData("2020-01-01T24:00")]
    [InlineData("٢٠٢٠-01-01")]
    [InlineData("−002020-01-01")]
    public void G20_relative_to_strings_off_the_grammar_are_refused(string text) => Refuse(text, RelativeTo);

    [Fact]
    public void G20_relative_to_strings_take_the_zoned_goal_then_the_plain_one()
    {
        Assert.Equal(TemporalGoal.DateTime, Accept("2020-01-01", RelativeTo).Goal);
        Assert.Equal(TemporalGoal.ZonedDateTime, Accept("2020-01-01T00:00[UTC]", RelativeTo).Goal);
        Assert.Equal(TemporalGoal.ZonedDateTime, Accept("2020-01-01T00:00Z[UTC]", RelativeTo).Goal);
        Assert.Equal("hebrew", Accept("2020-01-01[u-ca=hebrew]", RelativeTo).Calendar);
    }

    // ---- G21 to G23: duration strings ----------------------------------------------------------------------

    private static DurationFields Duration(string text)
    {
        Assert.True(TemporalGrammar.TryParseDuration(text, out var fields, out var error), text + " was refused");
        Assert.Equal(TemporalParseError.None, error);
        return fields;
    }

    [Fact]
    public void G21_duration_strings_are_the_grammars()
    {
        Assert.Equal(Fields(years: 1), Duration("P1Y"));
        Assert.Equal(Fields(years: 1), Duration("p1y"));
        Assert.Equal(Fields(1, 2, 3, 4, 5, 6, 7), Duration("P1Y2M3W4DT5H6M7S"));
        Assert.Equal(Fields(days: 1, hours: 1), Duration("P1Dt1H"));
        Assert.Equal(Fields(days: -1), Duration("-P1D"));
        Assert.Equal(Fields(days: 1), Duration("+P1D"));
        Assert.Equal(Fields(), Duration("PT0S"));
        Assert.Equal(Fields(hours: 1, minutes: 30), Duration("PT1.5H"));
        Assert.Equal(Fields(hours: 1, minutes: 1, seconds: 30), Duration("PT1H1.5M"));
        Assert.Equal(Fields(hours: 1, seconds: 30), Duration("PT1H0.5M"));
        Assert.Equal(Fields(seconds: 1, milliseconds: 500), Duration("PT1,5S"));
        Assert.Equal(Fields(milliseconds: 123, microseconds: 456, nanoseconds: 789), Duration("PT0.123456789S"));
        Assert.Equal(Fields(hours: 46, minutes: 66, seconds: 71, milliseconds: 500, microseconds: 409, nanoseconds: 40), Duration("PT46H66M71.50040904S"));
        Assert.Equal(Fields(microseconds: 3, nanoseconds: 600), Duration("PT0.000000001H"));
        Assert.Equal(Fields(minutes: -1, nanoseconds: -60), Duration("-PT1.000000001M"));
        Assert.Equal(Fields(hours: 1, minutes: 59, seconds: 59, milliseconds: 999, microseconds: 996, nanoseconds: 400), Duration("PT1.999999999H"));

        var zero = Duration("-P0D");
        Assert.False(double.IsNegative(zero.Days));
    }

    [Theory]
    [InlineData("P")]
    [InlineData("PT")]
    [InlineData("P1YT")] // G22
    [InlineData("P1DT")] // G22
    [InlineData("P1Dt")] // G22
    [InlineData("PT1.5H30M")]
    [InlineData("PT1.5M30S")]
    [InlineData("PT1.5H0M")]
    [InlineData("PT0.1234567890S")]
    [InlineData("PT1.S")]
    [InlineData("PT.5S")]
    [InlineData("P1.5D")]
    [InlineData("P1.5Y")]
    [InlineData("P1.5W")]
    [InlineData("P1D1H")]
    [InlineData("P1H")]
    [InlineData("PT1D")]
    [InlineData("P1M1Y")]
    [InlineData("P1W1M")]
    [InlineData("PT1S1M")]
    [InlineData("PT1M1H")]
    [InlineData("P1Y1Y")]
    [InlineData("PT1H1H")]
    [InlineData("P 1D")]
    [InlineData("P1D ")]
    [InlineData(" P1D")]
    [InlineData("-P1Y-1M")]
    [InlineData("P-1D")]
    [InlineData("PTT1H")]
    [InlineData("1D")]
    [InlineData("P1ſ")] // LATIN SMALL LETTER LONG S is not an S
    [InlineData("")]
    public void G21_G22_strings_off_the_duration_grammar_are_refused(string text)
    {
        Assert.False(TemporalGrammar.TryParseDuration(text, out var fields, out var error));
        Assert.Equal(TemporalParseError.Malformed, error);
        Assert.Equal(default, fields);
    }

    [Fact]
    public void G23_each_duration_part_is_exact_and_made_a_number_once()
    {
        Assert.Equal(Fields(seconds: 9007199254740991, milliseconds: 999, microseconds: 999, nanoseconds: 999), Duration("PT9007199254740991.999999999S"));
        Assert.Equal(Fields(hours: 2501999792983, minutes: 59, seconds: 59, milliseconds: 999, microseconds: 996, nanoseconds: 400), Duration("PT2501999792983.999999999H"));
        Assert.Equal(Fields(days: 1), Duration("P00000000000000000000001D"));
        Assert.Equal(Fields(seconds: 1), Duration("PT00000000000000000000001S"));
        Assert.Equal(Fields(minutes: 150119987579016), Duration("PT150119987579016M"));

        // A part past 2^53 is the double nearest its exact value, and one past every double is an infinity.
        Assert.Equal(9007199254740992.0, Duration("P9007199254740993D").Days);
        Assert.True(double.IsPositiveInfinity(Duration("P1" + new string('0', 400) + "Y").Years));
    }

    // ---- G24: no option is read by the grammar ---------------------------------------------------------------

    [Theory]
    [InlineData("2020-01-01T00:00+01:00[UTC]")] // a valid string: the options are read after it
    [InlineData("2021-02-29T00:00[UTC]")] // an invalid date
    [InlineData("2020-01-01T00:00[Mars Olympus]")] // an invalid zone annotation
    [InlineData("2020-01-01T00:00[UTC][u-ca=gregory][!u-ca=iso8601]")] // an invalid calendar annotation
    [InlineData("2020-01-01T00:00[UTC][!foo=bar]")] // an unknown critical annotation
    [InlineData("2020-01-01T00:00+24:00[UTC]")] // an invalid offset
    public void G24_the_strings_checks_complete_before_any_option_is_read(string text)
    {
        // TryParse takes no option, so every refusal of the string is known before the adapter reads one, as both
        // engines read disambiguation, offset and overflow after the string's errors.
        var accepted = TemporalGrammar.TryParse(text, [TemporalGoal.ZonedDateTime], out var result, out var error);
        Assert.Equal(text.StartsWith("2020-01-01T00:00+01:00", StringComparison.Ordinal), accepted);
        Assert.Equal(accepted, result is not null);
        Assert.Equal(accepted, error == TemporalParseError.None);
    }

    // ---- the meter ----------------------------------------------------------------------------------------

    [Fact]
    public void Every_member_whose_work_grows_charges_the_texts_length_once_before_reading_it()
    {
        var charges = new List<ulong>();
        void Meter(ulong units) => charges.Add(units);

        TemporalGrammar.TryParse("2020-01-01T00:00[UTC]", [TemporalGoal.ZonedDateTime, TemporalGoal.DateTime], out _, out _, Meter);
        TemporalGrammar.TryParse("not a date", Every, out _, out _, Meter);
        TemporalGrammar.TryParseInstant("2020-01-01T00:00Z", out _, out _, Meter);
        TemporalGrammar.TryParseTimeZoneString("Europe/Berlin", out _, out _, Meter);
        TemporalGrammar.TryParseTimeZoneIdentifier("UTC", out _, Meter);
        TemporalGrammar.TryParseCalendarString("gregory", out _, out _, Meter);
        TemporalGrammar.TryParseDuration("P1D", out _, out _, Meter);
        TemporalGrammar.IsTimeZoneIdentifier("+01:00", Meter);
        TemporalGrammar.IsAnnotationValue("islamic-civil", Meter);
        TemporalGrammar.TryParse(string.Empty, Every, out _, out _, Meter);
        Assert.Equal(new ulong[] { 21, 10, 17, 13, 3, 7, 3, 6, 13 }, charges);

        // A meter that throws stops the parse before any character is read.
        Assert.Throws<OperationCanceledException>(() =>
            TemporalGrammar.TryParse("2020-01-01", [TemporalGoal.DateTime], out _, out _, static _ => throw new OperationCanceledException()));
    }

    [Fact]
    public void A_goal_outside_the_enumeration_is_a_broken_precondition() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => TemporalGrammar.TryParse("2020-01-01", [(TemporalGoal)6], out _, out _));
}
