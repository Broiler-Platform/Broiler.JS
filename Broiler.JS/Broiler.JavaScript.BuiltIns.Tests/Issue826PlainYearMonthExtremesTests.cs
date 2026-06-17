using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Temporal.PlainYearMonth.from at the extremes of the supported range. Issue #826 P18:
// a PlainYearMonth is bounded by ISOYearMonthWithinLimits (year/month only), but Broiler
// reused the day-precise PlainDate range, so a non-ISO calendar's maximum month — whose
// day-1 reference date lands in +275760-09 past the 13th — was wrongly rejected with
// "Temporal: date is out of range". Mirrors test262
// intl402/Temporal/PlainYearMonth/from/extreme-dates.
public class Issue826PlainYearMonthExtremesTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    // calendar, year, era, eraYear, month, monthCode, expected reference ISO day.
    [Theory]
    [InlineData("coptic", 275471, "am", 275471, 6, "M06", 22)]
    [InlineData("ethioaa", 281247, "aa", 281247, 6, "M06", 22)]
    [InlineData("ethiopic", 275747, "am", 275747, 6, "M06", 22)]
    [InlineData("indian", 275682, "shaka", 275682, 7, "M07", 23)]
    [InlineData("islamic-civil", 283583, "ah", 283583, 6, "M06", 21)]
    [InlineData("islamic-tbla", 283583, "ah", 283583, 6, "M06", 20)]
    [InlineData("islamic-umalqura", 283583, "ah", 283583, 6, "M06", 21)]
    public void MaximumSupportedMonth(string calendar, int year, string era, int eraYear, int month, string monthCode, int isoDay)
        => Assert.Equal($"{year}|{month}|{monthCode}|{isoDay}", Eval($$"""
            var m = Temporal.PlainYearMonth.from({ calendar: "{{calendar}}", year: {{year}}, era: "{{era}}", eraYear: {{eraYear}}, month: {{month}}, monthCode: "{{monthCode}}" });
            var iso = m.toString().match(/(\d{2})\[u-ca/)[1];
            m.year + "|" + m.month + "|" + m.monthCode + "|" + parseInt(iso, 10);
        """));

    // The month one past the maximum overflows ISOYearMonthWithinLimits (reference month > 9) and is rejected.
    [Theory]
    [InlineData("coptic", 275471, 7)]
    [InlineData("indian", 275682, 8)]
    [InlineData("islamic-civil", 283583, 7)]
    public void OnePastMaximumMonth_Throws(string calendar, int year, int month)
        => Assert.Equal("RangeError", Eval($$"""
            try {
              Temporal.PlainYearMonth.from({ calendar: "{{calendar}}", year: {{year}}, month: {{month}} });
              "no throw";
            } catch (e) { e.constructor.name; }
        """));

    // The ISO calendar's own boundary is unaffected: +275760-09 is the maximum, +275760-10 is rejected.
    [Fact]
    public void IsoYearMonthBoundary()
        => Assert.Equal("9|RangeError", Eval("""
            var max = Temporal.PlainYearMonth.from({ year: 275760, month: 9 }).month;
            var over;
            try { Temporal.PlainYearMonth.from({ year: 275760, month: 10 }); over = "no throw"; }
            catch (e) { over = e.constructor.name; }
            max + "|" + over;
        """));

    // A PlainDate at the day-precise boundary is still enforced (the fix only relaxes YearMonth).
    [Fact]
    public void PlainDateDayPreciseLimitStillEnforced()
        => Assert.Equal("RangeError", Eval("""
            try { Temporal.PlainDate.from({ year: 275760, month: 9, day: 14 }); "no throw"; }
            catch (e) { e.constructor.name; }
        """));
}
