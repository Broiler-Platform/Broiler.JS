using System.Linq;
using Xunit;

namespace Broiler.ECMAScript.Intl.Tests;

/// <summary>
/// The decoder implementation over Broiler.JS's tables as the data home commits them
/// (<c>Broiler.ECMAScript/data/intl/generated/</c>): every table decodes, and a few answers are CLDR 48.2.0's.
/// </summary>
public sealed class HomeDataTests
{
    private static readonly TableLocaleData Home = new(HomeDataSet.Current);

    [Fact]
    public void Every_table_decodes()
    {
        Assert.Equal("48.2.0", Home.CldrVersion);
        Assert.Contains("en-US", Home.Locales);
        Assert.Contains("und", Home.Locales);
        Assert.NotEmpty(Home.CurrencyCodes);
        Assert.Contains("latn", Home.NumberingSystems);
        Assert.Equal("en-Latn-US", Home.LikelySubtags("en"));
        Assert.Equal("he", Home.Alias(LocaleAliasKind.Language, "iw"));
        Assert.NotNull(Home.HourCycles("001"));
        Assert.NotNull(Home.Week("001"));
        Assert.True(Home.IsRightToLeft("Arab"));
        Assert.NotNull(Home.DayPeriods("en"));
        Assert.NotNull(Home.ListPattern("en", "standard"));
        Assert.NotNull(Home.RelativeTime("en", "day", "-1"));
        Assert.NotNull(Home.DisplayName("en", "language", "de"));
    }

    [Fact]
    public void Answers_are_CLDR_48_2_0_s()
    {
        var composite = new CompositeLocaleData(Home);
        Assert.Equal(".", composite.NumberValue("en-US", "symbols.decimal"));
        Assert.Equal(",", composite.NumberValue("de-DE", "symbols.decimal"));
        Assert.Equal("\u066B", composite.NumberValue("ar", "symbols@arab.decimal"));
        Assert.Equal("January", composite.DateValue("en-US", "gregory", "months.format.wide.1"));
        Assert.Equal("Januar", composite.DateValue("de-DE", "gregory", "months.format.wide.1"));
        Assert.Equal(3, Home.CurrencyDigits("BHD"));
        Assert.Equal("one", composite.PluralRules("en-US", PluralRuleType.Cardinal)!.First().Category);
        Assert.Equal(10, composite.NumberingSystemDigits("arab")!.Count);
    }

    [Fact]
    public void The_date_layers_exist_for_a_main_data_locale()
    {
        foreach (var calendar in (string[])["iso8601", "buddhist", "chinese", "coptic", "dangi", "ethiopic", "ethioaa", "hebrew", "indian",
            "islamic", "islamic-civil", "islamic-tbla", "islamic-umalqura", "japanese", "persian", "roc"])
        {
            Assert.NotNull(Home.DateValues("en", calendar));
        }
    }
}
