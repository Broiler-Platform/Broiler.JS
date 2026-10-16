using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Intl.DateTimeFormat.prototype.formatToParts renders a format with minute and second but no hour
// through its own branch. That branch read the minute and second through DateTimeOffset.ToLocalTime(),
// the host's zone, where every other path reads the zoned fields of the format's resolved timeZone. A
// zone whose offset has a minute part then showed the host's minute: on this suite's UTC host,
// Asia/Kolkata (+05:30) gave "00" where `format` gave "30". The branch also threw .NET's
// ArgumentOutOfRangeException for a time value past DateTimeOffset's years 0001 to 9999.
public class DateTimeFormatMinuteSecondTimeZoneTests
{
    // 2026-10-16T12:00:05.678Z.
    private const string Instant = "Date.UTC(2026, 9, 16, 12, 0, 5, 678)";

    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    private static string Parts(string options, string value = Instant)
        => Eval($"new Intl.DateTimeFormat('en', {options}).formatToParts({value})"
            + ".map(p => p.type + '=' + p.value).join('|')");

    [Theory]
    // Zones whose offset has a minute part: the wall-clock minute is the zone's, not the host's.
    [InlineData("Asia/Kolkata", "minute=30|literal=:|second=05")]
    [InlineData("Asia/Kathmandu", "minute=45|literal=:|second=05")]
    [InlineData("Australia/Eucla", "minute=45|literal=:|second=05")]
    [InlineData("+01:15", "minute=15|literal=:|second=05")]
    [InlineData("-09:30", "minute=30|literal=:|second=05")]
    // Whole-hour zones, which agreed before, still do.
    [InlineData("UTC", "minute=00|literal=:|second=05")]
    [InlineData("America/New_York", "minute=00|literal=:|second=05")]
    public void Minute_and_second_are_the_time_zones(string timeZone, string expected)
        => Assert.Equal(expected, Parts($"{{ minute: '2-digit', second: '2-digit', timeZone: '{timeZone}' }}"));

    [Fact]
    public void Fractional_seconds_are_the_time_zones_too()
        => Assert.Equal("minute=30|literal=:|second=05|literal=.|fractionalSecond=678",
            Parts("{ minute: '2-digit', second: '2-digit', fractionalSecondDigits: 3, timeZone: 'Asia/Kolkata' }"));

    [Fact]
    public void Without_a_time_zone_the_parts_are_the_hosts()
        => Assert.Equal("minute=00|literal=:|second=05", Parts("{ minute: '2-digit', second: '2-digit' }"));

    [Theory]
    [InlineData("Asia/Kolkata")]
    [InlineData("Asia/Kathmandu")]
    [InlineData("+01:15")]
    public void The_parts_join_to_format(string timeZone)
        => Assert.Equal("true", Eval($$"""
            var dtf = new Intl.DateTimeFormat('en', { minute: '2-digit', second: '2-digit', timeZone: '{{timeZone}}' });
            dtf.formatToParts({{Instant}}).map(p => p.value).join('') === dtf.format({{Instant}});
        """));

    [Theory]
    // TimeClip's bounds, past DateTimeOffset's years 0001 to 9999.
    [InlineData("8.64e15")]
    [InlineData("-8.64e15")]
    public void A_time_value_outside_the_years_0001_to_9999_is_formatted(string value)
        => Assert.Equal("minute=00|literal=:|second=00",
            Parts("{ minute: '2-digit', second: '2-digit', timeZone: 'UTC' }", value));
}
