// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   16
// Annotated:        16/16
// Exempt:           2
// Human-reviewed:   0/16
// IP risk:          Low
// Security risk:    High
// Criteria:         1/1
// Resource impact:  1/10 max
// Unverified:       16
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Generic;
using Broiler.ECMAScript.Time;

namespace Broiler.ECMAScript.Intl;

/// <summary>
/// The information an <c>Intl.Locale</c> gives (ECMA-402 <c>locale.html</c>, the Intl Locale Info proposal as the pinned text
/// holds it): its calendars, collations, hour cycles, numbering systems, time zones, text direction and week, each from the
/// locale data or the engine's services (Broiler.VM's JSD-0082, sub-area (a), rows L15 to L27).
/// </summary>
/// <remarks>
/// <para>
/// <b>Ported from Broiler.VM's <c>JsRealm.Locale</c></b> at its SJL-9 rule commit <c>4cf9ca44</c>: the information methods,
/// <c>RegionPreference</c>, <c>Subdivision</c>, <c>HourCyclesOf</c>, <c>TextDirectionOf</c> and <c>WeekInfoOf</c>. Two
/// answers follow the pinned text where Broiler.VM's code did not:
/// </para>
/// <list type="bullet">
/// <item><b>Row L18, a correction of Broiler.VM</b> (decision D32): CollationsOfLocale of a locale whose language the engine's
/// <c>Intl.Collator</c> does not support answers DefaultLocale's collations, and <c>emoji</c> and <c>eor</c> only for
/// <c>und</c>. Broiler.VM answered <c>emoji</c> and <c>eor</c> for every such locale.</item>
/// <item><b>Row L22, data policy</b> (decisions D5 and D13): NumberingSystemsOfLocale answers the matched locale's default
/// numbering system from the locale data, <c>latn</c> where the data names none. Broiler.VM answered <c>latn</c>, which its
/// data states for every locale it carries, so none of its answers moves.</item>
/// </list>
/// <para>
/// <b>What an engine hands in</b>: the locale data; for collations, its <c>Intl.Collator</c>'s data and DefaultLocale (rows
/// L17, L18 and L40, each engine's until milestone SJL-17); for numbering systems, its <c>Intl.NumberFormat</c>'s available
/// locales; for time zones, the shared time zone database. No method charges a meter: each works on a record the
/// constructor has made, whose text it charged for (decision D18).
/// </para>
/// </remarks>
// Broiler-AI:           Origin=Ported; Spec=ECMA-402 locale.html Locale Info via Broiler.VM JsRealm.Locale; IP=Low; Security=Low; Resources=0; Fingerprint=1AEABD
// Broiler-Human:        PENDING
public static class LocaleInfo
{
    /// <summary>
    /// CalendarsOfLocale (<c>sec-calendarsoflocale</c>): the <c>ca</c> keyword, else the region's calendars in common use,
    /// which no embedding's data carries, so <c>gregory</c> (row L16, the same in both engines).
    /// </summary>
    /// <param name="locale">The Locale.</param>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 locale.html Locale Info via Broiler.VM JsRealm.Locale; IP=Low; Security=Low; Resources=1; Fingerprint=F9A4AD
    // Broiler-Human:        PENDING
    public static IReadOnlyList<string> Calendars(LocaleRecord locale)
    {
        ArgumentNullException.ThrowIfNull(locale);
        return [locale.Calendar ?? "gregory"];
    }

    /// <summary>
    /// CollationsOfLocale (<c>sec-collationsoflocale</c>): the <c>co</c> keyword; else, for a language other than <c>und</c>,
    /// the collations of the Collator's locale that matches, or of DefaultLocale when none does, without their leading null;
    /// else <c>emoji</c> and <c>eor</c>; sorted by code unit (rows L15, L17 and L18).
    /// </summary>
    /// <param name="locale">The Locale.</param>
    /// <param name="collator">The engine's <c>Intl.Collator</c> data.</param>
    /// <param name="defaultLocale">DefaultLocale, from the engine's provider.</param>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 locale.html Locale Info via Broiler.VM JsRealm.Locale; IP=Low; Security=High; Resources=1; Fingerprint=ED121E
    // Broiler-Falsified-If: a locale whose language is not und and that the Collator's available locales do not match answers anything but DefaultLocale's collations (row L18), or und answers anything but emoji and eor
    // Broiler-Human:        PENDING
    public static IReadOnlyList<string> Collations(LocaleRecord locale, ICollatorLocaleData collator, string defaultLocale)
    {
        ArgumentNullException.ThrowIfNull(locale);
        ArgumentNullException.ThrowIfNull(collator);
        ArgumentNullException.ThrowIfNull(defaultLocale);

        if (locale.Collation is not null)
        {
            return [locale.Collation];
        }

        var list = new List<string>();

        if (Tag(locale.Locale).Language != "und")
        {
            // ROW L18 (decision D32): no match is DefaultLocale's data, where Broiler.VM answered emoji and eor.
            var found = LocaleNegotiation.Lookup(collator.AvailableLocales, locale.Locale)?.Locale ?? defaultLocale;

            foreach (var value in collator.SortLocaleData(found))
            {
                if (value is not null)
                {
                    list.Add(value);
                }
            }
        }
        else
        {
            list.Add("emoji");
            list.Add("eor");
        }

        list.Sort(StringComparer.Ordinal);
        return list.AsReadOnly();
    }

    /// <summary>
    /// HourCyclesOfLocale (<c>sec-hourcyclesoflocale</c>): the <c>hc</c> keyword, else CLDR's time data for the preferred
    /// regions (the <c>rg</c> override first), the preferred cycle and then the allowed ones, <c>h23</c> when the data has
    /// none (rows L19 to L21).
    /// </summary>
    /// <param name="locale">The Locale.</param>
    /// <param name="data">The locale data.</param>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 locale.html Locale Info via Broiler.VM JsRealm.Locale; IP=Low; Security=Low; Resources=1; Fingerprint=E68B79
    // Broiler-Human:        PENDING
    public static IReadOnlyList<string> HourCycles(LocaleRecord locale, ILocaleData data)
    {
        ArgumentNullException.ThrowIfNull(locale);
        ArgumentNullException.ThrowIfNull(data);

        if (locale.HourCycle is not null)
        {
            return [locale.HourCycle];
        }

        var preference = RegionPreference(locale.Locale, data);
        var language = Tag(locale.Locale).Language;
        var cycles = new List<string>();
        string[] regions = preference.RegionOverride is { } regionOverride ? [regionOverride, preference.Region] : [preference.Region];

        foreach (var preferred in regions)
        {
            if (cycles.Count != 0)
            {
                break;
            }

            if ((data.HourCycles(language + "_" + preferred) ?? data.HourCycles(preferred)) is not { } time)
            {
                continue;
            }

            foreach (var format in (time.Preferred + " " + time.Allowed).Split(' '))
            {
                var cycle = format.Length == 0 ? null : format[0] switch
                {
                    'h' => "h12",
                    'H' => "h23",
                    'K' => "h11",
                    'k' => "h24",
                    _ => null,
                };

                if (cycle is not null && !cycles.Contains(cycle))
                {
                    cycles.Add(cycle);
                }
            }
        }

        return cycles.Count == 0 ? ["h23"] : cycles.AsReadOnly();
    }

    /// <summary>
    /// NumberingSystemsOfLocale (<c>sec-numberingsystemsoflocale</c>): the <c>nu</c> keyword; else the default numbering system
    /// of the <c>Intl.NumberFormat</c> locale that matches, from the locale data's <c>defaultNumberingSystem</c> along its parent
    /// chain; else <c>latn</c> (rows L15 and L22).
    /// </summary>
    /// <param name="locale">The Locale.</param>
    /// <param name="numberFormatLocales">The engine's <c>Intl.NumberFormat</c> available locales.</param>
    /// <param name="data">The locale data.</param>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 locale.html Locale Info via Broiler.VM JsRealm.Locale; IP=Low; Security=Medium; Resources=1; Fingerprint=CC919D
    // Broiler-Human:        PENDING
    public static IReadOnlyList<string> NumberingSystems(LocaleRecord locale, IReadOnlyList<string> numberFormatLocales, ILocaleData data)
    {
        ArgumentNullException.ThrowIfNull(locale);
        ArgumentNullException.ThrowIfNull(numberFormatLocales);
        ArgumentNullException.ThrowIfNull(data);

        if (locale.NumberingSystem is not null)
        {
            return [locale.NumberingSystem];
        }

        if (LocaleNegotiation.Lookup(numberFormatLocales, locale.Locale) is { } match)
        {
            foreach (var tag in CompositeLocaleData.Chain(match.Locale))
            {
                if (data.NumberValue(tag, "defaultNumberingSystem") is { Length: > 0 } system)
                {
                    return [system];
                }
            }
        }

        return ["latn"];
    }

    /// <summary>
    /// TimeZonesOfLocale (<c>sec-timezonesoflocale</c>): <see langword="null"/>, for <b>undefined</b>, when the locale names no
    /// region; else the region's zones from the shared time zone database's <c>zone.tab</c>, in code-unit order (row L23).
    /// </summary>
    /// <param name="locale">The Locale.</param>
    /// <param name="timeZones">The time zone database the engine embeds.</param>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 locale.html Locale Info via Broiler.VM JsRealm.Locale; IP=Low; Security=Low; Resources=1; Fingerprint=4C3CC8
    // Broiler-Human:        PENDING
    public static IReadOnlyList<string>? TimeZones(LocaleRecord locale, ITimeZoneDatabase timeZones)
    {
        ArgumentNullException.ThrowIfNull(locale);
        ArgumentNullException.ThrowIfNull(timeZones);
        return Tag(locale.Locale).Region is not { } region ? null : timeZones.RegionTimeZones(region);
    }

    /// <summary>
    /// TextDirectionOfLocale (<c>sec-textdirectionoflocale</c>): the line direction of the locale's script, or of its likely
    /// script, from CLDR's script metadata; <see langword="null"/>, for <b>undefined</b>, when no direction is known (rows L24
    /// and L25).
    /// </summary>
    /// <param name="locale">The Locale.</param>
    /// <param name="data">The locale data.</param>
    /// <returns><c>"rtl"</c>, <c>"ltr"</c>, or <see langword="null"/>.</returns>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 locale.html Locale Info via Broiler.VM JsRealm.Locale; IP=Low; Security=Low; Resources=1; Fingerprint=08E021
    // Broiler-Human:        PENDING
    public static string? TextDirection(LocaleRecord locale, ILocaleData data)
    {
        ArgumentNullException.ThrowIfNull(locale);
        ArgumentNullException.ThrowIfNull(data);

        var tag = Tag(locale.Locale);

        if (tag.Script is null && !tag.Maximize(data))
        {
            return null;
        }

        return tag.Script is { } script && data.IsRightToLeft(script) is { } rightToLeft
            ? rightToLeft ? "rtl" : "ltr"
            : null;
    }

    /// <summary>
    /// WeekInfoOfLocale (<c>sec-weekinfooflocale</c>): CLDR's first day and weekend for the preferred region (the <c>rg</c>
    /// override where the data has it, else the region, else the world's), the <c>fw</c> keyword's day over the first day
    /// (rows L21 and L26).
    /// </summary>
    /// <param name="locale">The Locale.</param>
    /// <param name="data">The locale data, whose week data holds the world's, <c>001</c>.</param>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 locale.html Locale Info via Broiler.VM JsRealm.Locale; IP=Low; Security=Low; Resources=1; Fingerprint=4FA0FA
    // Broiler-Human:        PENDING
    public static LocaleWeekInfo WeekInfo(LocaleRecord locale, ILocaleData data)
    {
        ArgumentNullException.ThrowIfNull(locale);
        ArgumentNullException.ThrowIfNull(data);

        var world = data.Week("001") ?? throw new ArgumentException("the locale data holds no week data for the world, 001", nameof(data));
        var preference = RegionPreference(locale.Locale, data);
        var week = (preference.RegionOverride is not null ? data.Week(preference.RegionOverride) : null) ?? data.Week(preference.Region) ?? world;

        var firstDay = Day(week.FirstDay.Length != 0 ? week.FirstDay : world.FirstDay);
        var start = Day(week.WeekendStart.Length != 0 ? week.WeekendStart : world.WeekendStart);
        var end = Day(week.WeekendEnd.Length != 0 ? week.WeekendEnd : world.WeekendEnd);
        var weekend = new List<int>();

        for (var day = start; ; day = (day % 7) + 1)
        {
            weekend.Add(day);

            if (day == end || weekend.Count == 7)
            {
                break;
            }
        }

        weekend.Sort();

        if (locale.FirstDayOfWeek is { } keyword && Day(keyword) is > 0 and var overridden)
        {
            firstDay = overridden;
        }

        return new LocaleWeekInfo(firstDay, weekend.AsReadOnly());
    }

    /// <summary>
    /// ECMA-402's RegionPreference (<c>sec-regionpreference</c>): the locale's region, else its <c>sd</c> subdivision's, else its
    /// likely one, else <c>001</c>; and the <c>rg</c> override (row L21).
    /// </summary>
    /// <param name="locale">A canonical locale identifier.</param>
    /// <param name="data">The locale data.</param>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 locale.html Locale Info via Broiler.VM JsRealm.Locale; IP=Low; Security=Low; Resources=1; Fingerprint=E62750
    // Broiler-Human:        PENDING
    public static LocaleRegionPreference RegionPreference(string locale, ILocaleData data)
    {
        ArgumentNullException.ThrowIfNull(locale);
        ArgumentNullException.ThrowIfNull(data);

        var tag = Tag(locale);
        var region = tag.Region ?? Subdivision(data, tag, "sd");

        if (region is null)
        {
            var maximal = Tag(locale);
            _ = maximal.Maximize(data);
            region = maximal.Canonicalize(data).Region ?? "001";
        }

        return new LocaleRegionPreference(region, Subdivision(data, tag, "rg"));
    }

    /// <summary>ECMA-402's CanonicalUnicodeSubdivision (<c>sec-canonicalunicodesubdivision</c>): the region a subdivision keyword names.</summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 locale.html Locale Info via Broiler.VM JsRealm.Locale; IP=Low; Security=Low; Resources=1; Fingerprint=7459AD
    // Broiler-Human:        PENDING
    private static string? Subdivision(ILocaleData data, LocaleTag tag, string key)
    {
        var entry = tag.Keywords.FindIndex(pair => pair.Key == key);

        if (entry < 0)
        {
            return null;
        }

        var value = tag.Keywords[entry].Type;
        var prefix = value.Length >= 3 && char.IsAsciiDigit(value[0]) ? 3 : 2;

        if (value.Length < prefix + 1 || value.Length > prefix + 4 || !LocaleTag.IsAlphanumeric(value) ||
            !(prefix == 3 ? LocaleTag.IsDigits(value[..3]) : LocaleTag.IsAlpha(value[..2])))
        {
            return null;
        }

        return LocaleTag.Parse("und-" + value[..prefix])?.Canonicalize(data).Region;
    }

    /// <summary>A week data day as ECMA-402's weekday number, Monday 1 to Sunday 7; 0 for anything else.</summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 locale.html Locale Info via Broiler.VM JsRealm.Locale; IP=Low; Security=Low; Resources=1; Fingerprint=DC903D
    // Broiler-Human:        PENDING
    private static int Day(string key) => key switch
    {
        "mon" => 1,
        "tue" => 2,
        "wed" => 3,
        "thu" => 4,
        "fri" => 5,
        "sat" => 6,
        "sun" => 7,
        _ => 0,
    };

    /// <summary>The parsed form of a Locale's canonical identifier.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-402 locale.html Locale Info via Broiler.VM JsRealm.Locale; IP=Low; Security=Low; Resources=1; Fingerprint=195C73
    // Broiler-Human:        PENDING
    private static LocaleTag Tag(string locale) =>
        LocaleTag.Parse(locale) ?? throw new ArgumentException("the locale " + locale + " is not a well-formed tag", nameof(locale));
}

/// <summary>What WeekInfoOfLocale answers (ECMA-402's WeekInfo Record): the first day, and the weekend's days in ascending order, Monday 1 to Sunday 7.</summary>
/// <param name="FirstDay">[[FirstDay]].</param>
/// <param name="Weekend">[[Weekend]].</param>
// Broiler-AI:           Origin=AI; Spec=ECMA-402 locale.html records and the Collator's data; IP=Low; Security=Low; Resources=0; Fingerprint=4D63D4
// Broiler-Human:        PENDING
public sealed record LocaleWeekInfo(int FirstDay, IReadOnlyList<int> Weekend);

/// <summary>What RegionPreference answers: the region whose data a locale reads, and the <c>rg</c> override.</summary>
/// <param name="Region">[[Region]].</param>
/// <param name="RegionOverride">[[RegionOverride]], or <see langword="null"/>.</param>
// Broiler-AI:           Origin=AI; Spec=ECMA-402 locale.html records and the Collator's data; IP=Low; Security=Low; Resources=0; Fingerprint=7958E9
// Broiler-Human:        PENDING
public sealed record LocaleRegionPreference(string Region, string? RegionOverride);

/// <summary>
/// The data of an engine's <c>Intl.Collator</c> that sub-area (a) reads (rows L17, L18, L32 and L40): its available locales,
/// each one's sort collations, and the collations it supports. Each engine keeps its own Collator until milestone SJL-17
/// shares collation (decision SJL-D-24 (c)); Broiler.VM's lists <c>phonebk</c> for German alone (JSD-0043).
/// </summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-402 locale.html records and the Collator's data; IP=Low; Security=Low; Resources=0; Fingerprint=CBB9CD
// Broiler-Human:        PENDING
public interface ICollatorLocaleData
{
    /// <summary>%Intl.Collator%.[[AvailableLocales]].</summary>
    IReadOnlyList<string> AvailableLocales { get; }

    /// <summary>The collations ECMA-402's AvailableCanonicalCollations names (<c>sec-availablecanonicalcollations</c>), for <c>Intl.supportedValuesOf("collation")</c>.</summary>
    IReadOnlyList<string> Collations { get; }

    /// <summary>%Intl.Collator%.[[SortLocaleData]].[[&lt;locale&gt;]].[[co]]: the locale's collations for sorting, <see langword="null"/> first.</summary>
    /// <param name="locale">An available locale, or DefaultLocale.</param>
    // Broiler-AI:           Origin=AI; Spec=ECMA-402 locale.html records and the Collator's data; IP=Low; Security=Low; Resources=1; Fingerprint=6AF046
    // Broiler-Human:        PENDING
    IReadOnlyList<string?> SortLocaleData(string locale);
}
