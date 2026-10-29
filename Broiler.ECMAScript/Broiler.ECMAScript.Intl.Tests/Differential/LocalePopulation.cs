using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Broiler.ECMAScript.Conformance;

namespace Broiler.ECMAScript.Intl.Tests.Differential;

/// <summary>An <c>Intl.Locale</c> constructor's options as a script gives them: each a String, or a Boolean for <c>numeric</c>, or absent.</summary>
internal sealed record LocaleOptionsInput
{
    internal string? Language { get; init; }

    internal string? Script { get; init; }

    internal string? Region { get; init; }

    internal string? Variants { get; init; }

    internal string? Calendar { get; init; }

    internal string? Collation { get; init; }

    internal string? FirstDayOfWeek { get; init; }

    internal string? HourCycle { get; init; }

    internal string? CaseFirst { get; init; }

    internal bool? Numeric { get; init; }

    internal string? NumberingSystem { get; init; }

    /// <summary>The options in the order the constructor reads them, as (property, String or Boolean).</summary>
    internal IEnumerable<(string Property, object Value)> Present()
    {
        if (Language is not null) yield return ("language", Language);
        if (Script is not null) yield return ("script", Script);
        if (Region is not null) yield return ("region", Region);
        if (Variants is not null) yield return ("variants", Variants);
        if (Calendar is not null) yield return ("calendar", Calendar);
        if (Collation is not null) yield return ("collation", Collation);
        if (FirstDayOfWeek is not null) yield return ("firstDayOfWeek", FirstDayOfWeek);
        if (HourCycle is not null) yield return ("hourCycle", HourCycle);
        if (CaseFirst is not null) yield return ("caseFirst", CaseFirst);
        if (Numeric is { } numeric) yield return ("numeric", numeric);
        if (NumberingSystem is not null) yield return ("numberingSystem", NumberingSystem);
    }

    /// <summary>The options as a script writes them, for a difference's report.</summary>
    public override string ToString() =>
        "{" + string.Join(",", Present().Select(static p => p.Property + ":" + (p.Value is bool b ? (b ? "true" : "false") : "\"" + p.Value + "\""))) + "}";
}

/// <summary>A service's ResolveLocale: which service, the locales argument's elements, and the options its constructor hands over.</summary>
/// <param name="Service"><c>Collator</c>, <c>NumberFormat</c>, <c>DateTimeFormat</c> or <c>plain</c> (a service with no relevant key).</param>
/// <param name="Requested">The locales argument's String elements, before CanonicalizeLocaleList.</param>
/// <param name="Options">The options by key, as the constructor read and checked them; <see cref="Vm.JsRealm.NullOption"/> for an option read as <c>null</c>.</param>
internal sealed record Resolution(string Service, IReadOnlyList<string> Requested, IReadOnlyDictionary<string, string?> Options);

/// <summary>
/// Sub-area (a)'s population (Broiler.VM's JSD-0082, decision D22): every String input of every Intl vector, every dataset
/// program's String literals and the locales dataset's cases with their options, each table's keys made into tags, and
/// seeded random tags, options, locale lists and resolutions, so many per unit of scale.
/// </summary>
internal sealed class LocalePopulation
{
    private const string Alpha = "abcdefghijklmnopqrstuvwxyz";

    private const string Digits = "0123456789";

    private const string Alphanumeric = Alpha + Digits;

    /// <summary>Characters an ASCII-only reading must refuse and a culture's casing would fold or accept: U+212A KELVIN SIGN, U+0130, U+0131, U+017F, a fullwidth letter, an Arabic-Indic digit, a line feed and a NUL (rows L2, L14 and L45).</summary>
    private static readonly string[] NonAscii = ["K", "İ", "ı", "ſ", "ｅ", "١", "\n", "\0"];

    private static readonly string[] FixedVariants = ["1901", "1994", "1996", "fonipa", "rozaj", "biske", "valencia", "preeuro", "heploc", "hepburn", "saaho", "aaland", "arevmda", "polyton", "pinyin", "posix"];

    private static readonly string[] Weekdays = ["0", "1", "2", "3", "4", "5", "6", "7", "8", "mon", "MON", "tue", "wed", "thu", "fri", "sat", "sun", "monday", "xyz", "ab", "moK"];

    private static readonly string[] HourCycles = ["h11", "h12", "h23", "h24", "H23", "h25", ""];

    private static readonly string[] CaseFirsts = ["upper", "lower", "false", "FALSE", "true", "x"];

    private readonly List<string> tags = [];

    private readonly HashSet<string> seen = new(StringComparer.Ordinal);

    private LocalePopulation()
    {
    }

    /// <summary>Every tag, once, in the order first given.</summary>
    internal IReadOnlyList<string> Tags => tags;

    /// <summary>The <c>Intl.Locale</c> constructor's inputs: a tag and its options, or none.</summary>
    internal List<(string Tag, LocaleOptionsInput? Options)> Locales { get; } = [];

    /// <summary>CanonicalizeLocaleList's inputs: the locales argument's String elements.</summary>
    internal List<IReadOnlyList<string>> Lists { get; } = [];

    /// <summary>ResolveLocale's inputs.</summary>
    internal List<Resolution> Resolutions { get; } = [];

    /// <summary><c>Intl.supportedValuesOf</c>'s keys.</summary>
    internal List<string> SupportedKeys { get; } = [];

    /// <summary>The population of one embedding: its tables give the pieces random tags are made of.</summary>
    internal static LocalePopulation Build(Embedding embedding, SeededRandom random, int scale)
    {
        var population = new LocalePopulation();
        var pieces = new Pieces(embedding);

        population.AddVectors();
        population.AddDatasets();
        population.AddTables(pieces);

        for (var i = 0; i < 2000 * scale; i++)
            population.Add(pieces.RandomTag(random));

        var pool = population.tags.ToList();

        foreach (var tag in pool)
            population.Locales.Add((tag, null));

        for (var i = 0; i < 1000 * scale; i++)
            population.Locales.Add((random.Next(4) == 0 ? pieces.RandomTag(random) : random.Pick(pool), pieces.RandomOptions(random)));

        for (var i = 0; i < 1000 * scale; i++)
        {
            var list = new List<string>();
            for (var n = random.Next(5); n > 0; n--)
                list.Add(random.Next(3) == 0 ? pieces.NegotiationTag(random) : random.Pick(pool));
            if (list.Count > 0 && random.Next(4) == 0)
                list.Add(random.Next(2) == 0 ? list[0] : list[0].ToUpperInvariant());
            population.Lists.Add(list);
        }

        foreach (var service in new[] { "Collator", "NumberFormat", "DateTimeFormat", "plain" })
        {
            for (var i = 0; i < 500 * scale; i++)
            {
                var requested = new List<string>();
                for (var n = random.Next(4); n > 0; n--)
                    requested.Add(random.Next(5) == 0 ? random.Pick(pool) : pieces.NegotiationTag(random));
                population.Resolutions.Add(new Resolution(service, requested, pieces.RandomServiceOptions(random, service)));
            }
        }

        population.SupportedKeys.AddRange(["calendar", "collation", "currency", "numberingSystem", "timeZone", "unit"]);
        foreach (var key in new[] { "Calendar", "CALENDAR", "calendars", "timezone", "units", "", "language", "region", "script", "hourCycle", "numberingsystem", "collations" })
        {
            if (!population.SupportedKeys.Contains(key))
                population.SupportedKeys.Add(key);
        }

        for (var i = 0; i < 20 * scale; i++)
            population.SupportedKeys.Add(random.NextString(Alpha + "ABCDEFGHIJKLMNOPQRSTUVWXYZ", 0, 16));

        return population;
    }

    private void Add(string tag)
    {
        if (seen.Add(tag))
            tags.Add(tag);
    }

    /// <summary>Every Intl vector: each String input as a tag, each vector's String inputs as one list, and sub-area (a)'s options and keys as its operation reads them.</summary>
    private void AddVectors()
    {
        foreach (var file in new[] { "locales.vectors", "numbers-and-text.vectors", "date-times.vectors", "durations.vectors", "values-and-plurals.vectors" })
        {
            foreach (var vector in Population.IntlVectors(file))
            {
                var inputs = LibraryRun.Inputs(vector);
                var strings = inputs.Where(static i => i.Kind == TypedValueKind.String).Select(static i => i.Chars).ToList();
                foreach (var text in strings)
                    Add(text);
                Lists.Add(strings);

                if (file != "locales.vectors" || strings.Count == 0)
                    continue;

                var op = vector["op"] ?? string.Empty;
                var tag = strings[0];
                var option = strings.Count > 1 ? strings[1] : null;

                switch (op)
                {
                    case "Intl.Locale (language option)": Locales.Add((tag, new LocaleOptionsInput { Language = option })); break;
                    case "Intl.Locale (region option)": Locales.Add((tag, new LocaleOptionsInput { Region = option })); break;
                    case "Intl.Locale (variants option)": Locales.Add((tag, new LocaleOptionsInput { Variants = option })); break;
                    case "Intl.Locale (calendar option)": Locales.Add((tag, new LocaleOptionsInput { Calendar = option })); break;
                    case "Intl.Locale (collation option)": Locales.Add((tag, new LocaleOptionsInput { Collation = option })); break;
                    case "Intl.Locale (firstDayOfWeek option)": Locales.Add((tag, new LocaleOptionsInput { FirstDayOfWeek = option })); break;
                    case "Intl.Locale (numberingSystem option)": Locales.Add((tag, new LocaleOptionsInput { NumberingSystem = option })); break;
                    case "Intl.Collator, resolvedOptions()" when option is not null && LocaleId.IsUnicodeType(option):
                        Resolutions.Add(new Resolution("Collator", [tag], new Dictionary<string, string?> { ["co"] = option }));
                        break;
                    case "Intl.NumberFormat (numberingSystem option)" when option is not null && LocaleId.IsUnicodeType(option):
                        Resolutions.Add(new Resolution("NumberFormat", [tag], new Dictionary<string, string?> { ["nu"] = option }));
                        break;
                    case "Intl.supportedValuesOf":
                        SupportedKeys.Add(tag);
                        break;
                    case var _ when op.StartsWith("Intl.Collator", StringComparison.Ordinal):
                        Resolutions.Add(new Resolution("Collator", strings, new Dictionary<string, string?>()));
                        break;
                    case var _ when op.StartsWith("Intl.NumberFormat", StringComparison.Ordinal) || op.StartsWith("Intl.RelativeTimeFormat", StringComparison.Ordinal) || op.StartsWith("Intl.DurationFormat", StringComparison.Ordinal):
                        Resolutions.Add(new Resolution("NumberFormat", strings, new Dictionary<string, string?>()));
                        break;
                    case var _ when op.StartsWith("Intl.DateTimeFormat", StringComparison.Ordinal):
                        Resolutions.Add(new Resolution("DateTimeFormat", strings, new Dictionary<string, string?>()));
                        break;
                    case var _ when op.StartsWith("Intl.PluralRules", StringComparison.Ordinal) || op.StartsWith("Intl.ListFormat", StringComparison.Ordinal)
                        || op.StartsWith("Intl.DisplayNames", StringComparison.Ordinal) || op.StartsWith("Intl.Segmenter", StringComparison.Ordinal):
                        Resolutions.Add(new Resolution("plain", strings, new Dictionary<string, string?>()));
                        break;
                }
            }
        }
    }

    /// <summary>Every dataset program's String literals as tags, and the locales dataset's cases with their options.</summary>
    private void AddDatasets()
    {
        var literal = new Regex("'((?:[^'\\\\\\n]|\\\\.)*)'|\"((?:[^\"\\\\\\n]|\\\\.)*)\"", RegexOptions.CultureInvariant);
        var replaced = new Regex("'([^']*)'\\.replace\\('([^']*)',\\s*'([^']*)'\\)", RegexOptions.CultureInvariant);
        var option = new Regex("(\\w+):\\s*(?:'([^']*)'|(true|false))", RegexOptions.CultureInvariant);

        foreach (var (name, program) in Population.DatasetPrograms())
        {
            var programTags = new List<string>();
            foreach (Match match in literal.Matches(program))
            {
                var text = Unescape(match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value);
                Add(text);
                programTags.Add(text);
            }

            Lists.Add(programTags.Where(static t => t.Length is > 0 and < 40).Distinct(StringComparer.Ordinal).Take(8).ToList());

            if (name != "locales")
                continue;

            // THE LOCALES DATASET'S CASES: [tag] or [tag, {options}], the tag a literal or 'a'.replace('b', 'c').
            var start = program.IndexOf("var cases = [", StringComparison.Ordinal);
            var end = program.IndexOf("];", start, StringComparison.Ordinal);
            var cases = program[(start + "var cases = [".Length)..end];

            foreach (Match match in new Regex("\\[(?<tag>'[^']*'(?:\\.replace\\('[^']*',\\s*'[^']*'\\))?)(?:\\s*,\\s*\\{(?<options>[^}]*)\\})?\\]", RegexOptions.CultureInvariant).Matches(cases))
            {
                var source = match.Groups["tag"].Value;
                var tag = replaced.Match(source) is { Success: true } r
                    ? ReplaceFirst(r.Groups[1].Value, r.Groups[2].Value, r.Groups[3].Value)
                    : source[1..^1];
                Add(tag);

                var options = new LocaleOptionsInput();
                foreach (Match pair in option.Matches(match.Groups["options"].Value))
                {
                    var value = pair.Groups[2].Success ? pair.Groups[2].Value : null;
                    options = pair.Groups[1].Value switch
                    {
                        "language" => options with { Language = value },
                        "script" => options with { Script = value },
                        "region" => options with { Region = value },
                        "variants" => options with { Variants = value },
                        "calendar" => options with { Calendar = value },
                        "collation" => options with { Collation = value },
                        "firstDayOfWeek" => options with { FirstDayOfWeek = value },
                        "hourCycle" => options with { HourCycle = value },
                        "caseFirst" => options with { CaseFirst = value },
                        "numeric" => options with { Numeric = pair.Groups[3].Value == "true" },
                        "numberingSystem" => options with { NumberingSystem = value },
                        _ => throw new InvalidOperationException("the locales dataset gives an option this population does not read: " + pair.Groups[1].Value),
                    };
                }

                Locales.Add((tag, match.Groups["options"].Success ? options : null));
            }
        }
    }

    /// <summary>Each table's keys as tags: likely subtags' sources, every alias's source in a tag of its kind, and every BCP 47 type in a <c>-u-</c> or <c>-t-</c> extension.</summary>
    private void AddTables(Pieces pieces)
    {
        foreach (var source in pieces.LikelySources)
            Add(source.Replace('_', '-'));

        foreach (var (kind, source) in pieces.AliasSources)
        {
            switch (kind)
            {
                case "language": Add(source); Add(source + "-US"); break;
                case "script": Add("und-" + source); Add("en-" + source); Add("und-" + source + "-US"); break;
                case "region": Add("und-" + source); Add("en-" + source); Add("und-Cyrl-" + source); break;
                case "variant": Add("en-" + source); Add("und-" + source); break;
                case "subdivision": Add("en-u-sd-" + source); Add("en-u-rg-" + source); break;
            }
        }

        foreach (var (singleton, key, type) in pieces.ExtensionTypes)
            Add(singleton == "u" ? "en-u-" + key + (type.Length == 0 ? string.Empty : "-" + type) : "en-t-" + key + "-" + type);
    }

    private static string ReplaceFirst(string text, string search, string replacement)
    {
        var at = text.IndexOf(search, StringComparison.Ordinal);
        return at < 0 ? text : text[..at] + replacement + text[(at + search.Length)..];
    }

    private static string Unescape(string text)
    {
        if (!text.Contains('\\', StringComparison.Ordinal))
            return text;

        var result = new StringBuilder(text.Length);
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] != '\\' || i + 1 == text.Length)
            {
                result.Append(text[i]);
                continue;
            }

            var next = text[++i];
            switch (next)
            {
                case 'n': result.Append('\n'); break;
                case 't': result.Append('\t'); break;
                case 'u' when i + 4 < text.Length && int.TryParse(text.AsSpan(i + 1, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var unit):
                    result.Append((char)unit);
                    i += 4;
                    break;
                default: result.Append(next); break;
            }
        }

        return result.ToString();
    }

    /// <summary>The pieces random tags are made of, from one embedding's tables.</summary>
    private sealed class Pieces
    {
        internal Pieces(Embedding embedding)
        {
            var likely = embedding.Lines(IntlTable.LikelySubtags).Select(static l => l.Split('|')).ToList();
            LikelySources = likely.Select(static f => f[0]).ToList();

            var languages = new SortedSet<string>(StringComparer.Ordinal) { "und", "en", "de", "fr", "ar", "zh", "sr", "ja" };
            var scripts = new SortedSet<string>(StringComparer.Ordinal) { "Zzzz", "Qaai", "Qaaa", "Zyyy", "Latf" };
            var regions = new SortedSet<string>(StringComparer.Ordinal) { "ZZ", "001", "150", "419", "AQ" };
            foreach (var fields in likely)
            {
                var target = fields[1].Split('-');
                languages.Add(target[0]);
                if (target.Length > 1) scripts.Add(target[1]);
                if (target.Length > 2) regions.Add(target[2]);
            }

            AliasSources = embedding.Lines(IntlTable.Aliases).Select(static l => l.Split('|')).Select(static f => (f[0], f[1])).ToList();
            foreach (var (kind, source) in AliasSources)
            {
                if (kind == "region") regions.Add(source);
                if (kind == "script") scripts.Add(source);
            }

            foreach (var line in embedding.Lines(IntlTable.Scripts))
                scripts.Add(line[..line.IndexOf('|')]);

            Languages = [.. languages];
            Scripts = [.. scripts];
            Regions = [.. regions];
            AliasLanguages = AliasSources.Where(static a => a.Kind == "language").Select(static a => a.Source).ToList();
            Variants = [.. FixedVariants, .. AliasSources.Where(static a => a.Kind == "variant").Select(static a => a.Source)];
            Subdivisions = [.. AliasSources.Where(static a => a.Kind == "subdivision").Select(static a => a.Source), "usca", "gbsct", "gbzzzz", "axzzzz", "sazzzz", "twzzzz", "uszzzz", "frgp"];

            ExtensionTypes = embedding.Lines(IntlTable.Extensions).Select(static l => l.Split('|')).Select(static f => (f[0], f[1], f[2])).ToList();
            foreach (var (singleton, key, type) in ExtensionTypes)
            {
                var keys = singleton == "u" ? UTypes : TTypes;
                if (!keys.TryGetValue(key, out var types))
                    keys[key] = types = [];
                if (type.Length != 0)
                    types.Add(type);
            }

            UKeys = [.. UTypes.Keys.Order(StringComparer.Ordinal)];
            TKeys = [.. TTypes.Keys.Order(StringComparer.Ordinal)];
            Available = embedding.Lines(IntlTable.Locales).Select(static l => l.Split('|')[0]).Where(static l => l != "und").ToList();
        }

        internal List<string> LikelySources { get; }

        internal List<(string Kind, string Source)> AliasSources { get; }

        internal List<(string Singleton, string Key, string Type)> ExtensionTypes { get; }

        private List<string> Languages { get; }

        private List<string> Scripts { get; }

        private List<string> Regions { get; }

        private List<string> AliasLanguages { get; }

        private List<string> Variants { get; }

        private List<string> Subdivisions { get; }

        private Dictionary<string, List<string>> UTypes { get; } = new(StringComparer.Ordinal);

        private Dictionary<string, List<string>> TTypes { get; } = new(StringComparer.Ordinal);

        private List<string> UKeys { get; }

        private List<string> TKeys { get; }

        private List<string> Available { get; }

        /// <summary>A random tag: a language, script, region and variants from the tables or made up, and extensions, in random case, sometimes malformed.</summary>
        internal string RandomTag(SeededRandom random)
        {
            var parts = new List<string>
            {
                random.Next(12) switch
                {
                    0 => "und",
                    1 => random.NextString(Alpha, 2, 8),
                    2 or 3 => random.Pick(AliasLanguages),
                    _ => random.Pick(Languages),
                },
            };

            if (random.Next(4) == 0)
                parts.Add(random.Next(4) == 0 ? random.NextString(Alpha, 4, 4) : random.Pick(Scripts));
            if (random.Next(2) == 0)
                parts.Add(random.Next(5) == 0 ? (random.Next(2) == 0 ? random.NextString(Alpha, 2, 2) : random.NextString(Digits, 3, 3)) : random.Pick(Regions));
            for (var n = random.Next(6) == 0 ? 1 + random.Next(2) : 0; n > 0; n--)
                parts.Add(random.Next(3) == 0 ? random.NextString(Digits, 1, 1) + random.NextString(Alphanumeric, 3, 3) : random.Pick(Variants));

            if (random.Next(3) == 0)
                parts.Add(UnicodeExtension(random));
            if (random.Next(8) == 0)
                parts.Add(TransformExtension(random));
            if (random.Next(12) == 0)
                parts.Add(random.Pick("abcdefghijklmnopqrsvwyz".Select(static c => c.ToString()).ToList()) + "-" + random.NextString(Alphanumeric, 2, 8));
            if (random.Next(10) == 0)
                parts.Add("x-" + random.NextString(Alphanumeric, 1, 8));

            var tag = RandomCase(random, string.Join("-", parts));
            return random.Next(10) == 0 ? Malform(random, tag) : tag;
        }

        /// <summary>A tag a resolution may match: an available locale, with a script, region, variant, keywords or private use added.</summary>
        internal string NegotiationTag(SeededRandom random)
        {
            var tag = new StringBuilder(random.Next(8) == 0 ? random.Pick(Languages) : random.Pick(Available));
            if (random.Next(6) == 0)
                tag.Append('-').Append(random.Pick(Regions));
            if (random.Next(6) == 0)
                tag.Append('-').Append(random.Pick(Variants));
            if (random.Next(2) == 0)
                tag.Append('-').Append(UnicodeExtension(random, ["ca", "co", "hc", "kf", "kn", "nu", "nu", "co"]));
            if (random.Next(8) == 0)
                tag.Append("-x-").Append(random.NextString(Alphanumeric, 1, 8));
            return random.Next(6) == 0 ? RandomCase(random, tag.ToString()) : tag.ToString();
        }

        /// <summary>The Locale constructor's options: each present at random, from its values, made up, or not ASCII.</summary>
        internal LocaleOptionsInput RandomOptions(SeededRandom random)
        {
            string? Maybe(Func<string> value) => random.Next(5) == 0 ? value() : null;
            string Odd(string value) => random.Next(8) == 0 ? value + random.Pick(NonAscii) : random.Next(6) == 0 ? value.ToUpperInvariant() : value;

            return new LocaleOptionsInput
            {
                Language = Maybe(() => Odd(random.Next(4) == 0 ? random.NextString(Alpha, 1, 9) : random.Pick(Languages))),
                Script = Maybe(() => Odd(random.Next(4) == 0 ? random.NextString(Alpha, 3, 5) : random.Pick(Scripts))),
                Region = Maybe(() => Odd(random.Next(4) == 0 ? random.NextString(Alphanumeric, 1, 4) : random.Pick(Regions))),
                Variants = Maybe(() => Odd(random.Next(5) == 0 ? string.Empty : string.Join("-", Enumerable.Range(0, 1 + random.Next(3)).Select(_ => random.Pick(Variants))))),
                Calendar = Maybe(() => Odd(Type(random, "ca"))),
                Collation = Maybe(() => Odd(Type(random, "co"))),
                FirstDayOfWeek = Maybe(() => random.Pick(Weekdays)),
                HourCycle = Maybe(() => random.Pick(HourCycles)),
                CaseFirst = Maybe(() => random.Pick(CaseFirsts)),
                Numeric = random.Next(5) == 0 ? random.Next(2) == 0 : null,
                NumberingSystem = Maybe(() => Odd(Type(random, "nu"))),
            };
        }

        /// <summary>
        /// A service's options as its constructor hands them to ResolveLocale: checked as a <c>type</c> or against GetOption's
        /// values first, so ASCII (row L42), in any case, and <c>hc</c> sometimes read as <c>null</c>.
        /// </summary>
        internal Dictionary<string, string?> RandomServiceOptions(SeededRandom random, string service)
        {
            var options = new Dictionary<string, string?>(StringComparer.Ordinal);
            string Cased(string value) => random.Next(4) == 0 ? value.ToUpperInvariant() : value;

            switch (service)
            {
                case "Collator":
                    if (random.Next(3) == 0) options["co"] = Cased(Type(random, "co"));
                    if (random.Next(3) == 0) options["kn"] = random.Next(2) == 0 ? "true" : "false";
                    if (random.Next(3) == 0) options["kf"] = random.Pick(new[] { "upper", "lower", "false" });
                    break;
                case "NumberFormat":
                    if (random.Next(2) == 0) options["nu"] = Cased(Type(random, "nu"));
                    break;
                case "DateTimeFormat":
                    if (random.Next(3) == 0) options["ca"] = Cased(Type(random, "ca"));
                    if (random.Next(3) == 0) options["hc"] = random.Next(3) == 0 ? Vm.JsRealm.NullOption : random.Pick(new[] { "h11", "h12", "h23", "h24" });
                    if (random.Next(3) == 0) options["nu"] = Cased(Type(random, "nu"));
                    break;
            }

            return options;
        }

        /// <summary>A <c>type</c> for a <c>-u-</c> key: one of the table's, a made-up one, or two subtags.</summary>
        private string Type(SeededRandom random, string key) =>
            random.Next(5) switch
            {
                0 => random.NextString(Alphanumeric, 3, 8),
                1 => random.NextString(Alphanumeric, 3, 8) + "-" + random.NextString(Alphanumeric, 3, 8),
                _ => UTypes.TryGetValue(key, out var types) && types.Count > 0 ? random.Pick(types) : random.NextString(Alphanumeric, 3, 8),
            };

        private string UnicodeExtension(SeededRandom random, IReadOnlyList<string>? keys = null)
        {
            var parts = new List<string> { "u" };
            for (var n = keys is null && random.Next(5) == 0 ? 1 + random.Next(2) : 0; n > 0; n--)
                parts.Add(random.NextString(Alphanumeric, 3, 8));

            for (var n = 1 + random.Next(3); n > 0; n--)
            {
                var key = random.Next(10) == 0 ? random.NextString(Alpha, 1, 1) + random.NextString(Alphanumeric, 1, 1) : random.Pick(keys ?? UKeys);
                parts.Add(key);

                if (key is "sd" or "rg")
                {
                    parts.Add(random.Next(5) == 0 ? random.NextString(Alpha, 2, 2) + "zzzz" : random.Pick(Subdivisions));
                    continue;
                }

                switch (random.Next(10))
                {
                    case 0: parts.Add("true"); break;
                    case 1: break;
                    case 2: parts.Add(random.NextString(Alphanumeric, 3, 8)); break;
                    default:
                        if (UTypes.TryGetValue(key, out var types) && types.Count > 0)
                            parts.Add(random.Pick(types));
                        break;
                }
            }

            return string.Join("-", parts);
        }

        private string TransformExtension(SeededRandom random)
        {
            var parts = new List<string> { "t" };
            if (random.Next(3) != 0)
            {
                parts.Add(random.Next(3) == 0 ? random.Pick(AliasLanguages) : random.Pick(Languages));
                if (random.Next(3) == 0) parts.Add(random.Pick(Regions));
            }

            for (var n = parts.Count == 1 ? 1 + random.Next(2) : random.Next(2); n > 0; n--)
            {
                var key = random.Pick(TKeys);
                parts.Add(key);
                parts.Add(TTypes[key].Count > 0 && random.Next(5) != 0 ? random.Pick(TTypes[key]) : random.NextString(Alphanumeric, 3, 8));
            }

            return string.Join("-", parts);
        }

        private static string RandomCase(SeededRandom random, string text)
        {
            if (random.Next(3) != 0)
                return text;

            var chars = text.ToCharArray();
            for (var i = 0; i < chars.Length; i++)
            {
                if (random.Next(3) == 0)
                    chars[i] = chars[i] is >= 'a' and <= 'z' ? (char)(chars[i] - 32) : chars[i] is >= 'A' and <= 'Z' ? (char)(chars[i] + 32) : chars[i];
            }

            return new string(chars);
        }

        /// <summary>A tag made ill-formed one way: an empty subtag, a separator at an end, an underscore, a character outside ASCII, a long subtag, a repeated subtag or an empty extension.</summary>
        private static string Malform(SeededRandom random, string tag)
        {
            var at = random.Next(tag.Length + 1);
            var last = tag[(tag.LastIndexOf('-') + 1)..];

            return random.Next(9) switch
            {
                0 => tag[..at] + "--" + tag[at..],
                1 => tag + "-",
                2 => "-" + tag,
                3 => tag.Replace('-', '_'),
                4 => tag[..at] + random.Pick(NonAscii) + tag[at..],
                5 => tag + "-" + random.NextString(Alphanumeric, 9, 9),
                6 => tag + "-" + last,
                7 => tag + "-u",
                _ => tag + "-x",
            };
        }
    }
}
