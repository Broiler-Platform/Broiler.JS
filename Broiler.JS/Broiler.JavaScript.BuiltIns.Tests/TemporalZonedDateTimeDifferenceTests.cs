using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.BuiltIns.Tests;

// ZonedDateTime.since/until calendar-unit differencing (issue #800, problems 3, 4, 6, 7). The
// ZonedDateTime difference had its own ISO date-difference routine that stepped a *constrained*
// intermediate forward month-by-month, so a short month in between stranded the day (Dec 30 + N
// months landing on Feb 28) and a wrap (Jan 29 + 1 month = Feb 29) was mis-counted as a whole month.
public class TemporalZonedDateTimeDifferenceTests
{
    private static void EnsureBuiltInsLoaded()
        => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Since(string aSpec, string bSpec, string largestUnit, string calendar = "iso8601")
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval($$"""
            const cal = "{{calendar}}";
            const mk = (s) => Temporal.ZonedDateTime.from(Object.assign({ hour: 12, minute: 34, timeZone: "UTC", calendar: cal }, s));
            const d = mk({{aSpec}}).since(mk({{bSpec}}), { largestUnit: "{{largestUnit}}" });
            [d.years, d.months, d.weeks, d.days].join(',');
        """);
        return result.ToString();
    }

    [Theory]
    [InlineData("iso8601")]
    [InlineData("gregory")]
    [InlineData("buddhist")]
    [InlineData("japanese")]
    [InlineData("roc")]
    public void Since_OneYearSixMonthsSixteenDays(string calendar)
    {
        // 2019-12-30 to 2021-07-16 = 1 year, 6 months, 16 days (not 18: the Feb wrap must not strand
        // the day). Result is negative because the receiver is earlier.
        Assert.Equal("-1,-6,0,-16",
            Since("{ year: 2019, monthCode: 'M12', day: 30 }", "{ year: 2021, monthCode: 'M07', day: 16 }", "years", calendar));
    }

    [Theory]
    // End of longer month to end of following shorter month (gregory). Jan 28->Feb 28 is one month;
    // Jan 29/30/31 -> Feb 28 are 30/29/28 days (the wrap does not count as a whole month).
    [InlineData("M01", 28, "0,-1,0,0")]
    [InlineData("M01", 29, "0,0,0,-30")]
    [InlineData("M01", 30, "0,0,0,-29")]
    [InlineData("M01", 31, "0,0,0,-28")]
    public void Since_WrappingAtEndOfMonth(string monthCode, int day, string expected)
    {
        Assert.Equal(expected,
            Since($"{{ year: 1970, monthCode: '{monthCode}', day: {day} }}",
                  "{ year: 1970, monthCode: 'M02', day: 28 }", "years", "gregory"));
    }

    [Fact]
    public void Since_BackwardDirectionIsAnchoredAtTheStart()
    {
        // since/until are intentionally asymmetric across differing month lengths: the later.since
        // (earlier) difference is anchored at the later date, so 2021-07-16 back to 2019-12-30 is
        // 1 year, 6 months and 17 days (matching test262's reverse basic-gregory cases), not 16.
        Assert.Equal("1,6,0,17",
            Since("{ year: 2021, monthCode: 'M07', day: 16 }", "{ year: 2019, monthCode: 'M12', day: 30 }", "years", "gregory"));
    }
}
