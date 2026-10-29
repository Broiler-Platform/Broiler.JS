using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Temporal.Duration round/total with a relativeTo. Issue #826:
//   P48 round/rounding-increment-relativeto: rounding 1m30d to whole months (increment 2)
//        relative to 1970-07-31 returned 1m30d unchanged instead of 2m, because the rounded
//        unit count was re-derived through AddCalendarDate + DiffCalendarDate, which is
//        asymmetric around month-end clamping. When smallest == largest unit the rounded
//        count is used directly.
//   P41 total/relativeto-date-limits: a ZonedDateTime relativeTo within one day of the
//        maximum instant overflows when the day-after boundary is computed, which is a
//        RangeError; the nudge-window boundary was not range-validated.
//   P5  round/balances-up-to-weeks: rounding to weeks while the largest unit is a coarser
//        calendar unit (year/month) must throw, since a month is not a whole number of weeks.
public class Issue826DurationRelativeRoundingTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    // P48 — the temporal_rs buggy case: month-end clamping in the relativeTo date.
    [Theory]
    [InlineData("new Temporal.PlainDate(1970, 7, 31)")]
    [InlineData("new Temporal.PlainDate(1970, 3, 1)")]
    public void RoundMonths_IncrementTwo_RelativeToMonthEnd(string relativeTo)
        => Assert.Equal("0,2,0,0", Eval($$"""
            var r = new Temporal.Duration(0, 1, 0, 30).round({
                smallestUnit: "months", roundingIncrement: 2, relativeTo: {{relativeTo}} });
            [r.years, r.months, r.weeks, r.days].join(",");
        """));

    [Theory]
    [InlineData("new Temporal.PlainDate(2020, 1, 1)")]
    [InlineData("new Temporal.ZonedDateTime(0n, \"UTC\")")]
    public void RoundWeeks_IncrementTwo(string relativeTo)
        => Assert.Equal("0,0,2,0", Eval($$"""
            var r = new Temporal.Duration(0, 0, 1, 0, 168).round({
                smallestUnit: "weeks", roundingIncrement: 2, relativeTo: {{relativeTo}} });
            [r.years, r.months, r.weeks, r.days].join(",");
        """));

    // Month smallest, year largest still decomposes correctly (14 months -> 1y2m).
    [Fact]
    public void RoundMonths_LargestYear_Decomposes()
        => Assert.Equal("1,2,0,0", Eval("""
            var r = new Temporal.Duration(0, 14, 0, 0).round({
                smallestUnit: "months", largestUnit: "years", relativeTo: new Temporal.PlainDate(2020, 1, 1) });
            [r.years, r.months, r.weeks, r.days].join(",");
        """));

    // P41 — a ZonedDateTime relativeTo one second past the maximum usable instant overflows
    // the day-after boundary needed to total in days.
    [Fact]
    public void TotalDays_ZonedRelativeToBeyondLimit_Throws()
        => Assert.Equal("RangeError", Eval("""
            try {
              new Temporal.Duration(0).total({ unit: "days", relativeTo: "+275760-09-12T00:00:01+00:00[UTC]" });
              "no throw";
            } catch (e) { e.constructor.name; }
        """));

    [Fact]
    public void TotalDays_ZonedRelativeToAtLimit_IsZero()
        => Assert.Equal("0", Eval("""
            "" + new Temporal.Duration(0).total({ unit: "days", relativeTo: "+275760-09-12T00:00:00+00:00[UTC]" });
        """));

    // P5 — rounding to weeks while the largest unit is coarser (defaults to month here) throws.
    [Fact]
    public void RoundWeeks_WithMonths_NoLargestUnit_Throws()
        => Assert.Equal("RangeError", Eval("""
            try {
              new Temporal.Duration(0, 1, 0, 1).round({
                relativeTo: new Temporal.PlainDate(2024, 1, 1),
                smallestUnit: "weeks", roundingIncrement: 99, roundingMode: "ceil" });
              "no throw";
            } catch (e) { e.constructor.name; }
        """));

    [Fact]
    public void RoundWeeks_WithMonths_ExplicitWeeksLargestUnit_Succeeds()
        => Assert.Equal("0,0,99,0", Eval("""
            var r = new Temporal.Duration(0, 1, 0, 1).round({
                relativeTo: new Temporal.PlainDate(2024, 1, 1),
                largestUnit: "weeks", smallestUnit: "weeks", roundingIncrement: 99, roundingMode: "ceil" });
            [r.years, r.months, r.weeks, r.days].join(",");
        """));
}
