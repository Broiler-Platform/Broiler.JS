// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   26
// Annotated:        26/26
// Exempt:           5
// Human-reviewed:   0/26
// IP risk:          Low
// Security risk:    High
// Criteria:         2/2
// Resource impact:  1/10 max
// Unverified:       26
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Generic;

namespace Broiler.ECMAScript.Intl;

/// <summary>
/// The providers of <see cref="ILocaleData"/> in the data contract's order (Broiler.VM's JSD-0082, decision D8; the home's
/// design, section 1): the shared data first, then an optional secondary provider, then the locale's parent and root.
/// </summary>
/// <remarks>
/// <para>
/// <b>A question about a locale</b> walks the locale's chain: the locale, then each tag its last subtag's removal leaves,
/// then <c>und</c>, CLDR's root. At each tag it asks the shared data, and then the secondary provider. The walk stops at the
/// first answer, and at the first tag whose table the shared data holds (<see cref="ILocaleData.HoldsLocale"/>): the shared
/// tables are CLDR's resolved data, so a value a held locale lacks is one CLDR does not give it, and neither a parent nor
/// the secondary provider may supply it. So the secondary provider answers only for a locale the shared data lacks, as
/// decision D6 asks of Broiler.JS's host-culture provider.
/// </para>
/// <para>
/// <b>A question about the world's data</b> - likely subtags, aliases, extensions, a region's hour cycles and week, a
/// script's direction, currency digits and numbering systems - asks the shared data and then the secondary provider. The
/// lists the shared data names (<see cref="Locales"/>, <see cref="CurrencyCodes"/>, <see cref="NumberingSystems"/>) are
/// the shared data's alone: the available locales are an engine's provider's (decision D7), not a data provider's.
/// </para>
/// <para>
/// <b>The parent is the tag less its last subtag.</b> The tables carry no <c>parentLocales</c> data, and for every locale an
/// embedding lists the truncation is CLDR's parent or a locale the data lacks; a locale outside the lists, which only a
/// secondary provider can make available, reads its parent's data the same way.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 composite order; IP=Low; Security=Low; Resources=0; Fingerprint=E4CD50
// Broiler-Human:        PENDING
public sealed class CompositeLocaleData : ILocaleData
{
    private readonly ILocaleData shared;

    private readonly ILocaleData? secondary;

    /// <summary>Orders the shared data and an optional secondary provider.</summary>
    /// <param name="shared">The shared data, ordinarily a <see cref="TableLocaleData"/>.</param>
    /// <param name="secondary">The secondary provider, or <see langword="null"/>; Broiler.VM registers none (decision D6).</param>
    /// <exception cref="ArgumentNullException"><paramref name="shared"/> is null.</exception>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 composite order; IP=Low; Security=Low; Resources=1; Fingerprint=1694B4
    // Broiler-Human:        PENDING
    public CompositeLocaleData(ILocaleData shared, ILocaleData? secondary = null)
    {
        ArgumentNullException.ThrowIfNull(shared);
        this.shared = shared;
        this.secondary = secondary;
    }

    /// <summary>The locale chain a question about a locale walks: the locale, each tag its last subtag's removal leaves, and <c>und</c>.</summary>
    /// <param name="locale">The locale as the tables key it.</param>
    /// <returns>The chain, from the locale to <c>und</c>, each tag once.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="locale"/> is null.</exception>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 composite order; IP=Low; Security=Medium; Resources=1; Fingerprint=F74417
    // Broiler-Human:        PENDING
    public static IReadOnlyList<string> Chain(string locale)
    {
        ArgumentNullException.ThrowIfNull(locale);

        var chain = new List<string>();

        for (var tag = locale; ;)
        {
            chain.Add(tag);

            if (tag == "und")
            {
                return chain.AsReadOnly();
            }

            var dash = tag.LastIndexOf('-');

            if (dash <= 0)
            {
                chain.Add("und");
                return chain.AsReadOnly();
            }

            tag = tag[..dash];
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> Locales => shared.Locales;

    /// <inheritdoc/>
    /// <remarks>The composite holds a locale when its shared data does.</remarks>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 composite order; IP=Low; Security=Low; Resources=1; Fingerprint=DD6A47
    // Broiler-Human:        PENDING
    public bool HoldsLocale(IntlTable table, string locale) => shared.HoldsLocale(table, locale);

    /// <inheritdoc/>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 composite order; IP=Low; Security=Low; Resources=1; Fingerprint=1AED59
    // Broiler-Human:        PENDING
    public string? LikelySubtags(string source) => shared.LikelySubtags(source) ?? secondary?.LikelySubtags(source);

    /// <inheritdoc/>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 composite order; IP=Low; Security=Low; Resources=1; Fingerprint=6D651A
    // Broiler-Human:        PENDING
    public string? Alias(LocaleAliasKind kind, string from) => shared.Alias(kind, from) ?? secondary?.Alias(kind, from);

    /// <inheritdoc/>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 composite order; IP=Low; Security=Low; Resources=1; Fingerprint=7141DB
    // Broiler-Human:        PENDING
    public bool IsExtensionKey(char singleton, string key) =>
        shared.IsExtensionKey(singleton, key) || secondary is not null && secondary.IsExtensionKey(singleton, key);

    /// <inheritdoc/>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 composite order; IP=Low; Security=Low; Resources=1; Fingerprint=E7C8B2
    // Broiler-Human:        PENDING
    public bool TryExtensionType(char singleton, string key, string type, out string preferred)
    {
        if (shared.TryExtensionType(singleton, key, type, out preferred))
        {
            return true;
        }

        if (secondary is not null && secondary.TryExtensionType(singleton, key, type, out preferred))
        {
            return true;
        }

        preferred = type;
        return false;
    }

    /// <inheritdoc/>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 composite order; IP=Low; Security=Low; Resources=1; Fingerprint=BD1E4A
    // Broiler-Human:        PENDING
    public RegionHourCycles? HourCycles(string region) => shared.HourCycles(region) ?? secondary?.HourCycles(region);

    /// <inheritdoc/>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 composite order; IP=Low; Security=Low; Resources=1; Fingerprint=7193CB
    // Broiler-Human:        PENDING
    public RegionWeek? Week(string region) => shared.Week(region) ?? secondary?.Week(region);

    /// <inheritdoc/>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 composite order; IP=Low; Security=Low; Resources=1; Fingerprint=150BE3
    // Broiler-Human:        PENDING
    public bool? IsRightToLeft(string script) => shared.IsRightToLeft(script) ?? secondary?.IsRightToLeft(script);

    /// <inheritdoc/>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 composite order; IP=Low; Security=Low; Resources=1; Fingerprint=F31C7A
    // Broiler-Human:        PENDING
    public string? NumberValue(string locale, string key) =>
        Resolve(IntlTable.NumberLocales, locale, (data, tag) => data.NumberValue(tag, key));

    /// <inheritdoc/>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 composite order; IP=Low; Security=Low; Resources=1; Fingerprint=A2D3CE
    // Broiler-Human:        PENDING
    public CurrencyNames? Currency(string locale, string code) =>
        ResolveValue(IntlTable.Currencies, locale, (data, tag) => data.Currency(tag, code));

    /// <inheritdoc/>
    public IReadOnlyList<string> CurrencyCodes => shared.CurrencyCodes;

    /// <inheritdoc/>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 composite order; IP=Low; Security=Low; Resources=1; Fingerprint=1D46C6
    // Broiler-Human:        PENDING
    public int? CurrencyDigits(string code) => shared.CurrencyDigits(code) ?? secondary?.CurrencyDigits(code);

    /// <inheritdoc/>
    public IReadOnlyList<string> NumberingSystems => shared.NumberingSystems;

    /// <inheritdoc/>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 composite order; IP=Low; Security=Low; Resources=1; Fingerprint=0AB88A
    // Broiler-Human:        PENDING
    public IReadOnlyList<string>? NumberingSystemDigits(string system) =>
        shared.NumberingSystemDigits(system) ?? secondary?.NumberingSystemDigits(system);

    /// <inheritdoc/>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 composite order; IP=Low; Security=Low; Resources=1; Fingerprint=3DD4C0
    // Broiler-Human:        PENDING
    public IReadOnlyList<PluralRuleText>? PluralRules(string language, PluralRuleType type) =>
        Resolve(type == PluralRuleType.Ordinal ? IntlTable.Ordinals : IntlTable.Plurals, language, (data, tag) => data.PluralRules(tag, type));

    /// <inheritdoc/>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 composite order; IP=Low; Security=Low; Resources=1; Fingerprint=804520
    // Broiler-Human:        PENDING
    public string? PluralRange(string language, string start, string end) =>
        Resolve(IntlTable.PluralRanges, language, (data, tag) => data.PluralRange(tag, start, end));

    /// <inheritdoc/>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 composite order; IP=Low; Security=Low; Resources=1; Fingerprint=0700B2
    // Broiler-Human:        PENDING
    public string? UnitPattern(string locale, string width, string unit, string field) =>
        Resolve(IntlTable.Units, locale, (data, tag) => data.UnitPattern(tag, width, unit, field));

    /// <inheritdoc/>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 composite order; IP=Low; Security=Low; Resources=1; Fingerprint=0A2CC6
    // Broiler-Human:        PENDING
    public ListPatterns? ListPattern(string locale, string type) =>
        ResolveValue(IntlTable.ListPatterns, locale, (data, tag) => data.ListPattern(tag, type));

    /// <inheritdoc/>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 composite order; IP=Low; Security=Low; Resources=1; Fingerprint=9CA788
    // Broiler-Human:        PENDING
    public string? RelativeTime(string locale, string field, string key) =>
        Resolve(IntlTable.RelativeTimes, locale, (data, tag) => data.RelativeTime(tag, field, key));

    /// <inheritdoc/>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 composite order; IP=Low; Security=Low; Resources=1; Fingerprint=BB9D4E
    // Broiler-Human:        PENDING
    public string? DisplayName(string locale, string kind, string code) =>
        Resolve(IntlTable.DisplayNames, locale, (data, tag) => data.DisplayName(tag, kind, code));

    /// <inheritdoc/>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 composite order; IP=Low; Security=Low; Resources=1; Fingerprint=337D68
    // Broiler-Human:        PENDING
    public string? DateValue(string locale, string calendar, string key) =>
        Resolve(IntlTable.DateLocales, locale, (data, tag) => data.DateValue(tag, calendar, key));

    /// <inheritdoc/>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 composite order; IP=Low; Security=Low; Resources=1; Fingerprint=99CC00
    // Broiler-Human:        PENDING
    public IReadOnlyDictionary<string, string>? DateValues(string locale, string calendar) =>
        Resolve(IntlTable.DateLocales, locale, (data, tag) => data.DateValues(tag, calendar));

    /// <inheritdoc/>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 composite order; IP=Low; Security=Low; Resources=1; Fingerprint=1F2D95
    // Broiler-Human:        PENDING
    public IReadOnlyList<DayPeriodRule>? DayPeriods(string language) =>
        Resolve(IntlTable.DayPeriods, language, (data, tag) => data.DayPeriods(tag));

    /// <summary>A question about a locale whose answer is a reference, asked along the chain.</summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 composite order; IP=Low; Security=High; Resources=1; Fingerprint=3E0643
    // Broiler-Falsified-If: a question about a locale is answered by the secondary provider or by a parent for a tag whose table the shared data holds, or the secondary provider is asked before the shared data at a tag
    // Broiler-Human:        PENDING
    private T? Resolve<T>(IntlTable table, string locale, Func<ILocaleData, string, T?> question)
        where T : class
    {
        foreach (var tag in Chain(locale))
        {
            if (question(shared, tag) is { } answer)
            {
                return answer;
            }

            if (shared.HoldsLocale(table, tag))
            {
                return null;
            }

            if (secondary is not null && question(secondary, tag) is { } secondaryAnswer)
            {
                return secondaryAnswer;
            }
        }

        return null;
    }

    /// <summary>A question about a locale whose answer is a value, asked along the chain.</summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 composite order; IP=Low; Security=High; Resources=1; Fingerprint=794AA7
    // Broiler-Falsified-If: a question about a locale is answered by the secondary provider or by a parent for a tag whose table the shared data holds, or the secondary provider is asked before the shared data at a tag
    // Broiler-Human:        PENDING
    private T? ResolveValue<T>(IntlTable table, string locale, Func<ILocaleData, string, T?> question)
        where T : struct
    {
        foreach (var tag in Chain(locale))
        {
            if (question(shared, tag) is { } answer)
            {
                return answer;
            }

            if (shared.HoldsLocale(table, tag))
            {
                return null;
            }

            if (secondary is not null && question(secondary, tag) is { } secondaryAnswer)
            {
                return secondaryAnswer;
            }
        }

        return null;
    }
}
