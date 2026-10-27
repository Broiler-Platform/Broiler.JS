// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   12
// Annotated:        12/12
// Exempt:           13
// Human-reviewed:   0/12
// IP risk:          Low
// Security risk:    High
// Criteria:         1/1
// Resource impact:  2/10 max
// Unverified:       12
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Broiler.ECMAScript.Intl;

/// <summary>
/// An <c>Intl.Locale</c>'s internal slots (ECMA-402 <c>sec-intl.locale</c>, MakeLocaleRecord): its canonical locale
/// identifier and the values of its seven relevant extension keys, <c>ca</c>, <c>co</c>, <c>fw</c>, <c>hc</c>, <c>kf</c>,
/// <c>kn</c> and <c>nu</c>; and the constructor's steps after an engine has read the tag and the options (rows L13 and L14).
/// </summary>
/// <param name="Locale">[[Locale]]: the canonical locale identifier.</param>
/// <param name="Calendar">[[Calendar]], or <see langword="null"/>.</param>
/// <param name="Collation">[[Collation]], or <see langword="null"/>.</param>
/// <param name="FirstDayOfWeek">[[FirstDayOfWeek]], or <see langword="null"/>.</param>
/// <param name="HourCycle">[[HourCycle]], or <see langword="null"/>.</param>
/// <param name="CaseFirst">[[CaseFirst]], or <see langword="null"/>.</param>
/// <param name="Numeric">[[Numeric]].</param>
/// <param name="NumberingSystem">[[NumberingSystem]], or <see langword="null"/>.</param>
/// <remarks>
/// <para>
/// <b>Ported from Broiler.VM's <c>JsRealm.Locale</c></b> at its SJL-9 rule commit <c>4cf9ca44</c>: <c>NewLocaleObject</c>'s
/// steps, <c>UpdateLanguageId</c> and <c>CanonicalizeUValue</c>. The engine reads the options in the specification's order
/// (calendar, collation, firstDayOfWeek, hourCycle, caseFirst, numeric, numberingSystem, after language, script, region and
/// variants) and checks each as it reads it with this type's <c>Check</c> methods, so a refused option stops the reads at
/// the moment ECMA-402 does; <see cref="TryCreate"/> then makes the record, checking again. The value lists of
/// <c>hourCycle</c> and <c>caseFirst</c> are GetOption's, which the engine checks.
/// </para>
/// <para>
/// <b>Every check is over ASCII alone</b> (rows L2 and L14): a KELVIN SIGN or a dotted capital I in an option is refused,
/// never folded into a letter first.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-intl.locale records; IP=Low; Security=Low; Resources=0; Fingerprint=FB2794
// Broiler-Human:        PENDING
public sealed record LocaleRecord(
    string Locale,
    string? Calendar,
    string? Collation,
    string? FirstDayOfWeek,
    string? HourCycle,
    string? CaseFirst,
    bool Numeric,
    string? NumberingSystem)
{
    /// <summary>%Intl.Locale%.[[LocaleExtensionKeys]]: the five ECMA-402 requires, and the collator's two.</summary>
    private static ImmutableArray<string> LocaleExtensionKeys { get; } = ["ca", "co", "fw", "hc", "kf", "kn", "nu"];

    /// <summary>The <c>language</c> option's check in UpdateLanguageId (<c>sec-updatelanguageid</c>): a <c>unicode_language_subtag</c>.</summary>
    /// <param name="value">The option's String.</param>
    /// <returns><see cref="IntlStatus.Completed"/>, or <see cref="IntlStatus.InvalidLanguageSubtag"/>, a <c>RangeError</c>.</returns>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-intl.locale sec-updatelanguageid via Broiler.VM JsRealm.Locale; IP=Low; Security=Low; Resources=1; Fingerprint=4F4846
    // Broiler-Human:        PENDING
    public static IntlStatus CheckLanguage(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return LocaleTag.IsLanguage(value) ? IntlStatus.Completed : IntlStatus.InvalidLanguageSubtag;
    }

    /// <summary>The <c>script</c> option's check in UpdateLanguageId: a <c>unicode_script_subtag</c>.</summary>
    /// <param name="value">The option's String.</param>
    /// <returns><see cref="IntlStatus.Completed"/>, or <see cref="IntlStatus.InvalidScriptSubtag"/>.</returns>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-intl.locale sec-updatelanguageid via Broiler.VM JsRealm.Locale; IP=Low; Security=Low; Resources=1; Fingerprint=EE068A
    // Broiler-Human:        PENDING
    public static IntlStatus CheckScript(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return LocaleTag.IsScript(value) ? IntlStatus.Completed : IntlStatus.InvalidScriptSubtag;
    }

    /// <summary>The <c>region</c> option's check in UpdateLanguageId: a <c>unicode_region_subtag</c>.</summary>
    /// <param name="value">The option's String.</param>
    /// <returns><see cref="IntlStatus.Completed"/>, or <see cref="IntlStatus.InvalidRegionSubtag"/>.</returns>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-intl.locale sec-updatelanguageid via Broiler.VM JsRealm.Locale; IP=Low; Security=Low; Resources=1; Fingerprint=2BF7C4
    // Broiler-Human:        PENDING
    public static IntlStatus CheckRegion(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return LocaleTag.IsRegion(value) ? IntlStatus.Completed : IntlStatus.InvalidRegionSubtag;
    }

    /// <summary>
    /// The <c>variants</c> option's check in UpdateLanguageId: not empty, and, ASCII-lowercased, <c>unicode_variant_subtag</c>s
    /// joined by <c>-</c> with no repeat.
    /// </summary>
    /// <param name="value">The option's String.</param>
    /// <returns><see cref="IntlStatus.Completed"/>, or <see cref="IntlStatus.InvalidVariants"/>.</returns>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-intl.locale sec-updatelanguageid via Broiler.VM JsRealm.Locale; IP=Low; Security=Low; Resources=1; Fingerprint=4490C7
    // Broiler-Human:        PENDING
    public static IntlStatus CheckVariants(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (value.Length == 0)
        {
            return IntlStatus.InvalidVariants;
        }

        // ASCII-lowercase, not the platform's casing, which would lower U+212A KELVIN SIGN to `k` and let it through.
        var lower = LocaleTag.AsciiLower(value);
        var seen = new List<string>();

        foreach (var variant in lower.Split('-'))
        {
            if (!LocaleTag.IsVariant(variant) || seen.Contains(variant))
            {
                return IntlStatus.InvalidVariants;
            }

            seen.Add(variant);
        }

        return IntlStatus.Completed;
    }

    /// <summary>
    /// The check of a <c>calendar</c>, <c>collation</c> or <c>numberingSystem</c> option: matched by UTS #35's <c>type</c>
    /// nonterminal (<see cref="LocaleId.IsUnicodeType"/>).
    /// </summary>
    /// <param name="value">The option's String.</param>
    /// <returns><see cref="IntlStatus.Completed"/>, or <see cref="IntlStatus.InvalidType"/>.</returns>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-intl.locale sec-updatelanguageid via Broiler.VM JsRealm.Locale; IP=Low; Security=Low; Resources=1; Fingerprint=ABE1A4
    // Broiler-Human:        PENDING
    public static IntlStatus CheckType(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return LocaleTag.IsTypeSequence(value) ? IntlStatus.Completed : IntlStatus.InvalidType;
    }

    /// <summary>The <c>firstDayOfWeek</c> option's check: <see cref="WeekdayToUValue"/>, then <see cref="CheckType"/>.</summary>
    /// <param name="value">The option's String, as read.</param>
    /// <returns><see cref="IntlStatus.Completed"/>, or <see cref="IntlStatus.InvalidType"/>.</returns>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-intl.locale sec-updatelanguageid via Broiler.VM JsRealm.Locale; IP=Low; Security=Low; Resources=1; Fingerprint=76AFE4
    // Broiler-Human:        PENDING
    public static IntlStatus CheckFirstDayOfWeek(string value) => CheckType(WeekdayToUValue(value));

    /// <summary>ECMA-402's WeekdayToUValue (<c>sec-weekdaytouvalue</c>): <c>"0"</c> to <c>"7"</c> as <c>sun</c> to <c>sun</c>, and any other String unchanged.</summary>
    /// <param name="value">The <c>firstDayOfWeek</c> option's String.</param>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-intl.locale sec-updatelanguageid via Broiler.VM JsRealm.Locale; IP=Low; Security=Low; Resources=1; Fingerprint=26657D
    // Broiler-Human:        PENDING
    public static string WeekdayToUValue(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Length == 1 && value[0] is >= '0' and <= '7' ? LocaleTag.WeekdayKeys[value[0] - '0'] : value;
    }

    /// <summary>
    /// ECMA-402's UpdateLanguageId (<c>sec-updatelanguageid</c>) once the engine has read the four options: the tag's language,
    /// script, region and variants, each replaced by its option where the option is given, each checked, and the tag's
    /// extensions kept.
    /// </summary>
    /// <param name="tag">The canonical tag (<see cref="LocaleId.TryCanonicalize"/>).</param>
    /// <param name="language">The <c>language</c> option, or <see langword="null"/> when absent.</param>
    /// <param name="script">The <c>script</c> option, or <see langword="null"/>.</param>
    /// <param name="region">The <c>region</c> option, or <see langword="null"/>.</param>
    /// <param name="variants">The <c>variants</c> option, or <see langword="null"/>.</param>
    /// <param name="updated">The updated tag, not yet canonical, or the empty string on a failure.</param>
    /// <returns><see cref="IntlStatus.Completed"/>, or the status of the first check that fails, in the options' order.</returns>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-intl.locale sec-updatelanguageid via Broiler.VM JsRealm.Locale; IP=Low; Security=Medium; Resources=2; Fingerprint=C94783
    // Broiler-Human:        PENDING
    public static IntlStatus UpdateLanguageId(string tag, string? language, string? script, string? region, string? variants, out string updated)
    {
        ArgumentNullException.ThrowIfNull(tag);
        updated = string.Empty;

        if (LocaleTag.Parse(tag) is not { } parsed)
        {
            return IntlStatus.InvalidLanguageTag;
        }

        var baseName = parsed.LanguageId();

        language ??= parsed.Language;

        if (CheckLanguage(language) is var languageStatus && languageStatus != IntlStatus.Completed)
        {
            return languageStatus;
        }

        script ??= parsed.Script;

        if (script is not null && CheckScript(script) is var scriptStatus && scriptStatus != IntlStatus.Completed)
        {
            return scriptStatus;
        }

        region ??= parsed.Region;

        if (region is not null && CheckRegion(region) is var regionStatus && regionStatus != IntlStatus.Completed)
        {
            return regionStatus;
        }

        variants ??= parsed.Variants.Count == 0 ? null : string.Join('-', parsed.Variants);

        if (variants is not null && CheckVariants(variants) is var variantsStatus && variantsStatus != IntlStatus.Completed)
        {
            return variantsStatus;
        }

        var extensions = tag[baseName.Length..];
        var text = new StringBuilder(language);

        if (script is not null)
        {
            text.Append('-').Append(script);
        }

        if (region is not null)
        {
            text.Append('-').Append(region);
        }

        if (variants is not null)
        {
            text.Append('-').Append(variants);
        }

        updated = text.Append(extensions).ToString();
        return IntlStatus.Completed;
    }

    /// <summary>
    /// The <c>Intl.Locale</c> constructor's steps after the engine has read the tag's String and the options
    /// (<c>sec-intl.locale</c>, steps 10 to 35): IsWellFormedLanguageTag and CanonicalizeUnicodeLocaleId of the tag,
    /// UpdateLanguageId, each keyword option's check, and MakeLocaleRecord, whose locale is canonicalised.
    /// </summary>
    /// <param name="tag">The tag's String: a String argument, or an <c>Intl.Locale</c>'s [[Locale]].</param>
    /// <param name="options">The options as read; <see cref="LocaleOptions.None"/> for none.</param>
    /// <param name="data">The locale data canonicalisation reads.</param>
    /// <param name="record">The record, or <see langword="null"/> on a failure.</param>
    /// <param name="charge">The caller's meter: the tag's length in code units, before it is parsed.</param>
    /// <returns><see cref="IntlStatus.Completed"/>, or the status of the first step that fails; every one is a <c>RangeError</c>.</returns>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-intl.locale sec-updatelanguageid via Broiler.VM JsRealm.Locale; IP=Low; Security=High; Resources=2; Fingerprint=94404F
    // Broiler-Falsified-If: an option outside ASCII or outside its grammar is accepted, an option is checked in another order than ECMA-402's, or the record's locale is not the canonical tag of the tag and options
    // Broiler-Human:        PENDING
    public static IntlStatus TryCreate(string tag, LocaleOptions options, ILocaleData data, out LocaleRecord? record, WorkCharge? charge = null)
    {
        ArgumentNullException.ThrowIfNull(tag);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(data);
        record = null;

        var status = LocaleId.TryCanonicalize(tag, data, out var canonical, charge);

        if (status != IntlStatus.Completed)
        {
            return status;
        }

        status = UpdateLanguageId(canonical, options.Language, options.Script, options.Region, options.Variants, out var text);

        if (status != IntlStatus.Completed)
        {
            return status;
        }

        foreach (var typed in new[] { options.Calendar, options.Collation })
        {
            if (typed is not null && CheckType(typed) is var typeStatus && typeStatus != IntlStatus.Completed)
            {
                return typeStatus;
            }
        }

        var firstDay = options.FirstDayOfWeek is { } fw ? WeekdayToUValue(fw) : null;

        if (firstDay is not null && CheckType(firstDay) is var firstDayStatus && firstDayStatus != IntlStatus.Completed)
        {
            return firstDayStatus;
        }

        if (options.NumberingSystem is { } nu && CheckType(nu) is var nuStatus && nuStatus != IntlStatus.Completed)
        {
            return nuStatus;
        }

        var overrides = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["ca"] = options.Calendar,
            ["co"] = options.Collation,
            ["fw"] = firstDay,
            ["hc"] = options.HourCycle,
            ["kf"] = options.CaseFirst,
            ["kn"] = options.Numeric is { } numeric ? (numeric ? "true" : "false") : null,
            ["nu"] = options.NumberingSystem,
        };

        // MAKELOCALERECORD: each key from the tag's keywords, overridden by its option.
        var parsed = LocaleTag.Parse(text);

        if (parsed is null)
        {
            return IntlStatus.InvalidLanguageTag;
        }

        var values = new Dictionary<string, string?>(StringComparer.Ordinal);

        foreach (var key in LocaleExtensionKeys)
        {
            var entry = parsed.Keywords.FindIndex(pair => pair.Key == key);
            var value = entry >= 0 ? parsed.Keywords[entry].Type : null;

            if (overrides[key] is { } overrideValue)
            {
                value = CanonicalizeUValue(data, key, overrideValue);

                if (entry >= 0)
                {
                    parsed.Keywords[entry] = (key, value);
                }
                else
                {
                    parsed.Keywords.Add((key, value));
                }
            }

            values[key] = value;
        }

        parsed.HasUnicodeExtension = parsed.Attributes.Count != 0 || parsed.Keywords.Count != 0;
        var locale = parsed.Canonicalize(data).ToString();

        record = new LocaleRecord(
            locale,
            values["ca"],
            values["co"],
            values["fw"],
            values["hc"],
            values["kf"],
            values["kn"] is "true" or "",
            values["nu"]);
        return IntlStatus.Completed;
    }

    /// <summary>ECMA-402's CanonicalizeUValue (<c>sec-canonicalizeuvalue</c>): the ASCII-lowercased value, a deprecated one replaced, and <c>true</c> left implicit.</summary>
    /// <param name="data">The locale data whose BCP 47 types it reads.</param>
    /// <param name="key">The key, lower case.</param>
    /// <param name="value">The value.</param>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-intl.locale sec-updatelanguageid via Broiler.VM JsRealm.Locale; IP=Low; Security=Low; Resources=1; Fingerprint=CC0F09
    // Broiler-Human:        PENDING
    public static string CanonicalizeUValue(ILocaleData data, string key, string value)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);

        var lower = LocaleTag.AsciiLower(value);

        if (data.TryExtensionType('u', key, lower, out var preferred))
        {
            lower = preferred;
        }

        return lower == "true" ? string.Empty : lower;
    }
}

/// <summary>
/// The options of the <c>Intl.Locale</c> constructor, each as the engine read it and converted it to a String or a
/// Boolean, or <see langword="null"/> where absent (ECMA-402 <c>sec-intl.locale</c>).
/// </summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-intl.locale records; IP=Low; Security=Low; Resources=0; Fingerprint=3F7A51
// Broiler-Human:        PENDING
public sealed record LocaleOptions
{
    /// <summary>No option given.</summary>
    public static LocaleOptions None { get; } = new();

    /// <summary>The <c>language</c> option.</summary>
    public string? Language { get; init; }

    /// <summary>The <c>script</c> option.</summary>
    public string? Script { get; init; }

    /// <summary>The <c>region</c> option.</summary>
    public string? Region { get; init; }

    /// <summary>The <c>variants</c> option.</summary>
    public string? Variants { get; init; }

    /// <summary>The <c>calendar</c> option.</summary>
    public string? Calendar { get; init; }

    /// <summary>The <c>collation</c> option.</summary>
    public string? Collation { get; init; }

    /// <summary>The <c>firstDayOfWeek</c> option, as read: WeekdayToUValue is the library's.</summary>
    public string? FirstDayOfWeek { get; init; }

    /// <summary>The <c>hourCycle</c> option, which GetOption has held to <c>h11</c>, <c>h12</c>, <c>h23</c> and <c>h24</c>.</summary>
    public string? HourCycle { get; init; }

    /// <summary>The <c>caseFirst</c> option, which GetOption has held to <c>upper</c>, <c>lower</c> and <c>false</c>.</summary>
    public string? CaseFirst { get; init; }

    /// <summary>The <c>numeric</c> option.</summary>
    public bool? Numeric { get; init; }

    /// <summary>The <c>numberingSystem</c> option.</summary>
    public string? NumberingSystem { get; init; }
}
