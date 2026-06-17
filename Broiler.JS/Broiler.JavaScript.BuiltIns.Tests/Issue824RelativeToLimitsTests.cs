using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Issue #824: Temporal.Duration round/total relativeTo ISO strings at the edges of the representable
// range (test262 Duration/prototype/{round,total}/relativeto-string-limits.js). A relativeTo whose
// instant is representable but which overflows once a non-zero duration is added/differenced — either
// because adding to a boundary instant exceeds the instant range (zoned relativeTo) or because the
// relativeTo's own midnight is below the minimum PlainDateTime (plain relativeTo) — is a RangeError,
// yet a blank (zero) duration short-circuits before that conversion and does not throw.
public class Issue824RelativeToLimitsTests
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

    // Strings whose relativeTo is representable, so a blank duration succeeds, but a non-zero duration
    // overflows after conversion to a DateTime / instant.
    public static readonly string[] FailAfterEarlyReturn =
    {
        "+275760-09-13T00:00Z[UTC]",
        "+275760-09-13T01:00+01:00[+01:00]",
        "+275760-09-13T23:59+23:59[+23:59]",
        "-271821-04-19",
        "-271821-04-19T01:00",
    };

    public static IEnumerable<object[]> FailCases() => FailAfterEarlyReturn.Select(s => new object[] { s });

    [Theory]
    [MemberData(nameof(FailCases))]
    public void BlankDurationRoundSucceeds(string relativeTo)
        => Assert.Equal("ok", Outcome(
            $"new Temporal.Duration().round({{ smallestUnit: 'minutes', relativeTo: '{relativeTo}' }})"));

    [Theory]
    [MemberData(nameof(FailCases))]
    public void NonBlankDurationRoundThrows(string relativeTo)
        => Assert.Equal("RangeError", Outcome(
            $"new Temporal.Duration(0, 0, 0, 0, 0, 5).round({{ smallestUnit: 'minutes', relativeTo: '{relativeTo}' }})"));

    [Theory]
    [MemberData(nameof(FailCases))]
    public void BlankDurationTotalSucceeds(string relativeTo)
        => Assert.Equal("ok", Outcome(
            $"new Temporal.Duration().total({{ unit: 'minutes', relativeTo: '{relativeTo}' }})"));

    [Theory]
    [MemberData(nameof(FailCases))]
    public void NonBlankDurationTotalThrows(string relativeTo)
        => Assert.Equal("RangeError", Outcome(
            $"new Temporal.Duration(0, 0, 0, 0, 0, 5).total({{ unit: 'minutes', relativeTo: '{relativeTo}' }})"));

    // Strings that are valid as relativeTo for any duration (instant well inside the range).
    public static readonly string[] AlwaysValid =
    {
        "-271821-04-20T00:00Z[UTC]",
        "+275760-09-13",
        "+275760-09-13T23:00",
    };

    public static IEnumerable<object[]> ValidCases() => AlwaysValid.Select(s => new object[] { s });

    [Theory]
    [MemberData(nameof(ValidCases))]
    public void ValidRelativeToRoundSucceeds(string relativeTo)
        => Assert.Equal("ok", Outcome(
            $"new Temporal.Duration(0, 0, 0, 0, 0, 5).round({{ smallestUnit: 'minutes', relativeTo: '{relativeTo}' }})"));

    // Strings whose relativeTo instant is itself out of range: a RangeError for any duration.
    public static readonly string[] AlwaysInvalid =
    {
        "-271821-04-19T23:59:59.999999999Z[UTC]",
        "+275760-09-13T00:00:00.000000001Z[UTC]",
        "+275760-09-14",
        "+275760-09-14T01:00",
        "-271821-04-18",
        "-271821-04-18T23:00",
    };

    public static IEnumerable<object[]> InvalidCases() => AlwaysInvalid.Select(s => new object[] { s });

    [Theory]
    [MemberData(nameof(InvalidCases))]
    public void InvalidRelativeToRoundThrowsForBlank(string relativeTo)
        => Assert.Equal("RangeError", Outcome(
            $"new Temporal.Duration().round({{ smallestUnit: 'minutes', relativeTo: '{relativeTo}' }})"));

    [Theory]
    [MemberData(nameof(InvalidCases))]
    public void InvalidRelativeToRoundThrowsForNonBlank(string relativeTo)
        => Assert.Equal("RangeError", Outcome(
            $"new Temporal.Duration(0, 0, 0, 0, 0, 5).round({{ smallestUnit: 'minutes', relativeTo: '{relativeTo}' }})"));
}
