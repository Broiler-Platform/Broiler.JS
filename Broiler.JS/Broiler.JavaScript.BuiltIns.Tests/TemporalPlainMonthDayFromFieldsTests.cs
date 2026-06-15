using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Verifies Temporal.PlainMonthDay.from({ ...fields }) for the ISO calendar (issue #800, problem 1
// and the PlainMonthDay part of problem 20). A bare numeric `month`/`day` without a `year` or
// `monthCode` resolves against the leap-year reference (1972) rather than throwing a TypeError; a
// supplied `year` is used to validate/constrain the day; and non-positive month/day always throw a
// RangeError regardless of the overflow handling.
public class TemporalPlainMonthDayFromFieldsTests
{
    private static void EnsureBuiltInsLoaded()
        => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string MonthDay(string expr)
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval($$"""
            var md = {{expr}};
            md.monthCode + '|' + md.day;
        """);
        return result.ToString();
    }

    [Theory]
    // month/day without year resolves against the 1972 leap-year reference.
    [InlineData("Temporal.PlainMonthDay.from({ month: 10, day: 1 })", "M10|1")]
    [InlineData("Temporal.PlainMonthDay.from({ month: 2, day: 29 })", "M02|29")]
    // a supplied (leap) year keeps 02-29; a supplied common year constrains it.
    [InlineData("Temporal.PlainMonthDay.from({ month: 2, day: 29, year: 1996 })", "M02|29")]
    [InlineData("Temporal.PlainMonthDay.from({ month: 2, day: 29, year: 2001 }, { overflow: 'constrain' })", "M02|28")]
    // default overflow is constrain; the reference year (1972) is a leap year.
    [InlineData("Temporal.PlainMonthDay.from({ month: 2, day: 31 })", "M02|29")]
    [InlineData("Temporal.PlainMonthDay.from({ month: 1, day: 32 })", "M01|31")]
    [InlineData("Temporal.PlainMonthDay.from({ year: 2021, month: 999999, day: 500 }, { overflow: 'constrain' })", "M12|31")]
    public void From_Fields_ResolvesMonthDay(string expr, string expected)
        => Assert.Equal(expected, MonthDay(expr));

    [Theory]
    // A supplied common year rejects 02-29.
    [InlineData("Temporal.PlainMonthDay.from({ month: 2, day: 29, year: 2001 }, { overflow: 'reject' })")]
    [InlineData("Temporal.PlainMonthDay.from({ month: 1, day: 32 }, { overflow: 'reject' })")]
    // Non-positive month/day are out of range regardless of overflow handling.
    [InlineData("Temporal.PlainMonthDay.from({ day: 1, month: -1 })")]
    [InlineData("Temporal.PlainMonthDay.from({ month: 1, day: -1 })")]
    [InlineData("Temporal.PlainMonthDay.from({ year: 2021, month: 0, day: 1 }, { overflow: 'constrain' })")]
    public void From_Fields_ThrowsRangeError(string expr)
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval($$"""
            var threw = false;
            try { {{expr}}; } catch (e) { threw = e instanceof RangeError; }
            threw;
        """);
        Assert.True(result.BooleanValue);
    }
}
