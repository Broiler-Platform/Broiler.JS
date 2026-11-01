using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Broiler.ECMAScript.Intl.Tests.Differential.Vm;

namespace Broiler.ECMAScript.Intl.Tests.Differential;

/// <summary>
/// Section D0, the skeleton's own (the plan's task 5.0): Broiler.VM's <c>JsIntlTables</c> decoders against the library's
/// <see cref="TableLocaleData"/>, over every line of every table they decode in each embedding, and seeded random misses.
/// Each answer is compared as Broiler.VM's code reads it and as the library answers the question, so that every later
/// section's data is known to be the same data.
/// </summary>
internal static class DecoderSection
{
    private const string TagAlphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-";

    internal static IEnumerable<Section> Run(Embedding embedding, SeededRandom random, int scale)
    {
        var vm = new JsIntlTables(embedding.Vm);
        var library = new TableLocaleData(embedding.Library);
        var suffix = " (" + embedding.Name + ")";

        yield return Identifiers(embedding, vm, library, random, scale, "D0.1 likely subtags, aliases, extensions and locales" + suffix);
        yield return Numbers(vm, library, random, scale, "D0.2 numbers, currencies, units, plural rules and ranges" + suffix);
        yield return Dates(vm, library, random, scale, "D0.3 date data in every calendar, hour cycles and day periods" + suffix);
        yield return LocaleInfo(vm, library, random, scale, "D0.4 weeks, scripts, lists, relative times and display names" + suffix);
    }

    private static Section Identifiers(Embedding embedding, JsIntlTables vm, TableLocaleData library, SeededRandom random, int scale, string name)
    {
        var section = new Section(name);

        var sources = embedding.Lines(IntlTable.LikelySubtags).Select(static l => l[..l.IndexOf('|')]).ToList();
        foreach (var source in sources.Concat(Misses(random, sources, scale)))
            section.Compare("likely " + source, vm.Likely(source), library.LikelySubtags(source));

        var kinds = new[] { "language", "script", "region", "variant", "subdivision" };
        var aliases = embedding.Lines(IntlTable.Aliases).Select(static l => l.Split('|')).ToList();
        foreach (var fields in aliases)
        {
            foreach (var kind in kinds)
                section.Compare("alias " + kind + " " + fields[1], vm.Alias(kind, fields[1]), library.Alias(Kind(kind), fields[1]));
        }

        foreach (var miss in Misses(random, aliases.Select(static f => f[1]).ToList(), scale))
            section.Compare("alias language " + miss, vm.Alias("language", miss), library.Alias(LocaleAliasKind.Language, miss));

        var extensions = embedding.Lines(IntlTable.Extensions).Select(static l => l.Split('|')).ToList();
        foreach (var fields in extensions)
        {
            var singleton = fields[0][0];
            section.Compare("key " + fields[0] + "-" + fields[1], Bool(vm.IsKey(singleton, fields[1])), Bool(library.IsExtensionKey(singleton, fields[1])));
            section.Compare("type " + fields[0] + "-" + fields[1] + "-" + fields[2],
                Type(vm.TryType(singleton, fields[1], fields[2], out var vmPreferred), vmPreferred),
                Type(library.TryExtensionType(singleton, fields[1], fields[2], out var preferred), preferred));
        }

        for (var i = 0; i < 1000 * scale; i++)
        {
            var fields = random.Pick(extensions);
            var singleton = random.Next(3) == 0 ? 'x' : fields[0][0];
            var key = random.Next(2) == 0 ? fields[1] : random.NextString("abcdefghijklmnopqrstuvwxyz0123456789", 2, 2);
            var type = random.NextString("abcdefghijklmnopqrstuvwxyz0123456789-", 0, 9);
            section.Compare("key " + singleton + "-" + key, Bool(vm.IsKey(singleton, key)), Bool(library.IsExtensionKey(singleton, key)));
            section.Compare("type " + singleton + "-" + key + "-" + type,
                Type(vm.TryType(singleton, key, type, out var vmPreferred), vmPreferred),
                Type(library.TryExtensionType(singleton, key, type, out var preferred), preferred));
        }

        section.Compare("locales", string.Join(" ", vm.Locales), string.Join(" ", library.Locales));
        section.Compare("cldr-version", vm.CldrVersion, library.CldrVersion);
        return section;
    }

    private static Section Numbers(JsIntlTables vm, TableLocaleData library, SeededRandom random, int scale, string name)
    {
        var section = new Section(name);
        var numbers = vm.Numbers;

        foreach (var (language, values) in numbers.Locales)
        {
            section.Compare("holds numbers " + language, Bool(true), Bool(library.HoldsLocale(IntlTable.NumberLocales, language)));
            foreach (var (key, value) in values)
                section.Compare("number " + language + " " + key, value, library.NumberValue(language, key));
            foreach (var miss in Misses(random, [.. values.Keys], scale / 10 + 1))
                section.Compare("number " + language + " " + miss, numbers.Value(language, miss), library.NumberValue(language, miss));
        }

        var languages = numbers.Locales.Keys.ToList();
        foreach (var miss in Misses(random, languages, scale))
        {
            section.Compare("holds numbers " + miss, Bool(numbers.Locales.ContainsKey(miss)), Bool(library.HoldsLocale(IntlTable.NumberLocales, miss)));
            section.Compare("number " + miss + " symbols.decimal", numbers.Value(miss, "symbols.decimal"), library.NumberValue(miss, "symbols.decimal"));
        }

        foreach (var (key, names) in numbers.Currencies)
        {
            var bar = key.IndexOf('|');
            section.Compare("currency " + key, Render(names), library.Currency(key[..bar], key[(bar + 1)..]) is { } c ? Render(c) : null);
        }

        section.Compare("currency codes",
            string.Join(" ", numbers.Currencies.Keys.Select(static k => k[(k.IndexOf('|') + 1)..]).Distinct(StringComparer.Ordinal)),
            string.Join(" ", library.CurrencyCodes));

        foreach (var (code, digits) in numbers.CurrencyDigits)
            section.Compare("currency digits " + code, digits.ToString(CultureInfo.InvariantCulture), library.CurrencyDigits(code)?.ToString(CultureInfo.InvariantCulture));

        foreach (var code in Misses(random, [.. numbers.CurrencyDigits.Keys], scale))
            section.Compare("currency digits " + code, numbers.CurrencyDigits.TryGetValue(code, out var d) ? d.ToString(CultureInfo.InvariantCulture) : null, library.CurrencyDigits(code)?.ToString(CultureInfo.InvariantCulture));

        section.Compare("numbering systems", string.Join(" ", numbers.NumberingSystems.Keys), string.Join(" ", library.NumberingSystems));
        foreach (var (system, digits) in numbers.NumberingSystems)
            section.Compare("digits " + system, string.Join(" ", digits), library.NumberingSystemDigits(system) is { } ds ? string.Join(" ", ds) : null);

        foreach (var (rules, type) in new[] { (numbers.Plurals, PluralRuleType.Cardinal), (numbers.Ordinals, PluralRuleType.Ordinal) })
        {
            foreach (var (language, list) in rules)
            {
                var text = library.PluralRules(language, type);
                section.Compare("plural categories " + type + " " + language, string.Join(" ", list.Select(static r => r.Category)), text is null ? null : string.Join(" ", text.Select(static r => r.Category)));

                // The library keeps the rule's text; Broiler.VM parsed it. Selected through Broiler.VM's own rule reader, the two must
                // choose the same category for every operand, which they do only if each rule's text is the one Broiler.VM read.
                var parsed = text?.Select(static r => (r.Category, Rule: JsPluralRule.Parse(r.Rule))).ToList() ?? [];
                for (var i = 0; i < 40 * scale; i++)
                {
                    var operands = Operands(random);
                    section.Compare("plural " + type + " " + language + " " + Render(operands),
                        numbers.Plural(language, operands, type == PluralRuleType.Ordinal), Select(parsed, operands));
                }
            }
        }

        foreach (var (key, result) in numbers.PluralRanges)
        {
            var fields = key.Split('|');
            section.Compare("plural range " + key, result, library.PluralRange(fields[0], fields[1], fields[2]));
        }

        foreach (var (key, value) in numbers.Units)
        {
            var fields = key.Split('|');
            section.Compare("unit " + key, value, library.UnitPattern(fields[0], fields[1], fields[2], fields[3]));
        }

        foreach (var (table, holds) in new[]
        {
            (IntlTable.Currencies, numbers.Currencies.Keys.Select(static k => k[..k.IndexOf('|')])),
            (IntlTable.Units, numbers.Units.Keys.Select(static k => k[..k.IndexOf('|')])),
            (IntlTable.Plurals, numbers.Plurals.Keys.AsEnumerable()),
            (IntlTable.Ordinals, numbers.Ordinals.Keys.AsEnumerable()),
            (IntlTable.PluralRanges, numbers.PluralRanges.Keys.Select(static k => k[..k.IndexOf('|')])),
        })
        {
            foreach (var language in holds.Distinct(StringComparer.Ordinal))
                section.Compare("holds " + table + " " + language, Bool(true), Bool(library.HoldsLocale(table, language)));
        }

        return section;
    }

    private static Section Dates(JsIntlTables vm, TableLocaleData library, SeededRandom random, int scale, string name)
    {
        var section = new Section(name);
        var dates = vm.Dates;

        foreach (var (dataLanguage, values) in dates.Locales)
        {
            // "@iso8601" alone is the ISO 8601 calendar's root patterns, a pseudo-locale each language's iso8601 layer is built
            // from; no question asks for it, and its values are compared through every language's layer.
            if (dataLanguage.StartsWith('@'))
                continue;

            var at = dataLanguage.IndexOf('@');
            var language = at < 0 ? dataLanguage : dataLanguage[..at];
            var calendar = at < 0 ? "gregory" : dataLanguage[(at + 1)..];

            foreach (var (key, value) in values)
                section.Compare("date " + dataLanguage + " " + key, value, library.DateValue(language, calendar, key));

            section.Compare("date order " + dataLanguage, string.Join("\n", values.Keys), library.DateValues(language, calendar) is { } all ? string.Join("\n", all.Keys) : null);

            foreach (var miss in Misses(random, [.. values.Keys], scale / 10 + 1))
                section.Compare("date " + dataLanguage + " " + miss, dates.Value(dataLanguage, miss), library.DateValue(language, calendar, miss));

            if (at < 0)
                section.Compare("holds dates " + language, Bool(true), Bool(library.HoldsLocale(IntlTable.DateLocales, language)));
        }

        var calendars = new[] { "gregory", "iso8601", "buddhist", "chinese", "coptic", "dangi", "ethiopic", "ethioaa", "hebrew", "indian", "islamic",
            "islamic-civil", "islamic-tbla", "islamic-umalqura", "islamic-rgsa", "japanese", "persian", "roc", "generic", "unknown" };
        foreach (var miss in Misses(random, [.. dates.Locales.Keys], scale))
        {
            // The question Broiler.VM's key names: its language is the key before its first '@', and its calendar the rest.
            var picked = random.Pick(calendars);
            var dataLanguage = picked == "gregory" ? miss : miss + "@" + picked;
            var at = dataLanguage.IndexOf('@');
            var language = at < 0 ? dataLanguage : dataLanguage[..at];
            var calendar = at < 0 ? "gregory" : dataLanguage[(at + 1)..];
            if (language.Length == 0)
                continue;
            section.Compare("date " + dataLanguage + " months.format.wide.1", dates.Value(dataLanguage, "months.format.wide.1"), library.DateValue(language, calendar, "months.format.wide.1"));
        }

        foreach (var (region, (allowed, preferred)) in dates.TimeData)
            section.Compare("hour cycles " + region, allowed + "|" + preferred, library.HourCycles(region) is { } h ? h.Allowed + "|" + h.Preferred : null);

        foreach (var region in Misses(random, [.. dates.TimeData.Keys], scale))
            section.Compare("hour cycles " + region, dates.TimeData.TryGetValue(region, out var h) ? h.Allowed + "|" + h.Preferred : null, library.HourCycles(region) is { } l ? l.Allowed + "|" + l.Preferred : null);

        foreach (var (language, rules) in dates.DayPeriods)
        {
            section.Compare("day periods " + language,
                string.Join(" ", rules.Select(static r => Render(r.Period, r.From, r.Before, r.At))),
                library.DayPeriods(language) is { } lr ? string.Join(" ", lr.Select(static r => Render(r.Period, r.From, r.Before, r.At))) : null);
            section.Compare("holds day periods " + language, Bool(true), Bool(library.HoldsLocale(IntlTable.DayPeriods, language)));
        }

        return section;
    }

    private static Section LocaleInfo(JsIntlTables vm, TableLocaleData library, SeededRandom random, int scale, string name)
    {
        var section = new Section(name);
        var info = vm.LocaleInfo;

        foreach (var (region, week) in info.Weeks)
            section.Compare("week " + region, string.Join("|", week.FirstDay, week.WeekendStart, week.WeekendEnd, week.MinDays),
                library.Week(region) is { } w ? string.Join("|", w.FirstDay, w.WeekendStart, w.WeekendEnd, w.MinDays) : null);

        foreach (var region in Misses(random, [.. info.Weeks.Keys], scale))
            section.Compare("week " + region, info.Weeks.ContainsKey(region) ? "held" : null, library.Week(region) is null ? null : "held");

        foreach (var (script, rightToLeft) in info.RightToLeft)
            section.Compare("direction " + script, Bool(rightToLeft), library.IsRightToLeft(script) is { } r ? Bool(r) : null);

        foreach (var script in Misses(random, [.. info.RightToLeft.Keys], scale))
            section.Compare("direction " + script, info.RightToLeft.TryGetValue(script, out var r) ? Bool(r) : null, library.IsRightToLeft(script) is { } l ? Bool(l) : null);

        foreach (var (key, patterns) in info.ListPatterns)
        {
            var bar = key.IndexOf('|');
            section.Compare("list " + key, string.Join("|", patterns.Start, patterns.Middle, patterns.End, patterns.Pair),
                library.ListPattern(key[..bar], key[(bar + 1)..]) is { } p ? string.Join("|", p.Start, p.Middle, p.End, p.Pair) : null);
            section.Compare("holds lists " + key[..bar], Bool(true), Bool(library.HoldsLocale(IntlTable.ListPatterns, key[..bar])));
        }

        foreach (var (key, value) in info.RelativeTimes)
        {
            var fields = key.Split('|');
            section.Compare("relative time " + key, value, library.RelativeTime(fields[0], fields[1], fields[2]));
        }

        foreach (var (key, value) in info.DisplayNames)
        {
            var fields = key.Split('|');
            section.Compare("display name " + key, value, library.DisplayName(fields[0], fields[1], fields[2]));
        }

        foreach (var miss in Misses(random, [.. info.DisplayNames.Keys.Select(static k => k[..k.IndexOf('|')]).Distinct(StringComparer.Ordinal)], scale))
            section.Compare("display name " + miss + " language en", info.DisplayNames.GetValueOrDefault(miss + "|language|en"), library.DisplayName(miss, "language", "en"));

        return section;
    }

    /// <summary>Seeded inputs the tables do not hold: each a mutation of a held one or a random tag, drawn <paramref name="scale"/> times 100.</summary>
    private static IEnumerable<string> Misses(SeededRandom random, IReadOnlyList<string> held, int scale)
    {
        for (var i = 0; i < 100 * scale; i++)
        {
            if (held.Count == 0 || random.Next(3) == 0)
            {
                yield return random.NextString(TagAlphabet, 0, 12);
                continue;
            }

            var source = random.Pick(held);
            yield return random.Next(4) switch
            {
                0 => source + random.NextString(TagAlphabet, 1, 3),
                1 => source.Length > 1 ? source[..^1] : source + "x",
                2 => source.ToUpperInvariant(),
                _ => source.Replace('-', '_'),
            };
        }
    }

    private static JsPluralOperands Operands(SeededRandom random)
    {
        var integer = random.Next(5) switch
        {
            0 => random.Next(3).ToString(CultureInfo.InvariantCulture),
            1 => random.Next(30).ToString(CultureInfo.InvariantCulture),
            2 => random.Next(1000).ToString(CultureInfo.InvariantCulture),
            3 => random.Next(10_000_000).ToString(CultureInfo.InvariantCulture),
            _ => random.NextString("0123456789", 1, 20),
        };
        var fraction = random.Next(2) == 0 ? string.Empty : random.NextString("0123456789", 1, 4);
        var exponent = random.Next(4) == 0 ? random.Next(7) : 0;
        return JsPluralOperands.Of(fraction.Length == 0 ? integer : integer + "." + fraction, exponent);
    }

    /// <summary>The plural category the rules select, as Broiler.VM's <c>JsNumberData.Plural</c> selects it: the first holding rule but <c>other</c>.</summary>
    private static string Select(IReadOnlyList<(string Category, JsPluralRule Rule)> rules, JsPluralOperands operands)
    {
        foreach (var (category, rule) in rules)
        {
            if (category != "other" && rule.Holds(operands))
                return category;
        }

        return "other";
    }

    private static LocaleAliasKind Kind(string kind) => kind switch
    {
        "language" => LocaleAliasKind.Language,
        "script" => LocaleAliasKind.Script,
        "region" => LocaleAliasKind.Region,
        "variant" => LocaleAliasKind.Variant,
        _ => LocaleAliasKind.Subdivision,
    };

    private static string Bool(bool value) => value ? "true" : "false";

    private static string Type(bool defined, string preferred) => defined ? "defined " + preferred : "undefined " + preferred;

    private static string Render(JsCurrencyNames names) =>
        string.Join("|", names.Symbol, names.Narrow, names.One, names.Other, names.Name, names.SymbolEnds, names.NarrowEnds);

    private static string Render(CurrencyNames names) =>
        string.Join("|", names.Symbol, names.Narrow, names.One, names.Other, names.Name, names.SymbolEnds, names.NarrowEnds);

    private static string Render(JsPluralOperands operands) =>
        operands.Integer.ToString(CultureInfo.InvariantCulture) + (operands.IntegerHuge ? "+" : string.Empty) + "." + operands.Fraction + "e" + operands.CompactExponent.ToString(CultureInfo.InvariantCulture);

    private static string Render(string period, int from, int before, bool at) =>
        period + "@" + from.ToString(CultureInfo.InvariantCulture) + (at ? string.Empty : "-" + before.ToString(CultureInfo.InvariantCulture));
}
