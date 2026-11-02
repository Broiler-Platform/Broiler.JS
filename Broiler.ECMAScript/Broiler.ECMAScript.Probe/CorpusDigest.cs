using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Broiler.ECMAScript.Runners;
using Broiler.ECMAScript.Syntax;

namespace Broiler.ECMAScript.Probe;

/// <summary>
/// The probe's <c>--digest-corpus</c> mode (stage SJL-13.2e; the home rule's part C, "the corpus runner's dump mode"):
/// <c>--digest-corpus corpus-root --out file [--test262 root] [--test262-json root] [--reverse] [--two-threads]</c>. For
/// every input of the parse corpus, under each of its variants in configuration V and, for an eval-family input, as an
/// eval request under each of E0 to E7, it writes one line - the id, the variant, the outcome and the SHA-256 of the
/// parse's canonical dump (<see cref="SyntaxDump"/>) - in the manifest's order whatever order it parsed in. With
/// <c>--reverse</c> it parses in the reverse order; with <c>--two-threads</c> it parses each input twice concurrently, once
/// on each of two threads, and reports a difference between the two. Every parse runs on a thread of
/// <see cref="ParseLimits.StackBytes"/>. Run under the JIT and from the Native AOT image, the two files are compared byte
/// for byte.
/// </summary>
internal static class CorpusDigest
{
    internal static int Run(string[] args)
    {
        string? corpusRoot = null;
        string? output = null;
        string? test262 = null;
        string? test262Json = null;
        var reverse = false;
        var twoThreads = false;
        for (var i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--out" when i + 1 < args.Length:
                    output = args[++i];
                    break;
                case "--test262" when i + 1 < args.Length:
                    test262 = args[++i];
                    break;
                case "--test262-json" when i + 1 < args.Length:
                    test262Json = args[++i];
                    break;
                case "--reverse":
                    reverse = true;
                    break;
                case "--two-threads":
                    twoThreads = true;
                    break;
                default:
                    if (corpusRoot is not null)
                        return Usage();
                    corpusRoot = args[i];
                    break;
            }
        }

        if (corpusRoot is null || output is null)
            return Usage();

        var corpus = ParseCorpus.Load(corpusRoot, test262, test262Json);
        var work = new List<(int Index, ParseCorpus.Row Row, string Variant)>();
        foreach (var row in corpus.Rows)
        {
            foreach (var variant in row.Variants)
                work.Add((work.Count, row, variant));
            if (row.EvalFamily)
            {
                foreach (var flags in new[] { "E0", "E1", "E2", "E3", "E4", "E5", "E6", "E7" })
                    work.Add((work.Count, row, flags));
            }
        }

        var lines = new string[work.Count];
        var problems = new List<string>();
        if (reverse)
            work.Reverse();

        void Digest(int from, int step, string[] into, List<string> found)
        {
            for (var i = from; i < work.Count; i += step)
            {
                var (index, row, variant) = work[i];
                var text = corpus.Text(row, out var problem);
                if (text is null)
                {
                    lock (found)
                        found.Add(row.Id + "\t" + problem);
                    into[index] = row.Id + "\t" + variant + "\tabsent\t-";
                    continue;
                }

                var result = SyntaxParser.Parse(corpus.Request(text, variant, "V", AlgorithmsLibrary.RunnerRegularExpressions.Instance));
                into[index] = row.Id + "\t" + variant + "\t" + (result.Outcome == ParseOutcome.Accepted ? "accepted" : "refused") + "\t" + SyntaxDump.Digest(result);
            }
        }

        if (!twoThreads)
        {
            var thread = new Thread(() => Digest(0, 1, lines, problems), ParseLimits.StackBytes);
            thread.Start();
            thread.Join();
        }
        else
        {
            // Each input parsed twice at once, one parse on each thread: the threads walk the same order.
            var second = new string[work.Count];
            var a = new Thread(() => Digest(0, 1, lines, problems), ParseLimits.StackBytes);
            var b = new Thread(() => Digest(0, 1, second, new List<string>()), ParseLimits.StackBytes);
            a.Start();
            b.Start();
            a.Join();
            b.Join();
            for (var i = 0; i < lines.Length; i++)
            {
                if (!string.Equals(lines[i], second[i], StringComparison.Ordinal))
                    problems.Add("the two threads differ: " + lines[i] + " | " + second[i]);
            }
        }

        var text = new StringBuilder();
        text.Append("# parse corpus ").Append(corpus.Digest).Append(", ").Append(Program.Decimal(lines.Length)).Append(" parses in configuration V\n");
        foreach (var line in lines)
            text.Append(line).Append('\n');
        File.WriteAllText(output, text.ToString(), new UTF8Encoding(false));

        foreach (var problem in problems)
            Console.Error.Write(problem + "\n");
        Console.Out.Write("Broiler.ECMAScript.Probe: digest of " + Program.Decimal(lines.Length) + " parses, " + Program.Decimal(problems.Count) + " problems\n");
        return problems.Count == 0 ? 0 : 1;
    }

    private static int Usage()
    {
        Console.Error.Write("usage: Broiler.ECMAScript.Probe --digest-corpus corpus-root --out file [--test262 root] [--test262-json root] [--reverse] [--two-threads]\n");
        return 2;
    }
}
