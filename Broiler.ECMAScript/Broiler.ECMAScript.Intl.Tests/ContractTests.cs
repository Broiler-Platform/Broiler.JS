using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Broiler.ECMAScript.Intl.Tests;

/// <summary>The table contract's numbers (Broiler.VM's JSD-0082, decision D8) and the statuses' numbers.</summary>
public sealed class ContractTests
{
    /// <summary>Broiler.VM's <c>JsIntlTable</c> at 4cf9ca44 (<c>src/Broiler.VM.Profile.JavaScript.Format/JsIntlData.cs</c>), name and number.</summary>
    private static readonly (string Name, int Number)[] BroilerVmTables =
    [
        ("None", 0), ("LikelySubtags", 1), ("Aliases", 2), ("Extensions", 3), ("Locales", 4), ("CollationRoot", 5),
        ("CollationTailorings", 6), ("NumberLocales", 8), ("Currencies", 9), ("CurrencyDigits", 10), ("NumberingSystems", 11),
        ("Plurals", 12), ("PluralRanges", 13), ("Units", 14), ("DateLocales", 15), ("TimeData", 16), ("DayPeriods", 17),
        ("WeekData", 18), ("Scripts", 19), ("Ordinals", 20), ("ListPatterns", 21), ("RelativeTimes", 22), ("DisplayNames", 25),
        ("TimeZones", 26), ("TimeZoneIds", 27), ("TimeZoneRegions", 28), ("Calendars", 29), ("MetaZones", 30), ("TimeZoneRelease", 31),
    ];

    [Fact]
    public void IntlTable_keeps_JsIntlTable_s_numbers_and_appends_none()
    {
        var values = Enum.GetValues<IntlTable>().Select(static t => (t.ToString(), (int)t)).ToArray();
        Assert.Equal(BroilerVmTables, values);
    }

    [Fact]
    public void The_numbers_Broiler_VM_retired_stay_retired()
    {
        foreach (var retired in (int[])[7, 23, 24])
            Assert.False(Enum.IsDefined((IntlTable)retired), retired + " is reused");
    }

    /// <summary>
    /// Every table the data home's generator writes for Broiler.JS is one Broiler.VM's data carries, so the stage appends no
    /// value (decision D8): each file of <c>data/intl/generated/</c> but the manifest names an <see cref="IntlTable"/>.
    /// </summary>
    [Fact]
    public void Every_table_of_the_data_home_is_an_IntlTable()
    {
        var files = Directory.EnumerateFiles(HomeDataSet.Generated()).Select(static p => Path.GetFileName(p)).Where(static f => f != "tables.manifest").ToList();
        Assert.NotEmpty(files);
        var named = Enum.GetValues<IntlTable>()
            .SelectMany(static t => new[] { HomeDataSet.FileName(t, text: true), HomeDataSet.FileName(t, text: false) })
            .ToHashSet(StringComparer.Ordinal);
        Assert.All(files, f => Assert.Contains(f, named));
    }

    [Fact]
    public void IntlStatus_numbers_are_stable()
    {
        Assert.Equal(
            [("Completed", 0), ("InvalidLanguageTag", 1), ("InvalidLanguageSubtag", 2), ("InvalidScriptSubtag", 3), ("InvalidRegionSubtag", 4),
             ("InvalidVariants", 5), ("InvalidType", 6), ("InvalidKey", 7)],
            Enum.GetValues<IntlStatus>().Select(static s => (s.ToString(), (int)s)).ToArray());
    }
}
