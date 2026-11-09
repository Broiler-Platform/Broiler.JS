using System;
using System.Collections.Generic;
using System.Linq;
using Broiler.ECMAScript.Numbers;
using Xunit;

namespace Broiler.ECMAScript.Intl.Tests;

/// <summary>
/// The owner's decision of 2026-11-08, "Keep + fix library first": a currency's name in every plural form CLDR gives (row N4 of
/// Broiler.VM's JSD-0082), and the currency style's grouping separator, CLDR's <c>currencyGroup</c> (row N3c), each over the data
/// home's Broiler.JS tables, with their fallbacks: a category without its own form takes <c>other</c>'s, then the display name,
/// then the code; a locale without <c>currencyGroup</c> groups with <c>group</c>.
/// </summary>
public sealed class CurrencyNamesAndGroupingTests
{
    private static readonly CompositeLocaleData Data = new(new TableLocaleData(HomeDataSet.Current));

    private static NumberFormatter Currency(ILocaleData data, string locale, string code, CurrencyDisplay display = CurrencyDisplay.Symbol, int? fractionDigits = null)
    {
        var defaults = DigitOptionRules.NumberFormatDefaults(NumberStyle.Currency, code, NumberNotation.Standard, data);
        var hasFd = fractionDigits is not null;
        var values = new DigitOptionValues(1, false, null, null, hasFd, fractionDigits, fractionDigits, 1, RoundingMode.HalfExpand, RoundingPriority.Auto, TrailingZeroDisplay.Auto);
        Assert.Equal(IntlStatus.Completed, DigitOptionRules.SetNumberFormatDigitOptions(values, defaults, NumberNotation.Standard, out var digits));
        return NumberFormatter.Create(data, locale, locale, "latn", new NumberFormatOptions
        {
            Style = NumberStyle.Currency,
            Currency = code,
            CurrencyDisplay = display,
            DigitOptions = digits!,
            UseGrouping = NumberGrouping.Auto,
        });
    }

    private static NumberFormatter Decimal(ILocaleData data, string locale) =>
        NumberFormatter.Create(data, locale, locale, "latn", new NumberFormatOptions
        {
            Style = NumberStyle.Decimal,
            DigitOptions = Digits(data),
            UseGrouping = NumberGrouping.Auto,
        });

    private static DigitOptions Digits(ILocaleData data)
    {
        var defaults = DigitOptionRules.NumberFormatDefaults(NumberStyle.Decimal, null, NumberNotation.Standard, data);
        var values = new DigitOptionValues(1, false, null, null, false, null, null, 1, RoundingMode.HalfExpand, RoundingPriority.Auto, TrailingZeroDisplay.Auto);
        Assert.Equal(IntlStatus.Completed, DigitOptionRules.SetNumberFormatDigitOptions(values, defaults, NumberNotation.Standard, out var digits));
        return digits!;
    }

    private static string Name(string locale, double value, int? fractionDigits = null) =>
        Currency(Data, locale, "USD", CurrencyDisplay.Name, fractionDigits).Format(IntlDecimal.FromNumber(value));

    // ---- N4: every plural form of the name --------------------------------------------------------------------------------

    [Fact]
    public void N4_Czech_names_a_currency_in_its_one_few_many_and_other_forms()
    {
        // Vector intl/numbers-and-text/families/0082: 2,00 has visible fraction digits, so cs's category is many.
        Assert.Equal("2,00 amerického dolaru", Name("cs", 2));
        Assert.Equal("1 americký dolar", Name("cs", 1, 0));
        Assert.Equal("3 americké dolary", Name("cs", 3, 0));
        Assert.Equal("5 amerických dolarů", Name("cs", 5, 0));
        Assert.Equal(
            [("integer", "2"), ("decimal", ","), ("fraction", "00"), ("literal", " "), ("currency", "amerického dolaru")],
            Currency(Data, "cs", "USD", CurrencyDisplay.Name).FormatToParts(IntlDecimal.FromNumber(2)).Select(static p => (p.Type, p.Value)));
    }

    [Fact]
    public void N4_Lithuanian_names_a_currency_in_its_one_few_many_and_other_forms()
    {
        // Vector intl/numbers-and-text/families/0460: 2,00 is few; a fraction other than zero is many.
        Assert.Equal("2,00 JAV doleriai", Name("lt", 2));
        Assert.Equal("1,00 JAV doleris", Name("lt", 1));
        Assert.Equal("1,50 JAV dolerio", Name("lt", 1.5));
        Assert.Equal("10,00 JAV dolerių", Name("lt", 10));
    }

    [Fact]
    public void N4_Slovenian_names_a_currency_in_its_one_two_few_and_other_forms()
    {
        // Vector intl/numbers-and-text/families/0616: 2,00 has visible fraction digits, so sl's category is few.
        Assert.Equal("2,00 ameriški dolarji", Name("sl", 2));
        Assert.Equal("1 ameriški dolar", Name("sl", 1, 0));
        Assert.Equal("2 ameriška dolarja", Name("sl", 2, 0));
        Assert.Equal("3 ameriški dolarji", Name("sl", 3, 0));
        Assert.Equal("5 ameriških dolarjev", Name("sl", 5, 0));
    }

    [Fact]
    public void N4_A_range_s_name_takes_the_range_s_plural()
    {
        // cs's range rules: other to many is many.
        Assert.Equal(IntlStatus.Completed, Currency(Data, "cs", "USD", CurrencyDisplay.Name).FormatRange(IntlDecimal.FromNumber(1), IntlDecimal.FromNumber(2), out var range));
        Assert.EndsWith(" amerického dolaru", range, StringComparison.Ordinal);
    }

    [Fact]
    public void N4_The_name_falls_back_to_other_then_the_display_name_then_the_code()
    {
        var names = new CurrencyNames("$", "$", "dolar", "dolarů", "americký dolar", "SS", "SS") { Few = "dolary" };
        Assert.Equal("dolary", names.DisplayName("few"));
        Assert.Equal("dolar", names.DisplayName("one"));
        Assert.Equal("dolarů", names.DisplayName("many"));
        Assert.Equal("dolarů", names.DisplayName("two"));
        Assert.Equal("dolarů", names.DisplayName("other"));
        Assert.Equal("dolarů", names.DisplayName("no such category"));
        Assert.Equal("americký dolar", (names with { Other = string.Empty }).DisplayName("many"));
        Assert.Equal(string.Empty, new CurrencyNames("$", "$", string.Empty, string.Empty, string.Empty, "SS", "SS").DisplayName("one"));
        Assert.Equal(string.Empty, default(CurrencyNames).DisplayName("one"));

        // Over data with one and other alone, as Broiler.VM's tables carry them, cs's many takes other's form; with no name
        // at all, the code.
        var oneAndOther = new CompositeLocaleData(new Edited(new TableLocaleData(HomeDataSet.Current), withoutPluralForms: true));
        Assert.Equal("2,00 amerických dolarů", Currency(oneAndOther, "cs", "USD", CurrencyDisplay.Name).Format(IntlDecimal.FromNumber(2)));
        var nameless = new CompositeLocaleData(new Edited(new TableLocaleData(HomeDataSet.Current), withoutNames: true));
        Assert.Equal("2,00 USD", Currency(nameless, "cs", "USD", CurrencyDisplay.Name).Format(IntlDecimal.FromNumber(2)));
    }

    [Fact]
    public void N4_The_decoder_reads_the_four_appended_forms_and_none_from_a_line_of_nine_fields()
    {
        var tables = TableLocaleDataTests.SmallTables();
        tables[IntlTable.Currencies] = "en|USD|$|$|US dollar|US dollars|US Dollar|SS|SS\ncs|USD|US$|$|americk\\u00FD dolar|americk\\u00FDch dolar\\u016F|americk\\u00FD dolar|LS|SS|||americk\\u00E9 dolary|americk\\u00E9ho dolaru";
        var data = new TableLocaleData(new TextDataSet(tables));

        var cs = data.Currency("cs", "USD")!.Value;
        Assert.Equal(("", "", "americké dolary", "amerického dolaru"), (cs.Zero, cs.Two, cs.Few, cs.Many));
        Assert.Equal("americký dolar", cs.One);
        Assert.Equal(new CurrencyNames("$", "$", "US dollar", "US dollars", "US Dollar", "SS", "SS"), data.Currency("en", "USD"));

        var home = Data.Currency("sl", "USD")!.Value;
        Assert.Equal(("", "ameriška dolarja", "ameriški dolarji", ""), (home.Zero, home.Two, home.Few, home.Many));
    }

    // ---- N3c: the currency style's grouping separator -----------------------------------------------------------------------

    [Fact]
    public void N3c_de_AT_groups_a_currency_with_its_currencyGroup_and_a_decimal_with_its_group()
    {
        // Vector intl/numbers-and-text/families/0101.
        Assert.Equal("-€ 1.234,50", Currency(Data, "de-AT", "EUR").Format(IntlDecimal.FromNumber(-1234.5)));
        Assert.Equal("1.234,50 Euro", Currency(Data, "de-AT", "EUR", CurrencyDisplay.Name).Format(IntlDecimal.FromNumber(1234.5)));
        Assert.Equal("1 234,5", Decimal(Data, "de-AT").Format(IntlDecimal.FromNumber(1234.5)));
        Assert.Equal(
            [("minusSign", "-"), ("currency", "€"), ("literal", " "), ("integer", "1"), ("group", "."), ("integer", "234"), ("decimal", ","), ("fraction", "50")],
            Currency(Data, "de-AT", "EUR").FormatToParts(IntlDecimal.FromNumber(-1234.5)).Select(static p => (p.Type, p.Value)));
    }

    [Fact]
    public void N3c_Without_currencyGroup_a_currency_groups_with_group()
    {
        Assert.Equal("1.234,50 €", Currency(Data, "de", "EUR").Format(IntlDecimal.FromNumber(1234.5)));
        Assert.Equal("$1,234.50", Currency(Data, "en", "USD").Format(IntlDecimal.FromNumber(1234.5)));
        Assert.Null(Data.NumberValue("de", "symbols.currencyGroup"));

        var ungrouped = new CompositeLocaleData(new Edited(new TableLocaleData(HomeDataSet.Current), withoutCurrencyGroup: true));
        Assert.Equal("-€ 1 234,50", Currency(ungrouped, "de-AT", "EUR").Format(IntlDecimal.FromNumber(-1234.5)));
    }

    /// <summary>The home's data with currency plural forms, every currency name or <c>currencyGroup</c> taken out.</summary>
    private sealed class Edited(ILocaleData inner, bool withoutPluralForms = false, bool withoutNames = false, bool withoutCurrencyGroup = false) : ILocaleData
    {
        public IReadOnlyList<string> Locales => inner.Locales;

        public bool HoldsLocale(IntlTable table, string locale) => inner.HoldsLocale(table, locale);

        public string? LikelySubtags(string source) => inner.LikelySubtags(source);

        public string? Alias(LocaleAliasKind kind, string from) => inner.Alias(kind, from);

        public bool IsExtensionKey(char singleton, string key) => inner.IsExtensionKey(singleton, key);

        public bool TryExtensionType(char singleton, string key, string type, out string preferred) => inner.TryExtensionType(singleton, key, type, out preferred);

        public RegionHourCycles? HourCycles(string region) => inner.HourCycles(region);

        public RegionWeek? Week(string region) => inner.Week(region);

        public bool? IsRightToLeft(string script) => inner.IsRightToLeft(script);

        public string? NumberValue(string locale, string key) =>
            withoutCurrencyGroup && key.EndsWith(".currencyGroup", StringComparison.Ordinal) ? null : inner.NumberValue(locale, key);

        public CurrencyNames? Currency(string locale, string code) => inner.Currency(locale, code) is not { } names ? null
            : withoutNames ? names with { One = string.Empty, Other = string.Empty, Name = string.Empty, Zero = string.Empty, Two = string.Empty, Few = string.Empty, Many = string.Empty }
            : withoutPluralForms ? names with { Zero = string.Empty, Two = string.Empty, Few = string.Empty, Many = string.Empty }
            : names;

        public IReadOnlyList<string> CurrencyCodes => inner.CurrencyCodes;

        public int? CurrencyDigits(string code) => inner.CurrencyDigits(code);

        public IReadOnlyList<string> NumberingSystems => inner.NumberingSystems;

        public IReadOnlyList<string>? NumberingSystemDigits(string system) => inner.NumberingSystemDigits(system);

        public IReadOnlyList<PluralRuleText>? PluralRules(string language, PluralRuleType type) => inner.PluralRules(language, type);

        public string? PluralRange(string language, string start, string end) => inner.PluralRange(language, start, end);

        public string? UnitPattern(string locale, string width, string unit, string field) => inner.UnitPattern(locale, width, unit, field);

        public ListPatterns? ListPattern(string locale, string type) => inner.ListPattern(locale, type);

        public string? RelativeTime(string locale, string field, string key) => inner.RelativeTime(locale, field, key);

        public string? DisplayName(string locale, string kind, string code) => inner.DisplayName(locale, kind, code);

        public string? DateValue(string locale, string calendar, string key) => inner.DateValue(locale, calendar, key);

        public IReadOnlyDictionary<string, string>? DateValues(string locale, string calendar) => inner.DateValues(locale, calendar);

        public IReadOnlyList<DayPeriodRule>? DayPeriods(string language) => inner.DayPeriods(language);

        public IReadOnlyList<MetazonePeriod>? MetazonePeriods(string zone) => inner.MetazonePeriods(zone);

        public string? GoldenZone(string metazone, string region) => inner.GoldenZone(metazone, region);

        public string? PrimaryZone(string region) => inner.PrimaryZone(region);
    }
}
