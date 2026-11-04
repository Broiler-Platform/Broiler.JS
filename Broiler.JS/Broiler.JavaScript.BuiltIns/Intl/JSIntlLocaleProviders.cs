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

    // A culture name as a canonical Unicode locale identifier, or null when it is not a well-formed tag.
    private static string Canonical(string name) =>
        LocaleId.TryCanonicalize(name, JSIntlData.LocaleData, out var canonical) == IntlStatus.Completed ? canonical : null;
}
