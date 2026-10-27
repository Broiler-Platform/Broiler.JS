using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Broiler.ECMAScript.Conformance;
using Broiler.ECMAScript.Generation.Intl;
using Broiler.ECMAScript.Runners;

namespace Broiler.ECMAScript.Intl.Tests.Differential;

/// <summary>
/// The differential check of Broiler.VM's JSD-0082, decision D22: Broiler.VM's code at its SJL-9 rule commit, compiled into
/// this test project from the pinned copies in <c>Vm/</c>, against the library, over a declared population - every Intl
/// vector, every dataset input as a library call, and seeded random inputs - with every difference either a row of
/// Broiler.VM's correspondence (<see cref="KnownDifference"/>) or reported as unclassified. Test-only and JIT only: it runs
/// in this project's tests at a small scale and, at any scale, as the assembly's entry point (<see cref="Program"/>), as
/// SJL-8.3's and SJL-10.3's checks ran.
/// </summary>
/// <remarks>
/// <b>Sections.</b> Each stage of milestone SJL-9.3 adds the sections of its sub-area; the skeleton's own is D0, the table
/// decoders (<see cref="DecoderSection"/>), and sub-area (a)'s are L1 to L4, locale identifiers, negotiation, Intl.Locale
/// and <c>Intl.supportedValuesOf</c> (<see cref="LocaleSection"/>). A section names what it compares, and an answer is rendered as text so that
/// the comparison is exact: a string as it is, a missing answer as <c>&lt;none&gt;</c>, a record by its fields.
/// </remarks>
internal static class DifferentialCheck
{
    /// <summary>Broiler.VM's commit the copies in <c>Vm/</c> come from: its SJL-9 rule commit.</summary>
    internal const string VmCommit = "4cf9ca449fc96a1a5be810f6d0ca31c42b855c8f";

    /// <summary>The seed a run takes when none is given.</summary>
    internal const ulong DefaultSeed = 20261027;

    /// <summary>The differences Broiler.VM's correspondence classes, by section; a difference matched by none is unclassified.</summary>
    /// <remarks>
    /// None for the skeleton's section: the decoders are a port with no correction. A stage that ports a row the
    /// correspondence classes a correction of Broiler.VM (L9, L18, Z8), or whose answer is each engine's own data, adds the
    /// row here, by the inputs it names, never a new row: sub-area (a)'s are L9, L18 and L22 (<see cref="LocaleSection.Known"/>).
    /// </remarks>
    internal static IReadOnlyList<KnownDifference> Known { get; } = [.. LocaleSection.Known()];

    /// <summary>Runs the sections named (all when none is), at a scale, from a seed.</summary>
    internal static IReadOnlyList<Section> Run(ulong seed, int scale, IReadOnlyCollection<string>? only = null)
    {
        var random = new SeededRandom(seed);
        var sections = new List<Section>();

        foreach (var embedding in Embedding.Both())
        {
            if (only is null || only.Contains("D0"))
                sections.AddRange(DecoderSection.Run(embedding, random, scale));
            if (only is null || only.Any(static s => s is "L1" or "L2" or "L3" or "L4"))
                sections.AddRange(LocaleSection.Run(embedding, random, scale, only));
        }

        return sections;
    }

    /// <summary>The report of a run: the population it read, and each section's counts and unclassified examples.</summary>
    internal static string Report(IReadOnlyList<Section> sections, ulong seed, int scale)
    {
        var text = new StringBuilder();
        text.Append("SJL-9.3 differential check: Broiler.VM's code at ").Append(VmCommit)
            .Append(" (the pinned copies of Broiler.ECMAScript.Intl.Tests/Differential/Vm/) against Broiler.ECMAScript.Intl\n");
        text.Append("seed ").Append(seed.ToString(CultureInfo.InvariantCulture)).Append(", scale ").Append(scale.ToString(CultureInfo.InvariantCulture)).Append('\n');
        text.Append('\n');
        text.Append(string.Format(CultureInfo.InvariantCulture, "{0,-72} {1,12} {2,12} {3,12}\n", "Section", "Compared", "Classified", "Unclassified"));

        foreach (var section in sections)
        {
            text.Append(string.Format(CultureInfo.InvariantCulture, "{0,-72} {1,12:N0} {2,12:N0} {3,12:N0}\n",
                section.Name, section.Compared, section.Classified, section.Unclassified));
        }

        foreach (var section in sections.Where(static s => s.Examples.Count > 0))
        {
            text.Append('\n').Append(section.Name).Append(": unclassified differences, the first of them\n");
            foreach (var example in section.Examples)
                text.Append("  ").Append(example).Append('\n');
        }

        foreach (var section in sections.Where(static s => s.ClassifiedByRow.Count > 0))
        {
            text.Append('\n').Append(section.Name).Append(": classified differences by row\n");
            foreach (var (row, count) in section.ClassifiedByRow.OrderBy(static p => p.Key, StringComparer.Ordinal))
            {
                text.Append("  ").Append(row).Append(' ').Append(count.ToString(CultureInfo.InvariantCulture)).Append(", the first of them:\n");
                foreach (var example in section.ClassifiedExamples[row])
                    text.Append("    ").Append(example).Append('\n');
            }
        }

        return text.ToString();
    }
}

/// <summary>A difference Broiler.VM's correspondence classes: the section it is in, the inputs it covers and its row.</summary>
/// <param name="Section">The section's name, as <see cref="Section.Name"/> begins.</param>
/// <param name="Row">The correspondence's row, for example <c>L9</c>.</param>
/// <param name="Covers">Whether a compared input, with each arm's answer, is the row's.</param>
internal sealed record KnownDifference(string Section, string Row, Func<string, string, string, bool> Covers);

/// <summary>One section of a run: what it compared and how each difference was classed.</summary>
internal sealed class Section
{
    private const int ExampleLimit = 20;

    private const int ClassifiedExampleLimit = 3;

    internal Section(string name) => Name = name;

    internal string Name { get; }

    internal long Compared { get; private set; }

    internal long Classified { get; private set; }

    internal long Unclassified { get; private set; }

    internal List<string> Examples { get; } = [];

    internal Dictionary<string, long> ClassifiedByRow { get; } = new(StringComparer.Ordinal);

    /// <summary>The first few classified differences of each row, so that a reader can see that the row is the difference.</summary>
    internal Dictionary<string, List<string>> ClassifiedExamples { get; } = new(StringComparer.Ordinal);

    /// <summary>Compares one input's answers: Broiler.VM's and the library's, each rendered as text.</summary>
    internal void Compare(string input, string? vm, string? library)
    {
        Compared++;
        vm ??= "<none>";
        library ??= "<none>";

        if (string.Equals(vm, library, StringComparison.Ordinal))
            return;

        foreach (var known in DifferentialCheck.Known)
        {
            if (Name.StartsWith(known.Section, StringComparison.Ordinal) && known.Covers(input, vm, library))
            {
                Classified++;
                ClassifiedByRow[known.Row] = ClassifiedByRow.GetValueOrDefault(known.Row) + 1;
                if (!ClassifiedExamples.TryGetValue(known.Row, out var examples))
                    ClassifiedExamples[known.Row] = examples = [];
                if (examples.Count < ClassifiedExampleLimit)
                    examples.Add(input + " => vm " + Quote(vm) + ", library " + Quote(library));
                return;
            }
        }

        Unclassified++;
        if (Examples.Count < ExampleLimit)
            Examples.Add(input + " => vm " + Quote(vm) + ", library " + Quote(library));
    }

    private static string Quote(string text)
    {
        var quoted = new StringBuilder("\"");
        foreach (var c in text)
        {
            if (c is >= ' ' and <= '~' and not '"' and not '\\')
                quoted.Append(c);
            else
                quoted.Append("\\u").Append(((int)c).ToString("X4", CultureInfo.InvariantCulture));
        }

        return quoted.Append('"').ToString();
    }
}

/// <summary>A seeded source of random inputs, SplitMix64, the same on every machine and runtime.</summary>
internal sealed class SeededRandom(ulong seed)
{
    private ulong state = seed;

    internal ulong NextUInt64()
    {
        var z = state += 0x9E3779B97F4A7C15UL;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }

    /// <summary>A number from 0 up to, not including, <paramref name="limit"/>.</summary>
    internal int Next(int limit) => (int)(NextUInt64() % (ulong)limit);

    /// <summary>A string of <paramref name="minimum"/> to <paramref name="maximum"/> characters drawn from <paramref name="alphabet"/>.</summary>
    internal string NextString(string alphabet, int minimum, int maximum)
    {
        var length = minimum + Next(maximum - minimum + 1);
        var text = new StringBuilder(length);
        for (var i = 0; i < length; i++)
            text.Append(alphabet[Next(alphabet.Length)]);
        return text.ToString();
    }

    /// <summary>One element of a list.</summary>
    internal T Pick<T>(IReadOnlyList<T> list) => list[Next(list.Count)];
}

/// <summary>One embedding's tables, handed to both arms: Broiler.VM's code through its <c>IJsIntlData</c>, the library through <see cref="IIntlDataSet"/>.</summary>
internal sealed class Embedding
{
    private Embedding(string name, IntlTables tables, IReadOnlyDictionary<string, byte[]>? libraryOverrides = null)
    {
        Name = name;
        var bytes = tables.Tables.ToDictionary(static t => t.Name, static t => t.Data.ToArray(), StringComparer.Ordinal);
        Vm = new VmData(tables.CldrVersion, bytes);
        var libraryBytes = new Dictionary<string, byte[]>(bytes, StringComparer.Ordinal);
        foreach (var (table, data) in libraryOverrides ?? new Dictionary<string, byte[]>())
            libraryBytes[table] = data;
        Library = new LibraryData(tables.CldrVersion, libraryBytes);
        Text = bytes;
    }

    /// <summary><c>broiler-vm</c> or <c>broiler-js</c>: whose list the tables were generated for.</summary>
    internal string Name { get; }

    /// <summary>The tables as Broiler.VM's code reads them.</summary>
    internal Vm.IJsIntlData Vm { get; }

    /// <summary>The tables as the library reads them.</summary>
    internal IIntlDataSet Library { get; }

    /// <summary>The tables' bytes by name, for the population a section reads from them.</summary>
    internal IReadOnlyDictionary<string, byte[]> Text { get; }

    /// <summary>The lines of a text table, as the population reads them.</summary>
    internal IReadOnlyList<string> Lines(IntlTable table) =>
        Text.TryGetValue(table.ToString(), out var bytes)
            ? Encoding.ASCII.GetString(bytes).Split('\n').Where(static l => l.Length != 0).ToList()
            : [];

    private static readonly Lazy<Embedding> BroilerVm = new(static () => new Embedding("broiler-vm", IntlDataHome.GenerateBroilerVm(IntlDataHome.Current)));

    private static readonly Lazy<Embedding> BroilerJs = new(static () => new Embedding("broiler-js", IntlDataHome.GenerateBroilerJs(IntlDataHome.Current)));

    /// <summary>
    /// Both embeddings' tables, as the data home's generator writes them from the home's pins: Broiler.VM's list, whose source
    /// form is Broiler.VM's <c>JsCldrTables.g.cs</c> byte for byte (the generator's own tests hold it), and Broiler.JS's, which
    /// rule SL7 holds to <c>data/intl/generated/</c>.
    /// </summary>
    internal static IEnumerable<Embedding> Both()
    {
        yield return BroilerVm.Value;
        yield return BroilerJs.Value;
    }

    /// <summary>Broiler.VM's tables, with some of the library's replaced: the negative control's input.</summary>
    internal static Embedding WithLibraryTables(string name, IReadOnlyDictionary<string, byte[]> overrides)
    {
        var tables = IntlDataHome.GenerateBroilerVm(IntlDataHome.Current);
        return new Embedding(name, tables, overrides);
    }

    private sealed class VmData(string cldrVersion, Dictionary<string, byte[]> tables) : Vm.IJsIntlData
    {
        public string CldrVersion { get; } = cldrVersion;

        public ReadOnlySpan<byte> Table(Vm.JsIntlTable table) => tables.TryGetValue(table.ToString(), out var bytes) ? bytes : default;
    }

    private sealed class LibraryData(string cldrVersion, Dictionary<string, byte[]> tables) : IIntlDataSet
    {
        public string CldrVersion { get; } = cldrVersion;

        public ReadOnlySpan<byte> Table(IntlTable table) => tables.TryGetValue(table.ToString(), out var bytes) ? bytes : default;
    }
}

/// <summary>
/// The population's other sources (decision D22): the Intl vectors and the reference datasets, as the stages that compare
/// algorithms read them. The decoder section reads the tables themselves.
/// </summary>
internal static class Population
{
    /// <summary>The records of one Intl vector file, <c>conformance/vectors/intl/&lt;file&gt;</c>, for example <c>locales.vectors</c>.</summary>
    internal static IReadOnlyList<VectorRecord> IntlVectors(string file)
    {
        var root = CorpusDirectory.Home(AppContext.BaseDirectory);
        var name = "intl/" + file;
        var read = VectorReader.Read(File.ReadAllBytes(Path.Combine(root, "intl", file)), name);
        if (read.Diagnostics.Count > 0)
            throw new InvalidDataException(name + ": " + read.Diagnostics[0]);
        return read.Records;
    }

    /// <summary>The reference datasets' programs, <c>conformance/datasets/intl/&lt;name&gt;.js</c>, by name, in ordinal order.</summary>
    internal static IReadOnlyList<(string Name, string Program)> DatasetPrograms()
    {
        var home = Path.Combine(Path.GetDirectoryName(CorpusDirectory.Home(AppContext.BaseDirectory))!, "datasets", "intl");
        return Directory.EnumerateFiles(home, "*.js")
            .Order(StringComparer.Ordinal)
            .Select(static path => (Path.GetFileNameWithoutExtension(path), File.ReadAllText(path)))
            .ToList();
    }
}
