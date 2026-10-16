using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Intl.DateTimeFormat wrote the `era` option only beside a year: the pattern appended its 'G' field
// to a date that showed one, so `{ era, month, day }` and `{ era, minute, second }` formatted with no
// era at all, in format and formatToParts alike. The era is now written beside a format with no year
// too, in CLDR's en layout: after a date ("Oct 16 AD") and ahead of a time alone ("AD, 03:05"); a
// lone day, month or weekday beside a short era is appended to the era ("AD (day: 16)", "AD Fri").
// The expected strings are V8's (ICU 77.1, CLDR 47) for the same options. An era-using calendar's
// own era, written without the option, still needs a year.
public class DateTimeFormatEraWithoutYearTests
{
    // 2026-10-16T12:03:05.678Z, a Friday.
    private const string Instant = "Date.UTC(2026, 9, 16, 12, 3, 5, 678)";

    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    private static string Format(string locale, string options, string value = Instant)
        => Eval($"new Intl.DateTimeFormat('{locale}', Object.assign({{ timeZone: 'UTC' }}, {options})).format({value})");

    private static string Parts(string locale, string options, string value = Instant)
        => Eval($"new Intl.DateTimeFormat('{locale}', Object.assign({{ timeZone: 'UTC' }}, {options})).formatToParts({value})"
            + ".map(p => p.type + '=' + p.value).join('|')");

    [Theory]
    // Ahead of a time alone.
    [InlineData("{ era: 'short', minute: '2-digit', second: '2-digit' }", "AD, 03:05")]
    [InlineData("{ era: 'short', minute: '2-digit', second: '2-digit', fractionalSecondDigits: 2 }", "AD, 03:05.67")]
    [InlineData("{ era: 'short', hour: 'numeric', minute: '2-digit' }", "AD, 12:03 PM")]
    [InlineData("{ era: 'long', hour: 'numeric' }", "Anno Domini, 12 PM")]
    [InlineData("{ era: 'long', hour: 'numeric', minute: '2-digit', hourCycle: 'h23' }", "Anno Domini, 12:03")]
    // After a date with no year.
    [InlineData("{ era: 'short', month: 'short', day: 'numeric' }", "Oct 16 AD")]
    [InlineData("{ era: 'short', month: 'numeric', day: 'numeric' }", "10/16 AD")]
    [InlineData("{ era: 'short', weekday: 'short', month: 'short', day: 'numeric' }", "Fri, Oct 16 AD")]
    [InlineData("{ era: 'narrow', month: 'short', day: 'numeric', hour: 'numeric', minute: '2-digit' }", "Oct 16 A, 12:03 PM")]
    // A lone day or month beside a short era is appended to it, "{0} ({2}: {1})"; a lone weekday
    // follows it, "{0} {1}". Each joins a time as a date does.
    [InlineData("{ era: 'short', day: 'numeric' }", "AD (day: 16)")]
    [InlineData("{ era: 'short', day: '2-digit' }", "AD (day: 16)")]
    [InlineData("{ era: 'short', month: 'long' }", "AD (month: October)")]
    [InlineData("{ era: 'short', month: 'short' }", "AD (month: Oct)")]
    [InlineData("{ era: 'short', month: 'narrow' }", "AD (month: O)")]
    [InlineData("{ era: 'short', month: 'numeric' }", "AD (month: 10)")]
    [InlineData("{ era: 'short', month: '2-digit' }", "AD (month: 10)")]
    [InlineData("{ era: 'short', weekday: 'short' }", "AD Fri")]
    [InlineData("{ era: 'short', weekday: 'long' }", "AD Friday")]
    [InlineData("{ era: 'short', weekday: 'narrow' }", "AD F")]
    [InlineData("{ era: 'short', day: 'numeric', hour: 'numeric', minute: '2-digit' }", "AD (day: 16), 12:03 PM")]
    [InlineData("{ era: 'short', month: 'short', hour: 'numeric', minute: '2-digit' }", "AD (month: Oct), 12:03 PM")]
    [InlineData("{ era: 'short', weekday: 'short', hour: 'numeric', minute: '2-digit' }", "AD Fri, 12:03 PM")]
    // Beside a long or narrow era the field comes first, as ICU's skeleton matching mostly has it.
    [InlineData("{ era: 'long', day: 'numeric' }", "16 Anno Domini")]
    [InlineData("{ era: 'long', month: 'short' }", "Oct Anno Domini")]
    [InlineData("{ era: 'long', weekday: 'short' }", "Fri Anno Domini")]
    [InlineData("{ era: 'narrow', day: 'numeric' }", "16 A")]
    [InlineData("{ era: 'narrow', month: 'long' }", "October A")]
    [InlineData("{ era: 'narrow', weekday: 'long' }", "Friday A")]
    // With a year, unchanged.
    [InlineData("{ era: 'short', year: 'numeric' }", "2026 AD")]
    [InlineData("{ era: 'short' }", "10/16/2026 AD")]
    public void The_era_is_written_beside_a_format_with_no_year(string options, string expected)
        => Assert.Equal(expected, Format("en", options));

    [Theory]
    [InlineData("{ era: 'short', month: 'short', day: 'numeric' }", "Jan 1 BC")]
    [InlineData("{ era: 'short', minute: '2-digit', second: '2-digit' }", "BC, 00:00")]
    public void A_date_before_the_common_era_is_BC(string options, string expected)
        => Assert.Equal(expected, Format("en", options, "Date.UTC(-100, 0, 1, 12)"));

    [Fact]
    public void The_era_is_a_part()
    {
        Assert.Equal("era=AD|literal=, |minute=03|literal=:|second=05",
            Parts("en", "{ era: 'short', minute: '2-digit', second: '2-digit' }"));
        Assert.Equal("month=Oct|literal= |day=16|literal= |era=AD",
            Parts("en", "{ era: 'short', month: 'short', day: 'numeric' }"));
        Assert.Equal("era=AD|literal= (day: |day=16|literal=)",
            Parts("en", "{ era: 'short', day: 'numeric' }"));
        Assert.Equal("era=AD|literal= (month: |month=October|literal=)",
            Parts("en", "{ era: 'short', month: 'long' }"));
    }

    [Theory]
    // The option writes an era-using calendar's era too; without it, that era still needs a year.
    [InlineData("en-u-ca-buddhist", "{ era: 'short', month: 'short', day: 'numeric' }", "Oct 16 BE")]
    [InlineData("en-u-ca-buddhist", "{ era: 'short', day: 'numeric' }", "BE (day: 16)")]
    [InlineData("en-u-ca-buddhist", "{ month: 'short', day: 'numeric' }", "Oct 16")]
    [InlineData("en-u-ca-buddhist", "{ hour: 'numeric', minute: '2-digit' }", "12:03 PM")]
    public void An_era_using_calendar_s_era_needs_the_option_or_a_year(string locale, string options, string expected)
        => Assert.Equal(expected, Format(locale, options));

    [Theory]
    // A Temporal type with no era drops the option, as it did.
    [InlineData("new Temporal.PlainTime(12, 3, 5)", "{ era: 'short', hour: 'numeric', minute: '2-digit' }", "12:03 PM")]
    [InlineData("Temporal.PlainMonthDay.from('10-16')", "{ era: 'short', month: 'short', day: 'numeric', calendar: 'iso8601' }", "Oct 16")]
    // A PlainDate has one.
    [InlineData("new Temporal.PlainDate(2026, 10, 16)", "{ era: 'short', month: 'short', day: 'numeric' }", "Oct 16 AD")]
    public void A_Temporal_value_writes_the_era_only_when_it_has_one(string value, string options, string expected)
        => Assert.Equal(expected, Eval($"{value}.toLocaleString('en', {options})"));
}
