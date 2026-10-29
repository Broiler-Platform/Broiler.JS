using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// GetStartOfDay across a spring-forward gap that covers midnight: the day starts at the transition
// instant (the first existing local time), which may be neither 00:00 nor 01:00. America/Toronto
// sprang forward 1919-03-30T23:30 → 1919-03-31T00:30, so 1919-03-31 starts at 00:30. Issue #805
// problem 11. A date-only ZonedDateTime string and PlainDate.toZonedDateTime (no plainTime) use
// start-of-day; an explicit midnight uses disambiguation (→ 01:00), 30 minutes later.
public class TemporalStartOfDayGapTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);
    private static string E(string e) { Load(); using var c = new JSContext(); return c.Eval(e).ToString(); }

    [Theory]
    [InlineData("Temporal.ZonedDateTime.from('1919-03-31[America/Toronto]').toString()")]
    [InlineData("new Temporal.PlainDate(1919,3,31).toZonedDateTime('America/Toronto').toString()")]
    [InlineData("new Temporal.PlainDate(1919,3,31).toZonedDateTime({ timeZone: 'America/Toronto' }).toString()")]
    public void StartOfDay_GapCoversMidnight_IsTransitionInstant(string expr)
        => Assert.StartsWith("1919-03-31T00:30:00", E(expr));

    [Fact]
    public void ExplicitMidnight_GapCoversMidnight_DisambiguatesLater()
        // An explicit T00 is midnight-with-disambiguation (compatible → after the gap), i.e. 01:00.
        => Assert.StartsWith("1919-03-31T01:00:00",
            E("Temporal.ZonedDateTime.from('1919-03-31T00[America/Toronto]').toString()"));

    [Fact]
    public void StartOfDay_IsThirtyMinutesBeforeDisambiguatedMidnight()
        => Assert.Equal("PT30M", E("""
            var sod = Temporal.ZonedDateTime.from('1919-03-31[America/Toronto]');
            var mid = Temporal.ZonedDateTime.from('1919-03-31T00[America/Toronto]');
            sod.until(mid).toString();
        """));

    [Fact]
    public void StartOfDay_NormalDay_IsMidnight()
        => Assert.StartsWith("2020-06-15T00:00:00",
            E("new Temporal.PlainDate(2020,6,15).toZonedDateTime('America/Toronto').toString()"));

    [Fact]
    public void PlainDate_ToZonedDateTime_WithPlainTime_UsesThatTime()
        => Assert.StartsWith("2020-06-15T08:30:00",
            E("new Temporal.PlainDate(2020,6,15).toZonedDateTime({ timeZone: 'America/Toronto', plainTime: new Temporal.PlainTime(8,30) }).toString()"));
}
