using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Intl.DateTimeFormat wrote the `era` option only beside a year: the pattern appended its 'G' field
// to a date that showed one, so `{ era, month, day }` and `{ era, minute, second }` formatted with no
// era at all, in format and formatToParts alike. The era is now written beside a format with no year
// too, in CLDR's en layout: after a date ("Oct 16 AD") and ahead of a time alone ("AD, 03:05"); a
// lone day, month or weekday as ICU's pattern generator lays it out ("AD (day: 16)", "AD Fri",
// "16 Anno Domini"), by the nearer of the era's and the field's skeletons.
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
    // A lone field joins a time as a date does.
    [InlineData("{ era: 'short', day: 'numeric', hour: 'numeric', minute: '2-digit' }", "AD (day: 16), 12:03 PM")]
    [InlineData("{ era: 'short', month: 'short', hour: 'numeric', minute: '2-digit' }", "AD (month: Oct), 12:03 PM")]
    [InlineData("{ era: 'short', weekday: 'short', hour: 'numeric', minute: '2-digit' }", "AD Fri, 12:03 PM")]
    [InlineData("{ era: 'long', day: '2-digit', hour: 'numeric', minute: '2-digit' }", "Anno Domini (day: 16), 12:03 PM")]
    [InlineData("{ era: 'long', day: 'numeric', hour: 'numeric', minute: '2-digit' }", "16 Anno Domini, 12:03 PM")]
    [InlineData("{ era: 'narrow', weekday: 'narrow', hour: 'numeric', minute: '2-digit' }", "A F, 12:03 PM")]
    // With a year, unchanged.
    [InlineData("{ era: 'short', year: 'numeric' }", "2026 AD")]
    [InlineData("{ era: 'short' }", "10/16/2026 AD")]
    public void The_era_is_written_beside_a_format_with_no_year(string options, string expected)
        => Assert.Equal(expected, Format("en", options));

    // A lone day, month or weekday beside each era width, all 30 of V8's layouts: the era first
    // where ICU starts from it, the day or month appended "{0} ({2}: {1})" and a weekday "{0} {1}";
    // the field first where its own skeleton is nearer the requested width.
    [Theory]
    [InlineData("short", "day: 'numeric'", "AD (day: 16)")]
    [InlineData("short", "day: '2-digit'", "AD (day: 16)")]
    [InlineData("short", "month: 'long'", "AD (month: October)")]
    [InlineData("short", "month: 'short'", "AD (month: Oct)")]
    [InlineData("short", "month: 'narrow'", "AD (month: O)")]
    [InlineData("short", "month: 'numeric'", "AD (month: 10)")]
    [InlineData("short", "month: '2-digit'", "AD (month: 10)")]
    [InlineData("short", "weekday: 'short'", "AD Fri")]
    [InlineData("short", "weekday: 'long'", "AD Friday")]
    [InlineData("short", "weekday: 'narrow'", "AD F")]
    [InlineData("long", "day: 'numeric'", "16 Anno Domini")]
    [InlineData("long", "day: '2-digit'", "Anno Domini (day: 16)")]
    [InlineData("long", "month: 'long'", "Anno Domini (month: October)")]
    [InlineData("long", "month: 'short'", "Oct Anno Domini")]
    [InlineData("long", "month: 'narrow'", "Anno Domini (month: O)")]
    [InlineData("long", "month: 'numeric'", "10 Anno Domini")]
    [InlineData("long", "month: '2-digit'", "Anno Domini (month: 10)")]
    [InlineData("long", "weekday: 'short'", "Fri Anno Domini")]
    [InlineData("long", "weekday: 'long'", "Anno Domini Friday")]
    [InlineData("long", "weekday: 'narrow'", "Anno Domini F")]
    [InlineData("narrow", "day: 'numeric'", "16 A")]
    [InlineData("narrow", "day: '2-digit'", "16 A")]
    [InlineData("narrow", "month: 'long'", "October A")]
    [InlineData("narrow", "month: 'short'", "Oct A")]
    [InlineData("narrow", "month: 'narrow'", "A (month: O)")]
    [InlineData("narrow", "month: 'numeric'", "10 A")]
    [InlineData("narrow", "month: '2-digit'", "10 A")]
    [InlineData("narrow", "weekday: 'short'", "Fri A")]
    [InlineData("narrow", "weekday: 'long'", "Friday A")]
    [InlineData("narrow", "weekday: 'narrow'", "A F")]
    public void A_lone_field_is_laid_out_as_ICU_lays_it_out(string era, string field, string expected)
        => Assert.Equal(expected, Format("en", $"{{ era: '{era}', {field} }}"));

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
