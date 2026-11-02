using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Broiler.ECMAScript.Temporal;

namespace Broiler.ECMAScript.Intl.Tests.Differential;

/// <summary>One duration format a vector's script constructs, and the durations its script formats with it.</summary>
/// <param name="Id">The vector.</param>
/// <param name="Locale">The constructor's locale.</param>
/// <param name="Bag">The constructor's options.</param>
/// <param name="Durations">The valid duration records the script formats.</param>
internal sealed record DurationCall(string Id, string Locale, OptionsBag Bag, IReadOnlyList<DurationFields> Durations);

/// <summary>
/// The population of sub-area (e)'s sections (Broiler.VM's JSD-0082, decision D22): every duration format and duration of the
/// <c>durations</c> dataset program, as library calls, its error group's formats included; each <c>durations.vectors</c> vector's
/// own format and the durations it formats (a property bag of literals, a Temporal duration of one, or an ISO 8601 duration
/// String); and seeded random formats over the embedding's locales and random valid durations, so many per unit of scale.
/// </summary>
/// <remarks>
/// A duration is the record the engine's ToTemporalDuration makes, which the library takes: only a valid one is a duration of the
/// population, and the argument's refusals are the engine's (decision D24, row U22).
/// </remarks>
internal sealed partial class DurationPopulation
{
    private static readonly string[] Units = ["years", "months", "weeks", "days", "hours", "minutes", "seconds", "milliseconds", "microseconds", "nanoseconds"];

    private static readonly string[][] UnitStyles =
    [
        ["long", "short", "narrow"], ["long", "short", "narrow"], ["long", "short", "narrow"], ["long", "short", "narrow"],
        ["long", "short", "narrow", "numeric", "2-digit"], ["long", "short", "narrow", "numeric", "2-digit"], ["long", "short", "narrow", "numeric", "2-digit"],
        ["long", "short", "narrow", "numeric"], ["long", "short", "narrow", "numeric"], ["long", "short", "narrow", "numeric"],
    ];

    /// <summary>The dataset program's formats, each with its locale: every locale with every format, then the error group's.</summary>
    internal List<(string Locale, OptionsBag Bag)> DatasetFormats { get; } = [];

    /// <summary>The dataset program's valid durations, which the section formats with every dataset format.</summary>
    internal List<DurationFields> DatasetDurations { get; } = [];

    /// <summary>Each vector's own format and durations.</summary>
    internal List<DurationCall> VectorCalls { get; } = [];

    /// <summary>Every format: the dataset's, the vectors' and the random ones.</summary>
    internal List<(string Locale, OptionsBag Bag)> Formats { get; } = [];

    /// <summary>The random formats the random durations are formatted with.</summary>
    internal List<(string Locale, OptionsBag Bag)> RandomFormats { get; } = [];

    internal List<DurationFields> RandomDurations { get; } = [];

    internal List<string> Locales { get; } = [];

    internal static DurationPopulation Build(Embedding embedding, SeededRandom random, int scale)
    {
        var population = new DurationPopulation();

        // EVERY DATASET INPUT: the locales with the formats, the durations, and the error group's formats and arguments.
        foreach (var (name, program) in Population.DatasetPrograms().Where(static p => p.Name == "durations"))
        {
            var locales = DecimalPopulation.Array(program, "locales").OfType<string>().ToList();
            var formats = DecimalPopulation.Array(program, "formats").OfType<OptionsBag>().ToList();
            foreach (var locale in locales)
            {
                foreach (var bag in formats)
                    population.DatasetFormats.Add((locale, bag));
            }

            foreach (var item in DecimalPopulation.Array(program, "durations"))
            {
                if (item is OptionsBag bag && Record(bag) is { } duration)
                    population.DatasetDurations.Add(duration);
            }

            foreach (Match match in DatasetConstructor().Matches(program))
            {
                var bag = match.Groups[2].Success ? new DecimalPopulation.LiteralReader(match.Groups[2].Value, 0).Value() as OptionsBag : OptionsBag.Empty;
                if (bag is not null)
                    population.DatasetFormats.Add((match.Groups[1].Value, bag));
            }

            foreach (Match match in DatasetArgument().Matches(program))
            {
                var duration = match.Groups[1].Success
                    ? new DecimalPopulation.LiteralReader(match.Groups[1].Value, 0).Value() is OptionsBag bag ? Record(bag) : null
                    : Parsed(match.Groups[2].Value);
                if (duration is { } valid)
                    population.DatasetDurations.Add(valid);
            }
        }

        // EVERY VECTOR'S OWN FORMAT and the durations its script formats.
        foreach (var vector in Population.IntlVectors("durations.vectors"))
        {
            var script = vector.Script;
            var format = VectorConstructor().Match(script);
            var locale = format.Success ? format.Groups[1].Value : null;
            var bagText = format.Success && format.Groups[2].Success ? format.Groups[2].Value : null;
            if (!format.Success && ToLocaleString().Match(script) is { Success: true } toLocale)
            {
                locale = toLocale.Groups[1].Value;
                bagText = toLocale.Groups[2].Value;
            }

            if (locale is null)
                continue;
            var parsedBag = bagText is null ? OptionsBag.Empty : new DecimalPopulation.LiteralReader(bagText, 0).Value() as OptionsBag;
            if (parsedBag is null)
                continue;

            var durations = new List<DurationFields>();
            foreach (Match argument in VectorArgument().Matches(script))
            {
                var duration = argument.Groups[1].Success
                    ? new DecimalPopulation.LiteralReader(argument.Groups[1].Value, 0).Value() is OptionsBag bag ? Record(bag) : null
                    : Parsed(argument.Groups[2].Value);
                if (duration is { } valid)
                    durations.Add(valid);
            }

            population.VectorCalls.Add(new DurationCall(vector["id"] ?? string.Empty, locale, parsedBag, durations));
            population.Locales.Add(locale);
        }

        // THE EMBEDDING'S OWN LOCALES, then the seeded random formats and durations.
        population.Locales.AddRange(embedding.Lines(IntlTable.Locales).Select(static l => l.Split('|')[0]));
        population.Locales.AddRange(population.DatasetFormats.Select(static f => f.Locale));
        var tags = population.Locales.Distinct(StringComparer.Ordinal).ToList();
        for (var i = 0; i < 150 * scale; i++)
            population.RandomFormats.Add((random.Pick(tags) + (random.Next(12) == 0 ? "-u-nu-" + random.Pick(new[] { "arab", "latn", "thai", "deva" }) : string.Empty), RandomBag(random)));
        for (var i = 0; i < 400 * scale; i++)
            population.RandomDurations.Add(RandomDuration(random));

        population.Formats.AddRange(population.DatasetFormats);
        population.Formats.AddRange(population.VectorCalls.Select(static c => (c.Locale, c.Bag)));
        population.Formats.AddRange(population.RandomFormats);
        Distinct(population.Formats, static f => f.Locale + " " + f.Bag.Label);
        Distinct(population.Locales, static l => l);
        return population;
    }

    /// <summary>
    /// The record ToTemporalDuration makes of a property bag of the population: each field a Number, a String read as one, or
    /// absent; <see langword="null"/> where it refuses the bag (no field, a field that is not an integral Number, an invalid
    /// duration).
    /// </summary>
    internal static DurationFields? Record(OptionsBag bag)
    {
        var fields = new double[10];
        var any = false;
        for (var i = 0; i < Units.Length; i++)
        {
            if (bag[Units[i]] is not { } value)
                continue;
            var number = value switch
            {
                double d => d,
                string s => double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : double.NaN,
                _ => double.NaN,
            };
            if (!double.IsFinite(number) || Math.Truncate(number) != number)
                return null;
            fields[i] = number + 0.0;
            any = true;
        }

        return any ? Valid(fields) : null;
    }

    /// <summary>An ISO 8601 duration String through Temporal's grammar and limits, or <see langword="null"/> where they refuse it.</summary>
    internal static DurationFields? Parsed(string text) =>
        TemporalGrammar.TryParseDuration(text, out var duration, out _) && TemporalLimits.CheckDuration(duration) == TemporalStatus.Completed ? duration : null;

    private static DurationFields? Valid(double[] f)
    {
        var duration = new DurationFields(f[0], f[1], f[2], f[3], f[4], f[5], f[6], f[7], f[8], f[9]);
        return TemporalLimits.CheckDuration(duration) == TemporalStatus.Completed ? duration : null;
    }

    /// <summary>
    /// A duration format's options bag: a style most of the time; now and then each unit's style from its list (or a style the
    /// unit does not admit) and display, <c>fractionalDigits</c> (in range or not) and a numbering system (known, unknown or
    /// ill-formed).
    /// </summary>
    private static OptionsBag RandomBag(SeededRandom random)
    {
        var entries = new List<KeyValuePair<string, object>>();
        if (random.Next(4) != 0)
            entries.Add(new("style", random.Pick(new[] { "long", "short", "narrow", "digital", "digital" })));
        for (var i = 0; i < Units.Length; i++)
        {
            if (random.Next(7) == 0)
                entries.Add(new(Units[i], random.Next(25) == 0 ? random.Pick(new[] { "numeric", "2-digit", "fractional", "auto" }) : random.Pick(UnitStyles[i])));
            if (random.Next(9) == 0)
                entries.Add(new(Units[i] + "Display", random.Next(30) == 0 ? "never" : random.Pick(new[] { "auto", "always" })));
        }

        if (random.Next(5) == 0)
            entries.Add(new("fractionalDigits", random.Next(15) == 0 ? random.Pick(new[] { 10.0, -1.0, double.NaN, 2.5 }) : (double)random.Next(10)));
        if (random.Next(6) == 0)
            entries.Add(new("numberingSystem", random.Pick(new[] { "arab", "latn", "thai", "deva", "hanidec", "arabext", "abcd", "latn-" })));
        return new OptionsBag(entries);
    }

    /// <summary>
    /// A valid duration of one sign: each field present or zero, small, middling or near its limit, a negative duration's zero
    /// fields now and then <c>-0</c>; the fields drawn again where the whole is not valid.
    /// </summary>
    private static DurationFields RandomDuration(SeededRandom random)
    {
        while (true)
        {
            var negative = random.Next(3) == 0;
            var fields = new double[10];
            var present = random.Next(4) switch { 0 => 1, 1 => 2, 2 => 3, _ => 10 };
            for (var i = 0; i < 10; i++)
            {
                if (random.Next(10) >= present)
                {
                    fields[i] = negative && random.Next(6) == 0 ? -0.0 : 0.0;
                    continue;
                }

                double magnitude = random.Next(8) switch
                {
                    0 => 0,
                    1 or 2 or 3 => random.Next(10),
                    4 or 5 => random.Next(1000),
                    6 => random.Next(1_000_000),
                    _ => i < 3 ? 4294967295 - random.Next(3) : Math.Floor(9007199254740991.0 / Math.Pow(1000, Math.Max(0, i - 6)) / (i switch { 3 => 86400, 4 => 3600, 5 => 60, _ => 1 })) - random.Next(3),
                };
                fields[i] = negative ? -magnitude : magnitude;
            }

            if (Valid(fields) is { } duration)
                return duration;
        }
    }

    private static void Distinct<T>(List<T> list, Func<T, string> key)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        list.RemoveAll(item => !seen.Add(key(item)));
    }

    /// <summary>The dataset's <c>new Intl.DurationFormat('locale')</c> or <c>new Intl.DurationFormat('locale', { ... })</c>.</summary>
    [GeneratedRegex(@"new Intl\.DurationFormat\('([^']*)'(?:, (\{[^{}]*\}))?\)", RegexOptions.CultureInvariant)]
    private static partial Regex DatasetConstructor();

    /// <summary>The dataset's <c>plain.format({ ... })</c> or <c>plain.format('...')</c>.</summary>
    [GeneratedRegex(@"plain\.format\((?:(\{[^{}]*\})|'([^']*)')\)", RegexOptions.CultureInvariant)]
    private static partial Regex DatasetArgument();

    /// <summary>A vector's <c>new Intl.DurationFormat("locale")</c> or <c>new Intl.DurationFormat("locale", { ... })</c>.</summary>
    [GeneratedRegex(@"new Intl\.DurationFormat\(""([^""]*)""(?:, (\{[^{}]*\}))?\)", RegexOptions.CultureInvariant)]
    private static partial Regex VectorConstructor();

    /// <summary>A vector's <c>toLocaleString("locale", { ... })</c> of a Temporal duration.</summary>
    [GeneratedRegex(@"toLocaleString\(""([^""]*)"", (\{[^{}]*\})\)", RegexOptions.CultureInvariant)]
    private static partial Regex ToLocaleString();

    /// <summary>A vector's argument: <c>format({ ... })</c>, <c>formatToParts({ ... })</c>, <c>Duration.from({ ... })</c> or <c>format("...")</c>.</summary>
    [GeneratedRegex(@"(?:\.format|\.formatToParts|Duration\.from)\((?:(\{[^{}]*\})|""([^""]*)"")\)", RegexOptions.CultureInvariant)]
    private static partial Regex VectorArgument();
}
