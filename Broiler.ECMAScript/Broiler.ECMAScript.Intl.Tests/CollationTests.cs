using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Broiler.ECMAScript.Runners;
using Broiler.Unicode.Algorithms;
using Xunit;

namespace Broiler.ECMAScript.Intl.Tests;

/// <summary>
/// The shared collator (milestone SJL-17.3; Broiler.VM's JSD-0084): UCA's conformance files at library level, the L4 oracle
/// (decision E9), every line in order; the data's tailorings, their settings and the choices an engine's Collator asks of
/// them; the charge; and the status that replaces Broiler.VM's error.
/// </summary>
public sealed class CollationTests
{
    private static readonly Lazy<UnicodeNormalization> Normalization = new(static () => new UnicodeNormalization(UnicodeDataHome.Tables));

    private static readonly UcaCollationData Js = new(HomeDataSet.Current);

    private static UcaCollator Collator(UcaCollationData data, CollationStrength strength = CollationStrength.Tertiary, bool shifted = false, string? tailoring = null, bool numeric = false, CollationCaseFirst caseFirst = CollationCaseFirst.Off, bool caseLevel = false) =>
        new(data, new UcaCollatorOptions(strength, caseLevel, caseFirst, shifted, numeric, tailoring), Normalization.Value);

    private static int Compare(UcaCollator collator, string x, string y, WorkCharge? charge = null)
    {
        Assert.Equal(IntlStatus.Completed, collator.Compare(x, y, int.MaxValue, charge, out var order));
        return order;
    }

    /// <summary>The lines of one of CLDR 48.2.0's archived CollationTest files, each as the string its code points spell.</summary>
    private static List<string> CollationTestLines(string file)
    {
        var path = Path.Combine(IntlDataHome.Current, "cldr", "cldr-48.2.0", "common", "uca", file);
        var lines = new List<string>();

        foreach (var raw in File.ReadAllLines(path))
        {
            var line = raw.Split('#')[0].Split(';')[0].Trim();

            if (line.Length == 0)
                continue;

            var text = new StringBuilder();
            foreach (var hex in line.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var codePoint = int.Parse(hex, NumberStyles.AllowHexSpecifier, NumberFormatInfo.InvariantInfo);
                if (codePoint > 0xFFFF)
                    text.Append(char.ConvertFromUtf32(codePoint));
                else
                    text.Append((char)codePoint);
            }

            lines.Add(text.ToString());
        }

        return lines;
    }

    /// <summary>
    /// UTS #10's conformance test for CLDR 48.2.0's root, as the slice compiler's checks and row O2 read it: every line of the file
    /// orders at or after the line before it, under <c>{sensitivity: 'variant'}</c> for the non-ignorable file and with the
    /// shifted alternate, <c>ignorePunctuation: true</c>, for the shifted one.
    /// </summary>
    [Theory]
    [InlineData("CollationTest_CLDR_NON_IGNORABLE_SHORT.txt", false)]
    [InlineData("CollationTest_CLDR_SHIFTED_SHORT.txt", true)]
    public void The_root_orders_every_line_of_UCA_s_conformance_files(string file, bool shifted)
    {
        var lines = CollationTestLines(file);
        Assert.True(lines.Count > 200_000, file + " holds " + lines.Count + " lines");
        var collator = Collator(Js, shifted: shifted);
        var out_of_order = new List<string>();

        for (var i = 1; i < lines.Count; i++)
        {
            if (Compare(collator, lines[i - 1], lines[i]) > 0)
                out_of_order.Add(i.ToString(CultureInfo.InvariantCulture));
        }

        Assert.True(out_of_order.Count == 0, out_of_order.Count + " pairs out of order, the first at lines " + string.Join(", ", out_of_order.Take(10)));
    }

    /// <summary>Broiler.VM's tables carry its three tailorings and no setting; Broiler.JS's carry decision E3's list and Thai's settings.</summary>
    [Fact]
    public void Each_embedding_carries_its_list()
    {
        var vm = new UcaCollationData(new GeneratedDataSet(IntlDataHome.GenerateBroilerVm(IntlDataHome.Current)));
        Assert.Equal(["und-search", "de-phonebk", "de-search"], vm.TailoringNames);
        Assert.All(vm.TailoringNames, name => Assert.False(vm.ShiftedByDefault(name)));
        Assert.Equal(
            ["und-search", "de-phonebk", "de-search", "es-standard", "es-search", "es-trad", "sv-standard", "sv-search", "sv-trad", "th-standard"],
            Js.TailoringNames);
        Assert.True(Js.ShiftedByDefault("th-standard"));
        Assert.False(Js.ShiftedByDefault("sv-standard"));
        Assert.False(Js.ShiftedByDefault(null));
        Assert.False(Js.ShiftedByDefault("xx-standard"));
    }

    /// <summary>
    /// NewCollator's choice of a tailoring, read from the data: over Broiler.VM's tables it is Broiler.VM's - German search, root
    /// search for every other search, German phonebook only when the resolved collation is <c>phonebk</c> - and over Broiler.JS's
    /// the same rule reaches the standard and traditional collations of <c>es</c>, <c>sv</c> and <c>th</c>.
    /// </summary>
    [Fact]
    public void The_tailoring_follows_the_language_usage_and_collation()
    {
        var vm = new UcaCollationData(new GeneratedDataSet(IntlDataHome.GenerateBroilerVm(IntlDataHome.Current)));
        Assert.Equal("de-search", vm.TailoringFor("de", "search", null));
        Assert.Equal("de-search", vm.TailoringFor("de", "search", "phonebk"));
        Assert.Equal("und-search", vm.TailoringFor("en", "search", null));
        Assert.Equal("de-phonebk", vm.TailoringFor("de", "sort", "phonebk"));
        Assert.Null(vm.TailoringFor("de", "sort", null));
        Assert.Null(vm.TailoringFor("de", "sort", "default"));
        Assert.Null(vm.TailoringFor("en", "sort", null));
        Assert.Null(vm.TailoringFor("es", "sort", null));

        Assert.Equal("es-standard", Js.TailoringFor("es", "sort", null));
        Assert.Equal("es-trad", Js.TailoringFor("es", "sort", "trad"));
        Assert.Equal("es-search", Js.TailoringFor("es", "search", "trad"));
        Assert.Equal("sv-standard", Js.TailoringFor("sv", "sort", "default"));
        Assert.Equal("th-standard", Js.TailoringFor("th", "sort", null));
        Assert.Equal("und-search", Js.TailoringFor("th", "search", null));
        Assert.Null(Js.TailoringFor("en", "sort", null));
    }

    /// <summary>CollatorKeyData: the <c>co</c> values a locale's sort data has, never <c>standard</c> or <c>search</c>; <c>kn</c> and <c>kf</c>.</summary>
    [Fact]
    public void The_key_values_are_the_data_s()
    {
        var vm = new UcaCollationData(new GeneratedDataSet(IntlDataHome.GenerateBroilerVm(IntlDataHome.Current)));
        Assert.Equal([null, "phonebk"], vm.KeyValues("de", "co", "sort"));
        Assert.Equal([null], vm.KeyValues("de", "co", "search"));
        Assert.Equal([null], vm.KeyValues("en", "co", "sort"));
        Assert.Equal([null, "trad"], Js.KeyValues("es", "co", "sort"));
        Assert.Equal([null, "trad"], Js.KeyValues("sv", "co", "sort"));
        Assert.Equal([null], Js.KeyValues("th", "co", "sort"));
        Assert.Equal(["false", "true"], Js.KeyValues("en", "kn", "sort"));
        Assert.Equal(["false", "lower", "upper"], Js.KeyValues("en", "kf", "search"));
        Assert.Equal([null], Js.KeyValues("en", "nu", "sort"));
    }

    /// <summary>The tailorings order as CLDR's rules say: German phonebook, Spanish n with a tilde and its traditional ch, Swedish's letters after z, Thai's reordering.</summary>
    [Fact]
    public void The_tailorings_order_their_letters()
    {
        Assert.Equal(0, Compare(Collator(Js, CollationStrength.Primary, tailoring: "de-phonebk"), "\u00E4", "ae"));
        Assert.True(Compare(Collator(Js, CollationStrength.Primary), "\u00E4", "ae") < 0);
        Assert.True(Compare(Collator(Js, tailoring: "es-standard"), "\u00F1", "nz") > 0);
        Assert.True(Compare(Collator(Js, tailoring: "es-standard"), "\u00F1", "o") < 0);
        Assert.True(Compare(Collator(Js, tailoring: "es-trad"), "ch", "cz") > 0);
        Assert.True(Compare(Collator(Js, tailoring: "es-standard"), "ch", "cz") < 0);
        Assert.True(Compare(Collator(Js, tailoring: "sv-standard"), "\u00E5", "z") > 0);
        Assert.True(Compare(Collator(Js, tailoring: "sv-standard"), "\u00E5", "\u00E4") < 0);
        Assert.True(Compare(Collator(Js, tailoring: "sv-standard"), "\u00E4", "\u00F6") < 0);
        Assert.True(Compare(Collator(Js, tailoring: "sv-standard"), "\u00FE", "t") > 0);
        Assert.True(Compare(Collator(Js, tailoring: "sv-standard"), "\u00FE", "u") < 0);

        // [reorder Thai]: Thai letters before Latin ones, digits still before both.
        Assert.True(Compare(Collator(Js), "\u0E01", "a") > 0);
        Assert.True(Compare(Collator(Js, tailoring: "th-standard"), "\u0E01", "a") < 0);
        Assert.True(Compare(Collator(Js, tailoring: "th-standard"), "1", "\u0E01") < 0);
        Assert.True(Compare(Collator(Js, tailoring: "th-standard"), "\u0E2E", "a") < 0);

        // Thai's PAIYANNOI, tailored before ANGKHANKHU, is variable: shifted, it weighs nothing.
        Assert.Equal(0, Compare(Collator(Js, tailoring: "th-standard", shifted: true), "\u0E2F", ""));
        Assert.True(Compare(Collator(Js, tailoring: "th-standard", shifted: false), "\u0E2F", "") > 0);
    }

    /// <summary>A tailoring the data does not carry collates by the root, as Broiler.VM's <c>TailoringNamed</c> answered nothing.</summary>
    [Fact]
    public void An_unknown_tailoring_is_the_root()
    {
        Assert.Equal(Compare(Collator(Js), "\u00E4", "af"), Compare(Collator(Js, tailoring: "xx-unknown"), "\u00E4", "af"));
    }

    /// <summary>
    /// The charge (decision E7, row O17): NFD as <c>normalize</c> charges it, then one unit per element of both strings; equal
    /// strings charge nothing.
    /// </summary>
    [Fact]
    public void The_collator_charges_NFD_and_each_element()
    {
        var collator = Collator(Js);
        ulong charged = 0;
        Compare(collator, "abc", "abc", units => charged += units);
        Assert.Equal(0UL, charged);

        ulong nfd = 0;
        Assert.Equal(UnicodeStatus.Success, Normalization.Value.Transform("ab", NormalForm.Nfd, int.MaxValue, units => nfd += units, out _));
        Assert.Equal(UnicodeStatus.Success, Normalization.Value.Transform("abc", NormalForm.Nfd, int.MaxValue, units => nfd += units, out _));

        Compare(collator, "ab", "abc", units => charged += units);
        Assert.Equal(nfd + 5, charged);

        var thrown = Assert.Throws<OperationCanceledException>(() => collator.Compare("ab", "abd", int.MaxValue, static _ => throw new OperationCanceledException(), out _));
        Assert.NotNull(thrown);
    }

    /// <summary>A canonical decomposition past the caller's ceiling is a status, Broiler.VM's <c>RangeError</c>, and no order.</summary>
    [Fact]
    public void A_decomposition_past_the_ceiling_is_a_status()
    {
        var collator = Collator(Js);
        Assert.Equal(IntlStatus.NormalizedStringTooLong, collator.Compare("\u00E4\u00E4", "a", 3, null, out var order));
        Assert.Equal(0, order);
        Assert.Equal(IntlStatus.Completed, collator.Compare("\u00E4\u00E4", "a", 4, null, out order));
        Assert.Equal(1, order);
        Assert.Throws<ArgumentOutOfRangeException>(() => collator.Compare("a", "b", -1, null, out _));
    }

    /// <summary>The options' mapping, NewCollator's: sensitivity to a strength and a case level, the case order, the shifted alternate.</summary>
    [Fact]
    public void The_resolved_options_map_onto_UCA_settings()
    {
        Assert.Equal(new UcaCollatorOptions(CollationStrength.Primary, false, CollationCaseFirst.Off, false, false, null), UcaCollatorOptions.FromResolved("base", false, "false", false, null));
        Assert.Equal(new UcaCollatorOptions(CollationStrength.Primary, true, CollationCaseFirst.Upper, true, true, "x"), UcaCollatorOptions.FromResolved("case", true, "upper", true, "x"));
        Assert.Equal(CollationStrength.Secondary, UcaCollatorOptions.FromResolved("accent", false, "lower", false, null).Strength);
        Assert.Equal(CollationCaseFirst.Lower, UcaCollatorOptions.FromResolved("accent", false, "lower", false, null).CaseFirst);
        Assert.Equal(CollationStrength.Tertiary, UcaCollatorOptions.FromResolved("variant", false, "false", false, null).Strength);

        Assert.Equal(0, Compare(Collator(Js, CollationStrength.Primary), "a", "\u00C1"));
        Assert.True(Compare(Collator(Js, CollationStrength.Primary, caseLevel: true), "a", "A") < 0);
        Assert.Equal(0, Compare(Collator(Js, CollationStrength.Primary, caseLevel: true), "a", "\u00E1"));
        Assert.True(Compare(Collator(Js, caseFirst: CollationCaseFirst.Upper), "A", "a") < 0);
        Assert.True(Compare(Collator(Js, numeric: true), "a2", "a10") < 0);
        Assert.True(Compare(Collator(Js), "a2", "a10") > 0);
        Assert.Equal(0, Compare(Collator(Js, shifted: true), "a-b", "ab"));
        Assert.True(Compare(Collator(Js, shifted: true), "a+b", "ab") < 0);
    }

    /// <summary>The tables are decoded once, on first use; the settings table, which Broiler.VM's data lacks, may be absent.</summary>
    [Fact]
    public void The_data_is_decoded_once_and_settings_are_optional()
    {
        var tables = IntlDataHome.GenerateBroilerVm(IntlDataHome.Current);
        var counted = new GeneratedDataSet(tables);
        var data = new UcaCollationData(counted);
        Assert.Equal(0, counted.Reads(IntlTable.CollationRoot));
        Assert.True(data.HasRoot);
        _ = data.TailoringNames;
        _ = data.TailoringNames;
        Assert.Equal(2, counted.Reads(IntlTable.CollationRoot));
        Assert.Equal(1, counted.Reads(IntlTable.CollationSettings));
    }

    /// <summary>A settings line the format does not state, or for a tailoring the data lacks, is refused when the data is decoded.</summary>
    [Theory]
    [InlineData("th-standard|alternate|non-ignorable")]
    [InlineData("xx-standard|alternate|shifted")]
    [InlineData("th-standard|reorder|373B|377A")]
    [InlineData("th-standard|reorder|377A|373B|23EC")]
    [InlineData("th-standard|reorder|373B|377A|4000")]
    [InlineData("th-standard|backwards|2")]
    public void A_malformed_setting_is_refused(string line)
    {
        var tables = IntlDataHome.GenerateBroilerJs(IntlDataHome.Current);
        var data = new UcaCollationData(new GeneratedDataSet(tables, new Dictionary<string, byte[]> { ["CollationSettings"] = Encoding.ASCII.GetBytes(line) }));
        Assert.Throws<InvalidDataException>(() => data.TailoringNames);
        Assert.False(new UcaCollationData(new GeneratedDataSet(tables, new Dictionary<string, byte[]> { ["CollationRoot"] = [] })).HasRoot);
    }

    /// <summary>The generator's tables as an engine hands them in, counting each table's reads, with some replaced.</summary>
    private sealed class GeneratedDataSet(Generation.Intl.IntlTables tables, IReadOnlyDictionary<string, byte[]>? overrides = null) : IIntlDataSet
    {
        private readonly int[] reads = new int[64];

        public string CldrVersion => tables.CldrVersion;

        public ReadOnlySpan<byte> Table(IntlTable table)
        {
            System.Threading.Interlocked.Increment(ref reads[(int)table]);
            var name = table.ToString();
            if (overrides is not null && overrides.TryGetValue(name, out var replaced))
                return replaced;
            return tables.Tables.FirstOrDefault(t => t.Name == name) is { } found ? found.Data.Span : default;
        }

        internal int Reads(IntlTable table) => reads[(int)table];
    }
}
