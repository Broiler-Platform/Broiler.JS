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

    /// <summary>
    /// The values a stage appended for a table Broiler.VM's data never carries (decision D8): milestone SJL-17.3's collation
    /// settings, which an engine without them answers as an empty span. <i>(Added 2026-11-02, at milestone SJL-17.3; until then
    /// the test held that no value was appended.)</i>
    /// </summary>
    private static readonly (string Name, int Number)[] Appended = [("CollationSettings", 32)];

    [Fact]
    public void IntlTable_keeps_JsIntlTable_s_numbers_and_appends_only_tables_Broiler_VM_s_data_never_carries()
    {
        var values = Enum.GetValues<IntlTable>().Select(static t => (t.ToString(), (int)t)).ToArray();
        Assert.Equal([.. BroilerVmTables, .. Appended], values);
    }

    [Fact]
    public void The_numbers_Broiler_VM_retired_stay_retired()
    {
        foreach (var retired in (int[])[7, 23, 24])
            Assert.False(Enum.IsDefined((IntlTable)retired), retired + " is reused");
    }

    /// <summary>
    /// Every table the data home's generator writes for Broiler.JS is one Broiler.VM's data carries, so the stage appends no
    /// value (decision D8): each file of <c>data/intl/generated/</c> but the manifest names an <see cref="IntlTable"/>. <i>(Revised
    /// 2026-11-02, at milestone SJL-17.3: Broiler.JS's tables carry the collation settings too, the one value appended for a table
    /// Broiler.VM's data never carries; the file of each is still named by an <see cref="IntlTable"/>.)</i>
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
             ("InvalidVariants", 5), ("InvalidType", 6), ("InvalidKey", 7), ("NumberOptionOutOfRange", 8), ("RoundingIncrementNotAdmitted", 9),
             ("FractionDigitsOutOfOrder", 10), ("RoundingIncrementNeedsFractionDigits", 11), ("RoundingIncrementNeedsEqualFractionDigits", 12),
             ("RangeIsNotANumber", 13), ("RelativeTimeValueNotFinite", 14), ("InvalidRelativeTimeUnit", 15), ("InvalidDisplayNamesCode", 16),
             ("TimeZoneOffsetHasSeconds", 17), ("InvalidTimeZone", 18), ("StyleWithComponents", 19), ("DateFormatWithTimeStyle", 20),
             ("TimeFormatWithDateStyle", 21), ("TimeValueOutOfRange", 22), ("TemporalTypeNotFormatted", 23), ("TemporalCalendarMismatch", 24),
             ("ZonedDateTimeNotFormatted", 25), ("RangeOfDifferentTypes", 26), ("FractionalDurationUnitAlwaysDisplayed", 27),
             ("DurationUnitAfterFractional", 28), ("DurationUnitAfterNumeric", 29),
             ("NormalizedStringTooLong", 30)],
            Enum.GetValues<IntlStatus>().Select(static s => (s.ToString(), (int)s)).ToArray());
    }
}
