// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   5
// Annotated:        5/5
// Exempt:           4
// Human-reviewed:   0/5
// IP risk:          Low
// Security risk:    High
// Criteria:         1/1
// Resource impact:  3/10 max
// Unverified:       5
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace Broiler.ECMAScript.Generation.Intl;

/// <summary>
/// An embedding's locale list (decision JSD-0082, decision D5): the locales its data supports, those whose
/// CLDR main data is written, and the languages whose plural, ordinal, plural-range and day-period rules are
/// written. The data home keeps one per embedding, <c>Broiler.ECMAScript/data/intl/locales/broiler-vm.txt</c>
/// and <c>broiler-js.txt</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>The text.</b> One statement per line; <c>#</c> starts a comment line and blank lines are skipped.
/// <c>locale &lt;tag&gt;</c> names a locale the data supports, and <c>locale &lt;tag&gt; main</c> one whose
/// own CLDR main data - numbers, currencies, units, dates, lists, relative times and display names - is
/// written; a locale without <c>main</c> reads its data through its CLDR parent. <c>supplemental
/// &lt;language&gt;</c> names a language whose supplemental rules are written, and <c>supplemental *</c>, alone,
/// every language CLDR states each kind of rule for.
/// </para>
/// <para>
/// <b>Order is part of the data.</b> Locales and languages are listed in ordinal order, each once, as the
/// tables write them; a list out of order is refused rather than sorted, so the file reads as the table.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=JSD-0082 D5 D11 locale list; IP=Low; Security=Medium; Resources=2; Fingerprint=2646AD
// Broiler-Human:        PENDING
public sealed class IntlLocaleList
{
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D5 locale list; IP=Low; Security=Low; Resources=0; Fingerprint=6D6227
    // Broiler-Human:        PENDING
    private IntlLocaleList(ImmutableArray<string> locales, ImmutableArray<string> mainDataLocales, ImmutableArray<string> supplementalLanguages, bool everyLanguage)
    {
        Locales = locales;
        MainDataLocales = mainDataLocales;
        SupplementalLanguages = supplementalLanguages;
        EverySupplementalLanguage = everyLanguage;
    }

    /// <summary>Every locale the data supports, in ordinal order: the <c>Locales</c> table.</summary>
    public ImmutableArray<string> Locales { get; }

    /// <summary>The locales whose own CLDR main data is written, in ordinal order.</summary>
    public ImmutableArray<string> MainDataLocales { get; }

    /// <summary>The languages whose supplemental rules are written, in ordinal order; empty when <see cref="EverySupplementalLanguage"/>.</summary>
    public ImmutableArray<string> SupplementalLanguages { get; }

    /// <summary>Whether every language CLDR states a kind of supplemental rule for has that kind written (<c>supplemental *</c>).</summary>
    public bool EverySupplementalLanguage { get; }

    /// <summary>Parses a locale list's text.</summary>
    /// <param name="text">The list's text.</param>
    /// <exception cref="InvalidDataException">A line is not one of the statements above, a tag is malformed, repeated or out of order, or no locale is listed.</exception>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D5 D11 locale list; IP=Low; Security=High; Resources=3; Fingerprint=6C3047
    // Broiler-Falsified-If: a list out of ordinal order, with a repeated tag, a malformed tag or line, or no locale, is accepted
    // Broiler-Human:        PENDING
    public static IntlLocaleList Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var locales = new List<string>();
        var main = new List<string>();
        var supplemental = new List<string>();
        var every = false;

        foreach (var raw in text.Split('\n'))
        {
            var line = raw.Trim();

            if (line.Length == 0 || line[0] == '#')
            {
                continue;
            }

            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts[0] == "locale" && (parts.Length == 2 || parts.Length == 3 && parts[2] == "main") && IsTag(parts[1]))
            {
                Append(locales, parts[1], line);

                if (parts.Length == 3)
                {
                    main.Add(parts[1]);
                }

                continue;
            }

            if (parts[0] == "supplemental" && parts.Length == 2 && (parts[1] == "*" || IsTag(parts[1])))
            {
                if (parts[1] == "*")
                {
                    every = true;
                }
                else
                {
                    Append(supplemental, parts[1], line);
                }

                if (every && supplemental.Count > 0)
                {
                    throw new InvalidDataException("the locale list names `supplemental *` beside named languages");
                }

                continue;
            }

            throw new InvalidDataException("`" + line + "` is not `locale <tag>`, `locale <tag> main` or `supplemental <language>|*`");
        }

        if (locales.Count == 0)
        {
            throw new InvalidDataException("the locale list names no locale");
        }

        return new IntlLocaleList([.. locales], [.. main], [.. supplemental], every);
    }

    /// <summary>Appends a tag, refusing one that repeats or does not follow the last in ordinal order.</summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D5 locale list; IP=Low; Security=Medium; Resources=1; Fingerprint=804F2D
    // Broiler-Human:        PENDING
    private static void Append(List<string> list, string tag, string line)
    {
        if (list.Count > 0 && string.CompareOrdinal(list[^1], tag) >= 0)
        {
            throw new InvalidDataException("`" + line + "` repeats a tag or is out of ordinal order");
        }

        list.Add(tag);
    }

    /// <summary>A tag of ASCII letters and digits in subtags of one to eight, separated by hyphens.</summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D5 locale list; IP=Low; Security=Medium; Resources=1; Fingerprint=0CDCF2
    // Broiler-Human:        PENDING
    private static bool IsTag(string tag) =>
        tag.Split('-').All(static subtag => subtag.Length is >= 1 and <= 8 &&
            subtag.All(static c => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9'));
}
