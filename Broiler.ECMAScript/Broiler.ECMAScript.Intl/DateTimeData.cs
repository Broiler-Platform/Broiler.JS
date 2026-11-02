// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   13
// Annotated:        13/13
// Exempt:           8
// Human-reviewed:   0/13
// IP risk:          Low
// Security risk:    Medium
// Criteria:         0/0
// Resource impact:  2/10 max
// Unverified:       13
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
// Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 provider for sub-area (d), no table read outside the provider; IP=Low; Security=Low; Resources=0; Fingerprint=46C18D
// Broiler-Human:        PENDING
public sealed class DateTimeData
{
    private readonly Lazy<ZoneNamer> zoneNames;

    /// <summary>Creates the date-time data over an engine's providers; nothing is read until a format needs it.</summary>
    /// <param name="data">The locale data, ordinarily a <see cref="CompositeLocaleData"/>.</param>
    /// <param name="timeZones">The IANA Time Zone Database the formats' zones are found in.</param>
    /// <param name="calendars">The calendars a format other than the Gregorian and ISO 8601 ones writes its fields in.</param>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 provider for sub-area (d), no table read outside the provider; IP=Low; Security=Low; Resources=1; Fingerprint=72F008
    // Broiler-Human:        PENDING
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
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 provider for sub-area (d), no table read outside the provider; IP=Low; Security=Low; Resources=0; Fingerprint=CBD23D
    // Broiler-Human:        PENDING
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
// Broiler-AI:           Origin=Ported; Spec=JSD-0082 C5 data reads at the data locale (D5) via Broiler.VM JsDateData; IP=Low; Security=Low; Resources=0; Fingerprint=ECB0AE
// Broiler-Human:        PENDING
internal sealed class DateData
{
    private readonly ILocaleData data;

    private readonly Lock gate = new();

    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 C5 data reads at the data locale (D5) via Broiler.VM JsDateData; IP=Low; Security=Low; Resources=0; Fingerprint=2AC135
    // Broiler-Human:        PENDING
    private readonly Dictionary<string, DateTimePatternGenerator> generators = new(StringComparer.Ordinal);

    internal DateData(ILocaleData data) => this.data = data;

    /// <summary>The locale data.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 C5 data reads at the data locale (D5) via Broiler.VM JsDateData; IP=Low; Security=Low; Resources=0; Fingerprint=CBB118
    // Broiler-Human:        PENDING
    internal ILocaleData Locale => data;

    /// <summary>The locale of a data language: <c>en</c> for <c>en@hebrew</c>.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 C5 data reads at the data locale (D5) via Broiler.VM JsDateData; IP=Low; Security=Low; Resources=1; Fingerprint=2423B2
    // Broiler-Human:        PENDING
    internal static string LanguageOf(string dataLanguage)
    {
        var at = dataLanguage.IndexOf('@', StringComparison.Ordinal);
        return at < 0 ? dataLanguage : dataLanguage[..at];
    }

    /// <summary>The calendar of a data language: <c>gregory</c> for <c>en</c>, <c>hebrew</c> for <c>en@hebrew</c>.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 C5 data reads at the data locale (D5) via Broiler.VM JsDateData; IP=Low; Security=Low; Resources=1; Fingerprint=C33AE3
    // Broiler-Human:        PENDING
    private static string CalendarOf(string dataLanguage)
    {
        var at = dataLanguage.IndexOf('@', StringComparison.Ordinal);
        return at < 0 ? "gregory" : dataLanguage[(at + 1)..];
    }

    /// <summary>A data language's value for a key, or nothing.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 C5 data reads at the data locale (D5) via Broiler.VM JsDateData; IP=Low; Security=Low; Resources=1; Fingerprint=AC40D5
    // Broiler-Human:        PENDING
    internal string? Value(string dataLanguage, string key) => data.DateValue(LanguageOf(dataLanguage), CalendarOf(dataLanguage), key);

    /// <summary>Every value of a data language, in the order the decoding left them, or nothing.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 C5 data reads at the data locale (D5) via Broiler.VM JsDateData; IP=Low; Security=Low; Resources=1; Fingerprint=8424B6
    // Broiler-Human:        PENDING
    internal IReadOnlyDictionary<string, string>? Values(string dataLanguage) => data.DateValues(LanguageOf(dataLanguage), CalendarOf(dataLanguage));

    /// <summary>A language's day period rules, or nothing; a calendar's data is its language's for day periods, which belong to no calendar.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 C5 data reads at the data locale (D5) via Broiler.VM JsDateData; IP=Low; Security=Low; Resources=1; Fingerprint=0A5911
    // Broiler-Human:        PENDING
    internal IReadOnlyList<DayPeriodRule>? DayPeriods(string dataLanguage) => data.DayPeriods(LanguageOf(dataLanguage));

    /// <summary>The pattern generator of a data language whose default hour character is <paramref name="hourChar"/>.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 C5 data reads at the data locale (D5) via Broiler.VM JsDateData; IP=Low; Security=Medium; Resources=2; Fingerprint=000431
    // Broiler-Human:        PENDING
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
    /// The hour character a region prefers - <c>h</c>, <c>H</c>, <c>K</c> or <c>k</c> - from CLDR's time data, the world's
    /// where the region has none.
    /// </summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 C5 data reads at the data locale (D5) via Broiler.VM JsDateData; IP=Low; Security=Low; Resources=1; Fingerprint=8E92E1
    // Broiler-Human:        PENDING
    internal char PreferredHourChar(string region)
    {
        if ((data.HourCycles(region) ?? data.HourCycles("001")) is not { } cycles)
        {
            return 'H';
        }

        return cycles.Preferred.Length != 0 ? cycles.Preferred[0] : 'H';
    }
}
