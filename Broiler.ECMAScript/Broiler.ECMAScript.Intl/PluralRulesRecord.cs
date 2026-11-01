// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   8
// Annotated:        8/8
// Exempt:           8
// Human-reviewed:   0/8
// IP risk:          Low
// Security risk:    High
// Criteria:         1/1
// Resource impact:  2/10 max
// Unverified:       8
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Generic;

namespace Broiler.ECMAScript.Intl;

/// <summary>What ResolvePlural answers (<c>sec-resolveplural</c>): the plural category and the String it was selected over.</summary>
/// <param name="PluralCategory">[[PluralCategory]]: <c>zero</c>, <c>one</c>, <c>two</c>, <c>few</c>, <c>many</c> or <c>other</c>.</param>
/// <param name="FormattedString">[[FormattedString]]: FormatNumericToString's String, or <c>NaN</c>, <c>Infinity</c> or <c>-Infinity</c>.</param>
// Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-resolveplural record; IP=Low; Security=Low; Resources=0; Fingerprint=2659B7
// Broiler-Human:        PENDING
public readonly record struct ResolvedPlural(string PluralCategory, string FormattedString);

/// <summary>
/// An <c>Intl.PluralRules</c>'s internal slots once its constructor has read its options (<c>sec-intl.pluralrules</c>,
/// <c>sec-intl.pluralrules-internal-slots</c>), and the operations over them: ResolvePlural (<c>sec-resolveplural</c>),
/// ResolvePluralRange (<c>sec-resolvepluralrange</c>) and the categories <c>resolvedOptions</c> lists (Broiler.VM's JSD-0082,
/// sub-area (b), rows P1 to P8).
/// </summary>
/// <remarks>
/// <para>
/// <b>A plural rules object is a decimal format that selects instead of writing</b>: its digit options are
/// SetNumberFormatDigitOptions' (<see cref="DigitOptionRules"/>) and its rounding FormatNumericToString's
/// (<see cref="NumericFormat"/>), so <see cref="ResolvePlural"/> selects over the String a decimal <c>Intl.NumberFormat</c> with
/// the same options would write before its pattern (row P1).
/// </para>
/// <para>
/// <b>The compact exponent</b> (decision D20). With compact notation, PluralRuleSelect reads the operands <c>c</c> and <c>e</c>
/// as the exponent the compact pattern of the value's magnitude gives, ComputeExponent (<c>sec-computeexponent</c>) over the
/// locale's short or long decimal compact patterns, which the number formatter's own plural selection reads; in every other
/// notation they are 0. Broiler.VM passed 0 in every notation (row P4); no rule of its <c>de</c> and <c>en</c> data reads
/// either operand, so none of its answers moves: a latent correction.
/// </para>
/// <para>
/// <b>Ported from Broiler.VM's <c>JsRealm.NewPluralRules</c>' record, <c>ResolvePlural</c>, <c>ResolvePluralRange</c> and
/// <c>PluralCategoriesOf</c></b>, with <c>JsNumberFormatter</c>'s ComputeExponent for the compact exponent, at its SJL-9 rule
/// commit <c>4cf9ca44</c>, with statuses where it threw and the caller's <see cref="WorkCharge"/> where it charged its engine's
/// meter, in the same units (decision D18). The option reads, in the constructor's order, are the engine's.
/// </para>
/// <para><b>Immutable</b>: one may be shared between threads (convention 10).</para>
/// </remarks>
// Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-resolveplural sec-resolvepluralrange sec-computeexponent via Broiler.VM JsRealm.PluralRules; IP=Low; Security=Low; Resources=0; Fingerprint=ECE56D
// Broiler-Human:        PENDING
public sealed class PluralRulesRecord
{
    private readonly ILocaleData data;

    private PluralRulesRecord(ILocaleData data, string locale, PluralRuleType type, NumberNotation notation, CompactDisplay compactDisplay, DigitOptions digitOptions, PluralEngine engine)
    {
        this.data = data;
        Locale = locale;
        Type = type;
        Notation = notation;
        CompactDisplay = compactDisplay;
        DigitOptions = digitOptions;
        Engine = engine;
    }

    /// <summary>[[Locale]]: the resolved locale.</summary>
    public string Locale { get; }

    /// <summary>[[Type]].</summary>
    public PluralRuleType Type { get; }

    /// <summary>[[Notation]].</summary>
    public NumberNotation Notation { get; }

    /// <summary>[[CompactDisplay]], which <c>resolvedOptions</c> reports only in compact notation.</summary>
    public CompactDisplay CompactDisplay { get; }

    /// <summary>The digit options SetNumberFormatDigitOptions resolved.</summary>
    public DigitOptions DigitOptions { get; }

    /// <summary>The plural engine of the resolved locale's language and the type.</summary>
    public PluralEngine Engine { get; }

    /// <summary>The categories PluralRuleSelect can answer, in <c>resolvedOptions</c>' order (<c>pluralCategories</c>).</summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-resolveplural sec-resolvepluralrange sec-computeexponent via Broiler.VM JsRealm.PluralRules; IP=Low; Security=Low; Resources=0; Fingerprint=7F590F
    // Broiler-Human:        PENDING
    public IReadOnlyList<string> PluralCategories => Engine.Categories;

    /// <summary>
    /// The record an <c>Intl.PluralRules</c> constructor makes from what it read (<c>sec-intl.pluralrules</c>): ResolveLocale's
    /// locale, the type, the notation, the compact display and the digit options, with the plural engine of the data locale's
    /// language.
    /// </summary>
    /// <param name="data">The locale data.</param>
    /// <param name="locale">ResolveLocale's [[Locale]].</param>
    /// <param name="dataLocale">ResolveLocale's [[DataLocale]], whose language subtag selects the rules, as Broiler.VM reads it.</param>
    /// <param name="type">The <c>type</c> option.</param>
    /// <param name="notation">The <c>notation</c> option.</param>
    /// <param name="compactDisplay">The <c>compactDisplay</c> option.</param>
    /// <param name="digitOptions">What SetNumberFormatDigitOptions resolved, with 0 and 3 as the fraction-digit defaults.</param>
    /// <returns>The record.</returns>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-resolveplural sec-resolvepluralrange sec-computeexponent via Broiler.VM JsRealm.PluralRules; IP=Low; Security=Low; Resources=1; Fingerprint=374DE3
    // Broiler-Human:        PENDING
    public static PluralRulesRecord Create(ILocaleData data, string locale, string dataLocale, PluralRuleType type, NumberNotation notation, CompactDisplay compactDisplay, DigitOptions digitOptions)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(locale);
        ArgumentNullException.ThrowIfNull(dataLocale);
        ArgumentNullException.ThrowIfNull(digitOptions);
        var language = LocaleId.TryParse(dataLocale, out var id) == IntlStatus.Completed ? id!.Language : "en";
        return new PluralRulesRecord(data, locale, type, notation, compactDisplay, digitOptions, PluralEngine.Create(data, language, type));
    }

    /// <summary>
    /// ResolvePlural (<c>sec-resolveplural</c>): <c>other</c> for not-a-number and the infinities, and otherwise PluralRuleSelect
    /// over FormatNumericToString's String, with the compact exponent in compact notation (decision D20).
    /// </summary>
    /// <param name="n">The Intl mathematical value.</param>
    /// <param name="charge">The caller's meter: FormatNumericToString's <c>digits / 16 + 1</c> units, which cover the selection's
    /// reading of the String those digits make and the compact exponent's second rounding of the same digits (Broiler.VM's
    /// charges, decision D18).</param>
    /// <returns>The category and the String.</returns>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-resolveplural sec-resolvepluralrange sec-computeexponent via Broiler.VM JsRealm.PluralRules; IP=Low; Security=High; Resources=2; Fingerprint=526DAF
    // Broiler-Falsified-If: the category is selected over another String than FormatNumericToString's, or compact notation passes another exponent than ComputeExponent's
    // Broiler-Human:        PENDING
    public ResolvedPlural ResolvePlural(IntlDecimal n, WorkCharge? charge = null)
    {
        switch (n.Kind)
        {
            case IntlDecimalKind.NaN:
                return new ResolvedPlural("other", "NaN");
            case IntlDecimalKind.PositiveInfinity:
                return new ResolvedPlural("other", "Infinity");
            case IntlDecimalKind.NegativeInfinity:
                return new ResolvedPlural("other", "-Infinity");
        }

        var formatted = NumericFormat.FormatNumericToString(DigitOptions, n, charge).FormattedString;
        return new ResolvedPlural(Engine.Select(formatted, CompactExponent(n)), formatted);
    }

    /// <summary>
    /// ResolvePluralRange (<c>sec-resolvepluralrange</c>): refused at not-a-number; the start's category where both ends write
    /// the same String (row P5a); and otherwise PluralRuleSelectRange over the two categories (row P5b).
    /// </summary>
    /// <param name="x">The start.</param>
    /// <param name="y">The end.</param>
    /// <param name="category">The range's category, or the empty string.</param>
    /// <param name="charge">The caller's meter, charged by each end's <see cref="ResolvePlural"/>.</param>
    /// <returns><see cref="IntlStatus.Completed"/>, or <see cref="IntlStatus.RangeIsNotANumber"/>, a <c>RangeError</c>.</returns>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-resolveplural sec-resolvepluralrange sec-computeexponent via Broiler.VM JsRealm.PluralRules; IP=Low; Security=Low; Resources=1; Fingerprint=7659B0
    // Broiler-Human:        PENDING
    public IntlStatus ResolvePluralRange(IntlDecimal x, IntlDecimal y, out string category, WorkCharge? charge = null)
    {
        if (x.Kind == IntlDecimalKind.NaN || y.Kind == IntlDecimalKind.NaN)
        {
            category = string.Empty;
            return IntlStatus.RangeIsNotANumber;
        }

        var start = ResolvePlural(x, charge);
        var end = ResolvePlural(y, charge);
        category = string.Equals(start.FormattedString, end.FormattedString, StringComparison.Ordinal)
            ? start.PluralCategory
            : Engine.SelectRange(start.PluralCategory, end.PluralCategory);
        return IntlStatus.Completed;
    }

    /// <summary>
    /// The compact decimal exponent of <paramref name="x"/> (decision D20): ComputeExponent (<c>sec-computeexponent</c>) over the
    /// language's decimal compact patterns of the compact display, in compact notation; 0 in every other notation and for zero.
    /// </summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-resolveplural sec-resolvepluralrange sec-computeexponent via Broiler.VM JsRealm.PluralRules; IP=Low; Security=Medium; Resources=2; Fingerprint=464676
    // Broiler-Human:        PENDING
    private int CompactExponent(IntlDecimal x)
    {
        if (Notation != NumberNotation.Compact || x.IsZero)
        {
            return 0;
        }

        var magnitude = x.Magnitude;
        var exponent = ExponentForMagnitude(magnitude);

        // THE SECOND ROUNDING OF THE SAME DIGITS, which ResolvePlural's meter covers, charges nothing of its own.
        var rounded = NumericFormat.FormatNumericToString(DigitOptions, x.Abs().Scale(-exponent)).RoundedNumber;

        if (rounded.IsZero)
        {
            return exponent;
        }

        return rounded.Magnitude == magnitude - exponent ? exponent : ExponentForMagnitude(magnitude + 1);
    }

    /// <summary>
    /// ComputeExponentForMagnitude (<c>sec-computeexponentformagnitude</c>) in compact notation: the magnitude less the compact
    /// pattern's digits after the first, or 0 where the locale does not compact the magnitude.
    /// </summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-resolveplural sec-resolvepluralrange sec-computeexponent via Broiler.VM JsRealm.PluralRules; IP=Low; Security=Low; Resources=1; Fingerprint=E18355
    // Broiler-Human:        PENDING
    private int ExponentForMagnitude(int magnitude)
    {
        if (magnitude < 3)
        {
            return 0;
        }

        var root = CompactDisplay == CompactDisplay.Long ? "decimal.long.decimalFormat." : "decimal.short.decimalFormat.";
        var pattern = data.NumberValue(Engine.Language, root + "1" + new string('0', Math.Min(magnitude, 14)) + "-count-other");

        if (pattern is null or "0")
        {
            return 0;
        }

        var zeros = 0;

        foreach (var c in pattern)
        {
            if (c == '0')
            {
                zeros++;
            }
        }

        return Math.Min(magnitude, 14) - (zeros - 1);
    }
}
