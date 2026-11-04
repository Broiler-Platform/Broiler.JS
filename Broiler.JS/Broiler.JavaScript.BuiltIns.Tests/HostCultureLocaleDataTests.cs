using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Broiler.ECMAScript.Intl;

namespace Broiler.JavaScript.BuiltIns.Tests;

// The host-culture provider (milestone SJL-9.5, task 8.2; Broiler.VM's JSD-0082, decisions SJL-D-22 (c) and D6; the
// home's design, section 5): an ILocaleData over CultureInfo for the answers .NET exposes exactly, asked by the composite
// only for a locale the shared data lacks; every other question is "no data"; a locale the host lacks is unavailable;
// nothing throws. In both modes: normal globalization here, and invariant globalization in a child process
// (InvariantGlobalizationChild), where the host lists no named culture.
public class HostCultureLocaleDataTests
{
    private static Assembly BuiltIns => typeof(RegExp.JSRegExp).Assembly;

    private static ILocaleData Host =>
        (ILocaleData)BuiltIns.GetType("Broiler.JavaScript.BuiltIns.Intl.HostCultureLocaleData", throwOnError: true)!
            .GetProperty("Host", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;

    private static ILocaleData Data(string name) =>
        (ILocaleData)BuiltIns.GetType("Broiler.JavaScript.BuiltIns.Intl.JSIntlData", throwOnError: true)!
            .GetProperty(name, BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;

    private static ILocaleData Shared => Data("Shared");

    private static ILocaleData Composite => Data("LocaleData");

    // A culture the host has and the shared data neither lists nor holds; Greek, whose numbers are written in ASCII digits
    // and whose default calendar is the Gregorian one.
    private const string HostOnly = "el";

    private static bool HasCulture(string name) =>
        CultureInfo.GetCultures(CultureTypes.AllCultures).Any(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));

    [Fact]
    public void The_host_lists_its_named_cultures_in_normal_globalization()
    {
        Assert.False(InvariantGlobalizationChild.IsChild);
        Assert.True(HasCulture(HostOnly), "the host has no " + HostOnly + " culture");
        Assert.Contains(HostOnly, Host.Locales, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain(HostOnly, Shared.Locales);
        Assert.False(Shared.HoldsLocale(IntlTable.NumberLocales, HostOnly));
    }

    // Number symbols, as the latn keys, from the culture's NumberFormatInfo.
    [Fact]
    public void Number_symbols_are_the_culture_s()
    {
        var format = CultureInfo.GetCultureInfo(HostOnly).NumberFormat;
        Assert.True(Host.HoldsLocale(IntlTable.NumberLocales, HostOnly));
        Assert.Equal(format.NumberDecimalSeparator, Host.NumberValue(HostOnly, "symbols.decimal"));
        Assert.Equal(format.NumberGroupSeparator, Host.NumberValue(HostOnly, "symbols.group"));
        Assert.Equal(format.NegativeSign, Host.NumberValue(HostOnly, "symbols.minusSign"));
        Assert.Equal(format.PositiveSign, Host.NumberValue(HostOnly, "symbols.plusSign"));
        Assert.Equal(format.PercentSymbol, Host.NumberValue(HostOnly, "symbols.percentSign"));
        Assert.Equal(format.PerMilleSymbol, Host.NumberValue(HostOnly, "symbols.perMille"));
        Assert.Equal(format.PositiveInfinitySymbol, Host.NumberValue(HostOnly, "symbols.infinity"));
        Assert.Equal(format.NaNSymbol, Host.NumberValue(HostOnly, "symbols.nan"));
        // Not a symbol: a pattern, which .NET does not state as CLDR does, and another system's symbols.
        Assert.Null(Host.NumberValue(HostOnly, "decimal.standard"));
        Assert.Null(Host.NumberValue(HostOnly, "symbols@arab.decimal"));
        Assert.Null(Host.NumberValue(HostOnly, "defaultNumberingSystem"));
    }

    // Month, day and era names and AM and PM of the Gregorian calendar, from the culture's DateTimeFormatInfo.
    [Fact]
    public void Month_day_and_era_names_and_am_pm_are_the_culture_s()
    {
        var format = CultureInfo.GetCultureInfo(HostOnly).DateTimeFormat;
        Assert.IsType<GregorianCalendar>(format.Calendar);
        Assert.True(Host.HoldsLocale(IntlTable.DateLocales, HostOnly));
        Assert.Equal(format.MonthGenitiveNames[0], Host.DateValue(HostOnly, "gregory", "months.format.wide.1"));
        Assert.Equal(format.MonthNames[11], Host.DateValue(HostOnly, "gregory", "months.stand-alone.wide.12"));
        Assert.Equal(format.AbbreviatedMonthGenitiveNames[4], Host.DateValue(HostOnly, "gregory", "months.format.abbreviated.5"));
        Assert.Equal(format.AbbreviatedMonthNames[4], Host.DateValue(HostOnly, "gregory", "months.stand-alone.abbreviated.5"));
        Assert.Equal(format.DayNames[1], Host.DateValue(HostOnly, "gregory", "days.stand-alone.wide.mon"));
        Assert.Equal(format.AbbreviatedDayNames[0], Host.DateValue(HostOnly, "gregory", "days.stand-alone.abbreviated.sun"));
        Assert.Equal(format.ShortestDayNames[6], Host.DateValue(HostOnly, "gregory", "days.stand-alone.short.sat"));
        Assert.Equal(format.GetEraName(1), Host.DateValue(HostOnly, "gregory", "eras.eraAbbr.1"));
        Assert.Equal(format.GetAbbreviatedEraName(1), Host.DateValue(HostOnly, "gregory", "eras.eraNarrow.1"));
        Assert.Equal(format.AMDesignator, Host.DateValue(HostOnly, "gregory", "dayPeriods.format.abbreviated.am"));
        Assert.Equal(format.PMDesignator, Host.DateValue(HostOnly, "gregory", "dayPeriods.format.abbreviated.pm"));
        // .NET states no narrow month, no format-context day, no wide era name, no era before era 1, no pattern, and no other
        // calendar's names.
        Assert.Null(Host.DateValue(HostOnly, "gregory", "months.format.narrow.1"));
        Assert.Null(Host.DateValue(HostOnly, "gregory", "days.format.wide.mon"));
        Assert.Null(Host.DateValue(HostOnly, "gregory", "eras.eraNames.1"));
        Assert.Null(Host.DateValue(HostOnly, "gregory", "eras.eraAbbr.0"));
        Assert.Null(Host.DateValue(HostOnly, "gregory", "dateFormats.short"));
        Assert.Null(Host.DateValue(HostOnly, "japanese", "months.format.wide.1"));
        Assert.Null(Host.DateValues(HostOnly, "gregory"));
    }

    // A culture whose numbers are written in another system's digits states that system's symbols, which .NET does not
    // name, and a culture whose default calendar is not the Gregorian one states that calendar's names: no answer for either.
    [Theory]
    [InlineData("bn")]
    [InlineData("fa")]
    [InlineData("th-TH")]
    [InlineData("ar-SA")]
    public void A_culture_whose_digits_or_calendar_are_others_answers_neither(string name)
    {
        if (!HasCulture(name))
            return;

        var culture = CultureInfo.GetCultureInfo(name);
        if (culture.NumberFormat.NativeDigits[1] != "1")
            Assert.Null(Host.NumberValue(name, "symbols.decimal"));
        if (culture.DateTimeFormat.Calendar is not GregorianCalendar)
            Assert.Null(Host.DateValue(name, "gregory", "months.format.wide.1"));
    }

    // Every other question answers "no data": .NET has no plural, compact, unit, list, relative-time, display-name,
    // interval or day-period data, and the world questions are the shared data's. The first day of the week and native
    // digits have no question keyed as .NET gives them (decision D6, the design's notes).
    [Fact]
    public void Every_other_question_answers_no_data()
    {
        Assert.False(Host.HoldsLocale(IntlTable.Plurals, HostOnly));
        Assert.False(Host.HoldsLocale(IntlTable.Units, HostOnly));
        Assert.Null(Host.LikelySubtags(HostOnly));
        Assert.Null(Host.Alias(LocaleAliasKind.Language, "iw"));
        Assert.False(Host.IsExtensionKey('u', "ca"));
        Assert.False(Host.TryExtensionType('u', "ca", "gregory", out var preferred));
        Assert.Equal("gregory", preferred);
        Assert.Null(Host.HourCycles("GR"));
        Assert.Null(Host.Week("GR"));
        Assert.Null(Host.IsRightToLeft("Grek"));
        Assert.Null(Host.Currency(HostOnly, "EUR"));
        Assert.Empty(Host.CurrencyCodes);
        Assert.Null(Host.CurrencyDigits("JPY"));
        Assert.Empty(Host.NumberingSystems);
        Assert.Null(Host.NumberingSystemDigits("latn"));
        Assert.Null(Host.PluralRules(HostOnly, PluralRuleType.Cardinal));
        Assert.Null(Host.PluralRange(HostOnly, "one", "other"));
        Assert.Null(Host.UnitPattern(HostOnly, "long", "meter", "unitPattern-count-other"));
        Assert.Null(Host.ListPattern(HostOnly, "standard"));
        Assert.Null(Host.RelativeTime(HostOnly, "day", "1"));
        Assert.Null(Host.DisplayName(HostOnly, "language", "en"));
        Assert.Null(Host.DayPeriods(HostOnly));
        Assert.Null(Host.MetazonePeriods("Europe/Athens"));
        Assert.Null(Host.GoldenZone("Europe_Eastern", "GR"));
        Assert.Null(Host.PrimaryZone("GR"));
    }

    // A locale the host has no culture for is unavailable, and no question about it throws.
    [Theory]
    [InlineData("xx")]
    [InlineData("zz-ZZ")]
    [InlineData("")]
    [InlineData("-")]
    [InlineData("en_US")]
    [InlineData("und")]
    [InlineData("x-private")]
    [InlineData("en-u-ca-gregory")]
    [InlineData("Ko")]
    public void A_locale_the_host_lacks_is_unavailable_and_nothing_throws(string locale)
    {
        Assert.False(Host.HoldsLocale(IntlTable.NumberLocales, locale));
        Assert.False(Host.HoldsLocale(IntlTable.DateLocales, locale));
        Assert.Null(Host.NumberValue(locale, "symbols.decimal"));
        Assert.Null(Host.DateValue(locale, "gregory", "months.format.wide.1"));
    }

    // The composite asks the host only at a locale the shared data neither lists nor holds: a host-only culture reads the
    // host's symbol, and a default-content locale the shared data lists, fr-FR, reads CLDR's fr, never the host's.
    [Fact]
    public void The_composite_asks_the_host_only_for_a_locale_the_shared_data_lacks()
    {
        Assert.Equal(Host.NumberValue(HostOnly, "symbols.decimal"), Composite.NumberValue(HostOnly, "symbols.decimal"));
        Assert.Equal(Host.DateValue(HostOnly, "gregory", "months.format.wide.3"), Composite.DateValue(HostOnly, "gregory", "months.format.wide.3"));
        Assert.Contains("fr-FR", Shared.Locales);
        Assert.Equal(Shared.NumberValue("fr", "symbols.group"), Composite.NumberValue("fr-FR", "symbols.group"));
        // What the host does not answer, a host-only locale reads from root (list patterns are main-data tables, which carry
        // no el); plural rules cover every CLDR language in the shared data (decision D5), so el's are CLDR's own.
        Assert.Null(Shared.ListPattern(HostOnly, "standard"));
        Assert.Equal(Shared.ListPattern("und", "standard"), Composite.ListPattern(HostOnly, "standard"));
        Assert.Equal(Shared.PluralRules(HostOnly, PluralRuleType.Cardinal), Composite.PluralRules(HostOnly, PluralRuleType.Cardinal));
    }

    // Invariant globalization, in a child process: the host lists no named culture, so the provider reports every locale
    // unavailable and answers nothing, without throwing; the composite then answers as the shared data and root do.
    [Fact]
    public void Under_invariant_globalization_every_locale_is_unavailable_and_nothing_throws()
        => InvariantGlobalizationChild.Run(typeof(HostCultureLocaleDataTests).FullName + "." + nameof(InvariantChild));

    [Fact(Explicit = true)]
    public void InvariantChild()
    {
        Assert.True(InvariantGlobalizationChild.IsChild);
        Assert.DoesNotContain(CultureInfo.GetCultures(CultureTypes.AllCultures), static c => c.Name.Length != 0);
        Assert.Empty(Host.Locales);
        foreach (var locale in (string[])["en-US", "en", HostOnly, "de", "fr-FR", "xx", "", "und"])
        {
            Assert.False(Host.HoldsLocale(IntlTable.NumberLocales, locale));
            Assert.False(Host.HoldsLocale(IntlTable.DateLocales, locale));
            Assert.Null(Host.NumberValue(locale, "symbols.decimal"));
            Assert.Null(Host.DateValue(locale, "gregory", "months.format.wide.1"));
        }

        Assert.Equal(Shared.NumberValue("und", "symbols.decimal"), Composite.NumberValue(HostOnly, "symbols.decimal"));
        Assert.Equal(Shared.NumberValue("fr", "symbols.group"), Composite.NumberValue("fr-FR", "symbols.group"));
    }
}
