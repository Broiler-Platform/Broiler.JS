using Broiler.JavaScript.Engine;
using Xunit;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/794
//
// Fixed here:
//   * Problem 15 — a Temporal string annotation key must be lowercase: PlainDate/PlainDateTime/
//     PlainMonthDay/PlainTime now call RejectMalformedAnnotations (previously only PlainYearMonth
//     did), so a capitalized key such as [U-CA=iso8601] is a RangeError.
//   * Problems 2/11/13/14 — ZonedDateTime.add/subtract perform the wall-clock date arithmetic in
//     the receiver's calendar (TemporalNonIso.AddToIso) for the non-ISO calendars, instead of always
//     using ISO arithmetic; chinese/dangi/islamic/coptic/ethiopic etc. now keep the day-of-month.
//   * Problem 12 — the Hebrew leap month Adar I (M05L) constrains to Adar (M06), not Shevat (M05),
//     when added/subtracted into a common year (ResolveMonthAfterYearShift).
//   * Problems 9/10/19/20 — non-ISO since/until project the start month into the candidate year by
//     monthCode (not raw ordinal), so a whole-year difference across a leap-month boundary no longer
//     reports a spurious ±1 month.
//   * Problem 8 — Temporal.PlainDate from/compare/equals/since/until consume a ZonedDateTime via its
//     internal slots (its wall-clock ISO date in its own time zone + calendar), not its getters.
public class Issue794Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    private static string ErrorName(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval($"var c='NONE'; try {{ {code}; }} catch (e) {{ c = e.constructor.name; }} c").ToString();
    }

    // ── Problem 15: annotation keys must be lowercase ─────────────────────────────

    [Theory]
    [InlineData("Temporal.PlainDateTime.from('1970-01-01T00:00[U-CA=iso8601]')")]
    [InlineData("Temporal.PlainDate.from('1970-01-01[U-CA=iso8601]')")]
    [InlineData("Temporal.PlainMonthDay.from('--01-01[U-CA=iso8601]')")]
    [InlineData("Temporal.PlainTime.from('12:00[FOO=bar]')")]
    public void Annotation_UppercaseKey_Throws(string code)
        => Assert.Equal("RangeError", ErrorName(code));

    [Fact]
    public void Annotation_LowercaseKey_StillAccepted()
        => Assert.Equal(
            "1970-01-01T00:00:00",
            Eval("Temporal.PlainDateTime.from('1970-01-01T00:00[u-ca=iso8601]').toString()"));

    // ── Problems 2/13: ZonedDateTime add uses the calendar's date arithmetic ──────

    [Fact]
    public void ZonedDateTime_AddYear_Chinese_KeepsDay()
    {
        const string code = @"
            var zdt = Temporal.ZonedDateTime.from({year:2022,monthCode:'M01',day:1,hour:0,timeZone:'UTC',calendar:'chinese'});
            var r = zdt.add({years:1});
            r.day + ',' + r.monthCode";
        Assert.Equal("1,M01", Eval(code));
    }

    [Fact]
    public void ZonedDateTime_AddMonth_Ethiopic_KeepsDay30()
    {
        const string code = @"
            var zdt = Temporal.ZonedDateTime.from({year:2000,monthCode:'M01',day:30,hour:0,timeZone:'UTC',calendar:'ethiopic'});
            var r = zdt.add({months:1});
            r.day + ',' + r.monthCode";
        Assert.Equal("30,M02", Eval(code));
    }

    // ── Problem 11: islamic era boundary (1 year before Hijra keeps the day) ───────

    [Fact]
    public void ZonedDateTime_AddYear_IslamicCivil_AcrossEra_KeepsDay()
    {
        const string code = @"
            var zdt = Temporal.ZonedDateTime.from({year:-1,monthCode:'M01',day:15,hour:0,timeZone:'UTC',calendar:'islamic-civil'});
            var r = zdt.add({years:1});
            r.day + ',' + r.era";
        Assert.Equal("15,bh", Eval(code));
    }

    // ── Problem 12: Hebrew Adar I (M05L) add/subtract constrains to Adar (M06) ─────

    [Fact]
    public void Hebrew_AddYear_AdarI_ToCommonYear_ConstrainsToAdar()
    {
        const string code = @"
            var pd = Temporal.PlainDate.from({year:5779,monthCode:'M05L',day:1,calendar:'hebrew'});
            pd.add({years:1}, {overflow:'constrain'}).monthCode";
        Assert.Equal("M06", Eval(code));
    }

    [Fact]
    public void Hebrew_AddYear_AdarI_ToCommonYear_RejectThrows()
        => Assert.Equal("RangeError", ErrorName(
            "Temporal.PlainDate.from({year:5779,monthCode:'M05L',day:1,calendar:'hebrew'}).add({years:1}, {overflow:'reject'})"));

    // ── Problem 3: chinese calendar below the .NET span (≤1900) ───────────────────
    // The astronomical fallback (CldrChineseCalendar) covers years the .NET
    // ChineseLunisolarCalendar (1901–2100) cannot.

    [Fact] // 1900-01-01 ISO is the well-known 己亥年十二月初一 (chinese 1899, M12, day 1)
    public void Chinese1899_M12_Day1_IsoIs1900_01_01()
        => Assert.Equal("1900-01-01", Eval(
            "Temporal.PlainDate.from({year:1899,monthCode:'M12',day:1,calendar:'chinese'}).withCalendar('iso8601').toString()"));

    [Fact] // reverse: ISO 1900-01-01 read in the chinese calendar
    public void Chinese_Iso1900_01_01_Fields()
        => Assert.Equal("1899,12,M12,1,false", Eval(
            "(function(){var d=Temporal.PlainDate.from('1900-01-01[u-ca=chinese]');" +
            "return [d.year,d.month,d.monthCode,d.day,d.inLeapYear].join(',');})()"));

    [Fact] // chinese 1900 is a leap year with a leap 8th month (M08L), 384 days
    public void Chinese1900_HasLeapEighthMonth()
        => Assert.Equal("M08L,9,384", Eval(
            "(function(){var d=Temporal.PlainDate.from({year:1900,monthCode:'M08L',day:1,calendar:'chinese'});" +
            "return [d.monthCode,d.month,d.daysInYear].join(',');})()"));

    [Fact] // the fallback meets the .NET calendar seamlessly: 1901 new year = 1901-02-19
    public void Chinese1901_NewYear_BoundaryWithDotNet()
        => Assert.Equal("1901-02-19", Eval(
            "Temporal.PlainDate.from({year:1901,monthCode:'M01',day:1,calendar:'chinese'}).withCalendar('iso8601').toString()"));

    // ── Problem 4: PlainMonthDay for non-ISO calendars ───────────────────────────
    // The reference year is the ISO year of the reference date: the latest occurrence on or before
    // ISO 1972-12-31, else the earliest after.

    [Theory] // regular chinese month codes: day 1/29 -> 1972; day 30 -> latest year it has 30 days
    [InlineData("M01", 1, "M01", 1, 1972)]
    [InlineData("M01", 29, "M01", 29, 1972)]
    [InlineData("M01", 30, "M01", 30, 1970)]
    [InlineData("M02", 30, "M02", 30, 1972)]
    [InlineData("M11", 29, "M11", 29, 1972)] // day 29 of the 11th month rolls into the next ISO year
    public void Chinese_RegularMonthCode_ReferenceYear(string code, int day, string expCode, int expDay, int expYear)
        => Assert.Equal($"{expCode},{expDay},{expYear}", Eval(
            $"(function(){{var d=Temporal.PlainMonthDay.from({{calendar:'chinese',monthCode:'{code}',day:{day}}});" +
            "return [d.monthCode,d.day,d.toString().slice(0,4)].join(',');})()"));

    [Theory] // rare leap months whose first occurrence is in the future use a future reference year
    [InlineData("M02L", 1, 1947)]
    [InlineData("M05L", 1, 1971)]
    [InlineData("M10L", 1, 1984)]
    [InlineData("M11L", 1, 2033)]
    [InlineData("M11L", 29, 2034)] // day 29 rolls into the next ISO year
    public void Chinese_LeapMonthCode_ReferenceYear(string code, int day, int expYear)
        => Assert.Equal($"{code},{day},{expYear}", Eval(
            $"(function(){{var d=Temporal.PlainMonthDay.from({{calendar:'chinese',monthCode:'{code}',day:{day}}});" +
            "return [d.monthCode,d.day,d.toString().slice(0,4)].join(',');})()"));

    [Fact] // a leap month that never reaches 30 days constrains to its regular month
    public void Chinese_LeapMonthWithoutDay30_ConstrainsToRegularMonth()
        => Assert.Equal("M02,30", Eval(
            "(function(){var d=Temporal.PlainMonthDay.from({calendar:'chinese',monthCode:'M02L',day:30});" +
            "return d.monthCode+','+d.day;})()"));

    [Fact] // an out-of-range month code is a RangeError for chinese
    public void Chinese_InvalidMonthCode_Throws()
        => Assert.Equal("RangeError", ErrorName("Temporal.PlainMonthDay.from({calendar:'chinese',monthCode:'M15',day:1})"));

    [Theory] // a day one past a leap day constrains to the leap day, across the non-ISO calendars
    [InlineData("hebrew", "M02", 31)]
    [InlineData("islamic-civil", "M01", 31)]
    [InlineData("coptic", "M13", 7)]
    [InlineData("persian", "M12", 31)]
    [InlineData("chinese", "M01", 31)]
    public void NonIso_ConstrainPastLeapDay(string calendar, string code, int day)
        => Assert.Equal((day - 1).ToString(), Eval(
            $"String(Temporal.PlainMonthDay.from({{calendar:'{calendar}',monthCode:'{code}',day:{day}}}).day)"));

    // ── Problems 9/10: whole-year non-ISO difference reports 0 residual months ─────

    [Theory]
    [InlineData("until")]
    [InlineData("since")]
    public void Chinese_TenYearDifference_HasZeroMonths(string method)
    {
        var code = $@"
            var a = Temporal.PlainDate.from({{year:1990,monthCode:'M05',day:10,calendar:'chinese'}});
            var b = a.add({{years:10}});
            var d = {(method == "until" ? "a.until(b" : "b.since(a")}, {{largestUnit:'years'}});
            d.years + ',' + d.months + ',' + d.days";
        Assert.Equal("10,0,0", Eval(code));
    }

    // ── Problem 8: PlainDate consumes a ZonedDateTime via its slots ───────────────

    [Fact] // ZDT at epoch 0 in UTC -> 1970-01-01
    public void PlainDateFrom_ZonedDateTime_Utc()
        => Assert.Equal("1970-01-01", Eval("Temporal.PlainDate.from(new Temporal.ZonedDateTime(0n,'UTC')).toString()"));

    [Fact] // the wall-clock date is taken in the ZDT's own zone (offset -08:00 -> previous ISO day)
    public void PlainDateFrom_ZonedDateTime_HonoursZone()
        => Assert.Equal("1969-12-31", Eval("Temporal.PlainDate.from(new Temporal.ZonedDateTime(0n,'-08:00')).toString()"));

    [Fact]
    public void PlainDateCompare_AcceptsZonedDateTime()
        => Assert.Equal("0", Eval(
            "String(Temporal.PlainDate.compare(new Temporal.ZonedDateTime(0n,'UTC'), Temporal.PlainDate.from('1970-01-01')))"));

    [Fact]
    public void PlainDateEquals_AcceptsZonedDateTime()
        => Assert.Equal("true", Eval(
            "String(Temporal.PlainDate.from('1970-01-01').equals(new Temporal.ZonedDateTime(0n,'UTC')))"));

    [Fact]
    public void PlainDateUntil_AcceptsZonedDateTime()
        => Assert.Equal("P1D", Eval(
            "Temporal.PlainDate.from('1970-01-01').until(new Temporal.ZonedDateTime(86400000000000n,'UTC')).toString()"));

    [Fact] // conversion must read slots, not getters: a clobbered prototype getter must not be invoked
    public void PlainDateFrom_ZonedDateTime_DoesNotCallGetters()
        => Assert.Equal("1970-01-01", Eval(@"
            var zdt = new Temporal.ZonedDateTime(0n, 'UTC');
            Object.defineProperty(Temporal.ZonedDateTime.prototype, 'day', { configurable: true, get() { throw new Error('getter called'); } });
            Temporal.PlainDate.from(zdt).toString();"));

    // ── Problem 7: boundary arithmetic must not spuriously throw "result is out of range" ─────

    private const string MinDate = "Temporal.PlainDate.from({year:-271821, month:4, day:19})";
    private const string MaxDate = "Temporal.PlainDate.from({year:275760, month:9, day:13})";

    [Theory] // adding the maximum duration to the minimum date lands exactly on the maximum date
    [InlineData("{seconds:17280000172799, nanoseconds:999999998}")] // time units scaled to ns overflow Int64
    [InlineData("{hours:4800000047, minutes:59, seconds:59, milliseconds:999, microseconds:999, nanoseconds:999}")]
    [InlineData("{days:200000001, nanoseconds:86399999999999}")]
    public void PlainDateAdd_MaxDurationToMinDate_LandsOnMax(string dur)
        => Assert.Equal("+275760-09-13", Eval($"{MinDate}.add({dur}).toString()"));

    [Fact] // and the symmetric subtract
    public void PlainDateSubtract_MaxDurationFromMaxDate_LandsOnMin()
        => Assert.Equal("-271821-04-19", Eval(
            $"{MaxDate}.subtract({{seconds:17280000172799, nanoseconds:999999998}}).toString()"));

    [Fact] // a fractional rounding increment is truncated (2.5 -> 2): 5 days trunc to a multiple of 2 -> 4
    public void PlainDateSince_FractionalIncrement_Truncates()
        => Assert.Equal("P4D", Eval(
            "new Temporal.PlainDate(2000,5,7).since(new Temporal.PlainDate(2000,5,2), {roundingIncrement: 2.5, roundingMode: 'trunc'}).toString()"));

    [Fact] // a huge day increment rounds the day difference without building an out-of-range date
    public void PlainDateSince_HugeDayIncrement_RoundsToDayCount()
        => Assert.Equal("P1000000000D", Eval(
            "new Temporal.PlainDate(2000,5,7).since(new Temporal.PlainDate(2000,5,2), {smallestUnit:'days', roundingIncrement: 1e9 + 0.5, roundingMode:'expand'}).toString()"));

    [Theory] // PlainYearMonth since/until accepts strings at the ISO limits (exact diff, no end boundary built)
    [InlineData("+275760-09")]
    [InlineData("+275760-09-30T23:59:59.999999999")]
    [InlineData("-271821-05")]
    public void PlainYearMonthSince_BoundaryString_DoesNotThrow(string arg)
        => Assert.Equal("ok", Eval(
            $"new Temporal.PlainYearMonth(1970,1).since('{arg}'); 'ok'"));

    // ── PlainDateTime since/until rounding (unlocks the P1 rounded-out-of-range case) ─────────

    [Fact] // rounding to a coarse unit at a huge increment builds an out-of-range boundary -> RangeError
    public void PlainDateTimeSince_RoundedDateOutOfRange_Throws()
        => Assert.Equal("RangeError", ErrorName(
            "new Temporal.PlainDateTime(1970,1,1).since(new Temporal.PlainDateTime(1971,1,1), {roundingIncrement:100000000, smallestUnit:'months'})"));

    [Theory] // calendar-unit rounding; default largestUnit is the larger of "day" and smallestUnit
    [InlineData("year", "halfExpand", "P2Y")]    // 1y7m, smallestUnit year -> 2y
    [InlineData("month", "trunc", "P19M")]       // largestUnit defaults to month -> 19 months (exact)
    public void PlainDateTimeUntil_CalendarRounding(string smallest, string mode, string expected)
        => Assert.Equal(expected, Eval(
            $"new Temporal.PlainDateTime(2000,1,1).until(new Temporal.PlainDateTime(2001,8,1), {{smallestUnit:'{smallest}', roundingMode:'{mode}'}}).toString()"));

    [Fact] // a day rounding with a sub-day remainder past the half mark rounds up
    public void PlainDateTimeUntil_DayRounding_FoldsTime()
        => Assert.Equal("P1D", Eval(
            "new Temporal.PlainDateTime(2000,1,1,10).until(new Temporal.PlainDateTime(2000,1,2,8), {smallestUnit:'day', roundingMode:'halfExpand'}).toString()"));

    [Theory] // time-unit rounding of a 1-day-5h40m difference
    [InlineData("minute", 30, "halfExpand", "days", "P1DT5H30M")] // residual 5h40m -> 5h30m, date kept
    [InlineData("hour", 1, "halfExpand", "hours", "PT30H")]        // 29h40m -> 30h (days folded into hours)
    public void PlainDateTimeUntil_TimeRounding(string smallest, int inc, string mode, string largest, string expected)
        => Assert.Equal(expected, Eval(
            $"new Temporal.PlainDateTime(2000,1,1,0,0,0).until(new Temporal.PlainDateTime(2000,1,2,5,40,0), {{largestUnit:'{largest}', smallestUnit:'{smallest}', roundingIncrement:{inc}, roundingMode:'{mode}'}}).toString()"));

    [Fact] // since negates the rounding mode and the result
    public void PlainDateTimeSince_NegatesRounding()
        => Assert.Equal("PT1H30M", Eval(
            "new Temporal.PlainDateTime(2000,1,1,1,20,0).since(new Temporal.PlainDateTime(2000,1,1,0,0,0), {smallestUnit:'minute', roundingIncrement:30, roundingMode:'halfExpand'}).toString()"));

    [Fact] // the default (no rounding) result is unchanged
    public void PlainDateTimeUntil_DefaultUnchanged()
        => Assert.Equal("P399DT4H5M6S", Eval(
            "new Temporal.PlainDateTime(2000,1,1,0,0,0).until(new Temporal.PlainDateTime(2001,2,3,4,5,6)).toString()"));

    [Theory] // since/until validate the rounding increment divides its unit
    [InlineData("{smallestUnit:'minute', roundingIncrement:29}")]
    [InlineData("{largestUnit:'minutes', smallestUnit:'hours'}")]
    public void PlainDateTimeSince_InvalidRounding_Throws(string opts)
        => Assert.Equal("RangeError", ErrorName(
            $"new Temporal.PlainDateTime(2000,1,1).since(new Temporal.PlainDateTime(2001,1,1), {opts})"));
}
