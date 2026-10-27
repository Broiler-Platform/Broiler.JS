using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    }
}
