using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Broiler.ECMAScript.Intl;

namespace Broiler.JavaScript.BuiltIns.Intl;

/// <summary>
/// Broiler.JS's optional provider backed by the host's cultures (milestone SJL-9.5, task 8.2; Broiler.VM's JSD-0082,
/// decisions SJL-D-22 (c) and D6; <c>Broiler.ECMAScript/docs/intl/design.md</c>, section 5): an <see cref="ILocaleData"/>
/// that <see cref="CompositeLocaleData"/> asks after the shared tables, and only for a locale the shared data neither lists
/// nor holds. It is never a second implementation of the algorithms.
/// </summary>
/// <remarks>
/// <para>
/// <b>It answers only what .NET exposes exactly</b>, for a culture the host has whose default calendar is the Gregorian one
/// and whose native digits are ASCII's:
/// </para>
/// <list type="bullet">
/// <item><b>Number symbols</b>, as the <c>latn</c> numbering system's keys of the number table: <c>symbols.decimal</c>,
/// <c>symbols.group</c>, <c>symbols.minusSign</c>, <c>symbols.plusSign</c>, <c>symbols.percentSign</c>,
/// <c>symbols.perMille</c>, <c>symbols.infinity</c> and <c>symbols.nan</c>, from the culture's
/// <see cref="NumberFormatInfo"/>. A culture whose native digits are not ASCII's writes another numbering system's symbols,
/// which .NET does not name, so it answers none.</item>
/// <item><b>Month and day names</b> of the Gregorian calendar, as the date table's keys: the stand-alone wide and
/// abbreviated months (<see cref="DateTimeFormatInfo.MonthNames"/>, <see cref="DateTimeFormatInfo.AbbreviatedMonthNames"/>),
/// the format wide and abbreviated months (<see cref="DateTimeFormatInfo.MonthGenitiveNames"/>,
/// <see cref="DateTimeFormatInfo.AbbreviatedMonthGenitiveNames"/>), and the stand-alone wide, abbreviated and short days
/// (<see cref="DateTimeFormatInfo.DayNames"/>, <see cref="DateTimeFormatInfo.AbbreviatedDayNames"/>,
/// <see cref="DateTimeFormatInfo.ShortestDayNames"/>). .NET names no narrow form, and no format form of a day.</item>
/// <item><b>The era names and AM and PM</b>: era 1 of the Gregorian calendar, abbreviated and narrow, which is what
/// <see cref="DateTimeFormatInfo.GetEraName"/> and <see cref="DateTimeFormatInfo.GetAbbreviatedEraName"/> give over ICU
/// (<c>AD</c> and <c>A</c> in <c>en-US</c>; .NET's Gregorian calendar has no era before it, and no wide name), and
/// <c>dayPeriods.format.abbreviated.am</c> and <c>.pm</c>
/// (<see cref="DateTimeFormatInfo.AMDesignator"/>, <see cref="DateTimeFormatInfo.PMDesignator"/>).</item>
/// </list>
/// <para>
/// <b>Two of decision D6's items have no question to answer</b>, as the design's dated notes read the contract: the first day
/// of the week, which .NET gives by culture where <see cref="ILocaleData.Week"/> is keyed by region; and native digits,
/// which .NET gives by culture where <see cref="ILocaleData.NumberingSystemDigits"/> is keyed by a numbering system .NET does
/// not name. Both answer "no data".
/// </para>
/// <para>
/// <b>Every other question answers "no data"</b>, so the algorithm uses the parent or root: .NET has no plural, compact,
/// unit, list, relative-time, display-name, interval or day-period data.
/// </para>
/// <para>
/// <b>It never throws for want of host data.</b> A locale the host has no culture for, or every locale under invariant
/// globalization (where <see cref="CultureInfo.GetCultures"/> lists the invariant culture alone), is reported unavailable:
/// <see cref="HoldsLocale"/> answers <see langword="false"/> and every question <see langword="null"/>. The host's
/// culture list is read once, and a culture is looked up only when a locale names one in that list: by .NET's name, by its
/// canonical tag, or by one of the shorter tags .NET resolves to it (<see cref="Tags"/>; <c>zh-TW</c> for <c>zh-Hant-TW</c>),
/// so a tag the available-locale provider lists for a host culture reads that culture.
/// </para>
/// <para>
/// It is neither the default-locale nor the available-locale provider (decision D7).
/// </para>
/// </remarks>
internal sealed class HostCultureLocaleData : ILocaleData
{
    private static readonly Lazy<HostCultureLocaleData> HostInstance =
        new(static () => new HostCultureLocaleData(), LazyThreadSafetyMode.ExecutionAndPublication);

    private static readonly string[] DayKeys = ["sun", "mon", "tue", "wed", "thu", "fri", "sat"];

    private readonly Lazy<Dictionary<string, string>> cultures;

    private readonly Lazy<IReadOnlyList<string>> locales;

    // Each tag that names a host culture, canonical tags and shorter tags alike, in order, and the .NET name of its culture.
    private readonly Lazy<(string[] Tags, Dictionary<string, string> Names)> tags;

    private HostCultureLocaleData()
    {
        cultures = new(ReadCultures, LazyThreadSafetyMode.ExecutionAndPublication);
        locales = new(() => [.. cultures.Value.Values], LazyThreadSafetyMode.ExecutionAndPublication);
        tags = new(() => ReadTags(cultures.Value.Values), LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <summary>The provider over this process's host cultures.</summary>
    internal static HostCultureLocaleData Host => HostInstance.Value;

    /// <inheritdoc/>
    /// <remarks>The host's named cultures, by .NET's names, in the order .NET lists them; empty under invariant globalization.</remarks>
    public IReadOnlyList<string> Locales => locales.Value;

    /// <summary>
    /// The host's cultures by their canonical tags, in the order .NET lists them, each followed by the shorter tags .NET
    /// resolves to it, each tag once; none under invariant globalization. The available-locale provider lists these
    /// (<see cref="JSIntlLocaleProviders.AvailableLocales"/>), and each reads its culture here.
    /// </summary>
    internal IReadOnlyList<string> Tags => tags.Value.Tags;

    /// <inheritdoc/>
    /// <remarks>The number and date tables, for a locale the host has a culture for whose answers this provider gives.</remarks>
    public bool HoldsLocale(IntlTable table, string locale) =>
        table switch
        {
            IntlTable.NumberLocales => NumberFormat(locale) is not null,
            IntlTable.DateLocales => DateFormat(locale) is not null,
            _ => false,
        };

    /// <inheritdoc/>
    public string? LikelySubtags(string source) => null;

    /// <inheritdoc/>
    public string? Alias(LocaleAliasKind kind, string from) => null;

    /// <inheritdoc/>
    public bool IsExtensionKey(char singleton, string key) => false;

    /// <inheritdoc/>
    public bool TryExtensionType(char singleton, string key, string type, out string preferred)
    {
        preferred = type;
        return false;
    }

    /// <inheritdoc/>
    public RegionHourCycles? HourCycles(string region) => null;

    /// <inheritdoc/>
    /// <remarks>No data: week data is keyed by region, and .NET gives a first day by culture (decision D6, the design's note).</remarks>
    public RegionWeek? Week(string region) => null;

    /// <inheritdoc/>
    public bool? IsRightToLeft(string script) => null;

    /// <inheritdoc/>
    public string? NumberValue(string locale, string key)
    {
        if (NumberFormat(locale) is not { } format)
            return null;

        return key switch
        {
            "symbols.decimal" => format.NumberDecimalSeparator,
            "symbols.group" => format.NumberGroupSeparator,
            "symbols.minusSign" => format.NegativeSign,
            "symbols.plusSign" => format.PositiveSign,
            "symbols.percentSign" => format.PercentSymbol,
            "symbols.perMille" => format.PerMilleSymbol,
            "symbols.infinity" => format.PositiveInfinitySymbol,
            "symbols.nan" => format.NaNSymbol,
            _ => null,
        } is { Length: > 0 } value ? value : null;
    }

    /// <inheritdoc/>
    public CurrencyNames? Currency(string locale, string code) => null;

    /// <inheritdoc/>
    public IReadOnlyList<string> CurrencyCodes => [];

    /// <inheritdoc/>
    public int? CurrencyDigits(string code) => null;

    /// <inheritdoc/>
    public IReadOnlyList<string> NumberingSystems => [];

    /// <inheritdoc/>
    /// <remarks>No data: .NET gives a culture's native digits, not a numbering system's (decision D6).</remarks>
    public IReadOnlyList<string>? NumberingSystemDigits(string system) => null;

    /// <inheritdoc/>
    public IReadOnlyList<PluralRuleText>? PluralRules(string language, PluralRuleType type) => null;

    /// <inheritdoc/>
    public string? PluralRange(string language, string start, string end) => null;

    /// <inheritdoc/>
    public string? UnitPattern(string locale, string width, string unit, string field) => null;

    /// <inheritdoc/>
    public ListPatterns? ListPattern(string locale, string type) => null;

    /// <inheritdoc/>
    public string? RelativeTime(string locale, string field, string key) => null;

    /// <inheritdoc/>
    public string? DisplayName(string locale, string kind, string code) => null;

    /// <inheritdoc/>
    public string? DateValue(string locale, string calendar, string key)
    {
        if (calendar != "gregory" || DateFormat(locale) is not { } format)
            return null;

        return DateKeys(format).TryGetValue(key, out var value) ? value : null;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// No data: the pattern search reads a locale's whole date data, and the host's names are not a whole; a value asked by
    /// its key (<see cref="DateValue"/>) is answered.
    /// </remarks>
    public IReadOnlyDictionary<string, string>? DateValues(string locale, string calendar) => null;

    /// <inheritdoc/>
    public IReadOnlyList<DayPeriodRule>? DayPeriods(string language) => null;

    /// <inheritdoc/>
    public IReadOnlyList<MetazonePeriod>? MetazonePeriods(string zone) => null;

    /// <inheritdoc/>
    public string? GoldenZone(string metazone, string region) => null;

    /// <inheritdoc/>
    public string? PrimaryZone(string region) => null;

    /// <summary>The host culture a locale names, or <see langword="null"/> when the host has none; never throws.</summary>
    internal CultureInfo? Culture(string locale)
    {
        if (string.IsNullOrEmpty(locale)
            || (!cultures.Value.TryGetValue(locale, out var name) && !tags.Value.Names.TryGetValue(locale, out name)))
            return null;

        try
        {
            return CultureInfo.GetCultureInfo(name);
        }
        catch (CultureNotFoundException)
        {
            return null;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    // The number format of a culture whose numbers are written in ASCII digits, the latn system the keys name.
    private NumberFormatInfo? NumberFormat(string locale)
    {
        if (Culture(locale) is not { } culture)
            return null;

        var format = culture.NumberFormat;
        var digits = format.NativeDigits;
        if (digits is not { Length: 10 })
            return null;

        for (var i = 0; i < 10; i++)
        {
            if (digits[i] is not { Length: 1 } digit || digit[0] != (char)('0' + i))
                return null;
        }

        return format;
    }

    // The date format of a culture whose default calendar is the Gregorian one, whose names the keys name.
    private DateTimeFormatInfo? DateFormat(string locale) =>
        Culture(locale) is { } culture && culture.DateTimeFormat is { Calendar: GregorianCalendar } format ? format : null;

    private static Dictionary<string, string> DateKeys(DateTimeFormatInfo format)
    {
        var keys = new Dictionary<string, string>(StringComparer.Ordinal);

        void Add(string key, string? value)
        {
            if (!string.IsNullOrEmpty(value))
                keys[key] = value;
        }

        for (var month = 1; month <= 12; month++)
        {
            Add("months.stand-alone.wide." + month, format.MonthNames[month - 1]);
            Add("months.stand-alone.abbreviated." + month, format.AbbreviatedMonthNames[month - 1]);
            Add("months.format.wide." + month, format.MonthGenitiveNames[month - 1]);
            Add("months.format.abbreviated." + month, format.AbbreviatedMonthGenitiveNames[month - 1]);
        }

        for (var day = 0; day < 7; day++)
        {
            Add("days.stand-alone.wide." + DayKeys[day], format.DayNames[day]);
            Add("days.stand-alone.abbreviated." + DayKeys[day], format.AbbreviatedDayNames[day]);
            Add("days.stand-alone.short." + DayKeys[day], format.ShortestDayNames[day]);
        }

        // .NET's era name is ICU's abbreviated era (CLDR's eraAbbr, "AD" in en) and its abbreviated era name ICU's narrow one
        // (CLDR's eraNarrow, "A"); it states no wide era name (CLDR's eraNames, "Anno Domini").
        Add("eras.eraAbbr.1", format.GetEraName(1));
        Add("eras.eraNarrow.1", format.GetAbbreviatedEraName(1));
        Add("dayPeriods.format.abbreviated.am", format.AMDesignator);
        Add("dayPeriods.format.abbreviated.pm", format.PMDesignator);
        return keys;
    }

    // The host's cultures by canonical tag, each followed by the tags that name the same culture without the script its
    // likely subtags supply: .NET lists zh-Hant-TW and sr-Cyrl-RS but not zh-TW or sr-RS, which it resolves to them, so
    // those are the host's too (each tag X with Add Likely Subtags(X) = Add Likely Subtags(culture)): Remove Likely
    // Subtags of the culture, and its language and region alone. A culture name that is no well-formed tag gives none.
    private static (string[] Tags, Dictionary<string, string> Names) ReadTags(IEnumerable<string> cultureNames)
    {
        var names = new Dictionary<string, string>(StringComparer.Ordinal);
        var list = new List<string>();
        var data = JSIntlData.LocaleData;
        foreach (var name in cultureNames)
        {
            if (LocaleId.TryCanonicalize(name, data, out var tag) != IntlStatus.Completed || !names.TryAdd(tag, name))
                continue;

            list.Add(tag);
            if (LocaleId.TryParse(tag, out var id) != IntlStatus.Completed || id.Script == null || id.Variants.Count != 0)
                continue;

            var maximal = id.Maximize(data, out _).ToString();
            var aliases = new List<string> { id.Minimize(data, out _).ToString() };
            if (id.Region != null)
                aliases.Add(id.Language + "-" + id.Region);

            foreach (var alias in aliases)
            {
                if (!names.ContainsKey(alias) && LocaleId.TryParse(alias, out var aliasId) == IntlStatus.Completed
                    && aliasId.Maximize(data, out _).ToString() == maximal && names.TryAdd(alias, name))
                    list.Add(alias);
            }
        }

        return ([.. list], names);
    }

    // The host's named cultures, by name, case-insensitively: none but the invariant culture under invariant globalization.
    private static Dictionary<string, string> ReadCultures()
    {
        var names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            foreach (var culture in CultureInfo.GetCultures(CultureTypes.AllCultures))
            {
                if (!string.IsNullOrEmpty(culture.Name))
                    names.TryAdd(culture.Name, culture.Name);
            }
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PlatformNotSupportedException)
        {
            names.Clear();
        }

        return names;
    }
}
