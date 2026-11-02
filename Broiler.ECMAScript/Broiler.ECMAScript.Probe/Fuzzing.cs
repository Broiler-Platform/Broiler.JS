using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Broiler.ECMAScript.Runners;
using Broiler.ECMAScript.Syntax;

namespace Broiler.ECMAScript.Probe;

/// <summary>
/// The home's fuzz target for the shared syntax library (stage SJL-13.2e; Broiler.VM's JSD-0083, decision D22; the home
/// rule's part F): <c>--fuzz seeds [--test262 root] [--test262-json root] [--lines first:last] [--findings dir]</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>What it reads.</b> The seeds file the home rule's bundle commits,
/// <c>docs/evidence/sjl-13-001/inputs/fuzz-seeds.txt</c>: its header names the parse corpus's manifest and its SHA-256,
/// which the target holds the manifest to, the time bound per parse, and each line a seed by its corpus id, its goal, its
/// configuration (V or J, the home rule's section "The two configurations") and its count of mutants. A seed's text is
/// the corpus input's, read as the inputs README says.
/// </para>
/// <para>
/// <b>The mutants.</b> Each line's mutants come from a deterministic stream, SplitMix64 seeded with
/// <see cref="StreamSeed"/> mixed with the FNV-1a hash of the line's id and configuration, so that two runs make the same
/// mutants on any host and in any order of lines. A mutant is its seed with one to four operations applied in turn, each
/// chosen from the stream: delete a span; duplicate a span elsewhere; insert a token of the lexical grammar's edges (a
/// punctuator, a keyword, a template's or a comment's delimiter, an escape); replace one code unit with one of the lexical
/// grammar's hard cases (a line or paragraph separator, a lone surrogate, a byte-order mark, NUL); swap two spans; cut the
/// text; wrap a span in up to 600 levels of one of the corpus's nesting shapes, past both configurations' nesting bounds;
/// and insert a binary chain of up to 12,000 operators, past the tree-depth constant.
/// </para>
/// <para>
/// <b>What it reads from a parse</b> (part F's reading): a parse must return a tree or diagnostics. A finding is an
/// exception escaping the library, a parse past the seeds file's time bound, an accepted result with no tree or with a
/// diagnostic, a refused result with none, or a diagnostic whose code no row of the catalogue
/// (<c>Broiler.ECMAScript/docs/frontend/early-errors.tsv</c>) carries. A parse that runs past ten times the bound, and at
/// least twenty seconds, is reported as a hang and ends the process with code 3; a process that ends with no summary line
/// lost its stack, which is a finding too. Every parse runs on one thread of <see cref="ParseLimits.StackBytes"/>, the
/// stack the library documents.
/// </para>
/// </remarks>
internal sealed class Fuzzing
{
    /// <summary>The seed of the mutation stream, fixed so that every run makes the same mutants (stage SJL-13.2e).</summary>
    internal const ulong StreamSeed = 0x534A_4C13_2E00_F022;

    private readonly ParseCorpus corpus;
    private readonly HashSet<string> codes;
    private readonly int timeBound;
    private readonly string? findingsDirectory;
    private readonly List<string> findings = new();
    private readonly SortedDictionary<string, int> tally = new(StringComparer.Ordinal);
    private readonly FuzzValidator validator = new();

    // The parse in flight, read by the watchdog.
    private long started;
    private volatile string current = "-";

    private Fuzzing(ParseCorpus corpus, HashSet<string> codes, int timeBound, string? findingsDirectory)
    {
        this.corpus = corpus;
        this.codes = codes;
        this.timeBound = timeBound;
        this.findingsDirectory = findingsDirectory;
    }

    /// <summary>The probe's <c>--fuzz</c> mode.</summary>
    internal static int Run(string[] args)
    {
        string? seeds = null;
        string? test262 = null;
        string? test262Json = null;
        string? findingsDirectory = null;
        var first = 0;
        var last = int.MaxValue;
        for (var i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--test262" when i + 1 < args.Length:
                    test262 = args[++i];
                    break;
                case "--test262-json" when i + 1 < args.Length:
                    test262Json = args[++i];
                    break;
                case "--findings" when i + 1 < args.Length:
                    findingsDirectory = args[++i];
                    break;
                case "--lines" when i + 1 < args.Length:
                    var range = args[++i].Split(':');
                    first = Number(range[0]);
                    last = Number(range[1]);
                    break;
                default:
                    if (seeds is not null)
                        return Usage();
                    seeds = args[i];
                    break;
            }
        }

        if (seeds is null)
            return Usage();

        var root = UnicodeDataHome.Root(AppContext.BaseDirectory);
        var header = new Dictionary<string, string[]>(StringComparer.Ordinal);
        var lines = new List<(string Id, string Goal, string Configuration, int Mutants)>();
        foreach (var line in File.ReadAllText(seeds, new UTF8Encoding(false, true)).Split('\n'))
        {
            if (line.StartsWith("#% ", StringComparison.Ordinal))
            {
                var fields = line[3..].Split('\t');
                header[fields[0]] = fields[1..];
            }
            else if (line.Length > 0 && line[0] != '#' && !line.StartsWith("id\t", StringComparison.Ordinal))
            {
                var c = line.Split('\t');
                lines.Add((c[0], c[1], c[2], Number(c[3])));
            }
        }

        if (!header.TryGetValue("corpus", out var corpusField) || !header.TryGetValue("time-bound-per-parse-ms", out var boundField))
        {
            Console.Error.Write("fuzz: the seeds file names no corpus or no time bound\n");
            return 2;
        }

        var manifest = Path.Combine(root, corpusField[0].Replace('/', Path.DirectorySeparatorChar));
        var corpus = ParseCorpus.Load(Path.GetDirectoryName(manifest)!, test262, test262Json);
        if (!string.Equals(corpus.Digest, corpusField[1], StringComparison.Ordinal))
        {
            Console.Error.Write("fuzz: the manifest's SHA-256 is " + corpus.Digest + ", not the seeds file's " + corpusField[1] + "\n");
            return 2;
        }

        var codes = new HashSet<string>(StringComparer.Ordinal);
        var catalogue = File.ReadAllText(Path.Combine(root, "Broiler.ECMAScript", "docs", "frontend", "early-errors.tsv"), new UTF8Encoding(false, true)).Split('\n');
        var codeColumn = Array.IndexOf(catalogue[0].Split('\t'), "shared_code");
        for (var i = 1; i < catalogue.Length; i++)
        {
            if (catalogue[i].Length > 0)
                codes.Add(catalogue[i].Split('\t')[codeColumn]);
        }

        var target = new Fuzzing(corpus, codes, Number(boundField[0]), findingsDirectory);
        var selected = lines.GetRange(Math.Min(first, lines.Count), Math.Max(0, Math.Min(last, lines.Count) - Math.Min(first, lines.Count)));
        return target.Campaign(selected);
    }

    private int Campaign(List<(string Id, string Goal, string Configuration, int Mutants)> lines)
    {
        var rows = new Dictionary<string, ParseCorpus.Row>(StringComparer.Ordinal);
        foreach (var row in corpus.Rows)
            rows[row.Id] = row;

        var parsed = 0;
        var seedsRead = 0;
        var worker = new Thread(
            () =>
            {
                foreach (var (id, goal, configuration, mutants) in lines)
                {
                    if (!rows.TryGetValue(id, out var row))
                    {
                        Finding(id, configuration, -1, "seed", "no row of the manifest has this id", null);
                        continue;
                    }

                    var text = corpus.Text(row, out var problem);
                    if (text is null)
                    {
                        Finding(id, configuration, -1, "seed", problem!, null);
                        continue;
                    }

                    seedsRead++;
                    var variant = goal == "module" ? "module" : "sloppy";
                    var stream = new SplitMix64(StreamSeed ^ Fnv1a(id + "/" + configuration));
                    for (var k = -1; k < mutants; k++)
                    {
                        // The seed itself first, then its mutants.
                        var mutant = k < 0 ? text : Mutate(text, ref stream);
                        Parse(id, configuration, k, corpus.Request(mutant, variant, configuration, validator), mutant);
                        parsed++;
                    }
                }
            },
            ParseLimits.StackBytes);

        worker.Start();
        while (!worker.Join(100))
        {
            var since = Volatile.Read(ref started);
            if (since != 0 && Environment.TickCount64 - since > Math.Max(10L * timeBound, 20_000))
            {
                Console.Out.Write("fuzz: hang\t" + current + "\n");
                Console.Out.Flush();
                Environment.Exit(3);
            }
        }

        foreach (var finding in findings)
            Console.Out.Write("finding\t" + finding + "\n");
        foreach (var (key, count) in tally)
            Console.Out.Write("tally\t" + key + "\t" + Program.Decimal(count) + "\n");
        Console.Out.Write("fuzz: " + Program.Decimal(lines.Count) + " seed lines, " + Program.Decimal(seedsRead) + " seeds read, "
            + Program.Decimal(parsed) + " parses, " + Program.Decimal(findings.Count) + " findings\n");
        return findings.Count == 0 ? 0 : 1;
    }

    private void Parse(string id, string configuration, int index, ParseRequest request, string text)
    {
        current = id + "\t" + configuration + "\t" + Program.Decimal(index);
        Volatile.Write(ref started, Environment.TickCount64);
        ParseResult? result = null;
        Exception? escaped = null;
        try
        {
            result = SyntaxParser.Parse(request);
        }
        catch (Exception e)
        {
            escaped = e;
        }

        var elapsed = Environment.TickCount64 - Volatile.Read(ref started);
        Volatile.Write(ref started, 0);
        if (escaped is not null)
        {
            Finding(id, configuration, index, "exception", escaped.Message, text);
            return;
        }

        if (elapsed > timeBound)
            Finding(id, configuration, index, "time", Program.Decimal((int)Math.Min(elapsed, int.MaxValue)) + " ms", text);

        if (result!.Outcome == ParseOutcome.Accepted)
        {
            Count(configuration + "\taccepted");
            if (result.Tree is null || result.Diagnostics.Count != 0)
                Finding(id, configuration, index, "result", "accepted with no tree or with a diagnostic", text);
            return;
        }

        if (result.Outcome != ParseOutcome.Refused || result.Diagnostics.Count == 0)
        {
            Finding(id, configuration, index, "result", "neither a tree nor a diagnostic", text);
            return;
        }

        var code = result.Diagnostics[0].Code;
        Count(configuration + "\trefused\t" + code);
        foreach (var diagnostic in result.Diagnostics)
        {
            if (!codes.Contains(diagnostic.Code))
                Finding(id, configuration, index, "code", diagnostic.Code + " is no catalogue row's code", text);
        }
    }

    private void Count(string key)
    {
        tally.TryGetValue(key, out var count);
        tally[key] = count + 1;
    }

    private void Finding(string id, string configuration, int index, string kind, string detail, string? text)
    {
        findings.Add(id + "\t" + configuration + "\t" + Program.Decimal(index) + "\t" + kind + "\t" + detail);
        if (findingsDirectory is not null && text is not null)
        {
            Directory.CreateDirectory(findingsDirectory);
            File.WriteAllText(Path.Combine(findingsDirectory, id + "-" + configuration + "-" + Program.Decimal(index) + ".js"), text, new UTF8Encoding(false));
        }
    }

    // ---------------------------------------------------------------------------------------------------------------
    // The mutation operators.

    private readonly string[] tokens =
    [
        "(", ")", "[", "]", "{", "}", ";", ",", "=>", "...", "?.", "??", "?", ":", "=", "==", "===", "+", "++", "-", "--", "*", "**",
        "/", "/=", "%", "<", "<<", ">>>", "&&", "||", "!", "~", "@", "#x", "`", "${", "}`", "`${", "'", "\"", "\\", "\\u0061",
        "\\u{61}", "/*", "*/", "//", "<!--", "-->", "\n", "\r\n", " ", "0x", "0b1", "0o7", "07", "08", "1n", "1m", "1_0", ".5", "1e",
        "/a/g", "/[/]/v", "function", "function*", "async", "await", "yield", "class", "extends", "super", "new", "new.target",
        "import", "import.meta", "export", "default", "let", "const", "var", "using", "await using", "static", "get", "set",
        "accessor", "return", "break", "continue", "if", "else", "for", "of", "in", "while", "do", "switch", "case", "try",
        "catch", "finally", "throw", "with", "delete", "typeof", "void", "this", "null", "true", "arguments", "eval",
        "\"use strict\";", "enum", "implements", "#!",
    ];

    private readonly char[] hardUnits = ['\0', '\u2028', '\u2029', '\uFEFF', '\uD800', '\uDC00', '\u00A0', '\u200C', '\u200D', '\u0085', '\t', '\v', '\f'];

    private readonly (string Open, string Close)[] nests =
    [
        ("(", ")"), ("[", "]"), ("{", "}"), ("`${", "}`"), ("function f(){", "}"), ("{a:", "}"), ("!", ""), ("a=>", ""),
    ];

    private string Mutate(string seed, ref SplitMix64 stream)
    {
        var text = seed;
        var operations = 1 + (int)stream.Next(4);
        for (var i = 0; i < operations; i++)
        {
            var at = text.Length == 0 ? 0 : (int)stream.Next((ulong)text.Length + 1);
            var span = Math.Min(text.Length - at, 1 + (int)stream.Next(32));
            switch (stream.Next(8))
            {
                case 0:
                    text = text.Remove(at, span);
                    break;
                case 1:
                    var to = (int)stream.Next((ulong)text.Length + 1);
                    text = text.Insert(to, text.Substring(at, span));
                    break;
                case 2:
                    text = text.Insert(at, tokens[stream.Next((ulong)tokens.Length)]);
                    break;
                case 3:
                    if (at < text.Length)
                        text = text.Remove(at, 1).Insert(at, hardUnits[stream.Next((ulong)hardUnits.Length)].ToString());
                    break;
                case 4:
                    var other = (int)stream.Next((ulong)text.Length + 1);
                    var otherSpan = Math.Min(text.Length - other, span);
                    if (other >= at + span && otherSpan > 0)
                        text = text[..at] + text.Substring(other, otherSpan) + text[(at + span)..other] + text.Substring(at, span) + text[(other + otherSpan)..];
                    break;
                case 5:
                    text = text[..at];
                    break;
                case 6:
                    var (open, close) = nests[stream.Next((ulong)nests.Length)];
                    var levels = 1 + (int)stream.Next(600);
                    text = text[..at] + Repeat(open, levels) + text.Substring(at, span) + Repeat(close, levels) + text[(at + span)..];
                    break;
                default:
                    text = text.Insert(at, "a" + Repeat("+a", 1 + (int)stream.Next(12_000)));
                    break;
            }
        }

        return text;
    }

    private static string Repeat(string text, int count)
    {
        var builder = new StringBuilder(text.Length * count);
        for (var i = 0; i < count; i++)
            builder.Append(text);
        return builder.ToString();
    }

    private static ulong Fnv1a(string text)
    {
        var hash = 0xCBF2_9CE4_8422_2325UL;
        foreach (var c in text)
        {
            hash ^= c;
            hash *= 0x0000_0100_0000_01B3UL;
        }

        return hash;
    }

    /// <summary>SplitMix64 (Steele, Lea and Flood, 2014): a deterministic stream of 64-bit values.</summary>
    private struct SplitMix64(ulong state)
    {
        private ulong state = state;

        /// <summary>The next value below <paramref name="bound"/>, which is at least 1.</summary>
        internal ulong Next(ulong bound)
        {
            state += 0x9E37_79B9_7F4A_7C15UL;
            var z = state;
            z = (z ^ (z >> 30)) * 0xBF58_476D_1CE4_E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D0_49BB_1331_11EBUL;
            return (z ^ (z >> 31)) % bound;
        }
    }

    /// <summary>
    /// The fuzz target's regular-expression provider: it refuses a pattern holding <c>]]</c> with EE0019's identifier and
    /// admits every other, so that the provider's refusal is reached as well as its admission.
    /// </summary>
    private sealed class FuzzValidator : IRegularExpressionValidator
    {
        public RegularExpressionVerdict Validate(string pattern, string flags) =>
            pattern.Contains("]]", StringComparison.Ordinal) ? RegularExpressionVerdict.Invalid("regexp.literal.invalid", null, 0, 0, false) : RegularExpressionVerdict.Valid;
    }

    private static int Number(string text) => int.Parse(text, NumberStyles.None, NumberFormatInfo.InvariantInfo);

    private static int Usage()
    {
        Console.Error.Write("usage: Broiler.ECMAScript.Probe --fuzz seeds [--test262 root] [--test262-json root] [--lines first:last] [--findings dir]\n");
        return 2;
    }
}
