// Scratch probe for SJL-12.3 task 3.11 (never committed): every diagnostic of Broiler.VM's wide front end for each
// listed test262 input and variant, with the source line that made it (from the instrumented copy's ProbeSites).
using System.Text;
using Broiler.VM.Profile.JavaScript.Compiler;
using Broiler.VM.Profile.JavaScript.Format;

static string ReadUtf8(string path)
{
    var bytes = File.ReadAllBytes(path);
    var start = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF ? 3 : 0;
    return Encoding.UTF8.GetString(bytes, start, bytes.Length - start);
}
static string Clean(string s) => (s ?? "").Replace('\t', ' ').Replace('\n', ' ').Replace('\r', ' ');
var suite = args[0]; var list = args[1]; var output = args[2]; var threads = int.Parse(args[3]);
var request = new JsCompileRequest(JsFeatureManifest.Wide, JsOutputForm.Bytecode);
const string VirtualRoot = "/test262/";
var lines = File.ReadAllLines(list).Where(l => l.Length > 0).ToArray();
var results = new string[lines.Length];

IReadOnlyList<JsModuleUnit> LoadGraph(string rootKey, string rootText)
{
    var modules = new List<JsModuleUnit>();
    var texts = new Dictionary<string, string>(StringComparer.Ordinal) { [rootKey] = rootText };
    var seen = new HashSet<string>(StringComparer.Ordinal) { rootKey };
    var pending = new Queue<string>(); pending.Enqueue(rootKey);
    string Real(string key) => Path.Combine(suite, key.Substring(VirtualRoot.Length));
    string Resolve(string referrer, string specifier)
    {
        if (!specifier.StartsWith("./") && !specifier.StartsWith("../") && !specifier.StartsWith("/")) return string.Empty;
        var dir = Path.GetDirectoryName(referrer);
        return Path.GetFullPath(specifier.StartsWith("/") ? specifier : Path.Combine(dir ?? "/", specifier)).Replace('\\', '/');
    }
    while (pending.Count != 0)
    {
        var key = pending.Dequeue();
        var p = JsFormat.SplitTypedSpecifier(key, out var type);
        if (modules.Count == 256) break;
        if (!texts.TryGetValue(key, out var text)) { text = File.Exists(Real(p)) ? ReadUtf8(Real(p)) : ""; texts[key] = text; }
        if (type.Length != 0) { modules.Add(new JsModuleUnit(key, text, SliceParseOptions.Module) { Type = type }); continue; }
        var requests = JsCompiler.Requests(text, SliceParseOptions.Module);
        if (!requests.Succeeded) { modules.Add(new JsModuleUnit(key, text, SliceParseOptions.Module)); continue; }
        var res = new List<JsResolvedRequest>();
        foreach (var r in requests.Specifiers)
        {
            var spec = JsFormat.SplitTypedSpecifier(r, out var rt);
            var resolved = Resolve(key, spec);
            if (resolved.Length == 0 || !resolved.StartsWith(VirtualRoot) || !File.Exists(Real(resolved))) { res.Add(new JsResolvedRequest(r, r)); continue; }
            var target = JsFormat.TypedSpecifier(resolved, rt);
            res.Add(new JsResolvedRequest(r, target));
            if (seen.Add(target)) pending.Enqueue(target);
        }
        modules.Add(new JsModuleUnit(key, text, SliceParseOptions.Module, res));
    }
    return modules;
}

string RunOne(string line)
{
    var parts = line.Split('\t'); var rel = parts[0]; var variant = parts[1];
    try
    {
        var text = ReadUtf8(Path.Combine(suite, rel));
        IReadOnlyList<SliceSourceDiagnostic> diags; bool ok;
        if (variant == "module")
        {
            var c = JsCompiler.Compile(Array.Empty<JsScriptUnit>(), LoadGraph(VirtualRoot + rel, text), request);
            ok = c.Succeeded; diags = c.Diagnostics;
        }
        else
        {
            var c = JsCompiler.Compile(new[] { new JsScriptUnit("main", text, SliceParseOptions.Script, variant == "strict") }, Array.Empty<JsModuleUnit>(), request);
            ok = c.Succeeded; diags = c.Diagnostics;
        }
        var sb = new StringBuilder();
        foreach (var d in diags.Take(4))
        {
            ProbeSites.Where.TryGetValue(d, out var where);
            sb.Append((int)d.Code).Append('@').Append(d.Line).Append(':').Append(d.Column).Append(' ').Append(Clean(d.Message)).Append(" {").Append(where ?? "?").Append("} || ");
        }
        return $"{rel}\t{variant}\t{(ok ? "accepted" : "refused")}\t{diags.Count}\t{(sb.Length == 0 ? "-" : sb.ToString())}";
    }
    catch (Exception e) { return $"{rel}\t{variant}\tcrash\t-\t{e.GetType().Name}: {Clean(e.Message)}"; }
}

var sw = System.Diagnostics.Stopwatch.StartNew();
var t = new Thread(() => Parallel.For(0, lines.Length, new ParallelOptions { MaxDegreeOfParallelism = threads }, i => results[i] = RunOne(lines[i])), 256 * 1024 * 1024);
t.Start(); t.Join();
File.WriteAllLines(output, results);
Console.WriteLine($"siteprobe: {lines.Length} inputs in {sw.Elapsed.TotalSeconds:F1}s");
