using System.Collections;
using System.Globalization;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Broiler.JavaScript.Ast;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.Parser;
using Broiler.JavaScript.Runtime;
using ParseCorpus;

namespace SideBySide;

/// <summary>
/// Bundle SJL-14-001's side-by-side tool (decision rule part C): every manifest input under each of its variants at
/// entry points P, B, K and K-on, on one thread, with the canonical dumps of P's and B's trees. README.md beside this
/// file says how it is run and what its report holds.
/// </summary>
internal static class Program
{
    /// <summary>The stack of the thread the tool parses and compiles on: 256 MB, as bundle sjl-12-001's tool.</summary>
    private const int StackBytes = 256 * 1024 * 1024;

    private const string Usage = """
        usage:
          side-by-side --manifest <corpus.manifest> --test262 <suite root> --out <report[.gz]>
                       [--entries P,B,K,K-on] [--dump] [--text <dumps[.gz]>] [--only <id>[,<id>...]] [--trace]

        Run with TZ=UTC and with LANG, every LC_* variable and every BROILER_* variable unset;
        the tool refuses any other environment.
        """;

    private static readonly string[] AllEntries = ["P", "B", "K", "K-on"];

    private static int Main(string[] args)
    {
        Options options;
        try
        {
            options = Options.Parse(args);
        }
        catch (ArgumentException exception)
        {
            Console.Error.WriteLine("side-by-side: " + exception.Message);
            Console.Error.WriteLine(Usage);
            return 2;
        }

        var refusal = EnvironmentRefusal();
        if (refusal != null)
        {
            Console.Error.WriteLine("side-by-side: refused: " + refusal);
            return 2;
        }

        // As sjl-12-001's tool: every compile on the tool's own thread.
        CompilationStack.SizeBytes = 0;

        Manifest manifest;
        List<(ManifestRow Row, string Variant)> items;
        try
        {
            manifest = Manifest.Read(options.Manifest);
            var rows = options.Only == null
                ? manifest.Rows
                : manifest.Rows.Where(row => options.Only.Contains(row.Id)).ToList();
            if (options.Only != null && rows.Count != options.Only.Count)
                throw new InvalidDataException("--only names an id the manifest does not have");
            manifest.Verify(options.Test262, rows, Console.Error);
            items = rows.SelectMany(row => row.Variants.Select(variant => (row, variant))).ToList();
        }
        catch (Exception exception) when (exception is InvalidDataException or IOException or UnauthorizedAccessException or FormatException or System.Text.Json.JsonException or DecoderFallbackException)
        {
            Console.Error.WriteLine("side-by-side: " + exception.Message);
            return 2;
        }

        var bridge = Bridge.Find();
        var report = new List<string>(items.Count * options.Entries.Count);
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                Run(options, manifest, items, bridge, report);
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        }, StackBytes)
        { Name = "side-by-side" };
        thread.Start();
        thread.Join();
        if (failure != null)
            throw new InvalidOperationException("the worker thread failed: " + failure.Message, failure);

        WriteReport(options, manifest, bridge, report);
        Console.Error.WriteLine($"side-by-side: {items.Count.ToString(CultureInfo.InvariantCulture)} input variant(s), {report.Count.ToString(CultureInfo.InvariantCulture)} row(s), written to {options.Out}");
        return 0;
    }

    private static void Run(Options options, Manifest manifest, List<(ManifestRow Row, string Variant)> items, Bridge bridge, List<string> report)
    {
        using var context = Entries.NewContext();
        using var writer = new DumpWriter();
        using var text = options.Text == null ? null : new StreamWriter(OpenOut(options.Text), new UTF8Encoding(false)) { NewLine = "\n" };
        for (var i = 0; i < items.Count; i++)
        {
            var (row, variant) = items[i];
            if (options.Trace)
                Console.Error.WriteLine($"side-by-side: {row.Id} {variant}");
            else if (i % 10000 == 0)
                Console.Error.WriteLine($"side-by-side: {i.ToString(CultureInfo.InvariantCulture)}/{items.Count.ToString(CultureInfo.InvariantCulture)}");

            var source = manifest.Load(options.Test262, row);
            var composed = variant == "strict" ? manifest.StrictPrologue + source : source;
            var module = variant == "module";
            foreach (var entry in options.Entries)
            {
                Outcome outcome;
                AstProgram? program = null;
                switch (entry)
                {
                    case "P":
                        outcome = Entries.Parse(composed, module, out program);
                        break;
                    case "B":
                        outcome = bridge.Parse(composed, module, out program);
                        break;
                    case "K":
                        outcome = Entries.Compile(composed, module, row.SourceName);
                        break;
                    default:
                        outcome = bridge.Compile(context, composed, module, row.SourceName);
                        break;
                }

                var hash = "-";
                if (options.Dump && program != null)
                    hash = Dump(writer, program, text, $"{row.Id} {variant} {entry}");
                report.Add(string.Join('\t', row.Id, variant, entry, outcome.Kind, Text.Field(outcome.Type), Text.Field(outcome.Message), hash));
            }
        }
    }

    // A tree's canonical dump (bundle sjl-12-001's), hashed; dump-failed and the exception's type where it cannot be completed.
    private static string Dump(DumpWriter writer, AstProgram program, StreamWriter? text, string label)
    {
        var copy = text == null ? null : new MemoryStream();
        writer.Start(copy);
        try
        {
            CanonicalDump.Write(writer, program);
            var hash = writer.Finish();
            if (text != null)
            {
                text.Write("=== " + label + "\n");
                text.Flush();
                copy!.Position = 0;
                copy.CopyTo(text.BaseStream);
            }

            return hash;
        }
        catch (Exception exception)
        {
            try
            {
                writer.Finish();
            }
            catch (Exception)
            {
                // The failure already caught is the one reported.
            }

            return "dump-failed:" + (exception.GetType().FullName ?? "?");
        }
    }

    private static void WriteReport(Options options, Manifest manifest, Bridge bridge, List<string> report)
    {
        var header = new List<(string Key, string Value)>
        {
            ("format", "broiler-js-side-by-side-report\t1"),
            ("tool", "docs/evidence/sjl-14-001/tools/side-by-side"),
            ("manifest-sha256", manifest.Digest),
            ("manifest-rows", manifest.Rows.Count.ToString(CultureInfo.InvariantCulture)),
            ("pin-test262", manifest.PinTest262),
            ("selection", options.Only == null ? "all" : "only\t" + string.Join(",", options.Only.Order(StringComparer.Ordinal))),
            ("entries", string.Join(",", options.Entries)),
            ("dump", options.Dump ? "sjl-12-001's canonical dump of P's and B's trees" : "none"),
            ("bridge", bridge.Status),
            ("switch", bridge.SwitchStatus),
            ("thread-stack-bytes", StackBytes.ToString(CultureInfo.InvariantCulture)),
            ("compile-stack-bytes", CompilationStack.SizeBytes.ToString(CultureInfo.InvariantCulture)),
            ("environment", "TZ=UTC\tLANG unset\tLC_* unset\tBROILER_* unset"),
            ("runtime", RuntimeInformation.FrameworkDescription),
            ("os", RuntimeInformation.OSDescription + " " + RuntimeInformation.OSArchitecture),
            ("context", "JSContext(experimentalFeatures: JavaScriptFeatureFlags.AllExperimentalEs2026), one"),
        };
        foreach (var assembly in new[] { typeof(AstNode).Assembly, typeof(FastParser).Assembly, typeof(JSContext).Assembly, typeof(CoreScript).Assembly })
        {
            var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "-";
            header.Add(("assembly", $"{assembly.GetName().Name}\t{version}"));
        }

        header.Add(("rows", report.Count.ToString(CultureInfo.InvariantCulture)));
        using var stream = OpenOut(options.Out);
        using var writer = new StreamWriter(stream, new UTF8Encoding(false)) { NewLine = "\n" };
        foreach (var (key, value) in header)
            writer.WriteLine("#% " + key + "\t" + value);
        writer.WriteLine("id\tvariant\tentry\toutcome\ttype\tmessage\tdump-sha256");
        foreach (var line in report)
            writer.WriteLine(line);
        writer.WriteLine("#% end\t" + report.Count.ToString(CultureInfo.InvariantCulture));
    }

    private static Stream OpenOut(string path)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (directory != null)
            Directory.CreateDirectory(directory);
        Stream stream = File.Create(path);
        return path.EndsWith(".gz", StringComparison.Ordinal) ? new GZipStream(stream, CompressionLevel.Optimal) : stream;
    }

    // As sjl-12-001's tool: TZ=UTC, and no LANG, LC_* or BROILER_* variable.
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

    /// <summary>
    /// Entry points B and K-on, found by name when the tool runs, as sjl-12-001's tool finds the for-loop pass: the
    /// bridge, <c>Broiler.JavaScript.Parser.Bridge.SharedSyntaxBridge.Parse(in StringSpan, BridgeRequest)</c>, and the
    /// compiler's test-only switch, the internal <c>JSCompilationOptions.ParseThroughBridge</c>. At W1's first parent
    /// neither exists, and B and K-on answer <c>absent</c>.
    /// </summary>
    private sealed class Bridge
    {
        private const string BridgeType = "Broiler.JavaScript.Parser.Bridge.SharedSyntaxBridge";
        private const string RequestType = "Broiler.JavaScript.Parser.Bridge.BridgeRequest";

        private MethodInfo? parse;
        private object? script;
        private object? module;
        private PropertyInfo? @switch;

        internal string Status { get; private set; } = "absent";

        internal string SwitchStatus { get; private set; } = "absent";

        internal static Bridge Find()
        {
            var bridge = new Bridge();
            const BindingFlags any = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            var assembly = typeof(FastParser).Assembly;
            var type = assembly.GetType(BridgeType, throwOnError: false);
            var request = assembly.GetType(RequestType, throwOnError: false);
            var method = type?.GetMethod("Parse", any, binder: null, [typeof(StringSpan).MakeByRefType(), request ?? typeof(object)], modifiers: null);
            if (type == null || request == null || method == null)
            {
                bridge.Status = $"absent: {assembly.GetName().Name} declares no {BridgeType}.Parse(in StringSpan, BridgeRequest)";
            }
            else
            {
                bridge.parse = method;
                bridge.script = request.GetProperty("Script", any)?.GetValue(null);
                bridge.module = request.GetProperty("Module", any)?.GetValue(null);
                bridge.Status = "found: " + BridgeType + ".Parse(in StringSpan, BridgeRequest), requests BridgeRequest.Script and BridgeRequest.Module";
            }

            var property = typeof(JSCompilationOptions).GetProperty("ParseThroughBridge", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (property is { CanWrite: true, PropertyType: var t } && t == typeof(bool))
            {
                bridge.@switch = property;
                bridge.SwitchStatus = "found: JSCompilationOptions.ParseThroughBridge";
            }
            else
            {
                bridge.SwitchStatus = "absent: JSCompilationOptions declares no ParseThroughBridge";
            }

            return bridge;
        }

        /// <summary>B, the bridge: accepted with the tree, refused (a <c>FastParseException</c>), crashed, or absent.</summary>
        internal Outcome Parse(string text, bool module, out AstProgram? program)
        {
            program = null;
            if (parse == null)
                return new Outcome("absent", null, null);
            try
            {
                program = (AstProgram)parse.Invoke(null, [new StringSpan(text), module ? this.module : script])!;
                return Outcome.Accept();
            }
            catch (TargetInvocationException wrapped) when (wrapped.InnerException is FastParseException refusal)
            {
                return new Outcome(Outcome.Refused, refusal.GetType().FullName, refusal.Message);
            }
            catch (TargetInvocationException wrapped) when (wrapped.InnerException != null)
            {
                return Outcome.Crash(wrapped.InnerException);
            }
        }

        /// <summary>K-on, the whole compile with the switch on: answered as sjl-12-001's K, or absent.</summary>
        internal Outcome Compile(JSContext context, string text, bool module, string sourceName)
        {
            if (@switch == null)
                return new Outcome("absent", null, null);

            // The options K's compile takes (CoreScript's AsScript of the context's), with the switch on.
            object boxed = context.CompilationOptions with { IsModule = false, AllowTopLevelAwait = false };
            @switch.SetValue(boxed, true);
            var options = (JSCompilationOptions)boxed;
            try
            {
                CoreScript.Compile(new StringSpan(text), location: sourceName, args: null, codeCache: PassThrough.Instance, compilationOptions: options, isModule: module);
                return Outcome.Accept();
            }
            catch (JSException exception)
            {
                var name = ErrorName(exception);
                var type = exception.GetType().FullName + ":" + name;
                return name == "SyntaxError"
                    ? new Outcome(Outcome.Refused, type, exception.Message)
                    : new Outcome(Outcome.Crashed, type, exception.Message);
            }
            catch (Exception exception)
            {
                return Outcome.Crash(exception);
            }
        }

        private static string ErrorName(JSException exception)
        {
            try
            {
                var error = exception.Error;
                if (error == null)
                    return "(none)";
                var name = error[Broiler.JavaScript.Storage.KeyStrings.GetOrCreate("name")];
                return name.IsUndefined ? "(undefined)" : name.ToString();
            }
            catch (Exception reading)
            {
                return "(unreadable: " + reading.GetType().FullName + ")";
            }
        }

        private sealed class PassThrough : ICodeCache
        {
            internal static readonly PassThrough Instance = new();

            public JSFunctionDelegate GetOrCreate(in JSCode code) => Broiler.JavaScript.ExpressionCompiler.Runtime.RuntimeAssembly.CompileWithNestedLambdas(code.Compiler());
        }
    }

    private sealed class Options
    {
        internal string Manifest { get; private set; } = "";
        internal string Test262 { get; private set; } = "";
        internal string Out { get; private set; } = "";
        internal bool Dump { get; private set; }
        internal string? Text { get; private set; }
        internal bool Trace { get; private set; }
        internal HashSet<string>? Only { get; private set; }
        internal List<string> Entries { get; private set; } = [.. AllEntries];

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
                    case "--text": options.Text = Value(); break;
                    case "--trace": options.Trace = true; break;
                    case "--only": options.Only = Value().Split(',', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.Ordinal); break;
                    case "--entries":
                        options.Entries = Value().Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
                        if (options.Entries.Any(entry => !AllEntries.Contains(entry)))
                            throw new ArgumentException("--entries takes P, B, K and K-on");
                        break;
                    default: throw new ArgumentException("unknown argument " + args[i]);
                }
            }

            if (options.Manifest.Length == 0 || options.Test262.Length == 0 || options.Out.Length == 0)
                throw new ArgumentException("--manifest, --test262 and --out are required");
            if (options.Text != null && !options.Dump)
                throw new ArgumentException("--text needs --dump");
            return options;
        }
    }
}
