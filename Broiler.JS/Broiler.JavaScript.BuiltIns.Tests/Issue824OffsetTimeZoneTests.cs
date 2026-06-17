using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Issue #824 (problems 57, 61, 59): Intl.DateTimeFormat offset time zones. resolvedOptions().timeZone
// normalizes a valid offset to ±HH:MM (zero offset → "+00:00"), and the constructor rejects malformed
// offsets with a RangeError. Vectors mirror test262 DateTimeFormat offset-timezone-{basic,change}.js
// and constructor-invalid-offset-timezone.js.
public class Issue824OffsetTimeZoneTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string expr)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(expr).ToString();
    }

    private static string ResolvedTimeZone(string tz)
        => Eval($"new Intl.DateTimeFormat(undefined, {{ timeZone: '{tz}' }}).resolvedOptions().timeZone");

    private static string Outcome(string tz)
        => Eval($$"""
            var out = "ok";
            try { new Intl.DateTimeFormat(undefined, { timeZone: '{{tz}}' }); }
            catch (e) { out = e.constructor.name; }
            out;
        """);

    [Theory]
    // offset-timezone-basic: 3-char forms gain ":00"; HH:MM forms are unchanged.
    [InlineData("+03", "+03:00")]
    [InlineData("+13", "+13:00")]
    [InlineData("+23", "+23:00")]
    [InlineData("-07", "-07:00")]
    [InlineData("-14", "-14:00")]
    [InlineData("-21", "-21:00")]
    [InlineData("+01:03", "+01:03")]
    [InlineData("+15:59", "+15:59")]
    [InlineData("+22:27", "+22:27")]
    [InlineData("-02:32", "-02:32")]
    [InlineData("-17:01", "-17:01")]
    [InlineData("-22:23", "-22:23")]
    // offset-timezone-change: HHMM and zero-offset normalization.
    [InlineData("-00", "+00:00")]
    [InlineData("-00:00", "+00:00")]
    [InlineData("+00", "+00:00")]
    [InlineData("+0000", "+00:00")]
    [InlineData("+0300", "+03:00")]
    [InlineData("+2300", "+23:00")]
    [InlineData("-2100", "-21:00")]
    [InlineData("+0103", "+01:03")]
    [InlineData("+2227", "+22:27")]
    [InlineData("-1701", "-17:01")]
    public void NormalizesValidOffset(string tz, string expected)
        => Assert.Equal(expected, ResolvedTimeZone(tz));

    [Theory]
    [InlineData("+3")]
    [InlineData("+24")]
    [InlineData("+23:0")]
    [InlineData("+130")]
    [InlineData("+13234")]
    [InlineData("+135678")]
    [InlineData("-7")]
    [InlineData("-10.50")]
    [InlineData("-10,50")]
    [InlineData("-24")]
    [InlineData("-014")]
    [InlineData("-210")]
    [InlineData("-2400")]
    [InlineData("-1:10")]
    [InlineData("-21:0")]
    [InlineData("+0:003")]
    [InlineData("+15:59:00")]
    [InlineData("+15:59.50")]
    [InlineData("+15:59,50")]
    [InlineData("+222700")]
    [InlineData("-02:3200")]
    [InlineData("-170100")]
    [InlineData("-22230")]
    public void RejectsInvalidOffset(string tz)
        => Assert.Equal("RangeError", Outcome(tz));

    [Theory]
    // Named zones are unaffected.
    [InlineData("UTC", "UTC")]
    public void NamedZonesUnchanged(string tz, string expected)
        => Assert.Equal(expected, ResolvedTimeZone(tz));
}
