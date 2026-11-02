// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Broiler.ECMAScript.Probe;
using Broiler.ECMAScript.Syntax;

namespace FrontendCorpus;

/// <summary>
/// The home rule's part A corpus runner: <c>frontend-corpus --corpus conformance/parse --test262 root --classification
/// file --catalogue file --stages file --out report [--commit id]</c>. Every input of the parse corpus is parsed under
/// its goal and each of its variants in configurations V and J, and every eval-family input as eval code under E0 to E7
/// in configuration V, in the classification's order, on one thread of <see cref="ParseLimits.StackBytes"/>; each line
/// of the report is the outcome (accepted, refused, crashed, or absent when the input's text is missing or differs
/// from the manifest's SHA-256), and for a refusal the first diagnostic's shared code, phase and span, and for a refusal
/// at either limit the depth the library reports (its diagnostic's argument, the bound it refused past). The report
/// holds no expectation: the comparison (<c>../compare_corpus.py</c>) reads the classification beside it.
/// </summary>
/// <remarks>
/// <b>The regular-expression provider</b> (decision D26) answers as each configuration's engine validator answered the
/// input, as the classification records it: for an input, variant and configuration the classification expects refused
/// with a row among the rows it names first whose stage-table basis is <c>provider</c>, it refuses the first literal the
/// library asks it about, naming the message identifier of the first such row; it admits every other literal. A
/// <c>json</c> variant is read under the Script goal (the probe's requests).
/// </remarks>
internal static class Program
{
    private static readonly string[] EvalFlags = { "E0", "E1", "E2", "E3", "E4", "E5", "E6", "E7" };

    private static int Main(string[] args)
    {
        var options = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var i = 0; i + 1 < args.Length; i += 2)
            options[args[i]] = args[i + 1];
        foreach (var required in new[] { "--corpus", "--test262", "--classification", "--catalogue", "--stages", "--out" })
        {
            if (!options.ContainsKey(required))
            {
                Console.Error.Write("usage: frontend-corpus --corpus dir --test262 dir --classification file --catalogue file --stages file --out file [--commit id]\n");
                return 2;
            }
        }

        var corpus = ParseCorpus.Load(options["--corpus"], options["--test262"], null);
        var messageIds = ReadColumn(options["--catalogue"], "id", "message_id");
        var basis = ReadColumn(options["--stages"], "id", "basis");
        var refusals = ProviderRefusals(options["--classification"], basis, messageIds);

        var lines = new List<string>();
        var counts = new int[4];
        var work = new Thread(
            () =>
            {
                foreach (var row in corpus.Rows)
                {
                    var text = corpus.Text(row, out _);
                    foreach (var variant in row.Variants)
                    {
                        foreach (var configuration in new[] { "V", "J" })
                            lines.Add(Run(corpus, row, text, variant, configuration, refusals, counts));
                    }

                    if (row.EvalFamily)
                    {
                        foreach (var flags in EvalFlags)
                            lines.Add(Run(corpus, row, text, flags, "V", refusals, counts));
                    }
                }
            },
            ParseLimits.StackBytes);
        work.Start();
        work.Join();

        var report = new StringBuilder();
        report.Append("#% format\tsjl-13-001-frontend-corpus\t1\n");
        report.Append("#% commit\t").Append(options.TryGetValue("--commit", out var commit) ? commit : "-").Append('\n');
        report.Append("#% manifest\t").Append(corpus.Digest).Append('\n');
        report.Append("#% classification\t").Append(Sha256(options["--classification"])).Append('\n');
        report.Append("#% catalogue\t").Append(Sha256(options["--catalogue"])).Append('\n');
        report.Append("#% stages\t").Append(Sha256(options["--stages"])).Append('\n');
        report.Append("#% stack-bytes\t").Append(ParseLimits.StackBytes.ToString(CultureInfo.InvariantCulture)).Append('\n');
        report.Append("#% provider-refusals\t").Append(refusals.Count.ToString(CultureInfo.InvariantCulture)).Append('\n');
        report.Append("id\tvariant\tconfig\toutcome\tcode\tphase\tspan\tdepth\n");
        foreach (var line in lines)
            report.Append(line).Append('\n');
        File.WriteAllText(options["--out"], report.ToString(), new UTF8Encoding(false));

        Console.Out.Write(string.Create(CultureInfo.InvariantCulture,
            $"frontend-corpus: {lines.Count} parses: {counts[0]} accepted, {counts[1]} refused, {counts[2]} crashed, {counts[3]} absent\n"));
        return counts[2] + counts[3] == 0 ? 0 : 1;
    }

    private static string Run(ParseCorpus corpus, ParseCorpus.Row row, string? text, string variant, string configuration,
        Dictionary<string, string> refusals, int[] counts)
    {
        var prefix = row.Id + "\t" + variant + "\t" + configuration + "\t";
        if (text is null)
        {
            counts[3]++;
            return prefix + "absent\t-\t-\t-\t-";
        }

        var provider = new ClassifiedProvider(refusals.TryGetValue(row.Id + "\t" + variant + "\t" + configuration, out var id) ? id : null);
        ParseResult result;
        try
        {
            result = SyntaxParser.Parse(corpus.Request(text, variant, configuration, provider));
        }
        catch (Exception e)
        {
            counts[2]++;
            return prefix + "crashed\t" + e.GetType().FullName + "\t-\t-\t-";
        }

        if (result.Outcome == ParseOutcome.Accepted)
        {
            counts[0]++;
            return prefix + "accepted\t-\t-\t-\t-";
        }

        counts[1]++;
        var first = result.Diagnostics[0];
        var span = string.Create(CultureInfo.InvariantCulture, $"{first.Span.Offset}+{first.Span.Length}@{first.Span.Line}:{first.Span.Column}");
        var depth = first.Code is "JSE9201" or "JSE9202" && first.Arguments.Count > 0 ? first.Arguments[0] : "-";
        return prefix + "refused\t" + first.Code + "\t" + first.Phase.ToString() + "\t" + span + "\t" + depth;
    }

    /// <summary>For each input, variant and configuration the classification expects refused with a provider row first: that row's message identifier.</summary>
    private static Dictionary<string, string> ProviderRefusals(string classification, Dictionary<string, string> basis, Dictionary<string, string> messageIds)
    {
        var refusals = new Dictionary<string, string>(StringComparer.Ordinal);
        using var stream = new GZipStream(File.OpenRead(classification), CompressionMode.Decompress);
        using var reader = new StreamReader(stream, new UTF8Encoding(false));
        string? line;
        var header = true;
        while ((line = reader.ReadLine()) is not null)
        {
            if (line.StartsWith('#'))
                continue;
            if (header)
            {
                header = false;
                continue;
            }

            var c = line.Split('\t');
            if (c[3] != "refused")
                continue;
            foreach (var rowId in c[4].Split(';'))
            {
                if (basis.TryGetValue(rowId, out var b) && b == "provider")
                {
                    refusals[c[0] + "\t" + c[1] + "\t" + c[2]] = messageIds[rowId];
                    break;
                }
            }
        }

        return refusals;
    }

    private static Dictionary<string, string> ReadColumn(string path, string key, string value)
    {
        var lines = File.ReadAllLines(path, new UTF8Encoding(false));
        var columns = new List<string>(lines[0].Split('\t'));
        var (k, v) = (columns.IndexOf(key), columns.IndexOf(value));
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var i = 1; i < lines.Length; i++)
        {
            if (lines[i].Length == 0)
                continue;
            var c = lines[i].Split('\t');
            map[c[k]] = c[v];
        }

        return map;
    }

    private static string Sha256(string path) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));

    /// <summary>The provider of one parse: refuses the first literal it is asked about with the given identifier, if any, and admits every other.</summary>
    private sealed class ClassifiedProvider(string? messageId) : IRegularExpressionValidator
    {
        private bool asked;

        public RegularExpressionVerdict Validate(string pattern, string flags)
        {
            if (messageId is null || asked)
                return RegularExpressionVerdict.Valid;
            asked = true;
            return RegularExpressionVerdict.Invalid(messageId, null, 0, 0, inFlags: false);
        }
    }
}
