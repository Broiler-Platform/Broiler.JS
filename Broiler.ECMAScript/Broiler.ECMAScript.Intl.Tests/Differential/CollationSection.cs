using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Broiler.ECMAScript.Conformance;
using Broiler.ECMAScript.Intl.Tests.Differential.Vm;
using Broiler.ECMAScript.Runners;

namespace Broiler.ECMAScript.Intl.Tests.Differential;

/// <summary>
/// Milestone SJL-17.3's sections of the differential check (Broiler.VM's JSD-0084, decision E9 and its plan's task 17.3):
/// Broiler.VM's <c>JsCollator</c> and <c>JsCollationData</c> (pinned copies, <c>Vm/</c>) against the library's
/// <see cref="UcaCollator"/> and <see cref="UcaCollationData"/>, over each embedding's tables.
/// </summary>
/// <remarks>
/// <para>
/// <b>C1, the comparison with its charge.</b> Each input is a pair of strings, a tailoring of the embedding (or the root) and
/// one of the settings an <c>Intl.Collator</c>'s resolved options map onto - strength, case level, case order, the shifted
/// alternate, numeric collation. Both arms compare the pair, and the answer is the order and the units charged: Broiler.VM's
/// engine's meter, through NFD as <c>normalize</c> charges it and each element of both strings, against the library's
/// <see cref="WorkCharge"/> (decision E7, no C2).
/// </para>
/// <para>
/// <b>The population</b>, as decision D22 builds the others: every pair of neighbouring strings of each collation vector's
/// inputs (<c>conformance/vectors/collation/</c>), every pair of the <c>orderings</c> dataset's words
/// (<c>conformance/datasets/intl/orderings.js</c>), each under every tailoring and eight settings; and seeded random strings over
/// the code points the vectors reach, with combining marks and lone surrogates among them, each pair under a random tailoring
/// and settings, two thousand pairs per unit of scale.
/// </para>
/// <para>
/// <b>C2, the Collator's locale data</b>: Broiler.VM's <c>CollatorKeyData</c> against <see cref="UcaCollationData.KeyValues"/>,
/// for every language the vectors and the data name, both usages and the keys <c>co</c>, <c>kn</c>, <c>kf</c> and one more.
/// </para>
/// <para>
/// <b>The one row</b>: over Broiler.JS's tables, Thai's tailoring states <c>[reorder Thai]</c>, which the library reads from the
/// settings table Broiler.VM's code never reads and Broiler.VM's tables never carry; a difference there is row O16's, data policy
/// (C5), each engine's locale list (decision E3). Broiler.VM's <c>CollatorKeyData</c> names German's <c>phonebk</c> alone, which
/// the library reads from the tailorings the data carries; over Broiler.JS's tables, <c>es</c> and <c>sv</c> name <c>trad</c> too,
/// row O16 again.
/// </para>
/// </remarks>
internal static class CollationSection
{
    /// <summary>The row of a difference that Broiler.JS's wider list of tailorings makes.</summary>
    internal const string DataRow = "O16 data policy (C5): decision E3's tailorings";

    private const string ReorderMark = " [th-standard reorders Thai]";

    private const string ListMark = " [Broiler.JS's list]";

    private static readonly (int Strength, bool CaseLevel)[] Sensitivities = [(1, false), (2, false), (1, true), (3, false)];

    internal static IEnumerable<Section> Run(Embedding embedding, SeededRandom random, int scale, IReadOnlyCollection<string>? only)
    {
        var arms = new Arms(embedding);
        var suffix = " (" + embedding.Name + ")";

        if (only is null || only.Contains("C1"))
            yield return Comparison(arms, random, scale, "C1 the collator's comparison, with the charge: UCA's elements, NFD, the levels and the options" + suffix);
        if (only is null || only.Contains("C2"))
            yield return KeyData(arms, "C2 the Collator's locale data: CollatorKeyData's values for co, kn and kf" + suffix);
    }

    internal static IEnumerable<KnownDifference> Known()
    {
        yield return new KnownDifference("C1", DataRow, static (input, _, _) => input.EndsWith(ReorderMark, StringComparison.Ordinal));
        yield return new KnownDifference("C2", DataRow, static (input, _, _) => input.EndsWith(ListMark, StringComparison.Ordinal));
    }

    // ---- C1 --------------------------------------------------------------------------------------------------------------

    private static Section Comparison(Arms arms, SeededRandom random, int scale, string name)
    {
        var section = new Section(name);
        var tailorings = new List<string?> { null };
        tailorings.AddRange(arms.Library.TailoringNames);

        foreach (var (x, y) in Pairs())
        {
            foreach (var tailoring in tailorings)
            {
                for (var setting = 0; setting < 8; setting++)
                {
                    var (strength, caseLevel) = Sensitivities[setting % 4];
                    var shifted = setting >= 4;
                    var numeric = (x.Length + setting) % 3 == 0;
                    var caseFirst = (x.Length + y.Length + setting) % 3;
                    Compare(section, arms, x, y, tailoring, strength, caseLevel, caseFirst, shifted, numeric);
                }
            }
        }

        var alphabet = Alphabet();
        for (var i = 0; i < 2000 * scale; i++)
        {
            var x = RandomString(random, alphabet);
            var y = random.Next(4) == 0 ? x + RandomString(random, alphabet) : RandomString(random, alphabet);
            var (strength, caseLevel) = Sensitivities[random.Next(4)];
            Compare(section, arms, x, y, tailorings[random.Next(tailorings.Count)], strength, caseLevel, random.Next(3), random.Next(2) == 0, random.Next(2) == 0);
        }

        return section;
    }

    private static void Compare(Section section, Arms arms, string x, string y, string? tailoring, int strength, bool caseLevel, int caseFirst, bool shifted, bool numeric)
    {
        var input = string.Format(
            CultureInfo.InvariantCulture,
            "compare({0}, {1}) under {2}, strength {3}{4}, caseFirst {5}{6}{7}",
            LocaleSection.Quote(x), LocaleSection.Quote(y), tailoring ?? "the root", strength, caseLevel ? " with the case level" : string.Empty,
            caseFirst switch { 1 => "upper", 2 => "lower", _ => "off" }, shifted ? ", shifted" : string.Empty, numeric ? ", numeric" : string.Empty);

        var engine = arms.Engine;
        var before = engine.Charged;
        var vm = new JsCollator(arms.Vm, tailoring is null ? null : arms.Vm.TailoringNamed(tailoring), strength, caseLevel, (JsCaseFirst)caseFirst, shifted, numeric);
        var vmOrder = vm.Compare(engine, x, y);
        var vmAnswer = vmOrder.ToString(CultureInfo.InvariantCulture) + ", charged " + (engine.Charged - before).ToString(CultureInfo.InvariantCulture);

        ulong charged = 0;
        var options = new UcaCollatorOptions((CollationStrength)strength, caseLevel, (CollationCaseFirst)caseFirst, shifted, numeric, tailoring);
        var library = new UcaCollator(arms.Library, options, JsUnicode.Normalization);
        var status = library.Compare(x, y, JsRealm.StringLengthCeiling, units => charged += units, out var order);
        var libraryAnswer = status == IntlStatus.Completed
            ? order.ToString(CultureInfo.InvariantCulture) + ", charged " + charged.ToString(CultureInfo.InvariantCulture)
            : status.ToString();

        if (!string.Equals(vmAnswer, libraryAnswer, StringComparison.Ordinal) && tailoring is not null && arms.Library.Decoded.TailoringNamed(tailoring)?.Reorder is not null)
            input += ReorderMark;

        section.Compare(input, vmAnswer, libraryAnswer);
    }

    // ---- C2 --------------------------------------------------------------------------------------------------------------

    private static Section KeyData(Arms arms, string name)
    {
        var section = new Section(name);
        var languages = new SortedSet<string>(StringComparer.Ordinal) { "und", "en", "de", "es", "sv", "th", "fi", "zh", "ja", "tr", "da", "xx" };
        foreach (var tailoring in arms.Library.TailoringNames)
            languages.Add(tailoring[..tailoring.IndexOf('-', StringComparison.Ordinal)]);

        foreach (var language in languages)
        {
            foreach (var usage in new[] { "sort", "search" })
            {
                foreach (var key in new[] { "co", "kn", "kf", "nu" })
                {
                    var input = "CollatorKeyData(" + language + ", " + key + ", " + usage + ")";
                    var vm = Render(JsRealm.CollatorKeyDataForTheHarness(language, key, usage));
                    var library = Render(arms.Library.KeyValues(language, key, usage));
                    if (!string.Equals(vm, library, StringComparison.Ordinal) && arms.Embedding.Name == "broiler-js" && key == "co")
                        input += ListMark;
                    section.Compare(input, vm, library);
                }
            }
        }

        return section;
    }

    private static string Render(IReadOnlyList<string?> values) => "[" + string.Join(", ", values.Select(static v => v ?? "null")) + "]";

    // ---- the population ----------------------------------------------------------------------------------------------

    private static readonly Lazy<IReadOnlyList<(string X, string Y)>> FixedPairs = new(BuildPairs);

    private static IReadOnlyList<(string X, string Y)> Pairs() => FixedPairs.Value;

    private static IReadOnlyList<(string X, string Y)> BuildPairs()
    {
        var pairs = new List<(string, string)>();

        foreach (var vector in CollationVectors())
        {
            var strings = LibraryRun.Inputs(vector).Where(static v => v.Kind == TypedValueKind.String).Select(static v => v.Chars).ToList();
            for (var i = 1; i < strings.Count; i++)
                pairs.Add((strings[i - 1], strings[i]));
        }

        var words = OrderingsWords();
        for (var i = 0; i < words.Count; i++)
        {
            for (var j = i + 1; j < words.Count; j++)
                pairs.Add((words[i], words[j]));
        }

        return pairs;
    }

    private static readonly Lazy<string[]> AlphabetValue = new(BuildAlphabet);

    private static string[] Alphabet() => AlphabetValue.Value;

    /// <summary>The code points the vectors' strings reach, each as a string, with combining marks and lone surrogates added.</summary>
    private static string[] BuildAlphabet()
    {
        var set = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var (x, y) in Pairs())
        {
            foreach (var text in new[] { x, y })
            {
                for (var i = 0; i < text.Length; i++)
                {
                    var length = char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]) ? 2 : 1;
                    set.Add(text.Substring(i, length));
                    i += length - 1;
                }
            }
        }

        foreach (var extra in new[] { "\u0300", "\u0301", "\u0308", "\u030A", "\u0323", "\u0327", "\u0E4D", "\u0E32", "\uD800", "\uDC00", "0", "1", "9", "\u0661", "\uFF12" })
            set.Add(extra);

        return [.. set];
    }

    private static string RandomString(SeededRandom random, string[] alphabet)
    {
        var length = random.Next(7);
        var text = new StringBuilder();
        for (var i = 0; i < length; i++)
            text.Append(alphabet[random.Next(alphabet.Length)]);
        return text.ToString();
    }

    private static IReadOnlyList<VectorRecord> CollationVectors()
    {
        var root = Path.Combine(CorpusDirectory.Home(AppContext.BaseDirectory), "collation");
        var records = new List<VectorRecord>();
        foreach (var path in Directory.EnumerateFiles(root, "*.vectors").Order(StringComparer.Ordinal))
        {
            var read = VectorReader.Read(File.ReadAllBytes(path), "collation/" + Path.GetFileName(path));
            if (read.Diagnostics.Count > 0)
                throw new InvalidDataException(path + ": " + read.Diagnostics[0]);
            records.AddRange(read.Records);
        }

        return records;
    }

    /// <summary>The <c>orderings</c> dataset's words, its <c>var words = [...]</c> literals with their escapes resolved.</summary>
    private static IReadOnlyList<string> OrderingsWords()
    {
        var home = Path.Combine(Path.GetDirectoryName(CorpusDirectory.Home(AppContext.BaseDirectory))!, "datasets", "intl");
        var program = File.ReadAllText(Path.Combine(home, "orderings.js"));
        var start = program.IndexOf("var words = [", StringComparison.Ordinal);
        var end = program.IndexOf("];", start, StringComparison.Ordinal);
        var words = new List<string>();

        for (var at = start; at < end; at++)
        {
            if (program[at] != '\'')
                continue;

            var text = new StringBuilder();
            at++;
            while (program[at] != '\'')
            {
                if (program[at] == '\\' && program[at + 1] == 'u')
                {
                    text.Append((char)int.Parse(program.AsSpan(at + 2, 4), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture));
                    at += 6;
                    continue;
                }

                if (program[at] == '\\')
                    at++;
                text.Append(program[at++]);
            }

            words.Add(text.ToString());
        }

        if (words.Count < 50)
            throw new InvalidDataException("the orderings dataset's words were not read: " + words.Count);
        return words;
    }

    private sealed class Arms
    {
        internal Arms(Embedding embedding)
        {
            Embedding = embedding;
            Vm = new JsCollationData(embedding.Text["CollationRoot"], embedding.Text.TryGetValue("CollationTailorings", out var tailored) ? tailored : []);
            Library = new UcaCollationData(embedding.Library);
            Engine = new JsEngine(new VmIntl(new JsIntlTables(embedding.Vm), HomeTimeZones.Current));
        }

        internal Embedding Embedding { get; }

        internal JsCollationData Vm { get; }

        internal UcaCollationData Library { get; }

        internal JsEngine Engine { get; }
    }
}
