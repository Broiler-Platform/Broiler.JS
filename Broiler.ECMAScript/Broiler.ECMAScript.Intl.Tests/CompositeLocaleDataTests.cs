using System;
using System.Collections.Generic;
using Xunit;

namespace Broiler.ECMAScript.Intl.Tests;

/// <summary>
/// <see cref="CompositeLocaleData"/>'s order (Broiler.VM's JSD-0082, decision D8): the shared data first, then the optional
/// secondary provider, then the locale's parent and root; and the secondary provider only for a locale the shared data lacks.
/// </summary>
public sealed class CompositeLocaleDataTests
{
    private static readonly TableLocaleData Shared = new(new TextDataSet(Tables()));

    private static Dictionary<IntlTable, string> Tables()
    {
        var tables = TableLocaleDataTests.SmallTables();
        tables[IntlTable.NumberLocales] =
            "de|symbols.decimal|,\nen|symbols.group|,\nfr|symbols.decimal|,\nfr|decimal.standard|#,##0.###\nund|symbols.decimal|.\nund|symbols.nan|NaN\nzh|zh.only|zh\nzh-Hant|symbols.decimal|.";
        tables[IntlTable.DisplayNames] = "en|language|de|German\nund|language|und|Unknown language";
        return tables;
    }

    /// <summary>A secondary provider that answers a few questions for any locale, and counts what it is asked.</summary>
    private sealed class Secondary : ILocaleData
    {
        internal List<string> Asked { get; } = [];

        public IReadOnlyList<string> Locales => ["xx-SECONDARY"];

        public bool HoldsLocale(IntlTable table, string locale) => true;

        public string? LikelySubtags(string source) => source == "qq" ? "qq-Latn-QQ" : "secondary";

        public string? Alias(LocaleAliasKind kind, string from) => null;

        public bool IsExtensionKey(char singleton, string key) => key == "zz";

        public bool TryExtensionType(char singleton, string key, string type, out string preferred)
        {
            preferred = "secondary-" + type;
            return key == "zz";
        }

        public RegionHourCycles? HourCycles(string region) => region == "QQ" ? new RegionHourCycles("H", "H") : null;

        public RegionWeek? Week(string region) => null;

        public bool? IsRightToLeft(string script) => null;

        public string? NumberValue(string locale, string key)
        {
            Asked.Add(locale + "|" + key);
            return key == "symbols.decimal" ? "host" : null;
        }

        public CurrencyNames? Currency(string locale, string code) =>
            code == "XQQ" ? new CurrencyNames("Q", "Q", "q", "qs", "Q", "SS", "SS") : null;

        public IReadOnlyList<string> CurrencyCodes => ["XQQ"];

        public int? CurrencyDigits(string code) => code == "XQQ" ? 4 : null;

        public IReadOnlyList<string> NumberingSystems => ["qqqq"];

        public IReadOnlyList<string>? NumberingSystemDigits(string system) => null;

        public IReadOnlyList<PluralRuleText>? PluralRules(string language, PluralRuleType type) => null;

        public string? PluralRange(string language, string start, string end) => null;

        public string? UnitPattern(string locale, string width, string unit, string field) => null;

        public ListPatterns? ListPattern(string locale, string type) => null;

        public string? RelativeTime(string locale, string field, string key) => null;

        public string? DisplayName(string locale, string kind, string code) => "secondary";

        public string? DateValue(string locale, string calendar, string key) => null;

        public IReadOnlyDictionary<string, string>? DateValues(string locale, string calendar) => null;

        public IReadOnlyList<DayPeriodRule>? DayPeriods(string language) => null;
    }

    [Fact]
    public void The_chain_is_the_locale_its_truncations_and_root()
    {
        Assert.Equal(["zh-Hant-TW", "zh-Hant", "zh", "und"], CompositeLocaleData.Chain("zh-Hant-TW"));
        Assert.Equal(["en", "und"], CompositeLocaleData.Chain("en"));
        Assert.Equal(["und"], CompositeLocaleData.Chain("und"));
        Assert.Equal(["und-Latn", "und"], CompositeLocaleData.Chain("und-Latn"));
        Assert.Equal(["", "und"], CompositeLocaleData.Chain(""));
        Assert.Throws<ArgumentNullException>(() => CompositeLocaleData.Chain(null!));
    }

    [Fact]
    public void Without_a_secondary_provider_a_locale_reads_its_parent_and_then_root()
    {
        var composite = new CompositeLocaleData(Shared);
        Assert.Equal(",", composite.NumberValue("de-DE", "symbols.decimal"));
        Assert.Equal(",", composite.NumberValue("de", "symbols.decimal"));
        Assert.Equal(",", composite.NumberValue("fr-CH", "symbols.decimal"));
        Assert.Equal(".", composite.NumberValue("xx-YY", "symbols.decimal"));
        Assert.Equal("NaN", composite.NumberValue("xx", "symbols.nan"));
        Assert.Null(composite.NumberValue("xx", "symbols.none"));
    }

    [Fact]
    public void A_locale_whose_table_the_shared_data_holds_takes_nothing_from_its_parent_or_root()
    {
        var composite = new CompositeLocaleData(Shared);

        // zh-Hant is held: zh's key is not its, and neither is root's symbols.nan.
        Assert.Null(composite.NumberValue("zh-Hant", "zh.only"));
        Assert.Null(composite.NumberValue("zh-Hant-TW", "symbols.nan"));
        Assert.Equal(".", composite.NumberValue("zh-Hant-TW", "symbols.decimal"));

        // A table that does not hold a locale passes the question on: de's display names come from root.
        Assert.Equal("Unknown language", composite.DisplayName("de", "language", "und"));
        Assert.Null(composite.DisplayName("en", "language", "und"));
    }

    [Fact]
    public void The_secondary_provider_answers_only_for_a_locale_the_shared_data_lacks()
    {
        var secondary = new Secondary();
        var composite = new CompositeLocaleData(Shared, secondary);

        Assert.Equal(",", composite.NumberValue("de", "symbols.decimal"));
        Assert.Equal(",", composite.NumberValue("fr", "symbols.decimal"));
        Assert.Empty(secondary.Asked);

        // fr-CH: the shared data lacks it, so the secondary provider is asked before the parent.
        Assert.Equal("host", composite.NumberValue("fr-CH", "symbols.decimal"));
        Assert.Equal(["fr-CH|symbols.decimal"], secondary.Asked);

        // A question the secondary provider does not answer goes on to the parent, which holds the table.
        secondary.Asked.Clear();
        Assert.Equal("#,##0.###", composite.NumberValue("fr-CH", "decimal.standard"));
        Assert.Equal(["fr-CH|decimal.standard"], secondary.Asked);

        // A held locale's missing value is not asked of the secondary provider.
        secondary.Asked.Clear();
        Assert.Null(composite.NumberValue("fr", "symbols.nan"));
        Assert.Empty(secondary.Asked);

        Assert.Equal("German", composite.DisplayName("en", "language", "de"));
        Assert.Equal("secondary", composite.DisplayName("fr", "language", "de"));
    }

    [Fact]
    public void A_locale_the_shared_data_lists_reads_its_parent_and_never_the_secondary_provider()
    {
        // Decision D6: en-US and de-DE are listed - CLDR's default-content locales, which hold no lines of their own - so each
        // reads its parent within the shared data, and the host is never asked, though it would answer.
        var secondary = new Secondary();
        var composite = new CompositeLocaleData(Shared, secondary);

        Assert.Equal(",", composite.NumberValue("en-US", "symbols.group"));
        Assert.Null(composite.NumberValue("en-US", "symbols.decimal"));
        Assert.Equal(",", composite.NumberValue("de-DE", "symbols.decimal"));
        Assert.Equal("German", composite.DisplayName("en-US", "language", "de"));
        Assert.Equal("Unknown language", composite.DisplayName("de-DE", "language", "und"));
        Assert.Empty(secondary.Asked);

        // und is listed too: root's data is the shared data's alone.
        Assert.Null(composite.NumberValue("und", "symbols.none"));
        Assert.Empty(secondary.Asked);

        // A locale the shared data neither lists nor holds is still the secondary provider's.
        Assert.Equal("host", composite.NumberValue("en-AU", "symbols.decimal"));
        Assert.Equal(["en-AU|symbols.decimal"], secondary.Asked);
    }

    [Fact]
    public void The_world_s_data_is_the_shared_data_s_then_the_secondary_provider_s()
    {
        var composite = new CompositeLocaleData(Shared, new Secondary());
        Assert.Equal("en-Latn-US", composite.LikelySubtags("en"));
        Assert.Equal("qq-Latn-QQ", composite.LikelySubtags("qq"));
        Assert.True(composite.IsExtensionKey('u', "ca"));
        Assert.True(composite.IsExtensionKey('u', "zz"));
        Assert.True(composite.TryExtensionType('u', "ca", "islamicc", out var preferred));
        Assert.Equal("islamic-civil", preferred);
        Assert.True(composite.TryExtensionType('u', "zz", "x", out preferred));
        Assert.Equal("secondary-x", preferred);
        Assert.False(composite.TryExtensionType('u', "yy", "x", out preferred));
        Assert.Equal("x", preferred);
        Assert.Equal(new RegionHourCycles("h H hB hb", "h"), composite.HourCycles("US"));
        Assert.Equal(new RegionHourCycles("H", "H"), composite.HourCycles("QQ"));
        Assert.Equal(4, composite.CurrencyDigits("XQQ"));
        Assert.Equal(0, composite.CurrencyDigits("JPY"));
        Assert.Equal("Q", composite.Currency("fr-CH", "XQQ")!.Value.Symbol);

        // The lists are the shared data's alone.
        Assert.Equal(Shared.Locales, composite.Locales);
        Assert.Equal(Shared.CurrencyCodes, composite.CurrencyCodes);
        Assert.Equal(Shared.NumberingSystems, composite.NumberingSystems);
        Assert.Equal(Shared.HoldsLocale(IntlTable.NumberLocales, "fr"), composite.HoldsLocale(IntlTable.NumberLocales, "fr"));
        Assert.False(composite.HoldsLocale(IntlTable.NumberLocales, "fr-CH"));
    }

    [Fact]
    public void Every_question_about_a_locale_walks_the_chain()
    {
        var composite = new CompositeLocaleData(Shared);
        Assert.Equal("$", composite.Currency("en-US", "USD")!.Value.Symbol);
        Assert.Equal("i = 1 and v = 0", composite.PluralRules("en-US", PluralRuleType.Cardinal)![0].Rule);
        Assert.Equal("one", composite.PluralRules("en-GB", PluralRuleType.Ordinal)![0].Category);
        Assert.Equal("other", composite.PluralRange("en-US", "one", "other"));
        Assert.Equal("meters", composite.UnitPattern("en-US", "long", "meter", "displayName"));
        Assert.Equal("{0} and {1}", composite.ListPattern("en-US", "standard")!.Value.Pair);
        Assert.Equal("yesterday", composite.RelativeTime("en-US", "day", "-1"));
        Assert.Equal("January", composite.DateValue("en-US", "gregory", "months.format.wide.1"));
        Assert.Equal("Muharram", composite.DateValue("en-US", "islamic-civil", "months.format.wide.1"));
        Assert.Equal(4, composite.DateValues("en-US", "gregory")!.Count);
        Assert.Equal(2, composite.DayPeriods("en-US")!.Count);
        Assert.Null(composite.DayPeriods("de-DE"));
    }

    [Fact]
    public void The_shared_data_is_required()
    {
        Assert.Throws<ArgumentNullException>(() => new CompositeLocaleData(null!));
    }
}
