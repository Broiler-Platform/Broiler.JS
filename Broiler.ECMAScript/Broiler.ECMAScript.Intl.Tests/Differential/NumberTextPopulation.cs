using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Broiler.ECMAScript.Conformance;

namespace Broiler.ECMAScript.Intl.Tests.Differential;

/// <summary>One call of sub-area (c) a vector's script makes, with its own object's locale and options.</summary>
/// <param name="Id">The vector.</param>
/// <param name="Service"><c>NumberFormat</c>, <c>ListFormat</c>, <c>RelativeTimeFormat</c> or <c>DisplayNames</c>.</param>
/// <param name="Locale">The constructor's locale.</param>
/// <param name="Bag">The constructor's options.</param>
/// <param name="Method">The method called.</param>
/// <param name="Arguments">Its literal arguments.</param>
internal sealed record TextCall(string Id, string Service, string Locale, OptionsBag Bag, string Method, IReadOnlyList<object?> Arguments);

/// <summary>
/// The population of sub-area (c)'s sections (Broiler.VM's JSD-0082, decision D22): every Intl vector's own call of a
/// number, list, relative-time or display-name operation; every dataset input of the numbers, lists, relative-times and
/// display-names programs, as library calls; the Number, String and BigInt values sub-area (b)'s population holds; and seeded
/// random formats, values, ranges, lists, units and codes over the embedding's locales.
/// </summary>
internal sealed class NumberTextPopulation
{
    /// <summary>Each NumberFormat a dataset case or a vector constructs, at its own locale, then the random ones.</summary>
    internal List<(string Locale, OptionsBag Bag)> NumberFormats { get; } = [];

    /// <summary>The numbers dataset's cases, which the sections format with every dataset value and range.</summary>
    internal List<(string Locale, OptionsBag Bag)> DatasetNumberFormats { get; } = [];

    /// <summary>The values a format is handed: the datasets', the vectors', sub-area (b)'s and seeded random ones.</summary>
    internal List<DecimalInput> Values { get; } = [];

    internal List<DecimalInput> DatasetValues { get; } = [];

    internal List<(DecimalInput Start, DecimalInput End)> DatasetRanges { get; } = [];

    internal List<(DecimalInput Start, DecimalInput End)> Ranges { get; } = [];

    /// <summary>The lists dataset's locales, types, styles and lists, and the random lists.</summary>
    internal List<(string Locale, OptionsBag Bag)> ListFormats { get; } = [];

    internal List<List<string>> Lists { get; } = [];

    internal List<(string Locale, OptionsBag Bag)> RelativeTimeFormats { get; } = [];

    internal List<(double Value, string Unit)> RelativeTimes { get; } = [];

    /// <summary>The relative-times dataset's formats and inputs, which the section compares in full.</summary>
    internal List<(string Locale, OptionsBag Bag)> DatasetRelativeTimeFormats { get; } = [];

    internal List<(double Value, string Unit)> DatasetRelativeTimes { get; } = [];

    internal List<(string Locale, OptionsBag Bag)> DisplayNameFormats { get; } = [];

    /// <summary>Each display-name type's codes: the dataset's valid and invalid ones, the vectors' and random ones.</summary>
    internal Dictionary<string, List<string>> Codes { get; } = new(StringComparer.Ordinal);

    internal List<string> Locales { get; } = [];

    /// <summary>Each vector's own call, with its literal arguments.</summary>
    internal List<TextCall> VectorCalls { get; } = [];

    internal static readonly string[] DisplayTypes = ["language", "region", "script", "currency", "calendar", "dateTimeField"];

    private static readonly string[] Units = ["second", "minute", "hour", "day", "week", "month", "quarter", "year"];

    internal static NumberTextPopulation Build(Embedding embedding, SeededRandom random, int scale)
    {
        var population = new NumberTextPopulation();
        foreach (var type in DisplayTypes)
            population.Codes[type] = [];

        // EVERY VECTOR'S OWN CALL of the four services, from the files whose scripts sub-area (c) reads.
        foreach (var file in new[] { "numbers-and-text.vectors", "values-and-plurals.vectors" })
        {
            foreach (var vector in Population.IntlVectors(file))
            {
                foreach (var call in Calls(vector["id"] ?? string.Empty, vector.Script))
                {
                    if (file == "values-and-plurals.vectors" && call.Service != "NumberFormat")
                        continue;
                    population.VectorCalls.Add(call);
                    population.Locales.Add(call.Locale);
                }

                foreach (var input in LibraryRun.Inputs(vector))
                {
                    if (input.Kind == TypedValueKind.Number)
                        population.Values.Add(DecimalInput.Of(input.Number));
                }
            }
        }

        foreach (var call in population.VectorCalls)
        {
            switch (call.Service)
            {
                case "NumberFormat":
                    population.NumberFormats.Add((call.Locale, call.Bag));
                    foreach (var argument in call.Arguments)
                    {
                        if (DecimalPopulation.Input(argument) is { } value)
                            population.Values.Add(value);
                    }

                    if (call.Method is "formatRange" or "formatRangeToParts" && call.Arguments.Count == 2
                        && DecimalPopulation.Input(call.Arguments[0]) is { } start && DecimalPopulation.Input(call.Arguments[1]) is { } end)
                        population.Ranges.Add((start, end));
                    break;
                case "ListFormat":
                    population.ListFormats.Add((call.Locale, call.Bag));
                    if (call.Arguments.Count == 1 && call.Arguments[0] is List<object?> items && items.All(static i => i is string))
                        population.Lists.Add(items.Cast<string>().ToList());
                    break;
                case "RelativeTimeFormat":
                    population.RelativeTimeFormats.Add((call.Locale, call.Bag));
                    if (call.Arguments.Count == 2 && call.Arguments[0] is double d && call.Arguments[1] is string unit)
                        population.RelativeTimes.Add((d, unit));
                    break;
                case "DisplayNames":
                    population.DisplayNameFormats.Add((call.Locale, call.Bag));
                    if (call.Bag["type"] is string type && population.Codes.TryGetValue(type, out var codes) && call.Arguments.Count == 1 && call.Arguments[0] is string code)
                        codes.Add(code);
                    break;
            }
        }

        // EVERY DATASET INPUT: the numbers program's values, ranges and cases; the lists, relative-times and display-names
        // programs' locales, options and inputs.
        foreach (var (name, program) in Population.DatasetPrograms())
        {
            switch (name)
            {
                case "numbers":
                    foreach (var item in DecimalPopulation.Array(program, "values"))
                    {
                        if (DecimalPopulation.Input(item) is { } input)
                            population.DatasetValues.Add(input);
                    }

                    foreach (var item in DecimalPopulation.Array(program, "ranges"))
                    {
                        if (item is List<object?> pair && pair.Count == 2 && DecimalPopulation.Input(pair[0]) is { } start && DecimalPopulation.Input(pair[1]) is { } end)
                            population.DatasetRanges.Add((start, end));
                    }

                    foreach (var item in DecimalPopulation.Array(program, "cases"))
                    {
                        if (item is List<object?> format && format.Count > 0 && format[0] is string locale)
                            population.DatasetNumberFormats.Add((locale, format.Count > 1 && format[1] is OptionsBag bag ? bag : OptionsBag.Empty));
                    }

                    break;
                case "lists":
                    foreach (var locale in Strings(program, "locales"))
                    {
                        foreach (var type in Strings(program, "types"))
                        {
                            foreach (var style in Strings(program, "styles"))
                                population.ListFormats.Add((locale, Bag(("type", type), ("style", style))));
                        }
                    }

                    foreach (var item in DecimalPopulation.Array(program, "lists"))
                    {
                        if (item is List<object?> list && list.All(static i => i is string))
                            population.Lists.Add(list.Cast<string>().ToList());
                    }

                    break;
                case "relativetimes":
                    foreach (var locale in Strings(program, "locales"))
                    {
                        foreach (var style in Strings(program, "styles"))
                        {
                            foreach (var numeric in Strings(program, "numerics"))
                                population.DatasetRelativeTimeFormats.Add((locale, Bag(("style", style), ("numeric", numeric))));
                        }
                    }

                    foreach (var unit in Strings(program, "units"))
                    {
                        foreach (var item in DecimalPopulation.Array(program, "values"))
                        {
                            if (item is double value)
                                population.DatasetRelativeTimes.Add((value, unit));
                        }
                    }

                    break;
                case "displaynames":
                    foreach (var locale in Strings(program, "locales"))
                    {
                        foreach (var type in DisplayTypes)
                        {
                            foreach (var style in Strings(program, "styles"))
                            {
                                foreach (var display in type == "language" ? new[] { "dialect", "standard" } : new string?[] { null })
                                {
                                    foreach (var fallback in new[] { "code", "none" })
                                    {
                                        var entries = new List<(string, object)> { ("type", type), ("style", style), ("fallback", fallback) };
                                        if (display is not null)
                                            entries.Add(("languageDisplay", display));
                                        population.DisplayNameFormats.Add((locale, Bag([.. entries])));
                                    }
                                }
                            }
                        }
                    }

                    foreach (var type in DisplayTypes)
                    {
                        population.Codes[type].AddRange(Keyed(program, "codes", type));
                        population.Codes[type].AddRange(Keyed(program, "invalid", type));
                    }

                    break;
            }
        }

        population.RelativeTimeFormats.AddRange(population.DatasetRelativeTimeFormats);
        population.RelativeTimes.AddRange(population.DatasetRelativeTimes);

        // THE EMBEDDING'S OWN LOCALES, and sub-area (b)'s values (its population, from the seed's own draw).
        population.Locales.AddRange(embedding.Lines(IntlTable.Locales).Select(static l => l.Split('|')[0]));
        population.Values.AddRange(population.DatasetValues);

        // SEEDED RANDOM FORMATS, VALUES, RANGES, LISTS, RELATIVE TIMES AND CODES.
        var locales = population.Locales.Distinct(StringComparer.Ordinal).ToList();
        for (var i = 0; i < 500 * scale; i++)
            population.Values.Add(DecimalPopulation.RandomValue(random));
        for (var i = 0; i < 120 * scale; i++)
            population.NumberFormats.Add((random.Pick(locales), RandomNumberBag(random)));
        for (var i = 0; i < 150 * scale; i++)
            population.Ranges.Add((random.Pick(population.Values), random.Pick(population.Values)));
        for (var i = 0; i < 40 * scale; i++)
            population.ListFormats.Add((random.Pick(locales), Bag(("type", random.Pick(new[] { "conjunction", "disjunction", "unit" })), ("style", random.Pick(new[] { "long", "short", "narrow" })))));
        for (var i = 0; i < 60 * scale; i++)
        {
            var list = new List<string>();
            var size = random.Next(7);
            for (var j = 0; j < size; j++)
                list.Add(random.NextString("ab{}01 ,\u00e9\u0661", 0, 5));
            population.Lists.Add(list);
        }

        for (var i = 0; i < 40 * scale; i++)
        {
            var entries = new List<(string, object)> { ("style", random.Pick(new[] { "long", "short", "narrow" })), ("numeric", random.Pick(new[] { "always", "auto" })) };
            if (random.Next(4) == 0)
                entries.Add(("numberingSystem", random.Pick(new[] { "arab", "thai", "latn", "deva", "hanidec", "mathbold", "roman" })));
            population.RelativeTimeFormats.Add((random.Pick(locales), Bag([.. entries])));
        }

        for (var i = 0; i < 300 * scale; i++)
        {
            var unit = random.Pick(Units) + (random.Next(3) == 0 ? "s" : string.Empty);
            if (random.Next(20) == 0)
                unit = random.Pick(new[] { "decade", "Day", "days ", "", "millisecond" });
            var value = random.Next(10) switch
            {
                0 => random.Pick(new[] { -0.0, 0.0, double.NaN, double.PositiveInfinity, -1.0, 1.0, 2.0, -2.0 }),
                1 or 2 => Math.Round((random.Next(4000) - 2000) / 7.0, random.Next(4)),
                _ => random.Next(41) - 20,
            };
            population.RelativeTimes.Add((value, unit));
        }

        for (var i = 0; i < 40 * scale; i++)
        {
            var type = random.Pick(DisplayTypes);
            var entries = new List<(string, object)> { ("type", type), ("style", random.Pick(new[] { "long", "short", "narrow" })), ("fallback", random.Pick(new[] { "code", "none" })) };
            if (type == "language" && random.Next(2) == 0)
                entries.Add(("languageDisplay", random.Pick(new[] { "dialect", "standard" })));
            population.DisplayNameFormats.Add((random.Pick(locales), Bag([.. entries])));
        }

        for (var i = 0; i < 60 * scale; i++)
        {
            population.Codes["language"].Add(random.Pick(locales) is var tag && random.Next(3) == 0 ? CaseFlipped(random, tag) : tag);
            population.Codes["region"].Add(random.Next(3) == 0 ? random.NextString("0123456789", 3, 3) : random.NextString("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz", 2, 2));
            population.Codes["script"].Add(random.Pick(new[] { "Latn", "Cyrl", "Arab", "Hans", "Hant", "Grek", "Deva", "Thai", "Kore", "Jpan" }) is var script && random.Next(2) == 0 ? CaseFlipped(random, script) : script);
            population.Codes["currency"].Add(random.NextString("ABCDEFGHIJKLMNOPQRSTUVWXYZ", 3, 3));
            population.Codes["calendar"].Add(random.Pick(new[] { "gregory", "buddhist", "chinese", "coptic", "dangi", "ethiopic", "hebrew", "indian", "islamic", "japanese", "persian", "roc", "iso8601", "islamic-civil" }));
            population.Codes["dateTimeField"].Add(random.Pick(new[] { "era", "year", "quarter", "month", "weekOfYear", "weekday", "day", "dayPeriod", "hour", "minute", "second", "timeZoneName", "Month", "zone" }));
        }

        Distinct(population.NumberFormats, static f => f.Locale + " " + f.Bag.Label);
        Distinct(population.ListFormats, static f => f.Locale + " " + f.Bag.Label);
        Distinct(population.RelativeTimeFormats, static f => f.Locale + " " + f.Bag.Label);
        Distinct(population.DisplayNameFormats, static f => f.Locale + " " + f.Bag.Label);
        Distinct(population.Values, static v => v.Label + (v.Text is null ? string.Empty : "s"));
        Distinct(population.DatasetValues, static v => v.Label + (v.Text is null ? string.Empty : "s"));
        Distinct(population.Lists, static l => string.Join("\u0001", l) + "\u0002" + l.Count);
        Distinct(population.RelativeTimes, static r => BitConverter.DoubleToInt64Bits(r.Value) + " " + r.Unit);
        foreach (var codes in population.Codes.Values)
            Distinct(codes, static c => c);
        Distinct(population.Locales, static l => l);
        return population;
    }

    /// <summary>A NumberFormat's options bag of sub-area (c): a style and its currency or unit, a notation, the grouping, the sign, and now and then a digit option or a value no list admits.</summary>
    private static OptionsBag RandomNumberBag(SeededRandom random)
    {
        var entries = new List<(string, object)>();
        var style = random.Pick(new[] { "decimal", "percent", "currency", "currency", "unit", "unit" });
        if (random.Next(5) != 0)
            entries.Add(("style", style));
        else
            style = "decimal";

        if (style == "currency")
        {
            entries.Add(("currency", random.Pick(new[] { "USD", "EUR", "JPY", "KWD", "CHF", "GBP", "BTN", "XYZ", "eur", "INR" })));
            if (random.Next(2) == 0)
                entries.Add(("currencyDisplay", random.Pick(new[] { "symbol", "narrowSymbol", "code", "name" })));
            if (random.Next(3) == 0)
                entries.Add(("currencySign", random.Pick(new[] { "standard", "accounting" })));
        }

        if (style == "unit")
        {
            entries.Add(("unit", random.Pick(new[] { "liter", "kilometer-per-hour", "percent", "byte-per-second", "kilobit-per-stone", "hour", "megabyte", "liter-per-kilometer", "fahrenheit", "nanosecond" })));
            if (random.Next(2) == 0)
                entries.Add(("unitDisplay", random.Pick(new[] { "short", "narrow", "long" })));
        }

        if (random.Next(2) == 0)
            entries.Add(("notation", random.Pick(new[] { "standard", "scientific", "engineering", "compact", "compact" })));
        if (random.Next(4) == 0)
            entries.Add(("compactDisplay", random.Pick(new[] { "short", "long" })));
        if (random.Next(4) == 0)
            entries.Add(("useGrouping", random.Pick(new object[] { false, true, "min2", "always", "auto", "true" })));
        if (random.Next(4) == 0)
            entries.Add(("signDisplay", random.Pick(new[] { "auto", "never", "always", "exceptZero", "negative" })));
        if (random.Next(5) == 0)
            entries.Add(("maximumFractionDigits", (double)random.Next(6)));
        if (random.Next(6) == 0)
            entries.Add(("maximumSignificantDigits", (double)(1 + random.Next(8))));
        if (random.Next(25) == 0 && random.Pick(new[] { "style", "currencyDisplay", "notation", "useGrouping", "signDisplay" }) is var bogus && !entries.Exists(e => e.Item1 == bogus))
            entries.Add((bogus, "bogus"));
        return Bag([.. entries]);
    }

    private static void Distinct<T>(List<T> list, Func<T, string> key)
    {
        var kept = list.DistinctBy(key, StringComparer.Ordinal).ToList();
        list.Clear();
        list.AddRange(kept);
    }

    private static string CaseFlipped(SeededRandom random, string text)
    {
        var chars = text.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (char.IsAsciiLetter(chars[i]) && random.Next(2) == 0)
                chars[i] = (char)(chars[i] ^ 0x20);
        }

        return new string(chars);
    }

    private static OptionsBag Bag(params (string Key, object Value)[] entries) =>
        new([.. entries.Select(static e => new KeyValuePair<string, object>(e.Key, e.Value))]);

    private static IEnumerable<string> Strings(string program, string name) => DecimalPopulation.Array(program, name).OfType<string>();

    /// <summary>The Strings of one key of a program's <c>var name = { key: [...], ... }</c>.</summary>
    private static IEnumerable<string> Keyed(string program, string name, string key)
    {
        var at = program.IndexOf("var " + name + " = {", StringComparison.Ordinal);
        var end = program.IndexOf("};", at, StringComparison.Ordinal);
        var field = program.IndexOf(key + ": [", at, StringComparison.Ordinal);
        if (at < 0 || field < 0 || field > end)
            return [];
        var reader = new DecimalPopulation.LiteralReader(program, field + key.Length + 2);
        return (reader.Value() as List<object?> ?? []).OfType<string>();
    }

    /// <summary>
    /// The call a vector's script makes, when it is one constructor of the four services with a String locale and an
    /// optional options bag of literals and one method of literal arguments - possibly inside <c>JSON.stringify</c>, joined
    /// with another in an array, or <c>toLocaleString</c> through a NumberFormat; none for any other script.
    /// </summary>
    private static IEnumerable<TextCall> Calls(string id, string script)
    {
        var text = script.Trim();
        if (text.StartsWith("[", StringComparison.Ordinal) && text.EndsWith("].join(\"|\")", StringComparison.Ordinal))
        {
            foreach (var item in text[1..^"].join(\"|\")".Length].Split(", new Intl.", StringSplitOptions.None).Select(static (s, i) => i == 0 ? s : "new Intl." + s))
            {
                if (Call(id, item) is { } part)
                    yield return part;
            }

            yield break;
        }

        if (text.StartsWith("JSON.stringify(", StringComparison.Ordinal) && text.EndsWith(')'))
            text = text["JSON.stringify(".Length..^1];

        if (Call(id, text) is { } call)
            yield return call;
    }

    private static TextCall? Call(string id, string text)
    {
        var reader = new DecimalPopulation.LiteralReader(text, 0);

        bool Constructor(out string locale, out OptionsBag bag)
        {
            locale = string.Empty;
            bag = OptionsBag.Empty;
            if (reader.Value() is not string given)
                return false;
            locale = given;
            if (reader.Take(","))
            {
                if (reader.Value() is not OptionsBag options)
                    return false;
                bag = options;
            }

            return reader.Take(")");
        }

        // (x).toLocaleString(locale, bag): a NumberFormat's format.
        if (reader.Take("("))
        {
            var value = reader.Value();
            if (DecimalPopulation.Input(value) is null || !reader.Take(")") || !reader.Take(".toLocaleString(") || !Constructor(out var locale, out var bag))
                return null;
            return reader.AtEnd ? new TextCall(id, "NumberFormat", locale, bag, "format", [value]) : null;
        }

        foreach (var service in new[] { "NumberFormat", "ListFormat", "RelativeTimeFormat", "DisplayNames" })
        {
            if (!reader.Take("new Intl." + service + "("))
                continue;
            if (!Constructor(out var locale, out var bag))
                return null;

            string? method = null;
            foreach (var name in new[] { "formatRangeToParts", "formatRange", "formatToParts", "format", "of" })
            {
                if (reader.Take("." + name + "("))
                {
                    method = name;
                    break;
                }
            }

            if (method is null)
                return null;

            var arguments = new List<object?>();
            if (!reader.Take(")"))
            {
                while (true)
                {
                    var argument = reader.Value();
                    if (argument is null)
                        return null;
                    arguments.Add(argument);
                    if (reader.Take(")"))
                        break;
                    if (!reader.Take(","))
                        return null;
                }
            }

            return reader.AtEnd || reader.Take(".length") ? new TextCall(id, service, locale, bag, method, arguments) : null;
        }

        return null;
    }
}
