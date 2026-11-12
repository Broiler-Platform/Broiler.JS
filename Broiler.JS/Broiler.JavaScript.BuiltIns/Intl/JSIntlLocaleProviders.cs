using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using Broiler.ECMAScript.Intl;

namespace Broiler.JavaScript.BuiltIns.Intl;

/// <summary>
/// This engine's default-locale and available-locale providers (decision SJL-D-19; Broiler.VM's JSD-0082, decision D7 as
/// revised; <c>Broiler.ECMAScript/docs/intl/design.md</c>, section 2), which the shared negotiation reads (milestone
/// SJL-9.5, task 8.3, sub-area (a)).
/// </summary>
/// <remarks>
/// <para>
/// <b>The policies stand</b>: the default locale is the host's <see cref="CultureInfo.CurrentCulture"/>, <c>en-US</c> when
/// that is the invariant culture or names no well-formed tag; the available locales are the host's cultures, each by its
/// canonical tag and by the shorter tags .NET resolves to it (<c>zh-TW</c> for <c>zh-Hant-TW</c>), which the lookup
/// matcher would otherwise truncate to another variety (<c>zh</c>).
/// </para>
/// <para>
/// <b>A reading of decision D7 that waits on the owner</b> (2026-11-04, SJL-9.5, task 8.3): D7 names Broiler.JS's available
/// locales "the host's cultures", and its revised text says the two readings "move Broiler.JS's answers in two places only:
/// under invariant globalization, and with no locale under an <c>az</c>, <c>lt</c> or <c>tr</c> host culture". This class
/// reads "the host's cultures" as each culture by its canonical tag and by the shorter tags .NET resolves to it
/// (<see cref="HostCultureLocaleData.Tags"/>), which .NET does not list. Without the shorter tags, row L29's resolution
/// truncates <c>zh-TW</c> to <c>zh</c>, and four test262 files passing at the base fail
/// (<c>intl402/NumberFormat/prototype/format/notation-compact-zh-TW.js</c> and <c>unit-zh-TW.js</c>, and the same two under
/// <c>formatToParts</c>). The home's design (<c>Broiler.ECMAScript/docs/intl/design.md</c>, the locale-parent note) asks
/// that SJL-9.5, "before it makes such a locale available, either walks through likely subtags (<c>zh-TW</c>,
/// <c>zh-Hant-TW</c>, <c>zh-Hant</c>) or adds a <c>parentLocales</c> table": the host-culture provider answers such a tag
/// from its culture, and the walk for the shared data is task 8.4's, before any service reads locale-keyed shared data.
/// Both are for the owner to confirm in JSD-0082 or this repository's plan pair.
/// </para>
/// <para>
/// <b>The two readings decision D7 adds</b>, which the pinned text requires of any policy:
/// </para>
/// <list type="bullet">
/// <item><b>The default is available</b> (row L44): the available locales always hold the default locale and each less
/// narrow tag ECMA-402's lookup reaches from it, so under invariant globalization, where the host lists no named culture,
/// they are <c>en-US</c> and <c>en</c>.</item>
/// <item><b>One default</b> (row L35): every reader of DefaultLocale reads <see cref="DefaultLocale"/> - every Intl
/// service's resolution, and TransformCase with no locale.</item>
/// </list>
/// <para>
/// The host-culture provider (<see cref="HostCultureLocaleData"/>) is a data provider, not one of these.
/// </para>
/// </remarks>
internal static class JSIntlLocaleProviders
{
    // The available locales for each default locale a process has seen: the host's, with the default and its prefixes.
    private static readonly ConcurrentDictionary<string, IReadOnlyList<string>> AvailableByDefault = new(StringComparer.Ordinal);

    // The canonical tag of each culture name CurrentCulture has had.
    private static readonly ConcurrentDictionary<string, string> DefaultByCulture = new(StringComparer.Ordinal);

    /// <summary>
    /// ECMA-402's DefaultLocale: the host's current culture as a canonical tag, or <c>en-US</c> when the culture is the
    /// invariant one or its name is no well-formed tag.
    /// </summary>
    internal static string DefaultLocale
    {
        get
        {
            // Kept on the host's culture: the default-locale provider, decision D7 (milestone SJL-9's clause 8, on the
            // owner's ruling of 2026-11-11, "Fix all + enforce"). It names the culture it reads, so the globalization
            // rules the engine's build holds (Broiler.JS/host-culture.globalconfig) accept it, and it needs no
            // suppression.
            var name = CultureInfo.CurrentCulture.Name;
            return string.IsNullOrEmpty(name)
                ? "en-US"
                : DefaultByCulture.GetOrAdd(name, static culture => Canonical(culture) ?? "en-US");
        }
    }

    /// <summary>
    /// Every service's [[AvailableLocales]]: the host's cultures by their canonical tags and the shorter tags .NET resolves to
    /// them (<see cref="HostCultureLocaleData.Tags"/>), with <see cref="DefaultLocale"/> and
    /// each less narrow tag of it the lookup reaches, in that order, each once.
    /// </summary>
    internal static IReadOnlyList<string> AvailableLocales
    {
        get
        {
            var defaultLocale = DefaultLocale;
            return AvailableByDefault.GetOrAdd(defaultLocale, static locale => WithDefault(HostCultureLocaleData.Host.Tags, locale));
        }
    }

    // The host's list, then the default and the prefixes ECMA-402's lookup truncates it to, those the list lacks.
    private static IReadOnlyList<string> WithDefault(IReadOnlyList<string> host, string defaultLocale)
    {
        var seen = new HashSet<string>(host, StringComparer.Ordinal);
        var list = new List<string>(host);
        for (var tag = defaultLocale; tag.Length != 0;)
        {
            if (seen.Add(tag))
                list.Add(tag);

            var dash = tag.LastIndexOf('-');
            if (dash < 0)
                break;

            tag = tag[..dash];
            if (tag.Length >= 2 && tag[^2] == '-')
                tag = tag[..^2];
        }

        return list.AsReadOnly();
    }

    // The locales the shared data lists, for DataLocale.
    private static readonly Lazy<HashSet<string>> SharedLocales =
        new(static () => new HashSet<string>(JSIntlData.Shared.Locales, StringComparer.Ordinal));

    /// <summary>
    /// The locale at which a service reads the locale data for a matched available locale ([[DataLocale]]): the locale's
    /// language identifier, except that a tag with a region and no script whose likely script differs from its language's
    /// (<c>zh-TW</c>, whose likely subtags are <c>zh-Hant-TW</c> where <c>zh</c>'s are <c>zh-Hans-CN</c>) is read with that
    /// script, so the parent walk by truncation (<see cref="CompositeLocaleData.Chain"/>) goes <c>zh-Hant-TW</c>,
    /// <c>zh-Hant</c>, <c>zh</c> rather than <c>zh-TW</c>, <c>zh</c>.
    /// </summary>
    /// <remarks>
    /// The walk through likely subtags the home's design asks for before such a locale reads locale-keyed shared data
    /// (<c>Broiler.ECMAScript/docs/intl/design.md</c>, the locale-parent note), which stage 1 of SJL-9.5 left to task 8.4
    /// (recorded 2026-11-08, at SJL-9.5, task 8.4). The resolved locale a service reports is the matched one, unchanged.
    /// <para>
    /// Not covered (recorded 2026-11-08, at SJL-9.5, task 8.4, a review finding): a host locale whose CLDR parent is not its
    /// truncation and whose likely script is its language's still reads by truncation - <c>es-MX</c>, <c>es-AR</c> and the
    /// other children of <c>es-419</c> read <c>es</c>; <c>pt-AO</c> and <c>pt-MZ</c> read <c>pt</c>, not <c>pt-PT</c>;
    /// <c>en-SG</c> and <c>en-ZA</c> read <c>en</c>, not <c>en-001</c>. The design's other answer, a <c>parentLocales</c>
    /// table, is data the home does not carry (decision D8); it is a change to the data home and the library, made as its
    /// own merge, not here.
    /// </para>
    /// </remarks>
    internal static string DataLocale(string locale)
    {
        if (LocaleId.TryParse(locale, out var id) != IntlStatus.Completed || id is null)
            return locale;

        var baseName = id.BaseName;
        if (id.Script is not null || id.Region is null || SharedLocales.Value.Contains(baseName))
            return baseName;

        var data = JSIntlData.LocaleData;
        var full = id.WithoutUnicodeExtension().Maximize(data, out var found);
        if (!found || full.Script is not { } script
            || LocaleId.TryParse(id.Language, out var language) != IntlStatus.Completed || language is null)
            return baseName;

        var languageScript = language.Maximize(data, out var languageFound).Script;
        return languageFound && languageScript != script ? id.Language + "-" + script + "-" + id.Region : baseName;
    }

    // A culture name as a canonical Unicode locale identifier, or null when it is not a well-formed tag.
    private static string Canonical(string name) =>
        LocaleId.TryCanonicalize(name, JSIntlData.LocaleData, out var canonical) == IntlStatus.Completed ? canonical : null;
}

/// <summary>
/// The locale data a service of sub-areas (b) and (c) reads for one data locale: <see cref="JSIntlData.LocaleData"/>, except
/// that the plural and plural-range rules of the data locale's language are read at the data locale itself, so its parent
/// walk reaches a regional variety's own rules (<c>pt-PT</c>'s <c>i = 1 and v = 0</c>, where <c>pt</c>'s read
/// <c>i = 0..1</c>) (SJL-9.5, task 8.4, 2026-11-08).
/// </summary>
/// <remarks>
/// The shared library selects plural rules by the data locale's language (<c>Broiler.ECMAScript/docs/intl/design.md</c>,
/// the note on plural rules), and that note asks SJL-9.5, before it makes <c>pt-PT</c> available, to "either keys the rules
/// by the data locale's parent chain or records the choice"; this adapter keys them by the chain, as the composite walks
/// every other locale-keyed question. Every other question is the inner data's.
/// </remarks>
internal sealed class DataLocalePluralData(ILocaleData inner, string dataLocale) : ILocaleData
{
    private readonly string language = LocaleId.TryParse(dataLocale, out var id) == IntlStatus.Completed && id is not null ? id.Language : dataLocale;

    /// <summary>The adapter for a data locale, or the shared composite itself where the locale is its own language.</summary>
    internal static ILocaleData For(string dataLocale)
    {
        var data = JSIntlData.LocaleData;
        return LocaleId.TryParse(dataLocale, out var id) == IntlStatus.Completed && id is not null && id.BaseName != id.Language
            ? new DataLocalePluralData(data, id.BaseName)
            : data;
    }

    public IReadOnlyList<PluralRuleText>? PluralRules(string language, PluralRuleType type)
        => language == this.language ? inner.PluralRules(dataLocale, type) : inner.PluralRules(language, type);

    public string? PluralRange(string language, string start, string end)
        => language == this.language ? inner.PluralRange(dataLocale, start, end) : inner.PluralRange(language, start, end);

    public IReadOnlyList<string> Locales => inner.Locales;
    public bool HoldsLocale(IntlTable table, string locale) => inner.HoldsLocale(table, locale);
    public string? LikelySubtags(string source) => inner.LikelySubtags(source);
    public string? Alias(LocaleAliasKind kind, string from) => inner.Alias(kind, from);
    public bool IsExtensionKey(char singleton, string key) => inner.IsExtensionKey(singleton, key);
    public bool TryExtensionType(char singleton, string key, string type, out string preferred) => inner.TryExtensionType(singleton, key, type, out preferred);
    public RegionHourCycles? HourCycles(string region) => inner.HourCycles(region);
    public RegionWeek? Week(string region) => inner.Week(region);
    public bool? IsRightToLeft(string script) => inner.IsRightToLeft(script);
    public string? NumberValue(string locale, string key) => inner.NumberValue(locale, key);
    public CurrencyNames? Currency(string locale, string code) => inner.Currency(locale, code);
    public IReadOnlyList<string> CurrencyCodes => inner.CurrencyCodes;
    public int? CurrencyDigits(string code) => inner.CurrencyDigits(code);
    public IReadOnlyList<string> NumberingSystems => inner.NumberingSystems;
    public IReadOnlyList<string>? NumberingSystemDigits(string system) => inner.NumberingSystemDigits(system);
    public string? UnitPattern(string locale, string width, string unit, string field) => inner.UnitPattern(locale, width, unit, field);
    public ListPatterns? ListPattern(string locale, string type) => inner.ListPattern(locale, type);
    public string? RelativeTime(string locale, string field, string key) => inner.RelativeTime(locale, field, key);
    public string? DisplayName(string locale, string kind, string code) => inner.DisplayName(locale, kind, code);
    public string? DateValue(string locale, string calendar, string key) => inner.DateValue(locale, calendar, key);
    public IReadOnlyDictionary<string, string>? DateValues(string locale, string calendar) => inner.DateValues(locale, calendar);
    public IReadOnlyList<DayPeriodRule>? DayPeriods(string language) => inner.DayPeriods(language);
    public IReadOnlyList<MetazonePeriod>? MetazonePeriods(string zone) => inner.MetazonePeriods(zone);
    public string? GoldenZone(string metazone, string region) => inner.GoldenZone(metazone, region);
    public string? PrimaryZone(string region) => inner.PrimaryZone(region);
}
