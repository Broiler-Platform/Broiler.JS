using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Intl.DateTimeFormat.prototype.formatToParts wrote a format with minute and second but no hour as
// "mm:ss[.fff]" by hand, where `format` writes the resolved pattern. The hand-written parts kept ASCII
// digits and a "." whatever the numbering system (ar-EG's default "arab" formats as "٠٣:٠٥"), and
// dropped every other field the format asks for: a date field, a weekday or a time-zone name. Such a
// format now takes the pattern `format` takes, so its parts join to what `format` writes.
public class DateTimeFormatMinuteSecondPartsTests
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

    private static string Parts(string locale, string options)
        => Eval($"new Intl.DateTimeFormat('{locale}', {options}).formatToParts({Instant})"
            + ".map(p => p.type + '=' + p.value).join('|')");

    [Theory]
    // A locale's default numbering system, and one a -u-nu- extension chooses.
    [InlineData("ar-EG", "minute=٠٣|literal=:|second=٠٥")]
    [InlineData("fa", "minute=۰۳|literal=:|second=۰۵")]
    [InlineData("bn", "minute=০৩|literal=:|second=০৫")]
    [InlineData("en-u-nu-arab", "minute=٠٣|literal=:|second=٠٥")]
    [InlineData("en-u-nu-hanidec", "minute=〇三|literal=:|second=〇五")]
    // Latin digits are unchanged.
    [InlineData("en", "minute=03|literal=:|second=05")]
    public void The_digits_follow_the_numbering_system(string locale, string expected)
        => Assert.Equal(expected, Parts(locale, "{ minute: '2-digit', second: '2-digit', timeZone: 'UTC' }"));

    [Theory]
    // arab writes the fraction after its own decimal separator, U+066B; beng keeps ".".
    [InlineData("en-u-nu-arab", "minute=٠٣|literal=:|second=٠٥|literal=٫|fractionalSecond=٦٧٨")]
    [InlineData("bn", "minute=০৩|literal=:|second=০৫|literal=.|fractionalSecond=৬৭৮")]
    [InlineData("en", "minute=03|literal=:|second=05|literal=.|fractionalSecond=678")]
    public void Fractional_seconds_follow_the_numbering_system(string locale, string expected)
        => Assert.Equal(expected,
            Parts(locale, "{ minute: '2-digit', second: '2-digit', fractionalSecondDigits: 3, timeZone: 'UTC' }"));

    [Theory]
    [InlineData("day: 'numeric'", "day")]
    [InlineData("month: 'short'", "month")]
    [InlineData("weekday: 'short'", "weekday")]
    [InlineData("timeZoneName: 'short'", "timeZoneName")]
    public void The_other_fields_the_format_asks_for_are_written(string field, string type)
        => Assert.Equal("true", Eval($$"""
            var parts = new Intl.DateTimeFormat('en', { {{field}}, minute: '2-digit', second: '2-digit', timeZone: 'UTC' })
                .formatToParts({{Instant}});
            parts.some(p => p.type === '{{type}}');
        """));

    [Theory]
    [InlineData("en", "{ minute: 'numeric', second: 'numeric' }")]
    [InlineData("de", "{ minute: '2-digit', second: '2-digit', fractionalSecondDigits: 2 }")]
    [InlineData("ar-EG", "{ minute: '2-digit', second: '2-digit' }")]
    [InlineData("ar-EG", "{ minute: '2-digit', second: '2-digit', fractionalSecondDigits: 1 }")]
    [InlineData("fa", "{ minute: 'numeric', second: '2-digit', fractionalSecondDigits: 3 }")]
    [InlineData("en-u-nu-hanidec", "{ minute: '2-digit', second: '2-digit', fractionalSecondDigits: 2 }")]
    [InlineData("en", "{ day: 'numeric', minute: '2-digit', second: '2-digit' }")]
    [InlineData("en", "{ weekday: 'short', minute: '2-digit', second: '2-digit' }")]
    [InlineData("en", "{ minute: '2-digit', second: '2-digit', timeZoneName: 'short' }")]
    public void The_parts_join_to_format(string locale, string options)
        => Assert.Equal("true", Eval($$"""
            var dtf = new Intl.DateTimeFormat('{{locale}}', Object.assign({ timeZone: 'UTC' }, {{options}}));
            dtf.formatToParts({{Instant}}).map(p => p.value).join('') === dtf.format({{Instant}});
        """));
}
