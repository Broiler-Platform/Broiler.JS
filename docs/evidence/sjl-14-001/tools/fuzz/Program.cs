// Bundle sjl-14-001's part J: the seeded mutation session (decision-rule.md, part J), committed with window W2's change.
//
//     dotnet run --project docs/evidence/sjl-14-001/tools/fuzz -c Release -- \
//         --seeds docs/evidence/sjl-14-001/inputs/fuzz-seeds.txt --test262 <test262 checkout> --out <folder>
//
// For every line of the seed list, in a child process of its own (the same executable with --seed <id>, so that a lost
// process is a finding and not the end of the session), the session reads the seed's text (a parse-corpus input by its
// id, checked against the manifest's SHA-256, or a file of inputs/seeds/ by its path and SHA-256), makes the line's count
// of mutants from it with the operators below, from a deterministic stream seeded by the session's seed and the seed's
// id, and compiles each through CoreScript.Compile under the line's goal, with the header's time bound per compile.
// A mutant is accepted, or refused with a JavaScript SyntaxError; any other exception, a lost process or a compile past
// the bound is a finding. The session writes mutants.tsv (one line per mutant: seed, index, operator, SHA-256 of the
// text, outcome, and for a finding its type and message), findings.tsv, and each finding's text under findings/.
// It judges nothing; the part's reading does.
//
// The operators (each picks positions from the stream): delete a range of up to eight code units; duplicate a range of
// up to sixteen; insert one of the punctuators and keywords in Inserts; swap two adjacent code units; replace one code
// unit with one of Inserts' single characters; and splice a range of the text into another position.

using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.ExpressionCompiler.Runtime;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Storage;

var arguments = Arguments.Parse(args);
if (arguments.Seed is { } one)
    return Session.RunOne(arguments, one);
return Session.RunAll(arguments);

internal sealed record Arguments(string Seeds, string Test262, string Out, string? Seed, ulong SessionSeed)
{
    internal static Arguments Parse(string[] args)
    {
        string? Get(string name) => Array.IndexOf(args, name) is var i and >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        return new Arguments(
            Get("--seeds") ?? throw new ArgumentException("--seeds is required"),
            Get("--test262") ?? throw new ArgumentException("--test262 is required"),
            Get("--out") ?? throw new ArgumentException("--out is required"),
            Get("--seed"),
            Get("--session-seed") is { } s ? ulong.Parse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture) : 0x5142026_1119UL);
    }
}

internal sealed record SeedLine(string Seed, string Goal, string Source, int Mutants, string Selection);

internal static class Session
{
    internal static readonly string[] Inserts =
    [
        "(", ")", "{", "}", "[", "]", ";", ",", "/", "`", "${", "=>", "?.", "...", "#", "@", "\n", "'", "\"", "*", "=",
        " await ", " yield ", " let ", " async ", " function ", " class ", " static ", " super ", " new.target ",
        " eval(", " import(", " export ", " continue ", " 0m ", " 08 ", " /a/v ", " \\u0061 ",
    ];

    internal static int RunAll(Arguments arguments)
    {
        var (header, lines) = ReadSeeds(arguments.Seeds);
        Directory.CreateDirectory(arguments.Out);
        var self = Environment.ProcessPath ?? throw new InvalidOperationException("no process path");
        var selfArgs = Environment.GetCommandLineArgs()[0].EndsWith(".dll", StringComparison.Ordinal) ? Environment.GetCommandLineArgs()[0] : null;
        var log = new StringBuilder("seed\texit\tseconds\n");
        foreach (var line in lines)
        {
            var start = new ProcessStartInfo(self) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
            if (selfArgs != null)
                start.ArgumentList.Add(selfArgs);
            foreach (var a in new[] { "--seeds", arguments.Seeds, "--test262", arguments.Test262, "--out", arguments.Out, "--seed", line.Seed })
                start.ArgumentList.Add(a);
            var watch = Stopwatch.StartNew();
            using var child = Process.Start(start)!;
            var stdout = child.StandardOutput.ReadToEndAsync();
            var stderr = child.StandardError.ReadToEndAsync();
            // The bound per compile times the line's count of mutants, and a minute besides, for the process itself.
            var limit = TimeSpan.FromMilliseconds(header.BoundMs * (long)line.Mutants + 60_000);
            if (!child.WaitForExit(limit))
            {
                child.Kill(entireProcessTree: true);
                File.AppendAllText(Path.Combine(arguments.Out, "findings.tsv"), $"{line.Seed}\t-\tprocess\tlost: past {limit}\t\n");
            }
            else if (child.ExitCode != 0)
            {
                File.AppendAllText(Path.Combine(arguments.Out, "findings.tsv"),
                    $"{line.Seed}\t-\tprocess\tlost: exit {child.ExitCode}\t{Flatten(stderr.Result)}\n");
            }
            log.Append(CultureInfo.InvariantCulture, $"{line.Seed}\t{(child.HasExited ? child.ExitCode : -1)}\t{watch.Elapsed.TotalSeconds:F1}\n");
            Console.Error.WriteLine($"{line.Seed}: {(child.HasExited ? "exit " + child.ExitCode : "killed")}");
        }

        File.WriteAllText(Path.Combine(arguments.Out, "processes.tsv"), log.ToString());
        return 0;
    }

    internal static int RunOne(Arguments arguments, string id)
    {
        var (header, lines) = ReadSeeds(arguments.Seeds);
        var line = lines.Single(l => l.Seed == id);
        var text = Load(header, line, arguments);
        var random = new Stream(arguments.SessionSeed ^ Fnv(id));
        var output = new StringBuilder();
        var findings = new StringBuilder();
        for (var index = 0; index < line.Mutants; index++)
        {
            var (op, mutant) = Mutate(text, random);
            var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(mutant)));
            var (outcome, type, message) = Compile(mutant, line.Goal == "module", header.BoundMs);
            output.Append(CultureInfo.InvariantCulture, $"{id}\t{index}\t{op}\t{hash}\t{outcome}\t{type}\t{Flatten(message)}\n");
            if (outcome == "finding")
            {
                findings.Append(CultureInfo.InvariantCulture, $"{id}\t{index}\t{type}\t{Flatten(message)}\t{hash}\n");
                Directory.CreateDirectory(Path.Combine(arguments.Out, "findings"));
                File.WriteAllText(Path.Combine(arguments.Out, "findings", $"{id.Replace('/', '_')}-{index}.js"), mutant);
            }
        }

        lock (typeof(Session))
        {
            File.AppendAllText(Path.Combine(arguments.Out, "mutants.tsv"), output.ToString());
            if (findings.Length > 0)
                File.AppendAllText(Path.Combine(arguments.Out, "findings.tsv"), findings.ToString());
        }

        return 0;
    }

    private static (string Outcome, string Type, string Message) Compile(string text, bool module, int boundMs)
    {
        (string, string, string) result = ("finding", "timeout", $"past the bound of {boundMs} ms");
        var worker = new Thread(() =>
        {
            try
            {
                // A compile builds its SyntaxError in a context, made on the thread that uses it, as the parse-corpus
                // tool's K makes one.
                using var context = new JSContext(experimentalFeatures: JavaScriptFeatureFlags.AllExperimentalEs2026);
                CoreScript.Compile(new StringSpan(text), location: "mutant.js", args: null, codeCache: PassThrough.Instance,
                    compilationOptions: null, isModule: module);
                result = ("accepted", "", "");
            }
            catch (JSException exception) when (ErrorName(exception) == "SyntaxError")
            {
                result = ("refused", "SyntaxError", exception.Message);
            }
            catch (Exception exception)
            {
                result = ("finding", exception.GetType().FullName ?? "?", exception.Message);
            }
        }, 16 * 1024 * 1024);
        worker.Start();
        return worker.Join(boundMs) ? result : ("finding", "timeout", $"past the bound of {boundMs} ms");
    }

    private static string ErrorName(JSException exception)
    {
        try
        {
            var name = exception.Error?[KeyStrings.GetOrCreate("name")];
            return name == null || name.IsUndefined ? "" : name.ToString();
        }
        catch (Exception)
        {
            return "";
        }
    }

    private static (string Op, string Text) Mutate(string text, Stream random)
    {
        if (text.Length == 0)
            return ("insert", Inserts[random.Next(Inserts.Length)]);

        var at = random.Next(text.Length);
        switch (random.Next(6))
        {
            case 0:
                return ("delete", text.Remove(at, Math.Min(1 + random.Next(8), text.Length - at)));
            case 1:
                var length = Math.Min(1 + random.Next(16), text.Length - at);
                return ("duplicate", text.Insert(at, text.Substring(at, length)));
            case 2:
                return ("insert", text.Insert(at, Inserts[random.Next(Inserts.Length)]));
            case 3:
                if (at + 1 >= text.Length)
                    return ("swap", text);
                var chars = text.ToCharArray();
                (chars[at], chars[at + 1]) = (chars[at + 1], chars[at]);
                return ("swap", new string(chars));
            case 4:
                var single = Inserts.Where(i => i.Length == 1).ToArray();
                return ("replace", text.Remove(at, 1).Insert(at, single[random.Next(single.Length)]));
            default:
                var from = random.Next(text.Length);
                var count = Math.Min(1 + random.Next(32), text.Length - from);
                return ("splice", text.Insert(at, text.Substring(from, count)));
        }
    }

    private static string Load(SeedHeader header, SeedLine line, Arguments arguments)
    {
        if (line.Source == "corpus")
        {
            var row = header.Corpus(line.Seed);
            var root = row.Origin == "test262" ? arguments.Test262 : header.CorpusDirectory;
            var bytes = File.ReadAllBytes(Path.Combine(root, row.Path.Replace('/', Path.DirectorySeparatorChar)));
            var actual = Convert.ToHexStringLower(SHA256.HashData(bytes));
            if (actual != row.Sha256)
                throw new InvalidDataException($"{line.Seed}: SHA-256 {actual}, the manifest says {row.Sha256}");
            return new UTF8Encoding(false, true).GetString(bytes);
        }

        // A file of inputs/seeds/: its path, from the seed list's folder, in the seed column, and its SHA-256 in the
        // source column.
        var file = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(arguments.Seeds))!, line.Seed.Replace('/', Path.DirectorySeparatorChar));
        var data = File.ReadAllBytes(file);
        if (Convert.ToHexStringLower(SHA256.HashData(data)) != line.Source)
            throw new InvalidDataException($"{line.Seed} is not the seed its line names (SHA-256)");
        return new UTF8Encoding(false, true).GetString(data);
    }

    private static (SeedHeader Header, List<SeedLine> Lines) ReadSeeds(string path)
    {
        var header = new SeedHeader(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var lines = new List<SeedLine>();
        var seenColumns = false;
        foreach (var raw in File.ReadAllLines(path))
        {
            if (raw.StartsWith("#%", StringComparison.Ordinal))
            {
                header.Read(raw[2..].Trim().Split('\t'));
                continue;
            }
            if (raw.Length == 0 || raw.StartsWith('#'))
                continue;
            if (!seenColumns)
            {
                seenColumns = true;
                continue;
            }
            var f = raw.Split('\t');
            lines.Add(new SeedLine(f[0], f[1], f[2], int.Parse(f[3], CultureInfo.InvariantCulture), f.Length > 4 ? f[4] : ""));
        }
        return (header, lines);
    }

    private static string Flatten(string text) => text.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');

    private static ulong Fnv(string text)
    {
        var hash = 14695981039346656037UL;
        foreach (var c in text)
            hash = (hash ^ c) * 1099511628211UL;
        return hash;
    }

    private sealed class PassThrough : ICodeCache
    {
        internal static readonly PassThrough Instance = new();

        public JSFunctionDelegate GetOrCreate(in JSCode code) => code.Compiler().CompileWithNestedLambdas();
    }
}

internal sealed record CorpusRow(string Origin, string Path, string Sha256);

internal sealed class SeedHeader(string seedsDirectory)
{
    private Dictionary<string, CorpusRow>? rows;
    private string? manifest;

    internal int BoundMs { get; private set; } = 10_000;

    internal string CorpusDirectory => System.IO.Path.GetDirectoryName(Manifest)!;

    private string Manifest => manifest ?? throw new InvalidDataException("the seed list names no corpus manifest");

    internal void Read(string[] fields)
    {
        switch (fields[0])
        {
            case "corpus":
                // The manifest's path is the repository's; the seed list sits in docs/evidence/sjl-14-001/inputs/.
                manifest = System.IO.Path.GetFullPath(System.IO.Path.Combine(seedsDirectory, "..", "..", "..", "..", fields[1]));
                break;
            case "time-bound-per-compile-ms":
                BoundMs = int.Parse(fields[1], CultureInfo.InvariantCulture);
                break;
        }
    }

    internal CorpusRow Corpus(string id)
    {
        if (rows == null)
        {
            rows = new Dictionary<string, CorpusRow>(StringComparer.Ordinal);
            foreach (var line in File.ReadLines(Manifest))
            {
                if (line.StartsWith('#') || line.StartsWith("id\t", StringComparison.Ordinal))
                    continue;
                var f = line.Split('\t');
                rows[f[0]] = new CorpusRow(f[1], f[2], f[3]);
            }
        }

        return rows.TryGetValue(id, out var row) ? row : throw new InvalidDataException($"{id} is not in {Manifest}");
    }
}

// A deterministic stream (xorshift64*), so that a session's mutants are the same on every run of the same commit.
internal sealed class Stream(ulong seed)
{
    private ulong state = seed == 0 ? 0x9E3779B97F4A7C15UL : seed;

    internal int Next(int bound)
    {
        state ^= state >> 12;
        state ^= state << 25;
        state ^= state >> 27;
        return (int)((state * 2685821657736338717UL) % (ulong)bound);
    }
}
