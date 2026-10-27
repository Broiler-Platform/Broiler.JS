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
        var sections = DifferentialCheck.Run(DifferentialCheck.DefaultSeed, scale: 1);
        var report = DifferentialCheck.Report(sections, DifferentialCheck.DefaultSeed, 1);

        Assert.Equal(8, sections.Count);
        Assert.All(sections, static s => Assert.True(s.Compared > 1000, s.Name + " compared almost nothing"));
        Assert.True(sections.All(static s => s.Unclassified == 0), report);
        Assert.All(sections, static s => Assert.Equal(0, s.Classified));
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
