using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Non-Gregorian calendar fixes from issue #800:
//   • problems 11/12: PlainDate.with({ year }) on a lunisolar leap month (chinese/dangi) constrains
//     a missing leap month MnnL back to the regular month Mnn (or rejects under overflow "reject").
//   • problem 30: PlainMonthDay.from for an arithmetic calendar (islamic family) whose year
//     numbering differs from the ISO year resolves the reference ISO year near 1972.
public class TemporalNonIsoCalendarTests
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
    // leapMonth = chinese 2017 M06L; shifting the year keeps M06L where it exists, else constrains.
    [InlineData("2025", "{ overflow: 'reject' }", "2025|7|M06L")] // another leap year with M06L
    [InlineData("2020", "undefined", "2020|7|M06")]               // leap year without M06L -> M06
    [InlineData("2024", "undefined", "2024|6|M06")]               // common year -> M06
    public void With_Year_ConstrainsLunisolarLeapMonth(string year, string opts, string expected)
    {
        var result = Eval($$"""
            const leap = Temporal.PlainDate.from({ year: 2017, monthCode: "M06L", day: 1, calendar: "chinese" }, { overflow: "reject" });
            const d = leap.with({ year: {{year}} }, {{opts}});
            d.year + '|' + d.month + '|' + d.monthCode;
        """);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("2020")]
    [InlineData("2024")]
    public void With_Year_RejectsMissingLeapMonth(string year)
    {
        var result = Eval($$"""
            const leap = Temporal.PlainDate.from({ year: 2017, monthCode: "M06L", day: 1, calendar: "chinese" }, { overflow: "reject" });
            let threw = false;
            try { leap.with({ year: {{year}} }, { overflow: "reject" }); } catch (e) { threw = e instanceof RangeError; }
            threw;
        """);
        Assert.Equal("true", result);
    }

    [Theory]
    // Islamic-civil month codes resolve to a reference ISO year near 1972 (not far in the future).
    [InlineData("M01", "1|1972")]
    [InlineData("M12", "1|1972")]
    [InlineData("M02", "1|1972")]
    public void MonthDayFrom_IslamicCivil_ReferenceYearNear1972(string monthCode, string expected)
    {
        var result = Eval($$"""
            const pmd = Temporal.PlainMonthDay.from({ calendar: "islamic-civil", monthCode: "{{monthCode}}", day: 1 });
            const iso = pmd.toString().slice(0, 4) === '1972';
            pmd.day + '|' + (iso ? '1972' : pmd.toString());
        """);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void MonthDayFrom_IslamicCivil_ConstrainsDay()
    {
        // Islamic M02 has 29 days; day 30 constrains to 29 and rejects under "reject".
        Assert.Equal("M02|29", Eval("""
            const pmd = Temporal.PlainMonthDay.from({ calendar: "islamic-civil", monthCode: "M02", day: 30 }, { overflow: "constrain" });
            pmd.monthCode + '|' + pmd.day;
        """));
        Assert.Equal("true", Eval("""
            let threw = false;
            try { Temporal.PlainMonthDay.from({ calendar: "islamic-civil", monthCode: "M02", day: 30 }, { overflow: "reject" }); }
            catch (e) { threw = e instanceof RangeError; }
            threw;
        """));
    }
}
