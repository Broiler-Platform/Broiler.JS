using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Issue #824 (problems 62, 63, 64): Temporal.Duration round/total/compare accept a relativeTo whose
// UTC offset is an inexact (rounded to HH:MM) or exact (HH:MM:SS) sub-minute offset, matched against
// the named zone's historical offset. Mirrors test262 Duration/{compare,prototype/total,prototype/
// round}/relativeto-sub-minute-offset.js (Pacific/Niue −11:19:40, Africa/Monrovia −00:44:30).
public class Issue824SubMinuteOffsetRelativeToTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    private static string Outcome(string expr)
        => Eval($$"""
            var out;
            try { out = String({{expr}}); }
            catch (e) { out = e.constructor.name; }
            out;
        """);

    // 63 — total({unit:"days"}) with Africa/Monrovia string offsets.
    [Theory]
    [InlineData("\"1970-01-01T00:00-00:45:00[-00:45]\"", "366")]
    [InlineData("\"1970-01-01T00:00:00-00:45[Africa/Monrovia]\"", "366")]
    [InlineData("\"1970-01-01T00:00:00-00:44:30[Africa/Monrovia]\"", "366")]
    [InlineData("\"1970-01-01T00:00:00-00:44:40[Africa/Monrovia]\"", "RangeError")]
    [InlineData("\"1970-01-01T00:00:00-00:45:00[Africa/Monrovia]\"", "RangeError")]
    [InlineData("\"1970-01-01T00:00+00:44:30.123456789[+00:45]\"", "RangeError")]
    public void TotalDaysMonrovia(string relativeTo, string expected)
        => Assert.Equal(expected, Outcome(
            $"new Temporal.Duration(1, 0, 0, 0, 24).total({{ unit: 'days', relativeTo: {relativeTo} }})"));

    [Theory]
    // The property-bag offset must match the zone exactly (no minute rounding): -00:45 ≠ -00:44:30.
    [InlineData("{ year: 1970, month: 1, day: 1, offset: '+00:45:00.000000000', timeZone: '+00:45' }", "366")]
    [InlineData("{ year: 1970, month: 1, day: 1, offset: '-00:45', timeZone: 'Africa/Monrovia' }", "RangeError")]
    public void TotalDaysMonroviaBag(string relativeTo, string expected)
        => Assert.Equal(expected, Outcome(
            $"new Temporal.Duration(1, 0, 0, 0, 24).total({{ unit: 'days', relativeTo: {relativeTo} }})"));

    // 63/64 — Pacific/Niue edge case: a calendar day spans the −11:19:40 → −11:20 change.
    [Theory]
    [InlineData("\"1952-10-15T23:59:59-11:19:40[Pacific/Niue]\"", "86420")]
    [InlineData("\"1952-10-15T23:59:59-11:20[Pacific/Niue]\"", "86420")]
    [InlineData("\"1952-10-15T23:59:59-11:20:00[Pacific/Niue]\"", "86400")]
    [InlineData("\"1952-10-15T23:59:59-11:19:50[Pacific/Niue]\"", "RangeError")]
    public void TotalSecondsNiue(string relativeTo, string expected)
        => Assert.Equal(expected, Outcome(
            $"new Temporal.Duration(0, 0, 0, 1).total({{ unit: 'seconds', relativeTo: {relativeTo} }})"));

    // 64 — round({largestUnit:"seconds"}) Niue edge case (seconds component).
    [Theory]
    [InlineData("\"1952-10-15T23:59:59-11:19:40[Pacific/Niue]\"", "86420")]
    [InlineData("\"1952-10-15T23:59:59-11:20:00[Pacific/Niue]\"", "86400")]
    public void RoundSecondsNiue(string relativeTo, string expected)
        => Assert.Equal(expected, Outcome(
            $"new Temporal.Duration(0, 0, 0, 1).round({{ largestUnit: 'seconds', relativeTo: {relativeTo} }}).seconds"));

    // 62 — compare Niue edge case: PT24H vs P1D where the day is 86420 s.
    [Theory]
    [InlineData("\"1952-10-15T23:59:59-11:19:40[Pacific/Niue]\"", "-1")]
    [InlineData("\"1952-10-15T23:59:59-11:20[Pacific/Niue]\"", "-1")]
    [InlineData("\"1952-10-15T23:59:59-11:20:00[Pacific/Niue]\"", "0")]
    [InlineData("\"1952-10-15T23:59:59-11:19:50[Pacific/Niue]\"", "RangeError")]
    public void CompareNiue(string relativeTo, string expected)
        => Assert.Equal(expected, Outcome(
            $"Temporal.Duration.compare(new Temporal.Duration(0,0,0,0,24), new Temporal.Duration(0,0,0,1), {{ relativeTo: {relativeTo} }})"));

    // 62 — compare Monrovia string offsets (P31D vs P1M).
    [Theory]
    [InlineData("\"1970-01-01T00:00:00-00:45[Africa/Monrovia]\"", "0")]
    [InlineData("\"1970-01-01T00:00:00-00:44:30[Africa/Monrovia]\"", "0")]
    [InlineData("\"1970-01-01T00:00:00-00:44:40[Africa/Monrovia]\"", "RangeError")]
    [InlineData("\"1970-01-01T00:00:00-00:45:00[Africa/Monrovia]\"", "RangeError")]
    public void CompareMonrovia(string relativeTo, string expected)
        => Assert.Equal(expected, Outcome(
            $"Temporal.Duration.compare(new Temporal.Duration(0,0,0,31), new Temporal.Duration(0,1), {{ relativeTo: {relativeTo} }})"));
}
