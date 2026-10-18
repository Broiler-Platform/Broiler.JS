using System;
using System.Numerics;
using Broiler.ECMAScript.Numbers;
using Broiler.ECMAScript.Temporal;
using Broiler.ECMAScript.Time;
using Xunit;
using static Broiler.ECMAScript.Algorithms.Tests.TemporalLimitsTests;

namespace Broiler.ECMAScript.Algorithms.Tests;

/// <summary>
/// The Temporal core's balancing, regulation, addition and rounding of points (milestone SJL-10.3, part A): the rows B1
/// to B9 of bundle sjl-10-001's correspondence, each answered as Broiler.VM answers it (its run files,
/// <c>correspondence-runs/core/b*.vm.txt</c>). Where a row reaches through an engine's method, the test composes the
/// library's operations in the order the method's steps call them.
/// </summary>
public class TemporalBalancingTests
{
    private const long Hour = 3_600_000_000_000L;

    private const long Minute = 60_000_000_000L;

    private const long Second = 1_000_000_000L;

    private static readonly BigInteger Max = ZonedTime.MaxEpochNanoseconds;

    private static (IsoTime Time, long Days) AddTime(IsoTime time, BigInteger duration) => (IsoArithmetic.AddTime(time, duration, out var days), days);

    private static (TemporalStatus Status, IsoDate Date) Add(IsoDate date, double years = 0, double months = 0, double weeks = 0, double days = 0, TemporalOverflow overflow = TemporalOverflow.Constrain)
    {
        var status = IsoArithmetic.AddIsoDate(date, new DateDuration(years, months, weeks, days), overflow, out var result);
        return (status, result);
    }

    // AddDurationToDateTime's steps: AddTime, the days carried into the date duration, CalendarDateAdd, the limit.
    private static (TemporalStatus Status, IsoDateTime Result) AddToDateTime(IsoDateTime dateTime, DateDuration date, BigInteger time)
    {
        var (added, days) = AddTime(dateTime.Time, time);
        var status = IsoArithmetic.AddIsoDate(dateTime.Date, date with { Days = date.Days + days }, TemporalOverflow.Constrain, out var result);
        if (status != TemporalStatus.Completed)
            return (status, default);

        var sum = new IsoDateTime(result, added);
        return (TemporalLimits.CheckIsoDateTime(sum), sum);
    }

    // AddDurationToDate's steps for time units: the whole days of the time duration, truncated toward zero.
    private static (TemporalStatus Status, IsoDate Date) AddTimeUnitsToDate(IsoDate date, double days, BigInteger time) =>
        Add(date, days: days + (double)BigInteger.Divide(time, TemporalLimits.NanosecondsPerDay));

    // ---- B1: PlainTime arithmetic wraps modulo a day exactly ---------------------------------------------------

    [Fact]
    public void B1_a_time_plus_a_time_duration_wraps_modulo_a_day_and_answers_the_days_beside_it()
    {
        Assert.Equal((T(0), 1L), AddTime(T(23, 59, 59, 999, 999, 999), 1));
        Assert.Equal((T(23, 59, 59, 999, 999, 999), -1L), AddTime(T(0), -1));
        Assert.Equal(T(4), AddTime(T(12), (BigInteger)1_000_000_000 * Hour).Time);
        Assert.Equal(T(4, 24), AddTime(T(12), -((BigInteger)2501999792983 * Hour + 36 * Minute)).Time);
        Assert.Equal(T(4, 0, 0, 385, 875, 968), AddTime(T(12), new BigInteger(9e24)).Time);
        Assert.Equal(T(13), AddTime(T(12), Hour).Time); // {days: 3, hours: 1}: the days are not a time's
        Assert.Equal(T(12), AddTime(T(12), 0).Time); // {years: 1, months: 1, weeks: 1}
        Assert.Equal(T(14, 30), AddTime(T(12), 90 * Minute + 3600 * Second).Time);
        Assert.Equal(T(23, 59), AddTime(T(1), -61 * Minute).Time);

        Assert.True(TemporalGrammar.TryParseDuration("-PT36H", out var duration, out _));
        Assert.Equal((T(0), -1L), AddTime(T(12), (BigInteger)duration.Hours * Hour));
    }

    [Fact]
    public void B1_balance_time_carries_each_field_into_the_next()
    {
        Assert.Equal(T(1, 0, 0, 0, 0, 1), IsoArithmetic.BalanceTime(24, 59, 59, 999, 999, 1001, out var days));
        Assert.Equal(1, days);
        Assert.Equal(T(23, 59, 59, 999, 999, 999), IsoArithmetic.BalanceTime(0, 0, 0, 0, 0, -1, out days));
        Assert.Equal(-1, days);
        Assert.Equal(T(0), IsoArithmetic.BalanceTime(BigInteger.Zero, out days));
        Assert.Equal(0, days);
    }

    // ---- B2: PlainDateTime and Instant across days ------------------------------------------------------------

    [Fact]
    public void B2_a_date_time_balances_its_time_into_its_date()
    {
        Assert.Equal((TemporalStatus.Completed, Dt(2021, 1, 2)), AddToDateTime(Dt(2020, 12, 31, 23), default, 25 * Hour));
        Assert.Equal((TemporalStatus.Completed, Dt(2019, 12, 31, 23, 59)), AddToDateTime(Dt(2020, 1, 1), default, -Minute));
        Assert.Equal((TemporalStatus.Completed, Dt(2019, 12, 31)), AddToDateTime(Dt(2020, 1, 1, 12), default, -36 * Hour));
        Assert.Equal((TemporalStatus.Completed, Dt(2020, 3, 1)), AddToDateTime(Dt(2020, 2, 28, 12), new DateDuration(0, 0, 0, 1), 12 * Hour));
        Assert.Equal((TemporalStatus.Completed, Dt(2020, 3, 1, 1)), AddToDateTime(Dt(2020, 1, 31, 23), new DateDuration(0, 1, 0, 0), 2 * Hour));
        Assert.Equal((TemporalStatus.Completed, Dt(2021, 1, 1)), AddToDateTime(Dt(2020, 1, 1), default, (BigInteger)TemporalLimits.NanosecondsPerDay * 366));
        Assert.Equal(TemporalStatus.OutOfRange, AddToDateTime(Dt(2020, 1, 1), default, (BigInteger)9007199254740991 * Second).Status);
        Assert.Equal(Dt(2020, 1, 1), IsoArithmetic.BalanceIsoDateTime(D(2019, 12, 31), TemporalLimits.NanosecondsPerDay));
    }

    [Fact]
    public void B2_an_instant_adds_exactly_and_refuses_past_the_range()
    {
        var start = IsoArithmetic.GetUtcEpochNanoseconds(Dt(2020, 1, 1));
        Assert.Equal(TemporalStatus.Completed, IsoArithmetic.AddInstant(start, 48 * Hour + 1, out var sum));
        Assert.Equal(Dt(2020, 1, 3, ns: 1), IsoArithmetic.GetIsoPartsFromEpoch(sum));
        Assert.Equal(TemporalStatus.OutOfRange, IsoArithmetic.AddInstant(start, -(BigInteger)2501999792983 * Hour, out sum));
        Assert.Equal(BigInteger.Zero, sum);
    }

    // ---- B3: AddISODate under both overflows -------------------------------------------------------------------

    [Fact]
    public void B3_the_iso_calendar_adds_years_and_months_then_regulates_then_adds_days()
    {
        Assert.Equal((TemporalStatus.Completed, D(2020, 2, 29)), Add(D(2020, 1, 31), months: 1));
        Assert.Equal(TemporalStatus.InvalidDate, Add(D(2020, 1, 31), months: 1, overflow: TemporalOverflow.Reject).Status);
        Assert.Equal((TemporalStatus.Completed, D(2021, 2, 28)), Add(D(2020, 2, 29), years: 1));
        Assert.Equal(TemporalStatus.InvalidDate, Add(D(2020, 2, 29), years: 1, overflow: TemporalOverflow.Reject).Status);
        Assert.Equal((TemporalStatus.Completed, D(2020, 2, 29)), Add(D(2020, 3, 31), months: -1));
        Assert.Equal((TemporalStatus.Completed, D(2020, 3, 1)), Add(D(2020, 1, 31), months: 1, days: 1));
        Assert.Equal((TemporalStatus.Completed, D(2020, 2, 29)), Add(D(2019, 1, 31), years: 1, months: 1));
        Assert.Equal((TemporalStatus.Completed, D(2020, 1, 18)), Add(D(2020, 1, 1), weeks: 2, days: 3));
        Assert.Equal((TemporalStatus.Completed, D(2018, 12, 15)), Add(D(2020, 3, 15), months: -15));
        Assert.Equal((TemporalStatus.Completed, D(102020, 1, 1)), Add(D(2020, 1, 1), months: 1_200_000));
        Assert.Equal(TemporalStatus.OutOfRange, Add(D(2020, 1, 1), days: 1e11).Status);
        Assert.Equal(TemporalStatus.OutOfRange, Add(D(2020, 1, 1), years: 1e9 + 1).Status);
    }

    [Theory]
    [InlineData("constrain", true, TemporalOverflow.Constrain)]
    [InlineData("reject", true, TemporalOverflow.Reject)]
    [InlineData("bogus", false, TemporalOverflow.Constrain)]
    [InlineData("Reject", false, TemporalOverflow.Constrain)]
    [InlineData("", false, TemporalOverflow.Constrain)]
    public void B3_the_overflow_option_is_constrain_or_reject_compared_exactly(string value, bool valid, TemporalOverflow overflow)
    {
        Assert.Equal(valid, TemporalOptionRules.TryParseOverflow(value, out var read));
        Assert.Equal(overflow, read);
        Assert.Equal(TemporalOverflow.Constrain, TemporalOptionRules.DefaultOverflow);
    }

    // ---- B4: RegulateISODate and RegulateTime ---------------------------------------------------------------

    [Fact]
    public void B4_regulation_constrains_each_field_or_refuses_under_reject()
    {
        Assert.Equal(TemporalStatus.Completed, IsoArithmetic.RegulateIsoDate(2021, 2, 31, TemporalOverflow.Constrain, out var date));
        Assert.Equal(D(2021, 2, 28), date);
        Assert.Equal(TemporalStatus.Completed, IsoArithmetic.RegulateIsoDate(2021, 13, 1, TemporalOverflow.Constrain, out date));
        Assert.Equal(D(2021, 12, 1), date);
        Assert.Equal(TemporalStatus.InvalidDate, IsoArithmetic.RegulateIsoDate(2021, 2, 31, TemporalOverflow.Reject, out date));
        Assert.Equal(default, date);
        Assert.Equal(TemporalStatus.Completed, IsoArithmetic.RegulateIsoDate(2021, 2, 29, TemporalOverflow.Constrain, out date)); // with({year: 2021})
        Assert.Equal(D(2021, 2, 28), date);
        Assert.Equal(TemporalStatus.InvalidDate, IsoArithmetic.RegulateIsoDate(2021, 2, 29, TemporalOverflow.Reject, out _));
        Assert.Equal(TemporalStatus.Completed, IsoArithmetic.RegulateIsoDate(2020, 13, 1, TemporalOverflow.Constrain, out date)); // a year-month's
        Assert.Equal(D(2020, 12, 1), date);
        Assert.Equal(TemporalStatus.InvalidDate, IsoArithmetic.RegulateIsoDate(2020, 13, 1, TemporalOverflow.Reject, out _));

        // A month or day of 0 is refused by the adapter's ToPositiveIntegerWithTruncation before regulation.
        Assert.Equal(TemporalStatus.InvalidDate, IsoArithmetic.RegulateIsoDate(2021, 0, 1, TemporalOverflow.Reject, out _));

        Assert.Equal(TemporalStatus.Completed, IsoArithmetic.RegulateTime(25, 61, 61, 1000, 0, 0, TemporalOverflow.Constrain, out var time));
        Assert.Equal(T(23, 59, 59, 999), time);
        Assert.Equal(TemporalStatus.InvalidTime, IsoArithmetic.RegulateTime(25, 0, 0, 0, 0, 0, TemporalOverflow.Reject, out _));
        Assert.Equal(TemporalStatus.Completed, IsoArithmetic.RegulateTime(-1, -1, 0, 0, 0, 0, TemporalOverflow.Constrain, out time));
        Assert.Equal(T(0), time);
        Assert.Equal(TemporalStatus.InvalidTime, IsoArithmetic.RegulateTime(-1, 0, 0, 0, 0, 0, TemporalOverflow.Reject, out _));
        Assert.Equal(TemporalStatus.Completed, IsoArithmetic.RegulateTime(0, 0, 0, 0, 1000, 1000, TemporalOverflow.Constrain, out time));
        Assert.Equal(T(0, 0, 0, 0, 999, 999), time);
        Assert.Equal(TemporalStatus.Completed, IsoArithmetic.RegulateTime(0, 0, 60, 0, 0, 0, TemporalOverflow.Constrain, out time));
        Assert.Equal(T(0, 0, 59), time);
        Assert.Equal(TemporalStatus.InvalidTime, IsoArithmetic.RegulateTime(0, 0, 60, 0, 0, 0, TemporalOverflow.Reject, out _));
        Assert.Equal(TemporalStatus.Completed, IsoArithmetic.RegulateTime(12, 75, 0, 0, 0, 0, TemporalOverflow.Constrain, out time)); // with({minute: 75})
        Assert.Equal(T(12, 59), time);
    }

    // ---- B5: addition at the limits -----------------------------------------------------------------------------

    [Fact]
    public void B5_an_addition_past_a_limit_is_out_of_range()
    {
        Assert.Equal(TemporalStatus.OutOfRange, Add(D(275760, 9, 13), days: 1).Status);
        Assert.Equal(TemporalStatus.OutOfRange, Add(D(-271821, 4, 19), days: -1).Status);
        Assert.Equal((TemporalStatus.Completed, D(275760, 9, 13)), Add(D(1970, 1, 1), days: 100_000_000));
        Assert.Equal(TemporalStatus.OutOfRange, Add(D(1970, 1, 1), days: 100_000_001).Status);
        Assert.Equal((TemporalStatus.Completed, D(-271821, 4, 19)), Add(D(-271821, 4, 19)));

        Assert.Equal((TemporalStatus.Completed, Dt(275760, 9, 13, 23, 59)), AddToDateTime(Dt(275760, 9, 13), default, 23 * Hour + 59 * Minute));
        Assert.Equal(TemporalStatus.OutOfRange, AddToDateTime(Dt(275760, 9, 13), new DateDuration(0, 0, 0, 1), 0).Status);
        Assert.Equal(TemporalStatus.OutOfRange, AddToDateTime(Dt(-271821, 4, 19, ns: 1), default, -1).Status);
    }

    [Fact]
    public void B5_a_year_month_adds_to_its_first_day_which_the_date_limits_hold()
    {
        // AddDurationToYearMonth: the first of the month, CalendarDateAdd, then the year-month limit.
        Assert.Equal(TemporalStatus.OutOfRange, Add(D(275760, 9, 1), months: 1).Status);
        Assert.Equal(TemporalStatus.OutOfRange, Add(D(-271821, 4, 1), months: 0).Status);
        Assert.Equal(TemporalStatus.OutOfRange, Add(D(-271821, 5, 1), months: -1).Status); // Broiler.JS's correction (B5)
        var (status, date) = Add(D(275760, 8, 1), months: 1);
        Assert.Equal(TemporalStatus.Completed, status);
        Assert.Equal(TemporalStatus.Completed, TemporalLimits.CheckIsoYearMonth(date));
    }

    [Fact]
    public void B5_an_instant_and_a_zoned_date_time_at_the_limit()
    {
        Assert.Equal(TemporalStatus.OutOfRange, IsoArithmetic.AddInstant(Max, 1, out _));
        Assert.Equal(TemporalStatus.Completed, IsoArithmetic.AddInstant(Max - 1, 1, out var sum));
        Assert.Equal(Max, sum);

        // ZonedDateTime in UTC plus P1D (AddZonedDateTime): the wall clock's date moved within the date-time limit,
        // then read in the zone, which refuses an instant past the range.
        var utc = new FixedOffsetTimeZoneRules(0);
        var (within, past) = AddToDateTime(Dt(275760, 9, 12, 12), new DateDuration(0, 0, 0, 1), 0);
        Assert.Equal(TemporalStatus.Completed, within);
        Assert.Equal(TimeStatus.OutOfRange, ZonedTime.GetEpochNanosecondsFor(utc, past, Disambiguation.Compatible, out _));
        var (completed, result) = AddToDateTime(Dt(275760, 9, 12), new DateDuration(0, 0, 0, 1), 0);
        Assert.Equal(TemporalStatus.Completed, completed);
        Assert.Equal(TimeStatus.Completed, ZonedTime.GetEpochNanosecondsFor(utc, result, Disambiguation.Compatible, out var instant));
        Assert.Equal(Max, instant);
    }

    // ---- B6: ZonedDateTime arithmetic in New York ----------------------------------------------------------

    [Theory]
    [InlineData(2026, 3, 7, 2, 30, 7, 30, false, 1, "2026-03-08T07:30")] // P1D: the wall clock's day, 03:30-04:00
    [InlineData(2026, 3, 7, 2, 30, 7, 30, true, 1, "2026-03-08T07:30")] // PT24H
    [InlineData(2026, 11, 1, 0, 30, 4, 30, false, 1, "2026-11-02T05:30")] // P1D: 00:30-05:00
    [InlineData(2026, 11, 1, 0, 30, 4, 30, true, 1, "2026-11-02T04:30")] // PT24H: 2026-11-01T23:30-05:00
    [InlineData(2026, 10, 31, 1, 30, 5, 30, false, 1, "2026-11-01T05:30")] // P1D into the fold: the earlier, 01:30-04:00
    public void B6_a_calendar_day_moves_the_wall_clock_and_hours_move_the_timeline(
        int year, int month, int day, int hour, int minute, int utcHour, int utcMinute, bool hours, int amount, string expectedUtc)
    {
        var newYork = TimeZoneTests.Rules("America/New_York");
        var start = IsoArithmetic.GetUtcEpochNanoseconds(Dt(year, month, day, utcHour, utcMinute));
        BigInteger result;
        if (hours)
        {
            Assert.Equal(TemporalStatus.Completed, IsoArithmetic.AddInstant(start, amount * 24 * (BigInteger)Hour, out result));
        }
        else
        {
            // AddZonedDateTime: the wall clock, CalendarDateAdd, the same time, compatible disambiguation.
            var wall = IsoArithmetic.GetIsoPartsFromEpoch(start + ZonedTime.GetOffsetNanosecondsFor(newYork, start));
            Assert.Equal(Dt(year, month, day, hour, minute), wall);
            Assert.Equal(TemporalStatus.Completed, IsoArithmetic.AddIsoDate(wall.Date, new DateDuration(0, 0, 0, amount), TemporalOverflow.Constrain, out var date));
            Assert.Equal(TimeStatus.Completed, ZonedTime.GetEpochNanosecondsFor(newYork, new IsoDateTime(date, wall.Time), Disambiguation.Compatible, out result));
        }

        Assert.Equal(expectedUtc, TemporalFormat.DateTimeToString(IsoArithmetic.GetIsoPartsFromEpoch(result), "iso8601", TemporalFormat.MinutePrecision, ShowCalendarName.Never));
    }

    // ---- B7: a PlainDate plus time units ---------------------------------------------------------------------

    [Theory]
    [InlineData(0, 47 * Hour, 2020, 1, 2)]
    [InlineData(0, 48 * Hour, 2020, 1, 3)]
    [InlineData(0, -25 * Hour, 2019, 12, 31)]
    [InlineData(0, -23 * Hour, 2020, 1, 1)]
    [InlineData(0, TemporalLimits.NanosecondsPerDay - 1, 2020, 1, 1)]
    [InlineData(1, 24 * Hour, 2020, 1, 3)]
    public void B7_a_date_takes_a_time_durations_whole_days_truncated_toward_zero(double days, long time, int year, int month, int day) =>
        Assert.Equal((TemporalStatus.Completed, D(year, month, day)), AddTimeUnitsToDate(D(2020, 1, 1), days, time));

    [Fact]
    public void B7_months_and_time_units_and_the_limits()
    {
        var (_, plusMonth) = Add(D(2020, 1, 31), months: 1);
        Assert.Equal((TemporalStatus.Completed, D(2020, 3, 1)), Add(D(2020, 1, 31), months: 1, days: 1));
        Assert.Equal(D(2020, 2, 29), plusMonth);
        Assert.Equal(TemporalStatus.OutOfRange, AddTimeUnitsToDate(D(2020, 1, 1), 0, (BigInteger)9007199254740991 * Second).Status);
        Assert.Equal(TemporalStatus.OutOfRange, AddTimeUnitsToDate(D(2020, 1, 1), 0, (BigInteger)2501999792983 * Hour).Status);
    }

    // ---- B8: PlainYearMonth arithmetic -------------------------------------------------------------------------

    [Fact]
    public void B8_a_year_month_adds_years_and_months_to_its_first_day()
    {
        Assert.Equal((TemporalStatus.Completed, D(2019, 12, 1)), Add(D(2020, 1, 1), months: -1));
        Assert.Equal((TemporalStatus.Completed, D(2022, 2, 1)), Add(D(2020, 1, 1), years: 1, months: 13));
        Assert.Equal((TemporalStatus.Completed, D(2020, 2, 1)), Add(D(2020, 1, 1), months: 1, overflow: TemporalOverflow.Reject));
        Assert.Equal((TemporalStatus.Completed, D(-97980, 1, 1)), Add(D(2020, 1, 1), months: -1_200_000));
        Assert.Equal("-097980-01", TemporalFormat.YearMonthToString(D(-97980, 1, 1), "iso8601", ShowCalendarName.Auto));
    }

    // ---- B9: rounding of points ---------------------------------------------------------------------------------

    [Theory]
    [InlineData(RoundingMode.Ceil, 13, -1)]
    [InlineData(RoundingMode.Floor, 12, -2)]
    [InlineData(RoundingMode.Expand, 13, -1)]
    [InlineData(RoundingMode.Trunc, 12, -2)]
    [InlineData(RoundingMode.HalfCeil, 13, -1)]
    [InlineData(RoundingMode.HalfFloor, 12, -2)]
    [InlineData(RoundingMode.HalfExpand, 13, -1)]
    [InlineData(RoundingMode.HalfTrunc, 12, -2)]
    [InlineData(RoundingMode.HalfEven, 12, -2)]
    public void B9_a_time_rounds_its_quantity_and_an_instant_rounds_as_if_positive(RoundingMode mode, int hour, int seconds)
    {
        Assert.Equal(T(hour), IsoArithmetic.RoundTime(T(12, 30), 1, TemporalUnit.Hour, mode, out var days));
        Assert.Equal(0, days);
        Assert.Equal((BigInteger)seconds * Second, IsoArithmetic.RoundTemporalInstant(-1_500_000_000, 1, TemporalUnit.Second, mode));
    }

    [Fact]
    public void B9_rounding_carries_into_the_day_and_past_the_limits()
    {
        Assert.Equal(T(0), IsoArithmetic.RoundTime(T(23, 59, 59, 500), 1, TemporalUnit.Second, RoundingMode.HalfExpand, out var days));
        Assert.Equal(1, days);
        Assert.Equal(T(0), IsoArithmetic.RoundTime(T(13), 12, TemporalUnit.Hour, RoundingMode.Ceil, out days));
        Assert.Equal(1, days);
        Assert.Equal(Dt(2021, 1, 1), IsoArithmetic.RoundIsoDateTime(Dt(2020, 12, 31, 23, 59, 59, 500), 1, TemporalUnit.Second, RoundingMode.HalfExpand));
        Assert.Equal(Dt(2021, 1, 1), IsoArithmetic.RoundIsoDateTime(Dt(2020, 12, 31, 12), 1, TemporalUnit.Day, RoundingMode.HalfExpand));
        Assert.False(TemporalOptionRules.IsValidRoundingIncrement(2, 1, inclusive: true)); // a day's increment
        Assert.Equal(TemporalStatus.OutOfRange, TemporalLimits.CheckIsoDateTime(
            IsoArithmetic.RoundIsoDateTime(Dt(275760, 9, 13, 12), 1, TemporalUnit.Day, RoundingMode.HalfExpand)));
        Assert.Equal(TemporalStatus.OutOfRange, TemporalLimits.CheckIsoDateTime(
            IsoArithmetic.RoundIsoDateTime(Dt(-271821, 4, 19, ns: 1), 1, TemporalUnit.Day, RoundingMode.Floor)));
        Assert.Equal(TemporalStatus.OutOfRange, TemporalLimits.CheckIsoDateTime(
            IsoArithmetic.RoundIsoDateTime(Dt(-271821, 4, 19, ns: 1), 1, TemporalUnit.Microsecond, RoundingMode.Floor)));
    }

    [Fact]
    public void B9_an_instant_rounds_at_the_limit_and_its_increment_divides_a_day()
    {
        Assert.Equal(Max, IsoArithmetic.RoundTemporalInstant(Max, 1, TemporalUnit.Hour, RoundingMode.Ceil));
        Assert.Equal(Max, IsoArithmetic.RoundTemporalInstant(Max - 1, 24, TemporalUnit.Hour, RoundingMode.HalfExpand));
        Assert.True(TemporalOptionRules.IsValidRoundingIncrement(1440, 1440, inclusive: true));
        Assert.False(TemporalOptionRules.IsValidRoundingIncrement(1441, 1440, inclusive: true));
        var instant = IsoArithmetic.GetUtcEpochNanoseconds(Dt(2020, 1, 1, 12));
        Assert.Equal(Dt(2020, 1, 2), IsoArithmetic.GetIsoPartsFromEpoch(IsoArithmetic.RoundTemporalInstant(instant, 1440, TemporalUnit.Minute, RoundingMode.HalfExpand)));
    }

    [Fact]
    public void B9_a_zoned_date_time_rounds_its_wall_clock_and_reads_it_back_in_the_zone()
    {
        var newYork = TimeZoneTests.Rules("America/New_York");
        BigInteger Round(IsoDateTime wall, TemporalUnit unit)
        {
            var rounded = IsoArithmetic.RoundIsoDateTime(wall, 1, unit, RoundingMode.HalfExpand);
            Assert.Equal(TimeStatus.Completed, ZonedTime.GetEpochNanosecondsFor(newYork, rounded, Disambiguation.Compatible, out var instant));
            return instant;
        }

        // 01:59:59.5 on the day of the gap rounds to 02:00, which does not exist: 03:00-04:00.
        Assert.Equal(IsoArithmetic.GetUtcEpochNanoseconds(Dt(2026, 3, 8, 7)), Round(Dt(2026, 3, 8, 1, 59, 59, 500), TemporalUnit.Second));

        // In the fold, 01:59:59.5-04:00 rounds to 02:00, which is once, -05:00; and 01:30-05:00 to the hour likewise.
        Assert.Equal(IsoArithmetic.GetUtcEpochNanoseconds(Dt(2026, 11, 1, 7)), Round(Dt(2026, 11, 1, 1, 59, 59, 500), TemporalUnit.Second));
        Assert.Equal(IsoArithmetic.GetUtcEpochNanoseconds(Dt(2026, 11, 1, 7)), Round(Dt(2026, 11, 1, 1, 30), TemporalUnit.Hour));

        // Rounding to a day at the limit needs the next day's start, which is out of range.
        var utc = new FixedOffsetTimeZoneRules(0);
        Assert.Equal(TimeStatus.OutOfRange, ZonedTime.GetStartOfDay(utc, D(275760, 9, 14), out _));
        Assert.Equal(TimeStatus.Completed, ZonedTime.GetStartOfDay(utc, D(275760, 9, 13), out var end));
        Assert.Equal(Max, end);
    }

    // ---- the comparisons and the preconditions ---------------------------------------------------------------

    [Fact]
    public void Comparisons_order_dates_times_and_date_times()
    {
        Assert.Equal(-1, IsoArithmetic.CompareIsoDate(D(-1, 12, 31), D(0, 1, 1)));
        Assert.Equal(1, IsoArithmetic.CompareIsoDate(D(2020, 2, 1), D(2020, 1, 31)));
        Assert.Equal(0, IsoArithmetic.CompareIsoDate(D(2020, 2, 1), D(2020, 2, 1)));
        Assert.Equal(-1, IsoArithmetic.CompareTime(T(0, 0, 0, 0, 0, 1), T(0, 0, 0, 0, 1)));
        Assert.Equal(1, IsoArithmetic.CompareIsoDateTime(Dt(2020, 1, 1, ns: 1), Dt(2020, 1, 1)));
        Assert.Equal(-TemporalLimits.NanosecondsPerDay + 1, IsoArithmetic.DifferenceTime(T(23, 59, 59, 999, 999, 999), T(0)));
        Assert.Equal(D(2020, 3, 1), IsoArithmetic.BalanceIsoDate(2019, 14, 30));
        Assert.Equal(D(2019, 12, 31), IsoArithmetic.BalanceIsoDate(2020, 1, 0));
        Assert.Equal(D(2019, 11, 30), IsoArithmetic.AddDaysToIsoDate(D(2020, 1, 1), -32));
    }

    [Fact]
    public void A_broken_precondition_throws_and_is_documented()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => IsoArithmetic.TimeToNanoseconds(T(24)));
        Assert.Throws<ArgumentOutOfRangeException>(() => IsoArithmetic.RoundTime(T(0), 0, TemporalUnit.Hour, RoundingMode.Trunc, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => IsoArithmetic.RoundTime(T(0), 1, TemporalUnit.Month, RoundingMode.Trunc, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => IsoArithmetic.RegulateIsoDate(2020, double.NaN, 1, TemporalOverflow.Reject, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => IsoArithmetic.AddIsoDate(D(2020, 2, 30), default, TemporalOverflow.Reject, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => IsoArithmetic.AddIsoDate(D(2020, 1, 1), new DateDuration(double.NaN, 0, 0, 0), TemporalOverflow.Reject, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => IsoArithmetic.AddDaysToIsoDate(D(2020, 1, 1), long.MaxValue));
        Assert.Throws<ArgumentOutOfRangeException>(() => IsoArithmetic.BalanceTime(BigInteger.Pow(10, 40), out _));

        // A date far outside the limits is out of range, not a broken precondition.
        Assert.Equal(TemporalStatus.OutOfRange, Add(D(long.MaxValue, 1, 1), years: 1).Status);
        Assert.Equal((TemporalStatus.Completed, D(-271821, 4, 19)), Add(D(-271822, 4, 19), years: 1));
    }
}
