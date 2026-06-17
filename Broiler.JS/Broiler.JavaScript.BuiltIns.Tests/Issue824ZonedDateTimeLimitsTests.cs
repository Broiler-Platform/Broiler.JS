using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Issue #824 problem 20: Temporal.ZonedDateTime.from ISO strings at the edges of the representable
// range (test262 ZonedDateTime/from/argument-string-limits.js). The wall-clock date is only
// range-checked on the "prefer"/"reject" offset paths (CheckISODaysRange); "use"/"ignore"/"exact"
// only require the resolved instant to be representable, so boundary strings whose wall clock is one
// day before the minimum instant's date must still parse there.
public class Issue824ZonedDateTimeLimitsTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Outcome(string expr)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval($$"""
            var out = "ok";
            try { {{expr}}; }
            catch (e) { out = e.constructor.name; }
            out;
        """).ToString();
    }

    public static IEnumerable<object[]> ValidUseIgnore()
    {
        foreach (var offset in new[] { "use", "ignore" })
            foreach (var arg in new[]
            {
                "-271821-04-20T00:00Z[UTC]",
                "-271821-04-19T23:00-01:00[-01:00]",
                "-271821-04-19T00:01-23:59[-23:59]",
                "+275760-09-13T00:00Z[UTC]",
                "+275760-09-13T01:00+01:00[+01:00]",
                "+275760-09-13T23:59+23:59[+23:59]",
            })
                yield return new object[] { arg, offset };
    }

    [Theory]
    [MemberData(nameof(ValidUseIgnore))]
    public void ValidForUseIgnore(string arg, string offset)
        => Assert.Equal("ok", Outcome($"Temporal.ZonedDateTime.from('{arg}', {{ offset: '{offset}' }})"));

    public static IEnumerable<object[]> ValidPreferReject()
    {
        foreach (var offset in new[] { "prefer", "reject" })
            foreach (var arg in new[]
            {
                "-271821-04-20T00:00Z[UTC]",
                "+275760-09-13T00:00Z[UTC]",
                "+275760-09-13T01:00+01:00[+01:00]",
                "+275760-09-13T23:59+23:59[+23:59]",
            })
                yield return new object[] { arg, offset };
    }

    [Theory]
    [MemberData(nameof(ValidPreferReject))]
    public void ValidForPreferReject(string arg, string offset)
        => Assert.Equal("ok", Outcome($"Temporal.ZonedDateTime.from('{arg}', {{ offset: '{offset}' }})"));

    public static IEnumerable<object[]> InvalidPreferReject()
    {
        foreach (var offset in new[] { "prefer", "reject" })
            foreach (var arg in new[]
            {
                "-271821-04-19T23:00-01:00[-01:00]",
                "-271821-04-19T00:00:01-23:59[-23:59]",
            })
                yield return new object[] { arg, offset };
    }

    [Theory]
    [MemberData(nameof(InvalidPreferReject))]
    public void InvalidForPreferReject(string arg, string offset)
        => Assert.Equal("RangeError", Outcome($"Temporal.ZonedDateTime.from('{arg}', {{ offset: '{offset}' }})"));

    public static IEnumerable<object[]> AlwaysInvalid()
    {
        foreach (var offset in new[] { "use", "ignore", "prefer", "reject" })
            foreach (var arg in new[]
            {
                "-271821-04-19T23:59:59.999999999Z[UTC]",
                "-271821-04-19T23:00-00:59[-00:59]",
                "-271821-04-19T00:00:00-23:59[-23:59]",
                "+275760-09-13T00:00:00.000000001Z[UTC]",
                "+275760-09-13T01:00+00:59[+00:59]",
                "+275760-09-14T00:00+23:59[+23:59]",
            })
                yield return new object[] { arg, offset };
    }

    [Theory]
    [MemberData(nameof(AlwaysInvalid))]
    public void InvalidForAllOffsets(string arg, string offset)
        => Assert.Equal("RangeError", Outcome($"Temporal.ZonedDateTime.from('{arg}', {{ offset: '{offset}' }})"));
}
