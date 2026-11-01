// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Text;
using Broiler.ECMAScript.Numbers;

namespace Broiler.ECMAScript.Intl;

/// <summary>[[Numeric]] of an <c>Intl.RelativeTimeFormat</c>.</summary>
public enum RelativeTimeNumeric
{
    /// <summary><c>always</c>: a number in every case, "in 1 day".</summary>
    Always = 0,

    /// <summary><c>auto</c>: CLDR's literal where it names one, "tomorrow".</summary>
    Auto = 1,
}

/// <summary>One part of a formatted relative time (<c>sec-FormatRelativeTimeToParts</c>).</summary>
/// <param name="Type">A number part's type, or <c>literal</c>.</param>
/// <param name="Value">The part's text.</param>
/// <param name="Unit">The singular unit, for a part of the number; <see langword="null"/> for a literal.</param>
public readonly record struct RelativeTimePart(string Type, string Value, string? Unit);

/// <summary>
/// One configured <c>Intl.RelativeTimeFormat</c>'s formatting: PartitionRelativeTimePattern (<c>sec-PartitionRelativeTimePattern</c>)
/// over CLDR's relative-time patterns, with the format's own number format and plural rules (Broiler.VM's JSD-0082, sub-area
/// (c), rows S4 to S7).
/// </summary>
/// <remarks>
/// <para>
/// <b>The number is the format's own [[NumberFormat]] and the category its own [[PluralRules]]</b>, each made as the
/// constructor makes them: a decimal format of the locale and the resolved numbering system (row S5), and cardinal plural
/// rules, both with the default digit options.
/// </para>
/// <para>
/// <b>The number written is the value's magnitude</b> (decision D33 (a), row S7). The tense carries the sign: CLDR's past
/// patterns say "ago", and test262's <c>en-us-numeric-always.js</c>, both engines and ICU write <c>-1</c> days as "1 day ago".
/// The pinned text's step passes the signed value to PartitionNumberPattern, whose <c>signDisplay</c> is <c>auto</c>; the
/// decision makes the passing test the arbiter.
/// </para>
/// <para>
/// <b>Ported from Broiler.VM's <c>JsRealm.NewRelativeTimeFormat</c>'s record, <c>SingularRelativeTimeUnit</c> and
/// <c>PartitionRelativeTimePattern</c></b> at its SJL-9 rule commit <c>4cf9ca44</c>, with a status where it threw and the
/// caller's <see cref="WorkCharge"/> where its number format and plural rules charged its engine's meter (decision D18). The
/// patterns are read at the data locale, which Broiler.VM read at its language; the reads of the value and the unit, the
/// option reads and the parts arrays are the engine's.
/// </para>
/// <para><b>Immutable</b>: one may be shared between threads (convention 10).</para>
/// </remarks>
public sealed class RelativeTimeFormatter
{
    private readonly ILocaleData data;

    private readonly string dataLocale;

    private RelativeTimeFormatter(ILocaleData data, string locale, string dataLocale, FormatStyle style, RelativeTimeNumeric numeric, NumberFormatter numberFormatter, PluralRulesRecord pluralRules)
    {
        this.data = data;
        this.dataLocale = dataLocale;
        Locale = locale;
        Style = style;
        Numeric = numeric;
        NumberFormat = numberFormatter;
        PluralRules = pluralRules;
    }

    /// <summary>[[Locale]].</summary>
    public string Locale { get; }

    /// <summary>[[Style]].</summary>
    public FormatStyle Style { get; }

    /// <summary>[[Numeric]].</summary>
    public RelativeTimeNumeric Numeric { get; }

    /// <summary>[[NumberingSystem]]: [[NumberFormat]]'s.</summary>
    public string NumberingSystem => NumberFormat.NumberingSystem;

    /// <summary>[[NumberFormat]]: a decimal format of the locale and the numbering system.</summary>
    public NumberFormatter NumberFormat { get; }

    /// <summary>[[PluralRules]]: the locale's cardinal plural rules.</summary>
    public PluralRulesRecord PluralRules { get; }

    /// <summary>
    /// The formatter an <c>Intl.RelativeTimeFormat</c> constructor makes from what it resolved (<c>sec-Intl.RelativeTimeFormat</c>),
    /// with its [[NumberFormat]] and [[PluralRules]] made as the constructor makes them.
    /// </summary>
    /// <param name="data">The locale data, ordinarily a <see cref="CompositeLocaleData"/>.</param>
    /// <param name="locale">ResolveLocale's [[Locale]].</param>
    /// <param name="dataLocale">ResolveLocale's [[DataLocale]], at which the patterns are read.</param>
    /// <param name="style">The <c>style</c> option.</param>
    /// <param name="numeric">The <c>numeric</c> option.</param>
    /// <param name="numberingSystem">The resolved <c>nu</c> key's value, which the constructor hands its NumberFormat.</param>
    /// <returns>The formatter.</returns>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    public static RelativeTimeFormatter Create(ILocaleData data, string locale, string dataLocale, FormatStyle style, RelativeTimeNumeric numeric, string numberingSystem)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(locale);
        ArgumentNullException.ThrowIfNull(dataLocale);
        ArgumentNullException.ThrowIfNull(numberingSystem);

        // [[NUMBERFORMAT]] AND [[PLURALRULES]], CONSTRUCTED AS THE CONSTRUCTOR CONSTRUCTS THEM: no option but the numbering system.
        var defaults = new DigitOptionValues(1, false, null, null, false, null, null, 1, RoundingMode.HalfExpand, RoundingPriority.Auto, TrailingZeroDisplay.Auto);
        _ = DigitOptionRules.SetNumberFormatDigitOptions(defaults, DigitOptionRules.NumberFormatDefaults(NumberStyle.Decimal, null, NumberNotation.Standard, data), NumberNotation.Standard, out var numberDigits);
        _ = DigitOptionRules.SetNumberFormatDigitOptions(defaults, new FractionDigitDefaults(0, 3), NumberNotation.Standard, out var pluralDigits);
        var numberFormat = NumberFormatter.Create(data, locale, dataLocale, numberingSystem, new NumberFormatOptions { DigitOptions = numberDigits! });
        var pluralRules = PluralRulesRecord.Create(data, locale, dataLocale, PluralRuleType.Cardinal, NumberNotation.Standard, CompactDisplay.Short, pluralDigits!);
        return new RelativeTimeFormatter(data, locale, dataLocale, style, numeric, numberFormat, pluralRules);
    }

    /// <summary>SingularRelativeTimeUnit (<c>sec-singularrelativetimeunit</c>): a unit, or its plural, as the singular.</summary>
    /// <param name="unit">The unit, as ToString of the argument.</param>
    /// <param name="singular">The singular unit, or the empty string.</param>
    /// <returns><see cref="IntlStatus.Completed"/>, or <see cref="IntlStatus.InvalidRelativeTimeUnit"/>, a <c>RangeError</c>.</returns>
    public static IntlStatus SingularRelativeTimeUnit(string unit, out string singular)
    {
        ArgumentNullException.ThrowIfNull(unit);
        singular = unit switch
        {
            "second" or "seconds" => "second",
            "minute" or "minutes" => "minute",
            "hour" or "hours" => "hour",
            "day" or "days" => "day",
            "week" or "weeks" => "week",
            "month" or "months" => "month",
            "quarter" or "quarters" => "quarter",
            "year" or "years" => "year",
            _ => string.Empty,
        };
        return singular.Length == 0 ? IntlStatus.InvalidRelativeTimeUnit : IntlStatus.Completed;
    }

    /// <summary>FormatRelativeTime (<c>sec-FormatRelativeTime</c>): the String PartitionRelativeTimePattern's parts make.</summary>
    /// <param name="value">ToNumber of the value.</param>
    /// <param name="unit">ToString of the unit.</param>
    /// <param name="formatted">The String, or the empty string.</param>
    /// <param name="charge">The caller's meter: the plural rules' and the number format's FormatNumericToString, each
    /// <c>digits / 16 + 1</c> units, where a number is written.</param>
    /// <returns>As <see cref="FormatToParts"/>.</returns>
    public IntlStatus Format(double value, string unit, out string formatted, WorkCharge? charge = null)
    {
        var status = FormatToParts(value, unit, out var parts, charge);
        var text = new StringBuilder();

        foreach (var part in parts)
        {
            text.Append(part.Value);
        }

        formatted = text.ToString();
        return status;
    }

    /// <summary>
    /// PartitionRelativeTimePattern (<c>sec-PartitionRelativeTimePattern</c>): the literal for the value where the numeric option
    /// is <c>auto</c> and CLDR names one, and otherwise the tense's pattern for the plural category, its placeable the formatted
    /// magnitude's parts, each with the unit.
    /// </summary>
    /// <param name="value">ToNumber of the value.</param>
    /// <param name="unit">ToString of the unit.</param>
    /// <param name="parts">The parts; empty where refused.</param>
    /// <param name="charge">The caller's meter, as for <see cref="Format"/>.</param>
    /// <returns><see cref="IntlStatus.Completed"/>; <see cref="IntlStatus.RelativeTimeValueNotFinite"/>, a <c>RangeError</c>, for
    /// not-a-number or an infinity, checked first; or <see cref="IntlStatus.InvalidRelativeTimeUnit"/>, a <c>RangeError</c>.</returns>
    public IntlStatus FormatToParts(double value, string unit, out IReadOnlyList<RelativeTimePart> parts, WorkCharge? charge = null)
    {
        ArgumentNullException.ThrowIfNull(unit);
        parts = [];

        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return IntlStatus.RelativeTimeValueNotFinite;
        }

        if (SingularRelativeTimeUnit(unit, out var singular) != IntlStatus.Completed)
        {
            return IntlStatus.InvalidRelativeTimeUnit;
        }

        var field = singular;

        if (Style != FormatStyle.Long)
        {
            var styled = singular + (Style == FormatStyle.Short ? "-short" : "-narrow");

            if (data.RelativeTime(dataLocale, styled, "future.other") is not null)
            {
                field = styled;
            }
        }

        var result = new List<RelativeTimePart>();

        if (Numeric == RelativeTimeNumeric.Auto && data.RelativeTime(dataLocale, field, NumberText.ToString(value)) is { } literal)
        {
            result.Add(new RelativeTimePart("literal", literal, null));
            parts = result;
            return IntlStatus.Completed;
        }

        var tense = value < 0 || (value == 0 && double.IsNegative(value)) ? "past" : "future";
        var magnitude = IntlDecimal.FromNumber(Math.Abs(value));
        var category = PluralRules.ResolvePlural(magnitude, charge).PluralCategory;
        // A LOCALE WITH NO PATTERN FOR THE TENSE writes the number alone, where Broiler.VM's table lookup threw: CLDR gives every
        // locale and root a pattern, so no input of either embedding reaches it, and the library never throws for want of data.
        var pattern = data.RelativeTime(dataLocale, field, tense + "." + category) ?? data.RelativeTime(dataLocale, field, tense + ".other") ?? "{0}";

        // MAKEPARTSLIST (sec-makepartslist): the literals of the pattern, and the number's parts in its place.
        var at = 0;

        while (at < pattern.Length)
        {
            var open = pattern.IndexOf("{0}", at, StringComparison.Ordinal);

            if (open < 0)
            {
                result.Add(new RelativeTimePart("literal", pattern[at..], null));
                break;
            }

            if (open > at)
            {
                result.Add(new RelativeTimePart("literal", pattern[at..open], null));
            }

            foreach (var part in NumberFormat.FormatToParts(magnitude, charge))
            {
                result.Add(new RelativeTimePart(part.Type, part.Value, singular));
            }

            at = open + 3;
        }

        parts = result;
        return IntlStatus.Completed;
    }
}
