using System;
using System.Collections.Generic;
using System.Globalization;

namespace Broiler.JavaScript.BuiltIns.Intl;

/// <summary>
/// Broiler.JS's optional host collation provider (milestone SJL-17.5; Broiler.VM's JSD-0084, decision E3, behind the
/// same provider as decision SJL-D-22 (c)): the host's <see cref="CompareInfo"/> for what the shared collator does not
/// cover, which is every locale outside decision E3's list (CJK among them), and a collation the shared data does not
/// carry for a covered locale (<c>eor</c> and <c>emoji</c>, which the host resolves for every locale), or for a region or
/// variant of one in a CLDR file of its own (<c>de-AT</c>'s phonebook and <c>en-US-posix</c>'s standard, added on
/// 2026-11-16 at the review of SJL-17.5, where the covered language's tailoring compared them). It is never a second
/// implementation of the algorithm: it hands both strings, after Broiler.Unicode's NFD, to the host.
/// </summary>
/// <remarks>
/// <para>
/// <b>It answers nothing where the host has no collation data.</b> Under invariant globalization the host lists no
/// culture (<see cref="HostCultureLocaleData.Locales"/> is empty) and its <see cref="CompareInfo"/> compares by code unit,
/// so <see cref="HasData"/> is <see langword="false"/> and <see cref="CompareInfoFor"/> answers <see langword="null"/>, and
/// the Collator compares by the shared collator instead (decision E4, row O14): no comparison falls back to code-unit
/// order. A culture the host does not know answers <see langword="null"/> too, where the Collator compared by the
/// invariant culture's <see cref="CompareInfo"/> before SJL-17.5.
/// </para>
/// <para>
/// <b>What moved here at SJL-17.5</b> (decision E11, X6): <c>NetCollationSuffix</c>, from <c>JSIntlCollator</c>, as the
/// deletion list says; and, for the locales and collations this provider serves and only for them, the host's comparison
/// that was <c>JSIntlCollator</c>'s <c>ResolveCompareInfo</c> and its <c>CompareOptions</c> mapping in <c>Compare</c>.
/// For the covered locales both are deleted: the shared <c>UcaCollator</c> compares them. <c>StripIgnorablePunctuation</c>'s
/// tie-break is deleted for every locale (row O19): Thai, the one collation input reached it through, is covered.
/// </para>
/// <para>
/// <b>Its collations</b> are the ones the Collator resolved from the host before SJL-17.5: <c>emoji</c> and <c>eor</c> for
/// every locale, and the tailorings .NET exposes by name (<see cref="NetCollationSuffix"/>). As before, <c>emoji</c> and
/// <c>eor</c> are resolved and reported but the host applies neither: their comparison is the locale's default
/// <see cref="CompareInfo"/>.
/// </para>
/// </remarks>
internal static class HostCollationProvider
{
    // The collation types this provider resolves for some locale: emoji and eor everywhere, and NetCollationSuffix's.
    internal static readonly string[] Types = ["emoji", "eor", "phonebk", "phonetic", "pinyin", "stroke", "trad", "unihan", "zhuyin"];

    /// <summary>
    /// Whether the host has collation data: it lists cultures, which it does when ICU is loaded and lists none under
    /// invariant globalization (<see cref="HostCultureLocaleData.Locales"/>).
    /// </summary>
    internal static bool HasData => HostCultureLocaleData.Host.Locales.Count != 0;

    /// <summary>Whether the provider resolves <paramref name="collation"/> for a locale of <paramref name="language"/>.</summary>
    /// <param name="language">The resolved locale's language subtag.</param>
    /// <param name="collation">A canonical collation type, never <c>standard</c> or <c>search</c>.</param>
    internal static bool Resolves(string language, string collation)
        => collation is "emoji" or "eor" || NetCollationSuffix(language, collation) != null;

    // Kept on the host's culture: the optional host collation provider for the locales and collations the shared data
    // does not cover (JSD-0084, decision E3). It names the culture it reads, which the five globalization rules accept; the
    // banned-API rule (Broiler.JS/BannedSymbols.HostCulture.txt) bans a culture built from the host's data everywhere but
    // the kept paths, and is suppressed for this method for that reason. Until SJL-17.5 this was JSIntlCollator's
    // ResolveCompareInfo, for every locale.
    /// <summary>
    /// The host's comparison for a locale, its usage and its resolved collation, or <see langword="null"/> when the host
    /// has no collation data or no culture for the locale.
    /// </summary>
    /// <param name="locale">The resolved locale's data locale, with no <c>-u-</c> extension.</param>
    /// <param name="collation">The resolved collation; <c>default</c> for the locale's own.</param>
    /// <param name="usage"><c>sort</c> or <c>search</c>.</param>
#pragma warning disable RS0030 // Kept: the optional host collation provider reads the host's collation data for what the shared data does not cover (JSD-0084, E3).
    internal static CompareInfo? CompareInfoFor(string locale, string collation, string usage)
    {
        if (!HasData || string.IsNullOrEmpty(locale))
            return null;

        // usage:"search" selects the locale's ICU "search" collation, which tailors comparison for string matching rather
        // than sorting. ECMA-402 never reflects "search" as the resolved collation, so it is applied here independently of
        // the -u-co- option.
        if (usage == "search")
        {
            try
            {
                return CultureInfo.GetCultureInfo(locale + "-u-co-search").CompareInfo;
            }
            catch (CultureNotFoundException) { /* fall through */ }
        }

        // .NET ships a small set of named collation variants through the BCP-47 culture-construction path:
        // GetCultureInfo("zh-u-co-pinyin").CompareInfo applies the tailoring (CompareInfo.GetCompareInfo with .NET's
        // suffix keeps the name but not the tailoring). An unrecognised one falls back to the locale's own collation.
        if (NetCollationSuffix(Language(locale), collation) != null)
        {
            try
            {
                return CultureInfo.GetCultureInfo(locale + "-u-co-" + collation).CompareInfo;
            }
            catch (CultureNotFoundException) { /* fall through */ }
        }

        try
        {
            return CultureInfo.GetCultureInfo(locale).CompareInfo;
        }
        catch (CultureNotFoundException)
        {
            return null;
        }
    }
#pragma warning restore RS0030

    /// <summary>
    /// The host's comparison of two strings already in NFD, under <c>sensitivity</c> and <c>ignorePunctuation</c> mapped onto
    /// <see cref="CompareOptions"/>, as <c>JSIntlCollator.Compare</c> mapped them for every locale until SJL-17.5. The
    /// mapping is the host's alone: the shared collator reads the options as its record (JSD-0084, X3).
    /// </summary>
    internal static int Compare(CompareInfo compareInfo, string left, string right, string sensitivity, bool ignorePunctuation)
    {
        var options = CompareOptions.None;
        if (sensitivity == "base")
            options |= CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;
        else if (sensitivity == "accent")
            options |= CompareOptions.IgnoreCase;
        else if (sensitivity == "case")
            options |= CompareOptions.IgnoreNonSpace;
        if (ignorePunctuation)
            options |= CompareOptions.IgnoreSymbols;

        return Math.Sign(compareInfo.Compare(left, right, options));
    }

    // .NET's CompareInfo names map the BCP-47 collation key to a string suffix in a small (language-specific) table.
    // Only the variants .NET actually exposes are mapped here. Moved from JSIntlCollator at SJL-17.5 (decision E11);
    // de phonebk is the shared collator's for de, a covered locale, and stays in the table as the host exposes it.
    private static string? NetCollationSuffix(string language, string collation)
    {
        if (string.IsNullOrEmpty(collation) || collation == "default" || collation == "standard")
            return null;
        return (language, collation) switch
        {
            ("de", "phonebk") => "phoneboo",
            ("zh", "pinyin") => "pinyin",
            ("zh", "stroke") => "stroke",
            ("zh", "phonetic") or ("zh", "zhuyin") => "pronun",
            ("zh", "trad") or ("zh", "unihan") => "radstr",
            ("ja", "unihan") => "radstr",
            ("ko", "unihan") => "korean",
            ("hu", "trad") => "techni",
            _ => null,
        };
    }

    // A tag's language subtag, lowered as ASCII.
    private static string Language(string tag)
    {
        var dash = tag.IndexOf('-');
        var language = dash < 0 ? tag : tag[..dash];
        return Broiler.ECMAScript.Intl.LocaleId.AsciiLowercase(language);
    }

    /// <summary>The provider's collation types for a language, beyond the shared data's, in <see cref="Types"/>' order.</summary>
    internal static IEnumerable<string> TypesFor(string language)
    {
        foreach (var type in Types)
        {
            if (Resolves(language, type))
                yield return type;
        }
    }
}
