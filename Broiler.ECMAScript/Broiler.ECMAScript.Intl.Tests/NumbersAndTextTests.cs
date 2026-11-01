using System;
using System.Collections.Generic;
using System.Linq;
using Broiler.ECMAScript.Numbers;
using Xunit;

namespace Broiler.ECMAScript.Intl.Tests;

/// <summary>
/// Sub-area (c) of the Intl library (stage SJL-9.3c): the number formatter, and the list, relative-time and display-name
/// formatters, each held to the answers Broiler.VM gives at its SJL-9 rule commit as the correspondence of bundle
/// <c>sjl-9-001</c> records them, row by row, over the data home's Broiler.JS tables in the composite's order.
/// </summary>
public sealed class NumbersAndTextTests
{
    private static readonly CompositeLocaleData Data = new(new TableLocaleData(HomeDataSet.Current));

    // ---- the engine's side, as an adapter does it ------------------------------------------------------------------------

    private static DigitOptions Digits(NumberStyle style = NumberStyle.Decimal, string? currency = null, NumberNotation notation = NumberNotation.Standard,
        double? mnfd = null, double? mxfd = null, double? mnsd = null, double? mxsd = null)
    {
        var defaults = DigitOptionRules.NumberFormatDefaults(style, currency, notation, Data);
        var hasSd = mnsd is not null || mxsd is not null;
        var hasFd = mnfd is not null || mxfd is not null;
        var plan = DigitOptionRules.Plan(hasSd, hasFd, RoundingPriority.Auto, notation);
        int? minSd = null, maxSd = null, minFd = null, maxFd = null;

        int? Converted(double? given, int minimum, int maximum, int? fallback)
        {
            if (given is null)
                return fallback;
            Assert.Equal(IntlStatus.Completed, DigitOptionRules.DefaultNumberOption(given.Value, minimum, maximum, out var converted));
            return converted;
        }

        if (plan.NeedSignificantDigits && hasSd)
        {
            minSd = Converted(mnsd, 1, 21, 1);
            maxSd = Converted(mxsd, minSd!.Value, 21, 21);
        }

        if (plan.NeedFractionDigits && hasFd)
        {
            minFd = Converted(mnfd, 0, 100, null);
            maxFd = Converted(mxfd, 0, 100, null);
        }

        var values = new DigitOptionValues(1, hasSd, minSd, maxSd, hasFd, minFd, maxFd, 1, RoundingMode.HalfExpand, RoundingPriority.Auto, TrailingZeroDisplay.Auto);
        Assert.Equal(IntlStatus.Completed, DigitOptionRules.SetNumberFormatDigitOptions(values, defaults, notation, out var digits));
        return digits!;
    }

    private static NumberFormatter Formatter(string locale, NumberStyle style = NumberStyle.Decimal, string? currency = null,
        CurrencyDisplay currencyDisplay = CurrencyDisplay.Symbol, CurrencySign sign = CurrencySign.Standard, string? unit = null,
        UnitDisplay unitDisplay = UnitDisplay.Short, NumberNotation notation = NumberNotation.Standard, CompactDisplay compact = CompactDisplay.Short,
        string numberingSystem = "latn", double? mxfd = null, double? mxsd = null, SignDisplay signDisplay = SignDisplay.Auto,
        NumberGrouping? grouping = null) =>
        NumberFormatter.Create(Data, locale, locale, numberingSystem, new NumberFormatOptions
        {
            Style = style,
            Currency = currency,
            CurrencyDisplay = currencyDisplay,
            CurrencySign = sign,
            Unit = unit,
            UnitDisplay = unitDisplay,
            DigitOptions = Digits(style, currency, notation, mxfd: mxfd, mxsd: mxsd),
            Notation = notation,
            CompactDisplay = compact,
            UseGrouping = grouping ?? (notation == NumberNotation.Compact ? NumberGrouping.Min2 : NumberGrouping.Auto),
            SignDisplay = signDisplay,
        });

    private static IntlDecimal N(double value) => IntlDecimal.FromNumber(value);

    private static string Range(NumberFormatter formatter, double x, double y)
    {
        Assert.Equal(IntlStatus.Completed, formatter.FormatRange(N(x), N(y), out var formatted));
        return formatted;
    }

    // ---- N: number formatting --------------------------------------------------------------------------------------------

    [Fact]
    public void N1a_N2a_N7a_The_decimal_percent_and_scientific_patterns_of_de_and_en()
    {
        Assert.Equal("1,234,567.891", Formatter("en").Format(N(1234567.891)));
        Assert.Equal("1.234.567,891", Formatter("de").Format(N(1234567.891)));
        Assert.Equal("-0", Formatter("en").Format(IntlDecimal.NegativeZero));
        Assert.Equal("NaN", Formatter("en").Format(IntlDecimal.NaN));
        Assert.Equal("-\u221e", Formatter("en").Format(IntlDecimal.NegativeInfinity));
        Assert.Equal("+5", Formatter("en", signDisplay: SignDisplay.Always).Format(N(5)));
        Assert.Equal("1234", Formatter("en", grouping: NumberGrouping.Off).Format(N(1234)));
        Assert.Equal("50 %", Formatter("de", NumberStyle.Percent).Format(N(0.5)).Replace('\u00a0', ' '));
        Assert.Equal("50%", Formatter("en", NumberStyle.Percent).Format(N(0.5)));
        Assert.Equal("1.2E5", Formatter("en", notation: NumberNotation.Scientific).Format(N(120000)));
        Assert.Equal("1.235E5", Formatter("en", notation: NumberNotation.Scientific).Format(N(123456)));
        Assert.Equal(
            ["integer", "decimal", "fraction", "exponentSeparator", "exponentInteger"],
            Formatter("en", notation: NumberNotation.Scientific).FormatToParts(N(123456)).Select(static p => p.Type));
        Assert.Equal("-1.5E-7", Formatter("en", notation: NumberNotation.Scientific).Format(N(-1.5e-7)));
    }

    [Fact]
    public void N3_Currencies_their_digits_spacing_and_accounting()
    {
        Assert.Equal("$1.00", Formatter("en", NumberStyle.Currency, "USD").Format(N(1)));
        Assert.Equal("($5.00)", Formatter("en", NumberStyle.Currency, "USD", sign: CurrencySign.Accounting).Format(N(-5)));
        Assert.Equal("1,00\u00a0\u20ac", Formatter("de", NumberStyle.Currency, "EUR").Format(N(1)));
        Assert.Equal("\u20ac1.00", Formatter("en", NumberStyle.Currency, "eur").Format(N(1)));
        Assert.Equal("EUR", Formatter("en", NumberStyle.Currency, "eur").Currency);
        Assert.Equal("\u00a51", Formatter("en", NumberStyle.Currency, "JPY").Format(N(1)));

        // N3b: CLDR's currencySpacing between a letter and the digits.
        Assert.Equal("EUR\u00a01.00", Formatter("en", NumberStyle.Currency, "EUR", CurrencyDisplay.Code).Format(N(1)));
        Assert.Equal("CHF\u00a01.00", Formatter("en", NumberStyle.Currency, "CHF").Format(N(1)));
    }

    [Fact]
    public void N4_A_currency_s_name_takes_the_number_s_plural()
    {
        var euros = Formatter("en", NumberStyle.Currency, "EUR", CurrencyDisplay.Name);
        Assert.Equal("1.00 euros", euros.Format(N(1)));
        Assert.Equal("2.00 euros", euros.Format(N(2)));
        Assert.Equal("1 Japanese yen", Formatter("en", NumberStyle.Currency, "JPY", CurrencyDisplay.Name).Format(N(1)));
        Assert.Equal("1,00 Euro", Formatter("de", NumberStyle.Currency, "EUR", CurrencyDisplay.Name).Format(N(1)));
        Assert.Equal(
            [("integer", "1"), ("decimal", "."), ("fraction", "00"), ("literal", " "), ("currency", "euros")],
            euros.FormatToParts(N(1)).Select(static p => (p.Type, p.Value)));
        Assert.Equal("1.00 XYZ", Formatter("en", NumberStyle.Currency, "XYZ", CurrencyDisplay.Name).Format(N(1)));
    }

    [Fact]
    public void N5_Units_simple_paired_and_composed()
    {
        Assert.Equal("2 liters", Formatter("en", NumberStyle.Unit, unit: "liter", unitDisplay: UnitDisplay.Long).Format(N(2)));
        Assert.Equal("2 km/h", Formatter("en", NumberStyle.Unit, unit: "kilometer-per-hour").Format(N(2)));
        Assert.Equal("-1,5 Kilometer pro Stunde", Formatter("de", NumberStyle.Unit, unit: "kilometer-per-hour", unitDisplay: UnitDisplay.Long).Format(N(-1.5)));

        // N5b: a compound CLDR has no pattern for, composed from its two units (implementation-defined).
        Assert.Equal("1 L/km", Formatter("en", NumberStyle.Unit, unit: "liter-per-kilometer").Format(N(1)));
        Assert.Equal("2 liters per kilometer", Formatter("en", NumberStyle.Unit, unit: "liter-per-kilometer", unitDisplay: UnitDisplay.Long).Format(N(2)));

        // N5c: the plural is read from the formatted digits.
        var oneDigit = NumberFormatter.Create(Data, "en", "en", "latn", new NumberFormatOptions
        {
            Style = NumberStyle.Unit,
            Unit = "liter",
            UnitDisplay = UnitDisplay.Long,
            DigitOptions = Digits(NumberStyle.Unit, mnfd: 1),
        });
        Assert.Equal("1.0 liters", oneDigit.Format(N(1)));

        // N2c (decision D35): percent as a unit keeps the percent pattern in every display.
        Assert.Equal("50 %", Formatter("de", NumberStyle.Unit, unit: "percent", unitDisplay: UnitDisplay.Long).Format(N(50)).Replace('\u00a0', ' '));
        Assert.Equal(["integer", "unit"], Formatter("en", NumberStyle.Unit, unit: "percent").FormatToParts(N(50)).Select(static p => p.Type));
    }

    [Fact]
    public void N6_Compact_notation_takes_its_exponent_after_rounding_over_the_exact_value()
    {
        var shortCompact = Formatter("en", notation: NumberNotation.Compact);
        Assert.Equal("1K", shortCompact.Format(N(999.5)));
        Assert.Equal("1M", shortCompact.Format(N(999999)));
        Assert.Equal("1M", shortCompact.Format(N(999999.9999999999)));
        Assert.Equal("1 thousand", Formatter("en", notation: NumberNotation.Compact, compact: CompactDisplay.Long).Format(N(999.5)));
        Assert.Equal("1 Tausend", Formatter("de", notation: NumberNotation.Compact, compact: CompactDisplay.Long).Format(N(999.5)));
        Assert.Equal("2 Millionen", Formatter("de", notation: NumberNotation.Compact, compact: CompactDisplay.Long).Format(N(2e6)));
        Assert.Equal("12.3K", Formatter("en", notation: NumberNotation.Compact, mxsd: 3).Format(N(12345)));
        Assert.Equal("$1.2M", Formatter("en", NumberStyle.Currency, "USD", notation: NumberNotation.Compact).Format(N(1234567)));
        Assert.Equal("$1.235E3", Formatter("en", NumberStyle.Currency, "USD", notation: NumberNotation.Scientific).Format(N(1234.5)));
        Assert.Equal("123,456,789,012,345,679T",
            shortCompact.Format(IntlDecimal.FromString("123456789012345678901234567890.5", BigIntPolicy.Unbounded)));
    }

    [Fact]
    public void N8_N17_Numbering_systems_write_their_digits_and_D21_s_own_symbols()
    {
        Assert.Equal("\u0e51,\u0e52\u0e53\u0e54.\u0e55", Formatter("en", numberingSystem: "thai").Format(N(1234.5)));
        Assert.Equal("\U0001d7cf\U0001d7d0\U0001d7d1", Formatter("en", numberingSystem: "mathbold", grouping: NumberGrouping.Off).Format(N(123)));
        Assert.Equal("\U0001d7d1\u2013\U0001d7d3", Range(Formatter("en", numberingSystem: "mathbold"), 3, 5));

        // Decision D21: arab's own symbols where Broiler.JS's embedding carries them, root's for en.
        Assert.Equal("\u061c-\u0661\u066c\u0662\u0663\u0664\u066b\u0665", Formatter("en", numberingSystem: "arab").Format(N(-1234.5)));

        // Without the keys, as in Broiler.VM's embedding, the latn symbols with the system's digits.
        var latnOnly = new CompositeLocaleData(new WithoutOwnSymbols(new TableLocaleData(HomeDataSet.Current)));
        var vm = NumberFormatter.Create(latnOnly, "en", "en", "arab", new NumberFormatOptions { DigitOptions = Digits() });
        Assert.Equal("-\u0661,\u0662\u0663\u0664.\u0665", vm.Format(N(-1234.5)));
    }

    [Fact]
    public void N9_Ranges_collapse_and_space_as_ICU_does_and_never_make_two_ranges_alike()
    {
        var en = Formatter("en");
        Assert.Equal("-1 \u2013 1", Range(en, -1, 1));
        Assert.Equal("-5 \u2013 -1", Range(en, -5, -1));
        Assert.Equal("-5 \u2013 1", Range(en, -5, 1));
        Assert.Equal("3\u20135", Range(en, 3, 5));
        Assert.Equal("~3", Range(en, 3, 3));
        Assert.Equal("\u22481", Range(Formatter("de"), 1, 1));
        Assert.Equal("300% \u2013 500%", Range(Formatter("en", NumberStyle.Percent), 3, 5));
        Assert.Equal("3E0 \u2013 5E0", Range(Formatter("en", notation: NumberNotation.Scientific), 3, 5));
        Assert.Equal("1K \u2013 5K", Range(Formatter("en", notation: NumberNotation.Compact), 1000, 5000));
        Assert.Equal("EUR 3.00\u20135.00", Range(Formatter("en", NumberStyle.Currency, "EUR", CurrencyDisplay.Code), 3, 5).Replace('\u00a0', ' '));
        Assert.Equal("($5.00\u20131.00)", Range(Formatter("en", NumberStyle.Currency, "USD", sign: CurrencySign.Accounting), -5, -1));

        // N9d: a collapsed name takes the range's plural, by the plural ranges.
        Assert.Equal("3\u20135 kilometers", Range(Formatter("en", NumberStyle.Unit, unit: "kilometer", unitDisplay: UnitDisplay.Long), 3, 5));

        Assert.Equal(IntlStatus.RangeIsNotANumber, en.FormatRange(IntlDecimal.NaN, N(1), out _));
        Assert.Equal(IntlStatus.Completed, en.FormatRangeToParts(N(3), N(5), out var parts));
        Assert.Equal([("integer", "3", "startRange"), ("literal", "\u2013", "shared"), ("integer", "5", "endRange")], parts.Select(static p => (p.Type, p.Value, p.Source!)));
    }

    [Fact]
    public void N16_The_formatter_charges_FormatNumericToString_at_each_rounding()
    {
        ulong charged = 0;
        WorkCharge charge = units => charged += units;
        Formatter("en").Format(N(1234.5), charge);
        Assert.Equal(1UL, charged);

        // Compact notation rounds twice, ComputeExponent's and the value's.
        charged = 0;
        Formatter("en", notation: NumberNotation.Compact).Format(N(1234.5), charge);
        Assert.Equal(2UL, charged);

        // A range formats each end twice, and the approximately sign's end once more.
        charged = 0;
        Assert.Equal(IntlStatus.Completed, Formatter("en").FormatRange(N(3), N(5), out _, charge));
        Assert.Equal(4UL, charged);
        charged = 0;
        Assert.Equal(IntlStatus.Completed, Formatter("en").FormatRange(N(3), N(3), out _, charge));
        Assert.Equal(3UL, charged);
    }

    [Fact]
    public void Construction_holds_the_constructor_s_checks_and_the_numbering_system_values()
    {
        Assert.True(NumberFormatter.IsWellFormedCurrencyCode("eur"));
        Assert.False(NumberFormatter.IsWellFormedCurrencyCode("\u0131SD"));
        Assert.False(NumberFormatter.IsWellFormedCurrencyCode("USD\n"));
        Assert.True(NumberFormatter.IsWellFormedUnitIdentifier("kilobit-per-stone"));
        Assert.False(NumberFormatter.IsWellFormedUnitIdentifier("kilometer-per-hour-per-second"));
        Assert.Throws<ArgumentException>(() => NumberFormatter.Create(Data, "en", "en", "latn", new NumberFormatOptions { Style = NumberStyle.Currency, DigitOptions = Digits() }));

        var en = NumberFormatter.NumberingSystemKeyValues(Data, "en");
        Assert.Equal("latn", en[0]);
        Assert.Equal("arab", NumberFormatter.NumberingSystemKeyValues(Data, "ar-EG")[0]);
        Assert.Equal("latn", NumberFormatter.NumberingSystemKeyValues(Data, "ar-EG")[1]);
        Assert.Equal(en.Count, en.Distinct().Count());
    }

    // ---- S: lists, relative times, display names -------------------------------------------------------------------------

    [Fact]
    public void S1_S3_Lists_by_CLDR_s_patterns_with_one_charge_per_template_step()
    {
        Assert.Equal("A, B, C, and D", ListFormatter.Create(Data, "en", "en", ListFormatType.Conjunction, FormatStyle.Long).Format(["A", "B", "C", "D"]));
        Assert.Equal("A & B", ListFormatter.Create(Data, "en", "en", ListFormatType.Conjunction, FormatStyle.Short).Format(["A", "B"]));
        Assert.Equal("A B C D", ListFormatter.Create(Data, "en", "en", ListFormatType.Unit, FormatStyle.Narrow).Format(["A", "B", "C", "D"]));
        Assert.Equal("a, b, c und d", ListFormatter.Create(Data, "de", "de", ListFormatType.Conjunction, FormatStyle.Long).Format(["a", "b", "c", "d"]));
        Assert.Equal("A, B, or C", ListFormatter.Create(Data, "en", "en", ListFormatType.Disjunction, FormatStyle.Long).Format(["A", "B", "C"]));
        Assert.Equal("{1}, {0}, and {0}{1}", ListFormatter.Create(Data, "en", "en", ListFormatType.Conjunction, FormatStyle.Long).Format(["{1}", "{0}", "{0}{1}"]));
        Assert.Equal("", ListFormatter.Create(Data, "en", "en", ListFormatType.Conjunction, FormatStyle.Long).Format([]));

        ulong charged = 0;
        var parts = ListFormatter.Create(Data, "en", "en", ListFormatType.Conjunction, FormatStyle.Long).FormatToParts(["a", "b", "c", "d"], units => charged += units);
        Assert.Equal(3UL, charged);
        Assert.Equal(["element", "literal", "element", "literal", "element", "literal", "element"], parts.Select(static p => p.Type));
    }

    [Fact]
    public void S4_S7_Relative_times_write_the_magnitude_in_the_tense_s_pattern()
    {
        var always = RelativeTimeFormatter.Create(Data, "en", "en", FormatStyle.Long, RelativeTimeNumeric.Always, "latn");
        Assert.Equal("3 days ago", Format(always, -3, "days"));
        Assert.Equal("1 second ago", Format(always, -1, "second"));
        Assert.Equal("0 seconds ago", Format(always, -0.0, "second"));
        Assert.Equal("1,000 seconds ago", Format(always, -1000, "second"));
        Assert.Equal("in 1.5 days", Format(always, 1.5, "day"));
        Assert.Equal("in 1 day", Format(always, 1.0001, "day"));
        Assert.Equal("in 1 hr.", Format(RelativeTimeFormatter.Create(Data, "en", "en", FormatStyle.Short, RelativeTimeNumeric.Always, "latn"), 1, "hour"));

        var auto = RelativeTimeFormatter.Create(Data, "de", "de", FormatStyle.Long, RelativeTimeNumeric.Auto, "latn");
        Assert.Equal("vorgestern", Format(auto, -2, "day"));
        Assert.Equal("heute", Format(auto, -0.0, "day"));
        Assert.Equal("vor 1 Jahr", Format(RelativeTimeFormatter.Create(Data, "de", "de", FormatStyle.Long, RelativeTimeNumeric.Always, "latn"), -1.0001, "year"));

        // S5: the numbering system reaches the inner NumberFormat.
        Assert.Equal("in \u0661\u0662 days", Format(RelativeTimeFormatter.Create(Data, "en", "en", FormatStyle.Long, RelativeTimeNumeric.Always, "arab"), 12, "day"));

        Assert.Equal(IntlStatus.RelativeTimeValueNotFinite, always.Format(double.PositiveInfinity, "decade", out _));
        Assert.Equal(IntlStatus.InvalidRelativeTimeUnit, always.Format(1, "decade", out _));
        Assert.Equal(IntlStatus.Completed, always.FormatToParts(-1, "second", out var parts));
        Assert.Equal([("integer", "1", "second"), ("literal", " second ago", (string?)null)], parts.Select(static p => (p.Type, p.Value, p.Unit)));

        // The plural rules' and the number format's charges, each FormatNumericToString's.
        ulong charged = 0;
        Assert.Equal(IntlStatus.Completed, always.Format(3, "day", out _, units => charged += units));
        Assert.Equal(2UL, charged);
    }

    private static string Format(RelativeTimeFormatter formatter, double value, string unit)
    {
        Assert.Equal(IntlStatus.Completed, formatter.Format(value, unit, out var formatted));
        return formatted;
    }

    [Fact]
    public void S8_S10_Display_names_by_CLDR_and_codes_by_ASCII_casing()
    {
        Assert.Equal("British English", Of("en", DisplayNamesType.Language, "en-GB"));
        Assert.Equal("English (United Kingdom)", Of("en", DisplayNamesType.Language, "en-GB", languageDisplay: LanguageDisplay.Standard));
        Assert.Equal("United States", Of("en", DisplayNamesType.Region, "us"));
        Assert.Equal("UK", Of("en", DisplayNamesType.Region, "GB", FormatStyle.Short));
        Assert.Equal("Latin", Of("en", DisplayNamesType.Script, "latn"));
        Assert.Equal("Gregorian Calendar", Of("en", DisplayNamesType.Calendar, "GREGORY"));
        Assert.Equal("week", Of("en", DisplayNamesType.DateTimeField, "weekOfYear"));
        Assert.Equal("Euro", Of("en", DisplayNamesType.Currency, "eur"));
        Assert.Equal("Deutsch", Of("de", DisplayNamesType.Language, "de"));
        Assert.Equal("XYZ", Of("en", DisplayNamesType.Currency, "xyz"));
        Assert.Null(Of("en", DisplayNamesType.Currency, "XYZ", fallback: DisplayNamesFallback.None));
        Assert.Equal("xyz (Germany)", Of("en", DisplayNamesType.Language, "xyz-DE"));
        Assert.Null(Of("en", DisplayNamesType.Language, "xyz-DE", fallback: DisplayNamesFallback.None));

        // S9: the canonical code, and every ill-formed one refused.
        var region = DisplayNamesCore.Create(Data, "en", "en", FormatStyle.Long, DisplayNamesType.Region, DisplayNamesFallback.Code, LanguageDisplay.Dialect);
        Assert.Equal(IntlStatus.Completed, region.CanonicalCode("Us", out var canonical));
        Assert.Equal("US", canonical);
        foreach (var (type, code) in new[]
        {
            (DisplayNamesType.Region, "US\n"), (DisplayNamesType.Region, "\u0661\u0662\u0663"), (DisplayNamesType.Region, "U\u212a"),
            (DisplayNamesType.Script, "Latn\n"), (DisplayNamesType.Calendar, "gregory\n"), (DisplayNamesType.Language, "en\n"),
            (DisplayNamesType.Language, "de-\u0661\u0662\u0663"), (DisplayNamesType.Language, "en-u-ca-gregory"), (DisplayNamesType.Language, "en_GB"),
            (DisplayNamesType.DateTimeField, "WEEKDAY"), (DisplayNamesType.Currency, "\u0131SD"),
        })
        {
            var names = DisplayNamesCore.Create(Data, "en", "en", FormatStyle.Long, type, DisplayNamesFallback.Code, LanguageDisplay.Dialect);
            Assert.Equal(IntlStatus.InvalidDisplayNamesCode, names.Of(code, out _));
        }

        Assert.Null(DisplayNamesCore.Create(Data, "en", "en", FormatStyle.Long, DisplayNamesType.Region, DisplayNamesFallback.Code, LanguageDisplay.Dialect).LanguageDisplay);
    }

    private static string? Of(string locale, DisplayNamesType type, string code, FormatStyle style = FormatStyle.Long,
        DisplayNamesFallback fallback = DisplayNamesFallback.Code, LanguageDisplay languageDisplay = LanguageDisplay.Dialect)
    {
        var names = DisplayNamesCore.Create(Data, locale, locale, style, type, fallback, languageDisplay);
        Assert.Equal(IntlStatus.Completed, names.Of(code, out var name));
        return name;
    }

    /// <summary>The home's data less every numbering system's own symbols, as Broiler.VM's embedding carries none (decision D21).</summary>
    private sealed class WithoutOwnSymbols(ILocaleData inner) : ILocaleData
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

        public string? NumberValue(string locale, string key) => key.StartsWith("symbols@", StringComparison.Ordinal) ? null : inner.NumberValue(locale, key);

        public CurrencyNames? Currency(string locale, string code) => inner.Currency(locale, code);

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
    }
}
