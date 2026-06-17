using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Temporal.ZonedDateTime.prototype.getTimeZoneTransition smoke test of specific IANA
// values. Issue #826 P16: the transition scan used a fixed ~50-year horizon, so the
// "next" transition from an instant far before a zone's first recorded transition
// (e.g. America/New_York's first transition, 1883-11-18, is 83 years after a 1800
// relativeTo) returned null and `.epochNanoseconds` threw. The scan is now adaptive
// (fine near the start, then coarse) with a much larger horizon.
// Mirrors test262 intl402/.../getTimeZoneTransition/specific-tzdb-values.
public class Issue826TimeZoneTransitionTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    [Theory]
    // a1: America/New_York next transition from 2019-04-16 -> 2019-11-03T06:00:00Z (DST end).
    [InlineData("1555448460000000000", "America/New_York", "next", "1572760800000000000")]
    // a2: America/New_York next transition from 1800-01-01 -> 1883-11-18T17:00:00Z (first ever).
    [InlineData("-5364662400000000000", "America/New_York", "next", "-2717650800000000000")]
    // a3: Europe/London previous transition from 2020-06-11 -> 2020-03-29T01:00:00Z.
    [InlineData("1591909260000000000", "Europe/London", "previous", "1585443600000000000")]
    // a4: Europe/London previous transition from 1848-01-01 -> 1847-12-01T00:01:15Z.
    [InlineData("-3849984000000000000", "Europe/London", "previous", "-3852662325000000000")]
    public void GetTimeZoneTransition_SpecificValues(string epoch, string zone, string direction, string expected)
        => Assert.Equal(expected, Eval($$"""
            var z = new Temporal.ZonedDateTime({{epoch}}n, "{{zone}}");
            var t = z.getTimeZoneTransition("{{direction}}");
            t === null ? "null" : t.epochNanoseconds.toString();
        """));

    // An instant before a zone's first transition has no "previous" transition.
    [Fact]
    public void GetTimeZoneTransition_BeforeFirst_PreviousIsNull()
        => Assert.Equal("null", Eval("""
            var z = new Temporal.ZonedDateTime(-5364662400000000000n, "America/New_York");
            var t = z.getTimeZoneTransition("previous");
            t === null ? "null" : t.epochNanoseconds.toString();
        """));
}
