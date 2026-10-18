using System;
using System.Numerics;
using Broiler.ECMAScript.Numbers;
using Broiler.ECMAScript.Temporal;
using Broiler.ECMAScript.Time;
using Xunit;

namespace Broiler.ECMAScript.Algorithms.Tests;

/// <summary>
/// The Temporal core's records and limits (milestone SJL-10.3, part A): the rows R1 to R8 of bundle sjl-10-001's
/// correspondence, each answered as Broiler.VM answers it (its run files, <c>correspondence-runs/core/r*.vm.txt</c>),
/// with R6's five faulting cases answered as a status, JSD-0078's decision D9.
/// </summary>
public class TemporalLimitsTests
{
    private static readonly BigInteger Max = ZonedTime.MaxEpochNanoseconds;

    private static readonly BigInteger Min = ZonedTime.MinEpochNanoseconds;

    internal static IsoDate D(long year, int month, int day) => new(year, month, day);

    internal static IsoTime T(int hour, int minute = 0, int second = 0, int millisecond = 0, int microsecond = 0, int nanosecond = 0) =>
        new(hour, minute, second, millisecond, microsecond, nanosecond);

    internal static IsoDateTime Dt(long year, int month, int day, int hour = 0, int minute = 0, int second = 0, int ms = 0, int us = 0, int ns = 0) =>
        new(D(year, month, day), T(hour, minute, second, ms, us, ns));

    internal static DurationFields Fields(
        double years = 0, double months = 0, double weeks = 0, double days = 0, double hours = 0, double minutes = 0, double seconds = 0,
        double milliseconds = 0, double microseconds = 0, double nanoseconds = 0) =>
        new(years, months, weeks, days, hours, minutes, seconds, milliseconds, microseconds, nanoseconds);

    // ---- R1: dates at and one day beyond each limit ----------------------------------------------------------

    [Theory]
    [InlineData(-271821, 4, 19, TemporalStatus.Completed)]
    [InlineData(-271821, 4, 18, TemporalStatus.OutOfRange)]
    [InlineData(275760, 9, 13, TemporalStatus.Completed)]
    [InlineData(275760, 9, 14, TemporalStatus.OutOfRange)]
    [InlineData(-271821, 1, 1, TemporalStatus.OutOfRange)]
    [InlineData(275760, 12, 31, TemporalStatus.OutOfRange)]
    public void R1_a_date_is_within_the_limits_from_minus_271821_04_19_to_275760_09_13(long year, int month, int day, TemporalStatus status) =>
        Assert.Equal(status, TemporalLimits.CheckIsoDate(D(year, month, day)));

    [Fact]
    public void R1_a_date_string_at_and_past_the_limits_parses_and_the_limit_decides()
    {
        Assert.True(TemporalGrammar.TryParse("-271821-04-19", [TemporalGoal.DateTime], out var low, out _));
        Assert.Equal(TemporalStatus.Completed, TemporalLimits.CheckIsoDate(D(low.Year!.Value, low.Month, low.Day)));
        Assert.True(TemporalGrammar.TryParse("-271821-04-18", [TemporalGoal.DateTime], out var below, out _));
        Assert.Equal(TemporalStatus.OutOfRange, TemporalLimits.CheckIsoDate(D(below.Year!.Value, below.Month, below.Day)));
        Assert.True(TemporalGrammar.TryParse("+275760-09-14", [TemporalGoal.DateTime], out var above, out _));
        Assert.Equal(TemporalStatus.OutOfRange, TemporalLimits.CheckIsoDate(D(above.Year!.Value, above.Month, above.Day)));

        // A month-day's reference year is held to the date limits: PlainMonthDay(4, 18, undefined, -271821) throws.
        Assert.Equal(TemporalStatus.OutOfRange, TemporalLimits.CheckIsoDate(D(-271821, 4, 18)));
        Assert.Equal(TemporalStatus.Completed, TemporalLimits.CheckIsoDate(D(-271821, 4, 19)));
        Assert.Equal(TemporalStatus.OutOfRange, TemporalLimits.CheckIsoDate(D(275760, 9, 14)));
    }

    // ---- R2: date-times at the limits, ISODateTimeWithinLimits's strict comparison ----------------------------

    [Fact]
    public void R2_a_date_time_is_strictly_within_a_day_of_the_instant_range()
    {
        Assert.Equal(TemporalStatus.OutOfRange, TemporalLimits.CheckIsoDateTime(Dt(-271821, 4, 19)));
        Assert.Equal(TemporalStatus.Completed, TemporalLimits.CheckIsoDateTime(Dt(-271821, 4, 19, ns: 1)));
        Assert.Equal(TemporalStatus.Completed, TemporalLimits.CheckIsoDateTime(Dt(275760, 9, 13, 23, 59, 59, 999, 999, 999)));
        Assert.Equal(TemporalStatus.OutOfRange, TemporalLimits.CheckIsoDateTime(Dt(275760, 9, 14)));

        // PlainDate("-271821-04-19").toPlainDateTime() is midnight, which is refused.
        Assert.Equal(TemporalStatus.Completed, TemporalLimits.CheckIsoDate(D(-271821, 4, 19)));
        Assert.Equal(TemporalStatus.OutOfRange, TemporalLimits.CheckIsoDateTime(new IsoDateTime(D(-271821, 4, 19), default)));
    }

    // ---- R3: year-months ---------------------------------------------------------------------------------------

    [Theory]
    [InlineData(-271821, 4, 1, TemporalStatus.Completed)]
    [InlineData(-271821, 4, 19, TemporalStatus.Completed)]
    [InlineData(-271821, 3, 1, TemporalStatus.OutOfRange)]
    [InlineData(275760, 9, 1, TemporalStatus.Completed)]
    [InlineData(275760, 10, 1, TemporalStatus.OutOfRange)]
    public void R3_a_year_month_is_within_april_minus_271821_to_september_275760_whatever_its_reference_day(
        long year, int month, int day, TemporalStatus status) =>
        Assert.Equal(status, TemporalLimits.CheckIsoYearMonth(D(year, month, day)));

    [Fact]
    public void R3_the_year_month_limit_is_not_the_date_limit()
    {
        // new PlainYearMonth(-271821, 4, undefined, 1): its reference date is outside the date limits.
        Assert.Equal(TemporalStatus.OutOfRange, TemporalLimits.CheckIsoDate(D(-271821, 4, 1)));
        Assert.Equal(TemporalStatus.Completed, TemporalLimits.CheckIsoYearMonth(D(-271821, 4, 1)));
    }

    // ---- R4: instants ------------------------------------------------------------------------------------------

    [Fact]
    public void R4_an_instant_is_at_most_8_64_times_10_to_the_21_nanoseconds_from_the_epoch()
    {
        Assert.Equal(BigInteger.Parse("8640000000000000000000"), Max);
        Assert.Equal(TemporalStatus.Completed, TemporalLimits.CheckEpochNanoseconds(Max));
        Assert.Equal(TemporalStatus.OutOfRange, TemporalLimits.CheckEpochNanoseconds(Max + 1));
        Assert.Equal(TemporalStatus.Completed, TemporalLimits.CheckEpochNanoseconds(Min));
        Assert.Equal(TemporalStatus.OutOfRange, TemporalLimits.CheckEpochNanoseconds(Min - 1));

        // Instant.fromEpochMilliseconds(8.64e15 + 1).
        Assert.Equal(TemporalStatus.OutOfRange, TemporalLimits.CheckEpochNanoseconds((BigInteger)(8.64e15 + 1) * 1_000_000));
    }

    [Theory]
    [InlineData("-271821-04-20T00:00Z", "-8640000000000000000000")]
    [InlineData("+275760-09-13T00:00Z", "8640000000000000000000")]
    [InlineData("+275760-09-13T01:00+01:00", "8640000000000000000000")]
    [InlineData("-271821-04-19T23:00-01:00", "-8640000000000000000000")]
    [InlineData("-271821-04-19T23:59:59.999999999Z", null)]
    [InlineData("+275760-09-13T00:00:00.000000001Z", null)]
    public void R4_an_instant_string_at_and_past_the_limits(string text, string? expected)
    {
        var accepted = TemporalGrammar.TryParseInstant(text, out var instant, out var error);
        if (expected is null)
        {
            Assert.False(accepted);
            Assert.Equal(TemporalParseError.OutOfRange, error);
        }
        else
        {
            Assert.True(accepted);
            Assert.Equal(BigInteger.Parse(expected), instant);
        }
    }

    [Fact]
    public void R4_the_wall_clock_of_the_extreme_instants_in_the_extreme_zones()
    {
        IsoDateTime Wall(string zone, BigInteger instant) =>
            IsoArithmetic.GetIsoPartsFromEpoch(instant + ZonedTime.GetOffsetNanosecondsFor(TimeZoneTests.Rules(zone), instant));

        Assert.Equal(Dt(275760, 9, 13, 14), Wall("Etc/GMT-14", Max));
        Assert.Equal(Dt(-271821, 4, 19, 12), Wall("Etc/GMT+12", Min));
        Assert.Equal(Dt(-271821, 4, 19, 0, 1), IsoArithmetic.GetIsoPartsFromEpoch(Min - 86_340_000_000_000));
    }

    // ---- R5: time and date fields ------------------------------------------------------------------------------

    [Fact]
    public void R5_time_fields_out_of_their_ranges_are_refused_and_fractions_truncate_before()
    {
        Assert.True(TemporalLimits.IsValidTime(T(23, 59, 59, 999, 999, 999)));
        Assert.Equal(TemporalStatus.InvalidTime, IsoArithmetic.RegulateTime(24, 0, 0, 0, 0, 0, TemporalOverflow.Reject, out _));
        Assert.Equal(TemporalStatus.InvalidTime, IsoArithmetic.RegulateTime(0, 60, 0, 0, 0, 0, TemporalOverflow.Reject, out _));
        Assert.Equal(TemporalStatus.InvalidTime, IsoArithmetic.RegulateTime(0, 0, 0, 1000, 0, 0, TemporalOverflow.Reject, out _));
        Assert.Equal(TemporalStatus.InvalidTime, IsoArithmetic.RegulateTime(-1, 0, 0, 0, 0, 0, TemporalOverflow.Reject, out _));
        Assert.Equal(TemporalStatus.InvalidTime, TemporalLimits.CheckIsoDateTime(new IsoDateTime(D(2020, 1, 1), T(24))));

        // The constructor's ToIntegerWithTruncation is SJL-4's: 1.9 is 1 and -0.5 is 0; NaN and Infinity are the
        // adapter's RangeError before the library is called.
        Assert.Equal(TemporalStatus.Completed, IsoArithmetic.RegulateTime(
            IntegerConversions.ToIntegerOrInfinity(1.9), IntegerConversions.ToIntegerOrInfinity(2.9), 0, 0, 0, 0, TemporalOverflow.Reject, out var time));
        Assert.Equal(T(1, 2), time);
        Assert.Equal(TemporalStatus.Completed, IsoArithmetic.RegulateTime(IntegerConversions.ToIntegerOrInfinity(-0.5), 0, 0, 0, 0, 0, TemporalOverflow.Reject, out time));
        Assert.Equal(T(0), time);
    }

    [Theory]
    [InlineData(2020, 2, 30)]
    [InlineData(2021, 2, 29)]
    [InlineData(2020, 13, 1)]
    [InlineData(2020, 0, 1)]
    [InlineData(2020, 1, 0)]
    public void R5_a_record_that_is_not_a_date_is_refused_by_every_check(long year, int month, int day)
    {
        Assert.Equal(TemporalStatus.InvalidDate, TemporalLimits.CheckIsoDate(D(year, month, day)));
        Assert.Equal(TemporalStatus.InvalidDate, TemporalLimits.CheckIsoDateTime(new IsoDateTime(D(year, month, day), default)));
        Assert.Equal(TemporalStatus.InvalidDate, TemporalLimits.CheckIsoYearMonth(D(year, month, day)));
        Assert.Equal(TemporalStatus.InvalidDate, TemporalLimits.CheckIsoDaysRange(D(year, month, day)));
        Assert.Equal(TemporalStatus.InvalidDate, IsoArithmetic.RegulateIsoDate(year, month, day, TemporalOverflow.Reject, out _));
    }

    // ---- R6: integers beyond the fields, and Broiler.VM's correction --------------------------------------------

    [Theory]
    [InlineData(2147483648L)] // 2^31
    [InlineData(4294969316L)] // 2^32 + 2020
    [InlineData(9007199254740992L)] // 2^53
    [InlineData(9007199254740994L)] // 2^53 + 2: Broiler.VM's invocation ended here (R6)
    [InlineData(long.MaxValue)] // 10^20, saturated by the adapter's conversion: Broiler.VM's invocation ended here (R6)
    [InlineData(long.MinValue)] // -10^20, likewise
    [InlineData(-9007199254740994L)]
    public void R6_every_check_takes_any_year_and_answers_a_status(long year)
    {
        Assert.Equal(TemporalStatus.OutOfRange, TemporalLimits.CheckIsoDate(D(year, 1, 1)));
        Assert.Equal(TemporalStatus.OutOfRange, TemporalLimits.CheckIsoDateTime(Dt(year, 1, 1)));
        Assert.Equal(TemporalStatus.OutOfRange, TemporalLimits.CheckIsoYearMonth(D(year, 1, 1)));
        Assert.Equal(TemporalStatus.OutOfRange, TemporalLimits.CheckIsoDaysRange(D(year, 1, 1)));

        // new PlainMonthDay(1, 1, undefined, year): the reference year is held to the date limits.
        Assert.Equal(TemporalStatus.OutOfRange, TemporalLimits.CheckIsoDate(D(year, 2, 29 - (IsoCalendarMath.IsLeapYear(year) ? 0 : 1))));
    }

    [Fact]
    public void R6_months_days_and_hours_past_2_to_the_32_constrain_or_are_refused()
    {
        const double Beyond = 4294967297.0; // 2^32 + 1
        Assert.Equal(TemporalStatus.InvalidDate, IsoArithmetic.RegulateIsoDate(2020, Beyond, 1, TemporalOverflow.Reject, out _));
        Assert.Equal(TemporalStatus.InvalidDate, IsoArithmetic.RegulateIsoDate(2020, 1, Beyond, TemporalOverflow.Reject, out _));
        Assert.Equal(TemporalStatus.Completed, IsoArithmetic.RegulateIsoDate(2020, Beyond, 1, TemporalOverflow.Constrain, out var date));
        Assert.Equal(D(2020, 12, 1), date);
        Assert.Equal(TemporalStatus.Completed, IsoArithmetic.RegulateIsoDate(2020, 1, Beyond, TemporalOverflow.Constrain, out date));
        Assert.Equal(D(2020, 1, 31), date);
        Assert.Equal(TemporalStatus.Completed, IsoArithmetic.RegulateIsoDate(2020, 1, 1e20, TemporalOverflow.Constrain, out date));
        Assert.Equal(D(2020, 1, 31), date);

        Assert.Equal(TemporalStatus.Completed, IsoArithmetic.RegulateTime(Beyond, 0, 0, 0, 0, 0, TemporalOverflow.Constrain, out var time));
        Assert.Equal(T(23), time);
        Assert.Equal(TemporalStatus.Completed, IsoArithmetic.RegulateTime(-4294967295.0, 0, 0, 0, 0, 0, TemporalOverflow.Constrain, out time));
        Assert.Equal(T(0), time);
        Assert.Equal(TemporalStatus.InvalidTime, IsoArithmetic.RegulateTime(Beyond, 0, 0, 0, 0, 0, TemporalOverflow.Reject, out _));

        // PlainDate.from({year: 2^32 + 2020, ...}): regulated as it is, then refused by the limit.
        Assert.Equal(TemporalStatus.Completed, IsoArithmetic.RegulateIsoDate(4294969316L, 1, 1, TemporalOverflow.Constrain, out date));
        Assert.Equal(TemporalStatus.OutOfRange, TemporalLimits.CheckIsoDate(date));
    }

    // ---- R7: narrowing inside addition, which Broiler.VM refuses ------------------------------------------------

    [Theory]
    [InlineData(4294967295.0, 0)]
    [InlineData(-4294967295.0, 0)]
    [InlineData(4294964275.0, 0)] // 2^32 - 1 - 2020 - 1000
    [InlineData(0, 4294967295.0)]
    public void R7_an_addition_of_years_or_months_past_the_limits_is_out_of_range(double years, double months)
    {
        Assert.Equal(TemporalStatus.OutOfRange, IsoArithmetic.AddIsoDate(D(2020, 6, 15), new DateDuration(years, months, 0, 0), TemporalOverflow.Constrain, out _));

        // PlainYearMonth adds to its month's first day.
        Assert.Equal(TemporalStatus.OutOfRange, IsoArithmetic.AddIsoDate(D(2020, 6, 1), new DateDuration(years, months, 0, 0), TemporalOverflow.Constrain, out _));
    }

    [Theory]
    [InlineData(9e24, 1)] // nanoseconds
    [InlineData(9e21, 1000)] // microseconds
    [InlineData(9e18, 1_000_000)] // milliseconds
    public void R7_a_time_duration_of_9_times_10_to_the_24_nanoseconds_takes_a_date_out_of_range(double field, long unit)
    {
        var timeDuration = new BigInteger(field) * unit;

        // PlainDate: its days are the time duration's whole days.
        var days = (double)BigInteger.Divide(timeDuration, TemporalLimits.NanosecondsPerDay);
        Assert.Equal(TemporalStatus.OutOfRange, IsoArithmetic.AddIsoDate(D(2020, 1, 1), new DateDuration(0, 0, 0, days), TemporalOverflow.Constrain, out _));

        // PlainDateTime: AddTime's days.
        IsoArithmetic.AddTime(T(0), timeDuration, out var carried);
        Assert.Equal(TemporalStatus.OutOfRange, IsoArithmetic.AddIsoDate(D(2020, 1, 1), new DateDuration(0, 0, 0, carried), TemporalOverflow.Constrain, out _));

        // ZonedDateTime with no date part: AddInstant.
        Assert.Equal(TemporalStatus.OutOfRange, IsoArithmetic.AddInstant(IsoArithmetic.GetUtcEpochNanoseconds(Dt(2020, 1, 1)), timeDuration, out _));
    }

    // ---- R8: the duration limits -----------------------------------------------------------------------------

    [Fact]
    public void R8_a_duration_is_valid_up_to_its_limits_and_not_one_past()
    {
        const double TwoTo53 = 9007199254740992.0;
        Assert.Equal(TemporalStatus.Completed, TemporalLimits.CheckDuration(Fields(years: 4294967295)));
        Assert.Equal(TemporalStatus.InvalidDuration, TemporalLimits.CheckDuration(Fields(years: 4294967296)));
        Assert.Equal(TemporalStatus.InvalidDuration, TemporalLimits.CheckDuration(Fields(months: 4294967296)));
        Assert.Equal(TemporalStatus.InvalidDuration, TemporalLimits.CheckDuration(Fields(weeks: -4294967296)));
        Assert.Equal(TemporalStatus.Completed, TemporalLimits.CheckDuration(Fields(days: 104249991374)));
        Assert.Equal(TemporalStatus.InvalidDuration, TemporalLimits.CheckDuration(Fields(days: 104249991375)));
        Assert.Equal(TemporalStatus.Completed, TemporalLimits.CheckDuration(Fields(seconds: TwoTo53 - 1)));
        Assert.Equal(TemporalStatus.InvalidDuration, TemporalLimits.CheckDuration(Fields(seconds: TwoTo53)));
        Assert.Equal(TemporalStatus.Completed, TemporalLimits.CheckDuration(Fields(seconds: TwoTo53 - 1, milliseconds: 999, microseconds: 999, nanoseconds: 999)));
        Assert.Equal(TemporalStatus.InvalidDuration, TemporalLimits.CheckDuration(Fields(seconds: TwoTo53 - 1, milliseconds: 999, microseconds: 999, nanoseconds: 1000)));
        Assert.Equal(TemporalStatus.Completed, TemporalLimits.CheckDuration(Fields(nanoseconds: 9e24)));
        Assert.Equal(TemporalStatus.InvalidDuration, TemporalLimits.CheckDuration(Fields(nanoseconds: TwoTo53 * 1e9)));
        Assert.Equal(TemporalStatus.Completed, TemporalLimits.CheckDuration(Fields(hours: 2501999792983, minutes: 36)));
        Assert.Equal(TemporalStatus.InvalidDuration, TemporalLimits.CheckDuration(Fields(hours: 2501999792983, minutes: 37)));
        Assert.Equal(TemporalStatus.InvalidDuration, TemporalLimits.CheckDuration(Fields(years: 1, months: -1)));
        Assert.Equal(TemporalStatus.InvalidDuration, TemporalLimits.CheckDuration(Fields(years: double.PositiveInfinity)));
        Assert.Equal(TemporalStatus.InvalidDuration, TemporalLimits.CheckDuration(Fields(days: double.NaN)));
        Assert.Equal(TemporalStatus.Completed, TemporalLimits.CheckDuration(Fields(years: -0.0)));

        // The days and the time together: 104,249,991,374 days and the seconds left to 2^53 - 1.
        Assert.Equal(TemporalStatus.Completed, TemporalLimits.CheckDuration(Fields(days: 104249991374, seconds: TwoTo53 - 1 - (104249991374 * 86400.0))));
        Assert.Equal(TemporalStatus.InvalidDuration, TemporalLimits.CheckDuration(Fields(days: 104249991374, seconds: TwoTo53 - (104249991374 * 86400.0))));

        // The exact maximum.
        Assert.Equal(BigInteger.Parse("9007199254740991999999999"), TemporalLimits.MaxTimeDuration);
    }

    [Theory]
    [InlineData("P4294967295Y", TemporalStatus.Completed)]
    [InlineData("P4294967296Y", TemporalStatus.InvalidDuration)]
    [InlineData("PT9007199254740991.999999999S", TemporalStatus.Completed)]
    [InlineData("PT9007199254740992S", TemporalStatus.InvalidDuration)]
    [InlineData("P104249991375D", TemporalStatus.InvalidDuration)]
    [InlineData("PT2501999792983.999999999H", TemporalStatus.InvalidDuration)]
    public void R8_the_limits_from_strings_are_the_validity_of_the_parsed_fields(string text, TemporalStatus status)
    {
        Assert.True(TemporalGrammar.TryParseDuration(text, out var fields, out _));
        Assert.Equal(status, TemporalLimits.CheckDuration(fields));
    }

    [Fact]
    public void R8_a_nanoseconds_field_of_9_times_10_to_the_24_is_kept_as_its_double()
    {
        Assert.Equal("PT9000000000000000.385875968S", TemporalFormat.DurationToString(Fields(nanoseconds: 9e24), TemporalFormat.AutoPrecision));
        Assert.Equal("PT9007199254740991.999999999S", TemporalFormat.DurationToString(
            Fields(seconds: 9007199254740991, milliseconds: 999, microseconds: 999, nanoseconds: 999), TemporalFormat.AutoPrecision));
    }

    // ---- the checks never throw (decision D9) -------------------------------------------------------------------

    [Fact]
    public void No_limit_check_throws_for_any_record()
    {
        long[] years = [long.MinValue, -9007199254740993, -1_000_000_000, -271822, 0, 275761, 9007199254740993, long.MaxValue];
        int[] fields = [int.MinValue, -1, 0, 1, 12, 13, 31, 32, int.MaxValue];
        foreach (var year in years)
        {
            foreach (var month in fields)
            {
                foreach (var day in fields)
                {
                    var date = D(year, month, day);
                    _ = TemporalLimits.CheckIsoDate(date);
                    _ = TemporalLimits.CheckIsoYearMonth(date);
                    _ = TemporalLimits.CheckIsoDaysRange(date);
                    _ = TemporalLimits.CheckIsoDateTime(new IsoDateTime(date, new IsoTime(month, day, 0, 0, 0, 0)));
                }
            }
        }

        Assert.Equal(TemporalStatus.InvalidTime, TemporalLimits.CheckIsoDateTime(new IsoDateTime(D(2020, 1, 1), new IsoTime(int.MaxValue, 0, 0, 0, 0, 0))));
        Assert.Equal(TemporalStatus.OutOfRange, TemporalLimits.CheckEpochNanoseconds(BigInteger.Pow(10, 100)));
        Assert.Equal(TemporalStatus.InvalidDuration, TemporalLimits.CheckDuration(Fields(nanoseconds: double.MaxValue)));
    }
}
