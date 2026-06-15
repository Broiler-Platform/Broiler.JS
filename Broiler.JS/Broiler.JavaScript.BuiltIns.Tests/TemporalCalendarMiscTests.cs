using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Assorted Temporal calendar/parsing fixes from issue #800:
//   • problem 9: a Temporal ISO string (including the basic, no-hyphen date form) is accepted as a
//     calendar slot value and yields its annotation (default iso8601).
//   • problem 8: the Japanese calendar's Meiji era is numbered from 1868 but only begins at the
//     1873 Gregorian adoption, so Meiji 1-5 display as the Gregorian ce era.
//   • problems 18/19: weekOfYear / yearOfWeek are undefined for every non-ISO calendar.
public class TemporalCalendarMiscTests
{
    private static void EnsureBuiltInsLoaded()
        => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    [Theory]
    // Basic (no-hyphen) date forms previously failed to parse as a calendar slot value (problem 9).
    [InlineData("19761118T15:23:30.1+00:00")]
    [InlineData("+0019761118T152330.1+0000")]
    [InlineData("1976-11-18T15:23:30.1")]
    [InlineData("152330")]
    [InlineData("T15:23:30")]
    public void WithCalendar_AcceptsIsoString(string arg)
    {
        var result = Eval($$"""
            Temporal.PlainDate.from({ year: 1976, month: 11, day: 18 }).withCalendar("{{arg}}").calendarId;
        """);
        Assert.Equal("iso8601", result);
    }

    [Theory]
    // { era, eraYear, calendar: japanese } resolves to the ISO year, then the displayed era is
    // recomputed from the date. Meiji 1-5 (1868-1872) display as ce; Meiji 6+ (1873+) as meiji.
    [InlineData("meiji", 1, "M10", 23, "1868|ce|1868")]   // era start date, still ce
    [InlineData("meiji", 1, "M10", 22, "1868|ce|1868")]   // before era start, ce
    [InlineData("meiji", 5, "M12", 31, "1872|ce|1872")]   // last common-era Meiji year
    [InlineData("ce", 1873, "M01", 1, "1873|meiji|6")]    // first Gregorian Meiji year
    [InlineData("taisho", 1, "M07", 29, "1912|meiji|45")] // day before Taisho starts
    [InlineData("reiwa", 1, "M04", 30, "2019|heisei|31")] // before Reiwa start -> Heisei
    public void JapaneseEra_ResolvesAndDisplays(string era, int eraYear, string monthCode, int day, string expected)
    {
        var result = Eval($$"""
            var d = Temporal.PlainDate.from(
                { era: "{{era}}", eraYear: {{eraYear}}, monthCode: "{{monthCode}}", day: {{day}}, calendar: "japanese" },
                { overflow: "reject" });
            d.year + '|' + d.era + '|' + d.eraYear;
        """);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("buddhist")]
    [InlineData("gregory")]
    [InlineData("japanese")]
    [InlineData("hebrew")]
    [InlineData("chinese")]
    public void WeekOfYear_UndefinedForNonIsoCalendars(string calendar)
    {
        var result = Eval($$"""
            var d = new Temporal.PlainDate(2024, 1, 1, "{{calendar}}");
            (d.weekOfYear === undefined) + '|' + (d.yearOfWeek === undefined);
        """);
        Assert.Equal("true|true", result);
    }

    [Fact]
    public void WeekOfYear_DefinedForIsoCalendar()
        => Assert.Equal("1|2024", Eval("var d = new Temporal.PlainDate(2024, 1, 1); d.weekOfYear + '|' + d.yearOfWeek;"));
}
