using System;
using System.Globalization;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Sub-area (a) of the Intl area on the shared library (milestone SJL-9.5, task 8.3; Broiler.VM's JSD-0082): tag validation,
// canonicalisation and the alias tables, negotiation, Intl.Locale's record, parts, likely subtags and information, and the
// default-locale and available-locale providers (decision D7). Each case names the row of SJL-9.1's correspondence
// (Broiler.VM's docs/evidence/sjl-9-001/correspondence.md) it holds; a correction failed before the cut-over and passes after.
public class SharedIntlLocaleTests
{
    private static string Eval(string source) => SharedDateAlgorithmTests.Eval(source);

    private static string Error(string source) => Eval($"var e = 'none'; try {{ {source}; }} catch (x) {{ e = x.name; }} e");

    private static T UnderCulture<T>(string culture, Func<T> body)
    {
        var saved = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            return body();
        }
        finally
        {
            CultureInfo.CurrentCulture = saved;
        }
    }

    // L2 and L45 (corrections): only ASCII letters, digits and hyphens make a tag; a KELVIN SIGN, a dotless i, a final line
    // feed or Arabic-Indic digits are a RangeError, where culture casing folded some into ASCII and the expression admitted the rest.
    [Theory]
    [InlineData("Ko")]
    [InlineData("en-KE")]
    [InlineData("ıt")]
    [InlineData("en\n")]
    [InlineData("de-١٢٣")]
    public void A_tag_is_ASCII_alone(string tag)
    {
        var literal = tag.Replace("\n", "\\n");
        Assert.Equal("RangeError", Error($"Intl.getCanonicalLocales('{literal}')"));
        Assert.Equal("RangeError", Error($"new Intl.Locale('{literal}')"));
        Assert.Equal("RangeError", Error($"new Intl.NumberFormat('{literal}')"));
    }

    // L5 to L8 (data): CLDR 48.2.0's aliases, generated, where hand tables listed what test262 exercised; L10 (a correction):
    // a -t- tlang whose alias adds a subtag, where the canonicalisation wrote past its array.
    [Theory]
    [InlineData("tl", "fil")]
    [InlineData("aju", "jrb")]
    [InlineData("und-Qaai", "und-Zinh")]
    [InlineData("en-840", "en-US")]
    [InlineData("en-AN", "en-CW")]
    [InlineData("en-u-tz-usnavajo", "en-u-tz-usden")]
    [InlineData("en-t-sh", "en-t-sr-latn")]
    [InlineData("art-lojban", "jbo")]
    public void Canonicalization_reads_CLDR_s_aliases(string tag, string canonical)
        => Assert.Equal(canonical, Eval($"Intl.getCanonicalLocales('{tag}')[0]"));

    // L12 (a correction): Add Likely Subtags removes Zzzz and ZZ before the lookup.
    [Theory]
    [InlineData("new Intl.Locale('und-Zzzz').maximize().toString()", "en-Latn-US")]
    [InlineData("new Intl.Locale('und-ZZ').maximize().toString()", "en-Latn-US")]
    [InlineData("new Intl.Locale('und-Zzzz-ZZ').minimize().toString()", "en")]
    [InlineData("new Intl.Locale('zh-TW').maximize().toString()", "zh-Hant-TW")]
    public void Likely_subtags_are_CLDR_s(string expression, string expected)
        => Assert.Equal(expected, Eval(expression));

    // L14 (a correction): an option's value is checked over ASCII alone.
    [Theory]
    [InlineData("new Intl.Locale('en', {variants: 'Kaaaa'})")]
    [InlineData("new Intl.Locale('en', {numberingSystem: 'Khmr'})")]
    [InlineData("new Intl.Locale('en', {collation: 'phoKebk'})")]
    [InlineData("new Intl.Locale('en', {firstDayOfWeek: 'moK'})")]
    [InlineData("new Intl.NumberFormat('en', {numberingSystem: 'Khmr'})")]
    public void An_option_is_checked_over_ASCII_alone(string expression)
        => Assert.Equal("RangeError", Error(expression));

    // L15 (a correction): an information method on a Locale that carries the keyword answers the keyword alone.
    [Theory]
    [InlineData("new Intl.Locale('de-u-co-phonebk').getCollations().join()", "phonebk")]
    [InlineData("new Intl.Locale('en-u-hc-h23').getHourCycles().join()", "h23")]
    [InlineData("new Intl.Locale('en-u-ca-japanese').getCalendars().join()", "japanese")]
    [InlineData("new Intl.Locale('en-u-nu-arab').getNumberingSystems().join()", "arab")]
    public void An_information_method_answers_the_keyword_alone(string expression, string expected)
        => Assert.Equal(expected, Eval(expression));

    // L17 (a correction; decision D33 (c)): getCollations with no keyword answers the collations this engine's Intl.Collator
    // resolves for the locale, where it answered "default".
    [Theory]
    [InlineData("de", "emoji,eor,phonebk")]
    [InlineData("en", "emoji,eor")]
    [InlineData("zh", "emoji,eor,phonetic,pinyin,stroke,trad,unihan,zhuyin")]
    public void GetCollations_answers_this_Collator_s_collations(string tag, string expected)
        => Assert.Equal(expected, Eval($"new Intl.Locale('{tag}').getCollations().join()"));

    // L19 (data), L20 and L21 (corrections): the hour cycles in use from CLDR's time data, the preferred first, read for the
    // preferred region and its rg override; the week from CLDR's week data, read the same way.
    [Theory]
    [InlineData("new Intl.Locale('ja-JP').getHourCycles()[0]", "h23")]
    [InlineData("new Intl.Locale('ko-KR').getHourCycles()[0]", "h12")]
    [InlineData("new Intl.Locale('en').getHourCycles().length > 1", "true")]
    [InlineData("new Intl.Locale('en-US-u-rg-gbzzzz').getHourCycles()[0]", "h23")]
    [InlineData("JSON.stringify(new Intl.Locale('ar-EG').getWeekInfo())", "{\"firstDay\":6,\"weekend\":[5,6]}")]
    [InlineData("new Intl.Locale('en-US-u-rg-gbzzzz').getWeekInfo().firstDay", "1")]
    [InlineData("new Intl.Locale('en-u-fw-wed').getWeekInfo().firstDay", "3")]
    public void Hour_cycles_and_weeks_are_CLDR_s(string expression, string expected)
        => Assert.Equal(expected, Eval(expression));

    // L24 (data) and L25 (a correction): the text direction is the script's, or its likely script's, in CLDR's script
    // metadata, and undefined when none is known.
    [Theory]
    [InlineData("new Intl.Locale('ar').getTextInfo().direction", "rtl")]
    [InlineData("new Intl.Locale('und-Thaa').getTextInfo().direction", "rtl")]
    [InlineData("new Intl.Locale('en').getTextInfo().direction", "ltr")]
    [InlineData("String(new Intl.Locale('xyz').getTextInfo().direction)", "undefined")]
    [InlineData("String(new Intl.Locale('und-Zyyy').getTextInfo().direction)", "undefined")]
    public void The_text_direction_is_CLDR_s(string expression, string expected)
        => Assert.Equal(expected, Eval(expression));

    // L29 (a correction): the resolved locale is the available locale matched, not the request.
    [Theory]
    [InlineData("new Intl.NumberFormat('en-US-x-foo').resolvedOptions().locale", "en-US")]
    [InlineData("new Intl.PluralRules('de-CH-1996').resolvedOptions().locale", "de-CH")]
    public void The_resolved_locale_is_an_available_locale(string expression, string expected)
        => Assert.Equal(expected, Eval(expression));

    // L30 (a correction): only the service's relevant extension keys are kept in the resolved locale.
    [Theory]
    [InlineData("new Intl.ListFormat('en-u-ca-japanese-nu-arab').resolvedOptions().locale", "en")]
    [InlineData("new Intl.PluralRules('en-u-nu-arab').resolvedOptions().locale", "en")]
    [InlineData("new Intl.RelativeTimeFormat('en-u-ca-japanese-nu-arab').resolvedOptions().locale", "en-u-nu-arab")]
    [InlineData("new Intl.NumberFormat('en-u-ca-japanese-nu-arab').resolvedOptions().locale", "en-u-nu-arab")]
    public void Only_the_relevant_keys_are_kept(string expression, string expected)
        => Assert.Equal(expected, Eval(expression));

    // L36 and L37 (L37 a correction): every service has supportedLocalesOf, Intl.DisplayNames included, which keeps each
    // requested tag a prefix of which is available, as requested.
    [Fact]
    public void Every_service_has_supportedLocalesOf()
    {
        Assert.Equal("function", Eval("typeof Intl.DisplayNames.supportedLocalesOf"));
        Assert.Equal("1", Eval("Intl.DisplayNames.supportedLocalesOf.length"));
        Assert.Equal("de-DE-x-foo,en-US-u-ca-gregory", Eval("Intl.DisplayNames.supportedLocalesOf(['zz', 'de-DE-x-foo', 'en-US-u-ca-gregory']).join()"));
        Assert.Equal("RangeError", Error("Intl.DisplayNames.supportedLocalesOf('en', {localeMatcher: 'x'})"));
    }

    // L33 (engine policy, decision D7): the default locale is the host's current culture, en-US for the invariant one.
    [Fact]
    public void The_default_locale_is_the_host_s_culture()
    {
        Assert.Equal("en-US", Eval("new Intl.NumberFormat().resolvedOptions().locale"));
        Assert.Equal("de-DE", UnderCulture("de-DE", () => Eval("new Intl.NumberFormat().resolvedOptions().locale")));
        Assert.Equal("de-DE", UnderCulture("de-DE", () => Eval("new Intl.Collator([]).resolvedOptions().locale")));
    }

    // L35 (decision D7, one default): TransformCase with no locale reads the same default-locale provider every service reads,
    // so a tr host culture lowers I to dotless i, as that culture's Intl services resolve tr-TR.
    [Fact]
    public void Casing_with_no_locale_reads_the_default_locale()
    {
        Assert.Equal("i,I", Eval("['I'.toLocaleLowerCase(), 'i'.toLocaleUpperCase()].join()"));
        Assert.Equal("ı,İ,i,I", UnderCulture("tr-TR", () => Eval("['I'.toLocaleLowerCase(), 'i'.toLocaleUpperCase(), 'I'.toLowerCase(), 'i'.toUpperCase()].join()")));
    }

    // L44 (a correction; decision D7, the default is available): under invariant globalization the host lists no named
    // culture, and the available locales are the default, en-US, and en, where they were none; run in a child process.
    [Fact]
    public void Under_invariant_globalization_the_default_locale_is_available()
        => InvariantGlobalizationChild.Run(typeof(SharedIntlLocaleTests).FullName + "." + nameof(InvariantChild));

    [Fact(Explicit = true)]
    public void InvariantChild()
    {
        Assert.True(InvariantGlobalizationChild.IsChild);
        Assert.DoesNotContain(CultureInfo.GetCultures(CultureTypes.AllCultures), static c => c.Name.Length != 0);
        foreach (var service in (string[])["Collator", "NumberFormat", "DateTimeFormat", "PluralRules", "RelativeTimeFormat", "ListFormat", "DisplayNames", "Segmenter", "DurationFormat"])
        {
            Assert.Equal("en-US,en", Eval($"Intl.{service}.supportedLocalesOf(['en-US', 'en', 'de', 'fr-FR']).join()"));
        }

        Assert.Equal("en-US", Eval("new Intl.Collator().resolvedOptions().locale"));
        Assert.Equal("en-US", Eval("new Intl.NumberFormat('de').resolvedOptions().locale"));
        Assert.Equal("en", Eval("new Intl.PluralRules('en').resolvedOptions().locale"));
    }
}
