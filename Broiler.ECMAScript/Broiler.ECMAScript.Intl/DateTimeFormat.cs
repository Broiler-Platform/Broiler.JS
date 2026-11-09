// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// HAIRS Code Assurance
// --------------------
// Relevant units:   25
// Annotated:        25/25
// Exempt:           17
// Human-reviewed:   0/25
// IP risk:          Low
// Security risk:    High
// Criteria:         5/5
// Resource impact:  2/10 max
// Unverified:       25
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading;
using Broiler.ECMAScript.Temporal;
using Broiler.ECMAScript.Time;

namespace Broiler.ECMAScript.Intl;

/// <summary>
/// One <c>Intl.DateTimeFormat</c>'s resolved state: CreateDateTimeFormat after the reads (ECMA-402 <c>sec-createdatetimeformat</c>,
/// as <c>tc39/proposal-temporal@e8cc03fc</c>'s <c>intl.html</c> amends it), its formatter, the formats of Temporal's types,
/// HandleDateTimeValue over Temporal records, and what <c>resolvedOptions</c> reports (Broiler.VM's JSD-0082, sub-area (d),
/// rows T1 to T42 and Z8).
/// </summary>
/// <remarks>
/// <para>
/// <b>The engine reads the options; the library takes the decisions after them.</b> An engine runs CanonicalizeLocaleList, reads
/// <c>localeMatcher</c>, <c>calendar</c> (checking it with <see cref="LocaleId.IsUnicodeType"/>), <c>numberingSystem</c>,
/// <c>hour12</c> and <c>hourCycle</c>, and runs ResolveLocale with the <c>ca</c>, <c>hc</c> and <c>nu</c> keys and the values
/// <see cref="CalendarKeyValues"/>, <see cref="HourCycleKeyValues"/> and <see cref="NumberingSystemKeyValues"/> give; it reads
/// <c>timeZone</c> and resolves it (<see cref="ResolvedTimeZone.Resolve"/>), reads the components, <c>formatMatcher</c>,
/// <c>dateStyle</c> and <c>timeStyle</c>, and hands what it read to <see cref="Create"/>, which answers the steps' last two
/// <c>TypeError</c>s as statuses. The bound <c>format</c>, the legacy constructor (row T5), ToDateTimeFormattable's ToNumber and
/// the reading of a Temporal object's slots, the parts arrays, and the default zone (row T11) stay the engine's.
/// </para>
/// <para>
/// <b>Ported from Broiler.VM's realm</b> (<c>JsRealm.DateTimeFormat.cs</c>: CreateDateTimeFormat from ResolveLocale on,
/// TemporalFormatPattern, AdjustStylePattern, the hour-cycle letters, ReplaceHourCycle, SkeletonOfComponents, StylePattern,
/// ComponentsOf, HandleDateTimeValue and the range's checks, and <c>JsTemporalDateFormats</c>) at its SJL-9 rule commit
/// <c>4cf9ca44</c>, with each <c>RangeError</c> and <c>TypeError</c> a status, and the locale data read at the data locale
/// where Broiler.VM read the data locale's language, which for its tables reads the same lines. The resolved time zone
/// answers both the identifier and its primary (decision D15): [[TimeZone]] is the identifier, the zone is named by the
/// primary.
/// </para>
/// <para><b>Immutable</b> once made, but for the formats of Temporal's types, each made under a lock the first time it is
/// asked for: one may be shared between threads (convention 10).</para>
/// </remarks>
// Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-createdatetimeformat as amended by proposal-temporal e8cc03fc via Broiler.VM JsRealm.DateTimeFormat; IP=Low; Security=Low; Resources=0; Fingerprint=8D4870
// Hairs-Human:        PENDING
public sealed class DateTimeFormat
{
    /// <summary>
    /// The locale data's <c>ca</c> values: the Gregorian calendar first, as every supported locale's default, then the others,
    /// and the deprecated Hijri types the constructor falls back from (Broiler.VM's <c>CalendarKeyValues</c>).
    /// </summary>
    public static IReadOnlyList<string?> CalendarKeyValues { get; } =
    [
        "gregory", "buddhist", "chinese", "coptic", "dangi", "ethioaa", "ethiopic", "hebrew", "indian", "islamic",
        "islamic-civil", "islamic-rgsa", "islamic-tbla", "islamic-umalqura", "iso8601", "japanese", "persian", "roc",
    ];

    /// <summary>The <c>hc</c> values: ECMA-402's <b>null</b> first, the locale's default, then the four hour cycles.</summary>
    public static IReadOnlyList<string?> HourCycleKeyValues { get; } = [null, "h11", "h12", "h23", "h24"];

    private readonly DateTimeData data;

    private readonly Func<string, DateTimeFormatter?> temporalBuild;

    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-createdatetimeformat as amended by proposal-temporal e8cc03fc via Broiler.VM JsRealm.DateTimeFormat; IP=Low; Security=Low; Resources=0; Fingerprint=DDA092
    // Hairs-Human:        PENDING
    private readonly Dictionary<string, DateTimeFormatter?> temporal = new(StringComparer.Ordinal);

    private readonly Lock gate = new();

    private DateTimeFormat(DateTimeData data, DateTimeFormatter formatter, Func<string, DateTimeFormatter?> temporalBuild)
    {
        this.data = data;
        Formatter = formatter;
        this.temporalBuild = temporalBuild;
    }

    /// <summary>[[Locale]].</summary>
    public string Locale { get; private init; } = string.Empty;

    /// <summary>[[Calendar]]: the resolved calendar, <c>islamic</c> and <c>islamic-rgsa</c> resolved to <c>islamic-tbla</c> (row T33).</summary>
    public string Calendar { get; private init; } = "gregory";

    /// <summary>[[NumberingSystem]].</summary>
    public string NumberingSystem { get; private init; } = "latn";

    /// <summary>[[TimeZone]]: the identifier as given, in the database's case (decision D15).</summary>
    public string TimeZone => Formatter.TimeZone;

    /// <summary>
    /// [[HourCycle]] as <c>resolvedOptions</c> reports it: <see langword="null"/> for a format whose pattern has no hour, as
    /// test262 at <c>ccaac100</c> asserts and both engines answer (row T8, decision D33 (b)).
    /// </summary>
    public string? HourCycle { get; private init; }

    /// <summary><c>hour12</c> as <c>resolvedOptions</c> reports it: whether [[HourCycle]] is <c>h11</c> or <c>h12</c>; <see langword="null"/> where it is.</summary>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-createdatetimeformat as amended by proposal-temporal e8cc03fc via Broiler.VM JsRealm.DateTimeFormat; IP=Low; Security=Low; Resources=0; Fingerprint=6C74C9
    // Hairs-Human:        PENDING
    public bool? Hour12 => HourCycle is null ? null : HourCycle is "h11" or "h12";

    /// <summary>[[DateStyle]], or <see langword="null"/>.</summary>
    public string? DateStyle { get; private init; }

    /// <summary>[[TimeStyle]], or <see langword="null"/>.</summary>
    public string? TimeStyle { get; private init; }

    /// <summary>Whether the format is <c>Temporal.ZonedDateTime.prototype.toLocaleString</c>'s.</summary>
    public bool ForZonedDateTime { get; private init; }

    /// <summary>The formatter of a Number's time value: [[DateTimeFormat]]'s pattern in the resolved zone.</summary>
    public DateTimeFormatter Formatter { get; }

    /// <summary>
    /// The components <c>resolvedOptions</c> reports, in Table 16's order (<c>sec-intl.datetimeformat.prototype.resolvedoptions</c>):
    /// those of the selected format's pattern (row T7), <c>fractionalSecondDigits</c> as its digit count; none for a style.
    /// </summary>
    public IReadOnlyList<KeyValuePair<string, string>> ResolvedComponents { get; private init; } = [];

    // ---- the relevant keys ----------------------------------------------------------------------------------------------

    /// <summary>
    /// The <c>nu</c> values for a locale ([[LocaleData]].[[&lt;locale&gt;]].[[nu]]): the locale's default numbering system first,
    /// then <c>latn</c>, then the data's others, as <see cref="NumberFormatter.NumberingSystemKeyValues"/> gives them (decision
    /// D13). For Broiler.VM's tables, whose locales' default is <c>latn</c>, they are Broiler.VM's list, <c>latn</c> first.
    /// </summary>
    /// <param name="data">The locale data, ordinarily a <see cref="CompositeLocaleData"/>.</param>
    /// <param name="locale">The matched available locale.</param>
    /// <returns>The values, each once.</returns>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-intl.datetimeformat-internal-slots nu values, JSD-0082 D13; IP=Low; Security=Low; Resources=1; Fingerprint=34508B
    // Hairs-Human:        PENDING
    public static IReadOnlyList<string?> NumberingSystemKeyValues(ILocaleData data, string locale) => NumberFormatter.NumberingSystemKeyValues(data, locale);

    /// <summary>
    /// The <c>ca</c> values for a locale ([[LocaleData]].[[&lt;locale&gt;]].[[ca]], <c>sec-intl.datetimeformat-internal-slots</c>,
    /// whose first element is the locale's default calendar): the first calendar of CLDR's calendar preferences for the locale's
    /// region (UTS #35 Part 4, Calendar Preference Data) that is one of <see cref="CalendarKeyValues"/>, then the others in
    /// <see cref="CalendarKeyValues"/>' order - <c>persian</c> first for <c>fa</c>, whose likely region is IR, and <c>buddhist</c>
    /// for <c>th</c>. Where the data states no preference for the region it reads the world's, <c>001</c>; where it states none at
    /// all - Broiler.VM's tables, which carry no <see cref="IntlTable.CalendarPreferences"/> table - or the first preferred is
    /// <c>gregory</c>, the answer is <see cref="CalendarKeyValues"/> itself, the Gregorian calendar first. An engine hands it to
    /// ResolveLocale as the <c>ca</c> key's values for the matched locale, as it hands <see cref="NumberingSystemKeyValues"/> for
    /// <c>nu</c>; the <c>-u-ca-</c> keyword and the <c>calendar</c> option still choose over it.
    /// <i>(Added 2026-11-09, by the owner's decision of that day, "Keep + fix library first": the default calendar was the
    /// Gregorian for every locale.)</i>
    /// </summary>
    /// <param name="data">The locale data, ordinarily a <see cref="CompositeLocaleData"/>.</param>
    /// <param name="locale">The matched available locale, as ResolveLocale's matching steps found it.</param>
    /// <returns>The values, each once, the default first.</returns>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    // Hairs-AI:           Origin=AI; Spec=ECMA-402 sec-intl.datetimeformat-internal-slots ca values over UTS35 Part 4 calendarPreferenceData; IP=Low; Security=Low; Resources=1; Fingerprint=302EFA
    // Hairs-Human:        PENDING
    public static IReadOnlyList<string?> CalendarKeyValuesOf(ILocaleData data, string locale)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(locale);

        var (_, region) = LanguageAndRegion(data, locale);
        var preferences = data.CalendarPreferences(region) ?? data.CalendarPreferences("001");
        string? first = null;

        foreach (var calendar in preferences ?? [])
        {
            if (Contains(CalendarKeyValues, calendar))
            {
                first = calendar;
                break;
            }
        }

        if (first is null || first == "gregory")
        {
            return CalendarKeyValues;
        }

        var values = new List<string?>(CalendarKeyValues.Count) { first };

        foreach (var calendar in CalendarKeyValues)
        {
            if (calendar != first)
            {
                values.Add(calendar);
            }
        }

        return values.AsReadOnly();
    }

    /// <summary>
    /// A locale's language and region: its own region, else the region CLDR's likely subtags give the whole locale (UTS #35 Part 1,
    /// Likely Subtags: <c>zh-Hant</c> is <c>zh-Hant-TW</c>), else <c>001</c>; <c>en</c> for a locale that is not a well-formed tag.
    /// <i>(Added 2026-11-09, by the owner's decision of that day, "Keep + fix library first": the region was the likely one of the
    /// language alone, so <c>zh-Hant</c> read China's.)</i>
    /// </summary>
    // Hairs-AI:           Origin=AI; Spec=UTS35 Part 1 Add Likely Subtags for ECMA-402 sec-intl.datetimeformat-internal-slots locale data; IP=Low; Security=Low; Resources=1; Fingerprint=8BE13E
    // Hairs-Human:        PENDING
    private static (string Language, string Region) LanguageAndRegion(ILocaleData data, string locale)
    {
        if (LocaleTag.Parse(locale) is not { } tag)
        {
            return ("en", "001");
        }

        if (tag.Region is { Length: > 0 } region)
        {
            return (tag.Language, region);
        }

        var maximal = LocaleTag.Parse(locale)!;
        return (tag.Language, maximal.Maximize(data) && maximal.Region is { Length: > 0 } likely ? likely : "001");
    }

    /// <summary>
    /// The decimal separator a numbering system writes (decision D21): its own symbol at the data locale, then at root, where the
    /// data carries it, then the data locale's <c>latn</c> one, then <c>.</c>, as <see cref="NumberFormatter"/> reads a symbol.
    /// <i>(Added 2026-11-09, by the owner's decision of that day, "Keep + fix library first".)</i>
    /// </summary>
    // Hairs-AI:           Origin=AI; Spec=ECMA-402 sec-formatdatetimepattern fractionalSecondDigits separator over UTS35 Part 3 numbering-system symbols, JSD-0082 D21; IP=Low; Security=Low; Resources=1; Fingerprint=73096B
    // Hairs-Human:        PENDING
    private static string DecimalSymbol(ILocaleData data, string dataLocale, string numberingSystem)
    {
        if (numberingSystem != "latn")
        {
            var own = "symbols@" + numberingSystem + ".decimal";

            if ((data.NumberValue(dataLocale, own) ?? data.NumberValue("und", own)) is { } stated)
            {
                return stated;
            }
        }

        return data.NumberValue(dataLocale, "symbols.decimal") ?? ".";
    }

    /// <summary>Whether a list of key values holds a value, compared ordinally.</summary>
    // Hairs-AI:           Origin=AI; Spec=ECMA-402 sec-intl.datetimeformat-internal-slots ca values; IP=Low; Security=Low; Resources=1; Fingerprint=AFF895
    // Hairs-Human:        PENDING
    private static bool Contains(IReadOnlyList<string?> values, string value)
    {
        foreach (var candidate in values)
        {
            if (candidate == value)
            {
                return true;
            }
        }

        return false;
    }

    // ---- making one -----------------------------------------------------------------------------------------------------

    /// <summary>
    /// CreateDateTimeFormat's steps after ResolveLocale and the reads (<c>sec-createdatetimeformat</c>, as <c>e8cc03fc</c>
    /// amends it): the hour cycle, the calendar's data, the pattern of the styles or of the components with the defaults, the
    /// numbering system's digits and the zone's names.
    /// </summary>
    /// <param name="data">The engine's date-time data.</param>
    /// <param name="resolved">ResolveLocale's answer over the <c>ca</c>, <c>hc</c> and <c>nu</c> keys, the <c>hc</c> key's option
    /// <b>null</b> where <c>hour12</c> was given.</param>
    /// <param name="options">What the constructor read after ResolveLocale.</param>
    /// <param name="format">The format, or <see langword="null"/> when it is refused.</param>
    /// <returns><see cref="IntlStatus.Completed"/>; or a <c>TypeError</c>'s status: <see cref="IntlStatus.StyleWithComponents"/>,
    /// <see cref="IntlStatus.DateFormatWithTimeStyle"/> or <see cref="IntlStatus.TimeFormatWithDateStyle"/>.</returns>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    /// <exception cref="ArgumentException">A component, style or key value outside the values ECMA-402 admits: the engine's
    /// GetOption and ResolveLocale, which come first, would not give it.</exception>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-createdatetimeformat as amended by proposal-temporal e8cc03fc via Broiler.VM JsRealm.DateTimeFormat, hourCycle12 and hourCycle24 by CLDR's allowed formats (JSD-0082 row T10); IP=Low; Security=High; Resources=2; Fingerprint=CB81DE
    // Hairs-Falsified-If: a resolved option differs from CreateDateTimeFormat's as amended by proposal-temporal e8cc03fc, or a refusal is another status
    // Hairs-Human:        PENDING
    public static IntlStatus Create(DateTimeData data, ResolvedLocale resolved, DateTimeFormatOptions options, out DateTimeFormat? format)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(resolved);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.TimeZone, nameof(options));
        ArgumentNullException.ThrowIfNull(options.Components, nameof(options));
        Check(options);
        format = null;

        var locale = data.Data;
        var dates = data.Dates;
        var keys = resolved.KeyValues;
        string? Key(string key) => keys.TryGetValue(key, out var value) ? value : null;

        var dataLocale = resolved.DataLocale;

        // THE DATA LOCALE'S REGION is its own, else the one CLDR's likely subtags give the whole locale - zh-Hant's TW, where its
        // language alone gives CN - else the world's (UTS #35 Part 1, Likely Subtags; added 2026-11-09, by the owner's decision of
        // that day, "Keep + fix library first"). It read: "var region = tag?.Region is { Length: > 0 } explicitRegion ? explicitRegion
        // : locale.LikelySubtags(language) is { } likely ? likely[(likely.LastIndexOf('-') + 1)..] : "001";".
        var (language, region) = LanguageAndRegion(locale, dataLocale);

        // THE HOUR CYCLES are CLDR's time data for the locale's language and region where it states them, then the region's (UTS #35
        // Part 4, Time Data; row T10): ar-001 prefers h and fr-CA H (added 2026-11-09, by the same decision). It read: "var preferred =
        // dates.PreferredHourChar(region);", the region's alone.
        var time = dates.HourCycles(language, region);
        var preferred = DateData.PreferredHourChar(time);
        var defaultCycle = HourCycleOfChar(preferred);

        // [[hourCycle12]] AND [[hourCycle24]] are the locale's preferred cycle (its language and region's time data, then its
        // region's, then the world's) where it is of the kind asked for, and otherwise the first of that kind that time data
        // allows (row T10): Japan allows "H K h", so ja's is h11, as test262's hourCycle-default.js asks. Broiler.VM read "h12"
        // and "h23" there, the same answers for its de and en. (Revised 2026-11-09, at review of the owner's decision of that
        // day, "Keep + fix library first": it read "are the region's preferred cycle where it is of the kind asked for, and
        // otherwise the first of that kind CLDR's time data allows for the region (row T10)".)
        var hc = options.Hour12 switch
        {
            true => defaultCycle is "h11" or "h12" ? defaultCycle : HourCycleOfChar(DateData.AllowedHourChar(time, twelveHour: true)),
            false => defaultCycle is "h23" or "h24" ? defaultCycle : HourCycleOfChar(DateData.AllowedHourChar(time, twelveHour: false)),
            null => Key("hc") ?? defaultCycle,
        };

        var components = new Dictionary<string, string>(StringComparer.Ordinal);
        var hasExplicitComponents = false;

        foreach (var name in DateTimeComponents.Names)
        {
            if (options.Components.Value(name) is { } value)
            {
                components[name] = value;
                hasExplicitComponents = true;
            }
        }

        var dateStyle = options.DateStyle;
        var timeStyle = options.TimeStyle;
        var hcChar = CharOfHourCycle(hc);
        var numberingSystem = Key("nu") ?? "latn";

        // THE DECIMAL SEPARATOR of a fractional second is the numbering system's own (decision D21), read as the number formatter
        // reads a system's symbol: the number table's symbols@<system>.decimal at the data locale, then at root, then the data
        // locale's latn one, so en-u-nu-arab writes U+066B. Broiler.VM's tables carry no symbols@ key, so its formats write the latn
        // symbol as before. (Added 2026-11-09, by the owner's decision of that day, "Keep + fix library first"; it read
        // "var decimalSymbol = locale.NumberValue(dataLocale, "symbols.decimal") ?? ".";", the latn symbol alone.)
        var decimalSymbol = DecimalSymbol(locale, dataLocale, numberingSystem);
        var resolvedCalendar = Key("ca") ?? "gregory";

        // THE DEPRECATED HIJRI CALENDARS FALL BACK to the astronomical tabular one (the Intl era and month code proposal's
        // amendment of CreateDateTimeFormat, row T33).
        if (resolvedCalendar is "islamic" or "islamic-rgsa")
        {
            resolvedCalendar = "islamic-tbla";
        }

        // A CALENDAR'S DATA is the data locale's under its own layer: the ISO 8601 calendar's patterns, or CLDR's names and
        // patterns of another calendar.
        var dataLanguage = resolvedCalendar == "gregory" ? dataLocale : dataLocale + "@" + resolvedCalendar;
        (TemporalCalendars Calendars, CalendarId Id)? calendarSystem =
            resolvedCalendar is "gregory" or "iso8601" ? null : (data.Calendars, CalendarIdOf(resolvedCalendar));
        var generator = dates.Generator(dataLanguage, Key("hc") is { } keyword ? CharOfHourCycle(keyword) : preferred, decimalSymbol);
        var explicitComponents = new Dictionary<string, string>(components, StringComparer.Ordinal);
        string pattern;

        if (dateStyle is not null || timeStyle is not null)
        {
            if (hasExplicitComponents)
            {
                return IntlStatus.StyleWithComponents;
            }

            if (options.Required == DateTimeRequired.Date && timeStyle is not null)
            {
                return IntlStatus.DateFormatWithTimeStyle;
            }

            if (options.Required == DateTimeRequired.Time && dateStyle is not null)
            {
                return IntlStatus.TimeFormatWithDateStyle;
            }

            pattern = StylePattern(dates, dataLanguage, dateStyle, timeStyle, hc, hcChar, generator);
        }
        else
        {
            var needDefaults = true;

            if (options.Required is DateTimeRequired.Date or DateTimeRequired.Any)
            {
                needDefaults &= !(components.ContainsKey("weekday") || components.ContainsKey("year") ||
                    components.ContainsKey("month") || components.ContainsKey("day"));
            }

            if (options.Required is DateTimeRequired.Time or DateTimeRequired.Any)
            {
                needDefaults &= !(components.ContainsKey("dayPeriod") || components.ContainsKey("hour") ||
                    components.ContainsKey("minute") || components.ContainsKey("second") || components.ContainsKey("fractionalSecondDigits"));
            }

            if (needDefaults && options.Defaults is DateTimeDefaults.Date or DateTimeDefaults.All)
            {
                components["year"] = "numeric";
                components["month"] = "numeric";
                components["day"] = "numeric";
            }

            if (needDefaults && options.Defaults is DateTimeDefaults.Time or DateTimeDefaults.All)
            {
                components["hour"] = "numeric";
                components["minute"] = "numeric";
                components["second"] = "numeric";
            }

            pattern = ReplaceHourCycle(
                generator.BestPattern(SkeletonOfComponents(components, hcChar), DateTimePatternGenerator.MatchHourLength), hcChar);
        }

        var digitsOfSystem = Digits(locale, numberingSystem);
        var patternCycle = HourCycleOf(pattern);
        var intervalChar = patternCycle is null ? preferred : hcChar;
        var zone = options.TimeZone;

        // AN IANA ZONE'S NAMES are read with the locale's region's golden zones and names.
        var naming = zone.Kind == TimeZoneKind.Named
            ? new ZoneNaming(data.ZoneNames, region, code => locale.DisplayName(dataLocale, "region", code))
            : null;

        var formatter = new DateTimeFormatter(
            dates,
            dataLanguage,
            digitsOfSystem,
            zone,
            pattern,
            () => new DateIntervalFormatter(dates, dataLanguage, dates.Generator(dataLanguage, intervalChar, decimalSymbol), DateTimePatternGenerator.SkeletonOf(pattern)),
            calendarSystem,
            naming);

        // THE FORMATS OF TEMPORAL'S OBJECTS (the proposal's amendment of CreateDateTimeFormat), each built when one is first
        // formatted.
        var stylePattern = dateStyle is not null || timeStyle is not null ? pattern : null;
        var zoned = options.ForZonedDateTime;

        DateTimeFormatter Formatter(string formatPattern, bool plain)
        {
            var cycle = HourCycleOf(formatPattern) is null ? preferred : hcChar;
            return new DateTimeFormatter(
                dates,
                dataLanguage,
                digitsOfSystem,
                plain ? ResolvedTimeZone.Utc : zone,
                formatPattern,
                () => new DateIntervalFormatter(dates, dataLanguage, dates.Generator(dataLanguage, cycle, decimalSymbol), DateTimePatternGenerator.SkeletonOf(formatPattern)),
                calendarSystem,
                plain ? null : naming);
        }

        string Best(Dictionary<string, string> formatOptions) =>
            ReplaceHourCycle(generator.BestPattern(SkeletonOfComponents(formatOptions, hcChar), DateTimePatternGenerator.MatchHourLength), hcChar);

        DateTimeFormatter? Build(string kindOfValue)
        {
            string? formatPattern;

            if (stylePattern is not null)
            {
                formatPattern = kindOfValue switch
                {
                    "date" => dateStyle is null ? null : AdjustStylePattern(stylePattern, ["weekday", "era", "year", "month", "day"], Best),
                    "year-month" => dateStyle is null ? null : AdjustStylePattern(stylePattern, ["era", "year", "month"], Best),
                    "month-day" => dateStyle is null ? null : AdjustStylePattern(stylePattern, ["month", "day"], Best),
                    "time" => timeStyle is null ? null : AdjustStylePattern(stylePattern, ["dayPeriod", "hour", "minute", "second", "fractionalSecondDigits"], Best),
                    "date-time" => AdjustStylePattern(stylePattern, ["weekday", "era", "year", "month", "day", "dayPeriod", "hour", "minute", "second", "fractionalSecondDigits"], Best),
                    _ => stylePattern,
                };
            }
            else
            {
                formatPattern = kindOfValue switch
                {
                    "date" => TemporalFormatPattern(explicitComponents, "date", "date", inheritAll: false, Best),
                    "year-month" => TemporalFormatPattern(explicitComponents, "year-month", "year-month", inheritAll: false, Best),
                    "month-day" => TemporalFormatPattern(explicitComponents, "month-day", "month-day", inheritAll: false, Best),
                    "time" => TemporalFormatPattern(explicitComponents, "time", "time", inheritAll: false, Best),
                    "date-time" => TemporalFormatPattern(explicitComponents, "any", "all", inheritAll: false, Best),
                    _ => TemporalFormatPattern(explicitComponents, "any", zoned ? "zoned-date-time" : "all", inheritAll: true, Best),
                };
            }

            return formatPattern is null ? null : Formatter(formatPattern, plain: kindOfValue != "instant");
        }

        format = new DateTimeFormat(data, formatter, Build)
        {
            Locale = resolved.Locale,
            Calendar = resolvedCalendar,
            NumberingSystem = numberingSystem,
            HourCycle = patternCycle is null ? null : hc,
            DateStyle = dateStyle,
            TimeStyle = timeStyle,
            ForZonedDateTime = zoned,
            ResolvedComponents = dateStyle is null && timeStyle is null ? Resolved(pattern) : [],
        };

        return IntlStatus.Completed;
    }

    /// <summary>The checks of a precondition the engine's reads hold: each value one ECMA-402 admits.</summary>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-createdatetimeformat as amended by proposal-temporal e8cc03fc via Broiler.VM JsRealm.DateTimeFormat; IP=Low; Security=Medium; Resources=1; Fingerprint=F60779
    // Hairs-Human:        PENDING
    private static void Check(DateTimeFormatOptions options)
    {
        foreach (var name in DateTimeComponents.Names)
        {
            if (name == "fractionalSecondDigits")
            {
                if (options.Components.FractionalSecondDigits is < 1 or > 3)
                {
                    throw new ArgumentException("fractionalSecondDigits is 1 to 3", nameof(options));
                }

                continue;
            }

            if (options.Components.Value(name) is { } value && Array.IndexOf(DateTimeComponents.Admitted(name)!, value) < 0)
            {
                throw new ArgumentException("the component " + name + " admits no value " + value, nameof(options));
            }
        }

        foreach (var style in new[] { options.DateStyle, options.TimeStyle })
        {
            if (style is not null and not ("full" or "long" or "medium" or "short"))
            {
                throw new ArgumentException("a style is full, long, medium or short", nameof(options));
            }
        }
    }

    /// <summary>A calendar type's identifier: the shared calendars' canonical one.</summary>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-createdatetimeformat as amended by proposal-temporal e8cc03fc via Broiler.VM JsRealm.DateTimeFormat; IP=Low; Security=Low; Resources=1; Fingerprint=5BD4B8
    // Hairs-Human:        PENDING
    private static CalendarId CalendarIdOf(string calendar) =>
        TemporalCalendars.Canonicalize(calendar, out var id) == CalendarStatus.Completed
            ? id
            : throw new ArgumentException("the ca key resolved to a calendar that is not one of CalendarKeyValues: " + calendar);

    /// <summary>A numbering system's ten digits, the <c>latn</c> ones where the data gives the system none.</summary>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-intl.datetimeformat-internal-slots nu values, JSD-0082 D13; IP=Low; Security=Low; Resources=1; Fingerprint=94AF7E
    // Hairs-Human:        PENDING
    private static string[] Digits(ILocaleData data, string system)
    {
        if ((data.NumberingSystemDigits(system) ?? data.NumberingSystemDigits("latn")) is { Count: 10 } digits)
        {
            var copied = new string[10];

            for (var i = 0; i < 10; i++)
            {
                copied[i] = digits[i];
            }

            return copied;
        }

        return ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9"];
    }

    // ---- the values a format writes ---------------------------------------------------------------------------------------

    /// <summary>
    /// The proposal's HandleDateTimeValue (<c>sec-handledatetimevalue</c>, and its HandleDateTimeTemporal* operations): the
    /// formatter a value is written in, and its time in milliseconds. A Number is clipped; a plain Temporal value's format is
    /// written at UTC, so no zone moves it (<c>377fb80e</c>), and a date's time is its noon.
    /// </summary>
    /// <param name="value">The value, as ToDateTimeFormattable gave it.</param>
    /// <param name="formatter">The formatter, or <see langword="null"/> when the value is refused.</param>
    /// <param name="time">The time value to write.</param>
    /// <returns><see cref="IntlStatus.Completed"/>; <see cref="IntlStatus.TimeValueOutOfRange"/> (<c>RangeError</c>) for a Number
    /// TimeClip refuses; <see cref="IntlStatus.TemporalCalendarMismatch"/> (<c>RangeError</c>) for a value in a calendar the
    /// format does not write, checked first (row T39); <see cref="IntlStatus.TemporalTypeNotFormatted"/> (<c>TypeError</c>) for a
    /// type the options give no field of; or <see cref="IntlStatus.ZonedDateTimeNotFormatted"/> (<c>TypeError</c>) for a
    /// ZonedDateTime, which a format other than its own <c>toLocaleString</c>'s does not write.</returns>
    // Hairs-AI:           Origin=Ported; Spec=proposal-temporal e8cc03fc sec-temporal-handledatetimevalue sec-temporal-getdatetimeformat sec-temporal-adjustdatetimestyleformat via Broiler.VM JsRealm.DateTimeFormat; IP=Low; Security=High; Resources=2; Fingerprint=D0B2D8
    // Hairs-Falsified-If: a value is formatted with another format than HandleDateTimeValue selects, or one it refuses is formatted
    // Hairs-Human:        PENDING
    public IntlStatus HandleDateTimeValue(DateTimeValue value, out DateTimeFormatter? formatter, out double time)
    {
        formatter = null;
        time = double.NaN;

        if (!value.IsTemporal)
        {
            var clipped = EcmaTime.TimeClip(value.Time);

            if (double.IsNaN(clipped))
            {
                return IntlStatus.TimeValueOutOfRange;
            }

            formatter = Formatter;
            time = clipped;
            return IntlStatus.Completed;
        }

        static double Milliseconds(BigInteger epochNs)
        {
            var quotient = BigInteger.DivRem(epochNs, 1_000_000, out var remainder);
            return (double)(remainder.Sign < 0 ? quotient - 1 : quotient);
        }

        static double Noon(IsoDate date) =>
            Milliseconds(IsoArithmetic.GetUtcEpochNanoseconds(new IsoDateTime(date, new IsoTime(12, 0, 0, 0, 0, 0))));

        bool SameCalendar(bool isoAllowed) => value.Calendar == Calendar || (isoAllowed && value.Calendar == "iso8601");

        IntlStatus Required(string kind, double at, out DateTimeFormatter? found, out double written)
        {
            found = TemporalFormat(kind);
            written = found is null ? double.NaN : at;
            return found is null ? IntlStatus.TemporalTypeNotFormatted : IntlStatus.Completed;
        }

        switch (value.Kind)
        {
            case DateTimeValueKind.PlainDate:
                return !SameCalendar(isoAllowed: true) ? IntlStatus.TemporalCalendarMismatch : Required("date", Noon(value.Date), out formatter, out time);
            case DateTimeValueKind.PlainYearMonth:
                return !SameCalendar(isoAllowed: false) ? IntlStatus.TemporalCalendarMismatch : Required("year-month", Noon(value.Date), out formatter, out time);
            case DateTimeValueKind.PlainMonthDay:
                return !SameCalendar(isoAllowed: false) ? IntlStatus.TemporalCalendarMismatch : Required("month-day", Noon(value.Date), out formatter, out time);
            case DateTimeValueKind.PlainTime:
                return Required("time", Milliseconds(IsoArithmetic.GetUtcEpochNanoseconds(new IsoDateTime(new IsoDate(1970, 1, 1), value.TimeOfDay))), out formatter, out time);
            case DateTimeValueKind.PlainDateTime:
                return !SameCalendar(isoAllowed: true)
                    ? IntlStatus.TemporalCalendarMismatch
                    : Required("date-time", Milliseconds(IsoArithmetic.GetUtcEpochNanoseconds(new IsoDateTime(value.Date, value.TimeOfDay))), out formatter, out time);
            case DateTimeValueKind.Instant:
                return Required("instant", Milliseconds(value.EpochNanoseconds), out formatter, out time);
            default:
                // TEMPORAL.ZONEDDATETIME.PROTOTYPE.TOLOCALESTRING writes its own value through its Instant, after checking its
                // calendar; every other format refuses a ZonedDateTime.
                if (!ForZonedDateTime)
                {
                    return IntlStatus.ZonedDateTimeNotFormatted;
                }

                return value.Calendar != "iso8601" && value.Calendar != Calendar
                    ? IntlStatus.TemporalCalendarMismatch
                    : Required("instant", Milliseconds(value.EpochNanoseconds), out formatter, out time);
        }
    }

    /// <summary>
    /// The start and end a range method formats (<c>sec-intl.datetimeformat.prototype.formatRange</c>, as the proposal amends
    /// it): two Temporal values of one type or two Numbers, each through <see cref="HandleDateTimeValue"/>, the start's format
    /// writing both. The engine refuses an <c>undefined</c> start or end first.
    /// </summary>
    /// <param name="x">The start.</param>
    /// <param name="y">The end.</param>
    /// <param name="formatter">The formatter, or <see langword="null"/> when the range is refused.</param>
    /// <param name="xTime">The start's time value.</param>
    /// <param name="yTime">The end's time value.</param>
    /// <returns><see cref="IntlStatus.Completed"/>, <see cref="IntlStatus.RangeOfDifferentTypes"/> (<c>TypeError</c>,
    /// SameTemporalType), or a status of <see cref="HandleDateTimeValue"/>'s for the start, then for the end.</returns>
    // Hairs-AI:           Origin=Ported; Spec=proposal-temporal e8cc03fc sec-temporal-handledatetimevalue sec-temporal-getdatetimeformat sec-temporal-adjustdatetimestyleformat via Broiler.VM JsRealm.DateTimeFormat; IP=Low; Security=High; Resources=2; Fingerprint=DB73C7
    // Hairs-Falsified-If: a range of two values is formatted although they are of different types, or with another format than HandleDateTimeValue selects
    // Hairs-Human:        PENDING
    public IntlStatus HandleDateTimeRange(DateTimeValue x, DateTimeValue y, out DateTimeFormatter? formatter, out double xTime, out double yTime)
    {
        formatter = null;
        xTime = yTime = double.NaN;

        if ((x.IsTemporal || y.IsTemporal) && x.Kind != y.Kind)
        {
            return IntlStatus.RangeOfDifferentTypes;
        }

        var status = HandleDateTimeValue(x, out var start, out xTime);

        if (status != IntlStatus.Completed)
        {
            return status;
        }

        status = HandleDateTimeValue(y, out _, out yTime);

        if (status != IntlStatus.Completed)
        {
            xTime = double.NaN;
            return status;
        }

        formatter = start;
        return IntlStatus.Completed;
    }

    /// <summary>
    /// The format of a Temporal type ([[TemporalPlainDateFormat]] to [[TemporalInstantFormat]]): <c>date</c>, <c>year-month</c>,
    /// <c>month-day</c>, <c>time</c>, <c>date-time</c> or <c>instant</c>, or <see langword="null"/> where the options exclude
    /// the type; each built the first time it is asked for.
    /// </summary>
    // Hairs-AI:           Origin=Ported; Spec=proposal-temporal e8cc03fc sec-temporal-handledatetimevalue sec-temporal-getdatetimeformat sec-temporal-adjustdatetimestyleformat via Broiler.VM JsRealm.DateTimeFormat; IP=Low; Security=Medium; Resources=1; Fingerprint=B04E31
    // Hairs-Human:        PENDING
    internal DateTimeFormatter? TemporalFormat(string kind)
    {
        lock (gate)
        {
            if (!temporal.TryGetValue(kind, out var formatter))
            {
                formatter = temporalBuild(kind);
                temporal[kind] = formatter;
            }

            return formatter;
        }
    }

    /// <summary>The date-time data the format reads.</summary>
    internal DateTimeData Data => data;

    // ---- the patterns ------------------------------------------------------------------------------------------------------

    /// <summary>
    /// The proposal's GetDateTimeFormat (<c>sec-getdatetimeformat</c>) over the explicitly requested components: the pattern a
    /// Temporal type is written in, or nothing where the options name components and none of the type's own.
    /// </summary>
    // Hairs-AI:           Origin=Ported; Spec=proposal-temporal e8cc03fc sec-temporal-handledatetimevalue sec-temporal-getdatetimeformat sec-temporal-adjustdatetimestyleformat via Broiler.VM JsRealm.DateTimeFormat; IP=Low; Security=High; Resources=2; Fingerprint=2C1F8E
    // Hairs-Falsified-If: a Temporal type's pattern holds a field GetDateTimeFormat's required and defaults for that type exclude, or lacks one they include
    // Hairs-Human:        PENDING
    private static string? TemporalFormatPattern(Dictionary<string, string> options, string required, string defaults, bool inheritAll, Func<Dictionary<string, string>, string> best)
    {
        string[] requiredOptions = required switch
        {
            "date" => ["weekday", "year", "month", "day"],
            "time" => ["dayPeriod", "hour", "minute", "second", "fractionalSecondDigits"],
            "year-month" => ["year", "month"],
            "month-day" => ["month", "day"],
            _ => ["weekday", "year", "month", "day", "dayPeriod", "hour", "minute", "second", "fractionalSecondDigits"],
        };

        string[] defaultOptions = defaults switch
        {
            "date" => ["year", "month", "day"],
            "time" => ["hour", "minute", "second"],
            "year-month" => ["year", "month"],
            "month-day" => ["month", "day"],
            _ => ["year", "month", "day", "hour", "minute", "second"],
        };

        var formatOptions = inheritAll
            ? new Dictionary<string, string>(options, StringComparer.Ordinal)
            : new Dictionary<string, string>(StringComparer.Ordinal);

        if (!inheritAll && required is "date" or "year-month" or "any" && options.TryGetValue("era", out var era))
        {
            formatOptions["era"] = era;
        }

        var anyPresent = false;

        foreach (var name in new[] { "weekday", "year", "month", "day", "dayPeriod", "hour", "minute", "second", "fractionalSecondDigits" })
        {
            anyPresent |= options.ContainsKey(name);
        }

        var needDefaults = true;

        foreach (var name in requiredOptions)
        {
            if (options.TryGetValue(name, out var value))
            {
                formatOptions[name] = value;
                needDefaults = false;
            }
        }

        if (needDefaults)
        {
            if (anyPresent && !inheritAll)
            {
                return null;
            }

            foreach (var name in defaultOptions)
            {
                formatOptions[name] = "numeric";
            }

            if (defaults == "zoned-date-time" && !formatOptions.ContainsKey("timeZoneName"))
            {
                formatOptions["timeZoneName"] = "short";
            }
        }

        return best(formatOptions);
    }

    /// <summary>
    /// The proposal's AdjustDateTimeStyleFormat (<c>sec-adjustdatetimestyleformat</c>): the style's pattern where it writes only
    /// fields the type has, else the best pattern for those of its fields the type has.
    /// </summary>
    // Hairs-AI:           Origin=Ported; Spec=proposal-temporal e8cc03fc sec-temporal-handledatetimevalue sec-temporal-getdatetimeformat sec-temporal-adjustdatetimestyleformat via Broiler.VM JsRealm.DateTimeFormat; IP=Low; Security=High; Resources=2; Fingerprint=67FA27
    // Hairs-Falsified-If: a style's pattern for a Temporal type differs from AdjustDateTimeStyleFormat's
    // Hairs-Human:        PENDING
    private static string AdjustStylePattern(string stylePattern, string[] allowed, Func<Dictionary<string, string>, string> best)
    {
        var components = ComponentsOf(stylePattern);
        var conflicting = false;

        foreach (var name in components.Keys)
        {
            conflicting |= Array.IndexOf(allowed, name) < 0;
        }

        if (!conflicting)
        {
            return stylePattern;
        }

        var formatOptions = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var name in allowed)
        {
            if (components.TryGetValue(name, out var value))
            {
                formatOptions[name] = value;
            }
        }

        return best(formatOptions);
    }

    /// <summary>The hour cycle a CLDR hour letter writes.</summary>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-createdatetimeformat as amended by proposal-temporal e8cc03fc via Broiler.VM JsRealm.DateTimeFormat; IP=Low; Security=Low; Resources=1; Fingerprint=160F23
    // Hairs-Human:        PENDING
    internal static string HourCycleOfChar(char c) => c switch { 'h' => "h12", 'K' => "h11", 'k' => "h24", _ => "h23" };

    /// <summary>The CLDR hour letter of an hour cycle.</summary>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-createdatetimeformat as amended by proposal-temporal e8cc03fc via Broiler.VM JsRealm.DateTimeFormat; IP=Low; Security=Low; Resources=1; Fingerprint=00FA5A
    // Hairs-Human:        PENDING
    internal static char CharOfHourCycle(string hc) => hc switch { "h11" => 'K', "h12" => 'h', "h24" => 'k', _ => 'H' };

    /// <summary>The hour cycle of a pattern's first hour field, or nothing for a pattern without an hour.</summary>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-createdatetimeformat as amended by proposal-temporal e8cc03fc via Broiler.VM JsRealm.DateTimeFormat; IP=Low; Security=Low; Resources=1; Fingerprint=08B2E8
    // Hairs-Human:        PENDING
    internal static string? HourCycleOf(string pattern)
    {
        var quoted = false;

        foreach (var c in pattern)
        {
            if (c == '\'')
            {
                quoted = !quoted;
            }
            else if (!quoted && c is 'h' or 'H' or 'k' or 'K')
            {
                return HourCycleOfChar(c);
            }
        }

        return null;
    }

    /// <summary>A pattern with every hour field written in <paramref name="hourChar"/>, as V8 writes the resolved cycle into ICU's.</summary>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-createdatetimeformat as amended by proposal-temporal e8cc03fc via Broiler.VM JsRealm.DateTimeFormat; IP=Low; Security=Medium; Resources=1; Fingerprint=EC6EB7
    // Hairs-Human:        PENDING
    private static string ReplaceHourCycle(string pattern, char hourChar)
    {
        var result = new StringBuilder(pattern.Length + 1);
        var replace = true;
        var last = '\0';

        foreach (var c in pattern)
        {
            if (c == '\'')
            {
                replace = !replace;
                result.Append(c);
            }
            else if (c is 'h' or 'H' or 'k' or 'K')
            {
                if (replace && last == 'd')
                {
                    result.Append(' ');
                }

                result.Append(replace ? hourChar : c);
            }
            else
            {
                result.Append(c);
            }

            last = c;
        }

        return result.ToString();
    }

    /// <summary>The skeleton of the requested components, the hour in the resolved cycle's letter.</summary>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-createdatetimeformat as amended by proposal-temporal e8cc03fc via Broiler.VM JsRealm.DateTimeFormat; IP=Low; Security=Medium; Resources=1; Fingerprint=F7724F
    // Hairs-Human:        PENDING
    private static string SkeletonOfComponents(Dictionary<string, string> components, char hourChar)
    {
        var skeleton = new StringBuilder();

        string? Value(string name) => components.TryGetValue(name, out var value) ? value : null;

        skeleton.Append(Value("weekday") switch { "narrow" => "EEEEE", "short" => "EEE", "long" => "EEEE", _ => string.Empty });
        skeleton.Append(Value("era") switch { "narrow" => "GGGGG", "short" => "GGG", "long" => "GGGG", _ => string.Empty });
        skeleton.Append(Value("year") switch { "2-digit" => "yy", "numeric" => "y", _ => string.Empty });
        skeleton.Append(Value("month") switch
        {
            "narrow" => "MMMMM",
            "long" => "MMMM",
            "short" => "MMM",
            "2-digit" => "MM",
            "numeric" => "M",
            _ => string.Empty,
        });
        skeleton.Append(Value("day") switch { "2-digit" => "dd", "numeric" => "d", _ => string.Empty });
        skeleton.Append(Value("dayPeriod") switch { "narrow" => "BBBBB", "long" => "BBBB", "short" => "B", _ => string.Empty });
        skeleton.Append(Value("hour") switch { "2-digit" => new string(hourChar, 2), "numeric" => hourChar.ToString(), _ => string.Empty });
        skeleton.Append(Value("minute") switch { "2-digit" => "mm", "numeric" => "m", _ => string.Empty });
        skeleton.Append(Value("second") switch { "2-digit" => "ss", "numeric" => "s", _ => string.Empty });
        skeleton.Append(Value("fractionalSecondDigits") ?? string.Empty);
        skeleton.Append(Value("timeZoneName") switch
        {
            "short" => "z",
            "long" => "zzzz",
            "shortOffset" => "O",
            "longOffset" => "OOOO",
            "shortGeneric" => "v",
            "longGeneric" => "vvvv",
            _ => string.Empty,
        });

        return skeleton.ToString();
    }

    /// <summary>
    /// The pattern of a date style, a time style or both, joined by CLDR's date-time pattern for the date style; a time whose
    /// hour cycle is not the resolved one is regenerated from its skeleton.
    /// </summary>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-createdatetimeformat as amended by proposal-temporal e8cc03fc via Broiler.VM JsRealm.DateTimeFormat; IP=Low; Security=Medium; Resources=1; Fingerprint=4ECBB8
    // Hairs-Human:        PENDING
    private static string StylePattern(DateData dates, string language, string? dateStyle, string? timeStyle, string hc, char hcChar, DateTimePatternGenerator generator)
    {
        var date = dateStyle is null ? null : dates.Value(language, "dateFormats." + dateStyle);
        var time = timeStyle is null ? null : dates.Value(language, "timeFormats." + timeStyle);
        var pattern = date is not null && time is not null
            ? DateTimePatternGenerator.Substitute(dates.Value(language, "atTime." + dateStyle) ?? "{1} {0}", time, date, string.Empty)
            : date ?? time ?? string.Empty;

        if (time is null || HourCycleOf(pattern) == hc)
        {
            return pattern;
        }

        var skeleton = new StringBuilder();

        foreach (var c in DateTimePatternGenerator.SkeletonOf(pattern))
        {
            if (c is 'h' or 'H' or 'k' or 'K')
            {
                skeleton.Append(hcChar);
            }
            else if (c is not ('a' or 'b' or 'B'))
            {
                skeleton.Append(c);
            }
        }

        return ReplaceHourCycle(generator.BestPattern(skeleton.ToString(), DateTimePatternGenerator.MatchHourLength), hcChar);
    }

    /// <summary>The components a pattern writes, as resolvedOptions reports them.</summary>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-createdatetimeformat as amended by proposal-temporal e8cc03fc via Broiler.VM JsRealm.DateTimeFormat; IP=Low; Security=Medium; Resources=1; Fingerprint=DD7F43
    // Hairs-Human:        PENDING
    internal static Dictionary<string, string> ComponentsOf(string pattern)
    {
        var components = new Dictionary<string, string>(StringComparer.Ordinal);
        var tokens = DateTimePatternGenerator.Tokens(pattern);

        for (var index = 0; index < tokens.Count; index++)
        {
            var token = tokens[index];

            if (token[0] == '\'')
            {
                index = DateTimePatternGenerator.QuoteEnd(tokens, index, null);
                continue;
            }

            var count = token.Length;
            string Text() => count <= 3 ? "short" : count == 4 ? "long" : "narrow";
            string Digits() => count == 2 ? "2-digit" : "numeric";

            switch (token[0])
            {
                case 'G':
                    components["era"] = Text();
                    break;
                case 'y':
                    components["year"] = Digits();
                    break;
                case 'M' or 'L':
                    components["month"] = count <= 2 ? Digits() : Text();
                    break;
                case 'E' or 'c':
                    components["weekday"] = count == 6 ? "short" : Text();
                    break;
                case 'd':
                    components["day"] = Digits();
                    break;
                case 'B' or 'b':
                    components["dayPeriod"] = Text();
                    break;
                case 'h' or 'H' or 'k' or 'K':
                    components["hour"] = Digits();
                    break;
                case 'm':
                    components["minute"] = Digits();
                    break;
                case 's':
                    components["second"] = Digits();
                    break;
                case 'S':
                    components["fractionalSecondDigits"] = token;
                    break;
                case 'z':
                    components["timeZoneName"] = count < 4 ? "short" : "long";
                    break;
                case 'O':
                    components["timeZoneName"] = count < 4 ? "shortOffset" : "longOffset";
                    break;
                case 'v':
                    components["timeZoneName"] = count < 4 ? "shortGeneric" : "longGeneric";
                    break;
            }
        }

        return components;
    }

    /// <summary>The components of a pattern in Table 16's order, <c>fractionalSecondDigits</c> as its digit count.</summary>
    // Hairs-AI:           Origin=Ported; Spec=ECMA-402 sec-createdatetimeformat as amended by proposal-temporal e8cc03fc via Broiler.VM JsRealm.DateTimeFormat; IP=Low; Security=Low; Resources=1; Fingerprint=65DD12
    // Hairs-Human:        PENDING
    private static IReadOnlyList<KeyValuePair<string, string>> Resolved(string pattern)
    {
        var components = ComponentsOf(pattern);
        var resolved = new List<KeyValuePair<string, string>>();

        foreach (var name in DateTimeComponents.Names)
        {
            if (components.TryGetValue(name, out var value))
            {
                resolved.Add(new(name, name == "fractionalSecondDigits" ? value.Length.ToString(System.Globalization.NumberFormatInfo.InvariantInfo) : value));
            }
        }

        return resolved.AsReadOnly();
    }
}
