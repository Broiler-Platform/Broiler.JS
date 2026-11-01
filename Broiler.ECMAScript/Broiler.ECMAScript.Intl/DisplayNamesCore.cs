// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Broiler.ECMAScript.Intl;

/// <summary>[[Type]] of an <c>Intl.DisplayNames</c>: what its codes name.</summary>
public enum DisplayNamesType
{
    /// <summary><c>language</c>: a <c>unicode_language_id</c>.</summary>
    Language = 0,

    /// <summary><c>region</c>: a <c>unicode_region_subtag</c>.</summary>
    Region = 1,

    /// <summary><c>script</c>: a <c>unicode_script_subtag</c>.</summary>
    Script = 2,

    /// <summary><c>currency</c>: an ISO 4217 code.</summary>
    Currency = 3,

    /// <summary><c>calendar</c>: a calendar type.</summary>
    Calendar = 4,

    /// <summary><c>dateTimeField</c>: one of Table 19's date-time fields.</summary>
    DateTimeField = 5,
}

/// <summary>[[Fallback]] of an <c>Intl.DisplayNames</c>: what <c>of</c> answers for a code with no name.</summary>
public enum DisplayNamesFallback
{
    /// <summary><c>code</c>: the canonical code itself.</summary>
    Code = 0,

    /// <summary><c>none</c>: <c>undefined</c>.</summary>
    None = 1,
}

/// <summary>[[LanguageDisplay]] of an <c>Intl.DisplayNames</c> of the language type.</summary>
public enum LanguageDisplay
{
    /// <summary><c>dialect</c>: a dialect's own name where CLDR has one, "British English".</summary>
    Dialect = 0,

    /// <summary><c>standard</c>: the language's name with its qualifiers, "English (United Kingdom)".</summary>
    Standard = 1,
}

/// <summary>
/// One configured <c>Intl.DisplayNames</c>' naming: CanonicalCodeForDisplayNames (<c>sec-canonicalcodefordisplaynames</c>)
/// and the names [[Fields]] holds, from CLDR's display names for the format's locale (Broiler.VM's JSD-0082, sub-area (c),
/// rows S8 to S10).
/// </summary>
/// <remarks>
/// <para>
/// <b>[[Fields]] holds a name for every code whose every part CLDR names</b> (row S8, an X1 adoption: Broiler.JS names
/// nothing today, and takes this naming). A language's name is composed as ICU's LocaleDisplayNames composes it: a dialect name
/// for the whole code where the data has one (<c>en-GB</c> is "British English"), and otherwise the language's name with its
/// script, region and variants in CLDR's locale pattern. A code with a part CLDR does not name has no field, so <c>of</c>
/// answers the code itself or <c>undefined</c>, as the fallback says.
/// </para>
/// <para>
/// <b>The canonical code's case is regularized by ASCII casing</b> (row S9; decision D19): a region upper case, a script's
/// first letter upper and the rest lower, a calendar lower, a currency upper, and a language canonicalized as a
/// <c>unicode_language_id</c>.
/// </para>
/// <para>
/// <b>Ported from Broiler.VM's <c>JsRealm.NewDisplayNames</c>' record, <c>CanonicalCodeForDisplayNames</c>, <c>DisplayName</c> and
/// <c>LanguageName</c></b> at its SJL-9 rule commit <c>4cf9ca44</c>, with ASCII casing where it called <c>ToUpperInvariant</c>
/// and <c>ToLowerInvariant</c> (decision D19), a status where it threw, and the caller's <see cref="WorkCharge"/> on the language
/// code's parse and canonicalisation, in <see cref="LocaleId"/>'s units, where Broiler.VM charged nothing (row L43), so that its
/// adapter passes a charge that charges nothing there (decision D18). The names are read at the data locale, which Broiler.VM
/// read at its language; the option reads are the engine's.
/// </para>
/// <para><b>Immutable</b>: one may be shared between threads (convention 10).</para>
/// </remarks>
public sealed class DisplayNamesCore
{
    /// <summary>The date-time field codes of Table 19 (<c>sec-isvaliddatetimefieldcode</c>).</summary>
    private static readonly ImmutableArray<string> DateTimeFieldCodes = ["era", "year", "quarter", "month", "weekOfYear", "weekday", "day", "dayPeriod", "hour", "minute", "second", "timeZoneName"];

    private readonly ILocaleData data;

    private readonly string dataLocale;

    private DisplayNamesCore(ILocaleData data, string locale, string dataLocale, FormatStyle style, DisplayNamesType type, DisplayNamesFallback fallback, LanguageDisplay? languageDisplay)
    {
        this.data = data;
        this.dataLocale = dataLocale;
        Locale = locale;
        Style = style;
        Type = type;
        Fallback = fallback;
        LanguageDisplay = languageDisplay;
    }

    /// <summary>[[Locale]].</summary>
    public string Locale { get; }

    /// <summary>[[Style]].</summary>
    public FormatStyle Style { get; }

    /// <summary>[[Type]].</summary>
    public DisplayNamesType Type { get; }

    /// <summary>[[Fallback]].</summary>
    public DisplayNamesFallback Fallback { get; }

    /// <summary>[[LanguageDisplay]] for the language type; <see langword="null"/> for every other.</summary>
    public LanguageDisplay? LanguageDisplay { get; }

    /// <summary>The record an <c>Intl.DisplayNames</c> constructor makes from what it resolved (<c>sec-Intl.DisplayNames</c>).</summary>
    /// <param name="data">The locale data, ordinarily a <see cref="CompositeLocaleData"/>.</param>
    /// <param name="locale">ResolveLocale's [[Locale]].</param>
    /// <param name="dataLocale">ResolveLocale's [[DataLocale]], at which the names are read.</param>
    /// <param name="style">The <c>style</c> option.</param>
    /// <param name="type">The <c>type</c> option, which the constructor requires.</param>
    /// <param name="fallback">The <c>fallback</c> option.</param>
    /// <param name="languageDisplay">The <c>languageDisplay</c> option, kept for the language type alone.</param>
    /// <returns>The record.</returns>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    public static DisplayNamesCore Create(ILocaleData data, string locale, string dataLocale, FormatStyle style, DisplayNamesType type, DisplayNamesFallback fallback, LanguageDisplay languageDisplay)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(locale);
        ArgumentNullException.ThrowIfNull(dataLocale);
        return new DisplayNamesCore(data, locale, dataLocale, style, type, fallback, type == DisplayNamesType.Language ? languageDisplay : null);
    }

    /// <summary>
    /// <c>Intl.DisplayNames.prototype.of</c>'s steps after ToString of the code (<c>sec-Intl.DisplayNames.prototype.of</c>): the
    /// canonical code, then the name [[Fields]] holds for it, or the code under the <c>code</c> fallback, or no name.
    /// </summary>
    /// <param name="code">ToString of the argument.</param>
    /// <param name="name">The name; the canonical code under the <c>code</c> fallback; <see langword="null"/> for <c>undefined</c>.</param>
    /// <param name="charge">The caller's meter, for a language code's parse and canonicalisation (<see cref="CanonicalCode"/>).</param>
    /// <returns><see cref="IntlStatus.Completed"/>, or <see cref="IntlStatus.InvalidDisplayNamesCode"/>, a <c>RangeError</c>.</returns>
    public IntlStatus Of(string code, out string? name, WorkCharge? charge = null)
    {
        var status = CanonicalCode(code, out var canonical, charge);

        if (status != IntlStatus.Completed)
        {
            name = null;
            return status;
        }

        name = DisplayName(canonical) ?? (Fallback == DisplayNamesFallback.Code ? canonical : null);
        return IntlStatus.Completed;
    }

    /// <summary>
    /// CanonicalCodeForDisplayNames (<c>sec-canonicalcodefordisplaynames</c>): a well-formed code of the type, its case
    /// regularized by ASCII casing.
    /// </summary>
    /// <param name="code">The code.</param>
    /// <param name="canonical">The canonical code, or the empty string.</param>
    /// <param name="charge">The caller's meter: a language code's parse and canonicalisation, one unit per code unit and per
    /// alias look-up, as <see cref="LocaleId"/> charges them; no other type charges.</param>
    /// <returns><see cref="IntlStatus.Completed"/>, or <see cref="IntlStatus.InvalidDisplayNamesCode"/>, a <c>RangeError</c>.</returns>
    public IntlStatus CanonicalCode(string code, out string canonical, WorkCharge? charge = null)
    {
        ArgumentNullException.ThrowIfNull(code);
        canonical = string.Empty;

        static bool Letters(string text) => text.Length > 0 && LocaleTag.IsAlpha(text);

        static bool Digits(string text)
        {
            foreach (var c in text)
            {
                if (!char.IsAsciiDigit(c))
                {
                    return false;
                }
            }

            return text.Length > 0;
        }

        switch (Type)
        {
            case DisplayNamesType.Language:
                {
                    // UNICODE_LANGUAGE_ID IN ITS BCP 47 FORM: no extension, no private use, no underscore.
                    if (code.Contains('_', StringComparison.Ordinal) ||
                        LocaleId.TryParse(code, out var tag, charge) != IntlStatus.Completed ||
                        tag!.BaseName.Length != code.Length)
                    {
                        return IntlStatus.InvalidDisplayNamesCode;
                    }

                    canonical = tag.Canonicalize(data, charge).ToString();
                    return IntlStatus.Completed;
                }

            case DisplayNamesType.Region:
                if (!(code.Length == 2 && Letters(code)) && !(code.Length == 3 && Digits(code)))
                {
                    return IntlStatus.InvalidDisplayNamesCode;
                }

                canonical = LocaleTag.AsciiUpper(code);
                return IntlStatus.Completed;

            case DisplayNamesType.Script:
                if (code.Length != 4 || !Letters(code))
                {
                    return IntlStatus.InvalidDisplayNamesCode;
                }

                canonical = LocaleTag.AsciiUpper(code[..1]) + LocaleTag.AsciiLower(code[1..]);
                return IntlStatus.Completed;

            case DisplayNamesType.Calendar:
                if (!LocaleTag.IsTypeSequence(code) || code.Contains('_', StringComparison.Ordinal))
                {
                    return IntlStatus.InvalidDisplayNamesCode;
                }

                canonical = LocaleTag.AsciiLower(code);
                return IntlStatus.Completed;

            case DisplayNamesType.DateTimeField:
                if (DateTimeFieldCodes.IndexOf(code) < 0)
                {
                    return IntlStatus.InvalidDisplayNamesCode;
                }

                canonical = code;
                return IntlStatus.Completed;

            default:
                if (!NumberFormatter.IsWellFormedCurrencyCode(code))
                {
                    return IntlStatus.InvalidDisplayNamesCode;
                }

                canonical = LocaleTag.AsciiUpper(code);
                return IntlStatus.Completed;
        }
    }

    /// <summary>The name [[Fields]] holds for a canonical code, or <see langword="null"/> where it holds none.</summary>
    /// <param name="canonicalCode">A code <see cref="CanonicalCode"/> answered.</param>
    /// <returns>The name, or <see langword="null"/>.</returns>
    public string? DisplayName(string canonicalCode)
    {
        ArgumentNullException.ThrowIfNull(canonicalCode);
        var shortened = Style != FormatStyle.Long;

        string? Named(string kind, string key) =>
            (shortened ? data.DisplayName(dataLocale, kind, key + "-alt-short") : null) ?? data.DisplayName(dataLocale, kind, key);

        switch (Type)
        {
            case DisplayNamesType.Region:
                return Named("region", canonicalCode);

            case DisplayNamesType.Script:
                return data.DisplayName(dataLocale, "script", canonicalCode);

            case DisplayNamesType.Calendar:
                return data.DisplayName(dataLocale, "calendar", canonicalCode);

            case DisplayNamesType.DateTimeField:
                return data.DisplayName(dataLocale, "field", canonicalCode + "-" + StyleName(Style));

            case DisplayNamesType.Currency:
                return data.Currency(dataLocale, canonicalCode) is { Name.Length: > 0 } currency ? currency.Name : null;

            default:
                return LanguageName(canonicalCode, Named);
        }
    }

    private static string StyleName(FormatStyle style) => style switch
    {
        FormatStyle.Short => "short",
        FormatStyle.Narrow => "narrow",
        _ => "long",
    };

    /// <summary>
    /// A language code's name, composed as ICU's LocaleDisplayNames composes it: under the dialect display the whole code's own
    /// name where CLDR has one, then the language's name, and the script, region and variants it did not cover in CLDR's locale
    /// pattern. An unnamed part is its code under the code fallback, as ICU writes "xyz (Germany)", and makes the whole name
    /// nothing otherwise.
    /// </summary>
    private string? LanguageName(string code, Func<string, string, string?> named)
    {
        var tag = LocaleTag.Parse(code)!;
        var language = tag.Language;
        var script = tag.Script;
        var region = tag.Region;
        string? name = null;

        if (LanguageDisplay == Intl.LanguageDisplay.Dialect)
        {
            // ONE DIALECT KEY, THE WHOLE CODE'S: a script and a region together are never split.
            if (script is not null && region is not null)
            {
                if (named("language", language + "-" + script + "-" + region) is { } full)
                {
                    (name, script, region) = (full, null, null);
                }
            }
            else if (script is not null)
            {
                if (named("language", language + "-" + script) is { } withScript)
                {
                    (name, script) = (withScript, null);
                }
            }
            else if (region is not null && named("language", language + "-" + region) is { } withRegion)
            {
                (name, region) = (withRegion, null);
            }
        }

        var substitute = Fallback == DisplayNamesFallback.Code;
        name ??= named("language", language) ?? (substitute ? language : null);

        if (name is null)
        {
            return null;
        }

        var details = new List<string>();

        if (script is not null)
        {
            if (data.DisplayName(dataLocale, "script", script) is not { } scriptName)
            {
                if (!substitute)
                {
                    return null;
                }

                scriptName = script;
            }

            details.Add(scriptName);
        }

        if (region is not null)
        {
            if (named("region", region) is not { } regionName)
            {
                if (!substitute)
                {
                    return null;
                }

                regionName = region;
            }

            details.Add(regionName);
        }

        foreach (var variant in tag.Variants)
        {
            var upper = LocaleTag.AsciiUpper(variant);

            if (data.DisplayName(dataLocale, "variant", upper) is not { } variantName)
            {
                if (!substitute)
                {
                    return null;
                }

                variantName = upper;
            }

            details.Add(variantName);
        }

        if (details.Count == 0)
        {
            return name;
        }

        var separator = data.DisplayName(dataLocale, "pattern", "localeSeparator") ?? "{0}, {1}";
        var pattern = data.DisplayName(dataLocale, "pattern", "localePattern") ?? "{0} ({1})";
        var joined = details[0];

        for (var i = 1; i < details.Count; i++)
        {
            joined = separator.Replace("{0}", joined, StringComparison.Ordinal).Replace("{1}", details[i], StringComparison.Ordinal);
        }

        return pattern.Replace("{0}", name, StringComparison.Ordinal).Replace("{1}", joined, StringComparison.Ordinal);
    }
}
