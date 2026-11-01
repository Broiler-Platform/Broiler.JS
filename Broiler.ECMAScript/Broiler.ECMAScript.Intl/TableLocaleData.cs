// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   61
// Annotated:        61/61
// Exempt:           13
// Human-reviewed:   0/61
// IP risk:          Low
// Security risk:    High
// Criteria:         2/2
// Resource impact:  3/10 max
// Unverified:       61
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;

namespace Broiler.ECMAScript.Intl;

/// <summary>
/// The answers to <see cref="ILocaleData"/>'s questions from an <see cref="IIntlDataSet"/>'s tables: the library's decoder
/// implementation, the shared data <see cref="CompositeLocaleData"/> asks first (Broiler.VM's JSD-0082, decision D8).
/// </summary>
/// <remarks>
/// <para>
/// <b>Ported from Broiler.VM's <c>JsIntlTables</c></b> at its SJL-9 rule commit <c>4cf9ca44</c>: the readers of the likely
/// subtags, aliases, extensions and locales; <c>ReadNumbers</c>, <c>ReadDates</c> with its <c>Layered</c> calendars, and
/// <c>ReadLocaleInfo</c>, into the dictionaries of its <c>JsNumberData</c>, <c>JsDateData</c> and <c>JsLocaleInfo</c>, built
/// by the same steps in the same order, so that a value - and the order in which a locale's date data enumerates - is the
/// one Broiler.VM decodes. Three things change (decision D19 and the data contract): numbers are parsed through
/// <see cref="NumberFormatInfo.InvariantInfo"/>; a plural rule is kept as its text, which sub-area (b)'s plural engine
/// parses where Broiler.VM parsed it here; and a line short of its table's fields is refused as
/// <see cref="InvalidDataException"/> where Broiler.VM's index would have thrown. The time zone, calendar, metazone and
/// collation tables are not read here: the first two are <c>Broiler.ECMAScript.Algorithms</c>' to decode, the metazones
/// sub-area (d)'s and the collation tables milestone SJL-17's.
/// </para>
/// <para>
/// <b>Each group of tables is decoded the first time a question needs it</b>, once, under a thread-safe lazy, as
/// Broiler.VM's are (decision D23's L3 reading): the likely subtags, the aliases, the extensions and the locales each alone;
/// the number, currency, unit and plural tables together; the date, hour-cycle and day-period tables together; and the
/// week, script, list, relative-time and display-name tables together. Nothing decoded changes afterwards, so every answer
/// is safe to read from several threads.
/// </para>
/// <para>
/// <b>Precondition:</b> the data set's tables are the data home's generator's output in the format <see cref="IntlTable"/>
/// documents. A table that is not may throw <see cref="InvalidDataException"/> or <see cref="FormatException"/> from the
/// question that first decodes it, and from every later one.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 decoder implementation; IP=Low; Security=Medium; Resources=1; Fingerprint=0F61D9
// Broiler-Human:        PENDING
public sealed class TableLocaleData : ILocaleData
{
    /// <summary>The kinds of alias the table names, in Broiler.VM's order.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D8 decoders via Broiler.VM JsIntlTables; IP=Low; Security=Low; Resources=0; Fingerprint=E5A822
    // Broiler-Human:        PENDING
    private static ImmutableArray<string> AliasKinds { get; } = ["language", "script", "region", "variant", "subdivision"];

    private readonly IIntlDataSet data;

    private readonly Lazy<Dictionary<string, string>> likely;

    private readonly Lazy<Dictionary<string, Dictionary<string, string>>> aliases;

    private readonly Lazy<Dictionary<string, string>> extensions;

    private readonly Lazy<ReadOnlyCollection<string>> locales;

    private readonly Lazy<NumberTables> numbers;

    private readonly Lazy<DateTables> dates;

    private readonly Lazy<LocaleInfoTables> localeInfo;

    /// <summary>Creates the decoder over a data set; nothing is decoded until a question needs it.</summary>
    /// <param name="data">The tables, which answer the same bytes for the life of the process.</param>
    /// <exception cref="ArgumentNullException"><paramref name="data"/> is null.</exception>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 decoder implementation; IP=Low; Security=Medium; Resources=1; Fingerprint=41EDD5
    // Broiler-Human:        PENDING
    public TableLocaleData(IIntlDataSet data)
    {
        ArgumentNullException.ThrowIfNull(data);
        this.data = data;
        likely = new(() => IntlTableText.Pairs(data.Table(IntlTable.LikelySubtags), IntlTable.LikelySubtags));
        aliases = new(ReadAliases);
        extensions = new(ReadExtensions);
        locales = new(() => IntlTableText.Lines(data.Table(IntlTable.Locales)).AsReadOnly());
        numbers = new(ReadNumbers);
        dates = new(ReadDates);
        localeInfo = new(ReadLocaleInfo);
    }

    /// <summary>The CLDR release the tables were generated from, as the data set states it (convention 13).</summary>
    public string CldrVersion => data.CldrVersion;

    /// <inheritdoc/>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 decoder implementation; IP=Low; Security=Low; Resources=0; Fingerprint=1A0433
    // Broiler-Human:        PENDING
    public IReadOnlyList<string> Locales => locales.Value;

    /// <inheritdoc/>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 decoder implementation; IP=Low; Security=Low; Resources=1; Fingerprint=B7872A
    // Broiler-Human:        PENDING
    public bool HoldsLocale(IntlTable table, string locale)
    {
        ArgumentNullException.ThrowIfNull(locale);

        return table switch
        {
            IntlTable.NumberLocales => numbers.Value.Locales.ContainsKey(locale),
            IntlTable.Currencies => numbers.Value.CurrencyLocales.Contains(locale),
            IntlTable.Units => numbers.Value.UnitLocales.Contains(locale),
            IntlTable.Plurals => numbers.Value.Plurals.ContainsKey(locale),
            IntlTable.Ordinals => numbers.Value.Ordinals.ContainsKey(locale),
            IntlTable.PluralRanges => numbers.Value.PluralRangeLocales.Contains(locale),
            IntlTable.DateLocales => locale.Length != 0 && !locale.Contains('@', StringComparison.Ordinal) && dates.Value.Locales.ContainsKey(locale),
            IntlTable.DayPeriods => dates.Value.DayPeriods.ContainsKey(locale),
            IntlTable.ListPatterns => localeInfo.Value.ListPatternLocales.Contains(locale),
            IntlTable.RelativeTimes => localeInfo.Value.RelativeTimeLocales.Contains(locale),
            IntlTable.DisplayNames => localeInfo.Value.DisplayNameLocales.Contains(locale),
            _ => false,
        };
    }

    /// <inheritdoc/>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 decoder implementation; IP=Low; Security=Low; Resources=1; Fingerprint=48FC15
    // Broiler-Human:        PENDING
    public string? LikelySubtags(string source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return likely.Value.TryGetValue(source, out var target) ? target : null;
    }

    /// <inheritdoc/>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 decoder implementation; IP=Low; Security=Low; Resources=1; Fingerprint=ABF0A9
    // Broiler-Human:        PENDING
    public string? Alias(LocaleAliasKind kind, string from)
    {
        ArgumentNullException.ThrowIfNull(from);

        if (kind is < LocaleAliasKind.Language or > LocaleAliasKind.Subdivision)
        {
            return null;
        }

        return aliases.Value.TryGetValue(AliasKinds[(int)kind], out var table) && table.TryGetValue(from, out var replacement) ? replacement : null;
    }

    /// <inheritdoc/>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 decoder implementation; IP=Low; Security=Low; Resources=1; Fingerprint=FB55EE
    // Broiler-Human:        PENDING
    public bool IsExtensionKey(char singleton, string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return extensions.Value.ContainsKey(singleton + "|" + key + "|");
    }

    /// <inheritdoc/>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 decoder implementation; IP=Low; Security=Low; Resources=1; Fingerprint=11F151
    // Broiler-Human:        PENDING
    public bool TryExtensionType(char singleton, string key, string type, out string preferred)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(type);

        if (extensions.Value.TryGetValue(singleton + "|" + key + "|" + type, out var stated))
        {
            preferred = stated.Length == 0 ? type : stated;
            return true;
        }

        preferred = type;
        return false;
    }

    /// <inheritdoc/>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 decoder implementation; IP=Low; Security=Low; Resources=1; Fingerprint=4FA7D6
    // Broiler-Human:        PENDING
    public RegionHourCycles? HourCycles(string region)
    {
        ArgumentNullException.ThrowIfNull(region);
        return dates.Value.TimeData.TryGetValue(region, out var cycles) ? cycles : null;
    }

    /// <inheritdoc/>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 decoder implementation; IP=Low; Security=Low; Resources=1; Fingerprint=EA6847
    // Broiler-Human:        PENDING
    public RegionWeek? Week(string region)
    {
        ArgumentNullException.ThrowIfNull(region);
        return localeInfo.Value.Weeks.TryGetValue(region, out var week) ? week : null;
    }

    /// <inheritdoc/>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 decoder implementation; IP=Low; Security=Low; Resources=1; Fingerprint=6264A1
    // Broiler-Human:        PENDING
    public bool? IsRightToLeft(string script)
    {
        ArgumentNullException.ThrowIfNull(script);
        return localeInfo.Value.RightToLeft.TryGetValue(script, out var rightToLeft) ? rightToLeft : null;
    }

    /// <inheritdoc/>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 decoder implementation; IP=Low; Security=Low; Resources=1; Fingerprint=8CEDC9
    // Broiler-Human:        PENDING
    public string? NumberValue(string locale, string key)
    {
        ArgumentNullException.ThrowIfNull(locale);
        ArgumentNullException.ThrowIfNull(key);
        return numbers.Value.Locales.TryGetValue(locale, out var values) && values.TryGetValue(key, out var value) ? value : null;
    }

    /// <inheritdoc/>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 decoder implementation; IP=Low; Security=Low; Resources=1; Fingerprint=7AD19C
    // Broiler-Human:        PENDING
    public CurrencyNames? Currency(string locale, string code)
    {
        ArgumentNullException.ThrowIfNull(locale);
        ArgumentNullException.ThrowIfNull(code);
        return numbers.Value.Currencies.TryGetValue(locale + "|" + code, out var names) ? names : null;
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> CurrencyCodes => numbers.Value.CurrencyCodes;

    /// <inheritdoc/>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 decoder implementation; IP=Low; Security=Low; Resources=1; Fingerprint=2FC603
    // Broiler-Human:        PENDING
    public int? CurrencyDigits(string code)
    {
        ArgumentNullException.ThrowIfNull(code);
        return numbers.Value.CurrencyDigits.TryGetValue(code, out var digits) ? digits : null;
    }

    /// <inheritdoc/>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 decoder implementation; IP=Low; Security=Low; Resources=0; Fingerprint=9DAB07
    // Broiler-Human:        PENDING
    public IReadOnlyList<string> NumberingSystems => numbers.Value.NumberingSystemNames;

    /// <inheritdoc/>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 decoder implementation; IP=Low; Security=Low; Resources=1; Fingerprint=4ECA20
    // Broiler-Human:        PENDING
    public IReadOnlyList<string>? NumberingSystemDigits(string system)
    {
        ArgumentNullException.ThrowIfNull(system);
        return numbers.Value.NumberingSystems.TryGetValue(system, out var digits) ? digits : null;
    }

    /// <inheritdoc/>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 decoder implementation; IP=Low; Security=Low; Resources=1; Fingerprint=30F7BA
    // Broiler-Human:        PENDING
    public IReadOnlyList<PluralRuleText>? PluralRules(string language, PluralRuleType type)
    {
        ArgumentNullException.ThrowIfNull(language);
        var rules = type == PluralRuleType.Ordinal ? numbers.Value.Ordinals : numbers.Value.Plurals;
        return type is PluralRuleType.Cardinal or PluralRuleType.Ordinal && rules.TryGetValue(language, out var list) ? list.AsReadOnly() : null;
    }

    /// <inheritdoc/>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 decoder implementation; IP=Low; Security=Low; Resources=1; Fingerprint=B0280D
    // Broiler-Human:        PENDING
    public string? PluralRange(string language, string start, string end)
    {
        ArgumentNullException.ThrowIfNull(language);
        ArgumentNullException.ThrowIfNull(start);
        ArgumentNullException.ThrowIfNull(end);
        return numbers.Value.PluralRanges.TryGetValue(language + "|" + start + "|" + end, out var result) ? result : null;
    }

    /// <inheritdoc/>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 decoder implementation; IP=Low; Security=Low; Resources=1; Fingerprint=7615A0
    // Broiler-Human:        PENDING
    public string? UnitPattern(string locale, string width, string unit, string field)
    {
        ArgumentNullException.ThrowIfNull(locale);
        ArgumentNullException.ThrowIfNull(width);
        ArgumentNullException.ThrowIfNull(unit);
        ArgumentNullException.ThrowIfNull(field);
        return numbers.Value.Units.TryGetValue(locale + "|" + width + "|" + unit + "|" + field, out var value) ? value : null;
    }

    /// <inheritdoc/>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 decoder implementation; IP=Low; Security=Low; Resources=1; Fingerprint=EB623B
    // Broiler-Human:        PENDING
    public ListPatterns? ListPattern(string locale, string type)
    {
        ArgumentNullException.ThrowIfNull(locale);
        ArgumentNullException.ThrowIfNull(type);
        return localeInfo.Value.ListPatterns.TryGetValue(locale + "|" + type, out var patterns) ? patterns : null;
    }

    /// <inheritdoc/>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 decoder implementation; IP=Low; Security=Low; Resources=1; Fingerprint=E4D952
    // Broiler-Human:        PENDING
    public string? RelativeTime(string locale, string field, string key)
    {
        ArgumentNullException.ThrowIfNull(locale);
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(key);
        return localeInfo.Value.RelativeTimes.TryGetValue(locale + "|" + field + "|" + key, out var value) ? value : null;
    }

    /// <inheritdoc/>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 decoder implementation; IP=Low; Security=Low; Resources=1; Fingerprint=9B5118
    // Broiler-Human:        PENDING
    public string? DisplayName(string locale, string kind, string code)
    {
        ArgumentNullException.ThrowIfNull(locale);
        ArgumentNullException.ThrowIfNull(kind);
        ArgumentNullException.ThrowIfNull(code);
        return localeInfo.Value.DisplayNames.TryGetValue(locale + "|" + kind + "|" + code, out var name) ? name : null;
    }

    /// <inheritdoc/>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 decoder implementation; IP=Low; Security=Low; Resources=1; Fingerprint=E1452C
    // Broiler-Human:        PENDING
    public string? DateValue(string locale, string calendar, string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return DateLanguage(locale, calendar) is { } values && values.TryGetValue(key, out var value) ? value : null;
    }

    /// <inheritdoc/>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 decoder implementation; IP=Low; Security=Low; Resources=1; Fingerprint=EBC824
    // Broiler-Human:        PENDING
    public IReadOnlyDictionary<string, string>? DateValues(string locale, string calendar) =>
        DateLanguage(locale, calendar) is { } values ? values.AsReadOnly() : null;

    /// <inheritdoc/>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 decoder implementation; IP=Low; Security=Low; Resources=1; Fingerprint=4A5D9E
    // Broiler-Human:        PENDING
    public IReadOnlyList<DayPeriodRule>? DayPeriods(string language)
    {
        ArgumentNullException.ThrowIfNull(language);
        return dates.Value.DayPeriods.TryGetValue(language, out var rules) ? rules.AsReadOnly() : null;
    }

    /// <summary>
    /// A locale's date data in a calendar: the locale's own for <c>gregory</c>, and its layer <c>locale@calendar</c> for every
    /// other calendar, as Broiler.VM's date data keys them.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 decoder implementation; IP=Low; Security=Low; Resources=1; Fingerprint=624A35
    // Broiler-Human:        PENDING
    private Dictionary<string, string>? DateLanguage(string locale, string calendar)
    {
        ArgumentNullException.ThrowIfNull(locale);
        ArgumentNullException.ThrowIfNull(calendar);

        if (locale.Length == 0 || locale.Contains('@', StringComparison.Ordinal) || calendar.Length == 0)
        {
            return null;
        }

        var key = calendar == "gregory" ? locale : locale + "@" + calendar;
        return dates.Value.Locales.TryGetValue(key, out var values) ? values : null;
    }

    // ---- the decoders, Broiler.VM's JsIntlTables at 4cf9ca44 ------------------------------------

    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D8 decoders via Broiler.VM JsIntlTables; IP=Low; Security=Low; Resources=2; Fingerprint=CB2899
    // Broiler-Human:        PENDING
    private Dictionary<string, Dictionary<string, string>> ReadAliases()
    {
        var kinds = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);

        foreach (var kind in AliasKinds)
        {
            kinds[kind] = new(StringComparer.Ordinal);
        }

        foreach (var line in IntlTableText.Lines(data.Table(IntlTable.Aliases)))
        {
            var fields = IntlTableText.Fields(line, 3, IntlTable.Aliases);

            if (!kinds.TryGetValue(fields[0], out var kind))
            {
                throw new InvalidDataException("the Aliases table names no alias kind " + fields[0]);
            }

            kind[fields[1]] = fields[2];
        }

        return kinds;
    }

    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D8 decoders via Broiler.VM JsIntlTables; IP=Low; Security=Low; Resources=2; Fingerprint=BE6FFE
    // Broiler-Human:        PENDING
    private Dictionary<string, string> ReadExtensions()
    {
        var table = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var line in IntlTableText.Lines(data.Table(IntlTable.Extensions)))
        {
            var fields = IntlTableText.Fields(line, 4, IntlTable.Extensions);
            table[fields[0] + "|" + fields[1] + "|" + fields[2]] = fields[3];
        }

        return table;
    }

    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D8 decoders via Broiler.VM JsIntlTables; IP=Low; Security=High; Resources=3; Fingerprint=21E4C4
    // Broiler-Falsified-If: a number, currency, unit, plural, plural-range or numbering-system answer differs from what Broiler.VM's JsIntlTables.ReadNumbers decodes from the same tables
    // Broiler-Human:        PENDING
    private NumberTables ReadNumbers()
    {
        var tables = new NumberTables();

        foreach (var line in IntlTableText.Lines(data.Table(IntlTable.NumberLocales)))
        {
            var fields = IntlTableText.Fields(line, 3, IntlTable.NumberLocales);

            if (!tables.Locales.TryGetValue(fields[0], out var locale))
            {
                locale = new(StringComparer.Ordinal);
                tables.Locales[fields[0]] = locale;
            }

            locale[fields[1]] = IntlTableText.Unescape(fields[2]);
        }

        var codes = new List<string>();
        var named = new HashSet<string>(StringComparer.Ordinal);

        foreach (var line in IntlTableText.Lines(data.Table(IntlTable.Currencies)))
        {
            var fields = IntlTableText.Fields(line, 9, IntlTable.Currencies);
            tables.Currencies[fields[0] + "|" + fields[1]] = new CurrencyNames(
                IntlTableText.Unescape(fields[2]), IntlTableText.Unescape(fields[3]), IntlTableText.Unescape(fields[4]),
                IntlTableText.Unescape(fields[5]), IntlTableText.Unescape(fields[6]), fields[7], fields[8]);
            tables.CurrencyLocales.Add(fields[0]);

            if (named.Add(fields[1]))
            {
                codes.Add(fields[1]);
            }
        }

        tables.CurrencyCodes = codes.AsReadOnly();

        foreach (var line in IntlTableText.Lines(data.Table(IntlTable.CurrencyDigits)))
        {
            var bar = IntlTableText.Bar(line, IntlTable.CurrencyDigits);
            tables.CurrencyDigits[line[..bar]] = int.Parse(line.AsSpan(bar + 1), NumberStyles.Integer, NumberFormatInfo.InvariantInfo);
        }

        var systems = new List<string>();

        foreach (var line in IntlTableText.Lines(data.Table(IntlTable.NumberingSystems)))
        {
            var bar = IntlTableText.Bar(line, IntlTable.NumberingSystems);
            var digits = IntlTableText.Unescape(line[(bar + 1)..]);
            var list = new string[10];
            var index = 0;

            for (var at = 0; at < digits.Length && index < 10; index++)
            {
                var width = char.IsHighSurrogate(digits[at]) && at + 1 < digits.Length ? 2 : 1;
                list[index] = digits.Substring(at, width);
                at += width;
            }

            if (index < 10)
            {
                throw new InvalidDataException("the NumberingSystems table gives " + line[..bar] + " fewer than ten digits");
            }

            if (!tables.NumberingSystems.ContainsKey(line[..bar]))
            {
                systems.Add(line[..bar]);
            }

            tables.NumberingSystems[line[..bar]] = Array.AsReadOnly(list);
        }

        tables.NumberingSystemNames = systems.AsReadOnly();

        foreach (var (table, plurals) in new[] { (IntlTable.Plurals, tables.Plurals), (IntlTable.Ordinals, tables.Ordinals) })
        {
            foreach (var line in IntlTableText.Lines(data.Table(table)))
            {
                var fields = IntlTableText.Fields(line, 3, table);

                if (!plurals.TryGetValue(fields[0], out var rules))
                {
                    rules = [];
                    plurals[fields[0]] = rules;
                }

                rules.Add(new PluralRuleText(fields[1], IntlTableText.Unescape(fields[2])));
            }
        }

        foreach (var line in IntlTableText.Lines(data.Table(IntlTable.PluralRanges)))
        {
            var fields = IntlTableText.Fields(line, 4, IntlTable.PluralRanges);
            tables.PluralRanges[fields[0] + "|" + fields[1] + "|" + fields[2]] = fields[3];
            tables.PluralRangeLocales.Add(fields[0]);
        }

        foreach (var line in IntlTableText.Lines(data.Table(IntlTable.Units)))
        {
            var fields = IntlTableText.Fields(line, 5, IntlTable.Units);
            tables.Units[fields[0] + "|" + fields[1] + "|" + fields[2] + "|" + fields[3]] = IntlTableText.Unescape(fields[4]);
            tables.UnitLocales.Add(fields[0]);
        }

        return tables;
    }

    /// <summary>A layer of date data over the data below it: its values set, and its <c>-</c> keys removed.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D8 decoders via Broiler.VM JsIntlTables; IP=Low; Security=Low; Resources=1; Fingerprint=9D65CD
    // Broiler-Human:        PENDING
    private static Dictionary<string, string> Layered(Dictionary<string, string> below, Dictionary<string, string> layer)
    {
        var result = new Dictionary<string, string>(below, StringComparer.Ordinal);

        foreach (var (key, value) in layer)
        {
            if (key.StartsWith('-'))
            {
                result.Remove(key[1..]);
            }
            else
            {
                result[key] = value;
            }
        }

        return result;
    }

    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D8 decoders via Broiler.VM JsIntlTables; IP=Low; Security=High; Resources=3; Fingerprint=E8AF35
    // Broiler-Falsified-If: a language's date data in a calendar holds a key or a value, or enumerates in an order, other than Broiler.VM's JsIntlTables.ReadDates decodes from the same table
    // Broiler-Human:        PENDING
    private DateTables ReadDates()
    {
        var tables = new DateTables();

        foreach (var line in IntlTableText.Lines(data.Table(IntlTable.DateLocales)))
        {
            var fields = IntlTableText.Fields(line, 3, IntlTable.DateLocales);

            if (!tables.Locales.TryGetValue(fields[0], out var locale))
            {
                locale = new(StringComparer.Ordinal);
                tables.Locales[fields[0]] = locale;
            }

            locale[fields[1]] = IntlTableText.Unescape(fields[2]);
        }

        // EACH LANGUAGE'S ISO 8601 CALENDAR (Broiler.VM's JSD-0055) is its Gregorian names under root's ISO 8601 patterns, as
        // CLDR's aliases make it: `<language>@iso8601`. The date-time glue stays the language's Gregorian one, as ICU resolves
        // it, where root's would be "{1} {0}".
        if (tables.Locales.TryGetValue(DateTables.Iso8601, out var iso8601))
        {
            foreach (var language in new List<string>(tables.Locales.Keys))
            {
                if (language.Contains('@', StringComparison.Ordinal))
                {
                    continue;
                }

                var calendar = new Dictionary<string, string>(StringComparer.Ordinal);

                foreach (var (key, value) in tables.Locales[language])
                {
                    if (!DateTables.IsPatternKey(key) || key.StartsWith("dateTime.", StringComparison.Ordinal) || key.StartsWith("atTime.", StringComparison.Ordinal))
                    {
                        calendar[key] = value;
                    }
                }

                foreach (var (key, value) in iso8601)
                {
                    if (!key.StartsWith("dateTime.", StringComparison.Ordinal) && !key.StartsWith("atTime.", StringComparison.Ordinal))
                    {
                        calendar[key] = value;
                    }
                }

                tables.Locales[language + DateTables.Iso8601] = calendar;
            }
        }

        // EACH LANGUAGE'S OTHER CALENDARS (Broiler.VM's JSD-0057) are layers: CLDR's generic patterns over the language's
        // Gregorian data, and each calendar over the generic layer or its parent's. A layer's key beginning with `-` removes
        // the key below it.
        foreach (var language in new List<string>(tables.Locales.Keys))
        {
            if (language.Contains('@', StringComparison.Ordinal) || !tables.Locales.TryGetValue(language + "@generic", out var genericDiff))
            {
                continue;
            }

            var generic = Layered(tables.Locales[language], genericDiff);
            tables.Locales.Remove(language + "@generic");

            // A LAYER THE TABLE HOLDS NO LINE OF is its parent's data unchanged.
            foreach (var (calendar, parent) in DateTables.CalendarLayers)
            {
                var diff = tables.Locales.TryGetValue(language + "@" + calendar, out var lines) ? lines : [];
                tables.Locales[language + "@" + calendar] = Layered(parent is null ? generic : tables.Locales[language + "@" + parent], diff);
            }
        }

        foreach (var line in IntlTableText.Lines(data.Table(IntlTable.TimeData)))
        {
            var fields = IntlTableText.Fields(line, 3, IntlTable.TimeData);
            tables.TimeData[fields[0]] = new RegionHourCycles(fields[1], fields[2]);
        }

        foreach (var line in IntlTableText.Lines(data.Table(IntlTable.DayPeriods)))
        {
            var fields = IntlTableText.Fields(line, 4, IntlTable.DayPeriods);

            if (!tables.DayPeriods.TryGetValue(fields[0], out var rules))
            {
                rules = [];
                tables.DayPeriods[fields[0]] = rules;
            }

            rules.Add(fields[3].Length == 0
                ? new DayPeriodRule(fields[1], Minutes(fields[2]), -1, At: true)
                : new DayPeriodRule(fields[1], Minutes(fields[2]), Minutes(fields[3]), At: false));
        }

        return tables;
    }

    /// <summary>A day period table's time, <c>hh:mm</c>, as the minute of the day; <c>-1</c> for an empty field.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D8 decoders via Broiler.VM JsIntlTables; IP=Low; Security=Low; Resources=1; Fingerprint=39A256
    // Broiler-Human:        PENDING
    private static int Minutes(string time) =>
        time.Length == 0
            ? -1
            : (int.Parse(time.AsSpan(0, 2), NumberStyles.Integer, NumberFormatInfo.InvariantInfo) * 60)
                + int.Parse(time.AsSpan(3), NumberStyles.Integer, NumberFormatInfo.InvariantInfo);

    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D8 decoders via Broiler.VM JsIntlTables; IP=Low; Security=Low; Resources=2; Fingerprint=B27D7D
    // Broiler-Human:        PENDING
    private LocaleInfoTables ReadLocaleInfo()
    {
        var info = new LocaleInfoTables();

        foreach (var line in IntlTableText.Lines(data.Table(IntlTable.WeekData)))
        {
            var fields = IntlTableText.Fields(line, 5, IntlTable.WeekData);
            info.Weeks[fields[0]] = new RegionWeek(fields[1], fields[2], fields[3], fields[4]);
        }

        foreach (var line in IntlTableText.Lines(data.Table(IntlTable.Scripts)))
        {
            var bar = IntlTableText.Bar(line, IntlTable.Scripts);
            info.RightToLeft[line[..bar]] = line[(bar + 1)..] == "YES";
        }

        foreach (var line in IntlTableText.Lines(data.Table(IntlTable.ListPatterns)))
        {
            var fields = IntlTableText.Fields(line, 6, IntlTable.ListPatterns);
            info.ListPatterns[fields[0] + "|" + fields[1]] = new ListPatterns(
                IntlTableText.Unescape(fields[2]), IntlTableText.Unescape(fields[3]), IntlTableText.Unescape(fields[4]), IntlTableText.Unescape(fields[5]));
            info.ListPatternLocales.Add(fields[0]);
        }

        foreach (var line in IntlTableText.Lines(data.Table(IntlTable.RelativeTimes)))
        {
            var fields = IntlTableText.Fields(line, 4, IntlTable.RelativeTimes);
            info.RelativeTimes[fields[0] + "|" + fields[1] + "|" + fields[2]] = IntlTableText.Unescape(fields[3]);
            info.RelativeTimeLocales.Add(fields[0]);
        }

        foreach (var line in IntlTableText.Lines(data.Table(IntlTable.DisplayNames)))
        {
            var fields = IntlTableText.Fields(line, 4, IntlTable.DisplayNames);
            info.DisplayNames[fields[0] + "|" + fields[1] + "|" + fields[2]] = IntlTableText.Unescape(fields[3]);
            info.DisplayNameLocales.Add(fields[0]);
        }

        return info;
    }

    /// <summary>The number, currency, unit and plural tables, decoded: Broiler.VM's <c>JsNumberData</c>'s dictionaries.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D8 decoders via Broiler.VM JsIntlTables; IP=Low; Security=Low; Resources=0; Fingerprint=69CBCC
    // Broiler-Human:        PENDING
    private sealed class NumberTables
    {
        // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D8 decoders via Broiler.VM JsIntlTables; IP=Low; Security=Low; Resources=0; Fingerprint=163FE2
        // Broiler-Human:        PENDING
        internal Dictionary<string, Dictionary<string, string>> Locales { get; } = new(StringComparer.Ordinal);

        // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D8 decoders via Broiler.VM JsIntlTables; IP=Low; Security=Low; Resources=0; Fingerprint=CED128
        // Broiler-Human:        PENDING
        internal Dictionary<string, CurrencyNames> Currencies { get; } = new(StringComparer.Ordinal);

        // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D8 decoders via Broiler.VM JsIntlTables; IP=Low; Security=Low; Resources=0; Fingerprint=23C33A
        // Broiler-Human:        PENDING
        internal HashSet<string> CurrencyLocales { get; } = new(StringComparer.Ordinal);

        internal IReadOnlyList<string> CurrencyCodes { get; set; } = [];

        // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D8 decoders via Broiler.VM JsIntlTables; IP=Low; Security=Low; Resources=0; Fingerprint=EEA0DA
        // Broiler-Human:        PENDING
        internal Dictionary<string, int> CurrencyDigits { get; } = new(StringComparer.Ordinal);

        // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D8 decoders via Broiler.VM JsIntlTables; IP=Low; Security=Low; Resources=0; Fingerprint=51AA6F
        // Broiler-Human:        PENDING
        internal Dictionary<string, IReadOnlyList<string>> NumberingSystems { get; } = new(StringComparer.Ordinal);

        internal IReadOnlyList<string> NumberingSystemNames { get; set; } = [];

        // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D8 decoders via Broiler.VM JsIntlTables; IP=Low; Security=Low; Resources=0; Fingerprint=762B62
        // Broiler-Human:        PENDING
        internal Dictionary<string, List<PluralRuleText>> Plurals { get; } = new(StringComparer.Ordinal);

        // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D8 decoders via Broiler.VM JsIntlTables; IP=Low; Security=Low; Resources=0; Fingerprint=D20849
        // Broiler-Human:        PENDING
        internal Dictionary<string, List<PluralRuleText>> Ordinals { get; } = new(StringComparer.Ordinal);

        // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D8 decoders via Broiler.VM JsIntlTables; IP=Low; Security=Low; Resources=0; Fingerprint=A8EF7A
        // Broiler-Human:        PENDING
        internal Dictionary<string, string> PluralRanges { get; } = new(StringComparer.Ordinal);

        // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D8 decoders via Broiler.VM JsIntlTables; IP=Low; Security=Low; Resources=0; Fingerprint=D59F88
        // Broiler-Human:        PENDING
        internal HashSet<string> PluralRangeLocales { get; } = new(StringComparer.Ordinal);

        // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D8 decoders via Broiler.VM JsIntlTables; IP=Low; Security=Low; Resources=0; Fingerprint=099EEC
        // Broiler-Human:        PENDING
        internal Dictionary<string, string> Units { get; } = new(StringComparer.Ordinal);

        // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D8 decoders via Broiler.VM JsIntlTables; IP=Low; Security=Low; Resources=0; Fingerprint=E52EC8
        // Broiler-Human:        PENDING
        internal HashSet<string> UnitLocales { get; } = new(StringComparer.Ordinal);
    }

    /// <summary>The date, hour-cycle and day-period tables, decoded: Broiler.VM's <c>JsDateData</c>'s dictionaries.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D8 decoders via Broiler.VM JsIntlTables; IP=Low; Security=Low; Resources=0; Fingerprint=C45FE9
    // Broiler-Human:        PENDING
    private sealed class DateTables
    {
        /// <summary>The pseudo-locale of the ISO 8601 calendar's patterns, and the suffix of each language's data in that calendar.</summary>
        // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D8 decoders via Broiler.VM JsIntlTables; IP=Low; Security=Low; Resources=0; Fingerprint=B2F82B
        // Broiler-Human:        PENDING
        internal const string Iso8601 = "@iso8601";

        /// <summary>
        /// The calendars whose data is a layer of the date table (Broiler.VM's JSD-0057), each after the one it is a layer over;
        /// null for the layer of CLDR's generic patterns. <c>islamic</c> is a parent only.
        /// </summary>
        // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D8 decoders via Broiler.VM JsIntlTables; IP=Low; Security=Low; Resources=0; Fingerprint=652E11
        // Broiler-Human:        PENDING
        internal static ImmutableArray<(string Calendar, string? Parent)> CalendarLayers { get; } =
        [
            ("buddhist", null), ("chinese", null), ("coptic", null), ("dangi", "chinese"), ("ethiopic", null), ("ethioaa", "ethiopic"),
            ("hebrew", null), ("indian", null), ("islamic", null), ("islamic-civil", "islamic"), ("islamic-tbla", "islamic"),
            ("islamic-umalqura", "islamic"), ("japanese", null), ("persian", null), ("roc", null),
        ];

        // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D8 decoders via Broiler.VM JsIntlTables; IP=Low; Security=Low; Resources=0; Fingerprint=163FE2
        // Broiler-Human:        PENDING
        internal Dictionary<string, Dictionary<string, string>> Locales { get; } = new(StringComparer.Ordinal);

        // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D8 decoders via Broiler.VM JsIntlTables; IP=Low; Security=Low; Resources=0; Fingerprint=266AA6
        // Broiler-Human:        PENDING
        internal Dictionary<string, RegionHourCycles> TimeData { get; } = new(StringComparer.Ordinal);

        // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D8 decoders via Broiler.VM JsIntlTables; IP=Low; Security=Low; Resources=0; Fingerprint=FAA6B6
        // Broiler-Human:        PENDING
        internal Dictionary<string, List<DayPeriodRule>> DayPeriods { get; } = new(StringComparer.Ordinal);

        /// <summary>Whether a key is one of a calendar's patterns rather than one of its names.</summary>
        // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D8 decoders via Broiler.VM JsIntlTables; IP=Low; Security=Low; Resources=1; Fingerprint=362031
        // Broiler-Human:        PENDING
        internal static bool IsPatternKey(string key) =>
            key.StartsWith("dateFormats.", StringComparison.Ordinal) ||
            key.StartsWith("timeFormats.", StringComparison.Ordinal) ||
            key.StartsWith("dateTime.", StringComparison.Ordinal) ||
            key.StartsWith("atTime.", StringComparison.Ordinal) ||
            key.StartsWith("available.", StringComparison.Ordinal) ||
            key.StartsWith("append.", StringComparison.Ordinal) ||
            key.StartsWith("interval.", StringComparison.Ordinal);
    }

    /// <summary>The week, script, list, relative-time and display-name tables, decoded: Broiler.VM's <c>JsLocaleInfo</c>'s dictionaries.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D8 decoders via Broiler.VM JsIntlTables; IP=Low; Security=Low; Resources=0; Fingerprint=B1B534
    // Broiler-Human:        PENDING
    private sealed class LocaleInfoTables
    {
        // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D8 decoders via Broiler.VM JsIntlTables; IP=Low; Security=Low; Resources=0; Fingerprint=52405F
        // Broiler-Human:        PENDING
        internal Dictionary<string, RegionWeek> Weeks { get; } = new(StringComparer.Ordinal);

        // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D8 decoders via Broiler.VM JsIntlTables; IP=Low; Security=Low; Resources=0; Fingerprint=1F42BE
        // Broiler-Human:        PENDING
        internal Dictionary<string, bool> RightToLeft { get; } = new(StringComparer.Ordinal);

        // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D8 decoders via Broiler.VM JsIntlTables; IP=Low; Security=Low; Resources=0; Fingerprint=EF63A2
        // Broiler-Human:        PENDING
        internal Dictionary<string, ListPatterns> ListPatterns { get; } = new(StringComparer.Ordinal);

        // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D8 decoders via Broiler.VM JsIntlTables; IP=Low; Security=Low; Resources=0; Fingerprint=386B9B
        // Broiler-Human:        PENDING
        internal HashSet<string> ListPatternLocales { get; } = new(StringComparer.Ordinal);

        // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D8 decoders via Broiler.VM JsIntlTables; IP=Low; Security=Low; Resources=0; Fingerprint=E1AA84
        // Broiler-Human:        PENDING
        internal Dictionary<string, string> RelativeTimes { get; } = new(StringComparer.Ordinal);

        // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D8 decoders via Broiler.VM JsIntlTables; IP=Low; Security=Low; Resources=0; Fingerprint=AFDFFD
        // Broiler-Human:        PENDING
        internal HashSet<string> RelativeTimeLocales { get; } = new(StringComparer.Ordinal);

        // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D8 decoders via Broiler.VM JsIntlTables; IP=Low; Security=Low; Resources=0; Fingerprint=7A3186
        // Broiler-Human:        PENDING
        internal Dictionary<string, string> DisplayNames { get; } = new(StringComparer.Ordinal);

        // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D8 decoders via Broiler.VM JsIntlTables; IP=Low; Security=Low; Resources=0; Fingerprint=540785
        // Broiler-Human:        PENDING
        internal HashSet<string> DisplayNameLocales { get; } = new(StringComparer.Ordinal);
    }
}
