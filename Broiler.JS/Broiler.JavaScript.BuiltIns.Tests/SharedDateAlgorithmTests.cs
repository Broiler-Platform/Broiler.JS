using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using Broiler.JavaScript.BuiltIns.Date;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// The Date area's corrections and policies that the shared corpus does not hold (SJL-6.5): its vectors
// do not depend on the zone, the culture or the host, and hold neither a structured clone nor the CLR
// views. Each correction failed before the cut-over and passes after it. The suite runs in UTC (the
// TestCultureInitializer); the cases that need a zone with daylight-saving time are in
// SharedDateAlgorithmNewYorkTests below.
public class SharedDateAlgorithmTests
{
    internal static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    internal static string Eval(string source, JavaScriptFeatureFlags features = JavaScriptFeatureFlags.None)
    {
        Load();
        using var ctx = new JSContext(experimentalFeatures: features);
        return ctx.Eval(source).ToString();
    }

    // D4: getTimezoneOffset reads a date outside the years 1 to 9999, where it had no path for one.
    [Fact]
    public void GetTimezoneOffset_reads_a_date_outside_years_1_to_9999()
        => Assert.Equal("0", Eval("new Date(-1e15).getTimezoneOffset()"));

    // T3, for setYear (B.2.3.2): the time value is read before the argument is converted, where it was
    // read after, so a valueOf that moved the date set the year of the moved date.
    [Fact]
    public void SetYear_reads_the_time_value_before_its_argument()
        => Assert.Equal("1999-01-01T00:00:00.000Z", Eval(
            "var d = new Date(Date.UTC(2000, 0, 1)); d.setYear({ valueOf() { d.setTime(86400000 * 40); return 99; } }); d.toISOString()"));

    // D5: a structured clone copies the time value, where it copied only the DateTimeOffset storage.
    [Theory]
    [InlineData("structuredClone(new Date(-1e15)).getTime()", "-1000000000000000")]
    [InlineData("structuredClone(new Date(-62135596800000)).getTime()", "-62135596800000")]
    [InlineData("structuredClone(new Date(NaN)).getTime()", "NaN")]
    public void A_structured_clone_keeps_the_time_value(string expression, string expected)
        => Assert.Equal(expected, Eval(expression, JavaScriptFeatureFlags.StructuredClone));

    // P8: the engine's toUTCString reads back under a culture whose calendar is not the Gregorian one,
    // where the culture's parse read its year in that calendar.
    [Theory]
    [InlineData("fa-IR")]
    [InlineData("ar-SA")]
    public void ToUTCString_reads_back_under_any_culture(string culture)
    {
        var saved = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            Assert.Equal("true", Eval("var d = new Date(Date.UTC(2021, 5, 7, 8, 9, 10)); Date.parse(d.toUTCString()) === d.getTime()"));
        }
        finally
        {
            CultureInfo.CurrentCulture = saved;
        }
    }

    // P4 and P6, decided at SJL-6.5: the format's leniencies this engine keeps in its own fallback, a
    // fraction of none to nine digits and an offset without its colon, in every form of the format;
    // lower-case t keeps the culture's reading. Outside them, a 'T'-joined string is not read leniently.
    [Theory]
    [InlineData("Date.parse('2021-03-04T10:00:00.1Z')", "1614852000100")]
    [InlineData("Date.parse('2021-03-04T10:00:00.123456789Z')", "1614852000123")]
    [InlineData("Date.parse('0001-01-01T00:00:00.-05:30')", "-62135577000000")]
    [InlineData("Date.parse('0000T00:00:00.0000Z')", "-62167219200000")]
    [InlineData("Date.parse('0001-01-01T00:00-0530')", "-62135577000000")]
    [InlineData("Date.parse('-000001-01-01T00:00:00.1+0100')", "-62198758799900")]
    [InlineData("Date.parse('0001-01-01t00:00-05:30')", "-62135577000000")]
    [InlineData("Date.parse('1997-3-8T11:19:20')", "NaN")]
    [InlineData("Date.parse('1997-03-08T11:19:10-07')", "NaN")]
    [InlineData("Date.parse('2021-03-04T10:00:00.1234567890Z')", "NaN")]
    public void The_format_leniencies_are_kept(string expression, string expected)
        => Assert.Equal(expected, Eval(expression));

    // The order of grammars (JSD-0072): what the format and this engine's fallback both refuse,
    // Broiler.VM's dialect reads, among it this engine's renderings of the years outside 0001 to 9999
    // (P9) and a day past its month's length, carried (P5).
    [Theory]
    [InlineData("var d = new Date(Date.UTC(-1, 0, 1)); Date.parse(String(d)) === d.getTime() && Date.parse(d.toUTCString()) === d.getTime()", "true")]
    [InlineData("var d = new Date(Date.UTC(10000, 0, 1)); Date.parse(String(d)) === d.getTime() && Date.parse(d.toUTCString()) === d.getTime()", "true")]
    [InlineData("Date.parse('2021-02-30')", "1614643200000")]
    [InlineData("Date.parse('3/4/21')", "1614816000000")]
    public void Date_parse_reads_the_grammars_in_order(string expression, string expected)
        => Assert.Equal(expected, Eval(expression));

    // R5, decided at SJL-6.5: a .NET format string formats a date from the year 1 to the year 9999 only;
    // outside them the extension is a RangeError, where it answered the invalid-date sentinel's fields.
    [Theory]
    [InlineData("try { new Date(Date.UTC(-1, 0, 1)).toLocaleString(undefined, 'yyyy-MM-dd') } catch (e) { e.constructor.name }", "RangeError")]
    [InlineData("new Date(Date.UTC(2021, 6, 1, 12)).toLocaleString(undefined, 'yyyy-MM-dd HH:mm')", "2021-07-01 12:00")]
    [InlineData("new Date(NaN).toLocaleString(undefined, 'yyyy-MM-dd')", "Invalid Date")]
    public void A_net_format_string_formats_years_1_to_9999(string expression, string expected)
        => Assert.Equal(expected, Eval(expression));

    // A Gregorian-family PlainMonthDay with a year is bound-checked with its month as constrain
    // regulates it, where the civil copy the check called carried a month past 12 past the year.
    [Theory]
    [InlineData("var md = Temporal.PlainMonthDay.from({ calendar: 'gregory', year: 275759, month: 21, day: 13 }); md.monthCode + ' ' + md.day", "M12 13")]
    [InlineData("try { Temporal.PlainMonthDay.from({ calendar: 'gregory', year: 275759, month: 21, day: 13 }, { overflow: 'reject' }) } catch (e) { e.constructor.name }", "RangeError")]
    [InlineData("try { Temporal.PlainMonthDay.from({ calendar: 'gregory', year: 999999, month: 2, day: 29 }) } catch (e) { e.constructor.name }", "RangeError")]
    public void A_month_day_is_bound_checked_with_its_regulated_month(string expression, string expected)
        => Assert.Equal(expected, Eval(expression));

    // Decision SJL-D-25: the CLR views keep their signatures over the time value.
    [Fact]
    public void The_clr_views_read_the_time_value()
    {
        Load();
        using var ctx = new JSContext();

        // The view is the instant at the host's offset, UTC here, and the date is its time value.
        var instant = new DateTimeOffset(2021, 7, 1, 12, 0, 0, TimeSpan.FromHours(2));
        var date = new JSDate(instant);
        Assert.Equal(instant, date.Value);
        Assert.Equal(TimeSpan.Zero, date.Value.Offset);
        Assert.Equal("Thu Jul 01 2021 10:00:00 GMT+0000 (Coordinated Universal Time)", date.ToDetailString());
        Assert.True(date.ConvertTo(typeof(DateTimeOffset), out var view));
        Assert.Equal(instant, (DateTimeOffset)view);

        // DateTimeOffset.MinValue still makes an invalid date, while script reads the instant
        // 0001-01-01T00:00Z as a valid one (D3).
        Assert.Equal("Invalid Date", new JSDate(DateTimeOffset.MinValue).ToDetailString());
        Assert.Equal("-62135596800000", ctx.Eval("new Date(-62135596800000).getTime()").ToString());

        // A valid date outside DateTimeOffset's range has the sentinel for its view and converts to no
        // CLR date.
        var far = (JSDate)ctx.Eval("new Date(-1e15)");
        Assert.Equal(DateTimeOffset.MinValue, far.Value);
        Assert.False(far.ConvertTo(typeof(DateTime), out _));
        Assert.False(far.ConvertTo(typeof(DateTimeOffset), out _));
        Assert.True(far.ConvertTo(typeof(object), out var self));
        Assert.Same(far, self);
    }
}

[CollectionDefinition(nameof(NewYorkZoneCollection), DisableParallelization = true)]
public class NewYorkZoneCollection
{
}

// The corrections that need a zone with daylight-saving time, under America/New_York, which this
// collection sets as the process's local zone and then gives back; it runs alone, after the parallel
// collections.
[Collection(nameof(NewYorkZoneCollection))]
public sealed class SharedDateAlgorithmNewYorkTests : IDisposable
{
    public SharedDateAlgorithmNewYorkTests()
    {
        // TZ selects the local zone on Unix only, and Windows reads it from the system, so the zone is also
        // set in TimeZoneInfo's cache, which TimeZoneInfo.Local reads on every platform. The cache fills its
        // local zone under its own lock, from another thread too (the runner reads the clock), so the zone
        // is set under that lock, where no fill in progress can overwrite it.
        Environment.SetEnvironmentVariable("TZ", "America/New_York");
        TimeZoneInfo.ClearCachedData();
        var zone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
        var cache = typeof(TimeZoneInfo).GetField("s_cachedData", BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null);
        var local = cache?.GetType().GetField("_localTimeZone", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("TimeZoneInfo's cache of the local zone was not found.");
        lock (cache)
            local.SetValue(cache, zone);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("TZ", "UTC");
        TimeZoneInfo.ClearCachedData();
    }

    private static string Eval(string source) => SharedDateAlgorithmTests.Eval(source);

    // L2: one offset per instant, the one in force at it, for storage, the components constructor, the
    // getters and toString alike.
    [Theory]
    [InlineData("var d = new Date(2021, 6, 1); d.getHours() + ' ' + d.toISOString()", "0 2021-07-01T04:00:00.000Z")]
    [InlineData("new Date(Date.UTC(2021, 6, 1, 12)).getHours()", "8")]
    [InlineData("String(new Date(2021, 6, 1)).slice(0, 33)", "Thu Jul 01 2021 00:00:00 GMT-0400")]
    [InlineData("var d = new Date(2021, 0, 1, 12); d.setMonth(6); d.getHours() + ' ' + d.toISOString()", "12 2021-07-01T16:00:00.000Z")]
    [InlineData("new Date(2021, 6, 1).getTimezoneOffset() + ' ' + new Date(2021, 0, 1).getTimezoneOffset()", "240 300")]
    public void Local_time_takes_the_offset_in_force_at_the_instant(string expression, string expected)
        => Assert.Equal(expected, Eval(expression));

    // L3: a skipped local time is read at the offset before the transition, a repeated one at its
    // earlier instant, and a local time after a transition at the offset after it.
    [Theory]
    [InlineData("var d = new Date(2021, 2, 14, 2, 30); d.toISOString() + ' ' + d.getHours()", "2021-03-14T07:30:00.000Z 3")]
    [InlineData("new Date(2021, 10, 7, 1, 30).toISOString()", "2021-11-07T05:30:00.000Z")]
    [InlineData("new Date(Date.parse('2021-03-14T03:30:00.0000')).toISOString()", "2021-03-14T07:30:00.000Z")]
    public void Skipped_and_repeated_local_times_are_disambiguated(string expression, string expected)
        => Assert.Equal(expected, Eval(expression));

    // L4: a date outside DateTimeOffset's range takes the offset the zone's rules give at its instant,
    // where it took the offset in force at the moment of the call.
    [Theory]
    [InlineData("new Date(10000, 0, 1).toISOString()", "+010000-01-01T05:00:00.000Z")]
    [InlineData("new Date('+010000-07-01T00:00').toISOString()", "+010000-07-01T04:00:00.000Z")]
    [InlineData("new Date(-1e15).getTimezoneOffset() > 240", "true")]
    public void A_date_outside_years_1_to_9999_takes_its_own_offset(string expression, string expected)
        => Assert.Equal(expected, Eval(expression));

    // P1: a date-only form is UTC, where it was read as local time for the years 0001 to 9999.
    [Theory]
    [InlineData("Date.parse('2021-03-04')", "1614816000000")]
    [InlineData("Date.parse('2021-03')", "1614556800000")]
    [InlineData("Date.parse('2021')", "1609459200000")]
    [InlineData("Date.parse('+002021-03-04')", "1614816000000")]
    public void A_date_only_form_is_UTC(string expression, string expected)
        => Assert.Equal(expected, Eval(expression));

    // R3: toString's and toTimeString's offset is written with U+002D whatever the culture, where the
    // culture's negative sign was written, and it reads back.
    [Theory]
    [InlineData("sv-SE")]
    [InlineData("fa-IR")]
    [InlineData("ar-SA")]
    public void The_offset_is_written_with_a_hyphen_minus(string culture)
    {
        var saved = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            Assert.Equal("GMT-0500 GMT-0500 true", Eval(
                "var d = new Date(Date.UTC(2021, 0, 1, 12)); /GMT[^ ]+/.exec(String(d))[0] + ' ' + " +
                "/GMT[^ ]+/.exec(d.toTimeString())[0] + ' ' + (Date.parse(String(d)) === d.getTime())"));
        }
        finally
        {
            CultureInfo.CurrentCulture = saved;
        }
    }
}
