using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
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
    // The host's cultures, each canonicalized once; read on first use.
    private static readonly Lazy<string[]> HostLocales = new(ReadHostLocales, LazyThreadSafetyMode.ExecutionAndPublication);

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
            var name = CultureInfo.CurrentCulture.Name;
            return string.IsNullOrEmpty(name)
                ? "en-US"
                : DefaultByCulture.GetOrAdd(name, static culture => Canonical(culture) ?? "en-US");
        }
    }

    /// <summary>
    /// Every service's [[AvailableLocales]]: the host's cultures by their canonical tags, with <see cref="DefaultLocale"/> and
    /// each less narrow tag of it the lookup reaches, in that order, each once.
    /// </summary>
    internal static IReadOnlyList<string> AvailableLocales
    {
        get
        {
            var defaultLocale = DefaultLocale;
            return AvailableByDefault.GetOrAdd(defaultLocale, static locale => WithDefault(HostLocales.Value, locale));
        }
    }

    /// <summary>The host's cultures by their canonical tags, in the order .NET lists them, each once; none under invariant globalization.</summary>
    internal static IReadOnlyList<string> HostCultureLocales => HostLocales.Value;

    // The host's list, then the default and the prefixes ECMA-402's lookup truncates it to, those the list lacks.
    private static IReadOnlyList<string> WithDefault(string[] host, string defaultLocale)
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

    // The host's cultures by canonical tag, each followed by the tags that name the same culture without the script its
    // likely subtags supply: .NET lists zh-Hant-TW and sr-Cyrl-RS but not zh-TW or sr-RS, which it resolves to them, so
    // those are the host's too (each tag X with Add Likely Subtags(X) = Add Likely Subtags(culture)): Remove Likely
    // Subtags of the culture, and its language and region alone.
    private static string[] ReadHostLocales()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var list = new List<string>();
        var data = JSIntlData.LocaleData;
        foreach (var name in HostCultureLocaleData.Host.Locales)
        {
            if (Canonical(name) is not { } tag || !seen.Add(tag))
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
                if (!seen.Contains(alias) && LocaleId.TryParse(alias, out var aliasId) == IntlStatus.Completed
                    && aliasId.Maximize(data, out _).ToString() == maximal && seen.Add(alias))
                    list.Add(alias);
            }
        }

        return [.. list];
    }

    // A culture name as a canonical Unicode locale identifier, or null when it is not a well-formed tag.
    private static string Canonical(string name) =>
        LocaleId.TryCanonicalize(name, JSIntlData.LocaleData, out var canonical) == IntlStatus.Completed ? canonical : null;
}
