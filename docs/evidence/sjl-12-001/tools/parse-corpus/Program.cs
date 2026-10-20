using System.Collections;
using System.Globalization;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Broiler.JavaScript.Ast;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Parser;
using Broiler.JavaScript.Runtime;

namespace ParseCorpus;

/// <summary>
/// Bundle SJL-12-001's parse-corpus tool. Without <c>--dump</c> it takes decision rule part C's
/// run: every manifest input under each of its variants at entry points P and K. With
/// <c>--dump</c> it takes one of part D's three runs. README.md beside this file says how.
/// </summary>
internal static class Program
{
    /// <summary>The stack of every thread the tool parses or compiles on: 256 MB (rule part C).</summary>
    internal const int StackBytes = 256 * 1024 * 1024;

    private const string Usage = """
        usage:
          parse-corpus --manifest <corpus.manifest> --test262 <suite root> --out <report[.gz]>
                       [--only <id>[,<id>...]] [--trace]
          parse-corpus --dump --manifest <corpus.manifest> --test262 <suite root> --out <report[.gz]>
                       [--order manifest|reverse] [--threads 1|2] [--text <dumps[.gz]>]
                       [--only <id>[,<id>...]] [--trace]

        Run with TZ=UTC and with LANG, every LC_* variable and every BROILER_* variable unset;
        the tool refuses any other environment.
        """;

    private static int Main(string[] args)
    {
        Options options;
        try
        {
            options = Options.Parse(args);
        }
        catch (ArgumentException exception)
        {
            Console.Error.WriteLine("parse-corpus: " + exception.Message);
            Console.Error.WriteLine(Usage);
            return 2;
        }

        var refusal = EnvironmentRefusal();
        if (refusal != null)
        {
            Console.Error.WriteLine("parse-corpus: refused: " + refusal);
            return 2;
        }

        Manifest manifest;
        List<(int Row, string Variant)> items;
        try
        {
            manifest = Manifest.Read(options.Manifest);
            var rows = options.Only == null
                ? manifest.Rows
                : manifest.Rows.Where(row => options.Only.Contains(row.Id)).ToList();
            if (options.Only != null && rows.Count != options.Only.Count)
                throw new InvalidDataException("--only names an id the manifest does not have: "
                    + string.Join(",", options.Only.Except(manifest.Rows.Select(row => row.Id), StringComparer.Ordinal)));

            manifest.Verify(options.Test262, rows, Console.Error);

            var rowIndex = manifest.Rows.Select((row, index) => (row.Id, index)).ToDictionary(pair => pair.Id, pair => pair.index, StringComparer.Ordinal);
            items = rows.SelectMany(row => row.Variants.Select(variant => (rowIndex[row.Id], variant))).ToList();
        }
        catch (Exception exception) when (exception is InvalidDataException or IOException or UnauthorizedAccessException or FormatException or OverflowException or System.Text.Json.JsonException or DecoderFallbackException)
        {
            Console.Error.WriteLine("parse-corpus: " + exception.Message);
            return 2;
        }

        var started = DateTime.UtcNow;
        var lines = options.Dump ? RunDump(options, manifest, items) : RunAcceptance(options, manifest, items);
        WriteReport(options, manifest, lines);
        Console.Error.WriteLine(
            $"parse-corpus: {items.Count.ToString(CultureInfo.InvariantCulture)} input variant(s), {lines.Report.Count.ToString(CultureInfo.InvariantCulture)} row(s), "
            + $"{(DateTime.UtcNow - started).TotalSeconds.ToString("F1", CultureInfo.InvariantCulture)} s, written to {options.Out}");
        return 0;
    }

    // ---- part C: P and K, on one thread ---------------------------------------------------------

    private static RunResult RunAcceptance(Options options, Manifest manifest, List<(int Row, string Variant)> items)
    {
        var results = new (Outcome P, Outcome K)[items.Count];

        RunOnThreads(1, (_, _, _) =>
        {
            using var context = Entries.NewContext();
            for (var i = 0; i < items.Count; i++)
            {
                var row = manifest.Rows[items[i].Row];
                var variant = items[i].Variant;
                Progress(options, i, items.Count, row.Id, variant);

                if (row.Goal == "json")
                {
                    var none = new Outcome(Outcome.NotApplicable, null, Entries.NoEntryPoint);
                    results[i] = (none, none);
                    continue;
                }

                var text = Compose(manifest, variant, manifest.Load(options.Test262, row));
                var module = variant == "module";
                results[i] = (Entries.Parse(text, module, out _), Entries.Compile(text, module, row.SourceName));
            }
        });

        var report = new List<string>(items.Count * 2);
        for (var i = 0; i < items.Count; i++)
        {
            var id = manifest.Rows[items[i].Row].Id;
            report.Add(OutcomeLine(id, items[i].Variant, "P", results[i].P));
            report.Add(OutcomeLine(id, items[i].Variant, "K", results[i].K));
        }

        return new RunResult(report, ["id", "variant", "entry", "outcome", "type", "message"], ExtraHeader: []);
    }

    private static string OutcomeLine(string id, string variant, string entry, Outcome outcome)
        => string.Join('\t', id, variant, entry, outcome.Kind, Text.Field(outcome.Type), Text.Field(outcome.Message));

    // ---- part D: the canonical dumps, in one of the three runs ---------------------------------

    private static RunResult RunDump(Options options, Manifest manifest, List<(int Row, string Variant)> items)
    {
        var pass = ForLoopPass.Find();
        Console.Error.WriteLine("parse-corpus: for-loop pass " + pass.Status);

        var order = Enumerable.Range(0, items.Count).ToArray();
        if (options.Order == "reverse")
            Array.Reverse(order);

        var slots = options.Threads == 1 ? new[] { "1" } : ["A", "B"];
        var results = new DumpResult[slots.Length][];
        for (var s = 0; s < slots.Length; s++)
            results[s] = new DumpResult[items.Count];

        using var textSink = options.Text == null ? null : new TextSink(options.Text);
        var texts = new string?[1];

        // On two threads every input is parsed and prepared twice at once, once on each: the
        // barrier's post-phase action reads the next input's text, then both threads start on it.
        using var barrier = new Barrier(slots.Length, completed =>
        {
            var phase = (int)completed.CurrentPhaseNumber;
            if (phase < order.Length)
            {
                var item = items[order[phase]];
                var row = manifest.Rows[item.Row];
                texts[0] = row.Goal == "json" ? null : Compose(manifest, item.Variant, manifest.Load(options.Test262, row));
            }
        });

        RunOnThreads(slots.Length, (slot, ready, cancel) =>
        {
            using var context = Entries.NewContext();
            ready();
            for (var k = 0; k < order.Length; k++)
            {
                var i = order[k];
                var row = manifest.Rows[items[i].Row];
                var variant = items[i].Variant;
                if (slot == 0)
                    Progress(options, k, order.Length, row.Id, variant);

                barrier.SignalAndWait(cancel);
                results[slot][i] = DumpOne(texts[0], variant == "module", pass, textSink, $"{row.Id} {variant} {slots[slot]}");
            }
        });

        var report = new List<string>(items.Count * slots.Length);
        for (var i = 0; i < items.Count; i++)
        {
            var id = manifest.Rows[items[i].Row].Id;
            for (var s = 0; s < slots.Length; s++)
            {
                var r = results[s][i];
                report.Add(string.Join('\t', id, items[i].Variant, slots[s], r.Parse, r.ParserHash, r.Pass, r.PassHash));
            }
        }

        return new RunResult(
            report,
            ["id", "variant", "slot", "parse", "parser-sha256", "pass", "pass-sha256"],
            ExtraHeader: [("for-loop-pass", pass.Status)]);
    }

    private readonly record struct DumpResult(string Parse, string ParserHash, string Pass, string PassHash);

    private static DumpResult DumpOne(string? text, bool module, ForLoopPass pass, TextSink? sink, string label)
    {
        if (text == null)
            return new DumpResult(Outcome.NotApplicable, "-", "-", "-");

        var parse = Entries.Parse(text, module, out var program);
        if (parse.Kind != Outcome.Accepted)
            return new DumpResult(parse.Kind, "-", "-", "-");

        // The parser's tree is dumped before the pass is called: the pass may prepare it in place.
        var parserText = DumpText(() => CanonicalDump.Of(program!));
        sink?.Write(label + " parser", parserText);
        var parserHash = CanonicalDump.Hash(parserText);

        if (!pass.Found)
            return new DumpResult(Outcome.Accepted, parserHash, "absent", "-");

        string passText, passStatus;
        try
        {
            var prepared = pass.Prepare(program!);
            passText = DumpText(() => prepared == null ? "program: null\n" : CanonicalDump.Of(prepared));
            passStatus = "prepared";
        }
        catch (Exception exception)
        {
            // Where the pass throws, the dump is of the exception's type and message in its place.
            passText = CanonicalDump.OfException(exception);
            passStatus = "threw";
        }

        sink?.Write(label + " " + passStatus, passText);
        return new DumpResult(Outcome.Accepted, parserHash, passStatus, CanonicalDump.Hash(passText));
    }

    // A dump that cannot be completed is recorded as the failure, so the run still completes.
    private static string DumpText(Func<string> dump)
    {
        try
        {
            return dump();
        }
        catch (Exception exception)
        {
            return "dump failed " + exception.GetType().FullName + " " + Text.Quote(exception.Message) + "\n";
        }
    }

    // ---- shared ----------------------------------------------------------------------------------

    /// <summary>
    /// A variant's text. Strict is composed by text, as format 1 says Broiler.JS's tool does: the
    /// manifest's strict prologue followed by the text. Every other variant is the text unchanged.
    /// </summary>
    private static string Compose(Manifest manifest, string variant, string text)
        => variant == "strict" ? manifest.StrictPrologue + text : text;

    /// <summary>
    /// Runs <paramref name="work"/> on <paramref name="count"/> threads of <see cref="StackBytes"/>
    /// each and waits for them. Each thread makes its context in turn (the second after the first
    /// has made its own) and then calls the <c>ready</c> callback. A failure on any thread cancels
    /// the token the others wait with, and is rethrown here.
    /// </summary>
    private static void RunOnThreads(int count, Action<int, Action, CancellationToken> work)
    {
        var failures = new Exception?[count];
        using var cancellation = new CancellationTokenSource();
        var turns = Enumerable.Range(0, count).Select(_ => new ManualResetEventSlim(false)).ToArray();
        turns[0].Set();

        var threads = Enumerable.Range(0, count).Select(index => new Thread(() =>
        {
            try
            {
                turns[index].Wait();
                work(index, () =>
                {
                    if (index + 1 < count)
                        turns[index + 1].Set();
                }, cancellation.Token);
            }
            catch (Exception exception)
            {
                failures[index] = exception;
                cancellation.Cancel();
                // Let a waiting thread proceed to its own failure rather than wait for ever.
                foreach (var turn in turns)
                    turn.Set();
            }
        }, StackBytes)
        {
            Name = "parse-corpus-" + index.ToString(CultureInfo.InvariantCulture),
        }).ToArray();

        foreach (var thread in threads)
            thread.Start();
        foreach (var thread in threads)
            thread.Join();
        foreach (var turn in turns)
            turn.Dispose();

        var failure = failures.FirstOrDefault(exception => exception is not null and not OperationCanceledException)
            ?? failures.FirstOrDefault(exception => exception != null);
        if (failure != null)
            throw new InvalidOperationException("a worker thread failed: " + failure.Message, failure);
    }

    private static void Progress(Options options, int index, int count, string id, string variant)
    {
        if (options.Trace)
            Console.Error.WriteLine($"parse-corpus: {id} {variant}");
        else if (index % 10000 == 0)
            Console.Error.WriteLine($"parse-corpus: {index.ToString(CultureInfo.InvariantCulture)}/{count.ToString(CultureInfo.InvariantCulture)}");
    }

    private sealed record RunResult(List<string> Report, string[] Columns, (string Key, string Value)[] ExtraHeader);

    private static void WriteReport(Options options, Manifest manifest, RunResult result)
    {
        var header = new List<(string Key, string Value)>
        {
            ("format", "broiler-js-parse-corpus-report\t1"),
            ("mode", options.Dump ? "dump" : "acceptance"),
            ("tool", "docs/evidence/sjl-12-001/tools/parse-corpus"),
            ("manifest-sha256", manifest.Digest),
            ("manifest-rows", manifest.Rows.Count.ToString(CultureInfo.InvariantCulture)),
            ("corpus", manifest.Corpus),
            ("pin-test262", manifest.PinTest262),
            ("selection", options.Only == null ? "all" : "only\t" + string.Join(",", options.Only.Order(StringComparer.Ordinal))),
            ("order", options.Order),
            ("threads", options.Threads.ToString(CultureInfo.InvariantCulture)),
            ("thread-stack-bytes", StackBytes.ToString(CultureInfo.InvariantCulture)),
            ("environment", "TZ=UTC\tLANG unset\tLC_* unset\tBROILER_* unset"),
            ("culture", CultureInfo.CurrentCulture.Name.Length == 0 ? "(invariant)" : CultureInfo.CurrentCulture.Name),
            ("time-zone", TimeZoneInfo.Local.Id),
            ("runtime", RuntimeInformation.FrameworkDescription),
            ("os", RuntimeInformation.OSDescription + " " + RuntimeInformation.OSArchitecture),
            ("processors", Environment.ProcessorCount.ToString(CultureInfo.InvariantCulture)),
            ("context", "JSContext(experimentalFeatures: JavaScriptFeatureFlags.AllExperimentalEs2026), one per thread"),
            ("strict-variant", "the manifest's strict-prologue followed by the text"),
            ("source-names", "virtual-root or stored-root followed by the path"),
        };

        foreach (var assembly in ProductAssemblies())
        {
            var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "-";
            header.Add(("assembly", $"{assembly.GetName().Name}\t{version}\t{assembly.ManifestModule.ModuleVersionId:D}"));
        }

        header.AddRange(result.ExtraHeader);
        header.Add(("rows", result.Report.Count.ToString(CultureInfo.InvariantCulture)));

        using var stream = OpenOut(options.Out);
        using var writer = new StreamWriter(stream, new UTF8Encoding(false)) { NewLine = "\n" };
        foreach (var (key, value) in header)
            writer.WriteLine("#% " + key + "\t" + value);
        writer.WriteLine(string.Join('\t', result.Columns));
        foreach (var line in result.Report)
            writer.WriteLine(line);
        writer.WriteLine("#% end\t" + result.Report.Count.ToString(CultureInfo.InvariantCulture));
    }

    private static IEnumerable<Assembly> ProductAssemblies()
    {
        yield return typeof(AstNode).Assembly;
        yield return typeof(FastParser).Assembly;
        yield return Assembly.Load(ForLoopPass.AssemblyName);
        yield return typeof(JSContext).Assembly;
        yield return typeof(CoreScript).Assembly;
    }

    internal static Stream OpenOut(string path)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (directory != null)
            Directory.CreateDirectory(directory);

        Stream stream = File.Create(path);
        return path.EndsWith(".gz", StringComparison.Ordinal)
            ? new GZipStream(stream, CompressionLevel.Optimal)
            : stream;
    }

    /// <summary>
    /// The rule's environment: TZ=UTC and no LANG. The tool also refuses any LC_* variable, which
    /// would set the locale LANG is left unset to keep out, and any BROILER_* variable, every one of
    /// which changes how the product parses or compiles (BROILER_JS_COMPILE_STACK_BYTES,
    /// BROILER_JS_VISITOR_SEGMENT_BYTES, BROILER_JS_DUMP_PROGRAMS among them).
    /// </summary>
    private static string? EnvironmentRefusal()
    {
        var problems = new List<string>();
        var tz = Environment.GetEnvironmentVariable("TZ");
        if (tz != "UTC")
            problems.Add(tz == null ? "TZ is unset, not UTC" : $"TZ is \"{tz}\", not UTC");

        foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            var name = (string)entry.Key;
            if (name == "LANG" || name.StartsWith("LC_", StringComparison.Ordinal) || name.StartsWith("BROILER_", StringComparison.Ordinal))
                problems.Add(name + " is set");
        }

        problems.Sort(StringComparer.Ordinal);
        return problems.Count == 0 ? null : string.Join("; ", problems);
    }

    private sealed class TextSink : IDisposable
    {
        private readonly Lock gate = new();
        private readonly StreamWriter writer;

        internal TextSink(string path)
            => writer = new StreamWriter(OpenOut(path), new UTF8Encoding(false)) { NewLine = "\n" };

        internal void Write(string label, string text)
        {
            lock (gate)
            {
                writer.Write("=== ");
                writer.Write(label);
                writer.Write('\n');
                writer.Write(text);
            }
        }

        public void Dispose() => writer.Dispose();
    }

    private sealed class Options
    {
        internal string Manifest { get; private set; } = "";
        internal string Test262 { get; private set; } = "";
        internal string Out { get; private set; } = "";
        internal bool Dump { get; private set; }
        internal string Order { get; private set; } = "manifest";
        internal int Threads { get; private set; } = 1;
        internal string? Text { get; private set; }
        internal HashSet<string>? Only { get; private set; }
        internal bool Trace { get; private set; }

        internal static Options Parse(string[] args)
        {
            var options = new Options();
            for (var i = 0; i < args.Length; i++)
            {
                string Value() => i + 1 < args.Length ? args[++i] : throw new ArgumentException(args[i] + " needs a value");

                switch (args[i])
                {
                    case "--manifest": options.Manifest = Value(); break;
                    case "--test262": options.Test262 = Value(); break;
                    case "--out": options.Out = Value(); break;
                    case "--dump": options.Dump = true; break;
                    case "--order": options.Order = Value(); break;
                    case "--threads": options.Threads = int.TryParse(Value(), NumberStyles.None, CultureInfo.InvariantCulture, out var threads) ? threads : 0; break;
                    case "--text": options.Text = Value(); break;
                    case "--only": options.Only = new HashSet<string>(Value().Split(',', StringSplitOptions.RemoveEmptyEntries), StringComparer.Ordinal); break;
                    case "--trace": options.Trace = true; break;
                    default: throw new ArgumentException("unknown argument " + args[i]);
                }
            }

            if (options.Manifest.Length == 0 || options.Test262.Length == 0 || options.Out.Length == 0)
                throw new ArgumentException("--manifest, --test262 and --out are required");
            if (options.Order is not ("manifest" or "reverse"))
                throw new ArgumentException("--order is manifest or reverse");
            if (options.Threads is not (1 or 2))
                throw new ArgumentException("--threads is 1 or 2");
            if (!options.Dump && (options.Order != "manifest" || options.Threads != 1 || options.Text != null))
                throw new ArgumentException("--order, --threads and --text belong to --dump; part C runs in the manifest's order on one thread");
            if (options.Only is { Count: 0 })
                throw new ArgumentException("--only names no id");

            return options;
        }
    }
}
