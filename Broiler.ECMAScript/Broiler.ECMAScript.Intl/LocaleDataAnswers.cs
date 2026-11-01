// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   8
// Annotated:        8/8
// Exempt:           7
// Human-reviewed:   0/8
// IP risk:          Low
// Security risk:    Low
// Criteria:         0/0
// Resource impact:  0/10 max
// Unverified:       8
//
// GENERATED - DO NOT EDIT MANUALLY

namespace Broiler.ECMAScript.Intl;

/// <summary>The kinds of CLDR alias (UTS #35 Part 1, Annex C), as the <see cref="IntlTable.Aliases"/> table names them.</summary>
// Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 answers; IP=Low; Security=Low; Resources=0; Fingerprint=D07CBE
// Broiler-Human:        PENDING
public enum LocaleAliasKind
{
    /// <summary><c>language</c>: a language rule, which may carry a script, a region and variants (<c>languageAlias</c>).</summary>
    Language = 0,

    /// <summary><c>script</c> (<c>scriptAlias</c>).</summary>
    Script = 1,

    /// <summary><c>region</c>, whose replacement may be a list of regions (<c>territoryAlias</c>).</summary>
    Region = 2,

    /// <summary><c>variant</c> (<c>variantAlias</c>).</summary>
    Variant = 3,

    /// <summary><c>subdivision</c>, whose replacement may be a list of subdivisions or a region (<c>subdivisionAlias</c>).</summary>
    Subdivision = 4,
}

/// <summary>The two types of plural rule (UTS #35 Part 3, section 5; ECMA-402's PluralRules <c>type</c> option).</summary>
// Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 answers; IP=Low; Security=Low; Resources=0; Fingerprint=33D4C4
// Broiler-Human:        PENDING
public enum PluralRuleType
{
    /// <summary>Cardinal: the <see cref="IntlTable.Plurals"/> table.</summary>
    Cardinal = 0,

    /// <summary>Ordinal: the <see cref="IntlTable.Ordinals"/> table.</summary>
    Ordinal = 1,
}

/// <summary>A currency's names in one locale (UTS #35 Part 3, section 4); an empty field is one CLDR does not give.</summary>
/// <param name="Symbol">The symbol, for example <c>$</c> or <c>US$</c>.</param>
/// <param name="Narrow">The narrow symbol.</param>
/// <param name="One">The display name for the plural category <c>one</c>.</param>
/// <param name="Other">The display name for the plural category <c>other</c>.</param>
/// <param name="Name">The display name.</param>
/// <param name="SymbolEnds">For the symbol, whether its first and its last character are a symbol or separator (<c>S</c>) or not (<c>L</c>): two characters, as UTS #35 Part 3's currency spacing reads them.</param>
/// <param name="NarrowEnds">The same for the narrow symbol.</param>
// Broiler-AI:           Origin=Ported; Spec=JSD-0082 D8 answers; IP=Low; Security=Low; Resources=0; Fingerprint=EEC6FA
// Broiler-Human:        PENDING
public readonly record struct CurrencyNames(string Symbol, string Narrow, string One, string Other, string Name, string SymbolEnds, string NarrowEnds);

/// <summary>A locale's list patterns of one type (UTS #35 Part 2, List Patterns): the patterns of a list's start, middle and end, and of a list of two.</summary>
/// <param name="Start">The pattern joining the first element to the rest.</param>
/// <param name="Middle">The pattern joining a middle element to the rest.</param>
/// <param name="End">The pattern joining the last two elements.</param>
/// <param name="Pair">The pattern of a list of two.</param>
// Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 answers; IP=Low; Security=Low; Resources=0; Fingerprint=B69E5B
// Broiler-Human:        PENDING
public readonly record struct ListPatterns(string Start, string Middle, string End, string Pair);

/// <summary>The hour cycles a region allows and prefers, as CLDR's time data writes them (UTS #35 Part 4, Time Data).</summary>
/// <param name="Allowed">The allowed formats, space-separated, for example <c>h H hB hb</c>.</param>
/// <param name="Preferred">The preferred hour format, for example <c>h</c>.</param>
// Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 answers; IP=Low; Security=Low; Resources=0; Fingerprint=C1BCE3
// Broiler-Human:        PENDING
public readonly record struct RegionHourCycles(string Allowed, string Preferred);

/// <summary>A region's week, as CLDR's week data writes it (UTS #35 Part 4, Week Data); a field is empty where the world's (<c>001</c>) applies.</summary>
/// <param name="FirstDay">The first day of the week, for example <c>mon</c>.</param>
/// <param name="WeekendStart">The first day of the weekend.</param>
/// <param name="WeekendEnd">The last day of the weekend.</param>
/// <param name="MinDays">The minimal days in the first week, as digits.</param>
// Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 answers; IP=Low; Security=Low; Resources=0; Fingerprint=0CBCC4
// Broiler-Human:        PENDING
public readonly record struct RegionWeek(string FirstDay, string WeekendStart, string WeekendEnd, string MinDays);

/// <summary>A day period rule (UTS #35 Part 4, Day Period Rules): the period, and the minute of the day it is at, or the minutes it is from and before.</summary>
/// <param name="Period">The period, for example <c>morning1</c> or <c>noon</c>.</param>
/// <param name="From">The minute of the day the period is at or starts from.</param>
/// <param name="Before">The minute of the day the period ends before, or <c>-1</c> for a period that is at one minute.</param>
/// <param name="At">Whether the period is at <see cref="From"/> alone.</param>
// Broiler-AI:           Origin=Ported; Spec=JSD-0082 D8 answers; IP=Low; Security=Low; Resources=0; Fingerprint=C5B0BD
// Broiler-Human:        PENDING
public readonly record struct DayPeriodRule(string Period, int From, int Before, bool At);

/// <summary>A plural rule as CLDR writes it (UTS #35 Part 3, section 5.1): its category and its condition's text, empty for <c>other</c>.</summary>
/// <param name="Category">The category, for example <c>one</c>.</param>
/// <param name="Rule">The condition, for example <c>i = 1 and v = 0</c>.</param>
// Broiler-AI:           Origin=AI; Spec=JSD-0082 D8 answers; IP=Low; Security=Low; Resources=0; Fingerprint=93CC83
// Broiler-Human:        PENDING
public readonly record struct PluralRuleText(string Category, string Rule);
