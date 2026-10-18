using System;
using System.Numerics;
using Broiler.ECMAScript.Numbers;
using Broiler.ECMAScript.Temporal;
using Broiler.ECMAScript.Time;
using Xunit;
using static Broiler.ECMAScript.Algorithms.Tests.TemporalLimitsTests;

namespace Broiler.ECMAScript.Algorithms.Tests;

/// <summary>
/// The Temporal string forms and the <c>toString</c> options' rules (milestone SJL-10.3, part A): the rows S1 to S11 of
/// bundle sjl-10-001's correspondence, each written as Broiler.VM writes it (its run files,
/// <c>correspondence-runs/syntax/s*.vm.txt</c>). An instant's and a zoned date-time's offset is read from SJL-8's zone
/// rules over the data home's tables, as an adapter reads it.
/// </summary>
public class TemporalFormatTests
{
    private const int Auto = TemporalFormat.AutoPrecision;

    private static BigInteger Ns(IsoDateTime utc) => IsoArithmetic.GetUtcEpochNanoseconds(utc);

    private static long Offset(string zone, BigInteger instant) => ZonedTime.GetOffsetNanosecondsFor(TimeZoneTests.Rules(zone), instant);

    private static SecondsStringPrecision Precision(TemporalUnit smallestUnit = TemporalUnit.Unset, int digits = Auto) =>
        TemporalOptionRules.ToSecondsStringPrecisionRecord(smallestUnit, digits);

    // Temporal.ZonedDateTime.prototype.toString's steps after its reads: round the instant, read the offset there, write.
    private static string Zoned(
        BigInteger instant, string zone, string calendar = "iso8601", SecondsStringPrecision? precision = null, RoundingMode mode = RoundingMode.Trunc,
        ShowCalendarName showCalendar = ShowCalendarName.Auto, ShowTimeZoneName showTimeZone = ShowTimeZoneName.Auto, ShowOffset showOffset = ShowOffset.Auto)
    {
        var p = precision ?? Precision();
        var rounded = IsoArithmetic.RoundTemporalInstant(instant, p.Increment, p.Unit, mode);
        var offset = zone.StartsWith('+') || zone.StartsWith('-')
            ? (OffsetTimeZones.TryParseOffsetIdentifier(zone, out var minutes) ? minutes * 60_000_000_000L : throw new ArgumentException(zone))
            : Offset(zone, rounded);
        return TemporalFormat.ZonedDateTimeToString(rounded, offset, zone, calendar, p.Precision, showCalendar, showTimeZone, showOffset);
    }

    // Temporal.Instant.prototype.toString's steps after its reads.
    private static string Instant(BigInteger instant, string? zone = null, SecondsStringPrecision? precision = null, RoundingMode mode = RoundingMode.Trunc)
    {
        var p = precision ?? Precision();
        var rounded = IsoArithmetic.RoundTemporalInstant(instant, p.Increment, p.Unit, mode);
        long? offset = null;
        if (zone is not null)
        {
            Assert.True(TemporalGrammar.TryParseTimeZoneString(zone, out var record, out _));
            if (record.Name is { } name)
            {
                Assert.True(TimeTablesFixture.Database.TryFind(name, out var id));
                offset = ZonedTime.GetOffsetNanosecondsFor(TimeTablesFixture.Database.Rules(id), rounded);
            }
            else
            {
                offset = record.OffsetMinutes * 60_000_000_000L;
            }
        }

        return TemporalFormat.InstantToString(rounded, offset, p.Precision);
    }

    private static string Duration(DurationFields fields, SecondsStringPrecision precision, RoundingMode mode = RoundingMode.Trunc)
    {
        var status = TemporalFormat.TryDurationToString(fields, precision, mode, out var text);
        return status == TemporalStatus.Completed ? text! : status.ToString();
    }

    private static DurationFields ParsedDuration(string text)
    {
        Assert.True(TemporalGrammar.TryParseDuration(text, out var fields, out _));
        return fields;
    }

    // ---- S1: years and padding in every form ---------------------------------------------------------------

    [Theory]
    [InlineData(0, "0000")]
    [InlineData(1, "0001")]
    [InlineData(999, "0999")]
    [InlineData(1000, "1000")]
    [InlineData(9999, "9999")]
    [InlineData(10000, "+010000")]
    [InlineData(-1, "-000001")]
    [InlineData(-9999, "-009999")]
    [InlineData(-10000, "-010000")]
    [InlineData(123456, "+123456")]
    [InlineData(-123456, "-123456")]
    [InlineData(1234567, "+1234567")]
    [InlineData(long.MinValue, "-9223372036854775808")]
    public void S1_a_year_is_four_digits_from_0_to_9999_and_otherwise_a_sign_and_six(long year, string padded)
    {
        Assert.Equal(padded, TemporalFormat.PadIsoYear(year));
        if (year is < -300000 or > 300000)
            return;

        Assert.Equal(padded + "-01-02", TemporalFormat.DateToString(D(year, 1, 2), "iso8601", ShowCalendarName.Auto));
        Assert.Equal(padded + "-03", TemporalFormat.YearMonthToString(D(year, 3, 1), "iso8601", ShowCalendarName.Auto));
        Assert.Equal(padded + "-01-02T03:04:05.006007008", TemporalFormat.DateTimeToString(Dt(year, 1, 2, 3, 4, 5, 6, 7, 8), "iso8601", Auto, ShowCalendarName.Auto));
        Assert.Equal(padded + "-01-02T03:04:05.000000006Z", Instant(Ns(Dt(year, 1, 2, 3, 4, 5, ns: 6))));
        Assert.Equal(padded + "-01-02T03:04:05+01:00[+01:00]", Zoned(Ns(Dt(year, 1, 2, 2, 4, 5)), "+01:00"));
    }

    [Fact]
    public void S1_the_limits_and_the_reference_fields()
    {
        Assert.Equal("-271821-04-19", TemporalFormat.DateToString(D(-271821, 4, 19), "iso8601", ShowCalendarName.Auto));
        Assert.Equal("+275760-09-13", TemporalFormat.DateToString(D(275760, 9, 13), "iso8601", ShowCalendarName.Auto));
        Assert.Equal("-271821-04", TemporalFormat.YearMonthToString(D(-271821, 4, 1), "iso8601", ShowCalendarName.Auto));
        Assert.Equal("-271821-04-20T00:00:00Z", Instant(ZonedTime.MinEpochNanoseconds));
        Assert.Equal("+275760-09-13T00:00:00Z", Instant(ZonedTime.MaxEpochNanoseconds));
        Assert.Equal("02-29", TemporalFormat.MonthDayToString(D(1972, 2, 29), "iso8601", ShowCalendarName.Auto));
        Assert.Equal("02-29", TemporalFormat.MonthDayToString(D(2000, 2, 29), "iso8601", ShowCalendarName.Auto));
        Assert.Equal("2020-02", TemporalFormat.YearMonthToString(D(2020, 2, 15), "iso8601", ShowCalendarName.Auto));
    }

    // ---- S2: calendarName ---------------------------------------------------------------------------------

    [Theory]
    [InlineData("iso8601", ShowCalendarName.Auto, "2020-03-15", "2020-02", "02-10", "")]
    [InlineData("gregory", ShowCalendarName.Auto, "2020-03-15[u-ca=gregory]", "2020-02-01[u-ca=gregory]", "1972-02-10[u-ca=gregory]", "[u-ca=gregory]")]
    [InlineData("iso8601", ShowCalendarName.Always, "2020-03-15[u-ca=iso8601]", "2020-02-01[u-ca=iso8601]", "1972-02-10[u-ca=iso8601]", "[u-ca=iso8601]")]
    [InlineData("gregory", ShowCalendarName.Always, "2020-03-15[u-ca=gregory]", "2020-02-01[u-ca=gregory]", "1972-02-10[u-ca=gregory]", "[u-ca=gregory]")]
    [InlineData("iso8601", ShowCalendarName.Never, "2020-03-15", "2020-02", "02-10", "")]
    [InlineData("gregory", ShowCalendarName.Never, "2020-03-15", "2020-02-01", "1972-02-10", "")]
    [InlineData("iso8601", ShowCalendarName.Critical, "2020-03-15[!u-ca=iso8601]", "2020-02-01[!u-ca=iso8601]", "1972-02-10[!u-ca=iso8601]", "[!u-ca=iso8601]")]
    [InlineData("gregory", ShowCalendarName.Critical, "2020-03-15[!u-ca=gregory]", "2020-02-01[!u-ca=gregory]", "1972-02-10[!u-ca=gregory]", "[!u-ca=gregory]")]
    public void S2_the_calendar_annotation_and_the_reference_fields_as_calendar_name_says(
        string calendar, ShowCalendarName show, string date, string yearMonth, string monthDay, string annotation)
    {
        Assert.Equal(date, TemporalFormat.DateToString(D(2020, 3, 15), calendar, show));
        Assert.Equal(date[..10] + "T12:00:00" + annotation, TemporalFormat.DateTimeToString(Dt(2020, 3, 15, 12), calendar, Auto, show));
        Assert.Equal(yearMonth, TemporalFormat.YearMonthToString(D(2020, 2, 1), calendar, show));
        Assert.Equal(monthDay, TemporalFormat.MonthDayToString(D(1972, 2, 10), calendar, show));
        Assert.Equal("2020-03-15T12:00:00+00:00[UTC]" + annotation, Zoned(Ns(Dt(2020, 3, 15, 12)), "UTC", calendar, showCalendar: show));
        Assert.Equal(annotation, TemporalFormat.FormatCalendarAnnotation(calendar, show));
    }

    [Fact]
    public void S2_a_year_month_and_month_day_outside_iso_8601_show_their_reference_day_and_year()
    {
        Assert.Equal("2019-10-30[u-ca=hebrew]", TemporalFormat.YearMonthToString(D(2019, 10, 30), "hebrew", ShowCalendarName.Auto));
        Assert.Equal("1972-10-18[u-ca=hebrew]", TemporalFormat.MonthDayToString(D(1972, 10, 18), "hebrew", ShowCalendarName.Auto));
        Assert.Equal("2020-02-23[u-ca=chinese]", TemporalFormat.YearMonthToString(D(2020, 2, 23), "chinese", ShowCalendarName.Auto));
        Assert.Equal("1972-03-24", TemporalFormat.MonthDayToString(D(1972, 3, 24), "chinese", ShowCalendarName.Never));
    }

    [Theory]
    [InlineData("auto", true, ShowCalendarName.Auto)]
    [InlineData("always", true, ShowCalendarName.Always)]
    [InlineData("never", true, ShowCalendarName.Never)]
    [InlineData("critical", true, ShowCalendarName.Critical)]
    [InlineData("x", false, ShowCalendarName.Auto)]
    [InlineData("AUTO", false, ShowCalendarName.Auto)]
    [InlineData("", false, ShowCalendarName.Auto)]
    [InlineData("null", false, ShowCalendarName.Auto)] // null, 1 and true reach the rule as ToString's strings
    [InlineData("1", false, ShowCalendarName.Auto)]
    [InlineData("true", false, ShowCalendarName.Auto)]
    public void S2_calendar_name_is_one_of_four_values_compared_exactly(string value, bool valid, ShowCalendarName show)
    {
        Assert.Equal(valid, TemporalOptionRules.TryParseShowCalendarName(value, out var read));
        Assert.Equal(show, read);
    }

    // ---- S3: toJSON is toString with no options --------------------------------------------------------------

    [Fact]
    public void S3_to_json_writes_each_form_with_every_default()
    {
        Assert.Equal("03-15", TemporalFormat.MonthDayToString(D(1972, 3, 15), "iso8601", ShowCalendarName.Auto));
        Assert.Equal("2020-03-15T12:00:00.5[u-ca=japanese]", TemporalFormat.DateTimeToString(Dt(2020, 3, 15, 12, ms: 500), "japanese", Auto, ShowCalendarName.Auto));
        Assert.Equal("12:00:00.000001", TemporalFormat.TimeToString(T(12, 0, 0, 0, 1), Auto));
        Assert.Equal("2020-03-15T12:00:00.1+05:30[Asia/Kolkata][u-ca=roc]", Zoned(Ns(Dt(2020, 3, 15, 6, 30, ms: 100)), "Asia/Kolkata", "roc"));
        Assert.Equal("2020-03-15T12:00:00.12Z", Instant(Ns(Dt(2020, 3, 15, 12, ms: 120))));
        Assert.Equal("P1Y2M3W4DT5H6M7.00800901S", TemporalFormat.DurationToString(Fields(1, 2, 3, 4, 5, 6, 7, 8, 9, 10), Auto));
        Assert.Equal("P1Y2M3W4DT5H6M7.00800901S", Duration(Fields(1, 2, 3, 4, 5, 6, 7, 8, 9, 10), Precision()));
    }

    // ---- S4: fractionalSecondDigits -------------------------------------------------------------------------

    [Theory]
    [InlineData(0.0, true, 0)]
    [InlineData(1.0, true, 1)]
    [InlineData(9.0, true, 9)]
    [InlineData(2.5, true, 2)]
    [InlineData(9.9, true, 9)]
    [InlineData(10.0, false, Auto)]
    [InlineData(-1.0, false, Auto)]
    [InlineData(-0.5, false, Auto)]
    [InlineData(double.NaN, false, Auto)]
    [InlineData(double.PositiveInfinity, false, Auto)]
    [InlineData(double.NegativeInfinity, false, Auto)]
    public void S4_a_number_is_floored_and_held_to_0_to_9(double value, bool valid, int digits)
    {
        Assert.Equal(valid, TemporalOptionRules.TryParseFractionalSecondDigits(value, out var read));
        Assert.Equal(digits, read);
        Assert.True(TemporalOptionRules.TryParseFractionalSecondDigits(-0.0, out var zero));
        Assert.Equal(0, zero);
    }

    [Theory]
    [InlineData("auto", true)]
    [InlineData("AUTO", false)]
    [InlineData("3", false)] // the string "3" and the BigInt 3n are not Numbers
    [InlineData("x", false)]
    [InlineData("true", false)]
    [InlineData("null", false)]
    public void S4_anything_else_is_auto_or_refused(string value, bool auto) =>
        Assert.Equal(auto, TemporalOptionRules.IsFractionalSecondDigitsAuto(value));

    [Theory]
    [InlineData(Auto, ".123456789")]
    [InlineData(0, "")]
    [InlineData(1, ".1")]
    [InlineData(4, ".1234")]
    [InlineData(7, ".1234567")]
    [InlineData(9, ".123456789")]
    public void S4_each_form_writes_the_digits(int digits, string fraction)
    {
        var precision = Precision(digits: digits);
        Assert.Equal("12:34:56" + fraction, TemporalFormat.PlainTimeToString(T(12, 34, 56, 123, 456, 789), precision, RoundingMode.Trunc));
        Assert.Equal(TemporalStatus.Completed, TemporalFormat.TryPlainDateTimeToString(
            Dt(2020, 1, 1, 12, 34, 56, 123, 456, 789), "iso8601", precision, RoundingMode.Trunc, ShowCalendarName.Auto, out var dateTime));
        Assert.Equal("2020-01-01T12:34:56" + fraction, dateTime);
        Assert.Equal("2020-01-01T12:34:56" + fraction + "Z", Instant(Ns(Dt(2020, 1, 1, 12, 34, 56, 123, 456, 789)), precision: precision));
        Assert.Equal("2020-01-01T12:34:56" + fraction + "+00:00[UTC]", Zoned(Ns(Dt(2020, 1, 1, 12, 34, 56, 123, 456, 789)), "UTC", precision: precision));
        Assert.Equal("PT12H34M56" + fraction + "S", Duration(Fields(hours: 12, minutes: 34, seconds: 56, milliseconds: 123, microseconds: 456, nanoseconds: 789), precision));
    }

    // ---- S5: smallestUnit and its precision ------------------------------------------------------------------

    [Theory]
    [InlineData("minute", TemporalUnit.Minute, true, false)]
    [InlineData("minutes", TemporalUnit.Minute, true, false)]
    [InlineData("second", TemporalUnit.Second, true, true)]
    [InlineData("seconds", TemporalUnit.Second, true, true)]
    [InlineData("millisecond", TemporalUnit.Millisecond, true, true)]
    [InlineData("microseconds", TemporalUnit.Microsecond, true, true)]
    [InlineData("nanosecond", TemporalUnit.Nanosecond, true, true)]
    [InlineData("hour", TemporalUnit.Hour, false, false)]
    [InlineData("day", TemporalUnit.Day, false, false)]
    [InlineData("week", TemporalUnit.Week, false, false)]
    [InlineData("year", TemporalUnit.Year, false, false)]
    [InlineData("auto", TemporalUnit.Auto, false, false)]
    public void S5_smallest_unit_is_read_as_any_unit_and_validated_as_a_time_unit_after(string value, TemporalUnit unit, bool valid, bool validForDuration)
    {
        Assert.True(TemporalOptionRules.TryParseUnit(value, out var read));
        Assert.Equal(unit, read);
        Assert.Equal(valid, TemporalOptionRules.IsValidToStringSmallestUnit(read, duration: false));
        Assert.Equal(validForDuration, TemporalOptionRules.IsValidToStringSmallestUnit(read, duration: true));
        Assert.True(TemporalOptionRules.IsValidToStringSmallestUnit(TemporalUnit.Unset, duration: true));
    }

    [Theory]
    [InlineData("MINUTE")]
    [InlineData("")]
    [InlineData("Second")]
    [InlineData("secs")]
    public void S5_a_unit_name_off_the_list_is_refused_when_read(string value)
    {
        Assert.False(TemporalOptionRules.TryParseUnit(value, out var read));
        Assert.Equal(TemporalUnit.Unset, read);
    }

    [Fact]
    public void S5_the_smallest_unit_wins_over_the_digits_and_writes_its_precision()
    {
        Assert.Equal(new SecondsStringPrecision(TemporalFormat.MinutePrecision, TemporalUnit.Minute, 1), Precision(TemporalUnit.Minute, 5));
        Assert.Equal(new SecondsStringPrecision(0, TemporalUnit.Second, 1), Precision(TemporalUnit.Second, 5));
        Assert.Equal(new SecondsStringPrecision(3, TemporalUnit.Millisecond, 1), Precision(TemporalUnit.Millisecond));
        Assert.Equal(new SecondsStringPrecision(6, TemporalUnit.Microsecond, 1), Precision(TemporalUnit.Microsecond));
        Assert.Equal(new SecondsStringPrecision(9, TemporalUnit.Nanosecond, 1), Precision(TemporalUnit.Nanosecond, 1));
        Assert.Equal(new SecondsStringPrecision(Auto, TemporalUnit.Nanosecond, 1), Precision());
        Assert.Equal(new SecondsStringPrecision(0, TemporalUnit.Second, 1), Precision(digits: 0));
        Assert.Equal(new SecondsStringPrecision(2, TemporalUnit.Millisecond, 10), Precision(digits: 2));
        Assert.Equal(new SecondsStringPrecision(4, TemporalUnit.Microsecond, 100), Precision(digits: 4));
        Assert.Equal(new SecondsStringPrecision(8, TemporalUnit.Nanosecond, 10), Precision(digits: 8));

        Assert.Equal("12:34", TemporalFormat.PlainTimeToString(T(12, 34, 56, 123, 456, 789), Precision(TemporalUnit.Minute), RoundingMode.Trunc));
        Assert.Equal("12:34:56", TemporalFormat.PlainTimeToString(T(12, 34, 56, 123, 456, 789), Precision(TemporalUnit.Second), RoundingMode.Trunc));
        Assert.Equal("12:34:56.123456789", TemporalFormat.PlainTimeToString(T(12, 34, 56, 123, 456, 789), Precision(TemporalUnit.Nanosecond), RoundingMode.Trunc));
        Assert.Throws<ArgumentOutOfRangeException>(() => Precision(TemporalUnit.Hour));
        Assert.Throws<ArgumentOutOfRangeException>(() => Precision(digits: 10));
    }

    // ---- S6: roundingMode -----------------------------------------------------------------------------------

    [Theory]
    [InlineData(RoundingMode.Ceil, "12:34:57", "12:34:58", "00:00:00.00000000", "00:00")]
    [InlineData(RoundingMode.Trunc, "12:34:56", "12:34:57", "23:59:59.99999999", "23:59")]
    [InlineData(RoundingMode.HalfExpand, "12:34:57", "12:34:58", "00:00:00.00000000", "00:00")]
    [InlineData(RoundingMode.HalfEven, "12:34:56", "12:34:58", "00:00:00.00000000", "00:00")]
    [InlineData(RoundingMode.Floor, "12:34:56", "12:34:57", "23:59:59.99999999", "23:59")]
    public void S6_a_time_rounds_at_ties_and_carries_past_midnight(RoundingMode mode, string even, string odd, string carry, string minute)
    {
        Assert.Equal(even, TemporalFormat.PlainTimeToString(T(12, 34, 56, 500), Precision(TemporalUnit.Second), mode));
        Assert.Equal(odd, TemporalFormat.PlainTimeToString(T(12, 34, 57, 500), Precision(TemporalUnit.Second), mode));
        Assert.Equal(carry, TemporalFormat.PlainTimeToString(T(23, 59, 59, 999, 999, 995), Precision(digits: 8), mode));
        Assert.Equal(minute, TemporalFormat.PlainTimeToString(T(23, 59, 30), Precision(TemporalUnit.Minute), mode));
    }

    [Theory]
    [InlineData(RoundingMode.Ceil, "1970-01-01T00:00:00Z", "1969-12-31T23:59:59Z", "1969-12-31T23:59:59.3Z", "2021-01-01T00:00:00")]
    [InlineData(RoundingMode.Trunc, "1969-12-31T23:59:59Z", "1969-12-31T23:59:58Z", "1969-12-31T23:59:59.2Z", "2020-12-31T23:59:59")]
    [InlineData(RoundingMode.HalfExpand, "1970-01-01T00:00:00Z", "1969-12-31T23:59:59Z", "1969-12-31T23:59:59.3Z", "2021-01-01T00:00:00")]
    public void S6_an_instant_rounds_as_if_positive_before_the_epoch(RoundingMode mode, string half, string earlierHalf, string quarter, string dateTime)
    {
        Assert.Equal(half, Instant(-500_000_000, precision: Precision(TemporalUnit.Second), mode: mode));
        Assert.Equal(earlierHalf, Instant(-1_500_000_000, precision: Precision(TemporalUnit.Second), mode: mode));
        Assert.Equal(quarter, Instant(-750_000_000, precision: Precision(digits: 1), mode: mode));
        Assert.Equal(TemporalStatus.Completed, TemporalFormat.TryPlainDateTimeToString(
            Dt(2020, 12, 31, 23, 59, 59, 500), "iso8601", Precision(TemporalUnit.Second), mode, ShowCalendarName.Auto, out var text));
        Assert.Equal(dateTime, text);
        Assert.Equal(mode == RoundingMode.Trunc ? "2020-01-01T23:59:59-05:00[America/New_York]" : "2020-01-02T00:00:00-05:00[America/New_York]",
            Zoned(Ns(Dt(2020, 1, 2, 4, 59, 59, 500)), "America/New_York", precision: Precision(TemporalUnit.Second), mode: mode));
    }

    [Theory]
    [InlineData(RoundingMode.Ceil, "PT2S", "-PT1S", "-PT2S", "-PT0.00000001S")]
    [InlineData(RoundingMode.Trunc, "PT1S", "-PT1S", "-PT2S", "-PT0.00000001S")]
    [InlineData(RoundingMode.HalfExpand, "PT2S", "-PT2S", "-PT3S", "-PT0.00000002S")]
    public void S6_a_duration_rounds_its_time_by_its_sign(RoundingMode mode, string positive, string negative, string negativeTie, string small)
    {
        Assert.Equal(positive, Duration(ParsedDuration("PT1.5S"), Precision(TemporalUnit.Second), mode));
        Assert.Equal(negative, Duration(ParsedDuration("-PT1.5S"), Precision(TemporalUnit.Second), mode));
        Assert.Equal(negativeTie, Duration(ParsedDuration("-PT2.5S"), Precision(TemporalUnit.Second), mode));
        Assert.Equal(small, Duration(ParsedDuration("-PT0.000000015S"), Precision(digits: 8), mode));
    }

    [Fact]
    public void S6_rounding_at_the_limits_and_an_invalid_mode()
    {
        Assert.Equal(TemporalStatus.OutOfRange, TemporalFormat.TryPlainDateTimeToString(
            Dt(275760, 9, 13, 23, 59, 59, 999, 999, 999), "iso8601", Precision(TemporalUnit.Second), RoundingMode.Ceil, ShowCalendarName.Auto, out var text));
        Assert.Null(text);
        Assert.Equal("+275760-09-13T00:00:00Z", Instant(ZonedTime.MaxEpochNanoseconds - 1, precision: Precision(TemporalUnit.Second), mode: RoundingMode.Ceil));
        Assert.Equal("+275760-09-13T00:00+00:00[UTC]", Zoned(ZonedTime.MaxEpochNanoseconds, "UTC", precision: Precision(TemporalUnit.Minute), mode: RoundingMode.Ceil));
        Assert.False(ExactRounding.TryParseRoundingMode("x", out _));
        Assert.Equal(RoundingMode.Trunc, TemporalOptionRules.DefaultToStringRoundingMode);
    }

    // ---- S7: ZonedDateTime's timeZoneName and offset --------------------------------------------------------

    [Theory]
    [InlineData(ShowTimeZoneName.Auto, ShowOffset.Auto, "2020-07-01T12:00:00.5+02:00[Europe/Berlin][u-ca=japanese]")]
    [InlineData(ShowTimeZoneName.Auto, ShowOffset.Never, "2020-07-01T12:00:00.5[Europe/Berlin][u-ca=japanese]")]
    [InlineData(ShowTimeZoneName.Never, ShowOffset.Auto, "2020-07-01T12:00:00.5+02:00[u-ca=japanese]")]
    [InlineData(ShowTimeZoneName.Never, ShowOffset.Never, "2020-07-01T12:00:00.5[u-ca=japanese]")]
    [InlineData(ShowTimeZoneName.Critical, ShowOffset.Auto, "2020-07-01T12:00:00.5+02:00[!Europe/Berlin][u-ca=japanese]")]
    [InlineData(ShowTimeZoneName.Critical, ShowOffset.Never, "2020-07-01T12:00:00.5[!Europe/Berlin][u-ca=japanese]")]
    public void S7_the_time_zone_annotation_and_the_offset_as_their_options_say(ShowTimeZoneName showTimeZone, ShowOffset showOffset, string expected) =>
        Assert.Equal(expected, Zoned(Ns(Dt(2020, 7, 1, 10, ms: 500)), "Europe/Berlin", "japanese", showTimeZone: showTimeZone, showOffset: showOffset));

    [Fact]
    public void S7_the_option_values_and_a_calendar_annotation_beside_a_critical_zone()
    {
        Assert.True(TemporalOptionRules.TryParseShowTimeZoneName("critical", out var zone));
        Assert.Equal(ShowTimeZoneName.Critical, zone);
        Assert.True(TemporalOptionRules.TryParseShowOffset("never", out var offset));
        Assert.Equal(ShowOffset.Never, offset);
        foreach (var value in new[] { "x", "AUTO", "null", "always", "" })
            Assert.False(TemporalOptionRules.TryParseShowTimeZoneName(value, out _));
        foreach (var value in new[] { "x", "critical", "AUTO", "always" })
            Assert.False(TemporalOptionRules.TryParseShowOffset(value, out _));

        var instant = Ns(Dt(2020, 7, 1, 10));
        Assert.Equal("2020-07-01T12:00:00+02:00[!Europe/Berlin]", Zoned(instant, "Europe/Berlin", showTimeZone: ShowTimeZoneName.Critical));
        Assert.Equal("2020-07-01T12:00:00+02:00[!Europe/Berlin][u-ca=iso8601]",
            Zoned(instant, "Europe/Berlin", showCalendar: ShowCalendarName.Always, showTimeZone: ShowTimeZoneName.Critical));
        Assert.Equal("2020-07-01T12:00:00+02:00[!Europe/Berlin][!u-ca=iso8601]",
            Zoned(instant, "Europe/Berlin", showCalendar: ShowCalendarName.Critical, showTimeZone: ShowTimeZoneName.Critical));
    }

    // ---- S8: sub-minute offsets -----------------------------------------------------------------------------

    [Theory]
    [InlineData("Africa/Monrovia", 1970, 1, "1969-12-31T23:15:30-00:45", "-00:44:30")]
    [InlineData("Europe/Dublin", 1900, 1, "1899-12-31T23:34:39-00:25", "-00:25:21")]
    [InlineData("America/New_York", 1850, 1, "1849-12-31T19:03:58-04:56", "-04:56:02")]
    [InlineData("Asia/Kolkata", 1850, 1, "1850-01-01T05:53:28+05:53", "+05:53:28")]
    [InlineData("Asia/Kolkata", 1900, 1, "1900-01-01T05:21:10+05:21", "+05:21:10")]
    [InlineData("Africa/Abidjan", 1850, 1, "1849-12-31T23:43:52-00:16", "-00:16:08")]
    [InlineData("Europe/Amsterdam", 1930, 6, "1930-06-01T01:00:00+01:00", "+01:00")]
    public void S8_a_strings_offset_is_rounded_to_the_minute_and_the_getters_is_not(string zone, int year, int month, string text, string getter)
    {
        var instant = Ns(Dt(year, month, 1));
        Assert.Equal(text + "[" + zone + "]", Zoned(instant, zone));
        Assert.Equal(text, Instant(instant, zone));
        Assert.Equal(getter, TemporalFormat.FormatUtcOffsetNanoseconds(Offset(zone, instant)));
    }

    [Fact]
    public void S8_the_string_reads_back_to_its_instant_by_matching_the_minutes()
    {
        // ZonedDateTime.from of the string: the candidate whose offset, rounded to the minute, is the string's.
        var monrovia = TimeZoneTests.Rules("Africa/Monrovia");
        Assert.True(TemporalGrammar.TryParse("1969-12-31T23:15:30-00:45[Africa/Monrovia]", [TemporalGoal.ZonedDateTime], out var parsed, out _));
        Assert.False(parsed.OffsetHasSeconds);
        var wall = new IsoDateTime(D(parsed.Year!.Value, parsed.Month, parsed.Day), parsed.Time!.Value);
        var candidates = new BigInteger[ZonedTime.MaxPossibleEpochNanoseconds];
        Assert.Equal(TimeStatus.Completed, ZonedTime.GetPossibleEpochNanoseconds(monrovia, wall, candidates, out var count));
        Assert.Equal(1, count);
        var candidateOffset = IsoArithmetic.GetUtcEpochNanoseconds(wall) - candidates[0];
        Assert.Equal(parsed.OffsetNanoseconds, ExactRounding.RoundNumberToIncrement(candidateOffset, BigInteger.One, 60_000_000_000, RoundingMode.HalfExpand));
        Assert.Equal(BigInteger.Zero, candidates[0]);
    }

    [Theory]
    [InlineData(0L, "+00:00", "+00:00")]
    [InlineData(-1L, "-00:00:00.000000001", "+00:00")]
    [InlineData(29_999_999_999L, "+00:00:29.999999999", "+00:00")]
    [InlineData(30_000_000_000L, "+00:00:30", "+00:01")]
    [InlineData(-30_000_000_000L, "-00:00:30", "-00:01")]
    [InlineData(86_399_999_999_999L, "+23:59:59.999999999", "+24:00")]
    public void S8_the_offsets_two_formats(long offset, string getter, string rounded)
    {
        Assert.Equal(getter, TemporalFormat.FormatUtcOffsetNanoseconds(offset));
        Assert.Equal(rounded, TemporalFormat.FormatDateTimeUtcOffsetRounded(offset));
    }

    // ---- S9: Instant.prototype.toString's timeZone --------------------------------------------------------

    [Theory]
    [InlineData("UTC", "2020-07-01T12:00:00.123+00:00")]
    [InlineData("utc", "2020-07-01T12:00:00.123+00:00")]
    [InlineData("+01:00", "2020-07-01T13:00:00.123+01:00")]
    [InlineData("-00:00", "2020-07-01T12:00:00.123+00:00")]
    [InlineData("+05:30", "2020-07-01T17:30:00.123+05:30")]
    [InlineData("+0530", "2020-07-01T17:30:00.123+05:30")]
    [InlineData("Asia/Kolkata", "2020-07-01T17:30:00.123+05:30")]
    [InlineData("Europe/Berlin", "2020-07-01T14:00:00.123+02:00")]
    [InlineData("2020-01-01T00:00+01:00", "2020-07-01T13:00:00.123+01:00")]
    public void S9_an_instant_writes_the_wall_clock_and_offset_of_its_time_zone_option(string zone, string expected) =>
        Assert.Equal(expected, Instant(Ns(Dt(2020, 7, 1, 12, ms: 123)), zone));

    [Fact]
    public void S9_a_zone_that_is_not_one_and_the_smallest_unit()
    {
        Assert.True(TemporalGrammar.TryParseTimeZoneString("Mars/Olympus", out var mars, out _));
        Assert.False(TimeTablesFixture.Database.TryFind(mars.Name!, out _)); // the adapter's RangeError
        Assert.False(TemporalGrammar.TryParseTimeZoneString("+01:00:01", out _, out _));
        Assert.False(TemporalOptionRules.IsValidToStringSmallestUnit(TemporalUnit.Hour, duration: false));
        Assert.Equal("2020-07-01T17:30+05:30", Instant(Ns(Dt(2020, 7, 1, 12, ms: 123)), "Asia/Kolkata", Precision(TemporalUnit.Minute)));
    }

    // ---- S10: duration strings ----------------------------------------------------------------------------

    [Theory]
    [InlineData("P1Y", "P1Y", "P1YT0S", "P1YT0.000S")]
    [InlineData("-P1Y", "-P1Y", "-P1YT0S", "-P1YT0.000S")]
    [InlineData("P3W4D", "P3W4D", "P3W4DT0S", "P3W4DT0.000S")]
    [InlineData("PT25H", "PT25H", "PT25H0S", "PT25H0.000S")]
    [InlineData("PT61M", "PT61M", "PT61M0S", "PT61M0.000S")]
    [InlineData("PT61S", "PT61S", "PT61S", "PT61.000S")]
    [InlineData("PT0S", "PT0S", "PT0S", "PT0.000S")]
    [InlineData("P4294967295Y", "P4294967295Y", "P4294967295YT0S", "P4294967295YT0.000S")]
    [InlineData("P104249991374D", "P104249991374D", "P104249991374DT0S", "P104249991374DT0.000S")]
    [InlineData("PT2501999792983H", "PT2501999792983H", "PT2501999792983H0S", "PT2501999792983H0.000S")]
    [InlineData("PT9007199254740991S", "PT9007199254740991S", "PT9007199254740991S", "PT9007199254740991.000S")]
    [InlineData("PT9007199254740991.999999999S", "PT9007199254740991.999999999S", "PT9007199254740991S", "PT9007199254740991.999S")]
    [InlineData("-P1DT1H", "-P1DT1H", "-P1DT1H0S", "-P1DT1H0.000S")]
    [InlineData("PT1H0.000000001S", "PT1H0.000000001S", "PT1H0S", "PT1H0.000S")]
    [InlineData("PT1M0S", "PT1M", "PT1M0S", "PT1M0.000S")]
    public void S10_a_duration_written_with_auto_0_and_3_digits(string text, string auto, string zero, string three)
    {
        var fields = ParsedDuration(text);
        Assert.Equal(auto, TemporalFormat.DurationToString(fields, Auto));
        Assert.Equal(zero, Duration(fields, Precision(digits: 0)));
        Assert.Equal(three, Duration(fields, Precision(digits: 3)));
    }

    [Fact]
    public void S10_sub_second_fields_past_a_second_are_written_as_seconds()
    {
        Assert.Equal("PT1.5S", TemporalFormat.DurationToString(Fields(milliseconds: 1500), Auto));
        Assert.Equal("PT1S", Duration(Fields(milliseconds: 1500), Precision(digits: 0)));
        Assert.Equal("PT1S", TemporalFormat.DurationToString(Fields(milliseconds: 1000), Auto));
        Assert.Equal("PT1.000001S", TemporalFormat.DurationToString(Fields(microseconds: 1000001), Auto));
        Assert.Equal("PT1.000000001S", TemporalFormat.DurationToString(Fields(nanoseconds: 1000000001), Auto));
        Assert.Equal("PT2S", TemporalFormat.DurationToString(Fields(seconds: 1, milliseconds: 999, microseconds: 999, nanoseconds: 1000), Auto));
        Assert.Equal("PT9007199254740.991S", TemporalFormat.DurationToString(Fields(milliseconds: 9007199254740991), Auto));
        Assert.Equal("PT9007199254.740S", Duration(Fields(microseconds: 9007199254740991), Precision(digits: 3)));
        Assert.Equal("PT9007199.254740991S", TemporalFormat.DurationToString(Fields(nanoseconds: 9007199254740991), Auto));
        Assert.Equal("PT150119987579016M", TemporalFormat.DurationToString(Fields(minutes: 150119987579016), Auto));
        Assert.Equal(TemporalStatus.InvalidDuration, TemporalLimits.CheckDuration(Fields(milliseconds: 9007199254740992000)));
        Assert.Throws<ArgumentOutOfRangeException>(() => TemporalFormat.DurationToString(Fields(milliseconds: 9007199254740992000), Auto));
    }

    [Theory]
    [InlineData("PT59.999999999S", RoundingMode.Ceil, "PT60S")]
    [InlineData("PT59.5S", RoundingMode.HalfExpand, "PT60S")]
    [InlineData("PT59M59.5S", RoundingMode.HalfExpand, "PT60M0S")]
    [InlineData("PT23H59M59.5S", RoundingMode.HalfExpand, "PT24H0S")]
    [InlineData("P1DT23H59M59.5S", RoundingMode.HalfExpand, "P2DT0S")]
    [InlineData("P1WT23H59M59.5S", RoundingMode.HalfExpand, "P1W1DT0S")]
    [InlineData("P1YT23H59M59.5S", RoundingMode.HalfExpand, "P1Y1DT0S")]
    [InlineData("PT0.5S", RoundingMode.HalfEven, "PT0S")]
    [InlineData("PT1.5S", RoundingMode.HalfEven, "PT2S")]
    [InlineData("-PT59.5S", RoundingMode.HalfExpand, "-PT60S")]
    [InlineData("-PT59.5S", RoundingMode.Floor, "-PT60S")]
    [InlineData("-PT59.5S", RoundingMode.Ceil, "-PT59S")]
    [InlineData("PT9007199254740991.999999999S", RoundingMode.Ceil, "OutOfRange")]
    [InlineData("PT9007199254740991.5S", RoundingMode.HalfExpand, "OutOfRange")]
    [InlineData("PT9007199254740990.5S", RoundingMode.HalfEven, "PT9007199254740990S")]
    public void S10_rounding_carries_across_units_and_stops_at_the_limit(string text, RoundingMode mode, string expected)
    {
        var fields = ParsedDuration(text);
        Assert.Equal(expected, Duration(fields, Precision(digits: 0), mode));
        Assert.Equal(expected, Duration(fields, Precision(TemporalUnit.Second), mode));
    }

    [Fact]
    public void S10_rounding_to_hundredths_and_a_duration_past_the_limit_at_creation()
    {
        Assert.Equal("PT2H0.00S", Duration(ParsedDuration("PT1H59M59.999S"), Precision(digits: 2), RoundingMode.Ceil));
        Assert.Equal("PT2H0S", Duration(ParsedDuration("PT1H59M59.999S"), Precision(TemporalUnit.Second), RoundingMode.Ceil));
        Assert.Equal(TemporalStatus.InvalidDuration, TemporalLimits.CheckDuration(ParsedDuration("PT2501999792983H59M59.999999999S")));
    }

    // ---- S11: the reads are the adapter's, in its order; the rules are independent --------------------------

    [Fact]
    public void S11_each_rule_answers_on_its_own_and_smallest_unit_is_validated_after_every_read()
    {
        // An invalid value is refused by its own rule when the adapter reads it, whatever the other options are.
        Assert.False(TemporalOptionRules.TryParseShowCalendarName("x", out _));
        Assert.False(TemporalOptionRules.TryParseFractionalSecondDigits(10, out _));
        Assert.False(ExactRounding.TryParseRoundingMode("x", out _));
        Assert.False(TemporalOptionRules.TryParseUnit("x", out _));
        Assert.False(TemporalOptionRules.TryParseShowTimeZoneName("x", out _));
        Assert.False(TemporalOptionRules.TryParseShowOffset("x", out _));

        // smallestUnit is read as any unit or auto, and its group is checked only after the last read.
        Assert.True(TemporalOptionRules.TryParseUnit("year", out var year));
        Assert.True(TemporalOptionRules.TryParseUnit("hour", out var hour));
        Assert.False(TemporalOptionRules.IsValidToStringSmallestUnit(year, duration: false));
        Assert.False(TemporalOptionRules.IsValidToStringSmallestUnit(hour, duration: false));
    }

    [Fact]
    public void Preconditions_of_the_formatters()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TemporalFormat.TimeToString(T(24), Auto));
        Assert.Throws<ArgumentOutOfRangeException>(() => TemporalFormat.FormatFractionalSeconds(1_000_000_000, Auto));
        Assert.Throws<ArgumentOutOfRangeException>(() => TemporalFormat.FormatFractionalSeconds(0, TemporalFormat.MinutePrecision));
        Assert.Throws<ArgumentOutOfRangeException>(() => TemporalFormat.InstantToString(0, TemporalLimits.NanosecondsPerDay, Auto));
        Assert.Throws<ArgumentNullException>(() => TemporalFormat.DateToString(D(2020, 1, 1), null!, ShowCalendarName.Auto));
        Assert.Throws<ArgumentOutOfRangeException>(() => TemporalFormat.TryDurationToString(Fields(), Precision(TemporalUnit.Minute), RoundingMode.Trunc, out _));
        Assert.Equal(".000000001", TemporalFormat.FormatFractionalSeconds(1, Auto));
        Assert.Equal(".00", TemporalFormat.FormatFractionalSeconds(1, 2));
        Assert.Equal(string.Empty, TemporalFormat.FormatFractionalSeconds(123, 0));
        Assert.Equal("23:59", TemporalFormat.FormatTimeString(23, 59, 59, 999_999_999, TemporalFormat.MinutePrecision));
    }
}
