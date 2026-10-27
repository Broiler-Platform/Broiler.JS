using System;
using System.Collections.Generic;
using System.Linq;
using Broiler.ECMAScript.Time;
using Xunit;

namespace Broiler.ECMAScript.Intl.Tests;

/// <summary>
/// Sub-area (a) of Broiler.VM's JSD-0082 over Broiler.JS's tables as the data home commits them: <see cref="LocaleId"/>,
/// <see cref="LocaleNegotiation"/>, <see cref="LocaleRecord"/>, <see cref="LocaleInfo"/> and <see cref="SupportedValues"/>,
/// each answer the one the correspondence's rows L1 to L45 and the Intl vectors record, rows L9 and L18 corrected.
/// </summary>
public sealed class LocaleIdentifierTests
{
    private static readonly TableLocaleData Data = new(HomeDataSet.Current);

    /// <summary>Broiler.VM's providers (decision D7): the default locale and its data's locales.</summary>
    private static readonly IReadOnlyList<string> VmLocales = ["de", "de-DE", "en", "en-US"];

    /// <summary>Broiler.VM's <c>Intl.Collator</c> data (JSD-0043): German's <c>phonebk</c> alone.</summary>
    private sealed class VmCollator : ICollatorLocaleData
    {
        public IReadOnlyList<string> AvailableLocales => VmLocales;

        public IReadOnlyList<string> Collations => ["phonebk"];

        public IReadOnlyList<string?> SortLocaleData(string locale) =>
            LocaleId.TryParse(locale, out var id) == IntlStatus.Completed && id!.Language == "de" ? [null, "phonebk"] : [null];
    }

    private static string Canonical(string tag) =>
        LocaleId.TryCanonicalize(tag, Data, out var canonical) == IntlStatus.Completed ? canonical : "RangeError";

    private static LocaleRecord Locale(string tag, LocaleOptions? options = null)
    {
        Assert.Equal(IntlStatus.Completed, LocaleRecord.TryCreate(tag, options ?? LocaleOptions.None, Data, out var record));
        return record!;
    }

    [Theory]
    [InlineData("")]
    [InlineData("en-")]
    [InlineData("-en")]
    [InlineData("en--US")]
    [InlineData("x")]
    [InlineData("root")]
    [InlineData("en_US")]
    [InlineData("i-klingon")]
    [InlineData("zh-min-nan")]
    [InlineData("en-a-bbb-a-ccc")]
    [InlineData("de-1996-1996")]
    [InlineData("en-u")]
    [InlineData("en-t")]
    [InlineData("en-x")]
    [InlineData("abcdefghi")]
    [InlineData("x-private")]
    [InlineData("zh-cmn-Hans")]
    [InlineData("Ko")]
    [InlineData("en-KE")]
    [InlineData("ſr")]
    [InlineData("İT")]
    [InlineData("en\n")]
    [InlineData("de-DE\n")]
    [InlineData("de-١٢٣")]
    public void A_tag_that_is_not_well_formed_is_refused(string tag)
    {
        Assert.Equal(IntlStatus.InvalidLanguageTag, LocaleId.TryParse(tag, out var locale));
        Assert.Null(locale);
        Assert.Equal("RangeError", Canonical(tag));
    }

    [Theory]
    [InlineData("english", "english")]
    [InlineData("en-u-1a-gregory", "en-u-1a-gregory")]
    [InlineData("DE-1996-1901", "de-1901-1996")]
    [InlineData("en-z-zzz-u-ca-gregory-a-aaa-x-private", "en-a-aaa-u-ca-gregory-z-zzz-x-private")]
    [InlineData("en-u-ca-true", "en-u-ca")]
    [InlineData("tl", "fil")]
    [InlineData("aa-saaho", "ssy")]
    [InlineData("und-aaland", "und-AX")]
    [InlineData("sh", "sr-Latn")]
    [InlineData("ja-Latn-hepburn-heploc", "ja-Latn-alalc97")]
    [InlineData("und-Qaai-US", "und-Zinh-US")]
    [InlineData("en-840", "en-US")]
    [InlineData("und-Cyrl-172", "und-Cyrl-RU")]
    [InlineData("cs-200", "cs-CZ")]
    [InlineData("en-u-tz-aqams", "en-u-tz-aqmcm")]
    [InlineData("en-t-d0-name", "en-t-d0-charname")]
    [InlineData("en-u-ka-yes", "en-u-ka-yes")]
    [InlineData("en-u-kn-yes", "en-u-kn")]
    [InlineData("en-t-sh", "en-t-sr-latn")]
    [InlineData("en-u-sd-cn11", "en-u-sd-cnbj")]
    public void A_tag_canonicalises_as_UTS_35_Annex_C_and_CLDR_48_2_0_s_aliases_have_it(string tag, string canonical)
    {
        Assert.Equal(canonical, Canonical(tag));
        Assert.Equal(canonical, Canonical(canonical));
    }

    [Theory]
    [InlineData("en-u-sd-fi01", "en-u-sd-axzzzz")]
    [InlineData("en-u-rg-fi01", "en-u-rg-axzzzz")]
    [InlineData("en-u-rg-cn71", "en-u-rg-twzzzz")]
    [InlineData("zh-u-sd-cn91", "zh-u-sd-hkzzzz")]
    [InlineData("fr-u-sd-frgp", "fr-u-sd-gpzzzz")]
    [InlineData("fr-u-rg-frgua", "fr-u-rg-gpzzzz")]
    public void A_subdivision_replaced_by_a_region_is_written_as_a_subdivision_row_L9(string tag, string canonical)
    {
        // Row L9, a correction of Broiler.VM (decision D32): Broiler.VM wrote en-u-sd-AX, which is no canonical tag.
        Assert.Equal(canonical, Canonical(tag));
        Assert.Equal(canonical, Canonical(canonical));
        Assert.Equal(canonical, Locale(tag).Locale);
    }

    [Theory]
    [InlineData("zh-TW", "zh-Hant-TW", "zh-TW")]
    [InlineData("und", "en-Latn-US", "en")]
    [InlineData("und-Arab", "ar-Arab-EG", "ar")]
    [InlineData("und-Zzzz", "en-Latn-US", "en")]
    [InlineData("und-ZZ", "en-Latn-US", "en")]
    [InlineData("und-Zzzz-ZZ", "en-Latn-US", "en")]
    [InlineData("en-Zzzz-US", "en-Latn-US", "en")]
    [InlineData("zh-Hant-TW-u-ca-roc-x-q", "zh-Hant-TW-u-ca-roc-x-q", "zh-TW-u-ca-roc-x-q")]
    public void Likely_subtags_are_added_and_removed_as_UTS_35_has_them(string tag, string maximal, string minimal)
    {
        Assert.Equal(IntlStatus.Completed, LocaleId.TryParse(Canonical(tag), out var id));
        Assert.Equal(maximal, id!.Maximize(Data, out var found).ToString());
        Assert.True(found);
        Assert.Equal(minimal, id.Minimize(Data, out _).ToString());

        // Immutable: the identifier itself is unchanged.
        Assert.Equal(Canonical(tag), id.ToString());
    }

    [Fact]
    public void An_identifier_s_parts_are_its_subtags()
    {
        Assert.Equal(IntlStatus.Completed, LocaleId.TryParse("EN-latn-us-FONIPA-u-CA-Gregory-kn-x-Priv", out var id));
        Assert.Equal("en", id!.Language);
        Assert.Equal("Latn", id.Script);
        Assert.Equal("US", id.Region);
        Assert.Equal(["fonipa"], id.Variants);
        Assert.Equal("en-Latn-US-fonipa", id.BaseName);
        Assert.True(id.HasUnicodeExtension);
        Assert.Equal([new LocaleKeyword("ca", "gregory"), new LocaleKeyword("kn", string.Empty)], id.Keywords);
        Assert.Equal("gregory", id.UnicodeExtensionValue("ca"));
        Assert.Equal(string.Empty, id.UnicodeExtensionValue("kn"));
        Assert.Null(id.UnicodeExtensionValue("nu"));
        Assert.Equal("en-Latn-US-fonipa-x-priv", id.WithoutUnicodeExtension().ToString());
    }

    [Fact]
    public void The_grammar_checks_read_ASCII_alone()
    {
        Assert.True(LocaleId.IsUnicodeLanguageSubtag("EN"));
        Assert.False(LocaleId.IsUnicodeLanguageSubtag("Ko"));
        Assert.True(LocaleId.IsUnicodeScriptSubtag("Latn"));
        Assert.True(LocaleId.IsUnicodeRegionSubtag("419"));
        Assert.False(LocaleId.IsUnicodeRegionSubtag("١٢٣"));
        Assert.True(LocaleId.IsUnicodeVariantSubtag("1996"));
        Assert.True(LocaleId.IsUnicodeType("islamic-civil"));
        Assert.False(LocaleId.IsUnicodeType("Khmr"));
        Assert.False(LocaleId.IsUnicodeType("gregorİ"));
        Assert.Equal("abcK", LocaleId.AsciiLowercase("ABCK"));
    }

    [Fact]
    public void A_locale_list_is_canonicalised_once_each_in_order()
    {
        Assert.Equal(IntlStatus.Completed, LocaleNegotiation.CanonicalizeLocaleList(["en-us", "EN-US", "iw", "he", "de"], Data, out var locales, out var failedAt));
        Assert.Equal(["en-US", "he", "de"], locales);
        Assert.Equal(-1, failedAt);

        Assert.Equal(IntlStatus.InvalidLanguageTag, LocaleNegotiation.CanonicalizeLocaleList(["en", "en\n", "de"], Data, out locales, out failedAt));
        Assert.Empty(locales);
        Assert.Equal(1, failedAt);
    }

    [Fact]
    public void The_lookup_matcher_truncates_a_subtag_and_its_singleton_at_a_time()
    {
        Assert.Equal("en-US", LocaleNegotiation.LookupMatchingLocaleByPrefix(VmLocales, ["en-US-x-foo"])!.Locale);
        Assert.Equal("de-DE", LocaleNegotiation.LookupMatchingLocaleByPrefix(VmLocales, ["de-DE-1996"])!.Locale);
        Assert.Equal("en", LocaleNegotiation.LookupMatchingLocaleByBestFit(VmLocales, ["fr", "en-GB"])!.Locale);
        Assert.Null(LocaleNegotiation.LookupMatchingLocaleByPrefix(VmLocales, ["fr", "ja"]));

        var match = LocaleNegotiation.LookupMatchingLocaleByPrefix(VmLocales, ["en-US-u-ca-gregory-nu-arab"])!;
        Assert.Equal("en-US", match.Locale);
        Assert.Equal([new LocaleKeyword("ca", "gregory"), new LocaleKeyword("nu", "arab")], match.ExtensionKeywords);
    }

    [Fact]
    public void FilterLocales_keeps_the_requested_locales_as_requested()
    {
        Assert.Equal(
            ["de-CH", "en-US-u-ca-gregory", "de-DE-x-foo", "en-GB", "de-1996"],
            LocaleNegotiation.FilterLocales(VmLocales, ["ja", "zz", "sw-KE", "de-CH", "en-US-u-ca-gregory", "de-DE-x-foo", "sr", "en-GB", "de-1996"]));
    }

    [Fact]
    public void ResolveLocale_keeps_only_the_relevant_keywords_the_data_supports_rows_L29_to_L31()
    {
        var nu = new RelevantKey("nu", ["latn", "arab", "thai"]);

        Assert.Equal("en-u-nu-arab", LocaleNegotiation.ResolveLocale(VmLocales, ["en-u-attr-ca-japanese-nu-arab-co-phonebk-kn-kf-upper-hc-h23-xx-yyy"], "en-US", [nu], Data).Locale);
        Assert.Equal("en", LocaleNegotiation.ResolveLocale(VmLocales, ["en-u-nu-arab"], "en-US", [], Data).Locale);
        Assert.Equal("en-US", LocaleNegotiation.ResolveLocale(VmLocales, ["xx"], "en-US", [nu], Data).Locale);
        Assert.Equal("en-US", LocaleNegotiation.ResolveLocale(VmLocales, [], "en-US", [], Data).DataLocale);

        // The option supersedes the keyword, and drops it from the locale; an option the data lacks does not.
        var resolved = LocaleNegotiation.ResolveLocale(VmLocales, ["en-u-nu-arab"], "en-US", [nu with { Option = "THAI" }], Data);
        Assert.Equal("en", resolved.Locale);
        Assert.Equal("thai", resolved.KeyValues["nu"]);
        resolved = LocaleNegotiation.ResolveLocale(VmLocales, ["en-u-nu-arab"], "en-US", [nu with { Option = "xxxx" }], Data);
        Assert.Equal("en-u-nu-arab", resolved.Locale);
        Assert.Equal("arab", resolved.KeyValues["nu"]);

        // A null option drops the keyword where the data supports null (DateTimeFormat's hc with hour12).
        var hc = new RelevantKey("hc", [null, "h11", "h12", "h23", "h24"]);
        resolved = LocaleNegotiation.ResolveLocale(VmLocales, ["en-u-hc-h23"], "en-US", [hc with { OptionIsNull = true }], Data);
        Assert.Equal("en", resolved.Locale);
        Assert.Null(resolved.KeyValues["hc"]);

        // A keyword with no value is true where the data supports it.
        var kn = new RelevantKey("kn", ["false", "true"]);
        resolved = LocaleNegotiation.ResolveLocale(VmLocales, ["de-u-kn"], "en-US", [kn], Data);
        Assert.Equal("de-u-kn", resolved.Locale);
        Assert.Equal("true", resolved.KeyValues["kn"]);
    }

    [Fact]
    public void The_Locale_constructor_reads_its_options_as_ECMA_402_has_them()
    {
        var locale = Locale("en", new LocaleOptions
        {
            Calendar = "islamicc", Collation = "emoji", HourCycle = "h11", CaseFirst = "upper", Numeric = true, NumberingSystem = "arab", FirstDayOfWeek = "0",
        });
        Assert.Equal("en-u-ca-islamic-civil-co-emoji-fw-sun-hc-h11-kf-upper-kn-nu-arab", locale.Locale);
        Assert.Equal("islamic-civil", locale.Calendar);
        Assert.Equal("sun", locale.FirstDayOfWeek);
        Assert.True(locale.Numeric);

        locale = Locale("en-US", new LocaleOptions { Language = "fr", Script = "Cyrl", Region = "BE", Variants = "Fonipa-1996" });
        Assert.Equal("fr-Cyrl-BE-1996-fonipa", locale.Locale);

        Assert.False(Locale("de-u-kn-false").Numeric);
        Assert.True(Locale("de-u-kn-false", new LocaleOptions { Numeric = true }).Numeric);
        Assert.Equal("de-u-kn", Locale("de-u-kn-false", new LocaleOptions { Numeric = true }).Locale);
        Assert.Equal("ja-US", Locale("ja-JP", new LocaleOptions { Region = "US" }).Locale);
        Assert.Equal("jbo", Locale("art-lojban").Locale);
    }

    [Theory]
    [InlineData(IntlStatus.InvalidVariants, "variants", "Kaaaa")]
    [InlineData(IntlStatus.InvalidVariants, "variants", "")]
    [InlineData(IntlStatus.InvalidVariants, "variants", "fonipa-FONIPA")]
    [InlineData(IntlStatus.InvalidType, "numberingSystem", "Khmr")]
    [InlineData(IntlStatus.InvalidType, "collation", "phoKebk")]
    [InlineData(IntlStatus.InvalidType, "firstDayOfWeek", "moK")]
    [InlineData(IntlStatus.InvalidType, "calendar", "gregorİ")]
    [InlineData(IntlStatus.InvalidLanguageSubtag, "language", "Ko")]
    [InlineData(IntlStatus.InvalidScriptSubtag, "script", "Lat")]
    [InlineData(IntlStatus.InvalidRegionSubtag, "region", "UK")]
    public void An_option_outside_ASCII_or_its_grammar_is_refused_rows_L13_and_L14(IntlStatus status, string option, string value)
    {
        var options = option switch
        {
            "variants" => new LocaleOptions { Variants = value },
            "numberingSystem" => new LocaleOptions { NumberingSystem = value },
            "collation" => new LocaleOptions { Collation = value },
            "firstDayOfWeek" => new LocaleOptions { FirstDayOfWeek = value },
            "calendar" => new LocaleOptions { Calendar = value },
            "language" => new LocaleOptions { Language = value },
            "script" => new LocaleOptions { Script = value },
            _ => new LocaleOptions { Region = value },
        };

        Assert.Equal(status, LocaleRecord.TryCreate("en", options, Data, out var record));
        Assert.Null(record);
    }

    [Fact]
    public void The_Locale_s_information_follows_CLDR_and_the_pinned_text()
    {
        var keyed = Locale("en-u-ca-japanese-co-emoji-hc-h23-nu-arab-fw-wed");
        Assert.Equal(["japanese"], LocaleInfo.Calendars(keyed));
        Assert.Equal(["emoji"], LocaleInfo.Collations(keyed, new VmCollator(), "en-US"));
        Assert.Equal(["h23"], LocaleInfo.HourCycles(keyed, Data));
        Assert.Equal(["arab"], LocaleInfo.NumberingSystems(keyed, VmLocales, Data));
        Assert.Equal(3, LocaleInfo.WeekInfo(keyed, Data).FirstDay);

        Assert.Equal(["gregory"], LocaleInfo.Calendars(Locale("th-TH")));
        Assert.Equal(["h12", "h23"], LocaleInfo.HourCycles(Locale("en"), Data));
        Assert.Equal(["h23", "h11", "h12"], LocaleInfo.HourCycles(Locale("ja-JP"), Data));
        Assert.Equal(["h23", "h12"], LocaleInfo.HourCycles(Locale("en-US-u-rg-gbzzzz"), Data));
        Assert.Equal(["h12", "h23"], LocaleInfo.HourCycles(Locale("en-u-sd-usca"), Data));
        Assert.Equal(["h23", "h12"], LocaleInfo.HourCycles(Locale("en-u-sd-gbsct"), Data));

        Assert.Equal("rtl", LocaleInfo.TextDirection(Locale("und-Avst"), Data));
        Assert.Equal("rtl", LocaleInfo.TextDirection(Locale("pa-PK"), Data));
        Assert.Equal("ltr", LocaleInfo.TextDirection(Locale("ku"), Data));
        Assert.Null(LocaleInfo.TextDirection(Locale("xyz"), Data));
        Assert.Null(LocaleInfo.TextDirection(Locale("und-Zzzz"), Data));

        Assert.Equal(new LocaleWeekInfo(6, [5, 6]), LocaleInfo.WeekInfo(Locale("ar-EG"), Data), WeekComparer.Instance);
        Assert.Equal(new LocaleWeekInfo(7, [7]), LocaleInfo.WeekInfo(Locale("en-IN"), Data), WeekComparer.Instance);
        Assert.Equal(new LocaleWeekInfo(7, [6, 7]), LocaleInfo.WeekInfo(Locale("en"), Data), WeekComparer.Instance);
        Assert.Equal(new LocaleWeekInfo(1, [6, 7]), LocaleInfo.WeekInfo(Locale("en-u-rg-gbzzzz"), Data), WeekComparer.Instance);

        Assert.Equal(new LocaleRegionPreference("US", "GB"), LocaleInfo.RegionPreference("en-u-rg-gbzzzz", Data));
        Assert.Equal(new LocaleRegionPreference("001", null), LocaleInfo.RegionPreference("xyz", Data));
    }

    [Fact]
    public void Collations_of_a_language_the_Collator_lacks_are_DefaultLocale_s_row_L18()
    {
        var collator = new VmCollator();
        Assert.Equal([], LocaleInfo.Collations(Locale("en"), collator, "en-US"));
        Assert.Equal(["phonebk"], LocaleInfo.Collations(Locale("de-DE"), collator, "en-US"));

        // Row L18, a correction of Broiler.VM (decision D32): Broiler.VM answered emoji and eor for each of these.
        Assert.Equal([], LocaleInfo.Collations(Locale("fr-FR"), collator, "en-US"));
        Assert.Equal([], LocaleInfo.Collations(Locale("xyz"), collator, "en-US"));
        Assert.Equal(["phonebk"], LocaleInfo.Collations(Locale("fr-FR"), collator, "de"));

        // emoji and eor for und alone, as CollationsOfLocale has it.
        Assert.Equal(["emoji", "eor"], LocaleInfo.Collations(Locale("und"), collator, "en-US"));
    }

    [Fact]
    public void A_numbering_system_is_the_matched_locale_s_default_from_the_data_row_L22()
    {
        Assert.Equal(["arab"], LocaleInfo.NumberingSystems(Locale("ar-EG"), Data.Locales, Data));
        Assert.Equal(["latn"], LocaleInfo.NumberingSystems(Locale("ar-EG"), VmLocales, Data));
        Assert.Equal(["latn"], LocaleInfo.NumberingSystems(Locale("de"), VmLocales, Data));
        Assert.Equal(["latn"], LocaleInfo.NumberingSystems(Locale("xyz"), Data.Locales, Data));
    }

    [Fact]
    public void Time_zones_are_the_region_s_from_the_shared_database_row_L23()
    {
        var zones = HomeTimeZones.Current;
        Assert.Equal(["Pacific/Auckland", "Pacific/Chatham"], LocaleInfo.TimeZones(Locale("en-NZ"), zones));
        Assert.Null(LocaleInfo.TimeZones(Locale("en"), zones));
        Assert.Empty(LocaleInfo.TimeZones(Locale("xx-ZZ"), zones)!);
    }

    [Fact]
    public void SupportedValuesOf_lists_each_key_sorted_rows_L39_to_L41()
    {
        var zones = HomeTimeZones.Current;
        Assert.Equal(IntlStatus.Completed, SupportedValues.Of("calendar", Data, zones, new VmCollator(), out var calendars));
        Assert.Equal(SupportedValues.AvailableCalendars, calendars);
        Assert.DoesNotContain("islamic", calendars);
        Assert.Equal(IntlStatus.Completed, SupportedValues.Of("collation", Data, zones, new VmCollator(), out var collations));
        Assert.Equal(["phonebk"], collations);
        Assert.Equal(IntlStatus.Completed, SupportedValues.Of("currency", Data, zones, new VmCollator(), out var currencies));
        Assert.Contains("EUR", currencies);
        Assert.Equal(currencies.Order(StringComparer.Ordinal), currencies);
        Assert.Equal(IntlStatus.Completed, SupportedValues.Of("numberingSystem", Data, zones, new VmCollator(), out var systems));
        Assert.Contains("arab", systems);
        Assert.Equal(IntlStatus.Completed, SupportedValues.Of("timeZone", Data, zones, new VmCollator(), out var timeZones));
        Assert.Equal(zones.PrimaryIdentifiers.Order(StringComparer.Ordinal), timeZones);
        Assert.Equal(IntlStatus.Completed, SupportedValues.Of("unit", Data, zones, new VmCollator(), out var units));
        Assert.Equal(SupportedValues.SanctionedUnits, units);
        Assert.Equal(IntlStatus.InvalidKey, SupportedValues.Of("calendars", Data, zones, new VmCollator(), out var none));
        Assert.Empty(none);
    }

    [Fact]
    public void The_caller_s_meter_is_charged_before_the_work_decision_D18()
    {
        ulong charged = 0;
        WorkCharge charge = units => charged += units;

        // The tag's length, then the alias-table keys the one round of alias replacement looks up: en, en-US and und-US.
        _ = LocaleId.TryCanonicalize("en-US-u-ca-gregory", Data, out _, charge);
        Assert.Equal(18UL + 3UL, charged);

        // en: 2 and the key en; de-DE: 5 and the keys de, de-DE and und-DE.
        charged = 0;
        _ = LocaleNegotiation.CanonicalizeLocaleList(["en", "de-DE"], Data, out _, out _, charge);
        Assert.Equal(2UL + 1UL + 5UL + 3UL, charged);

        charged = 0;
        _ = LocaleNegotiation.FilterLocales(VmLocales, ["fr", "de", "en"], charge);
        Assert.Equal(3UL, charged);

        // The quadratic work is charged: a tag of 50 variants looks up, for each of its stems (en and und), each variant and
        // each ordered pair of them, which its length of 252 code units does not bound.
        charged = 0;
        _ = LocaleId.TryCanonicalize(ManyVariants("en", 50), Data, out var canonical, charge);
        Assert.Equal(252UL + (2UL * (50UL + (50UL * 49UL))) + 1UL, charged);
        Assert.Equal(ManyVariants("en", 50), canonical);

        // The meter may throw, and nothing is answered past it.
        Assert.Throws<InvalidOperationException>(() => LocaleId.TryParse("en", out _, _ => throw new InvalidOperationException()));
    }

    [Fact]
    public void Every_operation_whose_work_grows_with_its_input_charges_the_caller_s_meter_decision_D18()
    {
        ulong charged = 0;
        WorkCharge charge = units => charged += units;
        var variants = ManyVariants("en", 50)[3..];

        // Intl.Locale with a variants option: the tag and its key (3), the canonical tag and the option (2 and 249), the
        // updated tag (252), and the keys its canonicalisation looks up (as above, 5,001).
        Assert.Equal(IntlStatus.Completed, LocaleRecord.TryCreate("en", new LocaleOptions { Variants = variants }, Data, out var record, charge));
        Assert.Equal(ManyVariants("en", 50), record!.Locale);
        Assert.Equal(3UL + 2UL + 249UL + 252UL + 5001UL, charged);

        // Each type option is charged as it is checked, and each value it sets as it is canonicalised: the tag and its key
        // (3), the canonical tag (2), the check (7), the updated tag (2), the value (7) and the record's key (1).
        charged = 0;
        Assert.Equal(IntlStatus.Completed, LocaleRecord.TryCreate("en", new LocaleOptions { Calendar = "gregory" }, Data, out _, charge));
        Assert.Equal(3UL + 2UL + 7UL + 2UL + 7UL + 1UL, charged);

        charged = 0;
        Assert.Equal(IntlStatus.Completed, LocaleRecord.CheckVariants(variants, charge));
        Assert.Equal((ulong)variants.Length, charged);

        charged = 0;
        Assert.Equal(IntlStatus.Completed, LocaleRecord.UpdateLanguageId("en-US", "fr", null, null, "fonipa", out var updated, charge));
        Assert.Equal("fr-US-fonipa", updated);
        Assert.Equal(5UL + 2UL + 6UL, charged);

        charged = 0;
        Assert.Equal(IntlStatus.InvalidType, LocaleRecord.CheckType("gregorİ", charge));
        Assert.Equal(IntlStatus.Completed, LocaleRecord.CheckFirstDayOfWeek("0", charge));
        Assert.Equal("islamic-civil", LocaleRecord.CanonicalizeUValue(Data, "ca", "islamicc", charge));
        Assert.True(LocaleId.IsUnicodeType("islamic-civil", charge));
        Assert.Equal("abck", LocaleId.AsciiLowercase("ABCk", charge));
        Assert.Equal(7UL + 3UL + 8UL + 13UL + 4UL, charged);

        // Maximize, Minimize and WithoutUnicodeExtension charge the identifier's length.
        Assert.Equal(IntlStatus.Completed, LocaleId.TryParse("zh-TW-u-ca-roc", out var id));
        charged = 0;
        _ = id!.Maximize(Data, out _, charge);
        _ = id.Minimize(Data, out _, charge);
        _ = id.WithoutUnicodeExtension(charge);
        Assert.Equal(3UL * 14UL, charged);

        // Locale info charges the Locale's identifier, and the week and hour cycles the keys the likely tag's
        // canonicalisation looks up (en-Latn-US: seven stems with a key).
        var locale = Locale("en-u-rg-gbzzzz");
        charged = 0;
        _ = LocaleInfo.WeekInfo(locale, Data, charge);
        Assert.Equal(14UL + 7UL, charged);
        charged = 0;
        _ = LocaleInfo.HourCycles(locale, Data, charge);
        Assert.Equal(14UL + 7UL, charged);
        charged = 0;
        _ = LocaleInfo.RegionPreference("en-NZ", Data, charge);
        _ = LocaleInfo.TimeZones(locale, HomeTimeZones.Current, charge);
        _ = LocaleInfo.TextDirection(locale, Data, charge);
        _ = LocaleInfo.Collations(locale, new VmCollator(), "en-US", charge);
        _ = LocaleInfo.NumberingSystems(locale, VmLocales, Data, charge);
        Assert.Equal(5UL + (4UL * 14UL), charged);
    }

    [Theory]
    [InlineData("en")]
    [InlineData("und")]
    [InlineData("en-US")]
    [InlineData("und-Latn-US")]
    [InlineData("ja-Latn-hepburn-heploc")]
    [InlineData("en-Latn-US-fonipa-1996-1901")]
    [InlineData("und-1901-1996-fonipa")]
    public void The_charge_for_alias_replacement_is_the_number_of_keys_it_looks_up(string tag)
    {
        var parsed = LocaleTag.Parse(tag)!;
        Assert.Equal((ulong)parsed.Candidates().Count(), parsed.CandidateCount());
    }

    /// <summary>A tag of <paramref name="count"/> distinct variants, each four characters beginning with a digit, in canonical order.</summary>
    private static string ManyVariants(string language, int count) =>
        language + string.Concat(Enumerable.Range(0, count).Select(static i => "-0a" + (char)('a' + (i / 26)) + (char)('a' + (i % 26))));

    private sealed class WeekComparer : IEqualityComparer<LocaleWeekInfo>
    {
        internal static readonly WeekComparer Instance = new();

        public bool Equals(LocaleWeekInfo? x, LocaleWeekInfo? y) => x!.FirstDay == y!.FirstDay && x.Weekend.SequenceEqual(y.Weekend);

        public int GetHashCode(LocaleWeekInfo obj) => obj.FirstDay;
    }
}
