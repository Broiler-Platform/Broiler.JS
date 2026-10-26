// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   39
// Annotated:        39/39
// Exempt:           0
// Human-reviewed:   0/39
// IP risk:          Low
// Security risk:    High
// Criteria:         3/3
// Resource impact:  8/10 max
// Unverified:       39
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Broiler.ECMAScript.Generation.Time;

namespace Broiler.ECMAScript.Generation.Intl;

/// <summary>
/// Writes the Intl tables - locale identifiers, numbers, plurals, lists, relative times, display names, units,
/// dates, zone names, the regions' hour cycles and weeks, the scripts' directions, and the root collation and
/// its tailorings - from the CLDR files an <see cref="IntlCldrPin"/> names and the UCD files a
/// <see cref="CldrUcd"/> reads, for the locale list and the other parameters an
/// <see cref="IntlGeneratorOptions"/> gives.
/// </summary>
/// <remarks>
/// <para>
/// <b>PORTED from Broiler.VM's <c>CldrTableGenerator</c></b> at Broiler.VM <c>4cf9ca44</c> (decision JSD-0082,
/// decision D11: the generator moves whole, collation included). Each table is the function Broiler.VM's
/// generator wrote, its reads, order, escaping and layout unchanged, so that with Broiler.VM's options and
/// pins it writes Broiler.VM's data byte for byte, which <see cref="IntlTableSource"/> wraps into Broiler.VM's
/// <c>JsCldrTables.g.cs</c>. What changes is what the hosting forbids or parameterizes:
/// </para>
/// <list type="bullet">
/// <item>its hard-coded locale lists (<c>SupportedLocales</c>, <c>NumberLanguages</c>) become the options' locale
/// list (decisions D5 and D11), and the calendars and tailorings options;</item>
/// <item>its regular expressions become hand-written readers of the same patterns, its <c>ToLowerInvariant</c>
/// ASCII folding, its <c>char.IsWhiteSpace</c> the same set written out, and its culture-invariant formatting
/// <see cref="NumberFormatInfo.InvariantInfo"/> (the conventions' rule SL4; decision D19);</item>
/// <item>its static arrays become immutable collections (rule SL2), and it reads no file: the caller hands it
/// the verified bytes (rule SL3);</item>
/// <item>a main-data locale whose CLDR package carries no names file of one kind has no names of that kind
/// (<see cref="IntlTables.AbsentFiles"/>), and with <see cref="IntlNumberingSymbols.EveryNumberingSystem"/>
/// the number table carries every numbering system's symbols (decision D21).</item>
/// </list>
/// <para>
/// <b>The text tables are lines of fields separated by <c>|</c></b>, each character a line cannot carry
/// written <c>\uXXXX</c>, and the collation tables are binary in the layouts Broiler.VM's
/// <c>JsCollationData</c> reads. Nothing here reads a clock, the environment or a file: two runs over the same
/// bytes write the same tables.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 CLDR tables via Broiler.VM CldrTableGenerator; IP=Low; Security=Medium; Resources=8; Fingerprint=AD2E42
// Broiler-Human:        PENDING
public static partial class CldrTableGenerator
{
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 CLDR tables via Broiler.VM CldrTableGenerator; IP=Low; Security=Low; Resources=0; Fingerprint=B294A2
    // Broiler-Human:        PENDING
    private const string Root = "cldr-48.2.0/";

    /// <summary>The per-member exemption Broiler.VM's generated file states, which its options for <see cref="IntlTableSource"/> repeat.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 CLDR tables via Broiler.VM CldrTableGenerator; IP=Low; Security=Low; Resources=0; Fingerprint=16A62D
    // Broiler-Human:        PENDING
    public const string BroilerVmExemptReason =
        "CLDR 48.2.0 table data written by CldrTableGenerator from the files cldr.pin names, compared byte for byte by rule N28";

    /// <summary>
    /// UCA 17.0.0's implicit-weight ranges for the siniform ideographic scripts, which DUCET states in its
    /// <c>@implicitweights</c> lines and <c>allkeys_CLDR.txt</c> leaves out; quoted from
    /// <c>https://www.unicode.org/Public/17.0.0/uca/allkeys.txt</c> by Broiler.VM's generator. The archived
    /// CollationTest files are what hold them to the root collation.
    /// </summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 UCA implicit weights via Broiler.VM CldrTableGenerator; IP=Low; Security=Low; Resources=0; Fingerprint=0D6C34
    // Broiler-Human:        PENDING
    internal static ImmutableArray<(int First, int Last, int Base)> SiniformRanges { get; } = ImmutableArray.Create(
        (0x17000, 0x187FF, 0xFB00),
        (0x18800, 0x18AFF, 0xFB01),
        (0x18D00, 0x18D7F, 0xFB00),
        (0x18D80, 0x18DFF, 0xFB01),
        (0x1B170, 0x1B2FF, 0xFB02),
        (0x18B00, 0x18CFF, 0xFB03));

    /// <summary>ECMA-402's sanctioned single units (table 2 of the pinned edition).</summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sanctioned units via Broiler.VM CldrTableGenerator; IP=Low; Security=Low; Resources=0; Fingerprint=D2132B
    // Broiler-Human:        PENDING
    internal static FrozenSet<string> SanctionedUnits { get; } = FrozenSet.Create(
        StringComparer.Ordinal,
        "acre", "bit", "byte", "celsius", "centimeter", "day", "degree", "fahrenheit", "fluid-ounce", "foot",
        "gallon", "gigabit", "gigabyte", "gram", "hectare", "hour", "inch", "kilobit", "kilobyte", "kilogram",
        "kilometer", "liter", "megabit", "megabyte", "meter", "microsecond", "mile", "mile-scandinavian",
        "milliliter", "millimeter", "millisecond", "minute", "month", "nanosecond", "ounce", "percent", "petabyte",
        "pound", "second", "stone", "terabit", "terabyte", "week", "yard", "year");

    /// <summary>The names files of a locale's display names: its package's file and the kind its lines carry.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 display names via Broiler.VM CldrTableGenerator; IP=Low; Security=Low; Resources=0; Fingerprint=56B879
    // Broiler-Human:        PENDING
    private static ImmutableArray<(string File, string Kind)> NameFiles { get; } =
        ImmutableArray.Create(("languages", "language"), ("territories", "region"), ("scripts", "script"), ("variants", "variant"));

    /// <summary>The date-time field codes of ECMA-402's table 19, and the CLDR date field each names.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 display names via Broiler.VM CldrTableGenerator; IP=Low; Security=Low; Resources=0; Fingerprint=FA6508
    // Broiler-Human:        PENDING
    private static ImmutableArray<(string Code, string Field)> DateTimeFieldCodes { get; } = ImmutableArray.Create(
        ("era", "era"), ("year", "year"), ("quarter", "quarter"), ("month", "month"), ("weekOfYear", "week"),
        ("weekday", "weekday"), ("day", "day"), ("dayPeriod", "dayperiod"), ("hour", "hour"), ("minute", "minute"),
        ("second", "second"), ("timeZoneName", "zone"));

    /// <summary>
    /// Every table, from the verified CLDR archive and the UCD subset, for <paramref name="options"/>.
    /// </summary>
    /// <param name="pin">The CLDR pin the archive is verified against before anything reads it.</param>
    /// <param name="cldr">Every file the pin names, by the path the pin writes it under.</param>
    /// <param name="ucd">The UCD subset, read from files the caller verified against a pin.</param>
    /// <param name="options">The locale list and the other parameters.</param>
    /// <exception cref="InvalidDataException">The archive is not the one the pin describes, a file a table reads is not pinned, or a value is not one the table can carry.</exception>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 CLDR tables via Broiler.VM CldrTableGenerator; IP=Low; Security=High; Resources=8; Fingerprint=E3BEF1
    // Broiler-Falsified-If: tables are written from an archive the pin does not describe, or with Broiler.VM's options they differ from Broiler.VM's generator's
    // Broiler-Human:        PENDING
    public static IntlTables Generate(IntlCldrPin pin, IReadOnlyDictionary<string, byte[]> cldr, CldrUcd ucd, IntlGeneratorOptions options)
    {
        ArgumentNullException.ThrowIfNull(pin);
        ArgumentNullException.ThrowIfNull(cldr);
        ArgumentNullException.ThrowIfNull(ucd);
        ArgumentNullException.ThrowIfNull(options);

        pin.Verified(cldr);

        // THE ARCHIVE IS THE PIN'S FILES AND NO OTHER: a file the caller hands in beside them is never read,
        // whether a table enumerates a directory or asks whether an optional file is there.
        var pinned = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        foreach (var entry in pin.Entries)
        {
            pinned[entry.Name] = cldr[entry.Name];
        }

        var archive = new Archive(pinned);
        var list = options.Locales;
        var main = list.MainDataLocales;

        var root = RootCollation.Read(ArchiveReading.Decode(archive.Bytes(Root + "common/uca/allkeys_CLDR.txt")));
        var tailorings = Tailorings(root, ucd, archive, options.Tailorings);

        var tables = new List<IntlGeneratedTable>
        {
            Text("LikelySubtags", "CLDR's likely subtags: from, to.", LikelySubtags(archive)),
            Text("Aliases", "CLDR's language, script, region, variant and subdivision aliases: kind, from, replacement.", Aliases(archive)),
            Text("Extensions", "The BCP 47 extension keys and their types: extension, key, type, preferred type.", Extensions(archive)),
            Text("Locales", "The locales this data supports.", list.Locales),
            Text("NumberLocales", "The number data of each supported language for the latn numbering system, flattened: language, key, value.", NumberLocales(archive, main, options.NumberingSymbols)),
            Text("Currencies", "The currency names of each supported language: language, code, symbol, narrow symbol, singular name, plural name, name, and whether each symbol's first and last characters are symbols or separators.", Currencies(archive, main, ucd)),
            Text("CurrencyDigits", "The currencies whose fraction digits are not 2: code, digits.", CurrencyDigits(archive)),
            Text("NumberingSystems", "The numbering systems with a simple digit mapping: name, digits.", NumberingSystems(archive)),
            Text("Plurals", "The cardinal plural rules of each supported language: language, category, rule.", PluralRules(archive, "plurals.json", "plurals-type-cardinal", list)),
            Text("PluralRanges", "The plural range rules of each supported language: language, start, end, result.", PluralRanges(archive, list)),
            Text("Ordinals", "The ordinal plural rules of each supported language: language, category, rule.", PluralRules(archive, "ordinals.json", "plurals-type-ordinal", list)),
            Text("ListPatterns", "The list patterns of each supported language: language, type, start, middle, end, pair.", ListPatterns(archive, main)),
            Text("RelativeTimes", "The relative time patterns of each supported language: language, field, key, pattern.", RelativeTimes(archive, main)),
            Text("DisplayNames", "The display names of each supported language: language, kind, code, name.", DisplayNames(archive, main)),
            Text("Units", "The sanctioned units' patterns of each supported language: language, width, unit, field, value.", Units(archive, main)),
            Text("DateLocales", "The Gregorian calendar, date field and zone name data of each supported language, flattened: language, key, value.", DateLocales(archive, main, options.Calendars)),
            Text("MetaZones", "Which metazone each zone uses when (z: zone, from, to, metazone), each metazone's golden zone in a region (g: metazone, region, zone), and a country's primary zone (p: region, zone), by primary identifier.", MetaZones(archive)),
            Text("TimeData", "The hour cycles allowed and preferred in each region: region, allowed, preferred.", TimeData(archive)),
            Text("DayPeriods", "The day period rules of each supported language: language, period, at or from, before.", DayPeriods(archive, list)),
            Text("WeekData", "Each region's week: region, first day, weekend start, weekend end, minimal days; empty where the world's applies.", WeekData(archive)),
            Text("Scripts", "The line direction of each script CLDR states one for: script, right to left (YES) or not (NO).", Scripts(archive)),
            new("CollationRoot", "The root collation: allkeys_CLDR.txt in runs and entries, the implicit-weight ranges and the unified ideographs.", false, root.Encode(ucd)),
            new(
                "CollationTailorings",
                "The tailorings the supported locales use, each in weights scaled by 256: root search, German phonebook and German search.",
                false,
                Tailoring.Encode(tailorings)),
        };

        return new IntlTables(pin.Version, pin.Values.TryGetValue("uca-version", out var uca) ? uca : string.Empty, tables.AsReadOnly(), archive.Absent());
    }

    /// <summary>
    /// The archive a generation reads: the files the pin names, by the paths it writes them under, and no other,
    /// recording each optional names file the options asked for and the pin does not name.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D11 CLDR tables; IP=Low; Security=Medium; Resources=2; Fingerprint=7B4C3A
    // Broiler-Human:        PENDING
    private sealed class Archive(IReadOnlyDictionary<string, byte[]> files)
    {
        // Broiler-AI:           Origin=AI; Spec=JSD-0082 D11 CLDR tables; IP=Low; Security=Low; Resources=1; Fingerprint=1716CD
        // Broiler-Human:        PENDING
        private readonly SortedSet<string> absent = new(StringComparer.Ordinal);

        /// <summary>Every path the archive holds.</summary>
        // Broiler-AI:           Origin=AI; Spec=JSD-0082 D11 CLDR tables; IP=Low; Security=Low; Resources=0; Fingerprint=871A1C
        // Broiler-Human:        PENDING
        internal IEnumerable<string> Names => files.Keys;

        /// <summary>A pinned file's bytes, or a refusal naming the file a table reads and the pin does not name.</summary>
        // Broiler-AI:           Origin=AI; Spec=JSD-0082 D11 CLDR tables; IP=Low; Security=High; Resources=1; Fingerprint=ADFB0A
        // Broiler-Falsified-If: a file the pin does not name is read, or its absence is answered with empty data
        // Broiler-Human:        PENDING
        internal byte[] Bytes(string path) =>
            files.TryGetValue(path, out var bytes)
                ? bytes
                : throw new InvalidDataException("a table reads " + path + ", and the CLDR pin does not name it");

        /// <summary>A pinned file's parsed root element.</summary>
        // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 CLDR tables via Broiler.VM CldrTableGenerator; IP=Low; Security=Medium; Resources=2; Fingerprint=6F6F2A
        // Broiler-Human:        PENDING
        internal JsonElement Json(string path)
        {
            using var document = JsonDocument.Parse(Bytes(Root + path));
            return document.RootElement.Clone();
        }

        /// <summary>An optional names file's parsed root element, or false - recorded - when the pin does not name it.</summary>
        // Broiler-AI:           Origin=AI; Spec=JSD-0082 D11 CLDR tables; IP=Low; Security=Medium; Resources=2; Fingerprint=49553B
        // Broiler-Human:        PENDING
        internal bool TryJson(string path, out JsonElement element)
        {
            if (!files.ContainsKey(Root + path))
            {
                absent.Add(Root + path);
                element = default;
                return false;
            }

            element = Json(path);
            return true;
        }

        /// <summary>The optional files asked for and not pinned, in ordinal order.</summary>
        // Broiler-AI:           Origin=AI; Spec=JSD-0082 D11 CLDR tables; IP=Low; Security=Low; Resources=1; Fingerprint=E037CE
        // Broiler-Human:        PENDING
        internal IReadOnlyList<string> Absent() => absent.ToList().AsReadOnly();
    }

    /// <summary>A text table: its lines, each refused unless it is printable ASCII without a quote, joined by LF.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 CLDR tables via Broiler.VM CldrTableGenerator; IP=Low; Security=High; Resources=2; Fingerprint=1713B4
    // Broiler-Falsified-If: a line holding a quote, a control character or a character above U+007E, or a table with no line, is written
    // Broiler-Human:        PENDING
    private static IntlGeneratedTable Text(string name, string summary, IEnumerable<string> lines)
    {
        var text = new StringBuilder();

        foreach (var line in lines)
        {
            if (line.Contains('"', StringComparison.Ordinal) || line.Any(static c => c > 0x7E || c < 0x20))
            {
                throw new InvalidDataException("a " + name + " line is not plain ASCII: `" + line + "`");
            }

            text.Append(line).Append('\n');
        }

        if (text.Length == 0)
        {
            throw new InvalidDataException("the " + name + " table has no line");
        }

        text.Length--;
        return new IntlGeneratedTable(name, summary, true, Encoding.ASCII.GetBytes(text.ToString()));
    }

    /// <summary>The tailorings the options name, each built from its collation rules over the root.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 tailorings via Broiler.VM CldrTableGenerator; IP=Low; Security=Medium; Resources=6; Fingerprint=2AF9D9
    // Broiler-Human:        PENDING
    private static List<(string Name, Tailoring Tailoring)> Tailorings(RootCollation root, CldrUcd ucd, Archive archive, ImmutableArray<string> names)
    {
        var rootXml = ArchiveReading.Decode(archive.Bytes(Root + "common/collation/root.xml"));
        var deXml = ArchiveReading.Decode(archive.Bytes(Root + "common/collation/de.xml"));
        var noImports = FrozenDictionary<string, string>.Empty;

        if (names.Any(name => !IntlGeneratorOptions.BroilerVmTailorings.Contains(name, StringComparer.Ordinal)) ||
            !names.SequenceEqual(IntlGeneratorOptions.BroilerVmTailorings.Where(names.Contains), StringComparer.Ordinal))
        {
            throw new InvalidDataException("the tailorings `" + string.Join(", ", names) + "` are not a subset of `" + string.Join(", ", IntlGeneratorOptions.BroilerVmTailorings) + "` in its order");
        }

        var built = new List<(string Name, Tailoring Tailoring)>();

        foreach (var name in names)
        {
            built.Add((name, name switch
            {
                "und-search" => Tailoring.Build(root, ucd, CollationRules(rootXml, "search"), imports: noImports),
                "de-phonebk" => Tailoring.Build(root, ucd, CollationRules(deXml, "phonebook"), imports: noImports),
                _ => Tailoring.Build(
                    root,
                    ucd,
                    CollationRules(deXml, "search"),
                    imports: new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["und-u-co-search"] = CollationRules(rootXml, "search"),
                        ["de-u-co-phonebk"] = CollationRules(deXml, "phonebook"),
                    }),
            }));
        }

        return built;
    }

    /// <summary>Likely subtags, ordinal by source.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 likely subtags via Broiler.VM CldrTableGenerator; IP=Low; Security=Low; Resources=2; Fingerprint=A00998
    // Broiler-Human:        PENDING
    private static IEnumerable<string> LikelySubtags(Archive archive) =>
        archive.Json("json/cldr-core/supplemental/likelySubtags.json")
            .GetProperty("supplemental").GetProperty("likelySubtags")
            .EnumerateObject()
            .Select(static entry => entry.Name + "|" + entry.Value.GetString())
            .Order(StringComparer.Ordinal);

    /// <summary>Every alias kind ECMA-402's canonicalization reads.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 aliases via Broiler.VM CldrTableGenerator; IP=Low; Security=Low; Resources=2; Fingerprint=7FA483
    // Broiler-Human:        PENDING
    private static IEnumerable<string> Aliases(Archive archive)
    {
        var alias = archive.Json("json/cldr-core/supplemental/aliases.json")
            .GetProperty("supplemental").GetProperty("metadata").GetProperty("alias");

        var lines = new List<string>();

        foreach (var (kind, property) in new[]
                 {
                     ("language", "languageAlias"),
                     ("script", "scriptAlias"),
                     ("region", "territoryAlias"),
                     ("variant", "variantAlias"),
                     ("subdivision", "subdivisionAlias"),
                 })
        {
            foreach (var entry in alias.GetProperty(property).EnumerateObject())
            {
                lines.Add(kind + "|" + entry.Name + "|" + entry.Value.GetProperty("_replacement").GetString());
            }
        }

        return lines.Order(StringComparer.Ordinal);
    }

    /// <summary>Every BCP 47 key and type the <c>bcp47</c> files define, with a deprecated type's preferred one.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 BCP 47 extensions via Broiler.VM CldrTableGenerator; IP=Low; Security=Low; Resources=3; Fingerprint=C0339F
    // Broiler-Human:        PENDING
    private static IEnumerable<string> Extensions(Archive archive)
    {
        var lines = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var name in archive.Names.Where(static name => name.StartsWith(Root + "json/cldr-bcp47/bcp47/", StringComparison.Ordinal)))
        {
            JsonElement keyword;

            using (var document = JsonDocument.Parse(archive.Bytes(name)))
            {
                keyword = document.RootElement.GetProperty("keyword").Clone();
            }

            foreach (var extension in keyword.EnumerateObject())
            {
                foreach (var key in extension.Value.EnumerateObject())
                {
                    lines.Add(extension.Name + "|" + key.Name + "||");

                    var types = key.Value.EnumerateObject()
                        .Where(static type => !type.Name.StartsWith('_'))
                        .ToList();

                    foreach (var type in types)
                    {
                        var preferred = type.Value.TryGetProperty("_preferred", out var value) ? value.GetString() : string.Empty;
                        lines.Add(extension.Name + "|" + key.Name + "|" + type.Name + "|" + preferred);
                    }

                    // A TYPE'S ALIASES ARE THE OTHER SPELLINGS IT ACCEPTS - `yes` for `true` - and
                    // canonicalization replaces each by the type, or by the type's own preferred
                    // one when the type is deprecated. Only an alias that is a well-formed type is
                    // one: a time zone's aliases are IANA names, and those of one segment - `Eire`,
                    // `EST5EDT` - are types once lower-cased, as UTS #35 reads a BCP 47 value.
                    foreach (var type in types)
                    {
                        if (!type.Value.TryGetProperty("_alias", out var alias))
                        {
                            continue;
                        }

                        var target = type.Value.TryGetProperty("_preferred", out var preferredType)
                            ? preferredType.GetString()!
                            : type.Name;

                        foreach (var written in alias.GetString()!.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                        {
                            var spelling = key.Name == "tz" ? ArchiveReading.AsciiLower(written) : written;

                            if (IsWellFormedType(spelling) &&
                                !types.Any(other => string.Equals(other.Name, spelling, StringComparison.Ordinal)))
                            {
                                lines.Add(extension.Name + "|" + key.Name + "|" + spelling + "|" + target);
                            }
                        }
                    }
                }
            }
        }

        return lines;
    }

    /// <summary>
    /// A BCP 47 type: subtags of three to eight lowercase letters and digits, separated by hyphens. Broiler.VM's
    /// generator matched <c>^[a-z0-9]{3,8}(-[a-z0-9]{3,8})*$</c>, which this reads by hand (rule SL4); the
    /// spellings it is asked about hold no line feed, the one input on which the expression's <c>$</c> differs.
    /// </summary>
    // Broiler-AI:           Origin=Ported; Spec=UTS 35 BCP 47 type via Broiler.VM CldrTableGenerator; IP=Low; Security=Medium; Resources=1; Fingerprint=4E60EC
    // Broiler-Human:        PENDING
    internal static bool IsWellFormedType(string text) =>
        text.Split('-').All(static subtag => subtag.Length is >= 3 and <= 8 && subtag.All(static c => c is >= 'a' and <= 'z' or >= '0' and <= '9'));

    /// <summary>
    /// A table value with every character a line cannot carry written as <c>\uXXXX</c>: what is not printable
    /// ASCII, and the backslash, the bar and the quote.
    /// </summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 table escaping via Broiler.VM CldrTableGenerator; IP=Low; Security=Medium; Resources=1; Fingerprint=FC248C
    // Broiler-Human:        PENDING
    internal static string Escape(string value)
    {
        var escaped = new StringBuilder(value.Length);

        foreach (var c in value)
        {
            if (c < 0x20 || c > 0x7E || c is '\\' or '|' or '"')
            {
                escaped.Append("\\u").Append(((int)c).ToString("X4", NumberFormatInfo.InvariantInfo));
            }
            else
            {
                escaped.Append(c);
            }
        }

        return escaped.ToString();
    }

    /// <summary>
    /// Each main-data locale's number data for <c>latn</c>, its JSON flattened to dotted keys; its duration
    /// patterns; and with <see cref="IntlNumberingSymbols.EveryNumberingSystem"/> its other numbering systems'
    /// symbols, and root's own for <c>und</c>.
    /// </summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 D21 D34 number data via Broiler.VM CldrTableGenerator; IP=Low; Security=Low; Resources=4; Fingerprint=F989B0
    // Broiler-Human:        PENDING
    private static IEnumerable<string> NumberLocales(Archive archive, ImmutableArray<string> languages, IntlNumberingSymbols symbols)
    {
        var lines = new List<string>();

        foreach (var language in languages)
        {
            var numbers = archive.Json("json/cldr-numbers-full/main/" + language + "/numbers.json")
                .GetProperty("main").GetProperty(language).GetProperty("numbers");

            foreach (var property in numbers.EnumerateObject())
            {
                var key = property.Name switch
                {
                    "symbols-numberSystem-latn" => "symbols",
                    "decimalFormats-numberSystem-latn" => "decimal",
                    "percentFormats-numberSystem-latn" => "percent",
                    "scientificFormats-numberSystem-latn" => "scientific",
                    "currencyFormats-numberSystem-latn" => "currency",
                    "miscPatterns-numberSystem-latn" => "misc",
                    "defaultNumberingSystem" or "minimumGroupingDigits" => property.Name,
                    _ => null,
                };

                // EVERY OTHER NUMBERING SYSTEM'S OWN SYMBOLS (decision D21), under a key no latn line uses.
                if (key is null && symbols == IntlNumberingSymbols.EveryNumberingSystem &&
                    property.Name.StartsWith("symbols-numberSystem-", StringComparison.Ordinal))
                {
                    key = "symbols@" + property.Name["symbols-numberSystem-".Length..];
                }

                if (key is not null)
                {
                    Flatten(language, key, property.Value, lines);
                }
            }

            if (symbols == IntlNumberingSymbols.EveryNumberingSystem && language == "und")
            {
                lines.AddRange(RootSymbols(archive, numbers));
            }

            // THE DURATION PATTERNS, which say whether a digital duration writes its hours with two
            // digits (Intl.DurationFormat's [[TwoDigitHours]], JSD-0051).
            var units = archive.Json("json/cldr-units-full/main/" + language + "/units.json")
                .GetProperty("main").GetProperty(language).GetProperty("units");

            foreach (var type in new[] { "hm", "hms", "ms" })
            {
                lines.Add(language + "|duration." + type + "|" + Escape(units.GetProperty("durationUnit-type-" + type).GetProperty("durationUnitPattern").GetString()!));
            }
        }

        return lines.Order(StringComparer.Ordinal);
    }

    /// <summary>
    /// Root's own symbols for each numbering system <c>common/main/root.xml</c> states them for and
    /// <c>und/numbers.json</c> does not - <c>arab</c> and <c>arabext</c> - as <c>und</c> lines. A system root
    /// aliases to the locale's <c>latn</c> symbols carries no line.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D21 root numbering-system symbols; IP=Low; Security=Medium; Resources=3; Fingerprint=7AFCBA
    // Broiler-Human:        PENDING
    private static IEnumerable<string> RootSymbols(Archive archive, JsonElement undNumbers)
    {
        var root = XDocument.Parse(Encoding.UTF8.GetString(archive.Bytes(Root + "common/main/root.xml")));
        var numbers = root.Descendants("numbers").Single();

        foreach (var system in numbers.Elements("symbols"))
        {
            var name = (string?)system.Attribute("numberSystem");

            if (name is null || name == "latn" || system.Elements("alias").Any() ||
                undNumbers.TryGetProperty("symbols-numberSystem-" + name, out _))
            {
                continue;
            }

            foreach (var symbol in system.Elements().Where(static element => element.Attribute("alt") is null))
            {
                yield return "und|symbols@" + name + "." + symbol.Name.LocalName + "|" + Escape(symbol.Value);
            }
        }
    }

    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 number data via Broiler.VM CldrTableGenerator; IP=Low; Security=Low; Resources=2; Fingerprint=E9E88C
    // Broiler-Human:        PENDING
    private static void Flatten(string language, string key, JsonElement value, List<string> lines)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in value.EnumerateObject())
            {
                Flatten(language, key + "." + property.Name, property.Value, lines);
            }

            return;
        }

        lines.Add(language + "|" + key + "|" + Escape(value.GetString()!));
    }

    /// <summary>Each main-data locale's currency symbols and names.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 currencies via Broiler.VM CldrTableGenerator; IP=Low; Security=Low; Resources=3; Fingerprint=5C08E5
    // Broiler-Human:        PENDING
    private static IEnumerable<string> Currencies(Archive archive, ImmutableArray<string> languages, CldrUcd ucd)
    {
        var lines = new List<string>();

        // CLDR'S CURRENCY SPACING asks whether a symbol's character next to the number is
        // [[:^S:]&[:^Z:]]; the profile has no General_Category table, so each symbol states it here:
        // `S` for a symbol or separator, `L` for anything else, first character then last.
        string Ends(string symbol)
        {
            if (symbol.Length == 0)
            {
                return string.Empty;
            }

            char Kind(int codePoint) => ucd.IsSymbolOrSeparator(codePoint) ? 'S' : 'L';
            return new string([Kind(char.ConvertToUtf32(symbol, 0)), Kind(char.IsLowSurrogate(symbol[^1]) ? char.ConvertToUtf32(symbol, symbol.Length - 2) : symbol[^1])]);
        }

        foreach (var language in languages)
        {
            var currencies = archive.Json("json/cldr-numbers-full/main/" + language + "/currencies.json")
                .GetProperty("main").GetProperty(language).GetProperty("numbers").GetProperty("currencies");

            foreach (var currency in currencies.EnumerateObject())
            {
                string Field(string name) =>
                    currency.Value.TryGetProperty(name, out var field) ? Escape(field.GetString()!) : string.Empty;

                string Raw(string name) =>
                    currency.Value.TryGetProperty(name, out var field) ? field.GetString()! : string.Empty;

                lines.Add(string.Join('|',
                    language, currency.Name, Field("symbol"), Field("symbol-alt-narrow"),
                    Field("displayName-count-one"), Field("displayName-count-other"), Field("displayName"),
                    Ends(Raw("symbol")), Ends(Raw("symbol-alt-narrow"))));
            }
        }

        return lines.Order(StringComparer.Ordinal);
    }

    /// <summary>CLDR's currency fraction digits that differ from its default of 2.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 currency digits via Broiler.VM CldrTableGenerator; IP=Low; Security=Low; Resources=2; Fingerprint=EFD801
    // Broiler-Human:        PENDING
    private static IEnumerable<string> CurrencyDigits(Archive archive) =>
        archive.Json("json/cldr-core/supplemental/currencyData.json")
            .GetProperty("supplemental").GetProperty("currencyData").GetProperty("fractions")
            .EnumerateObject()
            .Where(static currency => currency.Name != "DEFAULT" && currency.Value.GetProperty("_digits").GetString() != "2")
            .Select(static currency => currency.Name + "|" + currency.Value.GetProperty("_digits").GetString())
            .Order(StringComparer.Ordinal);

    /// <summary>The numeric numbering systems and their digits.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 numbering systems via Broiler.VM CldrTableGenerator; IP=Low; Security=Low; Resources=2; Fingerprint=7124A8
    // Broiler-Human:        PENDING
    private static IEnumerable<string> NumberingSystems(Archive archive) =>
        archive.Json("json/cldr-core/supplemental/numberingSystems.json")
            .GetProperty("supplemental").GetProperty("numberingSystems")
            .EnumerateObject()
            .Where(static system => system.Value.GetProperty("_type").GetString() == "numeric")
            .Select(static system => system.Name + "|" + Escape(system.Value.GetProperty("_digits").GetString()!))
            .Order(StringComparer.Ordinal);

    /// <summary>
    /// The languages a supplemental table is written for: the list's, or with <c>supplemental *</c> every
    /// language <paramref name="rules"/> states, in ordinal order.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D5 supplemental languages; IP=Low; Security=Low; Resources=1; Fingerprint=5CBB4B
    // Broiler-Human:        PENDING
    private static IEnumerable<string> SupplementalLanguages(IntlLocaleList list, JsonElement rules) =>
        list.EverySupplementalLanguage
            ? rules.EnumerateObject().Select(static language => language.Name).Order(StringComparer.Ordinal)
            : list.SupplementalLanguages;

    /// <summary>Each supplemental language's rules of one plural type, without their samples.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D5 D11 plural rules via Broiler.VM CldrTableGenerator; IP=Low; Security=Low; Resources=3; Fingerprint=85557A
    // Broiler-Human:        PENDING
    private static IEnumerable<string> PluralRules(Archive archive, string file, string type, IntlLocaleList list)
    {
        var rules = archive.Json("json/cldr-core/supplemental/" + file)
            .GetProperty("supplemental").GetProperty(type);

        foreach (var language in SupplementalLanguages(list, rules))
        {
            foreach (var rule in rules.GetProperty(language).EnumerateObject())
            {
                var text = rule.Value.GetString()!;
                var samples = text.IndexOf('@', StringComparison.Ordinal);
                yield return language + "|" + rule.Name["pluralRule-count-".Length..] + "|" + Escape((samples < 0 ? text : text[..samples]).Trim());
            }
        }
    }

    /// <summary>Each supplemental language's plural range rules.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D5 D11 plural ranges via Broiler.VM CldrTableGenerator; IP=Low; Security=Low; Resources=3; Fingerprint=6A7C01
    // Broiler-Human:        PENDING
    private static IEnumerable<string> PluralRanges(Archive archive, IntlLocaleList list)
    {
        var ranges = archive.Json("json/cldr-core/supplemental/pluralRanges.json")
            .GetProperty("supplemental").GetProperty("plurals");

        foreach (var language in SupplementalLanguages(list, ranges))
        {
            foreach (var range in ranges.GetProperty(language).EnumerateObject())
            {
                // pluralRange-start-<start>-end-<end>
                var parts = range.Name.Split('-');
                yield return language + "|" + parts[2] + "|" + parts[4] + "|" + range.Value.GetString();
            }
        }
    }

    /// <summary>
    /// Each main-data locale's list patterns: the type as CLDR names it (<c>standard</c>, <c>or-short</c>,
    /// <c>unit-narrow</c> and the rest), and its start, middle, end and two-element patterns.
    /// </summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 list patterns via Broiler.VM CldrTableGenerator; IP=Low; Security=Low; Resources=2; Fingerprint=68402D
    // Broiler-Human:        PENDING
    private static IEnumerable<string> ListPatterns(Archive archive, ImmutableArray<string> languages)
    {
        foreach (var language in languages)
        {
            var patterns = archive.Json("json/cldr-misc-full/main/" + language + "/listPatterns.json")
                .GetProperty("main").GetProperty(language).GetProperty("listPatterns");

            foreach (var type in patterns.EnumerateObject().OrderBy(static entry => entry.Name, StringComparer.Ordinal))
            {
                var fields = new[] { "start", "middle", "end", "2" }.Select(field => Escape(type.Value.GetProperty(field).GetString()!));
                yield return language + "|" + type.Name["listPattern-type-".Length..] + "|" + string.Join('|', fields);
            }
        }
    }

    /// <summary>
    /// Each main-data locale's relative time data for the eight units ECMA-402 names, in the long,
    /// <c>-short</c> and <c>-narrow</c> fields: the literal for a value (<c>-1</c>, <c>0</c>, <c>1</c> and the
    /// rest CLDR names), and the <c>future.</c> and <c>past.</c> pattern for each plural category.
    /// </summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 relative times via Broiler.VM CldrTableGenerator; IP=Low; Security=Low; Resources=3; Fingerprint=843F29
    // Broiler-Human:        PENDING
    private static IEnumerable<string> RelativeTimes(Archive archive, ImmutableArray<string> languages)
    {
        foreach (var language in languages)
        {
            var fields = archive.Json("json/cldr-dates-full/main/" + language + "/dateFields.json")
                .GetProperty("main").GetProperty(language).GetProperty("dates").GetProperty("fields");

            foreach (var unit in new[] { "second", "minute", "hour", "day", "week", "month", "quarter", "year" })
            {
                foreach (var field in new[] { unit, unit + "-short", unit + "-narrow" })
                {
                    foreach (var entry in fields.GetProperty(field).EnumerateObject().OrderBy(static entry => entry.Name, StringComparer.Ordinal))
                    {
                        if (entry.Name.StartsWith("relative-type-", StringComparison.Ordinal))
                        {
                            yield return language + "|" + field + "|" + entry.Name["relative-type-".Length..] + "|" + Escape(entry.Value.GetString()!);
                        }
                        else if (entry.Name.StartsWith("relativeTime-type-", StringComparison.Ordinal))
                        {
                            var tense = entry.Name["relativeTime-type-".Length..];

                            foreach (var pattern in entry.Value.EnumerateObject().OrderBy(static pattern => pattern.Name, StringComparer.Ordinal))
                            {
                                yield return language + "|" + field + "|" + tense + "." + pattern.Name["relativeTimePattern-count-".Length..] + "|" + Escape(pattern.Value.GetString()!);
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Each main-data locale's display names (JSD-0052): its names of languages, regions, scripts and variants
    /// with their alternate forms but the menu ones; its locale patterns; its calendar names under their BCP 47
    /// identifiers; and its date-time field names in three widths. A names file CLDR carries for no such
    /// locale gives no names of its kind (<see cref="IntlTables.AbsentFiles"/>), and a locale whose display
    /// names state no calendar's has no calendar names.
    /// </summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 display names via Broiler.VM CldrTableGenerator; IP=Low; Security=Low; Resources=4; Fingerprint=36F508
    // Broiler-Human:        PENDING
    private static IEnumerable<string> DisplayNames(Archive archive, ImmutableArray<string> languages)
    {
        var calendars = archive.Json("json/cldr-bcp47/bcp47/calendar.json")
            .GetProperty("keyword").GetProperty("u").GetProperty("ca");

        foreach (var language in languages)
        {
            var lines = new List<string>();

            foreach (var (file, kind) in NameFiles)
            {
                if (!archive.TryJson("json/cldr-localenames-full/main/" + language + "/" + file + ".json", out var document))
                {
                    continue;
                }

                var names = document.GetProperty("main").GetProperty(language).GetProperty("localeDisplayNames").GetProperty(file);

                foreach (var entry in names.EnumerateObject())
                {
                    if (!entry.Name.EndsWith("-alt-menu", StringComparison.Ordinal))
                    {
                        lines.Add(language + "|" + kind + "|" + entry.Name + "|" + Escape(entry.Value.GetString()!));
                    }
                }
            }

            var display = archive.Json("json/cldr-localenames-full/main/" + language + "/localeDisplayNames.json")
                .GetProperty("main").GetProperty(language).GetProperty("localeDisplayNames");

            foreach (var pattern in new[] { "localePattern", "localeSeparator" })
            {
                lines.Add(language + "|pattern|" + pattern + "|" + Escape(display.GetProperty("localeDisplayPattern").GetProperty(pattern).GetString()!));
            }

            // A LOCALE WHOSE DATA NAMES NO CALENDAR - a seed locale of CLDR's, whose resolved file has no
            // `types` - has no calendar names; Broiler.VM's languages all have them.
            var calendarNames = display.TryGetProperty("types", out var types) && types.TryGetProperty("calendar", out var stated)
                ? stated
                : default;

            foreach (var calendar in calendars.EnumerateObject())
            {
                if (calendar.Name.StartsWith('_') || calendarNames.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var key = calendar.Value.TryGetProperty("_alias", out var alias) ? alias.GetString()! : calendar.Name;

                if (calendarNames.TryGetProperty(key, out var name) || calendarNames.TryGetProperty(calendar.Name, out name))
                {
                    lines.Add(language + "|calendar|" + calendar.Name + "|" + Escape(name.GetString()!));
                }
            }

            var fields = archive.Json("json/cldr-dates-full/main/" + language + "/dateFields.json")
                .GetProperty("main").GetProperty(language).GetProperty("dates").GetProperty("fields");

            foreach (var (code, field) in DateTimeFieldCodes)
            {
                foreach (var (width, suffix) in new[] { ("long", ""), ("short", "-short"), ("narrow", "-narrow") })
                {
                    if (fields.TryGetProperty(field + suffix, out var named) && named.TryGetProperty("displayName", out var value))
                    {
                        lines.Add(language + "|field|" + code + "-" + width + "|" + Escape(value.GetString()!));
                    }
                }
            }

            foreach (var line in lines.Order(StringComparer.Ordinal))
            {
                yield return line;
            }
        }
    }

    /// <summary>
    /// Each main-data locale's patterns for the sanctioned units and the compound units CLDR names whose two
    /// halves are sanctioned, in the three widths, and each width's compound pattern.
    /// </summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 units via Broiler.VM CldrTableGenerator; IP=Low; Security=Low; Resources=3; Fingerprint=6D41B0
    // Broiler-Human:        PENDING
    private static IEnumerable<string> Units(Archive archive, ImmutableArray<string> languages)
    {
        var lines = new List<string>();

        static bool Wanted(string unit)
        {
            if (SanctionedUnits.Contains(unit))
            {
                return true;
            }

            var per = unit.IndexOf("-per-", StringComparison.Ordinal);
            return per > 0 &&
                SanctionedUnits.Contains(unit[..per]) &&
                SanctionedUnits.Contains(unit[(per + "-per-".Length)..]);
        }

        foreach (var language in languages)
        {
            var units = archive.Json("json/cldr-units-full/main/" + language + "/units.json")
                .GetProperty("main").GetProperty(language).GetProperty("units");

            foreach (var width in new[] { "long", "short", "narrow" })
            {
                foreach (var entry in units.GetProperty(width).EnumerateObject())
                {
                    if (entry.Name == "per")
                    {
                        lines.Add(language + "|" + width + "|per|compoundUnitPattern|" + Escape(entry.Value.GetProperty("compoundUnitPattern").GetString()!));
                        continue;
                    }

                    var dash = entry.Name.IndexOf('-');

                    if (dash < 0 || entry.Value.ValueKind != JsonValueKind.Object || !Wanted(entry.Name[(dash + 1)..]))
                    {
                        continue;
                    }

                    foreach (var field in entry.Value.EnumerateObject())
                    {
                        if (field.Name is "displayName" or "perUnitPattern" || field.Name.StartsWith("unitPattern-count-", StringComparison.Ordinal))
                        {
                            lines.Add(language + "|" + width + "|" + entry.Name[(dash + 1)..] + "|" + field.Name + "|" + Escape(field.Value.GetString()!));
                        }
                    }
                }
            }
        }

        return lines.Order(StringComparer.Ordinal);
    }

    /// <summary>The hour cycles CLDR allows and prefers in each region it names: region, allowed, preferred.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 time data via Broiler.VM CldrTableGenerator; IP=Low; Security=Low; Resources=2; Fingerprint=BE7E85
    // Broiler-Human:        PENDING
    private static IEnumerable<string> TimeData(Archive archive) =>
        archive.Json("json/cldr-core/supplemental/timeData.json").GetProperty("supplemental").GetProperty("timeData")
            .EnumerateObject()
            .Select(static entry => entry.Name + "|" + entry.Value.GetProperty("_allowed").GetString() + "|" + entry.Value.GetProperty("_preferred").GetString())
            .Order(StringComparer.Ordinal)
            .ToList();

    /// <summary>
    /// Each region's week: region, first day, weekend start, weekend end and minimal days in the first week, a
    /// field empty where CLDR states none and the world's (<c>001</c>) applies.
    /// </summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 week data via Broiler.VM CldrTableGenerator; IP=Low; Security=Low; Resources=2; Fingerprint=FF61DD
    // Broiler-Human:        PENDING
    private static IEnumerable<string> WeekData(Archive archive)
    {
        var week = archive.Json("json/cldr-core/supplemental/weekData.json").GetProperty("supplemental").GetProperty("weekData");
        var fields = new[] { "firstDay", "weekendStart", "weekendEnd", "minDays" };
        var regions = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var field in fields)
        {
            foreach (var entry in week.GetProperty(field).EnumerateObject())
            {
                if (!entry.Name.Contains("-alt-", StringComparison.Ordinal))
                {
                    regions.Add(entry.Name);
                }
            }
        }

        return regions.Select(region => region + string.Concat(fields.Select(field =>
            "|" + (week.GetProperty(field).TryGetProperty(region, out var value) ? value.GetString() : string.Empty)))).ToList();
    }

    /// <summary>Each script whose line direction CLDR states: script, and whether it is right to left (<c>YES</c>) or not (<c>NO</c>).</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D11 scripts via Broiler.VM CldrTableGenerator; IP=Low; Security=Low; Resources=2; Fingerprint=3CF86E
    // Broiler-Human:        PENDING
    private static IEnumerable<string> Scripts(Archive archive) =>
        archive.Json("json/cldr-core/scriptMetadata.json").GetProperty("scriptMetadata")
            .EnumerateObject()
            .Where(static entry => entry.Value.GetProperty("rtl").GetString() is "YES" or "NO")
            .Select(static entry => entry.Name + "|" + entry.Value.GetProperty("rtl").GetString())
            .Order(StringComparer.Ordinal)
            .ToList();

    /// <summary>
    /// Each supplemental language's day period rules: language, period, and the time it is at or the time it is
    /// from and the time it is before.
    /// </summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D5 D11 day periods via Broiler.VM CldrTableGenerator; IP=Low; Security=Low; Resources=2; Fingerprint=B5D88E
    // Broiler-Human:        PENDING
    private static IEnumerable<string> DayPeriods(Archive archive, IntlLocaleList list)
    {
        var rules = archive.Json("json/cldr-core/supplemental/dayPeriods.json").GetProperty("supplemental").GetProperty("dayPeriodRuleSet");
        var lines = new List<string>();

        foreach (var language in SupplementalLanguages(list, rules))
        {
            foreach (var period in rules.GetProperty(language).EnumerateObject())
            {
                lines.Add(period.Value.TryGetProperty("_at", out var at)
                    ? language + "|" + period.Name + "|" + at.GetString() + "|"
                    : language + "|" + period.Name + "|" + period.Value.GetProperty("_from").GetString() + "|" + period.Value.GetProperty("_before").GetString());
            }
        }

        return lines.Order(StringComparer.Ordinal);
    }
}
