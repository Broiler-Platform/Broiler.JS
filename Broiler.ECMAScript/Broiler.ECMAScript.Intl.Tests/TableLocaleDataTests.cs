using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Broiler.ECMAScript.Intl.Tests;

/// <summary>
/// The decoder implementation over small tables in the data home's format: every question, the date layers Broiler.VM's
/// <c>ReadDates</c> builds, the locales each table holds, and the decoding once per process (decision D23's L3 reading).
/// </summary>
public sealed class TableLocaleDataTests
{
    internal static Dictionary<IntlTable, string> SmallTables() => new()
    {
        [IntlTable.LikelySubtags] = "en|en-Latn-US\nund|en-Latn-US\nund-Arab|ar-Arab-EG",
        [IntlTable.Aliases] = "language|iw|he\nlanguage|sgn-GR|gss\nscript|Qaai|Zinh\nregion|SU|RU AM AZ\nvariant|heploc|alalc97\nsubdivision|fi01|AX",
        [IntlTable.Extensions] = "u|ca|gregory|\nu|ca|islamicc|islamic-civil\nu|ca||\nt|m0|names|prprname\nt|m0||",
        [IntlTable.Locales] = "de\nde-DE\nen\nen-US\nund",
        [IntlTable.NumberLocales] = "en|symbols.decimal|.\nen|decimal.standard|#,##0.###\nde|symbols.decimal|,\nde|symbols.group|\\u00A0",
        [IntlTable.Currencies] = "en|USD|$|$|US dollar|US dollars|US Dollar|SS|SS\nde|EUR|\\u20AC|\\u20AC|Euro|Euro|Euro|SS|SS\nen|EUR|\\u20AC|\\u20AC|euro|euros|Euro|SS|SS",
        [IntlTable.CurrencyDigits] = "JPY|0\nKWD|3",
        [IntlTable.NumberingSystems] = "adlm|\\uD83A\\uDD50\\uD83A\\uDD51\\uD83A\\uDD52\\uD83A\\uDD53\\uD83A\\uDD54\\uD83A\\uDD55\\uD83A\\uDD56\\uD83A\\uDD57\\uD83A\\uDD58\\uD83A\\uDD59\narab|\\u0660\\u0661\\u0662\\u0663\\u0664\\u0665\\u0666\\u0667\\u0668\\u0669\nlatn|0123456789",
        [IntlTable.Plurals] = "en|one|i = 1 and v = 0\nen|other|\nde|one|i = 1 and v = 0\nde|other|",
        [IntlTable.Ordinals] = "en|one|n % 10 = 1 and n % 100 != 11\nen|other|",
        [IntlTable.PluralRanges] = "en|one|other|other\nen|other|one|other",
        [IntlTable.Units] = "en|long|meter|displayName|meters\nen|long|meter|unitPattern-count-one|{0} meter",
        [IntlTable.DateLocales] =
            "@iso8601|dateFormats.short|y-MM-dd\n@iso8601|dateTime.short|{1}, {0}\n"
            + "en|months.format.wide.1|January\nen|dateFormats.short|M/d/yy\nen|dateTime.short|{1}, {0}\nen|available.yMd|M/d/y\n"
            + "en@generic|available.yMd|M/d/y G\nen@generic|-dateTime.short|\n"
            + "en@islamic|months.format.wide.1|Muharram\nen@islamic-civil|available.yMd|d/M/y G",
        [IntlTable.TimeData] = "001|H h|H\nUS|h H hB hb|h",
        [IntlTable.DayPeriods] = "en|midnight|00:00|\nen|morning1|06:00|12:00",
        [IntlTable.WeekData] = "001|mon|sat|sun|1\nUS|sun|||",
        [IntlTable.Scripts] = "Arab|YES\nLatn|NO",
        [IntlTable.ListPatterns] = "en|standard|{0}, {1}|{0}, {1}|{0}, and {1}|{0} and {1}",
        [IntlTable.RelativeTimes] = "en|day|-1|yesterday\nen|day|future.one|in {0} day",
        [IntlTable.DisplayNames] = "en|language|de|German\nen|region|DE|Germany",
    };

    private static TableLocaleData Small(out TextDataSet data)
    {
        data = new TextDataSet(SmallTables());
        return new TableLocaleData(data);
    }

    [Fact]
    public void The_identifier_data_answers_as_the_tables_hold_it()
    {
        var locale = Small(out _);
        Assert.Equal("en-Latn-US", locale.LikelySubtags("en"));
        Assert.Null(locale.LikelySubtags("xx"));
        Assert.Equal("he", locale.Alias(LocaleAliasKind.Language, "iw"));
        Assert.Equal("gss", locale.Alias(LocaleAliasKind.Language, "sgn-GR"));
        Assert.Equal("Zinh", locale.Alias(LocaleAliasKind.Script, "Qaai"));
        Assert.Equal("RU AM AZ", locale.Alias(LocaleAliasKind.Region, "SU"));
        Assert.Equal("alalc97", locale.Alias(LocaleAliasKind.Variant, "heploc"));
        Assert.Equal("AX", locale.Alias(LocaleAliasKind.Subdivision, "fi01"));
        Assert.Null(locale.Alias(LocaleAliasKind.Region, "iw"));
        Assert.Null(locale.Alias((LocaleAliasKind)9, "iw"));

        Assert.True(locale.IsExtensionKey('u', "ca"));
        Assert.False(locale.IsExtensionKey('t', "ca"));
        Assert.True(locale.TryExtensionType('u', "ca", "gregory", out var preferred));
        Assert.Equal("gregory", preferred);
        Assert.True(locale.TryExtensionType('u', "ca", "islamicc", out preferred));
        Assert.Equal("islamic-civil", preferred);
        Assert.False(locale.TryExtensionType('u', "ca", "nope", out preferred));
        Assert.Equal("nope", preferred);

        Assert.Equal(["de", "de-DE", "en", "en-US", "und"], locale.Locales);
        Assert.Equal("48.2.0", locale.CldrVersion);
        Assert.Equal(new RegionHourCycles("h H hB hb", "h"), locale.HourCycles("US"));
        Assert.Null(locale.HourCycles("ZZ"));
        Assert.Equal(new RegionWeek("sun", "", "", ""), locale.Week("US"));
        Assert.True(locale.IsRightToLeft("Arab"));
        Assert.False(locale.IsRightToLeft("Latn"));
        Assert.Null(locale.IsRightToLeft("Zzzz"));
    }

    [Fact]
    public void The_number_data_answers_as_the_tables_hold_it()
    {
        var locale = Small(out _);
        Assert.Equal(",", locale.NumberValue("de", "symbols.decimal"));
        Assert.Equal("\u00A0", locale.NumberValue("de", "symbols.group"));
        Assert.Null(locale.NumberValue("de-DE", "symbols.decimal"));
        Assert.Equal(new CurrencyNames("$", "$", "US dollar", "US dollars", "US Dollar", "SS", "SS"), locale.Currency("en", "USD"));
        Assert.Equal("\u20AC", locale.Currency("de", "EUR")!.Value.Symbol);
        Assert.Null(locale.Currency("de", "USD"));
        Assert.Equal(["USD", "EUR"], locale.CurrencyCodes);
        Assert.Equal(3, locale.CurrencyDigits("KWD"));
        Assert.Null(locale.CurrencyDigits("USD"));
        Assert.Equal(["adlm", "arab", "latn"], locale.NumberingSystems);
        Assert.Equal("\uD83A\uDD59", locale.NumberingSystemDigits("adlm")![9]);
        Assert.Equal("\u0663", locale.NumberingSystemDigits("arab")![3]);
        Assert.Equal(10, locale.NumberingSystemDigits("latn")!.Count);
        Assert.Null(locale.NumberingSystemDigits("thai"));
        Assert.Equal([new PluralRuleText("one", "i = 1 and v = 0"), new PluralRuleText("other", "")], locale.PluralRules("en", PluralRuleType.Cardinal));
        Assert.Equal("one", locale.PluralRules("en", PluralRuleType.Ordinal)![0].Category);
        Assert.Null(locale.PluralRules("de", PluralRuleType.Ordinal));
        Assert.Null(locale.PluralRules("en", (PluralRuleType)7));
        Assert.Equal("other", locale.PluralRange("en", "one", "other"));
        Assert.Null(locale.PluralRange("en", "one", "one"));
        Assert.Equal("{0} meter", locale.UnitPattern("en", "long", "meter", "unitPattern-count-one"));
        Assert.Null(locale.UnitPattern("en", "short", "meter", "unitPattern-count-one"));
    }

    [Fact]
    public void The_small_formatters_data_answers_as_the_tables_hold_it()
    {
        var locale = Small(out _);
        Assert.Equal(new ListPatterns("{0}, {1}", "{0}, {1}", "{0}, and {1}", "{0} and {1}"), locale.ListPattern("en", "standard"));
        Assert.Null(locale.ListPattern("en", "or"));
        Assert.Equal("yesterday", locale.RelativeTime("en", "day", "-1"));
        Assert.Equal("German", locale.DisplayName("en", "language", "de"));
        Assert.Null(locale.DisplayName("de", "language", "de"));
    }

    [Fact]
    public void The_date_data_is_Broiler_VM_s_layers()
    {
        var locale = Small(out _);
        Assert.Equal("January", locale.DateValue("en", "gregory", "months.format.wide.1"));

        // The ISO 8601 calendar: the language's names and date-time glue under root's ISO patterns (JSD-0055).
        Assert.Equal("y-MM-dd", locale.DateValue("en", "iso8601", "dateFormats.short"));
        Assert.Equal("{1}, {0}", locale.DateValue("en", "iso8601", "dateTime.short"));
        Assert.Equal("January", locale.DateValue("en", "iso8601", "months.format.wide.1"));
        Assert.Null(locale.DateValue("en", "iso8601", "available.yMd"));

        // The generic layer over the Gregorian data, removing what its '-' keys name, and each calendar over it (JSD-0057).
        Assert.Equal("M/d/y G", locale.DateValue("en", "buddhist", "available.yMd"));
        Assert.Null(locale.DateValue("en", "buddhist", "dateTime.short"));
        Assert.Equal("Muharram", locale.DateValue("en", "islamic", "months.format.wide.1"));
        Assert.Equal("Muharram", locale.DateValue("en", "islamic-civil", "months.format.wide.1"));
        Assert.Equal("d/M/y G", locale.DateValue("en", "islamic-civil", "available.yMd"));
        Assert.Equal("M/d/y G", locale.DateValue("en", "islamic-umalqura", "available.yMd"));
        Assert.Null(locale.DateValue("en", "generic", "available.yMd"));
        Assert.Null(locale.DateValue("en@islamic", "gregory", "months.format.wide.1"));
        Assert.Null(locale.DateValue("en", "", "months.format.wide.1"));

        Assert.Equal(["months.format.wide.1", "dateFormats.short", "dateTime.short", "available.yMd"], locale.DateValues("en", "gregory")!.Keys);
        Assert.Null(locale.DateValues("de", "gregory"));

        Assert.Equal([new DayPeriodRule("midnight", 0, -1, At: true), new DayPeriodRule("morning1", 360, 720, At: false)], locale.DayPeriods("en"));
        Assert.Null(locale.DayPeriods("de"));
    }

    [Fact]
    public void A_table_holds_the_locales_it_has_lines_for()
    {
        var locale = Small(out _);
        Assert.True(locale.HoldsLocale(IntlTable.NumberLocales, "de"));
        Assert.False(locale.HoldsLocale(IntlTable.NumberLocales, "de-DE"));
        Assert.True(locale.HoldsLocale(IntlTable.Currencies, "de"));
        Assert.True(locale.HoldsLocale(IntlTable.Units, "en"));
        Assert.False(locale.HoldsLocale(IntlTable.Units, "de"));
        Assert.True(locale.HoldsLocale(IntlTable.Plurals, "de"));
        Assert.False(locale.HoldsLocale(IntlTable.Ordinals, "de"));
        Assert.True(locale.HoldsLocale(IntlTable.PluralRanges, "en"));
        Assert.True(locale.HoldsLocale(IntlTable.DateLocales, "en"));
        Assert.False(locale.HoldsLocale(IntlTable.DateLocales, "en@islamic"));
        Assert.False(locale.HoldsLocale(IntlTable.DateLocales, "@iso8601"));
        Assert.True(locale.HoldsLocale(IntlTable.DayPeriods, "en"));
        Assert.True(locale.HoldsLocale(IntlTable.ListPatterns, "en"));
        Assert.True(locale.HoldsLocale(IntlTable.RelativeTimes, "en"));
        Assert.True(locale.HoldsLocale(IntlTable.DisplayNames, "en"));
        Assert.False(locale.HoldsLocale(IntlTable.LikelySubtags, "en"));
        Assert.False(locale.HoldsLocale(IntlTable.WeekData, "US"));
    }

    [Fact]
    public void A_data_set_without_tables_answers_no_data()
    {
        var locale = new TableLocaleData(new TextDataSet(new Dictionary<IntlTable, string>()));
        Assert.Empty(locale.Locales);
        Assert.Null(locale.LikelySubtags("en"));
        Assert.Null(locale.Alias(LocaleAliasKind.Language, "iw"));
        Assert.False(locale.IsExtensionKey('u', "ca"));
        Assert.Null(locale.NumberValue("en", "symbols.decimal"));
        Assert.Empty(locale.CurrencyCodes);
        Assert.Empty(locale.NumberingSystems);
        Assert.Null(locale.DateValue("en", "gregory", "dateFormats.short"));
        Assert.Null(locale.DayPeriods("en"));
        Assert.Null(locale.Week("001"));
        Assert.False(locale.HoldsLocale(IntlTable.NumberLocales, "en"));
    }

    [Fact]
    public void Each_table_is_decoded_once_lazily_and_thread_safely()
    {
        var locale = Small(out var data);
        Assert.All(Enum.GetValues<IntlTable>(), t => Assert.Equal(0, data.Reads(t)));

        Parallel.For(0, 64, i =>
        {
            _ = locale.LikelySubtags("en");
            _ = locale.Alias(LocaleAliasKind.Language, "iw");
            _ = locale.IsExtensionKey('u', "ca");
            _ = locale.Locales;
            _ = locale.NumberValue("en", "symbols.decimal");
            _ = locale.DateValue("en", "gregory", "dateFormats.short");
            _ = locale.Week("US");
        });

        foreach (var table in (IntlTable[])[IntlTable.LikelySubtags, IntlTable.Aliases, IntlTable.Extensions, IntlTable.Locales,
            IntlTable.NumberLocales, IntlTable.Currencies, IntlTable.CurrencyDigits, IntlTable.NumberingSystems, IntlTable.Plurals,
            IntlTable.Ordinals, IntlTable.PluralRanges, IntlTable.Units, IntlTable.DateLocales, IntlTable.TimeData, IntlTable.DayPeriods,
            IntlTable.WeekData, IntlTable.Scripts, IntlTable.ListPatterns, IntlTable.RelativeTimes, IntlTable.DisplayNames])
        {
            Assert.Equal(1, data.Reads(table));
        }

        Assert.Equal(0, data.Reads(IntlTable.CollationRoot));
        Assert.Equal(0, data.Reads(IntlTable.MetaZones));
        Assert.Equal(0, data.Reads(IntlTable.TimeZones));
    }

    [Fact]
    public void A_table_out_of_its_format_is_refused_by_the_question_that_decodes_it()
    {
        var tables = SmallTables();
        tables[IntlTable.Aliases] = "planet|Qaai|Zinh";
        tables[IntlTable.Currencies] = "en|USD|$";
        var locale = new TableLocaleData(new TextDataSet(tables));
        Assert.Throws<InvalidDataException>(() => locale.Alias(LocaleAliasKind.Script, "Qaai"));
        Assert.Throws<InvalidDataException>(() => locale.Currency("en", "USD"));
        Assert.Equal("en-Latn-US", locale.LikelySubtags("en"));
    }

    [Fact]
    public void A_numbering_system_without_ten_digits_is_refused()
    {
        // Every other number table well-formed, so that the refusal is the NumberingSystems decoder's own.
        var tables = SmallTables();
        tables[IntlTable.NumberingSystems] = "latn|012";
        var locale = new TableLocaleData(new TextDataSet(tables));
        var refused = Assert.Throws<InvalidDataException>(() => locale.NumberingSystemDigits("latn"));
        Assert.Contains("NumberingSystems table gives latn fewer than ten digits", refused.Message, StringComparison.Ordinal);
        Assert.Throws<InvalidDataException>(() => locale.NumberingSystems);
        Assert.Throws<InvalidDataException>(() => locale.NumberValue("en", "symbols.decimal"));

        // A table the number decoder does not read is unaffected.
        Assert.Equal("en-Latn-US", locale.LikelySubtags("en"));
        Assert.Equal("German", locale.DisplayName("en", "language", "de"));
    }

    [Fact]
    public void A_null_argument_is_refused()
    {
        var locale = Small(out _);
        Assert.Throws<ArgumentNullException>(() => new TableLocaleData(null!));
        Assert.Throws<ArgumentNullException>(() => locale.NumberValue(null!, "symbols.decimal"));
        Assert.Throws<ArgumentNullException>(() => locale.DateValue("en", null!, "x"));
        Assert.Throws<ArgumentNullException>(() => locale.HoldsLocale(IntlTable.NumberLocales, null!));
    }
}
