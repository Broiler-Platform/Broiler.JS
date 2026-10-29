// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   22
// Annotated:        22/22
// Exempt:           3
// Human-reviewed:   0/22
// IP risk:          Low
// Security risk:    Medium
// Criteria:         0/0
// Resource impact:  0/10 max
// Unverified:       22
//
// GENERATED - DO NOT EDIT MANUALLY

using System.Collections.Generic;

namespace Broiler.ECMAScript.Intl;

/// <summary>
/// The locale data the library's algorithms ask for, question by question: the second of the two layers of the data
/// contract (Broiler.VM's JSD-0082, decision D8; the home's design, section 1). <see cref="TableLocaleData"/> answers it
/// from an <see cref="IIntlDataSet"/>'s tables; an engine's secondary provider - Broiler.JS's host-culture provider,
/// decision D6 - answers the questions it can; and <see cref="CompositeLocaleData"/> orders them.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every question answers "no data" as <see langword="null"/></b> (or an empty list, or <see langword="false"/>), never
/// by throwing: the composite then asks the next provider, the locale's parent or root.
/// </para>
/// <para>
/// <b>Two kinds of question.</b> A question that takes a <c>locale</c> or a <c>language</c> is asked of the locale as the
/// tables key it - a CLDR locale such as <c>en</c>, <c>en-GB</c>, <c>zh-Hant</c> or <c>und</c> - and the composite walks
/// the locale's parents; <see cref="HoldsLocale"/> says whether a provider's answers for a locale are its own, so that
/// none is taken from a parent. A question that takes a region, a script, a code or a source is about data CLDR states
/// once for the world (UTS #35 Part 4's supplemental data), and the composite asks the providers in order.
/// </para>
/// <para>
/// <b>An answer is data, not a decision.</b> The algorithms of each sub-area read it: a plural rule is answered as the
/// table's text and evaluated by sub-area (b)'s plural engine, a week as the table's fields and read by sub-area (a)'s
/// Locale info. An implementation answers the same thing for the life of the process and is safe to ask from several
/// threads (convention 10).
/// </para>
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 question layer; IP=Low; Security=Medium; Resources=0; Fingerprint=CCFC5F
// Broiler-Human:        PENDING
public interface ILocaleData
{
    /// <summary>The locales this data supports, in canonical form and in the data's order (the <see cref="IntlTable.Locales"/> table); empty when it states none.</summary>
    IReadOnlyList<string> Locales { get; }

    /// <summary>
    /// Whether this provider's answers from <paramref name="table"/> for <paramref name="locale"/> are its own: the table holds
    /// the locale, so a question it does not answer has no answer on the locale's parent chain either.
    /// </summary>
    /// <param name="table">One of the tables keyed by locale or language (<see cref="IntlTable.NumberLocales"/>,
    /// <see cref="IntlTable.Currencies"/>, <see cref="IntlTable.Units"/>, <see cref="IntlTable.Plurals"/>,
    /// <see cref="IntlTable.Ordinals"/>, <see cref="IntlTable.PluralRanges"/>, <see cref="IntlTable.DateLocales"/>,
    /// <see cref="IntlTable.DayPeriods"/>, <see cref="IntlTable.ListPatterns"/>, <see cref="IntlTable.RelativeTimes"/>,
    /// <see cref="IntlTable.DisplayNames"/>).</param>
    /// <param name="locale">The locale as the tables key it.</param>
    /// <returns><see langword="true"/> when the table holds the locale; <see langword="false"/> otherwise, and for every other table.</returns>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 question layer; IP=Low; Security=Low; Resources=0; Fingerprint=D25A24
    // Broiler-Human:        PENDING
    bool HoldsLocale(IntlTable table, string locale);

    /// <summary>The likely subtags of a source in CLDR's form - <c>en</c>, <c>und-Latn</c>, <c>und-US</c> - or <see langword="null"/> (UTS #35 Part 1, Likely Subtags).</summary>
    /// <param name="source">The source, a language, script and region in CLDR's likely-subtags form.</param>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 question layer; IP=Low; Security=Low; Resources=0; Fingerprint=BE0E46
    // Broiler-Human:        PENDING
    string? LikelySubtags(string source);

    /// <summary>The replacement of an alias of one kind, or <see langword="null"/> (UTS #35 Part 1, Annex C, as CanonicalizeUnicodeLocaleId reads it).</summary>
    /// <param name="kind">The alias kind.</param>
    /// <param name="from">The alias, as the table writes it: a language rule such as <c>sgn-GR</c> or <c>aa-saaho</c>, a script, a region, a variant or a subdivision.</param>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 question layer; IP=Low; Security=Low; Resources=0; Fingerprint=24A5A0
    // Broiler-Human:        PENDING
    string? Alias(LocaleAliasKind kind, string from);

    /// <summary>Whether <paramref name="key"/> is a key of extension <paramref name="singleton"/> the BCP 47 data defines (UTS #35 Part 1, section 3.6).</summary>
    /// <param name="singleton">The extension, <c>u</c> or <c>t</c>.</param>
    /// <param name="key">The key, lower case.</param>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 question layer; IP=Low; Security=Low; Resources=0; Fingerprint=916CE0
    // Broiler-Human:        PENDING
    bool IsExtensionKey(char singleton, string key);

    /// <summary>
    /// Whether <paramref name="type"/> is a type of <paramref name="key"/> the BCP 47 data defines, and its preferred form:
    /// itself, or the type a deprecated one is replaced by (UTS #35 Part 1, section 3.2.1).
    /// </summary>
    /// <param name="singleton">The extension, <c>u</c> or <c>t</c>.</param>
    /// <param name="key">The key, lower case.</param>
    /// <param name="type">The type, lower case.</param>
    /// <param name="preferred">The preferred type when it is defined; <paramref name="type"/> otherwise.</param>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 question layer; IP=Low; Security=Low; Resources=0; Fingerprint=5FA4AE
    // Broiler-Human:        PENDING
    bool TryExtensionType(char singleton, string key, string type, out string preferred);

    /// <summary>The hour cycles a region allows and prefers (UTS #35 Part 4, Time Data), or <see langword="null"/>.</summary>
    /// <param name="region">The region, as CLDR names it, or <c>001</c> for the world.</param>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 question layer; IP=Low; Security=Low; Resources=0; Fingerprint=CDD284
    // Broiler-Human:        PENDING
    RegionHourCycles? HourCycles(string region);

    /// <summary>A region's week as CLDR states it (UTS #35 Part 4, Week Data), or <see langword="null"/>.</summary>
    /// <param name="region">The region, or <c>001</c> for the world.</param>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 question layer; IP=Low; Security=Low; Resources=0; Fingerprint=BD1016
    // Broiler-Human:        PENDING
    RegionWeek? Week(string region);

    /// <summary>Whether a script is written right to left (CLDR's script metadata, as TextDirectionOfLocale reads it), or <see langword="null"/> when the data states no direction for it.</summary>
    /// <param name="script">The script, title case, for example <c>Arab</c>.</param>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 question layer; IP=Low; Security=Low; Resources=0; Fingerprint=D4E0C0
    // Broiler-Human:        PENDING
    bool? IsRightToLeft(string script);

    /// <summary>
    /// One value of a locale's number data by its dotted key - <c>symbols.decimal</c>, <c>decimal.standard</c>,
    /// <c>currency.unitPattern-count-one</c>, <c>symbols@arab.decimal</c> - or <see langword="null"/> (UTS #35 Part 3).
    /// </summary>
    /// <param name="locale">The locale as the tables key it.</param>
    /// <param name="key">The dotted key, as the generator writes it.</param>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 question layer; IP=Low; Security=Low; Resources=0; Fingerprint=6F34A7
    // Broiler-Human:        PENDING
    string? NumberValue(string locale, string key);

    /// <summary>A currency's names in a locale (UTS #35 Part 3, section 4), or <see langword="null"/>.</summary>
    /// <param name="locale">The locale as the tables key it.</param>
    /// <param name="code">The ISO 4217 code, upper case.</param>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 question layer; IP=Low; Security=Low; Resources=0; Fingerprint=ACB3E4
    // Broiler-Human:        PENDING
    CurrencyNames? Currency(string locale, string code);

    /// <summary>Every currency code the currency table names, each once, in the order the table first names it.</summary>
    IReadOnlyList<string> CurrencyCodes { get; }

    /// <summary>A currency's fraction digits when they are not 2 (ECMA-402 CurrencyDigits, <c>sec-currencydigits</c>), or <see langword="null"/>.</summary>
    /// <param name="code">The ISO 4217 code, upper case.</param>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 question layer; IP=Low; Security=Low; Resources=0; Fingerprint=188C8B
    // Broiler-Human:        PENDING
    int? CurrencyDigits(string code);

    /// <summary>The numbering systems with a simple digit mapping, in the table's order (UTS #35 Part 3, section 1).</summary>
    IReadOnlyList<string> NumberingSystems { get; }

    /// <summary>A numbering system's ten digits, each one code point, or <see langword="null"/> (ECMA-402 Table 12, Numbering systems with simple digit mappings).</summary>
    /// <param name="system">The numbering system, for example <c>arab</c>.</param>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 question layer; IP=Low; Security=Low; Resources=0; Fingerprint=5D5D2B
    // Broiler-Human:        PENDING
    IReadOnlyList<string>? NumberingSystemDigits(string system);

    /// <summary>A language's plural rules of one type, in the table's order, each as CLDR's rule text (UTS #35 Part 3, section 5), or <see langword="null"/>.</summary>
    /// <param name="language">The language as the tables key it.</param>
    /// <param name="type">Cardinal or ordinal.</param>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 question layer; IP=Low; Security=Low; Resources=0; Fingerprint=33ECA2
    // Broiler-Human:        PENDING
    IReadOnlyList<PluralRuleText>? PluralRules(string language, PluralRuleType type);

    /// <summary>The plural category of a range from <paramref name="start"/> to <paramref name="end"/> by a language's range rules (UTS #35 Part 3, section 5.3), or <see langword="null"/>.</summary>
    /// <param name="language">The language as the tables key it.</param>
    /// <param name="start">The start's plural category.</param>
    /// <param name="end">The end's plural category.</param>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 question layer; IP=Low; Security=Low; Resources=0; Fingerprint=E28C61
    // Broiler-Human:        PENDING
    string? PluralRange(string language, string start, string end);

    /// <summary>One field of a unit's patterns in a locale (UTS #35 Part 2, Unit Elements), or <see langword="null"/>.</summary>
    /// <param name="locale">The locale as the tables key it.</param>
    /// <param name="width"><c>long</c>, <c>short</c> or <c>narrow</c>.</param>
    /// <param name="unit">The unit, as CLDR names it without its category, for example <c>kilometer</c>.</param>
    /// <param name="field">The field, for example <c>displayName</c>, <c>perUnitPattern</c> or <c>unitPattern-count-one</c>.</param>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 question layer; IP=Low; Security=Low; Resources=0; Fingerprint=0AA41A
    // Broiler-Human:        PENDING
    string? UnitPattern(string locale, string width, string unit, string field);

    /// <summary>A locale's list patterns of one type as CLDR names it - <c>standard</c>, <c>or-short</c>, <c>unit-narrow</c> (UTS #35 Part 2, List Patterns) - or <see langword="null"/>.</summary>
    /// <param name="locale">The locale as the tables key it.</param>
    /// <param name="type">The type, as CLDR names it.</param>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 question layer; IP=Low; Security=Low; Resources=0; Fingerprint=0E6E8A
    // Broiler-Human:        PENDING
    ListPatterns? ListPattern(string locale, string type);

    /// <summary>One value of a locale's relative time data (UTS #35 Part 4, Relative Time), or <see langword="null"/>.</summary>
    /// <param name="locale">The locale as the tables key it.</param>
    /// <param name="field">A unit, or a unit with <c>-short</c> or <c>-narrow</c>.</param>
    /// <param name="key">A value's literal (<c>-1</c>, <c>0</c>, <c>1</c>), or <c>future.</c> or <c>past.</c> and a plural category.</param>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 question layer; IP=Low; Security=Low; Resources=0; Fingerprint=850AED
    // Broiler-Human:        PENDING
    string? RelativeTime(string locale, string field, string key);

    /// <summary>A display name in a locale (UTS #35 Part 2, Display Names), or <see langword="null"/>.</summary>
    /// <param name="locale">The locale as the tables key it.</param>
    /// <param name="kind"><c>language</c>, <c>region</c>, <c>script</c>, <c>variant</c>, <c>pattern</c>, <c>calendar</c> or <c>field</c>.</param>
    /// <param name="code">The code named, as the generator writes it.</param>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 question layer; IP=Low; Security=Low; Resources=0; Fingerprint=934FF6
    // Broiler-Human:        PENDING
    string? DisplayName(string locale, string kind, string code);

    /// <summary>One value of a locale's date data in a calendar by its dotted key (UTS #35 Part 4, Dates), or <see langword="null"/>.</summary>
    /// <param name="locale">The locale as the tables key it.</param>
    /// <param name="calendar">The BCP 47 calendar type: <c>gregory</c> for the locale's Gregorian data, <c>iso8601</c>, or one of the calendars the date table carries a layer of.</param>
    /// <param name="key">The dotted key, as the generator writes it.</param>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 question layer; IP=Low; Security=Low; Resources=0; Fingerprint=11A1A7
    // Broiler-Human:        PENDING
    string? DateValue(string locale, string calendar, string key);

    /// <summary>
    /// Every value of a locale's date data in a calendar, in the order the decoding left them, which the pattern search of
    /// sub-area (d) reads; or <see langword="null"/>.
    /// </summary>
    /// <param name="locale">The locale as the tables key it.</param>
    /// <param name="calendar">As <see cref="DateValue"/>'s.</param>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 question layer; IP=Low; Security=Low; Resources=0; Fingerprint=0F416D
    // Broiler-Human:        PENDING
    IReadOnlyDictionary<string, string>? DateValues(string locale, string calendar);

    /// <summary>A language's day period rules, in the table's order (UTS #35 Part 4, Day Period Rules), or <see langword="null"/>.</summary>
    /// <param name="language">The language as the tables key it.</param>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 question layer; IP=Low; Security=Low; Resources=0; Fingerprint=9862E9
    // Broiler-Human:        PENDING
    IReadOnlyList<DayPeriodRule>? DayPeriods(string language);
}
