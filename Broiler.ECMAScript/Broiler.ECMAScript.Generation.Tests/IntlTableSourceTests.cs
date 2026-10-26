using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Broiler.ECMAScript.Generation.Intl;
using Broiler.ECMAScript.Generation.Time;
using Broiler.ECMAScript.Runners;
using Xunit;

namespace Broiler.ECMAScript.Generation.Tests;

/// <summary>
/// The Intl generator's byte identity with Broiler.VM's (decision JSD-0082, decision D11; the verdict's
/// falsifier "the home's generator, from this family's pins, does not write <c>JsCldrTables.g.cs</c> byte for
/// byte"): with Broiler.VM's options, over the home's copy of Broiler.VM's pinned files and the home's UCD
/// copies, the source writer writes Broiler.VM's <c>JsCldrTables.g.cs</c> as Broiler.VM's own generator wrote
/// it before its assurance pass. Broiler.VM's milestone SJL-9.2c regenerates its file with this package.
/// </summary>
public sealed class IntlTableSourceTests
{
    /// <summary>
    /// The SHA-256 of Broiler.VM's <c>src/Broiler.VM.Profile.JavaScript.Intl/JsCldrTables.g.cs</c> at Broiler.VM
    /// <c>4cf9ca449fc96a1a5be810f6d0ca31c42b855c8f</c> as its <c>CldrTableGenerator.Generate</c> wrote it, before
    /// its assurance generator added the header and filled the fingerprints: the committed file from
    /// <c>using System;</c> on, each computed <c>Fingerprint=</c> value put back to <c>TBF</c>. Both readings -
    /// Broiler.VM's generator run at that commit, and the committed file so restored - give this digest (bundle
    /// sjl-9-001's README, "The data home").
    /// </summary>
    internal const string BroilerVmCldrSourceSha256 = "839bdc22afca187b58252f2487cd31c4b613396cd6596ffdcb6622fe1b534561";

    private static readonly Lazy<IntlTables> BroilerVm = new(static () => IntlDataHome.GenerateBroilerVm(IntlDataHome.Current));

    /// <summary>Broiler.VM's options for <c>JsCldrTables.g.cs</c>, as its generator's header constant states them.</summary>
    internal static TableSourceOptions BroilerVmCldr => new("Broiler.VM.Profile.JavaScript.Intl", "JsCldrTables")
    {
        Header =
        [
            "Written by CldrTableGenerator (src/tests/Broiler.VM.Architecture.Tests) from the CLDR 48.2.0",
            "files that src/tests/cldr/pins/cldr.pin names and the Unicode Character Database 17.0.0 files",
            "that src/tests/unicode/pins/unicode.pin names, under decision JSD-0043. Rule N28 regenerates",
            "this file and compares it byte for byte, so a change belongs in the generator or a pinned",
            "archive and never here: run the architecture tests with BROILER_CLDR_WRITE=1.",
            "",
            "The table data is derived from Unicode data files and is subject to the Unicode License v3",
            "(SPDX Unicode-3.0), whose text THIRD_PARTY_NOTICES.md carries. The SPDX lines above are written",
            "on every product file by the code assurance generator and describe this file's code.",
        ],
        ClassAnnotation =
        [
            "Broiler-AI:           Origin=Derived; Spec=JSD-0043; IP=Low; Security=Low; Resources=1; Fingerprint=TBF",
            "Broiler-Human:        PENDING",
        ],
        MemberAnnotation =
        [
            "Broiler-AI:           EXEMPT=" + CldrTableGenerator.BroilerVmExemptReason,
            "Broiler-Human:        PENDING",
        ],
    };

    private static string Sha256(string text) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)));

    [Fact]
    public void With_Broiler_VM_s_options_the_writer_writes_Broiler_VM_s_JsCldrTables_byte_for_byte()
    {
        var text = IntlTableSource.Write(BroilerVm.Value, BroilerVmCldr);

        Assert.Equal(BroilerVmCldrSourceSha256, Sha256(text));
        Assert.Empty(BroilerVm.Value.AbsentFiles);
    }

    /// <summary>The control: one changed byte of one table, or one table's summary, moves the digest.</summary>
    [Fact]
    public void A_changed_byte_or_summary_does_not_write_Broiler_VM_s_file()
    {
        var tables = BroilerVm.Value;
        var locales = tables["Locales"];
        var changed = locales.Data.ToArray();
        changed[0] ^= 1;

        var edited = IntlTablesWith(tables, locales with { Data = changed });
        Assert.NotEqual(BroilerVmCldrSourceSha256, Sha256(IntlTableSource.Write(edited, BroilerVmCldr)));

        var resummarized = IntlTablesWith(tables, locales with { Summary = locales.Summary + " " });
        Assert.NotEqual(BroilerVmCldrSourceSha256, Sha256(IntlTableSource.Write(resummarized, BroilerVmCldr)));
    }

    /// <summary>Two runs, and two threads, write the same bytes for both embeddings (rule SL9's reading for the generator).</summary>
    [Fact]
    public async Task Two_runs_and_two_threads_write_the_same_tables()
    {
        var home = IntlDataHome.Current;
        var vm = IntlTableSource.Write(IntlDataHome.GenerateBroilerVm(home), BroilerVmCldr);
        var threads = await Task.WhenAll(
            Task.Run(() => IntlTableSource.Write(IntlDataHome.GenerateBroilerVm(home), BroilerVmCldr)),
            Task.Run(() => IntlTableSource.Write(IntlDataHome.GenerateBroilerVm(home), BroilerVmCldr)));

        Assert.All(threads, text => Assert.Equal(vm, text));
        Assert.Equal(BroilerVmCldrSourceSha256, Sha256(vm));

        var js = await Task.WhenAll(
            Task.Run(() => IntlDataHome.Files(IntlDataHome.GenerateBroilerJs(home))),
            Task.Run(() => IntlDataHome.Files(IntlDataHome.GenerateBroilerJs(home))));

        Assert.Equal(js[0].Keys, js[1].Keys);
        Assert.All(js[0], pair => Assert.True(pair.Value.AsSpan().SequenceEqual(js[1][pair.Key]), pair.Key + " differs between two threads"));
    }

    /// <summary>The writer writes the caller's namespace and class, and each table's bytes in its literal.</summary>
    [Fact]
    public void The_writer_writes_the_caller_s_class_and_each_table_s_bytes()
    {
        var tables = BroilerVm.Value;
        var text = IntlTableSource.Write(tables, new TableSourceOptions("Example.Engine.Intl", "IntlTableData"));

        Assert.StartsWith("using System;\n\nnamespace Example.Engine.Intl;\n\n/// <summary>The CLDR 48.2.0 tables, as the generator wrote them.</summary>\ninternal static class IntlTableData\n{\n", text, StringComparison.Ordinal);
        Assert.Contains("    internal const string Version = \"48.2.0\";\n", text, StringComparison.Ordinal);
        Assert.DoesNotContain('\r', text);

        foreach (var table in tables.Tables.Where(static table => table.IsText))
        {
            var open = text.IndexOf("ReadOnlySpan<byte> " + table.Name + " =>\n        \"\"\"\n", StringComparison.Ordinal);
            var from = text.IndexOf("\"\"\"\n", open, StringComparison.Ordinal) + 4;
            var to = text.IndexOf("\n        \"\"\"u8;", from, StringComparison.Ordinal);
            var lines = text[from..to].Split('\n').Select(static line => line[8..]);
            Assert.Equal(table.Data.ToArray(), Encoding.ASCII.GetBytes(string.Join('\n', lines)));
        }
    }

    private static IntlTables IntlTablesWith(IntlTables tables, IntlGeneratedTable replacement) =>
        new(tables.CldrVersion, tables.UcaVersion, tables.Tables.Select(table => table.Name == replacement.Name ? replacement : table).ToList(), tables.AbsentFiles);
}
