using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Broiler.ECMAScript.Time;
using Xunit;

namespace Broiler.ECMAScript.Intl.Tests.Differential;

/// <summary>
/// The differential check of Broiler.VM's JSD-0082, decision D22, at the scale the test run affords: no difference may be
/// unclassified, every section must compare something, and the check must see a difference that is there.
/// </summary>
public sealed class DifferentialCheckTests
{
    [Fact]
    public void Broiler_VM_s_decoders_and_the_library_s_answer_alike_over_both_embeddings()
    {
        var sections = DifferentialCheck.Run(DifferentialCheck.DefaultSeed, scale: 1, only: ["D0"]);
        var report = DifferentialCheck.Report(sections, DifferentialCheck.DefaultSeed, 1);

        Assert.Equal(8, sections.Count);
        Assert.All(sections, static s => Assert.True(s.Compared > 1000, s.Name + " compared almost nothing"));
        Assert.True(sections.All(static s => s.Unclassified == 0), report);
        Assert.All(sections, static s => Assert.Equal(0, s.Classified));
    }

    /// <summary>
    /// Sub-area (a), stage SJL-9.3a: every difference between Broiler.VM's locale code and the library is a row of the
    /// correspondence - L9 and L18, the corrections of Broiler.VM the library makes, and L22, each engine's own numbering
    /// systems, which differ only over Broiler.JS's data - and each of the two corrections is seen, so the check reaches it.
    /// </summary>
    [Fact]
    public void Broiler_VM_s_locale_code_and_the_library_differ_only_by_the_correspondence_s_rows()
    {
        var sections = DifferentialCheck.Run(DifferentialCheck.DefaultSeed, scale: 1, only: ["L1", "L2", "L3", "L4"]);
        var report = DifferentialCheck.Report(sections, DifferentialCheck.DefaultSeed, 1);

        Assert.Equal(8, sections.Count);
        Assert.True(sections.All(static s => s.Unclassified == 0), report);
        Assert.All(sections.Where(static s => !s.Name.StartsWith("L4", StringComparison.Ordinal)), static s => Assert.True(s.Compared > 10000, s.Name + " compared almost nothing"));
        Assert.All(sections.Where(static s => s.Name.StartsWith("L4", StringComparison.Ordinal)), static s => Assert.True(s.Compared > 30, s.Name + " compared almost nothing"));

        var rows = sections.SelectMany(static s => s.ClassifiedByRow.Keys.Select(row => (Section: s.Name, Row: row))).ToList();
        Assert.All(rows, static r => Assert.Contains(r.Row, new[] { "L9", "L18", "L22" }));
        Assert.All(rows.Where(static r => r.Row == "L22"), static r => Assert.EndsWith("(broiler-js)", r.Section, StringComparison.Ordinal));
        Assert.Contains(rows, static r => r.Row == "L9" && r.Section.StartsWith("L1", StringComparison.Ordinal));
        Assert.Contains(rows, static r => r.Row == "L18" && r.Section.StartsWith("L3", StringComparison.Ordinal));
    }

    /// <summary>
    /// A row covers a difference only when its correction explains both answers, never by the input alone: getCollations of
    /// a Locale that names a subdivision is row L18's, and an answer the subdivision's form does not explain stays
    /// unclassified, so that an unrelated difference on such an input is not hidden.
    /// </summary>
    [Fact]
    public void A_row_covers_a_difference_only_when_its_correction_explains_both_answers()
    {
        var identifiers = new Section("L1 identifiers (test)");
        identifiers.Compare("canonical \"en-u-sd-fi01\"", "en-u-sd-AX", "en-u-sd-axzzzz");
        identifiers.Compare("canonical twice \"en-u-sd-fi01\"", "en-u-ax-sd", "en-u-sd-axzzzz");
        identifiers.Compare("parts \"en-u-sd-fi01\"", "en | en | undefined | undefined |  | sd=;ax=", "en | en | undefined | undefined |  | sd=axzzzz");
        identifiers.Compare("lookup [\"en-u-rg-cn71\"]", "en rg=;tw=", "en rg=twzzzz");
        Assert.Equal(4, identifiers.ClassifiedByRow["L9"]);
        Assert.Equal(0, identifiers.Unclassified);

        // The same inputs with answers the correction does not explain: another region, and no zzzz at all.
        identifiers.Compare("maximize \"en-u-sd-fi01\"", "found en-Latn-US-u-sd-ax", "found en-Latn-GB-u-sd-axzzzz");
        identifiers.Compare("canonical \"en-u-sd-fi01\"", "en-u-sd-AX", "en-u-sd-fi01");
        identifiers.Compare("lookup [\"en-u-rg-cn71\"]", "en rg=;tw=", "de rg=twzzzz");
        Assert.Equal(3, identifiers.Unclassified);

        var locales = new Section("L3 Intl.Locale (test)");
        locales.Compare("getCollations zh-u-sd-hkzzzz of \"zh-u-sd-cn91\"", "[emoji,eor]", "[]");
        locales.Compare("getHourCycles en-u-sd-axzzzz of \"en-u-sd-fi01\"", "[h12,h23]", "[h23]");
        locales.Compare("getWeekInfo en-u-rg-axzzzz of \"en-u-rg-fi01\"", "{firstDay:7,weekend:[6,7]}", "{firstDay:1,weekend:[6,7]}");
        locales.Compare("getCalendars en-u-sd-axzzzz of \"en-u-sd-fi01\"", "[gregory]", "[buddhist]");
        locales.Compare("getHourCycles en-u-sd-fi01 of \"en-u-sd-fi01\"", "[h12,h23]", "[h23]");
        Assert.Equal(1, locales.ClassifiedByRow["L18"]);
        Assert.Equal(2, locales.ClassifiedByRow["L9"]);
        Assert.Equal(2, locales.Unclassified);
    }

    [Fact]
    public void A_difference_in_one_table_is_found_and_left_unclassified()
    {
        // The negative control: the library reads Broiler.VM's tables with one value of English's number data changed.
        var vm = Embedding.Both().First();
        var numbers = Encoding.ASCII.GetString(vm.Text[nameof(IntlTable.NumberLocales)]);
        Assert.Contains("en|symbols.decimal|.\n", numbers, StringComparison.Ordinal);
        var changed = Encoding.ASCII.GetBytes(numbers.Replace("en|symbols.decimal|.\n", "en|symbols.decimal|,\n", StringComparison.Ordinal));

        var control = Embedding.WithLibraryTables("control", new Dictionary<string, byte[]> { [nameof(IntlTable.NumberLocales)] = changed });
        var sections = DecoderSection.Run(control, new SeededRandom(DifferentialCheck.DefaultSeed), 1).ToList();

        var differing = sections.Where(static s => s.Unclassified > 0).ToList();
        var section = Assert.Single(differing);
        Assert.StartsWith("D0.2", section.Name, StringComparison.Ordinal);
        Assert.Contains(section.Examples, static e => e.StartsWith("number en symbols.decimal => vm \".\", library \",\"", StringComparison.Ordinal));
    }

    [Fact]
    public void A_difference_in_the_likely_subtags_is_found_and_left_unclassified()
    {
        // The negative control of sub-area (a): the library reads Broiler.VM's tables with English's likely subtags changed.
        var vm = Embedding.Both().First();
        var likely = Encoding.ASCII.GetString(vm.Text[nameof(IntlTable.LikelySubtags)]);
        Assert.Contains("\nen|en-Latn-US\n", likely, StringComparison.Ordinal);
        var changed = Encoding.ASCII.GetBytes(likely.Replace("\nen|en-Latn-US\n", "\nen|en-Latn-GB\n", StringComparison.Ordinal));

        var control = Embedding.WithLibraryTables("control", new Dictionary<string, byte[]> { [nameof(IntlTable.LikelySubtags)] = changed });
        var sections = LocaleSection.Run(control, new SeededRandom(DifferentialCheck.DefaultSeed), 1, ["L1"]).ToList();

        var section = Assert.Single(sections);
        Assert.True(section.Unclassified > 0);
        Assert.Contains(section.Examples, static e => e.Contains("maximize \"", StringComparison.Ordinal) && e.Contains("vm \"found en-Latn-US", StringComparison.Ordinal) && e.Contains("library \"found en-Latn-GB", StringComparison.Ordinal));
    }

    /// <summary>
    /// Sub-area (b) (stage SJL-9.3b): Broiler.VM's mathematical value, digit options, FormatNumericToString and plural rules
    /// answer as the library does over both embeddings, but row P4's compact exponent (decision D20), which reaches an answer
    /// in Broiler.JS's embedding alone, whose languages' rules read <c>c</c> and <c>e</c>.
    /// </summary>
    [Fact]
    public void Broiler_VM_s_decimal_and_plural_code_and_the_library_differ_only_by_row_P4()
    {
        var sections = DifferentialCheck.Run(DifferentialCheck.DefaultSeed, scale: 1, only: ["M1", "M2", "M3", "P1", "P2"]);
        var report = DifferentialCheck.Report(sections, DifferentialCheck.DefaultSeed, 1);

        Assert.Equal(10, sections.Count);
        Assert.True(sections.All(static s => s.Unclassified == 0), report);
        Assert.All(sections, static s => Assert.True(s.Compared > 200, s.Name + " compared almost nothing"));

        var rows = sections.SelectMany(static s => s.ClassifiedByRow.Keys.Select(row => (Section: s.Name, Row: row))).ToList();
        Assert.All(rows, static r => Assert.Equal("P4", r.Row));
        Assert.All(rows, static r => Assert.EndsWith("(broiler-js)", r.Section, StringComparison.Ordinal));
        Assert.Contains(rows, static r => r.Section.StartsWith("P1", StringComparison.Ordinal));
    }

    /// <summary>
    /// Sub-area (b)'s population keeps what its sections compare in full before their random draws (decision D22): each
    /// dataset format with its own locale, the dataset values and ranges, and the own call of every <c>select</c> and
    /// <c>selectRange</c> vector whose arguments are literals - all but the one that selects <c>Object(1n)</c>.
    /// </summary>
    [Fact]
    public void Sub_area_b_s_population_keeps_every_dataset_format_value_and_range_and_each_vector_s_own_call()
    {
        var population = DecimalPopulation.Build(Embedding.Both().First(), new SeededRandom(DifferentialCheck.DefaultSeed), 1);

        Assert.Equal(56, population.DatasetNumberFormats.Count);
        Assert.Equal(27, population.DatasetPluralFormats.Count);
        Assert.Contains(("de-u-co-phonebk", "{localeMatcher:\"lookup\"}"), population.DatasetPluralFormats.Select(static f => (f.Locale, f.Bag.Label)));
        Assert.Contains(population.DatasetValues, static v => v.Text == "1.00000000000000000001");
        Assert.Contains(population.DatasetValues, static v => v.BigInt == System.Numerics.BigInteger.Parse("-12345678901234567890", System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal(17, population.DatasetRanges.Count);

        var vectors = Population.IntlVectors("values-and-plurals.vectors")
            .Where(static v => v["op"] is "Intl.PluralRules.prototype.select" or "Intl.PluralRules.prototype.selectRange")
            .ToList();
        var called = population.VectorCalls.Select(static c => c.Id).ToHashSet(StringComparer.Ordinal);
        var missed = vectors.Where(v => !called.Contains(v["id"]!)).Select(static v => v.Script).ToList();
        Assert.Equal(["new Intl.PluralRules(\"en\", {}).select(Object(1n))"], missed);
        Assert.Equal(107, population.VectorCalls.Count(static c => c.Method == "selectRange"));
        Assert.Contains(population.VectorCalls, static c => c is { Locale: "ar", Method: "select" } && c.Arguments.Count == 1 && c.Arguments[0].Number == 101);
    }

    /// <summary>
    /// Sub-area (c) (stage SJL-9.3c): Broiler.VM's number formatter and its list, relative-time and display-name code answer as
    /// the library does over Broiler.VM's embedding, with no difference at all, and over Broiler.JS's every difference is one the
    /// library's answer under Broiler.VM's reading of the data removes - the data locale's language, Broiler.VM's numbering
    /// systems and no own symbols (decisions D5, D13 and D21).
    /// </summary>
    [Fact]
    public void Broiler_VM_s_number_and_text_code_and_the_library_differ_only_by_the_data_they_read()
    {
        var sections = DifferentialCheck.Run(DifferentialCheck.DefaultSeed, scale: 1, only: ["N1", "N2", "N3", "S1", "S2", "S3"]);
        var report = DifferentialCheck.Report(sections, DifferentialCheck.DefaultSeed, 1);
        Assert.Equal(12, sections.Count);
        Assert.True(sections.All(static s => s.Unclassified == 0), report);
        Assert.All(sections, static s => Assert.True(s.Compared > 500, s.Name + " compared almost nothing"));
        Assert.All(sections.Where(static s => s.Name.EndsWith("(broiler-vm)", StringComparison.Ordinal)), static s => Assert.Equal(0, s.Classified));
        var rows = sections.SelectMany(static s => s.ClassifiedByRow.Keys).Distinct().ToList();
        Assert.Equal([NumberTextSection.DataRow], rows);
    }

    /// <summary>
    /// The negative control of sub-area (c): the library reads Broiler.VM's tables with English's decimal pattern changed, and the
    /// difference is found and left unclassified, since Broiler.VM's reading of the same tables gives the library's answer.
    /// </summary>
    [Fact]
    public void A_difference_in_the_decimal_pattern_is_found_and_left_unclassified()
    {
        var vm = Embedding.Both().First();
        var numbers = Encoding.ASCII.GetString(vm.Text[nameof(IntlTable.NumberLocales)]);
        Assert.Contains("en|decimal.standard|#,##0.###\n", numbers, StringComparison.Ordinal);
        var changed = Encoding.ASCII.GetBytes(numbers.Replace("en|decimal.standard|#,##0.###\n", "en|decimal.standard|#,##,##0.###\n", StringComparison.Ordinal));
        var control = Embedding.WithLibraryTables("control", new Dictionary<string, byte[]> { [nameof(IntlTable.NumberLocales)] = changed });
        var section = Assert.Single(NumberTextSection.Run(control, new SeededRandom(DifferentialCheck.DefaultSeed), 1, ["N1"]));
        Assert.True(section.Unclassified > 0);
        Assert.Equal(0, section.Classified);
        Assert.Contains(section.Examples, static e => e.Contains("vm \"integer:100|group:,|integer:000", StringComparison.Ordinal)
            && e.Contains("library \"integer:1|group:,|integer:00|group:,|integer:000", StringComparison.Ordinal));
    }

    /// <summary>
    /// Sub-area (c)'s population keeps every dataset input and each vector's own call (decision D22): the numbers program's
    /// cases, values and ranges, the lists, relative-times and display-names programs' formats and inputs, and the calls of the
    /// four services every numbers-and-text vector whose script is one call of literals makes.
    /// </summary>
    [Fact]
    public void Sub_area_c_s_population_keeps_every_dataset_input_and_each_vector_s_own_call()
    {
        var population = NumberTextPopulation.Build(Embedding.Both().First(), new SeededRandom(DifferentialCheck.DefaultSeed), 1);
        Assert.Equal(56, population.DatasetNumberFormats.Count);
        Assert.Contains(population.DatasetValues, static v => v.Text == "123456789012345678901234567890.5");
        Assert.NotEmpty(population.DatasetRanges);
        Assert.Equal(24, population.DatasetRelativeTimeFormats.Count);
        Assert.Equal(96, population.DatasetRelativeTimes.Count);
        Assert.Contains(population.Lists, static l => l.SequenceEqual(["Anna", "", "Carl", "Dora", "Emil"]));
        Assert.Contains("sl-rozaj-biske", population.Codes["language"]);
        Assert.Contains("abcdefghi", population.Codes["calendar"]);

        var vectors = Population.IntlVectors("numbers-and-text.vectors");
        var called = population.VectorCalls.Select(static c => c.Id).ToHashSet(StringComparer.Ordinal);
        var formats = vectors.Where(static v => v["op"] is "Intl.NumberFormat.prototype.format" or "Intl.ListFormat.prototype.format"
            or "Intl.RelativeTimeFormat.prototype.format" or "Intl.DisplayNames.prototype.of" or "Intl.NumberFormat.prototype.formatRange").ToList();
        var missed = formats.Where(v => !called.Contains(v["id"]!)).Select(static v => v.Script).ToList();
        Assert.True(missed.Count <= 3, string.Join("\n", missed));
    }

    /// <summary>
    /// Sub-area (d) (stage SJL-9.3d): Broiler.VM's date-time code answers as the library does over Broiler.VM's embedding but for
    /// row Z8, the resolved zone's identifier as given where Broiler.VM reported its primary (decision D15), and over Broiler.JS's
    /// every other difference is one the library's answer under Broiler.VM's reading of the data removes - the data locale's
    /// language and Broiler.VM's numbering systems (decisions D5 and D13). No formatted String, part or charge differs over
    /// Broiler.VM's embedding.
    /// </summary>
    [Fact]
    public void Broiler_VM_s_date_time_code_and_the_library_differ_only_by_row_Z8_and_the_data_they_read()
    {
        var sections = DifferentialCheck.Run(DifferentialCheck.DefaultSeed, scale: 1, only: ["T1", "T2", "T3", "T4", "T5"]);
        var report = DifferentialCheck.Report(sections, DifferentialCheck.DefaultSeed, 1);
        Assert.Equal(10, sections.Count);
        Assert.True(sections.All(static s => s.Unclassified == 0), report);
        Assert.All(sections, static s => Assert.True(s.Compared > 300, s.Name + " compared almost nothing"));
        var vm = sections.Where(static s => s.Name.EndsWith("(broiler-vm)", StringComparison.Ordinal)).ToList();
        Assert.All(vm, static s => Assert.True(s.ClassifiedByRow.Keys.All(static r => r == DateTimeSection.ZoneRow), s.Name));
        Assert.All(vm.Where(static s => s.Name[..2] is "T2" or "T3" or "T4"), static s => Assert.Equal(0, s.Classified));
        Assert.True(vm.Single(static s => s.Name.StartsWith("T5", StringComparison.Ordinal)).Classified > 0, report);
        var rows = sections.SelectMany(static s => s.ClassifiedByRow.Keys).Distinct().Order(StringComparer.Ordinal).ToList();
        Assert.Equal([DateTimeSection.DataRow, DateTimeSection.ZoneRow], rows);
    }

    /// <summary>
    /// The negative control of sub-area (d): the library reads Broiler.VM's tables with English's wide January changed, and the
    /// difference is found and left unclassified, since neither reading of the same tables gives Broiler.VM's answer.
    /// </summary>
    [Fact]
    public void A_difference_in_a_month_name_is_found_and_left_unclassified()
    {
        var vm = Embedding.Both().First();
        var dates = Encoding.ASCII.GetString(vm.Text[nameof(IntlTable.DateLocales)]);
        Assert.Contains("en|months.format.wide.1|January\n", dates, StringComparison.Ordinal);
        var changed = Encoding.ASCII.GetBytes(dates.Replace("en|months.format.wide.1|January\n", "en|months.format.wide.1|Janvier\n", StringComparison.Ordinal));
        var control = Embedding.WithLibraryTables("control", new Dictionary<string, byte[]> { [nameof(IntlTable.DateLocales)] = changed });
        var section = Assert.Single(DateTimeSection.Run(control, new SeededRandom(DifferentialCheck.DefaultSeed), 1, ["T2"]));
        Assert.True(section.Unclassified > 0);
        Assert.Equal(0, section.Classified);
        Assert.Contains(section.Examples, static e => e.Contains("January", StringComparison.Ordinal) && e.Contains("Janvier", StringComparison.Ordinal));
    }

    /// <summary>
    /// Sub-area (d)'s population keeps every dataset input and each vector's own format (decision D22): the dates program's cases,
    /// instants and ranges, the calendar-formats program's calendars and dates, the temporal-intl program's formats and values, the
    /// zone-names program's zones, styles and instants, the database's names, and the format each date-times vector constructs.
    /// </summary>
    [Fact]
    public void Sub_area_d_s_population_keeps_every_dataset_input_and_each_vector_s_own_format()
    {
        var population = DateTimePopulation.Build(Embedding.Both().First(), new SeededRandom(DifferentialCheck.DefaultSeed), 1, HomeTimeZones.Current);
        Assert.Contains(population.DatasetCases, static c => c.Format.Locale == "en" && c.Format.Bag["dateStyle"] is "full" && c.Times.Contains(8.64e15) && c.Ranges.Count == 6);
        Assert.Contains(population.DatasetCases, static c => c.Format.Bag["calendar"] is "chinese" && c.Times.Count == 13);
        Assert.Contains(population.DatasetCases, static c => c.Format.Bag["timeZone"] is "Antarctica/Troll" && c.Format.Bag["timeZoneName"] is "longGeneric" && c.Times.Count == 3);
        Assert.Equal(72, population.TemporalFormats.Count);
        Assert.Contains(population.TemporalValues, static v => v.Kind == "zoned" && v.Zone == "Europe/Berlin");
        Assert.Contains("Asia/Calcutta", population.Zones);
        var datasetZones = DateTimePopulation.TimeZoneDatasetNames();
        Assert.Equal(597, datasetZones.Count);
        Assert.Contains("Africa/Asmera", datasetZones);
        Assert.Contains("America/Knox_IN", datasetZones);
        Assert.All(datasetZones, z => Assert.Contains(z, population.Zones));
        Assert.All(HomeTimeZones.Current.PrimaryIdentifiers, z => Assert.Contains(z, population.Zones));
        Assert.Contains(population.TemporalValues, static v => v.Kind == "date" && v.Date == new IsoDate(-271821, 4, 19));
        Assert.Contains(population.TemporalValues, static v => v.Kind == "year-month" && v.Date == new IsoDate(-271821, 4, 1));
        Assert.Contains(population.TemporalRanges, static r => r.Start.Kind == "date-time" && r.End.Date == new IsoDate(275760, 9, 13));

        var vectors = Population.IntlVectors("date-times.vectors");
        var constructing = vectors.Count(static v => v.Script.Contains("new Intl.DateTimeFormat(", StringComparison.Ordinal) || v.Script.Contains(").toLocale", StringComparison.Ordinal));
        Assert.True(population.VectorCases.Count >= constructing - 10, population.VectorCases.Count + " of " + constructing);
        Assert.Contains(population.VectorCases, static c => c.Format.Locale == "de" && c.Format.Bag["timeZone"] is "America/New_York" && c.Times.Contains(1704467229123));
    }

    [Fact]
    public void Row_Z8_covers_a_difference_only_when_the_primary_identifier_gives_Broiler_VM_s_answer()
    {
        var zones = new Section("T5 the time zone's resolution (test)");
        zones.Compare("zone \"Asia/Calcutta\" (Z8's reading: \"Asia/Kolkata 0 Named rules\") (Broiler.VM's reading: \"Asia/Kolkata 0 Named rules\")",
            "Asia/Kolkata 0 Named rules", "Asia/Calcutta 0 Named rules");
        Assert.Equal(1, zones.ClassifiedByRow[DateTimeSection.ZoneRow]);

        // Another primary, or a difference neither reading removes: unclassified.
        zones.Compare("zone \"Asia/Calcutta\" (Z8's reading: \"Asia/Calcutta 0 Named rules\") (Broiler.VM's reading: \"Asia/Calcutta 0 Named rules\")",
            "Asia/Kolkata 0 Named rules", "Asia/Calcutta 0 Named rules");
        Assert.Equal(1, zones.Unclassified);
    }

    [Fact]
    public void Row_P4_covers_a_difference_only_when_the_exponent_at_zero_gives_Broiler_VM_s_answer()
    {
        var plurals = new Section("P1 plural rules (test)");
        plurals.Compare("select \"fr\" {notation:\"compact\"} 1000000 (c=e=0: many)", "many | 1000000", "many | 1000000");
        plurals.Compare("select \"fr\" {notation:\"compact\"} 2000000 (c=e=0: other)", "other | 2000000", "many | 2000000");
        Assert.Equal(1, plurals.ClassifiedByRow["P4"]);

        // Another answer at zero, another String, or no compact notation: unclassified.
        plurals.Compare("select \"fr\" {notation:\"compact\"} 2000000 (c=e=0: many)", "other | 2000000", "many | 2000000");
        plurals.Compare("select \"fr\" {notation:\"compact\"} 2000000 (c=e=0: other)", "other | 2000000", "many | 2000000.0");
        plurals.Compare("select \"fr\" {} 2000000", "other | 2000000", "many | 2000000");
        Assert.Equal(3, plurals.Unclassified);
    }

    [Fact]
    public void A_difference_in_a_plural_rule_is_found_and_left_unclassified()
    {
        // The negative control of sub-area (b): the library reads Broiler.VM's tables with English's "one" rule changed.
        var vm = Embedding.Both().First();
        var plurals = Encoding.ASCII.GetString(vm.Text[nameof(IntlTable.Plurals)]);
        Assert.Contains("en|one|i = 1 and v = 0\n", plurals, StringComparison.Ordinal);
        var changed = Encoding.ASCII.GetBytes(plurals.Replace("en|one|i = 1 and v = 0\n", "en|one|i = 1\n", StringComparison.Ordinal));

        var control = Embedding.WithLibraryTables("control", new Dictionary<string, byte[]> { [nameof(IntlTable.Plurals)] = changed });
        var sections = DecimalSection.Run(control, new SeededRandom(DifferentialCheck.DefaultSeed), 1, ["P1"]).ToList();

        var section = Assert.Single(sections);
        Assert.True(section.Unclassified > 0);
        Assert.Contains(section.Examples, static e => e.Contains("vm \"other | 1.", StringComparison.Ordinal) && e.Contains("library \"one | 1.", StringComparison.Ordinal));
    }

    /// <summary>
    /// Sub-area (e) (stage SJL-9.3e): Broiler.VM's duration format answers as the library does over Broiler.VM's embedding - every
    /// resolved option, formatted part and charge - and over Broiler.JS's every difference is one the library's answer under
    /// Broiler.VM's reading of the data removes: the data locale's language, Broiler.VM's numbering systems and no own symbols
    /// (decisions D5, D13, D21 and D34; rows U13 to U16).
    /// </summary>
    [Fact]
    public void Broiler_VM_s_duration_code_and_the_library_differ_only_by_the_data_they_read()
    {
        var sections = DifferentialCheck.Run(DifferentialCheck.DefaultSeed, scale: 1, only: ["U1", "U2"]);
        var report = DifferentialCheck.Report(sections, DifferentialCheck.DefaultSeed, 1);
        Assert.Equal(4, sections.Count);
        Assert.True(sections.All(static s => s.Unclassified == 0), report);
        Assert.All(sections, static s => Assert.True(s.Compared > 250, s.Name + " compared almost nothing"));
        Assert.All(sections.Where(static s => s.Name.EndsWith("(broiler-vm)", StringComparison.Ordinal)), static s => Assert.Equal(0, s.Classified));
        var rows = sections.SelectMany(static s => s.ClassifiedByRow.Keys).Distinct().ToList();
        Assert.Equal([DurationSection.DataRow], rows);
    }

    /// <summary>
    /// The negative control of sub-area (e): the library reads Broiler.VM's tables with English's time separator changed, and the
    /// difference is found in the clock and left unclassified, since Broiler.VM's reading of the same tables gives the library's
    /// answer.
    /// </summary>
    [Fact]
    public void A_difference_in_the_time_separator_is_found_and_left_unclassified()
    {
        var vm = Embedding.Both().First();
        var numbers = Encoding.ASCII.GetString(vm.Text[nameof(IntlTable.NumberLocales)]);
        Assert.Contains("en|symbols.timeSeparator|:", numbers, StringComparison.Ordinal);
        var changed = Encoding.ASCII.GetBytes(numbers.Replace("en|symbols.timeSeparator|:", "en|symbols.timeSeparator|.", StringComparison.Ordinal));
        var control = Embedding.WithLibraryTables("control", new Dictionary<string, byte[]> { [nameof(IntlTable.NumberLocales)] = changed });
        var section = Assert.Single(DurationSection.Run(control, new SeededRandom(DifferentialCheck.DefaultSeed), 1, ["U2"]));
        Assert.True(section.Unclassified > 0);
        Assert.Equal(0, section.Classified);
        Assert.Contains(section.Examples, static e => e.Contains("literal:::-", StringComparison.Ordinal) && e.Contains("literal:.:-", StringComparison.Ordinal));
    }

    /// <summary>
    /// Sub-area (e)'s population keeps every dataset input and each vector's own format (decision D22): the durations program's
    /// two locales with its fifteen formats, its error group's formats and its sixteen durations with the valid ones of its error
    /// group, and the format and durations of each durations vector whose script constructs one.
    /// </summary>
    [Fact]
    public void Sub_area_e_s_population_keeps_every_dataset_input_and_each_vector_s_own_format()
    {
        var population = DurationPopulation.Build(Embedding.Both().First(), new SeededRandom(DifferentialCheck.DefaultSeed), 1);
        Assert.Equal(30 + 5, population.DatasetFormats.Count);
        Assert.Contains(population.DatasetFormats, static f => f.Locale == "de" && f.Bag["numberingSystem"] is "arab");
        Assert.Contains(population.DatasetFormats, static f => f.Bag["fractionalDigits"] is 10.0);
        Assert.Equal(16 + 1, population.DatasetDurations.Count);
        Assert.Contains(population.DatasetDurations, static d => d.Minutes == -5 && d.Seconds == 0);
        Assert.Contains(population.DatasetDurations, static d => d.Hours == 1 && d.Minutes == 0 && d.Years == 0);

        var vectors = Population.IntlVectors("durations.vectors");
        var constructing = vectors.Count(static v => v.Script.Contains("new Intl.DurationFormat(", StringComparison.Ordinal) || v.Script.Contains(").toLocaleString(", StringComparison.Ordinal));
        Assert.Equal(constructing, population.VectorCalls.Count);
        Assert.Contains(population.VectorCalls, static c => c.Locale == "en" && c.Durations.Any(static d => d.Hours == 1 && d.Minutes == 30 && d.Seconds == 0));
        Assert.Contains(population.VectorCalls, static c => c.Id.StartsWith("intl/durations/unit-options/", StringComparison.Ordinal) && c.Bag["minutes"] is "numeric"
            && c.Durations.Any(static d => d.Minutes == 5 && d.Hours == 0));
        Assert.Contains(population.VectorCalls, static c => c.Durations.Any(static d => d.Years == 1 && d.Nanoseconds == 10 && d.Seconds == 7));
        Assert.Equal(400, population.RandomDurations.Count);
    }

    /// <summary>
    /// Milestone SJL-17.3's sections: over Broiler.VM's tables the collator and its locale data answer as Broiler.VM's, order and
    /// charge alike, with no difference; over Broiler.JS's, every difference is row O16's, Thai's reordering and the traditional
    /// collations of <c>es</c> and <c>sv</c>, which Broiler.VM's code does not read.
    /// </summary>
    [Fact]
    public void Broiler_VM_s_collator_and_the_library_differ_only_by_the_tailorings_Broiler_JS_adds()
    {
        var sections = DifferentialCheck.Run(DifferentialCheck.DefaultSeed, scale: 1, only: ["C1", "C2"]);
        var report = DifferentialCheck.Report(sections, DifferentialCheck.DefaultSeed, 1);
        Assert.Equal(4, sections.Count);
        Assert.True(sections.All(static s => s.Unclassified == 0), report);
        Assert.All(sections, static s => Assert.True(s.Compared > 90, s.Name + " compared almost nothing"));
        var vm = sections.Where(static s => s.Name.EndsWith("(broiler-vm)", StringComparison.Ordinal)).ToList();
        Assert.All(vm, static s => Assert.Equal(0, s.Classified));
        Assert.True(sections.Single(static s => s.Name.StartsWith("C1", StringComparison.Ordinal) && s.Name.EndsWith("(broiler-vm)", StringComparison.Ordinal)).Compared > 100_000, report);
        var js = sections.Where(static s => s.Name.EndsWith("(broiler-js)", StringComparison.Ordinal)).ToList();
        Assert.All(js, static s => Assert.True(s.Classified > 0, s.Name));
        Assert.Equal([CollationSection.DataRow], sections.SelectMany(static s => s.ClassifiedByRow.Keys).Distinct());
    }

    /// <summary>
    /// The negative control of milestone SJL-17.3: the library reads Broiler.VM's tables with German phonebook's tailoring renamed,
    /// so that the arms read the tailoring under different names and one of them collates by the root, and the difference is found and left
    /// unclassified.
    /// </summary>
    [Fact]
    public void A_difference_in_a_tailoring_is_found_and_left_unclassified()
    {
        var vm = Embedding.Both().First();
        var tailorings = (byte[])vm.Text[nameof(IntlTable.CollationTailorings)].Clone();
        var name = Encoding.ASCII.GetBytes("de-phonebk");
        var at = tailorings.AsSpan().IndexOf(name);
        Assert.True(at > 0);
        tailorings[at] = (byte)'x';
        var control = Embedding.WithLibraryTables("control", new Dictionary<string, byte[]> { [nameof(IntlTable.CollationTailorings)] = tailorings });
        var section = Assert.Single(CollationSection.Run(control, new SeededRandom(DifferentialCheck.DefaultSeed), 1, ["C1"]));
        Assert.True(section.Unclassified > 0);
        Assert.Equal(0, section.Classified);
        Assert.Contains(section.Examples, static e => e.Contains("xe-phonebk", StringComparison.Ordinal));
    }

    [Fact]
    public void The_seeded_random_inputs_are_the_same_on_every_run()
    {
        var a = new SeededRandom(7);
        var b = new SeededRandom(7);
        Assert.Equal(Enumerable.Range(0, 32).Select(_ => a.NextUInt64()), Enumerable.Range(0, 32).Select(_ => b.NextUInt64()));
        Assert.Equal(0xE220A8397B1DCDAFUL, new SeededRandom(0).NextUInt64());
    }

    [Fact]
    public void The_population_s_other_sources_are_readable()
    {
        Assert.NotEmpty(Population.IntlVectors("locales.vectors"));
        Assert.Contains(Population.DatasetPrograms(), static p => p.Name == "locales");
        Assert.Contains(Population.DatasetPrograms(), static p => p.Name == "plurals");
        Assert.Contains(Population.DatasetPrograms(), static p => p.Name == "numbers");
    }
}
