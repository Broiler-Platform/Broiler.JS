// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   5
// Annotated:        5/5
// Exempt:           6
// Human-reviewed:   0/5
// IP risk:          Low
// Security risk:    Medium
// Criteria:         0/0
// Resource impact:  1/10 max
// Unverified:       5
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Immutable;

namespace Broiler.ECMAScript.Generation.Intl;

/// <summary>Which numbering systems' symbols the number table carries (decision JSD-0082, decision D21).</summary>
// Broiler-AI:           Origin=AI; Spec=JSD-0082 D21 numbering-system symbols; IP=Low; Security=Low; Resources=0; Fingerprint=3E18A8
// Broiler-Human:        PENDING
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
/// The generator's parameters (decision JSD-0082, decision D11): the locale list, which numbering systems'
/// symbols the number table carries, the calendars whose layers the date table carries, and the collation
/// tailorings.
/// </summary>
/// <remarks>
/// With Broiler.VM's list (<c>de</c>, <c>de-DE</c>, <c>en</c>, <c>en-US</c> and <c>und</c>, main data for
/// <c>de</c> and <c>en</c>, supplemental rules for <c>de</c> and <c>en</c>) and every other parameter at its
/// default, the generator writes Broiler.VM's tables byte for byte.
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=JSD-0082 D11 generator parameters; IP=Low; Security=Medium; Resources=1; Fingerprint=D5DC31
// Broiler-Human:        PENDING
public sealed record IntlGeneratorOptions
{
    /// <summary>The options for <paramref name="locales"/>, every other parameter at Broiler.VM's value.</summary>
    /// <param name="locales">The embedding's locale list.</param>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D11 generator parameters; IP=Low; Security=Low; Resources=0; Fingerprint=A0FF1C
    // Broiler-Human:        PENDING
    public IntlGeneratorOptions(IntlLocaleList locales)
    {
        ArgumentNullException.ThrowIfNull(locales);
        Locales = locales;
    }

    /// <summary>The calendars Broiler.VM's tables carry a layer for, besides the Gregorian, generic and ISO 8601 ones, in the order its generator writes them.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 calendars via Broiler.VM CldrTableGenerator; IP=Low; Security=Low; Resources=0; Fingerprint=CBFBF3
    // Broiler-Human:        PENDING
    public static ImmutableArray<string> BroilerVmCalendars { get; } = ImmutableArray.Create(
        "buddhist", "chinese", "coptic", "dangi", "ethiopic", "ethioaa", "hebrew", "indian", "islamic", "islamic-civil",
        "islamic-tbla", "islamic-umalqura", "japanese", "persian", "roc");

    /// <summary>The collation tailorings Broiler.VM's tables carry, in the order its generator writes them.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 tailorings via Broiler.VM CldrTableGenerator; IP=Low; Security=Low; Resources=0; Fingerprint=529599
    // Broiler-Human:        PENDING
    public static ImmutableArray<string> BroilerVmTailorings { get; } = ImmutableArray.Create("und-search", "de-phonebk", "de-search");

    /// <summary>The embedding's locale list.</summary>
    public IntlLocaleList Locales { get; }

    /// <summary>Which numbering systems' symbols the number table carries; <see cref="IntlNumberingSymbols.Latin"/> by default.</summary>
    public IntlNumberingSymbols NumberingSymbols { get; init; } = IntlNumberingSymbols.Latin;

    /// <summary>The calendars the date table carries a layer for, a subset of <see cref="BroilerVmCalendars"/> in its order; all of them by default.</summary>
    public ImmutableArray<string> Calendars { get; init; } = BroilerVmCalendars;

    /// <summary>The collation tailorings, a subset of <see cref="BroilerVmTailorings"/> in its order; all of them by default.</summary>
    public ImmutableArray<string> Tailorings { get; init; } = BroilerVmTailorings;
}
