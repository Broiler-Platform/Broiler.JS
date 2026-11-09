using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Intl.DateTimeFormat over the shared Intl library (SJL-9.5, task 8.5, sub-area (d), 2026-11-09): the answers the cut-over
// corrects, each with its row of Broiler.VM's bundle sjl-9-001 correspondence.
// - Row T4 (a correction): the bound format is made once, so dtf.format === dtf.format.
// - Row T2 (a correction): Date.prototype.toLocaleString and its two siblings read each option once, through
//   CreateDateTimeFormat's required and defaults, where ToDateTimeOptions read the components and styles a first time.
// - Row T37 with decision D21 (data policy): a numbering system writes its own decimal symbol from the data, U+066B for
//   arab, where a hand table in the engine gave it; and de's arab digits take the same symbol.
// - Rows T14 and T12 (a correction and data policy): German names and CLDR 48.2.0's atTime join.
// - Rows T2 and T38 (corrections, noted 2026-11-09 at the review of task 8.5): the options go through CoerceOptionsToObject,
//   so options null is a TypeError for Date.prototype's toLocale* methods (ToDateTimeOptions read it as an empty bag) and
//   for Temporal.ZonedDateTime.prototype.toLocaleString (read as an empty bag before), and a primitive such as 5 is boxed
//   for the latter, which formats where it was a TypeError.
public class IntlDateTimeFormatLibraryTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    private const string Instant = "new Date(Date.UTC(2024, 0, 5, 15, 7, 9, 789))";

    [Fact]
    public void The_bound_format_is_made_once()
        => Assert.Equal("true|1|", Eval(
            "var d = new Intl.DateTimeFormat('en'); var f = d.format; [f === d.format, f.length, f.name].join('|')"));

    [Fact]
    public void Date_toLocaleString_reads_each_option_once()
        => Assert.Equal("tz,year", Eval(
            "var r = []; var o = { get year() { r.push('year'); return 'numeric'; }, get timeZone() { r.push('tz'); return 'UTC'; } };"
            + Instant + ".toLocaleString('en', o); r.join()"));

    [Theory]
    [InlineData("en-US-u-nu-arab", "\u0663:\u0660\u0667:\u0660\u0669\u066B\u0667\u0668\u0669\u202FPM")]
    [InlineData("de-u-nu-arab", "\u0661\u0665:\u0660\u0667:\u0660\u0669\u066B\u0667\u0668\u0669")]
    [InlineData("en-US-u-nu-deva", "\u0969:\u0966\u096D:\u0966\u096F.\u096D\u096E\u096F\u202FPM")]
    public void A_numbering_system_writes_its_own_decimal_symbol(string locale, string expected)
        => Assert.Equal(expected, Eval(
            $"new Intl.DateTimeFormat('{locale}', {{ timeZone: 'UTC', hour: 'numeric', minute: 'numeric', second: 'numeric', fractionalSecondDigits: 3 }}).format({Instant})"));

    [Theory]
    [InlineData("toLocaleString")]
    [InlineData("toLocaleDateString")]
    [InlineData("toLocaleTimeString")]
    public void Date_toLocale_with_options_null_is_a_TypeError(string method)
        => Assert.Equal("TypeError", Eval(
            $"try {{ {Instant}.{method}('en', null); 'none' }} catch (e) {{ e.name }}"));

    [Fact]
    public void ZonedDateTime_toLocaleString_with_options_null_is_a_TypeError()
        => Assert.Equal("TypeError", Eval(
            "try { Temporal.ZonedDateTime.from('2024-01-05T15:07:09[UTC]').toLocaleString('en', null); 'none' } catch (e) { e.name }"));

    [Fact]
    public void ZonedDateTime_toLocaleString_boxes_a_primitive_options_value()
        => Assert.Equal(
            Eval("Temporal.ZonedDateTime.from('2024-01-05T15:07:09[UTC]').toLocaleString('en')"),
            Eval("Temporal.ZonedDateTime.from('2024-01-05T15:07:09[UTC]').toLocaleString('en', 5)"));

    [Fact]
    public void German_names_and_CLDR_s_join()
        => Assert.Equal("Freitag, 5. Januar 2024 um 15:07:09 UTC", Eval(
            $"new Intl.DateTimeFormat('de', {{ timeZone: 'UTC', dateStyle: 'full', timeStyle: 'long' }}).format({Instant})"));
}
