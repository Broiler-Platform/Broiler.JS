// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   8
// Annotated:        8/8
// Exempt:           11
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
using System.Text;

namespace Broiler.ECMAScript.Intl;

/// <summary>The width of a list, relative-time or display-name format: [[Style]], <c>long</c>, <c>short</c> or <c>narrow</c>.</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-Intl.ListFormat options and parts; IP=Low; Security=Low; Resources=0; Fingerprint=1BB13D
// Broiler-Human:        PENDING
public enum FormatStyle
{
    /// <summary><c>long</c>.</summary>
    Long = 0,

    /// <summary><c>short</c>.</summary>
    Short = 1,

    /// <summary><c>narrow</c>.</summary>
    Narrow = 2,
}

/// <summary>[[Type]] of an <c>Intl.ListFormat</c>.</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-Intl.ListFormat options and parts; IP=Low; Security=Low; Resources=0; Fingerprint=07CE98
// Broiler-Human:        PENDING
public enum ListFormatType
{
    /// <summary><c>conjunction</c>: "A, B, and C".</summary>
    Conjunction = 0,

    /// <summary><c>disjunction</c>: "A, B, or C".</summary>
    Disjunction = 1,

    /// <summary><c>unit</c>: "A, B, C".</summary>
    Unit = 2,
}

/// <summary>One part of a formatted list (<c>sec-formatlisttoparts</c>): an <c>element</c> or a <c>literal</c>.</summary>
/// <param name="Type"><c>element</c> or <c>literal</c>.</param>
/// <param name="Value">The part's text.</param>
// Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-Intl.ListFormat options and parts; IP=Low; Security=Low; Resources=0; Fingerprint=D0BBAF
// Broiler-Human:        PENDING
public readonly record struct ListPart(string Type, string Value);

/// <summary>
/// One configured <c>Intl.ListFormat</c>'s formatting: CreatePartsFromList (<c>sec-createpartsfromlist</c>) over CLDR's list
/// patterns for the format's type and style (Broiler.VM's JSD-0082, sub-area (c), rows S1 to S3).
/// </summary>
/// <remarks>
/// <para>
/// <b>One template per type and style</b> (row S2). ECMA-402 lets an implementation choose among several templates by the
/// elements, as Spanish chooses <c>y</c> or <c>e</c> by the next word; the data carries one per type and style, so the index
/// into [[Templates]] is always the one template CLDR gives. Where the data has none, every pattern is <c>{0}, {1}</c>, as
/// Broiler.VM's.
/// </para>
/// <para>
/// <b>Ported from Broiler.VM's <c>JsRealm.NewListFormat</c>'s templates, <c>CreatePartsFromList</c> and
/// <c>DeconstructPattern</c></b> at its SJL-9 rule commit <c>4cf9ca44</c>, with the caller's <see cref="WorkCharge"/> where it
/// charged its engine's meter, one unit per template step (decision D18). The patterns are read at the data locale, which
/// Broiler.VM read at its language; StringListFromIterable, the option reads and the parts arrays are the engine's.
/// </para>
/// <para><b>Immutable</b>: one may be shared between threads (convention 10).</para>
/// </remarks>
// Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-createpartsfromlist sec-deconstructpattern via Broiler.VM JsRealm.ListFormat; IP=Low; Security=Low; Resources=0; Fingerprint=4B5FA8
// Broiler-Human:        PENDING
public sealed class ListFormatter
{
    private ListFormatter(string locale, ListFormatType type, FormatStyle style, ListPatterns templates)
    {
        Locale = locale;
        Type = type;
        Style = style;
        Templates = templates;
    }

    /// <summary>[[Locale]].</summary>
    public string Locale { get; }

    /// <summary>[[Type]].</summary>
    public ListFormatType Type { get; }

    /// <summary>[[Style]].</summary>
    public FormatStyle Style { get; }

    /// <summary>[[Templates]]: CLDR's start, middle, end and two-element patterns for the type and style.</summary>
    public ListPatterns Templates { get; }

    /// <summary>The formatter an <c>Intl.ListFormat</c> constructor makes from what it resolved (<c>sec-Intl.ListFormat</c>).</summary>
    /// <param name="data">The locale data, ordinarily a <see cref="CompositeLocaleData"/>.</param>
    /// <param name="locale">ResolveLocale's [[Locale]].</param>
    /// <param name="dataLocale">ResolveLocale's [[DataLocale]], at which the patterns are read.</param>
    /// <param name="type">The <c>type</c> option.</param>
    /// <param name="style">The <c>style</c> option.</param>
    /// <returns>The formatter.</returns>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-createpartsfromlist sec-deconstructpattern via Broiler.VM JsRealm.ListFormat; IP=Low; Security=Low; Resources=1; Fingerprint=F496E1
    // Broiler-Human:        PENDING
    public static ListFormatter Create(ILocaleData data, string locale, string dataLocale, ListFormatType type, FormatStyle style)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(locale);
        ArgumentNullException.ThrowIfNull(dataLocale);

        // [[TEMPLATES]]: CLDR's type for the type and style.
        var name = type switch { ListFormatType.Disjunction => "or", ListFormatType.Unit => "unit", _ => "standard" } +
            style switch { FormatStyle.Short => "-short", FormatStyle.Narrow => "-narrow", _ => string.Empty };
        var templates = data.ListPattern(dataLocale, name) ?? new ListPatterns("{0}, {1}", "{0}, {1}", "{0}, {1}", "{0}, {1}");
        return new ListFormatter(locale, type, style, templates);
    }

    /// <summary>FormatList (<c>sec-formatlist</c>): the String CreatePartsFromList's parts make.</summary>
    /// <param name="list">The elements, StringListFromIterable's list.</param>
    /// <param name="charge">The caller's meter: one unit per template step of a list of three elements or more.</param>
    /// <returns>The formatted String.</returns>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-createpartsfromlist sec-deconstructpattern via Broiler.VM JsRealm.ListFormat; IP=Low; Security=Low; Resources=1; Fingerprint=4F0A02
    // Broiler-Human:        PENDING
    public string Format(IReadOnlyList<string> list, WorkCharge? charge = null)
    {
        var text = new StringBuilder();

        foreach (var part in FormatToParts(list, charge))
        {
            text.Append(part.Value);
        }

        return text.ToString();
    }

    /// <summary>
    /// CreatePartsFromList (<c>sec-createpartsfromlist</c>): nothing, the element, the pair pattern, or the end pattern folded
    /// leftward through the middle patterns to the start pattern.
    /// </summary>
    /// <param name="list">The elements.</param>
    /// <param name="charge">The caller's meter, as for <see cref="Format"/>.</param>
    /// <returns>The parts.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="list"/> or an element is null.</exception>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-createpartsfromlist sec-deconstructpattern via Broiler.VM JsRealm.ListFormat; IP=Low; Security=High; Resources=2; Fingerprint=91E3D7
    // Broiler-Falsified-If: the parts differ from CreatePartsFromList's over the type and style's templates, or the charge from one per template step
    // Broiler-Human:        PENDING
    public IReadOnlyList<ListPart> FormatToParts(IReadOnlyList<string> list, WorkCharge? charge = null)
    {
        ArgumentNullException.ThrowIfNull(list);
        var parts = new List<ListPart>();
        var size = list.Count;

        for (var i = 0; i < size; i++)
        {
            ArgumentNullException.ThrowIfNull(list[i], nameof(list));
        }

        if (size == 0)
        {
            return parts;
        }

        if (size == 1)
        {
            parts.Add(new ListPart("element", list[0]));
            return parts;
        }

        if (size == 2)
        {
            return DeconstructPattern(Templates.Pair, [new ListPart("element", list[0])], [new ListPart("element", list[1])]);
        }

        parts.Add(new ListPart("element", list[size - 1]));

        for (var i = size - 2; i >= 0; i--)
        {
            charge?.Invoke(1);
            var pattern = i == 0 ? Templates.Start : i < size - 2 ? Templates.Middle : Templates.End;
            parts = DeconstructPattern(pattern, [new ListPart("element", list[i])], parts);
        }

        return parts;
    }

    /// <summary>
    /// DeconstructPattern (<c>sec-deconstructpattern</c>) over a list pattern: each non-empty literal between the placeables,
    /// and the parts of <c>{0}</c> and <c>{1}</c> in their places.
    /// </summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-createpartsfromlist sec-deconstructpattern via Broiler.VM JsRealm.ListFormat; IP=Low; Security=Low; Resources=1; Fingerprint=845737
    // Broiler-Human:        PENDING
    private static List<ListPart> DeconstructPattern(string pattern, List<ListPart> zero, List<ListPart> one)
    {
        var result = new List<ListPart>();
        var at = 0;

        while (at < pattern.Length)
        {
            var open = pattern.IndexOf('{', at);

            if (open < 0)
            {
                result.Add(new ListPart("literal", pattern[at..]));
                break;
            }

            var close = pattern.IndexOf('}', open);

            if (open > at)
            {
                result.Add(new ListPart("literal", pattern[at..open]));
            }

            result.AddRange(pattern[(open + 1)..close] == "0" ? zero : one);
            at = close + 1;
        }

        return result;
    }
}
