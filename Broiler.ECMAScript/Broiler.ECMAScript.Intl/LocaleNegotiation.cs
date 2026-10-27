// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   12
// Annotated:        12/12
// Exempt:           3
// Human-reviewed:   0/12
// IP risk:          Low
// Security risk:    High
// Criteria:         2/2
// Resource impact:  2/10 max
// Unverified:       12
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Generic;

namespace Broiler.ECMAScript.Intl;

/// <summary>
/// ECMA-402's locale negotiation (<c>negotiation.html</c>): CanonicalizeLocaleList's String part, the lookup and best-fit
/// matchers, FilterLocales and ResolveLocale, over the available locales and the default locale an engine's providers give
/// (Broiler.VM's JSD-0082, sub-area (a), rows L4 and L28 to L38; decision D7).
/// </summary>
/// <remarks>
/// <para>
/// <b>Ported from Broiler.VM's <c>JsRealm.Intl</c></b> at its SJL-9 rule commit <c>4cf9ca44</c>: <c>CanonicalizeLocaleList</c>'s
/// work on each element's String, <c>LookupMatchingLocale</c>, <c>FilterLocales</c> and <c>ResolveLocale</c>, in the same
/// steps. What reads a script value stays in the engine (convention 2): the walk over the locales argument, each element's
/// type check and ToString, the options object and its <c>localeMatcher</c>, and every option the constructors read. The
/// best-fit matcher is the lookup matcher, which ECMA-402 admits as a best fit (row L38). An option's value is lowered by
/// ASCII casing where Broiler.VM's source used the invariant culture (decision D19; row L42).
/// </para>
/// <para>
/// <b>The locales the engine gives are its providers'</b> (decision D7): the available locales, which always hold the default
/// locale, and the default locale. Each must be a well-formed, canonical tag; a resolution that needs to parse one that is
/// not throws <see cref="ArgumentException"/>.
/// </para>
/// <para>
/// <b>Metering</b> (decision D18). CanonicalizeLocaleList charges each tag's length, as <see cref="LocaleId.TryCanonicalize"/>
/// does; the matchers and FilterLocales charge one unit per requested locale they examine. Broiler.VM charges one unit per
/// element in its adapter, where it walks the script value (<c>JsRealm.Intl.cs:282</c> and <c>:550</c>), and passes no meter
/// here; Broiler.JS passes none.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=Ported; Spec=ECMA-402 negotiation.html via Broiler.VM JsRealm.Intl; IP=Low; Security=Medium; Resources=0; Fingerprint=21832C
// Broiler-Human:        PENDING
public static class LocaleNegotiation
{
    /// <summary>
    /// CanonicalizeLocaleList's work on the Strings an engine read from its argument (<c>sec-canonicalizelocalelist</c>, steps
    /// 7.c.v to 7.c.viii): each tag held to IsWellFormedLanguageTag, canonicalised, and kept once, in the order of first
    /// occurrence.
    /// </summary>
    /// <param name="tags">The elements' Strings, in the argument's order.</param>
    /// <param name="data">The locale data canonicalisation reads.</param>
    /// <param name="locales">The canonical tags, each once, or an empty list when one is not well-formed.</param>
    /// <param name="failedAt">The index of the first tag that is not well-formed, or -1.</param>
    /// <param name="charge">The caller's meter: each tag's length in code units, before it is parsed.</param>
    /// <returns><see cref="IntlStatus.Completed"/>, or <see cref="IntlStatus.InvalidLanguageTag"/>, a <c>RangeError</c>.</returns>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 negotiation.html via Broiler.VM JsRealm.Intl; IP=Low; Security=Medium; Resources=2; Fingerprint=E00DDD
    // Broiler-Human:        PENDING
    public static IntlStatus CanonicalizeLocaleList(IReadOnlyList<string> tags, ILocaleData data, out IReadOnlyList<string> locales, out int failedAt, WorkCharge? charge = null)
    {
        ArgumentNullException.ThrowIfNull(tags);
        ArgumentNullException.ThrowIfNull(data);

        var seen = new List<string>();

        for (var index = 0; index < tags.Count; index++)
        {
            var status = LocaleId.TryCanonicalize(tags[index], data, out var canonical, charge);

            if (status != IntlStatus.Completed)
            {
                locales = [];
                failedAt = index;
                return status;
            }

            if (!seen.Contains(canonical))
            {
                seen.Add(canonical);
            }
        }

        locales = seen.AsReadOnly();
        failedAt = -1;
        return IntlStatus.Completed;
    }

    /// <summary>
    /// ECMA-402's LookupMatchingLocaleByPrefix (<c>sec-lookupmatchinglocalebyprefix</c>): for each requested locale, its
    /// <c>-u-</c> extension set aside, the longest available prefix, truncated a subtag (and a singleton before it) at a time.
    /// </summary>
    /// <param name="availableLocales">The service's available locales.</param>
    /// <param name="requestedLocales">The requested locales, canonical, in order.</param>
    /// <param name="charge">The caller's meter: one unit per requested locale examined.</param>
    /// <returns>The available locale matched and the requested locale's extension keywords, or <see langword="null"/>.</returns>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 negotiation.html via Broiler.VM JsRealm.Intl; IP=Low; Security=Low; Resources=1; Fingerprint=A4627D
    // Broiler-Human:        PENDING
    public static LocaleMatch? LookupMatchingLocaleByPrefix(IReadOnlyList<string> availableLocales, IReadOnlyList<string> requestedLocales, WorkCharge? charge = null)
    {
        ArgumentNullException.ThrowIfNull(availableLocales);
        ArgumentNullException.ThrowIfNull(requestedLocales);

        foreach (var locale in requestedLocales)
        {
            charge?.Invoke(1);

            if (Lookup(availableLocales, locale) is { } match)
            {
                return match;
            }
        }

        return null;
    }

    /// <summary>
    /// ECMA-402's LookupMatchingLocaleByBestFit (<c>sec-lookupmatchinglocalebybestfit</c>), which this library answers by the
    /// lookup matcher, a conforming best fit (row L38, where both engines do so).
    /// </summary>
    /// <param name="availableLocales">The service's available locales.</param>
    /// <param name="requestedLocales">The requested locales, canonical, in order.</param>
    /// <param name="charge">The caller's meter: one unit per requested locale examined.</param>
    public static LocaleMatch? LookupMatchingLocaleByBestFit(IReadOnlyList<string> availableLocales, IReadOnlyList<string> requestedLocales, WorkCharge? charge = null) =>
        LookupMatchingLocaleByPrefix(availableLocales, requestedLocales, charge);

    /// <summary>
    /// ECMA-402's FilterLocales (<c>sec-filterlocales</c>) after the engine has read <c>localeMatcher</c>: the requested
    /// locales a prefix of which is available, as requested and in order (row L36). Either matcher is the lookup matcher.
    /// </summary>
    /// <param name="availableLocales">The service's available locales.</param>
    /// <param name="requestedLocales">The requested locales, canonical, in order.</param>
    /// <param name="charge">The caller's meter: one unit per requested locale, before it is matched.</param>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 negotiation.html via Broiler.VM JsRealm.Intl; IP=Low; Security=Low; Resources=1; Fingerprint=AC037E
    // Broiler-Human:        PENDING
    public static IReadOnlyList<string> FilterLocales(IReadOnlyList<string> availableLocales, IReadOnlyList<string> requestedLocales, WorkCharge? charge = null)
    {
        ArgumentNullException.ThrowIfNull(availableLocales);
        ArgumentNullException.ThrowIfNull(requestedLocales);

        var subset = new List<string>();

        foreach (var locale in requestedLocales)
        {
            charge?.Invoke(1);

            if (Lookup(availableLocales, locale) is not null)
            {
                subset.Add(locale);
            }
        }

        return subset.AsReadOnly();
    }

    /// <summary>
    /// ResolveLocale's matching steps (<c>sec-resolvelocale</c>, steps 1 to 4): the best-fit match of the requested locales,
    /// or the default locale with no extension when nothing matches. The engine reads its service's locale data for the
    /// matched locale, and hands it to <see cref="ResolveLocale(LocaleMatch, IReadOnlyList{RelevantKey}, ILocaleData)"/>.
    /// </summary>
    /// <param name="availableLocales">The service's available locales.</param>
    /// <param name="requestedLocales">The requested locales, canonical, in order.</param>
    /// <param name="defaultLocale">The default locale (ECMA-402 DefaultLocale), from the engine's provider.</param>
    /// <param name="charge">The caller's meter: one unit per requested locale examined.</param>
    // Broiler-AI:           Origin=AI; Spec=ECMA-402 negotiation.html via Broiler.VM JsRealm.Intl; IP=Low; Security=Low; Resources=1; Fingerprint=92B361
    // Broiler-Human:        PENDING
    public static LocaleMatch MatchLocale(IReadOnlyList<string> availableLocales, IReadOnlyList<string> requestedLocales, string defaultLocale, WorkCharge? charge = null)
    {
        ArgumentNullException.ThrowIfNull(defaultLocale);
        return LookupMatchingLocaleByBestFit(availableLocales, requestedLocales, charge) ?? new LocaleMatch(defaultLocale, []);
    }

    /// <summary>
    /// ResolveLocale's steps after the match (<c>sec-resolvelocale</c>, steps 5 to 12): for each relevant key, the locale
    /// data's default value, the requested keyword's value where the data supports it, and an option's value over it; the
    /// supported keywords kept in the resolved locale, canonicalised (rows L29 to L31).
    /// </summary>
    /// <param name="match">The match <see cref="MatchLocale"/> answered.</param>
    /// <param name="relevantKeys">The service's relevant extension keys in its order, each with the locale data's values for the matched locale and the option, if any.</param>
    /// <param name="data">The locale data whose BCP 47 types canonicalise an option's value and the resolved locale.</param>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 negotiation.html via Broiler.VM JsRealm.Intl; IP=Low; Security=High; Resources=2; Fingerprint=71E8F5
    // Broiler-Falsified-If: a resolved locale names a keyword the locale data does not support, an option the data supports is not honoured, or a key's value differs from what ECMA-402's ResolveLocale gives for the same data
    // Broiler-Human:        PENDING
    public static ResolvedLocale ResolveLocale(LocaleMatch match, IReadOnlyList<RelevantKey> relevantKeys, ILocaleData data)
    {
        ArgumentNullException.ThrowIfNull(match);
        ArgumentNullException.ThrowIfNull(relevantKeys);
        ArgumentNullException.ThrowIfNull(data);

        var found = match.Locale;
        var keywords = match.ExtensionKeywords;
        var supported = new List<(string Key, string Type)>();
        var result = new Dictionary<string, string?>(StringComparer.Ordinal);

        foreach (var relevant in relevantKeys)
        {
            var key = relevant.Key;
            var values = relevant.Values;

            if (values.Count == 0)
            {
                throw new ArgumentException("the locale data gives no value for the key " + key, nameof(relevantKeys));
            }

            var value = values[0];
            (string Key, string Type)? keyword = null;

            var entry = -1;

            for (var i = 0; i < keywords.Count; i++)
            {
                if (keywords[i].Key == key)
                {
                    entry = i;
                    break;
                }
            }

            if (entry >= 0)
            {
                var requestedValue = keywords[entry].Value;

                if (requestedValue.Length != 0)
                {
                    if (Contains(values, requestedValue))
                    {
                        value = requestedValue;
                        keyword = (key, value);
                    }
                }
                else if (Contains(values, "true"))
                {
                    value = "true";
                    keyword = (key, string.Empty);
                }
            }

            if (relevant.OptionIsNull)
            {
                if (value is not null && Contains(values, null))
                {
                    value = null;
                    keyword = null;
                }
            }
            else if (relevant.Option is { } optionValue)
            {
                // D19: ASCII-lowercase, where Broiler.VM's source lowered by the invariant culture (row L42).
                var canonical = LocaleTag.AsciiLower(optionValue);

                if (data.TryExtensionType('u', key, canonical, out var preferred))
                {
                    canonical = preferred;
                }

                if (canonical.Length == 0)
                {
                    canonical = "true";
                }

                if (!string.Equals(canonical, value, StringComparison.Ordinal) && Contains(values, canonical))
                {
                    value = canonical;
                    keyword = null;
                }
            }

            if (keyword is { } kept)
            {
                supported.Add(kept);
            }

            result[key] = value;
        }

        var locale = found;

        if (supported.Count != 0)
        {
            var tag = LocaleTag.Parse(found) ?? throw new ArgumentException("the matched locale " + found + " is not a well-formed tag", nameof(match));
            tag.HasUnicodeExtension = true;
            tag.Keywords.AddRange(supported);
            locale = tag.Canonicalize(data).ToString();
        }

        return new ResolvedLocale(locale, found, result.AsReadOnly());
    }

    /// <summary>
    /// ECMA-402's ResolveLocale (<c>sec-resolvelocale</c>) where the service's locale data is the same for every locale:
    /// <see cref="MatchLocale"/>, then <see cref="ResolveLocale(LocaleMatch, IReadOnlyList{RelevantKey}, ILocaleData)"/>.
    /// </summary>
    /// <param name="availableLocales">The service's available locales.</param>
    /// <param name="requestedLocales">The requested locales, canonical, in order.</param>
    /// <param name="defaultLocale">The default locale, from the engine's provider.</param>
    /// <param name="relevantKeys">The service's relevant extension keys, with their values and options.</param>
    /// <param name="data">The locale data.</param>
    /// <param name="charge">The caller's meter: one unit per requested locale examined.</param>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 negotiation.html via Broiler.VM JsRealm.Intl; IP=Low; Security=High; Resources=2; Fingerprint=1FBC7D
    // Broiler-Falsified-If: a resolved locale names a keyword the locale data does not support, an option the data supports is not honoured, or a key's value differs from what ECMA-402's ResolveLocale gives for the same data
    // Broiler-Human:        PENDING
    public static ResolvedLocale ResolveLocale(IReadOnlyList<string> availableLocales, IReadOnlyList<string> requestedLocales, string defaultLocale, IReadOnlyList<RelevantKey> relevantKeys, ILocaleData data, WorkCharge? charge = null) =>
        ResolveLocale(MatchLocale(availableLocales, requestedLocales, defaultLocale, charge), relevantKeys, data);

    /// <summary>The lookup of one requested locale: its longest available prefix, the <c>-u-</c> extension set aside.</summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 negotiation.html via Broiler.VM JsRealm.Intl; IP=Low; Security=Low; Resources=1; Fingerprint=4EA916
    // Broiler-Human:        PENDING
    internal static LocaleMatch? Lookup(IReadOnlyList<string> available, string locale)
    {
        var tag = LocaleTag.Parse(locale);

        if (tag is null)
        {
            return null;
        }

        var extension = tag.HasUnicodeExtension ? tag.Keywords.ConvertAll(static k => new LocaleKeyword(k.Key, k.Type)) : [];
        var prefix = tag.WithoutUnicodeExtension();

        while (prefix.Length != 0)
        {
            if (Contains(available, prefix))
            {
                return new LocaleMatch(prefix, extension.AsReadOnly());
            }

            var dash = prefix.LastIndexOf('-');

            if (dash < 0)
            {
                break;
            }

            prefix = prefix[..dash];

            if (prefix.Length >= 2 && prefix[^2] == '-')
            {
                prefix = prefix[..^2];
            }
        }

        return null;
    }

    // Broiler-AI:           Origin=AI; Spec=ECMA-402 negotiation.html via Broiler.VM JsRealm.Intl; IP=Low; Security=Low; Resources=1; Fingerprint=3A3FFC
    // Broiler-Human:        PENDING
    private static bool Contains(IReadOnlyList<string?> values, string? value)
    {
        foreach (var candidate in values)
        {
            if (string.Equals(candidate, value, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}

/// <summary>What ResolveLocale's matching steps found: the available locale, and the requested locale's <c>-u-</c> keywords.</summary>
/// <param name="Locale">The available locale matched, or the default locale when nothing was.</param>
/// <param name="ExtensionKeywords">The <c>-u-</c> keywords of the requested locale that matched, in its order; empty when it had none or nothing matched.</param>
// Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-resolvelocale records; IP=Low; Security=Low; Resources=0; Fingerprint=5E0CF7
// Broiler-Human:        PENDING
public sealed record LocaleMatch(string Locale, IReadOnlyList<LocaleKeyword> ExtensionKeywords);

/// <summary>
/// One relevant extension key of a service, as ResolveLocale reads it (<c>sec-resolvelocale</c>): the key, the locale data's
/// values for the matched locale (its default first), and the option the engine read for it.
/// </summary>
/// <param name="Key">The key, for example <c>nu</c>.</param>
/// <param name="Values">The service's <c>[[LocaleData]]</c> values for the key in the matched locale, the default first; <see langword="null"/> stands for ECMA-402's <b>null</b>.</param>
// Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-resolvelocale records; IP=Low; Security=Low; Resources=0; Fingerprint=71A60C
// Broiler-Human:        PENDING
public sealed record RelevantKey(string Key, IReadOnlyList<string?> Values)
{
    /// <summary>The option's value as the engine read it, or <see langword="null"/> when the option is absent.</summary>
    public string? Option { get; init; }

    /// <summary>
    /// Whether the option is ECMA-402's <b>null</b> rather than absent, which drops a keyword's value where the data supports
    /// null (DateTimeFormat's <c>hc</c> when <c>hour12</c> is given).
    /// </summary>
    public bool OptionIsNull { get; init; }
}

/// <summary>What ResolveLocale answers (<c>sec-resolvelocale</c>): the resolved locale, the data locale, and each relevant key's value.</summary>
/// <param name="Locale">The resolved locale: the matched locale with the supported requested keywords.</param>
/// <param name="DataLocale">The matched locale, whose data the service reads.</param>
/// <param name="KeyValues">Each relevant key's value, by key; <see langword="null"/> stands for ECMA-402's <b>null</b>.</param>
// Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-resolvelocale records; IP=Low; Security=Low; Resources=0; Fingerprint=3322DB
// Broiler-Human:        PENDING
public sealed record ResolvedLocale(string Locale, string DataLocale, IReadOnlyDictionary<string, string?> KeyValues);
