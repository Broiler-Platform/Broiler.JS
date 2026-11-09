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
// - Rows T10, T16, T37 and L19 (data policy C5; added 2026-11-09, at SJL-9.5's stage 4, with the shared library's merge
//   Broiler-Platform/Broiler.JS#1108 on the owner's decision of 2026-11-09, "Keep + fix library first"): hour cycles from the
//   locale's own time data (ar-001 h12, fr-CA h23), the whole locale's likely region (zh-Hant Taiwan's h12), the region's
//   calendar preference (fa persian, th buddhist, and the Temporal calendar check that follows from it), a locale's default
//   numbering system's own decimal symbol (fa's arabext, U+066B), and Intl.Locale's hour cycles. Before the merge the first
//   three answered h23, h12 and h23, the calendars gregory, and fa's decimal came from the engine's deleted adapter
//   NumberingSystemDecimalData.
// - Revised 2026-11-09, at SJL-9.5's stage 4 (review): the T37 answer did not move at stage 4. The note above lists it with
//   the moved answers, and the commit that added it called it one; but the deleted adapter NumberingSystemDecimalData gave
//   fa's arabext U+066B before the merge as well, and the library now gives the same answer itself. Its test guards that
//   answer against a regression. A moved answer of row T16 is added: fa's persian calendar has CLDR's ms "m:ss" and no
//   mmss, so { minute: '2-digit', second: '2-digit' } writes "۳:۰۵" (row T13's field widths), where its gregorian calendar
//   writes "۰۳:۰۵" (DateTimeFormatMinuteSecondPartsTests' fa row, revised the same day).
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

    [Theory]
    [InlineData("ar-001", "h12", "3:07 \u0645")]
    [InlineData("fr-CA", "h23", "15 h 07")]
    public void T10_hour_cycles_come_from_the_locale_s_own_time_data(string locale, string cycle, string time)
    {
        Assert.Equal(cycle, Eval($"new Intl.DateTimeFormat('{locale}', {{ hour: 'numeric' }}).resolvedOptions().hourCycle"));
        Assert.Equal(time, Eval(
            $"new Intl.DateTimeFormat('{locale}', {{ timeZone: 'UTC', hour: 'numeric', minute: 'numeric' }}).format({Instant})"));
    }

    [Fact]
    public void T16_zh_Hant_reads_its_likely_region_s_time_data()
        => Assert.Equal("h12|\u4E0B\u53483\u6642", Eval(
            $"var f = new Intl.DateTimeFormat('zh-Hant', {{ timeZone: 'UTC', hour: 'numeric' }}); f.resolvedOptions().hourCycle + '|' + f.format({Instant})"));

    [Theory]
    [InlineData("fa", "persian")]
    [InlineData("fa-IR", "persian")]
    [InlineData("th", "buddhist")]
    [InlineData("th-TH", "buddhist")]
    [InlineData("th-u-ca-gregory", "gregory")]
    [InlineData("en", "gregory")]
    public void T16_the_default_calendar_is_the_region_s_preference(string locale, string calendar)
        => Assert.Equal(calendar, Eval($"new Intl.DateTimeFormat('{locale}').resolvedOptions().calendar"));

    [Theory]
    // fa's persian calendar (CLDR 48.2.0's ca-persian.json and ca-generic.json) has the skeleton ms, "m:ss", and no mmss, so
    // the pattern generator's match keeps the minute's width (row T13); ICU 77.1 answers "۳:۰۵" and minute "numeric" too.
    [InlineData("fa", "\u06F3:\u06F0\u06F5|numeric|persian")]
    [InlineData("fa-u-ca-gregory", "\u06F0\u06F3:\u06F0\u06F5|2-digit|gregory")]
    public void T16_fa_s_default_persian_calendar_writes_its_own_minute_and_second_pattern(string locale, string expected)
        => Assert.Equal(expected, Eval(
            $"var f = new Intl.DateTimeFormat('{locale}', {{ timeZone: 'UTC', minute: '2-digit', second: '2-digit' }}); var r = f.resolvedOptions(); "
            + "f.format(new Date(Date.UTC(2026, 9, 16, 12, 3, 5))) + '|' + r.minute + '|' + r.calendar"));

    [Fact]
    // A gregory-calendar value is refused where the default is buddhist or persian; an ISO value is written in the default.
    public void T16_a_Temporal_date_must_be_in_the_default_calendar()
    {
        Assert.Equal("RangeError", Eval("try { new Temporal.PlainDate(2024, 1, 5, 'gregory').toLocaleString('th'); 'none' } catch (e) { e.name }"));
        Assert.Equal("RangeError", Eval("try { new Temporal.PlainDate(2024, 1, 5, 'gregory').toLocaleString('fa'); 'none' } catch (e) { e.name }"));
        Assert.Equal("5/1/2567", Eval("new Temporal.PlainDate(2024, 1, 5, 'buddhist').toLocaleString('th')"));
        Assert.Equal("5/1/2567", Eval("new Temporal.PlainDate(2024, 1, 5).toLocaleString('th')"));
    }

    [Fact]
    public void T37_a_locale_s_default_numbering_system_writes_its_own_decimal_symbol()
        => Assert.Equal("\u06F9\u066B\u06F7\u06F8", Eval(
            $"new Intl.DateTimeFormat('fa', {{ timeZone: 'UTC', second: 'numeric', fractionalSecondDigits: 2 }}).format({Instant})"));

    [Theory]
    [InlineData("ar-001", "h12,h23")]
    [InlineData("en-001", "h12,h23")]
    [InlineData("fr-CA", "h23,h12")]
    public void L19_Intl_Locale_s_hour_cycles_come_from_the_locale_s_own_time_data(string locale, string cycles)
        => Assert.Equal(cycles, Eval($"new Intl.Locale('{locale}').getHourCycles().join(',')"));
}
