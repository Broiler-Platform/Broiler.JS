// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// HAIRS Code Assurance
// --------------------
// Relevant units:   7
// Annotated:        7/7
// Exempt:           9
// Human-reviewed:   0/7
// IP risk:          Low
// Security risk:    Medium
// Criteria:         0/0
// Resource impact:  1/10 max
// Unverified:       7
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Immutable;

namespace Broiler.ECMAScript.Generation.Intl;

/// <summary>Which numbering systems' symbols the number table carries (decision JSD-0082, decision D21).</summary>
// Hairs-AI:           Origin=AI; Spec=JSD-0082 D21 numbering-system symbols; IP=Low; Security=Low; Resources=0; Fingerprint=3E18A8
// Hairs-Human:        PENDING
public enum IntlNumberingSymbols
{
    /// <summary>
    /// The <c>latn</c> symbols alone, for every numbering system: Broiler.VM's tables, whose limitation decision
    /// D21 keeps under its rule N28's budget.
    /// </summary>
    Latin = 0,

    /// <summary>
    /// Besides the <c>latn</c> symbols, each numbering system's own CLDR symbols, as
    /// <c>symbols@&lt;system&gt;.&lt;name&gt;</c> keys of the number table: those a locale's <c>numbers.json</c>
    /// states; those its <c>common/main</c> XML and its parent chain's below root state for the systems
    /// <c>numbers.json</c> leaves out; and for <c>und</c>, root's own from <c>common/main/root.xml</c>
    /// (<c>arab</c> and <c>arabext</c>), which the <c>cldr-json</c> package's root file leaves out. A symbol no
    /// locale on the parent chain states is root's, under <c>und</c>, or where root aliases the system, the
    /// locale's <c>latn</c> symbol.
    /// </summary>
    EveryNumberingSystem = 1,
}

/// <summary>
/// Which plural forms of a currency's display name the currency table carries (the owner's decision of 2026-11-08,
/// "Keep + fix library first", for row N4 of Broiler.VM's JSD-0082).
/// </summary>
// Hairs-AI:           Origin=AI; Spec=JSD-0082 N4 currency plural names; IP=Low; Security=Low; Resources=0; Fingerprint=1903DC
// Hairs-Human:        PENDING
public enum IntlCurrencyNamePlurals
{
    /// <summary>
    /// The <c>one</c> and <c>other</c> forms alone, the table's nine fields: Broiler.VM's tables, which its rule N28 holds
    /// byte for byte.
    /// </summary>
    OneAndOther = 0,

    /// <summary>
    /// Besides them, every other plural form CLDR gives a currency's display name - <c>zero</c>, <c>two</c>, <c>few</c> and
    /// <c>many</c> - as four fields appended to each line, in that order, empty where CLDR gives none.
    /// </summary>
    Every = 1,
}

/// <summary>
/// The generator's parameters (decision JSD-0082, decision D11): the locale list, which numbering systems'
/// symbols the number table carries, which plural forms of a currency's name the currency table carries, the
/// calendars whose layers the date table carries, and the collation tailorings.
/// </summary>
/// <remarks>
/// With Broiler.VM's list (<c>de</c>, <c>de-DE</c>, <c>en</c>, <c>en-US</c> and <c>und</c>, main data for
/// <c>de</c> and <c>en</c>, supplemental rules for <c>de</c> and <c>en</c>) and every other parameter at its
/// default, the generator writes Broiler.VM's tables byte for byte.
/// </remarks>
// Hairs-AI:           Origin=AI; Spec=JSD-0082 D11 generator parameters; IP=Low; Security=Medium; Resources=1; Fingerprint=D5DC31
// Hairs-Human:        PENDING
public sealed record IntlGeneratorOptions
{
    /// <summary>The options for <paramref name="locales"/>, every other parameter at Broiler.VM's value.</summary>
    /// <param name="locales">The embedding's locale list.</param>
    // Hairs-AI:           Origin=AI; Spec=JSD-0082 D11 generator parameters; IP=Low; Security=Low; Resources=0; Fingerprint=A0FF1C
    // Hairs-Human:        PENDING
    public IntlGeneratorOptions(IntlLocaleList locales)
    {
        ArgumentNullException.ThrowIfNull(locales);
        Locales = locales;
    }

    /// <summary>The calendars Broiler.VM's tables carry a layer for, besides the Gregorian, generic and ISO 8601 ones, in the order its generator writes them.</summary>
    // Hairs-AI:           Origin=Ported; Spec=JSD-0082 D11 calendars via Broiler.VM CldrTableGenerator; IP=Low; Security=Low; Resources=0; Fingerprint=CBFBF3
    // Hairs-Human:        PENDING
    public static ImmutableArray<string> BroilerVmCalendars { get; } = ImmutableArray.Create(
        "buddhist", "chinese", "coptic", "dangi", "ethiopic", "ethioaa", "hebrew", "indian", "islamic", "islamic-civil",
        "islamic-tbla", "islamic-umalqura", "japanese", "persian", "roc");

    /// <summary>The collation tailorings Broiler.VM's tables carry, in the order its generator writes them.</summary>
    // Hairs-AI:           Origin=Ported; Spec=JSD-0082 D11 tailorings via Broiler.VM CldrTableGenerator; IP=Low; Security=Low; Resources=0; Fingerprint=529599
    // Hairs-Human:        PENDING
    public static ImmutableArray<string> BroilerVmTailorings { get; } = ImmutableArray.Create("und-search", "de-phonebk", "de-search");

    /// <summary>
    /// Broiler.JS's collation tailorings (milestone SJL-17.3; Broiler.VM's JSD-0084, decision E3): Broiler.VM's three, then every
    /// collation CLDR 48.2.0 gives for <c>es</c>, <c>sv</c> and <c>th</c>, each named <c>language-type</c> by its BCP 47 type
    /// (<c>standard</c>, <c>search</c>, <c>trad</c>). <c>en</c> and <c>de</c>'s standard order are the root's, and CLDR gives them no
    /// tailoring.
    /// </summary>
    // Hairs-AI:           Origin=AI; Spec=JSD-0084 E3 tailorings; IP=Low; Security=Low; Resources=0; Fingerprint=3A957F
    // Hairs-Human:        PENDING
    public static ImmutableArray<string> BroilerJsTailorings { get; } = ImmutableArray.Create(
        "und-search", "de-phonebk", "de-search", "es-standard", "es-search", "es-trad", "sv-standard", "sv-search", "sv-trad", "th-standard");

    /// <summary>The embedding's locale list.</summary>
    public IntlLocaleList Locales { get; }

    /// <summary>Which numbering systems' symbols the number table carries; <see cref="IntlNumberingSymbols.Latin"/> by default.</summary>
    public IntlNumberingSymbols NumberingSymbols { get; init; } = IntlNumberingSymbols.Latin;

    /// <summary>
    /// Which plural forms of a currency's display name the currency table carries; <see cref="IntlCurrencyNamePlurals.OneAndOther"/>,
    /// Broiler.VM's, by default. <i>(Added 2026-11-09, by the owner's decision of 2026-11-08, "Keep + fix library first".)</i>
    /// </summary>
    public IntlCurrencyNamePlurals CurrencyNamePlurals { get; init; } = IntlCurrencyNamePlurals.OneAndOther;

    /// <summary>The calendars the date table carries a layer for, a subset of <see cref="BroilerVmCalendars"/> in its order; all of them by default.</summary>
    public ImmutableArray<string> Calendars { get; init; } = BroilerVmCalendars;

    /// <summary>
    /// The collation tailorings, a subset of <see cref="BroilerJsTailorings"/> in its order; Broiler.VM's, <see cref="BroilerVmTailorings"/>,
    /// by default. <i>(Revised 2026-11-02, at milestone SJL-17.3, which adds Broiler.JS's list; it read "a subset of
    /// <see cref="BroilerVmTailorings"/> in its order; all of them by default".)</i>
    /// </summary>
    public ImmutableArray<string> Tailorings { get; init; } = BroilerVmTailorings;
}
