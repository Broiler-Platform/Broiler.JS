using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Broiler.ECMAScript.Intl.Tests.Differential;
using Broiler.ECMAScript.Time;
using Xunit;

namespace Broiler.ECMAScript.Intl.Tests;

/// <summary>
/// The owner's decision of 2026-11-09, "Keep + fix library first": the date-time format follows the pinned CLDR 48.2.0 data in the
/// five places it departed from it - the locale's own time data, the locale's likely region, the region's calendar preference, the
/// numbering system's own decimal symbol and the interval patterns' field widths - and Intl.Locale's hour cycles read the locale's
/// own time data too (the owner's ruling of that day, "Fix both consumers"). Each is held over Broiler.JS's tables, with the
/// fallbacks where the data has no entry, and over Broiler.VM's tables, which carry neither the calendar preferences nor a
/// numbering system's own symbols, as before.
/// </summary>
public sealed class DateTimeLocaleDataTests
{
    private static readonly CompositeLocaleData Data = new(new TableLocaleData(HomeDataSet.Current));

    private static readonly DateTimeData Dates = new(Data, HomeTimeZones.Current, HomeCalendars.Current);

    private static readonly Lazy<CompositeLocaleData> VmData = new(static () => new CompositeLocaleData(new TableLocaleData(Embedding.Both().First().Library)));

    /// <summary>2024-01-05T15:07:09.123Z, the vectors' instant.</summary>
    private const double Instant = 1704467229123;

    // ---- the engine's side, as an adapter does it, with the ca values of the matched locale -----------------------------------

    private static ResolvedLocale Resolve(ILocaleData data, string locale, string? calendar = null, string? numberingSystem = null, string? hourCycle = null)
    {
        Assert.Equal(IntlStatus.Completed, LocaleNegotiation.CanonicalizeLocaleList([locale], data, out var requested, out _));
        var match = LocaleNegotiation.MatchLocale(data.Locales, requested, "en-US");
        return LocaleNegotiation.ResolveLocale(match,
        [
            new RelevantKey("ca", DateTimeFormat.CalendarKeyValuesOf(data, match.Locale)) { Option = calendar },
            new RelevantKey("hc", DateTimeFormat.HourCycleKeyValues) { Option = hourCycle },
            new RelevantKey("nu", DateTimeFormat.NumberingSystemKeyValues(data, match.Locale)) { Option = numberingSystem },
        ], data);
    }

    private static DateTimeFormat Format(string locale, DateTimeComponents? components = null, string? dateStyle = null, string? timeStyle = null,
        string? calendar = null, string? numberingSystem = null, string? hourCycle = null, DateTimeData? dates = null)
    {
        dates ??= Dates;
        var status = DateTimeFormat.Create(dates, Resolve(dates.Data, locale, calendar, numberingSystem, hourCycle), new DateTimeFormatOptions
        {
            TimeZone = ResolvedTimeZone.Utc,
            Components = components ?? DateTimeComponents.None,
            DateStyle = dateStyle,
            TimeStyle = timeStyle,
        }, out var format);
        Assert.Equal(IntlStatus.Completed, status);
        return format!;
    }

    private static string Write(DateTimeFormat format, double time) => format.Formatter.Format(time);

    private static double Utc(int year, int month, int day, int hour = 0, int minute = 0) =>
        EcmaTime.MakeDate(EcmaTime.MakeDay(year, month - 1, day), EcmaTime.MakeTime(hour, minute, 0, 0));

    private static readonly DateTimeComponents HourAndDayPeriod = new() { Hour = "numeric", DayPeriod = "long" };

    // ---- 1. the locale's own time data (UTS #35 Part 4, Time Data; row T10) ----------------------------------------------------

    [Fact]
    public void Item_1_the_hour_cycle_is_the_locale_s_own_time_data_before_its_region_s()
    {
        // ar-001: CLDR's entry `ar-001|h hB hb H|h`, where the world's 001 prefers H (vector intl/date-times/families/0028).
        Assert.Equal("h12", Format("ar-001", new DateTimeComponents { Hour = "numeric" }).HourCycle);
        Assert.Equal("3 بعد الظهر", Write(Format("ar-001", HourAndDayPeriod), Utc(2024, 1, 5, 15)));

        // fr-CA: CLDR's entry `fr-CA|H h hB|H`, where Canada prefers h (vectors 0173 and 0175).
        Assert.Equal("h23", Format("fr-CA", timeStyle: "short").HourCycle);
        Assert.Equal("15 h 07", Write(Format("fr-CA", timeStyle: "short"), Instant));
        Assert.Equal("15 h", Write(Format("fr-CA", HourAndDayPeriod), Utc(2024, 1, 5, 15)));
    }

    [Fact]
    public void Item_1_without_an_entry_of_its_own_a_locale_reads_its_region_s_and_then_the_world_s()
    {
        Assert.Null(Data.HourCycles("en-CA"));
        Assert.Equal(Dates.Dates.HourCycles("en", "CA"), Data.HourCycles("CA"));
        Assert.Equal(Dates.Dates.HourCycles("fr", "CA"), Data.HourCycles("fr-CA"));
        Assert.Null(Data.HourCycles("de-DE"));
        Assert.Equal(Dates.Dates.HourCycles("de", "DE"), Data.HourCycles("DE"));
        Assert.Equal(Dates.Dates.HourCycles("xx", "ZZ"), Data.HourCycles("001"));
        Assert.Equal("h12", Format("en-CA", new DateTimeComponents { Hour = "numeric" }).HourCycle);
        Assert.Equal("h23", Format("de", new DateTimeComponents { Hour = "numeric" }).HourCycle);
        Assert.Equal("h12", Format("en", new DateTimeComponents { Hour = "numeric" }).HourCycle);
    }

    [Fact]
    public void Item_1_Intl_Locale_reads_the_locale_s_own_time_data_too()
    {
        // The owner's ruling "Fix both consumers": HourCyclesOfLocale keyed language-region, as the table writes CLDR's entries.
        Assert.Equal(["h23", "h12"], LocaleInfo.HourCycles(Locale("fr-CA"), Data));
        Assert.Equal(["h12", "h23"], LocaleInfo.HourCycles(Locale("ar-001"), Data));
        Assert.Equal(["h12", "h23"], LocaleInfo.HourCycles(Locale("en-001"), Data));

        // The rg override reads the entry of the language and the override's region; a region without an entry reads its own.
        Assert.Equal(["h23", "h12"], LocaleInfo.HourCycles(Locale("fr-u-rg-cazzzz"), Data));
        Assert.Equal(["h12", "h23"], LocaleInfo.HourCycles(Locale("en-CA"), Data));
        Assert.Equal(["h23", "h12"], LocaleInfo.HourCycles(Locale("de"), Data));

        // Broiler.VM's tables carry the same time data, so its Intl.Locale's answers for these move alike at its next re-pin.
        Assert.Equal(["h23", "h12"], LocaleInfo.HourCycles(Locale("fr-CA", VmData.Value), VmData.Value));
    }

    private static LocaleRecord Locale(string tag, ILocaleData? data = null)
    {
        Assert.Equal(IntlStatus.Completed, LocaleRecord.TryCreate(tag, LocaleOptions.None, data ?? Data, out var record));
        return record!;
    }

    // ---- 2. the region from likely subtags (UTS #35 Part 1) ---------------------------------------------------------------------

    [Fact]
    public void Item_2_a_locale_without_a_region_takes_the_likely_region_of_the_whole_locale()
    {
        // zh-Hant is zh-Hant-TW, where zh alone gives CN: Taiwan prefers h (vector intl/date-times/families/0490).
        Assert.Equal("h12", Format("zh-Hant", new DateTimeComponents { Hour = "numeric" }).HourCycle);
        Assert.Equal("下午3時", Write(Format("zh-Hant", HourAndDayPeriod), Utc(2024, 1, 5, 15)));

        // zh alone is zh-Hans-CN: China prefers H. A locale with a region keeps its own: fa-AF is Afghanistan's, persian
        // first, and fa with Germany's region reads no preference; a locale without likely subtags reads the world's.
        Assert.Equal("h23", Format("zh", new DateTimeComponents { Hour = "numeric" }).HourCycle);
        Assert.Equal("persian", DateTimeFormat.CalendarKeyValuesOf(Data, "fa-AF")[0]);
        Assert.Same(DateTimeFormat.CalendarKeyValues, DateTimeFormat.CalendarKeyValuesOf(Data, "fa-DE"));
        Assert.Same(DateTimeFormat.CalendarKeyValues, DateTimeFormat.CalendarKeyValuesOf(Data, "zxx"));
        Assert.Same(DateTimeFormat.CalendarKeyValues, DateTimeFormat.CalendarKeyValuesOf(Data, "not a tag"));
    }

    // ---- 3. the default calendar from the region's preference (UTS #35 Part 4, Calendar Preference Data) ----------------------------

    [Fact]
    public void Item_3_the_default_calendar_is_the_region_s_first_preference()
    {
        // fa is fa-Arab-IR: IR prefers persian (vectors 0142 and 0146); th is th-Thai-TH: TH prefers buddhist (0421 and 0425).
        Assert.Equal("persian", Format("fa").Calendar);
        Assert.Equal("persian", Format("fa-IR").Calendar);
        Assert.Equal("buddhist", Format("th").Calendar);
        Assert.Equal("buddhist", Format("th-TH").Calendar);
        Assert.Equal("۱۴۰۲ دی ۱۵, جمعه", Write(Format("fa", dateStyle: "full"), Instant));
        Assert.Equal("วันศุกร์ที่ 5 มกราคม พ.ศ. 2567",
            Write(Format("th", dateStyle: "full"), Instant));
        Assert.Equal("۱۵ تا ۱۷ دی ۱۴۰۲",
            Format("fa", dateStyle: "medium").Formatter.FormatRange(Utc(2024, 1, 5), Utc(2024, 1, 7)));
        Assert.Equal("5–7 ม.ค. 2567", Format("th", dateStyle: "medium").Formatter.FormatRange(Utc(2024, 1, 5), Utc(2024, 1, 7)));

        // The values hold every calendar once, the default first.
        var values = DateTimeFormat.CalendarKeyValuesOf(Data, "fa");
        Assert.Equal(DateTimeFormat.CalendarKeyValues.Count, values.Count);
        Assert.Equal(DateTimeFormat.CalendarKeyValues.Order(StringComparer.Ordinal), values.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void Item_3_the_keyword_and_the_option_choose_over_the_default_and_a_region_without_a_preference_reads_the_world_s()
    {
        Assert.Equal("gregory", Format("fa-u-ca-gregory").Calendar);
        Assert.Equal("fa-u-ca-gregory", Format("fa-u-ca-gregory").Locale);
        Assert.Equal("gregory", Format("th", calendar: "gregory").Calendar);
        Assert.Equal("th", Format("th", calendar: "gregory").Locale);
        Assert.Equal("gregory", Format("en").Calendar);
        Assert.Equal("gregory", Format("ja").Calendar);
        Assert.Equal("gregory", Format("ar-SA").Calendar);

        // A region CLDR states no preference for reads the world's, 001: the Gregorian calendar, the static values themselves.
        Assert.Null(Data.CalendarPreferences("DE"));
        Assert.Equal(["gregory"], Data.CalendarPreferences("001"));
        Assert.Same(DateTimeFormat.CalendarKeyValues, DateTimeFormat.CalendarKeyValuesOf(Data, "de"));
        Assert.Same(DateTimeFormat.CalendarKeyValues, DateTimeFormat.CalendarKeyValuesOf(Data, "ja"));
        Assert.Equal(["persian", "gregory", "islamic", "islamic-civil", "islamic-tbla"], Data.CalendarPreferences("IR"));
        Assert.Equal(["buddhist", "gregory"], Data.CalendarPreferences("TH"));
    }

    [Fact]
    public void Item_3_Intl_Locale_s_calendars_stay_the_Gregorian_calendar()
    {
        // Row L16 and part C of the SJL-9 rule hold getCalendars at ["gregory"] in both engines through SJL-9's window: the
        // calendar preference table feeds the date-time format's default alone.
        Assert.Equal(["gregory"], LocaleInfo.Calendars(Locale("fa")));
        Assert.Equal(["gregory"], LocaleInfo.Calendars(Locale("th-TH")));
    }

    // ---- 4. the numbering system's own decimal symbol (decision D21) -----------------------------------------------------------------

    private static readonly DateTimeComponents Seconds = new() { Hour = "numeric", Minute = "numeric", Second = "numeric", FractionalSecondDigits = 3 };

    [Fact]
    public void Item_4_a_fractional_second_takes_the_numbering_system_s_own_decimal_symbol()
    {
        // en-u-nu-arab and en-u-nu-arabext: root's symbols@arab.decimal and symbols@arabext.decimal, U+066B (vectors
        // intl/date-times/numbering/0001 and 0003); de-u-nu-arab the same, where de's latn symbol is ",".
        Assert.Equal("\u0661\u0665:\u0660\u0667:\u0660\u0669\u066B\u0661\u0662\u0663", Write(Format("en-u-nu-arab", Seconds, hourCycle: "h23"), Instant));
        Assert.Equal("\u06F1\u06F5:\u06F0\u06F7:\u06F0\u06F9\u066B\u06F1\u06F2\u06F3", Write(Format("en-u-nu-arabext", Seconds, hourCycle: "h23"), Instant));
        Assert.Equal("\u0661\u0665:\u0660\u0667:\u0660\u0669\u066B\u0661\u0662\u0663", Write(Format("de", Seconds, numberingSystem: "arab"), Instant));
        Assert.Contains(Format("en-u-nu-arab", Seconds, hourCycle: "h23").Formatter.FormatToParts(Instant), static p => p.Type == "literal" && p.Value == "\u066B");

        // A time separator is the pattern's literal: the reference writes ":" for arabext too, whose root symbol is U+066B.
        Assert.DoesNotContain("\u066B", Write(Format("en-u-nu-arabext", new DateTimeComponents { Hour = "numeric", Minute = "numeric" }, hourCycle: "h23"), Instant), StringComparison.Ordinal);
    }

    [Fact]
    public void Item_4_a_system_without_a_symbol_of_its_own_and_latn_write_the_locale_s_latn_symbol()
    {
        Assert.Equal("15:07:09.123", Write(Format("en", Seconds, hourCycle: "h23"), Instant));
        Assert.Equal("15:07:09,123", Write(Format("de", Seconds), Instant));
        Assert.Null(Data.NumberValue("en", "symbols@thai.decimal"));
        Assert.Null(Data.NumberValue("und", "symbols@thai.decimal"));
        Assert.Equal("\u0E51\u0E55:\u0E50\u0E57:\u0E50\u0E59.\u0E51\u0E52\u0E53", Write(Format("en-u-nu-thai", Seconds, hourCycle: "h23"), Instant));
    }

    // ---- 5. interval patterns' field widths (ICU's DateIntervalFormat) ----------------------------------------------------------------

    /// <summary>
    /// The brief's item 5 read: "Ranges do not widen fields to a requested 2-digit width. The library gives 1/2/2020 – 1/5/2020
    /// where ICU gives 01/02/2020 – 01/05/2020." The pinned reference (Node v26.12.0, ICU 78.3, CLDR 48.0, the bundle's
    /// <c>reference.txt</c>) and the retained answers' ICU 77.1 both answer <c>1/2/2020 – 1/5/2020</c>: ICU's DateIntervalFormat
    /// widens an interval pattern's field to the skeleton's only where the nearest interval skeleton differs from the normalized one
    /// (<c>adjustFieldWidth</c>, two-digit years), and a numeric month or day's two digits are normalized away. The library already
    /// answers as ICU does, so the item changes no code; this test holds the library to the reference's answers.
    /// </summary>
    [Theory]
    [InlineData("en", "numeric", "2-digit", "2-digit", null, null, 2020, 1, 2, 2020, 1, 5, "1/2/2020\u2009\u2013\u20091/5/2020")]
    [InlineData("en", "2-digit", "2-digit", "2-digit", null, null, 2020, 1, 2, 2020, 1, 5, "1/2/20\u2009\u2013\u20091/5/20")]
    [InlineData("en", null, "2-digit", "2-digit", null, null, 2020, 1, 2, 2020, 1, 5, "1/2\u2009\u2013\u20091/5")]
    [InlineData("en", null, "2-digit", null, null, null, 2020, 1, 2, 2020, 3, 5, "01\u2009\u2013\u200903")]
    [InlineData("en", null, null, "2-digit", null, null, 2020, 1, 2, 2020, 1, 5, "2\u2009\u2013\u20095")]
    [InlineData("en", "numeric", "2-digit", "2-digit", null, null, 2020, 1, 2, 2021, 3, 5, "1/2/2020\u2009\u2013\u20093/5/2021")]
    [InlineData("fr", "numeric", "2-digit", "2-digit", null, null, 2020, 1, 2, 2020, 1, 5, "02/01/2020\u2009\u2013\u200905/01/2020")]
    [InlineData("ja", "numeric", "2-digit", "2-digit", null, null, 2020, 1, 2, 2020, 1, 5, "2020/01/02\uFF5E2020/01/05")]
    [InlineData("de", "numeric", "numeric", "numeric", null, null, 2020, 1, 2, 2020, 1, 5, "02.\u201305.01.2020")]
    [InlineData("en", null, null, null, "2-digit", "2-digit", 2020, 1, 2, 2020, 1, 2, "9:00\u2009\u2013\u200910:00\u202FAM")]
    public void Item_5_range_fields_take_the_width_ICU_s_interval_patterns_give(string locale, string? year, string? month, string? day, string? hour,
        string? minute, int y1, int m1, int d1, int y2, int m2, int d2, string expected)
    {
        var format = Format(locale, new DateTimeComponents { Year = year, Month = month, Day = day, Hour = hour, Minute = minute });
        var (from, to) = hour is null ? (Utc(y1, m1, d1), Utc(y2, m2, d2)) : (Utc(y1, m1, d1, 9), Utc(y2, m2, d2, 10));
        Assert.Equal(expected, format.Formatter.FormatRange(from, to));
    }

    [Fact]
    public void Item_5_a_two_digit_hour_on_a_24_hour_clock_keeps_CLDR_s_two_digits()
    {
        Assert.Equal("09:00\u2009\u2013\u200910:00", Format("en", new DateTimeComponents { Hour = "2-digit", Minute = "2-digit" }, hourCycle: "h23")
            .Formatter.FormatRange(Utc(2020, 1, 2, 9), Utc(2020, 1, 2, 10)));
    }

    // ---- Broiler.VM's tables decode and answer as before ---------------------------------------------------------------------------

    [Fact]
    public void Broiler_VM_s_tables_carry_no_calendar_preference_and_no_own_symbol_and_answer_as_before()
    {
        var vm = VmData.Value;
        var vmDates = new DateTimeData(vm, HomeTimeZones.Current, HomeCalendars.Current);

        Assert.Null(vm.CalendarPreferences("001"));
        Assert.Null(vm.CalendarPreferences("IR"));
        Assert.Same(DateTimeFormat.CalendarKeyValues, DateTimeFormat.CalendarKeyValuesOf(vm, "en"));
        Assert.Same(DateTimeFormat.CalendarKeyValues, DateTimeFormat.CalendarKeyValuesOf(vm, "de-DE"));
        Assert.Null(vm.NumberValue("und", "symbols@arab.decimal"));

        // Its list's formats: the Gregorian calendar, the latn decimal symbol with arab's digits (decision D21), and the regions'
        // hour cycles, none of its data locales having a script or an entry of its own in the time data.
        Assert.Equal("gregory", Format("en", dates: vmDates).Calendar);
        Assert.Equal("\u0661\u0665:\u0660\u0667:\u0660\u0669.\u0661\u0662\u0663", Write(Format("en-u-nu-arab", Seconds, hourCycle: "h23", dates: vmDates), Instant));
        Assert.Equal("15:07:09,123", Write(Format("de", Seconds, dates: vmDates), Instant));
        Assert.Equal("h12", Format("en-US", new DateTimeComponents { Hour = "numeric" }, dates: vmDates).HourCycle);
        Assert.Equal("h23", Format("de-DE", new DateTimeComponents { Hour = "numeric" }, dates: vmDates).HourCycle);
        foreach (var locale in new[] { "de", "de-DE", "en", "en-US", "und" })
        {
            Assert.Null(vm.HourCycles(locale));
            Assert.Same(DateTimeFormat.CalendarKeyValues, DateTimeFormat.CalendarKeyValuesOf(vm, locale));
        }
    }

    // ---- the table and the question ----------------------------------------------------------------------------------------------

    [Fact]
    public void The_calendar_preference_table_decodes_and_a_malformed_line_is_refused()
    {
        var table = new TableLocaleData(new OneTable(IntlTable.CalendarPreferences, "001|gregory\nIR|persian gregory\nTH|buddhist gregory"));
        Assert.Equal(["persian", "gregory"], table.CalendarPreferences("IR"));
        Assert.Equal(["gregory"], table.CalendarPreferences("001"));
        Assert.Null(table.CalendarPreferences("DE"));
        Assert.Throws<ArgumentNullException>(() => table.CalendarPreferences(null!));

        Assert.Throws<System.IO.InvalidDataException>(() => new TableLocaleData(new OneTable(IntlTable.CalendarPreferences, "IR|persian\nIR|gregory")).CalendarPreferences("IR"));
        Assert.Throws<System.IO.InvalidDataException>(() => new TableLocaleData(new OneTable(IntlTable.CalendarPreferences, "IR|")).CalendarPreferences("IR"));
        Assert.Null(new TableLocaleData(new OneTable(IntlTable.TimeData, "001|H h|H")).CalendarPreferences("001"));

        // A provider written before the question answers "no data" through the interface's default, and the composite asks on.
        ILocaleData before = new Before();
        Assert.Null(before.CalendarPreferences("IR"));
        Assert.Equal(["persian", "gregory", "islamic", "islamic-civil", "islamic-tbla"], new CompositeLocaleData(new TableLocaleData(HomeDataSet.Current), before).CalendarPreferences("IR"));
    }

    private sealed class OneTable(IntlTable name, string text) : IIntlDataSet
    {
        public string CldrVersion => "48.2.0";

        public ReadOnlySpan<byte> Table(IntlTable table) => table == name ? Encoding.ASCII.GetBytes(text) : default;
    }

    /// <summary>A secondary provider that implements none of the questions added since: every answer is "no data".</summary>
    private sealed class Before : ILocaleData
    {
        public IReadOnlyList<string> Locales => [];
        public bool HoldsLocale(IntlTable table, string locale) => false;
        public string? LikelySubtags(string source) => null;
        public string? Alias(LocaleAliasKind kind, string from) => null;
        public bool IsExtensionKey(char singleton, string key) => false;
        public bool TryExtensionType(char singleton, string key, string type, out string preferred)
        {
            preferred = type;
            return false;
        }

        public RegionHourCycles? HourCycles(string region) => null;
        public RegionWeek? Week(string region) => null;
        public bool? IsRightToLeft(string script) => null;
        public string? NumberValue(string locale, string key) => null;
        public CurrencyNames? Currency(string locale, string code) => null;
        public IReadOnlyList<string> CurrencyCodes => [];
        public int? CurrencyDigits(string code) => null;
        public IReadOnlyList<string> NumberingSystems => [];
        public IReadOnlyList<string>? NumberingSystemDigits(string system) => null;
        public IReadOnlyList<PluralRuleText>? PluralRules(string language, PluralRuleType type) => null;
        public string? PluralRange(string language, string start, string end) => null;
        public string? UnitPattern(string locale, string width, string unit, string field) => null;
        public ListPatterns? ListPattern(string locale, string type) => null;
        public string? RelativeTime(string locale, string field, string key) => null;
        public string? DisplayName(string locale, string kind, string code) => null;
        public string? DateValue(string locale, string calendar, string key) => null;
        public IReadOnlyDictionary<string, string>? DateValues(string locale, string calendar) => null;
        public IReadOnlyList<DayPeriodRule>? DayPeriods(string language) => null;
        public IReadOnlyList<MetazonePeriod>? MetazonePeriods(string zone) => null;
        public string? GoldenZone(string metazone, string region) => null;
        public string? PrimaryZone(string region) => null;
    }
}
