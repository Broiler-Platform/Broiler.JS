using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Broiler.ECMAScript.Numbers;
using Broiler.ECMAScript.Temporal;
using Broiler.ECMAScript.Time;
using Xunit;
using static Broiler.ECMAScript.Algorithms.Tests.CalendarTestSupport;

namespace Broiler.ECMAScript.Algorithms.Tests;

/// <summary>
/// The contract of the durations, rounding and differences sub-areas (milestone SJL-10.3, part C): the option rules
/// (units per operation, defaults, increments and their maxima, difference settings), the statuses, the preconditions
/// each member throws on, the provider interfaces as an engine implements them, and concurrent use.
/// </summary>
public class DurationApiTests
{
    private static readonly DurationFields Zero = default;

    private static DurationFields D(double years = 0, double months = 0, double weeks = 0, double days = 0, double hours = 0, double minutes = 0,
        double seconds = 0, double milliseconds = 0, double microseconds = 0, double nanoseconds = 0) =>
        new(years, months, weeks, days, hours, minutes, seconds, milliseconds, microseconds, nanoseconds);

    private static readonly BigInteger NsPerDay = new(TemporalLimits.NanosecondsPerDay);

    // ---- Option rules -------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(1.0, true, 1L)]
    [InlineData(1.9, true, 1L)]
    [InlineData(1e9, true, 1_000_000_000L)]
    [InlineData(1e9 + 0.5, true, 1_000_000_000L)]
    [InlineData(1e9 + 1, false, 1L)]
    [InlineData(0.99, false, 1L)]
    [InlineData(-1.0, false, 1L)]
    [InlineData(double.NaN, false, 1L)]
    [InlineData(double.PositiveInfinity, false, 1L)]
    public void A_rounding_increment_is_truncated_and_held_to_1_to_10_to_the_9(double value, bool accepted, long increment)
    {
        Assert.Equal(accepted, TemporalOptionRules.TryParseRoundingIncrement(value, out var parsed));
        Assert.Equal(increment, parsed);
    }

    [Fact]
    public void The_maximum_increments_are_the_units_of_the_next_unit_and_none_for_a_date_unit()
    {
        Assert.Equal(24, TemporalOptionRules.MaximumRoundingIncrement(TemporalUnit.Hour));
        Assert.Equal(60, TemporalOptionRules.MaximumRoundingIncrement(TemporalUnit.Minute));
        Assert.Equal(60, TemporalOptionRules.MaximumRoundingIncrement(TemporalUnit.Second));
        Assert.Equal(1000, TemporalOptionRules.MaximumRoundingIncrement(TemporalUnit.Millisecond));
        Assert.Equal(1000, TemporalOptionRules.MaximumRoundingIncrement(TemporalUnit.Nanosecond));
        foreach (var unit in new[] { TemporalUnit.Year, TemporalUnit.Month, TemporalUnit.Week, TemporalUnit.Day })
            Assert.Null(TemporalOptionRules.MaximumRoundingIncrement(unit));
    }

    [Fact]
    public void Unit_groups_admit_their_units_and_auto_only_where_allowed()
    {
        Assert.True(TemporalOptionRules.IsValidUnit(TemporalUnit.Unset, TemporalUnitGroup.Date, allowAuto: false));
        Assert.True(TemporalOptionRules.IsValidUnit(TemporalUnit.Auto, TemporalUnitGroup.Time, allowAuto: true));
        Assert.False(TemporalOptionRules.IsValidUnit(TemporalUnit.Auto, TemporalUnitGroup.Time, allowAuto: false));
        Assert.True(TemporalOptionRules.IsValidUnit(TemporalUnit.Day, TemporalUnitGroup.Date, allowAuto: false));
        Assert.False(TemporalOptionRules.IsValidUnit(TemporalUnit.Hour, TemporalUnitGroup.Date, allowAuto: false));
        Assert.False(TemporalOptionRules.IsValidUnit(TemporalUnit.Day, TemporalUnitGroup.Time, allowAuto: false));
        Assert.True(TemporalOptionRules.IsValidUnit(TemporalUnit.Year, TemporalUnitGroup.DateTime, allowAuto: false));
        Assert.True(TemporalOptionRules.IsCalendarUnit(TemporalUnit.Week));
        Assert.False(TemporalOptionRules.IsCalendarUnit(TemporalUnit.Day));
        Assert.True(TemporalOptionRules.IsDateUnit(TemporalUnit.Day));
        Assert.Equal(TemporalUnit.Month, TemporalOptionRules.LargerOfTwoUnits(TemporalUnit.Hour, TemporalUnit.Month));
    }

    [Theory]
    [InlineData(TemporalType.PlainDate, TemporalUnit.Day, TemporalUnit.Day)]
    [InlineData(TemporalType.PlainDateTime, TemporalUnit.Nanosecond, TemporalUnit.Day)]
    [InlineData(TemporalType.PlainTime, TemporalUnit.Nanosecond, TemporalUnit.Hour)]
    [InlineData(TemporalType.PlainYearMonth, TemporalUnit.Month, TemporalUnit.Year)]
    [InlineData(TemporalType.Instant, TemporalUnit.Nanosecond, TemporalUnit.Second)]
    [InlineData(TemporalType.ZonedDateTime, TemporalUnit.Nanosecond, TemporalUnit.Hour)]
    public void Each_type_has_its_difference_defaults(TemporalType type, TemporalUnit smallest, TemporalUnit largest)
    {
        Assert.True(TemporalOptionRules.TryGetDifferenceSettings(
            type, DifferenceOperation.Until, TemporalUnit.Unset, 1, TemporalOptionRules.DefaultDifferenceRoundingMode, TemporalUnit.Unset, out var settings));
        Assert.Equal(new DifferenceSettings(smallest, largest, RoundingMode.Trunc, 1), settings);
        Assert.True(TemporalOptionRules.TryGetDifferenceSettings(
            type, DifferenceOperation.Until, TemporalUnit.Auto, 1, RoundingMode.Trunc, TemporalUnit.Unset, out var auto));
        Assert.Equal(settings, auto);
    }

    [Fact]
    public void Since_negates_the_mode_and_a_larger_smallest_unit_raises_the_default_largest()
    {
        Assert.True(TemporalOptionRules.TryGetDifferenceSettings(
            TemporalType.PlainTime, DifferenceOperation.Since, TemporalUnit.Unset, 1, RoundingMode.Floor, TemporalUnit.Unset, out var since));
        Assert.Equal(RoundingMode.Ceil, since.RoundingMode);
        Assert.True(TemporalOptionRules.TryGetDifferenceSettings(
            TemporalType.PlainDateTime, DifferenceOperation.Until, TemporalUnit.Unset, 1, RoundingMode.HalfExpand, TemporalUnit.Month, out var month));
        Assert.Equal(new DifferenceSettings(TemporalUnit.Month, TemporalUnit.Month, RoundingMode.HalfExpand, 1), month);
        Assert.Equal(RoundingMode.HalfEven, ExactRounding.Negate(RoundingMode.HalfEven));
    }

    [Theory]
    [InlineData(TemporalType.PlainYearMonth, TemporalUnit.Week, 1L, TemporalUnit.Unset)] // disallowed largest
    [InlineData(TemporalType.PlainYearMonth, TemporalUnit.Unset, 1L, TemporalUnit.Day)] // disallowed smallest
    [InlineData(TemporalType.PlainTime, TemporalUnit.Day, 1L, TemporalUnit.Unset)] // not a time unit
    [InlineData(TemporalType.Instant, TemporalUnit.Unset, 1L, TemporalUnit.Day)] // not a time unit
    [InlineData(TemporalType.PlainDate, TemporalUnit.Day, 1L, TemporalUnit.Month)] // largest smaller than smallest
    [InlineData(TemporalType.PlainDateTime, TemporalUnit.Unset, 1L, TemporalUnit.Auto)] // auto smallest
    [InlineData(TemporalType.PlainTime, TemporalUnit.Unset, 24L, TemporalUnit.Hour)] // the maximum itself
    [InlineData(TemporalType.PlainTime, TemporalUnit.Unset, 7L, TemporalUnit.Hour)] // does not divide 24
    [InlineData(TemporalType.Instant, TemporalUnit.Unset, 1000L, TemporalUnit.Millisecond)]
    public void A_difference_refuses_options_that_do_not_fit_together(TemporalType type, TemporalUnit largest, long increment, TemporalUnit smallest)
    {
        Assert.False(TemporalOptionRules.TryGetDifferenceSettings(type, DifferenceOperation.Until, largest, increment, RoundingMode.Trunc, smallest, out var settings));
        Assert.Equal(default, settings);
    }

    [Fact]
    public void Date_unit_increments_are_unbounded_in_differences()
    {
        Assert.True(TemporalOptionRules.TryGetDifferenceSettings(
            TemporalType.PlainDate, DifferenceOperation.Until, TemporalUnit.Year, 1_000_000_000, RoundingMode.Trunc, TemporalUnit.Month, out var settings));
        Assert.Equal(1_000_000_000, settings.RoundingIncrement);
    }

    [Fact]
    public void Duration_round_needs_one_unit_and_an_increment_on_a_date_unit_only_where_it_is_also_the_largest()
    {
        var duration = D(days: 40, hours: 5);
        Assert.False(TemporalOptionRules.TryGetDurationRoundUnits(duration, TemporalUnit.Unset, 1, TemporalUnit.Unset, out _, out _));
        Assert.True(TemporalOptionRules.TryGetDurationRoundUnits(duration, TemporalUnit.Unset, 1, TemporalUnit.Hour, out var largest, out var smallest));
        Assert.Equal((TemporalUnit.Day, TemporalUnit.Hour), (largest, smallest));
        Assert.True(TemporalOptionRules.TryGetDurationRoundUnits(duration, TemporalUnit.Auto, 1, TemporalUnit.Unset, out largest, out smallest));
        Assert.Equal((TemporalUnit.Day, TemporalUnit.Nanosecond), (largest, smallest));
        Assert.False(TemporalOptionRules.TryGetDurationRoundUnits(duration, TemporalUnit.Month, 2, TemporalUnit.Day, out _, out _));
        Assert.True(TemporalOptionRules.TryGetDurationRoundUnits(duration, TemporalUnit.Day, 2, TemporalUnit.Day, out _, out _));
        Assert.False(TemporalOptionRules.TryGetDurationRoundUnits(duration, TemporalUnit.Hour, 1, TemporalUnit.Day, out _, out _));
        Assert.False(TemporalOptionRules.TryGetDurationRoundUnits(duration, TemporalUnit.Unset, 60, TemporalUnit.Minute, out _, out _));
        Assert.True(TemporalOptionRules.TryGetDurationRoundUnits(duration, TemporalUnit.Unset, 30, TemporalUnit.Minute, out _, out _));
    }

    [Fact]
    public void Round_options_differ_by_type()
    {
        Assert.True(TemporalOptionRules.IsValidRoundOptions(TemporalType.Instant, TemporalUnit.Hour, 24));
        Assert.False(TemporalOptionRules.IsValidRoundOptions(TemporalType.Instant, TemporalUnit.Hour, 5));
        Assert.False(TemporalOptionRules.IsValidRoundOptions(TemporalType.PlainTime, TemporalUnit.Hour, 24));
        Assert.True(TemporalOptionRules.IsValidRoundOptions(TemporalType.PlainTime, TemporalUnit.Hour, 12));
        Assert.True(TemporalOptionRules.IsValidRoundOptions(TemporalType.ZonedDateTime, TemporalUnit.Day, 1));
        Assert.False(TemporalOptionRules.IsValidRoundOptions(TemporalType.PlainDateTime, TemporalUnit.Day, 2));
        Assert.False(TemporalOptionRules.IsValidRoundOptions(TemporalType.PlainTime, TemporalUnit.Day, 1));
        Assert.False(TemporalOptionRules.IsValidRoundOptions(TemporalType.Instant, TemporalUnit.Unset, 1));
        Assert.True(TemporalOptionRules.IsValidTotalUnit(TemporalUnit.Year));
        Assert.False(TemporalOptionRules.IsValidTotalUnit(TemporalUnit.Auto));
        Assert.False(TemporalOptionRules.IsValidTotalUnit(TemporalUnit.Unset));
    }

    [Fact]
    public void The_option_rules_throw_on_values_outside_their_domains()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TemporalOptionRules.TryGetDifferenceSettings((TemporalType)9, DifferenceOperation.Until, TemporalUnit.Unset, 1, RoundingMode.Trunc, TemporalUnit.Unset, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => TemporalOptionRules.TryGetDifferenceSettings(TemporalType.PlainDate, (DifferenceOperation)2, TemporalUnit.Unset, 1, RoundingMode.Trunc, TemporalUnit.Unset, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => TemporalOptionRules.TryGetDifferenceSettings(TemporalType.PlainDate, DifferenceOperation.Until, TemporalUnit.Unset, 0, RoundingMode.Trunc, TemporalUnit.Unset, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => TemporalOptionRules.TryGetDifferenceSettings(TemporalType.PlainDate, DifferenceOperation.Until, TemporalUnit.Unset, 1, (RoundingMode)99, TemporalUnit.Unset, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => TemporalOptionRules.TryGetDurationRoundUnits(Zero, TemporalUnit.Unset, 1_000_000_001, TemporalUnit.Hour, out _, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => TemporalOptionRules.IsValidRoundOptions(TemporalType.PlainDate, TemporalUnit.Day, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => TemporalOptionRules.IsValidRoundOptions(TemporalType.Instant, TemporalUnit.Hour, 0));
    }

    // ---- Durations ------------------------------------------------------------------------------------------------

    [Fact]
    public void Sign_default_largest_unit_negation_and_magnitude()
    {
        Assert.Equal(-1, DurationMath.Sign(D(days: -1, hours: -2)));
        Assert.Equal(0, DurationMath.Sign(Zero));
        Assert.Equal(1, DurationMath.Sign(new DateDuration(0, 0, 0, 1)));
        Assert.Equal(-1, DurationMath.Sign(new InternalDuration(default, -BigInteger.One)));
        Assert.Equal(TemporalUnit.Week, DurationMath.DefaultLargestUnit(D(weeks: 1, seconds: 1)));
        Assert.Equal(TemporalUnit.Nanosecond, DurationMath.DefaultLargestUnit(Zero));

        var negated = DurationMath.Negate(D(years: 1, hours: 2));
        Assert.Equal(D(years: -1, hours: -2), negated);
        Assert.False(double.IsNegative(negated.Months), "a zero negates to +0");
        Assert.Equal(D(years: 1, hours: 2), DurationMath.Abs(negated));
    }

    [Fact]
    public void Internal_conversion_keeps_calendar_units_and_balances_time_exactly()
    {
        var internalDuration = DurationMath.ToInternal(D(months: 1, days: 2, hours: 25, nanoseconds: 1));
        Assert.Equal(new DateDuration(0, 1, 0, 2), internalDuration.Date);
        Assert.Equal((BigInteger)25 * 3_600_000_000_000 + 1, internalDuration.TimeNanoseconds);

        var withDays = DurationMath.ToInternalWith24HourDays(D(days: 2, hours: 1));
        Assert.Equal(default, withDays.Date);
        Assert.Equal((2 * NsPerDay) + 3_600_000_000_000, withDays.TimeNanoseconds);

        Assert.Equal(TemporalStatus.Completed, DurationMath.FromInternal(withDays, TemporalUnit.Hour, out var fields));
        Assert.Equal(D(hours: 49), fields);
    }

    [Fact]
    public void Calendar_units_need_a_relativeTo_and_without_one_the_status_says_so()
    {
        Assert.Equal(TemporalStatus.RelativeToRequired, DurationMath.AddDurations(D(months: 1), D(days: 1), out _));
        Assert.Equal(TemporalStatus.RelativeToRequired, DurationMath.Compare(D(months: 1), D(days: 30), RelativeTo.None, out _));
        Assert.Equal(TemporalStatus.RelativeToRequired, DurationMath.Round(D(days: 40), TemporalUnit.Month, 1, TemporalUnit.Day, RoundingMode.Trunc, RelativeTo.None, out _));
        Assert.Equal(TemporalStatus.RelativeToRequired, DurationMath.Total(D(weeks: 1), TemporalUnit.Day, RelativeTo.None, out _));
        Assert.Equal(TemporalStatus.RelativeToRequired, DurationMath.Total(D(days: 1), TemporalUnit.Year, RelativeTo.None, out _));

        Assert.Equal(TemporalStatus.Completed, DurationMath.Compare(D(months: 1), D(months: 1), RelativeTo.None, out var same));
        Assert.Equal(0, same);
        Assert.Equal(TemporalStatus.Completed, DurationMath.Total(D(days: 1, hours: 12), TemporalUnit.Day, RelativeTo.None, out var total));
        Assert.Equal(1.5, total);
    }

    [Fact]
    public void A_sum_past_the_limits_is_an_invalid_duration()
    {
        Assert.Equal(TemporalStatus.InvalidDuration, DurationMath.AddDurations(D(seconds: 9007199254740991), D(seconds: 1), out var result));
        Assert.Equal(default, result);
        Assert.Equal(TemporalStatus.Completed, DurationMath.AddDurations(D(hours: 23), D(hours: 2), out result));
        Assert.Equal(D(hours: 25), result);
        Assert.Equal(TemporalStatus.Completed, DurationMath.AddDurations(D(days: 1), D(hours: 2), out result));
        Assert.Equal(D(days: 1, hours: 2), result);
    }

    [Fact]
    public void Relative_operations_answer_through_a_plain_date()
    {
        var relativeTo = RelativeTo.FromPlainDate(new IsoDate(2021, 1, 31), Calendars, CalendarId.Iso8601);
        Assert.True(relativeTo.IsPlain);
        Assert.False(relativeTo.IsZoned);
        Assert.Equal(TemporalStatus.Completed, DurationMath.Round(D(days: 45), TemporalUnit.Month, 1, TemporalUnit.Day, RoundingMode.Trunc, relativeTo, out var rounded));
        Assert.Equal(D(months: 1, days: 17), rounded);
        Assert.Equal(TemporalStatus.Completed, DurationMath.Total(D(months: 1), TemporalUnit.Day, relativeTo, out var total));
        Assert.Equal(28, total);
        Assert.Equal(TemporalStatus.Completed, DurationMath.Compare(D(months: 1), D(days: 29), relativeTo, out var compared));
        Assert.Equal(-1, compared);
        Assert.Equal(TemporalStatus.Completed, DurationMath.DateDurationDays(Calendars, CalendarId.Iso8601, new DateDuration(0, 1, 0, 2), new IsoDate(2021, 1, 31), out var days));
        Assert.Equal(30, days);
    }

    [Fact]
    public void The_relativeTo_record_compares_by_its_values()
    {
        var one = RelativeTo.FromPlainDate(new IsoDate(2021, 1, 31), Calendars, CalendarId.Iso8601);
        var two = RelativeTo.FromPlainDate(new IsoDate(2021, 1, 31), Calendars, CalendarId.Iso8601);
        Assert.Equal(one, two);
        Assert.True(one == two);
        Assert.Equal(one.GetHashCode(), two.GetHashCode());
        Assert.True(one != RelativeTo.None);
        Assert.Equal(default, RelativeTo.None);
        Assert.Null(RelativeTo.None.Calendars);
    }

    [Fact]
    public void The_duration_members_throw_on_broken_preconditions()
    {
        var plain = RelativeTo.FromPlainDate(new IsoDate(2021, 1, 31), Calendars, CalendarId.Iso8601);
        Assert.Throws<ArgumentOutOfRangeException>(() => DurationMath.ToInternal(D(days: double.NaN)));
        Assert.Throws<ArgumentOutOfRangeException>(() => DurationMath.AddDurations(D(hours: double.PositiveInfinity), Zero, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => DurationMath.Round(D(days: 1), TemporalUnit.Hour, 1, TemporalUnit.Day, RoundingMode.Trunc, plain, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => DurationMath.Round(D(days: 1), TemporalUnit.Auto, 1, TemporalUnit.Day, RoundingMode.Trunc, plain, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => DurationMath.Round(D(days: 1), TemporalUnit.Day, 0, TemporalUnit.Day, RoundingMode.Trunc, plain, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => DurationMath.Total(D(days: 1), TemporalUnit.Unset, plain, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => DurationMath.FromInternal(default, TemporalUnit.Auto, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => DurationMath.RoundTimeDuration(BigInteger.One, 1, TemporalUnit.Week, RoundingMode.Trunc, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => DurationMath.RoundTimeDurationToIncrement(BigInteger.One, BigInteger.Zero, RoundingMode.Trunc, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => DurationMath.TotalTimeDuration(BigInteger.One, TemporalUnit.Month));
        Assert.Throws<ArgumentOutOfRangeException>(() => DurationMath.Add24HourDaysToTimeDuration(BigInteger.One, double.NaN, out _));
        Assert.Throws<ArgumentNullException>(() => RelativeTo.FromPlainDate(default, null!, CalendarId.Iso8601));
        Assert.Throws<ArgumentNullException>(() => RelativeTo.FromZoned(BigInteger.Zero, null!, Calendars, CalendarId.Iso8601));
        Assert.Throws<ArgumentNullException>(() => DurationMath.DateDurationDays(null!, CalendarId.Iso8601, default, default, out _));
        Assert.Throws<ArgumentNullException>(() => DurationMath.AddZonedDateTime(BigInteger.Zero, null!, Calendars, CalendarId.Iso8601, default, TemporalOverflow.Constrain, out _));
        Assert.Throws<ArgumentNullException>(() => DurationMath.RoundRelativeDuration(
            default, BigInteger.Zero, BigInteger.One, default, null, null!, CalendarId.Iso8601, TemporalUnit.Day, 1, TemporalUnit.Day, RoundingMode.Trunc, out _));
    }

    [Fact]
    public void Time_durations_round_and_total_exactly()
    {
        var duration = (BigInteger)90 * 60_000_000_000; // 90 minutes
        Assert.Equal(TemporalStatus.Completed, DurationMath.RoundTimeDuration(duration, 1, TemporalUnit.Hour, RoundingMode.HalfEven, out var rounded));
        Assert.Equal((BigInteger)2 * 3_600_000_000_000, rounded);
        Assert.Equal(TemporalStatus.Completed, DurationMath.RoundTimeDuration(duration, 1, TemporalUnit.Hour, RoundingMode.Trunc, out rounded));
        Assert.Equal((BigInteger)3_600_000_000_000, rounded);
        Assert.Equal(1.5, DurationMath.TotalTimeDuration(duration, TemporalUnit.Hour));
        Assert.Equal(1.0 / 16, DurationMath.TotalTimeDuration(duration, TemporalUnit.Day));

        // The time duration's limit: 2^53 seconds, less a nanosecond.
        var limit = (BigInteger.One << 53) * 1_000_000_000 - 1;
        Assert.Equal(TemporalStatus.Completed, DurationMath.AddTimeDuration(limit, BigInteger.Zero, out _));
        Assert.Equal(TemporalStatus.InvalidDuration, DurationMath.AddTimeDuration(limit, BigInteger.One, out _));
        Assert.Equal(TemporalStatus.InvalidDuration, DurationMath.RoundTimeDurationToIncrement(limit, (BigInteger)1_000_000_000, RoundingMode.Ceil, out _));
    }

    [Fact]
    public void A_date_duration_record_past_the_limits_is_an_invalid_duration()
    {
        Assert.Equal(TemporalStatus.Completed, DurationMath.CreateDateDurationRecord(1, 2, 3, 4, out var record));
        Assert.Equal(new DateDuration(1, 2, 3, 4), record);
        Assert.Equal(TemporalStatus.InvalidDuration, DurationMath.CreateDateDurationRecord(1, -2, 0, 0, out _));
        Assert.Equal(TemporalStatus.InvalidDuration, DurationMath.CreateDateDurationRecord(0, 0, 0, 1e17, out _));
    }

    // ---- Differences ----------------------------------------------------------------------------------------------

    [Fact]
    public void Differences_answer_each_type_with_its_settings()
    {
        Settings(TemporalType.PlainDate, TemporalUnit.Unset, TemporalUnit.Unset, out var settings);
        Assert.Equal(TemporalStatus.Completed, TemporalDifferences.DifferenceTemporalPlainDate(
            DifferenceOperation.Until, Calendars, CalendarId.Iso8601, new IsoDate(2020, 1, 31), new IsoDate(2020, 3, 1), settings, out var days));
        Assert.Equal(D(days: 30), days);

        Settings(TemporalType.PlainDate, TemporalUnit.Month, TemporalUnit.Unset, out settings);
        Assert.Equal(TemporalStatus.Completed, TemporalDifferences.DifferenceTemporalPlainDate(
            DifferenceOperation.Since, Calendars, CalendarId.Iso8601, new IsoDate(2020, 1, 31), new IsoDate(2020, 3, 1), settings, out var months));
        Assert.Equal(D(months: -1, days: -1), months);

        Settings(TemporalType.PlainTime, TemporalUnit.Unset, TemporalUnit.Minute, out settings);
        Assert.Equal(TemporalStatus.Completed, TemporalDifferences.DifferenceTemporalPlainTime(
            DifferenceOperation.Until, new IsoTime(23, 0, 0, 0, 0, 0), new IsoTime(1, 30, 59, 0, 0, 0), settings, out var time));
        Assert.Equal(D(hours: -21, minutes: -29), time);

        Settings(TemporalType.Instant, TemporalUnit.Hour, TemporalUnit.Unset, out settings);
        Assert.Equal(TemporalStatus.Completed, TemporalDifferences.DifferenceTemporalInstant(
            DifferenceOperation.Until, BigInteger.Zero, 2 * NsPerDay + 1, settings, out var instant));
        Assert.Equal(D(hours: 48, nanoseconds: 1), instant);

        Settings(TemporalType.PlainDateTime, TemporalUnit.Unset, TemporalUnit.Unset, out settings);
        Assert.Equal(TemporalStatus.Completed, TemporalDifferences.DifferenceTemporalPlainDateTime(
            DifferenceOperation.Until, Calendars, CalendarId.Iso8601,
            new IsoDateTime(new IsoDate(2020, 1, 1), new IsoTime(12, 0, 0, 0, 0, 0)), new IsoDateTime(new IsoDate(2020, 1, 3), default), settings, out var dateTime));
        Assert.Equal(D(days: 1, hours: 12), dateTime);

        Settings(TemporalType.PlainYearMonth, TemporalUnit.Unset, TemporalUnit.Unset, out settings);
        Assert.Equal(TemporalStatus.Completed, TemporalDifferences.DifferenceTemporalPlainYearMonth(
            DifferenceOperation.Until, Calendars, CalendarId.Iso8601, new IsoDate(2019, 7, 1), new IsoDate(2021, 3, 1), settings, out var yearMonth));
        Assert.Equal(D(years: 1, months: 8), yearMonth);
    }

    [Fact]
    public void Zoned_differences_are_exact_below_a_day_and_wall_clock_from_the_day_up()
    {
        var zone = Zone("America/New_York");
        // 2021-03-13T12:00-05:00 to 2021-03-14T12:00-04:00: a calendar day of 23 hours.
        var one = IsoArithmetic.GetUtcEpochNanoseconds(new IsoDateTime(new IsoDate(2021, 3, 13), new IsoTime(17, 0, 0, 0, 0, 0)));
        var two = IsoArithmetic.GetUtcEpochNanoseconds(new IsoDateTime(new IsoDate(2021, 3, 14), new IsoTime(16, 0, 0, 0, 0, 0)));
        Settings(TemporalType.ZonedDateTime, TemporalUnit.Unset, TemporalUnit.Unset, out var hours);
        Assert.Equal(TemporalStatus.Completed, TemporalDifferences.DifferenceTemporalZonedDateTime(
            DifferenceOperation.Until, one, two, zone, Calendars, CalendarId.Iso8601, hours, out var exact));
        Assert.Equal(D(hours: 23), exact);
        Settings(TemporalType.ZonedDateTime, TemporalUnit.Day, TemporalUnit.Unset, out var days);
        Assert.Equal(TemporalStatus.Completed, TemporalDifferences.DifferenceTemporalZonedDateTime(
            DifferenceOperation.Until, one, two, zone, Calendars, CalendarId.Iso8601, days, out var wall));
        Assert.Equal(D(days: 1), wall);
    }

    [Fact]
    public void The_difference_members_throw_on_broken_preconditions()
    {
        Settings(TemporalType.PlainTime, TemporalUnit.Unset, TemporalUnit.Unset, out var time);
        Settings(TemporalType.PlainDate, TemporalUnit.Unset, TemporalUnit.Unset, out var date);
        Assert.Throws<ArgumentNullException>(() => TemporalDifferences.DifferenceTemporalPlainDate(
            DifferenceOperation.Until, null!, CalendarId.Iso8601, default, default, date, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => TemporalDifferences.DifferenceTemporalPlainDate(
            DifferenceOperation.Until, Calendars, CalendarId.Iso8601, default, default, time, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => TemporalDifferences.DifferenceTemporalPlainTime(
            DifferenceOperation.Until, default, default, date, out _));
        Assert.Throws<ArgumentNullException>(() => TemporalDifferences.DifferenceTemporalZonedDateTime(
            DifferenceOperation.Until, BigInteger.Zero, BigInteger.Zero, null!, Calendars, CalendarId.Iso8601, time, out _));
    }

    // ---- Providers ------------------------------------------------------------------------------------------------

    [Fact]
    public void An_engines_own_providers_answer_as_the_librarys()
    {
        // A zone an engine implements itself, a fixed offset of an hour, against SJL-8's rules for the same zone; and a
        // calendar provider that forwards, counting its calls.
        var own = new FixedHourZone();
        var library = new TemporalTimeZone(new FixedOffsetTimeZoneRules(3600));
        var counting = new CountingCalendars();
        var origin = IsoArithmetic.GetUtcEpochNanoseconds(new IsoDateTime(new IsoDate(2020, 1, 31), new IsoTime(23, 30, 0, 0, 0, 0)));
        var duration = D(months: 1, days: 3, hours: 7);

        foreach (var unit in new[] { TemporalUnit.Year, TemporalUnit.Month, TemporalUnit.Week, TemporalUnit.Day, TemporalUnit.Hour })
        {
            Assert.Equal(TemporalStatus.Completed, DurationMath.Round(
                duration, TemporalUnit.Year, 1, unit, RoundingMode.HalfExpand, RelativeTo.FromZoned(origin, own, counting, CalendarId.Iso8601), out var mine));
            Assert.Equal(TemporalStatus.Completed, DurationMath.Round(
                duration, TemporalUnit.Year, 1, unit, RoundingMode.HalfExpand, RelativeTo.FromZoned(origin, library, Calendars, CalendarId.Iso8601), out var theirs));
            Assert.Equal(theirs, mine);
            Assert.Equal(TemporalStatus.Completed, DurationMath.Total(duration, unit, RelativeTo.FromZoned(origin, own, counting, CalendarId.Iso8601), out var myTotal));
            Assert.Equal(TemporalStatus.Completed, DurationMath.Total(duration, unit, RelativeTo.FromZoned(origin, library, Calendars, CalendarId.Iso8601), out var theirTotal));
            Assert.Equal(theirTotal, myTotal);
        }

        Assert.True(counting.Calls > 0);
    }

    [Fact]
    public void A_zone_providers_refusal_is_out_of_range()
    {
        var refusing = new RefusingZone();
        var relativeTo = RelativeTo.FromZoned(BigInteger.Zero, refusing, Calendars, CalendarId.Iso8601);
        Assert.Equal(TemporalStatus.OutOfRange, DurationMath.Round(D(days: 1), TemporalUnit.Day, 1, TemporalUnit.Day, RoundingMode.Trunc, relativeTo, out _));
        Assert.Equal(TemporalStatus.OutOfRange, DurationMath.AddZonedDateTime(
            BigInteger.Zero, refusing, Calendars, CalendarId.Iso8601, DurationMath.ToInternal(D(months: 1)), TemporalOverflow.Constrain, out _));
    }

    [Fact]
    public void A_week_or_day_window_that_misses_its_destination_is_a_broken_assertion()
    {
        // ROW D12: a calendar provider that adds nothing makes the week window [origin, origin], which cannot hold a
        // destination ten days on; only inconsistent providers reach this.
        var origin = new IsoDateTime(new IsoDate(2020, 1, 1), default);
        var originNs = IsoArithmetic.GetUtcEpochNanoseconds(origin);
        var duration = new InternalDuration(new DateDuration(0, 0, 1, 3), BigInteger.Zero);
        Assert.Throws<InvalidOperationException>(() => DurationMath.RoundRelativeDuration(
            duration, originNs, originNs + (10 * NsPerDay), origin, null, new StuckCalendars(), CalendarId.Iso8601, TemporalUnit.Week, 1, TemporalUnit.Week,
            RoundingMode.Trunc, out _));
        Assert.Equal(TemporalStatus.Completed, DurationMath.RoundRelativeDuration(
            duration, originNs, originNs + (10 * NsPerDay), origin, null, Calendars, CalendarId.Iso8601, TemporalUnit.Week, 1, TemporalUnit.Week,
            RoundingMode.Trunc, out var rounded));
        Assert.Equal(new InternalDuration(new DateDuration(0, 0, 1, 0), BigInteger.Zero), rounded);
    }

    [Fact]
    public void The_durations_answer_alike_from_many_threads()
    {
        var zone = Zone("Europe/Berlin");
        var origin = IsoArithmetic.GetUtcEpochNanoseconds(new IsoDateTime(new IsoDate(2021, 3, 27), new IsoTime(23, 30, 0, 0, 0, 0)));
        DurationFields Compute(int n)
        {
            var relativeTo = n % 2 == 0
                ? RelativeTo.FromZoned(origin, zone, Calendars, CalendarId.Iso8601)
                : RelativeTo.FromPlainDate(new IsoDate(2020, 1, 31), Calendars, NonIso.Length > 0 ? Id(NonIso[n % NonIso.Length]) : CalendarId.Iso8601);
            var status = DurationMath.Round(D(months: n % 13, days: n % 40, hours: n % 30), TemporalUnit.Year, 1, (TemporalUnit)(n % 5), RoundingMode.HalfExpand, relativeTo, out var result);
            Assert.Equal(TemporalStatus.Completed, status);
            return result;
        }

        var expected = Enumerable.Range(0, 256).Select(Compute).ToArray();
        var actual = new DurationFields[expected.Length];
        Parallel.For(0, 64, n =>
        {
            for (var i = n; i < expected.Length; i += 64)
                actual[i] = Compute(i);
        });
        Assert.Equal(expected, actual);
    }

    // ---- Helpers ---------------------------------------------------------------------------------------------------

    private static void Settings(TemporalType type, TemporalUnit largest, TemporalUnit smallest, out DifferenceSettings settings) =>
        Assert.True(TemporalOptionRules.TryGetDifferenceSettings(type, DifferenceOperation.Until, largest, 1, RoundingMode.Trunc, smallest, out settings));

    private static ITimeZoneArithmetic Zone(string name)
    {
        Assert.True(TimeTablesFixture.Database.TryFind(name, out var id));
        return new TemporalTimeZone(TimeTablesFixture.Database.Rules(id));
    }

    // A zone of a constant hour east of UTC, as an engine might write one.
    private sealed class FixedHourZone : ITimeZoneArithmetic
    {
        private static readonly BigInteger Hour = 3_600_000_000_000;

        public long GetOffsetNanosecondsFor(BigInteger epochNanoseconds) => (long)Hour;

        public TimeStatus GetEpochNanosecondsFor(IsoDateTime dateTime, Disambiguation disambiguation, out BigInteger epochNanoseconds)
        {
            epochNanoseconds = IsoArithmetic.GetUtcEpochNanoseconds(dateTime) - Hour;
            return TemporalLimits.CheckEpochNanoseconds(epochNanoseconds) == TemporalStatus.Completed ? TimeStatus.Completed : TimeStatus.OutOfRange;
        }

        public TimeStatus GetStartOfDay(IsoDate date, out BigInteger epochNanoseconds) =>
            GetEpochNanosecondsFor(new IsoDateTime(date, default), Disambiguation.Compatible, out epochNanoseconds);
    }

    // A zone that refuses every wall clock.
    private sealed class RefusingZone : ITimeZoneArithmetic
    {
        public long GetOffsetNanosecondsFor(BigInteger epochNanoseconds) => 0;

        public TimeStatus GetEpochNanosecondsFor(IsoDateTime dateTime, Disambiguation disambiguation, out BigInteger epochNanoseconds)
        {
            epochNanoseconds = BigInteger.Zero;
            return TimeStatus.OutOfRange;
        }

        public TimeStatus GetStartOfDay(IsoDate date, out BigInteger epochNanoseconds)
        {
            epochNanoseconds = BigInteger.Zero;
            return TimeStatus.NoStartOfDay;
        }
    }

    // A calendar provider that forwards to the library's, counting its calls.
    private sealed class CountingCalendars : ICalendarArithmetic
    {
        private int calls;

        public int Calls => calls;

        public CalendarStatus DateAdd(CalendarId calendar, IsoDate date, CalendarDuration duration, CalendarOverflow overflow, out IsoDate result)
        {
            System.Threading.Interlocked.Increment(ref calls);
            return Calendars.DateAdd(calendar, date, duration, overflow, out result);
        }

        public CalendarStatus DateUntil(CalendarId calendar, IsoDate one, IsoDate two, CalendarUnit largestUnit, out CalendarDuration result)
        {
            System.Threading.Interlocked.Increment(ref calls);
            return Calendars.DateUntil(calendar, one, two, largestUnit, out result);
        }
    }

    // An inconsistent calendar provider: every addition answers the date it was given.
    private sealed class StuckCalendars : ICalendarArithmetic
    {
        public CalendarStatus DateAdd(CalendarId calendar, IsoDate date, CalendarDuration duration, CalendarOverflow overflow, out IsoDate result)
        {
            result = date;
            return CalendarStatus.Completed;
        }

        public CalendarStatus DateUntil(CalendarId calendar, IsoDate one, IsoDate two, CalendarUnit largestUnit, out CalendarDuration result)
        {
            result = default;
            return CalendarStatus.Completed;
        }
    }
}
