// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// HAIRS Code Assurance
// --------------------
// Relevant units:   15
// Annotated:        15/15
// Exempt:           8
// Human-reviewed:   0/15
// IP risk:          Low
// Security risk:    Medium
// Criteria:         0/0
// Resource impact:  2/10 max
// Unverified:       15
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Generic;
using System.Threading;
using Broiler.ECMAScript.Temporal;
using Broiler.ECMAScript.Time;

namespace Broiler.ECMAScript.Intl;

/// <summary>
/// What <c>Intl.DateTimeFormat</c> reads besides its options (ECMA-402 <c>sec-intl.datetimeformat-internal-slots</c>, as
/// <c>tc39/proposal-temporal@e8cc03fc</c> amends it): the locale data, the IANA Time Zone Database and the calendars, each an
/// engine's, which an engine makes once and hands every date-time format it creates (Broiler.VM's JSD-0082, sub-area (d)).
/// </summary>
/// <remarks>
/// <para>
/// <b>It holds what is made once and shared</b>, as Broiler.VM's <c>JsIntlTables</c> holds its date data, its calendars and
/// its zone names: the pattern generators, one per data locale, hour character and decimal symbol, each made the first
/// time a format asks for it and never changed afterwards; and the zone names' metazone data (<see cref="ZoneNamer"/>),
/// built the first time a zone is named. Both are made under a lock, so one instance may be shared between threads
/// (convention 10). The generators are bounded by the locales, the four hour characters and the locales' decimal
/// symbols (convention 11).
/// </para>
/// <para>
/// <b>The locale data is read through the <see cref="ILocaleData"/> it is handed</b>, ordinarily a
/// <see cref="CompositeLocaleData"/>, at ResolveLocale's [[DataLocale]], as sub-area (c)'s formatters read theirs; the time
/// zones are SJL-8's <see cref="ITimeZoneDatabase"/> and the calendars SJL-10's <see cref="TemporalCalendars"/>, so no table
/// is read here but through the provider and those two.
/// </para>
/// </remarks>
// Hairs-AI:           Origin=AI; Spec=JSD-0082 D8 provider for sub-area (d), no table read outside the provider; IP=Low; Security=Low; Resources=0; Fingerprint=46C18D
// Hairs-Human:        PENDING
public sealed class DateTimeData
{
    private readonly Lazy<ZoneNamer> zoneNames;

    /// <summary>Creates the date-time data over an engine's providers; nothing is read until a format needs it.</summary>
    /// <param name="data">The locale data, ordinarily a <see cref="CompositeLocaleData"/>.</param>
    /// <param name="timeZones">The IANA Time Zone Database the formats' zones are found in.</param>
    /// <param name="calendars">The calendars a format other than the Gregorian and ISO 8601 ones writes its fields in.</param>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    // Hairs-AI:           Origin=AI; Spec=JSD-0082 D8 provider for sub-area (d), no table read outside the provider; IP=Low; Security=Low; Resources=1; Fingerprint=72F008
    // Hairs-Human:        PENDING
    public DateTimeData(ILocaleData data, ITimeZoneDatabase timeZones, TemporalCalendars calendars)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(timeZones);
        ArgumentNullException.ThrowIfNull(calendars);
        Data = data;
        TimeZones = timeZones;
        Calendars = calendars;
        Dates = new DateData(data);
        zoneNames = new(() => new ZoneNamer(data, timeZones), LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <summary>The locale data.</summary>
    public ILocaleData Data { get; }

    /// <summary>The IANA Time Zone Database.</summary>
    public ITimeZoneDatabase TimeZones { get; }

    /// <summary>The calendars.</summary>
    public TemporalCalendars Calendars { get; }

    /// <summary>The date data as the formatters read it, with the pattern generators made once.</summary>
    internal DateData Dates { get; }

    /// <summary>CLDR's metazones and the algorithms that name a zone by them, built the first time a zone is named.</summary>
    // Hairs-AI:           Origin=AI; Spec=JSD-0082 D8 provider for sub-area (d), no table read outside the provider; IP=Low; Security=Low; Resources=0; Fingerprint=CBD23D
    // Hairs-Human:        PENDING
    internal ZoneNamer ZoneNames => zoneNames.Value;
}

/// <summary>
/// The Gregorian calendar, other calendars', hour cycle and day period data the date-time formatters read, as Broiler.VM's
/// <c>JsDateData</c> holds it, over the question-level <see cref="ILocaleData"/> (decision D8).
/// </summary>
/// <remarks>
/// <para>
/// <b>A data language</b> is a locale, or a locale and a calendar as <c>locale@calendar</c>, as Broiler.VM keys a calendar's
/// layer of its date data: <c>en</c> for the Gregorian calendar's, <c>en@hebrew</c> for the Hebrew one's and
/// <c>en@iso8601</c> for the ISO 8601 one's. Broiler.VM's locale was the data locale's language; it is the data locale here,
/// which for Broiler.VM's tables, holding their locales' languages alone, reads the same lines.
/// </para>
/// <para>
/// <b>The pattern generators are made once per data language, hour character and decimal symbol</b> and shared, as
/// Broiler.VM's are: a generator never changes after it is made. They are made under a lock.
/// </para>
/// </remarks>
// Hairs-AI:           Origin=Ported; Spec=JSD-0082 C5 data reads at the data locale (D5) via Broiler.VM JsDateData; IP=Low; Security=Low; Resources=0; Fingerprint=ECB0AE
// Hairs-Human:        PENDING
internal sealed class DateData
{
    private readonly ILocaleData data;

    private readonly Lock gate = new();

    // Hairs-AI:           Origin=Ported; Spec=JSD-0082 C5 data reads at the data locale (D5) via Broiler.VM JsDateData; IP=Low; Security=Low; Resources=0; Fingerprint=2AC135
    // Hairs-Human:        PENDING
    private readonly Dictionary<string, DateTimePatternGenerator> generators = new(StringComparer.Ordinal);

    internal DateData(ILocaleData data) => this.data = data;

    /// <summary>The locale data.</summary>
    // Hairs-AI:           Origin=Ported; Spec=JSD-0082 C5 data reads at the data locale (D5) via Broiler.VM JsDateData; IP=Low; Security=Low; Resources=0; Fingerprint=CBB118
    // Hairs-Human:        PENDING
    internal ILocaleData Locale => data;

    /// <summary>The locale of a data language: <c>en</c> for <c>en@hebrew</c>.</summary>
    // Hairs-AI:           Origin=Ported; Spec=JSD-0082 C5 data reads at the data locale (D5) via Broiler.VM JsDateData; IP=Low; Security=Low; Resources=1; Fingerprint=2423B2
    // Hairs-Human:        PENDING
    internal static string LanguageOf(string dataLanguage)
    {
        var at = dataLanguage.IndexOf('@', StringComparison.Ordinal);
        return at < 0 ? dataLanguage : dataLanguage[..at];
    }

    /// <summary>The calendar of a data language: <c>gregory</c> for <c>en</c>, <c>hebrew</c> for <c>en@hebrew</c>.</summary>
    // Hairs-AI:           Origin=Ported; Spec=JSD-0082 C5 data reads at the data locale (D5) via Broiler.VM JsDateData; IP=Low; Security=Low; Resources=1; Fingerprint=C33AE3
    // Hairs-Human:        PENDING
    private static string CalendarOf(string dataLanguage)
    {
        var at = dataLanguage.IndexOf('@', StringComparison.Ordinal);
        return at < 0 ? "gregory" : dataLanguage[(at + 1)..];
    }

    /// <summary>A data language's value for a key, or nothing.</summary>
    // Hairs-AI:           Origin=Ported; Spec=JSD-0082 C5 data reads at the data locale (D5) via Broiler.VM JsDateData; IP=Low; Security=Low; Resources=1; Fingerprint=AC40D5
    // Hairs-Human:        PENDING
    internal string? Value(string dataLanguage, string key) => data.DateValue(LanguageOf(dataLanguage), CalendarOf(dataLanguage), key);

    /// <summary>Every value of a data language, in the order the decoding left them, or nothing.</summary>
    // Hairs-AI:           Origin=Ported; Spec=JSD-0082 C5 data reads at the data locale (D5) via Broiler.VM JsDateData; IP=Low; Security=Low; Resources=1; Fingerprint=8424B6
    // Hairs-Human:        PENDING
    internal IReadOnlyDictionary<string, string>? Values(string dataLanguage) => data.DateValues(LanguageOf(dataLanguage), CalendarOf(dataLanguage));

    /// <summary>A language's day period rules, or nothing; a calendar's data is its language's for day periods, which belong to no calendar.</summary>
    // Hairs-AI:           Origin=Ported; Spec=JSD-0082 C5 data reads at the data locale (D5) via Broiler.VM JsDateData; IP=Low; Security=Low; Resources=1; Fingerprint=0A5911
    // Hairs-Human:        PENDING
    internal IReadOnlyList<DayPeriodRule>? DayPeriods(string dataLanguage) => data.DayPeriods(LanguageOf(dataLanguage));

    /// <summary>The pattern generator of a data language whose default hour character is <paramref name="hourChar"/>.</summary>
    // Hairs-AI:           Origin=Ported; Spec=JSD-0082 C5 data reads at the data locale (D5) via Broiler.VM JsDateData; IP=Low; Security=Medium; Resources=2; Fingerprint=000431
    // Hairs-Human:        PENDING
    internal DateTimePatternGenerator Generator(string dataLanguage, char hourChar, string decimalSymbol)
    {
        var key = dataLanguage + "|" + hourChar + "|" + decimalSymbol;

        lock (gate)
        {
            if (!generators.TryGetValue(key, out var generator))
            {
                generator = new DateTimePatternGenerator(this, dataLanguage, hourChar, decimalSymbol);
                generators[key] = generator;
            }

            return generator;
        }
    }

    /// <summary>
    /// The time data of a locale's language and region (UTS #35 Part 4, Time Data): CLDR's entry for the locale itself where it
    /// states one - <c>fr-CA</c>'s <c>H</c>, <c>ar-001</c>'s <c>h</c> - then the region's, then the world's (row T10).
    /// <i>(Added 2026-11-09, by the owner's decision of that day, "Keep + fix library first": the formats read the region's alone,
    /// so <c>ar-001</c> took the world's 24-hour cycle and <c>fr-CA</c> Canada's 12-hour one.)</i>
    /// </summary>
    /// <param name="language">The locale's language subtag.</param>
    /// <param name="region">The locale's region, its likely one where it names none.</param>
    // Hairs-AI:           Origin=AI; Spec=UTS35 Part 4 Time Data locale then region for ECMA-402 sec-intl.datetimeformat-internal-slots hourCycle, JSD-0082 row T10 (C5); IP=Low; Security=Low; Resources=1; Fingerprint=A7ECE8
    // Hairs-Human:        PENDING
    internal RegionHourCycles? HourCycles(string language, string region) =>
        data.HourCycles(language + "-" + region) ?? data.HourCycles(region) ?? data.HourCycles("001");

    /// <summary>The hour character time data prefers: <c>h</c>, <c>H</c>, <c>K</c> or <c>k</c>, and <c>H</c> where there is none.</summary>
    // Hairs-AI:           Origin=AI; Spec=ECMA-402 sec-intl.datetimeformat-internal-slots hourCycle over UTS35 Part 4 Time Data preferred, JSD-0082 row T10 (C5); IP=Low; Security=Low; Resources=1; Fingerprint=381D46
    // Hairs-Human:        PENDING
    internal static char PreferredHourChar(RegionHourCycles? cycles) =>
        cycles is { } time && time.Preferred.Length != 0 ? time.Preferred[0] : 'H';

    /// <summary>
    /// The hour character of a 12-hour cycle (<c>h</c> or <c>K</c>) or 24-hour cycle (<c>H</c> or <c>k</c>), the
    /// [[hourCycle12]] or [[hourCycle24]] of time data already found - a locale's, a region's or the world's (see
    /// <see cref="HourCycles(string, string)"/>) - where it prefers a cycle of the other kind: the first of that kind among
    /// its allowed formats, and <c>h</c> or <c>H</c> where there is no time data or none of them is of that kind (row T10).
    /// CLDR allows Japan's <c>H K h</c>, so <c>ja</c>'s 12-hour cycle is <c>h11</c>, as test262's
    /// <c>intl402/DateTimeFormat/prototype/resolvedOptions/hourCycle-default.js</c> asks; every other region of CLDR 48.2.0's
    /// time data allows <c>h</c> before any <c>K</c>, and none allows <c>k</c>.
    /// <i>(Added 2026-11-09, with <see cref="HourCycles(string, string)"/>, by the owner's decision of that day, "Keep + fix
    /// library first"; revised the same day, at review, when the instance overloads <c>AllowedHourChar(string region, bool
    /// twelveHour)</c> and <c>PreferredHourChar(string region)</c>, which read a region's time data alone and which nothing
    /// called any more, were removed. This summary read "As <c>AllowedHourChar(string, bool)</c>, over time data already found:
    /// a locale's, a region's or the world's."; the removed overload's read "The hour character of a region's 12-hour cycle
    /// (<c>h</c> or <c>K</c>) or 24-hour cycle (<c>H</c> or <c>k</c>), its [[hourCycle12]] or [[hourCycle24]] where the region
    /// prefers a cycle of the other kind: the first of that kind among CLDR's allowed formats for the region, the world's where
    /// the region has no time data, and <c>h</c> or <c>H</c> where none of them is of that kind (row T10)", and
    /// <c>PreferredHourChar(string)</c>'s "The hour character a region prefers - <c>h</c>, <c>H</c>, <c>K</c> or <c>k</c> -
    /// from CLDR's time data, the world's where the region has none.")</i>
    /// </summary>
    /// <param name="time">The time data that applies, or <see langword="null"/> where there is none.</param>
    /// <param name="twelveHour">Whether the 12-hour cycle is asked for, else the 24-hour one.</param>
    // Hairs-AI:           Origin=AI; Spec=ECMA-402 sec-intl.datetimeformat-internal-slots hourCycle12 and hourCycle24 over UTS #35 Time Data allowed formats, JSD-0082 row T10 (C5); IP=Low; Security=Low; Resources=1; Fingerprint=E4A52D
    // Hairs-Human:        PENDING
    internal static char AllowedHourChar(RegionHourCycles? time, bool twelveHour)
    {
        if (time is { } cycles)
        {
            foreach (var format in cycles.Allowed.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var c = format[0];
                if (twelveHour ? c is 'h' or 'K' : c is 'H' or 'k')
                {
                    return c;
                }
            }
        }

        return twelveHour ? 'h' : 'H';
    }
}
